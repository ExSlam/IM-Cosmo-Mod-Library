using System;
using System.Collections.Generic;
using System.IO;
using ModLocalizationSystem;
using StaffPortraits.EmbeddedIMUiFramework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace StaffPortraits
{
    public static class StaffPortraitActions
    {
        /// <summary>
        /// Public Mod Buttons action. Mod Buttons calls this method when the player chooses
        /// Staff Stylist from the Action Hub.
        /// </summary>
        public static void OpenStaffStylist()
        {
            StaffPortraitStylist.Open();
        }
    }

    internal static partial class StaffPortraitStylist
    {
        private static readonly PopupManager._type PopupType =
            (PopupManager._type)StaffPortraitsConstants.PopupTypeValue;

        private static PopupManager popupManager;
        private static PopupScaffold scaffold;
        private static readonly List<staff._staff> PickerStaff = new List<staff._staff>();
        private static int selectedStaffIndex;
        private static StaffPortraitSelection selection;

        private static TextMeshProUGUI staffNameText;
        private static TextMeshProUGUI staffRoleText;
        private static TextMeshProUGUI stateText;
        private static readonly Dictionary<data_girls_textures._spriteType, TextMeshProUGUI> SelectorValueTexts =
            new Dictionary<data_girls_textures._spriteType, TextMeshProUGUI>();
        private static TextMeshProUGUI packValueText;
        private static Button applyButton;
        private static Button randomizeButton;
        private static readonly List<Image> PreviewLayers = new List<Image>();
        private static readonly List<Sprite> PreviewSprites = new List<Sprite>();
        private static readonly List<Texture2D> PreviewTextures = new List<Texture2D>();
        private static IMUiTheme theme;

        internal static void Initialize(PopupManager manager)
        {
            if (manager == null)
            {
                return;
            }

            if (ReferenceEquals(popupManager, manager) && scaffold != null && scaffold.IsValid)
            {
                return;
            }

            ResetUiState();
            popupManager = manager;
            IMUiKit.Initialize(manager);
            PopupManager._popup existing = manager.GetByType(PopupType);
            if (existing != null)
            {
                Debug.LogWarning(StaffPortraitsConstants.LogPrefix + "The Staff Stylist popup ID is already registered.");
                return;
            }

            PopupScaffold created;
            if (!IMUiKit.TryCreateRegisteredPopupScaffold(
                PopupType,
                "StaffPortraits_StaffStylist",
                Text(StaffPortraitsConstants.TitleKey, StaffPortraitsConstants.TitleFallback),
                new Vector2(StaffPortraitsConstants.PopupWidth, StaffPortraitsConstants.PopupHeight),
                true,
                true,
                out created))
            {
                Debug.LogWarning(StaffPortraitsConstants.LogPrefix + "Could not create the Staff Stylist popup.");
                return;
            }

            scaffold = created;
            theme = IMUiTheme.Vanilla();
            scaffold.Popup.OnOpen.AddListener(OnOpened);
            BuildEditorUi();
        }

        internal static void Open()
        {
            EnsureInitializedFromGame();
            if (scaffold == null || !scaffold.IsValid)
            {
                Debug.LogWarning(StaffPortraitsConstants.LogPrefix + "Staff Stylist is unavailable because PopupManager is not ready.");
                return;
            }

            if (!IMUiKit.TryOpenRegisteredPopup(PopupType))
            {
                Debug.LogWarning(StaffPortraitsConstants.LogPrefix + "Staff Stylist could not be opened through PopupManager.");
            }
        }

        private static void EnsureInitializedFromGame()
        {
            if (scaffold != null && scaffold.IsValid)
            {
                return;
            }

            PopupManager manager = popupManager;
            if (manager == null && Camera.main != null)
            {
                mainScript main = Camera.main.GetComponent<mainScript>();
                if (main != null && main.Data != null)
                {
                    manager = main.Data.GetComponent<PopupManager>();
                }
            }
            Initialize(manager);
        }


        private static void ResetUiState()
        {
            ClearPreviewResources();
            PreviewLayers.Clear();
            SelectorValueTexts.Clear();
            PickerStaff.Clear();
            selectedStaffIndex = StaffPortraitsConstants.FirstIndex;
            selection = null;
            staffNameText = null;
            staffRoleText = null;
            stateText = null;
            packValueText = null;
            applyButton = null;
            randomizeButton = null;
            scaffold = null;
            theme = null;
            ResetPickerUi();
        }

        private static void OnOpened()
        {
            if (scaffold != null && scaffold.TitleText != null)
            {
                scaffold.TitleText.text = Text(StaffPortraitsConstants.TitleKey, StaffPortraitsConstants.TitleFallback);
            }
            if (scaffold != null && scaffold.CloseButton != null)
            {
                StaffPortraitUi.SetButtonText(scaffold.CloseButton, Text(StaffPortraitsConstants.CloseKey, StaffPortraitsConstants.CloseFallback));
            }

            StaffPortraitCatalog.EnsureLoaded();
            RefreshPickerStaff();
            selectedStaffIndex = Mathf.Clamp(selectedStaffIndex, 0, Mathf.Max(0, PickerStaff.Count - 1));
            RebuildStaffPicker();
            LoadCurrentStaffSelection();
            Render();
        }

        private static void RefreshPickerStaff()
        {
            PickerStaff.Clear();
            if (staff.Staff == null)
            {
                return;
            }

            for (int staffIndex = 0; staffIndex < staff.Staff.Count; staffIndex++)
            {
                staff._staff staffer = staff.Staff[staffIndex];
                if (StaffPortraitAssignment.IsListedStaff(staffer))
                {
                    PickerStaff.Add(staffer);
                }
            }
        }

        private static void LoadCurrentStaffSelection()
        {
            if (PickerStaff.Count == 0 || StaffPortraitCatalog.AllPacks.Count == 0
                || !StaffPortraitAssignment.IsEditableStaff(PickerStaff[selectedStaffIndex]))
            {
                selection = null;
                return;
            }

            staff._staff staffer = PickerStaff[selectedStaffIndex];
            StaffPortraitPack fallbackPack = StaffPortraitCatalog.AllPacks[0];
            selection = StaffPortraitAssignment.CreateSelectionFromCurrentPortrait(staffer, fallbackPack);
            NormalizeSelection();
        }

        private static void ChangePack(int offset)
        {
            IList<StaffPortraitPack> packs = StaffPortraitCatalog.AllPacks;
            if (packs.Count == 0)
            {
                return;
            }

            int currentIndex = 0;
            if (selection != null && selection.Pack != null)
            {
                for (int packIndex = 0; packIndex < packs.Count; packIndex++)
                {
                    if (ReferenceEquals(packs[packIndex], selection.Pack))
                    {
                        currentIndex = packIndex;
                        break;
                    }
                }
            }

            StaffPortraitPack selectedPack = packs[WrapIndex(currentIndex + offset, packs.Count)];
            selection = new StaffPortraitSelection
            {
                Pack = selectedPack,
                BodyIndex = 0,
                HairIndex = 0,
                FaceIndex = 0,
                AccessoryIndex = StaffPortraitsConstants.AccessoryNoneSelectionIndex
            };
            NormalizeSelection();
            Render();
        }

        private static void ChangePart(data_girls_textures._spriteType type, int offset)
        {
            if (selection == null || selection.Pack == null)
            {
                return;
            }

            List<StaffPortraitAsset> assets = StaffPortraitCatalog.GetAssetList(selection.Pack, type);
            if (type == data_girls_textures._spriteType.acc)
            {
                int optionCount = assets.Count + 1;
                int currentOption = selection.AccessoryIndex < 0 ? 0 : selection.AccessoryIndex + 1;
                int nextOption = WrapIndex(currentOption + offset, optionCount);
                selection.AccessoryIndex = nextOption == 0
                    ? StaffPortraitsConstants.AccessoryNoneSelectionIndex
                    : nextOption - 1;
            }
            else if (assets.Count > 0)
            {
                int currentIndex = GetSelectionIndex(type);
                SetSelectionIndex(type, WrapIndex(currentIndex + offset, assets.Count));
            }

            Render();
        }

        private static void RandomizeSelection()
        {
            if (selection == null || selection.Pack == null)
            {
                return;
            }

            selection = StaffPortraitAssignment.CreateRandomSelection(selection.Pack);
            Render();
        }

        private static void ApplySelection()
        {
            if (PickerStaff.Count == 0 || selection == null)
            {
                return;
            }

            staff._staff staffer = PickerStaff[selectedStaffIndex];
            if (!StaffPortraitAssignment.Apply(staffer, selection))
            {
                return;
            }

            string format = Text(StaffPortraitsConstants.AppliedNotificationKey, StaffPortraitsConstants.AppliedNotificationFallback);
            NotificationManager.AddNotification(
                string.Format(format, staffer.GetName(true, false)),
                mainScript.green32,
                NotificationManager._notification._type.idol_stat_change);
        }

        private static void NormalizeSelection()
        {
            if (selection == null || selection.Pack == null)
            {
                return;
            }

            selection.BodyIndex = ClampRequiredIndex(selection.BodyIndex, selection.Pack.BodyAssets.Count);
            selection.HairIndex = ClampRequiredIndex(selection.HairIndex, selection.Pack.HairAssets.Count);
            selection.FaceIndex = ClampRequiredIndex(selection.FaceIndex, selection.Pack.FaceAssets.Count);
            if (selection.AccessoryIndex >= selection.Pack.AccessoryAssets.Count)
            {
                selection.AccessoryIndex = StaffPortraitsConstants.AccessoryNoneSelectionIndex;
            }
        }

        private static int ClampRequiredIndex(int index, int count)
        {
            if (count <= 0)
            {
                return StaffPortraitsConstants.NoSelectionIndex;
            }
            return Mathf.Clamp(index, 0, count - 1);
        }

        private static void Render()
        {
            bool hasStaff = PickerStaff.Count > 0;
            bool hasPacks = StaffPortraitCatalog.AllPacks.Count > 0;
            bool editable = hasStaff && StaffPortraitAssignment.IsEditableStaff(PickerStaff[selectedStaffIndex]);
            bool ready = editable && hasPacks && selection != null && selection.Pack != null && selection.Pack.HasRequiredAssets;

            if (staffNameText != null)
            {
                staffNameText.text = hasStaff ? PickerStaff[selectedStaffIndex].GetName(true, false) : string.Empty;
            }
            if (staffRoleText != null)
            {
                staffRoleText.text = hasStaff ? GetStaffJobTitle(PickerStaff[selectedStaffIndex]) : string.Empty;
            }

            if (stateText != null)
            {
                string stateMessage = string.Empty;
                if (!hasStaff)
                {
                    stateMessage = Text(StaffPortraitsConstants.NoStaffKey, StaffPortraitsConstants.NoStaffFallback);
                }
                else if (!editable)
                {
                    stateMessage = Text(StaffPortraitsConstants.ProtectedPortraitKey, StaffPortraitsConstants.ProtectedPortraitFallback);
                }
                else if (!hasPacks)
                {
                    stateMessage = Text(StaffPortraitsConstants.NoPacksKey, StaffPortraitsConstants.NoPacksFallback);
                }
                else if (!ready)
                {
                    stateMessage = Text(StaffPortraitsConstants.PackEmptyKey, StaffPortraitsConstants.PackEmptyFallback);
                }
                stateText.text = stateMessage;
                stateText.gameObject.SetActive(!string.IsNullOrEmpty(stateMessage));
            }

            if (applyButton != null)
            {
                StaffPortraitUi.SetInteractable(applyButton, ready);
                StaffPortraitUi.SetButtonText(applyButton, Text(StaffPortraitsConstants.ApplyKey, StaffPortraitsConstants.ApplyFallback));
            }
            if (randomizeButton != null)
            {
                StaffPortraitUi.SetInteractable(randomizeButton, ready);
                StaffPortraitUi.SetButtonText(randomizeButton, Text(StaffPortraitsConstants.RandomizeKey, StaffPortraitsConstants.RandomizeFallback));
            }

            if (packValueText != null)
            {
                packValueText.text = selection != null && selection.Pack != null ? selection.Pack.DisplayName : string.Empty;
            }
            UpdateSelectorValue(data_girls_textures._spriteType.body, selection != null ? selection.BodyIndex : -1);
            UpdateSelectorValue(data_girls_textures._spriteType.hair, selection != null ? selection.HairIndex : -1);
            UpdateSelectorValue(data_girls_textures._spriteType.face, selection != null ? selection.FaceIndex : -1);
            UpdateSelectorValue(data_girls_textures._spriteType.acc, selection != null ? selection.AccessoryIndex : -1);
            RefreshPickerSelection();
            foreach (Button selectorButton in SelectorButtons)
                StaffPortraitUi.SetInteractable(selectorButton, editable && hasPacks);
            RenderPreview(ready);
        }

        private static void UpdateSelectorValue(data_girls_textures._spriteType type, int index)
        {
            TextMeshProUGUI valueText;
            if (!SelectorValueTexts.TryGetValue(type, out valueText) || valueText == null)
            {
                return;
            }

            if (selection == null || selection.Pack == null)
            {
                valueText.text = string.Empty;
                return;
            }

            List<StaffPortraitAsset> assets = StaffPortraitCatalog.GetAssetList(selection.Pack, type);
            if (type == data_girls_textures._spriteType.acc && index < 0)
            {
                valueText.text = Text(StaffPortraitsConstants.NoneLabelKey, StaffPortraitsConstants.NoneLabelFallback);
                return;
            }

            valueText.text = index >= 0 && index < assets.Count && assets[index] != null
                ? assets[index].DisplayName
                : string.Empty;
        }

        private static void RenderPreview(bool ready)
        {
            ClearPreviewResources();
            for (int layerIndex = 0; layerIndex < PreviewLayers.Count; layerIndex++)
            {
                PreviewLayers[layerIndex].sprite = null;
                PreviewLayers[layerIndex].enabled = false;
            }
            if (!ready)
            {
                return;
            }

            StaffPortraitAsset[] orderedAssets =
            {
                GetSelectedAsset(selection.Pack.BodyAssets, selection.BodyIndex),
                GetSelectedAsset(selection.Pack.FaceAssets, selection.FaceIndex),
                GetSelectedAsset(selection.Pack.HairAssets, selection.HairIndex),
                GetSelectedAsset(selection.Pack.AccessoryAssets, selection.AccessoryIndex)
            };

            for (int layerIndex = 0; layerIndex < orderedAssets.Length && layerIndex < PreviewLayers.Count; layerIndex++)
            {
                StaffPortraitAsset asset = orderedAssets[layerIndex];
                if (asset == null)
                {
                    continue;
                }

                Sprite sprite;
                Texture2D texture;
                if (!TryLoadSprite(asset.FullPath, out sprite, out texture))
                {
                    continue;
                }

                PreviewSprites.Add(sprite);
                PreviewTextures.Add(texture);
                PreviewLayers[layerIndex].sprite = sprite;
                PreviewLayers[layerIndex].enabled = true;
            }
        }

        private static bool TryLoadSprite(string path, out Sprite sprite, out Texture2D texture)
        {
            sprite = null;
            texture = null;
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    return false;
                }

                byte[] bytes = File.ReadAllBytes(path);
                texture = new Texture2D(StaffPortraitsConstants.PreviewTextureBootstrapPixels, StaffPortraitsConstants.PreviewTextureBootstrapPixels, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(texture, bytes))
                {
                    UnityEngine.Object.Destroy(texture);
                    texture = null;
                    return false;
                }
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    StaffPortraitsConstants.PortraitSpritePixelsPerUnit,
                    StaffPortraitsConstants.SpriteExtrudePixels,
                    SpriteMeshType.FullRect);
                return sprite != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(StaffPortraitsConstants.LogPrefix + "Could not load a portrait preview image: " + exception.Message);
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                    texture = null;
                }
                return false;
            }
        }

        private static void ClearPreviewResources()
        {
            for (int spriteIndex = 0; spriteIndex < PreviewSprites.Count; spriteIndex++)
            {
                if (PreviewSprites[spriteIndex] != null)
                {
                    UnityEngine.Object.Destroy(PreviewSprites[spriteIndex]);
                }
            }
            PreviewSprites.Clear();

            for (int textureIndex = 0; textureIndex < PreviewTextures.Count; textureIndex++)
            {
                if (PreviewTextures[textureIndex] != null)
                {
                    UnityEngine.Object.Destroy(PreviewTextures[textureIndex]);
                }
            }
            PreviewTextures.Clear();
        }

        private static int GetSelectionIndex(data_girls_textures._spriteType type)
        {
            switch (type)
            {
                case data_girls_textures._spriteType.body:
                    return selection.BodyIndex;
                case data_girls_textures._spriteType.hair:
                    return selection.HairIndex;
                case data_girls_textures._spriteType.face:
                    return selection.FaceIndex;
                case data_girls_textures._spriteType.acc:
                    return selection.AccessoryIndex;
                default:
                    return StaffPortraitsConstants.NoSelectionIndex;
            }
        }

        private static void SetSelectionIndex(data_girls_textures._spriteType type, int index)
        {
            switch (type)
            {
                case data_girls_textures._spriteType.body:
                    selection.BodyIndex = index;
                    break;
                case data_girls_textures._spriteType.hair:
                    selection.HairIndex = index;
                    break;
                case data_girls_textures._spriteType.face:
                    selection.FaceIndex = index;
                    break;
                case data_girls_textures._spriteType.acc:
                    selection.AccessoryIndex = index;
                    break;
            }
        }

        private static StaffPortraitAsset GetSelectedAsset(List<StaffPortraitAsset> assets, int index)
        {
            return assets != null && index >= 0 && index < assets.Count ? assets[index] : null;
        }

        private static int WrapIndex(int value, int count)
        {
            if (count <= 0)
            {
                return 0;
            }
            int wrapped = value % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }

        private static string Text(string key, string fallback)
        {
            return ModLocalization.Get(key, fallback);
        }

    }
}
