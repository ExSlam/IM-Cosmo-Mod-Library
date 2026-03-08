using HarmonyLib;

namespace StaffFiringFreezeFix
{
    internal static class PatchTargets
    {
        internal const string FireMethodName = nameof(staff._staff.Fire);
        internal const string FireWithSeveranceMethodName = nameof(staff._staff.Fire_Severance);
        internal const string DoBusinessProposalMethodName = nameof(agency._room.DoBusinessProposal);
    }

    internal static class StaffRoomTaskCancellation
    {
        private static readonly agency._room._status NormalRoomStatus = agency._room._status.normal;
        private const float ClearedProgressValue = 0f;

        internal static void PrepareRoomForSafeStaffRemoval(staff._staff staffMember)
        {
            if (staffMember == null)
            {
                return;
            }

            agency._room assignedRoom = staffMember.room;
            if (assignedRoom == null)
            {
                return;
            }

            if (assignedRoom.status != NormalRoomStatus)
            {
                assignedRoom.CancelJob();
            }

            ClearDeferredBusinessProposalState(assignedRoom);
        }

        internal static void ClearDeferredBusinessProposalState(agency._room room)
        {
            if (room == null)
            {
                return;
            }

            room.business_minutes_before_finish = ClearedProgressValue;
            room.businessType = null;
            room.businessData = null;
            room.Progress_Init = ClearedProgressValue;
        }
    }

    [HarmonyPatch(typeof(staff._staff), PatchTargets.FireMethodName)]
    internal static class StaffFirePatch
    {
        private static void Prefix(staff._staff __instance)
        {
            StaffRoomTaskCancellation.PrepareRoomForSafeStaffRemoval(__instance);
        }
    }

    [HarmonyPatch(typeof(staff._staff), PatchTargets.FireWithSeveranceMethodName)]
    internal static class StaffFireWithSeverancePatch
    {
        private static void Prefix(staff._staff __instance)
        {
            StaffRoomTaskCancellation.PrepareRoomForSafeStaffRemoval(__instance);
        }
    }

    [HarmonyPatch(typeof(agency._room), PatchTargets.DoBusinessProposalMethodName)]
    internal static class RoomDoBusinessProposalPatch
    {
        private static bool Prefix(agency._room __instance)
        {
            if (__instance == null)
            {
                return false;
            }

            if (__instance.staffer == null)
            {
                StaffRoomTaskCancellation.ClearDeferredBusinessProposalState(__instance);
                return false;
            }

            return true;
        }
    }
}
