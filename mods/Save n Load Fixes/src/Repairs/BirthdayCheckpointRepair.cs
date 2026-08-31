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
    /// Repair A10 / Area #2 A2-A10: closes the birthday-queue checkpoint gap that
    /// exists between one birthday popup closing and the next queued birthday being
    /// presented. The queue remains vanilla runtime state; SNLF does not serialize it.
    ///
    /// Repaired timelines hold one BirthdayQueueBetweenPopups lease while the queue
    /// is non-empty. The generated _MoveQueue iterator is bound to LoadEpoch so an
    /// iterator from a discarded F9 timeline cannot consume target-timeline objects.
    /// For pre-fix saves only, the already-saved current-age Graduation_History row
    /// is the conservative witness that a same-day birthday sweep had begun.
    /// </summary>
    internal static class BirthdayCheckpointRepair
    {
        private sealed class PendingMoveQueue
        {
            internal long Epoch;
        }

        private static readonly object Sync = new object();
        private static readonly ConditionalWeakTable<object, PendingMoveQueue> PendingIterators =
            new ConditionalWeakTable<object, PendingMoveQueue>();

        private static CheckpointBlockerLease queueLease;
        private static long activeTrackedIteratorCount;
        private static long registeredIteratorCount;
        private static long completedIteratorCount;
        private static long staleIteratorSuppressionCount;
        private static long staleQueueClearCount;
        private static long staleQueueClearedGirlCount;
        private static long legacyReconstructionCount;
        private static long legacyReconstructedGirlCount;
        private static long legacyNoWitnessSkipCount;
        private static string lastDiagnostic = string.Empty;
        private static int unhealthyBlockerWarningLogged;

        internal static bool IsImplemented
        {
            get { return BirthdayCheckpointPatchHealth.IsHealthy; }
        }

        internal static bool IsQueueBlockerActive
        {
            get
            {
                lock (Sync)
                {
                    return queueLease != null;
                }
            }
        }

        internal static long ActiveTrackedIteratorCount
        {
            get { return Interlocked.Read(ref activeTrackedIteratorCount); }
        }

        internal static long RegisteredIteratorCount
        {
            get { return Interlocked.Read(ref registeredIteratorCount); }
        }

        internal static long CompletedIteratorCount
        {
            get { return Interlocked.Read(ref completedIteratorCount); }
        }

        internal static long StaleIteratorSuppressionCount
        {
            get { return Interlocked.Read(ref staleIteratorSuppressionCount); }
        }

        internal static long StaleQueueClearCount
        {
            get { return Interlocked.Read(ref staleQueueClearCount); }
        }

        internal static long StaleQueueClearedGirlCount
        {
            get { return Interlocked.Read(ref staleQueueClearedGirlCount); }
        }

        internal static long LegacyReconstructionCount
        {
            get { return Interlocked.Read(ref legacyReconstructionCount); }
        }

        internal static long LegacyReconstructedGirlCount
        {
            get { return Interlocked.Read(ref legacyReconstructedGirlCount); }
        }

        internal static long LegacyNoWitnessSkipCount
        {
            get { return Interlocked.Read(ref legacyNoWitnessSkipCount); }
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
        /// Synchronizes the single queue-lifetime blocker with vanilla's authoritative
        /// Birthday.Queue. The source audit proves DoBirthday/Add and OnClose/Remove
        /// are the two vanilla mutation seams.
        /// </summary>
        internal static void SyncQueueBlocker(string source)
        {
            if (!BirthdayCheckpointPatchHealth.IsHealthy)
            {
                if (Interlocked.Exchange(ref unhealthyBlockerWarningLogged, 1) == 0)
                {
                    Debug.LogWarning(
                        SaveNLoadFixesConstants.LogPrefix +
                        "Birthday checkpoint repair is not healthy; birthday queue " +
                        "blockers will not be acquired.");
                }
                return;
            }

            int queueCount = GetQueueCount();
            CheckpointBlockerLease leaseToDispose = null;
            bool acquired = false;
            bool released = false;

            lock (Sync)
            {
                if (queueCount > 0 && queueLease == null)
                {
                    queueLease = CheckpointGate.Acquire(
                        CheckpointBlockerKind.BirthdayQueueBetweenPopups,
                        "source=" + (source ?? "<unknown>") +
                        ";count=" + queueCount.ToString(CultureInfo.InvariantCulture) +
                        ";epoch=" + LoadEpoch.Current.ToString(CultureInfo.InvariantCulture));
                    acquired = true;
                }
                else if (queueCount == 0 && queueLease != null)
                {
                    leaseToDispose = queueLease;
                    queueLease = null;
                    released = true;
                }

                if (acquired)
                {
                    lastDiagnostic =
                        "Birthday queue checkpoint blocker acquired for " +
                        queueCount.ToString(CultureInfo.InvariantCulture) +
                        " queued idol(s).";
                }
                else if (released)
                {
                    lastDiagnostic = "Birthday queue drained; checkpoint blocker released.";
                }
            }

            if (leaseToDispose != null)
            {
                leaseToDispose.Dispose();
            }
        }

        /// <summary>
        /// Runs at data_girls.LoadFunction entry after a successful target SavedData
        /// adoption but before target idols are rebuilt. Old static queue entries are
        /// CLR references from the discarded timeline and must never survive F9.
        /// </summary>
        internal static void ClearStaleQueueBeforeGirlRebuild()
        {
            int discarded = GetQueueCount();
            if (Birthday.Queue != null)
            {
                Birthday.Queue.Clear();
            }

            CheckpointBlockerLease leaseToDispose = null;
            lock (Sync)
            {
                if (queueLease != null)
                {
                    leaseToDispose = queueLease;
                    queueLease = null;
                }

                lastDiagnostic = discarded == 0
                    ? "Target load confirmed birthday queue empty before idol reconstruction."
                    : "Cleared " + discarded.ToString(CultureInfo.InvariantCulture) +
                      " stale birthday queue reference(s) before idol reconstruction.";
            }

            if (leaseToDispose != null)
            {
                leaseToDispose.Dispose();
            }

            Interlocked.Increment(ref staleQueueClearCount);
            if (discarded != 0)
            {
                Interlocked.Add(ref staleQueueClearedGirlCount, discarded);
            }
        }

        /// <summary>
        /// Registers the one audited delayed queue iterator. The iterator contains no
        /// girl reference and rereads Birthday.Queue[0], which is exactly why the epoch
        /// guard must prevent a discarded iterator from reaching a loaded target queue.
        /// </summary>
        internal static void RegisterMoveQueue(IEnumerator iterator)
        {
            if (iterator == null || !BirthdayCheckpointPatchHealth.IsHealthy)
            {
                return;
            }

            PendingMoveQueue pending = new PendingMoveQueue
            {
                Epoch = LoadEpoch.Capture()
            };

            bool duplicate = false;
            lock (Sync)
            {
                PendingMoveQueue existing;
                if (PendingIterators.TryGetValue(iterator, out existing))
                {
                    duplicate = true;
                }
                else
                {
                    PendingIterators.Add(iterator, pending);
                    lastDiagnostic =
                        "Registered Birthday._MoveQueue iterator at load epoch " +
                        pending.Epoch.ToString(CultureInfo.InvariantCulture) + ".";
                }
            }

            if (!duplicate)
            {
                Interlocked.Increment(ref activeTrackedIteratorCount);
                Interlocked.Increment(ref registeredIteratorCount);
            }
        }

        internal static bool ShouldRunMoveNext(object iterator)
        {
            PendingMoveQueue pending;
            if (!TryGetPendingIterator(iterator, out pending))
            {
                return true;
            }

            if (LoadEpoch.IsCurrent(pending.Epoch))
            {
                return true;
            }

            if (RemovePendingIterator(iterator, pending))
            {
                Interlocked.Increment(ref staleIteratorSuppressionCount);
                SetLastDiagnostic(
                    "Discarded stale Birthday._MoveQueue iterator from load epoch " +
                    pending.Epoch.ToString(CultureInfo.InvariantCulture) +
                    "; current epoch is " +
                    LoadEpoch.Current.ToString(CultureInfo.InvariantCulture) + ".");
            }

            return false;
        }

        internal static void MarkMoveNextCompleted(object iterator)
        {
            PendingMoveQueue pending;
            if (!TryGetPendingIterator(iterator, out pending))
            {
                return;
            }

            if (RemovePendingIterator(iterator, pending))
            {
                Interlocked.Increment(ref completedIteratorCount);
            }
        }

        internal static void MarkMoveNextFaulted(object iterator, Exception exception)
        {
            PendingMoveQueue pending;
            if (TryGetPendingIterator(iterator, out pending))
            {
                RemovePendingIterator(iterator, pending);
            }

            // Reflect authoritative queue state. If the queue still contains work the
            // blocker intentionally remains active; if external mutation emptied it,
            // this prevents an unnecessary permanent blocker.
            SyncQueueBlocker("Birthday._MoveQueue fault");

            string diagnostic =
                "Birthday._MoveQueue faulted; preserved original " +
                (exception == null ? "exception" : exception.GetType().Name) +
                " and synchronized the queue blocker with vanilla state.";
            SetLastDiagnostic(diagnostic);
            Debug.LogError(SaveNLoadFixesConstants.LogPrefix + diagnostic);
        }

        /// <summary>
        /// Conservative pre-fix compatibility path. It executes only when the caller
        /// proves that a successful career load advanced LoadEpoch. A current-age
        /// Graduation_History entry on at least one same-day idol is required as the
        /// witness that today's birthday sweep had already begun. Only same-day,
        /// still-eligible loaded idols lacking that witness are queued, in roster order.
        /// </summary>
        internal static void TryReconstructLegacyQueueAfterCareerLoad(
            SaveManager manager,
            long epochBeforeLoad)
        {
            if (!BirthdayCheckpointPatchHealth.IsHealthy ||
                manager == null ||
                manager.Data == null ||
                LoadEpoch.Current == epochBeforeLoad)
            {
                return;
            }

            DateTime targetDate;
            try
            {
                targetDate = ExtensionMethods.ToDateTime(manager.Data.staticVars__dateTime);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    SaveNLoadFixesConstants.LogPrefix +
                    "Birthday legacy reconstruction skipped because target save date " +
                    "could not be parsed: " + exception.Message);
                return;
            }

            if (data_girls.girl == null || data_girls.girl.Count == 0)
            {
                return;
            }

            List<data_girls.girls> pending = new List<data_girls.girls>();
            int witnessCount = 0;
            int sameDayEligibleCount = 0;

            foreach (data_girls.girls girl in data_girls.girl)
            {
                if (!IsEligibleSameDayGirl(girl, targetDate))
                {
                    continue;
                }

                sameDayEligibleCount++;
                int age = GetAgeAtDate(girl.birthday, targetDate);
                if (HasCurrentAgeHistoryWitness(girl, age))
                {
                    witnessCount++;
                }
                else
                {
                    pending.Add(girl);
                }
            }

            if (witnessCount == 0)
            {
                if (sameDayEligibleCount != 0)
                {
                    Interlocked.Increment(ref legacyNoWitnessSkipCount);
                    SetLastDiagnostic(
                        "Birthday legacy reconstruction found " +
                        sameDayEligibleCount.ToString(CultureInfo.InvariantCulture) +
                        " same-day eligible idol(s) but no current-age history witness; " +
                        "fabricated nothing.");
                }
                return;
            }

            if (pending.Count == 0)
            {
                SetLastDiagnostic(
                    "Birthday legacy witness found, but every same-day eligible idol " +
                    "already has the current-age history row; no queue reconstruction needed.");
                return;
            }

            // Load-prefix cleanup should already have emptied this static list. Clear
            // once more defensively so compatibility reconstruction never merges stale
            // object references with target-save objects.
            Birthday.Queue.Clear();
            Birthday.Queue.AddRange(pending);
            SyncQueueBlocker("legacy birthday reconstruction");

            Interlocked.Increment(ref legacyReconstructionCount);
            Interlocked.Add(ref legacyReconstructedGirlCount, pending.Count);
            SetLastDiagnostic(
                "Reconstructed " + pending.Count.ToString(CultureInfo.InvariantCulture) +
                " pending birthday(s) from " +
                witnessCount.ToString(CultureInfo.InvariantCulture) +
                " current-age history witness(es) on target date " +
                targetDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".");

            try
            {
                Birthday.MoveQueue();
            }
            catch (Exception exception)
            {
                // Compatibility recovery must not turn an otherwise loadable old save
                // into a hard load failure. Retract only the SNLF-fabricated queue and
                // its blocker, log the exact failure, and leave vanilla target state.
                Birthday.Queue.Clear();
                SyncQueueBlocker("legacy birthday reconstruction rollback");
                string diagnostic =
                    "Birthday legacy queue reconstruction could not start normal queue " +
                    "progression and was rolled back: " +
                    exception.GetType().Name + ": " + exception.Message;
                SetLastDiagnostic(diagnostic);
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + diagnostic);
            }
        }

        private static bool IsEligibleSameDayGirl(data_girls.girls girl, DateTime targetDate)
        {
            if (girl == null ||
                girl.status == data_girls._status.graduated ||
                girl.status == data_girls._status.announced_graduation)
            {
                return false;
            }

            return girl.birthday.Month == targetDate.Month &&
                   girl.birthday.Day == targetDate.Day;
        }

        private static int GetAgeAtDate(DateTime birthday, DateTime targetDate)
        {
            int targetNumber = targetDate.Year * 10000 + targetDate.Month * 100 + targetDate.Day;
            int birthdayNumber = birthday.Year * 10000 + birthday.Month * 100 + birthday.Day;
            return (targetNumber - birthdayNumber) / 10000;
        }

        private static bool HasCurrentAgeHistoryWitness(data_girls.girls girl, int age)
        {
            if (girl.Graduation_History == null)
            {
                return false;
            }

            foreach (data_girls.girls._grad_history row in girl.Graduation_History)
            {
                if (row != null && row.Age == age)
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetQueueCount()
        {
            return Birthday.Queue == null ? 0 : Birthday.Queue.Count;
        }

        private static bool TryGetPendingIterator(
            object iterator,
            out PendingMoveQueue pending)
        {
            pending = null;
            if (iterator == null)
            {
                return false;
            }

            lock (Sync)
            {
                return PendingIterators.TryGetValue(iterator, out pending);
            }
        }

        private static bool RemovePendingIterator(
            object iterator,
            PendingMoveQueue expected)
        {
            bool removed = false;
            lock (Sync)
            {
                PendingMoveQueue current;
                if (iterator != null &&
                    PendingIterators.TryGetValue(iterator, out current) &&
                    object.ReferenceEquals(current, expected))
                {
                    removed = PendingIterators.Remove(iterator);
                }
            }

            if (removed)
            {
                Interlocked.Decrement(ref activeTrackedIteratorCount);
            }

            return removed;
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
    /// Task-5 health requires every seam needed to acquire and retire a birthday
    /// queue blocker safely, clear stale target-load references, guard the one delayed
    /// iterator, and run compatibility reconstruction only after successful loads.
    /// </summary>
    internal static class BirthdayCheckpointPatchHealth
    {
        private static readonly object Sync = new object();
        private static bool doBirthdayResolved;
        private static bool closeCallbackResolved;
        private static bool iteratorFactoryResolved;
        private static bool moveNextResolved;
        private static bool dataGirlsLoadResolved;
        private static readonly HashSet<string> CareerLoads = new HashSet<string>();
        private static bool failureReported;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return !failureReported &&
                           doBirthdayResolved &&
                           closeCallbackResolved &&
                           iteratorFactoryResolved &&
                           moveNextResolved &&
                           dataGirlsLoadResolved &&
                           CareerLoads.Count == 2;
                }
            }
        }

        internal static void ReportDoBirthdayResolved()
        {
            lock (Sync)
            {
                doBirthdayResolved = true;
            }
        }

        internal static void ReportCloseCallbackResolved()
        {
            lock (Sync)
            {
                closeCallbackResolved = true;
            }
        }

        internal static void ReportIteratorFactoryResolved()
        {
            lock (Sync)
            {
                iteratorFactoryResolved = true;
            }
        }

        internal static void ReportMoveNextResolved()
        {
            lock (Sync)
            {
                moveNextResolved = true;
            }
        }

        internal static void ReportDataGirlsLoadResolved()
        {
            lock (Sync)
            {
                dataGirlsLoadResolved = true;
            }
        }

        internal static void ReportCareerLoadResolved(MethodBase method)
        {
            lock (Sync)
            {
                CareerLoads.Add(DescribeMethod(method));
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
                "Birthday checkpoint repair failed to resolve " +
                (seam ?? "<unknown seam>") + ": " +
                (reason ?? string.Empty));
        }

        private static string DescribeMethod(MethodBase method)
        {
            if (method == null)
            {
                return "<unknown method>";
            }

            ParameterInfo[] parameters = method.GetParameters();
            string signature = string.Empty;
            for (int index = 0; index < parameters.Length; index++)
            {
                if (index != 0)
                {
                    signature += ",";
                }
                signature += parameters[index].ParameterType.FullName;
            }

            return (method.DeclaringType == null
                    ? "<unknown type>"
                    : method.DeclaringType.FullName) +
                   "." + method.Name + "(" + signature + ")";
        }
    }
}
