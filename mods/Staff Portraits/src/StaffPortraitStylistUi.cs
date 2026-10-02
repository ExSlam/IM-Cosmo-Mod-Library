using System;
using System.Collections.Generic;
using StaffPortraits.EmbeddedIMUiFramework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace StaffPortraits
{
    internal static partial class StaffPortraitStylist
    {
        private const string EditorName = "StaffPortraits_Editor";
        private const string PickerName = "StaffPicker";
        private const string PickerItemsName = "StaffItems";
        private const string PickerEntryName = "StaffEntry";
        private const string PortraitName = "Portrait";
        private const string CaptionName = "StaffName";
        private const string JobName = "StaffJob";
        private const string PickerHeadingKey = "ui.staff.select";
        private const string PickerHeadingFallback = "Select a staff member";
        private const string PreviousPartKey = "ui.selector.previous";
        private const string PreviousPartFallback = "Previous {0}";
        private const string NextPartKey = "ui.selector.next";
        private const string NextPartFallback = "Next {0}";
        private const string MissingScrollMessage = "[StaffPortraits] Native staff picker scroll view is unavailable.";
        private const string MissingArrowMessage = "[StaffPortraits] Native chart arrow is unavailable.";
        private const float PanelInset = 20f;
        private const float EditorTop = 80f;
        private const float EditorWidth = StaffPortraitsConstants.PopupWidth - PanelInset * 2f;
        private const float EditorHeight = 530f;
        private const float PickerWidth = 230f;
        private const float ColumnGap = 18f;
        private const float PreviewLeft = PickerWidth + ColumnGap;
        private const float PreviewWidth = 250f;
        private const float SelectorsLeft = PreviewLeft + PreviewWidth + ColumnGap;
        private const float SelectorsWidth = EditorWidth - SelectorsLeft;
        private const float HeaderNameHeight = 32f;
        private const float HeaderRoleTop = 36f;
        private const float HeaderRoleHeight = 26f;
        private const float HeaderFontSize = 25f;
        private const float BodyFontSize = 17f;
        private const float CaptionFontSize = 14f;
        private const float CardsTop = 72f;
        private const float CardsHeight = 392f;
        private const float CardInset = 12f;
        private const float RowHeight = 64f;
        private const float RowGap = 12f;
        private const float SelectorLabelHeight = 24f;
        private const float SelectorValueTop = 24f;
        private const float SelectorValueHeight = 40f;
        private const float ArrowSize = 36f;
        private const float ArrowTextGap = 8f;
        private const float ArrowTopInset = 2f;
        private const float ActionsTop = 484f;
        private const float ActionHeight = 40f;
        private const float ActionGap = 18f;
        private const float PickerHeadingHeight = CardsTop - ColumnGap;
        private const float PickerTop = CardsTop;
        private const float PickerHeight = CardsHeight;
        private const float PickerScrollGutter = 18f;
        private const int PickerColumns = 2;
        private const int PickerPadding = 8;
        private const float PickerCellWidth = 92f;
        private const float PickerCellHeight = JobTop + JobHeight + PickerPadding;
        private const float PickerSpacing = 12f;
        private const float MiniPortraitSize = 72f;
        private const float MiniPortraitTop = 8f;
        private const float CaptionTop = 84f;
        private const float CaptionHeight = 44f;
        private const float CaptionJobGap = 4f;
        private const float JobTop = CaptionTop + CaptionHeight + CaptionJobGap;
        private const float JobHeight = 40f;
        private const float JobFontSize = 13f;
        private const float ScrollExtentTolerance = 0.5f;
        private const float ScrollTop = 1f;
        private const byte SelectionAlpha = 64;
        private static readonly Color32 SelectedStaffColor = new Color32(110, 205, 149, SelectionAlpha);

        private static IMUiScrollViewHandle staffPicker;
        private static readonly List<Image> PickerBackgrounds = new List<Image>();
        private static readonly List<Button> SelectorButtons = new List<Button>();

        private static void ResetPickerUi()
        {
            staffPicker = null;
            PickerBackgrounds.Clear();
            SelectorButtons.Clear();
        }

        private static void BuildEditorUi()
        {
            // This editor fits on one sheet. Only the roster scrolls, so editor text cannot
            // inherit an oversized scrolling row or extend into adjacent columns.
            GameObject oldScroll = scaffold.ScrollRect.gameObject;
            oldScroll.SetActive(false);
            UnityEngine.Object.Destroy(oldScroll);
            scaffold.ScrollRect = null;
            GameObject editor = StaffPortraitUi.Object(EditorName, scaffold.PanelRect);
            StaffPortraitUi.Place(editor.GetComponent<RectTransform>(), PanelInset, EditorTop, EditorWidth, EditorHeight);
            scaffold.ContentRoot = editor.transform;
            scaffold.Root.AddComponent<StaffStylistLifetime>();

            if (scaffold.CloseButton != null)
            {
                scaffold.CloseButton.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(scaffold.CloseButton.gameObject);
            }
            scaffold.CloseButton = StaffPortraitUi.Button(editor.transform, StaffPortraitsConstants.CloseKey,
                Text(StaffPortraitsConstants.CloseKey, StaffPortraitsConstants.CloseFallback),
                PreviewLeft, ActionsTop, PreviewWidth, ActionHeight, delegate { scaffold.Close(null); });

            CreateStaffPicker(editor.transform);
            staffNameText = StaffPortraitUi.Label(editor.transform, CaptionName, string.Empty,
                PreviewLeft, 0f, EditorWidth - PreviewLeft, HeaderNameHeight, HeaderFontSize);
            staffRoleText = StaffPortraitUi.Label(editor.transform, JobName, string.Empty,
                PreviewLeft, HeaderRoleTop, EditorWidth - PreviewLeft, HeaderRoleHeight, BodyFontSize);
            CreatePreviewColumn(editor.transform);
            CreateSelectorColumn(editor.transform);

            float actionWidth = (SelectorsWidth - ActionGap) / 2f;
            randomizeButton = StaffPortraitUi.Button(editor.transform, StaffPortraitsConstants.RandomizeKey,
                Text(StaffPortraitsConstants.RandomizeKey, StaffPortraitsConstants.RandomizeFallback),
                SelectorsLeft, ActionsTop, actionWidth, ActionHeight, RandomizeSelection);
            applyButton = StaffPortraitUi.Button(editor.transform, StaffPortraitsConstants.ApplyKey,
                Text(StaffPortraitsConstants.ApplyKey, StaffPortraitsConstants.ApplyFallback),
                SelectorsLeft + actionWidth + ActionGap, ActionsTop, actionWidth, ActionHeight, ApplySelection);
        }

        private static void CreateStaffPicker(Transform parent)
        {
            StaffPortraitUi.Label(parent, PickerHeadingKey, Text(PickerHeadingKey, PickerHeadingFallback),
                0f, 0f, PickerWidth, PickerHeadingHeight, BodyFontSize);
            if (!IMUiComposer.TryCreateScrollView(parent, new IMUiScrollViewOptions
            {
                ObjectName = PickerName,
                OffsetMin = Vector2.zero, OffsetMax = Vector2.zero,
                VanillaViewportRightInset = PickerScrollGutter,
                VanillaIndicatorRightCenterInset = PickerScrollGutter / 2f,
                VanillaIndicatorHideFill = true,
                Theme = theme
            }, out staffPicker))
                throw new InvalidOperationException(MissingScrollMessage);
            StaffPortraitUi.Place(staffPicker.Root.GetComponent<RectTransform>(),
                0f, PickerTop, PickerWidth, PickerHeight);

            // Replace the framework's vertical-list content, not just its LayoutGroup:
            // Unity defers component destruction and would briefly run both layouts.
            GameObject oldContent = staffPicker.Content.gameObject;
            oldContent.SetActive(false);
            GameObject content = StaffPortraitUi.Object(PickerItemsName, staffPicker.Viewport);
            RectTransform rect = content.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = rect.sizeDelta = Vector2.zero;
            staffPicker.Content = content.transform;
            staffPicker.ScrollRect.content = rect;
            UnityEngine.Object.Destroy(oldContent);
            GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(PickerCellWidth, PickerCellHeight);
            grid.spacing = new Vector2(PickerSpacing, PickerSpacing);
            grid.padding = new RectOffset(PickerPadding, PickerPadding, PickerPadding, PickerPadding);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = PickerColumns;
            grid.childAlignment = TextAnchor.UpperCenter;
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void RebuildStaffPicker()
        {
            if (staffPicker == null) return;
            foreach (Transform child in staffPicker.Content)
            {
                child.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
            PickerBackgrounds.Clear();
            for (int index = 0; index < PickerStaff.Count; index++)
            {
                int staffIndex = index;
                staff._staff staffer = PickerStaff[index];
                GameObject item = IMUiPrimitives.CreateCard(staffPicker.Content, PickerEntryName,
                    new Vector2(PickerCellWidth, PickerCellHeight), theme);
                Image background = item.GetComponent<Image>();
                background.raycastTarget = true;
                PickerBackgrounds.Add(background);

                GameObject portraitObject = StaffPortraitUi.Object(PortraitName, item.transform);
                Image portrait = portraitObject.AddComponent<Image>();
                portrait.preserveAspect = true;
                portrait.raycastTarget = false;
                StaffPortraitUi.Place(portrait.rectTransform, (PickerCellWidth - MiniPortraitSize) / 2f,
                    MiniPortraitTop, MiniPortraitSize, MiniPortraitSize);
                StaffPortraitUi.Label(item.transform, CaptionName, staffer.GetName(true, false),
                    0f, CaptionTop, PickerCellWidth, CaptionHeight, CaptionFontSize);
                StaffPortraitUi.Label(item.transform, JobName, GetStaffJobTitle(staffer),
                    0f, JobTop, PickerCellWidth, JobHeight, JobFontSize);
                Button button = item.AddComponent<Button>();
                button.targetGraphic = background;
                button.onClick.AddListener(delegate { SelectStaff(staffIndex); });
                item.AddComponent<StaffPortraitPickerEntry>().Bind(staffer, portrait, staffPicker.ScrollRect.viewport);
            }

            // Resolve the grid's full height before resetting the native two-way Slider
            // binding. A movable handle must not suggest hidden entries when the list fits.
            IMUiKit.RebuildLayout(staffPicker.Content);
            Canvas.ForceUpdateCanvases();
            ScrollRect scroll = staffPicker.ScrollRect;
            bool canScroll = scroll.content.rect.height > scroll.viewport.rect.height + ScrollExtentTolerance;
            scroll.vertical = canScroll;
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = ScrollTop;
            Slider indicator = staffPicker.ScrollIndicator == null
                ? null : staffPicker.ScrollIndicator.GetComponent<Slider>();
            if (indicator != null)
            {
                indicator.interactable = canScroll;
                indicator.SetValueWithoutNotify(ScrollTop);
            }
        }

        private static string GetStaffJobTitle(staff._staff staffer)
        {
            // Use the game's translated job title, including other mods' staff-type patches.
            string title = staffer.GetJobTitle();
            return string.IsNullOrEmpty(title)
                ? Text(StaffPortraitsConstants.JobUnavailableKey, StaffPortraitsConstants.JobUnavailableFallback)
                : title;
        }

        private static void SelectStaff(int index)
        {
            if (index < 0 || index >= PickerStaff.Count) return;
            selectedStaffIndex = index;
            LoadCurrentStaffSelection();
            Render();
        }

        private static void RefreshPickerSelection()
        {
            for (int index = 0; index < PickerBackgrounds.Count; index++)
                if (PickerBackgrounds[index] != null)
                    PickerBackgrounds[index].color = index == selectedStaffIndex ? SelectedStaffColor : mainScript.white32;
        }

        private static void CreatePreviewColumn(Transform parent)
        {
            const string previewName = "PortraitPreview";
            const string layerName = "PreviewLayer";
            const string stateName = "PreviewState";
            GameObject card = IMUiPrimitives.CreateCard(parent, previewName, new Vector2(PreviewWidth, CardsHeight), theme);
            StaffPortraitUi.Place(card.GetComponent<RectTransform>(), PreviewLeft, CardsTop, PreviewWidth, CardsHeight);
            for (int index = 0; index < StaffPortraitsConstants.PreviewLayerCount; index++)
            {
                Image layer = StaffPortraitUi.Object(layerName, card.transform).AddComponent<Image>();
                StaffPortraitUi.Stretch(layer.rectTransform, CardInset);
                layer.preserveAspect = true;
                layer.raycastTarget = false;
                layer.color = Color.white;
                PreviewLayers.Add(layer);
            }
            // The state message belongs to this column, with the same bounds as its portrait.
            stateText = StaffPortraitUi.Label(card.transform, stateName, string.Empty,
                CardInset, CardInset, PreviewWidth - CardInset * 2f, CardsHeight - CardInset * 2f, BodyFontSize);
            stateText.gameObject.SetActive(false);
        }

        private static void CreateSelectorColumn(Transform parent)
        {
            const string selectorsName = "PortraitSelectors";
            GameObject card = IMUiPrimitives.CreateCard(parent, selectorsName,
                new Vector2(SelectorsWidth, CardsHeight), theme);
            StaffPortraitUi.Place(card.GetComponent<RectTransform>(), SelectorsLeft, CardsTop, SelectorsWidth, CardsHeight);
            float top = CardInset;
            packValueText = CreateSelectorRow(card.transform, StaffPortraitsConstants.PackLabelKey,
                Text(StaffPortraitsConstants.PackLabelKey, StaffPortraitsConstants.PackLabelFallback), top,
                delegate { ChangePack(StaffPortraitsConstants.PreviousSelectionOffset); },
                delegate { ChangePack(StaffPortraitsConstants.NextSelectionOffset); });
            data_girls_textures._spriteType[] types = { data_girls_textures._spriteType.body,
                data_girls_textures._spriteType.hair, data_girls_textures._spriteType.face, data_girls_textures._spriteType.acc };
            string[] keys = { StaffPortraitsConstants.BodyLabelKey, StaffPortraitsConstants.HairLabelKey,
                StaffPortraitsConstants.FaceLabelKey, StaffPortraitsConstants.AccessoryLabelKey };
            string[] fallbacks = { StaffPortraitsConstants.BodyLabelFallback, StaffPortraitsConstants.HairLabelFallback,
                StaffPortraitsConstants.FaceLabelFallback, StaffPortraitsConstants.AccessoryLabelFallback };
            for (int index = 0; index < types.Length; index++)
            {
                data_girls_textures._spriteType type = types[index];
                top += RowHeight + RowGap;
                SelectorValueTexts[type] = CreateSelectorRow(card.transform, keys[index], Text(keys[index], fallbacks[index]), top,
                    delegate { ChangePart(type, StaffPortraitsConstants.PreviousSelectionOffset); },
                    delegate { ChangePart(type, StaffPortraitsConstants.NextSelectionOffset); });
            }
        }

        private static TextMeshProUGUI CreateSelectorRow(Transform parent, string name, string label, float top,
            UnityAction previous, UnityAction next)
        {
            float width = SelectorsWidth - CardInset * 2f;
            GameObject row = StaffPortraitUi.Object(name, parent);
            StaffPortraitUi.Place(row.GetComponent<RectTransform>(), CardInset, top, width, RowHeight);
            StaffPortraitUi.Label(row.transform, name, label, 0f, 0f, width, SelectorLabelHeight,
                BodyFontSize, TextAlignmentOptions.Left);
            float valueLeft = ArrowSize + ArrowTextGap;
            TextMeshProUGUI value = StaffPortraitUi.Label(row.transform, name, string.Empty, valueLeft,
                SelectorValueTop, width - valueLeft * 2f, SelectorValueHeight, BodyFontSize);
            CreateSelectorArrow(row.transform, true, 0f, previous,
                string.Format(Text(PreviousPartKey, PreviousPartFallback), label));
            CreateSelectorArrow(row.transform, false, width - ArrowSize, next,
                string.Format(Text(NextPartKey, NextPartFallback), label));
            return value;
        }

        private static void CreateSelectorArrow(Transform parent, bool previous, float left,
            UnityAction onClick, string tooltip)
        {
            IMUiElementHandle handle;
            // Do not pass empty text: that removes the chart's private-use arrow glyph.
            // Do not apply the body font or an interactive theme over its native icon font.
            if (!(previous ? IMUiPresets.PreviousMonthButton() : IMUiPresets.NextMonthButton())
                .Parent(parent).OnClick(onClick).Build(out handle) || handle == null || handle.Root == null)
                throw new InvalidOperationException(MissingArrowMessage);
            Button button = handle.Get<Button>();
            if (button == null) throw new InvalidOperationException(MissingArrowMessage);
            StaffPortraitUi.Place(button.GetComponent<RectTransform>(), left,
                SelectorValueTop + ArrowTopInset, ArrowSize, ArrowSize);
            IMUiKit.SetTooltip(button.gameObject, tooltip);
            SelectorButtons.Add(button);
        }

        internal static void ReleasePreviewResources()
        {
            foreach (Image layer in PreviewLayers)
                if (layer != null) { layer.sprite = null; layer.enabled = false; }
            ClearPreviewResources();
        }
    }

    internal sealed class StaffStylistLifetime : MonoBehaviour
    {
        private void OnDisable() { StaffPortraitStylist.ReleasePreviewResources(); }
    }
}
