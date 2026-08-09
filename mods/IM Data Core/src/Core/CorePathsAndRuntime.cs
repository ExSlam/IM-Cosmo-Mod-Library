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
        /// Returns the private directory used only for recoverable sidecar
        /// transactions. Callers must still validate every mutation path before use.
        /// </summary>
        internal static string GetTransactionRootDirectory()
        {
            return Path.Combine(
                GetSavesRootDirectory(),
                TransactionFolderName);
        }

        /// <summary>
        /// Converts one supported vanilla save path to a relative manifest value.
        /// Absolute vanilla paths are deliberately never persisted in transaction
        /// manifests.
        /// </summary>
        internal static bool TryGetVanillaSaveRelativePath(
            string saveFilePath,
            out string relativeSaveFilePath)
        {
            relativeSaveFilePath = string.Empty;
            CoreSaveScope saveScope;
            if (!TryCreateSaveScope(saveFilePath, out saveScope) ||
                saveScope == null)
            {
                return false;
            }

            try
            {
                string dataRootDirectory = Path.GetFullPath(
                    Path.Combine(
                        Application.persistentDataPath,
                        GameDataRootFolderName)).TrimEnd(
                            Path.DirectorySeparatorChar,
                            Path.AltDirectorySeparatorChar);
                string dataRootPrefix = dataRootDirectory +
                    Path.DirectorySeparatorChar;
                if (!saveScope.SaveFilePath.StartsWith(
                    dataRootPrefix,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                string candidateRelativePath = saveScope.SaveFilePath.Substring(
                    dataRootPrefix.Length);
                if (!IsSupportedVanillaSaveRelativePath(
                    candidateRelativePath))
                {
                    return false;
                }

                relativeSaveFilePath = candidateRelativePath.Replace(
                    Path.AltDirectorySeparatorChar,
                    Path.DirectorySeparatorChar);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Resolves a validated relative manifest value beneath vanilla's data root.
        /// This is the only supported recovery path; manifest-provided absolute paths
        /// are rejected.
        /// </summary>
        internal static bool TryResolveVanillaSaveRelativePath(
            string relativeSaveFilePath,
            out string saveFilePath,
            out CoreSaveScope saveScope)
        {
            saveFilePath = string.Empty;
            saveScope = null;
            if (!IsSupportedVanillaSaveRelativePath(relativeSaveFilePath))
            {
                return false;
            }

            try
            {
                string dataRootDirectory = Path.GetFullPath(
                    Path.Combine(
                        Application.persistentDataPath,
                        GameDataRootFolderName));
                string candidatePath = Path.GetFullPath(
                    Path.Combine(
                        dataRootDirectory,
                        relativeSaveFilePath));
                if (!TryCreateSaveScope(candidatePath, out saveScope) ||
                    saveScope == null)
                {
                    return false;
                }

                saveFilePath = saveScope.SaveFilePath;
                return true;
            }
            catch
            {
                saveFilePath = string.Empty;
                saveScope = null;
                return false;
            }
        }

        /// <summary>
        /// Canonicalizes one IM Data Core mutation target and rejects every existing
        /// reparse-point ancestor. Optional tree validation is required before a
        /// recursive delete so a link cannot redirect cleanup outside the private root.
        /// </summary>
        internal static bool TryValidateContainedMutationPath(
            string candidatePath,
            bool validateExistingTree,
            out string normalizedPath,
            out string errorMessage)
        {
            normalizedPath = string.Empty;
            errorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(candidatePath))
            {
                errorMessage = "The IM Data Core mutation path is empty.";
                return false;
            }

            try
            {
                string normalizedRoot = Path.GetFullPath(
                    GetRootDirectory()).TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);
                string normalizedCandidate = Path.GetFullPath(
                    candidatePath).TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);
                string rootPrefix = normalizedRoot +
                    Path.DirectorySeparatorChar;
                if (!normalizedCandidate.StartsWith(
                    rootPrefix,
                    StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage =
                        "Refused an IM Data Core mutation outside its private root.";
                    return false;
                }

                string existingPath = normalizedCandidate;
                while (!string.IsNullOrEmpty(existingPath))
                {
                    if (File.Exists(existingPath) || Directory.Exists(existingPath))
                    {
                        FileAttributes attributes = File.GetAttributes(existingPath);
                        if ((attributes & FileAttributes.ReparsePoint) != 0)
                        {
                            errorMessage =
                                "Refused an IM Data Core mutation through a reparse point: " +
                                existingPath;
                            return false;
                        }
                    }

                    if (string.Equals(
                        existingPath,
                        normalizedRoot,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    string parentPath = Path.GetDirectoryName(existingPath);
                    if (string.IsNullOrEmpty(parentPath) ||
                        string.Equals(
                            parentPath,
                            existingPath,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        errorMessage =
                            "The mutation path did not resolve beneath the IM Data Core root.";
                        return false;
                    }

                    existingPath = parentPath.TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);
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
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// Creates a contained directory and verifies it again after creation to close
        /// the ordinary missing-ancestor case.
        /// </summary>
        internal static bool TryCreateContainedDirectory(
            string directoryPath,
            out string normalizedDirectoryPath,
            out string errorMessage)
        {
            normalizedDirectoryPath = string.Empty;
            if (!TryValidateContainedMutationPath(
                directoryPath,
                false,
                out normalizedDirectoryPath,
                out errorMessage))
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(normalizedDirectoryPath);
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }

            return TryValidateContainedMutationPath(
                normalizedDirectoryPath,
                false,
                out normalizedDirectoryPath,
                out errorMessage);
        }

        /// <summary>
        /// Deletes one contained ordinary file after validating its final path and all
        /// existing ancestors. Reparse-point files are rejected by the validator.
        /// </summary>
        internal static bool TryDeleteContainedFile(
            string filePath,
            out string errorMessage)
        {
            string normalizedFilePath;
            if (!TryValidateContainedMutationPath(
                filePath,
                false,
                out normalizedFilePath,
                out errorMessage))
            {
                return false;
            }

            try
            {
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
        /// Publishes a fully prepared transaction directory with same-volume directory
        /// renames. A durable relative-path journal makes the two-rename sequence
        /// recoverable after process termination.
        /// </summary>
        internal static bool TryPublishStagingDirectory(
            string stagingDirectory,
            CoreSaveScope targetSaveScope,
            string transactionToken,
            string expectedVanillaContentIdentity,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (!IsValidLowerHexToken(transactionToken))
            {
                errorMessage = "The transaction token is invalid.";
                return false;
            }
            if (!IsValidContentIdentity(expectedVanillaContentIdentity))
            {
                errorMessage =
                    "The expected vanilla content identity is invalid.";
                return false;
            }

            string vanillaRelativeSavePath;
            if (targetSaveScope == null ||
                !TryGetVanillaSaveRelativePath(
                    targetSaveScope.SaveFilePath,
                    out vanillaRelativeSavePath))
            {
                errorMessage = "The canonical vanilla save scope is invalid.";
                return false;
            }

            string canonicalDirectory = GetSaveDirectory(targetSaveScope);
            string fingerprintErrorMessage;
            if (!TryVerifyVanillaContentIdentity(
                    targetSaveScope.SaveFilePath,
                    expectedVanillaContentIdentity,
                    out fingerprintErrorMessage))
            {
                errorMessage =
                    "The vanilla save no longer matches the sidecar stage: " +
                    fingerprintErrorMessage;
                return false;
            }

            string normalizedStagingDirectory;
            if (!TryValidateContainedMutationPath(
                stagingDirectory,
                true,
                out normalizedStagingDirectory,
                out errorMessage))
            {
                return false;
            }

            string normalizedCanonicalDirectory;
            if (!TryValidateContainedMutationPath(
                canonicalDirectory,
                false,
                out normalizedCanonicalDirectory,
                out errorMessage))
            {
                return false;
            }

            try
            {
                string pendingRecoveryErrorMessage;
                if (!TryRecoverInterruptedPublishes(
                    out pendingRecoveryErrorMessage))
                {
                    errorMessage =
                        "A prior sidecar publish must be recovered first: " +
                        pendingRecoveryErrorMessage;
                    return false;
                }

                string normalizedTransactionRoot = Path.GetFullPath(
                    GetTransactionRootDirectory()).TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);
                string transactionPrefix = normalizedTransactionRoot +
                    Path.DirectorySeparatorChar;
                string normalizedSavesRoot = Path.GetFullPath(
                    GetSavesRootDirectory()).TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);
                string savesPrefix = normalizedSavesRoot +
                    Path.DirectorySeparatorChar;
                if (!normalizedStagingDirectory.StartsWith(
                        transactionPrefix,
                        StringComparison.OrdinalIgnoreCase) ||
                    !normalizedCanonicalDirectory.StartsWith(
                        savesPrefix,
                        StringComparison.OrdinalIgnoreCase) ||
                    normalizedCanonicalDirectory.StartsWith(
                        transactionPrefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage =
                        "The sidecar publish paths are not a staging/canonical pair.";
                    return false;
                }

                string stagingDirectoryName = Path.GetFileName(
                    normalizedStagingDirectory);
                string stagingParentDirectory = Path.GetDirectoryName(
                    normalizedStagingDirectory);
                bool supportedPublishStage =
                    stagingDirectoryName.StartsWith(
                        LoadTransactionPrefix,
                        StringComparison.Ordinal) ||
                    stagingDirectoryName.StartsWith(
                        SaveTransactionPrefix,
                        StringComparison.Ordinal);
                string stageToken = supportedPublishStage
                    ? stagingDirectoryName.Substring(
                        stagingDirectoryName.IndexOf('_') + 1)
                    : string.Empty;
                if (!supportedPublishStage ||
                    !string.Equals(
                        stagingParentDirectory,
                        normalizedTransactionRoot,
                        StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(
                        stageToken,
                        transactionToken,
                        StringComparison.Ordinal) ||
                    !IsValidTransactionDirectoryName(stagingDirectoryName) ||
                    !Directory.Exists(normalizedStagingDirectory))
                {
                    errorMessage =
                        "The sidecar staging directory is missing or invalid.";
                    return false;
                }

                string canonicalParentDirectory = Path.GetDirectoryName(
                    normalizedCanonicalDirectory);
                string validatedCanonicalParent;
                if (string.IsNullOrEmpty(canonicalParentDirectory) ||
                    !TryCreateContainedDirectory(
                        canonicalParentDirectory,
                        out validatedCanonicalParent,
                        out errorMessage))
                {
                    return false;
                }

                string journalDirectory = Path.Combine(
                    normalizedTransactionRoot,
                    "publish_" + transactionToken);
                string normalizedJournalDirectory;
                if (!TryValidateContainedMutationPath(
                    journalDirectory,
                    false,
                    out normalizedJournalDirectory,
                    out errorMessage))
                {
                    return false;
                }

                if (Directory.Exists(normalizedJournalDirectory) ||
                    File.Exists(normalizedJournalDirectory))
                {
                    errorMessage =
                        "The sidecar publish journal already exists.";
                    return false;
                }

                if (!IsSupportedVanillaSaveRelativePath(
                    vanillaRelativeSavePath))
                {
                    errorMessage =
                        "The canonical sidecar relative path is invalid.";
                    return false;
                }

                if (!TryWriteStagePublishMarker(
                    normalizedStagingDirectory,
                    transactionToken,
                    vanillaRelativeSavePath,
                    stagingDirectoryName,
                    expectedVanillaContentIdentity,
                    out errorMessage))
                {
                    return false;
                }

                string validatedJournalDirectory;
                if (!TryCreateContainedDirectory(
                    normalizedJournalDirectory,
                    out validatedJournalDirectory,
                    out errorMessage))
                {
                    return false;
                }

                string backupDirectory = Path.Combine(
                    validatedJournalDirectory,
                    "prior");
                string normalizedBackupDirectory;
                if (!TryValidateContainedMutationPath(
                    backupDirectory,
                    false,
                    out normalizedBackupDirectory,
                    out errorMessage) ||
                    !TryWritePublishJournal(
                        validatedJournalDirectory,
                        transactionToken,
                        vanillaRelativeSavePath,
                        stagingDirectoryName,
                        expectedVanillaContentIdentity,
                        "prepared",
                        out errorMessage))
                {
                    return false;
                }

                bool canonicalMovedToBackup = false;
                try
                {
                    if (!TryVerifyVanillaContentIdentity(
                        targetSaveScope.SaveFilePath,
                        expectedVanillaContentIdentity,
                        out fingerprintErrorMessage))
                    {
                        string recoveryErrorMessage;
                        bool detached = TryRecoverPublishJournal(
                            validatedJournalDirectory,
                            out recoveryErrorMessage);
                        errorMessage =
                            "The vanilla save changed before sidecar publication." +
                            (detached
                                ? string.Empty
                                : " Recovery failed: " + recoveryErrorMessage);
                        return false;
                    }

                    if (Directory.Exists(normalizedCanonicalDirectory))
                    {
                        Directory.Move(
                            normalizedCanonicalDirectory,
                            normalizedBackupDirectory);
                        canonicalMovedToBackup = true;
                        if (!TryWritePublishJournal(
                            validatedJournalDirectory,
                            transactionToken,
                            vanillaRelativeSavePath,
                            stagingDirectoryName,
                            expectedVanillaContentIdentity,
                            "canonical_backed_up",
                            out errorMessage))
                        {
                            throw new IOException(errorMessage);
                        }
                    }

                    if (!TryVerifyVanillaContentIdentity(
                        targetSaveScope.SaveFilePath,
                        expectedVanillaContentIdentity,
                        out fingerprintErrorMessage))
                    {
                        string recoveryErrorMessage;
                        bool detached = TryRecoverPublishJournal(
                            validatedJournalDirectory,
                            out recoveryErrorMessage);
                        errorMessage =
                            "The vanilla save changed during sidecar publication." +
                            (detached
                                ? string.Empty
                                : " Recovery failed: " + recoveryErrorMessage);
                        return false;
                    }

                    Directory.Move(
                        normalizedStagingDirectory,
                        normalizedCanonicalDirectory);
                    if (!TryWritePublishJournal(
                        validatedJournalDirectory,
                        transactionToken,
                        vanillaRelativeSavePath,
                        stagingDirectoryName,
                        expectedVanillaContentIdentity,
                        "published",
                        out errorMessage))
                    {
                        throw new IOException(errorMessage);
                    }
                }
                catch
                {
                    // Do not perform an unjournaled compensating rename here. The
                    // durable phase plus observed canonical/stage/prior state lets the
                    // recovery matrix finish or restore unambiguously.
                    throw;
                }

                if (!TryVerifyVanillaContentIdentity(
                    targetSaveScope.SaveFilePath,
                    expectedVanillaContentIdentity,
                    out fingerprintErrorMessage))
                {
                    string recoveryErrorMessage;
                    bool detached = TryRecoverPublishJournal(
                        validatedJournalDirectory,
                        out recoveryErrorMessage);
                    errorMessage =
                        "The vanilla save changed before sidecar publication became final." +
                        (detached
                            ? string.Empty
                            : " Recovery failed: " + recoveryErrorMessage);
                    return false;
                }

                if (canonicalMovedToBackup &&
                    Directory.Exists(normalizedBackupDirectory))
                {
                    string cleanupErrorMessage;
                    if (TryValidateContainedMutationPath(
                        normalizedBackupDirectory,
                        true,
                        out normalizedBackupDirectory,
                        out cleanupErrorMessage))
                    {
                        Directory.Delete(normalizedBackupDirectory, true);
                    }
                    else
                    {
                        CoreLog.Warn(
                            "The prior sidecar backup was retained: " +
                            cleanupErrorMessage);
                        return true;
                    }
                }

                string normalizedJournalForCleanup;
                string journalCleanupError;
                if (TryValidateContainedMutationPath(
                    validatedJournalDirectory,
                    true,
                    out normalizedJournalForCleanup,
                    out journalCleanupError))
                {
                    Directory.Delete(normalizedJournalForCleanup, true);
                }
                else
                {
                    CoreLog.Warn(
                        "The completed sidecar publish journal was retained: " +
                        journalCleanupError);
                    return true;
                }

                // The journal is removed before the canonical marker. A crash in the
                // remaining window leaves only a harmless marker, never an
                // unrecoverable journal that requires a now-missing marker.
                string markerCleanupErrorMessage;
                if (!TryDeleteContainedFile(
                    Path.Combine(
                        normalizedCanonicalDirectory,
                        "publish.stage"),
                    out markerCleanupErrorMessage))
                {
                    CoreLog.Warn(
                        "The completed sidecar stage marker was retained: " +
                        markerCleanupErrorMessage);
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
        /// Recovers only validated publish journals directly beneath _transactions.
        /// The journal stores a relative canonical path, never an absolute one.
        /// </summary>
        internal static bool TryRecoverInterruptedPublishes(
            out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                string transactionRoot = GetTransactionRootDirectory();
                if (!Directory.Exists(transactionRoot))
                {
                    return true;
                }

                string normalizedTransactionRoot;
                if (!TryValidateContainedMutationPath(
                    transactionRoot,
                    false,
                    out normalizedTransactionRoot,
                    out errorMessage))
                {
                    return false;
                }

                string[] candidateDirectories = Directory.GetDirectories(
                    normalizedTransactionRoot,
                    "publish_*",
                    SearchOption.TopDirectoryOnly);
                List<string> validatedCandidateDirectories =
                    new List<string>();
                HashSet<string> canonicalTargets =
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                HashSet<string> stagingTargets =
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int candidateIndex = 0;
                    candidateIndex < candidateDirectories.Length;
                    candidateIndex++)
                {
                    string candidateDirectory;
                    if (!TryValidateContainedMutationPath(
                        candidateDirectories[candidateIndex],
                        true,
                        out candidateDirectory,
                        out errorMessage))
                    {
                        return false;
                    }

                    string directoryName = Path.GetFileName(candidateDirectory);
                    if (!IsValidTransactionDirectoryName(directoryName))
                    {
                        continue;
                    }

                    string token = directoryName.Substring(
                        "publish_".Length);
                    string journalPath = Path.Combine(
                        candidateDirectory,
                        "publish.journal");
                    string temporaryJournalPath = journalPath + "." +
                        token +
                        ".tmp";
                    if (!File.Exists(journalPath) &&
                        !File.Exists(temporaryJournalPath))
                    {
                        string priorDirectory = Path.Combine(
                            candidateDirectory,
                            "prior");
                        if (Directory.Exists(priorDirectory))
                        {
                            errorMessage =
                                "A publish journal is missing after its canonical backup was created.";
                            return false;
                        }

                        string validatedOrphanDirectory;
                        if (!TryValidateContainedMutationPath(
                            candidateDirectory,
                            true,
                            out validatedOrphanDirectory,
                            out errorMessage))
                        {
                            return false;
                        }

                        Directory.Delete(validatedOrphanDirectory, true);
                        continue;
                    }

                    string vanillaRelativeSavePath;
                    string stagingDirectoryName;
                    string expectedVanillaContentIdentity;
                    string phase;
                    if (!TryReadPublishJournal(
                        candidateDirectory,
                        token,
                        out vanillaRelativeSavePath,
                        out stagingDirectoryName,
                        out expectedVanillaContentIdentity,
                        out phase,
                        out errorMessage))
                    {
                        return false;
                    }

                    if (!canonicalTargets.Add(vanillaRelativeSavePath) ||
                        !stagingTargets.Add(stagingDirectoryName))
                    {
                        errorMessage =
                            "Conflicting sidecar publish journals target the same save or staging directory.";
                        return false;
                    }

                    validatedCandidateDirectories.Add(candidateDirectory);
                }

                for (int candidateIndex = 0;
                    candidateIndex < validatedCandidateDirectories.Count;
                    candidateIndex++)
                {
                    string recoveryErrorMessage;
                    if (!TryRecoverPublishJournal(
                        validatedCandidateDirectories[candidateIndex],
                        out recoveryErrorMessage))
                    {
                        errorMessage = recoveryErrorMessage;
                        return false;
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        private static bool TryRecoverPublishJournal(
            string journalDirectory,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            string normalizedJournalDirectory;
            if (!TryValidateContainedMutationPath(
                journalDirectory,
                true,
                out normalizedJournalDirectory,
                out errorMessage))
            {
                return false;
            }

            string directoryName = Path.GetFileName(normalizedJournalDirectory);
            string token = directoryName.Substring("publish_".Length);
            string vanillaRelativeSavePath;
            string stagingDirectoryName;
            string expectedVanillaContentIdentity;
            string phase;
            if (!TryReadPublishJournal(
                normalizedJournalDirectory,
                token,
                out vanillaRelativeSavePath,
                out stagingDirectoryName,
                out expectedVanillaContentIdentity,
                out phase,
                out errorMessage))
            {
                return false;
            }

            string vanillaSaveFilePath;
            CoreSaveScope targetSaveScope;
            if (!TryResolveVanillaSaveRelativePath(
                vanillaRelativeSavePath,
                out vanillaSaveFilePath,
                out targetSaveScope))
            {
                errorMessage =
                    "The publish journal vanilla save path is invalid.";
                return false;
            }

            string canonicalDirectory = GetSaveDirectory(targetSaveScope);
            string stagingDirectory = Path.Combine(
                GetTransactionRootDirectory(),
                stagingDirectoryName);
            string backupDirectory = Path.Combine(
                normalizedJournalDirectory,
                "prior");
            string normalizedCanonicalDirectory;
            string normalizedStagingDirectory;
            string normalizedBackupDirectory;
            if (!TryValidateContainedMutationPath(
                    canonicalDirectory,
                    false,
                    out normalizedCanonicalDirectory,
                    out errorMessage) ||
                !TryValidateContainedMutationPath(
                    stagingDirectory,
                    false,
                    out normalizedStagingDirectory,
                    out errorMessage) ||
                !TryValidateContainedMutationPath(
                    backupDirectory,
                    false,
                    out normalizedBackupDirectory,
                    out errorMessage))
            {
                return false;
            }

            bool canonicalExists = Directory.Exists(
                normalizedCanonicalDirectory);
            bool stagingExists = Directory.Exists(
                normalizedStagingDirectory);
            bool backupExists = Directory.Exists(
                normalizedBackupDirectory);

            CoreFileFingerprint currentVanillaFingerprint;
            string vanillaFingerprintError;
            bool vanillaContentMatches =
                CoreFileFingerprintUtility.TryReadStable(
                    vanillaSaveFilePath,
                    out currentVanillaFingerprint,
                    out vanillaFingerprintError) &&
                string.Equals(
                    currentVanillaFingerprint.ContentIdentity,
                    expectedVanillaContentIdentity,
                    StringComparison.Ordinal);

            if (!vanillaContentMatches)
            {
                return TryDetachPublishForVanillaMismatch(
                    normalizedJournalDirectory,
                    normalizedCanonicalDirectory,
                    normalizedStagingDirectory,
                    normalizedBackupDirectory,
                    token,
                    vanillaRelativeSavePath,
                    stagingDirectoryName,
                    expectedVanillaContentIdentity,
                    phase,
                    canonicalExists,
                    stagingExists,
                    backupExists,
                    vanillaFingerprintError,
                    out errorMessage);
            }

            string markerContainerDirectory = stagingExists
                ? normalizedStagingDirectory
                : (canonicalExists
                    ? normalizedCanonicalDirectory
                    : string.Empty);
            if (!string.IsNullOrEmpty(markerContainerDirectory) &&
                !TryValidateStagePublishMarker(
                    markerContainerDirectory,
                    token,
                    vanillaRelativeSavePath,
                    stagingDirectoryName,
                    expectedVanillaContentIdentity,
                    out errorMessage))
            {
                if (phase == "prepared" &&
                    !stagingExists &&
                    canonicalExists &&
                    !backupExists)
                {
                    // No rename could have installed the marked stage. Preserve the
                    // existing canonical and remove only the empty intent journal.
                    string validatedAbortedJournal;
                    if (!TryValidateContainedMutationPath(
                        normalizedJournalDirectory,
                        true,
                        out validatedAbortedJournal,
                        out errorMessage))
                    {
                        return false;
                    }

                    Directory.Delete(validatedAbortedJournal, true);
                    return true;
                }

                return false;
            }

            if (phase == "prepared")
            {
                if (stagingExists && canonicalExists)
                {
                    if (backupExists)
                    {
                        errorMessage =
                            "The prepared publish has both canonical and backup directories.";
                        return false;
                    }

                    Directory.Move(
                        normalizedCanonicalDirectory,
                        normalizedBackupDirectory);
                    canonicalExists = false;
                    backupExists = true;
                    if (!TryWritePublishJournal(
                        normalizedJournalDirectory,
                        token,
                        vanillaRelativeSavePath,
                        stagingDirectoryName,
                        expectedVanillaContentIdentity,
                        "canonical_backed_up",
                        out errorMessage))
                    {
                        Directory.Move(
                            normalizedBackupDirectory,
                            normalizedCanonicalDirectory);
                        return false;
                    }

                    phase = "canonical_backed_up";
                }
                else if (stagingExists && !canonicalExists && backupExists)
                {
                    // The first rename completed before the phase update became
                    // durable. Continue from the observed filesystem state.
                    phase = "canonical_backed_up";
                }
            }

            if ((phase == "prepared" ||
                 phase == "canonical_backed_up") &&
                stagingExists &&
                !canonicalExists)
            {
                string canonicalParent = Path.GetDirectoryName(
                    normalizedCanonicalDirectory);
                string ignoredDirectory;
                if (!TryCreateContainedDirectory(
                    canonicalParent,
                    out ignoredDirectory,
                    out errorMessage))
                {
                    return false;
                }

                Directory.Move(
                    normalizedStagingDirectory,
                    normalizedCanonicalDirectory);
                canonicalExists = true;
                stagingExists = false;
                if (!TryWritePublishJournal(
                    normalizedJournalDirectory,
                    token,
                    vanillaRelativeSavePath,
                    stagingDirectoryName,
                    expectedVanillaContentIdentity,
                    "published",
                    out errorMessage))
                {
                    return false;
                }

                phase = "published";
            }

            if (phase == "canonical_backed_up" &&
                canonicalExists && stagingExists)
            {
                errorMessage =
                    "The backed-up publish has conflicting canonical and staging directories.";
                return false;
            }

            if (!canonicalExists && !stagingExists && backupExists)
            {
                // The verified stage is gone, so preserve the prior canonical rather
                // than claiming a publish that cannot be proven.
                Directory.Move(
                    normalizedBackupDirectory,
                    normalizedCanonicalDirectory);
                canonicalExists = true;
                backupExists = false;
                string validatedRestoredJournal;
                if (!TryValidateContainedMutationPath(
                    normalizedJournalDirectory,
                    true,
                    out validatedRestoredJournal,
                    out errorMessage))
                {
                    return false;
                }

                Directory.Delete(validatedRestoredJournal, true);
                return true;
            }

            if (!canonicalExists || stagingExists)
            {
                errorMessage =
                    "An interrupted sidecar publish could not install its verified staging directory.";
                return false;
            }

            if (!TryValidateStagePublishMarker(
                normalizedCanonicalDirectory,
                token,
                vanillaRelativeSavePath,
                stagingDirectoryName,
                expectedVanillaContentIdentity,
                out errorMessage))
            {
                return false;
            }

            CoreFileFingerprint finalVanillaFingerprint;
            string finalFingerprintErrorMessage;
            bool finalVanillaContentMatches =
                CoreFileFingerprintUtility.TryReadStable(
                    vanillaSaveFilePath,
                    out finalVanillaFingerprint,
                    out finalFingerprintErrorMessage) &&
                string.Equals(
                    finalVanillaFingerprint.ContentIdentity,
                    expectedVanillaContentIdentity,
                    StringComparison.Ordinal);
            if (!finalVanillaContentMatches)
            {
                return TryDetachPublishForVanillaMismatch(
                    normalizedJournalDirectory,
                    normalizedCanonicalDirectory,
                    normalizedStagingDirectory,
                    normalizedBackupDirectory,
                    token,
                    vanillaRelativeSavePath,
                    stagingDirectoryName,
                    expectedVanillaContentIdentity,
                    phase,
                    canonicalExists,
                    stagingExists,
                    backupExists,
                    finalFingerprintErrorMessage,
                    out errorMessage);
            }

            if (backupExists)
            {
                string validatedBackupDirectory;
                if (!TryValidateContainedMutationPath(
                    normalizedBackupDirectory,
                    true,
                    out validatedBackupDirectory,
                    out errorMessage))
                {
                    return false;
                }

                Directory.Delete(validatedBackupDirectory, true);
            }

            string validatedJournalDirectory;
            if (!TryValidateContainedMutationPath(
                normalizedJournalDirectory,
                true,
                out validatedJournalDirectory,
                out errorMessage))
            {
                return false;
            }

            Directory.Delete(validatedJournalDirectory, true);
            if (!TryDeleteContainedFile(
                Path.Combine(
                    normalizedCanonicalDirectory,
                    "publish.stage"),
                out errorMessage))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Reverses an interrupted publish when the vanilla file no longer has the
        /// exact content identity bound into the durable journal. The prepared stage
        /// is detached for its owning save intent; it is never installed beside a
        /// different vanilla payload.
        /// </summary>
        private static bool TryDetachPublishForVanillaMismatch(
            string journalDirectory,
            string canonicalDirectory,
            string stagingDirectory,
            string backupDirectory,
            string token,
            string vanillaRelativeSavePath,
            string stagingDirectoryName,
            string expectedVanillaContentIdentity,
            string phase,
            bool canonicalExists,
            bool stagingExists,
            bool backupExists,
            string fingerprintErrorMessage,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                bool stagingHasMarker = stagingExists &&
                    TryValidateStagePublishMarker(
                        stagingDirectory,
                        token,
                        vanillaRelativeSavePath,
                        stagingDirectoryName,
                        expectedVanillaContentIdentity,
                        out errorMessage);
                bool canonicalHasMarker = canonicalExists &&
                    TryValidateStagePublishMarker(
                        canonicalDirectory,
                        token,
                        vanillaRelativeSavePath,
                        stagingDirectoryName,
                        expectedVanillaContentIdentity,
                        out errorMessage);

                if (canonicalHasMarker)
                {
                    if (stagingExists)
                    {
                        errorMessage =
                            "The stale published sidecar cannot be detached because its staging path is occupied.";
                        return false;
                    }

                    Directory.Move(canonicalDirectory, stagingDirectory);
                    canonicalExists = false;
                    stagingExists = true;
                    stagingHasMarker = true;
                }
                else if (canonicalExists &&
                    !stagingHasMarker &&
                    (phase != "prepared" || backupExists))
                {
                    errorMessage =
                        "The interrupted sidecar publish cannot identify the installed stage while the vanilla content differs.";
                    return false;
                }

                if (!canonicalExists && backupExists)
                {
                    Directory.Move(backupDirectory, canonicalDirectory);
                    canonicalExists = true;
                    backupExists = false;
                }
                else if (canonicalExists && backupExists)
                {
                    errorMessage =
                        "The interrupted sidecar publish retained both a canonical directory and its prior backup.";
                    return false;
                }

                string validatedJournalDirectory;
                if (!TryValidateContainedMutationPath(
                    journalDirectory,
                    true,
                    out validatedJournalDirectory,
                    out errorMessage))
                {
                    return false;
                }

                Directory.Delete(validatedJournalDirectory, true);
                if (stagingHasMarker)
                {
                    string markerCleanupErrorMessage;
                    if (!TryDeleteContainedFile(
                        Path.Combine(stagingDirectory, "publish.stage"),
                        out markerCleanupErrorMessage))
                    {
                        CoreLog.Warn(
                            "The detached sidecar stage marker was retained: " +
                            markerCleanupErrorMessage);
                    }
                }

                errorMessage = string.IsNullOrEmpty(fingerprintErrorMessage)
                    ? "The sidecar stage was detached because the vanilla content changed."
                    : "The sidecar stage was detached because the vanilla content could not be verified: " +
                        fingerprintErrorMessage;
                return true;
            }
            catch (Exception exception)
            {
                errorMessage =
                    "The stale sidecar publish could not be safely detached: " +
                    exception.Message;
                return false;
            }
        }

        private static bool TryWritePublishJournal(
            string journalDirectory,
            string token,
            string vanillaRelativeSavePath,
            string stagingDirectoryName,
            string expectedVanillaContentIdentity,
            string phase,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (!IsValidLowerHexToken(token) ||
                !IsSupportedVanillaSaveRelativePath(vanillaRelativeSavePath) ||
                !IsValidTransactionDirectoryName(stagingDirectoryName) ||
                !IsValidContentIdentity(expectedVanillaContentIdentity) ||
                (phase != "prepared" &&
                 phase != "canonical_backed_up" &&
                 phase != "published"))
            {
                errorMessage = "The sidecar publish journal fields are invalid.";
                return false;
            }

            string journalPath = Path.Combine(
                journalDirectory,
                "publish.journal");
            string temporaryJournalPath = journalPath + "." + token + ".tmp";
            string normalizedJournalPath;
            string normalizedTemporaryJournalPath;
            if (!TryValidateContainedMutationPath(
                    journalPath,
                    false,
                    out normalizedJournalPath,
                    out errorMessage) ||
                !TryValidateContainedMutationPath(
                    temporaryJournalPath,
                    false,
                    out normalizedTemporaryJournalPath,
                    out errorMessage))
            {
                return false;
            }

            string journalPayload = string.Join(
                "\n",
                new string[]
                {
                    "version=2",
                    "token=" + token,
                    "vanilla=" + vanillaRelativeSavePath.Replace('\\', '/'),
                    "staging=" + stagingDirectoryName,
                    "content=" + expectedVanillaContentIdentity,
                    "phase=" + phase,
                    string.Empty
                });
            string journalText = journalPayload +
                "checksum=" +
                ComputeSha256Text(journalPayload) +
                "\n";
            try
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(journalText);
                using (FileStream journalStream = new FileStream(
                    normalizedTemporaryJournalPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    journalStream.Write(bytes, 0, bytes.Length);
                    journalStream.Flush(true);
                }

                if (File.Exists(normalizedJournalPath))
                {
                    File.Replace(
                        normalizedTemporaryJournalPath,
                        normalizedJournalPath,
                        null);
                }
                else
                {
                    File.Move(
                        normalizedTemporaryJournalPath,
                        normalizedJournalPath);
                }

                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        private static bool TryWriteStagePublishMarker(
            string stagingDirectory,
            string token,
            string vanillaRelativeSavePath,
            string stagingDirectoryName,
            string expectedVanillaContentIdentity,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (!IsValidLowerHexToken(token) ||
                !IsSupportedVanillaSaveRelativePath(vanillaRelativeSavePath) ||
                !IsValidTransactionDirectoryName(stagingDirectoryName) ||
                !IsValidContentIdentity(expectedVanillaContentIdentity))
            {
                errorMessage = "The sidecar stage marker fields are invalid.";
                return false;
            }

            string markerPayload = string.Join(
                "\n",
                new string[]
                {
                    "version=2",
                    "token=" + token,
                    "vanilla=" + vanillaRelativeSavePath.Replace('\\', '/'),
                    "staging=" + stagingDirectoryName,
                    "content=" + expectedVanillaContentIdentity,
                    string.Empty
                });
            string markerText = markerPayload +
                "checksum=" +
                ComputeSha256Text(markerPayload) +
                "\n";
            string markerPath = Path.Combine(
                stagingDirectory,
                "publish.stage");
            string temporaryMarkerPath = markerPath + "." + token + ".tmp";
            string normalizedMarkerPath;
            string normalizedTemporaryMarkerPath;
            if (!TryValidateContainedMutationPath(
                    markerPath,
                    false,
                    out normalizedMarkerPath,
                    out errorMessage) ||
                !TryValidateContainedMutationPath(
                    temporaryMarkerPath,
                    false,
                    out normalizedTemporaryMarkerPath,
                    out errorMessage))
            {
                return false;
            }

            try
            {
                if (File.Exists(normalizedMarkerPath))
                {
                    return TryValidateStagePublishMarkerFile(
                        normalizedMarkerPath,
                        token,
                        vanillaRelativeSavePath,
                        stagingDirectoryName,
                        expectedVanillaContentIdentity,
                        out errorMessage);
                }

                if (!File.Exists(normalizedMarkerPath) &&
                    File.Exists(normalizedTemporaryMarkerPath))
                {
                    string temporaryValidationError;
                    if (!TryValidateStagePublishMarkerFile(
                        normalizedTemporaryMarkerPath,
                        token,
                        vanillaRelativeSavePath,
                        stagingDirectoryName,
                        expectedVanillaContentIdentity,
                        out temporaryValidationError))
                    {
                        errorMessage =
                            "The durable temporary stage marker is invalid: " +
                            temporaryValidationError;
                        return false;
                    }

                    File.Move(
                        normalizedTemporaryMarkerPath,
                        normalizedMarkerPath);
                    return true;
                }

                byte[] bytes = new UTF8Encoding(false).GetBytes(markerText);
                using (FileStream markerStream = new FileStream(
                    normalizedTemporaryMarkerPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    markerStream.Write(bytes, 0, bytes.Length);
                    markerStream.Flush(true);
                }

                if (File.Exists(normalizedMarkerPath))
                {
                    File.Replace(
                        normalizedTemporaryMarkerPath,
                        normalizedMarkerPath,
                        null);
                }
                else
                {
                    File.Move(
                        normalizedTemporaryMarkerPath,
                        normalizedMarkerPath);
                }

                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        private static bool TryValidateStagePublishMarker(
            string containerDirectory,
            string expectedToken,
            string expectedVanillaRelativeSavePath,
            string expectedStagingDirectoryName,
            string expectedVanillaContentIdentity,
            out string errorMessage)
        {
            return TryValidateStagePublishMarkerFile(
                Path.Combine(containerDirectory, "publish.stage"),
                expectedToken,
                expectedVanillaRelativeSavePath,
                expectedStagingDirectoryName,
                expectedVanillaContentIdentity,
                out errorMessage);
        }

        private static bool TryValidateStagePublishMarkerFile(
            string markerPath,
            string expectedToken,
            string expectedVanillaRelativeSavePath,
            string expectedStagingDirectoryName,
            string expectedVanillaContentIdentity,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                string normalizedMarkerPath;
                if (!TryValidateContainedMutationPath(
                    markerPath,
                    false,
                    out normalizedMarkerPath,
                    out errorMessage) ||
                    !File.Exists(normalizedMarkerPath))
                {
                    errorMessage = "The durable sidecar stage marker is missing.";
                    return false;
                }

                string[] lines = File.ReadAllLines(
                    normalizedMarkerPath,
                    new UTF8Encoding(false, true));
                Dictionary<string, string> fields =
                    new Dictionary<string, string>(StringComparer.Ordinal);
                for (int lineIndex = 0;
                    lineIndex < lines.Length;
                    lineIndex++)
                {
                    string line = lines[lineIndex];
                    int separatorIndex = line.IndexOf('=');
                    if (separatorIndex <= 0)
                    {
                        continue;
                    }

                    string key = line.Substring(0, separatorIndex);
                    string value = line.Substring(separatorIndex + 1);
                    if (fields.ContainsKey(key) ||
                        (key != "version" &&
                         key != "token" &&
                         key != "vanilla" &&
                         key != "staging" &&
                         key != "content" &&
                         key != "checksum"))
                    {
                        errorMessage = "The sidecar stage marker is malformed.";
                        return false;
                    }

                    fields.Add(key, value);
                }

                string version;
                string token;
                string vanilla;
                string staging;
                string content;
                string checksum;
                if (fields.Count != 6 ||
                    !fields.TryGetValue("version", out version) ||
                    !fields.TryGetValue("token", out token) ||
                    !fields.TryGetValue("vanilla", out vanilla) ||
                    !fields.TryGetValue("staging", out staging) ||
                    !fields.TryGetValue("content", out content) ||
                    !fields.TryGetValue("checksum", out checksum))
                {
                    errorMessage = "The sidecar stage marker is incomplete.";
                    return false;
                }

                string markerPayload = string.Join(
                    "\n",
                    new string[]
                    {
                        "version=2",
                        "token=" + token,
                        "vanilla=" + vanilla,
                        "staging=" + staging,
                        "content=" + content,
                        string.Empty
                    });
                if (version != "2" ||
                    !string.Equals(token, expectedToken, StringComparison.Ordinal) ||
                    !string.Equals(
                        vanilla.Replace('/', Path.DirectorySeparatorChar),
                        expectedVanillaRelativeSavePath,
                        StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(
                        staging,
                        expectedStagingDirectoryName,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        content,
                        expectedVanillaContentIdentity,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        checksum,
                        ComputeSha256Text(markerPayload),
                        StringComparison.Ordinal))
                {
                    errorMessage = "The sidecar stage marker identity is invalid.";
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

        private static bool TryReadPublishJournal(
            string journalDirectory,
            string expectedToken,
            out string vanillaRelativeSavePath,
            out string stagingDirectoryName,
            out string expectedVanillaContentIdentity,
            out string phase,
            out string errorMessage)
        {
            vanillaRelativeSavePath = string.Empty;
            stagingDirectoryName = string.Empty;
            expectedVanillaContentIdentity = string.Empty;
            phase = string.Empty;
            errorMessage = string.Empty;
            try
            {
                string journalPath = Path.Combine(
                    journalDirectory,
                    "publish.journal");
                string normalizedJournalPath;
                if (!TryValidateContainedMutationPath(
                    journalPath,
                    false,
                    out normalizedJournalPath,
                    out errorMessage))
                {
                    return false;
                }

                if (!File.Exists(normalizedJournalPath))
                {
                    string temporaryJournalPath = journalPath + "." +
                        expectedToken +
                        ".tmp";
                    string normalizedTemporaryJournalPath;
                    if (!TryValidateContainedMutationPath(
                        temporaryJournalPath,
                        false,
                        out normalizedTemporaryJournalPath,
                        out errorMessage) ||
                        !File.Exists(normalizedTemporaryJournalPath))
                    {
                        errorMessage =
                            "The publish journal and its durable temporary file are missing.";
                        return false;
                    }

                    // A crash may occur after Flush(true) and before the initial move.
                    // Promote only the exact token-scoped temporary file.
                    File.Move(
                        normalizedTemporaryJournalPath,
                        normalizedJournalPath);
                }

                string[] lines = File.ReadAllLines(
                    normalizedJournalPath,
                    new UTF8Encoding(false, true));
                string token = string.Empty;
                string version = string.Empty;
                string checksum = string.Empty;
                HashSet<string> observedKeys =
                    new HashSet<string>(StringComparer.Ordinal);
                for (int lineIndex = 0;
                    lineIndex < lines.Length;
                    lineIndex++)
                {
                    string line = lines[lineIndex];
                    int separatorIndex = line.IndexOf('=');
                    if (separatorIndex <= 0)
                    {
                        continue;
                    }

                    string key = line.Substring(0, separatorIndex);
                    string value = line.Substring(separatorIndex + 1);
                    if (!observedKeys.Add(key))
                    {
                        errorMessage =
                            "The publish journal contains a duplicate field.";
                        return false;
                    }

                    if (key == "version") version = value;
                    else if (key == "token") token = value;
                    else if (key == "vanilla") vanillaRelativeSavePath = value.Replace('/', Path.DirectorySeparatorChar);
                    else if (key == "staging") stagingDirectoryName = value;
                    else if (key == "content") expectedVanillaContentIdentity = value;
                    else if (key == "phase") phase = value;
                    else if (key == "checksum") checksum = value;
                    else
                    {
                        errorMessage =
                            "The publish journal contains an unknown field.";
                        return false;
                    }
                }

                string canonicalPayload = string.Join(
                    "\n",
                    new string[]
                    {
                        "version=2",
                        "token=" + token,
                        "vanilla=" + vanillaRelativeSavePath.Replace('\\', '/'),
                        "staging=" + stagingDirectoryName,
                        "content=" + expectedVanillaContentIdentity,
                        "phase=" + phase,
                        string.Empty
                    });
                bool supportedStagingName =
                    stagingDirectoryName.StartsWith(
                        LoadTransactionPrefix,
                        StringComparison.Ordinal) ||
                    stagingDirectoryName.StartsWith(
                        SaveTransactionPrefix,
                        StringComparison.Ordinal);
                string stagingToken = supportedStagingName
                    ? stagingDirectoryName.Substring(
                        stagingDirectoryName.IndexOf('_') + 1)
                    : string.Empty;
                if (version != "2" ||
                    observedKeys.Count != 7 ||
                    !string.Equals(
                        checksum,
                        ComputeSha256Text(canonicalPayload),
                        StringComparison.Ordinal) ||
                    !string.Equals(token, expectedToken, StringComparison.Ordinal) ||
                    !IsValidLowerHexToken(token) ||
                    !IsSupportedVanillaSaveRelativePath(vanillaRelativeSavePath) ||
                    !IsValidContentIdentity(expectedVanillaContentIdentity) ||
                    !supportedStagingName ||
                    !string.Equals(
                        stagingToken,
                        token,
                        StringComparison.Ordinal) ||
                    !IsValidTransactionDirectoryName(stagingDirectoryName) ||
                    (phase != "prepared" &&
                     phase != "canonical_backed_up" &&
                     phase != "published"))
                {
                    errorMessage = "The sidecar publish journal is invalid.";
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

        private static string ComputeSha256Text(string value)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(
                    new UTF8Encoding(false).GetBytes(value ?? string.Empty));
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                for (int byteIndex = 0;
                    byteIndex < hash.Length;
                    byteIndex++)
                {
                    builder.Append(hash[byteIndex].ToString(
                        "x2",
                        CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }
        }

        private static bool IsValidLowerHexToken(string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length != 32)
            {
                return false;
            }

            for (int characterIndex = 0;
                characterIndex < token.Length;
                characterIndex++)
            {
                char character = token[characterIndex];
                if (!((character >= '0' && character <= '9') ||
                    (character >= 'a' && character <= 'f')))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidContentIdentity(string contentIdentity)
        {
            if (string.IsNullOrEmpty(contentIdentity))
            {
                return false;
            }

            string[] parts = contentIdentity.Split(':');
            long contentLength;
            if (parts.Length != 3 ||
                parts[0] != "v1" ||
                !long.TryParse(
                    parts[1],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out contentLength) ||
                contentLength < 0 ||
                parts[2].Length != 64)
            {
                return false;
            }

            for (int characterIndex = 0;
                characterIndex < parts[2].Length;
                characterIndex++)
            {
                char character = parts[2][characterIndex];
                if (!((character >= '0' && character <= '9') ||
                    (character >= 'a' && character <= 'f')))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryVerifyVanillaContentIdentity(
            string vanillaSaveFilePath,
            string expectedContentIdentity,
            out string errorMessage)
        {
            CoreFileFingerprint fingerprint;
            if (!CoreFileFingerprintUtility.TryReadStable(
                vanillaSaveFilePath,
                out fingerprint,
                out errorMessage))
            {
                return false;
            }

            if (!string.Equals(
                fingerprint.ContentIdentity,
                expectedContentIdentity,
                StringComparison.Ordinal))
            {
                errorMessage =
                    "The vanilla save content identity does not match the durable sidecar intent.";
                return false;
            }

            return true;
        }

        private static bool TryValidateTreeHasNoReparsePoints(
            string rootDirectory,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            Stack<string> pendingDirectories = new Stack<string>();
            pendingDirectories.Push(rootDirectory);
            try
            {
                while (pendingDirectories.Count > 0)
                {
                    string directoryPath = pendingDirectories.Pop();
                    FileAttributes directoryAttributes =
                        File.GetAttributes(directoryPath);
                    if ((directoryAttributes & FileAttributes.ReparsePoint) != 0)
                    {
                        errorMessage =
                            "Refused recursive cleanup through a reparse point: " +
                            directoryPath;
                        return false;
                    }

                    string[] entries = Directory.GetFileSystemEntries(
                        directoryPath);
                    for (int entryIndex = 0;
                        entryIndex < entries.Length;
                        entryIndex++)
                    {
                        string entryPath = entries[entryIndex];
                        FileAttributes entryAttributes =
                            File.GetAttributes(entryPath);
                        if ((entryAttributes & FileAttributes.ReparsePoint) != 0)
                        {
                            errorMessage =
                                "Refused recursive cleanup through a reparse point: " +
                                entryPath;
                            return false;
                        }

                        if ((entryAttributes & FileAttributes.Directory) != 0)
                        {
                            pendingDirectories.Push(entryPath);
                        }
                    }
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

                string validatedSaveDirectory;
                if (!TryValidateContainedMutationPath(
                    normalizedSaveDirectory,
                    true,
                    out validatedSaveDirectory,
                    out errorMessage))
                {
                    return false;
                }

                if (Directory.Exists(validatedSaveDirectory))
                {
                    Directory.Delete(validatedSaveDirectory, true);
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


                string transactionDirectoryName = Path.GetFileName(
                    normalizedSaveDirectory);
                if (!IsValidTransactionDirectoryName(
                    transactionDirectoryName))
                {
                    errorMessage =
                        "Refused to delete an invalid transaction directory.";
                    return false;
                }

                string validatedSaveDirectory;
                if (!TryValidateContainedMutationPath(
                    normalizedSaveDirectory,
                    true,
                    out validatedSaveDirectory,
                    out errorMessage))
                {
                    return false;
                }

                if (Directory.Exists(validatedSaveDirectory))
                {
                    Directory.Delete(validatedSaveDirectory, true);
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
        /// Accepts only transaction directories created by IM Data Core itself.
        /// </summary>
        internal static bool IsValidTransactionDirectoryName(
            string directoryName)
        {
            if (string.IsNullOrEmpty(directoryName))
            {
                return false;
            }

            string token;
            if (directoryName.StartsWith(
                LoadTransactionPrefix,
                StringComparison.Ordinal))
            {
                token = directoryName.Substring(LoadTransactionPrefix.Length);
            }
            else if (directoryName.StartsWith(
                SaveTransactionPrefix,
                StringComparison.Ordinal))
            {
                token = directoryName.Substring(SaveTransactionPrefix.Length);
            }
            else if (directoryName.StartsWith(
                "backup_",
                StringComparison.Ordinal))
            {
                token = directoryName.Substring("backup_".Length);
            }
            else if (directoryName.StartsWith(
                "copy_",
                StringComparison.Ordinal))
            {
                token = directoryName.Substring("copy_".Length);
            }
            else if (directoryName.StartsWith(
                "publish_",
                StringComparison.Ordinal))
            {
                token = directoryName.Substring("publish_".Length);
            }
            else
            {
                return false;
            }

            if (token.Length != 32)
            {
                return false;
            }

            for (int characterIndex = 0;
                characterIndex < token.Length;
                characterIndex++)
            {
                char character = token[characterIndex];
                bool isLowerHex = character >= 'a' && character <= 'f';
                bool isDigit = character >= '0' && character <= '9';
                if (!isLowerHex && !isDigit)
                {
                    return false;
                }
            }

            return true;
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
