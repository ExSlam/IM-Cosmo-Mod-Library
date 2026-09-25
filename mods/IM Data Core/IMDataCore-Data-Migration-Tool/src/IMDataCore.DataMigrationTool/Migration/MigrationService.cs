using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace IMDataCore.DataMigrationTool.Migration;

internal sealed class MigrationService
{
    public LegacyInspection InspectLegacy(string sourcePath)
    {
        string full = Path.GetFullPath(sourcePath);
        if (!File.Exists(full)) throw new FileNotFoundException("Legacy IMDataCore file was not found.", full);
        return IsSqlite(full) ? LegacySqliteReader.Inspect(full) : LegacyFlatJsonReader.Inspect(full);
    }

    public MigrationPlan BuildPlan(string sourcePath, string savePath, string? outputRoot, string? sourceSaveKey, string? legacyVersionOverride, bool allowUnverified)
    {
        string source = Path.GetFullPath(sourcePath);
        if (!File.Exists(source)) throw new FileNotFoundException("Legacy IMDataCore source was not found.", source);
        VanillaSaveInfo vanilla = VanillaSaveReader.Read(savePath);
        string root = string.IsNullOrWhiteSpace(outputRoot) ? Path.Combine(vanilla.PersistentRoot, "IMDataCore") : Path.GetFullPath(outputRoot);
        string defaultRoot = Path.GetFullPath(Path.Combine(vanilla.PersistentRoot, "IMDataCore"));
        string dataRoot = Path.GetFullPath(vanilla.DataRoot);
        if (IsUnder(root, dataRoot)) throw new InvalidDataException("Output root must not be inside vanilla's data directory.");
        string output = Path.GetFullPath(Path.Combine(root, vanilla.RelativeSavePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!IsUnder(output, root)) throw new InvalidDataException("Resolved output escaped the selected IMDataCore root.");
        if (!string.IsNullOrWhiteSpace(legacyVersionOverride) && legacyVersionOverride is not ("1.0.0" or "1.1.0" or "1.2.0" or "1.3.0"))
            throw new InvalidDataException("--legacy-version must be 1.0.0, 1.1.0, 1.2.0, or 1.3.0.");
        return new MigrationPlan
        {
            SourcePath = source, SourceSaveKey = sourceSaveKey, LegacyVersionOverride = legacyVersionOverride ?? "",
            AllowUnverifiedCheckpoint = allowUnverified, Vanilla = vanilla, OutputRoot = root, OutputSidecarPath = output
        };
    }

    public MigrationReport ValidatePlan(MigrationPlan plan)
    {
        var report = new MigrationReport { OutputPath = plan.OutputSidecarPath };
        try
        {
            LegacySnapshot source = ReadSnapshot(plan);
            V6Document document = V6Writer.Build(source, plan.Vanilla, plan.SourcePath, plan.LegacyVersionOverride);
            string json = V6Writer.Serialize(document);
            using JsonDocument parsed = JsonDocument.Parse(json);
            if (parsed.RootElement.GetProperty("FormatVersion").GetInt32() != 6) throw new InvalidDataException("Serialized output lost v6 discriminator.");
            report.Success = true;
            report.Summary = "Plan is statically valid and serializes as a strict v6 JSON document.";
            report.EventCount = source.Events.Count; report.CustomDataCount = source.CustomData.Count;
            report.Messages.Add("Validation scope: v6 schema/serialization and migration invariants passed; Validate only did not write the output file.");
            if (string.Equals(source.CheckpointStatus, "not_available", StringComparison.Ordinal))
                report.Messages.Add("Association confidence: this legacy generation has no exact save checkpoint, so validation does not prove that the legacy state came from this exact vanilla save generation.");
            else if (string.Equals(source.CheckpointStatus, "exact_match", StringComparison.Ordinal))
                report.Messages.Add("Association confidence: exact legacy save checkpoint matched the selected vanilla save fingerprint.");
            else if (source.CheckpointStatus.StartsWith("override_", StringComparison.Ordinal))
                report.Messages.Add("Association confidence: advanced override is active; exact legacy checkpoint matching was not established.");
            report.Messages.Add("Destination root: " + plan.OutputRoot);
            report.Messages.Add("Relative save path: " + plan.Vanilla.RelativeSavePath);
            report.Messages.Add("Legacy checkpoint status: " + source.CheckpointStatus);
            report.Messages.AddRange(source.Warnings.Select(x => "WARNING: " + x));
        }
        catch (Exception ex) { report.Success = false; report.Summary = ex.Message; }
        return report;
    }

    public MigrationReport Migrate(MigrationPlan plan, bool overwrite)
    {
        MigrationReport report = ValidatePlan(plan);
        if (!report.Success) return report;
        if (IsGameRunning())
        {
            report.Success = false;
            report.Summary = "Idol Manager appears to be running. Close the game before writing a live IMDataCore sidecar.";
            return report;
        }

        try
        {
            LegacySnapshot source = ReadSnapshot(plan);
            V6Document document = V6Writer.Build(source, plan.Vanilla, plan.SourcePath, plan.LegacyVersionOverride);
            string json = V6Writer.Serialize(document);
            Directory.CreateDirectory(Path.GetDirectoryName(plan.OutputSidecarPath)!);
            if (HasLiveSiblingArtifacts(plan.OutputSidecarPath) && !overwrite)
                throw new IOException("One or more live sidecar/journal/backup artifacts already exist at the destination. Leave them untouched or explicitly enable overwrite/backup.");
            if (overwrite) ArchiveLiveSiblingArtifacts(plan.OutputSidecarPath, report.Messages);
            AtomicWrite(plan.OutputSidecarPath, json);
            ValidateWrittenSidecar(plan);
            report.Messages.Add("Post-write sidecar revalidation passed.");
            // A journal bound to the previous base must never remain beside the new base.
            string staleJournal = plan.OutputSidecarPath + ".imdc.journal";
            if (File.Exists(staleJournal)) throw new IOException("A stale live journal remained after archival; refusing to finish migration.");
            report.Success = true; report.Summary = "Migration completed. The new sidecar is v6, exact-save bound, and ready for current IMDataCore to populate newer fields on subsequent saves.";
            report.EventCount = source.Events.Count; report.CustomDataCount = source.CustomData.Count;
            report.Messages.Add("Written atomically: " + plan.OutputSidecarPath);
        }
        catch (Exception ex) { report.Success = false; report.Summary = ex.Message; }
        return report;
    }

    public LegacyRepairAnalysis AnalyzeRepair(MigrationPlan plan) => LegacyRepairService.Analyze(plan);

    public MigrationReport ValidateRepairPlan(MigrationPlan plan, LegacyRepairMode mode, int? branchIndex)
    {
        var report = new MigrationReport { OutputPath = plan.OutputSidecarPath };
        try
        {
            LegacySnapshot source = LegacyRepairService.BuildRepairedSnapshot(plan, mode, branchIndex);
            V6Document document = V6Writer.Build(source, plan.Vanilla, plan.SourcePath, plan.LegacyVersionOverride);
            string json = V6Writer.Serialize(document);
            using JsonDocument parsed = JsonDocument.Parse(json);
            if (parsed.RootElement.GetProperty("FormatVersion").GetInt32() != 6)
                throw new InvalidDataException("Serialized repaired output lost v6 discriminator.");
            report.Success = true;
            report.Summary = mode == LegacyRepairMode.CleanBaseline
                ? "Clean-baseline repair is statically valid and ready to write."
                : "Branch-salvage repair is statically valid and ready to write.";
            report.EventCount = source.Events.Count;
            report.CustomDataCount = source.CustomData.Count;
            report.Messages.Add("Repair mode: " + source.RepairMode);
            report.Messages.Add(source.RepairSummary);
            report.Messages.Add($"Dropped legacy events: {source.RepairDroppedEventCount:N0}; dropped custom-data rows: {source.RepairDroppedCustomDataCount:N0}");
            report.Messages.Add("Destination root: " + plan.OutputRoot);
            report.Messages.Add("Relative save path: " + plan.Vanilla.RelativeSavePath);
            report.Messages.AddRange(source.Warnings.Select(x => "WARNING: " + x));
        }
        catch (Exception ex)
        {
            report.Success = false;
            report.Summary = ex.Message;
        }
        return report;
    }

    public MigrationReport RepairAndMigrate(MigrationPlan plan, LegacyRepairMode mode, int? branchIndex, bool overwrite)
    {
        MigrationReport report = ValidateRepairPlan(plan, mode, branchIndex);
        if (!report.Success) return report;
        if (IsGameRunning())
        {
            report.Success = false;
            report.Summary = "Idol Manager appears to be running. Close the game before writing a repaired live IMDataCore sidecar.";
            return report;
        }

        try
        {
            LegacySnapshot source = LegacyRepairService.BuildRepairedSnapshot(plan, mode, branchIndex);
            V6Document document = V6Writer.Build(source, plan.Vanilla, plan.SourcePath, plan.LegacyVersionOverride);
            string json = V6Writer.Serialize(document);
            Directory.CreateDirectory(Path.GetDirectoryName(plan.OutputSidecarPath)!);
            if (HasLiveSiblingArtifacts(plan.OutputSidecarPath) && !overwrite)
                throw new IOException("One or more live sidecar/journal/backup artifacts already exist at the destination. Leave them untouched or explicitly enable overwrite/backup.");
            if (overwrite) ArchiveLiveSiblingArtifacts(plan.OutputSidecarPath, report.Messages);
            AtomicWrite(plan.OutputSidecarPath, json);
            ValidateWrittenSidecar(plan);
            report.Messages.Add("Post-write sidecar revalidation passed.");
            string staleJournal = plan.OutputSidecarPath + ".imdc.journal";
            if (File.Exists(staleJournal)) throw new IOException("A stale live journal remained after archival; refusing to finish repaired migration.");
            report.Success = true;
            report.Summary = mode == LegacyRepairMode.CleanBaseline
                ? "Clean-baseline repair completed. A fresh exact v6 checkpoint was created without contaminated legacy state."
                : "Branch repair completed. Only the selected branch through the vanilla save date was retained; mutable legacy custom-data state was discarded.";
            report.EventCount = source.Events.Count;
            report.CustomDataCount = source.CustomData.Count;
            report.Messages.Add("Written atomically: " + plan.OutputSidecarPath);
        }
        catch (Exception ex)
        {
            report.Success = false;
            report.Summary = ex.Message;
        }
        return report;
    }


    public static void ValidateWrittenSidecar(MigrationPlan plan)
    {
        if (!File.Exists(plan.OutputSidecarPath))
            throw new FileNotFoundException("The migrated sidecar was not found during post-write verification.", plan.OutputSidecarPath);

        string json = File.ReadAllText(plan.OutputSidecarPath, Encoding.UTF8);
        V6Document document = JsonSerializer.Deserialize<V6Document>(json)
            ?? throw new InvalidDataException("The written sidecar could not be deserialized for post-write verification.");
        V6StaticValidator.Validate(document);

        string expectedRelative = V6Writer.NormalizeRelative(plan.Vanilla.RelativeSavePath);
        if (!string.Equals(V6Writer.NormalizeRelative(document.RelativeSavePath), expectedRelative, StringComparison.Ordinal))
            throw new InvalidDataException("Post-write verification failed: RelativeSavePath does not match the selected vanilla save.");
        if (document.Checkpoints.Count != 1 ||
            !string.Equals(document.Checkpoints[0].ContentFingerprint, plan.Vanilla.V6ContentFingerprint, StringComparison.Ordinal))
            throw new InvalidDataException("Post-write verification failed: checkpoint fingerprint does not match the selected vanilla save.");
    }

    private LegacySnapshot ReadSnapshot(MigrationPlan plan) => IsSqlite(plan.SourcePath)
        ? LegacySqliteReader.Read(plan.SourcePath, plan.Vanilla, plan.SourceSaveKey, plan.AllowUnverifiedCheckpoint)
        : LegacyFlatJsonReader.Read(plan.SourcePath, plan.Vanilla, plan.AllowUnverifiedCheckpoint);

    private static bool IsSqlite(string path)
    {
        using FileStream fs = File.OpenRead(path);
        byte[] header = new byte[Math.Min(16, (int)Math.Min(fs.Length, 16))];
        fs.ReadExactly(header);
        return Encoding.ASCII.GetString(header).StartsWith("SQLite format 3\0", StringComparison.Ordinal);
    }

    private static void AtomicWrite(string path, string text)
    {
        string temp = path + ".data-migration-tool.tmp." + Guid.NewGuid().ToString("N");
        try
        {
            using (var fs = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var sw = new StreamWriter(fs, new UTF8Encoding(false)))
            {
                sw.Write(text); sw.Flush(); fs.Flush(true);
            }
            File.Move(temp, path, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private static bool HasLiveSiblingArtifacts(string sidecar)
    {
        string[] live = { sidecar, sidecar + ".imdc.journal", sidecar + ".imdc.bak", sidecar + ".imdc.bak.imdc.journal" };
        return live.Any(File.Exists);
    }

    private static void ArchiveLiveSiblingArtifacts(string sidecar, List<string> messages)
    {
        string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        string[] live = { sidecar, sidecar + ".imdc.journal", sidecar + ".imdc.bak", sidecar + ".imdc.bak.imdc.journal" };
        foreach (string file in live)
        {
            if (!File.Exists(file)) continue;
            string archived = file + ".data-migration-tool-backup-" + stamp;
            int suffix = 2;
            while (File.Exists(archived)) archived = file + ".data-migration-tool-backup-" + stamp + "-" + suffix++;
            File.Move(file, archived);
            messages.Add("Archived existing artifact: " + archived);
        }
    }

    private static bool IsUnder(string child, string parent)
    {
        string c = Path.GetFullPath(child).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string p = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return c.StartsWith(p, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGameRunning()
    {
        try { return Process.GetProcesses().Any(p => p.ProcessName.Contains("Idol Manager", StringComparison.OrdinalIgnoreCase)); }
        catch { return false; }
    }
}
