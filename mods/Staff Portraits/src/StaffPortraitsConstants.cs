namespace StaffPortraits
{
    internal static class StaffPortraitsConstants
    {
        internal const string HarmonyId = "com.cosmo.staffportraits";
        internal const string LogPrefix = "[StaffPortraits] ";
        internal const string StaffPortraitDirectoryName = "StaffPortraits";
        internal const string PackFileName = "pack.json";
        internal const string AssetIdPrefix = "staffportraits:1:";
        internal const string StableIdSeparator = ":";
        internal const string PopupManagerStartMethodName = "Start";
        internal const string StaffLoadFunctionMethodName = "LoadFunction";
        internal const string TextureAssetGetIdMethodName = "GetID";
        internal const string TextureAssetLookupMethodName = "GetTextureAssetByID";
        internal const string StaffTextureRefreshMethodName = "UpdateTextureData";

        internal const int PackFormatVersion = 1;
        internal const int PopupTypeValue = 1431194261;
        // Idol Manager's composite portrait renderer is authored around the idol portrait canvas.
        // Staff-only layers use the same canvas so body/face/hair/accessory parts align predictably.
        internal const int PortraitCanvasWidthPixels = 1024;
        internal const int PortraitCanvasHeightPixels = 1500;
        internal const int MaximumPortraitPngBytes = 33554432;
        internal const int MaximumPackJsonBytes = 1048576;
        internal const int MaximumAssetsPerPack = 512;
        internal const int MaximumDisplayTextCharacters = 128;
        internal const int MaximumIdentifierCharacters = 96;
        internal const int PngHeaderBytes = 24;
        internal const int PngWidthOffset = 16;
        internal const int PngHeightOffset = 20;
        internal const int Int32ByteCount = 4;
        internal const int BitsPerByte = 8;
        internal const int StableHashSeed = 17;
        internal const int StableHashMultiplier = 31;
        internal const int MinimumPositiveHashValue = 1;
        internal const int NoSelectionIndex = -1;
        internal const int FirstIndex = 0;
        internal const int PreviousSelectionOffset = -1;
        internal const int NextSelectionOffset = 1;
        internal const int AccessoryNoneSelectionIndex = -1;

        internal const float PopupWidth = 1040f;
        internal const float PopupHeight = 700f;
        internal const float PortraitSpritePixelsPerUnit = 100f;
        internal const int PreviewLayerCount = 4;
        internal const int PreviewTextureBootstrapPixels = 2;
        internal const int SpriteExtrudePixels = 0;

        internal const string TitleKey = "ui.title";
        internal const string CloseKey = "common.close";
        internal const string PreviousStaffKey = "ui.staff.previous";
        internal const string NextStaffKey = "ui.staff.next";
        internal const string PackLabelKey = "ui.selector.pack";
        internal const string BodyLabelKey = "ui.selector.body";
        internal const string HairLabelKey = "ui.selector.hair";
        internal const string FaceLabelKey = "ui.selector.face";
        internal const string AccessoryLabelKey = "ui.selector.accessory";
        internal const string NoneLabelKey = "ui.selector.none";
        internal const string RandomizeKey = "ui.button.randomize";
        internal const string ApplyKey = "ui.button.apply";
        internal const string NoStaffKey = "ui.state.no_staff";
        internal const string ProtectedPortraitKey = "ui.state.protected_portrait";
        internal const string JobUnavailableKey = "ui.staff.job_unavailable";
        internal const string NoPacksKey = "ui.state.no_packs";
        internal const string PackEmptyKey = "ui.state.pack_empty";
        internal const string AppliedNotificationKey = "notification.applied";
        internal const string ActionHubLabelKey = "actionhub.staff_stylist.label";
        internal const string ActionHubTooltipKey = "actionhub.staff_stylist.tooltip";

        internal const string TitleFallback = "Staff Stylist";
        internal const string CloseFallback = "Close";
        internal const string PreviousStaffFallback = "Previous staff member";
        internal const string NextStaffFallback = "Next staff member";
        internal const string PackLabelFallback = "Portrait Pack";
        internal const string BodyLabelFallback = "Outfit";
        internal const string HairLabelFallback = "Hair";
        internal const string FaceLabelFallback = "Face";
        internal const string AccessoryLabelFallback = "Accessory";
        internal const string NoneLabelFallback = "None";
        internal const string RandomizeFallback = "Randomize";
        internal const string ApplyFallback = "Apply";
        internal const string NoStaffFallback = "No staff members are available.";
        internal const string ProtectedPortraitFallback = "This staff member keeps their original portrait. You can view their staff card, but cannot change their portrait here.";
        internal const string JobUnavailableFallback = "Job unavailable";
        internal const string NoPacksFallback = "No staff portrait packs are installed. Enable a staff portrait pack to customize this portrait.";
        internal const string PackEmptyFallback = "This portrait pack does not contain the required body, hair, and face parts.";
        internal const string AppliedNotificationFallback = "Updated {0}'s staff portrait.";
    }
}
