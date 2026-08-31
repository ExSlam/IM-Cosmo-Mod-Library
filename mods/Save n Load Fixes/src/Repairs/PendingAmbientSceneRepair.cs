using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using SaveNLoadFixes.Safety;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// N12-A / finding #13 foundation: capture each already-selected delayed ambient
    /// scene as semantic future work at the exact AssignSceneWithDelay factory seam.
    ///
    /// This capture layer freezes only durable semantics consumed by the separate
    /// N12-B persistence and N12-C restore layers: due game time, scene type, stable
    /// idol IDs, and a structural room locator. The existing A08 exact MoveNext
    /// guard remains the sole LoadEpoch mutation guard for original/replacement coroutines.
    /// </summary>
    internal static class PendingAmbientSceneRepair
    {
        internal sealed class PendingAmbientSceneSnapshot
        {
            internal long JobId;
            internal long Epoch;
            internal DateTime DueGameTime;
            internal int FloorId;
            internal int RoomOrdinal;
            internal int RoomType;
            internal int SceneType;
            internal List<int> GirlIds;

            internal PendingAmbientSceneSnapshot Clone()
            {
                return new PendingAmbientSceneSnapshot
                {
                    JobId = this.JobId,
                    Epoch = this.Epoch,
                    DueGameTime = this.DueGameTime,
                    FloorId = this.FloorId,
                    RoomOrdinal = this.RoomOrdinal,
                    RoomType = this.RoomType,
                    SceneType = this.SceneType,
                    GirlIds = this.GirlIds == null
                        ? new List<int>()
                        : new List<int>(this.GirlIds)
                };
            }
        }

        private sealed class PendingLease
        {
            internal long JobId;
            internal long Epoch;
            private int terminal;

            internal bool TryFinish()
            {
                return Interlocked.Exchange(ref this.terminal, 1) == 0;
            }

            ~PendingLease()
            {
                PendingAmbientSceneRepair.ObserveAbandoned(this);
            }
        }

        private static readonly object Sync = new object();
        private static readonly ConditionalWeakTable<object, PendingLease> LeaseByIterator =
            new ConditionalWeakTable<object, PendingLease>();
        private static readonly Dictionary<long, PendingAmbientSceneSnapshot> ActiveByJobId =
            new Dictionary<long, PendingAmbientSceneSnapshot>();

        private static long nextJobId;
        private static long activeTrackedCount;
        private static long registeredCount;
        private static long completedCount;
        private static long staleRetiredCount;
        private static long faultRetiredCount;
        private static long abandonedCount;
        private static long rejectedCaptureCount;
        private static long restoreRollbackRetiredCount;
        private static int unhealthyWarningLogged;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get
            {
                // A08 already owns the exact stale-generation MoveNext suppression.
                // N12-A is healthy only when both its semantic capture seams and that
                // pre-existing guard are healthy together.
                return PendingAmbientScenePatchHealth.IsHealthy &&
                       DeferredCarrierEpochPatchHealth.IsHealthy;
            }
        }

        internal static long ActiveTrackedCount
        {
            get { return Interlocked.Read(ref activeTrackedCount); }
        }

        internal static int CurrentEpochJobCount
        {
            get
            {
                long current = LoadEpoch.Current;
                int count = 0;
                lock (Sync)
                {
                    foreach (PendingAmbientSceneSnapshot job in ActiveByJobId.Values)
                    {
                        if (job != null && job.Epoch == current)
                        {
                            count++;
                        }
                    }
                }
                return count;
            }
        }

        internal static long RegisteredCount { get { return Interlocked.Read(ref registeredCount); } }
        internal static long CompletedCount { get { return Interlocked.Read(ref completedCount); } }
        internal static long StaleRetiredCount { get { return Interlocked.Read(ref staleRetiredCount); } }
        internal static long FaultRetiredCount { get { return Interlocked.Read(ref faultRetiredCount); } }
        internal static long AbandonedCount { get { return Interlocked.Read(ref abandonedCount); } }
        internal static long RejectedCaptureCount { get { return Interlocked.Read(ref rejectedCaptureCount); } }
        internal static long RestoreRollbackRetiredCount { get { return Interlocked.Read(ref restoreRollbackRetiredCount); } }
        internal static string LastDiagnostic { get { lock (Sync) { return lastDiagnostic; } } }

        internal static void RegisterScheduledJob(
            IEnumerator iterator,
            DateTime dueGameTime,
            agency._room room,
            Scenes.type sceneType,
            List<data_girls.girls> usedGirls,
            agency liveAgency)
        {
            if (iterator == null)
            {
                Reject("N12-A AssignSceneWithDelay returned a null iterator; semantic job was not registered.");
                return;
            }

            if (!IsImplemented)
            {
                if (Interlocked.Exchange(ref unhealthyWarningLogged, 1) == 0)
                {
                    Debug.LogWarning(
                        SaveNLoadFixesConstants.LogPrefix +
                        "N12-A pending ambient-scene registry is not healthy because its exact capture seams or the existing A08 LoadEpoch guard are incomplete.");
                }
                return;
            }

            int floorId;
            int roomOrdinal;
            int roomType;
            string error;
            if (!TryLocateRoom(liveAgency, room, out floorId, out roomOrdinal, out roomType, out error))
            {
                Reject(error);
                return;
            }

            List<int> girlIds;
            if (!TrySnapshotGirlIds(usedGirls, out girlIds, out error))
            {
                Reject(error);
                return;
            }

            if (!Enum.IsDefined(typeof(Scenes.type), sceneType))
            {
                Reject("N12-A pending ambient scene has an undefined scene type.");
                return;
            }

            long epoch = LoadEpoch.Capture();
            long jobId = Interlocked.Increment(ref nextJobId);
            PendingLease lease = new PendingLease
            {
                JobId = jobId,
                Epoch = epoch
            };
            PendingAmbientSceneSnapshot snapshot = new PendingAmbientSceneSnapshot
            {
                JobId = jobId,
                Epoch = epoch,
                DueGameTime = dueGameTime,
                FloorId = floorId,
                RoomOrdinal = roomOrdinal,
                RoomType = roomType,
                SceneType = (int)sceneType,
                GirlIds = girlIds
            };

            lock (Sync)
            {
                PendingLease existing;
                if (LeaseByIterator.TryGetValue(iterator, out existing))
                {
                    RejectLocked("N12-A received the same delayed-scene iterator more than once; duplicate semantic registration was rejected.");
                    lease.TryFinish();
                    GC.SuppressFinalize(lease);
                    return;
                }

                LeaseByIterator.Add(iterator, lease);
                ActiveByJobId.Add(jobId, snapshot);
                lastDiagnostic =
                    "N12-A registered pending ambient scene job " +
                    jobId.ToString(CultureInfo.InvariantCulture) +
                    " at load epoch " + epoch.ToString(CultureInfo.InvariantCulture) +
                    " for room " + BuildLocator(floorId, roomOrdinal, roomType) + ".";
            }

            Interlocked.Increment(ref activeTrackedCount);
            Interlocked.Increment(ref registeredCount);
        }

        /// <summary>
        /// N12-A observes MoveNext before A08's default-priority guard. It never
        /// decides whether vanilla may run. When the epoch is stale it retires only
        /// N12 semantic bookkeeping; A08 remains responsible for suppressing the
        /// old room.assign(...) mutation.
        /// </summary>
        internal static void ObserveMoveNextEntry(object iterator)
        {
            PendingLease lease;
            if (!TryGetLease(iterator, out lease) || LoadEpoch.IsCurrent(lease.Epoch))
            {
                return;
            }

            if (Retire(iterator, lease))
            {
                Interlocked.Increment(ref staleRetiredCount);
                SetDiagnostic(
                    "N12-A retired stale pending ambient scene job " +
                    lease.JobId.ToString(CultureInfo.InvariantCulture) +
                    " before A08 suppressed its discarded-timeline MoveNext.");
            }
        }

        internal static void ObserveMoveNextReturn(object iterator, bool result)
        {
            if (result)
            {
                return;
            }

            PendingLease lease;
            if (!TryGetLease(iterator, out lease))
            {
                return;
            }

            if (!LoadEpoch.IsCurrent(lease.Epoch))
            {
                if (Retire(iterator, lease))
                {
                    Interlocked.Increment(ref staleRetiredCount);
                    SetDiagnostic(
                        "N12-A retired stale pending ambient scene job " +
                        lease.JobId.ToString(CultureInfo.InvariantCulture) + ".");
                }
                return;
            }

            if (Retire(iterator, lease))
            {
                Interlocked.Increment(ref completedCount);
                SetDiagnostic(
                    "N12-A pending ambient scene job " +
                    lease.JobId.ToString(CultureInfo.InvariantCulture) +
                    " completed in its original load epoch.");
            }
        }

        internal static void ObserveMoveNextFault(object iterator, Exception exception)
        {
            PendingLease lease;
            if (!TryGetLease(iterator, out lease) || !Retire(iterator, lease))
            {
                return;
            }

            Interlocked.Increment(ref faultRetiredCount);
            SetDiagnostic(
                "N12-A pending ambient scene job " +
                lease.JobId.ToString(CultureInfo.InvariantCulture) +
                " faulted and was retired: " +
                (exception == null ? "<unknown exception>" : exception.GetType().FullName) + ".");
        }

        /// <summary>
        /// N12-B envelope capture consumes this detached semantic snapshot.
        /// Stale-epoch jobs are deliberately filtered even if Unity has not yet
        /// resumed their old iterator and allowed A08 to terminate it.
        /// </summary>
        internal static bool TrySnapshotCurrentJobs(
            out List<PendingAmbientSceneSnapshot> snapshots,
            out string error)
        {
            snapshots = new List<PendingAmbientSceneSnapshot>();
            error = string.Empty;
            if (!IsImplemented)
            {
                error = "N12-A pending ambient-scene capture/epoch guard is not healthy.";
                return false;
            }

            long current = LoadEpoch.Current;
            lock (Sync)
            {
                foreach (PendingAmbientSceneSnapshot job in ActiveByJobId.Values)
                {
                    if (job != null && job.Epoch == current)
                    {
                        snapshots.Add(job.Clone());
                    }
                }
            }

            snapshots.Sort(delegate(PendingAmbientSceneSnapshot left, PendingAmbientSceneSnapshot right)
            {
                return left.JobId.CompareTo(right.JobId);
            });
            return true;
        }

        /// <summary>
        /// N12-C verifies that invoking vanilla's patched factory registered exactly
        /// the semantic job it asked to recreate before admitting the coroutine.
        /// </summary>
        internal static bool TryGetRegisteredSnapshot(
            object iterator,
            out PendingAmbientSceneSnapshot snapshot)
        {
            snapshot = null;
            PendingLease lease;
            if (!TryGetLease(iterator, out lease))
            {
                return false;
            }

            lock (Sync)
            {
                PendingAmbientSceneSnapshot current;
                if (!ActiveByJobId.TryGetValue(lease.JobId, out current) || current == null)
                {
                    return false;
                }
                snapshot = current.Clone();
                return true;
            }
        }

        /// <summary>
        /// Transaction rollback for an N12-C replacement iterator that was created
        /// but must not survive a failed all-or-nothing restore attempt. This changes
        /// only N12 semantic bookkeeping; A08 remains the sole MoveNext mutation guard.
        /// </summary>
        internal static bool CancelRegisteredJob(object iterator, string reason)
        {
            PendingLease lease;
            if (!TryGetLease(iterator, out lease) || !Retire(iterator, lease))
            {
                return false;
            }

            Interlocked.Increment(ref restoreRollbackRetiredCount);
            SetDiagnostic(
                (reason ?? "N12-C cancelled a replacement pending ambient-scene job.") +
                " Job " + lease.JobId.ToString(CultureInfo.InvariantCulture) + ".");
            return true;
        }

        private static bool TryLocateRoom(
            agency liveAgency,
            agency._room room,
            out int floorId,
            out int roomOrdinal,
            out int roomType,
            out string error)
        {
            floorId = -1;
            roomOrdinal = -1;
            roomType = -1;
            error = string.Empty;
            if (liveAgency == null || liveAgency.floors == null || room == null)
            {
                error = "N12-A cannot resolve the scheduled room because live agency/floor state is unavailable.";
                return false;
            }

            HashSet<int> floorIds = new HashSet<int>();
            int matches = 0;
            for (int f = 0; f < liveAgency.floors.Count; f++)
            {
                agency._floor floor = liveAgency.floors[f];
                if (floor == null || floor.floor == null || !floorIds.Add(floor.FloorID))
                {
                    error = "N12-A live agency floor structure is invalid or has duplicate FloorID values.";
                    return false;
                }

                for (int r = 0; r < floor.floor.Count; r++)
                {
                    if (!object.ReferenceEquals(floor.floor[r], room))
                    {
                        continue;
                    }

                    floorId = floor.FloorID;
                    roomOrdinal = r;
                    roomType = (int)room.type;
                    matches++;
                }
            }

            if (matches != 1 || roomOrdinal < 0)
            {
                error = "N12-A scheduled room does not resolve to exactly one FloorID + room-ordinal structural locator.";
                return false;
            }

            return true;
        }

        private static bool TrySnapshotGirlIds(
            List<data_girls.girls> usedGirls,
            out List<int> girlIds,
            out string error)
        {
            girlIds = new List<int>();
            error = string.Empty;
            if (usedGirls == null)
            {
                error = "N12-A delayed ambient scene has a null selected-idol list.";
                return false;
            }

            HashSet<int> seen = new HashSet<int>();
            for (int i = 0; i < usedGirls.Count; i++)
            {
                data_girls.girls girl = usedGirls[i];
                if (girl == null || girl.id < 0 || !seen.Add(girl.id) ||
                    !object.ReferenceEquals(data_girls.GetGirlByID(girl.id), girl))
                {
                    error = "N12-A delayed ambient scene does not have one canonical unique stable idol ID for every selected idol.";
                    return false;
                }
                girlIds.Add(girl.id);
            }
            return true;
        }

        private static bool TryGetLease(object iterator, out PendingLease lease)
        {
            lease = null;
            if (iterator == null)
            {
                return false;
            }
            lock (Sync)
            {
                return LeaseByIterator.TryGetValue(iterator, out lease);
            }
        }

        private static bool Retire(object iterator, PendingLease lease)
        {
            if (iterator == null || lease == null)
            {
                return false;
            }

            bool retired = false;
            lock (Sync)
            {
                PendingLease current;
                if (LeaseByIterator.TryGetValue(iterator, out current) &&
                    object.ReferenceEquals(current, lease) && current.TryFinish())
                {
                    LeaseByIterator.Remove(iterator);
                    ActiveByJobId.Remove(current.JobId);
                    retired = true;
                }
            }

            if (!retired)
            {
                return false;
            }

            GC.SuppressFinalize(lease);
            Interlocked.Decrement(ref activeTrackedCount);
            return true;
        }

        private static void ObserveAbandoned(PendingLease lease)
        {
            if (lease == null || !lease.TryFinish())
            {
                return;
            }

            lock (Sync)
            {
                ActiveByJobId.Remove(lease.JobId);
                lastDiagnostic =
                    "N12-A pending ambient scene job " +
                    lease.JobId.ToString(CultureInfo.InvariantCulture) +
                    " was abandoned and removed with its iterator.";
            }
            Interlocked.Decrement(ref activeTrackedCount);
            Interlocked.Increment(ref abandonedCount);
        }

        private static string BuildLocator(int floorId, int roomOrdinal, int roomType)
        {
            return floorId.ToString(CultureInfo.InvariantCulture) + ":" +
                   roomOrdinal.ToString(CultureInfo.InvariantCulture) + ":" +
                   roomType.ToString(CultureInfo.InvariantCulture);
        }

        private static void Reject(string diagnostic)
        {
            lock (Sync)
            {
                RejectLocked(diagnostic);
            }
        }

        private static void RejectLocked(string diagnostic)
        {
            Interlocked.Increment(ref rejectedCaptureCount);
            lastDiagnostic = diagnostic ?? string.Empty;
            Debug.LogError(SaveNLoadFixesConstants.LogPrefix + (diagnostic ?? string.Empty));
        }

        private static void SetDiagnostic(string diagnostic)
        {
            lock (Sync)
            {
                lastDiagnostic = diagnostic ?? string.Empty;
            }
        }
    }

    /// <summary>
    /// Exact N12-A health: the private AssignSceneWithDelay factory plus its one
    /// generated MoveNext. Full implementation health additionally requires A08's
    /// already-frozen seven-carrier LoadEpoch manifest.
    /// </summary>
    internal static class PendingAmbientScenePatchHealth
    {
        internal const int ExpectedTargetMethodCount = 2;

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
                    return ResolvedTargets.Count == ExpectedTargetMethodCount &&
                           string.IsNullOrEmpty(failure);
                }
            }
        }

        internal static void ReportResolved(System.Reflection.MethodBase method)
        {
            if (method == null)
            {
                ReportFailure("<null>", "resolved target was null");
                return;
            }
            lock (Sync)
            {
                ResolvedTargets.Add(Describe(method));
            }
        }

        internal static void ReportFailure(string seam, string reason)
        {
            lock (Sync)
            {
                failure = (seam ?? "<unknown>") + ": " + (reason ?? string.Empty);
            }
            Debug.LogError(
                SaveNLoadFixesConstants.LogPrefix +
                "N12-A pending ambient-scene patch failed at " + failure);
        }

        private static string Describe(System.Reflection.MethodBase method)
        {
            string owner = method.DeclaringType == null ? "<unknown>" : method.DeclaringType.FullName;
            return owner + "." + method.Name;
        }
    }
}
