using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using SaveNLoadFixes.Persistence;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    internal static class BusinessRemainingMinutesRepair
    {
        internal const int SectionVersion = 1;

        private sealed class ValidatedLoadState
        {
            internal bool Legacy;
            internal bool Valid;
            internal string Error = string.Empty;
            internal readonly Dictionary<string, float> MinutesByRoom =
                new Dictionary<string, float>(StringComparer.Ordinal);
        }

        private static readonly object Sync = new object();
        private static readonly ConditionalWeakTable<SaveManager.SavedData, ValidatedLoadState> Validated =
            new ConditionalWeakTable<SaveManager.SavedData, ValidatedLoadState>();

        private static long restoredLoadCount;
        private static long restoredRoomCount;
        private static long legacySectionAbsentCount;
        private static long invalidSectionCount;
        private static long associationMissingCount;
        private static long captureFailureCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented { get { return BusinessRemainingMinutesPatchHealth.IsHealthy; } }
        internal static long RestoredLoadCount { get { return Interlocked.Read(ref restoredLoadCount); } }
        internal static long RestoredRoomCount { get { return Interlocked.Read(ref restoredRoomCount); } }
        internal static long LegacySectionAbsentCount { get { return Interlocked.Read(ref legacySectionAbsentCount); } }
        internal static long InvalidSectionCount { get { return Interlocked.Read(ref invalidSectionCount); } }
        internal static long AssociationMissingCount { get { return Interlocked.Read(ref associationMissingCount); } }
        internal static long CaptureFailureCount { get { return Interlocked.Read(ref captureFailureCount); } }
        internal static string LastDiagnostic { get { lock (Sync) { return lastDiagnostic; } } }

        internal static bool TryCaptureForEnvelope(
            SaveManager.SavedData dataToSave,
            out List<BusinessRemainingMinutesRecordV1> records,
            out string error)
        {
            records = new List<BusinessRemainingMinutesRecordV1>();
            error = string.Empty;
            if (dataToSave == null || dataToSave.agency__Floors == null)
            {
                return CaptureFailed("N10 target SavedData agency floors are unavailable.", out error);
            }

            agency liveAgency = GetAgency();
            if (liveAgency == null || liveAgency.floors == null)
            {
                return CaptureFailed("N10 live agency/floor state is unavailable.", out error);
            }

            if (liveAgency.floors.Count != dataToSave.agency__Floors.Count)
            {
                return CaptureFailed("N10 serialized/live floor counts do not match exactly.", out error);
            }

            HashSet<int> floorIds = new HashSet<int>();
            HashSet<string> locators = new HashSet<string>(StringComparer.Ordinal);
            for (int floorIndex = 0; floorIndex < dataToSave.agency__Floors.Count; floorIndex++)
            {
                agency.FloorData savedFloor = dataToSave.agency__Floors[floorIndex];
                agency._floor liveFloor = liveAgency.floors[floorIndex];
                if (savedFloor == null || liveFloor == null || savedFloor.Rooms == null || liveFloor.floor == null ||
                    savedFloor.FloorID != liveFloor.FloorID || !floorIds.Add(savedFloor.FloorID) ||
                    savedFloor.Rooms.Count != liveFloor.floor.Count)
                {
                    return CaptureFailed("N10 serialized/live floor structure is not an exact ordered match.", out error);
                }

                for (int roomOrdinal = 0; roomOrdinal < savedFloor.Rooms.Count; roomOrdinal++)
                {
                    agency.RoomData savedRoom = savedFloor.Rooms[roomOrdinal];
                    agency._room liveRoom = liveFloor.floor[roomOrdinal];
                    if (savedRoom == null || liveRoom == null || savedRoom.Type != liveRoom.type ||
                        savedRoom.status != liveRoom.status)
                    {
                        return CaptureFailed("N10 serialized/live room structure is not an exact type/status match.", out error);
                    }

                    float minutes = liveRoom.business_minutes_before_finish;
                    if (minutes == 0f)
                    {
                        continue;
                    }

                    if (!IsFinite(minutes) || savedRoom.status == agency._room._status.business ||
                        savedRoom.businessType < 0 || liveRoom.businessType == null ||
                        (int)liveRoom.businessType.Value != savedRoom.businessType)
                    {
                        return CaptureFailed("N10 paused business remainder does not match the audited hidden-duration state.", out error);
                    }

                    string locator = BuildLocator(savedFloor.FloorID, roomOrdinal, savedRoom.Type);
                    if (!locators.Add(locator))
                    {
                        return CaptureFailed("N10 duplicate structural room locator detected during capture.", out error);
                    }

                    records.Add(new BusinessRemainingMinutesRecordV1
                    {
                        floor_id = savedFloor.FloorID,
                        room_ordinal = roomOrdinal,
                        room_type = (int)savedRoom.Type,
                        minutes_before_finish = minutes
                    });
                }
            }

            records.Sort(delegate(BusinessRemainingMinutesRecordV1 left, BusinessRemainingMinutesRecordV1 right)
            {
                int floor = left.floor_id.CompareTo(right.floor_id);
                return floor != 0 ? floor : left.room_ordinal.CompareTo(right.room_ordinal);
            });
            return true;
        }

        internal static void RestoreForRoom(agency.RoomData savedRoom, agency._room loadedRoom)
        {
            if (savedRoom == null || loadedRoom == null)
            {
                return;
            }

            SaveManager.SavedData target = GetTargetSavedData();
            if (target == null)
            {
                Interlocked.Increment(ref associationMissingCount);
                SetDiagnostic("N10 target SavedData is unavailable during room reconstruction.");
                return;
            }

            ValidatedLoadState validated = GetOrValidate(target);
            if (validated == null || !validated.Valid || validated.Legacy)
            {
                return;
            }

            int floorId;
            int roomOrdinal;
            if (!TryLocateSavedRoom(target, savedRoom, out floorId, out roomOrdinal))
            {
                RecordInvalid("N10 could not map reconstructed RoomData to one structural saved-room locator.");
                return;
            }

            string locator = BuildLocator(floorId, roomOrdinal, savedRoom.Type);
            float minutes;
            if (!validated.MinutesByRoom.TryGetValue(locator, out minutes))
            {
                return;
            }

            if (loadedRoom.business_minutes_before_finish != 0f || savedRoom.status == agency._room._status.business ||
                loadedRoom.status != savedRoom.status || savedRoom.businessType < 0 || loadedRoom.businessType == null ||
                (int)loadedRoom.businessType.Value != savedRoom.businessType)
            {
                RecordInvalid("N10 reconstructed room no longer matches the audited paused-business shape.");
                return;
            }

            loadedRoom.business_minutes_before_finish = minutes;
            Interlocked.Increment(ref restoredRoomCount);
            SetDiagnostic("N10 restored paused business remaining minutes for one reconstructed room.");
        }

        private static ValidatedLoadState GetOrValidate(SaveManager.SavedData target)
        {
            lock (Sync)
            {
                ValidatedLoadState existing;
                if (Validated.TryGetValue(target, out existing))
                {
                    return existing;
                }

                ValidatedLoadState created = Validate(target);
                Validated.Add(target, created);
                if (created.Valid && !created.Legacy)
                {
                    Interlocked.Increment(ref restoredLoadCount);
                }
                return created;
            }
        }

        private static ValidatedLoadState Validate(SaveManager.SavedData target)
        {
            ValidatedLoadState result = new ValidatedLoadState();
            RepairEnvelopeLoadState state;
            if (!RepairEnvelopeTransport.TryGetLoadState(target, out state))
            {
                Interlocked.Increment(ref associationMissingCount);
                result.Error = "N10 adopted SavedData has no repair-envelope read association.";
                SetDiagnostic(result.Error);
                return result;
            }

            if (!state.Present || state.Envelope == null || state.Envelope.records == null ||
                state.Envelope.records.business_remaining_minutes_version == 0)
            {
                result.Valid = true;
                result.Legacy = true;
                Interlocked.Increment(ref legacySectionAbsentCount);
                SetDiagnostic("N10 loaded a pre-N10 save; no hidden business remainder was fabricated.");
                return result;
            }

            RepairEnvelopeRecordsV1 records = state.Envelope.records;
            if (!state.Valid || records.business_remaining_minutes_version != SectionVersion ||
                records.business_remaining_minutes == null || target.agency__Floors == null)
            {
                result.Error = "N10 repair section is invalid or unsupported.";
                RecordInvalid(result.Error);
                return result;
            }

            HashSet<string> locators = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < records.business_remaining_minutes.Count; i++)
            {
                BusinessRemainingMinutesRecordV1 record = records.business_remaining_minutes[i];
                if (record == null || record.room_ordinal < 0 || record.minutes_before_finish == 0f ||
                    !IsFinite(record.minutes_before_finish) ||
                    record.room_type < (int)agency._type.yourOffice ||
                    record.room_type > (int)agency._type.recreation_room)
                {
                    result.Error = "N10 repair section contains an invalid record.";
                    RecordInvalid(result.Error);
                    return result;
                }

                agency.RoomData saved;
                if (!TryGetSavedRoom(target, record.floor_id, record.room_ordinal, out saved) ||
                    saved == null || (int)saved.Type != record.room_type ||
                    saved.status == agency._room._status.business || saved.businessType < 0)
                {
                    result.Error = "N10 record does not resolve to the exact saved paused-business room structure.";
                    RecordInvalid(result.Error);
                    return result;
                }

                string locator = BuildLocator(record.floor_id, record.room_ordinal, saved.Type);
                if (!locators.Add(locator))
                {
                    result.Error = "N10 repair section contains a duplicate room locator.";
                    RecordInvalid(result.Error);
                    return result;
                }

                result.MinutesByRoom.Add(locator, record.minutes_before_finish);
            }

            result.Valid = true;
            return result;
        }

        private static bool TryLocateSavedRoom(
            SaveManager.SavedData target,
            agency.RoomData room,
            out int floorId,
            out int ordinal)
        {
            floorId = -1;
            ordinal = -1;
            if (target == null || target.agency__Floors == null)
            {
                return false;
            }

            int matches = 0;
            for (int f = 0; f < target.agency__Floors.Count; f++)
            {
                agency.FloorData floor = target.agency__Floors[f];
                if (floor == null || floor.Rooms == null)
                {
                    continue;
                }

                for (int r = 0; r < floor.Rooms.Count; r++)
                {
                    if (object.ReferenceEquals(floor.Rooms[r], room))
                    {
                        floorId = floor.FloorID;
                        ordinal = r;
                        matches++;
                    }
                }
            }
            return matches == 1;
        }

        private static bool TryGetSavedRoom(
            SaveManager.SavedData target,
            int floorId,
            int ordinal,
            out agency.RoomData room)
        {
            room = null;
            if (target == null || target.agency__Floors == null || ordinal < 0)
            {
                return false;
            }

            int matches = 0;
            for (int f = 0; f < target.agency__Floors.Count; f++)
            {
                agency.FloorData floor = target.agency__Floors[f];
                if (floor != null && floor.FloorID == floorId && floor.Rooms != null && ordinal < floor.Rooms.Count)
                {
                    room = floor.Rooms[ordinal];
                    matches++;
                }
            }
            return matches == 1;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static string BuildLocator(int floorId, int roomOrdinal, agency._type roomType)
        {
            return floorId.ToString() + ":" + roomOrdinal.ToString() + ":" + ((int)roomType).ToString();
        }

        private static agency GetAgency()
        {
            try
            {
                if (Camera.main == null)
                {
                    return null;
                }
                mainScript main = Camera.main.GetComponent<mainScript>();
                return main == null || main.Data == null ? null : main.Data.GetComponent<agency>();
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
                if (Camera.main == null)
                {
                    return null;
                }
                mainScript main = Camera.main.GetComponent<mainScript>();
                return main == null ? null : main.GetSavedData();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool CaptureFailed(string diagnostic, out string error)
        {
            Interlocked.Increment(ref captureFailureCount);
            SetDiagnostic(diagnostic);
            error = diagnostic;
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

    internal static class BusinessRemainingMinutesPatchHealth
    {
        internal const int ExpectedTargetMethodCount = 1;
        private static readonly object Sync = new object();
        private static int resolvedTargetMethodCount;
        private static string failure = string.Empty;

        internal static bool IsHealthy
        {
            get
            {
                lock (Sync)
                {
                    return resolvedTargetMethodCount == ExpectedTargetMethodCount && string.IsNullOrEmpty(failure);
                }
            }
        }

        internal static void ReportTargetResolved()
        {
            lock (Sync)
            {
                resolvedTargetMethodCount++;
                if (resolvedTargetMethodCount > ExpectedTargetMethodCount)
                {
                    failure = "N10 resolved more target methods than the frozen one-method manifest.";
                }
            }
        }

        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync)
            {
                failure = diagnostic ?? "unknown N10 patch failure";
            }
        }
    }
}
