using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace UIRecoveryTools
{
    internal static class ModLocalization
    {
        private const string LocalizationDirectoryName = "Localization";
        private const string EnglishFolderName = "en";
        private const string StringsFileName = "strings.txt";
        private const string PrimaryKeyPrefix = "c.";
        private const int MaxLanguageCodeLength = 16;
        private const int MaxLocalizationEntries = 4096;
        private const int MaxLineLength = 8192;
        private const int MaxKeyLength = 96;
        private const int MaxValueLength = 4096;

        private static readonly Dictionary<string, string> Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static bool loaded;

        internal static string Get(string key, string fallback)
        {
            EnsureLoaded();
            string value;
            if (!string.IsNullOrEmpty(key) && Values.TryGetValue(key, out value) && !string.IsNullOrEmpty(value))
            {
                return value;
            }
            return fallback;
        }

        internal static string GetRaw(string fallback)
        {
            return Get(fallback, fallback);
        }

        private static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;
            try
            {
                string assemblyDir = GetAssemblyDirectory();
                if (string.IsNullOrEmpty(assemblyDir))
                {
                    return;
                }

                string localizationDir = Path.Combine(assemblyDir, LocalizationDirectoryName);

                // New convention baseline: Localization/en/strings.txt
                LoadFile(Path.Combine(localizationDir, EnglishFolderName, StringsFileName));

                // Backward-compatibility fallback for older packs: Localization/strings.txt
                LoadFile(Path.Combine(localizationDir, StringsFileName));

                List<string> languageCodes = BuildLanguageCodeCandidates();
                List<string> folderCandidates = BuildLanguageFolderCandidates(languageCodes);
                for (int i = 0; i < folderCandidates.Count; i++)
                {
                    string folder = folderCandidates[i];
                    if (string.Equals(folder, EnglishFolderName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    LoadFile(Path.Combine(localizationDir, folder, StringsFileName));
                }
            }
            catch
            {
            }
        }

        private static List<string> BuildLanguageCodeCandidates()
        {
            List<string> list = new List<string>();
            string configured = GetConfiguredLanguageCode();
            if (string.IsNullOrEmpty(configured))
            {
                return list;
            }

            AddCodeCandidate(list, configured);
            AddCodeCandidate(list, configured.Replace("-", string.Empty).Replace("_", string.Empty));

            int separatorIndex = configured.IndexOfAny(new[] { '-', '_' });
            if (separatorIndex > 0)
            {
                AddCodeCandidate(list, configured.Substring(0, separatorIndex));
            }

            string alias = GetAlias(configured);
            AddCodeCandidate(list, alias);
            return list;
        }

        private static void AddCodeCandidate(List<string> list, string code)
        {
            if (list == null || string.IsNullOrEmpty(code))
            {
                return;
            }

            string normalized = NormalizeLanguageCode(code);
            if (string.IsNullOrEmpty(normalized))
            {
                return;
            }

            if (!list.Contains(normalized))
            {
                list.Add(normalized);
            }
        }

        private static List<string> BuildLanguageFolderCandidates(List<string> languageCodes)
        {
            List<string> folders = new List<string>();
            if (languageCodes == null || languageCodes.Count == 0)
            {
                return folders;
            }

            for (int i = 0; i < languageCodes.Count; i++)
            {
                AddLanguageFolderVariants(folders, languageCodes[i]);
            }

            return folders;
        }

        private static void AddLanguageFolderVariants(List<string> folders, string code)
        {
            if (folders == null || string.IsNullOrEmpty(code))
            {
                return;
            }

            string normalized = NormalizeLanguageCode(code);
            if (string.IsNullOrEmpty(normalized))
            {
                return;
            }

            AddCodeCandidate(folders, normalized);

            string canonical = GetAlias(normalized);
            if (string.IsNullOrEmpty(canonical))
            {
                canonical = normalized;
            }

            AddCodeCandidate(folders, canonical);

            switch (canonical)
            {
                case "en":
                    AddCodeCandidate(folders, "english");
                    AddCodeCandidate(folders, "enus");
                    AddCodeCandidate(folders, "enusutf8");
                    return;
                case "jp":
                    AddCodeCandidate(folders, "ja");
                    AddCodeCandidate(folders, "japanese");
                    return;
                case "cn":
                    AddCodeCandidate(folders, "zh");
                    AddCodeCandidate(folders, "zhcn");
                    AddCodeCandidate(folders, "zh-cn");
                    AddCodeCandidate(folders, "zh_cn");
                    AddCodeCandidate(folders, "schinese");
                    AddCodeCandidate(folders, "tchinese");
                    AddCodeCandidate(folders, "chinese");
                    return;
                case "ru":
                    AddCodeCandidate(folders, "russian");
                    AddCodeCandidate(folders, "ua");
                    AddCodeCandidate(folders, "ukrainian");
                    return;
                case "ptbr":
                    AddCodeCandidate(folders, "pt");
                    AddCodeCandidate(folders, "pt-br");
                    AddCodeCandidate(folders, "pt_br");
                    AddCodeCandidate(folders, "portuguese");
                    AddCodeCandidate(folders, "brazilian");
                    return;
            }
        }

        private static string GetConfiguredLanguageCode()
        {
            try
            {
                if (staticVars.Settings != null && !string.IsNullOrEmpty(staticVars.Settings.Language))
                {
                    return staticVars.Settings.Language.Trim().ToLowerInvariant();
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string GetAlias(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return string.Empty;
            }

            switch (code)
            {
                case "english":
                case "en":
                case "enus":
                case "enusutf8":
                    return "en";
                case "japanese":
                case "ja":
                case "jp":
                    return "jp";
                case "schinese":
                case "tchinese":
                case "zh":
                case "zhcn":
                case "cn":
                    return "cn";
                case "russian":
                case "ukrainian":
                case "ru":
                case "ua":
                    return "ru";
                case "portuguese":
                case "brazilian":
                case "pt":
                case "ptbr":
                case "pt-br":
                case "pt_br":
                    return "ptbr";
                default:
                    return string.Empty;
            }
        }

        private static string GetAssemblyDirectory()
        {
            try
            {
                string location = Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrEmpty(location))
                {
                    return string.Empty;
                }

                string directory = Path.GetDirectoryName(location);
                return directory ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void LoadFile(string path)
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

            for (int i = 0; i < lines.Length; i++)
            {
                if (Values.Count >= MaxLocalizationEntries)
                {
                    return;
                }

                string raw = lines[i];
                if (string.IsNullOrEmpty(raw))
                {
                    continue;
                }

                if (raw.Length > MaxLineLength)
                {
                    continue;
                }

                string trimmed = raw.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("#") || trimmed.StartsWith(";") || trimmed.StartsWith("//"))
                {
                    continue;
                }

                int separatorIndex = raw.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                string key = NormalizeKey(raw.Substring(0, separatorIndex));
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                string value = raw.Substring(separatorIndex + 1);
                Values[key] = SanitizeValue(Unescape(value));
            }
        }

        private static string Unescape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t");
        }

        private static string NormalizeLanguageCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return string.Empty;
            }

            string normalized = code.Trim().ToLowerInvariant();
            if (normalized.Length == 0 || normalized.Length > MaxLanguageCodeLength)
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

        private static string NormalizeKey(string rawKey)
        {
            if (string.IsNullOrEmpty(rawKey))
            {
                return string.Empty;
            }

            string key = rawKey.Trim();
            if (key.Length < 3 || key.Length > MaxKeyLength || !key.StartsWith(PrimaryKeyPrefix, StringComparison.Ordinal))
            {
                return string.Empty;
            }

            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];
                bool allowed = char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-';
                if (!allowed)
                {
                    return string.Empty;
                }
            }

            return key;
        }

        private static string SanitizeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.Length > MaxValueLength)
            {
                value = value.Substring(0, MaxValueLength);
            }

            string cleaned = value
                .Replace('\0', ' ')
                .Replace('<', '＜')
                .Replace('>', '＞');

            char[] buffer = new char[cleaned.Length];
            int count = 0;
            for (int i = 0; i < cleaned.Length; i++)
            {
                char c = cleaned[i];
                if (char.IsControl(c) && c != '\n' && c != '\t')
                {
                    continue;
                }

                buffer[count++] = c;
            }

            return count == cleaned.Length ? cleaned : new string(buffer, 0, count);
        }
    }
}
