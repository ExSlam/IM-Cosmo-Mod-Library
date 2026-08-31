using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using SaveNLoadFixes.Persistence;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs numbered finding N08. Vanilla Event_Overlord.SaveFunction is empty,
    /// while LoadFunction calls Reset and overwrites the private pacing anchor with
    /// staticVars.StartDate. Repaired saves persist the exact anchor; older saves use
    /// the audited conservative target-save game date rather than current process RAM.
    /// </summary>
    internal static class EventOverlordLatestEventRepair
    {
        internal const int SectionVersion = 1;

        private static readonly FieldInfo LatestEventField = AccessTools.Field(
            typeof(Event_Overlord),
            "Latest_Event");

        private static long restoredLoadCount;
        private static long legacyTargetDateFallbackCount;
        private static long invalidSectionCount;
        private static long associationMissingCount;
        private static long captureFailureCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return EventOverlordLatestEventPatchHealth.IsHealthy && LatestEventField != null; }
        }

        internal static long RestoredLoadCount { get { return Interlocked.Read(ref restoredLoadCount); } }
        internal static long LegacyTargetDateFallbackCount { get { return Interlocked.Read(ref legacyTargetDateFallbackCount); } }
        internal static long InvalidSectionCount { get { return Interlocked.Read(ref invalidSectionCount); } }
        internal static long AssociationMissingCount { get { return Interlocked.Read(ref associationMissingCount); } }
        internal static long CaptureFailureCount { get { return Interlocked.Read(ref captureFailureCount); } }
        internal static string LastDiagnostic { get { return lastDiagnostic; } }

        internal static bool TryCaptureForEnvelope(
            SaveManager.SavedData dataToSave,
            out string latestEventGameDate,
            out string error)
        {
            latestEventGameDate = string.Empty;
            error = string.Empty;

            if (LatestEventField == null)
            {
                return CaptureFailed("N08 private Event_Overlord.Latest_Event field could not be resolved.", out error);
            }

            DateTime targetSaveDate;
            if (!TryReadTargetSaveDate(dataToSave, out targetSaveDate))
            {
                return CaptureFailed("N08 target SavedData game date is missing or invalid.", out error);
            }

            DateTime latestEvent;
            try
            {
                object value = LatestEventField.GetValue(null);
                if (!(value is DateTime))
                {
                    return CaptureFailed("N08 private Latest_Event field is not a DateTime value.", out error);
                }

                latestEvent = (DateTime)value;
            }
            catch (Exception exception)
            {
                return CaptureFailed("N08 could not read private Latest_Event: " + exception.Message, out error);
            }

            // Vanilla only assigns StartDate or the then-current game time. Rejecting a
            // future anchor prevents a corrupted live value from becoming authoritative.
            if (latestEvent > targetSaveDate)
            {
                return CaptureFailed("N08 Latest_Event is later than the exact target-save game date.", out error);
            }

            latestEventGameDate = ExtensionMethods.ToDataString(latestEvent);
            return true;
        }

        internal static void RestoreAfterVanillaLoad()
        {
            SaveManager.SavedData target = GetTargetSavedData();
            RepairEnvelopeLoadState state;
            if (target == null || !RepairEnvelopeTransport.TryGetLoadState(target, out state))
            {
                Interlocked.Increment(ref associationMissingCount);
                lastDiagnostic = "N08 failed closed because the adopted SavedData object has no repair-envelope read association.";
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
                return;
            }

            DateTime targetSaveDate;
            if (!TryReadTargetSaveDate(target, out targetSaveDate))
            {
                RecordInvalid("N08 target SavedData game date is missing or invalid.");
                return;
            }

            if (!state.Present || state.Envelope == null || state.Envelope.records == null ||
                state.Envelope.records.event_overlord_latest_event_version == 0)
            {
                if (!TrySetLatestEvent(targetSaveDate))
                {
                    RecordInvalid("N08 could not apply the audited target-save-date legacy fallback.");
                    return;
                }

                Interlocked.Increment(ref legacyTargetDateFallbackCount);
                lastDiagnostic = "N08 loaded a pre-N08 save; Event_Overlord.Latest_Event was conservatively anchored to the target-save game date.";
                return;
            }

            RepairEnvelopeRecordsV1 records = state.Envelope.records;
            if (!state.Valid ||
                records.event_overlord_latest_event_version != SectionVersion ||
                string.IsNullOrEmpty(records.event_overlord_latest_event_game_date))
            {
                RecordInvalid("N08 repair section is invalid, unsupported, or missing its exact timestamp.");
                return;
            }

            DateTime restored;
            if (!TryParseDataDate(records.event_overlord_latest_event_game_date, out restored) ||
                restored > targetSaveDate)
            {
                RecordInvalid("N08 saved Latest_Event timestamp is invalid or later than the target-save game date.");
                return;
            }

            if (!TrySetLatestEvent(restored))
            {
                RecordInvalid("N08 could not assign the validated Latest_Event timestamp.");
                return;
            }

            Interlocked.Increment(ref restoredLoadCount);
            lastDiagnostic = "N08 restored exact Event_Overlord.Latest_Event pacing anchor for SNLF checkpoint " +
                state.Envelope.checkpoint_id + ".";
        }

        private static bool TryReadTargetSaveDate(
            SaveManager.SavedData target,
            out DateTime targetSaveDate)
        {
            targetSaveDate = default(DateTime);
            return target != null &&
                !string.IsNullOrEmpty(target.staticVars__dateTime) &&
                TryParseDataDate(target.staticVars__dateTime, out targetSaveDate);
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

        private static bool TrySetLatestEvent(DateTime value)
        {
            if (LatestEventField == null)
            {
                return false;
            }

            try
            {
                LatestEventField.SetValue(null, value);
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
            lastDiagnostic = diagnostic ?? "N08 envelope capture failed.";
            error = lastDiagnostic;
            return false;
        }

        private static void RecordInvalid(string diagnostic)
        {
            Interlocked.Increment(ref invalidSectionCount);
            lastDiagnostic = diagnostic ?? "N08 repair section failed validation.";
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

    internal static class EventOverlordLatestEventPatchHealth
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
                    failure = "N08 resolved more target methods than the frozen one-method manifest.";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown N08 patch failure";
            }
        }
    }
}
