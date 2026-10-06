using HarmonyLib;

namespace HarmonyModBrowser
{
    [HarmonyPatch(typeof(Mods_Popup), "Render")]
    internal static class ModsPopupRenderPatch
    {
        private static bool Prefix(Mods_Popup __instance)
        {
            try
            {
                ModBrowserController controller = ModBrowserController.GetOrCreate(__instance);
                return controller == null || !controller.BeginRender();
            }
            catch (System.Exception ex)
            {
                HmbLog.Warning("Enhanced Mods browser render failed; falling back to the vanilla renderer. " + ex.Message);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Mod_Button), "RenderScreenshot")]
    internal static class ModButtonScreenshotPatch
    {
        private static bool Prefix(Mod_Button __instance)
        {
            return !ModBrowserController.IsManaged(__instance);
        }
    }

    [HarmonyPatch(typeof(Mod_Button), "RenderTooltips")]
    internal static class ModButtonTooltipsPatch
    {
        private static bool Prefix(Mod_Button __instance)
        {
            return !ModBrowserController.IsManaged(__instance);
        }
    }

    [HarmonyPatch(typeof(Mods), "LoadMods")]
    internal static class ModsLoadOwnershipPatch
    {
        private static void Prefix()
        {
            SteamOwnershipMonitor.Reset();
        }

        private static void Postfix()
        {
            HmbGlyphWarmup.QueueLoadedMods();
        }
    }

    [HarmonyPatch(typeof(Mods), "OnSteamRequestDone")]
    internal static class ModsSteamDetailsCompletedPatch
    {
        private static void Postfix()
        {
            SteamOwnershipMonitor.NotifyCompleted();
            HmbGlyphWarmup.QueueLoadedMods();
        }
    }

    [HarmonyPatch(typeof(Mods), "OnSteamRequestFailed")]
    internal static class ModsSteamDetailsFailedPatch
    {
        private static void Postfix()
        {
            SteamOwnershipMonitor.NotifyFailed();
        }
    }

    [HarmonyPatch(typeof(Mods_Upload_Update), "Set")]
    internal static class UpdateSourceSearchPatch
    {
        private static void Postfix(Mods_Upload_Update __instance)
        {
            try
            {
                UpdateSourceSearchController.Attach(__instance);
            }
            catch (System.Exception ex)
            {
                HmbLog.Warning("Could not attach search to the local-version update list; keeping the vanilla list. " + ex.Message);
            }
        }
    }
}
