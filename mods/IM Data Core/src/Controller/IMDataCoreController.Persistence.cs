using System;
using System.Collections.Generic;
using System.IO;
namespace IMDataCore
{
    /// <summary>
    /// Lightweight save/load coordination for IM Data Core 3.4. This partial is
    /// deliberately limited to in-memory branch management and explicit sidecar
    /// persistence boundaries.
    /// </summary>
    internal sealed partial class IMDataCoreController
    {
        private const int BufferedEventFlushThreshold = 256;
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

                List<LightweightModSnapshotRecord> enabledMods =
                    CaptureCurrentModSnapshot(true);
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
                            enabledMods,
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
                bool persistenceSnapshotIsCurrent;
                if (!engineForWrite.TryPersistSnapshot(
                        persistenceSnapshot,
                        out persistenceSnapshotIsCurrent,
                        out errorMessage))
                {
                    CoreLog.Warn(
                        "IM Data Core could not persist its sidecar: " +
                        errorMessage);
                    return;
                }

                lock (runtimeLock)
                {
                    if (!ReferenceEquals(storageEngine, engineForWrite) ||
                        !persistenceSnapshotIsCurrent)
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
                // Loading the same physical sidecar must be one atomic handoff with
                // respect to every old/new engine I/O operation on that path. Without
                // this process-wide lease, an old queued compactor can replace the base
                // or delete a journal after the replacement engine has already loaded it.
                object sidecarIoLock =
                    LightweightCoreStorageEngine.GetSharedPersistenceIoLock(
                        targetScope.SidecarFilePath);
                lock (sidecarIoLock)
                {
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
                    if (hasExistingSidecarDocument && checkpointFound)
                    {
                        List<LightweightModSnapshotRecord> requiredMods;
                        if (!loadedEngine.TryGetCheckpointModSnapshot(
                                stamp,
                                out requiredMods,
                                out errorMessage))
                        {
                            CoreLog.Warn(errorMessage);
                        }
                        else
                        {
                            WarnForCheckpointModDifferences(requiredMods);
                        }
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
            if (targetScope != null && !targetScope.IsTransient)
            {
                object sidecarIoLock =
                    LightweightCoreStorageEngine.GetSharedPersistenceIoLock(
                        targetScope.SidecarFilePath);
                lock (sidecarIoLock)
                {
                    InstallSafeEmptyLoadedStateCore(targetScope);
                }
                return;
            }

            InstallSafeEmptyLoadedStateCore(targetScope);
        }

        private void InstallSafeEmptyLoadedStateCore(CoreSaveScope targetScope)
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
        /// Moves captured events into the in-memory branch. Non-forced capture
        /// calls flush at a bounded threshold so payload normalization/indexing is
        /// amortized during play instead of accumulating entirely at save/read time.
        /// This remains memory-only; disk persistence still occurs only at explicit
        /// save/flush boundaries.
        /// </summary>
        private bool FlushLocked(bool forceFlush, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (storageEngine == null)
            {
                errorMessage = CoreConstants.MessageStorageUnavailable;
                return false;
            }
            if (!forceFlush &&
                bufferedEvents.Count < BufferedEventFlushThreshold)
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

                if (pendingSingleChartResolutionBySingleId.Count == 0)
                {
                    return;
                }

                List<int> stalePendingIds = null;
                foreach (KeyValuePair<int, singles._single> pendingEntry in
                    pendingSingleChartResolutionBySingleId)
                {
                    singles._single releasedSingle = pendingEntry.Value;
                    if (releasedSingle == null ||
                        releasedSingle.id != pendingEntry.Key ||
                        releasedSingle.status != singles._single._status.released)
                    {
                        if (stalePendingIds == null)
                        {
                            stalePendingIds = new List<int>();
                        }
                        stalePendingIds.Add(pendingEntry.Key);
                        continue;
                    }

                    int chartPosition = ResolveChartPosition(releasedSingle);
                    if (chartPosition <= CoreConstants.ZeroBasedListStartIndex)
                    {
                        continue;
                    }

                    int knownChartPosition;
                    if (resolvedSingleChartPositionBySingleId.TryGetValue(
                            releasedSingle.id,
                            out knownChartPosition) &&
                        knownChartPosition == chartPosition)
                    {
                        if (stalePendingIds == null)
                        {
                            stalePendingIds = new List<int>();
                        }
                        stalePendingIds.Add(releasedSingle.id);
                        continue;
                    }

                    pendingChartUpdates.Add(
                        new KeyValuePair<singles._single, int>(
                            releasedSingle,
                            chartPosition));
                }

                if (stalePendingIds != null)
                {
                    for (int index = 0;
                        index < stalePendingIds.Count;
                        index++)
                    {
                        pendingSingleChartResolutionBySingleId.Remove(
                            stalePendingIds[index]);
                    }
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
            pendingSingleChartResolutionBySingleId.Clear();
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
            pendingSingleChartResolutionBySingleId.Clear();
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
                else
                {
                    pendingSingleChartResolutionBySingleId[releasedSingle.id] =
                        releasedSingle;
                }
            }
        }

        /// <summary>
        /// Captures the Idol Manager mod registry, not Harmony ownership. This includes
        /// JSON-only mods and mods that have no dependency on IM Data Core.
        /// </summary>
        private static List<LightweightModSnapshotRecord> CaptureCurrentModSnapshot(
            bool enabledOnly)
        {
            List<LightweightModSnapshotRecord> result =
                new List<LightweightModSnapshotRecord>();
            if (Mods._Mods == null)
            {
                return result;
            }

            for (int index = 0; index < Mods._Mods.Count; index++)
            {
                Mods._mod mod = Mods._Mods[index];
                if (mod == null || (enabledOnly && !mod.IsEnabled()))
                {
                    continue;
                }

                string modName = (mod.ModName ?? string.Empty).Trim();
                string title = (mod.Title ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(modName))
                {
                    modName = title;
                }

                LightweightModSnapshotRecord record =
                    new LightweightModSnapshotRecord
                    {
                        ModName = modName,
                        Title = title,
                        Author = (mod.Author ?? string.Empty).Trim(),
                        Version = (mod.Version ?? string.Empty).Trim(),
                        DllNames = FindModDllNames(mod.Path)
                    };
                result.Add(record);
            }

            result.Sort(delegate(
                LightweightModSnapshotRecord left,
                LightweightModSnapshotRecord right)
            {
                return string.Compare(
                    left != null ? left.ModName : string.Empty,
                    right != null ? right.ModName : string.Empty,
                    StringComparison.OrdinalIgnoreCase);
            });
            return result;
        }

        private static List<string> FindModDllNames(string modPath)
        {
            List<string> result = new List<string>();
            if (string.IsNullOrEmpty(modPath) || !Directory.Exists(modPath))
            {
                return result;
            }

            try
            {
                string[] dllPaths = Directory.GetFiles(
                    modPath,
                    "*.dll",
                    SearchOption.AllDirectories);
                HashSet<string> uniqueNames = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);
                for (int index = 0; index < dllPaths.Length; index++)
                {
                    string name = Path.GetFileName(dllPaths[index]);
                    if (!string.IsNullOrEmpty(name))
                    {
                        uniqueNames.Add(name);
                    }
                }
                result.AddRange(uniqueNames);
                result.Sort(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "IM Data Core could not enumerate DLL names for mod path '" +
                    modPath + "': " + exception.Message);
            }
            return result;
        }

        /// <summary>
        /// Compares the exact save checkpoint's enabled-mod inventory with the current
        /// registry after main-menu toggles or a later process restart. Differences are
        /// diagnostic only; vanilla remains authoritative and load is never blocked.
        /// </summary>
        private static void WarnForCheckpointModDifferences(
            IReadOnlyList<LightweightModSnapshotRecord> requiredMods)
        {
            if (requiredMods == null || requiredMods.Count == 0)
            {
                // Version-3 checkpoints have no mod inventory.
                return;
            }

            List<LightweightModSnapshotRecord> installed =
                CaptureCurrentModSnapshot(false);
            Dictionary<string, LightweightModSnapshotRecord> installedByName =
                new Dictionary<string, LightweightModSnapshotRecord>(
                    StringComparer.OrdinalIgnoreCase);
            Dictionary<string, bool> enabledByName =
                new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            if (Mods._Mods != null)
            {
                for (int index = 0; index < Mods._Mods.Count; index++)
                {
                    Mods._mod mod = Mods._Mods[index];
                    if (mod == null)
                    {
                        continue;
                    }
                    string key = (mod.ModName ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(key))
                    {
                        key = (mod.Title ?? string.Empty).Trim();
                    }
                    if (!string.IsNullOrEmpty(key))
                    {
                        enabledByName[key] = mod.IsEnabled();
                    }
                }
            }

            for (int index = 0; index < installed.Count; index++)
            {
                LightweightModSnapshotRecord row = installed[index];
                if (row != null && !string.IsNullOrEmpty(row.ModName))
                {
                    installedByName[row.ModName] = row;
                }
            }

            List<string> missing = new List<string>();
            List<string> disabled = new List<string>();
            List<string> mismatched = new List<string>();
            for (int index = 0; index < requiredMods.Count; index++)
            {
                LightweightModSnapshotRecord required = requiredMods[index];
                if (required == null || string.IsNullOrEmpty(required.ModName))
                {
                    continue;
                }

                LightweightModSnapshotRecord current;
                if (!installedByName.TryGetValue(required.ModName, out current) ||
                    current == null)
                {
                    missing.Add(DescribeModSnapshot(required));
                    continue;
                }

                bool isEnabled;
                if (!enabledByName.TryGetValue(required.ModName, out isEnabled) ||
                    !isEnabled)
                {
                    disabled.Add(DescribeModSnapshot(required));
                    continue;
                }

                List<string> differences = new List<string>();
                if (!string.Equals(
                        required.Author ?? string.Empty,
                        current.Author ?? string.Empty,
                        StringComparison.Ordinal))
                {
                    differences.Add(
                        "author '" + (required.Author ?? string.Empty) +
                        "' -> '" + (current.Author ?? string.Empty) + "'");
                }
                if (!string.Equals(
                        required.Version ?? string.Empty,
                        current.Version ?? string.Empty,
                        StringComparison.OrdinalIgnoreCase))
                {
                    differences.Add(
                        "version '" + (required.Version ?? string.Empty) +
                        "' -> '" + (current.Version ?? string.Empty) + "'");
                }
                if (!StringListsEqualIgnoreCase(required.DllNames, current.DllNames))
                {
                    differences.Add(
                        "DLLs [" + JoinStrings(required.DllNames) + "] -> [" +
                        JoinStrings(current.DllNames) + "]");
                }

                if (differences.Count > 0)
                {
                    mismatched.Add(
                        DescribeModSnapshot(required) + " (" +
                        string.Join("; ", differences.ToArray()) + ")");
                }
            }

            if (missing.Count == 0 && disabled.Count == 0 && mismatched.Count == 0)
            {
                return;
            }

            List<string> sections = new List<string>();
            if (missing.Count > 0)
            {
                sections.Add("missing: " + string.Join(", ", missing.ToArray()));
            }
            if (disabled.Count > 0)
            {
                sections.Add("disabled: " + string.Join(", ", disabled.ToArray()));
            }
            if (mismatched.Count > 0)
            {
                sections.Add("mismatched: " + string.Join(", ", mismatched.ToArray()));
            }

            CoreLog.Warn(
                "The loaded save's IM Data Core checkpoint was created with a different " +
                "mod set; " + string.Join(" | ", sections.ToArray()) + ".");
        }

        private static bool StringListsEqualIgnoreCase(
            IReadOnlyList<string> left,
            IReadOnlyList<string> right)
        {
            int leftCount = left != null ? left.Count : 0;
            int rightCount = right != null ? right.Count : 0;
            if (leftCount != rightCount)
            {
                return false;
            }
            for (int index = 0; index < leftCount; index++)
            {
                if (!string.Equals(
                        left[index] ?? string.Empty,
                        right[index] ?? string.Empty,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true;
        }

        private static string DescribeModSnapshot(LightweightModSnapshotRecord mod)
        {
            if (mod == null)
            {
                return "<unknown mod>";
            }
            string title = string.IsNullOrEmpty(mod.Title) ? mod.ModName : mod.Title;
            string suffix = string.IsNullOrEmpty(mod.Version)
                ? string.Empty
                : " v" + mod.Version;
            return title + suffix;
        }

        private static string JoinStrings(IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return string.Empty;
            }
            string[] copy = new string[values.Count];
            for (int index = 0; index < values.Count; index++)
            {
                copy[index] = values[index] ?? string.Empty;
            }
            return string.Join(", ", copy);
        }

    }
}
