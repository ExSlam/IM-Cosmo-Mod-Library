using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    [HarmonyPatch]
    internal static class SnsMessageLoad_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(SNS_Manager),
                "LoadFunction",
                Type.EmptyTypes);

            if (method == null || method.ReturnType != typeof(void) || method.IsStatic)
            {
                SnsMessagePatchHealth.ReportFailure(
                    "SNS_Manager.LoadFunction() could not be resolved with the audited instance-void signature.");
                throw new MissingMethodException(typeof(SNS_Manager).FullName, "LoadFunction");
            }

            SnsMessagePatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SNS_Manager __instance)
        {
            SnsMessageRepair.RestoreAfterVanillaLoad(__instance);
        }
    }
}
