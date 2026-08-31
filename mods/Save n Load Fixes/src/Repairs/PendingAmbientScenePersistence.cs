using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using SaveNLoadFixes.Persistence;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// N12-B / finding #13 persistence half: freeze the detached current-epoch
    /// semantic jobs captured by N12-A into the exact caller-thread SavedData
    /// repair envelope. This class does not restore or reschedule jobs; N12-C owns
    /// target-save validation/rebinding and fresh coroutine creation after load.
    /// </summary>
    internal static class PendingAmbientScenePersistence
    {
        internal const int SectionVersion = 1;

        private static readonly object Sync = new object();
        private static long capturedCheckpointCount;
        private static long capturedJobCount;
        private static long captureFailureCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented
        {
            get { return PendingAmbientSceneRepair.IsImplemented; }
        }

        internal static long CapturedCheckpointCount
        {
            get { return Interlocked.Read(ref capturedCheckpointCount); }
        }

        internal static long CapturedJobCount
        {
            get { return Interlocked.Read(ref capturedJobCount); }
        }

        internal static long CaptureFailureCount
        {
            get { return Interlocked.Read(ref captureFailureCount); }
        }

        internal static string LastDiagnostic
        {
            get { lock (Sync) { return lastDiagnostic; } }
        }

        internal static bool TryCaptureForEnvelope(
            SaveManager.SavedData dataToSave,
            out List<PendingAmbientSceneRecordV1> records,
            out string error)
        {
            records = new List<PendingAmbientSceneRecordV1>();
            error = string.Empty;

            if (dataToSave == null)
            {
                return CaptureFailed(
                    "N12-B cannot capture pending ambient scenes without the exact SavedData request.",
                    out error);
            }

            List<PendingAmbientSceneRepair.PendingAmbientSceneSnapshot> snapshots;
            if (!PendingAmbientSceneRepair.TrySnapshotCurrentJobs(out snapshots, out error))
            {
                return CaptureFailed(
                    "N12-B could not obtain the current-epoch N12-A semantic snapshot: " + error,
                    out error);
            }

            // An empty list is authoritative and must still be section-marked in the
            // envelope. It means this exact save request had no pending ambient jobs.
            if (snapshots.Count == 0)
            {
                Interlocked.Increment(ref capturedCheckpointCount);
                SetDiagnostic("N12-B captured an authoritative empty pending ambient-scene section.");
                return true;
            }

            if (dataToSave.agency__Floors == null)
            {
                return CaptureFailed(
                    "N12-B cannot validate pending ambient-scene room locators because SavedData.agency__Floors is null.",
                    out error);
            }
            if (dataToSave.data_girls__Girls == null)
            {
                return CaptureFailed(
                    "N12-B cannot validate pending ambient-scene idol IDs because SavedData.data_girls__Girls is null.",
                    out error);
            }

            Dictionary<int, int> savedGirlIdCounts = BuildSavedGirlIdCounts(dataToSave.data_girls__Girls);

            for (int index = 0; index < snapshots.Count; index++)
            {
                PendingAmbientSceneRepair.PendingAmbientSceneSnapshot snapshot = snapshots[index];
                if (snapshot == null)
                {
                    return CaptureFailed("N12-B received a null N12-A semantic job snapshot.", out error);
                }

                if (!Enum.IsDefined(typeof(Scenes.type), snapshot.SceneType))
                {
                    return CaptureFailed("N12-B pending ambient-scene snapshot has an undefined scene type.", out error);
                }
                if (!Enum.IsDefined(typeof(agency._type), snapshot.RoomType))
                {
                    return CaptureFailed("N12-B pending ambient-scene snapshot has an undefined room type.", out error);
                }
                if (snapshot.RoomOrdinal < 0)
                {
                    return CaptureFailed("N12-B pending ambient-scene snapshot has a negative room ordinal.", out error);
                }
                if (snapshot.GirlIds == null)
                {
                    return CaptureFailed("N12-B pending ambient-scene snapshot has a null idol-ID list.", out error);
                }

                agency.RoomData savedRoom;
                if (!TryResolveSavedRoom(
                        dataToSave,
                        snapshot.FloorId,
                        snapshot.RoomOrdinal,
                        snapshot.RoomType,
                        out savedRoom,
                        out error))
                {
                    return CaptureFailed(error, out error);
                }

                HashSet<int> perJobGirlIds = new HashSet<int>();
                for (int g = 0; g < snapshot.GirlIds.Count; g++)
                {
                    int girlId = snapshot.GirlIds[g];
                    int savedCount;
                    if (girlId < 0 || !perJobGirlIds.Add(girlId) ||
                        !savedGirlIdCounts.TryGetValue(girlId, out savedCount) || savedCount != 1)
                    {
                        return CaptureFailed(
                            "N12-B pending ambient-scene idol ID does not resolve exactly once in the target SavedData girl list.",
                            out error);
                    }
                }

                string dueGameTime = ExtensionMethods.ToDataString(snapshot.DueGameTime);
                DateTime dueRoundTrip;
                try
                {
                    dueRoundTrip = ExtensionMethods.ToDateTime(dueGameTime);
                }
                catch (Exception exception)
                {
                    return CaptureFailed(
                        "N12-B could not encode the pending ambient-scene due game time: " + exception.Message,
                        out error);
                }
                if (dueRoundTrip != snapshot.DueGameTime)
                {
                    return CaptureFailed(
                        "N12-B pending ambient-scene due game time is outside vanilla's exact persisted second precision.",
                        out error);
                }

                records.Add(new PendingAmbientSceneRecordV1
                {
                    due_game_time = dueGameTime,
                    floor_id = snapshot.FloorId,
                    room_ordinal = snapshot.RoomOrdinal,
                    room_type = snapshot.RoomType,
                    scene_type = snapshot.SceneType,
                    girl_ids = new List<int>(snapshot.GirlIds)
                });
            }

            Interlocked.Increment(ref capturedCheckpointCount);
            Interlocked.Add(ref capturedJobCount, records.Count);
            SetDiagnostic(
                "N12-B captured " + records.Count.ToString(CultureInfo.InvariantCulture) +
                " pending ambient-scene job(s) into the caller-thread repair envelope.");
            return true;
        }

        private static Dictionary<int, int> BuildSavedGirlIdCounts(List<data_girls.GirlData> savedGirls)
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

        private static bool TryResolveSavedRoom(
            SaveManager.SavedData target,
            int floorId,
            int roomOrdinal,
            int roomType,
            out agency.RoomData room,
            out string error)
        {
            room = null;
            error = string.Empty;
            int floorMatches = 0;

            for (int f = 0; f < target.agency__Floors.Count; f++)
            {
                agency.FloorData floor = target.agency__Floors[f];
                if (floor == null || floor.FloorID != floorId)
                {
                    continue;
                }

                floorMatches++;
                if (floor.Rooms == null || roomOrdinal >= floor.Rooms.Count)
                {
                    error = "N12-B pending ambient-scene room locator falls outside the target SavedData floor structure.";
                    return false;
                }

                room = floor.Rooms[roomOrdinal];
            }

            if (floorMatches != 1 || room == null || (int)room.Type != roomType)
            {
                error = "N12-B pending ambient-scene room locator does not resolve exactly to the saved FloorID + ordinal + room-type witness.";
                return false;
            }

            return true;
        }

        private static bool CaptureFailed(string diagnostic, out string error)
        {
            error = diagnostic ?? "unknown N12-B capture failure";
            Interlocked.Increment(ref captureFailureCount);
            SetDiagnostic(error);
            return false;
        }

        private static void SetDiagnostic(string diagnostic)
        {
            lock (Sync)
            {
                lastDiagnostic = diagnostic ?? string.Empty;
            }
        }
    }
}
