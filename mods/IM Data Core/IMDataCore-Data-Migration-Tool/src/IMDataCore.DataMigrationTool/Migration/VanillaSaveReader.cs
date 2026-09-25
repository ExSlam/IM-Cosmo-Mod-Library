using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace IMDataCore.DataMigrationTool.Migration;

internal static class VanillaSaveReader
{
    private const int SavePathHashLength = 16;
    private const int SavePathTokenLength = 32;
    private const int SaveKeyMaximumLength = 64;

    public static string DefaultDataDirectory()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, "AppData", "LocalLow", "Glitch Pitch", "Idol Manager", "data");
    }

    public static VanillaSaveInfo Read(string filePath) => ReadInternal(filePath, computeFingerprints: true);

    public static VanillaSaveInfo ReadForIdentification(string filePath) => ReadInternal(filePath, computeFingerprints: false);

    private static VanillaSaveInfo ReadInternal(string filePath, bool computeFingerprints)
    {
        string fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("Vanilla save was not found.", fullPath);
        byte[] raw = File.ReadAllBytes(fullPath);
        string text = Encoding.UTF8.GetString(raw);
        if (text.Length > 0 && text[0] == '\uFEFF') text = text[1..];
        JsonNode rootNode = JsonUtil.ParseNode(text, "Vanilla save");
        if (rootNode is not JsonObject root) throw new InvalidDataException("Vanilla save root must be an object.");
        if (root["staticVars__PlayerData"] is not JsonObject player)
            throw new InvalidDataException("Vanilla save does not contain staticVars__PlayerData.");

        string dataRoot = FindDataRoot(fullPath);
        string persistentRoot = Path.GetDirectoryName(dataRoot)!;
        string relative = Path.GetRelativePath(dataRoot, fullPath).Replace('\\', '/');
        ValidateSupportedRelativePath(relative);

        string rawHash = "";
        string v6Fingerprint = "";
        string oldFingerprint = "";
        if (computeFingerprints)
        {
            string compact = JsonUtil.StripInsignificantWhitespace(text);
            rawHash = HashUtil.Sha256Hex(raw);
            v6Fingerprint = "sha256:" + HashUtil.Sha256Hex(Encoding.UTF8.GetBytes(compact));
            oldFingerprint = $"v1:{raw.LongLength}:{rawHash}";
        }

        bool story = GetBool(player, "IsStoryMode");
        int chapter = GetInt32(player, "Chapter", 10);
        string saveFolderName = JsonUtil.GetString(player, "SaveFolderName");
        string slotToken = ResolveSlotToken(relative);
        string effectiveSaveFolderName = saveFolderName;
        if (string.IsNullOrWhiteSpace(effectiveSaveFolderName) && story)
            effectiveSaveFolderName = ResolveStorySaveFolderName(relative);
        string fileKey = BuildLegacyFileSaveKey(fullPath, dataRoot);
        string firstName = JsonUtil.GetString(player, "FirstName");
        string lastName = JsonUtil.GetString(player, "LastName");
        string groupName = JsonUtil.GetString(player, "GroupName");
        string agencyFallbackKey = BuildLegacyAgencyFallbackKey(story, firstName, lastName, groupName, chapter);
        string agencyKey = BuildLegacyAgencySaveKey(effectiveSaveFolderName, agencyFallbackKey);

        return new VanillaSaveInfo
        {
            FilePath = fullPath,
            PersistentRoot = persistentRoot,
            DataRoot = dataRoot,
            RelativeSavePath = relative,
            GameVersion = JsonUtil.GetString(root, "version"),
            SaveDisplayName = JsonUtil.GetString(player, "SaveFileName"),
            SaveFolderName = saveFolderName,
            PlayerFirstName = firstName,
            PlayerLastName = lastName,
            GroupName = groupName,
            IsStoryMode = story,
            ChapterName = ChapterName(chapter),
            DifficultyName = DifficultyName(GetInt32(player, "Difficulty", 1)),
            SaveKind = ResolveSaveKind(relative),
            SlotToken = slotToken,
            LastSave = JsonUtil.GetString(player, "LastSave"),
            PlaytimeSeconds = JsonUtil.GetInt64(player, "Playtime_Seconds"),
            GameDateTime = JsonUtil.GetString(root, "staticVars__dateTime"),
            IdolCount = CountArray(root, "data_girls__Girls"),
            StaffCount = CountArray(root, "staff__Staff"),
            SingleCount = CountArray(root, "singles__Singles"),
            ShowCount = CountArray(root, "shows__Shows"),
            IdolIds = ReadIdSet(root, "data_girls__Girls"),
            StaffIds = ReadIdSet(root, "staff__Staff"),
            SingleIds = ReadIdSet(root, "singles__Singles"),
            ShowIds = ReadIdSet(root, "shows__Shows"),
            LegacySavesRoot = Path.Combine(persistentRoot, "Mods", "IMDataCore", "saves"),
            LegacyFileSaveKeyCandidate = fileKey,
            LegacyAgencySaveKeyCandidate = agencyKey,
            LegacyAgencyFallbackKeyCandidate = agencyFallbackKey,
            V6ContentFingerprint = v6Fingerprint,
            LegacyFileFingerprint = oldFingerprint,
            RawSha256 = rawHash,
            RawLength = raw.LongLength
        };
    }

    public static List<LegacySourceCandidate> FindLegacySourceCandidates(VanillaSaveInfo info, string? rootOverride = null)
    {
        string root = string.IsNullOrWhiteSpace(rootOverride) ? info.LegacySavesRoot : Path.GetFullPath(rootOverride);
        var candidates = new List<LegacySourceCandidate>();
        if (!Directory.Exists(root)) return candidates;

        // Accept either the historical `...\Mods\IMDataCore\saves` root, the
        // `...\Mods\IMDataCore` directory itself, or one copied per-save folder.
        string nestedSaves = Path.Combine(root, "saves");
        if (Directory.Exists(nestedSaves) && !ContainsLegacyDataFile(root)) root = nestedSaves;

        AddCandidateDirectory(info, root, candidates);
        foreach (string dir in Directory.EnumerateDirectories(root)) AddCandidateDirectory(info, dir, candidates);

        return candidates
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.FolderKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.SourcePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddCandidateDirectory(VanillaSaveInfo info, string dir, List<LegacySourceCandidate> candidates)
    {
        string key = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        (int score, string reason) = ScoreLegacyFolder(info, key);
        if (score <= 0) return;

        foreach (string fileName in new[] { "im_data_core.db", "im_data_core.fallback.json" })
        {
            string path = Path.Combine(dir, fileName);
            if (!File.Exists(path)) continue;
            candidates.Add(new LegacySourceCandidate
            {
                SourcePath = path,
                FolderKey = key,
                MatchReason = reason,
                Score = score + (fileName.EndsWith(".db", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            });
        }
    }

    private static bool ContainsLegacyDataFile(string dir) =>
        File.Exists(Path.Combine(dir, "im_data_core.db")) ||
        File.Exists(Path.Combine(dir, "im_data_core.fallback.json"));

    private static (int score, string reason) ScoreLegacyFolder(VanillaSaveInfo info, string key)
    {
        if (!string.IsNullOrWhiteSpace(info.LegacyFileSaveKeyCandidate) &&
            string.Equals(key, info.LegacyFileSaveKeyCandidate, StringComparison.OrdinalIgnoreCase))
            return (100, "exact 1.x file-save key");

        if (!string.IsNullOrWhiteSpace(info.LegacyAgencySaveKeyCandidate) &&
            string.Equals(key, info.LegacyAgencySaveKeyCandidate, StringComparison.OrdinalIgnoreCase))
            return (95, "exact 1.x agency key");

        if (!string.IsNullOrWhiteSpace(info.LegacyAgencyFallbackKeyCandidate) &&
            string.Equals(key, info.LegacyAgencyFallbackKeyCandidate, StringComparison.OrdinalIgnoreCase))
            return (94, "exact 1.x fallback agency key");

        if (!string.IsNullOrWhiteSpace(info.SaveFolderName) &&
            string.Equals(key, SanitizeToken(info.SaveFolderName, 64), StringComparison.OrdinalIgnoreCase))
            return (93, "embedded SaveFolderName");

        if (!string.IsNullOrWhiteSpace(info.SlotToken) &&
            string.Equals(key, info.SlotToken, StringComparison.OrdinalIgnoreCase))
            return (90, "vanilla slot folder");

        if (!string.IsNullOrWhiteSpace(info.SlotToken) && key.Contains(info.SlotToken, StringComparison.OrdinalIgnoreCase))
            return (70, "folder contains vanilla slot token");

        return (0, "");
    }

    private static string FindDataRoot(string savePath)
    {
        DirectoryInfo? dir = new FileInfo(savePath).Directory;
        while (dir is not null)
        {
            if (string.Equals(dir.Name, "data", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(dir.Parent?.Name, "Idol Manager", StringComparison.OrdinalIgnoreCase))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidDataException("Safe migration requires selecting the raw save beneath ...\\Glitch Pitch\\Idol Manager\\data. Copy/export mode is intentionally separate from live-tree migration.");
    }

    private static void ValidateSupportedRelativePath(string relative)
    {
        string[] p = relative.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (p.Length == 0 || string.Equals(p[^1], "global_data.json", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("global_data.json is not an IMDataCore save scope.");
        bool direct = p.Length == 1 && (Eq(p[0], "auto_save.json") || Eq(p[0], "manual_save.json"));
        bool manual = p.Length == 3 && Eq(p[0], "manual_saves") && Safe(p[1]) && Eq(p[2], "save.json");
        bool storyDirect = p.Length == 3 && Eq(p[0], "story_mode") && Safe(p[1]) && (Eq(p[2], "auto_save.json") || Eq(p[2], "manual_save.json"));
        bool storyManual = p.Length == 5 && Eq(p[0], "story_mode") && Safe(p[1]) && Eq(p[2], "manual_saves") && Safe(p[3]) && Eq(p[4], "save.json");
        bool storyChapter = p.Length == 4 && Eq(p[0], "story_mode") && Safe(p[1]) && IsChapter(p[2]) && Eq(p[3], "save.json");
        if (!(direct || manual || storyDirect || storyManual || storyChapter))
            throw new InvalidDataException("The selected vanilla save path is not one of the save layouts accepted by current IMDataCore CorePaths.");
    }

    private static string ResolveSaveKind(string relative)
    {
        string[] p = relative.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (p.Length == 1 && Eq(p[0], "auto_save.json")) return "autosave";
        if (p.Length == 1 && Eq(p[0], "manual_save.json")) return "manual save";
        if (p.Length == 3 && Eq(p[0], "manual_saves")) return "manual slot";
        if (p.Length == 3 && Eq(p[0], "story_mode") && Eq(p[2], "auto_save.json")) return "story autosave";
        if (p.Length == 3 && Eq(p[0], "story_mode") && Eq(p[2], "manual_save.json")) return "story manual save";
        if (p.Length == 5 && Eq(p[2], "manual_saves")) return "story manual slot";
        if (p.Length == 4 && IsChapter(p[2])) return "story chapter snapshot";
        return "save";
    }

    private static string ResolveSlotToken(string relative)
    {
        string[] p = relative.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (p.Length == 3 && Eq(p[0], "manual_saves")) return p[1];
        if (p.Length == 5 && Eq(p[0], "story_mode") && Eq(p[2], "manual_saves")) return p[3];
        if (p.Length >= 3 && Eq(p[0], "story_mode")) return p[1];
        if (p.Length == 1) return Path.GetFileNameWithoutExtension(p[0]);
        return "";
    }

    private static string ResolveStorySaveFolderName(string relative)
    {
        string[] p = relative.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        return p.Length >= 2 && Eq(p[0], "story_mode") ? p[1] : "";
    }

    private static string BuildLegacyFileSaveKey(string fullPath, string dataRoot)
    {
        string normalizedLowerPath = Path.GetFullPath(fullPath).ToLowerInvariant();
        string normalizedDataRoot = Path.GetFullPath(dataRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string prefix = normalizedDataRoot.ToLowerInvariant() + Path.DirectorySeparatorChar;
        string relativePath = normalizedLowerPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? normalizedLowerPath[prefix.Length..]
            : normalizedLowerPath;
        string pathTokenSource = relativePath
            .Replace(Path.DirectorySeparatorChar, '_')
            .Replace(Path.AltDirectorySeparatorChar, '_');
        string pathToken = SanitizeToken(pathTokenSource, SavePathTokenLength);
        string hash = HashUtil.Sha256Hex(Encoding.UTF8.GetBytes(normalizedLowerPath));
        string pathHash = hash.Length <= SavePathHashLength ? hash : hash[..SavePathHashLength];
        return SanitizeToken("file_" + pathToken + "_" + pathHash, SaveKeyMaximumLength);
    }

    private static string BuildLegacyAgencySaveKey(string saveFolderName, string fallbackKey)
    {
        string folderKey = SanitizeToken(saveFolderName, 64);
        return !string.IsNullOrWhiteSpace(folderKey) ? folderKey : fallbackKey;
    }

    private static string BuildLegacyAgencyFallbackKey(bool story, string firstName, string lastName, string groupName, int chapter)
    {
        string fallback = string.Join("_", new[]
        {
            story ? "story" : "freeplay",
            firstName ?? "",
            lastName ?? "",
            groupName ?? "",
            ChapterName(chapter)
        });
        return SanitizeToken(fallback, SaveKeyMaximumLength);
    }

    private static string SanitizeToken(string rawValue, int maximumLength)
    {
        if (string.IsNullOrEmpty(rawValue)) return "";
        var output = new StringBuilder(Math.Min(rawValue.Length, maximumLength));
        foreach (char c in rawValue)
        {
            if (output.Length >= maximumLength) break;
            bool letter = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
            if (letter || (c >= '0' && c <= '9') || c is '_' or '-' or '.') output.Append(c);
        }
        return output.ToString();
    }

    private static string ChapterName(int value) => value switch
    {
        0 => "chapter_0",
        1 => "chapter_1",
        2 => "chapter_2",
        3 => "chapter_3",
        4 => "chapter_4",
        5 => "chapter_5",
        6 => "post_game",
        10 => "NONE",
        _ => value.ToString(System.Globalization.CultureInfo.InvariantCulture)
    };

    private static string DifficultyName(int value) => value switch
    {
        0 => "easy",
        1 => "normal",
        2 => "hard",
        _ => value.ToString(System.Globalization.CultureInfo.InvariantCulture)
    };

    private static int CountArray(JsonObject root, string name) => root[name] is JsonArray array ? array.Count : 0;
    private static HashSet<int> ReadIdSet(JsonObject root, string name)
    {
        var result = new HashSet<int>();
        if (root[name] is not JsonArray array) return result;
        foreach (JsonNode? node in array)
        {
            if (node is not JsonObject obj) continue;
            if (obj["id"] is JsonValue value && value.TryGetValue<int>(out int id)) result.Add(id);
        }
        return result;
    }
    private static bool GetBool(JsonObject obj, string name, bool fallback = false) =>
        obj[name] is JsonValue value && value.TryGetValue<bool>(out bool result) ? result : fallback;
    private static int GetInt32(JsonObject obj, string name, int fallback = 0) =>
        obj[name] is JsonValue value && value.TryGetValue<int>(out int result) ? result : fallback;
    private static bool Eq(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    private static bool Safe(string value) => !string.IsNullOrWhiteSpace(value) && value is not "." and not "..";
    private static bool IsChapter(string value) => value.StartsWith("chapter_", StringComparison.OrdinalIgnoreCase) && int.TryParse(value[8..], out int n) && n is >= 0 and <= 6;
}
