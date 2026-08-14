using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

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
                    CorePaths.PathComparison) &&
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
        internal long StateRevision;
        internal bool IsIncremental;
        internal int BaseEventCount;
        internal int BaseCustomMutationCount;
        internal int BaseCheckpointCount;
        internal int TotalEventCount;
        internal int TotalCustomMutationCount;
        internal int TotalCheckpointCount;
        // Full snapshots contain complete lists. Incremental snapshots contain only
        // the immutable suffix beyond the committed base counts above.
        internal LightweightSidecarDocument Document;
    }

    internal sealed class LightweightLoadedPersistenceInfo
    {
        internal string BaseFileHash = string.Empty;
        internal long BaseFileBytes;
        internal long JournalBytes;
        internal int JournalEntryCount;
        internal bool ForceFullSnapshot;
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
        // Pre-transformed v3 storage representation. This is deliberately not a
        // public sidecar field; the manual codec writes it as the structural Payload
        // value. Records are immutable after insertion, so the expensive transform
        // only needs to happen once.
        internal string StoragePayloadJson = string.Empty;
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
        internal string StorageValueJson = string.Empty;
    }

    /// <summary>
    /// The sole normal-runtime persistence implementation for IM Data Core 3.3.
    /// Mutations update memory only; callers explicitly persist at vanilla save
    /// boundaries or through TryFlushNow.
    /// </summary>
    internal sealed class LightweightCoreStorageEngine : IDisposable
    {
        internal const string SidecarFormatName = "IMDataCore.LightweightSidecar";
        internal const int SidecarFormatVersion = 3;
        internal const string CustomOperationSet = "SET";
        internal const string CustomOperationRemove = "REMOVE";
        internal const string JournalFormatName = "IMDataCore.LightweightJournal";
        internal const int LegacyJournalFormatVersion = 1;
        internal const int JournalFormatVersion = 2;
        private const long MinimumJournalCompactionBytes = 1024L * 1024L;
        private const long MaximumJournalCompactionBytes = 16L * 1024L * 1024L;
        private const int MaximumJournalEntriesBeforeCompaction = 256;

        private sealed class HashingReadStream : Stream
        {
            private readonly Stream inner;
            private readonly HashAlgorithm hash;
            private bool finalized;

            internal HashingReadStream(Stream innerStream)
            {
                inner = innerStream ?? throw new ArgumentNullException("innerStream");
                hash = SHA256.Create();
            }

            internal string GetHashHex()
            {
                FinalizeHash();
                return ToLowerHex(hash.Hash);
            }

            private void FinalizeHash()
            {
                if (!finalized)
                {
                    hash.TransformFinalBlock(new byte[0], 0, 0);
                    finalized = true;
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int read = inner.Read(buffer, offset, count);
                if (read > 0)
                {
                    hash.TransformBlock(buffer, offset, read, buffer, offset);
                }
                else
                {
                    FinalizeHash();
                }
                return read;
            }

            public override bool CanRead { get { return inner.CanRead; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { return inner.Length; } }
            public override long Position
            {
                get { return inner.Position; }
                set { throw new NotSupportedException(); }
            }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }
            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }
            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    hash.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        private sealed class HashingWriteStream : Stream
        {
            private readonly Stream inner;
            private readonly HashAlgorithm hash;
            private bool finalized;

            internal HashingWriteStream(Stream innerStream)
            {
                inner = innerStream ?? throw new ArgumentNullException("innerStream");
                hash = SHA256.Create();
            }

            internal string CompleteAndGetHashHex()
            {
                if (!finalized)
                {
                    hash.TransformFinalBlock(new byte[0], 0, 0);
                    finalized = true;
                }
                return ToLowerHex(hash.Hash);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (finalized)
                {
                    throw new InvalidOperationException(
                        "The hashing write stream has already been finalized.");
                }
                if (count > 0)
                {
                    hash.TransformBlock(buffer, offset, count, buffer, offset);
                    inner.Write(buffer, offset, count);
                }
            }

            public override bool CanRead { get { return false; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return inner.CanWrite; } }
            public override long Length { get { return inner.Length; } }
            public override long Position
            {
                get { return inner.Position; }
                set { throw new NotSupportedException(); }
            }
            public override void Flush() { inner.Flush(); }
            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }
            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    hash.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        private static string ToLowerHex(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }
            StringBuilder builder = new StringBuilder(bytes.Length * 2);
            for (int index = 0; index < bytes.Length; index++)
            {
                builder.Append(bytes[index].ToString(
                    "x2",
                    CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }

        private sealed class CommittedPathState
        {
            internal string BaseFileHash = string.Empty;
            internal long BaseFileBytes;
            internal long JournalBytes;
            internal int JournalEntryCount;
            internal int EventCount;
            internal int CustomMutationCount;
            internal int CheckpointCount;
            internal long LastIssuedSequence;
            internal long StateRevision;
        }

        private sealed class TourRuntimeState
        {
            internal long Sequence;
            internal List<int> ParticipantIdolIdentifiers = new List<int>();
            internal string StartDate = string.Empty;
        }

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
        private readonly Dictionary<string, long> latestCommittedPersistenceGenerationByPath =
            new Dictionary<string, long>(CorePaths.PathComparer);
        private readonly Dictionary<string, object> persistenceIoLocksByPath =
            new Dictionary<string, object>(CorePaths.PathComparer);
        private readonly Dictionary<string, CommittedPathState> committedPathStates =
            new Dictionary<string, CommittedPathState>(CorePaths.PathComparer);
        private readonly Dictionary<string, List<LightweightCheckpointRecord>>
            activeCheckpointsByRelativePath =
                new Dictionary<string, List<LightweightCheckpointRecord>>(
                    CorePaths.PathComparer);
        private readonly Dictionary<CheckpointIdentity, LightweightCheckpointRecord>
            activeCheckpointsByIdentity =
                new Dictionary<CheckpointIdentity, LightweightCheckpointRecord>();
        private readonly Dictionary<CheckpointIdentity, LightweightCheckpointRecord>
            durableCheckpointsByIdentity =
                new Dictionary<CheckpointIdentity, LightweightCheckpointRecord>();
        private readonly HashSet<string> backgroundCompactionsInFlight =
            new HashSet<string>(CorePaths.PathComparer);
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
        private readonly Dictionary<int, List<int>> latestSingleCastBySingleId =
            new Dictionary<int, List<int>>();
        private readonly Dictionary<int, TourRuntimeState> latestTourStateByTourId =
            new Dictionary<int, TourRuntimeState>();

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
        private string lastPersistenceMode = "none";
        private long lastBaseSnapshotBytes;
        private long lastJournalBytes;
        private int lastJournalEntryCount;
        private long activeStateRevision;
        private long maxActiveEventSequence;
        private long maxActiveCustomMutationSequence;
        private long maxActiveCheckpointSequence;
        private DateTime maxActiveEventGameDate = DateTime.MinValue;
        private DateTime maxActiveCustomMutationGameDate = DateTime.MinValue;
        private DateTime maxActiveCheckpointGameDate = DateTime.MinValue;
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
                LightweightLoadedPersistenceInfo primaryLoadInfo;
                string primaryError;
                if (TryLoadValidatedDocumentFromPathLocked(
                        currentSidecarPath,
                        out document,
                        out primaryLoadInfo,
                        out primaryError))
                {
                    bool loadRequiresFullSnapshot = LoadDocumentLocked(document);
                    primaryLoadInfo.ForceFullSnapshot |=
                        loadRequiresFullSnapshot;
                    RegisterLoadedPathStateLocked(
                        currentSidecarPath,
                        document,
                        primaryLoadInfo);
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
                    LightweightLoadedPersistenceInfo backupLoadInfo;
                    if (TryLoadValidatedDocumentFromPathLocked(
                            normalizedBackupPath,
                            out document,
                            out backupLoadInfo,
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
                    List<string> storagePayloads =
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
                        string storagePayload;
                        string jsonError;
                        if (!LightweightSidecarJson.TryNormalizeEventPayloadForStorage(
                                pending.PayloadJson ?? CoreConstants.EmptyJsonObject,
                                !string.IsNullOrEmpty(pending.NamespaceIdentifier),
                                out normalizedPayload,
                                out storagePayload,
                                out jsonError))
                        {
                            errorMessage =
                                "An event payload is not valid JSON: " + jsonError;
                            return false;
                        }

                        normalizedPayloads.Add(normalizedPayload);
                        storagePayloads.Add(storagePayload);
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
                            PayloadJson = normalizedPayloads[index],
                            StoragePayloadJson = storagePayloads[index]
                        };

                        activeEvents.Add(record);
                        activeMutationSequences.Add(record.Sequence);
                        IndexEventLocked(record);
                        UpdateEventWatermarkLocked(record);
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
            Func<long> sequenceFactory,
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

                    if (sequenceFactory == null)
                    {
                        errorMessage = "The IMDC sequence factory is missing.";
                        return false;
                    }

                    long sequence = sequenceFactory();
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
                            ValueJson = normalizedJson,
                            StorageValueJson = normalizedJson
                        };

                    activeCustomMutations.Add(mutation);
                    ApplyMaterializedCustomSetLocked(
                        mutation.NamespaceIdentifier,
                        mutation.DataKey,
                        mutation.ValueJson);
                    UpdateCustomMutationWatermarkLocked(mutation);
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
            Func<long> sequenceFactory,
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

                    if (sequenceFactory == null)
                    {
                        errorMessage = "The IMDC sequence factory is missing.";
                        return false;
                    }

                    long sequence = sequenceFactory();
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
                    UpdateCustomMutationWatermarkLocked(mutation);
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
            bool ignoredHasMore;
            return TryReadEventsForIdolPage(
                idolId,
                0L,
                maxCount,
                out events,
                out ignoredHasMore,
                out errorMessage);
        }

        /// <summary>
        /// Reads one newest-to-oldest page from the derived per-idol timeline.
        /// The cursor is an event sequence from the previous page. The underlying
        /// idol/global lists are binary-searched by the cursor's timeline sort key,
        /// so walking a long history stays O(pageCount * pageSize + pageCount * log N)
        /// rather than rescanning from the newest event for every page.
        /// </summary>
        internal bool TryReadEventsForIdolPage(
            int idolId,
            long beforeEventIdExclusive,
            int maxCount,
            out List<IMDataCoreEvent> events,
            out bool hasMore,
            out string errorMessage)
        {
            events = new List<IMDataCoreEvent>();
            hasMore = false;
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

                    LightweightEventRecord cursor = null;
                    if (beforeEventIdExclusive > 0L)
                    {
                        cursor = FindActiveEventBySequenceLocked(
                            beforeEventIdExclusive);
                        if (cursor == null)
                        {
                            errorMessage =
                                "The requested timeline page cursor is no longer present in the active branch.";
                            return false;
                        }
                    }

                    List<LightweightEventRecord> idolEvents;
                    timelineEventsByIdolId.TryGetValue(idolId, out idolEvents);

                    int idolIndex = FindLastEventBeforeCursor(
                        idolEvents,
                        cursor);
                    int globalIndex = FindLastEventBeforeCursor(
                        globalTimelineEvents,
                        cursor);

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

                    hasMore = idolIndex >= 0 || globalIndex >= 0;
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

        private LightweightEventRecord FindActiveEventBySequenceLocked(
            long sequence)
        {
            int low = 0;
            int high = activeEvents.Count - 1;
            while (low <= high)
            {
                int middle = low + ((high - low) / 2);
                LightweightEventRecord candidate = activeEvents[middle];
                long candidateSequence = candidate != null
                    ? candidate.Sequence
                    : long.MinValue;
                if (candidateSequence == sequence)
                {
                    return candidate;
                }

                if (candidateSequence < sequence)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return null;
        }

        private static int FindLastEventBeforeCursor(
            List<LightweightEventRecord> rows,
            LightweightEventRecord cursor)
        {
            if (rows == null || rows.Count == 0)
            {
                return -1;
            }

            if (cursor == null)
            {
                return rows.Count - 1;
            }

            int low = 0;
            int high = rows.Count;
            while (low < high)
            {
                int middle = low + ((high - low) / 2);
                if (CompareEventsAscending(rows[middle], cursor) < 0)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            return low - 1;
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
                List<int> cached;
                if (!latestSingleCastBySingleId.TryGetValue(singleId, out cached))
                {
                    return false;
                }

                slotIdolIdentifiers = new List<int>(cached);
                return true;
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
                TourRuntimeState cached;
                if (!latestTourStateByTourId.TryGetValue(tourId, out cached) ||
                    cached == null)
                {
                    return false;
                }

                participantIdolIdentifiers =
                    new List<int>(cached.ParticipantIdolIdentifiers);
                startDate = cached.StartDate ?? string.Empty;
                return true;
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
                    LightweightCheckpointRecord matchingCheckpoint;
                    if (!durableCheckpointsByIdentity.TryGetValue(
                            CheckpointIdentity.From(stamp),
                            out matchingCheckpoint) ||
                        matchingCheckpoint == null)
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

                    // The normal load case already has exactly the tip state in
                    // active lists. Avoid rebuilding/sorting every history list
                    // unless this checkpoint actually selects an older branch.
                    if (!ActiveStateFitsCheckpointLocked(
                            matchingCheckpoint.Sequence,
                            checkpointGameDate))
                    {
                        ActivateThroughSequenceLocked(
                            matchingCheckpoint.Sequence,
                            checkpointGameDate);
                    }

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

                    CheckpointIdentity checkpointIdentity =
                        CheckpointIdentity.From(stamp);
                    LightweightCheckpointRecord existingCheckpoint;
                    if (activeCheckpointsByIdentity.TryGetValue(
                            checkpointIdentity,
                            out existingCheckpoint) &&
                        existingCheckpoint != null)
                    {
                        activeCheckpoints.Remove(existingCheckpoint);
                        activeStateRevision++;
                        RecomputeCheckpointWatermarkLocked();
                    }

                    LightweightCheckpointRecord newCheckpoint =
                        new LightweightCheckpointRecord
                        {
                            RelativeSavePath = stamp.RelativeSavePath,
                            LastSave = stamp.LastSave,
                            PlaytimeSeconds = stamp.PlaytimeSeconds,
                            GameDateTime = stamp.GameDateTime,
                            Sequence = sequence
                        };
                    activeCheckpoints.Add(newCheckpoint);
                    IndexCheckpointByPathLocked(newCheckpoint);
                    UpdateCheckpointWatermarkLocked(
                        newCheckpoint,
                        checkpointGameDate);
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
                    bool preserveExistingBackup = recoveredFromBackup &&
                        string.Equals(
                            normalizedSidecarPath,
                            Path.GetFullPath(currentSidecarPath ?? string.Empty),
                            CorePaths.PathComparison);
                    snapshot = BuildPersistenceSnapshotLocked(
                        normalizedSidecarPath,
                        saveScope.RelativeSavePath,
                        generation,
                        preserveExistingBackup);
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
                    snapshot = BuildPersistenceSnapshotLocked(
                        normalizedSidecarPath,
                        currentRelativeSavePath,
                        generation,
                        recoveredFromBackup);
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
            bool isCurrent;
            return TryPersistSnapshot(
                snapshot,
                out isCurrent,
                out errorMessage);
        }

        internal bool TryPersistSnapshot(
            LightweightPersistenceSnapshot snapshot,
            out bool isCurrent,
            out string errorMessage)
        {
            isCurrent = false;
            errorMessage = string.Empty;
            if (snapshot == null || snapshot.Document == null ||
                string.IsNullOrEmpty(snapshot.TargetPath))
            {
                errorMessage = "The IMDC persistence snapshot is invalid.";
                return false;
            }

            object pathIoLock = GetPersistenceIoLock(snapshot.TargetPath);
            lock (pathIoLock)
            {
                CommittedPathState baselineState = null;
                bool canAppendJournal = false;
                bool noPhysicalWriteRequired = false;
                int eventDeltaStartIndex = 0;
                int customMutationDeltaStartIndex = 0;
                int checkpointDeltaStartIndex = 0;
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
                        // Report success to the caller, but explicitly mark this
                        // snapshot as superseded so controller scope cannot regress.
                        return true;
                    }

                    committedPathStates.TryGetValue(
                        snapshot.TargetPath,
                        out baselineState);

                    if (snapshot.IsIncremental)
                    {
                        if (baselineState == null ||
                            snapshot.PreserveExistingBackup ||
                            !File.Exists(snapshot.TargetPath) ||
                            baselineState.StateRevision != snapshot.StateRevision ||
                            string.IsNullOrEmpty(baselineState.BaseFileHash) ||
                            baselineState.EventCount < snapshot.BaseEventCount ||
                            baselineState.EventCount > snapshot.TotalEventCount ||
                            baselineState.CustomMutationCount <
                                snapshot.BaseCustomMutationCount ||
                            baselineState.CustomMutationCount >
                                snapshot.TotalCustomMutationCount ||
                            baselineState.CheckpointCount < snapshot.BaseCheckpointCount ||
                            baselineState.CheckpointCount > snapshot.TotalCheckpointCount ||
                            baselineState.LastIssuedSequence >
                                snapshot.Document.LastIssuedSequence)
                        {
                            errorMessage =
                                "The IMDC incremental snapshot no longer has a compatible durable baseline.";
                            return false;
                        }

                        eventDeltaStartIndex =
                            baselineState.EventCount - snapshot.BaseEventCount;
                        customMutationDeltaStartIndex =
                            baselineState.CustomMutationCount -
                            snapshot.BaseCustomMutationCount;
                        checkpointDeltaStartIndex =
                            baselineState.CheckpointCount - snapshot.BaseCheckpointCount;

                        if (eventDeltaStartIndex > snapshot.Document.Events.Count ||
                            customMutationDeltaStartIndex >
                                snapshot.Document.CustomMutations.Count ||
                            checkpointDeltaStartIndex >
                                snapshot.Document.Checkpoints.Count)
                        {
                            errorMessage =
                                "The IMDC incremental snapshot delta is inconsistent with its durable baseline.";
                            return false;
                        }

                        noPhysicalWriteRequired =
                            baselineState.EventCount == snapshot.TotalEventCount &&
                            baselineState.CustomMutationCount ==
                                snapshot.TotalCustomMutationCount &&
                            baselineState.CheckpointCount ==
                                snapshot.TotalCheckpointCount &&
                            baselineState.LastIssuedSequence ==
                                snapshot.Document.LastIssuedSequence;

                        canAppendJournal = !noPhysicalWriteRequired;
                    }
                }

                Stopwatch stopwatch = Stopwatch.StartNew();
                string persistenceMode;
                long baseBytes = baselineState != null
                    ? baselineState.BaseFileBytes
                    : 0L;
                long journalBytes = baselineState != null
                    ? baselineState.JournalBytes
                    : 0L;
                string baseFileHash = baselineState != null
                    ? baselineState.BaseFileHash
                    : string.Empty;
                int journalEntryCount = baselineState != null
                    ? baselineState.JournalEntryCount
                    : 0;

                if (noPhysicalWriteRequired)
                {
                    persistenceMode = "noop";
                }
                else if (canAppendJournal)
                {
                    long appendedBytes;
                    string journalError;
                    if (TryAppendJournalEntry(
                            snapshot.TargetPath,
                            baselineState,
                            snapshot.Document,
                            checkpointDeltaStartIndex,
                            eventDeltaStartIndex,
                            customMutationDeltaStartIndex,
                            out appendedBytes,
                            out journalBytes,
                            out journalError))
                    {
                        journalEntryCount = baselineState.JournalEntryCount + 1;
                        persistenceMode = "journal";
                    }
                    else
                    {
                        // A stale/torn/mismatched journal must never strand future
                        // saves. Compact the complete logical state into a fresh
                        // atomic base snapshot and remove the problematic journal.
                        CoreLog.Warn(
                            "IM Data Core journal append was unavailable; " +
                            "falling back to a compact snapshot: " + journalError);
                        if (!TryWriteFullSnapshotFile(
                                snapshot,
                                baselineState,
                                checkpointDeltaStartIndex,
                                eventDeltaStartIndex,
                                customMutationDeltaStartIndex,
                                out baseBytes,
                                out baseFileHash,
                                out errorMessage))
                        {
                            return false;
                        }

                        journalBytes = 0L;
                        journalEntryCount = 0;
                        persistenceMode = "snapshot_fallback";
                    }
                }
                else
                {
                    if (!TryWriteFullSnapshotFile(
                            snapshot,
                            baselineState,
                            0,
                            0,
                            0,
                            out baseBytes,
                            out baseFileHash,
                            out errorMessage))
                    {
                        return false;
                    }

                    journalBytes = 0L;
                    journalEntryCount = 0;
                    persistenceMode = "snapshot";
                }
                stopwatch.Stop();

                bool scheduleBackgroundCompaction = false;
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

                    CommittedPathState committedState =
                        new CommittedPathState
                        {
                            BaseFileHash = baseFileHash ?? string.Empty,
                            BaseFileBytes = baseBytes,
                            JournalBytes = journalBytes,
                            JournalEntryCount = journalEntryCount,
                            EventCount = snapshot.TotalEventCount,
                            CustomMutationCount =
                                snapshot.TotalCustomMutationCount,
                            CheckpointCount = snapshot.TotalCheckpointCount,
                            LastIssuedSequence = snapshot.Document.LastIssuedSequence,
                            StateRevision = snapshot.StateRevision
                        };
                    committedPathStates[snapshot.TargetPath] = committedState;
                    scheduleBackgroundCompaction =
                        string.Equals(
                            persistenceMode,
                            "journal",
                            StringComparison.Ordinal) &&
                        ShouldCompactJournal(committedState);

                    lastPersistenceMode = persistenceMode;
                    lastBaseSnapshotBytes = baseBytes;
                    lastJournalBytes = journalBytes;
                    lastJournalEntryCount = journalEntryCount;

                    isCurrent = !disposed &&
                        snapshot.Generation == nextPersistenceGeneration;

                    if (!disposed &&
                        snapshot.Generation > lastCommittedPersistenceGeneration)
                    {
                        lastCommittedPersistenceGeneration = snapshot.Generation;
                    }

                    // Only the newest prepared generation is allowed to rebind the
                    // engine's global active/durable scope. An older write to another
                    // path can still become durable in its own CommittedPathState, but
                    // it must not temporarily drag controller-adjacent storage state
                    // backwards while a newer generation is waiting on I/O.
                    if (!disposed && isCurrent)
                    {
                        currentSidecarPath = snapshot.TargetPath;
                        currentRelativeSavePath = snapshot.RelativeSavePath;
                        blockedPersistencePath = string.Empty;
                        blockedPersistenceReason = string.Empty;
                        recoveredFromBackup = false;
                        if (!snapshot.IsIncremental)
                        {
                            durableEvents = new List<LightweightEventRecord>(
                                snapshot.Document.Events);
                            durableCustomMutations =
                                new List<LightweightCustomMutationRecord>(
                                    snapshot.Document.CustomMutations);
                            durableCheckpoints =
                                new List<LightweightCheckpointRecord>(
                                    snapshot.Document.Checkpoints);
                            RebuildDurableCheckpointIdentityIndexLocked();

                            // A full Save As snapshot filters checkpoints to its target.
                            activeCheckpoints =
                                new List<LightweightCheckpointRecord>(
                                    snapshot.Document.Checkpoints);
                            RecomputeCheckpointWatermarkLocked();
                        }
                    }
                }

                if (scheduleBackgroundCompaction)
                {
                    QueueBackgroundCompaction(
                        snapshot.TargetPath,
                        snapshot.RelativeSavePath,
                        snapshot.Generation);
                }

                CoreLog.Info(
                    "IM Data Core persisted sidecar: mode=" + persistenceMode +
                    ", events=" +
                    snapshot.TotalEventCount.ToString(CultureInfo.InvariantCulture) +
                    ", custom_mutations=" +
                    snapshot.TotalCustomMutationCount.ToString(CultureInfo.InvariantCulture) +
                    ", checkpoints=" +
                    snapshot.TotalCheckpointCount.ToString(CultureInfo.InvariantCulture) +
                    ", base_bytes=" + baseBytes.ToString(CultureInfo.InvariantCulture) +
                    ", journal_bytes=" +
                    journalBytes.ToString(CultureInfo.InvariantCulture) +
                    ", elapsed_ms=" +
                    stopwatch.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) +
                    ".");
                return true;
            }
        }

        private static bool ShouldCompactJournal(CommittedPathState state)
        {
            if (state == null)
            {
                return false;
            }

            long proportionalThreshold = state.BaseFileBytes > 0L
                ? state.BaseFileBytes / 4L
                : MinimumJournalCompactionBytes;
            long byteThreshold = Math.Max(
                MinimumJournalCompactionBytes,
                Math.Min(
                    MaximumJournalCompactionBytes,
                    proportionalThreshold));
            return state.JournalBytes >= byteThreshold ||
                state.JournalEntryCount >= MaximumJournalEntriesBeforeCompaction;
        }

        private void QueueBackgroundCompaction(
            string targetPath,
            string relativeSavePath,
            long generation)
        {
            lock (storageLock)
            {
                if (disposed ||
                    !backgroundCompactionsInFlight.Add(targetPath))
                {
                    return;
                }
            }

            ThreadPool.QueueUserWorkItem(
                delegate
                {
                    try
                    {
                        RunBackgroundCompaction(
                            targetPath,
                            relativeSavePath,
                            generation);
                    }
                    catch (Exception exception)
                    {
                        CoreLog.Warn(
                            "IM Data Core background compaction failed: " +
                            exception.Message);
                    }
                    finally
                    {
                        lock (storageLock)
                        {
                            backgroundCompactionsInFlight.Remove(targetPath);
                        }
                    }
                });
        }

        private void RunBackgroundCompaction(
            string targetPath,
            string relativeSavePath,
            long generation)
        {
            object pathIoLock = GetPersistenceIoLock(targetPath);
            lock (pathIoLock)
            {
                CommittedPathState expectedState;
                lock (storageLock)
                {
                    long latestGeneration;
                    if (disposed ||
                        !latestCommittedPersistenceGenerationByPath.TryGetValue(
                            targetPath,
                            out latestGeneration) ||
                        latestGeneration != generation ||
                        !committedPathStates.TryGetValue(
                            targetPath,
                            out expectedState) ||
                        !ShouldCompactJournal(expectedState))
                    {
                        return;
                    }
                }

                LightweightSidecarDocument document;
                LightweightLoadedPersistenceInfo loadedInfo;
                string loadError;
                if (!TryLoadValidatedDocumentFromPathLocked(
                        targetPath,
                        relativeSavePath,
                        out document,
                        out loadedInfo,
                        out loadError))
                {
                    CoreLog.Warn(
                        "IM Data Core skipped background compaction because the " +
                        "durable journal could not be materialized: " + loadError);
                    return;
                }

                if (!string.Equals(
                        loadedInfo.BaseFileHash,
                        expectedState.BaseFileHash,
                        StringComparison.Ordinal) ||
                    document.Events.Count != expectedState.EventCount ||
                    document.CustomMutations.Count !=
                        expectedState.CustomMutationCount ||
                    document.Checkpoints.Count != expectedState.CheckpointCount ||
                    document.LastIssuedSequence != expectedState.LastIssuedSequence)
                {
                    return;
                }

                LightweightPersistenceSnapshot compactSnapshot =
                    new LightweightPersistenceSnapshot
                    {
                        TargetPath = targetPath,
                        RelativeSavePath = relativeSavePath,
                        Generation = generation,
                        PreserveExistingBackup = false,
                        StateRevision = expectedState.StateRevision,
                        IsIncremental = false,
                        BaseEventCount = 0,
                        BaseCustomMutationCount = 0,
                        BaseCheckpointCount = 0,
                        TotalEventCount = document.Events.Count,
                        TotalCustomMutationCount = document.CustomMutations.Count,
                        TotalCheckpointCount = document.Checkpoints.Count,
                        Document = document
                    };

                long baseBytes;
                string baseHash;
                string writeError;
                if (!TryWriteFullSnapshotFile(
                        compactSnapshot,
                        null,
                        0,
                        0,
                        0,
                        out baseBytes,
                        out baseHash,
                        out writeError))
                {
                    CoreLog.Warn(
                        "IM Data Core background compaction could not commit: " +
                        writeError);
                    return;
                }

                lock (storageLock)
                {
                    long latestGeneration;
                    CommittedPathState currentState;
                    if (disposed ||
                        !latestCommittedPersistenceGenerationByPath.TryGetValue(
                            targetPath,
                            out latestGeneration) ||
                        latestGeneration != generation ||
                        !committedPathStates.TryGetValue(
                            targetPath,
                            out currentState) ||
                        currentState.EventCount != expectedState.EventCount ||
                        currentState.CustomMutationCount !=
                            expectedState.CustomMutationCount ||
                        currentState.CheckpointCount !=
                            expectedState.CheckpointCount ||
                        currentState.LastIssuedSequence !=
                            expectedState.LastIssuedSequence ||
                        currentState.StateRevision != expectedState.StateRevision)
                    {
                        return;
                    }

                    currentState.BaseFileHash = baseHash ?? string.Empty;
                    currentState.BaseFileBytes = baseBytes;
                    currentState.JournalBytes = 0L;
                    currentState.JournalEntryCount = 0;
                    lastPersistenceMode = "background_compaction";
                    lastBaseSnapshotBytes = baseBytes;
                    lastJournalBytes = 0L;
                    lastJournalEntryCount = 0;
                }

                CoreLog.Info(
                    "IM Data Core compacted its journal in the background: base_bytes=" +
                    baseBytes.ToString(CultureInfo.InvariantCulture) + ".");
            }
        }

        private bool TryMaterializeIncrementalSnapshot(
            LightweightPersistenceSnapshot snapshot,
            CommittedPathState baselineState,
            int checkpointDeltaStartIndex,
            int eventDeltaStartIndex,
            int customMutationDeltaStartIndex,
            out LightweightSidecarDocument document,
            out string errorMessage)
        {
            document = null;
            errorMessage = string.Empty;
            if (snapshot == null || !snapshot.IsIncremental ||
                snapshot.Document == null || baselineState == null)
            {
                errorMessage =
                    "The IMDC incremental snapshot cannot be materialized without its durable baseline.";
                return false;
            }

            LightweightLoadedPersistenceInfo loadedInfo;
            if (!TryLoadValidatedDocumentFromPathLocked(
                    snapshot.TargetPath,
                    snapshot.RelativeSavePath,
                    out document,
                    out loadedInfo,
                    out errorMessage))
            {
                return false;
            }

            bool durableStateIsBaseline =
                document.Events.Count == baselineState.EventCount &&
                document.CustomMutations.Count ==
                    baselineState.CustomMutationCount &&
                document.Checkpoints.Count == baselineState.CheckpointCount &&
                document.LastIssuedSequence == baselineState.LastIssuedSequence;
            bool durableStateAlreadyMatchesSnapshot =
                document.Events.Count == snapshot.TotalEventCount &&
                document.CustomMutations.Count ==
                    snapshot.TotalCustomMutationCount &&
                document.Checkpoints.Count == snapshot.TotalCheckpointCount &&
                document.LastIssuedSequence == snapshot.Document.LastIssuedSequence;

            if (durableStateAlreadyMatchesSnapshot)
            {
                // A journal append can become fully readable before Flush(true)
                // reports an I/O failure. Treat that as an already-applied delta
                // for fallback compaction instead of appending the suffix twice.
                return TryValidateDocumentLocked(
                    document,
                    snapshot.RelativeSavePath,
                    out errorMessage);
            }

            if (!durableStateIsBaseline)
            {
                errorMessage =
                    "The durable IMDC journal state changed while materializing an incremental snapshot.";
                document = null;
                return false;
            }

            try
            {
                AppendSuffix(
                    document.Checkpoints,
                    snapshot.Document.Checkpoints,
                    checkpointDeltaStartIndex);
                AppendSuffix(
                    document.Events,
                    snapshot.Document.Events,
                    eventDeltaStartIndex);
                AppendSuffix(
                    document.CustomMutations,
                    snapshot.Document.CustomMutations,
                    customMutationDeltaStartIndex);
                document.LastIssuedSequence = snapshot.Document.LastIssuedSequence;

                if (document.Events.Count != snapshot.TotalEventCount ||
                    document.CustomMutations.Count !=
                        snapshot.TotalCustomMutationCount ||
                    document.Checkpoints.Count != snapshot.TotalCheckpointCount ||
                    !TryValidateDocumentLocked(
                        document,
                        snapshot.RelativeSavePath,
                        out errorMessage))
                {
                    if (string.IsNullOrEmpty(errorMessage))
                    {
                        errorMessage =
                            "The materialized IMDC snapshot does not match its frozen totals.";
                    }
                    document = null;
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                errorMessage =
                    "Materializing the IMDC incremental snapshot failed: " +
                    exception.Message;
                document = null;
                return false;
            }
        }

        private static void AppendSuffix<T>(
            List<T> target,
            IReadOnlyList<T> source,
            int startIndex)
        {
            if (target == null || source == null ||
                startIndex < 0 || startIndex > source.Count)
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }

            for (int index = startIndex; index < source.Count; index++)
            {
                target.Add(source[index]);
            }
        }

        private bool TryWriteFullSnapshotFile(
            LightweightPersistenceSnapshot snapshot,
            CommittedPathState baselineState,
            int checkpointDeltaStartIndex,
            int eventDeltaStartIndex,
            int customMutationDeltaStartIndex,
            out long baseBytes,
            out string baseFileHash,
            out string errorMessage)
        {
            baseBytes = 0L;
            baseFileHash = string.Empty;
            errorMessage = string.Empty;
            if (snapshot == null || snapshot.Document == null)
            {
                errorMessage = "The IMDC full snapshot is invalid.";
                return false;
            }

            LightweightSidecarDocument documentToWrite = snapshot.Document;
            if (snapshot.IsIncremental &&
                !TryMaterializeIncrementalSnapshot(
                    snapshot,
                    baselineState,
                    checkpointDeltaStartIndex,
                    eventDeltaStartIndex,
                    customMutationDeltaStartIndex,
                    out documentToWrite,
                    out errorMessage))
            {
                return false;
            }

            bool targetExisted = File.Exists(snapshot.TargetPath);
            string currentJournalPath = snapshot.TargetPath + ".imdc.journal";
            bool currentJournalExisted = File.Exists(currentJournalPath);

            if (!TryWriteAtomically(
                    snapshot.TargetPath,
                    documentToWrite,
                    snapshot.PreserveExistingBackup,
                    out baseBytes,
                    out baseFileHash,
                    out errorMessage))
            {
                return false;
            }

            // File.Replace moved the previous base to .imdc.bak. Preserve the
            // journal tied to that base as well so backup recovery represents the
            // complete previous logical generation, not merely its compact base.
            if (targetExisted && !snapshot.PreserveExistingBackup)
            {
                string backupJournalPath =
                    snapshot.TargetPath + ".imdc.bak.imdc.journal";
                string backupJournalError;
                if (currentJournalExisted)
                {
                    if (!TryCopyContainedFileDurably(
                            currentJournalPath,
                            backupJournalPath,
                            out backupJournalError))
                    {
                        CoreLog.Warn(
                            "IM Data Core could not preserve the previous journal " +
                            "with its backup base: " + backupJournalError);
                    }
                }
                else if (File.Exists(backupJournalPath) &&
                    !CorePaths.TryDeleteContainedFile(
                        backupJournalPath,
                        out backupJournalError))
                {
                    CoreLog.Warn(
                        "IM Data Core could not remove a stale backup journal: " +
                        backupJournalError);
                }
            }

            string cleanupError;
            if (!TryDeleteJournal(snapshot.TargetPath, out cleanupError) &&
                !string.IsNullOrEmpty(cleanupError))
            {
                // The new base is authoritative. A journal tied to the previous
                // base hash is rejected on load, and a later append will compact
                // again rather than trusting it.
                CoreLog.Warn(
                    "IM Data Core could not remove a stale persistence journal: " +
                    cleanupError);
            }

            return true;
        }

        private bool TryAppendJournalEntry(
            string targetPath,
            CommittedPathState baselineState,
            LightweightSidecarDocument document,
            int checkpointStartIndex,
            int eventStartIndex,
            int customMutationStartIndex,
            out long appendedBytes,
            out long journalBytes,
            out string errorMessage)
        {
            appendedBytes = 0L;
            journalBytes = baselineState != null
                ? baselineState.JournalBytes
                : 0L;
            errorMessage = string.Empty;
            if (baselineState == null ||
                string.IsNullOrEmpty(baselineState.BaseFileHash))
            {
                errorMessage = "The IMDC journal baseline is unavailable.";
                return false;
            }

            string journalPath = targetPath + ".imdc.journal";
            string normalizedJournalPath;
            if (!CorePaths.TryValidateContainedMutationPath(
                    journalPath,
                    false,
                    out normalizedJournalPath,
                    out errorMessage))
            {
                return false;
            }

            bool createJournal = !File.Exists(normalizedJournalPath);
            if (createJournal &&
                (baselineState.JournalBytes != 0L ||
                 baselineState.JournalEntryCount != 0))
            {
                errorMessage =
                    "The durable IMDC journal disappeared after its state was committed.";
                return false;
            }
            if (!createJournal)
            {
                try
                {
                    long existingLength = new FileInfo(normalizedJournalPath).Length;
                    if (existingLength != baselineState.JournalBytes)
                    {
                        errorMessage =
                            "The durable IMDC journal length changed unexpectedly.";
                        return false;
                    }
                    if (existingLength <= 0L ||
                        !FileEndsWithNewline(normalizedJournalPath))
                    {
                        errorMessage =
                            "The existing IMDC journal has an incomplete tail.";
                        return false;
                    }

                    using (FileStream readStream = new FileStream(
                        normalizedJournalPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        4096,
                        FileOptions.SequentialScan))
                    using (StreamReader reader = new StreamReader(
                        readStream,
                        Encoding.UTF8,
                        true,
                        4096,
                        false))
                    {
                        string existingBaseHash;
                        int existingJournalVersion;
                        string headerError;
                        if (!LightweightSidecarJson.TryReadJournalHeader(
                                reader.ReadLine(),
                                out existingBaseHash,
                                out existingJournalVersion,
                                out headerError) ||
                            !string.Equals(
                                existingBaseHash,
                                baselineState.BaseFileHash,
                                StringComparison.Ordinal))
                        {
                            errorMessage =
                                "The existing IMDC journal does not match its base snapshot. " +
                                headerError;
                            return false;
                        }
                        if (existingJournalVersion != JournalFormatVersion)
                        {
                            errorMessage =
                                "The existing IMDC journal uses the legacy v1 entry format and must be compacted before appending.";
                            return false;
                        }
                    }
                }
                catch (Exception exception)
                {
                    errorMessage = exception.Message;
                    return false;
                }
            }

            long beforeLength = createJournal
                ? 0L
                : new FileInfo(normalizedJournalPath).Length;
            try
            {
                using (FileStream stream = new FileStream(
                    normalizedJournalPath,
                    createJournal ? FileMode.CreateNew : FileMode.Append,
                    FileAccess.Write,
                    FileShare.Read,
                    64 * 1024,
                    FileOptions.SequentialScan))
                using (StreamWriter writer = new StreamWriter(
                    stream,
                    new UTF8Encoding(false),
                    64 * 1024,
                    true))
                {
                    if (createJournal)
                    {
                        writer.Write(
                            LightweightSidecarJson.SerializeJournalHeader(
                                baselineState.BaseFileHash));
                        writer.Write('\n');
                    }

                    LightweightSidecarJson.SerializeJournalTransactionTo(
                        writer,
                        document,
                        checkpointStartIndex,
                        eventStartIndex,
                        customMutationStartIndex,
                        baselineState.CheckpointCount,
                        baselineState.EventCount,
                        baselineState.CustomMutationCount);
                    writer.Flush();
                    stream.Flush(true);
                }

                journalBytes = new FileInfo(normalizedJournalPath).Length;
                appendedBytes = Math.Max(0L, journalBytes - beforeLength);
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        private static bool TryCopyContainedFileDurably(
            string sourcePath,
            string destinationPath,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            string normalizedSource;
            string normalizedDestination;
            if (!CorePaths.TryValidateContainedMutationPath(
                    sourcePath,
                    false,
                    out normalizedSource,
                    out errorMessage) ||
                !CorePaths.TryValidateContainedMutationPath(
                    destinationPath,
                    false,
                    out normalizedDestination,
                    out errorMessage))
            {
                return false;
            }

            string temporaryPath = normalizedDestination + ".tmp." +
                Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            try
            {
                using (FileStream source = new FileStream(
                    normalizedSource,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    64 * 1024,
                    FileOptions.SequentialScan))
                using (FileStream destination = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    64 * 1024,
                    FileOptions.SequentialScan))
                {
                    source.CopyTo(destination, 64 * 1024);
                    destination.Flush(true);
                }

                if (File.Exists(normalizedDestination))
                {
                    File.Replace(
                        temporaryPath,
                        normalizedDestination,
                        null,
                        true);
                }
                else
                {
                    File.Move(temporaryPath, normalizedDestination);
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

        private static bool FileEndsWithNewline(string path)
        {
            using (FileStream stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.RandomAccess))
            {
                if (stream.Length <= 0L)
                {
                    return false;
                }

                stream.Seek(-1L, SeekOrigin.End);
                return stream.ReadByte() == '\n';
            }
        }

        private static bool TryDeleteJournal(
            string targetPath,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            string journalPath = targetPath + ".imdc.journal";
            if (!File.Exists(journalPath))
            {
                return true;
            }

            return CorePaths.TryDeleteContainedFile(
                journalPath,
                out errorMessage);
        }

        private object GetPersistenceIoLock(string targetPath)
        {
            lock (storageLock)
            {
                object pathLock;
                if (!persistenceIoLocksByPath.TryGetValue(
                        targetPath ?? string.Empty,
                        out pathLock))
                {
                    pathLock = new object();
                    persistenceIoLocksByPath[targetPath ?? string.Empty] = pathLock;
                }
                return pathLock;
            }
        }

        internal IMDataCorePersistenceDiagnostics GetPersistenceDiagnostics(
            int dirtyBufferedEventCount,
            string activeSavePath)
        {
            lock (storageLock)
            {
                CommittedPathState state = null;
                if (!string.IsNullOrEmpty(currentSidecarPath))
                {
                    committedPathStates.TryGetValue(currentSidecarPath, out state);
                }

                return new IMDataCorePersistenceDiagnostics
                {
                    PersistenceMode = lastPersistenceMode ?? "none",
                    ActiveSavePath = activeSavePath ?? string.Empty,
                    SidecarPath = currentSidecarPath ?? string.Empty,
                    IsPersistenceBlocked =
                        !string.IsNullOrEmpty(blockedPersistencePath),
                    BlockedReason = blockedPersistenceReason ?? string.Empty,
                    RecoveredFromBackup = recoveredFromBackup,
                    EventCount = activeEvents.Count,
                    CustomMutationCount = activeCustomMutations.Count,
                    CheckpointCount = activeCheckpoints.Count,
                    DirtyBufferedEventCount = Math.Max(0, dirtyBufferedEventCount),
                    LastIssuedSequence = lastIssuedSequence,
                    LastCommittedGeneration = lastCommittedPersistenceGeneration,
                    BaseSnapshotBytes = state != null
                        ? state.BaseFileBytes
                        : lastBaseSnapshotBytes,
                    JournalBytes = state != null
                        ? state.JournalBytes
                        : lastJournalBytes,
                    JournalEntryCount = state != null
                        ? state.JournalEntryCount
                        : lastJournalEntryCount
                };
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
            out LightweightLoadedPersistenceInfo persistenceInfo,
            out string errorMessage)
        {
            return TryLoadValidatedDocumentFromPathLocked(
                path,
                currentRelativeSavePath,
                out document,
                out persistenceInfo,
                out errorMessage);
        }

        private bool TryLoadValidatedDocumentFromPathLocked(
            string path,
            string expectedRelativeSavePath,
            out LightweightSidecarDocument document,
            out LightweightLoadedPersistenceInfo persistenceInfo,
            out string errorMessage)
        {
            document = null;
            persistenceInfo = new LightweightLoadedPersistenceInfo();
            errorMessage = string.Empty;
            try
            {
                using (FileStream stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    64 * 1024,
                    FileOptions.SequentialScan))
                using (HashingReadStream hashingStream =
                    new HashingReadStream(stream))
                using (StreamReader reader = new StreamReader(
                    hashingStream,
                    Encoding.UTF8,
                    true,
                    64 * 1024,
                    false))
                {
                    document = LightweightSidecarJson.DeserializeFrom(reader);
                    // DeserializeFrom stops at the end of the JSON document. Drain
                    // legal trailing whitespace so the fingerprint covers every byte.
                    reader.ReadToEnd();
                    persistenceInfo.BaseFileBytes = stream.Length;
                    persistenceInfo.BaseFileHash = hashingStream.GetHashHex();
                }

                if (!TryValidateDocumentLocked(
                        document,
                        expectedRelativeSavePath,
                        out errorMessage))
                {
                    document = null;
                    return false;
                }

                if (!TryReplayJournalLocked(
                        path,
                        persistenceInfo.BaseFileHash,
                        document,
                        persistenceInfo,
                        out errorMessage))
                {
                    document = null;
                    return false;
                }

                if (persistenceInfo.JournalEntryCount > 0 &&
                    !TryValidateDocumentLocked(
                        document,
                        expectedRelativeSavePath,
                        out errorMessage))
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

        private bool TryReplayJournalLocked(
            string basePath,
            string baseFileHash,
            LightweightSidecarDocument document,
            LightweightLoadedPersistenceInfo persistenceInfo,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            string journalPath = basePath + ".imdc.journal";
            string normalizedJournalPath;
            if (!CorePaths.TryValidateContainedMutationPath(
                    journalPath,
                    false,
                    out normalizedJournalPath,
                    out errorMessage))
            {
                return false;
            }

            if (!File.Exists(normalizedJournalPath))
            {
                return true;
            }

            long journalLength = new FileInfo(normalizedJournalPath).Length;
            persistenceInfo.JournalBytes = journalLength;
            if (journalLength == 0L)
            {
                persistenceInfo.ForceFullSnapshot = true;
                return true;
            }

            bool endsWithNewline = false;
            using (FileStream tailStream = new FileStream(
                normalizedJournalPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.RandomAccess))
            {
                tailStream.Seek(-1L, SeekOrigin.End);
                int lastByte = tailStream.ReadByte();
                endsWithNewline = lastByte == '\n';
            }

            using (FileStream stream = new FileStream(
                normalizedJournalPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                64 * 1024,
                FileOptions.SequentialScan))
            using (StreamReader reader = new StreamReader(
                stream,
                Encoding.UTF8,
                true,
                64 * 1024,
                false))
            {
                string headerLine = reader.ReadLine();
                string journalBaseHash;
                int journalFormatVersion;
                string headerError;
                if (!LightweightSidecarJson.TryReadJournalHeader(
                        headerLine,
                        out journalBaseHash,
                        out journalFormatVersion,
                        out headerError))
                {
                    if (!endsWithNewline && reader.Peek() < 0)
                    {
                        // A crash while creating the first journal header cannot
                        // invalidate the already-fsynced base snapshot.
                        persistenceInfo.ForceFullSnapshot = true;
                        return true;
                    }

                    errorMessage = "The IMDC journal header is invalid: " +
                        headerError;
                    return false;
                }

                if (!string.Equals(
                        journalBaseHash,
                        baseFileHash,
                        StringComparison.Ordinal))
                {
                    // The base snapshot was atomically replaced but a crash occurred
                    // before stale-journal cleanup. The hash makes that journal
                    // unambiguously inapplicable, so ignore it and compact next save.
                    persistenceInfo.ForceFullSnapshot = true;
                    return true;
                }

                if (journalFormatVersion == JournalFormatVersion)
                {
                    int replayedEntryCount;
                    bool forceFullSnapshot;
                    string replayError;
                    if (!LightweightSidecarJson.TryReplayJournalTransactions(
                            reader,
                            endsWithNewline,
                            document,
                            out replayedEntryCount,
                            out forceFullSnapshot,
                            out replayError))
                    {
                        errorMessage =
                            "The IMDC journal contains an invalid v2 transaction: " +
                            replayError;
                        return false;
                    }

                    persistenceInfo.JournalEntryCount += replayedEntryCount;
                    persistenceInfo.ForceFullSnapshot |= forceFullSnapshot;
                }
                else
                {
                    while (true)
                    {
                        string entryLine = reader.ReadLine();
                        if (entryLine == null)
                        {
                            break;
                        }

                        try
                        {
                            LightweightSidecarJson.ApplyJournalEntry(
                                entryLine,
                                document);
                            persistenceInfo.JournalEntryCount++;
                        }
                        catch (Exception exception)
                        {
                            if (!endsWithNewline && reader.Peek() < 0)
                            {
                                persistenceInfo.ForceFullSnapshot = true;
                                break;
                            }

                            errorMessage =
                                "The IMDC legacy journal contains an invalid entry: " +
                                exception.Message;
                            return false;
                        }
                    }

                    // Read old v1 journals for compatibility, but compact them at
                    // the next save so all new appends use transactional v2 rows.
                    persistenceInfo.ForceFullSnapshot = true;
                }
            }

            return true;
        }

        private void RegisterLoadedPathStateLocked(
            string path,
            LightweightSidecarDocument document,
            LightweightLoadedPersistenceInfo persistenceInfo)
        {
            if (string.IsNullOrEmpty(path) ||
                document == null ||
                persistenceInfo == null)
            {
                return;
            }

            committedPathStates[path] = new CommittedPathState
            {
                BaseFileHash = persistenceInfo.BaseFileHash ?? string.Empty,
                BaseFileBytes = persistenceInfo.BaseFileBytes,
                JournalBytes = persistenceInfo.JournalBytes,
                JournalEntryCount = persistenceInfo.JournalEntryCount,
                EventCount = activeEvents.Count,
                CustomMutationCount = activeCustomMutations.Count,
                CheckpointCount = activeCheckpoints.Count,
                LastIssuedSequence = lastIssuedSequence,
                // Preserve the loaded base as an incremental baseline unless
                // replay/retention/format cleanup actually changed its durable
                // representation. A real rollback increments activeStateRevision
                // during checkpoint activation and invalidates this baseline later.
                StateRevision = persistenceInfo.ForceFullSnapshot
                    ? activeStateRevision - 1L
                    : activeStateRevision
            };
            lastPersistenceMode = persistenceInfo.JournalEntryCount > 0
                ? "journal_loaded"
                : "snapshot_loaded";
            lastBaseSnapshotBytes = persistenceInfo.BaseFileBytes;
            lastJournalBytes = persistenceInfo.JournalBytes;
            lastJournalEntryCount = persistenceInfo.JournalEntryCount;
        }

        private bool TryValidateDocumentLocked(
            LightweightSidecarDocument document,
            out string errorMessage)
        {
            return TryValidateDocumentLocked(
                document,
                currentRelativeSavePath,
                out errorMessage);
        }

        private bool TryValidateDocumentLocked(
            LightweightSidecarDocument document,
            string expectedRelativeSavePath,
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
                    VanillaSaveStamp.NormalizeRelativePath(expectedRelativeSavePath),
                    CorePaths.PathComparison))
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
            HashSet<CheckpointIdentity> validatedCheckpointIdentities =
                new HashSet<CheckpointIdentity>();
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

                if (document.FormatVersion < 3)
                {
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
                }
                else if (record.PayloadJson == null)
                {
                    errorMessage =
                        "The sidecar contains an invalid event payload.";
                    return false;
                }

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
                    if (document.FormatVersion < 3)
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
                    else if (mutation.ValueJson == null)
                    {
                        errorMessage =
                            "The sidecar contains an invalid custom-data value.";
                        return false;
                    }
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

                if (!validatedCheckpointIdentities.Add(
                        CheckpointIdentity.From(checkpoint)))
                {
                    errorMessage =
                        "The sidecar contains duplicate checkpoint identities.";
                    return false;
                }
            }

            if (document.LastIssuedSequence < maximumSequence)
            {
                errorMessage =
                    "The sidecar sequence watermark is inconsistent.";
                return false;
            }

            return true;
        }


        private bool LoadDocumentLocked(LightweightSidecarDocument document)
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
            RebuildDurableCheckpointIdentityIndexLocked();
            activeEvents = new List<LightweightEventRecord>(durableEvents);
            activeCustomMutations =
                new List<LightweightCustomMutationRecord>(
                    durableCustomMutations);
            activeCheckpoints =
                new List<LightweightCheckpointRecord>(durableCheckpoints);
            lastIssuedSequence = document.LastIssuedSequence;
            RebuildRuntimeIndexesLocked();
            return retiredTechnicalEventCount > 0 ||
                sparseMoneyPayloadCount > 0 ||
                sharedParticipantRowsRemoved > 0;
        }

        private void ActivateThroughSequenceLocked(
            long sequence,
            DateTime cutoffGameDate)
        {
            activeStateRevision++;
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
            bool checkpointTrimmed = false;

            bool eventScanRequired =
                maxActiveEventSequence > sequence ||
                maxActiveEventGameDate > cutoffGameDate;
            if (eventScanRequired)
            {
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
            }

            bool customMutationScanRequired =
                maxActiveCustomMutationSequence > sequence ||
                maxActiveCustomMutationGameDate > cutoffGameDate;
            if (customMutationScanRequired)
            {
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
            }

            bool checkpointScanRequired =
                maxActiveCheckpointSequence > sequence ||
                maxActiveCheckpointGameDate > cutoffGameDate;
            if (checkpointScanRequired)
            {
                for (int index = activeCheckpoints.Count - 1; index >= 0; index--)
                {
                    LightweightCheckpointRecord checkpoint = activeCheckpoints[index];
                    if (checkpoint == null ||
                        checkpoint.Sequence > sequence ||
                        !CheckpointIsAtOrBefore(checkpoint, cutoffGameDate))
                    {
                        activeCheckpoints.RemoveAt(index);
                        checkpointTrimmed = true;
                    }
                }
            }

            // Forward saves normally trim nothing. Rebuilding and sorting every
            // timeline/custom-data index in that common case is pure O(history)
            // overhead on top of serialization. Rebuild only when event or custom
            // mutation membership actually changed. Checkpoint-only changes do not
            // participate in those derived indexes.
            if (activeMutationTrimmed)
            {
                activeStateRevision++;
                RebuildRuntimeIndexesLocked();
            }
            else if (checkpointTrimmed)
            {
                activeStateRevision++;
                RecomputeCheckpointWatermarkLocked();
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
            latestSingleCastBySingleId.Clear();
            latestTourStateByTourId.Clear();
            ResetActiveWatermarksLocked();

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
                UpdateEventWatermarkLocked(record);
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
                UpdateCustomMutationWatermarkLocked(mutation);
            }

            for (int index = 0; index < activeCheckpoints.Count; index++)
            {
                LightweightCheckpointRecord checkpoint = activeCheckpoints[index];
                if (checkpoint != null)
                {
                    IndexCheckpointByPathLocked(checkpoint);
                    UpdateCheckpointWatermarkLocked(checkpoint, null);
                }
            }
        }

        private void ResetActiveWatermarksLocked()
        {
            maxActiveEventSequence = 0L;
            maxActiveCustomMutationSequence = 0L;
            maxActiveCheckpointSequence = 0L;
            maxActiveEventGameDate = DateTime.MinValue;
            maxActiveCustomMutationGameDate = DateTime.MinValue;
            maxActiveCheckpointGameDate = DateTime.MinValue;
            activeCheckpointsByRelativePath.Clear();
            activeCheckpointsByIdentity.Clear();
        }

        private void UpdateEventWatermarkLocked(LightweightEventRecord record)
        {
            if (record == null)
            {
                return;
            }

            if (record.Sequence > maxActiveEventSequence)
            {
                maxActiveEventSequence = record.Sequence;
            }

            DateTime gameDate;
            if (!TryParseRoundTripDate(record.GameDateTime, out gameDate))
            {
                // Unknown dates must force the conservative trim path rather than
                // accidentally proving that no record can be beyond a checkpoint.
                maxActiveEventGameDate = DateTime.MaxValue;
            }
            else if (gameDate > maxActiveEventGameDate)
            {
                maxActiveEventGameDate = gameDate;
            }
        }

        private void UpdateCustomMutationWatermarkLocked(
            LightweightCustomMutationRecord mutation)
        {
            if (mutation == null)
            {
                return;
            }

            if (mutation.Sequence > maxActiveCustomMutationSequence)
            {
                maxActiveCustomMutationSequence = mutation.Sequence;
            }

            DateTime gameDate;
            if (!TryParseRoundTripDate(mutation.GameDateTime, out gameDate))
            {
                maxActiveCustomMutationGameDate = DateTime.MaxValue;
            }
            else if (gameDate > maxActiveCustomMutationGameDate)
            {
                maxActiveCustomMutationGameDate = gameDate;
            }
        }

        private void UpdateCheckpointWatermarkLocked(
            LightweightCheckpointRecord checkpoint,
            DateTime? knownGameDate)
        {
            if (checkpoint == null)
            {
                return;
            }

            if (checkpoint.Sequence > maxActiveCheckpointSequence)
            {
                maxActiveCheckpointSequence = checkpoint.Sequence;
            }

            DateTime gameDate;
            if (knownGameDate.HasValue)
            {
                gameDate = knownGameDate.Value;
            }
            else if (!TryParseRoundTripDate(checkpoint.GameDateTime, out gameDate))
            {
                maxActiveCheckpointGameDate = DateTime.MaxValue;
                return;
            }

            if (gameDate > maxActiveCheckpointGameDate)
            {
                maxActiveCheckpointGameDate = gameDate;
            }
        }

        private bool ActiveStateFitsCheckpointLocked(
            long sequence,
            DateTime cutoffGameDate)
        {
            return maxActiveEventSequence <= sequence &&
                maxActiveCustomMutationSequence <= sequence &&
                maxActiveCheckpointSequence <= sequence &&
                maxActiveEventGameDate <= cutoffGameDate &&
                maxActiveCustomMutationGameDate <= cutoffGameDate &&
                maxActiveCheckpointGameDate <= cutoffGameDate;
        }

        private void RebuildDurableCheckpointIdentityIndexLocked()
        {
            durableCheckpointsByIdentity.Clear();
            for (int index = 0; index < durableCheckpoints.Count; index++)
            {
                LightweightCheckpointRecord checkpoint = durableCheckpoints[index];
                if (checkpoint != null)
                {
                    durableCheckpointsByIdentity[CheckpointIdentity.From(checkpoint)] =
                        checkpoint;
                }
            }
        }

        private void RecomputeCheckpointWatermarkLocked()
        {
            maxActiveCheckpointSequence = 0L;
            maxActiveCheckpointGameDate = DateTime.MinValue;
            activeCheckpointsByRelativePath.Clear();
            activeCheckpointsByIdentity.Clear();
            for (int index = 0; index < activeCheckpoints.Count; index++)
            {
                LightweightCheckpointRecord checkpoint = activeCheckpoints[index];
                IndexCheckpointByPathLocked(checkpoint);
                UpdateCheckpointWatermarkLocked(checkpoint, null);
            }
        }

        private void IndexCheckpointByPathLocked(
            LightweightCheckpointRecord checkpoint)
        {
            if (checkpoint == null)
            {
                return;
            }

            activeCheckpointsByIdentity[CheckpointIdentity.From(checkpoint)] =
                checkpoint;
            string relativePath = VanillaSaveStamp.NormalizeRelativePath(
                checkpoint.RelativeSavePath);
            List<LightweightCheckpointRecord> pathRows;
            if (!activeCheckpointsByRelativePath.TryGetValue(
                    relativePath,
                    out pathRows))
            {
                pathRows = new List<LightweightCheckpointRecord>();
                activeCheckpointsByRelativePath[relativePath] = pathRows;
            }
            pathRows.Add(checkpoint);
        }

        private IReadOnlyList<LightweightCheckpointRecord>
            GetActiveCheckpointsForPathLocked(string relativeSavePath)
        {
            string normalizedPath = VanillaSaveStamp.NormalizeRelativePath(
                relativeSavePath);
            List<LightweightCheckpointRecord> pathRows;
            return activeCheckpointsByRelativePath.TryGetValue(
                    normalizedPath,
                    out pathRows)
                ? pathRows
                : (IReadOnlyList<LightweightCheckpointRecord>)
                    Array.Empty<LightweightCheckpointRecord>();
        }

        private static bool TryParseRoundTripDate(
            string value,
            out DateTime gameDate)
        {
            return DateTime.TryParseExact(
                value ?? string.Empty,
                CoreConstants.RoundTripDateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out gameDate);
        }


        private void IndexLatestBuiltInStateLocked(
            LightweightEventRecord record)
        {
            if (record == null ||
                !string.IsNullOrEmpty(record.NamespaceIdentifier))
            {
                return;
            }

            bool isSingleRelease =
                string.Equals(
                    record.EntityKind,
                    CoreConstants.EventEntityKindSingle,
                    StringComparison.Ordinal) &&
                string.Equals(
                    record.EventType,
                    CoreConstants.EventTypeSingleReleased,
                    StringComparison.Ordinal);
            bool isTourStart =
                string.Equals(
                    record.EntityKind,
                    CoreConstants.EventEntityKindTour,
                    StringComparison.Ordinal) &&
                string.Equals(
                    record.EventType,
                    CoreConstants.EventTypeTourStarted,
                    StringComparison.Ordinal);
            if (!isSingleRelease && !isTourStart)
            {
                return;
            }

            int entityIdentifier;
            if (!int.TryParse(
                    record.EntityId,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out entityIdentifier))
            {
                return;
            }

            if (isSingleRelease)
            {
                List<int> slotIds;
                if (SharedTimelineParticipants.TryReadSingleCastSlotIds(
                        record.PayloadJson,
                        out slotIds) &&
                    ContainsValidIdolIdentifier(slotIds))
                {
                    latestSingleCastBySingleId[entityIdentifier] =
                        new List<int>(slotIds);
                }
                return;
            }

            List<int> participantIds;
            if (!SharedTimelineParticipants.TryReadTourParticipantIds(
                    record.PayloadJson,
                    out participantIds))
            {
                return;
            }

            string startDate;
            LightweightSidecarJson.TryReadStringProperty(
                record.PayloadJson,
                CoreConstants.JsonFieldTourStartDate,
                out startDate);
            latestTourStateByTourId[entityIdentifier] =
                new TourRuntimeState
                {
                    Sequence = record.Sequence,
                    ParticipantIdolIdentifiers =
                        new List<int>(participantIds),
                    StartDate = startDate ?? string.Empty
                };
        }

        private void IndexEventLocked(LightweightEventRecord record)
        {
            if (record == null)
            {
                return;
            }

            IndexLatestBuiltInStateLocked(record);

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

        private LightweightPersistenceSnapshot BuildPersistenceSnapshotLocked(
            string normalizedTargetPath,
            string relativeSavePath,
            long generation,
            bool preserveExistingBackup)
        {
            string normalizedRelativePath =
                VanillaSaveStamp.NormalizeRelativePath(relativeSavePath);
            IReadOnlyList<LightweightCheckpointRecord> pathCheckpointRows =
                GetActiveCheckpointsForPathLocked(normalizedRelativePath);

            CommittedPathState baselineState = null;
            bool canUseIncrementalSnapshot =
                !preserveExistingBackup &&
                committedPathStates.TryGetValue(
                    normalizedTargetPath,
                    out baselineState) &&
                baselineState != null &&
                File.Exists(normalizedTargetPath) &&
                baselineState.StateRevision == activeStateRevision &&
                !string.IsNullOrEmpty(baselineState.BaseFileHash) &&
                lastIssuedSequence >= baselineState.LastIssuedSequence &&
                activeEvents.Count >= baselineState.EventCount &&
                activeCustomMutations.Count >= baselineState.CustomMutationCount &&
                pathCheckpointRows.Count >= baselineState.CheckpointCount;

            if (canUseIncrementalSnapshot)
            {
                return new LightweightPersistenceSnapshot
                {
                    TargetPath = normalizedTargetPath,
                    RelativeSavePath = normalizedRelativePath,
                    Generation = generation,
                    PreserveExistingBackup = false,
                    StateRevision = activeStateRevision,
                    IsIncremental = true,
                    BaseEventCount = baselineState.EventCount,
                    BaseCustomMutationCount =
                        baselineState.CustomMutationCount,
                    BaseCheckpointCount = baselineState.CheckpointCount,
                    TotalEventCount = activeEvents.Count,
                    TotalCustomMutationCount = activeCustomMutations.Count,
                    TotalCheckpointCount = pathCheckpointRows.Count,
                    Document = new LightweightSidecarDocument
                    {
                        FormatName = SidecarFormatName,
                        FormatVersion = SidecarFormatVersion,
                        RelativeSavePath = normalizedRelativePath,
                        LastIssuedSequence = lastIssuedSequence,
                        Events = CopySuffix(
                            activeEvents,
                            baselineState.EventCount),
                        CustomMutations = CopySuffix(
                            activeCustomMutations,
                            baselineState.CustomMutationCount),
                        Checkpoints = CopySuffix(
                            pathCheckpointRows,
                            baselineState.CheckpointCount)
                    }
                };
            }

            LightweightSidecarDocument fullDocument =
                BuildDocumentLocked(
                    relativeSavePath,
                    CloneCheckpoints(pathCheckpointRows));
            return new LightweightPersistenceSnapshot
            {
                TargetPath = normalizedTargetPath,
                RelativeSavePath = normalizedRelativePath,
                Generation = generation,
                PreserveExistingBackup = preserveExistingBackup,
                StateRevision = activeStateRevision,
                IsIncremental = false,
                BaseEventCount = 0,
                BaseCustomMutationCount = 0,
                BaseCheckpointCount = 0,
                TotalEventCount = fullDocument.Events.Count,
                TotalCustomMutationCount = fullDocument.CustomMutations.Count,
                TotalCheckpointCount = fullDocument.Checkpoints.Count,
                Document = fullDocument
            };
        }

        private static List<T> CopySuffix<T>(IReadOnlyList<T> source, int startIndex)
        {
            if (source == null)
            {
                return new List<T>();
            }
            if (startIndex < 0 || startIndex > source.Count)
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }

            List<T> suffix = new List<T>(source.Count - startIndex);
            for (int index = startIndex; index < source.Count; index++)
            {
                suffix.Add(source[index]);
            }
            return suffix;
        }

        private LightweightSidecarDocument BuildDocumentLocked(
            string relativeSavePath)
        {
            return BuildDocumentLocked(
                relativeSavePath,
                CloneCheckpoints(
                    GetActiveCheckpointsForPathLocked(relativeSavePath)));
        }

        private LightweightSidecarDocument BuildDocumentLocked(
            string relativeSavePath,
            List<LightweightCheckpointRecord> pathCheckpoints)
        {
            return new LightweightSidecarDocument
            {
                FormatName = SidecarFormatName,
                FormatVersion = SidecarFormatVersion,
                RelativeSavePath = VanillaSaveStamp.NormalizeRelativePath(
                    relativeSavePath),
                LastIssuedSequence = lastIssuedSequence,
                Checkpoints = pathCheckpoints ??
                    new List<LightweightCheckpointRecord>(),
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
            out long committedBytes,
            out string committedHash,
            out string errorMessage)
        {
            committedBytes = 0L;
            committedHash = string.Empty;
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
                using (HashingWriteStream hashingStream =
                    new HashingWriteStream(stream))
                {
                    using (StreamWriter writer = new StreamWriter(
                        hashingStream,
                        new System.Text.UTF8Encoding(false),
                        65536,
                        true))
                    {
                        LightweightSidecarJson.SerializeTo(writer, document);
                    }

                    // Finalize only after StreamWriter.Dispose has flushed its
                    // encoder. No later text write can escape the fingerprint.
                    committedHash = hashingStream.CompleteAndGetHashHex();
                    stream.Flush(true);
                    committedBytes = stream.Length;
                }

                // The complete fingerprint is known before the physical commit.
                // No post-replace failure can leave memory describing the old base
                // while disk already contains the new generation.
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
            RebuildDurableCheckpointIdentityIndexLocked();
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
                    CorePaths.PathComparison);
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
            activeCheckpointsByIdentity.Clear();
            durableCheckpointsByIdentity.Clear();
            backgroundCompactionsInFlight.Clear();
            customValues.Clear();
            customUsageByNamespace.Clear();
            activeMutationSequences.Clear();
            customEventIdempotencyKeys.Clear();
            timelineEventsByIdolId.Clear();
            globalTimelineEvents.Clear();
            moneyTransactionsByDateKey.Clear();
            moneyLedgerCoverageStartEvent = null;
            latestSingleCastBySingleId.Clear();
            latestTourStateByTourId.Clear();
            currentSidecarPath = string.Empty;
            currentRelativeSavePath = string.Empty;
            blockedPersistencePath = string.Empty;
            blockedPersistenceReason = string.Empty;
            latestCommittedPersistenceGenerationByPath.Clear();
            committedPathStates.Clear();
            nextPersistenceGeneration = 0L;
            lastCommittedPersistenceGeneration = 0L;
            lastPersistenceMode = "none";
            lastBaseSnapshotBytes = 0L;
            lastJournalBytes = 0L;
            lastJournalEntryCount = 0;
            activeStateRevision = 0L;
            ResetActiveWatermarksLocked();
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

        private struct CheckpointIdentity : IEquatable<CheckpointIdentity>
        {
            private readonly string relativeSavePath;
            private readonly string lastSave;
            private readonly long playtimeSeconds;
            private readonly string gameDateTime;

            private CheckpointIdentity(
                string relativeSavePath,
                string lastSave,
                long playtimeSeconds,
                string gameDateTime)
            {
                this.relativeSavePath = relativeSavePath ?? string.Empty;
                this.lastSave = lastSave ?? string.Empty;
                this.playtimeSeconds = playtimeSeconds;
                this.gameDateTime = gameDateTime ?? string.Empty;
            }

            internal static CheckpointIdentity From(VanillaSaveStamp stamp)
            {
                return new CheckpointIdentity(
                    stamp != null
                        ? VanillaSaveStamp.NormalizeRelativePath(
                            stamp.RelativeSavePath)
                        : string.Empty,
                    stamp != null ? stamp.LastSave : string.Empty,
                    stamp != null ? stamp.PlaytimeSeconds : 0L,
                    stamp != null ? stamp.GameDateTime : string.Empty);
            }

            internal static CheckpointIdentity From(
                LightweightCheckpointRecord checkpoint)
            {
                return new CheckpointIdentity(
                    checkpoint != null
                        ? VanillaSaveStamp.NormalizeRelativePath(
                            checkpoint.RelativeSavePath)
                        : string.Empty,
                    checkpoint != null ? checkpoint.LastSave : string.Empty,
                    checkpoint != null ? checkpoint.PlaytimeSeconds : 0L,
                    checkpoint != null ? checkpoint.GameDateTime : string.Empty);
            }

            public bool Equals(CheckpointIdentity other)
            {
                return CorePaths.PathComparer.Equals(
                        relativeSavePath,
                        other.relativeSavePath) &&
                    StringComparer.Ordinal.Equals(
                        lastSave,
                        other.lastSave) &&
                    playtimeSeconds == other.playtimeSeconds &&
                    StringComparer.Ordinal.Equals(
                        gameDateTime,
                        other.gameDateTime);
            }

            public override bool Equals(object obj)
            {
                return obj is CheckpointIdentity &&
                    Equals((CheckpointIdentity)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = (hash * 31) +
                        CorePaths.PathComparer.GetHashCode(
                            relativeSavePath ?? string.Empty);
                    hash = (hash * 31) +
                        StringComparer.Ordinal.GetHashCode(
                            lastSave ?? string.Empty);
                    hash = (hash * 31) + playtimeSeconds.GetHashCode();
                    hash = (hash * 31) +
                        StringComparer.Ordinal.GetHashCode(
                            gameDateTime ?? string.Empty);
                    return hash;
                }
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
                    CorePaths.PathComparison) &&
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
                PayloadJson = source.PayloadJson ?? CoreConstants.EmptyJsonObject,
                StoragePayloadJson = source.StoragePayloadJson ?? string.Empty
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
                ValueJson = source.ValueJson ?? string.Empty,
                StorageValueJson = source.StorageValueJson ?? string.Empty
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
