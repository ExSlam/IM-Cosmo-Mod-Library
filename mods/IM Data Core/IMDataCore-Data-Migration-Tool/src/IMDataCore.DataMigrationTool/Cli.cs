using IMDataCore.DataMigrationTool.Migration;

namespace IMDataCore.DataMigrationTool;

internal static class Cli
{
    internal static int Run(string[] args)
    {
        string command = args[0].ToLowerInvariant();
        var options = Parse(args.Skip(1).ToArray());
        if (command is "help" or "--help" or "-h")
        {
            PrintHelp();
            return 0;
        }


        if (command is "bulk-scan" or "scan-contamination")
        {
            string? root = Get(options, "root");
            if (string.IsNullOrWhiteSpace(root)) return Fail("--root is required for bulk-scan.");

            BulkContaminationScanResult result = LegacyContaminationScanner.ScanRoot(root, p =>
                Console.Error.WriteLine($"[{p.CurrentFile}/{p.TotalFiles}] {p.SourcePath}"));

            Console.WriteLine($"Root: {result.RootPath}");
            Console.WriteLine($"Legacy files discovered: {result.FilesDiscovered}; scanned: {result.FilesScanned}; failed: {result.FilesFailed}");
            Console.WriteLine($"Save streams: {result.Entries.Count}; contaminated/divergent: {result.ContaminatedEntries}; no branch reset detected: {result.CleanEntries}");

            bool includeClean = Has(options, "include-clean");
            foreach (LegacyContaminationEntry entry in result.Entries
                         .Where(x => includeClean || x.IsContaminated)
                         .OrderByDescending(x => x.IsContaminated)
                         .ThenBy(x => x.SourceRelativePath, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(x => x.SaveKey, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine();
                Console.WriteLine(entry.ToMultilineString());
            }

            if (result.Errors.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Scan errors:");
                foreach (BulkScanError error in result.Errors)
                    Console.WriteLine($"  {error.SourceRelativePath}: {error.Message}");
            }

            string? csv = Get(options, "csv");
            if (!string.IsNullOrWhiteSpace(csv))
            {
                LegacyContaminationScanner.WriteCsv(result, Path.GetFullPath(csv));
                Console.WriteLine("CSV written: " + Path.GetFullPath(csv));
            }

            string? json = Get(options, "json");
            if (!string.IsNullOrWhiteSpace(json))
            {
                LegacyContaminationScanner.WriteJson(result, Path.GetFullPath(json));
                Console.WriteLine("JSON written: " + Path.GetFullPath(json));
            }
            return result.FilesFailed == 0 ? 0 : 2;
        }

        if (command is "bulk-plan" or "bulk-convert")
        {
            string? legacyRoot = Get(options, "legacy-root") ?? Get(options, "root");
            if (string.IsNullOrWhiteSpace(legacyRoot)) return Fail("--legacy-root is required for bulk-plan/bulk-convert.");

            string policyText = (Get(options, "contaminated") ?? "auto").Trim().ToLowerInvariant();
            BulkConvertContaminatedPolicy policy = policyText switch
            {
                "auto" or "repair" => BulkConvertContaminatedPolicy.AutoRepair,
                "skip" => BulkConvertContaminatedPolicy.Skip,
                "clean" or "baseline" => BulkConvertContaminatedPolicy.CleanBaseline,
                _ => throw new ArgumentException("--contaminated must be auto, skip, or clean.")
            };

            var bulk = new BulkConversionService();
            BulkConvertPlan plan = bulk.BuildPlan(
                legacyRoot,
                Get(options, "data-root"),
                Get(options, "output-root"),
                policy,
                p => Console.Error.WriteLine($"[{p.Current}/{p.Total}] {p.Message}"));

            Console.WriteLine($"Legacy root: {plan.LegacyRoot}");
            Console.WriteLine($"Vanilla data root: {plan.DataRoot}");
            Console.WriteLine($"Destination root: {plan.OutputRoot}");
            Console.WriteLine($"Streams: {plan.Items.Count}; ready: {plan.ReadyCount}; skipped/manual review: {plan.SkippedCount}; contaminated: {plan.ContaminatedCount}");
            foreach (BulkConvertItem item in plan.Items.OrderByDescending(x => x.IsReady).ThenBy(x => x.SourceRelativePath, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine();
                Console.WriteLine($"[{item.Status}] {item.SourceRelativePath}");
                Console.WriteLine($"  key: {item.SourceSaveKey}");
                Console.WriteLine($"  vanilla: {item.VanillaRelativePath}");
                Console.WriteLine($"  match: {item.MatchScore} {item.MatchKind}");
                Console.WriteLine($"  action: {item.ActionDisplay}" + (item.RepairBranchIndex.HasValue ? $" branch {item.RepairBranchIndex}" : ""));
                Console.WriteLine($"  output: {item.OutputSidecarPath}");
                if (!string.IsNullOrWhiteSpace(item.Details)) Console.WriteLine("  " + item.Details.Replace(Environment.NewLine, Environment.NewLine + "  "));
            }

            string? planJson = Get(options, "plan-json");
            if (!string.IsNullOrWhiteSpace(planJson))
            {
                BulkConversionService.WritePlanJson(plan, Path.GetFullPath(planJson));
                Console.WriteLine("Plan JSON written: " + Path.GetFullPath(planJson));
            }

            if (command == "bulk-plan") return plan.ReadyCount > 0 ? 0 : 1;

            foreach (BulkConvertItem item in plan.Items) item.Selected = item.IsReady;
            bool dryRun = Has(options, "dry-run");
            bool recycleAfterSuccess = Has(options, "recycle-after-success") && !dryRun;
            BulkConvertRunResult run = bulk.Run(plan, Has(options, "overwrite"), dryRun, p =>
                Console.Error.WriteLine($"[{p.Current}/{p.Total}] {p.Message}"), recycleAfterSuccess);
            Console.WriteLine();
            Console.WriteLine($"{(dryRun ? "Validation" : "Bulk conversion")} complete: success {run.SuccessCount}; skipped {run.SkippedCount}; failed {run.FailedCount}.");
            foreach (BulkConvertRunItem item in run.Items.Where(x => !x.Success || x.Skipped))
                Console.WriteLine($"  {(item.Skipped ? "SKIPPED" : "FAILED")}: {item.OutputSidecarPath} — {item.Summary.Split('\n')[0]}");
            if (run.CleanupSuccessCount > 0 || run.CleanupFailedCount > 0)
                Console.WriteLine($"Legacy source cleanup: succeeded {run.CleanupSuccessCount}; failed {run.CleanupFailedCount}.");

            if (Has(options, "recycle-orphans"))
            {
                if (dryRun)
                {
                    Console.WriteLine("Orphan cleanup was not performed because --dry-run is active.");
                }
                else
                {
                    LegacyCleanupBatchResult orphanCleanup = bulk.RecycleOrphanedSources(plan);
                    Console.WriteLine($"Orphan cleanup: sources succeeded {orphanCleanup.SuccessCount}; failed {orphanCleanup.FailedCount}; files moved to Recycle Bin {orphanCleanup.RecycledFileCount}.");
                }
            }

            string? resultJson = Get(options, "result-json");
            if (!string.IsNullOrWhiteSpace(resultJson))
            {
                BulkConversionService.WriteRunJson(run, Path.GetFullPath(resultJson));
                Console.WriteLine("Result JSON written: " + Path.GetFullPath(resultJson));
            }
            return run.FailedCount == 0 ? 0 : 2;
        }

        if (command is "reverse-match" or "find-save")
        {
            if (!options.TryGetValue("source", out string? reverseSource) || string.IsNullOrWhiteSpace(reverseSource))
                return Fail("--source is required for reverse-match.");

            var reverseService = new MigrationService();
            LegacyInspection inspection = reverseService.InspectLegacy(reverseSource);
            ReverseMatchScanResult result = VanillaReverseMatcher.Scan(inspection, Get(options, "data-root"), Get(options, "save-key"));
            Console.WriteLine($"Scanned {result.JsonFilesExamined} JSON files; recognized {result.ValidSavesRead} valid Idol Manager saves.");
            Console.WriteLine("Data root: " + result.DataRoot);
            if (result.Candidates.Count == 0)
            {
                Console.WriteLine("No reverse-match candidates found.");
                return 1;
            }

            Console.WriteLine("Matching vanilla saves:");
            foreach (VanillaSaveCandidate candidate in result.Candidates)
            {
                Console.WriteLine();
                Console.WriteLine($"[{candidate.Score}] {candidate.MatchKind}");
                Console.WriteLine("Matched legacy key: " + candidate.MatchedSaveKey);
                Console.WriteLine(candidate.SaveInfo.ToIdentityMultilineString());
                Console.WriteLine("Folder token: " + candidate.SaveInfo.SlotToken);
                Console.WriteLine("Full path: " + candidate.SaveInfo.FilePath);
            }
            return 0;
        }

        if (command is "save-info" or "identify-save")
        {
            if (!options.TryGetValue("save", out string? identifySave) || string.IsNullOrWhiteSpace(identifySave))
                return Fail("--save is required for save-info.");
            VanillaSaveInfo info = VanillaSaveReader.Read(identifySave);
            Console.WriteLine(info.ToIdentityMultilineString());
            Console.WriteLine();
            Console.WriteLine("Legacy IMDataCore 1.x match hints:");
            Console.WriteLine(info.ToLegacyMatchMultilineString());
            List<LegacySourceCandidate> candidates = VanillaSaveReader.FindLegacySourceCandidates(info, Get(options, "legacy-root"));
            if (candidates.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Likely legacy source files:");
                foreach (LegacySourceCandidate candidate in candidates) Console.WriteLine("  " + candidate);
            }
            return 0;
        }

        if (!options.TryGetValue("source", out string? source) || string.IsNullOrWhiteSpace(source))
            return Fail("--source is required.");

        var service = new MigrationService();
        if (command == "inspect")
        {
            LegacyInspection info = service.InspectLegacy(source);
            Console.WriteLine(info.ToMultilineString());
            return 0;
        }

        if (!options.TryGetValue("save", out string? save) || string.IsNullOrWhiteSpace(save))
            return Fail("--save is required for migrate/validate.");

        string? outputRoot = Get(options, "output-root");
        string? sourceSaveKey = Get(options, "save-key");
        string? legacyVersion = Get(options, "legacy-version");
        bool overwrite = Has(options, "overwrite");
        bool allowUnverified = Has(options, "allow-unverified-checkpoint");

        if (command is "repair-analyze" or "analyze-repair")
        {
            MigrationPlan plan = service.BuildPlan(source, save, outputRoot, sourceSaveKey, legacyVersion, allowUnverified: true);
            LegacyRepairAnalysis analysis = service.AnalyzeRepair(plan);
            Console.WriteLine(analysis.ToMultilineString());
            foreach (LegacyRepairBranch branch in analysis.Branches)
                Console.WriteLine("  " + branch);
            return analysis.HasDivergence ? 0 : 1;
        }

        if (command == "repair")
        {
            MigrationPlan plan = service.BuildPlan(source, save, outputRoot, sourceSaveKey, legacyVersion, allowUnverified: true);
            string modeText = (Get(options, "repair-mode") ?? "salvage").Trim().ToLowerInvariant();
            LegacyRepairMode mode = modeText switch
            {
                "salvage" or "branch" => LegacyRepairMode.ConservativeBranchSalvage,
                "clean" or "baseline" => LegacyRepairMode.CleanBaseline,
                _ => throw new ArgumentException("--repair-mode must be salvage or clean.")
            };
            int? branch = null;
            string? branchText = Get(options, "branch");
            if (!string.IsNullOrWhiteSpace(branchText))
            {
                if (!int.TryParse(branchText, out int parsedBranch) || parsedBranch <= 0)
                    return Fail("--branch must be a positive branch number.");
                branch = parsedBranch;
            }
            MigrationReport report = service.RepairAndMigrate(plan, mode, branch, overwrite);
            if (report.Success && Has(options, "recycle-source"))
            {
                if (LegacyCleanupService.CanRecycleWholeSource(service, plan, out string cleanupReason))
                {
                    LegacyCleanupBatchResult cleanup = LegacyCleanupService.RecycleSources(new[] { plan.SourcePath });
                    LegacyCleanupItemResult cleanupItem = cleanup.Items.Single();
                    report.Messages.Add("Legacy source cleanup: " + cleanupItem.Summary);
                }
                else report.Messages.Add("Legacy source cleanup: " + cleanupReason);
            }
            Console.WriteLine(report.ToMultilineString());
            return report.Success ? 0 : 1;
        }

        if (command == "validate")
        {
            MigrationPlan plan = service.BuildPlan(source, save, outputRoot, sourceSaveKey, legacyVersion, allowUnverified);
            MigrationReport report = service.ValidatePlan(plan);
            Console.WriteLine(report.ToMultilineString());
            return report.Success ? 0 : 1;
        }

        if (command == "migrate")
        {
            MigrationPlan plan = service.BuildPlan(source, save, outputRoot, sourceSaveKey, legacyVersion, allowUnverified);
            MigrationReport report = service.Migrate(plan, overwrite);
            if (report.Success && Has(options, "recycle-source"))
            {
                if (LegacyCleanupService.CanRecycleWholeSource(service, plan, out string cleanupReason))
                {
                    LegacyCleanupBatchResult cleanup = LegacyCleanupService.RecycleSources(new[] { plan.SourcePath });
                    LegacyCleanupItemResult cleanupItem = cleanup.Items.Single();
                    report.Messages.Add("Legacy source cleanup: " + cleanupItem.Summary);
                }
                else report.Messages.Add("Legacy source cleanup: " + cleanupReason);
            }
            Console.WriteLine(report.ToMultilineString());
            return report.Success ? 0 : 1;
        }

        PrintHelp();
        return 1;
    }

    private static Dictionary<string, string?> Parse(string[] args)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
        {
            string token = args[i];
            if (!token.StartsWith("--", StringComparison.Ordinal)) continue;
            string key = token[2..];
            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                result[key] = args[++i];
            else
                result[key] = null;
        }
        return result;
    }

    private static string? Get(Dictionary<string, string?> map, string key) =>
        map.TryGetValue(key, out string? value) ? value : null;

    private static bool Has(Dictionary<string, string?> map, string key) => map.ContainsKey(key);

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        Console.Error.WriteLine("Run \"IMDataCore Data Migration Tool.exe\" help for usage.");
        return 1;
    }

    private static void PrintHelp()
    {
        const string exe = "\"IMDataCore Data Migration Tool.exe\"";
        Console.WriteLine($"{ToolInfo.ProductName} {ToolInfo.Version} - by {ToolInfo.Author}");
        Console.WriteLine("Offline IMDataCore 1.0.0-1.3.0 -> current IMDataCore v6 migration tool\n");
        Console.WriteLine($"GUI:       {exe}");
        Console.WriteLine($"Save info:     {exe} save-info --save <vanilla-save> [--legacy-root <dir>]");
        Console.WriteLine($"Reverse match: {exe} reverse-match --source <legacy-file> [--data-root <dir>] [--save-key <key>]");
        Console.WriteLine($"Bulk scan:     {exe} bulk-scan --root <legacy-root> [--csv <file>] [--json <file>] [--include-clean]");
        Console.WriteLine($"Bulk plan:     {exe} bulk-plan --legacy-root <dir> [--data-root <dir>] [--output-root <dir>] [--contaminated auto|skip|clean] [--plan-json <file>]");
        Console.WriteLine($"Bulk convert:  {exe} bulk-convert --legacy-root <dir> [--data-root <dir>] [--output-root <dir>] [--contaminated auto|skip|clean] [--overwrite] [--recycle-after-success] [--recycle-orphans] [--dry-run] [--result-json <file>]");
        Console.WriteLine($"Inspect:   {exe} inspect --source <legacy-file>");
        Console.WriteLine($"Repair analysis: {exe} repair-analyze --source <legacy-file> --save <vanilla-save> [options]");
        Console.WriteLine($"Repair:    {exe} repair --source <legacy-file> --save <vanilla-save> [--repair-mode salvage|clean] [--branch <n>] [options]");
        Console.WriteLine($"Validate:  {exe} validate --source <legacy-file> --save <vanilla-save> [options]");
        Console.WriteLine($"Migrate:   {exe} migrate --source <legacy-file> --save <vanilla-save> [options]\n");
        Console.WriteLine("Options:");
        Console.WriteLine("  --legacy-root <dir>         Root containing old 1.x per-save folders; used by save-info candidate discovery.");
        Console.WriteLine("  --root <dir>                Legacy IMDataCore root recursively scanned by bulk-scan.");
        Console.WriteLine("  --csv <file>                Write bulk-scan results as CSV.");
        Console.WriteLine("  --json <file>               Write bulk-scan results as JSON.");
        Console.WriteLine("  --include-clean             Also print non-divergent streams in bulk-scan console output.");
        Console.WriteLine("  --contaminated <policy>     Bulk conversion policy: auto (default), skip, or clean.");
        Console.WriteLine("  --recycle-after-success     After a successful written/revalidated bulk conversion, move that legacy source to the Windows Recycle Bin.");
        Console.WriteLine("  --recycle-orphans           Move strict No-vanilla-match legacy sources to the Windows Recycle Bin after bulk conversion (never with --dry-run).");
        Console.WriteLine("  --recycle-source            After successful single migrate/repair, move the selected legacy source to the Windows Recycle Bin.");
        Console.WriteLine("  --plan-json <file>          Write the bulk conversion plan as JSON.");
        Console.WriteLine("  --result-json <file>        Write bulk conversion/validation results as JSON.");
        Console.WriteLine("  --dry-run                   Validate selected bulk conversions without writing sidecars.");
        Console.WriteLine("  --data-root <dir>           Idol Manager data tree to scan for reverse matching.");
        Console.WriteLine("  --output-root <dir>         IMDataCore root. Defaults to sibling of the selected data folder.");
        Console.WriteLine("  --save-key <key>            SQLite source save_key when more than one is present.");
        Console.WriteLine("  --legacy-version <version>  1.0.0, 1.1.0, 1.2.0, or 1.3.0. Auto-detected when possible.");
        Console.WriteLine("  --repair-mode <mode>        Branch repair mode: salvage (default) or clean.");
        Console.WriteLine("  --branch <n>                Explicit branch number for salvage repair. Omit to use a confident recommendation.");
        Console.WriteLine("  --overwrite                 Replace an existing sidecar after archiving every live sibling artifact.");
        Console.WriteLine("  --allow-unverified-checkpoint  Advanced: allow an unverified legacy/save association, including a missing late-1.3 checkpoint or pre-checkpoint history newer than the selected save.");
    }
}
