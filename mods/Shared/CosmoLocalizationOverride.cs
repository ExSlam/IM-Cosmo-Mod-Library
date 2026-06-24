using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace CosmoModLibrary
{
    // Deliberately compiled into every localized Cosmo mod. This keeps the choice
    // available even when Mod Buttons is not installed.
    internal static class CosmoLocalizationOverride
    {
        private const string ConfigDirectoryName = "Cosmo Mod Library";
        private const string ConfigFileName = "localization.ini";
        private const string ConfigKey = "language";
        private const string FollowGameLanguage = "game";
        private const string KoreanLanguage = "kr";
        private const string FrenchLanguage = "fr";

        internal static string GetLanguageOrFallback(string gameLanguage)
        {
            string selected = GetSelectedLanguage();
            if (string.Equals(selected, KoreanLanguage, StringComparison.Ordinal))
            {
                EnsureKoreanTmpFallback();
            }

            return string.IsNullOrEmpty(selected) ? gameLanguage ?? string.Empty : selected;
        }

        internal static string GetSelectedLanguage()
        {
            try
            {
                string path = GetConfigPath();
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    return string.Empty;
                }

                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string raw = lines[i];
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        continue;
                    }

                    string line = raw.Trim();
                    if (line.StartsWith("#") || line.StartsWith(";") || line.StartsWith("//"))
                    {
                        continue;
                    }

                    int separator = line.IndexOf('=');
                    if (separator <= 0)
                    {
                        continue;
                    }

                    string key = line.Substring(0, separator).Trim();
                    if (!string.Equals(key, ConfigKey, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    return NormalizeLanguage(line.Substring(separator + 1));
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        internal static bool SetSelectedLanguage(string language)
        {
            string normalized = NormalizeLanguage(language);
            bool followGame = IsFollowGameLanguage(language);
            if (string.IsNullOrEmpty(normalized) && !followGame)
            {
                return false;
            }

            if (followGame)
            {
                normalized = FollowGameLanguage;
            }

            try
            {
                string path = GetConfigPath();
                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }

                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllLines(path, new[]
                {
                    "# Cosmo Mod Library localization override.",
                    "# Use game to follow Idol Manager, kr for Korean, or fr for French.",
                    ConfigKey + "=" + normalized
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string GetConfigPath()
        {
            try
            {
                return Path.Combine(
                    Application.persistentDataPath,
                    "Mods",
                    ConfigDirectoryName,
                    ConfigFileName);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string NormalizeLanguage(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string normalized = value.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case FollowGameLanguage:
                case "default":
                case "follow-game":
                    return string.Empty;
                case KoreanLanguage:
                case "ko":
                case "korean":
                    return KoreanLanguage;
                case FrenchLanguage:
                case "french":
                case "francais":
                case "français":
                    return FrenchLanguage;
                default:
                    return string.Empty;
            }
        }

        private static bool IsFollowGameLanguage(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            string normalized = value.Trim().ToLowerInvariant();
            return normalized == FollowGameLanguage ||
                normalized == "default" ||
                normalized == "follow-game";
        }

        // Idol Manager does not ship a Korean language pack, so selecting the
        // mod-only Korean pack must not replace the game's active font. When a
        // common Korean Windows font is available, add a runtime TMP fallback
        // for text created by the mods. Failure is intentionally non-fatal.
        private static void EnsureKoreanTmpFallback()
        {
            try
            {
                if (TMP_Settings.fallbackFontAssets == null || HasKoreanFallback())
                {
                    return;
                }

                Font font = GetInstalledKoreanFont();
                if (font == null)
                {
                    return;
                }

                TMP_FontAsset fontAsset = CreateTmpFontAsset(font);
                if (fontAsset != null && !TMP_Settings.fallbackFontAssets.Contains(fontAsset))
                {
                    TMP_Settings.fallbackFontAssets.Add(fontAsset);
                }
            }
            catch
            {
            }
        }

        private static bool HasKoreanFallback()
        {
            for (int i = 0; i < TMP_Settings.fallbackFontAssets.Count; i++)
            {
                TMP_FontAsset font = TMP_Settings.fallbackFontAssets[i];
                if (font == null || string.IsNullOrEmpty(font.name))
                {
                    continue;
                }

                string family = font.name;
                if (family.IndexOf("Malgun", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    family.IndexOf("Nanum", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static Font GetInstalledKoreanFont()
        {
            string[] preferredFonts = { "Malgun Gothic", "NanumGothic", "Nanum Gothic" };
            string[] installedFonts = Font.GetOSInstalledFontNames();
            for (int preferredIndex = 0; preferredIndex < preferredFonts.Length; preferredIndex++)
            {
                for (int installedIndex = 0; installedIndex < installedFonts.Length; installedIndex++)
                {
                    if (string.Equals(preferredFonts[preferredIndex], installedFonts[installedIndex], StringComparison.OrdinalIgnoreCase))
                    {
                        return Font.CreateDynamicFontFromOSFont(installedFonts[installedIndex], 16);
                    }
                }
            }

            return null;
        }

        private static TMP_FontAsset CreateTmpFontAsset(Font font)
        {
            MethodInfo[] methods = typeof(TMP_FontAsset).GetMethods(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (!string.Equals(method.Name, "CreateFontAsset", StringComparison.Ordinal))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(Font))
                {
                    continue;
                }

                return method.Invoke(null, new object[] { font }) as TMP_FontAsset;
            }

            return null;
        }
    }
}
