using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Awards.LoadFunction() is the source-correct late-rebind seam: vanilla first
    /// Reset()s the static award registries, then reconstructs final AwardData and
    /// SpeechData using data_girls/singles ID lookups. A28 clears discarded F9 slate
    /// state before that work and restores the target save's pending slate afterward.
    /// </summary>
    [HarmonyPatch]
    internal static class AwardTempNominationLoad_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(Awards), nameof(Awards.LoadFunction), Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                AwardTempNominationPatchHealth.ReportFailure(
                    "Awards.LoadFunction() could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(Awards).FullName, nameof(Awards.LoadFunction));
            }

            AwardTempNominationPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix()
        {
            AwardTempNominationRepair.ClearBeforeVanillaAwardsLoad();
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            AwardTempNominationRepair.RestoreAfterVanillaAwardsLoad();
        }
    }
}
