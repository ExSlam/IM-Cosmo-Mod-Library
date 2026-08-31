using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Vanilla Shows.LoadFunction rebuilds the complete show registry. Restore N05
    /// only after that reconstruction has completed. UpdateList merely schedules its
    /// coroutine at this point; the live show list is already authoritative.
    /// </summary>
    [HarmonyPatch]
    internal static class ShowFanAppealLoad_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Shows),
                nameof(Shows.LoadFunction),
                Type.EmptyTypes);

            if (method == null || method.ReturnType != typeof(void))
            {
                ShowFanAppealPatchHealth.ReportFailure(
                    "Shows.LoadFunction() could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(Shows).FullName, "LoadFunction");
            }

            ShowFanAppealPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            ShowFanAppealRepair.RestoreAfterVanillaLoad();
        }
    }
}
