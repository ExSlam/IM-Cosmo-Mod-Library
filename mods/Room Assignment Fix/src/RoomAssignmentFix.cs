using System;
using HarmonyLib;

namespace RoomAssignmentFix
{
    /// <summary>
    /// Vanilla's legacy Room.onMouseUp handler assigns the current idol directly, bypassing the
    /// availability checks used by DragAndDropManager.  The automatic-room paths can also reach
    /// agency._room.assign directly.  Keep an idol owned by at most one room in both cases.
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
                return false;
            }

            if (girl.status == data_girls._status.normal || girl.status == data_girls._status.scene)
            {
                return true;
            }

            // Doctors' offices are the vanilla exception: they must be able to receive an
            // injured or depressed idol, provided she is not already assigned to another room.
            return room.type == agency._type.doctorsOffice && girl.IsSick();
        }

        internal static bool CanStartRoomAssignment(agency._room room, data_girls.girls girl, data_girls._paramType? forceParam)
        {
            return IsAvailableForRoom(room, girl) &&
                   room.canAssign(girl, forceParam) &&
                   room.canTrain(girl, forceParam);
        }
    }

    [HarmonyPatch(typeof(Room), nameof(Room.onMouseUp))]
    internal static class RoomOnMouseUpAssignmentGuardPatch
    {
        private static bool Prefix(Room __instance)
        {
            if (__instance == null || __instance.room == null)
            {
                return false;
            }

            data_girls.girls girl = data_girls.getGirl(staticVars.dragAndDrop_id);
            return IdolAssignmentGuard.CanStartRoomAssignment(__instance.room, girl, null);
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
}
