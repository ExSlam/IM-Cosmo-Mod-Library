using System;
using System.Collections.Generic;
using System.Threading;
using SaveNLoadFixes.Persistence;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A15: preserves the bounded gameplay recency timeline consumed by
    /// Stats.CountActivities(...) and derives Activities.LastHeal from the newest
    /// restored spa occurrence. The repair owns one rolling 10-day timeline only;
    /// LastHeal is never persisted as a second independent value.
    /// </summary>
    internal static class RecentActivityRecencyRepair
    {
        internal const int SectionVersion = 1;
        internal const int HorizonDays = 10;

        private static long captureFailureCount;
        private static long restoredLoadCount;
        private static long restoredRowCount;
        private static long restoredSpaAnchorCount;
        private static long legacyEmptyFallbackCount;
        private static long invalidSectionCount;
        private static long associationMissingCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return RecentActivityRecencyPatchHealth.IsHealthy; }
        }

        internal static long CaptureFailureCount { get { return Interlocked.Read(ref captureFailureCount); } }
        internal static long RestoredLoadCount { get { return Interlocked.Read(ref restoredLoadCount); } }
        internal static long RestoredRowCount { get { return Interlocked.Read(ref restoredRowCount); } }
        internal static long RestoredSpaAnchorCount { get { return Interlocked.Read(ref restoredSpaAnchorCount); } }
        internal static long LegacyEmptyFallbackCount { get { return Interlocked.Read(ref legacyEmptyFallbackCount); } }
        internal static long InvalidSectionCount { get { return Interlocked.Read(ref invalidSectionCount); } }
        internal static long AssociationMissingCount { get { return Interlocked.Read(ref associationMissingCount); } }
        internal static string LastDiagnostic { get { return lastDiagnostic; } }

        internal static bool TryCaptureForEnvelope(
            SaveManager.SavedData dataToSave,
            out List<RecentActivityRecordV1> records,
            out string error)
        {
            records = new List<RecentActivityRecordV1>();
            error = string.Empty;

            DateTime targetSaveDate;
            if (!TryReadTargetSaveDate(dataToSave, out targetSaveDate))
            {
                return CaptureFailed("A15 target SavedData game date is missing or invalid.", out error);
            }

            if (Stats.Activities_Stats == null)
            {
                return CaptureFailed("A15 live Stats.Activities_Stats registry is null.", out error);
            }

            DateTime horizonStart = targetSaveDate.AddDays(-HorizonDays);
            DateTime? newestSpa = null;
            for (int index = 0; index < Stats.Activities_Stats.Count; index++)
            {
                Stats._activities_stats live = Stats.Activities_Stats[index];
                if (live == null)
                {
                    return CaptureFailed("A15 live recent-activity registry contains a null row.", out error);
                }

                if (live._Date > targetSaveDate)
                {
                    return CaptureFailed("A15 live activity timestamp is later than the exact target-save game date.", out error);
                }

                if (live._Date <= horizonStart)
                {
                    continue;
                }

                int type = (int)live.Type;
                if (!IsValidActivityType(type))
                {
                    return CaptureFailed("A15 live activity row contains a type outside the audited enum domain.", out error);
                }

                records.Add(
                    new RecentActivityRecordV1
                    {
                        activity_type = type,
                        game_date = ExtensionMethods.ToDataString(live._Date)
                    });

                if (live.Type == Activity._type.spa_treatment &&
                    (!newestSpa.HasValue || live._Date > newestSpa.Value))
                {
                    newestSpa = live._Date;
                }
            }

            // Under vanilla SpaTreatment(), Stats.AddActivity(spa) and LastHeal use the
            // same staticVars.dateTime. Validate that the single persisted timeline can
            // reproduce the live spa recency semantics before freezing the checkpoint.
            DateTime lastHeal = Activities.LastHeal;
            if (lastHeal > targetSaveDate)
            {
                return CaptureFailed("A15 Activities.LastHeal is later than the exact target-save game date.", out error);
            }

            if (newestSpa.HasValue)
            {
                if (lastHeal != newestSpa.Value)
                {
                    return CaptureFailed("A15 recent spa timeline does not agree with the live LastHeal anchor.", out error);
                }
            }
            else if (lastHeal > horizonStart)
            {
                return CaptureFailed("A15 live LastHeal is recent but the bounded timeline contains no matching spa occurrence.", out error);
            }

            return true;
        }

        internal static void RestoreAfterVanillaStatsLoad()
        {
            SaveManager.SavedData target = GetTargetSavedData();
            RepairEnvelopeLoadState state;
            if (target == null || !RepairEnvelopeTransport.TryGetLoadState(target, out state))
            {
                Interlocked.Increment(ref associationMissingCount);
                lastDiagnostic = "A15 failed closed because the adopted SavedData object has no repair-envelope read association.";
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
                return;
            }

            DateTime targetSaveDate;
            if (!TryReadTargetSaveDate(target, out targetSaveDate))
            {
                RecordInvalid("A15 target SavedData game date is missing or invalid.");
                return;
            }

            if (!state.Present || state.Envelope == null || state.Envelope.records == null ||
                state.Envelope.records.recent_activity_timeline_version == 0)
            {
                if (!TryApplyTimeline(new List<Stats._activities_stats>(), targetSaveDate.AddDays(-HorizonDays)))
                {
                    RecordInvalid("A15 could not apply the audited empty legacy recency baseline.");
                    return;
                }

                Interlocked.Increment(ref legacyEmptyFallbackCount);
                lastDiagnostic = "A15 loaded a pre-A15 save; recent activity history remains empty and LastHeal uses a deterministic old-enough anchor.";
                return;
            }

            RepairEnvelopeRecordsV1 envelopeRecords = state.Envelope.records;
            if (!state.Valid ||
                envelopeRecords.recent_activity_timeline_version != SectionVersion ||
                envelopeRecords.recent_activity_timeline == null)
            {
                RecordInvalid("A15 repair section is invalid, unsupported, or missing its bounded activity list.");
                return;
            }

            DateTime horizonStart = targetSaveDate.AddDays(-HorizonDays);
            List<Stats._activities_stats> restored = new List<Stats._activities_stats>();
            DateTime? newestSpa = null;
            for (int index = 0; index < envelopeRecords.recent_activity_timeline.Count; index++)
            {
                RecentActivityRecordV1 record = envelopeRecords.recent_activity_timeline[index];
                DateTime rowDate;
                if (record == null ||
                    !IsValidActivityType(record.activity_type) ||
                    !TryParseDataDate(record.game_date, out rowDate) ||
                    rowDate <= horizonStart ||
                    rowDate > targetSaveDate)
                {
                    RecordInvalid("A15 bounded activity record contains an invalid type or timestamp outside the strict 10-day target horizon.");
                    return;
                }

                Activity._type type = (Activity._type)record.activity_type;
                restored.Add(
                    new Stats._activities_stats
                    {
                        Type = type,
                        _Date = rowDate
                    });

                if (type == Activity._type.spa_treatment &&
                    (!newestSpa.HasValue || rowDate > newestSpa.Value))
                {
                    newestSpa = rowDate;
                }
            }

            DateTime lastHeal = newestSpa.HasValue
                ? newestSpa.Value
                : horizonStart;

            if (!TryApplyTimeline(restored, lastHeal))
            {
                RecordInvalid("A15 validated recency timeline could not be applied atomically.");
                return;
            }

            Interlocked.Increment(ref restoredLoadCount);
            Interlocked.Add(ref restoredRowCount, restored.Count);
            if (newestSpa.HasValue)
            {
                Interlocked.Increment(ref restoredSpaAnchorCount);
            }

            lastDiagnostic = "A15 restored the bounded 10-day activity recency timeline for SNLF checkpoint " +
                state.Envelope.checkpoint_id + ".";
        }

        private static bool TryApplyTimeline(List<Stats._activities_stats> timeline, DateTime lastHeal)
        {
            List<Stats._activities_stats> oldTimeline = Stats.Activities_Stats;
            DateTime oldLastHeal = Activities.LastHeal;
            try
            {
                Stats.Activities_Stats = timeline;
                Activities.LastHeal = lastHeal;
                return true;
            }
            catch (Exception)
            {
                try
                {
                    Stats.Activities_Stats = oldTimeline;
                    Activities.LastHeal = oldLastHeal;
                }
                catch (Exception)
                {
                    // Best-effort rollback; caller still records the repair as failed closed.
                }
                return false;
            }
        }

        private static bool IsValidActivityType(int value)
        {
            return value >= (int)Activity._type.performance &&
                value <= (int)Activity._type.spa_treatment;
        }

        private static bool TryReadTargetSaveDate(SaveManager.SavedData target, out DateTime result)
        {
            result = default(DateTime);
            return target != null && TryParseDataDate(target.staticVars__dateTime, out result);
        }

        private static bool TryParseDataDate(string value, out DateTime result)
        {
            result = default(DateTime);
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            try
            {
                result = ExtensionMethods.ToDateTime(value);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool CaptureFailed(string diagnostic, out string error)
        {
            Interlocked.Increment(ref captureFailureCount);
            lastDiagnostic = diagnostic ?? "A15 envelope capture failed.";
            error = lastDiagnostic;
            return false;
        }

        private static void RecordInvalid(string diagnostic)
        {
            Interlocked.Increment(ref invalidSectionCount);
            lastDiagnostic = diagnostic ?? "A15 repair section failed validation.";
            Debug.LogError(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
        }

        private static SaveManager.SavedData GetTargetSavedData()
        {
            try
            {
                if (Camera.main == null)
                {
                    return null;
                }

                mainScript main = Camera.main.GetComponent<mainScript>();
                return main == null ? null : main.GetSavedData();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    internal static class RecentActivityRecencyPatchHealth
    {
        internal const int ExpectedTargetMethodCount = 1;

        private static readonly object Sync = new object();
        private static int resolvedTargetMethodCount;
        private static string failure = string.Empty;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return resolvedTargetMethodCount == ExpectedTargetMethodCount &&
                        string.IsNullOrEmpty(failure);
                }
            }
        }

        internal static int ResolvedTargetMethodCount
        {
            get { lock (Sync) { return resolvedTargetMethodCount; } }
        }

        internal static void ReportTargetResolved()
        {
            lock (Sync)
            {
                resolvedTargetMethodCount++;
                if (resolvedTargetMethodCount > ExpectedTargetMethodCount)
                {
                    failure = "A15 resolved more target methods than the frozen one-method manifest.";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown A15 patch failure";
            }
        }
    }
}
