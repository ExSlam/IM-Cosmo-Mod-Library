using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Reconstructs the omitted training-only private baseline immediately after the
    /// vanilla RoomData -> _room copier has restored its authoritative serialized inputs.
    /// </summary>
    [HarmonyPatch]
    internal static class TrainingProgressInit_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(agency),
                "GetRoomDataForLoading",
                new Type[] { typeof(agency.RoomData) });

            if (method == null || method.ReturnType != typeof(agency._room))
            {
                TrainingProgressInitPatchHealth.ReportFailure(
                    "agency.GetRoomDataForLoading(RoomData) could not be resolved with the audited return type.");
                throw new MissingMethodException(typeof(agency).FullName, "GetRoomDataForLoading");
            }

            TrainingProgressInitPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(agency.RoomData __0, agency._room __result)
        {
            TrainingProgressInitRepair.ReconstructFromTargetSave(__0, __result);
        }
    }
}
