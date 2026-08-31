using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    [HarmonyPatch]
    internal static class AgencyLoadPausedTrainingClear_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(agency), nameof(agency.LoadFunction), Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                PausedTrainingGirlPatchHealth.ReportFailure("agency.LoadFunction() could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(agency).FullName, "LoadFunction");
            }
            PausedTrainingGirlPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix()
        {
            PausedTrainingGirlRepair.BeginAgencyLoad();
        }
    }

    [HarmonyPatch]
    internal static class AgencyRoomPausedTrainingRestore_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(agency), "GetRoomDataForLoading", new Type[] { typeof(agency.RoomData) });
            if (method == null || method.ReturnType != typeof(agency._room))
            {
                PausedTrainingGirlPatchHealth.ReportFailure("agency.GetRoomDataForLoading(RoomData) could not be resolved with the audited return type.");
                throw new MissingMethodException(typeof(agency).FullName, "GetRoomDataForLoading");
            }
            PausedTrainingGirlPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(agency.RoomData __0, agency._room __result)
        {
            PausedTrainingGirlRepair.RestoreForRoom(__0, __result);
        }
    }
}
