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
        /// Returns a bounded list of recent events for one idol.
        /// </summary>
        bool TryReadRecentEventsForIdol(string saveKey, int idolId, int maxCount, out List<IMDataCoreEvent> events, out string errorMessage);

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
