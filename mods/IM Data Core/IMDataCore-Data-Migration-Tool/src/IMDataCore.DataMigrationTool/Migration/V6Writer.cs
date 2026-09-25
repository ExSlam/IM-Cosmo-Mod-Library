using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace IMDataCore.DataMigrationTool.Migration;

internal static class V6Writer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static string Serialize(V6Document document) => JsonSerializer.Serialize(document, Options);

    public static V6Document Build(LegacySnapshot source, VanillaSaveInfo vanilla, string sourcePath, string versionOverride)
    {
        ValidateLegacySource(source, vanilla);
        var document = new V6Document { RelativeSavePath = NormalizeRelative(vanilla.RelativeSavePath) };

        var orderedEvents = source.Events.OrderBy(e => e.EventId).ToList();
        long previous = 0;
        foreach (LegacyEvent old in orderedEvents)
        {
            if (old.EventId <= 0 || old.EventId <= previous)
                throw new InvalidDataException("Legacy event identifiers must be positive and strictly increasing after sorting.");
            previous = old.EventId;
            document.Events.Add(new V6Event
            {
                Sequence = old.EventId,
                GameDateTime = old.GameDateTime,
                IdolId = old.IdolId,
                EntityKind = old.EntityKind ?? "",
                EntityId = old.EntityId ?? "",
                EventType = old.EventType ?? "",
                SourcePatch = old.SourcePatch ?? "",
                NamespaceIdentifier = old.NamespaceIdentifier ?? "",
                Payload = ParseAnyJson(old.PayloadJson, "legacy event payload"),
                // Current IMDC explicitly permits 0 for migrated shared events whose current participant
                // contract was not provable from the old generation.
                ParticipantSchemaVersion = 0
            });
        }

        long nextSequence = Math.Max(previous, Math.Max(0, source.NextEventId - 1));
        string syntheticMutationGameDateTime = GameDateUtilities.ToRoundTripFromVanilla(vanilla.GameDateTime);
        foreach (LegacyCustomData old in source.CustomData.OrderBy(x => x.NamespaceIdentifier, StringComparer.Ordinal).ThenBy(x => x.DataKey, StringComparer.Ordinal))
        {
            checked { nextSequence++; }
            document.CustomMutations.Add(new V6CustomMutation
            {
                Sequence = nextSequence,
                GameDateTime = syntheticMutationGameDateTime,
                NamespaceIdentifier = old.NamespaceIdentifier ?? "",
                DataKey = old.DataKey ?? "",
                Operation = "set",
                Value = ParseAnyJson(old.ValueJson, "legacy custom-data value")
            });
        }

        document.LastIssuedSequence = nextSequence;
        document.Checkpoints.Add(new V6Checkpoint
        {
            RelativeSavePath = document.RelativeSavePath,
            LastSave = vanilla.LastSave,
            PlaytimeSeconds = vanilla.PlaytimeSeconds,
            GameDateTime = vanilla.GameDateTime,
            ContentFingerprint = vanilla.V6ContentFingerprint,
            Sequence = nextSequence,
            // The old generation did not carry v6 checkpoint correlation state. Empty/incomplete is the
            // schema-sanctioned representation and lets current IMDC bind/populate it on a later save.
            IdentityBindingsVersion = 1,
            IdentityBindingsComplete = false
        });

        var namespaces = document.Events.Select(e => e.NamespaceIdentifier)
            .Concat(document.CustomMutations.Select(m => m.NamespaceIdentifier))
            .Where(x => !string.IsNullOrEmpty(x)).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal);
        foreach (string ns in namespaces)
            document.NamespaceOwnerBindings.Add(new V6NamespaceBinding { NamespaceIdentifier = ns });

        string sourceHash = "sha256:" + HashUtil.Sha256Hex(File.ReadAllBytes(sourcePath));
        document.MigrationProvenance = new V6MigrationProvenance
        {
            SourceDocumentHash = sourceHash,
            SourceLastIssuedSequence = nextSequence
        };
        document.MigrationProvenance.ConversionId = BuildConversionId(document.MigrationProvenance, document.RelativeSavePath);

        string selectedVersion = string.IsNullOrWhiteSpace(versionOverride) ? source.DetectedVersion : versionOverride;
        var extensionPayload = new JsonObject
        {
            ["Tool"] = "IMDataCore Data Migration Tool",
            ["ToolVersion"] = IMDataCore.DataMigrationTool.ToolInfo.Version,
            ["Author"] = "Cosmo",
            ["OriginalBackend"] = source.BackendKind,
            ["OriginalIMDataCoreVersion"] = selectedVersion,
            ["DetectionNote"] = source.DetectionNote,
            ["CheckpointStatus"] = source.CheckpointStatus,
            ["SourceFileName"] = Path.GetFileName(sourcePath),
            ["SourceSha256"] = sourceHash,
            ["LegacyNextEventId"] = source.NextEventId,
            ["MigratedEventCount"] = source.Events.Count,
            ["MigratedCustomDataCount"] = source.CustomData.Count,
            ["ProjectionCounts"] = JsonSerializer.SerializeToNode(source.ProjectionCounts),
            ["Warnings"] = JsonSerializer.SerializeToNode(source.Warnings),
            ["BridgeModel"] = "pre2-db -> synthetic-lightweight-v1 provenance envelope -> sidecar-v6"
        };
        document.ForwardExtensions.Add(new V6ForwardExtension
        {
            ExtensionId = "cosmo.imdatacore.data-migration-tool.pre2-migration",
            ExtensionSchemaVersion = 1,
            RequiredForRead = false,
            PayloadJson = extensionPayload.ToJsonString(new JsonSerializerOptions { WriteIndented = false })
        });

        if (source.RepairApplied)
        {
            var repairPayload = new JsonObject
            {
                ["Tool"] = "IMDataCore Data Migration Tool",
                ["ToolVersion"] = IMDataCore.DataMigrationTool.ToolInfo.Version,
                ["RepairMode"] = source.RepairMode,
                ["RepairSummary"] = source.RepairSummary,
                ["SelectedBranchIndex"] = source.RepairBranchIndex,
                ["BranchStartEventId"] = source.RepairBranchStartEventId,
                ["BranchEndEventId"] = source.RepairBranchEndEventId,
                ["OriginalEventCount"] = source.RepairOriginalEventCount,
                ["RetainedEventCount"] = source.Events.Count,
                ["DroppedEventCount"] = source.RepairDroppedEventCount,
                ["OriginalCustomDataCount"] = source.RepairOriginalCustomDataCount,
                ["RetainedCustomDataCount"] = source.CustomData.Count,
                ["DroppedCustomDataCount"] = source.RepairDroppedCustomDataCount,
                ["SelectedVanillaGameDateTime"] = vanilla.GameDateTime,
                ["DoesNotInventMissingHistory"] = true
            };
            document.ForwardExtensions.Add(new V6ForwardExtension
            {
                ExtensionId = "cosmo.imdatacore.data-migration-tool.branch-repair",
                ExtensionSchemaVersion = 1,
                RequiredForRead = false,
                PayloadJson = repairPayload.ToJsonString(new JsonSerializerOptions { WriteIndented = false })
            });
        }

        V6StaticValidator.Validate(document);
        return document;
    }

    private static void ValidateLegacySource(LegacySnapshot source, VanillaSaveInfo vanilla)
    {
        if (!GameDateUtilities.TryParseVanillaOrRoundTrip(vanilla.GameDateTime, out _))
            throw new InvalidDataException("Vanilla save staticVars__dateTime is not a valid Idol Manager game date; custom-data baseline/checkpoint cannot be safely stamped.");
        foreach (LegacyEvent e in source.Events)
        {
            if (!GameDateUtilities.TryParseRoundTrip(e.GameDateTime, out DateTime parsed))
                throw new InvalidDataException("A legacy event has an invalid round-trip GameDateTime.");
            int expectedKey = checked(parsed.Year * 10000 + parsed.Month * 100 + parsed.Day);
            if (e.GameDateKey != expectedKey)
                throw new InvalidDataException($"Legacy event {e.EventId} has GameDateKey {e.GameDateKey}, expected {expectedKey} from GameDateTime.");
            _ = ParseAnyJson(e.PayloadJson, "legacy event payload");
        }
        foreach (LegacyCustomData c in source.CustomData)
        {
            if (string.IsNullOrWhiteSpace(c.NamespaceIdentifier) || string.IsNullOrWhiteSpace(c.DataKey))
                throw new InvalidDataException("A legacy custom-data row has an empty namespace or data key.");
            _ = ParseAnyJson(c.ValueJson, "legacy custom-data value");
        }
    }


    private static JsonNode? ParseAnyJson(string json, string label)
    {
        try { return JsonNode.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json); }
        catch (JsonException ex) { throw new InvalidDataException(label + " is invalid JSON: " + ex.Message, ex); }
    }

    internal static string BuildConversionId(V6MigrationProvenance p, string relativePath)
    {
        var sb = new StringBuilder(512);
        Append(sb, p.ProvenanceSchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Append(sb, p.Origin); Append(sb, p.SourceFormatName);
        Append(sb, p.SourceFormatVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Append(sb, p.SourceDocumentHash);
        Append(sb, p.SourceLastIssuedSequence.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Append(sb, NormalizeRelative(relativePath)); Append(sb, p.TargetFormatName);
        Append(sb, p.TargetFormatVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Append(sb, p.TargetJournalFormatVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return "migration-v1:" + HashUtil.Sha256Hex(sb.ToString());
    }

    private static void Append(StringBuilder sb, string? value)
    {
        string safe = value ?? "";
        sb.Append(safe.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(':').Append(safe).Append('|');
    }

    internal static string NormalizeRelative(string value) => (value ?? "").Trim().Replace('\\', '/');
}
