using HarmonyLib;
using StaffPortraits.EmbeddedIMUiFramework;

namespace StaffPortraits
{
    /// <summary>
    /// Initializes the embedded UI framework after PopupManager has created the vanilla popup registry.
    /// Build the editor only when its action is opened: Settings-tab button templates are not
    /// guaranteed to exist yet during PopupManager.Start.
    /// </summary>
    [HarmonyPatch(typeof(PopupManager), StaffPortraitsConstants.PopupManagerStartMethodName)]
    internal static class PopupManager_Start_StaffPortraitsPatch
    {
        private static void Postfix(PopupManager __instance)
        {
            IMUiKit.Initialize(__instance);
        }
    }

    /// <summary>
    /// Vanilla staff hires normally keep their fixed staff portrait. After a normal non-idol,
    /// non-unique staff hire completes, Staff Portraits assigns a staff-only composite portrait
    /// when at least one valid staff portrait pack is available.
    /// </summary>
    [HarmonyPatch(typeof(staff), nameof(staff.Hire))]
    internal static class staff_Hire_StaffPortraitsPatch
    {
        private static void Postfix(staff._staff Staffer)
        {
            StaffPortraitAssignment.TryAssignIfMissing(Staffer);
        }
    }

    /// <summary>
    /// Existing saves can contain ordinary staff created before Staff Portraits was installed.
    /// Vanilla load leaves those fixed portraits unchanged, so this patch backfills only eligible
    /// staff members who do not already have a composite portrait after staff loading finishes.
    /// </summary>
    [HarmonyPatch(typeof(staff), StaffPortraitsConstants.StaffLoadFunctionMethodName)]
    internal static class staff_LoadFunction_StaffPortraitsPatch
    {
        private static void Postfix()
        {
            StaffPortraitAssignment.BackfillEligibleStaff();
        }
    }

    /// <summary>
    /// Vanilla saves composite portrait parts by calling _textureAsset.GetID(). Staff Portraits
    /// keeps its private staff-only assets out of the global idol texture pool, so this patch returns
    /// a stable namespaced ID only for runtime assets owned by Staff Portraits. All other texture
    /// assets keep their original vanilla or third-party ID behavior.
    /// </summary>
    [HarmonyPatch(typeof(data_girls_textures._textureAsset), StaffPortraitsConstants.TextureAssetGetIdMethodName)]
    internal static class textureAsset_GetID_StaffPortraitsPatch
    {
        private static bool Prefix(data_girls_textures._textureAsset __instance, ref string __result)
        {
            string stableId;
            if (!StaffPortraitCatalog.TryGetStableId(__instance, out stableId))
            {
                return true;
            }

            __result = stableId;
            return false;
        }
    }

    /// <summary>
    /// Vanilla load resolves saved portrait IDs through the global idol texture list. Staff Portraits
    /// deliberately does not register staff assets there, so this prefix resolves only IDs carrying
    /// the Staff Portraits namespace and leaves every other lookup to vanilla.
    /// </summary>
    [HarmonyPatch(typeof(data_girls_textures), StaffPortraitsConstants.TextureAssetLookupMethodName)]
    internal static class data_girls_textures_GetTextureAssetByID_StaffPortraitsPatch
    {
        private static bool Prefix(
            string id,
            data_girls_textures._spriteType spr_type,
            ref data_girls_textures._textureAsset __result)
        {
            data_girls_textures._textureAsset resolved;
            if (!StaffPortraitCatalog.TryResolveStableId(id, spr_type, out resolved))
            {
                return true;
            }

            __result = resolved;
            return false;
        }
    }
}
