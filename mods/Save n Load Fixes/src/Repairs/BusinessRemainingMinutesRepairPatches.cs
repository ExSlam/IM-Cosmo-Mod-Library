using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    [HarmonyPatch]
    internal static class AgencyRoomBusinessRemainingMinutesRestore_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(agency),
                "GetRoomDataForLoading",
                new Type[] { typeof(agency.RoomData) });
            if (method == null || method.ReturnType != typeof(agency._room))
            {
                BusinessRemainingMinutesPatchHealth.ReportFailure(
                    "agency.GetRoomDataForLoading(RoomData) could not be resolved with the audited return type.");
                throw new MissingMethodException(typeof(agency).FullName, "GetRoomDataForLoading");
            }

            BusinessRemainingMinutesPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(agency.RoomData __0, agency._room __result)
        {
            BusinessRemainingMinutesRepair.RestoreForRoom(__0, __result);
        }
    }
}
