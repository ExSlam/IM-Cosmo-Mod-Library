using System.Globalization;
using System.Text;
using System.Text.Json;

namespace IMDataCore.DataMigrationTool.Migration;

internal static class LegacyContaminationScanner
{
    private static readonly HashSet<string> LegacyFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "im_data_core.db",
        "im_data_core.fallback.json"
    };

    public static BulkContaminationScanResult ScanRoot(string rootPath, Action<BulkScanProgress>? progress = null)
    {
        string root = Path.GetFullPath(rootPath);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException("Legacy IMDataCore root was not found: " + root);

        List<string> files = Directory
            .EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => LegacyFileNames.Contains(Path.GetFileName(path)))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var result = new BulkContaminationScanResult
        {
            RootPath = root,
            FilesDiscovered = files.Count
        };

        for (int i = 0; i < files.Count; i++)
        {
            string path = files[i];
            progress?.Invoke(new BulkScanProgress(i + 1, files.Count, path));
            try
            {
                List<LegacyContaminationEntry> entries = IsSqlite(path)
                    ? LegacySqliteReader.AnalyzeContamination(path)
                    : LegacyFlatJsonReader.AnalyzeContamination(path);

                foreach (LegacyContaminationEntry entry in entries)
                    entry.SourceRelativePath = Path.GetRelativePath(root, entry.SourcePath);

                result.Entries.AddRange(entries);
                result.FilesScanned++;
            }
            catch (Exception ex)
            {
                result.FilesFailed++;
                result.Errors.Add(new BulkScanError
                {
                    SourcePath = path,
                    SourceRelativePath = Path.GetRelativePath(root, path),
                    Message = ex.Message
                });
            }
        }

        return result;
    }

    public static LegacyContaminationEntry AnalyzeTimeline(
        string sourcePath,
        string backendKind,
        string saveKey,
        bool hasCheckpointSupport,
        IReadOnlyList<LegacyEvent> events)
    {
        var entry = new LegacyContaminationEntry
        {
            SourcePath = Path.GetFullPath(sourcePath),
            BackendKind = backendKind,
            SaveKey = saveKey,
            HasCheckpointSupport = hasCheckpointSupport,
            EventCount = events.Count
        };

        LegacyEvent? previousParsedEvent = null;
        DateTime previousDate = default;

        foreach (LegacyEvent current in events.OrderBy(x => x.EventId))
        {
            if (!GameDateUtilities.TryParseVanillaOrRoundTrip(current.GameDateTime, out DateTime currentDate))
            {
                entry.InvalidDateCount++;
                continue;
            }

            entry.ParsedEventCount++;
            if (entry.FirstParsedEventId == 0)
            {
                entry.FirstParsedEventId = current.EventId;
                entry.FirstGameDateTime = current.GameDateTime;
            }
            entry.LastParsedEventId = current.EventId;
            entry.LastGameDateTime = current.GameDateTime;

            if (previousParsedEvent is not null && currentDate < previousDate)
            {
                TimeSpan rollback = previousDate - currentDate;
                entry.Divergences.Add(new LegacyDivergencePoint
                {
                    PreviousEventId = previousParsedEvent.EventId,
                    PreviousGameDateTime = previousParsedEvent.GameDateTime,
                    DivergenceEventId = current.EventId,
                    DivergenceGameDateTime = current.GameDateTime,
                    RollbackSeconds = checked((long)Math.Round(rollback.TotalSeconds, MidpointRounding.AwayFromZero))
                });
            }

            previousParsedEvent = current;
            previousDate = currentDate;
        }

        return entry;
    }

    public static void WriteJson(BulkContaminationScanResult result, string outputPath)
    {
        var payload = new
        {
            schema = "IMDataCore.DataMigrationTool.ContaminationScan",
            schemaVersion = 1,
            generatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            toolVersion = IMDataCore.DataMigrationTool.ToolInfo.Version,
            rootPath = result.RootPath,
            filesDiscovered = result.FilesDiscovered,
            filesScanned = result.FilesScanned,
            filesFailed = result.FilesFailed,
            contaminatedEntries = result.Entries.Count(x => x.IsContaminated),
            entries = result.Entries.Select(ToExportEntry).ToArray(),
            errors = result.Errors
        };
        string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputPath, json, new UTF8Encoding(false));
    }

    public static void WriteCsv(BulkContaminationScanResult result, string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("status,source_relative_path,source_path,backend,save_key,checkpoint_support,event_count,parsed_event_count,invalid_date_count,branch_count,divergence_count,primary_previous_event_id,primary_previous_date,primary_divergence_event_id,primary_divergence_date,primary_rollback_seconds,primary_rollback_days,all_divergence_points");
        foreach (LegacyContaminationEntry entry in result.Entries)
        {
            LegacyDivergencePoint? primary = entry.PrimaryDivergence;
            string all = string.Join(" | ", entry.Divergences.Select(d =>
                $"{d.PreviousEventId}:{d.PreviousGameDateTime} -> {d.DivergenceEventId}:{d.DivergenceGameDateTime} ({d.RollbackSeconds}s)"));

            string[] values =
            {
                entry.Status,
                entry.SourceRelativePath,
                entry.SourcePath,
                entry.BackendKind,
                entry.SaveKey,
                entry.HasCheckpointSupport ? "yes" : "no",
                entry.EventCount.ToString(CultureInfo.InvariantCulture),
                entry.ParsedEventCount.ToString(CultureInfo.InvariantCulture),
                entry.InvalidDateCount.ToString(CultureInfo.InvariantCulture),
                entry.BranchCount.ToString(CultureInfo.InvariantCulture),
                entry.Divergences.Count.ToString(CultureInfo.InvariantCulture),
                primary?.PreviousEventId.ToString(CultureInfo.InvariantCulture) ?? "",
                primary?.PreviousGameDateTime ?? "",
                primary?.DivergenceEventId.ToString(CultureInfo.InvariantCulture) ?? "",
                primary?.DivergenceGameDateTime ?? "",
                primary?.RollbackSeconds.ToString(CultureInfo.InvariantCulture) ?? "",
                primary is null ? "" : primary.RollbackDays.ToString("0.######", CultureInfo.InvariantCulture),
                all
            };
            sb.AppendLine(string.Join(',', values.Select(Csv)));
        }

        if (result.Errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("scan_error_source_relative_path,scan_error_source_path,scan_error_message");
            foreach (BulkScanError error in result.Errors)
                sb.AppendLine(string.Join(',', new[] { error.SourceRelativePath, error.SourcePath, error.Message }.Select(Csv)));
        }

        File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(true));
    }

    private static object ToExportEntry(LegacyContaminationEntry entry)
    {
        LegacyDivergencePoint? primary = entry.PrimaryDivergence;
        return new
        {
            status = entry.Status,
            sourceRelativePath = entry.SourceRelativePath,
            sourcePath = entry.SourcePath,
            backend = entry.BackendKind,
            saveKey = entry.SaveKey,
            checkpointSupport = entry.HasCheckpointSupport,
            eventCount = entry.EventCount,
            parsedEventCount = entry.ParsedEventCount,
            invalidDateCount = entry.InvalidDateCount,
            branchCount = entry.BranchCount,
            divergenceCount = entry.Divergences.Count,
            firstEventId = entry.FirstParsedEventId,
            firstGameDateTime = entry.FirstGameDateTime,
            lastEventId = entry.LastParsedEventId,
            lastGameDateTime = entry.LastGameDateTime,
            primaryDivergence = primary,
            divergences = entry.Divergences
        };
    }

    private static bool IsSqlite(string path)
    {
        using FileStream fs = File.OpenRead(path);
        byte[] header = new byte[Math.Min(16, checked((int)Math.Min(fs.Length, 16)))];
        if (header.Length > 0) fs.ReadExactly(header);
        return Encoding.ASCII.GetString(header).StartsWith("SQLite format 3\0", StringComparison.Ordinal);
    }

    private static string Csv(string value)
    {
        value ??= string.Empty;
        if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
