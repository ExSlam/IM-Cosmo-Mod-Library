using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Versioned JSON envelope for the lightweight sidecar. Version 3 stores
    /// JSON values structurally and omits fields that can be derived at load.
    /// Runtime dictionaries and read indexes are always rebuilt.
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

    /// <summary>
    /// Frozen list snapshot for one physical sidecar write. Record objects are
    /// immutable after insertion, so copying list references is sufficient and
    /// avoids duplicating an entire long-running campaign under the runtime lock.
    /// </summary>
    internal sealed class LightweightPersistenceSnapshot
    {
        internal string TargetPath = string.Empty;
        internal string RelativeSavePath = string.Empty;
        internal long Generation;
        internal bool PreserveExistingBackup;
        internal LightweightSidecarDocument Document;
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
        public int GameDateKey;
        public string GameDateTime = string.Empty;
        public int IdolId = CoreConstants.InvalidIdValue;
        public string EntityKind = string.Empty;
        public string EntityId = string.Empty;
        public string EventType = string.Empty;
        public string SourcePatch = string.Empty;
        public string NamespaceIdentifier = string.Empty;
        public string IdempotencyKey = string.Empty;
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
    /// The sole normal-runtime persistence implementation for IM Data Core 3.1.
    /// Mutations update memory only; callers explicitly persist at vanilla save
    /// boundaries or through TryFlushNow.
    /// </summary>
    internal sealed class LightweightCoreStorageEngine : IDisposable
    {
        internal const string SidecarFormatName = "IMDataCore.LightweightSidecar";
        internal const int SidecarFormatVersion = 3;
        internal const string CustomOperationSet = "SET";
        internal const string CustomOperationRemove = "REMOVE";

        private sealed class MaterializedCustomValue
        {
            internal string NamespaceIdentifier = string.Empty;
            internal string DataKey = string.Empty;
            internal string ValueJson = string.Empty;
        }

        private sealed class NamespaceUsage
        {
            internal int KeyCount;
            internal int TotalValueLength;
        }

        private readonly object storageLock = new object();
        private readonly object persistenceIoLock = new object();
        private readonly Dictionary<string, long> latestCommittedPersistenceGenerationByPath =
            new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, MaterializedCustomValue> customValues =
            new Dictionary<string, MaterializedCustomValue>(StringComparer.Ordinal);
        private readonly Dictionary<string, NamespaceUsage> customUsageByNamespace =
            new Dictionary<string, NamespaceUsage>(StringComparer.Ordinal);
        private readonly HashSet<long> activeMutationSequences = new HashSet<long>();
        private readonly HashSet<string> customEventIdempotencyKeys =
            new HashSet<string>(StringComparer.Ordinal);

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
        private string blockedPersistencePath = string.Empty;
        private string blockedPersistenceReason = string.Empty;
        private long lastIssuedSequence;
        private long nextPersistenceGeneration;
        private long lastCommittedPersistenceGeneration;
        private bool recoveredFromBackup;
        private bool loadedExistingSidecarDocument;
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

        internal bool HasLoadedSidecarDocument
        {
            get
            {
                lock (storageLock)
                {
                    return loadedExistingSidecarDocument;
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

        internal bool IsPersistenceBlocked
        {
            get
            {
                lock (storageLock)
                {
                    return !string.IsNullOrEmpty(blockedPersistencePath);
                }
            }
        }

        internal string PersistenceBlockReason
        {
            get
            {
                lock (storageLock)
                {
                    return blockedPersistenceReason ?? string.Empty;
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

                LightweightSidecarDocument document;
                string primaryError;
                if (TryLoadValidatedDocumentFromPathLocked(
                        currentSidecarPath,
                        out document,
                        out primaryError))
                {
                    LoadDocumentLocked(document);
                    loadedExistingSidecarDocument = true;
                    return true;
                }

                string backupPath = currentSidecarPath + ".imdc.bak";
                string normalizedBackupPath;
                string backupValidationError;
                if (CorePaths.TryValidateContainedMutationPath(
                        backupPath,
                        false,
                        out normalizedBackupPath,
                        out backupValidationError) &&
                    File.Exists(normalizedBackupPath))
                {
                    string backupError;
                    if (TryLoadValidatedDocumentFromPathLocked(
                            normalizedBackupPath,
                            out document,
                            out backupError))
                    {
                        LoadDocumentLocked(document);
                        loadedExistingSidecarDocument = true;
                        recoveredFromBackup = true;
                        errorMessage =
                            "The primary IM Data Core sidecar was unreadable or invalid " +
                            "and was left untouched. IMDC recovered this session from " +
                            "the last-known-good .imdc.bak generation. Primary error: " +
                            primaryError;
                        return true;
                    }

                    primaryError = primaryError +
                        " Backup recovery also failed: " + backupError;
                }

                errorMessage =
                    "The lightweight sidecar could not be loaded safely and was " +
                    "preserved. " + primaryError;
                BlockPersistenceForCurrentScopeLocked(errorMessage);
                return false;
            }
        }


        /// <summary>
        /// Starts an empty writable branch only when the physical sidecar does not
        /// already exist. Existing unreadable/unsupported sidecars are never silently
        /// converted into writable empty state.
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
                    if (!CorePaths.TryValidateContainedMutationPath(
                            currentSidecarPath,
                            false,
                            out normalizedSidecarPath,
                            out errorMessage))
                    {
                        ResetStateLocked();
                        return false;
                    }

                    if (File.Exists(currentSidecarPath))
                    {
                        errorMessage =
                            "An existing IM Data Core sidecar was preserved instead of " +
                            "being replaced with empty state.";
                        BlockPersistenceForCurrentScopeLocked(errorMessage);
                        return false;
                    }

                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = exception.Message;
                    ResetStateLocked();
                    return false;
                }
            }
        }

        /// <summary>
        /// Clears active supplemental state while protecting the existing physical
        /// sidecar from overwrite. Saving to another vanilla path remains allowed.
        /// </summary>
        internal void EnterReadOnlyEmptyForCurrentScope(string reason)
        {
            lock (storageLock)
            {
                ThrowIfDisposed();
                string sidecarPath = currentSidecarPath;
                string relativePath = currentRelativeSavePath;
                ResetStateLocked();
                currentSidecarPath = sidecarPath ?? string.Empty;
                currentRelativeSavePath = relativePath ?? string.Empty;
                blockedPersistencePath = currentSidecarPath;
                blockedPersistenceReason = string.IsNullOrEmpty(reason)
                    ? "The existing IM Data Core sidecar is protected from overwrite."
                    : reason;
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
                    // of physical records. Compaction intentionally leaves gaps.
                    // If every incoming event is retention-filtered there is nothing
                    // else to validate, so advancing the issued watermark is safe.
                    if (retained.Count == 0)
                    {
                        if (highestIncomingSequence > lastIssuedSequence)
                        {
                            lastIssuedSequence = highestIncomingSequence;
                        }

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
                    HashSet<string> batchIdempotencyKeys =
                        new HashSet<string>(StringComparer.Ordinal);
                    List<string> normalizedPayloads =
                        new List<string>(compacted.Count);

                    for (int index = 0; index < compacted.Count; index++)
                    {
                        PendingEvent pending = compacted[index];
                        if (pending.CaptureSequence <= 0L ||
                            activeMutationSequences.Contains(pending.CaptureSequence) ||
                            !batchSequences.Add(pending.CaptureSequence))
                        {
                            errorMessage =
                                "An event has an invalid or duplicate sequence.";
                            return false;
                        }

                        if (!string.IsNullOrEmpty(pending.IdempotencyKey))
                        {
                            if (string.IsNullOrEmpty(pending.NamespaceIdentifier))
                            {
                                errorMessage =
                                    "Only namespaced custom events may use idempotency keys.";
                                return false;
                            }

                            string compositeIdempotencyKey =
                                BuildCustomEventIdempotencyCompositeKey(
                                    pending.NamespaceIdentifier,
                                    pending.IdempotencyKey);
                            if (customEventIdempotencyKeys.Contains(
                                    compositeIdempotencyKey) ||
                                !batchIdempotencyKeys.Add(
                                    compositeIdempotencyKey))
                            {
                                errorMessage =
                                    "A custom event idempotency key is already active.";
                                return false;
                            }
                        }

                        string normalizedPayload;
                        string jsonError;
                        if (!LightweightSidecarJson.TryNormalizeJsonDocument(
                                pending.PayloadJson ?? CoreConstants.EmptyJsonObject,
                                out normalizedPayload,
                                out jsonError))
                        {
                            errorMessage =
                                "An event payload is not valid JSON: " + jsonError;
                            return false;
                        }

                        normalizedPayloads.Add(normalizedPayload);
                    }

                    // No state is mutated for retained events until every compacted
                    // record has passed sequence and JSON validation.
                    if (highestIncomingSequence > lastIssuedSequence)
                    {
                        lastIssuedSequence = highestIncomingSequence;
                    }

                    for (int index = 0; index < compacted.Count; index++)
                    {
                        PendingEvent pending = compacted[index];
                        LightweightEventRecord record = new LightweightEventRecord
                        {
                            Sequence = pending.CaptureSequence,
                            GameDateKey = pending.GameDateKey,
                            GameDateTime = pending.GameDateTime ?? string.Empty,
                            IdolId = pending.IdolId,
                            EntityKind = pending.EntityKind ?? string.Empty,
                            EntityId = pending.EntityId ?? string.Empty,
                            EventType = pending.EventType ?? string.Empty,
                            SourcePatch = pending.SourcePatch ?? string.Empty,
                            NamespaceIdentifier =
                                pending.NamespaceIdentifier ?? string.Empty,
                            IdempotencyKey = pending.IdempotencyKey ?? string.Empty,
                            PayloadJson = normalizedPayloads[index]
                        };

                        activeEvents.Add(record);
                        activeMutationSequences.Add(record.Sequence);
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
        }
        internal bool TrySetCustomData(
            long sequence,
            DateTime gameDate,
            string namespaceIdentifier,
            string dataKey,
            string jsonValue,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            string normalizedJson;
            if (!LightweightSidecarJson.TryNormalizeJsonDocument(
                    jsonValue,
                    out normalizedJson,
                    out errorMessage))
            {
                return false;
            }

            lock (storageLock)
            {
                try
                {
                    ThrowIfDisposed();

                    if (normalizedJson.Length >
                        CoreConstants.MaximumCustomValueCharacterCount)
                    {
                        errorMessage = CoreConstants.MessageJsonValueTooLong;
                        return false;
                    }

                    string normalizedNamespace =
                        namespaceIdentifier ?? string.Empty;
                    string normalizedDataKey =
                        dataKey ?? string.Empty;
                    string compositeKey = BuildCustomDataCompositeKey(
                        normalizedNamespace,
                        normalizedDataKey);

                    MaterializedCustomValue existing;
                    bool exists = customValues.TryGetValue(
                        compositeKey,
                        out existing) &&
                        existing != null;

                    if (exists &&
                        string.Equals(
                            existing.ValueJson ?? string.Empty,
                            normalizedJson,
                            StringComparison.Ordinal))
                    {
                        // A SET to the already-materialized value is a logical no-op.
                        return true;
                    }

                    NamespaceUsage usage =
                        GetNamespaceUsageLocked(normalizedNamespace);
                    if (!exists &&
                        usage.KeyCount >=
                            CoreConstants.MaximumCustomKeysPerNamespace)
                    {
                        errorMessage =
                            CoreConstants.MessageNamespaceKeyQuotaExceeded;
                        return false;
                    }

                    int existingLength = exists &&
                        existing.ValueJson != null
                            ? existing.ValueJson.Length
                            : 0;
                    int projectedLength =
                        usage.TotalValueLength -
                        existingLength +
                        normalizedJson.Length;
                    if (projectedLength >
                        CoreConstants.MaximumNamespaceCharacterBudget)
                    {
                        errorMessage =
                            CoreConstants.MessageNamespaceDataBudgetExceeded;
                        return false;
                    }

                    if (!TryReserveMutationSequenceLocked(
                            sequence,
                            out errorMessage))
                    {
                        return false;
                    }

                    LightweightCustomMutationRecord mutation =
                        new LightweightCustomMutationRecord
                        {
                            Sequence = sequence,
                            GameDateKey =
                                CoreDateTimeUtility.BuildGameDateKey(gameDate),
                            GameDateTime =
                                CoreDateTimeUtility.ToRoundTripString(gameDate),
                            NamespaceIdentifier = normalizedNamespace,
                            DataKey = normalizedDataKey,
                            Operation = CustomOperationSet,
                            ValueJson = normalizedJson
                        };

                    activeCustomMutations.Add(mutation);
                    ApplyMaterializedCustomSetLocked(
                        mutation.NamespaceIdentifier,
                        mutation.DataKey,
                        mutation.ValueJson);
                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage =
                        "Setting custom data failed: " +
                        exception.Message;
                    return false;
                }
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
                    if (!customValues.ContainsKey(compositeKey))
                    {
                        // Removing an absent key is a logical no-op.
                        return true;
                    }

                    if (!TryReserveMutationSequenceLocked(
                            sequence,
                            out errorMessage))
                    {
                        return false;
                    }

                    LightweightCustomMutationRecord mutation =
                        new LightweightCustomMutationRecord
                        {
                            Sequence = sequence,
                            GameDateKey =
                                CoreDateTimeUtility.BuildGameDateKey(gameDate),
                            GameDateTime =
                                CoreDateTimeUtility.ToRoundTripString(gameDate),
                            NamespaceIdentifier =
                                namespaceIdentifier ?? string.Empty,
                            DataKey = dataKey ?? string.Empty,
                            Operation = CustomOperationRemove,
                            ValueJson = string.Empty
                        };

                    activeCustomMutations.Add(mutation);
                    ApplyMaterializedCustomRemoveLocked(
                        mutation.NamespaceIdentifier,
                        mutation.DataKey);
                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage =
                        "Removing custom data failed: " +
                        exception.Message;
                    return false;
                }
            }
        }


        internal bool ContainsCustomEventIdempotencyKey(
            string namespaceIdentifier,
            string idempotencyKey)
        {
            if (string.IsNullOrEmpty(namespaceIdentifier) ||
                string.IsNullOrEmpty(idempotencyKey))
            {
                return false;
            }

            lock (storageLock)
            {
                ThrowIfDisposed();
                return customEventIdempotencyKeys.Contains(
                    BuildCustomEventIdempotencyCompositeKey(
                        namespaceIdentifier,
                        idempotencyKey));
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

                    List<LightweightEventRecord> idolEvents;
                    timelineEventsByIdolId.TryGetValue(idolId, out idolEvents);

                    int idolIndex = idolEvents != null
                        ? idolEvents.Count - 1
                        : -1;
                    int globalIndex = globalTimelineEvents.Count - 1;

                    while (events.Count < maxCount &&
                        (idolIndex >= 0 || globalIndex >= 0))
                    {
                        LightweightEventRecord next;
                        bool fromIdol;
                        if (idolIndex < 0)
                        {
                            next = globalTimelineEvents[globalIndex--];
                            fromIdol = false;
                        }
                        else if (globalIndex < 0)
                        {
                            next = idolEvents[idolIndex--];
                            fromIdol = true;
                        }
                        else if (CompareEventsAscending(
                            idolEvents[idolIndex],
                            globalTimelineEvents[globalIndex]) >= 0)
                        {
                            next = idolEvents[idolIndex--];
                            fromIdol = true;
                        }
                        else
                        {
                            next = globalTimelineEvents[globalIndex--];
                            fromIdol = false;
                        }

                        IMDataCoreEvent publicEvent = ToPublicEvent(next);
                        if (fromIdol &&
                            SharedTimelineParticipants.IsSharedEvent(next))
                        {
                            publicEvent.IdolId = idolId;
                            publicEvent.PayloadJson =
                                SharedTimelineParticipants.ExpandPayloadForPublic(
                                    next,
                                    idolId);
                        }
                        events.Add(publicEvent);
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

        internal bool TryCreatePersistenceSnapshot(
            CoreSaveScope saveScope,
            out LightweightPersistenceSnapshot snapshot,
            out string errorMessage)
        {
            snapshot = null;
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
                    string normalizedSidecarPath;
                    if (!CorePaths.TryValidateContainedMutationPath(
                            candidatePath,
                            false,
                            out normalizedSidecarPath,
                            out errorMessage))
                    {
                        return false;
                    }

                    if (IsPersistenceBlockedForPathLocked(candidatePath))
                    {
                        errorMessage = blockedPersistenceReason +
                            " Save As to a different vanilla path is allowed.";
                        return false;
                    }

                    long generation = ++nextPersistenceGeneration;
                    snapshot = new LightweightPersistenceSnapshot
                    {
                        TargetPath = normalizedSidecarPath,
                        RelativeSavePath = VanillaSaveStamp.NormalizeRelativePath(
                            saveScope.RelativeSavePath),
                        Generation = generation,
                        PreserveExistingBackup = recoveredFromBackup &&
                            string.Equals(
                                normalizedSidecarPath,
                                Path.GetFullPath(currentSidecarPath ?? string.Empty),
                                StringComparison.OrdinalIgnoreCase),
                        Document = BuildDocumentLocked(saveScope.RelativeSavePath)
                    };
                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = "Creating the IMDC persistence snapshot failed: " +
                        exception.Message;
                    return false;
                }
            }
        }

        internal bool TryCreateCurrentPersistenceSnapshot(
            out LightweightPersistenceSnapshot snapshot,
            out string errorMessage)
        {
            snapshot = null;
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
                    if (IsPersistenceBlockedForPathLocked(currentSidecarPath))
                    {
                        errorMessage = blockedPersistenceReason;
                        return false;
                    }

                    string normalizedSidecarPath;
                    if (!CorePaths.TryValidateContainedMutationPath(
                            currentSidecarPath,
                            false,
                            out normalizedSidecarPath,
                            out errorMessage))
                    {
                        return false;
                    }

                    long generation = ++nextPersistenceGeneration;
                    snapshot = new LightweightPersistenceSnapshot
                    {
                        TargetPath = normalizedSidecarPath,
                        RelativeSavePath = currentRelativeSavePath,
                        Generation = generation,
                        PreserveExistingBackup = recoveredFromBackup,
                        Document = BuildDocumentLocked(currentRelativeSavePath)
                    };
                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = "Creating the current IMDC persistence snapshot failed: " +
                        exception.Message;
                    return false;
                }
            }
        }

        internal bool TryPersistSnapshot(
            LightweightPersistenceSnapshot snapshot,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (snapshot == null || snapshot.Document == null ||
                string.IsNullOrEmpty(snapshot.TargetPath))
            {
                errorMessage = "The IMDC persistence snapshot is invalid.";
                return false;
            }

            lock (persistenceIoLock)
            {
                lock (storageLock)
                {
                    if (disposed)
                    {
                        errorMessage = "The IMDC storage engine is disposed.";
                        return false;
                    }

                    long latestCommitted;
                    if (latestCommittedPersistenceGenerationByPath.TryGetValue(
                            snapshot.TargetPath,
                            out latestCommitted) &&
                        latestCommitted > snapshot.Generation)
                    {
                        // A newer write for this exact path is already durable.
                        // Never let an older concurrent snapshot regress it.
                        return true;
                    }
                }

                Stopwatch stopwatch = Stopwatch.StartNew();
                if (!TryWriteAtomically(
                        snapshot.TargetPath,
                        snapshot.Document,
                        snapshot.PreserveExistingBackup,
                        out errorMessage))
                {
                    return false;
                }
                stopwatch.Stop();

                long persistedBytes = 0L;
                try
                {
                    persistedBytes = new FileInfo(snapshot.TargetPath).Length;
                }
                catch
                {
                }

                lock (storageLock)
                {
                    long committedForPath;
                    if (!latestCommittedPersistenceGenerationByPath.TryGetValue(
                            snapshot.TargetPath,
                            out committedForPath) ||
                        snapshot.Generation > committedForPath)
                    {
                        latestCommittedPersistenceGenerationByPath[
                            snapshot.TargetPath] = snapshot.Generation;
                    }

                    if (!disposed &&
                        snapshot.Generation >= lastCommittedPersistenceGeneration)
                    {
                        lastCommittedPersistenceGeneration = snapshot.Generation;
                        currentSidecarPath = snapshot.TargetPath;
                        currentRelativeSavePath = snapshot.RelativeSavePath;
                        blockedPersistencePath = string.Empty;
                        blockedPersistenceReason = string.Empty;
                        recoveredFromBackup = false;
                        durableEvents = new List<LightweightEventRecord>(
                            snapshot.Document.Events);
                        durableCustomMutations =
                            new List<LightweightCustomMutationRecord>(
                                snapshot.Document.CustomMutations);
                        durableCheckpoints =
                            new List<LightweightCheckpointRecord>(
                                snapshot.Document.Checkpoints);

                        // Only replace the active path-filtered checkpoint list when
                        // no newer save snapshot was prepared while this file wrote.
                        if (snapshot.Generation == nextPersistenceGeneration)
                        {
                            activeCheckpoints =
                                new List<LightweightCheckpointRecord>(
                                    snapshot.Document.Checkpoints);
                        }
                    }
                }

                CoreLog.Info(
                    "IM Data Core persisted sidecar: events=" +
                    snapshot.Document.Events.Count.ToString(CultureInfo.InvariantCulture) +
                    ", custom_mutations=" +
                    snapshot.Document.CustomMutations.Count.ToString(CultureInfo.InvariantCulture) +
                    ", checkpoints=" +
                    snapshot.Document.Checkpoints.Count.ToString(CultureInfo.InvariantCulture) +
                    ", bytes=" + persistedBytes.ToString(CultureInfo.InvariantCulture) +
                    ", elapsed_ms=" +
                    stopwatch.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) +
                    ".");
                return true;
            }
        }

        internal bool TryPersistForScope(
            CoreSaveScope saveScope,
            out string errorMessage)
        {
            LightweightPersistenceSnapshot snapshot;
            return TryCreatePersistenceSnapshot(
                    saveScope,
                    out snapshot,
                    out errorMessage) &&
                TryPersistSnapshot(snapshot, out errorMessage);
        }

        internal bool TryPersistCurrent(out string errorMessage)
        {
            LightweightPersistenceSnapshot snapshot;
            return TryCreateCurrentPersistenceSnapshot(
                    out snapshot,
                    out errorMessage) &&
                TryPersistSnapshot(snapshot, out errorMessage);
        }


        private bool TryLoadValidatedDocumentFromPathLocked(
            string path,
            out LightweightSidecarDocument document,
            out string errorMessage)
        {
            document = null;
            errorMessage = string.Empty;
            try
            {
                string rawJson = File.ReadAllText(path);
                document = LightweightSidecarJson.Deserialize(rawJson);
                if (!TryValidateDocumentLocked(document, out errorMessage))
                {
                    document = null;
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                document = null;
                return false;
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
                document.FormatVersion < 1 ||
                document.FormatVersion > SidecarFormatVersion)
            {
                errorMessage =
                    "The sidecar format is unsupported by this IM Data Core version.";
                return false;
            }

            string declaredRelativePath =
                VanillaSaveStamp.NormalizeRelativePath(
                    document.RelativeSavePath);
            if (!string.Equals(
                    declaredRelativePath,
                    currentRelativeSavePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                errorMessage =
                    "The sidecar belongs to a different vanilla save path.";
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
            HashSet<string> eventIdempotencyKeys =
                new HashSet<string>(StringComparer.Ordinal);
            List<LightweightCheckpointRecord> validatedCheckpoints =
                new List<LightweightCheckpointRecord>();
            long maximumSequence = 0L;

            for (int index = 0; index < document.Events.Count; index++)
            {
                LightweightEventRecord record = document.Events[index];
                if (record == null ||
                    record.Sequence <= 0L ||
                    !sequences.Add(record.Sequence))
                {
                    errorMessage =
                        "The sidecar contains an invalid or duplicate event sequence.";
                    return false;
                }

                if (!string.IsNullOrEmpty(record.IdempotencyKey))
                {
                    string sanitizedIdempotencyKey =
                        CoreTokenUtility.SanitizeToken(
                            record.IdempotencyKey,
                            CoreConstants.IdempotencyKeyMaximumLength);
                    if (string.IsNullOrEmpty(record.NamespaceIdentifier) ||
                        sanitizedIdempotencyKey.Length <
                            CoreConstants.IdempotencyKeyMinimumLength ||
                        !string.Equals(
                            record.IdempotencyKey,
                            sanitizedIdempotencyKey,
                            StringComparison.Ordinal) ||
                        !eventIdempotencyKeys.Add(
                            BuildCustomEventIdempotencyCompositeKey(
                                record.NamespaceIdentifier,
                                record.IdempotencyKey)))
                    {
                        errorMessage =
                            "The sidecar contains an invalid or duplicate custom-event idempotency key.";
                        return false;
                    }
                }

                string normalizedPayload;
                string jsonError;
                if (!LightweightSidecarJson.TryNormalizeJsonDocument(
                        record.PayloadJson,
                        out normalizedPayload,
                        out jsonError))
                {
                    errorMessage =
                        "The sidecar contains an invalid event payload: " +
                        jsonError;
                    return false;
                }

                record.PayloadJson = normalizedPayload;
                maximumSequence = Math.Max(
                    maximumSequence,
                    record.Sequence);
            }

            for (int index = 0;
                index < document.CustomMutations.Count;
                index++)
            {
                LightweightCustomMutationRecord mutation =
                    document.CustomMutations[index];
                bool operationIsSet = mutation != null &&
                    string.Equals(
                        mutation.Operation,
                        CustomOperationSet,
                        StringComparison.Ordinal);
                bool operationIsRemove = mutation != null &&
                    string.Equals(
                        mutation.Operation,
                        CustomOperationRemove,
                        StringComparison.Ordinal);

                if ((!operationIsSet && !operationIsRemove) ||
                    mutation.Sequence <= 0L ||
                    !sequences.Add(mutation.Sequence))
                {
                    errorMessage =
                        "The sidecar contains an invalid or duplicate custom-data mutation.";
                    return false;
                }

                if (operationIsSet)
                {
                    string normalizedValue;
                    string jsonError;
                    if (!LightweightSidecarJson.TryNormalizeJsonDocument(
                            mutation.ValueJson,
                            out normalizedValue,
                            out jsonError))
                    {
                        errorMessage =
                            "The sidecar contains an invalid custom-data value: " +
                            jsonError;
                        return false;
                    }

                    mutation.ValueJson = normalizedValue;
                }
                else
                {
                    mutation.ValueJson = string.Empty;
                }

                maximumSequence = Math.Max(
                    maximumSequence,
                    mutation.Sequence);
            }

            for (int index = 0;
                index < document.Checkpoints.Count;
                index++)
            {
                LightweightCheckpointRecord checkpoint =
                    document.Checkpoints[index];

                if (checkpoint == null ||
                    checkpoint.Sequence < 0L ||
                    checkpoint.Sequence > document.LastIssuedSequence ||
                    string.IsNullOrEmpty(
                        VanillaSaveStamp.NormalizeRelativePath(
                            checkpoint.RelativeSavePath)))
                {
                    errorMessage =
                        "The sidecar contains an invalid checkpoint.";
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
                errorMessage =
                    "The sidecar sequence watermark is inconsistent.";
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

            // Records are immutable after validation/compaction. Keep separate
            // lists but share record objects so loading a multi-decade campaign
            // does not multiply every event/mutation/checkpoint allocation.
            durableEvents = new List<LightweightEventRecord>(compactedEvents);
            durableCustomMutations =
                new List<LightweightCustomMutationRecord>(
                    document.CustomMutations);
            durableCheckpoints =
                new List<LightweightCheckpointRecord>(document.Checkpoints);
            activeEvents = new List<LightweightEventRecord>(durableEvents);
            activeCustomMutations =
                new List<LightweightCustomMutationRecord>(
                    durableCustomMutations);
            activeCheckpoints =
                new List<LightweightCheckpointRecord>(durableCheckpoints);
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
                    activeEvents.Add(record);
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
                    activeCustomMutations.Add(mutation);
                }
            }

            for (int index = 0; index < durableCheckpoints.Count; index++)
            {
                LightweightCheckpointRecord checkpoint = durableCheckpoints[index];
                if (checkpoint != null &&
                    checkpoint.Sequence <= sequence &&
                    CheckpointIsAtOrBefore(checkpoint, cutoffGameDate))
                {
                    activeCheckpoints.Add(checkpoint);
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
            bool activeMutationTrimmed = false;

            for (int index = activeEvents.Count - 1; index >= 0; index--)
            {
                LightweightEventRecord record = activeEvents[index];
                if (record == null ||
                    record.Sequence > sequence ||
                    !EventIsAtOrBefore(record, cutoffGameDate))
                {
                    activeEvents.RemoveAt(index);
                    activeMutationTrimmed = true;
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
                    activeMutationTrimmed = true;
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

            // Forward saves normally trim nothing. Rebuilding and sorting every
            // timeline/custom-data index in that common case is pure O(history)
            // overhead on top of serialization. Rebuild only when event or custom
            // mutation membership actually changed. Checkpoint-only changes do not
            // participate in those derived indexes.
            if (activeMutationTrimmed)
            {
                RebuildRuntimeIndexesLocked();
            }
        }

        private void RebuildRuntimeIndexesLocked()
        {
            customValues.Clear();
            customUsageByNamespace.Clear();
            activeMutationSequences.Clear();
            customEventIdempotencyKeys.Clear();
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
                if (string.Equals(
                        mutation.Operation,
                        CustomOperationRemove,
                        StringComparison.Ordinal))
                {
                    ApplyMaterializedCustomRemoveLocked(
                        mutation.NamespaceIdentifier,
                        mutation.DataKey);
                }
                else
                {
                    ApplyMaterializedCustomSetLocked(
                        mutation.NamespaceIdentifier,
                        mutation.DataKey,
                        mutation.ValueJson);
                }
            }
        }


        private void IndexEventLocked(LightweightEventRecord record)
        {
            if (record == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(record.NamespaceIdentifier) &&
                !string.IsNullOrEmpty(record.IdempotencyKey))
            {
                customEventIdempotencyKeys.Add(
                    BuildCustomEventIdempotencyCompositeKey(
                        record.NamespaceIdentifier,
                        record.IdempotencyKey));
            }

            if (string.IsNullOrEmpty(record.NamespaceIdentifier) &&
                string.Equals(
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

            if (string.IsNullOrEmpty(record.NamespaceIdentifier) &&
                string.Equals(
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
                AddEventSortedAscending(globalTimelineEvents, record);
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

            AddEventSortedAscending(globalTimelineEvents, record);
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

            AddEventSortedAscending(idolRows, record);
        }

        private static void AddEventSortedAscending(
            List<LightweightEventRecord> rows,
            LightweightEventRecord record)
        {
            if (rows == null || record == null)
            {
                return;
            }

            if (rows.Count == 0 ||
                CompareEventsAscending(rows[rows.Count - 1], record) <= 0)
            {
                rows.Add(record);
                return;
            }

            int low = 0;
            int high = rows.Count;
            while (low < high)
            {
                int middle = low + ((high - low) / 2);
                if (CompareEventsAscending(rows[middle], record) <= 0)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }
            rows.Insert(low, record);
        }

        private static string BuildCustomEventIdempotencyCompositeKey(
            string namespaceIdentifier,
            string idempotencyKey)
        {
            return string.Concat(
                namespaceIdentifier ?? string.Empty,
                "\n",
                idempotencyKey ?? string.Empty);
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
                Events = new List<LightweightEventRecord>(activeEvents),
                CustomMutations =
                    new List<LightweightCustomMutationRecord>(
                        activeCustomMutations)
            };
        }

        private bool TryWriteAtomically(
            string targetPath,
            LightweightSidecarDocument document,
            bool preserveExistingBackup,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            string targetDirectory = Path.GetDirectoryName(targetPath);
            string normalizedDirectoryPath;
            if (!CorePaths.TryCreateContainedDirectory(
                    targetDirectory,
                    out normalizedDirectoryPath,
                    out errorMessage))
            {
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
                using (FileStream stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    65536,
                    FileOptions.SequentialScan))
                using (StreamWriter writer = new StreamWriter(
                    stream,
                    new System.Text.UTF8Encoding(false),
                    65536,
                    true))
                {
                    LightweightSidecarJson.SerializeTo(writer, document);
                    writer.Flush();
                    stream.Flush(true);
                }

                if (File.Exists(targetPath))
                {
                    if (preserveExistingBackup)
                    {
                        // The primary was corrupt and this session was recovered
                        // from .imdc.bak. Replace the damaged primary without
                        // overwriting the known-good recovery generation.
                        File.Replace(temporaryPath, targetPath, null, true);
                    }
                    else
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
            durableEvents = new List<LightweightEventRecord>(activeEvents);
            durableCustomMutations =
                new List<LightweightCustomMutationRecord>(activeCustomMutations);
            durableCheckpoints =
                new List<LightweightCheckpointRecord>(activeCheckpoints);
        }

        private void BlockPersistenceForCurrentScopeLocked(string reason)
        {
            blockedPersistencePath =
                currentSidecarPath ?? string.Empty;
            blockedPersistenceReason =
                string.IsNullOrEmpty(reason)
                    ? "The existing IM Data Core sidecar is protected from overwrite."
                    : reason;
        }

        private bool IsPersistenceBlockedForPathLocked(string path)
        {
            return !string.IsNullOrEmpty(blockedPersistencePath) &&
                string.Equals(
                    Path.GetFullPath(path ?? string.Empty),
                    Path.GetFullPath(blockedPersistencePath),
                    StringComparison.OrdinalIgnoreCase);
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
            customUsageByNamespace.Clear();
            activeMutationSequences.Clear();
            customEventIdempotencyKeys.Clear();
            timelineEventsByIdolId.Clear();
            globalTimelineEvents.Clear();
            moneyTransactionsByDateKey.Clear();
            moneyLedgerCoverageStartEvent = null;
            currentSidecarPath = string.Empty;
            currentRelativeSavePath = string.Empty;
            blockedPersistencePath = string.Empty;
            blockedPersistenceReason = string.Empty;
            latestCommittedPersistenceGenerationByPath.Clear();
            nextPersistenceGeneration = 0L;
            lastCommittedPersistenceGeneration = 0L;
            recoveredFromBackup = false;
            loadedExistingSidecarDocument = false;
            lastIssuedSequence = 0L;
        }

        private static bool IsMoneyLedgerInternalEvent(
            LightweightEventRecord record)
        {
            return record != null &&
                string.IsNullOrEmpty(record.NamespaceIdentifier) &&
                (string.Equals(
                    record.EventType,
                    MoneyLedgerConstants.EventTypeTransaction,
                    StringComparison.Ordinal) ||
                 string.Equals(
                    record.EventType,
                    MoneyLedgerConstants.EventTypeCoverageStarted,
                    StringComparison.Ordinal));
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
                EventId = record.Sequence,
                GameDateKey = record.GameDateKey,
                GameDateTime = record.GameDateTime ?? string.Empty,
                IdolId = record.IdolId,
                EntityKind = record.EntityKind ?? string.Empty,
                EntityId = record.EntityId ?? string.Empty,
                EventType = record.EventType ?? string.Empty,
                SourcePatch = record.SourcePatch ?? string.Empty,
                PayloadJson = record.PayloadJson ?? CoreConstants.EmptyJsonObject,
                NamespaceId = record.NamespaceIdentifier ?? string.Empty,
                IdempotencyKey = record.IdempotencyKey ?? string.Empty
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

        private NamespaceUsage GetNamespaceUsageLocked(
            string namespaceIdentifier)
        {
            string normalizedNamespace =
                namespaceIdentifier ?? string.Empty;

            NamespaceUsage usage;
            if (!customUsageByNamespace.TryGetValue(
                    normalizedNamespace,
                    out usage))
            {
                usage = new NamespaceUsage();
            }

            return usage;
        }

        private NamespaceUsage GetOrCreateNamespaceUsageLocked(
            string namespaceIdentifier)
        {
            string normalizedNamespace =
                namespaceIdentifier ?? string.Empty;

            NamespaceUsage usage;
            if (!customUsageByNamespace.TryGetValue(
                    normalizedNamespace,
                    out usage))
            {
                usage = new NamespaceUsage();
                customUsageByNamespace[normalizedNamespace] = usage;
            }

            return usage;
        }

        private void ApplyMaterializedCustomSetLocked(
            string namespaceIdentifier,
            string dataKey,
            string valueJson)
        {
            string normalizedNamespace =
                namespaceIdentifier ?? string.Empty;
            string normalizedDataKey =
                dataKey ?? string.Empty;
            string normalizedValue =
                valueJson ?? string.Empty;
            string compositeKey =
                BuildCustomDataCompositeKey(
                    normalizedNamespace,
                    normalizedDataKey);

            MaterializedCustomValue existing;
            NamespaceUsage usage =
                GetOrCreateNamespaceUsageLocked(
                    normalizedNamespace);

            if (customValues.TryGetValue(
                    compositeKey,
                    out existing) &&
                existing != null)
            {
                usage.TotalValueLength -=
                    existing.ValueJson == null
                        ? 0
                        : existing.ValueJson.Length;
            }
            else
            {
                usage.KeyCount++;
            }

            usage.TotalValueLength +=
                normalizedValue.Length;

            customValues[compositeKey] =
                new MaterializedCustomValue
                {
                    NamespaceIdentifier =
                        normalizedNamespace,
                    DataKey =
                        normalizedDataKey,
                    ValueJson =
                        normalizedValue
                };
        }

        private void ApplyMaterializedCustomRemoveLocked(
            string namespaceIdentifier,
            string dataKey)
        {
            string normalizedNamespace =
                namespaceIdentifier ?? string.Empty;
            string compositeKey =
                BuildCustomDataCompositeKey(
                    normalizedNamespace,
                    dataKey);

            MaterializedCustomValue existing;
            if (!customValues.TryGetValue(
                    compositeKey,
                    out existing) ||
                existing == null)
            {
                return;
            }

            NamespaceUsage usage =
                GetOrCreateNamespaceUsageLocked(
                    normalizedNamespace);

            usage.KeyCount =
                Math.Max(0, usage.KeyCount - 1);
            usage.TotalValueLength =
                Math.Max(
                    0,
                    usage.TotalValueLength -
                    (existing.ValueJson == null
                        ? 0
                        : existing.ValueJson.Length));

            customValues.Remove(compositeKey);

            if (usage.KeyCount == 0 &&
                usage.TotalValueLength == 0)
            {
                customUsageByNamespace.Remove(
                    normalizedNamespace);
            }
        }

        private static int CompareEventsDescending(
            LightweightEventRecord left,
            LightweightEventRecord right)
        {
            int dateComparison = right.GameDateKey.CompareTo(left.GameDateKey);
            return dateComparison != 0
                ? dateComparison
                : right.Sequence.CompareTo(left.Sequence);
        }

        private static int CompareEventsAscending(
            LightweightEventRecord left,
            LightweightEventRecord right)
        {
            int dateComparison = left.GameDateKey.CompareTo(right.GameDateKey);
            return dateComparison != 0
                ? dateComparison
                : left.Sequence.CompareTo(right.Sequence);
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
                GameDateKey = source.GameDateKey,
                GameDateTime = source.GameDateTime ?? string.Empty,
                IdolId = source.IdolId,
                EntityKind = source.EntityKind ?? string.Empty,
                EntityId = source.EntityId ?? string.Empty,
                EventType = source.EventType ?? string.Empty,
                SourcePatch = source.SourcePatch ?? string.Empty,
                NamespaceIdentifier = source.NamespaceIdentifier ?? string.Empty,
                IdempotencyKey = source.IdempotencyKey ?? string.Empty,
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
