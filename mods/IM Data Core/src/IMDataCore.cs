using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace IMDataCore
{
    /// <summary>
    /// Central constants used across IM Data Core to avoid magic values and keep behavior explicit.
    /// </summary>
    internal static class CoreConstants
    {
        internal const string LogPrefix = "[IMDataCore] ";

        internal const string ModFolderName = "IMDataCore";
        internal const string ModsFolderName = "Mods";
        internal const string SaveFolderName = "saves";
        internal const string DefaultSaveKey = "default";
        internal const string DatabaseFileName = "im_data_core.db";
        internal const string FlatFileDatabaseFileName = "im_data_core.fallback.json";

        internal const string SaveModeStory = "story";
        internal const string SaveModeFreePlay = "freeplay";
        internal const string SaveKeyJoinSeparator = "_";

        internal const int SaveKeyMaximumLength = 64;
        internal const int SaveKeyMinimumLength = 1;
        internal const int SaveTokenMaximumLength = 64;
        internal const int NamespaceMaximumLength = 64;
        internal const int NamespaceMinimumLength = 3;
        internal const int DataKeyMaximumLength = 128;
        internal const int DataKeyMinimumLength = 1;

        internal const int OneKilobyteCharacterCount = 1024;
        internal const int NamespaceBudgetMegabytes = 5;
        internal const int MaximumNamespaceCharacterBudget = NamespaceBudgetMegabytes * OneKilobyteCharacterCount * OneKilobyteCharacterCount;
        internal const int MaximumCustomKeysPerNamespace = 4096;
        internal const int MaximumCustomValueCharacterCount = 65536;

        internal const int MinimumRecentEventRequestCount = 1;
        internal const int DefaultRecentEventRequestCount = 200;
        internal const int MaximumRecentEventRequestCount = 1000;

        internal const int DateKeyYearMultiplier = 10000;
        internal const int DateKeyMonthMultiplier = 100;
        internal const string RoundTripDateFormat = "o";

        internal const float FlushIntervalSeconds = 3f;
        internal const int ImmediateFlushQueueThreshold = 128;
        internal const int MaximumBufferedEventsAfterPersistenceFailure = 8192;
        internal const bool PrettyPrintJsonPayload = false;

        internal const string ProviderNamePrimary = "Mono.Data.Sqlite";
        internal const string ProviderNameFallback = "Mono.Data.SqliteClient";
        internal const string ProviderFactoryTypePrimary = "Mono.Data.Sqlite.SqliteFactory, Mono.Data.Sqlite";
        internal const string ProviderFactoryTypeFallback = "Mono.Data.SqliteClient.SqliteFactory, Mono.Data.SqliteClient";
        internal static readonly string[] ProviderCandidates = new[] { ProviderNamePrimary, ProviderNameFallback };
        internal const string SystemDataFactoryTypeName = "System.Data.Common.DbProviderFactory, System.Data";
        internal const string SystemDataDbTypeName = "System.Data.DbType, System.Data";
        internal const string SqliteProviderAssemblyFileName = "Mono.Data.Sqlite.dll";
        internal const string SystemDataAssemblyFileName = "System.Data.dll";
        internal const string SqliteProviderPathEnvironmentVariableName = "IMDATACORE_SQLITE_PROVIDER_PATH";
        internal const string SystemDataPathEnvironmentVariableName = "IMDATACORE_SYSTEM_DATA_PATH";
        internal const string EnableExternalSqliteLoadEnvironmentVariableName = "IMDATACORE_ENABLE_EXTERNAL_SQLITE";
        internal const string ConnectionStringFormat = "Data Source={0};Version=3;Pooling=True;";

        internal const string SqlPragmaJournalMode = "PRAGMA journal_mode=WAL;";
        internal const string SqlPragmaSynchronous = "PRAGMA synchronous=NORMAL;";
        internal const string SqlPragmaForeignKeys = "PRAGMA foreign_keys=ON;";

        internal const string MetaSchemaVersionKey = "schema_version";
        internal const string MetaProviderKey = "db_provider";
        internal const string SchemaVersionValue = "2";

        internal const string UnknownAssemblyIdentity = "unknown_assembly_identity";
        internal const string SessionTokenFormat = "N";

        internal const string EventTypeSingleCreated = "single_created";
        internal const string EventTypeSingleReleased = "single_released";
        internal const string EventTypeSingleParticipationRecorded = "single_participation_recorded";
        internal const string EventTypeSingleCancelled = "single_cancelled";
        internal const string EventTypeSingleStatusChanged = "single_status_changed";
        internal const string EventTypeSingleCastChanged = "single_cast_changed";
        internal const string EventTypeSingleGroupChanged = "single_group_changed";
        internal const string EventTypeIdolGroupTransferred = "idol_group_transferred";
        internal const string EventTypeGroupCreated = "group_created";
        internal const string EventTypeGroupDisbanded = "group_disbanded";
        internal const string EventTypeGroupParamPointsChanged = "group_param_points_changed";
        internal const string EventTypeGroupAppealPointsSpent = "group_appeal_points_spent";
        internal const string EventTypeStatusChanged = "idol_status_changed";
        internal const string EventTypeStatusChangedLegacy = "status_changed";
        internal const string EventTypeStatusStarted = "status_started";
        internal const string EventTypeStatusEnded = "status_ended";
        internal const string EventTypeDatingPartnerStatusChanged = "dating_partner_status_changed";
        internal const string EventTypeIdolDatingStatusChanged = "idol_dating_status_changed";
        internal const string EventTypeContractActivated = "contract_activated";
        internal const string EventTypeContractWindowOpened = "contract_window_opened";
        internal const string EventTypeContractAccepted = "contract_accepted";
        internal const string EventTypeContractCancelled = "contract_cancelled";
        internal const string EventTypeContractCanceled = "contract_canceled";
        internal const string EventTypeContractFinished = "contract_finished";
        internal const string EventTypeContractWeeklyEarningsApplied = "contract_weekly_earnings_applied";
        internal const string EventTypeContractWeeklyBenefitsApplied = "contract_weekly_benefits_applied";
        internal const string EventTypeContractBroken = "contract_broken";
        internal const string EventTypeShowCreated = "show_created";
        internal const string EventTypeShowReleased = "show_released";
        internal const string EventTypeShowCancelled = "show_cancelled";
        internal const string EventTypeShowStatusChanged = "show_status_changed";
        internal const string EventTypeShowEpisodeReleased = "show_episode_released";
        internal const string EventTypeShowEpisode = "show_episode";
        internal const string EventTypeShowCastChanged = "show_cast_changed";
        internal const string EventTypeShowConfigurationChanged = "show_configuration_changed";
        internal const string EventTypeShowRelaunchStarted = "show_relaunch_started";
        internal const string EventTypeShowRelaunchFinished = "show_relaunch_finished";
        internal const string EventTypeTourCreated = "tour_created";
        internal const string EventTypeTourStarted = "tour_started";
        internal const string EventTypeTourFinished = "tour_finished";
        internal const string EventTypeTourCancelled = "tour_cancelled";
        internal const string EventTypeTourCountryResult = "tour_country_result";
        internal const string EventTypeTourParticipation = "tour_participation";
        internal const string EventTypeTourStatusChanged = "tour_status_changed";
        internal const string EventTypeElectionCreated = "election_created";
        internal const string EventTypeElectionFinished = "election_finished";
        internal const string EventTypeElectionCancelled = "election_cancelled";
        internal const string EventTypeElectionResultsGenerated = "election_results_generated";
        internal const string EventTypeElectionPlaceAdjusted = "election_place_adjusted";
        internal const string EventTypeElectionStatusChanged = "election_status_changed";
        internal const string EventTypeElectionResultRecorded = "election_result_recorded";
        internal const string EventTypeScandalPointsChanged = "scandal_points_changed";
        internal const string EventTypeMedicalInjury = "medical_injury";
        internal const string EventTypeMedicalDepression = "medical_depression";
        internal const string EventTypeMedicalHiatusStarted = "medical_hiatus_started";
        internal const string EventTypeMedicalHealed = "medical_healed";
        internal const string EventTypeMedicalHiatusFinished = "medical_hiatus_finished";
        internal const string EventTypeConcertCreated = "concert_created";
        internal const string EventTypeConcertStarted = "concert_started";
        internal const string EventTypeConcertFinished = "concert_finished";
        internal const string EventTypeConcertCancelled = "concert_cancelled";
        internal const string EventTypeConcertParticipation = "concert_participation";
        internal const string EventTypeConcertCastChanged = "concert_cast_changed";
        internal const string EventTypeConcertStatusChanged = "concert_status_changed";
        internal const string EventTypeConcertConfigurationChanged = "concert_configuration_changed";
        internal const string EventTypeAwardNominated = "award_nominated";
        internal const string EventTypeAwardResult = "award_result";
        internal const string EventTypePushWindowStarted = "push_window_started";
        internal const string EventTypePushWindowEnded = "push_window_ended";
        internal const string EventTypePushWindowDayIncrement = "push_window_day_increment";
        internal const string EventTypeIdolDatingStarted = "idol_dating_started";
        internal const string EventTypeIdolDatingEnded = "idol_dating_ended";
        internal const string EventTypeCliqueJoined = "clique_joined";
        internal const string EventTypeCliqueLeft = "clique_left";
        internal const string EventTypeBullyingStarted = "bullying_started";
        internal const string EventTypeBullyingEnded = "bullying_ended";
        internal const string EventTypeScandalMitigated = "scandal_mitigated";
        internal const string EventTypePlayerRelationshipChanged = "player_relationship_changed";
        internal const string EventTypePlayerDateInteraction = "player_date_interaction";
        internal const string EventTypePlayerMarriageOutcome = "player_marriage_outcome";
        internal const string EventTypeIdolRelationshipStatusChanged = "idol_relationship_status_changed";
        internal const string EventTypeIdolHired = "idol_hired";
        internal const string EventTypeIdolGraduationAnnounced = "idol_graduation_announced";
        internal const string EventTypeIdolGraduated = "idol_graduated";
        internal const string EventTypeIdolSalaryChanged = "idol_salary_changed";
        internal const string EventTypeLoanAdded = "loan_added";
        internal const string EventTypeLoanInitialized = "loan_initialized";
        internal const string EventTypeLoanPaidOff = "loan_paid_off";
        internal const string EventTypeBankruptcyDangerSet = "bankruptcy_danger_set";
        internal const string EventTypeBankruptcyCheck = "bankruptcy_check";
        internal const string EventTypeScandalCheck = "scandal_check";
        internal const string EventTypeAuditionFailureTriggered = "audition_failure_triggered";
        internal const string EventTypePolicyDecisionSelected = "policy_decision_selected";
        internal const string EventTypeActivityPerformance = "activity_performance";
        internal const string EventTypeActivityPromotion = "activity_promotion";
        internal const string EventTypeActivitySpaTreatment = "activity_spa_treatment";
        internal const string EventTypeIdolEarningsRecorded = "idol_earnings_recorded";
        internal const string EventTypeTheaterCreated = "theater_created";
        internal const string EventTypeTheaterDestroyed = "theater_destroyed";
        internal const string EventTypeTheaterDailyResult = "theater_daily_result";
        internal const string EventTypeCafeCreated = "cafe_created";
        internal const string EventTypeCafeDestroyed = "cafe_destroyed";
        internal const string EventTypeCafeDailyResult = "cafe_daily_result";
        internal const string EventTypeStaffHired = "staff_hired";
        internal const string EventTypeStaffFired = "staff_fired";
        internal const string EventTypeStaffFiredSeverance = "staff_fired_severance";
        internal const string EventTypeStaffLevelUp = "staff_level_up";
        internal const string EventTypeResearchParamAssigned = "research_param_assigned";
        internal const string EventTypeResearchPointsPurchased = "research_points_purchased";
        internal const string EventTypeResearchParamLevelUp = "research_param_level_up";
        internal const string EventTypeStoryRouteLocked = "story_route_locked";
        internal const string EventTypeTaskCompleted = "task_completed";
        internal const string EventTypeTaskFailed = "task_failed";
        internal const string EventTypeTaskDone = "task_done";
        internal const string EventTypeConcertCardUsed = "concert_card_used";
        internal const string EventTypeConcertCrisisDecision = "concert_crisis_decision";
        internal const string EventTypeConcertCrisisApplied = "concert_crisis_applied";
        internal const string EventTypeConcertFinalResolved = "concert_final_resolved";
        internal const string EventTypeElectionStarted = "election_started";
        internal const string EventTypeWishGenerated = "wish_generated";
        internal const string EventTypeWishFulfilled = "wish_fulfilled";
        internal const string EventTypeWishDone = "wish_done";
        internal const string EventTypeResearchPointsAccrued = "research_points_accrued";
        internal const string EventTypeAgencyRoomBuilt = "agency_room_built";
        internal const string EventTypeAgencyRoomDestroyed = "agency_room_destroyed";
        internal const string EventTypeAgencyRoomCostPaid = "agency_room_cost_paid";
        internal const string EventTypeAuditionStarted = "audition_started";
        internal const string EventTypeAuditionCostPaid = "audition_cost_paid";
        internal const string EventTypeAuditionCooldownReset = "audition_cooldown_reset";
        internal const string EventTypeRandomEventStarted = "random_event_started";
        internal const string EventTypeRandomEventConcluded = "random_event_concluded";
        internal const string EventTypeSubstoryStarted = "substory_started";
        internal const string EventTypeSubstoryDelayed = "substory_delayed";
        internal const string EventTypeSubstoryCompleted = "substory_completed";
        internal const string EventTypeEconomyWeeklyExpenseApplied = "economy_weekly_expense_applied";
        internal const string EventTypeEconomyDailyTick = "economy_daily_tick";
        internal const string EventTypeInfluenceBlackmailQueued = "influence_blackmail_queued";
        internal const string EventTypeInfluenceBlackmailTriggered = "influence_blackmail_triggered";
        internal const string EventTypeMentorshipStarted = "mentorship_started";
        internal const string EventTypeMentorshipEnded = "mentorship_ended";
        internal const string EventTypeMentorshipWeeklyTick = "mentorship_weekly_tick";
        internal const string EventTypeRivalTrendsUpdated = "rival_trends_updated";
        internal const string EventTypeRivalMonthlyRecalculated = "rival_monthly_recalculated";
        internal const string EventTypeSummerGamesFinalized = "summer_games_finalized";

        internal const string EventEntityKindSingle = "single";
        internal const string EventEntityKindGroup = "group";
        internal const string EventEntityKindIdol = "idol";
        internal const string EventEntityKindStatus = "idol_status";
        internal const string EventEntityKindDatingRelationship = "dating_relationship";
        internal const string EventEntityKindIdolDatingState = "idol_dating_state";
        internal const string EventEntityKindContract = "contract";
        internal const string EventEntityKindShow = "show";
        internal const string EventEntityKindTour = "tour";
        internal const string EventEntityKindElection = "election";
        internal const string EventEntityKindScandal = "scandal";
        internal const string EventEntityKindMedical = "medical";
        internal const string EventEntityKindConcert = "concert";
        internal const string EventEntityKindAward = "award";
        internal const string EventEntityKindPush = "push";
        internal const string EventEntityKindRelationship = "relationship";
        internal const string EventEntityKindClique = "clique";
        internal const string EventEntityKindBullying = "bullying";
        internal const string EventEntityKindPlayerRelationship = "player_relationship";
        internal const string EventEntityKindPlayerDating = "player_dating";
        internal const string EventEntityKindPlayerMarriage = "player_marriage";
        internal const string EventEntityKindLoan = "loan";
        internal const string EventEntityKindBankruptcy = "bankruptcy";
        internal const string EventEntityKindPolicy = "policy";
        internal const string EventEntityKindActivity = "activity";
        internal const string EventEntityKindIdolEarnings = "idol_earnings";
        internal const string EventEntityKindTheater = "theater";
        internal const string EventEntityKindCafe = "cafe";
        internal const string EventEntityKindStaff = "staff";
        internal const string EventEntityKindResearch = "research";
        internal const string EventEntityKindTask = "task";
        internal const string EventEntityKindWish = "wish";
        internal const string EventEntityKindAgencyRoom = "agency_room";
        internal const string EventEntityKindAudition = "audition";
        internal const string EventEntityKindRandomEvent = "random_event";
        internal const string EventEntityKindSubstory = "substory";
        internal const string EventEntityKindEconomy = "economy";
        internal const string EventEntityKindInfluence = "influence";
        internal const string EventEntityKindMentorship = "mentorship";
        internal const string EventEntityKindRivalMarket = "rival_market";
        internal const string EventEntityKindSummerGames = "summer_games";

        internal const string EventSourceSingleAddNewPatch = "patch.singles.AddNewSingle.Postfix";
        internal const string EventSourceSingleReleasePatch = "patch.singles.ReleaseSingle.Postfix";
        internal const string EventSourceSingleChartPopupPatch = "patch.Chart_Song.Set.Postfix";
        internal const string EventSourceSingleChartBackfillPatch = "patch.imdatacore.single_chart_backfill";
        internal const string EventSourceSingleCancelPatch = "patch.singles.CancelSingle.Postfix";
        internal const string EventSourceSingleStatusPatch = "patch.singles._single.SetStatus.Postfix";
        internal const string EventSourceSingleRemoveGirlPatch = "patch.singles._single.RemoveGirl.Postfix";
        internal const string EventSourceSinglePopupSenbatsuConfirmPatch = "patch.SinglePopup_Senbatsu.OnConfirm.Postfix";
        internal const string EventSourceGroupsTransferPatch = "patch.Groups.Transfer.Postfix";
        internal const string EventSourceGroupsDisbandPatch = "patch.Groups.Disband.Postfix";
        internal const string EventSourceNewGroupPopupOnContinuePatch = "patch.New_Group_Popup.OnContinue.Postfix";
        internal const string EventSourceGroupsGroupAddPointsParamPatch = "patch.Groups._group.AddPoints.param.Postfix";
        internal const string EventSourceGroupsGroupSpendPointsPatch = "patch.Groups._group.SpendPoints.Postfix";
        internal const string EventSourceStatusTransitionPatch = "patch.data_girls.girls.SetStatus.Postfix";
        internal const string EventSourceDatingPartnerStatusPatch = "patch.Dating._partner.SetStatus.Postfix";
        internal const string EventSourceIdolDatingStatusPatch = "patch.data_girls.girls._dating_data.SetDatingStatus.Postfix";
        internal const string EventSourceContractActivationPatch = "patch.business.AddActiveProposal.Postfix";
        internal const string EventSourceContractAcceptPatch = "patch.business.Accept.Postfix";
        internal const string EventSourceContractCancellationPatch = "patch.business.CancelContract.Prefix";
        internal const string EventSourceContractNaturalCompletionPatch = "patch.business.CheckActiveProposals.Postfix";
        internal const string EventSourceContractWeeklyEarningsPatch = "patch.business.AddWeeklyEarnings.Postfix";
        internal const string EventSourceContractWeeklyBenefitsPatch = "patch.business.DoWeeklyFans.Postfix";
        internal const string EventSourceContractBreakSingleIdolPatch = "patch.business.BreakContracts.data_girls.girls.Postfix";
        internal const string EventSourceContractBreakEventActorsPatch = "patch.business.BreakContracts.EventActors.Postfix";
        internal const string EventSourceShowAddNewPatch = "patch.Shows.AddNewShow.Postfix";
        internal const string EventSourceShowReleasePatch = "patch.Shows.ReleaseShow.Postfix";
        internal const string EventSourceShowCancelPatch = "patch.Shows.CancelShow.Postfix";
        internal const string EventSourceShowCancelMethodPatch = "patch.Shows._show.Cancel.Postfix";
        internal const string EventSourceShowStatusPatch = "patch.Shows._show.SetStatus.Postfix";
        internal const string EventSourceShowEpisodePatch = "patch.Shows._show.NewEpisode.Postfix";
        internal const string EventSourceShowRemoveGirlPatch = "patch.Shows._show.RemoveGirl.Postfix";
        internal const string EventSourceShowPopupContinuePatch = "patch.Show_Popup.OnContinue.Postfix";
        internal const string EventSourceShowRelaunchStartPatch = "patch.Shows._show.OnRelaunchStart.Postfix";
        internal const string EventSourceShowRelaunchFinishPatch = "patch.Shows._show.OnRelaunchFinish.Postfix";
        internal const string EventSourceTourSetPatch = "patch.SEvent_Tour.SetTour.Postfix";
        internal const string EventSourceTourStartPatch = "patch.SEvent_Tour.StartTour.Postfix";
        internal const string EventSourceTourFinishPatch = "patch.SEvent_Tour.FinishTour.Postfix";
        internal const string EventSourceTourCancelPatch = "patch.SEvent_Tour.CancelTour.Postfix";
        internal const string EventSourceTourStatusPatch = "patch.SEvent_Tour.tour.SetStatus.Postfix";
        internal const string EventSourceElectionSetPatch = "patch.SEvent_SSK.SetSSK.Postfix";
        internal const string EventSourceElectionCancelPatch = "patch.SEvent_SSK.CancelSSK.Postfix";
        internal const string EventSourceElectionGenerateResultsPatch = "patch.SEvent_SSK._SSK.GenerateResults.Postfix";
        internal const string EventSourceElectionSetPlacePatch = "patch.SEvent_SSK._SSK.SetPlace.Postfix";
        internal const string EventSourceElectionStatusPatch = "patch.SEvent_SSK._SSK.SetStatus.Postfix";
        internal const string EventSourceElectionResultPatch = "patch.SEvent_SSK._SSK.Finish.Postfix";
        internal const string EventSourceScandalParameterAddPatch = "patch.data_girls.girls.param.add.Postfix";
        internal const string EventSourceScandalParameterValueSetterPatch = "patch.data_girls.girls.param.val.Setter.Postfix";
        internal const string EventSourceMedicalInjuryPatch = "patch.data_girls.girls.Set_Injured.Postfix";
        internal const string EventSourceMedicalDepressionPatch = "patch.data_girls.girls.Set_Depressed.Postfix";
        internal const string EventSourceMedicalHiatusStartPatch = "patch.data_girls.girls.SendOnHiatus.Postfix";
        internal const string EventSourceMedicalHealPatch = "patch.data_girls.girls.Heal.Postfix";
        internal const string EventSourceMedicalHiatusFinishPatch = "patch.data_girls.girls.FinishHiatus.Postfix";
        internal const string EventSourceConcertSetPatch = "patch.SEvent_Concerts.SetConcert.Postfix";
        internal const string EventSourceConcertStartPatch = "patch.SEvent_Concerts.StartConcert.Postfix";
        internal const string EventSourceConcertFinishPatch = "patch.SEvent_Concerts._concert.Finish.Postfix";
        internal const string EventSourceConcertCancelPatch = "patch.SEvent_Concerts.CancelConcert.Postfix";
        internal const string EventSourceConcertRemoveGirlPatch = "patch.SEvent_Concerts._concert.RemoveGirl.Postfix";
        internal const string EventSourceConcertStatusPatch = "patch.SEvent_Concerts._concert.SetStatus.Postfix";
        internal const string EventSourceConcertNewPopupContinuePatch = "patch.Concert_New_Popup.OnContinue.Postfix";
        internal const string EventSourceAwardNominationsPatch = "patch.Awards.GenerateTempNominations.Postfix";
        internal const string EventSourceAwardResultsPatch = "patch.Awards.SetWins.Postfix";
        internal const string EventSourcePushSetPushesPatch = "patch.Pushes.SetPushes.Postfix";
        internal const string EventSourcePushRemovePatch = "patch.Pushes.RemovePush.Prefix";
        internal const string EventSourcePushOnNewDayPatch = "patch.Pushes.OnNewDay.Postfix";
        internal const string EventSourceIdolRelationshipStartPatch = "patch.Relationships._relationship.StartDating.Postfix";
        internal const string EventSourceIdolRelationshipBreakPatch = "patch.Relationships._relationship.BreakUp.Postfix";
        internal const string EventSourceCliqueAddMemberPatch = "patch.Relationships._clique.AddMember.Postfix";
        internal const string EventSourceCliqueQuitPatch = "patch.Relationships._clique.Quit.Postfix";
        internal const string EventSourceCliqueAddBulliedPatch = "patch.Relationships._clique.AddBulliedGirl.Postfix";
        internal const string EventSourceCliqueStopBullyingPatch = "patch.Relationships._clique.StopBullying.Postfix";
        internal const string EventSourceRelationshipStopBullyingPatch = "patch.Relationships._relationship.StopBullying.Postfix";
        internal const string EventSourceScandalPopupContinuePatch = "patch.ScandalPoints_Popup.OnContinue.Postfix";
        internal const string EventSourcePlayerRelationshipAddPointsPatch = "patch.Relationships_Player.AddPoints.Postfix";
        internal const string EventSourcePlayerDateGoOnDatePatch = "patch.Dating.GoOnDate.Postfix";
        internal const string EventSourcePlayerDateGoOnSpecificDatePatch = "patch.Dating.GoOnSpecificDate.Postfix";
        internal const string EventSourcePlayerMarriageGirlQuitsPatch = "patch.Dating.Marriage_Girl_Quits.Postfix";
        internal const string EventSourcePlayerMarriageAfterPatch = "patch.Dating.AfterMarriage.Postfix";
        internal const string EventSourceIdolRelationshipAddPatch = "patch.Relationships._relationship.Add.Postfix";
        internal const string EventSourceDataGirlsHirePatch = "patch.data_girls.Hire.Postfix";
        internal const string EventSourceDataGirlsGraduationAnnounceConfirmPatch = "patch.data_girls.girls.Graduation_Announce_Confirm.Postfix";
        internal const string EventSourceDataGirlsGraduatePatch = "patch.data_girls.girls.Graduate.Postfix";
        internal const string EventSourceDataGirlsIncreaseSalaryPatch = "patch.data_girls.girls.IncreaseSalary.Postfix";
        internal const string EventSourceDataGirlsLowerSalaryPatch = "patch.data_girls.girls.LowerSalary.Postfix";
        internal const string EventSourceSalaryManualOnSavePatch = "patch.Salary_Manual.OnSave.Postfix";
        internal const string EventSourceLoansAddLoanPatch = "patch.loans.AddLoan.Postfix";
        internal const string EventSourceLoansInitializePatch = "patch.loans._loan.Initialize.Postfix";
        internal const string EventSourceLoansPayOffPatch = "patch.loans._loan.PayOff.Postfix";
        internal const string EventSourceLoansSetBankruptcyDangerPatch = "patch.loans.SetBankruptcyDanger.Postfix";
        internal const string EventSourceBankruptcyCheckBankruptcyPatch = "patch.Bankruptcy.CheckBankruptcy.Postfix";
        internal const string EventSourceBankruptcyCheckScandalPatch = "patch.Bankruptcy.CheckScandal.Postfix";
        internal const string EventSourceBankruptcyTriggerAuditionFailurePatch = "patch.Bankruptcy.TriggerAuditionFailure.Postfix";
        internal const string EventSourcePolicySelectPatch = "patch.policies.value._Select.Postfix";
        internal const string EventSourceActivitiesPerformancePatch = "patch.Activities.Performance.Postfix";
        internal const string EventSourceActivitiesPromotionPatch = "patch.Activities.Promotion.Postfix";
        internal const string EventSourceActivitiesSpaTreatmentPatch = "patch.Activities.SpaTreatment.Postfix";
        internal const string EventSourceDataGirlsEarnPatch = "patch.data_girls.girls.Earn.Postfix";
        internal const string EventSourceTheatersNewTheaterPatch = "patch.Theaters.NewTheater.Postfix";
        internal const string EventSourceTheatersDestroyTheaterPatch = "patch.Theaters.DestroyTheater.Postfix";
        internal const string EventSourceTheatersCompleteDayPatch = "patch.Theaters.CompleteDay.Postfix";
        internal const string EventSourceCafesNewCafePatch = "patch.Cafes.NewCafe.Postfix";
        internal const string EventSourceCafesDestroyCafePatch = "patch.Cafes.DestroyCafe.Postfix";
        internal const string EventSourceCafesRenderCafePatch = "patch.Cafes.RenderCafe.Postfix";
        internal const string EventSourceStaffHirePatch = "patch.staff.Hire.Postfix";
        internal const string EventSourceStaffFirePatch = "patch.staff._staff.Fire.Postfix";
        internal const string EventSourceStaffFireSeverancePatch = "patch.staff._staff.Fire_Severance.Postfix";
        internal const string EventSourceStaffLevelUpPatch = "patch.staff._staff.LevelUp.Postfix";
        internal const string EventSourceResearchCategorySetParamPatch = "patch.Research.category.SetParam.Postfix";
        internal const string EventSourceResearchCategoryBuyPointsPatch = "patch.Research.category.Buy_Points.Postfix";
        internal const string EventSourceSinglesParamLevelUpPatch = "patch.singles._param.LevelUp.Postfix";
        internal const string EventSourceTasksLockRoutePatch = "patch.tasks.LockRoute.Postfix";
        internal const string EventSourceTasksTaskOnCompletePatch = "patch.tasks._task.OnComplete.Postfix";
        internal const string EventSourceTasksTaskOnFailPatch = "patch.tasks._task.OnFail.Postfix";
        internal const string EventSourceTasksTaskDonePatch = "patch.tasks._task.Done.Postfix";
        internal const string EventSourceConcertUseCardPatch = "patch.SEvent_Concerts.UseCard.Postfix";
        internal const string EventSourceConcertCrisisOptionPatch = "patch.Concert_CrisisPopup.Option.Postfix";
        internal const string EventSourceConcertCrisisClosePatch = "patch.Concert_CrisisPopup.Close.Postfix";
        internal const string EventSourceConcertPopupFinishPatch = "patch.Concert_Popup.FinishConcert.Postfix";
        internal const string EventSourceElectionStartPatch = "patch.SEvent_SSK.StartSSK.Postfix";
        internal const string EventSourceDataGirlsWishGeneratePatch = "patch.data_girls.girls.Wish_Generate.Postfix";
        internal const string EventSourceDataGirlsWishFulfillPatch = "patch.data_girls.girls.Wish_Fullfill.Postfix";
        internal const string EventSourceDataGirlsWishDonePatch = "patch.data_girls.girls.Wish_Done.Postfix";
        internal const string EventSourceResearchCategoryAddPointsPatch = "patch.Research.category.AddPoints.Postfix";
        internal const string EventSourceAgencyAddRoomPatch = "patch.agency.addRoom.Postfix";
        internal const string EventSourceAgencyDestroyRoomPatch = "patch.agency.DestroyRoom.Postfix";
        internal const string EventSourceAuditionsGeneratePatch = "patch.Auditions.GenerateAudition.Postfix";
        internal const string EventSourceAuditionsResetCooldownPatch = "patch.Auditions.ResetCooldown.Postfix";
        internal const string EventSourceEventManagerStartEventPatch = "patch.Event_Manager.StartEvent.Postfix";
        internal const string EventSourceEventManagerConcludeEventPatch = "patch.Event_Manager.ConcludeEvent.Postfix";
        internal const string EventSourceSubstoriesStartDialoguePatch = "patch.Substories_Manager.StartDialogue.Postfix";
        internal const string EventSourceResourcesOnNewWeekPatch = "patch.resources.OnNewWeek.Postfix";
        internal const string EventSourceResourcesOnNewDayPatch = "patch.resources.OnNewDay.Postfix";
        internal const string EventSourceDateInfluenceAddBlackmailPatch = "patch.Date_Influence.AddBlackmail.Postfix";
        internal const string EventSourceDateInfluenceBlackmailTriggerPatch = "patch.Date_Influence.Blackmail_Trigger.Postfix";
        internal const string EventSourceGirlsMentorsAddKohaiPatch = "patch.Girls_Mentors.AddKohai.Postfix";
        internal const string EventSourceGirlsMentorsRemoveKohaiPatch = "patch.Girls_Mentors.RemoveKohai.Postfix";
        internal const string EventSourceGirlsMentorsOnNewWeekPatch = "patch.Girls_Mentors.OnNewWeek.Postfix";
        internal const string EventSourceRivalsUpdateTrendsPatch = "patch.Rivals.UpdateTrends.Postfix";
        internal const string EventSourceRivalsOnNewMonthPatch = "patch.Rivals.OnNewMonth.Postfix";
        internal const string EventSourceSummerGamesOnProceedPatch = "patch.Summer_Games._data.OnProceed.Postfix";
        internal const string EventSourceCustomApi = "api.IMDataCore.CustomEvent";
        internal const string EventSourceSaveFlushPatch = "patch.SaveManager.SaveData.Prefix";

        internal const string ContractBreakContextSingleIdol = "single_idol";
        internal const string ContractBreakContextEventActors = "event_actors";
        internal const string ContractWeeklyAccrualActionEarningsApplied = "earnings_applied";
        internal const string ContractWeeklyAccrualActionBenefitsApplied = "benefits_applied";
        internal const int ContractWeeklyAccrualTrainingPointsPerTick = 1;
        internal const string SingleLifecycleActionCreated = "created";
        internal const string SingleLifecycleActionCancelled = "cancelled";
        internal const string GroupLifecycleActionCreated = "created";
        internal const string GroupLifecycleActionDisbanded = "disbanded";
        internal const string ShowLifecycleActionCreated = "created";
        internal const string ShowLifecycleActionReleased = "released";
        internal const string ShowLifecycleActionCancelled = "cancelled";
        internal const string ShowLifecycleActionRelaunchStarted = "relaunch_started";
        internal const string ShowLifecycleActionRelaunchFinished = "relaunch_finished";
        internal const string TourLifecycleActionCreated = "created";
        internal const string TourLifecycleActionStarted = "started";
        internal const string TourLifecycleActionFinished = "finished";
        internal const string TourLifecycleActionCancelled = "cancelled";
        internal const string ElectionLifecycleActionCreated = "created";
        internal const string ElectionLifecycleActionFinished = "finished";
        internal const string ElectionLifecycleActionCancelled = "cancelled";
        internal const string IdolLifecycleActionHired = "hired";
        internal const string IdolLifecycleActionGraduationAnnounced = "graduation_announced";
        internal const string IdolLifecycleActionGraduated = "graduated";
        internal const string LoanLifecycleActionAdded = "added";
        internal const string LoanLifecycleActionInitialized = "initialized";
        internal const string LoanLifecycleActionPaidOff = "paid_off";
        internal const string EarningsSourceUnknown = "unknown";
        internal const string EarningsSourceActivitiesPerformance = "activities.performance";
        internal const string EarningsSourceActivitiesPromotion = "activities.promotion";
        internal const string EarningsSourceActivitiesSpaTreatment = "activities.spa_treatment";
        internal const string EarningsSourceConcertFinish = "concert.finish";
        internal const string EarningsSourceBusinessAccept = "business.accept";
        internal const string EarningsSourceBusinessWeekly = "business.weekly_earnings";
        internal const string EarningsSourceTourFinish = "tour.finish";
        internal const string EarningsSourceShowRevenue = "show.revenue";
        internal const string EarningsSourceSingleRelease = "single.release";
        internal const string TheaterLifecycleActionCreated = "created";
        internal const string TheaterLifecycleActionDestroyed = "destroyed";
        internal const string CafeLifecycleActionCreated = "created";
        internal const string CafeLifecycleActionDestroyed = "destroyed";
        internal const string ElectionLifecycleActionStarted = "started";
        internal const string TaskLifecycleActionCompleted = "completed";
        internal const string TaskLifecycleActionFailed = "failed";
        internal const string TaskLifecycleActionDone = "done";
        internal const string WishLifecycleActionGenerated = "generated";
        internal const string WishLifecycleActionFulfilled = "fulfilled";
        internal const string WishLifecycleActionDone = "done";
        internal const string StaffLifecycleActionHired = "hired";
        internal const string StaffLifecycleActionFired = "fired";
        internal const string StaffLifecycleActionFiredSeverance = "fired_severance";
        internal const string StaffLifecycleActionLevelUp = "level_up";
        internal const string AgencyRoomLifecycleActionBuilt = "built";
        internal const string AgencyRoomLifecycleActionDestroyed = "destroyed";
        internal const string SubstoryLifecycleActionStarted = "started";
        internal const string SubstoryLifecycleActionDelayed = "delayed";
        internal const string SubstoryLifecycleActionCompleted = "completed";
        internal const string MentorshipLifecycleActionStarted = "started";
        internal const string MentorshipLifecycleActionEnded = "ended";
        internal const string MentorshipLifecycleActionWeeklyTick = "weekly_tick";
        internal const string RivalLifecycleActionTrendsUpdated = "trends_updated";
        internal const string RivalLifecycleActionMonthlyRecalculated = "monthly_recalculated";
        internal const string InfluenceLifecycleActionBlackmailQueued = "blackmail_queued";
        internal const string InfluenceLifecycleActionBlackmailTriggered = "blackmail_triggered";
        internal const string TheaterScheduleFanTypeEveryone = "everyone";
        internal const string SubstoryFlagBankruptcyBailOut = "bankruptcy_bail_out";
        internal const string SubstoryFlagStoryRecruit = "story_recruit";
        internal const string SubstoryFlagGameOverBankruptcy = "game_over_bankruptcy";
        internal const string SubstoryFlagFirstScandalPoints = "first_scandal_points";
        internal const string SubstoryFlagGameOverWarningScandalPoints = "game_over_warning_scandal_points";
        internal const string SubstoryFlagScandalPointsParents = "scandal_points_parents";
        internal const string SubstoryFlagGameOverScandalPoints = "game_over_scandal_points";
        internal const string ConcertCrisisChoiceSafe = "safe";
        internal const string ConcertCrisisChoiceRisky = "risky";
        internal const string ConcertCrisisChoiceUnknown = "unknown";
        internal const string SalaryChangeActionIncreased = "increased";
        internal const string SalaryChangeActionLowered = "lowered";
        internal const string SalaryChangeActionManualSet = "manual_set";
        internal const string ScandalMutationSourceParameterAdd = "parameter_add";
        internal const string ScandalMutationSourceParameterValueSetter = "parameter_value_setter";
        internal const string MedicalEventTypeInjury = "injury";
        internal const string MedicalEventTypeDepression = "depression";
        internal const string MedicalEventTypeHiatusStarted = "hiatus_started";
        internal const string MedicalEventTypeHealed = "healed";
        internal const string MedicalEventTypeHiatusFinished = "hiatus_finished";
        internal const string PushLifecycleActionStarted = "started";
        internal const string PushLifecycleActionEnded = "ended";
        internal const string PushLifecycleActionDayIncrement = "day_increment";
        internal const string PushEntityIdentifierPrefix = "slot";
        internal const string PushDayIncrementIdempotencyPrefix = "push_day";
        internal const string BullyingLifecycleIdempotencyPrefix = "bullying";
        internal const string ShowLifecycleIdempotencyPrefix = "show";
        internal const string TourLifecycleIdempotencyPrefix = "tour";
        internal const string ElectionLifecycleIdempotencyPrefix = "election";
        internal const string RelationshipBreakReasonGeneric = "generic_breakup";
        internal const string DateInteractionTypeGoOnDate = "go_on_date";
        internal const string DateInteractionTypeGoOnSpecificDate = "go_on_specific_date";
        internal const string DateInteractionResultTokenPublicDate = "pub";
        internal const string DateInteractionResultTokenRoutineDate = "generic";
        internal const string DateInteractionResultTokenNone = "none";
        internal const string DateInteractionResultTokenDeferred = "deferred";
        internal const string DateInteractionResultTokenSeparator = "|";
        internal const string DateInteractionResultSummaryCodeDialogueFollowup = "dialogue_followup";
        internal const string DateInteractionResultSummaryCodeNoSpecialResult = "no_special_result";
        internal const string DateInteractionResultSummaryCodePublicDate = "public_date";
        internal const string DateInteractionResultSummaryCodeRoutineDate = "routine_date";
        internal const string DateInteractionResultSummaryCodeMultiResult = "multi_result";
        internal const string CliqueSignatureMemberSeparator = "-";
        internal const string UnknownCliqueEntityIdentifier = "unknown_clique";
        internal const string UnknownBullyingEntityIdentifier = "unknown_bullying";
        internal const string ScandalPointsPopupGroupEntityIdentifier = "group";
        internal const string PlayerMarriageEntityIdentifier = "player_marriage";
        internal const string PlayerMarriageStageGirlQuits = "girl_quits";
        internal const string PlayerMarriageStageAfterMarriage = "after_marriage";
        internal const string VariablesKeyKidsString = "kids_string";
        internal const string VariablesKeyCareerStringOne = "career_string_1";
        internal const string VariablesKeyCareerStringTwo = "career_string_2";
        internal const string VariablesKeyRelationshipOutcomeString = "relationship_outcome_string";
        internal const string VariablesKeyCustodyString = "custody_string";
        internal const int WishEffectMinimumInitializedYear = 1902;

        internal const string StatusCodeUnknown = "unknown";
        internal const string StatusCodeNormal = "normal";
        internal const string StatusCodePractice = "practice";
        internal const string StatusCodeScene = "scene";
        internal const string StatusCodeInjured = "injured";
        internal const string StatusCodeDepressed = "depressed";
        internal const string StatusCodeHiatus = "hiatus";
        internal const string StatusCodeAnnouncedGraduation = "announced_graduation";
        internal const string StatusCodeGraduated = "graduated";

        internal static readonly HashSet<string> StatusCodesTrackedAsWindows = new HashSet<string>
        {
            StatusCodeInjured,
            StatusCodeDepressed,
            StatusCodeHiatus
        };

        internal const string SqlCreateTableMeta = "CREATE TABLE IF NOT EXISTS meta (meta_key TEXT PRIMARY KEY, meta_value TEXT NOT NULL);";

        internal const string SqlCreateTableEventStream =
            "CREATE TABLE IF NOT EXISTS event_stream (" +
            "event_id INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "save_key TEXT NOT NULL, " +
            "game_date_key INTEGER NOT NULL, " +
            "game_datetime TEXT NOT NULL, " +
            "idol_id INTEGER, " +
            "entity_kind TEXT NOT NULL, " +
            "entity_id TEXT, " +
            "event_type TEXT NOT NULL, " +
            "source_patch TEXT NOT NULL, " +
            "namespace_id TEXT NOT NULL DEFAULT '', " +
            "payload_json TEXT NOT NULL" +
            ");";

        internal const string SqlCreateTableSingleParticipation =
            "CREATE TABLE IF NOT EXISTS single_participation (" +
            "save_key TEXT NOT NULL, " +
            "single_id INTEGER NOT NULL, " +
            "idol_id INTEGER NOT NULL, " +
            "row_index INTEGER NOT NULL, " +
            "position_index INTEGER NOT NULL, " +
            "is_center INTEGER NOT NULL, " +
            "release_date TEXT NOT NULL, " +
            "PRIMARY KEY(save_key, single_id, idol_id)" +
            ");";

        internal const string SqlCreateTableStatusWindow =
            "CREATE TABLE IF NOT EXISTS status_window (" +
            "window_id INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "save_key TEXT NOT NULL, " +
            "idol_id INTEGER NOT NULL, " +
            "status_type TEXT NOT NULL, " +
            "start_date TEXT NOT NULL, " +
            "end_date TEXT" +
            ");";

        internal const string SqlCreateTableShowCastWindow =
            "CREATE TABLE IF NOT EXISTS show_cast_window (" +
            "window_id INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "save_key TEXT NOT NULL, " +
            "show_id TEXT NOT NULL, " +
            "idol_id INTEGER NOT NULL, " +
            "start_date TEXT NOT NULL, " +
            "end_date TEXT, " +
            "end_reason TEXT NOT NULL DEFAULT ''" +
            ");";

        internal const string SqlCreateTableContractWindow =
            "CREATE TABLE IF NOT EXISTS contract_window (" +
            "window_id INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "save_key TEXT NOT NULL, " +
            "contract_key TEXT NOT NULL, " +
            "idol_id INTEGER NOT NULL, " +
            "start_date TEXT NOT NULL, " +
            "end_date TEXT, " +
            "end_reason TEXT NOT NULL DEFAULT ''" +
            ");";

        internal const string SqlCreateTableRelationshipWindow =
            "CREATE TABLE IF NOT EXISTS relationship_window (" +
            "window_id INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "save_key TEXT NOT NULL, " +
            "relationship_key TEXT NOT NULL, " +
            "idol_id INTEGER NOT NULL, " +
            "relationship_type TEXT NOT NULL, " +
            "start_date TEXT NOT NULL, " +
            "end_date TEXT, " +
            "end_reason TEXT NOT NULL DEFAULT ''" +
            ");";

        internal const string SqlCreateTableTourParticipation =
            "CREATE TABLE IF NOT EXISTS tour_participation (" +
            "row_id INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "save_key TEXT NOT NULL, " +
            "tour_id TEXT NOT NULL, " +
            "idol_id INTEGER NOT NULL, " +
            "lifecycle_action TEXT NOT NULL, " +
            "event_date TEXT NOT NULL, " +
            "UNIQUE(save_key, tour_id, idol_id, lifecycle_action)" +
            ");";

        internal const string SqlCreateTableAwardResultProjection =
            "CREATE TABLE IF NOT EXISTS award_result_projection (" +
            "row_id INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "save_key TEXT NOT NULL, " +
            "award_key TEXT NOT NULL, " +
            "idol_id INTEGER NOT NULL, " +
            "event_date TEXT NOT NULL, " +
            "UNIQUE(save_key, award_key, idol_id)" +
            ");";

        internal const string SqlCreateTableElectionResultProjection =
            "CREATE TABLE IF NOT EXISTS election_result_projection (" +
            "row_id INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "save_key TEXT NOT NULL, " +
            "election_id TEXT NOT NULL, " +
            "idol_id INTEGER NOT NULL, " +
            "event_date TEXT NOT NULL, " +
            "UNIQUE(save_key, election_id, idol_id)" +
            ");";

        internal const string SqlCreateTablePushWindow =
            "CREATE TABLE IF NOT EXISTS push_window (" +
            "window_id INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "save_key TEXT NOT NULL, " +
            "slot_key TEXT NOT NULL, " +
            "idol_id INTEGER NOT NULL, " +
            "start_date TEXT NOT NULL, " +
            "end_date TEXT, " +
            "last_days_in_slot INTEGER NOT NULL DEFAULT -1, " +
            "end_reason TEXT NOT NULL DEFAULT ''" +
            ");";

        internal const string SqlCreateTableCustomData =
            "CREATE TABLE IF NOT EXISTS custom_data (" +
            "save_key TEXT NOT NULL, " +
            "namespace_id TEXT NOT NULL, " +
            "data_key TEXT NOT NULL, " +
            "value_json TEXT NOT NULL, " +
            "updated_utc TEXT NOT NULL, " +
            "PRIMARY KEY(save_key, namespace_id, data_key)" +
            ");";

        internal const string SqlCreateIndexEventIdolDate =
            "CREATE INDEX IF NOT EXISTS idx_event_stream_save_idol_date ON event_stream(save_key, idol_id, game_date_key DESC, event_id DESC);";
        internal const string SqlCreateIndexEventTypeDate =
            "CREATE INDEX IF NOT EXISTS idx_event_stream_save_type_date ON event_stream(save_key, event_type, game_date_key DESC, event_id DESC);";
        internal const string SqlCreateIndexSingleParticipation =
            "CREATE INDEX IF NOT EXISTS idx_single_participation_save_idol ON single_participation(save_key, idol_id, release_date DESC);";
        internal const string SqlCreateIndexStatusWindow =
            "CREATE INDEX IF NOT EXISTS idx_status_window_save_idol_status ON status_window(save_key, idol_id, status_type, start_date DESC);";
        internal const string SqlCreateIndexShowCastWindow =
            "CREATE INDEX IF NOT EXISTS idx_show_cast_window_save_idol ON show_cast_window(save_key, idol_id, show_id, start_date DESC);";
        internal const string SqlCreateIndexContractWindow =
            "CREATE INDEX IF NOT EXISTS idx_contract_window_save_idol ON contract_window(save_key, idol_id, start_date DESC);";
        internal const string SqlCreateIndexRelationshipWindow =
            "CREATE INDEX IF NOT EXISTS idx_relationship_window_save_idol ON relationship_window(save_key, idol_id, relationship_type, start_date DESC);";
        internal const string SqlCreateIndexTourParticipation =
            "CREATE INDEX IF NOT EXISTS idx_tour_participation_save_idol ON tour_participation(save_key, idol_id, event_date DESC);";
        internal const string SqlCreateIndexAwardResultProjection =
            "CREATE INDEX IF NOT EXISTS idx_award_result_projection_save_idol ON award_result_projection(save_key, idol_id, event_date DESC);";
        internal const string SqlCreateIndexElectionResultProjection =
            "CREATE INDEX IF NOT EXISTS idx_election_result_projection_save_idol ON election_result_projection(save_key, idol_id, event_date DESC);";
        internal const string SqlCreateIndexPushWindow =
            "CREATE INDEX IF NOT EXISTS idx_push_window_save_idol ON push_window(save_key, idol_id, start_date DESC);";
        internal const string SqlCreateIndexCustomData =
            "CREATE INDEX IF NOT EXISTS idx_custom_data_save_namespace ON custom_data(save_key, namespace_id);";
        internal const string SqlInsertMeta = "INSERT OR REPLACE INTO meta(meta_key, meta_value) VALUES(@meta_key, @meta_value);";

        internal const string SqlInsertEvent =
            "INSERT INTO event_stream(" +
            "save_key, game_date_key, game_datetime, idol_id, entity_kind, entity_id, event_type, source_patch, namespace_id, payload_json" +
            ") VALUES (" +
            "@save_key, @game_date_key, @game_datetime, @idol_id, @entity_kind, @entity_id, @event_type, @source_patch, @event_namespace_id, @payload_json" +
            ");";

        internal const string SqlUpsertSingleParticipation =
            "INSERT OR REPLACE INTO single_participation(" +
            "save_key, single_id, idol_id, row_index, position_index, is_center, release_date" +
            ") VALUES (" +
            "@save_key, @single_id, @idol_id, @row_index, @position_index, @is_center, @release_date" +
            ");";

        internal const string SqlCloseStatusWindow =
            "UPDATE status_window SET end_date = @end_date " +
            "WHERE save_key = @save_key AND idol_id = @idol_id AND status_type = @status_type AND end_date IS NULL;";

        internal const string SqlOpenStatusWindowIfMissing =
            "INSERT INTO status_window(save_key, idol_id, status_type, start_date, end_date) " +
            "SELECT @save_key, @idol_id, @status_type, @start_date, NULL " +
            "WHERE NOT EXISTS(" +
            "SELECT 1 FROM status_window " +
            "WHERE save_key = @save_key AND idol_id = @idol_id AND status_type = @status_type AND end_date IS NULL" +
            ");";

        internal const string SqlOpenShowCastWindowIfMissing =
            "INSERT INTO show_cast_window(save_key, show_id, idol_id, start_date, end_date, end_reason) " +
            "SELECT @save_key, @show_id, @idol_id, @start_date, NULL, '' " +
            "WHERE NOT EXISTS(" +
            "SELECT 1 FROM show_cast_window " +
            "WHERE save_key = @save_key AND show_id = @show_id AND idol_id = @idol_id AND end_date IS NULL" +
            ");";

        internal const string SqlCloseShowCastWindow =
            "UPDATE show_cast_window SET end_date = @end_date, end_reason = @end_reason " +
            "WHERE save_key = @save_key AND show_id = @show_id AND idol_id = @idol_id AND end_date IS NULL;";

        internal const string SqlOpenContractWindowIfMissing =
            "INSERT INTO contract_window(save_key, contract_key, idol_id, start_date, end_date, end_reason) " +
            "SELECT @save_key, @contract_key, @idol_id, @start_date, NULL, '' " +
            "WHERE NOT EXISTS(" +
            "SELECT 1 FROM contract_window " +
            "WHERE save_key = @save_key AND contract_key = @contract_key AND idol_id = @idol_id AND end_date IS NULL" +
            ");";

        internal const string SqlCloseContractWindow =
            "UPDATE contract_window SET end_date = @end_date, end_reason = @end_reason " +
            "WHERE save_key = @save_key AND contract_key = @contract_key AND idol_id = @idol_id AND end_date IS NULL;";

        internal const string SqlOpenRelationshipWindowIfMissing =
            "INSERT INTO relationship_window(save_key, relationship_key, idol_id, relationship_type, start_date, end_date, end_reason) " +
            "SELECT @save_key, @relationship_key, @idol_id, @relationship_type, @start_date, NULL, '' " +
            "WHERE NOT EXISTS(" +
            "SELECT 1 FROM relationship_window " +
            "WHERE save_key = @save_key AND relationship_key = @relationship_key AND idol_id = @idol_id AND relationship_type = @relationship_type AND end_date IS NULL" +
            ");";

        internal const string SqlCloseRelationshipWindow =
            "UPDATE relationship_window SET end_date = @end_date, end_reason = @end_reason " +
            "WHERE save_key = @save_key AND relationship_key = @relationship_key AND idol_id = @idol_id AND relationship_type = @relationship_type AND end_date IS NULL;";

        internal const string SqlUpsertTourParticipationProjection =
            "INSERT OR REPLACE INTO tour_participation(save_key, tour_id, idol_id, lifecycle_action, event_date) " +
            "VALUES(@save_key, @tour_id, @idol_id, @lifecycle_action, @event_date);";

        internal const string SqlUpsertAwardResultProjection =
            "INSERT OR REPLACE INTO award_result_projection(save_key, award_key, idol_id, event_date) " +
            "VALUES(@save_key, @award_key, @idol_id, @event_date);";

        internal const string SqlUpsertElectionResultProjection =
            "INSERT OR REPLACE INTO election_result_projection(save_key, election_id, idol_id, event_date) " +
            "VALUES(@save_key, @election_id, @idol_id, @event_date);";

        internal const string SqlOpenPushWindowIfMissing =
            "INSERT INTO push_window(save_key, slot_key, idol_id, start_date, end_date, last_days_in_slot, end_reason) " +
            "SELECT @save_key, @slot_key, @idol_id, @start_date, NULL, @push_days_in_slot, '' " +
            "WHERE NOT EXISTS(" +
            "SELECT 1 FROM push_window " +
            "WHERE save_key = @save_key AND slot_key = @slot_key AND idol_id = @idol_id AND end_date IS NULL" +
            ");";

        internal const string SqlClosePushWindow =
            "UPDATE push_window SET end_date = @end_date, last_days_in_slot = @push_days_in_slot, end_reason = @end_reason " +
            "WHERE save_key = @save_key AND slot_key = @slot_key AND idol_id = @idol_id AND end_date IS NULL;";

        internal const string SqlTouchPushWindow =
            "UPDATE push_window SET last_days_in_slot = @push_days_in_slot " +
            "WHERE save_key = @save_key AND slot_key = @slot_key AND idol_id = @idol_id AND end_date IS NULL;";

        internal const string SqlUpsertCustomData =
            "INSERT OR REPLACE INTO custom_data(" +
            "save_key, namespace_id, data_key, value_json, updated_utc" +
            ") VALUES (" +
            "@save_key, @namespace_id, @data_key, @value_json, @updated_utc" +
            ");";

        internal const string SqlSelectCustomData =
            "SELECT value_json FROM custom_data WHERE save_key = @save_key AND namespace_id = @namespace_id AND data_key = @data_key;";

        internal const string SqlDeleteCustomData =
            "DELETE FROM custom_data WHERE save_key = @save_key AND namespace_id = @namespace_id AND data_key = @data_key;";

        internal const string SqlCountCustomKeysForNamespace =
            "SELECT COUNT(*) FROM custom_data WHERE save_key = @save_key AND namespace_id = @namespace_id;";

        internal const string SqlSumCustomValueLengthsForNamespace =
            "SELECT COALESCE(SUM(LENGTH(value_json)), 0) FROM custom_data WHERE save_key = @save_key AND namespace_id = @namespace_id;";

        internal const string SqlLengthForCustomValue =
            "SELECT LENGTH(value_json) FROM custom_data WHERE save_key = @save_key AND namespace_id = @namespace_id AND data_key = @data_key;";

        internal const string SqlSelectLastInsertRowId = "SELECT last_insert_rowid();";

        internal const string SqlReadRecentEventsForIdol =
            "SELECT event_id, game_date_key, game_datetime, idol_id, entity_kind, entity_id, event_type, source_patch, payload_json, namespace_id " +
            "FROM event_stream " +
            "WHERE save_key = @save_key AND (idol_id = @idol_id OR idol_id < 0 OR idol_id IS NULL) " +
            "ORDER BY CASE WHEN idol_id = @idol_id THEN 0 ELSE 1 END, game_date_key DESC, event_id DESC " +
            "LIMIT @limit_count;";

        internal const string SqlSelectEventNamespaceColumnExists =
            "SELECT COUNT(*) FROM pragma_table_info('event_stream') WHERE name = 'namespace_id';";

        internal const string SqlAlterTableEventStreamAddNamespaceIdentifier =
            "ALTER TABLE event_stream ADD COLUMN namespace_id TEXT NOT NULL DEFAULT '';";

        internal const int SenbatsuCenterPositionIndex = 0;
        internal const int ZeroBasedListStartIndex = 0;
        internal const int MinimumValidIdolIdentifier = 0;
        internal const int MinimumNonEmptyCollectionCount = 1;
        internal const int MinimumRelationshipPairMemberCount = 2;
        internal const int LastElementOffsetFromCount = 1;
        internal const int PreviousElementOffsetFromCount = 2;
        internal const int HexCharactersPerByte = 2;
        internal const int DisabledCenterFlag = 0;
        internal const int EnabledCenterFlag = 1;
        internal const int InvalidIdValue = -1;
        internal const int SingleChartMonthOffsetFromReleaseDate = 1;
        internal const int DefaultScandalGameOverThreshold = 20;
        internal const int UninitializedDateKey = int.MinValue;
        internal const float ScandalPointsDeltaEpsilon = 0.0001f;
        internal static readonly float ScandalPointsRawStateUnavailableValue = float.NaN;
        internal const int EventStreamColumnIndexEventId = 0;
        internal const int EventStreamColumnIndexGameDateKey = 1;
        internal const int EventStreamColumnIndexGameDateTime = 2;
        internal const int EventStreamColumnIndexIdolId = 3;
        internal const int EventStreamColumnIndexEntityKind = 4;
        internal const int EventStreamColumnIndexEntityId = 5;
        internal const int EventStreamColumnIndexEventType = 6;
        internal const int EventStreamColumnIndexSourcePatch = 7;
        internal const int EventStreamColumnIndexPayloadJson = 8;
        internal const int EventStreamColumnIndexNamespaceIdentifier = 9;

        internal const int MinimumQueueSizeForFlush = 1;
        internal const int MinimumTokenLength = 1;
        internal const int EventKindMaximumLength = 64;
        internal const int EventIdMaximumLength = 128;
        internal const int EventTypeMaximumLength = 64;
        internal const int EventSourceMaximumLength = 128;

        internal const string EmptyJsonObject = "{}";
        internal const string DefaultEntityIdentifier = "";
        internal const string MessageStorageInitializationFailure = "Storage initialization failed: ";
        internal const string MessageStorageSaveSwitchFailure = "Storage save switch failed: ";
        internal const string MessageSaveLoadInitializationFailurePrefix = "Save-load initialization failed: ";
        internal const string MessageSaveLoadRollbackFailurePrefix = "Save-load rollback failed: ";
        internal const string MessageSaveWritePreparationFailurePrefix = "Save-write preparation failed: ";
        internal const string MessageSaveDataMigrationFailedPrefix = "Save-data migration failed: ";
        internal const string MessageLegacySaveDataMigrationFailedPrefix = "Legacy save-data migration failed: ";
        internal const string MessageCoreInitializedForSaveKeyPrefix = "IM Data Core initialized for save key '";
        internal const string MessageCoreInitializedForSaveKeySuffix = "'.";
        internal const string MessageSaveKeyDerivationFailurePrefix = "Failed to derive save key from player data. ";
        internal const string MessageSqliteEngineInitializedProviderPrefix = "SQLite engine initialized with provider '";
        internal const string MessageSqliteEngineInitializedProviderSuffix = "'.";
        internal const string MessageSqliteUnavailableFallbackPrefix = "SQLite storage unavailable; falling back to flat-file storage: ";
        internal const string MessageSystemDataUnavailableFallback = "System.Data is unavailable; using flat-file storage.";
        internal const string MessageRuntimeDependencyProbeFailedPrefix = "SQLite runtime dependency probe failed: ";
        internal const string MessageLoadedSystemDataAssemblyPrefix = "Loaded System.Data assembly from: ";
        internal const string MessageLoadedSqliteProviderAssemblyPrefix = "Loaded SQLite provider assembly from: ";
        internal const string MessageFlatFileEngineInitialized = "Flat-file storage engine initialized.";
        internal const string MessageFlatFileReadFailedPrefix = "Flat-file read failed: ";
        internal const string MessageFlatFileWriteFailedPrefix = "Flat-file write failed: ";
        internal const string MessageDatabasePathEmpty = "Database path cannot be empty.";
        internal const string MessageConnectionInstanceCreationFailed = "Failed to create database connection instance.";
        internal const string MessageStorageEngineDisposed = "Storage engine is disposed.";
        internal const string MessagePersistBatchFailedPrefix = "PersistBatch failed: ";
        internal const string MessageJsonValueNull = "JSON value cannot be null.";
        internal const string MessageJsonValueTooLong = "JSON value exceeds maximum allowed length.";
        internal const string MessageNamespaceKeyQuotaExceeded = "Namespace key quota exceeded.";
        internal const string MessageNamespaceDataBudgetExceeded = "Namespace data budget exceeded.";
        internal const string MessageTrySetCustomDataFailedPrefix = "TrySetCustomData failed: ";
        internal const string MessageTryGetCustomDataFailedPrefix = "TryGetCustomData failed: ";
        internal const string MessageTryRemoveCustomDataFailedPrefix = "TryRemoveCustomData failed: ";
        internal const string MessageTryReadRecentEventsFailedPrefix = "TryReadRecentEventsForIdol failed: ";
        internal const string MessageTryRollbackToGameDateTimeFailedPrefix = "TryRollbackToGameDateTime failed: ";
        internal const string MessageTryRemapSaveKeyFailedPrefix = "TryRemapSaveKey failed: ";
        internal const string MessageDatabaseDisposeErrorPrefix = "Error while disposing database connection: ";
        internal const string MessageNoCompatibleSqliteProvider = "No compatible SQLite provider was found in the current runtime.";
        internal const string MessageRecoveryAttemptFailedPrefix = " Recovery attempt failed: ";
        internal const string MessageRecoveredSqliteInitializationPrefix = "Recovered SQLite initialization by recreating DB files after failure: ";
        internal const string MessageFailedEnableWalFallbackDeletePrefix = "Failed to enable WAL journal mode; retrying DELETE mode: ";
        internal const string MessageSqliteOptionalStatementFailedPrefix = "SQLite optional statement failed. SQL: ";
        internal const string MessageSqliteOptionalStatementFailedDetailsSeparator = " Details: ";
        internal const string MessageSqliteParameterNotFoundPrefix = "SQLite parameter not found: ";
        internal const string MessageSqliteResultCodePrefix = "SQLite result code ";
        internal const string MessageSqliteResultMessageOpeningToken = " (";
        internal const string MessageSqliteDatabaseMessagePrefix = ". DB message: ";
        internal const string MessageSqliteSqlPreviewPrefix = " SQL: ";
        internal const string LogSeparatorColonSpace = ": ";
        internal const string MessageNamespaceInvalid = "Namespace must contain only safe token characters and meet minimum length.";
        internal const string MessageNamespaceAlreadyClaimed = "Namespace is already registered by another assembly.";
        internal const string MessageSessionNull = "Session cannot be null.";
        internal const string MessageSessionNotRegistered = "Session is not registered.";
        internal const string MessageSessionNamespaceMismatch = "Session namespace does not match registered namespace.";
        internal const string MessageSessionTokenMismatch = "Session token is invalid for the namespace.";
        internal const string MessageSessionAssemblyMismatch = "Calling assembly does not own this namespace session.";
        internal const string MessageDataKeyInvalid = "Data key must contain only safe token characters and meet minimum length.";
        internal const string MessageEntityKindInvalid = "Entity kind must contain only safe token characters and meet minimum length.";
        internal const string MessageEventTypeInvalid = "Event type must contain only safe token characters and meet minimum length.";
        internal const string MessagePayloadNull = "Payload JSON cannot be null.";
        internal const string MessageControllerNotInitialized = "IM Data Core is not initialized.";
        internal const string MessageStorageUnavailable = "Storage engine is not available.";
        internal const string MessageIdolInvalid = "Idol id must be non-negative.";
        internal const string MessageFlushFailed = "Flush failed: ";

        internal const string AssemblyIdentityStrongFormat = "{0}|mvid={1}|location={2}|sha256={3}";
        internal const string UnknownAssemblyLocation = "unknown_assembly_location";
        internal const string UnknownAssemblyModuleVersionIdentifier = "unknown_assembly_module_version_identifier";
        internal const string UnknownAssemblyContentHash = "unknown_assembly_content_hash";
        internal const string GuidDefaultFormat = "D";
        internal const string ByteToLowerHexFormat = "x2";
        internal const string ReflectionFactoryInstanceFieldName = "Instance";
        internal const string ReflectionScandalPointsPopupPointsFieldName = "Points";
        internal const string ReflectionBankruptcyScandalGameOverThresholdFieldName = "Scandal_Points_For_Game_Over";
        internal const string ReflectionConcertCrisisPopupConcertFieldName = "Concert";
        internal const string ReflectionConcertCrisisPopupAccidentFieldName = "Accident";
        internal const string ReflectionConcertCrisisPopupResultFieldName = "Result";
        internal const string HarmonyPopupManagerStartMethodName = "Start";
        internal const string HarmonyBusinessCheckActiveProposalsMethodName = "CheckActiveProposals";
        internal const string HarmonyLoansSetBankruptcyDangerMethodName = "SetBankruptcyDanger";
        internal const string HarmonyBankruptcyCheckBankruptcyMethodName = "CheckBankruptcy";
        internal const string HarmonyBankruptcyCheckScandalMethodName = "CheckScandal";
        internal const string HarmonyAgencyAddRoomMethodName = "addRoom";
        internal const string HarmonyAgencyDestroyRoomMethodName = "DestroyRoom";
        internal const string HarmonyAuditionsGenerateMethodName = "GenerateAudition";
        internal const string HarmonyAuditionsResetCooldownMethodName = "ResetCooldown";
        internal const string HarmonySubstoriesStartDialogueMethodName = "StartDialogue";
        internal const string HarmonyResourcesOnNewWeekMethodName = "OnNewWeek";
        internal const string HarmonyResourcesOnNewDayMethodName = "OnNewDay";
        internal const string HarmonyDateInfluenceBlackmailTriggerMethodName = "Blackmail_Trigger";
        internal const string HarmonyGirlsMentorsOnNewWeekMethodName = "OnNewWeek";
        internal const string HarmonyRivalsUpdateTrendsMethodName = "UpdateTrends";
        internal const string HarmonyRivalsOnNewMonthMethodName = "OnNewMonth";
        internal const string HarmonySummerGamesProceedMethodName = "OnProceed";
        internal const string HarmonyPoliciesValueSelectMethodName = "_Select";
        internal const string HarmonyTheatersCompleteDayMethodName = "CompleteDay";
        internal const string HarmonyCafesRenderCafeMethodName = "RenderCafe";
        internal const string HarmonyConcertCrisisOptionMethodName = "Option";
        internal const string HarmonyConcertCrisisCloseMethodName = "Close";
        internal const string HarmonyShowsShowSetRevenueMethodName = "SetRevenue";
        internal const string HarmonySinglesAddMoneyMethodName = "AddMoney";
        internal const string HarmonyPushesOnNewDayMethodName = "OnNewDay";
        internal const string HarmonyDataGirlsParameterValuePropertyName = "val";
        internal const string JsonEscapedUnicodePrefix = "\\u";
        internal const string FourDigitLowerHexFormat = "x4";

        internal const string SqliteWriteAheadLogFileSuffix = "-wal";
        internal const string SqliteSharedMemoryFileSuffix = "-shm";

        internal const string SqlParameterSaveKey = "@save_key";
        internal const string SqlParameterNamespaceIdentifier = "@namespace_id";
        internal const string SqlParameterDataKey = "@data_key";
        internal const string SqlParameterValueJson = "@value_json";
        internal const string SqlParameterUpdatedUtc = "@updated_utc";
        internal const string SqlParameterIdolId = "@idol_id";
        internal const string SqlParameterLimitCount = "@limit_count";
        internal const string SqlParameterGameDateKey = "@game_date_key";
        internal const string SqlParameterGameDateTime = "@game_datetime";
        internal const string SqlParameterEntityKind = "@entity_kind";
        internal const string SqlParameterEntityId = "@entity_id";
        internal const string SqlParameterEventNamespaceIdentifier = "@event_namespace_id";
        internal const string SqlParameterEventType = "@event_type";
        internal const string SqlParameterSourcePatch = "@source_patch";
        internal const string SqlParameterPayloadJson = "@payload_json";
        internal const string SqlParameterEventId = "@event_id";
        internal const string SqlParameterSingleId = "@single_id";
        internal const string SqlParameterRowIndex = "@row_index";
        internal const string SqlParameterPositionIndex = "@position_index";
        internal const string SqlParameterIsCenter = "@is_center";
        internal const string SqlParameterReleaseDate = "@release_date";
        internal const string SqlParameterEndDate = "@end_date";
        internal const string SqlParameterStatusType = "@status_type";
        internal const string SqlParameterStartDate = "@start_date";
        internal const string SqlParameterMetaKey = "@meta_key";
        internal const string SqlParameterMetaValue = "@meta_value";
        internal const string SqlParameterShowId = "@show_id";
        internal const string SqlParameterContractKey = "@contract_key";
        internal const string SqlParameterRelationshipKey = "@relationship_key";
        internal const string SqlParameterRelationshipType = "@relationship_type";
        internal const string SqlParameterTourId = "@tour_id";
        internal const string SqlParameterLifecycleAction = "@lifecycle_action";
        internal const string SqlParameterEventDate = "@event_date";
        internal const string SqlParameterAwardKey = "@award_key";
        internal const string SqlParameterElectionId = "@election_id";
        internal const string SqlParameterSlotKey = "@slot_key";
        internal const string SqlParameterEndReason = "@end_reason";
        internal const string SqlParameterPushDaysInSlot = "@push_days_in_slot";

        internal const string ProjectionRelationshipTypeIdolDating = "idol_dating";
        internal const string ProjectionRelationshipTypeBullying = "bullying";
        internal const string ProjectionRelationshipTypeClique = "clique";
        internal const string ProjectionLifecycleActionStarted = "started";
        internal const string ProjectionLifecycleActionFinished = "finished";
        internal const string ProjectionLifecycleActionCancelled = "cancelled";
        internal const int ProjectionUnknownDayCount = -1;

        internal const int JsonBuilderDefaultCapacity = 512;
        internal const char JsonObjectStartCharacter = '{';
        internal const char JsonObjectEndCharacter = '}';
        internal const char JsonStringQuoteCharacter = '"';
        internal const char JsonPropertySeparatorCharacter = ',';
        internal const char JsonNameValueSeparatorCharacter = ':';
        internal const char JsonEscapeCharacter = '\\';
        internal const char JsonLineFeedCharacter = '\n';
        internal const char JsonCarriageReturnCharacter = '\r';
        internal const char JsonTabCharacter = '\t';
        internal const char TokenUnderscoreCharacter = '_';
        internal const char TokenHyphenCharacter = '-';
        internal const char TokenDotCharacter = '.';
        internal const string JsonEscapedQuote = "\\\"";
        internal const string JsonEscapedBackslash = "\\\\";
        internal const string JsonEscapedNewLine = "\\n";
        internal const string JsonEscapedCarriageReturn = "\\r";
        internal const string JsonEscapedTab = "\\t";
        internal const string JsonBooleanTrue = "true";
        internal const string JsonBooleanFalse = "false";
        internal const string JsonNullLiteral = "null";
        internal const string JsonFloatRoundTripFormat = "R";
        internal const string PayloadFieldKeyRawJson = "__raw_json";
        internal const string PayloadValueKindString = "string";
        internal const string PayloadValueKindNumber = "number";
        internal const string PayloadValueKindBoolean = "boolean";
        internal const string PayloadValueKindNull = "null";
        internal const string PayloadValueKindObject = "object";
        internal const string PayloadValueKindArray = "array";
        internal const string PayloadValueKindRaw = "raw";

        internal const string JsonFieldSingleId = "single_id";
        internal const string JsonFieldSingleTitle = "single_title";
        internal const string JsonFieldSingleLifecycleAction = "single_lifecycle_action";
        internal const string JsonFieldSingleStatus = "single_status";
        internal const string JsonFieldSinglePreviousStatus = "single_previous_status";
        internal const string JsonFieldSingleNewStatus = "single_new_status";
        internal const string JsonFieldSingleCastCount = "single_cast_count";
        internal const string JsonFieldSingleCastIdList = "single_cast_id_list";
        internal const string JsonFieldSingleCastCountBefore = "single_cast_count_before";
        internal const string JsonFieldSingleCastCountAfter = "single_cast_count_after";
        internal const string JsonFieldSingleCastIdListBefore = "single_cast_id_list_before";
        internal const string JsonFieldSingleCastIdListAfter = "single_cast_id_list_after";
        internal const string JsonFieldSingleCastIdListAdded = "single_cast_id_list_added";
        internal const string JsonFieldSingleCastIdListRemoved = "single_cast_id_list_removed";
        internal const string JsonFieldSingleRemovedIdolId = "single_removed_idol_id";
        internal const string JsonFieldSingleIsDigital = "single_is_digital";
        internal const string JsonFieldSingleLinkedElectionId = "single_linked_election_id";
        internal const string JsonFieldFromGroupId = "from_group_id";
        internal const string JsonFieldFromGroupTitle = "from_group_title";
        internal const string JsonFieldFromGroupStatus = "from_group_status";
        internal const string JsonFieldToGroupId = "to_group_id";
        internal const string JsonFieldToGroupTitle = "to_group_title";
        internal const string JsonFieldToGroupStatus = "to_group_status";
        internal const string JsonFieldTransferDate = "transfer_date";
        internal const string JsonFieldGroupId = "group_id";
        internal const string JsonFieldGroupTitle = "group_title";
        internal const string JsonFieldGroupStatus = "group_status";
        internal const string JsonFieldGroupLifecycleAction = "group_lifecycle_action";
        internal const string JsonFieldGroupDateCreated = "group_date_created";
        internal const string JsonFieldGroupEventDate = "group_event_date";
        internal const string JsonFieldGroupMemberCount = "group_member_count";
        internal const string JsonFieldGroupMemberIdList = "group_member_id_list";
        internal const string JsonFieldGroupSingleCount = "group_single_count";
        internal const string JsonFieldGroupNonReleasedSingleCount = "group_non_released_single_count";
        internal const string JsonFieldGroupAppealGender = "group_appeal_gender";
        internal const string JsonFieldGroupAppealHardcoreness = "group_appeal_hardcoreness";
        internal const string JsonFieldGroupAppealAge = "group_appeal_age";
        internal const string JsonFieldGroupSourceParamType = "group_source_param_type";
        internal const string JsonFieldGroupTargetFanType = "group_target_fan_type";
        internal const string JsonFieldGroupPointsRequested = "group_points_requested";
        internal const string JsonFieldGroupPointsApplied = "group_points_applied";
        internal const string JsonFieldGroupPointsBefore = "group_points_before";
        internal const string JsonFieldGroupPointsAfter = "group_points_after";
        internal const string JsonFieldGroupPointsDelta = "group_points_delta";
        internal const string JsonFieldGroupPointsSpentBefore = "group_points_spent_before";
        internal const string JsonFieldGroupPointsSpentAfter = "group_points_spent_after";
        internal const string JsonFieldGroupAvailablePointsBefore = "group_available_points_before";
        internal const string JsonFieldGroupAvailablePointsAfter = "group_available_points_after";
        internal const string JsonFieldGroupTargetPointsBefore = "group_target_points_before";
        internal const string JsonFieldGroupTargetPointsAfter = "group_target_points_after";
        internal const string JsonFieldIdolId = "idol_id";
        internal const string JsonFieldIdolLifecycleAction = "idol_lifecycle_action";
        internal const string JsonFieldIdolStatus = "idol_status";
        internal const string JsonFieldIdolType = "idol_type";
        internal const string JsonFieldIdolAge = "idol_age";
        internal const string JsonFieldIdolHiringDate = "idol_hiring_date";
        internal const string JsonFieldIdolGraduationDate = "idol_graduation_date";
        internal const string JsonFieldIdolTrivia = "idol_trivia";
        internal const string JsonFieldIdolCustomTrivia = "idol_custom_trivia";
        internal const string JsonFieldIdolGraduationWithDialogue = "idol_graduation_with_dialogue";
        internal const string JsonFieldSalaryAction = "salary_action";
        internal const string JsonFieldSalaryBefore = "salary_before";
        internal const string JsonFieldSalaryAfter = "salary_after";
        internal const string JsonFieldSalaryDelta = "salary_delta";
        internal const string JsonFieldSalarySatisfactionBefore = "salary_satisfaction_before";
        internal const string JsonFieldSalarySatisfactionAfter = "salary_satisfaction_after";
        internal const string JsonFieldRowIndex = "row_index";
        internal const string JsonFieldPositionIndex = "position_index";
        internal const string JsonFieldIsCenter = "is_center";
        internal const string JsonFieldSingleReleaseDate = "single_release_date";
        internal const string JsonFieldSingleQuantity = "single_quantity";
        internal const string JsonFieldSingleProductionCost = "single_production_cost";
        internal const string JsonFieldSingleMarketingResult = "single_marketing_result";
        internal const string JsonFieldSingleMarketingResultStatus = "single_marketing_result_status";
        internal const string JsonFieldSingleGrossRevenue = "single_gross_revenue";
        internal const string JsonFieldSingleOneCdCost = "single_one_cd_cost";
        internal const string JsonFieldSingleOneCdRevenue = "single_one_cd_revenue";
        internal const string JsonFieldSingleOtherExpenses = "single_other_expenses";
        internal const string JsonFieldSingleIsGroupHandshake = "single_is_group_handshake";
        internal const string JsonFieldSingleIsIndividualHandshake = "single_is_individual_handshake";
        internal const string JsonFieldSingleFamePointsAwarded = "single_fame_points_awarded";
        internal const string JsonFieldTotalSales = "total_sales";
        internal const string JsonFieldQuality = "quality";
        internal const string JsonFieldFanSatisfaction = "fan_satisfaction";
        internal const string JsonFieldFanBuzz = "fan_buzz";
        internal const string JsonFieldNewFans = "new_fans";
        internal const string JsonFieldNewHardcoreFans = "new_hardcore_fans";
        internal const string JsonFieldNewCasualFans = "new_casual_fans";
        internal const string JsonFieldSingleProfit = "single_profit";
        internal const string JsonFieldSingleSalesPerFan = "single_sales_per_fan";
        internal const string JsonFieldSingleFameOfSenbatsu = "single_fame_of_senbatsu";
        internal const string JsonFieldSingleMostPopularGenre = "single_most_popular_genre";
        internal const string JsonFieldSingleMostPopularLyrics = "single_most_popular_lyrics";
        internal const string JsonFieldSingleMostPopularChoreo = "single_most_popular_choreo";
        internal const string JsonFieldSingleFanAppealMale = "single_fan_appeal_male";
        internal const string JsonFieldSingleFanAppealFemale = "single_fan_appeal_female";
        internal const string JsonFieldSingleFanAppealCasual = "single_fan_appeal_casual";
        internal const string JsonFieldSingleFanAppealHardcore = "single_fan_appeal_hardcore";
        internal const string JsonFieldSingleFanAppealTeen = "single_fan_appeal_teen";
        internal const string JsonFieldSingleFanAppealYoungAdult = "single_fan_appeal_young_adult";
        internal const string JsonFieldSingleFanAppealAdult = "single_fan_appeal_adult";
        internal const string JsonFieldSingleFanSegmentSalesSummary = "single_fan_segment_sales_summary";
        internal const string JsonFieldSingleFanSegmentNewFansSummary = "single_fan_segment_new_fans_summary";
        internal const string JsonFieldSingleSenbatsuStatsSnapshot = "single_senbatsu_stats_snapshot";
        internal const string JsonFieldChartPosition = "chart_position";
        internal const string JsonFieldPreviousStatus = "previous_status";
        internal const string JsonFieldNewStatus = "new_status";
        internal const string JsonFieldDatingRoute = "dating_route";
        internal const string JsonFieldDatingRouteStage = "dating_route_stage";
        internal const string JsonFieldPreviousPartnerStatus = "previous_partner_status";
        internal const string JsonFieldNewPartnerStatus = "new_partner_status";
        internal const string JsonFieldDatingHadScandal = "dating_had_scandal";
        internal const string JsonFieldDatingHadScandalEver = "dating_had_scandal_ever";
        internal const string JsonFieldDatingUsedGoods = "dating_used_goods";
        internal const string JsonFieldDatingDatedIdol = "dating_dated_idol";
        internal const string JsonFieldContractType = "contract_type";
        internal const string JsonFieldContractSkill = "contract_skill";
        internal const string JsonFieldContractIsGroup = "contract_is_group";
        internal const string JsonFieldContractWeeklyPayment = "contract_weekly_payment";
        internal const string JsonFieldContractWeeklyBuzz = "contract_weekly_buzz";
        internal const string JsonFieldContractWeeklyFame = "contract_weekly_fame";
        internal const string JsonFieldContractWeeklyFans = "contract_weekly_fans";
        internal const string JsonFieldContractWeeklyStamina = "contract_weekly_stamina";
        internal const string JsonFieldContractLiability = "contract_liability";
        internal const string JsonFieldContractAgentName = "contract_agent_name";
        internal const string JsonFieldContractProductName = "contract_product_name";
        internal const string JsonFieldContractStartDate = "contract_start_date";
        internal const string JsonFieldContractEndDate = "contract_end_date";
        internal const string JsonFieldContractDurationMonths = "contract_duration_months";
        internal const string JsonFieldContractIsImmediate = "contract_is_immediate";
        internal const string JsonFieldContractDamagesApplied = "contract_damages_applied";
        internal const string JsonFieldContractTotalBrokenLiability = "contract_total_broken_liability";
        internal const string JsonFieldContractBreakContext = "contract_break_context";
        internal const string JsonFieldContractWeeklyAction = "contract_weekly_action";
        internal const string JsonFieldContractWeeklyTrainingPoints = "contract_weekly_training_points";
        internal const string JsonFieldShowId = "show_id";
        internal const string JsonFieldShowTitle = "show_title";
        internal const string JsonFieldShowTitleBefore = "show_title_before";
        internal const string JsonFieldShowTitleAfter = "show_title_after";
        internal const string JsonFieldShowLifecycleAction = "show_lifecycle_action";
        internal const string JsonFieldShowStatus = "show_status";
        internal const string JsonFieldShowMediumCode = "show_medium_code";
        internal const string JsonFieldShowMediumTitle = "show_medium_title";
        internal const string JsonFieldShowGenreCode = "show_genre_code";
        internal const string JsonFieldShowGenreTitle = "show_genre_title";
        internal const string JsonFieldShowCastIdList = "show_cast_id_list";
        internal const string JsonFieldShowLaunchDate = "show_launch_date";
        internal const string JsonFieldShowPreviousStatus = "show_previous_status";
        internal const string JsonFieldShowNewStatus = "show_new_status";
        internal const string JsonFieldShowCastType = "show_cast_type";
        internal const string JsonFieldShowEpisodeCount = "show_episode_count";
        internal const string JsonFieldShowEpisodeDate = "show_episode_date";
        internal const string JsonFieldShowCastCount = "show_cast_count";
        internal const string JsonFieldShowCastCountBefore = "show_cast_count_before";
        internal const string JsonFieldShowCastCountAfter = "show_cast_count_after";
        internal const string JsonFieldShowCastIdListBefore = "show_cast_id_list_before";
        internal const string JsonFieldShowCastIdListAfter = "show_cast_id_list_after";
        internal const string JsonFieldShowCastIdListAdded = "show_cast_id_list_added";
        internal const string JsonFieldShowCastIdListRemoved = "show_cast_id_list_removed";
        internal const string JsonFieldShowCastTypeBefore = "show_cast_type_before";
        internal const string JsonFieldShowCastTypeAfter = "show_cast_type_after";
        internal const string JsonFieldShowRemovedIdolId = "show_removed_idol_id";
        internal const string JsonFieldShowMcCodeBefore = "show_mc_code_before";
        internal const string JsonFieldShowMcCodeAfter = "show_mc_code_after";
        internal const string JsonFieldShowMcTitleBefore = "show_mc_title_before";
        internal const string JsonFieldShowMcTitleAfter = "show_mc_title_after";
        internal const string JsonFieldShowProductionCostBefore = "show_production_cost_before";
        internal const string JsonFieldShowProductionCostAfter = "show_production_cost_after";
        internal const string JsonFieldShowFanAppealSummaryBefore = "show_fan_appeal_summary_before";
        internal const string JsonFieldShowFanAppealSummaryAfter = "show_fan_appeal_summary_after";
        internal const string JsonFieldShowLatestAudience = "show_latest_audience";
        internal const string JsonFieldShowLatestRevenue = "show_latest_revenue";
        internal const string JsonFieldShowLatestNewFans = "show_latest_new_fans";
        internal const string JsonFieldShowLatestBuzz = "show_latest_buzz";
        internal const string JsonFieldShowLatestFatigue = "show_latest_fatigue";
        internal const string JsonFieldShowRelaunchCount = "show_relaunch_count";
        internal const string JsonFieldShowWasRelaunched = "show_was_relaunched";
        internal const string JsonFieldShowPreviousAudience = "show_previous_audience";
        internal const string JsonFieldShowPreviousRevenue = "show_previous_revenue";
        internal const string JsonFieldShowPreviousProfit = "show_previous_profit";
        internal const string JsonFieldShowPreviousNewFans = "show_previous_new_fans";
        internal const string JsonFieldShowPreviousBuzz = "show_previous_buzz";
        internal const string JsonFieldShowPreviousFatigue = "show_previous_fatigue";
        internal const string JsonFieldShowPreviousFame = "show_previous_fame";
        internal const string JsonFieldShowPreviousFamePoints = "show_previous_fame_points";
        internal const string JsonFieldShowAudienceDelta = "show_audience_delta";
        internal const string JsonFieldShowRevenueDelta = "show_revenue_delta";
        internal const string JsonFieldShowProfitDelta = "show_profit_delta";
        internal const string JsonFieldShowNewFansDelta = "show_new_fans_delta";
        internal const string JsonFieldShowBuzzDelta = "show_buzz_delta";
        internal const string JsonFieldShowFatigueDelta = "show_fatigue_delta";
        internal const string JsonFieldShowFameDelta = "show_fame_delta";
        internal const string JsonFieldShowFamePointsDelta = "show_fame_points_delta";
        internal const string JsonFieldShowLatestFame = "show_latest_fame";
        internal const string JsonFieldShowLatestFamePoints = "show_latest_fame_points";
        internal const string JsonFieldShowLatestProfit = "show_latest_profit";
        internal const string JsonFieldShowEpisodeBudget = "show_episode_budget";
        internal const string JsonFieldShowStaminaCost = "show_stamina_cost";
        internal const string JsonFieldConcertId = "concert_id";
        internal const string JsonFieldConcertTitle = "concert_title";
        internal const string JsonFieldConcertTitleBefore = "concert_title_before";
        internal const string JsonFieldConcertTitleAfter = "concert_title_after";
        internal const string JsonFieldConcertRawTitleBefore = "concert_raw_title_before";
        internal const string JsonFieldConcertRawTitleAfter = "concert_raw_title_after";
        internal const string JsonFieldConcertVenue = "concert_venue";
        internal const string JsonFieldConcertVenueBefore = "concert_venue_before";
        internal const string JsonFieldConcertVenueAfter = "concert_venue_after";
        internal const string JsonFieldConcertStatus = "concert_status";
        internal const string JsonFieldConcertSongCount = "concert_song_count";
        internal const string JsonFieldConcertParticipantCount = "concert_participant_count";
        internal const string JsonFieldConcertParticipantIdList = "concert_participant_id_list";
        internal const string JsonFieldConcertSetlistSummary = "concert_setlist_summary";
        internal const string JsonFieldConcertProjectedAudience = "concert_projected_audience";
        internal const string JsonFieldConcertActualAudience = "concert_actual_audience";
        internal const string JsonFieldConcertProjectedRevenue = "concert_projected_revenue";
        internal const string JsonFieldConcertActualRevenue = "concert_actual_revenue";
        internal const string JsonFieldConcertProductionCost = "concert_production_cost";
        internal const string JsonFieldConcertProfit = "concert_profit";
        internal const string JsonFieldConcertHype = "concert_hype";
        internal const string JsonFieldConcertTicketPrice = "concert_ticket_price";
        internal const string JsonFieldConcertTicketPriceBefore = "concert_ticket_price_before";
        internal const string JsonFieldConcertTicketPriceAfter = "concert_ticket_price_after";
        internal const string JsonFieldConcertFinishDate = "concert_finish_date";
        internal const string JsonFieldConcertPreviousStatus = "concert_previous_status";
        internal const string JsonFieldConcertNewStatus = "concert_new_status";
        internal const string JsonFieldConcertSongCountBefore = "concert_song_count_before";
        internal const string JsonFieldConcertSongCountAfter = "concert_song_count_after";
        internal const string JsonFieldConcertParticipantCountBefore = "concert_participant_count_before";
        internal const string JsonFieldConcertParticipantCountAfter = "concert_participant_count_after";
        internal const string JsonFieldConcertParticipantIdListBefore = "concert_participant_id_list_before";
        internal const string JsonFieldConcertParticipantIdListAfter = "concert_participant_id_list_after";
        internal const string JsonFieldConcertParticipantIdListAdded = "concert_participant_id_list_added";
        internal const string JsonFieldConcertParticipantIdListRemoved = "concert_participant_id_list_removed";
        internal const string JsonFieldConcertSetlistSummaryBefore = "concert_setlist_summary_before";
        internal const string JsonFieldConcertSetlistSummaryAfter = "concert_setlist_summary_after";
        internal const string JsonFieldConcertRemovedIdolId = "concert_removed_idol_id";
        internal const string JsonFieldAwardType = "award_type";
        internal const string JsonFieldAwardYear = "award_year";
        internal const string JsonFieldAwardIsNomination = "award_is_nomination";
        internal const string JsonFieldAwardWon = "award_won";
        internal const string JsonFieldAwardSingleId = "award_single_id";
        internal const string JsonFieldPushSlotIndex = "push_slot_index";
        internal const string JsonFieldPushPreviousIdolId = "push_previous_idol_id";
        internal const string JsonFieldPushCurrentIdolId = "push_current_idol_id";
        internal const string JsonFieldPushDaysInSlot = "push_days_in_slot";
        internal const string JsonFieldPushLifecycleAction = "push_lifecycle_action";
        internal const string JsonFieldRelationshipIdolAId = "relationship_idol_a_id";
        internal const string JsonFieldRelationshipIdolBId = "relationship_idol_b_id";
        internal const string JsonFieldRelationshipStatus = "relationship_status";
        internal const string JsonFieldRelationshipDynamic = "relationship_dynamic";
        internal const string JsonFieldRelationshipKnownToPlayer = "relationship_known_to_player";
        internal const string JsonFieldRelationshipPairKey = "relationship_pair_key";
        internal const string JsonFieldRelationshipBreakReason = "relationship_break_reason";
        internal const string JsonFieldRelationshipIsDating = "relationship_is_dating";
        internal const string JsonFieldRelationshipPreviousStatus = "relationship_previous_status";
        internal const string JsonFieldRelationshipNewStatus = "relationship_new_status";
        internal const string JsonFieldRelationshipRequestedDelta = "relationship_requested_delta";
        internal const string JsonFieldRelationshipRatio = "relationship_ratio";
        internal const string JsonFieldCliqueLeaderId = "clique_leader_id";
        internal const string JsonFieldCliqueLeaderIdBefore = "clique_leader_id_before";
        internal const string JsonFieldCliqueLeaderIdAfter = "clique_leader_id_after";
        internal const string JsonFieldCliqueMemberCount = "clique_member_count";
        internal const string JsonFieldCliqueSignature = "clique_signature";
        internal const string JsonFieldCliqueQuitWasViolent = "clique_quit_was_violent";
        internal const string JsonFieldBullyingTargetId = "bullying_target_id";
        internal const string JsonFieldBullyingLeaderId = "bullying_leader_id";
        internal const string JsonFieldBullyingKnownToPlayer = "bullying_known_to_player";
        internal const string JsonFieldScandalPointsAvailableBefore = "scandal_points_available_before";
        internal const string JsonFieldScandalPointsRemoved = "scandal_points_removed";
        internal const string JsonFieldScandalGroupPointsRemoved = "scandal_group_points_removed";
        internal const string JsonFieldScandalGroupPointsRemaining = "scandal_group_points_remaining";
        internal const string JsonFieldScandalPointsBefore = "scandal_points_before";
        internal const string JsonFieldScandalPointsAfter = "scandal_points_after";
        internal const string JsonFieldPlayerRelationshipType = "player_relationship_type";
        internal const string JsonFieldPlayerPointsRequestedDelta = "player_points_requested_delta";
        internal const string JsonFieldPlayerPointsAppliedDelta = "player_points_applied_delta";
        internal const string JsonFieldPlayerPointsBefore = "player_points_before";
        internal const string JsonFieldPlayerPointsAfter = "player_points_after";
        internal const string JsonFieldPlayerLevelBefore = "player_level_before";
        internal const string JsonFieldPlayerLevelAfter = "player_level_after";
        internal const string JsonFieldPlayerLevelChanged = "player_level_changed";
        internal const string JsonFieldDateInteractionType = "date_interaction_type";
        internal const string JsonFieldDateRouteBefore = "date_route_before";
        internal const string JsonFieldDateRouteAfter = "date_route_after";
        internal const string JsonFieldDateStageBefore = "date_stage_before";
        internal const string JsonFieldDateStageAfter = "date_stage_after";
        internal const string JsonFieldDateStatusBefore = "date_status_before";
        internal const string JsonFieldDateStatusAfter = "date_status_after";
        internal const string JsonFieldDateResultToken = "date_result_token";
        internal const string JsonFieldDateResultSummaryCode = "date_result_summary_code";
        internal const string JsonFieldDateCaughtBefore = "date_caught_before";
        internal const string JsonFieldDateCaughtAfter = "date_caught_after";
        internal const string JsonFieldDateRelationshipLevelBefore = "date_relationship_level_before";
        internal const string JsonFieldDateRelationshipLevelAfter = "date_relationship_level_after";
        internal const string JsonFieldReplyEffectEntries = "reply_effect_entries";
        internal const string JsonFieldReplyEffectTarget = "target";
        internal const string JsonFieldReplyEffectParameter = "parameter";
        internal const string JsonFieldReplyEffectFormula = "formula";
        internal const string JsonFieldReplyEffectSpecial = "special";
        internal const string JsonFieldMarriageStage = "marriage_stage";
        internal const string JsonFieldMarriageRoute = "marriage_route";
        internal const string JsonFieldMarriagePartnerStatus = "marriage_partner_status";
        internal const string JsonFieldMarriageGoodOutcome = "marriage_good_outcome";
        internal const string JsonFieldMarriageGirlQuitTriggered = "marriage_girl_quit_triggered";
        internal const string JsonFieldMarriageKidsString = "marriage_kids_string";
        internal const string JsonFieldMarriageCareerStringOne = "marriage_career_string_one";
        internal const string JsonFieldMarriageCareerStringTwo = "marriage_career_string_two";
        internal const string JsonFieldMarriageRelationshipOutcomeString = "marriage_relationship_outcome_string";
        internal const string JsonFieldMarriageCustodyString = "marriage_custody_string";
        internal const string JsonFieldMarriageGraduationTrivia = "marriage_graduation_trivia";
        internal const string JsonFieldMarriageIdolStatus = "marriage_idol_status";
        internal const string JsonFieldTourId = "tour_id";
        internal const string JsonFieldTourLifecycleAction = "tour_lifecycle_action";
        internal const string JsonFieldTourStatus = "tour_status";
        internal const string JsonFieldTourParticipantCount = "tour_participant_count";
        internal const string JsonFieldTourParticipantIdList = "tour_participant_id_list";
        internal const string JsonFieldTourSelectedCountryLevelList = "tour_selected_country_level_list";
        internal const string JsonFieldTourStaminaCost = "tour_stamina_cost";
        internal const string JsonFieldTourProfit = "tour_profit";
        internal const string JsonFieldTourStartDate = "tour_start_date";
        internal const string JsonFieldTourPreviousStatus = "tour_previous_status";
        internal const string JsonFieldTourNewStatus = "tour_new_status";
        internal const string JsonFieldTourSelectedCountryCount = "tour_selected_country_count";
        internal const string JsonFieldTourTotalAudience = "tour_total_audience";
        internal const string JsonFieldTourTotalRevenue = "tour_total_revenue";
        internal const string JsonFieldTourTotalNewFans = "tour_total_new_fans";
        internal const string JsonFieldTourProductionCost = "tour_production_cost";
        internal const string JsonFieldTourExpectedRevenue = "tour_expected_revenue";
        internal const string JsonFieldTourSaving = "tour_saving";
        internal const string JsonFieldTourFinishDate = "tour_finish_date";
        internal const string JsonFieldTourCountryCode = "tour_country_code";
        internal const string JsonFieldTourCountryLevel = "tour_country_level";
        internal const string JsonFieldTourCountryAttendance = "tour_country_attendance";
        internal const string JsonFieldTourCountryAudience = "tour_country_audience";
        internal const string JsonFieldTourCountryNewFans = "tour_country_new_fans";
        internal const string JsonFieldTourCountryRevenue = "tour_country_revenue";
        internal const string JsonFieldTourCountryDiscount = "tour_country_discount";
        internal const string JsonFieldElectionId = "election_id";
        internal const string JsonFieldElectionLifecycleAction = "election_lifecycle_action";
        internal const string JsonFieldElectionStatus = "election_status";
        internal const string JsonFieldElectionPreviousStatus = "election_previous_status";
        internal const string JsonFieldElectionNewStatus = "election_new_status";
        internal const string JsonFieldElectionBroadcastType = "election_broadcast_type";
        internal const string JsonFieldElectionSingleId = "election_single_id";
        internal const string JsonFieldElectionConcertId = "election_concert_id";
        internal const string JsonFieldElectionReleaseSingleId = "election_release_single_id";
        internal const string JsonFieldElectionResultCount = "election_result_count";
        internal const string JsonFieldElectionFinishDate = "election_finish_date";
        internal const string JsonFieldElectionPlace = "election_place";
        internal const string JsonFieldElectionVotes = "election_votes";
        internal const string JsonFieldElectionFamePoints = "election_fame_points";
        internal const string JsonFieldElectionExpectedPlace = "election_expected_place";
        internal const string JsonFieldElectionGeneratedPlace = "election_generated_place";
        internal const string JsonFieldElectionGeneratedVotes = "election_generated_votes";
        internal const string JsonFieldElectionGeneratedFamePoints = "election_generated_fame_points";
        internal const string JsonFieldElectionPlaceBefore = "election_place_before";
        internal const string JsonFieldElectionPlaceAfter = "election_place_after";
        internal const string JsonFieldScandalPreviousPoints = "scandal_previous_points";
        internal const string JsonFieldScandalNewPoints = "scandal_new_points";
        internal const string JsonFieldScandalDeltaPoints = "scandal_delta_points";
        internal const string JsonFieldScandalPreviousPointsRaw = "scandal_previous_points_raw";
        internal const string JsonFieldScandalNewPointsRaw = "scandal_new_points_raw";
        internal const string JsonFieldScandalDeltaPointsRaw = "scandal_delta_points_raw";
        internal const string JsonFieldScandalMutationSource = "scandal_mutation_source";
        internal const string JsonFieldMedicalEventType = "medical_event_type";
        internal const string JsonFieldMedicalPreviousStatus = "medical_previous_status";
        internal const string JsonFieldMedicalCurrentStatus = "medical_current_status";
        internal const string JsonFieldMedicalHiatusEndDate = "medical_hiatus_end_date";
        internal const string JsonFieldMedicalFinishWasForced = "medical_finish_was_forced";
        internal const string JsonFieldMedicalInjuryCounter = "medical_injury_counter";
        internal const string JsonFieldMedicalDepressionCounter = "medical_depression_counter";

        internal const long ZeroLongValue = 0L;
        internal const string IdentifierListSeparator = ",";
        internal const string SingleFanSegmentEntrySeparator = "|";
        internal const string SingleFanSegmentValueSeparator = ":";
        internal const string SingleFanSegmentDimensionSeparator = ",";
        internal const string TourCountryLevelPairSeparator = "|";
        internal const string TourCountryLevelValueSeparator = ":";
        internal const string ConcertSetlistEntrySeparator = "|";
        internal const string ConcertSetlistValueSeparator = ":";
        internal const string ConcertSetlistEntryTypeSong = "song";
        internal const string ConcertSetlistEntryTypeMc = "mc";
        internal const string ConcertSetlistEntryTypeGeneric = "item";
        internal const string SetlistTextSeparatorReplacement = "/";
    }

    /// <summary>
    /// Public facade that other mods can call to use IM Data Core services.
    /// </summary>
    public static class IMDataCoreApi
    {
        /// <summary>
        /// Returns true when the core runtime and storage backend are ready.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool IsReady()
        {
            return IMDataCoreController.Instance.IsReady();
        }

        /// <summary>
        /// Registers a namespace for the calling assembly and returns an authenticated session.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryRegisterNamespace(string namespaceIdentifier, out IMDataCoreSession session, out string errorMessage)
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            return IMDataCoreController.Instance.TryRegisterNamespace(namespaceIdentifier, callingAssembly, out session, out errorMessage);
        }

        /// <summary>
        /// Removes the namespace session for the calling assembly.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryUnregisterNamespace(IMDataCoreSession session, out string errorMessage)
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            return IMDataCoreController.Instance.TryUnregisterNamespace(session, callingAssembly, out errorMessage);
        }

        /// <summary>
        /// Stores one JSON document in the caller's namespace.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TrySetCustomJson(IMDataCoreSession session, string dataKey, string jsonValue, out string errorMessage)
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            return IMDataCoreController.Instance.TrySetCustomJson(session, callingAssembly, dataKey, jsonValue, out errorMessage);
        }

        /// <summary>
        /// Reads one JSON document from the caller's namespace.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryGetCustomJson(IMDataCoreSession session, string dataKey, out string jsonValue, out string errorMessage)
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            return IMDataCoreController.Instance.TryGetCustomJson(session, callingAssembly, dataKey, out jsonValue, out errorMessage);
        }

        /// <summary>
        /// Removes one JSON document from the caller's namespace.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryRemoveCustomJson(IMDataCoreSession session, string dataKey, out string errorMessage)
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            return IMDataCoreController.Instance.TryRemoveCustomJson(session, callingAssembly, dataKey, out errorMessage);
        }

        /// <summary>
        /// Appends a custom event produced by another mod into the shared event stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryAppendCustomEvent(
            IMDataCoreSession session,
            int idolId,
            string entityKind,
            string entityId,
            string eventType,
            string payloadJson,
            string sourcePatch,
            out string errorMessage)
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            return IMDataCoreController.Instance.TryAppendCustomEvent(
                session,
                callingAssembly,
                idolId,
                entityKind,
                entityId,
                eventType,
                payloadJson,
                sourcePatch,
                out errorMessage);
        }

        /// <summary>
        /// Returns recent events for one idol from the active save scope.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryReadRecentEventsForIdol(int idolId, int maxCount, out List<IMDataCoreEvent> events, out string errorMessage)
        {
            return IMDataCoreController.Instance.TryReadRecentEventsForIdol(idolId, maxCount, out events, out errorMessage);
        }

        /// <summary>
        /// Forces immediate persistence of all currently queued records.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryFlushNow(out string errorMessage)
        {
            return IMDataCoreController.Instance.TryFlushNow(out errorMessage);
        }

        /// <summary>
        /// Returns the currently resolved save key used by IM Data Core.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryGetActiveSaveKey(out string saveKey, out string errorMessage)
        {
            return IMDataCoreController.Instance.TryGetActiveSaveKey(out saveKey, out errorMessage);
        }
    }

    /// <summary>
    /// Compatibility alias for callers that prefer uppercase API naming.
    /// </summary>
    public static class IMDataCoreAPI
    {
        /// <summary>
        /// Returns true when the core runtime and storage backend are ready.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool IsReady()
        {
            return IMDataCoreController.Instance.IsReady();
        }

        /// <summary>
        /// Registers a namespace for the calling assembly and returns an authenticated session.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryRegisterNamespace(string namespaceIdentifier, out IMDataCoreSession session, out string errorMessage)
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            return IMDataCoreController.Instance.TryRegisterNamespace(namespaceIdentifier, callingAssembly, out session, out errorMessage);
        }

        /// <summary>
        /// Removes the namespace session for the calling assembly.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryUnregisterNamespace(IMDataCoreSession session, out string errorMessage)
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            return IMDataCoreController.Instance.TryUnregisterNamespace(session, callingAssembly, out errorMessage);
        }

        /// <summary>
        /// Stores one JSON document in the caller's namespace.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TrySetCustomJson(IMDataCoreSession session, string dataKey, string jsonValue, out string errorMessage)
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            return IMDataCoreController.Instance.TrySetCustomJson(session, callingAssembly, dataKey, jsonValue, out errorMessage);
        }

        /// <summary>
        /// Reads one JSON document from the caller's namespace.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryGetCustomJson(IMDataCoreSession session, string dataKey, out string jsonValue, out string errorMessage)
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            return IMDataCoreController.Instance.TryGetCustomJson(session, callingAssembly, dataKey, out jsonValue, out errorMessage);
        }

        /// <summary>
        /// Removes one JSON document from the caller's namespace.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryRemoveCustomJson(IMDataCoreSession session, string dataKey, out string errorMessage)
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            return IMDataCoreController.Instance.TryRemoveCustomJson(session, callingAssembly, dataKey, out errorMessage);
        }

        /// <summary>
        /// Appends a custom event produced by another mod into the shared event stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryAppendCustomEvent(
            IMDataCoreSession session,
            int idolId,
            string entityKind,
            string entityId,
            string eventType,
            string payloadJson,
            string sourcePatch,
            out string errorMessage)
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            return IMDataCoreController.Instance.TryAppendCustomEvent(
                session,
                callingAssembly,
                idolId,
                entityKind,
                entityId,
                eventType,
                payloadJson,
                sourcePatch,
                out errorMessage);
        }

        /// <summary>
        /// Returns recent events for one idol from the active save scope.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryReadRecentEventsForIdol(int idolId, int maxCount, out List<IMDataCoreEvent> events, out string errorMessage)
        {
            return IMDataCoreController.Instance.TryReadRecentEventsForIdol(idolId, maxCount, out events, out errorMessage);
        }

        /// <summary>
        /// Forces immediate persistence of all currently queued records.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryFlushNow(out string errorMessage)
        {
            return IMDataCoreController.Instance.TryFlushNow(out errorMessage);
        }

        /// <summary>
        /// Returns the currently resolved save key used by IM Data Core.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryGetActiveSaveKey(out string saveKey, out string errorMessage)
        {
            return IMDataCoreController.Instance.TryGetActiveSaveKey(out saveKey, out errorMessage);
        }
    }

    /// <summary>
    /// Runtime coordinator that owns buffering, storage lifecycle, and API authorization.
    /// </summary>
    internal sealed partial class IMDataCoreController
    {
        private static readonly IMDataCoreController sharedInstance = new IMDataCoreController();
        private static readonly FieldInfo scandalPointsPopupPointsField =
            AccessTools.Field(typeof(ScandalPoints_Popup), CoreConstants.ReflectionScandalPointsPopupPointsFieldName);

        private readonly object runtimeLock = new object();
        private readonly List<PendingEvent> bufferedEvents = new List<PendingEvent>();
        private readonly List<SingleParticipationProjection> bufferedSingleParticipationRows = new List<SingleParticipationProjection>();
        private readonly List<StatusTransitionProjection> bufferedStatusTransitions = new List<StatusTransitionProjection>();
        private readonly Dictionary<int, TourRuntimeCaptureState> tourRuntimeStateByTourId = new Dictionary<int, TourRuntimeCaptureState>();
        private readonly Dictionary<int, ConcertCastChangeSnapshot> concertEditBaselineByConcertId = new Dictionary<int, ConcertCastChangeSnapshot>();
        private readonly Dictionary<int, int> resolvedSingleChartPositionBySingleId = new Dictionary<int, int>();
        private readonly Dictionary<string, int> pendingSubstoryCompletionCountByDialogueId = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> idempotencyKeysForCurrentDate = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, NamespaceSessionRegistration> namespaceRegistrations =
            new Dictionary<string, NamespaceSessionRegistration>(StringComparer.Ordinal);

        private ICoreStorageEngine storageEngine;
        private bool initialized;
        private string activeSaveKey = CoreConstants.DefaultSaveKey;
        private DateTime nextFlushUtc;
        private int idempotencyDateKey = CoreConstants.UninitializedDateKey;

        /// <summary>
        /// Prevents external construction and enforces singleton lifecycle.
        /// </summary>
        private IMDataCoreController()
        {
            nextFlushUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// Singleton accessor used by patches and public API methods.
        /// </summary>
        internal static IMDataCoreController Instance
        {
            get { return sharedInstance; }
        }

        /// <summary>
        /// Returns true when the runtime and storage are initialized.
        /// </summary>
        internal bool IsReady()
        {
            lock (runtimeLock)
            {
                return initialized && storageEngine != null;
            }
        }

        /// <summary>
        /// Performs one-time startup initialization when game systems become available.
        /// </summary>
        internal void BootstrapIfNeeded()
        {
            string errorMessage;
            if (!EnsureInitialized(out errorMessage))
            {
                CoreLog.Warn(errorMessage);
            }
        }

        /// <summary>
        /// Registers a namespace and returns a session bound to the calling assembly.
        /// </summary>
        internal bool TryRegisterNamespace(string namespaceIdentifier, Assembly callingAssembly, out IMDataCoreSession session, out string errorMessage)
        {
            session = null;
            errorMessage = string.Empty;

            lock (runtimeLock)
            {
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    return false;
                }

                string sanitizedNamespaceIdentifier;
                if (!TrySanitizeStrictToken(
                    namespaceIdentifier,
                    CoreConstants.NamespaceMaximumLength,
                    CoreConstants.NamespaceMinimumLength,
                    CoreConstants.MessageNamespaceInvalid,
                    out sanitizedNamespaceIdentifier,
                    out errorMessage))
                {
                    return false;
                }

                string callingAssemblyIdentity = BuildAssemblyIdentity(callingAssembly);
                NamespaceSessionRegistration existingRegistration;
                if (namespaceRegistrations.TryGetValue(sanitizedNamespaceIdentifier, out existingRegistration))
                {
                    if (!string.Equals(existingRegistration.CallingAssemblyIdentity, callingAssemblyIdentity, StringComparison.Ordinal))
                    {
                        errorMessage = CoreConstants.MessageNamespaceAlreadyClaimed;
                        return false;
                    }
                }

                string sessionToken = Guid.NewGuid().ToString(CoreConstants.SessionTokenFormat, CultureInfo.InvariantCulture);
                NamespaceSessionRegistration registration = new NamespaceSessionRegistration
                {
                    NamespaceIdentifier = sanitizedNamespaceIdentifier,
                    SessionToken = sessionToken,
                    CallingAssemblyIdentity = callingAssemblyIdentity
                };

                namespaceRegistrations[sanitizedNamespaceIdentifier] = registration;
                session = new IMDataCoreSession(sanitizedNamespaceIdentifier, sessionToken);
                return true;
            }
        }

        /// <summary>
        /// Unregisters a namespace session for the calling assembly.
        /// </summary>
        internal bool TryUnregisterNamespace(IMDataCoreSession session, Assembly callingAssembly, out string errorMessage)
        {
            errorMessage = string.Empty;

            lock (runtimeLock)
            {
                NamespaceSessionRegistration registration;
                if (!TryValidateSessionLocked(session, callingAssembly, out registration, out errorMessage))
                {
                    return false;
                }

                namespaceRegistrations.Remove(registration.NamespaceIdentifier);
                return true;
            }
        }

        /// <summary>
        /// Persists a caller-scoped JSON document under a namespaced key.
        /// </summary>
        internal bool TrySetCustomJson(IMDataCoreSession session, Assembly callingAssembly, string dataKey, string jsonValue, out string errorMessage)
        {
            errorMessage = string.Empty;

            lock (runtimeLock)
            {
                NamespaceSessionRegistration registration;
                if (!TryValidateSessionLocked(session, callingAssembly, out registration, out errorMessage))
                {
                    return false;
                }

                string sanitizedDataKey;
                if (!TrySanitizeStrictToken(
                    dataKey,
                    CoreConstants.DataKeyMaximumLength,
                    CoreConstants.DataKeyMinimumLength,
                    CoreConstants.MessageDataKeyInvalid,
                    out sanitizedDataKey,
                    out errorMessage))
                {
                    return false;
                }

                if (jsonValue == null)
                {
                    errorMessage = CoreConstants.MessageJsonValueNull;
                    return false;
                }

                if (!EnsureInitializedLocked(out errorMessage))
                {
                    return false;
                }

                return storageEngine.TrySetCustomData(activeSaveKey, registration.NamespaceIdentifier, sanitizedDataKey, jsonValue, out errorMessage);
            }
        }

        /// <summary>
        /// Reads a caller-scoped JSON document from namespaced storage.
        /// </summary>
        internal bool TryGetCustomJson(IMDataCoreSession session, Assembly callingAssembly, string dataKey, out string jsonValue, out string errorMessage)
        {
            jsonValue = string.Empty;
            errorMessage = string.Empty;

            lock (runtimeLock)
            {
                NamespaceSessionRegistration registration;
                if (!TryValidateSessionLocked(session, callingAssembly, out registration, out errorMessage))
                {
                    return false;
                }

                string sanitizedDataKey;
                if (!TrySanitizeStrictToken(
                    dataKey,
                    CoreConstants.DataKeyMaximumLength,
                    CoreConstants.DataKeyMinimumLength,
                    CoreConstants.MessageDataKeyInvalid,
                    out sanitizedDataKey,
                    out errorMessage))
                {
                    return false;
                }

                if (!EnsureInitializedLocked(out errorMessage))
                {
                    return false;
                }

                return storageEngine.TryGetCustomData(activeSaveKey, registration.NamespaceIdentifier, sanitizedDataKey, out jsonValue, out errorMessage);
            }
        }

        /// <summary>
        /// Deletes a caller-scoped JSON document from namespaced storage.
        /// </summary>
        internal bool TryRemoveCustomJson(IMDataCoreSession session, Assembly callingAssembly, string dataKey, out string errorMessage)
        {
            errorMessage = string.Empty;

            lock (runtimeLock)
            {
                NamespaceSessionRegistration registration;
                if (!TryValidateSessionLocked(session, callingAssembly, out registration, out errorMessage))
                {
                    return false;
                }

                string sanitizedDataKey;
                if (!TrySanitizeStrictToken(
                    dataKey,
                    CoreConstants.DataKeyMaximumLength,
                    CoreConstants.DataKeyMinimumLength,
                    CoreConstants.MessageDataKeyInvalid,
                    out sanitizedDataKey,
                    out errorMessage))
                {
                    return false;
                }

                if (!EnsureInitializedLocked(out errorMessage))
                {
                    return false;
                }

                return storageEngine.TryRemoveCustomData(activeSaveKey, registration.NamespaceIdentifier, sanitizedDataKey, out errorMessage);
            }
        }

        /// <summary>
        /// Appends a custom event provided by another mod.
        /// </summary>
        internal bool TryAppendCustomEvent(
            IMDataCoreSession session,
            Assembly callingAssembly,
            int idolId,
            string entityKind,
            string entityId,
            string eventType,
            string payloadJson,
            string sourcePatch,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            lock (runtimeLock)
            {
                NamespaceSessionRegistration registration;
                if (!TryValidateSessionLocked(session, callingAssembly, out registration, out errorMessage))
                {
                    return false;
                }

                string sanitizedEntityKind;
                if (!TrySanitizeStrictToken(
                    entityKind,
                    CoreConstants.EventKindMaximumLength,
                    CoreConstants.MinimumTokenLength,
                    CoreConstants.MessageEntityKindInvalid,
                    out sanitizedEntityKind,
                    out errorMessage))
                {
                    return false;
                }

                string sanitizedEventType;
                if (!TrySanitizeStrictToken(
                    eventType,
                    CoreConstants.EventTypeMaximumLength,
                    CoreConstants.MinimumTokenLength,
                    CoreConstants.MessageEventTypeInvalid,
                    out sanitizedEventType,
                    out errorMessage))
                {
                    return false;
                }

                string sanitizedEntityId = CoreTokenUtility.SanitizeToken(entityId, CoreConstants.EventIdMaximumLength);
                string sanitizedSourcePatch = CoreTokenUtility.SanitizeToken(sourcePatch, CoreConstants.EventSourceMaximumLength);
                if (string.IsNullOrEmpty(sanitizedSourcePatch))
                {
                    sanitizedSourcePatch = CoreConstants.EventSourceCustomApi;
                }

                if (payloadJson == null)
                {
                    errorMessage = CoreConstants.MessagePayloadNull;
                    return false;
                }

                if (!EnsureInitializedLocked(out errorMessage))
                {
                    return false;
                }

                DateTime gameDate = staticVars.dateTime;
                PendingEvent pendingEvent = new PendingEvent
                {
                    SaveKey = activeSaveKey,
                    GameDateKey = CoreDateTimeUtility.BuildGameDateKey(gameDate),
                    GameDateTime = CoreDateTimeUtility.ToRoundTripString(gameDate),
                    IdolId = idolId >= CoreConstants.MinimumValidIdolIdentifier ? idolId : CoreConstants.InvalidIdValue,
                    EntityKind = sanitizedEntityKind,
                    EntityId = sanitizedEntityId,
                    EventType = sanitizedEventType,
                    SourcePatch = sanitizedSourcePatch,
                    NamespaceIdentifier = registration.NamespaceIdentifier,
                    PayloadJson = payloadJson
                };

                bufferedEvents.Add(pendingEvent);
                return FlushLocked(false, out errorMessage);
            }
        }

        /// <summary>
        /// Reads recent persisted events for one idol.
        /// </summary>
        internal bool TryReadRecentEventsForIdol(int idolId, int maxCount, out List<IMDataCoreEvent> events, out string errorMessage)
        {
            events = new List<IMDataCoreEvent>();
            errorMessage = string.Empty;

            if (idolId < CoreConstants.MinimumValidIdolIdentifier)
            {
                errorMessage = CoreConstants.MessageIdolInvalid;
                return false;
            }

            lock (runtimeLock)
            {
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    return false;
                }

                if (!FlushLocked(true, out errorMessage))
                {
                    return false;
                }

                int clampedMaxCount = Mathf.Clamp(
                    maxCount,
                    CoreConstants.MinimumRecentEventRequestCount,
                    CoreConstants.MaximumRecentEventRequestCount);

                return storageEngine.TryReadRecentEventsForIdol(activeSaveKey, idolId, clampedMaxCount, out events, out errorMessage);
            }
        }

        /// <summary>
        /// Forces an immediate flush of all queued writes.
        /// </summary>
        internal bool TryFlushNow(out string errorMessage)
        {
            errorMessage = string.Empty;

            lock (runtimeLock)
            {
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    return false;
                }

                return FlushLocked(true, out errorMessage);
            }
        }

        /// <summary>
        /// Returns the save key currently bound to the active storage engine.
        /// </summary>
        internal bool TryGetActiveSaveKey(out string saveKey, out string errorMessage)
        {
            saveKey = CoreConstants.DefaultSaveKey;
            errorMessage = string.Empty;

            lock (runtimeLock)
            {
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    return false;
                }

                saveKey = activeSaveKey;
                return true;
            }
        }

        /// <summary>
        /// Forces persistence before game save operations.
        /// </summary>
        internal void ForceFlushBeforeSave()
        {
            CaptureResolvedSingleChartPositionsBeforeSave();

            string errorMessage;
            if (!TryFlushNow(out errorMessage))
            {
                CoreLog.Warn(CoreConstants.EventSourceSaveFlushPatch + CoreConstants.LogSeparatorColonSpace + errorMessage);
            }
        }

        /// <summary>
        /// Captures one save-file path hint before load starts so storage rebinding resolves per file.
        /// </summary>
        internal void OnSaveLoadStarting(string saveFilePath)
        {
            CorePaths.SetActiveSaveFilePathHint(saveFilePath);
        }

        /// <summary>
        /// Rebinds storage to the save file that is about to be written.
        /// When the target slot differs, current storage is cloned into the target slot before write.
        /// </summary>
        internal void OnSaveWriteStarting(string saveFilePath)
        {
            lock (runtimeLock)
            {
                string initializationErrorMessage;
                if (!EnsureInitializedLocked(out initializationErrorMessage))
                {
                    CoreLog.Warn(CoreConstants.MessageSaveWritePreparationFailurePrefix + initializationErrorMessage);
                    return;
                }

                string sourceSaveKey = activeSaveKey;
                CorePaths.SetActiveSaveFilePathHint(saveFilePath);
                string targetSaveKey = NormalizeSaveKey(CorePaths.GetSaveKey());
                if (string.IsNullOrEmpty(targetSaveKey) ||
                    string.Equals(targetSaveKey, sourceSaveKey, StringComparison.Ordinal))
                {
                    return;
                }

                string flushErrorMessage;
                if (!FlushLocked(true, out flushErrorMessage))
                {
                    CoreLog.Warn(CoreConstants.MessageStorageSaveSwitchFailure + CoreConstants.MessageFlushFailed + flushErrorMessage);
                }

                DisposeStorageLocked();

                string switchInitializationErrorMessage;
                if (!InitializeStorageLocked(
                    targetSaveKey,
                    sourceSaveKey,
                    true,
                    out switchInitializationErrorMessage))
                {
                    CoreLog.Warn(CoreConstants.MessageSaveWritePreparationFailurePrefix + switchInitializationErrorMessage);
                }
            }
        }

        /// <summary>
        /// Rebinds storage to the loaded save file and removes rows newer than the loaded save snapshot.
        /// </summary>
        internal void OnSaveLoaded(string saveFilePath)
        {
            lock (runtimeLock)
            {
                CorePaths.SetActiveSaveFilePathHint(saveFilePath);

                string initializationErrorMessage;
                if (!EnsureInitializedLocked(out initializationErrorMessage))
                {
                    CoreLog.Warn(CoreConstants.MessageSaveLoadInitializationFailurePrefix + initializationErrorMessage);
                    return;
                }

                if (storageEngine == null)
                {
                    CoreLog.Warn(CoreConstants.MessageStorageUnavailable);
                    return;
                }

                DateTime loadedSnapshotDateTime = staticVars.dateTime;
                string rollbackErrorMessage;
                if (!storageEngine.TryRollbackToGameDateTime(activeSaveKey, loadedSnapshotDateTime, out rollbackErrorMessage))
                {
                    CoreLog.Warn(CoreConstants.MessageSaveLoadRollbackFailurePrefix + rollbackErrorMessage);
                }

                RemoveBufferedDataForSaveKeyLocked(activeSaveKey);
                ResetRuntimeCaptureStateLocked();
            }
        }

        /// <summary>
        /// Captures chart-position backfill events for released singles before save flush.
        /// </summary>
        private void CaptureResolvedSingleChartPositionsBeforeSave()
        {
            List<KeyValuePair<singles._single, int>> pendingChartUpdates = new List<KeyValuePair<singles._single, int>>();

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                if (singles.Singles == null || singles.Singles.Count < CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    return;
                }

                for (int singleIndex = CoreConstants.ZeroBasedListStartIndex; singleIndex < singles.Singles.Count; singleIndex++)
                {
                    singles._single releasedSingle = singles.Singles[singleIndex];
                    if (releasedSingle == null
                        || releasedSingle.id < CoreConstants.MinimumValidIdolIdentifier
                        || releasedSingle.status != singles._single._status.released)
                    {
                        continue;
                    }

                    int chartPosition = ResolveChartPosition(releasedSingle);
                    if (chartPosition <= CoreConstants.ZeroBasedListStartIndex)
                    {
                        continue;
                    }

                    int knownChartPosition;
                    if (resolvedSingleChartPositionBySingleId.TryGetValue(releasedSingle.id, out knownChartPosition)
                        && knownChartPosition == chartPosition)
                    {
                        continue;
                    }

                    pendingChartUpdates.Add(new KeyValuePair<singles._single, int>(releasedSingle, chartPosition));
                }
            }

            for (int updateIndex = CoreConstants.ZeroBasedListStartIndex; updateIndex < pendingChartUpdates.Count; updateIndex++)
            {
                KeyValuePair<singles._single, int> update = pendingChartUpdates[updateIndex];
                CaptureSingleChartPositionResolved(update.Key, update.Value, CoreConstants.EventSourceSingleChartBackfillPatch);
            }
        }

        /// <summary>
        /// Ensures runtime initialization and save binding exist.
        /// </summary>
        private bool EnsureInitialized(out string errorMessage)
        {
            lock (runtimeLock)
            {
                return EnsureInitializedLocked(out errorMessage);
            }
        }

        /// <summary>
        /// Ensures runtime initialization and save binding exist while caller already holds runtime lock.
        /// </summary>
        private bool EnsureInitializedLocked(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!initialized || storageEngine == null)
            {
                return InitializeStorageLocked(CorePaths.GetSaveKey(), activeSaveKey, out errorMessage);
            }

            string currentSaveKey = NormalizeSaveKey(CorePaths.GetSaveKey());
            if (string.Equals(currentSaveKey, activeSaveKey, StringComparison.Ordinal))
            {
                return true;
            }

            string flushErrorMessage;
            if (!FlushLocked(true, out flushErrorMessage))
            {
                CoreLog.Warn(CoreConstants.MessageStorageSaveSwitchFailure + CoreConstants.MessageFlushFailed + flushErrorMessage);
            }

            string previousSaveKey = activeSaveKey;
            DisposeStorageLocked();
            if (!InitializeStorageLocked(currentSaveKey, previousSaveKey, out errorMessage))
            {
                errorMessage = CoreConstants.MessageStorageSaveSwitchFailure + errorMessage;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates and initializes a storage engine for one save key.
        /// </summary>
        private bool InitializeStorageLocked(string saveKey, out string errorMessage)
        {
            return InitializeStorageLocked(saveKey, string.Empty, false, out errorMessage);
        }

        /// <summary>
        /// Creates and initializes a storage engine for one save key with optional source-save migration.
        /// </summary>
        private bool InitializeStorageLocked(string saveKey, string sourceSaveKeyForMigration, out string errorMessage)
        {
            return InitializeStorageLocked(saveKey, sourceSaveKeyForMigration, false, out errorMessage);
        }

        /// <summary>
        /// Creates and initializes one storage engine with optional source-save copy semantics.
        /// </summary>
        private bool InitializeStorageLocked(
            string saveKey,
            string sourceSaveKeyForMigration,
            bool overwriteTargetWithSourceStorage,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            string normalizedSaveKey = NormalizeSaveKey(saveKey);
            string sqliteDatabasePath = CorePaths.GetDatabasePath(normalizedSaveKey);
            string flatFileDatabasePath = CorePaths.GetFlatFileDatabasePath(normalizedSaveKey);
            string copiedFromSaveKey;
            TryMigrateLegacyStorageIfNeeded(
                normalizedSaveKey,
                sourceSaveKeyForMigration,
                sqliteDatabasePath,
                flatFileDatabasePath,
                overwriteTargetWithSourceStorage,
                out copiedFromSaveKey);

            ICoreStorageEngine newStorageEngine;
            if (!TryCreateAndInitializeStorageEngine(sqliteDatabasePath, flatFileDatabasePath, out newStorageEngine, out errorMessage))
            {
                storageEngine = null;
                initialized = false;
                errorMessage = CoreConstants.MessageStorageInitializationFailure + errorMessage;
                return false;
            }

            if (!string.IsNullOrEmpty(copiedFromSaveKey)
                && !string.Equals(copiedFromSaveKey, normalizedSaveKey, StringComparison.Ordinal))
            {
                string remapErrorMessage;
                if (!newStorageEngine.TryRemapSaveKey(copiedFromSaveKey, normalizedSaveKey, out remapErrorMessage))
                {
                    newStorageEngine.Dispose();
                    storageEngine = null;
                    initialized = false;
                    errorMessage = CoreConstants.MessageStorageInitializationFailure + remapErrorMessage;
                    return false;
                }
            }

            string normalizeSaveKeyErrorMessage;
            if (!newStorageEngine.TryRemapSaveKey(string.Empty, normalizedSaveKey, out normalizeSaveKeyErrorMessage))
            {
                newStorageEngine.Dispose();
                storageEngine = null;
                initialized = false;
                errorMessage = CoreConstants.MessageStorageInitializationFailure + normalizeSaveKeyErrorMessage;
                return false;
            }

            storageEngine = newStorageEngine;
            activeSaveKey = normalizedSaveKey;
            initialized = true;
            nextFlushUtc = DateTime.UtcNow.AddSeconds(CoreConstants.FlushIntervalSeconds);
            ResetRuntimeCaptureStateLocked();
            CoreLog.Info(CoreConstants.MessageCoreInitializedForSaveKeyPrefix + activeSaveKey + CoreConstants.MessageCoreInitializedForSaveKeySuffix);
            return true;
        }

        /// <summary>
        /// Copies legacy agency-scoped storage into the new file-scoped location on first access.
        /// </summary>
        private void TryMigrateLegacyStorageIfNeeded(
            string normalizedSaveKey,
            string sourceSaveKeyForMigration,
            string sqliteDatabasePath,
            string flatFileDatabasePath,
            bool overwriteTargetWithSourceStorage,
            out string copiedFromSaveKey)
        {
            copiedFromSaveKey = string.Empty;
            string normalizedSourceSaveKey = NormalizeSaveKey(sourceSaveKeyForMigration);
            bool targetStorageExists = File.Exists(sqliteDatabasePath) || File.Exists(flatFileDatabasePath);
            if (overwriteTargetWithSourceStorage)
            {
                if (TryCopyStorageFromSourceSaveKey(
                    normalizedSourceSaveKey,
                    normalizedSaveKey,
                    sqliteDatabasePath,
                    flatFileDatabasePath,
                    true))
                {
                    copiedFromSaveKey = normalizedSourceSaveKey;
                    return;
                }

                if (targetStorageExists)
                {
                    return;
                }
            }
            else if (targetStorageExists)
            {
                return;
            }

            if (TryCopyStorageFromSourceSaveKey(
                normalizedSourceSaveKey,
                normalizedSaveKey,
                sqliteDatabasePath,
                flatFileDatabasePath,
                false))
            {
                copiedFromSaveKey = normalizedSourceSaveKey;
                return;
            }

            string rawLegacyAgencySaveKey = CorePaths.GetLegacyAgencySaveKey();
            if (rawLegacyAgencySaveKey.Length < CoreConstants.SaveKeyMinimumLength)
            {
                return;
            }

            string legacyAgencySaveKey = NormalizeSaveKey(rawLegacyAgencySaveKey);
            if (string.IsNullOrEmpty(legacyAgencySaveKey))
            {
                return;
            }

            if (TryCopyStorageFromSourceSaveKey(
                legacyAgencySaveKey,
                normalizedSaveKey,
                sqliteDatabasePath,
                flatFileDatabasePath,
                false))
            {
                copiedFromSaveKey = legacyAgencySaveKey;
            }
        }

        /// <summary>
        /// Copies storage from one source save-key into one target save-key.
        /// </summary>
        private static bool TryCopyStorageFromSourceSaveKey(
            string sourceSaveKey,
            string targetSaveKey,
            string targetSqliteDatabasePath,
            string targetFlatFileDatabasePath,
            bool replaceTargetStorageFiles)
        {
            if (string.IsNullOrEmpty(sourceSaveKey))
            {
                return false;
            }

            string normalizedSourceSaveKey = NormalizeSaveKey(sourceSaveKey);
            if (string.IsNullOrEmpty(normalizedSourceSaveKey) ||
                string.Equals(normalizedSourceSaveKey, targetSaveKey, StringComparison.Ordinal))
            {
                return false;
            }

            string sourceSqliteDatabasePath = CorePaths.GetDatabasePath(normalizedSourceSaveKey);
            string sourceFlatFileDatabasePath = CorePaths.GetFlatFileDatabasePath(normalizedSourceSaveKey);
            string sourceSqliteWriteAheadLogPath = sourceSqliteDatabasePath + CoreConstants.SqliteWriteAheadLogFileSuffix;
            string sourceSqliteSharedMemoryPath = sourceSqliteDatabasePath + CoreConstants.SqliteSharedMemoryFileSuffix;
            string targetSqliteWriteAheadLogPath = targetSqliteDatabasePath + CoreConstants.SqliteWriteAheadLogFileSuffix;
            string targetSqliteSharedMemoryPath = targetSqliteDatabasePath + CoreConstants.SqliteSharedMemoryFileSuffix;
            try
            {
                if (File.Exists(sourceSqliteDatabasePath))
                {
                    if (replaceTargetStorageFiles)
                    {
                        DeleteFileIfExists(targetSqliteDatabasePath);
                        DeleteFileIfExists(targetSqliteWriteAheadLogPath);
                        DeleteFileIfExists(targetSqliteSharedMemoryPath);
                        DeleteFileIfExists(targetFlatFileDatabasePath);
                    }

                    string targetDirectory = Path.GetDirectoryName(targetSqliteDatabasePath);
                    if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }

                    File.Copy(sourceSqliteDatabasePath, targetSqliteDatabasePath, replaceTargetStorageFiles);
                    CopyOptionalFileIfExists(
                        sourceSqliteWriteAheadLogPath,
                        targetSqliteWriteAheadLogPath,
                        replaceTargetStorageFiles);
                    CopyOptionalFileIfExists(
                        sourceSqliteSharedMemoryPath,
                        targetSqliteSharedMemoryPath,
                        replaceTargetStorageFiles);
                    return true;
                }

                if (File.Exists(sourceFlatFileDatabasePath))
                {
                    if (replaceTargetStorageFiles)
                    {
                        DeleteFileIfExists(targetSqliteDatabasePath);
                        DeleteFileIfExists(targetSqliteWriteAheadLogPath);
                        DeleteFileIfExists(targetSqliteSharedMemoryPath);
                        DeleteFileIfExists(targetFlatFileDatabasePath);
                    }

                    string targetDirectory = Path.GetDirectoryName(targetFlatFileDatabasePath);
                    if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }

                    File.Copy(sourceFlatFileDatabasePath, targetFlatFileDatabasePath, replaceTargetStorageFiles);
                    return true;
                }
            }
            catch (Exception exception)
            {
                CoreLog.Warn(CoreConstants.MessageSaveDataMigrationFailedPrefix + exception.Message);
            }

            return false;
        }

        /// <summary>
        /// Copies one optional file if it exists in source storage.
        /// </summary>
        private static void CopyOptionalFileIfExists(
            string sourceFilePath,
            string targetFilePath,
            bool overwriteTargetFile)
        {
            if (!File.Exists(sourceFilePath))
            {
                return;
            }

            if (!overwriteTargetFile && File.Exists(targetFilePath))
            {
                return;
            }

            string targetDirectory = Path.GetDirectoryName(targetFilePath);
            if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            File.Copy(sourceFilePath, targetFilePath, overwriteTargetFile);
        }

        /// <summary>
        /// Deletes one file when it exists.
        /// </summary>
        private static void DeleteFileIfExists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return;
            }

            File.Delete(filePath);
        }

        /// <summary>
        /// Clears transient capture state that must not leak between save loads.
        /// </summary>
        private void ResetRuntimeCaptureStateLocked()
        {
            tourRuntimeStateByTourId.Clear();
            concertEditBaselineByConcertId.Clear();
            resolvedSingleChartPositionBySingleId.Clear();
            pendingSubstoryCompletionCountByDialogueId.Clear();
            idempotencyKeysForCurrentDate.Clear();
            idempotencyDateKey = CoreConstants.UninitializedDateKey;
        }

        /// <summary>
        /// Selects a runtime-compatible storage engine and initializes it.
        /// </summary>
        private bool TryCreateAndInitializeStorageEngine(
            string sqliteDatabasePath,
            string flatFileDatabasePath,
            out ICoreStorageEngine createdStorageEngine,
            out string errorMessage)
        {
            createdStorageEngine = null;
            errorMessage = string.Empty;

            string runtimeDependencyErrorMessage;
            if (CoreRuntimeCapabilities.TryEnsureSqliteRuntimeSupport(out runtimeDependencyErrorMessage))
            {
                string sqliteInitializationError;
                if (TryInitializeSqliteStorageEngine(sqliteDatabasePath, out createdStorageEngine, out sqliteInitializationError))
                {
                    return true;
                }

                CoreLog.Warn(CoreConstants.MessageSqliteUnavailableFallbackPrefix + sqliteInitializationError);
            }
            else
            {
                CoreLog.Warn(CoreConstants.MessageRuntimeDependencyProbeFailedPrefix + runtimeDependencyErrorMessage);
            }

            ICoreStorageEngine flatFileStorageEngine = new FlatFileCoreStorageEngine();
            if (!flatFileStorageEngine.Initialize(flatFileDatabasePath, out errorMessage))
            {
                flatFileStorageEngine.Dispose();
                return false;
            }

            createdStorageEngine = flatFileStorageEngine;
            return true;
        }

        /// <summary>
        /// Tries to initialize the SQLite storage engine and captures compatibility failures.
        /// </summary>
        private static bool TryInitializeSqliteStorageEngine(string databasePath, out ICoreStorageEngine createdStorageEngine, out string errorMessage)
        {
            createdStorageEngine = null;
            errorMessage = string.Empty;

            ICoreStorageEngine sqliteStorageEngine = null;
            try
            {
                sqliteStorageEngine = new SqliteCoreStorageEngine();
                if (!sqliteStorageEngine.Initialize(databasePath, out errorMessage))
                {
                    sqliteStorageEngine.Dispose();
                    return false;
                }

                createdStorageEngine = sqliteStorageEngine;
                return true;
            }
            catch (Exception exception)
            {
                if (sqliteStorageEngine != null)
                {
                    try
                    {
                        sqliteStorageEngine.Dispose();
                    }
                    catch
                    {
                    }
                }

                errorMessage = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// Disposes the current storage engine.
        /// </summary>
        private void DisposeStorageLocked()
        {
            if (storageEngine != null)
            {
                storageEngine.Dispose();
                storageEngine = null;
            }
        }

        /// <summary>
        /// Flushes buffers based on policy or on-demand forcing.
        /// </summary>
        private bool FlushLocked(bool forceFlush, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (storageEngine == null)
            {
                errorMessage = CoreConstants.MessageStorageUnavailable;
                return false;
            }

            bool hasBufferedData = HasBufferedDataForSaveKeyLocked(activeSaveKey);

            if (!hasBufferedData)
            {
                return true;
            }

            int bufferedEventCount = CountBufferedEventsForSaveKeyLocked(activeSaveKey);
            bool thresholdReached = bufferedEventCount >= CoreConstants.ImmediateFlushQueueThreshold;
            bool intervalElapsed = DateTime.UtcNow >= nextFlushUtc;
            if (!forceFlush && !thresholdReached && !intervalElapsed)
            {
                return true;
            }

            List<PendingEvent> eventsSnapshot = CreateEventSnapshotForSaveKeyLocked(activeSaveKey);
            List<SingleParticipationProjection> singleProjectionSnapshot = CreateSingleParticipationSnapshotForSaveKeyLocked(activeSaveKey);
            List<StatusTransitionProjection> transitionSnapshot = CreateStatusTransitionSnapshotForSaveKeyLocked(activeSaveKey);
            bool hasSnapshotData =
                eventsSnapshot.Count >= CoreConstants.MinimumQueueSizeForFlush
                || singleProjectionSnapshot.Count >= CoreConstants.MinimumQueueSizeForFlush
                || transitionSnapshot.Count >= CoreConstants.MinimumQueueSizeForFlush;
            if (!hasSnapshotData)
            {
                return true;
            }

            if (!storageEngine.PersistBatch(eventsSnapshot, singleProjectionSnapshot, transitionSnapshot, out errorMessage))
            {
                TrimBuffersAfterPersistenceFailureLocked(activeSaveKey);
                nextFlushUtc = DateTime.UtcNow.AddSeconds(CoreConstants.FlushIntervalSeconds);
                return false;
            }

            RemoveBufferedDataForSaveKeyLocked(activeSaveKey);
            nextFlushUtc = DateTime.UtcNow.AddSeconds(CoreConstants.FlushIntervalSeconds);
            return true;
        }

        /// <summary>
        /// Returns true when at least one buffered record exists for the requested save key.
        /// </summary>
        private bool HasBufferedDataForSaveKeyLocked(string saveKey)
        {
            return CountBufferedEventsForSaveKeyLocked(saveKey) >= CoreConstants.MinimumQueueSizeForFlush
                || CountBufferedSingleParticipationRowsForSaveKeyLocked(saveKey) >= CoreConstants.MinimumQueueSizeForFlush
                || CountBufferedStatusTransitionsForSaveKeyLocked(saveKey) >= CoreConstants.MinimumQueueSizeForFlush;
        }

        /// <summary>
        /// Counts buffered event rows for the requested save key.
        /// </summary>
        private int CountBufferedEventsForSaveKeyLocked(string saveKey)
        {
            int matchingEventCount = CoreConstants.ZeroBasedListStartIndex;
            for (int eventIndex = CoreConstants.ZeroBasedListStartIndex; eventIndex < bufferedEvents.Count; eventIndex++)
            {
                PendingEvent pendingEvent = bufferedEvents[eventIndex];
                if (pendingEvent != null && string.Equals(pendingEvent.SaveKey, saveKey, StringComparison.Ordinal))
                {
                    matchingEventCount++;
                }
            }

            return matchingEventCount;
        }

        /// <summary>
        /// Counts buffered single-participation projection rows for the requested save key.
        /// </summary>
        private int CountBufferedSingleParticipationRowsForSaveKeyLocked(string saveKey)
        {
            int matchingProjectionCount = CoreConstants.ZeroBasedListStartIndex;
            for (int rowIndex = CoreConstants.ZeroBasedListStartIndex; rowIndex < bufferedSingleParticipationRows.Count; rowIndex++)
            {
                SingleParticipationProjection projection = bufferedSingleParticipationRows[rowIndex];
                if (projection != null && string.Equals(projection.SaveKey, saveKey, StringComparison.Ordinal))
                {
                    matchingProjectionCount++;
                }
            }

            return matchingProjectionCount;
        }

        /// <summary>
        /// Counts buffered status-transition projection rows for the requested save key.
        /// </summary>
        private int CountBufferedStatusTransitionsForSaveKeyLocked(string saveKey)
        {
            int matchingTransitionCount = CoreConstants.ZeroBasedListStartIndex;
            for (int transitionIndex = CoreConstants.ZeroBasedListStartIndex; transitionIndex < bufferedStatusTransitions.Count; transitionIndex++)
            {
                StatusTransitionProjection transition = bufferedStatusTransitions[transitionIndex];
                if (transition != null && string.Equals(transition.SaveKey, saveKey, StringComparison.Ordinal))
                {
                    matchingTransitionCount++;
                }
            }

            return matchingTransitionCount;
        }

        /// <summary>
        /// Creates one event snapshot containing only rows for the active save key.
        /// </summary>
        private List<PendingEvent> CreateEventSnapshotForSaveKeyLocked(string saveKey)
        {
            List<PendingEvent> saveScopedSnapshot = new List<PendingEvent>();
            for (int eventIndex = CoreConstants.ZeroBasedListStartIndex; eventIndex < bufferedEvents.Count; eventIndex++)
            {
                PendingEvent pendingEvent = bufferedEvents[eventIndex];
                if (pendingEvent != null && string.Equals(pendingEvent.SaveKey, saveKey, StringComparison.Ordinal))
                {
                    saveScopedSnapshot.Add(pendingEvent);
                }
            }

            return saveScopedSnapshot;
        }

        /// <summary>
        /// Creates one single-participation snapshot containing only rows for the active save key.
        /// </summary>
        private List<SingleParticipationProjection> CreateSingleParticipationSnapshotForSaveKeyLocked(string saveKey)
        {
            List<SingleParticipationProjection> saveScopedSnapshot = new List<SingleParticipationProjection>();
            for (int rowIndex = CoreConstants.ZeroBasedListStartIndex; rowIndex < bufferedSingleParticipationRows.Count; rowIndex++)
            {
                SingleParticipationProjection projection = bufferedSingleParticipationRows[rowIndex];
                if (projection != null && string.Equals(projection.SaveKey, saveKey, StringComparison.Ordinal))
                {
                    saveScopedSnapshot.Add(projection);
                }
            }

            return saveScopedSnapshot;
        }

        /// <summary>
        /// Creates one status-transition snapshot containing only rows for the active save key.
        /// </summary>
        private List<StatusTransitionProjection> CreateStatusTransitionSnapshotForSaveKeyLocked(string saveKey)
        {
            List<StatusTransitionProjection> saveScopedSnapshot = new List<StatusTransitionProjection>();
            for (int transitionIndex = CoreConstants.ZeroBasedListStartIndex; transitionIndex < bufferedStatusTransitions.Count; transitionIndex++)
            {
                StatusTransitionProjection transition = bufferedStatusTransitions[transitionIndex];
                if (transition != null && string.Equals(transition.SaveKey, saveKey, StringComparison.Ordinal))
                {
                    saveScopedSnapshot.Add(transition);
                }
            }

            return saveScopedSnapshot;
        }

        /// <summary>
        /// Removes all buffered rows for one save key after successful persistence.
        /// </summary>
        private void RemoveBufferedDataForSaveKeyLocked(string saveKey)
        {
            for (int eventIndex = bufferedEvents.Count - CoreConstants.LastElementOffsetFromCount; eventIndex >= CoreConstants.ZeroBasedListStartIndex; eventIndex--)
            {
                PendingEvent pendingEvent = bufferedEvents[eventIndex];
                if (pendingEvent != null && string.Equals(pendingEvent.SaveKey, saveKey, StringComparison.Ordinal))
                {
                    bufferedEvents.RemoveAt(eventIndex);
                }
            }

            for (int rowIndex = bufferedSingleParticipationRows.Count - CoreConstants.LastElementOffsetFromCount; rowIndex >= CoreConstants.ZeroBasedListStartIndex; rowIndex--)
            {
                SingleParticipationProjection projection = bufferedSingleParticipationRows[rowIndex];
                if (projection != null && string.Equals(projection.SaveKey, saveKey, StringComparison.Ordinal))
                {
                    bufferedSingleParticipationRows.RemoveAt(rowIndex);
                }
            }

            for (int transitionIndex = bufferedStatusTransitions.Count - CoreConstants.LastElementOffsetFromCount; transitionIndex >= CoreConstants.ZeroBasedListStartIndex; transitionIndex--)
            {
                StatusTransitionProjection transition = bufferedStatusTransitions[transitionIndex];
                if (transition != null && string.Equals(transition.SaveKey, saveKey, StringComparison.Ordinal))
                {
                    bufferedStatusTransitions.RemoveAt(transitionIndex);
                }
            }
        }

        /// <summary>
        /// Applies bounded retention when persistence fails to prevent unbounded memory growth.
        /// </summary>
        private void TrimBuffersAfterPersistenceFailureLocked(string saveKey)
        {
            TrimPendingEventsToMaximumCountForSaveKey(bufferedEvents, saveKey, CoreConstants.MaximumBufferedEventsAfterPersistenceFailure);
            TrimSingleParticipationRowsToMaximumCountForSaveKey(bufferedSingleParticipationRows, saveKey, CoreConstants.MaximumBufferedEventsAfterPersistenceFailure);
            TrimStatusTransitionsToMaximumCountForSaveKey(bufferedStatusTransitions, saveKey, CoreConstants.MaximumBufferedEventsAfterPersistenceFailure);
        }

        /// <summary>
        /// Keeps only the newest pending events up to the configured maximum count for one save key.
        /// </summary>
        private static void TrimPendingEventsToMaximumCountForSaveKey(List<PendingEvent> pendingEvents, string saveKey, int maximumCount)
        {
            if (pendingEvents == null)
            {
                return;
            }

            int matchingEventCount = CoreConstants.ZeroBasedListStartIndex;
            for (int eventIndex = CoreConstants.ZeroBasedListStartIndex; eventIndex < pendingEvents.Count; eventIndex++)
            {
                PendingEvent pendingEvent = pendingEvents[eventIndex];
                if (pendingEvent != null && string.Equals(pendingEvent.SaveKey, saveKey, StringComparison.Ordinal))
                {
                    matchingEventCount++;
                }
            }

            int matchingEventsToRemove = matchingEventCount - maximumCount;
            if (matchingEventsToRemove <= CoreConstants.ZeroBasedListStartIndex)
            {
                return;
            }

            for (int eventIndex = CoreConstants.ZeroBasedListStartIndex; eventIndex < pendingEvents.Count && matchingEventsToRemove > CoreConstants.ZeroBasedListStartIndex;)
            {
                PendingEvent pendingEvent = pendingEvents[eventIndex];
                if (pendingEvent != null && string.Equals(pendingEvent.SaveKey, saveKey, StringComparison.Ordinal))
                {
                    pendingEvents.RemoveAt(eventIndex);
                    matchingEventsToRemove--;
                    continue;
                }

                eventIndex++;
            }
        }

        /// <summary>
        /// Keeps only the newest single-participation rows up to the configured maximum count for one save key.
        /// </summary>
        private static void TrimSingleParticipationRowsToMaximumCountForSaveKey(
            List<SingleParticipationProjection> singleParticipationRows,
            string saveKey,
            int maximumCount)
        {
            if (singleParticipationRows == null)
            {
                return;
            }

            int matchingRowCount = CoreConstants.ZeroBasedListStartIndex;
            for (int rowIndex = CoreConstants.ZeroBasedListStartIndex; rowIndex < singleParticipationRows.Count; rowIndex++)
            {
                SingleParticipationProjection projection = singleParticipationRows[rowIndex];
                if (projection != null && string.Equals(projection.SaveKey, saveKey, StringComparison.Ordinal))
                {
                    matchingRowCount++;
                }
            }

            int matchingRowsToRemove = matchingRowCount - maximumCount;
            if (matchingRowsToRemove <= CoreConstants.ZeroBasedListStartIndex)
            {
                return;
            }

            for (int rowIndex = CoreConstants.ZeroBasedListStartIndex; rowIndex < singleParticipationRows.Count && matchingRowsToRemove > CoreConstants.ZeroBasedListStartIndex;)
            {
                SingleParticipationProjection projection = singleParticipationRows[rowIndex];
                if (projection != null && string.Equals(projection.SaveKey, saveKey, StringComparison.Ordinal))
                {
                    singleParticipationRows.RemoveAt(rowIndex);
                    matchingRowsToRemove--;
                    continue;
                }

                rowIndex++;
            }
        }

        /// <summary>
        /// Keeps only the newest status-transition rows up to the configured maximum count for one save key.
        /// </summary>
        private static void TrimStatusTransitionsToMaximumCountForSaveKey(
            List<StatusTransitionProjection> statusTransitions,
            string saveKey,
            int maximumCount)
        {
            if (statusTransitions == null)
            {
                return;
            }

            int matchingTransitionCount = CoreConstants.ZeroBasedListStartIndex;
            for (int transitionIndex = CoreConstants.ZeroBasedListStartIndex; transitionIndex < statusTransitions.Count; transitionIndex++)
            {
                StatusTransitionProjection transition = statusTransitions[transitionIndex];
                if (transition != null && string.Equals(transition.SaveKey, saveKey, StringComparison.Ordinal))
                {
                    matchingTransitionCount++;
                }
            }

            int matchingTransitionsToRemove = matchingTransitionCount - maximumCount;
            if (matchingTransitionsToRemove <= CoreConstants.ZeroBasedListStartIndex)
            {
                return;
            }

            for (int transitionIndex = CoreConstants.ZeroBasedListStartIndex; transitionIndex < statusTransitions.Count && matchingTransitionsToRemove > CoreConstants.ZeroBasedListStartIndex;)
            {
                StatusTransitionProjection transition = statusTransitions[transitionIndex];
                if (transition != null && string.Equals(transition.SaveKey, saveKey, StringComparison.Ordinal))
                {
                    statusTransitions.RemoveAt(transitionIndex);
                    matchingTransitionsToRemove--;
                    continue;
                }

                transitionIndex++;
            }
        }

        /// <summary>
        /// Validates and sanitizes a token while enforcing exact-safe input and length bounds.
        /// </summary>
        private static bool TrySanitizeStrictToken(
            string rawToken,
            int maximumLength,
            int minimumLength,
            string invalidMessage,
            out string sanitizedToken,
            out string errorMessage)
        {
            sanitizedToken = CoreTokenUtility.SanitizeToken(rawToken, maximumLength);
            errorMessage = string.Empty;

            bool hasMinimumLength = sanitizedToken.Length >= minimumLength;
            bool isExactSafeMatch = string.Equals(rawToken, sanitizedToken, StringComparison.Ordinal);
            if (!hasMinimumLength || !isExactSafeMatch)
            {
                sanitizedToken = string.Empty;
                errorMessage = invalidMessage;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates that a session belongs to the calling assembly and is still registered.
        /// </summary>
        private bool TryValidateSessionLocked(
            IMDataCoreSession session,
            Assembly callingAssembly,
            out NamespaceSessionRegistration registration,
            out string errorMessage)
        {
            registration = null;
            errorMessage = string.Empty;

            if (session == null)
            {
                errorMessage = CoreConstants.MessageSessionNull;
                return false;
            }

            NamespaceSessionRegistration registrationFromMap;
            if (!namespaceRegistrations.TryGetValue(session.NamespaceIdentifier, out registrationFromMap))
            {
                errorMessage = CoreConstants.MessageSessionNotRegistered;
                return false;
            }

            if (!string.Equals(registrationFromMap.NamespaceIdentifier, session.NamespaceIdentifier, StringComparison.Ordinal))
            {
                errorMessage = CoreConstants.MessageSessionNamespaceMismatch;
                return false;
            }

            if (!string.Equals(registrationFromMap.SessionToken, session.SessionToken, StringComparison.Ordinal))
            {
                errorMessage = CoreConstants.MessageSessionTokenMismatch;
                return false;
            }

            string callingAssemblyIdentity = BuildAssemblyIdentity(callingAssembly);
            if (!string.Equals(registrationFromMap.CallingAssemblyIdentity, callingAssemblyIdentity, StringComparison.Ordinal))
            {
                errorMessage = CoreConstants.MessageSessionAssemblyMismatch;
                return false;
            }

            registration = registrationFromMap;
            return true;
        }

        /// <summary>
        /// Builds a stable assembly identity used for namespace ownership checks.
        /// </summary>
        private static string BuildAssemblyIdentity(Assembly assembly)
        {
            if (assembly == null)
            {
                return CoreConstants.UnknownAssemblyIdentity;
            }

            string assemblyFullName = assembly.FullName ?? CoreConstants.UnknownAssemblyIdentity;
            string assemblyModuleVersionIdentifier = ResolveAssemblyModuleVersionIdentifier(assembly);
            string assemblyLocation = ResolveAssemblyLocation(assembly);
            string assemblyContentHash = ComputeAssemblyContentHashHex(assemblyLocation);

            return string.Format(
                CultureInfo.InvariantCulture,
                CoreConstants.AssemblyIdentityStrongFormat,
                assemblyFullName,
                assemblyModuleVersionIdentifier,
                assemblyLocation,
                assemblyContentHash);
        }

        /// <summary>
        /// Resolves module version identifier (MVID) for one assembly.
        /// </summary>
        private static string ResolveAssemblyModuleVersionIdentifier(Assembly assembly)
        {
            if (assembly == null)
            {
                return CoreConstants.UnknownAssemblyModuleVersionIdentifier;
            }

            try
            {
                Module manifestModule = assembly.ManifestModule;
                if (manifestModule == null)
                {
                    return CoreConstants.UnknownAssemblyModuleVersionIdentifier;
                }

                return manifestModule.ModuleVersionId.ToString(CoreConstants.GuidDefaultFormat, CultureInfo.InvariantCulture);
            }
            catch
            {
                return CoreConstants.UnknownAssemblyModuleVersionIdentifier;
            }
        }

        /// <summary>
        /// Resolves full assembly location path with safe fallback.
        /// </summary>
        private static string ResolveAssemblyLocation(Assembly assembly)
        {
            if (assembly == null)
            {
                return CoreConstants.UnknownAssemblyLocation;
            }

            try
            {
                string assemblyLocation = assembly.Location;
                if (string.IsNullOrEmpty(assemblyLocation))
                {
                    return CoreConstants.UnknownAssemblyLocation;
                }

                return Path.GetFullPath(assemblyLocation);
            }
            catch
            {
                return CoreConstants.UnknownAssemblyLocation;
            }
        }

        /// <summary>
        /// Computes SHA-256 hash for one assembly file location.
        /// </summary>
        private static string ComputeAssemblyContentHashHex(string assemblyLocation)
        {
            if (string.IsNullOrEmpty(assemblyLocation) || string.Equals(assemblyLocation, CoreConstants.UnknownAssemblyLocation, StringComparison.Ordinal))
            {
                return CoreConstants.UnknownAssemblyContentHash;
            }

            if (!File.Exists(assemblyLocation))
            {
                return CoreConstants.UnknownAssemblyContentHash;
            }

            try
            {
                using (FileStream stream = File.OpenRead(assemblyLocation))
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(stream);
                    if (hashBytes == null || hashBytes.Length < CoreConstants.MinimumNonEmptyCollectionCount)
                    {
                        return CoreConstants.UnknownAssemblyContentHash;
                    }

                    StringBuilder builder = new StringBuilder(hashBytes.Length * CoreConstants.HexCharactersPerByte);
                    for (int byteIndex = CoreConstants.ZeroBasedListStartIndex; byteIndex < hashBytes.Length; byteIndex++)
                    {
                        builder.Append(hashBytes[byteIndex].ToString(CoreConstants.ByteToLowerHexFormat, CultureInfo.InvariantCulture));
                    }

                    return builder.ToString();
                }
            }
            catch
            {
                return CoreConstants.UnknownAssemblyContentHash;
            }
        }

        /// <summary>
        /// Resolves a safe save key token used for storage partitioning.
        /// </summary>
        private static string NormalizeSaveKey(string rawSaveKey)
        {
            string sanitizedSaveKey = CoreTokenUtility.SanitizeToken(rawSaveKey, CoreConstants.SaveKeyMaximumLength);
            if (sanitizedSaveKey.Length < CoreConstants.SaveKeyMinimumLength)
            {
                return CoreConstants.DefaultSaveKey;
            }

            return sanitizedSaveKey;
        }

        /// <summary>
        /// Returns release date text for a single, falling back to current game date.
        /// </summary>
        private static string ResolveReleaseDate(singles._single releasedSingle, DateTime fallbackGameDate)
        {
            if (releasedSingle != null && releasedSingle.ReleaseData != null)
            {
                DateTime releaseDate = releasedSingle.ReleaseData.ReleaseDate;
                if (releaseDate != default(DateTime))
                {
                    return CoreDateTimeUtility.ToRoundTripString(releaseDate);
                }
            }

            return CoreDateTimeUtility.ToRoundTripString(fallbackGameDate);
        }

        /// <summary>
        /// Returns total sales using release snapshot data when available.
        /// </summary>
        private static long ResolveTotalSales(singles._single releasedSingle)
        {
            if (releasedSingle != null && releasedSingle.ReleaseData != null)
            {
                return releasedSingle.ReleaseData.Sales;
            }

            return releasedSingle != null ? releasedSingle.GetTotalSales() : CoreConstants.ZeroLongValue;
        }

        /// <summary>
        /// Returns quality metric from single release data.
        /// </summary>
        private static int ResolveQuality(singles._single releasedSingle)
        {
            if (releasedSingle != null && releasedSingle.ReleaseData != null)
            {
                return releasedSingle.ReleaseData.Quality;
            }

            return CoreConstants.ZeroBasedListStartIndex;
        }

        /// <summary>
        /// Returns fan satisfaction metric from single release data.
        /// </summary>
        private static int ResolveFanSatisfaction(singles._single releasedSingle)
        {
            if (releasedSingle != null && releasedSingle.ReleaseData != null)
            {
                return releasedSingle.ReleaseData.Fan_Satisfaction;
            }

            return CoreConstants.ZeroBasedListStartIndex;
        }

        /// <summary>
        /// Returns fan buzz metric from single release data.
        /// </summary>
        private static int ResolveFanBuzz(singles._single releasedSingle)
        {
            if (releasedSingle != null && releasedSingle.ReleaseData != null)
            {
                return releasedSingle.ReleaseData.Fan_Buzz;
            }

            return CoreConstants.ZeroBasedListStartIndex;
        }

        /// <summary>
        /// Returns total new fans metric from single release data.
        /// </summary>
        private static int ResolveNewFans(singles._single releasedSingle)
        {
            if (releasedSingle != null && releasedSingle.ReleaseData != null)
            {
                return releasedSingle.ReleaseData.NewFans;
            }

            return CoreConstants.ZeroBasedListStartIndex;
        }

        /// <summary>
        /// Returns new hardcore-fans metric from single release data.
        /// </summary>
        private static int ResolveNewHardcoreFans(singles._single releasedSingle)
        {
            if (releasedSingle != null && releasedSingle.ReleaseData != null)
            {
                return releasedSingle.ReleaseData.NewHardcoreFans;
            }

            return CoreConstants.ZeroBasedListStartIndex;
        }

        /// <summary>
        /// Returns new casual-fans metric from single release data.
        /// </summary>
        private static int ResolveNewCasualFans(singles._single releasedSingle)
        {
            if (releasedSingle != null && releasedSingle.ReleaseData != null)
            {
                return releasedSingle.ReleaseData.NewCasualFans;
            }

            return CoreConstants.ZeroBasedListStartIndex;
        }

        /// <summary>
        /// Returns production quantity for one released single.
        /// </summary>
        private static int ResolveSingleQuantity(singles._single releasedSingle)
        {
            if (releasedSingle == null)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            return releasedSingle.qty;
        }

        /// <summary>
        /// Returns production cost for one released single.
        /// </summary>
        private static long ResolveSingleProductionCost(singles._single releasedSingle)
        {
            if (releasedSingle == null)
            {
                return CoreConstants.ZeroLongValue;
            }

            return releasedSingle.productionCost;
        }

        /// <summary>
        /// Returns marketing roll value for one released single.
        /// </summary>
        private static float ResolveSingleMarketingResult(singles._single releasedSingle)
        {
            if (releasedSingle == null)
            {
                return 0f;
            }

            return releasedSingle.Marketing_Result;
        }

        /// <summary>
        /// Returns marketing roll status code for one released single.
        /// </summary>
        private static string ResolveSingleMarketingResultStatus(singles._single releasedSingle)
        {
            if (releasedSingle == null)
            {
                return string.Empty;
            }

            return releasedSingle.Marketing_Result_Status.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// Returns gross revenue for one released single before production-cost subtraction.
        /// </summary>
        private static long ResolveSingleGrossRevenue(singles._single releasedSingle)
        {
            if (releasedSingle == null)
            {
                return CoreConstants.ZeroLongValue;
            }

            return releasedSingle.GetMoney();
        }

        /// <summary>
        /// Returns one-CD production cost for one released single.
        /// </summary>
        private static int ResolveSingleOneCdCost(singles._single releasedSingle)
        {
            if (releasedSingle == null)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            return releasedSingle.GetOneCDCost();
        }

        /// <summary>
        /// Returns one-CD sales revenue for one released single.
        /// </summary>
        private static int ResolveSingleOneCdRevenue(singles._single releasedSingle)
        {
            if (releasedSingle == null)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            return releasedSingle.GetOneCDRevenue();
        }

        /// <summary>
        /// Returns additional expenses for one released single.
        /// </summary>
        private static long ResolveSingleOtherExpenses(singles._single releasedSingle)
        {
            if (releasedSingle == null)
            {
                return CoreConstants.ZeroLongValue;
            }

            return releasedSingle.GetOtherExpenses();
        }

        /// <summary>
        /// Returns whether one released single is group-handshake content.
        /// </summary>
        private static bool ResolveSingleIsGroupHandshake(singles._single releasedSingle)
        {
            return releasedSingle != null && releasedSingle.IsGroupHS();
        }

        /// <summary>
        /// Returns whether one released single is individual-handshake content.
        /// </summary>
        private static bool ResolveSingleIsIndividualHandshake(singles._single releasedSingle)
        {
            return releasedSingle != null && releasedSingle.IsIndividualHS();
        }

        /// <summary>
        /// Returns total fame points awarded by one released single.
        /// </summary>
        private static int ResolveSingleFamePointsAwarded(singles._single releasedSingle)
        {
            if (releasedSingle == null)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            return releasedSingle.famePoints;
        }

        /// <summary>
        /// Returns profit metric from single release data.
        /// </summary>
        private static long ResolveSingleProfit(singles._single releasedSingle)
        {
            if (releasedSingle != null && releasedSingle.ReleaseData != null)
            {
                return releasedSingle.ReleaseData.Profit;
            }

            return CoreConstants.ZeroLongValue;
        }

        /// <summary>
        /// Returns sales-per-fan metric from single release data.
        /// </summary>
        private static float ResolveSalesPerFan(singles._single releasedSingle)
        {
            if (releasedSingle != null && releasedSingle.ReleaseData != null)
            {
                return releasedSingle.ReleaseData.Sales_Per_Fan;
            }

            return 0f;
        }

        /// <summary>
        /// Returns fame-of-the-senbatsu metric from single release data.
        /// </summary>
        private static float ResolveFameOfSenbatsu(singles._single releasedSingle)
        {
            if (releasedSingle != null && releasedSingle.ReleaseData != null)
            {
                return releasedSingle.ReleaseData.Fame_Of_The_Senbatsu;
            }

            return 0f;
        }

        /// <summary>
        /// Builds one stable senbatsu stat snapshot string (parameter-code to value pairs) at release time.
        /// </summary>
        private static string BuildSingleSenbatsuStatsSnapshot(singles._single releasedSingle)
        {
            if (releasedSingle == null || releasedSingle.girls == null || releasedSingle.girls.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return string.Empty;
            }

            List<data_girls.girls.param> senbatsuStats;
            try
            {
                senbatsuStats = releasedSingle.GetSenbatsuStats(releasedSingle.girls, releasedSingle.GetGroup());
            }
            catch
            {
                return string.Empty;
            }

            if (senbatsuStats == null || senbatsuStats.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return string.Empty;
            }

            StringBuilder snapshotBuilder = new StringBuilder(senbatsuStats.Count * 20);
            bool hasAnyValue = false;
            for (int statIndex = CoreConstants.ZeroBasedListStartIndex; statIndex < senbatsuStats.Count; statIndex++)
            {
                data_girls.girls.param stat = senbatsuStats[statIndex];
                if (stat == null)
                {
                    continue;
                }

                string parameterCode = CoreEnumNameMapping.ToIdolParameterCode(stat.type);
                if (string.IsNullOrEmpty(parameterCode))
                {
                    continue;
                }

                float parameterValue = stat.val;
                if (float.IsNaN(parameterValue) || float.IsInfinity(parameterValue))
                {
                    continue;
                }

                if (hasAnyValue)
                {
                    snapshotBuilder.Append(CoreConstants.JsonPropertySeparatorCharacter);
                }

                snapshotBuilder.Append(parameterCode);
                snapshotBuilder.Append(CoreConstants.JsonNameValueSeparatorCharacter);
                snapshotBuilder.Append(parameterValue.ToString(CoreConstants.JsonFloatRoundTripFormat, CultureInfo.InvariantCulture));
                hasAnyValue = true;
            }

            return hasAnyValue ? snapshotBuilder.ToString() : string.Empty;
        }

        /// <summary>
        /// Returns whether the released single was most popular in genre.
        /// </summary>
        private static bool ResolveMostPopularGenre(singles._single releasedSingle)
        {
            return releasedSingle != null
                && releasedSingle.ReleaseData != null
                && releasedSingle.ReleaseData.MostPopular_Genre;
        }

        /// <summary>
        /// Returns whether the released single was most popular in lyrics.
        /// </summary>
        private static bool ResolveMostPopularLyrics(singles._single releasedSingle)
        {
            return releasedSingle != null
                && releasedSingle.ReleaseData != null
                && releasedSingle.ReleaseData.MostPopular_Lyrics;
        }

        /// <summary>
        /// Returns whether the released single was most popular in choreography.
        /// </summary>
        private static bool ResolveMostPopularChoreo(singles._single releasedSingle)
        {
            return releasedSingle != null
                && releasedSingle.ReleaseData != null
                && releasedSingle.ReleaseData.MostPopular_Choreo;
        }

        /// <summary>
        /// Returns fan-appeal ratio for one target fan type from single release data.
        /// </summary>
        private static float ResolveReleaseFanAppealRatio(singles._single releasedSingle, resources.fanType fanType)
        {
            if (releasedSingle == null || releasedSingle.ReleaseData == null || releasedSingle.ReleaseData.FanAppeal == null)
            {
                return 0f;
            }

            for (int appealIndex = CoreConstants.ZeroBasedListStartIndex; appealIndex < releasedSingle.ReleaseData.FanAppeal.Count; appealIndex++)
            {
                singles._fanAppeal fanAppeal = releasedSingle.ReleaseData.FanAppeal[appealIndex];
                if (fanAppeal != null && fanAppeal.type == fanType)
                {
                    return fanAppeal.ratio;
                }
            }

            return 0f;
        }

        /// <summary>
        /// Builds a compact per-segment sales summary for one released single.
        /// </summary>
        private static string BuildSingleFanSegmentSalesSummary(singles._single releasedSingle)
        {
            return BuildSingleFanSegmentSummary(releasedSingle, true);
        }

        /// <summary>
        /// Builds a compact per-segment new-fan summary for one released single.
        /// </summary>
        private static string BuildSingleFanSegmentNewFansSummary(singles._single releasedSingle)
        {
            return BuildSingleFanSegmentSummary(releasedSingle, false);
        }

        /// <summary>
        /// Builds one compact fan-segment summary string from release sales rows.
        /// </summary>
        private static string BuildSingleFanSegmentSummary(singles._single releasedSingle, bool includeSales)
        {
            if (releasedSingle == null)
            {
                return string.Empty;
            }

            resources.fanType[] genders = { resources.fanType.male, resources.fanType.female };
            resources.fanType[] hardcorenessValues = { resources.fanType.casual, resources.fanType.hardcore };
            resources.fanType[] ages = { resources.fanType.teen, resources.fanType.youngAdult, resources.fanType.adult };

            StringBuilder summaryBuilder = new StringBuilder();
            for (int genderIndex = CoreConstants.ZeroBasedListStartIndex; genderIndex < genders.Length; genderIndex++)
            {
                resources.fanType gender = genders[genderIndex];
                for (int hardcorenessIndex = CoreConstants.ZeroBasedListStartIndex; hardcorenessIndex < hardcorenessValues.Length; hardcorenessIndex++)
                {
                    resources.fanType hardcoreness = hardcorenessValues[hardcorenessIndex];
                    for (int ageIndex = CoreConstants.ZeroBasedListStartIndex; ageIndex < ages.Length; ageIndex++)
                    {
                        resources.fanType age = ages[ageIndex];
                        string fanSegmentKey = BuildSingleFanSegmentKey(gender, hardcoreness, age);
                        if (fanSegmentKey.Length < CoreConstants.SaveKeyMinimumLength)
                        {
                            continue;
                        }

                        if (summaryBuilder.Length > CoreConstants.ZeroBasedListStartIndex)
                        {
                            summaryBuilder.Append(CoreConstants.SingleFanSegmentEntrySeparator);
                        }

                        singles._single._sales sale = releasedSingle.GetFanSales(gender, hardcoreness, age);
                        long metricValue = CoreConstants.ZeroLongValue;
                        if (sale != null)
                        {
                            metricValue = includeSales ? sale.sales : sale.new_fans;
                        }

                        summaryBuilder.Append(fanSegmentKey);
                        summaryBuilder.Append(CoreConstants.SingleFanSegmentValueSeparator);
                        summaryBuilder.Append(metricValue.ToString(CultureInfo.InvariantCulture));
                    }
                }
            }

            return summaryBuilder.ToString();
        }

        /// <summary>
        /// Builds one normalized fan-segment key in `gender,hardcoreness,age` format.
        /// </summary>
        private static string BuildSingleFanSegmentKey(resources._fan fan)
        {
            if (fan == null)
            {
                return string.Empty;
            }

            return BuildSingleFanSegmentKey(fan.gender, fan.hardcoreness, fan.age);
        }

        /// <summary>
        /// Builds one normalized fan-segment key in `gender,hardcoreness,age` format.
        /// </summary>
        private static string BuildSingleFanSegmentKey(resources.fanType gender, resources.fanType hardcoreness, resources.fanType age)
        {
            return string.Concat(
                CoreEnumNameMapping.ToFanTypeCode(gender),
                CoreConstants.SingleFanSegmentDimensionSeparator,
                CoreEnumNameMapping.ToFanTypeCode(hardcoreness),
                CoreConstants.SingleFanSegmentDimensionSeparator,
                CoreEnumNameMapping.ToFanTypeCode(age));
        }

        /// <summary>
        /// Returns chart position metric from single release data.
        /// </summary>
        private static int ResolveChartPosition(singles._single releasedSingle)
        {
            if (releasedSingle != null && releasedSingle.ReleaseData != null && releasedSingle.ReleaseData.Chart_Position > CoreConstants.ZeroBasedListStartIndex)
            {
                return releasedSingle.ReleaseData.Chart_Position;
            }

            int derivedChartPosition = ResolveChartPositionFromRivalsChart(releasedSingle);
            if (derivedChartPosition > CoreConstants.ZeroBasedListStartIndex)
            {
                if (releasedSingle != null && releasedSingle.ReleaseData != null)
                {
                    releasedSingle.ReleaseData.Chart_Position = derivedChartPosition;
                }

                return derivedChartPosition;
            }

            return CoreConstants.ZeroBasedListStartIndex;
        }

        /// <summary>
        /// Resolves chart position using rival monthly chart projections when release data has no position.
        /// </summary>
        private static int ResolveChartPositionFromRivalsChart(singles._single releasedSingle)
        {
            if (releasedSingle == null || releasedSingle.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            if (Rivals.Date_To_Month == null || Rivals.Date_To_Month.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            int expectedChartMonthId = ResolveSingleChartMonthIdFromReleaseDate(releasedSingle);
            if (expectedChartMonthId >= CoreConstants.MinimumValidIdolIdentifier)
            {
                int expectedMonthChartPosition = ResolveChartPositionFromRivalsMonth(releasedSingle, expectedChartMonthId);
                if (expectedMonthChartPosition > CoreConstants.ZeroBasedListStartIndex)
                {
                    return expectedMonthChartPosition;
                }
            }

            for (int monthIndex = CoreConstants.ZeroBasedListStartIndex; monthIndex < Rivals.Date_To_Month.Count; monthIndex++)
            {
                Rivals._date_to_month_id monthData = Rivals.Date_To_Month[monthIndex];
                if (monthData == null)
                {
                    continue;
                }

                int chartPosition = ResolveChartPositionFromRivalsMonth(releasedSingle, monthData.ID);
                if (chartPosition > CoreConstants.ZeroBasedListStartIndex)
                {
                    return chartPosition;
                }
            }

            return CoreConstants.ZeroBasedListStartIndex;
        }

        /// <summary>
        /// Resolves expected chart month id from single release date (+1 month chart window).
        /// </summary>
        private static int ResolveSingleChartMonthIdFromReleaseDate(singles._single releasedSingle)
        {
            if (releasedSingle == null || releasedSingle.ReleaseData == null || Rivals.Date_To_Month == null)
            {
                return CoreConstants.InvalidIdValue;
            }

            DateTime releaseDate = releasedSingle.ReleaseData.ReleaseDate;
            if (releaseDate.Year <= DateTime.MinValue.Year)
            {
                return CoreConstants.InvalidIdValue;
            }

            DateTime expectedChartDate = releaseDate.AddMonths(CoreConstants.SingleChartMonthOffsetFromReleaseDate);
            for (int monthIndex = CoreConstants.ZeroBasedListStartIndex; monthIndex < Rivals.Date_To_Month.Count; monthIndex++)
            {
                Rivals._date_to_month_id monthData = Rivals.Date_To_Month[monthIndex];
                if (monthData == null)
                {
                    continue;
                }

                if (monthData.Date.Year == expectedChartDate.Year && monthData.Date.Month == expectedChartDate.Month)
                {
                    return monthData.ID;
                }
            }

            return CoreConstants.InvalidIdValue;
        }

        /// <summary>
        /// Resolves chart position for one single in one specific rivals chart month.
        /// </summary>
        private static int ResolveChartPositionFromRivalsMonth(singles._single releasedSingle, int monthId)
        {
            if (releasedSingle == null || monthId < CoreConstants.MinimumValidIdolIdentifier)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            List<Rivals._group._single> monthSingles;
            try
            {
                monthSingles = Rivals.GetSingles(monthId);
            }
            catch
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            return ResolveChartPositionFromRivalsSingles(releasedSingle, monthSingles);
        }

        /// <summary>
        /// Resolves one single chart position by scanning one rivals month row set.
        /// </summary>
        private static int ResolveChartPositionFromRivalsSingles(
            singles._single releasedSingle,
            List<Rivals._group._single> monthSingles)
        {
            if (releasedSingle == null || monthSingles == null || monthSingles.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            for (int chartIndex = CoreConstants.ZeroBasedListStartIndex; chartIndex < monthSingles.Count; chartIndex++)
            {
                Rivals._group._single chartRow = monthSingles[chartIndex];
                if (chartRow == null || !chartRow.Player)
                {
                    continue;
                }

                int chartSingleId = chartRow.SingleID;
                if (chartSingleId < CoreConstants.MinimumValidIdolIdentifier)
                {
                    singles._single chartSingle = chartRow.GetSingle();
                    if (chartSingle != null)
                    {
                        chartSingleId = chartSingle.id;
                    }
                }

                if (chartSingleId != releasedSingle.id)
                {
                    continue;
                }

                return chartIndex + CoreConstants.MinimumNonEmptyCollectionCount;
            }

            return CoreConstants.ZeroBasedListStartIndex;
        }

        /// <summary>
        /// Flushes queued capture records and logs a warning if persistence fails.
        /// </summary>
        private void FlushAfterCaptureLocked()
        {
            string errorMessage;
            if (!FlushLocked(false, out errorMessage))
            {
                CoreLog.Warn(CoreConstants.MessageFlushFailed + errorMessage);
            }
        }

        /// <summary>
        /// Enqueues one event-stream record with normalized idol identifier handling.
        /// </summary>
        private void EnqueueEventRecordLocked(
            DateTime gameDate,
            int idolId,
            string entityKind,
            string entityId,
            string eventType,
            string sourcePatch,
            string payloadJson)
        {
            PendingEvent pendingEvent = new PendingEvent
            {
                SaveKey = activeSaveKey,
                GameDateKey = CoreDateTimeUtility.BuildGameDateKey(gameDate),
                GameDateTime = CoreDateTimeUtility.ToRoundTripString(gameDate),
                IdolId = idolId >= CoreConstants.MinimumValidIdolIdentifier ? idolId : CoreConstants.InvalidIdValue,
                EntityKind = entityKind ?? string.Empty,
                EntityId = entityId ?? string.Empty,
                EventType = eventType ?? string.Empty,
                SourcePatch = sourcePatch ?? string.Empty,
                NamespaceIdentifier = string.Empty,
                PayloadJson = payloadJson ?? CoreConstants.EmptyJsonObject
            };

            bufferedEvents.Add(pendingEvent);
        }

        /// <summary>
        /// Reserves one date-scoped idempotency key and returns false when already emitted for the same game day.
        /// </summary>
        private bool TryReserveIdempotencyKeyLocked(DateTime gameDate, string idempotencyKey)
        {
            int gameDateKey = CoreDateTimeUtility.BuildGameDateKey(gameDate);
            if (gameDateKey != idempotencyDateKey)
            {
                idempotencyDateKey = gameDateKey;
                idempotencyKeysForCurrentDate.Clear();
            }

            if (string.IsNullOrEmpty(idempotencyKey))
            {
                return true;
            }

            return idempotencyKeysForCurrentDate.Add(idempotencyKey);
        }

        /// <summary>
        /// Resolves owner idol identifier for one `_dating_data` object by reference matching.
        /// </summary>
        private static int ResolveIdolIdentifierFromDatingData(data_girls.girls._dating_data datingData)
        {
            if (datingData == null || data_girls.girl == null)
            {
                return CoreConstants.InvalidIdValue;
            }

            for (int idolIndex = CoreConstants.ZeroBasedListStartIndex; idolIndex < data_girls.girl.Count; idolIndex++)
            {
                data_girls.girls idol = data_girls.girl[idolIndex];
                if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
                {
                    continue;
                }

                if (ReferenceEquals(idol.DatingData, datingData))
                {
                    return idol.id;
                }
            }

            return CoreConstants.InvalidIdValue;
        }

        /// <summary>
        /// Resolves raw scandal-point value directly from idol parameter storage.
        /// </summary>
        private static float ResolveIdolScandalPointsRawValue(data_girls.girls idol)
        {
            if (idol == null)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            data_girls.girls.param scandalParameter = idol.getParam(data_girls._paramType.scandalPoints);
            if (scandalParameter == null)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            return scandalParameter.val;
        }

        /// <summary>
        /// Creates one contract payload from authoritative `active_proposal` state.
        /// </summary>
        private static ContractLifecyclePayload BuildContractLifecyclePayloadFromActiveContract(
            business.active_proposal activeContract,
            int idolId,
            bool damagesApplied,
            long totalBrokenLiability,
            string contractBreakContext)
        {
            if (activeContract == null)
            {
                return new ContractLifecyclePayload
                {
                    IdolId = idolId
                };
            }

            return new ContractLifecyclePayload
            {
                IdolId = idolId,
                ContractType = CoreEnumNameMapping.ToBusinessContractTypeCode(activeContract.Type),
                ContractSkill = CoreEnumNameMapping.ToIdolParameterCode(activeContract.Skill),
                IsGroupContract = activeContract.isGroup,
                WeeklyPayment = activeContract.Payment_per_week,
                WeeklyBuzz = activeContract.Buzz_per_week,
                WeeklyFame = activeContract.Fame_per_week,
                WeeklyFans = activeContract.Fans_per_week,
                WeeklyStamina = activeContract.Stamina_per_week,
                ContractLiability = activeContract.Liability,
                AgentName = activeContract.Agent_Name ?? string.Empty,
                ProductName = activeContract.Product_Name ?? string.Empty,
                ContractStartDate = string.Empty,
                ContractEndDate = CoreDateTimeUtility.ToRoundTripString(activeContract.EndDate),
                ContractDurationMonths = CoreConstants.ZeroBasedListStartIndex,
                ContractIsImmediate = false,
                DamagesApplied = damagesApplied,
                TotalBrokenLiability = totalBrokenLiability,
                ContractBreakContext = contractBreakContext ?? string.Empty
            };
        }

        /// <summary>
        /// Creates one contract payload from accepted proposal state.
        /// </summary>
        private static ContractLifecyclePayload BuildContractLifecyclePayloadFromProposal(
            business._proposal proposal,
            int idolId,
            DateTime contractStartDate,
            DateTime contractEndDate)
        {
            if (proposal == null)
            {
                return new ContractLifecyclePayload
                {
                    IdolId = idolId
                };
            }

            bool isImmediateContract = proposal.duration <= CoreConstants.ZeroBasedListStartIndex;
            return new ContractLifecyclePayload
            {
                IdolId = idolId,
                ContractType = CoreEnumNameMapping.ToBusinessContractTypeCode(proposal.type),
                ContractSkill = CoreEnumNameMapping.ToIdolParameterCode(proposal.skill),
                IsGroupContract = proposal.isGroup,
                WeeklyPayment = proposal.payment,
                WeeklyBuzz = proposal.buzz,
                WeeklyFame = proposal.fame,
                WeeklyFans = proposal.newFans,
                WeeklyStamina = proposal.stamina,
                ContractLiability = proposal.liability,
                AgentName = proposal.agentName ?? string.Empty,
                ProductName = proposal.productName ?? string.Empty,
                ContractStartDate = CoreDateTimeUtility.ToRoundTripString(contractStartDate),
                ContractEndDate = CoreDateTimeUtility.ToRoundTripString(contractEndDate),
                ContractDurationMonths = proposal.duration,
                ContractIsImmediate = isImmediateContract,
                DamagesApplied = false,
                TotalBrokenLiability = CoreConstants.ZeroLongValue,
                ContractBreakContext = string.Empty
            };
        }

        /// <summary>
        /// Creates one weekly contract-accrual payload from active contract state.
        /// </summary>
        private static ContractWeeklyAccrualPayload BuildContractWeeklyAccrualPayload(
            business.active_proposal activeContract,
            int idolId,
            string weeklyActionCode,
            int weeklyTrainingPoints)
        {
            if (activeContract == null)
            {
                return new ContractWeeklyAccrualPayload
                {
                    IdolId = idolId,
                    ContractWeeklyAction = weeklyActionCode ?? string.Empty
                };
            }

            return new ContractWeeklyAccrualPayload
            {
                IdolId = idolId,
                ContractType = CoreEnumNameMapping.ToBusinessContractTypeCode(activeContract.Type),
                ContractSkill = CoreEnumNameMapping.ToIdolParameterCode(activeContract.Skill),
                IsGroupContract = activeContract.isGroup,
                WeeklyPayment = activeContract.Payment_per_week,
                WeeklyBuzz = activeContract.Buzz_per_week,
                WeeklyFame = activeContract.Fame_per_week,
                WeeklyFans = activeContract.Fans_per_week,
                WeeklyStamina = activeContract.Stamina_per_week,
                WeeklyTrainingPoints = weeklyTrainingPoints,
                ContractEndDate = CoreDateTimeUtility.ToRoundTripString(activeContract.EndDate),
                ContractWeeklyAction = weeklyActionCode ?? string.Empty
            };
        }

        /// <summary>
        /// Resolves idol targets for one contract event using active state with proposal fallback for group contracts.
        /// </summary>
        private static List<int> ResolveContractTargetIdolIdentifiers(business.active_proposal activeContract, business._proposal sourceProposal)
        {
            List<int> idolIdentifiers = new List<int>();
            HashSet<int> uniqueIdentifiers = new HashSet<int>();

            if (activeContract != null && activeContract.Girl != null && activeContract.Girl.id >= CoreConstants.MinimumValidIdolIdentifier)
            {
                uniqueIdentifiers.Add(activeContract.Girl.id);
                idolIdentifiers.Add(activeContract.Girl.id);
            }

            if (sourceProposal == null)
            {
                return idolIdentifiers;
            }

            if (sourceProposal.girl != null && sourceProposal.girl.id >= CoreConstants.MinimumValidIdolIdentifier && uniqueIdentifiers.Add(sourceProposal.girl.id))
            {
                idolIdentifiers.Add(sourceProposal.girl.id);
            }

            if (sourceProposal.Girls == null || sourceProposal.Girls.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return idolIdentifiers;
            }

            for (int idolIndex = CoreConstants.ZeroBasedListStartIndex; idolIndex < sourceProposal.Girls.Count; idolIndex++)
            {
                data_girls.girls idol = sourceProposal.Girls[idolIndex];
                if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
                {
                    continue;
                }

                if (uniqueIdentifiers.Add(idol.id))
                {
                    idolIdentifiers.Add(idol.id);
                }
            }

            return idolIdentifiers;
        }

        /// <summary>
        /// Resolves idol identifiers from one proposal object with duplicate suppression.
        /// </summary>
        private static List<int> ResolveProposalTargetIdolIdentifiers(business._proposal proposal)
        {
            List<int> idolIdentifiers = new List<int>();
            if (proposal == null)
            {
                return idolIdentifiers;
            }

            HashSet<int> uniqueIdentifiers = new HashSet<int>();
            if (proposal.girl != null && proposal.girl.id >= CoreConstants.MinimumValidIdolIdentifier && uniqueIdentifiers.Add(proposal.girl.id))
            {
                idolIdentifiers.Add(proposal.girl.id);
            }

            if (proposal.Girls == null || proposal.Girls.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return idolIdentifiers;
            }

            for (int idolIndex = CoreConstants.ZeroBasedListStartIndex; idolIndex < proposal.Girls.Count; idolIndex++)
            {
                data_girls.girls idol = proposal.Girls[idolIndex];
                if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
                {
                    continue;
                }

                if (uniqueIdentifiers.Add(idol.id))
                {
                    idolIdentifiers.Add(idol.id);
                }
            }

            return idolIdentifiers;
        }

        /// <summary>
        /// Builds a deterministic contract entity identifier string.
        /// </summary>
        private static string BuildContractEntityIdentifier(int idolId, string contractTypeCode, DateTime contractEndDate)
        {
            int contractEndDateKey = CoreDateTimeUtility.BuildGameDateKey(contractEndDate);
            return string.Concat(
                idolId.ToString(CultureInfo.InvariantCulture),
                CoreConstants.SaveKeyJoinSeparator,
                contractTypeCode ?? CoreConstants.StatusCodeUnknown,
                CoreConstants.SaveKeyJoinSeparator,
                contractEndDateKey.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Resolves unique idol identifiers from one single's current cast.
        /// </summary>
        private static List<int> ResolveDistinctSingleCastIdolIdentifiers(singles._single single)
        {
            List<int> castIdolIdentifiers = new List<int>();
            if (single == null || single.girls == null || single.girls.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return castIdolIdentifiers;
            }

            HashSet<int> uniqueIdolIdentifiers = new HashSet<int>();
            for (int castIndex = CoreConstants.ZeroBasedListStartIndex; castIndex < single.girls.Count; castIndex++)
            {
                data_girls.girls idol = single.girls[castIndex];
                if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
                {
                    continue;
                }

                if (uniqueIdolIdentifiers.Add(idol.id))
                {
                    castIdolIdentifiers.Add(idol.id);
                }
            }

            return castIdolIdentifiers;
        }

        /// <summary>
        /// Creates one single lifecycle payload from authoritative single state.
        /// </summary>
        private static SingleLifecyclePayload BuildSingleLifecyclePayload(
            singles._single single,
            IReadOnlyList<int> castIdolIdentifiers,
            string lifecycleActionCode)
        {
            if (single == null)
            {
                return new SingleLifecyclePayload();
            }

            SEvent_SSK._SSK linkedElection = single.GetParentSSK();
            IReadOnlyList<int> castIdentifiers = castIdolIdentifiers ?? new List<int>();

            return new SingleLifecyclePayload
            {
                SingleId = single.id,
                SingleTitle = single.title ?? string.Empty,
                SingleLifecycleAction = lifecycleActionCode ?? string.Empty,
                SingleStatus = CoreEnumNameMapping.ToSingleStatusCode(single.status),
                SingleCastCount = castIdentifiers.Count,
                SingleCastIdList = BuildDelimitedIdentifierList(castIdentifiers),
                SingleIsDigital = single.IsDigital(),
                SingleLinkedElectionId = linkedElection != null ? linkedElection.ID : CoreConstants.InvalidIdValue
            };
        }

        /// <summary>
        /// Creates one single cast-change payload from before/after cast state.
        /// </summary>
        private static SingleCastChangePayload BuildSingleCastChangePayload(
            singles._single single,
            SingleCastChangeSnapshot snapshot,
            IReadOnlyList<int> castIdolIdentifiersAfter,
            IReadOnlyList<int> addedCastIdolIdentifiers,
            IReadOnlyList<int> removedCastIdolIdentifiers,
            data_girls.girls removedIdol)
        {
            if (single == null)
            {
                return new SingleCastChangePayload();
            }

            SingleCastChangeSnapshot previousState = snapshot ?? new SingleCastChangeSnapshot();
            IReadOnlyList<int> castIdolIdentifiersBefore = previousState.SingleCastIdolIdentifiersBefore ?? new List<int>();

            return new SingleCastChangePayload
            {
                SingleId = single.id,
                SingleTitle = single.title ?? string.Empty,
                PreviousSingleStatus = CoreEnumNameMapping.ToSingleStatusCode(previousState.SingleStatusBefore),
                NewSingleStatus = CoreEnumNameMapping.ToSingleStatusCode(single.status),
                SingleCastCountBefore = castIdolIdentifiersBefore.Count,
                SingleCastCountAfter = castIdolIdentifiersAfter != null ? castIdolIdentifiersAfter.Count : CoreConstants.ZeroBasedListStartIndex,
                SingleCastIdListBefore = BuildDelimitedIdentifierList(castIdolIdentifiersBefore),
                SingleCastIdListAfter = BuildDelimitedIdentifierList(castIdolIdentifiersAfter),
                SingleCastIdListAdded = BuildDelimitedIdentifierList(addedCastIdolIdentifiers),
                SingleCastIdListRemoved = BuildDelimitedIdentifierList(removedCastIdolIdentifiers),
                SingleRemovedIdolId = removedIdol != null ? removedIdol.id : CoreConstants.InvalidIdValue
            };
        }

        /// <summary>
        /// Creates one single group-change payload from before/after group assignment state.
        /// </summary>
        private static SingleGroupChangePayload BuildSingleGroupChangePayload(
            singles._single single,
            SingleCastChangeSnapshot snapshot,
            IReadOnlyList<int> castIdolIdentifiersAfter,
            Groups._group groupAfter)
        {
            if (single == null)
            {
                return new SingleGroupChangePayload();
            }

            SingleCastChangeSnapshot previousState = snapshot ?? new SingleCastChangeSnapshot();
            IReadOnlyList<int> castIdentifiers = castIdolIdentifiersAfter ?? new List<int>();

            return new SingleGroupChangePayload
            {
                SingleId = single.id,
                SingleTitle = single.title ?? string.Empty,
                PreviousSingleStatus = CoreEnumNameMapping.ToSingleStatusCode(previousState.SingleStatusBefore),
                NewSingleStatus = CoreEnumNameMapping.ToSingleStatusCode(single.status),
                SingleCastCount = castIdentifiers.Count,
                SingleCastIdList = BuildDelimitedIdentifierList(castIdentifiers),
                FromGroupId = previousState.SingleGroupIdBefore,
                FromGroupTitle = previousState.SingleGroupTitleBefore ?? string.Empty,
                FromGroupStatus = string.IsNullOrEmpty(previousState.SingleGroupStatusBefore)
                    ? CoreConstants.StatusCodeUnknown
                    : previousState.SingleGroupStatusBefore,
                ToGroupId = groupAfter != null ? groupAfter.ID : CoreConstants.InvalidIdValue,
                ToGroupTitle = groupAfter != null ? groupAfter.Title ?? string.Empty : string.Empty,
                ToGroupStatus = groupAfter != null
                    ? CoreEnumNameMapping.ToGroupStatusCode(groupAfter.Status)
                    : CoreConstants.StatusCodeUnknown
            };
        }

        /// <summary>
        /// Resolves linked election identifier from one single or returns invalid sentinel.
        /// </summary>
        private static int ResolveElectionIdFromSingle(singles._single single)
        {
            if (single == null)
            {
                return CoreConstants.InvalidIdValue;
            }

            SEvent_SSK._SSK linkedElection = single.GetParentSSK();
            return linkedElection != null ? linkedElection.ID : CoreConstants.InvalidIdValue;
        }

        /// <summary>
        /// Resolves unique idol identifiers from one group's member list.
        /// </summary>
        private static List<int> ResolveDistinctGroupMemberIdolIdentifiers(Groups._group group)
        {
            List<int> memberIdolIdentifiers = new List<int>();
            if (group == null || group.Girls == null || group.Girls.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return memberIdolIdentifiers;
            }

            HashSet<int> uniqueIdolIdentifiers = new HashSet<int>();
            for (int memberIndex = CoreConstants.ZeroBasedListStartIndex; memberIndex < group.Girls.Count; memberIndex++)
            {
                data_girls.girls idol = group.Girls[memberIndex];
                if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
                {
                    continue;
                }

                if (uniqueIdolIdentifiers.Add(idol.id))
                {
                    memberIdolIdentifiers.Add(idol.id);
                }
            }

            return memberIdolIdentifiers;
        }

        /// <summary>
        /// Resolves distinct group identifiers from one group list.
        /// </summary>
        private static List<int> ResolveDistinctGroupIds(IReadOnlyList<Groups._group> groups)
        {
            List<int> groupIds = new List<int>();
            if (groups == null || groups.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return groupIds;
            }

            HashSet<int> uniqueGroupIds = new HashSet<int>();
            for (int groupIndex = CoreConstants.ZeroBasedListStartIndex; groupIndex < groups.Count; groupIndex++)
            {
                Groups._group group = groups[groupIndex];
                if (group == null || !uniqueGroupIds.Add(group.ID))
                {
                    continue;
                }

                groupIds.Add(group.ID);
            }

            return groupIds;
        }

        /// <summary>
        /// Resolves one newly created group by diffing current groups against a pre-create snapshot.
        /// </summary>
        private static Groups._group ResolveCreatedGroupFromSnapshot(GroupCreationSnapshot snapshot)
        {
            if (Groups.Groups_ == null || Groups.Groups_.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return null;
            }

            GroupCreationSnapshot previousState = snapshot ?? new GroupCreationSnapshot();
            HashSet<int> existingGroupIds = new HashSet<int>();
            if (previousState.ExistingGroupIds != null)
            {
                for (int groupIndex = CoreConstants.ZeroBasedListStartIndex; groupIndex < previousState.ExistingGroupIds.Count; groupIndex++)
                {
                    int existingGroupId = previousState.ExistingGroupIds[groupIndex];
                    if (existingGroupId >= CoreConstants.MinimumValidIdolIdentifier)
                    {
                        existingGroupIds.Add(existingGroupId);
                    }
                }
            }

            Groups._group createdGroup = null;
            for (int groupIndex = CoreConstants.ZeroBasedListStartIndex; groupIndex < Groups.Groups_.Count; groupIndex++)
            {
                Groups._group candidateGroup = Groups.Groups_[groupIndex];
                if (candidateGroup == null || existingGroupIds.Contains(candidateGroup.ID))
                {
                    continue;
                }

                if (createdGroup == null || candidateGroup.ID > createdGroup.ID)
                {
                    createdGroup = candidateGroup;
                }
            }

            if (createdGroup != null)
            {
                return createdGroup;
            }

            return Groups.Groups_[Groups.Groups_.Count - CoreConstants.LastElementOffsetFromCount];
        }

        /// <summary>
        /// Resolves current total single count for one group.
        /// </summary>
        private static int ResolveGroupSingleCount(Groups._group group)
        {
            if (group == null)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            List<singles._single> groupSingles = group.GetSingles();
            if (groupSingles == null)
            {
                groupSingles = group.Singles;
            }

            if (groupSingles == null || groupSingles.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            int singleCount = CoreConstants.ZeroBasedListStartIndex;
            for (int singleIndex = CoreConstants.ZeroBasedListStartIndex; singleIndex < groupSingles.Count; singleIndex++)
            {
                if (groupSingles[singleIndex] != null)
                {
                    singleCount++;
                }
            }

            return singleCount;
        }

        /// <summary>
        /// Resolves current non-released single count for one group.
        /// </summary>
        private static int ResolveGroupNonReleasedSingleCount(Groups._group group)
        {
            if (group == null)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            List<singles._single> groupSingles = group.GetSingles();
            if (groupSingles == null)
            {
                groupSingles = group.Singles;
            }

            if (groupSingles == null || groupSingles.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            int nonReleasedSingleCount = CoreConstants.ZeroBasedListStartIndex;
            for (int singleIndex = CoreConstants.ZeroBasedListStartIndex; singleIndex < groupSingles.Count; singleIndex++)
            {
                singles._single single = groupSingles[singleIndex];
                if (single != null && single.status != singles._single._status.released)
                {
                    nonReleasedSingleCount++;
                }
            }

            return nonReleasedSingleCount;
        }

        /// <summary>
        /// Converts a date to round-trip text with an empty fallback for default values.
        /// </summary>
        private static string ResolveDateTimeOrEmpty(DateTime value)
        {
            return value == default(DateTime)
                ? string.Empty
                : CoreDateTimeUtility.ToRoundTripString(value);
        }

        /// <summary>
        /// Creates one idol group-transfer payload from before/after membership state.
        /// </summary>
        private static IdolGroupTransferPayload BuildIdolGroupTransferPayload(
            data_girls.girls idol,
            Groups._group toGroup,
            GroupTransferSnapshot snapshot)
        {
            GroupTransferSnapshot previousState = snapshot ?? new GroupTransferSnapshot();
            DateTime transferDate = idol != null && idol.TransferDate != default(DateTime)
                ? idol.TransferDate
                : staticVars.dateTime;

            return new IdolGroupTransferPayload
            {
                IdolId = idol != null ? idol.id : CoreConstants.InvalidIdValue,
                IdolStatus = idol != null ? CoreStatusMapping.ToStatusCode(idol.status) : CoreConstants.StatusCodeUnknown,
                FromGroupId = previousState.FromGroupId,
                FromGroupTitle = previousState.FromGroupTitle ?? string.Empty,
                FromGroupStatus = string.IsNullOrEmpty(previousState.FromGroupStatus)
                    ? CoreConstants.StatusCodeUnknown
                    : previousState.FromGroupStatus,
                ToGroupId = toGroup != null ? toGroup.ID : CoreConstants.InvalidIdValue,
                ToGroupTitle = toGroup != null ? toGroup.Title ?? string.Empty : string.Empty,
                ToGroupStatus = toGroup != null
                    ? CoreEnumNameMapping.ToGroupStatusCode(toGroup.Status)
                    : CoreConstants.StatusCodeUnknown,
                TransferDate = CoreDateTimeUtility.ToRoundTripString(transferDate)
            };
        }

        /// <summary>
        /// Creates one group lifecycle payload from authoritative group state.
        /// </summary>
        private static GroupLifecyclePayload BuildGroupLifecyclePayload(
            Groups._group group,
            IReadOnlyList<int> memberIdolIdentifiers,
            string lifecycleActionCode,
            string eventDate,
            string dateCreated,
            int singleCount,
            int nonReleasedSingleCount)
        {
            if (group == null)
            {
                return new GroupLifecyclePayload
                {
                    GroupLifecycleAction = lifecycleActionCode ?? string.Empty,
                    GroupDateCreated = dateCreated ?? string.Empty,
                    GroupEventDate = eventDate ?? string.Empty,
                    GroupMemberCount = memberIdolIdentifiers != null ? memberIdolIdentifiers.Count : CoreConstants.ZeroBasedListStartIndex,
                    GroupMemberIdList = BuildDelimitedIdentifierList(memberIdolIdentifiers),
                    GroupSingleCount = Mathf.Max(singleCount, CoreConstants.ZeroBasedListStartIndex),
                    GroupNonReleasedSingleCount = Mathf.Max(nonReleasedSingleCount, CoreConstants.ZeroBasedListStartIndex)
                };
            }

            IReadOnlyList<int> memberIdentifiers = memberIdolIdentifiers ?? new List<int>();
            return new GroupLifecyclePayload
            {
                GroupId = group.ID,
                GroupTitle = group.Title ?? string.Empty,
                GroupStatus = CoreEnumNameMapping.ToGroupStatusCode(group.Status),
                GroupLifecycleAction = lifecycleActionCode ?? string.Empty,
                GroupDateCreated = string.IsNullOrEmpty(dateCreated)
                    ? ResolveDateTimeOrEmpty(group.Date_Created)
                    : dateCreated,
                GroupEventDate = eventDate ?? string.Empty,
                GroupMemberCount = memberIdentifiers.Count,
                GroupMemberIdList = BuildDelimitedIdentifierList(memberIdentifiers),
                GroupSingleCount = Mathf.Max(singleCount, CoreConstants.ZeroBasedListStartIndex),
                GroupNonReleasedSingleCount = Mathf.Max(nonReleasedSingleCount, CoreConstants.ZeroBasedListStartIndex),
                GroupAppealGender = CoreEnumNameMapping.ToFanTypeCode(group.Appeal_Gender),
                GroupAppealHardcoreness = CoreEnumNameMapping.ToFanTypeCode(group.Appeal_Hardcoreness),
                GroupAppealAge = CoreEnumNameMapping.ToFanTypeCode(group.Appeal_Age)
            };
        }

        /// <summary>
        /// Resolves current points for one group parameter track.
        /// </summary>
        private static int ResolveGroupParameterPoints(Groups._group group, data_girls._paramType parameterType)
        {
            if (group == null)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            return group.GetPoints(parameterType);
        }

        /// <summary>
        /// Resolves current available upgrade points for one group parameter track.
        /// </summary>
        private static int ResolveGroupAvailableParameterPoints(Groups._group group, data_girls._paramType parameterType)
        {
            if (group == null)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            return group.GetAvailablePoints(parameterType);
        }

        /// <summary>
        /// Resolves current spent-upgrade points for one group parameter track.
        /// </summary>
        private static int ResolveGroupParameterSpentPoints(Groups._group group, data_girls._paramType parameterType)
        {
            if (group == null || group.Points == null || group.Points.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            for (int pointsIndex = CoreConstants.ZeroBasedListStartIndex; pointsIndex < group.Points.Count; pointsIndex++)
            {
                Groups._group._points points = group.Points[pointsIndex];
                if (points != null && points.Type == parameterType)
                {
                    return points.Points_Spent;
                }
            }

            return CoreConstants.ZeroBasedListStartIndex;
        }

        /// <summary>
        /// Resolves current appeal points for one group target fan type.
        /// </summary>
        private static int ResolveGroupFanAppealPoints(Groups._group group, resources.fanType targetFanType)
        {
            if (group == null || group.Fans == null || group.Fans.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            for (int fanIndex = CoreConstants.ZeroBasedListStartIndex; fanIndex < group.Fans.Count; fanIndex++)
            {
                Groups._group._fan fan = group.Fans[fanIndex];
                if (fan != null && fan.Type == targetFanType)
                {
                    return fan.Points;
                }
            }

            return CoreConstants.ZeroBasedListStartIndex;
        }

        /// <summary>
        /// Creates one idol lifecycle payload from authoritative idol state.
        /// </summary>
        private static IdolLifecyclePayload BuildIdolLifecyclePayload(
            data_girls.girls idol,
            string lifecycleActionCode,
            string customTrivia,
            bool graduatedWithDialogue)
        {
            if (idol == null)
            {
                return new IdolLifecyclePayload();
            }

            string hiringDate = idol.Hiring_Date == default(DateTime)
                ? string.Empty
                : CoreDateTimeUtility.ToRoundTripString(idol.Hiring_Date);
            string graduationDate = idol.Graduation_Date == default(DateTime)
                ? string.Empty
                : CoreDateTimeUtility.ToRoundTripString(idol.Graduation_Date);

            return new IdolLifecyclePayload
            {
                IdolId = idol.id,
                IdolLifecycleAction = lifecycleActionCode ?? string.Empty,
                IdolStatus = CoreStatusMapping.ToStatusCode(idol.status),
                IdolType = CoreEnumNameMapping.ToIdolTypeCode(idol.Type),
                IdolAge = idol.GetAge(),
                IdolHiringDate = hiringDate,
                IdolGraduationDate = graduationDate,
                IdolTrivia = idol.GetTriviaString() ?? string.Empty,
                IdolCustomTrivia = customTrivia ?? string.Empty,
                IdolGraduationWithDialogue = graduatedWithDialogue
            };
        }

        /// <summary>
        /// Creates one idol salary-change payload from before/after state.
        /// </summary>
        private static IdolSalaryChangePayload BuildIdolSalaryChangePayload(
            data_girls.girls idol,
            SalaryChangeSnapshot snapshot,
            string salaryActionCode)
        {
            if (idol == null)
            {
                return new IdolSalaryChangePayload();
            }

            SalaryChangeSnapshot previousState = snapshot ?? new SalaryChangeSnapshot();
            long previousSalary = previousState.SalaryBefore;
            long newSalary = idol.salary;

            return new IdolSalaryChangePayload
            {
                IdolId = idol.id,
                SalaryAction = salaryActionCode ?? string.Empty,
                SalaryBefore = previousSalary,
                SalaryAfter = newSalary,
                SalaryDelta = newSalary - previousSalary,
                SalarySatisfactionBefore = previousState.SalarySatisfactionBefore,
                SalarySatisfactionAfter = idol.GetSalarySatisfaction_Percentage()
            };
        }

        /// <summary>
        /// Resolves unique idol identifiers from one show's current cast.
        /// </summary>
        private static List<int> ResolveDistinctShowCastIdolIdentifiers(Shows._show show)
        {
            List<int> castIdolIdentifiers = new List<int>();
            if (show == null)
            {
                return castIdolIdentifiers;
            }

            List<data_girls.girls> cast = show.GetCast();
            if (cast == null || cast.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return castIdolIdentifiers;
            }

            HashSet<int> uniqueIdolIdentifiers = new HashSet<int>();
            for (int castIndex = CoreConstants.ZeroBasedListStartIndex; castIndex < cast.Count; castIndex++)
            {
                data_girls.girls idol = cast[castIndex];
                if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
                {
                    continue;
                }

                if (uniqueIdolIdentifiers.Add(idol.id))
                {
                    castIdolIdentifiers.Add(idol.id);
                }
            }

            return castIdolIdentifiers;
        }

        /// <summary>
        /// Creates one show lifecycle payload from authoritative show state.
        /// </summary>
        private static ShowLifecyclePayload BuildShowLifecyclePayload(
            Shows._show show,
            IReadOnlyList<int> castIdolIdentifiers,
            string lifecycleActionCode)
        {
            if (show == null)
            {
                return new ShowLifecyclePayload();
            }

            IReadOnlyList<int> castIdentifiers = castIdolIdentifiers ?? new List<int>();
            int castCount = castIdentifiers.Count;

            return new ShowLifecyclePayload
            {
                ShowId = show.id,
                ShowTitle = show.title ?? string.Empty,
                ShowLifecycleAction = lifecycleActionCode ?? string.Empty,
                ShowStatus = CoreEnumNameMapping.ToShowStatusCode(show.status),
                ShowCastType = CoreEnumNameMapping.ToShowCastTypeCode(show.castType),
                ShowMediumCode = ResolveShowParameterCode(show.medium),
                ShowMediumTitle = ResolveShowParameterTitle(show.medium),
                ShowGenreCode = ResolveShowParameterCode(show.genre),
                ShowGenreTitle = ResolveShowParameterTitle(show.genre),
                ShowEpisodeCount = show.episodeCount,
                ShowCastCount = castCount,
                ShowCastIdList = BuildDelimitedIdentifierList(castIdentifiers),
                ShowLatestAudience = ResolveLatestLongMetric(show.audience),
                ShowLatestRevenue = ResolveLatestLongMetric(show.revenue),
                ShowLatestNewFans = ResolveLatestIntMetric(show.fans),
                ShowLatestBuzz = ResolveLatestIntMetric(show.buzz),
                ShowRelaunchCount = show.NumberOfRelaunches,
                ShowWasRelaunched = show.WasRelaunched,
                ShowLaunchDate = show.LaunchDate == default(DateTime)
                    ? string.Empty
                    : CoreDateTimeUtility.ToRoundTripString(show.LaunchDate)
            };
        }

        /// <summary>
        /// Creates one show cast-change payload from before/after cast state.
        /// </summary>
        private static ShowCastChangePayload BuildShowCastChangePayload(
            Shows._show show,
            ShowCastChangeSnapshot snapshot,
            IReadOnlyList<int> castIdolIdentifiersAfter,
            IReadOnlyList<int> addedCastIdolIdentifiers,
            IReadOnlyList<int> removedCastIdolIdentifiers,
            data_girls.girls removedIdol)
        {
            if (show == null)
            {
                return new ShowCastChangePayload();
            }

            ShowCastChangeSnapshot previousState = snapshot ?? new ShowCastChangeSnapshot();
            IReadOnlyList<int> castIdolIdentifiersBefore = previousState.ShowCastIdolIdentifiersBefore ?? new List<int>();

            return new ShowCastChangePayload
            {
                ShowId = show.id,
                ShowTitle = show.title ?? string.Empty,
                PreviousShowStatus = CoreEnumNameMapping.ToShowStatusCode(previousState.ShowStatusBefore),
                NewShowStatus = CoreEnumNameMapping.ToShowStatusCode(show.status),
                ShowCastTypeBefore = CoreEnumNameMapping.ToShowCastTypeCode(previousState.ShowCastTypeBefore),
                ShowCastTypeAfter = CoreEnumNameMapping.ToShowCastTypeCode(show.castType),
                ShowCastCountBefore = castIdolIdentifiersBefore.Count,
                ShowCastCountAfter = castIdolIdentifiersAfter != null ? castIdolIdentifiersAfter.Count : CoreConstants.ZeroBasedListStartIndex,
                ShowCastIdListBefore = BuildDelimitedIdentifierList(castIdolIdentifiersBefore),
                ShowCastIdListAfter = BuildDelimitedIdentifierList(castIdolIdentifiersAfter),
                ShowCastIdListAdded = BuildDelimitedIdentifierList(addedCastIdolIdentifiers),
                ShowCastIdListRemoved = BuildDelimitedIdentifierList(removedCastIdolIdentifiers),
                ShowRemovedIdolId = removedIdol != null ? removedIdol.id : CoreConstants.InvalidIdValue
            };
        }

        /// <summary>
        /// Creates one show configuration-change payload from before/after popup commit state.
        /// </summary>
        private static ShowConfigurationChangePayload BuildShowConfigurationChangePayload(
            Shows._show show,
            ShowCastChangeSnapshot snapshot,
            IReadOnlyList<int> castIdolIdentifiersAfter,
            string titleAfter,
            string mcCodeAfter,
            string mcTitleAfter,
            int productionCostAfter,
            string fanAppealSummaryAfter)
        {
            if (show == null)
            {
                return new ShowConfigurationChangePayload();
            }

            ShowCastChangeSnapshot previousState = snapshot ?? new ShowCastChangeSnapshot();
            IReadOnlyList<int> castIdolIdentifiersBefore = previousState.ShowCastIdolIdentifiersBefore ?? new List<int>();
            IReadOnlyList<int> castIdentifiersAfter = castIdolIdentifiersAfter ?? new List<int>();

            return new ShowConfigurationChangePayload
            {
                ShowId = show.id,
                ShowTitleBefore = previousState.ShowTitleBefore ?? string.Empty,
                ShowTitleAfter = titleAfter ?? string.Empty,
                PreviousShowStatus = CoreEnumNameMapping.ToShowStatusCode(previousState.ShowStatusBefore),
                NewShowStatus = CoreEnumNameMapping.ToShowStatusCode(show.status),
                ShowCastTypeBefore = CoreEnumNameMapping.ToShowCastTypeCode(previousState.ShowCastTypeBefore),
                ShowCastTypeAfter = CoreEnumNameMapping.ToShowCastTypeCode(show.castType),
                ShowCastCountBefore = castIdolIdentifiersBefore.Count,
                ShowCastCountAfter = castIdentifiersAfter.Count,
                ShowCastIdListBefore = BuildDelimitedIdentifierList(castIdolIdentifiersBefore),
                ShowCastIdListAfter = BuildDelimitedIdentifierList(castIdentifiersAfter),
                ShowMcCodeBefore = previousState.ShowMcCodeBefore ?? string.Empty,
                ShowMcCodeAfter = mcCodeAfter ?? string.Empty,
                ShowMcTitleBefore = previousState.ShowMcTitleBefore ?? string.Empty,
                ShowMcTitleAfter = mcTitleAfter ?? string.Empty,
                ShowProductionCostBefore = previousState.ShowProductionCostBefore,
                ShowProductionCostAfter = productionCostAfter,
                ShowFanAppealSummaryBefore = previousState.ShowFanAppealSummaryBefore ?? string.Empty,
                ShowFanAppealSummaryAfter = fanAppealSummaryAfter ?? string.Empty
            };
        }

        /// <summary>
        /// Resolves one stable show-parameter code from id when available.
        /// </summary>
        private static string ResolveShowParameterCode(Shows._param showParameter)
        {
            if (showParameter == null)
            {
                return CoreConstants.StatusCodeUnknown;
            }

            if (showParameter.id >= CoreConstants.MinimumValidIdolIdentifier)
            {
                return showParameter.id.ToString(CultureInfo.InvariantCulture);
            }

            return CoreConstants.StatusCodeUnknown;
        }

        /// <summary>
        /// Resolves one user-facing show-parameter title using game localization helpers.
        /// </summary>
        private static string ResolveShowParameterTitle(Shows._param showParameter)
        {
            if (showParameter == null)
            {
                return string.Empty;
            }

            string localizedTitle = showParameter.GetTitle();
            if (!string.IsNullOrEmpty(localizedTitle))
            {
                return localizedTitle;
            }

            return showParameter.title ?? string.Empty;
        }

        /// <summary>
        /// Builds a deterministic fan-appeal summary string from fan type ratios.
        /// </summary>
        private static string BuildFanAppealSummary(IList<singles._fanAppeal> fanAppeal)
        {
            if (fanAppeal == null || fanAppeal.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return string.Empty;
            }

            List<string> entries = new List<string>();
            for (int appealIndex = CoreConstants.ZeroBasedListStartIndex; appealIndex < fanAppeal.Count; appealIndex++)
            {
                singles._fanAppeal appeal = fanAppeal[appealIndex];
                if (appeal == null)
                {
                    continue;
                }

                string fanTypeCode = CoreEnumNameMapping.ToFanTypeCode(appeal.type);
                string ratioText = appeal.ratio.ToString(CoreConstants.JsonFloatRoundTripFormat, CultureInfo.InvariantCulture);
                entries.Add(string.Concat(
                    fanTypeCode,
                    CoreConstants.SingleFanSegmentValueSeparator,
                    ratioText));
            }

            if (entries.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return string.Empty;
            }

            entries.Sort(StringComparer.Ordinal);
            return string.Join(CoreConstants.SingleFanSegmentEntrySeparator, entries.ToArray());
        }

        /// <summary>
        /// Resolves distinct active idol identifiers for world-tour participant capture.
        /// </summary>
        private static List<int> ResolveDistinctActiveIdolIdentifiers()
        {
            List<int> participantIdolIdentifiers = new List<int>();
            List<data_girls.girls> activeIdols = data_girls.GetActiveGirls(null);
            if (activeIdols == null || activeIdols.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return participantIdolIdentifiers;
            }

            HashSet<int> uniqueIdolIdentifiers = new HashSet<int>();
            for (int idolIndex = CoreConstants.ZeroBasedListStartIndex; idolIndex < activeIdols.Count; idolIndex++)
            {
                data_girls.girls idol = activeIdols[idolIndex];
                if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
                {
                    continue;
                }

                if (uniqueIdolIdentifiers.Add(idol.id))
                {
                    participantIdolIdentifiers.Add(idol.id);
                }
            }

            return participantIdolIdentifiers;
        }

        /// <summary>
        /// Creates one world-tour lifecycle payload from authoritative tour state.
        /// </summary>
        private static TourLifecyclePayload BuildTourLifecyclePayload(
            SEvent_Tour.tour tour,
            IReadOnlyList<int> participantIdolIdentifiers,
            string tourStartDate,
            string lifecycleActionCode)
        {
            if (tour == null)
            {
                return new TourLifecyclePayload();
            }

            long totalAudience = CoreConstants.ZeroLongValue;
            long totalRevenue = CoreConstants.ZeroLongValue;
            long totalNewFans = CoreConstants.ZeroLongValue;
            if (tour.SelectedCountries != null)
            {
                for (int countryIndex = CoreConstants.ZeroBasedListStartIndex; countryIndex < tour.SelectedCountries.Count; countryIndex++)
                {
                    SEvent_Tour.tour.selectedCountry selectedCountry = tour.SelectedCountries[countryIndex];
                    if (selectedCountry == null)
                    {
                        continue;
                    }

                    totalAudience += selectedCountry.Audience;
                    totalRevenue += selectedCountry.Revenue;
                    totalNewFans += selectedCountry.NewFans;
                }
            }

            IReadOnlyList<int> participants = participantIdolIdentifiers ?? new List<int>();
            return new TourLifecyclePayload
            {
                TourId = tour.ID,
                TourLifecycleAction = lifecycleActionCode ?? string.Empty,
                TourStatus = CoreEnumNameMapping.ToTourStatusCode(tour.Status),
                SelectedCountryCount = tour.SelectedCountries != null ? tour.SelectedCountries.Count : CoreConstants.ZeroBasedListStartIndex,
                TourSelectedCountryLevelList = BuildTourSelectedCountryLevelList(tour),
                TourParticipantCount = participants.Count,
                TourParticipantIdList = BuildDelimitedIdentifierList(participants),
                TourTotalAudience = totalAudience,
                TourTotalRevenue = totalRevenue,
                TourTotalNewFans = totalNewFans,
                TourProductionCost = tour.ProductionCost,
                TourExpectedRevenue = tour.ExpectedRevenue,
                TourSaving = tour.Saving,
                TourStaminaCost = tour.Stamina,
                TourProfit = tour.GetProfit(),
                TourStartDate = tourStartDate ?? string.Empty,
                TourFinishDate = tour.FinishDate == default(DateTime)
                    ? string.Empty
                    : CoreDateTimeUtility.ToRoundTripString(tour.FinishDate)
            };
        }

        /// <summary>
        /// Builds one compact country-level list for selected world-tour stops.
        /// </summary>
        private static string BuildTourSelectedCountryLevelList(SEvent_Tour.tour tour)
        {
            if (tour == null || tour.SelectedCountries == null || tour.SelectedCountries.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return string.Empty;
            }

            StringBuilder countryLevelBuilder = new StringBuilder();
            for (int countryIndex = CoreConstants.ZeroBasedListStartIndex; countryIndex < tour.SelectedCountries.Count; countryIndex++)
            {
                SEvent_Tour.tour.selectedCountry selectedCountry = tour.SelectedCountries[countryIndex];
                if (selectedCountry == null || selectedCountry.Country == null)
                {
                    continue;
                }

                if (countryLevelBuilder.Length > CoreConstants.ZeroBasedListStartIndex)
                {
                    countryLevelBuilder.Append(CoreConstants.TourCountryLevelPairSeparator);
                }

                countryLevelBuilder.Append(CoreEnumNameMapping.ToTourCountryCode(selectedCountry.Country.Type));
                countryLevelBuilder.Append(CoreConstants.TourCountryLevelValueSeparator);
                countryLevelBuilder.Append(selectedCountry.Level.ToString(CultureInfo.InvariantCulture));
            }

            return countryLevelBuilder.ToString();
        }

        /// <summary>
        /// Creates one world-tour country-result payload from selected-country state.
        /// </summary>
        private static TourCountryResultPayload BuildTourCountryResultPayload(SEvent_Tour.tour tour, SEvent_Tour.tour.selectedCountry selectedCountry)
        {
            if (tour == null || selectedCountry == null || selectedCountry.Country == null)
            {
                return new TourCountryResultPayload();
            }

            return new TourCountryResultPayload
            {
                TourId = tour.ID,
                TourStatus = CoreEnumNameMapping.ToTourStatusCode(tour.Status),
                TourFinishDate = tour.FinishDate == default(DateTime)
                    ? string.Empty
                    : CoreDateTimeUtility.ToRoundTripString(tour.FinishDate),
                TourCountryCode = CoreEnumNameMapping.ToTourCountryCode(selectedCountry.Country.Type),
                TourCountryLevel = selectedCountry.Level,
                TourCountryAttendance = selectedCountry.Attendance,
                TourCountryAudience = selectedCountry.Audience,
                TourCountryNewFans = selectedCountry.NewFans,
                TourCountryRevenue = selectedCountry.Revenue,
                TourCountryDiscount = selectedCountry.Discount
            };
        }

        /// <summary>
        /// Creates one world-tour participation payload for an idol participant.
        /// </summary>
        private static TourParticipationPayload BuildTourParticipationPayload(
            int tourId,
            int idolId,
            IReadOnlyList<int> participantIdolIdentifiers,
            string lifecycleActionCode)
        {
            IReadOnlyList<int> participants = participantIdolIdentifiers ?? new List<int>();
            return new TourParticipationPayload
            {
                TourId = tourId,
                IdolId = idolId,
                TourParticipantCount = participants.Count,
                TourParticipantIdList = BuildDelimitedIdentifierList(participants),
                TourLifecycleAction = lifecycleActionCode ?? string.Empty
            };
        }

        /// <summary>
        /// Builds a deterministic world-tour country entity identifier.
        /// </summary>
        private static string BuildTourCountryEntityIdentifier(int tourId, string countryCode)
        {
            return string.Concat(
                tourId.ToString(CultureInfo.InvariantCulture),
                CoreConstants.SaveKeyJoinSeparator,
                countryCode ?? CoreConstants.StatusCodeUnknown);
        }

        /// <summary>
        /// Creates one election lifecycle payload from authoritative election state.
        /// </summary>
        private static ElectionLifecyclePayload BuildElectionLifecyclePayload(SEvent_SSK._SSK election, string lifecycleActionCode)
        {
            if (election == null)
            {
                return new ElectionLifecyclePayload();
            }

            return new ElectionLifecyclePayload
            {
                ElectionId = election.ID,
                ElectionLifecycleAction = lifecycleActionCode ?? string.Empty,
                ElectionStatus = CoreEnumNameMapping.ToTourStatusCode(election.Status),
                ElectionBroadcastType = CoreEnumNameMapping.ToElectionBroadcastCode(election.Broadcast),
                ElectionSingleId = ResolveSingleIdOrInvalid(election.Single),
                ElectionConcertId = ResolveConcertIdOrInvalid(election.Concert),
                ElectionReleaseSingleId = ResolveSingleIdOrInvalid(election.ReleaseSingle),
                ElectionResultCount = election.Results != null ? election.Results.Count : CoreConstants.ZeroBasedListStartIndex,
                ElectionFinishDate = election.FinishDate == default(DateTime)
                    ? string.Empty
                    : CoreDateTimeUtility.ToRoundTripString(election.FinishDate)
            };
        }

        /// <summary>
        /// Resolves one idol place from current election results or returns invalid sentinel.
        /// </summary>
        private static int ResolveElectionPlaceForIdol(SEvent_SSK._SSK election, int idolId)
        {
            if (election == null
                || election.Results == null
                || election.Results.Count < CoreConstants.MinimumNonEmptyCollectionCount
                || idolId < CoreConstants.MinimumValidIdolIdentifier)
            {
                return CoreConstants.InvalidIdValue;
            }

            for (int resultIndex = CoreConstants.ZeroBasedListStartIndex; resultIndex < election.Results.Count; resultIndex++)
            {
                SEvent_SSK._SSK._result result = election.Results[resultIndex];
                if (result == null || result.Girl == null || result.Girl.id != idolId)
                {
                    continue;
                }

                return result.Place;
            }

            return CoreConstants.InvalidIdValue;
        }

        /// <summary>
        /// Resolves unique idol identifiers from one concert setlist cast.
        /// </summary>
        private static List<int> ResolveDistinctConcertParticipantIdolIdentifiers(SEvent_Concerts._concert concert)
        {
            List<int> participantIdolIdentifiers = new List<int>();
            if (concert == null)
            {
                return participantIdolIdentifiers;
            }

            List<data_girls.girls> participants = concert.GetGirls(true);
            if (participants == null || participants.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return participantIdolIdentifiers;
            }

            HashSet<int> uniqueParticipantIdolIdentifiers = new HashSet<int>();
            for (int participantIndex = CoreConstants.ZeroBasedListStartIndex; participantIndex < participants.Count; participantIndex++)
            {
                data_girls.girls participant = participants[participantIndex];
                if (participant == null || participant.id < CoreConstants.MinimumValidIdolIdentifier)
                {
                    continue;
                }

                if (uniqueParticipantIdolIdentifiers.Add(participant.id))
                {
                    participantIdolIdentifiers.Add(participant.id);
                }
            }

            return participantIdolIdentifiers;
        }

        /// <summary>
        /// Creates one concert payload from authoritative concert state.
        /// </summary>
        private static ConcertLifecyclePayload BuildConcertLifecyclePayload(
            SEvent_Concerts._concert concert,
            int idolId,
            IReadOnlyList<int> participantIdolIdentifiers)
        {
            if (concert == null)
            {
                return new ConcertLifecyclePayload
                {
                    IdolId = idolId
                };
            }

            SEvent_Concerts._concert._projectedValues projectedValues = concert.ProjectedValues;
            long projectedAudience = projectedValues != null ? projectedValues.GetNumberOfSoldTickets() : CoreConstants.ZeroLongValue;
            long actualAudience = projectedValues != null ? projectedValues.Actual_Audience : CoreConstants.ZeroLongValue;
            long projectedRevenue = projectedValues != null ? projectedValues.GetRevenue() : CoreConstants.ZeroLongValue;
            long actualRevenue = projectedValues != null ? projectedValues.Actual_Revenue : CoreConstants.ZeroLongValue;
            long productionCost = projectedValues != null ? projectedValues.GetProductionCost() : CoreConstants.ZeroLongValue;
            long profit = actualRevenue - productionCost;
            string concertFinishDate = concert.FinishDate == default(DateTime)
                ? string.Empty
                : CoreDateTimeUtility.ToRoundTripString(concert.FinishDate);
            IReadOnlyList<int> participants = participantIdolIdentifiers ?? new List<int>();

            return new ConcertLifecyclePayload
            {
                IdolId = idolId,
                ConcertId = concert.ID,
                ConcertTitle = concert.GetTitle() ?? string.Empty,
                ConcertVenue = CoreEnumNameMapping.ToConcertVenueCode(concert.Venue),
                ConcertStatus = CoreEnumNameMapping.ToTourStatusCode(concert.Status),
                ConcertSongCount = concert.GetNumberOfSongs(),
                ConcertParticipantCount = participants.Count,
                ConcertParticipantIdList = BuildDelimitedIdentifierList(participants),
                ConcertSetlistSummary = BuildConcertSetlistSummary(concert),
                ConcertProjectedAudience = projectedAudience,
                ConcertActualAudience = actualAudience,
                ConcertProjectedRevenue = projectedRevenue,
                ConcertActualRevenue = actualRevenue,
                ConcertProductionCost = productionCost,
                ConcertProfit = profit,
                ConcertHype = Mathf.RoundToInt(concert.Hype),
                ConcertTicketPrice = projectedValues != null ? projectedValues.TicketPrice : CoreConstants.ZeroBasedListStartIndex,
                ConcertFinishDate = concertFinishDate
            };
        }

        /// <summary>
        /// Creates one concert status payload from authoritative concert state.
        /// </summary>
        private static ConcertStatusPayload BuildConcertStatusPayload(
            SEvent_Concerts._concert concert,
            SEvent_Tour.tour._status previousStatus,
            SEvent_Tour.tour._status newStatus,
            IReadOnlyList<int> participantIdolIdentifiers)
        {
            if (concert == null)
            {
                return new ConcertStatusPayload();
            }

            SEvent_Concerts._concert._projectedValues projectedValues = concert.ProjectedValues;
            long projectedAudience = projectedValues != null ? projectedValues.GetNumberOfSoldTickets() : CoreConstants.ZeroLongValue;
            long projectedRevenue = projectedValues != null ? projectedValues.GetRevenue() : CoreConstants.ZeroLongValue;
            IReadOnlyList<int> participants = participantIdolIdentifiers ?? new List<int>();

            return new ConcertStatusPayload
            {
                ConcertId = concert.ID,
                ConcertTitle = concert.GetTitle() ?? string.Empty,
                PreviousConcertStatus = CoreEnumNameMapping.ToTourStatusCode(previousStatus),
                NewConcertStatus = CoreEnumNameMapping.ToTourStatusCode(newStatus),
                ConcertSongCount = concert.GetNumberOfSongs(),
                ConcertParticipantCount = participants.Count,
                ConcertParticipantIdList = BuildDelimitedIdentifierList(participants),
                ConcertSetlistSummary = BuildConcertSetlistSummary(concert),
                ConcertProjectedAudience = projectedAudience,
                ConcertProjectedRevenue = projectedRevenue,
                ConcertTicketPrice = projectedValues != null ? projectedValues.TicketPrice : CoreConstants.ZeroBasedListStartIndex
            };
        }

        /// <summary>
        /// Creates one concert configuration-change payload from before/after popup commit state.
        /// </summary>
        private static ConcertConfigurationChangePayload BuildConcertConfigurationChangePayload(
            SEvent_Concerts._concert concert,
            ConcertCastChangeSnapshot snapshot,
            IReadOnlyList<int> participantIdolIdentifiersAfter,
            int songCountAfter,
            string setlistSummaryAfter,
            string titleAfter,
            string rawTitleAfter,
            string venueAfterCode,
            int ticketPriceAfter)
        {
            if (concert == null)
            {
                return new ConcertConfigurationChangePayload();
            }

            ConcertCastChangeSnapshot previousState = snapshot ?? new ConcertCastChangeSnapshot();
            IReadOnlyList<int> participantIdolIdentifiersBefore = previousState.ConcertParticipantIdolIdentifiersBefore ?? new List<int>();

            return new ConcertConfigurationChangePayload
            {
                ConcertId = concert.ID,
                ConcertTitleBefore = previousState.ConcertTitleBefore ?? string.Empty,
                ConcertTitleAfter = titleAfter ?? string.Empty,
                ConcertRawTitleBefore = previousState.ConcertRawTitleBefore ?? string.Empty,
                ConcertRawTitleAfter = rawTitleAfter ?? string.Empty,
                ConcertVenueBefore = CoreEnumNameMapping.ToConcertVenueCode(previousState.ConcertVenueBefore),
                ConcertVenueAfter = venueAfterCode ?? string.Empty,
                ConcertTicketPriceBefore = previousState.ConcertTicketPriceBefore,
                ConcertTicketPriceAfter = ticketPriceAfter,
                ConcertSongCountBefore = previousState.ConcertSongCountBefore,
                ConcertSongCountAfter = songCountAfter,
                ConcertParticipantCountBefore = participantIdolIdentifiersBefore.Count,
                ConcertParticipantCountAfter = participantIdolIdentifiersAfter != null
                    ? participantIdolIdentifiersAfter.Count
                    : CoreConstants.ZeroBasedListStartIndex,
                ConcertParticipantIdListBefore = BuildDelimitedIdentifierList(participantIdolIdentifiersBefore),
                ConcertParticipantIdListAfter = BuildDelimitedIdentifierList(participantIdolIdentifiersAfter),
                ConcertSetlistSummaryBefore = previousState.ConcertSetlistSummaryBefore ?? string.Empty,
                ConcertSetlistSummaryAfter = setlistSummaryAfter ?? string.Empty
            };
        }

        /// <summary>
        /// Creates one concert cast-change payload from before/after setlist state.
        /// </summary>
        private static ConcertCastChangePayload BuildConcertCastChangePayload(
            SEvent_Concerts._concert concert,
            ConcertCastChangeSnapshot snapshot,
            IReadOnlyList<int> participantIdolIdentifiersAfter,
            IReadOnlyList<int> addedParticipantIdolIdentifiers,
            IReadOnlyList<int> removedParticipantIdolIdentifiers,
            int songCountAfter,
            string setlistSummaryAfter,
            data_girls.girls removedIdol)
        {
            if (concert == null)
            {
                return new ConcertCastChangePayload();
            }

            ConcertCastChangeSnapshot previousState = snapshot ?? new ConcertCastChangeSnapshot();
            IReadOnlyList<int> participantIdolIdentifiersBefore = previousState.ConcertParticipantIdolIdentifiersBefore ?? new List<int>();

            return new ConcertCastChangePayload
            {
                ConcertId = concert.ID,
                ConcertTitle = concert.GetTitle() ?? string.Empty,
                PreviousConcertStatus = CoreEnumNameMapping.ToTourStatusCode(previousState.ConcertStatusBefore),
                NewConcertStatus = CoreEnumNameMapping.ToTourStatusCode(concert.Status),
                ConcertSongCountBefore = previousState.ConcertSongCountBefore,
                ConcertSongCountAfter = songCountAfter,
                ConcertParticipantCountBefore = participantIdolIdentifiersBefore.Count,
                ConcertParticipantCountAfter = participantIdolIdentifiersAfter != null
                    ? participantIdolIdentifiersAfter.Count
                    : CoreConstants.ZeroBasedListStartIndex,
                ConcertParticipantIdListBefore = BuildDelimitedIdentifierList(participantIdolIdentifiersBefore),
                ConcertParticipantIdListAfter = BuildDelimitedIdentifierList(participantIdolIdentifiersAfter),
                ConcertParticipantIdListAdded = BuildDelimitedIdentifierList(addedParticipantIdolIdentifiers),
                ConcertParticipantIdListRemoved = BuildDelimitedIdentifierList(removedParticipantIdolIdentifiers),
                ConcertSetlistSummaryBefore = previousState.ConcertSetlistSummaryBefore ?? string.Empty,
                ConcertSetlistSummaryAfter = setlistSummaryAfter ?? string.Empty,
                ConcertRemovedIdolId = removedIdol != null ? removedIdol.id : CoreConstants.InvalidIdValue
            };
        }

        /// <summary>
        /// Resolves the current concert ticket price or zero fallback.
        /// </summary>
        private static int ResolveConcertTicketPrice(SEvent_Concerts._concert concert)
        {
            if (concert == null || concert.ProjectedValues == null)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            return concert.ProjectedValues.TicketPrice;
        }

        /// <summary>
        /// Builds a compact setlist summary string from concert setlist items.
        /// </summary>
        private static string BuildConcertSetlistSummary(SEvent_Concerts._concert concert)
        {
            if (concert == null || concert.SetListItems == null || concert.SetListItems.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return string.Empty;
            }

            StringBuilder setlistSummaryBuilder = new StringBuilder();
            for (int itemIndex = CoreConstants.ZeroBasedListStartIndex; itemIndex < concert.SetListItems.Count; itemIndex++)
            {
                SEvent_Concerts._concert.ISetlistItem setlistItem = concert.SetListItems[itemIndex];
                if (setlistItem == null)
                {
                    continue;
                }

                if (setlistSummaryBuilder.Length > CoreConstants.ZeroBasedListStartIndex)
                {
                    setlistSummaryBuilder.Append(CoreConstants.ConcertSetlistEntrySeparator);
                }

                if (setlistItem is SEvent_Concerts._concert._song songItem)
                {
                    int singleId = songItem.Single != null ? songItem.Single.id : CoreConstants.InvalidIdValue;
                    int centerId = songItem.Center != null ? songItem.Center.id : CoreConstants.InvalidIdValue;
                    setlistSummaryBuilder.Append(CoreConstants.ConcertSetlistEntryTypeSong);
                    setlistSummaryBuilder.Append(CoreConstants.ConcertSetlistValueSeparator);
                    setlistSummaryBuilder.Append(singleId.ToString(CultureInfo.InvariantCulture));
                    setlistSummaryBuilder.Append(CoreConstants.ConcertSetlistValueSeparator);
                    setlistSummaryBuilder.Append(centerId.ToString(CultureInfo.InvariantCulture));
                    setlistSummaryBuilder.Append(CoreConstants.ConcertSetlistValueSeparator);
                    setlistSummaryBuilder.Append(NormalizeSetlistTextValue(songItem.GetTitle()));
                    continue;
                }

                if (setlistItem is SEvent_Concerts._concert._mc mcItem)
                {
                    List<int> mcGirlIds = new List<int>();
                    List<data_girls.girls> mcGirls = mcItem.GetGirls(true);
                    if (mcGirls != null)
                    {
                        for (int girlIndex = CoreConstants.ZeroBasedListStartIndex; girlIndex < mcGirls.Count; girlIndex++)
                        {
                            data_girls.girls mcGirl = mcGirls[girlIndex];
                            if (mcGirl == null || mcGirl.id < CoreConstants.MinimumValidIdolIdentifier)
                            {
                                continue;
                            }

                            mcGirlIds.Add(mcGirl.id);
                        }
                    }

                    setlistSummaryBuilder.Append(CoreConstants.ConcertSetlistEntryTypeMc);
                    setlistSummaryBuilder.Append(CoreConstants.ConcertSetlistValueSeparator);
                    setlistSummaryBuilder.Append(BuildDelimitedIdentifierList(mcGirlIds));
                    setlistSummaryBuilder.Append(CoreConstants.ConcertSetlistValueSeparator);
                    setlistSummaryBuilder.Append(NormalizeSetlistTextValue(mcItem.GetTitle()));
                    continue;
                }

                setlistSummaryBuilder.Append(CoreConstants.ConcertSetlistEntryTypeGeneric);
                setlistSummaryBuilder.Append(CoreConstants.ConcertSetlistValueSeparator);
                setlistSummaryBuilder.Append(NormalizeSetlistTextValue(setlistItem.GetTitle()));
            }

            return setlistSummaryBuilder.ToString();
        }

        /// <summary>
        /// Replaces setlist delimiters in free-form text fields to keep summaries parse-safe.
        /// </summary>
        private static string NormalizeSetlistTextValue(string rawValue)
        {
            string safeValue = rawValue ?? string.Empty;
            safeValue = safeValue.Replace(CoreConstants.ConcertSetlistEntrySeparator, CoreConstants.SetlistTextSeparatorReplacement);
            safeValue = safeValue.Replace(CoreConstants.ConcertSetlistValueSeparator, CoreConstants.SetlistTextSeparatorReplacement);
            return safeValue;
        }

        /// <summary>
        /// Builds a deterministic award entity identifier string.
        /// </summary>
        private static string BuildAwardEntityIdentifier(string awardTypeCode, int awardYear, int idolId, int singleId)
        {
            return string.Concat(
                awardTypeCode ?? CoreConstants.StatusCodeUnknown,
                CoreConstants.SaveKeyJoinSeparator,
                awardYear.ToString(CultureInfo.InvariantCulture),
                CoreConstants.SaveKeyJoinSeparator,
                idolId.ToString(CultureInfo.InvariantCulture),
                CoreConstants.SaveKeyJoinSeparator,
                singleId.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Builds a date-scoped idempotency key for show lifecycle events.
        /// </summary>
        private static string BuildShowLifecycleIdempotencyKey(string eventTypeCode, string showEntityIdentifier)
        {
            return string.Concat(
                CoreConstants.ShowLifecycleIdempotencyPrefix,
                CoreConstants.SaveKeyJoinSeparator,
                eventTypeCode ?? string.Empty,
                CoreConstants.SaveKeyJoinSeparator,
                showEntityIdentifier ?? string.Empty);
        }

        /// <summary>
        /// Builds a date-scoped idempotency key for award capture events.
        /// </summary>
        private static string BuildAwardIdempotencyKey(string eventTypeCode, string awardEntityIdentifier, bool awardWon, bool isNomination)
        {
            return string.Concat(
                eventTypeCode ?? string.Empty,
                CoreConstants.SaveKeyJoinSeparator,
                awardEntityIdentifier ?? string.Empty,
                CoreConstants.SaveKeyJoinSeparator,
                awardWon ? CoreConstants.EnabledCenterFlag.ToString(CultureInfo.InvariantCulture) : CoreConstants.DisabledCenterFlag.ToString(CultureInfo.InvariantCulture),
                CoreConstants.SaveKeyJoinSeparator,
                isNomination ? CoreConstants.EnabledCenterFlag.ToString(CultureInfo.InvariantCulture) : CoreConstants.DisabledCenterFlag.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Resolves push-slot idol identifiers with explicit invalid-id placeholders for empty slots.
        /// </summary>
        private static List<int> ResolvePushSlotIdolIdentifiers(IList<data_girls.girls> pushGirls)
        {
            List<int> pushSlotIdolIdentifiers = new List<int>();
            if (pushGirls == null)
            {
                return pushSlotIdolIdentifiers;
            }

            for (int slotIndex = CoreConstants.ZeroBasedListStartIndex; slotIndex < pushGirls.Count; slotIndex++)
            {
                data_girls.girls pushedIdol = pushGirls[slotIndex];
                int idolId = pushedIdol != null && pushedIdol.id >= CoreConstants.MinimumValidIdolIdentifier
                    ? pushedIdol.id
                    : CoreConstants.InvalidIdValue;
                pushSlotIdolIdentifiers.Add(idolId);
            }

            return pushSlotIdolIdentifiers;
        }

        /// <summary>
        /// Resolves push-slot day counters and pads missing indices with zero values.
        /// </summary>
        private static List<int> ResolvePushSlotDayCounts(IList<int> pushDays, int targetSlotCount)
        {
            List<int> pushSlotDayCounts = new List<int>();
            if (targetSlotCount < CoreConstants.ZeroBasedListStartIndex)
            {
                return pushSlotDayCounts;
            }

            for (int slotIndex = CoreConstants.ZeroBasedListStartIndex; slotIndex < targetSlotCount; slotIndex++)
            {
                int daysInSlot = pushDays != null && slotIndex < pushDays.Count
                    ? pushDays[slotIndex]
                    : CoreConstants.ZeroBasedListStartIndex;
                pushSlotDayCounts.Add(daysInSlot);
            }

            return pushSlotDayCounts;
        }

        /// <summary>
        /// Builds a deterministic push-entity identifier string for one push slot.
        /// </summary>
        private static string BuildPushEntityIdentifier(int slotIndex)
        {
            return string.Concat(
                CoreConstants.PushEntityIdentifierPrefix,
                CoreConstants.SaveKeyJoinSeparator,
                slotIndex.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Builds one comma-delimited identifier list string from integer values.
        /// </summary>
        private static string BuildDelimitedIdentifierList(IReadOnlyList<int> identifierValues)
        {
            if (identifierValues == null || identifierValues.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return string.Empty;
            }

            StringBuilder listBuilder = new StringBuilder();
            for (int identifierIndex = CoreConstants.ZeroBasedListStartIndex; identifierIndex < identifierValues.Count; identifierIndex++)
            {
                if (identifierIndex > CoreConstants.ZeroBasedListStartIndex)
                {
                    listBuilder.Append(CoreConstants.IdentifierListSeparator);
                }

                listBuilder.Append(identifierValues[identifierIndex].ToString(CultureInfo.InvariantCulture));
            }

            return listBuilder.ToString();
        }

        /// <summary>
        /// Resolves identifiers newly added in current set compared to previous set.
        /// </summary>
        private static List<int> ResolveAddedIdentifiers(IReadOnlyList<int> previousIdentifiers, IReadOnlyList<int> currentIdentifiers)
        {
            List<int> addedIdentifiers = new List<int>();
            if (currentIdentifiers == null || currentIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return addedIdentifiers;
            }

            HashSet<int> previousIdentifierSet = new HashSet<int>();
            if (previousIdentifiers != null)
            {
                for (int previousIndex = CoreConstants.ZeroBasedListStartIndex; previousIndex < previousIdentifiers.Count; previousIndex++)
                {
                    int previousIdentifier = previousIdentifiers[previousIndex];
                    if (previousIdentifier >= CoreConstants.MinimumValidIdolIdentifier)
                    {
                        previousIdentifierSet.Add(previousIdentifier);
                    }
                }
            }

            HashSet<int> seenAddedIdentifiers = new HashSet<int>();
            for (int currentIndex = CoreConstants.ZeroBasedListStartIndex; currentIndex < currentIdentifiers.Count; currentIndex++)
            {
                int currentIdentifier = currentIdentifiers[currentIndex];
                if (currentIdentifier < CoreConstants.MinimumValidIdolIdentifier)
                {
                    continue;
                }

                if (previousIdentifierSet.Contains(currentIdentifier) || !seenAddedIdentifiers.Add(currentIdentifier))
                {
                    continue;
                }

                addedIdentifiers.Add(currentIdentifier);
            }

            return addedIdentifiers;
        }

        /// <summary>
        /// Resolves identifiers removed from previous set compared to current set.
        /// </summary>
        private static List<int> ResolveRemovedIdentifiers(IReadOnlyList<int> previousIdentifiers, IReadOnlyList<int> currentIdentifiers)
        {
            List<int> removedIdentifiers = new List<int>();
            if (previousIdentifiers == null || previousIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return removedIdentifiers;
            }

            HashSet<int> currentIdentifierSet = new HashSet<int>();
            if (currentIdentifiers != null)
            {
                for (int currentIndex = CoreConstants.ZeroBasedListStartIndex; currentIndex < currentIdentifiers.Count; currentIndex++)
                {
                    int currentIdentifier = currentIdentifiers[currentIndex];
                    if (currentIdentifier >= CoreConstants.MinimumValidIdolIdentifier)
                    {
                        currentIdentifierSet.Add(currentIdentifier);
                    }
                }
            }

            HashSet<int> seenRemovedIdentifiers = new HashSet<int>();
            for (int previousIndex = CoreConstants.ZeroBasedListStartIndex; previousIndex < previousIdentifiers.Count; previousIndex++)
            {
                int previousIdentifier = previousIdentifiers[previousIndex];
                if (previousIdentifier < CoreConstants.MinimumValidIdolIdentifier)
                {
                    continue;
                }

                if (currentIdentifierSet.Contains(previousIdentifier) || !seenRemovedIdentifiers.Add(previousIdentifier))
                {
                    continue;
                }

                removedIdentifiers.Add(previousIdentifier);
            }

            return removedIdentifiers;
        }

        /// <summary>
        /// Resolves a deterministic distinct union across two identifier lists and one optional identifier.
        /// </summary>
        private static List<int> ResolveDistinctUnionIdentifiers(
            IReadOnlyList<int> firstIdentifiers,
            IReadOnlyList<int> secondIdentifiers,
            int optionalIdentifier)
        {
            List<int> combinedIdentifiers = new List<int>();
            HashSet<int> seenIdentifiers = new HashSet<int>();

            if (firstIdentifiers != null)
            {
                for (int firstIndex = CoreConstants.ZeroBasedListStartIndex; firstIndex < firstIdentifiers.Count; firstIndex++)
                {
                    int firstIdentifier = firstIdentifiers[firstIndex];
                    if (firstIdentifier >= CoreConstants.MinimumValidIdolIdentifier && seenIdentifiers.Add(firstIdentifier))
                    {
                        combinedIdentifiers.Add(firstIdentifier);
                    }
                }
            }

            if (secondIdentifiers != null)
            {
                for (int secondIndex = CoreConstants.ZeroBasedListStartIndex; secondIndex < secondIdentifiers.Count; secondIndex++)
                {
                    int secondIdentifier = secondIdentifiers[secondIndex];
                    if (secondIdentifier >= CoreConstants.MinimumValidIdolIdentifier && seenIdentifiers.Add(secondIdentifier))
                    {
                        combinedIdentifiers.Add(secondIdentifier);
                    }
                }
            }

            if (optionalIdentifier >= CoreConstants.MinimumValidIdolIdentifier && seenIdentifiers.Add(optionalIdentifier))
            {
                combinedIdentifiers.Add(optionalIdentifier);
            }

            return combinedIdentifiers;
        }

        /// <summary>
        /// Creates one push lifecycle payload from slot assignment state.
        /// </summary>
        private static PushLifecyclePayload BuildPushLifecyclePayload(
            int idolId,
            int pushSlotIndex,
            int pushPreviousIdolId,
            int pushCurrentIdolId,
            int pushDaysInSlot,
            string pushLifecycleActionCode)
        {
            return new PushLifecyclePayload
            {
                IdolId = idolId,
                PushSlotIndex = pushSlotIndex,
                PushPreviousIdolId = pushPreviousIdolId,
                PushCurrentIdolId = pushCurrentIdolId,
                PushDaysInSlot = pushDaysInSlot,
                PushLifecycleAction = pushLifecycleActionCode ?? string.Empty
            };
        }

        /// <summary>
        /// Resolves valid idol identifiers from one relationship pair.
        /// </summary>
        private static bool TryResolveRelationshipPairIdolIdentifiers(Relationships._relationship relationship, out int idolAId, out int idolBId)
        {
            idolAId = CoreConstants.InvalidIdValue;
            idolBId = CoreConstants.InvalidIdValue;
            if (relationship == null || relationship.Girls == null || relationship.Girls.Count < CoreConstants.MinimumRelationshipPairMemberCount)
            {
                return false;
            }

            data_girls.girls firstIdol = relationship.Girls[CoreConstants.ZeroBasedListStartIndex];
            data_girls.girls secondIdol = relationship.Girls[CoreConstants.ZeroBasedListStartIndex + CoreConstants.LastElementOffsetFromCount];
            if (firstIdol == null || secondIdol == null)
            {
                return false;
            }

            if (firstIdol.id < CoreConstants.MinimumValidIdolIdentifier || secondIdol.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return false;
            }

            idolAId = firstIdol.id;
            idolBId = secondIdol.id;
            return true;
        }

        /// <summary>
        /// Builds a deterministic entity identifier for one relationship idol pair.
        /// </summary>
        private static string BuildRelationshipPairEntityIdentifier(int idolAId, int idolBId)
        {
            int firstIdolId = idolAId;
            int secondIdolId = idolBId;
            if (firstIdolId > secondIdolId)
            {
                int temporaryIdolId = firstIdolId;
                firstIdolId = secondIdolId;
                secondIdolId = temporaryIdolId;
            }

            return string.Concat(
                firstIdolId.ToString(CultureInfo.InvariantCulture),
                CoreConstants.SaveKeyJoinSeparator,
                secondIdolId.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Resolves clique leader identifier or invalid sentinel when unavailable.
        /// </summary>
        private static int ResolveCliqueLeaderIdOrInvalid(Relationships._clique clique)
        {
            if (clique == null || clique.Leader == null)
            {
                return CoreConstants.InvalidIdValue;
            }

            return clique.Leader.id >= CoreConstants.MinimumValidIdolIdentifier
                ? clique.Leader.id
                : CoreConstants.InvalidIdValue;
        }

        /// <summary>
        /// Builds a deterministic clique signature from sorted member idol identifiers.
        /// </summary>
        private static string BuildCliqueSignature(Relationships._clique clique)
        {
            if (clique == null || clique.Members == null || clique.Members.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return string.Empty;
            }

            List<int> memberIds = new List<int>();
            for (int memberIndex = CoreConstants.ZeroBasedListStartIndex; memberIndex < clique.Members.Count; memberIndex++)
            {
                data_girls.girls member = clique.Members[memberIndex];
                if (member == null || member.id < CoreConstants.MinimumValidIdolIdentifier)
                {
                    continue;
                }

                memberIds.Add(member.id);
            }

            if (memberIds.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return string.Empty;
            }

            memberIds.Sort();
            StringBuilder signatureBuilder = new StringBuilder();
            for (int memberIndex = CoreConstants.ZeroBasedListStartIndex; memberIndex < memberIds.Count; memberIndex++)
            {
                if (memberIndex > CoreConstants.ZeroBasedListStartIndex)
                {
                    signatureBuilder.Append(CoreConstants.CliqueSignatureMemberSeparator);
                }

                signatureBuilder.Append(memberIds[memberIndex].ToString(CultureInfo.InvariantCulture));
            }

            return signatureBuilder.ToString();
        }

        /// <summary>
        /// Builds a deterministic bullying entity identifier from leader/target identifiers.
        /// </summary>
        private static string BuildBullyingEntityIdentifier(int leaderId, int targetId)
        {
            if (targetId < CoreConstants.MinimumValidIdolIdentifier)
            {
                return string.Empty;
            }

            return string.Concat(
                leaderId.ToString(CultureInfo.InvariantCulture),
                CoreConstants.SaveKeyJoinSeparator,
                targetId.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Builds a date-scoped idempotency key for bullying lifecycle events.
        /// </summary>
        private static string BuildBullyingLifecycleIdempotencyKey(string eventTypeCode, string bullyingEntityIdentifier, int targetId)
        {
            return string.Concat(
                CoreConstants.BullyingLifecycleIdempotencyPrefix,
                CoreConstants.SaveKeyJoinSeparator,
                eventTypeCode ?? string.Empty,
                CoreConstants.SaveKeyJoinSeparator,
                bullyingEntityIdentifier ?? string.Empty,
                CoreConstants.SaveKeyJoinSeparator,
                targetId.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Converts a long value to int while clamping at int bounds.
        /// </summary>
        private static int ClampLongToInt(long value)
        {
            if (value > int.MaxValue)
            {
                return int.MaxValue;
            }

            if (value < int.MinValue)
            {
                return int.MinValue;
            }

            return (int)value;
        }

        /// <summary>
        /// Resolves current player relationship points for one idol/type tuple.
        /// </summary>
        private static int ResolvePlayerRelationshipPoints(Relationships_Player._type relationshipType, data_girls.girls idol)
        {
            if (idol == null)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            switch (relationshipType)
            {
                case Relationships_Player._type.Friendship:
                    return idol.Rel_Friendship_Points;
                case Relationships_Player._type.Influence:
                    return idol.Rel_Influence_Points;
                case Relationships_Player._type.Romance:
                    return idol.Rel_Romance_Points;
                default:
                    return CoreConstants.ZeroBasedListStartIndex;
            }
        }

        /// <summary>
        /// Builds a deterministic entity identifier for one player-idol relationship track.
        /// </summary>
        private static string BuildPlayerRelationshipEntityIdentifier(int idolId, string relationshipTypeCode)
        {
            return string.Concat(
                idolId.ToString(CultureInfo.InvariantCulture),
                CoreConstants.SaveKeyJoinSeparator,
                relationshipTypeCode ?? CoreConstants.StatusCodeUnknown);
        }

        /// <summary>
        /// Resolves one compact result token for date interaction payloads.
        /// </summary>
        private static string ResolveDateInteractionResultToken(IList<string> resultTokens)
        {
            if (resultTokens == null)
            {
                return CoreConstants.DateInteractionResultTokenDeferred;
            }

            if (resultTokens.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return CoreConstants.DateInteractionResultTokenNone;
            }

            if (resultTokens.Count == CoreConstants.MinimumNonEmptyCollectionCount)
            {
                string singleToken = resultTokens[CoreConstants.ZeroBasedListStartIndex];
                return string.IsNullOrEmpty(singleToken) ? CoreConstants.DateInteractionResultTokenNone : singleToken;
            }

            StringBuilder tokenBuilder = new StringBuilder();
            for (int tokenIndex = CoreConstants.ZeroBasedListStartIndex; tokenIndex < resultTokens.Count; tokenIndex++)
            {
                string token = resultTokens[tokenIndex];
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

                if (tokenBuilder.Length > CoreConstants.ZeroBasedListStartIndex)
                {
                    tokenBuilder.Append(CoreConstants.DateInteractionResultTokenSeparator);
                }

                tokenBuilder.Append(token);
            }

            return tokenBuilder.Length > CoreConstants.ZeroBasedListStartIndex
                ? tokenBuilder.ToString()
                : CoreConstants.DateInteractionResultTokenNone;
        }

        /// <summary>
        /// Resolves one normalized summary code for player date outcomes.
        /// </summary>
        private static string ResolveDateInteractionResultSummaryCode(IList<string> resultTokens)
        {
            if (resultTokens == null)
            {
                return CoreConstants.DateInteractionResultSummaryCodeDialogueFollowup;
            }

            int nonEmptyTokenCount = CoreConstants.ZeroBasedListStartIndex;
            string normalizedSingleToken = string.Empty;
            for (int tokenIndex = CoreConstants.ZeroBasedListStartIndex; tokenIndex < resultTokens.Count; tokenIndex++)
            {
                string token = resultTokens[tokenIndex];
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

                string normalizedToken = token.Trim().ToLowerInvariant();
                if (normalizedToken.Length == CoreConstants.ZeroBasedListStartIndex)
                {
                    continue;
                }

                if (nonEmptyTokenCount == CoreConstants.ZeroBasedListStartIndex)
                {
                    normalizedSingleToken = normalizedToken;
                }

                nonEmptyTokenCount++;
            }

            if (nonEmptyTokenCount < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return CoreConstants.DateInteractionResultSummaryCodeNoSpecialResult;
            }

            if (nonEmptyTokenCount > CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return CoreConstants.DateInteractionResultSummaryCodeMultiResult;
            }

            switch (normalizedSingleToken)
            {
                case CoreConstants.DateInteractionResultTokenPublicDate:
                    return CoreConstants.DateInteractionResultSummaryCodePublicDate;
                case CoreConstants.DateInteractionResultTokenRoutineDate:
                    return CoreConstants.DateInteractionResultSummaryCodeRoutineDate;
                default:
                    return normalizedSingleToken;
            }
        }

        /// <summary>
        /// Reads one variables value using safe empty-string fallback.
        /// </summary>
        private static string ResolveVariableValueOrEmpty(string variableKey)
        {
            if (string.IsNullOrEmpty(variableKey))
            {
                return string.Empty;
            }

            string variableValue = variables.Get(variableKey);
            return variableValue ?? string.Empty;
        }

        /// <summary>
        /// Returns the newest value from a long metric series.
        /// </summary>
        private static long ResolveLatestLongMetric(IList<long> values)
        {
            if (values == null || values.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return CoreConstants.ZeroLongValue;
            }

            return values[values.Count - CoreConstants.LastElementOffsetFromCount];
        }

        /// <summary>
        /// Returns the previous value from a long metric series.
        /// </summary>
        private static long ResolvePreviousLongMetric(IList<long> values)
        {
            if (values == null || values.Count < CoreConstants.MinimumNonEmptyCollectionCount + CoreConstants.LastElementOffsetFromCount)
            {
                return CoreConstants.ZeroLongValue;
            }

            return values[values.Count - CoreConstants.PreviousElementOffsetFromCount];
        }

        /// <summary>
        /// Returns the newest value from an integer metric series.
        /// </summary>
        private static int ResolveLatestIntMetric(IList<int> values)
        {
            if (values == null || values.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            return values[values.Count - CoreConstants.LastElementOffsetFromCount];
        }

        /// <summary>
        /// Returns the previous value from an integer metric series.
        /// </summary>
        private static int ResolvePreviousIntMetric(IList<int> values)
        {
            if (values == null || values.Count < CoreConstants.MinimumNonEmptyCollectionCount + CoreConstants.LastElementOffsetFromCount)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            return values[values.Count - CoreConstants.PreviousElementOffsetFromCount];
        }

        /// <summary>
        /// Returns the newest value from a float metric series.
        /// </summary>
        private static float ResolveLatestFloatMetric(IList<float> values)
        {
            if (values == null || values.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return 0f;
            }

            return values[values.Count - CoreConstants.LastElementOffsetFromCount];
        }

        /// <summary>
        /// Returns the previous value from a float metric series.
        /// </summary>
        private static float ResolvePreviousFloatMetric(IList<float> values)
        {
            if (values == null || values.Count < CoreConstants.MinimumNonEmptyCollectionCount + CoreConstants.LastElementOffsetFromCount)
            {
                return 0f;
            }

            return values[values.Count - CoreConstants.PreviousElementOffsetFromCount];
        }

        /// <summary>
        /// Resolves a single identifier or returns invalid sentinel.
        /// </summary>
        private static int ResolveSingleIdOrInvalid(singles._single single)
        {
            return single != null ? single.id : CoreConstants.InvalidIdValue;
        }

        /// <summary>
        /// Resolves a concert identifier or returns invalid sentinel.
        /// </summary>
        private static int ResolveConcertIdOrInvalid(SEvent_Concerts._concert concert)
        {
            return concert != null ? concert.ID : CoreConstants.InvalidIdValue;
        }
    }

    /// <summary>
    /// Logging helper to keep all runtime diagnostics consistent.
    /// </summary>
    internal static class CoreLog
    {
        /// <summary>
        /// Writes an informational log message.
        /// </summary>
        internal static void Info(string message)
        {
            try
            {
                Debug.Log(CoreConstants.LogPrefix + message);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Writes a warning log message.
        /// </summary>
        internal static void Warn(string message)
        {
            try
            {
                Debug.LogWarning(CoreConstants.LogPrefix + message);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Writes an error log message.
        /// </summary>
        internal static void Error(string message)
        {
            try
            {
                Debug.LogError(CoreConstants.LogPrefix + message);
            }
            catch
            {
            }
        }
    }

    /// <summary>
    /// Date/time helpers shared by runtime and storage layers.
    /// </summary>
    internal static class CoreDateTimeUtility
    {
        /// <summary>
        /// Converts a game date to an integer key sortable by chronology.
        /// </summary>
        internal static int BuildGameDateKey(DateTime gameDate)
        {
            return gameDate.Year * CoreConstants.DateKeyYearMultiplier +
                   gameDate.Month * CoreConstants.DateKeyMonthMultiplier +
                   gameDate.Day;
        }

        /// <summary>
        /// Converts a date to an invariant, round-trip string.
        /// </summary>
        internal static string ToRoundTripString(DateTime value)
        {
            return value.ToString(CoreConstants.RoundTripDateFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Produces a UTC timestamp string for storage metadata updates.
        /// </summary>
        internal static string ToUtcRoundTripString(DateTime value)
        {
            return value.ToUniversalTime().ToString(CoreConstants.RoundTripDateFormat, CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Lightweight JSON writer used to avoid extra serializer dependencies in the mod runtime.
    /// </summary>
    internal static class CoreJsonUtility
    {
        /// <summary>
        /// Serializes arbitrary payload objects with Unity JsonUtility for additive domains.
        /// </summary>
        internal static string SerializeObjectPayload(object payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            try
            {
                return JsonUtility.ToJson(payload, CoreConstants.PrettyPrintJsonPayload);
            }
            catch
            {
                return CoreConstants.EmptyJsonObject;
            }
        }

        /// <summary>
        /// Serializes a single-lifecycle payload into compact JSON.
        /// </summary>
        internal static string SerializeSingleLifecyclePayload(SingleLifecyclePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldSingleId, payload.SingleId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleTitle, payload.SingleTitle ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleLifecycleAction, payload.SingleLifecycleAction ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleStatus, payload.SingleStatus ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldSingleCastCount, payload.SingleCastCount, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleCastIdList, payload.SingleCastIdList ?? string.Empty, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldSingleIsDigital, payload.SingleIsDigital, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldSingleLinkedElectionId, payload.SingleLinkedElectionId, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes a single-status payload into compact JSON.
        /// </summary>
        internal static string SerializeSingleStatusPayload(SingleStatusPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldSingleId, payload.SingleId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleTitle, payload.SingleTitle ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSinglePreviousStatus, payload.PreviousSingleStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleNewStatus, payload.NewSingleStatus ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldSingleCastCount, payload.SingleCastCount, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleCastIdList, payload.SingleCastIdList ?? string.Empty, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldSingleIsDigital, payload.SingleIsDigital, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldSingleLinkedElectionId, payload.SingleLinkedElectionId, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes a single cast-change payload into compact JSON.
        /// </summary>
        internal static string SerializeSingleCastChangePayload(SingleCastChangePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldSingleId, payload.SingleId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleTitle, payload.SingleTitle ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSinglePreviousStatus, payload.PreviousSingleStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleNewStatus, payload.NewSingleStatus ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldSingleCastCountBefore, payload.SingleCastCountBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldSingleCastCountAfter, payload.SingleCastCountAfter, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleCastIdListBefore, payload.SingleCastIdListBefore ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleCastIdListAfter, payload.SingleCastIdListAfter ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleCastIdListAdded, payload.SingleCastIdListAdded ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleCastIdListRemoved, payload.SingleCastIdListRemoved ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldSingleRemovedIdolId, payload.SingleRemovedIdolId, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes a single group-change payload into compact JSON.
        /// </summary>
        internal static string SerializeSingleGroupChangePayload(SingleGroupChangePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldSingleId, payload.SingleId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleTitle, payload.SingleTitle ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSinglePreviousStatus, payload.PreviousSingleStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleNewStatus, payload.NewSingleStatus ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldSingleCastCount, payload.SingleCastCount, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleCastIdList, payload.SingleCastIdList ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldFromGroupId, payload.FromGroupId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldFromGroupTitle, payload.FromGroupTitle ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldFromGroupStatus, payload.FromGroupStatus ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldToGroupId, payload.ToGroupId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldToGroupTitle, payload.ToGroupTitle ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldToGroupStatus, payload.ToGroupStatus ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes an idol group-transfer payload into compact JSON.
        /// </summary>
        internal static string SerializeIdolGroupTransferPayload(IdolGroupTransferPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldIdolStatus, payload.IdolStatus ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldFromGroupId, payload.FromGroupId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldFromGroupTitle, payload.FromGroupTitle ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldFromGroupStatus, payload.FromGroupStatus ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldToGroupId, payload.ToGroupId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldToGroupTitle, payload.ToGroupTitle ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldToGroupStatus, payload.ToGroupStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldTransferDate, payload.TransferDate ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes a group lifecycle payload into compact JSON.
        /// </summary>
        internal static string SerializeGroupLifecyclePayload(GroupLifecyclePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldGroupId, payload.GroupId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldGroupTitle, payload.GroupTitle ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldGroupStatus, payload.GroupStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldGroupLifecycleAction, payload.GroupLifecycleAction ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldGroupDateCreated, payload.GroupDateCreated ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldGroupEventDate, payload.GroupEventDate ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldGroupMemberCount, payload.GroupMemberCount, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldGroupMemberIdList, payload.GroupMemberIdList ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldGroupSingleCount, payload.GroupSingleCount, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldGroupNonReleasedSingleCount, payload.GroupNonReleasedSingleCount, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldGroupAppealGender, payload.GroupAppealGender ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldGroupAppealHardcoreness, payload.GroupAppealHardcoreness ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldGroupAppealAge, payload.GroupAppealAge ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes a group-parameter points payload into compact JSON.
        /// </summary>
        internal static string SerializeGroupParamPointsPayload(GroupParamPointsPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldGroupId, payload.GroupId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldGroupTitle, payload.GroupTitle ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldGroupStatus, payload.GroupStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldGroupSourceParamType, payload.GroupSourceParamType ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldGroupPointsRequested, payload.GroupPointsRequested, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldGroupPointsBefore, payload.GroupPointsBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldGroupPointsAfter, payload.GroupPointsAfter, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldGroupPointsDelta, payload.GroupPointsDelta, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldGroupAvailablePointsBefore, payload.GroupAvailablePointsBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldGroupAvailablePointsAfter, payload.GroupAvailablePointsAfter, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldGroupEventDate, payload.GroupEventDate ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes a group-appeal spend payload into compact JSON.
        /// </summary>
        internal static string SerializeGroupAppealPointsSpentPayload(GroupAppealPointsSpentPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldGroupId, payload.GroupId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldGroupTitle, payload.GroupTitle ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldGroupStatus, payload.GroupStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldGroupSourceParamType, payload.GroupSourceParamType ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldGroupTargetFanType, payload.GroupTargetFanType ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldGroupPointsRequested, payload.GroupPointsRequested, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldGroupPointsApplied, payload.GroupPointsApplied, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldGroupAvailablePointsBefore, payload.GroupAvailablePointsBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldGroupAvailablePointsAfter, payload.GroupAvailablePointsAfter, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldGroupPointsSpentBefore, payload.GroupPointsSpentBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldGroupPointsSpentAfter, payload.GroupPointsSpentAfter, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldGroupTargetPointsBefore, payload.GroupTargetPointsBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldGroupTargetPointsAfter, payload.GroupTargetPointsAfter, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldGroupEventDate, payload.GroupEventDate ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes an idol-lifecycle payload into compact JSON.
        /// </summary>
        internal static string SerializeIdolLifecyclePayload(IdolLifecyclePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldIdolLifecycleAction, payload.IdolLifecycleAction ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldIdolStatus, payload.IdolStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldIdolType, payload.IdolType ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldIdolAge, payload.IdolAge, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldIdolHiringDate, payload.IdolHiringDate ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldIdolGraduationDate, payload.IdolGraduationDate ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldIdolTrivia, payload.IdolTrivia ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldIdolCustomTrivia, payload.IdolCustomTrivia ?? string.Empty, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldIdolGraduationWithDialogue, payload.IdolGraduationWithDialogue, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes an idol salary-change payload into compact JSON.
        /// </summary>
        internal static string SerializeIdolSalaryChangePayload(IdolSalaryChangePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSalaryAction, payload.SalaryAction ?? string.Empty, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldSalaryBefore, payload.SalaryBefore, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldSalaryAfter, payload.SalaryAfter, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldSalaryDelta, payload.SalaryDelta, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldSalarySatisfactionBefore, payload.SalarySatisfactionBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldSalarySatisfactionAfter, payload.SalarySatisfactionAfter, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes a single-participation payload into compact JSON.
        /// </summary>
        internal static string SerializeSingleParticipationPayload(SingleParticipationPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldSingleId, payload.SingleId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleTitle, payload.SingleTitle ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleStatus, payload.SingleStatus ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldRowIndex, payload.RowIndex, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldPositionIndex, payload.PositionIndex, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldIsCenter, payload.IsCenter, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleReleaseDate, payload.SingleReleaseDate ?? string.Empty, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldTotalSales, payload.TotalSales, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldQuality, payload.Quality, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldFanSatisfaction, payload.FanSatisfaction, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldFanBuzz, payload.FanBuzz, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldNewFans, payload.NewFans, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldNewHardcoreFans, payload.NewHardcoreFans, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldNewCasualFans, payload.NewCasualFans, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldSingleQuantity, payload.SingleQuantity, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldSingleProductionCost, payload.SingleProductionCost, ref isFirstProperty);
            AppendFloatProperty(builder, CoreConstants.JsonFieldSingleMarketingResult, payload.SingleMarketingResult, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleMarketingResultStatus, payload.SingleMarketingResultStatus ?? string.Empty, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldSingleGrossRevenue, payload.SingleGrossRevenue, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldSingleOneCdCost, payload.SingleOneCdCost, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldSingleOneCdRevenue, payload.SingleOneCdRevenue, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldSingleOtherExpenses, payload.SingleOtherExpenses, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldSingleIsGroupHandshake, payload.SingleIsGroupHandshake, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldSingleIsIndividualHandshake, payload.SingleIsIndividualHandshake, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldSingleFamePointsAwarded, payload.SingleFamePointsAwarded, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldSingleProfit, payload.SingleProfit, ref isFirstProperty);
            AppendFloatProperty(builder, CoreConstants.JsonFieldSingleSalesPerFan, payload.SingleSalesPerFan, ref isFirstProperty);
            AppendFloatProperty(builder, CoreConstants.JsonFieldSingleFameOfSenbatsu, payload.SingleFameOfSenbatsu, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldSingleMostPopularGenre, payload.SingleMostPopularGenre, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldSingleMostPopularLyrics, payload.SingleMostPopularLyrics, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldSingleMostPopularChoreo, payload.SingleMostPopularChoreo, ref isFirstProperty);
            AppendFloatProperty(builder, CoreConstants.JsonFieldSingleFanAppealMale, payload.SingleFanAppealMale, ref isFirstProperty);
            AppendFloatProperty(builder, CoreConstants.JsonFieldSingleFanAppealFemale, payload.SingleFanAppealFemale, ref isFirstProperty);
            AppendFloatProperty(builder, CoreConstants.JsonFieldSingleFanAppealCasual, payload.SingleFanAppealCasual, ref isFirstProperty);
            AppendFloatProperty(builder, CoreConstants.JsonFieldSingleFanAppealHardcore, payload.SingleFanAppealHardcore, ref isFirstProperty);
            AppendFloatProperty(builder, CoreConstants.JsonFieldSingleFanAppealTeen, payload.SingleFanAppealTeen, ref isFirstProperty);
            AppendFloatProperty(builder, CoreConstants.JsonFieldSingleFanAppealYoungAdult, payload.SingleFanAppealYoungAdult, ref isFirstProperty);
            AppendFloatProperty(builder, CoreConstants.JsonFieldSingleFanAppealAdult, payload.SingleFanAppealAdult, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleFanSegmentSalesSummary, payload.SingleFanSegmentSalesSummary ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleFanSegmentNewFansSummary, payload.SingleFanSegmentNewFansSummary ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldSingleSenbatsuStatsSnapshot, payload.SingleSenbatsuStatsSnapshot ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldChartPosition, payload.ChartPosition, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes a status-transition payload into compact JSON.
        /// </summary>
        internal static string SerializeStatusTransitionPayload(StatusTransitionPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldPreviousStatus, payload.PreviousStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldNewStatus, payload.NewStatus ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes a producer-dating status payload into compact JSON.
        /// </summary>
        internal static string SerializeDatingPartnerStatusPayload(DatingPartnerStatusPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldPreviousStatus, payload.PreviousStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldNewStatus, payload.NewStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldDatingRoute, payload.DatingRoute ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldDatingRouteStage, payload.DatingRouteStage ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes an idol-partner status payload into compact JSON.
        /// </summary>
        internal static string SerializeIdolDatingStatusPayload(IdolDatingStatusPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldPreviousPartnerStatus, payload.PreviousPartnerStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldNewPartnerStatus, payload.NewPartnerStatus ?? string.Empty, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldDatingHadScandal, payload.HadScandal, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldDatingHadScandalEver, payload.HadScandalEver, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldDatingUsedGoods, payload.UsedGoods, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldDatingDatedIdol, payload.DatedIdol, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes contract lifecycle payloads into compact JSON.
        /// </summary>
        internal static string SerializeContractLifecyclePayload(ContractLifecyclePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldContractType, payload.ContractType ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldContractSkill, payload.ContractSkill ?? string.Empty, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldContractIsGroup, payload.IsGroupContract, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldContractWeeklyPayment, payload.WeeklyPayment, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldContractWeeklyBuzz, payload.WeeklyBuzz, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldContractWeeklyFame, payload.WeeklyFame, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldContractWeeklyFans, payload.WeeklyFans, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldContractWeeklyStamina, payload.WeeklyStamina, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldContractLiability, payload.ContractLiability, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldContractAgentName, payload.AgentName ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldContractProductName, payload.ProductName ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldContractStartDate, payload.ContractStartDate ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldContractEndDate, payload.ContractEndDate ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldContractDurationMonths, payload.ContractDurationMonths, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldContractIsImmediate, payload.ContractIsImmediate, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldContractDamagesApplied, payload.DamagesApplied, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldContractTotalBrokenLiability, payload.TotalBrokenLiability, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldContractBreakContext, payload.ContractBreakContext ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes weekly contract accrual payloads into compact JSON.
        /// </summary>
        internal static string SerializeContractWeeklyAccrualPayload(ContractWeeklyAccrualPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldContractType, payload.ContractType ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldContractSkill, payload.ContractSkill ?? string.Empty, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldContractIsGroup, payload.IsGroupContract, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldContractWeeklyPayment, payload.WeeklyPayment, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldContractWeeklyBuzz, payload.WeeklyBuzz, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldContractWeeklyFame, payload.WeeklyFame, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldContractWeeklyFans, payload.WeeklyFans, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldContractWeeklyStamina, payload.WeeklyStamina, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldContractWeeklyTrainingPoints, payload.WeeklyTrainingPoints, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldContractEndDate, payload.ContractEndDate ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldContractWeeklyAction, payload.ContractWeeklyAction ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes show lifecycle payloads into compact JSON.
        /// </summary>
        internal static string SerializeShowLifecyclePayload(ShowLifecyclePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldShowId, payload.ShowId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowTitle, payload.ShowTitle ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowLifecycleAction, payload.ShowLifecycleAction ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowStatus, payload.ShowStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowCastType, payload.ShowCastType ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowMediumCode, payload.ShowMediumCode ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowMediumTitle, payload.ShowMediumTitle ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowGenreCode, payload.ShowGenreCode ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowGenreTitle, payload.ShowGenreTitle ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowEpisodeCount, payload.ShowEpisodeCount, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowCastCount, payload.ShowCastCount, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowCastIdList, payload.ShowCastIdList ?? string.Empty, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldShowLatestAudience, payload.ShowLatestAudience, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldShowLatestRevenue, payload.ShowLatestRevenue, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowLatestNewFans, payload.ShowLatestNewFans, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowLatestBuzz, payload.ShowLatestBuzz, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowRelaunchCount, payload.ShowRelaunchCount, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldShowWasRelaunched, payload.ShowWasRelaunched, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowLaunchDate, payload.ShowLaunchDate ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes show-status payloads into compact JSON.
        /// </summary>
        internal static string SerializeShowStatusPayload(ShowStatusPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldShowId, payload.ShowId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowTitle, payload.ShowTitle ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowPreviousStatus, payload.PreviousShowStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowNewStatus, payload.NewShowStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowCastType, payload.ShowCastType ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowEpisodeCount, payload.ShowEpisodeCount, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowCastCount, payload.ShowCastCount, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldShowLatestAudience, payload.ShowLatestAudience, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldShowLatestRevenue, payload.ShowLatestRevenue, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowLatestNewFans, payload.ShowLatestNewFans, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowLatestBuzz, payload.ShowLatestBuzz, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes show-episode payloads into compact JSON.
        /// </summary>
        internal static string SerializeShowEpisodePayload(ShowEpisodePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldShowId, payload.ShowId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowTitle, payload.ShowTitle ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowCastType, payload.ShowCastType ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowEpisodeCount, payload.ShowEpisodeCount, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowEpisodeDate, payload.ShowEpisodeDate ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowCastCount, payload.ShowCastCount, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowCastIdList, payload.ShowCastIdList ?? string.Empty, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldShowPreviousAudience, payload.ShowPreviousAudience, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldShowLatestAudience, payload.ShowLatestAudience, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldShowAudienceDelta, payload.ShowAudienceDelta, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldShowPreviousRevenue, payload.ShowPreviousRevenue, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldShowLatestRevenue, payload.ShowLatestRevenue, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldShowRevenueDelta, payload.ShowRevenueDelta, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldShowPreviousProfit, payload.ShowPreviousProfit, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldShowLatestProfit, payload.ShowLatestProfit, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldShowProfitDelta, payload.ShowProfitDelta, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowPreviousNewFans, payload.ShowPreviousNewFans, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowLatestNewFans, payload.ShowLatestNewFans, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowNewFansDelta, payload.ShowNewFansDelta, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowPreviousBuzz, payload.ShowPreviousBuzz, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowLatestBuzz, payload.ShowLatestBuzz, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowBuzzDelta, payload.ShowBuzzDelta, ref isFirstProperty);
            AppendFloatProperty(builder, CoreConstants.JsonFieldShowPreviousFatigue, payload.ShowPreviousFatigue, ref isFirstProperty);
            AppendFloatProperty(builder, CoreConstants.JsonFieldShowLatestFatigue, payload.ShowLatestFatigue, ref isFirstProperty);
            AppendFloatProperty(builder, CoreConstants.JsonFieldShowFatigueDelta, payload.ShowFatigueDelta, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowPreviousFame, payload.ShowPreviousFame, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowLatestFame, payload.ShowLatestFame, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowFameDelta, payload.ShowFameDelta, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowPreviousFamePoints, payload.ShowPreviousFamePoints, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowLatestFamePoints, payload.ShowLatestFamePoints, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowFamePointsDelta, payload.ShowFamePointsDelta, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowEpisodeBudget, payload.ShowEpisodeBudget, ref isFirstProperty);
            AppendFloatProperty(builder, CoreConstants.JsonFieldShowStaminaCost, payload.ShowStaminaCost, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes show cast-change payloads into compact JSON.
        /// </summary>
        internal static string SerializeShowCastChangePayload(ShowCastChangePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldShowId, payload.ShowId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowTitle, payload.ShowTitle ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowPreviousStatus, payload.PreviousShowStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowNewStatus, payload.NewShowStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowCastTypeBefore, payload.ShowCastTypeBefore ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowCastTypeAfter, payload.ShowCastTypeAfter ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowCastCountBefore, payload.ShowCastCountBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowCastCountAfter, payload.ShowCastCountAfter, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowCastIdListBefore, payload.ShowCastIdListBefore ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowCastIdListAfter, payload.ShowCastIdListAfter ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowCastIdListAdded, payload.ShowCastIdListAdded ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowCastIdListRemoved, payload.ShowCastIdListRemoved ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowRemovedIdolId, payload.ShowRemovedIdolId, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes show configuration-change payloads into compact JSON.
        /// </summary>
        internal static string SerializeShowConfigurationChangePayload(ShowConfigurationChangePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldShowId, payload.ShowId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowTitleBefore, payload.ShowTitleBefore ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowTitleAfter, payload.ShowTitleAfter ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowPreviousStatus, payload.PreviousShowStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowNewStatus, payload.NewShowStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowCastTypeBefore, payload.ShowCastTypeBefore ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowCastTypeAfter, payload.ShowCastTypeAfter ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowCastCountBefore, payload.ShowCastCountBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowCastCountAfter, payload.ShowCastCountAfter, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowCastIdListBefore, payload.ShowCastIdListBefore ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowCastIdListAfter, payload.ShowCastIdListAfter ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowMcCodeBefore, payload.ShowMcCodeBefore ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowMcCodeAfter, payload.ShowMcCodeAfter ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowMcTitleBefore, payload.ShowMcTitleBefore ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowMcTitleAfter, payload.ShowMcTitleAfter ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowProductionCostBefore, payload.ShowProductionCostBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldShowProductionCostAfter, payload.ShowProductionCostAfter, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowFanAppealSummaryBefore, payload.ShowFanAppealSummaryBefore ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldShowFanAppealSummaryAfter, payload.ShowFanAppealSummaryAfter ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes concert lifecycle payloads into compact JSON.
        /// </summary>
        internal static string SerializeConcertLifecyclePayload(ConcertLifecyclePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldConcertId, payload.ConcertId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertTitle, payload.ConcertTitle ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertVenue, payload.ConcertVenue ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertStatus, payload.ConcertStatus ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldConcertSongCount, payload.ConcertSongCount, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldConcertParticipantCount, payload.ConcertParticipantCount, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertParticipantIdList, payload.ConcertParticipantIdList ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertSetlistSummary, payload.ConcertSetlistSummary ?? string.Empty, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldConcertProjectedAudience, payload.ConcertProjectedAudience, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldConcertActualAudience, payload.ConcertActualAudience, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldConcertProjectedRevenue, payload.ConcertProjectedRevenue, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldConcertActualRevenue, payload.ConcertActualRevenue, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldConcertProductionCost, payload.ConcertProductionCost, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldConcertProfit, payload.ConcertProfit, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldConcertHype, payload.ConcertHype, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldConcertTicketPrice, payload.ConcertTicketPrice, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertFinishDate, payload.ConcertFinishDate ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes concert status payloads into compact JSON.
        /// </summary>
        internal static string SerializeConcertStatusPayload(ConcertStatusPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldConcertId, payload.ConcertId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertTitle, payload.ConcertTitle ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertPreviousStatus, payload.PreviousConcertStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertNewStatus, payload.NewConcertStatus ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldConcertSongCount, payload.ConcertSongCount, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldConcertParticipantCount, payload.ConcertParticipantCount, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertParticipantIdList, payload.ConcertParticipantIdList ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertSetlistSummary, payload.ConcertSetlistSummary ?? string.Empty, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldConcertProjectedAudience, payload.ConcertProjectedAudience, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldConcertProjectedRevenue, payload.ConcertProjectedRevenue, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldConcertTicketPrice, payload.ConcertTicketPrice, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes concert cast-change payloads into compact JSON.
        /// </summary>
        internal static string SerializeConcertCastChangePayload(ConcertCastChangePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldConcertId, payload.ConcertId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertTitle, payload.ConcertTitle ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertPreviousStatus, payload.PreviousConcertStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertNewStatus, payload.NewConcertStatus ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldConcertSongCountBefore, payload.ConcertSongCountBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldConcertSongCountAfter, payload.ConcertSongCountAfter, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldConcertParticipantCountBefore, payload.ConcertParticipantCountBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldConcertParticipantCountAfter, payload.ConcertParticipantCountAfter, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertParticipantIdListBefore, payload.ConcertParticipantIdListBefore ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertParticipantIdListAfter, payload.ConcertParticipantIdListAfter ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertParticipantIdListAdded, payload.ConcertParticipantIdListAdded ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertParticipantIdListRemoved, payload.ConcertParticipantIdListRemoved ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertSetlistSummaryBefore, payload.ConcertSetlistSummaryBefore ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertSetlistSummaryAfter, payload.ConcertSetlistSummaryAfter ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldConcertRemovedIdolId, payload.ConcertRemovedIdolId, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes concert configuration-change payloads into compact JSON.
        /// </summary>
        internal static string SerializeConcertConfigurationChangePayload(ConcertConfigurationChangePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldConcertId, payload.ConcertId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertTitleBefore, payload.ConcertTitleBefore ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertTitleAfter, payload.ConcertTitleAfter ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertRawTitleBefore, payload.ConcertRawTitleBefore ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertRawTitleAfter, payload.ConcertRawTitleAfter ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertVenueBefore, payload.ConcertVenueBefore ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertVenueAfter, payload.ConcertVenueAfter ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldConcertTicketPriceBefore, payload.ConcertTicketPriceBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldConcertTicketPriceAfter, payload.ConcertTicketPriceAfter, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldConcertSongCountBefore, payload.ConcertSongCountBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldConcertSongCountAfter, payload.ConcertSongCountAfter, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldConcertParticipantCountBefore, payload.ConcertParticipantCountBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldConcertParticipantCountAfter, payload.ConcertParticipantCountAfter, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertParticipantIdListBefore, payload.ConcertParticipantIdListBefore ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertParticipantIdListAfter, payload.ConcertParticipantIdListAfter ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertSetlistSummaryBefore, payload.ConcertSetlistSummaryBefore ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldConcertSetlistSummaryAfter, payload.ConcertSetlistSummaryAfter ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes award lifecycle payloads into compact JSON.
        /// </summary>
        internal static string SerializeAwardLifecyclePayload(AwardLifecyclePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldAwardType, payload.AwardType ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldAwardYear, payload.AwardYear, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldAwardIsNomination, payload.AwardIsNomination, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldAwardWon, payload.AwardWon, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldAwardSingleId, payload.AwardSingleId, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes push lifecycle payloads into compact JSON.
        /// </summary>
        internal static string SerializePushLifecyclePayload(PushLifecyclePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldPushSlotIndex, payload.PushSlotIndex, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldPushPreviousIdolId, payload.PushPreviousIdolId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldPushCurrentIdolId, payload.PushCurrentIdolId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldPushDaysInSlot, payload.PushDaysInSlot, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldPushLifecycleAction, payload.PushLifecycleAction ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes idol relationship lifecycle payloads into compact JSON.
        /// </summary>
        internal static string SerializeIdolRelationshipLifecyclePayload(IdolRelationshipLifecyclePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldRelationshipIdolAId, payload.IdolAId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldRelationshipIdolBId, payload.IdolBId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldRelationshipStatus, payload.RelationshipStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldRelationshipDynamic, payload.RelationshipDynamic ?? string.Empty, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldRelationshipKnownToPlayer, payload.RelationshipKnownToPlayer, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldRelationshipPairKey, payload.RelationshipPairKey ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldRelationshipBreakReason, payload.RelationshipBreakReason ?? string.Empty, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldRelationshipIsDating, payload.RelationshipIsDating, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes idol relationship status-change payloads into compact JSON.
        /// </summary>
        internal static string SerializeIdolRelationshipStatusChangePayload(IdolRelationshipStatusChangePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldRelationshipIdolAId, payload.IdolAId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldRelationshipIdolBId, payload.IdolBId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldRelationshipPreviousStatus, payload.RelationshipPreviousStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldRelationshipNewStatus, payload.RelationshipNewStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldRelationshipDynamic, payload.RelationshipDynamic ?? string.Empty, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldRelationshipKnownToPlayer, payload.RelationshipKnownToPlayer, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldRelationshipPairKey, payload.RelationshipPairKey ?? string.Empty, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldRelationshipIsDating, payload.RelationshipIsDating, ref isFirstProperty);
            AppendFloatProperty(builder, CoreConstants.JsonFieldRelationshipRequestedDelta, payload.RelationshipRequestedDelta, ref isFirstProperty);
            AppendFloatProperty(builder, CoreConstants.JsonFieldRelationshipRatio, payload.RelationshipRatio, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes clique lifecycle payloads into compact JSON.
        /// </summary>
        internal static string SerializeCliqueLifecyclePayload(CliqueLifecyclePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldCliqueLeaderId, payload.CliqueLeaderId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldCliqueLeaderIdBefore, payload.CliqueLeaderIdBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldCliqueLeaderIdAfter, payload.CliqueLeaderIdAfter, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldCliqueMemberCount, payload.CliqueMemberCount, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldCliqueSignature, payload.CliqueSignature ?? string.Empty, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldCliqueQuitWasViolent, payload.CliqueQuitWasViolent, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes bullying lifecycle payloads into compact JSON.
        /// </summary>
        internal static string SerializeBullyingLifecyclePayload(BullyingLifecyclePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldBullyingTargetId, payload.BullyingTargetId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldBullyingLeaderId, payload.BullyingLeaderId, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldBullyingKnownToPlayer, payload.BullyingKnownToPlayer, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldCliqueMemberCount, payload.CliqueMemberCount, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldCliqueSignature, payload.CliqueSignature ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes scandal mitigation payloads into compact JSON.
        /// </summary>
        internal static string SerializeScandalMitigationPayload(ScandalMitigationPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldScandalPointsAvailableBefore, payload.ScandalPointsAvailableBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldScandalPointsRemoved, payload.ScandalPointsRemoved, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldScandalPointsBefore, payload.ScandalPointsBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldScandalPointsAfter, payload.ScandalPointsAfter, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldScandalGroupPointsRemoved, payload.ScandalGroupPointsRemoved, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldScandalGroupPointsRemaining, payload.ScandalGroupPointsRemaining, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes player relationship delta payloads into compact JSON.
        /// </summary>
        internal static string SerializePlayerRelationshipDeltaPayload(PlayerRelationshipDeltaPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldPlayerRelationshipType, payload.PlayerRelationshipType ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldPlayerPointsRequestedDelta, payload.PlayerPointsRequestedDelta, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldPlayerPointsAppliedDelta, payload.PlayerPointsAppliedDelta, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldPlayerPointsBefore, payload.PlayerPointsBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldPlayerPointsAfter, payload.PlayerPointsAfter, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldPlayerLevelBefore, payload.PlayerLevelBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldPlayerLevelAfter, payload.PlayerLevelAfter, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldPlayerLevelChanged, payload.PlayerLevelChanged, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes player date interaction payloads into compact JSON.
        /// </summary>
        internal static string SerializePlayerDateInteractionPayload(PlayerDateInteractionPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldDateInteractionType, payload.DateInteractionType ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldDateRouteBefore, payload.DateRouteBefore ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldDateRouteAfter, payload.DateRouteAfter ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldDateStageBefore, payload.DateStageBefore ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldDateStageAfter, payload.DateStageAfter ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldDateStatusBefore, payload.DateStatusBefore ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldDateStatusAfter, payload.DateStatusAfter ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldDateResultToken, payload.DateResultToken ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldDateResultSummaryCode, payload.DateResultSummaryCode ?? string.Empty, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldDateCaughtBefore, payload.DateCaughtBefore, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldDateCaughtAfter, payload.DateCaughtAfter, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldDateRelationshipLevelBefore, payload.DateRelationshipLevelBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldDateRelationshipLevelAfter, payload.DateRelationshipLevelAfter, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes player marriage outcome payloads into compact JSON.
        /// </summary>
        internal static string SerializePlayerMarriageOutcomePayload(PlayerMarriageOutcomePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldMarriageStage, payload.MarriageStage ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldMarriageRoute, payload.MarriageRoute ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldMarriagePartnerStatus, payload.MarriagePartnerStatus ?? string.Empty, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldMarriageGoodOutcome, payload.MarriageGoodOutcome, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldMarriageGirlQuitTriggered, payload.MarriageGirlQuitTriggered, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldMarriageKidsString, payload.MarriageKidsString ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldMarriageCareerStringOne, payload.MarriageCareerStringOne ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldMarriageCareerStringTwo, payload.MarriageCareerStringTwo ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldMarriageRelationshipOutcomeString, payload.MarriageRelationshipOutcomeString ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldMarriageCustodyString, payload.MarriageCustodyString ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldMarriageGraduationTrivia, payload.MarriageGraduationTrivia ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldMarriageIdolStatus, payload.MarriageIdolStatus ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes world-tour lifecycle payloads into compact JSON.
        /// </summary>
        internal static string SerializeTourLifecyclePayload(TourLifecyclePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldTourId, payload.TourId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldTourLifecycleAction, payload.TourLifecycleAction ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldTourStatus, payload.TourStatus ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldTourSelectedCountryCount, payload.SelectedCountryCount, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldTourSelectedCountryLevelList, payload.TourSelectedCountryLevelList ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldTourParticipantCount, payload.TourParticipantCount, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldTourParticipantIdList, payload.TourParticipantIdList ?? string.Empty, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldTourTotalAudience, payload.TourTotalAudience, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldTourTotalRevenue, payload.TourTotalRevenue, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldTourTotalNewFans, payload.TourTotalNewFans, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldTourProductionCost, payload.TourProductionCost, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldTourExpectedRevenue, payload.TourExpectedRevenue, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldTourSaving, payload.TourSaving, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldTourStaminaCost, payload.TourStaminaCost, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldTourProfit, payload.TourProfit, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldTourStartDate, payload.TourStartDate ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldTourFinishDate, payload.TourFinishDate ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes world-tour country-result payloads into compact JSON.
        /// </summary>
        internal static string SerializeTourCountryResultPayload(TourCountryResultPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldTourId, payload.TourId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldTourStatus, payload.TourStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldTourFinishDate, payload.TourFinishDate ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldTourCountryCode, payload.TourCountryCode ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldTourCountryLevel, payload.TourCountryLevel, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldTourCountryAttendance, payload.TourCountryAttendance, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldTourCountryAudience, payload.TourCountryAudience, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldTourCountryNewFans, payload.TourCountryNewFans, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldTourCountryRevenue, payload.TourCountryRevenue, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldTourCountryDiscount, payload.TourCountryDiscount, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes world-tour idol participation payloads into compact JSON.
        /// </summary>
        internal static string SerializeTourParticipationPayload(TourParticipationPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldTourId, payload.TourId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldTourParticipantCount, payload.TourParticipantCount, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldTourParticipantIdList, payload.TourParticipantIdList ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldTourLifecycleAction, payload.TourLifecycleAction ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes tour-status payloads into compact JSON.
        /// </summary>
        internal static string SerializeTourStatusPayload(TourStatusPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldTourId, payload.TourId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldTourPreviousStatus, payload.PreviousTourStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldTourNewStatus, payload.NewTourStatus ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldTourSelectedCountryCount, payload.SelectedCountryCount, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldTourTotalAudience, payload.TourTotalAudience, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldTourTotalRevenue, payload.TourTotalRevenue, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldTourTotalNewFans, payload.TourTotalNewFans, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldTourProductionCost, payload.TourProductionCost, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldTourExpectedRevenue, payload.TourExpectedRevenue, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldTourSaving, payload.TourSaving, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldTourFinishDate, payload.TourFinishDate ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes election lifecycle payloads into compact JSON.
        /// </summary>
        internal static string SerializeElectionLifecyclePayload(ElectionLifecyclePayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldElectionId, payload.ElectionId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldElectionLifecycleAction, payload.ElectionLifecycleAction ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldElectionStatus, payload.ElectionStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldElectionBroadcastType, payload.ElectionBroadcastType ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldElectionSingleId, payload.ElectionSingleId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldElectionConcertId, payload.ElectionConcertId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldElectionReleaseSingleId, payload.ElectionReleaseSingleId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldElectionResultCount, payload.ElectionResultCount, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldElectionFinishDate, payload.ElectionFinishDate ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes generated election-result payloads into compact JSON.
        /// </summary>
        internal static string SerializeElectionGeneratedResultPayload(ElectionGeneratedResultPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldElectionId, payload.ElectionId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldElectionExpectedPlace, payload.ElectionExpectedPlace, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldElectionGeneratedPlace, payload.ElectionGeneratedPlace, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldElectionGeneratedVotes, payload.ElectionGeneratedVotes, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldElectionGeneratedFamePoints, payload.ElectionGeneratedFamePoints, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldElectionBroadcastType, payload.ElectionBroadcastType ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes manual election place-adjustment payloads into compact JSON.
        /// </summary>
        internal static string SerializeElectionPlaceAdjustedPayload(ElectionPlaceAdjustedPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldElectionId, payload.ElectionId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldElectionPlaceBefore, payload.ElectionPlaceBefore, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldElectionPlaceAfter, payload.ElectionPlaceAfter, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldElectionBroadcastType, payload.ElectionBroadcastType ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes election-status payloads into compact JSON.
        /// </summary>
        internal static string SerializeElectionStatusPayload(ElectionStatusPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldElectionId, payload.ElectionId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldElectionPreviousStatus, payload.PreviousElectionStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldElectionNewStatus, payload.NewElectionStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldElectionBroadcastType, payload.ElectionBroadcastType ?? string.Empty, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldElectionSingleId, payload.ElectionSingleId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldElectionConcertId, payload.ElectionConcertId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldElectionReleaseSingleId, payload.ElectionReleaseSingleId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldElectionResultCount, payload.ElectionResultCount, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldElectionFinishDate, payload.ElectionFinishDate ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes one election-result payload into compact JSON.
        /// </summary>
        internal static string SerializeElectionResultPayload(ElectionResultPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldElectionId, payload.ElectionId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldElectionPlace, payload.ElectionPlace, ref isFirstProperty);
            AppendLongProperty(builder, CoreConstants.JsonFieldElectionVotes, payload.ElectionVotes, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldElectionFamePoints, payload.ElectionFamePoints, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldElectionBroadcastType, payload.ElectionBroadcastType ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldElectionFinishDate, payload.ElectionFinishDate ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes scandal-point mutation payloads into compact JSON.
        /// </summary>
        internal static string SerializeScandalPointsPayload(ScandalPointsPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldScandalPreviousPoints, payload.PreviousScandalPoints, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldScandalNewPoints, payload.NewScandalPoints, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldScandalDeltaPoints, payload.ScandalPointDelta, ref isFirstProperty);
            AppendFloatProperty(builder, CoreConstants.JsonFieldScandalPreviousPointsRaw, payload.PreviousScandalPointsRaw, ref isFirstProperty);
            AppendFloatProperty(builder, CoreConstants.JsonFieldScandalNewPointsRaw, payload.NewScandalPointsRaw, ref isFirstProperty);
            AppendFloatProperty(builder, CoreConstants.JsonFieldScandalDeltaPointsRaw, payload.ScandalPointDeltaRaw, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldScandalMutationSource, payload.ScandalMutationSource ?? string.Empty, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Serializes medical lifecycle payloads into compact JSON.
        /// </summary>
        internal static string SerializeMedicalEventPayload(MedicalEventPayload payload)
        {
            if (payload == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            AppendIntProperty(builder, CoreConstants.JsonFieldIdolId, payload.IdolId, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldMedicalEventType, payload.MedicalEventType ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldMedicalPreviousStatus, payload.MedicalPreviousStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldMedicalCurrentStatus, payload.MedicalCurrentStatus ?? string.Empty, ref isFirstProperty);
            AppendStringProperty(builder, CoreConstants.JsonFieldMedicalHiatusEndDate, payload.MedicalHiatusEndDate ?? string.Empty, ref isFirstProperty);
            AppendBooleanProperty(builder, CoreConstants.JsonFieldMedicalFinishWasForced, payload.MedicalFinishWasForced, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldMedicalInjuryCounter, payload.MedicalInjuryCounter, ref isFirstProperty);
            AppendIntProperty(builder, CoreConstants.JsonFieldMedicalDepressionCounter, payload.MedicalDepressionCounter, ref isFirstProperty);

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        /// <summary>
        /// Writes an integer JSON property.
        /// </summary>
        private static void AppendIntProperty(StringBuilder builder, string propertyName, int value, ref bool isFirstProperty)
        {
            AppendPropertyPrefix(builder, propertyName, ref isFirstProperty);
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Writes a long JSON property.
        /// </summary>
        private static void AppendLongProperty(StringBuilder builder, string propertyName, long value, ref bool isFirstProperty)
        {
            AppendPropertyPrefix(builder, propertyName, ref isFirstProperty);
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Writes a float JSON property using round-trip precision.
        /// </summary>
        private static void AppendFloatProperty(StringBuilder builder, string propertyName, float value, ref bool isFirstProperty)
        {
            AppendPropertyPrefix(builder, propertyName, ref isFirstProperty);
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                builder.Append(CoreConstants.ZeroBasedListStartIndex.ToString(CultureInfo.InvariantCulture));
                return;
            }

            builder.Append(value.ToString(CoreConstants.JsonFloatRoundTripFormat, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Writes a boolean JSON property.
        /// </summary>
        private static void AppendBooleanProperty(StringBuilder builder, string propertyName, bool value, ref bool isFirstProperty)
        {
            AppendPropertyPrefix(builder, propertyName, ref isFirstProperty);
            builder.Append(value ? CoreConstants.JsonBooleanTrue : CoreConstants.JsonBooleanFalse);
        }

        /// <summary>
        /// Writes a string JSON property.
        /// </summary>
        private static void AppendStringProperty(StringBuilder builder, string propertyName, string value, ref bool isFirstProperty)
        {
            AppendPropertyPrefix(builder, propertyName, ref isFirstProperty);
            AppendEscapedString(builder, value ?? string.Empty);
        }

        /// <summary>
        /// Writes shared JSON property prefix syntax.
        /// </summary>
        private static void AppendPropertyPrefix(StringBuilder builder, string propertyName, ref bool isFirstProperty)
        {
            if (!isFirstProperty)
            {
                builder.Append(CoreConstants.JsonPropertySeparatorCharacter);
            }

            isFirstProperty = false;
            AppendEscapedString(builder, propertyName);
            builder.Append(CoreConstants.JsonNameValueSeparatorCharacter);
        }

        /// <summary>
        /// Appends a properly escaped JSON string literal.
        /// </summary>
        private static void AppendEscapedString(StringBuilder builder, string value)
        {
            builder.Append(CoreConstants.JsonStringQuoteCharacter);

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < value.Length; i++)
            {
                char currentCharacter = value[i];
                switch (currentCharacter)
                {
                    case CoreConstants.JsonStringQuoteCharacter:
                        builder.Append(CoreConstants.JsonEscapedQuote);
                        break;
                    case CoreConstants.JsonEscapeCharacter:
                        builder.Append(CoreConstants.JsonEscapedBackslash);
                        break;
                    case CoreConstants.JsonLineFeedCharacter:
                        builder.Append(CoreConstants.JsonEscapedNewLine);
                        break;
                    case CoreConstants.JsonCarriageReturnCharacter:
                        builder.Append(CoreConstants.JsonEscapedCarriageReturn);
                        break;
                    case CoreConstants.JsonTabCharacter:
                        builder.Append(CoreConstants.JsonEscapedTab);
                        break;
                    default:
                        if (currentCharacter < ' ')
                        {
                            builder.Append(CoreConstants.JsonEscapedUnicodePrefix);
                            builder.Append(((int)currentCharacter).ToString(CoreConstants.FourDigitLowerHexFormat, CultureInfo.InvariantCulture));
                            break;
                        }

                        builder.Append(currentCharacter);
                        break;
                }
            }

            builder.Append(CoreConstants.JsonStringQuoteCharacter);
        }
    }

    /// <summary>
    /// Token sanitization helper for save keys, namespaces, and data keys.
    /// </summary>
    internal static class CoreTokenUtility
    {
        /// <summary>
        /// Keeps only safe token characters and caps output length.
        /// </summary>
        internal static string SanitizeToken(string rawValue, int maximumLength)
        {
            if (string.IsNullOrEmpty(rawValue))
            {
                return string.Empty;
            }

            int expectedLength = Math.Min(rawValue.Length, maximumLength);
            char[] output = new char[expectedLength];
            int outputLength = CoreConstants.ZeroBasedListStartIndex;

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < rawValue.Length && outputLength < maximumLength; i++)
            {
                char character = rawValue[i];
                if (IsTokenCharacterAllowed(character))
                {
                    output[outputLength] = character;
                    outputLength++;
                }
            }

            if (outputLength <= CoreConstants.ZeroBasedListStartIndex)
            {
                return string.Empty;
            }

            return new string(output, CoreConstants.ZeroBasedListStartIndex, outputLength);
        }

        /// <summary>
        /// Returns true when the character belongs to the allowed token alphabet.
        /// </summary>
        private static bool IsTokenCharacterAllowed(char character)
        {
            bool isAsciiLetter =
                (character >= 'a' && character <= 'z') ||
                (character >= 'A' && character <= 'Z');
            if (isAsciiLetter || (character >= '0' && character <= '9'))
            {
                return true;
            }

            return character == CoreConstants.TokenUnderscoreCharacter
                || character == CoreConstants.TokenHyphenCharacter
                || character == CoreConstants.TokenDotCharacter;
        }
    }

    /// <summary>
    /// Converts game status enum values to stable status code strings.
    /// </summary>
    internal static class CoreStatusMapping
    {
        /// <summary>
        /// Maps a `data_girls._status` enum to a stable string identifier.
        /// </summary>
        internal static string ToStatusCode(data_girls._status status)
        {
            switch (status)
            {
                case data_girls._status.normal:
                    return CoreConstants.StatusCodeNormal;
                case data_girls._status.practice:
                    return CoreConstants.StatusCodePractice;
                case data_girls._status.scene:
                    return CoreConstants.StatusCodeScene;
                case data_girls._status.injured:
                    return CoreConstants.StatusCodeInjured;
                case data_girls._status.depressed:
                    return CoreConstants.StatusCodeDepressed;
                case data_girls._status.hiatus:
                    return CoreConstants.StatusCodeHiatus;
                case data_girls._status.announced_graduation:
                    return CoreConstants.StatusCodeAnnouncedGraduation;
                case data_girls._status.graduated:
                    return CoreConstants.StatusCodeGraduated;
                default:
                    return CoreConstants.StatusCodeUnknown;
            }
        }
    }

    /// <summary>
    /// Enum-to-string helpers used by capture payloads for stable, readable codes.
    /// </summary>
    internal static class CoreEnumNameMapping
    {
        /// <summary>
        /// Maps producer dating status enum values to lowercase string codes.
        /// </summary>
        internal static string ToDatingPartnerStatusCode(Dating._partner._status status)
        {
            return ToLowerInvariantEnumCode(status);
        }

        /// <summary>
        /// Maps producer dating route enum values to lowercase string codes.
        /// </summary>
        internal static string ToDatingPartnerRouteCode(Dating._partner._route route)
        {
            return ToLowerInvariantEnumCode(route);
        }

        /// <summary>
        /// Maps producer dating route-stage enum values to lowercase string codes.
        /// </summary>
        internal static string ToDatingPartnerRouteStageCode(Dating._partner._route_stage routeStage)
        {
            return ToLowerInvariantEnumCode(routeStage);
        }

        /// <summary>
        /// Maps idol partner status enum values to lowercase string codes.
        /// </summary>
        internal static string ToIdolDatingPartnerStatusCode(data_girls.girls._dating_data._partner_status status)
        {
            return ToLowerInvariantEnumCode(status);
        }

        /// <summary>
        /// Maps business contract type enum values to lowercase string codes.
        /// </summary>
        internal static string ToBusinessContractTypeCode(business._type contractType)
        {
            return ToLowerInvariantEnumCode(contractType);
        }

        /// <summary>
        /// Maps idol parameter enum values to lowercase string codes.
        /// </summary>
        internal static string ToIdolParameterCode(data_girls._paramType parameterType)
        {
            return ToLowerInvariantEnumCode(parameterType);
        }

        /// <summary>
        /// Maps idol type enum values to lowercase string codes.
        /// </summary>
        internal static string ToIdolTypeCode(data_girls.girls._type idolType)
        {
            return ToLowerInvariantEnumCode(idolType);
        }

        /// <summary>
        /// Maps single status enum values to lowercase string codes.
        /// </summary>
        internal static string ToSingleStatusCode(singles._single._status status)
        {
            return ToLowerInvariantEnumCode(status);
        }

        /// <summary>
        /// Maps group status enum values to lowercase string codes.
        /// </summary>
        internal static string ToGroupStatusCode(Groups._group._status status)
        {
            return ToLowerInvariantEnumCode(status);
        }

        /// <summary>
        /// Maps fan-type enum values to lowercase string codes.
        /// </summary>
        internal static string ToFanTypeCode(resources.fanType fanType)
        {
            return ToLowerInvariantEnumCode(fanType);
        }

        /// <summary>
        /// Maps show status enum values to lowercase string codes.
        /// </summary>
        internal static string ToShowStatusCode(Shows._show._status status)
        {
            return ToLowerInvariantEnumCode(status);
        }

        /// <summary>
        /// Maps show cast type enum values to lowercase string codes.
        /// </summary>
        internal static string ToShowCastTypeCode(Shows._show._castType castType)
        {
            return ToLowerInvariantEnumCode(castType);
        }

        /// <summary>
        /// Maps concert venue enum values to lowercase string codes.
        /// </summary>
        internal static string ToConcertVenueCode(SEvent_Concerts._venue venue)
        {
            return ToLowerInvariantEnumCode(venue);
        }

        /// <summary>
        /// Maps award type enum values to lowercase string codes.
        /// </summary>
        internal static string ToAwardTypeCode(Awards._type awardType)
        {
            return ToLowerInvariantEnumCode(awardType);
        }

        /// <summary>
        /// Maps idol-idol relationship status enum values to lowercase string codes.
        /// </summary>
        internal static string ToRelationshipStatusCode(Relationships._relationship._status relationshipStatus)
        {
            return ToLowerInvariantEnumCode(relationshipStatus);
        }

        /// <summary>
        /// Maps idol-idol relationship dynamic enum values to lowercase string codes.
        /// </summary>
        internal static string ToRelationshipDynamicCode(Relationships._relationship._dynamic relationshipDynamic)
        {
            return ToLowerInvariantEnumCode(relationshipDynamic);
        }

        /// <summary>
        /// Maps player relationship type enum values to lowercase string codes.
        /// </summary>
        internal static string ToPlayerRelationshipTypeCode(Relationships_Player._type playerRelationshipType)
        {
            return ToLowerInvariantEnumCode(playerRelationshipType);
        }

        /// <summary>
        /// Maps tour status enum values to lowercase string codes.
        /// </summary>
        internal static string ToTourStatusCode(SEvent_Tour.tour._status status)
        {
            return ToLowerInvariantEnumCode(status);
        }

        /// <summary>
        /// Maps tour-country enum values to lowercase string codes.
        /// </summary>
        internal static string ToTourCountryCode(SEvent_Tour._country countryType)
        {
            return ToLowerInvariantEnumCode(countryType);
        }

        /// <summary>
        /// Maps election broadcast enum values to lowercase string codes.
        /// </summary>
        internal static string ToElectionBroadcastCode(SEvent_SSK._broadcast broadcastType)
        {
            return ToLowerInvariantEnumCode(broadcastType);
        }

        /// <summary>
        /// Maps loan source enum values to lowercase string codes.
        /// </summary>
        internal static string ToLoanTypeCode(loans._loan._type loanType)
        {
            return ToLowerInvariantEnumCode(loanType);
        }

        /// <summary>
        /// Maps loan duration enum values to lowercase string codes.
        /// </summary>
        internal static string ToLoanDurationCode(loans._loan._duration duration)
        {
            return ToLowerInvariantEnumCode(duration);
        }

        /// <summary>
        /// Maps policy type enum values to lowercase string codes.
        /// </summary>
        internal static string ToPolicyTypeCode(policies._type policyType)
        {
            return ToLowerInvariantEnumCode(policyType);
        }

        /// <summary>
        /// Maps policy value enum values to lowercase string codes.
        /// </summary>
        internal static string ToPolicyValueCode(policies._value policyValue)
        {
            return ToLowerInvariantEnumCode(policyValue);
        }

        /// <summary>
        /// Maps theater schedule enum values to lowercase string codes.
        /// </summary>
        internal static string ToTheaterScheduleTypeCode(Theaters._theater._schedule._type scheduleType)
        {
            return ToLowerInvariantEnumCode(scheduleType);
        }

        /// <summary>
        /// Maps cafe dish enum values to lowercase string codes.
        /// </summary>
        internal static string ToCafeDishTypeCode(Cafes._cafe._dish._type dishType)
        {
            return ToLowerInvariantEnumCode(dishType);
        }

        /// <summary>
        /// Maps cafe staff-priority enum values to lowercase string codes.
        /// </summary>
        internal static string ToCafeStaffPriorityCode(Cafes._cafe._staff_prio staffPriority)
        {
            return ToLowerInvariantEnumCode(staffPriority);
        }

        /// <summary>
        /// Maps cafe operation-priority enum values to lowercase string codes.
        /// </summary>
        internal static string ToCafePriorityCode(Cafes._cafe._cafe_prio cafePriority)
        {
            return ToLowerInvariantEnumCode(cafePriority);
        }

        /// <summary>
        /// Maps staff role enum values to lowercase string codes.
        /// </summary>
        internal static string ToStaffTypeCode(staff._type staffType)
        {
            return ToLowerInvariantEnumCode(staffType);
        }

        /// <summary>
        /// Maps staff unique-type enum values to lowercase string codes.
        /// </summary>
        internal static string ToStaffUniqueTypeCode(staff._staff._unique_type uniqueType)
        {
            return ToLowerInvariantEnumCode(uniqueType);
        }

        /// <summary>
        /// Maps agency room enum values to lowercase string codes.
        /// </summary>
        internal static string ToAgencyRoomTypeCode(agency._type roomType)
        {
            return ToLowerInvariantEnumCode(roomType);
        }

        /// <summary>
        /// Maps research category enum values to lowercase string codes.
        /// </summary>
        internal static string ToResearchTypeCode(Research.type researchType)
        {
            return ToLowerInvariantEnumCode(researchType);
        }

        /// <summary>
        /// Maps single parameter enum values to lowercase string codes.
        /// </summary>
        internal static string ToSingleParamTypeCode(singles._param._type paramType)
        {
            return ToLowerInvariantEnumCode(paramType);
        }

        /// <summary>
        /// Maps story task type enum values to lowercase string codes.
        /// </summary>
        internal static string ToTaskTypeCode(tasks._task._type taskType)
        {
            return ToLowerInvariantEnumCode(taskType);
        }

        /// <summary>
        /// Maps story task goal enum values to lowercase string codes.
        /// </summary>
        internal static string ToTaskGoalCode(tasks._task._goal taskGoal)
        {
            return ToLowerInvariantEnumCode(taskGoal);
        }

        /// <summary>
        /// Maps story route enum values to lowercase string codes.
        /// </summary>
        internal static string ToTaskRouteCode(tasks._route route)
        {
            return ToLowerInvariantEnumCode(route);
        }

        /// <summary>
        /// Maps concert card enum values to lowercase string codes.
        /// </summary>
        internal static string ToConcertCardTypeCode(SEvent_Concerts._cardType cardType)
        {
            return ToLowerInvariantEnumCode(cardType);
        }

        /// <summary>
        /// Maps concert accident result enum values to lowercase string codes.
        /// </summary>
        internal static string ToConcertAccidentResultTypeCode(SEvent_Concerts._accident._result._type resultType)
        {
            return ToLowerInvariantEnumCode(resultType);
        }

        /// <summary>
        /// Maps idol wish enum values to lowercase string codes.
        /// </summary>
        internal static string ToWishTypeCode(girl_wishes._type wishType)
        {
            return ToLowerInvariantEnumCode(wishType);
        }

        /// <summary>
        /// Converts any enum value to lowercase text with an explicit unknown fallback.
        /// </summary>
        private static string ToLowerInvariantEnumCode(Enum enumValue)
        {
            if (enumValue == null)
            {
                return CoreConstants.StatusCodeUnknown;
            }

            string rawCode = enumValue.ToString();
            if (string.IsNullOrEmpty(rawCode))
            {
                return CoreConstants.StatusCodeUnknown;
            }

            return rawCode.ToLowerInvariant();
        }
    }

    /// <summary>
    /// JSON payload emitted for single lifecycle events.
    /// </summary>
    [Serializable]
    internal sealed class SingleLifecyclePayload
    {
        public int SingleId;
        public string SingleTitle = string.Empty;
        public string SingleLifecycleAction = string.Empty;
        public string SingleStatus = string.Empty;
        public int SingleCastCount;
        public string SingleCastIdList = string.Empty;
        public bool SingleIsDigital;
        public int SingleLinkedElectionId = CoreConstants.InvalidIdValue;
    }

    /// <summary>
    /// JSON payload emitted for single status transitions.
    /// </summary>
    [Serializable]
    internal sealed class SingleStatusPayload
    {
        public int SingleId;
        public string SingleTitle = string.Empty;
        public string PreviousSingleStatus = string.Empty;
        public string NewSingleStatus = string.Empty;
        public int SingleCastCount;
        public string SingleCastIdList = string.Empty;
        public bool SingleIsDigital;
        public int SingleLinkedElectionId = CoreConstants.InvalidIdValue;
    }

    /// <summary>
    /// JSON payload emitted for single cast-change events.
    /// </summary>
    [Serializable]
    internal sealed class SingleCastChangePayload
    {
        public int SingleId;
        public string SingleTitle = string.Empty;
        public string PreviousSingleStatus = string.Empty;
        public string NewSingleStatus = string.Empty;
        public int SingleCastCountBefore;
        public int SingleCastCountAfter;
        public string SingleCastIdListBefore = string.Empty;
        public string SingleCastIdListAfter = string.Empty;
        public string SingleCastIdListAdded = string.Empty;
        public string SingleCastIdListRemoved = string.Empty;
        public int SingleRemovedIdolId = CoreConstants.InvalidIdValue;
    }

    /// <summary>
    /// JSON payload emitted for single group-change events.
    /// </summary>
    [Serializable]
    internal sealed class SingleGroupChangePayload
    {
        public int SingleId;
        public string SingleTitle = string.Empty;
        public string PreviousSingleStatus = string.Empty;
        public string NewSingleStatus = string.Empty;
        public int SingleCastCount;
        public string SingleCastIdList = string.Empty;
        public int FromGroupId = CoreConstants.InvalidIdValue;
        public string FromGroupTitle = string.Empty;
        public string FromGroupStatus = string.Empty;
        public int ToGroupId = CoreConstants.InvalidIdValue;
        public string ToGroupTitle = string.Empty;
        public string ToGroupStatus = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for idol group-transfer events.
    /// </summary>
    [Serializable]
    internal sealed class IdolGroupTransferPayload
    {
        public int IdolId = CoreConstants.InvalidIdValue;
        public string IdolStatus = string.Empty;
        public int FromGroupId = CoreConstants.InvalidIdValue;
        public string FromGroupTitle = string.Empty;
        public string FromGroupStatus = string.Empty;
        public int ToGroupId = CoreConstants.InvalidIdValue;
        public string ToGroupTitle = string.Empty;
        public string ToGroupStatus = string.Empty;
        public string TransferDate = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for group lifecycle events.
    /// </summary>
    [Serializable]
    internal sealed class GroupLifecyclePayload
    {
        public int GroupId = CoreConstants.InvalidIdValue;
        public string GroupTitle = string.Empty;
        public string GroupStatus = string.Empty;
        public string GroupLifecycleAction = string.Empty;
        public string GroupDateCreated = string.Empty;
        public string GroupEventDate = string.Empty;
        public int GroupMemberCount;
        public string GroupMemberIdList = string.Empty;
        public int GroupSingleCount;
        public int GroupNonReleasedSingleCount;
        public string GroupAppealGender = string.Empty;
        public string GroupAppealHardcoreness = string.Empty;
        public string GroupAppealAge = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for group parameter-point mutations.
    /// </summary>
    [Serializable]
    internal sealed class GroupParamPointsPayload
    {
        public int GroupId = CoreConstants.InvalidIdValue;
        public string GroupTitle = string.Empty;
        public string GroupStatus = string.Empty;
        public string GroupSourceParamType = string.Empty;
        public int GroupPointsRequested;
        public int GroupPointsBefore;
        public int GroupPointsAfter;
        public int GroupPointsDelta;
        public int GroupAvailablePointsBefore;
        public int GroupAvailablePointsAfter;
        public string GroupEventDate = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted when group appeal points are spent.
    /// </summary>
    [Serializable]
    internal sealed class GroupAppealPointsSpentPayload
    {
        public int GroupId = CoreConstants.InvalidIdValue;
        public string GroupTitle = string.Empty;
        public string GroupStatus = string.Empty;
        public string GroupSourceParamType = string.Empty;
        public string GroupTargetFanType = string.Empty;
        public int GroupPointsRequested;
        public int GroupPointsApplied;
        public int GroupAvailablePointsBefore;
        public int GroupAvailablePointsAfter;
        public int GroupPointsSpentBefore;
        public int GroupPointsSpentAfter;
        public int GroupTargetPointsBefore;
        public int GroupTargetPointsAfter;
        public string GroupEventDate = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for idol lifecycle milestone events.
    /// </summary>
    [Serializable]
    internal sealed class IdolLifecyclePayload
    {
        public int IdolId = CoreConstants.InvalidIdValue;
        public string IdolLifecycleAction = string.Empty;
        public string IdolStatus = string.Empty;
        public string IdolType = string.Empty;
        public int IdolAge;
        public string IdolHiringDate = string.Empty;
        public string IdolGraduationDate = string.Empty;
        public string IdolTrivia = string.Empty;
        public string IdolCustomTrivia = string.Empty;
        public bool IdolGraduationWithDialogue;
    }

    /// <summary>
    /// JSON payload emitted for idol salary change events.
    /// </summary>
    [Serializable]
    internal sealed class IdolSalaryChangePayload
    {
        public int IdolId = CoreConstants.InvalidIdValue;
        public string SalaryAction = string.Empty;
        public long SalaryBefore;
        public long SalaryAfter;
        public long SalaryDelta;
        public int SalarySatisfactionBefore;
        public int SalarySatisfactionAfter;
    }

    /// <summary>
    /// JSON payload emitted for each single participation event.
    /// </summary>
    [Serializable]
    internal sealed class SingleParticipationPayload
    {
        public int SingleId;
        public string SingleTitle = string.Empty;
        public string SingleStatus = string.Empty;
        public int IdolId;
        public int RowIndex;
        public int PositionIndex;
        public bool IsCenter;
        public string SingleReleaseDate = string.Empty;
        public long TotalSales;
        public int Quality;
        public int FanSatisfaction;
        public int FanBuzz;
        public int NewFans;
        public int NewHardcoreFans;
        public int NewCasualFans;
        public int SingleQuantity;
        public long SingleProductionCost;
        public float SingleMarketingResult;
        public string SingleMarketingResultStatus = string.Empty;
        public long SingleGrossRevenue;
        public int SingleOneCdCost;
        public int SingleOneCdRevenue;
        public long SingleOtherExpenses;
        public bool SingleIsGroupHandshake;
        public bool SingleIsIndividualHandshake;
        public int SingleFamePointsAwarded;
        public long SingleProfit;
        public float SingleSalesPerFan;
        public float SingleFameOfSenbatsu;
        public bool SingleMostPopularGenre;
        public bool SingleMostPopularLyrics;
        public bool SingleMostPopularChoreo;
        public float SingleFanAppealMale;
        public float SingleFanAppealFemale;
        public float SingleFanAppealCasual;
        public float SingleFanAppealHardcore;
        public float SingleFanAppealTeen;
        public float SingleFanAppealYoungAdult;
        public float SingleFanAppealAdult;
        public string SingleFanSegmentSalesSummary = string.Empty;
        public string SingleFanSegmentNewFansSummary = string.Empty;
        public string SingleSenbatsuStatsSnapshot = string.Empty;
        public int ChartPosition;
    }

    /// <summary>
    /// JSON payload emitted for each idol status transition event.
    /// </summary>
    [Serializable]
    internal sealed class StatusTransitionPayload
    {
        public int IdolId;
        public string PreviousStatus = string.Empty;
        public string NewStatus = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for producer-dating status transitions.
    /// </summary>
    [Serializable]
    internal sealed class DatingPartnerStatusPayload
    {
        public int IdolId;
        public string PreviousStatus = string.Empty;
        public string NewStatus = string.Empty;
        public string DatingRoute = string.Empty;
        public string DatingRouteStage = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for idol dating partner status transitions.
    /// </summary>
    [Serializable]
    internal sealed class IdolDatingStatusPayload
    {
        public int IdolId;
        public string PreviousPartnerStatus = string.Empty;
        public string NewPartnerStatus = string.Empty;
        public bool HadScandal;
        public bool HadScandalEver;
        public bool UsedGoods;
        public bool DatedIdol;
    }

    /// <summary>
    /// JSON payload emitted for contract lifecycle events.
    /// </summary>
    [Serializable]
    internal sealed class ContractLifecyclePayload
    {
        public int IdolId;
        public string ContractType = string.Empty;
        public string ContractSkill = string.Empty;
        public bool IsGroupContract;
        public int WeeklyPayment;
        public int WeeklyBuzz;
        public int WeeklyFame;
        public int WeeklyFans;
        public int WeeklyStamina;
        public long ContractLiability;
        public string AgentName = string.Empty;
        public string ProductName = string.Empty;
        public string ContractStartDate = string.Empty;
        public string ContractEndDate = string.Empty;
        public int ContractDurationMonths;
        public bool ContractIsImmediate;
        public bool DamagesApplied;
        public long TotalBrokenLiability;
        public string ContractBreakContext = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for weekly contract accrual events.
    /// </summary>
    [Serializable]
    internal sealed class ContractWeeklyAccrualPayload
    {
        public int IdolId = CoreConstants.InvalidIdValue;
        public string ContractType = string.Empty;
        public string ContractSkill = string.Empty;
        public bool IsGroupContract;
        public int WeeklyPayment;
        public int WeeklyBuzz;
        public int WeeklyFame;
        public int WeeklyFans;
        public int WeeklyStamina;
        public int WeeklyTrainingPoints;
        public string ContractEndDate = string.Empty;
        public string ContractWeeklyAction = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for show-creation and show-release lifecycle events.
    /// </summary>
    [Serializable]
    internal sealed class ShowLifecyclePayload
    {
        public int ShowId;
        public string ShowTitle = string.Empty;
        public string ShowLifecycleAction = string.Empty;
        public string ShowStatus = string.Empty;
        public string ShowCastType = string.Empty;
        public string ShowMediumCode = string.Empty;
        public string ShowMediumTitle = string.Empty;
        public string ShowGenreCode = string.Empty;
        public string ShowGenreTitle = string.Empty;
        public int ShowEpisodeCount;
        public int ShowCastCount;
        public string ShowCastIdList = string.Empty;
        public long ShowLatestAudience;
        public long ShowLatestRevenue;
        public int ShowLatestNewFans;
        public int ShowLatestBuzz;
        public int ShowRelaunchCount;
        public bool ShowWasRelaunched;
        public string ShowLaunchDate = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for show status transitions.
    /// </summary>
    [Serializable]
    internal sealed class ShowStatusPayload
    {
        public int ShowId;
        public string ShowTitle = string.Empty;
        public string PreviousShowStatus = string.Empty;
        public string NewShowStatus = string.Empty;
        public string ShowCastType = string.Empty;
        public int ShowEpisodeCount;
        public int ShowCastCount;
        public long ShowLatestAudience;
        public long ShowLatestRevenue;
        public int ShowLatestNewFans;
        public int ShowLatestBuzz;
    }

    /// <summary>
    /// JSON payload emitted whenever one show episode is completed.
    /// </summary>
    [Serializable]
    internal sealed class ShowEpisodePayload
    {
        public int ShowId;
        public string ShowTitle = string.Empty;
        public string ShowCastType = string.Empty;
        public int ShowEpisodeCount;
        public string ShowEpisodeDate = string.Empty;
        public int ShowCastCount;
        public string ShowCastIdList = string.Empty;
        public long ShowPreviousAudience;
        public long ShowLatestAudience;
        public long ShowAudienceDelta;
        public long ShowPreviousRevenue;
        public long ShowLatestRevenue;
        public long ShowRevenueDelta;
        public long ShowPreviousProfit;
        public long ShowLatestProfit;
        public long ShowProfitDelta;
        public int ShowPreviousNewFans;
        public int ShowLatestNewFans;
        public int ShowNewFansDelta;
        public int ShowPreviousBuzz;
        public int ShowLatestBuzz;
        public int ShowBuzzDelta;
        public float ShowPreviousFatigue;
        public float ShowLatestFatigue;
        public float ShowFatigueDelta;
        public int ShowPreviousFame;
        public int ShowLatestFame;
        public int ShowFameDelta;
        public int ShowPreviousFamePoints;
        public int ShowLatestFamePoints;
        public int ShowFamePointsDelta;
        public int ShowEpisodeBudget;
        public float ShowStaminaCost;
    }

    /// <summary>
    /// JSON payload emitted for show cast-change events.
    /// </summary>
    [Serializable]
    internal sealed class ShowCastChangePayload
    {
        public int ShowId;
        public string ShowTitle = string.Empty;
        public string PreviousShowStatus = string.Empty;
        public string NewShowStatus = string.Empty;
        public string ShowCastTypeBefore = string.Empty;
        public string ShowCastTypeAfter = string.Empty;
        public int ShowCastCountBefore;
        public int ShowCastCountAfter;
        public string ShowCastIdListBefore = string.Empty;
        public string ShowCastIdListAfter = string.Empty;
        public string ShowCastIdListAdded = string.Empty;
        public string ShowCastIdListRemoved = string.Empty;
        public int ShowRemovedIdolId = CoreConstants.InvalidIdValue;
    }

    /// <summary>
    /// JSON payload emitted for show configuration-change events.
    /// </summary>
    [Serializable]
    internal sealed class ShowConfigurationChangePayload
    {
        public int ShowId;
        public string ShowTitleBefore = string.Empty;
        public string ShowTitleAfter = string.Empty;
        public string PreviousShowStatus = string.Empty;
        public string NewShowStatus = string.Empty;
        public string ShowCastTypeBefore = string.Empty;
        public string ShowCastTypeAfter = string.Empty;
        public int ShowCastCountBefore;
        public int ShowCastCountAfter;
        public string ShowCastIdListBefore = string.Empty;
        public string ShowCastIdListAfter = string.Empty;
        public string ShowMcCodeBefore = string.Empty;
        public string ShowMcCodeAfter = string.Empty;
        public string ShowMcTitleBefore = string.Empty;
        public string ShowMcTitleAfter = string.Empty;
        public int ShowProductionCostBefore;
        public int ShowProductionCostAfter;
        public string ShowFanAppealSummaryBefore = string.Empty;
        public string ShowFanAppealSummaryAfter = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for concert lifecycle and participation events.
    /// </summary>
    [Serializable]
    internal sealed class ConcertLifecyclePayload
    {
        public int IdolId = CoreConstants.InvalidIdValue;
        public int ConcertId;
        public string ConcertTitle = string.Empty;
        public string ConcertVenue = string.Empty;
        public string ConcertStatus = string.Empty;
        public int ConcertSongCount;
        public int ConcertParticipantCount;
        public string ConcertParticipantIdList = string.Empty;
        public string ConcertSetlistSummary = string.Empty;
        public long ConcertProjectedAudience;
        public long ConcertActualAudience;
        public long ConcertProjectedRevenue;
        public long ConcertActualRevenue;
        public long ConcertProductionCost;
        public long ConcertProfit;
        public int ConcertHype;
        public int ConcertTicketPrice;
        public string ConcertFinishDate = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for concert status transitions.
    /// </summary>
    [Serializable]
    internal sealed class ConcertStatusPayload
    {
        public int ConcertId;
        public string ConcertTitle = string.Empty;
        public string PreviousConcertStatus = string.Empty;
        public string NewConcertStatus = string.Empty;
        public int ConcertSongCount;
        public int ConcertParticipantCount;
        public string ConcertParticipantIdList = string.Empty;
        public string ConcertSetlistSummary = string.Empty;
        public long ConcertProjectedAudience;
        public long ConcertProjectedRevenue;
        public int ConcertTicketPrice;
    }

    /// <summary>
    /// JSON payload emitted for concert cast-change events.
    /// </summary>
    [Serializable]
    internal sealed class ConcertCastChangePayload
    {
        public int ConcertId;
        public string ConcertTitle = string.Empty;
        public string PreviousConcertStatus = string.Empty;
        public string NewConcertStatus = string.Empty;
        public int ConcertSongCountBefore;
        public int ConcertSongCountAfter;
        public int ConcertParticipantCountBefore;
        public int ConcertParticipantCountAfter;
        public string ConcertParticipantIdListBefore = string.Empty;
        public string ConcertParticipantIdListAfter = string.Empty;
        public string ConcertParticipantIdListAdded = string.Empty;
        public string ConcertParticipantIdListRemoved = string.Empty;
        public string ConcertSetlistSummaryBefore = string.Empty;
        public string ConcertSetlistSummaryAfter = string.Empty;
        public int ConcertRemovedIdolId = CoreConstants.InvalidIdValue;
    }

    /// <summary>
    /// JSON payload emitted for concert configuration-change events.
    /// </summary>
    [Serializable]
    internal sealed class ConcertConfigurationChangePayload
    {
        public int ConcertId;
        public string ConcertTitleBefore = string.Empty;
        public string ConcertTitleAfter = string.Empty;
        public string ConcertRawTitleBefore = string.Empty;
        public string ConcertRawTitleAfter = string.Empty;
        public string ConcertVenueBefore = string.Empty;
        public string ConcertVenueAfter = string.Empty;
        public int ConcertTicketPriceBefore;
        public int ConcertTicketPriceAfter;
        public int ConcertSongCountBefore;
        public int ConcertSongCountAfter;
        public int ConcertParticipantCountBefore;
        public int ConcertParticipantCountAfter;
        public string ConcertParticipantIdListBefore = string.Empty;
        public string ConcertParticipantIdListAfter = string.Empty;
        public string ConcertSetlistSummaryBefore = string.Empty;
        public string ConcertSetlistSummaryAfter = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for award nomination and result events.
    /// </summary>
    [Serializable]
    internal sealed class AwardLifecyclePayload
    {
        public int IdolId = CoreConstants.InvalidIdValue;
        public string AwardType = string.Empty;
        public int AwardYear;
        public bool AwardIsNomination;
        public bool AwardWon;
        public int AwardSingleId = CoreConstants.InvalidIdValue;
    }

    /// <summary>
    /// JSON payload emitted for push lifecycle events.
    /// </summary>
    [Serializable]
    internal sealed class PushLifecyclePayload
    {
        public int IdolId = CoreConstants.InvalidIdValue;
        public int PushSlotIndex;
        public int PushPreviousIdolId = CoreConstants.InvalidIdValue;
        public int PushCurrentIdolId = CoreConstants.InvalidIdValue;
        public int PushDaysInSlot;
        public string PushLifecycleAction = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for idol-idol relationship lifecycle events.
    /// </summary>
    [Serializable]
    internal sealed class IdolRelationshipLifecyclePayload
    {
        public int IdolAId = CoreConstants.InvalidIdValue;
        public int IdolBId = CoreConstants.InvalidIdValue;
        public string RelationshipStatus = string.Empty;
        public string RelationshipDynamic = string.Empty;
        public bool RelationshipKnownToPlayer;
        public string RelationshipPairKey = string.Empty;
        public string RelationshipBreakReason = string.Empty;
        public bool RelationshipIsDating;
    }

    /// <summary>
    /// JSON payload emitted for idol-idol relationship status transition events.
    /// </summary>
    [Serializable]
    internal sealed class IdolRelationshipStatusChangePayload
    {
        public int IdolAId = CoreConstants.InvalidIdValue;
        public int IdolBId = CoreConstants.InvalidIdValue;
        public string RelationshipPreviousStatus = string.Empty;
        public string RelationshipNewStatus = string.Empty;
        public string RelationshipDynamic = string.Empty;
        public bool RelationshipKnownToPlayer;
        public string RelationshipPairKey = string.Empty;
        public bool RelationshipIsDating;
        public float RelationshipRequestedDelta;
        public float RelationshipRatio;
    }

    /// <summary>
    /// JSON payload emitted for clique join/leave lifecycle events.
    /// </summary>
    [Serializable]
    internal sealed class CliqueLifecyclePayload
    {
        public int IdolId = CoreConstants.InvalidIdValue;
        public int CliqueLeaderId = CoreConstants.InvalidIdValue;
        public int CliqueLeaderIdBefore = CoreConstants.InvalidIdValue;
        public int CliqueLeaderIdAfter = CoreConstants.InvalidIdValue;
        public int CliqueMemberCount;
        public string CliqueSignature = string.Empty;
        public bool CliqueQuitWasViolent;
    }

    /// <summary>
    /// JSON payload emitted for bullying start/end events.
    /// </summary>
    [Serializable]
    internal sealed class BullyingLifecyclePayload
    {
        public int IdolId = CoreConstants.InvalidIdValue;
        public int BullyingTargetId = CoreConstants.InvalidIdValue;
        public int BullyingLeaderId = CoreConstants.InvalidIdValue;
        public bool BullyingKnownToPlayer;
        public int CliqueMemberCount;
        public string CliqueSignature = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for scandal mitigation events from the scandal points popup.
    /// </summary>
    [Serializable]
    internal sealed class ScandalMitigationPayload
    {
        public int IdolId = CoreConstants.InvalidIdValue;
        public int ScandalPointsAvailableBefore;
        public int ScandalPointsRemoved;
        public int ScandalPointsBefore;
        public int ScandalPointsAfter;
        public int ScandalGroupPointsRemoved;
        public int ScandalGroupPointsRemaining;
    }

    /// <summary>
    /// JSON payload emitted for player-idol relationship points changes.
    /// </summary>
    [Serializable]
    internal sealed class PlayerRelationshipDeltaPayload
    {
        public int IdolId = CoreConstants.InvalidIdValue;
        public string PlayerRelationshipType = string.Empty;
        public int PlayerPointsRequestedDelta;
        public int PlayerPointsAppliedDelta;
        public int PlayerPointsBefore;
        public int PlayerPointsAfter;
        public int PlayerLevelBefore;
        public int PlayerLevelAfter;
        public bool PlayerLevelChanged;
    }

    /// <summary>
    /// JSON payload emitted for player date interaction captures.
    /// </summary>
    [Serializable]
    internal sealed class PlayerDateInteractionPayload
    {
        public int IdolId = CoreConstants.InvalidIdValue;
        public string DateInteractionType = string.Empty;
        public string DateRouteBefore = string.Empty;
        public string DateRouteAfter = string.Empty;
        public string DateStageBefore = string.Empty;
        public string DateStageAfter = string.Empty;
        public string DateStatusBefore = string.Empty;
        public string DateStatusAfter = string.Empty;
        public string DateResultToken = string.Empty;
        public string DateResultSummaryCode = string.Empty;
        public bool DateCaughtBefore;
        public bool DateCaughtAfter;
        public int DateRelationshipLevelBefore;
        public int DateRelationshipLevelAfter;
    }

    /// <summary>
    /// JSON payload emitted for player marriage outcome events.
    /// </summary>
    [Serializable]
    internal sealed class PlayerMarriageOutcomePayload
    {
        public int IdolId = CoreConstants.InvalidIdValue;
        public string MarriageStage = string.Empty;
        public string MarriageRoute = string.Empty;
        public string MarriagePartnerStatus = string.Empty;
        public bool MarriageGoodOutcome;
        public bool MarriageGirlQuitTriggered;
        public string MarriageKidsString = string.Empty;
        public string MarriageCareerStringOne = string.Empty;
        public string MarriageCareerStringTwo = string.Empty;
        public string MarriageRelationshipOutcomeString = string.Empty;
        public string MarriageCustodyString = string.Empty;
        public string MarriageGraduationTrivia = string.Empty;
        public string MarriageIdolStatus = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for world-tour lifecycle create/start/finish events.
    /// </summary>
    [Serializable]
    internal sealed class TourLifecyclePayload
    {
        public int TourId;
        public string TourLifecycleAction = string.Empty;
        public string TourStatus = string.Empty;
        public int SelectedCountryCount;
        public string TourSelectedCountryLevelList = string.Empty;
        public int TourParticipantCount;
        public string TourParticipantIdList = string.Empty;
        public long TourTotalAudience;
        public long TourTotalRevenue;
        public long TourTotalNewFans;
        public int TourProductionCost;
        public int TourExpectedRevenue;
        public int TourSaving;
        public int TourStaminaCost;
        public int TourProfit;
        public string TourStartDate = string.Empty;
        public string TourFinishDate = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for one world-tour country result row.
    /// </summary>
    [Serializable]
    internal sealed class TourCountryResultPayload
    {
        public int TourId;
        public string TourStatus = string.Empty;
        public string TourFinishDate = string.Empty;
        public string TourCountryCode = string.Empty;
        public int TourCountryLevel;
        public int TourCountryAttendance;
        public int TourCountryAudience;
        public int TourCountryNewFans;
        public int TourCountryRevenue;
        public bool TourCountryDiscount;
    }

    /// <summary>
    /// JSON payload emitted for one world-tour idol participation row.
    /// </summary>
    [Serializable]
    internal sealed class TourParticipationPayload
    {
        public int TourId;
        public int IdolId;
        public int TourParticipantCount;
        public string TourParticipantIdList = string.Empty;
        public string TourLifecycleAction = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for tour status transitions.
    /// </summary>
    [Serializable]
    internal sealed class TourStatusPayload
    {
        public int TourId;
        public string PreviousTourStatus = string.Empty;
        public string NewTourStatus = string.Empty;
        public int SelectedCountryCount;
        public long TourTotalAudience;
        public long TourTotalRevenue;
        public long TourTotalNewFans;
        public int TourProductionCost;
        public int TourExpectedRevenue;
        public int TourSaving;
        public string TourFinishDate = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for election lifecycle creation/finish events.
    /// </summary>
    [Serializable]
    internal sealed class ElectionLifecyclePayload
    {
        public int ElectionId;
        public string ElectionLifecycleAction = string.Empty;
        public string ElectionStatus = string.Empty;
        public string ElectionBroadcastType = string.Empty;
        public int ElectionSingleId = CoreConstants.InvalidIdValue;
        public int ElectionConcertId = CoreConstants.InvalidIdValue;
        public int ElectionReleaseSingleId = CoreConstants.InvalidIdValue;
        public int ElectionResultCount;
        public string ElectionFinishDate = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for generated election ranks per idol.
    /// </summary>
    [Serializable]
    internal sealed class ElectionGeneratedResultPayload
    {
        public int ElectionId;
        public int IdolId;
        public int ElectionExpectedPlace;
        public int ElectionGeneratedPlace = CoreConstants.InvalidIdValue;
        public long ElectionGeneratedVotes;
        public int ElectionGeneratedFamePoints;
        public string ElectionBroadcastType = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for one manual election place adjustment.
    /// </summary>
    [Serializable]
    internal sealed class ElectionPlaceAdjustedPayload
    {
        public int ElectionId;
        public int IdolId;
        public int ElectionPlaceBefore = CoreConstants.InvalidIdValue;
        public int ElectionPlaceAfter = CoreConstants.InvalidIdValue;
        public string ElectionBroadcastType = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for election status transitions.
    /// </summary>
    [Serializable]
    internal sealed class ElectionStatusPayload
    {
        public int ElectionId;
        public string PreviousElectionStatus = string.Empty;
        public string NewElectionStatus = string.Empty;
        public string ElectionBroadcastType = string.Empty;
        public int ElectionSingleId = CoreConstants.InvalidIdValue;
        public int ElectionConcertId = CoreConstants.InvalidIdValue;
        public int ElectionReleaseSingleId = CoreConstants.InvalidIdValue;
        public int ElectionResultCount;
        public string ElectionFinishDate = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for one idol election result row.
    /// </summary>
    [Serializable]
    internal sealed class ElectionResultPayload
    {
        public int ElectionId;
        public int IdolId;
        public int ElectionPlace;
        public long ElectionVotes;
        public int ElectionFamePoints;
        public string ElectionBroadcastType = string.Empty;
        public string ElectionFinishDate = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for scandal-point mutations.
    /// </summary>
    [Serializable]
    internal sealed class ScandalPointsPayload
    {
        public int IdolId;
        public int PreviousScandalPoints;
        public int NewScandalPoints;
        public int ScandalPointDelta;
        public float PreviousScandalPointsRaw;
        public float NewScandalPointsRaw;
        public float ScandalPointDeltaRaw;
        public string ScandalMutationSource = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for medical lifecycle events.
    /// </summary>
    [Serializable]
    internal sealed class MedicalEventPayload
    {
        public int IdolId;
        public string MedicalEventType = string.Empty;
        public string MedicalPreviousStatus = string.Empty;
        public string MedicalCurrentStatus = string.Empty;
        public string MedicalHiatusEndDate = string.Empty;
        public bool MedicalFinishWasForced;
        public int MedicalInjuryCounter;
        public int MedicalDepressionCounter;
    }

    /// <summary>
    /// JSON payload emitted for debt-loan lifecycle events.
    /// </summary>
    [Serializable]
    internal sealed class LoanLifecycleEventPayload
    {
        public int loan_id = CoreConstants.InvalidIdValue;
        public string loan_lifecycle_action = string.Empty;
        public string loan_type = string.Empty;
        public string loan_duration = string.Empty;
        public bool loan_active_before;
        public bool loan_active_after;
        public long loan_amount;
        public int loan_payment_per_week;
        public int loan_interest_rate;
        public string loan_start_date = string.Empty;
        public string loan_end_date = string.Empty;
        public long loan_debt_before;
        public long loan_debt_after;
        public bool loan_can_pay_off_after;
        public bool loan_in_development_after;
        public int loan_days_to_develop;
        public int loan_count_active;
        public int loan_count_total;
        public int loan_total_payment_per_week;
        public long loan_total_debt;
        public long money_before;
        public long money_after;
        public long money_delta;
    }

    /// <summary>
    /// JSON payload emitted when bankruptcy danger state changes.
    /// </summary>
    [Serializable]
    internal sealed class BankruptcyDangerEventPayload
    {
        public bool requested_value;
        public bool bankruptcy_danger_before;
        public bool bankruptcy_danger_after;
        public string bankruptcy_date_before = string.Empty;
        public string bankruptcy_date_after = string.Empty;
        public int bankruptcy_days_remaining_after;
        public long money_after;
        public long total_debt_after;
    }

    /// <summary>
    /// JSON payload emitted when bankruptcy checks trigger a dialogue or fail state.
    /// </summary>
    [Serializable]
    internal sealed class BankruptcyCheckEventPayload
    {
        public bool bankruptcy_danger_before;
        public bool bankruptcy_danger_after;
        public int bankruptcy_days_remaining_before;
        public int bankruptcy_days_remaining_after;
        public long money_before;
        public long money_after;
        public long total_debt_before;
        public long total_debt_after;
        public bool bailout_used_before;
        public bool bailout_used_after;
        public bool story_recruit_used_before;
        public bool story_recruit_used_after;
        public bool game_over_bankruptcy_used_before;
        public bool game_over_bankruptcy_used_after;
        public string triggered_dialogue = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted when scandal checks trigger a dialogue or hard fail.
    /// </summary>
    [Serializable]
    internal sealed class ScandalCheckEventPayload
    {
        public bool test_go;
        public long scandal_points_total_before;
        public long scandal_points_total_after;
        public int scandal_threshold;
        public bool first_scandal_used_before;
        public bool first_scandal_used_after;
        public bool warning_used_before;
        public bool warning_used_after;
        public bool parents_used_before;
        public bool parents_used_after;
        public bool game_over_used_before;
        public bool game_over_used_after;
        public bool scandal_parent_cooldown_before;
        public bool scandal_parent_cooldown_after;
        public bool audition_failure_before;
        public bool audition_failure_after;
        public int active_idol_count_before;
        public int active_idol_count_after;
        public string triggered_dialogue = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted when audition failure resolves into idol wipeout.
    /// </summary>
    [Serializable]
    internal sealed class AuditionFailureEventPayload
    {
        public bool audition_failure_before;
        public bool audition_failure_after;
        public int active_idol_count_before;
        public int active_idol_count_after;
    }

    /// <summary>
    /// JSON payload emitted for policy decision selections.
    /// </summary>
    [Serializable]
    internal sealed class PolicyDecisionEventPayload
    {
        public string policy_type = string.Empty;
        public string previous_value = string.Empty;
        public string new_value = string.Empty;
        public int policy_cost;
        public bool free_selection;
        public string decision_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for daily activity action outcomes.
    /// </summary>
    [Serializable]
    internal sealed class ActivityActionEventPayload
    {
        public string activity_type = string.Empty;
        public int activity_level;
        public float activity_bonus;
        public long money_before;
        public long money_after;
        public long money_delta;
        public long fans_before;
        public long fans_after;
        public long fans_delta;
        public int active_idol_count_before;
        public int active_idol_count_after;
        public int per_idol_earnings;
        public float stamina_cost;
        public int spa_heal;
        public int spa_cost;
        public string activity_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for per-idol earnings ledger changes.
    /// </summary>
    [Serializable]
    internal sealed class IdolEarningsEventPayload
    {
        public int idol_id = CoreConstants.InvalidIdValue;
        public long earnings_delta;
        public long earnings_current_month_before;
        public long earnings_current_month_after;
        public string earnings_source = string.Empty;
        public string earnings_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for theater create/destroy lifecycle events.
    /// </summary>
    [Serializable]
    internal sealed class TheaterLifecycleEventPayload
    {
        public int theater_id = CoreConstants.InvalidIdValue;
        public string theater_lifecycle_action = string.Empty;
        public string theater_title = string.Empty;
        public int group_id = CoreConstants.InvalidIdValue;
        public int room_theater_id = CoreConstants.InvalidIdValue;
        public int ticket_price;
        public int subscription_price;
        public bool streaming_researched;
        public bool equipment_purchased;
        public long subscriber_total;
        public string schedule_summary = string.Empty;
        public string lifecycle_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for each theater daily completion result row.
    /// </summary>
    [Serializable]
    internal sealed class TheaterDailyResultEventPayload
    {
        public int theater_id = CoreConstants.InvalidIdValue;
        public string theater_title = string.Empty;
        public int group_id = CoreConstants.InvalidIdValue;
        public string result_date = string.Empty;
        public string schedule_type = string.Empty;
        public bool schedule_fan_type_everyone;
        public string schedule_fan_type = string.Empty;
        public int attendance;
        public long revenue;
        public int subscribers_delta;
        public long subscribers_total;
        public int avg_attendance_7d;
        public int avg_revenue_7d;
        public string weekly_schedule_summary = string.Empty;
        public long total_money_before;
        public long total_money_after;
        public long total_money_delta;
    }

    /// <summary>
    /// JSON payload emitted for cafe create/destroy lifecycle events.
    /// </summary>
    [Serializable]
    internal sealed class CafeLifecycleEventPayload
    {
        public int cafe_id = CoreConstants.InvalidIdValue;
        public string cafe_lifecycle_action = string.Empty;
        public string cafe_title = string.Empty;
        public int group_id = CoreConstants.InvalidIdValue;
        public int room_theater_id = CoreConstants.InvalidIdValue;
        public int wait_staff_count;
        public int working_staff_count;
        public string cafe_priority = string.Empty;
        public string staff_priority = string.Empty;
        public string menu_summary = string.Empty;
        public string lifecycle_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for cafe daily render results.
    /// </summary>
    [Serializable]
    internal sealed class CafeDailyResultEventPayload
    {
        public int cafe_id = CoreConstants.InvalidIdValue;
        public string cafe_title = string.Empty;
        public int group_id = CoreConstants.InvalidIdValue;
        public int dish_id = CoreConstants.InvalidIdValue;
        public string dish_type = string.Empty;
        public string dish_title = string.Empty;
        public int profit;
        public int new_fans;
        public string fan_type = string.Empty;
        public int staffed_idol_count;
        public string staffed_idol_id_list = string.Empty;
        public string result_date = string.Empty;
        public long total_money_before;
        public long total_money_after;
        public long total_money_delta;
    }

    /// <summary>
    /// JSON payload emitted for staff hire/fire/level-up lifecycle events.
    /// </summary>
    [Serializable]
    internal sealed class StaffLifecycleEventPayload
    {
        public int staff_id = CoreConstants.InvalidIdValue;
        public string staff_name = string.Empty;
        public string staff_type = string.Empty;
        public string staff_unique_type = string.Empty;
        public string staff_action = string.Empty;
        public int staff_salary;
        public int staff_level_before;
        public int staff_level_after;
        public bool fire_forced;
        public bool can_fire_before;
        public bool can_fire_severance_before;
        public int fire_points_cost;
        public int severance_cost;
        public long scandal_points_before;
        public long scandal_points_after;
        public long money_before;
        public long money_after;
        public string room_type = string.Empty;
        public string hire_date = string.Empty;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for research category parameter assignment intent changes.
    /// </summary>
    [Serializable]
    internal sealed class ResearchParamAssignmentEventPayload
    {
        public string research_type = string.Empty;
        public string previous_param_type = string.Empty;
        public int previous_param_id = CoreConstants.InvalidIdValue;
        public string previous_param_title = string.Empty;
        public string new_param_type = string.Empty;
        public int new_param_id = CoreConstants.InvalidIdValue;
        public string new_param_title = string.Empty;
        public long research_points_before;
        public long research_points_after;
        public bool has_param_after;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted when research points are purchased.
    /// </summary>
    [Serializable]
    internal sealed class ResearchPointsPurchaseEventPayload
    {
        public string research_type = string.Empty;
        public long points_before;
        public long points_after;
        public float points_raw_before;
        public float points_raw_after;
        public long purchase_cost_before;
        public long purchase_cost_after;
        public long money_before;
        public long money_after;
        public long money_delta;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for passive/active research point accrual ticks.
    /// </summary>
    [Serializable]
    internal sealed class ResearchPointsAccruedEventPayload
    {
        public string research_type = string.Empty;
        public float points_raw_before;
        public float points_raw_after;
        public float points_raw_delta;
        public long points_before;
        public long points_after;
        public long points_delta;
        public float points_requested_delta;
        public string active_param_type = string.Empty;
        public int active_param_id = CoreConstants.InvalidIdValue;
        public string active_param_title = string.Empty;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for single parameter research level-up progression.
    /// </summary>
    [Serializable]
    internal sealed class ResearchParamLevelUpEventPayload
    {
        public string research_type = string.Empty;
        public string param_type = string.Empty;
        public int param_id = CoreConstants.InvalidIdValue;
        public string param_title = string.Empty;
        public int level_before;
        public int level_after;
        public long points_needed_before;
        public long saved_points_before;
        public long saved_points_after;
        public float param_exp_before;
        public float param_exp_after;
        public float category_points_raw_before;
        public float category_points_raw_after;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted when one story route lock is applied.
    /// </summary>
    [Serializable]
    internal sealed class StoryRouteLockEventPayload
    {
        public string route_before = string.Empty;
        public string route_after = string.Empty;
        public int active_task_count_before;
        public int active_task_count_after;
        public int removed_task_count;
        public string removed_task_custom_list = string.Empty;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for task completion/failure/done lifecycle outcomes.
    /// </summary>
    [Serializable]
    internal sealed class TaskLifecycleEventPayload
    {
        public string task_action = string.Empty;
        public string task_custom = string.Empty;
        public string task_type = string.Empty;
        public string task_goal = string.Empty;
        public bool task_substory;
        public int task_girl_id = CoreConstants.InvalidIdValue;
        public string task_skill = string.Empty;
        public string task_agent_name = string.Empty;
        public bool fulfilled_before;
        public bool fulfilled_after;
        public bool active_before;
        public bool active_after;
        public bool skip_lock;
        public string route = string.Empty;
        public string available_from = string.Empty;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for concert crisis decision outcomes.
    /// </summary>
    [Serializable]
    internal sealed class ConcertCrisisDecisionEventPayload
    {
        public int concert_id = CoreConstants.InvalidIdValue;
        public string accident_title = string.Empty;
        public bool choice_safe;
        public string choice_type = string.Empty;
        public string result_type = string.Empty;
        public int result_hype_delta;
        public int accident_success_chance;
        public bool no_critical_failure;
        public int used_accident_count;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted when concert crisis outcomes are applied to hype.
    /// </summary>
    [Serializable]
    internal sealed class ConcertCrisisAppliedEventPayload
    {
        public int concert_id = CoreConstants.InvalidIdValue;
        public string accident_title = string.Empty;
        public bool choice_safe;
        public string choice_type = string.Empty;
        public string result_type = string.Empty;
        public int expected_hype_delta;
        public float hype_before;
        public float hype_after;
        public float hype_delta_applied;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for concert tactical card usage.
    /// </summary>
    [Serializable]
    internal sealed class ConcertCardUsedEventPayload
    {
        public int concert_id = CoreConstants.InvalidIdValue;
        public string card_type = string.Empty;
        public int card_level;
        public int card_effect_value;
        public int cards_before;
        public int cards_after;
        public bool card_consumed;
        public float card_accident_happening_before;
        public float card_accident_happening_after;
        public float card_accident_success_before;
        public float card_accident_success_after;
        public bool card_no_critical_failure_before;
        public bool card_no_critical_failure_after;
        public int no_accident_counter_before;
        public int no_accident_counter_after;
        public int used_accident_count_before;
        public int used_accident_count_after;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for final concert outcome economics and sentiment shifts.
    /// </summary>
    [Serializable]
    internal sealed class ConcertFinalResolvedEventPayload
    {
        public int concert_id = CoreConstants.InvalidIdValue;
        public long actual_revenue;
        public long actual_profit;
        public int ticket_price;
        public long money_before;
        public long money_after;
        public long money_delta;
        public int idol_payout_count;
        public long idol_payout_total;
        public string idol_payout_by_id = string.Empty;
        public string fan_opinion_shift = string.Empty;
        public int used_accident_count;
        public string used_accident_titles = string.Empty;
        public int no_accident_counter;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted when an election is explicitly started.
    /// </summary>
    [Serializable]
    internal sealed class ElectionStartedEventPayload
    {
        public int election_id = CoreConstants.InvalidIdValue;
        public string election_lifecycle_action = string.Empty;
        public string election_status = string.Empty;
        public string election_broadcast_type = string.Empty;
        public int election_single_id = CoreConstants.InvalidIdValue;
        public int election_concert_id = CoreConstants.InvalidIdValue;
        public int election_release_single_id = CoreConstants.InvalidIdValue;
        public int election_result_count;
        public string election_finish_date = string.Empty;
        public long start_cost;
        public long money_before;
        public long money_after;
        public long money_delta;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for idol wish lifecycle events.
    /// </summary>
    [Serializable]
    internal sealed class WishLifecycleEventPayload
    {
        public int idol_id = CoreConstants.InvalidIdValue;
        public string wish_action = string.Empty;
        public string wish_type_before = string.Empty;
        public string wish_type_after = string.Empty;
        public string wish_formula_before = string.Empty;
        public string wish_formula_after = string.Empty;
        public bool wish_fulfilled_before;
        public bool wish_fulfilled_after;
        public string wish_effect_until_before = string.Empty;
        public string wish_effect_until_after = string.Empty;
        public int influence_points_before;
        public int influence_points_after;
        public int influence_points_delta;
        public int influence_level_before;
        public int influence_level_after;
        public float mental_stamina_before;
        public float mental_stamina_after;
        public float mental_stamina_delta;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for agency room lifecycle events.
    /// </summary>
    [Serializable]
    internal sealed class AgencyRoomLifecycleEventPayload
    {
        public int room_id = CoreConstants.InvalidIdValue;
        public string room_type = string.Empty;
        public int floor_id = CoreConstants.InvalidIdValue;
        public int floor_index = CoreConstants.InvalidIdValue;
        public int room_space;
        public int room_cost;
        public bool build_flag;
        public string room_lifecycle_action = string.Empty;
        public long money_before;
        public long money_after;
        public long money_delta;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted when room build cost is paid.
    /// </summary>
    [Serializable]
    internal sealed class AgencyRoomCostPaidEventPayload
    {
        public int room_id = CoreConstants.InvalidIdValue;
        public string room_type = string.Empty;
        public int room_cost;
        public bool build_flag;
        public long money_before;
        public long money_after;
        public long money_delta;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for audition-start lifecycle events.
    /// </summary>
    [Serializable]
    internal sealed class AuditionStartedEventPayload
    {
        public string audition_type = string.Empty;
        public bool should_pay;
        public int cost;
        public float progress_before;
        public float progress_after;
        public int generated_candidate_count;
        public string regional_cooldown_before = string.Empty;
        public string regional_cooldown_after = string.Empty;
        public string nationwide_cooldown_before = string.Empty;
        public string nationwide_cooldown_after = string.Empty;
        public long money_before;
        public long money_after;
        public long money_delta;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted when audition cost is paid.
    /// </summary>
    [Serializable]
    internal sealed class AuditionCostPaidEventPayload
    {
        public string audition_type = string.Empty;
        public string spend_reason = string.Empty;
        public int cost;
        public long money_before;
        public long money_after;
        public long money_delta;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted when audition cooldown reset is requested.
    /// </summary>
    [Serializable]
    internal sealed class AuditionCooldownResetEventPayload
    {
        public string reset_type = string.Empty;
        public bool can_reset_before;
        public int reset_cost;
        public int regional_days_remaining_before;
        public int nationwide_days_remaining_before;
        public string regional_cooldown_before = string.Empty;
        public string regional_cooldown_after = string.Empty;
        public string nationwide_cooldown_before = string.Empty;
        public string nationwide_cooldown_after = string.Empty;
        public long money_before;
        public long money_after;
        public long money_delta;
        public long research_player_points_before;
        public long research_player_points_after;
        public long research_player_points_delta;
        public bool reset_applied;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted when a random event is scheduled/started.
    /// </summary>
    [Serializable]
    internal sealed class RandomEventStartedEventPayload
    {
        public string random_event_id = string.Empty;
        public string random_event_title = string.Empty;
        public string random_event_state = string.Empty;
        public bool random_event_force;
        public string random_event_mod_name = string.Empty;
        public string scheduled_date = string.Empty;
        public int actor_count;
        public string actors_summary = string.Empty;
        public int requirement_count;
        public int startup_effect_count;
        public long money_before;
        public long money_after;
        public long money_delta;
        public long fans_before;
        public long fans_after;
        public long fans_delta;
        public int fame_before;
        public int fame_after;
        public int fame_delta;
        public int buzz_before;
        public int buzz_after;
        public int buzz_delta;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted when a random-event reply is resolved.
    /// </summary>
    [Serializable]
    internal sealed class RandomEventReplyEffectEntry
    {
        public string target = string.Empty;
        public string parameter = string.Empty;
        public string formula = string.Empty;
        public string special = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted when a random-event reply is resolved.
    /// </summary>
    [Serializable]
    internal sealed class RandomEventConcludedEventPayload
    {
        public string random_event_id = string.Empty;
        public string random_event_title = string.Empty;
        public string random_event_state_before = string.Empty;
        public string random_event_state_after = string.Empty;
        public int reply_index = CoreConstants.InvalidIdValue;
        public string reply_text = string.Empty;
        public string reply_description = string.Empty;
        public int reply_effect_count;
        public string reply_effect_summary = string.Empty;
        public RandomEventReplyEffectEntry[] reply_effect_entries;
        public string actors_summary = string.Empty;
        public long estimated_liability;
        public long money_before;
        public long money_after;
        public long money_delta;
        public long fans_before;
        public long fans_after;
        public long fans_delta;
        public int fame_before;
        public int fame_after;
        public int fame_delta;
        public int buzz_before;
        public int buzz_after;
        public int buzz_delta;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for substory queue/lifecycle state transitions.
    /// </summary>
    [Serializable]
    internal sealed class SubstoryLifecycleEventPayload
    {
        public string substory_id = string.Empty;
        public string substory_parent_id = string.Empty;
        public string substory_type = string.Empty;
        public string substory_lifecycle_action = string.Empty;
        public bool debug_mode;
        public bool had_before_start_callback;
        public bool used_before;
        public bool used_after;
        public int queue_count_before;
        public int queue_count_after;
        public int delayed_queue_count_before;
        public int delayed_queue_count_after;
        public string scheduled_launch_time = string.Empty;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for weekly passive-expense application.
    /// </summary>
    [Serializable]
    internal sealed class EconomyWeeklyExpenseEventPayload
    {
        public long weekly_expense;
        public long money_before;
        public long money_after;
        public long money_delta;
        public long fans_total;
        public int fame_points;
        public int buzz_points;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for daily passive economy ticks.
    /// </summary>
    [Serializable]
    internal sealed class EconomyDailyTickEventPayload
    {
        public long money_before;
        public long money_after;
        public long money_delta;
        public long expected_daily_profit;
        public int buzz_before;
        public int buzz_after;
        public int buzz_delta;
        public int expected_daily_buzz_gain;
        public int fame_before;
        public int fame_after;
        public int fame_delta;
        public int expected_daily_fame_gain;
        public long fans_before;
        public long fans_after;
        public long fans_delta;
        public long fans_change_counter;
        public int fame_level_before;
        public int fame_level_after;
        public float fame_progress_before;
        public float fame_progress_after;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for influence blackmail queue/trigger lifecycle.
    /// </summary>
    [Serializable]
    internal sealed class InfluenceBlackmailEventPayload
    {
        public int spy_id = CoreConstants.InvalidIdValue;
        public int target_id = CoreConstants.InvalidIdValue;
        public string influence_action = string.Empty;
        public string report_date = string.Empty;
        public int days_until_report;
        public int queue_size_after;
        public int success_tier = CoreConstants.InvalidIdValue;
        public int influence_award;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for mentor system lifecycle and weekly ticks.
    /// </summary>
    [Serializable]
    internal sealed class MentorshipLifecycleEventPayload
    {
        public string mentorship_action = string.Empty;
        public int mentor_id = CoreConstants.InvalidIdValue;
        public int kohai_id = CoreConstants.InvalidIdValue;
        public int active_mentor_count;
        public float senpai_to_kohai_ratio_before;
        public float senpai_to_kohai_ratio_after;
        public float kohai_to_senpai_ratio_before;
        public float kohai_to_senpai_ratio_after;
        public string mentor_pairs_summary = string.Empty;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted for rival-market trend and monthly recalculation context.
    /// </summary>
    [Serializable]
    internal sealed class RivalMarketEventPayload
    {
        public string rival_action = string.Empty;
        public int month_index_before;
        public int month_index_after;
        public int active_group_count_before;
        public int active_group_count_after;
        public int rising_group_count_before;
        public int rising_group_count_after;
        public int dead_group_count_before;
        public int dead_group_count_after;
        public long office_research_points_before;
        public long office_research_points_after;
        public long office_research_points_delta;
        public int trend_update_cost;
        public string trends_genre_summary = string.Empty;
        public string trends_lyrics_summary = string.Empty;
        public string trends_choreo_summary = string.Empty;
        public string trend_last_updated_before = string.Empty;
        public string trend_last_updated_after = string.Empty;
        public bool show_popup;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// JSON payload emitted when summer games are finalized.
    /// </summary>
    [Serializable]
    internal sealed class SummerGamesFinalizedEventPayload
    {
        public int selected_single_id = CoreConstants.InvalidIdValue;
        public int genre_id = CoreConstants.InvalidIdValue;
        public int lyrics_id = CoreConstants.InvalidIdValue;
        public int choreography_id = CoreConstants.InvalidIdValue;
        public int genre_cost;
        public int lyrics_cost;
        public int choreography_cost;
        public int total_cost;
        public bool was_finalized_before;
        public bool is_finalized_after;
        public long vocal_points_before;
        public long vocal_points_after;
        public long vocal_points_delta;
        public long player_points_before;
        public long player_points_after;
        public long player_points_delta;
        public long dance_points_before;
        public long dance_points_after;
        public long dance_points_delta;
        public int happy_member_count;
        public string event_date = string.Empty;
    }

    /// <summary>
    /// Internal queued event representation before storage commit.
    /// </summary>
    internal sealed class PendingEvent
    {
        internal string SaveKey = string.Empty;
        internal int GameDateKey;
        internal string GameDateTime = string.Empty;
        internal int IdolId;
        internal string EntityKind = string.Empty;
        internal string EntityId = string.Empty;
        internal string EventType = string.Empty;
        internal string SourcePatch = string.Empty;
        internal string NamespaceIdentifier = string.Empty;
        internal string PayloadJson = string.Empty;
    }

    /// <summary>
    /// Projection row for single participation read-model updates.
    /// </summary>
    internal sealed class SingleParticipationProjection
    {
        internal string SaveKey = string.Empty;
        internal int SingleId;
        internal int IdolId;
        internal int RowIndex;
        internal int PositionIndex;
        internal int IsCenterFlag;
        internal string ReleaseDate = string.Empty;
    }

    /// <summary>
    /// Projection command for opening/closing status windows.
    /// </summary>
    internal sealed class StatusTransitionProjection
    {
        internal string SaveKey = string.Empty;
        internal int IdolId;
        internal string PreviousStatusCode = string.Empty;
        internal string NewStatusCode = string.Empty;
        internal string TransitionDate = string.Empty;
    }

    /// <summary>
    /// Mutation command for show-cast window projection updates.
    /// </summary>
    internal sealed class ShowCastWindowProjectionMutation
    {
        internal string SaveKey = string.Empty;
        internal string ShowId = string.Empty;
        internal int IdolId = CoreConstants.InvalidIdValue;
        internal string StartDate = string.Empty;
        internal string EndDate = string.Empty;
        internal string EndReason = string.Empty;
        internal string PayloadJson = CoreConstants.EmptyJsonObject;
        internal bool OpenWindow;
    }

    /// <summary>
    /// Mutation command for contract-window projection updates.
    /// </summary>
    internal sealed class ContractWindowProjectionMutation
    {
        internal string SaveKey = string.Empty;
        internal string ContractKey = string.Empty;
        internal int IdolId = CoreConstants.InvalidIdValue;
        internal string StartDate = string.Empty;
        internal string EndDate = string.Empty;
        internal string EndReason = string.Empty;
        internal string PayloadJson = CoreConstants.EmptyJsonObject;
        internal bool OpenWindow;
    }

    /// <summary>
    /// Mutation command for relationship-window projection updates.
    /// </summary>
    internal sealed class RelationshipWindowProjectionMutation
    {
        internal string SaveKey = string.Empty;
        internal string RelationshipKey = string.Empty;
        internal int IdolId = CoreConstants.InvalidIdValue;
        internal string RelationshipType = string.Empty;
        internal string StartDate = string.Empty;
        internal string EndDate = string.Empty;
        internal string EndReason = string.Empty;
        internal string PayloadJson = CoreConstants.EmptyJsonObject;
        internal bool OpenWindow;
    }

    /// <summary>
    /// Upsert row for tour participation projection.
    /// </summary>
    internal sealed class TourParticipationProjectionRow
    {
        internal string SaveKey = string.Empty;
        internal string TourId = string.Empty;
        internal int IdolId = CoreConstants.InvalidIdValue;
        internal string LifecycleAction = string.Empty;
        internal string EventDate = string.Empty;
        internal string PayloadJson = CoreConstants.EmptyJsonObject;
    }

    /// <summary>
    /// Upsert row for award-result projection.
    /// </summary>
    internal sealed class AwardResultProjectionRow
    {
        internal string SaveKey = string.Empty;
        internal string AwardKey = string.Empty;
        internal int IdolId = CoreConstants.InvalidIdValue;
        internal string EventDate = string.Empty;
        internal string PayloadJson = CoreConstants.EmptyJsonObject;
    }

    /// <summary>
    /// Upsert row for election-result projection.
    /// </summary>
    internal sealed class ElectionResultProjectionRow
    {
        internal string SaveKey = string.Empty;
        internal string ElectionId = string.Empty;
        internal int IdolId = CoreConstants.InvalidIdValue;
        internal string EventDate = string.Empty;
        internal string PayloadJson = CoreConstants.EmptyJsonObject;
    }

    /// <summary>
    /// Mutation command for push-window projection updates.
    /// </summary>
    internal sealed class PushWindowProjectionMutation
    {
        internal string SaveKey = string.Empty;
        internal string SlotKey = string.Empty;
        internal int IdolId = CoreConstants.InvalidIdValue;
        internal string StartDate = string.Empty;
        internal string EndDate = string.Empty;
        internal string EndReason = string.Empty;
        internal string PayloadJson = CoreConstants.EmptyJsonObject;
        internal int PushDaysInSlot = CoreConstants.ProjectionUnknownDayCount;
        internal bool OpenWindow;
        internal bool CloseWindow;
    }

    /// <summary>
    /// Converts canonical event rows into projection-mutation batches.
    /// </summary>
    internal static class CoreProjectionDerivation
    {
        internal static void DeriveFromEvents(
            IReadOnlyList<PendingEvent> pendingEvents,
            out List<ShowCastWindowProjectionMutation> showCastMutations,
            out List<ContractWindowProjectionMutation> contractMutations,
            out List<RelationshipWindowProjectionMutation> relationshipMutations,
            out List<TourParticipationProjectionRow> tourParticipationRows,
            out List<AwardResultProjectionRow> awardResultRows,
            out List<ElectionResultProjectionRow> electionResultRows,
            out List<PushWindowProjectionMutation> pushMutations)
        {
            showCastMutations = new List<ShowCastWindowProjectionMutation>();
            contractMutations = new List<ContractWindowProjectionMutation>();
            relationshipMutations = new List<RelationshipWindowProjectionMutation>();
            tourParticipationRows = new List<TourParticipationProjectionRow>();
            awardResultRows = new List<AwardResultProjectionRow>();
            electionResultRows = new List<ElectionResultProjectionRow>();
            pushMutations = new List<PushWindowProjectionMutation>();

            if (pendingEvents == null || pendingEvents.Count < CoreConstants.MinimumQueueSizeForFlush)
            {
                return;
            }

            for (int eventIndex = CoreConstants.ZeroBasedListStartIndex; eventIndex < pendingEvents.Count; eventIndex++)
            {
                PendingEvent pendingEvent = pendingEvents[eventIndex];
                if (pendingEvent == null || string.IsNullOrEmpty(pendingEvent.EventType))
                {
                    continue;
                }

                if (pendingEvent.IdolId < CoreConstants.MinimumValidIdolIdentifier && !string.Equals(pendingEvent.EventType, CoreConstants.EventTypeAwardResult, StringComparison.Ordinal))
                {
                    continue;
                }

                string eventType = pendingEvent.EventType;
                string saveKey = pendingEvent.SaveKey ?? string.Empty;
                string entityId = pendingEvent.EntityId ?? string.Empty;
                string gameDateTime = pendingEvent.GameDateTime ?? string.Empty;
                string payloadJson = pendingEvent.PayloadJson ?? CoreConstants.EmptyJsonObject;

                if (pendingEvent.IdolId >= CoreConstants.MinimumValidIdolIdentifier
                    && string.Equals(pendingEvent.EntityKind, CoreConstants.EventEntityKindShow, StringComparison.Ordinal)
                    && !string.IsNullOrEmpty(entityId))
                {
                    if (string.Equals(eventType, CoreConstants.EventTypeShowReleased, StringComparison.Ordinal))
                    {
                        showCastMutations.Add(
                            new ShowCastWindowProjectionMutation
                            {
                                SaveKey = saveKey,
                                ShowId = entityId,
                                IdolId = pendingEvent.IdolId,
                                StartDate = gameDateTime,
                                PayloadJson = payloadJson,
                                OpenWindow = true
                            });
                    }
                    else if (string.Equals(eventType, CoreConstants.EventTypeShowCancelled, StringComparison.Ordinal))
                    {
                        showCastMutations.Add(
                            new ShowCastWindowProjectionMutation
                            {
                                SaveKey = saveKey,
                                ShowId = entityId,
                                IdolId = pendingEvent.IdolId,
                                EndDate = gameDateTime,
                                EndReason = eventType,
                                PayloadJson = payloadJson,
                                OpenWindow = false
                            });
                    }
                }

                if (pendingEvent.IdolId >= CoreConstants.MinimumValidIdolIdentifier
                    && string.Equals(pendingEvent.EntityKind, CoreConstants.EventEntityKindContract, StringComparison.Ordinal)
                    && !string.IsNullOrEmpty(entityId))
                {
                    bool isContractOpen =
                        string.Equals(eventType, CoreConstants.EventTypeContractAccepted, StringComparison.Ordinal)
                        || string.Equals(eventType, CoreConstants.EventTypeContractWindowOpened, StringComparison.Ordinal)
                        || string.Equals(eventType, CoreConstants.EventTypeContractActivated, StringComparison.Ordinal);
                    bool isContractClose =
                        string.Equals(eventType, CoreConstants.EventTypeContractFinished, StringComparison.Ordinal)
                        || string.Equals(eventType, CoreConstants.EventTypeContractCancelled, StringComparison.Ordinal)
                        || string.Equals(eventType, CoreConstants.EventTypeContractCanceled, StringComparison.Ordinal)
                        || string.Equals(eventType, CoreConstants.EventTypeContractBroken, StringComparison.Ordinal);

                    if (isContractOpen || isContractClose)
                    {
                        contractMutations.Add(
                            new ContractWindowProjectionMutation
                            {
                                SaveKey = saveKey,
                                ContractKey = entityId,
                                IdolId = pendingEvent.IdolId,
                                StartDate = gameDateTime,
                                EndDate = gameDateTime,
                                EndReason = isContractClose ? eventType : string.Empty,
                                PayloadJson = payloadJson,
                                OpenWindow = isContractOpen
                            });
                    }
                }

                if (pendingEvent.IdolId >= CoreConstants.MinimumValidIdolIdentifier
                    && !string.IsNullOrEmpty(entityId))
                {
                    string relationshipType = string.Empty;
                    bool openRelationshipWindow = false;
                    bool closeRelationshipWindow = false;

                    if (string.Equals(eventType, CoreConstants.EventTypeIdolDatingStarted, StringComparison.Ordinal))
                    {
                        relationshipType = CoreConstants.ProjectionRelationshipTypeIdolDating;
                        openRelationshipWindow = true;
                    }
                    else if (string.Equals(eventType, CoreConstants.EventTypeIdolDatingEnded, StringComparison.Ordinal))
                    {
                        relationshipType = CoreConstants.ProjectionRelationshipTypeIdolDating;
                        closeRelationshipWindow = true;
                    }
                    else if (string.Equals(eventType, CoreConstants.EventTypeBullyingStarted, StringComparison.Ordinal))
                    {
                        relationshipType = CoreConstants.ProjectionRelationshipTypeBullying;
                        openRelationshipWindow = true;
                    }
                    else if (string.Equals(eventType, CoreConstants.EventTypeBullyingEnded, StringComparison.Ordinal))
                    {
                        relationshipType = CoreConstants.ProjectionRelationshipTypeBullying;
                        closeRelationshipWindow = true;
                    }
                    else if (string.Equals(eventType, CoreConstants.EventTypeCliqueJoined, StringComparison.Ordinal))
                    {
                        relationshipType = CoreConstants.ProjectionRelationshipTypeClique;
                        openRelationshipWindow = true;
                    }
                    else if (string.Equals(eventType, CoreConstants.EventTypeCliqueLeft, StringComparison.Ordinal))
                    {
                        relationshipType = CoreConstants.ProjectionRelationshipTypeClique;
                        closeRelationshipWindow = true;
                    }

                    if (!string.IsNullOrEmpty(relationshipType) && (openRelationshipWindow || closeRelationshipWindow))
                    {
                        relationshipMutations.Add(
                            new RelationshipWindowProjectionMutation
                            {
                                SaveKey = saveKey,
                                RelationshipKey = entityId,
                                IdolId = pendingEvent.IdolId,
                                RelationshipType = relationshipType,
                                StartDate = gameDateTime,
                                EndDate = gameDateTime,
                                EndReason = closeRelationshipWindow ? eventType : string.Empty,
                                PayloadJson = payloadJson,
                                OpenWindow = openRelationshipWindow
                            });
                    }
                }

                if (pendingEvent.IdolId >= CoreConstants.MinimumValidIdolIdentifier
                    && string.Equals(eventType, CoreConstants.EventTypeTourParticipation, StringComparison.Ordinal)
                    && !string.IsNullOrEmpty(entityId))
                {
                    tourParticipationRows.Add(
                        new TourParticipationProjectionRow
                        {
                            SaveKey = saveKey,
                            TourId = entityId,
                            IdolId = pendingEvent.IdolId,
                            LifecycleAction = ResolveTourLifecycleAction(pendingEvent.SourcePatch),
                            EventDate = gameDateTime,
                            PayloadJson = payloadJson
                        });
                }

                if (string.Equals(eventType, CoreConstants.EventTypeAwardResult, StringComparison.Ordinal)
                    && !string.IsNullOrEmpty(entityId))
                {
                    awardResultRows.Add(
                        new AwardResultProjectionRow
                        {
                            SaveKey = saveKey,
                            AwardKey = entityId,
                            IdolId = pendingEvent.IdolId,
                            EventDate = gameDateTime,
                            PayloadJson = payloadJson
                        });
                }

                if (pendingEvent.IdolId >= CoreConstants.MinimumValidIdolIdentifier
                    && string.Equals(eventType, CoreConstants.EventTypeElectionResultRecorded, StringComparison.Ordinal)
                    && !string.IsNullOrEmpty(entityId))
                {
                    electionResultRows.Add(
                        new ElectionResultProjectionRow
                        {
                            SaveKey = saveKey,
                            ElectionId = entityId,
                            IdolId = pendingEvent.IdolId,
                            EventDate = gameDateTime,
                            PayloadJson = payloadJson
                        });
                }

                if (pendingEvent.IdolId >= CoreConstants.MinimumValidIdolIdentifier
                    && !string.IsNullOrEmpty(entityId))
                {
                    bool isPushOpen = string.Equals(eventType, CoreConstants.EventTypePushWindowStarted, StringComparison.Ordinal);
                    bool isPushClose = string.Equals(eventType, CoreConstants.EventTypePushWindowEnded, StringComparison.Ordinal);
                    bool isPushTouch = string.Equals(eventType, CoreConstants.EventTypePushWindowDayIncrement, StringComparison.Ordinal);
                    if (isPushOpen || isPushClose || isPushTouch)
                    {
                        pushMutations.Add(
                            new PushWindowProjectionMutation
                            {
                                SaveKey = saveKey,
                                SlotKey = entityId,
                                IdolId = pendingEvent.IdolId,
                                StartDate = gameDateTime,
                                EndDate = gameDateTime,
                                EndReason = isPushClose ? eventType : string.Empty,
                                PayloadJson = payloadJson,
                                PushDaysInSlot = ExtractPushDayCount(payloadJson),
                                OpenWindow = isPushOpen,
                                CloseWindow = isPushClose
                            });
                    }
                }
            }
        }

        private static string ResolveTourLifecycleAction(string sourcePatch)
        {
            if (string.Equals(sourcePatch, CoreConstants.EventSourceTourStartPatch, StringComparison.Ordinal))
            {
                return CoreConstants.ProjectionLifecycleActionStarted;
            }

            if (string.Equals(sourcePatch, CoreConstants.EventSourceTourFinishPatch, StringComparison.Ordinal))
            {
                return CoreConstants.ProjectionLifecycleActionFinished;
            }

            if (string.Equals(sourcePatch, CoreConstants.EventSourceTourCancelPatch, StringComparison.Ordinal))
            {
                return CoreConstants.ProjectionLifecycleActionCancelled;
            }

            return CoreConstants.StatusCodeUnknown;
        }

        private static int ExtractPushDayCount(string payloadJson)
        {
            int dayCount;
            if (TryExtractIntegerProperty(payloadJson, CoreConstants.JsonFieldPushDaysInSlot, out dayCount))
            {
                return dayCount;
            }

            return CoreConstants.ProjectionUnknownDayCount;
        }

        private static bool TryExtractIntegerProperty(string payloadJson, string propertyName, out int value)
        {
            value = CoreConstants.InvalidIdValue;
            if (string.IsNullOrEmpty(payloadJson) || string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            string propertyToken = string.Concat(
                CoreConstants.JsonStringQuoteCharacter,
                propertyName,
                CoreConstants.JsonStringQuoteCharacter,
                CoreConstants.JsonNameValueSeparatorCharacter);
            int propertyIndex = payloadJson.IndexOf(propertyToken, StringComparison.Ordinal);
            if (propertyIndex < CoreConstants.ZeroBasedListStartIndex)
            {
                return false;
            }

            int valueIndex = propertyIndex + propertyToken.Length;
            while (valueIndex < payloadJson.Length && char.IsWhiteSpace(payloadJson[valueIndex]))
            {
                valueIndex++;
            }

            int numericStartIndex = valueIndex;
            if (valueIndex < payloadJson.Length && payloadJson[valueIndex] == '-')
            {
                valueIndex++;
            }

            while (valueIndex < payloadJson.Length && char.IsDigit(payloadJson[valueIndex]))
            {
                valueIndex++;
            }

            if (valueIndex <= numericStartIndex)
            {
                return false;
            }

            string numericToken = payloadJson.Substring(numericStartIndex, valueIndex - numericStartIndex);
            return int.TryParse(numericToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }
    }

    /// <summary>
    /// Snapshot captured before world-tour finalization clears manager references.
    /// </summary>
    internal sealed class TourFinishSnapshot
    {
        internal SEvent_Tour.tour Tour;
    }

    /// <summary>
    /// In-memory world-tour runtime state used to keep start participants deterministic until finish.
    /// </summary>
    internal sealed class TourRuntimeCaptureState
    {
        internal string StartDate = string.Empty;
        internal List<int> ParticipantIdolIdentifiers = new List<int>();
    }

    /// <summary>
    /// Snapshot captured before one election place-adjustment mutation.
    /// </summary>
    internal sealed class ElectionPlaceAdjustmentSnapshot
    {
        internal int RequestedPlace = CoreConstants.InvalidIdValue;
        internal int TargetIdolId = CoreConstants.InvalidIdValue;
        internal int PreviousPlace = CoreConstants.InvalidIdValue;
        internal bool HasCandidate;
    }

    /// <summary>
    /// Snapshot captured before one business proposal acceptance mutation.
    /// </summary>
    internal sealed class ContractAcceptedSnapshot
    {
        internal business._proposal AcceptedProposal;
        internal DateTime AcceptedDate = default(DateTime);
        internal List<int> TargetIdolIdentifiers = new List<int>();
    }

    /// <summary>
    /// Snapshot captured before one single cast-removal mutation executes.
    /// </summary>
    internal sealed class SingleCastChangeSnapshot
    {
        internal List<int> SingleCastIdolIdentifiersBefore = new List<int>();
        internal singles._single._status SingleStatusBefore;
        internal int SingleGroupIdBefore = CoreConstants.InvalidIdValue;
        internal string SingleGroupTitleBefore = string.Empty;
        internal string SingleGroupStatusBefore = string.Empty;
    }

    /// <summary>
    /// Snapshot captured before one idol group-transfer mutation executes.
    /// </summary>
    internal sealed class GroupTransferSnapshot
    {
        internal int FromGroupId = CoreConstants.InvalidIdValue;
        internal string FromGroupTitle = string.Empty;
        internal string FromGroupStatus = string.Empty;
    }

    /// <summary>
    /// Snapshot captured before one group-disband mutation executes.
    /// </summary>
    internal sealed class GroupDisbandSnapshot
    {
        internal int GroupId = CoreConstants.InvalidIdValue;
        internal string GroupTitle = string.Empty;
        internal string GroupStatus = string.Empty;
        internal string GroupDateCreated = string.Empty;
        internal int GroupSingleCount;
        internal int GroupNonReleasedSingleCount;
        internal List<int> GroupMemberIdolIdentifiers = new List<int>();
    }

    /// <summary>
    /// Snapshot captured before one new-group creation mutation executes.
    /// </summary>
    internal sealed class GroupCreationSnapshot
    {
        internal List<int> ExistingGroupIds = new List<int>();
    }

    /// <summary>
    /// Snapshot captured before one group-parameter AddPoints mutation executes.
    /// </summary>
    internal sealed class GroupParamPointChangeSnapshot
    {
        internal int GroupId = CoreConstants.InvalidIdValue;
        internal string GroupTitle = string.Empty;
        internal string GroupStatus = string.Empty;
        internal string GroupSourceParamType = string.Empty;
        internal int GroupPointsBefore;
        internal int GroupAvailablePointsBefore;
    }

    /// <summary>
    /// Snapshot captured before one group appeal-point SpendPoints mutation executes.
    /// </summary>
    internal sealed class GroupAppealPointSpendSnapshot
    {
        internal int GroupId = CoreConstants.InvalidIdValue;
        internal string GroupTitle = string.Empty;
        internal string GroupStatus = string.Empty;
        internal string GroupSourceParamType = string.Empty;
        internal string GroupTargetFanType = string.Empty;
        internal int GroupPointsRequested;
        internal int GroupAvailablePointsBefore;
        internal int GroupPointsSpentBefore;
        internal int GroupTargetPointsBefore;
    }

    /// <summary>
    /// Snapshot captured before idol salary mutation methods execute.
    /// </summary>
    internal sealed class SalaryChangeSnapshot
    {
        internal long SalaryBefore;
        internal int SalarySatisfactionBefore;
    }

    /// <summary>
    /// Snapshot captured before one loan mutation executes.
    /// </summary>
    internal sealed class LoanMutationSnapshot
    {
        internal int LoanId = CoreConstants.InvalidIdValue;
        internal bool LoanActive;
        internal long LoanDebt;
        internal bool LoanCanPayOff;
        internal bool LoanInDevelopment;
        internal string LoanStartDate = string.Empty;
        internal string LoanEndDate = string.Empty;
        internal int LoanDaysToDevelop;
        internal long Money;
        internal long TotalDebt;
        internal int TotalPaymentPerWeek;
        internal int ActiveLoanCount;
        internal int TotalLoanCount;
    }

    /// <summary>
    /// Snapshot captured before `loans.SetBankruptcyDanger` mutates static danger state.
    /// </summary>
    internal sealed class BankruptcyDangerSnapshot
    {
        internal bool DangerBefore;
        internal DateTime BankruptcyDateBefore;
        internal long MoneyBefore;
        internal long TotalDebtBefore;
    }

    /// <summary>
    /// Snapshot captured before one bankruptcy check evaluation.
    /// </summary>
    internal sealed class BankruptcyCheckSnapshot
    {
        internal bool DangerBefore;
        internal int DaysRemainingBefore;
        internal long MoneyBefore;
        internal long TotalDebtBefore;
        internal bool BailoutUsedBefore;
        internal bool StoryRecruitUsedBefore;
        internal bool GameOverUsedBefore;
    }

    /// <summary>
    /// Snapshot captured before one scandal check evaluation.
    /// </summary>
    internal sealed class ScandalCheckSnapshot
    {
        internal bool TestGameOver;
        internal long ScandalPointsBefore;
        internal int ScandalThreshold;
        internal bool FirstScandalUsedBefore;
        internal bool WarningUsedBefore;
        internal bool ParentsUsedBefore;
        internal bool GameOverUsedBefore;
        internal bool ScandalParentCooldownBefore;
        internal bool AuditionFailureBefore;
        internal int ActiveIdolCountBefore;
    }

    /// <summary>
    /// Snapshot captured before audition-failure wipes active idols.
    /// </summary>
    internal sealed class AuditionFailureSnapshot
    {
        internal bool AuditionFailureBefore;
        internal int ActiveIdolCountBefore;
    }

    /// <summary>
    /// Snapshot captured before one policy value selection mutation.
    /// </summary>
    internal sealed class PolicySelectionSnapshot
    {
        internal string PreviousSelectedValueCode = CoreConstants.StatusCodeUnknown;
    }

    /// <summary>
    /// Snapshot captured before one activity action mutates economy and idol stats.
    /// </summary>
    internal sealed class ActivityActionSnapshot
    {
        internal long MoneyBefore;
        internal long FansBefore;
        internal int ActiveIdolCountBefore;
    }

    /// <summary>
    /// Snapshot captured before one idol earnings mutation.
    /// </summary>
    internal sealed class IdolEarningsSnapshot
    {
        internal long EarningsCurrentMonthBefore;
    }

    /// <summary>
    /// Snapshot captured before one theater lifecycle mutation.
    /// </summary>
    internal sealed class TheaterLifecycleSnapshot
    {
        internal int TheaterId = CoreConstants.InvalidIdValue;
        internal string TheaterTitle = string.Empty;
        internal int GroupId = CoreConstants.InvalidIdValue;
        internal int RoomTheaterId = CoreConstants.InvalidIdValue;
        internal int TicketPrice;
        internal int SubscriptionPrice;
        internal bool StreamingResearched;
        internal bool EquipmentPurchased;
        internal long SubscriberTotal;
        internal string ScheduleSummary = string.Empty;
    }

    /// <summary>
    /// Snapshot captured before `Theaters.CompleteDay` mutates daily theater stats.
    /// </summary>
    internal sealed class TheaterCompleteDaySnapshot
    {
        internal long MoneyBefore;
        internal Dictionary<int, int> StatCountByTheaterId = new Dictionary<int, int>();
        internal Dictionary<int, long> SubscriberTotalByTheaterId = new Dictionary<int, long>();
    }

    /// <summary>
    /// Snapshot captured before one cafe lifecycle mutation.
    /// </summary>
    internal sealed class CafeLifecycleSnapshot
    {
        internal int CafeId = CoreConstants.InvalidIdValue;
        internal string CafeTitle = string.Empty;
        internal int GroupId = CoreConstants.InvalidIdValue;
        internal int RoomTheaterId = CoreConstants.InvalidIdValue;
        internal int WaitStaffCount;
        internal int WorkingStaffCount;
        internal string CafePriority = string.Empty;
        internal string StaffPriority = string.Empty;
        internal string MenuSummary = string.Empty;
    }

    /// <summary>
    /// Snapshot captured before one cafe daily render pass.
    /// </summary>
    internal sealed class CafeRenderSnapshot
    {
        internal long MoneyBefore;
        internal int StatCountBefore;
    }

    /// <summary>
    /// Snapshot captured before one staff lifecycle mutation.
    /// </summary>
    internal sealed class StaffLifecycleSnapshot
    {
        internal int StaffId = CoreConstants.InvalidIdValue;
        internal string StaffName = string.Empty;
        internal string StaffType = string.Empty;
        internal string StaffUniqueType = string.Empty;
        internal int StaffSalary;
        internal int StaffLevelBefore;
        internal bool CanFireBefore;
        internal bool CanFireSeveranceBefore;
        internal int FirePointsCostBefore;
        internal int SeveranceCostBefore;
        internal long ScandalPointsBefore;
        internal long MoneyBefore;
        internal string RoomType = string.Empty;
        internal string HireDate = string.Empty;
        internal bool WasInStaffListBefore;
    }

    /// <summary>
    /// Snapshot captured before one research category parameter assignment.
    /// </summary>
    internal sealed class ResearchSetParamSnapshot
    {
        internal string PreviousParamTypeCode = CoreConstants.StatusCodeUnknown;
        internal int PreviousParamId = CoreConstants.InvalidIdValue;
        internal string PreviousParamTitle = string.Empty;
        internal long ResearchPointsBefore;
    }

    /// <summary>
    /// Snapshot captured before one research points purchase operation.
    /// </summary>
    internal sealed class ResearchBuyPointsSnapshot
    {
        internal long MoneyBefore;
        internal long BuyingCostBefore;
        internal long PointsBefore;
        internal float PointsRawBefore;
    }

    /// <summary>
    /// Snapshot captured before one research points accrual mutation.
    /// </summary>
    internal sealed class ResearchAddPointsSnapshot
    {
        internal long PointsBefore;
        internal float PointsRawBefore;
    }

    /// <summary>
    /// Snapshot captured before one single research-parameter level-up attempt.
    /// </summary>
    internal sealed class ResearchParamLevelUpSnapshot
    {
        internal int LevelBefore;
        internal float ParamExpBefore;
        internal long PointsNeededBefore;
        internal long SavedPointsBefore;
        internal float CategoryPointsRawBefore;
        internal string ResearchTypeCode = CoreConstants.StatusCodeUnknown;
    }

    /// <summary>
    /// Snapshot captured before one story route lock operation.
    /// </summary>
    internal sealed class RouteLockSnapshot
    {
        internal tasks._route RouteBefore = tasks._route.NONE;
        internal List<string> ActiveTaskCustomsBefore = new List<string>();
    }

    /// <summary>
    /// Snapshot captured before one task lifecycle mutation.
    /// </summary>
    internal sealed class TaskLifecycleSnapshot
    {
        internal string TaskCustom = string.Empty;
        internal string TaskTypeCode = CoreConstants.StatusCodeUnknown;
        internal string TaskGoalCode = CoreConstants.StatusCodeUnknown;
        internal bool TaskSubstory;
        internal int TaskGirlId = CoreConstants.InvalidIdValue;
        internal string TaskSkillCode = CoreConstants.StatusCodeUnknown;
        internal string TaskAgentName = string.Empty;
        internal bool FulfilledBefore;
        internal bool WasActiveBefore;
        internal string RouteCodeBefore = CoreConstants.StatusCodeUnknown;
        internal string AvailableFrom = string.Empty;
    }

    /// <summary>
    /// Snapshot captured before one concert card usage mutation.
    /// </summary>
    internal sealed class ConcertCardUseSnapshot
    {
        internal int ConcertId = CoreConstants.InvalidIdValue;
        internal int CardsBefore;
        internal float CardAccidentHappeningBefore;
        internal float CardAccidentSuccessBefore;
        internal bool CardNoCriticalFailureBefore;
        internal int NoAccidentCounterBefore;
        internal int UsedAccidentCountBefore;
    }

    /// <summary>
    /// Snapshot captured after one concert crisis decision but before close applies hype.
    /// </summary>
    internal sealed class ConcertCrisisChoiceSnapshot
    {
        internal int ConcertId = CoreConstants.InvalidIdValue;
        internal string AccidentTitle = string.Empty;
        internal bool ChoiceSafe;
        internal string ChoiceTypeCode = CoreConstants.ConcertCrisisChoiceUnknown;
        internal string ResultTypeCode = CoreConstants.StatusCodeUnknown;
        internal int ExpectedHypeDelta;
    }

    /// <summary>
    /// Snapshot captured immediately before one concert crisis close applies hype delta.
    /// </summary>
    internal sealed class ConcertCrisisAppliedSnapshot
    {
        internal int ConcertId = CoreConstants.InvalidIdValue;
        internal string AccidentTitle = string.Empty;
        internal bool ChoiceSafe;
        internal string ChoiceTypeCode = CoreConstants.ConcertCrisisChoiceUnknown;
        internal string ResultTypeCode = CoreConstants.StatusCodeUnknown;
        internal int ExpectedHypeDelta;
        internal float HypeBefore;
    }

    /// <summary>
    /// Snapshot captured before final concert resolution is committed.
    /// </summary>
    internal sealed class ConcertFinishSnapshot
    {
        internal int ConcertId = CoreConstants.InvalidIdValue;
        internal long MoneyBefore;
        internal long ActualRevenue;
        internal long ActualProfit;
        internal int TicketPrice;
        internal int UsedAccidentCountBefore;
        internal int NoAccidentCounterBefore;
        internal Dictionary<int, long> EarningsBeforeByIdolId = new Dictionary<int, long>();
        internal Dictionary<resources.fanType, int> FanOpinionScoreBeforeByType = new Dictionary<resources.fanType, int>();
    }

    /// <summary>
    /// Snapshot captured before one election start operation.
    /// </summary>
    internal sealed class ElectionStartSnapshot
    {
        internal long MoneyBefore;
        internal long StartCost;
    }

    /// <summary>
    /// Snapshot captured before one idol wish lifecycle mutation.
    /// </summary>
    internal sealed class WishLifecycleSnapshot
    {
        internal int IdolId = CoreConstants.InvalidIdValue;
        internal string WishTypeCodeBefore = CoreConstants.StatusCodeUnknown;
        internal string WishFormulaBefore = string.Empty;
        internal bool WishFulfilledBefore;
        internal DateTime WishEffectUntilBefore;
        internal int InfluencePointsBefore;
        internal int InfluenceLevelBefore;
        internal float MentalStaminaBefore;
    }

    /// <summary>
    /// Snapshot captured before one show cast-removal mutation executes.
    /// </summary>
    internal sealed class ShowCastChangeSnapshot
    {
        internal List<int> ShowCastIdolIdentifiersBefore = new List<int>();
        internal Shows._show._castType ShowCastTypeBefore;
        internal Shows._show._status ShowStatusBefore;
        internal string ShowTitleBefore = string.Empty;
        internal string ShowMcCodeBefore = string.Empty;
        internal string ShowMcTitleBefore = string.Empty;
        internal int ShowProductionCostBefore;
        internal string ShowFanAppealSummaryBefore = string.Empty;
    }

    /// <summary>
    /// Snapshot captured before one concert cast-removal mutation executes.
    /// </summary>
    internal sealed class ConcertCastChangeSnapshot
    {
        internal List<int> ConcertParticipantIdolIdentifiersBefore = new List<int>();
        internal SEvent_Tour.tour._status ConcertStatusBefore;
        internal int ConcertSongCountBefore;
        internal string ConcertSetlistSummaryBefore = string.Empty;
        internal string ConcertTitleBefore = string.Empty;
        internal string ConcertRawTitleBefore = string.Empty;
        internal SEvent_Concerts._venue ConcertVenueBefore;
        internal int ConcertTicketPriceBefore;
    }

    /// <summary>
    /// Snapshot captured before one push removal operation executes.
    /// </summary>
    internal sealed class PushRemovalSnapshot
    {
        internal int SlotIndex = CoreConstants.InvalidIdValue;
        internal int IdolId = CoreConstants.InvalidIdValue;
        internal int DaysInSlot;
    }

    /// <summary>
    /// Snapshot captured before one clique-member quit mutation.
    /// </summary>
    internal sealed class CliqueQuitSnapshot
    {
        internal bool WasMemberBeforeQuit;
        internal int PreviousLeaderId = CoreConstants.InvalidIdValue;
    }

    /// <summary>
    /// Snapshot captured before scandal points popup applies mitigation.
    /// </summary>
    internal sealed class ScandalMitigationSnapshot
    {
        internal int PointsAvailableBefore;
        internal int GroupPointsToRemove;
        internal Dictionary<int, int> IdolPointsToRemoveById = new Dictionary<int, int>();
    }

    /// <summary>
    /// Snapshot captured before one player date interaction mutates dating state.
    /// </summary>
    internal sealed class PlayerDateInteractionSnapshot
    {
        internal string RouteBefore = CoreConstants.StatusCodeUnknown;
        internal string StageBefore = CoreConstants.StatusCodeUnknown;
        internal string StatusBefore = CoreConstants.StatusCodeUnknown;
        internal bool DateCaughtBefore;
        internal int RelationshipLevelBefore;
    }

    /// <summary>
    /// Snapshot captured before relationship-level stop-bullying logic executes.
    /// </summary>
    internal sealed class RelationshipStopBullyingSnapshot
    {
        internal Relationships._clique FirstClique;
        internal data_girls.girls FirstTarget;
        internal bool FirstWasBullied;
        internal Relationships._clique SecondClique;
        internal data_girls.girls SecondTarget;
        internal bool SecondWasBullied;
    }

    /// <summary>
    /// Snapshot captured before one agency room build mutation.
    /// </summary>
    internal sealed class AgencyRoomBuildSnapshot
    {
        internal long MoneyBefore;
        internal int RequestedTypeRaw;
        internal bool BuildFlag;
        internal HashSet<int> ExistingRoomIds = new HashSet<int>();
    }

    /// <summary>
    /// Snapshot captured before one agency room destroy mutation.
    /// </summary>
    internal sealed class AgencyRoomDestroySnapshot
    {
        internal int RoomId = CoreConstants.InvalidIdValue;
        internal agency._type RoomType = agency._type.yourOffice;
        internal int TheaterId = CoreConstants.InvalidIdValue;
        internal int FloorId = CoreConstants.InvalidIdValue;
        internal int FloorIndex = CoreConstants.InvalidIdValue;
        internal int RoomSpace;
        internal int RoomCost;
        internal long MoneyBefore;
    }

    /// <summary>
    /// Snapshot captured before one audition generation operation.
    /// </summary>
    internal sealed class AuditionStartSnapshot
    {
        internal long MoneyBefore;
        internal float ProgressBefore;
        internal DateTime RegionalCooldownBefore = Auditions.Regional_Date;
        internal DateTime NationwideCooldownBefore = Auditions.Nationwide_Date;
    }

    /// <summary>
    /// Snapshot captured before one audition cooldown reset request.
    /// </summary>
    internal sealed class AuditionCooldownResetSnapshot
    {
        internal bool CanResetBefore;
        internal int ResetCostBefore;
        internal int RegionalDaysRemainingBefore;
        internal int NationwideDaysRemainingBefore;
        internal DateTime RegionalCooldownBefore = Auditions.Regional_Date;
        internal DateTime NationwideCooldownBefore = Auditions.Nationwide_Date;
        internal long MoneyBefore;
        internal long ResearchPlayerPointsBefore;
    }

    /// <summary>
    /// Snapshot captured before one random-event start mutation.
    /// </summary>
    internal sealed class RandomEventStartSnapshot
    {
        internal long MoneyBefore;
        internal long FansBefore;
        internal int FameBefore;
        internal int BuzzBefore;
        internal int ActiveEventCountBefore;
    }

    /// <summary>
    /// Snapshot captured before one random-event conclusion mutation.
    /// </summary>
    internal sealed class RandomEventConcludeSnapshot
    {
        internal Event_Manager._activeEvent ActiveEventBefore;
        internal string ActiveEventStateBefore = string.Empty;
        internal string ActorSummaryBefore = string.Empty;
        internal long EstimatedLiabilityBefore;
        internal long MoneyBefore;
        internal long FansBefore;
        internal int FameBefore;
        internal int BuzzBefore;
    }

    /// <summary>
    /// Snapshot captured before one substory start-queue mutation.
    /// </summary>
    internal sealed class SubstoryStartSnapshot
    {
        internal int QueueCountBefore;
        internal int DelayedCountBefore;
        internal bool WasUsedBefore;
        internal bool WasDelayedBefore;
        internal bool WasQueuedBefore;
    }

    /// <summary>
    /// Snapshot captured before active dialogue hide for substory completion tracking.
    /// </summary>
    internal sealed class SubstoryCompletionSnapshot
    {
        internal string DialogueId = string.Empty;
        internal string ParentDialogueId = string.Empty;
        internal string DialogueTypeCode = string.Empty;
        internal bool ShouldEmit;
    }

    /// <summary>
    /// Snapshot captured before daily/weekly passive economy ticks.
    /// </summary>
    internal sealed class EconomyTickSnapshot
    {
        internal long MoneyBefore;
        internal long FansBefore;
        internal int FameBefore;
        internal int BuzzBefore;
        internal int FameLevelBefore;
        internal float FameProgressBefore;
    }

    /// <summary>
    /// Snapshot row for one mentor-kohai pair before mutation.
    /// </summary>
    internal sealed class MentorshipPairSnapshot
    {
        internal int MentorId = CoreConstants.InvalidIdValue;
        internal int KohaiId = CoreConstants.InvalidIdValue;
        internal float SenpaiToKohaiRatioBefore;
        internal float KohaiToSenpaiRatioBefore;
    }

    /// <summary>
    /// Snapshot captured before mentor removal mutation executes.
    /// </summary>
    internal sealed class MentorshipRemoveSnapshot
    {
        internal List<MentorshipPairSnapshot> RemovedPairs = new List<MentorshipPairSnapshot>();
    }

    /// <summary>
    /// Snapshot captured before mentor weekly relationship tick mutation.
    /// </summary>
    internal sealed class MentorshipWeeklySnapshot
    {
        internal List<MentorshipPairSnapshot> PairSnapshots = new List<MentorshipPairSnapshot>();
    }

    /// <summary>
    /// Snapshot captured before one rival-market mutation.
    /// </summary>
    internal sealed class RivalMarketSnapshot
    {
        internal int MonthIndexBefore;
        internal int ActiveGroupCountBefore;
        internal int RisingGroupCountBefore;
        internal int DeadGroupCountBefore;
        internal long OfficeResearchPointsBefore;
        internal string TrendLastUpdatedBefore = string.Empty;
        internal string TrendsGenreSummaryBefore = string.Empty;
        internal string TrendsLyricsSummaryBefore = string.Empty;
        internal string TrendsChoreoSummaryBefore = string.Empty;
    }

    /// <summary>
    /// Snapshot captured before summer games finalization spend.
    /// </summary>
    internal sealed class SummerGamesFinalizeSnapshot
    {
        internal int SelectedSingleIdBefore = CoreConstants.InvalidIdValue;
        internal int GenreIdBefore = CoreConstants.InvalidIdValue;
        internal int LyricsIdBefore = CoreConstants.InvalidIdValue;
        internal int ChoreographyIdBefore = CoreConstants.InvalidIdValue;
        internal int GenreCostBefore;
        internal int LyricsCostBefore;
        internal int ChoreographyCostBefore;
        internal int TotalCostBefore;
        internal bool WasFinalizedBefore;
        internal long VocalPointsBefore;
        internal long PlayerPointsBefore;
        internal long DancePointsBefore;
    }

    /// <summary>
    /// Public immutable event model returned to API consumers.
    /// </summary>
    public sealed class IMDataCoreEvent
    {
        public long EventId { get; internal set; }
        public int GameDateKey { get; internal set; }
        public string GameDateTime { get; internal set; }
        public int IdolId { get; internal set; }
        public string EntityKind { get; internal set; }
        public string EntityId { get; internal set; }
        public string EventType { get; internal set; }
        public string SourcePatch { get; internal set; }
        public string PayloadJson { get; internal set; }
        public string NamespaceId { get; internal set; }
    }

    /// <summary>
    /// Opaque API session handle that authorizes namespaced custom data access.
    /// </summary>
    public sealed class IMDataCoreSession
    {
        internal IMDataCoreSession(string namespaceIdentifier, string sessionToken)
        {
            NamespaceIdentifier = namespaceIdentifier;
            SessionToken = sessionToken;
        }

        public string NamespaceIdentifier { get; private set; }

        internal string SessionToken { get; private set; }
    }

    /// <summary>
    /// Internal registration metadata used to validate API session ownership.
    /// </summary>
    internal sealed class NamespaceSessionRegistration
    {
        internal string NamespaceIdentifier = string.Empty;
        internal string SessionToken = string.Empty;
        internal string CallingAssemblyIdentity = string.Empty;
    }


}

