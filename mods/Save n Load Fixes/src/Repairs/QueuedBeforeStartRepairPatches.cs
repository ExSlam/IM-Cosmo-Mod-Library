using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    [HarmonyPatch]
    internal static class SubstoriesManagerLoadQueuedBeforeStart_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(Substories_Manager), nameof(Substories_Manager.LoadFunction), Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                QueuedBeforeStartPatchHealth.ReportFailure("Substories_Manager.LoadFunction() could not be resolved with audited void signature.");
                throw new MissingMethodException(typeof(Substories_Manager).FullName, "LoadFunction");
            }
            QueuedBeforeStartPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(Substories_Manager __instance)
        {
            QueuedBeforeStartRepair.RestoreAfterSubstoriesLoad(__instance);
        }
    }
}
