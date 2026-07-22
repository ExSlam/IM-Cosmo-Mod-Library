using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ModLocalizationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace CheatsMod
{
    internal static class UniqueIdolRecruitmentPopup
    {
        private const int UniqueIdolRecruitmentPopupTypeValue = 1431194192;
        private const int ZeroCount = 0;
        private const int SortLeftAfterRight = 1;
        private const int SortLeftBeforeRight = -1;
        private const int FirstItemIndex = 0;
        private const int FirstTemplateIndex = 0;
        private const int PreviousPartSelectionOffset = -1;
        private const int NextPartSelectionOffset = 1;
        private const int MinimumGridColumns = 1;
        private const int MaximumDisplayedPotential = 100;
        private const int StatColumnItemCount = 4;
        private const int PortraitLoadTimeoutSeconds = 10;
        private const int PngDimensionHeaderLength = 24;
        private const int PngWidthByteOffset = 16;
        private const int PngHeightByteOffset = 20;
        private const int PngDimensionByteCount = 4;
        private const int BitsPerByte = 8;
        private const int MinimumMeaningfulTextureDimension = 2;
        private const int FallbackIdolMinimumAge = 16;
        private const int FallbackIdolMaximumAgeExclusive = 26;
        private const int NormalFallbackStatValue = 40;
        private const int NormalFallbackPotentialValue = 75;
        private const int SilverFallbackStatValue = 55;
        private const int SilverFallbackPotentialValue = 85;
        private const int GoldFallbackStatValue = 70;
        private const int GoldFallbackPotentialValue = 95;
        private const int PlatinumFallbackStatValue = 85;
        private const int PlatinumFallbackPotentialValue = 100;
        private const int RandomBirthMonthUpperBoundExclusive = 11;
        private const int RandomBirthDayUpperBoundExclusive = 25;
        private const int TransparentColorChannel = 0;
        private const int TransparentColorAlpha = 0;
        private const int SolidColorAlpha = 255;
        private const int PanelBackgroundColorChannel = 245;
        private const int ContentBackgroundColorChannel = 238;
        private const int CardBackgroundColorChannel = 250;
        private const int MutedTextColorChannel = 92;
        private const int PlatinumCardPrimaryTextColorChannel = 238;
        private const int PlatinumCardSecondaryTextColorChannel = 204;
        private const int AlreadyRecruitedAlpha = 135;
        private const int ScrollbarTrackAlpha = 180;
        private const int CommonFrameRed = 91;
        private const int CommonFrameGreen = 97;
        private const int CommonFrameBlue = 190;
        private const int SilverFrameColorChannel = 145;
        private const int GoldFrameRed = 222;
        private const int GoldFrameGreen = 192;
        private const int GoldFrameBlue = 119;
        private const int PlatinumFrameColorChannel = 178;
        private const int RandomFrameRed = 130;
        private const int RandomFrameGreen = 104;
        private const int RandomFrameBlue = 190;
        private const int TitleFontSize = 34;
        private const int CloseButtonFontSize = 20;
        private const int RarityFontSize = 14;
        private const int NameFontSize = 15;
        private const int AgeFontSize = 13;
        private const int StatFontSize = 10;
        private const int StatusFontSize = 12;
        private const int SearchInputFontSize = 16;
        private const int SelectorLabelFontSize = 12;
        private const int SelectorArrowFontSize = 18;
        private const int ContentPaddingLeft = 14;
        private const int ContentPaddingRight = 14;
        private const int ContentPaddingTop = 14;
        private const int ContentPaddingBottom = 18;

        private const float HiddenAlpha = 0f;
        private const float VisibleAlpha = 1f;
        private const float CenterAnchor = 0.5f;
        private const float EdgeAnchor = 1f;
        private const float BaseAnchor = 0f;
        private const float MinimumPanelWidth = 760f;
        private const float MaximumPanelWidth = 2200f;
        private const float MinimumPanelHeight = 360f;
        private const float MaximumPanelHeight = 1050f;
        private const float FallbackScreenWidth = 1280f;
        private const float FallbackScreenHeight = 720f;
        private const float MaximumPanelScreenWidthRatio = 0.90f;
        private const float MaximumPanelScreenHeightRatio = 0.90f;
        private const float MinimumVerticalScreenMargin = 42f;
        private const float PanelVerticalOffset = -4f;
        private const float PanelVerticalChromeHeight = 198f;
        private const float TitleWidth = 900f;
        private const float TitleHeight = 44f;
        private const float TitleVerticalOffset = -18f;
        private const float SearchBarOffsetLeft = 42f;
        private const float SearchBarOffsetRight = -46f;
        private const float SearchBarOffsetTop = -68f;
        private const float SearchBarOffsetBottom = -108f;
        private const float SearchTextHorizontalPadding = 12f;
        private const float SearchTextVerticalPadding = 3f;
        private const float ScrollViewOffsetLeft = 28f;
        private const float ScrollViewOffsetRight = -32f;
        private const float ScrollViewOffsetTop = -122f;
        private const float ScrollViewOffsetBottom = 64f;
        private const float ScrollbarViewportReserve = 30f;
        private const float ScrollSensitivity = 34f;
        private const float ScrollbarWidth = 12f;
        private const float ScrollbarOffsetX = -6f;
        private const float ScrollbarSpacing = 8f;
        private const float TileWidth = 320f;
        private const float TileHeight = 692f;
        private const float TileSpacingX = 20f;
        private const float TileSpacingY = 20f;
        private const float InnerCardInset = 6f;
        private const float RarityLabelTop = -6f;
        private const float RarityLabelHeight = 22f;
        private const float NameTop = -30f;
        private const float NameHeight = 28f;
        private const float AgeTop = -60f;
        private const float AgeHeight = 18f;
        private const float PortraitTop = -82f;
        private const float PortraitWidth = 270f;
        private const float PortraitHeight = 390f;
        private const float SelectorTop = -476f;
        private const float SelectorRowHeight = 20f;
        private const float SelectorRowSpacing = 2f;
        private const float SelectorHorizontalInset = 18f;
        private const float SelectorArrowButtonWidth = 34f;
        private const float StatsTop = -568f;
        private const float StatsHeight = 48f;
        private const float StatColumnWidth = 126f;
        private const float StatColumnSpacing = 8f;
        private const float RecruitButtonWidth = 250f;
        private const float RecruitButtonHeight = 38f;
        private const float RecruitButtonBottom = 12f;
        private const float CloseButtonWidth = 170f;
        private const float CloseButtonHeight = 38f;
        private const float CloseButtonOffsetY = 12f;
        private const float OutlineDistance = 2f;
        private const float DisabledColorMultiplier = 0.72f;
        private const float PortraitSourceCanvasWidth = 1024f;
        private const float PortraitSourceCanvasHeight = 1500f;
        private const float PortraitCanvasDisplayWidth =
            PortraitHeight * PortraitSourceCanvasWidth / PortraitSourceCanvasHeight;
        private const float PortraitSpritePixelsPerUnit = 100f;

        private const string PopupName = "CheatsModUniqueIdolRecruitmentPopup";
        private const string PanelObjectName = "UniqueIdolRecruitmentPanel";
        private const string TitleObjectName = "Title";
        private const string SearchInputObjectName = "SearchInput";
        private const string SearchTextObjectName = "SearchText";
        private const string SearchPlaceholderObjectName = "SearchPlaceholder";
        private const string ScrollViewObjectName = "ScrollView";
        private const string ViewportObjectName = "Viewport";
        private const string ContentObjectName = "Content";
        private const string ScrollbarObjectName = "Scrollbar";
        private const string ScrollbarHandleObjectName = "Handle";
        private const string TileSlotObjectNamePrefix = "UniqueIdolSlot_";
        private const string TileObjectNamePrefix = "UniqueIdolTile_";
        private const string InnerCardObjectName = "InnerCard";
        private const string RarityBackgroundObjectName = "RarityBackground";
        private const string RarityLabelObjectName = "Rarity";
        private const string PortraitObjectName = "Portrait";
        private const string PortraitLayerObjectNamePrefix = "PortraitLayer_";
        private const string SelectorRowObjectNamePrefix = "SelectorRow_";
        private const string SelectorPreviousButtonObjectName = "Previous";
        private const string SelectorNextButtonObjectName = "Next";
        private const string SelectorLabelObjectName = "Label";
        private const string NameObjectName = "Name";
        private const string AgeObjectName = "Age";
        private const string StatLeftObjectName = "StatsLeft";
        private const string StatRightObjectName = "StatsRight";
        private const string RecruitButtonObjectName = "Recruit";
        private const string CloseButtonObjectName = "Close";
        private const string ButtonTextObjectName = "Text";
        private const string UniqueAssetKeySeparator = "|";
        private const string EmptyString = "";
        private const string DefaultTextureAssetModName = "";
        private const string DefaultModName = "Default";
        private const string SilverRarityValue = "silver";
        private const string GoldenRarityValue = "gold";
        private const string PlatinumRarityValue = "platinum";
        private const string CommonRarityValue = "common";
        private const string NormalRarityValue = "normal";
        private const string RandomRarityValue = "random";
        private const string StatLineBreak = "\n";
        private const string NamePartSeparator = " ";
        private const string SelectorPreviousSymbol = "<";
        private const string SelectorNextSymbol = ">";
        private const string UniqueRecruitmentLogFormat = "[CheatsMod] Unique idol recruitment popup failed: {0}";
        private const string UniquePortraitLoadLogFormat = "[CheatsMod] Could not load unique idol portrait from {0}: {1}";

        private const string ButtonRecruitUniqueIdolKey = "cheat.button.recruit_unique_idol";
        private const string ButtonRecruitUniqueIdolFallback = "Recruit unique idol";
        private const string TooltipRecruitUniqueIdolKey = "cheat.tooltip.recruit_unique_idol";
        private const string TooltipRecruitUniqueIdolFallback = "Open a picker to recruit any loaded unique idol.";
        private const string PopupTitleKey = "ui.unique_idols.title";
        private const string PopupTitleFallback = "Recruit Unique Idol";
        private const string SearchPlaceholderKey = "ui.unique_idols.search_placeholder";
        private const string SearchPlaceholderFallback = "Search by idol or mod name";
        private const string CloseButtonKey = "ui.unique_idols.close";
        private const string CloseButtonFallback = "Close";
        private const string RecruitButtonKey = "ui.unique_idols.recruit";
        private const string RecruitButtonFallback = "Recruit";
        private const string AgeFormatKey = "ui.unique_idols.age_format";
        private const string AgeFormatFallback = "Age: {0}";
        private const string StatFormatKey = "ui.unique_idols.stat_format";
        private const string StatFormatFallback = "{0} {1}/{2}";
        private const string CommonRarityKey = "ui.unique_idols.rarity.common";
        private const string CommonRarityFallback = "Common";
        private const string SilverRarityKey = "ui.unique_idols.rarity.silver";
        private const string SilverRarityFallback = "Silver";
        private const string GoldRarityKey = "ui.unique_idols.rarity.gold";
        private const string GoldRarityFallback = "Gold";
        private const string PlatinumRarityKey = "ui.unique_idols.rarity.platinum";
        private const string PlatinumRarityFallback = "Platinum";
        private const string RandomRarityKey = "ui.unique_idols.rarity.random";
        private const string RandomRarityFallback = "Random";
        private const string RecruitedStatusKey = "ui.unique_idols.status.recruited";
        private const string RecruitedStatusFallback = "Recruited";
        private const string AlreadyRecruitedStatusKey = "ui.unique_idols.status.already_recruited";
        private const string AlreadyRecruitedStatusFallback = "Already recruited";
        private const string StatCuteKey = "ui.unique_idols.stat.cute";
        private const string StatCuteFallback = "Cute";
        private const string StatCoolKey = "ui.unique_idols.stat.cool";
        private const string StatCoolFallback = "Cool";
        private const string StatSexyKey = "ui.unique_idols.stat.sexy";
        private const string StatSexyFallback = "Sexy";
        private const string StatPrettyKey = "ui.unique_idols.stat.pretty";
        private const string StatPrettyFallback = "Pretty";
        private const string StatDanceKey = "ui.unique_idols.stat.dance";
        private const string StatDanceFallback = "Dance";
        private const string StatVocalKey = "ui.unique_idols.stat.vocal";
        private const string StatVocalFallback = "Vocal";
        private const string StatFunnyKey = "ui.unique_idols.stat.funny";
        private const string StatFunnyFallback = "Funny";
        private const string StatSmartKey = "ui.unique_idols.stat.smart";
        private const string StatSmartFallback = "Smart";
        private const string HairPartKey = "ui.unique_idols.part.hair";
        private const string HairPartFallback = "Hair";
        private const string FacePartKey = "ui.unique_idols.part.face";
        private const string FacePartFallback = "Face";
        private const string BodyPartKey = "ui.unique_idols.part.body";
        private const string BodyPartFallback = "Body";
        private const string AccessoryPartKey = "ui.unique_idols.part.accessory";
        private const string AccessoryPartFallback = "Accessory";
        private const string NoPartSelectionKey = "ui.unique_idols.part.none";
        private const string NoPartSelectionFallback = "None";
        private const string PartSelectionFormatKey = "ui.unique_idols.part.selection_format";
        private const string PartSelectionFormatFallback = "{0} {1}/{2}";
        private const string NoPartSelectionFormatKey = "ui.unique_idols.part.none_format";
        private const string NoPartSelectionFormatFallback = "{0}: {1}";
        private const string NotificationNoUniqueIdolsKey = "notification.no_unique_idols";
        private const string NotificationNoUniqueIdolsFallback = "No loaded unique idols found.";
        private const string NotificationUniqueIdolRecruitedKey = "notification.unique_idol_recruited";
        private const string NotificationUniqueIdolRecruitedFallback = "{0} recruited.";
        private const string NotificationUniqueIdolAlreadyRecruitedKey = "notification.unique_idol_already_recruited";
        private const string NotificationUniqueIdolAlreadyRecruitedFallback = "That unique idol is already recruited.";
        private const string NotificationUniqueIdolRecruitFailedKey = "notification.unique_idol_recruit_failed";
        private const string NotificationUniqueIdolRecruitFailedFallback = "Unique idol recruitment failed.";

        private static readonly data_girls._paramType[] DisplayedStatTypes = new data_girls._paramType[]
        {
            data_girls._paramType.cute,
            data_girls._paramType.cool,
            data_girls._paramType.sexy,
            data_girls._paramType.pretty,
            data_girls._paramType.dance,
            data_girls._paramType.vocal,
            data_girls._paramType.funny,
            data_girls._paramType.smart
        };

        private static readonly data_girls_textures._spriteType[] PortraitRenderPartTypes =
            new data_girls_textures._spriteType[]
            {
                data_girls_textures._spriteType.body,
                data_girls_textures._spriteType.face,
                data_girls_textures._spriteType.hair,
                data_girls_textures._spriteType.acc
            };

        private static readonly data_girls_textures._spriteType[] PortraitSelectorPartTypes =
            new data_girls_textures._spriteType[]
            {
                data_girls_textures._spriteType.hair,
                data_girls_textures._spriteType.face,
                data_girls_textures._spriteType.body,
                data_girls_textures._spriteType.acc
            };

        private static readonly data_girls_textures._spriteType[] RequiredRecruitmentPartTypes =
            new data_girls_textures._spriteType[]
            {
                data_girls_textures._spriteType.body,
                data_girls_textures._spriteType.hair,
                data_girls_textures._spriteType.face
            };

        private static GameObject popupRoot;
        private static TextMeshProUGUI defaultFontSource;
        private static Audition_Golden_Card rarityFrameTemplate;

        internal static void Open()
        {
            try
            {
                PopupManager popupManager = GetPopupManager();
                data_girls dataGirls = GetDataComponent<data_girls>();
                data_girls_textures textureData = GetDataComponent<data_girls_textures>();
                if (popupManager == null || dataGirls == null || textureData == null)
                {
                    NotifyWarning(CheatLocalizationKeys.NotificationGameUnavailable, CheatFallbackText.NotificationGameUnavailable);
                    return;
                }

                List<UniqueIdolEntry> entries = BuildUniqueIdolEntries();
                if (entries.Count == ZeroCount)
                {
                    NotifyWarning(NotificationNoUniqueIdolsKey, NotificationNoUniqueIdolsFallback);
                    return;
                }

                if (!CreatePopup(popupManager, dataGirls, textureData, entries))
                {
                    NotifyWarning(NotificationUniqueIdolRecruitFailedKey, NotificationUniqueIdolRecruitFailedFallback);
                    return;
                }

                PopupManager.OpenPopup((PopupManager._type)UniqueIdolRecruitmentPopupTypeValue);
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(CultureInfo.InvariantCulture, UniqueRecruitmentLogFormat, ex.Message));
                NotifyWarning(NotificationUniqueIdolRecruitFailedKey, NotificationUniqueIdolRecruitFailedFallback);
            }
        }

        internal static string GetButtonLabel()
        {
            return GetLocalized(ButtonRecruitUniqueIdolKey, ButtonRecruitUniqueIdolFallback);
        }

        internal static string GetButtonTooltip()
        {
            return GetLocalized(TooltipRecruitUniqueIdolKey, TooltipRecruitUniqueIdolFallback);
        }

        private static List<UniqueIdolEntry> BuildUniqueIdolEntries()
        {
            List<UniqueIdolEntry> entries = new List<UniqueIdolEntry>();
            List<data_girls_textures._textureAsset> bodyAssets = GetLoadedUniqueBodyAssets();
            for (int assetIndex = 0; assetIndex < bodyAssets.Count; assetIndex++)
            {
                data_girls_textures._textureAsset bodyAsset = bodyAssets[assetIndex];
                if (bodyAsset == null)
                {
                    continue;
                }

                UniqueIdolEntry entry = new UniqueIdolEntry
                {
                    Asset = bodyAsset,
                    Rarity = bodyAsset.GetRarity(),
                    RarityValue = GetNormalizedRarityValue(bodyAsset),
                    Name = GetAssignedAssetName(bodyAsset),
                    ModName = string.IsNullOrEmpty(bodyAsset.ModName)
                        ? DefaultModName
                        : bodyAsset.ModName,
                    ModTitle = GetSupplyingModTitle(bodyAsset.ModName),
                    Index = entries.Count
                };
                entry.PortraitParts = BuildPortraitPartStates(entry);
                entries.Add(entry);
            }

            entries.Sort(CompareUniqueIdolEntries);
            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                entries[entryIndex].Index = entryIndex;
            }

            return entries;
        }

        private static string GetSupplyingModTitle(string assetModName)
        {
            if (Mods._Mods != null)
            {
                for (int modIndex = FirstItemIndex; modIndex < Mods._Mods.Count; modIndex++)
                {
                    Mods._mod mod = Mods._Mods[modIndex];
                    if (mod == null
                        || !string.Equals(
                            mod.ModName,
                            assetModName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    return string.IsNullOrEmpty(mod.Title) ? mod.ModName : mod.Title;
                }
            }

            return string.IsNullOrEmpty(assetModName) ? DefaultModName : assetModName;
        }

        private static List<PortraitPartState> BuildPortraitPartStates(UniqueIdolEntry entry)
        {
            List<PortraitPartState> portraitParts = new List<PortraitPartState>();
            if (entry == null || entry.Asset == null)
            {
                return portraitParts;
            }

            for (int partTypeIndex = FirstItemIndex;
                partTypeIndex < PortraitRenderPartTypes.Length;
                partTypeIndex++)
            {
                data_girls_textures._spriteType partType = PortraitRenderPartTypes[partTypeIndex];
                List<data_girls_textures._textureAsset> partAssets =
                    GetStylistPortraitPartAssets(entry.Asset, partType);
                if (partAssets.Count == ZeroCount || !ContainsMeaningfulPortraitPartAsset(partAssets))
                {
                    continue;
                }

                int selectedIndex = partType == data_girls_textures._spriteType.body
                    ? FindPortraitPartAssetIndex(partAssets, entry.Asset)
                    : FindFirstMeaningfulPortraitPartAssetIndex(partAssets);
                portraitParts.Add(new PortraitPartState
                {
                    Type = partType,
                    Assets = partAssets,
                    SelectedIndex = selectedIndex,
                    AllowNoSelection = partType == data_girls_textures._spriteType.acc
                });
            }

            return portraitParts;
        }

        private static List<data_girls_textures._textureAsset> GetStylistPortraitPartAssets(
            data_girls_textures._textureAsset uniqueBodyAsset,
            data_girls_textures._spriteType partType)
        {
            List<data_girls_textures._textureAsset> stylistAssets =
                new List<data_girls_textures._textureAsset>();
            if (uniqueBodyAsset == null)
            {
                return stylistAssets;
            }

            string stylistModName = uniqueBodyAsset.Add_To_Default
                ? DefaultTextureAssetModName
                : uniqueBodyAsset.ModName ?? DefaultTextureAssetModName;
            List<data_girls_textures._textureAsset> loadedAssets = data_girls_textures.GetTextureAssets(
                partType,
                uniqueBodyAsset.body_id,
                stylistModName);
            if (loadedAssets == null)
            {
                return stylistAssets;
            }

            for (int assetIndex = FirstItemIndex; assetIndex < loadedAssets.Count; assetIndex++)
            {
                data_girls_textures._textureAsset candidateAsset = loadedAssets[assetIndex];
                if (candidateAsset == null)
                {
                    continue;
                }

                stylistAssets.Add(candidateAsset);
            }

            return stylistAssets;
        }

        private static bool ContainsMeaningfulPortraitPartAsset(
            List<data_girls_textures._textureAsset> assets)
        {
            if (assets == null)
            {
                return false;
            }

            for (int assetIndex = FirstItemIndex; assetIndex < assets.Count; assetIndex++)
            {
                if (IsMeaningfulPortraitPartAsset(assets[assetIndex]))
                {
                    return true;
                }
            }

            return false;
        }

        private static int FindFirstMeaningfulPortraitPartAssetIndex(
            List<data_girls_textures._textureAsset> assets)
        {
            if (assets == null)
            {
                return FirstItemIndex;
            }

            for (int assetIndex = FirstItemIndex; assetIndex < assets.Count; assetIndex++)
            {
                if (IsMeaningfulPortraitPartAsset(assets[assetIndex]))
                {
                    return assetIndex;
                }
            }

            return FirstItemIndex;
        }

        private static bool IsMeaningfulPortraitPartAsset(
            data_girls_textures._textureAsset asset)
        {
            if (asset == null || string.IsNullOrEmpty(asset.path) || !File.Exists(asset.path))
            {
                return false;
            }

            int textureWidth;
            int textureHeight;
            return TryReadPngDimensions(asset.path, out textureWidth, out textureHeight)
                && textureWidth >= MinimumMeaningfulTextureDimension
                && textureHeight >= MinimumMeaningfulTextureDimension;
        }

        private static bool TryReadPngDimensions(
            string path,
            out int textureWidth,
            out int textureHeight)
        {
            textureWidth = ZeroCount;
            textureHeight = ZeroCount;
            try
            {
                byte[] header = new byte[PngDimensionHeaderLength];
                using (FileStream stream = File.OpenRead(path))
                {
                    if (stream.Read(header, FirstItemIndex, header.Length) != header.Length)
                    {
                        return false;
                    }
                }

                textureWidth = ReadBigEndianInt32(header, PngWidthByteOffset);
                textureHeight = ReadBigEndianInt32(header, PngHeightByteOffset);
                return textureWidth > ZeroCount && textureHeight > ZeroCount;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static int ReadBigEndianInt32(byte[] bytes, int offset)
        {
            int value = ZeroCount;
            for (int byteIndex = FirstItemIndex;
                byteIndex < PngDimensionByteCount;
                byteIndex++)
            {
                value = (value << BitsPerByte) | bytes[offset + byteIndex];
            }

            return value;
        }

        private static int ComparePortraitPartAssets(
            data_girls_textures._textureAsset left,
            data_girls_textures._textureAsset right)
        {
            if (ReferenceEquals(left, right))
            {
                return ZeroCount;
            }

            if (left == null)
            {
                return SortLeftAfterRight;
            }

            if (right == null)
            {
                return SortLeftBeforeRight;
            }

            int partIdComparison = left.part_id.CompareTo(right.part_id);
            return partIdComparison != ZeroCount
                ? partIdComparison
                : string.Compare(left.path, right.path, StringComparison.OrdinalIgnoreCase);
        }

        private static int FindPortraitPartAssetIndex(
            List<data_girls_textures._textureAsset> assets,
            data_girls_textures._textureAsset selectedAsset)
        {
            if (assets == null || assets.Count == ZeroCount)
            {
                return FirstItemIndex;
            }

            for (int assetIndex = FirstItemIndex; assetIndex < assets.Count; assetIndex++)
            {
                if (ReferenceEquals(assets[assetIndex], selectedAsset)
                    || string.Equals(
                        assets[assetIndex].path,
                        selectedAsset.path,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return assetIndex;
                }
            }

            return FirstItemIndex;
        }

        private static List<data_girls_textures._textureAsset> GetLoadedUniqueBodyAssets()
        {
            List<data_girls_textures._textureAsset> assets = new List<data_girls_textures._textureAsset>();
            HashSet<string> seenKeys = new HashSet<string>();
            AddUniqueBodyAssetsForMod(DefaultTextureAssetModName, assets, seenKeys);

            if (Mods._Mods != null)
            {
                for (int modIndex = 0; modIndex < Mods._Mods.Count; modIndex++)
                {
                    Mods._mod mod = Mods._Mods[modIndex];
                    if (mod == null || !mod.IsEnabled())
                    {
                        continue;
                    }

                    AddUniqueBodyAssetsForMod(mod.ModName, assets, seenKeys);
                }
            }

            return assets;
        }

        private static void AddUniqueBodyAssetsForMod(
            string modName,
            List<data_girls_textures._textureAsset> assets,
            HashSet<string> seenKeys)
        {
            if (assets == null || seenKeys == null)
            {
                return;
            }

            List<data_girls_textures._textureAsset> textureAssets = data_girls_textures.GetTextureAssets(
                data_girls_textures._spriteType.body,
                -1,
                modName ?? DefaultTextureAssetModName);
            if (textureAssets == null)
            {
                return;
            }

            for (int assetIndex = 0; assetIndex < textureAssets.Count; assetIndex++)
            {
                data_girls_textures._textureAsset asset = textureAssets[assetIndex];
                if (asset == null || asset.type != data_girls_textures._spriteType.body || !asset.Unique)
                {
                    continue;
                }

                string assetKey = BuildUniqueAssetKey(asset);
                if (seenKeys.Contains(assetKey))
                {
                    continue;
                }

                seenKeys.Add(assetKey);
                assets.Add(asset);
            }
        }

        private static bool CreatePopup(
            PopupManager manager,
            data_girls dataGirls,
            data_girls_textures textureData,
            List<UniqueIdolEntry> entries)
        {
            if (manager == null
                || dataGirls == null
                || textureData == null
                || entries == null
                || entries.Count == ZeroCount)
            {
                return false;
            }

            Transform parent = GetPopupParent();
            if (parent == null)
            {
                return false;
            }

            DestroyExistingRoot();

            Vector2 panelSize = CalculatePanelSize(entries.Count, parent as RectTransform);
            int gridColumns = CalculateGridColumnCount(panelSize.x);

            GameObject root = new GameObject(PopupName, typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            root.transform.SetAsLastSibling();
            SetLayerRecursively(root, parent.gameObject.layer);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
            canvasGroup.alpha = HiddenAlpha;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            root.SetActive(false);

            GameObject panel = CreateUIObject(PanelObjectName, root.transform);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(CenterAnchor, CenterAnchor);
            panelRect.anchorMax = new Vector2(CenterAnchor, CenterAnchor);
            panelRect.pivot = new Vector2(CenterAnchor, CenterAnchor);
            panelRect.sizeDelta = panelSize;
            panelRect.anchoredPosition = new Vector2(BaseAnchor, PanelVerticalOffset);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = GetPanelBackgroundColor();
            panelImage.raycastTarget = true;

            CreateTitle(panel.transform);
            ScrollRect scrollRect;
            RectTransform contentRect;
            CreateScrollArea(panel.transform, out scrollRect, out contentRect);

            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                AddUniqueIdolSlot(contentRect.transform, entries[entryIndex], panelSize.x, gridColumns);
            }

            float contentHeight = CalculateGridContentHeight(entries.Count, gridColumns);
            contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            scrollRect.verticalNormalizedPosition = EdgeAnchor;

            UniqueIdolRecruitmentViewport viewportLoader = root.AddComponent<UniqueIdolRecruitmentViewport>();
            viewportLoader.Initialize(
                dataGirls,
                textureData,
                entries,
                scrollRect,
                contentRect,
                gridColumns,
                TileHeight + TileSpacingY,
                panelSize.x);

            CreateSearchBar(panel.transform, viewportLoader);
            CreateCloseButton(panel.transform);
            Popup popup = root.AddComponent<Popup>();
            popup.ShowAnimation = true;
            popup.HideAnimation = true;
            popup.HideFast = false;
            popup.Increase_Popup_Counter = true;
            popup.OnOpen = new UnityEvent();

            if (!TryRegisterPopup(manager, root))
            {
                UnityEngine.Object.Destroy(root);
                return false;
            }

            popupRoot = root;
            return true;
        }

        private static void CreateTitle(Transform panel)
        {
            TextMeshProUGUI title = CreateText(
                panel,
                TitleObjectName,
                GetLocalized(PopupTitleKey, PopupTitleFallback),
                TitleFontSize,
                TextAlignmentOptions.Center,
                mainScript.black32);
            title.enableWordWrapping = false;
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(CenterAnchor, EdgeAnchor);
            titleRect.anchorMax = new Vector2(CenterAnchor, EdgeAnchor);
            titleRect.pivot = new Vector2(CenterAnchor, EdgeAnchor);
            titleRect.sizeDelta = new Vector2(TitleWidth, TitleHeight);
            titleRect.anchoredPosition = new Vector2(BaseAnchor, TitleVerticalOffset);
        }

        private static void CreateSearchBar(
            Transform panel,
            UniqueIdolRecruitmentViewport viewportLoader)
        {
            if (panel == null || viewportLoader == null)
            {
                return;
            }

            GameObject searchObject = CreateUIObject(SearchInputObjectName, panel);
            RectTransform searchRect = searchObject.GetComponent<RectTransform>();
            searchRect.anchorMin = new Vector2(BaseAnchor, EdgeAnchor);
            searchRect.anchorMax = new Vector2(EdgeAnchor, EdgeAnchor);
            searchRect.pivot = new Vector2(CenterAnchor, EdgeAnchor);
            searchRect.offsetMin = new Vector2(SearchBarOffsetLeft, SearchBarOffsetBottom);
            searchRect.offsetMax = new Vector2(SearchBarOffsetRight, SearchBarOffsetTop);

            Image searchBackground = searchObject.AddComponent<Image>();
            searchBackground.color = mainScript.white32;
            searchBackground.raycastTarget = true;
            TMP_InputField searchInput = searchObject.AddComponent<TMP_InputField>();
            searchInput.targetGraphic = searchBackground;
            searchInput.textViewport = searchRect;
            searchInput.contentType = TMP_InputField.ContentType.Standard;
            searchInput.lineType = TMP_InputField.LineType.SingleLine;

            TextMeshProUGUI searchText = CreateText(
                searchObject.transform,
                SearchTextObjectName,
                EmptyString,
                SearchInputFontSize,
                TextAlignmentOptions.MidlineLeft,
                mainScript.black32);
            ConfigureSearchTextRect(searchText.GetComponent<RectTransform>());
            searchInput.textComponent = searchText;

            TextMeshProUGUI searchPlaceholder = CreateText(
                searchObject.transform,
                SearchPlaceholderObjectName,
                GetLocalized(SearchPlaceholderKey, SearchPlaceholderFallback),
                SearchInputFontSize,
                TextAlignmentOptions.MidlineLeft,
                new Color32(
                    MutedTextColorChannel,
                    MutedTextColorChannel,
                    MutedTextColorChannel,
                    SolidColorAlpha));
            ConfigureSearchTextRect(searchPlaceholder.GetComponent<RectTransform>());
            searchInput.placeholder = searchPlaceholder;
            searchInput.text = EmptyString;
            searchInput.onValueChanged = new TMP_InputField.OnChangeEvent();
            searchInput.onValueChanged.AddListener(viewportLoader.ApplyFilter);
        }

        private static void ConfigureSearchTextRect(RectTransform textRect)
        {
            if (textRect == null)
            {
                return;
            }

            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(
                SearchTextHorizontalPadding,
                SearchTextVerticalPadding);
            textRect.offsetMax = new Vector2(
                -SearchTextHorizontalPadding,
                -SearchTextVerticalPadding);
        }

        private static void CreateScrollArea(
            Transform panel,
            out ScrollRect scrollRect,
            out RectTransform contentRect)
        {
            GameObject scrollView = CreateUIObject(ScrollViewObjectName, panel);
            RectTransform scrollViewRect = scrollView.GetComponent<RectTransform>();
            scrollViewRect.anchorMin = new Vector2(BaseAnchor, BaseAnchor);
            scrollViewRect.anchorMax = new Vector2(EdgeAnchor, EdgeAnchor);
            scrollViewRect.offsetMin = new Vector2(ScrollViewOffsetLeft, ScrollViewOffsetBottom);
            scrollViewRect.offsetMax = new Vector2(ScrollViewOffsetRight, ScrollViewOffsetTop);
            Image scrollImage = scrollView.AddComponent<Image>();
            scrollImage.color = GetContentBackgroundColor();
            scrollImage.raycastTarget = true;
            scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = ScrollSensitivity;

            GameObject viewport = CreateUIObject(ViewportObjectName, scrollView.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(-ScrollbarViewportReserve, BaseAnchor);
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = GetContentBackgroundColor();
            viewportImage.raycastTarget = true;
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject content = CreateUIObject(ContentObjectName, viewport.transform);
            contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(BaseAnchor, EdgeAnchor);
            contentRect.anchorMax = new Vector2(EdgeAnchor, EdgeAnchor);
            contentRect.pivot = new Vector2(CenterAnchor, EdgeAnchor);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(BaseAnchor, BaseAnchor);

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            CreateScrollbar(scrollView.transform, scrollRect);
        }

        private static void AddUniqueIdolSlot(
            Transform parent,
            UniqueIdolEntry entry,
            float panelWidth,
            int gridColumns)
        {
            if (parent == null || entry == null)
            {
                return;
            }

            GameObject slot = CreateUIObject(
                TileSlotObjectNamePrefix + entry.Index.ToString(CultureInfo.InvariantCulture),
                parent);
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(BaseAnchor, EdgeAnchor);
            slotRect.anchorMax = new Vector2(BaseAnchor, EdgeAnchor);
            slotRect.pivot = new Vector2(BaseAnchor, EdgeAnchor);
            slotRect.sizeDelta = new Vector2(TileWidth, TileHeight);

            entry.SlotObject = slot;
            PositionUniqueIdolSlot(entry, entry.Index, panelWidth, gridColumns);
        }

        internal static void PositionUniqueIdolSlot(
            UniqueIdolEntry entry,
            int layoutIndex,
            float panelWidth,
            int gridColumns)
        {
            if (entry == null || entry.SlotObject == null)
            {
                return;
            }

            RectTransform slotRect = entry.SlotObject.GetComponent<RectTransform>();
            if (slotRect == null)
            {
                return;
            }

            int safeGridColumns = Mathf.Max(MinimumGridColumns, gridColumns);
            int rowIndex = layoutIndex / safeGridColumns;
            int columnIndex = layoutIndex % safeGridColumns;
            float viewportWidth = CalculateViewportWidth(panelWidth);
            float gridWidth = (safeGridColumns * TileWidth)
                + (Mathf.Max(ZeroCount, safeGridColumns - 1) * TileSpacingX);
            float gridLeft = Mathf.Max(ContentPaddingLeft, (viewportWidth - gridWidth) / 2f);
            float horizontalPosition = gridLeft + (columnIndex * (TileWidth + TileSpacingX));
            float verticalPosition = -(ContentPaddingTop + (rowIndex * (TileHeight + TileSpacingY)));
            slotRect.anchoredPosition = new Vector2(horizontalPosition, verticalPosition);
        }

        internal static void AddUniqueIdolTile(UniqueIdolEntry entry)
        {
            if (entry == null
                || entry.SlotObject == null
                || entry.PreviewGirl == null
                || entry.Asset == null)
            {
                return;
            }

            ReleaseEntryTile(entry);

            bool canRecruit = CanRecruitUniqueAsset(entry.Asset);
            Color32 frameColor = GetRarityFrameColor(entry);
            GameObject tile = CreateUIObject(
                TileObjectNamePrefix + entry.Index.ToString(CultureInfo.InvariantCulture),
                entry.SlotObject.transform);
            RectTransform tileRect = tile.GetComponent<RectTransform>();
            tileRect.anchorMin = Vector2.zero;
            tileRect.anchorMax = Vector2.one;
            tileRect.offsetMin = Vector2.zero;
            tileRect.offsetMax = Vector2.zero;
            Image frameImage = tile.AddComponent<Image>();
            frameImage.color = canRecruit ? frameColor : GetDisabledColor(frameColor);
            ApplyRarityFrameSprite(frameImage, entry);
            Outline outline = tile.AddComponent<Outline>();
            outline.effectColor = frameColor;
            outline.effectDistance = new Vector2(OutlineDistance, -OutlineDistance);
            frameImage.raycastTarget = false;

            GameObject inner = CreateUIObject(InnerCardObjectName, tile.transform);
            RectTransform innerRect = inner.GetComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(InnerCardInset, InnerCardInset);
            innerRect.offsetMax = new Vector2(-InnerCardInset, -InnerCardInset);
            Image innerImage = inner.AddComponent<Image>();
            innerImage.color = canRecruit ? GetCardBackgroundColor() : GetDisabledColor(GetCardBackgroundColor());
            innerImage.raycastTarget = false;

            CreateRarityBackground(inner.transform, entry, canRecruit);

            CreateRarityLabel(inner.transform, entry, frameColor);
            CreateNameLabel(inner.transform, entry, entry.PreviewGirl, canRecruit);
            CreateAgeLabel(inner.transform, entry, entry.PreviewGirl, canRecruit);
            CreatePortrait(inner.transform, entry, canRecruit);
            CreatePortraitPartSelectors(inner.transform, entry, canRecruit);
            CreateStatsLabels(inner.transform, entry, entry.PreviewGirl, canRecruit);
            TextMeshProUGUI recruitButtonLabel;
            entry.RecruitButton = CreateRecruitButton(
                inner.transform,
                entry,
                canRecruit,
                out recruitButtonLabel);
            entry.StatusText = recruitButtonLabel;
            entry.TileObject = tile;
        }

        private static void CreateRarityBackground(Transform parent, UniqueIdolEntry entry, bool canRecruit)
        {
            GameObject background = CreateUIObject(RarityBackgroundObjectName, parent);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = canRecruit
                ? mainScript.white32
                : GetDisabledColor(mainScript.white32);
            backgroundImage.raycastTarget = false;
            ApplyRarityBackgroundSprite(backgroundImage, entry);
        }

        private static void CreateRarityLabel(Transform parent, UniqueIdolEntry entry, Color32 frameColor)
        {
            TextMeshProUGUI rarity = CreateText(
                parent,
                RarityLabelObjectName,
                GetRarityLabel(entry),
                RarityFontSize,
                TextAlignmentOptions.Center,
                frameColor);
            rarity.enableWordWrapping = false;
            RectTransform rarityRect = rarity.GetComponent<RectTransform>();
            rarityRect.anchorMin = new Vector2(BaseAnchor, EdgeAnchor);
            rarityRect.anchorMax = new Vector2(EdgeAnchor, EdgeAnchor);
            rarityRect.pivot = new Vector2(CenterAnchor, EdgeAnchor);
            rarityRect.offsetMin = new Vector2(BaseAnchor, RarityLabelTop - RarityLabelHeight);
            rarityRect.offsetMax = new Vector2(BaseAnchor, RarityLabelTop);
        }

        private static void CreatePortrait(Transform parent, UniqueIdolEntry entry, bool canRecruit)
        {
            GameObject portraitObject = CreateUIObject(PortraitObjectName, parent);
            RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(CenterAnchor, EdgeAnchor);
            portraitRect.anchorMax = new Vector2(CenterAnchor, EdgeAnchor);
            portraitRect.pivot = new Vector2(CenterAnchor, EdgeAnchor);
            portraitRect.sizeDelta = new Vector2(PortraitWidth, PortraitHeight);
            portraitRect.anchoredPosition = new Vector2(BaseAnchor, PortraitTop);

            RectMask2D portraitMask = portraitObject.AddComponent<RectMask2D>();
            portraitMask.padding = Vector4.zero;
            if (entry == null || entry.PortraitParts == null)
            {
                return;
            }

            for (int partTypeIndex = FirstItemIndex;
                partTypeIndex < PortraitRenderPartTypes.Length;
                partTypeIndex++)
            {
                PortraitPartState partState = GetPortraitPartState(
                    entry,
                    PortraitRenderPartTypes[partTypeIndex]);
                if (partState == null)
                {
                    continue;
                }

                GameObject layerObject = CreateUIObject(
                    PortraitLayerObjectNamePrefix + partState.Type.ToString(),
                    portraitObject.transform);
                RectTransform layerRect = layerObject.GetComponent<RectTransform>();
                layerRect.anchorMin = new Vector2(CenterAnchor, EdgeAnchor);
                layerRect.anchorMax = new Vector2(CenterAnchor, EdgeAnchor);
                layerRect.pivot = new Vector2(CenterAnchor, EdgeAnchor);
                layerRect.sizeDelta = Vector2.zero;
                layerRect.anchoredPosition = Vector2.zero;
                Image layerImage = layerObject.AddComponent<Image>();
                layerImage.preserveAspect = false;
                layerImage.raycastTarget = false;
                layerImage.color = GetTransparentColor();
                partState.PortraitImage = layerImage;
                RefreshPortraitPart(entry, partState, canRecruit);
            }
        }

        private static void CreatePortraitPartSelectors(
            Transform parent,
            UniqueIdolEntry entry,
            bool canRecruit)
        {
            if (parent == null || entry == null || entry.PortraitParts == null)
            {
                return;
            }

            int visibleSelectorIndex = FirstItemIndex;
            for (int partTypeIndex = FirstItemIndex;
                partTypeIndex < PortraitSelectorPartTypes.Length;
                partTypeIndex++)
            {
                PortraitPartState partState = GetPortraitPartState(
                    entry,
                    PortraitSelectorPartTypes[partTypeIndex]);
                if (partState == null || partState.Assets == null || partState.Assets.Count == ZeroCount)
                {
                    continue;
                }

                CreatePortraitPartSelectorRow(
                    parent,
                    entry,
                    partState,
                    canRecruit,
                    visibleSelectorIndex);
                visibleSelectorIndex++;
            }
        }

        private static void CreatePortraitPartSelectorRow(
            Transform parent,
            UniqueIdolEntry entry,
            PortraitPartState partState,
            bool canRecruit,
            int selectorIndex)
        {
            GameObject rowObject = CreateUIObject(
                SelectorRowObjectNamePrefix + partState.Type.ToString(),
                parent);
            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(BaseAnchor, EdgeAnchor);
            rowRect.anchorMax = new Vector2(EdgeAnchor, EdgeAnchor);
            rowRect.pivot = new Vector2(CenterAnchor, EdgeAnchor);
            float rowTop = SelectorTop
                - (selectorIndex * (SelectorRowHeight + SelectorRowSpacing));
            rowRect.offsetMin = new Vector2(SelectorHorizontalInset, rowTop - SelectorRowHeight);
            rowRect.offsetMax = new Vector2(-SelectorHorizontalInset, rowTop);

            CreateSelectorArrowButton(
                rowObject.transform,
                SelectorPreviousButtonObjectName,
                SelectorPreviousSymbol,
                true,
                delegate
                {
                    ChangePortraitPartSelection(
                        entry,
                        partState,
                        PreviousPartSelectionOffset);
                });
            CreateSelectorArrowButton(
                rowObject.transform,
                SelectorNextButtonObjectName,
                SelectorNextSymbol,
                false,
                delegate
                {
                    ChangePortraitPartSelection(
                        entry,
                        partState,
                        NextPartSelectionOffset);
                });

            TextMeshProUGUI selectorLabel = CreateText(
                rowObject.transform,
                SelectorLabelObjectName,
                BuildPortraitPartSelectionLabel(partState),
                SelectorLabelFontSize,
                TextAlignmentOptions.Center,
                GetCardPrimaryTextColor(entry, canRecruit));
            selectorLabel.enableWordWrapping = false;
            RectTransform labelRect = selectorLabel.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(SelectorArrowButtonWidth, BaseAnchor);
            labelRect.offsetMax = new Vector2(-SelectorArrowButtonWidth, BaseAnchor);
            partState.SelectorLabel = selectorLabel;
        }

        private static void CreateSelectorArrowButton(
            Transform parent,
            string objectName,
            string symbol,
            bool alignLeft,
            UnityAction onClick)
        {
            GameObject buttonObject = CreateUIObject(objectName, parent);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(alignLeft ? BaseAnchor : EdgeAnchor, BaseAnchor);
            buttonRect.anchorMax = new Vector2(alignLeft ? BaseAnchor : EdgeAnchor, EdgeAnchor);
            buttonRect.pivot = new Vector2(alignLeft ? BaseAnchor : EdgeAnchor, CenterAnchor);
            buttonRect.sizeDelta = new Vector2(SelectorArrowButtonWidth, BaseAnchor);
            buttonRect.anchoredPosition = Vector2.zero;
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = mainScript.blue32;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(onClick);

            TextMeshProUGUI arrowLabel = CreateText(
                buttonObject.transform,
                ButtonTextObjectName,
                symbol,
                SelectorArrowFontSize,
                TextAlignmentOptions.Center,
                mainScript.white32);
            arrowLabel.enableWordWrapping = false;
            RectTransform arrowLabelRect = arrowLabel.GetComponent<RectTransform>();
            arrowLabelRect.anchorMin = Vector2.zero;
            arrowLabelRect.anchorMax = Vector2.one;
            arrowLabelRect.offsetMin = Vector2.zero;
            arrowLabelRect.offsetMax = Vector2.zero;
        }

        internal static bool EnsurePreviewGirl(
            data_girls dataGirls,
            data_girls_textures textureData,
            UniqueIdolEntry entry)
        {
            if (entry == null || entry.Asset == null || dataGirls == null || textureData == null)
            {
                return false;
            }

            if (entry.PreviewGirl != null)
            {
                return true;
            }

            data_girls.girls previewGirl = new data_girls.girls
            {
                firstName = nameGenerator.firstName(true),
                lastName = nameGenerator.lastName(),
                birthday = staticVars.dateTime
                    .AddYears(-UnityEngine.Random.Range(
                        FallbackIdolMinimumAge,
                        FallbackIdolMaximumAgeExclusive))
                    .AddMonths(-UnityEngine.Random.Range(
                        ZeroCount,
                        RandomBirthMonthUpperBoundExclusive))
                    .AddDays(-UnityEngine.Random.Range(
                        ZeroCount,
                        RandomBirthDayUpperBoundExclusive))
            };
            previewGirl.createListOfParams();
            ApplyFallbackPreviewStats(previewGirl, entry.Rarity);
            ApplyUniqueAssetData(previewGirl, entry.Asset);
            entry.PreviewGirl = previewGirl;
            entry.Name = previewGirl.GetName(true);
            return true;
        }

        private static void ApplyFallbackPreviewStats(
            data_girls.girls previewGirl,
            Auditions.data._girl._type rarity)
        {
            int statValue = NormalFallbackStatValue;
            int potentialValue = NormalFallbackPotentialValue;
            if (rarity == Auditions.data._girl._type.platinum)
            {
                statValue = PlatinumFallbackStatValue;
                potentialValue = PlatinumFallbackPotentialValue;
            }
            else if (rarity == Auditions.data._girl._type.golden)
            {
                statValue = GoldFallbackStatValue;
                potentialValue = GoldFallbackPotentialValue;
            }
            else if (rarity == Auditions.data._girl._type.silver)
            {
                statValue = SilverFallbackStatValue;
                potentialValue = SilverFallbackPotentialValue;
            }

            for (int statIndex = FirstItemIndex;
                statIndex < DisplayedStatTypes.Length;
                statIndex++)
            {
                data_girls.girls.param parameter = previewGirl.getParam(DisplayedStatTypes[statIndex]);
                if (parameter == null)
                {
                    continue;
                }

                parameter.val = statValue;
                parameter.potential = potentialValue;
            }
        }

        private static void ApplyUniqueAssetData(
            data_girls.girls previewGirl,
            data_girls_textures._textureAsset asset)
        {
            if (previewGirl == null || asset == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(asset.first_name))
            {
                previewGirl.firstName = asset.first_name;
            }

            if (!string.IsNullOrEmpty(asset.last_name))
            {
                previewGirl.lastName = asset.last_name;
            }

            if (asset.Trait != traits._trait._type.None)
            {
                previewGirl.trait = asset.Trait;
            }

            if (asset.Age != ZeroCount)
            {
                previewGirl.birthday = staticVars.dateTime
                    .AddYears(-asset.Age)
                    .AddMonths(-UnityEngine.Random.Range(ZeroCount, RandomBirthMonthUpperBoundExclusive))
                    .AddDays(-UnityEngine.Random.Range(ZeroCount, RandomBirthDayUpperBoundExclusive));
            }

            if (asset.Stats == null)
            {
                return;
            }

            for (int statIndex = 0; statIndex < asset.Stats.Count; statIndex++)
            {
                data_girls.girls.param sourceParameter = asset.Stats[statIndex];
                if (sourceParameter == null)
                {
                    continue;
                }

                data_girls.girls.param targetParameter = previewGirl.getParam(sourceParameter.type);
                if (targetParameter == null)
                {
                    continue;
                }

                targetParameter.val = sourceParameter.val;
                targetParameter.potential = sourceParameter.potential;
            }
        }

        internal static PortraitPartState GetNextPortraitPartToLoad(UniqueIdolEntry entry)
        {
            if (entry == null || entry.PortraitParts == null || entry.TileObject == null)
            {
                return null;
            }

            for (int partIndex = FirstItemIndex;
                partIndex < entry.PortraitParts.Count;
                partIndex++)
            {
                PortraitPartState partState = entry.PortraitParts[partIndex];
                if (partState != null
                    && partState.PortraitImage != null
                    && partState.SelectedAsset != null
                    && !partState.LoadAttempted
                    && partState.DisplaySprite == null)
                {
                    return partState;
                }
            }

            return null;
        }

        internal static IEnumerator LoadEntryPortraitPart(
            UniqueIdolEntry entry,
            PortraitPartState partState)
        {
            if (entry == null
                || partState == null
                || entry.TileObject == null
                || partState.PortraitImage == null
                || partState.SelectedAsset == null
                || partState.LoadAttempted
                || partState.DisplaySprite != null)
            {
                yield break;
            }

            data_girls_textures._textureAsset requestedAsset = partState.SelectedAsset;
            partState.LoadAttempted = true;
            partState.RequestedAsset = requestedAsset;
            string sourcePath = requestedAsset.path;
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            {
                yield break;
            }

            string sourceUri = new Uri(sourcePath).AbsoluteUri;
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(sourceUri, false))
            {
                request.timeout = PortraitLoadTimeoutSeconds;
                yield return request.SendWebRequest();
                if (request.isHttpError || request.isNetworkError)
                {
                    Debug.LogWarning(string.Format(
                        CultureInfo.InvariantCulture,
                        UniquePortraitLoadLogFormat,
                        sourcePath,
                        request.error));
                    yield break;
                }

                Texture2D sourceTexture = DownloadHandlerTexture.GetContent(request);
                if (sourceTexture == null)
                {
                    yield break;
                }

                if (entry.TileObject == null
                    || partState.PortraitImage == null
                    || !ReferenceEquals(partState.RequestedAsset, requestedAsset)
                    || !ReferenceEquals(partState.SelectedAsset, requestedAsset))
                {
                    UnityEngine.Object.Destroy(sourceTexture);
                    yield break;
                }

                TryApplySourcePortraitPart(entry, partState, sourcePath, sourceTexture);
            }
        }

        private static void TryApplySourcePortraitPart(
            UniqueIdolEntry entry,
            PortraitPartState partState,
            string sourcePath,
            Texture2D sourceTexture)
        {
            Sprite displaySprite = null;
            try
            {
                sourceTexture.filterMode = FilterMode.Bilinear;
                sourceTexture.wrapMode = TextureWrapMode.Clamp;
                displaySprite = Sprite.Create(
                    sourceTexture,
                    new Rect(
                        BaseAnchor,
                        BaseAnchor,
                        sourceTexture.width,
                        sourceTexture.height),
                    new Vector2(CenterAnchor, EdgeAnchor),
                    PortraitSpritePixelsPerUnit,
                    ZeroCount,
                    SpriteMeshType.FullRect);
                if (displaySprite == null)
                {
                    UnityEngine.Object.Destroy(sourceTexture);
                    return;
                }

                DestroyPortraitPartVisualAssets(partState);
                partState.DisplayTexture = sourceTexture;
                partState.DisplaySprite = displaySprite;
                sourceTexture = null;
                ConfigurePortraitPartBounds(partState);
                RefreshPortraitPart(
                    entry,
                    partState,
                    CanRecruitUniqueAsset(entry.Asset));
            }
            catch (Exception ex)
            {
                if (sourceTexture != null)
                {
                    UnityEngine.Object.Destroy(sourceTexture);
                }

                if (displaySprite != null)
                {
                    UnityEngine.Object.Destroy(displaySprite);
                }

                Debug.LogWarning(string.Format(
                    CultureInfo.InvariantCulture,
                    UniquePortraitLoadLogFormat,
                    sourcePath,
                    ex.Message));
            }
        }

        private static void ConfigurePortraitPartBounds(PortraitPartState partState)
        {
            if (partState == null
                || partState.PortraitImage == null
                || partState.DisplayTexture == null)
            {
                return;
            }

            RectTransform layerRect = partState.PortraitImage.rectTransform;
            layerRect.anchorMin = new Vector2(CenterAnchor, EdgeAnchor);
            layerRect.anchorMax = new Vector2(CenterAnchor, EdgeAnchor);
            layerRect.pivot = new Vector2(CenterAnchor, EdgeAnchor);
            layerRect.sizeDelta = new Vector2(
                partState.DisplayTexture.width
                    * PortraitCanvasDisplayWidth
                    / PortraitSourceCanvasWidth,
                partState.DisplayTexture.height
                    * PortraitHeight
                    / PortraitSourceCanvasHeight);
            layerRect.anchoredPosition = Vector2.zero;
        }

        internal static bool RefreshEntryPortrait(UniqueIdolEntry entry)
        {
            if (entry == null || entry.PortraitParts == null)
            {
                return false;
            }

            bool refreshedAnyPart = false;
            bool canRecruit = CanRecruitUniqueAsset(entry.Asset);
            for (int partIndex = FirstItemIndex;
                partIndex < entry.PortraitParts.Count;
                partIndex++)
            {
                refreshedAnyPart = RefreshPortraitPart(
                    entry,
                    entry.PortraitParts[partIndex],
                    canRecruit) || refreshedAnyPart;
            }

            return refreshedAnyPart;
        }

        private static bool RefreshPortraitPart(
            UniqueIdolEntry entry,
            PortraitPartState partState,
            bool canRecruit)
        {
            if (entry == null || partState == null || partState.PortraitImage == null)
            {
                return false;
            }

            if (partState.DisplaySprite == null || partState.SelectedAsset == null)
            {
                partState.PortraitImage.sprite = null;
                partState.PortraitImage.color = GetTransparentColor();
                return false;
            }

            partState.PortraitImage.sprite = partState.DisplaySprite;
            ConfigurePortraitPartBounds(partState);
            partState.PortraitImage.color = canRecruit
                ? mainScript.white32
                : GetDisabledColor(mainScript.white32);
            return true;
        }

        internal static void ReleaseEntryTile(UniqueIdolEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            if (entry.TileObject != null)
            {
                UnityEngine.Object.Destroy(entry.TileObject);
            }

            entry.TileObject = null;
            entry.RecruitButton = null;
            entry.StatusText = null;
            if (entry.PortraitParts == null)
            {
                return;
            }

            for (int partIndex = FirstItemIndex;
                partIndex < entry.PortraitParts.Count;
                partIndex++)
            {
                PortraitPartState partState = entry.PortraitParts[partIndex];
                if (partState == null)
                {
                    continue;
                }

                partState.PortraitImage = null;
                partState.SelectorLabel = null;
            }
        }

        internal static void ReleaseEntryPortrait(UniqueIdolEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            if (entry.PortraitParts == null)
            {
                return;
            }

            for (int partIndex = FirstItemIndex;
                partIndex < entry.PortraitParts.Count;
                partIndex++)
            {
                ReleasePortraitPart(entry.PortraitParts[partIndex]);
            }
        }

        private static void ReleasePortraitPart(PortraitPartState partState)
        {
            if (partState == null)
            {
                return;
            }

            DestroyPortraitPartVisualAssets(partState);
            partState.RequestedAsset = null;
            partState.LoadAttempted = false;
            if (partState.PortraitImage != null)
            {
                partState.PortraitImage.sprite = null;
                partState.PortraitImage.color = GetTransparentColor();
            }
        }

        private static void DestroyPortraitPartVisualAssets(PortraitPartState partState)
        {
            if (partState == null)
            {
                return;
            }

            if (partState.DisplaySprite != null)
            {
                UnityEngine.Object.Destroy(partState.DisplaySprite);
            }

            if (partState.DisplayTexture != null)
            {
                UnityEngine.Object.Destroy(partState.DisplayTexture);
            }

            partState.DisplaySprite = null;
            partState.DisplayTexture = null;
        }

        private static void ChangePortraitPartSelection(
            UniqueIdolEntry entry,
            PortraitPartState partState,
            int selectionOffset)
        {
            if (entry == null
                || partState == null
                || partState.Assets == null
                || partState.Assets.Count == ZeroCount)
            {
                return;
            }

            int noSelectionOptionCount = partState.AllowNoSelection
                ? NextPartSelectionOffset
                : ZeroCount;
            int optionCount = partState.Assets.Count + noSelectionOptionCount;
            int currentOptionIndex = partState.SelectedIndex + noSelectionOptionCount;
            int nextOptionIndex = (currentOptionIndex + selectionOffset) % optionCount;
            if (nextOptionIndex < ZeroCount)
            {
                nextOptionIndex += optionCount;
            }

            partState.SelectedIndex = nextOptionIndex - noSelectionOptionCount;
            ReleasePortraitPart(partState);
            if (partState.SelectorLabel != null)
            {
                partState.SelectorLabel.text = BuildPortraitPartSelectionLabel(partState);
            }

            RefreshPortraitPart(
                entry,
                partState,
                CanRecruitUniqueAsset(entry.Asset));
        }

        private static PortraitPartState GetPortraitPartState(
            UniqueIdolEntry entry,
            data_girls_textures._spriteType partType)
        {
            if (entry == null || entry.PortraitParts == null)
            {
                return null;
            }

            for (int partIndex = FirstItemIndex;
                partIndex < entry.PortraitParts.Count;
                partIndex++)
            {
                PortraitPartState partState = entry.PortraitParts[partIndex];
                if (partState != null && partState.Type == partType)
                {
                    return partState;
                }
            }

            return null;
        }

        private static string BuildPortraitPartSelectionLabel(PortraitPartState partState)
        {
            string partLabel = GetPortraitPartLabel(partState != null
                ? partState.Type
                : data_girls_textures._spriteType.NONE);
            if (partState == null || partState.SelectedAsset == null)
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    GetLocalized(NoPartSelectionFormatKey, NoPartSelectionFormatFallback),
                    partLabel,
                    GetLocalized(NoPartSelectionKey, NoPartSelectionFallback));
            }

            return string.Format(
                CultureInfo.CurrentCulture,
                GetLocalized(PartSelectionFormatKey, PartSelectionFormatFallback),
                partLabel,
                (partState.SelectedIndex + NextPartSelectionOffset)
                    .ToString(CultureInfo.CurrentCulture),
                partState.Assets.Count.ToString(CultureInfo.CurrentCulture));
        }

        private static string GetPortraitPartLabel(data_girls_textures._spriteType partType)
        {
            if (partType == data_girls_textures._spriteType.hair)
            {
                return GetLocalized(HairPartKey, HairPartFallback);
            }

            if (partType == data_girls_textures._spriteType.face)
            {
                return GetLocalized(FacePartKey, FacePartFallback);
            }

            if (partType == data_girls_textures._spriteType.body)
            {
                return GetLocalized(BodyPartKey, BodyPartFallback);
            }

            if (partType == data_girls_textures._spriteType.acc)
            {
                return GetLocalized(AccessoryPartKey, AccessoryPartFallback);
            }

            return EmptyString;
        }

        private static void CreateNameLabel(
            Transform parent,
            UniqueIdolEntry entry,
            data_girls.girls girl,
            bool canRecruit)
        {
            TextMeshProUGUI name = CreateText(
                parent,
                NameObjectName,
                girl.GetName(true),
                NameFontSize,
                TextAlignmentOptions.Center,
                GetCardPrimaryTextColor(entry, canRecruit));
            name.enableWordWrapping = true;
            name.enableAutoSizing = true;
            name.fontSizeMin = StatFontSize;
            name.fontSizeMax = NameFontSize;
            RectTransform nameRect = name.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(BaseAnchor, EdgeAnchor);
            nameRect.anchorMax = new Vector2(EdgeAnchor, EdgeAnchor);
            nameRect.pivot = new Vector2(CenterAnchor, EdgeAnchor);
            nameRect.offsetMin = new Vector2(InnerCardInset, NameTop - NameHeight);
            nameRect.offsetMax = new Vector2(-InnerCardInset, NameTop);
        }

        private static void CreateAgeLabel(
            Transform parent,
            UniqueIdolEntry entry,
            data_girls.girls girl,
            bool canRecruit)
        {
            string ageText = string.Format(
                CultureInfo.CurrentCulture,
                GetLocalized(AgeFormatKey, AgeFormatFallback),
                girl.GetAge().ToString(CultureInfo.CurrentCulture));
            TextMeshProUGUI age = CreateText(
                parent,
                AgeObjectName,
                ageText,
                AgeFontSize,
                TextAlignmentOptions.Center,
                GetCardSecondaryTextColor(entry, canRecruit));
            age.enableWordWrapping = false;
            RectTransform ageRect = age.GetComponent<RectTransform>();
            ageRect.anchorMin = new Vector2(BaseAnchor, EdgeAnchor);
            ageRect.anchorMax = new Vector2(EdgeAnchor, EdgeAnchor);
            ageRect.pivot = new Vector2(CenterAnchor, EdgeAnchor);
            ageRect.offsetMin = new Vector2(InnerCardInset, AgeTop - AgeHeight);
            ageRect.offsetMax = new Vector2(-InnerCardInset, AgeTop);
        }

        private static void CreateStatsLabels(
            Transform parent,
            UniqueIdolEntry entry,
            data_girls.girls girl,
            bool canRecruit)
        {
            Color32 statColor = GetCardPrimaryTextColor(entry, canRecruit);
            TextMeshProUGUI leftStats = CreateText(
                parent,
                StatLeftObjectName,
                BuildStatsText(girl, FirstItemIndex, StatColumnItemCount),
                StatFontSize,
                TextAlignmentOptions.Left,
                statColor);
            TextMeshProUGUI rightStats = CreateText(
                parent,
                StatRightObjectName,
                BuildStatsText(girl, StatColumnItemCount, StatColumnItemCount),
                StatFontSize,
                TextAlignmentOptions.Left,
                statColor);
            ConfigureStatColumn(leftStats, true);
            ConfigureStatColumn(rightStats, false);
        }

        private static void ConfigureStatColumn(TextMeshProUGUI text, bool left)
        {
            text.enableWordWrapping = false;
            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(CenterAnchor, EdgeAnchor);
            rect.anchorMax = new Vector2(CenterAnchor, EdgeAnchor);
            rect.pivot = new Vector2(CenterAnchor, EdgeAnchor);
            rect.sizeDelta = new Vector2(StatColumnWidth, StatsHeight);
            float columnOffset = left
                ? -(StatColumnWidth + StatColumnSpacing) / 2f
                : (StatColumnWidth + StatColumnSpacing) / 2f;
            rect.anchoredPosition = new Vector2(columnOffset, StatsTop);
        }

        private static Button CreateRecruitButton(
            Transform parent,
            UniqueIdolEntry entry,
            bool canRecruit,
            out TextMeshProUGUI label)
        {
            GameObject buttonObject = CreateUIObject(RecruitButtonObjectName, parent);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(CenterAnchor, BaseAnchor);
            buttonRect.anchorMax = new Vector2(CenterAnchor, BaseAnchor);
            buttonRect.pivot = new Vector2(CenterAnchor, BaseAnchor);
            buttonRect.sizeDelta = new Vector2(RecruitButtonWidth, RecruitButtonHeight);
            buttonRect.anchoredPosition = new Vector2(BaseAnchor, RecruitButtonBottom);

            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = canRecruit ? mainScript.green32 : mainScript.red32;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.interactable = canRecruit;
            if (canRecruit)
            {
                button.onClick.AddListener(delegate
                {
                    RecruitUniqueIdol(entry);
                });
            }

            label = CreateText(
                buttonObject.transform,
                ButtonTextObjectName,
                canRecruit
                    ? GetLocalized(RecruitButtonKey, RecruitButtonFallback)
                    : GetLocalized(AlreadyRecruitedStatusKey, AlreadyRecruitedStatusFallback),
                StatusFontSize,
                TextAlignmentOptions.Center,
                mainScript.white32);
            label.enableWordWrapping = false;
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            return button;
        }

        private static void CreateCloseButton(Transform parent)
        {
            GameObject closeObject = CreateUIObject(CloseButtonObjectName, parent);
            RectTransform closeRect = closeObject.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(CenterAnchor, BaseAnchor);
            closeRect.anchorMax = new Vector2(CenterAnchor, BaseAnchor);
            closeRect.pivot = new Vector2(CenterAnchor, BaseAnchor);
            closeRect.sizeDelta = new Vector2(CloseButtonWidth, CloseButtonHeight);
            closeRect.anchoredPosition = new Vector2(BaseAnchor, CloseButtonOffsetY);
            Image closeImage = closeObject.AddComponent<Image>();
            closeImage.color = mainScript.green32;
            Button closeButton = closeObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(Close);
            TextMeshProUGUI label = CreateText(
                closeObject.transform,
                ButtonTextObjectName,
                GetLocalized(CloseButtonKey, CloseButtonFallback),
                CloseButtonFontSize,
                TextAlignmentOptions.Center,
                mainScript.white32);
            label.enableWordWrapping = false;
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        private static void CreateScrollbar(Transform parent, ScrollRect target)
        {
            if (parent == null || target == null)
            {
                return;
            }

            Scrollbar template = GetScrollbarTemplate();
            GameObject scrollbarObject;
            Scrollbar scrollbar;
            if (template != null)
            {
                scrollbarObject = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
                scrollbarObject.name = ScrollbarObjectName;
                SetLayerRecursively(scrollbarObject, parent.gameObject.layer);
                scrollbarObject.SetActive(true);
                scrollbar = scrollbarObject.GetComponent<Scrollbar>();
                if (scrollbar == null)
                {
                    scrollbar = scrollbarObject.AddComponent<Scrollbar>();
                }
                CanvasGroup group = scrollbarObject.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    group.alpha = VisibleAlpha;
                    group.interactable = true;
                    group.blocksRaycasts = true;
                }
            }
            else
            {
                scrollbarObject = CreateUIObject(ScrollbarObjectName, parent);
                Image trackImage = scrollbarObject.AddComponent<Image>();
                trackImage.color = GetScrollbarTrackColor();
                trackImage.raycastTarget = true;
                scrollbar = scrollbarObject.AddComponent<Scrollbar>();
                GameObject handleObject = CreateUIObject(ScrollbarHandleObjectName, scrollbarObject.transform);
                RectTransform handleRect = handleObject.GetComponent<RectTransform>();
                handleRect.anchorMin = Vector2.zero;
                handleRect.anchorMax = Vector2.one;
                handleRect.offsetMin = Vector2.zero;
                handleRect.offsetMax = Vector2.zero;
                Image handleImage = handleObject.AddComponent<Image>();
                handleImage.color = mainScript.blue32;
                scrollbar.targetGraphic = handleImage;
                scrollbar.handleRect = handleRect;
            }

            RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(EdgeAnchor, BaseAnchor);
            scrollbarRect.anchorMax = new Vector2(EdgeAnchor, EdgeAnchor);
            scrollbarRect.pivot = new Vector2(EdgeAnchor, EdgeAnchor);
            scrollbarRect.sizeDelta = new Vector2(ScrollbarWidth, BaseAnchor);
            scrollbarRect.anchoredPosition = new Vector2(ScrollbarOffsetX, BaseAnchor);
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.onValueChanged = new Scrollbar.ScrollEvent();
            scrollbar.onValueChanged.AddListener(delegate(float value)
            {
                target.verticalNormalizedPosition = value;
            });
            target.verticalScrollbar = scrollbar;
            target.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            target.verticalScrollbarSpacing = ScrollbarSpacing;
        }

        private static void RecruitUniqueIdol(UniqueIdolEntry entry)
        {
            if (entry == null || entry.Asset == null || entry.PreviewGirl == null)
            {
                NotifyWarning(NotificationUniqueIdolRecruitFailedKey, NotificationUniqueIdolRecruitFailedFallback);
                return;
            }

            if (!CanRecruitUniqueAsset(entry.Asset))
            {
                SetEntryAlreadyRecruited(entry);
                NotifyWarning(NotificationUniqueIdolAlreadyRecruitedKey, NotificationUniqueIdolAlreadyRecruitedFallback);
                return;
            }

            data_girls dataGirls = GetDataComponent<data_girls>();
            data_girls_textures textureData = GetDataComponent<data_girls_textures>();
            if (dataGirls == null || textureData == null)
            {
                NotifyWarning(CheatLocalizationKeys.NotificationGameUnavailable, CheatFallbackText.NotificationGameUnavailable);
                return;
            }

            data_girls.girls recruitedGirl = CreateRecruitableGirl(dataGirls, textureData, entry);
            if (recruitedGirl == null)
            {
                NotifyWarning(NotificationUniqueIdolRecruitFailedKey, NotificationUniqueIdolRecruitFailedFallback);
                return;
            }

            entry.PreviewGirl = recruitedGirl;
            data_girls_textures.AddToQueue(recruitedGirl, null);
            dataGirls.Hire(recruitedGirl, true);
            dataGirls.UpdateList(true);
            entry.Recruited = true;
            if (entry.RecruitButton != null)
            {
                entry.RecruitButton.interactable = false;
            }

            if (entry.StatusText != null)
            {
                entry.StatusText.text = GetLocalized(RecruitedStatusKey, RecruitedStatusFallback);
                entry.StatusText.color = mainScript.white32;
            }

            string notificationFormat = GetLocalized(NotificationUniqueIdolRecruitedKey, NotificationUniqueIdolRecruitedFallback);
            NotificationManager.AddNotification(
                string.Format(CultureInfo.CurrentCulture, notificationFormat, entry.PreviewGirl.GetName(true)),
                mainScript.green32,
                NotificationManager._notification._type.idol_stat_change);
        }

        private static data_girls.girls CreateRecruitableGirl(
            data_girls dataGirls,
            data_girls_textures textureData,
            UniqueIdolEntry entry)
        {
            if (dataGirls == null
                || textureData == null
                || entry == null
                || entry.Asset == null
                || entry.PreviewGirl == null)
            {
                return null;
            }

            data_girls.girls recruitedGirl = dataGirls.GenerateGirl(
                false,
                Auditions.data._girl._type.normal,
                null);
            if (recruitedGirl == null)
            {
                return null;
            }

            recruitedGirl.textureAssets = BuildSelectedTextureAssets(entry);
            recruitedGirl.UpdateTextureData();
            recruitedGirl.firstName = entry.PreviewGirl.firstName;
            recruitedGirl.lastName = entry.PreviewGirl.lastName;
            recruitedGirl.birthday = entry.PreviewGirl.birthday;
            if (entry.Asset.Trait != traits._trait._type.None)
            {
                recruitedGirl.trait = entry.Asset.Trait;
            }

            for (int statIndex = FirstItemIndex;
                statIndex < DisplayedStatTypes.Length;
                statIndex++)
            {
                data_girls._paramType statType = DisplayedStatTypes[statIndex];
                data_girls.girls.param sourceParameter = entry.PreviewGirl.getParam(statType);
                data_girls.girls.param targetParameter = recruitedGirl.getParam(statType);
                if (sourceParameter == null || targetParameter == null)
                {
                    continue;
                }

                targetParameter.val = sourceParameter.val;
                targetParameter.potential = sourceParameter.potential;
            }

            return recruitedGirl;
        }

        private static List<data_girls.girls._textureAsset> BuildSelectedTextureAssets(
            UniqueIdolEntry entry)
        {
            List<data_girls.girls._textureAsset> selectedTextureAssets =
                new List<data_girls.girls._textureAsset>();
            if (entry == null)
            {
                return selectedTextureAssets;
            }

            for (int partTypeIndex = FirstItemIndex;
                partTypeIndex < RequiredRecruitmentPartTypes.Length;
                partTypeIndex++)
            {
                data_girls_textures._spriteType partType =
                    RequiredRecruitmentPartTypes[partTypeIndex];
                selectedTextureAssets.Add(new data_girls.girls._textureAsset
                {
                    type = partType,
                    asset = GetRequiredRecruitmentPartAsset(entry, partType)
                });
            }

            PortraitPartState accessoryState = GetPortraitPartState(
                entry,
                data_girls_textures._spriteType.acc);
            if (accessoryState != null && accessoryState.SelectedAsset != null)
            {
                selectedTextureAssets.Add(new data_girls.girls._textureAsset
                {
                    type = data_girls_textures._spriteType.acc,
                    asset = accessoryState.SelectedAsset
                });
            }

            return selectedTextureAssets;
        }

        private static data_girls_textures._textureAsset GetRequiredRecruitmentPartAsset(
            UniqueIdolEntry entry,
            data_girls_textures._spriteType partType)
        {
            if (entry == null || entry.Asset == null)
            {
                return null;
            }

            PortraitPartState partState = GetPortraitPartState(entry, partType);
            if (partState != null && partState.SelectedAsset != null)
            {
                return partState.SelectedAsset;
            }

            if (partType == data_girls_textures._spriteType.body)
            {
                return entry.Asset;
            }

            List<data_girls_textures._textureAsset> loadedAssets =
                GetStylistPortraitPartAssets(entry.Asset, partType);
            if (loadedAssets == null || loadedAssets.Count == ZeroCount)
            {
                return null;
            }

            for (int assetIndex = FirstItemIndex; assetIndex < loadedAssets.Count; assetIndex++)
            {
                data_girls_textures._textureAsset candidateAsset = loadedAssets[assetIndex];
                if (candidateAsset != null)
                {
                    return candidateAsset;
                }
            }

            return null;
        }

        private static void SetEntryAlreadyRecruited(UniqueIdolEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            if (entry.RecruitButton != null)
            {
                entry.RecruitButton.interactable = false;
            }

            if (entry.StatusText != null)
            {
                entry.StatusText.text = GetLocalized(AlreadyRecruitedStatusKey, AlreadyRecruitedStatusFallback);
                entry.StatusText.color = mainScript.white32;
            }
        }

        private static bool CanRecruitUniqueAsset(data_girls_textures._textureAsset asset)
        {
            if (asset == null)
            {
                return false;
            }

            if (data_girls.girl == null)
            {
                return true;
            }

            for (int girlIndex = 0; girlIndex < data_girls.girl.Count; girlIndex++)
            {
                data_girls.girls girl = data_girls.girl[girlIndex];
                if (girl == null || girl.textureAssets == null || girl.textureAssets.Count == ZeroCount)
                {
                    continue;
                }

                data_girls.girls._textureAsset bodyTexture = girl.GetTextureAsset(data_girls_textures._spriteType.body);
                if (bodyTexture == null || bodyTexture.asset == null)
                {
                    continue;
                }

                if (!TextureAssetMatches(bodyTexture.asset, asset))
                {
                    continue;
                }

                if (girl.status != data_girls._status.graduated || !asset.can_be_hired_again)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TextureAssetMatches(
            data_girls_textures._textureAsset left,
            data_girls_textures._textureAsset right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return left.body_id == right.body_id
                && string.Equals(
                    NormalizeModName(left.ModName),
                    NormalizeModName(right.ModName),
                    StringComparison.Ordinal);
        }

        private static int CompareUniqueIdolEntries(UniqueIdolEntry left, UniqueIdolEntry right)
        {
            if (left == null && right == null)
            {
                return ZeroCount;
            }

            if (left == null)
            {
                return SortLeftAfterRight;
            }

            if (right == null)
            {
                return SortLeftBeforeRight;
            }

            int rarityComparison = GetRaritySortValue(right).CompareTo(GetRaritySortValue(left));
            if (rarityComparison != ZeroCount)
            {
                return rarityComparison;
            }

            int nameComparison = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
            if (nameComparison != ZeroCount)
            {
                return nameComparison;
            }

            return BuildUniqueAssetKey(left.Asset).CompareTo(BuildUniqueAssetKey(right.Asset));
        }

        private static int GetRaritySortValue(UniqueIdolEntry entry)
        {
            if (entry == null)
            {
                return ZeroCount;
            }

            if (entry.Rarity == Auditions.data._girl._type.platinum)
            {
                return 4;
            }

            if (entry.Rarity == Auditions.data._girl._type.golden)
            {
                return 3;
            }

            if (entry.Rarity == Auditions.data._girl._type.silver)
            {
                return 2;
            }

            if (string.Equals(entry.RarityValue, RandomRarityValue, StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            return ZeroCount;
        }

        private static Vector2 CalculatePanelSize(int itemCount, RectTransform popupParentRect)
        {
            float screenWidth = popupParentRect != null && popupParentRect.rect.width > ZeroCount
                ? popupParentRect.rect.width
                : (Screen.width > ZeroCount ? Screen.width : FallbackScreenWidth);
            float screenHeight = popupParentRect != null && popupParentRect.rect.height > ZeroCount
                ? popupParentRect.rect.height
                : (Screen.height > ZeroCount ? Screen.height : FallbackScreenHeight);
            float widthLimit = Mathf.Min(
                MaximumPanelWidth,
                Mathf.Max(MinimumPanelWidth, screenWidth * MaximumPanelScreenWidthRatio));
            float panelWidth = widthLimit;
            int columns = CalculateGridColumnCount(panelWidth);
            float desiredHeight = CalculateGridContentHeight(itemCount, columns) + PanelVerticalChromeHeight;
            float heightWithScreenMargins = Mathf.Max(
                MinimumPanelHeight,
                screenHeight - (MinimumVerticalScreenMargin * 2f));
            float heightLimit = Mathf.Min(
                heightWithScreenMargins,
                Mathf.Min(
                    MaximumPanelHeight,
                    Mathf.Max(MinimumPanelHeight, screenHeight * MaximumPanelScreenHeightRatio)));
            float panelHeight = Mathf.Min(desiredHeight, heightLimit);
            panelHeight = Mathf.Max(MinimumPanelHeight, panelHeight);
            return new Vector2(panelWidth, panelHeight);
        }

        private static int CalculateGridColumnCount(float panelWidth)
        {
            float viewportWidth = CalculateViewportWidth(panelWidth);
            float availableWidth = viewportWidth - ContentPaddingLeft - ContentPaddingRight;
            int columns = Mathf.FloorToInt((availableWidth + TileSpacingX) / (TileWidth + TileSpacingX));
            return Mathf.Max(MinimumGridColumns, columns);
        }

        private static float CalculateViewportWidth(float panelWidth)
        {
            float scrollViewWidth = panelWidth - ScrollViewOffsetLeft + ScrollViewOffsetRight;
            return scrollViewWidth - ScrollbarViewportReserve;
        }

        internal static float CalculateGridContentHeight(int itemCount, int gridColumns)
        {
            if (itemCount <= ZeroCount)
            {
                return ContentPaddingTop + ContentPaddingBottom;
            }

            int safeColumns = Mathf.Max(MinimumGridColumns, gridColumns);
            int rowCount = Mathf.CeilToInt((float)itemCount / safeColumns);
            float itemRowsHeight = rowCount * TileHeight;
            float rowSpacingHeight = Mathf.Max(ZeroCount, rowCount - 1) * TileSpacingY;
            return ContentPaddingTop + itemRowsHeight + rowSpacingHeight + ContentPaddingBottom;
        }

        private static string BuildStatsText(data_girls.girls girl, int startIndex, int count)
        {
            StringBuilder builder = new StringBuilder();
            int endIndex = Mathf.Min(DisplayedStatTypes.Length, startIndex + count);
            for (int statIndex = startIndex; statIndex < endIndex; statIndex++)
            {
                data_girls._paramType statType = DisplayedStatTypes[statIndex];
                data_girls.girls.param parameter = girl.getParam(statType);
                int statValue = parameter != null ? parameter.GetIntegerPart() : ZeroCount;
                int potentialValue = parameter != null
                    ? Mathf.Clamp(parameter.GetPotential(), ZeroCount, MaximumDisplayedPotential)
                    : ZeroCount;
                if (builder.Length > ZeroCount)
                {
                    builder.Append(StatLineBreak);
                }

                builder.Append(string.Format(
                    CultureInfo.CurrentCulture,
                    GetLocalized(StatFormatKey, StatFormatFallback),
                    GetStatLabel(statType),
                    statValue.ToString(CultureInfo.CurrentCulture),
                    potentialValue.ToString(CultureInfo.CurrentCulture)));
            }

            return builder.ToString();
        }

        private static string GetStatLabel(data_girls._paramType statType)
        {
            if (statType == data_girls._paramType.cute)
            {
                return GetLocalized(StatCuteKey, StatCuteFallback);
            }

            if (statType == data_girls._paramType.cool)
            {
                return GetLocalized(StatCoolKey, StatCoolFallback);
            }

            if (statType == data_girls._paramType.sexy)
            {
                return GetLocalized(StatSexyKey, StatSexyFallback);
            }

            if (statType == data_girls._paramType.pretty)
            {
                return GetLocalized(StatPrettyKey, StatPrettyFallback);
            }

            if (statType == data_girls._paramType.dance)
            {
                return GetLocalized(StatDanceKey, StatDanceFallback);
            }

            if (statType == data_girls._paramType.vocal)
            {
                return GetLocalized(StatVocalKey, StatVocalFallback);
            }

            if (statType == data_girls._paramType.funny)
            {
                return GetLocalized(StatFunnyKey, StatFunnyFallback);
            }

            if (statType == data_girls._paramType.smart)
            {
                return GetLocalized(StatSmartKey, StatSmartFallback);
            }

            return data_girls.GetParamName(statType);
        }

        private static string GetRarityLabel(UniqueIdolEntry entry)
        {
            if (entry == null)
            {
                return GetLocalized(CommonRarityKey, CommonRarityFallback);
            }

            if (string.Equals(entry.RarityValue, RandomRarityValue, StringComparison.OrdinalIgnoreCase))
            {
                return GetLocalized(RandomRarityKey, RandomRarityFallback);
            }

            if (entry.Rarity == Auditions.data._girl._type.platinum)
            {
                return GetLocalized(PlatinumRarityKey, PlatinumRarityFallback);
            }

            if (entry.Rarity == Auditions.data._girl._type.golden)
            {
                return GetLocalized(GoldRarityKey, GoldRarityFallback);
            }

            if (entry.Rarity == Auditions.data._girl._type.silver)
            {
                return GetLocalized(SilverRarityKey, SilverRarityFallback);
            }

            return GetLocalized(CommonRarityKey, CommonRarityFallback);
        }

        private static Color32 GetRarityFrameColor(UniqueIdolEntry entry)
        {
            if (entry != null && string.Equals(entry.RarityValue, RandomRarityValue, StringComparison.OrdinalIgnoreCase))
            {
                return new Color32(RandomFrameRed, RandomFrameGreen, RandomFrameBlue, SolidColorAlpha);
            }

            if (entry != null && entry.Rarity == Auditions.data._girl._type.platinum)
            {
                return new Color32(PlatinumFrameColorChannel, PlatinumFrameColorChannel, PlatinumFrameColorChannel, SolidColorAlpha);
            }

            if (entry != null && entry.Rarity == Auditions.data._girl._type.golden)
            {
                return new Color32(GoldFrameRed, GoldFrameGreen, GoldFrameBlue, SolidColorAlpha);
            }

            if (entry != null && entry.Rarity == Auditions.data._girl._type.silver)
            {
                return new Color32(SilverFrameColorChannel, SilverFrameColorChannel, SilverFrameColorChannel, SolidColorAlpha);
            }

            return new Color32(CommonFrameRed, CommonFrameGreen, CommonFrameBlue, SolidColorAlpha);
        }

        private static void ApplyRarityFrameSprite(Image image, UniqueIdolEntry entry)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = GetRarityFrameSprite(entry);
            if (sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }

        private static void ApplyRarityBackgroundSprite(Image image, UniqueIdolEntry entry)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = GetRarityBackgroundSprite(entry);
            if (sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }

        private static Sprite GetRarityFrameSprite(UniqueIdolEntry entry)
        {
            Audition_Golden_Card template = GetRarityFrameTemplate();
            if (template == null || entry == null)
            {
                return null;
            }

            if (entry.Rarity == Auditions.data._girl._type.platinum)
            {
                return template.BG_Border_Platinum;
            }

            if (entry.Rarity == Auditions.data._girl._type.golden)
            {
                return template.BG_Border_Gold;
            }

            if (entry.Rarity == Auditions.data._girl._type.silver)
            {
                return template.BG_Border_Silver;
            }

            return template.BG_Border_Normal;
        }

        private static Sprite GetRarityBackgroundSprite(UniqueIdolEntry entry)
        {
            Audition_Golden_Card template = GetRarityFrameTemplate();
            if (template == null || entry == null)
            {
                return null;
            }

            if (entry.Rarity == Auditions.data._girl._type.platinum)
            {
                return template.BG_Platinum;
            }

            if (entry.Rarity == Auditions.data._girl._type.golden)
            {
                return template.BG_Gold;
            }

            if (entry.Rarity == Auditions.data._girl._type.silver)
            {
                return template.BG_Silver;
            }

            return template.BG_Normal;
        }

        private static Color32 GetPanelBackgroundColor()
        {
            return new Color32(
                PanelBackgroundColorChannel,
                PanelBackgroundColorChannel,
                PanelBackgroundColorChannel,
                SolidColorAlpha);
        }

        private static Color32 GetTransparentColor()
        {
            return new Color32(
                TransparentColorChannel,
                TransparentColorChannel,
                TransparentColorChannel,
                TransparentColorAlpha);
        }

        private static Color32 GetContentBackgroundColor()
        {
            return new Color32(
                ContentBackgroundColorChannel,
                ContentBackgroundColorChannel,
                ContentBackgroundColorChannel,
                SolidColorAlpha);
        }

        private static Color32 GetCardBackgroundColor()
        {
            return new Color32(
                CardBackgroundColorChannel,
                CardBackgroundColorChannel,
                CardBackgroundColorChannel,
                SolidColorAlpha);
        }

        private static Color32 GetMutedTextColor()
        {
            return new Color32(
                MutedTextColorChannel,
                MutedTextColorChannel,
                MutedTextColorChannel,
                SolidColorAlpha);
        }

        private static Color32 GetCardPrimaryTextColor(UniqueIdolEntry entry, bool canRecruit)
        {
            Color32 color = entry != null && entry.Rarity == Auditions.data._girl._type.platinum
                ? new Color32(
                    PlatinumCardPrimaryTextColorChannel,
                    PlatinumCardPrimaryTextColorChannel,
                    PlatinumCardPrimaryTextColorChannel,
                    SolidColorAlpha)
                : mainScript.black32;
            return canRecruit ? color : GetDisabledColor(color);
        }

        private static Color32 GetCardSecondaryTextColor(UniqueIdolEntry entry, bool canRecruit)
        {
            Color32 color = entry != null && entry.Rarity == Auditions.data._girl._type.platinum
                ? new Color32(
                    PlatinumCardSecondaryTextColorChannel,
                    PlatinumCardSecondaryTextColorChannel,
                    PlatinumCardSecondaryTextColorChannel,
                    SolidColorAlpha)
                : GetMutedTextColor();
            return canRecruit ? color : GetDisabledColor(color);
        }

        private static Color32 GetDisabledColor(Color32 color)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(color.r * DisabledColorMultiplier),
                (byte)Mathf.RoundToInt(color.g * DisabledColorMultiplier),
                (byte)Mathf.RoundToInt(color.b * DisabledColorMultiplier),
                AlreadyRecruitedAlpha);
        }

        private static Color32 GetScrollbarTrackColor()
        {
            Color32 color = GetContentBackgroundColor();
            color.a = ScrollbarTrackAlpha;
            return color;
        }

        private static string GetNormalizedRarityValue(data_girls_textures._textureAsset asset)
        {
            if (asset == null || string.IsNullOrEmpty(asset.Value))
            {
                return NormalRarityValue;
            }

            string value = asset.Value.Trim().ToLowerInvariant();
            if (string.Equals(value, GoldenRarityValue, StringComparison.Ordinal))
            {
                return GoldenRarityValue;
            }

            if (string.Equals(value, SilverRarityValue, StringComparison.Ordinal))
            {
                return SilverRarityValue;
            }

            if (string.Equals(value, PlatinumRarityValue, StringComparison.Ordinal))
            {
                return PlatinumRarityValue;
            }

            if (string.Equals(value, CommonRarityValue, StringComparison.Ordinal))
            {
                return CommonRarityValue;
            }

            if (string.Equals(value, RandomRarityValue, StringComparison.Ordinal))
            {
                return RandomRarityValue;
            }

            return NormalRarityValue;
        }

        private static string BuildUniqueAssetKey(data_girls_textures._textureAsset asset)
        {
            if (asset == null)
            {
                return EmptyString;
            }

            return string.Concat(
                NormalizeModName(asset.ModName),
                UniqueAssetKeySeparator,
                asset.body_id.ToString(CultureInfo.InvariantCulture),
                UniqueAssetKeySeparator,
                asset.Unique_ID ?? EmptyString);
        }

        private static string GetAssignedAssetName(data_girls_textures._textureAsset asset)
        {
            if (asset == null)
            {
                return EmptyString;
            }

            if (string.IsNullOrEmpty(asset.first_name))
            {
                return asset.last_name ?? asset.Unique_ID ?? EmptyString;
            }

            if (string.IsNullOrEmpty(asset.last_name))
            {
                return asset.first_name;
            }

            return string.Concat(asset.first_name, NamePartSeparator, asset.last_name);
        }

        private static string NormalizeModName(string modName)
        {
            if (string.IsNullOrEmpty(modName) || string.Equals(modName, DefaultModName, StringComparison.Ordinal))
            {
                return DefaultTextureAssetModName;
            }

            return modName;
        }

        private static bool TryRegisterPopup(PopupManager manager, GameObject root)
        {
            if (manager == null || root == null)
            {
                return false;
            }

            PopupManager._type type = (PopupManager._type)UniqueIdolRecruitmentPopupTypeValue;
            PopupManager._popup existing = manager.GetByType(type);
            if (existing != null)
            {
                if (existing.obj != null && existing.obj != root)
                {
                    UnityEngine.Object.Destroy(existing.obj);
                }

                existing.obj = root;
                existing.open = false;
                existing.BGBlur = true;
                existing.BGDarken = true;
                existing.BGRenderTexture = null;
                return true;
            }

            PopupManager._popup popup = new PopupManager._popup
            {
                type = type,
                obj = root,
                open = false,
                BGBlur = true,
                BGDarken = true
            };

            if (manager.popups == null)
            {
                manager.popups = new PopupManager._popup[] { popup };
                return true;
            }

            Array.Resize(ref manager.popups, manager.popups.Length + 1);
            manager.popups[manager.popups.Length - 1] = popup;
            return true;
        }

        private static Transform GetPopupParent()
        {
            PopupManager manager = GetPopupManager();
            if (manager != null && manager.popups != null)
            {
                PopupManager._popup awardsPopup = manager.GetByType(PopupManager._type.awards);
                if (awardsPopup != null && awardsPopup.obj != null && awardsPopup.obj.transform.parent != null)
                {
                    return awardsPopup.obj.transform.parent;
                }

                for (int popupIndex = 0; popupIndex < manager.popups.Length; popupIndex++)
                {
                    PopupManager._popup popup = manager.popups[popupIndex];
                    if (popup != null && popup.obj != null && popup.obj.transform.parent != null)
                    {
                        return popup.obj.transform.parent;
                    }
                }
            }

            return null;
        }

        private static Scrollbar GetScrollbarTemplate()
        {
            Scrollbar preferred = FindScrollbarInPopup(PopupManager._type.producer_salaries);
            if (preferred != null)
            {
                return preferred;
            }

            preferred = FindScrollbarInPopup(PopupManager._type.producer_contracts);
            if (preferred != null)
            {
                return preferred;
            }

            preferred = FindScrollbarInPopup(PopupManager._type.producer_loans);
            if (preferred != null)
            {
                return preferred;
            }

            preferred = FindScrollbarInPopup(PopupManager._type.notifications);
            if (preferred != null)
            {
                return preferred;
            }

            preferred = FindScrollbarInPopup(PopupManager._type.awards);
            if (preferred != null)
            {
                return preferred;
            }

            preferred = FindScrollbarInPopup(PopupManager._type.single_release);
            if (preferred != null)
            {
                return preferred;
            }

            preferred = FindScrollbarInPopup(PopupManager._type.single_senbatsu);
            if (preferred != null)
            {
                return preferred;
            }

            preferred = FindScrollbarInPopup(PopupManager._type.single_chart);
            if (preferred != null)
            {
                return preferred;
            }

            preferred = FindScrollbarInPopup(PopupManager._type.SNS);
            if (preferred != null)
            {
                return preferred;
            }

            Scrollbar[] scrollbars = UnityEngine.Object.FindObjectsOfType<Scrollbar>();
            if (scrollbars != null && scrollbars.Length > ZeroCount)
            {
                return scrollbars[FirstTemplateIndex];
            }

            return null;
        }

        private static Audition_Golden_Card GetRarityFrameTemplate()
        {
            if (rarityFrameTemplate != null)
            {
                return rarityFrameTemplate;
            }

            PopupManager manager = GetPopupManager();
            if (manager == null)
            {
                return null;
            }

            PopupManager._popup auditionEntry = manager.GetByType(PopupManager._type.audition);
            if (auditionEntry == null || auditionEntry.obj == null)
            {
                return null;
            }

            Popup_Audition auditionPopup = auditionEntry.obj.GetComponent<Popup_Audition>();
            if (auditionPopup == null || auditionPopup.prefab_golden_card == null)
            {
                return null;
            }

            rarityFrameTemplate = auditionPopup.prefab_golden_card.GetComponent<Audition_Golden_Card>();
            return rarityFrameTemplate;
        }

        private static Scrollbar FindScrollbarInPopup(PopupManager._type type)
        {
            GameObject popup = null;
            PopupManager manager = GetPopupManager();
            if (manager != null)
            {
                PopupManager._popup entry = manager.GetByType(type);
                if (entry != null)
                {
                    popup = entry.obj;
                }
            }

            if (popup == null)
            {
                return null;
            }

            Scrollbar[] scrollbars = popup.GetComponentsInChildren<Scrollbar>(true);
            if (scrollbars != null && scrollbars.Length > ZeroCount)
            {
                return scrollbars[FirstTemplateIndex];
            }

            return null;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            if (parent != null)
            {
                obj.layer = parent.gameObject.layer;
            }

            return obj;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string text,
            int fontSize,
            TextAlignmentOptions alignment,
            Color32 color)
        {
            GameObject obj = CreateUIObject(name, parent);
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            CaptureDefaultFont();
            if (defaultFontSource != null && defaultFontSource.font != null)
            {
                tmp.font = defaultFontSource.font;
            }

            return tmp;
        }

        private static void CaptureDefaultFont()
        {
            if (defaultFontSource != null)
            {
                return;
            }

            TextMeshProUGUI[] textObjects = UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>();
            if (textObjects == null)
            {
                return;
            }

            for (int textIndex = 0; textIndex < textObjects.Length; textIndex++)
            {
                if (textObjects[textIndex] != null && textObjects[textIndex].font != null)
                {
                    defaultFontSource = textObjects[textIndex];
                    return;
                }
            }
        }

        private static void Close()
        {
            PopupManager.Close_();
        }

        private static void DestroyExistingRoot()
        {
            if (popupRoot == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(popupRoot);
            popupRoot = null;
        }

        private static PopupManager GetPopupManager()
        {
            GameObject dataObject = GetMainScriptDataObject();
            return dataObject != null ? dataObject.GetComponent<PopupManager>() : null;
        }

        private static T GetDataComponent<T>() where T : Component
        {
            GameObject dataObject = GetMainScriptDataObject();
            return dataObject != null ? dataObject.GetComponent<T>() : null;
        }

        private static GameObject GetMainScriptDataObject()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return null;
            }

            mainScript main = camera.GetComponent<mainScript>();
            return main != null ? main.Data : null;
        }

        private static void NotifyWarning(string localizationKey, string fallback)
        {
            NotificationManager.AddNotification(
                GetLocalized(localizationKey, fallback),
                mainScript.red32,
                NotificationManager._notification._type.other);
        }

        private static string GetLocalized(string localizationKey, string fallback)
        {
            return ModLocalization.Get(localizationKey, fallback);
        }

        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null)
            {
                return;
            }

            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                if (child != null)
                {
                    SetLayerRecursively(child.gameObject, layer);
                }
            }
        }

        internal sealed class UniqueIdolEntry
        {
            internal data_girls_textures._textureAsset Asset;
            internal data_girls.girls PreviewGirl;
            internal List<PortraitPartState> PortraitParts;
            internal Auditions.data._girl._type Rarity;
            internal string RarityValue;
            internal string Name;
            internal string ModName;
            internal string ModTitle;
            internal int Index;
            internal GameObject SlotObject;
            internal GameObject TileObject;
            internal Button RecruitButton;
            internal TextMeshProUGUI StatusText;
            internal bool Recruited;
        }

        internal sealed class PortraitPartState
        {
            internal data_girls_textures._spriteType Type;
            internal List<data_girls_textures._textureAsset> Assets;
            internal int SelectedIndex;
            internal bool AllowNoSelection;
            internal Image PortraitImage;
            internal TextMeshProUGUI SelectorLabel;
            internal Texture2D DisplayTexture;
            internal Sprite DisplaySprite;
            internal data_girls_textures._textureAsset RequestedAsset;
            internal bool LoadAttempted;

            internal data_girls_textures._textureAsset SelectedAsset
            {
                get
                {
                    return Assets != null
                        && SelectedIndex >= FirstItemIndex
                        && SelectedIndex < Assets.Count
                        ? Assets[SelectedIndex]
                        : null;
                }
            }
        }
    }

    internal sealed class UniqueIdolRecruitmentViewport : MonoBehaviour
    {
        private const string EmptySearchQuery = "";
        private const int NoVisibleEntryIndex = -1;
        private const int FirstEntryIndex = 0;
        private const int SingleIndexOffset = 1;
        private const int VisibleRowBufferCount = 0;
        private const int MaximumCardsCreatedPerFrame = 1;
        private const int InitialPopulationDelayFrameCount = 3;
        private const int MinimumGridColumnCount = 1;
        private const float MinimumRowStride = 1f;

        private data_girls dataGirls;
        private data_girls_textures textureData;
        private List<UniqueIdolRecruitmentPopup.UniqueIdolEntry> entries;
        private readonly List<UniqueIdolRecruitmentPopup.UniqueIdolEntry> visibleEntries =
            new List<UniqueIdolRecruitmentPopup.UniqueIdolEntry>();
        private ScrollRect scrollRect;
        private RectTransform contentRect;
        private int gridColumns;
        private float rowStride;
        private float panelWidth;
        private string searchQuery = EmptySearchQuery;
        private int firstVisibleEntryIndex = NoVisibleEntryIndex;
        private int lastVisibleEntryIndex = NoVisibleEntryIndex;
        private int remainingPopulationDelayFrames = InitialPopulationDelayFrameCount;
        private bool portraitLoadInProgress;
        private bool initialized;

        internal void Initialize(
            data_girls girlsData,
            data_girls_textures girlsTextureData,
            List<UniqueIdolRecruitmentPopup.UniqueIdolEntry> uniqueIdolEntries,
            ScrollRect targetScrollRect,
            RectTransform targetContentRect,
            int targetGridColumns,
            float targetRowStride,
            float targetPanelWidth)
        {
            dataGirls = girlsData;
            textureData = girlsTextureData;
            entries = uniqueIdolEntries;
            scrollRect = targetScrollRect;
            contentRect = targetContentRect;
            gridColumns = Mathf.Max(MinimumGridColumnCount, targetGridColumns);
            rowStride = Mathf.Max(MinimumRowStride, targetRowStride);
            panelWidth = targetPanelWidth;
            visibleEntries.Clear();
            if (entries != null)
            {
                visibleEntries.AddRange(entries);
            }
            initialized = dataGirls != null
                && textureData != null
                && entries != null
                && scrollRect != null
                && contentRect != null;
        }

        internal void ApplyFilter(string query)
        {
            if (!initialized)
            {
                return;
            }

            string normalizedQuery = string.IsNullOrEmpty(query)
                ? EmptySearchQuery
                : query.Trim();
            if (string.Equals(searchQuery, normalizedQuery, StringComparison.Ordinal))
            {
                return;
            }

            searchQuery = normalizedQuery;
            StopAllCoroutines();
            portraitLoadInProgress = false;
            visibleEntries.Clear();

            for (int entryIndex = FirstEntryIndex; entryIndex < entries.Count; entryIndex++)
            {
                UniqueIdolRecruitmentPopup.UniqueIdolEntry entry = entries[entryIndex];
                UniqueIdolRecruitmentPopup.ReleaseEntryPortrait(entry);
                bool matchesSearch = MatchesSearch(entry, normalizedQuery);
                if (entry.SlotObject != null)
                {
                    entry.SlotObject.SetActive(matchesSearch);
                }

                if (!matchesSearch)
                {
                    UniqueIdolRecruitmentPopup.ReleaseEntryTile(entry);
                    continue;
                }

                int layoutIndex = visibleEntries.Count;
                visibleEntries.Add(entry);
                UniqueIdolRecruitmentPopup.PositionUniqueIdolSlot(
                    entry,
                    layoutIndex,
                    panelWidth,
                    gridColumns);
            }

            contentRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                UniqueIdolRecruitmentPopup.CalculateGridContentHeight(
                    visibleEntries.Count,
                    gridColumns));
            scrollRect.StopMovement();
            contentRect.anchoredPosition = Vector2.zero;
            scrollRect.verticalNormalizedPosition = 1f;
            firstVisibleEntryIndex = NoVisibleEntryIndex;
            lastVisibleEntryIndex = NoVisibleEntryIndex;
            remainingPopulationDelayFrames = FirstEntryIndex;
        }

        private static bool MatchesSearch(
            UniqueIdolRecruitmentPopup.UniqueIdolEntry entry,
            string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return true;
            }

            if (entry == null)
            {
                return false;
            }

            string displayedName = entry.PreviewGirl != null
                ? entry.PreviewGirl.GetName(true)
                : entry.Name;
            return ContainsIgnoreCase(displayedName, query)
                || ContainsIgnoreCase(entry.Name, query)
                || ContainsIgnoreCase(entry.ModName, query)
                || ContainsIgnoreCase(entry.ModTitle, query);
        }

        private static bool ContainsIgnoreCase(string value, string query)
        {
            return !string.IsNullOrEmpty(value)
                && value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= FirstEntryIndex;
        }

        private void Update()
        {
            if (!initialized || visibleEntries.Count == FirstEntryIndex)
            {
                return;
            }

            if (remainingPopulationDelayFrames > FirstEntryIndex)
            {
                remainingPopulationDelayFrames--;
                return;
            }

            CalculateVisibleEntryRange();
            ReleaseOffscreenContent();
            CreateVisibleCardsIncrementally();
            RefreshVisiblePortraits();
            LoadNextVisiblePortrait();
        }

        private void CalculateVisibleEntryRange()
        {
            float viewportHeight = scrollRect.viewport != null
                ? scrollRect.viewport.rect.height
                : rowStride;
            viewportHeight = Mathf.Max(rowStride, viewportHeight);
            float contentOffset = Mathf.Max(0f, contentRect.anchoredPosition.y);
            int totalRowCount = Mathf.CeilToInt((float)visibleEntries.Count / gridColumns);
            int firstVisibleRow = Mathf.FloorToInt(contentOffset / rowStride) - VisibleRowBufferCount;
            int lastVisibleRow = Mathf.FloorToInt(
                (contentOffset + viewportHeight) / rowStride) + VisibleRowBufferCount;
            firstVisibleRow = Mathf.Clamp(
                firstVisibleRow,
                FirstEntryIndex,
                Mathf.Max(FirstEntryIndex, totalRowCount - SingleIndexOffset));
            lastVisibleRow = Mathf.Clamp(
                lastVisibleRow,
                firstVisibleRow,
                Mathf.Max(FirstEntryIndex, totalRowCount - SingleIndexOffset));

            firstVisibleEntryIndex = firstVisibleRow * gridColumns;
            lastVisibleEntryIndex = Mathf.Min(
                visibleEntries.Count - SingleIndexOffset,
                ((lastVisibleRow + SingleIndexOffset) * gridColumns) - SingleIndexOffset);
        }

        private void ReleaseOffscreenContent()
        {
            for (int entryIndex = FirstEntryIndex; entryIndex < visibleEntries.Count; entryIndex++)
            {
                if (IsEntryVisible(entryIndex))
                {
                    continue;
                }

                UniqueIdolRecruitmentPopup.UniqueIdolEntry entry = visibleEntries[entryIndex];
                UniqueIdolRecruitmentPopup.ReleaseEntryTile(entry);
                UniqueIdolRecruitmentPopup.ReleaseEntryPortrait(entry);
            }
        }

        private void CreateVisibleCardsIncrementally()
        {
            int cardsCreated = FirstEntryIndex;
            for (int entryIndex = firstVisibleEntryIndex;
                entryIndex <= lastVisibleEntryIndex && entryIndex < visibleEntries.Count;
                entryIndex++)
            {
                UniqueIdolRecruitmentPopup.UniqueIdolEntry entry = visibleEntries[entryIndex];
                if (entry.TileObject != null)
                {
                    continue;
                }

                if (cardsCreated >= MaximumCardsCreatedPerFrame)
                {
                    break;
                }

                if (!UniqueIdolRecruitmentPopup.EnsurePreviewGirl(dataGirls, textureData, entry))
                {
                    continue;
                }

                UniqueIdolRecruitmentPopup.AddUniqueIdolTile(entry);
                cardsCreated++;
            }
        }

        private void RefreshVisiblePortraits()
        {
            for (int entryIndex = firstVisibleEntryIndex;
                entryIndex <= lastVisibleEntryIndex && entryIndex < visibleEntries.Count;
                entryIndex++)
            {
                UniqueIdolRecruitmentPopup.RefreshEntryPortrait(visibleEntries[entryIndex]);
            }
        }

        private void LoadNextVisiblePortrait()
        {
            if (portraitLoadInProgress)
            {
                return;
            }

            for (int entryIndex = firstVisibleEntryIndex;
                entryIndex <= lastVisibleEntryIndex && entryIndex < visibleEntries.Count;
                entryIndex++)
            {
                UniqueIdolRecruitmentPopup.UniqueIdolEntry entry = visibleEntries[entryIndex];
                if (entry.TileObject == null)
                {
                    continue;
                }

                UniqueIdolRecruitmentPopup.PortraitPartState partState =
                    UniqueIdolRecruitmentPopup.GetNextPortraitPartToLoad(entry);
                if (partState == null)
                {
                    continue;
                }

                portraitLoadInProgress = true;
                StartCoroutine(LoadVisiblePortrait(entry, partState));
                return;
            }
        }

        private IEnumerator LoadVisiblePortrait(
            UniqueIdolRecruitmentPopup.UniqueIdolEntry entry,
            UniqueIdolRecruitmentPopup.PortraitPartState partState)
        {
            yield return UniqueIdolRecruitmentPopup.LoadEntryPortraitPart(entry, partState);
            portraitLoadInProgress = false;
        }

        private bool IsEntryVisible(int entryIndex)
        {
            return entryIndex >= firstVisibleEntryIndex && entryIndex <= lastVisibleEntryIndex;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            portraitLoadInProgress = false;
            ReleaseAllContent();
        }

        private void OnDestroy()
        {
            ReleaseAllContent();
        }

        private void ReleaseAllContent()
        {
            if (entries == null)
            {
                return;
            }

            for (int entryIndex = FirstEntryIndex; entryIndex < entries.Count; entryIndex++)
            {
                UniqueIdolRecruitmentPopup.UniqueIdolEntry entry = entries[entryIndex];
                UniqueIdolRecruitmentPopup.ReleaseEntryTile(entry);
                UniqueIdolRecruitmentPopup.ReleaseEntryPortrait(entry);
            }

            firstVisibleEntryIndex = NoVisibleEntryIndex;
            lastVisibleEntryIndex = NoVisibleEntryIndex;
        }
    }
}
