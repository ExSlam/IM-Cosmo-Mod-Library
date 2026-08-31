using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repairs A03 at the narrow private single reconstruction helper. Vanilla is
    /// allowed to run unchanged; the Postfix only puts the already-serialized current-
    /// format Marketing_Result back after the invalid zero-as-uninitialized branch.
    /// </summary>
    [HarmonyPatch]
    internal static class RiskyMarketingZero_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = typeof(singles).GetMethod(
                "GetSingleDataForLoading",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(singles.SinglesData) },
                null);

            if (method == null || method.ReturnType != typeof(singles._single))
            {
                RiskyMarketingZeroPatchHealth.ReportFailure(
                    "singles.GetSingleDataForLoading(SinglesData) could not be resolved with the audited private signature");
                throw new MissingMethodException(
                    typeof(singles).FullName,
                    "GetSingleDataForLoading");
            }

            RiskyMarketingZeroPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(
            singles.SinglesData single,
            singles._single __result)
        {
            RiskyMarketingZeroRepair.RestoreSerializedCurrentFormatValue(
                single,
                __result);
        }
    }
}
