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
                try
                {
                    EnsureStateInitializedLocked();

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

                    return SaveStateLocked(out errorMessage);
                }
                catch (Exception exception)
                {
                    errorMessage = CoreConstants.MessagePersistBatchFailedPrefix + exception.Message;
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
                try
                {
                    EnsureStateInitializedLocked();

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

                    return SaveStateLocked(out errorMessage);
                }
                catch (Exception exception)
                {
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
                try
                {
                    EnsureStateInitializedLocked();
                    bool removedAny = false;

                    for (int i = state.CustomData.Count - CoreConstants.LastElementOffsetFromCount; i >= CoreConstants.ZeroBasedListStartIndex; i--)
                    {
                        FlatFileCustomDataRecord record = state.CustomData[i];
                        if (record == null)
                        {
                            continue;
                        }

                        bool namespaceMatches = string.Equals(record.NamespaceIdentifier, namespaceIdentifier ?? string.Empty, StringComparison.Ordinal);
                        bool keyMatches = string.Equals(record.DataKey, dataKey ?? string.Empty, StringComparison.Ordinal);
                        if (namespaceMatches && keyMatches)
                        {
                            state.CustomData.RemoveAt(i);
                            removedAny = true;
                        }
                    }

                    if (!removedAny)
                    {
                        return true;
                    }

                    return SaveStateLocked(out errorMessage);
                }
                catch (Exception exception)
                {
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

                        bool idolMatches = eventRecord.IdolId == idolId || eventRecord.IdolId < CoreConstants.MinimumValidIdolIdentifier;
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

        /// <summary>
        /// Removes rows newer than one loaded save snapshot date.
        /// </summary>
        public bool TryRollbackToGameDateTime(string saveKey, DateTime cutoffGameDateTime, out string errorMessage)
        {
            errorMessage = string.Empty;

            lock (storageLock)
            {
                try
                {
                    EnsureStateInitializedLocked();

                    string cutoffDateTime = CoreDateTimeUtility.ToRoundTripString(cutoffGameDateTime);

                    state.Events.RemoveAll(
                        record => record != null
                            && IsDateAfterCutoff(record.GameDateTime, cutoffDateTime, cutoffGameDateTime));
                    state.SingleParticipation.RemoveAll(
                        record => record != null
                            && IsDateAfterCutoff(record.ReleaseDate, cutoffDateTime, cutoffGameDateTime));
                    state.StatusWindows.RemoveAll(
                        record => record != null
                            && (IsDateAfterCutoff(record.StartDate, cutoffDateTime, cutoffGameDateTime)
                                || IsDateAfterCutoff(record.EndDate, cutoffDateTime, cutoffGameDateTime)));
                    state.ShowCastWindows.RemoveAll(
                        record => record != null
                            && (IsDateAfterCutoff(record.StartDate, cutoffDateTime, cutoffGameDateTime)
                                || IsDateAfterCutoff(record.EndDate, cutoffDateTime, cutoffGameDateTime)));
                    state.ContractWindows.RemoveAll(
                        record => record != null
                            && (IsDateAfterCutoff(record.StartDate, cutoffDateTime, cutoffGameDateTime)
                                || IsDateAfterCutoff(record.EndDate, cutoffDateTime, cutoffGameDateTime)));
                    state.RelationshipWindows.RemoveAll(
                        record => record != null
                            && (IsDateAfterCutoff(record.StartDate, cutoffDateTime, cutoffGameDateTime)
                                || IsDateAfterCutoff(record.EndDate, cutoffDateTime, cutoffGameDateTime)));
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
                            && (IsDateAfterCutoff(record.StartDate, cutoffDateTime, cutoffGameDateTime)
                                || IsDateAfterCutoff(record.EndDate, cutoffDateTime, cutoffGameDateTime)));

                    return SaveStateLocked(out errorMessage);
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
            if (!File.Exists(storagePath))
            {
                state = new FlatFileState();
                return;
            }

            string rawJson = File.ReadAllText(storagePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                state = new FlatFileState();
                return;
            }

            FlatFileState deserializedState = JsonUtility.FromJson<FlatFileState>(rawJson);
            state = deserializedState ?? new FlatFileState();
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
                string serializedJson = JsonUtility.ToJson(state, CoreConstants.PrettyPrintJsonPayload);
                File.WriteAllText(storagePath, serializedJson, new UTF8Encoding(false));
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
    }

}
