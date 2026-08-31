using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Tutorial.LoadFunction() calls Tutorial.Reset(), which calls Tutorial_Reqs.Reset()
    /// and clears both private activity baselines to -1. Restore N11 only after vanilla
    /// has completed that reset and restored Tutorial.Save_Data.
    /// </summary>
    [HarmonyPatch]
    internal static class TutorialLoadActivityBaselines_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Tutorial),
                nameof(Tutorial.LoadFunction),
                Type.EmptyTypes);

            if (method == null || method.ReturnType != typeof(void))
            {
                TutorialActivityBaselinePatchHealth.ReportFailure(
                    "Tutorial.LoadFunction() could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(Tutorial).FullName, "LoadFunction");
            }

            TutorialActivityBaselinePatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            TutorialActivityBaselineRepair.RestoreAfterVanillaTutorialLoad();
        }
    }
}
