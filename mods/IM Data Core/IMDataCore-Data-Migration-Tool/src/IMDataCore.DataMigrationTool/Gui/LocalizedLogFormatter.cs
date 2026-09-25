using System.Text;
using System.Text.RegularExpressions;
using IMDataCore.DataMigrationTool.Migration;

namespace IMDataCore.DataMigrationTool.Gui;

/// <summary>
/// Converts user-facing diagnostic/migration text into the selected GUI language.
/// The migration layer deliberately stays language-neutral for CLI/export use; the
/// WinForms UI formats its structured results here instead of dumping English model text.
/// </summary>
internal static class LocalizedLogFormatter
{
    public static string FormatInspection(LegacyInspection inspection)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Localization.Format("log_source", inspection.SourcePath));
        sb.AppendLine(Localization.Format("log_backend", BackendName(inspection.BackendKind)));
        sb.AppendLine(Localization.Format("log_detected_version", VersionName(inspection.DetectedVersion)));
        if (!string.IsNullOrWhiteSpace(inspection.DetectionNote))
            sb.AppendLine(Localization.Format("log_detection", DetectionNote(inspection.DetectionNote)));
        sb.AppendLine(Localization.Format("log_checkpoint_support", YesNo(inspection.HasCheckpointSupport)));
        sb.AppendLine(Localization.Format("log_source_hash", inspection.SourceSha256));
        if (inspection.SaveKeys.Count > 0)
            sb.AppendLine(Localization.Format("log_save_keys", string.Join(", ", inspection.SaveKeys)));
        foreach (string warning in inspection.Warnings)
            sb.AppendLine(Localization.Format("log_warning", TranslateRuntimeMessage(warning)));
        return sb.ToString().TrimEnd();
    }

    public static string FormatSourceInfo(LegacyInspection inspection)
    {
        string checkpoint = Localization.T(inspection.HasCheckpointSupport
            ? "log_source_info_exact"
            : "log_source_info_precheckpoint");
        return string.Format(System.Globalization.CultureInfo.CurrentCulture,
            checkpoint,
            VersionName(inspection.DetectedVersion),
            BackendName(inspection.BackendKind));
    }

    public static string FormatReport(MigrationReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Localization.T(report.Success ? "log_success" : "log_failed"));
        if (!string.IsNullOrWhiteSpace(report.Summary))
            sb.AppendLine(TranslateRuntimeMessage(report.Summary));
        if (!string.IsNullOrWhiteSpace(report.OutputPath))
            sb.AppendLine(Localization.Format("log_output", report.OutputPath));
        if (report.Success)
            sb.AppendLine(Localization.Format("log_migrated_counts", report.EventCount, report.CustomDataCount));
        foreach (string message in report.Messages)
            sb.AppendLine(TranslateReportLine(message));
        return sb.ToString().TrimEnd();
    }

    public static string FormatRepairAnalysis(LegacyRepairAnalysis analysis)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Localization.Format("repair_log_source", analysis.SourcePath));
        if (!string.IsNullOrWhiteSpace(analysis.SaveKey))
            sb.AppendLine(Localization.Format("repair_log_save_key", analysis.SaveKey));
        sb.AppendLine(Localization.Format("repair_log_selected_date", analysis.SelectedSaveGameDateTime));
        sb.AppendLine(Localization.Format("repair_log_counts", analysis.OriginalEventCount, analysis.OriginalCustomDataCount));
        sb.AppendLine(Localization.Format("repair_log_branch_counts", analysis.Branches.Count, analysis.Divergences.Count));
        if (analysis.RecommendedBranchIndex.HasValue)
            sb.AppendLine(Localization.Format("repair_log_recommended", analysis.RecommendedBranchIndex.Value, ConfidenceName(analysis.RecommendationConfidence)));
        sb.AppendLine(Localization.Format("repair_log_recommendation", TranslateRuntimeMessage(analysis.RecommendationReason)));
        return sb.ToString().TrimEnd();
    }


    public static string FormatLegacySourceCandidate(LegacySourceCandidate candidate)
    {
        string reason = candidate.MatchReason switch
        {
            "exact 1.x file-save key" => Localization.T("reverse_reason_exact_file"),
            "exact 1.x agency key" => Localization.T("reverse_reason_exact_agency"),
            "exact 1.x fallback agency key" => Localization.T("reverse_reason_exact_agency_fallback"),
            "embedded SaveFolderName" => Localization.T("legacy_reason_embedded_folder"),
            "vanilla slot folder" => Localization.T("legacy_reason_slot_folder"),
            "folder contains vanilla slot token" => Localization.T("legacy_reason_contains_slot"),
            _ => candidate.MatchReason
        };
        return $"{reason} • {candidate.FolderKey} • {Path.GetFileName(candidate.SourcePath)}";
    }

    public static string BackendName(string backendKind) => backendKind switch
    {
        "sqlite" => Localization.T("log_backend_database"),
        "flat-json" => Localization.T("log_backend_structured_file"),
        _ => Localization.T("log_backend_unknown")
    };

    public static string VersionName(string detectedVersion)
    {
        if (detectedVersion.EndsWith(" (unversioned)", StringComparison.Ordinal))
            return detectedVersion.Substring(0, detectedVersion.Length - " (unversioned)".Length) + " " + Localization.T("log_version_unversioned");
        if (detectedVersion.EndsWith(" (late)", StringComparison.Ordinal))
            return detectedVersion.Substring(0, detectedVersion.Length - " (late)".Length) + " " + Localization.T("log_version_late");
        return detectedVersion;
    }

    public static string CheckpointStatusName(string status)
    {
        if (string.Equals(status, "not_available", StringComparison.Ordinal)) return Localization.T("checkpoint_not_available");
        if (string.Equals(status, "exact_match", StringComparison.Ordinal)) return Localization.T("checkpoint_exact_match");
        if (status.StartsWith("override_", StringComparison.Ordinal)) return Localization.T("checkpoint_unverified_override");
        if (status.StartsWith("data_migration_tool_repair_", StringComparison.Ordinal)) return Localization.T("checkpoint_repair_generated");
        return status;
    }

    public static string ConfidenceName(string confidence) => confidence switch
    {
        "high" => Localization.T("confidence_high"),
        "medium" => Localization.T("confidence_medium"),
        "manual" => Localization.T("confidence_manual"),
        _ => Localization.T("confidence_low")
    };

    public static string TranslateMultiline(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        string normalized = text.Replace("\r\n", "\n");
        return string.Join(Environment.NewLine, normalized.Split('\n').Select(TranslateReportLine));
    }

    public static string TranslateReportLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return line;

        if (line.StartsWith("WARNING: ", StringComparison.Ordinal))
            return Localization.Format("log_warning", TranslateRuntimeMessage(line[9..]));
        if (line.StartsWith("ERROR: ", StringComparison.Ordinal))
            return Localization.Format("log_error", TranslateRuntimeMessage(line[7..]));
        if (line.StartsWith("Destination root: ", StringComparison.Ordinal))
            return Localization.Format("report_destination_root", line[18..]);
        if (line.StartsWith("Relative save path: ", StringComparison.Ordinal))
            return Localization.Format("report_relative_save_path", line[20..]);
        if (line.StartsWith("Legacy checkpoint status: ", StringComparison.Ordinal))
            return Localization.Format("report_checkpoint_status", CheckpointStatusName(line[26..]));
        if (line.StartsWith("Written atomically: ", StringComparison.Ordinal))
            return Localization.Format("report_written_atomically", line[20..]);
        if (line.StartsWith("Archived existing artifact: ", StringComparison.Ordinal))
            return Localization.Format("report_archived_existing", line[28..]);
        if (line.StartsWith("Legacy source cleanup: ", StringComparison.Ordinal))
            return Localization.Format("report_cleanup", TranslateRuntimeMessage(line[23..]));
        if (line.StartsWith("Repair mode: ", StringComparison.Ordinal))
            return Localization.Format("report_repair_mode", RepairModeName(line[13..]));

        Match dropped = Regex.Match(line, @"^Dropped legacy events: ([\d,]+); dropped custom-data rows: ([\d,]+)$", RegexOptions.CultureInvariant);
        if (dropped.Success)
            return Localization.Format("report_dropped_counts", dropped.Groups[1].Value, dropped.Groups[2].Value);

        return TranslateRuntimeMessage(line);
    }

    public static string TranslateRuntimeMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return message;

        string? key = message switch
        {
            "The 1.0/1.1/1.2/early-1.3 fallback envelope has no release discriminator." => "detect_flat_unversioned",
            "Money Ledger events first appear in 1.2.0; envelope remained unversioned until late 1.3.0." => "detect_flat_money",
            "Room-work events first appear in 1.1.0; storage envelope is otherwise unchanged." => "detect_flat_room",
            "Integrity/checkpoint envelope introduced during the 1.3.0 commit range." => "detect_flat_checkpoint",
            "Save-generation checkpoint tables were added during the 1.3.0 commit range." => "detect_database_checkpoint",
            "Money Ledger events first appear in 1.2.0." => "detect_database_money",
            "Room-work events first appear in 1.1.0." => "detect_database_room",
            "SQLite schema_version remained 2 across the 1.x releases, so the database alone may not identify the exact mod release." => "detect_database_schema",

            "Unversioned fallback files from 1.0-early 1.3 do not contain exact vanilla-save checkpoint identity." => "warning_unversioned_fallback",
            "SQLite source predates exact save-generation checkpoint tables; source/save association is user-supplied." => "warning_database_precheckpoint",
            "Source generation predates exact save checkpoint binding; association to the selected vanilla save is user-supplied." => "warning_file_precheckpoint",
            "This database contains multiple save_key values; choose the one corresponding to the selected vanilla save." => "warning_multiple_save_keys",
            "Advanced override used: checkpoint-capable 1.3 SQLite state was not proven to match the selected vanilla save." => "warning_override_database",
            "Advanced override used: late-1.3 source state was not proven to match the selected vanilla save." => "warning_override_file",

            "Plan is statically valid and serializes as a strict v6 JSON document." => "report_plan_valid",
            "Validation scope: v6 schema/serialization and migration invariants passed; Validate only did not write the output file." => "report_validation_scope",
            "Association confidence: this legacy generation has no exact save checkpoint, so validation does not prove that the legacy state came from this exact vanilla save generation." => "report_association_no_checkpoint",
            "Association confidence: exact legacy save checkpoint matched the selected vanilla save fingerprint." => "report_association_exact",
            "Association confidence: advanced override is active; exact legacy checkpoint matching was not established." => "report_association_override",
            "Post-write sidecar revalidation passed." => "report_post_write_valid",
            "Migration completed. The new sidecar is v6, exact-save bound, and ready for current IMDataCore to populate newer fields on subsequent saves." => "report_migration_completed",
            "Idol Manager appears to be running. Close the game before writing a live IMDataCore sidecar." => "report_game_running",
            "Idol Manager appears to be running. Close the game before writing a repaired live IMDataCore sidecar." => "report_game_running_repair",
            "Clean-baseline repair is statically valid and ready to write." => "report_repair_valid_clean",
            "Branch-salvage repair is statically valid and ready to write." => "report_repair_valid_salvage",
            "Clean-baseline repair completed. A fresh exact v6 checkpoint was created without contaminated legacy state." => "report_repair_complete_clean",
            "Branch repair completed. Only the selected branch through the vanilla save date was retained; mutable legacy custom-data state was discarded." => "report_repair_complete_salvage",

            "Serialized output lost v6 discriminator." => "error_serialized_v6",
            "Serialized repaired output lost v6 discriminator." => "error_serialized_repair_v6",
            "One or more live sidecar/journal/backup artifacts already exist at the destination. Leave them untouched or explicitly enable overwrite/backup." => "error_destination_artifacts",
            "A stale live journal remained after archival; refusing to finish migration." => "error_stale_history",
            "A stale live journal remained after archival; refusing to finish repaired migration." => "error_stale_history_repair",
            "The migrated sidecar was not found during post-write verification." => "error_written_missing",
            "The written sidecar could not be deserialized for post-write verification." => "error_written_unreadable",
            "Post-write verification failed: RelativeSavePath does not match the selected vanilla save." => "error_written_path_mismatch",
            "Post-write verification failed: checkpoint fingerprint does not match the selected vanilla save." => "error_written_checkpoint_mismatch",

            "No backwards-time branch boundary was detected. Ordinary migration is preferred; clean-baseline repair remains available if you intentionally want to discard legacy state." => "repair_reason_no_divergence",
            "Branch boundaries were detected, but no branch has valid events at or before the selected vanilla save date. Use clean-baseline repair." => "repair_reason_no_eligible",
            "Branch boundaries were detected, but the selected vanilla save does not provide enough direct identity evidence to choose a branch confidently. Clean-baseline repair is the safe default; manual branch salvage is available as an advanced choice." => "repair_reason_low_evidence",
            "Discarded all legacy event history and mutable custom-data state; emitted only a fresh exact v6 checkpoint bound to the selected vanilla save." => "repair_summary_clean",
            "IMDataCore Data Migration Tool branch repair was applied. The repair is conservative and does not invent missing historical events." => "repair_warning_applied",

            "Legacy source files were already absent; moved the now-empty legacy save folder to the Windows Recycle Bin." => "cleanup_already_absent_folder",
            "Legacy source was already absent; nothing was recycled." => "cleanup_already_absent",
            _ => null
        };
        if (key is not null) return Localization.T(key);

        Match integrity = Regex.Match(message, @"^Integrity validation failed: (.+)$", RegexOptions.CultureInvariant | RegexOptions.Singleline);
        if (integrity.Success)
            return Localization.Format("warning_integrity_failed", integrity.Groups[1].Value);

        Match cleanupMovedFolder = Regex.Match(message, @"^Moved ([\d,]+) legacy source file\(s\) and the now-empty per-save folder to the Windows Recycle Bin\.$", RegexOptions.CultureInvariant);
        if (cleanupMovedFolder.Success)
            return Localization.Format("cleanup_moved_files_folder", cleanupMovedFolder.Groups[1].Value);

        Match cleanupMoved = Regex.Match(message, @"^Moved ([\d,]+) legacy source file\(s\) to the Windows Recycle Bin\. The containing folder was kept because it still contains other files or folders\.$", RegexOptions.CultureInvariant);
        if (cleanupMoved.Success)
            return Localization.Format("cleanup_moved_files_kept_folder", cleanupMoved.Groups[1].Value);

        Match cleanupFailed = Regex.Match(message, @"^Moved ([\d,]+) file\(s\) before cleanup failed: (.+)$", RegexOptions.CultureInvariant | RegexOptions.Singleline);
        if (cleanupFailed.Success)
            return Localization.Format("cleanup_partial_failed", cleanupFailed.Groups[1].Value, cleanupFailed.Groups[2].Value);

        Match droppedCustom = Regex.Match(message, @"^Dropped ([\d,]+) legacy custom-data row\(s\) because 1\.x did not retain branch-versioned historical values\.$", RegexOptions.CultureInvariant);
        if (droppedCustom.Success)
            return Localization.Format("repair_warning_dropped_custom", droppedCustom.Groups[1].Value);

        Match strongest = Regex.Match(message, @"^Branch (\d+) is the strongest match to identities present in the selected vanilla save \((\d+) direct match\(es\), (\d+) mismatch\(es\)\)\. Conservative salvage can retain only this branch through the vanilla save date\.$", RegexOptions.CultureInvariant);
        if (strongest.Success)
            return Localization.Format("repair_reason_strongest", strongest.Groups[1].Value, strongest.Groups[2].Value, strongest.Groups[3].Value);

        Match salvaged = Regex.Match(message, @"^Salvaged branch (\d+) \((high|medium|low|manual) selection\): retained ([\d,]+) event\(s\) from event ([\d,]+) through ([\d,]+), clipped at vanilla game date (.+)\. All legacy custom-data rows were dropped because pre-v6 1\.x custom_data stored only mutable current values and cannot be safely assigned to a historical branch\.$", RegexOptions.CultureInvariant | RegexOptions.Singleline);
        if (salvaged.Success)
            return Localization.Format("repair_summary_salvaged", salvaged.Groups[1].Value, ConfidenceName(salvaged.Groups[2].Value), salvaged.Groups[3].Value, salvaged.Groups[4].Value, salvaged.Groups[5].Value, salvaged.Groups[6].Value);

        Match futureDatabase = Regex.Match(message,
            @"^This pre-checkpoint IMDataCore database contains ([\d,]+) event\(s\) later than the selected vanilla save game date \(([^)]+)\); the latest legacy event is ([^.]+)\. The 1\.x database therefore appears to contain history from a later branch/load that cannot be proven to belong to this vanilla checkpoint\. Select a matching later vanilla save, or enable the advanced unverified-association option to migrate the data knowingly\.$",
            RegexOptions.CultureInvariant | RegexOptions.Singleline);
        if (futureDatabase.Success)
            return Localization.Format("warning_future_database", futureDatabase.Groups[1].Value, futureDatabase.Groups[2].Value, futureDatabase.Groups[3].Value);

        Match futureFile = Regex.Match(message,
            @"^This pre-checkpoint IMDataCore fallback contains ([\d,]+) event\(s\) later than the selected vanilla save game date \(([^)]+)\); the latest legacy event is ([^.]+)\. The legacy state may contain history from a later branch/load\.(?: Use branch repair, select a matching later vanilla save, or explicitly enable the advanced unverified-association option\.)?$",
            RegexOptions.CultureInvariant | RegexOptions.Singleline);
        if (futureFile.Success)
            return Localization.Format("warning_future_file", futureFile.Groups[1].Value, futureFile.Groups[2].Value, futureFile.Groups[3].Value);

        if (message.StartsWith("Advanced override used: ", StringComparison.Ordinal))
            return Localization.Format("warning_advanced_prefix", TranslateRuntimeMessage(message[24..]));

        return message;
    }

    private static string DetectionNote(string note) => TranslateRuntimeMessage(note);
    private static string YesNo(bool value) => Localization.T(value ? "log_yes" : "log_no");

    private static string RepairModeName(string mode) => mode switch
    {
        "clean_baseline" => Localization.T("repair_mode_clean_short"),
        "conservative_branch_salvage" => Localization.T("repair_mode_salvage_short"),
        _ => mode
    };
}
