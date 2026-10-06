using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HarmonyModBrowser
{
    // Reserve real viewport space for fixed controls instead of drawing them over
    // scrolling cards. Moving the whole ScrollRect keeps its native indicator aligned.
    internal sealed class DockedModList
    {
        internal readonly RectTransform Root;
        private readonly RectTransform scroll;
        private readonly Transform parent;
        private readonly int sibling;
        private readonly Vector2 anchorMin, anchorMax, pivot, size, position;
        private readonly Vector3 scale;
        private readonly Quaternion rotation;
        private readonly NativeSliderDock sliderDock;

        internal DockedModList(ScrollRect list, float header, float footer, float gap)
        {
            Slider nativeSlider = RuntimeUi.FindScrollSlider(list);
            scroll = list.GetComponent<RectTransform>();
            parent = scroll.parent;
            sibling = scroll.GetSiblingIndex();
            anchorMin = scroll.anchorMin;
            anchorMax = scroll.anchorMax;
            pivot = scroll.pivot;
            size = scroll.sizeDelta;
            position = scroll.anchoredPosition;
            scale = scroll.localScale;
            rotation = scroll.localRotation;
            GameObject root = new GameObject("HMB Fixed List Layout", typeof(RectTransform));
            root.layer = scroll.gameObject.layer;
            Root = root.GetComponent<RectTransform>();
            Root.SetParent(parent, false);
            Root.SetSiblingIndex(sibling);
            RestoreRect(Root);
            scroll.SetParent(Root, false);
            scroll.localRotation = Quaternion.identity;
            float bottom = footer > 0f ? footer + gap : 0f;
            RuntimeUi.SetStretch(scroll, 0f, bottom, 0f, header + gap);
            sliderDock = new NativeSliderDock(nativeSlider, Root, header + gap, bottom);
            RectTransform viewport = list.viewport != null ? list.viewport : scroll;
            if (viewport.GetComponent<RectMask2D>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();
        }

        private void RestoreRect(RectTransform target)
        {
            target.anchorMin = anchorMin;
            target.anchorMax = anchorMax;
            target.pivot = pivot;
            target.sizeDelta = size;
            target.anchoredPosition = position;
            target.localScale = scale;
            target.localRotation = rotation;
        }

        internal void Restore()
        {
            if (scroll != null && parent != null)
            {
                scroll.SetParent(parent, false);
                scroll.SetSiblingIndex(sibling);
                RestoreRect(scroll);
            }
            sliderDock.Restore();
            if (Root != null) UnityEngine.Object.Destroy(Root.gameObject);
        }
    }

    internal sealed class NativeSliderDock
    {
        private readonly RectTransform rect;
        private readonly Transform parent;
        private readonly int sibling;
        private readonly Vector2 anchorMin, anchorMax, offsetMin, offsetMax;

        internal NativeSliderDock(Slider slider, Transform dock, float top, float bottom)
        {
            if (slider == null) return;
            rect = slider.GetComponent<RectTransform>();
            parent = rect.parent;
            sibling = rect.GetSiblingIndex();
            anchorMin = rect.anchorMin;
            anchorMax = rect.anchorMax;
            offsetMin = rect.offsetMin;
            offsetMax = rect.offsetMax;
            float width = rect.rect.width;
            rect.SetParent(dock, false);
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(0f, bottom);
            rect.offsetMax = new Vector2(width, -top);
        }

        internal void Restore()
        {
            if (rect == null || parent == null) return;
            rect.SetParent(parent, false);
            rect.SetSiblingIndex(sibling);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }

    // The game's fixed canvas can extend beyond a narrow window. Fit only this
    // mod's overlay panel to the visible screen, without changing the game canvas.
    internal sealed class ScreenFittedPanel : MonoBehaviour
    {
        private const float HorizontalMargin = 0.04f;
        private const float VerticalMargin = 0.05f;
        private RectTransform panel;
        private RectTransform parent;
        private Canvas canvas;
        private Vector2 screenSize;

        internal static void Attach(RectTransform target)
        {
            ScreenFittedPanel fit = target.gameObject.AddComponent<ScreenFittedPanel>();
            fit.panel = target;
            fit.parent = target.parent as RectTransform;
            fit.canvas = target.GetComponentInParent<Canvas>();
            fit.Refresh();
        }

        private void LateUpdate()
        {
            if (screenSize.x != Screen.width || screenSize.y != Screen.height) Refresh();
        }

        private void Refresh()
        {
            if (parent == null || canvas == null) return;
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 minimum, maximum;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,
                    new Vector2(Screen.width * HorizontalMargin, Screen.height * VerticalMargin), camera, out minimum) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,
                    new Vector2(Screen.width * (1f - HorizontalMargin), Screen.height * (1f - VerticalMargin)), camera, out maximum))
                return;
            panel.anchorMin = panel.anchorMax = parent.pivot;
            panel.offsetMin = minimum;
            panel.offsetMax = maximum;
            screenSize = new Vector2(Screen.width, Screen.height);
        }
    }

    internal sealed class OppositeBackButtonLayout : MonoBehaviour
    {
        private RectTransform button, back, boundary, parent;
        private readonly Vector3[] backCorners = new Vector3[4];
        private readonly Vector3[] boundaryCorners = new Vector3[4];

        internal static void Attach(RectTransform button, RectTransform back, RectTransform boundary)
        {
            OppositeBackButtonLayout layout = button.gameObject.AddComponent<OppositeBackButtonLayout>();
            layout.button = button;
            layout.back = back;
            layout.boundary = boundary;
            layout.parent = button.parent as RectTransform;
            layout.LateUpdate();
        }

        private void LateUpdate()
        {
            if (back == null || boundary == null || parent == null) return;
            back.GetWorldCorners(backCorners);
            boundary.GetWorldCorners(boundaryCorners);
            Vector3 bottom = parent.InverseTransformPoint(backCorners[0]);
            Vector3 top = parent.InverseTransformPoint(backCorners[2]);
            Vector3 right = parent.InverseTransformPoint(boundaryCorners[2]);
            Vector2 size = new Vector2(top.x - bottom.x, top.y - bottom.y);
            Vector2 position = new Vector2(right.x - size.x, bottom.y);
            if (button.sizeDelta == size && button.anchoredPosition == position && button.anchorMin == parent.pivot)
                return;
            button.localScale = Vector3.one;
            button.anchorMin = button.anchorMax = parent.pivot;
            button.pivot = Vector2.zero;
            button.sizeDelta = size;
            button.anchoredPosition = position;
        }
    }

    internal sealed class ResponsiveModGrid : MonoBehaviour
    {
        private const float WidthTolerance = 0.5f;
        private const float MinimumUsableWidth = 1f;
        internal const int MaximumBrowserColumns = 3;
        internal const int MaximumChooserColumns = 2;
        internal const float ChooserCardWidth = 350f;
        internal const float ChooserCardHeight = 148f;
        private GridLayoutGroup grid;
        private RectTransform viewport;
        private Vector2 preferredSize;
        private Vector2 originalCellSize;
        private GridLayoutGroup.Constraint originalConstraint;
        private int originalColumns;
        private int maximumColumns;
        private float lastWidth = -1f;

        internal static ResponsiveModGrid Attach(GridLayoutGroup target, RectTransform viewport,
            Vector2 cellSize, int maxColumns)
        {
            ResponsiveModGrid layout = target.GetComponent<ResponsiveModGrid>();
            if (layout == null) layout = target.gameObject.AddComponent<ResponsiveModGrid>();
            layout.grid = target;
            layout.viewport = viewport;
            layout.preferredSize = cellSize;
            layout.maximumColumns = maxColumns;
            layout.originalCellSize = target.cellSize;
            layout.originalConstraint = target.constraint;
            layout.originalColumns = target.constraintCount;
            layout.lastWidth = -1f;
            RectTransform content = target.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, content.sizeDelta.y);
            content.anchoredPosition = Vector2.zero;
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            layout.Refresh();
            return layout;
        }

        private void LateUpdate() { Refresh(); }

        internal void Refresh()
        {
            if (grid == null || viewport == null) return;
            float width = viewport.rect.width - grid.padding.horizontal;
            if (width < MinimumUsableWidth || Mathf.Abs(width - lastWidth) < WidthTolerance) return;
            lastWidth = width;
            int columns = Mathf.Clamp(Mathf.FloorToInt((width + grid.spacing.x) /
                (preferredSize.x + grid.spacing.x)), 1, maximumColumns);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = new Vector2((width - (columns - 1) * grid.spacing.x) / columns, preferredSize.y);
            LayoutRebuilder.MarkLayoutForRebuild(grid.GetComponent<RectTransform>());
        }

        internal void Restore()
        {
            enabled = false;
            if (grid != null)
            {
                grid.cellSize = originalCellSize;
                grid.constraint = originalConstraint;
                grid.constraintCount = originalColumns;
            }
            UnityEngine.Object.Destroy(this);
        }
    }

    // Embedded subset of IM UI Framework's game-font bridge. No external framework
    // or global Harmony patches are needed for these browser-only text controls.
    internal static class HmbGameFont
    {
        private const string CreateFontMethod = "CreateFontAsset";
        private const string RuntimeFontPrefix = "HMB Game Font - ";
        private static Font source;
        private static TMP_FontAsset generated;

        internal static void ApplyAll(GameObject root)
        {
            if (root == null) return;
            foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true)) Apply(text);
        }

        internal static void Apply(TextMeshProUGUI text)
        {
            if (text == null) return;
            TMP_FontAsset current = GetCurrent();
            if (current != null) text.font = current;
        }

        internal static TMP_FontAsset GetCurrent()
        {
            mainScript main = Camera.main == null ? null : Camera.main.GetComponent<mainScript>();
            Fonts fonts = main == null || main.Data == null ? null : main.Data.GetComponent<Fonts>();
            if (fonts == null || !fonts.IsReady()) return null;
            Font selected = fonts.GetFont();
            if (source != selected || generated == null)
            {
                TMP_FontAsset replacement = Create(selected);
                if (replacement != null)
                {
                    // Preserve the game's fallback glyphs, including the Steam icon
                    // prefixed to Workshop titles by Mod_Button.RenderTitle().
                    if (replacement.fallbackFontAssetTable == null)
                        replacement.fallbackFontAssetTable = new List<TMP_FontAsset>();
                    if (fonts.FontAsset != null && !replacement.fallbackFontAssetTable.Contains(fonts.FontAsset))
                        replacement.fallbackFontAssetTable.Add(fonts.FontAsset);
                    if (generated != null) UnityEngine.Object.Destroy(generated);
                    generated = replacement;
                    source = selected;
                }
            }
            return generated != null && source == selected ? generated : fonts.FontAsset;
        }

        private static TMP_FontAsset Create(Font font)
        {
            if (font == null) return null;
            try
            {
                MethodInfo factory = null;
                foreach (MethodInfo method in typeof(TMP_FontAsset).GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    ParameterInfo[] args = method.GetParameters();
                    if (method.Name == CreateFontMethod && args.Length > 0 && args[0].ParameterType == typeof(Font) &&
                        (factory == null || args.Length < factory.GetParameters().Length))
                        factory = method;
                }
                if (factory == null) return null;
                ParameterInfo[] parameters = factory.GetParameters();
                object[] values = new object[parameters.Length];
                values[0] = font;
                for (int index = 1; index < values.Length; index++)
                    values[index] = parameters[index].HasDefaultValue ? parameters[index].DefaultValue :
                        Activator.CreateInstance(parameters[index].ParameterType);
                TMP_FontAsset result = factory.Invoke(null, values) as TMP_FontAsset;
                if (result != null) result.name = RuntimeFontPrefix + font.name;
                return result;
            }
            catch { return null; }
        }
    }
}
