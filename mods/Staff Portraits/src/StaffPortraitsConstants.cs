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
        internal const float EditorHeight = 548f;
        internal const float PreviewWidth = 330f;
        internal const float PreviewHeight = 470f;
        internal const float SelectorAreaWidth = 570f;
        internal const float SelectorAreaHeight = 470f;
        internal const float SelectorRowHeight = 58f;
        internal const float SelectorRowSpacing = 8f;
        internal const float SelectorLabelWidth = 135f;
        internal const float SelectorValueWidth = 275f;
        internal const float SelectorArrowSize = 40f;
        internal const float StaffNavigationButtonWidth = 46f;
        internal const float StaffNavigationButtonHeight = 38f;
        internal const float HeaderNameHeight = 34f;
        internal const float HeaderRoleHeight = 24f;
        internal const float ActionButtonWidth = 190f;
        internal const float ActionButtonHeight = 40f;
        internal const float ActionButtonSpacing = 18f;
        internal const float OuterInset = 18f;
        internal const float ColumnGap = 22f;
        internal const float PortraitSpritePixelsPerUnit = 100f;
        internal const float SelectorFontSize = 17f;
        internal const float SelectorValueFontSize = 16f;
        internal const float StaffNameFontSize = 25f;
        internal const float StaffRoleFontSize = 16f;
        internal const float StateMessageFontSize = 20f;
        internal const float EditorWidthInset = 70f;
        internal const float HeaderNameYOffset = -2f;
        internal const float HeaderTextWidth = 760f;
        internal const float HeaderRoleYOffset = -38f;
        internal const float StaffNavigationXOffset = 430f;
        internal const float StaffNavigationYOffset = -13f;
        internal const float PreviewXOffset = -300f;
        internal const float MainCardsYOffset = -76f;
        internal const float PreviewContentInset = 20f;
        internal const float SelectorXOffset = 180f;
        internal const float SelectorInitialTop = -24f;
        internal const float SelectorInnerHorizontalInset = 30f;
        internal const float SelectorLabelXOffset = -190f;
        internal const float SelectorPreviousXOffset = 180f;
        internal const float SelectorNextXOffset = 228f;
        internal const float ActionButtonsYOffset = -520f;
        internal const float StateMessageYOffset = -210f;
        internal const float StateMessageWidth = 780f;
        internal const float StateMessageHeight = 120f;
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
        internal const string NoStaffFallback = "No editable staff members are available.";
        internal const string NoPacksFallback = "No staff portrait packs are installed. Add an enabled mod containing StaffPortraits/pack.json.";
        internal const string PackEmptyFallback = "This portrait pack does not contain the required body, hair, and face parts.";
        internal const string AppliedNotificationFallback = "Updated {0}'s staff portrait.";
    }
}
