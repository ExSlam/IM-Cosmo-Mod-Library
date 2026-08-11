using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace IMDataCore
{
    /// <summary>
    /// Outcome of the deliberately narrow legacy fallback import path. Distinct
    /// no-source, no-checkpoint, and unsupported outcomes let the controller log
    /// an accurate compatibility limitation without treating vanilla loading as
    /// a failure.
    /// </summary>
    internal enum LegacyFlatFileImportStatus
    {
        Imported,
        NoLegacySource,
        NoExactCheckpoint,
        UnsupportedLegacyFormat,
        InvalidLegacyData,
        AmbiguousMatch,
        InvalidTarget,
        ImportFailed
    }

    internal sealed class LegacyFlatFileImportResult
    {
        internal LegacyFlatFileImportStatus Status;
        internal string Message = string.Empty;
        internal string SourcePath = string.Empty;
        internal int ImportedEventCount;
        internal int ImportedCustomDataCount;

        internal bool Succeeded
        {
            get { return Status == LegacyFlatFileImportStatus.Imported; }
        }
    }

    /// <summary>
    /// Read-only bridge for the exact-generation JSON fallback emitted by the
    /// late 1.3 storage engine.
    ///
    /// This is intentionally not a general legacy backend. It never initializes
    /// a legacy engine and never creates, promotes, repairs, moves, deletes, or
    /// writes a legacy file. SQLite and early/unversioned JSON formats are left
    /// untouched because their association with one loaded vanilla checkpoint
    /// cannot be established by this small, safe importer.
    /// </summary>
    internal static class LegacyFlatFileImporter
    {
        private const int SupportedLegacyFormatVersion = 2;
        private const int MaximumRetainedSaveGenerations = 8;
        private const int UnknownLegacyPushDayCount = -1;
        private const string LegacyFallbackFileName =
            "im_data_core.fallback.json";
        private const string LegacyBackupFileSuffix = ".bak";
        private const string LegacyTemporaryFileSuffix = ".tmp";
        private const string FingerprintVersionPrefix = "v1";
        private const char JsonArrayStartCharacter = '[';
        private const char JsonArrayEndCharacter = ']';

#pragma warning disable CS0649 // JsonUtility assigns these reflection-only DTO fields.
        [Serializable]
        private sealed class LegacyFormatProbe
        {
            public int FormatVersion;
        }

        /// <summary>
        /// Exact field order and types of FlatFileState in late IMDC 1.3. Unity's
        /// JsonUtility serializes fields in declaration order, so this projection
        /// must remain byte-compatible for integrity verification.
        /// </summary>
        [Serializable]
        private sealed class LegacyFlatFileState
        {
            public int FormatVersion;
            public string IntegritySha256 = string.Empty;
            public long NextEventId = 1L;
            public List<LegacyEventRecord> Events =
                new List<LegacyEventRecord>();
            public List<LegacyCustomDataRecord> CustomData =
                new List<LegacyCustomDataRecord>();
            public List<LegacySingleParticipationRecord> SingleParticipation =
                new List<LegacySingleParticipationRecord>();
            public List<LegacyStatusWindowRecord> StatusWindows =
                new List<LegacyStatusWindowRecord>();
            public List<LegacyShowCastWindowRecord> ShowCastWindows =
                new List<LegacyShowCastWindowRecord>();
            public List<LegacyContractWindowRecord> ContractWindows =
                new List<LegacyContractWindowRecord>();
            public List<LegacyRelationshipWindowRecord> RelationshipWindows =
                new List<LegacyRelationshipWindowRecord>();
            public List<LegacyTourParticipationRecord> TourParticipation =
                new List<LegacyTourParticipationRecord>();
            public List<LegacyAwardResultRecord> AwardResults =
                new List<LegacyAwardResultRecord>();
            public List<LegacyElectionResultRecord> ElectionResults =
                new List<LegacyElectionResultRecord>();
            public List<LegacyPushWindowRecord> PushWindows =
                new List<LegacyPushWindowRecord>();
            public string CheckpointFingerprint = string.Empty;
            public long CheckpointEventWatermark;
            public string CheckpointSnapshotJson = string.Empty;
            public string CheckpointCreatedUtc = string.Empty;
            public List<LegacyCheckpointRecord> Checkpoints =
                new List<LegacyCheckpointRecord>();
        }

        [Serializable]
        private sealed class LegacyCheckpointRecord
        {
            public string Fingerprint = string.Empty;
            public long EventWatermark;
            public string SnapshotJson = string.Empty;
            public string CreatedUtc = string.Empty;
        }

        [Serializable]
        private sealed class LegacyEventRecord
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
        private sealed class LegacyCustomDataRecord
        {
            public string NamespaceIdentifier = string.Empty;
            public string DataKey = string.Empty;
            public string ValueJson = string.Empty;
            public string UpdatedUtc = string.Empty;
        }

        // Projection fields remain in this reader solely because they contribute
        // to the legacy integrity hash. They are validated and then intentionally
        // omitted: version 2 imports events plus custom state, not redundant old
        // materialized projections.
        [Serializable]
        private sealed class LegacySingleParticipationRecord
        {
            public int SingleId;
            public int IdolId;
            public int RowIndex;
            public int PositionIndex;
            public int IsCenterFlag;
            public string ReleaseDate = string.Empty;
        }

        [Serializable]
        private sealed class LegacyStatusWindowRecord
        {
            public int IdolId;
            public string StatusType = string.Empty;
            public string StartDate = string.Empty;
            public string EndDate = string.Empty;
        }

        [Serializable]
        private sealed class LegacyShowCastWindowRecord
        {
            public string ShowId = string.Empty;
            public int IdolId = CoreConstants.InvalidIdValue;
            public string StartDate = string.Empty;
            public string EndDate = string.Empty;
            public string EndReason = string.Empty;
            public string PayloadJson = CoreConstants.EmptyJsonObject;
        }

        [Serializable]
        private sealed class LegacyContractWindowRecord
        {
            public string ContractKey = string.Empty;
            public int IdolId = CoreConstants.InvalidIdValue;
            public string StartDate = string.Empty;
            public string EndDate = string.Empty;
            public string EndReason = string.Empty;
            public string PayloadJson = CoreConstants.EmptyJsonObject;
        }

        [Serializable]
        private sealed class LegacyRelationshipWindowRecord
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
        private sealed class LegacyTourParticipationRecord
        {
            public string TourId = string.Empty;
            public int IdolId = CoreConstants.InvalidIdValue;
            public string LifecycleAction = string.Empty;
            public string EventDate = string.Empty;
            public string PayloadJson = CoreConstants.EmptyJsonObject;
        }

        [Serializable]
        private sealed class LegacyAwardResultRecord
        {
            public string AwardKey = string.Empty;
            public int IdolId = CoreConstants.InvalidIdValue;
            public string EventDate = string.Empty;
            public string PayloadJson = CoreConstants.EmptyJsonObject;
        }

        [Serializable]
        private sealed class LegacyElectionResultRecord
        {
            public string ElectionId = string.Empty;
            public int IdolId = CoreConstants.InvalidIdValue;
            public string EventDate = string.Empty;
            public string PayloadJson = CoreConstants.EmptyJsonObject;
        }

        [Serializable]
        private sealed class LegacyPushWindowRecord
        {
            public string SlotKey = string.Empty;
            public int IdolId = CoreConstants.InvalidIdValue;
            public string StartDate = string.Empty;
            public string EndDate = string.Empty;
            public int LastDaysInSlot =
                UnknownLegacyPushDayCount;
            public string EndReason = string.Empty;
            public string PayloadJson = CoreConstants.EmptyJsonObject;
        }
#pragma warning restore CS0649

        private sealed class ExactCheckpointMatch
        {
            internal string SourcePath = string.Empty;
            internal LegacyCheckpointRecord Checkpoint;
            internal LegacyFlatFileState Snapshot;
        }

        private sealed class UnsupportedLegacyFormatException : Exception
        {
            internal UnsupportedLegacyFormatException(string message)
                : base(message)
            {
            }
        }

        /// <summary>
        /// Imports the snapshot whose old content fingerprint exactly matches the
        /// already-deserialized vanilla SavedData. The vanilla file is not opened.
        /// The target must be a pristine, physical lightweight engine so a failed
        /// import can never merge an uncertain legacy branch into existing state.
        /// </summary>
        internal static LegacyFlatFileImportResult TryImportExactGeneration(
            CoreSaveScope saveScope,
            SaveManager.SavedData loadedSaveData,
            LightweightCoreStorageEngine target)
        {
            LegacyFlatFileImportResult targetError = ValidateTarget(
                saveScope,
                loadedSaveData,
                target);
            if (targetError != null)
            {
                return targetError;
            }

            List<string> legacyFiles = CorePaths.GetExistingLegacyStorageFiles(
                saveScope,
                loadedSaveData.staticVars__PlayerData);
            List<string> fallbackFiles = new List<string>();
            for (int fileIndex = 0;
                fileIndex < legacyFiles.Count;
                fileIndex++)
            {
                if (IsLegacyFallbackFile(legacyFiles[fileIndex]))
                {
                    fallbackFiles.Add(legacyFiles[fileIndex]);
                }
            }

            if (fallbackFiles.Count == 0)
            {
                return CreateResult(
                    LegacyFlatFileImportStatus.NoLegacySource,
                    "No supported legacy fallback JSON source was found.");
            }

            string expectedFingerprint;
            try
            {
                expectedFingerprint = BuildLoadedSaveFingerprint(loadedSaveData);
            }
            catch (Exception exception)
            {
                return CreateResult(
                    LegacyFlatFileImportStatus.ImportFailed,
                    "The loaded vanilla save fingerprint could not be reproduced in memory: " +
                    exception.Message);
            }

            List<ExactCheckpointMatch> matches =
                new List<ExactCheckpointMatch>();
            bool fallbackSourceFound = false;
            bool supportedSourceFound = false;
            bool invalidSourceFound = false;
            string firstUnsupportedMessage = string.Empty;
            string firstInvalidMessage = string.Empty;

            for (int fileIndex = 0;
                fileIndex < fallbackFiles.Count;
                fileIndex++)
            {
                string sourcePath = fallbackFiles[fileIndex];

                fallbackSourceFound = true;
                try
                {
                    // This is the only legacy-source I/O in the importer.
                    string rawJson = File.ReadAllText(
                        sourcePath,
                        Encoding.UTF8);
                    LegacyFlatFileState state = DeserializeVersionTwo(rawJson);
                    supportedSourceFound = true;

                    for (int checkpointIndex = 0;
                        checkpointIndex < state.Checkpoints.Count;
                        checkpointIndex++)
                    {
                        LegacyCheckpointRecord checkpoint =
                            state.Checkpoints[checkpointIndex];
                        if (!string.Equals(
                            checkpoint.Fingerprint,
                            expectedFingerprint,
                            StringComparison.Ordinal))
                        {
                            continue;
                        }

                        matches.Add(
                            new ExactCheckpointMatch
                            {
                                SourcePath = sourcePath,
                                Checkpoint = checkpoint,
                                // Checkpoint history validation already parsed this
                                // snapshot. Parse it once more to retain the exact
                                // selected state without retaining all generations.
                                Snapshot = DeserializeVersionTwo(
                                    checkpoint.SnapshotJson)
                            });
                    }
                }
                catch (UnsupportedLegacyFormatException exception)
                {
                    if (string.IsNullOrEmpty(firstUnsupportedMessage))
                    {
                        firstUnsupportedMessage = sourcePath + ": " +
                            exception.Message;
                    }
                }
                catch (Exception exception)
                {
                    invalidSourceFound = true;
                    if (string.IsNullOrEmpty(firstInvalidMessage))
                    {
                        firstInvalidMessage = sourcePath + ": " +
                            exception.Message;
                    }
                }
            }

            if (matches.Count == 0)
            {
                if (supportedSourceFound)
                {
                    return CreateResult(
                        LegacyFlatFileImportStatus.NoExactCheckpoint,
                        "A valid legacy format-2 fallback was found, but it has no " +
                        "checkpoint matching the loaded vanilla save.");
                }

                if (invalidSourceFound)
                {
                    return CreateResult(
                        LegacyFlatFileImportStatus.InvalidLegacyData,
                        "Legacy fallback data was found but failed exact integrity " +
                        "validation. " + firstInvalidMessage);
                }

                if (fallbackSourceFound)
                {
                    return CreateResult(
                        LegacyFlatFileImportStatus.UnsupportedLegacyFormat,
                        "Legacy fallback data was found, but it is not the supported " +
                        "late-1.3 format-2 checkpoint format. " +
                        firstUnsupportedMessage);
                }

                return CreateResult(
                    LegacyFlatFileImportStatus.NoLegacySource,
                    "No supported legacy fallback JSON source was found.");
            }

            ExactCheckpointMatch selectedMatch = matches[0];
            for (int matchIndex = 1;
                matchIndex < matches.Count;
                matchIndex++)
            {
                ExactCheckpointMatch otherMatch = matches[matchIndex];
                if (otherMatch.Checkpoint.EventWatermark !=
                        selectedMatch.Checkpoint.EventWatermark ||
                    !string.Equals(
                        otherMatch.Checkpoint.SnapshotJson,
                        selectedMatch.Checkpoint.SnapshotJson,
                        StringComparison.Ordinal))
                {
                    return CreateResult(
                        LegacyFlatFileImportStatus.AmbiguousMatch,
                        "More than one valid legacy source contains the exact vanilla " +
                        "fingerprint, but their checkpoint snapshots conflict. No " +
                        "legacy data was imported.");
                }
            }

            return ImportSelectedSnapshot(
                selectedMatch,
                loadedSaveData,
                target);
        }

        private static LegacyFlatFileImportResult ValidateTarget(
            CoreSaveScope saveScope,
            SaveManager.SavedData loadedSaveData,
            LightweightCoreStorageEngine target)
        {
            if (saveScope == null ||
                saveScope.IsTransient ||
                !CorePaths.IsSupportedGameSavePath(saveScope.SaveFilePath))
            {
                return CreateResult(
                    LegacyFlatFileImportStatus.InvalidTarget,
                    "Legacy import requires an exact supported physical vanilla " +
                    "save scope; global_data.json is never eligible.");
            }

            if (loadedSaveData == null ||
                loadedSaveData.staticVars__PlayerData == null)
            {
                return CreateResult(
                    LegacyFlatFileImportStatus.InvalidTarget,
                    "Legacy import requires vanilla's already-deserialized SavedData.");
            }

            if (target == null || !target.HasPhysicalScope)
            {
                return CreateResult(
                    LegacyFlatFileImportStatus.InvalidTarget,
                    "Legacy import requires an initialized physical lightweight target.");
            }

            if (target.LastIssuedSequence != 0L)
            {
                return CreateResult(
                    LegacyFlatFileImportStatus.InvalidTarget,
                    "Legacy import is allowed only into a pristine lightweight target.");
            }

            string expectedRelativePath = VanillaSaveStamp.NormalizeRelativePath(
                saveScope.RelativeSavePath);
            if (!string.Equals(
                expectedRelativePath,
                VanillaSaveStamp.NormalizeRelativePath(
                    target.CurrentRelativeSavePath),
                StringComparison.OrdinalIgnoreCase))
            {
                return CreateResult(
                    LegacyFlatFileImportStatus.InvalidTarget,
                    "The lightweight target does not belong to the loaded vanilla save scope.");
            }

            return null;
        }

        private static LegacyFlatFileImportResult ImportSelectedSnapshot(
            ExactCheckpointMatch selectedMatch,
            SaveManager.SavedData loadedSaveData,
            LightweightCoreStorageEngine target)
        {
            LegacyFlatFileState snapshot = selectedMatch.Snapshot;
            string compatibilityError;
            if (!TryValidateImportCompatibility(
                snapshot,
                out compatibilityError))
            {
                return CreateResult(
                    LegacyFlatFileImportStatus.InvalidLegacyData,
                    compatibilityError,
                    selectedMatch.SourcePath);
            }

            DateTime loadedGameDate = DateTime.MinValue;
            if (snapshot.CustomData.Count > 0)
            {
                try
                {
                    loadedGameDate = ExtensionMethods.ToDateTime(
                        loadedSaveData.staticVars__dateTime);
                }
                catch (Exception exception)
                {
                    return CreateResult(
                        LegacyFlatFileImportStatus.ImportFailed,
                        "The loaded game date required for custom-data migration " +
                        "baselines could not be parsed: " + exception.Message,
                        selectedMatch.SourcePath);
                }
            }

            List<LegacyEventRecord> orderedEvents =
                new List<LegacyEventRecord>(snapshot.Events);
            orderedEvents.Sort(CompareLegacyEventsByIdentifier);

            long maximumLegacyEventIdentifier = 0L;
            for (int eventIndex = 0;
                eventIndex < orderedEvents.Count;
                eventIndex++)
            {
                if (orderedEvents[eventIndex].EventId >
                    maximumLegacyEventIdentifier)
                {
                    maximumLegacyEventIdentifier =
                        orderedEvents[eventIndex].EventId;
                }
            }

            int mutationCount = orderedEvents.Count + snapshot.CustomData.Count;
            long firstSequenceBase = Math.Max(
                target.LastIssuedSequence,
                Math.Max(
                    maximumLegacyEventIdentifier,
                    snapshot.NextEventId - 1L));
            if (mutationCount > 0 &&
                firstSequenceBase > long.MaxValue - mutationCount)
            {
                return CreateResult(
                    LegacyFlatFileImportStatus.InvalidLegacyData,
                    "The legacy snapshot cannot be assigned safe lightweight sequences.",
                    selectedMatch.SourcePath);
            }

            // Starting beyond both every preserved EventId and the old next-id
            // watermark prevents a later native v2 event (whose public EventId is
            // its sequence) from colliding with a sparse legacy identifier or
            // reusing an identifier removed by an old rollback. Sequence remains
            // new metadata; each retained public legacy EventId stays verbatim.
            long nextSequence = firstSequenceBase;
            target.SetLastIssuedSequence(firstSequenceBase);
            string appendError;
            for (int eventIndex = 0;
                eventIndex < orderedEvents.Count;
                eventIndex++)
            {
                LegacyEventRecord legacyEvent = orderedEvents[eventIndex];
                LightweightEventRecord importedEvent =
                    new LightweightEventRecord
                    {
                        Sequence = ++nextSequence,
                        EventId = legacyEvent.EventId,
                        GameDateKey = legacyEvent.GameDateKey,
                        GameDateTime = legacyEvent.GameDateTime,
                        IdolId = legacyEvent.IdolId,
                        EntityKind = legacyEvent.EntityKind,
                        EntityId = legacyEvent.EntityId,
                        EventType = legacyEvent.EventType,
                        SourcePatch = legacyEvent.SourcePatch,
                        NamespaceIdentifier =
                            legacyEvent.NamespaceIdentifier,
                        // Preserve the full legacy payload. It may contain the
                        // historical/transient information that justified IMDC.
                        PayloadJson = legacyEvent.PayloadJson
                    };
                if (!target.TryAppendImportedEvent(
                    importedEvent,
                    out appendError))
                {
                    return CreateResult(
                        LegacyFlatFileImportStatus.ImportFailed,
                        "Legacy event import failed; discard this pristine target: " +
                        appendError,
                        selectedMatch.SourcePath);
                }
            }

            for (int customIndex = 0;
                customIndex < snapshot.CustomData.Count;
                customIndex++)
            {
                LegacyCustomDataRecord customData =
                    snapshot.CustomData[customIndex];
                if (!target.TryAppendImportedCustomBaseline(
                    ++nextSequence,
                    loadedGameDate,
                    customData.NamespaceIdentifier,
                    customData.DataKey,
                    customData.ValueJson,
                    out appendError))
                {
                    return CreateResult(
                        LegacyFlatFileImportStatus.ImportFailed,
                        "Legacy custom-data import failed; discard this pristine target: " +
                        appendError,
                        selectedMatch.SourcePath);
                }
            }

            LegacyFlatFileImportResult result = CreateResult(
                LegacyFlatFileImportStatus.Imported,
                "Imported the exact matching late-1.3 fallback checkpoint in memory.",
                selectedMatch.SourcePath);
            result.ImportedEventCount = orderedEvents.Count;
            result.ImportedCustomDataCount = snapshot.CustomData.Count;
            return result;
        }

        private static bool TryValidateImportCompatibility(
            LegacyFlatFileState snapshot,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            Dictionary<string, int> namespaceKeyCounts =
                new Dictionary<string, int>(StringComparer.Ordinal);
            Dictionary<string, long> namespaceCharacterCounts =
                new Dictionary<string, long>(StringComparer.Ordinal);

            for (int customIndex = 0;
                customIndex < snapshot.CustomData.Count;
                customIndex++)
            {
                LegacyCustomDataRecord record = snapshot.CustomData[customIndex];
                if (record.ValueJson.Length >
                    CoreConstants.MaximumCustomValueCharacterCount)
                {
                    errorMessage =
                        "A legacy custom-data value exceeds the v2 value quota.";
                    return false;
                }

                int keyCount;
                namespaceKeyCounts.TryGetValue(
                    record.NamespaceIdentifier,
                    out keyCount);
                keyCount++;
                if (keyCount > CoreConstants.MaximumCustomKeysPerNamespace)
                {
                    errorMessage =
                        "A legacy custom-data namespace exceeds the v2 key quota.";
                    return false;
                }

                namespaceKeyCounts[record.NamespaceIdentifier] = keyCount;

                long characterCount;
                namespaceCharacterCounts.TryGetValue(
                    record.NamespaceIdentifier,
                    out characterCount);
                characterCount += record.ValueJson.Length;
                if (characterCount >
                    CoreConstants.MaximumNamespaceCharacterBudget)
                {
                    errorMessage =
                        "A legacy custom-data namespace exceeds the v2 data quota.";
                    return false;
                }

                namespaceCharacterCounts[record.NamespaceIdentifier] =
                    characterCount;
            }

            return true;
        }

        private static int CompareLegacyEventsByIdentifier(
            LegacyEventRecord left,
            LegacyEventRecord right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            return left.EventId.CompareTo(right.EventId);
        }

        private static string BuildLoadedSaveFingerprint(
            SaveManager.SavedData loadedSaveData)
        {
            // This exactly reproduces the deterministic bytes used by 1.3's
            // DataSaver interception and load fingerprint reconstruction. It
            // intentionally serializes the in-memory SavedData and never reads
            // the canonical vanilla save file.
            string serializedSave = JsonUtility.ToJson(loadedSaveData, true);
            byte[] serializedBytes = new UTF8Encoding(false).GetBytes(
                serializedSave ?? string.Empty);
            string hash = ComputeSha256Hex(serializedBytes);
            return string.Join(
                ":",
                new string[]
                {
                    FingerprintVersionPrefix,
                    serializedBytes.LongLength.ToString(
                        CultureInfo.InvariantCulture),
                    hash
                });
        }

        private static bool IsLegacyFallbackFile(string sourcePath)
        {
            try
            {
                string fileName = Path.GetFileName(sourcePath);
                return string.Equals(
                        fileName,
                        LegacyFallbackFileName,
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        fileName,
                        LegacyFallbackFileName + LegacyBackupFileSuffix,
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        fileName,
                        LegacyFallbackFileName + LegacyTemporaryFileSuffix,
                        StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Reproduces late 1.3's version-2 DeserializeState validation. The hash
        /// is calculated from JsonUtility's compact canonical projection with the
        /// IntegritySha256 field temporarily empty, then every embedded checkpoint
        /// snapshot is independently validated by the same routine.
        /// </summary>
        private static LegacyFlatFileState DeserializeVersionTwo(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                throw new InvalidDataException(
                    "The legacy flat-file state is empty.");
            }

            string trimmedJson = rawJson.Trim();
            if (trimmedJson.Length < 2 ||
                trimmedJson[0] != '{' ||
                trimmedJson[trimmedJson.Length - 1] != '}')
            {
                throw new InvalidDataException(
                    "The legacy flat-file state is not a JSON object.");
            }

            HashSet<string> topLevelFields =
                ReadTopLevelJsonFieldNames(trimmedJson);
            LegacyFormatProbe formatProbe =
                JsonUtility.FromJson<LegacyFormatProbe>(rawJson);
            if (!topLevelFields.Contains("FormatVersion") ||
                formatProbe == null ||
                formatProbe.FormatVersion != SupportedLegacyFormatVersion)
            {
                throw new UnsupportedLegacyFormatException(
                    "Only the late-1.3 fallback FormatVersion 2 is supported.");
            }

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
            RequireLegacyStateField(topLevelFields, "Checkpoints");

            LegacyFlatFileState state =
                JsonUtility.FromJson<LegacyFlatFileState>(rawJson);
            if (state == null)
            {
                throw new InvalidDataException(
                    "The legacy flat-file state could not be deserialized.");
            }

            NormalizeOptionalStateFields(state);
            ValidateStateStructure(state);

            string storedIntegrity = state.IntegritySha256 ?? string.Empty;
            if (string.IsNullOrEmpty(storedIntegrity))
            {
                throw new InvalidDataException(
                    "The legacy flat-file integrity hash is missing.");
            }

            string computedIntegrity = ComputeStateIntegritySha256(state);
            if (!string.Equals(
                storedIntegrity,
                computedIntegrity,
                StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The legacy flat-file integrity hash does not match its contents.");
            }

            MigrateSingularCheckpointMetadata(state);
            ValidateCheckpointHistory(state);
            return state;
        }

        private static void NormalizeOptionalStateFields(
            LegacyFlatFileState state)
        {
            state.IntegritySha256 = state.IntegritySha256 ?? string.Empty;
            state.CheckpointFingerprint =
                state.CheckpointFingerprint ?? string.Empty;
            state.CheckpointSnapshotJson =
                state.CheckpointSnapshotJson ?? string.Empty;
            state.CheckpointCreatedUtc =
                state.CheckpointCreatedUtc ?? string.Empty;
            if (state.Checkpoints == null)
            {
                state.Checkpoints = new List<LegacyCheckpointRecord>();
            }
        }

        /// <summary>
        /// Format 2 normally carries Checkpoints[]. This normalization mirrors the
        /// old validator for a file that was upgraded in place from the immediately
        /// preceding single-checkpoint representation, without accepting version 1
        /// or an unversioned envelope.
        /// </summary>
        private static void MigrateSingularCheckpointMetadata(
            LegacyFlatFileState state)
        {
            bool hasSingularCheckpoint =
                !string.IsNullOrEmpty(state.CheckpointFingerprint) ||
                !string.IsNullOrEmpty(state.CheckpointSnapshotJson) ||
                !string.IsNullOrEmpty(state.CheckpointCreatedUtc) ||
                state.CheckpointEventWatermark != 0L;
            if (hasSingularCheckpoint)
            {
                if (string.IsNullOrEmpty(state.CheckpointFingerprint) ||
                    string.IsNullOrEmpty(state.CheckpointSnapshotJson) ||
                    string.IsNullOrEmpty(state.CheckpointCreatedUtc))
                {
                    throw new InvalidDataException(
                        "The legacy singular checkpoint metadata is incomplete.");
                }

                bool alreadyPresent = false;
                for (int checkpointIndex = 0;
                    checkpointIndex < state.Checkpoints.Count;
                    checkpointIndex++)
                {
                    LegacyCheckpointRecord existing =
                        state.Checkpoints[checkpointIndex];
                    if (existing != null &&
                        string.Equals(
                            existing.Fingerprint,
                            state.CheckpointFingerprint,
                            StringComparison.Ordinal))
                    {
                        alreadyPresent = true;
                        break;
                    }
                }

                if (!alreadyPresent)
                {
                    state.Checkpoints.Add(
                        new LegacyCheckpointRecord
                        {
                            Fingerprint = state.CheckpointFingerprint,
                            EventWatermark =
                                state.CheckpointEventWatermark,
                            SnapshotJson = state.CheckpointSnapshotJson,
                            CreatedUtc = state.CheckpointCreatedUtc
                        });
                }
            }

            state.CheckpointFingerprint = string.Empty;
            state.CheckpointEventWatermark = 0L;
            state.CheckpointSnapshotJson = string.Empty;
            state.CheckpointCreatedUtc = string.Empty;
            while (state.Checkpoints.Count > MaximumRetainedSaveGenerations)
            {
                state.Checkpoints.RemoveAt(0);
            }
        }

        private static void ValidateCheckpointHistory(
            LegacyFlatFileState state)
        {
            if (state == null || state.Checkpoints == null)
            {
                throw new InvalidDataException(
                    "The legacy checkpoint history is missing.");
            }

            if (state.Checkpoints.Count > MaximumRetainedSaveGenerations)
            {
                throw new InvalidDataException(
                    "The legacy checkpoint history exceeds its retention bound.");
            }

            HashSet<string> fingerprints = new HashSet<string>(
                StringComparer.Ordinal);
            for (int checkpointIndex = 0;
                checkpointIndex < state.Checkpoints.Count;
                checkpointIndex++)
            {
                LegacyCheckpointRecord checkpoint =
                    state.Checkpoints[checkpointIndex];
                if (checkpoint == null ||
                    string.IsNullOrEmpty(checkpoint.Fingerprint) ||
                    checkpoint.EventWatermark < 0L ||
                    string.IsNullOrEmpty(checkpoint.SnapshotJson) ||
                    string.IsNullOrEmpty(checkpoint.CreatedUtc) ||
                    !fingerprints.Add(checkpoint.Fingerprint))
                {
                    throw new InvalidDataException(
                        "The legacy checkpoint history contains an invalid or " +
                        "duplicate generation.");
                }

                LegacyFlatFileState checkpointState =
                    DeserializeVersionTwo(checkpoint.SnapshotJson);
                if (HasAnyCheckpointMetadata(checkpointState))
                {
                    throw new InvalidDataException(
                        "A legacy generation snapshot contains nested checkpoint metadata.");
                }

                if (ComputeMaximumEventId(checkpointState) !=
                    checkpoint.EventWatermark)
                {
                    throw new InvalidDataException(
                        "A legacy generation event watermark is inconsistent.");
                }
            }
        }

        private static bool HasAnyCheckpointMetadata(
            LegacyFlatFileState state)
        {
            return state != null &&
                (!string.IsNullOrEmpty(state.CheckpointFingerprint) ||
                 !string.IsNullOrEmpty(state.CheckpointSnapshotJson) ||
                 !string.IsNullOrEmpty(state.CheckpointCreatedUtc) ||
                 state.CheckpointEventWatermark != 0L ||
                 (state.Checkpoints != null && state.Checkpoints.Count > 0));
        }

        private static long ComputeMaximumEventId(LegacyFlatFileState state)
        {
            long maximumEventId = 0L;
            if (state == null || state.Events == null)
            {
                return maximumEventId;
            }

            for (int eventIndex = 0;
                eventIndex < state.Events.Count;
                eventIndex++)
            {
                LegacyEventRecord record = state.Events[eventIndex];
                if (record != null && record.EventId > maximumEventId)
                {
                    maximumEventId = record.EventId;
                }
            }

            return maximumEventId;
        }

        private static void ValidateStateStructure(LegacyFlatFileState state)
        {
            if (state.Events == null ||
                state.CustomData == null ||
                state.SingleParticipation == null ||
                state.StatusWindows == null ||
                state.ShowCastWindows == null ||
                state.ContractWindows == null ||
                state.RelationshipWindows == null ||
                state.TourParticipation == null ||
                state.AwardResults == null ||
                state.ElectionResults == null ||
                state.PushWindows == null ||
                state.Checkpoints == null)
            {
                throw new InvalidDataException(
                    "The legacy flat-file state contains a null core list.");
            }

            if (state.IntegritySha256 == null ||
                state.CheckpointFingerprint == null ||
                state.CheckpointSnapshotJson == null ||
                state.CheckpointCreatedUtc == null ||
                state.CheckpointEventWatermark < 0L ||
                state.Checkpoints.Count > MaximumRetainedSaveGenerations)
            {
                throw new InvalidDataException(
                    "The legacy flat-file state contains invalid integrity or " +
                    "checkpoint metadata.");
            }

            long maximumEventId = 0L;
            HashSet<long> eventIds = new HashSet<long>();
            for (int eventIndex = 0;
                eventIndex < state.Events.Count;
                eventIndex++)
            {
                LegacyEventRecord record = state.Events[eventIndex];
                if (record == null ||
                    record.EventId <= 0L ||
                    !eventIds.Add(record.EventId) ||
                    HasNullString(
                        record.GameDateTime,
                        record.EntityKind,
                        record.EntityId,
                        record.EventType,
                        record.SourcePatch,
                        record.NamespaceIdentifier,
                        record.PayloadJson))
                {
                    throw new InvalidDataException(
                        "The legacy flat-file state contains an invalid event record.");
                }

                if (record.EventId > maximumEventId)
                {
                    maximumEventId = record.EventId;
                }
            }

            if (state.NextEventId <= maximumEventId ||
                state.NextEventId < 1L)
            {
                throw new InvalidDataException(
                    "The legacy next event identifier is not monotonic.");
            }

            bool hasSingularCheckpoint =
                !string.IsNullOrEmpty(state.CheckpointFingerprint) ||
                !string.IsNullOrEmpty(state.CheckpointSnapshotJson) ||
                !string.IsNullOrEmpty(state.CheckpointCreatedUtc) ||
                state.CheckpointEventWatermark != 0L;
            if (hasSingularCheckpoint &&
                (string.IsNullOrEmpty(state.CheckpointFingerprint) ||
                 string.IsNullOrEmpty(state.CheckpointSnapshotJson) ||
                 string.IsNullOrEmpty(state.CheckpointCreatedUtc)))
            {
                throw new InvalidDataException(
                    "The legacy singular checkpoint metadata is inconsistent.");
            }

            HashSet<string> customKeys = new HashSet<string>(
                StringComparer.Ordinal);
            for (int customIndex = 0;
                customIndex < state.CustomData.Count;
                customIndex++)
            {
                LegacyCustomDataRecord record = state.CustomData[customIndex];
                if (record == null ||
                    HasNullString(
                        record.NamespaceIdentifier,
                        record.DataKey,
                        record.ValueJson,
                        record.UpdatedUtc) ||
                    !customKeys.Add(
                        BuildCompositeStringKey(
                            record.NamespaceIdentifier,
                            record.DataKey)))
                {
                    throw new InvalidDataException(
                        "The legacy state contains invalid or duplicate custom data.");
                }
            }

            HashSet<string> singleKeys = new HashSet<string>(
                StringComparer.Ordinal);
            for (int recordIndex = 0;
                recordIndex < state.SingleParticipation.Count;
                recordIndex++)
            {
                LegacySingleParticipationRecord record =
                    state.SingleParticipation[recordIndex];
                string key = record == null
                    ? string.Empty
                    : record.SingleId.ToString(CultureInfo.InvariantCulture) +
                      ":" +
                      record.IdolId.ToString(CultureInfo.InvariantCulture);
                if (record == null ||
                    record.ReleaseDate == null ||
                    !singleKeys.Add(key))
                {
                    throw new InvalidDataException(
                        "The legacy state contains invalid or duplicate single participation.");
                }
            }

            for (int recordIndex = 0;
                recordIndex < state.StatusWindows.Count;
                recordIndex++)
            {
                LegacyStatusWindowRecord record =
                    state.StatusWindows[recordIndex];
                if (record == null ||
                    HasNullString(
                        record.StatusType,
                        record.StartDate,
                        record.EndDate))
                {
                    throw new InvalidDataException(
                        "The legacy state contains an invalid status window.");
                }
            }

            for (int recordIndex = 0;
                recordIndex < state.ShowCastWindows.Count;
                recordIndex++)
            {
                LegacyShowCastWindowRecord record =
                    state.ShowCastWindows[recordIndex];
                if (record == null ||
                    HasNullString(
                        record.ShowId,
                        record.StartDate,
                        record.EndDate,
                        record.EndReason,
                        record.PayloadJson))
                {
                    throw new InvalidDataException(
                        "The legacy state contains an invalid show-cast window.");
                }
            }

            for (int recordIndex = 0;
                recordIndex < state.ContractWindows.Count;
                recordIndex++)
            {
                LegacyContractWindowRecord record =
                    state.ContractWindows[recordIndex];
                if (record == null ||
                    HasNullString(
                        record.ContractKey,
                        record.StartDate,
                        record.EndDate,
                        record.EndReason,
                        record.PayloadJson))
                {
                    throw new InvalidDataException(
                        "The legacy state contains an invalid contract window.");
                }
            }

            for (int recordIndex = 0;
                recordIndex < state.RelationshipWindows.Count;
                recordIndex++)
            {
                LegacyRelationshipWindowRecord record =
                    state.RelationshipWindows[recordIndex];
                if (record == null ||
                    HasNullString(
                        record.RelationshipKey,
                        record.RelationshipType,
                        record.StartDate,
                        record.EndDate,
                        record.EndReason,
                        record.PayloadJson))
                {
                    throw new InvalidDataException(
                        "The legacy state contains an invalid relationship window.");
                }
            }

            for (int recordIndex = 0;
                recordIndex < state.TourParticipation.Count;
                recordIndex++)
            {
                LegacyTourParticipationRecord record =
                    state.TourParticipation[recordIndex];
                if (record == null ||
                    HasNullString(
                        record.TourId,
                        record.LifecycleAction,
                        record.EventDate,
                        record.PayloadJson))
                {
                    throw new InvalidDataException(
                        "The legacy state contains invalid tour participation.");
                }
            }

            for (int recordIndex = 0;
                recordIndex < state.AwardResults.Count;
                recordIndex++)
            {
                LegacyAwardResultRecord record =
                    state.AwardResults[recordIndex];
                if (record == null ||
                    HasNullString(
                        record.AwardKey,
                        record.EventDate,
                        record.PayloadJson))
                {
                    throw new InvalidDataException(
                        "The legacy state contains an invalid award result.");
                }
            }

            for (int recordIndex = 0;
                recordIndex < state.ElectionResults.Count;
                recordIndex++)
            {
                LegacyElectionResultRecord record =
                    state.ElectionResults[recordIndex];
                if (record == null ||
                    HasNullString(
                        record.ElectionId,
                        record.EventDate,
                        record.PayloadJson))
                {
                    throw new InvalidDataException(
                        "The legacy state contains an invalid election result.");
                }
            }

            for (int recordIndex = 0;
                recordIndex < state.PushWindows.Count;
                recordIndex++)
            {
                LegacyPushWindowRecord record = state.PushWindows[recordIndex];
                if (record == null ||
                    HasNullString(
                        record.SlotKey,
                        record.StartDate,
                        record.EndDate,
                        record.EndReason,
                        record.PayloadJson))
                {
                    throw new InvalidDataException(
                        "The legacy state contains an invalid push window.");
                }
            }
        }

        private static bool HasNullString(params string[] values)
        {
            if (values == null)
            {
                return true;
            }

            for (int valueIndex = 0;
                valueIndex < values.Length;
                valueIndex++)
            {
                if (values[valueIndex] == null)
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildCompositeStringKey(
            string first,
            string second)
        {
            string normalizedFirst = first ?? string.Empty;
            string normalizedSecond = second ?? string.Empty;
            return normalizedFirst.Length.ToString(CultureInfo.InvariantCulture) +
                ":" + normalizedFirst + normalizedSecond;
        }

        private static string ComputeStateIntegritySha256(
            LegacyFlatFileState state)
        {
            string originalIntegrity = state.IntegritySha256;
            try
            {
                state.IntegritySha256 = string.Empty;
                string canonicalJson = JsonUtility.ToJson(state, false);
                return ComputeSha256Hex(
                    new UTF8Encoding(false).GetBytes(
                        canonicalJson ?? string.Empty));
            }
            finally
            {
                state.IntegritySha256 = originalIntegrity;
            }
        }

        private static string ComputeSha256Hex(byte[] valueBytes)
        {
            byte[] hashBytes;
            using (SHA256 sha256 = SHA256.Create())
            {
                hashBytes = sha256.ComputeHash(
                    valueBytes ?? new byte[0]);
            }

            StringBuilder builder = new StringBuilder(hashBytes.Length * 2);
            for (int byteIndex = 0;
                byteIndex < hashBytes.Length;
                byteIndex++)
            {
                builder.Append(
                    hashBytes[byteIndex].ToString(
                        "x2",
                        CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private static void RequireLegacyStateField(
            HashSet<string> topLevelFields,
            string fieldName)
        {
            if (topLevelFields == null ||
                !topLevelFields.Contains(fieldName))
            {
                throw new InvalidDataException(
                    "The legacy flat-file state is missing the " +
                    fieldName + " field.");
            }
        }

        // The legacy validator enumerated root properties before invoking
        // JsonUtility because JsonUtility silently fills missing fields and ignores
        // duplicates. Keeping the same parser preserves those integrity semantics.
        private static HashSet<string> ReadTopLevelJsonFieldNames(
            string jsonText)
        {
            HashSet<string> fieldNames = new HashSet<string>(
                StringComparer.Ordinal);
            int index = 0;
            SkipJsonWhitespace(jsonText, ref index);
            if (index >= jsonText.Length || jsonText[index] != '{')
            {
                throw new InvalidDataException(
                    "The legacy flat-file state has no root JSON object.");
            }

            index++;
            SkipJsonWhitespace(jsonText, ref index);
            if (index < jsonText.Length && jsonText[index] == '}')
            {
                index++;
            }
            else
            {
                while (index < jsonText.Length)
                {
                    string fieldName = ReadPlainJsonPropertyName(
                        jsonText,
                        ref index);
                    if (!fieldNames.Add(fieldName))
                    {
                        throw new InvalidDataException(
                            "The legacy flat-file state contains a duplicate root field: " +
                            fieldName);
                    }

                    SkipJsonWhitespace(jsonText, ref index);
                    if (index >= jsonText.Length || jsonText[index] != ':')
                    {
                        throw new InvalidDataException(
                            "A legacy root field has no value separator.");
                    }

                    index++;
                    SkipOneJsonValue(jsonText, ref index);
                    SkipJsonWhitespace(jsonText, ref index);
                    if (index >= jsonText.Length)
                    {
                        throw new InvalidDataException(
                            "The legacy root object is unterminated.");
                    }

                    if (jsonText[index] == '}')
                    {
                        index++;
                        break;
                    }

                    if (jsonText[index] != ',')
                    {
                        throw new InvalidDataException(
                            "The legacy root object has an invalid field separator.");
                    }

                    index++;
                    SkipJsonWhitespace(jsonText, ref index);
                }
            }

            SkipJsonWhitespace(jsonText, ref index);
            if (index != jsonText.Length)
            {
                throw new InvalidDataException(
                    "The legacy flat-file state has data after its root object.");
            }

            return fieldNames;
        }

        private static string ReadPlainJsonPropertyName(
            string jsonText,
            ref int index)
        {
            SkipJsonWhitespace(jsonText, ref index);
            if (index >= jsonText.Length || jsonText[index] != '"')
            {
                throw new InvalidDataException(
                    "The legacy root object contains an invalid field name.");
            }

            index++;
            int startIndex = index;
            while (index < jsonText.Length)
            {
                char character = jsonText[index];
                if (character == '\\')
                {
                    throw new InvalidDataException(
                        "Escaped legacy root field names are not supported.");
                }

                if (character == '"')
                {
                    string fieldName = jsonText.Substring(
                        startIndex,
                        index - startIndex);
                    index++;
                    return fieldName;
                }

                if (character < ' ')
                {
                    throw new InvalidDataException(
                        "A legacy root field name contains a control character.");
                }

                index++;
            }

            throw new InvalidDataException(
                "The legacy root object contains an unterminated field name.");
        }

        private static void SkipOneJsonValue(string jsonText, ref int index)
        {
            SkipJsonWhitespace(jsonText, ref index);
            if (index >= jsonText.Length)
            {
                throw new InvalidDataException(
                    "The legacy root object contains a missing value.");
            }

            char firstCharacter = jsonText[index];
            if (firstCharacter == '"')
            {
                SkipJsonString(jsonText, ref index);
                return;
            }

            if (firstCharacter == '{' ||
                firstCharacter == JsonArrayStartCharacter)
            {
                Stack<char> expectedClosures = new Stack<char>();
                expectedClosures.Push(
                    firstCharacter == '{' ? '}' : JsonArrayEndCharacter);
                index++;
                while (index < jsonText.Length &&
                    expectedClosures.Count > 0)
                {
                    char character = jsonText[index];
                    if (character == '"')
                    {
                        SkipJsonString(jsonText, ref index);
                        continue;
                    }

                    if (character == '{')
                    {
                        expectedClosures.Push('}');
                    }
                    else if (character == JsonArrayStartCharacter)
                    {
                        expectedClosures.Push(JsonArrayEndCharacter);
                    }
                    else if (character == '}' ||
                        character == JsonArrayEndCharacter)
                    {
                        if (expectedClosures.Count == 0 ||
                            expectedClosures.Pop() != character)
                        {
                            throw new InvalidDataException(
                                "The legacy state contains mismatched JSON containers.");
                        }
                    }

                    index++;
                }

                if (expectedClosures.Count != 0)
                {
                    throw new InvalidDataException(
                        "The legacy state contains an unterminated JSON container.");
                }

                return;
            }

            int primitiveStartIndex = index;
            while (index < jsonText.Length &&
                jsonText[index] != ',' &&
                jsonText[index] != '}')
            {
                index++;
            }

            if (string.IsNullOrWhiteSpace(
                jsonText.Substring(
                    primitiveStartIndex,
                    index - primitiveStartIndex)))
            {
                throw new InvalidDataException(
                    "The legacy root object contains an empty primitive value.");
            }
        }

        private static void SkipJsonString(string jsonText, ref int index)
        {
            if (index >= jsonText.Length || jsonText[index] != '"')
            {
                throw new InvalidDataException(
                    "The legacy state contains an invalid JSON string.");
            }

            index++;
            while (index < jsonText.Length)
            {
                char character = jsonText[index++];
                if (character == '\\')
                {
                    if (index >= jsonText.Length)
                    {
                        throw new InvalidDataException(
                            "The legacy state contains an unterminated JSON escape.");
                    }

                    index++;
                    continue;
                }

                if (character == '"')
                {
                    return;
                }
            }

            throw new InvalidDataException(
                "The legacy state contains an unterminated JSON string.");
        }

        private static void SkipJsonWhitespace(string jsonText, ref int index)
        {
            while (index < jsonText.Length &&
                char.IsWhiteSpace(jsonText[index]))
            {
                index++;
            }
        }

        private static LegacyFlatFileImportResult CreateResult(
            LegacyFlatFileImportStatus status,
            string message)
        {
            return CreateResult(status, message, string.Empty);
        }

        private static LegacyFlatFileImportResult CreateResult(
            LegacyFlatFileImportStatus status,
            string message,
            string sourcePath)
        {
            return new LegacyFlatFileImportResult
            {
                Status = status,
                Message = message ?? string.Empty,
                SourcePath = sourcePath ?? string.Empty
            };
        }
    }
}
