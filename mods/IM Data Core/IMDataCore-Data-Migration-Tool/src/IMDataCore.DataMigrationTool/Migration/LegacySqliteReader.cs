using IMDataCore.DataMigrationTool.Interop;

namespace IMDataCore.DataMigrationTool.Migration;

internal static class LegacySqliteReader
{
    public static LegacyInspection Inspect(string path)
    {
        using TempSqliteCopy copy = TempSqliteCopy.Create(path);
        using var db = new WinSqliteDatabase(copy.DatabasePath);
        EnsureLegacyCoreTables(db);
        List<string> keys = ReadSaveKeys(db);
        bool checkpoints = db.TableExists("storage_save_generation") || db.TableExists("storage_save_checkpoint");
        List<LegacyEvent> sampleEvents = ReadEvents(db, "event_stream", keys.FirstOrDefault(), 10000);
        (string version, string note) = DetectVersion(checkpoints, sampleEvents);
        return new LegacyInspection
        {
            SourcePath = Path.GetFullPath(path), BackendKind = "sqlite", DetectedVersion = version,
            DetectionNote = note, SaveKeys = keys, HasCheckpointSupport = checkpoints,
            SourceSha256 = HashUtil.Sha256Hex(File.ReadAllBytes(path)),
            Warnings = keys.Count > 1 ? new List<string> { "This database contains multiple save_key values; choose the one corresponding to the selected vanilla save." } : new List<string>()
        };
    }

    public static List<LegacyContaminationEntry> AnalyzeContamination(string path)
    {
        using TempSqliteCopy copy = TempSqliteCopy.Create(path);
        using var db = new WinSqliteDatabase(copy.DatabasePath);
        EnsureLegacyCoreTables(db);
        List<string> keys = ReadSaveKeys(db);
        bool checkpoints = db.TableExists("storage_save_generation") || db.TableExists("storage_save_checkpoint");
        if (keys.Count == 0)
        {
            return new List<LegacyContaminationEntry>
            {
                LegacyContaminationScanner.AnalyzeTimeline(path, "sqlite", "(no save_key)", checkpoints, new List<LegacyEvent>())
            };
        }

        var result = new List<LegacyContaminationEntry>();
        foreach (string key in keys)
        {
            List<LegacyEvent> events = ReadEvents(db, "event_stream", key, int.MaxValue);
            result.Add(LegacyContaminationScanner.AnalyzeTimeline(path, "sqlite", key, checkpoints, events));
        }
        return result;
    }

    public static LegacySnapshot Read(string path, VanillaSaveInfo vanilla, string? requestedSaveKey, bool allowUnverified)
    {
        using TempSqliteCopy copy = TempSqliteCopy.Create(path);
        using var db = new WinSqliteDatabase(copy.DatabasePath);
        EnsureLegacyCoreTables(db);
        List<string> keys = ReadSaveKeys(db);
        string saveKey = ResolveSaveKey(keys, requestedSaveKey);
        string eventTable = "event_stream", customTable = "custom_data", checkpointStatus = "not_available";
        long? eventWatermark = null;
        bool hasGeneration = db.TableExists("storage_save_generation") && db.TableExists("storage_save_generation_table");
        bool hasSingleCheckpoint = db.TableExists("storage_save_checkpoint") && db.TableExists("storage_save_checkpoint_table");

        if (hasGeneration)
        {
            string sql = "SELECT generation_id, event_watermark FROM storage_save_generation WHERE save_key=" + Q(saveKey) +
                " AND vanilla_save_fingerprint=" + Q(vanilla.LegacyFileFingerprint) + " ORDER BY generation_id DESC LIMIT 1;";
            var rows = db.Query(sql);
            if (rows.Count > 0)
            {
                long id = AsLong(rows[0], "generation_id");
                eventWatermark = AsLong(rows[0], "event_watermark");
                customTable = ResolveManifestCustomTable(db, "storage_save_generation_table", "generation_id", id.ToString());
                checkpointStatus = "exact_match";
            }
            else if (!allowUnverified) throw NoCheckpoint();
            else checkpointStatus = "override_current_state_no_exact_match";
        }
        else if (hasSingleCheckpoint)
        {
            string sql = "SELECT vanilla_save_fingerprint, event_watermark FROM storage_save_checkpoint WHERE save_key=" + Q(saveKey) + " LIMIT 1;";
            var rows = db.Query(sql);
            if (rows.Count > 0 && string.Equals(AsString(rows[0], "vanilla_save_fingerprint"), vanilla.LegacyFileFingerprint, StringComparison.Ordinal))
            {
                eventWatermark = AsLong(rows[0], "event_watermark");
                customTable = ResolveManifestCustomTable(db, "storage_save_checkpoint_table", "save_key", Q(saveKey));
                checkpointStatus = "exact_match";
            }
            else if (!allowUnverified) throw NoCheckpoint();
            else checkpointStatus = "override_current_state_no_exact_match";
        }

        List<LegacyEvent> events = ReadEvents(db, eventTable, saveKey, int.MaxValue, eventWatermark);
        List<LegacyCustomData> custom = ReadCustomData(db, customTable, customTable == "custom_data" ? saveKey : null);
        (string version, string note) = DetectVersion(hasGeneration || hasSingleCheckpoint, events);
        long maxEventId = events.Count == 0 ? 0 : events.Max(e => e.EventId);
        long nextEventId = Math.Max(maxEventId, eventWatermark ?? 0L) + 1;
        var warnings = new List<string>();
        if (checkpointStatus == "not_available")
        {
            warnings.Add("SQLite source predates exact save-generation checkpoint tables; source/save association is user-supplied.");
            ValidatePreCheckpointTimeline(events, vanilla, allowUnverified, warnings);
        }
        if (checkpointStatus.StartsWith("override_", StringComparison.Ordinal)) warnings.Add("Advanced override used: checkpoint-capable 1.3 SQLite state was not proven to match the selected vanilla save.");
        return new LegacySnapshot
        {
            BackendKind = "sqlite", DetectedVersion = version, DetectionNote = note, SourceSaveKey = saveKey,
            CheckpointStatus = checkpointStatus, NextEventId = nextEventId, Events = events, CustomData = custom,
            ProjectionCounts = ReadProjectionCounts(db, saveKey), Warnings = warnings
        };
    }


    private static void ValidatePreCheckpointTimeline(
        List<LegacyEvent> events,
        VanillaSaveInfo vanilla,
        bool allowUnverified,
        List<string> warnings)
    {
        if (!GameDateUtilities.TryParseVanillaOrRoundTrip(vanilla.GameDateTime, out DateTime selectedSaveDate))
            return;

        int laterCount = 0;
        DateTime latest = DateTime.MinValue;
        foreach (LegacyEvent legacyEvent in events)
        {
            if (!GameDateUtilities.TryParseRoundTrip(legacyEvent.GameDateTime, out DateTime eventDate))
                continue;
            if (eventDate <= selectedSaveDate) continue;
            laterCount++;
            if (eventDate > latest) latest = eventDate;
        }

        if (laterCount == 0) return;

        string latestText = latest == DateTime.MinValue
            ? "unknown"
            : latest.ToString(GameDateUtilities.VanillaDataFormat, System.Globalization.CultureInfo.InvariantCulture);
        string selectedText = selectedSaveDate.ToString(GameDateUtilities.VanillaDataFormat, System.Globalization.CultureInfo.InvariantCulture);
        string message =
            $"This pre-checkpoint IMDataCore database contains {laterCount:N0} event(s) later than the selected vanilla save game date ({selectedText}); the latest legacy event is {latestText}. " +
            "The 1.x database therefore appears to contain history from a later branch/load that cannot be proven to belong to this vanilla checkpoint. " +
            "Select a matching later vanilla save, or enable the advanced unverified-association option to migrate the data knowingly.";

        if (!allowUnverified)
            throw new InvalidDataException(message);

        warnings.Add("Advanced override used: " + message);
    }

    private static string ResolveManifestCustomTable(WinSqliteDatabase db, string manifest, string idColumn, string idValue)
    {
        var rows = db.Query("SELECT table_name, snapshot_table_name FROM " + WinSqliteDatabase.QuoteIdentifier(manifest) +
            " WHERE " + WinSqliteDatabase.QuoteIdentifier(idColumn) + "=" + idValue + ";");
        foreach (var row in rows)
        {
            string table = AsString(row, "table_name"), snapshot = AsString(row, "snapshot_table_name");
            if (table == "custom_data" && db.TableExists(snapshot)) return snapshot;
        }
        throw new InvalidDataException("Exact SQLite checkpoint manifest has no custom_data snapshot; refusing to mix checkpointed events with current custom state.");
    }

    private static Dictionary<string, int> ReadProjectionCounts(WinSqliteDatabase db, string saveKey)
    {
        string[] tables = { "single_participation", "status_window", "show_cast_window", "contract_window", "relationship_window", "tour_participation", "award_result_projection", "election_result_projection", "push_window" };
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string t in tables)
        {
            if (!db.TableExists(t)) { result[t] = 0; continue; }
            var rows = db.Query("SELECT COUNT(*) AS n FROM " + WinSqliteDatabase.QuoteIdentifier(t) + " WHERE save_key=" + Q(saveKey) + ";");
            result[t] = rows.Count == 0 ? 0 : checked((int)AsLong(rows[0], "n"));
        }
        return result;
    }

    private static List<LegacyEvent> ReadEvents(WinSqliteDatabase db, string table, string? saveKey, int limit, long? maximumEventId = null)
    {
        var predicates = new List<string>();
        if (saveKey is not null) predicates.Add("save_key=" + Q(saveKey));
        if (maximumEventId.HasValue) predicates.Add("event_id <= " + maximumEventId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        string where = predicates.Count == 0 ? "" : " WHERE " + string.Join(" AND ", predicates);
        string lim = limit == int.MaxValue ? "" : " LIMIT " + limit;
        var rows = db.Query("SELECT event_id, game_date_key, game_datetime, idol_id, entity_kind, entity_id, event_type, source_patch, namespace_id, payload_json FROM " + WinSqliteDatabase.QuoteIdentifier(table) + where + " ORDER BY event_id ASC" + lim + ";");
        return rows.Select(r => new LegacyEvent
        {
            EventId = AsLong(r, "event_id"), GameDateKey = (int)AsLong(r, "game_date_key"), GameDateTime = AsString(r, "game_datetime"), IdolId = (int)AsLong(r, "idol_id", -1),
            EntityKind = AsString(r, "entity_kind"), EntityId = AsString(r, "entity_id"), EventType = AsString(r, "event_type"), SourcePatch = AsString(r, "source_patch"),
            NamespaceIdentifier = AsString(r, "namespace_id"), PayloadJson = string.IsNullOrWhiteSpace(AsString(r, "payload_json")) ? "{}" : AsString(r, "payload_json")
        }).ToList();
    }

    private static List<LegacyCustomData> ReadCustomData(WinSqliteDatabase db, string table, string? saveKey)
    {
        if (!db.TableExists(table)) return new List<LegacyCustomData>();
        string where = saveKey is null ? "" : " WHERE save_key=" + Q(saveKey);
        var rows = db.Query("SELECT namespace_id, data_key, value_json, updated_utc FROM " + WinSqliteDatabase.QuoteIdentifier(table) + where + " ORDER BY namespace_id, data_key;");
        return rows.Select(r => new LegacyCustomData { NamespaceIdentifier = AsString(r, "namespace_id"), DataKey = AsString(r, "data_key"), ValueJson = AsString(r, "value_json"), UpdatedUtc = AsString(r, "updated_utc") }).ToList();
    }

    private static void EnsureLegacyCoreTables(WinSqliteDatabase db)
    {
        if (!db.TableExists("event_stream") || !db.TableExists("custom_data"))
            throw new InvalidDataException("Selected SQLite file is not an IMDataCore 1.x database (event_stream/custom_data missing).");
    }

    private static List<string> ReadSaveKeys(WinSqliteDatabase db)
    {
        var rows = db.Query("SELECT save_key FROM event_stream UNION SELECT save_key FROM custom_data ORDER BY save_key;");
        return rows.Select(r => AsString(r, "save_key")).Where(x => !string.IsNullOrEmpty(x)).Distinct(StringComparer.Ordinal).ToList();
    }

    private static string ResolveSaveKey(List<string> keys, string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            if (!keys.Contains(requested, StringComparer.Ordinal)) throw new InvalidDataException("Requested source save_key was not found in the legacy SQLite database.");
            return requested;
        }
        if (keys.Count == 1) return keys[0];
        if (keys.Count == 0) return "default";
        throw new InvalidDataException("Legacy SQLite database contains multiple save_key values. Select one explicitly.");
    }

    private static (string, string) DetectVersion(bool checkpoints, List<LegacyEvent> events)
    {
        if (checkpoints) return ("1.3.0 (late)", "Save-generation checkpoint tables were added during the 1.3.0 commit range.");
        if (events.Any(e => e.EventType is "money_transaction" or "money_ledger_coverage_started")) return ("1.2.0-1.3.0", "Money Ledger events first appear in 1.2.0.");
        if (events.Any(e => e.EventType == "room_work_completed")) return ("1.1.0-1.3.0", "Room-work events first appear in 1.1.0.");
        return ("1.0.0-1.3.0", "SQLite schema_version remained 2 across the 1.x releases, so the database alone may not identify the exact mod release.");
    }

    private static InvalidDataException NoCheckpoint() => new("This late-1.3 SQLite database supports exact save checkpoints, but none matches the selected vanilla save. Refusing safe migration.");
    private static string Q(string value) => WinSqliteDatabase.QuoteLiteral(value);
    private static string AsString(Dictionary<string, object?> row, string key) => row.TryGetValue(key, out object? v) ? Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) ?? "" : "";
    private static long AsLong(Dictionary<string, object?> row, string key, long fallback = 0) => row.TryGetValue(key, out object? v) && v is not null ? Convert.ToInt64(v, System.Globalization.CultureInfo.InvariantCulture) : fallback;

    private sealed class TempSqliteCopy : IDisposable
    {
        public string DirectoryPath { get; private init; } = "";
        public string DatabasePath { get; private init; } = "";
        public static TempSqliteCopy Create(string source)
        {
            string dir = Path.Combine(Path.GetTempPath(), "IMDataCore-Data-Migration-Tool", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string db = Path.Combine(dir, Path.GetFileName(source));
            File.Copy(source, db, true);
            foreach (string suffix in new[] { "-wal", "-shm" }) if (File.Exists(source + suffix)) File.Copy(source + suffix, db + suffix, true);
            return new TempSqliteCopy { DirectoryPath = dir, DatabasePath = db };
        }
        public void Dispose() { try { Directory.Delete(DirectoryPath, true); } catch { } }
    }
}
