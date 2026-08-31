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
            FanAppealLastSinglePatchHealth.ReportTargetResolved("singles.LoadFunction()");
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
            FanAppealLastSinglePatchHealth.ReportTargetResolved(
                "singles.ReleaseSingle(singles._single)");
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
            FanAppealLastSinglePatchHealth.ReportTargetResolved(
                "SaveManager.LoadData(string)");
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
            FanAppealLastSinglePatchHealth.ReportTargetResolved(
                "SaveManager.LoadData(bool)");
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(SaveManager __instance)
        {
            FanAppealLastSingleRepair.RestoreAfterCareerLoad(__instance);
        }
    }

    /// <summary>
    /// Groups.LoadFunction deliberately clears the static group list during the
    /// career LoadEvent, then Groups.Update calls the private _Load method after
    /// three frames to reconstruct memberships. Pre-A29 compatibility synthesis
    /// needs that completed membership graph for _single.GetSenbatsuStats().
    /// </summary>
    [HarmonyPatch]
    internal static class GroupsLateLoadFanAppealLastSingle_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Groups),
                "_Load",
                Type.EmptyTypes);
            if (method == null || method.ReturnType != typeof(void))
            {
                FanAppealLastSinglePatchHealth.ReportFailure(
                    "Groups._Load() could not be resolved with the audited private void signature.");
                throw new MissingMethodException(typeof(Groups).FullName, "_Load");
            }
            FanAppealLastSinglePatchHealth.ReportTargetResolved("Groups._Load()");
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            FanAppealLastSingleRepair.CompleteDeferredLegacyCompatibilityAfterGroupsLoad();
        }
    }
}
