using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    [HarmonyPatch]
    internal static class LegacyRelationshipBootstrapScope_SaveNLoadFixes_Patch
    {
        private const string AuditedMethodName = "InitialCreationForOldSave";

        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(Relationships), AuditedMethodName, Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                LegacyRelationshipBootstrapMigrationPatchHealth.ReportFailure(
                    "Relationships.InitialCreationForOldSave() could not be resolved with the audited private void signature.");
                throw new MissingMethodException(typeof(Relationships).FullName, AuditedMethodName);
            }

            LegacyRelationshipBootstrapMigrationPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix()
        {
            LegacyRelationshipBootstrapMigration.EnterLegacyBootstrap();
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception)
        {
            LegacyRelationshipBootstrapMigration.ExitLegacyBootstrap(__exception);
            return __exception;
        }
    }

    [HarmonyPatch]
    internal static class LegacyRelationshipInitialize_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Relationships._relationship),
                nameof(Relationships._relationship.Initialize),
                Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                LegacyRelationshipBootstrapMigrationPatchHealth.ReportFailure(
                    "Relationships._relationship.Initialize() could not be resolved with the audited public void signature.");
                throw new MissingMethodException(typeof(Relationships._relationship).FullName, "Initialize");
            }

            LegacyRelationshipBootstrapMigrationPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(Relationships._relationship __instance)
        {
            return LegacyRelationshipBootstrapMigration.ShouldRunVanillaInitialize(__instance);
        }
    }
}
