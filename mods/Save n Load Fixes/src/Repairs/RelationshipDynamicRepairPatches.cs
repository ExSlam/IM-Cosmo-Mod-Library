using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Vanilla Relationships.LoadFunction reconstructs all relationship objects and
    /// caches them. Restore N01 only after that source-owned reconstruction completes.
    /// </summary>
    [HarmonyPatch]
    internal static class RelationshipDynamicLoad_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Relationships),
                nameof(Relationships.LoadFunction),
                Type.EmptyTypes);

            if (method == null || method.ReturnType != typeof(void))
            {
                RelationshipDynamicPatchHealth.ReportFailure(
                    "Relationships.LoadFunction() could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(Relationships).FullName, "LoadFunction");
            }

            RelationshipDynamicPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            RelationshipDynamicRepair.RestoreAfterVanillaLoad();
        }
    }
}
