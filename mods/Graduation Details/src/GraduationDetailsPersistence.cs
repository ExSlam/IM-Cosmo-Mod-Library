using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GraduationDetails
{
    /// <summary>
    /// One exact vanilla save target and the files owned by Graduation Details for it.
    /// </summary>
    internal sealed class GraduationDetailsSaveScope
    {
        internal string SaveFilePath = "";
        internal string RelativeSavePath = "";
        internal string SidecarFilePath = "";
        internal string PortraitDirectoryPath = "";
        internal string PersistentDataRootPath = "";
        internal string RootDirectoryPath = "";
        internal bool IsTransient;
    }

    /// <summary>
    /// Resolves vanilla's exact save paths into a sibling GraduationDetails tree.
    /// No method in this type reads or writes a vanilla file.
    /// </summary>
    internal static class GraduationDetailsPaths
    {
        private const string GameDataRootFolderName = "data";
        private const string GraduationDetailsRootFolderName = "GraduationDetails";
        private const string SaveFileName = "save.json";
        private const string AutoSaveFileName = "auto_save.json";
        private const string ManualSaveFileName = "manual_save.json";
        private const string GlobalDataFileName = "global_data.json";
        private const string JsonFileExtension = ".json";
        private const string ManualSavesFolderName = "manual_saves";
        private const string StoryModeFolderName = "story_mode";
        private const string StoryChapterFolderPrefix = "chapter_";
        private const string PortraitDirectorySuffix = ".portraits";
        private const int FirstStoryChapterIndex = 0;
        private const int LastStoryChapterIndex = 6;

        internal static string GetVanillaDataRootDirectory()
        {
            return GetVanillaDataRootDirectory(Application.persistentDataPath);
        }

        internal static string GetVanillaDataRootDirectory(string persistentDataRoot)
        {
            string normalizedRoot;
            if (!TryNormalizeDirectoryPath(persistentDataRoot, out normalizedRoot))
            {
                return "";
            }
            return NormalizeDirectoryPathOrEmpty(
                Path.Combine(normalizedRoot, GameDataRootFolderName));
        }

        internal static string GetRootDirectory()
        {
            return GetRootDirectory(Application.persistentDataPath);
        }

        internal static string GetRootDirectory(string persistentDataRoot)
        {
            string normalizedRoot;
            if (!TryNormalizeDirectoryPath(persistentDataRoot, out normalizedRoot))
            {
                return "";
            }
            return NormalizeDirectoryPathOrEmpty(
                Path.Combine(normalizedRoot, GraduationDetailsRootFolderName));
        }

        internal static bool TryResolveDataSaverPath(
            string dataFileName,
            bool isJson,
            bool fullPath,
            out string resolvedPath)
        {
            return TryResolveDataSaverPath(
                Application.persistentDataPath,
                dataFileName,
                isJson,
                fullPath,
                out resolvedPath);
        }

        internal static bool TryResolveDataSaverPath(
            string persistentDataRoot,
            string dataFileName,
            bool isJson,
            bool fullPath,
            out string resolvedPath)
        {
            resolvedPath = "";
            if (string.IsNullOrWhiteSpace(dataFileName))
            {
                return false;
            }

            try
            {
                // DataSaver uses isJson only for serialization. It always appends .json
                // when fullPath is false and otherwise uses the supplied path verbatim.
                string candidatePath = dataFileName;
                if (!fullPath)
                {
                    string dataRoot = GetVanillaDataRootDirectory(persistentDataRoot);
                    if (string.IsNullOrEmpty(dataRoot))
                    {
                        return false;
                    }
                    candidatePath = Path.Combine(
                        dataRoot,
                        dataFileName + JsonFileExtension);
                }

                GraduationDetailsSaveScope scope;
                if (!TryResolveSaveScope(
                        persistentDataRoot,
                        candidatePath,
                        out scope))
                {
                    return false;
                }
                resolvedPath = scope.SaveFilePath;
                return true;
            }
            catch
            {
                resolvedPath = "";
                return false;
            }
        }

        internal static bool TryResolveDataSaverLoadPath(
            string dataFileName,
            out string resolvedPath)
        {
            return TryResolveDataSaverLoadPath(
                Application.persistentDataPath,
                dataFileName,
                out resolvedPath);
        }

        internal static bool TryResolveDataSaverLoadPath(
            string persistentDataRoot,
            string dataFileName,
            out string resolvedPath)
        {
            resolvedPath = "";
            if (string.IsNullOrWhiteSpace(dataFileName))
            {
                return false;
            }

            try
            {
                // Match DataSaver.loadData: combine with data, append .json, then
                // collapse its literal duplicate-extension token.
                string dataRoot = GetVanillaDataRootDirectory(persistentDataRoot);
                if (string.IsNullOrEmpty(dataRoot))
                {
                    return false;
                }
                string candidatePath = Path.Combine(
                        dataRoot,
                        dataFileName + JsonFileExtension)
                    .Replace(
                        JsonFileExtension + JsonFileExtension,
                        JsonFileExtension);

                GraduationDetailsSaveScope scope;
                if (!TryResolveSaveScope(
                        persistentDataRoot,
                        candidatePath,
                        out scope))
                {
                    return false;
                }
                resolvedPath = scope.SaveFilePath;
                return true;
            }
            catch
            {
                resolvedPath = "";
                return false;
            }
        }

        internal static bool TryResolveSaveScope(
            string saveFilePath,
            out GraduationDetailsSaveScope scope)
        {
            return TryResolveSaveScope(
                Application.persistentDataPath,
                saveFilePath,
                out scope);
        }

        internal static bool TryResolveSaveScope(
            string persistentDataRoot,
            string saveFilePath,
            out GraduationDetailsSaveScope scope)
        {
            scope = null;
            if (string.IsNullOrWhiteSpace(saveFilePath) ||
                HasNavigationPathSegment(saveFilePath))
            {
                return false;
            }

            string normalizedPersistentRoot;
            if (!TryNormalizeDirectoryPath(
                    persistentDataRoot,
                    out normalizedPersistentRoot))
            {
                return false;
            }
            string dataRoot = GetVanillaDataRootDirectory(normalizedPersistentRoot);
            string detailsRoot = GetRootDirectory(normalizedPersistentRoot);
            if (string.IsNullOrEmpty(dataRoot) ||
                string.IsNullOrEmpty(detailsRoot) ||
                string.Equals(
                    dataRoot,
                    detailsRoot,
                    StringComparison.OrdinalIgnoreCase) ||
                IsStrictlyContainedPath(dataRoot, detailsRoot) ||
                IsStrictlyContainedPath(detailsRoot, dataRoot))
            {
                return false;
            }

            string normalizedSavePath;
            try
            {
                normalizedSavePath = Path.GetFullPath(
                    Path.IsPathRooted(saveFilePath)
                        ? saveFilePath
                        : Path.Combine(dataRoot, saveFilePath));
            }
            catch
            {
                return false;
            }

            if (!IsStrictlyContainedPath(dataRoot, normalizedSavePath))
            {
                return false;
            }
            string dataPrefix = BuildDirectoryPrefix(dataRoot);
            string relativePath = normalizedSavePath.Substring(dataPrefix.Length);
            string normalizedRelativePath;
            if (!TryNormalizeSupportedRelativeSavePath(
                    relativePath,
                    out normalizedRelativePath))
            {
                return false;
            }

            string validatedVanillaPath;
            if (!GraduationDetailsPersistenceIO.TryValidatePathUnderRoot(
                    dataRoot,
                    normalizedSavePath,
                    false,
                    out validatedVanillaPath))
            {
                return false;
            }

            string sidecarPath;
            if (!GraduationDetailsPersistenceIO.TryGetContainedPath(
                    detailsRoot,
                    normalizedRelativePath,
                    true,
                    out sidecarPath))
            {
                return false;
            }

            string relativeDirectory = Path.GetDirectoryName(
                normalizedRelativePath) ?? "";
            string portraitDirectoryName =
                Path.GetFileNameWithoutExtension(normalizedRelativePath) +
                PortraitDirectorySuffix;
            string portraitRelativePath = string.IsNullOrEmpty(relativeDirectory)
                ? portraitDirectoryName
                : Path.Combine(relativeDirectory, portraitDirectoryName);
            string portraitDirectoryPath;
            if (!GraduationDetailsPersistenceIO.TryGetContainedPath(
                    detailsRoot,
                    portraitRelativePath,
                    true,
                    out portraitDirectoryPath))
            {
                return false;
            }

            scope = new GraduationDetailsSaveScope
            {
                SaveFilePath = validatedVanillaPath,
                RelativeSavePath = normalizedRelativePath,
                SidecarFilePath = sidecarPath,
                PortraitDirectoryPath = portraitDirectoryPath,
                PersistentDataRootPath = normalizedPersistentRoot,
                RootDirectoryPath = detailsRoot,
                IsTransient = false
            };
            return true;
        }

        internal static bool TryGetVanillaSaveRelativePath(
            string saveFilePath,
            out string relativeSavePath)
        {
            relativeSavePath = "";
            GraduationDetailsSaveScope scope;
            if (!TryResolveSaveScope(saveFilePath, out scope))
            {
                return false;
            }
            relativeSavePath = scope.RelativeSavePath;
            return true;
        }

        internal static GraduationDetailsSaveScope CreateTransientScope()
        {
            return new GraduationDetailsSaveScope
            {
                IsTransient = true
            };
        }

        internal static bool IsSafePortraitFileName(string fileName)
        {
            if (!GraduationDetailsPersistenceIO.IsSafeLeafFileName(fileName) ||
                fileName.Length > 96 ||
                !fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string stem = fileName.Substring(0, fileName.Length - 4);
            if (stem.Length == 0)
            {
                return false;
            }
            for (int index = 0; index < stem.Length; index++)
            {
                char character = stem[index];
                if (!((character >= 'A' && character <= 'Z') ||
                    (character >= 'a' && character <= 'z') ||
                    (character >= '0' && character <= '9') ||
                    character == '_' ||
                    character == '-'))
                {
                    return false;
                }
            }

            string upperStem = stem.ToUpperInvariant();
            if (upperStem == "CON" || upperStem == "PRN" ||
                upperStem == "AUX" || upperStem == "NUL")
            {
                return false;
            }
            return !(upperStem.Length == 4 &&
                (upperStem.StartsWith("COM", StringComparison.Ordinal) ||
                 upperStem.StartsWith("LPT", StringComparison.Ordinal)) &&
                upperStem[3] >= '1' && upperStem[3] <= '9');
        }

        internal static string NormalizeRelativePath(string value)
        {
            return (value ?? "")
                .Trim()
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');
        }

        private static bool TryNormalizeSupportedRelativeSavePath(
            string relativeSavePath,
            out string normalizedRelativePath)
        {
            normalizedRelativePath = "";
            if (string.IsNullOrWhiteSpace(relativeSavePath) ||
                Path.IsPathRooted(relativeSavePath))
            {
                return false;
            }

            string candidate = relativeSavePath.Replace(
                Path.AltDirectorySeparatorChar,
                Path.DirectorySeparatorChar).TrimStart(
                    Path.DirectorySeparatorChar);
            if (!AreRelativePathSegmentsSafe(candidate))
            {
                return false;
            }
            string[] segments = SplitRelativePath(candidate);
            if (!IsSupportedVanillaSaveSegments(segments))
            {
                return false;
            }
            normalizedRelativePath = string.Join(
                Path.DirectorySeparatorChar.ToString(),
                segments);
            return true;
        }

        private static bool IsSupportedVanillaSaveSegments(string[] segments)
        {
            if (segments == null || segments.Length == 0)
            {
                return false;
            }
            string fileName = segments[segments.Length - 1];
            if (string.Equals(
                    fileName,
                    GlobalDataFileName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (segments.Length == 1)
            {
                return IsDirectSaveFileName(fileName);
            }
            if (segments.Length == 3 &&
                string.Equals(
                    segments[0],
                    ManualSavesFolderName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return IsOpaquePathSegment(segments[1]) &&
                    IsSaveJsonFileName(fileName);
            }
            if (segments.Length < 3 ||
                !string.Equals(
                    segments[0],
                    StoryModeFolderName,
                    StringComparison.OrdinalIgnoreCase) ||
                !IsOpaquePathSegment(segments[1]))
            {
                return false;
            }
            if (segments.Length == 3)
            {
                return IsDirectSaveFileName(fileName);
            }
            if (segments.Length == 5 &&
                string.Equals(
                    segments[2],
                    ManualSavesFolderName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return IsOpaquePathSegment(segments[3]) &&
                    IsSaveJsonFileName(fileName);
            }
            return segments.Length == 4 &&
                IsStoryChapterFolderName(segments[2]) &&
                IsSaveJsonFileName(fileName);
        }

        private static bool IsDirectSaveFileName(string fileName)
        {
            return string.Equals(
                    fileName,
                    AutoSaveFileName,
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    fileName,
                    ManualSaveFileName,
                    StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSaveJsonFileName(string fileName)
        {
            return string.Equals(
                fileName,
                SaveFileName,
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStoryChapterFolderName(string folderName)
        {
            if (string.IsNullOrEmpty(folderName) ||
                !folderName.StartsWith(
                    StoryChapterFolderPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            int chapterIndex;
            return int.TryParse(
                    folderName.Substring(StoryChapterFolderPrefix.Length),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out chapterIndex) &&
                chapterIndex >= FirstStoryChapterIndex &&
                chapterIndex <= LastStoryChapterIndex;
        }

        private static bool IsOpaquePathSegment(string segment)
        {
            return !string.IsNullOrWhiteSpace(segment) &&
                !string.Equals(segment, ".", StringComparison.Ordinal) &&
                !string.Equals(segment, "..", StringComparison.Ordinal);
        }

        private static bool AreRelativePathSegmentsSafe(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) ||
                Path.IsPathRooted(relativePath))
            {
                return false;
            }
            string[] segments = SplitRelativePath(relativePath);
            if (segments.Length == 0)
            {
                return false;
            }
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            foreach (string segment in segments)
            {
                if (string.IsNullOrEmpty(segment) ||
                    string.Equals(segment, ".", StringComparison.Ordinal) ||
                    string.Equals(segment, "..", StringComparison.Ordinal) ||
                    segment.IndexOfAny(invalidCharacters) >= 0)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool HasNavigationPathSegment(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            string[] segments = path.Split(
                new char[]
                {
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                },
                StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < segments.Length; index++)
            {
                if (string.Equals(
                        segments[index],
                        ".",
                        StringComparison.Ordinal) ||
                    string.Equals(
                        segments[index],
                        "..",
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] SplitRelativePath(string relativePath)
        {
            return relativePath.Split(
                new char[]
                {
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                },
                StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool IsStrictlyContainedPath(
            string rootDirectory,
            string candidatePath)
        {
            try
            {
                return Path.GetFullPath(candidatePath).StartsWith(
                    BuildDirectoryPrefix(Path.GetFullPath(rootDirectory)),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string BuildDirectoryPrefix(string directoryPath)
        {
            return TrimTrailingDirectorySeparators(directoryPath) +
                Path.DirectorySeparatorChar;
        }

        private static bool TryNormalizeDirectoryPath(
            string directoryPath,
            out string normalizedDirectoryPath)
        {
            normalizedDirectoryPath = "";
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return false;
            }
            try
            {
                normalizedDirectoryPath = TrimTrailingDirectorySeparators(
                    Path.GetFullPath(directoryPath));
                return !string.IsNullOrEmpty(normalizedDirectoryPath);
            }
            catch
            {
                normalizedDirectoryPath = "";
                return false;
            }
        }

        private static string NormalizeDirectoryPathOrEmpty(string path)
        {
            string normalized;
            return TryNormalizeDirectoryPath(path, out normalized)
                ? normalized
                : "";
        }

        private static string TrimTrailingDirectorySeparators(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "";
            }
            string root = Path.GetPathRoot(path) ?? "";
            int end = path.Length;
            while (end > root.Length &&
                (path[end - 1] == Path.DirectorySeparatorChar ||
                 path[end - 1] == Path.AltDirectorySeparatorChar))
            {
                end--;
            }
            return end == path.Length ? path : path.Substring(0, end);
        }
    }

    /// <summary>
    /// Exact identity copied from vanilla SavedData. The physical path selects a
    /// sidecar; these fields select the one logical checkpoint to activate.
    /// </summary>
    internal sealed class GraduationDetailsSaveStamp
    {
        internal string RelativeSavePath = "";
        internal string LastSave = "";
        internal long PlaytimeSeconds;
        internal string GameDateTime = "";

        internal static bool TryCreate(
            SaveManager.SavedData savedData,
            string relativeSavePath,
            out GraduationDetailsSaveStamp stamp,
            out string errorMessage)
        {
            stamp = null;
            errorMessage = "";
            if (savedData == null || savedData.staticVars__PlayerData == null)
            {
                errorMessage = "Vanilla SavedData does not contain PlayerData.";
                return false;
            }
            string normalizedRelativePath =
                GraduationDetailsPaths.NormalizeRelativePath(relativeSavePath);
            if (string.IsNullOrEmpty(normalizedRelativePath))
            {
                errorMessage = "The vanilla save relative path is empty.";
                return false;
            }
            stamp = new GraduationDetailsSaveStamp
            {
                RelativeSavePath = normalizedRelativePath,
                LastSave = savedData.staticVars__PlayerData.LastSave ?? "",
                PlaytimeSeconds =
                    savedData.staticVars__PlayerData.Playtime_Seconds,
                GameDateTime = savedData.staticVars__dateTime ?? ""
            };
            return true;
        }

        internal bool Matches(GraduationDetailsCheckpointRecord checkpoint)
        {
            return checkpoint != null &&
                string.Equals(
                    GraduationDetailsPaths.NormalizeRelativePath(
                        checkpoint.RelativeSavePath),
                    GraduationDetailsPaths.NormalizeRelativePath(
                        RelativeSavePath),
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    checkpoint.LastSave ?? "",
                    LastSave ?? "",
                    StringComparison.Ordinal) &&
                checkpoint.PlaytimeSeconds == PlaytimeSeconds &&
                string.Equals(
                    checkpoint.GameDateTime ?? "",
                    GameDateTime ?? "",
                    StringComparison.Ordinal);
        }
    }

    [Serializable]
    internal sealed class GraduationDetailsSidecarDocument
    {
        public string FormatName =
            GraduationDetailsStorageEngine.SidecarFormatName;
        public int FormatVersion =
            GraduationDetailsStorageEngine.SidecarFormatVersion;
        public string RelativeSavePath = "";
        public long LastIssuedSequence;
        public List<GraduationDetailsCheckpointRecord> Checkpoints =
            new List<GraduationDetailsCheckpointRecord>();
        public List<GraduationDetailsMarriageMutationRecord> MarriageMutations =
            new List<GraduationDetailsMarriageMutationRecord>();
        public List<GraduationDetailsStaffMutationRecord> StaffMutations =
            new List<GraduationDetailsStaffMutationRecord>();
        public List<GraduationDetailsSnapshotMutationRecord> SnapshotMutations =
            new List<GraduationDetailsSnapshotMutationRecord>();
    }

    [Serializable]
    internal sealed class GraduationDetailsCheckpointRecord
    {
        public string RelativeSavePath = "";
        public string LastSave = "";
        public long PlaytimeSeconds;
        public string GameDateTime = "";
        public long Sequence;
    }

    [Serializable]
    internal sealed class GraduationDetailsMarriageMutationRecord
    {
        public long Sequence;
        public string Operation = GraduationDetailsStorageEngine.OperationSet;
        public int GirlId = -1;
        public MarriageRecord Value;
    }

    [Serializable]
    internal sealed class GraduationDetailsStaffMutationRecord
    {
        public long Sequence;
        public string Operation = GraduationDetailsStorageEngine.OperationSet;
        public int StaffId = -1;
        public StaffIdolRecord Value;
    }

    [Serializable]
    internal sealed class GraduationDetailsSnapshotMutationRecord
    {
        public long Sequence;
        public string Operation = GraduationDetailsStorageEngine.OperationSet;
        public int GirlId = -1;
        public GraduationSnapshot Value;
    }
}

namespace GraduationDetails
{
    /// <summary>
    /// Fail-soft facade used by the gameplay stores and vanilla save/load patches.
    /// </summary>
    internal static class GraduationDetailsPersistenceController
    {
        private static readonly object PersistenceLock = new object();
        private static GraduationDetailsStorageEngine storageEngine;
        private static GraduationDetailsSaveScope activeScope;
        private static string workingPortraitDirectory = "";
        private static bool staleWorkingCacheCleanupAttempted;

        static GraduationDetailsPersistenceController()
        {
            ResetToTransientLocked();
        }

        internal static bool TryGetMarriageRecord(
            int girlId,
            out MarriageRecord record)
        {
            record = null;
            try
            {
                lock (PersistenceLock)
                {
                    EnsureInitializedLocked();
                    return storageEngine.TryGetMarriageRecord(
                        girlId,
                        out record);
                }
            }
            catch (Exception exception)
            {
                WarnSafely("Marriage lookup failed: " + exception.Message);
                record = null;
                return false;
            }
        }

        internal static void UpsertMarriageRecord(MarriageRecord record)
        {
            try
            {
                lock (PersistenceLock)
                {
                    EnsureInitializedLocked();
                    bool changed;
                    string errorMessage;
                    if (!storageEngine.TryUpsertMarriageRecord(
                            record,
                            out changed,
                            out errorMessage))
                    {
                        WarnSafely(errorMessage);
                    }
                }
            }
            catch (Exception exception)
            {
                WarnSafely("Marriage update failed: " + exception.Message);
            }
        }

        internal static bool TryGetStaffIdolRecord(
            int staffId,
            out StaffIdolRecord record)
        {
            record = null;
            try
            {
                lock (PersistenceLock)
                {
                    EnsureInitializedLocked();
                    return storageEngine.TryGetStaffRecord(staffId, out record);
                }
            }
            catch (Exception exception)
            {
                WarnSafely("Staff mapping lookup failed: " + exception.Message);
                record = null;
                return false;
            }
        }

        internal static void UpsertStaffIdolRecord(StaffIdolRecord record)
        {
            try
            {
                lock (PersistenceLock)
                {
                    EnsureInitializedLocked();
                    bool changed;
                    string errorMessage;
                    if (!storageEngine.TryUpsertStaffRecord(
                            record,
                            out changed,
                            out errorMessage))
                    {
                        WarnSafely(errorMessage);
                    }
                }
            }
            catch (Exception exception)
            {
                WarnSafely("Staff mapping update failed: " + exception.Message);
            }
        }

        internal static void RemoveStaffIdolRecord(int staffId)
        {
            try
            {
                lock (PersistenceLock)
                {
                    EnsureInitializedLocked();
                    bool changed;
                    string errorMessage;
                    if (!storageEngine.TryRemoveStaffRecord(
                            staffId,
                            out changed,
                            out errorMessage))
                    {
                        WarnSafely(errorMessage);
                    }
                }
            }
            catch (Exception exception)
            {
                WarnSafely("Staff mapping removal failed: " + exception.Message);
            }
        }

        internal static bool TryGetGraduationSnapshot(
            int girlId,
            out GraduationSnapshot snapshot)
        {
            snapshot = null;
            try
            {
                lock (PersistenceLock)
                {
                    EnsureInitializedLocked();
                    return storageEngine.TryGetSnapshot(girlId, out snapshot);
                }
            }
            catch (Exception exception)
            {
                WarnSafely("Graduation snapshot lookup failed: " + exception.Message);
                snapshot = null;
                return false;
            }
        }

        internal static void UpsertGraduationSnapshot(
            GraduationSnapshot snapshot)
        {
            try
            {
                lock (PersistenceLock)
                {
                    EnsureInitializedLocked();
                    bool changed;
                    string errorMessage;
                    if (!storageEngine.TryUpsertSnapshot(
                            snapshot,
                            out changed,
                            out errorMessage))
                    {
                        WarnSafely(errorMessage);
                    }
                }
            }
            catch (Exception exception)
            {
                WarnSafely("Graduation snapshot update failed: " + exception.Message);
            }
        }

        internal static string GetPortraitReadPath(string safeFileName)
        {
            if (!GraduationDetailsPaths.IsSafePortraitFileName(safeFileName))
            {
                return "";
            }
            try
            {
                lock (PersistenceLock)
                {
                    EnsureInitializedLocked();
                    string candidate;
                    if (TryGetWorkingPortraitPathLocked(
                            safeFileName,
                            false,
                            out candidate) &&
                        File.Exists(candidate))
                    {
                        return candidate;
                    }
                    if (TryGetActivePortraitPathLocked(
                            safeFileName,
                            false,
                            out candidate) &&
                        File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }
            catch (Exception exception)
            {
                WarnSafely("Portrait lookup failed: " + exception.Message);
            }
            return "";
        }

        internal static string GetPortraitCapturePath(string safeFileName)
        {
            if (!GraduationDetailsPaths.IsSafePortraitFileName(safeFileName))
            {
                return "";
            }
            try
            {
                lock (PersistenceLock)
                {
                    EnsureInitializedLocked();
                    string path;
                    return TryGetWorkingPortraitPathLocked(
                            safeFileName,
                            true,
                            out path)
                        ? path
                        : "";
                }
            }
            catch (Exception exception)
            {
                WarnSafely("Portrait staging failed: " + exception.Message);
                return "";
            }
        }

        internal static bool TryStagePortrait(
            string sourcePath,
            string expectedDestinationPath)
        {
            string safeFileName = string.IsNullOrEmpty(expectedDestinationPath)
                ? ""
                : Path.GetFileName(expectedDestinationPath);
            if (string.IsNullOrEmpty(sourcePath) ||
                !File.Exists(sourcePath) ||
                string.IsNullOrEmpty(expectedDestinationPath) ||
                !GraduationDetailsPaths.IsSafePortraitFileName(safeFileName))
            {
                return false;
            }
            try
            {
                lock (PersistenceLock)
                {
                    EnsureInitializedLocked();
                    string destinationPath;
                    if (!TryGetWorkingPortraitPathLocked(
                            safeFileName,
                            true,
                            out destinationPath) ||
                        !SamePath(
                            expectedDestinationPath,
                            destinationPath))
                    {
                        return false;
                    }
                    return
                        TryCopyPortraitAtomically(
                            workingPortraitDirectory,
                            sourcePath,
                            destinationPath);
                }
            }
            catch (Exception exception)
            {
                WarnSafely("Portrait staging failed: " + exception.Message);
                return false;
            }
        }

        internal static void PrepareVanillaSaveWrite(
            SaveManager.SavedData savedData,
            string dataFileName,
            bool isJson,
            bool fullPath)
        {
            try
            {
                string resolvedPath;
                GraduationDetailsSaveScope targetScope;
                if (savedData == null ||
                    !GraduationDetailsPaths.TryResolveDataSaverPath(
                        dataFileName,
                        isJson,
                        fullPath,
                        out resolvedPath) ||
                    !GraduationDetailsPaths.TryResolveSaveScope(
                        resolvedPath,
                        out targetScope))
                {
                    WarnSafely("Rejected an unsupported vanilla save target.");
                    return;
                }
                GraduationDetailsSaveStamp stamp;
                string errorMessage;
                if (!GraduationDetailsSaveStamp.TryCreate(
                        savedData,
                        targetScope.RelativeSavePath,
                        out stamp,
                        out errorMessage))
                {
                    WarnSafely(errorMessage);
                    return;
                }

                lock (PersistenceLock)
                {
                    EnsureInitializedLocked();
                    long checkpointSequence =
                        storageEngine.LastIssuedSequence;
                    if (!storageEngine.AddOrReplaceCheckpoint(
                            stamp,
                            checkpointSequence,
                            out errorMessage))
                    {
                        WarnSafely(errorMessage);
                        return;
                    }
                    bool portraitsReady =
                        CopyReferencedPortraitsLocked(targetScope);
                    if (!storageEngine.TryPersistForScope(
                            targetScope,
                            out errorMessage))
                    {
                        WarnSafely(
                            "Could not persist the Graduation Details sidecar: " +
                            errorMessage);
                        return;
                    }
                    if (portraitsReady)
                    {
                        activeScope = targetScope;
                        ClearWorkingPortraitFilesLocked();
                    }
                    else
                    {
                        WarnSafely(
                            "The sidecar was saved, but the prior portrait scope " +
                            "was retained so a failed portrait copy can be retried.");
                    }
                }
            }
            catch (Exception exception)
            {
                WarnSafely(
                    "Save-boundary persistence failed without blocking vanilla: " +
                    exception.Message);
            }
        }

        internal static void OnVanillaSaveDataRead(
            SaveManager.SavedData loadedSaveData,
            string dataFileName)
        {
            GraduationDetailsStorageEngine loadedEngine =
                new GraduationDetailsStorageEngine();
            GraduationDetailsSaveScope targetScope = null;
            try
            {
                string resolvedPath;
                if (loadedSaveData == null ||
                    !GraduationDetailsPaths.TryResolveDataSaverLoadPath(
                        dataFileName,
                        out resolvedPath) ||
                    !GraduationDetailsPaths.TryResolveSaveScope(
                        resolvedPath,
                        out targetScope))
                {
                    lock (PersistenceLock)
                    {
                        ResetToTransientLocked();
                    }
                    WarnSafely(
                        "Could not resolve the loaded vanilla save path; " +
                        "supplemental state was detached safely.");
                    return;
                }

                GraduationDetailsSaveStamp stamp;
                string errorMessage;
                if (!GraduationDetailsSaveStamp.TryCreate(
                        loadedSaveData,
                        targetScope.RelativeSavePath,
                        out stamp,
                        out errorMessage))
                {
                    loadedEngine.InitializeEmpty(targetScope, out errorMessage);
                    InstallLoadedEngine(loadedEngine, targetScope);
                    WarnSafely(errorMessage);
                    return;
                }

                if (!loadedEngine.Initialize(targetScope, out errorMessage))
                {
                    WarnSafely(
                        "The sidecar was ignored safely: " + errorMessage);
                    if (!loadedEngine.InitializeEmpty(
                            targetScope,
                            out errorMessage))
                    {
                        lock (PersistenceLock)
                        {
                            ResetToTransientLocked();
                        }
                        WarnSafely(errorMessage);
                        return;
                    }
                }

                bool checkpointFound;
                long activatedSequence;
                if (!loadedEngine.TryActivateCheckpoint(
                        stamp,
                        out checkpointFound,
                        out activatedSequence,
                        out errorMessage))
                {
                    WarnSafely(errorMessage);
                    loadedEngine.InitializeEmpty(targetScope, out errorMessage);
                }
                else if (!checkpointFound &&
                    File.Exists(targetScope.SidecarFilePath))
                {
                    WarnSafely(
                        "No exact checkpoint matched the loaded vanilla save; " +
                        "supplemental state started empty.");
                }
                InstallLoadedEngine(loadedEngine, targetScope);
            }
            catch (Exception exception)
            {
                WarnSafely(
                    "Load restoration failed without blocking vanilla: " +
                    exception.Message);
                if (targetScope != null)
                {
                    string ignoredError;
                    loadedEngine.InitializeEmpty(targetScope, out ignoredError);
                    InstallLoadedEngine(loadedEngine, targetScope);
                }
                else
                {
                    lock (PersistenceLock)
                    {
                        ResetToTransientLocked();
                    }
                }
            }
        }

        internal static void OnNewGameStarting()
        {
            try
            {
                lock (PersistenceLock)
                {
                    ResetToTransientLocked();
                }
            }
            catch (Exception exception)
            {
                WarnSafely("New-game reset failed: " + exception.Message);
            }
        }

        private static void InstallLoadedEngine(
            GraduationDetailsStorageEngine loadedEngine,
            GraduationDetailsSaveScope scope)
        {
            lock (PersistenceLock)
            {
                storageEngine = loadedEngine;
                activeScope = scope;
                BeginFreshWorkingPortraitScopeLocked();
            }
        }

        private static void EnsureInitializedLocked()
        {
            if (storageEngine == null)
            {
                ResetToTransientLocked();
            }
            if (string.IsNullOrEmpty(workingPortraitDirectory))
            {
                BeginFreshWorkingPortraitScopeLocked();
            }
        }

        private static void ResetToTransientLocked()
        {
            storageEngine = new GraduationDetailsStorageEngine();
            storageEngine.InitializeTransient();
            activeScope = GraduationDetailsPaths.CreateTransientScope();
            BeginFreshWorkingPortraitScopeLocked();
        }

        private static void BeginFreshWorkingPortraitScopeLocked()
        {
            string cacheRoot = Path.Combine(
                Path.GetTempPath(),
                "GraduationDetails");
            if (!staleWorkingCacheCleanupAttempted)
            {
                TryDeleteOwnedWorkingSessions(cacheRoot);
                staleWorkingCacheCleanupAttempted = true;
            }
            else
            {
                TryDeletePriorWorkingSession(
                    cacheRoot,
                    workingPortraitDirectory);
            }

            workingPortraitDirectory = Path.Combine(
                cacheRoot,
                Guid.NewGuid().ToString("N"),
                "Portraits");
        }

        private static void ClearWorkingPortraitFilesLocked()
        {
            try
            {
                string cacheRoot = Path.Combine(
                    Path.GetTempPath(),
                    "GraduationDetails");
                TryDeletePriorWorkingSession(
                    cacheRoot,
                    workingPortraitDirectory);
            }
            catch
            {
                // Temp cleanup is best-effort and never affects save loading.
            }
        }

        private static void TryDeleteOwnedWorkingSessions(string cacheRoot)
        {
            try
            {
                if (!Directory.Exists(cacheRoot) ||
                    (File.GetAttributes(cacheRoot) &
                        FileAttributes.ReparsePoint) != 0)
                {
                    return;
                }
                string[] sessionDirectories = Directory.GetDirectories(
                    cacheRoot,
                    "*",
                    SearchOption.TopDirectoryOnly);
                for (int index = 0;
                    index < sessionDirectories.Length;
                    index++)
                {
                    TryDeleteOwnedWorkingSession(
                        cacheRoot,
                        sessionDirectories[index]);
                }
            }
            catch
            {
                // Temp cleanup is best-effort and never affects save loading.
            }
        }

        private static void TryDeletePriorWorkingSession(
            string cacheRoot,
            string portraitDirectory)
        {
            if (string.IsNullOrEmpty(portraitDirectory) ||
                !string.Equals(
                    Path.GetFileName(portraitDirectory),
                    "Portraits",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            try
            {
                DirectoryInfo portraitDirectoryInfo =
                    new DirectoryInfo(portraitDirectory);
                if (portraitDirectoryInfo.Parent != null)
                {
                    TryDeleteOwnedWorkingSession(
                        cacheRoot,
                        portraitDirectoryInfo.Parent.FullName);
                }
            }
            catch
            {
                // Temp cleanup is best-effort and never affects save loading.
            }
        }

        private static void TryDeleteOwnedWorkingSession(
            string cacheRoot,
            string sessionDirectory)
        {
            try
            {
                DirectoryInfo sessionInfo = new DirectoryInfo(sessionDirectory);
                Guid sessionIdentifier;
                if (sessionInfo.Parent == null ||
                    !string.Equals(
                        Path.GetFullPath(sessionInfo.Parent.FullName),
                        Path.GetFullPath(cacheRoot),
                        StringComparison.OrdinalIgnoreCase) ||
                    !Guid.TryParseExact(
                        sessionInfo.Name,
                        "N",
                        out sessionIdentifier))
                {
                    return;
                }

                string validatedSessionDirectory;
                if (!GraduationDetailsPersistenceIO.TryValidatePathUnderRoot(
                        cacheRoot,
                        sessionInfo.FullName,
                        true,
                        out validatedSessionDirectory) ||
                    !Directory.Exists(validatedSessionDirectory))
                {
                    return;
                }

                if (ContainsReparsePointBelow(validatedSessionDirectory))
                {
                    return;
                }
                Directory.Delete(validatedSessionDirectory, true);
            }
            catch
            {
                // Temp cleanup is best-effort and never affects save loading.
            }
        }

        private static bool ContainsReparsePointBelow(string rootDirectory)
        {
            Stack<string> pendingDirectories = new Stack<string>();
            pendingDirectories.Push(rootDirectory);
            while (pendingDirectories.Count > 0)
            {
                string currentDirectory = pendingDirectories.Pop();
                string[] childDirectories = Directory.GetDirectories(
                    currentDirectory,
                    "*",
                    SearchOption.TopDirectoryOnly);
                for (int index = 0;
                    index < childDirectories.Length;
                    index++)
                {
                    if ((File.GetAttributes(childDirectories[index]) &
                        FileAttributes.ReparsePoint) != 0)
                    {
                        return true;
                    }
                    pendingDirectories.Push(childDirectories[index]);
                }

                string[] childFiles = Directory.GetFiles(
                    currentDirectory,
                    "*",
                    SearchOption.TopDirectoryOnly);
                for (int index = 0; index < childFiles.Length; index++)
                {
                    if ((File.GetAttributes(childFiles[index]) &
                        FileAttributes.ReparsePoint) != 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryGetWorkingPortraitPathLocked(
            string fileName,
            bool forWrite,
            out string path)
        {
            path = "";
            return GraduationDetailsPaths.IsSafePortraitFileName(fileName) &&
                GraduationDetailsPersistenceIO.TryGetSafeLeafPath(
                    workingPortraitDirectory,
                    fileName,
                    forWrite,
                    out path);
        }

        private static bool TryGetActivePortraitPathLocked(
            string fileName,
            bool forWrite,
            out string path)
        {
            path = "";
            return activeScope != null && !activeScope.IsTransient &&
                GraduationDetailsPaths.IsSafePortraitFileName(fileName) &&
                GraduationDetailsPersistenceIO.TryGetSafeLeafPath(
                    activeScope.PortraitDirectoryPath,
                    fileName,
                    forWrite,
                    out path);
        }

        private static bool CopyReferencedPortraitsLocked(
            GraduationDetailsSaveScope targetScope)
        {
            bool allAvailablePortraitsCopied = true;
            foreach (GraduationSnapshot snapshot in storageEngine.GetSnapshots())
            {
                string fileName = snapshot == null
                    ? ""
                    : snapshot.PortraitFile;
                if (!GraduationDetailsPaths.IsSafePortraitFileName(fileName))
                {
                    continue;
                }
                string destination;
                if (!GraduationDetailsPersistenceIO.TryGetSafeLeafPath(
                        targetScope.PortraitDirectoryPath,
                        fileName,
                        true,
                        out destination))
                {
                    allAvailablePortraitsCopied = false;
                    continue;
                }

                string workingSource;
                bool workingSourceExists =
                    TryGetWorkingPortraitPathLocked(
                        fileName,
                        false,
                        out workingSource) &&
                    File.Exists(workingSource);
                string activeSource;
                bool activeSourceExists =
                    TryGetActivePortraitPathLocked(
                        fileName,
                        false,
                        out activeSource) &&
                    File.Exists(activeSource);
                if (!workingSourceExists && activeSourceExists)
                {
                    string stagedPath;
                    if (TryGetWorkingPortraitPathLocked(
                            fileName,
                            true,
                            out stagedPath) &&
                        TryCopyPortraitAtomically(
                            workingPortraitDirectory,
                            activeSource,
                            stagedPath))
                    {
                        workingSource = stagedPath;
                        workingSourceExists = true;
                    }
                }

                string source = workingSourceExists
                    ? workingSource
                    : activeSourceExists
                        ? activeSource
                        : "";
                if (string.IsNullOrEmpty(source))
                {
                    // Some snapshots legitimately have no captured portrait. There is
                    // no source to strand and no copy that can be retried.
                    continue;
                }

                if (!TryCopyPortraitAtomically(
                        targetScope.RootDirectoryPath,
                        source,
                        destination))
                {
                    allAvailablePortraitsCopied = false;
                }
            }
            return allAvailablePortraitsCopied;
        }

        private static bool TryCopyPortraitAtomically(
            string containmentRoot,
            string source,
            string destination)
        {
            if (SamePath(source, destination) && File.Exists(destination))
            {
                return true;
            }

            string temporaryPath = destination + ".tmp." +
                Guid.NewGuid().ToString("N");
            string backupPath = destination + ".bak." +
                Guid.NewGuid().ToString("N");
            string validatedDestination;
            string validatedTemporary;
            string validatedBackup;
            if (!GraduationDetailsPersistenceIO.TryValidatePathUnderRoot(
                    containmentRoot,
                    destination,
                    true,
                    out validatedDestination) ||
                !GraduationDetailsPersistenceIO.TryValidatePathUnderRoot(
                    containmentRoot,
                    temporaryPath,
                    true,
                    out validatedTemporary) ||
                !GraduationDetailsPersistenceIO.TryValidatePathUnderRoot(
                    containmentRoot,
                    backupPath,
                    true,
                    out validatedBackup))
            {
                return false;
            }
            try
            {
                string directory = Path.GetDirectoryName(validatedDestination);
                if (string.IsNullOrEmpty(directory))
                {
                    return false;
                }
                Directory.CreateDirectory(directory);
                GraduationDetailsPersistenceIO.CopyFileDurable(
                    source,
                    validatedTemporary);
                if (File.Exists(validatedDestination))
                {
                    File.Replace(
                        validatedTemporary,
                        validatedDestination,
                        validatedBackup,
                        true);
                    try
                    {
                        if (File.Exists(validatedBackup))
                        {
                            File.Delete(validatedBackup);
                        }
                    }
                    catch
                    {
                        // The replacement is already committed; a retained backup
                        // must not turn a successful portrait repair into a failure.
                    }
                }
                else
                {
                    File.Move(validatedTemporary, validatedDestination);
                }
                return true;
            }
            catch (Exception exception)
            {
                WarnSafely("Portrait copy failed: " + exception.Message);
                return false;
            }
            finally
            {
                try
                {
                    if (File.Exists(validatedTemporary))
                    {
                        File.Delete(validatedTemporary);
                    }
                }
                catch
                {
                }
            }
        }

        private static bool SamePath(string first, string second)
        {
            if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second))
            {
                return false;
            }
            try
            {
                return string.Equals(
                    Path.GetFullPath(first),
                    Path.GetFullPath(second),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static void WarnSafely(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            try
            {
                Debug.LogWarning("[Graduation Details] " + message);
            }
            catch
            {
            }
        }
    }
}

namespace GraduationDetails
{
    internal sealed partial class GraduationDetailsStorageEngine
    {
        private static bool TryValidateScope(
            GraduationDetailsSaveScope scope,
            out GraduationDetailsSaveScope validatedScope,
            out string errorMessage)
        {
            validatedScope = null;
            errorMessage = "";
            if (scope == null || scope.IsTransient ||
                string.IsNullOrEmpty(scope.PersistentDataRootPath) ||
                string.IsNullOrEmpty(scope.SaveFilePath))
            {
                errorMessage = "A physical Graduation Details save scope is required.";
                return false;
            }
            if (!GraduationDetailsPaths.TryResolveSaveScope(
                    scope.PersistentDataRootPath,
                    scope.SaveFilePath,
                    out validatedScope) ||
                !SamePath(scope.SaveFilePath, validatedScope.SaveFilePath) ||
                !SamePath(scope.SidecarFilePath, validatedScope.SidecarFilePath) ||
                !SamePath(
                    scope.PortraitDirectoryPath,
                    validatedScope.PortraitDirectoryPath))
            {
                validatedScope = null;
                errorMessage = "The Graduation Details save scope is invalid.";
                return false;
            }
            return true;
        }

        private static bool TryValidateDocument(
            GraduationDetailsSidecarDocument document,
            GraduationDetailsSaveScope scope,
            out string errorMessage)
        {
            errorMessage = "";
            if (document == null ||
                !string.Equals(
                    document.FormatName,
                    SidecarFormatName,
                    StringComparison.Ordinal) ||
                document.FormatVersion != SidecarFormatVersion)
            {
                errorMessage = "The Graduation Details sidecar format is unsupported.";
                return false;
            }
            if (!string.Equals(
                    GraduationDetailsPaths.NormalizeRelativePath(
                        document.RelativeSavePath),
                    GraduationDetailsPaths.NormalizeRelativePath(
                        scope.RelativeSavePath),
                    StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "The sidecar belongs to a different vanilla save path.";
                return false;
            }
            if (document.LastIssuedSequence < 0L ||
                document.Checkpoints == null ||
                document.MarriageMutations == null ||
                document.StaffMutations == null ||
                document.SnapshotMutations == null)
            {
                errorMessage = "The sidecar document is incomplete.";
                return false;
            }

            HashSet<long> sequences = new HashSet<long>();
            foreach (GraduationDetailsMarriageMutationRecord mutation
                in document.MarriageMutations)
            {
                if (mutation == null || mutation.Sequence <= 0L ||
                    mutation.Sequence > document.LastIssuedSequence ||
                    !sequences.Add(mutation.Sequence) ||
                    !string.Equals(
                        mutation.Operation,
                        OperationSet,
                        StringComparison.Ordinal) ||
                    mutation.GirlId < 0 || mutation.Value == null ||
                    mutation.Value.GirlId != mutation.GirlId)
                {
                    errorMessage = "The sidecar contains an invalid marriage mutation.";
                    return false;
                }
            }
            foreach (GraduationDetailsStaffMutationRecord mutation
                in document.StaffMutations)
            {
                bool isSet = mutation != null && string.Equals(
                    mutation.Operation,
                    OperationSet,
                    StringComparison.Ordinal);
                bool isRemove = mutation != null && string.Equals(
                    mutation.Operation,
                    OperationRemove,
                    StringComparison.Ordinal);
                if (mutation == null || mutation.Sequence <= 0L ||
                    mutation.Sequence > document.LastIssuedSequence ||
                    !sequences.Add(mutation.Sequence) || mutation.StaffId < 0 ||
                    (!isSet && !isRemove) ||
                    (isSet && (mutation.Value == null ||
                        mutation.Value.StaffId != mutation.StaffId ||
                        mutation.Value.GirlId < 0)) ||
                    (isRemove && mutation.Value != null))
                {
                    errorMessage = "The sidecar contains an invalid staff mutation.";
                    return false;
                }
            }
            foreach (GraduationDetailsSnapshotMutationRecord mutation
                in document.SnapshotMutations)
            {
                if (mutation == null || mutation.Sequence <= 0L ||
                    mutation.Sequence > document.LastIssuedSequence ||
                    !sequences.Add(mutation.Sequence) ||
                    !string.Equals(
                        mutation.Operation,
                        OperationSet,
                        StringComparison.Ordinal) ||
                    mutation.GirlId < 0 || mutation.Value == null ||
                    mutation.Value.GirlId != mutation.GirlId ||
                    !IsValidSnapshot(mutation.Value))
                {
                    errorMessage = "The sidecar contains an invalid snapshot mutation.";
                    return false;
                }
            }

            for (int index = 0; index < document.Checkpoints.Count; index++)
            {
                GraduationDetailsCheckpointRecord checkpoint =
                    document.Checkpoints[index];
                if (checkpoint == null || checkpoint.Sequence < 0L ||
                    checkpoint.Sequence > document.LastIssuedSequence ||
                    !string.Equals(
                        GraduationDetailsPaths.NormalizeRelativePath(
                            checkpoint.RelativeSavePath),
                        GraduationDetailsPaths.NormalizeRelativePath(
                            scope.RelativeSavePath),
                        StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = "The sidecar contains an invalid checkpoint.";
                    return false;
                }
                for (int otherIndex = index + 1;
                    otherIndex < document.Checkpoints.Count;
                    otherIndex++)
                {
                    if (CheckpointIdentityMatches(
                        checkpoint,
                        document.Checkpoints[otherIndex]))
                    {
                        errorMessage =
                            "The sidecar contains duplicate checkpoint identities.";
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool IsValidSnapshot(GraduationSnapshot snapshot)
        {
            if (snapshot == null || snapshot.GirlId < 0 ||
                (!string.IsNullOrEmpty(snapshot.PortraitFile) &&
                 !GraduationDetailsPaths.IsSafePortraitFileName(
                     snapshot.PortraitFile)) ||
                snapshot.Fans == null || snapshot.Bonds == null)
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
                if (section == null || section.Entries == null ||
                    section.Entries.Any(entry => entry == null))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool CheckpointIdentityMatches(
            GraduationDetailsCheckpointRecord first,
            GraduationDetailsCheckpointRecord second)
        {
            return first != null && second != null &&
                string.Equals(
                    GraduationDetailsPaths.NormalizeRelativePath(
                        first.RelativeSavePath),
                    GraduationDetailsPaths.NormalizeRelativePath(
                        second.RelativeSavePath),
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    first.LastSave ?? "",
                    second.LastSave ?? "",
                    StringComparison.Ordinal) &&
                first.PlaytimeSeconds == second.PlaytimeSeconds &&
                string.Equals(
                    first.GameDateTime ?? "",
                    second.GameDateTime ?? "",
                    StringComparison.Ordinal);
        }

        private GraduationDetailsSidecarDocument BuildDocumentLocked(
            string relativeSavePath)
        {
            string normalizedRelativePath =
                GraduationDetailsPaths.NormalizeRelativePath(relativeSavePath);
            return new GraduationDetailsSidecarDocument
            {
                FormatName = SidecarFormatName,
                FormatVersion = SidecarFormatVersion,
                RelativeSavePath = normalizedRelativePath,
                LastIssuedSequence = lastIssuedSequence,
                Checkpoints = CloneCheckpoints(activeCheckpoints
                    .Where(checkpoint => string.Equals(
                        GraduationDetailsPaths.NormalizeRelativePath(
                            checkpoint.RelativeSavePath),
                        normalizedRelativePath,
                        StringComparison.OrdinalIgnoreCase))),
                MarriageMutations = CloneMarriageMutations(
                    activeMarriageMutations),
                StaffMutations = CloneStaffMutations(activeStaffMutations),
                SnapshotMutations = CloneSnapshotMutations(
                    activeSnapshotMutations)
            };
        }

        private static bool TryWriteAtomically(
            GraduationDetailsSaveScope scope,
            string content,
            out string errorMessage)
        {
            errorMessage = "";
            string targetPath;
            if (!GraduationDetailsPersistenceIO.TryValidatePathUnderRoot(
                    scope.RootDirectoryPath,
                    scope.SidecarFilePath,
                    true,
                    out targetPath))
            {
                errorMessage = "The sidecar path failed containment validation.";
                return false;
            }
            string directory = Path.GetDirectoryName(targetPath);
            if (string.IsNullOrEmpty(directory))
            {
                errorMessage = "The sidecar path has no parent directory.";
                return false;
            }

            string temporaryPath = targetPath + ".graduationdetails.tmp." +
                Guid.NewGuid().ToString("N");
            string backupPath = targetPath + ".graduationdetails.bak";
            string validatedTemporaryPath;
            string validatedBackupPath;
            if (!GraduationDetailsPersistenceIO.TryValidatePathUnderRoot(
                    scope.RootDirectoryPath,
                    temporaryPath,
                    true,
                    out validatedTemporaryPath) ||
                !GraduationDetailsPersistenceIO.TryValidatePathUnderRoot(
                    scope.RootDirectoryPath,
                    backupPath,
                    true,
                    out validatedBackupPath))
            {
                errorMessage = "The atomic sidecar paths are invalid.";
                return false;
            }

            try
            {
                Directory.CreateDirectory(directory);
                GraduationDetailsPersistenceIO.WriteUtf8Durable(
                    validatedTemporaryPath,
                    content ?? "");
                if (File.Exists(targetPath))
                {
                    if (File.Exists(validatedBackupPath))
                    {
                        File.Delete(validatedBackupPath);
                    }
                    File.Replace(
                        validatedTemporaryPath,
                        targetPath,
                        validatedBackupPath,
                        true);
                    try
                    {
                        if (File.Exists(validatedBackupPath))
                        {
                            File.Delete(validatedBackupPath);
                        }
                    }
                    catch
                    {
                        // The primary sidecar is already committed. A stale owned
                        // backup is harmless and must not turn success into failure.
                    }
                }
                else
                {
                    File.Move(validatedTemporaryPath, targetPath);
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
                try
                {
                    if (File.Exists(validatedTemporaryPath))
                    {
                        File.Delete(validatedTemporaryPath);
                    }
                }
                catch
                {
                }
            }
        }

        private static List<GraduationDetailsCheckpointRecord> CloneCheckpoints(
            IEnumerable<GraduationDetailsCheckpointRecord> source)
        {
            List<GraduationDetailsCheckpointRecord> result =
                new List<GraduationDetailsCheckpointRecord>();
            if (source == null)
            {
                return result;
            }
            foreach (GraduationDetailsCheckpointRecord checkpoint in source)
            {
                if (checkpoint == null)
                {
                    continue;
                }
                result.Add(new GraduationDetailsCheckpointRecord
                {
                    RelativeSavePath =
                        GraduationDetailsPaths.NormalizeRelativePath(
                            checkpoint.RelativeSavePath),
                    LastSave = checkpoint.LastSave ?? "",
                    PlaytimeSeconds = checkpoint.PlaytimeSeconds,
                    GameDateTime = checkpoint.GameDateTime ?? "",
                    Sequence = checkpoint.Sequence
                });
            }
            return result.OrderBy(checkpoint => checkpoint.Sequence).ToList();
        }

        private static List<GraduationDetailsMarriageMutationRecord>
            CloneMarriageMutations(
                IEnumerable<GraduationDetailsMarriageMutationRecord> source)
        {
            List<GraduationDetailsMarriageMutationRecord> result =
                new List<GraduationDetailsMarriageMutationRecord>();
            if (source == null)
            {
                return result;
            }
            foreach (GraduationDetailsMarriageMutationRecord mutation in source)
            {
                if (mutation == null)
                {
                    continue;
                }
                result.Add(new GraduationDetailsMarriageMutationRecord
                {
                    Sequence = mutation.Sequence,
                    Operation = mutation.Operation ?? "",
                    GirlId = mutation.GirlId,
                    Value = GraduationDetailsRecordUtility.Clone(mutation.Value)
                });
            }
            return result.OrderBy(mutation => mutation.Sequence).ToList();
        }

        private static List<GraduationDetailsStaffMutationRecord>
            CloneStaffMutations(
                IEnumerable<GraduationDetailsStaffMutationRecord> source)
        {
            List<GraduationDetailsStaffMutationRecord> result =
                new List<GraduationDetailsStaffMutationRecord>();
            if (source == null)
            {
                return result;
            }
            foreach (GraduationDetailsStaffMutationRecord mutation in source)
            {
                if (mutation == null)
                {
                    continue;
                }
                result.Add(new GraduationDetailsStaffMutationRecord
                {
                    Sequence = mutation.Sequence,
                    Operation = mutation.Operation ?? "",
                    StaffId = mutation.StaffId,
                    Value = GraduationDetailsRecordUtility.Clone(mutation.Value)
                });
            }
            return result.OrderBy(mutation => mutation.Sequence).ToList();
        }

        private static List<GraduationDetailsSnapshotMutationRecord>
            CloneSnapshotMutations(
                IEnumerable<GraduationDetailsSnapshotMutationRecord> source)
        {
            List<GraduationDetailsSnapshotMutationRecord> result =
                new List<GraduationDetailsSnapshotMutationRecord>();
            if (source == null)
            {
                return result;
            }
            foreach (GraduationDetailsSnapshotMutationRecord mutation in source)
            {
                if (mutation == null)
                {
                    continue;
                }
                result.Add(new GraduationDetailsSnapshotMutationRecord
                {
                    Sequence = mutation.Sequence,
                    Operation = mutation.Operation ?? "",
                    GirlId = mutation.GirlId,
                    Value = GraduationDetailsRecordUtility.Clone(mutation.Value)
                });
            }
            return result.OrderBy(mutation => mutation.Sequence).ToList();
        }

        private static bool SamePath(string first, string second)
        {
            try
            {
                return string.Equals(
                    Path.GetFullPath(first),
                    Path.GetFullPath(second),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}

namespace GraduationDetails
{
    /// <summary>
    /// Lightweight append-only mutation history with materialized runtime indexes.
    /// Mutations never touch disk; persistence occurs only at vanilla save boundaries.
    /// </summary>
    internal sealed partial class GraduationDetailsStorageEngine
    {
        internal const string SidecarFormatName =
            "GraduationDetails.LightweightSidecar";
        internal const int SidecarFormatVersion = 1;
        internal const string OperationSet = "SET";
        internal const string OperationRemove = "REMOVE";

        private readonly object stateLock = new object();
        private readonly Dictionary<int, MarriageRecord> marriageRecords =
            new Dictionary<int, MarriageRecord>();
        private readonly Dictionary<int, StaffIdolRecord> staffRecords =
            new Dictionary<int, StaffIdolRecord>();
        private readonly Dictionary<int, GraduationSnapshot> snapshots =
            new Dictionary<int, GraduationSnapshot>();

        private List<GraduationDetailsCheckpointRecord> durableCheckpoints =
            new List<GraduationDetailsCheckpointRecord>();
        private List<GraduationDetailsMarriageMutationRecord>
            durableMarriageMutations =
                new List<GraduationDetailsMarriageMutationRecord>();
        private List<GraduationDetailsStaffMutationRecord> durableStaffMutations =
            new List<GraduationDetailsStaffMutationRecord>();
        private List<GraduationDetailsSnapshotMutationRecord>
            durableSnapshotMutations =
                new List<GraduationDetailsSnapshotMutationRecord>();

        private List<GraduationDetailsCheckpointRecord> activeCheckpoints =
            new List<GraduationDetailsCheckpointRecord>();
        private List<GraduationDetailsMarriageMutationRecord>
            activeMarriageMutations =
                new List<GraduationDetailsMarriageMutationRecord>();
        private List<GraduationDetailsStaffMutationRecord> activeStaffMutations =
            new List<GraduationDetailsStaffMutationRecord>();
        private List<GraduationDetailsSnapshotMutationRecord>
            activeSnapshotMutations =
                new List<GraduationDetailsSnapshotMutationRecord>();

        private long lastIssuedSequence;

        internal long LastIssuedSequence
        {
            get
            {
                lock (stateLock)
                {
                    return lastIssuedSequence;
                }
            }
        }

        internal void InitializeTransient()
        {
            lock (stateLock)
            {
                ResetAllLocked();
            }
        }

        internal bool InitializeEmpty(
            GraduationDetailsSaveScope scope,
            out string errorMessage)
        {
            errorMessage = "";
            GraduationDetailsSaveScope validatedScope;
            if (!TryValidateScope(scope, out validatedScope, out errorMessage))
            {
                return false;
            }
            lock (stateLock)
            {
                ResetAllLocked();
                return true;
            }
        }

        internal bool Initialize(
            GraduationDetailsSaveScope scope,
            out string errorMessage)
        {
            errorMessage = "";
            GraduationDetailsSaveScope validatedScope;
            if (!TryValidateScope(scope, out validatedScope, out errorMessage))
            {
                return false;
            }

            try
            {
                if (!File.Exists(validatedScope.SidecarFilePath))
                {
                    return InitializeEmpty(validatedScope, out errorMessage);
                }
                string json = File.ReadAllText(validatedScope.SidecarFilePath);
                GraduationDetailsSidecarDocument document =
                    JsonUtility.FromJson<GraduationDetailsSidecarDocument>(json);
                if (!TryValidateDocument(
                        document,
                        validatedScope,
                        out errorMessage))
                {
                    return false;
                }

                lock (stateLock)
                {
                    ResetAllLocked();
                    lastIssuedSequence = document.LastIssuedSequence;
                    durableCheckpoints = CloneCheckpoints(document.Checkpoints);
                    durableMarriageMutations = CloneMarriageMutations(
                        document.MarriageMutations);
                    durableStaffMutations = CloneStaffMutations(
                        document.StaffMutations);
                    durableSnapshotMutations = CloneSnapshotMutations(
                        document.SnapshotMutations);
                    // Nothing is materialized until the loaded vanilla stamp selects
                    // an exact checkpoint.
                    ClearActiveLocked();
                }
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        internal bool TryActivateCheckpoint(
            GraduationDetailsSaveStamp stamp,
            out bool checkpointFound,
            out long activatedSequence,
            out string errorMessage)
        {
            checkpointFound = false;
            activatedSequence = 0L;
            errorMessage = "";
            if (stamp == null)
            {
                errorMessage = "The vanilla save stamp is missing.";
                return false;
            }

            lock (stateLock)
            {
                try
                {
                    GraduationDetailsCheckpointRecord match = null;
                    foreach (GraduationDetailsCheckpointRecord checkpoint
                        in durableCheckpoints)
                    {
                        if (!stamp.Matches(checkpoint))
                        {
                            continue;
                        }
                        if (match != null)
                        {
                            errorMessage =
                                "The sidecar contains duplicate checkpoint identities.";
                            ClearActiveLocked();
                            return false;
                        }
                        match = checkpoint;
                    }

                    if (match == null)
                    {
                        ClearActiveLocked();
                        return true;
                    }
                    ActivateThroughSequenceLocked(match.Sequence);
                    checkpointFound = true;
                    activatedSequence = match.Sequence;
                    return true;
                }
                catch (Exception exception)
                {
                    ClearActiveLocked();
                    errorMessage = exception.Message;
                    return false;
                }
            }
        }

        internal bool AddOrReplaceCheckpoint(
            GraduationDetailsSaveStamp stamp,
            long sequence,
            out string errorMessage)
        {
            errorMessage = "";
            if (stamp == null || sequence < 0L)
            {
                errorMessage = "The checkpoint is invalid.";
                return false;
            }
            lock (stateLock)
            {
                if (sequence > lastIssuedSequence)
                {
                    errorMessage =
                        "The checkpoint exceeds the mutation sequence watermark.";
                    return false;
                }
                activeCheckpoints.RemoveAll(stamp.Matches);
                activeCheckpoints.Add(new GraduationDetailsCheckpointRecord
                {
                    RelativeSavePath =
                        GraduationDetailsPaths.NormalizeRelativePath(
                            stamp.RelativeSavePath),
                    LastSave = stamp.LastSave ?? "",
                    PlaytimeSeconds = stamp.PlaytimeSeconds,
                    GameDateTime = stamp.GameDateTime ?? "",
                    Sequence = sequence
                });
                return true;
            }
        }

        internal bool TryGetMarriageRecord(
            int girlId,
            out MarriageRecord record)
        {
            record = null;
            lock (stateLock)
            {
                MarriageRecord stored;
                if (!marriageRecords.TryGetValue(girlId, out stored))
                {
                    return false;
                }
                record = GraduationDetailsRecordUtility.Clone(stored);
                return record != null;
            }
        }

        internal bool TryUpsertMarriageRecord(
            MarriageRecord record,
            out bool changed,
            out string errorMessage)
        {
            changed = false;
            errorMessage = "";
            if (record == null || record.GirlId < 0)
            {
                errorMessage = "The marriage record is invalid.";
                return false;
            }
            lock (stateLock)
            {
                MarriageRecord normalized =
                    GraduationDetailsRecordUtility.Clone(record);
                MarriageRecord existing;
                if (marriageRecords.TryGetValue(record.GirlId, out existing) &&
                    GraduationDetailsRecordUtility.Same(existing, normalized))
                {
                    return true;
                }
                long sequence;
                if (!TryIssueSequenceLocked(out sequence, out errorMessage))
                {
                    return false;
                }
                activeMarriageMutations.Add(
                    new GraduationDetailsMarriageMutationRecord
                    {
                        Sequence = sequence,
                        Operation = OperationSet,
                        GirlId = normalized.GirlId,
                        Value = GraduationDetailsRecordUtility.Clone(normalized)
                    });
                marriageRecords[normalized.GirlId] = normalized;
                changed = true;
                return true;
            }
        }

        internal bool TryGetStaffRecord(
            int staffId,
            out StaffIdolRecord record)
        {
            record = null;
            lock (stateLock)
            {
                StaffIdolRecord stored;
                if (!staffRecords.TryGetValue(staffId, out stored))
                {
                    return false;
                }
                record = GraduationDetailsRecordUtility.Clone(stored);
                return record != null;
            }
        }

        internal bool TryUpsertStaffRecord(
            StaffIdolRecord record,
            out bool changed,
            out string errorMessage)
        {
            changed = false;
            errorMessage = "";
            if (record == null || record.StaffId < 0 || record.GirlId < 0)
            {
                errorMessage = "The staff-to-idol record is invalid.";
                return false;
            }
            lock (stateLock)
            {
                StaffIdolRecord normalized =
                    GraduationDetailsRecordUtility.Clone(record);
                StaffIdolRecord existing;
                if (staffRecords.TryGetValue(record.StaffId, out existing) &&
                    GraduationDetailsRecordUtility.Same(existing, normalized))
                {
                    return true;
                }
                long sequence;
                if (!TryIssueSequenceLocked(out sequence, out errorMessage))
                {
                    return false;
                }
                activeStaffMutations.Add(
                    new GraduationDetailsStaffMutationRecord
                    {
                        Sequence = sequence,
                        Operation = OperationSet,
                        StaffId = normalized.StaffId,
                        Value = GraduationDetailsRecordUtility.Clone(normalized)
                    });
                staffRecords[normalized.StaffId] = normalized;
                changed = true;
                return true;
            }
        }

        internal bool TryRemoveStaffRecord(
            int staffId,
            out bool changed,
            out string errorMessage)
        {
            changed = false;
            errorMessage = "";
            if (staffId < 0)
            {
                errorMessage = "The staff identifier is invalid.";
                return false;
            }
            lock (stateLock)
            {
                if (!staffRecords.ContainsKey(staffId))
                {
                    return true;
                }
                long sequence;
                if (!TryIssueSequenceLocked(out sequence, out errorMessage))
                {
                    return false;
                }
                activeStaffMutations.Add(
                    new GraduationDetailsStaffMutationRecord
                    {
                        Sequence = sequence,
                        Operation = OperationRemove,
                        StaffId = staffId,
                        Value = null
                    });
                staffRecords.Remove(staffId);
                changed = true;
                return true;
            }
        }

        internal bool TryGetSnapshot(
            int girlId,
            out GraduationSnapshot snapshot)
        {
            snapshot = null;
            lock (stateLock)
            {
                GraduationSnapshot stored;
                if (!snapshots.TryGetValue(girlId, out stored))
                {
                    return false;
                }
                snapshot = GraduationDetailsRecordUtility.Clone(stored);
                return snapshot != null;
            }
        }

        internal bool TryUpsertSnapshot(
            GraduationSnapshot snapshot,
            out bool changed,
            out string errorMessage)
        {
            changed = false;
            errorMessage = "";
            if (snapshot == null || snapshot.GirlId < 0 ||
                (!string.IsNullOrEmpty(snapshot.PortraitFile) &&
                 !GraduationDetailsPaths.IsSafePortraitFileName(
                     snapshot.PortraitFile)))
            {
                errorMessage = "The graduation snapshot is invalid.";
                return false;
            }
            lock (stateLock)
            {
                GraduationSnapshot normalized =
                    GraduationDetailsRecordUtility.Clone(snapshot);
                GraduationSnapshot existing;
                if (snapshots.TryGetValue(snapshot.GirlId, out existing) &&
                    GraduationDetailsRecordUtility.Same(existing, normalized))
                {
                    return true;
                }
                long sequence;
                if (!TryIssueSequenceLocked(out sequence, out errorMessage))
                {
                    return false;
                }
                activeSnapshotMutations.Add(
                    new GraduationDetailsSnapshotMutationRecord
                    {
                        Sequence = sequence,
                        Operation = OperationSet,
                        GirlId = normalized.GirlId,
                        Value = GraduationDetailsRecordUtility.Clone(normalized)
                    });
                snapshots[normalized.GirlId] = normalized;
                changed = true;
                return true;
            }
        }

        internal List<GraduationSnapshot> GetSnapshots()
        {
            lock (stateLock)
            {
                return snapshots.Values
                    .OrderBy(snapshot => snapshot.GirlId)
                    .Select(GraduationDetailsRecordUtility.Clone)
                    .ToList();
            }
        }

        internal bool TryPersistForScope(
            GraduationDetailsSaveScope scope,
            out string errorMessage)
        {
            errorMessage = "";
            GraduationDetailsSaveScope validatedScope;
            if (!TryValidateScope(scope, out validatedScope, out errorMessage))
            {
                return false;
            }

            lock (stateLock)
            {
                try
                {
                    GraduationDetailsSidecarDocument document =
                        BuildDocumentLocked(validatedScope.RelativeSavePath);
                    string json = JsonUtility.ToJson(document, false);
                    if (!TryWriteAtomically(
                            validatedScope,
                            json,
                            out errorMessage))
                    {
                        return false;
                    }

                    durableCheckpoints = CloneCheckpoints(document.Checkpoints);
                    durableMarriageMutations = CloneMarriageMutations(
                        document.MarriageMutations);
                    durableStaffMutations = CloneStaffMutations(
                        document.StaffMutations);
                    durableSnapshotMutations = CloneSnapshotMutations(
                        document.SnapshotMutations);
                    activeCheckpoints = CloneCheckpoints(document.Checkpoints);
                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = exception.Message;
                    return false;
                }
            }
        }

        private bool TryIssueSequenceLocked(
            out long sequence,
            out string errorMessage)
        {
            sequence = 0L;
            errorMessage = "";
            if (lastIssuedSequence == long.MaxValue)
            {
                errorMessage = "The mutation sequence is exhausted.";
                return false;
            }
            lastIssuedSequence++;
            sequence = lastIssuedSequence;
            return true;
        }

        private void ActivateThroughSequenceLocked(long sequence)
        {
            activeMarriageMutations = CloneMarriageMutations(
                durableMarriageMutations
                    .Where(mutation => mutation.Sequence <= sequence));
            activeStaffMutations = CloneStaffMutations(
                durableStaffMutations
                    .Where(mutation => mutation.Sequence <= sequence));
            activeSnapshotMutations = CloneSnapshotMutations(
                durableSnapshotMutations
                    .Where(mutation => mutation.Sequence <= sequence));
            activeCheckpoints = CloneCheckpoints(
                durableCheckpoints
                    .Where(checkpoint => checkpoint.Sequence <= sequence));
            RebuildMaterializedStateLocked();
        }

        private void RebuildMaterializedStateLocked()
        {
            marriageRecords.Clear();
            staffRecords.Clear();
            snapshots.Clear();
            foreach (GraduationDetailsMarriageMutationRecord mutation
                in activeMarriageMutations.OrderBy(item => item.Sequence))
            {
                marriageRecords[mutation.GirlId] =
                    GraduationDetailsRecordUtility.Clone(mutation.Value);
            }
            foreach (GraduationDetailsStaffMutationRecord mutation
                in activeStaffMutations.OrderBy(item => item.Sequence))
            {
                if (string.Equals(
                    mutation.Operation,
                    OperationRemove,
                    StringComparison.Ordinal))
                {
                    staffRecords.Remove(mutation.StaffId);
                }
                else
                {
                    staffRecords[mutation.StaffId] =
                        GraduationDetailsRecordUtility.Clone(mutation.Value);
                }
            }
            foreach (GraduationDetailsSnapshotMutationRecord mutation
                in activeSnapshotMutations.OrderBy(item => item.Sequence))
            {
                snapshots[mutation.GirlId] =
                    GraduationDetailsRecordUtility.Clone(mutation.Value);
            }
        }

        private void ClearActiveLocked()
        {
            activeCheckpoints.Clear();
            activeMarriageMutations.Clear();
            activeStaffMutations.Clear();
            activeSnapshotMutations.Clear();
            marriageRecords.Clear();
            staffRecords.Clear();
            snapshots.Clear();
        }

        private void ResetAllLocked()
        {
            ClearActiveLocked();
            durableCheckpoints.Clear();
            durableMarriageMutations.Clear();
            durableStaffMutations.Clear();
            durableSnapshotMutations.Clear();
            lastIssuedSequence = 0L;
        }
    }
}

namespace GraduationDetails
{
    /// <summary>
    /// Defensive cloning and value comparison for the mutable DTOs exposed by the
    /// gameplay layer. Returning clones is required because Capture updates an
    /// existing snapshot before submitting the next sequenced mutation.
    /// </summary>
    internal static class GraduationDetailsRecordUtility
    {
        internal static MarriageRecord Clone(MarriageRecord source)
        {
            if (source == null)
            {
                return null;
            }
            return new MarriageRecord
            {
                GirlId = source.GirlId,
                MarriedToPlayer = source.MarriedToPlayer,
                PlayerName = source.PlayerName ?? "",
                KidsCount = source.KidsCount,
                Custody = source.Custody
            };
        }

        internal static StaffIdolRecord Clone(StaffIdolRecord source)
        {
            if (source == null)
            {
                return null;
            }
            return new StaffIdolRecord
            {
                StaffId = source.StaffId,
                GirlId = source.GirlId,
                CapturedAtHire = source.CapturedAtHire,
                FirstName = source.FirstName ?? "",
                LastName = source.LastName ?? "",
                Nickname = source.Nickname ?? "",
                TextureSignature = source.TextureSignature ?? ""
            };
        }

        internal static GraduationSnapshot Clone(GraduationSnapshot source)
        {
            if (source == null)
            {
                return null;
            }
            GraduationSnapshot clone = new GraduationSnapshot
            {
                GirlId = source.GirlId,
                Birthdate = source.Birthdate ?? "",
                AgeAtGraduation = source.AgeAtGraduation,
                PortraitFile = source.PortraitFile ?? "",
                FirstName = source.FirstName ?? "",
                LastName = source.LastName ?? "",
                Nickname = source.Nickname ?? "",
                TextureSignature = source.TextureSignature ?? "",
                Fans = new List<FanSnapshot>(),
                Bonds = new List<BondSectionSnapshot>()
            };
            if (source.Fans != null)
            {
                foreach (FanSnapshot fan in source.Fans)
                {
                    if (fan == null)
                    {
                        continue;
                    }
                    clone.Fans.Add(new FanSnapshot
                    {
                        Gender = fan.Gender,
                        Hardcoreness = fan.Hardcoreness,
                        Age = fan.Age,
                        People = fan.People,
                        Appeal = fan.Appeal,
                        Opinion = fan.Opinion
                    });
                }
            }
            if (source.Bonds != null)
            {
                foreach (BondSectionSnapshot section in source.Bonds)
                {
                    if (section == null)
                    {
                        continue;
                    }
                    BondSectionSnapshot sectionClone = new BondSectionSnapshot
                    {
                        Type = section.Type,
                        LeaderId = section.LeaderId,
                        Entries = new List<BondEntry>()
                    };
                    if (section.Entries != null)
                    {
                        foreach (BondEntry entry in section.Entries)
                        {
                            if (entry == null)
                            {
                                continue;
                            }
                            sectionClone.Entries.Add(new BondEntry
                            {
                                GirlId = entry.GirlId,
                                Known = entry.Known,
                                RelationshipRatio = entry.RelationshipRatio,
                                IsDatingKnown = entry.IsDatingKnown
                            });
                        }
                    }
                    clone.Bonds.Add(sectionClone);
                }
            }
            return clone;
        }

        internal static bool Same(MarriageRecord first, MarriageRecord second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }
            return first != null && second != null &&
                first.GirlId == second.GirlId &&
                first.MarriedToPlayer == second.MarriedToPlayer &&
                string.Equals(
                    first.PlayerName ?? "",
                    second.PlayerName ?? "",
                    StringComparison.Ordinal) &&
                first.KidsCount == second.KidsCount &&
                first.Custody == second.Custody;
        }

        internal static bool Same(StaffIdolRecord first, StaffIdolRecord second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }
            return first != null && second != null &&
                first.StaffId == second.StaffId &&
                first.GirlId == second.GirlId &&
                first.CapturedAtHire == second.CapturedAtHire &&
                SameText(first.FirstName, second.FirstName) &&
                SameText(first.LastName, second.LastName) &&
                SameText(first.Nickname, second.Nickname) &&
                SameText(first.TextureSignature, second.TextureSignature);
        }

        internal static bool Same(
            GraduationSnapshot first,
            GraduationSnapshot second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }
            if (first == null || second == null ||
                first.GirlId != second.GirlId ||
                first.AgeAtGraduation != second.AgeAtGraduation ||
                !SameText(first.Birthdate, second.Birthdate) ||
                !SameText(first.PortraitFile, second.PortraitFile) ||
                !SameText(first.FirstName, second.FirstName) ||
                !SameText(first.LastName, second.LastName) ||
                !SameText(first.Nickname, second.Nickname) ||
                !SameText(first.TextureSignature, second.TextureSignature) ||
                !SameFans(first.Fans, second.Fans) ||
                !SameBonds(first.Bonds, second.Bonds))
            {
                return false;
            }
            return true;
        }

        private static bool SameFans(
            List<FanSnapshot> first,
            List<FanSnapshot> second)
        {
            int firstCount = first == null ? 0 : first.Count;
            int secondCount = second == null ? 0 : second.Count;
            if (firstCount != secondCount)
            {
                return false;
            }
            for (int index = 0; index < firstCount; index++)
            {
                FanSnapshot left = first[index];
                FanSnapshot right = second[index];
                if (ReferenceEquals(left, right))
                {
                    continue;
                }
                if (left == null || right == null ||
                    left.Gender != right.Gender ||
                    left.Hardcoreness != right.Hardcoreness ||
                    left.Age != right.Age ||
                    left.People != right.People ||
                    !left.Appeal.Equals(right.Appeal) ||
                    !left.Opinion.Equals(right.Opinion))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool SameBonds(
            List<BondSectionSnapshot> first,
            List<BondSectionSnapshot> second)
        {
            int firstCount = first == null ? 0 : first.Count;
            int secondCount = second == null ? 0 : second.Count;
            if (firstCount != secondCount)
            {
                return false;
            }
            for (int sectionIndex = 0;
                sectionIndex < firstCount;
                sectionIndex++)
            {
                BondSectionSnapshot left = first[sectionIndex];
                BondSectionSnapshot right = second[sectionIndex];
                if (ReferenceEquals(left, right))
                {
                    continue;
                }
                if (left == null || right == null ||
                    left.Type != right.Type ||
                    left.LeaderId != right.LeaderId)
                {
                    return false;
                }
                int leftCount = left.Entries == null
                    ? 0
                    : left.Entries.Count;
                int rightCount = right.Entries == null
                    ? 0
                    : right.Entries.Count;
                if (leftCount != rightCount)
                {
                    return false;
                }
                for (int entryIndex = 0;
                    entryIndex < leftCount;
                    entryIndex++)
                {
                    BondEntry leftEntry = left.Entries[entryIndex];
                    BondEntry rightEntry = right.Entries[entryIndex];
                    if (ReferenceEquals(leftEntry, rightEntry))
                    {
                        continue;
                    }
                    if (leftEntry == null || rightEntry == null ||
                        leftEntry.GirlId != rightEntry.GirlId ||
                        leftEntry.Known != rightEntry.Known ||
                        !leftEntry.RelationshipRatio.Equals(
                            rightEntry.RelationshipRatio) ||
                        leftEntry.IsDatingKnown != rightEntry.IsDatingKnown)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool SameText(string first, string second)
        {
            return string.Equals(
                first ?? "",
                second ?? "",
                StringComparison.Ordinal);
        }
    }
}
