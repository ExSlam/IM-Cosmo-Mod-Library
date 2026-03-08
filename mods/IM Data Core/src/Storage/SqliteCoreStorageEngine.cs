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
    internal sealed class SqliteCoreStorageEngine : ICoreStorageEngine
    {
        private const string NativeProviderName = "winsqlite3";
        private const int SqliteOk = 0;
        private const int SqliteRow = 100;
        private const int SqliteDone = 101;
        private const int SqliteTypeInteger = 1;
        private const int SqliteTypeText = 3;
        private const int SqliteTypeNull = 5;
        private const int Utf16BytesPerCharacter = 2;
        private const int DefaultBusyTimeoutMilliseconds = 5000;
        private const int SqliteErrorMessageBuilderInitialCapacity = 64;
        private const int SqlPreviewMaximumLength = 160;
        private const string SqlPreviewEllipsis = "...";
        private const string DatabaseWriteAheadLogFileSuffix = "-wal";
        private const string DatabaseSharedMemoryFileSuffix = "-shm";
        private const string SqlPragmaJournalModeDelete = "PRAGMA journal_mode=DELETE;";
        private const string SqlInsertMetaSchemaVersionLiteral = "INSERT OR REPLACE INTO meta(meta_key, meta_value) VALUES('schema_version', '2');";
        private const string SqlInsertMetaProviderLiteral = "INSERT OR REPLACE INTO meta(meta_key, meta_value) VALUES('db_provider', '" + NativeProviderName + "');";
        private const string SqlBeginImmediate = "BEGIN IMMEDIATE;";
        private const string SqlCommit = "COMMIT;";
        private const string SqlRollback = "ROLLBACK;";
        private const string SqlParameterCutoffDateTime = "@cutoff_datetime";
        private const string SqlParameterSourceSaveKey = "@source_save_key";
        private const string SqlParameterTargetSaveKey = "@target_save_key";
        private const string SqlSelectUserTableNames =
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';";
        private const string SqlDeleteEventStreamRowsAfterCutoff =
            "DELETE FROM event_stream " +
            "WHERE save_key = @save_key " +
            "AND (game_date_key > @game_date_key OR (game_date_key = @game_date_key AND game_datetime > @cutoff_datetime));";
        private const string SqlDeleteSingleParticipationRowsAfterCutoff =
            "DELETE FROM single_participation WHERE save_key = @save_key AND release_date > @cutoff_datetime;";
        private const string SqlDeleteStatusWindowRowsAfterCutoff =
            "DELETE FROM status_window WHERE save_key = @save_key AND (start_date > @cutoff_datetime OR end_date > @cutoff_datetime);";
        private const string SqlDeleteShowCastWindowRowsAfterCutoff =
            "DELETE FROM show_cast_window WHERE save_key = @save_key AND (start_date > @cutoff_datetime OR end_date > @cutoff_datetime);";
        private const string SqlDeleteContractWindowRowsAfterCutoff =
            "DELETE FROM contract_window WHERE save_key = @save_key AND (start_date > @cutoff_datetime OR end_date > @cutoff_datetime);";
        private const string SqlDeleteRelationshipWindowRowsAfterCutoff =
            "DELETE FROM relationship_window WHERE save_key = @save_key AND (start_date > @cutoff_datetime OR end_date > @cutoff_datetime);";
        private const string SqlDeleteTourParticipationRowsAfterCutoff =
            "DELETE FROM tour_participation WHERE save_key = @save_key AND event_date > @cutoff_datetime;";
        private const string SqlDeleteAwardResultRowsAfterCutoff =
            "DELETE FROM award_result_projection WHERE save_key = @save_key AND event_date > @cutoff_datetime;";
        private const string SqlDeleteElectionResultRowsAfterCutoff =
            "DELETE FROM election_result_projection WHERE save_key = @save_key AND event_date > @cutoff_datetime;";
        private const string SqlDeletePushWindowRowsAfterCutoff =
            "DELETE FROM push_window WHERE save_key = @save_key AND (start_date > @cutoff_datetime OR end_date > @cutoff_datetime);";
        private const string SerializerMethodNameObjectPayload = nameof(CoreJsonUtility.SerializeObjectPayload);
        private const string SchemaSampleStringValue = "schema_sample";
        private const bool SchemaSampleBooleanValue = true;
        private const int SchemaSampleInt32Value = 1;
        private const long SchemaSampleInt64Value = 1L;
        private const float SchemaSampleSingleValue = 1.5f;
        private const double SchemaSampleDoubleValue = 1.5d;
        private const short SchemaSampleInt16Value = 1;
        private const byte SchemaSampleByteValue = 1;
        private const int SqlSelectUserTableNamesNameColumnIndex = 0;
        private const int SqlPragmaTableInfoNameColumnIndex = 1;
        private const string SqlPragmaTableInfoPrefix = "PRAGMA table_info(";
        private const string SqlPragmaTableInfoSuffix = ");";
        private const string SqlCreateTableIfNotExistsPrefix = "CREATE TABLE IF NOT EXISTS ";
        private const string SqlCreateTableOpenParenthesis = " (";
        private const string SqlCreateTableCloseWithEventForeignKey = ", FOREIGN KEY(event_id) REFERENCES event_stream(event_id) ON DELETE CASCADE);";
        private const string SqlInsertOrReplaceIntoPrefix = "INSERT OR REPLACE INTO ";
        private const string SqlInsertValuesClauseSeparator = ") VALUES (";
        private const string SqlStatementTerminator = ";";
        private const string SqlSelectClausePrefix = "SELECT ";
        private const string SqlFromClausePrefix = " FROM ";
        private const string SqlWhereClausePrefix = " WHERE ";
        private const string SqlLimitOneClause = " LIMIT 1";
        private const string SqlEqualsOperatorWithSpacing = " = ";
        private const string SqlOpenParenthesis = "(";
        private const string SqlCloseParenthesis = ")";
        private const string SqlColumnSeparator = ", ";
        private const string SqlSpaceSeparator = " ";
        private const string SqlPrimaryKeyConstraint = " PRIMARY KEY";
        private const string SqlNotNullConstraint = " NOT NULL";
        private const string SqlTypeText = "TEXT";
        private const string SqlTypeInteger = "INTEGER";
        private const string SqlTypeReal = "REAL";
        private const string SqlColumnEventId = "event_id";
        private const string SqlColumnSaveKey = "save_key";
        private const string SqlColumnGameDateKey = "game_date_key";
        private const string SqlColumnGameDateTime = "game_datetime";
        private const string SqlColumnIdolId = "idol_id";
        private const string SqlColumnEntityKind = "entity_kind";
        private const string SqlColumnEntityId = "entity_id";
        private const string SqlColumnSourcePatch = "source_patch";
        private const string SqlPayloadParameterPrefix = "@payload_value_";
        private const string BuiltInEventTableNamePrefix = "evt_";
        private const string BuiltInEventTableNameFallbackToken = "event";
        private const string PayloadColumnNamePrefix = "f_";
        private const string PayloadColumnNameFallbackToken = "value";
        private const string SanitizeIdentifierFallbackToken = "value";
        private const string SqlIdentifierQuote = "\"";
        private const string SqlIdentifierEscapedQuote = "\"\"";
        private const int SqliteBooleanTrueValue = 1;
        private const int SqliteBooleanFalseValue = 0;
        private const char NumericDecimalPointCharacter = '.';
        private const char NumericExponentLowerCharacter = 'e';
        private const char NumericExponentUpperCharacter = 'E';
        private const char JsonArrayStartCharacter = '[';
        private const char JsonArrayEndCharacter = ']';
        private const string EventTypeConstantFieldPrefix = "EventType";
        private static readonly IntPtr SqliteTransient = new IntPtr(-1);

        private sealed class EventPayloadFieldRow
        {
            internal string FieldKey = string.Empty;
            internal string ValueKind = CoreConstants.PayloadValueKindRaw;
            internal string ValueText = string.Empty;
        }

        private sealed class BuiltInEventSchemaSource
        {
            internal string SerializerMethodName = string.Empty;
            internal Type PayloadType;

            internal BuiltInEventSchemaSource(string serializerMethodName, Type payloadType)
            {
                SerializerMethodName = serializerMethodName ?? string.Empty;
                PayloadType = payloadType;
            }
        }

        private static readonly Dictionary<string, BuiltInEventSchemaSource> BuiltInEventSchemaSourcesByEventType = BuildBuiltInEventSchemaSources();

        private readonly object databaseLock = new object();
        private readonly Dictionary<string, HashSet<string>> builtInEventTableColumnsByTableName = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<EventPayloadFieldRow>> builtInEventSchemaRowsByEventType = new Dictionary<string, List<EventPayloadFieldRow>>(StringComparer.Ordinal);
        private IntPtr databaseHandle = IntPtr.Zero;
        private bool disposed;

        internal static bool TryProbeRuntime(out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                int versionNumber = NativeMethods.sqlite3_libversion_number();
                if (versionNumber > CoreConstants.ZeroBasedListStartIndex)
                {
                    return true;
                }

                errorMessage = CoreConstants.MessageNoCompatibleSqliteProvider;
                return false;
            }
            catch (Exception exception)
            {
                errorMessage = CoreConstants.MessageNoCompatibleSqliteProvider + CoreConstants.LogSeparatorColonSpace + exception.Message;
                return false;
            }
        }

        private static Dictionary<string, BuiltInEventSchemaSource> BuildBuiltInEventSchemaSources()
        {
            Dictionary<string, BuiltInEventSchemaSource> map = new Dictionary<string, BuiltInEventSchemaSource>(StringComparer.Ordinal);

            RegisterBuiltInObjectEventSchemaSource<ActivityActionEventPayload>(map, CoreConstants.EventTypeActivityPerformance);
            RegisterBuiltInObjectEventSchemaSource<ActivityActionEventPayload>(map, CoreConstants.EventTypeActivityPromotion);
            RegisterBuiltInObjectEventSchemaSource<ActivityActionEventPayload>(map, CoreConstants.EventTypeActivitySpaTreatment);
            RegisterBuiltInObjectEventSchemaSource<AgencyRoomLifecycleEventPayload>(map, CoreConstants.EventTypeAgencyRoomBuilt);
            RegisterBuiltInObjectEventSchemaSource<AgencyRoomCostPaidEventPayload>(map, CoreConstants.EventTypeAgencyRoomCostPaid);
            RegisterBuiltInObjectEventSchemaSource<AgencyRoomLifecycleEventPayload>(map, CoreConstants.EventTypeAgencyRoomDestroyed);
            RegisterBuiltInObjectEventSchemaSource<AuditionCooldownResetEventPayload>(map, CoreConstants.EventTypeAuditionCooldownReset);
            RegisterBuiltInObjectEventSchemaSource<AuditionCostPaidEventPayload>(map, CoreConstants.EventTypeAuditionCostPaid);
            RegisterBuiltInObjectEventSchemaSource<AuditionFailureEventPayload>(map, CoreConstants.EventTypeAuditionFailureTriggered);
            RegisterBuiltInObjectEventSchemaSource<AuditionStartedEventPayload>(map, CoreConstants.EventTypeAuditionStarted);
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeAwardNominated, nameof(CoreJsonUtility.SerializeAwardLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeAwardResult, nameof(CoreJsonUtility.SerializeAwardLifecyclePayload));
            RegisterBuiltInObjectEventSchemaSource<BankruptcyCheckEventPayload>(map, CoreConstants.EventTypeBankruptcyCheck);
            RegisterBuiltInObjectEventSchemaSource<BankruptcyDangerEventPayload>(map, CoreConstants.EventTypeBankruptcyDangerSet);
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeBullyingEnded, nameof(CoreJsonUtility.SerializeBullyingLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeBullyingStarted, nameof(CoreJsonUtility.SerializeBullyingLifecyclePayload));
            RegisterBuiltInObjectEventSchemaSource<CafeLifecycleEventPayload>(map, CoreConstants.EventTypeCafeCreated);
            RegisterBuiltInObjectEventSchemaSource<CafeDailyResultEventPayload>(map, CoreConstants.EventTypeCafeDailyResult);
            RegisterBuiltInObjectEventSchemaSource<CafeLifecycleEventPayload>(map, CoreConstants.EventTypeCafeDestroyed);
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeCliqueJoined, nameof(CoreJsonUtility.SerializeCliqueLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeCliqueLeft, nameof(CoreJsonUtility.SerializeCliqueLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeConcertCancelled, nameof(CoreJsonUtility.SerializeConcertLifecyclePayload));
            RegisterBuiltInObjectEventSchemaSource<ConcertCardUsedEventPayload>(map, CoreConstants.EventTypeConcertCardUsed);
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeConcertCastChanged, nameof(CoreJsonUtility.SerializeConcertCastChangePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeConcertConfigurationChanged, nameof(CoreJsonUtility.SerializeConcertConfigurationChangePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeConcertCreated, nameof(CoreJsonUtility.SerializeConcertLifecyclePayload));
            RegisterBuiltInObjectEventSchemaSource<ConcertCrisisAppliedEventPayload>(map, CoreConstants.EventTypeConcertCrisisApplied);
            RegisterBuiltInObjectEventSchemaSource<ConcertCrisisDecisionEventPayload>(map, CoreConstants.EventTypeConcertCrisisDecision);
            RegisterBuiltInObjectEventSchemaSource<ConcertFinalResolvedEventPayload>(map, CoreConstants.EventTypeConcertFinalResolved);
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeConcertFinished, nameof(CoreJsonUtility.SerializeConcertLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeConcertParticipation, nameof(CoreJsonUtility.SerializeConcertLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeConcertStarted, nameof(CoreJsonUtility.SerializeConcertLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeConcertStatusChanged, nameof(CoreJsonUtility.SerializeConcertStatusPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeContractAccepted, nameof(CoreJsonUtility.SerializeContractLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeContractActivated, nameof(CoreJsonUtility.SerializeContractLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeContractBroken, nameof(CoreJsonUtility.SerializeContractLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeContractCanceled, nameof(CoreJsonUtility.SerializeContractLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeContractCancelled, nameof(CoreJsonUtility.SerializeContractLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeContractFinished, nameof(CoreJsonUtility.SerializeContractLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeContractWeeklyBenefitsApplied, nameof(CoreJsonUtility.SerializeContractWeeklyAccrualPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeContractWeeklyEarningsApplied, nameof(CoreJsonUtility.SerializeContractWeeklyAccrualPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeContractWindowOpened, nameof(CoreJsonUtility.SerializeContractLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeDatingPartnerStatusChanged, nameof(CoreJsonUtility.SerializeDatingPartnerStatusPayload));
            RegisterBuiltInObjectEventSchemaSource<EconomyDailyTickEventPayload>(map, CoreConstants.EventTypeEconomyDailyTick);
            RegisterBuiltInObjectEventSchemaSource<EconomyWeeklyExpenseEventPayload>(map, CoreConstants.EventTypeEconomyWeeklyExpenseApplied);
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeElectionCancelled, nameof(CoreJsonUtility.SerializeElectionLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeElectionCreated, nameof(CoreJsonUtility.SerializeElectionLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeElectionFinished, nameof(CoreJsonUtility.SerializeElectionLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeElectionPlaceAdjusted, nameof(CoreJsonUtility.SerializeElectionPlaceAdjustedPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeElectionResultRecorded, nameof(CoreJsonUtility.SerializeElectionResultPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeElectionResultsGenerated, nameof(CoreJsonUtility.SerializeElectionGeneratedResultPayload));
            RegisterBuiltInObjectEventSchemaSource<ElectionStartedEventPayload>(map, CoreConstants.EventTypeElectionStarted);
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeElectionStatusChanged, nameof(CoreJsonUtility.SerializeElectionStatusPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeGroupAppealPointsSpent, nameof(CoreJsonUtility.SerializeGroupAppealPointsSpentPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeGroupCreated, nameof(CoreJsonUtility.SerializeGroupLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeGroupDisbanded, nameof(CoreJsonUtility.SerializeGroupLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeGroupParamPointsChanged, nameof(CoreJsonUtility.SerializeGroupParamPointsPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeIdolDatingEnded, nameof(CoreJsonUtility.SerializeIdolRelationshipLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeIdolDatingStarted, nameof(CoreJsonUtility.SerializeIdolRelationshipLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeIdolDatingStatusChanged, nameof(CoreJsonUtility.SerializeIdolDatingStatusPayload));
            RegisterBuiltInObjectEventSchemaSource<IdolEarningsEventPayload>(map, CoreConstants.EventTypeIdolEarningsRecorded);
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeIdolGraduated, nameof(CoreJsonUtility.SerializeIdolLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeIdolGraduationAnnounced, nameof(CoreJsonUtility.SerializeIdolLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeIdolGroupTransferred, nameof(CoreJsonUtility.SerializeIdolGroupTransferPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeIdolHired, nameof(CoreJsonUtility.SerializeIdolLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeIdolRelationshipStatusChanged, nameof(CoreJsonUtility.SerializeIdolRelationshipStatusChangePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeIdolSalaryChanged, nameof(CoreJsonUtility.SerializeIdolSalaryChangePayload));
            RegisterBuiltInObjectEventSchemaSource<InfluenceBlackmailEventPayload>(map, CoreConstants.EventTypeInfluenceBlackmailQueued);
            RegisterBuiltInObjectEventSchemaSource<InfluenceBlackmailEventPayload>(map, CoreConstants.EventTypeInfluenceBlackmailTriggered);
            RegisterBuiltInObjectEventSchemaSource<LoanLifecycleEventPayload>(map, CoreConstants.EventTypeLoanAdded);
            RegisterBuiltInObjectEventSchemaSource<LoanLifecycleEventPayload>(map, CoreConstants.EventTypeLoanInitialized);
            RegisterBuiltInObjectEventSchemaSource<LoanLifecycleEventPayload>(map, CoreConstants.EventTypeLoanPaidOff);
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeMedicalDepression, nameof(CoreJsonUtility.SerializeMedicalEventPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeMedicalHealed, nameof(CoreJsonUtility.SerializeMedicalEventPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeMedicalHiatusFinished, nameof(CoreJsonUtility.SerializeMedicalEventPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeMedicalHiatusStarted, nameof(CoreJsonUtility.SerializeMedicalEventPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeMedicalInjury, nameof(CoreJsonUtility.SerializeMedicalEventPayload));
            RegisterBuiltInObjectEventSchemaSource<MentorshipLifecycleEventPayload>(map, CoreConstants.EventTypeMentorshipEnded);
            RegisterBuiltInObjectEventSchemaSource<MentorshipLifecycleEventPayload>(map, CoreConstants.EventTypeMentorshipStarted);
            RegisterBuiltInObjectEventSchemaSource<MentorshipLifecycleEventPayload>(map, CoreConstants.EventTypeMentorshipWeeklyTick);
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypePlayerDateInteraction, nameof(CoreJsonUtility.SerializePlayerDateInteractionPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypePlayerMarriageOutcome, nameof(CoreJsonUtility.SerializePlayerMarriageOutcomePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypePlayerRelationshipChanged, nameof(CoreJsonUtility.SerializePlayerRelationshipDeltaPayload));
            RegisterBuiltInObjectEventSchemaSource<PolicyDecisionEventPayload>(map, CoreConstants.EventTypePolicyDecisionSelected);
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypePushWindowDayIncrement, nameof(CoreJsonUtility.SerializePushLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypePushWindowEnded, nameof(CoreJsonUtility.SerializePushLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypePushWindowStarted, nameof(CoreJsonUtility.SerializePushLifecyclePayload));
            RegisterBuiltInObjectEventSchemaSource<RandomEventConcludedEventPayload>(map, CoreConstants.EventTypeRandomEventConcluded);
            RegisterBuiltInObjectEventSchemaSource<RandomEventStartedEventPayload>(map, CoreConstants.EventTypeRandomEventStarted);
            RegisterBuiltInObjectEventSchemaSource<ResearchParamAssignmentEventPayload>(map, CoreConstants.EventTypeResearchParamAssigned);
            RegisterBuiltInObjectEventSchemaSource<ResearchParamLevelUpEventPayload>(map, CoreConstants.EventTypeResearchParamLevelUp);
            RegisterBuiltInObjectEventSchemaSource<ResearchPointsAccruedEventPayload>(map, CoreConstants.EventTypeResearchPointsAccrued);
            RegisterBuiltInObjectEventSchemaSource<ResearchPointsPurchaseEventPayload>(map, CoreConstants.EventTypeResearchPointsPurchased);
            RegisterBuiltInObjectEventSchemaSource<RivalMarketEventPayload>(map, CoreConstants.EventTypeRivalMonthlyRecalculated);
            RegisterBuiltInObjectEventSchemaSource<RivalMarketEventPayload>(map, CoreConstants.EventTypeRivalTrendsUpdated);
            RegisterBuiltInObjectEventSchemaSource<ScandalCheckEventPayload>(map, CoreConstants.EventTypeScandalCheck);
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeScandalMitigated, nameof(CoreJsonUtility.SerializeScandalMitigationPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeScandalPointsChanged, nameof(CoreJsonUtility.SerializeScandalPointsPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeShowCancelled, nameof(CoreJsonUtility.SerializeShowLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeShowCastChanged, nameof(CoreJsonUtility.SerializeShowCastChangePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeShowConfigurationChanged, nameof(CoreJsonUtility.SerializeShowConfigurationChangePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeShowCreated, nameof(CoreJsonUtility.SerializeShowLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeShowEpisode, nameof(CoreJsonUtility.SerializeShowEpisodePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeShowEpisodeReleased, nameof(CoreJsonUtility.SerializeShowEpisodePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeShowRelaunchFinished, nameof(CoreJsonUtility.SerializeShowLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeShowRelaunchStarted, nameof(CoreJsonUtility.SerializeShowLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeShowReleased, nameof(CoreJsonUtility.SerializeShowLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeShowStatusChanged, nameof(CoreJsonUtility.SerializeShowStatusPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeSingleCancelled, nameof(CoreJsonUtility.SerializeSingleLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeSingleCastChanged, nameof(CoreJsonUtility.SerializeSingleCastChangePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeSingleCreated, nameof(CoreJsonUtility.SerializeSingleLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeSingleGroupChanged, nameof(CoreJsonUtility.SerializeSingleGroupChangePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeSingleParticipationRecorded, nameof(CoreJsonUtility.SerializeSingleParticipationPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeSingleReleased, nameof(CoreJsonUtility.SerializeSingleParticipationPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeSingleStatusChanged, nameof(CoreJsonUtility.SerializeSingleStatusPayload));
            RegisterBuiltInObjectEventSchemaSource<StaffLifecycleEventPayload>(map, CoreConstants.EventTypeStaffFired);
            RegisterBuiltInObjectEventSchemaSource<StaffLifecycleEventPayload>(map, CoreConstants.EventTypeStaffFiredSeverance);
            RegisterBuiltInObjectEventSchemaSource<StaffLifecycleEventPayload>(map, CoreConstants.EventTypeStaffHired);
            RegisterBuiltInObjectEventSchemaSource<StaffLifecycleEventPayload>(map, CoreConstants.EventTypeStaffLevelUp);
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeStatusChanged, nameof(CoreJsonUtility.SerializeStatusTransitionPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeStatusChangedLegacy, nameof(CoreJsonUtility.SerializeStatusTransitionPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeStatusEnded, nameof(CoreJsonUtility.SerializeStatusTransitionPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeStatusStarted, nameof(CoreJsonUtility.SerializeStatusTransitionPayload));
            RegisterBuiltInObjectEventSchemaSource<StoryRouteLockEventPayload>(map, CoreConstants.EventTypeStoryRouteLocked);
            RegisterBuiltInObjectEventSchemaSource<SubstoryLifecycleEventPayload>(map, CoreConstants.EventTypeSubstoryCompleted);
            RegisterBuiltInObjectEventSchemaSource<SubstoryLifecycleEventPayload>(map, CoreConstants.EventTypeSubstoryDelayed);
            RegisterBuiltInObjectEventSchemaSource<SubstoryLifecycleEventPayload>(map, CoreConstants.EventTypeSubstoryStarted);
            RegisterBuiltInObjectEventSchemaSource<SummerGamesFinalizedEventPayload>(map, CoreConstants.EventTypeSummerGamesFinalized);
            RegisterBuiltInObjectEventSchemaSource<TaskLifecycleEventPayload>(map, CoreConstants.EventTypeTaskCompleted);
            RegisterBuiltInObjectEventSchemaSource<TaskLifecycleEventPayload>(map, CoreConstants.EventTypeTaskDone);
            RegisterBuiltInObjectEventSchemaSource<TaskLifecycleEventPayload>(map, CoreConstants.EventTypeTaskFailed);
            RegisterBuiltInObjectEventSchemaSource<TheaterLifecycleEventPayload>(map, CoreConstants.EventTypeTheaterCreated);
            RegisterBuiltInObjectEventSchemaSource<TheaterDailyResultEventPayload>(map, CoreConstants.EventTypeTheaterDailyResult);
            RegisterBuiltInObjectEventSchemaSource<TheaterLifecycleEventPayload>(map, CoreConstants.EventTypeTheaterDestroyed);
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeTourCancelled, nameof(CoreJsonUtility.SerializeTourLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeTourCountryResult, nameof(CoreJsonUtility.SerializeTourCountryResultPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeTourCreated, nameof(CoreJsonUtility.SerializeTourLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeTourFinished, nameof(CoreJsonUtility.SerializeTourLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeTourParticipation, nameof(CoreJsonUtility.SerializeTourParticipationPayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeTourStarted, nameof(CoreJsonUtility.SerializeTourLifecyclePayload));
            RegisterBuiltInEventSchemaSource(map, CoreConstants.EventTypeTourStatusChanged, nameof(CoreJsonUtility.SerializeTourStatusPayload));
            RegisterBuiltInObjectEventSchemaSource<WishLifecycleEventPayload>(map, CoreConstants.EventTypeWishDone);
            RegisterBuiltInObjectEventSchemaSource<WishLifecycleEventPayload>(map, CoreConstants.EventTypeWishFulfilled);
            RegisterBuiltInObjectEventSchemaSource<WishLifecycleEventPayload>(map, CoreConstants.EventTypeWishGenerated);

            return map;
        }

        private static void RegisterBuiltInEventSchemaSource(
            Dictionary<string, BuiltInEventSchemaSource> map,
            string eventType,
            string serializerMethodName)
        {
            RegisterBuiltInEventSchemaSource(map, eventType, serializerMethodName, null);
        }

        private static void RegisterBuiltInObjectEventSchemaSource<TPayload>(
            Dictionary<string, BuiltInEventSchemaSource> map,
            string eventType)
        {
            RegisterBuiltInEventSchemaSource(
                map,
                eventType,
                SerializerMethodNameObjectPayload,
                typeof(TPayload));
        }

        private static void RegisterBuiltInEventSchemaSource(
            Dictionary<string, BuiltInEventSchemaSource> map,
            string eventType,
            string serializerMethodName,
            Type payloadType)
        {
            if (map == null || string.IsNullOrEmpty(eventType) || string.IsNullOrEmpty(serializerMethodName))
            {
                return;
            }

            map[eventType] = new BuiltInEventSchemaSource(serializerMethodName, payloadType);
        }

        public bool Initialize(string databasePath, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(databasePath))
            {
                errorMessage = CoreConstants.MessageDatabasePathEmpty;
                return false;
            }

            try
            {
                string runtimeProbeErrorMessage;
                if (!TryProbeRuntime(out runtimeProbeErrorMessage))
                {
                    errorMessage = runtimeProbeErrorMessage;
                    return false;
                }

                string directoryPath = Path.GetDirectoryName(databasePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                InitializeDatabaseCore(databasePath);

                CoreLog.Info(
                    CoreConstants.MessageSqliteEngineInitializedProviderPrefix +
                    NativeProviderName +
                    CoreConstants.MessageSqliteEngineInitializedProviderSuffix);
                return true;
            }
            catch (Exception exception)
            {
                TryCloseHandleQuietly();

                string recoveryErrorMessage;
                if (TryInitializeAfterDatabaseReset(databasePath, exception, out recoveryErrorMessage))
                {
                    CoreLog.Info(
                        CoreConstants.MessageSqliteEngineInitializedProviderPrefix +
                        NativeProviderName +
                        CoreConstants.MessageSqliteEngineInitializedProviderSuffix);
                    return true;
                }

                errorMessage = CoreConstants.MessageStorageInitializationFailure + exception.Message;
                if (!string.IsNullOrEmpty(recoveryErrorMessage))
                {
                    errorMessage += CoreConstants.MessageRecoveryAttemptFailedPrefix + recoveryErrorMessage;
                }

                CoreLog.Error(errorMessage);
                TryCloseHandleQuietly();
                return false;
            }
        }

        /// <summary>
        /// Tries a one-time recovery by recreating SQLite files after initialization failure.
        /// </summary>
        private bool TryInitializeAfterDatabaseReset(string databasePath, Exception firstInitializationException, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(databasePath))
            {
                return false;
            }

            string walPath = databasePath + DatabaseWriteAheadLogFileSuffix;
            string shmPath = databasePath + DatabaseSharedMemoryFileSuffix;
            bool anyDatabaseArtifactExists =
                File.Exists(databasePath) ||
                File.Exists(walPath) ||
                File.Exists(shmPath);
            if (!anyDatabaseArtifactExists)
            {
                return false;
            }

            try
            {
                TryCloseHandleQuietly();

                DeleteFileIfExists(databasePath);
                DeleteFileIfExists(walPath);
                DeleteFileIfExists(shmPath);

                InitializeDatabaseCore(databasePath);
                CoreLog.Warn(CoreConstants.MessageRecoveredSqliteInitializationPrefix + firstInitializationException.Message);
                return true;
            }
            catch (Exception recoveryException)
            {
                errorMessage = recoveryException.Message;
                TryCloseHandleQuietly();
                return false;
            }
        }

        /// <summary>
        /// Deletes a file if present.
        /// </summary>
        private static void DeleteFileIfExists(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// Opens SQLite handle and initializes schema/meta structures.
        /// </summary>
        private void InitializeDatabaseCore(string databasePath)
        {
            int openResult = NativeMethods.sqlite3_open16(databasePath, out databaseHandle);
            if (openResult != SqliteOk || databaseHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException(BuildSqliteErrorMessage(openResult));
            }

            int busyTimeoutResult = NativeMethods.sqlite3_busy_timeout(databaseHandle, DefaultBusyTimeoutMilliseconds);
            if (busyTimeoutResult != SqliteOk)
            {
                throw CreateSqliteException(busyTimeoutResult);
            }

            builtInEventTableColumnsByTableName.Clear();
            builtInEventSchemaRowsByEventType.Clear();

            // Treat runtime pragma incompatibilities as non-fatal and keep going.
            try
            {
                ExecuteNonQuery(CoreConstants.SqlPragmaJournalMode);
            }
            catch (Exception exception)
            {
                CoreLog.Warn(CoreConstants.MessageFailedEnableWalFallbackDeletePrefix + exception.Message);
                TryExecuteNonQueryBestEffort(SqlPragmaJournalModeDelete);
            }

            TryExecuteNonQueryBestEffort(CoreConstants.SqlPragmaSynchronous);
            TryExecuteNonQueryBestEffort(CoreConstants.SqlPragmaForeignKeys);

            ExecuteNonQuery(CoreConstants.SqlCreateTableMeta);
            ExecuteNonQuery(CoreConstants.SqlCreateTableEventStream);
            ExecuteNonQuery(CoreConstants.SqlCreateTableSingleParticipation);
            ExecuteNonQuery(CoreConstants.SqlCreateTableStatusWindow);
            ExecuteNonQuery(CoreConstants.SqlCreateTableShowCastWindow);
            ExecuteNonQuery(CoreConstants.SqlCreateTableContractWindow);
            ExecuteNonQuery(CoreConstants.SqlCreateTableRelationshipWindow);
            ExecuteNonQuery(CoreConstants.SqlCreateTableTourParticipation);
            ExecuteNonQuery(CoreConstants.SqlCreateTableAwardResultProjection);
            ExecuteNonQuery(CoreConstants.SqlCreateTableElectionResultProjection);
            ExecuteNonQuery(CoreConstants.SqlCreateTablePushWindow);
            ExecuteNonQuery(CoreConstants.SqlCreateTableCustomData);
            EnsureEventNamespaceColumnExists();

            TryExecuteNonQueryBestEffort(CoreConstants.SqlCreateIndexEventIdolDate);
            TryExecuteNonQueryBestEffort(CoreConstants.SqlCreateIndexEventTypeDate);
            TryExecuteNonQueryBestEffort(CoreConstants.SqlCreateIndexSingleParticipation);
            TryExecuteNonQueryBestEffort(CoreConstants.SqlCreateIndexStatusWindow);
            TryExecuteNonQueryBestEffort(CoreConstants.SqlCreateIndexShowCastWindow);
            TryExecuteNonQueryBestEffort(CoreConstants.SqlCreateIndexContractWindow);
            TryExecuteNonQueryBestEffort(CoreConstants.SqlCreateIndexRelationshipWindow);
            TryExecuteNonQueryBestEffort(CoreConstants.SqlCreateIndexTourParticipation);
            TryExecuteNonQueryBestEffort(CoreConstants.SqlCreateIndexAwardResultProjection);
            TryExecuteNonQueryBestEffort(CoreConstants.SqlCreateIndexElectionResultProjection);
            TryExecuteNonQueryBestEffort(CoreConstants.SqlCreateIndexPushWindow);
            TryExecuteNonQueryBestEffort(CoreConstants.SqlCreateIndexCustomData);

            // Pre-create one typed table per built-in event kind.
            EnsureAllBuiltInEventTypeTablesExist();

            // Avoid parameter-binding edge cases during bootstrap metadata writes.
            ExecuteNonQuery(SqlInsertMetaSchemaVersionLiteral);
            ExecuteNonQuery(SqlInsertMetaProviderLiteral);
        }

        /// <summary>
        /// Ensures event-stream namespace attribution column exists for additive schema upgrades.
        /// </summary>
        private void EnsureEventNamespaceColumnExists()
        {
            object scalar = ExecuteScalar(CoreConstants.SqlSelectEventNamespaceColumnExists);
            int existingCount = scalar == null || scalar == DBNull.Value
                ? CoreConstants.ZeroBasedListStartIndex
                : Convert.ToInt32(scalar, CultureInfo.InvariantCulture);

            if (existingCount > CoreConstants.ZeroBasedListStartIndex)
            {
                return;
            }

            ExecuteNonQuery(CoreConstants.SqlAlterTableEventStreamAddNamespaceIdentifier);
        }

        /// <summary>
        /// Executes one statement and logs warning on failure without aborting initialization.
        /// </summary>
        private void TryExecuteNonQueryBestEffort(string commandText)
        {
            try
            {
                ExecuteNonQuery(commandText);
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    CoreConstants.MessageSqliteOptionalStatementFailedPrefix
                    + BuildSqlPreview(commandText)
                    + CoreConstants.MessageSqliteOptionalStatementFailedDetailsSeparator
                    + exception.Message);
            }
        }

        private void PersistBuiltInEventPayloadFields(
            long eventId,
            string saveKey,
            int gameDateKey,
            string gameDateTime,
            int idolId,
            string entityKind,
            string entityId,
            string eventType,
            string sourcePatch,
            string payloadJson)
        {
            if (eventId <= 0L)
            {
                return;
            }

            List<EventPayloadFieldRow> rows = ExtractBuiltInPayloadFieldRows(payloadJson);
            string tableName = BuildBuiltInEventTableName(eventType);
            EnsureBuiltInEventTableSchema(eventType);
            UpsertBuiltInEventTypedRow(
                tableName,
                eventId,
                saveKey,
                gameDateKey,
                gameDateTime,
                idolId,
                entityKind,
                entityId,
                sourcePatch,
                rows);
        }

        private void EnsureBuiltInEventTableSchema(string eventType)
        {
            string tableName = BuildBuiltInEventTableName(eventType);
            HashSet<string> knownColumns;
            if (builtInEventTableColumnsByTableName.TryGetValue(tableName, out knownColumns))
            {
                return;
            }

            List<EventPayloadFieldRow> schemaRows = ResolveBuiltInEventSchemaRows(eventType);
            ExecuteNonQuery(BuildCreateBuiltInEventTableSql(tableName, schemaRows));
            knownColumns = ReadTableColumns(tableName);
            builtInEventTableColumnsByTableName[tableName] = knownColumns;
        }

        private void EnsureAllBuiltInEventTypeTablesExist()
        {
            FieldInfo[] coreConstantFields = typeof(CoreConstants).GetFields(BindingFlags.NonPublic | BindingFlags.Static);
            for (int fieldIndex = CoreConstants.ZeroBasedListStartIndex; fieldIndex < coreConstantFields.Length; fieldIndex++)
            {
                FieldInfo field = coreConstantFields[fieldIndex];
                if (field == null || field.FieldType != typeof(string))
                {
                    continue;
                }

                if (!field.IsLiteral || field.IsInitOnly || !field.Name.StartsWith(EventTypeConstantFieldPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                string eventType = Convert.ToString(field.GetRawConstantValue(), CultureInfo.InvariantCulture);
                if (string.IsNullOrEmpty(eventType))
                {
                    continue;
                }

                EnsureBuiltInEventTableSchema(eventType);
            }
        }

        private List<EventPayloadFieldRow> ResolveBuiltInEventSchemaRows(string eventType)
        {
            string normalizedEventType = eventType ?? string.Empty;
            List<EventPayloadFieldRow> cachedRows;
            if (builtInEventSchemaRowsByEventType.TryGetValue(normalizedEventType, out cachedRows))
            {
                return cachedRows;
            }

            List<EventPayloadFieldRow> schemaRows = GenerateBuiltInEventSchemaRows(normalizedEventType);
            builtInEventSchemaRowsByEventType[normalizedEventType] = schemaRows;
            return schemaRows;
        }

        private static List<EventPayloadFieldRow> GenerateBuiltInEventSchemaRows(string eventType)
        {
            List<EventPayloadFieldRow> schemaRows = new List<EventPayloadFieldRow>();
            if (string.IsNullOrEmpty(eventType))
            {
                return schemaRows;
            }

            BuiltInEventSchemaSource schemaSource;
            if (!BuiltInEventSchemaSourcesByEventType.TryGetValue(eventType, out schemaSource))
            {
                return schemaRows;
            }

            string samplePayloadJson = CreateSamplePayloadJsonForSchema(schemaSource);
            if (string.IsNullOrEmpty(samplePayloadJson))
            {
                return schemaRows;
            }

            List<EventPayloadFieldRow> parsedRows = ExtractBuiltInPayloadFieldRows(samplePayloadJson);
            if (parsedRows == null || parsedRows.Count == CoreConstants.ZeroBasedListStartIndex)
            {
                return schemaRows;
            }

            HashSet<string> seenFieldKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int rowIndex = CoreConstants.ZeroBasedListStartIndex; rowIndex < parsedRows.Count; rowIndex++)
            {
                EventPayloadFieldRow parsedRow = parsedRows[rowIndex];
                if (parsedRow == null || string.IsNullOrEmpty(parsedRow.FieldKey))
                {
                    continue;
                }

                if (seenFieldKeys.Contains(parsedRow.FieldKey))
                {
                    continue;
                }

                seenFieldKeys.Add(parsedRow.FieldKey);
                schemaRows.Add(parsedRow);
            }

            return schemaRows;
        }

        private static string CreateSamplePayloadJsonForSchema(BuiltInEventSchemaSource schemaSource)
        {
            if (schemaSource == null || string.IsNullOrEmpty(schemaSource.SerializerMethodName))
            {
                return CoreConstants.EmptyJsonObject;
            }

            MethodInfo serializerMethod = typeof(CoreJsonUtility).GetMethod(
                schemaSource.SerializerMethodName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            if (serializerMethod == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            Type payloadType;
            if (string.Equals(schemaSource.SerializerMethodName, SerializerMethodNameObjectPayload, StringComparison.Ordinal))
            {
                payloadType = schemaSource.PayloadType;
                if (payloadType == null)
                {
                    return CoreConstants.EmptyJsonObject;
                }
            }
            else
            {
                ParameterInfo[] parameters = serializerMethod.GetParameters();
                if (parameters == null || parameters.Length != CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    return CoreConstants.EmptyJsonObject;
                }

                payloadType = parameters[CoreConstants.ZeroBasedListStartIndex].ParameterType;
            }

            object payloadInstance = CreateSamplePayloadInstance(payloadType);
            if (payloadInstance == null)
            {
                return CoreConstants.EmptyJsonObject;
            }

            try
            {
                object serialized = serializerMethod.Invoke(null, new object[] { payloadInstance });
                string payloadJson = serialized == null
                    ? CoreConstants.EmptyJsonObject
                    : Convert.ToString(serialized, CultureInfo.InvariantCulture);
                return string.IsNullOrEmpty(payloadJson) ? CoreConstants.EmptyJsonObject : payloadJson;
            }
            catch
            {
                return CoreConstants.EmptyJsonObject;
            }
        }

        private static object CreateSamplePayloadInstance(Type payloadType)
        {
            if (payloadType == null)
            {
                return null;
            }

            object payloadInstance;
            try
            {
                payloadInstance = Activator.CreateInstance(payloadType);
            }
            catch
            {
                return null;
            }

            FieldInfo[] fields = payloadType.GetFields(BindingFlags.Instance | BindingFlags.Public);
            for (int fieldIndex = CoreConstants.ZeroBasedListStartIndex; fieldIndex < fields.Length; fieldIndex++)
            {
                FieldInfo field = fields[fieldIndex];
                if (field == null || field.IsInitOnly)
                {
                    continue;
                }

                object sampleValue = BuildSampleFieldValue(field.FieldType);
                if (sampleValue == null && field.FieldType.IsValueType)
                {
                    continue;
                }

                try
                {
                    field.SetValue(payloadInstance, sampleValue);
                }
                catch
                {
                    // Best-effort sample population: leave default if assignment fails.
                }
            }

            return payloadInstance;
        }

        private static object BuildSampleFieldValue(Type fieldType)
        {
            if (fieldType == null)
            {
                return null;
            }

            if (fieldType == typeof(string))
            {
                return SchemaSampleStringValue;
            }

            if (fieldType == typeof(bool))
            {
                return SchemaSampleBooleanValue;
            }

            if (fieldType == typeof(int))
            {
                return SchemaSampleInt32Value;
            }

            if (fieldType == typeof(long))
            {
                return SchemaSampleInt64Value;
            }

            if (fieldType == typeof(float))
            {
                return SchemaSampleSingleValue;
            }

            if (fieldType == typeof(double))
            {
                return SchemaSampleDoubleValue;
            }

            if (fieldType == typeof(short))
            {
                return SchemaSampleInt16Value;
            }

            if (fieldType == typeof(byte))
            {
                return SchemaSampleByteValue;
            }

            if (fieldType.IsEnum)
            {
                Array values = Enum.GetValues(fieldType);
                if (values != null && values.Length > CoreConstants.ZeroBasedListStartIndex)
                {
                    return values.GetValue(CoreConstants.ZeroBasedListStartIndex);
                }
            }

            return null;
        }

        private HashSet<string> ReadTableColumns(string tableName)
        {
            HashSet<string> columns = new HashSet<string>(StringComparer.Ordinal);
            string pragmaSql = SqlPragmaTableInfoPrefix + QuoteSqlIdentifier(tableName) + SqlPragmaTableInfoSuffix;
            IntPtr statementHandle = PrepareStatement(pragmaSql);
            try
            {
                while (true)
                {
                    int stepResult = NativeMethods.sqlite3_step(statementHandle);
                    if (stepResult == SqliteDone)
                    {
                        break;
                    }

                    if (stepResult != SqliteRow)
                    {
                        throw CreateSqliteException(stepResult, pragmaSql);
                    }

                    if (!IsColumnNull(statementHandle, SqlPragmaTableInfoNameColumnIndex))
                    {
                        string columnName = GetColumnText(statementHandle, SqlPragmaTableInfoNameColumnIndex);
                        if (!string.IsNullOrEmpty(columnName))
                        {
                            columns.Add(columnName);
                        }
                    }
                }
            }
            finally
            {
                FinalizeStatement(statementHandle);
            }

            return columns;
        }

        private List<string> ReadUserTableNames()
        {
            List<string> tableNames = new List<string>();
            IntPtr statementHandle = PrepareStatement(SqlSelectUserTableNames);
            try
            {
                while (true)
                {
                    int stepResult = NativeMethods.sqlite3_step(statementHandle);
                    if (stepResult == SqliteDone)
                    {
                        break;
                    }

                    if (stepResult != SqliteRow)
                    {
                        throw CreateSqliteException(stepResult, SqlSelectUserTableNames);
                    }

                    if (IsColumnNull(statementHandle, SqlSelectUserTableNamesNameColumnIndex))
                    {
                        continue;
                    }

                    string tableName = GetColumnText(statementHandle, SqlSelectUserTableNamesNameColumnIndex);
                    if (!string.IsNullOrEmpty(tableName))
                    {
                        tableNames.Add(tableName);
                    }
                }
            }
            finally
            {
                FinalizeStatement(statementHandle);
            }

            return tableNames;
        }

        private static string BuildRemapSaveKeySql(string tableName, bool remapAllSourceKeys)
        {
            string quotedTableName = QuoteSqlIdentifier(tableName);
            string quotedSaveKeyColumn = QuoteSqlIdentifier(SqlColumnSaveKey);
            string whereClause = remapAllSourceKeys
                ? quotedSaveKeyColumn + " <> " + SqlParameterTargetSaveKey
                : quotedSaveKeyColumn + " = " + SqlParameterSourceSaveKey;

            return "UPDATE OR REPLACE "
                + quotedTableName
                + " SET "
                + quotedSaveKeyColumn
                + " = "
                + SqlParameterTargetSaveKey
                + " WHERE "
                + whereClause
                + SqlStatementTerminator;
        }

        private static string BuildCreateBuiltInEventTableSql(string tableName, IReadOnlyList<EventPayloadFieldRow> schemaRows)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(SqlCreateTableIfNotExistsPrefix).Append(QuoteSqlIdentifier(tableName)).Append(SqlCreateTableOpenParenthesis);
            builder.Append(SqlColumnEventId).Append(SqlSpaceSeparator).Append(SqlTypeInteger).Append(SqlPrimaryKeyConstraint).Append(SqlColumnSeparator);
            builder.Append(SqlColumnSaveKey).Append(SqlSpaceSeparator).Append(SqlTypeText).Append(SqlNotNullConstraint).Append(SqlColumnSeparator);
            builder.Append(SqlColumnGameDateKey).Append(SqlSpaceSeparator).Append(SqlTypeInteger).Append(SqlNotNullConstraint).Append(SqlColumnSeparator);
            builder.Append(SqlColumnGameDateTime).Append(SqlSpaceSeparator).Append(SqlTypeText).Append(SqlNotNullConstraint).Append(SqlColumnSeparator);
            builder.Append(SqlColumnIdolId).Append(SqlSpaceSeparator).Append(SqlTypeInteger).Append(SqlColumnSeparator);
            builder.Append(SqlColumnEntityKind).Append(SqlSpaceSeparator).Append(SqlTypeText).Append(SqlNotNullConstraint).Append(SqlColumnSeparator);
            builder.Append(SqlColumnEntityId).Append(SqlSpaceSeparator).Append(SqlTypeText).Append(SqlColumnSeparator);
            builder.Append(SqlColumnSourcePatch).Append(SqlSpaceSeparator).Append(SqlTypeText).Append(SqlNotNullConstraint);

            if (schemaRows != null && schemaRows.Count > CoreConstants.ZeroBasedListStartIndex)
            {
                HashSet<string> includedColumns = new HashSet<string>(StringComparer.Ordinal);
                for (int rowIndex = CoreConstants.ZeroBasedListStartIndex; rowIndex < schemaRows.Count; rowIndex++)
                {
                    EventPayloadFieldRow row = schemaRows[rowIndex];
                    if (row == null || string.IsNullOrEmpty(row.FieldKey))
                    {
                        continue;
                    }

                    string columnName = BuildPayloadColumnName(row.FieldKey);
                    if (includedColumns.Contains(columnName))
                    {
                        continue;
                    }

                    includedColumns.Add(columnName);
                    builder.Append(SqlColumnSeparator);
                    builder.Append(QuoteSqlIdentifier(columnName));
                    builder.Append(SqlSpaceSeparator);
                    builder.Append(ResolveSqlColumnType(row));
                }
            }

            builder.Append(SqlCreateTableCloseWithEventForeignKey);
            return builder.ToString();
        }

        private void UpsertBuiltInEventTypedRow(
            string tableName,
            long eventId,
            string saveKey,
            int gameDateKey,
            string gameDateTime,
            int idolId,
            string entityKind,
            string entityId,
            string sourcePatch,
            IReadOnlyList<EventPayloadFieldRow> rows)
        {
            HashSet<string> knownColumns;
            if (!builtInEventTableColumnsByTableName.TryGetValue(tableName, out knownColumns))
            {
                knownColumns = ReadTableColumns(tableName);
                builtInEventTableColumnsByTableName[tableName] = knownColumns;
            }

            SortedDictionary<string, EventPayloadFieldRow> payloadByColumnName = new SortedDictionary<string, EventPayloadFieldRow>(StringComparer.Ordinal);
            for (int rowIndex = CoreConstants.ZeroBasedListStartIndex; rowIndex < rows.Count; rowIndex++)
            {
                EventPayloadFieldRow row = rows[rowIndex];
                if (row == null || string.IsNullOrEmpty(row.FieldKey))
                {
                    continue;
                }

                string columnName = BuildPayloadColumnName(row.FieldKey);
                if (!knownColumns.Contains(columnName))
                {
                    continue;
                }

                payloadByColumnName[columnName] = row;
            }

            List<SqliteParameter> parameters = new List<SqliteParameter>();
            StringBuilder columnBuilder = new StringBuilder();
            StringBuilder valueBuilder = new StringBuilder();

            columnBuilder.Append(QuoteSqlIdentifier(SqlColumnEventId));
            valueBuilder.Append(CoreConstants.SqlParameterEventId);
            parameters.Add(CreateParameter(CoreConstants.SqlParameterEventId, eventId));

            columnBuilder.Append(SqlColumnSeparator).Append(QuoteSqlIdentifier(SqlColumnSaveKey));
            valueBuilder.Append(SqlColumnSeparator).Append(CoreConstants.SqlParameterSaveKey);
            parameters.Add(CreateParameter(CoreConstants.SqlParameterSaveKey, saveKey ?? string.Empty));

            columnBuilder.Append(SqlColumnSeparator).Append(QuoteSqlIdentifier(SqlColumnGameDateKey));
            valueBuilder.Append(SqlColumnSeparator).Append(CoreConstants.SqlParameterGameDateKey);
            parameters.Add(CreateParameter(CoreConstants.SqlParameterGameDateKey, gameDateKey));

            columnBuilder.Append(SqlColumnSeparator).Append(QuoteSqlIdentifier(SqlColumnGameDateTime));
            valueBuilder.Append(SqlColumnSeparator).Append(CoreConstants.SqlParameterGameDateTime);
            parameters.Add(CreateParameter(CoreConstants.SqlParameterGameDateTime, gameDateTime ?? string.Empty));

            columnBuilder.Append(SqlColumnSeparator).Append(QuoteSqlIdentifier(SqlColumnIdolId));
            valueBuilder.Append(SqlColumnSeparator).Append(CoreConstants.SqlParameterIdolId);
            parameters.Add(CreateParameter(
                CoreConstants.SqlParameterIdolId,
                idolId >= CoreConstants.MinimumValidIdolIdentifier ? (object)idolId : null));

            columnBuilder.Append(SqlColumnSeparator).Append(QuoteSqlIdentifier(SqlColumnEntityKind));
            valueBuilder.Append(SqlColumnSeparator).Append(CoreConstants.SqlParameterEntityKind);
            parameters.Add(CreateParameter(CoreConstants.SqlParameterEntityKind, entityKind ?? string.Empty));

            columnBuilder.Append(SqlColumnSeparator).Append(QuoteSqlIdentifier(SqlColumnEntityId));
            valueBuilder.Append(SqlColumnSeparator).Append(CoreConstants.SqlParameterEntityId);
            parameters.Add(CreateParameter(CoreConstants.SqlParameterEntityId, string.IsNullOrEmpty(entityId) ? null : entityId));

            columnBuilder.Append(SqlColumnSeparator).Append(QuoteSqlIdentifier(SqlColumnSourcePatch));
            valueBuilder.Append(SqlColumnSeparator).Append(CoreConstants.SqlParameterSourcePatch);
            parameters.Add(CreateParameter(CoreConstants.SqlParameterSourcePatch, sourcePatch ?? string.Empty));

            int parameterIndex = CoreConstants.ZeroBasedListStartIndex;
            foreach (KeyValuePair<string, EventPayloadFieldRow> pair in payloadByColumnName)
            {
                string parameterName = SqlPayloadParameterPrefix + parameterIndex.ToString(CultureInfo.InvariantCulture);
                columnBuilder.Append(SqlColumnSeparator).Append(QuoteSqlIdentifier(pair.Key));
                valueBuilder.Append(SqlColumnSeparator).Append(parameterName);
                parameters.Add(CreateParameter(parameterName, ResolveSqliteValue(pair.Value)));
                parameterIndex++;
            }

            string upsertSql =
                SqlInsertOrReplaceIntoPrefix + QuoteSqlIdentifier(tableName) +
                SqlOpenParenthesis + columnBuilder + SqlInsertValuesClauseSeparator + valueBuilder + SqlCloseParenthesis + SqlStatementTerminator;
            ExecuteNonQuery(upsertSql, parameters.ToArray());
        }

        private static string BuildBuiltInEventTableName(string eventType)
        {
            return BuiltInEventTableNamePrefix + SanitizeSqlIdentifierToken(eventType, BuiltInEventTableNameFallbackToken);
        }

        private static string BuildPayloadColumnName(string fieldKey)
        {
            return PayloadColumnNamePrefix + SanitizeSqlIdentifierToken(fieldKey, PayloadColumnNameFallbackToken);
        }

        private static string SanitizeSqlIdentifierToken(string token, string fallbackToken)
        {
            StringBuilder builder = new StringBuilder();
            string source = token ?? string.Empty;
            for (int characterIndex = CoreConstants.ZeroBasedListStartIndex; characterIndex < source.Length; characterIndex++)
            {
                char current = source[characterIndex];
                if (char.IsLetterOrDigit(current) || current == CoreConstants.TokenUnderscoreCharacter)
                {
                    builder.Append(char.ToLowerInvariant(current));
                }
                else
                {
                    builder.Append(CoreConstants.TokenUnderscoreCharacter);
                }
            }

            if (builder.Length == CoreConstants.ZeroBasedListStartIndex)
            {
                builder.Append(fallbackToken ?? SanitizeIdentifierFallbackToken);
            }

            if (char.IsDigit(builder[CoreConstants.ZeroBasedListStartIndex]))
            {
                builder.Insert(CoreConstants.ZeroBasedListStartIndex, CoreConstants.TokenUnderscoreCharacter);
            }

            return builder.ToString();
        }

        private static string QuoteSqlIdentifier(string identifier)
        {
            string token = identifier ?? string.Empty;
            return SqlIdentifierQuote + token.Replace(SqlIdentifierQuote, SqlIdentifierEscapedQuote) + SqlIdentifierQuote;
        }

        private static string ResolveSqlColumnType(EventPayloadFieldRow row)
        {
            if (row == null)
            {
                return SqlTypeText;
            }

            if (string.Equals(row.ValueKind, CoreConstants.PayloadValueKindBoolean, StringComparison.Ordinal))
            {
                return SqlTypeInteger;
            }

            if (string.Equals(row.ValueKind, CoreConstants.PayloadValueKindNumber, StringComparison.Ordinal))
            {
                string numericText = row.ValueText ?? string.Empty;
                return IsSqlRealNumericText(numericText) ? SqlTypeReal : SqlTypeInteger;
            }

            return SqlTypeText;
        }

        private static bool IsSqlRealNumericText(string numericText)
        {
            return numericText.IndexOf(NumericDecimalPointCharacter) >= CoreConstants.ZeroBasedListStartIndex
                || numericText.IndexOf(NumericExponentLowerCharacter) >= CoreConstants.ZeroBasedListStartIndex
                || numericText.IndexOf(NumericExponentUpperCharacter) >= CoreConstants.ZeroBasedListStartIndex;
        }

        private static object ResolveSqliteValue(EventPayloadFieldRow row)
        {
            if (row == null)
            {
                return string.Empty;
            }

            if (string.Equals(row.ValueKind, CoreConstants.PayloadValueKindNull, StringComparison.Ordinal))
            {
                return null;
            }

            if (string.Equals(row.ValueKind, CoreConstants.PayloadValueKindBoolean, StringComparison.Ordinal))
            {
                return string.Equals(row.ValueText, CoreConstants.JsonBooleanTrue, StringComparison.Ordinal)
                    ? SqliteBooleanTrueValue
                    : SqliteBooleanFalseValue;
            }

            if (string.Equals(row.ValueKind, CoreConstants.PayloadValueKindNumber, StringComparison.Ordinal))
            {
                string numericText = row.ValueText ?? string.Empty;
                if (IsSqlRealNumericText(numericText))
                {
                    double doubleValue;
                    if (double.TryParse(numericText, NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue))
                    {
                        return doubleValue;
                    }

                    return numericText;
                }

                long longValue;
                if (long.TryParse(numericText, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue))
                {
                    return longValue;
                }

                return numericText;
            }

            return row.ValueText ?? string.Empty;
        }

        private static List<EventPayloadFieldRow> ExtractBuiltInPayloadFieldRows(string payloadJson)
        {
            List<EventPayloadFieldRow> rows = new List<EventPayloadFieldRow>();
            if (string.IsNullOrEmpty(payloadJson))
            {
                return rows;
            }

            string trimmedPayload = payloadJson.Trim();
            if (trimmedPayload.Length == CoreConstants.ZeroBasedListStartIndex
                || string.Equals(trimmedPayload, CoreConstants.EmptyJsonObject, StringComparison.Ordinal))
            {
                return rows;
            }

            if (TryParseTopLevelJsonObject(trimmedPayload, rows))
            {
                return rows;
            }

            rows.Clear();
            rows.Add(
                new EventPayloadFieldRow
                {
                    FieldKey = CoreConstants.PayloadFieldKeyRawJson,
                    ValueKind = CoreConstants.PayloadValueKindRaw,
                    ValueText = payloadJson
                });
            return rows;
        }

        private static bool TryParseTopLevelJsonObject(string jsonText, List<EventPayloadFieldRow> rows)
        {
            if (rows == null || string.IsNullOrEmpty(jsonText))
            {
                return false;
            }

            int index = CoreConstants.ZeroBasedListStartIndex;
            SkipJsonWhitespace(jsonText, ref index);
            if (index >= jsonText.Length || jsonText[index] != CoreConstants.JsonObjectStartCharacter)
            {
                return false;
            }

            index++;
            while (index < jsonText.Length)
            {
                SkipJsonWhitespace(jsonText, ref index);
                if (index < jsonText.Length && jsonText[index] == CoreConstants.JsonObjectEndCharacter)
                {
                    index++;
                    SkipJsonWhitespace(jsonText, ref index);
                    return index == jsonText.Length;
                }

                string fieldKey;
                if (!TryReadJsonString(jsonText, ref index, out fieldKey))
                {
                    return false;
                }

                SkipJsonWhitespace(jsonText, ref index);
                if (index >= jsonText.Length || jsonText[index] != CoreConstants.JsonNameValueSeparatorCharacter)
                {
                    return false;
                }

                index++;
                SkipJsonWhitespace(jsonText, ref index);

                EventPayloadFieldRow row;
                if (!TryReadJsonValueAsFieldRow(jsonText, ref index, fieldKey, out row))
                {
                    return false;
                }

                rows.Add(row);
                SkipJsonWhitespace(jsonText, ref index);
                if (index < jsonText.Length && jsonText[index] == CoreConstants.JsonPropertySeparatorCharacter)
                {
                    index++;
                    continue;
                }

                if (index < jsonText.Length && jsonText[index] == CoreConstants.JsonObjectEndCharacter)
                {
                    index++;
                    SkipJsonWhitespace(jsonText, ref index);
                    return index == jsonText.Length;
                }

                return false;
            }

            return false;
        }

        private static void SkipJsonWhitespace(string jsonText, ref int index)
        {
            while (index < jsonText.Length && char.IsWhiteSpace(jsonText[index]))
            {
                index++;
            }
        }

        private static bool TryReadJsonString(string jsonText, ref int index, out string value)
        {
            value = string.Empty;
            if (index >= jsonText.Length || jsonText[index] != CoreConstants.JsonStringQuoteCharacter)
            {
                return false;
            }

            index++;
            StringBuilder builder = new StringBuilder();
            while (index < jsonText.Length)
            {
                char currentCharacter = jsonText[index++];
                if (currentCharacter == CoreConstants.JsonStringQuoteCharacter)
                {
                    value = builder.ToString();
                    return true;
                }

                if (currentCharacter != CoreConstants.JsonEscapeCharacter)
                {
                    builder.Append(currentCharacter);
                    continue;
                }

                if (index >= jsonText.Length)
                {
                    return false;
                }

                char escapedCharacter = jsonText[index++];
                switch (escapedCharacter)
                {
                    case '"':
                        builder.Append('"');
                        break;
                    case '\\':
                        builder.Append('\\');
                        break;
                    case '/':
                        builder.Append('/');
                        break;
                    case 'b':
                        builder.Append('\b');
                        break;
                    case 'f':
                        builder.Append('\f');
                        break;
                    case 'n':
                        builder.Append(CoreConstants.JsonLineFeedCharacter);
                        break;
                    case 'r':
                        builder.Append(CoreConstants.JsonCarriageReturnCharacter);
                        break;
                    case 't':
                        builder.Append(CoreConstants.JsonTabCharacter);
                        break;
                    case 'u':
                        if (index + 4 > jsonText.Length)
                        {
                            return false;
                        }

                        string unicodeHex = jsonText.Substring(index, 4);
                        int codePoint;
                        if (!int.TryParse(unicodeHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out codePoint))
                        {
                            return false;
                        }

                        builder.Append((char)codePoint);
                        index += 4;
                        break;
                    default:
                        return false;
                }
            }

            return false;
        }

        private static bool TryReadJsonValueAsFieldRow(string jsonText, ref int index, string fieldKey, out EventPayloadFieldRow row)
        {
            row = null;
            if (index >= jsonText.Length)
            {
                return false;
            }

            char currentCharacter = jsonText[index];
            if (currentCharacter == CoreConstants.JsonStringQuoteCharacter)
            {
                string stringValue;
                if (!TryReadJsonString(jsonText, ref index, out stringValue))
                {
                    return false;
                }

                row = new EventPayloadFieldRow
                {
                    FieldKey = fieldKey ?? string.Empty,
                    ValueKind = CoreConstants.PayloadValueKindString,
                    ValueText = stringValue ?? string.Empty
                };
                return true;
            }

            if (currentCharacter == CoreConstants.JsonObjectStartCharacter || currentCharacter == '[')
            {
                string compositeValue;
                if (!TryReadJsonCompositeValue(jsonText, ref index, out compositeValue))
                {
                    return false;
                }

                row = new EventPayloadFieldRow
                {
                    FieldKey = fieldKey ?? string.Empty,
                    ValueKind = currentCharacter == CoreConstants.JsonObjectStartCharacter
                        ? CoreConstants.PayloadValueKindObject
                        : CoreConstants.PayloadValueKindArray,
                    ValueText = compositeValue ?? string.Empty
                };
                return true;
            }

            int startIndex = index;
            while (index < jsonText.Length)
            {
                char literalCharacter = jsonText[index];
                if (literalCharacter == CoreConstants.JsonPropertySeparatorCharacter || literalCharacter == CoreConstants.JsonObjectEndCharacter)
                {
                    break;
                }

                index++;
            }

            string literalValue = jsonText.Substring(startIndex, index - startIndex).Trim();
            if (literalValue.Length == CoreConstants.ZeroBasedListStartIndex)
            {
                return false;
            }

            string valueKind = CoreConstants.PayloadValueKindRaw;
            string valueText = literalValue;
            if (string.Equals(literalValue, CoreConstants.JsonBooleanTrue, StringComparison.Ordinal)
                || string.Equals(literalValue, CoreConstants.JsonBooleanFalse, StringComparison.Ordinal))
            {
                valueKind = CoreConstants.PayloadValueKindBoolean;
            }
            else if (string.Equals(literalValue, CoreConstants.JsonNullLiteral, StringComparison.Ordinal))
            {
                valueKind = CoreConstants.PayloadValueKindNull;
                valueText = string.Empty;
            }
            else
            {
                double numericValue;
                if (double.TryParse(literalValue, NumberStyles.Float, CultureInfo.InvariantCulture, out numericValue))
                {
                    valueKind = CoreConstants.PayloadValueKindNumber;
                }
            }

            row = new EventPayloadFieldRow
            {
                FieldKey = fieldKey ?? string.Empty,
                ValueKind = valueKind,
                ValueText = valueText
            };
            return true;
        }

        private static bool TryReadJsonCompositeValue(string jsonText, ref int index, out string value)
        {
            value = string.Empty;
            if (index >= jsonText.Length)
            {
                return false;
            }

            int startIndex = index;
            int objectDepth = CoreConstants.ZeroBasedListStartIndex;
            int arrayDepth = CoreConstants.ZeroBasedListStartIndex;

            while (index < jsonText.Length)
            {
                char currentCharacter = jsonText[index];
                if (currentCharacter == CoreConstants.JsonStringQuoteCharacter)
                {
                    string ignored;
                    if (!TryReadJsonString(jsonText, ref index, out ignored))
                    {
                        return false;
                    }

                    continue;
                }

                if (currentCharacter == CoreConstants.JsonObjectStartCharacter)
                {
                    objectDepth++;
                }
                else if (currentCharacter == CoreConstants.JsonObjectEndCharacter)
                {
                    objectDepth--;
                }
                else if (currentCharacter == '[')
                {
                    arrayDepth++;
                }
                else if (currentCharacter == ']')
                {
                    arrayDepth--;
                }

                index++;
                if (objectDepth == CoreConstants.ZeroBasedListStartIndex && arrayDepth == CoreConstants.ZeroBasedListStartIndex)
                {
                    value = jsonText.Substring(startIndex, index - startIndex);
                    return true;
                }

                if (objectDepth < CoreConstants.ZeroBasedListStartIndex || arrayDepth < CoreConstants.ZeroBasedListStartIndex)
                {
                    return false;
                }
            }

            return false;
        }

        public bool PersistBatch(
            IReadOnlyList<PendingEvent> pendingEvents,
            IReadOnlyList<SingleParticipationProjection> singleParticipationRows,
            IReadOnlyList<StatusTransitionProjection> statusTransitions,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            lock (databaseLock)
            {
                if (disposed)
                {
                    errorMessage = CoreConstants.MessageStorageEngineDisposed;
                    return false;
                }

                try
                {
                    ExecuteWithinTransaction(delegate
                    {
                        InsertEvents(pendingEvents);
                        UpsertSingleParticipationRows(singleParticipationRows);
                        ApplyStatusTransitions(statusTransitions);
                        ApplyDerivedReadModelProjections(pendingEvents);
                    });

                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = CoreConstants.MessagePersistBatchFailedPrefix + exception.Message;
                    CoreLog.Error(errorMessage);
                    return false;
                }
            }
        }

        public bool TrySetCustomData(string saveKey, string namespaceIdentifier, string dataKey, string jsonValue, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (jsonValue == null)
            {
                errorMessage = CoreConstants.MessageJsonValueNull;
                return false;
            }

            if (jsonValue.Length > CoreConstants.MaximumCustomValueCharacterCount)
            {
                errorMessage = CoreConstants.MessageJsonValueTooLong;
                return false;
            }

            lock (databaseLock)
            {
                if (disposed)
                {
                    errorMessage = CoreConstants.MessageStorageEngineDisposed;
                    return false;
                }

                try
                {
                    bool customDataAlreadyExists;
                    int existingLength = GetExistingCustomValueLength(saveKey, namespaceIdentifier, dataKey, out customDataAlreadyExists);
                    int existingKeyCount = GetCustomNamespaceKeyCount(saveKey, namespaceIdentifier);
                    int existingTotalLength = GetCustomNamespaceTotalLength(saveKey, namespaceIdentifier);

                    if (!customDataAlreadyExists && existingKeyCount >= CoreConstants.MaximumCustomKeysPerNamespace)
                    {
                        errorMessage = CoreConstants.MessageNamespaceKeyQuotaExceeded;
                        return false;
                    }

                    int projectedTotalLength = existingTotalLength - existingLength + jsonValue.Length;
                    if (projectedTotalLength > CoreConstants.MaximumNamespaceCharacterBudget)
                    {
                        errorMessage = CoreConstants.MessageNamespaceDataBudgetExceeded;
                        return false;
                    }

                    ExecuteWithinTransaction(delegate
                    {
                        ExecuteNonQuery(
                            CoreConstants.SqlUpsertCustomData,
                            CreateParameter(CoreConstants.SqlParameterSaveKey, saveKey),
                            CreateParameter(CoreConstants.SqlParameterNamespaceIdentifier, namespaceIdentifier),
                            CreateParameter(CoreConstants.SqlParameterDataKey, dataKey),
                            CreateParameter(CoreConstants.SqlParameterValueJson, jsonValue),
                            CreateParameter(CoreConstants.SqlParameterUpdatedUtc, CoreDateTimeUtility.ToUtcRoundTripString(DateTime.UtcNow)));
                    });

                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = CoreConstants.MessageTrySetCustomDataFailedPrefix + exception.Message;
                    CoreLog.Error(errorMessage);
                    return false;
                }
            }
        }

        public bool TryGetCustomData(string saveKey, string namespaceIdentifier, string dataKey, out string jsonValue, out string errorMessage)
        {
            jsonValue = string.Empty;
            errorMessage = string.Empty;

            lock (databaseLock)
            {
                if (disposed)
                {
                    errorMessage = CoreConstants.MessageStorageEngineDisposed;
                    return false;
                }

                try
                {
                    object scalarValue = ExecuteScalar(
                        CoreConstants.SqlSelectCustomData,
                        CreateParameter(CoreConstants.SqlParameterSaveKey, saveKey),
                        CreateParameter(CoreConstants.SqlParameterNamespaceIdentifier, namespaceIdentifier),
                        CreateParameter(CoreConstants.SqlParameterDataKey, dataKey));

                    if (scalarValue == null || scalarValue == DBNull.Value)
                    {
                        return false;
                    }

                    jsonValue = Convert.ToString(scalarValue, CultureInfo.InvariantCulture);
                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = CoreConstants.MessageTryGetCustomDataFailedPrefix + exception.Message;
                    CoreLog.Error(errorMessage);
                    return false;
                }
            }
        }

        public bool TryRemoveCustomData(string saveKey, string namespaceIdentifier, string dataKey, out string errorMessage)
        {
            errorMessage = string.Empty;
            lock (databaseLock)
            {
                if (disposed)
                {
                    errorMessage = CoreConstants.MessageStorageEngineDisposed;
                    return false;
                }

                try
                {
                    ExecuteNonQuery(
                        CoreConstants.SqlDeleteCustomData,
                        CreateParameter(CoreConstants.SqlParameterSaveKey, saveKey),
                        CreateParameter(CoreConstants.SqlParameterNamespaceIdentifier, namespaceIdentifier),
                        CreateParameter(CoreConstants.SqlParameterDataKey, dataKey));
                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = CoreConstants.MessageTryRemoveCustomDataFailedPrefix + exception.Message;
                    CoreLog.Error(errorMessage);
                    return false;
                }
            }
        }

        public bool TryReadRecentEventsForIdol(string saveKey, int idolId, int maxCount, out List<IMDataCoreEvent> events, out string errorMessage)
        {
            events = new List<IMDataCoreEvent>();
            errorMessage = string.Empty;

            lock (databaseLock)
            {
                if (disposed)
                {
                    errorMessage = CoreConstants.MessageStorageEngineDisposed;
                    return false;
                }

                try
                {
                    if (maxCount <= CoreConstants.ZeroBasedListStartIndex)
                    {
                        return true;
                    }

                    IntPtr statementHandle = PrepareStatement(CoreConstants.SqlReadRecentEventsForIdol);
                    try
                    {
                        BindParameters(
                            statementHandle,
                            CreateParameter(CoreConstants.SqlParameterSaveKey, saveKey),
                            CreateParameter(CoreConstants.SqlParameterIdolId, idolId),
                            CreateParameter(CoreConstants.SqlParameterLimitCount, maxCount));

                        while (true)
                        {
                            int stepResult = NativeMethods.sqlite3_step(statementHandle);
                            if (stepResult == SqliteDone)
                            {
                                break;
                            }

                            if (stepResult != SqliteRow)
                            {
                                throw CreateSqliteException(stepResult);
                            }

                            string namespaceIdentifier = IsColumnNull(statementHandle, CoreConstants.EventStreamColumnIndexNamespaceIdentifier)
                                ? string.Empty
                                : GetColumnText(statementHandle, CoreConstants.EventStreamColumnIndexNamespaceIdentifier);
                            string persistedPayloadJson = IsColumnNull(statementHandle, CoreConstants.EventStreamColumnIndexPayloadJson)
                                ? string.Empty
                                : GetColumnText(statementHandle, CoreConstants.EventStreamColumnIndexPayloadJson);
                            long eventId = NativeMethods.sqlite3_column_int64(statementHandle, CoreConstants.EventStreamColumnIndexEventId);
                            string eventType = IsColumnNull(statementHandle, CoreConstants.EventStreamColumnIndexEventType)
                                ? string.Empty
                                : GetColumnText(statementHandle, CoreConstants.EventStreamColumnIndexEventType);
                            string payloadJson = ResolvePayloadJsonForRead(eventId, eventType, namespaceIdentifier, persistedPayloadJson);

                            IMDataCoreEvent item = new IMDataCoreEvent
                            {
                                EventId = eventId,
                                GameDateKey = NativeMethods.sqlite3_column_int(statementHandle, CoreConstants.EventStreamColumnIndexGameDateKey),
                                GameDateTime = GetColumnText(statementHandle, CoreConstants.EventStreamColumnIndexGameDateTime),
                                IdolId = IsColumnNull(statementHandle, CoreConstants.EventStreamColumnIndexIdolId)
                                    ? CoreConstants.InvalidIdValue
                                    : NativeMethods.sqlite3_column_int(statementHandle, CoreConstants.EventStreamColumnIndexIdolId),
                                EntityKind = IsColumnNull(statementHandle, CoreConstants.EventStreamColumnIndexEntityKind)
                                    ? string.Empty
                                    : GetColumnText(statementHandle, CoreConstants.EventStreamColumnIndexEntityKind),
                                EntityId = IsColumnNull(statementHandle, CoreConstants.EventStreamColumnIndexEntityId)
                                    ? string.Empty
                                    : GetColumnText(statementHandle, CoreConstants.EventStreamColumnIndexEntityId),
                                EventType = eventType,
                                SourcePatch = IsColumnNull(statementHandle, CoreConstants.EventStreamColumnIndexSourcePatch)
                                    ? string.Empty
                                    : GetColumnText(statementHandle, CoreConstants.EventStreamColumnIndexSourcePatch),
                                PayloadJson = payloadJson,
                                NamespaceId = namespaceIdentifier
                            };

                            events.Add(item);
                        }
                    }
                    finally
                    {
                        FinalizeStatement(statementHandle);
                    }

                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = CoreConstants.MessageTryReadRecentEventsFailedPrefix + exception.Message;
                    CoreLog.Error(errorMessage);
                    return false;
                }
            }
        }

        public bool TryRollbackToGameDateTime(string saveKey, DateTime cutoffGameDateTime, out string errorMessage)
        {
            errorMessage = string.Empty;

            lock (databaseLock)
            {
                if (disposed)
                {
                    errorMessage = CoreConstants.MessageStorageEngineDisposed;
                    return false;
                }

                try
                {
                    int cutoffGameDateKey = CoreDateTimeUtility.BuildGameDateKey(cutoffGameDateTime);
                    string cutoffDateTime = CoreDateTimeUtility.ToRoundTripString(cutoffGameDateTime);

                    ExecuteWithinTransaction(delegate
                    {
                        ExecuteNonQuery(
                            SqlDeleteEventStreamRowsAfterCutoff,
                            CreateParameter(CoreConstants.SqlParameterSaveKey, saveKey),
                            CreateParameter(CoreConstants.SqlParameterGameDateKey, cutoffGameDateKey),
                            CreateParameter(SqlParameterCutoffDateTime, cutoffDateTime));
                        ExecuteNonQuery(
                            SqlDeleteSingleParticipationRowsAfterCutoff,
                            CreateParameter(CoreConstants.SqlParameterSaveKey, saveKey),
                            CreateParameter(SqlParameterCutoffDateTime, cutoffDateTime));
                        ExecuteNonQuery(
                            SqlDeleteStatusWindowRowsAfterCutoff,
                            CreateParameter(CoreConstants.SqlParameterSaveKey, saveKey),
                            CreateParameter(SqlParameterCutoffDateTime, cutoffDateTime));
                        ExecuteNonQuery(
                            SqlDeleteShowCastWindowRowsAfterCutoff,
                            CreateParameter(CoreConstants.SqlParameterSaveKey, saveKey),
                            CreateParameter(SqlParameterCutoffDateTime, cutoffDateTime));
                        ExecuteNonQuery(
                            SqlDeleteContractWindowRowsAfterCutoff,
                            CreateParameter(CoreConstants.SqlParameterSaveKey, saveKey),
                            CreateParameter(SqlParameterCutoffDateTime, cutoffDateTime));
                        ExecuteNonQuery(
                            SqlDeleteRelationshipWindowRowsAfterCutoff,
                            CreateParameter(CoreConstants.SqlParameterSaveKey, saveKey),
                            CreateParameter(SqlParameterCutoffDateTime, cutoffDateTime));
                        ExecuteNonQuery(
                            SqlDeleteTourParticipationRowsAfterCutoff,
                            CreateParameter(CoreConstants.SqlParameterSaveKey, saveKey),
                            CreateParameter(SqlParameterCutoffDateTime, cutoffDateTime));
                        ExecuteNonQuery(
                            SqlDeleteAwardResultRowsAfterCutoff,
                            CreateParameter(CoreConstants.SqlParameterSaveKey, saveKey),
                            CreateParameter(SqlParameterCutoffDateTime, cutoffDateTime));
                        ExecuteNonQuery(
                            SqlDeleteElectionResultRowsAfterCutoff,
                            CreateParameter(CoreConstants.SqlParameterSaveKey, saveKey),
                            CreateParameter(SqlParameterCutoffDateTime, cutoffDateTime));
                        ExecuteNonQuery(
                            SqlDeletePushWindowRowsAfterCutoff,
                            CreateParameter(CoreConstants.SqlParameterSaveKey, saveKey),
                            CreateParameter(SqlParameterCutoffDateTime, cutoffDateTime));
                    });

                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = CoreConstants.MessageTryRollbackToGameDateTimeFailedPrefix + exception.Message;
                    CoreLog.Error(errorMessage);
                    return false;
                }
            }
        }

        /// <summary>
        /// Rewrites all persisted rows from one save key to another in the active SQLite database.
        /// </summary>
        public bool TryRemapSaveKey(string sourceSaveKey, string targetSaveKey, out string errorMessage)
        {
            errorMessage = string.Empty;
            bool remapAllSourceKeys = string.IsNullOrEmpty(sourceSaveKey);

            if (string.IsNullOrEmpty(targetSaveKey)
                || (!remapAllSourceKeys && string.Equals(sourceSaveKey, targetSaveKey, StringComparison.Ordinal)))
            {
                return true;
            }

            lock (databaseLock)
            {
                if (disposed)
                {
                    errorMessage = CoreConstants.MessageStorageEngineDisposed;
                    return false;
                }

                try
                {
                    ExecuteWithinTransaction(delegate
                    {
                        List<string> tableNames = ReadUserTableNames();
                        for (int tableIndex = CoreConstants.ZeroBasedListStartIndex; tableIndex < tableNames.Count; tableIndex++)
                        {
                            string tableName = tableNames[tableIndex];
                            if (string.IsNullOrEmpty(tableName))
                            {
                                continue;
                            }

                            HashSet<string> tableColumns = ReadTableColumns(tableName);
                            if (!tableColumns.Contains(SqlColumnSaveKey))
                            {
                                continue;
                            }

                            string updateSql = BuildRemapSaveKeySql(tableName, remapAllSourceKeys);
                            if (remapAllSourceKeys)
                            {
                                ExecuteNonQuery(
                                    updateSql,
                                    CreateParameter(SqlParameterTargetSaveKey, targetSaveKey));
                            }
                            else
                            {
                                ExecuteNonQuery(
                                    updateSql,
                                    CreateParameter(SqlParameterSourceSaveKey, sourceSaveKey),
                                    CreateParameter(SqlParameterTargetSaveKey, targetSaveKey));
                            }
                        }
                    });

                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = CoreConstants.MessageTryRemapSaveKeyFailedPrefix + exception.Message;
                    CoreLog.Error(errorMessage);
                    return false;
                }
            }
        }

        public void Dispose()
        {
            lock (databaseLock)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                builtInEventTableColumnsByTableName.Clear();
                builtInEventSchemaRowsByEventType.Clear();
                TryCloseHandleQuietly();
            }
        }

        private void InsertEvents(IReadOnlyList<PendingEvent> pendingEvents)
        {
            if (pendingEvents == null || pendingEvents.Count == CoreConstants.ZeroBasedListStartIndex)
            {
                return;
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < pendingEvents.Count; i++)
            {
                PendingEvent currentEvent = pendingEvents[i];
                bool isBuiltInEvent = string.IsNullOrEmpty(currentEvent.NamespaceIdentifier);
                string persistedPayloadJson = NormalizeEventPayloadForSqlite(currentEvent.NamespaceIdentifier, currentEvent.PayloadJson);
                ExecuteNonQuery(
                    CoreConstants.SqlInsertEvent,
                    CreateParameter(CoreConstants.SqlParameterSaveKey, currentEvent.SaveKey),
                    CreateParameter(CoreConstants.SqlParameterGameDateKey, currentEvent.GameDateKey),
                    CreateParameter(CoreConstants.SqlParameterGameDateTime, currentEvent.GameDateTime),
                    CreateParameter(CoreConstants.SqlParameterIdolId, currentEvent.IdolId >= CoreConstants.MinimumValidIdolIdentifier ? (object)currentEvent.IdolId : null),
                    CreateParameter(CoreConstants.SqlParameterEntityKind, currentEvent.EntityKind),
                    CreateParameter(CoreConstants.SqlParameterEntityId, string.IsNullOrEmpty(currentEvent.EntityId) ? null : currentEvent.EntityId),
                    CreateParameter(CoreConstants.SqlParameterEventType, currentEvent.EventType),
                    CreateParameter(CoreConstants.SqlParameterSourcePatch, currentEvent.SourcePatch),
                    CreateParameter(CoreConstants.SqlParameterEventNamespaceIdentifier, currentEvent.NamespaceIdentifier),
                    CreateParameter(CoreConstants.SqlParameterPayloadJson, persistedPayloadJson));

                if (!isBuiltInEvent)
                {
                    continue;
                }

                object eventIdentifierScalar = ExecuteScalar(CoreConstants.SqlSelectLastInsertRowId);
                long eventId = eventIdentifierScalar == null || eventIdentifierScalar == DBNull.Value
                    ? 0L
                    : Convert.ToInt64(eventIdentifierScalar, CultureInfo.InvariantCulture);
                PersistBuiltInEventPayloadFields(
                    eventId,
                    currentEvent.SaveKey,
                    currentEvent.GameDateKey,
                    currentEvent.GameDateTime,
                    currentEvent.IdolId,
                    currentEvent.EntityKind,
                    currentEvent.EntityId,
                    currentEvent.EventType,
                    currentEvent.SourcePatch,
                    currentEvent.PayloadJson);
            }
        }

        private void UpsertSingleParticipationRows(IReadOnlyList<SingleParticipationProjection> singleParticipationRows)
        {
            if (singleParticipationRows == null || singleParticipationRows.Count == CoreConstants.ZeroBasedListStartIndex)
            {
                return;
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < singleParticipationRows.Count; i++)
            {
                SingleParticipationProjection row = singleParticipationRows[i];
                ExecuteNonQuery(
                    CoreConstants.SqlUpsertSingleParticipation,
                    CreateParameter(CoreConstants.SqlParameterSaveKey, row.SaveKey),
                    CreateParameter(CoreConstants.SqlParameterSingleId, row.SingleId),
                    CreateParameter(CoreConstants.SqlParameterIdolId, row.IdolId),
                    CreateParameter(CoreConstants.SqlParameterRowIndex, row.RowIndex),
                    CreateParameter(CoreConstants.SqlParameterPositionIndex, row.PositionIndex),
                    CreateParameter(CoreConstants.SqlParameterIsCenter, row.IsCenterFlag),
                    CreateParameter(CoreConstants.SqlParameterReleaseDate, row.ReleaseDate));
            }
        }

        private void ApplyStatusTransitions(IReadOnlyList<StatusTransitionProjection> statusTransitions)
        {
            if (statusTransitions == null || statusTransitions.Count == CoreConstants.ZeroBasedListStartIndex)
            {
                return;
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < statusTransitions.Count; i++)
            {
                StatusTransitionProjection transition = statusTransitions[i];
                bool statusActuallyChanged = !string.Equals(transition.PreviousStatusCode, transition.NewStatusCode, StringComparison.Ordinal);
                if (!statusActuallyChanged)
                {
                    continue;
                }

                if (CoreConstants.StatusCodesTrackedAsWindows.Contains(transition.PreviousStatusCode))
                {
                    ExecuteNonQuery(
                        CoreConstants.SqlCloseStatusWindow,
                        CreateParameter(CoreConstants.SqlParameterEndDate, transition.TransitionDate),
                        CreateParameter(CoreConstants.SqlParameterSaveKey, transition.SaveKey),
                        CreateParameter(CoreConstants.SqlParameterIdolId, transition.IdolId),
                        CreateParameter(CoreConstants.SqlParameterStatusType, transition.PreviousStatusCode));
                }

                if (CoreConstants.StatusCodesTrackedAsWindows.Contains(transition.NewStatusCode))
                {
                    ExecuteNonQuery(
                        CoreConstants.SqlOpenStatusWindowIfMissing,
                        CreateParameter(CoreConstants.SqlParameterSaveKey, transition.SaveKey),
                        CreateParameter(CoreConstants.SqlParameterIdolId, transition.IdolId),
                        CreateParameter(CoreConstants.SqlParameterStatusType, transition.NewStatusCode),
                        CreateParameter(CoreConstants.SqlParameterStartDate, transition.TransitionDate));
                }
            }
        }

        private void ApplyDerivedReadModelProjections(IReadOnlyList<PendingEvent> pendingEvents)
        {
            List<ShowCastWindowProjectionMutation> showCastMutations;
            List<ContractWindowProjectionMutation> contractMutations;
            List<RelationshipWindowProjectionMutation> relationshipMutations;
            List<TourParticipationProjectionRow> tourParticipationRows;
            List<AwardResultProjectionRow> awardResultRows;
            List<ElectionResultProjectionRow> electionResultRows;
            List<PushWindowProjectionMutation> pushMutations;

            CoreProjectionDerivation.DeriveFromEvents(
                pendingEvents,
                out showCastMutations,
                out contractMutations,
                out relationshipMutations,
                out tourParticipationRows,
                out awardResultRows,
                out electionResultRows,
                out pushMutations);

            ApplyShowCastWindowMutations(showCastMutations);
            ApplyContractWindowMutations(contractMutations);
            ApplyRelationshipWindowMutations(relationshipMutations);
            UpsertTourParticipationRows(tourParticipationRows);
            UpsertAwardResultRows(awardResultRows);
            UpsertElectionResultRows(electionResultRows);
            ApplyPushWindowMutations(pushMutations);
        }

        private void ApplyShowCastWindowMutations(IReadOnlyList<ShowCastWindowProjectionMutation> mutations)
        {
            if (mutations == null || mutations.Count < CoreConstants.MinimumQueueSizeForFlush)
            {
                return;
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < mutations.Count; i++)
            {
                ShowCastWindowProjectionMutation mutation = mutations[i];
                if (mutation == null || mutation.IdolId < CoreConstants.MinimumValidIdolIdentifier || string.IsNullOrEmpty(mutation.ShowId))
                {
                    continue;
                }

                if (mutation.OpenWindow)
                {
                    ExecuteNonQuery(
                        CoreConstants.SqlOpenShowCastWindowIfMissing,
                        CreateParameter(CoreConstants.SqlParameterSaveKey, mutation.SaveKey),
                        CreateParameter(CoreConstants.SqlParameterShowId, mutation.ShowId),
                        CreateParameter(CoreConstants.SqlParameterIdolId, mutation.IdolId),
                        CreateParameter(CoreConstants.SqlParameterStartDate, mutation.StartDate));
                    continue;
                }

                ExecuteNonQuery(
                    CoreConstants.SqlCloseShowCastWindow,
                    CreateParameter(CoreConstants.SqlParameterEndDate, mutation.EndDate),
                    CreateParameter(CoreConstants.SqlParameterEndReason, mutation.EndReason),
                    CreateParameter(CoreConstants.SqlParameterSaveKey, mutation.SaveKey),
                    CreateParameter(CoreConstants.SqlParameterShowId, mutation.ShowId),
                    CreateParameter(CoreConstants.SqlParameterIdolId, mutation.IdolId));
            }
        }

        private void ApplyContractWindowMutations(IReadOnlyList<ContractWindowProjectionMutation> mutations)
        {
            if (mutations == null || mutations.Count < CoreConstants.MinimumQueueSizeForFlush)
            {
                return;
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < mutations.Count; i++)
            {
                ContractWindowProjectionMutation mutation = mutations[i];
                if (mutation == null || mutation.IdolId < CoreConstants.MinimumValidIdolIdentifier || string.IsNullOrEmpty(mutation.ContractKey))
                {
                    continue;
                }

                if (mutation.OpenWindow)
                {
                    ExecuteNonQuery(
                        CoreConstants.SqlOpenContractWindowIfMissing,
                        CreateParameter(CoreConstants.SqlParameterSaveKey, mutation.SaveKey),
                        CreateParameter(CoreConstants.SqlParameterContractKey, mutation.ContractKey),
                        CreateParameter(CoreConstants.SqlParameterIdolId, mutation.IdolId),
                        CreateParameter(CoreConstants.SqlParameterStartDate, mutation.StartDate));
                    continue;
                }

                ExecuteNonQuery(
                    CoreConstants.SqlCloseContractWindow,
                    CreateParameter(CoreConstants.SqlParameterEndDate, mutation.EndDate),
                    CreateParameter(CoreConstants.SqlParameterEndReason, mutation.EndReason),
                    CreateParameter(CoreConstants.SqlParameterSaveKey, mutation.SaveKey),
                    CreateParameter(CoreConstants.SqlParameterContractKey, mutation.ContractKey),
                    CreateParameter(CoreConstants.SqlParameterIdolId, mutation.IdolId));
            }
        }

        private void ApplyRelationshipWindowMutations(IReadOnlyList<RelationshipWindowProjectionMutation> mutations)
        {
            if (mutations == null || mutations.Count < CoreConstants.MinimumQueueSizeForFlush)
            {
                return;
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < mutations.Count; i++)
            {
                RelationshipWindowProjectionMutation mutation = mutations[i];
                if (mutation == null
                    || mutation.IdolId < CoreConstants.MinimumValidIdolIdentifier
                    || string.IsNullOrEmpty(mutation.RelationshipKey)
                    || string.IsNullOrEmpty(mutation.RelationshipType))
                {
                    continue;
                }

                if (mutation.OpenWindow)
                {
                    ExecuteNonQuery(
                        CoreConstants.SqlOpenRelationshipWindowIfMissing,
                        CreateParameter(CoreConstants.SqlParameterSaveKey, mutation.SaveKey),
                        CreateParameter(CoreConstants.SqlParameterRelationshipKey, mutation.RelationshipKey),
                        CreateParameter(CoreConstants.SqlParameterIdolId, mutation.IdolId),
                        CreateParameter(CoreConstants.SqlParameterRelationshipType, mutation.RelationshipType),
                        CreateParameter(CoreConstants.SqlParameterStartDate, mutation.StartDate));
                    continue;
                }

                ExecuteNonQuery(
                    CoreConstants.SqlCloseRelationshipWindow,
                    CreateParameter(CoreConstants.SqlParameterEndDate, mutation.EndDate),
                    CreateParameter(CoreConstants.SqlParameterEndReason, mutation.EndReason),
                    CreateParameter(CoreConstants.SqlParameterSaveKey, mutation.SaveKey),
                    CreateParameter(CoreConstants.SqlParameterRelationshipKey, mutation.RelationshipKey),
                    CreateParameter(CoreConstants.SqlParameterIdolId, mutation.IdolId),
                    CreateParameter(CoreConstants.SqlParameterRelationshipType, mutation.RelationshipType));
            }
        }

        private void UpsertTourParticipationRows(IReadOnlyList<TourParticipationProjectionRow> rows)
        {
            if (rows == null || rows.Count < CoreConstants.MinimumQueueSizeForFlush)
            {
                return;
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < rows.Count; i++)
            {
                TourParticipationProjectionRow row = rows[i];
                if (row == null || row.IdolId < CoreConstants.MinimumValidIdolIdentifier || string.IsNullOrEmpty(row.TourId))
                {
                    continue;
                }

                ExecuteNonQuery(
                    CoreConstants.SqlUpsertTourParticipationProjection,
                    CreateParameter(CoreConstants.SqlParameterSaveKey, row.SaveKey),
                    CreateParameter(CoreConstants.SqlParameterTourId, row.TourId),
                    CreateParameter(CoreConstants.SqlParameterIdolId, row.IdolId),
                    CreateParameter(CoreConstants.SqlParameterLifecycleAction, row.LifecycleAction),
                    CreateParameter(CoreConstants.SqlParameterEventDate, row.EventDate));
            }
        }

        private void UpsertAwardResultRows(IReadOnlyList<AwardResultProjectionRow> rows)
        {
            if (rows == null || rows.Count < CoreConstants.MinimumQueueSizeForFlush)
            {
                return;
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < rows.Count; i++)
            {
                AwardResultProjectionRow row = rows[i];
                if (row == null || string.IsNullOrEmpty(row.AwardKey))
                {
                    continue;
                }

                ExecuteNonQuery(
                    CoreConstants.SqlUpsertAwardResultProjection,
                    CreateParameter(CoreConstants.SqlParameterSaveKey, row.SaveKey),
                    CreateParameter(CoreConstants.SqlParameterAwardKey, row.AwardKey),
                    CreateParameter(CoreConstants.SqlParameterIdolId, row.IdolId),
                    CreateParameter(CoreConstants.SqlParameterEventDate, row.EventDate));
            }
        }

        private void UpsertElectionResultRows(IReadOnlyList<ElectionResultProjectionRow> rows)
        {
            if (rows == null || rows.Count < CoreConstants.MinimumQueueSizeForFlush)
            {
                return;
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < rows.Count; i++)
            {
                ElectionResultProjectionRow row = rows[i];
                if (row == null || row.IdolId < CoreConstants.MinimumValidIdolIdentifier || string.IsNullOrEmpty(row.ElectionId))
                {
                    continue;
                }

                ExecuteNonQuery(
                    CoreConstants.SqlUpsertElectionResultProjection,
                    CreateParameter(CoreConstants.SqlParameterSaveKey, row.SaveKey),
                    CreateParameter(CoreConstants.SqlParameterElectionId, row.ElectionId),
                    CreateParameter(CoreConstants.SqlParameterIdolId, row.IdolId),
                    CreateParameter(CoreConstants.SqlParameterEventDate, row.EventDate));
            }
        }

        private void ApplyPushWindowMutations(IReadOnlyList<PushWindowProjectionMutation> mutations)
        {
            if (mutations == null || mutations.Count < CoreConstants.MinimumQueueSizeForFlush)
            {
                return;
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < mutations.Count; i++)
            {
                PushWindowProjectionMutation mutation = mutations[i];
                if (mutation == null
                    || mutation.IdolId < CoreConstants.MinimumValidIdolIdentifier
                    || string.IsNullOrEmpty(mutation.SlotKey))
                {
                    continue;
                }

                if (mutation.OpenWindow)
                {
                    ExecuteNonQuery(
                        CoreConstants.SqlOpenPushWindowIfMissing,
                        CreateParameter(CoreConstants.SqlParameterSaveKey, mutation.SaveKey),
                        CreateParameter(CoreConstants.SqlParameterSlotKey, mutation.SlotKey),
                        CreateParameter(CoreConstants.SqlParameterIdolId, mutation.IdolId),
                        CreateParameter(CoreConstants.SqlParameterStartDate, mutation.StartDate),
                        CreateParameter(CoreConstants.SqlParameterPushDaysInSlot, mutation.PushDaysInSlot));
                    continue;
                }

                if (mutation.CloseWindow)
                {
                    ExecuteNonQuery(
                    CoreConstants.SqlClosePushWindow,
                    CreateParameter(CoreConstants.SqlParameterEndDate, mutation.EndDate),
                    CreateParameter(CoreConstants.SqlParameterPushDaysInSlot, mutation.PushDaysInSlot),
                    CreateParameter(CoreConstants.SqlParameterEndReason, mutation.EndReason),
                    CreateParameter(CoreConstants.SqlParameterSaveKey, mutation.SaveKey),
                    CreateParameter(CoreConstants.SqlParameterSlotKey, mutation.SlotKey),
                    CreateParameter(CoreConstants.SqlParameterIdolId, mutation.IdolId));
                    continue;
                }

                ExecuteNonQuery(
                    CoreConstants.SqlTouchPushWindow,
                    CreateParameter(CoreConstants.SqlParameterPushDaysInSlot, mutation.PushDaysInSlot),
                    CreateParameter(CoreConstants.SqlParameterSaveKey, mutation.SaveKey),
                    CreateParameter(CoreConstants.SqlParameterSlotKey, mutation.SlotKey),
                    CreateParameter(CoreConstants.SqlParameterIdolId, mutation.IdolId));
            }
        }

        private string ResolvePayloadJsonForRead(long eventId, string eventType, string namespaceIdentifier, string persistedPayloadJson)
        {
            string normalizedPersistedPayloadJson = string.IsNullOrEmpty(persistedPayloadJson)
                ? CoreConstants.EmptyJsonObject
                : persistedPayloadJson;

            if (!string.IsNullOrEmpty(namespaceIdentifier))
            {
                return normalizedPersistedPayloadJson;
            }

            if (!string.Equals(normalizedPersistedPayloadJson, CoreConstants.EmptyJsonObject, StringComparison.Ordinal))
            {
                return normalizedPersistedPayloadJson;
            }

            string rehydratedPayloadJson;
            if (TryRehydrateBuiltInPayloadJson(eventId, eventType, out rehydratedPayloadJson))
            {
                return rehydratedPayloadJson;
            }

            return normalizedPersistedPayloadJson;
        }

        private bool TryRehydrateBuiltInPayloadJson(long eventId, string eventType, out string payloadJson)
        {
            payloadJson = CoreConstants.EmptyJsonObject;
            if (eventId <= 0L || string.IsNullOrEmpty(eventType))
            {
                return false;
            }

            string tableName = BuildBuiltInEventTableName(eventType);
            List<EventPayloadFieldRow> schemaRows = ResolveBuiltInEventSchemaRows(eventType);
            List<EventPayloadFieldRow> distinctSchemaRows = BuildDistinctPayloadSchemaRows(schemaRows);
            if (distinctSchemaRows.Count == CoreConstants.ZeroBasedListStartIndex)
            {
                return false;
            }

            List<EventPayloadFieldRow> payloadRows;
            if (!TryReadBuiltInPayloadFieldRows(tableName, distinctSchemaRows, eventId, out payloadRows))
            {
                return false;
            }

            payloadJson = BuildPayloadJsonFromFieldRows(payloadRows);
            return !string.IsNullOrEmpty(payloadJson);
        }

        private static List<EventPayloadFieldRow> BuildDistinctPayloadSchemaRows(IReadOnlyList<EventPayloadFieldRow> schemaRows)
        {
            List<EventPayloadFieldRow> distinctRows = new List<EventPayloadFieldRow>();
            if (schemaRows == null || schemaRows.Count == CoreConstants.ZeroBasedListStartIndex)
            {
                return distinctRows;
            }

            HashSet<string> seenColumns = new HashSet<string>(StringComparer.Ordinal);
            for (int rowIndex = CoreConstants.ZeroBasedListStartIndex; rowIndex < schemaRows.Count; rowIndex++)
            {
                EventPayloadFieldRow schemaRow = schemaRows[rowIndex];
                if (schemaRow == null || string.IsNullOrEmpty(schemaRow.FieldKey))
                {
                    continue;
                }

                string columnName = BuildPayloadColumnName(schemaRow.FieldKey);
                if (seenColumns.Contains(columnName))
                {
                    continue;
                }

                seenColumns.Add(columnName);
                distinctRows.Add(
                    new EventPayloadFieldRow
                    {
                        FieldKey = schemaRow.FieldKey,
                        ValueKind = schemaRow.ValueKind
                    });
            }

            return distinctRows;
        }

        private bool TryReadBuiltInPayloadFieldRows(
            string tableName,
            IReadOnlyList<EventPayloadFieldRow> schemaRows,
            long eventId,
            out List<EventPayloadFieldRow> payloadRows)
        {
            payloadRows = new List<EventPayloadFieldRow>();
            if (string.IsNullOrEmpty(tableName)
                || schemaRows == null
                || schemaRows.Count == CoreConstants.ZeroBasedListStartIndex
                || eventId <= 0L)
            {
                return false;
            }

            string selectSql = BuildReadBuiltInPayloadSql(tableName, schemaRows);
            IntPtr statementHandle = IntPtr.Zero;

            try
            {
                statementHandle = PrepareStatement(selectSql);
                BindParameters(
                    statementHandle,
                    CreateParameter(CoreConstants.SqlParameterEventId, eventId));

                int stepResult = NativeMethods.sqlite3_step(statementHandle);
                if (stepResult == SqliteDone)
                {
                    return false;
                }

                if (stepResult != SqliteRow)
                {
                    throw CreateSqliteException(stepResult, selectSql);
                }

                for (int columnIndex = CoreConstants.ZeroBasedListStartIndex; columnIndex < schemaRows.Count; columnIndex++)
                {
                    EventPayloadFieldRow payloadRow = ReadBuiltInPayloadFieldRow(statementHandle, columnIndex, schemaRows[columnIndex]);
                    if (payloadRow != null)
                    {
                        payloadRows.Add(payloadRow);
                    }
                }

                return payloadRows.Count > CoreConstants.ZeroBasedListStartIndex;
            }
            catch
            {
                payloadRows.Clear();
                return false;
            }
            finally
            {
                if (statementHandle != IntPtr.Zero)
                {
                    FinalizeStatement(statementHandle);
                }
            }
        }

        private static string BuildReadBuiltInPayloadSql(string tableName, IReadOnlyList<EventPayloadFieldRow> schemaRows)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(SqlSelectClausePrefix);
            for (int columnIndex = CoreConstants.ZeroBasedListStartIndex; columnIndex < schemaRows.Count; columnIndex++)
            {
                EventPayloadFieldRow schemaRow = schemaRows[columnIndex];
                if (columnIndex > CoreConstants.ZeroBasedListStartIndex)
                {
                    builder.Append(SqlColumnSeparator);
                }

                string columnName = schemaRow == null
                    ? string.Empty
                    : BuildPayloadColumnName(schemaRow.FieldKey);
                builder.Append(QuoteSqlIdentifier(columnName));
            }

            builder.Append(SqlFromClausePrefix)
                .Append(QuoteSqlIdentifier(tableName))
                .Append(SqlWhereClausePrefix)
                .Append(QuoteSqlIdentifier(SqlColumnEventId))
                .Append(SqlEqualsOperatorWithSpacing)
                .Append(CoreConstants.SqlParameterEventId)
                .Append(SqlLimitOneClause)
                .Append(SqlStatementTerminator);
            return builder.ToString();
        }

        private EventPayloadFieldRow ReadBuiltInPayloadFieldRow(IntPtr statementHandle, int columnIndex, EventPayloadFieldRow schemaRow)
        {
            if (schemaRow == null || string.IsNullOrEmpty(schemaRow.FieldKey))
            {
                return null;
            }

            if (IsColumnNull(statementHandle, columnIndex))
            {
                return new EventPayloadFieldRow
                {
                    FieldKey = schemaRow.FieldKey,
                    ValueKind = CoreConstants.PayloadValueKindNull,
                    ValueText = string.Empty
                };
            }

            string columnText = GetColumnText(statementHandle, columnIndex);
            string valueKind = schemaRow.ValueKind ?? CoreConstants.PayloadValueKindRaw;
            string normalizedBooleanText;
            string normalizedNumberText;

            if (string.Equals(valueKind, CoreConstants.PayloadValueKindBoolean, StringComparison.Ordinal))
            {
                if (!TryNormalizeJsonBooleanText(columnText, out normalizedBooleanText))
                {
                    normalizedBooleanText = CoreConstants.JsonBooleanFalse;
                }

                return new EventPayloadFieldRow
                {
                    FieldKey = schemaRow.FieldKey,
                    ValueKind = CoreConstants.PayloadValueKindBoolean,
                    ValueText = normalizedBooleanText
                };
            }

            if (string.Equals(valueKind, CoreConstants.PayloadValueKindNumber, StringComparison.Ordinal))
            {
                if (TryNormalizeJsonNumberText(columnText, out normalizedNumberText))
                {
                    return new EventPayloadFieldRow
                    {
                        FieldKey = schemaRow.FieldKey,
                        ValueKind = CoreConstants.PayloadValueKindNumber,
                        ValueText = normalizedNumberText
                    };
                }

                return new EventPayloadFieldRow
                {
                    FieldKey = schemaRow.FieldKey,
                    ValueKind = CoreConstants.PayloadValueKindString,
                    ValueText = columnText ?? string.Empty
                };
            }

            if (string.Equals(valueKind, CoreConstants.PayloadValueKindNull, StringComparison.Ordinal))
            {
                if (TryNormalizeJsonBooleanText(columnText, out normalizedBooleanText))
                {
                    return new EventPayloadFieldRow
                    {
                        FieldKey = schemaRow.FieldKey,
                        ValueKind = CoreConstants.PayloadValueKindBoolean,
                        ValueText = normalizedBooleanText
                    };
                }

                if (TryNormalizeJsonNumberText(columnText, out normalizedNumberText))
                {
                    return new EventPayloadFieldRow
                    {
                        FieldKey = schemaRow.FieldKey,
                        ValueKind = CoreConstants.PayloadValueKindNumber,
                        ValueText = normalizedNumberText
                    };
                }

                if (IsRawJsonLiteral(columnText))
                {
                    return new EventPayloadFieldRow
                    {
                        FieldKey = schemaRow.FieldKey,
                        ValueKind = CoreConstants.PayloadValueKindRaw,
                        ValueText = columnText ?? string.Empty
                    };
                }

                return new EventPayloadFieldRow
                {
                    FieldKey = schemaRow.FieldKey,
                    ValueKind = CoreConstants.PayloadValueKindString,
                    ValueText = columnText ?? string.Empty
                };
            }

            if (string.Equals(valueKind, CoreConstants.PayloadValueKindObject, StringComparison.Ordinal)
                || string.Equals(valueKind, CoreConstants.PayloadValueKindArray, StringComparison.Ordinal)
                || string.Equals(valueKind, CoreConstants.PayloadValueKindRaw, StringComparison.Ordinal))
            {
                if (IsRawJsonLiteral(columnText))
                {
                    return new EventPayloadFieldRow
                    {
                        FieldKey = schemaRow.FieldKey,
                        ValueKind = valueKind,
                        ValueText = columnText ?? string.Empty
                    };
                }

                return new EventPayloadFieldRow
                {
                    FieldKey = schemaRow.FieldKey,
                    ValueKind = CoreConstants.PayloadValueKindString,
                    ValueText = columnText ?? string.Empty
                };
            }

            return new EventPayloadFieldRow
            {
                FieldKey = schemaRow.FieldKey,
                ValueKind = CoreConstants.PayloadValueKindString,
                ValueText = columnText ?? string.Empty
            };
        }

        private static string BuildPayloadJsonFromFieldRows(IReadOnlyList<EventPayloadFieldRow> rows)
        {
            if (rows == null || rows.Count == CoreConstants.ZeroBasedListStartIndex)
            {
                return CoreConstants.EmptyJsonObject;
            }

            StringBuilder builder = new StringBuilder(CoreConstants.JsonBuilderDefaultCapacity);
            builder.Append(CoreConstants.JsonObjectStartCharacter);
            bool isFirstProperty = true;

            for (int rowIndex = CoreConstants.ZeroBasedListStartIndex; rowIndex < rows.Count; rowIndex++)
            {
                EventPayloadFieldRow row = rows[rowIndex];
                if (row == null || string.IsNullOrEmpty(row.FieldKey))
                {
                    continue;
                }

                if (!isFirstProperty)
                {
                    builder.Append(CoreConstants.JsonPropertySeparatorCharacter);
                }

                isFirstProperty = false;
                AppendEscapedJsonStringLiteral(builder, row.FieldKey);
                builder.Append(CoreConstants.JsonNameValueSeparatorCharacter);
                AppendPayloadFieldValueJson(builder, row);
            }

            builder.Append(CoreConstants.JsonObjectEndCharacter);
            return builder.ToString();
        }

        private static void AppendPayloadFieldValueJson(StringBuilder builder, EventPayloadFieldRow row)
        {
            if (builder == null || row == null)
            {
                return;
            }

            string valueKind = row.ValueKind ?? CoreConstants.PayloadValueKindRaw;
            string valueText = row.ValueText ?? string.Empty;
            string normalizedBooleanText;
            string normalizedNumberText;

            if (string.Equals(valueKind, CoreConstants.PayloadValueKindNull, StringComparison.Ordinal))
            {
                builder.Append(CoreConstants.JsonNullLiteral);
                return;
            }

            if (string.Equals(valueKind, CoreConstants.PayloadValueKindBoolean, StringComparison.Ordinal))
            {
                if (!TryNormalizeJsonBooleanText(valueText, out normalizedBooleanText))
                {
                    normalizedBooleanText = CoreConstants.JsonBooleanFalse;
                }

                builder.Append(normalizedBooleanText);
                return;
            }

            if (string.Equals(valueKind, CoreConstants.PayloadValueKindNumber, StringComparison.Ordinal))
            {
                if (TryNormalizeJsonNumberText(valueText, out normalizedNumberText))
                {
                    builder.Append(normalizedNumberText);
                    return;
                }

                AppendEscapedJsonStringLiteral(builder, valueText);
                return;
            }

            if (string.Equals(valueKind, CoreConstants.PayloadValueKindObject, StringComparison.Ordinal)
                || string.Equals(valueKind, CoreConstants.PayloadValueKindArray, StringComparison.Ordinal)
                || string.Equals(valueKind, CoreConstants.PayloadValueKindRaw, StringComparison.Ordinal))
            {
                if (IsRawJsonLiteral(valueText))
                {
                    builder.Append(valueText);
                    return;
                }

                AppendEscapedJsonStringLiteral(builder, valueText);
                return;
            }

            AppendEscapedJsonStringLiteral(builder, valueText);
        }

        private static void AppendEscapedJsonStringLiteral(StringBuilder builder, string value)
        {
            builder.Append(CoreConstants.JsonStringQuoteCharacter);
            string normalizedValue = value ?? string.Empty;

            for (int index = CoreConstants.ZeroBasedListStartIndex; index < normalizedValue.Length; index++)
            {
                char currentCharacter = normalizedValue[index];
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
                        }
                        else
                        {
                            builder.Append(currentCharacter);
                        }

                        break;
                }
            }

            builder.Append(CoreConstants.JsonStringQuoteCharacter);
        }

        private static bool TryNormalizeJsonBooleanText(string valueText, out string normalizedText)
        {
            normalizedText = CoreConstants.JsonBooleanFalse;
            if (string.IsNullOrEmpty(valueText))
            {
                return false;
            }

            string trimmedValue = valueText.Trim();
            if (string.Equals(trimmedValue, CoreConstants.JsonBooleanTrue, StringComparison.OrdinalIgnoreCase))
            {
                normalizedText = CoreConstants.JsonBooleanTrue;
                return true;
            }

            if (string.Equals(trimmedValue, CoreConstants.JsonBooleanFalse, StringComparison.OrdinalIgnoreCase))
            {
                normalizedText = CoreConstants.JsonBooleanFalse;
                return true;
            }

            int integerValue;
            if (int.TryParse(trimmedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out integerValue))
            {
                normalizedText = integerValue != CoreConstants.ZeroBasedListStartIndex
                    ? CoreConstants.JsonBooleanTrue
                    : CoreConstants.JsonBooleanFalse;
                return true;
            }

            return false;
        }

        private static bool TryNormalizeJsonNumberText(string valueText, out string normalizedText)
        {
            normalizedText = string.Empty;
            if (string.IsNullOrEmpty(valueText))
            {
                return false;
            }

            string trimmedValue = valueText.Trim();
            long integerValue;
            if (long.TryParse(trimmedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out integerValue))
            {
                normalizedText = integerValue.ToString(CultureInfo.InvariantCulture);
                return true;
            }

            double floatingValue;
            if (!double.TryParse(trimmedValue, NumberStyles.Float, CultureInfo.InvariantCulture, out floatingValue))
            {
                return false;
            }

            if (double.IsNaN(floatingValue) || double.IsInfinity(floatingValue))
            {
                return false;
            }

            normalizedText = floatingValue.ToString(CoreConstants.JsonFloatRoundTripFormat, CultureInfo.InvariantCulture);
            return true;
        }

        private static bool IsRawJsonLiteral(string valueText)
        {
            if (string.IsNullOrEmpty(valueText))
            {
                return false;
            }

            string trimmedValue = valueText.Trim();
            if (trimmedValue.Length == CoreConstants.ZeroBasedListStartIndex)
            {
                return false;
            }

            if (string.Equals(trimmedValue, CoreConstants.JsonBooleanTrue, StringComparison.Ordinal)
                || string.Equals(trimmedValue, CoreConstants.JsonBooleanFalse, StringComparison.Ordinal)
                || string.Equals(trimmedValue, CoreConstants.JsonNullLiteral, StringComparison.Ordinal))
            {
                return true;
            }

            if (trimmedValue[CoreConstants.ZeroBasedListStartIndex] == CoreConstants.JsonObjectStartCharacter
                && trimmedValue[trimmedValue.Length - CoreConstants.LastElementOffsetFromCount] == CoreConstants.JsonObjectEndCharacter)
            {
                return true;
            }

            if (trimmedValue[CoreConstants.ZeroBasedListStartIndex] == JsonArrayStartCharacter
                && trimmedValue[trimmedValue.Length - CoreConstants.LastElementOffsetFromCount] == JsonArrayEndCharacter)
            {
                return true;
            }

            if (trimmedValue[CoreConstants.ZeroBasedListStartIndex] == CoreConstants.JsonStringQuoteCharacter
                && trimmedValue[trimmedValue.Length - CoreConstants.LastElementOffsetFromCount] == CoreConstants.JsonStringQuoteCharacter)
            {
                return true;
            }

            string normalizedNumberText;
            return TryNormalizeJsonNumberText(trimmedValue, out normalizedNumberText);
        }

        private static string NormalizeEventPayloadForSqlite(string namespaceIdentifier, string payloadJson)
        {
            if (string.IsNullOrEmpty(namespaceIdentifier))
            {
                return CoreConstants.EmptyJsonObject;
            }

            return string.IsNullOrEmpty(payloadJson) ? CoreConstants.EmptyJsonObject : payloadJson;
        }

        private int GetExistingCustomValueLength(string saveKey, string namespaceIdentifier, string dataKey, out bool exists)
        {
            exists = false;
            object scalarValue = ExecuteScalar(
                CoreConstants.SqlLengthForCustomValue,
                CreateParameter(CoreConstants.SqlParameterSaveKey, saveKey),
                CreateParameter(CoreConstants.SqlParameterNamespaceIdentifier, namespaceIdentifier),
                CreateParameter(CoreConstants.SqlParameterDataKey, dataKey));

            if (scalarValue == null || scalarValue == DBNull.Value)
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            exists = true;
            return Convert.ToInt32(scalarValue, CultureInfo.InvariantCulture);
        }

        private int GetCustomNamespaceKeyCount(string saveKey, string namespaceIdentifier)
        {
            object scalarValue = ExecuteScalar(
                CoreConstants.SqlCountCustomKeysForNamespace,
                CreateParameter(CoreConstants.SqlParameterSaveKey, saveKey),
                CreateParameter(CoreConstants.SqlParameterNamespaceIdentifier, namespaceIdentifier));
            return Convert.ToInt32(scalarValue, CultureInfo.InvariantCulture);
        }

        private int GetCustomNamespaceTotalLength(string saveKey, string namespaceIdentifier)
        {
            object scalarValue = ExecuteScalar(
                CoreConstants.SqlSumCustomValueLengthsForNamespace,
                CreateParameter(CoreConstants.SqlParameterSaveKey, saveKey),
                CreateParameter(CoreConstants.SqlParameterNamespaceIdentifier, namespaceIdentifier));
            return Convert.ToInt32(scalarValue, CultureInfo.InvariantCulture);
        }

        private void ExecuteWithinTransaction(Action action)
        {
            ExecuteNonQuery(SqlBeginImmediate);
            bool committed = false;

            try
            {
                action();
                ExecuteNonQuery(SqlCommit);
                committed = true;
            }
            finally
            {
                if (!committed)
                {
                    TryRollbackQuietly();
                }
            }
        }

        private void ExecuteNonQuery(string commandText, params SqliteParameter[] parameters)
        {
            IntPtr statementHandle = PrepareStatement(commandText);
            try
            {
                BindParameters(statementHandle, parameters);
                while (true)
                {
                    int stepResult = NativeMethods.sqlite3_step(statementHandle);
                    if (stepResult == SqliteDone)
                    {
                        break;
                    }

                    if (stepResult == SqliteRow)
                    {
                        continue;
                    }

                    throw CreateSqliteException(stepResult, commandText);
                }
            }
            finally
            {
                FinalizeStatement(statementHandle);
            }
        }

        private object ExecuteScalar(string commandText, params SqliteParameter[] parameters)
        {
            IntPtr statementHandle = PrepareStatement(commandText);
            try
            {
                BindParameters(statementHandle, parameters);
                int stepResult = NativeMethods.sqlite3_step(statementHandle);
                if (stepResult == SqliteDone)
                {
                    return null;
                }

                if (stepResult != SqliteRow)
                {
                    throw CreateSqliteException(stepResult, commandText);
                }

                int columnType = NativeMethods.sqlite3_column_type(statementHandle, CoreConstants.ZeroBasedListStartIndex);
                if (columnType == SqliteTypeNull)
                {
                    return null;
                }

                if (columnType == SqliteTypeInteger)
                {
                    return NativeMethods.sqlite3_column_int64(statementHandle, CoreConstants.ZeroBasedListStartIndex);
                }

                if (columnType == SqliteTypeText)
                {
                    return GetColumnText(statementHandle, CoreConstants.ZeroBasedListStartIndex);
                }

                return GetColumnText(statementHandle, CoreConstants.ZeroBasedListStartIndex);
            }
            finally
            {
                FinalizeStatement(statementHandle);
            }
        }

        private IntPtr PrepareStatement(string commandText)
        {
            if (databaseHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException(CoreConstants.MessageConnectionInstanceCreationFailed);
            }

            IntPtr statementHandle;
            int commandTextLengthInBytes = GetUtf16ByteCount(commandText);
            int prepareResult = NativeMethods.sqlite3_prepare16_v2(databaseHandle, commandText, commandTextLengthInBytes, out statementHandle, IntPtr.Zero);
            if (prepareResult != SqliteOk || statementHandle == IntPtr.Zero)
            {
                throw CreateSqliteException(prepareResult, commandText);
            }

            return statementHandle;
        }

        private void BindParameters(IntPtr statementHandle, params SqliteParameter[] parameters)
        {
            if (parameters == null || parameters.Length == CoreConstants.ZeroBasedListStartIndex)
            {
                return;
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < parameters.Length; i++)
            {
                SqliteParameter parameter = parameters[i];
                int parameterIndex = NativeMethods.sqlite3_bind_parameter_index(statementHandle, parameter.Name);
                if (parameterIndex <= CoreConstants.ZeroBasedListStartIndex)
                {
                    throw new InvalidOperationException(CoreConstants.MessageSqliteParameterNotFoundPrefix + parameter.Name);
                }

                int bindResult = BindParameterValue(statementHandle, parameterIndex, parameter.Value);
                if (bindResult != SqliteOk)
                {
                    throw CreateSqliteException(bindResult);
                }
            }
        }

        private static int BindParameterValue(IntPtr statementHandle, int parameterIndex, object parameterValue)
        {
            if (parameterValue == null || parameterValue == DBNull.Value)
            {
                return NativeMethods.sqlite3_bind_null(statementHandle, parameterIndex);
            }

            if (parameterValue is int)
            {
                return NativeMethods.sqlite3_bind_int(statementHandle, parameterIndex, (int)parameterValue);
            }

            if (parameterValue is long)
            {
                return NativeMethods.sqlite3_bind_int64(statementHandle, parameterIndex, (long)parameterValue);
            }

            if (parameterValue is bool)
            {
                return NativeMethods.sqlite3_bind_int(statementHandle, parameterIndex, ((bool)parameterValue) ? 1 : 0);
            }

            string textValue = Convert.ToString(parameterValue, CultureInfo.InvariantCulture);
            int textLengthInBytes = GetUtf16ByteCount(textValue);
            return NativeMethods.sqlite3_bind_text16(statementHandle, parameterIndex, textValue, textLengthInBytes, SqliteTransient);
        }

        private static bool IsColumnNull(IntPtr statementHandle, int columnIndex)
        {
            return NativeMethods.sqlite3_column_type(statementHandle, columnIndex) == SqliteTypeNull;
        }

        private static string GetColumnText(IntPtr statementHandle, int columnIndex)
        {
            IntPtr textPointer = NativeMethods.sqlite3_column_text16(statementHandle, columnIndex);
            if (textPointer == IntPtr.Zero)
            {
                return string.Empty;
            }

            string textValue = Marshal.PtrToStringUni(textPointer);
            return textValue ?? string.Empty;
        }

        private static void FinalizeStatement(IntPtr statementHandle)
        {
            if (statementHandle != IntPtr.Zero)
            {
                NativeMethods.sqlite3_finalize(statementHandle);
            }
        }

        private void TryRollbackQuietly()
        {
            try
            {
                ExecuteNonQuery(SqlRollback);
            }
            catch
            {
            }
        }

        private void TryCloseHandleQuietly()
        {
            if (databaseHandle == IntPtr.Zero)
            {
                return;
            }

            try
            {
                NativeMethods.sqlite3_close_v2(databaseHandle);
            }
            catch (Exception exception)
            {
                CoreLog.Warn(CoreConstants.MessageDatabaseDisposeErrorPrefix + exception.Message);
            }
            finally
            {
                databaseHandle = IntPtr.Zero;
            }
        }

        private Exception CreateSqliteException(int resultCode)
        {
            return new InvalidOperationException(BuildSqliteErrorMessage(resultCode));
        }

        private Exception CreateSqliteException(int resultCode, string commandText)
        {
            return new InvalidOperationException(
                BuildSqliteErrorMessage(resultCode)
                + CoreConstants.MessageSqliteSqlPreviewPrefix
                + BuildSqlPreview(commandText));
        }

        private string BuildSqliteErrorMessage(int resultCode)
        {
            string resultMessage = GetResultErrorString(resultCode);
            string databaseMessage = string.Empty;
            if (databaseHandle != IntPtr.Zero)
            {
                IntPtr databaseMessagePointer = NativeMethods.sqlite3_errmsg16(databaseHandle);
                if (databaseMessagePointer != IntPtr.Zero)
                {
                    databaseMessage = Marshal.PtrToStringUni(databaseMessagePointer) ?? string.Empty;
                }
            }

            StringBuilder builder = new StringBuilder(SqliteErrorMessageBuilderInitialCapacity);
            builder.Append(CoreConstants.MessageSqliteResultCodePrefix);
            builder.Append(resultCode.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(resultMessage))
            {
                builder.Append(CoreConstants.MessageSqliteResultMessageOpeningToken);
                builder.Append(resultMessage);
                builder.Append(')');
            }

            if (!string.IsNullOrEmpty(databaseMessage) && !string.Equals(databaseMessage, resultMessage, StringComparison.Ordinal))
            {
                builder.Append(CoreConstants.MessageSqliteDatabaseMessagePrefix);
                builder.Append(databaseMessage);
            }

            return builder.ToString();
        }

        private static int GetUtf16ByteCount(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            checked
            {
                return value.Length * Utf16BytesPerCharacter;
            }
        }

        private static string BuildSqlPreview(string commandText)
        {
            if (string.IsNullOrEmpty(commandText))
            {
                return string.Empty;
            }

            return commandText.Length <= SqlPreviewMaximumLength
                ? commandText
                : commandText.Substring(CoreConstants.ZeroBasedListStartIndex, SqlPreviewMaximumLength) + SqlPreviewEllipsis;
        }

        private static string GetResultErrorString(int resultCode)
        {
            IntPtr resultMessagePointer = NativeMethods.sqlite3_errstr(resultCode);
            if (resultMessagePointer == IntPtr.Zero)
            {
                return string.Empty;
            }

            return Marshal.PtrToStringAnsi(resultMessagePointer) ?? string.Empty;
        }

        private static SqliteParameter CreateParameter(string name, object value)
        {
            SqliteParameter parameter = new SqliteParameter();
            parameter.Name = name;
            parameter.Value = value;
            return parameter;
        }

        private struct SqliteParameter
        {
            public string Name;
            public object Value;
        }

        private static class NativeMethods
        {
            [DllImport(NativeProviderName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_libversion_number();

            [DllImport(NativeProviderName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
            internal static extern int sqlite3_open16(string filename, out IntPtr databaseHandle);

            [DllImport(NativeProviderName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_close_v2(IntPtr databaseHandle);

            [DllImport(NativeProviderName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
            internal static extern int sqlite3_prepare16_v2(
                IntPtr databaseHandle,
                string commandText,
                int commandTextLengthInBytes,
                out IntPtr statementHandle,
                IntPtr commandTailPointer);

            [DllImport(NativeProviderName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_step(IntPtr statementHandle);

            [DllImport(NativeProviderName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_finalize(IntPtr statementHandle);

            [DllImport(NativeProviderName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern int sqlite3_bind_parameter_index(IntPtr statementHandle, string parameterName);

            [DllImport(NativeProviderName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_bind_null(IntPtr statementHandle, int parameterIndex);

            [DllImport(NativeProviderName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_bind_int(IntPtr statementHandle, int parameterIndex, int parameterValue);

            [DllImport(NativeProviderName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_bind_int64(IntPtr statementHandle, int parameterIndex, long parameterValue);

            [DllImport(NativeProviderName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
            internal static extern int sqlite3_bind_text16(
                IntPtr statementHandle,
                int parameterIndex,
                string parameterValue,
                int parameterValueLengthInBytes,
                IntPtr textDestructor);

            [DllImport(NativeProviderName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_column_type(IntPtr statementHandle, int columnIndex);

            [DllImport(NativeProviderName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_column_int(IntPtr statementHandle, int columnIndex);

            [DllImport(NativeProviderName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern long sqlite3_column_int64(IntPtr statementHandle, int columnIndex);

            [DllImport(NativeProviderName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr sqlite3_column_text16(IntPtr statementHandle, int columnIndex);

            [DllImport(NativeProviderName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr sqlite3_errmsg16(IntPtr databaseHandle);

            [DllImport(NativeProviderName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr sqlite3_errstr(int resultCode);

            [DllImport(NativeProviderName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int sqlite3_busy_timeout(IntPtr databaseHandle, int timeoutMilliseconds);
        }
    }
    /// <summary>
}
