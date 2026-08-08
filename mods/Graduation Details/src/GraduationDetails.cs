using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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

        private const string GameDataFolder = "data";
        private const string JsonExtension = ".json";
        private const string GenericSaveFile = "save.json";
        private const string GlobalDataFile = "global_data.json";
        private const string UnboundScope = "<unbound>";

        private static string activeVanillaSaveFilePath = "";
        private static string activeSaveDirectory = "";
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

        internal static string ActiveVanillaSaveFilePath
        {
            get
            {
                return activeVanillaSaveFilePath;
            }
        }

        internal static string GetScopeId()
        {
            return HasActiveSaveScope ? activeSaveDirectory : UnboundScope;
        }

        internal static string GetSaveDir()
        {
            return activeSaveDirectory;
        }

        internal static string GetScopedFilePath(string fileName)
        {
            if (!HasActiveSaveScope || string.IsNullOrEmpty(fileName))
            {
                return "";
            }
            return Path.Combine(activeSaveDirectory, fileName);
        }

        internal static string GetScopedPortraitDir()
        {
            if (!HasActiveSaveScope)
            {
                return "";
            }
            return Path.Combine(activeSaveDirectory, PortraitsFolder);
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
        }

        internal static void ClearBinding()
        {
            activeVanillaSaveFilePath = "";
            activeSaveDirectory = "";
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

    internal static class GraduationDetailsLegacyMigration
    {
        private const string RootFlatMigrationMarker = ".root_flat_data_migrated";
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
                List<string> legacyKeys = GetLegacyKeys(vanillaSaveFilePath);
                List<string> sourceRoots = GetLegacySourceRoots();
                bool foundScopedData = false;

                // Search every historical root for the most specific agency key before trying
                // owner-folder fallbacks. This prevents a broad chapter/slot key in one root from
                // winning over the exact agency data in another root.
                foreach (string legacyKey in legacyKeys)
                {
                    foreach (string sourceRoot in sourceRoots)
                    {
                        string savesSource = Path.Combine(
                            sourceRoot,
                            GraduationDetailsPaths.SavesFolder,
                            legacyKey);
                        string directSource = Path.Combine(sourceRoot, legacyKey);
                        foundScopedData |= HasAnyDataFile(savesSource);
                        foundScopedData |= HasAnyDataFile(directSource);
                        TryCopyMissingData(savesSource, targetDirectory);
                        TryCopyMissingData(directSource, targetDirectory);
                    }
                }

                // Some historical installs split portraits into a keyed folder while leaving the
                // JSON at the root. Portraits alone must not suppress this guarded flat fallback.
                if (!foundScopedData && !HasAnyDataFile(targetDirectory))
                {
                    TryMigrateRootFlatDataOnce(sourceRoots, targetDirectory);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Graduation Details] Legacy migration failed: " + exception.Message);
            }
        }

        private static List<string> GetLegacyKeys(string vanillaSaveFilePath)
        {
            List<string> keys = new List<string>();
            // Both forms shipped: story normally used SaveFolderName, while freeplay and some
            // transitional saves used the identity fallback even when a folder name existed.
            AddUnique(keys, GraduationDetailsPaths.GetLegacyFolderKey());
            AddUnique(keys, GraduationDetailsPaths.GetLegacyFallbackKey());

            string ownerKey = GraduationDetailsPaths.GetLegacyOwnerKey(vanillaSaveFilePath);
            AddUnique(keys, ownerKey);
            AddUnique(keys, GraduationDetailsPaths.SanitizeLegacyFileToken(ownerKey));
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

        private static void TryMigrateRootFlatDataOnce(
            List<string> sourceRoots,
            string targetDirectory)
        {
            string markerPath = Path.Combine(
                GraduationDetailsPaths.RootDir,
                RootFlatMigrationMarker);
            if (File.Exists(markerPath))
            {
                return;
            }

            bool importedRootData = false;
            foreach (string sourceRoot in sourceRoots)
            {
                bool sourceHasData = HasAnyDataFile(sourceRoot);
                TryCopyMissingData(sourceRoot, targetDirectory);
                importedRootData |= sourceHasData && HasAnyDataFile(targetDirectory);
            }

            // Portrait-only roots are useful to merge, but are not evidence that the ambiguous
            // flat JSON fallback was consumed. Mark only after importing at least one data file.
            if (importedRootData)
            {
                Directory.CreateDirectory(GraduationDetailsPaths.RootDir);
                File.WriteAllText(markerPath, targetDirectory);
                Debug.Log("[Graduation Details] Imported root-level legacy data into " + targetDirectory);
            }
        }

        private static bool TryCopyMissingData(string sourceDirectory, string targetDirectory)
        {
            if (string.IsNullOrEmpty(sourceDirectory)
                || string.IsNullOrEmpty(targetDirectory)
                || !Directory.Exists(sourceDirectory)
                || PathsReferToSameDirectory(sourceDirectory, targetDirectory))
            {
                return false;
            }

            bool copied = false;
            copied |= CopyFileIfMissing(
                Path.Combine(sourceDirectory, GraduationDetailsPaths.MarriageFile),
                Path.Combine(targetDirectory, GraduationDetailsPaths.MarriageFile));
            copied |= CopyFileIfMissing(
                Path.Combine(sourceDirectory, GraduationDetailsPaths.StaffMapFile),
                Path.Combine(targetDirectory, GraduationDetailsPaths.StaffMapFile));
            copied |= CopyFileIfMissing(
                Path.Combine(sourceDirectory, GraduationDetailsPaths.SnapshotsFile),
                Path.Combine(targetDirectory, GraduationDetailsPaths.SnapshotsFile));

            string sourcePortraits = Path.Combine(
                sourceDirectory,
                GraduationDetailsPaths.PortraitsFolder);
            string targetPortraits = Path.Combine(
                targetDirectory,
                GraduationDetailsPaths.PortraitsFolder);
            copied |= CopyDirectoryFilesIfMissing(sourcePortraits, targetPortraits);

            if (copied)
            {
                Debug.Log("[Graduation Details] Copied legacy save data from " + sourceDirectory);
            }
            return copied;
        }

        private static bool CopyFileIfMissing(string sourceFile, string targetFile)
        {
            if (!File.Exists(sourceFile) || File.Exists(targetFile))
            {
                return false;
            }

            string targetParent = Path.GetDirectoryName(targetFile);
            if (!string.IsNullOrEmpty(targetParent) && !Directory.Exists(targetParent))
            {
                Directory.CreateDirectory(targetParent);
            }
            File.Copy(sourceFile, targetFile, false);
            return true;
        }

        private static bool CopyDirectoryFilesIfMissing(string sourceDirectory, string targetDirectory)
        {
            if (!Directory.Exists(sourceDirectory))
            {
                return false;
            }

            bool copied = false;
            foreach (string sourceFile in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                string relativePath = sourceFile.Substring(
                    sourceDirectory.TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar).Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                copied |= CopyFileIfMissing(sourceFile, Path.Combine(targetDirectory, relativePath));
            }
            return copied;
        }

        private static bool HasAnyDataFile(string directory)
        {
            return File.Exists(Path.Combine(directory, GraduationDetailsPaths.MarriageFile))
                || File.Exists(Path.Combine(directory, GraduationDetailsPaths.StaffMapFile))
                || File.Exists(Path.Combine(directory, GraduationDetailsPaths.SnapshotsFile));
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
    }

    internal static class GraduationDetailsPersistence
    {
        private static bool loadInProgress;
        private static bool loadPathCaptured;
        private static bool awaitingLatestAutosavePath;
        private static string stagedVanillaSaveFilePath = "";
        private static string stagedSaveDirectory = "";

        internal static bool LoadInProgress
        {
            get
            {
                return loadInProgress;
            }
        }

        internal static void BeginLoad(bool expectLatestAutosavePath = false)
        {
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

            GraduationDetailsPaths.BeginFreshWorkingPortraitScope();
            if (!string.IsNullOrEmpty(saveDirectory))
            {
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
            loadInProgress = false;
            loadPathCaptured = false;
            awaitingLatestAutosavePath = false;
            stagedVanillaSaveFilePath = "";
            stagedSaveDirectory = "";
        }

        internal static void ResetForNewGame()
        {
            CancelLoad();
            GraduationDetailsPaths.ClearBinding();
            GraduationDetailsPaths.BeginFreshWorkingPortraitScope();
            MarriageRecordStore.ResetForScopeChange();
            StaffIdolStore.ResetForScopeChange();
            GraduationSnapshotStore.ResetForScopeChange();
        }

        internal static void OnVanillaSaveScheduled(string dataFileName, bool fullPath)
        {
            if (loadInProgress)
            {
                return;
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
                return;
            }

            try
            {
                // Save As must serialize the live dictionaries even when they are empty. This
                // deliberately overwrites stale JSON left behind when vanilla reuses a deleted slot.
                MarriageRecordStore.EnsureReady();
                StaffIdolStore.EnsureReady();
                GraduationSnapshotStore.EnsureReady();
                MarriageRecordStore.SaveToDirectory(saveDirectory);
                StaffIdolStore.SaveToDirectory(saveDirectory);
                GraduationSnapshotStore.SaveToDirectory(saveDirectory);

                // Rebinding after the snapshot preserves the live cached state instead of clearing
                // it and loading an older destination when the player uses Save As.
                GraduationDetailsPaths.Bind(normalizedPath, saveDirectory);
                MarriageRecordStore.RebindLoadedScope();
                StaffIdolStore.RebindLoadedScope();
                GraduationSnapshotStore.RebindLoadedScope();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Graduation Details] Failed to snapshot save data: " + exception.Message);
            }
        }
    }

    internal static class MarriageRecordStore
    {
        private static readonly Dictionary<int, MarriageRecord> Records = new Dictionary<int, MarriageRecord>();
        private static bool loaded;
        private static string loadedScope = "";

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
            loadedScope = "";
            Records.Clear();
        }

        internal static void RebindLoadedScope()
        {
            loaded = true;
            loadedScope = GraduationDetailsPaths.GetScopeId();
        }

        internal static void SaveToDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);
            MarriageRecordList list = new MarriageRecordList();
            list.Records = Records.Values.ToList();
            string json = JsonUtility.ToJson(list, true);
            File.WriteAllText(
                Path.Combine(directory, GraduationDetailsPaths.MarriageFile),
                json);
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
            Records.Clear();
            try
            {
                if (!GraduationDetailsPaths.HasActiveSaveScope || !File.Exists(DataPath))
                {
                    return;
                }
                string loadPath = DataPath;
                string json = File.ReadAllText(loadPath);
                if (string.IsNullOrEmpty(json))
                {
                    return;
                }
                MarriageRecordList list = JsonUtility.FromJson<MarriageRecordList>(json);
                if (list == null || list.Records == null)
                {
                    return;
                }
                Records.Clear();
                foreach (MarriageRecord record in list.Records)
                {
                    if (record == null || record.GirlId < 0)
                    {
                        continue;
                    }
                    Records[record.GirlId] = record;
                }
            }
            catch
            {
                Records.Clear();
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
        private static string loadedScope = "";

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
            loadedScope = "";
            StaffToGirl.Clear();
        }

        internal static void RebindLoadedScope()
        {
            loaded = true;
            loadedScope = GraduationDetailsPaths.GetScopeId();
        }

        internal static void SaveToDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);
            StaffIdolRecordList list = new StaffIdolRecordList();
            foreach (KeyValuePair<int, StaffIdolRecord> entry in StaffToGirl)
            {
                if (entry.Value != null)
                {
                    list.Records.Add(entry.Value);
                }
            }
            string json = JsonUtility.ToJson(list, true);
            File.WriteAllText(
                Path.Combine(directory, GraduationDetailsPaths.StaffMapFile),
                json);
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
            StaffToGirl.Clear();
            try
            {
                if (!GraduationDetailsPaths.HasActiveSaveScope || !File.Exists(DataPath))
                {
                    return;
                }
                string loadPath = DataPath;
                string json = File.ReadAllText(loadPath);
                if (string.IsNullOrEmpty(json))
                {
                    return;
                }
                StaffIdolRecordList list = JsonUtility.FromJson<StaffIdolRecordList>(json);
                if (list == null || list.Records == null)
                {
                    return;
                }
                StaffToGirl.Clear();
                foreach (StaffIdolRecord record in list.Records)
                {
                    if (record == null || record.StaffId < 0 || record.GirlId < 0)
                    {
                        continue;
                    }
                    StaffToGirl[record.StaffId] = record;
                }
            }
            catch
            {
                StaffToGirl.Clear();
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
        private static string loadedScope = "";

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
            if (snapshot == null || string.IsNullOrEmpty(snapshot.PortraitFile))
            {
                return "";
            }
            string workingPath = Path.Combine(WorkingPortraitDir, snapshot.PortraitFile);
            if (File.Exists(workingPath))
            {
                return workingPath;
            }

            if (!string.IsNullOrEmpty(PortraitDir))
            {
                string scopedPath = Path.Combine(PortraitDir, snapshot.PortraitFile);
                if (File.Exists(scopedPath))
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
            loadedScope = "";
            Records.Clear();
        }

        internal static void RebindLoadedScope()
        {
            loaded = true;
            loadedScope = GraduationDetailsPaths.GetScopeId();
        }

        internal static void SaveToDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);
            GraduationSnapshotList list = new GraduationSnapshotList();
            list.Records = Records.Values.ToList();
            string json = JsonUtility.ToJson(list, true);
            File.WriteAllText(
                Path.Combine(directory, GraduationDetailsPaths.SnapshotsFile),
                json);

            string targetPortraitDirectory = Path.Combine(
                directory,
                GraduationDetailsPaths.PortraitsFolder);
            foreach (GraduationSnapshot snapshot in Records.Values)
            {
                if (snapshot == null || string.IsNullOrEmpty(snapshot.PortraitFile))
                {
                    continue;
                }

                string sourcePath = GetPortraitPath(snapshot);
                string targetPath = Path.Combine(targetPortraitDirectory, snapshot.PortraitFile);
                if (!File.Exists(sourcePath))
                {
                    // Portrait generation is asynchronous. Bind this already-written snapshot to
                    // its exact save target so the late result cannot follow a later active scope.
                    RegisterPendingPortraitTarget(sourcePath, targetPath);
                    continue;
                }
                if (PathsReferToSameFile(sourcePath, targetPath))
                {
                    continue;
                }
                CopyPortrait(sourcePath, targetPath);
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
            Records.Clear();
            try
            {
                if (!GraduationDetailsPaths.HasActiveSaveScope || !File.Exists(DataPath))
                {
                    return;
                }
                string loadPath = DataPath;
                string json = File.ReadAllText(loadPath);
                if (string.IsNullOrEmpty(json))
                {
                    return;
                }
                GraduationSnapshotList list = JsonUtility.FromJson<GraduationSnapshotList>(json);
                if (list == null || list.Records == null)
                {
                    return;
                }
                Records.Clear();
                foreach (GraduationSnapshot snapshot in list.Records)
                {
                    if (snapshot == null || snapshot.GirlId < 0)
                    {
                        continue;
                    }
                    Records[snapshot.GirlId] = snapshot;
                }
            }
            catch
            {
                Records.Clear();
            }
        }

        private static void Save()
        {
            // Mutations stay in memory until the matching vanilla save is scheduled.
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
            string destPath = Path.Combine(WorkingPortraitDir, snapshot.PortraitFile);
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
            if (string.IsNullOrEmpty(sourcePath)
                || string.IsNullOrEmpty(targetPath)
                || PathsReferToSameFile(sourcePath, targetPath))
            {
                return;
            }

            HashSet<string> targets;
            if (!PendingPortraitTargets.TryGetValue(sourcePath, out targets))
            {
                targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                PendingPortraitTargets[sourcePath] = targets;
            }
            targets.Add(targetPath);
        }

        private static void RegisterScopedPortraitRepair(GraduationSnapshot snapshot)
        {
            if (snapshot == null
                || string.IsNullOrEmpty(snapshot.PortraitFile)
                || string.IsNullOrEmpty(PortraitDir))
            {
                return;
            }

            string workingPath = Path.Combine(WorkingPortraitDir, snapshot.PortraitFile);
            string scopedPath = Path.Combine(PortraitDir, snapshot.PortraitFile);
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
                string dir = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.Copy(sourcePath, destPath, true);
                return true;
            }
            catch
            {
                // Ignore file copy errors to avoid breaking the game loop.
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
            MethodInfo captureMethod = AccessTools.Method(
                typeof(GraduationDetailsPersistence),
                nameof(GraduationDetailsPersistence.OnVanillaSaveScheduled),
                new Type[] { typeof(string), typeof(bool) });
            int injectedWriteCount = 0;

            foreach (CodeInstruction instruction in codes)
            {
                if (!IsSavedDataWrite(instruction))
                {
                    yield return instruction;
                    continue;
                }

                // Preserve the exact four vanilla arguments, schedule the game save unchanged,
                // and snapshot/rebind this mod only after DataSaver returns successfully.
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
                yield return instruction;
                yield return new CodeInstruction(OpCodes.Ldloc, dataFileNameLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, fullPathLocal);
                yield return new CodeInstruction(OpCodes.Call, captureMethod);
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
