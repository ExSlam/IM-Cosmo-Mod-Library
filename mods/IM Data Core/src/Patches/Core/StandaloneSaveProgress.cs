using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using CosmoSaveNotifications;
using HarmonyLib;
using UnityEngine;

namespace IMDataCore
{
    internal static class IMDataCoreSaveProgressBridge
    {
        internal static bool IsSnlOwnerActive()
        {
            return OrderedTransportProviderInterop.IsSnlSaveProgressCoordinatorActive();
        }

        internal static void BeginPersistence(SaveManager.SavedData savedData)
        {
            if (savedData == null)
            {
                return;
            }

            if (IsSnlOwnerActive())
            {
                string error;
                if (!OrderedTransportProviderInterop.TryBeginSnlIMDataCorePersistence(
                        savedData,
                        out error) &&
                    !string.IsNullOrEmpty(error))
                {
                    CoreLog.Warn(
                        "IM Data Core could not report save-progress start to SNLF: " +
                        error);
                }
                return;
            }

            StandaloneSaveProgressCoordinator.BeginPersistence(savedData);
        }

        internal static void RegisterVanillaTarget(
            SaveManager.SavedData savedData,
            string vanillaPath)
        {
            if (savedData == null || IsSnlOwnerActive())
            {
                return;
            }

            StandaloneSaveProgressCoordinator.RegisterVanillaTarget(
                savedData,
                vanillaPath);
        }

        internal static void RegisterExpectedVanillaFingerprint(
            SaveManager.SavedData savedData,
            string fingerprint)
        {
            if (savedData == null || IsSnlOwnerActive())
            {
                return;
            }

            StandaloneSaveProgressCoordinator.RegisterExpectedFingerprint(
                savedData,
                fingerprint);
        }

        internal static void ReportPersistenceResult(
            SaveManager.SavedData savedData,
            bool succeeded,
            string detail)
        {
            if (savedData == null)
            {
                return;
            }

            if (IsSnlOwnerActive())
            {
                string error;
                if (!OrderedTransportProviderInterop.TryReportSnlIMDataCorePersistenceResult(
                        savedData,
                        succeeded,
                        detail,
                        out error) &&
                    !string.IsNullOrEmpty(error))
                {
                    CoreLog.Warn(
                        "IM Data Core could not report save-progress completion to SNLF: " +
                        error);
                }
                return;
            }

            StandaloneSaveProgressCoordinator.ReportPersistenceResult(
                savedData,
                succeeded,
                detail);
        }
    }


    internal sealed class StandaloneSaveTransaction
    {
        internal SaveManager.SavedData SavedData;
        internal string Path = string.Empty;
        internal string Fingerprint = string.Empty;
        internal bool BaselineExists;
        internal DateTime BaselineWriteTime;
        internal long BaselineLength;
        internal string BaselineHash = string.Empty;
        internal float Started;
        internal float LastPoll;
        internal bool StartDisplayed;
        internal bool CompanionFinished;
        internal bool CompanionSucceeded;
    }

    internal static class StandaloneSaveProgressCoordinator
    {
        private const float WriteTimeoutSeconds = 120f;
        private const float PollIntervalSeconds = 0.15f;
        private static readonly List<StandaloneSaveTransaction> Transactions =
            new List<StandaloneSaveTransaction>();

        // IMDC calls these hooks on the main thread with the detached object used
        // by DataSaver. No Popup_Save dependency: autosaves take this path too.
        internal static void BeginPersistence(SaveManager.SavedData savedData)
        {
            if (savedData == null) return;
            Transactions.Add(new StandaloneSaveTransaction
            {
                SavedData = savedData,
                Started = Time.realtimeSinceStartup
            });
        }

        private static StandaloneSaveTransaction Find(SaveManager.SavedData savedData)
        {
            for (int index = Transactions.Count - 1; index >= 0; index--)
                if (ReferenceEquals(Transactions[index].SavedData, savedData))
                    return Transactions[index];
            return null;
        }

        internal static void RegisterVanillaTarget(SaveManager.SavedData savedData, string vanillaPath)
        {
            StandaloneSaveTransaction transaction = Find(savedData);
            if (transaction == null) return;
            transaction.Path = vanillaPath ?? string.Empty;
            try
            {
                FileInfo info = new FileInfo(transaction.Path);
                transaction.BaselineExists = info.Exists;
                if (info.Exists)
                {
                    transaction.BaselineWriteTime = info.LastWriteTimeUtc;
                    transaction.BaselineLength = info.Length;
                    transaction.BaselineHash = ComputeFileHash(transaction.Path);
                }
            }
            catch (Exception exception)
            {
                CoreLog.Warn("Could not observe the pre-save file: " + exception.Message);
            }
        }

        internal static void RegisterExpectedFingerprint(SaveManager.SavedData savedData, string fingerprint)
        {
            StandaloneSaveTransaction transaction = Find(savedData);
            if (transaction != null) transaction.Fingerprint = fingerprint ?? string.Empty;
        }

        internal static void ReportPersistenceResult(SaveManager.SavedData savedData, bool succeeded, string detail)
        {
            StandaloneSaveTransaction transaction = Find(savedData);
            if (transaction == null) return;
            transaction.CompanionFinished = true;
            transaction.CompanionSucceeded = succeeded;
            // Sidecar preparation can fail before it constructs the stamp. Still
            // require the COMPLETE matching vanilla payload before claiming success.
            if (!VanillaSavedDataFingerprint.IsValid(transaction.Fingerprint))
            {
                string fingerprint;
                string error;
                bool legacy;
                if (VanillaSavedDataFingerprint.TryComputeForSavedData(
                    savedData, false, out fingerprint, out legacy, out error))
                    transaction.Fingerprint = fingerprint;
            }
        }

        internal static void PumpNotifications()
        {
            if (IMDataCoreSaveProgressBridge.IsSnlOwnerActive()) return;
            float now = Time.realtimeSinceStartup;
            for (int index = 0; index < Transactions.Count;)
            {
                StandaloneSaveTransaction transaction = Transactions[index];
                if (!transaction.StartDisplayed)
                {
                    transaction.StartDisplayed = true;
                    SaveNotifications.Show(SaveNotifications.StartedKey, mainScript.lightBlue32);
                    index++;
                    continue;
                }
                if (now - transaction.LastPoll < PollIntervalSeconds || !transaction.CompanionFinished)
                {
                    index++;
                    continue;
                }
                transaction.LastPoll = now;
                string error = string.Empty;
                bool completed = !string.IsNullOrEmpty(transaction.Path) &&
                    TryObserveCompletedVanillaWrite(transaction.Path, transaction.Fingerprint,
                        transaction.BaselineExists, transaction.BaselineWriteTime,
                        transaction.BaselineLength, transaction.BaselineHash, out error);
                if (!completed && now - transaction.Started < WriteTimeoutSeconds)
                {
                    index++;
                    continue;
                }
                SaveNotifications.Show(completed ? SaveNotifications.CompletedKey : SaveNotifications.FailedKey,
                    completed ? mainScript.green32 : mainScript.red32);
                if (!transaction.CompanionSucceeded)
                    SaveNotifications.Show(SaveNotifications.CompanionFailedKey, mainScript.red32);
                Transactions.RemoveAt(index);
            }
            SaveNotifications.Pump();
        }

        private static bool TryObserveCompletedVanillaWrite(
            string path,
            string expectedFingerprint,
            bool baselineExists,
            DateTime baselineLastWriteUtc,
            long baselineLength,
            string baselineHash,
            out string error)
        {
            error = string.Empty;
            try
            {
                FileInfo info = new FileInfo(path);
                if (!info.Exists)
                {
                    return false;
                }

                bool changed = !baselineExists ||
                    info.LastWriteTimeUtc > baselineLastWriteUtc ||
                    info.Length != baselineLength;
                string currentHash = string.Empty;
                if (!changed && !string.IsNullOrEmpty(baselineHash))
                {
                    currentHash = ComputeFileHash(path);
                    changed = !string.IsNullOrEmpty(currentHash) &&
                        !string.Equals(currentHash, baselineHash, StringComparison.Ordinal);
                }
                if (!changed)
                {
                    return false;
                }

                string json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json))
                {
                    return false;
                }

                if (!VanillaSavedDataFingerprint.IsValid(expectedFingerprint))
                {
                    return false;
                }

                SaveManager.SavedData loaded = JsonUtility.FromJson<SaveManager.SavedData>(json);
                string observedFingerprint;
                bool legacy;
                string fingerprintError = string.Empty;
                if (loaded == null ||
                    !VanillaSavedDataFingerprint.TryComputeForSavedData(
                        loaded,
                        false,
                        out observedFingerprint,
                        out legacy,
                        out fingerprintError))
                {
                    error = fingerprintError ?? string.Empty;
                    return false;
                }

                return string.Equals(
                    observedFingerprint,
                    expectedFingerprint,
                    StringComparison.Ordinal);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static string ComputeFileHash(string path)
        {
            try
            {
                using (FileStream stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete))
                using (SHA256 sha = SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", string.Empty);
                }
            }
            catch
            {
                return string.Empty;
            }
        }

    }

    [HarmonyPatch(typeof(mainScript), "Update")]
    internal static class MainScript_Update_IMDataCoreStandaloneSaveNotifications_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            StandaloneSaveProgressCoordinator.PumpNotifications();
        }
    }
}
