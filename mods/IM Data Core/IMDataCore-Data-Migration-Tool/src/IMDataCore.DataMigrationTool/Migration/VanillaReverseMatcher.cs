namespace IMDataCore.DataMigrationTool.Migration;

internal static class VanillaReverseMatcher
{
    public static ReverseMatchScanResult Scan(LegacyInspection inspection, string? dataRootOverride = null, string? selectedSaveKey = null)
    {
        string dataRoot = ResolveDataRoot(dataRootOverride);
        IReadOnlyList<VanillaSaveInfo> saves = LoadRecognizedSaves(dataRoot, out int examined);
        return MatchCached(inspection, dataRoot, selectedSaveKey, saves, examined);
    }

    public static IReadOnlyList<VanillaSaveInfo> LoadRecognizedSaves(string? dataRootOverride, out int examined)
    {
        string dataRoot = ResolveDataRoot(dataRootOverride);
        return LoadRecognizedSavesResolved(dataRoot, out examined);
    }

    public static ReverseMatchScanResult MatchCached(
        LegacyInspection inspection,
        string dataRoot,
        string? selectedSaveKey,
        IReadOnlyList<VanillaSaveInfo> saves,
        int jsonFilesExamined)
    {
        var keys = new List<string>();
        if (!string.IsNullOrWhiteSpace(selectedSaveKey))
            keys.Add(selectedSaveKey);
        else
            keys.AddRange(inspection.SaveKeys.Where(x => !string.IsNullOrWhiteSpace(x)));

        string sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(inspection.SourcePath)) ?? string.Empty;
        string sourceFolderKey = Path.GetFileName(sourceDirectory) ?? string.Empty;
        var candidates = new List<VanillaSaveCandidate>();

        foreach (VanillaSaveInfo info in saves)
        {
            VanillaSaveCandidate? best = null;
            foreach (string key in keys)
            {
                VanillaSaveCandidate? candidate = ScoreKey(info, key);
                if (candidate is not null && (best is null || candidate.Score > best.Score)) best = candidate;
            }

            VanillaSaveCandidate? folderCandidate = ScoreSourceFolder(info, sourceFolderKey);
            if (folderCandidate is not null && (best is null || folderCandidate.Score > best.Score)) best = folderCandidate;
            if (best is not null) candidates.Add(best);
        }

        return new ReverseMatchScanResult
        {
            DataRoot = dataRoot,
            JsonFilesExamined = jsonFilesExamined,
            ValidSavesRead = saves.Count,
            Candidates = candidates
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.SaveInfo.LastSave, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.SaveInfo.RelativeSavePath, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private static string ResolveDataRoot(string? dataRootOverride)
    {
        string dataRoot = string.IsNullOrWhiteSpace(dataRootOverride)
            ? VanillaSaveReader.DefaultDataDirectory()
            : Path.GetFullPath(dataRootOverride);
        if (!Directory.Exists(dataRoot))
            throw new DirectoryNotFoundException("Vanilla data directory was not found: " + dataRoot);
        return dataRoot;
    }

    private static IReadOnlyList<VanillaSaveInfo> LoadRecognizedSavesResolved(string dataRoot, out int examined)
    {
        examined = 0;
        var saves = new List<VanillaSaveInfo>();
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(dataRoot, "*.json", SearchOption.AllDirectories).ToArray();
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            throw new IOException("Could not enumerate the vanilla data directory: " + ex.Message, ex);
        }

        foreach (string file in files)
        {
            examined++;
            try
            {
                saves.Add(VanillaSaveReader.ReadForIdentification(file));
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is IOException || ex is UnauthorizedAccessException || ex is System.Text.Json.JsonException)
            {
            }
        }
        return saves;
    }

    private static VanillaSaveCandidate? ScoreKey(VanillaSaveInfo info, string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        if (string.Equals(key, info.LegacyFileSaveKeyCandidate, StringComparison.OrdinalIgnoreCase))
            return Candidate(info, key, "exact_file_key", 120);
        if (string.Equals(key, info.LegacyAgencySaveKeyCandidate, StringComparison.OrdinalIgnoreCase))
            return Candidate(info, key, "exact_agency_key", 110);
        if (string.Equals(key, info.LegacyAgencyFallbackKeyCandidate, StringComparison.OrdinalIgnoreCase))
            return Candidate(info, key, "exact_agency_fallback_key", 108);
        if (FileKeyPathTokenMatches(key, info.LegacyFileSaveKeyCandidate))
            return Candidate(info, key, "file_path_token", 80);
        return null;
    }

    private static VanillaSaveCandidate? ScoreSourceFolder(VanillaSaveInfo info, string sourceFolderKey)
    {
        if (string.IsNullOrWhiteSpace(sourceFolderKey)) return null;
        if (string.Equals(sourceFolderKey, info.LegacyFileSaveKeyCandidate, StringComparison.OrdinalIgnoreCase))
            return Candidate(info, sourceFolderKey, "source_folder_file_key", 100);
        if (string.Equals(sourceFolderKey, info.LegacyAgencySaveKeyCandidate, StringComparison.OrdinalIgnoreCase))
            return Candidate(info, sourceFolderKey, "source_folder_agency_key", 95);
        if (string.Equals(sourceFolderKey, info.LegacyAgencyFallbackKeyCandidate, StringComparison.OrdinalIgnoreCase))
            return Candidate(info, sourceFolderKey, "source_folder_agency_fallback_key", 93);
        if (!string.IsNullOrWhiteSpace(info.SlotToken) && string.Equals(sourceFolderKey, info.SlotToken, StringComparison.OrdinalIgnoreCase))
            return Candidate(info, sourceFolderKey, "source_folder_slot", 60);
        return null;
    }

    private static VanillaSaveCandidate Candidate(VanillaSaveInfo info, string key, string kind, int score) => new()
    {
        SaveInfo = info,
        MatchedSaveKey = key,
        MatchKind = kind,
        Score = score
    };

    private static bool FileKeyPathTokenMatches(string historicalKey, string currentKey)
    {
        if (!TryGetFileKeyPathPrefix(historicalKey, out string oldPrefix) ||
            !TryGetFileKeyPathPrefix(currentKey, out string currentPrefix)) return false;
        return string.Equals(oldPrefix, currentPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetFileKeyPathPrefix(string key, out string prefix)
    {
        prefix = "";
        if (!key.StartsWith("file_", StringComparison.OrdinalIgnoreCase)) return false;
        int split = key.LastIndexOf('_');
        if (split <= 5 || split >= key.Length - 1) return false;
        string hash = key[(split + 1)..];
        if (hash.Length != 16 || hash.Any(c => !Uri.IsHexDigit(c))) return false;
        prefix = key[..split];
        return true;
    }
}
