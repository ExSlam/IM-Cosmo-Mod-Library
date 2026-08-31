using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using SaveNLoadFixes.Persistence;
using SaveNLoadFixes.Safety;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// N12-C / finding #13 restore half: validate the complete target-save section,
    /// rebind only fresh loaded room/idol objects, then recreate the original
    /// AssignSceneWithDelay iterator without rerunning GenerateScene or consuming RNG.
    /// A08 remains the sole stale-MoveNext mutation guard.
    /// </summary>
    internal static class PendingAmbientSceneRestore
    {
        private sealed class RestoreAttempt
        {
            internal long Epoch;
        }

        private sealed class AgencyLoadLease
        {
            internal long Epoch;
        }

        private sealed class ValidatedJob
        {
            internal DateTime DueGameTime;
            internal int FloorId;
            internal int RoomOrdinal;
            internal int RoomType;
            internal Scenes.type SceneType;
            internal List<int> GirlIds = new List<int>();
        }

        private sealed class ResolvedJob
        {
            internal DateTime DueGameTime;
            internal int FloorId;
            internal int RoomOrdinal;
            internal int RoomType;
            internal Scenes.type SceneType;
            internal agency._room Room;
            internal List<data_girls.girls> Girls = new List<data_girls.girls>();
        }

        private static readonly object Sync = new object();
        private static readonly ConditionalWeakTable<SaveManager.SavedData, RestoreAttempt> AttemptedTargets =
            new ConditionalWeakTable<SaveManager.SavedData, RestoreAttempt>();
        private static readonly ConditionalWeakTable<object, AgencyLoadLease> AgencyLoadEpochByIterator =
            new ConditionalWeakTable<object, AgencyLoadLease>();

        private static long restoredLoadCount;
        private static long rescheduledJobCount;
        private static long legacySectionAbsentCount;
        private static long invalidSectionCount;
        private static long associationMissingCount;
        private static long runtimeResolutionFailureCount;
        private static long schedulingFailureCount;
        private static long duplicateAttemptSuppressedCount;
        private static long staleAgencyLoadTriggerIgnoredCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get
            {
                return PendingAmbientScenePersistence.IsImplemented &&
                       PendingAmbientSceneRestorePatchHealth.IsHealthy;
            }
        }

        internal static long RestoredLoadCount { get { return Interlocked.Read(ref restoredLoadCount); } }
        internal static long RescheduledJobCount { get { return Interlocked.Read(ref rescheduledJobCount); } }
        internal static long LegacySectionAbsentCount { get { return Interlocked.Read(ref legacySectionAbsentCount); } }
        internal static long InvalidSectionCount { get { return Interlocked.Read(ref invalidSectionCount); } }
        internal static long AssociationMissingCount { get { return Interlocked.Read(ref associationMissingCount); } }
        internal static long RuntimeResolutionFailureCount { get { return Interlocked.Read(ref runtimeResolutionFailureCount); } }
        internal static long SchedulingFailureCount { get { return Interlocked.Read(ref schedulingFailureCount); } }
        internal static long DuplicateAttemptSuppressedCount { get { return Interlocked.Read(ref duplicateAttemptSuppressedCount); } }
        internal static long StaleAgencyLoadTriggerIgnoredCount { get { return Interlocked.Read(ref staleAgencyLoadTriggerIgnoredCount); } }
        internal static string LastDiagnostic { get { lock (Sync) { return lastDiagnostic; } } }

        internal static void RegisterAgencyLoadIterator(IEnumerator iterator)
        {
            if (iterator == null)
            {
                return;
            }

            long epoch = LoadEpoch.Capture();
            lock (Sync)
            {
                AgencyLoadLease existing;
                if (!AgencyLoadEpochByIterator.TryGetValue(iterator, out existing))
                {
                    AgencyLoadEpochByIterator.Add(iterator, new AgencyLoadLease { Epoch = epoch });
                }
            }
        }

        internal static void ObserveAgencyLoadMoveNext(object iterator, bool result)
        {
            if (result || iterator == null)
            {
                return;
            }

            AgencyLoadLease lease;
            lock (Sync)
            {
                if (!AgencyLoadEpochByIterator.TryGetValue(iterator, out lease))
                {
                    lastDiagnostic = "N12-C observed terminal agency LoadData without its exact factory epoch association.";
                    return;
                }
                AgencyLoadEpochByIterator.Remove(iterator);
            }

            if (!LoadEpoch.IsCurrent(lease.Epoch))
            {
                Interlocked.Increment(ref staleAgencyLoadTriggerIgnoredCount);
                SetDiagnostic(
                    "N12-C ignored terminal agency reconstruction from stale load epoch " +
                    lease.Epoch.ToString(CultureInfo.InvariantCulture) + ".");
                return;
            }

            RestoreAfterAgencyReconstruction(lease.Epoch);
        }

        private static void RestoreAfterAgencyReconstruction(long restoreEpoch)
        {
            if (!IsImplemented)
            {
                SetDiagnostic("N12-C restore is not healthy; no pending ambient-scene jobs were rescheduled.");
                return;
            }

            mainScript main = GetMain();
            SaveManager.SavedData target = main == null ? null : main.GetSavedData();
            if (target == null)
            {
                Interlocked.Increment(ref associationMissingCount);
                SetDiagnostic("N12-C target SavedData is unavailable after agency reconstruction.");
                return;
            }

            if (!LoadEpoch.IsCurrent(restoreEpoch))
            {
                Interlocked.Increment(ref staleAgencyLoadTriggerIgnoredCount);
                SetDiagnostic("N12-C agency completion lost current-epoch ownership before restore admission.");
                return;
            }

            lock (Sync)
            {
                RestoreAttempt existing;
                if (AttemptedTargets.TryGetValue(target, out existing))
                {
                    Interlocked.Increment(ref duplicateAttemptSuppressedCount);
                    lastDiagnostic =
                        "N12-C suppressed a duplicate restore attempt for the same target SavedData at epoch " +
                        restoreEpoch.ToString(CultureInfo.InvariantCulture) + ".";
                    return;
                }

                AttemptedTargets.Add(target, new RestoreAttempt { Epoch = restoreEpoch });
            }

            RepairEnvelopeLoadState state;
            if (!RepairEnvelopeTransport.TryGetLoadState(target, out state))
            {
                Interlocked.Increment(ref associationMissingCount);
                SetDiagnostic("N12-C target SavedData has no repair-envelope read association.");
                return;
            }

            if (!state.Present)
            {
                Interlocked.Increment(ref legacySectionAbsentCount);
                SetDiagnostic("N12-C loaded a pre-envelope save; pending ambient-scene future work is unknown and was not fabricated.");
                return;
            }

            if (!state.Valid || state.Envelope == null || state.Envelope.records == null)
            {
                RecordInvalid("N12-C loaded a present but invalid repair envelope; pending ambient-scene state was not treated as empty.");
                return;
            }

            RepairEnvelopeRecordsV1 envelopeRecords = state.Envelope.records;
            if (envelopeRecords.pending_ambient_scenes_version == 0)
            {
                Interlocked.Increment(ref legacySectionAbsentCount);
                SetDiagnostic("N12-C loaded a pre-N12-B envelope; pending ambient-scene future work is unknown and was not fabricated.");
                return;
            }

            if (envelopeRecords.pending_ambient_scenes_version != PendingAmbientScenePersistence.SectionVersion ||
                envelopeRecords.pending_ambient_scenes == null)
            {
                RecordInvalid("N12-C repair section is invalid, unsupported, or missing its pending-job list.");
                return;
            }

            if (envelopeRecords.pending_ambient_scenes.Count == 0)
            {
                Interlocked.Increment(ref restoredLoadCount);
                SetDiagnostic("N12-C restored an authoritative empty pending ambient-scene section.");
                return;
            }

            if (!LoadEpoch.IsCurrent(restoreEpoch))
            {
                Interlocked.Increment(ref schedulingFailureCount);
                SetDiagnostic("N12-C load epoch changed before pending ambient-scene validation could begin.");
                return;
            }

            agency liveAgency = main.Data == null ? null : main.Data.GetComponent<agency>();
            Scenes scenes = main.Data == null ? null : main.Data.GetComponent<Scenes>();
            if (liveAgency == null || scenes == null || liveAgency.floors == null || data_girls.girl == null)
            {
                Interlocked.Increment(ref runtimeResolutionFailureCount);
                SetDiagnostic("N12-C required loaded agency/Scenes/idol runtime state is unavailable.");
                return;
            }

            FieldInfo scenesAgencyField = typeof(Scenes).GetField(
                "Agency",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (scenesAgencyField == null || !object.ReferenceEquals(scenesAgencyField.GetValue(scenes), liveAgency))
            {
                Interlocked.Increment(ref runtimeResolutionFailureCount);
                SetDiagnostic("N12-C Scenes component is not bound to the reconstructed agency instance.");
                return;
            }

            List<ValidatedJob> validated;
            string error;
            if (!TryValidateCompleteSection(target, envelopeRecords.pending_ambient_scenes, out validated, out error))
            {
                RecordInvalid(error);
                return;
            }

            List<ResolvedJob> plan;
            if (!TryResolveRuntimePlan(liveAgency, validated, out plan, out error))
            {
                Interlocked.Increment(ref runtimeResolutionFailureCount);
                SetDiagnostic(error);
                return;
            }

            MethodInfo factory = ResolveFactory();
            if (factory == null)
            {
                Interlocked.Increment(ref runtimeResolutionFailureCount);
                SetDiagnostic("N12-C could not resolve the audited Scenes.AssignSceneWithDelay factory.");
                return;
            }

            List<IEnumerator> created = new List<IEnumerator>();
            List<IEnumerator> started = new List<IEnumerator>();
            try
            {
                for (int i = 0; i < plan.Count; i++)
                {
                    if (!LoadEpoch.IsCurrent(restoreEpoch))
                    {
                        throw new InvalidOperationException("load epoch changed while N12-C was creating replacement iterators");
                    }

                    ResolvedJob job = plan[i];
                    object raw = factory.Invoke(
                        scenes,
                        new object[]
                        {
                            job.DueGameTime,
                            job.Room,
                            job.SceneType,
                            new List<data_girls.girls>(job.Girls)
                        });
                    IEnumerator iterator = raw as IEnumerator;
                    if (iterator == null)
                    {
                        throw new InvalidOperationException("AssignSceneWithDelay returned a null/non-IEnumerator replacement");
                    }

                    PendingAmbientSceneRepair.PendingAmbientSceneSnapshot registered;
                    if (!PendingAmbientSceneRepair.TryGetRegisteredSnapshot(iterator, out registered) ||
                        !SnapshotMatchesResolvedJob(registered, job, restoreEpoch))
                    {
                        throw new InvalidOperationException("N12-A did not register the exact restored semantic job produced by the audited factory");
                    }

                    created.Add(iterator);
                }

                if (!LoadEpoch.IsCurrent(restoreEpoch))
                {
                    throw new InvalidOperationException("load epoch changed before N12-C replacement coroutines were admitted");
                }

                for (int i = 0; i < created.Count; i++)
                {
                    scenes.StartCoroutine(created[i]);
                    started.Add(created[i]);
                }
            }
            catch (Exception exception)
            {
                // First revoke A08 execution permission for every replacement iterator.
                // StopCoroutine is then only a best-effort cleanup. If Unity throws while
                // stopping one, A08 still suppresses that iterator before its terminal room mutation.
                for (int i = 0; i < created.Count; i++)
                {
                    DeferredCarrierEpochRepair.CancelCarrier(
                        created[i],
                        "N12-D revoked a replacement pending ambient-scene carrier after transactional reschedule failure.");
                }

                for (int i = 0; i < started.Count; i++)
                {
                    try
                    {
                        scenes.StopCoroutine(started[i]);
                    }
                    catch (Exception)
                    {
                    }
                }

                for (int i = 0; i < created.Count; i++)
                {
                    PendingAmbientSceneRepair.CancelRegisteredJob(
                        created[i],
                        "N12-C rolled back a replacement iterator after transactional reschedule failure.");
                }

                Interlocked.Increment(ref schedulingFailureCount);
                SetDiagnostic(
                    "N12-C failed to reschedule the complete pending ambient-scene set; no replacement job remains N12-owned: " +
                    UnwrapMessage(exception));
                return;
            }

            Interlocked.Increment(ref restoredLoadCount);
            Interlocked.Add(ref rescheduledJobCount, started.Count);
            SetDiagnostic(
                "N12-C rebound and rescheduled " + started.Count.ToString(CultureInfo.InvariantCulture) +
                " exact pending ambient-scene job(s) at load epoch " +
                restoreEpoch.ToString(CultureInfo.InvariantCulture) + ".");
        }

        private static bool TryValidateCompleteSection(
            SaveManager.SavedData target,
            List<PendingAmbientSceneRecordV1> records,
            out List<ValidatedJob> validated,
            out string error)
        {
            validated = new List<ValidatedJob>();
            error = string.Empty;
            if (target == null || records == null || target.agency__Floors == null || target.data_girls__Girls == null)
            {
                error = "N12-C cannot validate a nonempty section without target floor/room and idol DTO lists.";
                return false;
            }

            Dictionary<int, int> savedGirlCounts = BuildSavedGirlCounts(target.data_girls__Girls);
            for (int i = 0; i < records.Count; i++)
            {
                PendingAmbientSceneRecordV1 record = records[i];
                if (record == null || string.IsNullOrEmpty(record.due_game_time) ||
                    record.room_ordinal < 0 || record.girl_ids == null ||
                    !Enum.IsDefined(typeof(agency._type), record.room_type) ||
                    !Enum.IsDefined(typeof(Scenes.type), record.scene_type))
                {
                    error = "N12-C pending ambient-scene section contains a structurally invalid record.";
                    return false;
                }

                DateTime dueGameTime;
                try
                {
                    dueGameTime = ExtensionMethods.ToDateTime(record.due_game_time);
                }
                catch (Exception exception)
                {
                    error = "N12-C pending ambient-scene due game time cannot be parsed: " + exception.Message;
                    return false;
                }
                if (!string.Equals(
                        ExtensionMethods.ToDataString(dueGameTime),
                        record.due_game_time,
                        StringComparison.Ordinal))
                {
                    error = "N12-C pending ambient-scene due game time is not in vanilla's canonical persisted form.";
                    return false;
                }

                agency.RoomData savedRoom;
                if (!TryResolveSavedRoom(
                        target,
                        record.floor_id,
                        record.room_ordinal,
                        record.room_type,
                        out savedRoom))
                {
                    error = "N12-C pending ambient-scene room locator does not resolve exactly in target SavedData.";
                    return false;
                }

                HashSet<int> perJobIds = new HashSet<int>();
                for (int g = 0; g < record.girl_ids.Count; g++)
                {
                    int girlId = record.girl_ids[g];
                    int savedCount;
                    if (girlId < 0 || !perJobIds.Add(girlId) ||
                        !savedGirlCounts.TryGetValue(girlId, out savedCount) || savedCount != 1)
                    {
                        error = "N12-C pending ambient-scene idol ID does not resolve exactly once in target SavedData.";
                        return false;
                    }
                }

                validated.Add(new ValidatedJob
                {
                    DueGameTime = dueGameTime,
                    FloorId = record.floor_id,
                    RoomOrdinal = record.room_ordinal,
                    RoomType = record.room_type,
                    SceneType = (Scenes.type)record.scene_type,
                    GirlIds = new List<int>(record.girl_ids)
                });
            }

            return true;
        }

        private static bool TryResolveRuntimePlan(
            agency liveAgency,
            List<ValidatedJob> validated,
            out List<ResolvedJob> plan,
            out string error)
        {
            plan = new List<ResolvedJob>();
            error = string.Empty;
            Dictionary<int, data_girls.girls> liveGirls;
            if (!TryBuildUniqueLiveGirls(out liveGirls, out error))
            {
                return false;
            }

            for (int i = 0; i < validated.Count; i++)
            {
                ValidatedJob job = validated[i];
                agency._room liveRoom;
                if (!TryResolveLiveRoom(
                        liveAgency,
                        job.FloorId,
                        job.RoomOrdinal,
                        job.RoomType,
                        out liveRoom))
                {
                    error = "N12-C pending ambient-scene room locator does not resolve exactly in reconstructed agency state.";
                    return false;
                }

                List<data_girls.girls> liveJobGirls = new List<data_girls.girls>();
                for (int g = 0; g < job.GirlIds.Count; g++)
                {
                    int girlId = job.GirlIds[g];
                    data_girls.girls liveGirl;
                    if (!liveGirls.TryGetValue(girlId, out liveGirl) || liveGirl == null ||
                        !object.ReferenceEquals(data_girls.GetGirlByID(girlId), liveGirl))
                    {
                        error = "N12-C pending ambient-scene idol ID does not resolve exactly once in reconstructed runtime state.";
                        return false;
                    }
                    liveJobGirls.Add(liveGirl);
                }

                plan.Add(new ResolvedJob
                {
                    DueGameTime = job.DueGameTime,
                    FloorId = job.FloorId,
                    RoomOrdinal = job.RoomOrdinal,
                    RoomType = job.RoomType,
                    SceneType = job.SceneType,
                    Room = liveRoom,
                    Girls = liveJobGirls
                });
            }

            return true;
        }

        private static Dictionary<int, int> BuildSavedGirlCounts(List<data_girls.GirlData> savedGirls)
        {
            Dictionary<int, int> counts = new Dictionary<int, int>();
            for (int i = 0; i < savedGirls.Count; i++)
            {
                data_girls.GirlData girl = savedGirls[i];
                if (girl == null || girl.id < 0)
                {
                    continue;
                }
                int count;
                counts.TryGetValue(girl.id, out count);
                counts[girl.id] = count + 1;
            }
            return counts;
        }

        private static bool TryBuildUniqueLiveGirls(
            out Dictionary<int, data_girls.girls> byId,
            out string error)
        {
            byId = new Dictionary<int, data_girls.girls>();
            error = string.Empty;
            for (int i = 0; i < data_girls.girl.Count; i++)
            {
                data_girls.girls girl = data_girls.girl[i];
                if (girl == null || girl.id < 0 || byId.ContainsKey(girl.id))
                {
                    error = "N12-C reconstructed idol list contains a null/negative/duplicate canonical ID.";
                    return false;
                }
                byId.Add(girl.id, girl);
            }
            return true;
        }

        private static bool TryResolveSavedRoom(
            SaveManager.SavedData target,
            int floorId,
            int roomOrdinal,
            int roomType,
            out agency.RoomData room)
        {
            room = null;
            int matches = 0;
            for (int f = 0; f < target.agency__Floors.Count; f++)
            {
                agency.FloorData floor = target.agency__Floors[f];
                if (floor == null || floor.FloorID != floorId)
                {
                    continue;
                }
                if (floor.Rooms == null || roomOrdinal >= floor.Rooms.Count)
                {
                    return false;
                }
                room = floor.Rooms[roomOrdinal];
                matches++;
            }
            return matches == 1 && room != null && (int)room.Type == roomType;
        }

        private static bool TryResolveLiveRoom(
            agency liveAgency,
            int floorId,
            int roomOrdinal,
            int roomType,
            out agency._room room)
        {
            room = null;
            int matches = 0;
            for (int f = 0; f < liveAgency.floors.Count; f++)
            {
                agency._floor floor = liveAgency.floors[f];
                if (floor == null || floor.FloorID != floorId)
                {
                    continue;
                }
                if (floor.floor == null || roomOrdinal >= floor.floor.Count)
                {
                    return false;
                }
                room = floor.floor[roomOrdinal];
                matches++;
            }
            return matches == 1 && room != null && (int)room.type == roomType;
        }

        private static MethodInfo ResolveFactory()
        {
            MethodInfo factory = typeof(Scenes).GetMethod(
                "AssignSceneWithDelay",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(DateTime),
                    typeof(agency._room),
                    typeof(Scenes.type),
                    typeof(List<data_girls.girls>)
                },
                null);
            return factory != null && typeof(IEnumerator).IsAssignableFrom(factory.ReturnType)
                ? factory
                : null;
        }

        private static bool SnapshotMatchesResolvedJob(
            PendingAmbientSceneRepair.PendingAmbientSceneSnapshot snapshot,
            ResolvedJob job,
            long restoreEpoch)
        {
            if (snapshot == null || job == null || snapshot.Epoch != restoreEpoch ||
                snapshot.DueGameTime != job.DueGameTime || snapshot.FloorId != job.FloorId ||
                snapshot.RoomOrdinal != job.RoomOrdinal || snapshot.RoomType != job.RoomType ||
                snapshot.SceneType != (int)job.SceneType || snapshot.GirlIds == null ||
                snapshot.GirlIds.Count != job.Girls.Count)
            {
                return false;
            }

            for (int i = 0; i < snapshot.GirlIds.Count; i++)
            {
                if (job.Girls[i] == null || snapshot.GirlIds[i] != job.Girls[i].id)
                {
                    return false;
                }
            }
            return true;
        }

        private static mainScript GetMain()
        {
            try
            {
                return Camera.main == null ? null : Camera.main.GetComponent<mainScript>();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string UnwrapMessage(Exception exception)
        {
            TargetInvocationException invocation = exception as TargetInvocationException;
            Exception actual = invocation != null && invocation.InnerException != null
                ? invocation.InnerException
                : exception;
            return actual == null ? "<unknown exception>" : actual.Message;
        }

        private static void RecordInvalid(string diagnostic)
        {
            Interlocked.Increment(ref invalidSectionCount);
            SetDiagnostic(diagnostic);
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

    internal static class PendingAmbientSceneRestorePatchHealth
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
                    return ResolvedTargets.Count == ExpectedTargetMethodCount && string.IsNullOrEmpty(failure);
                }
            }
        }

        internal static void ReportResolved(MethodBase method)
        {
            if (method == null)
            {
                ReportFailure("<null>", "resolved target was null");
                return;
            }
            lock (Sync)
            {
                string owner = method.DeclaringType == null ? "<unknown>" : method.DeclaringType.FullName;
                ResolvedTargets.Add(owner + "." + method.Name);
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
                "N12-C pending ambient-scene restore patch failed at " + failure);
        }
    }
}
