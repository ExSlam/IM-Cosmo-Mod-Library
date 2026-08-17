using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GraduationCalendar.EmbeddedIMUiFramework
{
    /// <summary>
    /// Semantic surfaces used by custom Idol Manager UI. The geometry/sprite can still come from a
    /// vanilla scene object; this enum only decides which theme role supplies the color.
    /// </summary>
    public enum IMUiSurfaceRole
    {
        Outer,
        Inner,
        Raised,
        Muted,
        Accent,
        Divider,
        Transparent
    }

    public enum IMUiTextRole
    {
        Body,
        Secondary,
        Muted,
        Title,
        OnAccent,
        Success,
        Danger,
        Warning,
        Gold
    }

    /// <summary>
    /// Common button shapes. ChartPreviousMonth/ChartNextMonth are scene-native Idol Manager controls;
    /// the remaining entries are exact Modern UI Pack prefabs shipped in the game's Resources.
    /// </summary>
    public enum IMUiButtonPreset
    {
        Basic,
        BasicOutline,
        BasicWithIcon,
        BasicOutlineWithIcon,
        Rounded,
        RoundedOutline,
        RadialIcon,
        ChartPreviousMonth,
        ChartNextMonth
    }

    public sealed class IMUiSurfaceOptions
    {
        public string ObjectName = "Surface";
        public IMUiSurfaceRole Role = IMUiSurfaceRole.Inner;
        public IMUiTheme Theme;
        public Vector2 Size = Vector2.zero;
        public bool StretchToParent;
        public bool RaycastTarget;
        public bool UseVanillaPanelSprite = true;
    }

    public sealed class IMUiTextOptions
    {
        public string ObjectName = "Text";
        public string Text = string.Empty;
        public IMUiTextRole Role = IMUiTextRole.Body;
        public IMUiTheme Theme;
        public Vector2 Size = Vector2.zero;
        public float FontSize = 24f;
        public TextAlignmentOptions Alignment = TextAlignmentOptions.Left;
        public bool WordWrap = true;
        public bool RaycastTarget;
    }

    public sealed class IMUiButtonOptions
    {
        public string ObjectName = "Button";
        public IMUiButtonPreset Preset = IMUiButtonPreset.Basic;
        public string ResourcePath;
        // Null means preserve the source text/glyph. This is important for the chart arrow presets.
        public string Text;
        public IMUiTheme Theme;
        public Vector2 Size = Vector2.zero;
        public UnityAction OnClick;
        public bool Interactable = true;
        public bool Active = true;
    }

    public sealed class IMUiGridOptions
    {
        public string ObjectName = "Grid";
        public int Columns = 7;
        public Vector2 CellSize = new Vector2(100f, 100f);
        public Vector2 Spacing = new Vector2(4f, 4f);
        public RectOffset Padding = new RectOffset();
        public TextAnchor Alignment = TextAnchor.UpperLeft;
        public bool FitHeight = true;
    }

    /// <summary>
    /// Small, composable building blocks for custom layouts. Unlike the v3 popup-template API, these
    /// methods do not dictate the layout of a whole vanilla screen. They borrow vanilla sprites,
    /// fonts, transitions and exact controls while leaving placement/content/colors to the mod.
    /// </summary>
    public static class IMUiPrimitives
    {
        // Short-form overloads for the common case. Options objects remain available when a mod
        // needs precise layout or behavior, but simple custom UI should not need configuration
        // ceremony just to get a vanilla-looking panel, label, card, grid, or button.
        public static GameObject CreatePanel(Transform parent, string objectName, Vector2 size, IMUiTheme theme)
        {
            IMUiSurfaceOptions options = new IMUiSurfaceOptions();
            options.ObjectName = string.IsNullOrEmpty(objectName) ? "Panel" : objectName;
            options.Role = IMUiSurfaceRole.Outer;
            options.Size = size;
            options.Theme = theme;
            return CreateSurface(parent, options);
        }

        public static GameObject CreateCard(Transform parent, string objectName, Vector2 size, IMUiTheme theme)
        {
            IMUiSurfaceOptions options = new IMUiSurfaceOptions();
            options.ObjectName = string.IsNullOrEmpty(objectName) ? "Card" : objectName;
            options.Role = IMUiSurfaceRole.Raised;
            options.Size = size;
            options.Theme = theme;
            return CreateSurface(parent, options);
        }

        public static TextMeshProUGUI CreateLabel(
            Transform parent,
            string text,
            float fontSize,
            TextAlignmentOptions alignment,
            IMUiTheme theme)
        {
            IMUiTextOptions options = new IMUiTextOptions();
            options.Text = text ?? string.Empty;
            options.FontSize = fontSize;
            options.Alignment = alignment;
            options.Theme = theme;
            return CreateText(parent, options);
        }

        public static bool TryCreateButton(
            Transform parent,
            IMUiButtonPreset preset,
            string text,
            UnityAction onClick,
            IMUiTheme theme,
            out Button button)
        {
            IMUiButtonOptions options = new IMUiButtonOptions();
            options.Preset = preset;
            options.Text = text;
            options.OnClick = onClick;
            options.Theme = theme;
            return TryCreateButton(parent, options, out button);
        }

        public static GameObject CreateGrid(
            Transform parent,
            string objectName,
            int columns,
            Vector2 cellSize,
            Vector2 spacing)
        {
            IMUiGridOptions options = new IMUiGridOptions();
            options.ObjectName = objectName;
            options.Columns = columns;
            options.CellSize = cellSize;
            options.Spacing = spacing;
            return CreateGrid(parent, options);
        }

        public static GameObject CreateSurface(Transform parent, IMUiSurfaceOptions options)
        {
            if (parent == null) return null;
            if (options == null) options = new IMUiSurfaceOptions();
            IMUiTheme theme = options.Theme ?? IMUiTheme.Vanilla();

            GameObject root = new GameObject(
                string.IsNullOrEmpty(options.ObjectName) ? "Surface" : options.ObjectName,
                typeof(RectTransform),
                typeof(Image));
            root.transform.SetParent(parent, false);
            IMUiKit.ApplyLayerRecursively(root, parent.gameObject.layer);

            RectTransform rect = root.GetComponent<RectTransform>();
            if (options.StretchToParent)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else if (options.Size.x > 0f || options.Size.y > 0f)
            {
                rect.sizeDelta = options.Size;
            }

            Image image = root.GetComponent<Image>();
            if (options.UseVanillaPanelSprite)
            {
                TryCopyVanillaPanelVisual(image);
            }
            image.color = ResolveSurfaceColor(theme, options.Role);
            image.raycastTarget = options.RaycastTarget;
            return root;
        }

        public static TextMeshProUGUI CreateText(Transform parent, IMUiTextOptions options)
        {
            if (parent == null) return null;
            if (options == null) options = new IMUiTextOptions();
            IMUiTheme theme = options.Theme ?? IMUiTheme.Vanilla();

            GameObject root = new GameObject(
                string.IsNullOrEmpty(options.ObjectName) ? "Text" : options.ObjectName,
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            root.transform.SetParent(parent, false);
            IMUiKit.ApplyLayerRecursively(root, parent.gameObject.layer);

            RectTransform rect = root.GetComponent<RectTransform>();
            if (options.Size.x > 0f || options.Size.y > 0f) rect.sizeDelta = options.Size;

            TextMeshProUGUI text = root.GetComponent<TextMeshProUGUI>();
            text.text = options.Text ?? string.Empty;
            text.fontSize = options.FontSize;
            text.alignment = options.Alignment;
            text.enableWordWrapping = options.WordWrap;
            text.raycastTarget = options.RaycastTarget;
            text.color = ResolveTextColor(theme, options.Role);
            TMP_FontAsset chosenFont = options.Role == IMUiTextRole.Title ? theme.TitleFont : theme.BodyFont;
            if (chosenFont != null) text.font = chosenFont;
            else VanillaUiFonts.ApplyGameFont(text);
            return text;
        }

        public static bool TryCreateButton(Transform parent, IMUiButtonOptions options, out Button button)
        {
            button = null;
            if (parent == null) return false;
            if (options == null) options = new IMUiButtonOptions();

            IMUiElementBuilder builder;
            if (!string.IsNullOrEmpty(options.ResourcePath))
            {
                builder = IMUiElementBuilder.FromResource(options.ResourcePath);
            }
            else
            {
                switch (options.Preset)
                {
                    case IMUiButtonPreset.ChartPreviousMonth:
                        builder = IMUiPresets.PreviousMonthButton();
                        break;
                    case IMUiButtonPreset.ChartNextMonth:
                        builder = IMUiPresets.NextMonthButton();
                        break;
                    default:
                        builder = IMUiElementBuilder.FromResource(GetButtonResourcePath(options.Preset));
                        break;
                }
            }

            builder.Parent(parent)
                .Named(options.ObjectName)
                .Active(options.Active)
                .OnClick(options.OnClick);

            if (options.Text != null) builder.Text(options.Text);
            if (options.Theme != null) builder.Theme(options.Theme, IMUiThemeApplication.Interactive);
            if (options.Size.x > 0f || options.Size.y > 0f)
            {
                builder.Size(options.Size.x > 0f ? options.Size.x : -1f, options.Size.y > 0f ? options.Size.y : -1f);
            }

            IMUiElementHandle handle;
            if (!builder.Build(out handle) || handle == null || handle.Root == null) return false;
            button = handle.Get<Button>();
            if (button == null)
            {
                UnityEngine.Object.Destroy(handle.Root);
                return false;
            }
            button.interactable = options.Interactable;
            return true;
        }

        /// <summary>
        /// Creates any Modern UI Pack family using the same semantic theme system as the scene-native
        /// components. Exact prefab variants can still be selected through options.ResourcePath.
        /// </summary>
        public static bool TryCreateControl(Transform parent, VanillaControlOptions options, out GameObject instance)
        {
            return VanillaUiControlFactory.TryCreate(parent, options, out instance);
        }

        public static GameObject CreateDivider(Transform parent, string objectName, float thickness, IMUiTheme theme)
        {
            IMUiSurfaceOptions options = new IMUiSurfaceOptions();
            options.ObjectName = string.IsNullOrEmpty(objectName) ? "Divider" : objectName;
            options.Role = IMUiSurfaceRole.Divider;
            options.Theme = theme;
            options.Size = new Vector2(100f, Mathf.Max(1f, thickness));
            options.UseVanillaPanelSprite = false;
            return CreateSurface(parent, options);
        }

        public static GameObject CreateGrid(Transform parent, IMUiGridOptions options)
        {
            if (parent == null) return null;
            if (options == null) options = new IMUiGridOptions();

            GameObject root = new GameObject(
                string.IsNullOrEmpty(options.ObjectName) ? "Grid" : options.ObjectName,
                typeof(RectTransform),
                typeof(GridLayoutGroup));
            root.transform.SetParent(parent, false);
            IMUiKit.ApplyLayerRecursively(root, parent.gameObject.layer);

            GridLayoutGroup grid = root.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(1, options.Columns);
            grid.cellSize = options.CellSize;
            grid.spacing = options.Spacing;
            grid.padding = options.Padding ?? new RectOffset();
            grid.childAlignment = options.Alignment;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;

            if (options.FitHeight)
            {
                ContentSizeFitter fitter = root.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            return root;
        }

        public static void ApplyCard(GameObject target, IMUiTheme theme, bool muted)
        {
            if (target == null) return;
            if (theme == null) theme = IMUiTheme.Vanilla();
            Image image = target.GetComponent<Image>();
            if (image == null) image = target.AddComponent<Image>();
            TryCopyVanillaPanelVisual(image);
            image.color = muted ? theme.SurfaceMuted : theme.SurfaceRaised;
        }

        public static Color32 ResolveSurfaceColor(IMUiTheme theme, IMUiSurfaceRole role)
        {
            if (theme == null) theme = IMUiTheme.Vanilla();
            switch (role)
            {
                case IMUiSurfaceRole.Outer: return theme.SurfaceOuter;
                case IMUiSurfaceRole.Raised: return theme.SurfaceRaised;
                case IMUiSurfaceRole.Muted: return theme.SurfaceMuted;
                case IMUiSurfaceRole.Accent: return theme.Accent;
                case IMUiSurfaceRole.Divider: return theme.Divider;
                case IMUiSurfaceRole.Transparent: return mainScript.transparent32;
                default: return theme.SurfaceInner;
            }
        }

        public static Color32 ResolveTextColor(IMUiTheme theme, IMUiTextRole role)
        {
            if (theme == null) theme = IMUiTheme.Vanilla();
            switch (role)
            {
                case IMUiTextRole.Secondary: return theme.TextSecondary;
                case IMUiTextRole.Muted: return theme.TextMuted;
                case IMUiTextRole.Title: return theme.Title;
                case IMUiTextRole.OnAccent: return theme.TextOnAccent;
                case IMUiTextRole.Success: return theme.Success;
                case IMUiTextRole.Danger: return theme.Danger;
                case IMUiTextRole.Warning: return theme.Warning;
                case IMUiTextRole.Gold: return theme.Gold;
                default: return theme.TextPrimary;
            }
        }

        public static bool TryCopyVanillaPanelVisual(Image target)
        {
            if (target == null) return false;
            Transform source;
            if (!VanillaUiSceneCatalog.TryFindPopupChild(
                    PopupManager._type.single_chart,
                    IMUiPresets.ChartPanelPath,
                    out source) || source == null)
            {
                return false;
            }
            Image sourceImage = source.GetComponent<Image>();
            if (sourceImage == null) return false;
            IMUiStyle.CopyImageVisual(sourceImage, target, false);
            return true;
        }

        private static string GetButtonResourcePath(IMUiButtonPreset preset)
        {
            switch (preset)
            {
                case IMUiButtonPreset.BasicOutline:
                    return VanillaUiPrefabCatalog.Button.basic_outline_Standard;
                case IMUiButtonPreset.BasicWithIcon:
                    return VanillaUiPrefabCatalog.Button.basic_with_icon_Standard;
                case IMUiButtonPreset.BasicOutlineWithIcon:
                    return VanillaUiPrefabCatalog.Button.basic_outline_with_icon_Standard;
                case IMUiButtonPreset.Rounded:
                    return VanillaUiPrefabCatalog.Button.rounded_Standard;
                case IMUiButtonPreset.RoundedOutline:
                    return VanillaUiPrefabCatalog.Button.rounded_outline_Standard;
                case IMUiButtonPreset.RadialIcon:
                    return VanillaUiPrefabCatalog.Button.radial_only_icon_Standard;
                default:
                    return VanillaUiPrefabCatalog.Button.basic_Standard;
            }
        }
    }
}
