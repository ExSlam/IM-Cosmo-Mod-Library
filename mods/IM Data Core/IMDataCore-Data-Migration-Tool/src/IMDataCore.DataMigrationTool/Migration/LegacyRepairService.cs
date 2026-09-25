using System.Text;

namespace IMDataCore.DataMigrationTool.Migration;

internal static class LegacyRepairService
{
    private sealed class BranchWork
    {
        public int Index { get; init; }
        public List<LegacyEvent> Events { get; } = new();
        public LegacyRepairBranch Summary { get; set; } = new();
    }

    public static LegacyRepairAnalysis Analyze(MigrationPlan plan)
    {
        LegacySnapshot source = ReadUnverified(plan);
        return AnalyzeSnapshot(plan, source, source.CheckpointStatus != "not_available");
    }

    private static LegacyRepairAnalysis AnalyzeSnapshot(MigrationPlan plan, LegacySnapshot source, bool hasCheckpointSupport)
    {
        if (!GameDateUtilities.TryParseVanillaOrRoundTrip(plan.Vanilla.GameDateTime, out DateTime saveDate))
            throw new InvalidDataException("Selected vanilla save has an invalid game date and cannot be used for branch repair.");

        LegacyContaminationEntry contamination = LegacyContaminationScanner.AnalyzeTimeline(
            plan.SourcePath,
            source.BackendKind,
            source.SourceSaveKey,
            hasCheckpointSupport,
            source.Events);

        List<BranchWork> branches = BuildBranches(source.Events, plan.Vanilla, saveDate);
        LegacyRepairBranch? best = branches
            .Select(x => x.Summary)
            .Where(x => x.EligibleEventCount > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.DirectIdentityMatches)
            .ThenBy(x => x.DirectIdentityMismatches)
            .ThenByDescending(x => x.Index)
            .FirstOrDefault();

        LegacyRepairBranch? runner = branches
            .Select(x => x.Summary)
            .Where(x => x.EligibleEventCount > 0 && (best is null || x.Index != best.Index))
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.DirectIdentityMatches)
            .FirstOrDefault();

        string confidence = "low";
        int? recommended = null;
        string reason;

        if (!contamination.IsContaminated)
        {
            reason = "No backwards-time branch boundary was detected. Ordinary migration is preferred; clean-baseline repair remains available if you intentionally want to discard legacy state.";
        }
        else if (best is null)
        {
            reason = "Branch boundaries were detected, but no branch has valid events at or before the selected vanilla save date. Use clean-baseline repair.";
        }
        else
        {
            int runnerScore = runner?.Score ?? int.MinValue / 4;
            bool clearLead = runner is null || best.Score - runnerScore >= 10;
            double evidenceRatio = (best.DirectIdentityMatches + best.DirectIdentityMismatches) == 0
                ? 0
                : (double)best.DirectIdentityMatches / (best.DirectIdentityMatches + best.DirectIdentityMismatches);

            if (best.DirectIdentityMatches > 0 && best.DirectIdentityMismatches == 0 && clearLead)
                confidence = "high";
            else if (best.DirectIdentityMatches >= 2 && evidenceRatio >= 0.70 && best.Score > runnerScore)
                confidence = "medium";

            if (confidence == "high" || confidence == "medium")
            {
                recommended = best.Index;
                reason = $"Branch {best.Index} is the strongest match to identities present in the selected vanilla save ({best.DirectIdentityMatches} direct match(es), {best.DirectIdentityMismatches} mismatch(es)). Conservative salvage can retain only this branch through the vanilla save date.";
            }
            else
            {
                reason = "Branch boundaries were detected, but the selected vanilla save does not provide enough direct identity evidence to choose a branch confidently. Clean-baseline repair is the safe default; manual branch salvage is available as an advanced choice.";
            }
        }

        return new LegacyRepairAnalysis
        {
            SourcePath = plan.SourcePath,
            SaveKey = source.SourceSaveKey,
            SelectedSaveGameDateTime = plan.Vanilla.GameDateTime,
            OriginalEventCount = source.Events.Count,
            OriginalCustomDataCount = source.CustomData.Count,
            HasCheckpointSupport = hasCheckpointSupport,
            HasDivergence = contamination.IsContaminated,
            RecommendedBranchIndex = recommended,
            RecommendationConfidence = confidence,
            RecommendationReason = reason,
            Branches = branches.Select(x => x.Summary).ToList(),
            Divergences = contamination.Divergences.ToList()
        };
    }

    public static LegacySnapshot BuildRepairedSnapshot(MigrationPlan plan, LegacyRepairMode mode, int? branchIndex)
    {
        LegacySnapshot source = ReadUnverified(plan);
        LegacyRepairAnalysis analysis = AnalyzeSnapshot(plan, source, source.CheckpointStatus != "not_available");
        int originalEvents = source.Events.Count;
        int originalCustom = source.CustomData.Count;

        if (mode != LegacyRepairMode.ConservativeBranchSalvage && mode != LegacyRepairMode.CleanBaseline)
            throw new InvalidDataException("Unsupported branch repair mode.");

        if (mode == LegacyRepairMode.CleanBaseline)
        {
            return CloneWithRepair(
                source,
                events: new List<LegacyEvent>(),
                nextEventId: 1,
                mode: "clean_baseline",
                summary: "Discarded all legacy event history and mutable custom-data state; emitted only a fresh exact v6 checkpoint bound to the selected vanilla save.",
                branchIndex: 0,
                branchStart: 0,
                branchEnd: 0,
                originalEvents: originalEvents,
                originalCustom: originalCustom);
        }

        int selectedBranch = branchIndex ?? analysis.RecommendedBranchIndex
            ?? throw new InvalidDataException("No branch was selected and the Data Migration Tool could not recommend one confidently. Choose a branch explicitly or use clean-baseline repair.");
        LegacyRepairBranch summary = analysis.Branches.FirstOrDefault(x => x.Index == selectedBranch)
            ?? throw new InvalidDataException("Selected repair branch does not exist.");

        if (!GameDateUtilities.TryParseVanillaOrRoundTrip(plan.Vanilla.GameDateTime, out DateTime saveDate))
            throw new InvalidDataException("Selected vanilla save has an invalid game date and cannot be used for branch repair.");

        var retained = new List<LegacyEvent>();
        foreach (LegacyEvent e in source.Events.OrderBy(x => x.EventId))
        {
            if (e.EventId < summary.StartEventId || e.EventId > summary.EndEventId) continue;
            if (!GameDateUtilities.TryParseVanillaOrRoundTrip(e.GameDateTime, out DateTime eventDate)) continue;
            if (eventDate > saveDate) continue;
            retained.Add(NormalizeEventDate(e, eventDate));
        }

        if (retained.Count == 0)
            throw new InvalidDataException("The selected branch has no valid legacy events at or before the vanilla save date. Use clean-baseline repair instead.");

        long nextEventId = retained.Max(x => x.EventId) + 1;
        string confidence = analysis.RecommendedBranchIndex == selectedBranch ? analysis.RecommendationConfidence : "manual";
        string repairSummary =
            $"Salvaged branch {selectedBranch} ({confidence} selection): retained {retained.Count:N0} event(s) from event {summary.StartEventId} through {summary.EndEventId}, clipped at vanilla game date {plan.Vanilla.GameDateTime}. " +
            "All legacy custom-data rows were dropped because pre-v6 1.x custom_data stored only mutable current values and cannot be safely assigned to a historical branch.";

        return CloneWithRepair(
            source,
            retained,
            nextEventId,
            "conservative_branch_salvage",
            repairSummary,
            selectedBranch,
            summary.StartEventId,
            summary.EndEventId,
            originalEvents,
            originalCustom);
    }

    private static LegacySnapshot CloneWithRepair(
        LegacySnapshot source,
        List<LegacyEvent> events,
        long nextEventId,
        string mode,
        string summary,
        int branchIndex,
        long branchStart,
        long branchEnd,
        int originalEvents,
        int originalCustom)
    {
        var warnings = new List<string>(source.Warnings)
        {
            "IMDataCore Data Migration Tool branch repair was applied. The repair is conservative and does not invent missing historical events.",
            summary
        };
        if (originalCustom > 0)
            warnings.Add($"Dropped {originalCustom:N0} legacy custom-data row(s) because 1.x did not retain branch-versioned historical values.");

        return new LegacySnapshot
        {
            BackendKind = source.BackendKind,
            DetectedVersion = source.DetectedVersion,
            DetectionNote = source.DetectionNote,
            SourceSaveKey = source.SourceSaveKey,
            CheckpointStatus = "data_migration_tool_repair_" + mode,
            NextEventId = nextEventId,
            Events = events,
            CustomData = new List<LegacyCustomData>(),
            ProjectionCounts = new Dictionary<string, int>(source.ProjectionCounts, StringComparer.Ordinal),
            Warnings = warnings,
            RepairApplied = true,
            RepairMode = mode,
            RepairSummary = summary,
            RepairBranchIndex = branchIndex,
            RepairBranchStartEventId = branchStart,
            RepairBranchEndEventId = branchEnd,
            RepairOriginalEventCount = originalEvents,
            RepairDroppedEventCount = originalEvents - events.Count,
            RepairOriginalCustomDataCount = originalCustom,
            RepairDroppedCustomDataCount = originalCustom
        };
    }

    private static List<BranchWork> BuildBranches(IReadOnlyList<LegacyEvent> events, VanillaSaveInfo vanilla, DateTime saveDate)
    {
        var result = new List<BranchWork>();
        BranchWork current = new() { Index = 1 };
        result.Add(current);
        DateTime? previousParsed = null;

        foreach (LegacyEvent e in events.OrderBy(x => x.EventId))
        {
            bool parsed = GameDateUtilities.TryParseVanillaOrRoundTrip(e.GameDateTime, out DateTime date);
            if (parsed && previousParsed.HasValue && date < previousParsed.Value && current.Events.Count > 0)
            {
                current = new BranchWork { Index = result.Count + 1 };
                result.Add(current);
            }
            current.Events.Add(e);
            if (parsed) previousParsed = date;
        }

        foreach (BranchWork branch in result)
            branch.Summary = SummarizeBranch(branch.Index, branch.Events, vanilla, saveDate);
        return result;
    }

    private static LegacyRepairBranch SummarizeBranch(int index, IReadOnlyList<LegacyEvent> events, VanillaSaveInfo vanilla, DateTime saveDate)
    {
        int eligible = 0, future = 0, invalid = 0, matches = 0, mismatches = 0, unknown = 0;
        string start = "", end = "";

        foreach (LegacyEvent e in events)
        {
            if (!GameDateUtilities.TryParseVanillaOrRoundTrip(e.GameDateTime, out DateTime date))
            {
                invalid++;
                continue;
            }
            if (string.IsNullOrEmpty(start)) start = e.GameDateTime;
            end = e.GameDateTime;
            if (date > saveDate)
            {
                future++;
                continue;
            }

            eligible++;
            (int matched, int mismatched) = IdentityEvidence(e, vanilla);
            matches += matched;
            mismatches += mismatched;
            if (matched == 0 && mismatched == 0) unknown++;
        }

        int score = matches * 20 - mismatches * 8;
        if (matches > 0) score += 5;
        if (matches > 0 && mismatches == 0) score += 15;
        if (eligible > 0) score += Math.Min(5, eligible / 20);
        if (eligible == 0) score -= 100;

        double ratio = matches + mismatches == 0 ? 0 : (double)matches / (matches + mismatches);
        string confidence = matches > 0 && mismatches == 0 ? "strong" : matches >= 2 && ratio >= 0.70 ? "moderate" : "weak";

        return new LegacyRepairBranch
        {
            Index = index,
            StartEventId = events.Count == 0 ? 0 : events.Min(x => x.EventId),
            EndEventId = events.Count == 0 ? 0 : events.Max(x => x.EventId),
            StartGameDateTime = start,
            EndGameDateTime = end,
            EventCount = events.Count,
            EligibleEventCount = eligible,
            FutureEventCount = future,
            InvalidDateCount = invalid,
            DirectIdentityMatches = matches,
            DirectIdentityMismatches = mismatches,
            UnknownIdentityEvents = unknown,
            Score = score,
            Confidence = confidence
        };
    }

    private static LegacyEvent NormalizeEventDate(LegacyEvent e, DateTime parsed)
    {
        return new LegacyEvent
        {
            EventId = e.EventId,
            GameDateKey = checked(parsed.Year * 10000 + parsed.Month * 100 + parsed.Day),
            GameDateTime = parsed.ToString(GameDateUtilities.RoundTripFormat, System.Globalization.CultureInfo.InvariantCulture),
            IdolId = e.IdolId,
            EntityKind = e.EntityKind,
            EntityId = e.EntityId,
            EventType = e.EventType,
            SourcePatch = e.SourcePatch,
            NamespaceIdentifier = e.NamespaceIdentifier,
            PayloadJson = e.PayloadJson
        };
    }

    private static (int matches, int mismatches) IdentityEvidence(LegacyEvent e, VanillaSaveInfo vanilla)
    {
        int matches = 0, mismatches = 0;
        if (e.IdolId >= 0)
        {
            if (vanilla.IdolIds.Contains(e.IdolId)) matches++;
            else mismatches++;
        }

        if (!int.TryParse(e.EntityId, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int entityId))
            return (matches, mismatches);

        HashSet<int>? set = e.EntityKind.Trim().ToLowerInvariant() switch
        {
            "idol" or "girl" => vanilla.IdolIds,
            "staff" => vanilla.StaffIds,
            "single" => vanilla.SingleIds,
            "show" => vanilla.ShowIds,
            _ => null
        };
        if (set is not null)
        {
            if (set.Contains(entityId)) matches++;
            else mismatches++;
        }
        return (matches, mismatches);
    }

    private static LegacySnapshot ReadUnverified(MigrationPlan plan) => IsSqlite(plan.SourcePath)
        ? LegacySqliteReader.Read(plan.SourcePath, plan.Vanilla, plan.SourceSaveKey, allowUnverified: true)
        : LegacyFlatJsonReader.Read(plan.SourcePath, plan.Vanilla, allowUnverified: true);

    private static bool IsSqlite(string path)
    {
        using FileStream fs = File.OpenRead(path);
        byte[] header = new byte[Math.Min(16, checked((int)Math.Min(fs.Length, 16)))];
        if (header.Length > 0) fs.ReadExactly(header);
        return Encoding.ASCII.GetString(header).StartsWith("SQLite format 3\0", StringComparison.Ordinal);
    }
}
