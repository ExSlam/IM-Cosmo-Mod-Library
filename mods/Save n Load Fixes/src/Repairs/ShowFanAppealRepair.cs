using System;
using System.Collections.Generic;
using System.Threading;
using SaveNLoadFixes.Persistence;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs numbered finding N05. Vanilla reconstructs Shows._show without the
    /// last completed episode's FanAppeal list even though later show behavior,
    /// including cancellation disappointment, consumes that live field.
    /// </summary>
    internal static class ShowFanAppealRepair
    {
        private const int SectionVersion = 1;

        private static long restoredLoadCount;
        private static long restoredShowCount;
        private static long legacySectionAbsentCount;
        private static long invalidEnvelopeCount;
        private static long associationMissingCount;
        private static long exactSetMismatchCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return ShowFanAppealPatchHealth.IsHealthy; }
        }

        internal static long RestoredLoadCount { get { return Interlocked.Read(ref restoredLoadCount); } }
        internal static long RestoredShowCount { get { return Interlocked.Read(ref restoredShowCount); } }
        internal static long LegacySectionAbsentCount { get { return Interlocked.Read(ref legacySectionAbsentCount); } }
        internal static long InvalidEnvelopeCount { get { return Interlocked.Read(ref invalidEnvelopeCount); } }
        internal static long AssociationMissingCount { get { return Interlocked.Read(ref associationMissingCount); } }
        internal static long ExactSetMismatchCount { get { return Interlocked.Read(ref exactSetMismatchCount); } }
        internal static string LastDiagnostic { get { return lastDiagnostic; } }

        internal static void RestoreAfterVanillaLoad()
        {
            SaveManager.SavedData target = GetTargetSavedData();
            RepairEnvelopeLoadState state;
            if (target == null || !RepairEnvelopeTransport.TryGetLoadState(target, out state))
            {
                Interlocked.Increment(ref associationMissingCount);
                lastDiagnostic = "N05 skipped because the adopted SavedData object has no repair-envelope read association.";
                return;
            }

            if (!state.Present)
            {
                Interlocked.Increment(ref legacySectionAbsentCount);
                lastDiagnostic = "N05 loaded a pre-envelope save; exact last-episode show FanAppeal is unavailable.";
                return;
            }

            if (!state.Valid || state.Envelope == null || state.Envelope.records == null)
            {
                Interlocked.Increment(ref invalidEnvelopeCount);
                lastDiagnostic = "N05 failed closed because the repair envelope is invalid.";
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
                return;
            }

            RepairEnvelopeRecordsV1 records = state.Envelope.records;
            if (records.show_fan_appeals_version != SectionVersion)
            {
                Interlocked.Increment(ref legacySectionAbsentCount);
                lastDiagnostic = "N05 section is absent from this compatible older envelope; vanilla show FanAppeal is left untouched.";
                return;
            }

            if (records.show_fan_appeals == null)
            {
                RecordMismatch("N05 section marker is present but show_fan_appeals is null.");
                return;
            }

            List<Shows._show> current = Shows.shows;
            if (current == null)
            {
                RecordMismatch("N05 current show registry is null after vanilla LoadFunction.");
                return;
            }

            Dictionary<int, Shows._show> currentById = new Dictionary<int, Shows._show>();
            for (int index = 0; index < current.Count; index++)
            {
                Shows._show show = current[index];
                if (show == null || show.id < 0 || currentById.ContainsKey(show.id))
                {
                    RecordMismatch("N05 current show set contains a null, invalid, or duplicate show ID.");
                    return;
                }

                currentById.Add(show.id, show);
            }

            if (currentById.Count != records.show_fan_appeals.Count)
            {
                RecordMismatch("N05 envelope/current show counts do not form an exact one-to-one set.");
                return;
            }

            Dictionary<int, List<singles._fanAppeal>> restoredById =
                new Dictionary<int, List<singles._fanAppeal>>();

            for (int index = 0; index < records.show_fan_appeals.Count; index++)
            {
                ShowFanAppealRecordV1 record = records.show_fan_appeals[index];
                if (record == null || record.show_id < 0 || record.fan_appeal == null ||
                    restoredById.ContainsKey(record.show_id) || !currentById.ContainsKey(record.show_id))
                {
                    RecordMismatch("N05 envelope show IDs do not exactly match vanilla reconstruction.");
                    return;
                }

                List<singles._fanAppeal> restoredAppeal = new List<singles._fanAppeal>();
                for (int appealIndex = 0; appealIndex < record.fan_appeal.Count; appealIndex++)
                {
                    FanAppealRatioRecordV1 savedAppeal = record.fan_appeal[appealIndex];
                    if (savedAppeal == null ||
                        savedAppeal.fan_type < (int)resources.fanType.male ||
                        savedAppeal.fan_type > (int)resources.fanType.adult ||
                        float.IsNaN(savedAppeal.ratio) ||
                        float.IsInfinity(savedAppeal.ratio))
                    {
                        RecordMismatch("N05 envelope contains an invalid FanAppeal entry.");
                        return;
                    }

                    restoredAppeal.Add(
                        new singles._fanAppeal
                        {
                            type = (resources.fanType)savedAppeal.fan_type,
                            ratio = savedAppeal.ratio
                        });
                }

                restoredById.Add(record.show_id, restoredAppeal);
            }

            // Apply only after every show and every ordered FanAppeal entry validates.
            foreach (KeyValuePair<int, List<singles._fanAppeal>> pair in restoredById)
            {
                currentById[pair.Key].FanAppeal = pair.Value;
            }

            Interlocked.Increment(ref restoredLoadCount);
            Interlocked.Add(ref restoredShowCount, restoredById.Count);
            lastDiagnostic = "N05 restored exact last-episode show FanAppeal for SNLF checkpoint " +
                state.Envelope.checkpoint_id + ".";
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

        private static void RecordMismatch(string diagnostic)
        {
            Interlocked.Increment(ref exactSetMismatchCount);
            lastDiagnostic = diagnostic ?? "N05 exact show-set validation failed.";
            Debug.LogError(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
        }
    }

    internal static class ShowFanAppealPatchHealth
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

        internal static void ReportTargetResolved()
        {
            lock (Sync)
            {
                resolvedTargetMethodCount++;
                if (resolvedTargetMethodCount > ExpectedTargetMethodCount)
                {
                    failure = "N05 resolved more target methods than the frozen one-method manifest.";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown N05 patch failure";
            }
        }
    }
}
