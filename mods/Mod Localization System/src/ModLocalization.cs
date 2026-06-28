using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

namespace ModLocalizationSystem
{
    public sealed class ModLocalizer
    {
        private const string LocalizationDirectoryName = "Localization";
        private const string EnglishFolderName = "en";
        private const string StringsFileName = "strings.txt";
        private const int MaxLocalizationEntries = 4096;
        private const int MaxLineLength = 8192;
        private const int MaxKeyLength = 96;
        private const int MaxValueLength = 4096;

        private readonly string modDirectory;
        private readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private bool loaded;
        private string loadedLanguage = string.Empty;

        internal ModLocalizer(string modDirectory)
        {
            this.modDirectory = modDirectory ?? string.Empty;
        }

        public string Get(string key, string fallback)
        {
            EnsureLoaded();
            string value;
            // An explicit empty value is a valid translation.  This is needed
            // for languages whose natural word order omits one side of a
            // composed label, such as "Open <idol> profile".
            return !string.IsNullOrEmpty(key) && values.TryGetValue(key, out value)
                ? value
                : fallback;
        }

        public string GetRaw(string fallback)
        {
            return Get(fallback, fallback);
        }

        /// <summary>
        /// Resolves a language-specific copy of an asset relative to this mod.
        /// Localized copies live under Localization/&lt;language&gt;/ while keeping
        /// the asset's original relative path. Localization/en is required and
        /// is the fallback when the game-language copy does not exist.
        /// </summary>
        public string GetLocalizedAssetPath(string relativePath)
        {
            string normalizedRelativePath;
            if (!TryNormalizeAssetPath(relativePath, out normalizedRelativePath))
            {
                return string.Empty;
            }

            string englishPath = FindLocalizedAsset(normalizedRelativePath, EnglishFolderName);
            if (string.IsNullOrEmpty(englishPath))
            {
                // Requiring an English localized asset makes JSON localization
                // explicitly opt-in. A normal root JSON tree is not treated as
                // a localization pack and remains entirely game-owned.
                return string.Empty;
            }

            string localizedPath = FindLocalizedAsset(normalizedRelativePath, ModLocalization.GetEffectiveLanguage());
            return string.IsNullOrEmpty(localizedPath) ? englishPath : localizedPath;
        }

        internal void Reset()
        {
            values.Clear();
            loaded = false;
            loadedLanguage = string.Empty;
        }

        private string FindLocalizedAsset(string normalizedRelativePath, string language)
        {
            List<string> folders = BuildFolderCandidates(language);
            // strings.txt loads broad aliases first and exact tags last. Search
            // in reverse here so whole-file assets get the same exact-to-broad
            // language priority.
            for (int i = folders.Count - 1; i >= 0; i--)
            {
                string candidate = Path.Combine(modDirectory, LocalizationDirectoryName, folders[i], normalizedRelativePath);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return string.Empty;
        }

        private bool TryNormalizeAssetPath(string relativePath, out string normalizedRelativePath)
        {
            normalizedRelativePath = string.Empty;
            if (string.IsNullOrEmpty(modDirectory) || string.IsNullOrEmpty(relativePath))
            {
                return false;
            }

            try
            {
                string candidateRelativePath = relativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (candidateRelativePath.Length == 0 || Path.IsPathRooted(candidateRelativePath))
                {
                    return false;
                }

                string root = Path.GetFullPath(modDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                string candidate = Path.GetFullPath(Path.Combine(root, candidateRelativePath));
                if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                normalizedRelativePath = candidate.Substring(root.Length);
                return normalizedRelativePath.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private void EnsureLoaded()
        {
            string effectiveLanguage = ModLocalization.GetEffectiveLanguage();
            if (loaded && string.Equals(loadedLanguage, effectiveLanguage, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            values.Clear();
            loaded = true;
            loadedLanguage = effectiveLanguage ?? string.Empty;
            try
            {
                if (string.IsNullOrEmpty(modDirectory))
                {
                    return;
                }

                string localizationDirectory = Path.Combine(modDirectory, LocalizationDirectoryName);
                LoadFile(Path.Combine(localizationDirectory, EnglishFolderName, StringsFileName));

                List<string> folders = BuildFolderCandidates(effectiveLanguage);
                for (int i = 0; i < folders.Count; i++)
                {
                    if (!string.Equals(folders[i], EnglishFolderName, StringComparison.OrdinalIgnoreCase))
                    {
                        LoadFile(Path.Combine(localizationDirectory, folders[i], StringsFileName));
                    }
                }
            }
            catch
            {
            }
        }

        private void LoadFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch
            {
                return;
            }

            for (int i = 0; i < lines.Length && values.Count < MaxLocalizationEntries; i++)
            {
                string raw = lines[i];
                if (string.IsNullOrEmpty(raw) || raw.Length > MaxLineLength)
                {
                    continue;
                }

                string trimmed = raw.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("#") || trimmed.StartsWith(";") || trimmed.StartsWith("//"))
                {
                    continue;
                }

                int separator = raw.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                string key = raw.Substring(0, separator).Trim();
                if (key.Length == 0 || key.Length > MaxKeyLength)
                {
                    continue;
                }

                string value = raw.Substring(separator + 1)
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t")
                    .Replace('\0', ' ')
                    .Replace('<', '＜')
                    .Replace('>', '＞');
                values[key] = value.Length > MaxValueLength ? value.Substring(0, MaxValueLength) : value;
            }
        }

        private static List<string> BuildFolderCandidates(string language)
        {
            List<string> folders = new List<string>();
            string normalized = NormalizeLanguageTag(language);
            if (string.IsNullOrEmpty(normalized))
            {
                return folders;
            }

            int separator = normalized.IndexOfAny(new[] { '-', '_' });
            string primary = separator > 0 ? normalized.Substring(0, separator) : normalized;
            AddLegacyAliases(folders, normalized, primary);
            AddFolder(folders, primary);
            AddFolder(folders, normalized.Replace('-', '_'));
            AddFolder(folders, normalized);
            return folders;
        }

        private static void AddLegacyAliases(List<string> folders, string normalized, string primary)
        {
            switch (primary)
            {
                case "en":
                    AddFolder(folders, "english");
                    AddFolder(folders, "enus");
                    AddFolder(folders, "enusutf8");
                    break;
                case "ja":
                case "jp":
                    AddFolder(folders, "japanese");
                    if (primary == "ja")
                    {
                        AddFolder(folders, "jp");
                    }
                    else
                    {
                        AddFolder(folders, "ja");
                    }
                    break;
                case "zh":
                case "cn":
                    AddFolder(folders, "zh");
                    if (normalized.IndexOf("hant", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        normalized.EndsWith("-tw", StringComparison.OrdinalIgnoreCase) ||
                        normalized.EndsWith("-hk", StringComparison.OrdinalIgnoreCase))
                    {
                        AddFolder(folders, "zh-Hant");
                        AddFolder(folders, "tchinese");
                    }
                    else
                    {
                        if (primary == "cn")
                        {
                            AddFolder(folders, "zh-Hans");
                        }
                        AddFolder(folders, "schinese");
                        AddFolder(folders, "cn");
                    }
                    break;
                case "ru":
                    AddFolder(folders, "russian");
                    AddFolder(folders, "ru");
                    break;
                case "uk":
                case "ua":
                    AddFolder(folders, "ukrainian");
                    AddFolder(folders, "uk");
                    AddFolder(folders, "ua");
                    break;
                case "ptbr":
                    AddFolder(folders, "portuguese");
                    AddFolder(folders, "brazilian");
                    AddFolder(folders, "pt-BR");
                    break;
                case "pt":
                    AddFolder(folders, "portuguese");
                    if (normalized.Equals("pt-br", StringComparison.OrdinalIgnoreCase) ||
                        normalized.Equals("pt_br", StringComparison.OrdinalIgnoreCase))
                    {
                        AddFolder(folders, "brazilian");
                        AddFolder(folders, "ptbr");
                    }
                    break;
                case "ko":
                case "kr":
                    AddFolder(folders, "korean");
                    AddFolder(folders, "kr");
                    break;
                case "fr":
                    AddFolder(folders, "french");
                    AddFolder(folders, "fr");
                    break;
            }
        }

        private static void AddFolder(List<string> folders, string candidate)
        {
            if (string.IsNullOrEmpty(candidate))
            {
                return;
            }

            for (int i = 0; i < folders.Count; i++)
            {
                if (string.Equals(folders[i], candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            folders.Add(candidate);
        }

        private static string NormalizeLanguageTag(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string normalized = value.Trim();
            if (normalized.Length == 0 || normalized.Length > 64)
            {
                return string.Empty;
            }

            for (int i = 0; i < normalized.Length; i++)
            {
                char c = normalized[i];
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                {
                    return string.Empty;
                }
            }

            return normalized;
        }
    }

    public static class ModLocalization
    {
        private static readonly Dictionary<Assembly, ModLocalizer> Localizers = new Dictionary<Assembly, ModLocalizer>();
        private static readonly Dictionary<string, ModLocalizer> DirectoryLocalizers = new Dictionary<string, ModLocalizer>(StringComparer.OrdinalIgnoreCase);
        private static readonly object LocalizerLock = new object();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string Get(string key, string fallback)
        {
            return ForAssembly(Assembly.GetCallingAssembly()).Get(key, fallback);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetRaw(string fallback)
        {
            return ForAssembly(Assembly.GetCallingAssembly()).GetRaw(fallback);
        }

        /// <summary>
        /// Resolves an asset for the calling mod using Idol Manager's language,
        /// compatible language aliases, and the required English localized file.
        /// Returns an empty string when the asset did not opt into localization.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetLocalizedAssetPath(string relativePath)
        {
            return ForAssembly(Assembly.GetCallingAssembly()).GetLocalizedAssetPath(relativePath);
        }

        public static ModLocalizer ForAssembly(Assembly assembly)
        {
            if (assembly == null)
            {
                return new ModLocalizer(string.Empty);
            }

            lock (LocalizerLock)
            {
                ModLocalizer localizer;
                if (!Localizers.TryGetValue(assembly, out localizer))
                {
                    localizer = new ModLocalizer(GetAssemblyDirectory(assembly));
                    Localizers[assembly] = localizer;
                }

                return localizer;
            }
        }

        public static ModLocalizer ForDirectory(string modDirectory)
        {
            string normalizedDirectory = NormalizeDirectory(modDirectory);
            if (string.IsNullOrEmpty(normalizedDirectory))
            {
                return new ModLocalizer(string.Empty);
            }

            lock (LocalizerLock)
            {
                ModLocalizer localizer;
                if (!DirectoryLocalizers.TryGetValue(normalizedDirectory, out localizer))
                {
                    localizer = new ModLocalizer(normalizedDirectory);
                    DirectoryLocalizers[normalizedDirectory] = localizer;
                }

                return localizer;
            }
        }

        /// <summary>
        /// Returns Idol Manager's language currently used for mod localization.
        /// </summary>
        public static string GetEffectiveLanguageCode()
        {
            return GetEffectiveLanguage();
        }

        public static void ReloadAll()
        {
            lock (LocalizerLock)
            {
                foreach (ModLocalizer localizer in Localizers.Values)
                {
                    localizer.Reset();
                }

                foreach (ModLocalizer localizer in DirectoryLocalizers.Values)
                {
                    localizer.Reset();
                }
            }
        }

        internal static string GetEffectiveLanguage()
        {
            string gameLanguage;
            try
            {
                gameLanguage = staticVars.Settings == null ? string.Empty : staticVars.Settings.Language;
            }
            catch
            {
                gameLanguage = string.Empty;
            }

            // Font fallback setup must never invalidate an otherwise valid
            // game-language code. Type-loading failures can occur before the
            // fallback method's own body-level exception handler runs.
            try
            {
                EnsureKoreanTmpFallback(gameLanguage);
            }
            catch
            {
            }

            return gameLanguage ?? string.Empty;
        }

        private static string GetAssemblyDirectory(Assembly assembly)
        {
            try
            {
                string location = assembly.Location;
                string locationDirectory = string.IsNullOrEmpty(location) ? string.Empty : Path.GetDirectoryName(location) ?? string.Empty;
                if (!string.IsNullOrEmpty(locationDirectory))
                {
                    return locationDirectory;
                }
            }
            catch
            {
            }

            // Some mod loaders load Harmony assemblies from byte arrays and
            // leave Assembly.Location empty. Resolve the owning mod from the
            // game registry or local Mods directory in that case.
            string assemblyFileName;
            try
            {
                assemblyFileName = assembly.GetName().Name + ".dll";
            }
            catch
            {
                return string.Empty;
            }

            string registeredDirectory = FindRegisteredModDirectory(assemblyFileName);
            if (!string.IsNullOrEmpty(registeredDirectory))
            {
                return registeredDirectory;
            }

            return FindLocalModDirectory(assemblyFileName);
        }

        private static string FindRegisteredModDirectory(string assemblyFileName)
        {
            try
            {
                if (Mods._Mods == null)
                {
                    return string.Empty;
                }

                for (int i = 0; i < Mods._Mods.Count; i++)
                {
                    Mods._mod mod = Mods._Mods[i];
                    if (mod == null || string.IsNullOrEmpty(mod.Path))
                    {
                        continue;
                    }

                    if (File.Exists(Path.Combine(mod.Path, assemblyFileName)))
                    {
                        return mod.Path;
                    }
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string FindLocalModDirectory(string assemblyFileName)
        {
            try
            {
                string modsDirectory = Path.Combine(Application.persistentDataPath, "Mods");
                if (!Directory.Exists(modsDirectory))
                {
                    return string.Empty;
                }

                string[] directories = Directory.GetDirectories(modsDirectory);
                for (int i = 0; i < directories.Length; i++)
                {
                    if (File.Exists(Path.Combine(directories[i], assemblyFileName)))
                    {
                        return directories[i];
                    }
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string NormalizeDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void EnsureKoreanTmpFallback(string language)
        {
            string normalized = (language ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized != "kr" && normalized != "ko" &&
                !normalized.StartsWith("ko-", StringComparison.Ordinal) &&
                !normalized.StartsWith("ko_", StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                if (TMP_Settings.fallbackFontAssets == null || HasKoreanFallback())
                {
                    return;
                }

                string[] preferredFonts = { "Malgun Gothic", "NanumGothic", "Nanum Gothic" };
                string[] installedFonts = Font.GetOSInstalledFontNames();
                Font font = null;
                for (int preferred = 0; preferred < preferredFonts.Length && font == null; preferred++)
                {
                    for (int installed = 0; installed < installedFonts.Length; installed++)
                    {
                        if (string.Equals(preferredFonts[preferred], installedFonts[installed], StringComparison.OrdinalIgnoreCase))
                        {
                            font = Font.CreateDynamicFontFromOSFont(installedFonts[installed], 16);
                            break;
                        }
                    }
                }

                if (font == null)
                {
                    return;
                }

                MethodInfo method = typeof(TMP_FontAsset).GetMethod("CreateFontAsset", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Font) }, null);
                TMP_FontAsset asset = method == null ? null : method.Invoke(null, new object[] { font }) as TMP_FontAsset;
                if (asset != null && !TMP_Settings.fallbackFontAssets.Contains(asset))
                {
                    TMP_Settings.fallbackFontAssets.Add(asset);
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
                TMP_FontAsset asset = TMP_Settings.fallbackFontAssets[i];
                string name = asset == null ? string.Empty : asset.name;
                if (name.IndexOf("Malgun", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Nanum", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
