using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace IMDataCore
{
    /// <summary>
    /// Staged sidecar-v6 migration seam. It deliberately is not invoked by the
    /// live v5/v2 storage engine yet. The cutover can call this helper once the
    /// remaining coverage lifecycle has been integrated and regression-proven.
    ///
    /// The important invariant is atomic logical materialization: a legacy compact
    /// base and any matching committed v2 journal are decoded into one v6 document
    /// before any replacement v6 bytes are eligible for publication.
    /// </summary>
    internal static class LightweightLegacyGenerationMigration
    {
        internal sealed class Result
        {
            internal LightweightSidecarDocument Document;
            internal int SourceFormatVersion;
            internal int ReplayedJournalEntryCount;
            internal bool JournalHeaderMatched;
            internal bool ForceFullSnapshot;
        }

        internal static bool TryMaterializeForV6(
            byte[] legacyBaseBytes,
            byte[] legacyJournalBytes,
            string expectedRelativeSavePath,
            out Result result,
            out string errorMessage)
        {
            result = null;
            errorMessage = string.Empty;
            if (legacyBaseBytes == null || legacyBaseBytes.Length == 0)
            {
                errorMessage = "The legacy IMDC compact base is empty.";
                return false;
            }

            try
            {
                string baseJson = DecodeUtf8(legacyBaseBytes);
                LegacySidecarMigrationResult migration;
                using (StringReader reader = new StringReader(baseJson))
                {
                    migration = LightweightSidecarJson.DeserializeForMigration(reader);
                }

                LightweightSidecarDocument document = migration.Document;
                string expected = VanillaSaveStamp.NormalizeRelativePath(
                    expectedRelativeSavePath);
                string actual = VanillaSaveStamp.NormalizeRelativePath(
                    document.RelativeSavePath);
                if (!string.Equals(expected, actual, CorePaths.PathComparison))
                {
                    errorMessage =
                        "The legacy IMDC generation belongs to a different vanilla save path.";
                    return false;
                }

                int replayedJournalEntryCount = 0;
                bool journalHeaderMatched = false;
                bool forceFullSnapshot = false;
                if (legacyJournalBytes != null && legacyJournalBytes.Length > 0)
                {
                    string journalText = DecodeUtf8(legacyJournalBytes);
                    bool endsWithNewline = journalText.EndsWith("\n", StringComparison.Ordinal);
                    using (StringReader journalReader = new StringReader(journalText))
                    {
                        string headerLine = journalReader.ReadLine();
                        LightweightJournalHeaderAffinityDecision affinity;
                        LightweightJournalHeaderEnvelope header;
                        string headerError;
                        if (!LightweightSidecarJson.TryClassifyJournalHeaderForCandidate(
                                headerLine,
                                ComputeSha256Hex(legacyBaseBytes),
                                2,
                                out affinity,
                                out header,
                                out headerError))
                        {
                            errorMessage =
                                "The legacy IMDC journal header is invalid: " + headerError;
                            return false;
                        }

                        if (affinity ==
                            LightweightJournalHeaderAffinityDecision.HeaderMatchedUnsupported)
                        {
                            errorMessage =
                                "The journal bound to the legacy IMDC base uses an unsupported format.";
                            return false;
                        }

                        if (affinity ==
                            LightweightJournalHeaderAffinityDecision.HeaderMatchedSupported)
                        {
                            journalHeaderMatched = true;
                            string replayError;
                            if (!LightweightSidecarJson.TryReplayJournalTransactions(
                                    journalReader,
                                    endsWithNewline,
                                    document,
                                    out replayedJournalEntryCount,
                                    out forceFullSnapshot,
                                    out replayError))
                            {
                                errorMessage =
                                    "The matching legacy IMDC journal could not be replayed atomically: " +
                                    replayError;
                                return false;
                            }
                        }
                        else
                        {
                            // A stale/mismatched journal is not authoritative for this
                            // compact base. Migration may proceed from the validated base,
                            // but the first v6 publication must be a full snapshot.
                            forceFullSnapshot = true;
                        }
                    }
                }

                if (journalHeaderMatched && replayedJournalEntryCount > 0)
                {
                    // The migration provenance must identify the complete
                    // committed logical legacy source, not only the compact base
                    // whose hash was used for journal affinity. Keep base-only
                    // migrations byte-for-byte stable, but once a v2 transaction
                    // contributes durable state, fingerprint a canonical v1-v5
                    // logical materialization that includes the replayed suffix.
                    document.MigrationProvenance.SourceDocumentHash =
                        ComputeMaterializedLegacySourceHash(
                            document,
                            migration.SourceFormatVersion);
                }

                NormalizeJournalAddedLegacyRows(document);
                LightweightNamespaceOwnerSchema.ValidateDocumentForV6(document);
                LightweightCoverageSchema.ValidateDocumentForV6(document);
                LightweightHistoricalBaselineSchema.ValidateDocumentForV6(document);
                LightweightMigrationProvenanceSchema.ValidateDocumentForV6(document);

                result = new Result
                {
                    Document = document,
                    SourceFormatVersion = migration.SourceFormatVersion,
                    ReplayedJournalEntryCount = replayedJournalEntryCount,
                    JournalHeaderMatched = journalHeaderMatched,
                    // A converted legacy generation is never journal-appended in
                    // place. Its first durable v6 publication is an atomic snapshot.
                    ForceFullSnapshot = forceFullSnapshot || migration.SourceFormatVersion <= 5
                };
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                result = null;
                return false;
            }
        }

        private static string ComputeMaterializedLegacySourceHash(
            LightweightSidecarDocument document,
            int sourceFormatVersion)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }
            if (sourceFormatVersion <
                    LegacySidecarMigration.MinimumSourceFormatVersion ||
                sourceFormatVersion >
                    LegacySidecarMigration.MaximumSourceFormatVersion)
            {
                throw new ArgumentOutOfRangeException("sourceFormatVersion");
            }

            // Serialize only the legacy logical surface. The v6-only migration,
            // owner, capability, coverage, baseline, participant, and identity
            // metadata are deliberately excluded from the source witness. The
            // compact codec gives one deterministic representation of the base
            // three row families after committed v2 replay.
            LightweightSidecarDocument logicalSource =
                new LightweightSidecarDocument
                {
                    FormatName = LightweightCoreStorageEngine.SidecarFormatName,
                    FormatVersion = sourceFormatVersion,
                    RelativeSavePath = document.RelativeSavePath,
                    LastIssuedSequence = document.LastIssuedSequence,
                    Checkpoints = document.Checkpoints,
                    Events = document.Events,
                    CustomMutations = document.CustomMutations
                };
            using (StringWriter writer = new StringWriter())
            {
                LightweightSidecarJson.SerializeTo(writer, logicalSource);
                return LightweightMigrationProvenanceSchema
                    .ComputeSourceDocumentHash(writer.ToString());
            }
        }

        private static void NormalizeJournalAddedLegacyRows(
            LightweightSidecarDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }

            if (document.Checkpoints == null)
            {
                document.Checkpoints = new List<LightweightCheckpointRecord>();
            }
            for (int index = 0; index < document.Checkpoints.Count; index++)
            {
                LightweightCheckpointRecord checkpoint = document.Checkpoints[index];
                if (checkpoint == null)
                {
                    continue;
                }
                if (checkpoint.IdentityBindingsVersion !=
                        LightweightIdentityBindingSchema.IdentityBindingsVersion ||
                    checkpoint.IdentityBindings == null ||
                    checkpoint.IdentityCandidates == null)
                {
                    LightweightIdentityBindingSchema.InitializeLegacyUnboundCheckpoint(
                        checkpoint);
                }
            }

            document.NamespaceOwnerBindings =
                LightweightNamespaceOwnerSchema.BuildLegacyUnboundBindingsForPopulatedNamespaces(
                    document.Events,
                    document.CustomMutations);
            document.CoverageModelVersion = LightweightCoverageSchema.CoverageModelVersion;
            if (document.CoverageCapabilitySets == null)
            {
                document.CoverageCapabilitySets =
                    new List<LightweightCoverageCapabilitySetRecord>();
            }
            if (document.CoverageTransitions == null)
            {
                document.CoverageTransitions =
                    new List<LightweightCoverageTransitionRecord>();
            }
            if (document.HistoricalBaselineAssertions == null)
            {
                document.HistoricalBaselineAssertions =
                    new List<LightweightHistoricalBaselineAssertionRecord>();
            }

            if (document.MigrationProvenance == null)
            {
                throw new FormatException(
                    "The migrated IMDC generation is missing provenance.");
            }
            document.MigrationProvenance.SourceLastIssuedSequence =
                document.LastIssuedSequence;
            document.MigrationProvenance.ConversionId =
                LightweightMigrationProvenanceSchema.BuildConversionId(
                    document.MigrationProvenance,
                    document.RelativeSavePath);
        }

        private static string DecodeUtf8(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream(bytes, false))
            using (StreamReader reader = new StreamReader(
                stream,
                Encoding.UTF8,
                true,
                4096,
                false))
            {
                return reader.ReadToEnd();
            }
        }

        private static string ComputeSha256Hex(byte[] bytes)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                for (int index = 0; index < hash.Length; index++)
                {
                    builder.Append(hash[index].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
