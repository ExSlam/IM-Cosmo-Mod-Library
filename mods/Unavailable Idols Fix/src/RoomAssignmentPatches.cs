using HarmonyLib;

namespace UnavailableIdolsFix
{
    /// <summary>
    /// Graduation is stored in the same vanilla status field as room work. An idol who
    /// announces graduation while practicing therefore keeps her room assignment while
    /// losing the practice status that GirlButtonSmall uses to render its cancel button.
    /// Render from the authoritative room pointer without replacing the graduation status.
    /// </summary>
    [HarmonyPatch(typeof(GirlButtonSmall), "RenderStatus")]
    internal static class AnnouncedGraduationRoomCancelButtonPatch
    {
        private static void Postfix(GirlButtonSmall __instance)
        {
            if (__instance == null || !__instance.LeftTab || __instance.girl == null ||
                __instance.girl.status != data_girls._status.announced_graduation ||
                __instance.girl.room == null || __instance.Button_Drag == null ||
                __instance.Button_Cancel == null)
            {
                return;
            }

            __instance.Button_Drag.SetActive(false);
            __instance.Button_Cancel.SetActive(true);
        }
    }
}
