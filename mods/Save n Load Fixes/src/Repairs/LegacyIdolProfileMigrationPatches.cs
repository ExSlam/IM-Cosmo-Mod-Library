using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    [HarmonyPatch]
    internal static class LegacyIdolProfileMigration_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(data_girls),
                nameof(data_girls.LoadFunction),
                Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                LegacyIdolProfileMigrationPatchHealth.ReportFailure(
                    "data_girls.LoadFunction() could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(data_girls).FullName, nameof(data_girls.LoadFunction));
            }

            LegacyIdolProfileMigrationPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix()
        {
            LegacyIdolProfileMigration.PrepareLegacyFieldsBeforeGirlLoad();
        }
    }
}
