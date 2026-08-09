using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace IMDataCore
{
    internal sealed class CorePendingSaveTransaction
    {
        internal string Token = string.Empty;
        internal long Sequence;
        internal long SourceRuntimeEpoch;
        internal CoreSaveScope SourceSaveScope;
        internal string SourceSaveKey = string.Empty;
        internal string SourceSaveDirectory = string.Empty;
        internal bool SourceWasTransient;
        internal CoreSaveScope TargetSaveScope;
        internal string TargetSaveKey = string.Empty;
        internal string TargetSaveDirectory = string.Empty;
        internal CoreSaveScope StagingSaveScope;
        internal string StagingSaveDirectory = string.Empty;
        internal string VanillaRelativeSavePath = string.Empty;
        internal string VanillaSaveFilePath = string.Empty;
        internal long ExpectedLength;
        internal string ExpectedSha256 = string.Empty;
        internal long BaselineLength = -1L;
        internal bool BaselineExisted;
        internal long BaselineWriteUtcTicks;
        internal string BaselineSha256 = string.Empty;
        internal long CaptureBaselineSequence;
        internal long CustomBaselineSequence;
        internal long CreatedUtcTicks;
        internal bool ObservationCompleted;
        internal bool VanillaWriteMatched;
        internal CoreFileFingerprint ObservedFingerprint;
        internal bool Published;
        internal bool ScopeResolved;
        internal bool LoadSettlementPending;
        internal long SettlementFailureStartedUtcTicks;
        internal string LastError = string.Empty;
    }

    internal sealed class CoreDeferredCustomDataMutation
    {
        internal long Sequence;
        internal long RuntimeEpoch;
        internal string SaveKey = string.Empty;
        internal string NamespaceIdentifier = string.Empty;
        internal string DataKey = string.Empty;
        internal string JsonValue = string.Empty;
        internal bool Remove;
    }

    internal sealed partial class IMDataCoreController
    {
        private const string SaveTransactionManifestFileName =
            "save.intent";
        private const int SaveObservationTimeoutMilliseconds = 20000;
        private const int SaveObservationPollMilliseconds = 100;
        private const int SaveObservationQuiescenceMilliseconds = 1000;
        private const long SaveSettlementFailureGraceTicks =
            TimeSpan.TicksPerSecond * 5L;
        private const long StaleSaveIntentAgeTicks =
            TimeSpan.TicksPerMinute * 30L;

        private readonly List<CorePendingSaveTransaction>
            pendingSaveTransactions =
                new List<CorePendingSaveTransaction>();
        private readonly List<CoreDeferredCustomDataMutation>
            deferredCustomDataMutations =
                new List<CoreDeferredCustomDataMutation>();
        private long saveTransactionSequence;
        private long customDataMutationSequence;

        /// <summary>
        /// Creates a complete private sidecar snapshot before vanilla schedules its
        /// asynchronous file writer. The returned token is informational; observation
        /// begins only after the manifest has been flushed to disk.
        /// </summary>
        internal string PrepareVanillaSaveWrite(
            string vanillaSaveFilePath,
            byte[] expectedVanillaBytes)
        {
            if (expectedVanillaBytes == null)
            {
                return string.Empty;
            }

            CoreSaveScope targetSaveScope;
            string vanillaRelativeSavePath;
            if (!CorePaths.TryResolveSaveScope(
                    vanillaSaveFilePath,
                    out targetSaveScope) ||
                !CorePaths.TryGetVanillaSaveRelativePath(
                    vanillaSaveFilePath,
                    out vanillaRelativeSavePath))
            {
                return string.Empty;
            }

            string expectedSha256 =
                CoreFileFingerprintUtility.ComputeSha256(
                    expectedVanillaBytes);
            if (string.IsNullOrEmpty(expectedSha256))
            {
                return string.Empty;
            }

            CorePendingSaveTransaction pendingTransaction = null;
            lock (runtimeLock)
            {
                if (saveLoadPreparationActive)
                {
                    CoreLog.Warn(
                        "A vanilla save was not sidecar-staged during an active load transaction.");
                    return string.Empty;
                }

                string initializationErrorMessage;
                if (!EnsureInitializedLocked(
                    out initializationErrorMessage))
                {
                    CoreLog.Warn(
                        CoreConstants.MessageSaveWritePreparationFailurePrefix +
                        initializationErrorMessage);
                    return string.Empty;
                }

                bool alreadyDeferringActiveScope =
                    HasUnresolvedSaveTransactionForActiveScopeLocked();
                if (!alreadyDeferringActiveScope)
                {
                    string flushErrorMessage;
                    if (!FlushLocked(true, out flushErrorMessage))
                    {
                        CoreLog.Warn(
                            CoreConstants.MessageSaveWritePreparationFailurePrefix +
                            CoreConstants.MessageFlushFailed +
                            flushErrorMessage);
                        return string.Empty;
                    }
                }

                CoreSaveScope sourceSaveScope = activeSaveScope ??
                    CorePaths.GetSaveScope();
                string sourceSaveKey = activeSaveKey;
                string sourceSaveDirectory = activeSaveDirectory;
                bool sourceWasTransient = activeSaveScopeIsTransient;
                CoreSaveScope stagingSaveScope =
                    CorePaths.CreateStagingSaveScope(
                        targetSaveScope,
                        false);
                if (stagingSaveScope == null)
                {
                    return string.Empty;
                }

                string stagingSaveDirectory =
                    CorePaths.GetSaveDirectory(stagingSaveScope);
                string stagingDirectoryName = Path.GetFileName(
                    stagingSaveDirectory.TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar));
                string transactionToken =
                    stagingDirectoryName.StartsWith(
                        "save_",
                        StringComparison.Ordinal)
                            ? stagingDirectoryName.Substring(
                                "save_".Length)
                            : string.Empty;
                if (transactionToken.Length != 32)
                {
                    return string.Empty;
                }

                long captureBaseline = captureSequence;
                long customBaseline = customDataMutationSequence;
                List<PendingEvent> eventSnapshot =
                    CloneEventsThroughSequenceLocked(
                        sourceSaveKey,
                        targetSaveScope.InternalSaveKey,
                        captureBaseline);
                List<SingleParticipationProjection> singleSnapshot =
                    CloneSingleRowsThroughSequenceLocked(
                        sourceSaveKey,
                        targetSaveScope.InternalSaveKey,
                        captureBaseline);
                List<StatusTransitionProjection> transitionSnapshot =
                    CloneTransitionsThroughSequenceLocked(
                        sourceSaveKey,
                        targetSaveScope.InternalSaveKey,
                        captureBaseline);

                DisposeStorageLocked();
                ResetStorageBindingLocked();
                string cloneErrorMessage;
                bool cloneSucceeded =
                    TryCloneAuthoritativeStorageIntoStageLocked(
                        sourceSaveDirectory,
                        stagingSaveScope,
                        out cloneErrorMessage);

                CorePaths.RestoreSaveScope(sourceSaveScope);
                string reopenSourceErrorMessage;
                bool sourceReopened = InitializePreparedStorageLocked(
                    sourceSaveScope,
                    out reopenSourceErrorMessage);
                if (!cloneSucceeded || !sourceReopened)
                {
                    TryCleanupStagingSaveDirectory(
                        stagingSaveDirectory);
                    CoreLog.Warn(
                        CoreConstants.MessageSaveWritePreparationFailurePrefix +
                        (!cloneSucceeded
                            ? cloneErrorMessage
                            : reopenSourceErrorMessage));
                    return string.Empty;
                }

                ICoreStorageEngine stagingEngine;
                string stageInitializationErrorMessage;
                if (!TryCreateAndInitializeStorageEngine(
                    CorePaths.GetDatabasePath(stagingSaveScope),
                    CorePaths.GetFlatFileDatabasePath(stagingSaveScope),
                    out stagingEngine,
                    out stageInitializationErrorMessage))
                {
                    TryCleanupStagingSaveDirectory(
                        stagingSaveDirectory);
                    CoreLog.Warn(
                        CoreConstants.MessageSaveWritePreparationFailurePrefix +
                        stageInitializationErrorMessage);
                    return string.Empty;
                }

                string targetSaveKey = NormalizeSaveKey(
                    targetSaveScope.InternalSaveKey);
                string stageErrorMessage;
                bool stagePrepared = stagingEngine.TryRemapSaveKey(
                    sourceSaveKey,
                    targetSaveKey,
                    out stageErrorMessage);
                if (stagePrepared &&
                    (eventSnapshot.Count > 0 ||
                     singleSnapshot.Count > 0 ||
                     transitionSnapshot.Count > 0))
                {
                    stagePrepared = stagingEngine.PersistBatch(
                        eventSnapshot,
                        singleSnapshot,
                        transitionSnapshot,
                        out stageErrorMessage);
                }

                if (stagePrepared)
                {
                    stagePrepared = ApplyDeferredCustomMutationsToEngineLocked(
                        stagingEngine,
                        sourceSaveKey,
                        runtimeEpoch,
                        targetSaveKey,
                        0L,
                        customBaseline,
                        out stageErrorMessage);
                }

                if (stagePrepared)
                {
                    stagePrepared = stagingEngine.TryRecordSaveGeneration(
                        targetSaveKey,
                        CoreFileFingerprintUtility.BuildContentIdentity(
                            expectedVanillaBytes.LongLength,
                            expectedSha256),
                        out stageErrorMessage);
                }

                if (stagePrepared)
                {
                    stagePrepared = stagingEngine.TryValidateIntegrity(
                        out stageErrorMessage);
                }

                stagingEngine.Dispose();
                if (!stagePrepared)
                {
                    TryCleanupStagingSaveDirectory(
                        stagingSaveDirectory);
                    CoreLog.Warn(
                        CoreConstants.MessageSaveWritePreparationFailurePrefix +
                        stageErrorMessage);
                    return string.Empty;
                }

                CoreFileFingerprint baselineFingerprint;
                string ignoredFingerprintError;
                bool baselineExisted = File.Exists(
                    targetSaveScope.SaveFilePath);
                bool baselineCaptured =
                    CoreFileFingerprintUtility.TryReadStable(
                    targetSaveScope.SaveFilePath,
                    out baselineFingerprint,
                    out ignoredFingerprintError);
                if (baselineExisted && !baselineCaptured)
                {
                    TryCleanupStagingSaveDirectory(
                        stagingSaveDirectory);
                    CoreLog.Warn(
                        CoreConstants.MessageSaveWritePreparationFailurePrefix +
                        "The existing vanilla target could not be fingerprinted: " +
                        ignoredFingerprintError);
                    return string.Empty;
                }

                saveTransactionSequence++;
                pendingTransaction = new CorePendingSaveTransaction
                {
                    Token = transactionToken,
                    Sequence = saveTransactionSequence,
                    SourceRuntimeEpoch = runtimeEpoch,
                    SourceSaveScope = sourceSaveScope,
                    SourceSaveKey = sourceSaveKey,
                    SourceSaveDirectory = sourceSaveDirectory,
                    SourceWasTransient = sourceWasTransient,
                    TargetSaveScope = targetSaveScope,
                    TargetSaveKey = targetSaveKey,
                    TargetSaveDirectory = CorePaths.GetSaveDirectory(
                        targetSaveScope),
                    StagingSaveScope = stagingSaveScope,
                    StagingSaveDirectory = stagingSaveDirectory,
                    VanillaRelativeSavePath = vanillaRelativeSavePath,
                    VanillaSaveFilePath = targetSaveScope.SaveFilePath,
                    ExpectedLength = expectedVanillaBytes.LongLength,
                    ExpectedSha256 = expectedSha256,
                    BaselineLength = baselineFingerprint == null
                        ? -1L
                        : baselineFingerprint.Length,
                    BaselineWriteUtcTicks = baselineFingerprint == null
                        ? 0L
                        : baselineFingerprint.LastWriteUtcTicks,
                    BaselineSha256 = baselineFingerprint == null
                        ? string.Empty
                        : baselineFingerprint.Sha256,
                    BaselineExisted = baselineExisted,
                    CaptureBaselineSequence = captureBaseline,
                    CustomBaselineSequence = customBaseline,
                    CreatedUtcTicks = DateTime.UtcNow.Ticks
                };

                string manifestErrorMessage;
                if (!TryWriteSaveIntentManifest(
                    pendingTransaction,
                    out manifestErrorMessage))
                {
                    TryCleanupStagingSaveDirectory(
                        stagingSaveDirectory);
                    CoreLog.Warn(
                        CoreConstants.MessageSaveWritePreparationFailurePrefix +
                        manifestErrorMessage);
                    return string.Empty;
                }

                pendingSaveTransactions.Add(pendingTransaction);
            }

            bool observerQueued = false;
            try
            {
                observerQueued = ThreadPool.QueueUserWorkItem(
                    delegate
                    {
                        ObserveVanillaSaveWrite(pendingTransaction);
                    });
            }
            catch (Exception exception)
            {
                pendingTransaction.LastError = exception.Message;
            }

            if (!observerQueued)
            {
                lock (runtimeLock)
                {
                    pendingTransaction.VanillaWriteMatched = false;
                    pendingTransaction.ObservationCompleted = true;
                    if (string.IsNullOrEmpty(pendingTransaction.LastError))
                    {
                        pendingTransaction.LastError =
                            "The vanilla save observer could not be queued.";
                    }
                }
            }

            return pendingTransaction.Token;
        }

        private bool HasUnresolvedSaveTransactionForActiveScopeLocked()
        {
            for (int transactionIndex = 0;
                transactionIndex < pendingSaveTransactions.Count;
                transactionIndex++)
            {
                CorePendingSaveTransaction transaction =
                    pendingSaveTransactions[transactionIndex];
                if (transaction != null &&
                    !transaction.ScopeResolved &&
                    transaction.SourceRuntimeEpoch == runtimeEpoch &&
                    string.Equals(
                        transaction.SourceSaveKey,
                        activeSaveKey,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Treats the file selected for a synchronous vanilla load as the settled final
        /// payload for that target. A matching durable stage is promoted before load
        /// staging clones canonical storage; nonmatching candidates are detached.
        /// </summary>
        private void PausePendingSaveTransactionsForLoadLocked(
            CoreSaveScope targetSaveScope)
        {
            if (targetSaveScope == null)
            {
                return;
            }

            string targetDirectory = CorePaths.GetSaveDirectory(
                targetSaveScope);
            for (int transactionIndex = 0;
                transactionIndex < pendingSaveTransactions.Count;
                transactionIndex++)
            {
                CorePendingSaveTransaction transaction =
                    pendingSaveTransactions[transactionIndex];
                if (transaction != null &&
                    !transaction.ScopeResolved &&
                    string.Equals(
                        transaction.TargetSaveDirectory,
                        targetDirectory,
                        StringComparison.OrdinalIgnoreCase))
                {
                    transaction.LoadSettlementPending = true;
                }
            }
        }

        private bool PreparePendingSaveTransactionsForLoadLocked(
            CoreSaveScope targetSaveScope,
            string loadedContentIdentity,
            CoreFileFingerprint loadedFileFingerprint,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (targetSaveScope == null)
            {
                return true;
            }

            string targetDirectory = CorePaths.GetSaveDirectory(
                targetSaveScope);
            bool candidateSettled = false;
            bool attributableCandidate = false;
            bool uncertainExactCandidate = false;
            List<CorePendingSaveTransaction> attributedCandidates =
                new List<CorePendingSaveTransaction>();
            for (int transactionIndex = 0;
                transactionIndex < pendingSaveTransactions.Count;
                transactionIndex++)
            {
                CorePendingSaveTransaction transaction =
                    pendingSaveTransactions[transactionIndex];
                if (transaction == null ||
                    transaction.ScopeResolved ||
                    !string.Equals(
                        transaction.TargetSaveDirectory,
                        targetDirectory,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                transaction.LoadSettlementPending = false;
                string expectedContentIdentity =
                    CoreFileFingerprintUtility.BuildContentIdentity(
                        transaction.ExpectedLength,
                        transaction.ExpectedSha256);
                bool exactLoadedContent = string.Equals(
                    expectedContentIdentity,
                    loadedContentIdentity,
                    StringComparison.Ordinal);
                bool exactMatch = exactLoadedContent &&
                    HasObservedVanillaWriteTransition(
                    transaction,
                    loadedFileFingerprint);
                transaction.ObservedFingerprint = exactMatch
                    ? loadedFileFingerprint
                    : transaction.ObservedFingerprint;
                transaction.VanillaWriteMatched = exactMatch;
                transaction.ObservationCompleted = true;
                if (exactMatch)
                {
                    attributableCandidate = true;
                    attributedCandidates.Add(transaction);
                }
                else if (exactLoadedContent &&
                    loadedFileFingerprint == null)
                {
                    uncertainExactCandidate = true;
                }
                candidateSettled = true;
            }

            if (candidateSettled)
            {
                ProcessCompletedSaveTransactionsLocked();
            }

            if (attributableCandidate)
            {
                for (int candidateIndex = 0;
                    candidateIndex < attributedCandidates.Count;
                    candidateIndex++)
                {
                    if (attributedCandidates[candidateIndex].Published)
                    {
                        return true;
                    }
                }

                errorMessage =
                    "The exact pending sidecar stage for the loaded vanilla payload could not be published.";
                for (int candidateIndex = 0;
                    candidateIndex < attributedCandidates.Count;
                    candidateIndex++)
                {
                    attributedCandidates[candidateIndex]
                        .LoadSettlementPending = true;
                }

                return false;
            }

            if (uncertainExactCandidate)
            {
                errorMessage =
                    "A pending sidecar stage matches the loaded bytes, but the vanilla write transition cannot be proven.";
                return false;
            }

            return true;
        }

        private void ResumePendingSaveTransactionsAfterLoadFailureLocked(
            CoreSaveScope targetSaveScope)
        {
            if (targetSaveScope == null)
            {
                return;
            }

            string targetDirectory = CorePaths.GetSaveDirectory(
                targetSaveScope);
            for (int transactionIndex = 0;
                transactionIndex < pendingSaveTransactions.Count;
                transactionIndex++)
            {
                CorePendingSaveTransaction transaction =
                    pendingSaveTransactions[transactionIndex];
                if (transaction != null &&
                    string.Equals(
                        transaction.TargetSaveDirectory,
                        targetDirectory,
                        StringComparison.OrdinalIgnoreCase))
                {
                    transaction.LoadSettlementPending = false;
                }
            }

            ProcessCompletedSaveTransactionsLocked();
        }

        private bool TryDeferCustomDataMutationLocked(
            string namespaceIdentifier,
            string dataKey,
            string jsonValue,
            bool remove,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            CorePendingSaveTransaction validationTransaction = null;
            for (int transactionIndex = 0;
                transactionIndex < pendingSaveTransactions.Count;
                transactionIndex++)
            {
                CorePendingSaveTransaction candidate =
                    pendingSaveTransactions[transactionIndex];
                if (candidate != null &&
                    !candidate.ScopeResolved &&
                    candidate.SourceRuntimeEpoch == runtimeEpoch &&
                    string.Equals(
                        candidate.SourceSaveKey,
                        activeSaveKey,
                        StringComparison.Ordinal) &&
                    (validationTransaction == null ||
                     candidate.Sequence > validationTransaction.Sequence))
                {
                    validationTransaction = candidate;
                }
            }

            if (validationTransaction == null)
            {
                errorMessage =
                    "No save transaction is available for deferred custom-data validation.";
                return false;
            }

            CoreSaveScope validationScope = validationTransaction.Published
                ? validationTransaction.TargetSaveScope
                : validationTransaction.StagingSaveScope;
            ICoreStorageEngine validationEngine;
            if (!TryCreateAndInitializeStorageEngine(
                CorePaths.GetDatabasePath(validationScope),
                CorePaths.GetFlatFileDatabasePath(validationScope),
                out validationEngine,
                out errorMessage))
            {
                return false;
            }

            bool valid = validationEngine.TryValidateCustomDataMutation(
                validationTransaction.TargetSaveKey,
                namespaceIdentifier,
                dataKey,
                jsonValue,
                remove,
                out errorMessage);
            if (valid)
            {
                valid = remove
                    ? validationEngine.TryRemoveCustomData(
                        validationTransaction.TargetSaveKey,
                        namespaceIdentifier,
                        dataKey,
                        out errorMessage)
                    : validationEngine.TrySetCustomData(
                        validationTransaction.TargetSaveKey,
                        namespaceIdentifier,
                        dataKey,
                        jsonValue,
                        out errorMessage);
            }

            if (valid)
            {
                valid = validationEngine.TryValidateIntegrity(
                    out errorMessage);
            }

            validationEngine.Dispose();
            if (!valid)
            {
                return false;
            }

            customDataMutationSequence++;
            deferredCustomDataMutations.Add(
                new CoreDeferredCustomDataMutation
                {
                    Sequence = customDataMutationSequence,
                    RuntimeEpoch = runtimeEpoch,
                    SaveKey = activeSaveKey,
                    NamespaceIdentifier = namespaceIdentifier,
                    DataKey = dataKey,
                    JsonValue = jsonValue ?? string.Empty,
                    Remove = remove
                });
            return true;
        }

        private bool TryReadDeferredCustomDataLocked(
            string namespaceIdentifier,
            string dataKey,
            out bool overlayFound,
            out bool valueFound,
            out string jsonValue)
        {
            overlayFound = false;
            valueFound = false;
            jsonValue = string.Empty;
            if (!HasUnresolvedSaveTransactionForActiveScopeLocked())
            {
                return true;
            }

            for (int mutationIndex =
                    deferredCustomDataMutations.Count - 1;
                mutationIndex >= 0;
                mutationIndex--)
            {
                CoreDeferredCustomDataMutation mutation =
                    deferredCustomDataMutations[mutationIndex];
                if (mutation == null ||
                    mutation.RuntimeEpoch != runtimeEpoch ||
                    !string.Equals(
                        mutation.SaveKey,
                        activeSaveKey,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        mutation.NamespaceIdentifier,
                        namespaceIdentifier,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        mutation.DataKey,
                        dataKey,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                overlayFound = true;
                valueFound = !mutation.Remove;
                jsonValue = mutation.Remove
                    ? string.Empty
                    : mutation.JsonValue;
                return true;
            }

            return true;
        }

        private void ObserveVanillaSaveWrite(
            CorePendingSaveTransaction transaction)
        {
            DateTime deadlineUtc = DateTime.UtcNow.AddMilliseconds(
                SaveObservationTimeoutMilliseconds);
            DateTime exactStableSinceUtc = DateTime.MinValue;
            long stableWriteTicks = 0L;
            string stableContentIdentity = string.Empty;
            CoreFileFingerprint matchedFingerprint = null;
            while (DateTime.UtcNow < deadlineUtc)
            {
                CoreFileFingerprint observedFingerprint;
                string ignoredErrorMessage;
                if (CoreFileFingerprintUtility.TryReadStable(
                    transaction.VanillaSaveFilePath,
                    out observedFingerprint,
                    out ignoredErrorMessage))
                {
                    if (HasObservedVanillaWriteTransition(
                        transaction,
                        observedFingerprint))
                    {
                        if (!string.Equals(
                                stableContentIdentity,
                                observedFingerprint.ContentIdentity,
                                StringComparison.Ordinal) ||
                            stableWriteTicks !=
                                observedFingerprint.LastWriteUtcTicks)
                        {
                            stableContentIdentity =
                                observedFingerprint.ContentIdentity;
                            stableWriteTicks =
                                observedFingerprint.LastWriteUtcTicks;
                            exactStableSinceUtc = DateTime.UtcNow;
                        }

                        matchedFingerprint = observedFingerprint;
                        if ((DateTime.UtcNow - exactStableSinceUtc)
                            .TotalMilliseconds >=
                            SaveObservationQuiescenceMilliseconds)
                        {
                            lock (runtimeLock)
                            {
                                if (transaction.ObservationCompleted)
                                {
                                    return;
                                }

                                transaction.ObservedFingerprint =
                                    matchedFingerprint;
                                transaction.VanillaWriteMatched = true;
                                transaction.ObservationCompleted = true;
                            }

                            return;
                        }
                    }
                    else
                    {
                        exactStableSinceUtc = DateTime.MinValue;
                        stableContentIdentity = string.Empty;
                        stableWriteTicks = 0L;
                    }
                }

                Thread.Sleep(SaveObservationPollMilliseconds);
            }

            lock (runtimeLock)
            {
                if (transaction.ObservationCompleted)
                {
                    return;
                }

                transaction.LastError =
                    "The vanilla save writer did not produce the expected stable payload before timeout.";
                transaction.VanillaWriteMatched = false;
                transaction.ObservationCompleted = true;
            }
        }

        /// <summary>
        /// Publishes every independently verified save and resolves active binding only
        /// after all callers in the runtime epoch have reached a terminal observation.
        /// </summary>
        private void ProcessCompletedSaveTransactionsLocked()
        {
            for (int transactionIndex = 0;
                transactionIndex < pendingSaveTransactions.Count;
                transactionIndex++)
            {
                CorePendingSaveTransaction seed =
                    pendingSaveTransactions[transactionIndex];
                if (seed == null ||
                    seed.Published ||
                    seed.ScopeResolved ||
                    seed.LoadSettlementPending)
                {
                    continue;
                }

                bool targetGroupTerminal = true;
                for (int candidateIndex = 0;
                    candidateIndex < pendingSaveTransactions.Count;
                    candidateIndex++)
                {
                    CorePendingSaveTransaction candidate =
                        pendingSaveTransactions[candidateIndex];
                    if (candidate != null &&
                        !candidate.ScopeResolved &&
                        string.Equals(
                            candidate.TargetSaveDirectory,
                            seed.TargetSaveDirectory,
                            StringComparison.OrdinalIgnoreCase) &&
                        (!candidate.ObservationCompleted ||
                         candidate.LoadSettlementPending))
                    {
                        targetGroupTerminal = false;
                        break;
                    }
                }

                if (!targetGroupTerminal)
                {
                    continue;
                }

                bool anyMatchedCandidate = false;
                for (int candidateIndex = 0;
                    candidateIndex < pendingSaveTransactions.Count;
                    candidateIndex++)
                {
                    CorePendingSaveTransaction candidate =
                        pendingSaveTransactions[candidateIndex];
                    if (candidate != null &&
                        !candidate.ScopeResolved &&
                        candidate.ObservationCompleted &&
                        candidate.VanillaWriteMatched &&
                        string.Equals(
                            candidate.TargetSaveDirectory,
                            seed.TargetSaveDirectory,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        anyMatchedCandidate = true;
                        break;
                    }
                }

                if (!anyMatchedCandidate)
                {
                    MarkTargetGroupSupersededLocked(
                        seed.TargetSaveDirectory,
                        null);
                    continue;
                }

                CoreFileFingerprint finalFingerprint;
                string fingerprintErrorMessage;
                if (!CoreFileFingerprintUtility.TryReadStable(
                    seed.VanillaSaveFilePath,
                    out finalFingerprint,
                    out fingerprintErrorMessage))
                {
                    seed.LastError = fingerprintErrorMessage;
                    long nowTicks = DateTime.UtcNow.Ticks;
                    if (seed.SettlementFailureStartedUtcTicks == 0L)
                    {
                        seed.SettlementFailureStartedUtcTicks = nowTicks;
                    }
                    else if (nowTicks -
                        seed.SettlementFailureStartedUtcTicks >=
                            SaveSettlementFailureGraceTicks)
                    {
                        MarkTargetGroupSupersededLocked(
                            seed.TargetSaveDirectory,
                            null);
                    }
                    continue;
                }

                seed.SettlementFailureStartedUtcTicks = 0L;

                CorePendingSaveTransaction winner = null;
                for (int candidateIndex = 0;
                    candidateIndex < pendingSaveTransactions.Count;
                    candidateIndex++)
                {
                    CorePendingSaveTransaction candidate =
                        pendingSaveTransactions[candidateIndex];
                    if (candidate == null ||
                        candidate.ScopeResolved ||
                        !candidate.ObservationCompleted ||
                        !candidate.VanillaWriteMatched ||
                        !string.Equals(
                            candidate.TargetSaveDirectory,
                            seed.TargetSaveDirectory,
                            StringComparison.OrdinalIgnoreCase) ||
                        candidate.ExpectedLength != finalFingerprint.Length ||
                        !string.Equals(
                            candidate.ExpectedSha256,
                            finalFingerprint.Sha256,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (winner == null ||
                        candidate.Sequence > winner.Sequence)
                    {
                        winner = candidate;
                    }
                }

                if (winner == null)
                {
                    // Every previously observed payload has since been superseded.
                    // Durable stages remain available for exact recovery, but none may
                    // publish over the target's final bytes in this runtime.
                    MarkTargetGroupSupersededLocked(
                        seed.TargetSaveDirectory,
                        null);
                    continue;
                }

                winner.ObservedFingerprint = finalFingerprint;
                string publishErrorMessage;
                if (!TryCheckpointAndPublishSaveStageLocked(
                    winner,
                    out publishErrorMessage))
                {
                    winner.LastError = publishErrorMessage;
                    continue;
                }

                MarkTargetGroupSupersededLocked(
                    seed.TargetSaveDirectory,
                    winner);
            }

            ResolveCompletedSaveTransactionGroupsLocked();
        }

        private void MarkTargetGroupSupersededLocked(
            string targetSaveDirectory,
            CorePendingSaveTransaction winner)
        {
            for (int transactionIndex = 0;
                transactionIndex < pendingSaveTransactions.Count;
                transactionIndex++)
            {
                CorePendingSaveTransaction candidate =
                    pendingSaveTransactions[transactionIndex];
                if (candidate != null &&
                    !ReferenceEquals(candidate, winner) &&
                    !candidate.Published &&
                    string.Equals(
                        candidate.TargetSaveDirectory,
                        targetSaveDirectory,
                        StringComparison.OrdinalIgnoreCase))
                {
                    candidate.VanillaWriteMatched = false;
                    if (winner != null &&
                        candidate.ExpectedLength == winner.ExpectedLength &&
                        string.Equals(
                            candidate.ExpectedSha256,
                            winner.ExpectedSha256,
                            StringComparison.Ordinal) &&
                        Directory.Exists(candidate.StagingSaveDirectory))
                    {
                        // Same-content intents cannot become newly authoritative after
                        // the installed highest-sequence winner. Retaining them would
                        // let startup recovery overwrite that winner with stale state.
                        TryCleanupStagingSaveDirectory(
                            candidate.StagingSaveDirectory);
                    }
                }
            }
        }

        private static bool HasObservedVanillaWriteTransition(
            CorePendingSaveTransaction transaction,
            CoreFileFingerprint observedFingerprint)
        {
            if (transaction == null || observedFingerprint == null ||
                observedFingerprint.Length != transaction.ExpectedLength ||
                !string.Equals(
                    observedFingerprint.Sha256,
                    transaction.ExpectedSha256,
                    StringComparison.Ordinal))
            {
                return false;
            }

            return !transaction.BaselineExisted ||
                observedFingerprint.Length != transaction.BaselineLength ||
                observedFingerprint.LastWriteUtcTicks !=
                    transaction.BaselineWriteUtcTicks ||
                !string.Equals(
                    observedFingerprint.Sha256,
                    transaction.BaselineSha256,
                    StringComparison.Ordinal);
        }

        private bool TryCheckpointAndPublishSaveStageLocked(
            CorePendingSaveTransaction transaction,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (transaction == null ||
                transaction.ObservedFingerprint == null ||
                !Directory.Exists(transaction.StagingSaveDirectory))
            {
                errorMessage = "The verified sidecar save stage is unavailable.";
                return false;
            }

            CoreFileFingerprint currentFingerprint;
            string currentFingerprintError;
            if (!CoreFileFingerprintUtility.TryReadStable(
                transaction.VanillaSaveFilePath,
                out currentFingerprint,
                out currentFingerprintError))
            {
                errorMessage = currentFingerprintError;
                return false;
            }

            if (currentFingerprint.Length != transaction.ExpectedLength ||
                !string.Equals(
                    currentFingerprint.Sha256,
                    transaction.ExpectedSha256,
                    StringComparison.Ordinal))
            {
                transaction.VanillaWriteMatched = false;
                errorMessage =
                    "The observed save payload was superseded before sidecar publish.";
                return false;
            }

            transaction.ObservedFingerprint = currentFingerprint;

            ICoreStorageEngine stagingEngine;
            if (!TryCreateAndInitializeStorageEngine(
                CorePaths.GetDatabasePath(transaction.StagingSaveScope),
                CorePaths.GetFlatFileDatabasePath(transaction.StagingSaveScope),
                out stagingEngine,
                out errorMessage))
            {
                return false;
            }

            bool checkpointed = stagingEngine.TryValidateIntegrity(
                out errorMessage);

            stagingEngine.Dispose();
            if (!checkpointed)
            {
                return false;
            }

            bool targetIsActive = initialized &&
                storageEngine != null &&
                PathsReferToSameDirectory(
                    activeSaveDirectory,
                    transaction.TargetSaveDirectory);
            CoreSaveScope activeScopeBeforePublish = activeSaveScope;
            if (targetIsActive)
            {
                DisposeStorageLocked();
                ResetStorageBindingLocked();
            }

            if (!CorePaths.TryPublishStagingDirectory(
                transaction.StagingSaveDirectory,
                transaction.TargetSaveScope,
                transaction.Token,
                transaction.ObservedFingerprint.ContentIdentity,
                out errorMessage))
            {
                string recoveryErrorMessage;
                bool recoverySucceeded =
                    CorePaths.TryRecoverInterruptedPublishes(
                        out recoveryErrorMessage);
                CorePendingSaveTransaction installedTransaction;
                string installedManifestError;
                bool installedExpectedStage = recoverySucceeded &&
                    TryReadSaveIntentManifest(
                        transaction.TargetSaveDirectory,
                        false,
                        out installedTransaction,
                        out installedManifestError) &&
                    installedTransaction != null &&
                    string.Equals(
                        installedTransaction.Token,
                        transaction.Token,
                        StringComparison.Ordinal) &&
                    installedTransaction.ExpectedLength ==
                        transaction.ExpectedLength &&
                    string.Equals(
                        installedTransaction.ExpectedSha256,
                        transaction.ExpectedSha256,
                        StringComparison.Ordinal);
                if (installedExpectedStage)
                {
                    transaction.Published = true;
                    if (targetIsActive)
                    {
                        CorePaths.UseSaveScope(
                            transaction.TargetSaveScope);
                        if (!InitializePreparedStorageLocked(
                            transaction.TargetSaveScope,
                            out errorMessage))
                        {
                            return false;
                        }
                    }

                    return true;
                }

                if (targetIsActive)
                {
                    CorePaths.RestoreSaveScope(activeScopeBeforePublish);
                    string ignoredReopenError;
                    InitializePreparedStorageLocked(
                        activeScopeBeforePublish,
                        out ignoredReopenError);
                }

                if (!recoverySucceeded &&
                    !string.IsNullOrEmpty(recoveryErrorMessage))
                {
                    errorMessage += " Recovery failed: " +
                        recoveryErrorMessage;
                }

                return false;
            }

            transaction.Published = true;

            if (targetIsActive)
            {
                CorePaths.UseSaveScope(transaction.TargetSaveScope);
                if (!InitializePreparedStorageLocked(
                    transaction.TargetSaveScope,
                    out errorMessage))
                {
                    return false;
                }
            }

            return true;
        }

        private void ResolveCompletedSaveTransactionGroupsLocked()
        {
            for (int transactionIndex = 0;
                transactionIndex < pendingSaveTransactions.Count;
                transactionIndex++)
            {
                CorePendingSaveTransaction seed =
                    pendingSaveTransactions[transactionIndex];
                if (seed == null || seed.ScopeResolved)
                {
                    continue;
                }

                bool allTerminal = true;
                CorePendingSaveTransaction latestSuccessful = null;
                for (int candidateIndex = 0;
                    candidateIndex < pendingSaveTransactions.Count;
                    candidateIndex++)
                {
                    CorePendingSaveTransaction candidate =
                        pendingSaveTransactions[candidateIndex];
                    if (candidate == null ||
                        candidate.ScopeResolved ||
                        candidate.SourceRuntimeEpoch !=
                            seed.SourceRuntimeEpoch ||
                        !string.Equals(
                            candidate.SourceSaveKey,
                            seed.SourceSaveKey,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!candidate.ObservationCompleted ||
                        (candidate.VanillaWriteMatched &&
                         !candidate.Published))
                    {
                        allTerminal = false;
                    }

                    if (candidate.Published &&
                        IsPublishedTransactionStillAuthoritativeLocked(
                            candidate) &&
                        (latestSuccessful == null ||
                         candidate.Sequence >
                            latestSuccessful.Sequence))
                    {
                        latestSuccessful = candidate;
                    }
                }

                if (!allTerminal)
                {
                    continue;
                }

                string resolutionErrorMessage;
                bool resolved;
                if (latestSuccessful != null &&
                    seed.SourceRuntimeEpoch == runtimeEpoch &&
                    string.Equals(
                        seed.SourceSaveKey,
                        activeSaveKey,
                        StringComparison.Ordinal))
                {
                    resolved = TryBindLatestSuccessfulSaveLocked(
                        latestSuccessful,
                        out resolutionErrorMessage);
                }
                else
                {
                    resolved = TryReturnDeferredCustomDataToSourceLocked(
                        seed,
                        out resolutionErrorMessage);
                }

                if (!resolved)
                {
                    seed.LastError = resolutionErrorMessage;
                    continue;
                }

                CleanupInstalledSaveManifestsForGroupLocked(seed);

                for (int candidateIndex = 0;
                    candidateIndex < pendingSaveTransactions.Count;
                    candidateIndex++)
                {
                    CorePendingSaveTransaction candidate =
                        pendingSaveTransactions[candidateIndex];
                    if (candidate != null &&
                        candidate.SourceRuntimeEpoch ==
                            seed.SourceRuntimeEpoch &&
                        string.Equals(
                            candidate.SourceSaveKey,
                            seed.SourceSaveKey,
                            StringComparison.Ordinal))
                    {
                        candidate.ScopeResolved = true;
                    }
                }
            }

            for (int transactionIndex =
                    pendingSaveTransactions.Count - 1;
                transactionIndex >= 0;
                transactionIndex--)
            {
                if (pendingSaveTransactions[transactionIndex] == null ||
                    pendingSaveTransactions[transactionIndex].ScopeResolved)
                {
                    pendingSaveTransactions.RemoveAt(transactionIndex);
                }
            }
        }

        private bool IsPublishedTransactionStillAuthoritativeLocked(
            CorePendingSaveTransaction transaction)
        {
            CoreFileFingerprint currentFingerprint;
            string ignoredFingerprintError;
            if (!CoreFileFingerprintUtility.TryReadStable(
                    transaction.VanillaSaveFilePath,
                    out currentFingerprint,
                    out ignoredFingerprintError) ||
                currentFingerprint.Length != transaction.ExpectedLength ||
                !string.Equals(
                    currentFingerprint.Sha256,
                    transaction.ExpectedSha256,
                    StringComparison.Ordinal))
            {
                return false;
            }

            CorePendingSaveTransaction installed;
            string ignoredManifestError;
            return TryReadSaveIntentManifest(
                    transaction.TargetSaveDirectory,
                    false,
                    out installed,
                    out ignoredManifestError) &&
                installed != null &&
                string.Equals(
                    installed.Token,
                    transaction.Token,
                    StringComparison.Ordinal);
        }

        private void CleanupInstalledSaveManifestsForGroupLocked(
            CorePendingSaveTransaction seed)
        {
            for (int transactionIndex = 0;
                transactionIndex < pendingSaveTransactions.Count;
                transactionIndex++)
            {
                CorePendingSaveTransaction candidate =
                    pendingSaveTransactions[transactionIndex];
                if (candidate == null ||
                    !candidate.Published ||
                    candidate.SourceRuntimeEpoch !=
                        seed.SourceRuntimeEpoch ||
                    !string.Equals(
                        candidate.SourceSaveKey,
                        seed.SourceSaveKey,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                CorePendingSaveTransaction installed;
                string ignoredReadError;
                if (TryReadSaveIntentManifest(
                        candidate.TargetSaveDirectory,
                        false,
                        out installed,
                        out ignoredReadError) &&
                    installed != null &&
                    string.Equals(
                        installed.Token,
                        candidate.Token,
                        StringComparison.Ordinal))
                {
                    string ignoredDeleteError;
                    CorePaths.TryDeleteContainedFile(
                        Path.Combine(
                            candidate.TargetSaveDirectory,
                            SaveTransactionManifestFileName),
                        out ignoredDeleteError);
                }
            }
        }

        private bool TryBindLatestSuccessfulSaveLocked(
            CorePendingSaveTransaction transaction,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            ICoreStorageEngine targetEngine;
            if (!TryCreateAndInitializeStorageEngine(
                CorePaths.GetDatabasePath(transaction.TargetSaveScope),
                CorePaths.GetFlatFileDatabasePath(transaction.TargetSaveScope),
                out targetEngine,
                out errorMessage))
            {
                return false;
            }

            List<PendingEvent> laterEvents =
                CloneEventsAfterSequenceLocked(
                    transaction.SourceSaveKey,
                    transaction.TargetSaveKey,
                    transaction.CaptureBaselineSequence);
            List<SingleParticipationProjection> laterSingleRows =
                CloneSingleRowsAfterSequenceLocked(
                    transaction.SourceSaveKey,
                    transaction.TargetSaveKey,
                    transaction.CaptureBaselineSequence);
            List<StatusTransitionProjection> laterTransitions =
                CloneTransitionsAfterSequenceLocked(
                    transaction.SourceSaveKey,
                    transaction.TargetSaveKey,
                    transaction.CaptureBaselineSequence);
            bool applied = true;
            if (laterEvents.Count > 0 ||
                laterSingleRows.Count > 0 ||
                laterTransitions.Count > 0)
            {
                applied = targetEngine.PersistBatch(
                    laterEvents,
                    laterSingleRows,
                    laterTransitions,
                    out errorMessage);
            }

            if (applied)
            {
                applied = ApplyDeferredCustomMutationsToEngineLocked(
                    targetEngine,
                    transaction.SourceSaveKey,
                    transaction.SourceRuntimeEpoch,
                    transaction.TargetSaveKey,
                    transaction.CustomBaselineSequence,
                    long.MaxValue,
                    out errorMessage);
            }

            if (applied)
            {
                applied = targetEngine.TryValidateIntegrity(
                    out errorMessage);
            }

            targetEngine.Dispose();
            if (!applied)
            {
                return false;
            }

            DisposeStorageLocked();
            ResetStorageBindingLocked();
            CorePaths.UseSaveScope(transaction.TargetSaveScope);
            if (!InitializePreparedStorageLocked(
                transaction.TargetSaveScope,
                out errorMessage))
            {
                return false;
            }

            RemoveBufferedDataForSaveKeyLocked(
                transaction.SourceSaveKey);
            RemoveDeferredCustomMutationsLocked(
                transaction.SourceSaveKey,
                transaction.SourceRuntimeEpoch);
            runtimeEpoch++;
            if (transaction.SourceWasTransient &&
                !PathsReferToSameDirectory(
                    transaction.SourceSaveDirectory,
                    transaction.TargetSaveDirectory))
            {
                TryCleanupTransientSaveDirectory(
                    transaction.SourceSaveDirectory);
            }

            return true;
        }

        private bool TryReturnDeferredCustomDataToSourceLocked(
            CorePendingSaveTransaction transaction,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            bool activeSourceMatches = initialized &&
                storageEngine != null &&
                transaction.SourceRuntimeEpoch == runtimeEpoch &&
                string.Equals(
                    transaction.SourceSaveKey,
                    activeSaveKey,
                    StringComparison.Ordinal);
            ICoreStorageEngine sourceEngine = storageEngine;
            bool disposeSourceEngine = false;
            if (!activeSourceMatches)
            {
                if (!TryCreateAndInitializeStorageEngine(
                    CorePaths.GetDatabasePath(transaction.SourceSaveScope),
                    CorePaths.GetFlatFileDatabasePath(transaction.SourceSaveScope),
                    out sourceEngine,
                    out errorMessage))
                {
                    return false;
                }

                disposeSourceEngine = true;
            }

            bool applied = ApplyDeferredCustomMutationsToEngineLocked(
                sourceEngine,
                transaction.SourceSaveKey,
                transaction.SourceRuntimeEpoch,
                transaction.SourceSaveKey,
                0L,
                long.MaxValue,
                out errorMessage);
            if (disposeSourceEngine)
            {
                sourceEngine.Dispose();
            }

            if (!applied)
            {
                return false;
            }

            RemoveDeferredCustomMutationsLocked(
                transaction.SourceSaveKey,
                transaction.SourceRuntimeEpoch);
            return true;
        }

        private bool ApplyDeferredCustomMutationsToEngineLocked(
            ICoreStorageEngine engine,
            string sourceSaveKey,
            long sourceRuntimeEpoch,
            string targetSaveKey,
            long exclusiveMinimumSequence,
            long inclusiveMaximumSequence,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            for (int mutationIndex = 0;
                mutationIndex < deferredCustomDataMutations.Count;
                mutationIndex++)
            {
                CoreDeferredCustomDataMutation mutation =
                    deferredCustomDataMutations[mutationIndex];
                if (mutation == null ||
                    mutation.RuntimeEpoch != sourceRuntimeEpoch ||
                    !string.Equals(
                        mutation.SaveKey,
                        sourceSaveKey,
                        StringComparison.Ordinal) ||
                    mutation.Sequence <= exclusiveMinimumSequence ||
                    mutation.Sequence > inclusiveMaximumSequence)
                {
                    continue;
                }

                bool succeeded = mutation.Remove
                    ? engine.TryRemoveCustomData(
                        targetSaveKey,
                        mutation.NamespaceIdentifier,
                        mutation.DataKey,
                        out errorMessage)
                    : engine.TrySetCustomData(
                        targetSaveKey,
                        mutation.NamespaceIdentifier,
                        mutation.DataKey,
                        mutation.JsonValue,
                        out errorMessage);
                if (!succeeded)
                {
                    return false;
                }
            }

            return true;
        }

        private void RemoveDeferredCustomMutationsLocked(
            string saveKey,
            long sourceRuntimeEpoch)
        {
            for (int mutationIndex =
                    deferredCustomDataMutations.Count - 1;
                mutationIndex >= 0;
                mutationIndex--)
            {
                CoreDeferredCustomDataMutation mutation =
                    deferredCustomDataMutations[mutationIndex];
                if (mutation != null &&
                    mutation.RuntimeEpoch == sourceRuntimeEpoch &&
                    string.Equals(
                        mutation.SaveKey,
                        saveKey,
                        StringComparison.Ordinal))
                {
                    deferredCustomDataMutations.RemoveAt(mutationIndex);
                }
            }
        }

        private bool TryCloneAuthoritativeStorageIntoStageLocked(
            string sourceSaveDirectory,
            CoreSaveScope stagingSaveScope,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrEmpty(sourceSaveDirectory) ||
                !Directory.Exists(sourceSaveDirectory) ||
                !TryValidateSourceStorageDirectory(
                    sourceSaveDirectory,
                    out errorMessage))
            {
                if (string.IsNullOrEmpty(errorMessage))
                {
                    errorMessage = "The source sidecar directory is unavailable.";
                }

                return false;
            }

            string stagingDirectory =
                CorePaths.GetSaveDirectory(stagingSaveScope);
            TryCleanupStagingSaveDirectory(stagingDirectory);
            string normalizedStagingDirectory;
            if (!CorePaths.TryCreateContainedDirectory(
                stagingDirectory,
                out normalizedStagingDirectory,
                out errorMessage))
            {
                return false;
            }

            string sourceSqlitePath = Path.Combine(
                sourceSaveDirectory,
                CoreConstants.DatabaseFileName);
            string sourceFlatPath = Path.Combine(
                sourceSaveDirectory,
                CoreConstants.FlatFileDatabaseFileName);
            bool sqliteExists = File.Exists(sourceSqlitePath);
            bool flatExists = FlatStorageFamilyHasCandidate(sourceFlatPath);
            if (!sqliteExists && !flatExists)
            {
                errorMessage = "The source sidecar has no storage file.";
                return false;
            }

            DateTime sqliteWriteUtc = sqliteExists
                ? GetStorageFamilyLastWriteUtc(sourceSqlitePath)
                : DateTime.MinValue;
            DateTime flatWriteUtc = flatExists
                ? GetFlatStorageFamilyLastWriteUtc(sourceFlatPath)
                : DateTime.MinValue;
            bool trySqliteFirst = sqliteExists &&
                (!flatExists || sqliteWriteUtc >= flatWriteUtc);
            if (TryCloneStorageBackendCandidate(
                    sourceSqlitePath,
                    sourceFlatPath,
                    stagingSaveScope,
                    trySqliteFirst,
                    out errorMessage) ||
                (sqliteExists && flatExists &&
                 TryCloneStorageBackendCandidate(
                    sourceSqlitePath,
                    sourceFlatPath,
                    stagingSaveScope,
                    !trySqliteFirst,
                    out errorMessage)))
            {
                return true;
            }

            return false;
        }

        private bool TryCloneStorageBackendCandidate(
            string sourceSqlitePath,
            string sourceFlatPath,
            CoreSaveScope stagingSaveScope,
            bool cloneSqlite,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            string stagingDirectory =
                CorePaths.GetSaveDirectory(stagingSaveScope);
            TryCleanupStagingSaveDirectory(stagingDirectory);
            string ignoredDirectory;
            if (!CorePaths.TryCreateContainedDirectory(
                stagingDirectory,
                out ignoredDirectory,
                out errorMessage))
            {
                return false;
            }

            try
            {
                string sourcePath = cloneSqlite
                    ? sourceSqlitePath
                    : sourceFlatPath;
                if (cloneSqlite &&
                    (!File.Exists(sourcePath) ||
                     (File.GetAttributes(sourcePath) &
                        FileAttributes.ReparsePoint) != 0))
                {
                    errorMessage =
                        "The selected source storage file is unavailable or a reparse point.";
                    return false;
                }

                string targetPath = cloneSqlite
                    ? CorePaths.GetDatabasePath(stagingSaveScope)
                    : CorePaths.GetFlatFileDatabasePath(stagingSaveScope);
                string normalizedTargetPath;
                if (!CorePaths.TryValidateContainedMutationPath(
                    targetPath,
                    false,
                    out normalizedTargetPath,
                    out errorMessage))
                {
                    return false;
                }

                if (cloneSqlite)
                {
                    File.Copy(sourcePath, normalizedTargetPath, false);
                    string sourceWalPath = sourceSqlitePath +
                        CoreConstants.SqliteWriteAheadLogFileSuffix;
                    if (File.Exists(sourceWalPath))
                    {
                        if ((File.GetAttributes(sourceWalPath) &
                            FileAttributes.ReparsePoint) != 0)
                        {
                            errorMessage =
                                "The source SQLite WAL is a reparse point.";
                            return false;
                        }

                        string targetWalPath = normalizedTargetPath +
                            CoreConstants.SqliteWriteAheadLogFileSuffix;
                        string normalizedTargetWalPath;
                        if (!CorePaths.TryValidateContainedMutationPath(
                            targetWalPath,
                            false,
                            out normalizedTargetWalPath,
                            out errorMessage))
                        {
                            return false;
                        }

                        File.Copy(
                            sourceWalPath,
                            normalizedTargetWalPath,
                            false);
                    }
                }
                else
                {
                    string[] flatFamilySuffixes =
                        new string[] { string.Empty, ".tmp", ".bak" };
                    bool copiedFlatCandidate = false;
                    for (int suffixIndex = 0;
                        suffixIndex < flatFamilySuffixes.Length;
                        suffixIndex++)
                    {
                        string suffix = flatFamilySuffixes[suffixIndex];
                        string sourceFamilyPath = sourceFlatPath + suffix;
                        if (!File.Exists(sourceFamilyPath))
                        {
                            continue;
                        }

                        if ((File.GetAttributes(sourceFamilyPath) &
                            FileAttributes.ReparsePoint) != 0)
                        {
                            errorMessage =
                                "A source flat-file recovery artifact is a reparse point.";
                            return false;
                        }

                        string targetFamilyPath = normalizedTargetPath + suffix;
                        string normalizedTargetFamilyPath;
                        if (!CorePaths.TryValidateContainedMutationPath(
                            targetFamilyPath,
                            false,
                            out normalizedTargetFamilyPath,
                            out errorMessage))
                        {
                            return false;
                        }

                        File.Copy(
                            sourceFamilyPath,
                            normalizedTargetFamilyPath,
                            false);
                        copiedFlatCandidate = true;
                    }

                    if (!copiedFlatCandidate)
                    {
                        errorMessage =
                            "The selected flat-file storage family is unavailable.";
                        return false;
                    }
                }

                ICoreStorageEngine validationEngine;
                bool initializedCandidate = cloneSqlite
                    ? TryInitializeSqliteStorageEngine(
                        CorePaths.GetDatabasePath(stagingSaveScope),
                        out validationEngine,
                        out errorMessage)
                    : TryInitializeFlatFileStorageEngine(
                        CorePaths.GetFlatFileDatabasePath(stagingSaveScope),
                        out validationEngine,
                        out errorMessage);
                if (!initializedCandidate)
                {
                    return false;
                }

                bool valid = validationEngine.TryValidateIntegrity(
                    out errorMessage);
                validationEngine.Dispose();
                return valid;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        private static DateTime GetStorageFamilyLastWriteUtc(
            string sqlitePath)
        {
            DateTime latest = File.GetLastWriteTimeUtc(sqlitePath);
            string walPath = sqlitePath +
                CoreConstants.SqliteWriteAheadLogFileSuffix;
            if (File.Exists(walPath))
            {
                DateTime walWriteUtc = File.GetLastWriteTimeUtc(walPath);
                if (walWriteUtc > latest)
                {
                    latest = walWriteUtc;
                }
            }

            return latest;
        }

        private static bool FlatStorageFamilyHasCandidate(
            string flatFilePath)
        {
            return File.Exists(flatFilePath) ||
                File.Exists(flatFilePath + ".tmp") ||
                File.Exists(flatFilePath + ".bak");
        }

        private static DateTime GetFlatStorageFamilyLastWriteUtc(
            string flatFilePath)
        {
            DateTime latest = DateTime.MinValue;
            string[] suffixes = new string[]
            {
                string.Empty,
                ".tmp",
                ".bak"
            };
            for (int suffixIndex = 0;
                suffixIndex < suffixes.Length;
                suffixIndex++)
            {
                string candidatePath = flatFilePath + suffixes[suffixIndex];
                if (File.Exists(candidatePath))
                {
                    DateTime candidateWriteUtc =
                        File.GetLastWriteTimeUtc(candidatePath);
                    if (candidateWriteUtc > latest)
                    {
                        latest = candidateWriteUtc;
                    }
                }
            }

            return latest;
        }

        private static bool TryValidateSourceStorageDirectory(
            string sourceSaveDirectory,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                DirectoryInfo currentDirectory = new DirectoryInfo(
                    Path.GetFullPath(sourceSaveDirectory));
                while (currentDirectory != null)
                {
                    if (currentDirectory.Exists &&
                        (currentDirectory.Attributes &
                            FileAttributes.ReparsePoint) != 0)
                    {
                        errorMessage =
                            "The source sidecar path traverses a reparse point.";
                        return false;
                    }

                    currentDirectory = currentDirectory.Parent;
                }

                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        private List<PendingEvent> CloneEventsThroughSequenceLocked(
            string sourceSaveKey,
            string targetSaveKey,
            long maximumSequence)
        {
            return CloneEventsInSequenceRangeLocked(
                sourceSaveKey,
                targetSaveKey,
                0L,
                maximumSequence);
        }

        private List<PendingEvent> CloneEventsAfterSequenceLocked(
            string sourceSaveKey,
            string targetSaveKey,
            long minimumSequence)
        {
            return CloneEventsInSequenceRangeLocked(
                sourceSaveKey,
                targetSaveKey,
                minimumSequence,
                long.MaxValue);
        }

        private List<PendingEvent> CloneEventsInSequenceRangeLocked(
            string sourceSaveKey,
            string targetSaveKey,
            long exclusiveMinimumSequence,
            long inclusiveMaximumSequence)
        {
            List<PendingEvent> result = new List<PendingEvent>();
            for (int eventIndex = 0;
                eventIndex < bufferedEvents.Count;
                eventIndex++)
            {
                PendingEvent source = bufferedEvents[eventIndex];
                if (source == null ||
                    source.CaptureSequence <= exclusiveMinimumSequence ||
                    source.CaptureSequence > inclusiveMaximumSequence ||
                    !string.Equals(
                        source.SaveKey,
                        sourceSaveKey,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                result.Add(new PendingEvent
                {
                    CaptureSequence = source.CaptureSequence,
                    SaveKey = targetSaveKey,
                    GameDateKey = source.GameDateKey,
                    GameDateTime = source.GameDateTime,
                    IdolId = source.IdolId,
                    EntityKind = source.EntityKind,
                    EntityId = source.EntityId,
                    EventType = source.EventType,
                    SourcePatch = source.SourcePatch,
                    NamespaceIdentifier = source.NamespaceIdentifier,
                    PayloadJson = source.PayloadJson
                });
            }

            return result;
        }

        private List<SingleParticipationProjection>
            CloneSingleRowsThroughSequenceLocked(
                string sourceSaveKey,
                string targetSaveKey,
                long maximumSequence)
        {
            return CloneSingleRowsInSequenceRangeLocked(
                sourceSaveKey,
                targetSaveKey,
                0L,
                maximumSequence);
        }

        private List<SingleParticipationProjection>
            CloneSingleRowsAfterSequenceLocked(
                string sourceSaveKey,
                string targetSaveKey,
                long minimumSequence)
        {
            return CloneSingleRowsInSequenceRangeLocked(
                sourceSaveKey,
                targetSaveKey,
                minimumSequence,
                long.MaxValue);
        }

        private List<SingleParticipationProjection>
            CloneSingleRowsInSequenceRangeLocked(
                string sourceSaveKey,
                string targetSaveKey,
                long exclusiveMinimumSequence,
                long inclusiveMaximumSequence)
        {
            List<SingleParticipationProjection> result =
                new List<SingleParticipationProjection>();
            for (int rowIndex = 0;
                rowIndex < bufferedSingleParticipationRows.Count;
                rowIndex++)
            {
                SingleParticipationProjection source =
                    bufferedSingleParticipationRows[rowIndex];
                if (source == null ||
                    source.CaptureSequence <= exclusiveMinimumSequence ||
                    source.CaptureSequence > inclusiveMaximumSequence ||
                    !string.Equals(
                        source.SaveKey,
                        sourceSaveKey,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                result.Add(new SingleParticipationProjection
                {
                    CaptureSequence = source.CaptureSequence,
                    SaveKey = targetSaveKey,
                    SingleId = source.SingleId,
                    IdolId = source.IdolId,
                    RowIndex = source.RowIndex,
                    PositionIndex = source.PositionIndex,
                    IsCenterFlag = source.IsCenterFlag,
                    ReleaseDate = source.ReleaseDate
                });
            }

            return result;
        }

        private List<StatusTransitionProjection>
            CloneTransitionsThroughSequenceLocked(
                string sourceSaveKey,
                string targetSaveKey,
                long maximumSequence)
        {
            return CloneTransitionsInSequenceRangeLocked(
                sourceSaveKey,
                targetSaveKey,
                0L,
                maximumSequence);
        }

        private List<StatusTransitionProjection>
            CloneTransitionsAfterSequenceLocked(
                string sourceSaveKey,
                string targetSaveKey,
                long minimumSequence)
        {
            return CloneTransitionsInSequenceRangeLocked(
                sourceSaveKey,
                targetSaveKey,
                minimumSequence,
                long.MaxValue);
        }

        private List<StatusTransitionProjection>
            CloneTransitionsInSequenceRangeLocked(
                string sourceSaveKey,
                string targetSaveKey,
                long exclusiveMinimumSequence,
                long inclusiveMaximumSequence)
        {
            List<StatusTransitionProjection> result =
                new List<StatusTransitionProjection>();
            for (int transitionIndex = 0;
                transitionIndex < bufferedStatusTransitions.Count;
                transitionIndex++)
            {
                StatusTransitionProjection source =
                    bufferedStatusTransitions[transitionIndex];
                if (source == null ||
                    source.CaptureSequence <= exclusiveMinimumSequence ||
                    source.CaptureSequence > inclusiveMaximumSequence ||
                    !string.Equals(
                        source.SaveKey,
                        sourceSaveKey,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                result.Add(new StatusTransitionProjection
                {
                    CaptureSequence = source.CaptureSequence,
                    SaveKey = targetSaveKey,
                    IdolId = source.IdolId,
                    PreviousStatusCode = source.PreviousStatusCode,
                    NewStatusCode = source.NewStatusCode,
                    TransitionDate = source.TransitionDate
                });
            }

            return result;
        }

        private static bool TryWriteSaveIntentManifest(
            CorePendingSaveTransaction transaction,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            string manifestPayload = BuildSaveIntentManifestPayload(
                transaction);
            if (string.IsNullOrEmpty(manifestPayload))
            {
                errorMessage = "The save intent fields are invalid.";
                return false;
            }

            string manifestText = manifestPayload +
                "checksum=" +
                CoreFileFingerprintUtility.ComputeSha256(
                    new UTF8Encoding(false).GetBytes(manifestPayload)) +
                "\n";
            string manifestPath = Path.Combine(
                transaction.StagingSaveDirectory,
                SaveTransactionManifestFileName);
            string temporaryManifestPath = manifestPath + "." +
                transaction.Token +
                ".tmp";
            string normalizedManifestPath;
            string normalizedTemporaryManifestPath;
            if (!CorePaths.TryValidateContainedMutationPath(
                    manifestPath,
                    false,
                    out normalizedManifestPath,
                    out errorMessage) ||
                !CorePaths.TryValidateContainedMutationPath(
                    temporaryManifestPath,
                    false,
                    out normalizedTemporaryManifestPath,
                    out errorMessage))
            {
                return false;
            }

            try
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(manifestText);
                using (FileStream stream = new FileStream(
                    normalizedTemporaryManifestPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                File.Move(
                    normalizedTemporaryManifestPath,
                    normalizedManifestPath);
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        private static string BuildSaveIntentManifestPayload(
            CorePendingSaveTransaction transaction)
        {
            if (transaction == null ||
                transaction.Token.Length != 32 ||
                string.IsNullOrEmpty(transaction.VanillaRelativeSavePath) ||
                string.IsNullOrEmpty(transaction.ExpectedSha256))
            {
                return string.Empty;
            }

            return string.Join(
                "\n",
                new string[]
                {
                    "version=1",
                    "token=" + transaction.Token,
                    "vanilla=" + transaction.VanillaRelativeSavePath.Replace('\\', '/'),
                    "stage=save_" + transaction.Token,
                    "source_key=" + transaction.SourceSaveKey,
                    "target_key=" + transaction.TargetSaveKey,
                    "expected_length=" + transaction.ExpectedLength.ToString(CultureInfo.InvariantCulture),
                    "expected_sha256=" + transaction.ExpectedSha256,
                    "baseline_length=" + transaction.BaselineLength.ToString(CultureInfo.InvariantCulture),
                    "baseline_existed=" + (transaction.BaselineExisted ? "1" : "0"),
                    "baseline_write_ticks=" + transaction.BaselineWriteUtcTicks.ToString(CultureInfo.InvariantCulture),
                    "baseline_sha256=" + (string.IsNullOrEmpty(transaction.BaselineSha256) ? "-" : transaction.BaselineSha256),
                    "sequence=" + transaction.Sequence.ToString(CultureInfo.InvariantCulture),
                    "runtime_epoch=" + transaction.SourceRuntimeEpoch.ToString(CultureInfo.InvariantCulture),
                    "capture_baseline=" + transaction.CaptureBaselineSequence.ToString(CultureInfo.InvariantCulture),
                    "custom_baseline=" + transaction.CustomBaselineSequence.ToString(CultureInfo.InvariantCulture),
                    "created_utc_ticks=" + transaction.CreatedUtcTicks.ToString(CultureInfo.InvariantCulture),
                    string.Empty
                });
        }

        /// <summary>
        /// Recovers only direct, checksummed save intent stages. An intent is published
        /// when the current vanilla file has the exact expected content identity;
        /// mismatches remain detached until conservative stale cleanup.
        /// </summary>
        private bool TryRecoverPendingSaveIntentsLocked(
            out string errorMessage)
        {
            errorMessage = string.Empty;
            string transactionRoot = CorePaths.GetTransactionRootDirectory();
            if (!Directory.Exists(transactionRoot))
            {
                return true;
            }

            string normalizedTransactionRoot;
            if (!CorePaths.TryValidateContainedMutationPath(
                transactionRoot,
                false,
                out normalizedTransactionRoot,
                out errorMessage))
            {
                return false;
            }

            string[] stageDirectories;
            try
            {
                stageDirectories = Directory.GetDirectories(
                    normalizedTransactionRoot,
                    "save_*",
                    SearchOption.TopDirectoryOnly);
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }

            Dictionary<string, CorePendingSaveTransaction> winnerByVanillaPath =
                new Dictionary<string, CorePendingSaveTransaction>(
                    StringComparer.OrdinalIgnoreCase);
            List<CorePendingSaveTransaction> exactCandidates =
                new List<CorePendingSaveTransaction>();
            long nowTicks = DateTime.UtcNow.Ticks;
            for (int stageIndex = 0;
                stageIndex < stageDirectories.Length;
                stageIndex++)
            {
                string normalizedStageDirectory;
                if (!CorePaths.TryValidateContainedMutationPath(
                    stageDirectories[stageIndex],
                    true,
                    out normalizedStageDirectory,
                    out errorMessage))
                {
                    return false;
                }

                if (!string.Equals(
                    Path.GetDirectoryName(normalizedStageDirectory),
                    normalizedTransactionRoot,
                    StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage =
                        "A save intent is not a direct transaction-root child.";
                    return false;
                }

                string stageName = Path.GetFileName(
                    normalizedStageDirectory);
                string stageToken = stageName.StartsWith(
                    "save_",
                    StringComparison.Ordinal)
                        ? stageName.Substring("save_".Length)
                        : string.Empty;
                string manifestPath = Path.Combine(
                    normalizedStageDirectory,
                    SaveTransactionManifestFileName);
                string temporaryManifestPath = manifestPath + "." +
                    stageToken +
                    ".tmp";
                if (stageToken.Length != 32 ||
                    (!File.Exists(manifestPath) &&
                     !File.Exists(temporaryManifestPath)))
                {
                    // No durable intent was committed, so this cannot authorize a
                    // publish. It is safe to remove after full containment/tree checks.
                    TryCleanupStagingSaveDirectory(
                        normalizedStageDirectory);
                    continue;
                }

                CorePendingSaveTransaction transaction;
                string manifestReadError;
                if (!TryReadSaveIntentManifest(
                    normalizedStageDirectory,
                    true,
                    out transaction,
                    out manifestReadError))
                {
                    DateTime stageLastWriteUtc;
                    try
                    {
                        stageLastWriteUtc = Directory.GetLastWriteTimeUtc(
                            normalizedStageDirectory);
                    }
                    catch
                    {
                        stageLastWriteUtc = DateTime.UtcNow;
                    }

                    if (DateTime.UtcNow - stageLastWriteUtc >=
                        TimeSpan.FromMinutes(30.0))
                    {
                        TryCleanupStagingSaveDirectory(
                            normalizedStageDirectory);
                    }

                    // Quarantine in place while recent. One malformed intent must not
                    // starve recovery of independent valid saves.
                    continue;
                }

                if (transaction.Sequence > saveTransactionSequence)
                {
                    saveTransactionSequence = transaction.Sequence;
                }

                CoreFileFingerprint vanillaFingerprint;
                string ignoredFingerprintError;
                bool exactPayload =
                    CoreFileFingerprintUtility.TryReadStable(
                        transaction.VanillaSaveFilePath,
                        out vanillaFingerprint,
                        out ignoredFingerprintError) &&
                    vanillaFingerprint.Length == transaction.ExpectedLength &&
                    string.Equals(
                        vanillaFingerprint.Sha256,
                        transaction.ExpectedSha256,
                        StringComparison.Ordinal);
                bool writeTransitionObserved = exactPayload &&
                    (!transaction.BaselineExisted ||
                     vanillaFingerprint.Length != transaction.BaselineLength ||
                     vanillaFingerprint.LastWriteUtcTicks !=
                        transaction.BaselineWriteUtcTicks ||
                     !string.Equals(
                        vanillaFingerprint.Sha256,
                        transaction.BaselineSha256,
                        StringComparison.Ordinal));
                if (!exactPayload || !writeTransitionObserved)
                {
                    if (transaction.CreatedUtcTicks > 0L &&
                        nowTicks - transaction.CreatedUtcTicks >=
                            StaleSaveIntentAgeTicks)
                    {
                        TryCleanupStagingSaveDirectory(
                            transaction.StagingSaveDirectory);
                    }

                    continue;
                }

                ICoreStorageEngine candidateEngine;
                string candidateValidationError;
                if (!TryCreateAndInitializeStorageEngine(
                        CorePaths.GetDatabasePath(
                            transaction.StagingSaveScope),
                        CorePaths.GetFlatFileDatabasePath(
                            transaction.StagingSaveScope),
                        out candidateEngine,
                        out candidateValidationError))
                {
                    continue;
                }

                bool generationFound;
                bool candidateValid =
                    candidateEngine.TryRollbackToSaveGeneration(
                        transaction.TargetSaveKey,
                        CoreFileFingerprintUtility.BuildContentIdentity(
                            transaction.ExpectedLength,
                            transaction.ExpectedSha256),
                        out generationFound,
                        out candidateValidationError) &&
                    generationFound;
                if (candidateValid)
                {
                    candidateValid = candidateEngine.TryValidateIntegrity(
                        out candidateValidationError);
                }

                candidateEngine.Dispose();
                if (!candidateValid)
                {
                    continue;
                }

                transaction.ObservedFingerprint = vanillaFingerprint;
                transaction.ObservationCompleted = true;
                transaction.VanillaWriteMatched = true;
                exactCandidates.Add(transaction);
                CorePendingSaveTransaction currentWinner;
                if (!winnerByVanillaPath.TryGetValue(
                        transaction.VanillaRelativeSavePath,
                        out currentWinner) ||
                    transaction.Sequence > currentWinner.Sequence)
                {
                    winnerByVanillaPath[
                        transaction.VanillaRelativeSavePath] = transaction;
                }
            }

            foreach (KeyValuePair<string, CorePendingSaveTransaction> winnerPair
                in winnerByVanillaPath)
            {
                CorePendingSaveTransaction winner = winnerPair.Value;
                bool canonicalAlreadyMatches;
                string canonicalValidationError;
                if (TryCanonicalSidecarMatchesSaveIntentLocked(
                    winner,
                    out canonicalAlreadyMatches,
                    out canonicalValidationError) &&
                    canonicalAlreadyMatches)
                {
                    CleanupRecoveredExactCandidatesForTarget(
                        exactCandidates,
                        winner);
                    string ignoredCanonicalManifestCleanup;
                    CorePaths.TryDeleteContainedFile(
                        Path.Combine(
                            winner.TargetSaveDirectory,
                            SaveTransactionManifestFileName),
                        out ignoredCanonicalManifestCleanup);
                    continue;
                }

                ICoreStorageEngine validationEngine;
                if (!TryCreateAndInitializeStorageEngine(
                    CorePaths.GetDatabasePath(winner.StagingSaveScope),
                    CorePaths.GetFlatFileDatabasePath(winner.StagingSaveScope),
                    out validationEngine,
                    out errorMessage))
                {
                    return false;
                }

                bool valid = validationEngine.TryValidateIntegrity(
                    out errorMessage);
                validationEngine.Dispose();
                if (!valid)
                {
                    return false;
                }

                if (!CorePaths.TryPublishStagingDirectory(
                    winner.StagingSaveDirectory,
                    winner.TargetSaveScope,
                    winner.Token,
                    CoreFileFingerprintUtility.BuildContentIdentity(
                        winner.ExpectedLength,
                        winner.ExpectedSha256),
                    out errorMessage))
                {
                    return false;
                }

                string ignoredCleanupError;
                CorePaths.TryDeleteContainedFile(
                    Path.Combine(
                        winner.TargetSaveDirectory,
                        SaveTransactionManifestFileName),
                    out ignoredCleanupError);
            }

            for (int candidateIndex = 0;
                candidateIndex < exactCandidates.Count;
                candidateIndex++)
            {
                CorePendingSaveTransaction candidate =
                    exactCandidates[candidateIndex];
                CorePendingSaveTransaction winner =
                    winnerByVanillaPath[
                        candidate.VanillaRelativeSavePath];
                if (!ReferenceEquals(candidate, winner) &&
                    Directory.Exists(candidate.StagingSaveDirectory))
                {
                    TryCleanupStagingSaveDirectory(
                        candidate.StagingSaveDirectory);
                }
            }

            return true;
        }

        private static bool TryCleanupOrphanedLoadStagesLocked(
            out string errorMessage)
        {
            errorMessage = string.Empty;
            string transactionRoot = CorePaths.GetTransactionRootDirectory();
            if (!Directory.Exists(transactionRoot))
            {
                return true;
            }

            string normalizedTransactionRoot;
            if (!CorePaths.TryValidateContainedMutationPath(
                transactionRoot,
                false,
                out normalizedTransactionRoot,
                out errorMessage))
            {
                return false;
            }

            try
            {
                string[] loadDirectories = Directory.GetDirectories(
                    normalizedTransactionRoot,
                    "load_*",
                    SearchOption.TopDirectoryOnly);
                for (int loadIndex = 0;
                    loadIndex < loadDirectories.Length;
                    loadIndex++)
                {
                    string loadDirectory = loadDirectories[loadIndex];
                    string directoryName = Path.GetFileName(loadDirectory);
                    if (!CorePaths.IsValidTransactionDirectoryName(
                        directoryName) ||
                        !string.Equals(
                            Path.GetDirectoryName(
                                Path.GetFullPath(loadDirectory)),
                            normalizedTransactionRoot,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string cleanupError;
                    if (!CorePaths.TryDeleteStagingSaveDirectory(
                        loadDirectory,
                        out cleanupError))
                    {
                        errorMessage = cleanupError;
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

        private bool TryCanonicalSidecarMatchesSaveIntentLocked(
            CorePendingSaveTransaction transaction,
            out bool matches,
            out string errorMessage)
        {
            matches = false;
            errorMessage = string.Empty;
            if (!StorageDirectoryHasData(
                transaction.TargetSaveDirectory))
            {
                return true;
            }

            ICoreStorageEngine canonicalEngine;
            if (!TryCreateAndInitializeStorageEngine(
                CorePaths.GetDatabasePath(transaction.TargetSaveScope),
                CorePaths.GetFlatFileDatabasePath(transaction.TargetSaveScope),
                out canonicalEngine,
                out errorMessage))
            {
                // A corrupt canonical must not block a valid staged candidate.
                errorMessage = string.Empty;
                return true;
            }

            bool generationFound;
            string validationError;
            bool rolledBack = canonicalEngine.TryRollbackToSaveGeneration(
                transaction.TargetSaveKey,
                CoreFileFingerprintUtility.BuildContentIdentity(
                    transaction.ExpectedLength,
                    transaction.ExpectedSha256),
                out generationFound,
                out validationError);
            bool integrityValid = rolledBack &&
                generationFound &&
                canonicalEngine.TryValidateIntegrity(
                    out validationError);
            canonicalEngine.Dispose();
            matches = integrityValid;
            return true;
        }

        private static void CleanupRecoveredExactCandidatesForTarget(
            List<CorePendingSaveTransaction> exactCandidates,
            CorePendingSaveTransaction winner)
        {
            for (int candidateIndex = 0;
                candidateIndex < exactCandidates.Count;
                candidateIndex++)
            {
                CorePendingSaveTransaction candidate =
                    exactCandidates[candidateIndex];
                if (candidate != null &&
                    string.Equals(
                        candidate.VanillaRelativeSavePath,
                        winner.VanillaRelativeSavePath,
                        StringComparison.OrdinalIgnoreCase) &&
                    Directory.Exists(candidate.StagingSaveDirectory))
                {
                    TryCleanupStagingSaveDirectory(
                        candidate.StagingSaveDirectory);
                }
            }
        }

        private bool TryReadSaveIntentManifest(
            string manifestContainerDirectory,
            bool requireStageDirectoryMatch,
            out CorePendingSaveTransaction transaction,
            out string errorMessage)
        {
            transaction = null;
            errorMessage = string.Empty;
            string manifestPath = Path.Combine(
                manifestContainerDirectory,
                SaveTransactionManifestFileName);
            string normalizedManifestPath;
            if (!CorePaths.TryValidateContainedMutationPath(
                manifestPath,
                false,
                out normalizedManifestPath,
                out errorMessage))
            {
                return false;
            }

            string containerName = Path.GetFileName(
                manifestContainerDirectory.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar));
            string tokenFromContainer =
                containerName.StartsWith("save_", StringComparison.Ordinal)
                    ? containerName.Substring("save_".Length)
                    : string.Empty;
            if (!File.Exists(normalizedManifestPath) &&
                tokenFromContainer.Length == 32)
            {
                string temporaryManifestPath = normalizedManifestPath + "." +
                    tokenFromContainer +
                    ".tmp";
                string normalizedTemporaryManifestPath;
                if (CorePaths.TryValidateContainedMutationPath(
                        temporaryManifestPath,
                        false,
                        out normalizedTemporaryManifestPath,
                        out errorMessage) &&
                    File.Exists(normalizedTemporaryManifestPath))
                {
                    File.Move(
                        normalizedTemporaryManifestPath,
                        normalizedManifestPath);
                }
            }

            if (!File.Exists(normalizedManifestPath))
            {
                errorMessage = "The durable save intent manifest is missing.";
                return false;
            }

            Dictionary<string, string> fields =
                new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                string[] lines = File.ReadAllLines(
                    normalizedManifestPath,
                    new UTF8Encoding(false, true));
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
                        !IsKnownSaveIntentField(key))
                    {
                        errorMessage =
                            "The save intent contains a duplicate or unknown field.";
                        return false;
                    }

                    fields.Add(key, value);
                }
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }

            string version;
            string token;
            string vanilla;
            string stageName;
            string sourceKey;
            string targetKey;
            string expectedLengthText;
            string expectedSha256;
            string baselineLengthText;
            string baselineExistedText;
            string baselineWriteTicksText;
            string baselineSha256;
            string sequenceText;
            string runtimeEpochText;
            string captureBaselineText;
            string customBaselineText;
            string createdTicksText;
            string checksum;
            if (fields.Count != 18 ||
                !fields.TryGetValue("version", out version) ||
                !fields.TryGetValue("token", out token) ||
                !fields.TryGetValue("vanilla", out vanilla) ||
                !fields.TryGetValue("stage", out stageName) ||
                !fields.TryGetValue("source_key", out sourceKey) ||
                !fields.TryGetValue("target_key", out targetKey) ||
                !fields.TryGetValue("expected_length", out expectedLengthText) ||
                !fields.TryGetValue("expected_sha256", out expectedSha256) ||
                !fields.TryGetValue("baseline_length", out baselineLengthText) ||
                !fields.TryGetValue("baseline_existed", out baselineExistedText) ||
                !fields.TryGetValue("baseline_write_ticks", out baselineWriteTicksText) ||
                !fields.TryGetValue("baseline_sha256", out baselineSha256) ||
                !fields.TryGetValue("sequence", out sequenceText) ||
                !fields.TryGetValue("runtime_epoch", out runtimeEpochText) ||
                !fields.TryGetValue("capture_baseline", out captureBaselineText) ||
                !fields.TryGetValue("custom_baseline", out customBaselineText) ||
                !fields.TryGetValue("created_utc_ticks", out createdTicksText) ||
                !fields.TryGetValue("checksum", out checksum))
            {
                errorMessage = "The save intent manifest is incomplete.";
                return false;
            }

            long expectedLength;
            long baselineLength;
            long baselineWriteTicks;
            long sequence;
            long sourceRuntimeEpoch;
            long captureBaseline;
            long customBaseline;
            long createdTicks;
            if (version != "1" ||
                !IsLowerHexToken(token, 32) ||
                stageName != "save_" + token ||
                (requireStageDirectoryMatch && stageName != containerName) ||
                !IsLowerHexToken(expectedSha256, 64) ||
                (baselineSha256 != "-" &&
                 !IsLowerHexToken(baselineSha256, 64)) ||
                (baselineExistedText != "0" &&
                 baselineExistedText != "1") ||
                !long.TryParse(expectedLengthText, NumberStyles.None, CultureInfo.InvariantCulture, out expectedLength) ||
                !long.TryParse(baselineLengthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out baselineLength) ||
                !long.TryParse(baselineWriteTicksText, NumberStyles.None, CultureInfo.InvariantCulture, out baselineWriteTicks) ||
                !long.TryParse(sequenceText, NumberStyles.None, CultureInfo.InvariantCulture, out sequence) ||
                !long.TryParse(runtimeEpochText, NumberStyles.None, CultureInfo.InvariantCulture, out sourceRuntimeEpoch) ||
                !long.TryParse(captureBaselineText, NumberStyles.None, CultureInfo.InvariantCulture, out captureBaseline) ||
                !long.TryParse(customBaselineText, NumberStyles.None, CultureInfo.InvariantCulture, out customBaseline) ||
                !long.TryParse(createdTicksText, NumberStyles.None, CultureInfo.InvariantCulture, out createdTicks) ||
                expectedLength < 0L ||
                sequence <= 0L ||
                sequence == long.MaxValue ||
                sourceRuntimeEpoch <= 0L ||
                sourceRuntimeEpoch == long.MaxValue ||
                captureBaseline < 0L ||
                customBaseline < 0L ||
                createdTicks <= 0L ||
                createdTicks > DateTime.UtcNow.AddMinutes(5.0).Ticks ||
                string.IsNullOrEmpty(sourceKey) ||
                !string.Equals(
                    CoreTokenUtility.SanitizeToken(
                        sourceKey,
                        CoreConstants.SaveKeyMaximumLength),
                    sourceKey,
                    StringComparison.Ordinal) ||
                ((baselineExistedText == "1") &&
                    (baselineLength < 0L ||
                     baselineWriteTicks <= 0L ||
                     baselineSha256 == "-")) ||
                ((baselineExistedText == "0") &&
                    (baselineLength != -1L ||
                     baselineWriteTicks != 0L ||
                     baselineSha256 != "-")))
            {
                errorMessage = "The save intent manifest values are invalid.";
                return false;
            }

            string vanillaRelativePath = vanilla.Replace(
                '/',
                Path.DirectorySeparatorChar);
            string vanillaSaveFilePath;
            CoreSaveScope targetSaveScope;
            if (!CorePaths.TryResolveVanillaSaveRelativePath(
                    vanillaRelativePath,
                    out vanillaSaveFilePath,
                    out targetSaveScope) ||
                !string.Equals(
                    NormalizeSaveKey(targetSaveScope.InternalSaveKey),
                    targetKey,
                    StringComparison.Ordinal))
            {
                errorMessage =
                    "The save intent does not map to its canonical target scope.";
                return false;
            }

            CoreSaveScope stagingSaveScope = new CoreSaveScope
            {
                SaveFilePath = targetSaveScope.SaveFilePath,
                StorageRelativeDirectory = Path.Combine(
                    "_transactions",
                    stageName),
                InternalSaveKey = targetSaveScope.InternalSaveKey,
                LegacyOwnerSaveKey = targetSaveScope.LegacyOwnerSaveKey,
                IsStaging = true
            };
            transaction = new CorePendingSaveTransaction
            {
                Token = token,
                Sequence = sequence,
                SourceRuntimeEpoch = sourceRuntimeEpoch,
                SourceSaveKey = sourceKey,
                TargetSaveScope = targetSaveScope,
                TargetSaveKey = targetKey,
                TargetSaveDirectory = CorePaths.GetSaveDirectory(targetSaveScope),
                StagingSaveScope = stagingSaveScope,
                StagingSaveDirectory = CorePaths.GetSaveDirectory(stagingSaveScope),
                VanillaRelativeSavePath = vanillaRelativePath,
                VanillaSaveFilePath = vanillaSaveFilePath,
                ExpectedLength = expectedLength,
                ExpectedSha256 = expectedSha256,
                BaselineLength = baselineLength,
                BaselineExisted = baselineExistedText == "1",
                BaselineWriteUtcTicks = baselineWriteTicks,
                BaselineSha256 = baselineSha256 == "-"
                    ? string.Empty
                    : baselineSha256,
                CaptureBaselineSequence = captureBaseline,
                CustomBaselineSequence = customBaseline,
                CreatedUtcTicks = createdTicks
            };

            string canonicalPayload = BuildSaveIntentManifestPayload(transaction);
            string expectedChecksum =
                CoreFileFingerprintUtility.ComputeSha256(
                    new UTF8Encoding(false).GetBytes(canonicalPayload));
            if (string.IsNullOrEmpty(canonicalPayload) ||
                !string.Equals(
                    checksum,
                    expectedChecksum,
                    StringComparison.Ordinal))
            {
                transaction = null;
                errorMessage = "The save intent checksum is invalid.";
                return false;
            }

            return true;
        }

        private static bool IsKnownSaveIntentField(string key)
        {
            return key == "version" ||
                key == "token" ||
                key == "vanilla" ||
                key == "stage" ||
                key == "source_key" ||
                key == "target_key" ||
                key == "expected_length" ||
                key == "expected_sha256" ||
                key == "baseline_length" ||
                key == "baseline_existed" ||
                key == "baseline_write_ticks" ||
                key == "baseline_sha256" ||
                key == "sequence" ||
                key == "runtime_epoch" ||
                key == "capture_baseline" ||
                key == "custom_baseline" ||
                key == "created_utc_ticks" ||
                key == "checksum";
        }

        private static bool IsLowerHexToken(
            string value,
            int expectedLength)
        {
            if (string.IsNullOrEmpty(value) ||
                value.Length != expectedLength)
            {
                return false;
            }

            for (int characterIndex = 0;
                characterIndex < value.Length;
                characterIndex++)
            {
                char character = value[characterIndex];
                if (!((character >= '0' && character <= '9') ||
                    (character >= 'a' && character <= 'f')))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryWriteLoadIntentMarker(
            string stagingDirectory,
            string token,
            string vanillaRelativeSavePath,
            string contentIdentity,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            string markerPayload = string.Join(
                "\n",
                new string[]
                {
                    "version=1",
                    "token=" + token,
                    "vanilla=" + vanillaRelativeSavePath.Replace('\\', '/'),
                    "content=" + (contentIdentity ?? string.Empty),
                    string.Empty
                });
            string markerText = markerPayload +
                "checksum=" +
                CoreFileFingerprintUtility.ComputeSha256(
                    new UTF8Encoding(false).GetBytes(markerPayload)) +
                "\n";
            string markerPath = Path.Combine(
                stagingDirectory,
                "load.intent");
            string temporaryMarkerPath = markerPath + "." + token + ".tmp";
            string normalizedMarkerPath;
            string normalizedTemporaryMarkerPath;
            if (!CorePaths.TryValidateContainedMutationPath(
                    markerPath,
                    false,
                    out normalizedMarkerPath,
                    out errorMessage) ||
                !CorePaths.TryValidateContainedMutationPath(
                    temporaryMarkerPath,
                    false,
                    out normalizedTemporaryMarkerPath,
                    out errorMessage))
            {
                return false;
            }

            if (File.Exists(normalizedMarkerPath))
            {
                return TryValidateLoadIntentMarker(
                    stagingDirectory,
                    token,
                    vanillaRelativeSavePath,
                    contentIdentity,
                    out errorMessage);
            }

            if (File.Exists(normalizedTemporaryMarkerPath))
            {
                try
                {
                    if (!TryValidateLoadIntentMarkerFile(
                        normalizedTemporaryMarkerPath,
                        token,
                        vanillaRelativeSavePath,
                        contentIdentity,
                        out errorMessage))
                    {
                        return false;
                    }

                    File.Move(
                        normalizedTemporaryMarkerPath,
                        normalizedMarkerPath);
                    return true;
                }
                catch (Exception exception)
                {
                    errorMessage = exception.Message;
                    return false;
                }
            }

            try
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(markerText);
                using (FileStream stream = new FileStream(
                    normalizedTemporaryMarkerPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                File.Move(
                    normalizedTemporaryMarkerPath,
                    normalizedMarkerPath);
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        private static bool TryValidateLoadIntentMarker(
            string containerDirectory,
            string expectedToken,
            string expectedVanillaRelativeSavePath,
            string expectedContentIdentity,
            out string errorMessage)
        {
            return TryValidateLoadIntentMarkerFile(
                Path.Combine(containerDirectory, "load.intent"),
                expectedToken,
                expectedVanillaRelativeSavePath,
                expectedContentIdentity,
                out errorMessage);
        }

        private static bool TryValidateLoadIntentMarkerFile(
            string markerPath,
            string expectedToken,
            string expectedVanillaRelativeSavePath,
            string expectedContentIdentity,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                string normalizedMarkerPath;
                if (!CorePaths.TryValidateContainedMutationPath(
                    markerPath,
                    false,
                    out normalizedMarkerPath,
                    out errorMessage) ||
                    !File.Exists(normalizedMarkerPath))
                {
                    errorMessage = "The durable load intent marker is missing.";
                    return false;
                }

                Dictionary<string, string> fields =
                    new Dictionary<string, string>(StringComparer.Ordinal);
                string[] lines = File.ReadAllLines(
                    normalizedMarkerPath,
                    new UTF8Encoding(false, true));
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
                         key != "content" &&
                         key != "checksum"))
                    {
                        errorMessage = "The load intent marker is malformed.";
                        return false;
                    }

                    fields.Add(key, value);
                }

                string version;
                string token;
                string vanilla;
                string content;
                string checksum;
                if (fields.Count != 5 ||
                    !fields.TryGetValue("version", out version) ||
                    !fields.TryGetValue("token", out token) ||
                    !fields.TryGetValue("vanilla", out vanilla) ||
                    !fields.TryGetValue("content", out content) ||
                    !fields.TryGetValue("checksum", out checksum))
                {
                    errorMessage = "The load intent marker is incomplete.";
                    return false;
                }

                string payload = string.Join(
                    "\n",
                    new string[]
                    {
                        "version=1",
                        "token=" + token,
                        "vanilla=" + vanilla,
                        "content=" + content,
                        string.Empty
                    });
                if (version != "1" ||
                    !string.Equals(token, expectedToken, StringComparison.Ordinal) ||
                    !string.Equals(
                        vanilla.Replace('/', Path.DirectorySeparatorChar),
                        expectedVanillaRelativeSavePath,
                        StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(
                        content,
                        expectedContentIdentity ?? string.Empty,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        checksum,
                        CoreFileFingerprintUtility.ComputeSha256(
                            new UTF8Encoding(false).GetBytes(payload)),
                        StringComparison.Ordinal))
                {
                    errorMessage = "The load intent marker identity is invalid.";
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
    }
}
