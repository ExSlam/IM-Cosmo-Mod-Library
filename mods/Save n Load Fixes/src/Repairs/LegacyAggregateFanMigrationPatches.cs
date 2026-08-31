using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    [HarmonyPatch]
    internal static class LegacyAggregateFanMigration_SaveNLoadFixes_Patch
    {
        private const string AuditedMethodName = "RedistributeFansOnOldSaveLoad";

        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(data_girls),
                AuditedMethodName,
                Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                LegacyAggregateFanMigrationPatchHealth.ReportFailure(
                    "data_girls.RedistributeFansOnOldSaveLoad() could not be resolved with the audited private void signature.");
                throw new MissingMethodException(typeof(data_girls).FullName, AuditedMethodName);
            }

            LegacyAggregateFanMigrationPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool Prefix()
        {
            LegacyAggregateFanMigration.ReplaceVanillaLegacyRedistribution();

            // Never fall back to vanilla's shuffled compatibility path. For modern
            // saves this replacement is a no-op after the same eligibility check; for
            // malformed legacy targets it fails closed instead of rerandomizing.
            return false;
        }
    }
}
