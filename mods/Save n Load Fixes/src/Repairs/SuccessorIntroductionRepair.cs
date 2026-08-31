using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using SaveNLoadFixes.Persistence;
using SaveNLoadFixes.Safety;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A11 / regression 31: preserve the already-hired successor introduction flush.
    ///
    /// Date_Graduation.Find_Successor() durably hires the generated idol before it
    /// schedules a five-second _StartIntroductions carrier. Vanilla keeps the pending
    /// participant list only in data_girls.new_girls, so a restart loses the future
    /// introduction and an F9 can leave stale object references behind.
    ///
    /// This repair owns only that missing continuation token: ordered stable idol IDs.
    /// It never calls Find_Successor(), Hire(), GenerateGirl(), or consumes RNG. On a
    /// target load it clears stale new_girls references before roster reconstruction,
    /// resolves the saved IDs to the fresh loaded roster, and asks vanilla
    /// Date_Graduation.StartIntroductions() to create its normal five-second carrier.
    /// A08 remains the sole MoveNext suppression owner for discarded carriers.
    /// </summary>
    internal static class SuccessorIntroductionRepair
    {
        internal const int SectionVersion = 1;

        internal sealed class FlushState
        {
            internal long Token;
            internal bool HadPending;
            internal bool HadGirls;
        }

        private sealed class PendingIntroduction
        {
            internal long Token;
            internal long Epoch;
            internal List<int> GirlIds = new List<int>();
        }

        private static readonly object Sync = new object();
        private static PendingIntroduction pending;
        private static long nextToken;

        private static long scheduledCount;
        private static long supersededScheduleCount;
        private static long capturedCheckpointCount;
        private static long capturedPendingCount;
        private static long captureFailureCount;
        private static long completedFlushCount;
        private static long staleNewGirlsClearCount;
        private static long stalePendingTokenClearCount;
        private static long restoredLoadCount;
        private static long restoredGirlCount;
        private static long unresolvedLoadedGirlCount;
        private static long legacySectionAbsentCount;
        private static long invalidSectionCount;
        private static long associationMissingCount;
        private static long schedulingFailureCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get
            {
                return SuccessorIntroductionPatchHealth.IsHealthy &&
                    DeferredCarrierEpochRepair.IsImplemented;
            }
        }

        internal static long ScheduledCount { get { return Interlocked.Read(ref scheduledCount); } }
        internal static long SupersededScheduleCount { get { return Interlocked.Read(ref supersededScheduleCount); } }
        internal static long CapturedCheckpointCount { get { return Interlocked.Read(ref capturedCheckpointCount); } }
        internal static long CapturedPendingCount { get { return Interlocked.Read(ref capturedPendingCount); } }
        internal static long CaptureFailureCount { get { return Interlocked.Read(ref captureFailureCount); } }
        internal static long CompletedFlushCount { get { return Interlocked.Read(ref completedFlushCount); } }
        internal static long StaleNewGirlsClearCount { get { return Interlocked.Read(ref staleNewGirlsClearCount); } }
        internal static long StalePendingTokenClearCount { get { return Interlocked.Read(ref stalePendingTokenClearCount); } }
        internal static long RestoredLoadCount { get { return Interlocked.Read(ref restoredLoadCount); } }
        internal static long RestoredGirlCount { get { return Interlocked.Read(ref restoredGirlCount); } }
        internal static long UnresolvedLoadedGirlCount { get { return Interlocked.Read(ref unresolvedLoadedGirlCount); } }
        internal static long LegacySectionAbsentCount { get { return Interlocked.Read(ref legacySectionAbsentCount); } }
        internal static long InvalidSectionCount { get { return Interlocked.Read(ref invalidSectionCount); } }
        internal static long AssociationMissingCount { get { return Interlocked.Read(ref associationMissingCount); } }
        internal static long SchedulingFailureCount { get { return Interlocked.Read(ref schedulingFailureCount); } }
        internal static string LastDiagnostic { get { lock (Sync) { return lastDiagnostic; } } }

        /// <summary>
        /// Runs only after vanilla Date_Graduation.StartIntroductions() successfully
        /// scheduled its private five-second carrier. The only vanilla caller is the
        /// audited Find_Successor() path.
        /// </summary>
        internal static void RegisterScheduledIntroduction()
        {
            if (!IsImplemented)
            {
                SetDiagnostic("A11 successor-introduction repair is not healthy; schedule token was not claimed.");
                return;
            }

            List<int> ids;
            string error;
            if (!TrySnapshotLiveNewGirlIds(out ids, out error) || ids.Count == 0)
            {
                if (!string.IsNullOrEmpty(error))
                {
                    SetDiagnostic(error);
                }
                return;
            }

            PendingIntroduction replacement = new PendingIntroduction
            {
                Token = Interlocked.Increment(ref nextToken),
                Epoch = LoadEpoch.Capture(),
                GirlIds = ids
            };

            bool superseded = false;
            lock (Sync)
            {
                superseded = pending != null && pending.Epoch == replacement.Epoch;
                pending = replacement;
                lastDiagnostic =
                    "A11 registered pending successor introduction token " +
                    replacement.Token.ToString(CultureInfo.InvariantCulture) +
                    " with " + ids.Count.ToString(CultureInfo.InvariantCulture) +
                    " ordered idol ID(s) at load epoch " +
                    replacement.Epoch.ToString(CultureInfo.InvariantCulture) + ".";
            }

            Interlocked.Increment(ref scheduledCount);
            if (superseded)
            {
                Interlocked.Increment(ref supersededScheduleCount);
            }
        }

        /// <summary>
        /// Caller-thread repair-envelope capture. An empty list is authoritative for
        /// current-format saves: no successor introduction was pending at this exact
        /// save request. A current token is validated against both the live transient
        /// list and the target SavedData roster before it is persisted.
        /// </summary>
        internal static bool TryCaptureForEnvelope(
            SaveManager.SavedData dataToSave,
            out List<int> girlIds,
            out string error)
        {
            girlIds = new List<int>();
            error = string.Empty;

            if (!IsImplemented)
            {
                return CaptureFailed(
                    "A11 successor-introduction capture/epoch guard is not healthy.",
                    out error);
            }
            if (dataToSave == null)
            {
                return CaptureFailed(
                    "A11 cannot capture successor introduction state without the exact SavedData request.",
                    out error);
            }

            PendingIntroduction snapshot = SnapshotCurrentPending();
            if (snapshot == null)
            {
                Interlocked.Increment(ref capturedCheckpointCount);
                SetDiagnostic("A11 captured an authoritative empty successor-introduction section.");
                return true;
            }

            List<int> liveIds;
            if (!TrySnapshotLiveNewGirlIds(out liveIds, out error))
            {
                return CaptureFailed(error, out error);
            }

            HashSet<int> liveSet = new HashSet<int>(liveIds);
            for (int i = 0; i < snapshot.GirlIds.Count; i++)
            {
                if (!liveSet.Contains(snapshot.GirlIds[i]))
                {
                    return CaptureFailed(
                        "A11 pending successor token no longer matches data_girls.new_girls; refusing a falsely exact checkpoint.",
                        out error);
                }
            }

            if (dataToSave.data_girls__Girls == null)
            {
                return CaptureFailed(
                    "A11 target SavedData girl list is null after SaveEvent population.",
                    out error);
            }

            Dictionary<int, int> savedCounts = BuildSavedGirlIdCounts(dataToSave.data_girls__Girls);
            HashSet<int> unique = new HashSet<int>();
            for (int i = 0; i < snapshot.GirlIds.Count; i++)
            {
                int id = snapshot.GirlIds[i];
                int count;
                if (id < 0 || !unique.Add(id) || !savedCounts.TryGetValue(id, out count) || count != 1)
                {
                    return CaptureFailed(
                        "A11 pending successor idol ID does not resolve exactly once in the target SavedData roster.",
                        out error);
                }
                girlIds.Add(id);
            }

            Interlocked.Increment(ref capturedCheckpointCount);
            Interlocked.Increment(ref capturedPendingCount);
            SetDiagnostic(
                "A11 captured one pending successor-introduction flush with " +
                girlIds.Count.ToString(CultureInfo.InvariantCulture) +
                " ordered idol ID(s).");
            return true;
        }

        /// <summary>
        /// A successful target load must never inherit CLR references from the
        /// discarded timeline. data_girls.Reset() does not clear new_girls, so this
        /// runs before vanilla reconstructs the target roster.
        /// </summary>
        internal static void ClearBeforeTargetGirlLoad()
        {
            int staleCount = 0;
            if (data_girls.new_girls == null)
            {
                data_girls.new_girls = new List<data_girls.girls>();
            }
            else
            {
                staleCount = data_girls.new_girls.Count;
                data_girls.new_girls.Clear();
            }

            bool clearedPending = false;
            lock (Sync)
            {
                if (pending != null)
                {
                    pending = null;
                    clearedPending = true;
                }
            }

            if (staleCount > 0)
            {
                Interlocked.Add(ref staleNewGirlsClearCount, staleCount);
            }
            if (clearedPending)
            {
                Interlocked.Increment(ref stalePendingTokenClearCount);
            }

            if (staleCount > 0 || clearedPending)
            {
                SetDiagnostic(
                    "A11 cleared discarded-timeline successor introduction state before target idol reconstruction.");
            }
        }

        /// <summary>
        /// Called after vanilla data_girls.LoadFunction() has rebuilt the target idol
        /// objects. Current-format IDs are validated against the target DTO first,
        /// then rebound in order to fresh loaded objects. Runtime-unresolved IDs are
        /// logged/dropped as required by the audit; no replacement idol is generated.
        /// </summary>
        internal static void RestoreAfterTargetGirlLoad()
        {
            if (!IsImplemented)
            {
                SetDiagnostic("A11 restore is not healthy; no successor introduction was reconstructed.");
                return;
            }

            SaveManager.SavedData target = GetTargetSavedData();
            if (target == null)
            {
                Interlocked.Increment(ref associationMissingCount);
                SetDiagnostic("A11 target SavedData is unavailable after idol reconstruction.");
                return;
            }

            RepairEnvelopeLoadState state;
            if (!RepairEnvelopeTransport.TryGetLoadState(target, out state))
            {
                Interlocked.Increment(ref associationMissingCount);
                SetDiagnostic("A11 target SavedData has no repair-envelope read association.");
                return;
            }

            if (!state.Present)
            {
                Interlocked.Increment(ref legacySectionAbsentCount);
                SetDiagnostic(
                    "A11 loaded a pre-envelope save; pending successor introductions are unknowable and were not fabricated.");
                return;
            }

            if (!state.Valid || state.Envelope == null || state.Envelope.records == null)
            {
                RecordInvalid(
                    "A11 loaded a present but invalid repair envelope; successor-introduction state was not treated as empty.");
                return;
            }

            RepairEnvelopeRecordsV1 records = state.Envelope.records;
            if (records.successor_introductions_version == 0)
            {
                Interlocked.Increment(ref legacySectionAbsentCount);
                SetDiagnostic(
                    "A11 loaded a pre-A11 envelope; pending successor introductions are unknowable and were not fabricated.");
                return;
            }

            if (records.successor_introductions_version != SectionVersion ||
                records.successor_introduction_girl_ids == null || target.data_girls__Girls == null)
            {
                RecordInvalid("A11 repair section is invalid or unsupported.");
                return;
            }

            List<int> ids = records.successor_introduction_girl_ids;
            if (ids.Count == 0)
            {
                Interlocked.Increment(ref restoredLoadCount);
                SetDiagnostic("A11 restored an authoritative empty successor-introduction section.");
                return;
            }

            Dictionary<int, int> savedCounts = BuildSavedGirlIdCounts(target.data_girls__Girls);
            HashSet<int> unique = new HashSet<int>();
            for (int i = 0; i < ids.Count; i++)
            {
                int count;
                if (ids[i] < 0 || !unique.Add(ids[i]) || !savedCounts.TryGetValue(ids[i], out count) || count != 1)
                {
                    RecordInvalid(
                        "A11 repair section contains an idol ID that is invalid, duplicated, or absent from the target SavedData roster.");
                    return;
                }
            }

            Dictionary<int, data_girls.girls> liveById = new Dictionary<int, data_girls.girls>();
            HashSet<int> ambiguousLiveIds = new HashSet<int>();
            if (data_girls.girl != null)
            {
                for (int i = 0; i < data_girls.girl.Count; i++)
                {
                    data_girls.girls girl = data_girls.girl[i];
                    if (girl == null || girl.id < 0)
                    {
                        continue;
                    }
                    if (liveById.ContainsKey(girl.id))
                    {
                        ambiguousLiveIds.Add(girl.id);
                    }
                    else
                    {
                        liveById.Add(girl.id, girl);
                    }
                }
            }

            List<data_girls.girls> rebound = new List<data_girls.girls>();
            for (int i = 0; i < ids.Count; i++)
            {
                data_girls.girls girl;
                if (ambiguousLiveIds.Contains(ids[i]) || !liveById.TryGetValue(ids[i], out girl) || girl == null)
                {
                    Interlocked.Increment(ref unresolvedLoadedGirlCount);
                    Debug.LogWarning(
                        SaveNLoadFixesConstants.LogPrefix +
                        "A11 could not resolve saved successor idol ID " +
                        ids[i].ToString(CultureInfo.InvariantCulture) +
                        " to exactly one loaded idol; participant was dropped without generation/hire replay.");
                    continue;
                }
                rebound.Add(girl);
            }

            if (rebound.Count == 0)
            {
                Interlocked.Increment(ref restoredLoadCount);
                SetDiagnostic(
                    "A11 current-format successor token had no resolvable loaded participants; no replacement idol or introduction was fabricated.");
                return;
            }

            if (data_girls.new_girls == null)
            {
                data_girls.new_girls = new List<data_girls.girls>();
            }
            else
            {
                data_girls.new_girls.Clear();
            }
            data_girls.new_girls.AddRange(rebound);

            Date_Graduation graduation = GetDateGraduation();
            if (graduation == null)
            {
                data_girls.new_girls.Clear();
                Interlocked.Increment(ref schedulingFailureCount);
                SetDiagnostic("A11 could not resolve the loaded Date_Graduation component; rebound participants were cleared.");
                return;
            }

            try
            {
                // Deliberately restart vanilla's small five-second presentation wait.
                // The audit permits a remaining/small delay; using the stock full wait
                // avoids persisting scaled realtime and never replays successor generation.
                graduation.StartIntroductions();
            }
            catch (Exception exception)
            {
                data_girls.new_girls.Clear();
                ClearPendingToken();
                Interlocked.Increment(ref schedulingFailureCount);
                SetDiagnostic(
                    "A11 failed to schedule vanilla successor introductions after rebinding; participants were cleared: " +
                    exception.Message);
                return;
            }

            Interlocked.Increment(ref restoredLoadCount);
            Interlocked.Add(ref restoredGirlCount, rebound.Count);
            SetDiagnostic(
                "A11 rebound " + rebound.Count.ToString(CultureInfo.InvariantCulture) +
                " successor idol(s) and rescheduled vanilla's five-second introduction flush.");
        }

        internal static FlushState BeginIntroductionFlush()
        {
            FlushState state = new FlushState();
            lock (Sync)
            {
                if (pending == null || !LoadEpoch.IsCurrent(pending.Epoch))
                {
                    return state;
                }

                state.Token = pending.Token;
                state.HadPending = true;
                state.HadGirls = data_girls.new_girls != null && data_girls.new_girls.Count > 0;

                // A token with no remaining transient participants cannot produce an
                // introduction anymore. Retire it rather than persisting ghost work.
                if (!state.HadGirls)
                {
                    pending = null;
                    Interlocked.Increment(ref completedFlushCount);
                    lastDiagnostic = "A11 retired a pending successor token whose transient participant list was already empty.";
                }
            }
            return state;
        }

        internal static void CompleteIntroductionFlush(FlushState state)
        {
            if (state == null || !state.HadPending || !state.HadGirls ||
                data_girls.new_girls == null || data_girls.new_girls.Count != 0)
            {
                return;
            }

            bool cleared = false;
            lock (Sync)
            {
                if (pending != null && pending.Token == state.Token)
                {
                    pending = null;
                    cleared = true;
                    lastDiagnostic =
                        "A11 successor introduction token was consumed by vanilla data_girls.StartIntroductions().";
                }
            }

            if (cleared)
            {
                Interlocked.Increment(ref completedFlushCount);
            }
        }

        private static PendingIntroduction SnapshotCurrentPending()
        {
            lock (Sync)
            {
                if (pending == null)
                {
                    return null;
                }
                if (!LoadEpoch.IsCurrent(pending.Epoch))
                {
                    pending = null;
                    Interlocked.Increment(ref stalePendingTokenClearCount);
                    return null;
                }

                return new PendingIntroduction
                {
                    Token = pending.Token,
                    Epoch = pending.Epoch,
                    GirlIds = new List<int>(pending.GirlIds)
                };
            }
        }

        private static bool TrySnapshotLiveNewGirlIds(out List<int> ids, out string error)
        {
            ids = new List<int>();
            error = string.Empty;
            if (data_girls.new_girls == null)
            {
                error = "A11 data_girls.new_girls registry is null.";
                return false;
            }

            HashSet<int> unique = new HashSet<int>();
            for (int i = 0; i < data_girls.new_girls.Count; i++)
            {
                data_girls.girls girl = data_girls.new_girls[i];
                if (girl == null || girl.id < 0 || !unique.Add(girl.id))
                {
                    error = "A11 data_girls.new_girls contains a null, invalid, or duplicate idol identity.";
                    return false;
                }
                ids.Add(girl.id);
            }
            return true;
        }

        private static Dictionary<int, int> BuildSavedGirlIdCounts(List<data_girls.GirlData> rows)
        {
            Dictionary<int, int> counts = new Dictionary<int, int>();
            for (int i = 0; i < rows.Count; i++)
            {
                data_girls.GirlData row = rows[i];
                if (row == null || row.id < 0)
                {
                    continue;
                }
                int count;
                counts.TryGetValue(row.id, out count);
                counts[row.id] = count + 1;
            }
            return counts;
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

        private static Date_Graduation GetDateGraduation()
        {
            try
            {
                mainScript main = Camera.main == null ? null : Camera.main.GetComponent<mainScript>();
                return main == null || main.Data == null ? null : main.Data.GetComponent<Date_Graduation>();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void ClearPendingToken()
        {
            lock (Sync)
            {
                pending = null;
            }
        }

        private static bool CaptureFailed(string diagnostic, out string error)
        {
            error = diagnostic ?? "unknown A11 capture failure";
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

    internal static class SuccessorIntroductionPatchHealth
    {
        internal const int ExpectedTargetMethodCount = 3;
        private static readonly object Sync = new object();
        private static readonly HashSet<string> ResolvedTargets = new HashSet<string>(StringComparer.Ordinal);
        private static string failure = string.Empty;

        internal static int ResolvedTargetMethodCount
        {
            get { lock (Sync) { return ResolvedTargets.Count; } }
        }

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return ResolvedTargets.Count == ExpectedTargetMethodCount && string.IsNullOrEmpty(failure);
                }
            }
        }

        internal static void ReportTargetResolved(string target)
        {
            lock (Sync)
            {
                if (!ResolvedTargets.Add(target ?? string.Empty))
                {
                    failure = "A11 resolved the same Harmony target more than once: " + (target ?? string.Empty);
                }
                else if (ResolvedTargets.Count > ExpectedTargetMethodCount)
                {
                    failure = "A11 resolved more Harmony targets than the frozen three-method manifest.";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown A11 patch failure";
            }
        }
    }
}
