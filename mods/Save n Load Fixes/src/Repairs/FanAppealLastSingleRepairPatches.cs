using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    [HarmonyPatch]
    internal static class SinglesLoadFanAppealLastSingle_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(singles), nameof(singles.LoadFunction), Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                FanAppealLastSinglePatchHealth.ReportFailure("singles.LoadFunction() could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(singles).FullName, "LoadFunction");
            }
            FanAppealLastSinglePatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix()
        {
            FanAppealLastSingleRepair.ClearBeforeVanillaSinglesLoad();
        }
    }

    [HarmonyPatch]
    internal static class SinglesReleaseFanAppealLastSingle_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(singles),
                nameof(singles.ReleaseSingle),
                new Type[] { typeof(singles._single) });
            if (method == null || method.ReturnType != typeof(void))
            {
                FanAppealLastSinglePatchHealth.ReportFailure("singles.ReleaseSingle(_single) could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(singles).FullName, "ReleaseSingle");
            }
            FanAppealLastSinglePatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(singles._single single)
        {
            FanAppealLastSingleRepair.ObserveVanillaRelease(single);
        }
    }

    [HarmonyPatch]
    internal static class SaveManagerLoadPathFanAppealLastSingle_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(SaveManager),
                nameof(SaveManager.LoadData),
                new Type[] { typeof(string) });
            if (method == null || method.ReturnType != typeof(void))
            {
                FanAppealLastSinglePatchHealth.ReportFailure("SaveManager.LoadData(string) could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(SaveManager).FullName, "LoadData(string)");
            }
            FanAppealLastSinglePatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(SaveManager __instance)
        {
            FanAppealLastSingleRepair.RestoreAfterCareerLoad(__instance);
        }
    }

    [HarmonyPatch]
    internal static class SaveManagerLoadSlotFanAppealLastSingle_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(SaveManager),
                nameof(SaveManager.LoadData),
                new Type[] { typeof(bool) });
            if (method == null || method.ReturnType != typeof(void))
            {
                FanAppealLastSinglePatchHealth.ReportFailure("SaveManager.LoadData(bool) could not be resolved with the audited void signature.");
                throw new MissingMethodException(typeof(SaveManager).FullName, "LoadData(bool)");
            }
            FanAppealLastSinglePatchHealth.ReportTargetResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(SaveManager __instance)
        {
            FanAppealLastSingleRepair.RestoreAfterCareerLoad(__instance);
        }
    }
}
