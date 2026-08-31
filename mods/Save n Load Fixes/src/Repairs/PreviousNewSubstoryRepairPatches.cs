using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Vanilla Substories_Manager.LoadFunction() rebuilds the target queue/used state
    /// but never resets or restores PreviousNewSubstory. Restore A16 only after that
    /// target state is available.
    /// </summary>
    [HarmonyPatch]
    internal static class SubstoriesManagerLoadPreviousNewSubstory_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Substories_Manager),
                nameof(Substories_Manager.LoadFunction),
                Type.EmptyTypes);

            if (method == null || method.ReturnType != typeof(void))
            {
                PreviousNewSubstoryPatchHealth.ReportFailure(
                    "Substories_Manager.LoadFunction() could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(Substories_Manager).FullName, "LoadFunction");
            }

            PreviousNewSubstoryPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(Substories_Manager __instance)
        {
            PreviousNewSubstoryRepair.RestoreAfterVanillaSubstoriesLoad(__instance);
        }
    }
}
