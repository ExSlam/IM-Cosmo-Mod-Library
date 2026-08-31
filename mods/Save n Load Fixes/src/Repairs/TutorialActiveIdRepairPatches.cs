using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A27 has three narrow runtime seams:
    /// 1) Tutorial.Reset() clears process-stale state on startup/load/reset;
    /// 2) Tutorial_Window.Hide(Action) clears before either stock terminal caller can
    ///    hand checkpoint safety back by releasing PopupCounter;
    /// 3) agency._room.DoBusiness() normalizes stale state before vanilla evaluates its
    ///    existing Active_Tutorial_ID == "10_business_deals" forced-success branch.
    ///
    /// The supplied stock source has exactly two Tutorial_Window.Hide(...) callers: final
    /// OnContinue and Quit_Confirm. No method body or business RNG/result is replaced.
    /// </summary>
    [HarmonyPatch]
    internal static class TutorialResetActiveId_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Tutorial),
                "Reset",
                Type.EmptyTypes);

            if (method == null || !method.IsStatic || method.ReturnType != typeof(void))
            {
                TutorialActiveIdPatchHealth.ReportFailure(
                    "Tutorial.Reset() could not be resolved with the audited private static void signature");
                throw new MissingMethodException(typeof(Tutorial).FullName, "Reset");
            }

            TutorialActiveIdPatchHealth.ReportTutorialResetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            TutorialActiveIdRepair.ClearAfterTutorialReset();
        }
    }

    [HarmonyPatch]
    internal static class TutorialHideActiveId_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Tutorial_Window),
                nameof(Tutorial_Window.Hide),
                new[] { typeof(Action) });

            if (method == null || method.ReturnType != typeof(void))
            {
                TutorialActiveIdPatchHealth.ReportFailure(
                    "Tutorial_Window.Hide(Action) could not be resolved with the audited public void signature");
                throw new MissingMethodException(
                    typeof(Tutorial_Window).FullName,
                    nameof(Tutorial_Window.Hide));
            }

            TutorialActiveIdPatchHealth.ReportTutorialHideResolved();
            return method;
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Tutorial_Window __instance)
        {
            TutorialActiveIdRepair.ClearBeforeTutorialHide(__instance);
        }
    }

    [HarmonyPatch]
    internal static class BusinessTutorialOverride_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(agency._room),
                "DoBusiness",
                Type.EmptyTypes);

            if (method == null || method.IsStatic || method.ReturnType != typeof(void))
            {
                TutorialActiveIdPatchHealth.ReportFailure(
                    "agency._room.DoBusiness() could not be resolved with the audited private instance void signature");
                throw new MissingMethodException(typeof(agency._room).FullName, "DoBusiness");
            }

            TutorialActiveIdPatchHealth.ReportDoBusinessResolved();
            return method;
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix()
        {
            TutorialActiveIdRepair.NormalizeBeforeBusinessResolution();
        }
    }
}
