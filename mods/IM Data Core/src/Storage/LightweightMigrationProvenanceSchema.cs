using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace IMDataCore
{
    /// <summary>
    /// Sidecar-v6 physical-format provenance. This record is deliberately
    /// document-level and non-rewinding: it explains whether the v6 generation
    /// is native or the committed result of one bounded legacy conversion. It is
    /// not gameplay branch state and it must never be inferred from checkpoint
    /// mod inventory, current runtime objects, or first post-migration use.
    /// </summary>
    internal static class LightweightMigrationProvenanceSchema
    {
        internal const int ProvenanceSchemaVersion = 1;
        internal const string OriginNativeV6 = "native_v6";
        internal const string OriginLegacyMigration = "legacy_migration";
        internal const string SourceHashPrefix = "sha256:";
        internal const string ConversionIdPrefix = "migration-v1:";

        internal static LightweightMigrationProvenanceRecord CreateNativeV6()
        {
            return new LightweightMigrationProvenanceRecord
            {
                ProvenanceSchemaVersion = ProvenanceSchemaVersion,
                Origin = OriginNativeV6,
                SourceFormatName = LightweightCoreStorageEngine.SidecarFormatName,
                SourceFormatVersion = LightweightIdentityBindingSchema.SidecarFormatVersion,
                SourceDocumentHash = string.Empty,
                SourceLastIssuedSequence = 0L,
                TargetFormatName = LightweightCoreStorageEngine.SidecarFormatName,
                TargetFormatVersion = LightweightIdentityBindingSchema.SidecarFormatVersion,
                TargetJournalFormatVersion = LightweightJournalV3Schema.JournalFormatVersion,
                ConversionId = string.Empty
            };
        }

        internal static LightweightMigrationProvenanceRecord
            CreateLegacyMigration(
                string sourceFormatName,
                int sourceFormatVersion,
                string sourceRelativeSavePath,
                long sourceLastIssuedSequence,
                string sourceJson)
        {
            if (!string.Equals(
                    sourceFormatName,
                    LightweightCoreStorageEngine.SidecarFormatName,
                    StringComparison.Ordinal) ||
                sourceFormatVersion < LegacySidecarMigration.MinimumSourceFormatVersion ||
                sourceFormatVersion > LegacySidecarMigration.MaximumSourceFormatVersion ||
                sourceLastIssuedSequence < 0L)
            {
                throw new FormatException(
                    "Legacy migration provenance has an unsupported source generation.");
            }

            LightweightMigrationProvenanceRecord record =
                new LightweightMigrationProvenanceRecord
                {
                    ProvenanceSchemaVersion = ProvenanceSchemaVersion,
                    Origin = OriginLegacyMigration,
                    SourceFormatName = sourceFormatName,
                    SourceFormatVersion = sourceFormatVersion,
                    SourceDocumentHash = ComputeSourceDocumentHash(sourceJson),
                    SourceLastIssuedSequence = sourceLastIssuedSequence,
                    TargetFormatName = LightweightCoreStorageEngine.SidecarFormatName,
                    TargetFormatVersion = LightweightIdentityBindingSchema.SidecarFormatVersion,
                    TargetJournalFormatVersion = LightweightJournalV3Schema.JournalFormatVersion
                };
            record.ConversionId = BuildConversionId(
                record,
                sourceRelativeSavePath);
            return record;
        }

        /// <summary>
        /// Logical text fingerprint, not the journal affinity hash of the physical
        /// compact base. CR/LF spelling is normalized so the identity belongs to
        /// the decoded JSON source rather than to the host checkout convention.
        /// The physical base/journal SHA-256 affinity remains #58's responsibility.
        /// </summary>
        internal static string ComputeSourceDocumentHash(string sourceJson)
        {
            string normalized = (sourceJson ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");
            byte[] bytes = Encoding.UTF8.GetBytes(normalized);
            using (SHA256 sha256 = SHA256.Create())
            {
                return SourceHashPrefix + ToLowerHex(sha256.ComputeHash(bytes));
            }
        }

        internal static string BuildConversionId(
            LightweightMigrationProvenanceRecord record,
            string relativeSavePath)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            StringBuilder builder = new StringBuilder(512);
            AppendCanonicalField(
                builder,
                record.ProvenanceSchemaVersion.ToString(
                    CultureInfo.InvariantCulture));
            AppendCanonicalField(builder, record.Origin ?? string.Empty);
            AppendCanonicalField(builder, record.SourceFormatName ?? string.Empty);
            AppendCanonicalField(
                builder,
                record.SourceFormatVersion.ToString(
                    CultureInfo.InvariantCulture));
            AppendCanonicalField(builder, record.SourceDocumentHash ?? string.Empty);
            AppendCanonicalField(
                builder,
                record.SourceLastIssuedSequence.ToString(
                    CultureInfo.InvariantCulture));
            AppendCanonicalField(
                builder,
                VanillaSaveStamp.NormalizeRelativePath(relativeSavePath));
            AppendCanonicalField(builder, record.TargetFormatName ?? string.Empty);
            AppendCanonicalField(
                builder,
                record.TargetFormatVersion.ToString(
                    CultureInfo.InvariantCulture));
            AppendCanonicalField(
                builder,
                record.TargetJournalFormatVersion.ToString(
                    CultureInfo.InvariantCulture));

            byte[] bytes = Encoding.UTF8.GetBytes(builder.ToString());
            using (SHA256 sha256 = SHA256.Create())
            {
                return ConversionIdPrefix +
                    ToLowerHex(sha256.ComputeHash(bytes));
            }
        }

        internal static void ValidateDocumentForV6(
            LightweightSidecarDocument document)
        {
            if (document == null || document.MigrationProvenance == null)
            {
                throw new FormatException(
                    "A sidecar-v6 document is missing required migration provenance.");
            }

            LightweightMigrationProvenanceRecord record =
                document.MigrationProvenance;
            if (record.ProvenanceSchemaVersion != ProvenanceSchemaVersion ||
                !string.Equals(
                    record.TargetFormatName,
                    document.FormatName,
                    StringComparison.Ordinal) ||
                record.TargetFormatVersion != document.FormatVersion ||
                record.TargetFormatVersion !=
                    LightweightIdentityBindingSchema.SidecarFormatVersion ||
                record.TargetJournalFormatVersion !=
                    LightweightJournalV3Schema.JournalFormatVersion)
            {
                throw new FormatException(
                    "A sidecar-v6 migration provenance record targets an unsupported storage generation.");
            }

            if (string.Equals(
                    record.Origin,
                    OriginNativeV6,
                    StringComparison.Ordinal))
            {
                ValidateNativeRecord(record, document);
                return;
            }

            if (!string.Equals(
                    record.Origin,
                    OriginLegacyMigration,
                    StringComparison.Ordinal))
            {
                throw new FormatException(
                    "A sidecar-v6 migration provenance record has an unsupported origin.");
            }

            ValidateLegacyMigrationRecord(record, document);
        }

        internal static bool IsCommittedLegacyMigration(
            LightweightSidecarDocument document)
        {
            try
            {
                ValidateDocumentForV6(document);
                return string.Equals(
                    document.MigrationProvenance.Origin,
                    OriginLegacyMigration,
                    StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        private static void ValidateNativeRecord(
            LightweightMigrationProvenanceRecord record,
            LightweightSidecarDocument document)
        {
            if (!string.Equals(
                    record.SourceFormatName,
                    LightweightCoreStorageEngine.SidecarFormatName,
                    StringComparison.Ordinal) ||
                record.SourceFormatVersion !=
                    LightweightIdentityBindingSchema.SidecarFormatVersion ||
                !string.IsNullOrEmpty(record.SourceDocumentHash) ||
                record.SourceLastIssuedSequence != 0L ||
                !string.IsNullOrEmpty(record.ConversionId))
            {
                throw new FormatException(
                    "A native sidecar-v6 provenance record contains legacy-conversion state.");
            }

            if (document.CoverageTransitions == null)
            {
                return;
            }
            for (int index = 0; index < document.CoverageTransitions.Count; index++)
            {
                LightweightCoverageTransitionRecord transition =
                    document.CoverageTransitions[index];
                if (transition != null && string.Equals(
                        transition.Origin,
                        LightweightCoverageSchema.OriginLegacyResume,
                        StringComparison.Ordinal))
                {
                    throw new FormatException(
                        "A native sidecar-v6 document cannot claim a LegacyResume coverage origin.");
                }
            }
        }

        private static void ValidateLegacyMigrationRecord(
            LightweightMigrationProvenanceRecord record,
            LightweightSidecarDocument document)
        {
            if (!string.Equals(
                    record.SourceFormatName,
                    LightweightCoreStorageEngine.SidecarFormatName,
                    StringComparison.Ordinal) ||
                record.SourceFormatVersion <
                    LegacySidecarMigration.MinimumSourceFormatVersion ||
                record.SourceFormatVersion >
                    LegacySidecarMigration.MaximumSourceFormatVersion ||
                !IsValidPrefixedHash(
                    record.SourceDocumentHash,
                    SourceHashPrefix) ||
                record.SourceLastIssuedSequence < 0L ||
                record.SourceLastIssuedSequence > document.LastIssuedSequence)
            {
                throw new FormatException(
                    "A migrated sidecar-v6 provenance record has invalid legacy source metadata.");
            }

            string expectedConversionId = BuildConversionId(
                record,
                document.RelativeSavePath);
            if (!string.Equals(
                    record.ConversionId,
                    expectedConversionId,
                    StringComparison.Ordinal))
            {
                throw new FormatException(
                    "A migrated sidecar-v6 provenance record has an invalid conversion identity.");
            }

            if (document.CoverageTransitions == null)
            {
                return;
            }
            for (int index = 0; index < document.CoverageTransitions.Count; index++)
            {
                LightweightCoverageTransitionRecord transition =
                    document.CoverageTransitions[index];
                if (transition == null)
                {
                    continue;
                }

                if (string.Equals(
                        transition.Origin,
                        LightweightCoverageSchema.OriginCareerStart,
                        StringComparison.Ordinal) ||
                    string.Equals(
                        transition.Origin,
                        LightweightCoverageSchema.OriginLateAdoption,
                        StringComparison.Ordinal))
                {
                    throw new FormatException(
                        "A migrated sidecar-v6 document cannot backdate its legacy source as CareerStart or LateAdoption.");
                }
            }
        }

        private static bool IsValidPrefixedHash(
            string value,
            string prefix)
        {
            if (string.IsNullOrEmpty(value) ||
                string.IsNullOrEmpty(prefix) ||
                value.Length != prefix.Length + 64 ||
                !value.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            for (int index = prefix.Length; index < value.Length; index++)
            {
                char ch = value[index];
                if (!((ch >= '0' && ch <= '9') ||
                      (ch >= 'a' && ch <= 'f')))
                {
                    return false;
                }
            }
            return true;
        }

        private static void AppendCanonicalField(
            StringBuilder builder,
            string value)
        {
            string safe = value ?? string.Empty;
            builder.Append(safe.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(safe);
            builder.Append('|');
        }

        private static string ToLowerHex(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }

            const string Hex = "0123456789abcdef";
            char[] chars = new char[bytes.Length * 2];
            for (int index = 0; index < bytes.Length; index++)
            {
                byte value = bytes[index];
                chars[index * 2] = Hex[value >> 4];
                chars[(index * 2) + 1] = Hex[value & 0x0F];
            }
            return new string(chars);
        }
    }

    [Serializable]
    internal sealed class LightweightMigrationProvenanceRecord
    {
        public int ProvenanceSchemaVersion;
        public string Origin = string.Empty;
        public string SourceFormatName = string.Empty;
        public int SourceFormatVersion;
        public string SourceDocumentHash = string.Empty;
        public long SourceLastIssuedSequence;
        public string TargetFormatName = string.Empty;
        public int TargetFormatVersion;
        public int TargetJournalFormatVersion;
        public string ConversionId = string.Empty;
    }

    /// <summary>
    /// Typed signal used by the live v5 reader so a newer/older authoritative
    /// sidecar generation is not misclassified as ordinary corruption and healed
    /// from an older backup. The caller must preserve the primary bytes and block
    /// persistence for that physical save scope.
    /// </summary>
    internal sealed class LightweightUnsupportedSidecarFormatException :
        FormatException
    {
        internal readonly int UnsupportedFormatVersion;

        internal LightweightUnsupportedSidecarFormatException(int formatVersion)
            : base(
                "The lightweight sidecar format " +
                formatVersion.ToString(CultureInfo.InvariantCulture) +
                " is unsupported by this IM Data Core version.")
        {
            UnsupportedFormatVersion = formatVersion;
        }
    }
}
