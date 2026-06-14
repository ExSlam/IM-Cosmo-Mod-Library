using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace CheatsMod
{
    internal static class ModLocalization
    {
        private const string LocalizationDirectoryName = "Localization";
        private const string EnglishFolderName = "en";
        private const string StringsFileName = "strings.txt";
        private const string EmptyText = "";
        private const string DashText = "-";
        private const string UnderscoreText = "_";
        private const string NewLineEscape = "\\n";
        private const string CarriageReturnEscape = "\\r";
        private const string TabEscape = "\\t";
        private const string CommentHashPrefix = "#";
        private const string CommentSemicolonPrefix = ";";
        private const string CommentSlashPrefix = "//";
        private const char KeyValueSeparator = '=';
        private const char DashCharacter = '-';
        private const char UnderscoreCharacter = '_';
        private const char PeriodCharacter = '.';
        private const char NullCharacter = '\0';
        private const char SafeSpaceCharacter = ' ';
        private const char UnsafeOpeningAngleCharacter = '<';
        private const char UnsafeClosingAngleCharacter = '>';
        private const char SafeOpeningAngleCharacter = '＜';
        private const char SafeClosingAngleCharacter = '＞';
        private const char NewLineCharacter = '\n';
        private const char TabCharacter = '\t';
        private const int MaxLanguageCodeLength = 16;
        private const int MaxLocalizationEntries = 4096;
        private const int MaxLineLength = 8192;
        private const int MinKeyLength = 1;
        private const int MaxKeyLength = 96;
        private const int MaxValueLength = 4096;

        private const string CodeEnglish = "en";
        private const string CodeEnglishName = "english";
        private const string CodeEnglishSteam = "enus";
        private const string CodeEnglishSteamUtf8 = "enusutf8";
        private const string CodeJapanese = "jp";
        private const string CodeJapaneseIso = "ja";
        private const string CodeJapaneseName = "japanese";
        private const string CodeChinese = "cn";
        private const string CodeChineseIso = "zh";
        private const string CodeChineseSimplified = "zhcn";
        private const string CodeChineseSimplifiedDash = "zh-cn";
        private const string CodeChineseSimplifiedUnderscore = "zh_cn";
        private const string CodeChineseSimplifiedSteam = "schinese";
        private const string CodeChineseTraditionalSteam = "tchinese";
        private const string CodeChineseName = "chinese";
        private const string CodeRussian = "ru";
        private const string CodeRussianName = "russian";
        private const string CodeUkrainian = "ua";
        private const string CodeUkrainianName = "ukrainian";
        private const string CodePortugueseBrazil = "ptbr";
        private const string CodePortuguese = "pt";
        private const string CodePortugueseBrazilDash = "pt-br";
        private const string CodePortugueseBrazilUnderscore = "pt_br";
        private const string CodePortugueseName = "portuguese";
        private const string CodeBrazilianName = "brazilian";

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
                LoadFile(Path.Combine(localizationDir, EnglishFolderName, StringsFileName));
                LoadFile(Path.Combine(localizationDir, StringsFileName));

                List<string> languageCodes = BuildLanguageCodeCandidates();
                List<string> folderCandidates = BuildLanguageFolderCandidates(languageCodes);
                for (int folderIndex = 0; folderIndex < folderCandidates.Count; folderIndex++)
                {
                    string folder = folderCandidates[folderIndex];
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
            AddCodeCandidate(list, configured.Replace(DashText, EmptyText).Replace(UnderscoreText, EmptyText));

            int separatorIndex = configured.IndexOfAny(new[] { DashCharacter, UnderscoreCharacter });
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

            for (int languageIndex = 0; languageIndex < languageCodes.Count; languageIndex++)
            {
                AddLanguageFolderVariants(folders, languageCodes[languageIndex]);
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
                case CodeEnglish:
                    AddCodeCandidate(folders, CodeEnglishName);
                    AddCodeCandidate(folders, CodeEnglishSteam);
                    AddCodeCandidate(folders, CodeEnglishSteamUtf8);
                    return;
                case CodeJapanese:
                    AddCodeCandidate(folders, CodeJapaneseIso);
                    AddCodeCandidate(folders, CodeJapaneseName);
                    return;
                case CodeChinese:
                    AddCodeCandidate(folders, CodeChineseIso);
                    AddCodeCandidate(folders, CodeChineseSimplified);
                    AddCodeCandidate(folders, CodeChineseSimplifiedDash);
                    AddCodeCandidate(folders, CodeChineseSimplifiedUnderscore);
                    AddCodeCandidate(folders, CodeChineseSimplifiedSteam);
                    AddCodeCandidate(folders, CodeChineseTraditionalSteam);
                    AddCodeCandidate(folders, CodeChineseName);
                    return;
                case CodeRussian:
                    AddCodeCandidate(folders, CodeRussianName);
                    AddCodeCandidate(folders, CodeUkrainian);
                    AddCodeCandidate(folders, CodeUkrainianName);
                    return;
                case CodePortugueseBrazil:
                    AddCodeCandidate(folders, CodePortuguese);
                    AddCodeCandidate(folders, CodePortugueseBrazilDash);
                    AddCodeCandidate(folders, CodePortugueseBrazilUnderscore);
                    AddCodeCandidate(folders, CodePortugueseName);
                    AddCodeCandidate(folders, CodeBrazilianName);
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

            return EmptyText;
        }

        private static string GetAlias(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return EmptyText;
            }

            switch (code)
            {
                case CodeEnglishName:
                case CodeEnglish:
                case CodeEnglishSteam:
                case CodeEnglishSteamUtf8:
                    return CodeEnglish;
                case CodeJapaneseName:
                case CodeJapaneseIso:
                case CodeJapanese:
                    return CodeJapanese;
                case CodeChineseSimplifiedSteam:
                case CodeChineseTraditionalSteam:
                case CodeChineseIso:
                case CodeChineseSimplified:
                case CodeChinese:
                    return CodeChinese;
                case CodeRussianName:
                case CodeUkrainianName:
                case CodeRussian:
                case CodeUkrainian:
                    return CodeRussian;
                case CodePortugueseName:
                case CodeBrazilianName:
                case CodePortuguese:
                case CodePortugueseBrazil:
                case CodePortugueseBrazilDash:
                case CodePortugueseBrazilUnderscore:
                    return CodePortugueseBrazil;
                default:
                    return EmptyText;
            }
        }

        private static string GetAssemblyDirectory()
        {
            try
            {
                string location = Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrEmpty(location))
                {
                    return EmptyText;
                }

                string directory = Path.GetDirectoryName(location);
                return directory ?? EmptyText;
            }
            catch
            {
                return EmptyText;
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

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                if (Values.Count >= MaxLocalizationEntries)
                {
                    return;
                }

                string raw = lines[lineIndex];
                if (string.IsNullOrEmpty(raw) || raw.Length > MaxLineLength)
                {
                    continue;
                }

                string trimmed = raw.Trim();
                if (trimmed.Length == 0
                    || trimmed.StartsWith(CommentHashPrefix)
                    || trimmed.StartsWith(CommentSemicolonPrefix)
                    || trimmed.StartsWith(CommentSlashPrefix))
                {
                    continue;
                }

                int separatorIndex = raw.IndexOf(KeyValueSeparator);
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
                return EmptyText;
            }

            return value
                .Replace(NewLineEscape, NewLineCharacter.ToString())
                .Replace(CarriageReturnEscape, EmptyText)
                .Replace(TabEscape, TabCharacter.ToString());
        }

        private static string NormalizeLanguageCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return EmptyText;
            }

            string normalized = code.Trim().ToLowerInvariant();
            if (normalized.Length == 0 || normalized.Length > MaxLanguageCodeLength)
            {
                return EmptyText;
            }

            for (int charIndex = 0; charIndex < normalized.Length; charIndex++)
            {
                char character = normalized[charIndex];
                if (!char.IsLetterOrDigit(character) && character != DashCharacter && character != UnderscoreCharacter)
                {
                    return EmptyText;
                }
            }

            return normalized;
        }

        private static string NormalizeKey(string rawKey)
        {
            if (string.IsNullOrEmpty(rawKey))
            {
                return EmptyText;
            }

            string key = rawKey.Trim();
            if (key.Length < MinKeyLength || key.Length > MaxKeyLength)
            {
                return EmptyText;
            }

            for (int charIndex = 0; charIndex < key.Length; charIndex++)
            {
                char character = key[charIndex];
                bool allowed = char.IsLetterOrDigit(character)
                    || character == PeriodCharacter
                    || character == UnderscoreCharacter
                    || character == DashCharacter;
                if (!allowed)
                {
                    return EmptyText;
                }
            }

            return key;
        }

        private static string SanitizeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return EmptyText;
            }

            if (value.Length > MaxValueLength)
            {
                value = value.Substring(0, MaxValueLength);
            }

            string cleaned = value
                .Replace(NullCharacter, SafeSpaceCharacter)
                .Replace(UnsafeOpeningAngleCharacter, SafeOpeningAngleCharacter)
                .Replace(UnsafeClosingAngleCharacter, SafeClosingAngleCharacter);

            char[] buffer = new char[cleaned.Length];
            int count = 0;
            for (int charIndex = 0; charIndex < cleaned.Length; charIndex++)
            {
                char character = cleaned[charIndex];
                if (char.IsControl(character) && character != NewLineCharacter && character != TabCharacter)
                {
                    continue;
                }

                buffer[count++] = character;
            }

            return count == cleaned.Length ? cleaned : new string(buffer, 0, count);
        }
    }
}
