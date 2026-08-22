using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace IMDataCore
{
    /// <summary>
    /// Computes a stable content identity for one vanilla SavedData graph. The
    /// fingerprint is based on Unity's compact JSON representation, which provides
    /// a deterministic semantic identity across the save and later load boundary.
    /// A detached snapshot may register the fingerprint of the JSON that created it
    /// so checkpoint construction never performs a redundant full serialization.
    /// </summary>
    internal static class VanillaSavedDataFingerprint
    {
        internal const string Prefix = "sha256:";
        private const int Utf8ChunkCharacterCount = 4096;
        private const int Utf8ChunkByteCount = Utf8ChunkCharacterCount * 4;

        private sealed class CachedFingerprint
        {
            internal string Value = string.Empty;
        }

        private static readonly object CacheLock = new object();
        private static readonly ConditionalWeakTable<SaveManager.SavedData, CachedFingerprint>
            FrozenFingerprintCache =
                new ConditionalWeakTable<SaveManager.SavedData, CachedFingerprint>();

        internal static bool TryComputeForSavedData(
            SaveManager.SavedData savedData,
            out string fingerprint,
            out string errorMessage)
        {
            fingerprint = string.Empty;
            errorMessage = string.Empty;
            if (savedData == null)
            {
                errorMessage = "Vanilla SavedData is null.";
                return false;
            }

            lock (CacheLock)
            {
                CachedFingerprint cached;
                if (FrozenFingerprintCache.TryGetValue(savedData, out cached) &&
                    cached != null &&
                    IsValid(cached.Value))
                {
                    fingerprint = cached.Value;
                    FrozenFingerprintCache.Remove(savedData);
                    return true;
                }
            }

            try
            {
                string json = UnityEngine.JsonUtility.ToJson(savedData, false);
                fingerprint = ComputeForJson(json);
                return true;
            }
            catch (Exception exception)
            {
                errorMessage =
                    "Computing the vanilla SavedData fingerprint failed: " +
                    exception.Message;
                return false;
            }
        }

        internal static string ComputeForJson(string json)
        {
            string value = json ?? string.Empty;
            Encoding utf8 = Encoding.UTF8;
            byte[] byteBuffer = new byte[Utf8ChunkByteCount];

            using (SHA256 sha256 = SHA256.Create())
            {
                int characterIndex = 0;
                while (characterIndex < value.Length)
                {
                    int characterCount = Math.Min(
                        Utf8ChunkCharacterCount,
                        value.Length - characterIndex);

                    // Encoding.GetBytes(string, ...) is stateless. Keep UTF-16
                    // surrogate pairs in the same chunk so a boundary can never
                    // change the UTF-8 byte sequence being hashed.
                    int finalCharacterIndex = characterIndex + characterCount - 1;
                    if (characterCount > 0 &&
                        finalCharacterIndex + 1 < value.Length &&
                        char.IsHighSurrogate(value[finalCharacterIndex]) &&
                        char.IsLowSurrogate(value[finalCharacterIndex + 1]))
                    {
                        characterCount--;
                    }

                    int byteCount = utf8.GetBytes(
                        value,
                        characterIndex,
                        characterCount,
                        byteBuffer,
                        0);
                    if (byteCount > 0)
                    {
                        sha256.TransformBlock(
                            byteBuffer,
                            0,
                            byteCount,
                            byteBuffer,
                            0);
                    }
                    characterIndex += characterCount;
                }

                sha256.TransformFinalBlock(new byte[0], 0, 0);
                return Prefix + ToLowerHex(sha256.Hash);
            }
        }

        internal static void RegisterFrozenFingerprint(
            SaveManager.SavedData savedData,
            string fingerprint)
        {
            if (savedData == null || !IsValid(fingerprint))
            {
                return;
            }

            lock (CacheLock)
            {
                FrozenFingerprintCache.Remove(savedData);
                FrozenFingerprintCache.Add(
                    savedData,
                    new CachedFingerprint { Value = fingerprint });
            }
        }

        internal static bool IsValid(string fingerprint)
        {
            if (string.IsNullOrEmpty(fingerprint) ||
                fingerprint.Length != Prefix.Length + 64 ||
                !fingerprint.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return false;
            }

            for (int index = Prefix.Length; index < fingerprint.Length; index++)
            {
                char value = fingerprint[index];
                if (!((value >= '0' && value <= '9') ||
                      (value >= 'a' && value <= 'f')))
                {
                    return false;
                }
            }

            return true;
        }

        private static string ToLowerHex(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }

            char[] chars = new char[bytes.Length * 2];
            const string Hex = "0123456789abcdef";
            for (int index = 0; index < bytes.Length; index++)
            {
                byte value = bytes[index];
                chars[index * 2] = Hex[value >> 4];
                chars[(index * 2) + 1] = Hex[value & 0x0F];
            }
            return new string(chars);
        }
    }

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
        internal string ContentFingerprint = string.Empty;

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

            string contentFingerprint;
            if (!VanillaSavedDataFingerprint.TryComputeForSavedData(
                    savedData,
                    out contentFingerprint,
                    out errorMessage))
            {
                return false;
            }

            stamp = new VanillaSaveStamp
            {
                RelativeSavePath = normalizedRelativePath,
                LastSave = savedData.staticVars__PlayerData.LastSave ?? string.Empty,
                PlaytimeSeconds = savedData.staticVars__PlayerData.Playtime_Seconds,
                GameDateTime = savedData.staticVars__dateTime ?? string.Empty,
                ContentFingerprint = contentFingerprint
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
                string.Equals(checkpoint.GameDateTime, GameDateTime, StringComparison.Ordinal) &&
                string.Equals(
                    checkpoint.ContentFingerprint,
                    ContentFingerprint,
                    StringComparison.Ordinal);
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
    /// Versioned JSON envelope for the lightweight sidecar. Version 5 gives every
    /// vanilla-save checkpoint a SHA-256 content fingerprint so exact checkpoint
    /// identity no longer depends on second-resolution timestamps. Runtime
    /// dictionaries and read indexes are always rebuilt.
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
        internal long PathArchiveEpoch;
        internal bool PreserveExistingBackup;
        // When a session was recovered from .imdc.bak, this records the exact
        // journal that completed that backup generation. A healing snapshot uses
        // the provenance to preserve the complete known-good backup before it
        // removes any journal beside the repaired primary.
        internal string BackupRecoveryJournalPath = string.Empty;
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

    internal enum LightweightJournalReplayStatus
    {
        Missing = 0,
        TornBeforeHeader = 1,
        HeaderMismatch = 2,
        HeaderMatched = 3
    }

    internal sealed class LightweightLoadedPersistenceInfo
    {
        internal string BaseFileHash = string.Empty;
        internal long BaseFileBytes;
        internal long JournalBytes;
        internal int JournalEntryCount;
        internal bool ForceFullSnapshot;
        internal string ReplayedJournalPath = string.Empty;
    }

    [Serializable]
    internal sealed class LightweightModSnapshotRecord
    {
        public string ModName = string.Empty;
        public string Title = string.Empty;
        public string Author = string.Empty;
        public string Version = string.Empty;
        public List<string> DllNames = new List<string>();
    }

    [Serializable]
    internal sealed class LightweightAgencyRoomIdentityRecord
    {
        // Durable IMDC-owned generation identity. The raw vanilla room id is
        // intentionally not used here because vanilla does not serialize it.
        public string EntityId = string.Empty;
        public int FloorIndex = CoreConstants.InvalidIdValue;
        public int RoomIndex = CoreConstants.InvalidIdValue;
        public int RoomTypeRaw = CoreConstants.InvalidIdValue;
        public int TheaterId = CoreConstants.InvalidIdValue;
    }

    [Serializable]
    internal sealed class LightweightCheckpointRecord
    {
        public string RelativeSavePath = string.Empty;
        public string LastSave = string.Empty;
        public long PlaytimeSeconds;
        public string GameDateTime = string.Empty;
        public string ContentFingerprint = string.Empty;
        public long Sequence;
        public List<LightweightModSnapshotRecord> EnabledMods =
            new List<LightweightModSnapshotRecord>();
        public List<LightweightAgencyRoomIdentityRecord> AgencyRoomIdentities =
            new List<LightweightAgencyRoomIdentityRecord>();
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
        // Pre-transformed native sidecar storage representation. This is deliberately not a
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
    /// The sole normal-runtime persistence implementation for IM Data Core 3.4.
    /// Mutations update memory only; callers explicitly persist at vanilla save
    /// boundaries or through TryFlushNow.
    /// </summary>
    internal sealed class LightweightCoreStorageEngine : IDisposable
    {
        internal const string SidecarFormatName = "IMDataCore.LightweightSidecar";
        internal const int SidecarFormatVersion = 5;
        internal const string CustomOperationSet = "SET";
        internal const string CustomOperationRemove = "REMOVE";
        internal const string JournalFormatName = "IMDataCore.LightweightJournal";
        internal const int JournalFormatVersion = 2;
        private static readonly TimeSpan OrphanTemporaryFileMinimumAge =
            TimeSpan.FromHours(24.0);
        private const long MinimumJournalCompactionBytes = 1024L * 1024L;
        private const long MaximumJournalCompactionBytes = 16L * 1024L * 1024L;
        private const int MinimumJournalTransactionsBeforeCompaction = 2048;
        private const int MaximumJournalTransactionsBeforeCompaction = 32768;
        private const int JournalTransactionsPerBaseMiB = 64;

        // Physical sidecars outlive individual engine instances while background
        // compaction is queued. The lock therefore belongs to the process/path,
        // not to one LightweightCoreStorageEngine object.
        private static readonly object PersistenceIoRegistryLock = new object();
        private static readonly ReaderWriterLockSlim PersistenceTopologyLock =
            new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private static readonly Dictionary<string, object> PersistenceIoLocksByPath =
            new Dictionary<string, object>(CorePaths.PathComparer);
        private static readonly Dictionary<string, long> PersistenceArchiveEpochByPath =
            new Dictionary<string, long>(CorePaths.PathComparer);
        private static readonly HashSet<string> PersistenceArchiveBlockedDirectories =
            new HashSet<string>(CorePaths.PathComparer);
        private static readonly HashSet<string> PersistenceArchiveInProgressDirectories =
            new HashSet<string>(CorePaths.PathComparer);

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

        private sealed class DocumentValidationState
        {
            internal readonly HashSet<long> Sequences = new HashSet<long>();
            internal readonly HashSet<string> EventIdempotencyKeys =
                new HashSet<string>(StringComparer.Ordinal);
            internal readonly HashSet<CheckpointIdentity> CheckpointIdentities =
                new HashSet<CheckpointIdentity>();
            internal long MaximumSequence;
            internal long LastEventSequence;
            internal long LastCustomMutationSequence;
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
        private string recoveredBackupJournalPath = string.Empty;
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

            // Scavenge only temporary files derived from this exact sidecar name.
            // Do it under the per-path I/O lock, before taking storageLock, so the
            // lock order stays consistent with persistence/background compaction.
            // Fresh files are retained for 24 hours to avoid interfering with an
            // unusual concurrent process that may still own them.
            string scavengeSidecarPath;
            string scavengeValidationError;
            if (CorePaths.TryValidateContainedMutationPath(
                    saveScope.SidecarFilePath ?? string.Empty,
                    false,
                    out scavengeSidecarPath,
                    out scavengeValidationError))
            {
                using (AcquirePersistenceTopologyReadLease())
                {
                    object pathIoLock = GetPersistenceIoLock(scavengeSidecarPath);
                    lock (pathIoLock)
                    {
                        ScavengeAbandonedTemporaryFilesForScope(
                            scavengeSidecarPath);
                    }
                }
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
                            currentRelativeSavePath,
                            currentSidecarPath + ".imdc.journal",
                            out document,
                            out backupLoadInfo,
                            out backupError))
                    {
                        LoadDocumentLocked(document);
                        loadedExistingSidecarDocument = true;
                        recoveredFromBackup = true;
                        recoveredBackupJournalPath =
                            backupLoadInfo.ReplayedJournalPath ?? string.Empty;
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


        /// <summary>
        /// Invalidates every in-memory physical baseline/checkpoint that belongs to
        /// an IMDC directory being archived after vanilla deleted the matching save.
        /// Event/custom-data history is intentionally retained so a still-running
        /// career can later be saved under a new physical path.
        /// </summary>
        internal bool InvalidatePhysicalDirectoryForArchive(
            string sidecarDirectoryPath,
            out bool detachedCurrentScope,
            out string errorMessage)
        {
            detachedCurrentScope = false;
            errorMessage = string.Empty;

            string normalizedDirectory;
            if (!CorePaths.TryValidateContainedMutationPath(
                    sidecarDirectoryPath,
                    false,
                    out normalizedDirectory,
                    out errorMessage))
            {
                return false;
            }

            lock (storageLock)
            {
                try
                {
                    ThrowIfDisposed();

                    detachedCurrentScope = PathIsSameOrContained(
                        normalizedDirectory,
                        currentSidecarPath);

                    RemovePathStateRowsInsideDirectoryLocked(
                        latestCommittedPersistenceGenerationByPath,
                        normalizedDirectory);
                    RemovePathStateRowsInsideDirectoryLocked(
                        committedPathStates,
                        normalizedDirectory);

                    int activeCheckpointCountBefore = activeCheckpoints.Count;
                    activeCheckpoints.RemoveAll(
                        checkpoint => CheckpointTargetsDirectory(
                            checkpoint,
                            normalizedDirectory));
                    if (activeCheckpoints.Count != activeCheckpointCountBefore)
                    {
                        activeStateRevision++;
                        RecomputeCheckpointWatermarkLocked();
                    }

                    int durableCheckpointCountBefore = durableCheckpoints.Count;
                    durableCheckpoints.RemoveAll(
                        checkpoint => CheckpointTargetsDirectory(
                            checkpoint,
                            normalizedDirectory));
                    if (durableCheckpoints.Count != durableCheckpointCountBefore)
                    {
                        RebuildDurableCheckpointIdentityIndexLocked();
                    }

                    if (!string.IsNullOrEmpty(blockedPersistencePath) &&
                        PathIsSameOrContained(
                            normalizedDirectory,
                            blockedPersistencePath))
                    {
                        blockedPersistencePath = string.Empty;
                        blockedPersistenceReason = string.Empty;
                    }

                    if (detachedCurrentScope)
                    {
                        currentSidecarPath = string.Empty;
                        currentRelativeSavePath = string.Empty;
                        blockedPersistencePath = string.Empty;
                        blockedPersistenceReason = string.Empty;
                        recoveredFromBackup = false;
                        loadedExistingSidecarDocument = false;
                        lastPersistenceMode = "archived_detached";
                        lastBaseSnapshotBytes = 0L;
                        lastJournalBytes = 0L;
                        lastJournalEntryCount = 0;
                    }

                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage =
                        "Invalidating the archived IMDC persistence directory failed: " +
                        exception.Message;
                    return false;
                }
            }
        }

        private static void RemovePathStateRowsInsideDirectoryLocked<T>(
            Dictionary<string, T> rows,
            string directoryPath)
        {
            if (rows == null || rows.Count == 0)
            {
                return;
            }

            List<string> keys = new List<string>(rows.Keys);
            for (int index = 0; index < keys.Count; index++)
            {
                if (PathIsSameOrContained(directoryPath, keys[index]))
                {
                    rows.Remove(keys[index]);
                }
            }
        }

        private static bool CheckpointTargetsDirectory(
            LightweightCheckpointRecord checkpoint,
            string directoryPath)
        {
            if (checkpoint == null || string.IsNullOrEmpty(directoryPath))
            {
                return false;
            }

            try
            {
                string relativePath = VanillaSaveStamp.NormalizeRelativePath(
                    checkpoint.RelativeSavePath);
                if (string.IsNullOrEmpty(relativePath))
                {
                    return false;
                }

                string checkpointPath = Path.GetFullPath(
                    Path.Combine(
                        CorePaths.GetRootDirectory(),
                        relativePath));
                return PathIsSameOrContained(
                    directoryPath,
                    checkpointPath);
            }
            catch
            {
                return false;
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
        }

        internal bool TryGetMoneyTransactionTotals(
            DateTime startInclusive,
            DateTime endExclusive,
            out long incomeTotal,
            out long expenseTotal,
            out int transactionCount,
            out string errorMessage)
        {
            incomeTotal = 0L;
            expenseTotal = 0L;
            transactionCount = 0;
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

                    checked
                    {
                        foreach (KeyValuePair<int, List<LightweightEventRecord>> pair
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
                                IMDataCoreEvent publicMoneyEvent = ToPublicEvent(rows[index]);
                                publicMoneyEvent.PayloadJson =
                                    CorePayloadCompaction
                                        .ExpandMoneyTransactionPayloadForPublic(rows[index]);
                                IMDataCoreMoneyTransaction transaction =
                                    MoneyLedgerPayloadUtility.ToPublicModel(publicMoneyEvent);
                                if (transaction == null)
                                {
                                    continue;
                                }

                                transactionCount++;
                                if (transaction.Amount > 0L)
                                {
                                    incomeTotal += transaction.Amount;
                                }
                                else
                                {
                                    expenseTotal += transaction.Amount;
                                }
                            }
                        }
                    }
                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = "Calculating money transaction totals failed: " +
                        exception.Message;
                    return false;
                }
            }
        }

        internal bool TryGetMoneyLedgerCoverageStart(
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
        }

        /// <summary>
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

        /// <summary>
        /// Returns the enabled-mod inventory frozen into the exact vanilla-save
        /// checkpoint.
        /// </summary>
        internal bool TryGetCheckpointModSnapshot(
            VanillaSaveStamp stamp,
            out List<LightweightModSnapshotRecord> enabledMods,
            out string errorMessage)
        {
            enabledMods = new List<LightweightModSnapshotRecord>();
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
                    LightweightCheckpointRecord checkpoint;
                    if (!durableCheckpointsByIdentity.TryGetValue(
                            CheckpointIdentity.From(stamp),
                            out checkpoint) ||
                        checkpoint == null)
                    {
                        return true;
                    }

                    enabledMods = CloneModSnapshots(checkpoint.EnabledMods);
                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage =
                        "Reading the IMDC checkpoint mod snapshot failed: " +
                        exception.Message;
                    return false;
                }
            }
        }

        internal bool TryGetCheckpointAgencyRoomIdentities(
            VanillaSaveStamp stamp,
            out List<LightweightAgencyRoomIdentityRecord> roomIdentities,
            out string errorMessage)
        {
            roomIdentities = new List<LightweightAgencyRoomIdentityRecord>();
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
                    LightweightCheckpointRecord checkpoint;
                    if (!durableCheckpointsByIdentity.TryGetValue(
                            CheckpointIdentity.From(stamp),
                            out checkpoint) ||
                        checkpoint == null)
                    {
                        return true;
                    }

                    roomIdentities = CloneAgencyRoomIdentities(
                        checkpoint.AgencyRoomIdentities);
                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage =
                        "Reading the IMDC checkpoint room-identity snapshot failed: " +
                        exception.Message;
                    return false;
                }
            }
        }

        internal bool AddOrReplaceCheckpoint(
            VanillaSaveStamp stamp,
            long sequence,
            IReadOnlyList<LightweightModSnapshotRecord> enabledMods,
            IReadOnlyList<LightweightAgencyRoomIdentityRecord> agencyRoomIdentities,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (stamp == null)
            {
                errorMessage = "The vanilla save stamp is missing.";
                return false;
            }
            if (agencyRoomIdentities == null)
            {
                errorMessage = "The agency-room identity snapshot is missing.";
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
                            ContentFingerprint = stamp.ContentFingerprint,
                            Sequence = sequence,
                            EnabledMods = CloneModSnapshots(enabledMods),
                            AgencyRoomIdentities = CloneAgencyRoomIdentities(agencyRoomIdentities)
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

                    string archiveBlockReason;
                    if (TryGetPersistenceArchiveBlockReason(
                            normalizedSidecarPath,
                            out archiveBlockReason))
                    {
                        errorMessage = archiveBlockReason;
                        return false;
                    }

                    if (IsPersistenceBlockedForPathLocked(candidatePath))
                    {
                        errorMessage = blockedPersistenceReason +
                            " New Save to a different vanilla path is allowed.";
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

                    string archiveBlockReason;
                    if (TryGetPersistenceArchiveBlockReason(
                            normalizedSidecarPath,
                            out archiveBlockReason))
                    {
                        errorMessage = archiveBlockReason;
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

            using (AcquirePersistenceTopologyReadLease())
            {
                object pathIoLock = GetPersistenceIoLock(snapshot.TargetPath);
                lock (pathIoLock)
                {
                    if (!IsPersistenceArchiveEpochCurrent(
                            snapshot.TargetPath,
                            snapshot.PathArchiveEpoch))
                    {
                        // The vanilla directory was deleted/archived after this
                        // snapshot was prepared. Treat the write as superseded so a
                        // queued persistence operation cannot resurrect that path.
                        return true;
                    }

                    string archiveBlockReason;
                    if (TryGetPersistenceArchiveBlockReason(
                            snapshot.TargetPath,
                            out archiveBlockReason))
                    {
                        errorMessage = archiveBlockReason;
                        return false;
                    }

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
                        recoveredBackupJournalPath = string.Empty;
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

                            // A New Save writes only checkpoints belonging to its target
                            // path, but it must not prune the active multi-path checkpoint
                            // ledger. Doing so destroys the prefix invariant used by a
                            // later Overwrite Save back to an older physical path.
                        }
                    }
                }

                if (scheduleBackgroundCompaction)
                {
                    QueueBackgroundCompaction(snapshot);
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
        }


        internal bool IsPersistenceSnapshotStillCurrent(
            LightweightPersistenceSnapshot snapshot)
        {
            if (snapshot == null ||
                string.IsNullOrEmpty(snapshot.TargetPath) ||
                !IsPersistenceArchiveEpochCurrent(
                    snapshot.TargetPath,
                    snapshot.PathArchiveEpoch))
            {
                return false;
            }

            lock (storageLock)
            {
                return !disposed &&
                    snapshot.Generation == nextPersistenceGeneration &&
                    string.Equals(
                        currentSidecarPath,
                        snapshot.TargetPath,
                        CorePaths.PathComparison);
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
            long baseMiB = Math.Max(
                0L,
                state.BaseFileBytes / (1024L * 1024L));
            long scaledTransactionThreshold =
                MinimumJournalTransactionsBeforeCompaction +
                Math.Min(
                    (long)MaximumJournalTransactionsBeforeCompaction -
                        MinimumJournalTransactionsBeforeCompaction,
                    baseMiB * JournalTransactionsPerBaseMiB);
            int transactionThreshold = (int)Math.Min(
                MaximumJournalTransactionsBeforeCompaction,
                scaledTransactionThreshold);

            // Bytes/ratio are the normal trigger. The transaction count is only a
            // high replay-cost safety ceiling, so a large base is never rewritten
            // merely because a few hundred tiny checkpoint-only saves accumulated.
            return state.JournalBytes >= byteThreshold ||
                state.JournalEntryCount >= transactionThreshold;
        }

        private void QueueBackgroundCompaction(
            LightweightPersistenceSnapshot persistenceSnapshot)
        {
            if (persistenceSnapshot == null)
            {
                return;
            }

            string targetPath = persistenceSnapshot.TargetPath;
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
                        RunBackgroundCompaction(persistenceSnapshot);
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
            LightweightPersistenceSnapshot persistenceSnapshot)
        {
            if (persistenceSnapshot == null)
            {
                return;
            }

            string targetPath = persistenceSnapshot.TargetPath;
            string relativeSavePath = persistenceSnapshot.RelativeSavePath;
            long generation = persistenceSnapshot.Generation;
            using (AcquirePersistenceTopologyReadLease())
            {
                object pathIoLock = GetPersistenceIoLock(targetPath);
                lock (pathIoLock)
                {
                    if (!IsPersistenceArchiveEpochCurrent(
                            targetPath,
                            persistenceSnapshot.PathArchiveEpoch))
                    {
                        return;
                    }

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

                LightweightSidecarDocument compactionDocument;
                lock (storageLock)
                {
                    if (!TryBuildBackgroundCompactionDocumentLocked(
                            persistenceSnapshot,
                            out compactionDocument))
                    {
                        return;
                    }
                }

                if (compactionDocument.Events.Count != expectedState.EventCount ||
                    compactionDocument.CustomMutations.Count !=
                        expectedState.CustomMutationCount ||
                    compactionDocument.Checkpoints.Count !=
                        expectedState.CheckpointCount ||
                    compactionDocument.LastIssuedSequence !=
                        expectedState.LastIssuedSequence)
                {
                    return;
                }

                string physicalError;
                if (!TryVerifyCompactionPhysicalBaseline(
                        targetPath,
                        expectedState,
                        out physicalError))
                {
                    CoreLog.Warn(
                        "IM Data Core skipped background compaction because the " +
                        "physical base/journal generation changed: " + physicalError);
                    return;
                }

                LightweightPersistenceSnapshot compactSnapshot =
                    new LightweightPersistenceSnapshot
                    {
                        TargetPath = targetPath,
                        RelativeSavePath = relativeSavePath,
                        Generation = generation,
                        PathArchiveEpoch =
                            persistenceSnapshot.PathArchiveEpoch,
                        PreserveExistingBackup = false,
                        StateRevision = expectedState.StateRevision,
                        IsIncremental = false,
                        BaseEventCount = 0,
                        BaseCustomMutationCount = 0,
                        BaseCheckpointCount = 0,
                        TotalEventCount = compactionDocument.Events.Count,
                        TotalCustomMutationCount =
                            compactionDocument.CustomMutations.Count,
                        TotalCheckpointCount =
                            compactionDocument.Checkpoints.Count,
                        Document = compactionDocument
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
        }

        private bool TryBuildBackgroundCompactionDocumentLocked(
            LightweightPersistenceSnapshot snapshot,
            out LightweightSidecarDocument document)
        {
            document = null;
            if (snapshot == null ||
                snapshot.Document == null ||
                disposed ||
                snapshot.StateRevision != activeStateRevision ||
                activeEvents.Count < snapshot.TotalEventCount ||
                activeCustomMutations.Count <
                    snapshot.TotalCustomMutationCount)
            {
                return false;
            }

            IReadOnlyList<LightweightCheckpointRecord> pathCheckpoints =
                GetActiveCheckpointsForPathLocked(snapshot.RelativeSavePath);
            if (pathCheckpoints.Count < snapshot.TotalCheckpointCount)
            {
                return false;
            }

            document = new LightweightSidecarDocument
            {
                FormatName = SidecarFormatName,
                FormatVersion = SidecarFormatVersion,
                RelativeSavePath = snapshot.RelativeSavePath,
                LastIssuedSequence = snapshot.Document.LastIssuedSequence,
                Events = CopyPrefix(activeEvents, snapshot.TotalEventCount),
                CustomMutations = CopyPrefix(
                    activeCustomMutations,
                    snapshot.TotalCustomMutationCount),
                Checkpoints = CopyPrefix(
                    pathCheckpoints,
                    snapshot.TotalCheckpointCount)
            };
            return true;
        }

        private static List<T> CopyPrefix<T>(
            IReadOnlyList<T> source,
            int count)
        {
            if (source == null || count < 0 || count > source.Count)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            List<T> prefix = new List<T>(count);
            for (int index = 0; index < count; index++)
            {
                prefix.Add(source[index]);
            }
            return prefix;
        }

        private static bool TryVerifyCompactionPhysicalBaseline(
            string targetPath,
            CommittedPathState expectedState,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (expectedState == null || !File.Exists(targetPath))
            {
                errorMessage = "The compact base snapshot no longer exists.";
                return false;
            }

            try
            {
                string physicalBaseHash;
                using (FileStream stream = new FileStream(
                    targetPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    64 * 1024,
                    FileOptions.SequentialScan))
                using (SHA256 sha256 = SHA256.Create())
                {
                    physicalBaseHash = ToLowerHex(sha256.ComputeHash(stream));
                }

                if (!string.Equals(
                        physicalBaseHash,
                        expectedState.BaseFileHash,
                        StringComparison.Ordinal))
                {
                    errorMessage =
                        "The compact base fingerprint no longer matches the committed generation.";
                    return false;
                }

                string journalPath = targetPath + ".imdc.journal";
                long physicalJournalBytes = File.Exists(journalPath)
                    ? new FileInfo(journalPath).Length
                    : 0L;
                if (physicalJournalBytes != expectedState.JournalBytes)
                {
                    errorMessage =
                        "The journal length no longer matches the committed generation.";
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
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
            // If the copy fails, deliberately keep the original journal in place.
            // Primary load rejects it by base hash, while backup recovery can pair
            // it with .bak through the preferred-journal recovery path.
            bool keepCurrentJournalForBackupRecovery = false;
            string backupJournalPath =
                snapshot.TargetPath + ".imdc.bak.imdc.journal";
            if (targetExisted && !snapshot.PreserveExistingBackup)
            {
                string backupJournalError;
                if (currentJournalExisted)
                {
                    if (!TryCopyContainedFileDurably(
                            currentJournalPath,
                            backupJournalPath,
                            out backupJournalError))
                    {
                        keepCurrentJournalForBackupRecovery = true;
                        CoreLog.Warn(
                            "IM Data Core could not preserve the previous journal " +
                            "with its backup base; the original journal will be kept " +
                            "as the backup recovery journal: " + backupJournalError);
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
            else if (targetExisted && snapshot.PreserveExistingBackup &&
                !string.IsNullOrEmpty(snapshot.BackupRecoveryJournalPath) &&
                string.Equals(
                    snapshot.BackupRecoveryJournalPath,
                    currentJournalPath,
                    CorePaths.PathComparison))
            {
                // Backup recovery used the primary journal to complete .imdc.bak.
                // The healing write deliberately preserves that backup base, so
                // publish the exact recovery journal beside it before deleting the
                // now-stale primary journal. If publication fails, retain the source
                // journal in place so B + J remains recoverable.
                string backupJournalError;
                if (currentJournalExisted)
                {
                    if (!TryCopyContainedFileDurably(
                            currentJournalPath,
                            backupJournalPath,
                            out backupJournalError))
                    {
                        keepCurrentJournalForBackupRecovery = true;
                        CoreLog.Warn(
                            "IM Data Core healed the primary sidecar but could not " +
                            "publish the recovery journal beside the preserved backup; " +
                            "the original journal will be kept: " +
                            backupJournalError);
                    }
                }
                else
                {
                    CoreLog.Warn(
                        "IM Data Core healed the primary sidecar after backup recovery, " +
                        "but the primary journal that completed the preserved backup " +
                        "was no longer present to publish as .imdc.bak.imdc.journal.");
                }
            }

            string cleanupError;
            if (!keepCurrentJournalForBackupRecovery &&
                !TryDeleteJournal(snapshot.TargetPath, out cleanupError) &&
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
                                "The existing IMDC journal format is unsupported by this build.";
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

        private static void ScavengeAbandonedTemporaryFilesForScope(
            string normalizedSidecarPath)
        {
            if (string.IsNullOrEmpty(normalizedSidecarPath))
            {
                return;
            }

            string directoryPath = Path.GetDirectoryName(normalizedSidecarPath);
            if (string.IsNullOrEmpty(directoryPath) ||
                !Directory.Exists(directoryPath))
            {
                return;
            }

            string sidecarFileName = Path.GetFileName(normalizedSidecarPath);
            string snapshotTemporaryPrefix =
                sidecarFileName + ".imdc.tmp.";
            string backupJournalTemporaryPrefix =
                sidecarFileName + ".imdc.bak.imdc.journal.tmp.";
            DateTime cutoffUtc = DateTime.UtcNow.Subtract(
                OrphanTemporaryFileMinimumAge);

            string[] candidates;
            try
            {
                candidates = Directory.GetFiles(directoryPath);
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "IM Data Core could not inspect its sidecar directory for " +
                    "abandoned temporary files: " + exception.Message);
                return;
            }

            for (int index = 0; index < candidates.Length; index++)
            {
                string candidatePath = candidates[index];
                string candidateFileName = Path.GetFileName(candidatePath);
                if (!candidateFileName.StartsWith(
                        snapshotTemporaryPrefix,
                        CorePaths.PathComparison) &&
                    !candidateFileName.StartsWith(
                        backupJournalTemporaryPrefix,
                        CorePaths.PathComparison))
                {
                    continue;
                }

                DateTime lastWriteUtc;
                try
                {
                    lastWriteUtc = File.GetLastWriteTimeUtc(candidatePath);
                }
                catch (Exception exception)
                {
                    CoreLog.Warn(
                        "IM Data Core could not inspect an abandoned temporary file: " +
                        exception.Message);
                    continue;
                }

                if (lastWriteUtc > cutoffUtc)
                {
                    continue;
                }

                string cleanupError;
                if (!CorePaths.TryDeleteContainedFile(
                        candidatePath,
                        out cleanupError) &&
                    !string.IsNullOrEmpty(cleanupError))
                {
                    CoreLog.Warn(
                        "IM Data Core could not remove an abandoned temporary file: " +
                        cleanupError);
                }
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

        private sealed class PersistenceTopologyLease : IDisposable
        {
            private readonly bool write;
            private bool disposedLease;

            internal PersistenceTopologyLease(bool writeLease)
            {
                write = writeLease;
                if (write)
                {
                    PersistenceTopologyLock.EnterWriteLock();
                }
                else
                {
                    PersistenceTopologyLock.EnterReadLock();
                }
            }

            public void Dispose()
            {
                if (disposedLease)
                {
                    return;
                }

                disposedLease = true;
                if (write)
                {
                    PersistenceTopologyLock.ExitWriteLock();
                }
                else
                {
                    PersistenceTopologyLock.ExitReadLock();
                }
            }
        }

        internal static IDisposable AcquirePersistenceTopologyReadLease()
        {
            return new PersistenceTopologyLease(false);
        }

        internal static IDisposable AcquirePersistenceTopologyWriteLease()
        {
            return new PersistenceTopologyLease(true);
        }

        internal static object GetSharedPersistenceIoLock(string targetPath)
        {
            string key = NormalizePersistenceRegistryPath(targetPath);
            lock (PersistenceIoRegistryLock)
            {
                object pathLock;
                if (!PersistenceIoLocksByPath.TryGetValue(key, out pathLock))
                {
                    pathLock = new object();
                    PersistenceIoLocksByPath[key] = pathLock;
                }

                if (!PersistenceArchiveEpochByPath.ContainsKey(key))
                {
                    PersistenceArchiveEpochByPath[key] = 0L;
                }
                return pathLock;
            }
        }

        private static object GetPersistenceIoLock(string targetPath)
        {
            return GetSharedPersistenceIoLock(targetPath);
        }

        private static long GetPersistenceArchiveEpoch(string targetPath)
        {
            string key = NormalizePersistenceRegistryPath(targetPath);
            lock (PersistenceIoRegistryLock)
            {
                long epoch;
                if (!PersistenceArchiveEpochByPath.TryGetValue(key, out epoch))
                {
                    epoch = 0L;
                    PersistenceArchiveEpochByPath[key] = epoch;
                }

                if (!PersistenceIoLocksByPath.ContainsKey(key))
                {
                    PersistenceIoLocksByPath[key] = new object();
                }

                foreach (string archiveDirectory in
                    PersistenceArchiveInProgressDirectories)
                {
                    if (PathIsSameOrContained(archiveDirectory, key))
                    {
                        // A snapshot prepared while archival owns the topology write
                        // lease must never become writable after the rename. This
                        // sentinel can never equal a registered non-negative epoch.
                        return long.MinValue;
                    }
                }
                return epoch;
            }
        }

        private static bool IsPersistenceArchiveEpochCurrent(
            string targetPath,
            long expectedEpoch)
        {
            string key = NormalizePersistenceRegistryPath(targetPath);
            lock (PersistenceIoRegistryLock)
            {
                long currentEpoch;
                return PersistenceArchiveEpochByPath.TryGetValue(
                        key,
                        out currentEpoch) &&
                    currentEpoch == expectedEpoch;
            }
        }

        internal static void BeginPersistenceArchiveBoundaryForDirectory(
            string sidecarDirectoryPath)
        {
            if (!PersistenceTopologyLock.IsWriteLockHeld)
            {
                throw new InvalidOperationException(
                    "The IMDC persistence topology write lease is required before " +
                    "beginning an archive boundary.");
            }

            string normalizedDirectory = NormalizePersistenceRegistryPath(
                sidecarDirectoryPath);
            if (string.IsNullOrEmpty(normalizedDirectory))
            {
                throw new ArgumentException(
                    "The IMDC archive directory path is empty.",
                    "sidecarDirectoryPath");
            }

            lock (PersistenceIoRegistryLock)
            {
                PersistenceArchiveInProgressDirectories.Add(normalizedDirectory);
            }
        }

        internal static void CompletePersistenceArchiveBoundaryForDirectory(
            string sidecarDirectoryPath,
            bool archiveSucceeded)
        {
            if (!PersistenceTopologyLock.IsWriteLockHeld)
            {
                throw new InvalidOperationException(
                    "The IMDC persistence topology write lease is required before " +
                    "completing an archive boundary.");
            }

            string normalizedDirectory = NormalizePersistenceRegistryPath(
                sidecarDirectoryPath);
            if (string.IsNullOrEmpty(normalizedDirectory))
            {
                throw new ArgumentException(
                    "The IMDC archive directory path is empty.",
                    "sidecarDirectoryPath");
            }

            lock (PersistenceIoRegistryLock)
            {
                // Increment only at the end of the archive boundary. Snapshot
                // preparation is allowed to run without the topology lease, so any
                // path registered while archival is in progress must receive the old
                // epoch and become stale here. A genuinely later save then receives
                // the new epoch and may reuse the vanilla path safely.
                List<string> keys = new List<string>(
                    PersistenceArchiveEpochByPath.Keys);
                for (int index = 0; index < keys.Count; index++)
                {
                    string key = keys[index];
                    if (!PathIsSameOrContained(
                            normalizedDirectory,
                            key))
                    {
                        continue;
                    }

                    long epoch = PersistenceArchiveEpochByPath[key];
                    PersistenceArchiveEpochByPath[key] =
                        epoch == long.MaxValue ? 0L : epoch + 1L;
                }

                if (archiveSucceeded)
                {
                    List<string> blocked = new List<string>(
                        PersistenceArchiveBlockedDirectories);
                    for (int index = 0; index < blocked.Count; index++)
                    {
                        if (PathIsSameOrContained(
                                normalizedDirectory,
                                blocked[index]))
                        {
                            PersistenceArchiveBlockedDirectories.Remove(
                                blocked[index]);
                        }
                    }
                }
                else
                {
                    // The vanilla save is already gone, but the IMDC directory could
                    // not be renamed. Preserve that orphan verbatim: later persistence
                    // into the same directory is blocked for this process rather than
                    // risking destruction of the diary the archive was meant to keep.
                    PersistenceArchiveBlockedDirectories.Add(normalizedDirectory);
                }

                PersistenceArchiveInProgressDirectories.Remove(normalizedDirectory);
            }
        }

        private static bool TryGetPersistenceArchiveBlockReason(
            string targetPath,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            string normalizedTarget = NormalizePersistenceRegistryPath(targetPath);
            lock (PersistenceIoRegistryLock)
            {
                foreach (string blockedDirectory in
                    PersistenceArchiveBlockedDirectories)
                {
                    if (!PathIsSameOrContained(
                            blockedDirectory,
                            normalizedTarget))
                    {
                        continue;
                    }

                    errorMessage =
                        "IM Data Core persistence is blocked for this deleted-save " +
                        "directory because its preservation rename failed earlier " +
                        "in this process. The existing supplemental files were left " +
                        "untouched.";
                    return true;
                }
            }
            return false;
        }

        private static string NormalizePersistenceRegistryPath(string targetPath)
        {
            string key = targetPath ?? string.Empty;
            if (!string.IsNullOrEmpty(key))
            {
                try
                {
                    key = Path.GetFullPath(key);
                }
                catch
                {
                    // Callers validate physical mutation paths before I/O. Keep a
                    // stable fallback key here so synchronization itself never turns
                    // a supplemental persistence failure into a vanilla one.
                }
            }
            return key;
        }

        private static bool PathIsSameOrContained(
            string parentDirectory,
            string candidatePath)
        {
            if (string.IsNullOrEmpty(parentDirectory) ||
                string.IsNullOrEmpty(candidatePath))
            {
                return false;
            }

            string normalizedParent = NormalizePersistenceRegistryPath(
                parentDirectory);
            string normalizedCandidate = NormalizePersistenceRegistryPath(
                candidatePath);
            if (string.Equals(
                    normalizedParent,
                    normalizedCandidate,
                    CorePaths.PathComparison))
            {
                return true;
            }

            string prefix = normalizedParent.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return normalizedCandidate.StartsWith(
                prefix,
                CorePaths.PathComparison);
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
            return TryLoadValidatedDocumentFromPathLocked(
                path,
                expectedRelativeSavePath,
                null,
                out document,
                out persistenceInfo,
                out errorMessage);
        }

        private bool TryLoadValidatedDocumentFromPathLocked(
            string path,
            string expectedRelativeSavePath,
            string preferredJournalPath,
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

                DocumentValidationState validationState;
                if (!TryValidateDocumentLocked(
                        document,
                        expectedRelativeSavePath,
                        out validationState,
                        out errorMessage))
                {
                    document = null;
                    return false;
                }

                int baseCheckpointCount = document.Checkpoints.Count;
                int baseEventCount = document.Events.Count;
                int baseCustomMutationCount = document.CustomMutations.Count;

                if (!TryReplayJournalLocked(
                        path,
                        preferredJournalPath,
                        persistenceInfo.BaseFileHash,
                        document,
                        persistenceInfo,
                        out errorMessage))
                {
                    document = null;
                    return false;
                }

                if (persistenceInfo.JournalEntryCount > 0 &&
                    !TryValidateDocumentSuffixLocked(
                        document,
                        baseEventCount,
                        baseCustomMutationCount,
                        baseCheckpointCount,
                        validationState,
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
            string preferredJournalPath,
            string baseFileHash,
            LightweightSidecarDocument document,
            LightweightLoadedPersistenceInfo persistenceInfo,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            string defaultJournalPath = basePath + ".imdc.journal";

            // During atomic compaction File.Replace has already moved the prior base
            // to .bak before its matching journal is copied to the backup-journal name.
            // If the process dies in that narrow interval, the still-present primary
            // journal can be the one that belongs to the backup base. Prefer it during
            // backup recovery, but only treat it as authoritative after a real header
            // was parsed and its base hash matched. Empty/torn preferred journals must
            // not mask a valid sibling .bak.imdc.journal.
            if (!string.IsNullOrEmpty(preferredJournalPath) &&
                !string.Equals(
                    preferredJournalPath,
                    defaultJournalPath,
                    CorePaths.PathComparison) &&
                File.Exists(preferredJournalPath))
            {
                LightweightJournalReplayStatus preferredStatus;
                string preferredError;
                if (TryReplayJournalFileLocked(
                        preferredJournalPath,
                        baseFileHash,
                        document,
                        persistenceInfo,
                        out preferredStatus,
                        out preferredError))
                {
                    if (preferredStatus ==
                        LightweightJournalReplayStatus.HeaderMatched)
                    {
                        return true;
                    }

                    // Missing/torn/mismatched preferred journals did not contribute
                    // rows to the recovered document. Discard their diagnostics and
                    // inspect the backup-base sibling journal instead.
                    ResetJournalReplayDiagnostics(persistenceInfo);
                }
                else
                {
                    // Replay may have applied one or more fully committed rows before
                    // discovering later corruption. Do not attempt a second journal
                    // against that potentially advanced document.
                    errorMessage = preferredError;
                    return false;
                }
            }

            LightweightJournalReplayStatus defaultStatus;
            if (!TryReplayJournalFileLocked(
                    defaultJournalPath,
                    baseFileHash,
                    document,
                    persistenceInfo,
                    out defaultStatus,
                    out errorMessage))
            {
                return false;
            }

            if (defaultStatus == LightweightJournalReplayStatus.HeaderMismatch ||
                defaultStatus == LightweightJournalReplayStatus.TornBeforeHeader)
            {
                // An atomically replaced base can legitimately leave a stale or
                // first-header-torn journal behind. The compact base remains
                // authoritative, but the next persistence boundary should rewrite a
                // clean snapshot rather than append to that journal.
                persistenceInfo.ForceFullSnapshot = true;
            }

            return true;
        }

        private static void ResetJournalReplayDiagnostics(
            LightweightLoadedPersistenceInfo persistenceInfo)
        {
            if (persistenceInfo == null)
            {
                return;
            }

            persistenceInfo.JournalBytes = 0L;
            persistenceInfo.JournalEntryCount = 0;
            persistenceInfo.ForceFullSnapshot = false;
            persistenceInfo.ReplayedJournalPath = string.Empty;
        }

        private bool TryReplayJournalFileLocked(
            string journalPath,
            string baseFileHash,
            LightweightSidecarDocument document,
            LightweightLoadedPersistenceInfo persistenceInfo,
            out LightweightJournalReplayStatus replayStatus,
            out string errorMessage)
        {
            replayStatus = LightweightJournalReplayStatus.Missing;
            errorMessage = string.Empty;
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
                replayStatus = LightweightJournalReplayStatus.TornBeforeHeader;
                persistenceInfo.ForceFullSnapshot = true;
                return true;
            }

            bool endsWithNewline;
            using (FileStream tailStream = new FileStream(
                normalizedJournalPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.RandomAccess))
            {
                tailStream.Seek(-1L, SeekOrigin.End);
                endsWithNewline = tailStream.ReadByte() == '\n';
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
                        // invalidate the already-fsynced base snapshot. Crucially,
                        // this is not evidence that the journal belongs to the base.
                        replayStatus =
                            LightweightJournalReplayStatus.TornBeforeHeader;
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
                    replayStatus =
                        LightweightJournalReplayStatus.HeaderMismatch;
                    return true;
                }

                replayStatus = LightweightJournalReplayStatus.HeaderMatched;
                persistenceInfo.ReplayedJournalPath = normalizedJournalPath;

                if (journalFormatVersion != JournalFormatVersion)
                {
                    errorMessage =
                        "The IMDC journal format is unsupported by this IM Data Core version.";
                    return false;
                }

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
                        "The IMDC journal contains an invalid transaction: " +
                        replayError;
                    return false;
                }

                persistenceInfo.JournalEntryCount += replayedEntryCount;
                persistenceInfo.ForceFullSnapshot |= forceFullSnapshot;
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
            DocumentValidationState ignoredState;
            return TryValidateDocumentLocked(
                document,
                expectedRelativeSavePath,
                out ignoredState,
                out errorMessage);
        }

        private bool TryValidateDocumentLocked(
            LightweightSidecarDocument document,
            string expectedRelativeSavePath,
            out DocumentValidationState validationState,
            out string errorMessage)
        {
            validationState = null;
            errorMessage = string.Empty;
            if (document == null)
            {
                errorMessage = "The sidecar JSON is empty or invalid.";
                return false;
            }

            // Runtime IMDC accepts exactly the current sidecar schema. Historical
            // format migration belongs in an external migrator, not this reader.
            if (!string.Equals(
                    document.FormatName,
                    SidecarFormatName,
                    StringComparison.Ordinal) ||
                document.FormatVersion != SidecarFormatVersion)
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

            validationState = new DocumentValidationState();
            if (!TryValidateDocumentRowsLocked(
                    document,
                    0,
                    0,
                    0,
                    validationState,
                    out errorMessage))
            {
                validationState = null;
                return false;
            }

            return true;
        }

        private bool TryValidateDocumentSuffixLocked(
            LightweightSidecarDocument document,
            int eventStartIndex,
            int customMutationStartIndex,
            int checkpointStartIndex,
            DocumentValidationState validationState,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (document == null ||
                validationState == null ||
                eventStartIndex < 0 ||
                eventStartIndex > document.Events.Count ||
                customMutationStartIndex < 0 ||
                customMutationStartIndex > document.CustomMutations.Count ||
                checkpointStartIndex < 0 ||
                checkpointStartIndex > document.Checkpoints.Count)
            {
                errorMessage =
                    "The IMDC journal validation suffix is inconsistent.";
                return false;
            }

            return TryValidateDocumentRowsLocked(
                document,
                eventStartIndex,
                customMutationStartIndex,
                checkpointStartIndex,
                validationState,
                out errorMessage);
        }

        private bool TryValidateDocumentRowsLocked(
            LightweightSidecarDocument document,
            int eventStartIndex,
            int customMutationStartIndex,
            int checkpointStartIndex,
            DocumentValidationState validationState,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            for (int index = eventStartIndex;
                index < document.Events.Count;
                index++)
            {
                LightweightEventRecord record = document.Events[index];
                if (record == null ||
                    record.Sequence <= 0L ||
                    record.Sequence <= validationState.LastEventSequence ||
                    !validationState.Sequences.Add(record.Sequence))
                {
                    errorMessage =
                        "The sidecar contains an invalid, duplicate, or out-of-order event sequence.";
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
                        !validationState.EventIdempotencyKeys.Add(
                            BuildCustomEventIdempotencyCompositeKey(
                                record.NamespaceIdentifier,
                                record.IdempotencyKey)))
                    {
                        errorMessage =
                            "The sidecar contains an invalid or duplicate custom-event idempotency key.";
                        return false;
                    }
                }

                if (record.PayloadJson == null)
                {
                    errorMessage =
                        "The sidecar contains an invalid event payload.";
                    return false;
                }

                validationState.LastEventSequence = record.Sequence;
                validationState.MaximumSequence = Math.Max(
                    validationState.MaximumSequence,
                    record.Sequence);
            }

            for (int index = customMutationStartIndex;
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
                    mutation.Sequence <=
                        validationState.LastCustomMutationSequence ||
                    !validationState.Sequences.Add(mutation.Sequence))
                {
                    errorMessage =
                        "The sidecar contains an invalid, duplicate, or out-of-order custom-data mutation.";
                    return false;
                }

                if (operationIsSet)
                {
                    if (mutation.ValueJson == null)
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

                validationState.LastCustomMutationSequence = mutation.Sequence;
                validationState.MaximumSequence = Math.Max(
                    validationState.MaximumSequence,
                    mutation.Sequence);
            }

            for (int index = checkpointStartIndex;
                index < document.Checkpoints.Count;
                index++)
            {
                LightweightCheckpointRecord checkpoint =
                    document.Checkpoints[index];

                if (checkpoint == null ||
                    checkpoint.Sequence < 0L ||
                    checkpoint.Sequence > document.LastIssuedSequence ||
                    !VanillaSavedDataFingerprint.IsValid(
                        checkpoint.ContentFingerprint) ||
                    checkpoint.AgencyRoomIdentities == null ||
                    string.IsNullOrEmpty(
                        VanillaSaveStamp.NormalizeRelativePath(
                            checkpoint.RelativeSavePath)))
                {
                    errorMessage =
                        "The sidecar contains an invalid checkpoint.";
                    return false;
                }

                HashSet<string> roomEntityIds =
                    new HashSet<string>(StringComparer.Ordinal);
                for (int roomIdentityIndex = 0;
                    roomIdentityIndex < checkpoint.AgencyRoomIdentities.Count;
                    roomIdentityIndex++)
                {
                    LightweightAgencyRoomIdentityRecord roomIdentity =
                        checkpoint.AgencyRoomIdentities[roomIdentityIndex];
                    if (roomIdentity == null ||
                        string.IsNullOrEmpty(roomIdentity.EntityId) ||
                        roomIdentity.FloorIndex < 0 ||
                        roomIdentity.RoomIndex < 0 ||
                        roomIdentity.RoomTypeRaw < 0 ||
                        !roomEntityIds.Add(roomIdentity.EntityId))
                    {
                        errorMessage =
                            "The sidecar contains an invalid or duplicate agency-room identity snapshot entry.";
                        return false;
                    }
                }

                if (!validationState.CheckpointIdentities.Add(
                        CheckpointIdentity.From(checkpoint)))
                {
                    errorMessage =
                        "The sidecar contains duplicate checkpoint identities.";
                    return false;
                }
            }

            if (document.LastIssuedSequence <
                validationState.MaximumSequence)
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

            // Current v5 persistence is written in ascending sequence order.
            // Runtime appends/rollback preserve that order, so avoid O(N log N)
            // sorting on every load/rebuild unless a defensive monotonic scan finds
            // that an in-memory transform actually disturbed it.
            if (!IsEventSequenceOrdered(activeEvents))
            {
                activeEvents.Sort(CompareEventsBySequenceAscending);
            }
            if (!IsCustomMutationSequenceOrdered(activeCustomMutations))
            {
                activeCustomMutations.Sort(
                    CompareCustomMutationsBySequenceAscending);
            }

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
            else
            {
                try
                {
                    gameDate = ExtensionMethods.ToDateTime(
                        checkpoint.GameDateTime);
                }
                catch
                {
                    maxActiveCheckpointGameDate = DateTime.MaxValue;
                    return;
                }
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
            long pathArchiveEpoch = GetPersistenceArchiveEpoch(
                normalizedTargetPath);
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
                    PathArchiveEpoch = pathArchiveEpoch,
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
                PathArchiveEpoch = pathArchiveEpoch,
                PreserveExistingBackup = preserveExistingBackup,
                BackupRecoveryJournalPath = preserveExistingBackup
                    ? recoveredBackupJournalPath ?? string.Empty
                    : string.Empty,
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
            recoveredBackupJournalPath = string.Empty;
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
            private readonly string contentFingerprint;

            private CheckpointIdentity(
                string relativeSavePath,
                string lastSave,
                long playtimeSeconds,
                string gameDateTime,
                string contentFingerprint)
            {
                this.relativeSavePath = relativeSavePath ?? string.Empty;
                this.lastSave = lastSave ?? string.Empty;
                this.playtimeSeconds = playtimeSeconds;
                this.gameDateTime = gameDateTime ?? string.Empty;
                this.contentFingerprint = contentFingerprint ?? string.Empty;
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
                    stamp != null ? stamp.GameDateTime : string.Empty,
                    stamp != null ? stamp.ContentFingerprint : string.Empty);
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
                    checkpoint != null ? checkpoint.GameDateTime : string.Empty,
                    checkpoint != null
                        ? checkpoint.ContentFingerprint
                        : string.Empty);
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
                        other.gameDateTime) &&
                    StringComparer.Ordinal.Equals(
                        contentFingerprint,
                        other.contentFingerprint);
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
                    hash = (hash * 31) +
                        StringComparer.Ordinal.GetHashCode(
                            contentFingerprint ?? string.Empty);
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
                    StringComparison.Ordinal) &&
                string.Equals(
                    left.ContentFingerprint,
                    right.ContentFingerprint,
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

        private static bool IsEventSequenceOrdered(
            IReadOnlyList<LightweightEventRecord> records)
        {
            long previousSequence = 0L;
            if (records == null)
            {
                return true;
            }

            for (int index = 0; index < records.Count; index++)
            {
                LightweightEventRecord record = records[index];
                if (record == null || record.Sequence <= previousSequence)
                {
                    return false;
                }
                previousSequence = record.Sequence;
            }
            return true;
        }

        private static bool IsCustomMutationSequenceOrdered(
            IReadOnlyList<LightweightCustomMutationRecord> records)
        {
            long previousSequence = 0L;
            if (records == null)
            {
                return true;
            }

            for (int index = 0; index < records.Count; index++)
            {
                LightweightCustomMutationRecord record = records[index];
                if (record == null || record.Sequence <= previousSequence)
                {
                    return false;
                }
                previousSequence = record.Sequence;
            }
            return true;
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

        private static List<LightweightModSnapshotRecord> CloneModSnapshots(
            IReadOnlyList<LightweightModSnapshotRecord> source)
        {
            List<LightweightModSnapshotRecord> clone =
                new List<LightweightModSnapshotRecord>();
            if (source == null)
            {
                return clone;
            }

            for (int index = 0; index < source.Count; index++)
            {
                LightweightModSnapshotRecord row = source[index];
                if (row == null)
                {
                    continue;
                }

                clone.Add(new LightweightModSnapshotRecord
                {
                    ModName = row.ModName ?? string.Empty,
                    Title = row.Title ?? string.Empty,
                    Author = row.Author ?? string.Empty,
                    Version = row.Version ?? string.Empty,
                    DllNames = row.DllNames != null
                        ? new List<string>(row.DllNames)
                        : new List<string>()
                });
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
                ContentFingerprint = source.ContentFingerprint ?? string.Empty,
                Sequence = source.Sequence,
                EnabledMods = CloneModSnapshots(source.EnabledMods),
                AgencyRoomIdentities = CloneAgencyRoomIdentities(source.AgencyRoomIdentities)
            };
        }

        private static List<LightweightAgencyRoomIdentityRecord> CloneAgencyRoomIdentities(
            IReadOnlyList<LightweightAgencyRoomIdentityRecord> source)
        {
            if (source == null)
            {
                return new List<LightweightAgencyRoomIdentityRecord>();
            }

            List<LightweightAgencyRoomIdentityRecord> clone =
                new List<LightweightAgencyRoomIdentityRecord>(source.Count);
            for (int index = 0; index < source.Count; index++)
            {
                LightweightAgencyRoomIdentityRecord record = source[index];
                if (record == null)
                {
                    continue;
                }

                clone.Add(new LightweightAgencyRoomIdentityRecord
                {
                    EntityId = record.EntityId ?? string.Empty,
                    FloorIndex = record.FloorIndex,
                    RoomIndex = record.RoomIndex,
                    RoomTypeRaw = record.RoomTypeRaw,
                    TheaterId = record.TheaterId
                });
            }

            return clone;
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
