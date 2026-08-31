using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Restore A02 only after SaveManager.LoadData has completed the full LoadEvent
    /// multicast. At this point singles, shows, concerts, tours, and SSK have all
    /// reconstructed their owners and attached the deserialized parameter lists.
    /// </summary>
    [HarmonyPatch]
    internal static class SaveManagerLoadPathProjectProgressCounter_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(SaveManager),
                nameof(SaveManager.LoadData),
                new Type[] { typeof(string) });

            if (method == null || method.ReturnType != typeof(void))
            {
                ProjectProgressCounterPatchHealth.ReportFailure(
                    "SaveManager.LoadData(string) could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(SaveManager).FullName, "LoadData(string)");
            }

            ProjectProgressCounterPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(SaveManager __instance)
        {
            ProjectProgressCounterRepair.RestoreAfterCareerLoad(__instance);
        }
    }

    [HarmonyPatch]
    internal static class SaveManagerLoadSlotProjectProgressCounter_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(SaveManager),
                nameof(SaveManager.LoadData),
                new Type[] { typeof(bool) });

            if (method == null || method.ReturnType != typeof(void))
            {
                ProjectProgressCounterPatchHealth.ReportFailure(
                    "SaveManager.LoadData(bool) could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(SaveManager).FullName, "LoadData(bool)");
            }

            ProjectProgressCounterPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(SaveManager __instance)
        {
            ProjectProgressCounterRepair.RestoreAfterCareerLoad(__instance);
        }
    }
}
