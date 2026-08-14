using System;
using System.Collections.Generic;
namespace IMDataCore
{
    /// <summary>
    /// Lightweight save/load coordination for IM Data Core 3.2. This partial is
    /// deliberately limited to in-memory branch management and explicit sidecar
    /// persistence boundaries.
    /// </summary>
    internal sealed partial class IMDataCoreController
    {
        /// <summary>
        /// Captures vanilla's already-populated SavedData stamp and writes the
        /// current logical IMDC branch to the exact mirrored target. Exceptions
        /// never escape into vanilla's save call.
        /// </summary>
        internal void PrepareVanillaSaveWrite(
            SaveManager.SavedData savedData,
            string dataFileName,
            bool isJson,
            bool fullPath)
        {
            try
            {
                string resolvedVanillaPath;
                if (!CorePaths.TryResolveDataSaverPath(
                        dataFileName,
                        isJson,
                        fullPath,
                        out resolvedVanillaPath))
                {
                    CoreLog.Warn(
                        "IM Data Core rejected an unsupported vanilla save target.");
                    return;
                }

                CoreSaveScope targetScope;
                if (!CorePaths.TryResolveSaveScope(
                        resolvedVanillaPath,
                        out targetScope))
                {
                    CoreLog.Warn(
                        "IM Data Core could not resolve the vanilla save scope.");
                    return;
                }

                // Final safety nets for direct field writes or third-party patches
                // that bypass known capture methods.
                ReconcileAllPostModShows(
                    CorePayloadCompaction.CanonicalShowCastSourcePrefix +
                        "save_boundary");
                CaptureResolvedSingleChartPositionsBeforeSave();

                LightweightCoreStorageEngine engineForWrite;
                LightweightPersistenceSnapshot persistenceSnapshot;
                string errorMessage;
                lock (runtimeLock)
                {
                    if (!EnsureInitializedLocked(out errorMessage) ||
                        !FlushLocked(true, out errorMessage))
                    {
                        CoreLog.Warn(errorMessage);
                        return;
                    }

                    VanillaSaveStamp stamp;
                    if (!VanillaSaveStamp.TryCreate(
                            savedData,
                            targetScope.RelativeSavePath,
                            out stamp,
                            out errorMessage) ||
                        !storageEngine.AddOrReplaceCheckpoint(
                            stamp,
                            captureSequence,
                            out errorMessage) ||
                        !storageEngine.TryCreatePersistenceSnapshot(
                            targetScope,
                            out persistenceSnapshot,
                            out errorMessage))
                    {
                        CoreLog.Warn(
                            "IM Data Core could not prepare its sidecar: " +
                            errorMessage);
                        return;
                    }

                    engineForWrite = storageEngine;
                }

                // The snapshot owns stable list references. Serialize and fsync
                // outside runtimeLock so long campaign saves do not stall capture
                // and read APIs for the entire JSON/disk write.
                if (!engineForWrite.TryPersistSnapshot(
                        persistenceSnapshot,
                        out errorMessage))
                {
                    CoreLog.Warn(
                        "IM Data Core could not persist its sidecar: " +
                        errorMessage);
                    return;
                }

                lock (runtimeLock)
                {
                    if (!ReferenceEquals(storageEngine, engineForWrite))
                    {
                        return;
                    }

                    activeSaveScope = targetScope;
                    activeSaveKey = NormalizeSaveKey(
                        targetScope.InternalSaveKey);
                    CorePaths.SetActiveSaveFilePathHint(
                        targetScope.SaveFilePath);
                }
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "IM Data Core sidecar preparation failed without blocking vanilla: " +
                    exception.Message);
            }
        }
        /// <summary>
        /// Replaces the supplemental runtime immediately after vanilla assigns
        /// SaveManager.Data and before any LoadEvent subscriber mutates its stamp.
        /// </summary>
        internal void OnVanillaSaveDataRead(
            SaveManager.SavedData loadedSaveData,
            string dataFileName)
        {
            LightweightCoreStorageEngine loadedEngine = null;
            CoreSaveScope targetScope = null;
            bool engineInstalled = false;
            lock (runtimeLock)
            {
                if (saveLoadPreparationActive)
                {
                    CoreLog.Warn(
                        "IM Data Core ignored a duplicate SaveManager.Data restoration " +
                        "during the same vanilla load.");
                    return;
                }
                // This flag is now an idempotency guard for one vanilla LoadData
                // invocation. The successful postfix clears it.
                saveLoadPreparationActive = true;
                preparedLoadGameDate = DateTime.MinValue;
                preparedLoadGameDateValid = false;
            }
            try
            {
                string resolvedVanillaPath;
                if (loadedSaveData == null ||
                    !CorePaths.TryResolveDataSaverLoadPath(
                        dataFileName,
                        out resolvedVanillaPath) ||
                    !CorePaths.TryResolveSaveScope(
                        resolvedVanillaPath,
                        out targetScope))
                {
                    CoreLog.Warn(
                        "IM Data Core could not resolve the loaded vanilla save path; " +
                        "supplemental state was detached safely.");
                    InstallSafeEmptyLoadedState(null);
                    return;
                }
                VanillaSaveStamp stamp;
                string errorMessage;
                if (!VanillaSaveStamp.TryCreate(
                    loadedSaveData,
                    targetScope.RelativeSavePath,
                    out stamp,
                    out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    InstallSafeEmptyLoadedState(targetScope);
                    return;
                }
                DateTime loadedGameDate;
                try
                {
                    loadedGameDate = ExtensionMethods.ToDateTime(
                        stamp.GameDateTime);
                }
                catch (Exception exception)
                {
                    loadedGameDate = DateTime.MinValue;
                    CoreLog.Warn(
                        "IM Data Core could not parse the loaded game date: " +
                        exception.Message);
                }
                loadedEngine = new LightweightCoreStorageEngine();
                string loadError;
                bool sidecarLoaded = loadedEngine.Initialize(
                    targetScope,
                    out loadError);
                if (sidecarLoaded && !string.IsNullOrEmpty(loadError))
                {
                    CoreLog.Warn(loadError);
                }
                if (!sidecarLoaded)
                {
                    CoreLog.Warn(loadError);

                    if (!loadedEngine.IsPersistenceBlocked)
                    {
                        string emptyError;
                        if (!loadedEngine.InitializeEmpty(
                                targetScope,
                                out emptyError))
                        {
                            loadedEngine.Dispose();
                            CoreLog.Warn(emptyError);
                            InstallSafeEmptyLoadedState(targetScope);
                            return;
                        }
                    }
                    else
                    {
                        CoreLog.Warn(
                            "IM Data Core will keep supplemental state read-only for " +
                            "this physical save until a different save path is used. " +
                            "The unreadable/unsupported sidecar was left untouched.");
                    }
                }
                bool checkpointFound = false;
                long activatedSequence = 0L;
                bool hasExistingSidecarDocument =
                    sidecarLoaded && loadedEngine.HasLoadedSidecarDocument;
                if (hasExistingSidecarDocument &&
                    !loadedEngine.TryActivateCheckpoint(
                        stamp,
                        out checkpointFound,
                        out activatedSequence,
                        out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                }
                if (hasExistingSidecarDocument && !checkpointFound)
                {
                    // Existing sidecars are branch/checkpoint ledgers. A date-only
                    // cutoff cannot distinguish two histories that share the same
                    // in-game date, so an unmatched document must fail closed. A
                    // genuinely new physical save with no sidecar remains writable.
                    loadedEngine.EnterReadOnlyEmptyForCurrentScope(
                        "The loaded vanilla save has no exact IM Data Core checkpoint " +
                        "in this existing sidecar. Supplemental state was detached " +
                        "read-only and the sidecar was left untouched.");
                    sidecarLoaded = false;
                }
                InstallLoadedEngine(
                    loadedEngine,
                    targetScope,
                    loadedGameDate);
                engineInstalled = true;
            }
            catch (Exception exception)
            {
                // Vanilla remains canonical. A supplemental failure must never
                // prevent LoadEvent or mutate the loaded game save.
                CoreLog.Warn(
                    "IM Data Core load restoration failed without blocking vanilla: " +
                    exception.Message);
                if (!engineInstalled && loadedEngine != null)
                {
                    loadedEngine.Dispose();
                }
                InstallSafeEmptyLoadedState(targetScope);
            }
        }
        private void InstallSafeEmptyLoadedState(CoreSaveScope targetScope)
        {
            LightweightCoreStorageEngine safeEngine =
                CreateSafeEmptyEngine(targetScope);
            CoreSaveScope safeScope = targetScope;
            if (safeScope == null || safeScope.IsTransient)
            {
                CorePaths.ResetToTransientSaveScope();
                safeScope = CorePaths.GetSaveScope();
            }
            InstallLoadedEngine(
                safeEngine,
                safeScope,
                DateTime.MinValue);
        }
        private static LightweightCoreStorageEngine CreateSafeEmptyEngine(
            CoreSaveScope targetScope)
        {
            LightweightCoreStorageEngine safeEngine =
                new LightweightCoreStorageEngine();
            string ignoredError;
            if (targetScope != null &&
                !targetScope.IsTransient)
            {
                if (safeEngine.InitializeEmpty(
                        targetScope,
                        out ignoredError) ||
                    safeEngine.IsPersistenceBlocked)
                {
                    return safeEngine;
                }
            }

            safeEngine.InitializeTransient();
            return safeEngine;
        }
        private void InstallLoadedEngine(
            LightweightCoreStorageEngine loadedEngine,
            CoreSaveScope targetScope,
            DateTime loadedGameDate)
        {
            // If even a pristine physical engine could not be initialized, keep
            // the engine and its advertised scope aligned.  This is a last-resort
            // fail-safe: vanilla may still load, but IMDC remains detached until
            // a later real save successfully establishes a physical sidecar.
            if (!loadedEngine.HasPhysicalScope &&
                (targetScope == null || !targetScope.IsTransient))
            {
                CorePaths.ResetToTransientSaveScope();
                targetScope = CorePaths.GetSaveScope();
            }
            lock (runtimeLock)
            {
                if (storageEngine != null)
                {
                    storageEngine.Dispose();
                }
                storageEngine = loadedEngine;
                initialized = true;
                activeSaveScope = targetScope;
                activeSaveKey = NormalizeSaveKey(
                    targetScope.InternalSaveKey);
                captureSequence = loadedEngine.LastIssuedSequence;
                bufferedEvents.Clear();
                ResetRuntimeCaptureStateLocked();
                preparedLoadGameDate = loadedGameDate;
                preparedLoadGameDateValid =
                    loadedGameDate != DateTime.MinValue;
                saveLoadPreparationActive = true;
                if (targetScope.IsTransient)
                {
                    CorePaths.ResetToTransientSaveScope();
                }
                else
                {
                    CorePaths.SetActiveSaveFilePathHint(
                        targetScope.SaveFilePath);
                }
            }
        }
        /// <summary>
        /// Detaches the prior save without writing it and starts a fresh transient
        /// branch. The first real vanilla save supplies the physical scope.
        /// </summary>
        internal void OnNewGameStarting()
        {
            lock (runtimeLock)
            {
                if (storageEngine != null)
                {
                    storageEngine.Dispose();
                }
                storageEngine = new LightweightCoreStorageEngine();
                storageEngine.InitializeTransient();
                CorePaths.ResetToTransientSaveScope();
                activeSaveScope = CorePaths.GetSaveScope();
                activeSaveKey = NormalizeSaveKey(
                    activeSaveScope.InternalSaveKey);
                initialized = true;
                saveLoadPreparationActive = false;
                preparedLoadGameDate = DateTime.MinValue;
                preparedLoadGameDateValid = false;
                captureSequence = 0L;
                bufferedEvents.Clear();
                ResetRuntimeCaptureStateLocked();
            }
        }
        internal void OnVanillaLoadCompleted()
        {
            lock (runtimeLock)
            {
                saveLoadPreparationActive = false;
                preparedLoadGameDate = DateTime.MinValue;
                preparedLoadGameDateValid = false;
                try
                {
                    SeedResolvedSingleChartPositionsFromVanillaLocked();
                    SeedPendingSubstoryCompletionsFromVanillaLocked();
                }
                catch (Exception exception)
                {
                    // Supplemental post-load index rebuilding must never escape from
                    // a Harmony postfix and interrupt vanilla scene/game progression.
                    CoreLog.Warn(
                        "IM Data Core post-load runtime seeding failed without " +
                        "blocking vanilla: " +
                        exception.Message);
                }
            }
        }
        internal void CancelVanillaLoadPreparation()
        {
            lock (runtimeLock)
            {
                // Used only by Harmony failure/finalizer paths. Do not seed
                // supplemental state when vanilla itself did not complete loading.
                saveLoadPreparationActive = false;
                preparedLoadGameDate = DateTime.MinValue;
                preparedLoadGameDateValid = false;
            }
        }
        private bool EnsureInitialized(out string errorMessage)
        {
            lock (runtimeLock)
            {
                return EnsureInitializedLocked(out errorMessage);
            }
        }

        private bool EnsureInitializedLocked(out string errorMessage)
        {
            errorMessage = string.Empty;
            if (initialized && storageEngine != null)
            {
                return true;
            }
            try
            {
                storageEngine = new LightweightCoreStorageEngine();
                storageEngine.InitializeTransient();
                CorePaths.ResetToTransientSaveScope();
                activeSaveScope = CorePaths.GetSaveScope();
                activeSaveKey = NormalizeSaveKey(
                    activeSaveScope.InternalSaveKey);
                captureSequence = 0L;
                initialized = true;
                saveLoadPreparationActive = false;
                preparedLoadGameDate = DateTime.MinValue;
                preparedLoadGameDateValid = false;
                ResetRuntimeCaptureStateLocked();
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = CoreConstants.MessageStorageInitializationFailure +
                    exception.Message;
                storageEngine = null;
                initialized = false;
                return false;
            }
        }
        /// <summary>
        /// Moves captured events into the in-memory branch. A non-forced call is
        /// intentionally a no-op; it exists for compatibility with capture sites
        /// that previously triggered periodic/threshold persistence checks.
        /// </summary>
        private bool FlushLocked(bool forceFlush, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (storageEngine == null)
            {
                errorMessage = CoreConstants.MessageStorageUnavailable;
                return false;
            }
            if (!forceFlush)
            {
                return true;
            }

            if (bufferedEvents.Count > 0 &&
                !storageEngine.AppendEvents(bufferedEvents, out errorMessage))
            {
                return false;
            }

            bufferedEvents.Clear();
            pendingCustomEventIdempotencyKeys.Clear();
            storageEngine.SetLastIssuedSequence(captureSequence);
            return true;
        }
        /// <summary>
        /// Captures chart-position backfill events before a vanilla save boundary.
        /// </summary>
        private void CaptureResolvedSingleChartPositionsBeforeSave()
        {
            List<KeyValuePair<singles._single, int>> pendingChartUpdates =
                new List<KeyValuePair<singles._single, int>>();
            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                if (singles.Singles == null ||
                    singles.Singles.Count < CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    return;
                }
                for (int index = 0; index < singles.Singles.Count; index++)
                {
                    singles._single releasedSingle = singles.Singles[index];
                    if (releasedSingle == null ||
                        releasedSingle.id < CoreConstants.MinimumValidIdolIdentifier ||
                        releasedSingle.status != singles._single._status.released)
                    {
                        continue;
                    }
                    int chartPosition = ResolveChartPosition(releasedSingle);
                    if (chartPosition <= 0)
                    {
                        continue;
                    }
                    int knownChartPosition;
                    if (resolvedSingleChartPositionBySingleId.TryGetValue(
                            releasedSingle.id,
                            out knownChartPosition) &&
                        knownChartPosition == chartPosition)
                    {
                        continue;
                    }
                    pendingChartUpdates.Add(
                        new KeyValuePair<singles._single, int>(
                            releasedSingle,
                            chartPosition));
                }
            }
            for (int index = 0; index < pendingChartUpdates.Count; index++)
            {
                KeyValuePair<singles._single, int> update =
                    pendingChartUpdates[index];
                CaptureSingleChartPositionResolved(
                    update.Key,
                    update.Value,
                    CoreConstants.EventSourceSingleChartBackfillPatch);
            }
        }
        private void ResetRuntimeCaptureStateLocked()
        {
            tourRuntimeStateByTourId.Clear();
            concertEditBaselineByConcertId.Clear();
            resolvedSingleChartPositionBySingleId.Clear();
            pendingSubstoryCompletionCountByDialogueId.Clear();
            pendingCustomEventIdempotencyKeys.Clear();
            idempotencyKeysForCurrentDate.Clear();
            idempotencyDateKey = CoreConstants.UninitializedDateKey;
        }
        /// <summary>
        /// Rebuilds deferred dialogue-completion tokens from vanilla's restored
        /// queue. The queue itself is canonical save data; this method only restores
        /// IMDC's transient completion bookkeeping and emits nothing.
        /// </summary>
        private void SeedPendingSubstoryCompletionsFromVanillaLocked()
        {
            pendingSubstoryCompletionCountByDialogueId.Clear();
            if (Substories_Manager.dialogueQueue == null)
            {
                return;
            }

            for (int index = CoreConstants.ZeroBasedListStartIndex;
                index < Substories_Manager.dialogueQueue.Count;
                index++)
            {
                Substories_Manager._dialogueQueue queued =
                    Substories_Manager.dialogueQueue[index];
                data_dialogues._dialogue dialogue =
                    queued != null ? queued.dialogue : null;
                if (dialogue == null ||
                    dialogue.type != data_dialogues._dialogue._type.dialogue ||
                    string.IsNullOrEmpty(dialogue.id))
                {
                    continue;
                }

                int count;
                if (!pendingSubstoryCompletionCountByDialogueId.TryGetValue(
                        dialogue.id,
                        out count))
                {
                    count = CoreConstants.ZeroBasedListStartIndex;
                }

                pendingSubstoryCompletionCountByDialogueId[dialogue.id] =
                    count + 1;
            }
        }

        /// <summary>
        /// LoadEvent has now rebuilt vanilla's canonical singles. Seed only the
        /// transient duplicate-suppression index; do not emit or persist a second
        /// copy of that canonical state.
        /// </summary>
        private void SeedResolvedSingleChartPositionsFromVanillaLocked()
        {
            resolvedSingleChartPositionBySingleId.Clear();
            if (singles.Singles == null)
            {
                return;
            }
            for (int index = 0; index < singles.Singles.Count; index++)
            {
                singles._single releasedSingle = singles.Singles[index];
                if (releasedSingle == null ||
                    releasedSingle.id < CoreConstants.MinimumValidIdolIdentifier ||
                    releasedSingle.status != singles._single._status.released)
                {
                    continue;
                }
                int chartPosition = ResolveChartPosition(releasedSingle);
                if (chartPosition > CoreConstants.ZeroBasedListStartIndex)
                {
                    resolvedSingleChartPositionBySingleId[releasedSingle.id] =
                        chartPosition;
                }
            }
        }
    }
}
