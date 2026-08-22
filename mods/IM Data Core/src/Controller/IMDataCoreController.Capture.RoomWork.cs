using System;
using System.Collections.Generic;
using System.Globalization;

namespace IMDataCore
{
    /// <summary>
    /// Captures the staff responsible for room work.  This is intentionally Core-owned rather
    /// than Assistant-Manager-owned: Assistant Manager rooms use the same public room and staff
    /// state as vanilla rooms, while the payload remains useful to any consumer mod.
    /// </summary>
    internal sealed partial class IMDataCoreController
    {
        private readonly Dictionary<agency._room, StaffAttributionSnapshot> assignedStaffByRoom =
            new Dictionary<agency._room, StaffAttributionSnapshot>();

        private readonly Dictionary<Auditions.data, StaffAttributionSnapshot> staffByAudition =
            new Dictionary<Auditions.data, StaffAttributionSnapshot>();

        private readonly Dictionary<int, StaffAttributionSnapshot> pendingHireStaffByIdolId =
            new Dictionary<int, StaffAttributionSnapshot>();

        internal void TrackRoomWorkAssignment(agency._room room)
        {
            if (room == null || room.staffer == null)
            {
                return;
            }

            assignedStaffByRoom[room] = CreateStaffAttribution(room.staffer);
        }

        internal void ClearRoomWorkAssignment(agency._room room)
        {
            if (room != null)
            {
                assignedStaffByRoom.Remove(room);
            }
        }

        internal RoomWorkCompletionSnapshot CreateRoomWorkCompletionSnapshot(
            agency._room room,
            string workKind,
            string entityId,
            string title,
            string stage,
            List<int> idolIds)
        {
            RoomWorkCompletionSnapshot snapshot = new RoomWorkCompletionSnapshot();
            if (room == null)
            {
                return snapshot;
            }

            snapshot.Room = room;
            snapshot.IsDue = room.finishTime < staticVars.dateTime;
            snapshot.WorkKind = workKind ?? string.Empty;
            snapshot.EntityId = entityId ?? string.Empty;
            snapshot.Title = title ?? string.Empty;
            snapshot.Stage = stage ?? string.Empty;
            snapshot.IdolIds = idolIds ?? new List<int>();
            snapshot.AssignedStaff = GetAssignedStaff(room);
            return snapshot;
        }

        internal void CaptureRoomWorkCompleted(RoomWorkCompletionSnapshot snapshot, string sourcePatchCode)
        {
            if (snapshot == null || !snapshot.IsDue || snapshot.Room == null || snapshot.IdolIds == null ||
                snapshot.IdolIds.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return;
            }

            StaffAttributionSnapshot completedStaff = CreateStaffAttribution(snapshot.Room.staffer);
            StaffAttributionSnapshot assignedStaff = snapshot.AssignedStaff ?? completedStaff;
            StaffAttributionSnapshot primaryStaff = completedStaff ?? assignedStaff;
            List<int> participantIdolIds = new List<int>();
            HashSet<int> emittedIdols = new HashSet<int>();
            for (int index = CoreConstants.ZeroBasedListStartIndex; index < snapshot.IdolIds.Count; index++)
            {
                int idolId = snapshot.IdolIds[index];
                if (idolId >= CoreConstants.MinimumValidIdolIdentifier && emittedIdols.Add(idolId))
                {
                    participantIdolIds.Add(idolId);
                }
            }

            if (participantIdolIds.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                ClearRoomWorkAssignment(snapshot.Room);
                return;
            }

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                string roomEntityId =
                    GetOrCreateAgencyRoomHistoryEntityIdLocked(snapshot.Room);
                RoomWorkCompletedEventPayload payload = new RoomWorkCompletedEventPayload
                {
                    idol_id = CoreConstants.InvalidIdValue,
                    room_work_participant_id_list = BuildDelimitedIdentifierList(participantIdolIds),
                    room_id = snapshot.Room.id,
                    room_type = CoreEnumNameMapping.ToAgencyRoomTypeCode(snapshot.Room.type),
                    room_work_kind = snapshot.WorkKind,
                    room_work_stage = snapshot.Stage,
                    room_work_entity_id = snapshot.EntityId,
                    room_work_title = snapshot.Title
                };
                CopyPrimaryStaff(payload, primaryStaff);
                CopyAssignedStaff(payload, assignedStaff);
                CopyCompletedStaff(payload, completedStaff);

                EnqueueEventRecordLocked(
                    gameDate,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindRoomWork,
                    string.Concat(roomEntityId, ":", snapshot.WorkKind, ":", snapshot.EntityId),
                    CoreConstants.EventTypeRoomWorkCompleted,
                    sourcePatchCode ?? CoreConstants.EventSourceRoomWorkCompletedPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                FlushAfterCaptureLocked();
            }

            ClearRoomWorkAssignment(snapshot.Room);
        }

        /// <summary>
        /// Captures the primitive room assignment immediately before CancelJob erases it.
        /// </summary>
        internal RoomWorkCompletionSnapshot CreateRoomWorkCancellationSnapshot(
            agency._room room)
        {
            RoomWorkCompletionSnapshot snapshot = new RoomWorkCompletionSnapshot
            {
                Room = room,
                AssignedStaff = GetAssignedStaff(room)
            };
            if (room == null || room.status == agency._room._status.normal)
            {
                return snapshot;
            }

            try
            {
                if (room.status == agency._room._status.girlTraining ||
                    room.status == agency._room._status.injury_treatment ||
                    room.status == agency._room._status.depression_treatment ||
                    room.status == agency._room._status.date)
                {
                    data_girls.girls idol = room.girl;
                    if (idol == null ||
                        idol.id < CoreConstants.MinimumValidIdolIdentifier)
                    {
                        return snapshot;
                    }

                    snapshot.WorkKind = room.status == agency._room._status.girlTraining
                        ? "training"
                        : (room.status == agency._room._status.date
                            ? "date"
                            : "medical_recovery");
                    snapshot.EntityId = idol.id.ToString(CultureInfo.InvariantCulture);
                    snapshot.Title = idol.GetName(true) ?? string.Empty;
                    snapshot.Stage = room.status.ToString();
                    snapshot.IdolIds.Add(idol.id);
                    snapshot.HasWork = true;
                    return snapshot;
                }

                if (room.status == agency._room._status.singleProduction &&
                    room.single != null)
                {
                    snapshot.WorkKind = "single";
                    snapshot.EntityId = room.single.id.ToString(CultureInfo.InvariantCulture);
                    snapshot.Title = room.single.title ?? string.Empty;
                    snapshot.Stage = room.singleParamType(room.single).ToString();
                    snapshot.IdolIds = RoomWorkPatchUtility.GetIdolIds(room.single.girls);
                }
                else if (room.status == agency._room._status.showProduction &&
                    room.show != null)
                {
                    snapshot.WorkKind = "show";
                    snapshot.EntityId = room.show.id.ToString(CultureInfo.InvariantCulture);
                    snapshot.Title = room.show.title ?? string.Empty;
                    snapshot.Stage = room.showParamType(room.show).ToString();
                    snapshot.IdolIds = RoomWorkPatchUtility.GetIdolIds(room.show.GetCast());
                }
                else if (room.status == agency._room._status.concertProduction &&
                    room.concert != null)
                {
                    snapshot.WorkKind = "concert";
                    snapshot.EntityId = room.concert.ID.ToString(CultureInfo.InvariantCulture);
                    snapshot.Title = room.concert.Title ?? string.Empty;
                    snapshot.Stage = room.concertParamType(room.concert).ToString();
                    snapshot.IdolIds = RoomWorkPatchUtility.GetIdolIds(room.concert.GetGirls(true));
                }
                else if (room.status == agency._room._status.SSKProduction &&
                    room.SSK != null)
                {
                    snapshot.WorkKind = "event";
                    snapshot.EntityId = room.SSK.ID.ToString(CultureInfo.InvariantCulture);
                    snapshot.Title = room.SSK.GetTitle() ?? string.Empty;
                    snapshot.Stage = room.SSKParamType(room.SSK).ToString();
                    snapshot.IdolIds = RoomWorkPatchUtility.GetActiveIdolIds();
                }
                else if (room.status == agency._room._status.tourProduction &&
                    room.tour != null)
                {
                    snapshot.WorkKind = "event";
                    snapshot.EntityId = room.tour.ID.ToString(CultureInfo.InvariantCulture);
                    snapshot.Title = string.Concat(
                        "Tour #",
                        room.tour.ID.ToString(CultureInfo.InvariantCulture));
                    snapshot.Stage = room.TourParamType(room.tour).ToString();
                    snapshot.IdolIds = RoomWorkPatchUtility.GetActiveIdolIds();
                }

                snapshot.HasWork = snapshot.IdolIds != null &&
                    snapshot.IdolIds.Count >= CoreConstants.MinimumNonEmptyCollectionCount;
            }
            catch (Exception exception)
            {
                CoreLog.Warn(string.Concat(
                    "Unable to snapshot cancelled room work: ",
                    exception.Message));
                snapshot.HasWork = false;
            }

            return snapshot;
        }

        /// <summary>
        /// Records one cancelled assignment from its pre-mutation snapshot.
        /// </summary>
        internal void CaptureRoomWorkCancelled(RoomWorkCompletionSnapshot snapshot)
        {
            if (snapshot == null ||
                !snapshot.HasWork ||
                snapshot.Room == null ||
                snapshot.IdolIds == null)
            {
                return;
            }

            List<int> participantIdolIds = new List<int>();
            HashSet<int> emittedIdols = new HashSet<int>();
            for (int index = CoreConstants.ZeroBasedListStartIndex;
                index < snapshot.IdolIds.Count;
                index++)
            {
                int idolId = snapshot.IdolIds[index];
                if (idolId >= CoreConstants.MinimumValidIdolIdentifier &&
                    emittedIdols.Add(idolId))
                {
                    participantIdolIds.Add(idolId);
                }
            }
            if (participantIdolIds.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return;
            }

            RoomWorkCompletedEventPayload payload =
                new RoomWorkCompletedEventPayload
                {
                    idol_id = CoreConstants.InvalidIdValue,
                    room_work_participant_id_list =
                        BuildDelimitedIdentifierList(participantIdolIds),
                    room_id = snapshot.Room.id,
                    room_type = CoreEnumNameMapping.ToAgencyRoomTypeCode(
                        snapshot.Room.type),
                    room_work_kind = snapshot.WorkKind ?? string.Empty,
                    room_work_stage = snapshot.Stage ?? string.Empty,
                    room_work_entity_id = snapshot.EntityId ?? string.Empty,
                    room_work_title = snapshot.Title ?? string.Empty
                };
            CopyPrimaryStaff(payload, snapshot.AssignedStaff);
            CopyAssignedStaff(payload, snapshot.AssignedStaff);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                string roomEntityId =
                    GetOrCreateAgencyRoomHistoryEntityIdLocked(snapshot.Room);

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindRoomWork,
                    string.Concat(
                        roomEntityId,
                        ":",
                        snapshot.WorkKind,
                        ":",
                        snapshot.EntityId),
                    CoreConstants.EventTypeRoomWorkCancelled,
                    CoreConstants.EventSourceRoomWorkCancelJobPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));
                FlushAfterCaptureLocked();
            }
        }

        internal TrainingCompletionSnapshot CreateTrainingCompletionSnapshot(agency._room room, bool force)
        {
            TrainingCompletionSnapshot snapshot = new TrainingCompletionSnapshot();
            if (force || room == null || room.girl == null)
            {
                return snapshot;
            }

            data_girls._paramType? parameter = room.trainingParam();
            if (!parameter.HasValue)
            {
                return snapshot;
            }

            bool isCareerPractice = IsCareerPracticeParameter(parameter.Value);
            bool isDoctorStaminaRecovery = room.type == agency._type.doctorsOffice && IsStaminaParameter(parameter.Value);
            if (!isCareerPractice && !isDoctorStaminaRecovery)
            {
                return snapshot;
            }

            snapshot.Room = room;
            snapshot.Idol = room.girl;
            snapshot.Parameter = parameter.Value;
            snapshot.WorkKind = isDoctorStaminaRecovery ? "medical_recovery" : "training";
            snapshot.ValueBefore = room.girl.getParam(parameter.Value).val;
            snapshot.AssignedStaff = GetAssignedStaff(room);
            return snapshot;
        }

        internal void CaptureTrainingCompleted(TrainingCompletionSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Room == null || snapshot.Idol == null ||
                snapshot.Idol.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            float valueAfter = snapshot.Idol.getParam(snapshot.Parameter).val;
            if (valueAfter <= snapshot.ValueBefore)
            {
                return;
            }

            List<int> idolIds = new List<int> { snapshot.Idol.id };
            RoomWorkCompletionSnapshot workSnapshot = CreateRoomWorkCompletionSnapshot(
                snapshot.Room,
                snapshot.WorkKind,
                snapshot.Idol.id.ToString(CultureInfo.InvariantCulture),
                snapshot.Parameter.ToString(),
                snapshot.Parameter.ToString(),
                idolIds);
            workSnapshot.IsDue = true;
            workSnapshot.AssignedStaff = snapshot.AssignedStaff;
            CaptureRoomWorkCompleted(workSnapshot, CoreConstants.EventSourceRoomTrainingCompletedPatch);
        }

        internal void TrackAuditionGeneration(Auditions.data audition)
        {
            if (audition == null)
            {
                return;
            }

            List<agency._room> rooms = agency.GetRooms();
            if (rooms == null)
            {
                return;
            }

            for (int index = CoreConstants.ZeroBasedListStartIndex; index < rooms.Count; index++)
            {
                agency._room room = rooms[index];
                if (room == null || room.auditionData != audition || room.staffer == null)
                {
                    continue;
                }

                staffByAudition[audition] = CreateStaffAttribution(room.staffer);
                return;
            }
        }

        internal void TrackAuditionHireCandidate(Auditions.data audition, data_girls.girls idol)
        {
            if (audition == null || idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            StaffAttributionSnapshot attribution;
            if (staffByAudition.TryGetValue(audition, out attribution) && attribution != null)
            {
                pendingHireStaffByIdolId[idol.id] = attribution;
            }
        }

        internal bool TryTakeHireStaffAttribution(int idolId, out StaffAttributionSnapshot attribution)
        {
            attribution = null;
            if (idolId < CoreConstants.MinimumValidIdolIdentifier || !pendingHireStaffByIdolId.TryGetValue(idolId, out attribution))
            {
                return false;
            }

            pendingHireStaffByIdolId.Remove(idolId);
            return true;
        }

        internal static StaffAttributionSnapshot CreateStaffAttribution(staff._staff staffer)
        {
            if (staffer == null)
            {
                return null;
            }

            return new StaffAttributionSnapshot
            {
                StaffId = staffer.id,
                StaffName = staffer.GetName(true, false) ?? string.Empty,
                StaffRole = staffer.GetJobTitle() ?? string.Empty,
                StaffType = CoreEnumNameMapping.ToStaffTypeCode(staffer.type),
                StaffTypeRaw = (int)staffer.type,
                StaffUniqueTypeRaw = (int)staffer.UniqueType,
                StaffIsPro = staffer.LevelledUp,
                StaffIsProducer = staffer.type == staff._type.player || staffer.type == staff._type.player_female
            };
        }

        private StaffAttributionSnapshot GetAssignedStaff(agency._room room)
        {
            StaffAttributionSnapshot attribution;
            if (room != null && assignedStaffByRoom.TryGetValue(room, out attribution))
            {
                return attribution;
            }

            return room != null ? CreateStaffAttribution(room.staffer) : null;
        }

        private static bool IsCareerPracticeParameter(data_girls._paramType parameter)
        {
            return parameter == data_girls._paramType.dance ||
                   parameter == data_girls._paramType.vocal ||
                   parameter == data_girls._paramType.cute ||
                   parameter == data_girls._paramType.cool ||
                   parameter == data_girls._paramType.sexy ||
                   parameter == data_girls._paramType.pretty;
        }

        private static bool IsStaminaParameter(data_girls._paramType parameter)
        {
            return parameter == data_girls._paramType.physicalStamina ||
                   parameter == data_girls._paramType.mentalStamina;
        }

        private static void CopyPrimaryStaff(RoomWorkCompletedEventPayload payload, StaffAttributionSnapshot attribution)
        {
            if (payload == null || attribution == null)
            {
                return;
            }

            payload.staff_id = attribution.StaffId;
            payload.staff_name = attribution.StaffName ?? string.Empty;
            payload.staff_role = attribution.StaffRole ?? string.Empty;
            payload.staff_type = attribution.StaffType ?? string.Empty;
            payload.staff_type_raw = attribution.StaffTypeRaw;
            payload.staff_unique_type_raw = attribution.StaffUniqueTypeRaw;
            payload.staff_is_pro = attribution.StaffIsPro;
            payload.staff_is_producer = attribution.StaffIsProducer;
        }

        private static void CopyAssignedStaff(RoomWorkCompletedEventPayload payload, StaffAttributionSnapshot attribution)
        {
            if (payload == null || attribution == null)
            {
                return;
            }

            payload.assigned_staff_id = attribution.StaffId;
            payload.assigned_staff_name = attribution.StaffName ?? string.Empty;
            payload.assigned_staff_role = attribution.StaffRole ?? string.Empty;
        }

        private static void CopyCompletedStaff(RoomWorkCompletedEventPayload payload, StaffAttributionSnapshot attribution)
        {
            if (payload == null || attribution == null)
            {
                return;
            }

            payload.completed_staff_id = attribution.StaffId;
            payload.completed_staff_name = attribution.StaffName ?? string.Empty;
            payload.completed_staff_role = attribution.StaffRole ?? string.Empty;
        }
    }

    internal sealed class StaffAttributionSnapshot
    {
        internal int StaffId = CoreConstants.InvalidIdValue;
        internal string StaffName = string.Empty;
        internal string StaffRole = string.Empty;
        internal string StaffType = string.Empty;
        internal int StaffTypeRaw = CoreConstants.InvalidIdValue;
        internal int StaffUniqueTypeRaw = CoreConstants.InvalidIdValue;
        internal bool StaffIsPro;
        internal bool StaffIsProducer;
    }

    internal sealed class RoomWorkCompletionSnapshot
    {
        internal agency._room Room;
        internal bool IsDue;
        internal bool HasWork;
        internal string WorkKind = string.Empty;
        internal string EntityId = string.Empty;
        internal string Title = string.Empty;
        internal string Stage = string.Empty;
        internal List<int> IdolIds = new List<int>();
        internal StaffAttributionSnapshot AssignedStaff;
    }

    internal sealed class TrainingCompletionSnapshot
    {
        internal agency._room Room;
        internal data_girls.girls Idol;
        internal data_girls._paramType Parameter;
        internal string WorkKind = string.Empty;
        internal float ValueBefore;
        internal StaffAttributionSnapshot AssignedStaff;
    }

    /// <summary>
    /// Thread-scoped doctor attribution while the game's treatment method calls Heal or starts
    /// a reduced hiatus.  Nested game callbacks then record the same responsible doctor.
    /// </summary>
    internal static class MedicalStaffAttributionContext
    {
        [ThreadStatic]
        private static StaffAttributionSnapshot current;

        internal static StaffAttributionSnapshot Current
        {
            get { return current; }
        }

        internal static StaffAttributionSnapshot Push(StaffAttributionSnapshot attribution)
        {
            StaffAttributionSnapshot previous = current;
            current = attribution;
            return previous;
        }

        internal static void Restore(StaffAttributionSnapshot previous)
        {
            current = previous;
        }
    }
}
