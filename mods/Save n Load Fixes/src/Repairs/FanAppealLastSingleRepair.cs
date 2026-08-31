using System;
using System.Collections.Generic;
using System.Threading;
using SaveNLoadFixes.Persistence;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A29: preserves singles.FanAppeal_LastSingle, the seven-axis release-time
    /// comparison baseline used by later single/show satisfaction checks.
    ///
    /// Vanilla assigns the baseline to the released single's live FanAppeal list but
    /// omits the static from SavedData and never clears it on load. Repaired saves
    /// therefore store the exact seven ratios plus the source released-single ID.
    /// Load clears stale same-process RAM before vanilla reconstruction and restores a
    /// fresh list only after the complete career LoadEvent has rebuilt the target.
    ///
    /// Pre-A29 saves cannot recover the historical vector exactly. Compatibility uses
    /// one deterministic target-state synthesis: select the target's latest released
    /// single by saved ReleaseDate (stable ID tie-break), recompute the release-time
    /// FanAppeal formula against the loaded target state, copy the seven ratios, and
    /// restore the source single's pre-synthesis FanAppeal list. ReleaseData.FanAppeal
    /// is never used as a substitute.
    /// </summary>
    internal static class FanAppealLastSingleRepair
    {
        internal const int SectionVersion = 1;
        internal const int FanAxisCount = 7;

        private static int currentSourceSingleId = -1;
        private static long captureFailureCount;
        private static long staleClearCount;
        private static long releaseObservedCount;
        private static long restoredExactCount;
        private static long restoredNullCount;
        private static long legacySynthesizedCount;
        private static long legacyNoReleasedSingleCount;
        private static long invalidSectionCount;
        private static long associationMissingCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return FanAppealLastSinglePatchHealth.IsHealthy; }
        }

        internal static long CaptureFailureCount { get { return Interlocked.Read(ref captureFailureCount); } }
        internal static long StaleClearCount { get { return Interlocked.Read(ref staleClearCount); } }
        internal static long ReleaseObservedCount { get { return Interlocked.Read(ref releaseObservedCount); } }
        internal static long RestoredExactCount { get { return Interlocked.Read(ref restoredExactCount); } }
        internal static long RestoredNullCount { get { return Interlocked.Read(ref restoredNullCount); } }
        internal static long LegacySynthesizedCount { get { return Interlocked.Read(ref legacySynthesizedCount); } }
        internal static long LegacyNoReleasedSingleCount { get { return Interlocked.Read(ref legacyNoReleasedSingleCount); } }
        internal static long InvalidSectionCount { get { return Interlocked.Read(ref invalidSectionCount); } }
        internal static long AssociationMissingCount { get { return Interlocked.Read(ref associationMissingCount); } }
        internal static string LastDiagnostic { get { return lastDiagnostic; } }

        internal static bool TryCaptureForEnvelope(
            SaveManager.SavedData dataToSave,
            out FanAppealLastSingleRecordV1 record,
            out string error)
        {
            record = null;
            error = string.Empty;

            if (dataToSave == null || dataToSave.singles__Singles == null)
            {
                return CaptureFailed("A29 cannot capture from a null SavedData/singles list.", out error);
            }

            List<singles._fanAppeal> baseline = singles.FanAppeal_LastSingle;
            if (baseline == null)
            {
                record = new FanAppealLastSingleRecordV1
                {
                    has_value = false,
                    source_single_id = -1,
                    fan_appeal = new List<FanAppealRatioRecordV1>()
                };
                return true;
            }

            List<FanAppealRatioRecordV1> ratios;
            if (!TryCopyAndValidateVector(baseline, out ratios, out error))
            {
                return CaptureFailed("A29 live FanAppeal_LastSingle is not an exact seven-axis vector: " + error, out error);
            }

            int sourceId = currentSourceSingleId;
            if (sourceId < 0)
            {
                if (!TryInferSourceIdByReference(baseline, out sourceId, out error))
                {
                    return CaptureFailed("A29 could not infer the source released single: " + error, out error);
                }
            }

            if (!TargetContainsReleasedSingle(dataToSave, sourceId) ||
                singles.GetSingleByID(sourceId) == null ||
                singles.GetSingleByID(sourceId).status != singles._single._status.released)
            {
                return CaptureFailed("A29 source single ID does not resolve to a released single in both live and target DTO state.", out error);
            }

            record = new FanAppealLastSingleRecordV1
            {
                has_value = true,
                source_single_id = sourceId,
                fan_appeal = ratios
            };
            return true;
        }

        internal static void ClearBeforeVanillaSinglesLoad()
        {
            singles.FanAppeal_LastSingle = null;
            currentSourceSingleId = -1;
            Interlocked.Increment(ref staleClearCount);
            lastDiagnostic = "A29 cleared stale FanAppeal_LastSingle before vanilla singles reconstruction.";
        }

        internal static void ObserveVanillaRelease(singles._single releasedSingle)
        {
            if (releasedSingle == null || releasedSingle.id < 0 ||
                releasedSingle.status != singles._single._status.released ||
                singles.FanAppeal_LastSingle == null ||
                !object.ReferenceEquals(singles.FanAppeal_LastSingle, releasedSingle.FanAppeal))
            {
                RecordInvalid("A29 ReleaseSingle observer could not prove vanilla's source-list handoff.");
                return;
            }

            currentSourceSingleId = releasedSingle.id;
            Interlocked.Increment(ref releaseObservedCount);
            lastDiagnostic = "A29 observed vanilla release baseline handoff from single " + releasedSingle.id + ".";
        }

        internal static void RestoreAfterCareerLoad(SaveManager manager)
        {
            SaveManager.SavedData target = manager == null ? null : manager.Data;
            if (target == null)
            {
                return;
            }

            RepairEnvelopeLoadState state;
            if (!RepairEnvelopeTransport.TryGetLoadState(target, out state))
            {
                Interlocked.Increment(ref associationMissingCount);
                lastDiagnostic = "A29 failed closed because the adopted SavedData object has no repair-envelope read association.";
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
                return;
            }

            if (!state.Present || state.Envelope == null || state.Envelope.records == null ||
                state.Envelope.records.fan_appeal_last_single_version == 0)
            {
                RestoreLegacyCompatibility(target);
                return;
            }

            if (!state.Valid ||
                state.Envelope.records.fan_appeal_last_single_version != SectionVersion ||
                state.Envelope.records.fan_appeal_last_single == null)
            {
                RecordInvalid("A29 repair section is invalid, unsupported, or missing its record.");
                return;
            }

            FanAppealLastSingleRecordV1 record = state.Envelope.records.fan_appeal_last_single;
            if (!record.has_value)
            {
                if (record.source_single_id != -1 ||
                    (record.fan_appeal != null && record.fan_appeal.Count != 0))
                {
                    RecordInvalid("A29 null baseline record contains a source ID or fan ratios.");
                    return;
                }

                singles.FanAppeal_LastSingle = null;
                currentSourceSingleId = -1;
                Interlocked.Increment(ref restoredNullCount);
                lastDiagnostic = "A29 restored an exact null FanAppeal_LastSingle baseline.";
                return;
            }

            if (record.source_single_id < 0 ||
                !TargetContainsReleasedSingle(target, record.source_single_id))
            {
                RecordInvalid("A29 exact record source ID is not a released single in the target save.");
                return;
            }

            singles._single source = singles.GetSingleByID(record.source_single_id);
            if (source == null || source.status != singles._single._status.released)
            {
                RecordInvalid("A29 exact record source ID did not rebind to one loaded released single.");
                return;
            }

            List<singles._fanAppeal> restored;
            string vectorError;
            if (!TryBuildFreshVector(record.fan_appeal, out restored, out vectorError))
            {
                RecordInvalid("A29 exact seven-axis vector is invalid: " + vectorError);
                return;
            }

            singles.FanAppeal_LastSingle = restored;
            currentSourceSingleId = record.source_single_id;
            Interlocked.Increment(ref restoredExactCount);
            lastDiagnostic = "A29 restored exact FanAppeal_LastSingle from source single " +
                record.source_single_id + " for SNLF checkpoint " + state.Envelope.checkpoint_id + ".";
        }

        private static void RestoreLegacyCompatibility(SaveManager.SavedData target)
        {
            int sourceId;
            string sourceError;
            if (!TrySelectLegacySourceSingle(target, out sourceId, out sourceError))
            {
                RecordInvalid("A29 legacy source selection failed: " + sourceError);
                return;
            }

            if (sourceId < 0)
            {
                singles.FanAppeal_LastSingle = null;
                currentSourceSingleId = -1;
                Interlocked.Increment(ref legacyNoReleasedSingleCount);
                lastDiagnostic = "A29 pre-fix target contains no released single; compatibility baseline remains null.";
                return;
            }

            singles._single source = singles.GetSingleByID(sourceId);
            if (source == null || source.status != singles._single._status.released)
            {
                RecordInvalid("A29 legacy source ID did not rebind to one loaded released single.");
                return;
            }

            List<singles._fanAppeal> synthesized;
            string synthError;
            if (!TrySynthesizeFromTargetState(source, out synthesized, out synthError))
            {
                RecordInvalid("A29 deterministic target-state synthesis failed: " + synthError);
                return;
            }

            singles.FanAppeal_LastSingle = synthesized;
            currentSourceSingleId = sourceId;
            Interlocked.Increment(ref legacySynthesizedCount);
            lastDiagnostic = "A29 synthesized a deterministic pre-fix comparison baseline from loaded target single " +
                sourceId + "; it is compatibility state, not claimed recovered history.";
        }

        private static bool TrySynthesizeFromTargetState(
            singles._single source,
            out List<singles._fanAppeal> synthesized,
            out string error)
        {
            synthesized = null;
            error = string.Empty;

            if (source == null || source.FanAppeal == null)
            {
                error = "source single or its FanAppeal list is null";
                return false;
            }

            List<singles._fanAppeal> original = source.FanAppeal;
            try
            {
                source.FanAppeal = new List<singles._fanAppeal>();
                source.RecalcFanAppeal(source.GetSenbatsuStats(), true);

                List<FanAppealRatioRecordV1> copied;
                if (!TryCopyAndValidateVector(source.FanAppeal, out copied, out error))
                {
                    return false;
                }

                return TryBuildFreshVector(copied, out synthesized, out error);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
            finally
            {
                source.FanAppeal = original;
            }
        }

        private static bool TrySelectLegacySourceSingle(
            SaveManager.SavedData target,
            out int sourceId,
            out string error)
        {
            sourceId = -1;
            error = string.Empty;

            if (target == null || target.singles__Singles == null)
            {
                error = "target singles list is null";
                return false;
            }

            bool found = false;
            DateTime latestDate = default(DateTime);
            int latestId = -1;
            int lastReleasedByOrder = -1;
            bool allReleasedDatesValid = true;

            for (int index = 0; index < target.singles__Singles.Count; index++)
            {
                singles.SinglesData row = target.singles__Singles[index];
                if (row == null || row.id < 0)
                {
                    error = "target singles list contains a null or invalid row";
                    return false;
                }

                if (row.status != singles._single._status.released)
                {
                    continue;
                }

                lastReleasedByOrder = row.id;
                DateTime releaseDate;
                if (string.IsNullOrEmpty(row.ReleaseDate))
                {
                    allReleasedDatesValid = false;
                    continue;
                }

                try
                {
                    releaseDate = ExtensionMethods.ToDateTime(row.ReleaseDate);
                }
                catch
                {
                    allReleasedDatesValid = false;
                    continue;
                }

                if (!found || releaseDate > latestDate ||
                    (releaseDate == latestDate && row.id > latestId))
                {
                    latestDate = releaseDate;
                    latestId = row.id;
                    found = true;
                }
            }

            if (lastReleasedByOrder < 0)
            {
                return true;
            }

            // Valid saved release dates are the strongest target-state chronology witness.
            // If any released row lacks one, fall back to vanilla's deterministic loaded
            // list ordering instead of mixing complete and incomplete chronology evidence.
            sourceId = allReleasedDatesValid && found ? latestId : lastReleasedByOrder;
            return true;
        }

        private static bool TryInferSourceIdByReference(
            List<singles._fanAppeal> baseline,
            out int sourceId,
            out string error)
        {
            sourceId = -1;
            error = string.Empty;
            if (singles.Singles == null)
            {
                error = "live singles list is null";
                return false;
            }

            int matches = 0;
            for (int index = 0; index < singles.Singles.Count; index++)
            {
                singles._single single = singles.Singles[index];
                if (single != null &&
                    single.status == singles._single._status.released &&
                    object.ReferenceEquals(single.FanAppeal, baseline))
                {
                    sourceId = single.id;
                    matches++;
                }
            }

            if (matches != 1)
            {
                error = "expected exactly one released single to own the vanilla baseline list; observed " + matches;
                sourceId = -1;
                return false;
            }

            return true;
        }

        private static bool TargetContainsReleasedSingle(SaveManager.SavedData target, int id)
        {
            if (target == null || target.singles__Singles == null || id < 0)
            {
                return false;
            }

            int matches = 0;
            for (int index = 0; index < target.singles__Singles.Count; index++)
            {
                singles.SinglesData row = target.singles__Singles[index];
                if (row != null && row.id == id && row.status == singles._single._status.released)
                {
                    matches++;
                }
            }

            return matches == 1;
        }

        private static bool TryCopyAndValidateVector(
            List<singles._fanAppeal> source,
            out List<FanAppealRatioRecordV1> copied,
            out string error)
        {
            copied = new List<FanAppealRatioRecordV1>();
            error = string.Empty;
            if (source == null || source.Count != FanAxisCount)
            {
                error = "vector must contain exactly seven rows";
                return false;
            }

            Dictionary<int, float> byType = new Dictionary<int, float>();
            for (int index = 0; index < source.Count; index++)
            {
                singles._fanAppeal row = source[index];
                int type = row == null ? -1 : (int)row.type;
                if (row == null || type < 0 || type >= FanAxisCount ||
                    float.IsNaN(row.ratio) || float.IsInfinity(row.ratio) ||
                    byType.ContainsKey(type))
                {
                    error = "vector contains a null, duplicate, invalid type, or non-finite ratio";
                    return false;
                }

                byType.Add(type, row.ratio);
            }

            for (int type = 0; type < FanAxisCount; type++)
            {
                float ratio;
                if (!byType.TryGetValue(type, out ratio))
                {
                    error = "vector is missing one of the seven resources.fanType axes";
                    return false;
                }

                copied.Add(new FanAppealRatioRecordV1 { fan_type = type, ratio = ratio });
            }

            return true;
        }

        private static bool TryBuildFreshVector(
            List<FanAppealRatioRecordV1> records,
            out List<singles._fanAppeal> vector,
            out string error)
        {
            vector = null;
            error = string.Empty;
            if (records == null || records.Count != FanAxisCount)
            {
                error = "persisted vector must contain exactly seven rows";
                return false;
            }

            float[] ratios = new float[FanAxisCount];
            bool[] seen = new bool[FanAxisCount];
            for (int index = 0; index < records.Count; index++)
            {
                FanAppealRatioRecordV1 row = records[index];
                if (row == null || row.fan_type < 0 || row.fan_type >= FanAxisCount ||
                    seen[row.fan_type] || float.IsNaN(row.ratio) || float.IsInfinity(row.ratio))
                {
                    error = "persisted vector contains a null, duplicate, invalid type, or non-finite ratio";
                    return false;
                }

                seen[row.fan_type] = true;
                ratios[row.fan_type] = row.ratio;
            }

            List<singles._fanAppeal> built = new List<singles._fanAppeal>(FanAxisCount);
            for (int type = 0; type < FanAxisCount; type++)
            {
                if (!seen[type])
                {
                    error = "persisted vector is missing one of the seven resources.fanType axes";
                    return false;
                }

                built.Add(new singles._fanAppeal
                {
                    type = (resources.fanType)type,
                    ratio = ratios[type]
                });
            }

            vector = built;
            return true;
        }

        private static bool CaptureFailed(string diagnostic, out string error)
        {
            error = diagnostic ?? "unknown A29 capture failure";
            Interlocked.Increment(ref captureFailureCount);
            lastDiagnostic = error;
            return false;
        }

        private static void RecordInvalid(string diagnostic)
        {
            Interlocked.Increment(ref invalidSectionCount);
            lastDiagnostic = diagnostic ?? "unknown A29 repair failure";
            Debug.LogError(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
        }
    }

    internal static class FanAppealLastSinglePatchHealth
    {
        internal const int ExpectedTargetMethodCount = 4;

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

        internal static string Failure
        {
            get { lock (Sync) { return failure; } }
        }

        internal static void ReportTargetResolved()
        {
            lock (Sync)
            {
                resolvedTargetMethodCount++;
                if (resolvedTargetMethodCount > ExpectedTargetMethodCount)
                {
                    failure = "A29 resolved more patch targets than the frozen four-method manifest.";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown A29 patch failure";
            }
        }
    }
}
