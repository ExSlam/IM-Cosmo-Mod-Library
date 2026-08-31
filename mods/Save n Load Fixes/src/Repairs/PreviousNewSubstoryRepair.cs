using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using SaveNLoadFixes.Persistence;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A16: preserves Substories_Manager.PreviousNewSubstory, the private game-date
    /// anchor that gates the three too_many_dates* substories for 60 days. Repaired
    /// saves persist the exact anchor. Pre-A16 saves reconstruct only from target DTO
    /// evidence, never from same-process runtime dialogue timestamps.
    /// </summary>
    internal static class PreviousNewSubstoryRepair
    {
        internal const int SectionVersion = 1;

        private static readonly string[] RelevantDialogueIds =
        {
            "too_many_dates",
            "too_many_romantic_dates",
            "too_many_dates_conflict"
        };

        private static readonly FieldInfo PreviousNewSubstoryField = AccessTools.Field(
            typeof(Substories_Manager),
            "PreviousNewSubstory");

        private static long restoredLoadCount;
        private static long legacyQueuedFallbackCount;
        private static long legacyDialogueDateFallbackCount;
        private static long legacyStartDateFallbackCount;
        private static long legacyTargetDateFallbackCount;
        private static long invalidSectionCount;
        private static long associationMissingCount;
        private static long captureFailureCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return PreviousNewSubstoryPatchHealth.IsHealthy && PreviousNewSubstoryField != null; }
        }

        internal static long RestoredLoadCount { get { return Interlocked.Read(ref restoredLoadCount); } }
        internal static long LegacyQueuedFallbackCount { get { return Interlocked.Read(ref legacyQueuedFallbackCount); } }
        internal static long LegacyDialogueDateFallbackCount { get { return Interlocked.Read(ref legacyDialogueDateFallbackCount); } }
        internal static long LegacyStartDateFallbackCount { get { return Interlocked.Read(ref legacyStartDateFallbackCount); } }
        internal static long LegacyTargetDateFallbackCount { get { return Interlocked.Read(ref legacyTargetDateFallbackCount); } }
        internal static long InvalidSectionCount { get { return Interlocked.Read(ref invalidSectionCount); } }
        internal static long AssociationMissingCount { get { return Interlocked.Read(ref associationMissingCount); } }
        internal static long CaptureFailureCount { get { return Interlocked.Read(ref captureFailureCount); } }
        internal static string LastDiagnostic { get { return lastDiagnostic; } }

        internal static bool TryCaptureForEnvelope(
            SaveManager.SavedData dataToSave,
            out string previousNewSubstoryGameDate,
            out string error)
        {
            previousNewSubstoryGameDate = string.Empty;
            error = string.Empty;

            if (PreviousNewSubstoryField == null)
            {
                return CaptureFailed("A16 private Substories_Manager.PreviousNewSubstory field could not be resolved.", out error);
            }

            DateTime targetSaveDate;
            if (!TryReadTargetSaveDate(dataToSave, out targetSaveDate))
            {
                return CaptureFailed("A16 target SavedData game date is missing or invalid.", out error);
            }

            Substories_Manager manager = GetLiveManager();
            if (manager == null)
            {
                return CaptureFailed("A16 live Substories_Manager component is unavailable during caller-thread envelope capture.", out error);
            }

            DateTime anchor;
            try
            {
                object value = PreviousNewSubstoryField.GetValue(manager);
                if (!(value is DateTime))
                {
                    return CaptureFailed("A16 private PreviousNewSubstory field is not a DateTime value.", out error);
                }

                anchor = (DateTime)value;
            }
            catch (Exception exception)
            {
                return CaptureFailed("A16 could not read private PreviousNewSubstory: " + exception.Message, out error);
            }

            if (!IsAnchorInsideTargetTimeline(anchor, targetSaveDate))
            {
                return CaptureFailed("A16 PreviousNewSubstory is outside the audited StartDate-to-target-save timeline.", out error);
            }

            previousNewSubstoryGameDate = ExtensionMethods.ToDataString(anchor);
            return true;
        }

        internal static void RestoreAfterVanillaSubstoriesLoad(Substories_Manager manager)
        {
            if (manager == null || PreviousNewSubstoryField == null)
            {
                RecordInvalid("A16 cannot restore because Substories_Manager or its private PreviousNewSubstory field is unavailable.");
                return;
            }

            SaveManager.SavedData target = GetTargetSavedData();
            RepairEnvelopeLoadState state;
            if (target == null || !RepairEnvelopeTransport.TryGetLoadState(target, out state))
            {
                Interlocked.Increment(ref associationMissingCount);
                lastDiagnostic = "A16 failed closed because the adopted SavedData object has no repair-envelope read association.";
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
                return;
            }

            DateTime targetSaveDate;
            if (!TryReadTargetSaveDate(target, out targetSaveDate))
            {
                RecordInvalid("A16 target SavedData game date is missing or invalid.");
                return;
            }

            if (!state.Present)
            {
                RestoreLegacy(target, manager, targetSaveDate);
                return;
            }

            if (!state.Valid || state.Envelope == null || state.Envelope.records == null)
            {
                RecordInvalid("A16 failed closed because the present SNLF repair envelope is invalid.");
                return;
            }

            RepairEnvelopeRecordsV1 records = state.Envelope.records;
            if (records.previous_new_substory_version == 0)
            {
                RestoreLegacy(target, manager, targetSaveDate);
                return;
            }

            if (records.previous_new_substory_version != SectionVersion ||
                string.IsNullOrEmpty(records.previous_new_substory_game_date))
            {
                RecordInvalid("A16 repair section is unsupported or missing its exact spacing anchor.");
                return;
            }

            DateTime restored;
            if (!TryParseDataDate(records.previous_new_substory_game_date, out restored) ||
                !IsAnchorInsideTargetTimeline(restored, targetSaveDate))
            {
                RecordInvalid("A16 persisted PreviousNewSubstory timestamp is invalid or outside the target timeline.");
                return;
            }

            if (!TrySetAnchor(manager, restored))
            {
                RecordInvalid("A16 could not assign the validated PreviousNewSubstory timestamp.");
                return;
            }

            Interlocked.Increment(ref restoredLoadCount);
            lastDiagnostic = "A16 restored exact PreviousNewSubstory spacing anchor for SNLF checkpoint " +
                state.Envelope.checkpoint_id + ".";
        }

        private static void RestoreLegacy(
            SaveManager.SavedData target,
            Substories_Manager manager,
            DateTime targetSaveDate)
        {
            DateTime reconstructed;
            LegacyAnchorSource source;
            string error;
            if (!TryReconstructLegacyAnchor(target, targetSaveDate, out reconstructed, out source, out error))
            {
                RecordInvalid("A16 legacy reconstruction failed closed: " + error);
                return;
            }

            if (!TrySetAnchor(manager, reconstructed))
            {
                RecordInvalid("A16 could not assign the validated legacy spacing anchor.");
                return;
            }

            switch (source)
            {
                case LegacyAnchorSource.QueuedLaunchTime:
                    Interlocked.Increment(ref legacyQueuedFallbackCount);
                    lastDiagnostic = "A16 reconstructed legacy PreviousNewSubstory from the latest exact target queued too_many_dates* launchTime.";
                    break;
                case LegacyAnchorSource.DialogueDateLastTriggered:
                    Interlocked.Increment(ref legacyDialogueDateFallbackCount);
                    lastDiagnostic = "A16 reconstructed legacy PreviousNewSubstory from authoritative target data_dialogues__Data timestamp rows.";
                    break;
                case LegacyAnchorSource.StartDate:
                    Interlocked.Increment(ref legacyStartDateFallbackCount);
                    lastDiagnostic = "A16 found no target occurrence evidence and restored the ordinary staticVars.StartDate initial anchor.";
                    break;
                default:
                    Interlocked.Increment(ref legacyTargetDateFallbackCount);
                    lastDiagnostic = "A16 found target UsedSubstories occurrence evidence without a complete exact timestamp witness and used the target-save game date conservatively.";
                    break;
            }
        }

        private static bool TryReconstructLegacyAnchor(
            SaveManager.SavedData target,
            DateTime targetSaveDate,
            out DateTime anchor,
            out LegacyAnchorSource source,
            out string error)
        {
            anchor = default(DateTime);
            source = LegacyAnchorSource.StartDate;
            error = string.Empty;

            if (target == null)
            {
                error = "target SavedData is null";
                return false;
            }

            if (target.Substories_Manager__dialogueQueue == null ||
                target.Substories_Manager__UsedSubstories == null ||
                target.data_dialogues__Data == null)
            {
                error = "target substory/dialogue migration collections are null";
                return false;
            }

            bool hasQueued = false;
            DateTime latestQueued = default(DateTime);
            for (int index = 0; index < target.Substories_Manager__dialogueQueue.Count; index++)
            {
                Substories_Manager.QueueData row = target.Substories_Manager__dialogueQueue[index];
                if (row == null || !IsRelevantDialogueId(row.dialogue))
                {
                    continue;
                }

                DateTime launchTime;
                if (!TryParseDataDate(row.launchTime, out launchTime) ||
                    !IsAnchorInsideTargetTimeline(launchTime, targetSaveDate))
                {
                    error = "relevant target queued row has an invalid launchTime";
                    return false;
                }

                if (!hasQueued || launchTime > latestQueued)
                {
                    latestQueued = launchTime;
                    hasQueued = true;
                }
            }

            if (hasQueued)
            {
                anchor = latestQueued;
                source = LegacyAnchorSource.QueuedLaunchTime;
                return true;
            }

            HashSet<string> usedRelevant = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < target.Substories_Manager__UsedSubstories.Count; index++)
            {
                string id = target.Substories_Manager__UsedSubstories[index];
                if (IsRelevantDialogueId(id))
                {
                    usedRelevant.Add(id);
                }
            }

            Dictionary<string, DateTime> savedDates = new Dictionary<string, DateTime>(StringComparer.Ordinal);
            for (int index = 0; index < target.data_dialogues__Data.Count; index++)
            {
                data_dialogues.DialogueData row = target.data_dialogues__Data[index];
                if (row == null || !IsRelevantDialogueId(row.id))
                {
                    continue;
                }

                if (savedDates.ContainsKey(row.id))
                {
                    error = "target data_dialogues__Data contains duplicate relevant dialogue rows";
                    return false;
                }

                DateTime triggered;
                if (!TryParseDataDate(row.Date_LastTriggered, out triggered) ||
                    !IsAnchorInsideTargetTimeline(triggered, targetSaveDate))
                {
                    error = "relevant target Date_LastTriggered row is invalid or outside the target timeline";
                    return false;
                }

                savedDates.Add(row.id, triggered);
            }

            if (savedDates.Count > 0)
            {
                bool everyUsedHasDate = true;
                foreach (string usedId in usedRelevant)
                {
                    if (!savedDates.ContainsKey(usedId))
                    {
                        everyUsedHasDate = false;
                        break;
                    }
                }

                if (usedRelevant.Count == 0 || everyUsedHasDate)
                {
                    bool hasLatest = false;
                    DateTime latest = default(DateTime);
                    foreach (KeyValuePair<string, DateTime> pair in savedDates)
                    {
                        if (!hasLatest || pair.Value > latest)
                        {
                            latest = pair.Value;
                            hasLatest = true;
                        }
                    }

                    anchor = latest;
                    source = LegacyAnchorSource.DialogueDateLastTriggered;
                    return true;
                }
            }

            if (usedRelevant.Count == 0)
            {
                anchor = staticVars.StartDate;
                source = LegacyAnchorSource.StartDate;
                return true;
            }

            // UsedSubstories proves that at least one relevant occurrence happened, but
            // without a complete target timestamp witness the audit forbids inventing an
            // earlier historical date. The target save date conservatively closes the gate.
            anchor = targetSaveDate;
            source = LegacyAnchorSource.TargetSaveDate;
            return true;
        }

        private static bool IsRelevantDialogueId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            for (int index = 0; index < RelevantDialogueIds.Length; index++)
            {
                if (string.Equals(id, RelevantDialogueIds[index], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAnchorInsideTargetTimeline(DateTime value, DateTime targetSaveDate)
        {
            return value >= staticVars.StartDate && value <= targetSaveDate;
        }

        private static bool TrySetAnchor(Substories_Manager manager, DateTime value)
        {
            if (manager == null || PreviousNewSubstoryField == null)
            {
                return false;
            }

            try
            {
                PreviousNewSubstoryField.SetValue(manager, value);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
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

        private static Substories_Manager GetLiveManager()
        {
            try
            {
                if (Camera.main == null)
                {
                    return null;
                }

                mainScript main = Camera.main.GetComponent<mainScript>();
                if (main == null || main.Data == null)
                {
                    return null;
                }

                return main.Data.GetComponent<Substories_Manager>();
            }
            catch (Exception)
            {
                return null;
            }
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

        private static bool CaptureFailed(string diagnostic, out string error)
        {
            Interlocked.Increment(ref captureFailureCount);
            lastDiagnostic = diagnostic ?? "A16 envelope capture failed.";
            error = lastDiagnostic;
            return false;
        }

        private static void RecordInvalid(string diagnostic)
        {
            Interlocked.Increment(ref invalidSectionCount);
            lastDiagnostic = diagnostic ?? "A16 repair section failed validation.";
            Debug.LogError(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
        }

        private enum LegacyAnchorSource
        {
            QueuedLaunchTime,
            DialogueDateLastTriggered,
            StartDate,
            TargetSaveDate
        }
    }

    internal static class PreviousNewSubstoryPatchHealth
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
                    failure = "A16 resolved more target methods than the frozen one-method manifest.";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown A16 patch failure";
            }
        }
    }
}
