using System.Text;
using System.Text.Json.Nodes;

namespace IMDataCore.DataMigrationTool.Migration;

internal sealed class LegacyEvent
{
    public long EventId { get; init; }
    public int GameDateKey { get; init; }
    public string GameDateTime { get; init; } = "";
    public int IdolId { get; init; } = -1;
    public string EntityKind { get; init; } = "";
    public string EntityId { get; init; } = "";
    public string EventType { get; init; } = "";
    public string SourcePatch { get; init; } = "";
    public string NamespaceIdentifier { get; init; } = "";
    public string PayloadJson { get; init; } = "{}";
}

internal sealed class LegacyCustomData
{
    public string NamespaceIdentifier { get; init; } = "";
    public string DataKey { get; init; } = "";
    public string ValueJson { get; init; } = "{}";
    public string UpdatedUtc { get; init; } = "";
}

internal sealed class LegacySnapshot
{
    public string BackendKind { get; init; } = "unknown";
    public string DetectedVersion { get; init; } = "1.x";
    public string DetectionNote { get; init; } = "";
    public string SourceSaveKey { get; init; } = "";
    public string CheckpointStatus { get; init; } = "not_available";
    public long NextEventId { get; init; } = 1;
    public List<LegacyEvent> Events { get; init; } = new();
    public List<LegacyCustomData> CustomData { get; init; } = new();
    public Dictionary<string, int> ProjectionCounts { get; init; } = new(StringComparer.Ordinal);
    public List<string> Warnings { get; init; } = new();
    public bool RepairApplied { get; init; }
    public string RepairMode { get; init; } = "";
    public string RepairSummary { get; init; } = "";
    public int RepairBranchIndex { get; init; }
    public long RepairBranchStartEventId { get; init; }
    public long RepairBranchEndEventId { get; init; }
    public int RepairOriginalEventCount { get; init; }
    public int RepairDroppedEventCount { get; init; }
    public int RepairOriginalCustomDataCount { get; init; }
    public int RepairDroppedCustomDataCount { get; init; }
}

internal sealed class LegacyInspection
{
    public string SourcePath { get; init; } = "";
    public string BackendKind { get; init; } = "unknown";
    public string DetectedVersion { get; init; } = "1.x";
    public string DetectionNote { get; init; } = "";
    public List<string> SaveKeys { get; init; } = new();
    public bool HasCheckpointSupport { get; init; }
    public string SourceSha256 { get; init; } = "";
    public List<string> Warnings { get; init; } = new();

    public string ToMultilineString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Source: {SourcePath}");
        sb.AppendLine($"Backend: {BackendKind}");
        sb.AppendLine($"Detected IMDataCore version: {DetectedVersion}");
        if (!string.IsNullOrWhiteSpace(DetectionNote)) sb.AppendLine("Detection: " + DetectionNote);
        sb.AppendLine($"Checkpoint support: {(HasCheckpointSupport ? "yes" : "no")}");
        sb.AppendLine("Source SHA-256: " + SourceSha256);
        if (SaveKeys.Count > 0) sb.AppendLine("Save keys: " + string.Join(", ", SaveKeys));
        foreach (string warning in Warnings) sb.AppendLine("WARNING: " + warning);
        return sb.ToString().TrimEnd();
    }
}

internal sealed class VanillaSaveInfo
{
    public string FilePath { get; init; } = "";
    public string PersistentRoot { get; init; } = "";
    public string DataRoot { get; init; } = "";
    public string RelativeSavePath { get; init; } = "";
    public string GameVersion { get; init; } = "";
    public string SaveDisplayName { get; init; } = "";
    public string SaveFolderName { get; init; } = "";
    public string PlayerFirstName { get; init; } = "";
    public string PlayerLastName { get; init; } = "";
    public string GroupName { get; init; } = "";
    public bool IsStoryMode { get; init; }
    public string ChapterName { get; init; } = "";
    public string DifficultyName { get; init; } = "";
    public string SaveKind { get; init; } = "";
    public string SlotToken { get; init; } = "";
    public string LastSave { get; init; } = "";
    public long PlaytimeSeconds { get; init; }
    public string GameDateTime { get; init; } = "";
    public int IdolCount { get; init; }
    public int StaffCount { get; init; }
    public int SingleCount { get; init; }
    public int ShowCount { get; init; }
    public HashSet<int> IdolIds { get; init; } = new();
    public HashSet<int> StaffIds { get; init; } = new();
    public HashSet<int> SingleIds { get; init; } = new();
    public HashSet<int> ShowIds { get; init; } = new();
    public string LegacySavesRoot { get; init; } = "";
    public string LegacyFileSaveKeyCandidate { get; init; } = "";
    public string LegacyAgencySaveKeyCandidate { get; init; } = "";
    public string LegacyAgencyFallbackKeyCandidate { get; init; } = "";
    public string V6ContentFingerprint { get; init; } = "";
    public string LegacyFileFingerprint { get; init; } = "";
    public string RawSha256 { get; init; } = "";
    public long RawLength { get; init; }

    public string PlayerName => string.Join(" ", new[] { PlayerFirstName, PlayerLastName }.Where(x => !string.IsNullOrWhiteSpace(x)));

    public string ToIdentityMultilineString(Func<string, string>? t = null)
    {
        t ??= static key => key switch
        {
            "save_label" => "Save: ",
            "unnamed_save" => "(unnamed / game's default label)",
            "group_agency_label" => "Group / agency: ",
            "player_label" => "Player: ",
            "mode_story" => "Story",
            "mode_freeplay" => "Free play",
            "mode_label" => "Mode: ",
            "last_saved_label" => "Last saved: ",
            "ingame_date_label" => "In-game date: ",
            "playtime_label" => "Playtime: ",
            "content_label" => "Content: ",
            "vanilla_path_label" => "Vanilla path: ",
            "not_stored" => "(not stored)",
            _ => key
        };

        var sb = new StringBuilder();
        sb.AppendLine(t("save_label") + (string.IsNullOrWhiteSpace(SaveDisplayName) ? t("unnamed_save") : SaveDisplayName));
        sb.AppendLine(t("group_agency_label") + Empty(GroupName, t));
        sb.AppendLine(t("player_label") + Empty(PlayerName, t));
        sb.AppendLine($"{t("mode_label")}{(IsStoryMode ? t("mode_story") : t("mode_freeplay"))} • {LocalizedSaveKind(SaveKind, t)}" + (string.IsNullOrWhiteSpace(ChapterName) ? "" : " • " + ChapterName));
        sb.AppendLine(t("last_saved_label") + Empty(LastSave, t) + " • " + t("ingame_date_label") + Empty(GameDateTime, t) + " • " + t("playtime_label") + FormatPlaytime(PlaytimeSeconds, t));
        string counts = string.Format(System.Globalization.CultureInfo.CurrentCulture, t("content_counts_format"), IdolCount, StaffCount, SingleCount, ShowCount);
        string game = string.IsNullOrWhiteSpace(GameVersion) ? "" : " • " + string.Format(System.Globalization.CultureInfo.CurrentCulture, t("game_version_format"), GameVersion);
        sb.AppendLine(t("content_label") + counts + game);
        sb.Append(t("vanilla_path_label") + RelativeSavePath);
        return sb.ToString();
    }

    public string ToLegacyMatchMultilineString(Func<string, string>? t = null)
    {
        t ??= static key => key switch
        {
            "slot_folder_hint" => "slot folder = ",
            "embedded_savefolder_hint" => "embedded SaveFolderName = ",
            "agency_key_hint" => "1.x agency key = ",
            "agency_fallback_key_hint" => "1.x fallback agency key = ",
            "file_key_hint" => "1.x file key = ",
            "old_root_hint" => "old 1.x root = ",
            _ => key
        };

        var hints = new List<string>();
        if (!string.IsNullOrWhiteSpace(SlotToken)) hints.Add(t("slot_folder_hint") + SlotToken);
        if (!string.IsNullOrWhiteSpace(SaveFolderName)) hints.Add(t("embedded_savefolder_hint") + SaveFolderName);
        if (!string.IsNullOrWhiteSpace(LegacyAgencySaveKeyCandidate)) hints.Add(t("agency_key_hint") + LegacyAgencySaveKeyCandidate);
        if (!string.IsNullOrWhiteSpace(LegacyAgencyFallbackKeyCandidate) && !string.Equals(LegacyAgencyFallbackKeyCandidate, LegacyAgencySaveKeyCandidate, StringComparison.OrdinalIgnoreCase))
            hints.Add(t("agency_fallback_key_hint") + LegacyAgencyFallbackKeyCandidate);
        if (!string.IsNullOrWhiteSpace(LegacyFileSaveKeyCandidate)) hints.Add(t("file_key_hint") + LegacyFileSaveKeyCandidate);
        hints.Add(t("old_root_hint") + LegacySavesRoot);
        return string.Join(Environment.NewLine, hints);
    }

    private static string Empty(string value, Func<string, string> t) => string.IsNullOrWhiteSpace(value) ? t("not_stored") : value;

    private static string LocalizedSaveKind(string saveKind, Func<string, string> t) => saveKind switch
    {
        "autosave" => t("save_kind_autosave"),
        "manual save" => t("save_kind_manual_save"),
        "manual slot" => t("save_kind_manual_slot"),
        "story autosave" => t("save_kind_story_autosave"),
        "story manual save" => t("save_kind_story_manual_save"),
        "story manual slot" => t("save_kind_story_manual_slot"),
        "story chapter snapshot" => t("save_kind_story_chapter_snapshot"),
        _ => t("save_kind_generic")
    };

    private static string FormatPlaytime(long seconds, Func<string, string> t)
    {
        if (seconds < 0) seconds = 0;
        TimeSpan span = TimeSpan.FromSeconds(seconds);
        long hours = (long)span.TotalHours;
        return string.Format(System.Globalization.CultureInfo.CurrentCulture, t("playtime_format"), hours, span.Minutes);
    }
}

internal sealed class LegacySourceCandidate
{
    public string SourcePath { get; init; } = "";
    public string FolderKey { get; init; } = "";
    public string MatchReason { get; init; } = "";
    public int Score { get; init; }

    public override string ToString() => $"{MatchReason} • {FolderKey} • {Path.GetFileName(SourcePath)}";
}

internal sealed class VanillaSaveCandidate
{
    public VanillaSaveInfo SaveInfo { get; init; } = new();
    public string MatchedSaveKey { get; init; } = "";
    public string MatchKind { get; init; } = "";
    public int Score { get; init; }

    public override string ToString() =>
        $"{MatchKind} • {SaveInfo.GroupName} • {SaveInfo.PlayerName} • {SaveInfo.SlotToken} • {SaveInfo.RelativeSavePath}";
}

internal sealed class ReverseMatchScanResult
{
    public string DataRoot { get; init; } = "";
    public int JsonFilesExamined { get; init; }
    public int ValidSavesRead { get; init; }
    public List<VanillaSaveCandidate> Candidates { get; init; } = new();
}

internal sealed class LegacyDivergencePoint
{
    public long PreviousEventId { get; init; }
    public string PreviousGameDateTime { get; init; } = "";
    public long DivergenceEventId { get; init; }
    public string DivergenceGameDateTime { get; init; } = "";
    public long RollbackSeconds { get; init; }
    public double RollbackDays => TimeSpan.FromSeconds(RollbackSeconds).TotalDays;

    public override string ToString() =>
        $"event {PreviousEventId} {PreviousGameDateTime} -> event {DivergenceEventId} {DivergenceGameDateTime} (rollback {FormatRollback(RollbackSeconds)})";

    internal static string FormatRollback(long seconds)
    {
        if (seconds < 0) seconds = 0;
        TimeSpan span = TimeSpan.FromSeconds(seconds);
        if (span.TotalDays >= 1) return $"{span.TotalDays:0.##} days";
        if (span.TotalHours >= 1) return $"{span.TotalHours:0.##} hours";
        if (span.TotalMinutes >= 1) return $"{span.TotalMinutes:0.##} minutes";
        return $"{span.TotalSeconds:0.##} seconds";
    }
}

internal sealed class LegacyContaminationEntry
{
    public string SourcePath { get; init; } = "";
    public string SourceRelativePath { get; set; } = "";
    public string BackendKind { get; init; } = "";
    public string SaveKey { get; init; } = "";
    public bool HasCheckpointSupport { get; init; }
    public int EventCount { get; init; }
    public int ParsedEventCount { get; set; }
    public int InvalidDateCount { get; set; }
    public long FirstParsedEventId { get; set; }
    public string FirstGameDateTime { get; set; } = "";
    public long LastParsedEventId { get; set; }
    public string LastGameDateTime { get; set; } = "";
    public List<LegacyDivergencePoint> Divergences { get; } = new();

    public bool IsContaminated => Divergences.Count > 0;
    public int BranchCount => EventCount == 0 ? 0 : Divergences.Count + 1;
    public LegacyDivergencePoint? PrimaryDivergence => Divergences
        .OrderByDescending(x => x.RollbackSeconds)
        .ThenBy(x => x.DivergenceEventId)
        .FirstOrDefault();
    public string Status => IsContaminated
        ? (HasCheckpointSupport ? "branch reset(s) detected; checkpoint-capable" : "contaminated / divergent timeline")
        : (InvalidDateCount > 0 ? "no reset detected; invalid date rows present" : "no branch reset detected");

    public string ToMultilineString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Status: " + Status);
        sb.AppendLine("Source: " + SourcePath);
        sb.AppendLine("Backend: " + BackendKind);
        if (!string.IsNullOrWhiteSpace(SaveKey)) sb.AppendLine("Save key: " + SaveKey);
        sb.AppendLine($"Events: {EventCount:N0}; parsed dates: {ParsedEventCount:N0}; invalid dates: {InvalidDateCount:N0}; branches: {BranchCount:N0}");
        sb.AppendLine("Checkpoint support: " + (HasCheckpointSupport ? "yes" : "no"));
        if (!string.IsNullOrWhiteSpace(FirstGameDateTime)) sb.AppendLine($"Timeline: event {FirstParsedEventId} {FirstGameDateTime} -> event {LastParsedEventId} {LastGameDateTime}");
        if (Divergences.Count > 0)
        {
            sb.AppendLine("Likely primary divergence: " + PrimaryDivergence);
            sb.AppendLine("All backwards-time branch boundaries:");
            foreach (LegacyDivergencePoint point in Divergences) sb.AppendLine("  " + point);
        }
        return sb.ToString().TrimEnd();
    }
}


internal enum LegacyRepairMode
{
    ConservativeBranchSalvage,
    CleanBaseline
}

internal sealed class LegacyRepairBranch
{
    public int Index { get; init; }
    public long StartEventId { get; init; }
    public long EndEventId { get; init; }
    public string StartGameDateTime { get; init; } = "";
    public string EndGameDateTime { get; init; } = "";
    public int EventCount { get; init; }
    public int EligibleEventCount { get; init; }
    public int FutureEventCount { get; init; }
    public int InvalidDateCount { get; init; }
    public int DirectIdentityMatches { get; init; }
    public int DirectIdentityMismatches { get; init; }
    public int UnknownIdentityEvents { get; init; }
    public int Score { get; init; }
    public string Confidence { get; init; } = "low";

    public override string ToString() =>
        $"Branch {Index}: events {StartEventId}-{EndEventId}; {StartGameDateTime} -> {EndGameDateTime}; score {Score}; matches {DirectIdentityMatches}; mismatches {DirectIdentityMismatches}";
}

internal sealed class LegacyRepairAnalysis
{
    public string SourcePath { get; init; } = "";
    public string SaveKey { get; init; } = "";
    public string SelectedSaveGameDateTime { get; init; } = "";
    public int OriginalEventCount { get; init; }
    public int OriginalCustomDataCount { get; init; }
    public bool HasCheckpointSupport { get; init; }
    public bool HasDivergence { get; init; }
    public int? RecommendedBranchIndex { get; init; }
    public string RecommendationConfidence { get; init; } = "low";
    public string RecommendationReason { get; init; } = "";
    public List<LegacyRepairBranch> Branches { get; init; } = new();
    public List<LegacyDivergencePoint> Divergences { get; init; } = new();

    public bool CanConservativeSalvage => RecommendedBranchIndex.HasValue && (RecommendationConfidence == "high" || RecommendationConfidence == "medium");

    public string ToMultilineString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Source: {SourcePath}");
        if (!string.IsNullOrWhiteSpace(SaveKey)) sb.AppendLine("Save key: " + SaveKey);
        sb.AppendLine("Selected vanilla game date: " + SelectedSaveGameDateTime);
        sb.AppendLine($"Legacy events: {OriginalEventCount:N0}; custom-data rows: {OriginalCustomDataCount:N0}");
        sb.AppendLine($"Detected branches: {Branches.Count:N0}; backwards-time boundaries: {Divergences.Count:N0}");
        if (RecommendedBranchIndex.HasValue)
            sb.AppendLine($"Recommended branch: {RecommendedBranchIndex.Value} ({RecommendationConfidence} confidence)");
        sb.AppendLine("Recommendation: " + RecommendationReason);
        return sb.ToString().TrimEnd();
    }
}

internal sealed class BulkScanProgress
{
    public BulkScanProgress(int currentFile, int totalFiles, string sourcePath)
    {
        CurrentFile = currentFile;
        TotalFiles = totalFiles;
        SourcePath = sourcePath;
    }

    public int CurrentFile { get; }
    public int TotalFiles { get; }
    public string SourcePath { get; }
}

internal sealed class BulkScanError
{
    public string SourcePath { get; init; } = "";
    public string SourceRelativePath { get; init; } = "";
    public string Message { get; init; } = "";
}

internal sealed class BulkContaminationScanResult
{
    public string RootPath { get; init; } = "";
    public int FilesDiscovered { get; init; }
    public int FilesScanned { get; set; }
    public int FilesFailed { get; set; }
    public List<LegacyContaminationEntry> Entries { get; } = new();
    public List<BulkScanError> Errors { get; } = new();

    public int ContaminatedEntries => Entries.Count(x => x.IsContaminated);
    public int CleanEntries => Entries.Count - ContaminatedEntries;
}



internal enum BulkConvertContaminatedPolicy
{
    Skip,
    AutoRepair,
    CleanBaseline
}

internal enum BulkConvertAction
{
    Skip,
    NormalMigration,
    ConservativeBranchSalvage,
    CleanBaseline
}

internal sealed class BulkConvertItem
{
    public bool Selected { get; set; } = true;
    public string SourcePath { get; init; } = "";
    public string SourceRelativePath { get; init; } = "";
    public string? SourceSaveKey { get; init; }
    public string BackendKind { get; init; } = "";
    public bool HasCheckpointSupport { get; init; }
    public int LegacyEventCount { get; init; }
    public bool IsContaminated { get; init; }
    public int BranchCount { get; init; }
    public string VanillaSavePath { get; init; } = "";
    public string VanillaRelativePath { get; init; } = "";
    public string VanillaDisplay { get; init; } = "";
    public int MatchScore { get; init; }
    public string MatchKind { get; init; } = "";
    public string OutputSidecarPath { get; init; } = "";
    public BulkConvertAction Action { get; set; }
    public int? RepairBranchIndex { get; set; }
    public string RepairConfidence { get; set; } = "";
    public string Status { get; set; } = "";
    public string Details { get; set; } = "";
    public bool DestinationExists { get; set; }
    public bool ManualMatch { get; set; }
    public bool IsReady => Action != BulkConvertAction.Skip && !string.IsNullOrWhiteSpace(VanillaSavePath) && !string.IsNullOrWhiteSpace(OutputSidecarPath);

    public string ActionDisplay => Action switch
    {
        BulkConvertAction.NormalMigration => "Normal migration",
        BulkConvertAction.ConservativeBranchSalvage => "Branch salvage",
        BulkConvertAction.CleanBaseline => "Clean baseline",
        _ => "Skip"
    };
}

internal sealed class BulkConvertPlan
{
    public string LegacyRoot { get; init; } = "";
    public string DataRoot { get; init; } = "";
    public string OutputRoot { get; init; } = "";
    public int LegacyFilesDiscovered { get; init; }
    public int VanillaJsonFilesExamined { get; init; }
    public int VanillaSavesRecognized { get; init; }
    public List<BulkConvertItem> Items { get; } = new();
    public List<string> Errors { get; } = new();

    public int ReadyCount => Items.Count(x => x.IsReady);
    public int SelectedReadyCount => Items.Count(x => x.Selected && x.IsReady);
    public int SkippedCount => Items.Count(x => !x.IsReady);
    public int ContaminatedCount => Items.Count(x => x.IsContaminated);
}

internal sealed class BulkConvertRunItem
{
    public string SourcePath { get; init; } = "";
    public string? SourceSaveKey { get; init; }
    public string VanillaSavePath { get; init; } = "";
    public string OutputSidecarPath { get; init; } = "";
    public BulkConvertAction Action { get; init; }
    public bool Success { get; init; }
    public bool Skipped { get; init; }
    public string Summary { get; set; } = "";
    public bool LegacyCleanupAttempted { get; set; }
    public bool LegacyCleanupSucceeded { get; set; }
    public string LegacyCleanupSummary { get; set; } = "";
}

internal sealed class BulkConvertRunResult
{
    public bool DryRun { get; init; }
    public List<BulkConvertRunItem> Items { get; } = new();
    public int SuccessCount => Items.Count(x => x.Success && !x.Skipped);
    public int SkippedCount => Items.Count(x => x.Skipped);
    public int FailedCount => Items.Count(x => !x.Success && !x.Skipped);
    public int CleanupSuccessCount => Items.Count(x => x.LegacyCleanupAttempted && x.LegacyCleanupSucceeded);
    public int CleanupFailedCount => Items.Count(x => x.LegacyCleanupAttempted && !x.LegacyCleanupSucceeded);
}

internal sealed class BulkConvertProgress
{
    public BulkConvertProgress(int current, int total, string message)
    {
        Current = current;
        Total = total;
        Message = message;
    }

    public int Current { get; }
    public int Total { get; }
    public string Message { get; }
}

internal sealed class MigrationPlan
{
    public string SourcePath { get; init; } = "";
    public string? SourceSaveKey { get; init; }
    public string LegacyVersionOverride { get; init; } = "";
    public bool AllowUnverifiedCheckpoint { get; init; }
    public VanillaSaveInfo Vanilla { get; init; } = new();
    public string OutputRoot { get; init; } = "";
    public string OutputSidecarPath { get; init; } = "";
}

internal sealed class MigrationReport
{
    public bool Success { get; set; }
    public string Summary { get; set; } = "";
    public string OutputPath { get; set; } = "";
    public int EventCount { get; set; }
    public int CustomDataCount { get; set; }
    public List<string> Messages { get; } = new();

    public string ToMultilineString()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Success ? "SUCCESS" : "FAILED");
        if (!string.IsNullOrWhiteSpace(Summary)) sb.AppendLine(Summary);
        if (!string.IsNullOrWhiteSpace(OutputPath)) sb.AppendLine("Output: " + OutputPath);
        if (Success) sb.AppendLine($"Migrated events: {EventCount}; custom data keys: {CustomDataCount}");
        foreach (string message in Messages) sb.AppendLine(message);
        return sb.ToString().TrimEnd();
    }
}

internal sealed class V6Document
{
    public string FormatName { get; set; } = "IMDataCore.LightweightSidecar";
    public int FormatVersion { get; set; } = 6;
    public string RelativeSavePath { get; set; } = "";
    public long LastIssuedSequence { get; set; }
    public int ForwardCompatibilitySchemaVersion { get; set; } = 1;
    public List<V6ForwardExtension> ForwardExtensions { get; set; } = new();
    public V6MigrationProvenance MigrationProvenance { get; set; } = new();
    public List<V6Checkpoint> Checkpoints { get; set; } = new();
    public List<V6Event> Events { get; set; } = new();
    public List<V6CustomMutation> CustomMutations { get; set; } = new();
    public List<V6NamespaceBinding> NamespaceOwnerBindings { get; set; } = new();
    public int CoverageModelVersion { get; set; } = 1;
    public List<JsonNode> CoverageCapabilitySets { get; set; } = new();
    public List<JsonNode> CoverageTransitions { get; set; } = new();
    public List<JsonNode> HistoricalBaselineAssertions { get; set; } = new();
}

internal sealed class V6ForwardExtension
{
    public string ExtensionId { get; set; } = "";
    public int ExtensionSchemaVersion { get; set; } = 1;
    public bool RequiredForRead { get; set; }
    public string PayloadJson { get; set; } = "";
}

internal sealed class V6MigrationProvenance
{
    public int ProvenanceSchemaVersion { get; set; } = 1;
    public string Origin { get; set; } = "legacy_migration";
    public string SourceFormatName { get; set; } = "IMDataCore.LightweightSidecar";
    public int SourceFormatVersion { get; set; } = 1;
    public string SourceDocumentHash { get; set; } = "";
    public long SourceLastIssuedSequence { get; set; }
    public string TargetFormatName { get; set; } = "IMDataCore.LightweightSidecar";
    public int TargetFormatVersion { get; set; } = 6;
    public int TargetJournalFormatVersion { get; set; } = 3;
    public string ConversionId { get; set; } = "";
}

internal sealed class V6Checkpoint
{
    public string RelativeSavePath { get; set; } = "";
    public string LastSave { get; set; } = "";
    public long PlaytimeSeconds { get; set; }
    public string GameDateTime { get; set; } = "";
    public string ContentFingerprint { get; set; } = "";
    public long Sequence { get; set; }
    public List<JsonNode> EnabledMods { get; set; } = new();
    public List<JsonNode> AgencyRoomIdentities { get; set; } = new();
    public int IdentityBindingsVersion { get; set; } = 1;
    public bool IdentityBindingsComplete { get; set; }
    public List<JsonNode> IdentityBindings { get; set; } = new();
    public List<JsonNode> IdentityCandidates { get; set; } = new();
}

internal sealed class V6Event
{
    public long Sequence { get; set; }
    public string GameDateTime { get; set; } = "";
    public int IdolId { get; set; } = -1;
    public string EntityKind { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string EventType { get; set; } = "";
    public string SourcePatch { get; set; } = "";
    public string NamespaceIdentifier { get; set; } = "";
    public JsonNode? Payload { get; set; }
    public int ParticipantSchemaVersion { get; set; }
}

internal sealed class V6CustomMutation
{
    public long Sequence { get; set; }
    public string GameDateTime { get; set; } = "";
    public string NamespaceIdentifier { get; set; } = "";
    public string DataKey { get; set; } = "";
    public string Operation { get; set; } = "set";
    public JsonNode? Value { get; set; }
}

internal sealed class V6NamespaceBinding
{
    public string NamespaceIdentifier { get; set; } = "";
    public int BindingSchemaVersion { get; set; } = 1;
    public int BindingRevision { get; set; } = 1;
    public int OwnerSchemaVersion { get; set; }
    public bool OwnershipKnown { get; set; }
    public string StableOwnerId { get; set; } = "";
    public string Origin { get; set; } = "legacy_unbound";
    public string CurrentAssemblyWitness { get; set; } = "";
    public List<string> PreviousAssemblyWitnesses { get; set; } = new();
}
