using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IMUiFramework
{
    /// <summary>
    /// Every named Color32 exposed by Idol Manager's mainScript. These are semantic presets, not a
    /// requirement: custom UI can supply any Color/Color32 it wants.
    /// </summary>
    public enum IMUiVanillaColorPreset
    {
        White,
        Red,
        RedLight,
        Green,
        GreenLight,
        Blue,
        LightBlue,
        DarkBlue,
        Pink,
        GreyLight,
        BlackGold,
        Black,
        Gold,
        GoldDark,
        TabActive,
        TabInactive,
        PanelOuter,
        PanelInner,
        TrackGrey,
        ChartArrowHover,
        Transparent
    }

    public static class IMUiVanillaColors
    {
        public static Color32 Get(IMUiVanillaColorPreset preset)
        {
            switch (preset)
            {
                case IMUiVanillaColorPreset.White: return mainScript.white32;
                case IMUiVanillaColorPreset.Red: return mainScript.red32;
                case IMUiVanillaColorPreset.RedLight: return mainScript.red_light32;
                case IMUiVanillaColorPreset.Green: return mainScript.green32;
                case IMUiVanillaColorPreset.GreenLight: return mainScript.green_light32;
                case IMUiVanillaColorPreset.Blue: return mainScript.blue32;
                case IMUiVanillaColorPreset.LightBlue: return mainScript.lightBlue32;
                case IMUiVanillaColorPreset.DarkBlue: return mainScript.darkBlue32;
                case IMUiVanillaColorPreset.Pink: return mainScript.pink32;
                case IMUiVanillaColorPreset.GreyLight: return mainScript.grey_light32;
                case IMUiVanillaColorPreset.BlackGold: return mainScript.blackGold32;
                case IMUiVanillaColorPreset.Black: return mainScript.black32;
                case IMUiVanillaColorPreset.Gold: return mainScript.gold32;
                case IMUiVanillaColorPreset.GoldDark: return mainScript.gold_dark32;
                case IMUiVanillaColorPreset.TabActive: return mainScript.blue_tab_active;
                case IMUiVanillaColorPreset.TabInactive: return mainScript.blue_tab_not_active;
                case IMUiVanillaColorPreset.PanelOuter: return new Color32(235, 234, 233, 255);
                case IMUiVanillaColorPreset.PanelInner: return new Color32(254, 254, 254, 255);
                case IMUiVanillaColorPreset.TrackGrey: return new Color32(225, 222, 221, 255);
                case IMUiVanillaColorPreset.ChartArrowHover: return new Color32(163, 163, 209, 255);
                case IMUiVanillaColorPreset.Transparent: return mainScript.transparent32;
                default: return mainScript.white32;
            }
        }
    }

    public enum IMUiThemePreset
    {
        VanillaBlue,
        VanillaPink,
        VanillaGreen,
        VanillaGold,
        VanillaRed,
        VanillaDark
    }

    /// <summary>
    /// Semantic roles exposed by IMUiTheme. A mod can replace one role without rebuilding the rest
    /// of a vanilla-derived style.
    /// </summary>
    public enum IMUiColorRole
    {
        Accent,
        AccentHover,
        AccentPressed,
        AccentDisabled,
        AccentSoft,
        SurfaceOuter,
        SurfaceInner,
        SurfaceRaised,
        SurfaceMuted,
        Divider,
        ScrollTrack,
        TextPrimary,
        TextSecondary,
        TextMuted,
        TextOnAccent,
        Title,
        Success,
        Danger,
        Warning,
        Gold
    }

    /// <summary>
    /// Semantic color/typography palette for custom UI. Unlike VanillaUiThemeSettings, which mirrors
    /// Michsky's UIManager fields, this class describes the roles a mod actually thinks in: accent,
    /// popup surface, card surface, body text, muted text, dividers, scroll tracks, success, warning, etc.
    /// Clone a preset and change any role without losing vanilla sprites or geometry.
    /// </summary>
    public sealed class IMUiTheme
    {
        public Color32 Accent;
        public Color32 AccentHover;
        public Color32 AccentPressed;
        public Color32 AccentDisabled;
        public Color32 AccentSoft;

        public Color32 SurfaceOuter;
        public Color32 SurfaceInner;
        public Color32 SurfaceRaised;
        public Color32 SurfaceMuted;
        public Color32 Divider;
        public Color32 ScrollTrack;

        public Color32 TextPrimary;
        public Color32 TextSecondary;
        public Color32 TextMuted;
        public Color32 TextOnAccent;
        public Color32 Title;

        public Color32 Success;
        public Color32 Danger;
        public Color32 Warning;
        public Color32 Gold;

        public TMP_FontAsset BodyFont;
        public TMP_FontAsset TitleFont;
        public Font LegacyFont;

        public static IMUiTheme Vanilla()
        {
            IMUiTheme theme = new IMUiTheme();
            theme.SurfaceOuter = new Color32(235, 234, 233, 255);
            theme.SurfaceInner = new Color32(254, 254, 254, 255);
            theme.SurfaceRaised = mainScript.white32;
            theme.SurfaceMuted = new Color32(225, 222, 221, 255);
            theme.Divider = new Color32(225, 222, 221, 255);
            theme.ScrollTrack = new Color32(225, 222, 221, 255);

            theme.TextPrimary = mainScript.black32;
            theme.TextSecondary = mainScript.lightBlue32;
            theme.TextMuted = mainScript.grey_light32;
            theme.TextOnAccent = mainScript.white32;
            theme.Title = mainScript.lightBlue32;

            theme.Success = mainScript.green32;
            theme.Danger = mainScript.red32;
            theme.Warning = mainScript.gold32;
            theme.Gold = mainScript.gold32;

            theme.WithAccent(mainScript.blue32);
            // Exact ColorBlock values serialized on Single Chart's Prev Month / Next Month buttons.
            // Keeping these values in the default preset means "Vanilla" is a literal preset rather
            // than a mathematically approximated blue theme.
            theme.AccentHover = new Color32(163, 163, 209, 255);
            theme.AccentPressed = new Color32(163, 163, 209, 255);
            theme.AccentDisabled = new Color32(119, 123, 186, 128);

            theme.BodyFont = VanillaUiFonts.GetGameSelectedTmpFont();
            theme.TitleFont = theme.BodyFont;
            theme.LegacyFont = VanillaUiFonts.GetGameSelectedLegacyFont();
            return theme;
        }

        public static IMUiTheme FromAccent(Color32 accent)
        {
            return Vanilla().WithAccent(accent);
        }

        public static IMUiTheme FromAccent(IMUiVanillaColorPreset preset)
        {
            return Vanilla().WithAccent(preset);
        }

        public static IMUiTheme FromPreset(IMUiThemePreset preset)
        {
            IMUiTheme theme = Vanilla();
            switch (preset)
            {
                case IMUiThemePreset.VanillaPink:
                    return theme.WithAccent(mainScript.pink32);
                case IMUiThemePreset.VanillaGreen:
                    return theme.WithAccent(mainScript.green32);
                case IMUiThemePreset.VanillaGold:
                    return theme.WithAccent(mainScript.gold32);
                case IMUiThemePreset.VanillaRed:
                    return theme.WithAccent(mainScript.red32);
                case IMUiThemePreset.VanillaDark:
                    theme.SurfaceOuter = mainScript.darkBlue32;
                    theme.SurfaceInner = new Color32(63, 70, 74, 255);
                    theme.SurfaceRaised = new Color32(72, 79, 83, 255);
                    theme.SurfaceMuted = new Color32(86, 92, 96, 255);
                    theme.Divider = new Color32(96, 102, 106, 255);
                    theme.ScrollTrack = new Color32(96, 102, 106, 255);
                    theme.TextPrimary = mainScript.white32;
                    theme.TextSecondary = mainScript.lightBlue32;
                    theme.TextMuted = new Color32(190, 190, 190, 255);
                    return theme.WithAccent(mainScript.blue32);
                default:
                    return theme;
            }
        }

        public IMUiTheme Clone()
        {
            return (IMUiTheme)MemberwiseClone();
        }

        public IMUiTheme WithAccent(Color32 accent)
        {
            Accent = accent;
            // Same interaction shape as the game's chart arrows: a noticeable light hover/press,
            // half-alpha disabled state, and a pale accent wash for selected/card backgrounds.
            AccentHover = Lerp(accent, mainScript.white32, 0.32f);
            AccentPressed = Lerp(accent, mainScript.white32, 0.32f);
            AccentDisabled = WithAlpha(accent, 128);
            AccentSoft = Lerp(accent, mainScript.white32, 0.70f);
            return this;
        }

        public IMUiTheme WithAccent(IMUiVanillaColorPreset preset)
        {
            return WithAccent(IMUiVanillaColors.Get(preset));
        }

        public Color32 GetColor(IMUiColorRole role)
        {
            switch (role)
            {
                case IMUiColorRole.Accent: return Accent;
                case IMUiColorRole.AccentHover: return AccentHover;
                case IMUiColorRole.AccentPressed: return AccentPressed;
                case IMUiColorRole.AccentDisabled: return AccentDisabled;
                case IMUiColorRole.AccentSoft: return AccentSoft;
                case IMUiColorRole.SurfaceOuter: return SurfaceOuter;
                case IMUiColorRole.SurfaceInner: return SurfaceInner;
                case IMUiColorRole.SurfaceRaised: return SurfaceRaised;
                case IMUiColorRole.SurfaceMuted: return SurfaceMuted;
                case IMUiColorRole.Divider: return Divider;
                case IMUiColorRole.ScrollTrack: return ScrollTrack;
                case IMUiColorRole.TextPrimary: return TextPrimary;
                case IMUiColorRole.TextSecondary: return TextSecondary;
                case IMUiColorRole.TextMuted: return TextMuted;
                case IMUiColorRole.TextOnAccent: return TextOnAccent;
                case IMUiColorRole.Title: return Title;
                case IMUiColorRole.Success: return Success;
                case IMUiColorRole.Danger: return Danger;
                case IMUiColorRole.Warning: return Warning;
                case IMUiColorRole.Gold: return Gold;
                default: return TextPrimary;
            }
        }

        public IMUiTheme SetColor(IMUiColorRole role, Color32 color)
        {
            switch (role)
            {
                case IMUiColorRole.Accent: return WithAccent(color);
                case IMUiColorRole.AccentHover: AccentHover = color; break;
                case IMUiColorRole.AccentPressed: AccentPressed = color; break;
                case IMUiColorRole.AccentDisabled: AccentDisabled = color; break;
                case IMUiColorRole.AccentSoft: AccentSoft = color; break;
                case IMUiColorRole.SurfaceOuter: SurfaceOuter = color; break;
                case IMUiColorRole.SurfaceInner: SurfaceInner = color; break;
                case IMUiColorRole.SurfaceRaised: SurfaceRaised = color; break;
                case IMUiColorRole.SurfaceMuted: SurfaceMuted = color; break;
                case IMUiColorRole.Divider: Divider = color; break;
                case IMUiColorRole.ScrollTrack: ScrollTrack = color; break;
                case IMUiColorRole.TextPrimary: TextPrimary = color; break;
                case IMUiColorRole.TextSecondary: TextSecondary = color; break;
                case IMUiColorRole.TextMuted: TextMuted = color; break;
                case IMUiColorRole.TextOnAccent: TextOnAccent = color; break;
                case IMUiColorRole.Title: Title = color; break;
                case IMUiColorRole.Success: Success = color; break;
                case IMUiColorRole.Danger: Danger = color; break;
                case IMUiColorRole.Warning: Warning = color; break;
                case IMUiColorRole.Gold: Gold = color; break;
            }
            return this;
        }

        public IMUiTheme SetColor(IMUiColorRole role, IMUiVanillaColorPreset preset)
        {
            return SetColor(role, IMUiVanillaColors.Get(preset));
        }

        public IMUiTheme WithSurfaces(Color32 outer, Color32 inner, Color32 raised, Color32 muted)
        {
            SurfaceOuter = outer;
            SurfaceInner = inner;
            SurfaceRaised = raised;
            SurfaceMuted = muted;
            return this;
        }

        public IMUiTheme WithText(Color32 primary, Color32 secondary, Color32 muted, Color32 onAccent)
        {
            TextPrimary = primary;
            TextSecondary = secondary;
            TextMuted = muted;
            TextOnAccent = onAccent;
            return this;
        }

        private static Color32 Lerp(Color32 a, Color32 b, float t)
        {
            return (Color32)Color.Lerp((Color)a, (Color)b, Mathf.Clamp01(t));
        }

        private static Color32 WithAlpha(Color32 color, byte alpha)
        {
            color.a = alpha;
            return color;
        }
    }

    public enum IMUiThemeApplication
    {
        None = 0,
        AccentOnly = 1,
        Interactive = 2,
        Full = 3
    }

    public sealed class IMUiStyleOptions
    {
        public IMUiTheme Theme;
        public IMUiThemeApplication ThemeApplication = IMUiThemeApplication.None;
        public bool ApplyGameFont = true;
        public bool PreserveIconColors = true;
        public float Width = -1f;
        public float Height = -1f;
        public float FontSize = -1f;
    }

    /// <summary>
    /// Role-aware theming for arbitrary vanilla clones and Resource controls. It intentionally does
    /// not blanket-tint every Image, which would destroy portraits/icons. Instead it understands
    /// Selectable target graphics, slider handles/fills, input text/carets, named structural surfaces,
    /// and normal body text.
    /// </summary>
    public static class IMUiStyle
    {
        public static void Apply(GameObject root, IMUiStyleOptions options)
        {
            if (root == null || options == null)
            {
                return;
            }

            if (options.ApplyGameFont)
            {
                VanillaUiFonts.ApplyGameFont(root, true);
            }

            RectTransform rect = root.GetComponent<RectTransform>();
            if (rect != null && (options.Width >= 0f || options.Height >= 0f))
            {
                Vector2 size = rect.sizeDelta;
                if (options.Width >= 0f) size.x = options.Width;
                if (options.Height >= 0f) size.y = options.Height;
                rect.sizeDelta = size;

                LayoutElement layout = root.GetComponent<LayoutElement>();
                if (layout != null)
                {
                    if (options.Width >= 0f) layout.preferredWidth = options.Width;
                    if (options.Height >= 0f) layout.preferredHeight = options.Height;
                }
            }

            if (options.FontSize > 0f)
            {
                TextMeshProUGUI[] tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int i = 0; i < tmps.Length; i++) tmps[i].fontSize = options.FontSize;
                Text[] texts = root.GetComponentsInChildren<Text>(true);
                for (int i = 0; i < texts.Length; i++) texts[i].fontSize = Mathf.RoundToInt(options.FontSize);
            }

            if (options.Theme == null || options.ThemeApplication == IMUiThemeApplication.None)
            {
                return;
            }

            ApplyTheme(root, options.Theme, options.ThemeApplication, options.PreserveIconColors);
        }

        public static void ApplyTheme(GameObject root, IMUiTheme theme, IMUiThemeApplication application, bool preserveIconColors)
        {
            if (root == null || theme == null || application == IMUiThemeApplication.None)
            {
                return;
            }

            Selectable[] selectables = root.GetComponentsInChildren<Selectable>(true);
            for (int i = 0; i < selectables.Length; i++)
            {
                ApplySelectable(selectables[i], theme, application);
            }

            Slider[] sliders = root.GetComponentsInChildren<Slider>(true);
            for (int i = 0; i < sliders.Length; i++) ApplySlider(sliders[i], theme);

            Scrollbar[] scrollbars = root.GetComponentsInChildren<Scrollbar>(true);
            for (int i = 0; i < scrollbars.Length; i++) ApplyScrollbar(scrollbars[i], theme);

            Toggle[] toggles = root.GetComponentsInChildren<Toggle>(true);
            for (int i = 0; i < toggles.Length; i++) ApplyToggle(toggles[i], theme);

            TMP_InputField[] tmpInputs = root.GetComponentsInChildren<TMP_InputField>(true);
            for (int i = 0; i < tmpInputs.Length; i++) ApplyTmpInput(tmpInputs[i], theme);

            InputField[] inputs = root.GetComponentsInChildren<InputField>(true);
            for (int i = 0; i < inputs.Length; i++) ApplyLegacyInput(inputs[i], theme);

            if (application == IMUiThemeApplication.Full)
            {
                ApplyStructuralSurfaces(root, theme, preserveIconColors);
                ApplyBodyText(root, theme);
            }
        }

        public static void ApplySurface(GameObject target, IMUiTheme theme, bool raised)
        {
            if (target == null || theme == null) return;
            Image image = target.GetComponent<Image>();
            if (image == null) image = target.AddComponent<Image>();
            image.color = raised ? theme.SurfaceRaised : theme.SurfaceInner;
        }

        public static void CopyImageVisual(Image source, Image target, bool copyColor)
        {
            if (source == null || target == null) return;
            target.sprite = source.sprite;
            target.material = source.material;
            target.type = source.type;
            target.preserveAspect = source.preserveAspect;
            target.fillCenter = source.fillCenter;
            target.fillMethod = source.fillMethod;
            target.fillAmount = source.fillAmount;
            target.fillClockwise = source.fillClockwise;
            target.fillOrigin = source.fillOrigin;
            target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            if (copyColor) target.color = source.color;
        }

        private static void ApplySelectable(Selectable selectable, IMUiTheme theme, IMUiThemeApplication application)
        {
            if (selectable == null) return;

            Graphic target = selectable.targetGraphic;
            bool textTarget = target is TMP_Text || target is Text;

            if (selectable is TMP_InputField || selectable is InputField || selectable is TMP_Dropdown || selectable is Dropdown)
            {
                Image fieldImage = target as Image;
                if (fieldImage != null) fieldImage.color = theme.SurfaceRaised;
                ColorBlock fieldColors = selectable.colors;
                fieldColors.normalColor = mainScript.white32;
                fieldColors.highlightedColor = new Color32(248, 248, 248, 255);
                fieldColors.pressedColor = new Color32(238, 238, 238, 255);
                fieldColors.selectedColor = new Color32(248, 248, 248, 255);
                fieldColors.disabledColor = new Color32(255, 255, 255, 128);
                selectable.colors = fieldColors;
                TextMeshProUGUI[] fieldTmps = selectable.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int i = 0; i < fieldTmps.Length; i++) fieldTmps[i].color = theme.TextPrimary;
                Text[] fieldTexts = selectable.GetComponentsInChildren<Text>(true);
                for (int i = 0; i < fieldTexts.Length; i++) fieldTexts[i].color = theme.TextPrimary;
                return;
            }

            if (textTarget)
            {
                ColorBlock colors = selectable.colors;
                colors.normalColor = theme.Accent;
                colors.highlightedColor = theme.AccentHover;
                colors.pressedColor = theme.AccentPressed;
                colors.selectedColor = theme.AccentHover;
                colors.disabledColor = theme.AccentDisabled;
                selectable.colors = colors;
                target.color = mainScript.white32;
                return;
            }

            Image image = target as Image;
            if (image != null)
            {
                image.color = theme.Accent;
                ColorBlock colors = selectable.colors;
                colors.normalColor = mainScript.white32;
                colors.highlightedColor = new Color32(245, 245, 245, 255);
                colors.pressedColor = new Color32(230, 230, 230, 255);
                colors.selectedColor = new Color32(245, 245, 245, 255);
                colors.disabledColor = new Color32(255, 255, 255, 128);
                selectable.colors = colors;
            }

            if (application >= IMUiThemeApplication.Interactive)
            {
                TextMeshProUGUI[] tmps = selectable.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int i = 0; i < tmps.Length; i++)
                {
                    if (tmps[i] != target) tmps[i].color = theme.TextOnAccent;
                }
                Text[] texts = selectable.GetComponentsInChildren<Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i] != target) texts[i].color = theme.TextOnAccent;
                }
            }
        }

        private static Sprite neutralScrollbarHandleSprite;
        private static Image.Type neutralScrollbarHandleImageType = Image.Type.Sliced;
        private static bool neutralScrollbarHandleResolved;

        private static void ApplySlider(Slider slider, IMUiTheme theme)
        {
            if (slider == null) return;
            if (slider.handleRect != null)
            {
                Image handle = slider.handleRect.GetComponent<Image>();
                if (handle != null)
                {
                    // Producer Contracts/Salaries/Loans use a purple baked-color circular sprite with
                    // Image.color = white. Multiplying a pink/green/custom tint into that texture gives
                    // muddy colors. When a mod explicitly themes that scene-native slider, retain its
                    // exact 11.14-unit geometry but swap in the game's neutral rounded scrollbar sprite.
                    // With no custom theme the composer does not call this method, so vanilla remains
                    // pixel-for-pixel scene-native.
                    if (LooksLikeProducerListHandle(slider, handle))
                    {
                        ResolveNeutralScrollbarHandle();
                        if (neutralScrollbarHandleSprite != null)
                        {
                            handle.sprite = neutralScrollbarHandleSprite;
                            handle.type = neutralScrollbarHandleImageType;
                        }
                    }
                    handle.color = theme.Accent;
                }
            }
            if (slider.fillRect != null)
            {
                Image fill = slider.fillRect.GetComponent<Image>();
                if (fill != null) fill.color = theme.Accent;
            }
            Transform background = FindChildInsensitive(slider.transform, "Background");
            if (background != null)
            {
                Image image = background.GetComponent<Image>();
                if (image != null) image.color = theme.ScrollTrack;
            }
        }

        private static bool LooksLikeProducerListHandle(Slider slider, Image handle)
        {
            if (slider == null || handle == null || slider.handleRect == null) return false;
            RectTransform rect = slider.GetComponent<RectTransform>();
            RectTransform handleRect = slider.handleRect;
            if (rect == null || handleRect == null) return false;
            string parentName = handleRect.parent != null ? handleRect.parent.name : string.Empty;
            return string.Equals(parentName, "Handle Slide Area", StringComparison.OrdinalIgnoreCase)
                && Mathf.Abs(rect.sizeDelta.x - VanillaUiSceneTemplates.ProducerListSliderWidth) < 1.5f
                && Mathf.Abs(handleRect.sizeDelta.y - VanillaUiSceneTemplates.ProducerListHandleDiameter) < 1.5f;
        }

        private static void ResolveNeutralScrollbarHandle()
        {
            if (neutralScrollbarHandleResolved) return;
            neutralScrollbarHandleResolved = true;
            GameObject prefab = VanillaUiResources.LoadPrefab(VanillaUiPrefabCatalog.Scrollbar.Standard);
            if (prefab == null) return;
            Scrollbar scrollbar = prefab.GetComponent<Scrollbar>();
            if (scrollbar == null || scrollbar.handleRect == null) return;
            Image image = scrollbar.handleRect.GetComponent<Image>();
            if (image == null) return;
            neutralScrollbarHandleSprite = image.sprite;
            neutralScrollbarHandleImageType = image.type;
        }

        private static void ApplyScrollbar(Scrollbar scrollbar, IMUiTheme theme)
        {
            if (scrollbar == null) return;
            if (scrollbar.handleRect != null)
            {
                Image handle = scrollbar.handleRect.GetComponent<Image>();
                if (handle != null) handle.color = theme.Accent;
            }
            Transform background = FindChildInsensitive(scrollbar.transform, "Background");
            if (background != null)
            {
                Image image = background.GetComponent<Image>();
                if (image != null) image.color = theme.ScrollTrack;
            }
        }

        private static void ApplyToggle(Toggle toggle, IMUiTheme theme)
        {
            if (toggle == null) return;
            if (toggle.graphic != null) toggle.graphic.color = theme.Accent;
            Image background = toggle.targetGraphic as Image;
            if (background != null) background.color = theme.SurfaceRaised;
        }

        private static void ApplyTmpInput(TMP_InputField input, IMUiTheme theme)
        {
            if (input == null) return;
            if (input.textComponent != null) input.textComponent.color = theme.TextPrimary;
            Image background = input.targetGraphic as Image;
            if (background != null) background.color = theme.SurfaceRaised;
            Graphic placeholder = input.placeholder;
            if (placeholder != null) placeholder.color = theme.TextMuted;
            input.caretColor = theme.Accent;
            Color selection = theme.Accent;
            selection.a = 0.35f;
            input.selectionColor = selection;
        }

        private static void ApplyLegacyInput(InputField input, IMUiTheme theme)
        {
            if (input == null) return;
            if (input.textComponent != null) input.textComponent.color = theme.TextPrimary;
            Image background = input.targetGraphic as Image;
            if (background != null) background.color = theme.SurfaceRaised;
            Graphic placeholder = input.placeholder;
            if (placeholder != null) placeholder.color = theme.TextMuted;
            input.caretColor = theme.Accent;
            Color selection = theme.Accent;
            selection.a = 0.35f;
            input.selectionColor = selection;
        }

        private static void ApplyStructuralSurfaces(GameObject root, IMUiTheme theme, bool preserveIconColors)
        {
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null) continue;
                string name = image.gameObject.name == null ? string.Empty : image.gameObject.name.ToLowerInvariant();
                if (name.Contains("background") || name == "panel" || name.Contains("surface"))
                {
                    image.color = theme.SurfaceInner;
                }
                else if (name.Contains("divider") || name.Contains("separator") || name.Contains("line"))
                {
                    image.color = theme.Divider;
                }
                else if (name.Contains("fill") && image.GetComponentInParent<Slider>() == null)
                {
                    image.color = theme.Accent;
                }
                else if (!preserveIconColors && (name.Contains("icon") || name.Contains("arrow")))
                {
                    image.color = theme.Accent;
                }
            }
        }

        private static void ApplyBodyText(GameObject root, IMUiTheme theme)
        {
            TextMeshProUGUI[] tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < tmps.Length; i++)
            {
                TextMeshProUGUI tmp = tmps[i];
                if (tmp == null || IsSelectableTarget(tmp)) continue;
                string name = tmp.gameObject.name == null ? string.Empty : tmp.gameObject.name.ToLowerInvariant();
                tmp.color = name.Contains("title") || name.Contains("header") ? theme.Title : theme.TextPrimary;
            }
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null || IsSelectableTarget(text)) continue;
                string name = text.gameObject.name == null ? string.Empty : text.gameObject.name.ToLowerInvariant();
                text.color = name.Contains("title") || name.Contains("header") ? theme.Title : theme.TextPrimary;
            }
        }

        private static bool IsSelectableTarget(Graphic graphic)
        {
            if (graphic == null) return false;
            Selectable selectable = graphic.GetComponent<Selectable>();
            if (selectable != null && selectable.targetGraphic == graphic) return true;
            Selectable parent = graphic.GetComponentInParent<Selectable>();
            return parent != null && parent.targetGraphic == graphic;
        }

        private static Transform FindChildInsensitive(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (string.Equals(transforms[i].name, name, StringComparison.OrdinalIgnoreCase)) return transforms[i];
            }
            return null;
        }
    }

    /// <summary>
    /// Converts the semantic v3 theme into Michsky UIManager settings so Resource controls keep the
    /// requested colors even when their own manager components refresh on enable.
    /// </summary>
    public static class IMUiMuipThemeBridge
    {
        public static VanillaUiThemeSettings CreateSettings(IMUiTheme theme)
        {
            VanillaUiThemeSettings settings = VanillaUiThemeSettings.FromVanilla();
            if (settings == null || theme == null) return settings;
            Apply(theme, settings);
            return settings;
        }

        public static void Apply(IMUiTheme theme, VanillaUiThemeSettings settings)
        {
            if (theme == null || settings == null) return;

            settings.animatedIconColor = theme.Accent;
            settings.contextBackgroundColor = theme.SurfaceRaised;

            settings.buttonBorderColor = theme.Accent;
            settings.buttonFilledColor = theme.Accent;
            settings.buttonTextBasicColor = theme.TextPrimary;
            settings.buttonTextColor = theme.TextOnAccent;
            settings.buttonTextHighlightedColor = theme.TextOnAccent;
            settings.buttonIconBasicColor = theme.Accent;
            settings.buttonIconColor = theme.TextOnAccent;
            settings.buttonIconHighlightedColor = theme.TextOnAccent;

            settings.dropdownColor = theme.SurfaceRaised;
            settings.dropdownTextColor = theme.TextPrimary;
            settings.dropdownIconColor = theme.Accent;
            settings.dropdownItemColor = theme.SurfaceInner;
            settings.dropdownItemTextColor = theme.TextPrimary;
            settings.dropdownItemIconColor = theme.Accent;

            settings.selectorColor = theme.TextPrimary;
            settings.selectorHighlightedColor = theme.Accent;
            settings.inputFieldColor = theme.SurfaceRaised;

            settings.modalWindowTitleColor = theme.Title;
            settings.modalWindowDescriptionColor = theme.TextPrimary;
            settings.modalWindowIconColor = theme.Accent;
            settings.modalWindowBackgroundColor = theme.SurfaceOuter;
            settings.modalWindowContentPanelColor = theme.SurfaceInner;

            settings.notificationBackgroundColor = theme.SurfaceOuter;
            settings.notificationTitleColor = theme.Title;
            settings.notificationDescriptionColor = theme.TextPrimary;
            settings.notificationIconColor = theme.Accent;

            settings.progressBarColor = theme.Accent;
            settings.progressBarBackgroundColor = theme.SurfaceMuted;
            settings.progressBarLoopBackgroundColor = theme.SurfaceMuted;
            settings.progressBarLabelColor = theme.TextPrimary;

            settings.scrollbarColor = theme.Accent;
            settings.scrollbarBackgroundColor = theme.ScrollTrack;

            settings.sliderColor = theme.Accent;
            settings.sliderBackgroundColor = theme.SurfaceMuted;
            settings.sliderLabelColor = theme.TextPrimary;
            settings.sliderPopupLabelColor = theme.TextPrimary;
            settings.sliderHandleColor = theme.Accent;

            settings.switchBorderColor = theme.Accent;
            settings.switchBackgroundColor = theme.SurfaceMuted;
            settings.switchHandleOnColor = theme.Accent;
            settings.switchHandleOffColor = theme.TextMuted;

            settings.toggleTextColor = theme.TextPrimary;
            settings.toggleBorderColor = theme.Accent;
            settings.toggleBackgroundColor = theme.SurfaceRaised;
            settings.toggleCheckColor = theme.Accent;

            settings.tooltipTextColor = theme.TextPrimary;
            settings.tooltipBackgroundColor = theme.SurfaceRaised;

            if (theme.BodyFont != null)
            {
                settings.buttonFont = theme.BodyFont;
                settings.dropdownItemFont = theme.BodyFont;
                settings.dropdownFont = theme.BodyFont;
                settings.selectorFont = theme.BodyFont;
                settings.inputFieldFont = theme.BodyFont;
                settings.modalWindowContentFont = theme.BodyFont;
                settings.notificationDescriptionFont = theme.BodyFont;
                settings.progressBarLabelFont = theme.BodyFont;
                settings.sliderLabelFont = theme.BodyFont;
                settings.toggleFont = theme.BodyFont;
                settings.tooltipFont = theme.BodyFont;
            }
            if (theme.TitleFont != null)
            {
                settings.modalWindowTitleFont = theme.TitleFont;
                settings.notificationTitleFont = theme.TitleFont;
            }
        }
    }

}
