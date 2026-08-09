using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using ModLocalizationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GraduationDetails
{
    internal enum CustodyOwner
    {
        Unknown = 0,
        Idol = 1,
        Player = 2,
        None = 3
    }

    [Serializable]
    internal sealed class MarriageRecord
    {
        public int GirlId = -1;
        public bool MarriedToPlayer;
        public string PlayerName = "";
        public int KidsCount = -1;
        public CustodyOwner Custody = CustodyOwner.Unknown;
    }

    [Serializable]
    internal sealed class MarriageRecordList
    {
        public List<MarriageRecord> Records = new List<MarriageRecord>();
    }

    internal static class GraduationDetailsPaths
    {
        internal const string BaseFolder = "GraduationDetails";
        internal const string DisplayFolder = "Graduation Details";
        internal const string SavesFolder = "saves";
        internal const string PortraitsFolder = "Portraits";
        internal const string MarriageFile = "marriage_data.json";
        internal const string StaffMapFile = "staff_idol_map.json";
        internal const string SnapshotsFile = "graduation_snapshots.json";
        internal const string TransactionsFolder = ".transactions";
        internal const string CommittedSnapshotsFolder = ".snapshots";
        internal const string ActiveSnapshotFile = ".active_snapshot";
        internal const string ActiveSnapshotBackupFile = ".active_snapshot.bak";
        internal const string SnapshotManifestFile = "snapshot_manifest.json";

        private const string GameDataFolder = "data";
        private const string JsonExtension = ".json";
        private const string GenericSaveFile = "save.json";
        private const string GlobalDataFile = "global_data.json";
        private const string UnboundScope = "<unbound>";

        private static string activeVanillaSaveFilePath = "";
        private static string activeSaveDirectory = "";
        private static string activeDataDirectory = "";
        private static bool activeScopeWritable;
        private static string workingPortraitSession = Guid.NewGuid().ToString("N");

        internal static string RootDir
        {
            get
            {
                return Path.Combine(Application.persistentDataPath, BaseFolder);
            }
        }

        internal static string SavesRootDir
        {
            get
            {
                return Path.Combine(RootDir, SavesFolder);
            }
        }

        internal static bool HasActiveSaveScope
        {
            get
            {
                return !string.IsNullOrEmpty(activeSaveDirectory);
            }
        }

        internal static bool CanWriteActiveScope
        {
            get
            {
                return HasActiveSaveScope && activeScopeWritable;
            }
        }

        internal static string ActiveDataDirectory
        {
            get
            {
                return activeDataDirectory;
            }
        }

        internal static string ActiveVanillaSaveFilePath
        {
            get
            {
                return activeVanillaSaveFilePath;
            }
        }

        internal static string GetScopeId()
        {
            return HasActiveSaveScope
                ? activeVanillaSaveFilePath + "|" + activeDataDirectory
                : UnboundScope;
        }

        internal static string GetSaveDir()
        {
            return activeSaveDirectory;
        }

        internal static string GetScopedFilePath(string fileName)
        {
            string path;
            if (!HasActiveSaveScope
                || string.IsNullOrEmpty(activeDataDirectory)
                || !TryGetSafeLeafPath(activeDataDirectory, fileName, out path))
            {
                return "";
            }
            return path;
        }

        internal static string GetScopedPortraitDir()
        {
            string path;
            if (!HasActiveSaveScope
                || string.IsNullOrEmpty(activeDataDirectory)
                || !TryGetContainedPath(activeDataDirectory, PortraitsFolder, out path))
            {
                return "";
            }
            return path;
        }

        internal static string GetWorkingPortraitDir()
        {
            return Path.Combine(
                Path.GetTempPath(),
                BaseFolder,
                workingPortraitSession,
                PortraitsFolder);
        }

        internal static void BeginFreshWorkingPortraitScope()
        {
            // A fresh temporary overlay prevents portraits captured before a save from leaking
            // into a subsequently loaded game while avoiding destructive cleanup of old caches.
            workingPortraitSession = Guid.NewGuid().ToString("N");
        }

        internal static void Bind(string vanillaSaveFilePath, string saveDirectory)
        {
            if (IsGlobalDataFilePath(vanillaSaveFilePath))
            {
                ClearBinding();
                return;
            }
            activeVanillaSaveFilePath = vanillaSaveFilePath ?? "";
            activeSaveDirectory = saveDirectory ?? "";
            activeDataDirectory = "";
            activeScopeWritable = false;

            string readableDirectory;
            GraduationDetailsSnapshotResolution resolution =
                GraduationDetailsSnapshotStorage.ResolveReadableSnapshot(
                    activeSaveDirectory,
                    activeVanillaSaveFilePath,
                    out readableDirectory);
            if (resolution == GraduationDetailsSnapshotResolution.Valid)
            {
                activeDataDirectory = readableDirectory;
                activeScopeWritable = true;
            }
            else if (resolution == GraduationDetailsSnapshotResolution.Missing)
            {
                // A save without sidecar data is a valid empty scope. Its first verified vanilla
                // save will create an authoritative committed snapshot.
                activeDataDirectory = activeSaveDirectory;
                activeScopeWritable = true;
            }
        }

        internal static void ClearBinding()
        {
            activeVanillaSaveFilePath = "";
            activeSaveDirectory = "";
            activeDataDirectory = "";
            activeScopeWritable = false;
        }

        internal static bool IsSafeLeafFileName(string fileName)
        {
            return GraduationDetailsPersistenceIO.IsSafeLeafFileName(fileName);
        }

        internal static bool IsSafePortraitFileName(string fileName)
        {
            if (!IsSafeLeafFileName(fileName)
                || fileName.Length > 96
                || !fileName.EndsWith(".png", StringComparison.Ordinal))
            {
                return false;
            }

            string stem = fileName.Substring(0, fileName.Length - 4);
            if (stem.Length == 0)
            {
                return false;
            }
            for (int i = 0; i < stem.Length; i++)
            {
                char c = stem[i];
                if (!((c >= 'A' && c <= 'Z')
                    || (c >= 'a' && c <= 'z')
                    || (c >= '0' && c <= '9')
                    || c == '_'
                    || c == '-'))
                {
                    return false;
                }
            }
            string upperStem = stem.ToUpperInvariant();
            if (upperStem == "CON"
                || upperStem == "PRN"
                || upperStem == "AUX"
                || upperStem == "NUL")
            {
                return false;
            }
            return !(upperStem.Length == 4
                && (upperStem.StartsWith("COM", StringComparison.Ordinal)
                    || upperStem.StartsWith("LPT", StringComparison.Ordinal))
                && upperStem[3] >= '1'
                && upperStem[3] <= '9');
        }

        internal static bool TryGetSafePortraitPath(
            string rootDirectory,
            string fileName,
            bool forWrite,
            out string path)
        {
            path = "";
            return IsSafePortraitFileName(fileName)
                && GraduationDetailsPersistenceIO.TryGetContainedPath(
                    rootDirectory,
                    fileName,
                    forWrite,
                    out path);
        }

        internal static bool TryGetSafeLeafPath(string rootDirectory, string fileName, out string path)
        {
            path = "";
            return GraduationDetailsPersistenceIO.TryGetSafeLeafPath(
                rootDirectory,
                fileName,
                true,
                out path);
        }

        internal static bool TryGetContainedPath(string rootDirectory, string relativePath, out string path)
        {
            return GraduationDetailsPersistenceIO.TryGetContainedPath(
                rootDirectory,
                relativePath,
                true,
                out path);
        }

        internal static bool IsPathContainedBy(string rootDirectory, string candidatePath)
        {
            return GraduationDetailsPersistenceIO.IsPathContainedBy(rootDirectory, candidatePath);
        }

        internal static bool TryValidateOwnedDataDirectory(
            string directory,
            out string validatedDirectory)
        {
            validatedDirectory = "";
            return IsPathContainedBy(RootDir, directory)
                && GraduationDetailsPersistenceIO.TryValidatePathUnderRoot(
                    Application.persistentDataPath,
                    directory,
                    false,
                    out validatedDirectory);
        }

        internal static string GetVanillaRelativePath(string normalizedVanillaSaveFilePath)
        {
            try
            {
                string dataRoot = Path.GetFullPath(Path.Combine(
                    Application.persistentDataPath,
                    GameDataFolder)).TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);
                string dataRootPrefix = dataRoot + Path.DirectorySeparatorChar;
                string normalized = Path.GetFullPath(normalizedVanillaSaveFilePath);
                return normalized.StartsWith(dataRootPrefix, StringComparison.OrdinalIgnoreCase)
                    ? normalized.Substring(dataRootPrefix.Length)
                    : "";
            }
            catch
            {
                return "";
            }
        }

        internal static string ResolveDataSaverOutputPath(string dataFileName, bool fullPath)
        {
            if (string.IsNullOrWhiteSpace(dataFileName))
            {
                return "";
            }

            try
            {
                // Match DataSaver.saveData exactly. Non-full-path saves always append .json,
                // while a rooted filename replaces the game's data directory.
                string candidatePath = dataFileName;
                if (!fullPath)
                {
                    candidatePath = Path.Combine(
                        Application.persistentDataPath,
                        GameDataFolder);
                    candidatePath = Path.Combine(
                        candidatePath,
                        dataFileName + JsonExtension);
                }
                return Path.GetFullPath(candidatePath);
            }
            catch
            {
                return "";
            }
        }

        internal static string ResolveDataSaverLoadPath(string dataFileName)
        {
            if (string.IsNullOrWhiteSpace(dataFileName))
            {
                return "";
            }

            try
            {
                // Match DataSaver.loadData, including its duplicate-extension cleanup.
                string candidatePath = Path.Combine(
                    Application.persistentDataPath,
                    GameDataFolder);
                candidatePath = Path.Combine(
                    candidatePath,
                    dataFileName + JsonExtension);
                candidatePath = candidatePath.Replace(JsonExtension + JsonExtension, JsonExtension);
                return Path.GetFullPath(candidatePath);
            }
            catch
            {
                return "";
            }
        }

        internal static string GetQuickSaveDataFileName(bool autoSave)
        {
            try
            {
                // Match SaveManager.GetSaveFileName(bool). The false branch is used by
                // LoadData(false); autosave selection remains delegated to vanilla's
                // GetLatestAutosavePath so its existing directory scan only runs once.
                string relativePath = "";
                if (staticVars.IsStoryMode())
                {
                    relativePath = Path.Combine(
                        "story_mode",
                        staticVars.PlayerData.GetSaveFolderName());
                }
                return Path.Combine(
                    relativePath,
                    autoSave ? "auto_save" : "manual_save");
            }
            catch
            {
                return "";
            }
        }

        internal static bool TryResolveSaveDirectory(
            string vanillaSaveFilePath,
            out string normalizedVanillaSaveFilePath,
            out string saveDirectory)
        {
            normalizedVanillaSaveFilePath = "";
            saveDirectory = "";

            try
            {
                if (string.IsNullOrWhiteSpace(vanillaSaveFilePath))
                {
                    return false;
                }

                string dataRoot = Path.GetFullPath(Path.Combine(
                    Application.persistentDataPath,
                    GameDataFolder)).TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);
                string dataRootPrefix = dataRoot + Path.DirectorySeparatorChar;
                normalizedVanillaSaveFilePath = Path.GetFullPath(vanillaSaveFilePath);
                if (!normalizedVanillaSaveFilePath.StartsWith(
                    dataRootPrefix,
                    StringComparison.OrdinalIgnoreCase))
                {
                    normalizedVanillaSaveFilePath = "";
                    return false;
                }

                string relativeFilePath = normalizedVanillaSaveFilePath.Substring(dataRootPrefix.Length);
                if (string.IsNullOrEmpty(relativeFilePath))
                {
                    normalizedVanillaSaveFilePath = "";
                    return false;
                }
                if (IsGlobalDataFilePath(relativeFilePath))
                {
                    // global_data.json contains process-wide settings, not a game save. It must
                    // never acquire a Graduation Details scope even if a caller supplies it from
                    // inside vanilla's data directory.
                    normalizedVanillaSaveFilePath = "";
                    return false;
                }
                if (!IsVanillaGameSaveRelativePath(relativeFilePath))
                {
                    // Only bind files emitted by vanilla's game-save writers. Other JSON in the
                    // data directory (settings, backups, or files owned by another mod) is not a
                    // Graduation Details save scope.
                    normalizedVanillaSaveFilePath = "";
                    return false;
                }

                string relativeDirectory = Path.GetDirectoryName(relativeFilePath) ?? "";
                string fileStem = Path.GetFileNameWithoutExtension(relativeFilePath);
                string relativeSaveDirectory = string.Equals(
                    Path.GetFileName(relativeFilePath),
                    GenericSaveFile,
                    StringComparison.OrdinalIgnoreCase)
                        ? relativeDirectory
                        : Path.Combine(relativeDirectory, fileStem);
                if (string.IsNullOrEmpty(relativeSaveDirectory))
                {
                    normalizedVanillaSaveFilePath = "";
                    return false;
                }

                string savesRoot = Path.GetFullPath(SavesRootDir).TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
                string savesRootPrefix = savesRoot + Path.DirectorySeparatorChar;
                saveDirectory = Path.GetFullPath(Path.Combine(savesRoot, relativeSaveDirectory));
                if (!saveDirectory.StartsWith(savesRootPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    normalizedVanillaSaveFilePath = "";
                    saveDirectory = "";
                    return false;
                }

                return true;
            }
            catch
            {
                normalizedVanillaSaveFilePath = "";
                saveDirectory = "";
                return false;
            }
        }

        private static bool IsGlobalDataFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            try
            {
                return string.Equals(
                    Path.GetFileName(filePath),
                    GlobalDataFile,
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsVanillaGameSaveRelativePath(string relativeFilePath)
        {
            if (string.IsNullOrWhiteSpace(relativeFilePath))
            {
                return false;
            }

            string normalizedPath = relativeFilePath.Replace(
                Path.AltDirectorySeparatorChar,
                Path.DirectorySeparatorChar);
            string[] parts = normalizedPath.Split(
                new char[] { Path.DirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
            {
                return IsQuickSaveFile(parts[0]);
            }

            if (parts.Length == 3
                && string.Equals(parts[0], "manual_saves", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(parts[1])
                && string.Equals(parts[2], GenericSaveFile, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (parts.Length < 3
                || !string.Equals(parts[0], "story_mode", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(parts[1]))
            {
                return false;
            }

            if (parts.Length == 3)
            {
                return IsQuickSaveFile(parts[2]);
            }

            if (parts.Length == 4
                && IsChapterSaveFolder(parts[2])
                && string.Equals(parts[3], GenericSaveFile, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return parts.Length == 5
                && string.Equals(parts[2], "manual_saves", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(parts[3])
                && string.Equals(parts[4], GenericSaveFile, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsQuickSaveFile(string fileName)
        {
            return string.Equals(fileName, "auto_save.json", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fileName, "manual_save.json", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsChapterSaveFolder(string folderName)
        {
            const string chapterPrefix = "chapter_";
            return !string.IsNullOrEmpty(folderName)
                && folderName.Length == chapterPrefix.Length + 1
                && folderName.StartsWith(chapterPrefix, StringComparison.OrdinalIgnoreCase)
                && folderName[chapterPrefix.Length] >= '0'
                && folderName[chapterPrefix.Length] <= '6';
        }

        internal static string GetLegacyOwnerKey(string normalizedVanillaSaveFilePath)
        {
            try
            {
                string ownerDirectory = Path.GetDirectoryName(normalizedVanillaSaveFilePath);
                if (string.IsNullOrEmpty(ownerDirectory))
                {
                    return "";
                }
                string owner = Path.GetFileName(ownerDirectory.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar));
                if (string.Equals(owner, GameDataFolder, StringComparison.OrdinalIgnoreCase))
                {
                    owner = Path.GetFileNameWithoutExtension(normalizedVanillaSaveFilePath);
                }
                return owner ?? "";
            }
            catch
            {
                return "";
            }
        }

        internal static string GetLegacyFolderKey()
        {
            try
            {
                if (staticVars.PlayerData == null)
                {
                    return "";
                }

                string folder = staticVars.PlayerData.SaveFolderName;
                if (string.IsNullOrEmpty(folder) && staticVars.PlayerData.IsStoryMode)
                {
                    folder = staticVars.PlayerData.GetSaveFolderName();
                }
                string folderToken = SanitizeLegacyFileToken(folder);
                return folderToken;
            }
            catch
            {
                return "";
            }
        }

        internal static string GetLegacyFallbackKey()
        {
            try
            {
                if (staticVars.PlayerData == null)
                {
                    return "";
                }

                return SanitizeLegacyFileToken(string.Join("_", new string[]
                {
                    staticVars.PlayerData.IsStoryMode ? "story" : "freeplay",
                    staticVars.PlayerData.FirstName ?? "",
                    staticVars.PlayerData.LastName ?? "",
                    staticVars.PlayerData.GroupName ?? "",
                    staticVars.PlayerData.Chapter.ToString()
                }));
            }
            catch
            {
                return "";
            }
        }

        internal static string SanitizeLegacyFileToken(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            HashSet<char> invalidSet = new HashSet<char>(invalid);
            char[] chars = new char[value.Length];
            int count = 0;

            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                {
                    chars[count++] = c;
                    continue;
                }
                if (!char.IsControl(c) && !invalidSet.Contains(c))
                {
                    chars[count++] = '_';
                }
            }

            if (count == 0)
            {
                return "";
            }

            string token = new string(chars, 0, count).Trim('_');
            while (token.Contains("__"))
            {
                token = token.Replace("__", "_");
            }
            if (token.Length > 64)
            {
                token = token.Substring(0, 64);
            }
            return token;
        }
    }

    internal enum GraduationDetailsSnapshotResolution
    {
        Missing = 0,
        Valid = 1,
        Invalid = 2
    }

    [Serializable]
    internal sealed class GraduationDetailsSnapshotManifest
    {
        public int SchemaVersion = 1;
        public string TransactionId = "";
        public string TargetVanillaRelativePath = "";
        public string ExpectedVanillaSha256 = "";
        public long ExpectedVanillaLength = -1L;
        public bool PreWriteExisted;
        public string PreWriteSha256 = "";
        public long PreWriteLength = -1L;
        public long PreWriteLastWriteUtcTicks;
        public long CreatedUtcTicks;
        public string SourceKind = "live_save";
    }

    internal sealed class GraduationDetailsPreparedSave
    {
        internal string TransactionId = "";
        internal string NormalizedVanillaSaveFilePath = "";
        internal string SaveDirectory = "";
        internal string StageDirectory = "";
        internal string FinalDirectory = "";
        internal string ExpectedVanillaSha256 = "";
        internal long ExpectedVanillaLength;
        internal GraduationDetailsPersistenceIO.FileFingerprint PreWriteFingerprint;
        internal int RuntimeEpoch;
        internal long Sequence;
        internal float Deadline;
        internal bool WriteObserved;
        internal bool Expired;
        internal float MatchingSince = -1f;
        internal float NextPoll;
    }

    internal static class GraduationDetailsSnapshotStorage
    {
        private const int ManifestSchemaVersion = 1;
        private static readonly long AbandonedTransactionRetentionTicks =
            TimeSpan.FromHours(24).Ticks;

        internal static GraduationDetailsSnapshotResolution ResolveReadableSnapshot(
            string saveDirectory,
            string normalizedVanillaSaveFilePath,
            out string readableDirectory)
        {
            readableDirectory = "";
            if (string.IsNullOrEmpty(saveDirectory)
                || string.IsNullOrEmpty(normalizedVanillaSaveFilePath))
            {
                return GraduationDetailsSnapshotResolution.Invalid;
            }

            string pointerPath;
            string backupPath;
            bool pointerPathsValid = TryGetPointerPaths(
                saveDirectory,
                out pointerPath,
                out backupPath);
            bool pointerExists = PointerArtifactExists(saveDirectory);

            string token;
            string generationDirectory;
            if (pointerPathsValid && TryResolvePointer(
                pointerPath,
                saveDirectory,
                normalizedVanillaSaveFilePath,
                out token,
                out generationDirectory))
            {
                readableDirectory = generationDirectory;
                return GraduationDetailsSnapshotResolution.Valid;
            }

            // File.Replace retains the previous pointer as a backup. Validate its contents and
            // referenced generation before using it; never restore untrusted pointer text.
            if (pointerPathsValid && TryResolvePointer(
                backupPath,
                saveDirectory,
                normalizedVanillaSaveFilePath,
                out token,
                out generationDirectory))
            {
                try
                {
                    WriteActivePointer(saveDirectory, token);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[Graduation Details] Failed to repair snapshot pointer: " + exception.Message);
                }
                readableDirectory = generationDirectory;
                return GraduationDetailsSnapshotResolution.Valid;
            }

            if (pointerExists)
            {
                return GraduationDetailsSnapshotResolution.Invalid;
            }

            bool hasLegacyData;
            if (!ValidateLegacyDirectory(saveDirectory, out hasLegacyData))
            {
                return GraduationDetailsSnapshotResolution.Invalid;
            }
            if (hasLegacyData)
            {
                readableDirectory = saveDirectory;
                return GraduationDetailsSnapshotResolution.Valid;
            }
            return GraduationDetailsSnapshotResolution.Missing;
        }

        internal static bool TryStageLiveSnapshot(
            string normalizedVanillaSaveFilePath,
            string saveDirectory,
            byte[] expectedVanillaPayload,
            GraduationDetailsPersistenceIO.FileFingerprint preWriteFingerprint,
            int runtimeEpoch,
            long sequence,
            out GraduationDetailsPreparedSave prepared)
        {
            prepared = null;
            if (expectedVanillaPayload == null
                || preWriteFingerprint == null
                || !MarriageRecordStore.CanSnapshot
                || !StaffIdolStore.CanSnapshot
                || !GraduationSnapshotStore.CanSnapshot)
            {
                return false;
            }

            string relativeVanillaPath = GraduationDetailsPaths.GetVanillaRelativePath(
                normalizedVanillaSaveFilePath);
            if (string.IsNullOrEmpty(relativeVanillaPath))
            {
                return false;
            }

            string transactionId = Guid.NewGuid().ToString("N");
            string transactionsDirectory;
            string committedDirectory;
            string stageDirectory;
            string finalDirectory;
            if (!TryGetTransactionPaths(
                saveDirectory,
                transactionId,
                out transactionsDirectory,
                out committedDirectory,
                out stageDirectory,
                out finalDirectory))
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(transactionsDirectory);
                Directory.CreateDirectory(stageDirectory);
                MarriageRecordStore.SaveToDirectory(stageDirectory);
                StaffIdolStore.SaveToDirectory(stageDirectory);
                GraduationSnapshotStore.SaveToDirectory(stageDirectory);

                if (!ValidateCompleteDataDirectory(stageDirectory))
                {
                    throw new InvalidDataException("The staged sidecar snapshot failed validation.");
                }

                GraduationDetailsSnapshotManifest manifest = new GraduationDetailsSnapshotManifest
                {
                    SchemaVersion = ManifestSchemaVersion,
                    TransactionId = transactionId,
                    TargetVanillaRelativePath = relativeVanillaPath,
                    ExpectedVanillaSha256 = GraduationDetailsPersistenceIO.ComputeSha256(
                        expectedVanillaPayload),
                    ExpectedVanillaLength = expectedVanillaPayload.LongLength,
                    PreWriteExisted = preWriteFingerprint.Exists,
                    PreWriteSha256 = preWriteFingerprint.Sha256 ?? "",
                    PreWriteLength = preWriteFingerprint.Length,
                    PreWriteLastWriteUtcTicks = preWriteFingerprint.LastWriteUtcTicks,
                    CreatedUtcTicks = DateTime.UtcNow.Ticks,
                    SourceKind = "live_save"
                };
                string manifestPath;
                if (!GraduationDetailsPaths.TryGetSafeLeafPath(
                    stageDirectory,
                    GraduationDetailsPaths.SnapshotManifestFile,
                    out manifestPath))
                {
                    throw new InvalidDataException("The staged manifest path escaped its transaction.");
                }
                GraduationDetailsPersistenceIO.WriteUtf8Durable(
                    manifestPath,
                    JsonUtility.ToJson(manifest, true));

                prepared = new GraduationDetailsPreparedSave
                {
                    TransactionId = transactionId,
                    NormalizedVanillaSaveFilePath = normalizedVanillaSaveFilePath,
                    SaveDirectory = saveDirectory,
                    StageDirectory = stageDirectory,
                    FinalDirectory = finalDirectory,
                    ExpectedVanillaSha256 = manifest.ExpectedVanillaSha256,
                    ExpectedVanillaLength = manifest.ExpectedVanillaLength,
                    PreWriteFingerprint = preWriteFingerprint,
                    RuntimeEpoch = runtimeEpoch,
                    Sequence = sequence
                };
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Graduation Details] Failed to stage sidecar snapshot: " + exception.Message);
                TryDeleteOwnedTransactionDirectory(saveDirectory, stageDirectory);
                return false;
            }
        }

        internal static bool TryPublishPreparedSnapshot(
            GraduationDetailsPreparedSave prepared,
            out string committedDirectory)
        {
            committedDirectory = "";
            if (prepared == null)
            {
                return false;
            }

            try
            {
                GraduationDetailsPersistenceIO.FileFingerprint current;
                if (!GraduationDetailsPersistenceIO.TryCaptureFingerprint(
                    prepared.NormalizedVanillaSaveFilePath,
                    out current)
                    || !FingerprintMatchesExpected(
                        current,
                        prepared.ExpectedVanillaSha256,
                        prepared.ExpectedVanillaLength))
                {
                    return false;
                }

                GraduationDetailsSnapshotManifest manifest;
                if (!TryReadAndValidateManifest(
                    prepared.StageDirectory,
                    prepared.TransactionId,
                    prepared.NormalizedVanillaSaveFilePath,
                    current,
                    out manifest)
                    || !ValidateCompleteDataDirectory(prepared.StageDirectory))
                {
                    return false;
                }

                string committedRoot;
                string ignoredTransactionsRoot;
                string ignoredStage;
                string expectedFinal;
                if (!TryGetTransactionPaths(
                    prepared.SaveDirectory,
                    prepared.TransactionId,
                    out ignoredTransactionsRoot,
                    out committedRoot,
                    out ignoredStage,
                    out expectedFinal)
                    || !string.Equals(
                        expectedFinal,
                        prepared.FinalDirectory,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                Directory.CreateDirectory(committedRoot);
                if (!Directory.Exists(prepared.FinalDirectory))
                {
                    GraduationSnapshotStore.RemovePendingPortraitTargetsUnderDirectory(
                        prepared.StageDirectory);
                    Directory.Move(prepared.StageDirectory, prepared.FinalDirectory);
                }
                else
                {
                    GraduationDetailsSnapshotManifest existingManifest;
                    if (!TryReadAndValidateManifest(
                            prepared.FinalDirectory,
                            prepared.TransactionId,
                            prepared.NormalizedVanillaSaveFilePath,
                            current,
                            out existingManifest)
                        || !ValidateCompleteDataDirectory(prepared.FinalDirectory))
                    {
                        return false;
                    }
                }
                WriteActivePointer(prepared.SaveDirectory, prepared.TransactionId);
                committedDirectory = prepared.FinalDirectory;
                GraduationSnapshotStore.RegisterPendingPortraitTargetsForDirectory(
                    committedDirectory);
                PruneCommittedGenerations(prepared.SaveDirectory);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Graduation Details] Failed to publish sidecar snapshot: " + exception.Message);
                return false;
            }
        }

        internal static bool TryRecoverForVanillaSave(
            string saveDirectory,
            string normalizedVanillaSaveFilePath)
        {
            GraduationDetailsPersistenceIO.FileFingerprint current;
            if (!GraduationDetailsPersistenceIO.TryCaptureFingerprint(
                normalizedVanillaSaveFilePath,
                out current)
                || current == null
                || !current.Exists)
            {
                return false;
            }

            List<RecoveryCandidate> candidates = new List<RecoveryCandidate>();
            CollectRecoveryCandidates(
                saveDirectory,
                normalizedVanillaSaveFilePath,
                current,
                GraduationDetailsPaths.CommittedSnapshotsFolder,
                false,
                candidates);
            CollectRecoveryCandidates(
                saveDirectory,
                normalizedVanillaSaveFilePath,
                current,
                GraduationDetailsPaths.TransactionsFolder,
                true,
                candidates);

            RecoveryCandidate selected = candidates
                .OrderByDescending(candidate => candidate.CreatedUtcTicks)
                .FirstOrDefault();
            if (selected == null)
            {
                RollBackStagingDirectories(
                    saveDirectory,
                    normalizedVanillaSaveFilePath,
                    current,
                    "",
                    false);
                return false;
            }

            try
            {
                string currentActiveToken = "";
                string currentActiveDirectory;
                string pointerPath;
                string backupPath;
                if (TryGetPointerPaths(saveDirectory, out pointerPath, out backupPath))
                {
                    TryResolvePointer(
                        pointerPath,
                        saveDirectory,
                        normalizedVanillaSaveFilePath,
                        out currentActiveToken,
                        out currentActiveDirectory);
                }
                string finalDirectory = selected.Directory;
                if (selected.IsStaging)
                {
                    string transactionsRoot;
                    string committedRoot;
                    string expectedStage;
                    if (!TryGetTransactionPaths(
                        saveDirectory,
                        selected.TransactionId,
                        out transactionsRoot,
                        out committedRoot,
                        out expectedStage,
                        out finalDirectory)
                        || !string.Equals(
                            expectedStage,
                            selected.Directory,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    Directory.CreateDirectory(committedRoot);
                    if (!Directory.Exists(finalDirectory))
                    {
                        GraduationSnapshotStore.RemovePendingPortraitTargetsUnderDirectory(
                            selected.Directory);
                        Directory.Move(selected.Directory, finalDirectory);
                    }
                    else
                    {
                        GraduationDetailsSnapshotManifest existingManifest;
                        if (!TryReadAndValidateManifest(
                                finalDirectory,
                                selected.TransactionId,
                                normalizedVanillaSaveFilePath,
                                current,
                                out existingManifest)
                            || !ValidateCompleteDataDirectory(finalDirectory))
                        {
                            return false;
                        }
                    }
                }
                if (!string.Equals(
                    currentActiveToken,
                    selected.TransactionId,
                    StringComparison.Ordinal))
                {
                    WriteActivePointer(saveDirectory, selected.TransactionId);
                }
                RollBackStagingDirectories(
                    saveDirectory,
                    normalizedVanillaSaveFilePath,
                    current,
                    "",
                    true);
                PruneCommittedGenerations(saveDirectory);
                Debug.Log("[Graduation Details] Recovered committed sidecar snapshot " +
                    selected.TransactionId);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Graduation Details] Sidecar recovery failed: " + exception.Message);
                return false;
            }
        }

        internal static bool TryInstallMigratedSnapshot(
            string saveDirectory,
            string normalizedVanillaSaveFilePath,
            string scopedSourceDirectory,
            string relatedRootFlatDirectory,
            string sourceKind)
        {
            GraduationDetailsPersistenceIO.FileFingerprint current;
            if (!GraduationDetailsPersistenceIO.TryCaptureFingerprint(
                normalizedVanillaSaveFilePath,
                out current)
                || current == null
                || !current.Exists)
            {
                return false;
            }

            string transactionId = Guid.NewGuid().ToString("N");
            string transactionsRoot;
            string committedRoot;
            string stageDirectory;
            string finalDirectory;
            if (!TryGetTransactionPaths(
                saveDirectory,
                transactionId,
                out transactionsRoot,
                out committedRoot,
                out stageDirectory,
                out finalDirectory))
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(transactionsRoot);
                Directory.CreateDirectory(stageDirectory);
                if (!StageMigratedDataFile(
                        saveDirectory,
                        scopedSourceDirectory,
                        relatedRootFlatDirectory,
                        stageDirectory,
                        GraduationDetailsPaths.MarriageFile,
                        MarriageRecordStore.TryValidateFile,
                        MarriageRecordStore.GetEmptyJson())
                    || !StageMigratedDataFile(
                        saveDirectory,
                        scopedSourceDirectory,
                        relatedRootFlatDirectory,
                        stageDirectory,
                        GraduationDetailsPaths.StaffMapFile,
                        StaffIdolStore.TryValidateFile,
                        StaffIdolStore.GetEmptyJson())
                    || !StageMigratedDataFile(
                        saveDirectory,
                        scopedSourceDirectory,
                        relatedRootFlatDirectory,
                        stageDirectory,
                        GraduationDetailsPaths.SnapshotsFile,
                        GraduationSnapshotStore.TryValidateFile,
                        GraduationSnapshotStore.GetEmptyJson()))
                {
                    throw new InvalidDataException(
                        "A corrupt current sidecar file had no validated recovery source.");
                }

                CopyValidatedPortraitLeaves(saveDirectory, stageDirectory);
                CopyValidatedPortraitLeaves(scopedSourceDirectory, stageDirectory);
                CopyValidatedPortraitLeaves(relatedRootFlatDirectory, stageDirectory);

                if (!ValidateCompleteDataDirectory(stageDirectory))
                {
                    throw new InvalidDataException("The migrated sidecar snapshot failed validation.");
                }

                GraduationDetailsSnapshotManifest manifest = new GraduationDetailsSnapshotManifest
                {
                    SchemaVersion = ManifestSchemaVersion,
                    TransactionId = transactionId,
                    TargetVanillaRelativePath = GraduationDetailsPaths.GetVanillaRelativePath(
                        normalizedVanillaSaveFilePath),
                    ExpectedVanillaSha256 = current.Sha256,
                    ExpectedVanillaLength = current.Length,
                    PreWriteExisted = current.Exists,
                    PreWriteSha256 = current.Sha256,
                    PreWriteLength = current.Length,
                    PreWriteLastWriteUtcTicks = current.LastWriteUtcTicks,
                    CreatedUtcTicks = DateTime.UtcNow.Ticks,
                    SourceKind = IsKnownMigrationSourceKind(sourceKind)
                        ? sourceKind
                        : "legacy_migration"
                };
                string manifestPath;
                if (!GraduationDetailsPaths.TryGetSafeLeafPath(
                    stageDirectory,
                    GraduationDetailsPaths.SnapshotManifestFile,
                    out manifestPath))
                {
                    throw new InvalidDataException("The migration manifest path was unsafe.");
                }
                GraduationDetailsPersistenceIO.WriteUtf8Durable(
                    manifestPath,
                    JsonUtility.ToJson(manifest, true));

                GraduationDetailsPreparedSave prepared = new GraduationDetailsPreparedSave
                {
                    TransactionId = transactionId,
                    NormalizedVanillaSaveFilePath = normalizedVanillaSaveFilePath,
                    SaveDirectory = saveDirectory,
                    StageDirectory = stageDirectory,
                    FinalDirectory = finalDirectory,
                    ExpectedVanillaSha256 = current.Sha256,
                    ExpectedVanillaLength = current.Length,
                    PreWriteFingerprint = current
                };
                string publishedDirectory;
                return TryPublishPreparedSnapshot(prepared, out publishedDirectory);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Graduation Details] Validated legacy migration failed: " + exception.Message);
                TryDeleteOwnedTransactionDirectory(saveDirectory, stageDirectory);
                return false;
            }
        }

        internal static void DiscardPreparedSnapshot(GraduationDetailsPreparedSave prepared)
        {
            if (prepared != null)
            {
                GraduationSnapshotStore.RemovePendingPortraitTargetsUnderDirectory(
                    prepared.StageDirectory);
                TryDeleteOwnedTransactionDirectory(
                    prepared.SaveDirectory,
                    prepared.StageDirectory);
            }
        }

        internal static bool IsPreparedSnapshotValidForCurrentPayload(
            GraduationDetailsPreparedSave prepared,
            GraduationDetailsPersistenceIO.FileFingerprint current)
        {
            try
            {
                GraduationDetailsSnapshotManifest manifest;
                return prepared != null
                    && FingerprintMatchesExpected(
                        current,
                        prepared.ExpectedVanillaSha256,
                        prepared.ExpectedVanillaLength)
                    && TryReadAndValidateManifest(
                        prepared.StageDirectory,
                        prepared.TransactionId,
                        prepared.NormalizedVanillaSaveFilePath,
                        current,
                        out manifest)
                    && ValidateCompleteDataDirectory(prepared.StageDirectory);
            }
            catch
            {
                return false;
            }
        }

        internal static bool ValidateCompleteDataDirectory(string directory)
        {
            string marriagePath;
            string staffPath;
            string snapshotsPath;
            return TryGetDataPaths(directory, out marriagePath, out staffPath, out snapshotsPath)
                && File.Exists(marriagePath)
                && File.Exists(staffPath)
                && File.Exists(snapshotsPath)
                && MarriageRecordStore.TryValidateFile(marriagePath)
                && StaffIdolStore.TryValidateFile(staffPath)
                && GraduationSnapshotStore.TryValidateFile(snapshotsPath);
        }

        internal static bool ValidateLegacyDirectory(string directory, out bool hasData)
        {
            hasData = false;
            string marriagePath;
            string staffPath;
            string snapshotsPath;
            if (!TryGetDataPaths(directory, out marriagePath, out staffPath, out snapshotsPath))
            {
                return false;
            }
            string[] paths = new string[] { marriagePath, staffPath, snapshotsPath };
            Func<string, bool>[] validators = new Func<string, bool>[]
            {
                MarriageRecordStore.TryValidateFile,
                StaffIdolStore.TryValidateFile,
                GraduationSnapshotStore.TryValidateFile
            };
            for (int i = 0; i < paths.Length; i++)
            {
                if (!File.Exists(paths[i]))
                {
                    continue;
                }
                hasData = true;
                if (!validators[i](paths[i]))
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool TryGetDataPaths(
            string directory,
            out string marriagePath,
            out string staffPath,
            out string snapshotsPath)
        {
            marriagePath = "";
            staffPath = "";
            snapshotsPath = "";
            return GraduationDetailsPaths.TryGetSafeLeafPath(
                    directory,
                    GraduationDetailsPaths.MarriageFile,
                    out marriagePath)
                && GraduationDetailsPaths.TryGetSafeLeafPath(
                    directory,
                    GraduationDetailsPaths.StaffMapFile,
                    out staffPath)
                && GraduationDetailsPaths.TryGetSafeLeafPath(
                    directory,
                    GraduationDetailsPaths.SnapshotsFile,
                    out snapshotsPath);
        }

        private static bool StageMigratedDataFile(
            string currentDirectory,
            string scopedSourceDirectory,
            string relatedRootFlatDirectory,
            string stageDirectory,
            string fileName,
            Func<string, bool> validator,
            string emptyJson)
        {
            string destinationPath;
            if (!GraduationDetailsPaths.TryGetSafeLeafPath(
                stageDirectory,
                fileName,
                out destinationPath))
            {
                return false;
            }

            string[] candidates = new string[]
            {
                currentDirectory,
                scopedSourceDirectory,
                relatedRootFlatDirectory
            };
            bool corruptCurrentFile = false;
            for (int i = 0; i < candidates.Length; i++)
            {
                string candidateDirectory = candidates[i];
                string candidatePath;
                if (string.IsNullOrEmpty(candidateDirectory)
                    || !GraduationDetailsPersistenceIO.TryGetSafeLeafPath(
                        candidateDirectory,
                        fileName,
                        false,
                        out candidatePath)
                    || !File.Exists(candidatePath))
                {
                    continue;
                }
                if (!validator(candidatePath))
                {
                    if (i == 0)
                    {
                        corruptCurrentFile = true;
                    }
                    continue;
                }
                GraduationDetailsPersistenceIO.CopyFileDurable(
                    candidatePath,
                    destinationPath);
                return true;
            }
            if (corruptCurrentFile)
            {
                return false;
            }
            GraduationDetailsPersistenceIO.WriteUtf8Durable(destinationPath, emptyJson);
            return true;
        }

        private static void CopyValidatedPortraitLeaves(
            string sourceDataDirectory,
            string stageDirectory)
        {
            if (string.IsNullOrEmpty(sourceDataDirectory))
            {
                return;
            }
            string sourcePortraitDirectory;
            string destinationPortraitDirectory;
            if (!GraduationDetailsPersistenceIO.TryGetContainedPath(
                    sourceDataDirectory,
                    GraduationDetailsPaths.PortraitsFolder,
                    false,
                    out sourcePortraitDirectory)
                || !Directory.Exists(sourcePortraitDirectory)
                || !GraduationDetailsPersistenceIO.TryGetContainedPath(
                    stageDirectory,
                    GraduationDetailsPaths.PortraitsFolder,
                    true,
                    out destinationPortraitDirectory))
            {
                return;
            }
            foreach (string sourceFile in Directory.GetFiles(sourcePortraitDirectory))
            {
                string fileName = Path.GetFileName(sourceFile);
                string validatedSource;
                string destination;
                if (!GraduationDetailsPaths.IsSafePortraitFileName(fileName)
                    || !GraduationDetailsPersistenceIO.TryGetSafeLeafPath(
                        sourcePortraitDirectory,
                        fileName,
                        false,
                        out validatedSource)
                    || !string.Equals(
                        validatedSource,
                        Path.GetFullPath(sourceFile),
                        StringComparison.OrdinalIgnoreCase)
                    || !GraduationDetailsPersistenceIO.TryGetSafeLeafPath(
                        destinationPortraitDirectory,
                        fileName,
                        true,
                        out destination)
                    || File.Exists(destination))
                {
                    continue;
                }
                GraduationDetailsPersistenceIO.CopyFileDurable(validatedSource, destination);
            }
        }

        private static bool TryResolvePointer(
            string pointerPath,
            string saveDirectory,
            string normalizedVanillaSaveFilePath,
            out string token,
            out string generationDirectory)
        {
            token = "";
            generationDirectory = "";
            try
            {
                if (string.IsNullOrEmpty(pointerPath) || !File.Exists(pointerPath))
                {
                    return false;
                }
                token = File.ReadAllText(pointerPath).Trim();
                if (!IsSafeTransactionId(token))
                {
                    return false;
                }
                string transactionsRoot;
                string committedRoot;
                string ignoredStage;
                if (!TryGetTransactionPaths(
                    saveDirectory,
                    token,
                    out transactionsRoot,
                    out committedRoot,
                    out ignoredStage,
                    out generationDirectory)
                    || !Directory.Exists(generationDirectory))
                {
                    return false;
                }
                GraduationDetailsPersistenceIO.FileFingerprint current;
                GraduationDetailsSnapshotManifest manifest;
                return GraduationDetailsPersistenceIO.TryCaptureFingerprint(
                        normalizedVanillaSaveFilePath,
                        out current)
                    && TryReadAndValidateManifest(
                        generationDirectory,
                        token,
                        normalizedVanillaSaveFilePath,
                        current,
                        out manifest)
                    && ValidateCompleteDataDirectory(generationDirectory);
            }
            catch
            {
                token = "";
                generationDirectory = "";
                return false;
            }
        }

        private static bool TryReadAndValidateManifest(
            string directory,
            string expectedTransactionId,
            string normalizedVanillaSaveFilePath,
            GraduationDetailsPersistenceIO.FileFingerprint currentVanillaFingerprint,
            out GraduationDetailsSnapshotManifest manifest)
        {
            manifest = null;
            try
            {
                string manifestPath;
                if (!GraduationDetailsPaths.TryGetSafeLeafPath(
                    directory,
                    GraduationDetailsPaths.SnapshotManifestFile,
                    out manifestPath)
                    || !File.Exists(manifestPath))
                {
                    return false;
                }
                string json = File.ReadAllText(manifestPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }
                manifest = JsonUtility.FromJson<GraduationDetailsSnapshotManifest>(json);
                string expectedRelativePath = GraduationDetailsPaths.GetVanillaRelativePath(
                    normalizedVanillaSaveFilePath);
                return manifest != null
                    && manifest.SchemaVersion == ManifestSchemaVersion
                    && IsSafeTransactionId(manifest.TransactionId)
                    && IsKnownSourceKind(manifest.SourceKind)
                    && manifest.CreatedUtcTicks > 0L
                    && manifest.CreatedUtcTicks <= DateTime.UtcNow.AddMinutes(5).Ticks
                    && string.Equals(
                        manifest.TransactionId,
                        expectedTransactionId,
                        StringComparison.Ordinal)
                    && IsSafeRelativeToken(manifest.TargetVanillaRelativePath)
                    && !string.IsNullOrEmpty(expectedRelativePath)
                    && string.Equals(
                        NormalizeRelativePath(manifest.TargetVanillaRelativePath),
                        NormalizeRelativePath(expectedRelativePath),
                        StringComparison.OrdinalIgnoreCase)
                    && FingerprintMatchesExpected(
                        currentVanillaFingerprint,
                        manifest.ExpectedVanillaSha256,
                        manifest.ExpectedVanillaLength)
                    && ManifestHasObservedWrite(manifest, currentVanillaFingerprint);
            }
            catch
            {
                manifest = null;
                return false;
            }
        }

        private static bool FingerprintMatchesExpected(
            GraduationDetailsPersistenceIO.FileFingerprint fingerprint,
            string expectedSha256,
            long expectedLength)
        {
            return fingerprint != null
                && fingerprint.Exists
                && expectedLength >= 0L
                && fingerprint.Length == expectedLength
                && !string.IsNullOrEmpty(expectedSha256)
                && string.Equals(
                    fingerprint.Sha256,
                    expectedSha256,
                    StringComparison.Ordinal);
        }

        private static string NormalizeRelativePath(string path)
        {
            return (path ?? "").Replace(
                Path.AltDirectorySeparatorChar,
                Path.DirectorySeparatorChar).TrimStart(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
        }

        private static bool IsSafeRelativeToken(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path))
            {
                return false;
            }
            string[] parts = path.Split(
                new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                StringSplitOptions.None);
            foreach (string part in parts)
            {
                if (string.IsNullOrWhiteSpace(part)
                    || string.Equals(part, ".", StringComparison.Ordinal)
                    || string.Equals(part, "..", StringComparison.Ordinal)
                    || !GraduationDetailsPaths.IsSafeLeafFileName(part))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool ManifestHasObservedWrite(
            GraduationDetailsSnapshotManifest manifest,
            GraduationDetailsPersistenceIO.FileFingerprint current)
        {
            if (manifest == null || current == null)
            {
                return false;
            }
            if (!string.Equals(manifest.SourceKind, "live_save", StringComparison.Ordinal))
            {
                return true;
            }
            if (!manifest.PreWriteExisted)
            {
                return current.Exists;
            }
            if (manifest.PreWriteLength != manifest.ExpectedVanillaLength
                || !string.Equals(
                    manifest.PreWriteSha256,
                    manifest.ExpectedVanillaSha256,
                    StringComparison.Ordinal))
            {
                return true;
            }
            return current.LastWriteUtcTicks != manifest.PreWriteLastWriteUtcTicks;
        }

        private static bool IsKnownSourceKind(string sourceKind)
        {
            return string.Equals(sourceKind, "live_save", StringComparison.Ordinal)
                || IsKnownMigrationSourceKind(sourceKind);
        }

        private static bool IsKnownMigrationSourceKind(string sourceKind)
        {
            return string.Equals(sourceKind, "legacy_migration", StringComparison.Ordinal)
                || string.Equals(sourceKind, "canonical_legacy", StringComparison.Ordinal)
                || string.Equals(sourceKind, "scoped_legacy", StringComparison.Ordinal)
                || string.Equals(sourceKind, "marked_root_flat", StringComparison.Ordinal);
        }

        private static bool IsSafeTransactionId(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId) || transactionId.Length != 32)
            {
                return false;
            }
            for (int i = 0; i < transactionId.Length; i++)
            {
                char c = transactionId[i];
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool TryGetTransactionPaths(
            string saveDirectory,
            string transactionId,
            out string transactionsRoot,
            out string committedRoot,
            out string stageDirectory,
            out string finalDirectory)
        {
            transactionsRoot = "";
            committedRoot = "";
            stageDirectory = "";
            finalDirectory = "";
            string validatedSaveDirectory;
            return IsSafeTransactionId(transactionId)
                && GraduationDetailsPaths.IsPathContainedBy(
                    GraduationDetailsPaths.RootDir,
                    saveDirectory)
                && GraduationDetailsPersistenceIO.TryValidatePathUnderRoot(
                    Application.persistentDataPath,
                    saveDirectory,
                    false,
                    out validatedSaveDirectory)
                && GraduationDetailsPaths.TryGetContainedPath(
                    validatedSaveDirectory,
                    GraduationDetailsPaths.TransactionsFolder,
                    out transactionsRoot)
                && GraduationDetailsPaths.TryGetContainedPath(
                    validatedSaveDirectory,
                    GraduationDetailsPaths.CommittedSnapshotsFolder,
                    out committedRoot)
                && GraduationDetailsPersistenceIO.TryGetContainedPath(
                    transactionsRoot,
                    transactionId,
                    true,
                    out stageDirectory)
                && GraduationDetailsPersistenceIO.TryGetContainedPath(
                    committedRoot,
                    transactionId,
                    true,
                    out finalDirectory);
        }

        private static bool TryGetPointerPaths(
            string saveDirectory,
            out string pointerPath,
            out string backupPath)
        {
            pointerPath = "";
            backupPath = "";
            string validatedSaveDirectory;
            return GraduationDetailsPaths.IsPathContainedBy(
                    GraduationDetailsPaths.RootDir,
                    saveDirectory)
                && GraduationDetailsPersistenceIO.TryValidatePathUnderRoot(
                    Application.persistentDataPath,
                    saveDirectory,
                    false,
                    out validatedSaveDirectory)
                && GraduationDetailsPaths.TryGetSafeLeafPath(
                    validatedSaveDirectory,
                    GraduationDetailsPaths.ActiveSnapshotFile,
                    out pointerPath)
                && GraduationDetailsPaths.TryGetSafeLeafPath(
                    validatedSaveDirectory,
                    GraduationDetailsPaths.ActiveSnapshotBackupFile,
                    out backupPath);
        }

        private static bool PointerArtifactExists(string saveDirectory)
        {
            try
            {
                string validated;
                if (!GraduationDetailsPaths.TryValidateOwnedDataDirectory(
                    saveDirectory,
                    out validated))
                {
                    return true;
                }
                if (!Directory.Exists(validated))
                {
                    return false;
                }
                return Directory.GetFileSystemEntries(
                        validated,
                        GraduationDetailsPaths.ActiveSnapshotFile,
                        SearchOption.TopDirectoryOnly).Length > 0
                    || Directory.GetFileSystemEntries(
                        validated,
                        GraduationDetailsPaths.ActiveSnapshotBackupFile,
                        SearchOption.TopDirectoryOnly).Length > 0;
            }
            catch
            {
                // If the directory cannot be safely inspected, treat it as an invalid pointer
                // scope rather than authoring replacement sidecars into it.
                return true;
            }
        }

        private static void WriteActivePointer(string saveDirectory, string transactionId)
        {
            if (!IsSafeTransactionId(transactionId))
            {
                throw new InvalidDataException("Unsafe active snapshot transaction ID.");
            }
            string pointerPath;
            string backupPath;
            if (!TryGetPointerPaths(saveDirectory, out pointerPath, out backupPath))
            {
                throw new InvalidDataException("Unsafe active snapshot pointer path.");
            }
            GraduationDetailsPersistenceIO.WritePointerAtomically(
                pointerPath,
                backupPath,
                transactionId);
        }

        private static void CollectRecoveryCandidates(
            string saveDirectory,
            string normalizedVanillaSaveFilePath,
            GraduationDetailsPersistenceIO.FileFingerprint current,
            string folderName,
            bool isStaging,
            List<RecoveryCandidate> candidates)
        {
            string root;
            if (!GraduationDetailsPaths.TryGetContainedPath(saveDirectory, folderName, out root)
                || !Directory.Exists(root))
            {
                return;
            }
            foreach (string directory in Directory.GetDirectories(root))
            {
                string token = Path.GetFileName(directory);
                string normalizedDirectory;
                GraduationDetailsSnapshotManifest manifest;
                if (!IsSafeTransactionId(token)
                    || !GraduationDetailsPersistenceIO.TryGetContainedPath(
                        root,
                        token,
                        true,
                        out normalizedDirectory)
                    || !string.Equals(directory, normalizedDirectory, StringComparison.OrdinalIgnoreCase)
                    || !TryReadAndValidateManifest(
                        directory,
                        token,
                        normalizedVanillaSaveFilePath,
                        current,
                        out manifest)
                    || !ValidateCompleteDataDirectory(directory))
                {
                    continue;
                }
                candidates.Add(new RecoveryCandidate
                {
                    TransactionId = token,
                    Directory = directory,
                    CreatedUtcTicks = manifest.CreatedUtcTicks,
                    IsStaging = isStaging
                });
            }
        }

        private static void RollBackStagingDirectories(
            string saveDirectory,
            string normalizedVanillaSaveFilePath,
            GraduationDetailsPersistenceIO.FileFingerprint current,
            string keepTransactionId,
            bool committedSnapshotExists)
        {
            string transactionsRoot;
            if (!GraduationDetailsPaths.TryGetContainedPath(
                saveDirectory,
                GraduationDetailsPaths.TransactionsFolder,
                out transactionsRoot)
                || !Directory.Exists(transactionsRoot))
            {
                return;
            }
            foreach (string directory in Directory.GetDirectories(transactionsRoot))
            {
                string token = Path.GetFileName(directory);
                if (string.Equals(token, keepTransactionId, StringComparison.Ordinal))
                {
                    continue;
                }
                GraduationDetailsSnapshotManifest matchingManifest;
                bool matchesCurrent = IsSafeTransactionId(token)
                    && TryReadAndValidateManifest(
                        directory,
                        token,
                        normalizedVanillaSaveFilePath,
                        current,
                        out matchingManifest)
                    && ValidateCompleteDataDirectory(directory);
                if (matchesCurrent && !committedSnapshotExists)
                {
                    continue;
                }
                GraduationDetailsSnapshotManifest rawManifest;
                bool rawValid = TryReadRawManifest(directory, token, out rawManifest);
                long createdTicks = rawValid
                    ? rawManifest.CreatedUtcTicks
                    : Directory.GetCreationTimeUtc(directory).Ticks;
                bool oldEnough = createdTicks > 0L
                    && DateTime.UtcNow.Ticks - createdTicks
                        >= AbandonedTransactionRetentionTicks;
                if ((matchesCurrent && committedSnapshotExists) || oldEnough)
                {
                    TryDeleteOwnedTransactionDirectory(saveDirectory, directory);
                }
            }
        }

        private static bool TryReadRawManifest(
            string directory,
            string expectedTransactionId,
            out GraduationDetailsSnapshotManifest manifest)
        {
            manifest = null;
            try
            {
                string manifestPath;
                if (!IsSafeTransactionId(expectedTransactionId)
                    || !GraduationDetailsPaths.TryGetSafeLeafPath(
                        directory,
                        GraduationDetailsPaths.SnapshotManifestFile,
                        out manifestPath)
                    || !File.Exists(manifestPath))
                {
                    return false;
                }
                string json = File.ReadAllText(manifestPath);
                manifest = JsonUtility.FromJson<GraduationDetailsSnapshotManifest>(json);
                return manifest != null
                    && manifest.SchemaVersion == ManifestSchemaVersion
                    && string.Equals(
                        manifest.TransactionId,
                        expectedTransactionId,
                        StringComparison.Ordinal)
                    && IsSafeRelativeToken(manifest.TargetVanillaRelativePath)
                    && manifest.CreatedUtcTicks > 0L
                    && manifest.CreatedUtcTicks <= DateTime.UtcNow.AddMinutes(5).Ticks;
            }
            catch
            {
                manifest = null;
                return false;
            }
        }

        private static void TryDeleteOwnedTransactionDirectory(
            string saveDirectory,
            string directory)
        {
            try
            {
                string transactionsRoot;
                string committedRoot;
                string expectedStage;
                string ignoredFinal;
                string token = Path.GetFileName(directory);
                if (string.IsNullOrEmpty(directory)
                    || !IsSafeTransactionId(token)
                    || !TryGetTransactionPaths(
                        saveDirectory,
                        token,
                        out transactionsRoot,
                        out committedRoot,
                        out expectedStage,
                        out ignoredFinal)
                    || !string.Equals(
                        expectedStage,
                        directory,
                        StringComparison.OrdinalIgnoreCase)
                    || !Directory.Exists(directory)
                    || ContainsReparsePoint(directory))
                {
                    return;
                }
                Directory.Delete(directory, true);
            }
            catch
            {
                // A failed cleanup is harmless; recovery will inspect the durable manifest later.
            }
        }

        private static void PruneCommittedGenerations(string saveDirectory)
        {
            try
            {
                string pointerPath;
                string backupPath;
                if (!TryGetPointerPaths(saveDirectory, out pointerPath, out backupPath))
                {
                    return;
                }
                HashSet<string> protectedTokens = new HashSet<string>(StringComparer.Ordinal);
                if (!AddPointerToken(pointerPath, protectedTokens)
                    || !AddPointerToken(backupPath, protectedTokens))
                {
                    return;
                }

                string transactionsRoot;
                string committedRoot;
                string ignoredStage;
                string ignoredFinal;
                if (!TryGetTransactionPaths(
                        saveDirectory,
                        new string('0', 32),
                        out transactionsRoot,
                        out committedRoot,
                        out ignoredStage,
                        out ignoredFinal)
                    || !Directory.Exists(committedRoot))
                {
                    return;
                }
                foreach (string directory in Directory.GetDirectories(
                    committedRoot,
                    "*",
                    SearchOption.TopDirectoryOnly))
                {
                    string token = Path.GetFileName(directory);
                    if (!IsSafeTransactionId(token) || protectedTokens.Contains(token))
                    {
                        continue;
                    }
                    TryDeleteOwnedCommittedDirectory(saveDirectory, directory);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Graduation Details] Snapshot pruning was deferred: " +
                    exception.Message);
            }
        }

        private static bool AddPointerToken(string pointerPath, HashSet<string> tokens)
        {
            try
            {
                if (string.IsNullOrEmpty(pointerPath) || !File.Exists(pointerPath))
                {
                    return true;
                }
                string token = File.ReadAllText(pointerPath).Trim();
                if (!IsSafeTransactionId(token))
                {
                    return false;
                }
                tokens.Add(token);
                return true;
            }
            catch
            {
                // If either pointer cannot be read, fail closed and preserve every generation.
                return false;
            }
        }

        private static void TryDeleteOwnedCommittedDirectory(
            string saveDirectory,
            string directory)
        {
            try
            {
                string transactionsRoot;
                string committedRoot;
                string ignoredStage;
                string expectedFinal;
                string token = Path.GetFileName(directory);
                if (string.IsNullOrEmpty(directory)
                    || !IsSafeTransactionId(token)
                    || !TryGetTransactionPaths(
                        saveDirectory,
                        token,
                        out transactionsRoot,
                        out committedRoot,
                        out ignoredStage,
                        out expectedFinal)
                    || !string.Equals(
                        expectedFinal,
                        directory,
                        StringComparison.OrdinalIgnoreCase)
                    || !Directory.Exists(directory)
                    || ContainsReparsePoint(directory))
                {
                    return;
                }
                GraduationSnapshotStore.RemovePendingPortraitTargetsUnderDirectory(directory);
                Directory.Delete(directory, true);
            }
            catch
            {
                // Pruning is best effort; an active/backup generation is never a cleanup target.
            }
        }

        private static bool ContainsReparsePoint(string directory)
        {
            try
            {
                Stack<string> pending = new Stack<string>();
                pending.Push(directory);
                while (pending.Count > 0)
                {
                    string current = pending.Pop();
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    {
                        return true;
                    }
                    // Enumerate only one level. Checking a child before adding it prevents a
                    // junction from being followed outside the owned transaction tree.
                    foreach (string child in Directory.GetFileSystemEntries(current, "*",
                        SearchOption.TopDirectoryOnly))
                    {
                        FileAttributes attributes = File.GetAttributes(child);
                        if ((attributes & FileAttributes.ReparsePoint) != 0)
                        {
                            return true;
                        }
                        if ((attributes & FileAttributes.Directory) != 0)
                        {
                            pending.Push(child);
                        }
                    }
                }
                return false;
            }
            catch
            {
                // Cleanup must fail closed. A stranded transaction is harmless and can be
                // inspected again on a later load.
                return true;
            }
        }

        private sealed class RecoveryCandidate
        {
            internal string TransactionId = "";
            internal string Directory = "";
            internal long CreatedUtcTicks;
            internal bool IsStaging;
        }
    }

    internal static class GraduationDetailsLegacyMigration
    {
        private const string RootFlatMigrationMarker = ".root_flat_data_migrated";
        private const string ScopedMigrationOwnerMarker = ".scope_owner";
        private const string AssemblyFileName = "com.cosmo.graduationdetails.dll";
        private const string SteamAppsFolderName = "steamapps";
        private const string SteamCommonFolderName = "common";
        private const string SteamWorkshopFolderName = "workshop";
        private const string SteamWorkshopContentFolderName = "content";
        private const string IdolManagerSteamAppId = "821880";
        private const string LegacyGraduationDetailsWorkshopId = "3646637689";

        internal static void TryMigrateForScope(string targetDirectory, string vanillaSaveFilePath)
        {
            if (string.IsNullOrEmpty(targetDirectory) || string.IsNullOrEmpty(vanillaSaveFilePath))
            {
                return;
            }

            try
            {
                List<string> sourceRoots = GetLegacySourceRoots();
                string readableDirectory;
                GraduationDetailsSnapshotResolution currentResolution =
                    GraduationDetailsSnapshotStorage.ResolveReadableSnapshot(
                        targetDirectory,
                        vanillaSaveFilePath,
                        out readableDirectory);
                if (currentResolution == GraduationDetailsSnapshotResolution.Valid
                    && !PathsReferToSameDirectory(readableDirectory, targetDirectory))
                {
                    return;
                }
                if (currentResolution == GraduationDetailsSnapshotResolution.Invalid
                    && HasSnapshotPointer(targetDirectory))
                {
                    Debug.LogWarning(
                        "[Graduation Details] A snapshot pointer is corrupt; legacy migration " +
                        "was not allowed to replace it automatically.");
                    return;
                }

                // Historical folder/fallback buckets are scoped to a playthrough/chapter, not to
                // an individual vanilla filename. Treat them as the strongest available legacy
                // provenance, while still selecting one whole freshest bucket instead of merging
                // all matching roots.
                List<string> scopedKeys = GetStrongLegacyKeys();
                LegacyCandidate selected = FindFreshestValidatedCandidate(
                    sourceRoots,
                    scopedKeys,
                    targetDirectory);
                if (selected != null
                    && !HasExplicitScopedAssignment(
                        selected.Directory,
                        targetDirectory))
                {
                    Debug.LogWarning(
                        "[Graduation Details] Preserved playthrough-scoped legacy data at " +
                        selected.Directory + " because it has no explicit owner marker; no exact " +
                        "vanilla save can be proven from the historical key.");
                    selected = null;
                }

                bool targetHasData;
                bool targetValid = GraduationDetailsSnapshotStorage.ValidateLegacyDirectory(
                    targetDirectory,
                    out targetHasData);
                if (selected == null && targetValid && targetHasData)
                {
                    selected = new LegacyCandidate
                    {
                        Directory = targetDirectory,
                        DataFileCount = GetDataFileCount(targetDirectory),
                        FreshnessUtcTicks = GetDataFreshness(targetDirectory),
                        SourceKind = "canonical_legacy"
                    };
                }

                // Root-flat JSON was inherently unscoped. It is safe to recover only when its
                // historical marker explicitly names this canonical save directory. Sharing an
                // install root with a scoped bucket is not ownership evidence.
                string relatedRootFlat = "";
                string explicitlyAssignedRootFlat = FindExplicitlyAssignedRootFlat(
                    sourceRoots,
                    targetDirectory);
                if (selected != null)
                {
                    if (!string.IsNullOrEmpty(explicitlyAssignedRootFlat)
                        && !PathsReferToSameDirectory(
                            explicitlyAssignedRootFlat,
                            selected.Directory))
                    {
                        relatedRootFlat = explicitlyAssignedRootFlat;
                    }
                }
                else
                {
                    relatedRootFlat = explicitlyAssignedRootFlat;
                    if (!string.IsNullOrEmpty(relatedRootFlat))
                    {
                        selected = new LegacyCandidate
                        {
                            Directory = relatedRootFlat,
                            DataFileCount = GetDataFileCount(relatedRootFlat),
                            FreshnessUtcTicks = GetDataFreshness(relatedRootFlat),
                            SourceKind = "marked_root_flat"
                        };
                    }
                }

                if (selected == null)
                {
                    WarnAboutAmbiguousLegacyData(
                        sourceRoots,
                        vanillaSaveFilePath,
                        targetDirectory);
                    return;
                }

                if (GraduationDetailsSnapshotStorage.TryInstallMigratedSnapshot(
                    targetDirectory,
                    vanillaSaveFilePath,
                    selected.Directory,
                    relatedRootFlat,
                    selected.SourceKind))
                {
                    Debug.Log("[Graduation Details] Installed validated legacy sidecars from " +
                        selected.Directory);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Graduation Details] Legacy migration failed: " + exception.Message);
            }
        }

        private static List<string> GetStrongLegacyKeys()
        {
            List<string> keys = new List<string>();
            // Both forms shipped: story normally used SaveFolderName, while freeplay and some
            // transitional saves used the identity fallback even when a folder name existed.
            AddUnique(keys, GraduationDetailsPaths.GetLegacyFolderKey());
            AddUnique(keys, GraduationDetailsPaths.GetLegacyFallbackKey());
            return keys;
        }

        private static List<string> GetLegacySourceRoots()
        {
            List<string> roots = new List<string>();
            AddUnique(roots, GraduationDetailsPaths.RootDir);
            AddUnique(roots, Path.Combine(
                Application.persistentDataPath,
                "Mods",
                GraduationDetailsPaths.BaseFolder));
            AddUnique(roots, Path.Combine(
                Application.persistentDataPath,
                "Mods",
                GraduationDetailsPaths.DisplayFolder));

            try
            {
                AddUnique(roots, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            }
            catch
            {
            }

            try
            {
                // A locally deployed DLL may not be represented by the old subscribed Workshop
                // item in Mods._Mods. Derive its known installation from the active Steam library.
                DirectoryInfo dataDirectory = new DirectoryInfo(Application.dataPath);
                DirectoryInfo gameDirectory = dataDirectory.Parent;
                DirectoryInfo commonDirectory = gameDirectory != null ? gameDirectory.Parent : null;
                DirectoryInfo steamAppsDirectory = commonDirectory != null ? commonDirectory.Parent : null;
                if (commonDirectory != null
                    && steamAppsDirectory != null
                    && string.Equals(commonDirectory.Name, SteamCommonFolderName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(steamAppsDirectory.Name, SteamAppsFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    AddUnique(roots, Path.Combine(
                        steamAppsDirectory.FullName,
                        SteamWorkshopFolderName,
                        SteamWorkshopContentFolderName,
                        IdolManagerSteamAppId,
                        LegacyGraduationDetailsWorkshopId));
                }
            }
            catch
            {
                // Steam paths are optional on non-Steam builds and alternate deployments.
            }

            try
            {
                if (Mods._Mods != null)
                {
                    foreach (Mods._mod mod in Mods._Mods)
                    {
                        if (mod == null || string.IsNullOrEmpty(mod.Path))
                        {
                            continue;
                        }
                        bool matchingTitle = string.Equals(
                            mod.Title,
                            GraduationDetailsPaths.DisplayFolder,
                            StringComparison.OrdinalIgnoreCase);
                        bool matchingFolder = string.Equals(
                            mod.ModName,
                            GraduationDetailsPaths.BaseFolder,
                            StringComparison.OrdinalIgnoreCase)
                            || string.Equals(
                                mod.ModName,
                                GraduationDetailsPaths.DisplayFolder,
                                StringComparison.OrdinalIgnoreCase);
                        bool matchingAssembly = File.Exists(Path.Combine(mod.Path, AssemblyFileName));
                        if (matchingTitle || matchingFolder || matchingAssembly)
                        {
                            AddUnique(roots, mod.Path);
                        }
                    }
                }
            }
            catch
            {
                // Installed-mod discovery is best effort; explicit roots above remain available.
            }

            return roots;
        }

        private static LegacyCandidate FindFreshestValidatedCandidate(
            List<string> sourceRoots,
            List<string> scopedKeys,
            string targetDirectory)
        {
            List<LegacyCandidate> candidates = new List<LegacyCandidate>();
            foreach (string sourceRoot in sourceRoots)
            {
                foreach (string key in scopedKeys)
                {
                    ConsiderCandidate(
                        Path.Combine(sourceRoot, GraduationDetailsPaths.SavesFolder, key),
                        targetDirectory,
                        candidates);
                    ConsiderCandidate(
                        Path.Combine(sourceRoot, key),
                        targetDirectory,
                        candidates);
                }
            }
            if (candidates.Count == 0)
            {
                return null;
            }
            List<LegacyCandidate> explicitlyOwned = candidates
                .Where(candidate => candidate.ExplicitOwner)
                .ToList();
            return SelectBestCandidate(
                explicitlyOwned.Count > 0 ? explicitlyOwned : candidates,
                "Equally ranked legacy buckets disagree; all were preserved and none was " +
                    "assigned automatically.");
        }

        private static void ConsiderCandidate(
            string directory,
            string targetDirectory,
            List<LegacyCandidate> candidates)
        {
            if (PathsReferToSameDirectory(directory, targetDirectory))
            {
                return;
            }
            bool hasData;
            if (!GraduationDetailsSnapshotStorage.ValidateLegacyDirectory(directory, out hasData)
                || !hasData)
            {
                return;
            }
            long freshness = GetDataFreshness(directory);
            string identity = GetContentIdentity(directory);
            if (string.IsNullOrEmpty(identity))
            {
                return;
            }
            candidates.Add(new LegacyCandidate
            {
                Directory = directory,
                ExplicitOwner = HasExplicitScopedAssignment(directory, targetDirectory),
                DataFileCount = GetDataFileCount(directory),
                FreshnessUtcTicks = freshness,
                ContentIdentity = identity,
                SourceKind = "scoped_legacy"
            });
        }

        private static string FindExplicitlyAssignedRootFlat(
            List<string> sourceRoots,
            string targetDirectory)
        {
            List<LegacyCandidate> candidates = new List<LegacyCandidate>();
            foreach (string sourceRoot in sourceRoots)
            {
                string markerPath;
                if (!GraduationDetailsPaths.TryGetSafeLeafPath(
                        sourceRoot,
                        RootFlatMigrationMarker,
                        out markerPath)
                    || !File.Exists(markerPath))
                {
                    continue;
                }
                string assigned;
                try
                {
                    assigned = File.ReadAllText(markerPath).Trim();
                }
                catch
                {
                    continue;
                }
                if (!PathsReferToSameDirectory(assigned, targetDirectory))
                {
                    continue;
                }
                bool hasData;
                if (!GraduationDetailsSnapshotStorage.ValidateLegacyDirectory(sourceRoot, out hasData)
                    || !hasData)
                {
                    continue;
                }
                long freshness = GetDataFreshness(sourceRoot);
                string identity = GetContentIdentity(sourceRoot);
                if (string.IsNullOrEmpty(identity))
                {
                    continue;
                }
                candidates.Add(new LegacyCandidate
                {
                    Directory = sourceRoot,
                    DataFileCount = GetDataFileCount(sourceRoot),
                    FreshnessUtcTicks = freshness,
                    ContentIdentity = identity
                });
            }
            if (candidates.Count == 0)
            {
                return "";
            }
            LegacyCandidate selected = SelectBestCandidate(
                candidates,
                "Equally ranked marked root-flat sources disagree; neither was assigned " +
                    "automatically.");
            return selected != null ? selected.Directory : "";
        }

        private static LegacyCandidate SelectBestCandidate(
            List<LegacyCandidate> candidates,
            string ambiguityMessage)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }
            // Missing stores are semantically empty, but a partial bundle must never outrank an
            // older complete bundle merely because one file was touched recently.
            int bestCompleteness = candidates.Max(candidate => candidate.DataFileCount);
            List<LegacyCandidate> completeCandidates = candidates
                .Where(candidate => candidate.DataFileCount == bestCompleteness)
                .ToList();
            long freshest = completeCandidates.Max(candidate => candidate.FreshnessUtcTicks);
            List<LegacyCandidate> finalists = completeCandidates
                .Where(candidate => candidate.FreshnessUtcTicks == freshest)
                .ToList();
            if (finalists
                .Select(candidate => candidate.ContentIdentity)
                .Distinct(StringComparer.Ordinal)
                .Count() > 1)
            {
                Debug.LogWarning("[Graduation Details] " + ambiguityMessage);
                return null;
            }
            return finalists
                .OrderBy(candidate => candidate.Directory, StringComparer.OrdinalIgnoreCase)
                .First();
        }

        private static int GetDataFileCount(string directory)
        {
            string marriagePath;
            string staffPath;
            string snapshotsPath;
            if (!GraduationDetailsSnapshotStorage.TryGetDataPaths(
                directory,
                out marriagePath,
                out staffPath,
                out snapshotsPath))
            {
                return 0;
            }
            int count = 0;
            if (File.Exists(marriagePath))
            {
                count++;
            }
            if (File.Exists(staffPath))
            {
                count++;
            }
            if (File.Exists(snapshotsPath))
            {
                count++;
            }
            return count;
        }

        private static bool HasExplicitScopedAssignment(
            string scopedDirectory,
            string targetDirectory)
        {
            try
            {
                string markerPath;
                if (!GraduationDetailsPersistenceIO.TryGetSafeLeafPath(
                        scopedDirectory,
                        ScopedMigrationOwnerMarker,
                        false,
                        out markerPath)
                    || !File.Exists(markerPath))
                {
                    return false;
                }
                return PathsReferToSameDirectory(
                    File.ReadAllText(markerPath).Trim(),
                    targetDirectory);
            }
            catch
            {
                return false;
            }
        }

        private static void WarnAboutAmbiguousLegacyData(
            List<string> sourceRoots,
            string vanillaSaveFilePath,
            string targetDirectory)
        {
            List<string> ambiguousKeys = new List<string>();
            string ownerKey = GraduationDetailsPaths.GetLegacyOwnerKey(vanillaSaveFilePath);
            AddUnique(ambiguousKeys, ownerKey);
            AddUnique(ambiguousKeys, GraduationDetailsPaths.SanitizeLegacyFileToken(ownerKey));
            // This historical folder is deliberately recognized, but it cannot identify which
            // current save owns its contents and is therefore never imported automatically.
            AddUnique(ambiguousKeys, "default");
            foreach (string sourceRoot in sourceRoots)
            {
                bool rootHasData;
                if (!PathsReferToSameDirectory(sourceRoot, targetDirectory)
                    && GraduationDetailsSnapshotStorage.ValidateLegacyDirectory(
                        sourceRoot,
                        out rootHasData)
                    && rootHasData)
                {
                    Debug.LogWarning(
                        "[Graduation Details] Preserved ambiguous root-flat legacy data at " +
                        sourceRoot + "; it was not assigned to this save.");
                }
                foreach (string key in ambiguousKeys)
                {
                    WarnIfValidAmbiguous(Path.Combine(
                        sourceRoot,
                        GraduationDetailsPaths.SavesFolder,
                        key));
                    WarnIfValidAmbiguous(Path.Combine(sourceRoot, key));
                }
            }
        }

        private static void WarnIfValidAmbiguous(string directory)
        {
            bool hasData;
            if (GraduationDetailsSnapshotStorage.ValidateLegacyDirectory(directory, out hasData)
                && hasData)
            {
                Debug.LogWarning(
                    "[Graduation Details] Preserved ambiguous legacy data at " + directory +
                    "; it was not silently bound to the current save.");
            }
        }

        private static long GetDataFreshness(string directory)
        {
            string marriagePath;
            string staffPath;
            string snapshotsPath;
            if (!GraduationDetailsSnapshotStorage.TryGetDataPaths(
                directory,
                out marriagePath,
                out staffPath,
                out snapshotsPath))
            {
                return 0L;
            }
            long latest = 0L;
            foreach (string path in new[] { marriagePath, staffPath, snapshotsPath })
            {
                if (File.Exists(path))
                {
                    latest = Math.Max(latest, File.GetLastWriteTimeUtc(path).Ticks);
                }
            }
            return latest;
        }

        private static string GetContentIdentity(string directory)
        {
            try
            {
                List<string> identity = new List<string>();
                string marriagePath;
                string staffPath;
                string snapshotsPath;
                if (!GraduationDetailsSnapshotStorage.TryGetDataPaths(
                    directory,
                    out marriagePath,
                    out staffPath,
                    out snapshotsPath))
                {
                    return "";
                }
                foreach (string path in new[] { marriagePath, staffPath, snapshotsPath })
                {
                    GraduationDetailsPersistenceIO.FileFingerprint fingerprint;
                    if (!File.Exists(path))
                    {
                        identity.Add(Path.GetFileName(path) + ":missing");
                    }
                    else if (!GraduationDetailsPersistenceIO.TryCaptureFingerprint(
                        path,
                        out fingerprint))
                    {
                        return "";
                    }
                    else
                    {
                        identity.Add(Path.GetFileName(path) + ":" + fingerprint.Sha256);
                    }
                }
                string portraits;
                if (GraduationDetailsPersistenceIO.TryGetContainedPath(
                        directory,
                        GraduationDetailsPaths.PortraitsFolder,
                        false,
                        out portraits)
                    && Directory.Exists(portraits))
                {
                    foreach (string portrait in Directory.GetFiles(
                        portraits,
                        "*",
                        SearchOption.TopDirectoryOnly)
                        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                    {
                        string fileName = Path.GetFileName(portrait);
                        string safePath;
                        GraduationDetailsPersistenceIO.FileFingerprint fingerprint;
                        if (!GraduationDetailsPaths.IsSafePortraitFileName(fileName)
                            || !GraduationDetailsPersistenceIO.TryGetSafeLeafPath(
                                portraits,
                                fileName,
                                false,
                                out safePath)
                            || !GraduationDetailsPersistenceIO.TryCaptureFingerprint(
                                safePath,
                                out fingerprint))
                        {
                            return "";
                        }
                        identity.Add(fileName + ":" + fingerprint.Sha256);
                    }
                }
                return GraduationDetailsPersistenceIO.ComputeSha256(
                    Encoding.UTF8.GetBytes(string.Join("|", identity.ToArray())));
            }
            catch
            {
                return "";
            }
        }

        private static bool HasSnapshotPointer(string directory)
        {
            string pointer;
            string backup;
            return GraduationDetailsPaths.TryGetSafeLeafPath(
                    directory,
                    GraduationDetailsPaths.ActiveSnapshotFile,
                    out pointer)
                && GraduationDetailsPaths.TryGetSafeLeafPath(
                    directory,
                    GraduationDetailsPaths.ActiveSnapshotBackupFile,
                    out backup)
                && (File.Exists(pointer) || File.Exists(backup));
        }

        private static bool PathsReferToSameDirectory(string firstPath, string secondPath)
        {
            try
            {
                string first = Path.GetFullPath(firstPath).TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
                string second = Path.GetFullPath(secondPath).TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
                return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return true;
            }
        }

        private static void AddUnique(List<string> values, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }
            foreach (string existing in values)
            {
                if (string.Equals(existing, value, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            values.Add(value);
        }

        private sealed class LegacyCandidate
        {
            internal string Directory = "";
            internal bool ExplicitOwner;
            internal int DataFileCount;
            internal long FreshnessUtcTicks;
            internal string ContentIdentity = "";
            internal string SourceKind = "legacy_migration";
        }
    }

    internal static class GraduationDetailsPersistence
    {
        private const float SaveCompletionTimeoutSeconds = 30f;
        private const float SaveCompletionStableSeconds = 0.5f;
        private const float SavePollIntervalSeconds = 0.1f;

        private static bool loadInProgress;
        private static bool loadPathCaptured;
        private static bool awaitingLatestAutosavePath;
        private static string stagedVanillaSaveFilePath = "";
        private static string stagedSaveDirectory = "";
        private static float loadPauseStarted = -1f;
        private static int runtimeEpoch;
        private static long saveSequence;
        private static int latestReboundEpoch = -1;
        private static long latestReboundSequence = -1L;
        private static readonly Dictionary<string, GraduationDetailsPreparedSave> PendingByToken =
            new Dictionary<string, GraduationDetailsPreparedSave>(StringComparer.Ordinal);
        private static readonly Dictionary<string, List<GraduationDetailsPreparedSave>> PendingByTarget =
            new Dictionary<string, List<GraduationDetailsPreparedSave>>(
                StringComparer.OrdinalIgnoreCase);

        internal static bool LoadInProgress
        {
            get
            {
                return loadInProgress;
            }
        }

        internal static void BeginLoad(bool expectLatestAutosavePath = false)
        {
            if (!loadInProgress)
            {
                loadPauseStarted = Time.realtimeSinceStartup;
            }
            loadInProgress = true;
            loadPathCaptured = false;
            awaitingLatestAutosavePath = expectLatestAutosavePath;
            stagedVanillaSaveFilePath = "";
            stagedSaveDirectory = "";
        }

        internal static void CaptureLoadPath(string vanillaSaveFilePath, bool replaceCapturedPath = false)
        {
            if (!loadInProgress || (loadPathCaptured && !replaceCapturedPath))
            {
                return;
            }
            loadPathCaptured = true;
            stagedVanillaSaveFilePath = "";
            stagedSaveDirectory = "";

            string normalizedPath;
            string saveDirectory;
            if (GraduationDetailsPaths.TryResolveSaveDirectory(
                vanillaSaveFilePath,
                out normalizedPath,
                out saveDirectory))
            {
                stagedVanillaSaveFilePath = normalizedPath;
                stagedSaveDirectory = saveDirectory;
            }
        }

        internal static void CaptureLatestAutosavePath(string vanillaSaveFilePath)
        {
            if (!loadInProgress || !awaitingLatestAutosavePath)
            {
                return;
            }
            // Other save mods may resolve the selector in their own LoadData prefix. Keep the
            // override armed so vanilla's later selector result remains authoritative.
            CaptureLoadPath(vanillaSaveFilePath, true);
        }

        internal static void CompleteLoad(bool loadSucceeded)
        {
            bool capturedPath = loadPathCaptured;
            string vanillaSaveFilePath = stagedVanillaSaveFilePath;
            string saveDirectory = stagedSaveDirectory;
            ResumePendingObservers();
            loadInProgress = false;
            loadPathCaptured = false;
            awaitingLatestAutosavePath = false;
            stagedVanillaSaveFilePath = "";
            stagedSaveDirectory = "";

            if (!loadSucceeded || !capturedPath)
            {
                // The active scope and cached records were never changed while the load was
                // staged, so cancelling naturally restores the pre-load Graduation Details state.
                return;
            }

            // Pending saves from the prior scope may still finish and publish to their own
            // destinations, but must never rebind over the successfully loaded game.
            runtimeEpoch++;

            GraduationDetailsPaths.BeginFreshWorkingPortraitScope();
            if (!string.IsNullOrEmpty(saveDirectory))
            {
                GraduationDetailsSnapshotStorage.TryRecoverForVanillaSave(
                    saveDirectory,
                    vanillaSaveFilePath);
                GraduationDetailsLegacyMigration.TryMigrateForScope(
                    saveDirectory,
                    vanillaSaveFilePath);
                GraduationDetailsPaths.Bind(vanillaSaveFilePath, saveDirectory);
            }
            else
            {
                // A successful load outside the canonical game data directory must not inherit
                // records from the previously loaded save.
                GraduationDetailsPaths.ClearBinding();
            }

            MarriageRecordStore.ResetForScopeChange();
            StaffIdolStore.ResetForScopeChange();
            GraduationSnapshotStore.ResetForScopeChange();

            // The normal LoadFunction postfixes are suppressed while SaveManager dispatches its
            // LoadEvent. Run their backfills only after the exact mod scope has been loaded.
            GraduationSnapshotStore.Backfill(data_girls.girl);
            StaffIdolStore.BackfillFromStaff();
        }

        internal static void CancelLoad()
        {
            ResumePendingObservers();
            loadInProgress = false;
            loadPathCaptured = false;
            awaitingLatestAutosavePath = false;
            stagedVanillaSaveFilePath = "";
            stagedSaveDirectory = "";
        }

        internal static void ResetForNewGame()
        {
            CancelLoad();
            // Let verified old-game saves finish publishing, but make their epochs ineligible to
            // rebind over the new game.
            runtimeEpoch++;
            GraduationDetailsPaths.ClearBinding();
            GraduationDetailsPaths.BeginFreshWorkingPortraitScope();
            MarriageRecordStore.ResetForScopeChange();
            StaffIdolStore.ResetForScopeChange();
            GraduationSnapshotStore.ResetForScopeChange();
        }

        internal static string PrepareVanillaSave(
            SaveManager.SavedData data,
            string dataFileName,
            bool isJson,
            bool fullPath)
        {
            if (loadInProgress)
            {
                return "";
            }

            string outputPath = GraduationDetailsPaths.ResolveDataSaverOutputPath(
                dataFileName,
                fullPath);
            string normalizedPath;
            string saveDirectory;
            if (!GraduationDetailsPaths.TryResolveSaveDirectory(
                outputPath,
                out normalizedPath,
                out saveDirectory))
            {
                Debug.LogWarning("[Graduation Details] Ignored non-canonical save path: " + outputPath);
                return "";
            }

            try
            {
                MarriageRecordStore.EnsureReady();
                StaffIdolStore.EnsureReady();
                GraduationSnapshotStore.EnsureReady();
                string serialized = isJson
                    ? JsonUtility.ToJson(data, true)
                    : (data != null ? data.ToString() : "");
                byte[] expectedPayload = Encoding.UTF8.GetBytes(serialized ?? "");
                GraduationDetailsPersistenceIO.FileFingerprint preWriteFingerprint;
                if (!GraduationDetailsPersistenceIO.TryCaptureFingerprint(
                    normalizedPath,
                    out preWriteFingerprint))
                {
                    return "";
                }

                long sequence = ++saveSequence;
                GraduationDetailsPreparedSave prepared;
                if (!GraduationDetailsSnapshotStorage.TryStageLiveSnapshot(
                    normalizedPath,
                    saveDirectory,
                    expectedPayload,
                    preWriteFingerprint,
                    runtimeEpoch,
                    sequence,
                    out prepared))
                {
                    return "";
                }
                PendingByToken[prepared.TransactionId] = prepared;
                List<GraduationDetailsPreparedSave> targetPending;
                if (!PendingByTarget.TryGetValue(normalizedPath, out targetPending))
                {
                    targetPending = new List<GraduationDetailsPreparedSave>();
                    PendingByTarget[normalizedPath] = targetPending;
                }
                targetPending.Add(prepared);
                return prepared.TransactionId;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Graduation Details] Failed to prepare sidecar snapshot: " +
                    exception.Message);
                return "";
            }
        }

        internal static void OnVanillaSaveScheduled(string transactionId)
        {
            GraduationDetailsPreparedSave prepared;
            if (string.IsNullOrEmpty(transactionId)
                || !PendingByToken.TryGetValue(transactionId, out prepared))
            {
                return;
            }
            prepared.Deadline = Time.realtimeSinceStartup + SaveCompletionTimeoutSeconds;
            if (!GraduationDetailsPersistenceRunner.TryObserve(transactionId))
            {
                // The manifest was flushed before DataSaver was scheduled. With no guaranteed
                // main-thread pump, leave it durable for exact-payload recovery on the next load.
                DetachPrepared(prepared);
            }
        }

        internal static bool TickPendingSave(string transactionId)
        {
            GraduationDetailsPreparedSave requested;
            if (!PendingByToken.TryGetValue(transactionId, out requested))
            {
                return false;
            }
            if (loadInProgress)
            {
                return true;
            }
            float now = Time.realtimeSinceStartup;
            if (now < requested.NextPoll)
            {
                return true;
            }
            requested.NextPoll = now + SavePollIntervalSeconds;

            List<GraduationDetailsPreparedSave> targetPending;
            if (!PendingByTarget.TryGetValue(
                requested.NormalizedVanillaSaveFilePath,
                out targetPending))
            {
                return false;
            }
            GraduationDetailsPersistenceIO.FileFingerprint current;
            if (!GraduationDetailsPersistenceIO.TryCaptureFingerprint(
                requested.NormalizedVanillaSaveFilePath,
                out current))
            {
                return true;
            }
            foreach (GraduationDetailsPreparedSave candidate in targetPending)
            {
                if (FingerprintMatches(candidate, current)
                    && !candidate.PreWriteFingerprint.SameFileState(current))
                {
                    candidate.WriteObserved = true;
                }
                if (!candidate.WriteObserved && now >= candidate.Deadline)
                {
                    candidate.Expired = true;
                }
                if (!FingerprintMatches(candidate, current))
                {
                    candidate.MatchingSince = -1f;
                }
            }
            if (targetPending.Any(candidate => !candidate.WriteObserved && !candidate.Expired))
            {
                return true;
            }

            // DataSaver writes on independent threads. Intermediate writes can complete between
            // polls or finish out of scheduling order. Publish only the newest transition-observed,
            // fully validated generation whose payload equals the stable final file. An expired
            // identical-payload schedule is not evidence that its writer ever ran.
            GraduationDetailsPreparedSave latest = targetPending
                .Where(candidate => candidate.WriteObserved
                    && FingerprintMatches(candidate, current)
                    && GraduationDetailsSnapshotStorage
                        .IsPreparedSnapshotValidForCurrentPayload(candidate, current))
                .OrderByDescending(candidate => candidate.Sequence)
                .FirstOrDefault();
            if (latest == null)
            {
                DiscardTarget(targetPending);
                return false;
            }
            if (latest.MatchingSince < 0f)
            {
                latest.MatchingSince = now;
                return true;
            }
            if (now - latest.MatchingSince < SaveCompletionStableSeconds)
            {
                return true;
            }

            string committedDirectory;
            bool published = GraduationDetailsSnapshotStorage.TryPublishPreparedSnapshot(
                latest,
                out committedDirectory);
            foreach (GraduationDetailsPreparedSave candidate in targetPending.ToArray())
            {
                PendingByToken.Remove(candidate.TransactionId);
                if (!ReferenceEquals(candidate, latest))
                {
                    GraduationDetailsSnapshotStorage.DiscardPreparedSnapshot(candidate);
                }
            }
            PendingByTarget.Remove(latest.NormalizedVanillaSaveFilePath);
            if (!published)
            {
                // Keep the newest durable stage/final generation for load-time recovery.
                return false;
            }

            if (!loadInProgress
                && latest.RuntimeEpoch == runtimeEpoch
                && (latestReboundEpoch != runtimeEpoch
                    || latest.Sequence > latestReboundSequence))
            {
                GraduationDetailsPaths.Bind(
                    latest.NormalizedVanillaSaveFilePath,
                    latest.SaveDirectory);
                MarriageRecordStore.RebindLoadedScope();
                StaffIdolStore.RebindLoadedScope();
                GraduationSnapshotStore.RebindLoadedScope();
                latestReboundEpoch = runtimeEpoch;
                latestReboundSequence = latest.Sequence;
            }
            return false;
        }

        private static bool FingerprintMatches(
            GraduationDetailsPreparedSave prepared,
            GraduationDetailsPersistenceIO.FileFingerprint fingerprint)
        {
            return prepared != null
                && fingerprint != null
                && fingerprint.Exists
                && fingerprint.Length == prepared.ExpectedVanillaLength
                && string.Equals(
                    fingerprint.Sha256,
                    prepared.ExpectedVanillaSha256,
                    StringComparison.Ordinal);
        }

        private static void DiscardTarget(List<GraduationDetailsPreparedSave> targetPending)
        {
            if (targetPending == null || targetPending.Count == 0)
            {
                return;
            }
            string target = targetPending[0].NormalizedVanillaSaveFilePath;
            foreach (GraduationDetailsPreparedSave candidate in targetPending.ToArray())
            {
                PendingByToken.Remove(candidate.TransactionId);
                GraduationDetailsSnapshotStorage.DiscardPreparedSnapshot(candidate);
            }
            PendingByTarget.Remove(target);
        }

        private static void DetachPrepared(GraduationDetailsPreparedSave prepared)
        {
            PendingByToken.Remove(prepared.TransactionId);
            List<GraduationDetailsPreparedSave> targetPending;
            if (PendingByTarget.TryGetValue(
                prepared.NormalizedVanillaSaveFilePath,
                out targetPending))
            {
                targetPending.Remove(prepared);
                if (targetPending.Count == 0)
                {
                    PendingByTarget.Remove(prepared.NormalizedVanillaSaveFilePath);
                }
            }
        }

        private static void ResumePendingObservers()
        {
            if (loadPauseStarted < 0f)
            {
                return;
            }
            float pauseDuration = Math.Max(
                0f,
                Time.realtimeSinceStartup - loadPauseStarted);
            foreach (GraduationDetailsPreparedSave prepared in PendingByToken.Values)
            {
                if (prepared.Deadline > 0f)
                {
                    prepared.Deadline += pauseDuration;
                }
            }
            loadPauseStarted = -1f;
        }
    }

    internal sealed class GraduationDetailsPersistenceRunner : MonoBehaviour
    {
        private static GraduationDetailsPersistenceRunner instance;

        internal static void Ensure(GameObject host)
        {
            if (instance != null || host == null)
            {
                return;
            }
            instance = host.GetComponent<GraduationDetailsPersistenceRunner>();
            if (instance == null)
            {
                instance = host.AddComponent<GraduationDetailsPersistenceRunner>();
            }
        }

        internal static bool TryObserve(string transactionId)
        {
            if (instance == null || string.IsNullOrEmpty(transactionId))
            {
                return false;
            }
            instance.StartCoroutine(instance.Observe(transactionId));
            return true;
        }

        private IEnumerator Observe(string transactionId)
        {
            while (GraduationDetailsPersistence.TickPendingSave(transactionId))
            {
                yield return null;
            }
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(instance, this))
            {
                instance = null;
            }
        }
    }

    internal static class MarriageRecordStore
    {
        private static readonly Dictionary<int, MarriageRecord> Records = new Dictionary<int, MarriageRecord>();
        private static bool loaded;
        private static bool loadHealthy = true;
        private static string loadedScope = "";

        internal static bool CanSnapshot
        {
            get
            {
                EnsureLoaded();
                return loadHealthy
                    && (!GraduationDetailsPaths.HasActiveSaveScope
                        || GraduationDetailsPaths.CanWriteActiveScope);
            }
        }

        private static string DataPath
        {
            get
            {
                return GraduationDetailsPaths.GetScopedFilePath(
                    GraduationDetailsPaths.MarriageFile);
            }
        }

        internal static MarriageRecord GetRecord(int girlId)
        {
            EnsureLoaded();
            MarriageRecord record;
            if (Records.TryGetValue(girlId, out record))
            {
                return record;
            }
            return null;
        }

        internal static void Upsert(MarriageRecord record)
        {
            if (record == null || record.GirlId < 0)
            {
                return;
            }
            EnsureLoaded();
            Records[record.GirlId] = record;
            Save();
        }

        internal static void EnsureReady()
        {
            EnsureLoaded();
        }

        internal static void ResetForScopeChange()
        {
            loaded = false;
            loadHealthy = true;
            loadedScope = "";
            Records.Clear();
        }

        internal static void RebindLoadedScope()
        {
            loaded = true;
            loadHealthy = true;
            loadedScope = GraduationDetailsPaths.GetScopeId();
        }

        internal static void SaveToDirectory(string directory)
        {
            string path;
            string validatedDirectory;
            if (string.IsNullOrEmpty(directory)
                || !GraduationDetailsPaths.TryValidateOwnedDataDirectory(
                    directory,
                    out validatedDirectory)
                || !GraduationDetailsPersistenceIO.TryGetSafeLeafPath(
                    validatedDirectory,
                    GraduationDetailsPaths.MarriageFile,
                    true,
                    out path))
            {
                throw new InvalidDataException("Unsafe marriage snapshot path.");
            }

            Directory.CreateDirectory(validatedDirectory);
            MarriageRecordList list = new MarriageRecordList();
            list.Records = Records.Values.ToList();
            string json = JsonUtility.ToJson(list, true);
            GraduationDetailsPersistenceIO.WriteUtf8Durable(path, json);
        }

        internal static string GetEmptyJson()
        {
            return JsonUtility.ToJson(new MarriageRecordList(), true);
        }

        internal static bool TryValidateFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }
                if (!GraduationDetailsPersistenceIO.HasExplicitRootArrayProperty(
                    json,
                    "Records"))
                {
                    return false;
                }
                MarriageRecordList list = JsonUtility.FromJson<MarriageRecordList>(json);
                if (list == null || list.Records == null)
                {
                    return false;
                }
                HashSet<int> ids = new HashSet<int>();
                foreach (MarriageRecord record in list.Records)
                {
                    if (record == null
                        || record.GirlId < 0
                        || !ids.Add(record.GirlId)
                        || !Enum.IsDefined(typeof(CustodyOwner), record.Custody))
                    {
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureLoaded()
        {
            string scope = GraduationDetailsPaths.GetScopeId();
            if (loaded && loadedScope == scope)
            {
                return;
            }
            loadedScope = scope;
            loaded = true;
            loadHealthy = true;
            try
            {
                if (!GraduationDetailsPaths.HasActiveSaveScope)
                {
                    Records.Clear();
                    return;
                }
                if (!GraduationDetailsPaths.CanWriteActiveScope
                    || string.IsNullOrEmpty(DataPath))
                {
                    loadHealthy = false;
                    return;
                }
                if (!File.Exists(DataPath))
                {
                    Records.Clear();
                    return;
                }
                string loadPath = DataPath;
                string json = File.ReadAllText(loadPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    loadHealthy = false;
                    return;
                }
                if (!GraduationDetailsPersistenceIO.HasExplicitRootArrayProperty(
                    json,
                    "Records"))
                {
                    loadHealthy = false;
                    return;
                }
                MarriageRecordList list = JsonUtility.FromJson<MarriageRecordList>(json);
                if (list == null || list.Records == null)
                {
                    loadHealthy = false;
                    return;
                }
                Dictionary<int, MarriageRecord> parsed = new Dictionary<int, MarriageRecord>();
                foreach (MarriageRecord record in list.Records)
                {
                    if (record == null
                        || record.GirlId < 0
                        || parsed.ContainsKey(record.GirlId)
                        || !Enum.IsDefined(typeof(CustodyOwner), record.Custody))
                    {
                        loadHealthy = false;
                        return;
                    }
                    parsed[record.GirlId] = record;
                }
                Records.Clear();
                foreach (KeyValuePair<int, MarriageRecord> entry in parsed)
                {
                    Records[entry.Key] = entry.Value;
                }
            }
            catch (Exception exception)
            {
                loadHealthy = false;
                Debug.LogWarning("[Graduation Details] Marriage sidecar is unreadable and was " +
                    "left untouched: " + exception.Message);
            }
        }

        private static void Save()
        {
            // Keep changes in memory until vanilla schedules a matching game-save write.
            // The DataSaver patch snapshots every store together into that exact save scope.
        }
    }

    internal static class MarriageContext
    {
        internal static bool Active;
        internal static int GirlId = -1;
        internal static string PlayerName = "";
        internal static int KidsCount = -1;
        internal static CustodyOwner Custody = CustodyOwner.Unknown;

        internal static void Begin(data_girls.girls girl)
        {
            Reset();
            if (girl == null)
            {
                return;
            }
            Active = true;
            GirlId = girl.id;
            PlayerName = staticVars.PlayerData.GetPlayerName(staticVars._playerData.name_type.full_name, true);
        }

        internal static void SetKids(int count)
        {
            if (!Active)
            {
                return;
            }
            KidsCount = count;
            if (count <= 0)
            {
                Custody = CustodyOwner.None;
            }
        }

        internal static void SetCustody(string custodyString, bool goodOutcome, int numberOfKids)
        {
            if (!Active)
            {
                return;
            }
            if (KidsCount < 0)
            {
                KidsCount = numberOfKids;
            }
            if (goodOutcome || numberOfKids <= 0)
            {
                if (Custody == CustodyOwner.Unknown)
                {
                    Custody = CustodyOwner.None;
                }
                return;
            }
            if (string.IsNullOrEmpty(custodyString))
            {
                return;
            }
            if (custodyString.Contains("[g:casual]"))
            {
                Custody = CustodyOwner.Idol;
                return;
            }
            string youText = Language.Data["NOTIF__IDOL_REL_YOU"];
            if (!string.IsNullOrEmpty(youText) && custodyString.Contains(youText))
            {
                Custody = CustodyOwner.Player;
            }
        }

        internal static void SaveAndClear()
        {
            if (!Active || GirlId < 0)
            {
                Reset();
                return;
            }
            MarriageRecord record = new MarriageRecord
            {
                GirlId = GirlId,
                MarriedToPlayer = true,
                PlayerName = PlayerName ?? "",
                KidsCount = KidsCount,
                Custody = Custody
            };
            MarriageRecordStore.Upsert(record);
            Reset();
        }

        private static void Reset()
        {
            Active = false;
            GirlId = -1;
            PlayerName = "";
            KidsCount = -1;
            Custody = CustodyOwner.Unknown;
        }
    }

    internal static class GraduationIdentity
    {
        internal static string TextureSignature(data_girls.girls girl)
        {
            return girl != null ? TextureSignature(girl.textureAssets) : "";
        }

        internal static string TextureSignature(staff._staff staffer)
        {
            return staffer != null ? TextureSignature(staffer.textureAssets) : "";
        }

        internal static string TextureSignature(List<data_girls.girls._textureAsset> assets)
        {
            if (assets == null || assets.Count == 0)
            {
                return "";
            }
            List<string> parts = new List<string>();
            foreach (data_girls.girls._textureAsset asset in assets)
            {
                if (asset == null || asset.asset == null)
                {
                    continue;
                }
                string id = asset.asset.GetID();
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }
                parts.Add(asset.type + "=" + id);
            }
            if (parts.Count == 0)
            {
                return "";
            }
            parts.Sort(StringComparer.Ordinal);
            return string.Join("|", parts.ToArray());
        }

        internal static bool TextureAssetsMatch(List<data_girls.girls._textureAsset> first, List<data_girls.girls._textureAsset> second)
        {
            string firstSignature = TextureSignature(first);
            string secondSignature = TextureSignature(second);
            return !string.IsNullOrEmpty(firstSignature)
                && !string.IsNullOrEmpty(secondSignature)
                && string.Equals(firstSignature, secondSignature, StringComparison.Ordinal);
        }

        internal static bool NamesMatch(staff._staff staffer, data_girls.girls girl)
        {
            if (staffer == null || girl == null)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(staffer.firstName) && !string.IsNullOrEmpty(staffer.lastName)
                && !string.IsNullOrEmpty(girl.firstName) && !string.IsNullOrEmpty(girl.lastName))
            {
                if (string.Equals(staffer.firstName, girl.firstName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(staffer.lastName, girl.lastName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            if (!string.IsNullOrEmpty(staffer.nickname) && !string.IsNullOrEmpty(girl.nickname))
            {
                if (string.Equals(staffer.nickname, girl.nickname, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        internal static string ShortHash(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                value = "empty";
            }
            unchecked
            {
                uint hash = 2166136261U;
                foreach (char c in value)
                {
                    hash ^= char.ToUpperInvariant(c);
                    hash *= 16777619U;
                }
                return hash.ToString("x8");
            }
        }

        internal static bool SameText(string left, string right)
        {
            return string.Equals(left ?? "", right ?? "", StringComparison.OrdinalIgnoreCase);
        }
    }

    [Serializable]
    internal sealed class StaffIdolRecord
    {
        public int StaffId = -1;
        public int GirlId = -1;
        public bool CapturedAtHire;
        public string FirstName = "";
        public string LastName = "";
        public string Nickname = "";
        public string TextureSignature = "";
    }

    [Serializable]
    internal sealed class StaffIdolRecordList
    {
        public List<StaffIdolRecord> Records = new List<StaffIdolRecord>();
    }

    internal static class StaffIdolStore
    {
        private static readonly Dictionary<int, StaffIdolRecord> StaffToGirl = new Dictionary<int, StaffIdolRecord>();
        private static bool loaded;
        private static bool loadHealthy = true;
        private static string loadedScope = "";

        internal static bool CanSnapshot
        {
            get
            {
                EnsureLoaded();
                return loadHealthy
                    && (!GraduationDetailsPaths.HasActiveSaveScope
                        || GraduationDetailsPaths.CanWriteActiveScope);
            }
        }

        private static string DataPath
        {
            get
            {
                return GraduationDetailsPaths.GetScopedFilePath(
                    GraduationDetailsPaths.StaffMapFile);
            }
        }

        internal static bool TryGetRecord(int staffId, out StaffIdolRecord record)
        {
            EnsureLoaded();
            return StaffToGirl.TryGetValue(staffId, out record);
        }

        internal static bool TryGetGirlId(int staffId, out int girlId)
        {
            StaffIdolRecord record;
            if (TryGetRecord(staffId, out record))
            {
                girlId = record.GirlId;
                return true;
            }
            girlId = -1;
            return false;
        }

        internal static bool IsFormerIdolStaff(staff._staff staffer)
        {
            if (staffer == null || !staffer.IsIdol())
            {
                return false;
            }
            return staffer.UniqueType == staff._staff._unique_type.NONE;
        }

        internal static bool TryResolveStaffer(staff._staff staffer, out int girlId)
        {
            girlId = -1;
            if (staffer == null)
            {
                return false;
            }
            StaffIdolRecord record;
            if (TryGetRecord(staffer.id, out record))
            {
                girlId = record.GirlId;
                data_girls.girls mapped = data_girls.GetGirlByID(girlId);
                if (mapped != null && RecordMatchesStaffer(staffer, record) && IsLikelyMatch(staffer, mapped))
                {
                    if (!RecordHasIdentity(record))
                    {
                        Upsert(staffer, girlId, record.CapturedAtHire);
                    }
                    return true;
                }
                if (data_girls.girl == null)
                {
                    girlId = -1;
                    return false;
                }
                StaffToGirl.Remove(staffer.id);
                Save();
                girlId = -1;
            }
            if (!IsFormerIdolStaff(staffer))
            {
                return false;
            }
            girlId = TryResolveGirlId(staffer);
            if (girlId < 0)
            {
                return false;
            }
            Upsert(staffer, girlId, false);
            return true;
        }

        internal static void Upsert(int staffId, int girlId)
        {
            Upsert(null, staffId, girlId, false);
        }

        internal static void Upsert(int staffId, int girlId, bool capturedAtHire)
        {
            Upsert(null, staffId, girlId, capturedAtHire);
        }

        internal static void Upsert(staff._staff staffer, int girlId, bool capturedAtHire)
        {
            if (staffer == null)
            {
                return;
            }
            Upsert(staffer, staffer.id, girlId, capturedAtHire);
        }

        private static void Upsert(staff._staff staffer, int staffId, int girlId, bool capturedAtHire)
        {
            if (staffId < 0 || girlId < 0)
            {
                return;
            }
            EnsureLoaded();
            StaffIdolRecord record;
            if (!StaffToGirl.TryGetValue(staffId, out record) || record == null)
            {
                record = new StaffIdolRecord
                {
                    StaffId = staffId,
                    GirlId = girlId,
                    CapturedAtHire = capturedAtHire
                };
            }
            else
            {
                record.GirlId = girlId;
                if (capturedAtHire)
                {
                    record.CapturedAtHire = true;
                }
            }
            ApplyIdentity(record, staffer);
            StaffToGirl[staffId] = record;
            Save();
        }

        internal static void BackfillFromStaff()
        {
            EnsureLoaded();
            if (staff.Staff == null)
            {
                return;
            }
            foreach (staff._staff staffer in staff.Staff)
            {
                if (staffer == null)
                {
                    continue;
                }
                int girlId;
                if (!TryResolveStaffer(staffer, out girlId))
                {
                    continue;
                }
                data_girls.girls girl = data_girls.GetGirlByID(girlId);
                if (girl != null && GraduationSnapshotStore.GetSnapshot(girlId) == null)
                {
                    GraduationSnapshotStore.Capture(girl);
                }
            }
            Save();
        }

        internal static void EnsureReady()
        {
            EnsureLoaded();
        }

        internal static void ResetForScopeChange()
        {
            loaded = false;
            loadHealthy = true;
            loadedScope = "";
            StaffToGirl.Clear();
        }

        internal static void RebindLoadedScope()
        {
            loaded = true;
            loadHealthy = true;
            loadedScope = GraduationDetailsPaths.GetScopeId();
        }

        internal static void SaveToDirectory(string directory)
        {
            string path;
            string validatedDirectory;
            if (string.IsNullOrEmpty(directory)
                || !GraduationDetailsPaths.TryValidateOwnedDataDirectory(
                    directory,
                    out validatedDirectory)
                || !GraduationDetailsPersistenceIO.TryGetSafeLeafPath(
                    validatedDirectory,
                    GraduationDetailsPaths.StaffMapFile,
                    true,
                    out path))
            {
                throw new InvalidDataException("Unsafe staff snapshot path.");
            }

            Directory.CreateDirectory(validatedDirectory);
            StaffIdolRecordList list = new StaffIdolRecordList();
            foreach (KeyValuePair<int, StaffIdolRecord> entry in StaffToGirl)
            {
                if (entry.Value != null)
                {
                    list.Records.Add(entry.Value);
                }
            }
            string json = JsonUtility.ToJson(list, true);
            GraduationDetailsPersistenceIO.WriteUtf8Durable(path, json);
        }

        internal static string GetEmptyJson()
        {
            return JsonUtility.ToJson(new StaffIdolRecordList(), true);
        }

        internal static bool TryValidateFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }
                if (!GraduationDetailsPersistenceIO.HasExplicitRootArrayProperty(
                    json,
                    "Records"))
                {
                    return false;
                }
                StaffIdolRecordList list = JsonUtility.FromJson<StaffIdolRecordList>(json);
                if (list == null || list.Records == null)
                {
                    return false;
                }
                HashSet<int> ids = new HashSet<int>();
                foreach (StaffIdolRecord record in list.Records)
                {
                    if (record == null
                        || record.StaffId < 0
                        || record.GirlId < 0
                        || !ids.Add(record.StaffId))
                    {
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static int TryResolveGirlId(staff._staff staffer)
        {
            if (staffer == null || !staffer.IsIdol())
            {
                return -1;
            }
            if (data_girls.girl == null)
            {
                return -1;
            }
            int resolved = FindMatchByName(staffer, true);
            if (resolved >= 0)
            {
                return resolved;
            }
            resolved = FindMatchByName(staffer, false);
            if (resolved >= 0)
            {
                return resolved;
            }
            resolved = FindMatch(staffer, true);
            if (resolved >= 0)
            {
                return resolved;
            }
            return FindMatch(staffer, false);
        }

        private static int FindMatchByName(staff._staff staffer, bool requireGraduated)
        {
            if (staffer == null || data_girls.girl == null)
            {
                return -1;
            }
            List<data_girls.girls> matches = new List<data_girls.girls>();
            foreach (data_girls.girls girl in data_girls.girl)
            {
                if (girl == null)
                {
                    continue;
                }
                if (requireGraduated && girl.status != data_girls._status.graduated)
                {
                    continue;
                }
                if (GraduationIdentity.NamesMatch(staffer, girl))
                {
                    matches.Add(girl);
                }
            }
            if (matches.Count == 1)
            {
                return matches[0].id;
            }
            if (matches.Count > 1)
            {
                data_girls.girls textureMatch = null;
                foreach (data_girls.girls match in matches)
                {
                    if (GraduationIdentity.TextureAssetsMatch(staffer.textureAssets, match.textureAssets))
                    {
                        if (textureMatch != null)
                        {
                            return -1;
                        }
                        textureMatch = match;
                    }
                }
                if (textureMatch != null)
                {
                    return textureMatch.id;
                }
            }
            return -1;
        }

        private static int FindMatch(staff._staff staffer, bool requireGraduated)
        {
            foreach (data_girls.girls girl in data_girls.girl)
            {
                if (girl == null)
                {
                    continue;
                }
                if (requireGraduated && girl.status != data_girls._status.graduated)
                {
                    continue;
                }
                if (GraduationIdentity.TextureAssetsMatch(staffer.textureAssets, girl.textureAssets))
                {
                    return girl.id;
                }
            }
            return -1;
        }

        private static bool IsLikelyMatch(staff._staff staffer, data_girls.girls girl)
        {
            if (staffer == null || girl == null)
            {
                return false;
            }
            if (GraduationIdentity.NamesMatch(staffer, girl))
            {
                return true;
            }
            bool missingNames = string.IsNullOrEmpty(staffer.firstName) || string.IsNullOrEmpty(staffer.lastName)
                || string.IsNullOrEmpty(girl.firstName) || string.IsNullOrEmpty(girl.lastName);
            if (missingNames)
            {
                return GraduationIdentity.TextureAssetsMatch(staffer.textureAssets, girl.textureAssets);
            }
            return false;
        }

        private static bool RecordHasIdentity(StaffIdolRecord record)
        {
            return record != null
                && (!string.IsNullOrEmpty(record.FirstName)
                    || !string.IsNullOrEmpty(record.LastName)
                    || !string.IsNullOrEmpty(record.Nickname)
                    || !string.IsNullOrEmpty(record.TextureSignature));
        }

        private static bool RecordMatchesStaffer(staff._staff staffer, StaffIdolRecord record)
        {
            if (staffer == null || record == null)
            {
                return false;
            }
            if (!RecordHasIdentity(record))
            {
                return true;
            }
            if (!string.IsNullOrEmpty(record.FirstName) && !GraduationIdentity.SameText(record.FirstName, staffer.firstName))
            {
                return false;
            }
            if (!string.IsNullOrEmpty(record.LastName) && !GraduationIdentity.SameText(record.LastName, staffer.lastName))
            {
                return false;
            }
            if (!string.IsNullOrEmpty(record.Nickname) && !GraduationIdentity.SameText(record.Nickname, staffer.nickname))
            {
                return false;
            }
            string currentTextureSignature = GraduationIdentity.TextureSignature(staffer);
            if (!string.IsNullOrEmpty(record.TextureSignature)
                && !string.IsNullOrEmpty(currentTextureSignature)
                && !string.Equals(record.TextureSignature, currentTextureSignature, StringComparison.Ordinal))
            {
                return false;
            }
            return true;
        }

        private static void ApplyIdentity(StaffIdolRecord record, staff._staff staffer)
        {
            if (record == null || staffer == null)
            {
                return;
            }
            record.FirstName = staffer.firstName ?? "";
            record.LastName = staffer.lastName ?? "";
            record.Nickname = staffer.nickname ?? "";
            record.TextureSignature = GraduationIdentity.TextureSignature(staffer);
        }

        private static void EnsureLoaded()
        {
            string scope = GraduationDetailsPaths.GetScopeId();
            if (loaded && loadedScope == scope)
            {
                return;
            }
            loadedScope = scope;
            loaded = true;
            loadHealthy = true;
            try
            {
                if (!GraduationDetailsPaths.HasActiveSaveScope)
                {
                    StaffToGirl.Clear();
                    return;
                }
                if (!GraduationDetailsPaths.CanWriteActiveScope
                    || string.IsNullOrEmpty(DataPath))
                {
                    loadHealthy = false;
                    return;
                }
                if (!File.Exists(DataPath))
                {
                    StaffToGirl.Clear();
                    return;
                }
                string loadPath = DataPath;
                string json = File.ReadAllText(loadPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    loadHealthy = false;
                    return;
                }
                if (!GraduationDetailsPersistenceIO.HasExplicitRootArrayProperty(
                    json,
                    "Records"))
                {
                    loadHealthy = false;
                    return;
                }
                StaffIdolRecordList list = JsonUtility.FromJson<StaffIdolRecordList>(json);
                if (list == null || list.Records == null)
                {
                    loadHealthy = false;
                    return;
                }
                Dictionary<int, StaffIdolRecord> parsed = new Dictionary<int, StaffIdolRecord>();
                foreach (StaffIdolRecord record in list.Records)
                {
                    if (record == null
                        || record.StaffId < 0
                        || record.GirlId < 0
                        || parsed.ContainsKey(record.StaffId))
                    {
                        loadHealthy = false;
                        return;
                    }
                    parsed[record.StaffId] = record;
                }
                StaffToGirl.Clear();
                foreach (KeyValuePair<int, StaffIdolRecord> entry in parsed)
                {
                    StaffToGirl[entry.Key] = entry.Value;
                }
            }
            catch (Exception exception)
            {
                loadHealthy = false;
                Debug.LogWarning("[Graduation Details] Staff sidecar is unreadable and was left " +
                    "untouched: " + exception.Message);
            }
        }

        private static void Save()
        {
            // Mutations stay in memory until the matching vanilla save is scheduled.
        }
    }

    internal static class StaffHireContext
    {
        internal static bool Active;
        internal static int GirlId = -1;

        internal static void Begin(data_girls.girls girl)
        {
            Active = girl != null;
            GirlId = girl != null ? girl.id : -1;
        }

        internal static void Complete(staff._staff staffer)
        {
            if (staffer != null && GirlId >= 0)
            {
                StaffIdolStore.Upsert(staffer, GirlId, true);
            }
            Clear();
        }

        internal static void Clear()
        {
            Active = false;
            GirlId = -1;
        }
    }

    [Serializable]
    internal sealed class GraduationSnapshot
    {
        public int GirlId = -1;
        public string Birthdate = "";
        public int AgeAtGraduation = -1;
        public string PortraitFile = "";
        public string FirstName = "";
        public string LastName = "";
        public string Nickname = "";
        public string TextureSignature = "";
        public List<FanSnapshot> Fans = new List<FanSnapshot>();
        public List<BondSectionSnapshot> Bonds = new List<BondSectionSnapshot>();
    }

    [Serializable]
    internal sealed class GraduationSnapshotList
    {
        public List<GraduationSnapshot> Records = new List<GraduationSnapshot>();
    }

    [Serializable]
    internal sealed class FanSnapshot
    {
        public resources.fanType Gender;
        public resources.fanType Hardcoreness;
        public resources.fanType Age;
        public long People;
        public float Appeal;
        public float Opinion;
    }

    internal enum BondSectionType
    {
        CliqueKnown = 0,
        CliqueUnknown = 1,
        Bullies = 2,
        BulliedBy = 3,
        BestFriends = 4,
        Friends = 5,
        Dislikes = 6,
        Hates = 7,
        NoInfo = 8
    }

    [Serializable]
    internal sealed class BondEntry
    {
        public int GirlId = -1;
        public bool Known;
        public float RelationshipRatio = 0.5f;
        public bool IsDatingKnown;
    }

    [Serializable]
    internal sealed class BondSectionSnapshot
    {
        public BondSectionType Type;
        public int LeaderId = -1;
        public List<BondEntry> Entries = new List<BondEntry>();
    }

    internal static class GraduationSnapshotStore
    {
        private static readonly Dictionary<int, GraduationSnapshot> Records = new Dictionary<int, GraduationSnapshot>();
        private static readonly Dictionary<string, HashSet<string>> PendingPortraitTargets =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private static bool loaded;
        private static bool loadHealthy = true;
        private static string loadedScope = "";

        internal static bool CanSnapshot
        {
            get
            {
                EnsureLoaded();
                return loadHealthy
                    && (!GraduationDetailsPaths.HasActiveSaveScope
                        || GraduationDetailsPaths.CanWriteActiveScope);
            }
        }

        private static string DataPath
        {
            get
            {
                return GraduationDetailsPaths.GetScopedFilePath(
                    GraduationDetailsPaths.SnapshotsFile);
            }
        }

        private static string PortraitDir
        {
            get
            {
                return GraduationDetailsPaths.GetScopedPortraitDir();
            }
        }

        private static string WorkingPortraitDir
        {
            get
            {
                return GraduationDetailsPaths.GetWorkingPortraitDir();
            }
        }

        internal static GraduationSnapshot GetSnapshot(int girlId)
        {
            EnsureLoaded();
            GraduationSnapshot record;
            if (Records.TryGetValue(girlId, out record))
            {
                return record;
            }
            return null;
        }

        internal static void Capture(data_girls.girls girl)
        {
            if (girl == null)
            {
                return;
            }
            GraduationSnapshot existing = GetSnapshot(girl.id);
            GraduationSnapshot snapshot = existing;
            if (snapshot == null)
            {
                snapshot = new GraduationSnapshot
                {
                    GirlId = girl.id
                };
            }
            RefreshIdentity(snapshot, girl);

            // Keep existing metadata if already captured; this avoids drifting "at graduation" data later.
            if (string.IsNullOrEmpty(snapshot.Birthdate))
            {
                snapshot.Birthdate = ExtensionMethods.ToDataString(girl.birthday);
            }
            if (snapshot.AgeAtGraduation <= 0)
            {
                snapshot.AgeAtGraduation = girl.GetAge();
            }

            // Build current snapshots.
            List<FanSnapshot> candidateFans = BuildFanSnapshot(girl);
            List<BondSectionSnapshot> candidateBonds = BuildBondSnapshot(girl);

            // Preserve the best available fan data.
            // Staff-hire flow can occur after fan buckets are cleared/reset, so avoid overwriting a stronger snapshot.
            if (ShouldReplaceFans(existing, candidateFans))
            {
                snapshot.Fans = candidateFans;
            }
            else if (snapshot.Fans == null)
            {
                snapshot.Fans = new List<FanSnapshot>();
            }

            // Preserve meaningful bond data similarly.
            if (ShouldReplaceBonds(existing, candidateBonds))
            {
                snapshot.Bonds = candidateBonds;
            }
            else if (snapshot.Bonds == null)
            {
                snapshot.Bonds = new List<BondSectionSnapshot>();
            }

            Upsert(snapshot);
            TryCapturePortrait(girl, snapshot);
        }

        internal static void Backfill(List<data_girls.girls> girls)
        {
            if (girls == null)
            {
                return;
            }
            foreach (data_girls.girls girl in girls)
            {
                if (girl == null || girl.status != data_girls._status.graduated)
                {
                    continue;
                }
                GraduationSnapshot existing = GetSnapshot(girl.id);
                if (existing != null)
                {
                    // Older saves may contain the snapshot JSON but have missed an asynchronous
                    // portrait capture. Complete that already-persisted snapshot in its fixed
                    // scope when the recreated temporary portrait becomes available.
                    if (string.IsNullOrEmpty(existing.PortraitFile))
                    {
                        Capture(girl);
                        existing = GetSnapshot(girl.id);
                    }
                    else
                    {
                        TryCapturePortrait(girl, existing);
                    }
                    RegisterScopedPortraitRepair(existing);
                    continue;
                }
                Capture(girl);
            }
        }

        internal static string GetPortraitPath(GraduationSnapshot snapshot)
        {
            string workingPath;
            if (snapshot == null
                || !GraduationDetailsPaths.TryGetSafePortraitPath(
                    WorkingPortraitDir,
                    snapshot.PortraitFile,
                    false,
                    out workingPath))
            {
                return "";
            }
            if (File.Exists(workingPath))
            {
                return workingPath;
            }

            if (!string.IsNullOrEmpty(PortraitDir))
            {
                string scopedPath;
                if (GraduationDetailsPaths.TryGetSafePortraitPath(
                        PortraitDir,
                        snapshot.PortraitFile,
                        false,
                        out scopedPath)
                    && File.Exists(scopedPath))
                {
                    return scopedPath;
                }
            }
            return workingPath;
        }

        internal static long GetTotalFans(GraduationSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Fans == null)
            {
                return 0L;
            }
            long total = 0L;
            foreach (FanSnapshot fan in snapshot.Fans)
            {
                if (fan != null)
                {
                    total += fan.People;
                }
            }
            return total;
        }

        internal static long GetFanCount(GraduationSnapshot snapshot, resources.fanType gender, resources.fanType hardcoreness, resources.fanType age)
        {
            FanSnapshot fan = GetFanSnapshot(snapshot, gender, hardcoreness, age);
            return fan != null ? fan.People : 0L;
        }

        internal static long GetFanCount(GraduationSnapshot snapshot, resources.fanType type)
        {
            if (snapshot == null || snapshot.Fans == null)
            {
                return 0L;
            }
            long total = 0L;
            foreach (FanSnapshot fan in snapshot.Fans)
            {
                if (fan == null)
                {
                    continue;
                }
                if (fan.Gender == type || fan.Hardcoreness == type || fan.Age == type)
                {
                    total += fan.People;
                }
            }
            return total;
        }

        internal static float GetFanRatio(GraduationSnapshot snapshot, resources.fanType type)
        {
            long total = GetTotalFans(snapshot);
            if (total == 0L)
            {
                return 1f;
            }
            long count = GetFanCount(snapshot, type);
            return (float)count / (float)total;
        }

        internal static float GetFanAppeal(GraduationSnapshot snapshot, resources.fanType gender, resources.fanType hardcoreness, resources.fanType age)
        {
            FanSnapshot fan = GetFanSnapshot(snapshot, gender, hardcoreness, age);
            return fan != null ? fan.Appeal : 0f;
        }

        internal static float GetFanOpinion(GraduationSnapshot snapshot, resources.fanType gender, resources.fanType hardcoreness, resources.fanType age)
        {
            FanSnapshot fan = GetFanSnapshot(snapshot, gender, hardcoreness, age);
            return fan != null ? fan.Opinion : 0.5f;
        }

        internal static bool HasFanSnapshot(GraduationSnapshot snapshot)
        {
            return snapshot != null && snapshot.Fans != null && snapshot.Fans.Count > 0;
        }

        internal static bool HasBondSnapshot(GraduationSnapshot snapshot)
        {
            return snapshot != null && snapshot.Bonds != null && snapshot.Bonds.Count > 0;
        }

        private static FanSnapshot GetFanSnapshot(GraduationSnapshot snapshot, resources.fanType gender, resources.fanType hardcoreness, resources.fanType age)
        {
            if (snapshot == null || snapshot.Fans == null)
            {
                return null;
            }
            foreach (FanSnapshot fan in snapshot.Fans)
            {
                if (fan == null)
                {
                    continue;
                }
                if (fan.Gender == gender && fan.Hardcoreness == hardcoreness && fan.Age == age)
                {
                    return fan;
                }
            }
            return null;
        }

        private static bool ShouldReplaceFans(GraduationSnapshot existing, List<FanSnapshot> candidateFans)
        {
            if (candidateFans == null)
            {
                return existing == null || existing.Fans == null;
            }

            long candidateTotal = GetTotalFansFromList(candidateFans);
            if (existing == null || existing.Fans == null || existing.Fans.Count == 0)
            {
                return candidateFans.Count > 0;
            }

            long existingTotal = GetTotalFans(existing);
            if (existingTotal <= 0L)
            {
                return candidateFans.Count > 0;
            }

            // Never overwrite real fan history with empty/zero snapshots captured later.
            if (candidateTotal <= 0L)
            {
                return false;
            }

            // Prefer the richer snapshot when both are meaningful.
            return candidateTotal >= existingTotal;
        }

        private static long GetTotalFansFromList(List<FanSnapshot> fans)
        {
            if (fans == null)
            {
                return 0L;
            }
            long total = 0L;
            foreach (FanSnapshot fan in fans)
            {
                if (fan != null)
                {
                    total += fan.People;
                }
            }
            return total;
        }

        private static bool ShouldReplaceBonds(GraduationSnapshot existing, List<BondSectionSnapshot> candidateBonds)
        {
            if (candidateBonds == null)
            {
                return existing == null || existing.Bonds == null;
            }

            if (existing == null || existing.Bonds == null || existing.Bonds.Count == 0)
            {
                return candidateBonds.Count > 0;
            }

            bool existingMeaningful = HasMeaningfulBonds(existing.Bonds);
            bool candidateMeaningful = HasMeaningfulBonds(candidateBonds);
            if (!existingMeaningful)
            {
                return candidateBonds.Count > 0;
            }
            if (!candidateMeaningful)
            {
                return false;
            }
            return candidateBonds.Count >= existing.Bonds.Count;
        }

        private static bool HasMeaningfulBonds(List<BondSectionSnapshot> bonds)
        {
            if (bonds == null || bonds.Count == 0)
            {
                return false;
            }
            if (bonds.Count == 1)
            {
                BondSectionSnapshot only = bonds[0];
                if (only != null && only.Type == BondSectionType.NoInfo && (only.Entries == null || only.Entries.Count == 0))
                {
                    return false;
                }
            }
            return true;
        }

        private static List<FanSnapshot> BuildFanSnapshot(data_girls.girls girl)
        {
            List<FanSnapshot> list = new List<FanSnapshot>();
            if (girl == null || girl.Fans == null)
            {
                return list;
            }
            foreach (resources._fan fan in girl.Fans)
            {
                if (fan == null)
                {
                    continue;
                }
                FanSnapshot snapshot = new FanSnapshot
                {
                    Gender = fan.gender,
                    Hardcoreness = fan.hardcoreness,
                    Age = fan.age,
                    People = fan.people,
                    Appeal = fan.appeal,
                    Opinion = fan.Ratio
                };
                list.Add(snapshot);
            }
            return list;
        }

        private static List<BondSectionSnapshot> BuildBondSnapshot(data_girls.girls girl)
        {
            List<BondSectionSnapshot> sections = new List<BondSectionSnapshot>();
            if (girl == null)
            {
                return sections;
            }

            Relationships._clique clique = girl.GetClique();
            if (clique != null)
            {
                AddBondSection(sections,
                    clique.Known ? BondSectionType.CliqueKnown : BondSectionType.CliqueUnknown,
                    BuildEntriesFromGirls(clique.Members, girl, clique.Known, null),
                    clique.Leader != null ? clique.Leader.id : -1);

                if (clique.Bullied_Girls != null && clique.Bullied_Girls.Count > 0)
                {
                    AddBondSection(sections,
                        BondSectionType.Bullies,
                        BuildEntriesFromGirls(clique.Bullied_Girls, girl, false, clique.KnownBulliedGirls),
                        clique.Leader != null ? clique.Leader.id : -1);
                }
            }

            List<Relationships._clique> bullyingCliques = girl.GetCliquesThatBully(false);
            if (bullyingCliques != null)
            {
                foreach (Relationships._clique bullyClique in bullyingCliques)
                {
                    if (bullyClique == null)
                    {
                        continue;
                    }
                    bool targetKnown = bullyClique.KnownBulliedGirls != null && bullyClique.KnownBulliedGirls.Contains(girl);
                    bool known = false;
                    List<data_girls.girls> knownGirls = null;
                    if (targetKnown)
                    {
                        if (bullyClique.Known)
                        {
                            known = true;
                        }
                        else
                        {
                            known = false;
                            knownGirls = new List<data_girls.girls>();
                            if (bullyClique.Leader != null)
                            {
                                knownGirls.Add(bullyClique.Leader);
                            }
                        }
                    }
                    List<BondEntry> entries = BuildEntriesFromGirls(bullyClique.Members, girl, known, knownGirls);
                    AddBondSection(sections, BondSectionType.BulliedBy, entries, bullyClique.Leader != null ? bullyClique.Leader.id : -1);
                }
            }

            List<Relationships._relationship> allRelationships = Relationships.GetAllRelationships(girl, false);
            List<Relationships._relationship> bestFriends = new List<Relationships._relationship>();
            List<Relationships._relationship> friends = new List<Relationships._relationship>();
            List<Relationships._relationship> dislikes = new List<Relationships._relationship>();
            List<Relationships._relationship> hates = new List<Relationships._relationship>();
            foreach (Relationships._relationship relationship in allRelationships)
            {
                if (relationship == null || relationship.Girls == null || relationship.Girls.Count < 2)
                {
                    continue;
                }
                if (relationship.Girls[0] == null || relationship.Girls[1] == null)
                {
                    continue;
                }
                if (relationship.Girls[0].status == data_girls._status.graduated || relationship.Girls[1].status == data_girls._status.graduated)
                {
                    continue;
                }
                if (relationship.Status == Relationships._relationship._status.best_friends)
                {
                    bestFriends.Add(relationship);
                }
                else if (relationship.Status == Relationships._relationship._status.friends)
                {
                    friends.Add(relationship);
                }
                else if (relationship.Status == Relationships._relationship._status.dislikes)
                {
                    dislikes.Add(relationship);
                }
                else if (relationship.Status == Relationships._relationship._status.hates)
                {
                    hates.Add(relationship);
                }
            }

            AddBondSection(sections, BondSectionType.BestFriends, BuildEntriesFromRelationships(bestFriends, girl), -1);
            AddBondSection(sections, BondSectionType.Friends, BuildEntriesFromRelationships(friends, girl), -1);
            AddBondSection(sections, BondSectionType.Dislikes, BuildEntriesFromRelationships(dislikes, girl), -1);
            AddBondSection(sections, BondSectionType.Hates, BuildEntriesFromRelationships(hates, girl), -1);

            if (sections.Count == 0)
            {
                sections.Add(new BondSectionSnapshot
                {
                    Type = BondSectionType.NoInfo
                });
            }
            return sections;
        }

        private static void AddBondSection(List<BondSectionSnapshot> sections, BondSectionType type, List<BondEntry> entries, int leaderId)
        {
            if (sections == null || entries == null || entries.Count == 0)
            {
                return;
            }
            BondSectionSnapshot snapshot = new BondSectionSnapshot
            {
                Type = type,
                LeaderId = leaderId,
                Entries = entries
            };
            sections.Add(snapshot);
        }

        private static List<BondEntry> BuildEntriesFromGirls(List<data_girls.girls> girls, data_girls.girls parent, bool known, List<data_girls.girls> knownGirls)
        {
            List<BondEntry> entries = new List<BondEntry>();
            if (girls == null)
            {
                return entries;
            }
            foreach (data_girls.girls other in girls)
            {
                BondEntry entry = new BondEntry();
                if (other != null)
                {
                    bool isKnown = known || other == parent || (knownGirls != null && knownGirls.Contains(other));
                    entry.GirlId = other.id;
                    entry.Known = isKnown;
                    FillBondEntryStats(entry, parent, other);
                }
                else
                {
                    entry.GirlId = -1;
                    entry.Known = false;
                }
                entries.Add(entry);
            }
            return entries;
        }

        private static List<BondEntry> BuildEntriesFromRelationships(List<Relationships._relationship> relationships, data_girls.girls parent)
        {
            List<BondEntry> entries = new List<BondEntry>();
            if (relationships == null)
            {
                return entries;
            }
            foreach (Relationships._relationship relationship in relationships)
            {
                if (relationship == null || relationship.Girls == null || relationship.Girls.Count < 2)
                {
                    continue;
                }
                bool known = relationship.IsRelationshipKnown();
                if (!known)
                {
                    entries.Add(new BondEntry
                    {
                        GirlId = -1,
                        Known = false,
                        RelationshipRatio = relationship.Ratio,
                        IsDatingKnown = relationship.IsDatingAndKnown()
                    });
                    continue;
                }
                data_girls.girls other = relationship.Girls[0] == parent ? relationship.Girls[1] : relationship.Girls[0];
                if (other == null)
                {
                    entries.Add(new BondEntry
                    {
                        GirlId = -1,
                        Known = false,
                        RelationshipRatio = relationship.Ratio,
                        IsDatingKnown = relationship.IsDatingAndKnown()
                    });
                    continue;
                }
                entries.Add(new BondEntry
                {
                    GirlId = other.id,
                    Known = true,
                    RelationshipRatio = relationship.Ratio,
                    IsDatingKnown = relationship.IsDatingAndKnown()
                });
            }
            return entries;
        }

        private static void FillBondEntryStats(BondEntry entry, data_girls.girls parent, data_girls.girls other)
        {
            if (entry == null || parent == null || other == null || other == parent)
            {
                return;
            }
            Relationships._relationship relationship = TryGetRelationship(parent, other);
            if (relationship != null)
            {
                entry.RelationshipRatio = relationship.Ratio;
                entry.IsDatingKnown = relationship.IsDatingAndKnown();
            }
        }

        private static Relationships._relationship TryGetRelationship(data_girls.girls girl, data_girls.girls other)
        {
            if (girl == null || other == null)
            {
                return null;
            }
            Relationships._relationship relationship = girl.GetCachedRelationship(other);
            if (relationship != null)
            {
                return relationship;
            }
            relationship = other.GetCachedRelationship(girl);
            if (relationship != null)
            {
                return relationship;
            }
            if (Relationships.RelationshipsData == null)
            {
                return null;
            }
            foreach (Relationships._relationship candidate in Relationships.RelationshipsData)
            {
                if (candidate == null || candidate.Girls == null || candidate.Girls.Count < 2)
                {
                    continue;
                }
                if ((candidate.Girls[0] == girl && candidate.Girls[1] == other) || (candidate.Girls[0] == other && candidate.Girls[1] == girl))
                {
                    return candidate;
                }
            }
            return null;
        }

        private static void Upsert(GraduationSnapshot snapshot)
        {
            if (snapshot == null || snapshot.GirlId < 0)
            {
                return;
            }
            EnsureLoaded();
            Records[snapshot.GirlId] = snapshot;
            Save();
        }

        internal static void EnsureReady()
        {
            EnsureLoaded();
        }

        internal static void ResetForScopeChange()
        {
            loaded = false;
            loadHealthy = true;
            loadedScope = "";
            Records.Clear();
        }

        internal static void RebindLoadedScope()
        {
            loaded = true;
            loadHealthy = true;
            loadedScope = GraduationDetailsPaths.GetScopeId();
        }

        internal static void SaveToDirectory(string directory)
        {
            string dataPath;
            string targetPortraitDirectory;
            string validatedDirectory;
            if (string.IsNullOrEmpty(directory)
                || !GraduationDetailsPaths.TryValidateOwnedDataDirectory(
                    directory,
                    out validatedDirectory)
                || !GraduationDetailsPersistenceIO.TryGetSafeLeafPath(
                    validatedDirectory,
                    GraduationDetailsPaths.SnapshotsFile,
                    true,
                    out dataPath)
                || !GraduationDetailsPersistenceIO.TryGetContainedPath(
                    validatedDirectory,
                    GraduationDetailsPaths.PortraitsFolder,
                    true,
                    out targetPortraitDirectory))
            {
                throw new InvalidDataException("Unsafe graduation snapshot path.");
            }

            Directory.CreateDirectory(validatedDirectory);
            GraduationSnapshotList list = new GraduationSnapshotList();
            list.Records = Records.Values.ToList();
            string json = JsonUtility.ToJson(list, true);
            GraduationDetailsPersistenceIO.WriteUtf8Durable(dataPath, json);
            foreach (GraduationSnapshot snapshot in Records.Values)
            {
                string targetPath;
                if (snapshot == null)
                {
                    throw new InvalidDataException("Null record in live graduation snapshot.");
                }
                if (string.IsNullOrEmpty(snapshot.PortraitFile))
                {
                    continue;
                }
                if (!GraduationDetailsPaths.TryGetSafePortraitPath(
                        targetPortraitDirectory,
                        snapshot.PortraitFile,
                        true,
                        out targetPath))
                {
                    throw new InvalidDataException("Unsafe portrait filename in live snapshot.");
                }

                string sourcePath = GetPortraitPath(snapshot);
                if (!File.Exists(sourcePath))
                {
                    continue;
                }
                if (PathsReferToSameFile(sourcePath, targetPath))
                {
                    continue;
                }
                if (!CopyPortrait(sourcePath, targetPath))
                {
                    throw new IOException("Failed to durably stage graduation portrait.");
                }
            }
        }

        internal static string GetEmptyJson()
        {
            return JsonUtility.ToJson(new GraduationSnapshotList(), true);
        }

        internal static bool TryValidateFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }
                if (!GraduationDetailsPersistenceIO.HasExplicitRootArrayProperty(
                    json,
                    "Records"))
                {
                    return false;
                }
                GraduationSnapshotList list = JsonUtility.FromJson<GraduationSnapshotList>(json);
                if (list == null || list.Records == null)
                {
                    return false;
                }
                HashSet<int> ids = new HashSet<int>();
                foreach (GraduationSnapshot snapshot in list.Records)
                {
                    if (!IsValidSnapshot(snapshot) || !ids.Add(snapshot.GirlId))
                    {
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static void RegisterPendingPortraitTargetsForDirectory(string dataDirectory)
        {
            string targetPortraitDirectory;
            string snapshotDataPath;
            if (string.IsNullOrEmpty(dataDirectory)
                || !GraduationDetailsPersistenceIO.TryGetContainedPath(
                    dataDirectory,
                    GraduationDetailsPaths.PortraitsFolder,
                    true,
                    out targetPortraitDirectory)
                || !GraduationDetailsPersistenceIO.TryGetSafeLeafPath(
                    dataDirectory,
                    GraduationDetailsPaths.SnapshotsFile,
                    false,
                    out snapshotDataPath)
                || !TryValidateFile(snapshotDataPath))
            {
                return;
            }
            GraduationSnapshotList persisted;
            try
            {
                persisted = JsonUtility.FromJson<GraduationSnapshotList>(
                    File.ReadAllText(snapshotDataPath));
            }
            catch
            {
                return;
            }
            if (persisted == null || persisted.Records == null)
            {
                return;
            }
            foreach (GraduationSnapshot snapshot in persisted.Records)
            {
                string workingPath;
                string targetPath;
                if (snapshot == null
                    || !GraduationDetailsPaths.TryGetSafePortraitPath(
                        WorkingPortraitDir,
                        snapshot.PortraitFile,
                        false,
                        out workingPath)
                    || !GraduationDetailsPaths.TryGetSafePortraitPath(
                        targetPortraitDirectory,
                        snapshot.PortraitFile,
                        true,
                        out targetPath)
                    || File.Exists(targetPath))
                {
                    continue;
                }
                if (File.Exists(workingPath))
                {
                    CopyPortrait(workingPath, targetPath);
                }
                else
                {
                    RegisterPendingPortraitTarget(workingPath, targetPath);
                }
            }
        }

        internal static void RemovePendingPortraitTargetsUnderDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }
            foreach (string source in PendingPortraitTargets.Keys.ToArray())
            {
                if (GraduationDetailsPaths.IsPathContainedBy(directory, source))
                {
                    PendingPortraitTargets.Remove(source);
                    continue;
                }
                HashSet<string> targets = PendingPortraitTargets[source];
                targets.RemoveWhere(target =>
                    GraduationDetailsPaths.IsPathContainedBy(directory, target));
                if (targets.Count == 0)
                {
                    PendingPortraitTargets.Remove(source);
                }
            }
        }

        private static void EnsureLoaded()
        {
            string scope = GraduationDetailsPaths.GetScopeId();
            if (loaded && loadedScope == scope)
            {
                return;
            }
            loadedScope = scope;
            loaded = true;
            loadHealthy = true;
            try
            {
                if (!GraduationDetailsPaths.HasActiveSaveScope)
                {
                    Records.Clear();
                    return;
                }
                if (!GraduationDetailsPaths.CanWriteActiveScope
                    || string.IsNullOrEmpty(DataPath))
                {
                    loadHealthy = false;
                    return;
                }
                if (!File.Exists(DataPath))
                {
                    Records.Clear();
                    return;
                }
                string loadPath = DataPath;
                string json = File.ReadAllText(loadPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    loadHealthy = false;
                    return;
                }
                if (!GraduationDetailsPersistenceIO.HasExplicitRootArrayProperty(
                    json,
                    "Records"))
                {
                    loadHealthy = false;
                    return;
                }
                GraduationSnapshotList list = JsonUtility.FromJson<GraduationSnapshotList>(json);
                if (list == null || list.Records == null)
                {
                    loadHealthy = false;
                    return;
                }
                Dictionary<int, GraduationSnapshot> parsed =
                    new Dictionary<int, GraduationSnapshot>();
                foreach (GraduationSnapshot snapshot in list.Records)
                {
                    if (!IsValidSnapshot(snapshot) || parsed.ContainsKey(snapshot.GirlId))
                    {
                        loadHealthy = false;
                        return;
                    }
                    parsed[snapshot.GirlId] = snapshot;
                }
                Records.Clear();
                foreach (KeyValuePair<int, GraduationSnapshot> entry in parsed)
                {
                    Records[entry.Key] = entry.Value;
                }
            }
            catch (Exception exception)
            {
                loadHealthy = false;
                Debug.LogWarning("[Graduation Details] Graduation sidecar is unreadable and was " +
                    "left untouched: " + exception.Message);
            }
        }

        private static void Save()
        {
            // Mutations stay in memory until the matching vanilla save is scheduled.
        }

        private static bool IsValidSnapshot(GraduationSnapshot snapshot)
        {
            if (snapshot == null
                || snapshot.GirlId < 0
                || (!string.IsNullOrEmpty(snapshot.PortraitFile)
                    && !GraduationDetailsPaths.IsSafePortraitFileName(snapshot.PortraitFile))
                || snapshot.Fans == null
                || snapshot.Bonds == null)
            {
                return false;
            }
            foreach (FanSnapshot fan in snapshot.Fans)
            {
                if (fan == null)
                {
                    return false;
                }
            }
            foreach (BondSectionSnapshot section in snapshot.Bonds)
            {
                if (section == null || section.Entries == null)
                {
                    return false;
                }
                foreach (BondEntry entry in section.Entries)
                {
                    if (entry == null || entry.GirlId < 0)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static void TryCapturePortrait(data_girls.girls girl, GraduationSnapshot snapshot)
        {
            string existingPath = GetPortraitPath(snapshot);
            if (!string.IsNullOrEmpty(existingPath) && File.Exists(existingPath))
            {
                return;
            }
            if (snapshot == null || string.IsNullOrEmpty(snapshot.PortraitFile))
            {
                return;
            }
            string destPath;
            if (!GraduationDetailsPaths.TryGetSafePortraitPath(
                WorkingPortraitDir,
                snapshot.PortraitFile,
                true,
                out destPath))
            {
                return;
            }
            string sourcePath = GetSourcePortraitPath(girl);
            if (!string.IsNullOrEmpty(sourcePath) && File.Exists(sourcePath))
            {
                CopyWorkingPortraitAndPendingTargets(sourcePath, destPath);
                return;
            }
            mainScript main = Camera.main != null ? Camera.main.GetComponent<mainScript>() : null;
            if (main == null || main.Data == null)
            {
                return;
            }
            data_girls_textures textures = main.Data.GetComponent<data_girls_textures>();
            if (textures == null)
            {
                return;
            }
            data_girls_textures.AddToQueue(girl, null);
            main.StartCoroutine(WaitForPortraitAndCopy(girl, destPath));
        }

        private static string GetSourcePortraitPath(data_girls.girls girl)
        {
            if (girl == null || girl.texture == null)
            {
                return "";
            }
            return girl.texture.GetBigPortraitURL();
        }

        private static IEnumerator WaitForPortraitAndCopy(data_girls.girls girl, string destPath)
        {
            float start = Time.realtimeSinceStartup;
            const float TimeoutSeconds = 5f;
            while (Time.realtimeSinceStartup - start < TimeoutSeconds)
            {
                string sourcePath = GetSourcePortraitPath(girl);
                if (!string.IsNullOrEmpty(sourcePath) && File.Exists(sourcePath))
                {
                    CopyWorkingPortraitAndPendingTargets(sourcePath, destPath);
                    yield break;
                }
                yield return null;
            }
        }

        private static void RegisterPendingPortraitTarget(string sourcePath, string targetPath)
        {
            string validatedSource;
            string validatedTarget;
            if (string.IsNullOrEmpty(sourcePath)
                || string.IsNullOrEmpty(targetPath)
                || !TryValidateWorkingPortraitPath(sourcePath, false, out validatedSource)
                || !TryValidateOwnedPortraitDestination(targetPath, out validatedTarget)
                || PathsReferToSameFile(sourcePath, targetPath))
            {
                return;
            }

            HashSet<string> targets;
            if (!PendingPortraitTargets.TryGetValue(validatedSource, out targets))
            {
                targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                PendingPortraitTargets[validatedSource] = targets;
            }
            targets.Add(validatedTarget);
        }

        private static void RegisterScopedPortraitRepair(GraduationSnapshot snapshot)
        {
            if (snapshot == null
                || string.IsNullOrEmpty(snapshot.PortraitFile)
                || string.IsNullOrEmpty(PortraitDir))
            {
                return;
            }

            string workingPath;
            string scopedPath;
            if (!GraduationDetailsPaths.TryGetSafePortraitPath(
                    WorkingPortraitDir,
                    snapshot.PortraitFile,
                    false,
                    out workingPath)
                || !GraduationDetailsPaths.TryGetSafePortraitPath(
                    PortraitDir,
                    snapshot.PortraitFile,
                    true,
                    out scopedPath))
            {
                return;
            }
            if (File.Exists(scopedPath))
            {
                return;
            }
            if (File.Exists(workingPath))
            {
                CopyPortrait(workingPath, scopedPath);
                return;
            }
            RegisterPendingPortraitTarget(workingPath, scopedPath);
        }

        private static void CopyWorkingPortraitAndPendingTargets(
            string sourcePath,
            string workingPath)
        {
            if (!CopyPortrait(sourcePath, workingPath))
            {
                return;
            }

            HashSet<string> targets;
            if (!PendingPortraitTargets.TryGetValue(workingPath, out targets))
            {
                return;
            }

            HashSet<string> failedTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string targetPath in targets)
            {
                string portraitDirectory = Path.GetDirectoryName(targetPath);
                string targetDataDirectory = !string.IsNullOrEmpty(portraitDirectory)
                    ? Path.GetDirectoryName(portraitDirectory)
                    : "";
                if (string.IsNullOrEmpty(targetDataDirectory)
                    || !Directory.Exists(targetDataDirectory))
                {
                    // The transaction/generation was discarded or pruned. Never recreate it for
                    // an asynchronous portrait result.
                    continue;
                }
                if (!CopyPortrait(workingPath, targetPath))
                {
                    failedTargets.Add(targetPath);
                }
            }

            PendingPortraitTargets.Remove(workingPath);
            if (failedTargets.Count > 0)
            {
                PendingPortraitTargets[workingPath] = failedTargets;
            }
        }

        private static bool CopyPortrait(string sourcePath, string destPath)
        {
            try
            {
                string normalizedSource;
                string normalizedDestination;
                if (!GraduationDetailsPersistenceIO.TryNormalizeFilePath(
                        sourcePath,
                        out normalizedSource)
                    || !File.Exists(normalizedSource)
                    || (File.GetAttributes(normalizedSource) & FileAttributes.ReparsePoint) != 0
                    || !TryValidateOwnedPortraitDestination(
                        destPath,
                        out normalizedDestination))
                {
                    return false;
                }
                GraduationDetailsPersistenceIO.CopyFileDurable(
                    normalizedSource,
                    normalizedDestination);
                return true;
            }
            catch
            {
                // Ignore file copy errors to avoid breaking the game loop.
                return false;
            }
        }

        private static bool TryValidateWorkingPortraitPath(
            string path,
            bool forWrite,
            out string normalized)
        {
            normalized = "";
            string fileName;
            try
            {
                fileName = Path.GetFileName(path);
            }
            catch
            {
                return false;
            }
            return GraduationDetailsPaths.TryGetSafePortraitPath(
                    WorkingPortraitDir,
                    fileName,
                    forWrite,
                    out normalized)
                && PathsReferToSameFile(path, normalized);
        }

        private static bool TryValidateOwnedPortraitDestination(
            string path,
            out string normalized)
        {
            normalized = "";
            try
            {
                string parent = Path.GetDirectoryName(path);
                string fileName = Path.GetFileName(path);
                if (string.IsNullOrEmpty(parent)
                    || !string.Equals(
                        Path.GetFileName(parent),
                        GraduationDetailsPaths.PortraitsFolder,
                        StringComparison.OrdinalIgnoreCase)
                    || !GraduationDetailsPaths.TryGetSafePortraitPath(
                        parent,
                        fileName,
                        true,
                        out normalized)
                    || !PathsReferToSameFile(path, normalized))
                {
                    return false;
                }
                string validated;
                if (GraduationDetailsPaths.IsPathContainedBy(
                    GraduationDetailsPaths.RootDir,
                    normalized))
                {
                    return GraduationDetailsPersistenceIO.TryValidatePathUnderRoot(
                        Application.persistentDataPath,
                        normalized,
                        true,
                        out validated);
                }
                if (GraduationDetailsPaths.IsPathContainedBy(
                    WorkingPortraitDir,
                    normalized))
                {
                    return GraduationDetailsPersistenceIO.TryValidatePathUnderRoot(
                        Path.GetTempPath(),
                        normalized,
                        true,
                        out validated);
                }
                return false;
            }
            catch
            {
                normalized = "";
                return false;
            }
        }

        private static bool PathsReferToSameFile(string firstPath, string secondPath)
        {
            try
            {
                return string.Equals(
                    Path.GetFullPath(firstPath),
                    Path.GetFullPath(secondPath),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return string.Equals(firstPath, secondPath, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static void RefreshIdentity(GraduationSnapshot snapshot, data_girls.girls girl)
        {
            if (snapshot == null || girl == null)
            {
                return;
            }
            snapshot.FirstName = girl.firstName ?? "";
            snapshot.LastName = girl.lastName ?? "";
            snapshot.Nickname = girl.nickname ?? "";
            snapshot.TextureSignature = GraduationIdentity.TextureSignature(girl);

            string expectedFile = BuildPortraitFileName(girl, snapshot.TextureSignature);
            if (string.IsNullOrEmpty(snapshot.PortraitFile)
                || IsLegacyPortraitFile(snapshot.PortraitFile, girl)
                || !string.Equals(snapshot.PortraitFile, expectedFile, StringComparison.OrdinalIgnoreCase))
            {
                snapshot.PortraitFile = expectedFile;
            }
        }

        private static bool IsLegacyPortraitFile(string portraitFile, data_girls.girls girl)
        {
            if (string.IsNullOrEmpty(portraitFile) || girl == null)
            {
                return true;
            }
            return string.Equals(portraitFile, girl.id + ".png", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildPortraitFileName(data_girls.girls girl, string textureSignature)
        {
            if (girl == null)
            {
                return "unknown.png";
            }
            string identity = string.Join("|", new string[]
            {
                girl.id.ToString(),
                girl.firstName ?? "",
                girl.lastName ?? "",
                girl.nickname ?? "",
                textureSignature ?? ""
            });
            return "girl_" + girl.id + "_" + GraduationIdentity.ShortHash(identity) + ".png";
        }
    }

    internal static class GraduationDetailsState
    {
        internal static bool Active;
        internal static int GirlId = -1;
        internal static bool AllowNonGraduated;

        internal static void Begin(data_girls.girls girl, bool allowNonGraduated = false)
        {
            if (girl == null || (!allowNonGraduated && girl.status != data_girls._status.graduated))
            {
                Clear();
                return;
            }
            Active = true;
            GirlId = girl.id;
            AllowNonGraduated = allowNonGraduated;
        }

        internal static bool IsFor(data_girls.girls girl)
        {
            if (!Active || girl == null || girl.id != GirlId)
            {
                return false;
            }
            if (!AllowNonGraduated && girl.status != data_girls._status.graduated)
            {
                return false;
            }
            return true;
        }

        internal static void Clear()
        {
            Active = false;
            GirlId = -1;
            AllowNonGraduated = false;
        }
    }

    internal static class GraduationDetailsProfile
    {
        internal static void Show(data_girls.girls girl)
        {
            if (girl == null)
            {
                return;
            }
            GraduationSnapshotStore.Capture(girl);
            GraduationDetailsState.Begin(girl, false);

            OpenProfile(girl);
        }

        internal static void ShowForStaff(staff._staff staffer)
        {
            if (staffer == null)
            {
                return;
            }
            int girlId;
            if (!StaffIdolStore.TryResolveStaffer(staffer, out girlId))
            {
                return;
            }
            data_girls.girls girl = data_girls.GetGirlByID(girlId);
            if (girl == null)
            {
                return;
            }
            GraduationSnapshotStore.Capture(girl);
            GraduationDetailsState.Begin(girl, true);
            OpenProfile(girl);
        }

        private static void OpenProfile(data_girls.girls girl)
        {
            if (girl == null)
            {
                return;
            }

            if (Camera.main == null)
            {
                return;
            }
            mainScript main = Camera.main.GetComponent<mainScript>();
            if (main == null || main.Data == null)
            {
                return;
            }
            PopupManager popupManager = main.Data.GetComponent<PopupManager>();
            if (popupManager == null)
            {
                return;
            }
            PopupManager._popup popup = popupManager.GetByType(PopupManager._type.girl_profile);
            if (popup == null || popup.obj == null)
            {
                return;
            }
            Profile_Popup profile = popup.obj.GetComponent<Profile_Popup>();
            if (profile == null)
            {
                return;
            }
            popupManager.Open(PopupManager._type.girl_profile, true);
            profile.Set(girl);
            profile.SetTab(Profile_Popup._tabs.jobs);
        }
    }

    internal sealed class GraduationDetailsButton : MonoBehaviour
    {
        private int girlId = -1;
        private Graphic rootGraphic;
        private Button button;

        internal void SetGirl(data_girls.girls girl)
        {
            if (girl == null)
            {
                return;
            }
            this.girlId = girl.id;
            EnsureClickableTarget();
        }

        private void EnsureClickableTarget()
        {
            if (rootGraphic == null)
            {
                rootGraphic = GetComponent<Graphic>();
            }
            if (rootGraphic == null)
            {
                Image image = gameObject.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0f);
                rootGraphic = image;
            }
            rootGraphic.raycastTarget = true;

            if (button == null)
            {
                button = GetComponent<Button>();
                if (button == null)
                {
                    button = gameObject.AddComponent<Button>();
                }
                button.transition = Selectable.Transition.None;
                button.targetGraphic = rootGraphic;
            }
            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);

            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
            {
                if (graphic != rootGraphic)
                {
                    graphic.raycastTarget = false;
                }
            }
        }

        private void OnClick()
        {
            data_girls.girls girl = data_girls.GetGirlByID(girlId);
            if (girl == null)
            {
                return;
            }
            GraduationDetailsProfile.Show(girl);
        }
    }

    internal sealed class StaffProfileButton : MonoBehaviour
    {
        private int staffId = -1;

        internal void SetStaff(staff._staff staffer)
        {
            staffId = staffer != null ? staffer.id : -1;
        }

        public void OnClick()
        {
            staff._staff staffer = staff.GetStaffByID(staffId);
            if (staffer == null)
            {
                return;
            }
            GraduationDetailsProfile.ShowForStaff(staffer);
            ContextMenuController.Hide_();
        }
    }

    internal static class StaffProfileContextMenu
    {
        internal static void TryInject(ContextMenuController cmc, staff._staff staffer)
        {
            if (cmc == null || staffer == null)
            {
                return;
            }
            int girlId;
            bool mapped = StaffIdolStore.TryResolveStaffer(staffer, out girlId);
            if (!mapped && !StaffIdolStore.IsFormerIdolStaff(staffer))
            {
                return;
            }
            GameObject menu = cmc.open_mainMenu;
            if (menu == null)
            {
                return;
            }
            if (menu.GetComponentInChildren<StaffProfileButton>(true) != null)
            {
                return;
            }
            GameObject template = GetTemplateButton(menu);
            if (template == null)
            {
                return;
            }
            GameObject button = UnityEngine.Object.Instantiate(template);
            button.name = "ProfileButton";
            button.transform.SetParent(template.transform.parent, false);
            button.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            button.SetActive(true);
            ConfigureProfileButton(button, staffer, mapped);
        }

        private static GameObject GetTemplateButton(GameObject menu)
        {
            GameObject candidate = FindButtonByField(menu, "Nickname");
            if (candidate != null)
            {
                return candidate;
            }
            candidate = FindButtonByField(menu, "Fire");
            if (candidate != null)
            {
                return candidate;
            }
            ContextMenuButton contextButton = menu != null ? menu.GetComponentInChildren<ContextMenuButton>(true) : null;
            if (contextButton != null)
            {
                return contextButton.gameObject;
            }
            ButtonDefault buttonDefault = menu != null ? menu.GetComponentInChildren<ButtonDefault>(true) : null;
            if (buttonDefault != null)
            {
                return buttonDefault.gameObject;
            }
            Button uiButton = menu != null ? menu.GetComponentInChildren<Button>(true) : null;
            return uiButton != null ? uiButton.gameObject : null;
        }

        private static GameObject FindButtonByField(GameObject menu, string fieldName)
        {
            if (menu == null)
            {
                return null;
            }
            MonoBehaviour[] behaviours = menu.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }
                System.Reflection.FieldInfo field = behaviour.GetType().GetField(
                    fieldName,
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (field == null || field.FieldType != typeof(GameObject))
                {
                    continue;
                }
                GameObject obj = field.GetValue(behaviour) as GameObject;
                if (obj != null)
                {
                    return obj;
                }
            }
            return null;
        }

        private static void ConfigureProfileButton(GameObject button, staff._staff staffer, bool mapped)
        {
            if (button == null)
            {
                return;
            }
            DisableLocalization(button);
            ContextMenuButton contextButton = button.GetComponent<ContextMenuButton>();
            if (contextButton != null)
            {
                contextButton.prefab_childMenu = null;
                if (contextButton.arrow != null)
                {
                    contextButton.arrow.SetActive(false);
                }
            }
            ButtonDefault buttonDefault = button.GetComponent<ButtonDefault>();
            if (buttonDefault != null)
            {
                buttonDefault.SetTooltip(
                    mapped
                        ? ModLocalization.Get("context_menu.profile.tooltip_ready", "Show idol profile")
                        : ModLocalization.Get("context_menu.profile.tooltip_pending", "Show idol profile (data pending)"));
                buttonDefault.Activate(true, false);
            }
            SetButtonLabel(button, contextButton, buttonDefault, ModLocalization.Get("context_menu.profile.label", "Profile"));
            Button uiButton = button.GetComponent<Button>();
            if (uiButton == null)
            {
                uiButton = button.AddComponent<Button>();
            }
            uiButton.onClick = new Button.ButtonClickedEvent();
            StaffProfileButton handler = button.GetComponent<StaffProfileButton>();
            if (handler == null)
            {
                handler = button.AddComponent<StaffProfileButton>();
            }
            handler.SetStaff(staffer);
            uiButton.onClick.AddListener(handler.OnClick);
        }

        private static void DisableLocalization(GameObject root)
        {
            if (root == null)
            {
                return;
            }
            Lang_Button[] langButtons = root.GetComponentsInChildren<Lang_Button>(true);
            foreach (Lang_Button langButton in langButtons)
            {
                if (langButton == null)
                {
                    continue;
                }
                langButton.Constant = "";
                langButton.Tooltip = "";
                langButton.enabled = false;
            }
        }

        private static void SetButtonLabel(GameObject button, ContextMenuButton contextButton, ButtonDefault buttonDefault, string label)
        {
            if (!string.IsNullOrEmpty(label))
            {
                if (contextButton != null && contextButton.text != null)
                {
                    ExtensionMethods.SetText(contextButton.text, label);
                }
                if (buttonDefault != null && buttonDefault.Text != null)
                {
                    ExtensionMethods.SetText(buttonDefault.Text, label);
                }
                TextMeshProUGUI[] tmps = button.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (TextMeshProUGUI tmp in tmps)
                {
                    if (tmp != null)
                    {
                        tmp.text = label;
                    }
                }
                Text[] texts = button.GetComponentsInChildren<Text>(true);
                foreach (Text text in texts)
                {
                    if (text != null)
                    {
                        text.text = label;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(GirlButton_Graduated), nameof(GirlButton_Graduated.Set))]
    internal static class GirlButton_Graduated_Set_Patch
    {
        private static void Postfix(GirlButton_Graduated __instance, data_girls.girls _Girl)
        {
            if (__instance == null || _Girl == null)
            {
                return;
            }
            GraduationDetailsButton button = __instance.gameObject.GetComponent<GraduationDetailsButton>();
            if (button == null)
            {
                button = __instance.gameObject.AddComponent<GraduationDetailsButton>();
            }
            button.SetGirl(_Girl);
        }
    }

    [HarmonyPatch(typeof(ContextMenuController), nameof(ContextMenuController.Show), new Type[] { typeof(staff._staff) })]
    internal static class ContextMenuController_Show_Staff_Patch
    {
        private static void Postfix(ContextMenuController __instance, staff._staff _staff)
        {
            StaffProfileContextMenu.TryInject(__instance, _staff);
        }
    }

    [HarmonyPatch(typeof(ContextMenuController), nameof(ContextMenuController.Show), new Type[] { typeof(agency._room) })]
    internal static class ContextMenuController_Show_Room_Patch
    {
        private static void Postfix(ContextMenuController __instance, agency._room _room)
        {
            if (__instance == null)
            {
                return;
            }
            staff._staff staffer = __instance.Staff;
            if (staffer == null && _room != null)
            {
                staffer = _room.staffer;
            }
            StaffProfileContextMenu.TryInject(__instance, staffer);
        }
    }

    [HarmonyPatch(typeof(Profile_Popup), nameof(Profile_Popup.Set))]
    internal static class Profile_Popup_Set_Patch
    {
        private static void Prefix(Profile_Popup __instance, data_girls.girls _Girl)
        {
            if (_Girl == null || !GraduationDetailsState.IsFor(_Girl))
            {
                GraduationDetailsState.Clear();
            }
        }
    }

    [HarmonyPatch(typeof(Profile_Popup), "RenderHeader")]
    internal static class Profile_Popup_RenderHeader_Patch
    {
        private static void Postfix(Profile_Popup __instance)
        {
            if (__instance == null)
            {
                return;
            }
            if (!GraduationDetailsState.IsFor(__instance.Girl))
            {
                return;
            }
            GraduationSnapshot snapshot = GraduationSnapshotStore.GetSnapshot(__instance.Girl.id);
            if (snapshot == null)
            {
                return;
            }
            ApplySnapshotDob(__instance, snapshot);
            ApplySnapshotPortrait(__instance, snapshot);
        }

        private static void ApplySnapshotDob(Profile_Popup profile, GraduationSnapshot snapshot)
        {
            if (profile.Header_DateOfBirth == null)
            {
                return;
            }
            DateTime birthDate = profile.Girl != null ? profile.Girl.birthday : DateTime.MinValue;
            if (!string.IsNullOrEmpty(snapshot.Birthdate))
            {
                try
                {
                    birthDate = ExtensionMethods.ToDateTime(snapshot.Birthdate);
                }
                catch
                {
                    // Ignore parse errors; fall back to current data.
                }
            }
            int age = snapshot.AgeAtGraduation > 0 ? snapshot.AgeAtGraduation : (profile.Girl != null ? profile.Girl.GetAge() : 0);
            string dob = ExtensionMethods.ToString_Loc(birthDate, "DATETIME__BIRTHDAY");
            string ageText = Language.Insert("PROFILE__AGE", new string[]
            {
                age.ToString()
            });
            string text = Language.Data["PROFILE__BIRTHDATE"] + ": " + dob + " " + ageText;
            ExtensionMethods.SetText(profile.Header_DateOfBirth, text);
        }

        private static void ApplySnapshotPortrait(Profile_Popup profile, GraduationSnapshot snapshot)
        {
            string portraitPath = GraduationSnapshotStore.GetPortraitPath(snapshot);
            if (string.IsNullOrEmpty(portraitPath) || !File.Exists(portraitPath))
            {
                return;
            }
            Image portrait = profile.Portrait != null ? profile.Portrait.GetComponent<Image>() : null;
            Image shadow = profile.Portrait_Shadow != null ? profile.Portrait_Shadow.GetComponent<Image>() : null;
            if (portrait == null && shadow == null)
            {
                return;
            }
            string cacheKey = ("file://" + portraitPath).Replace("\\", "").Replace("/", "");
            Sprite cached = LoadTexture.GetSprite(cacheKey);
            if (cached != null)
            {
                if (portrait != null)
                {
                    portrait.sprite = cached;
                }
                if (shadow != null)
                {
                    shadow.sprite = cached;
                }
                return;
            }
            if (LoadTexture.instance != null)
            {
                if (portrait != null)
                {
                    LoadTexture.instance.StartCoroutine(LoadTexture.LoadSprite(portraitPath, portrait, null));
                }
                if (shadow != null)
                {
                    LoadTexture.instance.StartCoroutine(LoadTexture.LoadSprite(portraitPath, shadow, null));
                }
            }
        }
    }

    [HarmonyPatch(typeof(Profile_Popup), "RenderTab_Jobs")]
    internal static class Profile_Popup_RenderTab_Jobs_Patch
    {
        private static void Postfix(Profile_Popup __instance)
        {
            if (__instance == null)
            {
                return;
            }
            if (!GraduationDetailsState.IsFor(__instance.Girl))
            {
                SetActiveSafe(__instance.Jobs_Singles, true);
                SetActiveSafe(__instance.Jobs_Shows, true);
                SetActiveSafe(__instance.Jobs_Contracts, true);
                return;
            }

            data_girls.girls girl = __instance.Girl;
            if (girl == null)
            {
                return;
            }

            string earnings = ModLocalization.Get("jobs.total_earnings_prefix", "Total earnings: ")
                + ExtensionMethods.formatMoney(girl.GetTotalEarnings(), false, false, false);
            ExtensionMethods.SetText(__instance.Jobs_Salary, earnings);

            string singlesList = BuildReleasedSinglesList(girl);
            ExtensionMethods.SetText(__instance.Jobs_Earnings, Language.Data["SINGLES__PARTICIPATION"] + ":\n" + singlesList);

            ExtensionMethods.SetText(__instance.Jobs_Shows, BuildMarriageText(girl));
            ExtensionMethods.SetText(__instance.Jobs_Contracts, BuildCustodyText(girl));

            SetActiveSafe(__instance.Jobs_Singles, false);

            if (__instance.Jobs_Container != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.Jobs_Container.GetComponent<RectTransform>());
            }
        }

        private static void SetActiveSafe(GameObject obj, bool active)
        {
            if (obj != null)
            {
                obj.SetActive(active);
            }
        }

        private static string BuildReleasedSinglesList(data_girls.girls girl)
        {
            if (girl == null || singles.Singles == null)
            {
                return Language.Data["SSK__NA"];
            }
            List<string> lines = new List<string>();
            foreach (singles._single single in singles.Singles)
            {
                if (single == null || single.status != singles._single._status.released)
                {
                    continue;
                }
                if (single.girls == null || !single.girls.Contains(girl))
                {
                    continue;
                }
                string line = single.title;
                if (single.GetCenter() == girl)
                {
                    line += " (" + Language.Data["SINGLES__CENTER"] + ")";
                }
                lines.Add(line);
            }
            if (lines.Count == 0)
            {
                return Language.Data["SSK__NA"];
            }
            return string.Join("\n", lines.ToArray());
        }

        private static string FormatLocalizedValue(string templateOrPrefix, string value)
        {
            string safeTemplate = templateOrPrefix ?? string.Empty;
            string safeValue = value ?? string.Empty;
            if (safeTemplate.Contains("{0}"))
            {
                try
                {
                    return string.Format(safeTemplate, safeValue);
                }
                catch
                {
                }
            }
            return safeTemplate + safeValue;
        }

        private static string FormatLocalizedCount(string templateOrPrefix, int count, string legacySuffix)
        {
            string safeTemplate = templateOrPrefix ?? string.Empty;
            string countText = count.ToString();
            if (safeTemplate.Contains("{0}"))
            {
                try
                {
                    return string.Format(safeTemplate, countText);
                }
                catch
                {
                }
            }
            return safeTemplate + countText + legacySuffix + ")";
        }

        private static string BuildMarriageText(data_girls.girls girl)
        {
            MarriageRecord record = MarriageRecordStore.GetRecord(girl.id);
            if (record != null && record.MarriedToPlayer)
            {
                string name = record.PlayerName;
                if (string.IsNullOrEmpty(name))
                {
                    name = Language.Data["NOTIF__IDOL_REL_YOU"];
                }
                return FormatLocalizedValue(
                    ModLocalization.Get("jobs.married_to_prefix", "Married to {0}"),
                    name);
            }
            return ModLocalization.Get("jobs.married_to_none", "Married to: No");
        }

        private static string BuildCustodyText(data_girls.girls girl)
        {
            MarriageRecord record = MarriageRecordStore.GetRecord(girl.id);
            if (record == null)
            {
                return ModLocalization.Get("jobs.custody_unknown", "Custody: Unknown");
            }
            if (record.KidsCount < 0)
            {
                return ModLocalization.Get("jobs.custody_unknown", "Custody: Unknown");
            }
            int custodyCount = record.KidsCount;
            string suffix = custodyCount == 1
                ? ModLocalization.Get("jobs.custody_child_singular", " child")
                : ModLocalization.Get("jobs.custody_child_plural", " children");
            if (record.Custody == CustodyOwner.Player)
            {
                return FormatLocalizedCount(
                    ModLocalization.Get("jobs.custody_player_prefix", "Children Living With: Player ({0})"),
                    custodyCount,
                    suffix);
            }
            if (record.Custody == CustodyOwner.Idol)
            {
                return FormatLocalizedCount(
                    ModLocalization.Get("jobs.custody_idol_prefix", "Children Living With: Idol ({0})"),
                    custodyCount,
                    suffix);
            }
            if (record.Custody == CustodyOwner.None)
            {
                return ModLocalization.Get("jobs.custody_none", "Children: None");
            }
            return ModLocalization.Get("jobs.custody_unknown", "Children Living With: Unknown");
        }
    }

    [HarmonyPatch(typeof(Profile_Popup), "RenderTab_Fans")]
    internal static class Profile_Popup_RenderTab_Fans_Patch
    {
        private static void Postfix(Profile_Popup __instance)
        {
            if (__instance == null || !GraduationDetailsState.IsFor(__instance.Girl))
            {
                return;
            }
            GraduationSnapshot snapshot = GraduationSnapshotStore.GetSnapshot(__instance.Girl.id);
            if (!GraduationSnapshotStore.HasFanSnapshot(snapshot))
            {
                return;
            }
            TextMeshProUGUI text = __instance.Fans_Text_Total != null ? __instance.Fans_Text_Total.GetComponent<TextMeshProUGUI>() : null;
            if (text == null)
            {
                return;
            }
            long total = GraduationSnapshotStore.GetTotalFans(snapshot);
            text.text = Language.Data["TOTAL"] + ": " + ExtensionMethods.formatNumber(total, false, false);
        }
    }

    [HarmonyPatch(typeof(Profile_Fans_Pies), nameof(Profile_Fans_Pies.Render))]
    internal static class Profile_Fans_Pies_Render_Patch
    {
        private enum PieColor
        {
            Blue = 0,
            Green = 1,
            Gold = 2
        }

        private static bool Prefix(Profile_Fans_Pies __instance, data_girls.girls _Girl)
        {
            if (__instance == null || _Girl == null || !GraduationDetailsState.IsFor(_Girl))
            {
                return true;
            }
            GraduationSnapshot snapshot = GraduationSnapshotStore.GetSnapshot(_Girl.id);
            if (!GraduationSnapshotStore.HasFanSnapshot(snapshot))
            {
                return true;
            }
            RenderSnapshot(__instance, snapshot);
            return false;
        }

        private static void RenderSnapshot(Profile_Fans_Pies pies, GraduationSnapshot snapshot)
        {
            long total = GraduationSnapshotStore.GetTotalFans(snapshot);
            float male = 0.5f;
            float female = 0.5f;
            float casual = 0.5f;
            float hardcore = 0.5f;
            float teen = 0.33f;
            float youngAdult = 0.33f;
            float adult = 0.33f;
            if (total != 0L)
            {
                male = GraduationSnapshotStore.GetFanRatio(snapshot, resources.fanType.male);
                female = GraduationSnapshotStore.GetFanRatio(snapshot, resources.fanType.female);
                casual = GraduationSnapshotStore.GetFanRatio(snapshot, resources.fanType.casual);
                hardcore = GraduationSnapshotStore.GetFanRatio(snapshot, resources.fanType.hardcore);
                teen = GraduationSnapshotStore.GetFanRatio(snapshot, resources.fanType.teen);
                youngAdult = GraduationSnapshotStore.GetFanRatio(snapshot, resources.fanType.youngAdult);
                adult = GraduationSnapshotStore.GetFanRatio(snapshot, resources.fanType.adult);
            }

            if (male > female || male == female)
            {
                SetPie(pies.Fans_Pie_Male, male, PieColor.Green);
                SetPie(pies.Fans_Pie_Female, female, PieColor.Blue);
                SetValue(pies.Male, resources.fanType.male, male, PieColor.Green, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.male));
                SetValue(pies.Female, resources.fanType.female, female, PieColor.Blue, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.female));
            }
            else
            {
                SetPie(pies.Fans_Pie_Male, male, PieColor.Blue);
                SetPie(pies.Fans_Pie_Female, female, PieColor.Green);
                SetValue(pies.Male, resources.fanType.male, male, PieColor.Blue, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.male));
                SetValue(pies.Female, resources.fanType.female, female, PieColor.Green, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.female));
            }

            if (casual > hardcore || casual == hardcore)
            {
                SetPie(pies.Fans_Pie_Casual, casual, PieColor.Green);
                SetPie(pies.Fans_Pie_Hardcore, hardcore, PieColor.Blue);
                SetValue(pies.Casual, resources.fanType.casual, casual, PieColor.Green, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.casual));
                SetValue(pies.Hardcore, resources.fanType.hardcore, hardcore, PieColor.Blue, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.hardcore));
            }
            else
            {
                SetPie(pies.Fans_Pie_Casual, casual, PieColor.Blue);
                SetPie(pies.Fans_Pie_Hardcore, hardcore, PieColor.Green);
                SetValue(pies.Casual, resources.fanType.casual, casual, PieColor.Blue, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.casual));
                SetValue(pies.Hardcore, resources.fanType.hardcore, hardcore, PieColor.Green, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.hardcore));
            }

            PieColor teenColor = PieColor.Blue;
            PieColor yaColor = PieColor.Gold;
            PieColor adultColor = PieColor.Green;
            if (teen != youngAdult || teen != adult)
            {
                if (teen > youngAdult && teen > adult)
                {
                    teenColor = PieColor.Green;
                    if (youngAdult > adult)
                    {
                        yaColor = PieColor.Gold;
                        adultColor = PieColor.Blue;
                    }
                    else
                    {
                        yaColor = PieColor.Blue;
                        adultColor = PieColor.Gold;
                    }
                }
                else if (youngAdult > teen && youngAdult > adult)
                {
                    yaColor = PieColor.Green;
                    if (teen > adult)
                    {
                        teenColor = PieColor.Gold;
                        adultColor = PieColor.Blue;
                    }
                    else
                    {
                        teenColor = PieColor.Blue;
                        adultColor = PieColor.Gold;
                    }
                }
                else
                {
                    adultColor = PieColor.Green;
                    if (youngAdult > teen)
                    {
                        yaColor = PieColor.Gold;
                        teenColor = PieColor.Blue;
                    }
                    else
                    {
                        yaColor = PieColor.Blue;
                        teenColor = PieColor.Gold;
                    }
                }
            }
            SetPie(pies.Fans_Pie_Teen, teen, teenColor);
            SetPie(pies.Fans_Pie_YA, youngAdult, yaColor);
            SetPie(pies.Fans_Pie_Adult, adult + teen, adultColor);
            SetValue(pies.Teen, resources.fanType.teen, teen, teenColor, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.teen));
            SetValue(pies.YoungAdult, resources.fanType.youngAdult, youngAdult, yaColor, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.youngAdult));
            SetValue(pies.Adult, resources.fanType.adult, adult, adultColor, GraduationSnapshotStore.GetFanCount(snapshot, resources.fanType.adult));
        }

        private static Color32 GetPieColor(PieColor color)
        {
            switch (color)
            {
                case PieColor.Blue:
                    return mainScript.lightBlue32;
                case PieColor.Green:
                    return mainScript.green_light32;
                case PieColor.Gold:
                    return mainScript.gold32;
                default:
                    return mainScript.blue32;
            }
        }

        private static Color32 GetValueColor(PieColor color)
        {
            switch (color)
            {
                case PieColor.Blue:
                    return mainScript.blue32;
                case PieColor.Green:
                    return mainScript.green32;
                case PieColor.Gold:
                    return mainScript.gold32;
                default:
                    return mainScript.blue32;
            }
        }

        private static void SetPie(GameObject obj, float val, PieColor color)
        {
            if (obj == null)
            {
                return;
            }
            Image image = obj.GetComponent<Image>();
            if (image == null)
            {
                return;
            }
            image.fillAmount = val;
            image.color = GetPieColor(color);
        }

        private static void SetValue(GameObject obj, resources.fanType type, float ratio, PieColor color, long count)
        {
            if (obj == null)
            {
                return;
            }
            TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                return;
            }
            string value = ExtensionMethods.size(resources.GetFanTitle(type), 10) + "\n";
            value += ExtensionMethods.formatNumber(count, false, false);
            value = value + " [" + ExtensionMethods.toPercent(ratio) + "%]";
            text.text = value;
            text.color = GetValueColor(color);
        }
    }

    [HarmonyPatch(typeof(Profile_Fan), nameof(Profile_Fan.Set_Fans_Number))]
    internal static class Profile_Fan_Set_Fans_Number_Patch
    {
        private static bool Prefix(Profile_Fan __instance, data_girls.girls Girl, long Total)
        {
            if (__instance == null || Girl == null || !GraduationDetailsState.IsFor(Girl))
            {
                return true;
            }
            GraduationSnapshot snapshot = GraduationSnapshotStore.GetSnapshot(Girl.id);
            if (!GraduationSnapshotStore.HasFanSnapshot(snapshot))
            {
                return true;
            }
            long total = GraduationSnapshotStore.GetTotalFans(snapshot);
            long count = GraduationSnapshotStore.GetFanCount(snapshot, __instance.Gender, __instance.Hardcoreness, __instance.Age);
            float ratio = total > 0L ? (float)count / (float)total : 0f;
            Color32 barColor = count == 0L ? mainScript.lightBlue32 : mainScript.green_light32;
            Color32 textColor = count == 0L ? mainScript.blue32 : mainScript.green32;
            Image barImage = __instance.Bar != null ? __instance.Bar.GetComponent<Image>() : null;
            if (barImage != null)
            {
                barImage.color = barColor;
            }
            TextMeshProUGUI value = __instance.Value != null ? __instance.Value.GetComponent<TextMeshProUGUI>() : null;
            if (value != null)
            {
                value.color = textColor;
                value.text = ExtensionMethods.formatNumber(count, false, true) + " [" + ExtensionMethods.toPercent(ratio) + "%]";
            }
            RectTransform barTransform = __instance.Bar != null ? __instance.Bar.GetComponent<RectTransform>() : null;
            if (barTransform != null)
            {
                barTransform.localScale = new Vector2(1f, ratio);
            }
            Image portrait = __instance.Portrait != null ? __instance.Portrait.GetComponent<Image>() : null;
            if (portrait != null)
            {
                portrait.fillAmount = 0.97f;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(Profile_Fan), nameof(Profile_Fan.Set_Appeal))]
    internal static class Profile_Fan_Set_Appeal_Patch
    {
        private static bool Prefix(Profile_Fan __instance, data_girls.girls Girl)
        {
            if (__instance == null || Girl == null || !GraduationDetailsState.IsFor(Girl))
            {
                return true;
            }
            GraduationSnapshot snapshot = GraduationSnapshotStore.GetSnapshot(Girl.id);
            if (!GraduationSnapshotStore.HasFanSnapshot(snapshot))
            {
                return true;
            }
            float val = GraduationSnapshotStore.GetFanAppeal(snapshot, __instance.Gender, __instance.Hardcoreness, __instance.Age);
            Color32 barColor = mainScript.lightBlue32;
            Color32 textColor = mainScript.blue32;
            if (val > 0.5f)
            {
                barColor = mainScript.green_light32;
                textColor = mainScript.green32;
            }
            else if (val < 0.25f)
            {
                barColor = mainScript.red_light32;
                textColor = mainScript.red32;
            }
            Image barImage = __instance.Bar != null ? __instance.Bar.GetComponent<Image>() : null;
            if (barImage != null)
            {
                barImage.color = barColor;
            }
            TextMeshProUGUI value = __instance.Value != null ? __instance.Value.GetComponent<TextMeshProUGUI>() : null;
            if (value != null)
            {
                value.color = textColor;
                value.text = ExtensionMethods.toPercent(val) + "%";
            }
            RectTransform barTransform = __instance.Bar != null ? __instance.Bar.GetComponent<RectTransform>() : null;
            if (barTransform != null)
            {
                barTransform.localScale = new Vector2(1f, val);
            }
            Image portrait = __instance.Portrait != null ? __instance.Portrait.GetComponent<Image>() : null;
            if (portrait != null)
            {
                portrait.fillAmount = val;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(Profile_Fan), nameof(Profile_Fan.Set_Opinion))]
    internal static class Profile_Fan_Set_Opinion_Patch
    {
        private static bool Prefix(Profile_Fan __instance, data_girls.girls Girl)
        {
            if (__instance == null || Girl == null || !GraduationDetailsState.IsFor(Girl))
            {
                return true;
            }
            GraduationSnapshot snapshot = GraduationSnapshotStore.GetSnapshot(Girl.id);
            if (!GraduationSnapshotStore.HasFanSnapshot(snapshot))
            {
                return true;
            }
            float val = GraduationSnapshotStore.GetFanOpinion(snapshot, __instance.Gender, __instance.Hardcoreness, __instance.Age);
            Color32 barColor = mainScript.lightBlue32;
            Color32 textColor = mainScript.blue32;
            if (val > 0.75f)
            {
                barColor = mainScript.green_light32;
                textColor = mainScript.green32;
            }
            else if (val < 0.5f)
            {
                barColor = mainScript.red_light32;
                textColor = mainScript.red32;
            }
            Image barImage = __instance.Bar != null ? __instance.Bar.GetComponent<Image>() : null;
            if (barImage != null)
            {
                barImage.color = barColor;
            }
            RectTransform barTransform = __instance.Bar != null ? __instance.Bar.GetComponent<RectTransform>() : null;
            if (barTransform != null)
            {
                barTransform.localScale = new Vector2(1f, val);
            }
            Image portrait = __instance.Portrait != null ? __instance.Portrait.GetComponent<Image>() : null;
            if (portrait != null)
            {
                portrait.fillAmount = val;
            }
            float scaled = val * 200f - 100f;
            TextMeshProUGUI value = __instance.Value != null ? __instance.Value.GetComponent<TextMeshProUGUI>() : null;
            if (value != null)
            {
                value.color = textColor;
                value.text = Mathf.RoundToInt(scaled) + "%";
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(Profile_Popup), "RenderTab_Bonds")]
    internal static class Profile_Popup_RenderTab_Bonds_Patch
    {
        private static bool Prefix(Profile_Popup __instance)
        {
            if (__instance == null || !GraduationDetailsState.IsFor(__instance.Girl))
            {
                return true;
            }
            GraduationSnapshot snapshot = GraduationSnapshotStore.GetSnapshot(__instance.Girl.id);
            if (snapshot == null)
            {
                return true;
            }
            RenderSnapshot(__instance, snapshot);
            return false;
        }

        private static void RenderSnapshot(Profile_Popup profile, GraduationSnapshot snapshot)
        {
            if (profile == null || profile.Bonds_Container == null)
            {
                return;
            }
            ExtensionMethods.destroyChildren(profile.Bonds_Container.transform);
            bool rendered = false;
            if (snapshot.Bonds != null)
            {
                foreach (BondSectionSnapshot section in snapshot.Bonds)
                {
                    if (section == null)
                    {
                        continue;
                    }
                    if (section.Type == BondSectionType.NoInfo)
                    {
                        continue;
                    }
                    string title = GetSectionTitle(section);
                    if (!string.IsNullOrEmpty(title))
                    {
                        AddTitle(profile, title);
                    }
                    if (section.Entries != null && section.Entries.Count > 0)
                    {
                        RenderEntries(profile, profile.Girl, section.Entries);
                        rendered = true;
                    }
                    else
                    {
                        rendered = true;
                    }
                }
            }
            if (!rendered)
            {
                AddTitle(profile, Language.Data["PROFILE__NO_INFO"]);
            }
        }

        private static string GetSectionTitle(BondSectionSnapshot section)
        {
            switch (section.Type)
            {
                case BondSectionType.CliqueKnown:
                {
                    data_girls.girls leader = data_girls.GetGirlByID(section.LeaderId);
                    if (leader == null)
                    {
                        return Language.Data["PROFILE__UNKNOWN_CLIQUE"];
                    }
                    return Language.Insert("PROFILE__CLIQUE", new string[]
                    {
                        leader.GetName(true)
                    });
                }
                case BondSectionType.CliqueUnknown:
                    return Language.Data["PROFILE__UNKNOWN_CLIQUE"];
                case BondSectionType.Bullies:
                    return Language.Data["PROFILE__BULLIES"];
                case BondSectionType.BulliedBy:
                    return Language.Data["PROFILE__BULLIED_BY"];
                case BondSectionType.BestFriends:
                    return Language.Data["PROFILE__BEST_FRIENDS"];
                case BondSectionType.Friends:
                    return Language.Data["PROFILE__FRIENDS"];
                case BondSectionType.Dislikes:
                    return Language.Data["PROFILE__DISLIKES"];
                case BondSectionType.Hates:
                    return Language.Data["PROFILE__HATES"];
                case BondSectionType.NoInfo:
                    return Language.Data["PROFILE__NO_INFO"];
                default:
                    return "";
            }
        }

        private static void AddTitle(Profile_Popup profile, string text)
        {
            if (profile.prefab_Bonds_Title == null)
            {
                return;
            }
            GameObject obj = UnityEngine.Object.Instantiate(profile.prefab_Bonds_Title);
            ExtensionMethods.SetText(obj, text);
            obj.transform.SetParent(profile.Bonds_Container.transform, false);
        }

        private static void RenderEntries(Profile_Popup profile, data_girls.girls parent, List<BondEntry> entries)
        {
            if (profile.prefab_Bonds_Container == null || profile.prefab_Bonds_Girl == null || entries == null || entries.Count == 0)
            {
                return;
            }
            int count = 0;
            GameObject container = UnityEngine.Object.Instantiate(profile.prefab_Bonds_Container);
            foreach (BondEntry entry in entries)
            {
                GameObject item = UnityEngine.Object.Instantiate(profile.prefab_Bonds_Girl);
                Profile_Bond bond = item.GetComponent<Profile_Bond>();
                if (bond != null && entry != null && entry.Known && entry.GirlId >= 0)
                {
                    data_girls.girls other = data_girls.GetGirlByID(entry.GirlId);
                    if (other != null)
                    {
                        ApplyBond(bond, other, parent, entry);
                    }
                    else
                    {
                        bond.Set_Unknown();
                    }
                }
                else if (bond != null)
                {
                    bond.Set_Unknown();
                }
                item.transform.SetParent(container.transform, false);
                count++;
                if (count == 3)
                {
                    count = 0;
                    container.transform.SetParent(profile.Bonds_Container.transform, false);
                    container = UnityEngine.Object.Instantiate(profile.prefab_Bonds_Container);
                }
            }
            if (count > 0)
            {
                container.transform.SetParent(profile.Bonds_Container.transform, false);
            }
        }

        private static void ApplyBond(Profile_Bond bond, data_girls.girls girl, data_girls.girls parent, BondEntry entry)
        {
            if (bond == null || girl == null || parent == null)
            {
                return;
            }
            bond.Unknown.SetActive(false);
            bond.Portrait.SetActive(true);
            bond.Name.SetActive(true);
            bond.RelationshipBar.SetActive(true);
            bond.Girl = girl;
            bond.Parent = parent;
            GirlProfileOnHover hover = bond.GetComponent<GirlProfileOnHover>();
            if (hover != null)
            {
                hover.Set(girl, false);
            }
            Image portrait = bond.Portrait != null ? bond.Portrait.GetComponent<Image>() : null;
            if (portrait != null && girl.texture != null)
            {
                portrait.sprite = girl.texture.middle;
            }
            ExtensionMethods.SetText(bond.Name, girl.GetName(true));

            float ratio = entry != null ? Mathf.Clamp01(entry.RelationshipRatio) : 0.5f;
            RectTransform bar = bond.RelationshipBar != null ? bond.RelationshipBar.GetComponent<RectTransform>() : null;
            if (bar != null)
            {
                bar.localScale = new Vector2(ratio, 1f);
            }
            Image barImage = bond.RelationshipBar != null ? bond.RelationshipBar.GetComponent<Image>() : null;
            if (barImage != null)
            {
                Color32 color = mainScript.lightBlue32;
                if (entry != null && entry.IsDatingKnown)
                {
                    color = mainScript.pink32;
                }
                else
                {
                    Relationships._relationship._status status = StatusFromRatio(ratio);
                    if (status == Relationships._relationship._status.best_friends || status == Relationships._relationship._status.friends)
                    {
                        color = mainScript.green32;
                    }
                    else if (status == Relationships._relationship._status.dislikes)
                    {
                        color = mainScript.red32;
                    }
                    else if (status == Relationships._relationship._status.hates)
                    {
                        color = mainScript.black32;
                    }
                }
                barImage.color = color;
            }
        }

        private static Relationships._relationship._status StatusFromRatio(float ratio)
        {
            if (ratio > 0.9f)
            {
                return Relationships._relationship._status.best_friends;
            }
            if (ratio > 0.7f)
            {
                return Relationships._relationship._status.friends;
            }
            if (ratio < 0.2f)
            {
                return Relationships._relationship._status.hates;
            }
            if (ratio < 0.4f)
            {
                return Relationships._relationship._status.dislikes;
            }
            return Relationships._relationship._status.normal;
        }
    }

    [HarmonyPatch(typeof(PopupManager), "Close", new Type[] { typeof(Action) })]
    internal static class PopupManager_Close_Patch
    {
        private static void Postfix(PopupManager __instance)
        {
            // Only clear when profile popup is no longer open. Other popup closes (tooltips, etc.)
            // should not disable graduation snapshot rendering while profile is active.
            if (__instance != null)
            {
                PopupManager._popup profilePopup = __instance.GetByType(PopupManager._type.girl_profile);
                if (profilePopup != null && profilePopup.open)
                {
                    return;
                }
            }
            GraduationDetailsState.Clear();
        }
    }

    [HarmonyPatch(typeof(SaveManager), "Awake")]
    internal static class SaveManager_Awake_GraduationDetailsPersistenceRunner_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SaveManager __instance)
        {
            if (__instance != null)
            {
                GraduationDetailsPersistenceRunner.Ensure(__instance.gameObject);
            }
        }
    }

    [HarmonyPatch(typeof(PopupManager), "Start")]
    internal static class PopupManager_Start_GraduationDetailsPersistenceRunner_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(PopupManager __instance)
        {
            if (__instance != null)
            {
                GraduationDetailsPersistenceRunner.Ensure(__instance.gameObject);
            }
        }
    }

    /// <summary>
    /// Injects save-scope capture into vanilla's non-generic SavedData writer call sites.
    /// Patching a constructed DataSaver generic is unsafe on Mono because reference-type
    /// instantiations share native code with GlobalData.
    /// </summary>
    [HarmonyPatch]
    internal static class VanillaSavedDataWrite_GraduationDetails_Patch
    {
        private const string DataSaverSaveMethodName = "saveData";

        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return RequireMethod(
                typeof(SaveManager),
                nameof(SaveManager.SaveData),
                new Type[] { typeof(bool), typeof(bool) });
            yield return RequireMethod(
                typeof(SaveManager),
                nameof(SaveManager.SaveChapter),
                new Type[] { typeof(tasks._chapter) });
            yield return RequireMethod(
                typeof(Popup_Save),
                "Save",
                Type.EmptyTypes);
            yield return RequireMethod(
                typeof(Popup_Load_Story),
                "Do_Overwrite_Save",
                new Type[] { typeof(Popup_Load_Story.save_info) });
            yield return RequireMethod(
                typeof(Popup_Load_Story),
                nameof(Popup_Load_Story.Do_New_Save),
                new Type[] { typeof(string) });
        }

        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            LocalBuilder dataToSaveLocal = generator.DeclareLocal(
                typeof(SaveManager.SavedData));
            LocalBuilder dataFileNameLocal = generator.DeclareLocal(typeof(string));
            LocalBuilder isJsonLocal = generator.DeclareLocal(typeof(bool));
            LocalBuilder fullPathLocal = generator.DeclareLocal(typeof(bool));
            LocalBuilder transactionIdLocal = generator.DeclareLocal(typeof(string));
            MethodInfo prepareMethod = AccessTools.Method(
                typeof(GraduationDetailsPersistence),
                nameof(GraduationDetailsPersistence.PrepareVanillaSave),
                new Type[]
                {
                    typeof(SaveManager.SavedData),
                    typeof(string),
                    typeof(bool),
                    typeof(bool)
                });
            MethodInfo scheduledMethod = AccessTools.Method(
                typeof(GraduationDetailsPersistence),
                nameof(GraduationDetailsPersistence.OnVanillaSaveScheduled),
                new Type[] { typeof(string) });
            int injectedWriteCount = 0;

            foreach (CodeInstruction instruction in codes)
            {
                if (!IsSavedDataWrite(instruction))
                {
                    yield return instruction;
                    continue;
                }

                // Stage sidecars and the expected vanilla payload before vanilla starts its
                // background writer. The original four arguments and call remain unchanged.
                CodeInstruction firstInjectedInstruction =
                    new CodeInstruction(OpCodes.Stloc, fullPathLocal);
                firstInjectedInstruction.labels.AddRange(instruction.labels);
                firstInjectedInstruction.blocks.AddRange(instruction.blocks);
                instruction.labels.Clear();
                instruction.blocks.Clear();

                yield return firstInjectedInstruction;
                yield return new CodeInstruction(OpCodes.Stloc, isJsonLocal);
                yield return new CodeInstruction(OpCodes.Stloc, dataFileNameLocal);
                yield return new CodeInstruction(OpCodes.Stloc, dataToSaveLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, dataToSaveLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, dataFileNameLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, isJsonLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, fullPathLocal);
                yield return new CodeInstruction(OpCodes.Call, prepareMethod);
                yield return new CodeInstruction(OpCodes.Stloc, transactionIdLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, dataToSaveLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, dataFileNameLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, isJsonLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, fullPathLocal);
                yield return instruction;
                yield return new CodeInstruction(OpCodes.Ldloc, transactionIdLocal);
                yield return new CodeInstruction(OpCodes.Call, scheduledMethod);
                injectedWriteCount++;
            }

            if (injectedWriteCount != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one SavedData write in " +
                    (__originalMethod == null
                        ? "an unknown vanilla save caller."
                        : __originalMethod.DeclaringType.FullName +
                          "." +
                          __originalMethod.Name +
                          "."));
            }
        }

        private static bool IsSavedDataWrite(CodeInstruction instruction)
        {
            MethodInfo calledMethod = instruction.operand as MethodInfo;
            if (calledMethod == null
                || calledMethod.DeclaringType != typeof(DataSaver)
                || !string.Equals(
                    calledMethod.Name,
                    DataSaverSaveMethodName,
                    StringComparison.Ordinal)
                || !calledMethod.IsGenericMethod)
            {
                return false;
            }

            Type[] genericArguments = calledMethod.GetGenericArguments();
            return genericArguments.Length == 1
                && genericArguments[0] == typeof(SaveManager.SavedData);
        }

        private static MethodBase RequireMethod(
            Type declaringType,
            string methodName,
            Type[] parameterTypes)
        {
            MethodInfo method = AccessTools.Method(
                declaringType,
                methodName,
                parameterTypes);
            if (method == null)
            {
                throw new MissingMethodException(declaringType.FullName, methodName);
            }
            return method;
        }
    }

    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.GetLatestAutosavePath))]
    internal static class SaveManager_GetLatestAutosavePath_GraduationDetails_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(string __result)
        {
            if (!GraduationDetailsPersistence.LoadInProgress)
            {
                return;
            }

            // GetLatestAutosavePath deserializes every candidate before returning the path that
            // LoadData(bool) actually selects. Its result is authoritative over those previews.
            GraduationDetailsPersistence.CaptureLatestAutosavePath(
                GraduationDetailsPaths.ResolveDataSaverLoadPath(__result));
        }
    }

    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.LoadData), new Type[] { typeof(string) })]
    internal static class SaveManager_LoadDataPath_GraduationDetails_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(string path)
        {
            GraduationDetailsPersistence.BeginLoad();
            GraduationDetailsPersistence.CaptureLoadPath(
                GraduationDetailsPaths.ResolveDataSaverLoadPath(path));
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SaveManager __instance)
        {
            GraduationDetailsPersistence.CompleteLoad(
                __instance != null && __instance.Data != null);
        }

        [HarmonyFinalizer]
        private static void Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                GraduationDetailsPersistence.CancelLoad();
            }
        }
    }

    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.LoadData), new Type[] { typeof(bool) })]
    internal static class SaveManager_LoadDataFlag_GraduationDetails_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(bool autoSave)
        {
            GraduationDetailsPersistence.BeginLoad(autoSave);
            if (!autoSave)
            {
                GraduationDetailsPersistence.CaptureLoadPath(
                    GraduationDetailsPaths.ResolveDataSaverLoadPath(
                        GraduationDetailsPaths.GetQuickSaveDataFileName(false)));
            }
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SaveManager __instance)
        {
            GraduationDetailsPersistence.CompleteLoad(
                __instance != null && __instance.Data != null);
        }

        [HarmonyFinalizer]
        private static void Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                GraduationDetailsPersistence.CancelLoad();
            }
        }
    }

    /// <summary>
    /// Vanilla creates a relationship before resolving its saved girl IDs, then removes the
    /// relationship in CheckForCopies when either idol no longer exists. Some Recalc postfixes
    /// assume both resolved entries are non-null and throw before vanilla reaches that cleanup.
    /// Guard only the Recalc call inside the relationship-load overload; valid relationships
    /// still execute vanilla Recalc and every patch attached to it.
    /// </summary>
    [HarmonyPatch]
    internal static class Relationships_LoadDataRelationships_GraduationDetails_Patch
    {
        private const string LoadDataMethodName = "LoadData";

        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Relationships),
                LoadDataMethodName,
                new Type[] { typeof(List<Relationships.SaveData_Rel>) });
            if (method == null)
            {
                throw new MissingMethodException(
                    typeof(Relationships).FullName,
                    LoadDataMethodName);
            }
            return method;
        }

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            MethodInfo recalcMethod = AccessTools.Method(
                typeof(Relationships._relationship),
                nameof(Relationships._relationship.Recalc),
                new Type[] { typeof(bool) });
            MethodInfo guardedRecalcMethod = AccessTools.Method(
                typeof(Relationships_LoadDataRelationships_GraduationDetails_Patch),
                nameof(RecalcIfRelationshipMembersResolve),
                new Type[] { typeof(Relationships._relationship), typeof(bool) });

            int replacementCount = 0;
            foreach (CodeInstruction instruction in codes)
            {
                if (Equals(instruction.operand, recalcMethod))
                {
                    // Mutate the original instruction so all labels and exception blocks stay
                    // attached to the same point in the method body.
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = guardedRecalcMethod;
                    replacementCount++;
                }
            }

            if (replacementCount != 1)
            {
                throw new InvalidProgramException(
                    "Expected exactly one relationship Recalc call in Relationships.LoadData, found "
                    + replacementCount);
            }
            return codes;
        }

        private static void RecalcIfRelationshipMembersResolve(
            Relationships._relationship relationship,
            bool updateStatus)
        {
            if (relationship == null ||
                relationship.Girls == null ||
                relationship.Girls.Count < 2 ||
                relationship.Girls[0] == null ||
                relationship.Girls[1] == null)
            {
                return;
            }

            relationship.Recalc(updateStatus);
        }
    }

    [HarmonyPatch(typeof(MainMenu_LoadGameManager), nameof(MainMenu_LoadGameManager.StartNewGame))]
    internal static class MainMenu_LoadGameManager_StartNewGame_GraduationDetails_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix()
        {
            GraduationDetailsPersistence.ResetForNewGame();
        }
    }

    [HarmonyPatch(typeof(Dating), nameof(Dating.Marriage_Girl_Quits))]
    internal static class Dating_Marriage_Girl_Quits_Patch
    {
        private static void Prefix(data_girls.girls Girl)
        {
            MarriageContext.Begin(Girl);
        }

        private static void Postfix()
        {
            MarriageContext.SaveAndClear();
        }
    }

    [HarmonyPatch(typeof(Dating), "Marriage_Player_Quits")]
    internal static class Dating_Marriage_Player_Quits_Patch
    {
        private static void Prefix()
        {
            MarriageContext.Begin(Dating.GetWife());
        }

        private static void Postfix()
        {
            MarriageContext.SaveAndClear();
        }
    }

    [HarmonyPatch(typeof(Dating), "M_get_number_of_kids")]
    internal static class Dating_M_get_number_of_kids_Patch
    {
        private static void Postfix(int __result)
        {
            if (MarriageContext.Active)
            {
                MarriageContext.SetKids(__result);
            }
        }
    }

    [HarmonyPatch(typeof(Dating), "M_get_custody_string")]
    internal static class Dating_M_get_custody_string_Patch
    {
        private static void Postfix(string __result, bool good_outcome, int number_of_kids)
        {
            if (MarriageContext.Active)
            {
                MarriageContext.SetCustody(__result, good_outcome, number_of_kids);
            }
        }
    }

    [HarmonyPatch(typeof(Date_Graduation), nameof(Date_Graduation.Hire_As_Staffer))]
    internal static class Date_Graduation_Hire_As_Staffer_Patch
    {
        private static void Prefix(data_girls.girls Girl)
        {
            if (Girl == null)
            {
                return;
            }
            StaffHireContext.Begin(Girl);
            GraduationSnapshotStore.Capture(Girl);
        }

        private static void Postfix()
        {
            if (StaffHireContext.Active)
            {
                StaffHireContext.Clear();
            }
        }
    }

    [HarmonyPatch(typeof(staff), nameof(staff.Hire))]
    internal static class staff_Hire_Patch
    {
        private static void Postfix(staff._staff Staffer)
        {
            if (!StaffHireContext.Active)
            {
                return;
            }
            if (StaffHireContext.GirlId >= 0)
            {
                data_girls.girls girl = data_girls.GetGirlByID(StaffHireContext.GirlId);
                if (girl != null && GraduationSnapshotStore.GetSnapshot(girl.id) == null)
                {
                    GraduationSnapshotStore.Capture(girl);
                }
            }
            StaffHireContext.Complete(Staffer);
        }
    }

    [HarmonyPatch(typeof(staff), nameof(staff.LoadFunction))]
    internal static class staff_LoadFunction_Patch
    {
        private static void Postfix()
        {
            if (GraduationDetailsPersistence.LoadInProgress)
            {
                return;
            }
            StaffIdolStore.BackfillFromStaff();
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Graduate))]
    internal static class data_girls_girls_Graduate_Patch
    {
        private static void Prefix(data_girls.girls __instance)
        {
            GraduationSnapshotStore.Capture(__instance);
        }
    }

    [HarmonyPatch(typeof(data_girls), nameof(data_girls.LoadFunction))]
    internal static class data_girls_LoadFunction_Patch
    {
        private static void Postfix()
        {
            if (GraduationDetailsPersistence.LoadInProgress)
            {
                return;
            }
            GraduationSnapshotStore.Backfill(data_girls.girl);
        }
    }
}
