using System;
using System.Collections.Generic;
using SaveNLoadFixes.Persistence;
using CosmoSaveNotifications;

namespace SaveNLoadFixes.Transport
{
    internal enum SaveProgressResult { Pending, Succeeded, Failed }

    internal sealed class SaveProgressTransaction
    {
        internal SaveManager.SavedData SavedData;
        internal long AttemptId;
        internal string TargetPath = string.Empty;
        internal SaveProgressResult TransportResult;
        internal bool CompanionExpected;
        internal SaveProgressResult CompanionResult;
        internal bool StartDisplayed;
    }

    // Track actual SavedData writes, including autosaves and story-slot copies.
    // Concurrent writes must not steal each other's companion/completion result.
    internal static class SaveProgressCoordinator
    {
        private static readonly object SyncRoot = new object();
        private static readonly List<SaveProgressTransaction> Transactions = new List<SaveProgressTransaction>();

        internal static bool IsUiOwnerActive
        {
            get { return SaveTransportApi.IsAuthoritativeTransport; }
        }

        private static SaveProgressTransaction FindOrCreateLocked(SaveManager.SavedData data)
        {
            foreach (SaveProgressTransaction transaction in Transactions)
                if (ReferenceEquals(transaction.SavedData, data) && transaction.AttemptId == 0L)
                    return transaction;
            SaveProgressTransaction created = new SaveProgressTransaction { SavedData = data };
            Transactions.Add(created);
            return created;
        }

        internal static bool BeginCompanionPersistence(SaveManager.SavedData savedData, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (!IsUiOwnerActive || savedData == null)
            {
                errorMessage = "The save-progress coordinator is inactive or SavedData is null.";
                return false;
            }
            lock (SyncRoot)
            {
                SaveProgressTransaction transaction = FindOrCreateLocked(savedData);
                transaction.CompanionExpected = true;
                transaction.CompanionResult = SaveProgressResult.Pending;
            }
            return true;
        }

        internal static bool ReportCompanionPersistenceResult(
            SaveManager.SavedData savedData, bool succeeded, string error, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (!IsUiOwnerActive || savedData == null)
            {
                errorMessage = "The save-progress coordinator is inactive or SavedData is null.";
                return false;
            }
            RepairEnvelopeTransport.ReportIMDataCorePersistenceOutcome(savedData, succeeded);
            lock (SyncRoot)
            {
                SaveProgressTransaction transaction = FindOrCreateLocked(savedData);
                transaction.CompanionExpected = true;
                transaction.CompanionResult = succeeded ? SaveProgressResult.Succeeded : SaveProgressResult.Failed;
            }
            return true;
        }

        internal static void ReportSavedDataWriteSetupFailure(SaveManager.SavedData savedData, string error)
        {
            lock (SyncRoot)
                FindOrCreateLocked(savedData).TransportResult = SaveProgressResult.Failed;
        }

        internal static void BindSavedDataWrite(SaveManager.SavedData savedData, string targetPath, long savedDataAttemptId)
        {
            if (savedData == null || savedDataAttemptId <= 0L) return;
            lock (SyncRoot)
            {
                SaveProgressTransaction transaction = FindOrCreateLocked(savedData);
                transaction.AttemptId = savedDataAttemptId;
                transaction.TargetPath = targetPath ?? string.Empty;
            }
        }

        // Writer-thread callback; all Unity/UI work stays in PumpNotifications.
        internal static void ReportSavedDataWriteResult(
            string targetPath, long savedDataAttemptId, bool succeeded, string error)
        {
            lock (SyncRoot)
            {
                foreach (SaveProgressTransaction transaction in Transactions)
                {
                    if (savedDataAttemptId <= 0L || transaction.AttemptId != savedDataAttemptId) continue;
                    if (!string.Equals(transaction.TargetPath, targetPath,
                        System.IO.Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) continue;
                    transaction.TransportResult = succeeded ? SaveProgressResult.Succeeded : SaveProgressResult.Failed;
                    return;
                }
            }
        }

        internal static void PumpNotifications()
        {
            List<string> messages = new List<string>();
            lock (SyncRoot)
            {
                for (int index = 0; index < Transactions.Count;)
                {
                    SaveProgressTransaction transaction = Transactions[index];
                    if (!transaction.StartDisplayed)
                    {
                        transaction.StartDisplayed = true;
                        messages.Add(SaveNotifications.StartedKey);
                        // Separate frames keep start visible even for very fast saves.
                        index++;
                        continue;
                    }
                    if (transaction.TransportResult == SaveProgressResult.Pending ||
                        (transaction.CompanionExpected && transaction.CompanionResult == SaveProgressResult.Pending))
                    {
                        index++;
                        continue;
                    }
                    messages.Add(transaction.TransportResult == SaveProgressResult.Succeeded
                        ? SaveNotifications.CompletedKey : SaveNotifications.FailedKey);
                    if (transaction.CompanionExpected && transaction.CompanionResult == SaveProgressResult.Failed)
                        messages.Add(SaveNotifications.CompanionFailedKey);
                    Transactions.RemoveAt(index);
                }
            }
            foreach (string message in messages)
                SaveNotifications.Show(message, message == SaveNotifications.StartedKey ? mainScript.lightBlue32 :
                    message == SaveNotifications.CompletedKey ? mainScript.green32 : mainScript.red32);
            SaveNotifications.Pump();
        }
    }
}
