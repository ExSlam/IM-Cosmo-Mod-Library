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
    /// Repair A12 / Area #2 A2-A12: closes the paid-but-not-yet-generated SSK
    /// checkpoint interval without inventing a persisted SSK phase.
    ///
    /// Vanilla deducts production cost in StartSSK(), then schedules a two-second
    /// StartSSK_Coroutine that later commits cooldown/status/results and asks the
    /// SSK result popup to open. SNLF acquires one SskPostPaymentLaunch lease when
    /// that iterator is created, binds it to the current LoadEpoch, and releases it
    /// only after the iterator has completed its authoritative mutations and the
    /// SSK result popup has actually acquired vanilla popup protection.
    /// </summary>
    internal static class SskCheckpointRepair
    {
        private sealed class PendingLaunch
        {
            internal CheckpointBlockerLease Lease;
            internal long Epoch;
            internal int SskId;
            internal bool PopupOpenedDuringMoveNext;
            internal bool AwaitingQueuedPopup;
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

            internal bool IsTerminal
            {
                get { return Interlocked.CompareExchange(ref this.terminal, 0, 0) != 0; }
            }

            ~PendingLaunch()
            {
                if (this.TryFinishLease())
                {
                    Interlocked.Decrement(ref activeLaunchCount);
                    Interlocked.Increment(ref garbageCollectedCleanupCount);
                }
            }
        }

        private static readonly object Sync = new object();
        private static readonly ConditionalWeakTable<object, PendingLaunch> PendingByIterator =
            new ConditionalWeakTable<object, PendingLaunch>();
        private static readonly Queue<PendingLaunch> AwaitingPopup =
            new Queue<PendingLaunch>();

        [ThreadStatic]
        private static PendingLaunch currentMoveNextLaunch;

        private static long activeLaunchCount;
        private static long registeredCount;
        private static long completedIteratorCount;
        private static long synchronousPopupHandoffCount;
        private static long queuedPopupHandoffCount;
        private static long staleSuppressionCount;
        private static long faultCleanupCount;
        private static long garbageCollectedCleanupCount;
        private static string lastDiagnostic = string.Empty;
        private static int unhealthyRegistrationWarningLogged;

        internal static bool IsImplemented
        {
            get { return SskCheckpointPatchHealth.IsHealthy; }
        }

        internal static long ActiveLaunchCount
        {
            get { return Interlocked.Read(ref activeLaunchCount); }
        }

        internal static long RegisteredCount
        {
            get { return Interlocked.Read(ref registeredCount); }
        }

        internal static long CompletedIteratorCount
        {
            get { return Interlocked.Read(ref completedIteratorCount); }
        }

        internal static long SynchronousPopupHandoffCount
        {
            get { return Interlocked.Read(ref synchronousPopupHandoffCount); }
        }

        internal static long QueuedPopupHandoffCount
        {
            get { return Interlocked.Read(ref queuedPopupHandoffCount); }
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

        /// <summary>
        /// Called from the private StartSSK_Coroutine() factory. This seam is after
        /// vanilla's production-cost debit but still synchronous inside StartSSK(),
        /// before control can return to the frame loop.
        /// </summary>
        internal static void Register(IEnumerator iterator, SEvent_SSK owner)
        {
            if (iterator == null)
            {
                return;
            }

            // Fail open to vanilla behavior if the complete selective patch surface
            // is not known. Never acquire a blocker whose terminal popup handoff is
            // not guaranteed to be observable.
            if (!SskCheckpointPatchHealth.IsHealthy)
            {
                if (Interlocked.Exchange(ref unhealthyRegistrationWarningLogged, 1) == 0)
                {
                    Debug.LogWarning(
                        SaveNLoadFixesConstants.LogPrefix +
                        "SSK checkpoint repair is not healthy; post-payment blockers " +
                        "will not be acquired.");
                }
                return;
            }

            long epoch = LoadEpoch.Capture();
            int sskId = TryGetSskId(owner);
            PendingLaunch pending = new PendingLaunch
            {
                Epoch = epoch,
                SskId = sskId,
                Lease = CheckpointGate.Acquire(
                    CheckpointBlockerKind.SskPostPaymentLaunch,
                    BuildBlockerDetail(sskId, epoch))
            };

            bool duplicate = false;
            lock (Sync)
            {
                PendingLaunch existing;
                if (PendingByIterator.TryGetValue(iterator, out existing))
                {
                    duplicate = true;
                }
                else
                {
                    PendingByIterator.Add(iterator, pending);
                    lastDiagnostic =
                        "Registered SSK post-payment launch for SSK " +
                        sskId.ToString(CultureInfo.InvariantCulture) +
                        " at load epoch " +
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
                    "Duplicate SSK start iterator registration was ignored.");
                return;
            }

            Interlocked.Increment(ref activeLaunchCount);
            Interlocked.Increment(ref registeredCount);
        }

        /// <summary>
        /// Prefix guard for only SEvent_SSK.&lt;StartSSK_Coroutine&gt;d__28.MoveNext.
        /// A stale discarded-timeline iterator is terminated before it can apply
        /// cooldown/status/results to the target timeline.
        /// </summary>
        internal static bool PrepareMoveNext(object iterator)
        {
            PendingLaunch pending;
            if (!TryGetPending(iterator, out pending))
            {
                currentMoveNextLaunch = null;
                return true;
            }

            if (!LoadEpoch.IsCurrent(pending.Epoch))
            {
                currentMoveNextLaunch = null;
                if (FinishAndRemoveIterator(iterator, pending))
                {
                    Interlocked.Increment(ref staleSuppressionCount);
                    string diagnostic =
                        "Discarded stale SSK start coroutine for SSK " +
                        pending.SskId.ToString(CultureInfo.InvariantCulture) +
                        " from load epoch " +
                        pending.Epoch.ToString(CultureInfo.InvariantCulture) +
                        "; current epoch is " +
                        LoadEpoch.Current.ToString(CultureInfo.InvariantCulture) +
                        ".";
                    SetLastDiagnostic(diagnostic);
                    Debug.Log(SaveNLoadFixesConstants.LogPrefix + diagnostic);
                }
                return false;
            }

            currentMoveNextLaunch = pending;
            return true;
        }

        /// <summary>
        /// Postfix for the generated MoveNext. The first true return is only the
        /// WaitForSeconds(2f) yield. A false return means cooldown/status/results,
        /// PopupManager.Open, SSKPopup.Reset and SSKPopup.StartSSK have all returned.
        /// </summary>
        internal static void ObserveMoveNextReturn(object iterator, bool result)
        {
            PendingLaunch pending;
            if (!TryGetPending(iterator, out pending))
            {
                currentMoveNextLaunch = null;
                return;
            }

            currentMoveNextLaunch = null;
            if (result)
            {
                return;
            }

            Interlocked.Increment(ref completedIteratorCount);

            if (pending.PopupOpenedDuringMoveNext)
            {
                if (FinishAndRemoveIterator(iterator, pending))
                {
                    Interlocked.Increment(ref synchronousPopupHandoffCount);
                    SetLastDiagnostic(
                        "SSK " + pending.SskId.ToString(CultureInfo.InvariantCulture) +
                        " completed result generation and handed checkpoint safety " +
                        "to the synchronously opened SSK result popup.");
                }
                return;
            }

            // PopupManager.Open can enqueue instead of opening when another popup or
            // dialogue owns presentation. Detach the finished iterator but keep the
            // lease until PopupManager._popup.Open actually opens the queued SSK popup.
            if (DetachIteratorForQueuedPopup(iterator, pending))
            {
                lock (Sync)
                {
                    pending.AwaitingQueuedPopup = true;
                    AwaitingPopup.Enqueue(pending);
                    lastDiagnostic =
                        "SSK " + pending.SskId.ToString(CultureInfo.InvariantCulture) +
                        " finished gameplay-state commit but its result popup is queued; " +
                        "checkpoint blocker remains active.";
                }
            }
        }

        /// <summary>
        /// Postfix on PopupManager._popup.Open. SetActive(true) synchronously invokes
        /// Popup.OnEnable/Show, which increments PopupCounter for the blocking SSK
        /// result popup before this postfix runs.
        /// </summary>
        internal static void MarkSskPopupActuallyOpened(PopupManager._popup popup)
        {
            if (popup == null || popup.type != PopupManager._type.sevent_SSK)
            {
                return;
            }

            if (PopupManager.PopupCounter <= 0)
            {
                string diagnostic =
                    "SSK result popup opened without PopupCounter protection; SNLF " +
                    "retains the post-payment checkpoint blocker.";
                SetLastDiagnostic(diagnostic);
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + diagnostic);
                return;
            }

            PendingLaunch inFlight = currentMoveNextLaunch;
            if (inFlight != null && !inFlight.IsTerminal)
            {
                // The popup opened inside this launch's MoveNext. Do not release yet:
                // SSKPopup.Reset/StartSSK still execute after PopupManager.Open. The
                // MoveNext postfix will release only after those calls return.
                inFlight.PopupOpenedDuringMoveNext = true;
                return;
            }

            PendingLaunch queued = null;
            lock (Sync)
            {
                while (AwaitingPopup.Count != 0)
                {
                    PendingLaunch candidate = AwaitingPopup.Dequeue();
                    if (candidate == null || candidate.IsTerminal)
                    {
                        continue;
                    }

                    candidate.AwaitingQueuedPopup = false;
                    queued = candidate;
                    break;
                }
            }

            if (queued == null)
            {
                return;
            }

            if (FinishDetachedPending(queued))
            {
                Interlocked.Increment(ref queuedPopupHandoffCount);
                SetLastDiagnostic(
                    "Queued SSK result popup opened for SSK " +
                    queued.SskId.ToString(CultureInfo.InvariantCulture) +
                    "; checkpoint safety handed to vanilla PopupCounter.");
            }
        }

        /// <summary>
        /// Exception cleanup for the generated iterator. The original exception is
        /// returned unchanged by the Harmony finalizer; this method only prevents a
        /// permanent checkpoint blocker after a failed coroutine body.
        /// </summary>
        internal static void ObserveMoveNextFault(
            object iterator,
            Exception exception)
        {
            currentMoveNextLaunch = null;

            PendingLaunch pending;
            if (!TryGetPending(iterator, out pending))
            {
                return;
            }

            if (!FinishAndRemoveIterator(iterator, pending))
            {
                return;
            }

            Interlocked.Increment(ref faultCleanupCount);
            string diagnostic =
                "SSK start coroutine for SSK " +
                pending.SskId.ToString(CultureInfo.InvariantCulture) +
                " faulted; checkpoint blocker released without swallowing " +
                (exception == null ? "the original exception" : exception.GetType().Name) +
                ".";
            SetLastDiagnostic(diagnostic);
            Debug.LogError(SaveNLoadFixesConstants.LogPrefix + diagnostic);
        }

        private static bool TryGetPending(object iterator, out PendingLaunch pending)
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

        private static bool DetachIteratorForQueuedPopup(
            object iterator,
            PendingLaunch expected)
        {
            lock (Sync)
            {
                PendingLaunch current;
                if (iterator == null ||
                    !PendingByIterator.TryGetValue(iterator, out current) ||
                    !object.ReferenceEquals(current, expected))
                {
                    return false;
                }

                return PendingByIterator.Remove(iterator);
            }
        }

        private static bool FinishAndRemoveIterator(
            object iterator,
            PendingLaunch expected)
        {
            bool removed = false;
            lock (Sync)
            {
                PendingLaunch current;
                if (iterator != null &&
                    PendingByIterator.TryGetValue(iterator, out current) &&
                    object.ReferenceEquals(current, expected))
                {
                    removed = PendingByIterator.Remove(iterator);
                }
            }

            if (!removed)
            {
                return false;
            }

            return FinishDetachedPending(expected);
        }

        private static bool FinishDetachedPending(PendingLaunch pending)
        {
            if (pending == null || !pending.TryFinishLease())
            {
                return false;
            }

            GC.SuppressFinalize(pending);
            Interlocked.Decrement(ref activeLaunchCount);
            return true;
        }

        private static int TryGetSskId(SEvent_SSK owner)
        {
            try
            {
                return owner == null || owner.SSK == null ? -1 : owner.SSK.ID;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    SaveNLoadFixesConstants.LogPrefix +
                    "Could not read SSK identity while registering post-payment blocker. " +
                    exception.GetType().Name +
                    ": " +
                    exception.Message);
                return -1;
            }
        }

        private static string BuildBlockerDetail(int sskId, long epoch)
        {
            return "ssk=" +
                   sskId.ToString(CultureInfo.InvariantCulture) +
                   ";epoch=" +
                   epoch.ToString(CultureInfo.InvariantCulture);
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
    /// Health for the exact Task-6 selective surface: private SSK start-coroutine
    /// factory, its generated MoveNext, and PopupManager._popup.Open. Blockers are
    /// never acquired unless all three seams resolve.
    /// </summary>
    internal static class SskCheckpointPatchHealth
    {
        private static readonly object Sync = new object();
        private static bool factoryResolved;
        private static bool moveNextResolved;
        private static bool popupOpenResolved;
        private static bool failureReported;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return factoryResolved &&
                           moveNextResolved &&
                           popupOpenResolved &&
                           !failureReported;
                }
            }
        }

        internal static void ReportFactoryResolved()
        {
            lock (Sync)
            {
                factoryResolved = true;
            }
        }

        internal static void ReportMoveNextResolved()
        {
            lock (Sync)
            {
                moveNextResolved = true;
            }
        }

        internal static void ReportPopupOpenResolved()
        {
            lock (Sync)
            {
                popupOpenResolved = true;
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
                "SSK checkpoint repair failed to resolve " +
                (seam ?? "<unknown seam>") +
                ": " +
                (reason ?? string.Empty));
        }
    }
}
