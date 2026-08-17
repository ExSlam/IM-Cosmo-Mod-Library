using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Michsky.UI.ModernUIPack;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GraduationCalendar.EmbeddedIMUiFramework
{
    public enum VanillaMuipFontRole
    {
        Button,
        DropdownItem,
        Dropdown,
        HorizontalSelector,
        InputField,
        ModalTitle,
        ModalContent,
        NotificationTitle,
        NotificationDescription,
        ProgressBarLabel,
        SliderLabel,
        Toggle,
        Tooltip
    }

    /// <summary>
    /// Font bridge for both font systems used by Idol Manager: legacy UnityEngine.Font
    /// through Fonts/Font_Replacer, and TMP_FontAsset through MUIP/TMP.
    /// </summary>
    public static class VanillaUiFonts
    {
        private static Font cachedSelectedLegacyFont;
        private static TMP_FontAsset cachedSelectedTmpFont;
        private static bool ownsCachedSelectedTmpFont;

        public static TMP_FontAsset GetMuipFont(VanillaMuipFontRole role)
        {
            UIManager manager = VanillaUiResources.GetMuipManager();
            if (manager == null) return null;
            switch (role)
            {
                case VanillaMuipFontRole.Button: return manager.buttonFont;
                case VanillaMuipFontRole.DropdownItem: return manager.dropdownItemFont;
                case VanillaMuipFontRole.Dropdown: return manager.dropdownFont;
                case VanillaMuipFontRole.HorizontalSelector: return manager.selectorFont;
                case VanillaMuipFontRole.InputField: return manager.inputFieldFont;
                case VanillaMuipFontRole.ModalTitle: return manager.modalWindowTitleFont;
                case VanillaMuipFontRole.ModalContent: return manager.modalWindowContentFont;
                case VanillaMuipFontRole.NotificationTitle: return manager.notificationTitleFont;
                case VanillaMuipFontRole.NotificationDescription: return manager.notificationDescriptionFont;
                case VanillaMuipFontRole.ProgressBarLabel: return manager.progressBarLabelFont;
                case VanillaMuipFontRole.SliderLabel: return manager.sliderLabelFont;
                case VanillaMuipFontRole.Toggle: return manager.toggleFont;
                case VanillaMuipFontRole.Tooltip: return manager.tooltipFont;
                default: return manager.buttonFont;
            }
        }

        public static TMP_FontAsset GetLiberationSansSdf()
        {
            return Resources.Load<TMP_FontAsset>(VanillaUiResources.LiberationSansSdfPath);
        }

        public static bool TryGetGameFontsComponent(out Fonts fonts)
        {
            fonts = null;
            Camera camera = Camera.main;
            if (camera == null) return false;
            mainScript main = camera.GetComponent<mainScript>();
            if (main == null || main.Data == null) return false;
            fonts = main.Data.GetComponent<Fonts>();
            return fonts != null;
        }

        public static Font GetGameSelectedLegacyFont()
        {
            Fonts fonts;
            if (!TryGetGameFontsComponent(out fonts) || !fonts.IsReady()) return null;
            return fonts.GetFont();
        }

        public static TMP_FontAsset GetGameFontsTmpAsset()
        {
            Fonts fonts;
            if (!TryGetGameFontsComponent(out fonts)) return null;
            return fonts.FontAsset;
        }

        /// <summary>
        /// Resolves the TMP font that best represents Idol Manager's currently selected
        /// legacy game font. This is the correct default for mod-created TMP text because
        /// the base game's Fonts component can select a bundled font or an OS font at runtime.
        /// </summary>
        public static TMP_FontAsset GetGameSelectedTmpFont()
        {
            Font selected = GetGameSelectedLegacyFont();
            if (selected != null)
            {
                if (cachedSelectedLegacyFont == selected && cachedSelectedTmpFont != null)
                {
                    return cachedSelectedTmpFont;
                }

                TMP_FontAsset matching = FindLoadedTmpFont(selected.name);
                if (matching == null && selected.fontNames != null)
                {
                    for (int i = 0; i < selected.fontNames.Length; i++)
                    {
                        matching = FindLoadedTmpFont(selected.fontNames[i]);
                        if (matching != null) break;
                    }
                }

                if (matching != null)
                {
                    ReplaceSelectedTmpCache(selected, matching, false);
                    return matching;
                }

                TMP_FontAsset generated = CreateTmpFontAsset(selected);
                if (generated != null)
                {
                    ReplaceSelectedTmpCache(selected, generated, true);
                    return generated;
                }
            }

            TMP_FontAsset fallback = GetGameFontsTmpAsset();
            if (fallback == null) fallback = GetMuipFont(VanillaMuipFontRole.Dropdown);
            if (fallback == null) fallback = GetMuipFont(VanillaMuipFontRole.Button);
            if (fallback == null) fallback = GetLiberationSansSdf();
            ReplaceSelectedTmpCache(selected, fallback, false);
            return fallback;
        }

        /// <summary>
        /// Applies Idol Manager's selected font to all TMP and legacy UI text below a root.
        /// Legacy Text components optionally receive Font_Replacer so later game font changes
        /// keep following the same selection used by vanilla UI.
        /// </summary>
        public static void ApplyGameFont(GameObject root, bool addLegacyFontReplacer = true)
        {
            if (root == null) return;

            TMP_FontAsset tmpFont = GetGameSelectedTmpFont();
            TMP_Text[] tmpTexts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < tmpTexts.Length; i++)
            {
                if (tmpTexts[i] != null && tmpFont != null) tmpTexts[i].font = tmpFont;
            }

            Font legacyFont = GetGameSelectedLegacyFont();
            Text[] legacyTexts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < legacyTexts.Length; i++)
            {
                Text text = legacyTexts[i];
                if (text == null) continue;
                if (legacyFont != null) text.font = legacyFont;
                if (addLegacyFontReplacer) MakeLegacyTextFollowGameFont(text);
            }
        }

        public static void ApplyGameFont(TMP_Text text)
        {
            if (text == null) return;
            TMP_FontAsset font = GetGameSelectedTmpFont();
            if (font != null) text.font = font;
        }

        private static void ReplaceSelectedTmpCache(Font source, TMP_FontAsset font, bool ownsFont)
        {
            if (cachedSelectedLegacyFont == source && cachedSelectedTmpFont == font)
            {
                ownsCachedSelectedTmpFont = ownsCachedSelectedTmpFont || ownsFont;
                return;
            }

            if (ownsCachedSelectedTmpFont && cachedSelectedTmpFont != null)
            {
                try { UnityEngine.Object.Destroy(cachedSelectedTmpFont); } catch { }
            }

            cachedSelectedLegacyFont = source;
            cachedSelectedTmpFont = font;
            ownsCachedSelectedTmpFont = ownsFont;
        }

        public static IList<Font> GetGameBundledLegacyFonts()
        {
            Fonts fonts;
            if (!TryGetGameFontsComponent(out fonts) || fonts.FontFiles == null)
                return new List<Font>().AsReadOnly();
            return new List<Font>(fonts.FontFiles).AsReadOnly();
        }

        public static IList<TMP_FontAsset> GetLoadedTmpFonts()
        {
            TMP_FontAsset[] fonts = FindAllLoadedObjects<TMP_FontAsset>();
            return new List<TMP_FontAsset>(fonts ?? new TMP_FontAsset[0]).AsReadOnly();
        }

        public static IList<Font> GetLoadedLegacyFonts()
        {
            Font[] fonts = FindAllLoadedObjects<Font>();
            return new List<Font>(fonts ?? new Font[0]).AsReadOnly();
        }

        public static TMP_FontAsset FindLoadedTmpFont(string nameOrFamily)
        {
            if (string.IsNullOrEmpty(nameOrFamily)) return null;
            TMP_FontAsset[] fonts = FindAllLoadedObjects<TMP_FontAsset>();
            for (int i = 0; i < fonts.Length; i++)
            {
                TMP_FontAsset font = fonts[i];
                if (font == null) continue;
                if (ContainsInvariant(font.name, nameOrFamily)) return font;
                try
                {
                    if (ContainsInvariant(font.faceInfo.familyName, nameOrFamily)) return font;
                }
                catch { }
            }
            return null;
        }

        public static Font FindLoadedLegacyFont(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            Font[] fonts = FindAllLoadedObjects<Font>();
            for (int i = 0; i < fonts.Length; i++)
            {
                Font font = fonts[i];
                if (font == null) continue;
                if (ContainsInvariant(font.name, name)) return font;
                string[] names = font.fontNames;
                if (names == null) continue;
                for (int j = 0; j < names.Length; j++)
                {
                    if (ContainsInvariant(names[j], name)) return font;
                }
            }
            return null;
        }

        /// <summary>
        /// Mirrors the base game's Fonts.LoadFont behavior for OS fonts, while also accepting
        /// a file path when Unity's runtime supports the path as a dynamic-font source.
        /// </summary>
        public static Font LoadExternalOrOsLegacyFont(string filePathOrFontName, int size = 16)
        {
            if (string.IsNullOrEmpty(filePathOrFontName)) return null;
            int resolvedSize = Mathf.Max(1, size);

            Font loaded = FindLoadedLegacyFont(filePathOrFontName);
            if (loaded != null) return loaded;

            string candidate = filePathOrFontName;
            if (File.Exists(filePathOrFontName))
            {
                try
                {
                    loaded = Font.CreateDynamicFontFromOSFont(filePathOrFontName, resolvedSize);
                    if (loaded != null) return loaded;
                }
                catch { }
                candidate = Path.GetFileNameWithoutExtension(filePathOrFontName);
            }

            string[] installed;
            try { installed = Font.GetOSInstalledFontNames(); }
            catch { installed = new string[0]; }

            for (int i = 0; i < installed.Length; i++)
            {
                if (!string.Equals(installed[i], candidate, StringComparison.OrdinalIgnoreCase)) continue;
                try { return Font.CreateDynamicFontFromOSFont(installed[i], resolvedSize); }
                catch { return null; }
            }
            for (int i = 0; i < installed.Length; i++)
            {
                if (!ContainsInvariant(installed[i], candidate)) continue;
                try { return Font.CreateDynamicFontFromOSFont(installed[i], resolvedSize); }
                catch { return null; }
            }
            return null;
        }

        public static TMP_FontAsset CreateTmpFontAsset(Font sourceFont)
        {
            if (sourceFont == null) return null;
            try
            {
                MethodInfo[] methods = typeof(TMP_FontAsset).GetMethods(BindingFlags.Public | BindingFlags.Static);
                MethodInfo best = null;
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (!string.Equals(method.Name, "CreateFontAsset", StringComparison.Ordinal)) continue;
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 0 || parameters[0].ParameterType != typeof(Font)) continue;
                    bool supported = true;
                    for (int j = 1; j < parameters.Length; j++)
                    {
                        if (!parameters[j].HasDefaultValue && !parameters[j].ParameterType.IsValueType)
                        {
                            supported = false;
                            break;
                        }
                    }
                    if (!supported) continue;
                    if (best == null || parameters.Length < best.GetParameters().Length) best = method;
                }

                if (best != null)
                {
                    ParameterInfo[] parameters = best.GetParameters();
                    object[] args = new object[parameters.Length];
                    args[0] = sourceFont;
                    for (int i = 1; i < parameters.Length; i++)
                    {
                        args[i] = parameters[i].HasDefaultValue
                            ? parameters[i].DefaultValue
                            : Activator.CreateInstance(parameters[i].ParameterType);
                    }
                    TMP_FontAsset asset = best.Invoke(null, args) as TMP_FontAsset;
                    if (asset != null)
                    {
                        asset.name = "IMUiFramework Runtime TMP - " + sourceFont.name;
                        return asset;
                    }
                }
            }
            catch { }
            return null;
        }

        public static TMP_FontAsset LoadExternalOrOsTmpFont(string filePathOrFontName, int size = 32)
        {
            TMP_FontAsset existing = FindLoadedTmpFont(filePathOrFontName);
            if (existing != null) return existing;
            Font legacy = LoadExternalOrOsLegacyFont(filePathOrFontName, size);
            return CreateTmpFontAsset(legacy);
        }

        public static void Apply(TextMeshProUGUI text, TMP_FontAsset font)
        {
            if (text != null && font != null) text.font = font;
        }

        public static void Apply(TMP_Text text, TMP_FontAsset font)
        {
            if (text != null && font != null) text.font = font;
        }

        public static void Apply(Text text, Font font)
        {
            if (text != null && font != null) text.font = font;
        }

        public static Font_Replacer MakeLegacyTextFollowGameFont(Text text)
        {
            if (text == null) return null;
            Font selected = GetGameSelectedLegacyFont();
            if (selected != null) text.font = selected;
            Font_Replacer replacer = text.GetComponent<Font_Replacer>();
            if (replacer == null) replacer = text.gameObject.AddComponent<Font_Replacer>();
            return replacer;
        }

        private static T[] FindAllLoadedObjects<T>() where T : UnityEngine.Object
        {
            // Idol Manager ships an older Unity API surface where the generic
            // Resources.FindObjectsOfTypeAll<T>() overload is not available to the compiler.
            // Resolve the non-generic API by reflection so we still include inactive objects
            // and loaded assets when that runtime method exists.
            try
            {
                MethodInfo method = typeof(UnityEngine.Resources).GetMethod(
                    "FindObjectsOfTypeAll",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new Type[] { typeof(Type) },
                    null);
                if (method != null)
                {
                    Array values = method.Invoke(null, new object[] { typeof(T) }) as Array;
                    if (values != null)
                    {
                        List<T> matches = new List<T>(values.Length);
                        for (int i = 0; i < values.Length; i++)
                        {
                            T value = values.GetValue(i) as T;
                            if (value != null) matches.Add(value);
                        }
                        return matches.ToArray();
                    }
                }
            }
            catch { }

            // Conservative fallback for Unity versions where Resources lacks the API.
            // This can omit inactive/unreferenced assets, but never prevents font use.
            try
            {
                MethodInfo method = typeof(UnityEngine.Object).GetMethod(
                    "FindObjectsOfType",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new Type[] { typeof(Type) },
                    null);
                if (method != null)
                {
                    Array values = method.Invoke(null, new object[] { typeof(T) }) as Array;
                    if (values != null)
                    {
                        List<T> matches = new List<T>(values.Length);
                        for (int i = 0; i < values.Length; i++)
                        {
                            T value = values.GetValue(i) as T;
                            if (value != null) matches.Add(value);
                        }
                        return matches.ToArray();
                    }
                }
            }
            catch { }

            return new T[0];
        }

        private static bool ContainsInvariant(string value, string token)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(token)) return false;
            return value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
