using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace IMDataCore
{
    /// <summary>
    /// Immutable scalar identity copied from one vanilla SavedData instance. The
    /// relative path selects the physical sidecar; the remaining values select a
    /// lightweight sequence checkpoint inside the logical history.
    /// </summary>
    internal sealed class VanillaSaveStamp
    {
        internal string RelativeSavePath = string.Empty;
        internal string LastSave = string.Empty;
        internal long PlaytimeSeconds;
        internal string GameDateTime = string.Empty;

        internal static bool TryCreate(
            SaveManager.SavedData savedData,
            string relativeSavePath,
            out VanillaSaveStamp stamp,
            out string errorMessage)
        {
            stamp = null;
            errorMessage = string.Empty;
            if (savedData == null || savedData.staticVars__PlayerData == null)
            {
                errorMessage = "Vanilla SavedData does not contain PlayerData.";
                return false;
            }

            string normalizedRelativePath = NormalizeRelativePath(relativeSavePath);
            if (string.IsNullOrEmpty(normalizedRelativePath))
            {
                errorMessage = "The vanilla save relative path is empty.";
                return false;
            }

            stamp = new VanillaSaveStamp
            {
                RelativeSavePath = normalizedRelativePath,
                LastSave = savedData.staticVars__PlayerData.LastSave ?? string.Empty,
                PlaytimeSeconds = savedData.staticVars__PlayerData.Playtime_Seconds,
                GameDateTime = savedData.staticVars__dateTime ?? string.Empty
            };
            return true;
        }

        internal bool Matches(LightweightCheckpointRecord checkpoint)
        {
            return checkpoint != null &&
                string.Equals(
                    NormalizeRelativePath(checkpoint.RelativeSavePath),
                    NormalizeRelativePath(RelativeSavePath),
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(checkpoint.LastSave, LastSave, StringComparison.Ordinal) &&
                checkpoint.PlaytimeSeconds == PlaytimeSeconds &&
                string.Equals(checkpoint.GameDateTime, GameDateTime, StringComparison.Ordinal);
        }

        internal static string NormalizeRelativePath(string value)
        {
            return (value ?? string.Empty)
                .Trim()
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');
        }
    }

    /// <summary>
    /// Versioned JSON envelope for the 2.0 lightweight sidecar. Only source
    /// history is serialized; all dictionaries and read indexes are rebuilt.
    /// </summary>
    [Serializable]
    internal sealed class LightweightSidecarDocument
    {
        public string FormatName = LightweightCoreStorageEngine.SidecarFormatName;
        public int FormatVersion = LightweightCoreStorageEngine.SidecarFormatVersion;
        public string RelativeSavePath = string.Empty;
        public long LastIssuedSequence;
        public List<LightweightCheckpointRecord> Checkpoints =
            new List<LightweightCheckpointRecord>();
        public List<LightweightEventRecord> Events =
            new List<LightweightEventRecord>();
        public List<LightweightCustomMutationRecord> CustomMutations =
            new List<LightweightCustomMutationRecord>();
    }

    [Serializable]
    internal sealed class LightweightCheckpointRecord
    {
        public string RelativeSavePath = string.Empty;
        public string LastSave = string.Empty;
        public long PlaytimeSeconds;
        public string GameDateTime = string.Empty;
        public long Sequence;
    }

    [Serializable]
    internal sealed class LightweightEventRecord
    {
        public long Sequence;
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
    internal sealed class LightweightCustomMutationRecord
    {
        public long Sequence;
        public int GameDateKey;
        public string GameDateTime = string.Empty;
        public string NamespaceIdentifier = string.Empty;
        public string DataKey = string.Empty;
        public string Operation = LightweightCoreStorageEngine.CustomOperationSet;
        public string ValueJson = string.Empty;
    }

    /// <summary>
    /// The sole normal-runtime persistence implementation for IM Data Core 2.0.
    /// Mutations update memory only; callers explicitly persist at vanilla save
    /// boundaries or through TryFlushNow.
    /// </summary>
    internal sealed class LightweightCoreStorageEngine : IDisposable
    {
        internal const string SidecarFormatName = "IMDataCore.LightweightSidecar";
        internal const int SidecarFormatVersion = 2;
        internal const string CustomOperationSet = "SET";
        internal const string CustomOperationRemove = "REMOVE";

        private sealed class MaterializedCustomValue
        {
            internal string NamespaceIdentifier = string.Empty;
            internal string DataKey = string.Empty;
            internal string ValueJson = string.Empty;
        }

        private readonly object storageLock = new object();
        private readonly Dictionary<string, MaterializedCustomValue> customValues =
            new Dictionary<string, MaterializedCustomValue>(StringComparer.Ordinal);
        private readonly HashSet<long> activeMutationSequences = new HashSet<long>();
        private readonly HashSet<long> activeEventIdentifiers = new HashSet<long>();

        // Derived read indexes. They are rebuilt from activeEvents and are never
        // serialized, so they add no sidecar duplication.
        private readonly Dictionary<int, List<LightweightEventRecord>>
            timelineEventsByIdolId =
                new Dictionary<int, List<LightweightEventRecord>>();
        private readonly List<LightweightEventRecord> globalTimelineEvents =
            new List<LightweightEventRecord>();
        private readonly SortedDictionary<int, List<LightweightEventRecord>>
            moneyTransactionsByDateKey =
                new SortedDictionary<int, List<LightweightEventRecord>>();
        private LightweightEventRecord moneyLedgerCoverageStartEvent;

        private List<LightweightEventRecord> durableEvents =
            new List<LightweightEventRecord>();
        private List<LightweightCustomMutationRecord> durableCustomMutations =
            new List<LightweightCustomMutationRecord>();
        private List<LightweightCheckpointRecord> durableCheckpoints =
            new List<LightweightCheckpointRecord>();

        private List<LightweightEventRecord> activeEvents =
            new List<LightweightEventRecord>();
        private List<LightweightCustomMutationRecord> activeCustomMutations =
            new List<LightweightCustomMutationRecord>();
        private List<LightweightCheckpointRecord> activeCheckpoints =
            new List<LightweightCheckpointRecord>();

        private string currentSidecarPath = string.Empty;
        private string currentRelativeSavePath = string.Empty;
        private long lastIssuedSequence;
        private bool disposed;

        internal bool HasPhysicalScope
        {
            get
            {
                lock (storageLock)
                {
                    return !string.IsNullOrEmpty(currentSidecarPath);
                }
            }
        }

        internal string CurrentSidecarPath
        {
            get
            {
                lock (storageLock)
                {
                    return currentSidecarPath;
                }
            }
        }

        internal string CurrentRelativeSavePath
        {
            get
            {
                lock (storageLock)
                {
                    return currentRelativeSavePath;
                }
            }
        }

        internal long LastIssuedSequence
        {
            get
            {
                lock (storageLock)
                {
                    return lastIssuedSequence;
                }
            }
        }

        internal void InitializeTransient()
        {
            lock (storageLock)
            {
                ThrowIfDisposed();
                ResetStateLocked();
            }
        }

        internal bool Initialize(
            CoreSaveScope saveScope,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (saveScope == null || saveScope.IsTransient)
            {
                errorMessage = "A physical vanilla save scope is required.";
                return false;
            }

            lock (storageLock)
            {
                try
                {
                    ThrowIfDisposed();
                    ResetStateLocked();
                    currentSidecarPath = saveScope.SidecarFilePath ?? string.Empty;
                    currentRelativeSavePath = VanillaSaveStamp.NormalizeRelativePath(
                        saveScope.RelativeSavePath);

                    string validationError;
                    string normalizedSidecarPath;
                    if (!CorePaths.TryValidateContainedMutationPath(
                        currentSidecarPath,
                        false,
                        out normalizedSidecarPath,
                        out validationError))
                    {
                        errorMessage = validationError;
                        ResetStateLocked();
                        return false;
                    }

                    if (!File.Exists(currentSidecarPath))
                    {
                        return true;
                    }

                    string rawJson = File.ReadAllText(currentSidecarPath);
                    LightweightSidecarDocument document =
                        LightweightSidecarJson.Deserialize(rawJson);
                    if (!TryValidateDocumentLocked(document, out errorMessage))
                    {
                        ResetStateLocked();
                        return false;
                    }

                    LoadDocumentLocked(document);
                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = "The lightweight sidecar could not be loaded: " +
                        exception.Message;
                    ResetStateLocked();
                    return false;
                }
            }
        }

        /// <summary>
        /// Starts an empty branch for a valid scope after a missing, corrupt, or
        /// unsupported sidecar. Vanilla loading must continue in all such cases.
        /// </summary>
        internal bool InitializeEmpty(
            CoreSaveScope saveScope,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (saveScope == null || saveScope.IsTransient)
            {
                errorMessage = "A physical vanilla save scope is required.";
                return false;
            }

            lock (storageLock)
            {
                try
                {
                    ThrowIfDisposed();
                    ResetStateLocked();
                    currentSidecarPath = saveScope.SidecarFilePath ?? string.Empty;
                    currentRelativeSavePath = VanillaSaveStamp.NormalizeRelativePath(
                        saveScope.RelativeSavePath);
                    string normalizedSidecarPath;
                    return CorePaths.TryValidateContainedMutationPath(
                        currentSidecarPath,
                        false,
                        out normalizedSidecarPath,
                        out errorMessage);
                }
                catch (Exception exception)
                {
                    errorMessage = exception.Message;
                    ResetStateLocked();
                    return false;
                }
            }
        }

        internal void SetLastIssuedSequence(long sequence)
        {
            lock (storageLock)
            {
                ThrowIfDisposed();
                if (sequence > lastIssuedSequence)
                {
                    lastIssuedSequence = sequence;
                }
            }
        }

        internal bool AppendEvents(
            IReadOnlyList<PendingEvent> pendingEvents,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (pendingEvents == null || pendingEvents.Count == 0)
            {
                return true;
            }

            lock (storageLock)
            {
                try
                {
                    ThrowIfDisposed();

                    long highestIncomingSequence = lastIssuedSequence;
                    List<PendingEvent> retained =
                        new List<PendingEvent>(pendingEvents.Count);
                    for (int index = 0; index < pendingEvents.Count; index++)
                    {
                        PendingEvent pending = pendingEvents[index];
                        if (pending == null)
                        {
                            continue;
                        }

                        if (pending.CaptureSequence > highestIncomingSequence)
                        {
                            highestIncomingSequence = pending.CaptureSequence;
                        }

                        if (CoreEventRetention.ShouldPersist(pending))
                        {
                            retained.Add(pending);
                        }
                    }

                    // Sequence watermarks describe issued mutations, not the count
                    // of physical rows. Compaction intentionally leaves gaps.
                    if (highestIncomingSequence > lastIssuedSequence)
                    {
                        lastIssuedSequence = highestIncomingSequence;
                    }

                    if (retained.Count == 0)
                    {
                        return true;
                    }

                    int sparseMoneyPayloadCount;
                    int sharedParticipantRowsRemoved;
                    List<PendingEvent> compacted =
                        CorePayloadCompaction.CompactPendingEvents(
                            retained,
                            out sparseMoneyPayloadCount,
                            out sharedParticipantRowsRemoved);

                    HashSet<long> batchSequences = new HashSet<long>();
                    HashSet<long> batchEventIdentifiers = new HashSet<long>();
                    for (int index = 0; index < compacted.Count; index++)
                    {
                        PendingEvent pending = compacted[index];
                        long eventIdentifier = pending.CaptureSequence;
                        if (pending.CaptureSequence <= 0L ||
                            activeMutationSequences.Contains(pending.CaptureSequence) ||
                            !batchSequences.Add(pending.CaptureSequence) ||
                            activeEventIdentifiers.Contains(eventIdentifier) ||
                            !batchEventIdentifiers.Add(eventIdentifier))
                        {
                            errorMessage =
                                "An event has an invalid or duplicate sequence/identifier.";
                            return false;
                        }
                    }

                    for (int index = 0; index < compacted.Count; index++)
                    {
                        PendingEvent pending = compacted[index];
                        LightweightEventRecord record = new LightweightEventRecord
                        {
                            Sequence = pending.CaptureSequence,
                            EventId = pending.CaptureSequence,
                            GameDateKey = pending.GameDateKey,
                            GameDateTime = pending.GameDateTime ?? string.Empty,
                            IdolId = pending.IdolId,
                            EntityKind = pending.EntityKind ?? string.Empty,
                            EntityId = pending.EntityId ?? string.Empty,
                            EventType = pending.EventType ?? string.Empty,
                            SourcePatch = pending.SourcePatch ?? string.Empty,
                            NamespaceIdentifier =
                                pending.NamespaceIdentifier ?? string.Empty,
                            PayloadJson =
                                pending.PayloadJson ?? CoreConstants.EmptyJsonObject
                        };
                        activeEvents.Add(record);
                        activeMutationSequences.Add(record.Sequence);
                        activeEventIdentifiers.Add(record.EventId);
                        IndexEventLocked(record);
                    }

                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = "Appending in-memory IMDC events failed: " +
                        exception.Message;
                    return false;
                }
            }
        }        internal bool TryValidateCustomDataMutation(
            string namespaceIdentifier,
            string dataKey,
            string jsonValue,
            bool remove,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            lock (storageLock)
            {
                try
                {
                    ThrowIfDisposed();
                    if (remove)
                    {
                        return true;
                    }

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

                    string compositeKey = BuildCustomDataCompositeKey(
                        namespaceIdentifier,
                        dataKey);
                    MaterializedCustomValue existing;
                    bool exists = customValues.TryGetValue(compositeKey, out existing);
                    int existingLength = exists && existing.ValueJson != null
                        ? existing.ValueJson.Length
                        : 0;
                    int namespaceKeyCount = GetNamespaceKeyCountLocked(namespaceIdentifier);
                    if (!exists &&
                        namespaceKeyCount >= CoreConstants.MaximumCustomKeysPerNamespace)
                    {
                        errorMessage = CoreConstants.MessageNamespaceKeyQuotaExceeded;
                        return false;
                    }

                    int projectedLength = GetNamespaceTotalLengthLocked(namespaceIdentifier) -
                        existingLength + jsonValue.Length;
                    if (projectedLength > CoreConstants.MaximumNamespaceCharacterBudget)
                    {
                        errorMessage = CoreConstants.MessageNamespaceDataBudgetExceeded;
                        return false;
                    }

                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = "Custom-data validation failed: " + exception.Message;
                    return false;
                }
            }
        }

        internal bool TrySetCustomData(
            long sequence,
            DateTime gameDate,
            string namespaceIdentifier,
            string dataKey,
            string jsonValue,
            out string errorMessage)
        {
            if (!TryValidateCustomDataMutation(
                namespaceIdentifier,
                dataKey,
                jsonValue,
                false,
                out errorMessage))
            {
                return false;
            }

            lock (storageLock)
            {
                if (!TryReserveMutationSequenceLocked(sequence, out errorMessage))
                {
                    return false;
                }

                LightweightCustomMutationRecord mutation =
                    new LightweightCustomMutationRecord
                    {
                        Sequence = sequence,
                        GameDateKey = CoreDateTimeUtility.BuildGameDateKey(gameDate),
                        GameDateTime = CoreDateTimeUtility.ToRoundTripString(gameDate),
                        NamespaceIdentifier = namespaceIdentifier ?? string.Empty,
                        DataKey = dataKey ?? string.Empty,
                        Operation = CustomOperationSet,
                        ValueJson = jsonValue ?? string.Empty
                    };
                activeCustomMutations.Add(mutation);
                customValues[BuildCustomDataCompositeKey(
                    namespaceIdentifier,
                    dataKey)] = new MaterializedCustomValue
                    {
                        NamespaceIdentifier = namespaceIdentifier ?? string.Empty,
                        DataKey = dataKey ?? string.Empty,
                        ValueJson = jsonValue ?? string.Empty
                    };
                return true;
            }
        }

        internal bool TryGetCustomData(
            string namespaceIdentifier,
            string dataKey,
            out string jsonValue,
            out string errorMessage)
        {
            jsonValue = string.Empty;
            errorMessage = string.Empty;
            lock (storageLock)
            {
                try
                {
                    ThrowIfDisposed();
                    MaterializedCustomValue value;
                    if (!customValues.TryGetValue(
                        BuildCustomDataCompositeKey(namespaceIdentifier, dataKey),
                        out value))
                    {
                        return false;
                    }

                    jsonValue = value.ValueJson ?? string.Empty;
                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = "Reading custom data failed: " + exception.Message;
                    return false;
                }
            }
        }

        internal bool TryRemoveCustomData(
            long sequence,
            DateTime gameDate,
            string namespaceIdentifier,
            string dataKey,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            lock (storageLock)
            {
                try
                {
                    ThrowIfDisposed();
                    string compositeKey = BuildCustomDataCompositeKey(
                        namespaceIdentifier,
                        dataKey);
                    if (!TryReserveMutationSequenceLocked(sequence, out errorMessage))
                    {
                        return false;
                    }

                    activeCustomMutations.Add(
                        new LightweightCustomMutationRecord
                        {
                            Sequence = sequence,
                            GameDateKey = CoreDateTimeUtility.BuildGameDateKey(gameDate),
                            GameDateTime = CoreDateTimeUtility.ToRoundTripString(gameDate),
                            NamespaceIdentifier = namespaceIdentifier ?? string.Empty,
                            DataKey = dataKey ?? string.Empty,
                            Operation = CustomOperationRemove,
                            ValueJson = string.Empty
                        });
                    customValues.Remove(compositeKey);
                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = "Removing custom data failed: " + exception.Message;
                    return false;
                }
            }
        }

        internal bool TryReadRecentEventsForIdol(
            int idolId,
            int maxCount,
            out List<IMDataCoreEvent> events,
            out string errorMessage)
        {
            events = new List<IMDataCoreEvent>();
            errorMessage = string.Empty;
            lock (storageLock)
            {
                try
                {
                    ThrowIfDisposed();
                    if (maxCount <= 0)
                    {
                        return true;
                    }

                    List<LightweightEventRecord> indexedIdolEvents;
                    List<LightweightEventRecord> idolEvents =
                        timelineEventsByIdolId.TryGetValue(
                            idolId,
                            out indexedIdolEvents)
                            ? new List<LightweightEventRecord>(indexedIdolEvents)
                            : new List<LightweightEventRecord>();

                    List<LightweightEventRecord> globalEvents =
                        new List<LightweightEventRecord>(globalTimelineEvents);

                    idolEvents.Sort(CompareEventsDescending);
                    globalEvents.Sort(CompareEventsDescending);

                    AppendPublicEventsForIdol(
                        idolEvents,
                        idolId,
                        maxCount,
                        events);
                    if (events.Count < maxCount)
                    {
                        AppendPublicEvents(globalEvents, maxCount, events);
                    }

                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = CoreConstants.MessageTryReadRecentEventsFailedPrefix +
                        exception.Message;
                    return false;
                }
            }
        }

        /// <summary>
        /// Reads the newest active shared release formation for one single. This
        /// allows later chart snapshots to retain idols whom vanilla already
        /// removed from the live formation after graduation.
        /// </summary>
        internal bool TryGetLatestSingleCastSlotIdolIdentifiers(
            int singleId,
            out List<int> slotIdolIdentifiers)
        {
            slotIdolIdentifiers = new List<int>();
            lock (storageLock)
            {
                ThrowIfDisposed();
                string entityId = singleId.ToString(CultureInfo.InvariantCulture);
                long newestSequence = long.MinValue;
                for (int eventIndex = CoreConstants.ZeroBasedListStartIndex;
                    eventIndex < activeEvents.Count;
                    eventIndex++)
                {
                    LightweightEventRecord record = activeEvents[eventIndex];
                    if (record == null ||
                        record.Sequence <= newestSequence ||
                        !string.IsNullOrEmpty(record.NamespaceIdentifier) ||
                        !string.Equals(
                            record.EntityKind,
                            CoreConstants.EventEntityKindSingle,
                            StringComparison.Ordinal) ||
                        !string.Equals(
                            record.EntityId,
                            entityId,
                            StringComparison.Ordinal) ||
                        !string.Equals(
                            record.EventType,
                            CoreConstants.EventTypeSingleReleased,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    List<int> candidateSlotIdolIdentifiers;
                    if (!SharedTimelineParticipants.TryReadSingleCastSlotIds(
                            record.PayloadJson,
                            out candidateSlotIdolIdentifiers) ||
                        !ContainsValidIdolIdentifier(
                            candidateSlotIdolIdentifiers))
                    {
                        continue;
                    }

                    newestSequence = record.Sequence;
                    slotIdolIdentifiers = candidateSlotIdolIdentifiers;
                }

                return newestSequence != long.MinValue;
            }
        }

        /// <summary>
        /// Reads the newest active start snapshot for one world tour. The stored
        /// participant set survives save/load and later vanilla roster changes.
        /// </summary>
        internal bool TryGetLatestTourRuntimeState(
            int tourId,
            out List<int> participantIdolIdentifiers,
            out string startDate)
        {
            participantIdolIdentifiers = new List<int>();
            startDate = string.Empty;
            lock (storageLock)
            {
                ThrowIfDisposed();
                string entityId = tourId.ToString(CultureInfo.InvariantCulture);
                long newestSequence = long.MinValue;
                for (int eventIndex = CoreConstants.ZeroBasedListStartIndex;
                    eventIndex < activeEvents.Count;
                    eventIndex++)
                {
                    LightweightEventRecord record = activeEvents[eventIndex];
                    if (record == null ||
                        record.Sequence <= newestSequence ||
                        !string.IsNullOrEmpty(record.NamespaceIdentifier) ||
                        !string.Equals(record.EntityKind,
                            CoreConstants.EventEntityKindTour,
                            StringComparison.Ordinal) ||
                        !string.Equals(record.EntityId, entityId,
                            StringComparison.Ordinal) ||
                        !string.Equals(record.EventType,
                            CoreConstants.EventTypeTourStarted,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    List<int> candidateParticipants;
                    if (!SharedTimelineParticipants.TryReadTourParticipantIds(
                            record.PayloadJson,
                            out candidateParticipants))
                    {
                        continue;
                    }

                    string candidateStartDate;
                    LightweightSidecarJson.TryReadStringProperty(
                        record.PayloadJson,
                        CoreConstants.JsonFieldTourStartDate,
                        out candidateStartDate);
                    newestSequence = record.Sequence;
                    participantIdolIdentifiers = candidateParticipants;
                    startDate = candidateStartDate ?? string.Empty;
                }

                return newestSequence != long.MinValue;
            }
        }

        internal bool TryReadMoneyTransactions(
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
                    ThrowIfDisposed();
                    int startDateKey =
                        CoreDateTimeUtility.BuildGameDateKey(startInclusive);
                    int endDateKey =
                        CoreDateTimeUtility.BuildGameDateKey(endExclusive);
                    int requestedCount = Math.Max(
                        MoneyLedgerConstants.MinimumReadCount,
                        maxCount);

                    foreach (KeyValuePair<
                        int,
                        List<LightweightEventRecord>> pair
                        in moneyTransactionsByDateKey)
                    {
                        if (pair.Key < startDateKey)
                        {
                            continue;
                        }
                        if (pair.Key >= endDateKey)
                        {
                            break;
                        }

                        List<LightweightEventRecord> rows = pair.Value;
                        for (int index = 0;
                            rows != null && index < rows.Count;
                            index++)
                        {
                            IMDataCoreEvent publicMoneyEvent =
                                ToPublicEvent(rows[index]);
                            publicMoneyEvent.PayloadJson =
                                CorePayloadCompaction
                                    .ExpandMoneyTransactionPayloadForPublic(
                                        rows[index]);
                            IMDataCoreMoneyTransaction transaction =
                                MoneyLedgerPayloadUtility.ToPublicModel(
                                    publicMoneyEvent);
                            if (transaction == null)
                            {
                                continue;
                            }
                            if (transactions.Count >= requestedCount)
                            {
                                wasTruncated = true;
                                return true;
                            }

                            transactions.Add(transaction);
                        }
                    }

                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage =
                        CoreConstants.MessageTryReadRecentEventsFailedPrefix +
                        exception.Message;
                    return false;
                }
            }
        }        internal bool TryGetMoneyLedgerCoverageStart(
            out DateTime coverageStart,
            out string errorMessage)
        {
            coverageStart = DateTime.MinValue;
            errorMessage = string.Empty;
            lock (storageLock)
            {
                try
                {
                    ThrowIfDisposed();
                    return moneyLedgerCoverageStartEvent != null &&
                        DateTime.TryParseExact(
                            moneyLedgerCoverageStartEvent.GameDateTime,
                            CoreConstants.RoundTripDateFormat,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind,
                            out coverageStart);
                }
                catch (Exception exception)
                {
                    errorMessage =
                        CoreConstants.MessageTryReadRecentEventsFailedPrefix +
                        exception.Message;
                    return false;
                }
            }
        }        /// <summary>
        /// Selects the sequence explicitly associated with the loaded vanilla
        /// checkpoint. The greatest sidecar sequence is never an implicit choice.
        /// </summary>
        internal bool TryActivateCheckpoint(
            VanillaSaveStamp stamp,
            out bool checkpointFound,
            out long activatedSequence,
            out string errorMessage)
        {
            checkpointFound = false;
            activatedSequence = 0L;
            errorMessage = string.Empty;
            if (stamp == null)
            {
                errorMessage = "The vanilla save stamp is missing.";
                return false;
            }

            lock (storageLock)
            {
                try
                {
                    ThrowIfDisposed();
                    LightweightCheckpointRecord matchingCheckpoint = null;
                    for (int index = 0; index < durableCheckpoints.Count; index++)
                    {
                        LightweightCheckpointRecord candidate = durableCheckpoints[index];
                        if (!stamp.Matches(candidate))
                        {
                            continue;
                        }

                        if (matchingCheckpoint != null)
                        {
                            errorMessage =
                                "The sidecar contains ambiguous duplicate checkpoint identities.";
                            return false;
                        }

                        matchingCheckpoint = candidate;
                    }

                    if (matchingCheckpoint == null)
                    {
                        return true;
                    }

                    DateTime checkpointGameDate;
                    try
                    {
                        checkpointGameDate = ExtensionMethods.ToDateTime(
                            matchingCheckpoint.GameDateTime);
                    }
                    catch (Exception exception)
                    {
                        errorMessage =
                            "The exact IMDC checkpoint has an invalid game date: " +
                            exception.Message;
                        return false;
                    }

                    ActivateThroughSequenceLocked(
                        matchingCheckpoint.Sequence,
                        checkpointGameDate);
                    checkpointFound = true;
                    activatedSequence = matchingCheckpoint.Sequence;
                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = "Exact IMDC checkpoint activation failed: " +
                        exception.Message;
                    return false;
                }
            }
        }

        internal bool TryActivateThroughGameDate(
            DateTime cutoffGameDate,
            out long activatedSequence,
            out string errorMessage)
        {
            activatedSequence = 0L;
            errorMessage = string.Empty;
            lock (storageLock)
            {
                try
                {
                    ThrowIfDisposed();
                    activeEvents = new List<LightweightEventRecord>();
                    activeCustomMutations =
                        new List<LightweightCustomMutationRecord>();
                    activeCheckpoints = new List<LightweightCheckpointRecord>();

                    for (int index = 0; index < durableEvents.Count; index++)
                    {
                        LightweightEventRecord record = durableEvents[index];
                        if (record != null && EventIsAtOrBefore(record, cutoffGameDate))
                        {
                            activeEvents.Add(CloneEvent(record));
                            activatedSequence = Math.Max(
                                activatedSequence,
                                record.Sequence);
                        }
                    }

                    for (int index = 0; index < durableCustomMutations.Count; index++)
                    {
                        LightweightCustomMutationRecord mutation =
                            durableCustomMutations[index];
                        if (mutation != null &&
                            CustomMutationIsAtOrBefore(mutation, cutoffGameDate))
                        {
                            activeCustomMutations.Add(CloneCustomMutation(mutation));
                            activatedSequence = Math.Max(
                                activatedSequence,
                                mutation.Sequence);
                        }
                    }

                    for (int index = 0; index < durableCheckpoints.Count; index++)
                    {
                        LightweightCheckpointRecord checkpoint =
                            durableCheckpoints[index];
                        if (checkpoint != null &&
                            CheckpointIsAtOrBefore(checkpoint, cutoffGameDate) &&
                            checkpoint.Sequence <= activatedSequence)
                        {
                            activeCheckpoints.Add(CloneCheckpoint(checkpoint));
                        }
                    }

                    RebuildRuntimeIndexesLocked();
                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = "Game-date IMDC fallback activation failed: " +
                        exception.Message;
                    return false;
                }
            }
        }

        internal bool AddOrReplaceCheckpoint(
            VanillaSaveStamp stamp,
            long sequence,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (stamp == null)
            {
                errorMessage = "The vanilla save stamp is missing.";
                return false;
            }

            lock (storageLock)
            {
                try
                {
                    ThrowIfDisposed();

                    DateTime checkpointGameDate;
                    try
                    {
                        checkpointGameDate = ExtensionMethods.ToDateTime(
                            stamp.GameDateTime);
                    }
                    catch (Exception exception)
                    {
                        errorMessage =
                            "The vanilla save checkpoint has an invalid game date: " +
                            exception.Message;
                        return false;
                    }

                    // A save boundary is a branch boundary. Never allow a row from
                    // a later in-game date, even one carrying an older sequence
                    // number, to become durable under this checkpoint.
                    TrimActiveStateToCheckpointLocked(
                        sequence,
                        checkpointGameDate);

                    for (int index = activeCheckpoints.Count - 1; index >= 0; index--)
                    {
                        if (stamp.Matches(activeCheckpoints[index]))
                        {
                            activeCheckpoints.RemoveAt(index);
                        }
                    }

                    activeCheckpoints.Add(
                        new LightweightCheckpointRecord
                        {
                            RelativeSavePath = stamp.RelativeSavePath,
                            LastSave = stamp.LastSave,
                            PlaytimeSeconds = stamp.PlaytimeSeconds,
                            GameDateTime = stamp.GameDateTime,
                            Sequence = sequence
                        });
                    if (sequence > lastIssuedSequence)
                    {
                        lastIssuedSequence = sequence;
                    }

                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = "Recording the IMDC checkpoint failed: " +
                        exception.Message;
                    return false;
                }
            }
        }

        internal bool TryPersistForScope(
            CoreSaveScope saveScope,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (saveScope == null || saveScope.IsTransient)
            {
                errorMessage = "A physical vanilla save scope is required.";
                return false;
            }

            lock (storageLock)
            {
                try
                {
                    ThrowIfDisposed();
                    string candidatePath = saveScope.SidecarFilePath ?? string.Empty;
                    string validationError;
                    string normalizedSidecarPath;
                    if (!CorePaths.TryValidateContainedMutationPath(
                        candidatePath,
                        false,
                        out normalizedSidecarPath,
                        out validationError))
                    {
                        errorMessage = validationError;
                        return false;
                    }

                    LightweightSidecarDocument document = BuildDocumentLocked(
                        saveScope.RelativeSavePath);
                    string json = LightweightSidecarJson.Serialize(document);
                    if (!TryWriteAtomicallyLocked(candidatePath, json, out errorMessage))
                    {
                        return false;
                    }

                    currentSidecarPath = candidatePath;
                    currentRelativeSavePath = VanillaSaveStamp.NormalizeRelativePath(
                        saveScope.RelativeSavePath);
                    activeCheckpoints = CloneCheckpoints(
                        document.Checkpoints);
                    CommitActiveAsDurableLocked();
                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = "Persisting the lightweight sidecar failed: " +
                        exception.Message;
                    return false;
                }
            }
        }

        internal bool TryPersistCurrent(out string errorMessage)
        {
            errorMessage = string.Empty;
            lock (storageLock)
            {
                try
                {
                    ThrowIfDisposed();
                    if (string.IsNullOrEmpty(currentSidecarPath) ||
                        string.IsNullOrEmpty(currentRelativeSavePath))
                    {
                        errorMessage = "No physical vanilla save scope is active.";
                        return false;
                    }

                    LightweightSidecarDocument document = BuildDocumentLocked(
                        currentRelativeSavePath);
                    string json = LightweightSidecarJson.Serialize(document);
                    if (!TryWriteAtomicallyLocked(
                        currentSidecarPath,
                        json,
                        out errorMessage))
                    {
                        return false;
                    }

                    activeCheckpoints = CloneCheckpoints(
                        document.Checkpoints);
                    CommitActiveAsDurableLocked();
                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = "Persisting the lightweight sidecar failed: " +
                        exception.Message;
                    return false;
                }
            }
        }

        internal bool TryAppendImportedEvent(
            LightweightEventRecord importedEvent,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (importedEvent == null)
            {
                return true;
            }
            if (!CoreEventRetention.ShouldPersist(importedEvent))
            {
                return true;
            }

            lock (storageLock)
            {
                try
                {
                    ThrowIfDisposed();
                    if (!TryReserveMutationSequenceLocked(
                        importedEvent.Sequence,
                        out errorMessage))
                    {
                        return false;
                    }

                    if (importedEvent.EventId <= 0L ||
                        activeEventIdentifiers.Contains(importedEvent.EventId))
                    {
                        errorMessage = "A legacy event has an invalid or duplicate event identifier.";
                        activeMutationSequences.Remove(importedEvent.Sequence);
                        return false;
                    }

                    LightweightEventRecord importedClone =
                        CloneEvent(importedEvent);
                    activeEvents.Add(importedClone);
                    activeEventIdentifiers.Add(importedEvent.EventId);
                    IndexEventLocked(importedClone);
                    if (importedEvent.EventId > lastIssuedSequence)
                    {
                        lastIssuedSequence = importedEvent.EventId;
                    }
                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = "Importing a legacy event failed: " + exception.Message;
                    return false;
                }
            }
        }

        internal bool TryAppendImportedCustomBaseline(
            long sequence,
            DateTime loadedGameDate,
            string namespaceIdentifier,
            string dataKey,
            string valueJson,
            out string errorMessage)
        {
            if (!TryValidateCustomDataMutation(
                namespaceIdentifier,
                dataKey,
                valueJson,
                false,
                out errorMessage))
            {
                return false;
            }

            lock (storageLock)
            {
                if (!TryReserveMutationSequenceLocked(sequence, out errorMessage))
                {
                    return false;
                }

                LightweightCustomMutationRecord baseline =
                    new LightweightCustomMutationRecord
                    {
                        Sequence = sequence,
                        GameDateKey = CoreDateTimeUtility.BuildGameDateKey(
                            loadedGameDate),
                        GameDateTime = CoreDateTimeUtility.ToRoundTripString(
                            loadedGameDate),
                        NamespaceIdentifier = namespaceIdentifier ?? string.Empty,
                        DataKey = dataKey ?? string.Empty,
                        Operation = CustomOperationSet,
                        ValueJson = valueJson ?? string.Empty
                    };
                activeCustomMutations.Add(baseline);
                customValues[BuildCustomDataCompositeKey(
                    namespaceIdentifier,
                    dataKey)] = new MaterializedCustomValue
                    {
                        NamespaceIdentifier = namespaceIdentifier ?? string.Empty,
                        DataKey = dataKey ?? string.Empty,
                        ValueJson = valueJson ?? string.Empty
                    };
                return true;
            }
        }

        private bool TryValidateDocumentLocked(
            LightweightSidecarDocument document,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (document == null)
            {
                errorMessage = "The sidecar JSON is empty or invalid.";
                return false;
            }

            if (!string.Equals(
                    document.FormatName,
                    SidecarFormatName,
                    StringComparison.Ordinal) ||
                (document.FormatVersion != SidecarFormatVersion &&
                 document.FormatVersion != 1))
            {
                errorMessage = "The sidecar format is unsupported.";
                return false;
            }

            string declaredRelativePath = VanillaSaveStamp.NormalizeRelativePath(
                document.RelativeSavePath);
            if (!string.Equals(
                    declaredRelativePath,
                    currentRelativeSavePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "The sidecar belongs to a different vanilla save path.";
                return false;
            }

            if (document.Events == null ||
                document.CustomMutations == null ||
                document.Checkpoints == null)
            {
                errorMessage =
                    "The sidecar is missing one or more required history collections.";
                return false;
            }

            HashSet<long> sequences = new HashSet<long>();
            HashSet<long> eventIdentifiers = new HashSet<long>();
            List<LightweightCheckpointRecord> validatedCheckpoints =
                new List<LightweightCheckpointRecord>();
            long maximumSequence = 0L;
            for (int index = 0; index < document.Events.Count; index++)
            {
                LightweightEventRecord record = document.Events[index];
                if (record == null || record.Sequence <= 0L ||
                    record.EventId <= 0L ||
                    !sequences.Add(record.Sequence) ||
                    !eventIdentifiers.Add(record.EventId))
                {
                    errorMessage = "The sidecar contains an invalid or duplicate event record.";
                    return false;
                }

                maximumSequence = Math.Max(
                    maximumSequence,
                    Math.Max(record.Sequence, record.EventId));
            }

            for (int index = 0; index < document.CustomMutations.Count; index++)
            {
                LightweightCustomMutationRecord mutation =
                    document.CustomMutations[index];
                bool operationIsValid = mutation != null &&
                    (string.Equals(
                         mutation.Operation,
                         CustomOperationSet,
                         StringComparison.Ordinal) ||
                     string.Equals(
                         mutation.Operation,
                         CustomOperationRemove,
                         StringComparison.Ordinal));
                if (!operationIsValid || mutation.Sequence <= 0L ||
                    !sequences.Add(mutation.Sequence))
                {
                    errorMessage = "The sidecar contains an invalid or duplicate custom-data mutation.";
                    return false;
                }

                maximumSequence = Math.Max(maximumSequence, mutation.Sequence);
            }

            for (int index = 0; index < document.Checkpoints.Count; index++)
            {
                LightweightCheckpointRecord checkpoint = document.Checkpoints[index];
                if (checkpoint == null || checkpoint.Sequence < 0L ||
                    checkpoint.Sequence > document.LastIssuedSequence ||
                    string.IsNullOrEmpty(
                        VanillaSaveStamp.NormalizeRelativePath(
                            checkpoint.RelativeSavePath)))
                {
                    errorMessage = "The sidecar contains an invalid checkpoint.";
                    return false;
                }

                for (int priorIndex = 0;
                    priorIndex < validatedCheckpoints.Count;
                    priorIndex++)
                {
                    if (CheckpointsHaveSameIdentity(
                        checkpoint,
                        validatedCheckpoints[priorIndex]))
                    {
                        errorMessage =
                            "The sidecar contains duplicate checkpoint identities.";
                        return false;
                    }
                }

                validatedCheckpoints.Add(checkpoint);
            }

            if (document.LastIssuedSequence < maximumSequence)
            {
                errorMessage = "The sidecar sequence watermark is inconsistent.";
                return false;
            }

            return true;
        }

        private void LoadDocumentLocked(LightweightSidecarDocument document)
        {
            int retiredTechnicalEventCount;
            List<LightweightEventRecord> retainedEvents =
                CoreEventRetention.FilterLoadedEvents(
                    document.Events,
                    out retiredTechnicalEventCount);

            int sparseMoneyPayloadCount;
            int sharedParticipantRowsRemoved;
            List<LightweightEventRecord> compactedEvents =
                CorePayloadCompaction.CompactLoadedEvents(
                    retainedEvents,
                    out sparseMoneyPayloadCount,
                    out sharedParticipantRowsRemoved);

            if (retiredTechnicalEventCount > 0)
            {
                CoreLog.Info(
                    "IM Data Core retired " +
                    retiredTechnicalEventCount.ToString(
                        CultureInfo.InvariantCulture) +
                    " redundant built-in telemetry rows while loading this sidecar.");
            }

            if (sparseMoneyPayloadCount > 0 || sharedParticipantRowsRemoved > 0)
            {
                CoreLog.Info(
                    "IM Data Core compacted " +
                    sparseMoneyPayloadCount.ToString(
                        CultureInfo.InvariantCulture) +
                    " money-detail payloads and removed " +
                    sharedParticipantRowsRemoved.ToString(
                        CultureInfo.InvariantCulture) +
                    " duplicate shared-participant rows in memory. " +
                    "The smaller format will be written at the next IMDC save boundary.");
            }

            durableEvents = CloneEvents(compactedEvents);
            durableCustomMutations =
                CloneCustomMutations(document.CustomMutations);
            durableCheckpoints = CloneCheckpoints(document.Checkpoints);
            activeEvents = CloneEvents(durableEvents);
            activeCustomMutations =
                CloneCustomMutations(durableCustomMutations);
            activeCheckpoints = CloneCheckpoints(durableCheckpoints);
            lastIssuedSequence = document.LastIssuedSequence;
            RebuildRuntimeIndexesLocked();
        }

        private void ActivateThroughSequenceLocked(
            long sequence,
            DateTime cutoffGameDate)
        {
            activeEvents = new List<LightweightEventRecord>();
            activeCustomMutations = new List<LightweightCustomMutationRecord>();
            activeCheckpoints = new List<LightweightCheckpointRecord>();

            for (int index = 0; index < durableEvents.Count; index++)
            {
                LightweightEventRecord record = durableEvents[index];
                if (record != null &&
                    record.Sequence <= sequence &&
                    EventIsAtOrBefore(record, cutoffGameDate))
                {
                    activeEvents.Add(CloneEvent(record));
                }
            }

            for (int index = 0; index < durableCustomMutations.Count; index++)
            {
                LightweightCustomMutationRecord mutation =
                    durableCustomMutations[index];
                if (mutation != null &&
                    mutation.Sequence <= sequence &&
                    CustomMutationIsAtOrBefore(mutation, cutoffGameDate))
                {
                    activeCustomMutations.Add(CloneCustomMutation(mutation));
                }
            }

            for (int index = 0; index < durableCheckpoints.Count; index++)
            {
                LightweightCheckpointRecord checkpoint = durableCheckpoints[index];
                if (checkpoint != null &&
                    checkpoint.Sequence <= sequence &&
                    CheckpointIsAtOrBefore(checkpoint, cutoffGameDate))
                {
                    activeCheckpoints.Add(CloneCheckpoint(checkpoint));
                }
            }

            RebuildRuntimeIndexesLocked();
        }

        /// <summary>
        /// Prunes the active branch to the vanilla save checkpoint before the
        /// sidecar is serialized. The global sequence watermark is intentionally
        /// not rewound, so identifiers remain monotonic if play continues from an
        /// older branch.
        /// </summary>
        private void TrimActiveStateToCheckpointLocked(
            long sequence,
            DateTime cutoffGameDate)
        {
            for (int index = activeEvents.Count - 1; index >= 0; index--)
            {
                LightweightEventRecord record = activeEvents[index];
                if (record == null ||
                    record.Sequence > sequence ||
                    !EventIsAtOrBefore(record, cutoffGameDate))
                {
                    activeEvents.RemoveAt(index);
                }
            }

            for (int index = activeCustomMutations.Count - 1; index >= 0; index--)
            {
                LightweightCustomMutationRecord mutation =
                    activeCustomMutations[index];
                if (mutation == null ||
                    mutation.Sequence > sequence ||
                    !CustomMutationIsAtOrBefore(mutation, cutoffGameDate))
                {
                    activeCustomMutations.RemoveAt(index);
                }
            }

            for (int index = activeCheckpoints.Count - 1; index >= 0; index--)
            {
                LightweightCheckpointRecord checkpoint = activeCheckpoints[index];
                if (checkpoint == null ||
                    checkpoint.Sequence > sequence ||
                    !CheckpointIsAtOrBefore(checkpoint, cutoffGameDate))
                {
                    activeCheckpoints.RemoveAt(index);
                }
            }

            RebuildRuntimeIndexesLocked();
        }

        private void RebuildRuntimeIndexesLocked()
        {
            customValues.Clear();
            activeMutationSequences.Clear();
            activeEventIdentifiers.Clear();
            timelineEventsByIdolId.Clear();
            globalTimelineEvents.Clear();
            moneyTransactionsByDateKey.Clear();
            moneyLedgerCoverageStartEvent = null;

            activeEvents.Sort(CompareEventsBySequenceAscending);
            activeCustomMutations.Sort(
                CompareCustomMutationsBySequenceAscending);

            for (int index = 0; index < activeEvents.Count; index++)
            {
                LightweightEventRecord record = activeEvents[index];
                if (record == null)
                {
                    continue;
                }

                activeMutationSequences.Add(record.Sequence);
                activeEventIdentifiers.Add(record.EventId);
                IndexEventLocked(record);
            }

            for (int index = 0;
                index < activeCustomMutations.Count;
                index++)
            {
                LightweightCustomMutationRecord mutation =
                    activeCustomMutations[index];
                if (mutation == null)
                {
                    continue;
                }

                activeMutationSequences.Add(mutation.Sequence);
                string compositeKey = BuildCustomDataCompositeKey(
                    mutation.NamespaceIdentifier,
                    mutation.DataKey);
                if (string.Equals(
                    mutation.Operation,
                    CustomOperationRemove,
                    StringComparison.Ordinal))
                {
                    customValues.Remove(compositeKey);
                }
                else
                {
                    customValues[compositeKey] =
                        new MaterializedCustomValue
                        {
                            NamespaceIdentifier =
                                mutation.NamespaceIdentifier ?? string.Empty,
                            DataKey = mutation.DataKey ?? string.Empty,
                            ValueJson = mutation.ValueJson ?? string.Empty
                        };
                }
            }
        }

        private void IndexEventLocked(LightweightEventRecord record)
        {
            if (record == null)
            {
                return;
            }

            if (string.Equals(
                record.EventType,
                MoneyLedgerConstants.EventTypeTransaction,
                StringComparison.Ordinal))
            {
                List<LightweightEventRecord> rows;
                if (!moneyTransactionsByDateKey.TryGetValue(
                    record.GameDateKey,
                    out rows))
                {
                    rows = new List<LightweightEventRecord>();
                    moneyTransactionsByDateKey.Add(record.GameDateKey, rows);
                }
                rows.Add(record);
                return;
            }

            if (string.Equals(
                record.EventType,
                MoneyLedgerConstants.EventTypeCoverageStarted,
                StringComparison.Ordinal))
            {
                if (moneyLedgerCoverageStartEvent == null ||
                    CompareEventsAscending(
                        record,
                        moneyLedgerCoverageStartEvent) < 0)
                {
                    moneyLedgerCoverageStartEvent = record;
                }
                return;
            }

            List<int> timelineParticipantIds;
            SharedTimelineParticipantResolution participantResolution =
                SharedTimelineParticipants.ResolveParticipantIds(
                    record,
                    out timelineParticipantIds);
            if (participantResolution ==
                SharedTimelineParticipantResolution.Shared)
            {
                for (int index = 0; index < timelineParticipantIds.Count; index++)
                {
                    AddTimelineEventForIdolLocked(
                        timelineParticipantIds[index],
                        record);
                }
                return;
            }

            if (participantResolution ==
                SharedTimelineParticipantResolution.ValidEmpty)
            {
                globalTimelineEvents.Add(record);
                return;
            }

            if (participantResolution ==
                SharedTimelineParticipantResolution.Malformed)
            {
                return;
            }

            if (record.IdolId >= CoreConstants.MinimumValidIdolIdentifier)
            {
                AddTimelineEventForIdolLocked(record.IdolId, record);
                return;
            }

            globalTimelineEvents.Add(record);
        }

        private void AddTimelineEventForIdolLocked(
            int idolId,
            LightweightEventRecord record)
        {
            List<LightweightEventRecord> idolRows;
            if (!timelineEventsByIdolId.TryGetValue(idolId, out idolRows))
            {
                idolRows = new List<LightweightEventRecord>();
                timelineEventsByIdolId.Add(idolId, idolRows);
            }

            idolRows.Add(record);
        }

        private static bool ContainsValidIdolIdentifier(
            IReadOnlyList<int> idolIdentifiers)
        {
            if (idolIdentifiers == null)
            {
                return false;
            }

            for (int idolIndex = CoreConstants.ZeroBasedListStartIndex;
                idolIndex < idolIdentifiers.Count;
                idolIndex++)
            {
                if (idolIdentifiers[idolIndex] >=
                    CoreConstants.MinimumValidIdolIdentifier)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryReserveMutationSequenceLocked(
            long sequence,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            ThrowIfDisposed();
            if (sequence <= 0L || !activeMutationSequences.Add(sequence))
            {
                errorMessage = "The IMDC mutation sequence is invalid or already active.";
                return false;
            }

            if (sequence > lastIssuedSequence)
            {
                lastIssuedSequence = sequence;
            }

            return true;
        }

        private LightweightSidecarDocument BuildDocumentLocked(
            string relativeSavePath)
        {
            return new LightweightSidecarDocument
            {
                FormatName = SidecarFormatName,
                FormatVersion = SidecarFormatVersion,
                RelativeSavePath = VanillaSaveStamp.NormalizeRelativePath(
                    relativeSavePath),
                LastIssuedSequence = lastIssuedSequence,
                Checkpoints = CloneCheckpointsForPath(
                    activeCheckpoints,
                    relativeSavePath),
                Events = CloneEvents(activeEvents),
                CustomMutations = CloneCustomMutations(activeCustomMutations)
            };
        }

        private bool TryWriteAtomicallyLocked(
            string targetPath,
            string content,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            string targetDirectory = Path.GetDirectoryName(targetPath);
            string directoryError;
            string normalizedDirectoryPath;
            if (!CorePaths.TryCreateContainedDirectory(
                targetDirectory,
                out normalizedDirectoryPath,
                out directoryError))
            {
                errorMessage = directoryError;
                return false;
            }

            string temporaryPath = targetPath + ".imdc.tmp." +
                Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            string backupPath = targetPath + ".imdc.bak";
            string normalizedTemporaryPath;
            string normalizedBackupPath;
            if (!CorePaths.TryValidateContainedMutationPath(
                    temporaryPath,
                    false,
                    out normalizedTemporaryPath,
                    out errorMessage) ||
                !CorePaths.TryValidateContainedMutationPath(
                    backupPath,
                    false,
                    out normalizedBackupPath,
                    out errorMessage))
            {
                return false;
            }

            try
            {
                byte[] bytes = new System.Text.UTF8Encoding(false).GetBytes(
                    content ?? string.Empty);
                using (FileStream stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                if (File.Exists(targetPath))
                {
                    if (File.Exists(backupPath))
                    {
                        string cleanupError;
                        if (!CorePaths.TryDeleteContainedFile(
                            backupPath,
                            out cleanupError))
                        {
                            throw new IOException(cleanupError);
                        }
                    }

                    File.Replace(temporaryPath, targetPath, backupPath, true);
                    string backupCleanupError;
                    if (!CorePaths.TryDeleteContainedFile(
                        backupPath,
                        out backupCleanupError))
                    {
                        CoreLog.Warn(
                            "The obsolete IMDC sidecar backup was retained: " +
                            backupCleanupError);
                    }
                }
                else
                {
                    File.Move(temporaryPath, targetPath);
                }

                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    string ignoredError;
                    CorePaths.TryDeleteContainedFile(
                        temporaryPath,
                        out ignoredError);
                }
            }
        }

        private void CommitActiveAsDurableLocked()
        {
            durableEvents = CloneEvents(activeEvents);
            durableCustomMutations = CloneCustomMutations(activeCustomMutations);
            durableCheckpoints = CloneCheckpoints(activeCheckpoints);
        }

        private void ResetStateLocked()
        {
            durableEvents = new List<LightweightEventRecord>();
            durableCustomMutations =
                new List<LightweightCustomMutationRecord>();
            durableCheckpoints = new List<LightweightCheckpointRecord>();
            activeEvents = new List<LightweightEventRecord>();
            activeCustomMutations =
                new List<LightweightCustomMutationRecord>();
            activeCheckpoints = new List<LightweightCheckpointRecord>();
            customValues.Clear();
            activeMutationSequences.Clear();
            activeEventIdentifiers.Clear();
            timelineEventsByIdolId.Clear();
            globalTimelineEvents.Clear();
            moneyTransactionsByDateKey.Clear();
            moneyLedgerCoverageStartEvent = null;
            currentSidecarPath = string.Empty;
            currentRelativeSavePath = string.Empty;
            lastIssuedSequence = 0L;
        }

        private static bool IsMoneyLedgerInternalEvent(
            LightweightEventRecord record)
        {
            return string.Equals(
                    record.EventType,
                    MoneyLedgerConstants.EventTypeTransaction,
                    StringComparison.Ordinal) ||
                string.Equals(
                    record.EventType,
                    MoneyLedgerConstants.EventTypeCoverageStarted,
                    StringComparison.Ordinal);
        }

        private static void AppendPublicEventsForIdol(
            IReadOnlyList<LightweightEventRecord> source,
            int requestedIdolId,
            int maximumCount,
            ICollection<IMDataCoreEvent> target)
        {
            for (int index = 0;
                index < source.Count && target.Count < maximumCount;
                index++)
            {
                LightweightEventRecord record = source[index];
                IMDataCoreEvent publicEvent = ToPublicEvent(record);
                if (SharedTimelineParticipants.IsSharedEvent(record))
                {
                    publicEvent.IdolId = requestedIdolId;
                    publicEvent.PayloadJson = SharedTimelineParticipants
                        .ExpandPayloadForPublic(
                            record,
                            requestedIdolId);
                }
                target.Add(publicEvent);
            }
        }
        private static void AppendPublicEvents(
            IReadOnlyList<LightweightEventRecord> source,
            int maximumCount,
            ICollection<IMDataCoreEvent> target)
        {
            for (int index = 0;
                index < source.Count && target.Count < maximumCount;
                index++)
            {
                target.Add(ToPublicEvent(source[index]));
            }
        }

        private static IMDataCoreEvent ToPublicEvent(
            LightweightEventRecord record)
        {
            return new IMDataCoreEvent
            {
                EventId = record.EventId,
                GameDateKey = record.GameDateKey,
                GameDateTime = record.GameDateTime ?? string.Empty,
                IdolId = record.IdolId,
                EntityKind = record.EntityKind ?? string.Empty,
                EntityId = record.EntityId ?? string.Empty,
                EventType = record.EventType ?? string.Empty,
                SourcePatch = record.SourcePatch ?? string.Empty,
                PayloadJson = record.PayloadJson ?? CoreConstants.EmptyJsonObject,
                NamespaceId = record.NamespaceIdentifier ?? string.Empty
            };
        }

        private static bool EventIsAtOrBefore(
            LightweightEventRecord record,
            DateTime cutoff)
        {
            DateTime parsed;
            if (DateTime.TryParseExact(
                record.GameDateTime,
                CoreConstants.RoundTripDateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out parsed))
            {
                return parsed <= cutoff;
            }

            return record.GameDateKey <= CoreDateTimeUtility.BuildGameDateKey(cutoff);
        }

        private static bool CustomMutationIsAtOrBefore(
            LightweightCustomMutationRecord mutation,
            DateTime cutoff)
        {
            DateTime parsed;
            if (DateTime.TryParseExact(
                mutation.GameDateTime,
                CoreConstants.RoundTripDateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out parsed))
            {
                return parsed <= cutoff;
            }

            return mutation.GameDateKey <=
                CoreDateTimeUtility.BuildGameDateKey(cutoff);
        }

        private static bool CheckpointIsAtOrBefore(
            LightweightCheckpointRecord checkpoint,
            DateTime cutoff)
        {
            try
            {
                return ExtensionMethods.ToDateTime(checkpoint.GameDateTime) <= cutoff;
            }
            catch
            {
                return false;
            }
        }

        private static bool CheckpointsHaveSameIdentity(
            LightweightCheckpointRecord left,
            LightweightCheckpointRecord right)
        {
            return left != null &&
                right != null &&
                string.Equals(
                    VanillaSaveStamp.NormalizeRelativePath(
                        left.RelativeSavePath),
                    VanillaSaveStamp.NormalizeRelativePath(
                        right.RelativeSavePath),
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    left.LastSave,
                    right.LastSave,
                    StringComparison.Ordinal) &&
                left.PlaytimeSeconds == right.PlaytimeSeconds &&
                string.Equals(
                    left.GameDateTime,
                    right.GameDateTime,
                    StringComparison.Ordinal);
        }

        private static string BuildCustomDataCompositeKey(
            string namespaceIdentifier,
            string dataKey)
        {
            return (namespaceIdentifier ?? string.Empty) + "\u001f" +
                (dataKey ?? string.Empty);
        }

        private int GetNamespaceKeyCountLocked(string namespaceIdentifier)
        {
            int count = 0;
            foreach (KeyValuePair<string, MaterializedCustomValue> pair in customValues)
            {
                if (pair.Value != null && string.Equals(
                    pair.Value.NamespaceIdentifier,
                    namespaceIdentifier ?? string.Empty,
                    StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private int GetNamespaceTotalLengthLocked(string namespaceIdentifier)
        {
            int length = 0;
            foreach (KeyValuePair<string, MaterializedCustomValue> pair in customValues)
            {
                if (pair.Value != null && string.Equals(
                    pair.Value.NamespaceIdentifier,
                    namespaceIdentifier ?? string.Empty,
                    StringComparison.Ordinal))
                {
                    length += pair.Value.ValueJson == null
                        ? 0
                        : pair.Value.ValueJson.Length;
                }
            }

            return length;
        }

        private static int CompareEventsDescending(
            LightweightEventRecord left,
            LightweightEventRecord right)
        {
            int dateComparison = right.GameDateKey.CompareTo(left.GameDateKey);
            return dateComparison != 0
                ? dateComparison
                : right.EventId.CompareTo(left.EventId);
        }

        private static int CompareEventsAscending(
            LightweightEventRecord left,
            LightweightEventRecord right)
        {
            int dateComparison = left.GameDateKey.CompareTo(right.GameDateKey);
            return dateComparison != 0
                ? dateComparison
                : left.EventId.CompareTo(right.EventId);
        }

        private static int CompareEventsBySequenceAscending(
            LightweightEventRecord left,
            LightweightEventRecord right)
        {
            return left.Sequence.CompareTo(right.Sequence);
        }

        private static int CompareCustomMutationsBySequenceAscending(
            LightweightCustomMutationRecord left,
            LightweightCustomMutationRecord right)
        {
            return left.Sequence.CompareTo(right.Sequence);
        }

        private static List<LightweightEventRecord> CloneEvents(
            IReadOnlyList<LightweightEventRecord> source)
        {
            List<LightweightEventRecord> clone =
                new List<LightweightEventRecord>();
            if (source == null)
            {
                return clone;
            }

            for (int index = 0; index < source.Count; index++)
            {
                if (source[index] != null)
                {
                    clone.Add(CloneEvent(source[index]));
                }
            }

            return clone;
        }

        private static LightweightEventRecord CloneEvent(
            LightweightEventRecord source)
        {
            return new LightweightEventRecord
            {
                Sequence = source.Sequence,
                EventId = source.EventId,
                GameDateKey = source.GameDateKey,
                GameDateTime = source.GameDateTime ?? string.Empty,
                IdolId = source.IdolId,
                EntityKind = source.EntityKind ?? string.Empty,
                EntityId = source.EntityId ?? string.Empty,
                EventType = source.EventType ?? string.Empty,
                SourcePatch = source.SourcePatch ?? string.Empty,
                NamespaceIdentifier = source.NamespaceIdentifier ?? string.Empty,
                PayloadJson = source.PayloadJson ?? CoreConstants.EmptyJsonObject
            };
        }

        private static List<LightweightCustomMutationRecord> CloneCustomMutations(
            IReadOnlyList<LightweightCustomMutationRecord> source)
        {
            List<LightweightCustomMutationRecord> clone =
                new List<LightweightCustomMutationRecord>();
            if (source == null)
            {
                return clone;
            }

            for (int index = 0; index < source.Count; index++)
            {
                if (source[index] != null)
                {
                    clone.Add(CloneCustomMutation(source[index]));
                }
            }

            return clone;
        }

        private static LightweightCustomMutationRecord CloneCustomMutation(
            LightweightCustomMutationRecord source)
        {
            return new LightweightCustomMutationRecord
            {
                Sequence = source.Sequence,
                GameDateKey = source.GameDateKey,
                GameDateTime = source.GameDateTime ?? string.Empty,
                NamespaceIdentifier = source.NamespaceIdentifier ?? string.Empty,
                DataKey = source.DataKey ?? string.Empty,
                Operation = source.Operation ?? CustomOperationSet,
                ValueJson = source.ValueJson ?? string.Empty
            };
        }

        private static List<LightweightCheckpointRecord> CloneCheckpoints(
            IReadOnlyList<LightweightCheckpointRecord> source)
        {
            List<LightweightCheckpointRecord> clone =
                new List<LightweightCheckpointRecord>();
            if (source == null)
            {
                return clone;
            }

            for (int index = 0; index < source.Count; index++)
            {
                if (source[index] != null)
                {
                    clone.Add(CloneCheckpoint(source[index]));
                }
            }

            return clone;
        }

        private static List<LightweightCheckpointRecord>
            CloneCheckpointsForPath(
                IReadOnlyList<LightweightCheckpointRecord> source,
                string relativeSavePath)
        {
            List<LightweightCheckpointRecord> clone =
                new List<LightweightCheckpointRecord>();
            if (source == null)
            {
                return clone;
            }

            string normalizedTargetPath =
                VanillaSaveStamp.NormalizeRelativePath(relativeSavePath);
            for (int index = 0; index < source.Count; index++)
            {
                LightweightCheckpointRecord checkpoint = source[index];
                if (checkpoint != null &&
                    string.Equals(
                        VanillaSaveStamp.NormalizeRelativePath(
                            checkpoint.RelativeSavePath),
                        normalizedTargetPath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    clone.Add(CloneCheckpoint(checkpoint));
                }
            }

            return clone;
        }

        private static LightweightCheckpointRecord CloneCheckpoint(
            LightweightCheckpointRecord source)
        {
            return new LightweightCheckpointRecord
            {
                RelativeSavePath = source.RelativeSavePath ?? string.Empty,
                LastSave = source.LastSave ?? string.Empty,
                PlaytimeSeconds = source.PlaytimeSeconds,
                GameDateTime = source.GameDateTime ?? string.Empty,
                Sequence = source.Sequence
            };
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(LightweightCoreStorageEngine));
            }
        }

        public void Dispose()
        {
            lock (storageLock)
            {
                if (disposed)
                {
                    return;
                }

                ResetStateLocked();
                disposed = true;
            }
        }
    }
}
