using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using CheatsMod.EmbeddedIMUiFramework;
using HarmonyLib;
using ModLocalizationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CheatsMod
{
    // Shared presentation for the selectors. Game logic remains in each cheat controller.
    internal static class CheatUi
    {
        internal const float Center = 0.5f;
        internal const float RowHeight = 40f;
        internal const float RowSpacing = 6f;
        internal const float TitleInset = 14f;
        internal const float TitleHeight = 48f;
        internal const int TitleFontSize = 28;
        internal const int BodyFontSize = 17;
        internal const int SmallFontSize = 15;
        internal const float FooterInset = 50f;
        internal const float PickerCellWidth = 225f;
        internal const float PickerCellHeight = 123f;
        internal const int PickerColumns = 2;
        internal const int PickerPadding = 10;
        internal const float PickerSpacing = 10f;
        internal const float ScrollGutter = 30f;
        internal const string CloseKey = "ui.idol_cheat.close";
        internal const string ApplyKey = "ui.editor.apply";
        internal const string CancelKey = "ui.editor.cancel";
        internal const string MinKey = "ui.editor.min";
        internal const string MaxKey = "ui.editor.max";
        internal const string EditKey = "ui.editor.edit";
        internal const string IncreaseKey = "ui.editor.increase";
        internal const string DecreaseKey = "ui.editor.decrease";
        internal const string NumberFormat = "0.##";
        internal const string WholeNumberFormat = "0";
        internal const string PopupStartMethodName = "Start";
        private const string PersistentCountMethod = "GetPersistentEventCount";
        private const string PersistentNameMethod = "GetPersistentMethodName";
        private static readonly MethodInfo ReadPersistentCount = AccessTools.Method(typeof(UnityEventBase), PersistentCountMethod);
        private static readonly MethodInfo ReadPersistentName = AccessTools.Method(typeof(UnityEventBase), PersistentNameMethod);
        private const string SettingsButtonsPath = "ScrollRect/Container";
        private static readonly string[] NativeButtonNames = { "Settings", "Main Menu" };
        private const float VisibleAlpha = 1f;
        private static GameObject nativeButtonTemplate;
        internal const string SearchKey = "ui.editor.search";
        private const string SearchPlaceholderName = "Placeholder";
        private const string InputSheetName = "InputSheet";
        private const string InputTitleName = "InputTitle";
        private const float InputSheetMargin = 24f;
        private static readonly Color32 InputBackdropColor = new Color32(0, 0, 0, 160);
        private static readonly Color32 InputSheetColor = new Color32(248, 246, 250, 255);
        private static readonly Color32 SearchPlaceholderColor = new Color32(80, 80, 80, 255);
        private const string PanelName = "Panel";
        private const string GridContentName = "Items";
        private const string HighlightName = "SelectedIdol";
        private const string UiUnavailableMessage = "[CheatsMod] Required native UI template is unavailable.";
        private const byte HighlightAlpha = 64;
        private const float MinFontSize = 12f;
        private const float ButtonLabelInset = 4f;
        private static readonly Color32 PanelColor = new Color32(248, 246, 250, 255);

        internal static CultureInfo Culture
        {
            get
            {
                // Follow the selected game language, independently of the Windows locale.
                const string English = "en-US";
                string language = ModLocalization.GetEffectiveLanguageCode().ToLowerInvariant();
                if (string.IsNullOrEmpty(language)) language = English;
                switch (language)
                {
                    case "cn": case "zh": case "zh-hans": language = "zh-CN"; break;
                    case "jp": case "ja": language = "ja-JP"; break;
                    case "kr": case "ko": language = "ko-KR"; break;
                    case "ptbr": language = "pt-BR"; break;
                }
                try { return CultureInfo.GetCultureInfo(language); }
                catch (CultureNotFoundException) { return CultureInfo.GetCultureInfo(English); }
            }
        }

        internal static string Text(string key) { return ModLocalization.Get(key, string.Empty); }

        internal static void Initialize(PopupManager manager)
        {
            IMUiKit.Initialize(manager);
            nativeButtonTemplate = null;
        }

        internal static GameObject CreateShell(PopupManager manager, string name, Vector2 size, out Transform panel)
        {
            Initialize(manager);
            Transform parent = null;
            foreach (PopupManager._popup entry in manager.popups)
            {
                if (entry != null && entry.obj != null && entry.obj.transform.parent != null)
                {
                    parent = entry.obj.transform.parent;
                    break;
                }
            }
            if (parent == null) throw new InvalidOperationException(UiUnavailableMessage);
            GameObject root = Object(name, parent);
            Stretch(root.GetComponent<RectTransform>());
            root.SetActive(false);
            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = true;
            group.interactable = true;
            GameObject sheet = Object(PanelName, root.transform);
            RectTransform rect = sheet.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(Center, Center);
            rect.sizeDelta = size;
            Image surface = sheet.AddComponent<Image>();
            IMUiPrimitives.TryCopyVanillaPanelVisual(surface);
            surface.color = PanelColor;
            surface.raycastTarget = true;
            sheet.AddComponent<CheatUiScaleToCanvas>();
            Popup popup = root.AddComponent<Popup>();
            popup.ShowAnimation = popup.HideAnimation = popup.Increase_Popup_Counter = true;
            popup.HideFast = false;
            popup.OnOpen = new UnityEvent();
            panel = sheet.transform;
            return root;
        }

        internal static GameObject CreateInputSheet(Transform owner, string name, string title,
            Vector2 size, out Transform panel)
        {
            GameObject overlay = Object(name, owner);
            Stretch(overlay.GetComponent<RectTransform>());
            overlay.AddComponent<Image>().color = InputBackdropColor;
            GameObject sheet = Object(InputSheetName, overlay.transform);
            RectTransform rect = sheet.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(Center, Center);
            rect.sizeDelta = size;
            // The chart panel sprite has translucent pixels even at color alpha 255.
            // An untextured UI Image makes the entire input sheet opaque.
            Image surface = sheet.AddComponent<Image>();
            surface.sprite = null;
            surface.type = Image.Type.Simple;
            surface.color = InputSheetColor;
            surface.raycastTarget = true;
            panel = sheet.transform;
            TextMeshProUGUI heading = Label(panel, InputTitleName, title, TitleFontSize,
                TextAlignmentOptions.Center, mainScript.black32);
            Place(heading.rectTransform, InputSheetMargin, TitleInset,
                size.x - InputSheetMargin * 2f, TitleHeight);
            return overlay;
        }

        internal static GameObject SearchInput(Transform parent, string name, UnityAction<string> onChanged)
        {
            GameObject root;
            VanillaControlOptions options = new VanillaControlOptions
            {
                Type = VanillaControlType.InputField,
                ObjectName = name,
                Active = false,
                ResourcePath = VanillaUiPrefabCatalog.InputField.Input_Field_Standard_Middle,
                ConfigureTheme = delegate(VanillaUiThemeSettings theme)
                {
                    theme.inputFieldColor = mainScript.black32;
                    theme.inputFieldFontSize = BodyFontSize;
                    theme.inputFieldFont = VanillaUiFonts.GetGameSelectedTmpFont();
                }
            };
            if (!VanillaUiControlFactory.TryCreate(parent, options, out root))
                throw new InvalidOperationException(UiUnavailableMessage);
            TMP_InputField input = IMUiCompat.GetComponentInChildren<TMP_InputField>(root);
            if (input == null) throw new InvalidOperationException(UiUnavailableMessage);
            input.contentType = TMP_InputField.ContentType.Standard;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 0;
            input.onValueChanged = new TMP_InputField.OnChangeEvent();
            input.onEndEdit = new TMP_InputField.SubmitEvent();
            input.text = string.Empty;
            string placeholder = Text(SearchKey);
            // MUIP's animated Placeholder child is not necessarily assigned to input.placeholder.
            // Set both paths before activation so its shipped "Placeholder" copy is never shown.
            foreach (TextMeshProUGUI label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (label == input.textComponent) continue;
                if (label.name != SearchPlaceholderName && label != input.placeholder) continue;
                label.text = placeholder;
                label.color = SearchPlaceholderColor;
                Lang_Button language = label.GetComponent<Lang_Button>();
                if (language != null) language.Constant = placeholder;
            }
            input.onValueChanged.AddListener(onChanged);
            root.SetActive(true);
            return root;
        }

        internal static bool Register(int id, GameObject root)
        {
            return IMUiKit.TryRegisterPopup((PopupManager._type)id, root, true, true);
        }

        internal static GameObject Object(string name, Transform parent)
        {
            GameObject result = new GameObject(name, typeof(RectTransform));
            result.transform.SetParent(parent, false);
            result.layer = parent.gameObject.layer;
            return result;
        }

        internal static void Place(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        internal static TextMeshProUGUI Label(Transform parent, string name, string text, int size,
            TextAlignmentOptions alignment, Color32 color)
        {
            TextMeshProUGUI label = IMUiPrimitives.CreateText(parent, new IMUiTextOptions
            {
                ObjectName = name, Text = text, FontSize = size, Alignment = alignment
            });
            label.color = color;
            label.enableAutoSizing = true;
            label.fontSizeMin = MinFontSize;
            label.fontSizeMax = size;
            return label;
        }

        internal static TextMeshProUGUI LabelAt(Transform parent, string key, float left, float top,
            float width, float height, int size = BodyFontSize)
        {
            TextMeshProUGUI label = Label(parent, key, Text(key), size, TextAlignmentOptions.MidlineLeft, mainScript.black32);
            Place(label.rectTransform, left, top, width, height);
            return label;
        }

        internal static Button Button(Transform parent, string name, string label, float width,
            float height, UnityAction callback)
        {
            // Mod Buttons clones the Settings tab's scene button. Keep its native sliced
            // gradient, shadow and ColorBlock instead of tinting a MUIP resource prefab.
            GameObject template = GetNativeButtonTemplate();
            if (template == null) throw new InvalidOperationException(UiUnavailableMessage);
            GameObject clone = UnityEngine.Object.Instantiate(template, parent, false);
            clone.name = name;
            clone.SetActive(true);
            IMUiKit.ApplyLayerRecursively(clone, parent.gameObject.layer);
            IMUiKit.RebindAllButtons(clone, callback);
            IMUiKit.ActivateButtonDefaults(clone);
            foreach (CanvasGroup group in clone.GetComponentsInChildren<CanvasGroup>(true))
            {
                group.alpha = VisibleAlpha;
                group.interactable = group.blocksRaycasts = true;
            }
            foreach (ButtonDefault native in clone.GetComponentsInChildren<ButtonDefault>(true))
            {
                if (native.OnHover == null) native.OnHover = new ButtonDefault.MyEventType();
                native.DefaultTooltip = string.Empty;
                native.SetTooltip(string.Empty);
            }
            foreach (Graphic graphic in clone.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = graphic.gameObject == clone;
            RectTransform rect = clone.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(width, height);
            Button button = clone.GetComponent<Button>();
            if (button == null) throw new InvalidOperationException(UiUnavailableMessage);
            SetButtonText(button, label);
            return button;
        }

        private static GameObject GetNativeButtonTemplate()
        {
            if (nativeButtonTemplate != null) return nativeButtonTemplate;
            mainScript main = Camera.main == null ? null : Camera.main.GetComponent<mainScript>();
            Tabs_Manager tabs = main == null || main.Data == null ? null : main.Data.GetComponent<Tabs_Manager>();
            Tabs_Manager._tab settings = tabs == null ? null : tabs.GetTab(Tabs_Manager._tab._type.settings);
            Transform settingsRoot = settings == null || settings.Tab == null ? null : settings.Tab.transform;
            if (settingsRoot == null) return null;
            Transform container = settingsRoot.Find(SettingsButtonsPath);
            nativeButtonTemplate = FindNativeButton(container != null ? container : settingsRoot);
            if (nativeButtonTemplate == null && container != null)
                nativeButtonTemplate = FindNativeButton(settingsRoot);
            return nativeButtonTemplate;
        }

        private static GameObject FindNativeButton(Transform root)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            foreach (string templateName in NativeButtonNames)
            {
                foreach (Button candidate in buttons)
                {
                    if (candidate.GetComponent<ButtonDefault>() == null) continue;
                    if (candidate.name.IndexOf(templateName, StringComparison.OrdinalIgnoreCase) >= 0
                        && candidate.GetComponentsInChildren<TextMeshProUGUI>(true).Length > 0)
                        return candidate.gameObject;
                }
            }
            return null;
        }

        internal static void SetButtonText(Button button, string value)
        {
            // Match Mod Buttons' language-binding reset so Lang_Button.Start cannot restore
            // the Settings caption after the localized cheat label has been assigned.
            foreach (Lang_Button binding in button.GetComponentsInChildren<Lang_Button>(true))
            {
                binding.Tooltip = string.Empty;
                binding.Constant = binding.GetComponent<TextMeshProUGUI>() != null ? value : string.Empty;
            }
            VanillaUiFonts.ApplyGameFont(button.gameObject);
            foreach (TextMeshProUGUI label in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                label.gameObject.SetActive(true);
                label.text = value;
                label.color = mainScript.white32;
                label.alignment = TextAlignmentOptions.Center;
                label.enableWordWrapping = true;
                label.enableAutoSizing = true;
                label.fontSizeMin = MinFontSize;
                label.fontSizeMax = BodyFontSize;
                label.raycastTarget = false;
                Stretch(label.rectTransform);
                label.rectTransform.offsetMin = new Vector2(ButtonLabelInset, 0f);
                label.rectTransform.offsetMax = new Vector2(-ButtonLabelInset, 0f);
            }
        }

        internal static void SetButtonInteractable(Button button, bool enabled)
        {
            button.interactable = enabled;
            ButtonDefault native = button.GetComponent<ButtonDefault>();
            if (native != null) native.Activate(enabled);
        }

        internal static Button ButtonAt(Transform parent, string key, float left, float top,
            float width, UnityAction callback)
        {
            Button button = Button(parent, key, Text(key), width, RowHeight, callback);
            Place(button.GetComponent<RectTransform>(), left, top, width, RowHeight);
            return button;
        }

        internal static IMUiScrollViewHandle Scroll(Transform parent, string name, Vector2 min, Vector2 max)
        {
            IMUiScrollViewHandle handle;
            if (!IMUiComposer.TryCreateScrollView(parent, new IMUiScrollViewOptions
            {
                ObjectName = name, OffsetMin = min, OffsetMax = max,
                VanillaViewportRightInset = ScrollGutter, VanillaIndicatorHideFill = true,
                Theme = IMUiTheme.Vanilla()
            }, out handle)) throw new InvalidOperationException(UiUnavailableMessage);
            // Replace the list content before adding a grid: Unity defers Destroy until the end
            // of the frame, so removing only its LayoutGroup could conflict with the new grid.
            GameObject oldContent = handle.Content.gameObject;
            oldContent.SetActive(false);
            GameObject content = Object(GridContentName, handle.Viewport);
            RectTransform rect = content.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(Center, 1f);
            rect.anchoredPosition = rect.sizeDelta = Vector2.zero;
            handle.Content = content.transform;
            handle.ScrollRect.content = rect;
            UnityEngine.Object.Destroy(oldContent);
            return handle;
        }

        internal static IMUiScrollViewHandle Picker(Transform panel, string name, float left, float top,
            float width, float height, IList<data_girls.girls> girls, GameObject prefab,
            Action<data_girls.girls> select, Action<GameObject, data_girls.girls> decorate)
        {
            IMUiScrollViewHandle handle = Scroll(panel, name, Vector2.zero, Vector2.zero);
            Place(handle.Root.GetComponent<RectTransform>(), left, top, width, height);
            GridLayoutGroup grid = handle.Content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(PickerCellWidth, PickerCellHeight);
            grid.spacing = new Vector2(PickerSpacing, PickerSpacing);
            grid.padding = new RectOffset(PickerPadding, PickerPadding, PickerPadding, PickerPadding);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = PickerColumns;
            grid.childAlignment = TextAnchor.UpperCenter;
            int rows = Mathf.CeilToInt((float)girls.Count / PickerColumns);
            float contentHeight = PickerPadding * PickerColumns + rows * PickerCellHeight
                + Mathf.Max(0, rows - 1) * PickerSpacing;
            handle.ScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            foreach (data_girls.girls girl in girls)
            {
                GameObject item = UnityEngine.Object.Instantiate(prefab, handle.Content, false);
                IMUiKit.ApplyLayerRecursively(item, panel.gameObject.layer);
                GirlButtonSmall nativeCard = item.GetComponent<GirlButtonSmall>();
                nativeCard.DontDisableIfTraining = nativeCard.DontDisableIfHiatus = true;
                nativeCard.SetGirl(girl, false);
                IMUiKit.RebindAllButtons(item, delegate { select(girl); });
                IMUiKit.ActivateButtonDefaults(item);
                if (decorate != null) decorate(item, girl);
            }
            handle.ScrollRect.verticalNormalizedPosition = 1f;
            return handle;
        }

        internal static GameObject Highlight(GameObject card)
        {
            GameObject highlight = Object(HighlightName, card.transform);
            Stretch(highlight.GetComponent<RectTransform>());
            Image image = highlight.AddComponent<Image>();
            Color32 color = mainScript.green32;
            color.a = HighlightAlpha;
            image.color = color;
            image.raycastTarget = false;
            highlight.SetActive(false);
            return highlight;
        }

        internal enum NumericAction { Decrease, Increase, Edit }

        internal static Button NumericButton(Transform parent, NumericAction action, UnityAction callback)
        {
            // Find controls by their serialized callbacks, avoiding translated object names and glyph guesses.
            string method = action == NumericAction.Increase ? nameof(Salary_Line.OnIncrease)
                : action == NumericAction.Decrease ? nameof(Salary_Line.OnDecrease) : nameof(Salary_Line.OnEdit);
            string tooltip = Text(action == NumericAction.Increase ? IncreaseKey
                : action == NumericAction.Decrease ? DecreaseKey : EditKey);
            Salaries_Popup salaries = PopupManager.GetObject(PopupManager._type.producer_salaries).GetComponent<Salaries_Popup>();
            foreach (Button candidate in salaries.prefab_line.GetComponentsInChildren<Button>(true))
            {
                if (HasPersistentCallback(candidate.onClick, method))
                {
                    GameObject clone = UnityEngine.Object.Instantiate(candidate.gameObject, parent, false);
                    IMUiKit.ApplyLayerRecursively(clone, parent.gameObject.layer);
                    IMUiKit.ClearLocalizationComponents(clone);
                    IMUiKit.RebindAllButtons(clone, callback);
                    IMUiKit.ActivateButtonDefaults(clone);
                    ButtonDefault native = clone.GetComponent<ButtonDefault>();
                    if (native != null)
                    {
                        native.DefaultTooltip = tooltip;
                        native.SetTooltip(tooltip);
                        if (native.OnHover == null) native.OnHover = new ButtonDefault.MyEventType();
                    }
                    clone.SetActive(true);
                    return clone.GetComponent<Button>();
                }
            }
            throw new InvalidOperationException(UiUnavailableMessage);
        }

        private static bool HasPersistentCallback(UnityEventBase click, string method)
        {
            // The game's reduced compile references omit these public UnityEvent APIs.
            // Resolve them from the running Unity version, as the embedded framework does
            // for other Unity APIs missing from the reference assemblies.
            if (ReadPersistentCount == null || ReadPersistentName == null) return false;
            int count = (int)ReadPersistentCount.Invoke(click, null);
            for (int index = 0; index < count; index++)
                if (string.Equals((string)ReadPersistentName.Invoke(click, new object[] { index }),
                    method, StringComparison.Ordinal)) return true;
            return false;
        }

        internal static InputField NumericInput(Transform parent)
        {
            Salaries_Popup salaries = PopupManager.GetObject(PopupManager._type.producer_salaries).GetComponent<Salaries_Popup>();
            Salary_Manual manual = salaries.Manual_Popup.GetComponent<Salary_Manual>();
            GameObject clone = UnityEngine.Object.Instantiate(manual.Field, parent, false);
            IMUiKit.ApplyLayerRecursively(clone, parent.gameObject.layer);
            IMUiKit.ClearLocalizationComponents(clone);
            VanillaUiFonts.ApplyGameFont(clone);
            InputField input = clone.GetComponent<InputField>();
            input.onValueChanged = new InputField.OnChangeEvent();
            input.onEndEdit = new InputField.SubmitEvent();
            input.onValidateInput = null;
            input.characterLimit = CheatNumericEditor.InputCharacterLimit;
            input.contentType = InputField.ContentType.Standard;
            input.lineType = InputField.LineType.SingleLine;
            input.interactable = true;
            clone.SetActive(true);
            return input;
        }
    }

    internal sealed class CheatUiScaleToCanvas : MonoBehaviour
    {
        private const float AvailableCanvasRatio = 0.94f;
        private void OnEnable() { Resize(); }
        private void LateUpdate() { Resize(); }
        private void Resize()
        {
            RectTransform panel = transform as RectTransform;
            RectTransform canvas = transform.parent as RectTransform;
            if (panel == null || canvas == null || canvas.rect.width <= 0f || canvas.rect.height <= 0f) return;
            float scale = Mathf.Min(1f, Mathf.Min(canvas.rect.width * AvailableCanvasRatio / panel.rect.width,
                canvas.rect.height * AvailableCanvasRatio / panel.rect.height));
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    [HarmonyPatch(typeof(PopupManager), CheatUi.PopupStartMethodName)]
    internal static class CheatUiBootstrap
    {
        private static void Postfix(PopupManager __instance) { CheatUi.Initialize(__instance); }
    }
}
