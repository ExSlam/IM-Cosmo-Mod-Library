using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Runs immediately before Date_Popup's original target-only DatingData clear so
    /// an existing idol-idol relationship can still be resolved and torn down through
    /// vanilla Relationships._relationship.BreakUp().
    /// </summary>
    [HarmonyPatch]
    internal static class ForcedBreakup_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Date_Popup),
                "OnClick_ForceBreakup",
                Type.EmptyTypes);

            if (method == null || method.ReturnType != typeof(void))
            {
                ForcedBreakupPatchHealth.ReportFailure(
                    "Date_Popup.OnClick_ForceBreakup() could not be resolved with the audited private void signature.");
                throw new MissingMethodException(typeof(Date_Popup).FullName, "OnClick_ForceBreakup");
            }

            ForcedBreakupPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix(Date_Popup __instance)
        {
            ForcedBreakupRepair.RepairIdolRelationshipBeforeVanillaClear(__instance);
        }
    }
}
