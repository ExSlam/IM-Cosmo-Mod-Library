using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    [HarmonyPatch]
    internal static class EventManagerLoadSelectedBusinessProposal_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(Event_Manager), nameof(Event_Manager.LoadFunction), Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                SelectedBusinessProposalPatchHealth.ReportFailure("Event_Manager.LoadFunction() could not be resolved with audited void signature.");
                throw new MissingMethodException(typeof(Event_Manager).FullName, "LoadFunction");
            }
            SelectedBusinessProposalPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix()
        {
            SelectedBusinessProposalRepair.ClearBeforeVanillaEventLoad();
        }
    }

    [HarmonyPatch]
    internal static class SaveManagerLoadPathSelectedBusinessProposal_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(SaveManager), nameof(SaveManager.LoadData), new Type[] { typeof(string) });
            if (method == null || method.ReturnType != typeof(void))
            {
                SelectedBusinessProposalPatchHealth.ReportFailure("SaveManager.LoadData(string) could not be resolved with audited void signature.");
                throw new MissingMethodException(typeof(SaveManager).FullName, "LoadData(string)");
            }
            SelectedBusinessProposalPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(SaveManager __instance)
        {
            SelectedBusinessProposalRepair.RestoreAfterCareerLoad(__instance);
        }
    }

    [HarmonyPatch]
    internal static class SaveManagerLoadSlotSelectedBusinessProposal_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(SaveManager), nameof(SaveManager.LoadData), new Type[] { typeof(bool) });
            if (method == null || method.ReturnType != typeof(void))
            {
                SelectedBusinessProposalPatchHealth.ReportFailure("SaveManager.LoadData(bool) could not be resolved with audited void signature.");
                throw new MissingMethodException(typeof(SaveManager).FullName, "LoadData(bool)");
            }
            SelectedBusinessProposalPatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(SaveManager __instance)
        {
            SelectedBusinessProposalRepair.RestoreAfterCareerLoad(__instance);
        }
    }
}
