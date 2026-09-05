using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace IMDataCore
{
    /// <summary>
    /// Sidecar-v6 document-level namespace-owner provenance. Ownership is
    /// security/access provenance, not rewindable gameplay branch state.
    /// </summary>
    internal static class LightweightNamespaceOwnerSchema
    {
        internal const int BindingSchemaVersion = 1;
        internal const int UnknownOwnerSchemaVersion = 0;
        internal const string OriginNative = "native";
        internal const string OriginMigrationAdopted = "migration_adopted";
        internal const string OriginLegacyUnbound = "legacy_unbound";
        internal const string StableAssemblyLineagePrefix = "assembly-lineage-v1:";
        internal const string UnsignedPublicKeyToken = "unsigned";
        internal const string NeutralCulture = "neutral";
        internal const int MaximumStableOwnerIdLength = 512;
        internal const int MaximumAssemblyWitnessLength = 4096;

        internal static string BuildStableAssemblyOwnerId(Assembly assembly)
        {
            if (assembly == null)
            {
                return string.Empty;
            }

            AssemblyName name;
            try
            {
                name = assembly.GetName();
            }
            catch
            {
                return string.Empty;
            }

            if (name == null || string.IsNullOrWhiteSpace(name.Name))
            {
                return string.Empty;
            }

            string culture = NeutralCulture;
            try
            {
                if (name.CultureInfo != null &&
                    !string.IsNullOrWhiteSpace(name.CultureInfo.Name))
                {
                    culture = name.CultureInfo.Name;
                }
            }
            catch
            {
                culture = NeutralCulture;
            }

            string publicKeyToken = ResolvePublicKeyToken(name);
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}|culture={2}|pkt={3}",
                StableAssemblyLineagePrefix,
                name.Name,
                culture,
                publicKeyToken);
        }

        internal static LightweightNamespaceOwnerBindingRecord
            CreateLegacyUnboundBinding(string namespaceIdentifier)
        {
            LightweightNamespaceOwnerBindingRecord binding =
                new LightweightNamespaceOwnerBindingRecord
                {
                    NamespaceIdentifier = namespaceIdentifier ?? string.Empty,
                    BindingSchemaVersion = BindingSchemaVersion,
                    BindingRevision = 1,
                    OwnerSchemaVersion = UnknownOwnerSchemaVersion,
                    OwnershipKnown = false,
                    StableOwnerId = string.Empty,
                    Origin = OriginLegacyUnbound,
                    CurrentAssemblyWitness = string.Empty,
                    PreviousAssemblyWitnesses = new List<string>()
                };
            ValidateBinding(binding);
            return binding;
        }

        internal static LightweightNamespaceOwnerBindingRecord
            CreateNativeBinding(
                string namespaceIdentifier,
                string stableOwnerId,
                int ownerSchemaVersion,
                string currentAssemblyWitness)
        {
            LightweightNamespaceOwnerBindingRecord binding =
                new LightweightNamespaceOwnerBindingRecord
                {
                    NamespaceIdentifier = namespaceIdentifier ?? string.Empty,
                    BindingSchemaVersion = BindingSchemaVersion,
                    BindingRevision = 1,
                    OwnerSchemaVersion = ownerSchemaVersion,
                    OwnershipKnown = true,
                    StableOwnerId = stableOwnerId ?? string.Empty,
                    Origin = OriginNative,
                    CurrentAssemblyWitness = currentAssemblyWitness ?? string.Empty,
                    PreviousAssemblyWitnesses = new List<string>()
                };
            ValidateBinding(binding);
            return binding;
        }

        /// <summary>
        /// Normal restart/upgrade claim path. A known owner may rotate its strong
        /// assembly witness while keeping the stable lineage. A legacy-unbound
        /// record is intentionally not first-claimant adoptable through this path.
        /// </summary>
        internal static bool TryRefreshKnownOwnerBinding(
            LightweightNamespaceOwnerBindingRecord existing,
            string stableOwnerId,
            int ownerSchemaVersion,
            string currentAssemblyWitness,
            out LightweightNamespaceOwnerBindingRecord updated,
            out string errorMessage)
        {
            updated = null;
            errorMessage = string.Empty;
            try
            {
                ValidateBinding(existing);
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }

            if (!existing.OwnershipKnown)
            {
                errorMessage =
                    "The namespace owner is legacy-unbound and requires explicit migration adoption.";
                return false;
            }
            if (!string.Equals(
                    existing.StableOwnerId,
                    stableOwnerId ?? string.Empty,
                    StringComparison.Ordinal))
            {
                errorMessage =
                    "The namespace is durably bound to a different stable owner lineage.";
                return false;
            }

            string nextWitness = currentAssemblyWitness ?? string.Empty;
            if (existing.OwnerSchemaVersion == ownerSchemaVersion &&
                string.Equals(
                    existing.CurrentAssemblyWitness,
                    nextWitness,
                    StringComparison.Ordinal))
            {
                // Ordinary restart/re-registration of the same durable owner
                // is idempotent. A new immutable revision is necessary only
                // when owner-schema metadata or the strong witness changes.
                updated = CloneBinding(existing);
                return true;
            }

            updated = CloneBinding(existing);
            updated.BindingRevision = checked(existing.BindingRevision + 1);
            updated.OwnerSchemaVersion = ownerSchemaVersion;
            RotateAssemblyWitness(updated, nextWitness);
            try
            {
                ValidateBinding(updated);
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                updated = null;
                return false;
            }
        }

        /// <summary>
        /// Explicit one-time migration adoption path. The caller of this helper
        /// must already have obtained an external/user migration authorization;
        /// ordinary registration never sets this flag merely because it arrived
        /// first after restart.
        /// </summary>
        internal static bool TryAdoptLegacyUnboundBinding(
            LightweightNamespaceOwnerBindingRecord existing,
            string stableOwnerId,
            int ownerSchemaVersion,
            string currentAssemblyWitness,
            bool explicitAdoptionAuthorized,
            out LightweightNamespaceOwnerBindingRecord adopted,
            out string errorMessage)
        {
            adopted = null;
            errorMessage = string.Empty;
            try
            {
                ValidateBinding(existing);
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }

            if (existing.OwnershipKnown ||
                !string.Equals(
                    existing.Origin,
                    OriginLegacyUnbound,
                    StringComparison.Ordinal))
            {
                errorMessage =
                    "Only a legacy-unbound namespace owner can use migration adoption.";
                return false;
            }
            if (!explicitAdoptionAuthorized)
            {
                errorMessage =
                    "Legacy namespace ownership cannot be inferred from the current first claimant.";
                return false;
            }

            adopted = new LightweightNamespaceOwnerBindingRecord
            {
                NamespaceIdentifier = existing.NamespaceIdentifier,
                BindingSchemaVersion = BindingSchemaVersion,
                BindingRevision = checked(existing.BindingRevision + 1),
                OwnerSchemaVersion = ownerSchemaVersion,
                OwnershipKnown = true,
                StableOwnerId = stableOwnerId ?? string.Empty,
                Origin = OriginMigrationAdopted,
                CurrentAssemblyWitness = currentAssemblyWitness ?? string.Empty,
                PreviousAssemblyWitnesses = new List<string>()
            };

            try
            {
                ValidateBinding(adopted);
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                adopted = null;
                return false;
            }
        }

        internal static List<LightweightNamespaceOwnerBindingRecord>
            BuildLegacyUnboundBindingsForPopulatedNamespaces(
                IList<LightweightEventRecord> events,
                IList<LightweightCustomMutationRecord> customMutations)
        {
            SortedSet<string> namespaces =
                new SortedSet<string>(StringComparer.Ordinal);
            if (events != null)
            {
                for (int index = 0; index < events.Count; index++)
                {
                    LightweightEventRecord record = events[index];
                    if (record != null &&
                        !string.IsNullOrEmpty(record.NamespaceIdentifier))
                    {
                        namespaces.Add(record.NamespaceIdentifier);
                    }
                }
            }
            if (customMutations != null)
            {
                for (int index = 0; index < customMutations.Count; index++)
                {
                    LightweightCustomMutationRecord record =
                        customMutations[index];
                    if (record != null &&
                        !string.IsNullOrEmpty(record.NamespaceIdentifier))
                    {
                        namespaces.Add(record.NamespaceIdentifier);
                    }
                }
            }

            List<LightweightNamespaceOwnerBindingRecord> bindings =
                new List<LightweightNamespaceOwnerBindingRecord>(
                    namespaces.Count);
            foreach (string namespaceIdentifier in namespaces)
            {
                bindings.Add(CreateLegacyUnboundBinding(namespaceIdentifier));
            }
            return bindings;
        }

        internal static void ValidateDocumentForV6(
            LightweightSidecarDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }
            if (document.NamespaceOwnerBindings == null)
            {
                throw new FormatException(
                    "A sidecar-v6 document is missing NamespaceOwnerBindings.");
            }

            Dictionary<string, List<LightweightNamespaceOwnerBindingRecord>>
                revisionsByNamespace =
                    new Dictionary<string, List<LightweightNamespaceOwnerBindingRecord>>(
                        StringComparer.Ordinal);
            for (int index = 0;
                index < document.NamespaceOwnerBindings.Count;
                index++)
            {
                LightweightNamespaceOwnerBindingRecord binding =
                    document.NamespaceOwnerBindings[index];
                ValidateBinding(binding);
                List<LightweightNamespaceOwnerBindingRecord> revisions;
                if (!revisionsByNamespace.TryGetValue(
                        binding.NamespaceIdentifier,
                        out revisions))
                {
                    revisions = new List<LightweightNamespaceOwnerBindingRecord>();
                    revisionsByNamespace.Add(binding.NamespaceIdentifier, revisions);
                }
                revisions.Add(binding);
            }

            Dictionary<string, LightweightNamespaceOwnerBindingRecord> latestByNamespace =
                new Dictionary<string, LightweightNamespaceOwnerBindingRecord>(
                    StringComparer.Ordinal);
            foreach (KeyValuePair<string, List<LightweightNamespaceOwnerBindingRecord>> pair
                in revisionsByNamespace)
            {
                List<LightweightNamespaceOwnerBindingRecord> revisions = pair.Value;
                revisions.Sort(CompareBindingRevision);
                for (int index = 0; index < revisions.Count; index++)
                {
                    LightweightNamespaceOwnerBindingRecord current = revisions[index];
                    int expectedRevision = index + 1;
                    if (current.BindingRevision != expectedRevision)
                    {
                        throw new FormatException(
                            "A namespace-owner revision chain is missing or duplicating a revision.");
                    }
                    if (index > 0)
                    {
                        ValidateRevisionTransition(revisions[index - 1], current);
                    }
                }
                latestByNamespace.Add(
                    pair.Key,
                    revisions[revisions.Count - 1]);
            }

            RequireBindingsForPopulatedNamespaces(
                document.Events,
                document.CustomMutations,
                latestByNamespace);
        }

        internal static void ValidateBinding(
            LightweightNamespaceOwnerBindingRecord binding)
        {
            if (binding == null)
            {
                throw new FormatException(
                    "A namespace-owner binding is null.");
            }
            string sanitizedNamespace = CoreTokenUtility.SanitizeToken(
                binding.NamespaceIdentifier,
                CoreConstants.NamespaceMaximumLength);
            if (binding.NamespaceIdentifier == null ||
                binding.NamespaceIdentifier.Length <
                    CoreConstants.NamespaceMinimumLength ||
                !string.Equals(
                    binding.NamespaceIdentifier,
                    sanitizedNamespace,
                    StringComparison.Ordinal))
            {
                throw new FormatException(
                    "A namespace-owner binding has an invalid namespace token.");
            }
            if (binding.BindingSchemaVersion != BindingSchemaVersion)
            {
                throw new FormatException(
                    "A namespace-owner binding has an unsupported binding schema version.");
            }
            if (binding.BindingRevision <= 0)
            {
                throw new FormatException(
                    "A namespace-owner binding has an invalid revision.");
            }
            if (binding.PreviousAssemblyWitnesses == null)
            {
                throw new FormatException(
                    "A namespace-owner binding is missing previous assembly witnesses.");
            }

            if (!binding.OwnershipKnown)
            {
                if (!string.Equals(
                        binding.Origin,
                        OriginLegacyUnbound,
                        StringComparison.Ordinal) ||
                    binding.OwnerSchemaVersion != UnknownOwnerSchemaVersion ||
                    !string.IsNullOrEmpty(binding.StableOwnerId) ||
                    !string.IsNullOrEmpty(binding.CurrentAssemblyWitness) ||
                    binding.PreviousAssemblyWitnesses.Count != 0)
                {
                    throw new FormatException(
                        "A legacy-unbound namespace-owner binding contains invented owner certainty.");
                }
                return;
            }

            if (!string.Equals(binding.Origin, OriginNative, StringComparison.Ordinal) &&
                !string.Equals(
                    binding.Origin,
                    OriginMigrationAdopted,
                    StringComparison.Ordinal))
            {
                throw new FormatException(
                    "A known namespace-owner binding has an invalid origin.");
            }
            if (binding.OwnerSchemaVersion <= 0 ||
                string.IsNullOrWhiteSpace(binding.StableOwnerId) ||
                binding.StableOwnerId.Length > MaximumStableOwnerIdLength ||
                string.IsNullOrWhiteSpace(binding.CurrentAssemblyWitness) ||
                binding.CurrentAssemblyWitness.Length > MaximumAssemblyWitnessLength)
            {
                throw new FormatException(
                    "A known namespace-owner binding is missing required owner provenance.");
            }

            HashSet<string> witnesses =
                new HashSet<string>(StringComparer.Ordinal);
            witnesses.Add(binding.CurrentAssemblyWitness);
            for (int index = 0;
                index < binding.PreviousAssemblyWitnesses.Count;
                index++)
            {
                string witness = binding.PreviousAssemblyWitnesses[index];
                if (string.IsNullOrWhiteSpace(witness) ||
                    witness.Length > MaximumAssemblyWitnessLength ||
                    !witnesses.Add(witness))
                {
                    throw new FormatException(
                        "A namespace-owner binding contains an invalid or duplicate assembly witness.");
                }
            }
        }

        private static string ResolvePublicKeyToken(AssemblyName name)
        {
            try
            {
                byte[] token = name.GetPublicKeyToken();
                if (token == null || token.Length == 0)
                {
                    return UnsignedPublicKeyToken;
                }

                StringBuilder builder = new StringBuilder(token.Length * 2);
                for (int index = 0; index < token.Length; index++)
                {
                    builder.Append(token[index].ToString("x2", CultureInfo.InvariantCulture));
                }
                return builder.ToString();
            }
            catch
            {
                return UnsignedPublicKeyToken;
            }
        }

        private static void RotateAssemblyWitness(
            LightweightNamespaceOwnerBindingRecord binding,
            string currentAssemblyWitness)
        {
            string nextWitness = currentAssemblyWitness ?? string.Empty;
            if (string.Equals(
                    binding.CurrentAssemblyWitness,
                    nextWitness,
                    StringComparison.Ordinal))
            {
                return;
            }

            List<string> previous = new List<string>();
            if (!string.IsNullOrEmpty(binding.CurrentAssemblyWitness))
            {
                previous.Add(binding.CurrentAssemblyWitness);
            }
            for (int index = 0;
                index < binding.PreviousAssemblyWitnesses.Count;
                index++)
            {
                string witness = binding.PreviousAssemblyWitnesses[index];
                if (!string.Equals(witness, nextWitness, StringComparison.Ordinal) &&
                    !previous.Contains(witness))
                {
                    previous.Add(witness);
                }
            }

            binding.CurrentAssemblyWitness = nextWitness;
            binding.PreviousAssemblyWitnesses = previous;
        }

        private static LightweightNamespaceOwnerBindingRecord CloneBinding(
            LightweightNamespaceOwnerBindingRecord source)
        {
            return new LightweightNamespaceOwnerBindingRecord
            {
                NamespaceIdentifier = source.NamespaceIdentifier,
                BindingSchemaVersion = source.BindingSchemaVersion,
                BindingRevision = source.BindingRevision,
                OwnerSchemaVersion = source.OwnerSchemaVersion,
                OwnershipKnown = source.OwnershipKnown,
                StableOwnerId = source.StableOwnerId,
                Origin = source.Origin,
                CurrentAssemblyWitness = source.CurrentAssemblyWitness,
                PreviousAssemblyWitnesses =
                    new List<string>(source.PreviousAssemblyWitnesses)
            };
        }

        private static int CompareBindingRevision(
            LightweightNamespaceOwnerBindingRecord left,
            LightweightNamespaceOwnerBindingRecord right)
        {
            if (left == null && right == null)
            {
                return 0;
            }
            if (left == null)
            {
                return -1;
            }
            if (right == null)
            {
                return 1;
            }
            return left.BindingRevision.CompareTo(right.BindingRevision);
        }

        private static void ValidateRevisionTransition(
            LightweightNamespaceOwnerBindingRecord previous,
            LightweightNamespaceOwnerBindingRecord current)
        {
            if (previous == null || current == null ||
                !string.Equals(
                    previous.NamespaceIdentifier,
                    current.NamespaceIdentifier,
                    StringComparison.Ordinal))
            {
                throw new FormatException(
                    "A namespace-owner revision transition crosses namespaces.");
            }

            if (!previous.OwnershipKnown)
            {
                if (!current.OwnershipKnown ||
                    !string.Equals(
                        previous.Origin,
                        OriginLegacyUnbound,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        current.Origin,
                        OriginMigrationAdopted,
                        StringComparison.Ordinal))
                {
                    throw new FormatException(
                        "A legacy-unbound namespace can transition only through explicit migration adoption.");
                }
                return;
            }

            if (!current.OwnershipKnown ||
                !string.Equals(
                    previous.StableOwnerId,
                    current.StableOwnerId,
                    StringComparison.Ordinal))
            {
                throw new FormatException(
                    "A namespace-owner revision cannot change or erase the stable owner lineage.");
            }
            if (!current.PreviousAssemblyWitnesses.Contains(
                    previous.CurrentAssemblyWitness))
            {
                throw new FormatException(
                    "A namespace-owner revision must retain the prior strong assembly witness.");
            }
        }

        private static void RequireBindingsForPopulatedNamespaces(
            IList<LightweightEventRecord> events,
            IList<LightweightCustomMutationRecord> customMutations,
            IDictionary<string, LightweightNamespaceOwnerBindingRecord> bindings)
        {
            if (events != null)
            {
                for (int index = 0; index < events.Count; index++)
                {
                    LightweightEventRecord record = events[index];
                    if (record != null &&
                        !string.IsNullOrEmpty(record.NamespaceIdentifier) &&
                        !bindings.ContainsKey(record.NamespaceIdentifier))
                    {
                        throw new FormatException(
                            "A populated event namespace has no durable namespace-owner binding.");
                    }
                }
            }
            if (customMutations != null)
            {
                for (int index = 0; index < customMutations.Count; index++)
                {
                    LightweightCustomMutationRecord record =
                        customMutations[index];
                    if (record != null &&
                        !string.IsNullOrEmpty(record.NamespaceIdentifier) &&
                        !bindings.ContainsKey(record.NamespaceIdentifier))
                    {
                        throw new FormatException(
                            "A populated custom-data namespace has no durable namespace-owner binding.");
                    }
                }
            }
        }
    }

    [Serializable]
    internal sealed class LightweightNamespaceOwnerBindingRecord
    {
        public string NamespaceIdentifier = string.Empty;
        public int BindingSchemaVersion;
        public int BindingRevision;
        public int OwnerSchemaVersion;
        public bool OwnershipKnown;
        public string StableOwnerId = string.Empty;
        public string Origin = string.Empty;
        public string CurrentAssemblyWitness = string.Empty;
        public List<string> PreviousAssemblyWitnesses = new List<string>();
    }
}
