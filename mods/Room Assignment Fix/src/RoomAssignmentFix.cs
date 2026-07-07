using System;
using HarmonyLib;

namespace RoomAssignmentFix
{
    /// <summary>
    /// Automatic-room paths can reach agency._room.assign directly. Keep an idol owned by at
    /// most one room even when a caller bypasses DragAndDropManager's availability checks.
    /// </summary>
    internal static class IdolAssignmentGuard
    {
        internal static bool IsAvailableForRoom(agency._room room, data_girls.girls girl)
        {
            if (room == null || girl == null)
            {
                return false;
            }

            if (girl.room != null && girl.room != room)
            {
                if (IsActivelyOwnedByRoom(girl.room, girl))
                {
                    return false;
                }

                ReleaseStaleRoomPointer(girl);
            }

            if (girl.status == data_girls._status.normal || girl.status == data_girls._status.scene)
            {
                return true;
            }

            // Doctors' offices are the vanilla exception: they must be able to receive an
            // injured or depressed idol, provided she is not already assigned to another room.
            return room.type == agency._type.doctorsOffice && girl.IsSick();
        }

        private static bool IsActivelyOwnedByRoom(agency._room owner, data_girls.girls girl)
        {
            if (owner.girl == girl && owner.status != agency._room._status.normal)
            {
                return true;
            }

            if (owner.type == agency._type.cafeAndShop && girl.status == data_girls._status.practice)
            {
                Cafes._cafe cafe = owner.GetCafe();
                if ((cafe != null && cafe.WorkingGirls.Contains(girl)) ||
                    owner.cafe_girl_1 == girl ||
                    owner.cafe_girl_2 == girl ||
                    owner.cafe_girl_3 == girl)
                {
                    return true;
                }
            }

            // Practice and scene are unavailable states even if another vanilla state bug has
            // left the owning room's reverse pointer incomplete.
            return girl.status == data_girls._status.practice ||
                   girl.status == data_girls._status.scene;
        }

        private static void ReleaseStaleRoomPointer(data_girls.girls girl)
        {
            agency._room owner = girl.room;
            if (owner == null)
            {
                return;
            }

            if (owner.girl == girl && owner.status == agency._room._status.normal)
            {
                owner.girl = null;
            }

            if (owner.type == agency._type.cafeAndShop)
            {
                Cafes._cafe cafe = owner.GetCafe();
                if (cafe != null)
                {
                    cafe.WorkingGirls.Remove(girl);
                }
                if (owner.cafe_girl_1 == girl)
                {
                    owner.cafe_girl_1 = null;
                }
                if (owner.cafe_girl_2 == girl)
                {
                    owner.cafe_girl_2 = null;
                }
                if (owner.cafe_girl_3 == girl)
                {
                    owner.cafe_girl_3 = null;
                }
            }

            girl.room = null;
        }
    }

    [HarmonyPatch(typeof(Room), nameof(Room.onMouseUp))]
    internal static class ObsoleteRoomMouseUpAssignmentPatch
    {
        private static bool Prefix()
        {
            // Room.onMouseUp belongs to the game's obsolete drag path. It always looks up
            // staticVars.dragAndDrop_id, but the current GirlDraggable/DragAndDropManager path
            // never writes that field. It therefore remains zero and can assign idol 0 instead
            // of the idol being dragged, consuming a clinic before the real drop is processed.
            // DragAndDropManager.OnMouseUp is the canonical path and performs canAssign/canTrain
            // checks before calling agency._room.assign with the actual dragged idol.
            return false;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.assign), new Type[] { typeof(data_girls.girls), typeof(Nullable<data_girls._paramType>) })]
    internal static class RoomAssignIdolOwnershipGuardPatch
    {
        private static bool Prefix(agency._room __instance, data_girls.girls _girl)
        {
            return IdolAssignmentGuard.IsAvailableForRoom(__instance, _girl);
        }
    }

    [HarmonyPatch(typeof(DragAndDropManager), nameof(DragAndDropManager.OnMouseUp),
        new Type[] { typeof(data_girls.girls), typeof(staticVars.DragAndDrop), typeof(UnityEngine.GameObject) })]
    internal static class ClinicRecoverySlotDropFallbackPatch
    {
        private struct DropState
        {
            internal agency._room Room;
            internal data_girls.girls Girl;
            internal data_girls._paramType Parameter;
        }

        [HarmonyPrefix]
        private static void Prefix(
            data_girls.girls girl,
            staticVars.DragAndDrop type,
            agency._room ___hovering_room,
            out DropState __state)
        {
            __state = new DropState();
            if (type != staticVars.DragAndDrop.room || girl == null || ___hovering_room == null ||
                ___hovering_room.type != agency._type.doctorsOffice ||
                ___hovering_room.hovering_style == null)
            {
                return;
            }

            data_girls._paramType parameter = ___hovering_room.hovering_style.Value;
            if (parameter != data_girls._paramType.physicalStamina &&
                parameter != data_girls._paramType.mentalStamina)
            {
                return;
            }

            __state.Room = ___hovering_room;
            __state.Girl = girl;
            __state.Parameter = parameter;
        }

        [HarmonyPostfix]
        private static void Postfix(DropState __state)
        {
            agency._room room = __state.Room;
            data_girls.girls girl = __state.Girl;
            if (room == null || girl == null || room.girl == girl || room.girl != null ||
                room.status != agency._room._status.normal)
            {
                return;
            }

            data_girls._paramType? parameter = new data_girls._paramType?(__state.Parameter);
            if (room.canAssign(girl, parameter) && room.canTrain(girl, parameter))
            {
                room.assign(girl, parameter);
            }
        }
    }
}
