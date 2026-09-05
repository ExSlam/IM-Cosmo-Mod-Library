using HarmonyLib;

namespace IMDataCore
{
    [HarmonyPatch(typeof(Activities._activity), nameof(Activities._activity.LevelUp))]
    internal static class Activities_activity_LevelUp_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Activities._activity __instance, out int __state)
        {
            __state = __instance != null ? __instance.lvl : CoreConstants.InvalidIdValue;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Activities._activity __instance, int __state)
        {
            IMDataCoreController.Instance.CaptureActivityLevelUp(__instance, __state);
        }
    }

    [HarmonyPatch(typeof(tasks), nameof(tasks.AddTask_SummerGames))]
    internal static class tasks_AddTask_SummerGames_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(string str, out SummerGamesObjectiveSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateSummerGamesObjectiveSnapshot(str);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SummerGamesObjectiveSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureSummerGamesObjectiveActivated(__state);
        }
    }

    [HarmonyPatch(typeof(SEvent_Tour.country), nameof(SEvent_Tour.country.LevelUp))]
    internal static class SEvent_Tour_country_LevelUp_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(SEvent_Tour.country __instance, out int __state)
        {
            __state = __instance != null ? __instance.Level : CoreConstants.InvalidIdValue;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEvent_Tour.country __instance, int __state)
        {
            IMDataCoreController.Instance.CaptureTourCountryLevelUp(__instance, __state);
        }
    }

    [HarmonyPatch(typeof(vn_actions), "DoCustom")]
    internal static class vn_actions_DoCustom_IMDataCoreStoryStatusRestore_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(string formula, out StoryStatusRestoreSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateStoryStatusRestoreSnapshot(formula);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(StoryStatusRestoreSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureStoryStatusRestorations(__state);
        }
    }
}
