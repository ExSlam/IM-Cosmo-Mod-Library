using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace IMDataCore.DataMigrationTool.Migration;

internal static class LegacyFlatJsonReader
{
    private static readonly string[] ProjectionArrays =
    {
        "SingleParticipation", "StatusWindows", "ShowCastWindows", "ContractWindows",
        "RelationshipWindows", "TourParticipation", "AwardResults", "ElectionResults", "PushWindows"
    };

    public static LegacyInspection Inspect(string path)
    {
        string raw = File.ReadAllText(path, Encoding.UTF8);
        JsonObject root = RequireObject(JsonUtil.ParseNode(raw, "Legacy fallback JSON"));
        int format = GetInt(root, "FormatVersion", 0);
        bool checkpoints = format > 0 || root.ContainsKey("CheckpointFingerprint") || root.ContainsKey("Checkpoints");
        List<LegacyEvent> events = ReadEvents(root);
        (string version, string note) = DetectVersion(format, events, checkpoints);
        var warnings = new List<string>();
        if (format == 1 || format == 2)
        {
            try { ValidateIntegrity(raw, root, format); }
            catch (Exception ex) { warnings.Add("Integrity validation failed: " + ex.Message); }
        }
        if (format == 0) warnings.Add("Unversioned fallback files from 1.0-early 1.3 do not contain exact vanilla-save checkpoint identity.");
        return new LegacyInspection
        {
            SourcePath = Path.GetFullPath(path), BackendKind = "flat-json", DetectedVersion = version,
            DetectionNote = note, HasCheckpointSupport = checkpoints,
            SourceSha256 = HashUtil.Sha256Hex(File.ReadAllBytes(path)), Warnings = warnings
        };
    }

    public static List<LegacyContaminationEntry> AnalyzeContamination(string path)
    {
        string raw = File.ReadAllText(path, Encoding.UTF8);
        JsonObject root = RequireObject(JsonUtil.ParseNode(raw, "Legacy fallback JSON"));
        int format = GetInt(root, "FormatVersion", 0);
        bool checkpoints = format > 0 || root.ContainsKey("CheckpointFingerprint") || root.ContainsKey("Checkpoints");
        List<LegacyEvent> events = ReadEvents(root);
        return new List<LegacyContaminationEntry>
        {
            LegacyContaminationScanner.AnalyzeTimeline(path, "flat-json", "(flat-json)", checkpoints, events)
        };
    }

    public static LegacySnapshot Read(string path, VanillaSaveInfo vanilla, bool allowUnverified)
    {
        string raw = File.ReadAllText(path, Encoding.UTF8);
        JsonObject root = RequireObject(JsonUtil.ParseNode(raw, "Legacy fallback JSON"));
        int format = GetInt(root, "FormatVersion", 0);
        if (format is 1 or 2) ValidateIntegrity(raw, root, format);
        if (format > 2) throw new InvalidDataException("Unsupported legacy fallback FormatVersion " + format + ".");

        JsonObject selected = root;
        string checkpointStatus = "not_available";
        if (format > 0 || root.ContainsKey("CheckpointFingerprint") || root.ContainsKey("Checkpoints"))
        {
            string? snapshotJson = FindMatchingCheckpointSnapshot(root, vanilla.LegacyFileFingerprint);
            if (!string.IsNullOrEmpty(snapshotJson))
            {
                selected = RequireObject(JsonUtil.ParseNode(snapshotJson, "Selected legacy checkpoint snapshot"));
                int selectedFormat = GetInt(selected, "FormatVersion", 0);
                if (selectedFormat is 1 or 2) ValidateIntegrity(snapshotJson, selected, selectedFormat);
                checkpointStatus = "exact_match";
            }
            else if (!allowUnverified)
            {
                throw new InvalidDataException("This late-1.3 fallback supports exact checkpoints, but none matches the selected vanilla save. Refusing safe migration. Use the advanced override only if you intentionally want the file's current state.");
            }
            else checkpointStatus = "override_current_state_no_exact_match";
        }

        List<LegacyEvent> events = ReadEvents(selected);
        List<LegacyCustomData> custom = ReadCustomData(selected);
        (string version, string note) = DetectVersion(format, events, format > 0);
        var counts = ProjectionArrays.ToDictionary(x => x, x => CountArray(selected, x), StringComparer.Ordinal);
        var warnings = new List<string>();
        if (checkpointStatus == "not_available")
        {
            warnings.Add("Source generation predates exact save checkpoint binding; association to the selected vanilla save is user-supplied.");
            ValidatePreCheckpointTimeline(events, vanilla, allowUnverified, warnings);
        }
        if (checkpointStatus.StartsWith("override_", StringComparison.Ordinal)) warnings.Add("Advanced override used: late-1.3 source state was not proven to match the selected vanilla save.");
        return new LegacySnapshot
        {
            BackendKind = "flat-json", DetectedVersion = version, DetectionNote = note,
            CheckpointStatus = checkpointStatus, NextEventId = GetLong(selected, "NextEventId", 1),
            Events = events, CustomData = custom, ProjectionCounts = counts, Warnings = warnings
        };
    }

    private static void ValidatePreCheckpointTimeline(List<LegacyEvent> events, VanillaSaveInfo vanilla, bool allowUnverified, List<string> warnings)
    {
        if (!GameDateUtilities.TryParseVanillaOrRoundTrip(vanilla.GameDateTime, out DateTime selectedSaveDate)) return;
        int laterCount = 0;
        DateTime latest = DateTime.MinValue;
        foreach (LegacyEvent legacyEvent in events)
        {
            if (!GameDateUtilities.TryParseVanillaOrRoundTrip(legacyEvent.GameDateTime, out DateTime eventDate)) continue;
            if (eventDate <= selectedSaveDate) continue;
            laterCount++;
            if (eventDate > latest) latest = eventDate;
        }
        if (laterCount == 0) return;
        string latestText = latest == DateTime.MinValue ? "unknown" : latest.ToString(GameDateUtilities.VanillaDataFormat, System.Globalization.CultureInfo.InvariantCulture);
        string selectedText = selectedSaveDate.ToString(GameDateUtilities.VanillaDataFormat, System.Globalization.CultureInfo.InvariantCulture);
        string message = $"This pre-checkpoint IMDataCore fallback contains {laterCount:N0} event(s) later than the selected vanilla save game date ({selectedText}); the latest legacy event is {latestText}. The legacy state may contain history from a later branch/load.";
        if (!allowUnverified) throw new InvalidDataException(message + " Use branch repair, select a matching later vanilla save, or explicitly enable the advanced unverified-association option.");
        warnings.Add("Advanced override used: " + message);
    }

    private static string? FindMatchingCheckpointSnapshot(JsonObject root, string fingerprint)
    {
        if (root["Checkpoints"] is JsonArray history)
        {
            for (int i = history.Count - 1; i >= 0; i--)
            {
                if (history[i] is not JsonObject cp) continue;
                if (string.Equals(GetString(cp, "Fingerprint"), fingerprint, StringComparison.Ordinal))
                    return GetString(cp, "SnapshotJson");
            }
        }
        if (string.Equals(GetString(root, "CheckpointFingerprint"), fingerprint, StringComparison.Ordinal))
            return GetString(root, "CheckpointSnapshotJson");
        return null;
    }

    private static void ValidateIntegrity(string raw, JsonObject root, int format)
    {
        string stored = GetString(root, "IntegritySha256");
        if (stored.Length != 64) throw new InvalidDataException("IntegritySha256 is missing or malformed.");
        string canonical = Regex.Replace(raw, "(\\\"IntegritySha256\\\"\\s*:\\s*)\\\"(?:\\\\.|[^\\\"])*\\\"", "$1\"\"", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        string computed = HashUtil.Sha256Hex(canonical);
        if (!string.Equals(stored, computed, StringComparison.Ordinal))
            throw new InvalidDataException($"Format-{format} integrity SHA-256 does not match its contents.");
    }

    private static List<LegacyEvent> ReadEvents(JsonObject root)
    {
        var result = new List<LegacyEvent>();
        if (root["Events"] is not JsonArray array) return result;
        foreach (JsonNode? node in array)
        {
            if (node is not JsonObject e) continue;
            result.Add(new LegacyEvent
            {
                EventId = GetLong(e, "EventId", 0), GameDateKey = GetInt(e, "GameDateKey", 0),
                GameDateTime = GetString(e, "GameDateTime"), IdolId = GetInt(e, "IdolId", -1),
                EntityKind = GetString(e, "EntityKind"), EntityId = GetString(e, "EntityId"),
                EventType = GetString(e, "EventType"), SourcePatch = GetString(e, "SourcePatch"),
                NamespaceIdentifier = GetString(e, "NamespaceIdentifier"),
                PayloadJson = string.IsNullOrWhiteSpace(GetString(e, "PayloadJson")) ? "{}" : GetString(e, "PayloadJson")
            });
        }
        return result;
    }

    private static List<LegacyCustomData> ReadCustomData(JsonObject root)
    {
        var result = new List<LegacyCustomData>();
        if (root["CustomData"] is not JsonArray array) return result;
        foreach (JsonNode? node in array)
        {
            if (node is not JsonObject c) continue;
            result.Add(new LegacyCustomData
            {
                NamespaceIdentifier = GetString(c, "NamespaceIdentifier"), DataKey = GetString(c, "DataKey"),
                ValueJson = GetString(c, "ValueJson"), UpdatedUtc = GetString(c, "UpdatedUtc")
            });
        }
        return result;
    }

    private static (string, string) DetectVersion(int format, List<LegacyEvent> events, bool checkpoints)
    {
        if (format > 0 || checkpoints) return ("1.3.0 (late)", "Integrity/checkpoint envelope introduced during the 1.3.0 commit range.");
        if (events.Any(e => e.EventType is "money_transaction" or "money_ledger_coverage_started"))
            return ("1.2.0-1.3.0", "Money Ledger events first appear in 1.2.0; envelope remained unversioned until late 1.3.0.");
        if (events.Any(e => e.EventType == "room_work_completed"))
            return ("1.1.0-1.3.0", "Room-work events first appear in 1.1.0; storage envelope is otherwise unchanged.");
        return ("1.0.0-1.3.0 (unversioned)", "The 1.0/1.1/1.2/early-1.3 fallback envelope has no release discriminator.");
    }

    private static JsonObject RequireObject(JsonNode node) => node as JsonObject ?? throw new InvalidDataException("JSON root must be an object.");
    private static int CountArray(JsonObject obj, string name) => obj[name] is JsonArray a ? a.Count : 0;
    private static string GetString(JsonObject o, string n) => o[n]?.GetValue<string?>() ?? "";
    private static int GetInt(JsonObject o, string n, int f) => o[n] is JsonValue v && v.TryGetValue<int>(out int x) ? x : f;
    private static long GetLong(JsonObject o, string n, long f) => o[n] is JsonValue v && v.TryGetValue<long>(out long x) ? x : f;
}
