using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace IMDataCore
{
    /// <summary>
    /// Sidecar-v6 coverage/capability contract for findings #61-#65. Capability
    /// descriptors are immutable non-rewinding provenance. Coverage transitions
    /// are rewindable branch state and consume the shared durable sequence space.
    /// The live v5/v2 runtime does not publish this schema yet.
    /// </summary>
    internal static class LightweightCoverageSchema
    {
        internal const int CoverageModelVersion = 1;

        internal const string ScopeBackend = "backend";
        internal const string ScopeBuiltIn = "builtin";
        internal const string ScopeNamespace = "namespace";
        internal const string BackendScopeIdentifier = "backend";
        internal const string BuiltInScopeIdentifier = "builtin";

        internal const string StateActive = "active";
        internal const string StateGap = "gap";

        internal const string OriginCareerStart = "CareerStart";
        internal const string OriginLateAdoption = "LateAdoption";
        internal const string OriginLegacyResume = "LegacyResume";
        internal const string OriginBuiltInActivation = "BuiltInActivation";
        internal const string OriginNamespaceDeclaration = "NamespaceDeclaration";
        internal const string OriginNamespaceProcessGap = "NamespaceProcessGap";
        internal const string OriginNamespaceExplicitGap = "NamespaceExplicitGap";

        internal const string CapabilitySetIdPrefix = "capset-v1:";
        internal const string AnchorCheckpointKeyPrefix = "checkpoint-v1:";
        internal const int MaximumCapabilityTokenLength = 64;
        internal const int MaximumReasonLength = 128;

        internal static LightweightCoverageCapabilitySetRecord
            CreateBuiltInCapabilitySet(
                IList<LightweightCoverageCapabilityRevisionRecord> capabilities)
        {
            return CreateCapabilitySet(
                ScopeBuiltIn,
                BuiltInScopeIdentifier,
                string.Empty,
                capabilities);
        }

        internal static LightweightCoverageCapabilitySetRecord
            CreateNamespaceCapabilitySet(
                string namespaceIdentifier,
                string stableOwnerId,
                IList<LightweightCoverageCapabilityRevisionRecord> capabilities)
        {
            return CreateCapabilitySet(
                ScopeNamespace,
                namespaceIdentifier,
                stableOwnerId,
                capabilities);
        }

        private static LightweightCoverageCapabilitySetRecord CreateCapabilitySet(
            string scopeKind,
            string scopeIdentifier,
            string ownerStableId,
            IList<LightweightCoverageCapabilityRevisionRecord> capabilities)
        {
            List<LightweightCoverageCapabilityRevisionRecord> canonical =
                CloneAndSortCapabilities(capabilities);
            LightweightCoverageCapabilitySetRecord record =
                new LightweightCoverageCapabilitySetRecord
                {
                    ScopeKind = scopeKind ?? string.Empty,
                    ScopeIdentifier = scopeIdentifier ?? string.Empty,
                    OwnerStableId = ownerStableId ?? string.Empty,
                    Capabilities = canonical
                };
            record.CapabilitySetId = BuildCapabilitySetId(record);
            ValidateCapabilitySet(record);
            return record;
        }

        internal static string BuildCapabilitySetId(
            LightweightCoverageCapabilitySetRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            List<LightweightCoverageCapabilityRevisionRecord> canonical =
                CloneAndSortCapabilities(record.Capabilities);
            StringBuilder builder = new StringBuilder(512);
            AppendCanonicalField(builder, record.ScopeKind ?? string.Empty);
            AppendCanonicalField(builder, record.ScopeIdentifier ?? string.Empty);
            AppendCanonicalField(builder, record.OwnerStableId ?? string.Empty);
            for (int index = 0; index < canonical.Count; index++)
            {
                AppendCanonicalField(builder, canonical[index].Token ?? string.Empty);
                AppendCanonicalField(
                    builder,
                    canonical[index].Revision.ToString(
                        CultureInfo.InvariantCulture));
            }

            byte[] payload = Encoding.UTF8.GetBytes(builder.ToString());
            using (SHA256 sha256 = SHA256.Create())
            {
                return CapabilitySetIdPrefix +
                    ToLowerHex(sha256.ComputeHash(payload));
            }
        }

        internal static string BuildAnchorCheckpointKey(
            LightweightCheckpointRecord checkpoint)
        {
            if (checkpoint == null)
            {
                throw new ArgumentNullException("checkpoint");
            }
            if (!VanillaSavedDataFingerprint.IsValid(
                    checkpoint.ContentFingerprint))
            {
                throw new FormatException(
                    "A coverage anchor checkpoint has an invalid content fingerprint.");
            }

            StringBuilder builder = new StringBuilder(384);
            AppendCanonicalField(
                builder,
                VanillaSaveStamp.NormalizeRelativePath(
                    checkpoint.RelativeSavePath));
            AppendCanonicalField(builder, checkpoint.LastSave ?? string.Empty);
            AppendCanonicalField(
                builder,
                checkpoint.PlaytimeSeconds.ToString(
                    CultureInfo.InvariantCulture));
            AppendCanonicalField(builder, checkpoint.GameDateTime ?? string.Empty);
            AppendCanonicalField(
                builder,
                checkpoint.ContentFingerprint ?? string.Empty);

            byte[] payload = Encoding.UTF8.GetBytes(builder.ToString());
            using (SHA256 sha256 = SHA256.Create())
            {
                return AnchorCheckpointKeyPrefix +
                    ToLowerHex(sha256.ComputeHash(payload));
            }
        }

        internal static void ValidateDocumentForV6(
            LightweightSidecarDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }
            if (document.CoverageModelVersion != CoverageModelVersion)
            {
                throw new FormatException(
                    "A sidecar-v6 document has an unsupported coverage model version.");
            }
            if (document.CoverageCapabilitySets == null ||
                document.CoverageTransitions == null)
            {
                throw new FormatException(
                    "A sidecar-v6 document is missing required coverage collections.");
            }

            Dictionary<string, LightweightCoverageCapabilitySetRecord>
                descriptorsById =
                    new Dictionary<string, LightweightCoverageCapabilitySetRecord>(
                        StringComparer.Ordinal);
            for (int index = 0;
                index < document.CoverageCapabilitySets.Count;
                index++)
            {
                LightweightCoverageCapabilitySetRecord descriptor =
                    document.CoverageCapabilitySets[index];
                ValidateCapabilitySet(descriptor);
                if (descriptorsById.ContainsKey(descriptor.CapabilitySetId))
                {
                    throw new FormatException(
                        "A sidecar-v6 document contains a duplicate coverage capability-set ID.");
                }
                descriptorsById.Add(descriptor.CapabilitySetId, descriptor);
            }

            Dictionary<string, LightweightNamespaceOwnerBindingRecord>
                latestOwners = BuildLatestNamespaceOwners(
                    document.NamespaceOwnerBindings);
            foreach (KeyValuePair<string, LightweightCoverageCapabilitySetRecord>
                pair in descriptorsById)
            {
                LightweightCoverageCapabilitySetRecord descriptor = pair.Value;
                if (string.Equals(
                        descriptor.ScopeKind,
                        ScopeNamespace,
                        StringComparison.Ordinal))
                {
                    LightweightNamespaceOwnerBindingRecord owner;
                    if (!latestOwners.TryGetValue(
                            descriptor.ScopeIdentifier,
                            out owner) ||
                        owner == null ||
                        !owner.OwnershipKnown ||
                        !string.Equals(
                            owner.StableOwnerId,
                            descriptor.OwnerStableId,
                            StringComparison.Ordinal))
                    {
                        throw new FormatException(
                            "A namespace coverage descriptor is not bound to the current durable namespace owner lineage.");
                    }
                }
            }

            Dictionary<string, LightweightCheckpointRecord> anchors =
                BuildUniqueCheckpointAnchorMap(document.Checkpoints);
            long previousTransitionSequence = 0L;
            int backendStartCount = 0;
            for (int index = 0;
                index < document.CoverageTransitions.Count;
                index++)
            {
                LightweightCoverageTransitionRecord transition =
                    document.CoverageTransitions[index];
                ValidateTransition(
                    transition,
                    descriptorsById,
                    latestOwners,
                    anchors);
                if (transition.Sequence <= previousTransitionSequence)
                {
                    throw new FormatException(
                        "Coverage transitions must be stored in strictly increasing sequence order.");
                }
                previousTransitionSequence = transition.Sequence;
                if (string.Equals(
                        transition.ScopeKind,
                        ScopeBackend,
                        StringComparison.Ordinal))
                {
                    backendStartCount++;
                    if (backendStartCount > 1)
                    {
                        throw new FormatException(
                            "A selected branch cannot contain more than one backend coverage origin.");
                    }
                }
            }
        }

        internal static void ValidateCapabilitySet(
            LightweightCoverageCapabilitySetRecord record)
        {
            if (record == null)
            {
                throw new FormatException(
                    "A coverage capability-set record is null.");
            }
            if (record.Capabilities == null || record.Capabilities.Count == 0)
            {
                throw new FormatException(
                    "A coverage capability set must contain at least one semantic capability.");
            }

            bool isBuiltIn = string.Equals(
                record.ScopeKind,
                ScopeBuiltIn,
                StringComparison.Ordinal);
            bool isNamespace = string.Equals(
                record.ScopeKind,
                ScopeNamespace,
                StringComparison.Ordinal);
            if (!isBuiltIn && !isNamespace)
            {
                throw new FormatException(
                    "A coverage capability set has an invalid scope kind.");
            }
            if (isBuiltIn)
            {
                if (!string.Equals(
                        record.ScopeIdentifier,
                        BuiltInScopeIdentifier,
                        StringComparison.Ordinal) ||
                    !string.IsNullOrEmpty(record.OwnerStableId))
                {
                    throw new FormatException(
                        "A built-in coverage capability set has invalid scope ownership fields.");
                }
            }
            else
            {
                ValidateNamespaceIdentifier(record.ScopeIdentifier);
                if (string.IsNullOrWhiteSpace(record.OwnerStableId) ||
                    record.OwnerStableId.Length >
                        LightweightNamespaceOwnerSchema.MaximumStableOwnerIdLength)
                {
                    throw new FormatException(
                        "A namespace coverage capability set is missing a stable owner lineage.");
                }
            }

            string previousToken = null;
            for (int index = 0; index < record.Capabilities.Count; index++)
            {
                LightweightCoverageCapabilityRevisionRecord capability =
                    record.Capabilities[index];
                ValidateCapabilityRevision(capability);
                if (previousToken != null &&
                    StringComparer.Ordinal.Compare(
                        previousToken,
                        capability.Token) >= 0)
                {
                    throw new FormatException(
                        "Coverage capability pairs must be unique and canonically sorted by token.");
                }
                previousToken = capability.Token;
            }

            string expectedId = BuildCapabilitySetId(record);
            if (!string.Equals(
                    record.CapabilitySetId,
                    expectedId,
                    StringComparison.Ordinal))
            {
                throw new FormatException(
                    "A coverage capability-set ID does not match its canonical semantic descriptor.");
            }
        }

        internal static void ValidateTransition(
            LightweightCoverageTransitionRecord transition,
            IDictionary<string, LightweightCoverageCapabilitySetRecord>
                descriptorsById,
            IDictionary<string, LightweightNamespaceOwnerBindingRecord>
                latestOwners,
            IDictionary<string, LightweightCheckpointRecord> anchors)
        {
            if (transition == null)
            {
                throw new FormatException("A coverage transition is null.");
            }
            if (transition.Sequence <= 0L)
            {
                throw new FormatException(
                    "A coverage transition has an invalid shared sequence.");
            }
            DateTime parsedGameDate;
            if (!DateTime.TryParseExact(
                    transition.GameDateTime ?? string.Empty,
                    CoreConstants.RoundTripDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out parsedGameDate))
            {
                throw new FormatException(
                    "A coverage transition has an invalid GameDateTime.");
            }
            ValidateReasonToken(transition.Reason);

            bool backend = string.Equals(
                transition.ScopeKind,
                ScopeBackend,
                StringComparison.Ordinal);
            bool builtIn = string.Equals(
                transition.ScopeKind,
                ScopeBuiltIn,
                StringComparison.Ordinal);
            bool namespaced = string.Equals(
                transition.ScopeKind,
                ScopeNamespace,
                StringComparison.Ordinal);
            if (!backend && !builtIn && !namespaced)
            {
                throw new FormatException(
                    "A coverage transition has an invalid scope kind.");
            }

            if (backend)
            {
                if (!string.Equals(
                        transition.ScopeIdentifier,
                        BackendScopeIdentifier,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        transition.State,
                        StateActive,
                        StringComparison.Ordinal) ||
                    !string.IsNullOrEmpty(transition.CapabilitySetId) ||
                    (!string.Equals(
                            transition.Origin,
                            OriginCareerStart,
                            StringComparison.Ordinal) &&
                        !string.Equals(
                            transition.Origin,
                            OriginLateAdoption,
                            StringComparison.Ordinal) &&
                        !string.Equals(
                            transition.Origin,
                            OriginLegacyResume,
                            StringComparison.Ordinal)))
                {
                    throw new FormatException(
                        "A backend coverage transition has invalid state or origin fields.");
                }
            }
            else if (builtIn)
            {
                if (!string.Equals(
                        transition.ScopeIdentifier,
                        BuiltInScopeIdentifier,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        transition.State,
                        StateActive,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        transition.Origin,
                        OriginBuiltInActivation,
                        StringComparison.Ordinal))
                {
                    throw new FormatException(
                        "A built-in coverage transition has invalid state or origin fields.");
                }
                RequireMatchingDescriptor(
                    transition,
                    ScopeBuiltIn,
                    BuiltInScopeIdentifier,
                    descriptorsById);
            }
            else
            {
                ValidateNamespaceIdentifier(transition.ScopeIdentifier);
                LightweightNamespaceOwnerBindingRecord owner;
                if (latestOwners == null ||
                    !latestOwners.TryGetValue(
                        transition.ScopeIdentifier,
                        out owner) ||
                    owner == null ||
                    !owner.OwnershipKnown)
                {
                    throw new FormatException(
                        "A namespace coverage transition has no authenticated durable owner lineage.");
                }

                if (string.Equals(
                        transition.State,
                        StateActive,
                        StringComparison.Ordinal))
                {
                    if (!string.Equals(
                            transition.Origin,
                            OriginNamespaceDeclaration,
                            StringComparison.Ordinal))
                    {
                        throw new FormatException(
                            "An active namespace coverage transition must come from an explicit capability declaration.");
                    }
                    LightweightCoverageCapabilitySetRecord descriptor =
                        RequireMatchingDescriptor(
                            transition,
                            ScopeNamespace,
                            transition.ScopeIdentifier,
                            descriptorsById);
                    if (!string.Equals(
                            descriptor.OwnerStableId,
                            owner.StableOwnerId,
                            StringComparison.Ordinal))
                    {
                        throw new FormatException(
                            "A namespace coverage transition references a descriptor for a different durable owner lineage.");
                    }
                }
                else if (string.Equals(
                    transition.State,
                    StateGap,
                    StringComparison.Ordinal))
                {
                    if (!string.IsNullOrEmpty(transition.CapabilitySetId) ||
                        (!string.Equals(
                                transition.Origin,
                                OriginNamespaceProcessGap,
                                StringComparison.Ordinal) &&
                            !string.Equals(
                                transition.Origin,
                                OriginNamespaceExplicitGap,
                                StringComparison.Ordinal)))
                    {
                        throw new FormatException(
                            "A namespace coverage gap has invalid descriptor or origin fields.");
                    }
                }
                else
                {
                    throw new FormatException(
                        "A namespace coverage transition has an invalid state.");
                }
            }

            bool requiresAnchor = string.Equals(
                    transition.Origin,
                    OriginLateAdoption,
                    StringComparison.Ordinal) ||
                string.Equals(
                    transition.Origin,
                    OriginLegacyResume,
                    StringComparison.Ordinal) ||
                string.Equals(
                    transition.Origin,
                    OriginNamespaceProcessGap,
                    StringComparison.Ordinal);
            if (requiresAnchor &&
                string.IsNullOrEmpty(transition.AnchorCheckpointKey))
            {
                throw new FormatException(
                    "A load-origin coverage transition is missing its exact checkpoint anchor.");
            }

            if (backend &&
                string.Equals(transition.Origin, OriginCareerStart, StringComparison.Ordinal) &&
                !string.IsNullOrEmpty(transition.AnchorCheckpointKey))
            {
                throw new FormatException(
                    "CareerStart coverage must not carry a loaded-save checkpoint anchor.");
            }

            if (!string.IsNullOrEmpty(transition.AnchorCheckpointKey))
            {
                LightweightCheckpointRecord checkpoint;
                if (anchors == null ||
                    !anchors.TryGetValue(
                        transition.AnchorCheckpointKey,
                        out checkpoint) ||
                    checkpoint == null)
                {
                    throw new FormatException(
                        "A coverage transition checkpoint anchor does not resolve to exactly one accepted checkpoint.");
                }
                if (checkpoint.Sequence >= transition.Sequence)
                {
                    throw new FormatException(
                        "A coverage transition must be ordered after its exact checkpoint anchor.");
                }
            }
            else if (string.Equals(
                transition.Origin,
                OriginCareerStart,
                StringComparison.Ordinal) == false &&
                backend)
            {
                throw new FormatException(
                    "A non-career-start backend coverage origin requires an exact checkpoint anchor.");
            }
        }

        private static LightweightCoverageCapabilitySetRecord
            RequireMatchingDescriptor(
                LightweightCoverageTransitionRecord transition,
                string expectedScopeKind,
                string expectedScopeIdentifier,
                IDictionary<string, LightweightCoverageCapabilitySetRecord>
                    descriptorsById)
        {
            if (string.IsNullOrEmpty(transition.CapabilitySetId))
            {
                throw new FormatException(
                    "An active coverage transition is missing its capability-set ID.");
            }
            LightweightCoverageCapabilitySetRecord descriptor;
            if (descriptorsById == null ||
                !descriptorsById.TryGetValue(
                    transition.CapabilitySetId,
                    out descriptor) ||
                descriptor == null ||
                !string.Equals(
                    descriptor.ScopeKind,
                    expectedScopeKind,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    descriptor.ScopeIdentifier,
                    expectedScopeIdentifier,
                    StringComparison.Ordinal))
            {
                throw new FormatException(
                    "A coverage transition references a missing or incompatible capability descriptor.");
            }
            return descriptor;
        }

        private static Dictionary<string, LightweightCheckpointRecord>
            BuildUniqueCheckpointAnchorMap(
                IList<LightweightCheckpointRecord> checkpoints)
        {
            Dictionary<string, LightweightCheckpointRecord> anchors =
                new Dictionary<string, LightweightCheckpointRecord>(
                    StringComparer.Ordinal);
            HashSet<string> duplicates =
                new HashSet<string>(StringComparer.Ordinal);
            if (checkpoints != null)
            {
                for (int index = 0; index < checkpoints.Count; index++)
                {
                    LightweightCheckpointRecord checkpoint = checkpoints[index];
                    if (checkpoint == null)
                    {
                        continue;
                    }
                    string key = BuildAnchorCheckpointKey(checkpoint);
                    if (anchors.ContainsKey(key))
                    {
                        duplicates.Add(key);
                    }
                    else
                    {
                        anchors.Add(key, checkpoint);
                    }
                }
            }
            foreach (string duplicate in duplicates)
            {
                anchors.Remove(duplicate);
            }
            return anchors;
        }

        private static Dictionary<string, LightweightNamespaceOwnerBindingRecord>
            BuildLatestNamespaceOwners(
                IList<LightweightNamespaceOwnerBindingRecord> bindings)
        {
            Dictionary<string, LightweightNamespaceOwnerBindingRecord> latest =
                new Dictionary<string, LightweightNamespaceOwnerBindingRecord>(
                    StringComparer.Ordinal);
            if (bindings == null)
            {
                return latest;
            }
            for (int index = 0; index < bindings.Count; index++)
            {
                LightweightNamespaceOwnerBindingRecord binding = bindings[index];
                if (binding == null)
                {
                    continue;
                }
                LightweightNamespaceOwnerBindingRecord current;
                if (!latest.TryGetValue(
                        binding.NamespaceIdentifier,
                        out current) ||
                    current == null ||
                    binding.BindingRevision > current.BindingRevision)
                {
                    latest[binding.NamespaceIdentifier] = binding;
                }
            }
            return latest;
        }

        private static List<LightweightCoverageCapabilityRevisionRecord>
            CloneAndSortCapabilities(
                IList<LightweightCoverageCapabilityRevisionRecord> capabilities)
        {
            if (capabilities == null)
            {
                throw new FormatException(
                    "A coverage capability list is missing.");
            }
            List<LightweightCoverageCapabilityRevisionRecord> result =
                new List<LightweightCoverageCapabilityRevisionRecord>(
                    capabilities.Count);
            for (int index = 0; index < capabilities.Count; index++)
            {
                LightweightCoverageCapabilityRevisionRecord source =
                    capabilities[index];
                if (source == null)
                {
                    throw new FormatException(
                        "A coverage capability revision is null.");
                }
                ValidateCapabilityRevision(source);
                result.Add(new LightweightCoverageCapabilityRevisionRecord
                {
                    Token = source.Token,
                    Revision = source.Revision
                });
            }
            result.Sort(CompareCapabilityRevision);
            return result;
        }

        private static int CompareCapabilityRevision(
            LightweightCoverageCapabilityRevisionRecord left,
            LightweightCoverageCapabilityRevisionRecord right)
        {
            int token = StringComparer.Ordinal.Compare(
                left != null ? left.Token : string.Empty,
                right != null ? right.Token : string.Empty);
            if (token != 0)
            {
                return token;
            }
            int leftRevision = left != null ? left.Revision : 0;
            int rightRevision = right != null ? right.Revision : 0;
            return leftRevision.CompareTo(rightRevision);
        }

        private static void ValidateCapabilityRevision(
            LightweightCoverageCapabilityRevisionRecord capability)
        {
            if (capability == null ||
                capability.Revision <= 0 ||
                string.IsNullOrEmpty(capability.Token) ||
                capability.Token.Length > MaximumCapabilityTokenLength ||
                !string.Equals(
                    capability.Token,
                    CoreTokenUtility.SanitizeToken(
                        capability.Token,
                        MaximumCapabilityTokenLength),
                    StringComparison.Ordinal))
            {
                throw new FormatException(
                    "A coverage capability revision has an invalid semantic token or revision.");
            }
        }

        private static void ValidateNamespaceIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value) ||
                value.Length < CoreConstants.NamespaceMinimumLength ||
                !string.Equals(
                    value,
                    CoreTokenUtility.SanitizeToken(
                        value,
                        CoreConstants.NamespaceMaximumLength),
                    StringComparison.Ordinal))
            {
                throw new FormatException(
                    "A coverage scope contains an invalid namespace identifier.");
            }
        }

        private static void ValidateReasonToken(string value)
        {
            if (string.IsNullOrEmpty(value) ||
                value.Length > MaximumReasonLength ||
                !string.Equals(
                    value,
                    CoreTokenUtility.SanitizeToken(value, MaximumReasonLength),
                    StringComparison.Ordinal))
            {
                throw new FormatException(
                    "A coverage transition has an invalid reason token.");
            }
        }

        private static void AppendCanonicalField(
            StringBuilder builder,
            string value)
        {
            string normalized = value ?? string.Empty;
            builder.Append(normalized.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(normalized);
            builder.Append(';');
        }

        private static string ToLowerHex(byte[] bytes)
        {
            if (bytes == null)
            {
                return string.Empty;
            }
            const string Hex = "0123456789abcdef";
            char[] output = new char[bytes.Length * 2];
            for (int index = 0; index < bytes.Length; index++)
            {
                byte value = bytes[index];
                output[index * 2] = Hex[value >> 4];
                output[(index * 2) + 1] = Hex[value & 0x0F];
            }
            return new string(output);
        }
    }

    [Serializable]
    internal sealed class LightweightCoverageCapabilityRevisionRecord
    {
        public string Token = string.Empty;
        public int Revision;
    }

    [Serializable]
    internal sealed class LightweightCoverageCapabilitySetRecord
    {
        public string CapabilitySetId = string.Empty;
        public string ScopeKind = string.Empty;
        public string ScopeIdentifier = string.Empty;
        public string OwnerStableId = string.Empty;
        public List<LightweightCoverageCapabilityRevisionRecord> Capabilities =
            new List<LightweightCoverageCapabilityRevisionRecord>();
    }

    [Serializable]
    internal sealed class LightweightCoverageTransitionRecord
    {
        public long Sequence;
        public string GameDateTime = string.Empty;
        public string ScopeKind = string.Empty;
        public string ScopeIdentifier = string.Empty;
        public string State = string.Empty;
        public string CapabilitySetId = string.Empty;
        public string Origin = string.Empty;
        public string Reason = string.Empty;
        public string AnchorCheckpointKey = string.Empty;
    }
}
