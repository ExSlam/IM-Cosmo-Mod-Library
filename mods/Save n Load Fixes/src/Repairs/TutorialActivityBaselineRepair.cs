using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using SaveNLoadFixes.Persistence;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// N11 / finding #12. Tutorial_Reqs keeps two private static "do one more
    /// activity" baselines that vanilla resets to -1 on load. Persist nullable
    /// baseline semantics in the SNLF envelope and restore them after Tutorial's
    /// own reset/load has completed. Null maps only to vanilla's -1 sentinel.
    /// </summary>
    internal static class TutorialActivityBaselineRepair
    {
        internal const int SectionVersion = 1;

        private static readonly FieldInfo PerformanceCounterField =
            AccessTools.Field(typeof(Tutorial_Reqs), "performance_counter");
        private static readonly FieldInfo PromotionCounterField =
            AccessTools.Field(typeof(Tutorial_Reqs), "promotion_counter");

        private static long restoredLoadCount;
        private static long restoredInitializedBaselineCount;
        private static long legacyUninitializedCount;
        private static long invalidSectionCount;
        private static long associationMissingCount;
        private static long captureFailureCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get
            {
                return TutorialActivityBaselinePatchHealth.IsHealthy &&
                    PerformanceCounterField != null &&
                    PromotionCounterField != null;
            }
        }

        internal static long RestoredLoadCount { get { return Interlocked.Read(ref restoredLoadCount); } }
        internal static long RestoredInitializedBaselineCount { get { return Interlocked.Read(ref restoredInitializedBaselineCount); } }
        internal static long LegacyUninitializedCount { get { return Interlocked.Read(ref legacyUninitializedCount); } }
        internal static long InvalidSectionCount { get { return Interlocked.Read(ref invalidSectionCount); } }
        internal static long AssociationMissingCount { get { return Interlocked.Read(ref associationMissingCount); } }
        internal static long CaptureFailureCount { get { return Interlocked.Read(ref captureFailureCount); } }
        internal static string LastDiagnostic { get { return lastDiagnostic; } }

        internal static bool TryCaptureForEnvelope(
            SaveManager.SavedData dataToSave,
            out TutorialActivityBaselinesRecordV1 record,
            out string error)
        {
            record = null;
            error = string.Empty;

            if (PerformanceCounterField == null || PromotionCounterField == null)
            {
                return CaptureFailed("N11 private Tutorial_Reqs baseline fields could not be resolved.", out error);
            }

            if (dataToSave == null || dataToSave.Stats__data == null)
            {
                return CaptureFailed("N11 target SavedData Stats__data is unavailable after SaveEvent population.", out error);
            }

            int performance;
            int promotion;
            if (!TryReadCurrentBaselines(out performance, out promotion))
            {
                return CaptureFailed("N11 could not read the private Tutorial_Reqs baseline fields.", out error);
            }

            if (!IsValidBaseline(performance, dataToSave.Stats__data.activities_performance) ||
                !IsValidBaseline(promotion, dataToSave.Stats__data.activities_promotion))
            {
                return CaptureFailed("N11 live tutorial baseline is outside the exact target aggregate activity count.", out error);
            }

            record = new TutorialActivityBaselinesRecordV1
            {
                performance_baseline_has_value = performance >= 0,
                performance_baseline = performance >= 0 ? performance : 0,
                promotion_baseline_has_value = promotion >= 0,
                promotion_baseline = promotion >= 0 ? promotion : 0
            };
            return true;
        }

        internal static void RestoreAfterVanillaTutorialLoad()
        {
            SaveManager.SavedData target = GetTargetSavedData();
            RepairEnvelopeLoadState state;
            if (target == null || !RepairEnvelopeTransport.TryGetLoadState(target, out state))
            {
                Interlocked.Increment(ref associationMissingCount);
                lastDiagnostic = "N11 failed closed because the adopted SavedData object has no repair-envelope read association.";
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
                return;
            }

            if (!state.Present || state.Envelope == null || state.Envelope.records == null ||
                state.Envelope.records.tutorial_activity_baselines_version == 0)
            {
                // Vanilla Tutorial.LoadFunction() has already called Tutorial.Reset(), but
                // assign the audited sentinel explicitly so a discarded F9 timeline can
                // never leak a newer process-static baseline into a pre-N11 target.
                if (!TrySetBaselines(-1, -1))
                {
                    RecordInvalid("N11 could not preserve the uninitialized legacy tutorial baseline state.");
                    return;
                }

                Interlocked.Increment(ref legacyUninitializedCount);
                lastDiagnostic = "N11 loaded a pre-N11 save; tutorial activity baselines remain uninitialized so vanilla can seed from target aggregate counters on first check.";
                return;
            }

            RepairEnvelopeRecordsV1 records = state.Envelope.records;
            TutorialActivityBaselinesRecordV1 saved = records.tutorial_activity_baselines;
            if (!state.Valid ||
                records.tutorial_activity_baselines_version != SectionVersion ||
                saved == null ||
                target.Stats__data == null)
            {
                RecordInvalid("N11 repair section is invalid, unsupported, or missing its target Stats witness.");
                return;
            }

            int performance = saved.performance_baseline_has_value
                ? saved.performance_baseline
                : -1;
            int promotion = saved.promotion_baseline_has_value
                ? saved.promotion_baseline
                : -1;

            if (!IsValidBaseline(performance, target.Stats__data.activities_performance) ||
                !IsValidBaseline(promotion, target.Stats__data.activities_promotion))
            {
                RecordInvalid("N11 saved tutorial baseline exceeds or conflicts with the exact target aggregate activity count.");
                return;
            }

            if (!TrySetBaselines(performance, promotion))
            {
                RecordInvalid("N11 could not atomically assign the validated tutorial activity baselines.");
                return;
            }

            Interlocked.Increment(ref restoredLoadCount);
            long initialized = (performance >= 0 ? 1L : 0L) + (promotion >= 0 ? 1L : 0L);
            if (initialized != 0)
            {
                Interlocked.Add(ref restoredInitializedBaselineCount, initialized);
            }

            lastDiagnostic = "N11 restored nullable tutorial performance/promotion baselines for SNLF checkpoint " +
                state.Envelope.checkpoint_id + ".";
        }

        private static bool IsValidBaseline(int baseline, int targetAggregate)
        {
            return targetAggregate >= 0 && baseline >= -1 &&
                (baseline == -1 || baseline <= targetAggregate);
        }

        private static bool TryReadCurrentBaselines(out int performance, out int promotion)
        {
            performance = -1;
            promotion = -1;
            if (PerformanceCounterField == null || PromotionCounterField == null)
            {
                return false;
            }

            try
            {
                object performanceValue = PerformanceCounterField.GetValue(null);
                object promotionValue = PromotionCounterField.GetValue(null);
                if (!(performanceValue is int) || !(promotionValue is int))
                {
                    return false;
                }

                performance = (int)performanceValue;
                promotion = (int)promotionValue;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TrySetBaselines(int performance, int promotion)
        {
            if (PerformanceCounterField == null || PromotionCounterField == null)
            {
                return false;
            }

            int oldPerformance;
            int oldPromotion;
            if (!TryReadCurrentBaselines(out oldPerformance, out oldPromotion))
            {
                return false;
            }

            try
            {
                PerformanceCounterField.SetValue(null, performance);
                PromotionCounterField.SetValue(null, promotion);
                return true;
            }
            catch (Exception)
            {
                try
                {
                    PerformanceCounterField.SetValue(null, oldPerformance);
                    PromotionCounterField.SetValue(null, oldPromotion);
                }
                catch (Exception)
                {
                    // Best-effort rollback only; diagnostics still fail the repair closed.
                }
                return false;
            }
        }

        private static bool CaptureFailed(string diagnostic, out string error)
        {
            Interlocked.Increment(ref captureFailureCount);
            lastDiagnostic = diagnostic ?? "N11 envelope capture failed.";
            error = lastDiagnostic;
            return false;
        }

        private static void RecordInvalid(string diagnostic)
        {
            Interlocked.Increment(ref invalidSectionCount);
            lastDiagnostic = diagnostic ?? "N11 repair section failed validation.";
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

    internal static class TutorialActivityBaselinePatchHealth
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
                    failure = "N11 resolved more target methods than the frozen one-method manifest.";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown N11 patch failure";
            }
        }
    }
}
