using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace IdolCareerDiary
{
    internal static class ModLocalization
    {
        private const string LocalizationDirectoryName = "Localization";
        private const string EnglishFolderName = "en";
        private const string StringsFileName = "strings.txt";
        private const string PrimaryKeyPrefix = "c.";
        private const int MaxLanguageCodeLength = 64;
        private const int MaxLocalizationEntries = 4096;
        private const int MaxLineLength = 8192;
        private const int MaxKeyLength = 96;
        private const int MaxValueLength = 4096;
        private const string LanguageEnglish = "en";
        private const string LanguageJapanese = "ja";
        private const string LanguageChinese = "zh";
        private const string LanguageRussian = "ru";
        private const string LanguagePortuguese = "pt";
        private const string ScriptHans = "Hans";
        private const string ScriptHant = "Hant";
        private const string RegionBrazil = "BR";

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

                List<string> folderCandidates = BuildLanguageFolderCandidates(GetConfiguredLanguageCode());
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

        private static List<string> BuildLanguageFolderCandidates(string configuredLanguageCode)
        {
            List<string> folders = new List<string>();
            List<string> tags = BuildLanguageTagCandidates(configuredLanguageCode);
            for (int i = 0; i < tags.Count; i++)
            {
                AddFolderNameVariants(folders, tags[i]);
            }

            return folders;
        }

        private static List<string> BuildLanguageTagCandidates(string configuredLanguageCode)
        {
            List<string> tags = new List<string>();
            string canonical = ResolveConfiguredLanguageTag(configuredLanguageCode);
            if (string.IsNullOrEmpty(canonical))
            {
                return tags;
            }

            string language;
            string script;
            string region;
            ParseLanguageTag(canonical, out language, out script, out region);
            if (string.IsNullOrEmpty(language))
            {
                return tags;
            }

            if (string.Equals(language, LanguageChinese, StringComparison.OrdinalIgnoreCase))
            {
                string resolvedScript = !string.IsNullOrEmpty(script)
                    ? script
                    : InferChineseScript(region);

                AddLanguageTagCandidate(tags, BuildLanguageTag(language, resolvedScript, region));
                AddLanguageTagCandidate(tags, BuildLanguageTag(language, resolvedScript, string.Empty));
                AddLanguageTagCandidate(tags, BuildLanguageTag(language, string.Empty, region));
                AddLanguageTagCandidate(tags, language);
                return tags;
            }

            AddLanguageTagCandidate(tags, canonical);
            if (!string.IsNullOrEmpty(script) && !string.IsNullOrEmpty(region))
            {
                AddLanguageTagCandidate(tags, BuildLanguageTag(language, script, string.Empty));
                AddLanguageTagCandidate(tags, BuildLanguageTag(language, string.Empty, region));
            }
            else if (!string.IsNullOrEmpty(script))
            {
                AddLanguageTagCandidate(tags, BuildLanguageTag(language, script, string.Empty));
            }
            else if (!string.IsNullOrEmpty(region))
            {
                AddLanguageTagCandidate(tags, BuildLanguageTag(language, string.Empty, region));
            }

            AddLanguageTagCandidate(tags, language);
            return tags;
        }

        private static void AddLanguageTagCandidate(List<string> tags, string tag)
        {
            if (tags == null || string.IsNullOrEmpty(tag))
            {
                return;
            }

            string canonical = CanonicalizeLanguageTag(tag);
            if (string.IsNullOrEmpty(canonical))
            {
                return;
            }

            for (int i = 0; i < tags.Count; i++)
            {
                if (string.Equals(tags[i], canonical, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            tags.Add(canonical);
        }

        private static void AddFolderNameVariants(List<string> folders, string tag)
        {
            if (folders == null || string.IsNullOrEmpty(tag))
            {
                return;
            }

            string canonical = CanonicalizeLanguageTag(tag);
            if (string.IsNullOrEmpty(canonical))
            {
                return;
            }

            AddFolderCandidate(folders, canonical);
            AddFolderCandidate(folders, canonical.ToLowerInvariant());
            AddFolderCandidate(folders, canonical.Replace('-', '_'));
            AddFolderCandidate(folders, canonical.ToLowerInvariant().Replace('-', '_'));
            AddLegacyFolderAliases(folders, canonical);
        }

        private static void AddFolderCandidate(List<string> folders, string folder)
        {
            if (folders == null || string.IsNullOrEmpty(folder))
            {
                return;
            }

            for (int i = 0; i < folders.Count; i++)
            {
                if (string.Equals(folders[i], folder, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            folders.Add(folder);
        }

        private static void AddLegacyFolderAliases(List<string> folders, string canonicalTag)
        {
            if (folders == null || string.IsNullOrEmpty(canonicalTag))
            {
                return;
            }

            string language;
            string script;
            string region;
            ParseLanguageTag(canonicalTag, out language, out script, out region);

            if (string.Equals(language, LanguageEnglish, StringComparison.OrdinalIgnoreCase))
            {
                AddFolderCandidate(folders, "english");
                AddFolderCandidate(folders, "en-us");
                AddFolderCandidate(folders, "en-us-utf8");
                return;
            }

            if (string.Equals(language, LanguageJapanese, StringComparison.OrdinalIgnoreCase))
            {
                AddFolderCandidate(folders, "jp");
                AddFolderCandidate(folders, "japanese");
                return;
            }

            if (string.Equals(language, LanguageChinese, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(script, ScriptHans, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(script))
                {
                    AddFolderCandidate(folders, "schinese");
                    AddFolderCandidate(folders, "zhcn");
                    AddFolderCandidate(folders, "zh-cn");
                    AddFolderCandidate(folders, "zh_cn");
                }

                if (string.Equals(script, ScriptHant, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(script))
                {
                    AddFolderCandidate(folders, "tchinese");
                }

                AddFolderCandidate(folders, "cn");
                AddFolderCandidate(folders, "chinese");
                return;
            }

            if (string.Equals(language, LanguageRussian, StringComparison.OrdinalIgnoreCase))
            {
                AddFolderCandidate(folders, "russian");
                return;
            }

            if (string.Equals(language, LanguagePortuguese, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(region, RegionBrazil, StringComparison.OrdinalIgnoreCase))
                {
                    AddFolderCandidate(folders, "ptbr");
                    AddFolderCandidate(folders, "pt_br");
                    AddFolderCandidate(folders, "brazilian");
                }

                if (string.IsNullOrEmpty(region))
                {
                    AddFolderCandidate(folders, "portuguese");
                }
            }
        }

        private static string GetConfiguredLanguageCode()
        {
            try
            {
                if (staticVars.Settings != null && !string.IsNullOrEmpty(staticVars.Settings.Language))
                {
                    return staticVars.Settings.Language.Trim();
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string ResolveConfiguredLanguageTag(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return string.Empty;
            }

            string alias = ResolveKnownLanguageAlias(code);
            if (!string.IsNullOrEmpty(alias))
            {
                return alias;
            }

            return CanonicalizeLanguageTag(code);
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

            string normalized = code.Trim();
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

        private static string ResolveKnownLanguageAlias(string code)
        {
            string normalized = NormalizeLooseLanguageAliasKey(code);
            switch (normalized)
            {
                case "en":
                case "english":
                case "enus":
                case "enusutf8":
                    return "en";
                case "ja":
                case "jp":
                case "japanese":
                    return "ja";
                case "zh":
                case "chinese":
                    return "zh";
                case "zhcn":
                case "cn":
                case "schinese":
                    return "zh-Hans";
                case "zhtw":
                case "zhhk":
                case "zhmo":
                case "tchinese":
                    return "zh-Hant";
                case "ru":
                case "russian":
                    return "ru";
                case "pt":
                case "portuguese":
                    return "pt";
                case "ptbr":
                case "ptbrutf8":
                case "brazilian":
                    return "pt-BR";
                case "ko":
                case "korean":
                case "koreana":
                    return "ko";
                case "de":
                case "german":
                    return "de";
                case "fr":
                case "french":
                    return "fr";
                case "it":
                case "italian":
                    return "it";
                case "es":
                case "spanish":
                    return "es";
                case "es419":
                case "latam":
                case "spanishlatam":
                    return "es-419";
                case "pl":
                case "polish":
                    return "pl";
                case "tr":
                case "turkish":
                    return "tr";
                case "th":
                case "thai":
                    return "th";
                case "vi":
                case "vietnamese":
                    return "vi";
                case "uk":
                case "ukrainian":
                    return "uk";
                case "cs":
                case "czech":
                    return "cs";
                case "da":
                case "danish":
                    return "da";
                case "nl":
                case "dutch":
                    return "nl";
                case "fi":
                case "finnish":
                    return "fi";
                case "hu":
                case "hungarian":
                    return "hu";
                case "no":
                case "norwegian":
                    return "no";
                case "ro":
                case "romanian":
                    return "ro";
                case "sv":
                case "swedish":
                    return "sv";
                default:
                    return string.Empty;
            }
        }

        private static string NormalizeLooseLanguageAliasKey(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return string.Empty;
            }

            string trimmed = code.Trim().ToLowerInvariant();
            if (trimmed.Length == 0 || trimmed.Length > MaxLanguageCodeLength)
            {
                return string.Empty;
            }

            char[] buffer = new char[trimmed.Length];
            int count = 0;
            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                if (char.IsLetterOrDigit(c))
                {
                    buffer[count++] = c;
                }
            }

            return count == 0 ? string.Empty : new string(buffer, 0, count);
        }

        private static string CanonicalizeLanguageTag(string code)
        {
            string normalized = NormalizeLanguageCode(code);
            if (string.IsNullOrEmpty(normalized))
            {
                return string.Empty;
            }

            string collapsed = normalized.Replace('_', '-');
            string[] parts = collapsed.Split('-');
            if (parts.Length == 0)
            {
                return string.Empty;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0 || parts[i].Length > 8)
                {
                    return string.Empty;
                }

                for (int j = 0; j < parts[i].Length; j++)
                {
                    if (!char.IsLetterOrDigit(parts[i][j]))
                    {
                        return string.Empty;
                    }
                }

                if (i == 0)
                {
                    parts[i] = parts[i].ToLowerInvariant();
                    continue;
                }

                bool isScript = parts[i].Length == 4 && IsAllLetters(parts[i]);
                bool isRegion = (parts[i].Length == 2 && IsAllLetters(parts[i])) || (parts[i].Length == 3 && IsAllDigits(parts[i]));
                if (isScript)
                {
                    parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1).ToLowerInvariant();
                    continue;
                }

                if (isRegion)
                {
                    parts[i] = parts[i].ToUpperInvariant();
                    continue;
                }

                parts[i] = parts[i].ToLowerInvariant();
            }

            return string.Join("-", parts);
        }

        private static void ParseLanguageTag(string code, out string language, out string script, out string region)
        {
            language = string.Empty;
            script = string.Empty;
            region = string.Empty;

            string canonical = CanonicalizeLanguageTag(code);
            if (string.IsNullOrEmpty(canonical))
            {
                return;
            }

            string[] parts = canonical.Split('-');
            if (parts.Length == 0)
            {
                return;
            }

            language = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string part = parts[i];
                if (string.IsNullOrEmpty(script) && part.Length == 4 && IsAllLetters(part))
                {
                    script = char.ToUpperInvariant(part[0]) + part.Substring(1).ToLowerInvariant();
                    continue;
                }

                if (string.IsNullOrEmpty(region) && ((part.Length == 2 && IsAllLetters(part)) || (part.Length == 3 && IsAllDigits(part))))
                {
                    region = part.ToUpperInvariant();
                }
            }
        }

        private static string BuildLanguageTag(string language, string script, string region)
        {
            List<string> parts = new List<string>();
            if (!string.IsNullOrEmpty(language))
            {
                parts.Add(language);
            }

            if (!string.IsNullOrEmpty(script))
            {
                parts.Add(script);
            }

            if (!string.IsNullOrEmpty(region))
            {
                parts.Add(region);
            }

            if (parts.Count == 0)
            {
                return string.Empty;
            }

            return CanonicalizeLanguageTag(string.Join("-", parts.ToArray()));
        }

        private static string InferChineseScript(string region)
        {
            if (string.IsNullOrEmpty(region))
            {
                return string.Empty;
            }

            switch (region.ToUpperInvariant())
            {
                case "CN":
                case "SG":
                case "MY":
                    return ScriptHans;
                case "TW":
                case "HK":
                case "MO":
                    return ScriptHant;
                default:
                    return string.Empty;
            }
        }

        private static bool IsAllLetters(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsLetter(value[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsAllDigits(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsDigit(value[i]))
                {
                    return false;
                }
            }

            return true;
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
