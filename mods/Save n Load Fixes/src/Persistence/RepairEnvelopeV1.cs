using System;
using System.Collections.Generic;
using System.Globalization;
using SaveNLoadFixes.Repairs;

namespace SaveNLoadFixes.Persistence
{
    internal static class RepairEnvelopeConstants
    {
        internal const string RootKey = "__cosmo_save_n_load_fixes";
        internal const string FormatName = "cosmo-save-n-load-fixes";
        internal const int FormatVersion = 1;
    }

    [Serializable]
    internal sealed class RepairEnvelopeV1
    {
        public string format_name = RepairEnvelopeConstants.FormatName;
        public int format_version = RepairEnvelopeConstants.FormatVersion;
        public string checkpoint_id = string.Empty;
        public string mod_version = string.Empty;
        public string game_date = string.Empty;
        public RepairEnvelopeRecordsV1 records = new RepairEnvelopeRecordsV1();
    }

    [Serializable]
    internal sealed class RepairEnvelopeRecordsV1
    {
        public List<RelationshipDynamicRecordV1> relationship_dynamics =
            new List<RelationshipDynamicRecordV1>();

        // Section-level marker keeps Task-23 V1 envelopes distinguishable from
        // Task-24+ envelopes. Missing/zero means the N05 state was never captured.
        public int show_fan_appeals_version;
        public List<ShowFanAppealRecordV1> show_fan_appeals =
            new List<ShowFanAppealRecordV1>();

        // Section-level marker keeps Task-24 V1 envelopes distinguishable from
        // Task-25+ envelopes. Missing/zero means N07 was never captured.
        public int activity_chain_version;
        public List<int> activity_chain = new List<int>();

        // Section-level marker keeps Task-25 V1 envelopes distinguishable from
        // Task-26+ envelopes. Missing/zero means N08 was never captured.
        public int event_overlord_latest_event_version;
        public string event_overlord_latest_event_game_date = string.Empty;

        // Section-level marker keeps Task-26 V1 envelopes distinguishable from
        // Task-27+ envelopes. Missing/zero means N09 was never captured.
        public int paused_training_girls_version;
        public List<PausedTrainingGirlRecordV1> paused_training_girls =
            new List<PausedTrainingGirlRecordV1>();

        // Section-level marker keeps Task-27 V1 envelopes distinguishable from
        // Task-28+ envelopes. Missing/zero means N10 was never captured.
        public int business_remaining_minutes_version;
        public List<BusinessRemainingMinutesRecordV1> business_remaining_minutes =
            new List<BusinessRemainingMinutesRecordV1>();

        // Section-level marker keeps Task-28 V1 envelopes distinguishable from
        // Task-29+ envelopes. Missing/zero means N11 was never captured.
        public int tutorial_activity_baselines_version;
        public TutorialActivityBaselinesRecordV1 tutorial_activity_baselines;

        // Section-level marker keeps Task-29 V1 envelopes distinguishable from
        // Task-30+ envelopes. Missing/zero means A02 was never captured.
        public int project_progress_counters_version;
        public List<ProjectProgressCounterRecordV1> project_progress_counters =
            new List<ProjectProgressCounterRecordV1>();

        // Section-level marker keeps Task-30 V1 envelopes distinguishable from
        // Task-31+ envelopes. Missing/zero means A15 was never captured.
        public int recent_activity_timeline_version;
        public List<RecentActivityRecordV1> recent_activity_timeline =
            new List<RecentActivityRecordV1>();

        // Section-level marker keeps Task-31 V1 envelopes distinguishable from
        // Task-32+ envelopes. Missing/zero means A16 was never captured.
        public int previous_new_substory_version;
        public string previous_new_substory_game_date = string.Empty;

        // Section-level marker keeps Task-33 V1 envelopes distinguishable from
        // Task-34+ envelopes. Missing/zero means A29 was never captured.
        public int fan_appeal_last_single_version;
        public FanAppealLastSingleRecordV1 fan_appeal_last_single;

        // Section-level marker keeps Task-34 V1 envelopes distinguishable from
        // Task-35+ envelopes. Missing/zero means N02 was never captured.
        public int selected_business_proposal_version;
        public SelectedBusinessProposalRecordV1 selected_business_proposal;

        // Section-level marker keeps Task-35 V1 envelopes distinguishable from
        // Task-36+ envelopes. Missing/zero means N03 was never captured.
        public int queued_before_start_version;
        public List<QueuedBeforeStartRecordV1> queued_before_start =
            new List<QueuedBeforeStartRecordV1>();

        // Section-level marker keeps Task-36 V1 envelopes distinguishable from
        // Task-37+ envelopes. Missing/zero means N04 was never captured.
        public int room_substory_scenes_version;
        public List<RoomSubstorySceneRecordV1> room_substory_scenes =
            new List<RoomSubstorySceneRecordV1>();

        // Section-level marker keeps Task-38 V1 envelopes distinguishable from
        // Task-39+ envelopes. Missing/zero means N12 pending jobs were never captured.
        public int pending_ambient_scenes_version;
        public List<PendingAmbientSceneRecordV1> pending_ambient_scenes =
            new List<PendingAmbientSceneRecordV1>();

        // Section-level marker keeps Task-41 V1 envelopes distinguishable from
        // Task-42+ envelopes. Missing/zero means A11 successor introductions were never captured.
        public int successor_introductions_version;
        public List<int> successor_introduction_girl_ids = new List<int>();

        // Section-level marker keeps Task-42 V1 envelopes distinguishable from
        // Task-43+ envelopes. Missing/zero means A13 temporary auto-task bans were never captured.
        public int temporary_auto_task_bans_version;
        public List<TemporaryAutoTaskBanRecordV1> temporary_auto_task_bans =
            new List<TemporaryAutoTaskBanRecordV1>();

        // Section-level marker keeps Task-43 V1 envelopes distinguishable from
        // Task-44+ envelopes. Missing/zero means A14 delayed tutorial continuations were never captured.
        public int delayed_tutorial_continuations_version;
        public List<DelayedTutorialContinuationRecordV1> delayed_tutorial_continuations =
            new List<DelayedTutorialContinuationRecordV1>();

        // Section-level marker keeps Task-44 V1 envelopes distinguishable from
        // Task-45+ envelopes. Missing/zero means A28 pending award slate was never captured.
        public int award_temp_nominations_version;
        public List<AwardTempNominationRecordV1> award_temp_nominations =
            new List<AwardTempNominationRecordV1>();

        // Section-level marker keeps Task-45 V1 envelopes distinguishable from
        // Task-46+ envelopes. Missing/zero means A31 unresolved portrait identities were never captured.
        public int external_portrait_identities_version;
        public List<ExternalPortraitIdentityRecordV1> external_portrait_identities =
            new List<ExternalPortraitIdentityRecordV1>();
    }


    [Serializable]
    internal sealed class ExternalPortraitIdentityRecordV1
    {
        public string entity_kind = string.Empty;
        public int entity_id = -1;
        public int sprite_type = -1;
        public string asset_id = string.Empty;
    }

    [Serializable]
    internal sealed class AwardTempNominationRecordV1
    {
        public int award_type;
        public int year;
        public int girl_id = -1;
        public int single_id = -1;
    }

    [Serializable]
    internal sealed class DelayedTutorialContinuationRecordV1
    {
        public string kind = string.Empty;
        public bool has_remaining_scaled_delay;
        public float remaining_scaled_seconds;
    }

    [Serializable]
    internal sealed class TemporaryAutoTaskBanRecordV1
    {
        public int girl_id = -1;
        public float remaining_scaled_seconds;
    }

    [Serializable]
    internal sealed class PendingAmbientSceneRecordV1
    {
        public string due_game_time = string.Empty;
        public int floor_id;
        public int room_ordinal = -1;
        public int room_type;
        public int scene_type;
        public List<int> girl_ids = new List<int>();
    }

    [Serializable]
    internal sealed class RoomSubstorySceneRecordV1
    {
        public int floor_id;
        public int room_ordinal;
        public int room_type;
        public string dialogue_id = string.Empty;
    }

    [Serializable]
    internal sealed class QueuedBeforeStartRecordV1
    {
        public int queue_ordinal = -1;
        public string dialogue_id = string.Empty;
        public string launch_time = string.Empty;
        public float delay;
        public bool debug;
        public int setup_kind;
        public int girl_id_a = -1;
        public int girl_id_b = -1;
        public bool set_mask;
        public string semantic_arg = string.Empty;
    }

    [Serializable]
    internal sealed class SelectedBusinessProposalRecordV1
    {
        public bool has_value;
        public int active_event_ordinal = -1;
        public string event_id = string.Empty;
        public string event_date = string.Empty;
        public int event_state;
        public int business_actor_ordinal = -1;
        public int actor_girl_id = -1;
        public int actor_staff_id = -1;
        public int required_proposal_type = -1;
        public int proposal_ordinal = -1;
        public bool proposal_is_group;
        public int proposal_girl_id = -1;
        public int proposal_skill;
        public int proposal_type;
        public int payment_per_week;
        public int buzz_per_week;
        public int fame_per_week;
        public int stamina_per_week;
        public int fans_per_week;
        public string agent_name = string.Empty;
        public string product_name = string.Empty;
        public long liability;
        public string end_date = string.Empty;
    }

    [Serializable]
    internal sealed class PausedTrainingGirlRecordV1
    {
        public int floor_id;
        public int room_ordinal;
        public int room_type;
        public int girl_id;
    }

    [Serializable]
    internal sealed class BusinessRemainingMinutesRecordV1
    {
        public int floor_id;
        public int room_ordinal;
        public int room_type;
        public float minutes_before_finish;
    }

    [Serializable]
    internal sealed class TutorialActivityBaselinesRecordV1
    {
        public bool performance_baseline_has_value;
        public int performance_baseline;
        public bool promotion_baseline_has_value;
        public int promotion_baseline;
    }

    [Serializable]
    internal sealed class ProjectProgressCounterRecordV1
    {
        public int owner_kind;
        public int owner_id;
        public int parameter_type;
        public int counter;
    }

    [Serializable]
    internal sealed class RecentActivityRecordV1
    {
        public int activity_type;
        public string game_date = string.Empty;
    }

    [Serializable]
    internal sealed class FanAppealLastSingleRecordV1
    {
        public bool has_value;
        public int source_single_id = -1;
        public List<FanAppealRatioRecordV1> fan_appeal =
            new List<FanAppealRatioRecordV1>();
    }

    [Serializable]
    internal sealed class RelationshipDynamicRecordV1
    {
        public int girl_id_low;
        public int girl_id_high;
        public int dynamic;
    }

    [Serializable]
    internal sealed class ShowFanAppealRecordV1
    {
        public int show_id;
        public List<FanAppealRatioRecordV1> fan_appeal =
            new List<FanAppealRatioRecordV1>();
    }

    [Serializable]
    internal sealed class FanAppealRatioRecordV1
    {
        public int fan_type;
        public float ratio;
    }

    /// <summary>
    /// Captures only repair state that belongs to the exact vanilla SavedData request.
    /// Task 23 starts V1 with N01 relationship dynamics. Task 24 adds N05 show
    /// FanAppeal; Task 25 adds section-marked N07 Activities.Chain future intent;
    /// Task 26 adds N08 Event_Overlord.Latest_Event pacing continuity; Task 27 adds
    /// N09 paused-training displaced-idol rebinding; Task 28 adds N10 hidden business
    /// remaining-minutes continuity; Task 29 adds N11 nullable tutorial activity
    /// baselines; Task 30 adds A02 sparse private project progress counters; Task 31
    /// adds A15 bounded recent-activity recency state; Task 32 adds A16 exact
    /// PreviousNewSubstory spacing-anchor continuity; Task 34 adds A29 exact
    /// FanAppeal_LastSingle comparison-baseline continuity; Task 35 adds N02 exact
    /// event-selected business-proposal rebinding; Task 36 adds N03 queue-correlated
    /// semantic BeforeStart descriptors; Task 37 adds N04 active-room substoryScene
    /// dialogue identity rebinding; Task 39 adds N12-B pending ambient-scene semantic
    /// job persistence; Task 42 adds A11 successor-introduction pending idol IDs;
    /// Task 43 adds A13 temporary auto-task-ban idol IDs plus remaining scaled delay;
    /// Task 44 adds A14 fixed delayed-tutorial continuation kinds plus optional
    /// scaled presentation-delay remainder. Later semantic-rebinding tasks may add sparse typed sections
    /// without changing the physical transport contract.
    /// </summary>
    internal static class RepairEnvelopeBuilder
    {
        internal static bool TryBuild(
            SaveManager.SavedData dataToSave,
            out RepairEnvelopeV1 envelope,
            out string error)
        {
            envelope = null;
            error = string.Empty;

            if (dataToSave == null)
            {
                error = "SavedData request is null.";
                return false;
            }

            List<Relationships.SaveData_Rel> serializedRelationships =
                dataToSave.Relationships__RelationshipsData;
            if (serializedRelationships == null)
            {
                error = "SavedData relationship rows are null after SaveEvent population.";
                return false;
            }

            Dictionary<string, Relationships._relationship> liveByPair =
                new Dictionary<string, Relationships._relationship>(StringComparer.Ordinal);
            HashSet<string> ambiguousLivePairs =
                new HashSet<string>(StringComparer.Ordinal);

            List<Relationships._relationship> liveRelationships =
                Relationships.RelationshipsData;
            if (liveRelationships == null)
            {
                error = "Live relationship registry is null.";
                return false;
            }

            for (int index = 0; index < liveRelationships.Count; index++)
            {
                Relationships._relationship relationship = liveRelationships[index];
                int low;
                int high;
                if (!TryGetLivePair(relationship, out low, out high))
                {
                    continue;
                }

                string key = BuildPairKey(low, high);
                if (liveByPair.ContainsKey(key))
                {
                    ambiguousLivePairs.Add(key);
                    continue;
                }

                liveByPair.Add(key, relationship);
            }

            List<RelationshipDynamicRecordV1> records =
                new List<RelationshipDynamicRecordV1>();
            HashSet<string> serializedPairs =
                new HashSet<string>(StringComparer.Ordinal);

            for (int index = 0; index < serializedRelationships.Count; index++)
            {
                Relationships.SaveData_Rel saved = serializedRelationships[index];
                int low;
                int high;
                if (!TryGetSavedPair(saved, out low, out high))
                {
                    error = "Serialized relationship row does not contain exactly two distinct idol IDs.";
                    return false;
                }

                string key = BuildPairKey(low, high);
                if (!serializedPairs.Add(key))
                {
                    error = "Serialized relationship rows contain a duplicate unordered idol pair.";
                    return false;
                }

                Relationships._relationship live;
                if (ambiguousLivePairs.Contains(key) ||
                    !liveByPair.TryGetValue(key, out live) ||
                    live == null)
                {
                    error = "Serialized relationship row could not be matched to exactly one live relationship.";
                    return false;
                }

                int dynamicValue = (int)live.Dynamic;
                if (dynamicValue < (int)Relationships._relationship._dynamic.NONE ||
                    dynamicValue > (int)Relationships._relationship._dynamic.negative)
                {
                    error = "Live relationship Dynamic value is outside the audited enum domain.";
                    return false;
                }

                records.Add(
                    new RelationshipDynamicRecordV1
                    {
                        girl_id_low = low,
                        girl_id_high = high,
                        dynamic = dynamicValue
                    });
            }

            records.Sort(
                delegate(RelationshipDynamicRecordV1 left, RelationshipDynamicRecordV1 right)
                {
                    int lowCompare = left.girl_id_low.CompareTo(right.girl_id_low);
                    return lowCompare != 0
                        ? lowCompare
                        : left.girl_id_high.CompareTo(right.girl_id_high);
                });

            List<Shows.ShowData> serializedShows = dataToSave.shows__Shows;
            if (serializedShows == null)
            {
                error = "SavedData show rows are null after SaveEvent population.";
                return false;
            }

            if (Shows.shows == null)
            {
                error = "Live show registry is null.";
                return false;
            }

            Dictionary<int, Shows._show> liveShowsById = new Dictionary<int, Shows._show>();
            HashSet<int> ambiguousLiveShowIds = new HashSet<int>();
            for (int index = 0; index < Shows.shows.Count; index++)
            {
                Shows._show liveShow = Shows.shows[index];
                if (liveShow == null || liveShow.id < 0)
                {
                    error = "Live show registry contains a null or invalid-ID show.";
                    return false;
                }

                if (liveShowsById.ContainsKey(liveShow.id))
                {
                    ambiguousLiveShowIds.Add(liveShow.id);
                    continue;
                }

                liveShowsById.Add(liveShow.id, liveShow);
            }

            if (liveShowsById.Count != serializedShows.Count || ambiguousLiveShowIds.Count != 0)
            {
                error = "Serialized/live show sets do not form an exact unique-ID set.";
                return false;
            }

            List<ShowFanAppealRecordV1> showFanAppeals =
                new List<ShowFanAppealRecordV1>();
            HashSet<int> serializedShowIds = new HashSet<int>();
            for (int index = 0; index < serializedShows.Count; index++)
            {
                Shows.ShowData savedShow = serializedShows[index];
                if (savedShow == null || savedShow.id < 0 || !serializedShowIds.Add(savedShow.id))
                {
                    error = "Serialized show rows contain a null, invalid, or duplicate show ID.";
                    return false;
                }

                Shows._show liveShow;
                if (!liveShowsById.TryGetValue(savedShow.id, out liveShow) ||
                    liveShow == null || liveShow.FanAppeal == null)
                {
                    error = "Serialized show row could not be matched to exact live FanAppeal state.";
                    return false;
                }

                ShowFanAppealRecordV1 showRecord = new ShowFanAppealRecordV1
                {
                    show_id = savedShow.id,
                    fan_appeal = new List<FanAppealRatioRecordV1>()
                };

                // Preserve list order exactly. SetOpinion seeds tie-breaking from FanAppeal[0],
                // so sorting the inner vector could change behavior when ratios tie.
                for (int appealIndex = 0; appealIndex < liveShow.FanAppeal.Count; appealIndex++)
                {
                    singles._fanAppeal appeal = liveShow.FanAppeal[appealIndex];
                    if (appeal == null ||
                        (int)appeal.type < (int)resources.fanType.male ||
                        (int)appeal.type > (int)resources.fanType.adult ||
                        float.IsNaN(appeal.ratio) ||
                        float.IsInfinity(appeal.ratio))
                    {
                        error = "Live show FanAppeal contains an invalid entry.";
                        return false;
                    }

                    showRecord.fan_appeal.Add(
                        new FanAppealRatioRecordV1
                        {
                            fan_type = (int)appeal.type,
                            ratio = appeal.ratio
                        });
                }

                showFanAppeals.Add(showRecord);
            }

            showFanAppeals.Sort(
                delegate(ShowFanAppealRecordV1 left, ShowFanAppealRecordV1 right)
                {
                    return left.show_id.CompareTo(right.show_id);
                });

            if (Activities.Chain == null)
            {
                error = "Live Activities.Chain registry is null.";
                return false;
            }

            List<int> activityChain = new List<int>();
            for (int index = 0; index < Activities.Chain.Count; index++)
            {
                int activityType = (int)Activities.Chain[index];
                if (activityType < (int)Activity._type.performance ||
                    activityType > (int)Activity._type.spa_treatment)
                {
                    error = "Live Activities.Chain contains an activity type outside the audited enum domain.";
                    return false;
                }

                // Preserve player-authored future-intent order exactly. In particular,
                // the head remains present during Chain_Progress_Do's 0.1-second gap
                // until vanilla dispatches the activity and then removes index zero.
                activityChain.Add(activityType);
            }

            string latestEventGameDate;
            if (!EventOverlordLatestEventRepair.TryCaptureForEnvelope(
                    dataToSave,
                    out latestEventGameDate,
                    out error))
            {
                return false;
            }

            List<PausedTrainingGirlRecordV1> pausedTrainingGirls;
            if (!PausedTrainingGirlRepair.TryCaptureForEnvelope(
                    dataToSave,
                    out pausedTrainingGirls,
                    out error))
            {
                return false;
            }

            List<BusinessRemainingMinutesRecordV1> businessRemainingMinutes;
            if (!BusinessRemainingMinutesRepair.TryCaptureForEnvelope(
                    dataToSave,
                    out businessRemainingMinutes,
                    out error))
            {
                return false;
            }

            TutorialActivityBaselinesRecordV1 tutorialActivityBaselines;
            if (!TutorialActivityBaselineRepair.TryCaptureForEnvelope(
                    dataToSave,
                    out tutorialActivityBaselines,
                    out error))
            {
                return false;
            }

            List<ProjectProgressCounterRecordV1> projectProgressCounters;
            if (!ProjectProgressCounterRepair.TryCaptureForEnvelope(
                    dataToSave,
                    out projectProgressCounters,
                    out error))
            {
                return false;
            }

            List<RecentActivityRecordV1> recentActivityTimeline;
            if (!RecentActivityRecencyRepair.TryCaptureForEnvelope(
                    dataToSave,
                    out recentActivityTimeline,
                    out error))
            {
                return false;
            }

            string previousNewSubstoryGameDate;
            if (!PreviousNewSubstoryRepair.TryCaptureForEnvelope(
                    dataToSave,
                    out previousNewSubstoryGameDate,
                    out error))
            {
                return false;
            }

            FanAppealLastSingleRecordV1 fanAppealLastSingle;
            if (!FanAppealLastSingleRepair.TryCaptureForEnvelope(
                    dataToSave,
                    out fanAppealLastSingle,
                    out error))
            {
                return false;
            }

            SelectedBusinessProposalRecordV1 selectedBusinessProposal;
            if (!SelectedBusinessProposalRepair.TryCaptureForEnvelope(
                    dataToSave,
                    out selectedBusinessProposal,
                    out error))
            {
                return false;
            }

            List<QueuedBeforeStartRecordV1> queuedBeforeStart;
            if (!QueuedBeforeStartRepair.TryCaptureForEnvelope(
                    dataToSave,
                    out queuedBeforeStart,
                    out error))
            {
                return false;
            }

            List<RoomSubstorySceneRecordV1> roomSubstoryScenes;
            if (!RoomSubstorySceneRepair.TryCaptureForEnvelope(
                    dataToSave,
                    out roomSubstoryScenes,
                    out error))
            {
                return false;
            }

            List<PendingAmbientSceneRecordV1> pendingAmbientScenes;
            if (!PendingAmbientScenePersistence.TryCaptureForEnvelope(
                    dataToSave,
                    out pendingAmbientScenes,
                    out error))
            {
                return false;
            }

            List<int> successorIntroductionGirlIds;
            if (!SuccessorIntroductionRepair.TryCaptureForEnvelope(
                    dataToSave,
                    out successorIntroductionGirlIds,
                    out error))
            {
                return false;
            }

            List<TemporaryAutoTaskBanRecordV1> temporaryAutoTaskBans;
            if (!TemporaryAutoTaskBanRepair.TryCaptureForEnvelope(
                    dataToSave,
                    out temporaryAutoTaskBans,
                    out error))
            {
                return false;
            }

            List<DelayedTutorialContinuationRecordV1> delayedTutorialContinuations;
            if (!DelayedTutorialContinuationRepair.TryCaptureForEnvelope(
                    dataToSave,
                    out delayedTutorialContinuations,
                    out error))
            {
                return false;
            }

            List<AwardTempNominationRecordV1> awardTempNominations;
            if (!AwardTempNominationRepair.TryCaptureForEnvelope(
                    dataToSave,
                    out awardTempNominations,
                    out error))
            {
                return false;
            }

            List<ExternalPortraitIdentityRecordV1> externalPortraitIdentities;
            if (!ExternalPortraitIdentityRepair.TryCaptureForEnvelope(
                    dataToSave,
                    out externalPortraitIdentities,
                    out error))
            {
                return false;
            }

            envelope = new RepairEnvelopeV1
            {
                checkpoint_id = Guid.NewGuid().ToString("D"),
                mod_version = SaveNLoadFixesConstants.Version,
                game_date = dataToSave.staticVars__dateTime ?? string.Empty,
                records = new RepairEnvelopeRecordsV1
                {
                    relationship_dynamics = records,
                    show_fan_appeals_version = 1,
                    show_fan_appeals = showFanAppeals,
                    activity_chain_version = 1,
                    activity_chain = activityChain,
                    event_overlord_latest_event_version = EventOverlordLatestEventRepair.SectionVersion,
                    event_overlord_latest_event_game_date = latestEventGameDate,
                    paused_training_girls_version = PausedTrainingGirlRepair.SectionVersion,
                    paused_training_girls = pausedTrainingGirls,
                    business_remaining_minutes_version = BusinessRemainingMinutesRepair.SectionVersion,
                    business_remaining_minutes = businessRemainingMinutes,
                    tutorial_activity_baselines_version = TutorialActivityBaselineRepair.SectionVersion,
                    tutorial_activity_baselines = tutorialActivityBaselines,
                    project_progress_counters_version = ProjectProgressCounterRepair.SectionVersion,
                    project_progress_counters = projectProgressCounters,
                    recent_activity_timeline_version = RecentActivityRecencyRepair.SectionVersion,
                    recent_activity_timeline = recentActivityTimeline,
                    previous_new_substory_version = PreviousNewSubstoryRepair.SectionVersion,
                    previous_new_substory_game_date = previousNewSubstoryGameDate,
                    fan_appeal_last_single_version = FanAppealLastSingleRepair.SectionVersion,
                    fan_appeal_last_single = fanAppealLastSingle,
                    selected_business_proposal_version = SelectedBusinessProposalRepair.SectionVersion,
                    selected_business_proposal = selectedBusinessProposal,
                    queued_before_start_version = QueuedBeforeStartRepair.SectionVersion,
                    queued_before_start = queuedBeforeStart,
                    room_substory_scenes_version = RoomSubstorySceneRepair.SectionVersion,
                    room_substory_scenes = roomSubstoryScenes,
                    pending_ambient_scenes_version = PendingAmbientScenePersistence.SectionVersion,
                    pending_ambient_scenes = pendingAmbientScenes,
                    successor_introductions_version = SuccessorIntroductionRepair.SectionVersion,
                    successor_introduction_girl_ids = successorIntroductionGirlIds,
                    temporary_auto_task_bans_version = TemporaryAutoTaskBanRepair.SectionVersion,
                    temporary_auto_task_bans = temporaryAutoTaskBans,
                    delayed_tutorial_continuations_version = DelayedTutorialContinuationRepair.SectionVersion,
                    delayed_tutorial_continuations = delayedTutorialContinuations,
                    award_temp_nominations_version = AwardTempNominationRepair.SectionVersion,
                    award_temp_nominations = awardTempNominations,
                    external_portrait_identities_version = ExternalPortraitIdentityRepair.SectionVersion,
                    external_portrait_identities = externalPortraitIdentities
                }
            };

            return true;
        }

        internal static string BuildPairKey(int low, int high)
        {
            return low.ToString(CultureInfo.InvariantCulture) + ":" +
                high.ToString(CultureInfo.InvariantCulture);
        }

        internal static bool TryNormalizePair(
            int first,
            int second,
            out int low,
            out int high)
        {
            low = 0;
            high = 0;
            if (first < 0 || second < 0 || first == second)
            {
                return false;
            }

            low = Math.Min(first, second);
            high = Math.Max(first, second);
            return true;
        }

        private static bool TryGetSavedPair(
            Relationships.SaveData_Rel saved,
            out int low,
            out int high)
        {
            low = 0;
            high = 0;
            if (saved == null || saved.Girls == null || saved.Girls.Count != 2)
            {
                return false;
            }

            return TryNormalizePair(saved.Girls[0], saved.Girls[1], out low, out high);
        }

        private static bool TryGetLivePair(
            Relationships._relationship relationship,
            out int low,
            out int high)
        {
            low = 0;
            high = 0;
            if (relationship == null ||
                relationship.Girls == null ||
                relationship.Girls.Count != 2 ||
                relationship.Girls[0] == null ||
                relationship.Girls[1] == null)
            {
                return false;
            }

            return TryNormalizePair(
                relationship.Girls[0].id,
                relationship.Girls[1].id,
                out low,
                out high);
        }
    }
}
