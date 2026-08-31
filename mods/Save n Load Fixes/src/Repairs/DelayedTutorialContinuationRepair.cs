using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using SaveNLoadFixes.Persistence;
using SaveNLoadFixes.Safety;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A14 / regression 34: preserve the two source-proven delayed tutorial
    /// continuations without serializing iterator state.
    ///
    /// Vanilla Tutorial_Actions._Coroutine(string) owns exactly two semantic
    /// continuations: manager_after_conversation -> 8_hire_a_manager and
    /// activities_after_hire -> 5_activities. A08 already guards both generated
    /// iterators with LoadEpoch. A14 records only that semantic kind plus the
    /// optional scaled-time remainder until the next activities dialogue check,
    /// then recreates the canonical vanilla _Coroutine(kind) after target variables
    /// are loaded. Target-save variable state is the idempotency authority.
    /// </summary>
    internal static class DelayedTutorialContinuationRepair
    {
        internal const int SectionVersion = 1;
        internal const string ManagerKind = "manager_after_conversation";
        internal const string ActivitiesKind = "activities_after_hire";
        internal const string ManagerTargetTutorialId = "8_hire_a_manager";
        internal const string ActivitiesTargetTutorialId = "5_activities";
        internal const float VanillaActivitiesDelaySeconds = 5f;
        internal const float MaxPersistedActivitiesDelaySeconds = 10f;

        internal sealed class DispatchScope
        {
            internal string PreviousKind;
            internal bool Ended;
        }

        private sealed class ActiveContinuation
        {
            internal long Token;
            internal long Epoch;
            internal string Kind;
            internal string TargetTutorialId;
            internal object Iterator;
            internal bool IsAuthoritative;

            // Activities-only timing. AdditionalMandatoryDelay is five seconds while
            // vanilla is in the first of its two source-proven fixed waits; later loop
            // waits carry no mandatory follower.
            internal bool WaitActive;
            internal float DueScaledTime;
            internal float AdditionalMandatoryDelay;

            // Restore folds a persisted total remainder into the first wait, then makes
            // the immediately-following mandatory wait zero. This preserves the next
            // dialogue-check frontier without adding a durable phase field.
            internal bool RestoreHasRemaining;
            internal float RestoreRemaining;
            internal bool RestoreFirstWaitAdjusted;
            internal bool RestoreFollowupZeroed;
        }

        private sealed class RestoreClaim
        {
            internal string Kind;
            internal bool HasRemaining;
            internal float Remaining;
            internal bool Claimed;
            internal ActiveContinuation Continuation;
        }

        private enum SavedTutorialState
        {
            Locked = 0,
            AvailableOrDone = 1
        }

        private static readonly object Sync = new object();
        private static readonly ConditionalWeakTable<object, ActiveContinuation> ByIterator =
            new ConditionalWeakTable<object, ActiveContinuation>();
        private static readonly Dictionary<string, ActiveContinuation> ActiveByKind =
            new Dictionary<string, ActiveContinuation>(StringComparer.Ordinal);

        [ThreadStatic]
        private static string dispatchKind;

        [ThreadStatic]
        private static RestoreClaim restoreClaim;

        private static long nextToken;
        private static long registeredCount;
        private static long duplicateFactoryCount;
        private static long completedCount;
        private static long faultRetiredCount;
        private static long capturedCheckpointCount;
        private static long capturedContinuationCount;
        private static long captureFailureCount;
        private static long staleRegistryClearCount;
        private static long restoredLoadCount;
        private static long restoredContinuationCount;
        private static long idempotentDiscardCount;
        private static long legacySectionAbsentCount;
        private static long invalidSectionCount;
        private static long associationMissingCount;
        private static long schedulingFailureCount;
        private static long rollbackCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get
            {
                return DelayedTutorialContinuationPatchHealth.IsHealthy &&
                    DeferredCarrierEpochRepair.IsImplemented &&
                    ScaledTimeClock.IsImplemented;
            }
        }

        internal static long RegisteredCount { get { return Interlocked.Read(ref registeredCount); } }
        internal static long DuplicateFactoryCount { get { return Interlocked.Read(ref duplicateFactoryCount); } }
        internal static long CompletedCount { get { return Interlocked.Read(ref completedCount); } }
        internal static long FaultRetiredCount { get { return Interlocked.Read(ref faultRetiredCount); } }
        internal static long CapturedCheckpointCount { get { return Interlocked.Read(ref capturedCheckpointCount); } }
        internal static long CapturedContinuationCount { get { return Interlocked.Read(ref capturedContinuationCount); } }
        internal static long CaptureFailureCount { get { return Interlocked.Read(ref captureFailureCount); } }
        internal static long StaleRegistryClearCount { get { return Interlocked.Read(ref staleRegistryClearCount); } }
        internal static long RestoredLoadCount { get { return Interlocked.Read(ref restoredLoadCount); } }
        internal static long RestoredContinuationCount { get { return Interlocked.Read(ref restoredContinuationCount); } }
        internal static long IdempotentDiscardCount { get { return Interlocked.Read(ref idempotentDiscardCount); } }
        internal static long LegacySectionAbsentCount { get { return Interlocked.Read(ref legacySectionAbsentCount); } }
        internal static long InvalidSectionCount { get { return Interlocked.Read(ref invalidSectionCount); } }
        internal static long AssociationMissingCount { get { return Interlocked.Read(ref associationMissingCount); } }
        internal static long SchedulingFailureCount { get { return Interlocked.Read(ref schedulingFailureCount); } }
        internal static long RollbackCount { get { return Interlocked.Read(ref rollbackCount); } }

        internal static string LastDiagnostic
        {
            get
            {
                lock (Sync)
                {
                    return lastDiagnostic;
                }
            }
        }

        /// <summary>
        /// Keeps semantic registration bounded to Tutorial_Actions._Coroutine(ID).
        /// Direct reflective calls to the private iterator factories therefore do not
        /// become repair-owned tutorial continuations merely because A08 can guard them.
        /// </summary>
        internal static DispatchScope BeginDispatch(string id)
        {
            DispatchScope scope = new DispatchScope
            {
                PreviousKind = dispatchKind
            };
            dispatchKind = NormalizeKind(id);
            return scope;
        }

        internal static void EndDispatch(DispatchScope scope)
        {
            if (scope == null || scope.Ended)
            {
                return;
            }
            scope.Ended = true;
            dispatchKind = scope.PreviousKind;
        }

        /// <summary>
        /// Called after one of the two exact private factories returns its generated
        /// iterator while the canonical _Coroutine(ID) dispatcher is active.
        /// </summary>
        internal static void RegisterIterator(IEnumerator iterator, string kind)
        {
            kind = NormalizeKind(kind);
            if (iterator == null || kind == null || !string.Equals(dispatchKind, kind, StringComparison.Ordinal) ||
                !IsImplemented)
            {
                return;
            }

            string target = TargetForKind(kind);
            RestoreClaim claim = restoreClaim;
            bool isRestore = claim != null && !claim.Claimed &&
                string.Equals(claim.Kind, kind, StringComparison.Ordinal);

            ActiveContinuation continuation = new ActiveContinuation
            {
                Token = Interlocked.Increment(ref nextToken),
                Epoch = LoadEpoch.Capture(),
                Kind = kind,
                TargetTutorialId = target,
                Iterator = iterator,
                IsAuthoritative = true,
                RestoreHasRemaining = isRestore && claim.HasRemaining,
                RestoreRemaining = isRestore ? ClampPersistedDelay(claim.Remaining) : 0f
            };

            bool duplicate = false;
            lock (Sync)
            {
                ActiveContinuation existing;
                if (ActiveByKind.TryGetValue(kind, out existing) && existing != null &&
                    LoadEpoch.IsCurrent(existing.Epoch))
                {
                    // Vanilla authored data is source-proven to schedule one semantic
                    // occurrence at a time. Preserve the first as the durable owner if
                    // another mod directly duplicates the dispatcher call.
                    continuation.IsAuthoritative = false;
                    duplicate = true;
                }
                else
                {
                    ActiveByKind[kind] = continuation;
                }

                try
                {
                    ByIterator.Add(iterator, continuation);
                }
                catch (ArgumentException)
                {
                    continuation.IsAuthoritative = false;
                    duplicate = true;
                }

                lastDiagnostic =
                    "A14 registered delayed tutorial continuation '" + kind +
                    "' at load epoch " + continuation.Epoch.ToString(CultureInfo.InvariantCulture) + ".";
            }

            if (isRestore)
            {
                claim.Claimed = true;
                claim.Continuation = continuation;
            }

            Interlocked.Increment(ref registeredCount);
            if (duplicate)
            {
                Interlocked.Increment(ref duplicateFactoryCount);
            }
        }

        /// <summary>
        /// Called at the two exact WaitForSeconds(5f) construction sites in
        /// Activities_After_Hire. Normal carriers return vanilla's value unchanged.
        /// Restored carriers may fold one saved total remainder into the first fixed
        /// wait and zero the immediately-following fixed wait, avoiding a durable
        /// iterator-phase field while preserving the next dialogue-check frontier.
        /// </summary>
        internal static float ResolveActivitiesWaitSeconds(object iterator, float vanillaSeconds, int siteIndex)
        {
            ActiveContinuation continuation;
            if (iterator == null || !ByIterator.TryGetValue(iterator, out continuation) || continuation == null ||
                !string.Equals(continuation.Kind, ActivitiesKind, StringComparison.Ordinal))
            {
                return vanillaSeconds;
            }

            float now = ScaledTimeClock.Now;
            float delay = vanillaSeconds;
            float additional = siteIndex == 0 ? VanillaActivitiesDelaySeconds : 0f;

            lock (Sync)
            {
                if (continuation.RestoreHasRemaining && !continuation.RestoreFirstWaitAdjusted && siteIndex == 0)
                {
                    delay = ClampPersistedDelay(continuation.RestoreRemaining);
                    additional = 0f;
                    continuation.RestoreFirstWaitAdjusted = true;
                }
                else if (continuation.RestoreHasRemaining && continuation.RestoreFirstWaitAdjusted &&
                    !continuation.RestoreFollowupZeroed && siteIndex == 1)
                {
                    delay = 0f;
                    additional = 0f;
                    continuation.RestoreFollowupZeroed = true;
                    continuation.RestoreHasRemaining = false;
                }

                continuation.WaitActive = true;
                continuation.DueScaledTime = now + Math.Max(0f, delay);
                continuation.AdditionalMandatoryDelay = additional;
            }

            return Math.Max(0f, delay);
        }

        internal static void ObserveMoveNextReturn(object iterator, bool result)
        {
            if (result)
            {
                return;
            }

            ActiveContinuation continuation;
            if (!TryRetireIterator(iterator, out continuation))
            {
                return;
            }

            Interlocked.Increment(ref completedCount);
            SetDiagnostic(
                "A14 delayed tutorial continuation '" + continuation.Kind +
                "' reached terminal iterator completion.");
        }

        internal static void ObserveMoveNextFault(object iterator, Exception exception)
        {
            ActiveContinuation continuation;
            if (!TryRetireIterator(iterator, out continuation))
            {
                return;
            }

            Interlocked.Increment(ref faultRetiredCount);
            SetDiagnostic(
                "A14 delayed tutorial continuation '" + continuation.Kind +
                "' faulted and was retired: " +
                (exception == null ? "<unknown exception>" : exception.GetType().FullName) + ".");
        }

        internal static bool TryCaptureForEnvelope(
            SaveManager.SavedData dataToSave,
            out List<DelayedTutorialContinuationRecordV1> records,
            out string error)
        {
            records = new List<DelayedTutorialContinuationRecordV1>();
            error = string.Empty;

            if (!DelayedTutorialContinuationPatchHealth.IsHealthy)
            {
                string patchFailure = DelayedTutorialContinuationPatchHealth.Failure;
                return CaptureFailed(
                    "A14 is not healthy; delayed tutorial continuation state cannot be frozen exactly. " +
                    "Harmony health observed " +
                    DelayedTutorialContinuationPatchHealth.ResolvedTargetMethodCount.ToString(CultureInfo.InvariantCulture) +
                    "/" +
                    DelayedTutorialContinuationPatchHealth.ExpectedTargetMethodCount.ToString(CultureInfo.InvariantCulture) +
                    " logical targets and " +
                    DelayedTutorialContinuationPatchHealth.ObservedActivitiesWaitForSecondsSiteCount.ToString(CultureInfo.InvariantCulture) +
                    "/" +
                    DelayedTutorialContinuationPatchHealth.ExpectedActivitiesWaitForSecondsSiteCount.ToString(CultureInfo.InvariantCulture) +
                    " activities wait sites." +
                    (string.IsNullOrEmpty(patchFailure) ? string.Empty : " " + patchFailure),
                    out error);
            }
            if (!DeferredCarrierEpochRepair.IsImplemented)
            {
                return CaptureFailed(
                    "A14 is not healthy; the shared A08 deferred-carrier epoch guard is unavailable.",
                    out error);
            }
            if (!ScaledTimeClock.IsImplemented)
            {
                return CaptureFailed(
                    "A14 is not healthy; the shared Unity scaled-time clock is unavailable.",
                    out error);
            }
            if (dataToSave == null || dataToSave.variables__variables == null)
            {
                return CaptureFailed("A14 target SavedData variable list is unavailable during caller-thread freeze.", out error);
            }

            float now = ScaledTimeClock.Now;
            if (!TryCaptureKind(ManagerKind, dataToSave.variables__variables, now, records, out error) ||
                !TryCaptureKind(ActivitiesKind, dataToSave.variables__variables, now, records, out error))
            {
                return false;
            }

            Interlocked.Increment(ref capturedCheckpointCount);
            Interlocked.Add(ref capturedContinuationCount, records.Count);
            SetDiagnostic(
                "A14 captured " + records.Count.ToString(CultureInfo.InvariantCulture) +
                " delayed tutorial continuation(s) for the caller-thread SavedData request.");
            return true;
        }

        private static bool TryCaptureKind(
            string kind,
            List<variables._variable> savedVariables,
            float now,
            List<DelayedTutorialContinuationRecordV1> records,
            out string error)
        {
            error = string.Empty;
            ActiveContinuation snapshot = null;
            lock (Sync)
            {
                ActiveContinuation current;
                if (ActiveByKind.TryGetValue(kind, out current) && current != null)
                {
                    if (!LoadEpoch.IsCurrent(current.Epoch))
                    {
                        ActiveByKind.Remove(kind);
                        Interlocked.Increment(ref staleRegistryClearCount);
                    }
                    else if (current.IsAuthoritative)
                    {
                        snapshot = current;
                    }
                }
            }

            if (snapshot == null)
            {
                return true;
            }

            SavedTutorialState targetState;
            if (!TryReadSavedTutorialState(savedVariables, snapshot.TargetTutorialId, out targetState, out error))
            {
                return CaptureFailed(error, out error);
            }

            // If the target is already durable as available/done, a pending presentation
            // carrier is semantically obsolete and must not be written into a new save.
            if (targetState == SavedTutorialState.AvailableOrDone)
            {
                return true;
            }

            DelayedTutorialContinuationRecordV1 record = new DelayedTutorialContinuationRecordV1
            {
                kind = snapshot.Kind
            };

            if (string.Equals(snapshot.Kind, ActivitiesKind, StringComparison.Ordinal))
            {
                lock (Sync)
                {
                    if (snapshot.WaitActive)
                    {
                        float remaining = Math.Max(0f, snapshot.DueScaledTime - now) +
                            Math.Max(0f, snapshot.AdditionalMandatoryDelay);
                        record.has_remaining_scaled_delay = true;
                        record.remaining_scaled_seconds = ClampPersistedDelay(remaining);
                    }
                }
            }

            records.Add(record);
            return true;
        }

        /// <summary>
        /// variables.LoadFunction() is the exact source-owned adoption seam for the
        /// serialized tutorial status variables. Clear discarded A14 registry state
        /// before adoption; A08 independently prevents old generated carriers from
        /// running in the new load epoch.
        /// </summary>
        internal static void ClearBeforeTargetVariablesLoad()
        {
            int cleared;
            lock (Sync)
            {
                cleared = ActiveByKind.Count;
                ActiveByKind.Clear();
                lastDiagnostic = "A14 cleared discarded-timeline tutorial continuation registry before target variables load.";
            }
            if (cleared > 0)
            {
                Interlocked.Add(ref staleRegistryClearCount, cleared);
            }
        }

        internal static void RestoreAfterTargetVariablesLoad()
        {
            if (!IsImplemented)
            {
                SetDiagnostic("A14 restore is not healthy; no delayed tutorial continuation was reconstructed.");
                return;
            }

            SaveManager.SavedData target = GetTargetSavedData();
            if (target == null)
            {
                Interlocked.Increment(ref associationMissingCount);
                SetDiagnostic("A14 target SavedData is unavailable after variables reconstruction.");
                return;
            }

            RepairEnvelopeLoadState state;
            if (!RepairEnvelopeTransport.TryGetLoadState(target, out state))
            {
                Interlocked.Increment(ref associationMissingCount);
                SetDiagnostic("A14 target SavedData has no repair-envelope read association.");
                return;
            }

            if (!state.Present)
            {
                Interlocked.Increment(ref legacySectionAbsentCount);
                SetDiagnostic("A14 loaded a pre-envelope save; delayed tutorial continuation state was not fabricated.");
                return;
            }
            if (!state.Valid || state.Envelope == null || state.Envelope.records == null)
            {
                RecordInvalid("A14 loaded a present but invalid repair envelope; tutorial continuation state was not treated as empty.");
                return;
            }

            RepairEnvelopeRecordsV1 envelopeRecords = state.Envelope.records;
            if (envelopeRecords.delayed_tutorial_continuations_version == 0)
            {
                Interlocked.Increment(ref legacySectionAbsentCount);
                SetDiagnostic("A14 loaded a pre-A14 envelope; delayed tutorial continuation state was not fabricated.");
                return;
            }
            if (envelopeRecords.delayed_tutorial_continuations_version != SectionVersion ||
                envelopeRecords.delayed_tutorial_continuations == null || target.variables__variables == null)
            {
                RecordInvalid("A14 repair section is invalid or unsupported.");
                return;
            }

            List<DelayedTutorialContinuationRecordV1> records = envelopeRecords.delayed_tutorial_continuations;
            if (records.Count > 2)
            {
                RecordInvalid("A14 repair section contains more than the two source-proven continuation kinds.");
                return;
            }

            HashSet<string> uniqueKinds = new HashSet<string>(StringComparer.Ordinal);
            List<DelayedTutorialContinuationRecordV1> pending = new List<DelayedTutorialContinuationRecordV1>();
            for (int i = 0; i < records.Count; i++)
            {
                DelayedTutorialContinuationRecordV1 record = records[i];
                string kind = record == null ? null : NormalizeKind(record.kind);
                if (record == null || kind == null || !uniqueKinds.Add(kind))
                {
                    RecordInvalid("A14 repair section contains an unknown or duplicate continuation kind.");
                    return;
                }

                if (string.Equals(kind, ManagerKind, StringComparison.Ordinal))
                {
                    if (record.has_remaining_scaled_delay || record.remaining_scaled_seconds != 0f)
                    {
                        RecordInvalid("A14 manager_after_conversation record cannot carry a scaled delay.");
                        return;
                    }
                }
                else if (record.has_remaining_scaled_delay)
                {
                    if (!IsFiniteDelay(record.remaining_scaled_seconds) ||
                        record.remaining_scaled_seconds < 0f ||
                        record.remaining_scaled_seconds > MaxPersistedActivitiesDelaySeconds)
                    {
                        RecordInvalid("A14 activities_after_hire remaining delay is outside the source-proven 0..10 second domain.");
                        return;
                    }
                }
                else if (record.remaining_scaled_seconds != 0f)
                {
                    RecordInvalid("A14 delay value is nonzero while its optional-delay marker is false.");
                    return;
                }

                SavedTutorialState targetState;
                string stateError;
                if (!TryReadSavedTutorialState(
                        target.variables__variables,
                        TargetForKind(kind),
                        out targetState,
                        out stateError))
                {
                    RecordInvalid(stateError);
                    return;
                }

                if (targetState == SavedTutorialState.AvailableOrDone)
                {
                    Interlocked.Increment(ref idempotentDiscardCount);
                    continue;
                }

                pending.Add(record);
            }

            if (pending.Count == 0)
            {
                Interlocked.Increment(ref restoredLoadCount);
                SetDiagnostic(
                    records.Count == 0
                        ? "A14 restored an authoritative empty delayed-tutorial-continuation section."
                        : "A14 discarded all saved tutorial continuations because their target tutorials were already available/done in the target save.");
                return;
            }

            Tutorial_Actions actions = GetTutorialActions();
            if (actions == null)
            {
                Interlocked.Increment(ref schedulingFailureCount);
                SetDiagnostic("A14 could not resolve the loaded Tutorial_Actions component; no continuation was scheduled.");
                return;
            }

            List<ActiveContinuation> restored = new List<ActiveContinuation>();
            for (int i = 0; i < pending.Count; i++)
            {
                ActiveContinuation continuation;
                string scheduleError;
                if (!TryRestoreOne(actions, pending[i], out continuation, out scheduleError))
                {
                    RollBackRestored(restored);
                    Interlocked.Increment(ref schedulingFailureCount);
                    SetDiagnostic(scheduleError);
                    Debug.LogError(SaveNLoadFixesConstants.LogPrefix + scheduleError);
                    return;
                }
                restored.Add(continuation);
            }

            Interlocked.Increment(ref restoredLoadCount);
            Interlocked.Add(ref restoredContinuationCount, restored.Count);
            SetDiagnostic(
                "A14 restored " + restored.Count.ToString(CultureInfo.InvariantCulture) +
                " delayed tutorial continuation(s) through vanilla Tutorial_Actions._Coroutine(kind).");
        }

        private static bool TryRestoreOne(
            Tutorial_Actions actions,
            DelayedTutorialContinuationRecordV1 record,
            out ActiveContinuation continuation,
            out string error)
        {
            continuation = null;
            error = string.Empty;
            string kind = NormalizeKind(record.kind);
            RestoreClaim claim = new RestoreClaim
            {
                Kind = kind,
                HasRemaining = record.has_remaining_scaled_delay,
                Remaining = ClampPersistedDelay(record.remaining_scaled_seconds)
            };

            if (restoreClaim != null)
            {
                error = "A14 nested restore scheduling claim is not supported.";
                return false;
            }

            restoreClaim = claim;
            try
            {
                actions._Coroutine(kind);
            }
            catch (Exception exception)
            {
                if (claim.Continuation != null)
                {
                    DeferredCarrierEpochRepair.CancelCarrier(
                        claim.Continuation.Iterator,
                        "A14 cancelled a tutorial continuation whose vanilla restore scheduling threw.");
                    ActiveContinuation ignored;
                    TryRetireIterator(claim.Continuation.Iterator, out ignored);
                }
                error = "A14 failed while asking vanilla Tutorial_Actions._Coroutine('" + kind +
                    "') to restore the continuation: " + exception.Message;
                return false;
            }
            finally
            {
                restoreClaim = null;
            }

            if (!claim.Claimed || claim.Continuation == null || !claim.Continuation.IsAuthoritative)
            {
                if (claim.Continuation != null)
                {
                    DeferredCarrierEpochRepair.CancelCarrier(
                        claim.Continuation.Iterator,
                        "A14 cancelled an unclaimed/non-authoritative tutorial restore carrier.");
                    ActiveContinuation ignored;
                    TryRetireIterator(claim.Continuation.Iterator, out ignored);
                }
                error = "A14 vanilla restore scheduling did not claim exactly one authoritative iterator for '" + kind + "'.";
                return false;
            }

            continuation = claim.Continuation;
            return true;
        }

        private static void RollBackRestored(List<ActiveContinuation> restored)
        {
            if (restored == null)
            {
                return;
            }
            for (int i = 0; i < restored.Count; i++)
            {
                ActiveContinuation continuation = restored[i];
                if (continuation == null)
                {
                    continue;
                }
                DeferredCarrierEpochRepair.CancelCarrier(
                    continuation.Iterator,
                    "A14 rolled back a partially restored delayed-tutorial-continuation transaction.");
                ActiveContinuation ignored;
                TryRetireIterator(continuation.Iterator, out ignored);
                Interlocked.Increment(ref rollbackCount);
            }
        }

        private static bool TryRetireIterator(object iterator, out ActiveContinuation continuation)
        {
            continuation = null;
            if (iterator == null || !ByIterator.TryGetValue(iterator, out continuation) || continuation == null)
            {
                return false;
            }

            lock (Sync)
            {
                ByIterator.Remove(iterator);
                ActiveContinuation current;
                if (continuation.IsAuthoritative && ActiveByKind.TryGetValue(continuation.Kind, out current) &&
                    ReferenceEquals(current, continuation))
                {
                    ActiveByKind.Remove(continuation.Kind);
                }
            }
            return true;
        }

        private static bool TryReadSavedTutorialState(
            List<variables._variable> rows,
            string tutorialId,
            out SavedTutorialState state,
            out string error)
        {
            state = SavedTutorialState.Locked;
            error = string.Empty;
            if (rows == null || string.IsNullOrEmpty(tutorialId))
            {
                error = "A14 target tutorial-variable collection or tutorial ID is unavailable.";
                return false;
            }

            int matches = 0;
            string value = null;
            for (int i = 0; i < rows.Count; i++)
            {
                variables._variable row = rows[i];
                if (row != null && string.Equals(row.name, tutorialId, StringComparison.Ordinal))
                {
                    matches++;
                    value = row.value;
                }
            }

            if (matches > 1)
            {
                error = "A14 target SavedData contains duplicate tutorial variable '" + tutorialId + "'.";
                return false;
            }

            if (matches == 1 &&
                (string.Equals(value, "available", StringComparison.Ordinal) ||
                 string.Equals(value, "done", StringComparison.Ordinal)))
            {
                state = SavedTutorialState.AvailableOrDone;
            }
            return true;
        }

        private static Tutorial_Actions GetTutorialActions()
        {
            try
            {
                if (Camera.main == null)
                {
                    return null;
                }
                mainScript main = Camera.main.GetComponent<mainScript>();
                if (main == null || main.Data == null)
                {
                    return null;
                }
                return main.Data.GetComponent<Tutorial_Actions>();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static SaveManager.SavedData GetTargetSavedData()
        {
            try
            {
                mainScript main = Camera.main == null ? null : Camera.main.GetComponent<mainScript>();
                return main == null ? null : main.GetSavedData();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string NormalizeKind(string value)
        {
            if (string.Equals(value, ManagerKind, StringComparison.Ordinal))
            {
                return ManagerKind;
            }
            if (string.Equals(value, ActivitiesKind, StringComparison.Ordinal))
            {
                return ActivitiesKind;
            }
            return null;
        }

        private static string TargetForKind(string kind)
        {
            return string.Equals(kind, ManagerKind, StringComparison.Ordinal)
                ? ManagerTargetTutorialId
                : ActivitiesTargetTutorialId;
        }

        private static float ClampPersistedDelay(float value)
        {
            if (!IsFiniteDelay(value) || value <= 0f)
            {
                return 0f;
            }
            if (value > MaxPersistedActivitiesDelaySeconds)
            {
                return MaxPersistedActivitiesDelaySeconds;
            }
            return value;
        }

        private static bool IsFiniteDelay(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool CaptureFailed(string diagnostic, out string error)
        {
            error = diagnostic ?? "unknown A14 capture failure";
            Interlocked.Increment(ref captureFailureCount);
            SetDiagnostic(error);
            return false;
        }

        private static void RecordInvalid(string diagnostic)
        {
            Interlocked.Increment(ref invalidSectionCount);
            SetDiagnostic(diagnostic);
            Debug.LogError(SaveNLoadFixesConstants.LogPrefix + diagnostic);
        }

        private static void SetDiagnostic(string value)
        {
            lock (Sync)
            {
                lastDiagnostic = value ?? string.Empty;
            }
        }
    }

    internal static class DelayedTutorialContinuationPatchHealth
    {
        internal const int ExpectedTargetMethodCount = 6;
        internal const int ExpectedActivitiesWaitForSecondsSiteCount = 2;

        private static readonly object Sync = new object();
        private static readonly HashSet<string> ResolvedTargets = new HashSet<string>(StringComparer.Ordinal);
        private static int observedActivitiesWaitSites = -1;
        private static string failure = string.Empty;

        internal static int ResolvedTargetMethodCount
        {
            get { lock (Sync) { return ResolvedTargets.Count; } }
        }

        internal static int ObservedActivitiesWaitForSecondsSiteCount
        {
            get { lock (Sync) { return observedActivitiesWaitSites; } }
        }

        internal static string Failure
        {
            get { lock (Sync) { return failure; } }
        }

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return ResolvedTargets.Count == ExpectedTargetMethodCount &&
                        observedActivitiesWaitSites == ExpectedActivitiesWaitForSecondsSiteCount &&
                        string.IsNullOrEmpty(failure);
                }
            }
        }

        internal static void ReportTargetResolved(string target)
        {
            lock (Sync)
            {
                if (string.IsNullOrEmpty(target))
                {
                    failure = "A14 resolved an empty logical Harmony target.";
                    return;
                }

                // Harmony/HarmonyX may enumerate TargetMethods more than once while
                // recomposing a method after another patch is added. Rediscovery of
                // the same audited logical seam is idempotent; only the unique seam
                // set is part of the six-method health manifest.
                ResolvedTargets.Add(target);
                if (ResolvedTargets.Count > ExpectedTargetMethodCount)
                {
                    failure = "A14 resolved more Harmony targets than the frozen six-method manifest.";
                }
            }
        }

        internal static void ReportActivitiesWaitForSecondsSites(int count)
        {
            lock (Sync)
            {
                observedActivitiesWaitSites = count;
                if (count != ExpectedActivitiesWaitForSecondsSiteCount)
                {
                    failure = "A14 expected exactly two Activities_After_Hire WaitForSeconds(5f) construction sites but observed " +
                        count.ToString(CultureInfo.InvariantCulture) + ".";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown A14 patch failure";
            }
        }
    }
}
