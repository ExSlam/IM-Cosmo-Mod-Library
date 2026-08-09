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
    /// Persistence abstraction that can be swapped without changing capture logic.
    /// </summary>
    internal interface ICoreStorageEngine : IDisposable
    {
        /// <summary>
        /// Initializes the storage backend for a specific database path.
        /// </summary>
        bool Initialize(string databasePath, out string errorMessage);

        /// <summary>
        /// Revalidates the durable storage artifact before a staged candidate is published.
        /// </summary>
        bool TryValidateIntegrity(out string errorMessage);

        /// <summary>
        /// Persists one queued batch atomically.
        /// </summary>
        bool PersistBatch(
            IReadOnlyList<PendingEvent> pendingEvents,
            IReadOnlyList<SingleParticipationProjection> singleParticipationRows,
            IReadOnlyList<StatusTransitionProjection> statusTransitions,
            out string errorMessage);

        /// <summary>
        /// Upserts a namespaced custom JSON value with quota checks.
        /// </summary>
        bool TrySetCustomData(string saveKey, string namespaceIdentifier, string dataKey, string jsonValue, out string errorMessage);

        /// <summary>
        /// Reads a namespaced custom JSON value.
        /// </summary>
        bool TryGetCustomData(string saveKey, string namespaceIdentifier, string dataKey, out string jsonValue, out string errorMessage);

        /// <summary>
        /// Removes a namespaced custom JSON value.
        /// </summary>
        bool TryRemoveCustomData(string saveKey, string namespaceIdentifier, string dataKey, out string errorMessage);

        /// <summary>
        /// Validates one prospective custom-data mutation against the currently
        /// persisted namespace quotas without changing memory or durable storage.
        /// </summary>
        bool TryValidateCustomDataMutation(
            string saveKey,
            string namespaceIdentifier,
            string dataKey,
            string jsonValue,
            bool remove,
            out string errorMessage);

        /// <summary>
        /// Returns a bounded list of recent events for one idol.
        /// </summary>
        bool TryReadRecentEventsForIdol(string saveKey, int idolId, int maxCount, out List<IMDataCoreEvent> events, out string errorMessage);

        /// <summary>
        /// Returns money-ledger transactions inside a half-open game-date range.
        /// </summary>
        bool TryReadMoneyTransactions(
            string saveKey,
            DateTime startInclusive,
            DateTime endExclusive,
            int maxCount,
            out List<IMDataCoreMoneyTransaction> transactions,
            out bool wasTruncated,
            out string errorMessage);

        /// <summary>
        /// Returns the first game date at which exact money-ledger capture began.
        /// </summary>
        bool TryGetMoneyLedgerCoverageStart(string saveKey, out DateTime coverageStart, out string errorMessage);

        /// <summary>
        /// Records an exact storage checkpoint for confirmed bytes of one vanilla
        /// save file. Engines retain a bounded recent history; re-recording the same
        /// fingerprint replaces its older checkpoint with the latest storage state.
        /// </summary>
        bool TryRecordSaveGeneration(
            string saveKey,
            string vanillaSaveFingerprint,
            out string errorMessage);

        /// <summary>
        /// Restores an exact retained storage checkpoint associated with one vanilla
        /// save fingerprint. A fingerprint older than the bounded history is reported
        /// through generationFound so callers can fall back to legacy game-date rollback.
        /// </summary>
        bool TryRollbackToSaveGeneration(
            string saveKey,
            string vanillaSaveFingerprint,
            out bool generationFound,
            out string errorMessage);

        /// <summary>
        /// Removes persisted rows that are newer than one loaded save snapshot date.
        /// </summary>
        bool TryRollbackToGameDateTime(string saveKey, DateTime cutoffGameDateTime, out string errorMessage);

        /// <summary>
        /// Rewrites persisted rows from one save key to another inside the current storage file.
        /// When `sourceSaveKey` is empty, all non-target save keys are rewritten to `targetSaveKey`.
        /// </summary>
        bool TryRemapSaveKey(string sourceSaveKey, string targetSaveKey, out string errorMessage);
    }

}
