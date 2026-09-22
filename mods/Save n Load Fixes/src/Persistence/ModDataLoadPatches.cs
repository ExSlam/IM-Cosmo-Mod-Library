using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Persistence
{
    [HarmonyPatch(typeof(MainMenu_LoadGameManager), "StartNewGame")]
    internal static class ModDataNewCareer_Patch
    {
        [HarmonyPrefix]
        private static void Prefix() { ModDataStorage.ClearActive(); }
    }

    [HarmonyPatch]
    internal static class ModDataCareerLoad_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(SaveManager), "LoadData", new[] { typeof(string) });
            yield return AccessTools.Method(typeof(SaveManager), "LoadData", new[] { typeof(bool) });
        }
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SaveManager __instance) { ModDataStorage.RestoreLoaded(__instance.Data); }
    }
}
