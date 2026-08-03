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
    /// Save-scoped path resolution for IM Data Core.
    /// </summary>
    internal static class CorePaths
    {
        private static readonly object saveScopeLock = new object();
        private static string activeSaveFilePathHint = string.Empty;
        private const string GameDataRootFolderName = "data";
        private const string SaveFileKeyPrefix = "file";
        private const string SaveFileExtension = ".json";
        private const int SavePathHashLength = 16;
        private const int SavePathTokenLength = 32;
        private const char SavePathSeparatorReplacement = '_';
        private const string LegacyDisplayModFolderName = "IM Data Core";

        /// <summary>
        /// Returns the stable IM Data Core root beside the game's data directory.
        /// This path is independent of the Workshop or local mod installation path.
        /// </summary>
        internal static string GetRootDirectory()
        {
            string gameSaveDirectory = Path.Combine(
                Application.persistentDataPath,
                GameDataRootFolderName);

            string gameSaveParentDirectory = Path.GetDirectoryName(gameSaveDirectory);
            if (string.IsNullOrEmpty(gameSaveParentDirectory))
            {
                gameSaveParentDirectory = Application.persistentDataPath;
            }

            return Path.Combine(
                gameSaveParentDirectory,
                CoreConstants.ModFolderName);
        }

        /// <summary>
        /// The current layout stores game-save folders directly beneath IMDataCore.
        /// </summary>
        internal static string GetSavesRootDirectory()
        {
            return Path.Combine(
                GetRootDirectory(),
                CoreConstants.SaveFolderName);
        }

        /// <summary>
        /// Returns all current and historical directories from which storage may
        /// need to be copied during migration.
        /// </summary>
        internal static List<string> GetStorageSourceDirectories(string saveKey)
        {
            List<string> directories = new List<string>();

            if (string.IsNullOrEmpty(saveKey))
            {
                return directories;
            }

            // Current layout:
            // <persistent>\IMDataCore\<save-folder>
            AddUniqueDirectory(
                directories,
                Path.Combine(GetRootDirectory(), saveKey));

            // Previous persistent layout:
            // <persistent>\IMDataCore\saves\<save-key>
            AddUniqueDirectory(
                directories,
                Path.Combine(
                    GetRootDirectory(),
                    CoreConstants.SaveFolderName,
                    saveKey));

            string localModsDirectory = Path.Combine(
                Application.persistentDataPath,
                CoreConstants.ModsFolderName);

            // Older local-mod layouts, including both folder spellings.
            AddUniqueDirectory(
                directories,
                Path.Combine(
                    localModsDirectory,
                    CoreConstants.ModFolderName,
                    CoreConstants.SaveFolderName,
                    saveKey));

            AddUniqueDirectory(
                directories,
                Path.Combine(
                    localModsDirectory,
                    LegacyDisplayModFolderName,
                    CoreConstants.SaveFolderName,
                    saveKey));

            // Very old builds may have written beside the loaded DLL.
            // This covers either a Workshop installation or a local installation.
            try
            {
                string assemblyDirectory = Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location);

                if (!string.IsNullOrEmpty(assemblyDirectory))
                {
                    AddUniqueDirectory(
                        directories,
                        Path.Combine(
                            assemblyDirectory,
                            CoreConstants.SaveFolderName,
                            saveKey));

                    AddUniqueDirectory(
                        directories,
                        Path.Combine(
                            assemblyDirectory,
                            saveKey));
                }
            }
            catch
            {
                // Migration probing is best-effort.
            }

            return directories;
        }

        /// <summary>
        /// Adds a directory without introducing duplicate migration candidates.
        /// </summary>
        private static void AddUniqueDirectory(
            List<string> directories,
            string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                return;
            }

            for (
                int directoryIndex = CoreConstants.ZeroBasedListStartIndex;
                directoryIndex < directories.Count;
                directoryIndex++)
            {
                if (string.Equals(
                    directories[directoryIndex],
                    directoryPath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            directories.Add(directoryPath);
        }

        /// <summary>
        /// Stores one best-effort save-file path hint used for save-key derivation.
        /// </summary>
        internal static void SetActiveSaveFilePathHint(string saveFilePath)
        {
            string normalizedPath = NormalizeSaveFilePath(saveFilePath);
            lock (saveScopeLock)
            {
                activeSaveFilePathHint = normalizedPath;
            }
        }

        /// <summary>
        /// Derives the active save key from the actual game-save folder name.
        /// </summary>
        internal static string GetSaveKey()
        {
            try
            {
                string saveFolderKey =
                    TryBuildSaveFolderKeyFromActiveFilePathHint();

                if (saveFolderKey.Length >= CoreConstants.SaveKeyMinimumLength)
                {
                    return saveFolderKey;
                }

                string playerDataSaveKey =
                    BuildLegacyAgencySaveKeyFromPlayerData();

                if (playerDataSaveKey.Length >= CoreConstants.SaveKeyMinimumLength)
                {
                    return playerDataSaveKey;
                }
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    CoreConstants.MessageSaveKeyDerivationFailurePrefix +
                    exception.Message);
            }

            return CoreConstants.DefaultSaveKey;
        }

        /// <summary>
        /// Returns the pre-migration agency-scoped save key used by older IM Data Core builds.
        /// </summary>
        internal static string GetLegacyAgencySaveKey()
        {
            try
            {
                return BuildLegacyAgencySaveKeyFromPlayerData();
            }
            catch
            {
                return string.Empty;
            }
        }

                /// <summary>
        /// Returns the folder name that directly owns the selected game-save file.
        ///
        /// Examples:
        /// data\manual_saves\A1B2C3D4\save.json -> A1B2C3D4
        /// data\story_mode\Agency_1234\manual_save.json -> Agency_1234
        /// data\auto_save.json -> auto_save
        /// </summary>
        private static string TryBuildSaveFolderKeyFromActiveFilePathHint()
        {
            string normalizedSaveFilePath;

            lock (saveScopeLock)
            {
                normalizedSaveFilePath = activeSaveFilePathHint;
            }

            if (string.IsNullOrEmpty(normalizedSaveFilePath))
            {
                return string.Empty;
            }

            string saveDirectoryPath =
                Path.GetDirectoryName(normalizedSaveFilePath);

            if (string.IsNullOrEmpty(saveDirectoryPath))
            {
                return string.Empty;
            }

            string trimmedSaveDirectoryPath = saveDirectoryPath.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

            string saveFolderName =
                Path.GetFileName(trimmedSaveDirectoryPath);

            if (string.IsNullOrEmpty(saveFolderName))
            {
                return string.Empty;
            }

            // Free-play auto_save.json/manual_save.json may live directly in data.
            // In that case, use the save filename rather than the generic "data".
            if (string.Equals(
                saveFolderName,
                GameDataRootFolderName,
                StringComparison.OrdinalIgnoreCase))
            {
                saveFolderName =
                    Path.GetFileNameWithoutExtension(normalizedSaveFilePath);
            }

            return CoreTokenUtility.SanitizeToken(
                saveFolderName,
                CoreConstants.SaveKeyMaximumLength);
        }

        /// <summary>
        /// Reconstructs the hashed file-scoped key used by prior versions.
        /// It is retained only so their databases can be found and migrated.
        /// </summary>
        internal static string GetLegacyFileScopedSaveKey()
        {
            try
            {
                return TryBuildSaveKeyFromActiveFilePathHint();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Tries to build one stable save key from the active load/save file path hint.
        /// </summary>
        private static string TryBuildSaveKeyFromActiveFilePathHint()
        {
            string normalizedSaveFilePath;
            lock (saveScopeLock)
            {
                normalizedSaveFilePath = activeSaveFilePathHint;
            }

            if (string.IsNullOrEmpty(normalizedSaveFilePath))
            {
                return string.Empty;
            }

            string normalizedLowerPath = normalizedSaveFilePath.ToLowerInvariant();
            string relativePath = ResolveRelativeSavePath(normalizedLowerPath);
            string pathTokenSource = relativePath
                .Replace(Path.DirectorySeparatorChar, SavePathSeparatorReplacement)
                .Replace(Path.AltDirectorySeparatorChar, SavePathSeparatorReplacement);
            string pathToken = CoreTokenUtility.SanitizeToken(pathTokenSource, SavePathTokenLength);
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

            string saveKey = CoreTokenUtility.SanitizeToken(joinedToken, CoreConstants.SaveKeyMaximumLength);
            if (saveKey.Length < CoreConstants.SaveKeyMinimumLength)
            {
                return string.Empty;
            }

            return saveKey;
        }

        /// <summary>
        /// Reconstructs the legacy agency-scoped save key derivation used before file-scoped keys.
        /// </summary>
        private static string BuildLegacyAgencySaveKeyFromPlayerData()
        {
            if (staticVars.PlayerData == null)
            {
                return string.Empty;
            }

            string saveFolderName = staticVars.PlayerData.SaveFolderName;
            if (string.IsNullOrEmpty(saveFolderName) && staticVars.PlayerData.IsStoryMode)
            {
                saveFolderName = staticVars.PlayerData.GetSaveFolderName();
            }

            string folderKey = CoreTokenUtility.SanitizeToken(saveFolderName, CoreConstants.SaveTokenMaximumLength);
            if (folderKey.Length >= CoreConstants.SaveKeyMinimumLength)
            {
                return folderKey;
            }

            string joinedFallback = string.Join(
                CoreConstants.SaveKeyJoinSeparator,
                new string[]
                {
                    staticVars.PlayerData.IsStoryMode ? CoreConstants.SaveModeStory : CoreConstants.SaveModeFreePlay,
                    staticVars.PlayerData.FirstName ?? string.Empty,
                    staticVars.PlayerData.LastName ?? string.Empty,
                    staticVars.PlayerData.GroupName ?? string.Empty,
                    staticVars.PlayerData.Chapter.ToString()
                });
            string fallbackKey = CoreTokenUtility.SanitizeToken(joinedFallback, CoreConstants.SaveKeyMaximumLength);
            if (fallbackKey.Length >= CoreConstants.SaveKeyMinimumLength)
            {
                return fallbackKey;
            }

            return string.Empty;
        }

        /// <summary>
        /// Normalizes one save-file path to a canonical absolute path with extension.
        /// </summary>
        private static string NormalizeSaveFilePath(string saveFilePath)
        {
            if (string.IsNullOrWhiteSpace(saveFilePath))
            {
                return string.Empty;
            }

            string candidatePath = saveFilePath.Trim();
            if (!candidatePath.EndsWith(SaveFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                candidatePath += SaveFileExtension;
            }

            if (!Path.IsPathRooted(candidatePath))
            {
                candidatePath = Path.Combine(Application.persistentDataPath, GameDataRootFolderName, candidatePath);
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
        /// Resolves one save-file path relative to `<persistent>/data` when possible.
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
                dataRootPath = Path.GetFullPath(Path.Combine(Application.persistentDataPath, GameDataRootFolderName));
            }
            catch
            {
                return absoluteSavePath;
            }

            if (string.IsNullOrEmpty(dataRootPath))
            {
                return absoluteSavePath;
            }

            string dataRootPrefix = dataRootPath;
            string separatorText = Path.DirectorySeparatorChar.ToString();
            if (!dataRootPrefix.EndsWith(separatorText, StringComparison.Ordinal))
            {
                dataRootPrefix += separatorText;
            }

            if (absoluteSavePath.StartsWith(dataRootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return absoluteSavePath.Substring(dataRootPrefix.Length);
            }

            return absoluteSavePath;
        }

        /// <summary>
        /// Computes one short deterministic hash token for a normalized file path.
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
                    if (hashBytes == null || hashBytes.Length <= CoreConstants.ZeroBasedListStartIndex)
                    {
                        return string.Empty;
                    }

                    StringBuilder builder = new StringBuilder(hashBytes.Length * 2);
                    for (int i = CoreConstants.ZeroBasedListStartIndex; i < hashBytes.Length; i++)
                    {
                        builder.Append(hashBytes[i].ToString(CoreConstants.ByteToLowerHexFormat, CultureInfo.InvariantCulture));
                    }

                    string hashHex = builder.ToString();
                    if (hashHex.Length <= SavePathHashLength)
                    {
                        return hashHex;
                    }

                    return hashHex.Substring(CoreConstants.ZeroBasedListStartIndex, SavePathHashLength);
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Returns:
        /// <game-save-root-parent>\IMDataCore\saves\<game-save-folder-name>
        /// </summary>
        internal static string GetSaveDirectory(string saveKey)
        {
            return Path.Combine(GetSavesRootDirectory(), saveKey);
        }

        /// <summary>
        /// Returns the full save-scoped SQLite file path.
        /// </summary>
        internal static string GetDatabasePath(string saveKey)
        {
            return Path.Combine(GetSaveDirectory(saveKey), CoreConstants.DatabaseFileName);
        }

        /// <summary>
        /// Returns the full save-scoped flat-file fallback path.
        /// </summary>
        internal static string GetFlatFileDatabasePath(string saveKey)
        {
            return Path.Combine(GetSaveDirectory(saveKey), CoreConstants.FlatFileDatabaseFileName);
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
        internal static bool TryEnsureSqliteRuntimeSupport(out string errorMessage)
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
                if (SqliteCoreStorageEngine.TryProbeRuntime(out runtimeCheckErrorMessage))
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
