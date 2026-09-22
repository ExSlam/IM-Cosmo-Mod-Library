using System;
using HarmonyLib;

namespace SaveNLoadFixes.Transport
{
    [HarmonyPatch(typeof(mainScript), "Update")]
    internal static class MainScript_Update_SaveProgressNotifications_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            SaveShutdownCoordinator.EnsurePump();
            SaveProgressCoordinator.PumpNotifications();
        }
    }
}
