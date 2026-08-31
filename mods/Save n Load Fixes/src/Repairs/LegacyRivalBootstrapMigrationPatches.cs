using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    [HarmonyPatch]
    internal static class LegacyRivalBootstrapLoadScope_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(Rivals), nameof(Rivals.LoadFunction), Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                LegacyRivalBootstrapMigrationPatchHealth.ReportFailure(
                    "Rivals.LoadFunction() could not be resolved with the audited public void signature.");
                throw new MissingMethodException(typeof(Rivals).FullName, nameof(Rivals.LoadFunction));
            }

            LegacyRivalBootstrapMigrationPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix()
        {
            LegacyRivalBootstrapMigration.EnterRivalLoadScope();
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception)
        {
            LegacyRivalBootstrapMigration.ExitRivalLoadScope(__exception);
            return __exception;
        }
    }

    [HarmonyPatch]
    internal static class LegacyRivalBootstrapGenerate_SaveNLoadFixes_Patch
    {
        private const string AuditedMethodName = "Generate";

        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(Rivals), AuditedMethodName, Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                LegacyRivalBootstrapMigrationPatchHealth.ReportFailure(
                    "Rivals.Generate() could not be resolved with the audited private void signature.");
                throw new MissingMethodException(typeof(Rivals).FullName, AuditedMethodName);
            }

            LegacyRivalBootstrapMigrationPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(Rivals __instance)
        {
            return LegacyRivalBootstrapMigration.ShouldRunVanillaGenerate(__instance);
        }
    }
}
