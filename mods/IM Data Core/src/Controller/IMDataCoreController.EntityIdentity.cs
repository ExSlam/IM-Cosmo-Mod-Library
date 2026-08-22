using System;
using System.Collections.Generic;
using UnityEngine;

namespace IMDataCore
{
    /// <summary>
    /// Owns durable history identities for vanilla entities whose runtime IDs are
    /// either not serialized (agency rooms) or recyclable (theaters/cafes).
    ///
    /// Raw vanilla IDs remain in payloads. Timeline EntityId uses an IMDC-owned
    /// room-generation ID so historical grouping never relies on those ephemeral IDs.
    /// Theater/cafe identities intentionally reuse the owning room generation within
    /// their own EntityKind namespace.
    /// </summary>
    internal sealed partial class IMDataCoreController
    {
        private const string AgencyRoomGenerationPrefix = "g:";

        private readonly Dictionary<agency._room, string> agencyRoomEntityIdByReference =
            new Dictionary<agency._room, string>();

        // Vanilla agency loading reconstructs rooms asynchronously after LoadEvent.
        // Keep the checkpoint-ordered identity queue alive until the private room
        // deserializer has consumed every saved room row.
        private List<LightweightAgencyRoomIdentityRecord> pendingLoadedAgencyRoomIdentities;
        private int pendingLoadedAgencyRoomIdentityIndex;

        private static string CreateAgencyRoomGenerationId()
        {
            return AgencyRoomGenerationPrefix + Guid.NewGuid().ToString("N");
        }

        internal string ResolveAgencyRoomHistoryEntityId(agency._room room)
        {
            if (room == null)
            {
                return string.Empty;
            }

            lock (runtimeLock)
            {
                return GetOrCreateAgencyRoomHistoryEntityIdLocked(room);
            }
        }

        private string GetOrCreateAgencyRoomHistoryEntityIdLocked(agency._room room)
        {
            if (room == null)
            {
                return string.Empty;
            }

            string entityId;
            if (agencyRoomEntityIdByReference.TryGetValue(room, out entityId) &&
                !string.IsNullOrEmpty(entityId))
            {
                return entityId;
            }

            entityId = CreateAgencyRoomGenerationId();
            agencyRoomEntityIdByReference[room] = entityId;
            return entityId;
        }

        internal string ResolveTheaterHistoryEntityId(
            Theaters._theater theater,
            agency._room room)
        {
            agency._room resolvedRoom = room;
            if (resolvedRoom == null && theater != null)
            {
                resolvedRoom = theater.GetRoom();
            }

            return ResolveAgencyRoomHistoryEntityId(resolvedRoom);
        }

        internal string ResolveCafeHistoryEntityId(
            Cafes._cafe cafe,
            agency._room room)
        {
            agency._room resolvedRoom = room;
            if (resolvedRoom == null && cafe != null)
            {
                resolvedRoom = ResolveCafeRoom(cafe.ID);
            }

            return ResolveAgencyRoomHistoryEntityId(resolvedRoom);
        }

        internal void ForgetAgencyRoomHistoryEntityId(agency._room room)
        {
            if (room == null)
            {
                return;
            }

            lock (runtimeLock)
            {
                agencyRoomEntityIdByReference.Remove(room);
            }
        }

        /// <summary>
        /// Freezes the room-generation map into the exact vanilla checkpoint. The
        /// traversal mirrors agency.SaveFunction: floors in list order, then rooms
        /// in each floor's list order. This is the same order consumed by
        /// GetRoomDataForLoading on restore.
        /// </summary>
        internal List<LightweightAgencyRoomIdentityRecord>
            CaptureAgencyRoomIdentitySnapshotForCheckpoint(
                SaveManager.SavedData savedData)
        {
            lock (runtimeLock)
            {
                return CaptureAgencyRoomIdentitySnapshotForCheckpointLocked(
                    savedData);
            }
        }

        private List<LightweightAgencyRoomIdentityRecord>
            CaptureAgencyRoomIdentitySnapshotForCheckpointLocked(
                SaveManager.SavedData savedData)
        {
            List<LightweightAgencyRoomIdentityRecord> result =
                new List<LightweightAgencyRoomIdentityRecord>();
            if (savedData == null || savedData.agency__Floors == null)
            {
                return result;
            }

            List<agency._room> liveRooms = null;
            agency agencySystem = ResolveAgencySystemForIdentity();
            if (agencySystem != null)
            {
                liveRooms = agencySystem.allRooms(true, true);
            }

            int flatRoomIndex = 0;
            for (int floorIndex = 0;
                floorIndex < savedData.agency__Floors.Count;
                floorIndex++)
            {
                agency.FloorData floorData = savedData.agency__Floors[floorIndex];
                if (floorData == null || floorData.Rooms == null)
                {
                    continue;
                }

                for (int roomIndex = 0;
                    roomIndex < floorData.Rooms.Count;
                    roomIndex++)
                {
                    agency.RoomData roomData = floorData.Rooms[roomIndex];
                    agency._room liveRoom = liveRooms != null &&
                        flatRoomIndex < liveRooms.Count
                            ? liveRooms[flatRoomIndex]
                            : null;

                    string entityId;
                    if (liveRoom != null &&
                        roomData != null &&
                        (int)liveRoom.type == (int)roomData.Type)
                    {
                        entityId = GetOrCreateAgencyRoomHistoryEntityIdLocked(
                            liveRoom);
                    }
                    else
                    {
                        // A structural mismatch is fail-safe: do not bind an
                        // identity to the wrong room. The saved row receives a new
                        // generation which will be associated on the next load.
                        entityId = CreateAgencyRoomGenerationId();
                    }

                    result.Add(new LightweightAgencyRoomIdentityRecord
                    {
                        EntityId = entityId,
                        FloorIndex = floorIndex,
                        RoomIndex = roomIndex,
                        RoomTypeRaw = roomData != null
                            ? (int)roomData.Type
                            : CoreConstants.InvalidIdValue,
                        TheaterId = roomData != null
                            ? roomData.TheaterID
                            : CoreConstants.InvalidIdValue
                    });
                    flatRoomIndex++;
                }
            }

            return result;
        }

        internal static List<LightweightAgencyRoomIdentityRecord>
            CreateFreshAgencyRoomIdentitySnapshot(
                SaveManager.SavedData savedData)
        {
            List<LightweightAgencyRoomIdentityRecord> result =
                new List<LightweightAgencyRoomIdentityRecord>();
            if (savedData == null || savedData.agency__Floors == null)
            {
                return result;
            }

            for (int floorIndex = 0;
                floorIndex < savedData.agency__Floors.Count;
                floorIndex++)
            {
                agency.FloorData floorData = savedData.agency__Floors[floorIndex];
                if (floorData == null || floorData.Rooms == null)
                {
                    continue;
                }

                for (int roomIndex = 0;
                    roomIndex < floorData.Rooms.Count;
                    roomIndex++)
                {
                    agency.RoomData roomData = floorData.Rooms[roomIndex];
                    result.Add(new LightweightAgencyRoomIdentityRecord
                    {
                        EntityId = CreateAgencyRoomGenerationId(),
                        FloorIndex = floorIndex,
                        RoomIndex = roomIndex,
                        RoomTypeRaw = roomData != null
                            ? (int)roomData.Type
                            : CoreConstants.InvalidIdValue,
                        TheaterId = roomData != null
                            ? roomData.TheaterID
                            : CoreConstants.InvalidIdValue
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Arms the room loader with the exact checkpoint room-generation snapshot.
        /// A present but inconsistent snapshot fails safe by assigning fresh
        /// generations rather than binding history to the wrong rooms.
        /// </summary>
        internal void PrepareAgencyRoomIdentitiesForLoad(
            SaveManager.SavedData loadedSaveData,
            bool exactCheckpointSelected,
            IReadOnlyList<LightweightAgencyRoomIdentityRecord> checkpointSnapshot)
        {
            lock (runtimeLock)
            {
                List<LightweightAgencyRoomIdentityRecord> prepared;
                if (exactCheckpointSelected &&
                    IsAgencyRoomIdentitySnapshotCompatible(
                        loadedSaveData,
                        checkpointSnapshot))
                {
                    prepared = CloneAgencyRoomIdentitySnapshot(checkpointSnapshot);
                }
                else
                {
                    prepared = CreateFreshAgencyRoomIdentitySnapshot(loadedSaveData);
                    if (exactCheckpointSelected && prepared.Count > 0)
                    {
                        CoreLog.Warn(
                            "The exact IMDC checkpoint room-identity snapshot did not " +
                            "match the vanilla room layout. IMDC assigned fresh " +
                            "forward-safe room generations rather than binding history " +
                            "to the wrong rooms.");
                    }
                }

                pendingLoadedAgencyRoomIdentities = prepared;
                pendingLoadedAgencyRoomIdentityIndex = 0;
            }
        }

        internal void AssociateLoadedAgencyRoom(
            agency.RoomData roomData,
            agency._room loadedRoom)
        {
            if (loadedRoom == null)
            {
                return;
            }

            lock (runtimeLock)
            {
                string entityId = string.Empty;
                if (pendingLoadedAgencyRoomIdentities != null &&
                    pendingLoadedAgencyRoomIdentityIndex <
                        pendingLoadedAgencyRoomIdentities.Count)
                {
                    LightweightAgencyRoomIdentityRecord record =
                        pendingLoadedAgencyRoomIdentities[
                            pendingLoadedAgencyRoomIdentityIndex];
                    pendingLoadedAgencyRoomIdentityIndex++;

                    if (record != null &&
                        !string.IsNullOrEmpty(record.EntityId) &&
                        roomData != null &&
                        record.RoomTypeRaw == (int)roomData.Type)
                    {
                        entityId = record.EntityId;
                    }
                }

                if (string.IsNullOrEmpty(entityId))
                {
                    entityId = CreateAgencyRoomGenerationId();
                }

                agencyRoomEntityIdByReference[loadedRoom] = entityId;

                if (pendingLoadedAgencyRoomIdentities != null &&
                    pendingLoadedAgencyRoomIdentityIndex >=
                        pendingLoadedAgencyRoomIdentities.Count)
                {
                    pendingLoadedAgencyRoomIdentities = null;
                    pendingLoadedAgencyRoomIdentityIndex = 0;
                }
            }
        }

        private static bool IsAgencyRoomIdentitySnapshotCompatible(
            SaveManager.SavedData savedData,
            IReadOnlyList<LightweightAgencyRoomIdentityRecord> snapshot)
        {
            if (savedData == null || savedData.agency__Floors == null || snapshot == null)
            {
                return false;
            }

            int expectedCount = 0;
            for (int floorIndex = 0; floorIndex < savedData.agency__Floors.Count; floorIndex++)
            {
                agency.FloorData floorData = savedData.agency__Floors[floorIndex];
                if (floorData == null || floorData.Rooms == null)
                {
                    continue;
                }

                for (int roomIndex = 0; roomIndex < floorData.Rooms.Count; roomIndex++)
                {
                    if (expectedCount >= snapshot.Count)
                    {
                        return false;
                    }

                    agency.RoomData roomData = floorData.Rooms[roomIndex];
                    LightweightAgencyRoomIdentityRecord record = snapshot[expectedCount];
                    if (record == null ||
                        string.IsNullOrEmpty(record.EntityId) ||
                        record.FloorIndex != floorIndex ||
                        record.RoomIndex != roomIndex ||
                        roomData == null ||
                        record.RoomTypeRaw != (int)roomData.Type)
                    {
                        return false;
                    }

                    // TheaterID is the saved bridge for theater/cafe rooms and is
                    // useful mismatch detection there. For other room types it is
                    // not historical identity and does not need to participate.
                    if ((roomData.Type == agency._type.theatre ||
                         roomData.Type == agency._type.cafeAndShop) &&
                        record.TheaterId != roomData.TheaterID)
                    {
                        return false;
                    }

                    expectedCount++;
                }
            }

            return expectedCount == snapshot.Count;
        }

        private static List<LightweightAgencyRoomIdentityRecord>
            CloneAgencyRoomIdentitySnapshot(
                IReadOnlyList<LightweightAgencyRoomIdentityRecord> source)
        {
            List<LightweightAgencyRoomIdentityRecord> result =
                new List<LightweightAgencyRoomIdentityRecord>();
            if (source == null)
            {
                return result;
            }

            for (int index = 0; index < source.Count; index++)
            {
                LightweightAgencyRoomIdentityRecord record = source[index];
                if (record == null)
                {
                    continue;
                }

                result.Add(new LightweightAgencyRoomIdentityRecord
                {
                    EntityId = record.EntityId ?? string.Empty,
                    FloorIndex = record.FloorIndex,
                    RoomIndex = record.RoomIndex,
                    RoomTypeRaw = record.RoomTypeRaw,
                    TheaterId = record.TheaterId
                });
            }
            return result;
        }

        private static agency ResolveAgencySystemForIdentity()
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

            return main.Data.GetComponent<agency>();
        }

        private void ResetEntityIdentityRuntimeStateLocked()
        {
            agencyRoomEntityIdByReference.Clear();
            pendingLoadedAgencyRoomIdentities = null;
            pendingLoadedAgencyRoomIdentityIndex = 0;
        }

        private void CancelPendingAgencyRoomIdentityLoadLocked()
        {
            pendingLoadedAgencyRoomIdentities = null;
            pendingLoadedAgencyRoomIdentityIndex = 0;
        }
    }
}
