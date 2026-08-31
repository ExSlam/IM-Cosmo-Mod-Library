using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using SaveNLoadFixes.Safety;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repair A09 / Area #2 A2-A08: closes the explicit final-frame checkpoint gap
    /// in the audited ActiveDialogueController/vn_actions semantic callback carriers.
    ///
    /// The callback itself is deliberately not serialized. Each concrete iterator is
    /// registered when its factory returns, bound to the current LoadEpoch, and owns a
    /// SemanticCallbackFinalFrame lease until callback completion. A discarded F9
    /// iterator is suppressed before it can invoke its old callback against the target
    /// timeline.
    /// </summary>
    internal static class SemanticCallbackCheckpointRepair
    {
        private sealed class PendingCallback
        {
            internal CheckpointBlockerLease Lease;
            internal long Epoch;
            internal string Carrier;
            private int terminal;

            internal bool TryFinishLease()
            {
                if (Interlocked.Exchange(ref this.terminal, 1) != 0)
                {
                    return false;
                }

                CheckpointBlockerLease lease = this.Lease;
                this.Lease = null;
                if (lease != null)
                {
                    lease.Dispose();
                }

                return true;
            }

            ~PendingCallback()
            {
                if (this.TryFinishLease())
                {
                    Interlocked.Decrement(ref activeTrackedCount);
                    Interlocked.Increment(ref garbageCollectedCleanupCount);
                }
            }
        }

        private static readonly object Sync = new object();
        private static readonly ConditionalWeakTable<object, PendingCallback> PendingByIterator =
            new ConditionalWeakTable<object, PendingCallback>();

        private static long activeTrackedCount;
        private static long registeredCount;
        private static long completedCount;
        private static long staleSuppressionCount;
        private static long faultCleanupCount;
        private static long garbageCollectedCleanupCount;
        private static string lastDiagnostic = string.Empty;
        private static int unhealthyRegistrationWarningLogged;

        internal static bool IsImplemented
        {
            get { return SemanticCallbackCheckpointPatchHealth.IsHealthy; }
        }

        internal static long ActiveTrackedCount
        {
            get { return Interlocked.Read(ref activeTrackedCount); }
        }

        internal static long RegisteredCount
        {
            get { return Interlocked.Read(ref registeredCount); }
        }

        internal static long CompletedCount
        {
            get { return Interlocked.Read(ref completedCount); }
        }

        internal static long StaleSuppressionCount
        {
            get { return Interlocked.Read(ref staleSuppressionCount); }
        }

        internal static long FaultCleanupCount
        {
            get { return Interlocked.Read(ref faultCleanupCount); }
        }

        internal static long GarbageCollectedCleanupCount
        {
            get { return Interlocked.Read(ref garbageCollectedCleanupCount); }
        }

        internal static string LastDiagnostic
        {
            get
            {
                lock (Sync)
                {
                    return lastDiagnostic;
                }
            }
        }

        internal static void Register(
            IEnumerator iterator,
            Action callback,
            MethodBase factory)
        {
            if (iterator == null || callback == null)
            {
                return;
            }

            // As with Task 3, never create a lease when the exact selective patch
            // surface is incomplete. The safe fallback is vanilla behavior rather
            // than a blocker whose terminal callback seam is not under SNLF control.
            if (!SemanticCallbackCheckpointPatchHealth.IsHealthy)
            {
                if (Interlocked.Exchange(ref unhealthyRegistrationWarningLogged, 1) == 0)
                {
                    Debug.LogWarning(
                        SaveNLoadFixesConstants.LogPrefix +
                        "Semantic callback checkpoint repair is not healthy; callback " +
                        "blockers will not be acquired.");
                }
                return;
            }

            long epoch = LoadEpoch.Capture();
            string carrier = DescribeFactory(factory);
            PendingCallback pending = new PendingCallback
            {
                Epoch = epoch,
                Carrier = carrier,
                Lease = CheckpointGate.Acquire(
                    CheckpointBlockerKind.SemanticCallbackFinalFrame,
                    BuildBlockerDetail(carrier, epoch))
            };

            bool duplicate = false;
            lock (Sync)
            {
                PendingCallback existing;
                if (PendingByIterator.TryGetValue(iterator, out existing))
                {
                    duplicate = true;
                }
                else
                {
                    PendingByIterator.Add(iterator, pending);
                    lastDiagnostic =
                        "Registered semantic callback carrier '" +
                        carrier +
                        "' at load epoch " +
                        epoch.ToString(CultureInfo.InvariantCulture) +
                        ".";
                }
            }

            if (duplicate)
            {
                pending.TryFinishLease();
                GC.SuppressFinalize(pending);
                Debug.LogWarning(
                    SaveNLoadFixesConstants.LogPrefix +
                    "Duplicate semantic callback iterator registration was ignored for " +
                    carrier +
                    ".");
                return;
            }

            Interlocked.Increment(ref activeTrackedCount);
            Interlocked.Increment(ref registeredCount);
        }

        /// <summary>
        /// Prefix guard shared by the four audited generated MoveNext methods.
        /// </summary>
        internal static bool ShouldRunMoveNext(object iterator)
        {
            PendingCallback pending;
            if (!TryGetPending(iterator, out pending))
            {
                return true;
            }

            if (LoadEpoch.IsCurrent(pending.Epoch))
            {
                return true;
            }

            if (FinishAndRemove(iterator, pending))
            {
                Interlocked.Increment(ref staleSuppressionCount);
                string diagnostic =
                    "Discarded stale semantic callback carrier '" +
                    pending.Carrier +
                    "' from load epoch " +
                    pending.Epoch.ToString(CultureInfo.InvariantCulture) +
                    "; current epoch is " +
                    LoadEpoch.Current.ToString(CultureInfo.InvariantCulture) +
                    ".";
                SetLastDiagnostic(diagnostic);
                Debug.Log(SaveNLoadFixesConstants.LogPrefix + diagnostic);
            }

            return false;
        }

        /// <summary>
        /// Called from the MoveNext postfix. In all four audited iterator shapes a
        /// normal false return occurs only after callback() has executed, so the
        /// blocker lifetime includes the entire synchronous callback body.
        /// </summary>
        internal static void MarkMoveNextCompleted(object iterator)
        {
            PendingCallback pending;
            if (!TryGetPending(iterator, out pending))
            {
                return;
            }

            if (!FinishAndRemove(iterator, pending))
            {
                return;
            }

            Interlocked.Increment(ref completedCount);
            SetLastDiagnostic(
                "Semantic callback carrier '" +
                pending.Carrier +
                "' completed synchronously; checkpoint blocker released.");
        }

        /// <summary>
        /// Prevent an exception thrown by the scheduled callback from stranding the
        /// checkpoint gate. Harmony receives the original exception unchanged and
        /// therefore preserves the game's/mod's failure semantics.
        /// </summary>
        internal static void MarkMoveNextFaulted(
            object iterator,
            Exception exception)
        {
            PendingCallback pending;
            if (!TryGetPending(iterator, out pending))
            {
                return;
            }

            if (!FinishAndRemove(iterator, pending))
            {
                return;
            }

            Interlocked.Increment(ref faultCleanupCount);
            string diagnostic =
                "Semantic callback carrier '" +
                pending.Carrier +
                "' faulted; checkpoint blocker released without swallowing " +
                (exception == null ? "the original exception" : exception.GetType().Name) +
                ".";
            SetLastDiagnostic(diagnostic);
            Debug.LogError(SaveNLoadFixesConstants.LogPrefix + diagnostic);
        }

        private static bool TryGetPending(object iterator, out PendingCallback pending)
        {
            pending = null;
            if (iterator == null)
            {
                return false;
            }

            lock (Sync)
            {
                return PendingByIterator.TryGetValue(iterator, out pending);
            }
        }

        private static bool FinishAndRemove(
            object iterator,
            PendingCallback expected)
        {
            bool removed = false;
            lock (Sync)
            {
                PendingCallback current;
                if (iterator != null &&
                    PendingByIterator.TryGetValue(iterator, out current) &&
                    object.ReferenceEquals(current, expected))
                {
                    removed = PendingByIterator.Remove(iterator);
                }
            }

            if (!removed || !expected.TryFinishLease())
            {
                return false;
            }

            GC.SuppressFinalize(expected);
            Interlocked.Decrement(ref activeTrackedCount);
            return true;
        }

        private static string BuildBlockerDetail(string carrier, long epoch)
        {
            return "carrier=" +
                   (carrier ?? "<unknown>") +
                   ";epoch=" +
                   epoch.ToString(CultureInfo.InvariantCulture);
        }

        private static string DescribeFactory(MethodBase factory)
        {
            if (factory == null)
            {
                return "<unknown factory>";
            }

            return (factory.DeclaringType == null
                    ? "<unknown type>"
                    : factory.DeclaringType.FullName) +
                   "." +
                   factory.Name;
        }

        private static void SetLastDiagnostic(string diagnostic)
        {
            lock (Sync)
            {
                lastDiagnostic = diagnostic ?? string.Empty;
            }
        }
    }

    /// <summary>
    /// Task-4 patch health requires all four audited iterator factories and all four
    /// exact generated MoveNext methods. Registration is disabled unless the complete
    /// selective surface resolves, preventing a partial patch from creating leases
    /// that cannot be retired safely.
    /// </summary>
    internal static class SemanticCallbackCheckpointPatchHealth
    {
        private static readonly object Sync = new object();
        private static readonly System.Collections.Generic.HashSet<string> FactorySeams =
            new System.Collections.Generic.HashSet<string>();
        private static readonly System.Collections.Generic.HashSet<string> MoveNextSeams =
            new System.Collections.Generic.HashSet<string>();
        private static bool failureReported;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return !failureReported &&
                           FactorySeams.Count == 4 &&
                           MoveNextSeams.Count == 4;
                }
            }
        }

        internal static void ReportFactoryResolved(MethodBase method)
        {
            lock (Sync)
            {
                FactorySeams.Add(DescribeMethod(method));
            }
        }

        internal static void ReportMoveNextResolved(MethodBase method)
        {
            lock (Sync)
            {
                MoveNextSeams.Add(DescribeMethod(method));
            }
        }

        internal static void ReportFailure(string seam, string reason)
        {
            lock (Sync)
            {
                failureReported = true;
            }

            Debug.LogError(
                SaveNLoadFixesConstants.LogPrefix +
                "Semantic callback checkpoint repair failed to resolve " +
                (seam ?? "<unknown seam>") +
                ": " +
                (reason ?? string.Empty));
        }

        private static string DescribeMethod(MethodBase method)
        {
            if (method == null)
            {
                return "<unknown method>";
            }

            return (method.DeclaringType == null
                    ? "<unknown type>"
                    : method.DeclaringType.FullName) +
                   "." +
                   method.Name;
        }
    }
}
