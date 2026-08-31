using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using SaveNLoadFixes.Safety;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repair A08: finite same-process F9 stale-carrier invalidation.
    ///
    /// This class deliberately carries no continuation payload. It binds only the
    /// remaining seven source-audited authoritative iterator families to the
    /// process-local LoadEpoch. If a successful target load advances the epoch, an
    /// old iterator becomes inert before its next MoveNext can commit gameplay state.
    /// The same exact guard can also revoke one iterator explicitly when a repair
    /// transaction has created future work that must be rolled back fail-closed.
    ///
    /// Event_Templates, semantic callback tails, Birthday._MoveQueue and the SSK
    /// start coroutine already consume LoadEpoch in their family-specific repairs.
    /// This task closes the rest of the audit's finite carrier manifest without
    /// using blanket coroutine cancellation and without touching recurring infrastructure.
    /// </summary>
    internal static class DeferredCarrierEpochRepair
    {
        private sealed class PendingCarrier
        {
            internal long Epoch;
            internal string Carrier;
            private int terminal;
            private int explicitlyCancelled;

            internal bool IsExplicitlyCancelled
            {
                get { return Volatile.Read(ref this.explicitlyCancelled) != 0; }
            }

            internal bool TryCancel()
            {
                return Interlocked.Exchange(ref this.explicitlyCancelled, 1) == 0;
            }

            internal bool TryFinish()
            {
                return Interlocked.Exchange(ref this.terminal, 1) == 0;
            }

            ~PendingCarrier()
            {
                if (this.TryFinish())
                {
                    Interlocked.Decrement(ref activeTrackedCount);
                    Interlocked.Increment(ref garbageCollectedCleanupCount);
                }
            }
        }

        private static readonly object Sync = new object();
        private static readonly ConditionalWeakTable<object, PendingCarrier> PendingByIterator =
            new ConditionalWeakTable<object, PendingCarrier>();

        private static long activeTrackedCount;
        private static long registeredCount;
        private static long completedCount;
        private static long staleSuppressionCount;
        private static long explicitCancellationSuppressionCount;
        private static long faultCleanupCount;
        private static long garbageCollectedCleanupCount;
        private static long substoryMutexResetCount;
        private static string lastDiagnostic = string.Empty;
        private static int unhealthyRegistrationWarningLogged;

        internal static bool IsImplemented
        {
            get { return DeferredCarrierEpochPatchHealth.IsHealthy; }
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

        internal static long ExplicitCancellationSuppressionCount
        {
            get { return Interlocked.Read(ref explicitCancellationSuppressionCount); }
        }

        internal static long FaultCleanupCount
        {
            get { return Interlocked.Read(ref faultCleanupCount); }
        }

        internal static long GarbageCollectedCleanupCount
        {
            get { return Interlocked.Read(ref garbageCollectedCleanupCount); }
        }

        internal static long SubstoryMutexResetCount
        {
            get { return Interlocked.Read(ref substoryMutexResetCount); }
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

        internal static void Register(IEnumerator iterator, MethodBase factory)
        {
            if (iterator == null || factory == null)
            {
                return;
            }

            // A partial manifest is less safe than vanilla behavior because it could
            // make diagnostics claim A08 is closed while one authoritative carrier
            // remains live across F9. Do not register until all exact seams resolved.
            if (!DeferredCarrierEpochPatchHealth.IsHealthy)
            {
                if (Interlocked.Exchange(ref unhealthyRegistrationWarningLogged, 1) == 0)
                {
                    Debug.LogWarning(
                        SaveNLoadFixesConstants.LogPrefix +
                        "A08 deferred-carrier epoch repair is not healthy; remaining " +
                        "carrier registrations will be skipped.");
                }
                return;
            }

            string carrier = DescribeFactory(factory);
            long epoch = LoadEpoch.Capture();
            PendingCarrier pending = new PendingCarrier
            {
                Epoch = epoch,
                Carrier = carrier
            };

            bool duplicate = false;
            lock (Sync)
            {
                PendingCarrier existing;
                if (PendingByIterator.TryGetValue(iterator, out existing))
                {
                    duplicate = true;
                }
                else
                {
                    PendingByIterator.Add(iterator, pending);
                    lastDiagnostic =
                        "Registered deferred carrier '" + carrier +
                        "' at load epoch " +
                        epoch.ToString(CultureInfo.InvariantCulture) + ".";
                }
            }

            if (duplicate)
            {
                pending.TryFinish();
                GC.SuppressFinalize(pending);
                return;
            }

            Interlocked.Increment(ref activeTrackedCount);
            Interlocked.Increment(ref registeredCount);
        }

        /// <summary>
        /// Shared prefix guard for exactly the seven remaining generated MoveNext
        /// methods. Returning false terminates only the stale audited carrier.
        /// </summary>
        internal static bool ShouldRunMoveNext(object iterator)
        {
            PendingCarrier pending;
            if (!TryGetPending(iterator, out pending))
            {
                return true;
            }

            if (pending.IsExplicitlyCancelled)
            {
                if (FinishAndRemove(iterator, pending))
                {
                    Interlocked.Increment(ref explicitCancellationSuppressionCount);
                    string diagnostic =
                        "Suppressed explicitly cancelled deferred carrier '" + pending.Carrier +
                        "' at load epoch " +
                        pending.Epoch.ToString(CultureInfo.InvariantCulture) + ".";
                    SetLastDiagnostic(diagnostic);
                    Debug.Log(SaveNLoadFixesConstants.LogPrefix + diagnostic);
                }
                return false;
            }

            if (LoadEpoch.IsCurrent(pending.Epoch))
            {
                return true;
            }

            if (FinishAndRemove(iterator, pending))
            {
                Interlocked.Increment(ref staleSuppressionCount);
                string diagnostic =
                    "Discarded stale deferred carrier '" + pending.Carrier +
                    "' from load epoch " +
                    pending.Epoch.ToString(CultureInfo.InvariantCulture) +
                    "; current epoch is " +
                    LoadEpoch.Current.ToString(CultureInfo.InvariantCulture) + ".";
                SetLastDiagnostic(diagnostic);
                Debug.Log(SaveNLoadFixesConstants.LogPrefix + diagnostic);
            }

            return false;
        }


        /// <summary>
        /// Marks one already-registered audited carrier as explicitly cancelled while
        /// preserving A08 as the sole MoveNext suppression owner. The weak-table row
        /// intentionally remains until the iterator next runs or is collected, so a
        /// failed StopCoroutine cannot turn cancellation into accidental permission.
        /// </summary>
        internal static bool CancelCarrier(object iterator, string reason)
        {
            PendingCarrier pending;
            if (!TryGetPending(iterator, out pending) || !pending.TryCancel())
            {
                return false;
            }

            SetLastDiagnostic(
                (reason ?? "Explicitly cancelled deferred carrier.") +
                " Carrier '" + pending.Carrier + "' remains guarded until termination.");
            return true;
        }

        internal static void ObserveMoveNextReturn(object iterator, bool result)
        {
            if (result)
            {
                return;
            }

            PendingCarrier pending;
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
                "Deferred carrier '" + pending.Carrier +
                "' completed in its original load epoch.");
        }

        internal static void ObserveMoveNextFault(object iterator, Exception exception)
        {
            PendingCarrier pending;
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
                "Deferred carrier '" + pending.Carrier +
                "' faulted and was removed from A08 epoch tracking: " +
                (exception == null ? "<unknown exception>" : exception.GetType().FullName) + ".";
            SetLastDiagnostic(diagnostic);
        }

        /// <summary>
        /// Substories_Manager.checkingQueue is only a runtime coroutine mutex. Vanilla
        /// LoadFunction rebuilds the authoritative queue but Reset() does not clear this
        /// flag. If an old _CheckDialogueQueue had already set it true, suppressing that
        /// iterator after F9 would otherwise strand the target queue permanently.
        /// </summary>
        internal static void ResetSubstoryQueueMutex(ref bool checkingQueue)
        {
            if (checkingQueue)
            {
                Interlocked.Increment(ref substoryMutexResetCount);
                SetLastDiagnostic(
                    "Cleared stale Substories_Manager.checkingQueue mutex for target load.");
            }

            checkingQueue = false;
        }

        private static bool TryGetPending(object iterator, out PendingCarrier pending)
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

        private static bool FinishAndRemove(object iterator, PendingCarrier expected)
        {
            if (iterator == null || expected == null)
            {
                return false;
            }

            bool removed = false;
            lock (Sync)
            {
                PendingCarrier current;
                if (PendingByIterator.TryGetValue(iterator, out current) &&
                    object.ReferenceEquals(current, expected) &&
                    current.TryFinish())
                {
                    PendingByIterator.Remove(iterator);
                    removed = true;
                }
            }

            if (!removed)
            {
                return false;
            }

            GC.SuppressFinalize(expected);
            Interlocked.Decrement(ref activeTrackedCount);
            return true;
        }

        private static string DescribeFactory(MethodBase factory)
        {
            string owner = factory.DeclaringType == null
                ? "<unknown>"
                : factory.DeclaringType.FullName;
            return owner + "." + factory.Name;
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
    /// Exact-manifest health for A08's seven remaining iterator factories, seven
    /// generated MoveNext carriers, and the Substories queue-mutex reset seam.
    /// </summary>
    internal static class DeferredCarrierEpochPatchHealth
    {
        internal const int ExpectedFactoryCount = 7;
        internal const int ExpectedMoveNextCount = 7;

        private static readonly object Sync = new object();
        private static readonly HashSet<string> ResolvedFactories = new HashSet<string>();
        private static readonly HashSet<string> ResolvedMoveNext = new HashSet<string>();
        private static bool substoryMutexResetResolved;
        private static bool failureReported;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return !failureReported &&
                           ResolvedFactories.Count == ExpectedFactoryCount &&
                           ResolvedMoveNext.Count == ExpectedMoveNextCount &&
                           substoryMutexResetResolved;
                }
            }
        }

        internal static void ReportFactoryResolved(MethodBase method)
        {
            lock (Sync)
            {
                ResolvedFactories.Add(Describe(method));
            }
        }

        internal static void ReportMoveNextResolved(MethodBase method)
        {
            lock (Sync)
            {
                ResolvedMoveNext.Add(Describe(method));
            }
        }

        internal static void ReportSubstoryMutexResetResolved()
        {
            lock (Sync)
            {
                substoryMutexResetResolved = true;
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
                "A08 deferred-carrier patch failed at " +
                (seam ?? "<unknown>") + ": " +
                (reason ?? string.Empty));
        }

        private static string Describe(MethodBase method)
        {
            if (method == null)
            {
                return "<null>";
            }

            string owner = method.DeclaringType == null
                ? "<unknown>"
                : method.DeclaringType.FullName;
            return owner + "." + method.Name;
        }
    }
}
