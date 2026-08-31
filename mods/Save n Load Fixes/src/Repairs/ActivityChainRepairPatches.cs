using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Vanilla Activities.LoadFunction restores activity timing/levels but neither
    /// clears nor restores the static future-intent Chain. Run N07 after vanilla has
    /// adopted the target activity state. A08 already epoch-guards stale Chain_Progress_Do.
    /// </summary>
    [HarmonyPatch]
    internal static class ActivitiesLoadChain_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = typeof(Activities).GetMethod(
                nameof(Activities.LoadFunction),
                BindingFlags.Instance | BindingFlags.Public,
                null,
                Type.EmptyTypes,
                null);

            if (method == null)
            {
                ActivityChainPatchHealth.ReportFailure(
                    "Activities.LoadFunction() could not be resolved with the audited zero-argument signature.");
                throw new MissingMethodException(typeof(Activities).FullName, nameof(Activities.LoadFunction));
            }

            ActivityChainPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            ActivityChainRepair.RestoreAfterVanillaLoad();
        }
    }
}
