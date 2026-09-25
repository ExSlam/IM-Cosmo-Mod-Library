using System.Text;
using System.Text.Json;

namespace IMDataCore.DataMigrationTool.Migration;

internal sealed class BulkConversionService
{
    private const int MinimumAutomaticMatchScore = 93;
    private readonly MigrationService _migration = new();

    public BulkConvertPlan BuildPlan(
        string legacyRootPath,
        string? dataRootPath,
        string? outputRootPath,
        BulkConvertContaminatedPolicy contaminatedPolicy,
        Action<BulkConvertProgress>? progress = null)
    {
        string legacyRoot = Path.GetFullPath(legacyRootPath);
        if (!Directory.Exists(legacyRoot)) throw new DirectoryNotFoundException("Legacy IMDataCore root was not found: " + legacyRoot);

        string dataRoot = string.IsNullOrWhiteSpace(dataRootPath)
            ? VanillaSaveReader.DefaultDataDirectory()
            : Path.GetFullPath(dataRootPath);
        if (!Directory.Exists(dataRoot)) throw new DirectoryNotFoundException("Vanilla data root was not found: " + dataRoot);

        string persistentRoot = Directory.GetParent(dataRoot)?.FullName
            ?? throw new InvalidDataException("Could not resolve the Idol Manager persistent root from the vanilla data directory.");
        string outputRoot = string.IsNullOrWhiteSpace(outputRootPath)
            ? Path.Combine(persistentRoot, "IMDataCore")
            : Path.GetFullPath(outputRootPath);

        progress?.Invoke(new BulkConvertProgress(0, 1, "Reading vanilla saves once for reverse matching…"));
        IReadOnlyList<VanillaSaveInfo> cachedSaves = VanillaReverseMatcher.LoadRecognizedSaves(dataRoot, out int examined);

        progress?.Invoke(new BulkConvertProgress(0, 1, "Scanning legacy timelines for branch contamination…"));
        BulkContaminationScanResult contamination = LegacyContaminationScanner.ScanRoot(legacyRoot);

        var result = new BulkConvertPlan
        {
            LegacyRoot = legacyRoot,
            DataRoot = dataRoot,
            OutputRoot = outputRoot,
            LegacyFilesDiscovered = contamination.FilesDiscovered,
            VanillaJsonFilesExamined = examined,
            VanillaSavesRecognized = cachedSaves.Count
        };
        result.Errors.AddRange(contamination.Errors.Select(e => e.SourceRelativePath + ": " + e.Message));

        var inspectionCache = new Dictionary<string, LegacyInspection>(StringComparer.OrdinalIgnoreCase);
        int total = contamination.Entries.Count;
        for (int i = 0; i < total; i++)
        {
            LegacyContaminationEntry entry = contamination.Entries[i];
            progress?.Invoke(new BulkConvertProgress(i + 1, Math.Max(1, total), entry.SourceRelativePath));
            try
            {
                if (!inspectionCache.TryGetValue(entry.SourcePath, out LegacyInspection? inspection))
                {
                    inspection = _migration.InspectLegacy(entry.SourcePath);
                    inspectionCache[entry.SourcePath] = inspection;
                }
                result.Items.Add(BuildItem(result, entry, inspection, cachedSaves, examined, contaminatedPolicy));
            }
            catch (Exception ex)
            {
                result.Items.Add(new BulkConvertItem
                {
                    Selected = false,
                    SourcePath = entry.SourcePath,
                    SourceRelativePath = entry.SourceRelativePath,
                    SourceSaveKey = NullIfPlaceholder(entry.SaveKey),
                    BackendKind = entry.BackendKind,
                    HasCheckpointSupport = entry.HasCheckpointSupport,
                    LegacyEventCount = entry.EventCount,
                    IsContaminated = entry.IsContaminated,
                    BranchCount = entry.BranchCount,
                    Action = BulkConvertAction.Skip,
                    Status = "Error",
                    Details = ex.Message
                });
            }
        }

        ResolveOutputCollisions(result.Items);
        return result;
    }

    public BulkConvertRunResult Run(
        BulkConvertPlan plan,
        bool overwrite,
        bool dryRun,
        Action<BulkConvertProgress>? progress = null,
        bool recycleAfterSuccess = false)
    {
        var result = new BulkConvertRunResult { DryRun = dryRun };
        List<BulkConvertItem> selected = plan.Items.Where(x => x.Selected && x.IsReady).ToList();

        for (int i = 0; i < selected.Count; i++)
        {
            BulkConvertItem item = selected[i];
            progress?.Invoke(new BulkConvertProgress(i + 1, Math.Max(1, selected.Count), item.VanillaRelativePath));
            if (!dryRun && item.DestinationExists && !overwrite)
            {
                result.Items.Add(new BulkConvertRunItem
                {
                    SourcePath = item.SourcePath,
                    SourceSaveKey = item.SourceSaveKey,
                    VanillaSavePath = item.VanillaSavePath,
                    OutputSidecarPath = item.OutputSidecarPath,
                    Action = item.Action,
                    Success = false,
                    Skipped = true,
                    Summary = "Skipped because a live destination artifact already exists and overwrite-with-backup was not enabled."
                });
                continue;
            }
            try
            {
                bool repair = item.Action is BulkConvertAction.ConservativeBranchSalvage or BulkConvertAction.CleanBaseline;
                MigrationPlan migrationPlan = _migration.BuildPlan(
                    item.SourcePath,
                    item.VanillaSavePath,
                    plan.OutputRoot,
                    item.SourceSaveKey,
                    legacyVersionOverride: null,
                    allowUnverified: repair);

                MigrationReport report = item.Action switch
                {
                    BulkConvertAction.NormalMigration => dryRun
                        ? _migration.ValidatePlan(migrationPlan)
                        : _migration.Migrate(migrationPlan, overwrite),
                    BulkConvertAction.ConservativeBranchSalvage => dryRun
                        ? _migration.ValidateRepairPlan(migrationPlan, LegacyRepairMode.ConservativeBranchSalvage, item.RepairBranchIndex)
                        : _migration.RepairAndMigrate(migrationPlan, LegacyRepairMode.ConservativeBranchSalvage, item.RepairBranchIndex, overwrite),
                    BulkConvertAction.CleanBaseline => dryRun
                        ? _migration.ValidateRepairPlan(migrationPlan, LegacyRepairMode.CleanBaseline, null)
                        : _migration.RepairAndMigrate(migrationPlan, LegacyRepairMode.CleanBaseline, null, overwrite),
                    _ => throw new InvalidOperationException("Skipped bulk item reached the execution phase.")
                };

                var runItem = new BulkConvertRunItem
                {
                    SourcePath = item.SourcePath,
                    SourceSaveKey = item.SourceSaveKey,
                    VanillaSavePath = item.VanillaSavePath,
                    OutputSidecarPath = item.OutputSidecarPath,
                    Action = item.Action,
                    Success = report.Success,
                    Summary = report.ToMultilineString()
                };

                result.Items.Add(runItem);
                AppLog.Info($"Bulk {(dryRun ? "validation" : "conversion")} {(report.Success ? "succeeded" : "failed")}: {item.SourcePath} -> {item.OutputSidecarPath}; action={item.Action}");
            }
            catch (Exception ex)
            {
                result.Items.Add(new BulkConvertRunItem
                {
                    SourcePath = item.SourcePath,
                    SourceSaveKey = item.SourceSaveKey,
                    VanillaSavePath = item.VanillaSavePath,
                    OutputSidecarPath = item.OutputSidecarPath,
                    Action = item.Action,
                    Success = false,
                    Summary = ex.Message
                });
                AppLog.Error("Bulk conversion item failed: " + item.SourcePath, ex);
            }
        }
        if (!dryRun && recycleAfterSuccess)
            RecycleSuccessfulSources(plan, result);
        return result;
    }


    public LegacyCleanupBatchResult RecycleSuccessfulSources(BulkConvertPlan plan, BulkConvertRunResult run)
    {
        List<string> eligibleSources = GetSuccessfulCleanupSourcePaths(plan, run).ToList();
        LegacyCleanupBatchResult cleanup = LegacyCleanupService.RecycleSources(eligibleSources);
        var byPath = cleanup.Items.ToDictionary(x => Path.GetFullPath(x.SourcePath), StringComparer.OrdinalIgnoreCase);
        foreach (BulkConvertRunItem item in run.Items.Where(x => eligibleSources.Contains(x.SourcePath, StringComparer.OrdinalIgnoreCase)))
        {
            if (!byPath.TryGetValue(Path.GetFullPath(item.SourcePath), out LegacyCleanupItemResult? cleaned)) continue;
            item.LegacyCleanupAttempted = true;
            item.LegacyCleanupSucceeded = cleaned.Success;
            item.LegacyCleanupSummary = cleaned.Summary;
            if (!string.IsNullOrWhiteSpace(cleaned.Summary))
                item.Summary += Environment.NewLine + "Legacy source cleanup: " + cleaned.Summary;
        }
        return cleanup;
    }

    public LegacyCleanupBatchResult RecycleOrphanedSources(BulkConvertPlan plan) =>
        LegacyCleanupService.RecycleSources(GetStrictOrphanSourcePaths(plan));

    public static IEnumerable<string> GetStrictOrphanSourcePaths(BulkConvertPlan plan) =>
        plan.Items
            .GroupBy(x => x.SourcePath, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.All(IsStrictOrphan))
            .Select(g => g.Key);

    public static bool HasStrictOrphanSources(BulkConvertPlan plan) => GetStrictOrphanSourcePaths(plan).Any();

    public static IEnumerable<string> GetSuccessfulCleanupSourcePaths(BulkConvertPlan plan, BulkConvertRunResult run)
    {
        foreach (IGrouping<string, BulkConvertItem> group in plan.Items.GroupBy(x => x.SourcePath, StringComparer.OrdinalIgnoreCase))
        {
            List<BulkConvertItem> planned = group.ToList();
            // Never recycle a shared DB while any stream in it was unselected or unresolved.
            if (planned.Any(x => !x.IsReady || !x.Selected)) continue;

            bool allSucceeded = planned.All(p => run.Items.Any(r =>
                r.Success && !r.Skipped &&
                string.Equals(r.SourcePath, p.SourcePath, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.SourceSaveKey ?? "", p.SourceSaveKey ?? "", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.OutputSidecarPath, p.OutputSidecarPath, StringComparison.OrdinalIgnoreCase)));
            if (allSucceeded) yield return group.Key;
        }
    }

    public static bool IsStrictOrphan(BulkConvertItem item) =>
        item.Action == BulkConvertAction.Skip &&
        string.Equals(item.Status, "No vanilla match", StringComparison.Ordinal) &&
        string.IsNullOrWhiteSpace(item.VanillaSavePath) &&
        item.MatchScore == 0;

    public static void WritePlanJson(BulkConvertPlan plan, string outputPath)
    {
        var payload = new
        {
            schema = "IMDataCore.DataMigrationTool.BulkConvertPlan",
            schemaVersion = 1,
            generatedUtc = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
            toolVersion = ToolInfo.Version,
            legacyRoot = plan.LegacyRoot,
            dataRoot = plan.DataRoot,
            outputRoot = plan.OutputRoot,
            legacyFilesDiscovered = plan.LegacyFilesDiscovered,
            vanillaJsonFilesExamined = plan.VanillaJsonFilesExamined,
            vanillaSavesRecognized = plan.VanillaSavesRecognized,
            readyCount = plan.ReadyCount,
            skippedCount = plan.SkippedCount,
            contaminatedCount = plan.ContaminatedCount,
            items = plan.Items.Select(x => new
            {
                selected = x.Selected,
                status = x.Status,
                action = x.Action.ToString(),
                sourcePath = x.SourcePath,
                sourceRelativePath = x.SourceRelativePath,
                saveKey = x.SourceSaveKey,
                backend = x.BackendKind,
                checkpointSupport = x.HasCheckpointSupport,
                legacyEventCount = x.LegacyEventCount,
                contaminated = x.IsContaminated,
                branchCount = x.BranchCount,
                vanillaSavePath = x.VanillaSavePath,
                vanillaRelativePath = x.VanillaRelativePath,
                matchScore = x.MatchScore,
                matchKind = x.MatchKind,
                outputSidecarPath = x.OutputSidecarPath,
                repairBranchIndex = x.RepairBranchIndex,
                repairConfidence = x.RepairConfidence,
                destinationExists = x.DestinationExists,
                manualMatch = x.ManualMatch,
                details = x.Details
            }).ToArray(),
            errors = plan.Errors.ToArray()
        };
        File.WriteAllText(outputPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
    }

    public static void WriteRunJson(BulkConvertRunResult result, string outputPath)
    {
        var payload = new
        {
            schema = "IMDataCore.DataMigrationTool.BulkConvertResult",
            schemaVersion = 1,
            generatedUtc = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
            toolVersion = ToolInfo.Version,
            dryRun = result.DryRun,
            successCount = result.SuccessCount,
            skippedCount = result.SkippedCount,
            failedCount = result.FailedCount,
            cleanupSuccessCount = result.CleanupSuccessCount,
            cleanupFailedCount = result.CleanupFailedCount,
            items = result.Items
        };
        File.WriteAllText(outputPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
    }

    public IReadOnlyList<VanillaSaveCandidate> GetCandidates(BulkConvertPlan plan, BulkConvertItem item)
    {
        LegacyInspection inspection = _migration.InspectLegacy(item.SourcePath);
        IReadOnlyList<VanillaSaveInfo> saves = VanillaReverseMatcher.LoadRecognizedSaves(plan.DataRoot, out int examined);
        ReverseMatchScanResult matched = VanillaReverseMatcher.MatchCached(inspection, plan.DataRoot, item.SourceSaveKey, saves, examined);
        var byPath = matched.Candidates
            .GroupBy(x => x.SaveInfo.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Score).First(), StringComparer.OrdinalIgnoreCase);

        foreach (VanillaSaveInfo save in saves)
        {
            if (byPath.ContainsKey(save.FilePath)) continue;
            byPath[save.FilePath] = new VanillaSaveCandidate
            {
                SaveInfo = save,
                MatchedSaveKey = item.SourceSaveKey ?? "",
                MatchKind = "manual_review",
                Score = 0
            };
        }

        return byPath.Values
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.SaveInfo.LastSave, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.SaveInfo.RelativeSavePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public BulkConvertItem ResolveManualMatch(
        BulkConvertPlan plan,
        BulkConvertItem item,
        string vanillaSavePath,
        BulkConvertContaminatedPolicy policy)
    {
        LegacyInspection inspection = _migration.InspectLegacy(item.SourcePath);
        IReadOnlyList<VanillaSaveCandidate> candidates = GetCandidates(plan, item);
        VanillaSaveCandidate? candidate = candidates.FirstOrDefault(x =>
            string.Equals(Path.GetFullPath(x.SaveInfo.FilePath), Path.GetFullPath(vanillaSavePath), StringComparison.OrdinalIgnoreCase));

        if (candidate is null)
        {
            VanillaSaveInfo info = VanillaSaveReader.ReadForIdentification(vanillaSavePath);
            candidate = new VanillaSaveCandidate
            {
                SaveInfo = info,
                MatchedSaveKey = item.SourceSaveKey ?? "",
                MatchKind = "manual_selected",
                Score = 0
            };
        }

        return BuildResolvedItem(
            plan,
            item.SourcePath,
            item.SourceRelativePath,
            item.SourceSaveKey,
            inspection,
            item.LegacyEventCount,
            item.IsContaminated,
            item.BranchCount,
            candidate,
            policy,
            manualMatch: true);
    }

    private BulkConvertItem BuildItem(
        BulkConvertPlan plan,
        LegacyContaminationEntry entry,
        LegacyInspection inspection,
        IReadOnlyList<VanillaSaveInfo> cachedSaves,
        int examined,
        BulkConvertContaminatedPolicy policy)
    {
        string? saveKey = NullIfPlaceholder(entry.SaveKey);
        ReverseMatchScanResult match = VanillaReverseMatcher.MatchCached(inspection, plan.DataRoot, saveKey, cachedSaves, examined);
        List<VanillaSaveCandidate> uniqueCandidates = match.Candidates
            .GroupBy(x => x.SaveInfo.FilePath, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.Score).First())
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.SaveInfo.RelativeSavePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (uniqueCandidates.Count == 0)
            return Skip(entry, inspection, saveKey, "No vanilla match", "No vanilla save candidate matched this legacy stream. Use Resolve match to review all recognized vanilla saves or choose one manually.");

        VanillaSaveCandidate best = uniqueCandidates[0];
        int secondScore = uniqueCandidates.Count > 1 ? uniqueCandidates[1].Score : int.MinValue;
        if (best.Score < MinimumAutomaticMatchScore)
            return Skip(entry, inspection, saveKey, "Weak vanilla match", $"Best reverse match score was {best.Score}; unattended bulk conversion requires at least {MinimumAutomaticMatchScore}. Use Resolve match to confirm this candidate or choose another save.", best);
        if (secondScore == best.Score)
            return Skip(entry, inspection, saveKey, "Ambiguous vanilla match", $"More than one vanilla save tied at score {best.Score}. Use Resolve match to choose the correct save explicitly.", best);

        return BuildResolvedItem(
            plan,
            entry.SourcePath,
            entry.SourceRelativePath,
            saveKey,
            inspection,
            entry.EventCount,
            entry.IsContaminated,
            entry.BranchCount,
            best,
            policy,
            manualMatch: false);
    }

    private BulkConvertItem BuildResolvedItem(
        BulkConvertPlan plan,
        string sourcePath,
        string sourceRelativePath,
        string? saveKey,
        LegacyInspection inspection,
        int legacyEventCount,
        bool isContaminated,
        int branchCount,
        VanillaSaveCandidate candidate,
        BulkConvertContaminatedPolicy policy,
        bool manualMatch)
    {
        VanillaSaveInfo vanilla = VanillaSaveReader.Read(candidate.SaveInfo.FilePath);
        MigrationPlan migrationPlan = _migration.BuildPlan(sourcePath, vanilla.FilePath, plan.OutputRoot, saveKey, null, allowUnverified: isContaminated);
        BulkConvertAction action;
        int? repairBranch = null;
        string repairConfidence = "";
        string details;
        string status;

        if (isContaminated)
        {
            if (policy == BulkConvertContaminatedPolicy.Skip)
                return SkipResolved(sourcePath, sourceRelativePath, saveKey, inspection, legacyEventCount, true, branchCount, candidate,
                    "Contaminated: skipped", "Backwards-time branch boundary detected and bulk policy is Skip contaminated files.", migrationPlan.OutputSidecarPath, manualMatch);

            if (policy == BulkConvertContaminatedPolicy.CleanBaseline)
            {
                MigrationReport validation = _migration.ValidateRepairPlan(migrationPlan, LegacyRepairMode.CleanBaseline, null);
                if (!validation.Success)
                    return SkipResolved(sourcePath, sourceRelativePath, saveKey, inspection, legacyEventCount, true, branchCount, candidate,
                        "Repair validation failed", validation.ToMultilineString(), migrationPlan.OutputSidecarPath, manualMatch);
                action = BulkConvertAction.CleanBaseline;
                status = "Ready: clean baseline";
                details = $"Detected {branchCount} branches. Bulk policy forces clean-baseline repair, which discards legacy event/custom state and creates an exact v6 checkpoint.";
            }
            else
            {
                LegacyRepairAnalysis repair = _migration.AnalyzeRepair(migrationPlan);
                if (repair.CanConservativeSalvage && repair.RecommendedBranchIndex.HasValue)
                {
                    MigrationReport validation = _migration.ValidateRepairPlan(migrationPlan, LegacyRepairMode.ConservativeBranchSalvage, repair.RecommendedBranchIndex);
                    if (validation.Success)
                    {
                        action = BulkConvertAction.ConservativeBranchSalvage;
                        repairBranch = repair.RecommendedBranchIndex;
                        repairConfidence = repair.RecommendationConfidence;
                        status = "Ready: branch salvage";
                        details = repair.RecommendationReason + Environment.NewLine + validation.Summary;
                    }
                    else
                    {
                        MigrationReport clean = _migration.ValidateRepairPlan(migrationPlan, LegacyRepairMode.CleanBaseline, null);
                        if (!clean.Success)
                            return SkipResolved(sourcePath, sourceRelativePath, saveKey, inspection, legacyEventCount, true, branchCount, candidate,
                                "Repair validation failed", validation.ToMultilineString() + Environment.NewLine + clean.ToMultilineString(), migrationPlan.OutputSidecarPath, manualMatch);
                        action = BulkConvertAction.CleanBaseline;
                        status = "Ready: clean baseline fallback";
                        details = "Branch salvage validation failed; clean-baseline repair validated successfully. " + validation.Summary;
                    }
                }
                else
                {
                    MigrationReport clean = _migration.ValidateRepairPlan(migrationPlan, LegacyRepairMode.CleanBaseline, null);
                    if (!clean.Success)
                        return SkipResolved(sourcePath, sourceRelativePath, saveKey, inspection, legacyEventCount, true, branchCount, candidate,
                            "Repair validation failed", clean.ToMultilineString(), migrationPlan.OutputSidecarPath, manualMatch);
                    action = BulkConvertAction.CleanBaseline;
                    repairConfidence = repair.RecommendationConfidence;
                    status = "Ready: clean baseline fallback";
                    details = repair.RecommendationReason;
                }
            }
        }
        else
        {
            MigrationReport validation = _migration.ValidatePlan(migrationPlan);
            if (validation.Success)
            {
                action = BulkConvertAction.NormalMigration;
                status = "Ready: normal migration";
                details = validation.Summary;
            }
            else
            {
                // IMDataCore 1.x could persist after the vanilla slot's last explicit save. A monotonic
                // single-branch tail beyond the selected save date is therefore not itself evidence of
                // cross-save contamination. Safely clip the history to the vanilla checkpoint and drop
                // mutable custom_data, which cannot be rolled back to the older vanilla save.
                LegacyRepairAnalysis repair = _migration.AnalyzeRepair(migrationPlan);
                LegacyRepairBranch? onlyBranch = repair.Branches.Count == 1 ? repair.Branches[0] : null;
                if (!repair.HasDivergence && onlyBranch is not null && onlyBranch.FutureEventCount > 0 && onlyBranch.EligibleEventCount > 0)
                {
                    MigrationReport clipped = _migration.ValidateRepairPlan(migrationPlan, LegacyRepairMode.ConservativeBranchSalvage, 1);
                    if (clipped.Success)
                    {
                        action = BulkConvertAction.ConservativeBranchSalvage;
                        repairBranch = 1;
                        repairConfidence = "timeline";
                        status = "Ready: future-tail truncation";
                        details =
                            $"The legacy timeline is monotonic but contains {onlyBranch.FutureEventCount:N0} event(s) after the selected vanilla save date. " +
                            "IMDataCore 1.x could persist independently of an explicit vanilla slot save, so this is not treated as branch contamination. " +
                            "The Data Migration Tool will retain this single branch only through the vanilla save date and discard legacy mutable custom-data state that cannot be rolled back safely." +
                            Environment.NewLine + clipped.Summary;
                    }
                    else
                    {
                        return SkipResolved(sourcePath, sourceRelativePath, saveKey, inspection, legacyEventCount, false, branchCount, candidate,
                            "Validation failed", validation.ToMultilineString() + Environment.NewLine + clipped.ToMultilineString(), migrationPlan.OutputSidecarPath, manualMatch);
                    }
                }
                else
                {
                    return SkipResolved(sourcePath, sourceRelativePath, saveKey, inspection, legacyEventCount, false, branchCount, candidate,
                        "Validation failed", validation.ToMultilineString(), migrationPlan.OutputSidecarPath, manualMatch);
                }
            }
        }

        bool destinationExists = HasLiveArtifacts(migrationPlan.OutputSidecarPath);
        if (destinationExists) status += " (destination exists)";
        if (manualMatch) status += " (manual match)";
        return new BulkConvertItem
        {
            Selected = true,
            SourcePath = sourcePath,
            SourceRelativePath = sourceRelativePath,
            SourceSaveKey = saveKey,
            BackendKind = inspection.BackendKind,
            HasCheckpointSupport = inspection.HasCheckpointSupport,
            LegacyEventCount = legacyEventCount,
            IsContaminated = isContaminated,
            BranchCount = branchCount,
            VanillaSavePath = vanilla.FilePath,
            VanillaRelativePath = vanilla.RelativeSavePath,
            VanillaDisplay = DisplaySave(vanilla),
            MatchScore = candidate.Score,
            MatchKind = manualMatch ? candidate.MatchKind + "+manual" : candidate.MatchKind,
            OutputSidecarPath = migrationPlan.OutputSidecarPath,
            Action = action,
            RepairBranchIndex = repairBranch,
            RepairConfidence = repairConfidence,
            Status = status,
            Details = details,
            DestinationExists = destinationExists,
            ManualMatch = manualMatch
        };
    }

    private static BulkConvertItem SkipResolved(
        string sourcePath,
        string sourceRelativePath,
        string? saveKey,
        LegacyInspection inspection,
        int legacyEventCount,
        bool isContaminated,
        int branchCount,
        VanillaSaveCandidate candidate,
        string status,
        string details,
        string outputPath,
        bool manualMatch)
    {
        return new BulkConvertItem
        {
            Selected = false,
            SourcePath = sourcePath,
            SourceRelativePath = sourceRelativePath,
            SourceSaveKey = saveKey,
            BackendKind = inspection.BackendKind,
            HasCheckpointSupport = inspection.HasCheckpointSupport,
            LegacyEventCount = legacyEventCount,
            IsContaminated = isContaminated,
            BranchCount = branchCount,
            VanillaSavePath = candidate.SaveInfo.FilePath,
            VanillaRelativePath = candidate.SaveInfo.RelativeSavePath,
            VanillaDisplay = DisplaySave(candidate.SaveInfo),
            MatchScore = candidate.Score,
            MatchKind = manualMatch ? candidate.MatchKind + "+manual" : candidate.MatchKind,
            OutputSidecarPath = outputPath,
            Action = BulkConvertAction.Skip,
            Status = status,
            Details = details,
            DestinationExists = !string.IsNullOrWhiteSpace(outputPath) && HasLiveArtifacts(outputPath),
            ManualMatch = manualMatch
        };
    }

    private static BulkConvertItem Skip(
        LegacyContaminationEntry entry,
        LegacyInspection inspection,
        string? saveKey,
        string status,
        string details,
        VanillaSaveCandidate? candidate = null,
        string outputPath = "")
    {
        return new BulkConvertItem
        {
            Selected = false,
            SourcePath = entry.SourcePath,
            SourceRelativePath = entry.SourceRelativePath,
            SourceSaveKey = saveKey,
            BackendKind = inspection.BackendKind,
            HasCheckpointSupport = inspection.HasCheckpointSupport,
            LegacyEventCount = entry.EventCount,
            IsContaminated = entry.IsContaminated,
            BranchCount = entry.BranchCount,
            VanillaSavePath = candidate?.SaveInfo.FilePath ?? "",
            VanillaRelativePath = candidate?.SaveInfo.RelativeSavePath ?? "",
            VanillaDisplay = candidate is null ? "" : DisplaySave(candidate.SaveInfo),
            MatchScore = candidate?.Score ?? 0,
            MatchKind = candidate?.MatchKind ?? "",
            OutputSidecarPath = outputPath,
            Action = BulkConvertAction.Skip,
            Status = status,
            Details = details,
            DestinationExists = !string.IsNullOrWhiteSpace(outputPath) && HasLiveArtifacts(outputPath)
        };
    }

    private static void ResolveOutputCollisions(List<BulkConvertItem> items)
    {
        foreach (IGrouping<string, BulkConvertItem> group in items
                     .Where(x => x.IsReady)
                     .GroupBy(x => x.OutputSidecarPath, StringComparer.OrdinalIgnoreCase)
                     .Where(g => g.Count() > 1))
        {
            static int Priority(BulkConvertItem x) => (x.MatchScore * 10) +
                (string.Equals(x.BackendKind, "sqlite", StringComparison.OrdinalIgnoreCase) ? 2 : 0) +
                (x.HasCheckpointSupport ? 1 : 0);
            List<BulkConvertItem> candidates = group.OrderByDescending(Priority).ToList();
            BulkConvertItem best = candidates[0];
            int bestPriority = Priority(best);
            int secondPriority = Priority(candidates[1]);
            if (bestPriority > secondPriority)
            {
                best.Details += Environment.NewLine + $"Bulk collision resolution: preferred this legacy stream over {candidates.Count - 1} weaker source(s) targeting the same v6 output (match confidence, SQLite preference, then checkpoint support).";
                foreach (BulkConvertItem loser in candidates.Skip(1))
                {
                    loser.Selected = false;
                    loser.Action = BulkConvertAction.Skip;
                    loser.Status = "Superseded legacy source";
                    loser.Details += Environment.NewLine + $"A stronger legacy source targets the same vanilla save/output. This stream was skipped to prevent two writes to one sidecar.";
                }
            }
            else
            {
                foreach (BulkConvertItem item in candidates)
                {
                    item.Selected = false;
                    item.Action = BulkConvertAction.Skip;
                    item.Status = "Output collision: manual review";
                    item.Details += Environment.NewLine + "Multiple equally strong legacy streams target the same vanilla save/output. Bulk conversion will not choose between them.";
                }
            }
        }
    }

    private static string DisplaySave(VanillaSaveInfo info)
    {
        string label = string.IsNullOrWhiteSpace(info.SaveDisplayName) ? info.GroupName : info.SaveDisplayName;
        if (string.IsNullOrWhiteSpace(label)) label = info.SlotToken;
        return string.Join(" • ", new[] { label, info.PlayerName, info.RelativeSavePath }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string? NullIfPlaceholder(string key) =>
        string.IsNullOrWhiteSpace(key) || string.Equals(key, "(no save_key)", StringComparison.OrdinalIgnoreCase) ? null : key;

    private static bool HasLiveArtifacts(string sidecar) =>
        File.Exists(sidecar) || File.Exists(sidecar + ".imdc.journal") || File.Exists(sidecar + ".imdc.bak") || File.Exists(sidecar + ".imdc.bak.imdc.journal");
}
