using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HarmonyModBrowser
{
    internal sealed class SearchFieldParts
    {
        internal TMP_InputField Input;
        internal TextMeshProUGUI Placeholder;
        internal TextMeshProUGUI Text;
    }

    internal sealed class ScrollAreaParts
    {
        internal ScrollRect ScrollRect;
        internal RectTransform Viewport;
        internal RectTransform Content;
        internal GridLayoutGroup Grid;
    }

    internal static class RuntimeUi
    {
        internal static readonly Color PanelColor = new Color32(242, 242, 242, 255);
        internal static readonly Color FieldColor = new Color32(255, 255, 255, 255);
        internal static readonly Color TextColor = new Color32(70, 70, 70, 255);
        internal static readonly Color MutedTextColor = new Color32(110, 110, 110, 255);
        internal static readonly Color PlaceholderColor = new Color32(135, 135, 135, 210);
        internal static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.76f);
        internal static readonly Color LocalBadgeColor = new Color32(226, 244, 232, 255);
        internal static readonly Color WorkshopBadgeColor = new Color32(226, 231, 247, 255);

        internal static TextMeshProUGUI FindTextTemplate(Component root)
        {
            if (root == null)
            {
                return null;
            }
            TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].font != null)
                {
                    return texts[i];
                }
            }
            return texts.Length > 0 ? texts[0] : null;
        }

        internal static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return obj;
        }

        internal static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string text,
            float fontSize,
            TextAlignmentOptions alignment,
            TextMeshProUGUI template,
            Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            TextMeshProUGUI label = obj.GetComponent<TextMeshProUGUI>();
            CopyTextStyle(template, label);
            label.text = text ?? string.Empty;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            return label;
        }

        private const string SettingsButtonsPath = "ScrollRect/Container";
        private const string SettingsButtonName = "Settings";
        private const string MainMenuButtonName = "Main Menu";
        private const string BrowserBackButtonPath = "Cancel";
        private const float ScrollSliderWidth = 20f;
        private const float ScrollSliderGap = 4f;
        private const float ScrollSliderEndInset = 12f;
        private const string NativeSpinnerName = "Spinner";
        private const string NativeControlMissing = "The native mod-browser button or scroll template is unavailable.";
        private const float ButtonFontSize = 17f;
        private const float ButtonMinFontSize = 12f;
        private const float ButtonLabelInset = 8f;
        private static GameObject nativeButtonTemplate;
        private static ScrollRect nativeScrollTemplate;
        private static Slider nativeSliderTemplate;

        internal static void InitializeTemplates(Mods_Popup popup, ScrollRect list)
        {
            nativeScrollTemplate = list;
            nativeSliderTemplate = FindScrollSlider(list);
            // Same Settings-tab scene control used by Cheats Mod and Mod Buttons.
            mainScript main = Camera.main == null ? null : Camera.main.GetComponent<mainScript>();
            Tabs_Manager tabs = main == null || main.Data == null ? null : main.Data.GetComponent<Tabs_Manager>();
            Tabs_Manager._tab settings = tabs == null ? null : tabs.GetTab(Tabs_Manager._tab._type.settings);
            Transform settingsRoot = settings == null || settings.Tab == null ? null : settings.Tab.transform;
            nativeButtonTemplate = null;
            if (settingsRoot != null)
            {
                Transform buttons = settingsRoot.Find(SettingsButtonsPath) ?? settingsRoot;
                foreach (Button candidate in buttons.GetComponentsInChildren<Button>(true))
                {
                    if (candidate.GetComponent<ButtonDefault>() != null &&
                        (candidate.name.IndexOf(SettingsButtonName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                         candidate.name.IndexOf(MainMenuButtonName, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        nativeButtonTemplate = candidate.gameObject;
                        break;
                    }
                }
            }
            // The main menu has no Settings tab; its Back button is the game's
            // blue menu control, with the same native gradient and shadow.
            if (nativeButtonTemplate == null)
            {
                Transform back = popup.transform.Find(BrowserBackButtonPath);
                if (back != null && back.GetComponent<ButtonDefault>() != null)
                    nativeButtonTemplate = back.gameObject;
            }
            if (nativeButtonTemplate == null && popup.prefab_mod_button != null)
            {
                Mod_Button card = popup.prefab_mod_button.GetComponent<Mod_Button>();
                if (card != null) nativeButtonTemplate = card.Button_Upload;
            }
            if (nativeButtonTemplate == null || nativeScrollTemplate == null)
                throw new InvalidOperationException(NativeControlMissing);
        }

        internal static Button CreateButton(
            string name, Transform parent, string label,
            TextMeshProUGUI template, UnityAction onClick)
        {
            if (nativeButtonTemplate == null) throw new InvalidOperationException(NativeControlMissing);
            GameObject obj = UnityEngine.Object.Instantiate(nativeButtonTemplate, parent, false);
            obj.name = name;
            obj.SetActive(false);
            foreach (Transform child in obj.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = parent.gameObject.layer;
            foreach (Button control in obj.GetComponentsInChildren<Button>(true))
            {
                control.onClick = new Button.ButtonClickedEvent();
                if (onClick != null) control.onClick.AddListener(onClick);
            }
            foreach (ButtonDefault native in obj.GetComponentsInChildren<ButtonDefault>(true))
            {
                native.OnHover = new ButtonDefault.MyEventType();
                native.DefaultTooltip = string.Empty;
                native.SetTooltip(string.Empty);
                native.Activate(true);
            }
            foreach (CanvasGroup group in obj.GetComponentsInChildren<CanvasGroup>(true))
            {
                group.alpha = 1f;
                group.interactable = group.blocksRaycasts = true;
            }
            foreach (Graphic graphic in obj.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = graphic.gameObject == obj;
            Button button = obj.GetComponent<Button>();
            if (button == null) throw new InvalidOperationException(NativeControlMissing);
            SetButtonLabel(button, label);
            SetButtonInteractable(button, true);
            obj.SetActive(true);
            return button;
        }

        internal static void SetButtonLabel(Button button, string label)
        {
            if (button == null) return;
            label = label ?? string.Empty;
            foreach (Lang_Button binding in button.GetComponentsInChildren<Lang_Button>(true))
            {
                binding.Tooltip = string.Empty;
                binding.Constant = binding.GetComponent<TextMeshProUGUI>() != null ||
                    binding.GetComponent<Text>() != null ? label : string.Empty;
            }
            foreach (TextMeshProUGUI text in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text.name == NativeSpinnerName) continue;
                text.text = label;
                text.color = mainScript.white32;
                text.enableWordWrapping = true;
                text.enableAutoSizing = true;
                text.fontSizeMin = ButtonMinFontSize;
                text.fontSizeMax = ButtonFontSize;
                text.alignment = TextAlignmentOptions.Center;
                text.raycastTarget = false;
                SetStretch(text.rectTransform, ButtonLabelInset, 0f, ButtonLabelInset, 0f);
                HmbGameFont.Apply(text);
            }
            foreach (Text text in button.GetComponentsInChildren<Text>(true))
            {
                text.text = label;
                text.color = mainScript.white32;
                text.raycastTarget = false;
            }
        }

        internal static void SetButtonInteractable(Button button, bool enabled)
        {
            if (button == null) return;
            button.interactable = enabled;
            ButtonDefault native = button.GetComponent<ButtonDefault>();
            if (native != null) native.Activate(enabled);
        }

        internal static SearchFieldParts CreateSearchField(
            string name,
            Transform parent,
            string placeholder,
            TextMeshProUGUI template)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            root.transform.SetParent(parent, false);
            Image background = root.GetComponent<Image>();
            background.color = FieldColor;
            background.raycastTarget = true;

            GameObject viewportObject = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            viewportObject.transform.SetParent(root.transform, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            SetStretch(viewport, 10f, 4f, 10f, 4f);

            TextMeshProUGUI inputText = CreateText(
                "Text",
                viewportObject.transform,
                string.Empty,
                18f,
                TextAlignmentOptions.MidlineLeft,
                template,
                TextColor);
            SetStretch(inputText.rectTransform, 0f, 0f, 0f, 0f);
            inputText.enableWordWrapping = false;

            TextMeshProUGUI placeholderText = CreateText(
                "Placeholder",
                viewportObject.transform,
                placeholder,
                18f,
                TextAlignmentOptions.MidlineLeft,
                template,
                PlaceholderColor);
            SetStretch(placeholderText.rectTransform, 0f, 0f, 0f, 0f);
            placeholderText.fontStyle |= FontStyles.Italic;

            TMP_InputField input = root.GetComponent<TMP_InputField>();
            input.targetGraphic = background;
            input.textViewport = viewport;
            input.textComponent = inputText;
            input.placeholder = placeholderText;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 160;
            input.richText = false;
            input.customCaretColor = true;
            input.caretColor = TextColor;
            input.selectionColor = new Color(0.5f, 0.65f, 0.85f, 0.35f);

            return new SearchFieldParts
            {
                Input = input,
                Placeholder = placeholderText,
                Text = inputText
            };
        }

        internal static ScrollAreaParts CreateGridScrollArea(
            string name, Transform parent, Vector2 cellSize, Vector2 spacing, int columns)
        {
            ScrollAreaParts parts = CreateScrollArea(name, parent);
            GridLayoutGroup grid = parts.Content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = cellSize;
            grid.spacing = spacing;
            grid.padding = new RectOffset(6, 6, 6, 6);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Math.Max(1, columns);
            parts.Grid = grid;
            ResponsiveModGrid.Attach(grid, parts.Viewport, cellSize, columns);
            return parts;
        }

        internal static ScrollAreaParts CreateScrollArea(string name, Transform parent)
        {
            if (nativeScrollTemplate == null) throw new InvalidOperationException(NativeControlMissing);
            GameObject root = UnityEngine.Object.Instantiate(nativeScrollTemplate.gameObject, parent, false);
            root.name = name;
            ScrollRect scrollRect = root.GetComponent<ScrollRect>();
            RectTransform oldContent = scrollRect.content;
            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObject.transform.SetParent(root.transform, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            SetStretch(viewport, 0f, 0f, ScrollSliderWidth + ScrollSliderGap, 0f);
            if (oldContent != null)
            {
                oldContent.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(oldContent.gameObject);
            }
            if (viewport.GetComponent<RectMask2D>() == null) viewport.gameObject.AddComponent<RectMask2D>();

            GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport, false);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 28f;
            // Native sliders are siblings of the game's ScrollRect, not children.
            // Clone and rebind one explicitly so the chooser never drives the list behind it.
            if (nativeSliderTemplate != null)
            {
                Slider slider = UnityEngine.Object.Instantiate(nativeSliderTemplate, root.transform, false);
                slider.name = "Scroll Slider";
                RectTransform sliderRect = slider.GetComponent<RectTransform>();
                sliderRect.anchorMin = new Vector2(1f, 0f);
                sliderRect.anchorMax = Vector2.one;
                sliderRect.offsetMin = new Vector2(-ScrollSliderWidth, ScrollSliderEndInset);
                sliderRect.offsetMax = new Vector2(0f, -ScrollSliderEndInset);
                slider.onValueChanged = new Slider.SliderEvent();
                scrollRect.onValueChanged = new ScrollRect.ScrollRectEvent();
                slider.onValueChanged.AddListener(value => scrollRect.verticalNormalizedPosition = value);
                scrollRect.onValueChanged.AddListener(value => slider.value = Mathf.Clamp01(value.y));
                slider.value = 1f;
                slider.gameObject.SetActive(true);
            }
            root.SetActive(true);

            return new ScrollAreaParts
            {
                ScrollRect = scrollRect,
                Viewport = viewport,
                Content = content
            };
        }

        private const string NativeSliderName = "Slider";

        internal static Slider FindScrollSlider(ScrollRect scroll)
        {
            Transform parent = scroll == null ? null : scroll.transform.parent;
            Transform sibling = parent == null ? null : parent.Find(NativeSliderName);
            return sibling == null ? null : sibling.GetComponent<Slider>();
        }

        private const float CardInset = 12f;
        private const float CardPortraitSize = 96f;
        private const float CardTitleHeight = 30f;
        private const float CardDescriptionTop = 46f;
        private const float CardDescriptionBottom = 50f;
        private const float CardActionHeight = 32f;

        internal static void ArrangeSelectionCard(Mods_Upload_Update_Button card)
        {
            if (card == null) return;
            float textLeft = CardInset + CardPortraitSize + CardInset;
            GameObject portraitRoot = card.Screenshot == null ? null : card.Screenshot.transform.parent.gameObject;
            RectTransform portrait = ReparentCardPart(portraitRoot, card.transform);
            if (portrait != null)
            {
                portrait.anchorMin = portrait.anchorMax = portrait.pivot = new Vector2(0f, 1f);
                portrait.anchoredPosition = new Vector2(CardInset, -CardInset);
                portrait.sizeDelta = new Vector2(CardPortraitSize, CardPortraitSize);
                SetStretch(card.Screenshot.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
                Image image = card.Screenshot.GetComponent<Image>();
                if (image != null) image.preserveAspect = true;
            }
            // Keep the label inside its native masked title background and resize
            // the whole header. Reparenting only the label retains a stale clip rect.
            RectTransform titleBackground = card.Title == null ? null : card.Title.transform.parent as RectTransform;
            TopRow(titleBackground, CardInset, CardTitleHeight, textLeft, CardInset);
            if (card.Title != null)
                SetStretch(card.Title.GetComponent<RectTransform>(), ButtonLabelInset, 0f, ButtonLabelInset, 0f);
            RectTransform description = ReparentCardPart(card.Description, card.transform);
            SetStretch(description, textLeft, CardDescriptionBottom, CardInset, CardDescriptionTop);
            RectTransform action = ReparentCardPart(card.Button_Upload, card.transform);
            if (action != null)
            {
                action.anchorMin = Vector2.zero;
                action.anchorMax = new Vector2(1f, 0f);
                action.pivot = new Vector2(0.5f, 0f);
                action.offsetMin = new Vector2(textLeft, CardInset);
                action.offsetMax = new Vector2(-CardInset, CardInset + CardActionHeight);
            }
            FitText(card.Title, true);
            FitText(card.Description, true);
        }

        private static RectTransform ReparentCardPart(GameObject part, Transform parent)
        {
            if (part == null) return null;
            part.transform.SetParent(parent, false);
            part.transform.localScale = Vector3.one;
            return part.GetComponent<RectTransform>();
        }

        internal static void TopRow(RectTransform rect, float top, float height, float left, float right)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
            rect.localScale = Vector3.one;
        }

        internal static void FitText(GameObject root, bool wrap)
        {
            if (root == null) return;
            foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.enableWordWrapping = wrap;
                text.overflowMode = TextOverflowModes.Ellipsis;
                HmbGameFont.Apply(text);
            }
        }

        internal static void ResetScroll(ScrollRect scroll)
        {
            if (scroll == null || scroll.content == null) return;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
        }

        internal static void CopyTextStyle(TextMeshProUGUI template, TextMeshProUGUI target)
        {
            if (target == null || template == null)
            {
                return;
            }
            if (template.font != null)
            {
                target.font = template.font;
            }
            HmbGameFont.Apply(target);
        }

        internal static void SetNormalizedRect(RectTransform rect, float minX, float minY, float maxX, float maxY, float inset = 0f)
        {
            if (rect == null)
            {
                return;
            }
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            rect.localScale = Vector3.one;
        }

        internal static void SetStretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            if (rect == null)
            {
                return;
            }
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            rect.localScale = Vector3.one;
        }

        internal static ScrollRect FindScrollRect(RectTransform content)
        {
            Transform current = content;
            while (current != null)
            {
                ScrollRect candidate = current.GetComponent<ScrollRect>();
                if (candidate != null && (candidate.content == null || candidate.content == content))
                {
                    if (candidate.content == null)
                    {
                        candidate.content = content;
                    }
                    return candidate;
                }
                current = current.parent;
            }
            return null;
        }

        internal static void DestroyChildren(Transform transform)
        {
            if (transform == null)
            {
                return;
            }
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child != null)
                {
                    child.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(child.gameObject);
                }
            }
        }
    }
}
