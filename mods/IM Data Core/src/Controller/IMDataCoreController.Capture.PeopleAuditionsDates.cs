using System;
using System.Collections.Generic;
using System.Globalization;

namespace IMDataCore
{
    /// <summary>
    /// Narrow thread-scoped provenance for higher-level hire owners that synchronously
    /// enter data_girls.Hire. The value is observation context only and is never
    /// persisted as current gameplay state.
    /// </summary>
    internal static class IdolHireProvenanceContext
    {
        [ThreadStatic]
        private static string current;

        internal static string Enter(string provenance)
        {
            string previous = current;
            current = provenance ?? string.Empty;
            return previous;
        }

        internal static void Restore(string previous)
        {
            current = previous ?? string.Empty;
        }

        internal static string Current
        {
            get { return current ?? string.Empty; }
        }
    }

    internal sealed partial class IMDataCoreController
    {
        private const string AuditionOccurrencePrefix = "a:";
        private const string GenericDateOccurrencePrefix = "d:";
        private const string HireProvenanceAudition = "audition";
        private const string HireProvenanceGraduationSuccessor = "graduation_successor";
        private const string HireProvenanceUniqueStory = "unique_story";
        private const string HireProvenanceGeneric = "generic";

        private readonly Dictionary<Auditions.data, AuditionRuntimeContext> activeAuditionRunsByData =
            new Dictionary<Auditions.data, AuditionRuntimeContext>();
        private readonly Dictionary<int, GenericDateRuntimeContext> activeGenericDatesByIdol =
            new Dictionary<int, GenericDateRuntimeContext>();

        private static string CreateAuditionOccurrenceId()
        {
            return AuditionOccurrencePrefix + Guid.NewGuid().ToString("N");
        }

        private static string CreateGenericDateOccurrenceId()
        {
            return GenericDateOccurrencePrefix + Guid.NewGuid().ToString("N");
        }

        private void ResetPeopleAuditionDateRuntimeStateLocked()
        {
            activeAuditionRunsByData.Clear();
            activeGenericDatesByIdol.Clear();
        }

        private static string ResolveHireProvenance(data_girls.girls idol, string scopedProvenance)
        {
            if (!string.IsNullOrEmpty(scopedProvenance))
            {
                return scopedProvenance;
            }

            if (idol != null && idol.Type != data_girls.girls._type.NORMAL)
            {
                return HireProvenanceUniqueStory;
            }

            return HireProvenanceGeneric;
        }

        private static IdolHireProfilePayload BuildIdolHireProfilePayload(data_girls.girls idol)
        {
            IdolHireProfilePayload profile = new IdolHireProfilePayload();
            if (idol == null)
            {
                return profile;
            }

            profile.first_name = idol.firstName ?? string.Empty;
            profile.last_name = idol.lastName ?? string.Empty;
            profile.nickname = idol.nickname ?? string.Empty;
            profile.sexuality = idol.sexuality.ToString().ToLowerInvariant();
            profile.trait = idol.trait.ToString().ToLowerInvariant();
            profile.birthday = idol.birthday == default(DateTime)
                ? string.Empty
                : CoreDateTimeUtility.ToRoundTripString(idol.birthday);
            profile.peak_age = idol.peakAge;

            if (idol.parameters != null)
            {
                for (int i = 0; i < idol.parameters.Count; i++)
                {
                    data_girls.girls.param parameter = idol.parameters[i];
                    if (parameter == null)
                    {
                        continue;
                    }

                    // Read the backing fields directly. GetPotential() is intentionally
                    // forbidden here because it can consume RNG and mutate gameplay.
                    profile.parameters.Add(new IdolParameterBaselinePayload
                    {
                        parameter_type = parameter.type.ToString().ToLowerInvariant(),
                        value = parameter._val,
                        potential = parameter.potential,
                        potential_materialized = parameter.potential != 0
                    });
                }
            }

            return profile;
        }

        private static AuditionCandidateSummaryPayload BuildAuditionCandidateSummary(
            Auditions.data._girl candidate,
            string candidateId,
            int ordinal,
            bool includeHireOutcome)
        {
            data_girls.girls idol = candidate != null ? candidate.girl : null;
            bool hired = includeHireOutcome &&
                idol != null &&
                data_girls.girl != null &&
                data_girls.girl.Contains(idol);

            return new AuditionCandidateSummaryPayload
            {
                candidate_id = candidateId ?? string.Empty,
                candidate_ordinal = ordinal,
                generated_idol_id = idol != null ? idol.id : CoreConstants.InvalidIdValue,
                rarity = candidate != null ? candidate.type.ToString().ToLowerInvariant() : string.Empty,
                profile = BuildIdolHireProfilePayload(idol),
                hired = hired,
                resulting_idol_id = hired && idol != null ? idol.id : CoreConstants.InvalidIdValue
            };
        }

        private static List<AuditionCandidateSummaryPayload> BuildAuditionCandidateSummaries(
            Auditions.data auditionData,
            AuditionRuntimeContext context,
            bool includeHireOutcome)
        {
            List<AuditionCandidateSummaryPayload> result = new List<AuditionCandidateSummaryPayload>();
            if (auditionData == null || auditionData.Girls == null || context == null)
            {
                return result;
            }

            for (int i = 0; i < auditionData.Girls.Count; i++)
            {
                Auditions.data._girl candidate = auditionData.Girls[i];
                string candidateId = context.GetOrCreateCandidateId(i);
                result.Add(BuildAuditionCandidateSummary(candidate, candidateId, i, includeHireOutcome));
            }

            return result;
        }

        internal void CaptureAuditionCandidatesGenerated(Auditions.data auditionData)
        {
            if (auditionData == null)
            {
                return;
            }

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                AuditionRuntimeContext context;
                if (!activeAuditionRunsByData.TryGetValue(auditionData, out context) || context == null)
                {
                    // Direct/debug generation without a committed GenerateAudition start is
                    // not presented as a historical audition birth.
                    return;
                }

                AuditionCandidatesGeneratedEventPayload payload = new AuditionCandidatesGeneratedEventPayload
                {
                    audition_occurrence_id = context.OccurrenceId,
                    audition_type = context.AuditionType,
                    generated_candidate_count = auditionData.Girls != null ? auditionData.Girls.Count : 0,
                    candidates = BuildAuditionCandidateSummaries(auditionData, context, false),
                    event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
                };

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindAudition,
                    context.OccurrenceId,
                    CoreConstants.EventTypeAuditionCandidatesGenerated,
                    CoreConstants.EventSourceAuditionsGenerateGirlsPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        internal void CaptureAuditionCompleted(Auditions.data auditionData)
        {
            if (auditionData == null)
            {
                return;
            }

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                AuditionRuntimeContext context;
                if (!activeAuditionRunsByData.TryGetValue(auditionData, out context) || context == null)
                {
                    return;
                }

                List<AuditionCandidateSummaryPayload> candidates =
                    BuildAuditionCandidateSummaries(auditionData, context, true);
                int hiredCount = 0;
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (candidates[i] != null && candidates[i].hired)
                    {
                        hiredCount++;
                    }
                }

                AuditionCompletedEventPayload payload = new AuditionCompletedEventPayload
                {
                    audition_occurrence_id = context.OccurrenceId,
                    audition_type = context.AuditionType,
                    generated_candidate_count = candidates.Count,
                    hired_candidate_count = hiredCount,
                    rejected_candidate_count = candidates.Count - hiredCount,
                    candidates = candidates,
                    event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
                };

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindAudition,
                    context.OccurrenceId,
                    CoreConstants.EventTypeAuditionCompleted,
                    CoreConstants.EventSourceAuditionsCompletePatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                activeAuditionRunsByData.Remove(auditionData);
                FlushAfterCaptureLocked();
            }
        }

        internal GenericDateOccurrenceSnapshot CreateGenericDateOccurrenceSnapshot(data_girls.girls idol)
        {
            return new GenericDateOccurrenceSnapshot
            {
                Idol = idol,
                OccurrenceId = CreateGenericDateOccurrenceId(),
                LocationCode = Dating.Next_Location.ToString().ToLowerInvariant(),
                WearMasks = Dating.Wear_Masks,
                InteractionBefore = CreatePlayerDateInteractionSnapshot(idol)
            };
        }

        internal void CaptureGenericDatePresented(data_girls.girls idol, GenericDateOccurrenceSnapshot snapshot)
        {
            if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier || snapshot == null)
            {
                return;
            }

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                GenericDateRuntimeContext context = new GenericDateRuntimeContext
                {
                    Idol = idol,
                    OccurrenceId = snapshot.OccurrenceId,
                    LocationCode = snapshot.LocationCode,
                    WearMasks = snapshot.WearMasks,
                    InteractionBefore = snapshot.InteractionBefore ?? new PlayerDateInteractionSnapshot()
                };
                activeGenericDatesByIdol[idol.id] = context;

                GenericDateEventPayload payload = BuildGenericDateEventPayload(context, false);
                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    idol.id,
                    CoreConstants.EventEntityKindPlayerDating,
                    context.OccurrenceId,
                    CoreConstants.EventTypePlayerGenericDatePresented,
                    CoreConstants.EventSourcePlayerGenericDateGeneratePatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        internal void CaptureGenericDateCompletedFromDialogueAction(data_dialogues._action action)
        {
            if (action == null ||
                !string.Equals(action.target, "dating", StringComparison.Ordinal) ||
                !string.Equals(action.parameter, "add_points", StringComparison.Ordinal))
            {
                return;
            }

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                // Idol Manager presents one blocking dialogue at a time. Refuse to guess
                // if stale/malformed state somehow leaves more than one generic date open.
                if (activeGenericDatesByIdol.Count != 1)
                {
                    return;
                }

                GenericDateRuntimeContext context = null;
                foreach (KeyValuePair<int, GenericDateRuntimeContext> pair in activeGenericDatesByIdol)
                {
                    context = pair.Value;
                    break;
                }
                if (context == null || context.Idol == null)
                {
                    return;
                }

                GenericDateEventPayload payload = BuildGenericDateEventPayload(context, true);
                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    context.Idol.id,
                    CoreConstants.EventEntityKindPlayerDating,
                    context.OccurrenceId,
                    CoreConstants.EventTypePlayerGenericDateCompleted,
                    CoreConstants.EventSourcePlayerGenericDateEffectPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                activeGenericDatesByIdol.Remove(context.Idol.id);
                FlushAfterCaptureLocked();
            }
        }

        private static GenericDateEventPayload BuildGenericDateEventPayload(
            GenericDateRuntimeContext context,
            bool completed)
        {
            data_girls.girls idol = context != null ? context.Idol : null;
            PlayerDateInteractionSnapshot before = context != null && context.InteractionBefore != null
                ? context.InteractionBefore
                : new PlayerDateInteractionSnapshot();
            Dating._partner partnerAfter = idol != null ? idol.GetDatingData() : null;

            return new GenericDateEventPayload
            {
                occurrence_id = context != null ? context.OccurrenceId : string.Empty,
                idol_id = idol != null ? idol.id : CoreConstants.InvalidIdValue,
                location = context != null ? context.LocationCode : string.Empty,
                wear_masks = context != null && context.WearMasks,
                completed = completed,
                route_before = before.RouteBefore ?? CoreConstants.StatusCodeUnknown,
                route_after = partnerAfter != null ? CoreEnumNameMapping.ToDatingPartnerRouteCode(partnerAfter.Route) : CoreConstants.StatusCodeUnknown,
                stage_before = before.StageBefore ?? CoreConstants.StatusCodeUnknown,
                stage_after = partnerAfter != null ? CoreEnumNameMapping.ToDatingPartnerRouteStageCode(partnerAfter.Progress) : CoreConstants.StatusCodeUnknown,
                status_before = before.StatusBefore ?? CoreConstants.StatusCodeUnknown,
                status_after = partnerAfter != null ? CoreEnumNameMapping.ToDatingPartnerStatusCode(partnerAfter.Status) : CoreConstants.StatusCodeUnknown,
                relationship_level_before = before.RelationshipLevelBefore,
                relationship_level_after = idol != null ? idol.GetRelationshipLevel(Relationships_Player._type.Romance) : before.RelationshipLevelBefore,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };
        }

        internal PlayerFlirtSnapshot CreatePlayerFlirtSnapshot(data_girls.girls idol)
        {
            data_girls.girls._dating_data dating = idol != null ? idol.DatingData : null;
            return new PlayerFlirtSnapshot
            {
                Idol = idol,
                PreviousAttemptBefore = dating != null ? dating.Previous_Attempt : Date_Flirt._flirt._category.NONE,
                SuccessCounterBefore = dating != null ? dating.Success_Counter : 0,
                SexualityKnownBefore = dating != null && dating.Is_Sexuality_Known,
                PartnerStatusKnownBefore = dating != null && dating.Is_Partner_Status_Known,
                UninterestedBefore = dating != null && dating.Is_Uninterested,
                UsedGoodsBefore = dating != null && dating.Used_Goods,
                FriendshipLevelBefore = idol != null ? idol.GetRelationshipLevel(Relationships_Player._type.Friendship) : 0,
                RomanceLevelBefore = idol != null ? idol.GetRelationshipLevel(Relationships_Player._type.Romance) : 0
            };
        }

        internal void CapturePlayerFlirtOutcome(data_girls.girls idol, PlayerFlirtSnapshot snapshot)
        {
            if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier || snapshot == null)
            {
                return;
            }

            data_girls.girls._dating_data dating = idol.DatingData;
            Date_Flirt._flirt._category outcome = dating != null
                ? dating.Previous_Attempt
                : Date_Flirt._flirt._category.NONE;

            PlayerFlirtOutcomePayload payload = new PlayerFlirtOutcomePayload
            {
                idol_id = idol.id,
                outcome = outcome.ToString().ToLowerInvariant(),
                previous_attempt_before = snapshot.PreviousAttemptBefore.ToString().ToLowerInvariant(),
                previous_attempt_after = outcome.ToString().ToLowerInvariant(),
                success_counter_before = snapshot.SuccessCounterBefore,
                success_counter_after = dating != null ? dating.Success_Counter : snapshot.SuccessCounterBefore,
                sexuality_known_before = snapshot.SexualityKnownBefore,
                sexuality_known_after = dating != null && dating.Is_Sexuality_Known,
                partner_status_known_before = snapshot.PartnerStatusKnownBefore,
                partner_status_known_after = dating != null && dating.Is_Partner_Status_Known,
                uninterested_before = snapshot.UninterestedBefore,
                uninterested_after = dating != null && dating.Is_Uninterested,
                used_goods_before = snapshot.UsedGoodsBefore,
                used_goods_after = dating != null && dating.Used_Goods,
                friendship_level_before = snapshot.FriendshipLevelBefore,
                friendship_level_after = idol.GetRelationshipLevel(Relationships_Player._type.Friendship),
                romance_level_before = snapshot.RomanceLevelBefore,
                romance_level_after = idol.GetRelationshipLevel(Relationships_Player._type.Romance),
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    idol.id,
                    CoreConstants.EventEntityKindPlayerDating,
                    idol.id.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypePlayerFlirtOutcome,
                    CoreConstants.EventSourcePlayerFlirtPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }
        }
    }

    [Serializable]
    internal sealed class IdolParameterBaselinePayload
    {
        public string parameter_type = string.Empty;
        public float value;
        public int potential;
        public bool potential_materialized;
    }

    [Serializable]
    internal sealed class IdolHireProfilePayload
    {
        public string first_name = string.Empty;
        public string last_name = string.Empty;
        public string nickname = string.Empty;
        public string sexuality = string.Empty;
        public string trait = string.Empty;
        public string birthday = string.Empty;
        public int peak_age;
        public List<IdolParameterBaselinePayload> parameters = new List<IdolParameterBaselinePayload>();
    }

    internal sealed class AuditionRuntimeContext
    {
        internal string OccurrenceId = string.Empty;
        internal string AuditionType = string.Empty;
        internal readonly List<string> CandidateIds = new List<string>();

        internal string GetOrCreateCandidateId(int ordinal)
        {
            while (CandidateIds.Count <= ordinal)
            {
                CandidateIds.Add(OccurrenceId + ":candidate:" + CandidateIds.Count.ToString(CultureInfo.InvariantCulture));
            }
            return CandidateIds[ordinal];
        }
    }

    [Serializable]
    internal sealed class AuditionCandidateSummaryPayload
    {
        public string candidate_id = string.Empty;
        public int candidate_ordinal;
        public int generated_idol_id = CoreConstants.InvalidIdValue;
        public string rarity = string.Empty;
        public IdolHireProfilePayload profile = new IdolHireProfilePayload();
        public bool hired;
        public int resulting_idol_id = CoreConstants.InvalidIdValue;
    }

    [Serializable]
    internal sealed class AuditionCandidatesGeneratedEventPayload
    {
        public string audition_occurrence_id = string.Empty;
        public string audition_type = string.Empty;
        public int generated_candidate_count;
        public List<AuditionCandidateSummaryPayload> candidates = new List<AuditionCandidateSummaryPayload>();
        public string event_date = string.Empty;
    }

    [Serializable]
    internal sealed class AuditionCompletedEventPayload
    {
        public string audition_occurrence_id = string.Empty;
        public string audition_type = string.Empty;
        public int generated_candidate_count;
        public int hired_candidate_count;
        public int rejected_candidate_count;
        public List<AuditionCandidateSummaryPayload> candidates = new List<AuditionCandidateSummaryPayload>();
        public string event_date = string.Empty;
    }

    internal sealed class GenericDateOccurrenceSnapshot
    {
        internal data_girls.girls Idol;
        internal string OccurrenceId = string.Empty;
        internal string LocationCode = string.Empty;
        internal bool WearMasks;
        internal PlayerDateInteractionSnapshot InteractionBefore;
    }

    internal sealed class GenericDateRuntimeContext
    {
        internal data_girls.girls Idol;
        internal string OccurrenceId = string.Empty;
        internal string LocationCode = string.Empty;
        internal bool WearMasks;
        internal PlayerDateInteractionSnapshot InteractionBefore;
    }

    [Serializable]
    internal sealed class GenericDateEventPayload
    {
        public string occurrence_id = string.Empty;
        public int idol_id = CoreConstants.InvalidIdValue;
        public string location = string.Empty;
        public bool wear_masks;
        public bool completed;
        public string route_before = string.Empty;
        public string route_after = string.Empty;
        public string stage_before = string.Empty;
        public string stage_after = string.Empty;
        public string status_before = string.Empty;
        public string status_after = string.Empty;
        public int relationship_level_before;
        public int relationship_level_after;
        public string event_date = string.Empty;
    }

    internal sealed class PlayerFlirtSnapshot
    {
        internal data_girls.girls Idol;
        internal Date_Flirt._flirt._category PreviousAttemptBefore;
        internal int SuccessCounterBefore;
        internal bool SexualityKnownBefore;
        internal bool PartnerStatusKnownBefore;
        internal bool UninterestedBefore;
        internal bool UsedGoodsBefore;
        internal int FriendshipLevelBefore;
        internal int RomanceLevelBefore;
    }

    [Serializable]
    internal sealed class PlayerFlirtOutcomePayload
    {
        public int idol_id = CoreConstants.InvalidIdValue;
        public string outcome = string.Empty;
        public string previous_attempt_before = string.Empty;
        public string previous_attempt_after = string.Empty;
        public int success_counter_before;
        public int success_counter_after;
        public bool sexuality_known_before;
        public bool sexuality_known_after;
        public bool partner_status_known_before;
        public bool partner_status_known_after;
        public bool uninterested_before;
        public bool uninterested_after;
        public bool used_goods_before;
        public bool used_goods_after;
        public int friendship_level_before;
        public int friendship_level_after;
        public int romance_level_before;
        public int romance_level_after;
        public string event_date = string.Empty;
    }
}
