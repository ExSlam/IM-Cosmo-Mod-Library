using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IMUiFramework
{
    /// <summary>
    /// Named component-level presets discovered in IM_Scenes. They are intentionally small pieces,
    /// not whole popup clones, so a custom UI can combine them freely.
    /// </summary>
    public static class IMUiPresets
    {
        public const string ChartPreviousMonthPath = "Panel/Prev Month";
        public const string ChartNextMonthPath = "Panel/Next Month";
        public const string ChartMonthLabelPath = "Panel/Month";
        public const string ChartPanelPath = "Panel";
        public const string ProducerContractsScrollPath = "Panel/Slider";
        public const string ProducerSalariesScrollPath = "Panel/Slider";
        public const string ProducerLoansScrollPath = "Panel/Credit History/Slider";

        public static IMUiElementBuilder PreviousMonthButton()
        {
            // The arrow is the private-use glyph \uf33a from the serialized Linotte icon-capable TMP
            // font. Replacing that font with the user's body font can turn it into a missing-glyph box.
            return IMUiElementBuilder.FromPopup(PopupManager._type.single_chart, ChartPreviousMonthPath)
                .ApplyGameFont(false);
        }

        public static IMUiElementBuilder NextMonthButton()
        {
            // Same rule as PreviousMonthButton: preserve the exact source font for \uf33b.
            return IMUiElementBuilder.FromPopup(PopupManager._type.single_chart, ChartNextMonthPath)
                .ApplyGameFont(false);
        }

        public static IMUiElementBuilder ChartMonthLabel()
        {
            return IMUiElementBuilder.FromPopup(PopupManager._type.single_chart, ChartMonthLabelPath);
        }

        public static IMUiElementBuilder Control(VanillaControlType type)
        {
            return IMUiElementBuilder.FromControl(type);
        }
    }

    public sealed class IMUiMonthPagerOptions
    {
        public string ObjectName = "MonthPager";
        public string Label = string.Empty;
        public float Width = 420f;
        public float Height = 40f;
        public float LabelWidth = 280f;
        public float Spacing = 12f;
        // Optional horizontal breathing room added around the requested label width.
        // Zero preserves the pre-3.1.1 geometry for existing callers.
        public float LabelHorizontalPadding = 0f;
        public bool PreviousInteractable = true;
        public bool NextInteractable = true;
        public UnityAction OnPrevious;
        public UnityAction OnNext;
        public IMUiTheme Theme;
    }

    public sealed class IMUiMonthPagerHandle
    {
        public GameObject Root;
        public Button PreviousButton;
        public Button NextButton;
        public TextMeshProUGUI Label;

        public void SetLabel(string value)
        {
            if (Label != null) Label.text = value ?? string.Empty;
        }

        public void SetInteractable(bool previous, bool next)
        {
            if (PreviousButton != null) PreviousButton.interactable = previous;
            if (NextButton != null) NextButton.interactable = next;
        }
    }

    public sealed class IMUiScrollViewOptions
    {
        public string ObjectName = "ScrollView";
        public Vector2 OffsetMin = Vector2.zero;
        public Vector2 OffsetMax = Vector2.zero;
        public int PaddingLeft = 4;
        public int PaddingRight = 4;
        public int PaddingTop = 4;
        public int PaddingBottom = 4;
        public float Spacing = 1f;
        public bool AddBackground = true;
        public bool UseVanillaListIndicator = true;
        // Scene-derived defaults remain unchanged, while custom popups can place the
        // native producer-list slider closer to (or farther from) their right edge.
        public float VanillaIndicatorRightCenterInset = VanillaUiSceneTemplates.ProducerListSliderRightCenterInset;
        public float VanillaViewportRightInset = VanillaUiSceneTemplates.ProducerListViewportRightInset;
        // Some custom popups need the producer-list Slider to read as a fixed-thumb
        // scroll indicator rather than a progress/value control. Default false keeps
        // existing framework callers scene-exact.
        public bool VanillaIndicatorHideFill = false;
        public IMUiTheme Theme;
    }

    public sealed class IMUiScrollViewHandle
    {
        public GameObject Root;
        public RectTransform Viewport;
        public Transform Content;
        public ScrollRect ScrollRect;
        public GameObject ScrollIndicator;
    }

    public sealed class IMUiPopupOptions
    {
        public PopupManager._type RegistrationType;
        public string ObjectName = "CustomPopup";
        public string Title = "Custom Popup";
        public Vector2 Size = new Vector2(860f, 520f);
        public bool BlurBackground = true;
        public bool DarkenBackground = true;
        public IMUiTheme Theme;
        public bool UseChartPanelVisual = true;
        public bool RecolorChrome = true;
    }

    /// <summary>
    /// High-level patterns for custom UIs. These are where v3 saves setup code: a month selector is a
    /// real Single Chart selector, a list indicator is the Contracts/Salaries/Loans Slider pattern,
    /// and a popup shell can be composed with a custom theme without cloning an entire vanilla popup.
    /// </summary>
    public static class IMUiComposer
    {
        public static bool TryCreateMonthPager(
            Transform parent,
            string label,
            UnityAction onPrevious,
            UnityAction onNext,
            IMUiTheme theme,
            out IMUiMonthPagerHandle handle)
        {
            IMUiMonthPagerOptions options = new IMUiMonthPagerOptions();
            options.Label = label ?? string.Empty;
            options.OnPrevious = onPrevious;
            options.OnNext = onNext;
            options.Theme = theme;
            return TryCreateMonthPager(parent, options, out handle);
        }

        public static bool TryCreateScrollView(
            Transform parent,
            Vector2 offsetMin,
            Vector2 offsetMax,
            IMUiTheme theme,
            out IMUiScrollViewHandle handle)
        {
            IMUiScrollViewOptions options = new IMUiScrollViewOptions();
            options.OffsetMin = offsetMin;
            options.OffsetMax = offsetMax;
            options.Theme = theme;
            return TryCreateScrollView(parent, options, out handle);
        }

        public static bool TryCreateRegisteredPopup(
            PopupManager._type registrationType,
            string objectName,
            string title,
            Vector2 size,
            IMUiTheme theme,
            out PopupScaffold scaffold)
        {
            IMUiPopupOptions options = new IMUiPopupOptions();
            options.RegistrationType = registrationType;
            options.ObjectName = objectName;
            options.Title = title;
            options.Size = size;
            options.Theme = theme;
            return TryCreateRegisteredPopup(options, out scaffold);
        }

        public static bool TryCreateMonthPager(Transform parent, IMUiMonthPagerOptions options, out IMUiMonthPagerHandle handle)
        {
            handle = null;
            if (parent == null) return false;
            if (options == null) options = new IMUiMonthPagerOptions();

            GameObject root = new GameObject(
                string.IsNullOrEmpty(options.ObjectName) ? "MonthPager" : options.ObjectName,
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup));
            root.transform.SetParent(parent, false);
            IMUiKit.ApplyLayerRecursively(root, parent.gameObject.layer);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(options.Width, options.Height);

            HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = options.Spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            IMUiElementHandle previousHandle;
            bool previousCreated = IMUiPresets.PreviousMonthButton()
                .Parent(root.transform)
                .Named("Previous")
                .OnClick(options.OnPrevious)
                .Theme(options.Theme, options.Theme != null ? IMUiThemeApplication.AccentOnly : IMUiThemeApplication.None)
                .Build(out previousHandle);

            IMUiElementHandle labelHandle;
            bool labelCreated = IMUiPresets.ChartMonthLabel()
                .Parent(root.transform)
                .Named("Label")
                .Text(options.Label)
                .Build(out labelHandle);

            IMUiElementHandle nextHandle;
            bool nextCreated = IMUiPresets.NextMonthButton()
                .Parent(root.transform)
                .Named("Next")
                .OnClick(options.OnNext)
                .Theme(options.Theme, options.Theme != null ? IMUiThemeApplication.AccentOnly : IMUiThemeApplication.None)
                .Build(out nextHandle);

            if (!previousCreated || !labelCreated || !nextCreated)
            {
                UnityEngine.Object.Destroy(root);
                return false;
            }

            Button previous = previousHandle.Get<Button>();
            Button next = nextHandle.Get<Button>();
            TextMeshProUGUI label = labelHandle.Get<TextMeshProUGUI>();
            if (previous == null || next == null || label == null)
            {
                UnityEngine.Object.Destroy(root);
                return false;
            }

            previous.interactable = options.PreviousInteractable;
            next.interactable = options.NextInteractable;

            SetLayoutSize(previous.gameObject, GetPreferredWidth(previous.gameObject, 32.9545f), GetPreferredHeight(previous.gameObject, 29f));
            SetLayoutSize(next.gameObject, GetPreferredWidth(next.gameObject, 32.9545f), GetPreferredHeight(next.gameObject, 29f));
            float effectiveLabelWidth = Mathf.Max(0f, options.LabelWidth) +
                Mathf.Max(0f, options.LabelHorizontalPadding) * 2f;
            SetLayoutSize(label.gameObject, effectiveLabelWidth, options.Height);

            RectTransform labelRect = label.GetComponent<RectTransform>();
            if (labelRect != null)
            {
                labelRect.anchorMin = new Vector2(0.5f, 0.5f);
                labelRect.anchorMax = new Vector2(0.5f, 0.5f);
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = Vector2.zero;
                labelRect.sizeDelta = new Vector2(effectiveLabelWidth, options.Height);
            }
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            if (options.Theme != null) label.color = options.Theme.Title;

            IMUiMonthPagerHandle created = new IMUiMonthPagerHandle();
            created.Root = root;
            created.PreviousButton = previous;
            created.NextButton = next;
            created.Label = label;
            handle = created;
            return true;
        }

        public static bool TryCreateScrollView(Transform parent, IMUiScrollViewOptions options, out IMUiScrollViewHandle handle)
        {
            handle = null;
            if (parent == null) return false;
            if (options == null) options = new IMUiScrollViewOptions();
            IMUiTheme theme = options.Theme ?? IMUiTheme.Vanilla();

            GameObject root = new GameObject(
                string.IsNullOrEmpty(options.ObjectName) ? "ScrollView" : options.ObjectName,
                typeof(RectTransform),
                typeof(ScrollRect));
            root.transform.SetParent(parent, false);
            IMUiKit.ApplyLayerRecursively(root, parent.gameObject.layer);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = options.OffsetMin;
            rootRect.offsetMax = options.OffsetMax;

            if (options.AddBackground)
            {
                Image rootImage = root.AddComponent<Image>();
                ApplyChartPanelImage(rootImage, theme.SurfaceInner);
            }

            ScrollRect scroll = root.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            IMUiKit.ApplyVanillaScrollDefaults(scroll);

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            viewport.layer = root.layer;
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            Image viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = theme.SurfaceInner;
            // RectMask2D clips strictly to the viewport rectangle without depending
            // on the alpha/sprite geometry of the surface Image. This prevents child
            // cards and labels from leaking past the visible sheet while scrolling.

            GameObject content = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            content.layer = root.layer;
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.padding = new RectOffset(options.PaddingLeft, options.PaddingRight, options.PaddingTop, options.PaddingBottom);
            contentLayout.spacing = options.Spacing;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            GameObject indicator = null;
            if (options.UseVanillaListIndicator)
            {
                Slider ignored;
                if (VanillaUiSceneTemplates.TryCreateProducerListScrollSlider(
                        root.transform,
                        scroll,
                        "Slider",
                        options.VanillaIndicatorRightCenterInset,
                        options.VanillaViewportRightInset,
                        out indicator,
                        out ignored))
                {
                    if (options.Theme != null)
                    {
                        IMUiStyle.ApplyTheme(indicator, theme, IMUiThemeApplication.AccentOnly, true);
                    }
                    if (options.VanillaIndicatorHideFill && ignored != null && ignored.fillRect != null)
                    {
                        ignored.fillRect.gameObject.SetActive(false);
                    }
                }
            }

            IMUiScrollViewHandle created = new IMUiScrollViewHandle();
            created.Root = root;
            created.Viewport = viewportRect;
            created.Content = content.transform;
            created.ScrollRect = scroll;
            created.ScrollIndicator = indicator;
            handle = created;
            return true;
        }

        public static bool TryAttachVanillaListScrollIndicator(
            Transform parent,
            ScrollRect target,
            string objectName,
            IMUiTheme theme,
            out GameObject indicator)
        {
            indicator = null;
            if (parent == null || target == null) return false;
            Slider slider;
            if (!VanillaUiSceneTemplates.TryCreateProducerListScrollSlider(parent, target, objectName, out indicator, out slider))
            {
                return false;
            }
            if (theme != null)
            {
                IMUiStyle.ApplyTheme(indicator, theme, IMUiThemeApplication.AccentOnly, true);
            }
            return true;
        }

        public static bool TryCreateRegisteredPopup(IMUiPopupOptions options, out PopupScaffold scaffold)
        {
            scaffold = null;
            if (options == null) return false;
            IMUiTheme theme = options.Theme ?? IMUiTheme.Vanilla();

            if (!IMUiKit.TryCreateRegisteredPopupScaffold(
                options.RegistrationType,
                options.ObjectName,
                options.Title,
                options.Size,
                options.BlurBackground,
                options.DarkenBackground,
                out scaffold) || scaffold == null)
            {
                return false;
            }

            if (scaffold.PanelRect != null)
            {
                Image panel = scaffold.PanelRect.GetComponent<Image>();
                if (panel == null) panel = scaffold.PanelRect.gameObject.AddComponent<Image>();
                if (options.UseChartPanelVisual) ApplyChartPanelImage(panel, theme.SurfaceOuter);
                else panel.color = theme.SurfaceOuter;
            }

            if (scaffold.TitleText != null)
            {
                scaffold.TitleText.color = theme.Title;
                VanillaUiFonts.ApplyGameFont(scaffold.TitleText);
            }

            if (scaffold.ScrollRect != null)
            {
                Image image = scaffold.ScrollRect.GetComponent<Image>();
                if (image != null) image.color = theme.SurfaceInner;
                if (scaffold.ScrollRect.viewport != null)
                {
                    Image viewport = scaffold.ScrollRect.viewport.GetComponent<Image>();
                    if (viewport != null) viewport.color = theme.SurfaceInner;
                }
            }

            if (options.RecolorChrome && scaffold.CloseButton != null)
            {
                IMUiStyle.ApplyTheme(scaffold.CloseButton.gameObject, theme, IMUiThemeApplication.Interactive, true);
            }

            return true;
        }

        /// <summary>
        /// Applies a clean vanilla card surface to a custom calendar/grid cell. It reuses the actual
        /// sliced Single Chart panel sprite rather than an Outline component, so a custom grid reads as
        /// Idol Manager UI instead of a spreadsheet drawn with 1px boxes.
        /// </summary>
        public static void ApplyCalendarCellStyle(GameObject cell, IMUiTheme theme, bool emptyCell)
        {
            if (cell == null) return;
            if (theme == null) theme = IMUiTheme.Vanilla();

            Outline oldOutline = cell.GetComponent<Outline>();
            if (oldOutline != null)
            {
                try { UnityEngine.Object.DestroyImmediate(oldOutline); }
                catch { UnityEngine.Object.Destroy(oldOutline); }
            }

            Image image = cell.GetComponent<Image>();
            if (image == null) image = cell.AddComponent<Image>();
            ApplyChartPanelImage(image, emptyCell ? theme.SurfaceOuter : theme.SurfaceRaised);
            Color color = image.color;
            if (emptyCell) color.a = 0.55f;
            image.color = color;
            image.raycastTarget = !emptyCell;
        }

        public static GameObject CreateVanillaGrid(
            Transform parent,
            string objectName,
            int columns,
            Vector2 cellSize,
            Vector2 spacing,
            TextAnchor alignment)
        {
            if (parent == null) return null;
            GameObject root = new GameObject(
                string.IsNullOrEmpty(objectName) ? "Grid" : objectName,
                typeof(RectTransform),
                typeof(GridLayoutGroup));
            root.transform.SetParent(parent, false);
            IMUiKit.ApplyLayerRecursively(root, parent.gameObject.layer);
            GridLayoutGroup grid = root.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(1, columns);
            grid.cellSize = cellSize;
            grid.spacing = spacing;
            grid.childAlignment = alignment;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            return root;
        }

        private static void ApplyChartPanelImage(Image target, Color color)
        {
            if (target == null) return;
            Transform source;
            if (VanillaUiSceneCatalog.TryFindPopupChild(PopupManager._type.single_chart, IMUiPresets.ChartPanelPath, out source) && source != null)
            {
                Image sourceImage = source.GetComponent<Image>();
                if (sourceImage != null)
                {
                    IMUiStyle.CopyImageVisual(sourceImage, target, false);
                }
            }
            target.color = color;
        }

        private static float GetPreferredWidth(GameObject obj, float fallback)
        {
            if (obj == null) return fallback;
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect != null && rect.sizeDelta.x > 0f) return rect.sizeDelta.x;
            return fallback;
        }

        private static float GetPreferredHeight(GameObject obj, float fallback)
        {
            if (obj == null) return fallback;
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect != null && rect.sizeDelta.y > 0f) return rect.sizeDelta.y;
            return fallback;
        }

        private static void SetLayoutSize(GameObject obj, float width, float height)
        {
            if (obj == null) return;
            LayoutElement layout = obj.GetComponent<LayoutElement>();
            if (layout == null) layout = obj.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            layout.minWidth = 0f;
            layout.minHeight = 0f;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
        }
    }
}
