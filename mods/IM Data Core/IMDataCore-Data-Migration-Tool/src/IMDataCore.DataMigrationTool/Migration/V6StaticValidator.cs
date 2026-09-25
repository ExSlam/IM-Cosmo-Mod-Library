using System.Text.RegularExpressions;

namespace IMDataCore.DataMigrationTool.Migration;

internal static class V6StaticValidator
{
    private static readonly Regex Fingerprint = new("^sha256:[0-9a-f]{64}$", RegexOptions.CultureInvariant);
    private static readonly Regex Conversion = new("^migration-v1:[0-9a-f]{64}$", RegexOptions.CultureInvariant);

    public static void Validate(V6Document d)
    {
        if (d.FormatName != "IMDataCore.LightweightSidecar" || d.FormatVersion != 6) throw new InvalidDataException("Target envelope is not sidecar v6.");
        if (d.ForwardCompatibilitySchemaVersion != 1 || d.CoverageModelVersion != 1) throw new InvalidDataException("Target v6 schema discriminator mismatch.");
        if (string.IsNullOrWhiteSpace(d.RelativeSavePath)) throw new InvalidDataException("Target RelativeSavePath is empty.");
        if (d.MigrationProvenance.ProvenanceSchemaVersion != 1 || d.MigrationProvenance.Origin != "legacy_migration" || d.MigrationProvenance.SourceFormatVersion != 1 || d.MigrationProvenance.TargetFormatVersion != 6 || d.MigrationProvenance.TargetJournalFormatVersion != 3) throw new InvalidDataException("Target migration provenance is inconsistent.");
        if (!Fingerprint.IsMatch(d.MigrationProvenance.SourceDocumentHash)) throw new InvalidDataException("SourceDocumentHash is malformed.");
        string expected = V6Writer.BuildConversionId(d.MigrationProvenance, d.RelativeSavePath);
        if (!Conversion.IsMatch(d.MigrationProvenance.ConversionId) || d.MigrationProvenance.ConversionId != expected) throw new InvalidDataException("Migration ConversionId is invalid.");

        var used = new HashSet<long>();
        long prev = 0, maximum = 0;
        foreach (V6Event e in d.Events)
        {
            if (e.Sequence <= prev || e.Sequence <= 0 || !used.Add(e.Sequence)) throw new InvalidDataException("Event sequence order is invalid.");
            if (e.ParticipantSchemaVersion != 0) throw new InvalidDataException("Migrated event participant schema must be legacy-unknown (0).");
            prev = e.Sequence; maximum = Math.Max(maximum, e.Sequence);
        }
        prev = 0;
        foreach (V6CustomMutation m in d.CustomMutations)
        {
            if (m.Sequence <= prev || m.Sequence <= 0 || !used.Add(m.Sequence) || m.Operation != "set") throw new InvalidDataException("Custom mutation sequence/order is invalid.");
            prev = m.Sequence; maximum = Math.Max(maximum, m.Sequence);
        }
        if (d.LastIssuedSequence < maximum) throw new InvalidDataException("LastIssuedSequence is below retained history.");
        if (d.Checkpoints.Count != 1) throw new InvalidDataException("Data Migration Tool output must contain exactly one exact vanilla checkpoint.");
        V6Checkpoint c = d.Checkpoints[0];
        if (!Fingerprint.IsMatch(c.ContentFingerprint) || c.Sequence < 0 || c.Sequence > d.LastIssuedSequence) throw new InvalidDataException("Checkpoint fingerprint/sequence is invalid.");
        if (c.IdentityBindingsVersion != 1 || c.IdentityBindingsComplete || c.IdentityBindings.Count != 0 || c.IdentityCandidates.Count != 0) throw new InvalidDataException("Migrated checkpoint must be a valid legacy-unbound v6 identity checkpoint.");

        var bound = d.NamespaceOwnerBindings.Select(x => x.NamespaceIdentifier).ToHashSet(StringComparer.Ordinal);
        foreach (string ns in d.Events.Select(x => x.NamespaceIdentifier).Concat(d.CustomMutations.Select(x => x.NamespaceIdentifier)).Where(x => !string.IsNullOrEmpty(x)))
            if (!bound.Contains(ns)) throw new InvalidDataException("Populated namespace lacks durable legacy-unbound owner binding: " + ns);
        foreach (V6NamespaceBinding b in d.NamespaceOwnerBindings)
            if (b.BindingSchemaVersion != 1 || b.BindingRevision != 1 || b.OwnerSchemaVersion != 0 || b.OwnershipKnown || b.Origin != "legacy_unbound" || !string.IsNullOrEmpty(b.StableOwnerId) || !string.IsNullOrEmpty(b.CurrentAssemblyWitness)) throw new InvalidDataException("Namespace owner binding is not a valid legacy-unbound record.");
    }
}
