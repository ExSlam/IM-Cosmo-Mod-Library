using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using SaveNLoadFixes.Persistence;
using SaveNLoadFixes.Safety;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A13 / regression 33: preserve temporary agency auto-task bans.
    ///
    /// Vanilla BanGirlFromAutoJobs() inserts one idol object into the static
    /// GirlsBannedFromAutoTasks list and starts ReturnGirl(), whose sole delay is
    /// WaitForSeconds(60f). CanAutoTrain() directly consumes membership in that list.
    /// The list and remaining delay are otherwise absent from SavedData.
    ///
    /// This repair tracks the exact vanilla ReturnGirl iterator, measures remaining
    /// delay in Unity's scaled Time.time domain, persists stable idol ID + remaining
    /// scaled seconds, clears discarded CLR references before target idol rebuild,
    /// and asks vanilla BanGirlFromAutoJobs() to recreate exactly one expiry carrier.
    /// A08 remains the sole bool MoveNext suppression owner for stale/rolled-back
    /// ReturnGirl iterators. Duplicate vanilla ban calls still hit their original
    /// Contains() early-return and therefore never extend the tracked expiry.
    /// </summary>
    internal static class TemporaryAutoTaskBanRepair
    {
        internal const int SectionVersion = 1;
        internal const float VanillaDelaySeconds = 60f;

        private sealed class ActiveBan
        {
            internal long Token;
            internal long Epoch;
            internal int GirlId;
            internal data_girls.girls Girl;
            internal object Iterator;
            internal float ConfiguredDelaySeconds;
            internal bool WaitStarted;
            internal float DueScaledTime;
            internal bool IsAuthoritative;
        }

        private sealed class RestoreClaim
        {
            internal data_girls.girls Girl;
            internal float DelaySeconds;
            internal bool Claimed;
            internal ActiveBan Ban;
        }

        private static readonly object Sync = new object();
        private static readonly ConditionalWeakTable<object, ActiveBan> ByIterator =
            new ConditionalWeakTable<object, ActiveBan>();
        private static readonly Dictionary<int, ActiveBan> ActiveByGirlId =
            new Dictionary<int, ActiveBan>();

        [ThreadStatic]
        private static RestoreClaim restoreClaim;

        private static long nextToken;
        private static long registeredCount;
        private static long duplicateFactoryCount;
        private static long completedExpiryCount;
        private static long faultRetiredCount;
        private static long capturedCheckpointCount;
        private static long capturedBanCount;
        private static long captureFailureCount;
        private static long staleMembershipClearCount;
        private static long staleRegistryClearCount;
        private static long restoredLoadCount;
        private static long restoredBanCount;
        private static long immediateExpiryCount;
        private static long legacySectionAbsentCount;
        private static long invalidSectionCount;
        private static long associationMissingCount;
        private static long runtimeResolutionFailureCount;
        private static long schedulingFailureCount;
        private static long rollbackCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get
            {
                return TemporaryAutoTaskBanPatchHealth.IsHealthy &&
                    DeferredCarrierEpochRepair.IsImplemented &&
                    ScaledTimeClock.IsImplemented;
            }
        }

        internal static long RegisteredCount { get { return Interlocked.Read(ref registeredCount); } }
        internal static long DuplicateFactoryCount { get { return Interlocked.Read(ref duplicateFactoryCount); } }
        internal static long CompletedExpiryCount { get { return Interlocked.Read(ref completedExpiryCount); } }
        internal static long FaultRetiredCount { get { return Interlocked.Read(ref faultRetiredCount); } }
        internal static long CapturedCheckpointCount { get { return Interlocked.Read(ref capturedCheckpointCount); } }
        internal static long CapturedBanCount { get { return Interlocked.Read(ref capturedBanCount); } }
        internal static long CaptureFailureCount { get { return Interlocked.Read(ref captureFailureCount); } }
        internal static long StaleMembershipClearCount { get { return Interlocked.Read(ref staleMembershipClearCount); } }
        internal static long StaleRegistryClearCount { get { return Interlocked.Read(ref staleRegistryClearCount); } }
        internal static long RestoredLoadCount { get { return Interlocked.Read(ref restoredLoadCount); } }
        internal static long RestoredBanCount { get { return Interlocked.Read(ref restoredBanCount); } }
        internal static long ImmediateExpiryCount { get { return Interlocked.Read(ref immediateExpiryCount); } }
        internal static long LegacySectionAbsentCount { get { return Interlocked.Read(ref legacySectionAbsentCount); } }
        internal static long InvalidSectionCount { get { return Interlocked.Read(ref invalidSectionCount); } }
        internal static long AssociationMissingCount { get { return Interlocked.Read(ref associationMissingCount); } }
        internal static long RuntimeResolutionFailureCount { get { return Interlocked.Read(ref runtimeResolutionFailureCount); } }
        internal static long SchedulingFailureCount { get { return Interlocked.Read(ref schedulingFailureCount); } }
        internal static long RollbackCount { get { return Interlocked.Read(ref rollbackCount); } }

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
        /// Called from the exact private agency.ReturnGirl(girl) factory after vanilla
        /// has returned its generated iterator. The public duplicate-ban early return
        /// occurs before this factory, so normal duplicate calls cannot refresh expiry.
        /// </summary>
        internal static void RegisterIterator(IEnumerator iterator, data_girls.girls girl)
        {
            if (iterator == null || girl == null || girl.id < 0 || !IsImplemented)
            {
                return;
            }
            if (agency.GirlsBannedFromAutoTasks == null ||
                !agency.GirlsBannedFromAutoTasks.Contains(girl))
            {
                return;
            }

            RestoreClaim claim = restoreClaim;
            bool isRestore = claim != null && !claim.Claimed && ReferenceEquals(claim.Girl, girl);
            float configuredDelay = isRestore ? claim.DelaySeconds : VanillaDelaySeconds;
            configuredDelay = ClampDelay(configuredDelay);

            ActiveBan ban = new ActiveBan
            {
                Token = Interlocked.Increment(ref nextToken),
                Epoch = LoadEpoch.Capture(),
                GirlId = girl.id,
                Girl = girl,
                Iterator = iterator,
                ConfiguredDelaySeconds = configuredDelay,
                IsAuthoritative = true
            };

            bool duplicate = false;
            lock (Sync)
            {
                ActiveBan existing;
                if (ActiveByGirlId.TryGetValue(girl.id, out existing) &&
                    existing != null && LoadEpoch.IsCurrent(existing.Epoch))
                {
                    // This should not occur through vanilla BanGirlFromAutoJobs(), whose
                    // Contains() early-return prevents a second factory call. If a mod
                    // invokes _ReturnGirl directly, never let the extra carrier extend
                    // the authoritative expiry: give it at most the existing remainder
                    // and keep the original lease as the envelope authority.
                    float existingRemaining = GetRemainingSeconds(existing, ScaledTimeClock.Now);
                    ban.ConfiguredDelaySeconds = Math.Min(ban.ConfiguredDelaySeconds, existingRemaining);
                    ban.IsAuthoritative = false;
                    duplicate = true;
                }
                else
                {
                    ActiveByGirlId[girl.id] = ban;
                }

                try
                {
                    ByIterator.Add(iterator, ban);
                }
                catch (ArgumentException)
                {
                    ban.IsAuthoritative = false;
                    duplicate = true;
                }

                lastDiagnostic =
                    "A13 registered auto-task-ban expiry carrier for idol " +
                    girl.id.ToString(CultureInfo.InvariantCulture) +
                    " with " + configuredDelay.ToString("R", CultureInfo.InvariantCulture) +
                    " scaled second(s) at load epoch " +
                    ban.Epoch.ToString(CultureInfo.InvariantCulture) + ".";
            }

            if (isRestore)
            {
                claim.Claimed = true;
                claim.Ban = ban;
            }

            Interlocked.Increment(ref registeredCount);
            if (duplicate)
            {
                Interlocked.Increment(ref duplicateFactoryCount);
            }
        }

        /// <summary>
        /// Called by the A13 transpiler at the single audited WaitForSeconds(60f)
        /// construction. It changes only the delay value for a restored iterator;
        /// A08's prefix remains the sole execution/suppression decision.
        /// </summary>
        internal static float ResolveWaitSeconds(object iterator, float vanillaSeconds)
        {
            ActiveBan ban;
            if (iterator == null || !ByIterator.TryGetValue(iterator, out ban) || ban == null)
            {
                return vanillaSeconds;
            }

            lock (Sync)
            {
                if (!ban.WaitStarted)
                {
                    ban.WaitStarted = true;
                    ban.DueScaledTime = ScaledTimeClock.Now + ClampDelay(ban.ConfiguredDelaySeconds);
                }
                return ClampDelay(ban.ConfiguredDelaySeconds);
            }
        }

        internal static void ObserveExpiryMoveNextReturn(object iterator, bool result)
        {
            if (result)
            {
                return;
            }

            ActiveBan ban;
            if (!TryRetireIterator(iterator, out ban))
            {
                return;
            }

            Interlocked.Increment(ref completedExpiryCount);
            SetDiagnostic(
                "A13 retired completed auto-task-ban expiry for idol " +
                ban.GirlId.ToString(CultureInfo.InvariantCulture) + ".");
        }

        internal static void ObserveExpiryFault(object iterator, Exception exception)
        {
            ActiveBan ban;
            if (!TryRetireIterator(iterator, out ban))
            {
                return;
            }

            Interlocked.Increment(ref faultRetiredCount);
            string diagnostic =
                "A13 auto-task-ban expiry carrier faulted for idol " +
                ban.GirlId.ToString(CultureInfo.InvariantCulture) + ": " +
                (exception == null ? "unknown exception" : exception.Message);
            SetDiagnostic(diagnostic);
            Debug.LogError(SaveNLoadFixesConstants.LogPrefix + diagnostic);
        }

        /// <summary>
        /// Caller-thread envelope freeze. Current-format empty is authoritative.
        /// Every live membership must map to one current tracked expiry and one target
        /// SavedData idol ID, otherwise the checkpoint is not falsely declared exact.
        /// </summary>
        internal static bool TryCaptureForEnvelope(
            SaveManager.SavedData dataToSave,
            out List<TemporaryAutoTaskBanRecordV1> records,
            out string error)
        {
            records = new List<TemporaryAutoTaskBanRecordV1>();
            error = string.Empty;

            if (!IsImplemented)
            {
                return CaptureFailed("A13 capture/ReturnGirl epoch guard is not healthy.", out error);
            }
            if (dataToSave == null || dataToSave.data_girls__Girls == null)
            {
                return CaptureFailed("A13 cannot capture without the exact populated SavedData idol roster.", out error);
            }
            if (agency.GirlsBannedFromAutoTasks == null)
            {
                return CaptureFailed("A13 live GirlsBannedFromAutoTasks registry is null.", out error);
            }

            Dictionary<int, int> savedCounts = BuildSavedGirlIdCounts(dataToSave.data_girls__Girls);
            HashSet<int> liveIds = new HashSet<int>();
            float now = ScaledTimeClock.Now;

            lock (Sync)
            {
                for (int i = 0; i < agency.GirlsBannedFromAutoTasks.Count; i++)
                {
                    data_girls.girls girl = agency.GirlsBannedFromAutoTasks[i];
                    if (girl == null || girl.id < 0 || !liveIds.Add(girl.id))
                    {
                        return CaptureFailedLocked(
                            "A13 live ban registry contains a null, invalid, or duplicate idol identity.",
                            out error);
                    }

                    int count;
                    if (!savedCounts.TryGetValue(girl.id, out count) || count != 1)
                    {
                        return CaptureFailedLocked(
                            "A13 banned idol does not resolve exactly once in the target SavedData roster.",
                            out error);
                    }

                    ActiveBan ban;
                    if (!ActiveByGirlId.TryGetValue(girl.id, out ban) || ban == null ||
                        !ban.IsAuthoritative || !LoadEpoch.IsCurrent(ban.Epoch) ||
                        !ReferenceEquals(ban.Girl, girl))
                    {
                        return CaptureFailedLocked(
                            "A13 live ban membership has no exact current expiry lease; refusing a falsely exact checkpoint.",
                            out error);
                    }

                    records.Add(
                        new TemporaryAutoTaskBanRecordV1
                        {
                            girl_id = girl.id,
                            remaining_scaled_seconds = GetRemainingSeconds(ban, now)
                        });
                }

                foreach (KeyValuePair<int, ActiveBan> pair in ActiveByGirlId)
                {
                    ActiveBan ban = pair.Value;
                    if (ban != null && ban.IsAuthoritative && LoadEpoch.IsCurrent(ban.Epoch) &&
                        !liveIds.Contains(pair.Key))
                    {
                        return CaptureFailedLocked(
                            "A13 current expiry lease no longer matches live ban membership.",
                            out error);
                    }
                }
            }

            records.Sort(
                delegate(TemporaryAutoTaskBanRecordV1 left, TemporaryAutoTaskBanRecordV1 right)
                {
                    return left.girl_id.CompareTo(right.girl_id);
                });

            Interlocked.Increment(ref capturedCheckpointCount);
            Interlocked.Add(ref capturedBanCount, records.Count);
            SetDiagnostic(
                "A13 captured " + records.Count.ToString(CultureInfo.InvariantCulture) +
                " temporary auto-task ban(s) for the exact save request.");
            return true;
        }

        /// <summary>
        /// F9 rebuilds idol objects but vanilla leaves the static ban list untouched.
        /// Clear both stale object membership and our current-epoch index before the
        /// target roster is created. Old ReturnGirl iterators remain under A08 and
        /// become inert because LoadEpoch already advanced before LoadEvent.
        /// </summary>
        internal static void ClearBeforeTargetGirlLoad()
        {
            int membershipCount = 0;
            if (agency.GirlsBannedFromAutoTasks == null)
            {
                agency.GirlsBannedFromAutoTasks = new List<data_girls.girls>();
            }
            else
            {
                membershipCount = agency.GirlsBannedFromAutoTasks.Count;
                agency.GirlsBannedFromAutoTasks.Clear();
            }

            int registryCount;
            lock (Sync)
            {
                registryCount = ActiveByGirlId.Count;
                ActiveByGirlId.Clear();
                lastDiagnostic =
                    "A13 cleared discarded-timeline auto-task-ban membership before target idol reconstruction.";
            }

            if (membershipCount > 0)
            {
                Interlocked.Add(ref staleMembershipClearCount, membershipCount);
            }
            if (registryCount > 0)
            {
                Interlocked.Add(ref staleRegistryClearCount, registryCount);
            }
        }

        internal static void RestoreAfterTargetGirlLoad()
        {
            if (!IsImplemented)
            {
                SetDiagnostic("A13 restore is not healthy; no temporary auto-task bans were reconstructed.");
                return;
            }

            SaveManager.SavedData target = GetTargetSavedData();
            if (target == null)
            {
                Interlocked.Increment(ref associationMissingCount);
                SetDiagnostic("A13 target SavedData is unavailable after idol reconstruction.");
                return;
            }

            RepairEnvelopeLoadState state;
            if (!RepairEnvelopeTransport.TryGetLoadState(target, out state))
            {
                Interlocked.Increment(ref associationMissingCount);
                SetDiagnostic("A13 target SavedData has no repair-envelope read association.");
                return;
            }

            if (!state.Present)
            {
                Interlocked.Increment(ref legacySectionAbsentCount);
                SetDiagnostic("A13 loaded a pre-envelope save; temporary ban state is unknowable and was not fabricated.");
                return;
            }
            if (!state.Valid || state.Envelope == null || state.Envelope.records == null)
            {
                RecordInvalid("A13 loaded a present but invalid repair envelope; ban state was not treated as empty.");
                return;
            }

            RepairEnvelopeRecordsV1 envelopeRecords = state.Envelope.records;
            if (envelopeRecords.temporary_auto_task_bans_version == 0)
            {
                Interlocked.Increment(ref legacySectionAbsentCount);
                SetDiagnostic("A13 loaded a pre-A13 envelope; temporary ban state is unknowable and was not fabricated.");
                return;
            }
            if (envelopeRecords.temporary_auto_task_bans_version != SectionVersion ||
                envelopeRecords.temporary_auto_task_bans == null || target.data_girls__Girls == null)
            {
                RecordInvalid("A13 repair section is invalid or unsupported.");
                return;
            }

            List<TemporaryAutoTaskBanRecordV1> records = envelopeRecords.temporary_auto_task_bans;
            if (records.Count == 0)
            {
                Interlocked.Increment(ref restoredLoadCount);
                SetDiagnostic("A13 restored an authoritative empty temporary-auto-task-ban section.");
                return;
            }

            Dictionary<int, int> savedCounts = BuildSavedGirlIdCounts(target.data_girls__Girls);
            Dictionary<int, data_girls.girls> liveById = BuildUniqueLiveGirls();
            HashSet<int> unique = new HashSet<int>();
            List<data_girls.girls> resolvedGirls = new List<data_girls.girls>();

            for (int i = 0; i < records.Count; i++)
            {
                TemporaryAutoTaskBanRecordV1 record = records[i];
                int savedCount;
                if (record == null || record.girl_id < 0 || !unique.Add(record.girl_id) ||
                    !IsFiniteDelay(record.remaining_scaled_seconds) ||
                    record.remaining_scaled_seconds < 0f || record.remaining_scaled_seconds > VanillaDelaySeconds ||
                    !savedCounts.TryGetValue(record.girl_id, out savedCount) || savedCount != 1)
                {
                    RecordInvalid("A13 section contains an invalid/duplicate idol ID or out-of-domain remaining delay.");
                    return;
                }

                data_girls.girls live;
                if (!liveById.TryGetValue(record.girl_id, out live) || live == null)
                {
                    Interlocked.Increment(ref runtimeResolutionFailureCount);
                    SetDiagnostic(
                        "A13 could not resolve saved banned idol ID " +
                        record.girl_id.ToString(CultureInfo.InvariantCulture) +
                        " to exactly one freshly loaded idol; no partial ban set was restored.");
                    return;
                }
                resolvedGirls.Add(live);
            }

            List<ActiveBan> restored = new List<ActiveBan>();
            for (int i = 0; i < records.Count; i++)
            {
                float remaining = records[i].remaining_scaled_seconds;
                if (remaining <= 0f)
                {
                    Interlocked.Increment(ref immediateExpiryCount);
                    continue;
                }

                ActiveBan ban;
                string error;
                if (!TryRestoreOne(resolvedGirls[i], remaining, out ban, out error))
                {
                    RollBackRestored(restored);
                    Interlocked.Increment(ref schedulingFailureCount);
                    SetDiagnostic(error);
                    Debug.LogError(SaveNLoadFixesConstants.LogPrefix + error);
                    return;
                }
                restored.Add(ban);
            }

            Interlocked.Increment(ref restoredLoadCount);
            Interlocked.Add(ref restoredBanCount, restored.Count);
            SetDiagnostic(
                "A13 restored " + restored.Count.ToString(CultureInfo.InvariantCulture) +
                " temporary auto-task ban(s) with saved scaled-time remainder.");
        }

        private static bool TryRestoreOne(
            data_girls.girls girl,
            float remaining,
            out ActiveBan ban,
            out string error)
        {
            ban = null;
            error = string.Empty;
            RestoreClaim claim = new RestoreClaim
            {
                Girl = girl,
                DelaySeconds = ClampDelay(remaining)
            };

            if (restoreClaim != null)
            {
                error = "A13 nested restore scheduling claim is not supported.";
                return false;
            }

            restoreClaim = claim;
            try
            {
                agency.BanGirlFromAutoJobs(girl);
            }
            catch (Exception exception)
            {
                error = "A13 failed while asking vanilla BanGirlFromAutoJobs() to restore idol " +
                    girl.id.ToString(CultureInfo.InvariantCulture) + ": " + exception.Message;
                return false;
            }
            finally
            {
                restoreClaim = null;
            }

            if (!claim.Claimed || claim.Ban == null || !claim.Ban.IsAuthoritative ||
                agency.GirlsBannedFromAutoTasks == null || !agency.GirlsBannedFromAutoTasks.Contains(girl))
            {
                agency.GirlsBannedFromAutoTasks?.Remove(girl);
                error = "A13 vanilla restore scheduling did not claim exactly one ReturnGirl iterator for idol " +
                    girl.id.ToString(CultureInfo.InvariantCulture) + ".";
                return false;
            }

            ban = claim.Ban;
            return true;
        }

        private static void RollBackRestored(List<ActiveBan> restored)
        {
            if (restored == null)
            {
                return;
            }

            for (int i = 0; i < restored.Count; i++)
            {
                ActiveBan ban = restored[i];
                if (ban == null)
                {
                    continue;
                }

                // A08 remains the sole MoveNext execution-suppression owner. Revoking
                // the exact carrier before removing membership makes rollback safe even
                // though we intentionally do not own a separate StopCoroutine path.
                DeferredCarrierEpochRepair.CancelCarrier(
                    ban.Iterator,
                    "A13 rolled back a partially restored temporary auto-task-ban transaction.");
                if (agency.GirlsBannedFromAutoTasks != null)
                {
                    agency.GirlsBannedFromAutoTasks.Remove(ban.Girl);
                }
                ActiveBan ignored;
                TryRetireIterator(ban.Iterator, out ignored);
                Interlocked.Increment(ref rollbackCount);
            }
        }

        private static bool TryRetireIterator(object iterator, out ActiveBan ban)
        {
            ban = null;
            if (iterator == null || !ByIterator.TryGetValue(iterator, out ban) || ban == null)
            {
                return false;
            }

            lock (Sync)
            {
                ByIterator.Remove(iterator);
                ActiveBan current;
                if (ban.IsAuthoritative && ActiveByGirlId.TryGetValue(ban.GirlId, out current) &&
                    ReferenceEquals(current, ban))
                {
                    ActiveByGirlId.Remove(ban.GirlId);
                }
            }
            return true;
        }

        private static float GetRemainingSeconds(ActiveBan ban, float now)
        {
            if (ban == null)
            {
                return 0f;
            }
            if (!ban.WaitStarted)
            {
                return ClampDelay(ban.ConfiguredDelaySeconds);
            }
            return ClampDelay(Math.Max(0f, ban.DueScaledTime - now));
        }

        private static float ClampDelay(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds))
            {
                return VanillaDelaySeconds;
            }
            return Math.Max(0f, Math.Min(VanillaDelaySeconds, seconds));
        }

        private static bool IsFiniteDelay(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static Dictionary<int, int> BuildSavedGirlIdCounts(List<data_girls.GirlData> rows)
        {
            Dictionary<int, int> counts = new Dictionary<int, int>();
            for (int i = 0; i < rows.Count; i++)
            {
                data_girls.GirlData row = rows[i];
                if (row == null || row.id < 0)
                {
                    continue;
                }
                int count;
                counts.TryGetValue(row.id, out count);
                counts[row.id] = count + 1;
            }
            return counts;
        }

        private static Dictionary<int, data_girls.girls> BuildUniqueLiveGirls()
        {
            Dictionary<int, data_girls.girls> unique = new Dictionary<int, data_girls.girls>();
            HashSet<int> ambiguous = new HashSet<int>();
            if (data_girls.girl == null)
            {
                return unique;
            }

            for (int i = 0; i < data_girls.girl.Count; i++)
            {
                data_girls.girls girl = data_girls.girl[i];
                if (girl == null || girl.id < 0)
                {
                    continue;
                }
                if (unique.ContainsKey(girl.id))
                {
                    ambiguous.Add(girl.id);
                    continue;
                }
                unique.Add(girl.id, girl);
            }
            foreach (int id in ambiguous)
            {
                unique.Remove(id);
            }
            return unique;
        }

        private static SaveManager.SavedData GetTargetSavedData()
        {
            try
            {
                mainScript main = Camera.main == null ? null : Camera.main.GetComponent<mainScript>();
                return main == null ? null : main.GetSavedData();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool CaptureFailed(string diagnostic, out string error)
        {
            lock (Sync)
            {
                return CaptureFailedLocked(diagnostic, out error);
            }
        }

        private static bool CaptureFailedLocked(string diagnostic, out string error)
        {
            error = diagnostic ?? "unknown A13 capture failure";
            Interlocked.Increment(ref captureFailureCount);
            lastDiagnostic = error;
            return false;
        }

        private static void RecordInvalid(string diagnostic)
        {
            Interlocked.Increment(ref invalidSectionCount);
            SetDiagnostic(diagnostic);
            Debug.LogError(SaveNLoadFixesConstants.LogPrefix + diagnostic);
        }

        private static void SetDiagnostic(string value)
        {
            lock (Sync)
            {
                lastDiagnostic = value ?? string.Empty;
            }
        }
    }

    internal static class TemporaryAutoTaskBanPatchHealth
    {
        internal const int ExpectedTargetMethodCount = 3;
        internal const int ExpectedWaitForSecondsSiteCount = 1;

        private static readonly object Sync = new object();
        private static readonly HashSet<string> ResolvedTargets = new HashSet<string>(StringComparer.Ordinal);
        private static int observedWaitSites = -1;
        private static string failure = string.Empty;

        internal static int ResolvedTargetMethodCount
        {
            get { lock (Sync) { return ResolvedTargets.Count; } }
        }

        internal static int ObservedWaitForSecondsSiteCount
        {
            get { lock (Sync) { return observedWaitSites; } }
        }

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return ResolvedTargets.Count == ExpectedTargetMethodCount &&
                        observedWaitSites == ExpectedWaitForSecondsSiteCount &&
                        string.IsNullOrEmpty(failure);
                }
            }
        }

        internal static void ReportTargetResolved(string target)
        {
            lock (Sync)
            {
                if (!ResolvedTargets.Add(target ?? string.Empty))
                {
                    failure = "A13 resolved the same Harmony target more than once: " + (target ?? string.Empty);
                }
                else if (ResolvedTargets.Count > ExpectedTargetMethodCount)
                {
                    failure = "A13 resolved more Harmony targets than the frozen three-method manifest.";
                }
            }
        }

        internal static void ReportWaitForSecondsSites(int count)
        {
            lock (Sync)
            {
                observedWaitSites = count;
                if (count != ExpectedWaitForSecondsSiteCount)
                {
                    failure = "A13 expected exactly one ReturnGirl WaitForSeconds(60f) construction but observed " +
                        count.ToString(CultureInfo.InvariantCulture) + ".";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown A13 patch failure";
            }
        }
    }
}
