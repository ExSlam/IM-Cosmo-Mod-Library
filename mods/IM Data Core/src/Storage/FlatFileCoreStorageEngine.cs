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
    /// Flat-file JSON storage fallback used when System.Data/SQLite is unavailable.
    /// </summary>
    internal sealed class FlatFileCoreStorageEngine : ICoreStorageEngine
    {
        [Serializable]
        private sealed class FlatFileState
        {
            public int FormatVersion;
            public string IntegritySha256 = string.Empty;
            public long NextEventId = 1L;
            public List<FlatFileEventRecord> Events = new List<FlatFileEventRecord>();
            public List<FlatFileCustomDataRecord> CustomData = new List<FlatFileCustomDataRecord>();
            public List<FlatFileSingleParticipationRecord> SingleParticipation = new List<FlatFileSingleParticipationRecord>();
            public List<FlatFileStatusWindowRecord> StatusWindows = new List<FlatFileStatusWindowRecord>();
            public List<FlatFileShowCastWindowRecord> ShowCastWindows = new List<FlatFileShowCastWindowRecord>();
            public List<FlatFileContractWindowRecord> ContractWindows = new List<FlatFileContractWindowRecord>();
            public List<FlatFileRelationshipWindowRecord> RelationshipWindows = new List<FlatFileRelationshipWindowRecord>();
            public List<FlatFileTourParticipationRecord> TourParticipation = new List<FlatFileTourParticipationRecord>();
            public List<FlatFileAwardResultProjectionRecord> AwardResults = new List<FlatFileAwardResultProjectionRecord>();
            public List<FlatFileElectionResultProjectionRecord> ElectionResults = new List<FlatFileElectionResultProjectionRecord>();
            public List<FlatFilePushWindowRecord> PushWindows = new List<FlatFilePushWindowRecord>();
            public string CheckpointFingerprint = string.Empty;
            public long CheckpointEventWatermark = 0L;
            public string CheckpointSnapshotJson = string.Empty;
            public string CheckpointCreatedUtc = string.Empty;
            public List<FlatFileCheckpointRecord> Checkpoints =
                new List<FlatFileCheckpointRecord>();
        }

        [Serializable]
        private sealed class FlatFileCheckpointRecord
        {
            public string Fingerprint = string.Empty;
            public long EventWatermark;
            public string SnapshotJson = string.Empty;
            public string CreatedUtc = string.Empty;
        }

        /// <summary>
        /// Exact field layout used by format version 1. Keeping this projection lets
        /// version-1 integrity hashes remain verifiable after the history field was
        /// added to the current state envelope.
        /// </summary>
        [Serializable]
        private sealed class FlatFileStateVersionOne
        {
            public int FormatVersion = PreviousFlatFileFormatVersion;
            public string IntegritySha256 = string.Empty;
            public long NextEventId = 1L;
            public List<FlatFileEventRecord> Events = new List<FlatFileEventRecord>();
            public List<FlatFileCustomDataRecord> CustomData = new List<FlatFileCustomDataRecord>();
            public List<FlatFileSingleParticipationRecord> SingleParticipation = new List<FlatFileSingleParticipationRecord>();
            public List<FlatFileStatusWindowRecord> StatusWindows = new List<FlatFileStatusWindowRecord>();
            public List<FlatFileShowCastWindowRecord> ShowCastWindows = new List<FlatFileShowCastWindowRecord>();
            public List<FlatFileContractWindowRecord> ContractWindows = new List<FlatFileContractWindowRecord>();
            public List<FlatFileRelationshipWindowRecord> RelationshipWindows = new List<FlatFileRelationshipWindowRecord>();
            public List<FlatFileTourParticipationRecord> TourParticipation = new List<FlatFileTourParticipationRecord>();
            public List<FlatFileAwardResultProjectionRecord> AwardResults = new List<FlatFileAwardResultProjectionRecord>();
            public List<FlatFileElectionResultProjectionRecord> ElectionResults = new List<FlatFileElectionResultProjectionRecord>();
            public List<FlatFilePushWindowRecord> PushWindows = new List<FlatFilePushWindowRecord>();
            public string CheckpointFingerprint = string.Empty;
            public long CheckpointEventWatermark = 0L;
            public string CheckpointSnapshotJson = string.Empty;
            public string CheckpointCreatedUtc = string.Empty;
        }

        [Serializable]
        private sealed class FlatFileEventRecord
        {
            public long EventId;
            public int GameDateKey;
            public string GameDateTime = string.Empty;
            public int IdolId = CoreConstants.InvalidIdValue;
            public string EntityKind = string.Empty;
            public string EntityId = string.Empty;
            public string EventType = string.Empty;
            public string SourcePatch = string.Empty;
            public string NamespaceIdentifier = string.Empty;
            public string PayloadJson = CoreConstants.EmptyJsonObject;
        }

        [Serializable]
        private sealed class FlatFileCustomDataRecord
        {
            public string NamespaceIdentifier = string.Empty;
            public string DataKey = string.Empty;
            public string ValueJson = string.Empty;
            public string UpdatedUtc = string.Empty;
        }

        [Serializable]
        private sealed class FlatFileSingleParticipationRecord
        {
            public int SingleId;
            public int IdolId;
            public int RowIndex;
            public int PositionIndex;
            public int IsCenterFlag;
            public string ReleaseDate = string.Empty;
        }

        [Serializable]
        private sealed class FlatFileStatusWindowRecord
        {
            public int IdolId;
            public string StatusType = string.Empty;
            public string StartDate = string.Empty;
            public string EndDate = string.Empty;
        }

        [Serializable]
        private sealed class FlatFileShowCastWindowRecord
        {
            public string ShowId = string.Empty;
            public int IdolId = CoreConstants.InvalidIdValue;
            public string StartDate = string.Empty;
            public string EndDate = string.Empty;
            public string EndReason = string.Empty;
            public string PayloadJson = CoreConstants.EmptyJsonObject;
        }

        [Serializable]
        private sealed class FlatFileContractWindowRecord
        {
            public string ContractKey = string.Empty;
            public int IdolId = CoreConstants.InvalidIdValue;
            public string StartDate = string.Empty;
            public string EndDate = string.Empty;
            public string EndReason = string.Empty;
            public string PayloadJson = CoreConstants.EmptyJsonObject;
        }

        [Serializable]
        private sealed class FlatFileRelationshipWindowRecord
        {
            public string RelationshipKey = string.Empty;
            public int IdolId = CoreConstants.InvalidIdValue;
            public string RelationshipType = string.Empty;
            public string StartDate = string.Empty;
            public string EndDate = string.Empty;
            public string EndReason = string.Empty;
            public string PayloadJson = CoreConstants.EmptyJsonObject;
        }

        [Serializable]
        private sealed class FlatFileTourParticipationRecord
        {
            public string TourId = string.Empty;
            public int IdolId = CoreConstants.InvalidIdValue;
            public string LifecycleAction = string.Empty;
            public string EventDate = string.Empty;
            public string PayloadJson = CoreConstants.EmptyJsonObject;
        }

        [Serializable]
        private sealed class FlatFileAwardResultProjectionRecord
        {
            public string AwardKey = string.Empty;
            public int IdolId = CoreConstants.InvalidIdValue;
            public string EventDate = string.Empty;
            public string PayloadJson = CoreConstants.EmptyJsonObject;
        }

        [Serializable]
        private sealed class FlatFileElectionResultProjectionRecord
        {
            public string ElectionId = string.Empty;
            public int IdolId = CoreConstants.InvalidIdValue;
            public string EventDate = string.Empty;
            public string PayloadJson = CoreConstants.EmptyJsonObject;
        }

        [Serializable]
        private sealed class FlatFilePushWindowRecord
        {
            public string SlotKey = string.Empty;
            public int IdolId = CoreConstants.InvalidIdValue;
            public string StartDate = string.Empty;
            public string EndDate = string.Empty;
            public int LastDaysInSlot = CoreConstants.ProjectionUnknownDayCount;
            public string EndReason = string.Empty;
            public string PayloadJson = CoreConstants.EmptyJsonObject;
        }

        private readonly object storageLock = new object();
        private const int CurrentFlatFileFormatVersion = 2;
        private const int PreviousFlatFileFormatVersion = 1;
        private const int MaximumRetainedSaveGenerations = 8;
        private const char JsonArrayStartCharacter = '[';
        private const char JsonArrayEndCharacter = ']';
        private const string TemporaryFileSuffix = ".tmp";
        private const string BackupFileSuffix = ".bak";
        private string storagePath = string.Empty;
        private FlatFileState state = new FlatFileState();
        private bool disposed;

        /// <summary>
        /// Initializes fallback storage and loads existing state if present.
        /// </summary>
        public bool Initialize(string databasePath, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(databasePath))
            {
                errorMessage = CoreConstants.MessageDatabasePathEmpty;
                return false;
            }

            lock (storageLock)
            {
                try
                {
                    storagePath = databasePath;
                    string directoryPath = Path.GetDirectoryName(storagePath);
                    if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    LoadStateFromDiskLocked();
                    EnsureStateInitializedLocked();
                    if (!File.Exists(storagePath))
                    {
                        string initializationWriteError;
                        if (!SaveStateLocked(out initializationWriteError))
                        {
                            throw new IOException(initializationWriteError);
                        }
                    }

                    CoreLog.Info(CoreConstants.MessageFlatFileEngineInitialized);
                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = CoreConstants.MessageFlatFileReadFailedPrefix + exception.Message;
                    CoreLog.Error(errorMessage);
                    return false;
                }
            }
        }

        /// <summary>
        /// Persists queued events into flat-file state.
        /// </summary>
        public bool PersistBatch(
            IReadOnlyList<PendingEvent> pendingEvents,
            IReadOnlyList<SingleParticipationProjection> singleParticipationRows,
            IReadOnlyList<StatusTransitionProjection> statusTransitions,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (disposed)
            {
                errorMessage = CoreConstants.MessageStorageEngineDisposed;
                return false;
            }

            lock (storageLock)
            {
                FlatFileState originalState = state;
                try
                {
                    EnsureStateInitializedLocked();
                    originalState = state;
                    state = CloneStateLocked();

                    if (pendingEvents != null)
                    {
                        for (int i = CoreConstants.ZeroBasedListStartIndex; i < pendingEvents.Count; i++)
                        {
                            PendingEvent pendingEvent = pendingEvents[i];
                            if (pendingEvent == null)
                            {
                                continue;
                            }

                            FlatFileEventRecord eventRecord = new FlatFileEventRecord
                            {
                                EventId = state.NextEventId++,
                                GameDateKey = pendingEvent.GameDateKey,
                                GameDateTime = pendingEvent.GameDateTime ?? string.Empty,
                                IdolId = pendingEvent.IdolId,
                                EntityKind = pendingEvent.EntityKind ?? string.Empty,
                                EntityId = pendingEvent.EntityId ?? string.Empty,
                                EventType = pendingEvent.EventType ?? string.Empty,
                                SourcePatch = pendingEvent.SourcePatch ?? string.Empty,
                                NamespaceIdentifier = pendingEvent.NamespaceIdentifier ?? string.Empty,
                                PayloadJson = pendingEvent.PayloadJson ?? CoreConstants.EmptyJsonObject
                            };

                            state.Events.Add(eventRecord);
                        }
                    }

                    ApplySingleParticipationRowsLocked(singleParticipationRows);
                    ApplyStatusTransitionsLocked(statusTransitions);
                    ApplyDerivedReadModelProjectionsLocked(pendingEvents);

                    if (SaveStateLocked(out errorMessage))
                    {
                        return true;
                    }

                    state = originalState;
                    return false;
                }
                catch (Exception exception)
                {
                    state = originalState;
                    errorMessage = CoreConstants.MessagePersistBatchFailedPrefix + exception.Message;
                    CoreLog.Error(errorMessage);
                    return false;
                }
            }
        }

        /// <summary>
        /// Validates one custom-data mutation without changing flat-file state.
        /// </summary>
        public bool TryValidateCustomDataMutation(
            string saveKey,
            string namespaceIdentifier,
            string dataKey,
            string jsonValue,
            bool remove,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (!remove && jsonValue == null)
            {
                errorMessage = CoreConstants.MessageJsonValueNull;
                return false;
            }

            if (!remove && jsonValue.Length > CoreConstants.MaximumCustomValueCharacterCount)
            {
                errorMessage = CoreConstants.MessageJsonValueTooLong;
                return false;
            }

            lock (storageLock)
            {
                if (disposed)
                {
                    errorMessage = CoreConstants.MessageStorageEngineDisposed;
                    return false;
                }

                if (remove)
                {
                    return true;
                }

                try
                {
                    EnsureStateInitializedLocked();
                    int existingIndex = FindCustomDataIndexLocked(namespaceIdentifier, dataKey);
                    bool exists = existingIndex >= CoreConstants.ZeroBasedListStartIndex;
                    int existingLength = exists
                        ? GetSafeLength(state.CustomData[existingIndex].ValueJson)
                        : CoreConstants.ZeroBasedListStartIndex;
                    if (!exists && GetNamespaceKeyCountLocked(namespaceIdentifier) >= CoreConstants.MaximumCustomKeysPerNamespace)
                    {
                        errorMessage = CoreConstants.MessageNamespaceKeyQuotaExceeded;
                        return false;
                    }

                    int projectedTotalLength = GetNamespaceTotalLengthLocked(namespaceIdentifier)
                        - existingLength
                        + jsonValue.Length;
                    if (projectedTotalLength > CoreConstants.MaximumNamespaceCharacterBudget)
                    {
                        errorMessage = CoreConstants.MessageNamespaceDataBudgetExceeded;
                        return false;
                    }

                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = "TryValidateCustomDataMutation failed: " + exception.Message;
                    CoreLog.Error(errorMessage);
                    return false;
                }
            }
        }

        /// <summary>
        /// Writes one namespaced custom JSON value with quota checks.
        /// </summary>
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

            lock (storageLock)
            {
                FlatFileState originalState = state;
                try
                {
                    EnsureStateInitializedLocked();
                    originalState = state;

                    int existingIndex = FindCustomDataIndexLocked(namespaceIdentifier, dataKey);
                    bool exists = existingIndex >= CoreConstants.ZeroBasedListStartIndex;
                    int existingLength = exists ? GetSafeLength(state.CustomData[existingIndex].ValueJson) : CoreConstants.ZeroBasedListStartIndex;
                    int keyCount = GetNamespaceKeyCountLocked(namespaceIdentifier);
                    int totalLength = GetNamespaceTotalLengthLocked(namespaceIdentifier);

                    if (!exists && keyCount >= CoreConstants.MaximumCustomKeysPerNamespace)
                    {
                        errorMessage = CoreConstants.MessageNamespaceKeyQuotaExceeded;
                        return false;
                    }

                    int projectedTotalLength = totalLength - existingLength + jsonValue.Length;
                    if (projectedTotalLength > CoreConstants.MaximumNamespaceCharacterBudget)
                    {
                        errorMessage = CoreConstants.MessageNamespaceDataBudgetExceeded;
                        return false;
                    }

                    state = CloneStateLocked();
                    existingIndex = FindCustomDataIndexLocked(namespaceIdentifier, dataKey);
                    exists = existingIndex >= CoreConstants.ZeroBasedListStartIndex;

                    if (exists)
                    {
                        FlatFileCustomDataRecord existingRecord = state.CustomData[existingIndex];
                        existingRecord.ValueJson = jsonValue;
                        existingRecord.UpdatedUtc = CoreDateTimeUtility.ToUtcRoundTripString(DateTime.UtcNow);
                    }
                    else
                    {
                        state.CustomData.Add(
                            new FlatFileCustomDataRecord
                            {
                                NamespaceIdentifier = namespaceIdentifier ?? string.Empty,
                                DataKey = dataKey ?? string.Empty,
                                ValueJson = jsonValue,
                                UpdatedUtc = CoreDateTimeUtility.ToUtcRoundTripString(DateTime.UtcNow)
                            });
                    }

                    if (SaveStateLocked(out errorMessage))
                    {
                        return true;
                    }

                    state = originalState;
                    return false;
                }
                catch (Exception exception)
                {
                    state = originalState;
                    errorMessage = CoreConstants.MessageTrySetCustomDataFailedPrefix + exception.Message;
                    CoreLog.Error(errorMessage);
                    return false;
                }
            }
        }

        /// <summary>
        /// Reads one namespaced custom JSON value.
        /// </summary>
        public bool TryGetCustomData(string saveKey, string namespaceIdentifier, string dataKey, out string jsonValue, out string errorMessage)
        {
            jsonValue = string.Empty;
            errorMessage = string.Empty;

            lock (storageLock)
            {
                try
                {
                    EnsureStateInitializedLocked();
                    int existingIndex = FindCustomDataIndexLocked(namespaceIdentifier, dataKey);
                    if (existingIndex < CoreConstants.ZeroBasedListStartIndex)
                    {
                        return false;
                    }

                    FlatFileCustomDataRecord existingRecord = state.CustomData[existingIndex];
                    jsonValue = existingRecord.ValueJson ?? string.Empty;
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

        /// <summary>
        /// Removes one namespaced custom JSON value.
        /// </summary>
        public bool TryRemoveCustomData(string saveKey, string namespaceIdentifier, string dataKey, out string errorMessage)
        {
            errorMessage = string.Empty;

            lock (storageLock)
            {
                FlatFileState originalState = state;
                try
                {
                    EnsureStateInitializedLocked();
                    originalState = state;
                    int existingIndex = FindCustomDataIndexLocked(namespaceIdentifier, dataKey);
                    if (existingIndex < CoreConstants.ZeroBasedListStartIndex)
                    {
                        return true;
                    }

                    state = CloneStateLocked();
                    for (int i = state.CustomData.Count - CoreConstants.LastElementOffsetFromCount;
                        i >= CoreConstants.ZeroBasedListStartIndex;
                        i--)
                    {
                        FlatFileCustomDataRecord record = state.CustomData[i];
                        if (record != null
                            && string.Equals(
                                record.NamespaceIdentifier,
                                namespaceIdentifier ?? string.Empty,
                                StringComparison.Ordinal)
                            && string.Equals(record.DataKey, dataKey ?? string.Empty, StringComparison.Ordinal))
                        {
                            state.CustomData.RemoveAt(i);
                        }
                    }

                    if (SaveStateLocked(out errorMessage))
                    {
                        return true;
                    }

                    state = originalState;
                    return false;
                }
                catch (Exception exception)
                {
                    state = originalState;
                    errorMessage = CoreConstants.MessageTryRemoveCustomDataFailedPrefix + exception.Message;
                    CoreLog.Error(errorMessage);
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns recent events for one idol, newest first.
        /// </summary>
        public bool TryReadRecentEventsForIdol(string saveKey, int idolId, int maxCount, out List<IMDataCoreEvent> events, out string errorMessage)
        {
            events = new List<IMDataCoreEvent>();
            errorMessage = string.Empty;

            lock (storageLock)
            {
                try
                {
                    EnsureStateInitializedLocked();
                    if (maxCount <= CoreConstants.ZeroBasedListStartIndex)
                    {
                        return true;
                    }

                    List<FlatFileEventRecord> matchingEvents = new List<FlatFileEventRecord>();
                    List<FlatFileEventRecord> idolSpecificEvents = new List<FlatFileEventRecord>();
                    List<FlatFileEventRecord> globalEvents = new List<FlatFileEventRecord>();
                    for (int i = CoreConstants.ZeroBasedListStartIndex; i < state.Events.Count; i++)
                    {
                        FlatFileEventRecord eventRecord = state.Events[i];
                        if (eventRecord == null)
                        {
                            continue;
                        }

                        bool isLedgerInternalEvent =
                            string.Equals(eventRecord.EventType, MoneyLedgerConstants.EventTypeTransaction, StringComparison.Ordinal)
                            || string.Equals(eventRecord.EventType, MoneyLedgerConstants.EventTypeCoverageStarted, StringComparison.Ordinal);
                        bool idolMatches = !isLedgerInternalEvent
                            && (eventRecord.IdolId == idolId || eventRecord.IdolId < CoreConstants.MinimumValidIdolIdentifier);
                        if (idolMatches)
                        {
                            matchingEvents.Add(eventRecord);
                            if (eventRecord.IdolId == idolId)
                            {
                                idolSpecificEvents.Add(eventRecord);
                            }
                            else
                            {
                                globalEvents.Add(eventRecord);
                            }
                        }
                    }

                    idolSpecificEvents.Sort(CompareEventsDescending);
                    globalEvents.Sort(CompareEventsDescending);

                    matchingEvents.Clear();
                    matchingEvents.AddRange(idolSpecificEvents);
                    matchingEvents.AddRange(globalEvents);

                    int eventCount = Math.Min(maxCount, matchingEvents.Count);
                    for (int i = CoreConstants.ZeroBasedListStartIndex; i < eventCount; i++)
                    {
                        FlatFileEventRecord eventRecord = matchingEvents[i];
                        IMDataCoreEvent apiEvent = new IMDataCoreEvent
                        {
                            EventId = eventRecord.EventId,
                            GameDateKey = eventRecord.GameDateKey,
                            GameDateTime = eventRecord.GameDateTime ?? string.Empty,
                            IdolId = eventRecord.IdolId,
                            EntityKind = eventRecord.EntityKind ?? string.Empty,
                            EntityId = eventRecord.EntityId ?? string.Empty,
                            EventType = eventRecord.EventType ?? string.Empty,
                            SourcePatch = eventRecord.SourcePatch ?? string.Empty,
                            PayloadJson = eventRecord.PayloadJson ?? string.Empty,
                            NamespaceId = eventRecord.NamespaceIdentifier ?? string.Empty
                        };

                        events.Add(apiEvent);
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

        public bool TryReadMoneyTransactions(
            string saveKey,
            DateTime startInclusive,
            DateTime endExclusive,
            int maxCount,
            out List<IMDataCoreMoneyTransaction> transactions,
            out bool wasTruncated,
            out string errorMessage)
        {
            transactions = new List<IMDataCoreMoneyTransaction>();
            wasTruncated = false;
            errorMessage = string.Empty;

            lock (storageLock)
            {
                try
                {
                    EnsureStateInitializedLocked();
                    int startDateKey = CoreDateTimeUtility.BuildGameDateKey(startInclusive);
                    int endDateKey = CoreDateTimeUtility.BuildGameDateKey(endExclusive);
                    List<FlatFileEventRecord> matchingEvents = new List<FlatFileEventRecord>();
                    for (int eventIndex = CoreConstants.ZeroBasedListStartIndex; eventIndex < state.Events.Count; eventIndex++)
                    {
                        FlatFileEventRecord eventRecord = state.Events[eventIndex];
                        if (eventRecord == null
                            || !string.Equals(eventRecord.EventType, MoneyLedgerConstants.EventTypeTransaction, StringComparison.Ordinal)
                            || eventRecord.GameDateKey < startDateKey
                            || eventRecord.GameDateKey >= endDateKey)
                        {
                            continue;
                        }

                        matchingEvents.Add(eventRecord);
                    }

                    matchingEvents.Sort(CompareEventsAscending);
                    int requestedCount = Math.Max(MoneyLedgerConstants.MinimumReadCount, maxCount);
                    int resultCount = Math.Min(requestedCount, matchingEvents.Count);
                    wasTruncated = matchingEvents.Count > requestedCount;
                    for (int resultIndex = CoreConstants.ZeroBasedListStartIndex; resultIndex < resultCount; resultIndex++)
                    {
                        FlatFileEventRecord source = matchingEvents[resultIndex];
                        IMDataCoreEvent eventModel = new IMDataCoreEvent
                        {
                            EventId = source.EventId,
                            GameDateKey = source.GameDateKey,
                            GameDateTime = source.GameDateTime ?? string.Empty,
                            IdolId = source.IdolId,
                            EntityKind = source.EntityKind ?? string.Empty,
                            EntityId = source.EntityId ?? string.Empty,
                            EventType = source.EventType ?? string.Empty,
                            SourcePatch = source.SourcePatch ?? string.Empty,
                            PayloadJson = source.PayloadJson ?? CoreConstants.EmptyJsonObject,
                            NamespaceId = source.NamespaceIdentifier ?? string.Empty
                        };
                        IMDataCoreMoneyTransaction transaction = MoneyLedgerPayloadUtility.ToPublicModel(eventModel);
                        if (transaction != null)
                        {
                            transactions.Add(transaction);
                        }
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

        public bool TryGetMoneyLedgerCoverageStart(string saveKey, out DateTime coverageStart, out string errorMessage)
        {
            coverageStart = DateTime.MinValue;
            errorMessage = string.Empty;

            lock (storageLock)
            {
                try
                {
                    EnsureStateInitializedLocked();
                    FlatFileEventRecord earliest = null;
                    for (int eventIndex = CoreConstants.ZeroBasedListStartIndex; eventIndex < state.Events.Count; eventIndex++)
                    {
                        FlatFileEventRecord eventRecord = state.Events[eventIndex];
                        if (eventRecord == null || !string.Equals(eventRecord.EventType, MoneyLedgerConstants.EventTypeCoverageStarted, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        if (earliest == null || CompareEventsAscending(eventRecord, earliest) < CoreConstants.ZeroBasedListStartIndex)
                        {
                            earliest = eventRecord;
                        }
                    }

                    if (earliest == null)
                    {
                        return false;
                    }

                    return DateTime.TryParseExact(
                        earliest.GameDateTime,
                        CoreConstants.RoundTripDateFormat,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out coverageStart);
                }
                catch (Exception exception)
                {
                    errorMessage = CoreConstants.MessageTryReadRecentEventsFailedPrefix + exception.Message;
                    CoreLog.Error(errorMessage);
                    return false;
                }
            }
        }

        /// <summary>
        /// Records an exact durable checkpoint for the vanilla save fingerprint.
        /// </summary>
        public bool TryRecordSaveGeneration(
            string saveKey,
            string vanillaSaveFingerprint,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrEmpty(vanillaSaveFingerprint))
            {
                errorMessage = "Vanilla save fingerprint is empty.";
                return false;
            }

            lock (storageLock)
            {
                FlatFileState originalState = state;
                try
                {
                    if (disposed)
                    {
                        errorMessage = CoreConstants.MessageStorageEngineDisposed;
                        return false;
                    }

                    EnsureStateInitializedLocked();
                    originalState = state;
                    state = CloneStateLocked();

                    FlatFileState checkpointState = CloneStateLocked();
                    ClearCheckpointMetadata(checkpointState);
                    string checkpointJson = SerializeState(checkpointState);
                    RemoveCheckpointByFingerprintLocked(
                        vanillaSaveFingerprint);
                    state.Checkpoints.Add(
                        new FlatFileCheckpointRecord
                        {
                            Fingerprint = vanillaSaveFingerprint,
                            EventWatermark = ComputeMaximumEventIdLocked(),
                            SnapshotJson = checkpointJson,
                            CreatedUtc = CoreDateTimeUtility.ToUtcRoundTripString(
                                DateTime.UtcNow)
                        });
                    while (state.Checkpoints.Count >
                        MaximumRetainedSaveGenerations)
                    {
                        state.Checkpoints.RemoveAt(
                            CoreConstants.ZeroBasedListStartIndex);
                    }

                    if (SaveStateLocked(out errorMessage))
                    {
                        return true;
                    }

                    state = originalState;
                    return false;
                }
                catch (Exception exception)
                {
                    state = originalState;
                    errorMessage = "TryRecordSaveGeneration failed: " + exception.Message;
                    CoreLog.Error(errorMessage);
                    return false;
                }
            }
        }

        public bool TryValidateIntegrity(out string errorMessage)
        {
            errorMessage = string.Empty;
            lock (storageLock)
            {
                if (disposed)
                {
                    errorMessage = CoreConstants.MessageStorageEngineDisposed;
                    return false;
                }

                try
                {
                    if (string.IsNullOrEmpty(storagePath) || !File.Exists(storagePath))
                    {
                        errorMessage = "The flat-file primary state does not exist.";
                        return false;
                    }

                    // DeserializeState validates the envelope hash, every retained
                    // checkpoint, and every embedded snapshot before returning.
                    DeserializeState(File.ReadAllText(storagePath, Encoding.UTF8));
                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = "Flat-file integrity validation failed: " + exception.Message;
                    CoreLog.Error(errorMessage);
                    return false;
                }
            }
        }

        /// <summary>
        /// Restores the exact checkpoint associated with one vanilla save fingerprint.
        /// </summary>
        public bool TryRollbackToSaveGeneration(
            string saveKey,
            string vanillaSaveFingerprint,
            out bool generationFound,
            out string errorMessage)
        {
            generationFound = false;
            errorMessage = string.Empty;

            lock (storageLock)
            {
                FlatFileState originalState = state;
                try
                {
                    if (disposed)
                    {
                        errorMessage = CoreConstants.MessageStorageEngineDisposed;
                        return false;
                    }

                    EnsureStateInitializedLocked();
                    originalState = state;
                    int checkpointIndex = FindCheckpointIndexLocked(
                        vanillaSaveFingerprint);
                    if (checkpointIndex <
                        CoreConstants.ZeroBasedListStartIndex)
                    {
                        return true;
                    }

                    generationFound = true;
                    FlatFileCheckpointRecord checkpoint =
                        state.Checkpoints[checkpointIndex];
                    if (checkpoint == null ||
                        string.IsNullOrEmpty(checkpoint.SnapshotJson))
                    {
                        generationFound = false;
                        errorMessage = "The matching save-generation checkpoint is missing its snapshot.";
                        return false;
                    }

                    List<FlatFileCheckpointRecord> retainedCheckpoints =
                        CloneCheckpointHistoryLocked();
                    FlatFileState restoredState = DeserializeState(
                        checkpoint.SnapshotJson);
                    ClearCheckpointMetadata(restoredState);
                    restoredState.Checkpoints = retainedCheckpoints;
                    state = restoredState;
                    EnsureStateInitializedLocked();

                    if (SaveStateLocked(out errorMessage))
                    {
                        return true;
                    }

                    state = originalState;
                    generationFound = false;
                    return false;
                }
                catch (Exception exception)
                {
                    state = originalState;
                    generationFound = false;
                    errorMessage = "TryRollbackToSaveGeneration failed: " + exception.Message;
                    CoreLog.Error(errorMessage);
                    return false;
                }
            }
        }

        /// <summary>
        /// Removes rows newer than one loaded save snapshot date. This is a legacy fallback
        /// for saves that predate exact fingerprint checkpoints.
        /// </summary>
        public bool TryRollbackToGameDateTime(string saveKey, DateTime cutoffGameDateTime, out string errorMessage)
        {
            errorMessage = string.Empty;

            lock (storageLock)
            {
                FlatFileState originalState = state;
                try
                {
                    if (disposed)
                    {
                        errorMessage = CoreConstants.MessageStorageEngineDisposed;
                        return false;
                    }

                    EnsureStateInitializedLocked();
                    originalState = state;
                    state = CloneStateLocked();

                    string cutoffDateTime = CoreDateTimeUtility.ToRoundTripString(cutoffGameDateTime);

                    state.Events.RemoveAll(
                        record => record != null
                            && IsDateAfterCutoff(record.GameDateTime, cutoffDateTime, cutoffGameDateTime));
                    state.SingleParticipation.RemoveAll(
                        record => record != null
                            && IsDateAfterCutoff(record.ReleaseDate, cutoffDateTime, cutoffGameDateTime));
                    state.StatusWindows.RemoveAll(
                        record => record != null
                            && IsDateAfterCutoff(record.StartDate, cutoffDateTime, cutoffGameDateTime));
                    for (int i = CoreConstants.ZeroBasedListStartIndex; i < state.StatusWindows.Count; i++)
                    {
                        FlatFileStatusWindowRecord record = state.StatusWindows[i];
                        if (record != null && IsDateAfterCutoff(record.EndDate, cutoffDateTime, cutoffGameDateTime))
                        {
                            record.EndDate = string.Empty;
                        }
                    }

                    state.ShowCastWindows.RemoveAll(
                        record => record != null
                            && IsDateAfterCutoff(record.StartDate, cutoffDateTime, cutoffGameDateTime));
                    ReopenShowCastWindowsAfterCutoffLocked(cutoffDateTime, cutoffGameDateTime);
                    state.ContractWindows.RemoveAll(
                        record => record != null
                            && IsDateAfterCutoff(record.StartDate, cutoffDateTime, cutoffGameDateTime));
                    ReopenContractWindowsAfterCutoffLocked(cutoffDateTime, cutoffGameDateTime);
                    state.RelationshipWindows.RemoveAll(
                        record => record != null
                            && IsDateAfterCutoff(record.StartDate, cutoffDateTime, cutoffGameDateTime));
                    ReopenRelationshipWindowsAfterCutoffLocked(cutoffDateTime, cutoffGameDateTime);
                    state.TourParticipation.RemoveAll(
                        record => record != null
                            && IsDateAfterCutoff(record.EventDate, cutoffDateTime, cutoffGameDateTime));
                    state.AwardResults.RemoveAll(
                        record => record != null
                            && IsDateAfterCutoff(record.EventDate, cutoffDateTime, cutoffGameDateTime));
                    state.ElectionResults.RemoveAll(
                        record => record != null
                            && IsDateAfterCutoff(record.EventDate, cutoffDateTime, cutoffGameDateTime));
                    state.PushWindows.RemoveAll(
                        record => record != null
                            && IsDateAfterCutoff(record.StartDate, cutoffDateTime, cutoffGameDateTime));
                    ReopenPushWindowsAfterCutoffLocked(cutoffDateTime, cutoffGameDateTime);

                    if (SaveStateLocked(out errorMessage))
                    {
                        return true;
                    }

                    state = originalState;
                    return false;
                }
                catch (Exception exception)
                {
                    state = originalState;
                    errorMessage = CoreConstants.MessageTryRollbackToGameDateTimeFailedPrefix + exception.Message;
                    CoreLog.Error(errorMessage);
                    return false;
                }
            }
        }

        /// <summary>
        /// Flat-file storage is already scoped per file; save-key remap is a no-op.
        /// </summary>
        public bool TryRemapSaveKey(string sourceSaveKey, string targetSaveKey, out string errorMessage)
        {
            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Disposes flat-file state.
        /// </summary>
        public void Dispose()
        {
            lock (storageLock)
            {
                disposed = true;
                state = null;
            }
        }

        /// <summary>
        /// Returns true when a stored round-trip date string is later than one cutoff date.
        /// </summary>
        private static bool IsDateAfterCutoff(string dateValue, string cutoffDateTime, DateTime cutoffGameDateTime)
        {
            if (string.IsNullOrEmpty(dateValue))
            {
                return false;
            }

            DateTime parsedDateValue;
            bool parsed =
                DateTime.TryParse(
                    dateValue,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out parsedDateValue);
            if (parsed)
            {
                return parsedDateValue > cutoffGameDateTime;
            }

            return string.CompareOrdinal(dateValue, cutoffDateTime) > CoreConstants.ZeroBasedListStartIndex;
        }

        /// <summary>
        /// Loads state from disk if a fallback file exists.
        /// </summary>
        private void LoadStateFromDiskLocked()
        {
            string temporaryPath = storagePath + TemporaryFileSuffix;
            string backupPath = storagePath + BackupFileSuffix;
            bool primaryExists = File.Exists(storagePath);
            bool backupExists = File.Exists(backupPath);
            bool temporaryExists = File.Exists(temporaryPath);
            if (!primaryExists && !backupExists && !temporaryExists)
            {
                state = new FlatFileState();
                return;
            }

            Exception primaryReadException = null;
            if (File.Exists(storagePath))
            {
                try
                {
                    state = DeserializeState(File.ReadAllText(storagePath, Encoding.UTF8));
                    return;
                }
                catch (Exception exception)
                {
                    primaryReadException = exception;
                }
            }

            Exception backupReadException = null;
            if (backupExists)
            {
                try
                {
                    string backupJson = File.ReadAllText(backupPath, Encoding.UTF8);
                    FlatFileState recoveredState = DeserializeState(backupJson);
                    if (primaryExists)
                    {
                        PreserveCorruptFileLocked(storagePath);
                    }

                    WriteSerializedStateAtomicallyLocked(backupJson, false);
                    state = recoveredState;
                    return;
                }
                catch (Exception exception)
                {
                    backupReadException = exception;
                }
            }

            if (temporaryExists)
            {
                try
                {
                    string temporaryJson = File.ReadAllText(temporaryPath, Encoding.UTF8);
                    FlatFileState recoveredState = DeserializeState(temporaryJson);
                    if (primaryExists && File.Exists(storagePath))
                    {
                        PreserveCorruptFileLocked(storagePath);
                    }

                    if (backupExists && File.Exists(backupPath))
                    {
                        PreserveCorruptFileLocked(backupPath);
                    }

                    File.Move(temporaryPath, storagePath);
                    state = recoveredState;
                    return;
                }
                catch (Exception temporaryReadException)
                {
                    throw new InvalidDataException(
                        "The flat-file primary, backup, and temporary states are unreadable.",
                        BuildRecoveryException(primaryReadException, backupReadException, temporaryReadException));
                }
            }

            throw new InvalidDataException(
                "The flat-file state is unreadable and no valid recovery file is available.",
                BuildRecoveryException(primaryReadException, backupReadException));
        }

        /// <summary>
        /// Writes state to disk.
        /// </summary>
        private bool SaveStateLocked(out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                EnsureStateInitializedLocked();
                string serializedJson = SerializeState(state);
                WriteSerializedStateAtomicallyLocked(serializedJson, true);
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = CoreConstants.MessageFlatFileWriteFailedPrefix + exception.Message;
                CoreLog.Error(errorMessage);
                return false;
            }
        }

        /// <summary>
        /// Returns a detached copy so a failed write cannot leak tentative mutations into memory.
        /// </summary>
        private FlatFileState CloneStateLocked()
        {
            EnsureStateInitializedLocked();
            return DeserializeState(SerializeState(state));
        }

        private static string SerializeState(FlatFileState value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            int originalFormatVersion = value.FormatVersion;
            string originalIntegritySha256 = value.IntegritySha256;
            try
            {
                value.FormatVersion = CurrentFlatFileFormatVersion;
                value.IntegritySha256 = string.Empty;
                string canonicalJson = JsonUtility.ToJson(value, CoreConstants.PrettyPrintJsonPayload);
                value.IntegritySha256 = ComputeSha256Hex(canonicalJson);
                return JsonUtility.ToJson(value, CoreConstants.PrettyPrintJsonPayload);
            }
            finally
            {
                value.FormatVersion = originalFormatVersion;
                value.IntegritySha256 = originalIntegritySha256;
            }
        }

        private static FlatFileState DeserializeState(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                throw new InvalidDataException("The flat-file state is empty.");
            }

            string trimmedJson = rawJson.Trim();
            if (trimmedJson.Length < 2
                || trimmedJson[CoreConstants.ZeroBasedListStartIndex] != CoreConstants.JsonObjectStartCharacter
                || trimmedJson[trimmedJson.Length - CoreConstants.LastElementOffsetFromCount] != CoreConstants.JsonObjectEndCharacter)
            {
                throw new InvalidDataException("The flat-file state is not a JSON object.");
            }

            HashSet<string> topLevelFields = ReadTopLevelJsonFieldNames(trimmedJson);
            RequireLegacyStateField(topLevelFields, "NextEventId");
            RequireLegacyStateField(topLevelFields, "Events");
            RequireLegacyStateField(topLevelFields, "CustomData");
            RequireLegacyStateField(topLevelFields, "SingleParticipation");
            RequireLegacyStateField(topLevelFields, "StatusWindows");
            RequireLegacyStateField(topLevelFields, "ShowCastWindows");
            RequireLegacyStateField(topLevelFields, "ContractWindows");
            RequireLegacyStateField(topLevelFields, "RelationshipWindows");
            RequireLegacyStateField(topLevelFields, "TourParticipation");
            RequireLegacyStateField(topLevelFields, "AwardResults");
            RequireLegacyStateField(topLevelFields, "ElectionResults");
            RequireLegacyStateField(topLevelFields, "PushWindows");

            bool declaresIntegrityFormat = topLevelFields.Contains("FormatVersion")
                || topLevelFields.Contains("IntegritySha256");
            FlatFileState deserializedState = JsonUtility.FromJson<FlatFileState>(rawJson);
            if (deserializedState == null)
            {
                throw new InvalidDataException("The flat-file state could not be deserialized.");
            }

            if (deserializedState.FormatVersion == CoreConstants.ZeroBasedListStartIndex)
            {
                if (declaresIntegrityFormat)
                {
                    throw new InvalidDataException("The flat-file integrity metadata is incomplete.");
                }

                NormalizeOptionalStateFields(deserializedState);
                ValidateStateStructure(deserializedState);
                MigrateLegacyCheckpointMetadata(deserializedState);
                ValidateCheckpointHistory(deserializedState);
                return deserializedState;
            }

            if (deserializedState.FormatVersion ==
                PreviousFlatFileFormatVersion)
            {
                FlatFileStateVersionOne versionOneState =
                    JsonUtility.FromJson<FlatFileStateVersionOne>(rawJson);
                if (versionOneState == null ||
                    string.IsNullOrEmpty(versionOneState.IntegritySha256))
                {
                    throw new InvalidDataException(
                        "The version-1 flat-file integrity metadata is incomplete.");
                }

                string storedVersionOneHash =
                    versionOneState.IntegritySha256;
                versionOneState.IntegritySha256 = string.Empty;
                string versionOneCanonicalJson = JsonUtility.ToJson(
                    versionOneState,
                    CoreConstants.PrettyPrintJsonPayload);
                versionOneState.IntegritySha256 = storedVersionOneHash;
                if (!string.Equals(
                    storedVersionOneHash,
                    ComputeSha256Hex(versionOneCanonicalJson),
                    StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        "The version-1 flat-file integrity hash does not match its contents.");
                }

                deserializedState = ConvertVersionOneState(
                    versionOneState);
                NormalizeOptionalStateFields(deserializedState);
                ValidateStateStructure(deserializedState);
                MigrateLegacyCheckpointMetadata(deserializedState);
                ValidateCheckpointHistory(deserializedState);
                return deserializedState;
            }

            if (deserializedState.FormatVersion !=
                CurrentFlatFileFormatVersion)
            {
                throw new InvalidDataException("The flat-file format version is unsupported.");
            }

            RequireLegacyStateField(topLevelFields, "Checkpoints");
            NormalizeOptionalStateFields(deserializedState);
            ValidateStateStructure(deserializedState);

            string storedIntegritySha256 = deserializedState.IntegritySha256 ?? string.Empty;
            if (string.IsNullOrEmpty(storedIntegritySha256))
            {
                throw new InvalidDataException("The flat-file integrity hash is missing.");
            }

            string computedIntegritySha256 = ComputeStateIntegritySha256(deserializedState);
            if (!string.Equals(storedIntegritySha256, computedIntegritySha256, StringComparison.Ordinal))
            {
                throw new InvalidDataException("The flat-file integrity hash does not match its contents.");
            }

            MigrateLegacyCheckpointMetadata(deserializedState);
            ValidateCheckpointHistory(deserializedState);
            return deserializedState;
        }

        private static FlatFileState ConvertVersionOneState(
            FlatFileStateVersionOne source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new FlatFileState
            {
                FormatVersion = source.FormatVersion,
                IntegritySha256 = source.IntegritySha256,
                NextEventId = source.NextEventId,
                Events = source.Events,
                CustomData = source.CustomData,
                SingleParticipation = source.SingleParticipation,
                StatusWindows = source.StatusWindows,
                ShowCastWindows = source.ShowCastWindows,
                ContractWindows = source.ContractWindows,
                RelationshipWindows = source.RelationshipWindows,
                TourParticipation = source.TourParticipation,
                AwardResults = source.AwardResults,
                ElectionResults = source.ElectionResults,
                PushWindows = source.PushWindows,
                CheckpointFingerprint = source.CheckpointFingerprint,
                CheckpointEventWatermark =
                    source.CheckpointEventWatermark,
                CheckpointSnapshotJson = source.CheckpointSnapshotJson,
                CheckpointCreatedUtc = source.CheckpointCreatedUtc,
                Checkpoints = new List<FlatFileCheckpointRecord>()
            };
        }

        private static void NormalizeOptionalStateFields(FlatFileState value)
        {
            if (value == null)
            {
                return;
            }

            value.IntegritySha256 = value.IntegritySha256 ?? string.Empty;
            value.CheckpointFingerprint =
                value.CheckpointFingerprint ?? string.Empty;
            value.CheckpointSnapshotJson =
                value.CheckpointSnapshotJson ?? string.Empty;
            value.CheckpointCreatedUtc =
                value.CheckpointCreatedUtc ?? string.Empty;
            if (value.Checkpoints == null)
            {
                value.Checkpoints = new List<FlatFileCheckpointRecord>();
            }
        }

        private static void MigrateLegacyCheckpointMetadata(
            FlatFileState value)
        {
            if (value == null)
            {
                return;
            }

            bool hasLegacyCheckpoint =
                !string.IsNullOrEmpty(value.CheckpointFingerprint) ||
                !string.IsNullOrEmpty(value.CheckpointSnapshotJson) ||
                !string.IsNullOrEmpty(value.CheckpointCreatedUtc) ||
                value.CheckpointEventWatermark != 0L;
            if (hasLegacyCheckpoint)
            {
                if (string.IsNullOrEmpty(value.CheckpointFingerprint) ||
                    string.IsNullOrEmpty(value.CheckpointSnapshotJson) ||
                    string.IsNullOrEmpty(value.CheckpointCreatedUtc))
                {
                    throw new InvalidDataException(
                        "The legacy flat-file checkpoint metadata is incomplete.");
                }

                bool alreadyPresent = false;
                for (int checkpointIndex =
                        CoreConstants.ZeroBasedListStartIndex;
                    checkpointIndex < value.Checkpoints.Count;
                    checkpointIndex++)
                {
                    FlatFileCheckpointRecord existing =
                        value.Checkpoints[checkpointIndex];
                    if (existing != null &&
                        string.Equals(
                            existing.Fingerprint,
                            value.CheckpointFingerprint,
                            StringComparison.Ordinal))
                    {
                        alreadyPresent = true;
                        break;
                    }
                }

                if (!alreadyPresent)
                {
                    value.Checkpoints.Add(
                        new FlatFileCheckpointRecord
                        {
                            Fingerprint = value.CheckpointFingerprint,
                            EventWatermark =
                                value.CheckpointEventWatermark,
                            SnapshotJson = value.CheckpointSnapshotJson,
                            CreatedUtc = value.CheckpointCreatedUtc
                        });
                }
            }

            value.CheckpointFingerprint = string.Empty;
            value.CheckpointEventWatermark = 0L;
            value.CheckpointSnapshotJson = string.Empty;
            value.CheckpointCreatedUtc = string.Empty;
            while (value.Checkpoints.Count >
                MaximumRetainedSaveGenerations)
            {
                value.Checkpoints.RemoveAt(
                    CoreConstants.ZeroBasedListStartIndex);
            }
        }

        private static void ValidateCheckpointHistory(FlatFileState value)
        {
            if (value == null || value.Checkpoints == null)
            {
                throw new InvalidDataException(
                    "The flat-file checkpoint history is missing.");
            }

            if (value.Checkpoints.Count > MaximumRetainedSaveGenerations)
            {
                throw new InvalidDataException(
                    "The flat-file checkpoint history exceeds its retention bound.");
            }

            HashSet<string> fingerprints = new HashSet<string>(
                StringComparer.Ordinal);
            for (int checkpointIndex = CoreConstants.ZeroBasedListStartIndex;
                checkpointIndex < value.Checkpoints.Count;
                checkpointIndex++)
            {
                FlatFileCheckpointRecord checkpoint =
                    value.Checkpoints[checkpointIndex];
                if (checkpoint == null ||
                    string.IsNullOrEmpty(checkpoint.Fingerprint) ||
                    checkpoint.EventWatermark < 0L ||
                    string.IsNullOrEmpty(checkpoint.SnapshotJson) ||
                    string.IsNullOrEmpty(checkpoint.CreatedUtc) ||
                    !fingerprints.Add(checkpoint.Fingerprint))
                {
                    throw new InvalidDataException(
                        "The flat-file checkpoint history contains an invalid or duplicate generation.");
                }

                FlatFileState checkpointState = DeserializeState(
                    checkpoint.SnapshotJson);
                if (HasAnyCheckpointMetadata(checkpointState))
                {
                    throw new InvalidDataException(
                        "A flat-file save-generation snapshot contains nested checkpoint metadata.");
                }

                if (ComputeMaximumEventId(checkpointState) !=
                    checkpoint.EventWatermark)
                {
                    throw new InvalidDataException(
                        "A flat-file save-generation event watermark is inconsistent.");
                }
            }
        }

        private static bool HasAnyCheckpointMetadata(FlatFileState value)
        {
            return value != null &&
                (!string.IsNullOrEmpty(value.CheckpointFingerprint) ||
                 !string.IsNullOrEmpty(value.CheckpointSnapshotJson) ||
                 !string.IsNullOrEmpty(value.CheckpointCreatedUtc) ||
                 value.CheckpointEventWatermark != 0L ||
                 (value.Checkpoints != null &&
                  value.Checkpoints.Count >
                    CoreConstants.ZeroBasedListStartIndex));
        }

        private static void RequireLegacyStateField(HashSet<string> topLevelFields, string fieldName)
        {
            if (topLevelFields == null || !topLevelFields.Contains(fieldName))
            {
                throw new InvalidDataException("The flat-file state is missing the " + fieldName + " field.");
            }
        }

        private static HashSet<string> ReadTopLevelJsonFieldNames(string jsonText)
        {
            HashSet<string> fieldNames = new HashSet<string>(StringComparer.Ordinal);
            int index = CoreConstants.ZeroBasedListStartIndex;
            SkipJsonWhitespace(jsonText, ref index);
            if (index >= jsonText.Length || jsonText[index] != CoreConstants.JsonObjectStartCharacter)
            {
                throw new InvalidDataException("The flat-file state has no root JSON object.");
            }

            index++;
            SkipJsonWhitespace(jsonText, ref index);
            if (index < jsonText.Length && jsonText[index] == CoreConstants.JsonObjectEndCharacter)
            {
                index++;
            }
            else
            {
                while (index < jsonText.Length)
                {
                    string fieldName = ReadPlainJsonPropertyName(jsonText, ref index);
                    if (!fieldNames.Add(fieldName))
                    {
                        throw new InvalidDataException("The flat-file state contains a duplicate root field: " + fieldName);
                    }

                    SkipJsonWhitespace(jsonText, ref index);
                    if (index >= jsonText.Length || jsonText[index] != CoreConstants.JsonNameValueSeparatorCharacter)
                    {
                        throw new InvalidDataException("The flat-file state contains a root field without a value separator.");
                    }

                    index++;
                    SkipOneJsonValue(jsonText, ref index);
                    SkipJsonWhitespace(jsonText, ref index);
                    if (index >= jsonText.Length)
                    {
                        throw new InvalidDataException("The flat-file root object is unterminated.");
                    }

                    if (jsonText[index] == CoreConstants.JsonObjectEndCharacter)
                    {
                        index++;
                        break;
                    }

                    if (jsonText[index] != CoreConstants.JsonPropertySeparatorCharacter)
                    {
                        throw new InvalidDataException("The flat-file root object contains an invalid field separator.");
                    }

                    index++;
                    SkipJsonWhitespace(jsonText, ref index);
                }
            }

            SkipJsonWhitespace(jsonText, ref index);
            if (index != jsonText.Length)
            {
                throw new InvalidDataException("The flat-file state contains data after its root object.");
            }

            return fieldNames;
        }

        private static string ReadPlainJsonPropertyName(string jsonText, ref int index)
        {
            SkipJsonWhitespace(jsonText, ref index);
            if (index >= jsonText.Length || jsonText[index] != CoreConstants.JsonStringQuoteCharacter)
            {
                throw new InvalidDataException("The flat-file root object contains an invalid field name.");
            }

            index++;
            int startIndex = index;
            while (index < jsonText.Length)
            {
                char character = jsonText[index];
                if (character == CoreConstants.JsonEscapeCharacter)
                {
                    throw new InvalidDataException("Escaped flat-file root field names are not supported.");
                }

                if (character == CoreConstants.JsonStringQuoteCharacter)
                {
                    string fieldName = jsonText.Substring(startIndex, index - startIndex);
                    index++;
                    return fieldName;
                }

                if (character < ' ')
                {
                    throw new InvalidDataException("The flat-file root object contains a control character in a field name.");
                }

                index++;
            }

            throw new InvalidDataException("The flat-file root object contains an unterminated field name.");
        }

        private static void SkipOneJsonValue(string jsonText, ref int index)
        {
            SkipJsonWhitespace(jsonText, ref index);
            if (index >= jsonText.Length)
            {
                throw new InvalidDataException("The flat-file root object contains a missing value.");
            }

            char firstCharacter = jsonText[index];
            if (firstCharacter == CoreConstants.JsonStringQuoteCharacter)
            {
                SkipJsonString(jsonText, ref index);
                return;
            }

            if (firstCharacter == CoreConstants.JsonObjectStartCharacter || firstCharacter == JsonArrayStartCharacter)
            {
                Stack<char> expectedClosures = new Stack<char>();
                expectedClosures.Push(
                    firstCharacter == CoreConstants.JsonObjectStartCharacter
                        ? CoreConstants.JsonObjectEndCharacter
                        : JsonArrayEndCharacter);
                index++;
                while (index < jsonText.Length && expectedClosures.Count > CoreConstants.ZeroBasedListStartIndex)
                {
                    char character = jsonText[index];
                    if (character == CoreConstants.JsonStringQuoteCharacter)
                    {
                        SkipJsonString(jsonText, ref index);
                        continue;
                    }

                    if (character == CoreConstants.JsonObjectStartCharacter)
                    {
                        expectedClosures.Push(CoreConstants.JsonObjectEndCharacter);
                    }
                    else if (character == JsonArrayStartCharacter)
                    {
                        expectedClosures.Push(JsonArrayEndCharacter);
                    }
                    else if (character == CoreConstants.JsonObjectEndCharacter || character == JsonArrayEndCharacter)
                    {
                        if (expectedClosures.Count == CoreConstants.ZeroBasedListStartIndex || expectedClosures.Pop() != character)
                        {
                            throw new InvalidDataException("The flat-file state contains mismatched JSON containers.");
                        }
                    }

                    index++;
                }

                if (expectedClosures.Count != CoreConstants.ZeroBasedListStartIndex)
                {
                    throw new InvalidDataException("The flat-file state contains an unterminated JSON container.");
                }

                return;
            }

            int primitiveStartIndex = index;
            while (index < jsonText.Length
                && jsonText[index] != CoreConstants.JsonPropertySeparatorCharacter
                && jsonText[index] != CoreConstants.JsonObjectEndCharacter)
            {
                index++;
            }

            if (string.IsNullOrWhiteSpace(jsonText.Substring(primitiveStartIndex, index - primitiveStartIndex)))
            {
                throw new InvalidDataException("The flat-file root object contains an empty primitive value.");
            }
        }

        private static void SkipJsonString(string jsonText, ref int index)
        {
            if (index >= jsonText.Length || jsonText[index] != CoreConstants.JsonStringQuoteCharacter)
            {
                throw new InvalidDataException("The flat-file state contains an invalid JSON string.");
            }

            index++;
            while (index < jsonText.Length)
            {
                char character = jsonText[index++];
                if (character == CoreConstants.JsonEscapeCharacter)
                {
                    if (index >= jsonText.Length)
                    {
                        throw new InvalidDataException("The flat-file state contains an unterminated JSON escape.");
                    }

                    index++;
                    continue;
                }

                if (character == CoreConstants.JsonStringQuoteCharacter)
                {
                    return;
                }
            }

            throw new InvalidDataException("The flat-file state contains an unterminated JSON string.");
        }

        private static void SkipJsonWhitespace(string jsonText, ref int index)
        {
            while (index < jsonText.Length && char.IsWhiteSpace(jsonText[index]))
            {
                index++;
            }
        }

        private static void ValidateStateStructure(FlatFileState value)
        {
            if (value.Events == null
                || value.CustomData == null
                || value.SingleParticipation == null
                || value.StatusWindows == null
                || value.ShowCastWindows == null
                || value.ContractWindows == null
                || value.RelationshipWindows == null
                || value.TourParticipation == null
                || value.AwardResults == null
                || value.ElectionResults == null
                || value.PushWindows == null
                || value.Checkpoints == null)
            {
                throw new InvalidDataException("The flat-file state contains a null core list.");
            }

            if (value.IntegritySha256 == null
                || value.CheckpointFingerprint == null
                || value.CheckpointSnapshotJson == null
                || value.CheckpointCreatedUtc == null
                || value.CheckpointEventWatermark < 0L
                || value.Checkpoints.Count >
                    MaximumRetainedSaveGenerations)
            {
                throw new InvalidDataException("The flat-file state contains invalid integrity or checkpoint metadata.");
            }

            long maximumEventId = 0L;
            HashSet<long> eventIds = new HashSet<long>();
            for (int i = CoreConstants.ZeroBasedListStartIndex; i < value.Events.Count; i++)
            {
                FlatFileEventRecord record = value.Events[i];
                if (record == null
                    || record.EventId <= 0L
                    || !eventIds.Add(record.EventId)
                    || HasNullString(
                        record.GameDateTime,
                        record.EntityKind,
                        record.EntityId,
                        record.EventType,
                        record.SourcePatch,
                        record.NamespaceIdentifier,
                        record.PayloadJson))
                {
                    throw new InvalidDataException("The flat-file state contains an invalid event record.");
                }

                if (record.EventId > maximumEventId)
                {
                    maximumEventId = record.EventId;
                }
            }

            if (value.NextEventId <= maximumEventId || value.NextEventId < 1L)
            {
                throw new InvalidDataException("The flat-file next event identifier is not monotonic.");
            }

            bool hasCheckpoint = !string.IsNullOrEmpty(value.CheckpointFingerprint)
                || !string.IsNullOrEmpty(value.CheckpointSnapshotJson)
                || !string.IsNullOrEmpty(value.CheckpointCreatedUtc)
                || value.CheckpointEventWatermark != 0L;
            if (hasCheckpoint
                && (string.IsNullOrEmpty(value.CheckpointFingerprint)
                    || string.IsNullOrEmpty(value.CheckpointSnapshotJson)
                    || string.IsNullOrEmpty(value.CheckpointCreatedUtc)))
            {
                throw new InvalidDataException("The flat-file checkpoint metadata is incomplete or inconsistent.");
            }

            HashSet<string> customKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = CoreConstants.ZeroBasedListStartIndex; i < value.CustomData.Count; i++)
            {
                FlatFileCustomDataRecord record = value.CustomData[i];
                if (record == null
                    || HasNullString(record.NamespaceIdentifier, record.DataKey, record.ValueJson, record.UpdatedUtc)
                    || !customKeys.Add(BuildCompositeStringKey(record.NamespaceIdentifier, record.DataKey)))
                {
                    throw new InvalidDataException("The flat-file state contains an invalid or duplicate custom-data record.");
                }
            }

            HashSet<string> singleKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = CoreConstants.ZeroBasedListStartIndex; i < value.SingleParticipation.Count; i++)
            {
                FlatFileSingleParticipationRecord record = value.SingleParticipation[i];
                string key = record == null
                    ? string.Empty
                    : record.SingleId.ToString(CultureInfo.InvariantCulture) + ":" + record.IdolId.ToString(CultureInfo.InvariantCulture);
                if (record == null
                    || record.ReleaseDate == null
                    || !singleKeys.Add(key))
                {
                    throw new InvalidDataException("The flat-file state contains an invalid or duplicate single-participation record.");
                }
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < value.StatusWindows.Count; i++)
            {
                FlatFileStatusWindowRecord record = value.StatusWindows[i];
                if (record == null || HasNullString(record.StatusType, record.StartDate, record.EndDate))
                {
                    throw new InvalidDataException("The flat-file state contains an invalid status-window record.");
                }
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < value.ShowCastWindows.Count; i++)
            {
                FlatFileShowCastWindowRecord record = value.ShowCastWindows[i];
                if (record == null || HasNullString(record.ShowId, record.StartDate, record.EndDate, record.EndReason, record.PayloadJson))
                {
                    throw new InvalidDataException("The flat-file state contains an invalid show-cast-window record.");
                }
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < value.ContractWindows.Count; i++)
            {
                FlatFileContractWindowRecord record = value.ContractWindows[i];
                if (record == null || HasNullString(record.ContractKey, record.StartDate, record.EndDate, record.EndReason, record.PayloadJson))
                {
                    throw new InvalidDataException("The flat-file state contains an invalid contract-window record.");
                }
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < value.RelationshipWindows.Count; i++)
            {
                FlatFileRelationshipWindowRecord record = value.RelationshipWindows[i];
                if (record == null
                    || HasNullString(
                        record.RelationshipKey,
                        record.RelationshipType,
                        record.StartDate,
                        record.EndDate,
                        record.EndReason,
                        record.PayloadJson))
                {
                    throw new InvalidDataException("The flat-file state contains an invalid relationship-window record.");
                }
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < value.TourParticipation.Count; i++)
            {
                FlatFileTourParticipationRecord record = value.TourParticipation[i];
                if (record == null || HasNullString(record.TourId, record.LifecycleAction, record.EventDate, record.PayloadJson))
                {
                    throw new InvalidDataException("The flat-file state contains an invalid tour-participation record.");
                }
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < value.AwardResults.Count; i++)
            {
                FlatFileAwardResultProjectionRecord record = value.AwardResults[i];
                if (record == null || HasNullString(record.AwardKey, record.EventDate, record.PayloadJson))
                {
                    throw new InvalidDataException("The flat-file state contains an invalid award-result record.");
                }
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < value.ElectionResults.Count; i++)
            {
                FlatFileElectionResultProjectionRecord record = value.ElectionResults[i];
                if (record == null || HasNullString(record.ElectionId, record.EventDate, record.PayloadJson))
                {
                    throw new InvalidDataException("The flat-file state contains an invalid election-result record.");
                }
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < value.PushWindows.Count; i++)
            {
                FlatFilePushWindowRecord record = value.PushWindows[i];
                if (record == null || HasNullString(record.SlotKey, record.StartDate, record.EndDate, record.EndReason, record.PayloadJson))
                {
                    throw new InvalidDataException("The flat-file state contains an invalid push-window record.");
                }
            }
        }

        private static bool HasNullString(params string[] values)
        {
            if (values == null)
            {
                return true;
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < values.Length; i++)
            {
                if (values[i] == null)
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildCompositeStringKey(string first, string second)
        {
            string normalizedFirst = first ?? string.Empty;
            string normalizedSecond = second ?? string.Empty;
            return normalizedFirst.Length.ToString(CultureInfo.InvariantCulture)
                + ":" + normalizedFirst + normalizedSecond;
        }

        private static string ComputeStateIntegritySha256(FlatFileState value)
        {
            string originalIntegritySha256 = value.IntegritySha256;
            try
            {
                value.IntegritySha256 = string.Empty;
                string canonicalJson = JsonUtility.ToJson(value, CoreConstants.PrettyPrintJsonPayload);
                return ComputeSha256Hex(canonicalJson);
            }
            finally
            {
                value.IntegritySha256 = originalIntegritySha256;
            }
        }

        private static string ComputeSha256Hex(string value)
        {
            byte[] valueBytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            byte[] hashBytes;
            using (SHA256 sha256 = SHA256.Create())
            {
                hashBytes = sha256.ComputeHash(valueBytes);
            }

            StringBuilder builder = new StringBuilder(hashBytes.Length * 2);
            for (int i = CoreConstants.ZeroBasedListStartIndex; i < hashBytes.Length; i++)
            {
                builder.Append(
                    hashBytes[i].ToString(
                        CoreConstants.ByteToLowerHexFormat,
                        CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        /// <summary>
        /// Flushes a temporary file before atomically replacing the durable primary file.
        /// </summary>
        private void WriteSerializedStateAtomicallyLocked(string serializedJson, bool preserveCurrentAsBackup)
        {
            string temporaryPath = storagePath + TemporaryFileSuffix;
            string backupPath = storagePath + BackupFileSuffix;
            bool committed = false;

            TryDeleteFileBestEffort(temporaryPath);
            try
            {
                byte[] serializedBytes = new UTF8Encoding(false).GetBytes(serializedJson ?? string.Empty);
                using (FileStream stream = new FileStream(
                    temporaryPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    CoreConstants.JsonBuilderDefaultCapacity,
                    FileOptions.WriteThrough))
                {
                    stream.Write(serializedBytes, CoreConstants.ZeroBasedListStartIndex, serializedBytes.Length);
                    stream.Flush(true);
                }

                if (!File.Exists(storagePath))
                {
                    File.Move(temporaryPath, storagePath);
                    committed = true;
                    return;
                }

                try
                {
                    File.Replace(
                        temporaryPath,
                        storagePath,
                        preserveCurrentAsBackup ? backupPath : null,
                        true);
                }
                catch (PlatformNotSupportedException)
                {
                    ReplaceStateFileWithRecoverableMovesLocked(temporaryPath, backupPath, preserveCurrentAsBackup);
                }
                catch (NotSupportedException)
                {
                    ReplaceStateFileWithRecoverableMovesLocked(temporaryPath, backupPath, preserveCurrentAsBackup);
                }

                committed = true;
            }
            finally
            {
                if (!committed)
                {
                    TryDeleteFileBestEffort(temporaryPath);
                }
            }
        }

        private void ReplaceStateFileWithRecoverableMovesLocked(
            string temporaryPath,
            string backupPath,
            bool preserveCurrentAsBackup)
        {
            if (preserveCurrentAsBackup)
            {
                TryDeleteFileBestEffort(backupPath);
                File.Move(storagePath, backupPath);
            }
            else
            {
                File.Delete(storagePath);
            }

            File.Move(temporaryPath, storagePath);
        }

        private static void TryDeleteFileBestEffort(string filePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
            }
        }

        private static void PreserveCorruptFileLocked(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return;
            }

            string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            string quarantinePath = filePath + ".corrupt." + timestamp;
            int suffix = CoreConstants.ZeroBasedListStartIndex;
            while (File.Exists(quarantinePath))
            {
                suffix++;
                quarantinePath = filePath + ".corrupt." + timestamp + "." + suffix.ToString(CultureInfo.InvariantCulture);
            }

            File.Move(filePath, quarantinePath);
        }

        private static Exception BuildRecoveryException(params Exception[] exceptions)
        {
            List<Exception> nonNullExceptions = new List<Exception>();
            if (exceptions != null)
            {
                for (int i = CoreConstants.ZeroBasedListStartIndex; i < exceptions.Length; i++)
                {
                    if (exceptions[i] != null)
                    {
                        nonNullExceptions.Add(exceptions[i]);
                    }
                }
            }

            return nonNullExceptions.Count > CoreConstants.ZeroBasedListStartIndex
                ? (Exception)new AggregateException(nonNullExceptions)
                : new InvalidDataException("No recovery candidate was available.");
        }

        private static void ClearCheckpointMetadata(FlatFileState value)
        {
            if (value == null)
            {
                return;
            }

            value.CheckpointFingerprint = string.Empty;
            value.CheckpointEventWatermark = 0L;
            value.CheckpointSnapshotJson = string.Empty;
            value.CheckpointCreatedUtc = string.Empty;
            value.Checkpoints = new List<FlatFileCheckpointRecord>();
        }

        private int FindCheckpointIndexLocked(string fingerprint)
        {
            if (string.IsNullOrEmpty(fingerprint) ||
                state == null ||
                state.Checkpoints == null)
            {
                return CoreConstants.InvalidIdValue;
            }

            for (int checkpointIndex = state.Checkpoints.Count -
                    CoreConstants.LastElementOffsetFromCount;
                checkpointIndex >= CoreConstants.ZeroBasedListStartIndex;
                checkpointIndex--)
            {
                FlatFileCheckpointRecord checkpoint =
                    state.Checkpoints[checkpointIndex];
                if (checkpoint != null &&
                    string.Equals(
                        checkpoint.Fingerprint,
                        fingerprint,
                        StringComparison.Ordinal))
                {
                    return checkpointIndex;
                }
            }

            return CoreConstants.InvalidIdValue;
        }

        private void RemoveCheckpointByFingerprintLocked(string fingerprint)
        {
            if (state == null || state.Checkpoints == null)
            {
                return;
            }

            for (int checkpointIndex = state.Checkpoints.Count -
                    CoreConstants.LastElementOffsetFromCount;
                checkpointIndex >= CoreConstants.ZeroBasedListStartIndex;
                checkpointIndex--)
            {
                FlatFileCheckpointRecord checkpoint =
                    state.Checkpoints[checkpointIndex];
                if (checkpoint != null &&
                    string.Equals(
                        checkpoint.Fingerprint,
                        fingerprint,
                        StringComparison.Ordinal))
                {
                    state.Checkpoints.RemoveAt(checkpointIndex);
                }
            }
        }

        private List<FlatFileCheckpointRecord> CloneCheckpointHistoryLocked()
        {
            List<FlatFileCheckpointRecord> result =
                new List<FlatFileCheckpointRecord>();
            if (state == null || state.Checkpoints == null)
            {
                return result;
            }

            for (int checkpointIndex = CoreConstants.ZeroBasedListStartIndex;
                checkpointIndex < state.Checkpoints.Count;
                checkpointIndex++)
            {
                FlatFileCheckpointRecord source =
                    state.Checkpoints[checkpointIndex];
                if (source == null)
                {
                    continue;
                }

                result.Add(
                    new FlatFileCheckpointRecord
                    {
                        Fingerprint = source.Fingerprint,
                        EventWatermark = source.EventWatermark,
                        SnapshotJson = source.SnapshotJson,
                        CreatedUtc = source.CreatedUtc
                    });
            }

            return result;
        }

        private long ComputeMaximumEventIdLocked()
        {
            return ComputeMaximumEventId(state);
        }

        private static long ComputeMaximumEventId(FlatFileState value)
        {
            long maximumEventId = 0L;
            if (value == null || value.Events == null)
            {
                return maximumEventId;
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < value.Events.Count; i++)
            {
                FlatFileEventRecord record = value.Events[i];
                if (record != null && record.EventId > maximumEventId)
                {
                    maximumEventId = record.EventId;
                }
            }

            return maximumEventId;
        }

        private void ReopenShowCastWindowsAfterCutoffLocked(string cutoffDateTime, DateTime cutoffGameDateTime)
        {
            for (int i = CoreConstants.ZeroBasedListStartIndex; i < state.ShowCastWindows.Count; i++)
            {
                FlatFileShowCastWindowRecord record = state.ShowCastWindows[i];
                if (record != null && IsDateAfterCutoff(record.EndDate, cutoffDateTime, cutoffGameDateTime))
                {
                    record.EndDate = string.Empty;
                    record.EndReason = string.Empty;
                }
            }
        }

        private void ReopenContractWindowsAfterCutoffLocked(string cutoffDateTime, DateTime cutoffGameDateTime)
        {
            for (int i = CoreConstants.ZeroBasedListStartIndex; i < state.ContractWindows.Count; i++)
            {
                FlatFileContractWindowRecord record = state.ContractWindows[i];
                if (record != null && IsDateAfterCutoff(record.EndDate, cutoffDateTime, cutoffGameDateTime))
                {
                    record.EndDate = string.Empty;
                    record.EndReason = string.Empty;
                }
            }
        }

        private void ReopenRelationshipWindowsAfterCutoffLocked(string cutoffDateTime, DateTime cutoffGameDateTime)
        {
            for (int i = CoreConstants.ZeroBasedListStartIndex; i < state.RelationshipWindows.Count; i++)
            {
                FlatFileRelationshipWindowRecord record = state.RelationshipWindows[i];
                if (record != null && IsDateAfterCutoff(record.EndDate, cutoffDateTime, cutoffGameDateTime))
                {
                    record.EndDate = string.Empty;
                    record.EndReason = string.Empty;
                }
            }
        }

        private void ReopenPushWindowsAfterCutoffLocked(string cutoffDateTime, DateTime cutoffGameDateTime)
        {
            for (int i = CoreConstants.ZeroBasedListStartIndex; i < state.PushWindows.Count; i++)
            {
                FlatFilePushWindowRecord record = state.PushWindows[i];
                if (record != null && IsDateAfterCutoff(record.EndDate, cutoffDateTime, cutoffGameDateTime))
                {
                    record.EndDate = string.Empty;
                    record.EndReason = string.Empty;
                }
            }
        }

        /// <summary>
        /// Ensures state containers are present and identifiers are monotonic.
        /// </summary>
        private void EnsureStateInitializedLocked()
        {
            if (state == null)
            {
                state = new FlatFileState();
            }

            if (state.Events == null)
            {
                state.Events = new List<FlatFileEventRecord>();
            }

            if (state.CustomData == null)
            {
                state.CustomData = new List<FlatFileCustomDataRecord>();
            }

            if (state.SingleParticipation == null)
            {
                state.SingleParticipation = new List<FlatFileSingleParticipationRecord>();
            }

            if (state.StatusWindows == null)
            {
                state.StatusWindows = new List<FlatFileStatusWindowRecord>();
            }

            if (state.ShowCastWindows == null)
            {
                state.ShowCastWindows = new List<FlatFileShowCastWindowRecord>();
            }

            if (state.ContractWindows == null)
            {
                state.ContractWindows = new List<FlatFileContractWindowRecord>();
            }

            if (state.RelationshipWindows == null)
            {
                state.RelationshipWindows = new List<FlatFileRelationshipWindowRecord>();
            }

            if (state.TourParticipation == null)
            {
                state.TourParticipation = new List<FlatFileTourParticipationRecord>();
            }

            if (state.AwardResults == null)
            {
                state.AwardResults = new List<FlatFileAwardResultProjectionRecord>();
            }

            if (state.ElectionResults == null)
            {
                state.ElectionResults = new List<FlatFileElectionResultProjectionRecord>();
            }

            if (state.PushWindows == null)
            {
                state.PushWindows = new List<FlatFilePushWindowRecord>();
            }

            state.CheckpointFingerprint = state.CheckpointFingerprint ?? string.Empty;
            state.CheckpointSnapshotJson = state.CheckpointSnapshotJson ?? string.Empty;
            state.CheckpointCreatedUtc = state.CheckpointCreatedUtc ?? string.Empty;
            if (state.Checkpoints == null)
            {
                state.Checkpoints = new List<FlatFileCheckpointRecord>();
            }

            MigrateLegacyCheckpointMetadata(state);

            if (state.NextEventId < 1L)
            {
                state.NextEventId = ComputeNextEventIdLocked();
            }
        }

        /// <summary>
        /// Computes the next event identifier from existing rows.
        /// </summary>
        private long ComputeNextEventIdLocked()
        {
            long maximumEventId = 0L;
            for (int i = CoreConstants.ZeroBasedListStartIndex; i < state.Events.Count; i++)
            {
                FlatFileEventRecord eventRecord = state.Events[i];
                if (eventRecord != null && eventRecord.EventId > maximumEventId)
                {
                    maximumEventId = eventRecord.EventId;
                }
            }

            return maximumEventId + 1L;
        }

        /// <summary>
        /// Applies upsert behavior for single-participation projection rows.
        /// </summary>
        private void ApplySingleParticipationRowsLocked(IReadOnlyList<SingleParticipationProjection> singleParticipationRows)
        {
            if (singleParticipationRows == null || singleParticipationRows.Count == CoreConstants.ZeroBasedListStartIndex)
            {
                return;
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < singleParticipationRows.Count; i++)
            {
                SingleParticipationProjection row = singleParticipationRows[i];
                if (row == null)
                {
                    continue;
                }

                int existingIndex = FindSingleParticipationIndexLocked(row.SingleId, row.IdolId);
                FlatFileSingleParticipationRecord mappedRecord = new FlatFileSingleParticipationRecord
                {
                    SingleId = row.SingleId,
                    IdolId = row.IdolId,
                    RowIndex = row.RowIndex,
                    PositionIndex = row.PositionIndex,
                    IsCenterFlag = row.IsCenterFlag,
                    ReleaseDate = row.ReleaseDate ?? string.Empty
                };

                if (existingIndex >= CoreConstants.ZeroBasedListStartIndex)
                {
                    state.SingleParticipation[existingIndex] = mappedRecord;
                }
                else
                {
                    state.SingleParticipation.Add(mappedRecord);
                }
            }
        }

        /// <summary>
        /// Finds one single-participation projection index by `(single_id, idol_id)`.
        /// </summary>
        private int FindSingleParticipationIndexLocked(int singleId, int idolId)
        {
            for (int i = CoreConstants.ZeroBasedListStartIndex; i < state.SingleParticipation.Count; i++)
            {
                FlatFileSingleParticipationRecord record = state.SingleParticipation[i];
                if (record == null)
                {
                    continue;
                }

                if (record.SingleId == singleId && record.IdolId == idolId)
                {
                    return i;
                }
            }

            return CoreConstants.InvalidIdValue;
        }

        /// <summary>
        /// Applies close/open behavior for status-window projections.
        /// </summary>
        private void ApplyStatusTransitionsLocked(IReadOnlyList<StatusTransitionProjection> statusTransitions)
        {
            if (statusTransitions == null || statusTransitions.Count == CoreConstants.ZeroBasedListStartIndex)
            {
                return;
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < statusTransitions.Count; i++)
            {
                StatusTransitionProjection transition = statusTransitions[i];
                if (transition == null)
                {
                    continue;
                }

                bool statusActuallyChanged = !string.Equals(transition.PreviousStatusCode, transition.NewStatusCode, StringComparison.Ordinal);
                if (!statusActuallyChanged)
                {
                    continue;
                }

                if (CoreConstants.StatusCodesTrackedAsWindows.Contains(transition.PreviousStatusCode))
                {
                    for (int windowIndex = CoreConstants.ZeroBasedListStartIndex; windowIndex < state.StatusWindows.Count; windowIndex++)
                    {
                        FlatFileStatusWindowRecord existingWindow = state.StatusWindows[windowIndex];
                        if (existingWindow == null)
                        {
                            continue;
                        }

                        bool idolMatches = existingWindow.IdolId == transition.IdolId;
                        bool statusMatches = string.Equals(existingWindow.StatusType, transition.PreviousStatusCode, StringComparison.Ordinal);
                        bool isOpenWindow = string.IsNullOrEmpty(existingWindow.EndDate);
                        if (idolMatches && statusMatches && isOpenWindow)
                        {
                            existingWindow.EndDate = transition.TransitionDate ?? string.Empty;
                        }
                    }
                }

                if (CoreConstants.StatusCodesTrackedAsWindows.Contains(transition.NewStatusCode))
                {
                    bool openWindowExists = false;
                    for (int windowIndex = CoreConstants.ZeroBasedListStartIndex; windowIndex < state.StatusWindows.Count; windowIndex++)
                    {
                        FlatFileStatusWindowRecord existingWindow = state.StatusWindows[windowIndex];
                        if (existingWindow == null)
                        {
                            continue;
                        }

                        bool idolMatches = existingWindow.IdolId == transition.IdolId;
                        bool statusMatches = string.Equals(existingWindow.StatusType, transition.NewStatusCode, StringComparison.Ordinal);
                        bool isOpenWindow = string.IsNullOrEmpty(existingWindow.EndDate);
                        if (idolMatches && statusMatches && isOpenWindow)
                        {
                            openWindowExists = true;
                            break;
                        }
                    }

                    if (!openWindowExists)
                    {
                        state.StatusWindows.Add(
                            new FlatFileStatusWindowRecord
                            {
                                IdolId = transition.IdolId,
                                StatusType = transition.NewStatusCode ?? string.Empty,
                                StartDate = transition.TransitionDate ?? string.Empty,
                                EndDate = string.Empty
                            });
                    }
                }
            }
        }

        /// <summary>
        /// Applies event-derived read-model projection mutations.
        /// </summary>
        private void ApplyDerivedReadModelProjectionsLocked(IReadOnlyList<PendingEvent> pendingEvents)
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

            ApplyShowCastWindowMutationsLocked(showCastMutations);
            ApplyContractWindowMutationsLocked(contractMutations);
            ApplyRelationshipWindowMutationsLocked(relationshipMutations);
            UpsertTourParticipationRowsLocked(tourParticipationRows);
            UpsertAwardResultRowsLocked(awardResultRows);
            UpsertElectionResultRowsLocked(electionResultRows);
            ApplyPushWindowMutationsLocked(pushMutations);
        }

        /// <summary>
        /// Applies open/close mutations for show-cast windows.
        /// </summary>
        private void ApplyShowCastWindowMutationsLocked(IReadOnlyList<ShowCastWindowProjectionMutation> mutations)
        {
            if (mutations == null || mutations.Count == CoreConstants.ZeroBasedListStartIndex)
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
                    int existingOpenIndex;
                    if (!TryFindOpenShowCastWindowIndexLocked(mutation.ShowId, mutation.IdolId, out existingOpenIndex))
                    {
                        state.ShowCastWindows.Add(
                            new FlatFileShowCastWindowRecord
                            {
                                ShowId = mutation.ShowId ?? string.Empty,
                                IdolId = mutation.IdolId,
                                StartDate = mutation.StartDate ?? string.Empty,
                                EndDate = string.Empty,
                                EndReason = string.Empty,
                                PayloadJson = mutation.PayloadJson ?? CoreConstants.EmptyJsonObject
                            });
                    }

                    continue;
                }

                for (int recordIndex = CoreConstants.ZeroBasedListStartIndex; recordIndex < state.ShowCastWindows.Count; recordIndex++)
                {
                    FlatFileShowCastWindowRecord record = state.ShowCastWindows[recordIndex];
                    if (record == null)
                    {
                        continue;
                    }

                    bool showMatches = string.Equals(record.ShowId, mutation.ShowId, StringComparison.Ordinal);
                    bool idolMatches = record.IdolId == mutation.IdolId;
                    bool isOpen = string.IsNullOrEmpty(record.EndDate);
                    if (!showMatches || !idolMatches || !isOpen)
                    {
                        continue;
                    }

                    record.EndDate = mutation.EndDate ?? string.Empty;
                    record.EndReason = mutation.EndReason ?? string.Empty;
                    record.PayloadJson = mutation.PayloadJson ?? CoreConstants.EmptyJsonObject;
                }
            }
        }

        /// <summary>
        /// Applies open/close mutations for contract windows.
        /// </summary>
        private void ApplyContractWindowMutationsLocked(IReadOnlyList<ContractWindowProjectionMutation> mutations)
        {
            if (mutations == null || mutations.Count == CoreConstants.ZeroBasedListStartIndex)
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
                    int existingOpenIndex;
                    if (!TryFindOpenContractWindowIndexLocked(mutation.ContractKey, mutation.IdolId, out existingOpenIndex))
                    {
                        state.ContractWindows.Add(
                            new FlatFileContractWindowRecord
                            {
                                ContractKey = mutation.ContractKey ?? string.Empty,
                                IdolId = mutation.IdolId,
                                StartDate = mutation.StartDate ?? string.Empty,
                                EndDate = string.Empty,
                                EndReason = string.Empty,
                                PayloadJson = mutation.PayloadJson ?? CoreConstants.EmptyJsonObject
                            });
                    }

                    continue;
                }

                for (int recordIndex = CoreConstants.ZeroBasedListStartIndex; recordIndex < state.ContractWindows.Count; recordIndex++)
                {
                    FlatFileContractWindowRecord record = state.ContractWindows[recordIndex];
                    if (record == null)
                    {
                        continue;
                    }

                    bool contractMatches = string.Equals(record.ContractKey, mutation.ContractKey, StringComparison.Ordinal);
                    bool idolMatches = record.IdolId == mutation.IdolId;
                    bool isOpen = string.IsNullOrEmpty(record.EndDate);
                    if (!contractMatches || !idolMatches || !isOpen)
                    {
                        continue;
                    }

                    record.EndDate = mutation.EndDate ?? string.Empty;
                    record.EndReason = mutation.EndReason ?? string.Empty;
                    record.PayloadJson = mutation.PayloadJson ?? CoreConstants.EmptyJsonObject;
                }
            }
        }

        /// <summary>
        /// Applies open/close mutations for relationship windows.
        /// </summary>
        private void ApplyRelationshipWindowMutationsLocked(IReadOnlyList<RelationshipWindowProjectionMutation> mutations)
        {
            if (mutations == null || mutations.Count == CoreConstants.ZeroBasedListStartIndex)
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
                    int existingOpenIndex;
                    if (!TryFindOpenRelationshipWindowIndexLocked(
                        mutation.RelationshipKey,
                        mutation.IdolId,
                        mutation.RelationshipType,
                        out existingOpenIndex))
                    {
                        state.RelationshipWindows.Add(
                            new FlatFileRelationshipWindowRecord
                            {
                                RelationshipKey = mutation.RelationshipKey ?? string.Empty,
                                IdolId = mutation.IdolId,
                                RelationshipType = mutation.RelationshipType ?? string.Empty,
                                StartDate = mutation.StartDate ?? string.Empty,
                                EndDate = string.Empty,
                                EndReason = string.Empty,
                                PayloadJson = mutation.PayloadJson ?? CoreConstants.EmptyJsonObject
                            });
                    }

                    continue;
                }

                for (int recordIndex = CoreConstants.ZeroBasedListStartIndex; recordIndex < state.RelationshipWindows.Count; recordIndex++)
                {
                    FlatFileRelationshipWindowRecord record = state.RelationshipWindows[recordIndex];
                    if (record == null)
                    {
                        continue;
                    }

                    bool relationshipKeyMatches = string.Equals(record.RelationshipKey, mutation.RelationshipKey, StringComparison.Ordinal);
                    bool idolMatches = record.IdolId == mutation.IdolId;
                    bool relationshipTypeMatches = string.Equals(record.RelationshipType, mutation.RelationshipType, StringComparison.Ordinal);
                    bool isOpen = string.IsNullOrEmpty(record.EndDate);
                    if (!relationshipKeyMatches || !idolMatches || !relationshipTypeMatches || !isOpen)
                    {
                        continue;
                    }

                    record.EndDate = mutation.EndDate ?? string.Empty;
                    record.EndReason = mutation.EndReason ?? string.Empty;
                    record.PayloadJson = mutation.PayloadJson ?? CoreConstants.EmptyJsonObject;
                }
            }
        }

        /// <summary>
        /// Applies upsert behavior for tour-participation projection rows.
        /// </summary>
        private void UpsertTourParticipationRowsLocked(IReadOnlyList<TourParticipationProjectionRow> rows)
        {
            if (rows == null || rows.Count == CoreConstants.ZeroBasedListStartIndex)
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

                int existingIndex = FindTourParticipationIndexLocked(row.TourId, row.IdolId, row.LifecycleAction);
                FlatFileTourParticipationRecord mappedRecord = new FlatFileTourParticipationRecord
                {
                    TourId = row.TourId ?? string.Empty,
                    IdolId = row.IdolId,
                    LifecycleAction = row.LifecycleAction ?? string.Empty,
                    EventDate = row.EventDate ?? string.Empty,
                    PayloadJson = row.PayloadJson ?? CoreConstants.EmptyJsonObject
                };

                if (existingIndex >= CoreConstants.ZeroBasedListStartIndex)
                {
                    state.TourParticipation[existingIndex] = mappedRecord;
                }
                else
                {
                    state.TourParticipation.Add(mappedRecord);
                }
            }
        }

        /// <summary>
        /// Applies upsert behavior for award-result projection rows.
        /// </summary>
        private void UpsertAwardResultRowsLocked(IReadOnlyList<AwardResultProjectionRow> rows)
        {
            if (rows == null || rows.Count == CoreConstants.ZeroBasedListStartIndex)
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

                int existingIndex = FindAwardResultProjectionIndexLocked(row.AwardKey, row.IdolId);
                FlatFileAwardResultProjectionRecord mappedRecord = new FlatFileAwardResultProjectionRecord
                {
                    AwardKey = row.AwardKey ?? string.Empty,
                    IdolId = row.IdolId,
                    EventDate = row.EventDate ?? string.Empty,
                    PayloadJson = row.PayloadJson ?? CoreConstants.EmptyJsonObject
                };

                if (existingIndex >= CoreConstants.ZeroBasedListStartIndex)
                {
                    state.AwardResults[existingIndex] = mappedRecord;
                }
                else
                {
                    state.AwardResults.Add(mappedRecord);
                }
            }
        }

        /// <summary>
        /// Applies upsert behavior for election-result projection rows.
        /// </summary>
        private void UpsertElectionResultRowsLocked(IReadOnlyList<ElectionResultProjectionRow> rows)
        {
            if (rows == null || rows.Count == CoreConstants.ZeroBasedListStartIndex)
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

                int existingIndex = FindElectionResultProjectionIndexLocked(row.ElectionId, row.IdolId);
                FlatFileElectionResultProjectionRecord mappedRecord = new FlatFileElectionResultProjectionRecord
                {
                    ElectionId = row.ElectionId ?? string.Empty,
                    IdolId = row.IdolId,
                    EventDate = row.EventDate ?? string.Empty,
                    PayloadJson = row.PayloadJson ?? CoreConstants.EmptyJsonObject
                };

                if (existingIndex >= CoreConstants.ZeroBasedListStartIndex)
                {
                    state.ElectionResults[existingIndex] = mappedRecord;
                }
                else
                {
                    state.ElectionResults.Add(mappedRecord);
                }
            }
        }

        /// <summary>
        /// Applies open/close/touch mutations for push windows.
        /// </summary>
        private void ApplyPushWindowMutationsLocked(IReadOnlyList<PushWindowProjectionMutation> mutations)
        {
            if (mutations == null || mutations.Count == CoreConstants.ZeroBasedListStartIndex)
            {
                return;
            }

            for (int i = CoreConstants.ZeroBasedListStartIndex; i < mutations.Count; i++)
            {
                PushWindowProjectionMutation mutation = mutations[i];
                if (mutation == null || mutation.IdolId < CoreConstants.MinimumValidIdolIdentifier || string.IsNullOrEmpty(mutation.SlotKey))
                {
                    continue;
                }

                if (mutation.OpenWindow)
                {
                    int existingOpenIndex;
                    if (!TryFindOpenPushWindowIndexLocked(mutation.SlotKey, mutation.IdolId, out existingOpenIndex))
                    {
                        state.PushWindows.Add(
                            new FlatFilePushWindowRecord
                            {
                                SlotKey = mutation.SlotKey ?? string.Empty,
                                IdolId = mutation.IdolId,
                                StartDate = mutation.StartDate ?? string.Empty,
                                EndDate = string.Empty,
                                LastDaysInSlot = mutation.PushDaysInSlot,
                                EndReason = string.Empty,
                                PayloadJson = mutation.PayloadJson ?? CoreConstants.EmptyJsonObject
                            });
                    }

                    continue;
                }

                if (mutation.CloseWindow)
                {
                    for (int recordIndex = CoreConstants.ZeroBasedListStartIndex; recordIndex < state.PushWindows.Count; recordIndex++)
                    {
                        FlatFilePushWindowRecord record = state.PushWindows[recordIndex];
                        if (record == null)
                        {
                            continue;
                        }

                        bool slotMatches = string.Equals(record.SlotKey, mutation.SlotKey, StringComparison.Ordinal);
                        bool idolMatches = record.IdolId == mutation.IdolId;
                        bool isOpen = string.IsNullOrEmpty(record.EndDate);
                        if (!slotMatches || !idolMatches || !isOpen)
                        {
                            continue;
                        }

                        record.EndDate = mutation.EndDate ?? string.Empty;
                        record.LastDaysInSlot = mutation.PushDaysInSlot;
                        record.EndReason = mutation.EndReason ?? string.Empty;
                        record.PayloadJson = mutation.PayloadJson ?? CoreConstants.EmptyJsonObject;
                    }

                    continue;
                }

                for (int recordIndex = CoreConstants.ZeroBasedListStartIndex; recordIndex < state.PushWindows.Count; recordIndex++)
                {
                    FlatFilePushWindowRecord record = state.PushWindows[recordIndex];
                    if (record == null)
                    {
                        continue;
                    }

                    bool slotMatches = string.Equals(record.SlotKey, mutation.SlotKey, StringComparison.Ordinal);
                    bool idolMatches = record.IdolId == mutation.IdolId;
                    bool isOpen = string.IsNullOrEmpty(record.EndDate);
                    if (!slotMatches || !idolMatches || !isOpen)
                    {
                        continue;
                    }

                    record.LastDaysInSlot = mutation.PushDaysInSlot;
                    record.PayloadJson = mutation.PayloadJson ?? CoreConstants.EmptyJsonObject;
                }
            }
        }

        /// <summary>
        /// Finds one open show-cast window by `(show_id, idol_id)`.
        /// </summary>
        private bool TryFindOpenShowCastWindowIndexLocked(string showId, int idolId, out int existingOpenIndex)
        {
            for (int i = CoreConstants.ZeroBasedListStartIndex; i < state.ShowCastWindows.Count; i++)
            {
                FlatFileShowCastWindowRecord record = state.ShowCastWindows[i];
                if (record == null)
                {
                    continue;
                }

                bool showMatches = string.Equals(record.ShowId, showId ?? string.Empty, StringComparison.Ordinal);
                bool idolMatches = record.IdolId == idolId;
                bool isOpen = string.IsNullOrEmpty(record.EndDate);
                if (showMatches && idolMatches && isOpen)
                {
                    existingOpenIndex = i;
                    return true;
                }
            }

            existingOpenIndex = CoreConstants.InvalidIdValue;
            return false;
        }

        /// <summary>
        /// Finds one open contract window by `(contract_key, idol_id)`.
        /// </summary>
        private bool TryFindOpenContractWindowIndexLocked(string contractKey, int idolId, out int existingOpenIndex)
        {
            for (int i = CoreConstants.ZeroBasedListStartIndex; i < state.ContractWindows.Count; i++)
            {
                FlatFileContractWindowRecord record = state.ContractWindows[i];
                if (record == null)
                {
                    continue;
                }

                bool contractMatches = string.Equals(record.ContractKey, contractKey ?? string.Empty, StringComparison.Ordinal);
                bool idolMatches = record.IdolId == idolId;
                bool isOpen = string.IsNullOrEmpty(record.EndDate);
                if (contractMatches && idolMatches && isOpen)
                {
                    existingOpenIndex = i;
                    return true;
                }
            }

            existingOpenIndex = CoreConstants.InvalidIdValue;
            return false;
        }

        /// <summary>
        /// Finds one open relationship window by `(relationship_key, idol_id, relationship_type)`.
        /// </summary>
        private bool TryFindOpenRelationshipWindowIndexLocked(
            string relationshipKey,
            int idolId,
            string relationshipType,
            out int existingOpenIndex)
        {
            for (int i = CoreConstants.ZeroBasedListStartIndex; i < state.RelationshipWindows.Count; i++)
            {
                FlatFileRelationshipWindowRecord record = state.RelationshipWindows[i];
                if (record == null)
                {
                    continue;
                }

                bool relationshipKeyMatches = string.Equals(record.RelationshipKey, relationshipKey ?? string.Empty, StringComparison.Ordinal);
                bool idolMatches = record.IdolId == idolId;
                bool relationshipTypeMatches = string.Equals(record.RelationshipType, relationshipType ?? string.Empty, StringComparison.Ordinal);
                bool isOpen = string.IsNullOrEmpty(record.EndDate);
                if (relationshipKeyMatches && idolMatches && relationshipTypeMatches && isOpen)
                {
                    existingOpenIndex = i;
                    return true;
                }
            }

            existingOpenIndex = CoreConstants.InvalidIdValue;
            return false;
        }

        /// <summary>
        /// Finds one open push window by `(slot_key, idol_id)`.
        /// </summary>
        private bool TryFindOpenPushWindowIndexLocked(string slotKey, int idolId, out int existingOpenIndex)
        {
            for (int i = CoreConstants.ZeroBasedListStartIndex; i < state.PushWindows.Count; i++)
            {
                FlatFilePushWindowRecord record = state.PushWindows[i];
                if (record == null)
                {
                    continue;
                }

                bool slotMatches = string.Equals(record.SlotKey, slotKey ?? string.Empty, StringComparison.Ordinal);
                bool idolMatches = record.IdolId == idolId;
                bool isOpen = string.IsNullOrEmpty(record.EndDate);
                if (slotMatches && idolMatches && isOpen)
                {
                    existingOpenIndex = i;
                    return true;
                }
            }

            existingOpenIndex = CoreConstants.InvalidIdValue;
            return false;
        }

        /// <summary>
        /// Finds one tour participation projection index by `(tour_id, idol_id, lifecycle_action)`.
        /// </summary>
        private int FindTourParticipationIndexLocked(string tourId, int idolId, string lifecycleAction)
        {
            for (int i = CoreConstants.ZeroBasedListStartIndex; i < state.TourParticipation.Count; i++)
            {
                FlatFileTourParticipationRecord record = state.TourParticipation[i];
                if (record == null)
                {
                    continue;
                }

                bool tourMatches = string.Equals(record.TourId, tourId ?? string.Empty, StringComparison.Ordinal);
                bool idolMatches = record.IdolId == idolId;
                bool actionMatches = string.Equals(record.LifecycleAction, lifecycleAction ?? string.Empty, StringComparison.Ordinal);
                if (tourMatches && idolMatches && actionMatches)
                {
                    return i;
                }
            }

            return CoreConstants.InvalidIdValue;
        }

        /// <summary>
        /// Finds one award-result projection index by `(award_key, idol_id)`.
        /// </summary>
        private int FindAwardResultProjectionIndexLocked(string awardKey, int idolId)
        {
            for (int i = CoreConstants.ZeroBasedListStartIndex; i < state.AwardResults.Count; i++)
            {
                FlatFileAwardResultProjectionRecord record = state.AwardResults[i];
                if (record == null)
                {
                    continue;
                }

                bool awardMatches = string.Equals(record.AwardKey, awardKey ?? string.Empty, StringComparison.Ordinal);
                bool idolMatches = record.IdolId == idolId;
                if (awardMatches && idolMatches)
                {
                    return i;
                }
            }

            return CoreConstants.InvalidIdValue;
        }

        /// <summary>
        /// Finds one election-result projection index by `(election_id, idol_id)`.
        /// </summary>
        private int FindElectionResultProjectionIndexLocked(string electionId, int idolId)
        {
            for (int i = CoreConstants.ZeroBasedListStartIndex; i < state.ElectionResults.Count; i++)
            {
                FlatFileElectionResultProjectionRecord record = state.ElectionResults[i];
                if (record == null)
                {
                    continue;
                }

                bool electionMatches = string.Equals(record.ElectionId, electionId ?? string.Empty, StringComparison.Ordinal);
                bool idolMatches = record.IdolId == idolId;
                if (electionMatches && idolMatches)
                {
                    return i;
                }
            }

            return CoreConstants.InvalidIdValue;
        }

        /// <summary>
        /// Finds one custom-data index by namespace and key.
        /// </summary>
        private int FindCustomDataIndexLocked(string namespaceIdentifier, string dataKey)
        {
            string normalizedNamespace = namespaceIdentifier ?? string.Empty;
            string normalizedDataKey = dataKey ?? string.Empty;
            for (int i = CoreConstants.ZeroBasedListStartIndex; i < state.CustomData.Count; i++)
            {
                FlatFileCustomDataRecord record = state.CustomData[i];
                if (record == null)
                {
                    continue;
                }

                bool namespaceMatches = string.Equals(record.NamespaceIdentifier, normalizedNamespace, StringComparison.Ordinal);
                bool dataKeyMatches = string.Equals(record.DataKey, normalizedDataKey, StringComparison.Ordinal);
                if (namespaceMatches && dataKeyMatches)
                {
                    return i;
                }
            }

            return CoreConstants.InvalidIdValue;
        }

        /// <summary>
        /// Returns key count for one namespace.
        /// </summary>
        private int GetNamespaceKeyCountLocked(string namespaceIdentifier)
        {
            int keyCount = CoreConstants.ZeroBasedListStartIndex;
            string normalizedNamespace = namespaceIdentifier ?? string.Empty;
            for (int i = CoreConstants.ZeroBasedListStartIndex; i < state.CustomData.Count; i++)
            {
                FlatFileCustomDataRecord record = state.CustomData[i];
                if (record != null && string.Equals(record.NamespaceIdentifier, normalizedNamespace, StringComparison.Ordinal))
                {
                    keyCount++;
                }
            }

            return keyCount;
        }

        /// <summary>
        /// Returns total value length for one namespace.
        /// </summary>
        private int GetNamespaceTotalLengthLocked(string namespaceIdentifier)
        {
            int totalLength = CoreConstants.ZeroBasedListStartIndex;
            string normalizedNamespace = namespaceIdentifier ?? string.Empty;
            for (int i = CoreConstants.ZeroBasedListStartIndex; i < state.CustomData.Count; i++)
            {
                FlatFileCustomDataRecord record = state.CustomData[i];
                if (record != null && string.Equals(record.NamespaceIdentifier, normalizedNamespace, StringComparison.Ordinal))
                {
                    totalLength += GetSafeLength(record.ValueJson);
                }
            }

            return totalLength;
        }

        /// <summary>
        /// Null-safe string length.
        /// </summary>
        private static int GetSafeLength(string value)
        {
            return value == null ? CoreConstants.ZeroBasedListStartIndex : value.Length;
        }

        /// <summary>
        /// Sorts events by game date descending, then event id descending.
        /// </summary>
        private static int CompareEventsDescending(FlatFileEventRecord left, FlatFileEventRecord right)
        {
            if (ReferenceEquals(left, right))
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            if (left == null)
            {
                return CoreConstants.MinimumQueueSizeForFlush;
            }

            if (right == null)
            {
                return -CoreConstants.MinimumQueueSizeForFlush;
            }

            int dateComparison = right.GameDateKey.CompareTo(left.GameDateKey);
            if (dateComparison != CoreConstants.ZeroBasedListStartIndex)
            {
                return dateComparison;
            }

            return right.EventId.CompareTo(left.EventId);
        }

        private static int CompareEventsAscending(FlatFileEventRecord left, FlatFileEventRecord right)
        {
            if (ReferenceEquals(left, right))
            {
                return CoreConstants.ZeroBasedListStartIndex;
            }

            if (left == null)
            {
                return CoreConstants.MinimumQueueSizeForFlush;
            }

            if (right == null)
            {
                return -CoreConstants.MinimumQueueSizeForFlush;
            }

            int dateComparison = left.GameDateKey.CompareTo(right.GameDateKey);
            return dateComparison != CoreConstants.ZeroBasedListStartIndex
                ? dateComparison
                : left.EventId.CompareTo(right.EventId);
        }
    }

}
