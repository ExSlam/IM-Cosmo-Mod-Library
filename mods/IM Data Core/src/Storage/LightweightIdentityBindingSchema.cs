using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace IMDataCore
{
    /// <summary>
    /// Physical checkpoint contract introduced by the planned v6 sidecar.
    /// The live 3.4.24 runtime still uses v5/v2; this schema is consumed by the
    /// bounded migration/foundation path until the journal-v3 cutover lands.
    /// </summary>
    internal static class LightweightIdentityBindingSchema
    {
        internal const int SidecarFormatVersion = 6;
        internal const int IdentityBindingsVersion = 1;

        internal const string OriginNative = "native";
        internal const string OriginMigrationAdopted = "migration_adopted";

        // These names mirror the authoritative SavedData collection fields in
        // the supplied Idol Manager Assembly-CSharp/decompiled source.
        internal const string ContainerBusinessActiveProposals =
            "business__ActiveProposalsData";
        internal const string ContainerRelationshipsCliques =
            "Relationships__Cliques";
        internal const string ContainerTasksTaskData =
            "tasks__TaskData";
        internal const string BullyingChildLocatorPrefix = "bullied_target:";
        internal const string MigrationAdoptionSalt = "imdc.identity.adoption.v1";
        internal const string CandidateSourceNativeBinding = "native_binding";
        internal const string CandidateSourceMigrationAdoption = "migration_adoption";
        internal const string CandidateSourceRoomWorkNamespace = "room_work_namespace";
        internal const long NoExactAliasSequence = -1L;

        internal static void InitializeLegacyUnboundCheckpoint(
            LightweightCheckpointRecord checkpoint)
        {
            if (checkpoint == null)
            {
                throw new ArgumentNullException("checkpoint");
            }

            checkpoint.IdentityBindingsVersion = IdentityBindingsVersion;
            checkpoint.IdentityBindingsComplete = false;
            checkpoint.IdentityBindings =
                new List<LightweightIdentityBindingRecord>();
            checkpoint.IdentityCandidates =
                new List<LightweightIdentityCandidateRecord>();
        }

        internal static void ValidateCheckpointForV6(
            LightweightCheckpointRecord checkpoint)
        {
            if (checkpoint == null)
            {
                throw new FormatException(
                    "A v6 identity-binding checkpoint is null.");
            }
            if (checkpoint.IdentityBindingsVersion != IdentityBindingsVersion)
            {
                throw new FormatException(
                    "The v6 checkpoint identity-binding schema version is unsupported.");
            }
            if (checkpoint.IdentityBindings == null)
            {
                throw new FormatException(
                    "The v6 checkpoint identity-binding collection is missing.");
            }
            if (checkpoint.IdentityCandidates == null)
            {
                throw new FormatException(
                    "The v6 checkpoint identity-candidate collection is missing.");
            }
            if (!checkpoint.IdentityBindingsComplete &&
                (checkpoint.IdentityBindings.Count > 0 || checkpoint.IdentityCandidates.Count > 0))
            {
                throw new FormatException(
                    "A legacy-unbound v6 checkpoint must not carry partial canonical bindings or candidate certainty.");
            }

            HashSet<string> canonicalKeys =
                new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> locatorKeys =
                new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < checkpoint.IdentityBindings.Count; index++)
            {
                LightweightIdentityBindingRecord binding =
                    checkpoint.IdentityBindings[index];
                ValidateBinding(binding, checkpoint.Sequence);

                string canonicalKey =
                    (binding.EntityKind ?? string.Empty) + "\n" +
                    (binding.EntityId ?? string.Empty);
                if (!canonicalKeys.Add(canonicalKey))
                {
                    throw new FormatException(
                        "The v6 checkpoint contains a duplicate canonical identity binding.");
                }

                string locatorKey = string.Concat(
                    binding.EntityKind ?? string.Empty,
                    "\n",
                    binding.ContainerKind ?? string.Empty,
                    "\n",
                    binding.ContainerOrdinal.ToString(CultureInfo.InvariantCulture),
                    "\n",
                    binding.ParentEntityId ?? string.Empty,
                    "\n",
                    binding.ChildLocator ?? string.Empty);
                if (!locatorKeys.Add(locatorKey))
                {
                    throw new FormatException(
                        "The v6 checkpoint contains duplicate identity bindings for one serialized locator.");
                }
            }

            for (int index = 0; index < checkpoint.IdentityBindings.Count; index++)
            {
                LightweightIdentityBindingRecord binding =
                    checkpoint.IdentityBindings[index];
                if (binding != null &&
                    string.Equals(
                        binding.EntityKind,
                        CoreConstants.EventEntityKindBullying,
                        StringComparison.Ordinal))
                {
                    string parentKey = string.Concat(
                        CoreConstants.EventEntityKindClique,
                        "\n",
                        binding.ParentEntityId ?? string.Empty);
                    if (!canonicalKeys.Contains(parentKey))
                    {
                        throw new FormatException(
                            "A bullying identity binding references a missing parent clique generation.");
                    }
                }
            }


            ValidateIdentityCandidates(checkpoint.IdentityCandidates, checkpoint.Sequence);

            HashSet<string> candidateLinks = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < checkpoint.IdentityCandidates.Count; index++)
            {
                LightweightIdentityCandidateRecord candidate = checkpoint.IdentityCandidates[index];
                candidateLinks.Add(string.Concat(
                    candidate.LegacyEntityKind, "\n", candidate.LegacyEntityId, "\n",
                    candidate.CanonicalEntityKind, "\n", candidate.CanonicalEntityId));
            }
            for (int bindingIndex = 0; bindingIndex < checkpoint.IdentityBindings.Count; bindingIndex++)
            {
                LightweightIdentityBindingRecord binding = checkpoint.IdentityBindings[bindingIndex];
                for (int candidateIndex = 0; candidateIndex < binding.LegacyCandidateKeys.Count; candidateIndex++)
                {
                    string requiredLink = string.Concat(
                        binding.EntityKind, "\n", binding.LegacyCandidateKeys[candidateIndex], "\n",
                        binding.EntityKind, "\n", binding.EntityId);
                    if (!candidateLinks.Contains(requiredLink))
                    {
                        throw new FormatException(
                            "A complete v6 identity binding advertises a legacy key that is missing from the checkpoint candidate multimap.");
                    }
                }
            }
        }

        internal static string CreateDeterministicAdoptedEntityId(
            string generationPrefix,
            VanillaSaveStamp stamp,
            long checkpointSequence,
            string entityKind,
            string containerKind,
            int containerOrdinal,
            string parentEntityKind,
            string parentEntityId,
            string childLocator,
            string validationFingerprint)
        {
            if (string.IsNullOrEmpty(generationPrefix) || stamp == null ||
                string.IsNullOrEmpty(entityKind) || string.IsNullOrEmpty(containerKind) ||
                containerOrdinal < 0 ||
                !VanillaSavedDataFingerprint.IsValid(validationFingerprint))
            {
                throw new ArgumentException(
                    "A deterministic adopted identity requires an exact checkpoint locator and witness.");
            }

            StringBuilder canonical = new StringBuilder();
            AppendLengthPrefixed(canonical, MigrationAdoptionSalt);
            AppendLengthPrefixed(canonical, IdentityBindingsVersion.ToString(CultureInfo.InvariantCulture));
            AppendLengthPrefixed(canonical, VanillaSaveStamp.NormalizeRelativePath(stamp.RelativeSavePath));
            AppendLengthPrefixed(canonical, stamp.LastSave ?? string.Empty);
            AppendLengthPrefixed(canonical, stamp.PlaytimeSeconds.ToString(CultureInfo.InvariantCulture));
            AppendLengthPrefixed(canonical, stamp.GameDateTime ?? string.Empty);
            AppendLengthPrefixed(canonical, stamp.ContentFingerprint ?? string.Empty);
            AppendLengthPrefixed(canonical, checkpointSequence.ToString(CultureInfo.InvariantCulture));
            AppendLengthPrefixed(canonical, entityKind);
            AppendLengthPrefixed(canonical, containerKind);
            AppendLengthPrefixed(canonical, containerOrdinal.ToString(CultureInfo.InvariantCulture));
            AppendLengthPrefixed(canonical, parentEntityKind ?? string.Empty);
            AppendLengthPrefixed(canonical, parentEntityId ?? string.Empty);
            AppendLengthPrefixed(canonical, childLocator ?? string.Empty);
            AppendLengthPrefixed(canonical, validationFingerprint);

            byte[] digest;
            using (SHA256 sha256 = SHA256.Create())
            {
                digest = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()));
            }
            StringBuilder hex = new StringBuilder(32);
            for (int index = 0; index < 16; index++)
            {
                hex.Append(digest[index].ToString("x2", CultureInfo.InvariantCulture));
            }
            return generationPrefix + hex.ToString();
        }

        private static void AppendLengthPrefixed(StringBuilder builder, string value)
        {
            string safe = value ?? string.Empty;
            builder.Append(safe.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(safe);
            builder.Append('|');
        }

        private static void ValidateIdentityCandidates(
            IReadOnlyList<LightweightIdentityCandidateRecord> records,
            long checkpointSequence)
        {
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < records.Count; index++)
            {
                LightweightIdentityCandidateRecord record = records[index];
                if (record == null ||
                    string.IsNullOrEmpty(record.LegacyEntityKind) ||
                    string.IsNullOrEmpty(record.LegacyEntityId) ||
                    string.IsNullOrEmpty(record.CanonicalEntityKind) ||
                    string.IsNullOrEmpty(record.CanonicalEntityId) ||
                    string.IsNullOrEmpty(record.SourceKind))
                {
                    throw new FormatException(
                        "The v6 checkpoint contains an invalid identity-candidate record.");
                }

                string key = string.Concat(
                    record.LegacyEntityKind, "\n", record.LegacyEntityId, "\n",
                    record.CanonicalEntityKind, "\n", record.CanonicalEntityId, "\n",
                    record.ExactAlias ? "1" : "0", "\n",
                    record.ExactSequenceStartInclusive.ToString(CultureInfo.InvariantCulture), "\n",
                    record.ExactSequenceEndInclusive.ToString(CultureInfo.InvariantCulture));
                if (!keys.Add(key))
                {
                    throw new FormatException(
                        "The v6 checkpoint contains a duplicate identity-candidate record.");
                }

                if (record.ExactAlias)
                {
                    if (record.ExactSequenceStartInclusive < 0L ||
                        record.ExactSequenceEndInclusive < record.ExactSequenceStartInclusive ||
                        record.ExactSequenceEndInclusive > checkpointSequence)
                    {
                        throw new FormatException(
                            "A proven exact identity alias must use one bounded sequence interval inside the checkpoint branch.");
                    }
                }
                else if (record.ExactSequenceStartInclusive != NoExactAliasSequence ||
                    record.ExactSequenceEndInclusive != NoExactAliasSequence)
                {
                    throw new FormatException(
                        "An unproven identity candidate must not carry an exact-alias sequence range.");
                }
            }
        }

        private static void ValidateBinding(
            LightweightIdentityBindingRecord binding,
            long checkpointSequence)
        {
            if (binding == null ||
                string.IsNullOrEmpty(binding.EntityKind) ||
                string.IsNullOrEmpty(binding.EntityId) ||
                string.IsNullOrEmpty(binding.ContainerKind) ||
                binding.ContainerOrdinal < 0 ||
                !VanillaSavedDataFingerprint.IsValid(
                    binding.ValidationFingerprint) ||
                binding.CoverageStartSequence < 0L ||
                binding.CoverageStartSequence > checkpointSequence ||
                !(string.Equals(
                      binding.Origin,
                      OriginNative,
                      StringComparison.Ordinal) ||
                  string.Equals(
                      binding.Origin,
                      OriginMigrationAdopted,
                      StringComparison.Ordinal)))
            {
                throw new FormatException(
                    "The v6 checkpoint contains an invalid identity binding.");
            }

            bool isContract = string.Equals(
                binding.EntityKind,
                CoreConstants.EventEntityKindContract,
                StringComparison.Ordinal);
            bool isClique = string.Equals(
                binding.EntityKind,
                CoreConstants.EventEntityKindClique,
                StringComparison.Ordinal);
            bool isBullying = string.Equals(
                binding.EntityKind,
                CoreConstants.EventEntityKindBullying,
                StringComparison.Ordinal);
            bool isTask = string.Equals(
                binding.EntityKind,
                CoreConstants.EventEntityKindTask,
                StringComparison.Ordinal);

            if (!(isContract || isClique || isBullying || isTask))
            {
                throw new FormatException(
                    "The v6 checkpoint contains an unsupported identity-binding family.");
            }

            string expectedContainer = isContract
                ? ContainerBusinessActiveProposals
                : isTask
                    ? ContainerTasksTaskData
                    : ContainerRelationshipsCliques;
            if (!string.Equals(
                    binding.ContainerKind,
                    expectedContainer,
                    StringComparison.Ordinal))
            {
                throw new FormatException(
                    "The v6 checkpoint identity binding uses the wrong serialized container.");
            }

            if (isTask &&
                (!string.IsNullOrEmpty(binding.ParentEntityKind) ||
                 !string.IsNullOrEmpty(binding.ParentEntityId) ||
                 !string.IsNullOrEmpty(binding.ChildLocator)))
            {
                throw new FormatException(
                    "A generated-task identity binding must use only its serialized task-row locator.");
            }

            if (isBullying)
            {
                int targetId;
                string numericTarget = !string.IsNullOrEmpty(binding.ChildLocator) &&
                    binding.ChildLocator.StartsWith(
                        BullyingChildLocatorPrefix,
                        StringComparison.Ordinal)
                            ? binding.ChildLocator.Substring(BullyingChildLocatorPrefix.Length)
                            : string.Empty;
                if (!string.Equals(
                        binding.ParentEntityKind,
                        CoreConstants.EventEntityKindClique,
                        StringComparison.Ordinal) ||
                    string.IsNullOrEmpty(binding.ParentEntityId) ||
                    !int.TryParse(
                        numericTarget,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out targetId) ||
                    targetId < CoreConstants.MinimumValidIdolIdentifier)
                {
                    throw new FormatException(
                        "A bullying identity binding must name its parent clique and valid bullied-target child locator.");
                }
            }

            if (binding.LegacyCandidateKeys == null)
            {
                throw new FormatException(
                    "The v6 checkpoint identity binding candidate-key collection is missing.");
            }

            HashSet<string> candidateKeys =
                new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < binding.LegacyCandidateKeys.Count; index++)
            {
                string candidateKey = binding.LegacyCandidateKeys[index];
                if (string.IsNullOrEmpty(candidateKey) ||
                    !candidateKeys.Add(candidateKey))
                {
                    throw new FormatException(
                        "The v6 checkpoint contains an invalid or duplicate legacy candidate key.");
                }
            }
        }
    }

    /// <summary>
    /// One exact-checkpoint historical-correlation binding. Locator and witness
    /// fields validate which vanilla SavedData row receives a generation; they
    /// are deliberately insufficient to reconstruct gameplay state on their own.
    /// </summary>
    [Serializable]
    internal sealed class LightweightIdentityBindingRecord
    {
        public string EntityKind = string.Empty;
        public string EntityId = string.Empty;
        public string ContainerKind = string.Empty;
        public int ContainerOrdinal = CoreConstants.InvalidIdValue;
        public string ParentEntityKind = string.Empty;
        public string ParentEntityId = string.Empty;
        public string ChildLocator = string.Empty;
        public string ValidationFingerprint = string.Empty;
        public string Origin = string.Empty;
        public long CoverageStartSequence;
        public List<string> LegacyCandidateKeys = new List<string>();
    }
    [Serializable]
    internal sealed class LightweightIdentityCandidateRecord
    {
        public string LegacyEntityKind = string.Empty;
        public string LegacyEntityId = string.Empty;
        public string CanonicalEntityKind = string.Empty;
        public string CanonicalEntityId = string.Empty;
        public bool ExactAlias;
        public long ExactSequenceStartInclusive =
            LightweightIdentityBindingSchema.NoExactAliasSequence;
        public long ExactSequenceEndInclusive =
            LightweightIdentityBindingSchema.NoExactAliasSequence;
        public string SourceKind = string.Empty;
    }

}
