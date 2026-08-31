using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using SaveNLoadFixes.Persistence;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs A28 / regression 40: Awards.TempNominations is the authoritative
    /// award-eve pending slate, but vanilla omits it from SavedData and clears it
    /// in Awards.LoadFunction()->Reset().
    ///
    /// Current-format checkpoints persist only the minimal occurrence row:
    /// award type, year, nullable nominee idol ID, and nullable nominee single ID.
    /// Restore constructs fresh _award objects only after vanilla Awards.LoadFunction
    /// has already reached the source-correct idol/single rebinding seam.
    ///
    /// Pre-A28 compatibility is deliberately conservative. Only an award-eve target
    /// whose saved SpeechData rows prove the pending award TYPES is eligible. Speech
    /// giver/thanks/target identities are never treated as nominee identity. Individual
    /// nominees are selected deterministically from target-loaded state; best_single
    /// uses a stable ID tie-break instead of vanilla's 50/50 nomination RNG.
    /// </summary>
    internal static class AwardTempNominationRepair
    {
        internal const int SectionVersion = 1;

        private static long capturedCheckpointCount;
        private static long capturedNominationCount;
        private static long staleClearCount;
        private static long restoredExactLoadCount;
        private static long restoredExactNominationCount;
        private static long restoredEmptyLoadCount;
        private static long legacyReconstructedLoadCount;
        private static long legacyReconstructedNominationCount;
        private static long legacyNoWitnessCount;
        private static long legacyNullableNomineeCount;
        private static long invalidSectionCount;
        private static long associationMissingCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return AwardTempNominationPatchHealth.IsHealthy; }
        }

        internal static long CapturedCheckpointCount { get { return Interlocked.Read(ref capturedCheckpointCount); } }
        internal static long CapturedNominationCount { get { return Interlocked.Read(ref capturedNominationCount); } }
        internal static long StaleClearCount { get { return Interlocked.Read(ref staleClearCount); } }
        internal static long RestoredExactLoadCount { get { return Interlocked.Read(ref restoredExactLoadCount); } }
        internal static long RestoredExactNominationCount { get { return Interlocked.Read(ref restoredExactNominationCount); } }
        internal static long RestoredEmptyLoadCount { get { return Interlocked.Read(ref restoredEmptyLoadCount); } }
        internal static long LegacyReconstructedLoadCount { get { return Interlocked.Read(ref legacyReconstructedLoadCount); } }
        internal static long LegacyReconstructedNominationCount { get { return Interlocked.Read(ref legacyReconstructedNominationCount); } }
        internal static long LegacyNoWitnessCount { get { return Interlocked.Read(ref legacyNoWitnessCount); } }
        internal static long LegacyNullableNomineeCount { get { return Interlocked.Read(ref legacyNullableNomineeCount); } }
        internal static long InvalidSectionCount { get { return Interlocked.Read(ref invalidSectionCount); } }
        internal static long AssociationMissingCount { get { return Interlocked.Read(ref associationMissingCount); } }
        internal static string LastDiagnostic { get { return lastDiagnostic; } }

        internal static bool TryCaptureForEnvelope(
            SaveManager.SavedData dataToSave,
            out List<AwardTempNominationRecordV1> records,
            out string error)
        {
            records = new List<AwardTempNominationRecordV1>();
            error = string.Empty;

            if (!IsImplemented)
            {
                return CaptureFailed("A28 load/restore patch is not healthy.", out error);
            }
            if (dataToSave == null || dataToSave.data_girls__Girls == null || dataToSave.singles__Singles == null)
            {
                return CaptureFailed("A28 cannot capture without the exact populated SavedData idol/single graphs.", out error);
            }
            if (Awards.TempNominations == null)
            {
                return CaptureFailed("A28 live Awards.TempNominations registry is null.", out error);
            }

            DateTime targetDate;
            if (!TryGetTargetDate(dataToSave, out targetDate, out error))
            {
                return CaptureFailed("A28 target save date is invalid: " + error, out error);
            }

            Dictionary<int, int> savedGirlCounts = BuildSavedGirlIdCounts(dataToSave.data_girls__Girls);
            Dictionary<int, int> savedSingleCounts = BuildSavedSingleIdCounts(dataToSave.singles__Singles);
            HashSet<int> seenTypes = new HashSet<int>();

            for (int index = 0; index < Awards.TempNominations.Count; index++)
            {
                Awards._award live = Awards.TempNominations[index];
                if (live == null || !IsSupportedPendingType(live.Type) || !seenTypes.Add((int)live.Type))
                {
                    return CaptureFailed("A28 live temporary slate contains a null, unsupported, or duplicate award type.", out error);
                }
                if (!live.IsNomination)
                {
                    return CaptureFailed("A28 live temporary slate is no longer in the pre-result nomination phase.", out error);
                }
                if (live.Year != targetDate.Year)
                {
                    return CaptureFailed("A28 live temporary nomination year does not match the exact target save year.", out error);
                }

                int girlId = live.Girl == null ? -1 : live.Girl.id;
                int singleId = live.Single == null ? -1 : live.Single.id;
                if (!ValidateReferenceShape(live.Type, girlId, singleId, out error))
                {
                    return CaptureFailed("A28 live temporary nomination has an invalid nominee shape: " + error, out error);
                }
                if (girlId >= 0 && !OccursExactlyOnce(savedGirlCounts, girlId))
                {
                    return CaptureFailed("A28 live nominee idol does not occur exactly once in the target SavedData roster.", out error);
                }
                if (singleId >= 0 && !OccursExactlyOnce(savedSingleCounts, singleId))
                {
                    return CaptureFailed("A28 live nominee single does not occur exactly once in the target SavedData singles list.", out error);
                }

                records.Add(
                    new AwardTempNominationRecordV1
                    {
                        award_type = (int)live.Type,
                        year = live.Year,
                        girl_id = girlId,
                        single_id = singleId
                    });
            }

            Interlocked.Increment(ref capturedCheckpointCount);
            Interlocked.Add(ref capturedNominationCount, records.Count);
            SetDiagnostic(
                "A28 captured " + records.Count.ToString(CultureInfo.InvariantCulture) +
                " pending award nomination row(s) for the exact save request.");
            return true;
        }

        internal static void ClearBeforeVanillaAwardsLoad()
        {
            int count = Awards.TempNominations == null ? 0 : Awards.TempNominations.Count;
            if (Awards.TempNominations == null)
            {
                Awards.TempNominations = new List<Awards._award>();
            }
            else
            {
                Awards.TempNominations.Clear();
            }

            if (count > 0)
            {
                Interlocked.Add(ref staleClearCount, count);
            }
            SetDiagnostic("A28 cleared discarded-timeline TempNominations before vanilla Awards.LoadFunction().");
        }

        internal static void RestoreAfterVanillaAwardsLoad()
        {
            if (!IsImplemented)
            {
                SetDiagnostic("A28 restore is not healthy; no temporary award slate was reconstructed.");
                return;
            }

            SaveManager.SavedData target = GetTargetSavedData();
            if (target == null)
            {
                Interlocked.Increment(ref associationMissingCount);
                SetDiagnostic("A28 target SavedData is unavailable after Awards.LoadFunction().");
                return;
            }

            RepairEnvelopeLoadState state;
            if (!RepairEnvelopeTransport.TryGetLoadState(target, out state))
            {
                Interlocked.Increment(ref associationMissingCount);
                SetDiagnostic("A28 target SavedData has no repair-envelope read association.");
                return;
            }

            if (!state.Present)
            {
                RestoreLegacyCompatibility(target, "pre-envelope save");
                return;
            }
            if (!state.Valid || state.Envelope == null || state.Envelope.records == null)
            {
                RecordInvalid("A28 loaded a present but invalid repair envelope; pending award state was not treated as empty.");
                return;
            }

            RepairEnvelopeRecordsV1 envelopeRecords = state.Envelope.records;
            if (envelopeRecords.award_temp_nominations_version == 0)
            {
                RestoreLegacyCompatibility(target, "pre-A28 envelope");
                return;
            }
            if (envelopeRecords.award_temp_nominations_version != SectionVersion ||
                envelopeRecords.award_temp_nominations == null)
            {
                RecordInvalid("A28 repair section is invalid or unsupported.");
                return;
            }

            List<Awards._award> restored;
            string error;
            if (!TryBuildExactRows(target, envelopeRecords.award_temp_nominations, out restored, out error))
            {
                RecordInvalid("A28 exact repair section failed validation: " + error);
                return;
            }

            Awards.TempNominations.Clear();
            Awards.TempNominations.AddRange(restored);
            Interlocked.Increment(ref restoredExactLoadCount);
            Interlocked.Add(ref restoredExactNominationCount, restored.Count);
            if (restored.Count == 0)
            {
                Interlocked.Increment(ref restoredEmptyLoadCount);
                SetDiagnostic("A28 restored an authoritative empty temporary-award-nomination section.");
            }
            else
            {
                SetDiagnostic(
                    "A28 restored " + restored.Count.ToString(CultureInfo.InvariantCulture) +
                    " exact pending award row(s) for SNLF checkpoint " + state.Envelope.checkpoint_id + ".");
            }
        }

        private static bool TryBuildExactRows(
            SaveManager.SavedData target,
            List<AwardTempNominationRecordV1> records,
            out List<Awards._award> restored,
            out string error)
        {
            restored = new List<Awards._award>();
            error = string.Empty;
            if (target == null || records == null || target.data_girls__Girls == null || target.singles__Singles == null)
            {
                error = "target save graphs or record list are null";
                return false;
            }

            DateTime targetDate;
            if (!TryGetTargetDate(target, out targetDate, out error))
            {
                return false;
            }

            Dictionary<int, int> savedGirlCounts = BuildSavedGirlIdCounts(target.data_girls__Girls);
            Dictionary<int, int> savedSingleCounts = BuildSavedSingleIdCounts(target.singles__Singles);
            HashSet<int> seenTypes = new HashSet<int>();

            for (int index = 0; index < records.Count; index++)
            {
                AwardTempNominationRecordV1 record = records[index];
                if (record == null || !IsSupportedPendingTypeValue(record.award_type) || !seenTypes.Add(record.award_type))
                {
                    error = "record list contains a null, unsupported, or duplicate award type";
                    return false;
                }
                if (record.year != targetDate.Year)
                {
                    error = "record year does not match the target save year";
                    return false;
                }

                Awards._type type = (Awards._type)record.award_type;
                if (!ValidateReferenceShape(type, record.girl_id, record.single_id, out error))
                {
                    return false;
                }

                data_girls.girls girl = null;
                if (record.girl_id >= 0)
                {
                    if (!OccursExactlyOnce(savedGirlCounts, record.girl_id))
                    {
                        error = "nominee idol ID does not occur exactly once in the target save";
                        return false;
                    }
                    girl = data_girls.GetGirlByID(record.girl_id);
                    if (girl == null || girl.id != record.girl_id)
                    {
                        error = "nominee idol ID did not rebind to the freshly loaded idol object";
                        return false;
                    }
                }

                singles._single single = null;
                if (record.single_id >= 0)
                {
                    if (!OccursExactlyOnce(savedSingleCounts, record.single_id))
                    {
                        error = "nominee single ID does not occur exactly once in the target save";
                        return false;
                    }
                    single = singles.GetSingleByID(record.single_id);
                    if (single == null || single.id != record.single_id)
                    {
                        error = "nominee single ID did not rebind to the freshly loaded single object";
                        return false;
                    }
                }

                restored.Add(
                    new Awards._award
                    {
                        Type = type,
                        Year = record.year,
                        Girl = girl,
                        Single = single,
                        IsNomination = true
                    });
            }

            return true;
        }

        private static void RestoreLegacyCompatibility(SaveManager.SavedData target, string sourceKind)
        {
            DateTime targetDate;
            string error;
            if (!TryGetTargetDate(target, out targetDate, out error))
            {
                RecordInvalid("A28 legacy target date is invalid: " + error);
                return;
            }

            // Saved speeches are a valid TYPE witness only inside the source-proven
            // one-day-before-awards gap. Reset_After_Awards leaves speeches alive, so
            // using them on arbitrary dates would resurrect last year's slate.
            if (!IsAwardEve(targetDate) || target.Awards__Speeches == null || target.Awards__Speeches.Count == 0)
            {
                Interlocked.Increment(ref legacyNoWitnessCount);
                SetDiagnostic(
                    "A28 " + sourceKind + " has no source-proven award-eve speech-type witness; no pending slate was fabricated.");
                return;
            }

            Dictionary<int, int> savedGirlCounts = BuildSavedGirlIdCounts(target.data_girls__Girls);
            Dictionary<int, int> savedSingleCounts = BuildSavedSingleIdCounts(target.singles__Singles);
            HashSet<int> seenTypes = new HashSet<int>();
            List<Awards._award> reconstructed = new List<Awards._award>();

            for (int index = 0; index < target.Awards__Speeches.Count; index++)
            {
                Awards.SpeechData speech = target.Awards__Speeches[index];
                int typeValue = speech == null ? -1 : (int)speech.Type;
                if (speech == null || !IsSupportedPendingTypeValue(typeValue) || !seenTypes.Add(typeValue))
                {
                    RecordInvalid("A28 legacy award-eve speech witness contains a null, unsupported, or duplicate award type.");
                    return;
                }

                Awards._type type = (Awards._type)typeValue;
                data_girls.girls girl = null;
                singles._single single = null;

                if (RequiresGirl(type))
                {
                    girl = SelectLegacyGirl(type, targetDate);
                    if (girl != null && !OccursExactlyOnce(savedGirlCounts, girl.id))
                    {
                        RecordInvalid("A28 deterministic legacy nominee idol is not unique in the target save.");
                        return;
                    }
                    if (girl == null)
                    {
                        Interlocked.Increment(ref legacyNullableNomineeCount);
                    }
                }
                else if (RequiresSingle(type))
                {
                    single = SelectLegacySingle(targetDate);
                    if (single != null && !OccursExactlyOnce(savedSingleCounts, single.id))
                    {
                        RecordInvalid("A28 deterministic legacy nominee single is not unique in the target save.");
                        return;
                    }
                    if (single == null)
                    {
                        Interlocked.Increment(ref legacyNullableNomineeCount);
                    }
                }

                // Critically, SpeechData.Girl is the mutable SPEECH GIVER and is never
                // consumed here as nomination identity.
                reconstructed.Add(
                    new Awards._award
                    {
                        Type = type,
                        Year = targetDate.Year,
                        Girl = girl,
                        Single = single,
                        IsNomination = true
                    });
            }

            Awards.TempNominations.Clear();
            Awards.TempNominations.AddRange(reconstructed);
            Interlocked.Increment(ref legacyReconstructedLoadCount);
            Interlocked.Add(ref legacyReconstructedNominationCount, reconstructed.Count);
            SetDiagnostic(
                "A28 deterministically reconstructed " + reconstructed.Count.ToString(CultureInfo.InvariantCulture) +
                " legacy pending award row(s) from award-eve speech TYPES only; nominee identity was rebuilt from target state.");
        }

        private static data_girls.girls SelectLegacyGirl(Awards._type type, DateTime targetDate)
        {
            if (!RequiresGirl(type) || data_girls.girl == null)
            {
                return null;
            }

            DateTime threshold = GetNextAwardDate(targetDate).AddYears(-1).AddDays(-7.0);
            data_girls.girls selected = null;
            for (int index = 0; index < data_girls.girl.Count; index++)
            {
                data_girls.girls candidate = data_girls.girl[index];
                if (candidate == null || candidate.id < 0 || candidate.status == data_girls._status.graduated)
                {
                    continue;
                }
                if (type == Awards._type.best_debut_idol && candidate.Hiring_Date < threshold)
                {
                    continue;
                }

                if (selected == null || IsLegacyGirlAtLeastAsStrong(type, candidate, selected))
                {
                    selected = candidate;
                }
            }
            return selected;
        }

        private static bool IsLegacyGirlAtLeastAsStrong(
            Awards._type type,
            data_girls.girls candidate,
            data_girls.girls selected)
        {
            if (candidate == null || selected == null)
            {
                return false;
            }
            if (type == Awards._type.gravure_queen)
            {
                return candidate.Stats.GetYearly().Proposals_Sexy >= selected.Stats.GetYearly().Proposals_Sexy;
            }
            if (type == Awards._type.fashion_icon)
            {
                return candidate.Stats.GetYearly().Proposals_Pretty >= selected.Stats.GetYearly().Proposals_Pretty;
            }
            if (type == Awards._type.variety_queen)
            {
                return candidate.Stats.GetYearly().Variety_Appearences >= selected.Stats.GetYearly().Variety_Appearences;
            }
            if (type == Awards._type.best_debut_idol)
            {
                return candidate.GetFameLevel() >= selected.GetFameLevel();
            }
            return false;
        }

        private static singles._single SelectLegacySingle(DateTime targetDate)
        {
            if (singles.Singles == null)
            {
                return null;
            }

            singles._single selected = null;
            for (int index = 0; index < singles.Singles.Count; index++)
            {
                singles._single candidate = singles.Singles[index];
                if (candidate == null || candidate.id < 0 || candidate.status != singles._single._status.released ||
                    candidate.ReleaseData == null || (targetDate - candidate.ReleaseData.ReleaseDate).Days > 365 ||
                    !candidate.ReleaseData.MostPopular_Choreo || !candidate.ReleaseData.MostPopular_Genre ||
                    !candidate.ReleaseData.MostPopular_Lyrics)
                {
                    continue;
                }

                if (selected == null ||
                    candidate.ReleaseData.Chart_Position > selected.ReleaseData.Chart_Position ||
                    (candidate.ReleaseData.Chart_Position == selected.ReleaseData.Chart_Position && candidate.id < selected.id))
                {
                    // The lower stable ID is the deterministic compatibility tie-break.
                    // Do NOT call Awards.GetNominatedSingle(): vanilla consumes RNG on ties.
                    selected = candidate;
                }
            }
            return selected;
        }

        private static bool ValidateReferenceShape(
            Awards._type type,
            int girlId,
            int singleId,
            out string error)
        {
            error = string.Empty;
            if (!IsSupportedPendingType(type))
            {
                error = "unsupported award type";
                return false;
            }

            if (RequiresGirl(type))
            {
                if (singleId != -1 || girlId < 0)
                {
                    error = "individual award must contain only its nominee idol ID";
                    return false;
                }
                return girlId >= -1;
            }
            if (RequiresSingle(type))
            {
                if (girlId != -1 || singleId < 0)
                {
                    error = "best_single must contain only its nominee single ID";
                    return false;
                }
                return singleId >= -1;
            }

            if (girlId != -1 || singleId != -1)
            {
                error = "group award cannot contain idol/single nominee identity";
                return false;
            }
            return true;
        }

        private static bool IsSupportedPendingTypeValue(int value)
        {
            return value >= (int)Awards._type.most_performances &&
                value <= (int)Awards._type.most_prolific_group &&
                IsSupportedPendingType((Awards._type)value);
        }

        private static bool IsSupportedPendingType(Awards._type type)
        {
            return type == Awards._type.best_concert ||
                type == Awards._type.best_employer ||
                type == Awards._type.best_single ||
                type == Awards._type.most_performances ||
                type == Awards._type.most_prolific_group ||
                type == Awards._type.most_promotions ||
                type == Awards._type.fashion_icon ||
                type == Awards._type.gravure_queen ||
                type == Awards._type.variety_queen ||
                type == Awards._type.best_debut_idol;
        }

        private static bool RequiresGirl(Awards._type type)
        {
            return type == Awards._type.fashion_icon ||
                type == Awards._type.gravure_queen ||
                type == Awards._type.variety_queen ||
                type == Awards._type.best_debut_idol;
        }

        private static bool RequiresSingle(Awards._type type)
        {
            return type == Awards._type.best_single;
        }

        private static bool IsAwardEve(DateTime targetDate)
        {
            DateTime awardEve = GetNextAwardDate(targetDate).AddDays(-1.0);
            return awardEve.Year == targetDate.Year &&
                awardEve.Month == targetDate.Month &&
                awardEve.Day == targetDate.Day;
        }

        private static DateTime GetNextAwardDate(DateTime current)
        {
            bool beforeAnniversary = current.Month < staticVars.StartDate.Month ||
                (current.Month == staticVars.StartDate.Month && current.Day < staticVars.StartDate.Day);
            return beforeAnniversary
                ? new DateTime(current.Year, staticVars.StartDate.Month, staticVars.StartDate.Day)
                : new DateTime(current.Year + 1, staticVars.StartDate.Month, staticVars.StartDate.Day);
        }

        private static bool TryGetTargetDate(
            SaveManager.SavedData target,
            out DateTime targetDate,
            out string error)
        {
            targetDate = default(DateTime);
            error = string.Empty;
            if (target == null || string.IsNullOrEmpty(target.staticVars__dateTime))
            {
                error = "staticVars__dateTime is missing";
                return false;
            }
            try
            {
                targetDate = ExtensionMethods.ToDateTime(target.staticVars__dateTime);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static Dictionary<int, int> BuildSavedGirlIdCounts(List<data_girls.GirlData> rows)
        {
            Dictionary<int, int> counts = new Dictionary<int, int>();
            if (rows == null)
            {
                return counts;
            }
            for (int index = 0; index < rows.Count; index++)
            {
                data_girls.GirlData row = rows[index];
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

        private static Dictionary<int, int> BuildSavedSingleIdCounts(List<singles.SinglesData> rows)
        {
            Dictionary<int, int> counts = new Dictionary<int, int>();
            if (rows == null)
            {
                return counts;
            }
            for (int index = 0; index < rows.Count; index++)
            {
                singles.SinglesData row = rows[index];
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

        private static bool OccursExactlyOnce(Dictionary<int, int> counts, int id)
        {
            int count;
            return id >= 0 && counts != null && counts.TryGetValue(id, out count) && count == 1;
        }

        private static SaveManager.SavedData GetTargetSavedData()
        {
            try
            {
                mainScript main = Camera.main == null ? null : Camera.main.GetComponent<mainScript>();
                return main == null ? null : main.GetSavedData();
            }
            catch (Exception exception)
            {
                SetDiagnostic("A28 could not resolve the target SavedData: " + exception.Message);
                return null;
            }
        }

        private static bool CaptureFailed(string diagnostic, out string error)
        {
            error = diagnostic ?? "unknown A28 capture failure";
            SetDiagnostic(error);
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
            lastDiagnostic = value ?? string.Empty;
        }
    }

    internal static class AwardTempNominationPatchHealth
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
                    return resolvedTargetMethodCount == ExpectedTargetMethodCount && string.IsNullOrEmpty(failure);
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
                    failure = "A28 resolved more Awards.LoadFunction targets than the frozen one-method manifest.";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown A28 patch failure";
            }
        }
    }
}
