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
    /// One exact vanilla save target and its lightweight IM Data Core sidecar.
    /// The logical save key remains separate from the physical sidecar path for
    /// compatibility with the public API.
    /// </summary>
    internal sealed class CoreSaveScope
    {
        internal string SaveFilePath = string.Empty;
        internal string RelativeSavePath = string.Empty;
        internal string SidecarFilePath = string.Empty;
        internal string InternalSaveKey = "default";
        internal bool IsTransient;
    }

    /// <summary>
    /// Resolves exact vanilla save paths into sibling lightweight sidecar paths.
    /// This class never writes, moves, renames, or deletes a vanilla file.
    /// </summary>
    internal static class CorePaths
    {
        private const string GameDataRootFolderName = "data";
        private const string DataCoreRootFolderName = "IMDataCore";
        private const string SaveFileName = "save.json";
        private const string AutoSaveFileName = "auto_save.json";
        private const string ManualSaveFileName = "manual_save.json";
        private const string GlobalDataFileName = "global_data.json";
        private const string JsonFileExtension = ".json";
        private const string ManualSavesFolderName = "manual_saves";
        private const string StoryModeFolderName = "story_mode";
        private const string StoryChapterFolderPrefix = "chapter_";
        private const int FirstStoryChapterIndex = 0;
        private const int LastStoryChapterIndex = 6;
        private const string DefaultSaveKey = "default";
        private const string TransientSaveKeyPrefix = "transient_";
        private const string SaveFileKeyPrefix = "file";
        private const string SaveKeyJoinSeparator = "_";
        private const int SaveKeyMaximumLength = 64;
        private const int SaveTokenMaximumLength = 64;
        private const int SavePathTokenLength = 32;
        private const int SavePathHashLength = 16;
        private const char SavePathSeparatorReplacement = '_';

        private static readonly object SaveScopeLock = new object();
        private static string activeSaveFilePathHint = string.Empty;
        private static CoreSaveScope transientSaveScope =
            CreateTransientSaveScope();

        /// <summary>
        /// Returns the canonical vanilla data root for the running game.
        /// </summary>
        internal static string GetVanillaDataRootDirectory()
        {
            return GetVanillaDataRootDirectory(
                Application.persistentDataPath);
        }

        /// <summary>
        /// Pure overload used by path tests.
        /// </summary>
        internal static string GetVanillaDataRootDirectory(
            string persistentDataRoot)
        {
            string normalizedPersistentRoot;
            if (!TryNormalizeDirectoryPath(
                    persistentDataRoot,
                    out normalizedPersistentRoot))
            {
                return string.Empty;
            }

            return NormalizeDirectoryPathOrEmpty(
                Path.Combine(
                    normalizedPersistentRoot,
                    GameDataRootFolderName));
        }

        /// <summary>
        /// Returns the sibling IM Data Core root. There is intentionally no required
        /// "saves" layer in the current JSON-sidecar layout.
        /// </summary>
        internal static string GetRootDirectory()
        {
            return GetRootDirectory(Application.persistentDataPath);
        }

        /// <summary>
        /// Pure overload used by path tests.
        /// </summary>
        internal static string GetRootDirectory(
            string persistentDataRoot)
        {
            string normalizedPersistentRoot;
            if (!TryNormalizeDirectoryPath(
                    persistentDataRoot,
                    out normalizedPersistentRoot))
            {
                return string.Empty;
            }

            return NormalizeDirectoryPathOrEmpty(
                Path.Combine(
                    normalizedPersistentRoot,
                    DataCoreRootFolderName));
        }

        /// <summary>
        /// Reconstructs the exact path selection performed around DataSaver calls,
        /// then accepts it only when it is a supported game-save scope.
        /// </summary>
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

        /// <summary>
        /// Pure overload used by path tests.
        /// </summary>
        internal static bool TryResolveDataSaverPath(
            string persistentDataRoot,
            string dataFileName,
            bool isJson,
            bool fullPath,
            out string resolvedPath)
        {
            resolvedPath = string.Empty;
            if (string.IsNullOrWhiteSpace(dataFileName))
            {
                return false;
            }

            // DataSaver uses isJson only to choose serialization. The path is
            // exact when fullPath is true; otherwise it always appends .json.
            string candidatePath = dataFileName;
            if (!fullPath)
            {
                string dataRootDirectory =
                    GetVanillaDataRootDirectory(persistentDataRoot);
                if (string.IsNullOrEmpty(dataRootDirectory))
                {
                    return false;
                }

                candidatePath = Path.Combine(
                    dataRootDirectory,
                    dataFileName + JsonFileExtension);
            }

            CoreSaveScope saveScope;
            if (!TryCreateSaveScope(
                    persistentDataRoot,
                    candidatePath,
                    out saveScope))
            {
                return false;
            }

            resolvedPath = saveScope.SaveFilePath;
            return true;
        }

        /// <summary>
        /// Reconstructs DataSaver.loadData's path rules. Unlike saveData, the load
        /// routine always combines with the data root, appends .json, and then
        /// collapses the literal double-extension token. A rooted second argument
        /// intentionally wins under Path.Combine, matching vanilla chapter and
        /// autosave loads on Windows.
        /// </summary>
        internal static bool TryResolveDataSaverLoadPath(
            string dataFileName,
            out string resolvedPath)
        {
            return TryResolveDataSaverLoadPath(
                Application.persistentDataPath,
                dataFileName,
                out resolvedPath);
        }

        /// <summary>
        /// Pure overload used by path tests.
        /// </summary>
        internal static bool TryResolveDataSaverLoadPath(
            string persistentDataRoot,
            string dataFileName,
            out string resolvedPath)
        {
            resolvedPath = string.Empty;
            if (string.IsNullOrWhiteSpace(dataFileName))
            {
                return false;
            }

            string dataRootDirectory =
                GetVanillaDataRootDirectory(persistentDataRoot);
            if (string.IsNullOrEmpty(dataRootDirectory))
            {
                return false;
            }

            string candidatePath = Path.Combine(
                    dataRootDirectory,
                    dataFileName + JsonFileExtension)
                .Replace(
                    JsonFileExtension + JsonFileExtension,
                    JsonFileExtension);

            CoreSaveScope saveScope;
            if (!TryCreateSaveScope(
                    persistentDataRoot,
                    candidatePath,
                    out saveScope))
            {
                return false;
            }

            resolvedPath = saveScope.SaveFilePath;
            return true;
        }

        /// <summary>
        /// Returns the exact normalized path below the vanilla data root.
        /// </summary>
        internal static bool TryGetVanillaSaveRelativePath(
            string saveFilePath,
            out string relativeSaveFilePath)
        {
            return TryGetVanillaSaveRelativePath(
                Application.persistentDataPath,
                saveFilePath,
                out relativeSaveFilePath);
        }

        /// <summary>
        /// Pure overload used by path tests.
        /// </summary>
        internal static bool TryGetVanillaSaveRelativePath(
            string persistentDataRoot,
            string saveFilePath,
            out string relativeSaveFilePath)
        {
            relativeSaveFilePath = string.Empty;

            CoreSaveScope saveScope;
            if (!TryCreateSaveScope(
                    persistentDataRoot,
                    saveFilePath,
                    out saveScope))
            {
                return false;
            }

            relativeSaveFilePath = saveScope.RelativeSavePath;
            return true;
        }

        /// <summary>
        /// Resolves a supported relative vanilla save path without touching the file.
        /// </summary>
        internal static bool TryResolveVanillaSaveRelativePath(
            string relativeSaveFilePath,
            out string saveFilePath,
            out CoreSaveScope saveScope)
        {
            return TryResolveVanillaSaveRelativePath(
                Application.persistentDataPath,
                relativeSaveFilePath,
                out saveFilePath,
                out saveScope);
        }

        /// <summary>
        /// Pure overload used by path tests.
        /// </summary>
        internal static bool TryResolveVanillaSaveRelativePath(
            string persistentDataRoot,
            string relativeSaveFilePath,
            out string saveFilePath,
            out CoreSaveScope saveScope)
        {
            saveFilePath = string.Empty;
            saveScope = null;

            string normalizedRelativePath;
            if (!TryNormalizeSupportedRelativeSavePath(
                    relativeSaveFilePath,
                    out normalizedRelativePath))
            {
                return false;
            }

            string dataRootDirectory =
                GetVanillaDataRootDirectory(persistentDataRoot);
            if (string.IsNullOrEmpty(dataRootDirectory))
            {
                return false;
            }

            string candidatePath = Path.Combine(
                dataRootDirectory,
                normalizedRelativePath);

            if (!TryCreateSaveScope(
                    persistentDataRoot,
                    candidatePath,
                    out saveScope))
            {
                return false;
            }

            saveFilePath = saveScope.SaveFilePath;
            return true;
        }

        /// <summary>
        /// Resolves one physical vanilla save path without changing active state.
        /// </summary>
        internal static bool TryResolveSaveScope(
            string saveFilePath,
            out CoreSaveScope saveScope)
        {
            return TryCreateSaveScope(
                Application.persistentDataPath,
                saveFilePath,
                out saveScope);
        }

        /// <summary>
        /// Pure overload used by path tests.
        /// </summary>
        internal static bool TryResolveSaveScope(
            string persistentDataRoot,
            string saveFilePath,
            out CoreSaveScope saveScope)
        {
            return TryCreateSaveScope(
                persistentDataRoot,
                saveFilePath,
                out saveScope);
        }

        /// <summary>
        /// Creates one exact physical scope. No vanilla file is read or required to
        /// exist, which allows the same resolver to be used before a vanilla save.
        /// </summary>
        internal static bool TryCreateSaveScope(
            string saveFilePath,
            out CoreSaveScope saveScope)
        {
            return TryCreateSaveScope(
                Application.persistentDataPath,
                saveFilePath,
                out saveScope);
        }

        /// <summary>
        /// Pure overload used by path tests.
        /// </summary>
        internal static bool TryCreateSaveScope(
            string persistentDataRoot,
            string saveFilePath,
            out CoreSaveScope saveScope)
        {
            saveScope = null;
            if (string.IsNullOrWhiteSpace(saveFilePath))
            {
                return false;
            }

            string normalizedPersistentRoot;
            string dataRootDirectory;
            string dataCoreRootDirectory;
            if (!TryResolveSeparatedRoots(
                    persistentDataRoot,
                    out normalizedPersistentRoot,
                    out dataRootDirectory,
                    out dataCoreRootDirectory))
            {
                return false;
            }

            string normalizedSaveFilePath;
            try
            {
                string candidatePath = Path.IsPathRooted(saveFilePath)
                    ? saveFilePath
                    : Path.Combine(dataRootDirectory, saveFilePath);

                normalizedSaveFilePath =
                    Path.GetFullPath(candidatePath);
            }
            catch
            {
                return false;
            }

            if (!IsStrictlyContainedPath(
                    dataRootDirectory,
                    normalizedSaveFilePath))
            {
                return false;
            }

            string relativeSaveFilePath =
                normalizedSaveFilePath.Substring(
                    BuildDirectoryPrefix(
                        dataRootDirectory).Length);

            string normalizedRelativePath;
            if (!TryNormalizeSupportedRelativeSavePath(
                    relativeSaveFilePath,
                    out normalizedRelativePath))
            {
                return false;
            }

            string pathSafetyError;
            if (!TryValidateExistingPathChain(
                    dataRootDirectory,
                    normalizedSaveFilePath,
                    out pathSafetyError))
            {
                return false;
            }

            string sidecarFilePath;
            try
            {
                sidecarFilePath = Path.GetFullPath(
                    Path.Combine(
                        dataCoreRootDirectory,
                        normalizedRelativePath));
            }
            catch
            {
                return false;
            }

            if (!IsStrictlyContainedPath(
                    dataCoreRootDirectory,
                    sidecarFilePath) ||
                !TryValidateExistingPathChain(
                    dataCoreRootDirectory,
                    sidecarFilePath,
                    out pathSafetyError))
            {
                return false;
            }

            string internalSaveKey = BuildFileScopedSaveKey(
                normalizedPersistentRoot,
                normalizedSaveFilePath);
            if (string.IsNullOrEmpty(internalSaveKey))
            {
                return false;
            }

            saveScope = new CoreSaveScope
            {
                SaveFilePath = normalizedSaveFilePath,
                RelativeSavePath = normalizedRelativePath,
                SidecarFilePath = sidecarFilePath,
                InternalSaveKey = internalSaveKey,
                IsTransient = false
            };

            return true;
        }

        /// <summary>
        /// Stores an exact supported save-file hint. Invalid paths, including
        /// global_data.json, are ignored and can never replace the active scope.
        /// </summary>
        internal static void SetActiveSaveFilePathHint(
            string saveFilePath)
        {
            CoreSaveScope ignoredSaveScope;
            TrySetActiveSaveFilePathHint(
                saveFilePath,
                out ignoredSaveScope);
        }

        /// <summary>
        /// Stores an exact supported save-file hint and returns its resolved scope.
        /// </summary>
        internal static bool TrySetActiveSaveFilePathHint(
            string saveFilePath,
            out CoreSaveScope saveScope)
        {
            saveScope = null;
            if (!TryCreateSaveScope(
                    saveFilePath,
                    out saveScope))
            {
                return false;
            }

            lock (SaveScopeLock)
            {
                activeSaveFilePathHint =
                    saveScope.SaveFilePath;
            }

            return true;
        }

        /// <summary>
        /// Selects a previously resolved ordinary or transient scope. A physical
        /// scope is resolved again instead of trusting mutable path fields.
        /// </summary>
        internal static bool TryUseSaveScope(
            CoreSaveScope saveScope)
        {
            if (saveScope == null)
            {
                return false;
            }

            if (saveScope.IsTransient)
            {
                CoreSaveScope retainedTransientScope =
                    CloneScope(saveScope);
                retainedTransientScope.SaveFilePath =
                    string.Empty;
                retainedTransientScope.RelativeSavePath =
                    string.Empty;
                retainedTransientScope.SidecarFilePath =
                    string.Empty;
                retainedTransientScope.IsTransient = true;

                if (string.IsNullOrEmpty(
                        retainedTransientScope.InternalSaveKey))
                {
                    retainedTransientScope.InternalSaveKey =
                        TransientSaveKeyPrefix +
                        Guid.NewGuid().ToString("N");
                }

                lock (SaveScopeLock)
                {
                    activeSaveFilePathHint = string.Empty;
                    transientSaveScope =
                        retainedTransientScope;
                }

                return true;
            }

            CoreSaveScope resolvedSaveScope;
            if (!TryCreateSaveScope(
                    saveScope.SaveFilePath,
                    out resolvedSaveScope))
            {
                return false;
            }

            lock (SaveScopeLock)
            {
                activeSaveFilePathHint =
                    resolvedSaveScope.SaveFilePath;
            }

            return true;
        }

        /// <summary>
        /// Compatibility wrapper for callers that do not need a result.
        /// </summary>
        internal static void UseSaveScope(
            CoreSaveScope saveScope)
        {
            TryUseSaveScope(saveScope);
        }

        /// <summary>
        /// Restores a captured scope after an aborted load without any filesystem
        /// operation.
        /// </summary>
        internal static void RestoreSaveScope(
            CoreSaveScope saveScope)
        {
            TryUseSaveScope(saveScope);
        }

        /// <summary>
        /// Returns true only for supported vanilla game-save shapes. Global settings
        /// and arbitrary JSON files can never own a sidecar.
        /// </summary>
        internal static bool IsSupportedGameSavePath(
            string saveFilePath)
        {
            CoreSaveScope ignoredSaveScope;
            return TryCreateSaveScope(
                saveFilePath,
                out ignoredSaveScope);
        }

        /// <summary>
        /// Starts a fresh memory-only scope for a new game.
        /// </summary>
        internal static void ResetToTransient()
        {
            lock (SaveScopeLock)
            {
                activeSaveFilePathHint = string.Empty;
                transientSaveScope =
                    CreateTransientSaveScope();
            }
        }

        /// <summary>
        /// Compatibility name retained for lifecycle callers.
        /// </summary>
        internal static void ResetToTransientSaveScope()
        {
            ResetToTransient();
        }

        /// <summary>
        /// Returns the exact active physical scope, or the current memory-only scope
        /// before the first valid vanilla save target is known.
        /// </summary>
        internal static CoreSaveScope GetSaveScope()
        {
            string saveFilePath;
            lock (SaveScopeLock)
            {
                saveFilePath = activeSaveFilePathHint;
            }

            if (!string.IsNullOrEmpty(saveFilePath))
            {
                CoreSaveScope resolvedSaveScope;
                if (TryCreateSaveScope(
                        saveFilePath,
                        out resolvedSaveScope))
                {
                    return resolvedSaveScope;
                }
            }

            lock (SaveScopeLock)
            {
                return CloneScope(transientSaveScope);
            }
        }

        /// <summary>
        /// Returns the public logical key independently of the physical sidecar path.
        /// </summary>
        internal static string GetSaveKey()
        {
            return GetSaveScope().InternalSaveKey;
        }

        /// <summary>
        /// Canonicalizes one proposed IMDC mutation path, requires strict containment
        /// under the private root, and rejects every existing reparse-point ancestor.
        /// </summary>
        internal static bool TryValidateContainedMutationPath(
            string candidatePath,
            bool validateExistingTree,
            out string normalizedPath,
            out string errorMessage)
        {
            return TryValidateContainedMutationPath(
                Application.persistentDataPath,
                candidatePath,
                validateExistingTree,
                out normalizedPath,
                out errorMessage);
        }

        /// <summary>
        /// Pure-root overload used by tests and tooling.
        /// </summary>
        internal static bool TryValidateContainedMutationPath(
            string persistentDataRoot,
            string candidatePath,
            bool validateExistingTree,
            out string normalizedPath,
            out string errorMessage)
        {
            normalizedPath = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(candidatePath))
            {
                errorMessage =
                    "The IM Data Core mutation path is empty.";
                return false;
            }

            string normalizedPersistentRoot;
            string dataRootDirectory;
            string dataCoreRootDirectory;
            if (!TryResolveSeparatedRoots(
                    persistentDataRoot,
                    out normalizedPersistentRoot,
                    out dataRootDirectory,
                    out dataCoreRootDirectory))
            {
                errorMessage =
                    "The IM Data Core and vanilla data roots are invalid.";
                return false;
            }

            string normalizedCandidate;
            try
            {
                normalizedCandidate =
                    Path.GetFullPath(candidatePath);
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }

            if (!IsStrictlyContainedPath(
                    dataCoreRootDirectory,
                    normalizedCandidate))
            {
                errorMessage =
                    "Refused an IM Data Core mutation outside its private root.";
                return false;
            }

            if (IsSameOrContainedPath(
                    dataRootDirectory,
                    normalizedCandidate))
            {
                errorMessage =
                    "Refused an IM Data Core mutation beneath the vanilla data root.";
                return false;
            }

            if (!TryValidateExistingPathChain(
                    dataCoreRootDirectory,
                    normalizedCandidate,
                    out errorMessage))
            {
                return false;
            }

            if (validateExistingTree &&
                Directory.Exists(normalizedCandidate) &&
                !TryValidateTreeHasNoReparsePoints(
                    normalizedCandidate,
                    out errorMessage))
            {
                return false;
            }

            normalizedPath = normalizedCandidate;
            return true;
        }

        /// <summary>
        /// Creates the private IMDC root after validating it as the exact sibling of
        /// vanilla data.
        /// </summary>
        internal static bool TryEnsureRootDirectory(
            out string errorMessage)
        {
            string ignoredRoot;
            return TryEnsureRootDirectory(
                Application.persistentDataPath,
                out ignoredRoot,
                out errorMessage);
        }

        /// <summary>
        /// Pure-root overload used by tests.
        /// </summary>
        internal static bool TryEnsureRootDirectory(
            string persistentDataRoot,
            out string normalizedRootDirectory,
            out string errorMessage)
        {
            normalizedRootDirectory = string.Empty;
            errorMessage = string.Empty;

            string normalizedPersistentRoot;
            string dataRootDirectory;
            string dataCoreRootDirectory;
            if (!TryResolveSeparatedRoots(
                    persistentDataRoot,
                    out normalizedPersistentRoot,
                    out dataRootDirectory,
                    out dataCoreRootDirectory))
            {
                errorMessage =
                    "The IM Data Core and vanilla data roots are invalid.";
                return false;
            }

            if (!TryValidateExistingPathChain(
                    normalizedPersistentRoot,
                    dataCoreRootDirectory,
                    out errorMessage))
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(
                    dataCoreRootDirectory);
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }

            if (!TryValidateExistingPathChain(
                    normalizedPersistentRoot,
                    dataCoreRootDirectory,
                    out errorMessage))
            {
                return false;
            }

            normalizedRootDirectory =
                dataCoreRootDirectory;
            return true;
        }

        /// <summary>
        /// Creates a directory only at the private root or strictly below it, then
        /// validates the created path again.
        /// </summary>
        internal static bool TryCreateContainedDirectory(
            string directoryPath,
            out string normalizedDirectoryPath,
            out string errorMessage)
        {
            return TryCreateContainedDirectory(
                Application.persistentDataPath,
                directoryPath,
                out normalizedDirectoryPath,
                out errorMessage);
        }

        /// <summary>
        /// Pure-root overload used by tests.
        /// </summary>
        internal static bool TryCreateContainedDirectory(
            string persistentDataRoot,
            string directoryPath,
            out string normalizedDirectoryPath,
            out string errorMessage)
        {
            normalizedDirectoryPath = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                errorMessage =
                    "The IM Data Core directory path is empty.";
                return false;
            }

            string dataCoreRootDirectory =
                GetRootDirectory(persistentDataRoot);

            string normalizedCandidate;
            try
            {
                normalizedCandidate =
                    Path.GetFullPath(directoryPath);
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }

            if (string.Equals(
                    normalizedCandidate,
                    dataCoreRootDirectory,
                    StringComparison.OrdinalIgnoreCase))
            {
                return TryEnsureRootDirectory(
                    persistentDataRoot,
                    out normalizedDirectoryPath,
                    out errorMessage);
            }

            if (!TryValidateContainedMutationPath(
                    persistentDataRoot,
                    normalizedCandidate,
                    false,
                    out normalizedDirectoryPath,
                    out errorMessage))
            {
                return false;
            }

            string ignoredRoot;
            if (!TryEnsureRootDirectory(
                    persistentDataRoot,
                    out ignoredRoot,
                    out errorMessage))
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(
                    normalizedDirectoryPath);
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }

            return TryValidateContainedMutationPath(
                persistentDataRoot,
                normalizedDirectoryPath,
                false,
                out normalizedDirectoryPath,
                out errorMessage);
        }

        /// <summary>
        /// Creates and validates the parent directory for one physical sidecar.
        /// </summary>
        internal static bool TryEnsureSidecarParentDirectory(
            CoreSaveScope saveScope,
            out string errorMessage)
        {
            return TryEnsureSidecarParentDirectory(
                Application.persistentDataPath,
                saveScope,
                out errorMessage);
        }

        /// <summary>
        /// Pure-root overload used by tests.
        /// </summary>
        internal static bool TryEnsureSidecarParentDirectory(
            string persistentDataRoot,
            CoreSaveScope saveScope,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (saveScope == null ||
                saveScope.IsTransient ||
                string.IsNullOrEmpty(
                    saveScope.SidecarFilePath))
            {
                errorMessage =
                    "A physical IM Data Core save scope is required.";
                return false;
            }

            string normalizedSidecarPath;
            if (!TryValidateContainedMutationPath(
                    persistentDataRoot,
                    saveScope.SidecarFilePath,
                    false,
                    out normalizedSidecarPath,
                    out errorMessage))
            {
                return false;
            }

            string parentDirectory =
                Path.GetDirectoryName(
                    normalizedSidecarPath);
            if (string.IsNullOrEmpty(parentDirectory))
            {
                errorMessage =
                    "The sidecar parent directory is unavailable.";
                return false;
            }

            string normalizedParent;
            return TryCreateContainedDirectory(
                persistentDataRoot,
                parentDirectory,
                out normalizedParent,
                out errorMessage);
        }

        /// <summary>
        /// Deletes one ordinary file only after strict private-root containment and
        /// reparse-point validation.
        /// </summary>
        internal static bool TryDeleteContainedFile(
            string filePath,
            out string errorMessage)
        {
            return TryDeleteContainedFile(
                Application.persistentDataPath,
                filePath,
                out errorMessage);
        }

        /// <summary>
        /// Pure-root overload used by tests.
        /// </summary>
        internal static bool TryDeleteContainedFile(
            string persistentDataRoot,
            string filePath,
            out string errorMessage)
        {
            string normalizedFilePath;
            if (!TryValidateContainedMutationPath(
                    persistentDataRoot,
                    filePath,
                    false,
                    out normalizedFilePath,
                    out errorMessage))
            {
                return false;
            }

            try
            {
                if (Directory.Exists(
                        normalizedFilePath))
                {
                    errorMessage =
                        "Refused to delete a directory through the file API.";
                    return false;
                }

                if (File.Exists(normalizedFilePath))
                {
                    File.Delete(normalizedFilePath);
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
        /// Deletes one private subdirectory. The IMDC root itself is never accepted.
        /// Recursive deletion first validates the complete existing tree.
        /// </summary>
        internal static bool TryDeleteContainedDirectory(
            string directoryPath,
            bool recursive,
            out string errorMessage)
        {
            return TryDeleteContainedDirectory(
                Application.persistentDataPath,
                directoryPath,
                recursive,
                out errorMessage);
        }

        /// <summary>
        /// Pure-root overload used by tests.
        /// </summary>
        internal static bool TryDeleteContainedDirectory(
            string persistentDataRoot,
            string directoryPath,
            bool recursive,
            out string errorMessage)
        {
            string normalizedDirectoryPath;
            if (!TryValidateContainedMutationPath(
                    persistentDataRoot,
                    directoryPath,
                    recursive,
                    out normalizedDirectoryPath,
                    out errorMessage))
            {
                return false;
            }

            try
            {
                if (File.Exists(
                        normalizedDirectoryPath))
                {
                    errorMessage =
                        "Refused to delete a file through the directory API.";
                    return false;
                }

                if (Directory.Exists(
                        normalizedDirectoryPath))
                {
                    Directory.Delete(
                        normalizedDirectoryPath,
                        recursive);
                }

                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        private static bool TryResolveSeparatedRoots(
            string persistentDataRoot,
            out string normalizedPersistentRoot,
            out string dataRootDirectory,
            out string dataCoreRootDirectory)
        {
            normalizedPersistentRoot = string.Empty;
            dataRootDirectory = string.Empty;
            dataCoreRootDirectory = string.Empty;

            if (!TryNormalizeDirectoryPath(
                    persistentDataRoot,
                    out normalizedPersistentRoot))
            {
                return false;
            }

            dataRootDirectory =
                NormalizeDirectoryPathOrEmpty(
                    Path.Combine(
                        normalizedPersistentRoot,
                        GameDataRootFolderName));

            dataCoreRootDirectory =
                NormalizeDirectoryPathOrEmpty(
                    Path.Combine(
                        normalizedPersistentRoot,
                        DataCoreRootFolderName));

            if (string.IsNullOrEmpty(
                    dataRootDirectory) ||
                string.IsNullOrEmpty(
                    dataCoreRootDirectory) ||
                string.Equals(
                    dataRootDirectory,
                    dataCoreRootDirectory,
                    StringComparison.OrdinalIgnoreCase) ||
                IsStrictlyContainedPath(
                    dataRootDirectory,
                    dataCoreRootDirectory) ||
                IsStrictlyContainedPath(
                    dataCoreRootDirectory,
                    dataRootDirectory))
            {
                return false;
            }

            return true;
        }

        private static bool TryNormalizeSupportedRelativeSavePath(
            string relativeSaveFilePath,
            out string normalizedRelativePath)
        {
            normalizedRelativePath = string.Empty;

            if (string.IsNullOrWhiteSpace(
                    relativeSaveFilePath) ||
                Path.IsPathRooted(
                    relativeSaveFilePath))
            {
                return false;
            }

            string candidate =
                relativeSaveFilePath.Replace(
                    Path.AltDirectorySeparatorChar,
                    Path.DirectorySeparatorChar);

            candidate = candidate.TrimStart(
                Path.DirectorySeparatorChar);

            if (!AreRelativePathSegmentsSafe(
                    candidate))
            {
                return false;
            }

            string[] pathSegments =
                SplitRelativePath(candidate);

            if (!IsSupportedVanillaSaveSegments(
                    pathSegments))
            {
                return false;
            }

            normalizedRelativePath =
                string.Join(
                    Path.DirectorySeparatorChar
                        .ToString(),
                    pathSegments);

            return true;
        }

        private static bool IsSupportedVanillaSaveSegments(
            string[] pathSegments)
        {
            if (pathSegments == null ||
                pathSegments.Length == 0)
            {
                return false;
            }

            string fileName =
                pathSegments[
                    pathSegments.Length - 1];

            if (string.Equals(
                    fileName,
                    GlobalDataFileName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (pathSegments.Length == 1)
            {
                return IsDirectSaveFileName(
                    fileName);
            }

            if (pathSegments.Length == 3 &&
                string.Equals(
                    pathSegments[0],
                    ManualSavesFolderName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return IsOpaquePathSegment(
                        pathSegments[1]) &&
                    IsSaveJsonFileName(
                        fileName);
            }

            if (!string.Equals(
                    pathSegments[0],
                    StoryModeFolderName,
                    StringComparison.OrdinalIgnoreCase) ||
                pathSegments.Length < 3 ||
                !IsOpaquePathSegment(
                    pathSegments[1]))
            {
                return false;
            }

            if (pathSegments.Length == 3)
            {
                return IsDirectSaveFileName(
                    fileName);
            }

            if (pathSegments.Length == 5 &&
                string.Equals(
                    pathSegments[2],
                    ManualSavesFolderName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return IsOpaquePathSegment(
                        pathSegments[3]) &&
                    IsSaveJsonFileName(
                        fileName);
            }

            if (pathSegments.Length == 4 &&
                IsStoryChapterFolderName(
                    pathSegments[2]))
            {
                return IsSaveJsonFileName(
                    fileName);
            }

            return false;
        }

        private static bool IsDirectSaveFileName(
            string fileName)
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

        private static bool IsSaveJsonFileName(
            string fileName)
        {
            return string.Equals(
                fileName,
                SaveFileName,
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStoryChapterFolderName(
            string folderName)
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
                    folderName.Substring(
                        StoryChapterFolderPrefix.Length),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out chapterIndex) &&
                chapterIndex >=
                    FirstStoryChapterIndex &&
                chapterIndex <=
                    LastStoryChapterIndex;
        }

        private static bool IsOpaquePathSegment(
            string segment)
        {
            return !string.IsNullOrWhiteSpace(segment) &&
                !string.Equals(
                    segment,
                    ".",
                    StringComparison.Ordinal) &&
                !string.Equals(
                    segment,
                    "..",
                    StringComparison.Ordinal);
        }

        private static bool AreRelativePathSegmentsSafe(
            string relativePath)
        {
            if (string.IsNullOrWhiteSpace(
                    relativePath) ||
                Path.IsPathRooted(relativePath))
            {
                return false;
            }

            string[] segments =
                SplitRelativePath(relativePath);
            if (segments.Length == 0)
            {
                return false;
            }

            char[] invalidFileNameCharacters =
                Path.GetInvalidFileNameChars();

            for (int index = 0;
                index < segments.Length;
                index++)
            {
                string segment = segments[index];
                if (string.IsNullOrEmpty(segment) ||
                    string.Equals(
                        segment,
                        ".",
                        StringComparison.Ordinal) ||
                    string.Equals(
                        segment,
                        "..",
                        StringComparison.Ordinal) ||
                    segment.IndexOfAny(
                        invalidFileNameCharacters) >= 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static string[] SplitRelativePath(
            string relativePath)
        {
            return relativePath.Split(
                new char[]
                {
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                },
                StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool TryValidateExistingPathChain(
            string rootDirectory,
            string candidatePath,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            string normalizedRoot;
            string normalizedCandidate;
            if (!TryNormalizeDirectoryPath(
                    rootDirectory,
                    out normalizedRoot))
            {
                errorMessage =
                    "The containment root is invalid.";
                return false;
            }

            try
            {
                normalizedCandidate =
                    Path.GetFullPath(candidatePath);
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }

            if (!IsSameOrContainedPath(
                    normalizedRoot,
                    normalizedCandidate))
            {
                errorMessage =
                    "The path is outside its containment root.";
                return false;
            }

            string currentPath =
                normalizedCandidate;

            while (!string.IsNullOrEmpty(
                currentPath))
            {
                try
                {
                    if (File.Exists(currentPath) ||
                        Directory.Exists(currentPath))
                    {
                        FileAttributes attributes =
                            File.GetAttributes(
                                currentPath);

                        if ((attributes &
                                FileAttributes.ReparsePoint) != 0)
                        {
                            errorMessage =
                                "Refused a path through a reparse point: " +
                                currentPath;
                            return false;
                        }
                    }
                }
                catch (Exception exception)
                {
                    errorMessage =
                        exception.Message;
                    return false;
                }

                if (string.Equals(
                        TrimTrailingDirectorySeparators(
                            currentPath),
                        normalizedRoot,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                string parentPath =
                    Path.GetDirectoryName(
                        currentPath);

                if (string.IsNullOrEmpty(parentPath) ||
                    string.Equals(
                        parentPath,
                        currentPath,
                        StringComparison.OrdinalIgnoreCase) ||
                    !IsSameOrContainedPath(
                        normalizedRoot,
                        parentPath))
                {
                    errorMessage =
                        "The path chain escaped its containment root.";
                    return false;
                }

                currentPath =
                    TrimTrailingDirectorySeparators(
                        parentPath);
            }

            errorMessage =
                "The containment root was not reached.";
            return false;
        }

        private static bool TryValidateTreeHasNoReparsePoints(
            string rootDirectory,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                Stack<string> pendingDirectories =
                    new Stack<string>();

                pendingDirectories.Push(
                    rootDirectory);

                while (pendingDirectories.Count > 0)
                {
                    string directoryPath =
                        pendingDirectories.Pop();

                    FileAttributes directoryAttributes =
                        File.GetAttributes(
                            directoryPath);

                    if ((directoryAttributes &
                            FileAttributes.ReparsePoint) != 0)
                    {
                        errorMessage =
                            "Refused recursive access through a reparse point: " +
                            directoryPath;
                        return false;
                    }

                    string[] entries =
                        Directory.GetFileSystemEntries(
                            directoryPath);

                    for (int index = 0;
                        index < entries.Length;
                        index++)
                    {
                        string entryPath =
                            entries[index];

                        FileAttributes entryAttributes =
                            File.GetAttributes(
                                entryPath);

                        if ((entryAttributes &
                                FileAttributes.ReparsePoint) != 0)
                        {
                            errorMessage =
                                "Refused recursive access through a reparse point: " +
                                entryPath;
                            return false;
                        }

                        if ((entryAttributes &
                                FileAttributes.Directory) != 0)
                        {
                            pendingDirectories.Push(
                                entryPath);
                        }
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                errorMessage =
                    exception.Message;
                return false;
            }
        }

        private static bool IsSameOrContainedPath(
            string rootDirectory,
            string candidatePath)
        {
            string normalizedRoot =
                TrimTrailingDirectorySeparators(
                    Path.GetFullPath(
                        rootDirectory));

            string normalizedCandidate =
                TrimTrailingDirectorySeparators(
                    Path.GetFullPath(
                        candidatePath));

            return string.Equals(
                    normalizedRoot,
                    normalizedCandidate,
                    StringComparison.OrdinalIgnoreCase) ||
                normalizedCandidate.StartsWith(
                    BuildDirectoryPrefix(
                        normalizedRoot),
                    StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStrictlyContainedPath(
            string rootDirectory,
            string candidatePath)
        {
            try
            {
                string normalizedRoot =
                    TrimTrailingDirectorySeparators(
                        Path.GetFullPath(
                            rootDirectory));

                string normalizedCandidate =
                    TrimTrailingDirectorySeparators(
                        Path.GetFullPath(
                            candidatePath));

                return normalizedCandidate.StartsWith(
                    BuildDirectoryPrefix(
                        normalizedRoot),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string BuildDirectoryPrefix(
            string directoryPath)
        {
            string trimmedPath =
                TrimTrailingDirectorySeparators(
                    directoryPath);

            return trimmedPath +
                Path.DirectorySeparatorChar;
        }

        private static bool TryNormalizeDirectoryPath(
            string directoryPath,
            out string normalizedDirectoryPath)
        {
            normalizedDirectoryPath =
                string.Empty;

            if (string.IsNullOrWhiteSpace(
                    directoryPath))
            {
                return false;
            }

            try
            {
                normalizedDirectoryPath =
                    TrimTrailingDirectorySeparators(
                        Path.GetFullPath(
                            directoryPath));

                return !string.IsNullOrEmpty(
                    normalizedDirectoryPath);
            }
            catch
            {
                normalizedDirectoryPath =
                    string.Empty;
                return false;
            }
        }

        private static string NormalizeDirectoryPathOrEmpty(
            string directoryPath)
        {
            string normalizedDirectoryPath;

            return TryNormalizeDirectoryPath(
                    directoryPath,
                    out normalizedDirectoryPath)
                ? normalizedDirectoryPath
                : string.Empty;
        }

        private static string TrimTrailingDirectorySeparators(
            string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            string pathRoot =
                Path.GetPathRoot(path) ??
                string.Empty;

            int minimumLength =
                pathRoot.Length;

            int endIndex = path.Length;
            while (endIndex > minimumLength)
            {
                char character =
                    path[endIndex - 1];

                if (character !=
                        Path.DirectorySeparatorChar &&
                    character !=
                        Path.AltDirectorySeparatorChar)
                {
                    break;
                }

                endIndex--;
            }

            return endIndex == path.Length
                ? path
                : path.Substring(
                    0,
                    endIndex);
        }

        private static CoreSaveScope CreateTransientSaveScope()
        {
            return new CoreSaveScope
            {
                SaveFilePath = string.Empty,
                RelativeSavePath = string.Empty,
                SidecarFilePath = string.Empty,
                InternalSaveKey =
                    TransientSaveKeyPrefix +
                    Guid.NewGuid().ToString("N"),
                IsTransient = true
            };
        }

        private static CoreSaveScope CloneScope(
            CoreSaveScope source)
        {
            if (source == null)
            {
                return null;
            }

            return new CoreSaveScope
            {
                SaveFilePath =
                    source.SaveFilePath ??
                    string.Empty,
                RelativeSavePath =
                    source.RelativeSavePath ??
                    string.Empty,
                SidecarFilePath =
                    source.SidecarFilePath ??
                    string.Empty,
                InternalSaveKey =
                    source.InternalSaveKey ??
                    DefaultSaveKey,
                IsTransient =
                    source.IsTransient
            };
        }

        private static string BuildFileScopedSaveKey(
            string normalizedPersistentRoot,
            string normalizedSaveFilePath)
        {
            if (string.IsNullOrEmpty(
                    normalizedPersistentRoot) ||
                string.IsNullOrEmpty(
                    normalizedSaveFilePath))
            {
                return string.Empty;
            }

            string normalizedLowerPath =
                normalizedSaveFilePath
                    .ToLowerInvariant();

            string dataRootDirectory =
                NormalizeDirectoryPathOrEmpty(
                    Path.Combine(
                        normalizedPersistentRoot,
                        GameDataRootFolderName));

            string relativePath =
                normalizedLowerPath;

            if (!string.IsNullOrEmpty(
                    dataRootDirectory))
            {
                string lowerDataRoot =
                    dataRootDirectory
                        .ToLowerInvariant();

                string dataRootPrefix =
                    BuildDirectoryPrefix(
                        lowerDataRoot);

                if (normalizedLowerPath.StartsWith(
                        dataRootPrefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    relativePath =
                        normalizedLowerPath.Substring(
                            dataRootPrefix.Length);
                }
            }

            string pathTokenSource =
                relativePath
                    .Replace(
                        Path.DirectorySeparatorChar,
                        SavePathSeparatorReplacement)
                    .Replace(
                        Path.AltDirectorySeparatorChar,
                        SavePathSeparatorReplacement);

            string pathToken =
                SanitizeToken(
                    pathTokenSource,
                    SavePathTokenLength);

            string pathHashToken =
                SanitizeToken(
                    ComputeStablePathHash(
                        normalizedLowerPath),
                    SaveTokenMaximumLength);

            string joinedToken =
                string.Join(
                    SaveKeyJoinSeparator,
                    new string[]
                    {
                        SaveFileKeyPrefix,
                        pathToken,
                        pathHashToken
                    });

            return SanitizeToken(
                joinedToken,
                SaveKeyMaximumLength);
        }

        private static string ComputeStablePathHash(
            string normalizedPath)
        {
            if (string.IsNullOrEmpty(
                    normalizedPath))
            {
                return string.Empty;
            }

            try
            {
                byte[] bytes =
                    Encoding.UTF8.GetBytes(
                        normalizedPath);

                using (SHA256 hash =
                    SHA256.Create())
                {
                    byte[] hashBytes =
                        hash.ComputeHash(
                            bytes);

                    StringBuilder builder =
                        new StringBuilder(
                            hashBytes.Length * 2);

                    for (int index = 0;
                        index < hashBytes.Length;
                        index++)
                    {
                        builder.Append(
                            hashBytes[index].ToString(
                                "x2",
                                CultureInfo.InvariantCulture));
                    }

                    string hashHex =
                        builder.ToString();

                    return hashHex.Length <=
                            SavePathHashLength
                        ? hashHex
                        : hashHex.Substring(
                            0,
                            SavePathHashLength);
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string SanitizeToken(
            string rawValue,
            int maximumLength)
        {
            if (string.IsNullOrEmpty(
                    rawValue) ||
                maximumLength <= 0)
            {
                return string.Empty;
            }

            int expectedLength =
                Math.Min(
                    rawValue.Length,
                    maximumLength);

            char[] output =
                new char[expectedLength];

            int outputLength = 0;

            for (int index = 0;
                index < rawValue.Length &&
                outputLength < maximumLength;
                index++)
            {
                char character =
                    rawValue[index];

                bool isAsciiLetter =
                    (character >= 'a' &&
                        character <= 'z') ||
                    (character >= 'A' &&
                        character <= 'Z');

                bool isDigit =
                    character >= '0' &&
                    character <= '9';

                bool isPunctuation =
                    character == '_' ||
                    character == '-' ||
                    character == '.';

                if (isAsciiLetter ||
                    isDigit ||
                    isPunctuation)
                {
                    output[outputLength] =
                        character;
                    outputLength++;
                }
            }

            return outputLength == 0
                ? string.Empty
                : new string(
                    output,
                    0,
                    outputLength);
        }
    }
}
