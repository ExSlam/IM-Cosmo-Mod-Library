using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace IMDataCore
{
    /// <summary>
    /// Describes one vanilla save file and its independent IM Data Core storage scope.
    /// The visible directory mirrors the vanilla path while the internal key retains
    /// the historical full-path hash format used inside persisted records.
    /// </summary>
    internal sealed class CoreSaveScope
    {
        internal string SaveFilePath = string.Empty;
        internal string StorageRelativeDirectory = string.Empty;
        internal string InternalSaveKey = CoreConstants.DefaultSaveKey;
        internal string LegacyOwnerSaveKey = string.Empty;
        internal bool IsTransient;
        internal bool IsStaging;
    }

    /// <summary>
    /// Save-scoped path resolution for IM Data Core.
    /// </summary>
    internal static class CorePaths
    {
        private static readonly object saveScopeLock = new object();
        private static string activeSaveFilePathHint = string.Empty;
        private static CoreSaveScope activeSaveScopeOverride;
        private static CoreSaveScope transientSaveScope =
            CreateTransientSaveScope();

        private const string GameDataRootFolderName = "data";
        private const string SaveFileName = "save";
        private const string AutoSaveFileName = "auto_save";
        private const string ManualSaveFileName = "manual_save";
        private const string ManualSavesFolderName = "manual_saves";
        private const string StoryModeFolderName = "story_mode";
        private const string StoryChapterFolderPrefix = "chapter_";
        private const string TransactionFolderName = "_transactions";
        private const string LoadTransactionPrefix = "load_";
        private const string SaveTransactionPrefix = "save_";
        private const string GlobalDataFileName = "global_data.json";
        private const int FirstStoryChapterIndex = 0;
        private const int LastStoryChapterIndex = 6;
        private const string SaveFileKeyPrefix = "file";
        private const string SaveFileExtension = ".json";
        private const int SavePathHashLength = 16;
        private const int SavePathTokenLength = 32;
        private const char SavePathSeparatorReplacement = '_';
        private const string LegacyDisplayModFolderName = "IM Data Core";
        private const string LegacyWorkshopItemIdentifier = "3680836490";
        private const string IdolManagerSteamApplicationIdentifier = "821880";
        private const string SteamAppsFolderName = "steamapps";
        private const string WorkshopFolderName = "workshop";
        private const string WorkshopContentFolderName = "content";
        private const string DataCoreAssemblyFileName =
            "com.cosmo.imdatacore.dll";

        /// <summary>
        /// Returns the stable IM Data Core root beside the game's data directory.
        /// </summary>
        internal static string GetRootDirectory()
        {
            return Path.Combine(
                Application.persistentDataPath,
                CoreConstants.ModFolderName);
        }

        /// <summary>
        /// Returns the root beneath which the vanilla save directory layout is mirrored.
        /// </summary>
        internal static string GetSavesRootDirectory()
        {
            return Path.Combine(
                GetRootDirectory(),
                CoreConstants.SaveFolderName);
        }

        /// <summary>
        /// Stores one best-effort save-file path hint used for save-scope derivation.
        /// </summary>
        internal static void SetActiveSaveFilePathHint(string saveFilePath)
        {
            CoreSaveScope resolvedSaveScope;
            if (!TryCreateSaveScope(saveFilePath, out resolvedSaveScope))
            {
                return;
            }

            lock (saveScopeLock)
            {
                activeSaveFilePathHint = resolvedSaveScope.SaveFilePath;
                activeSaveScopeOverride = null;
            }
        }

        /// <summary>
        /// Resolves one physical vanilla save path without changing the active scope.
        /// </summary>
        internal static bool TryResolveSaveScope(
            string saveFilePath,
            out CoreSaveScope saveScope)
        {
            return TryCreateSaveScope(saveFilePath, out saveScope);
        }

        /// <summary>
        /// Creates a private storage scope that keeps the target save identity while
        /// directing all I/O into a disposable transaction directory.
        /// </summary>
        internal static CoreSaveScope CreateStagingSaveScope(
            CoreSaveScope targetSaveScope,
            bool forLoad)
        {
            if (targetSaveScope == null || targetSaveScope.IsTransient)
            {
                return null;
            }

            string transactionToken = Guid.NewGuid().ToString("N");
            return new CoreSaveScope
            {
                SaveFilePath = targetSaveScope.SaveFilePath,
                StorageRelativeDirectory = Path.Combine(
                    TransactionFolderName,
                    (forLoad
                        ? LoadTransactionPrefix
                        : SaveTransactionPrefix) + transactionToken),
                InternalSaveKey = targetSaveScope.InternalSaveKey,
                LegacyOwnerSaveKey = targetSaveScope.LegacyOwnerSaveKey,
                IsStaging = true
            };
        }

        /// <summary>
        /// Selects an already-resolved scope. Staging scopes are retained verbatim so
        /// EnsureInitialized cannot accidentally switch back to the canonical folder.
        /// </summary>
        internal static void UseSaveScope(CoreSaveScope saveScope)
        {
            if (saveScope == null)
            {
                return;
            }

            lock (saveScopeLock)
            {
                activeSaveFilePathHint = saveScope.SaveFilePath ?? string.Empty;
                activeSaveScopeOverride = saveScope.IsStaging
                    ? saveScope
                    : null;

                if (saveScope.IsTransient)
                {
                    transientSaveScope = saveScope;
                    activeSaveFilePathHint = string.Empty;
                }
            }
        }

        /// <summary>
        /// Returns true only for a physical vanilla game-save path. Global settings
        /// and arbitrary JSON files beneath the data root can never own a sidecar.
        /// </summary>
        internal static bool IsSupportedGameSavePath(string saveFilePath)
        {
            CoreSaveScope ignoredSaveScope;
            return TryCreateSaveScope(saveFilePath, out ignoredSaveScope);
        }

        /// <summary>
        /// Starts a fresh non-vanilla scope for a brand-new game. It prevents a new
        /// campaign from opening the previous game's auto-save sidecar before the
        /// first exact DataSaver target is known.
        /// </summary>
        internal static void ResetToTransientSaveScope()
        {
            lock (saveScopeLock)
            {
                activeSaveFilePathHint = string.Empty;
                activeSaveScopeOverride = null;
                transientSaveScope = CreateTransientSaveScope();
            }
        }

        /// <summary>
        /// Restores an earlier resolved scope after vanilla rejects or aborts a load.
        /// Keeping the same transient object is important because it already owns the
        /// temporary storage opened before the failed load attempt.
        /// </summary>
        internal static void RestoreSaveScope(CoreSaveScope saveScope)
        {
            if (saveScope == null)
            {
                return;
            }

            lock (saveScopeLock)
            {
                activeSaveScopeOverride = saveScope.IsStaging
                    ? saveScope
                    : null;

                if (saveScope.IsTransient)
                {
                    transientSaveScope = saveScope;
                    activeSaveFilePathHint = string.Empty;
                    return;
                }

                activeSaveFilePathHint = saveScope.SaveFilePath ??
                    string.Empty;
            }
        }

        /// <summary>
        /// Resolves the active save into its canonical vanilla path, mirrored storage
        /// directory, historical internal key, and prior owner-folder key.
        /// </summary>
        internal static CoreSaveScope GetSaveScope()
        {
            string saveFilePath;
            lock (saveScopeLock)
            {
                if (activeSaveScopeOverride != null)
                {
                    return activeSaveScopeOverride;
                }

                saveFilePath = activeSaveFilePathHint;
            }

            if (string.IsNullOrEmpty(saveFilePath))
            {
                lock (saveScopeLock)
                {
                    return transientSaveScope;
                }
            }

            CoreSaveScope saveScope;
            if (TryCreateSaveScope(saveFilePath, out saveScope))
            {
                return saveScope;
            }

            lock (saveScopeLock)
            {
                return transientSaveScope;
            }
        }

        /// <summary>
        /// Compatibility accessor for callers that only need the active internal key.
        /// </summary>
        internal static string GetSaveKey()
        {
            return GetSaveScope().InternalSaveKey;
        }

        /// <summary>
        /// Returns the exact path-derived key used by the previous file-scoped layout.
        /// </summary>
        internal static string GetLegacyFileScopedSaveKey()
        {
            return GetSaveScope().InternalSaveKey;
        }

        /// <summary>
        /// Returns every historical key that may legitimately belong to this save.
        /// Empty optional candidates are omitted rather than normalized to "default".
        /// </summary>
        internal static List<string> GetPlausibleLegacySaveKeys(CoreSaveScope saveScope)
        {
            return GetPlausibleLegacySaveKeys(saveScope, null);
        }

        /// <summary>
        /// Returns historical keys using PlayerData read from the target vanilla save
        /// when it is available during LoadEvent processing.
        /// </summary>
        internal static List<string> GetPlausibleLegacySaveKeys(
            CoreSaveScope saveScope,
            staticVars._playerData loadedPlayerData)
        {
            List<string> saveKeys = new List<string>();
            if (saveScope != null)
            {
                AddUniqueToken(saveKeys, saveScope.InternalSaveKey);
                AddUniqueToken(saveKeys, saveScope.LegacyOwnerSaveKey);

                if (saveScope.IsTransient)
                {
                    return saveKeys;
                }
            }

            List<string> agencySaveKeys = GetLegacyAgencySaveKeys(
                loadedPlayerData);
            for (
                int agencyKeyIndex = CoreConstants.ZeroBasedListStartIndex;
                agencyKeyIndex < agencySaveKeys.Count;
                agencyKeyIndex++)
            {
                AddUniqueToken(saveKeys, agencySaveKeys[agencyKeyIndex]);
            }

            return saveKeys;
        }

        /// <summary>
        /// Removes one temporary pre-save directory after its contents have been
        /// successfully cloned into a canonical mirrored save directory.
        /// </summary>
        internal static bool TryDeleteTransientSaveDirectory(
            string saveDirectory,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrEmpty(saveDirectory))
            {
                return true;
            }

            try
            {
                string transientRootDirectory = Path.GetFullPath(
                    Path.Combine(
                        GetSavesRootDirectory(),
                        "_transient")).TrimEnd(
                            Path.DirectorySeparatorChar,
                            Path.AltDirectorySeparatorChar);

                string normalizedSaveDirectory = Path.GetFullPath(
                    saveDirectory).TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);

                string transientRootPrefix = transientRootDirectory +
                    Path.DirectorySeparatorChar;

                if (!normalizedSaveDirectory.StartsWith(
                    transientRootPrefix,
                    StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage =
                        "Refused to delete a non-transient save directory.";
                    return false;
                }

                if (Directory.Exists(normalizedSaveDirectory))
                {
                    Directory.Delete(normalizedSaveDirectory, true);
                }

                try
                {
                    if (Directory.Exists(transientRootDirectory) &&
                        Directory.GetFileSystemEntries(
                            transientRootDirectory).Length == 0)
                    {
                        Directory.Delete(transientRootDirectory, false);
                    }
                }
                catch
                {
                    // Removing the now-empty parent is cosmetic and best-effort.
                }

                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// Removes one abandoned transaction directory after its engine is closed.
        /// The containment check prevents cleanup from ever reaching canonical saves,
        /// legacy migration sources, or vanilla data.
        /// </summary>
        internal static bool TryDeleteStagingSaveDirectory(
            string saveDirectory,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrEmpty(saveDirectory))
            {
                return true;
            }

            try
            {
                string transactionRootDirectory = Path.GetFullPath(
                    Path.Combine(
                        GetSavesRootDirectory(),
                        TransactionFolderName)).TrimEnd(
                            Path.DirectorySeparatorChar,
                            Path.AltDirectorySeparatorChar);

                string normalizedSaveDirectory = Path.GetFullPath(
                    saveDirectory).TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);

                string transactionRootPrefix = transactionRootDirectory +
                    Path.DirectorySeparatorChar;
                if (!normalizedSaveDirectory.StartsWith(
                    transactionRootPrefix,
                    StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage =
                        "Refused to delete a non-transaction save directory.";
                    return false;
                }

                if (Directory.Exists(normalizedSaveDirectory))
                {
                    Directory.Delete(normalizedSaveDirectory, true);
                }

                try
                {
                    if (Directory.Exists(transactionRootDirectory) &&
                        Directory.GetFileSystemEntries(
                            transactionRootDirectory).Length == 0)
                    {
                        Directory.Delete(transactionRootDirectory, false);
                    }
                }
                catch
                {
                    // Removing the now-empty parent is cosmetic and best-effort.
                }

                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// Returns both historical PlayerData-derived agency keys. Older builds used
        /// the identity fallback before SaveFolderName was assigned, so both must be tried.
        /// </summary>
        internal static List<string> GetLegacyAgencySaveKeys()
        {
            return GetLegacyAgencySaveKeys(null);
        }

        private static List<string> GetLegacyAgencySaveKeys(
            staticVars._playerData loadedPlayerData)
        {
            List<string> saveKeys = new List<string>();
            try
            {
                staticVars._playerData playerData =
                    loadedPlayerData ?? staticVars.PlayerData;
                if (playerData == null)
                {
                    return saveKeys;
                }

                string saveFolderName = playerData.SaveFolderName;

                AddUniqueToken(
                    saveKeys,
                    CoreTokenUtility.SanitizeToken(
                        saveFolderName,
                        CoreConstants.SaveTokenMaximumLength));

                string identityKeySource = string.Join(
                    CoreConstants.SaveKeyJoinSeparator,
                    new string[]
                    {
                        playerData.IsStoryMode
                            ? CoreConstants.SaveModeStory
                            : CoreConstants.SaveModeFreePlay,
                        playerData.FirstName ?? string.Empty,
                        playerData.LastName ?? string.Empty,
                        playerData.GroupName ?? string.Empty,
                        playerData.Chapter.ToString()
                    });

                AddUniqueToken(
                    saveKeys,
                    CoreTokenUtility.SanitizeToken(
                        identityKeySource,
                        CoreConstants.SaveKeyMaximumLength));
            }
            catch
            {
                // Historical migration probing is best-effort.
            }

            return saveKeys;
        }

        /// <summary>
        /// Returns every current and historical install-root directory that may contain
        /// keyed storage from an older IM Data Core build.
        /// </summary>
        internal static List<string> GetStorageSourceDirectories(string saveKey)
        {
            List<string> directories = new List<string>();
            string normalizedCandidateKey = NormalizeOptionalSaveKey(saveKey);
            if (string.IsNullOrEmpty(normalizedCandidateKey))
            {
                return directories;
            }

            AddLegacyKeyedDirectories(
                directories,
                GetRootDirectory(),
                normalizedCandidateKey);

            string localModsDirectory = Path.Combine(
                Application.persistentDataPath,
                CoreConstants.ModsFolderName);

            AddLegacyKeyedDirectories(
                directories,
                Path.Combine(localModsDirectory, CoreConstants.ModFolderName),
                normalizedCandidateKey);

            AddLegacyKeyedDirectories(
                directories,
                Path.Combine(localModsDirectory, LegacyDisplayModFolderName),
                normalizedCandidateKey);

            try
            {
                string assemblyDirectory = Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location);
                AddLegacyKeyedDirectories(
                    directories,
                    assemblyDirectory,
                    normalizedCandidateKey);
            }
            catch
            {
                // Assembly-location probing is best-effort.
            }

            AddLoadedModInstallDirectories(
                directories,
                normalizedCandidateKey);

            AddKnownWorkshopInstallDirectory(
                directories,
                normalizedCandidateKey);

            return directories;
        }

        /// <summary>
        /// Returns the mirrored directory for a save scope.
        /// </summary>
        internal static string GetSaveDirectory(CoreSaveScope saveScope)
        {
            if (saveScope == null ||
                string.IsNullOrEmpty(saveScope.StorageRelativeDirectory))
            {
                return Path.Combine(
                    GetSavesRootDirectory(),
                    CoreConstants.DefaultSaveKey);
            }

            string savesRootDirectory = GetSavesRootDirectory();
            string candidateDirectory = Path.Combine(
                savesRootDirectory,
                saveScope.StorageRelativeDirectory);

            try
            {
                string normalizedSavesRoot = Path.GetFullPath(
                    savesRootDirectory).TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);

                string normalizedCandidate = Path.GetFullPath(
                    candidateDirectory).TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);

                string savesRootPrefix = normalizedSavesRoot +
                    Path.DirectorySeparatorChar;

                if (normalizedCandidate.StartsWith(
                    savesRootPrefix,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return normalizedCandidate;
                }
            }
            catch
            {
                // Return the safe fallback below.
            }

            return Path.Combine(
                savesRootDirectory,
                CoreConstants.DefaultSaveKey);
        }

        /// <summary>
        /// Returns the full save-scoped SQLite file path.
        /// </summary>
        internal static string GetDatabasePath(CoreSaveScope saveScope)
        {
            return Path.Combine(
                GetSaveDirectory(saveScope),
                CoreConstants.DatabaseFileName);
        }

        /// <summary>
        /// Returns the full save-scoped flat-file fallback path.
        /// </summary>
        internal static string GetFlatFileDatabasePath(CoreSaveScope saveScope)
        {
            return Path.Combine(
                GetSaveDirectory(saveScope),
                CoreConstants.FlatFileDatabaseFileName);
        }

        /// <summary>
        /// Creates a scope only for canonical save paths contained by the vanilla data root.
        /// </summary>
        private static bool TryCreateSaveScope(
            string saveFilePath,
            out CoreSaveScope saveScope)
        {
            saveScope = null;
            string normalizedSaveFilePath = NormalizeSaveFilePath(saveFilePath);
            if (string.IsNullOrEmpty(normalizedSaveFilePath))
            {
                return false;
            }

            string dataRootDirectory;
            try
            {
                dataRootDirectory = Path.GetFullPath(
                    Path.Combine(
                        Application.persistentDataPath,
                        GameDataRootFolderName)).TrimEnd(
                            Path.DirectorySeparatorChar,
                            Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return false;
            }

            string dataRootPrefix = dataRootDirectory +
                Path.DirectorySeparatorChar;

            if (!normalizedSaveFilePath.StartsWith(
                dataRootPrefix,
                StringComparison.OrdinalIgnoreCase))
            {
                CoreLog.Warn(
                    CoreConstants.MessageSaveKeyDerivationFailurePrefix +
                    "Save file is outside the vanilla data directory: " +
                    normalizedSaveFilePath);
                return false;
            }

            string relativeSaveFilePath = normalizedSaveFilePath.Substring(
                dataRootPrefix.Length);

            if (!IsSupportedVanillaSaveRelativePath(relativeSaveFilePath))
            {
                return false;
            }

            string storageRelativeDirectory;
            if (!TryBuildStorageRelativeDirectory(
                relativeSaveFilePath,
                out storageRelativeDirectory))
            {
                return false;
            }

            string internalSaveKey = BuildFileScopedSaveKey(
                normalizedSaveFilePath);
            if (internalSaveKey.Length < CoreConstants.SaveKeyMinimumLength)
            {
                return false;
            }

            saveScope = new CoreSaveScope
            {
                SaveFilePath = normalizedSaveFilePath,
                StorageRelativeDirectory = storageRelativeDirectory,
                InternalSaveKey = internalSaveKey,
                LegacyOwnerSaveKey = BuildLegacyOwnerSaveKey(
                    normalizedSaveFilePath,
                    dataRootDirectory)
            };

            return true;
        }

        /// <summary>
        /// Restricts sidecars to the file shapes written by vanilla. This is also a
        /// hard exclusion for data/global_data.json, even if a future caller passes it
        /// to the scope resolver accidentally.
        /// </summary>
        private static bool IsSupportedVanillaSaveRelativePath(
            string relativeSaveFilePath)
        {
            if (string.IsNullOrWhiteSpace(relativeSaveFilePath) ||
                Path.IsPathRooted(relativeSaveFilePath))
            {
                return false;
            }

            string normalizedRelativePath = relativeSaveFilePath
                .Replace(
                    Path.AltDirectorySeparatorChar,
                    Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);
            if (!AreRelativePathSegmentsSafe(normalizedRelativePath))
            {
                return false;
            }

            string[] pathSegments = normalizedRelativePath.Split(
                new char[]
                {
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                },
                StringSplitOptions.RemoveEmptyEntries);
            if (pathSegments.Length <= CoreConstants.ZeroBasedListStartIndex)
            {
                return false;
            }

            string fileName = pathSegments[pathSegments.Length - 1];
            if (string.Equals(
                fileName,
                GlobalDataFileName,
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (pathSegments.Length == 1)
            {
                return IsDirectSaveFileName(fileName);
            }

            if (pathSegments.Length == 3 &&
                string.Equals(
                    pathSegments[0],
                    ManualSavesFolderName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return IsSaveJsonFileName(fileName);
            }

            if (!string.Equals(
                pathSegments[0],
                StoryModeFolderName,
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (pathSegments.Length == 3)
            {
                return IsDirectSaveFileName(fileName);
            }

            if (pathSegments.Length == 5 &&
                string.Equals(
                    pathSegments[2],
                    ManualSavesFolderName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return IsSaveJsonFileName(fileName);
            }

            if (pathSegments.Length == 4 &&
                IsStoryChapterFolderName(pathSegments[2]))
            {
                return IsSaveJsonFileName(fileName);
            }

            return false;
        }

        private static bool IsDirectSaveFileName(string fileName)
        {
            return string.Equals(
                    fileName,
                    AutoSaveFileName + SaveFileExtension,
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    fileName,
                    ManualSaveFileName + SaveFileExtension,
                    StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSaveJsonFileName(string fileName)
        {
            return string.Equals(
                fileName,
                SaveFileName + SaveFileExtension,
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

        /// <summary>
        /// Mirrors the vanilla relative path. A terminal save.json is represented by
        /// its owner directory; direct auto_save/manual_save files retain their stems.
        /// </summary>
        private static bool TryBuildStorageRelativeDirectory(
            string relativeSaveFilePath,
            out string storageRelativeDirectory)
        {
            storageRelativeDirectory = string.Empty;
            if (string.IsNullOrWhiteSpace(relativeSaveFilePath) ||
                Path.IsPathRooted(relativeSaveFilePath))
            {
                return false;
            }

            string normalizedRelativePath = relativeSaveFilePath
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            if (!AreRelativePathSegmentsSafe(normalizedRelativePath))
            {
                return false;
            }

            string fileName = Path.GetFileName(normalizedRelativePath);
            string relativeOwnerDirectory = Path.GetDirectoryName(
                normalizedRelativePath) ?? string.Empty;

            if (string.Equals(
                fileName,
                SaveFileName + SaveFileExtension,
                StringComparison.OrdinalIgnoreCase))
            {
                storageRelativeDirectory = relativeOwnerDirectory;
            }
            else
            {
                string fileStem = Path.GetFileNameWithoutExtension(fileName);
                storageRelativeDirectory = string.IsNullOrEmpty(
                    relativeOwnerDirectory)
                        ? fileStem
                        : Path.Combine(relativeOwnerDirectory, fileStem);
            }

            if (string.IsNullOrEmpty(storageRelativeDirectory))
            {
                storageRelativeDirectory = SaveFileName;
            }

            return AreRelativePathSegmentsSafe(storageRelativeDirectory);
        }

        /// <summary>
        /// Rejects traversal and invalid filename segments while preserving valid case,
        /// spaces, punctuation, and vanilla folder names exactly.
        /// </summary>
        private static bool AreRelativePathSegmentsSafe(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) ||
                Path.IsPathRooted(relativePath))
            {
                return false;
            }

            char[] separators = new char[]
            {
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            };

            string[] segments = relativePath.Split(
                separators,
                StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length <= CoreConstants.ZeroBasedListStartIndex)
            {
                return false;
            }

            char[] invalidFileNameCharacters = Path.GetInvalidFileNameChars();
            for (
                int segmentIndex = CoreConstants.ZeroBasedListStartIndex;
                segmentIndex < segments.Length;
                segmentIndex++)
            {
                string segment = segments[segmentIndex];
                if (string.IsNullOrEmpty(segment) ||
                    string.Equals(segment, ".", StringComparison.Ordinal) ||
                    string.Equals(segment, "..", StringComparison.Ordinal) ||
                    segment.IndexOfAny(invalidFileNameCharacters) >=
                        CoreConstants.ZeroBasedListStartIndex)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Reconstructs the exact historical file-scoped key algorithm.
        /// </summary>
        private static string BuildFileScopedSaveKey(
            string normalizedSaveFilePath)
        {
            if (string.IsNullOrEmpty(normalizedSaveFilePath))
            {
                return string.Empty;
            }

            string normalizedLowerPath =
                normalizedSaveFilePath.ToLowerInvariant();
            string relativePath = ResolveRelativeSavePath(
                normalizedLowerPath);
            string pathTokenSource = relativePath
                .Replace(
                    Path.DirectorySeparatorChar,
                    SavePathSeparatorReplacement)
                .Replace(
                    Path.AltDirectorySeparatorChar,
                    SavePathSeparatorReplacement);
            string pathToken = CoreTokenUtility.SanitizeToken(
                pathTokenSource,
                SavePathTokenLength);
            string pathHashToken = CoreTokenUtility.SanitizeToken(
                ComputeStablePathHash(normalizedLowerPath),
                CoreConstants.SaveTokenMaximumLength);
            string joinedToken = string.Join(
                CoreConstants.SaveKeyJoinSeparator,
                new string[]
                {
                    SaveFileKeyPrefix,
                    pathToken,
                    pathHashToken
                });

            return CoreTokenUtility.SanitizeToken(
                joinedToken,
                CoreConstants.SaveKeyMaximumLength);
        }

        /// <summary>
        /// Reconstructs the immediate owner-folder key introduced by the prior draft.
        /// </summary>
        private static string BuildLegacyOwnerSaveKey(
            string normalizedSaveFilePath,
            string dataRootDirectory)
        {
            string saveDirectoryPath = Path.GetDirectoryName(
                normalizedSaveFilePath);
            if (string.IsNullOrEmpty(saveDirectoryPath))
            {
                return string.Empty;
            }

            string trimmedSaveDirectoryPath = saveDirectoryPath.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            string saveFolderName = Path.GetFileName(
                trimmedSaveDirectoryPath);

            if (string.Equals(
                trimmedSaveDirectoryPath,
                dataRootDirectory,
                StringComparison.OrdinalIgnoreCase))
            {
                saveFolderName = Path.GetFileNameWithoutExtension(
                    normalizedSaveFilePath);
            }

            return CoreTokenUtility.SanitizeToken(
                saveFolderName,
                CoreConstants.SaveKeyMaximumLength);
        }

        /// <summary>
        /// Normalizes one save-file path to an absolute JSON path.
        /// </summary>
        private static string NormalizeSaveFilePath(string saveFilePath)
        {
            if (string.IsNullOrWhiteSpace(saveFilePath))
            {
                return string.Empty;
            }

            string candidatePath = saveFilePath.Trim();
            if (!candidatePath.EndsWith(
                SaveFileExtension,
                StringComparison.OrdinalIgnoreCase))
            {
                candidatePath += SaveFileExtension;
            }

            if (!Path.IsPathRooted(candidatePath))
            {
                candidatePath = Path.Combine(
                    Application.persistentDataPath,
                    GameDataRootFolderName,
                    candidatePath);
            }

            try
            {
                return Path.GetFullPath(candidatePath);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Creates a unique temporary scope used only until DataSaver supplies an exact
        /// vanilla target for a new game.
        /// </summary>
        private static CoreSaveScope CreateTransientSaveScope()
        {
            string transientToken = Guid.NewGuid().ToString("N");
            return new CoreSaveScope
            {
                SaveFilePath = string.Empty,
                StorageRelativeDirectory = Path.Combine(
                    "_transient",
                    transientToken),
                InternalSaveKey = "transient_" + transientToken,
                LegacyOwnerSaveKey = string.Empty,
                IsTransient = true
            };
        }

        /// <summary>
        /// Resolves a save-file path relative to the vanilla data root when possible.
        /// This method deliberately matches the previous key algorithm.
        /// </summary>
        private static string ResolveRelativeSavePath(string absoluteSavePath)
        {
            if (string.IsNullOrEmpty(absoluteSavePath))
            {
                return string.Empty;
            }

            string dataRootPath;
            try
            {
                dataRootPath = Path.GetFullPath(
                    Path.Combine(
                        Application.persistentDataPath,
                        GameDataRootFolderName));
            }
            catch
            {
                return absoluteSavePath;
            }

            string dataRootPrefix = dataRootPath;
            string separatorText =
                Path.DirectorySeparatorChar.ToString();
            if (!dataRootPrefix.EndsWith(
                separatorText,
                StringComparison.Ordinal))
            {
                dataRootPrefix += separatorText;
            }

            if (absoluteSavePath.StartsWith(
                dataRootPrefix,
                StringComparison.OrdinalIgnoreCase))
            {
                return absoluteSavePath.Substring(
                    dataRootPrefix.Length);
            }

            return absoluteSavePath;
        }

        /// <summary>
        /// Computes the short deterministic hash used by the historical key format.
        /// </summary>
        private static string ComputeStablePathHash(string normalizedPath)
        {
            if (string.IsNullOrEmpty(normalizedPath))
            {
                return string.Empty;
            }

            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(normalizedPath);
                using (SHA256 hash = SHA256.Create())
                {
                    byte[] hashBytes = hash.ComputeHash(bytes);
                    if (hashBytes == null ||
                        hashBytes.Length <= CoreConstants.ZeroBasedListStartIndex)
                    {
                        return string.Empty;
                    }

                    StringBuilder builder = new StringBuilder(
                        hashBytes.Length * 2);
                    for (
                        int byteIndex = CoreConstants.ZeroBasedListStartIndex;
                        byteIndex < hashBytes.Length;
                        byteIndex++)
                    {
                        builder.Append(
                            hashBytes[byteIndex].ToString(
                                CoreConstants.ByteToLowerHexFormat,
                                CultureInfo.InvariantCulture));
                    }

                    string hashHex = builder.ToString();
                    return hashHex.Length <= SavePathHashLength
                        ? hashHex
                        : hashHex.Substring(
                            CoreConstants.ZeroBasedListStartIndex,
                            SavePathHashLength);
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Keeps an optional historical candidate empty when no key is available.
        /// </summary>
        private static string NormalizeOptionalSaveKey(string rawSaveKey)
        {
            if (string.IsNullOrEmpty(rawSaveKey))
            {
                return string.Empty;
            }

            return CoreTokenUtility.SanitizeToken(
                rawSaveKey,
                CoreConstants.SaveKeyMaximumLength);
        }

        private static void AddUniqueToken(
            List<string> tokens,
            string rawToken)
        {
            string token = NormalizeOptionalSaveKey(rawToken);
            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            for (
                int tokenIndex = CoreConstants.ZeroBasedListStartIndex;
                tokenIndex < tokens.Count;
                tokenIndex++)
            {
                if (string.Equals(
                    tokens[tokenIndex],
                    token,
                    StringComparison.Ordinal))
                {
                    return;
                }
            }

            tokens.Add(token);
        }

        private static void AddLegacyKeyedDirectories(
            List<string> directories,
            string installRootDirectory,
            string saveKey)
        {
            if (string.IsNullOrEmpty(installRootDirectory) ||
                string.IsNullOrEmpty(saveKey))
            {
                return;
            }

            AddUniqueDirectory(
                directories,
                Path.Combine(
                    installRootDirectory,
                    CoreConstants.SaveFolderName,
                    saveKey));

            AddUniqueDirectory(
                directories,
                Path.Combine(installRootDirectory, saveKey));
        }

        private static void AddLoadedModInstallDirectories(
            List<string> directories,
            string saveKey)
        {
            try
            {
                if (Mods._Mods == null)
                {
                    return;
                }

                for (
                    int modIndex = CoreConstants.ZeroBasedListStartIndex;
                    modIndex < Mods._Mods.Count;
                    modIndex++)
                {
                    Mods._mod loadedMod = Mods._Mods[modIndex];
                    if (!IsMatchingDataCoreMod(loadedMod))
                    {
                        continue;
                    }

                    AddLegacyKeyedDirectories(
                        directories,
                        loadedMod.Path,
                        saveKey);
                }
            }
            catch
            {
                // Loaded-mod migration probing is best-effort.
            }
        }

        private static bool IsMatchingDataCoreMod(Mods._mod loadedMod)
        {
            if (loadedMod == null || string.IsNullOrEmpty(loadedMod.Path))
            {
                return false;
            }

            if (string.Equals(
                    loadedMod.ModName,
                    CoreConstants.ModFolderName,
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    loadedMod.ModName,
                    LegacyDisplayModFolderName,
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    loadedMod.Title,
                    LegacyDisplayModFolderName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            try
            {
                return File.Exists(
                    Path.Combine(
                        loadedMod.Path,
                        DataCoreAssemblyFileName));
            }
            catch
            {
                return false;
            }
        }

        private static void AddKnownWorkshopInstallDirectory(
            List<string> directories,
            string saveKey)
        {
            try
            {
                DirectoryInfo currentDirectory = new DirectoryInfo(
                    Application.dataPath);
                while (currentDirectory != null &&
                    !string.Equals(
                        currentDirectory.Name,
                        SteamAppsFolderName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    currentDirectory = currentDirectory.Parent;
                }

                if (currentDirectory == null)
                {
                    return;
                }

                string workshopInstallDirectory = Path.Combine(
                    currentDirectory.FullName,
                    WorkshopFolderName,
                    WorkshopContentFolderName,
                    IdolManagerSteamApplicationIdentifier,
                    LegacyWorkshopItemIdentifier);

                AddLegacyKeyedDirectories(
                    directories,
                    workshopInstallDirectory,
                    saveKey);
            }
            catch
            {
                // Steam library probing is best-effort.
            }
        }

        private static void AddUniqueDirectory(
            List<string> directories,
            string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                return;
            }

            string normalizedDirectoryPath = directoryPath;
            try
            {
                normalizedDirectoryPath = Path.GetFullPath(
                    directoryPath).TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);
            }
            catch
            {
            }

            for (
                int directoryIndex = CoreConstants.ZeroBasedListStartIndex;
                directoryIndex < directories.Count;
                directoryIndex++)
            {
                if (string.Equals(
                    directories[directoryIndex],
                    normalizedDirectoryPath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            directories.Add(normalizedDirectoryPath);
        }
    }

    /// <summary>
    /// Detects runtime SQLite support without loading external managed dependencies.
    /// </summary>
    internal static class CoreRuntimeCapabilities
    {
        private static readonly object runtimeProbeLock = new object();
        private static bool runtimeProbeCompleted;
        private static bool runtimeSupportAvailable;
        private static string runtimeSupportErrorMessage = string.Empty;

        /// <summary>
        /// Ensures a native SQLite runtime is reachable through the OS-provided winsqlite3 library.
        /// </summary>
        internal static bool TryEnsureSqliteRuntimeSupport(
            out string errorMessage)
        {
            lock (runtimeProbeLock)
            {
                if (runtimeProbeCompleted)
                {
                    errorMessage = runtimeSupportErrorMessage;
                    return runtimeSupportAvailable;
                }

                runtimeProbeCompleted = true;

                string runtimeCheckErrorMessage;
                if (SqliteCoreStorageEngine.TryProbeRuntime(
                    out runtimeCheckErrorMessage))
                {
                    runtimeSupportAvailable = true;
                    runtimeSupportErrorMessage = string.Empty;
                    errorMessage = string.Empty;
                    return true;
                }

                runtimeSupportAvailable = false;
                runtimeSupportErrorMessage = runtimeCheckErrorMessage;
                errorMessage = runtimeSupportErrorMessage;
                return false;
            }
        }
    }
}
