using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Stats.LoadFunction() calls Reset(), which replaces Activities_Stats with an
    /// empty list before restoring aggregate counters. Rebuild A15 only after that
    /// vanilla reset/restore step has completed.
    /// </summary>
    [HarmonyPatch]
    internal static class StatsLoadRecentActivityRecency_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Stats),
                nameof(Stats.LoadFunction),
                Type.EmptyTypes);

            if (method == null || method.ReturnType != typeof(void))
            {
                RecentActivityRecencyPatchHealth.ReportFailure(
                    "Stats.LoadFunction() could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(Stats).FullName, "LoadFunction");
            }

            RecentActivityRecencyPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            RecentActivityRecencyRepair.RestoreAfterVanillaStatsLoad();
        }
    }
}
