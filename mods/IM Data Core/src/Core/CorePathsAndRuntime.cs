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

        /// <summary>
        /// Returns the root directory where IM Data Core keeps all files.
        /// </summary>
        internal static string GetRootDirectory()
        {
            return Path.Combine(Application.persistentDataPath, CoreConstants.ModsFolderName, CoreConstants.ModFolderName);
        }

        /// <summary>
        /// Returns the root directory that contains per-save subdirectories.
        /// </summary>
        internal static string GetSavesRootDirectory()
        {
            return Path.Combine(GetRootDirectory(), CoreConstants.SaveFolderName);
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
        /// Derives the active save key from game state, with safe fallbacks.
        /// </summary>
        internal static string GetSaveKey()
        {
            try
            {
                string saveFileKey = TryBuildSaveKeyFromActiveFilePathHint();
                if (saveFileKey.Length >= CoreConstants.SaveKeyMinimumLength)
                {
                    return saveFileKey;
                }

                string legacyAgencySaveKey = BuildLegacyAgencySaveKeyFromPlayerData();
                if (legacyAgencySaveKey.Length >= CoreConstants.SaveKeyMinimumLength)
                {
                    return legacyAgencySaveKey;
                }
            }
            catch (Exception exception)
            {
                CoreLog.Warn(CoreConstants.MessageSaveKeyDerivationFailurePrefix + exception.Message);
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
        /// Returns the full save-scoped directory path.
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
