using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace IMDataCore
{
    /// <summary>
    /// Public coverage/knownness facade for audit findings #61-#65.
    ///
    /// The API surface is intentionally available before the v6/v3 live cutover,
    /// but the current v5/v2 backend can only return Unknown. It must never turn
    /// missing durable coverage records into a Complete-empty claim.
    /// </summary>
    internal sealed partial class IMDataCoreController
    {
        private const string StructuredCoverageCutoverPendingMessage =
            "Durable structured coverage requires the live sidecar-v6/journal-v3 backend; " +
            "this runtime remains conservative and reports Unknown.";

        internal bool TryGetBackendCoverageOrigin(
            out IMDataCoreBackendCoverage coverage,
            out string errorMessage)
        {
            coverage = CreateUnknownBackendCoverage();
            errorMessage = string.Empty;

            lock (runtimeLock)
            {
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    return false;
                }

                if (!storageEngine.SupportsStructuredCoverageModel)
                {
                    return true;
                }

                LightweightStructuredCoverageState state;
                if (!storageEngine.TryCaptureStructuredCoverageState(out state))
                {
                    errorMessage =
                        "The structured coverage state is unavailable from this storage generation.";
                    return false;
                }
                return TryBuildBackendCoverage(
                    state,
                    out coverage,
                    out errorMessage);
            }
        }

        internal bool TryGetBuiltInHistoryCoverage(
            string capabilityToken,
            int minimumRevision,
            out IMDataCoreHistoryCoverage coverage,
            out string errorMessage)
        {
            coverage = null;
            errorMessage = string.Empty;
            string token;
            if (!TryValidateCoverageCapability(
                    capabilityToken,
                    minimumRevision,
                    out token,
                    out errorMessage))
            {
                return false;
            }

            lock (runtimeLock)
            {
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    return false;
                }

                coverage = CreateUnknownCoverage(
                    LightweightCoverageSchema.ScopeBuiltIn,
                    LightweightCoverageSchema.BuiltInScopeIdentifier,
                    token,
                    minimumRevision);
                if (!storageEngine.SupportsStructuredCoverageModel)
                {
                    return true;
                }

                LightweightStructuredCoverageState state;
                if (!storageEngine.TryCaptureStructuredCoverageState(out state))
                {
                    errorMessage =
                        "The structured coverage state is unavailable from this storage generation.";
                    return false;
                }
                return TryBuildHistoryCoverage(
                    state,
                    LightweightCoverageSchema.ScopeBuiltIn,
                    LightweightCoverageSchema.BuiltInScopeIdentifier,
                    string.Empty,
                    token,
                    minimumRevision,
                    out coverage,
                    out errorMessage);
            }
        }

        internal bool TryAssessBuiltInHistoryRange(
            string capabilityToken,
            int minimumRevision,
            IMDataCoreHistoryBoundary startBoundary,
            IMDataCoreHistoryBoundary endBoundary,
            out IMDataCoreCoverageAssessment assessment,
            out string errorMessage)
        {
            assessment = null;
            IMDataCoreHistoryCoverage coverage;
            if (!TryGetBuiltInHistoryCoverage(
                    capabilityToken,
                    minimumRevision,
                    out coverage,
                    out errorMessage))
            {
                return false;
            }
            return TryAssessCoverageRange(
                coverage,
                startBoundary,
                endBoundary,
                out assessment,
                out errorMessage);
        }

        /// <summary>
        /// Returns the authoritative structured coverage frontier for exact money
        /// transaction history (#62). This binds the money product to the semantic
        /// money_transaction capability rather than the legacy DateTime marker.
        /// </summary>
        internal bool TryGetMoneyHistoryCoverage(
            out IMDataCoreHistoryCoverage coverage,
            out string errorMessage)
        {
            return TryGetBuiltInHistoryCoverage(
                MoneyLedgerConstants.EventTypeTransaction,
                LightweightBuiltInCapabilityCatalog.CatalogRevision,
                out coverage,
                out errorMessage);
        }

        /// <summary>
        /// Assesses exact money-history completeness over a half-open shared-sequence
        /// range. Sequence/checkpoint ordering is authoritative; game DateTime is not.
        /// </summary>
        internal bool TryAssessMoneyHistoryRange(
            IMDataCoreHistoryBoundary startBoundary,
            IMDataCoreHistoryBoundary endBoundary,
            out IMDataCoreCoverageAssessment assessment,
            out string errorMessage)
        {
            return TryAssessBuiltInHistoryRange(
                MoneyLedgerConstants.EventTypeTransaction,
                LightweightBuiltInCapabilityCatalog.CatalogRevision,
                startBoundary,
                endBoundary,
                out assessment,
                out errorMessage);
        }

        internal bool TryDeclareNamespaceCapabilities(
            IMDataCoreSession session,
            Assembly callingAssembly,
            IList<IMDataCoreCapabilityRevision> capabilities,
            out string capabilitySetId,
            out string errorMessage)
        {
            capabilitySetId = string.Empty;
            errorMessage = string.Empty;
            lock (runtimeLock)
            {
                NamespaceSessionRegistration registration;
                if (!TryValidateSessionLocked(
                        session,
                        callingAssembly,
                        out registration,
                        out errorMessage))
                {
                    return false;
                }

                List<LightweightCoverageCapabilityRevisionRecord> canonical;
                if (!TryBuildCoverageCapabilityRecords(
                        capabilities,
                        out canonical,
                        out errorMessage))
                {
                    return false;
                }

                if (!storageEngine.SupportsStructuredCoverageModel)
                {
                    errorMessage = StructuredCoverageCutoverPendingMessage;
                    return false;
                }

                LightweightCoverageCapabilitySetRecord descriptor =
                    LightweightCoverageSchema.CreateNamespaceCapabilitySet(
                        registration.NamespaceIdentifier,
                        registration.StableOwnerId,
                        canonical);
                capabilitySetId = descriptor.CapabilitySetId;

                DateTime mutationGameDate;
                if (!TryResolvePublicMutationGameDateLocked(
                        out mutationGameDate,
                        out errorMessage))
                {
                    capabilitySetId = string.Empty;
                    return false;
                }

                if (!storageEngine.TryDeclareNamespaceCoverage(
                        NextCaptureSequenceLocked,
                        mutationGameDate,
                        descriptor,
                        out errorMessage))
                {
                    capabilitySetId = string.Empty;
                    return false;
                }
                return true;
            }
        }

        internal bool TryGetNamespaceHistoryCoverage(
            IMDataCoreSession session,
            Assembly callingAssembly,
            string capabilityToken,
            int minimumRevision,
            out IMDataCoreHistoryCoverage coverage,
            out string errorMessage)
        {
            coverage = null;
            errorMessage = string.Empty;
            string token;
            if (!TryValidateCoverageCapability(
                    capabilityToken,
                    minimumRevision,
                    out token,
                    out errorMessage))
            {
                return false;
            }

            lock (runtimeLock)
            {
                NamespaceSessionRegistration registration;
                if (!TryValidateSessionLocked(
                        session,
                        callingAssembly,
                        out registration,
                        out errorMessage))
                {
                    return false;
                }

                coverage = CreateUnknownCoverage(
                    LightweightCoverageSchema.ScopeNamespace,
                    registration.NamespaceIdentifier,
                    token,
                    minimumRevision);
                if (!storageEngine.SupportsStructuredCoverageModel)
                {
                    return true;
                }

                LightweightStructuredCoverageState state;
                if (!storageEngine.TryCaptureStructuredCoverageState(out state))
                {
                    errorMessage =
                        "The structured coverage state is unavailable from this storage generation.";
                    return false;
                }
                return TryBuildHistoryCoverage(
                    state,
                    LightweightCoverageSchema.ScopeNamespace,
                    registration.NamespaceIdentifier,
                    registration.StableOwnerId,
                    token,
                    minimumRevision,
                    out coverage,
                    out errorMessage);
            }
        }

        internal bool TryAssessNamespaceHistoryRange(
            IMDataCoreSession session,
            Assembly callingAssembly,
            string capabilityToken,
            int minimumRevision,
            IMDataCoreHistoryBoundary startBoundary,
            IMDataCoreHistoryBoundary endBoundary,
            out IMDataCoreCoverageAssessment assessment,
            out string errorMessage)
        {
            assessment = null;
            IMDataCoreHistoryCoverage coverage;
            if (!TryGetNamespaceHistoryCoverage(
                    session,
                    callingAssembly,
                    capabilityToken,
                    minimumRevision,
                    out coverage,
                    out errorMessage))
            {
                return false;
            }
            return TryAssessCoverageRange(
                coverage,
                startBoundary,
                endBoundary,
                out assessment,
                out errorMessage);
        }

        internal bool TryGetGroupTargetAudienceHistoricalBaseline(
            int groupId,
            out IMDataCoreHistoricalBaselineAssertion assertion,
            out string errorMessage)
        {
            assertion = null;
            errorMessage = string.Empty;
            if (groupId < 0)
            {
                errorMessage = "A group historical-baseline query requires a non-negative group ID.";
                return false;
            }

            lock (runtimeLock)
            {
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    return false;
                }
                if (!storageEngine.SupportsStructuredCoverageModel)
                {
                    return true;
                }

                LightweightStructuredCoverageState state;
                if (!storageEngine.TryCaptureStructuredCoverageState(out state))
                {
                    errorMessage =
                        "The structured coverage state is unavailable from this storage generation.";
                    return false;
                }

                string entityId = groupId.ToString(CultureInfo.InvariantCulture);
                for (int index = state.HistoricalBaselineAssertions.Count - 1;
                    index >= 0;
                    index--)
                {
                    LightweightHistoricalBaselineAssertionRecord record =
                        state.HistoricalBaselineAssertions[index];
                    if (record != null &&
                        string.Equals(
                            record.BaselineKind,
                            LightweightHistoricalBaselineSchema
                                .BaselineKindGroupTargetAudienceOrigin,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            record.EntityKind,
                            CoreConstants.EventEntityKindGroup,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            record.EntityId,
                            entityId,
                            StringComparison.Ordinal))
                    {
                        assertion =
                            LightweightHistoricalBaselineSchema.ToPublicAssertion(record);
                        return true;
                    }
                }
                return true;
            }
        }

        private static bool TryValidateCoverageCapability(
            string capabilityToken,
            int minimumRevision,
            out string sanitizedToken,
            out string errorMessage)
        {
            sanitizedToken = CoreTokenUtility.SanitizeToken(
                capabilityToken,
                LightweightCoverageSchema.MaximumCapabilityTokenLength);
            errorMessage = string.Empty;
            if (minimumRevision <= 0 ||
                string.IsNullOrEmpty(sanitizedToken) ||
                !string.Equals(
                    capabilityToken,
                    sanitizedToken,
                    StringComparison.Ordinal))
            {
                sanitizedToken = string.Empty;
                errorMessage =
                    "A coverage capability requires an exact-safe token and a positive revision.";
                return false;
            }
            return true;
        }

        private static bool TryBuildCoverageCapabilityRecords(
            IList<IMDataCoreCapabilityRevision> capabilities,
            out List<LightweightCoverageCapabilityRevisionRecord> records,
            out string errorMessage)
        {
            records = new List<LightweightCoverageCapabilityRevisionRecord>();
            errorMessage = string.Empty;
            if (capabilities == null || capabilities.Count == 0)
            {
                errorMessage = "At least one namespace capability is required.";
                return false;
            }

            HashSet<string> tokens = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < capabilities.Count; index++)
            {
                IMDataCoreCapabilityRevision capability = capabilities[index];
                string token;
                if (capability == null ||
                    !TryValidateCoverageCapability(
                        capability != null ? capability.Token : string.Empty,
                        capability != null ? capability.Revision : 0,
                        out token,
                        out errorMessage))
                {
                    return false;
                }
                if (!tokens.Add(token))
                {
                    errorMessage =
                        "A namespace capability declaration contains a duplicate token.";
                    return false;
                }
                records.Add(new LightweightCoverageCapabilityRevisionRecord
                {
                    Token = token,
                    Revision = capability.Revision
                });
            }
            records.Sort(delegate(
                LightweightCoverageCapabilityRevisionRecord left,
                LightweightCoverageCapabilityRevisionRecord right)
            {
                return StringComparer.Ordinal.Compare(left.Token, right.Token);
            });
            return true;
        }

        private static IMDataCoreBackendCoverage CreateUnknownBackendCoverage()
        {
            return new IMDataCoreBackendCoverage
            {
                Knownness = IMDataCoreHistoryKnownness.Unknown,
                Origin = IMDataCoreCoverageOrigin.Unknown,
                FirstBoundary = null
            };
        }

        private static IMDataCoreHistoryCoverage CreateUnknownCoverage(
            string scopeKind,
            string scopeIdentifier,
            string capabilityToken,
            int minimumRevision)
        {
            return new IMDataCoreHistoryCoverage
            {
                Knownness = IMDataCoreHistoryKnownness.Unknown,
                ScopeKind = scopeKind ?? string.Empty,
                ScopeIdentifier = scopeIdentifier ?? string.Empty,
                CapabilityToken = capabilityToken ?? string.Empty,
                MinimumRevision = minimumRevision,
                Intervals = new List<IMDataCoreCoverageInterval>(),
                SemanticStartBoundary = null
            };
        }

        private static bool TryBuildBackendCoverage(
            LightweightStructuredCoverageState state,
            out IMDataCoreBackendCoverage coverage,
            out string errorMessage)
        {
            coverage = CreateUnknownBackendCoverage();
            errorMessage = string.Empty;
            if (state == null || state.Transitions == null ||
                state.AnchorCheckpointSequences == null)
            {
                errorMessage = "The selected structured coverage state is missing.";
                return false;
            }

            LightweightCoverageTransitionRecord backendTransition = null;
            for (int index = 0; index < state.Transitions.Count; index++)
            {
                LightweightCoverageTransitionRecord candidate = state.Transitions[index];
                if (candidate == null ||
                    !string.Equals(
                        candidate.ScopeKind,
                        LightweightCoverageSchema.ScopeBackend,
                        StringComparison.Ordinal))
                {
                    continue;
                }
                if (backendTransition != null)
                {
                    errorMessage =
                        "The selected branch contains more than one backend coverage origin.";
                    return false;
                }
                backendTransition = candidate;
            }

            if (backendTransition == null)
            {
                return true;
            }

            IMDataCoreCoverageOrigin origin;
            if (string.Equals(
                    backendTransition.Origin,
                    LightweightCoverageSchema.OriginCareerStart,
                    StringComparison.Ordinal))
            {
                origin = IMDataCoreCoverageOrigin.CareerStart;
            }
            else if (string.Equals(
                backendTransition.Origin,
                LightweightCoverageSchema.OriginLateAdoption,
                StringComparison.Ordinal))
            {
                origin = IMDataCoreCoverageOrigin.LateAdoption;
            }
            else if (string.Equals(
                backendTransition.Origin,
                LightweightCoverageSchema.OriginLegacyResume,
                StringComparison.Ordinal))
            {
                origin = IMDataCoreCoverageOrigin.LegacyResume;
            }
            else
            {
                errorMessage =
                    "The selected backend coverage origin is unsupported.";
                return false;
            }

            IMDataCoreHistoryBoundary boundary;
            if (!TryCreateCoverageBoundary(
                    backendTransition,
                    state.AnchorCheckpointSequences,
                    out boundary,
                    out errorMessage))
            {
                return false;
            }
            coverage = new IMDataCoreBackendCoverage
            {
                Knownness = IMDataCoreHistoryKnownness.Complete,
                Origin = origin,
                FirstBoundary = boundary
            };
            return true;
        }

        private static bool TryBuildHistoryCoverage(
            LightweightStructuredCoverageState state,
            string scopeKind,
            string scopeIdentifier,
            string requiredOwnerStableId,
            string capabilityToken,
            int minimumRevision,
            out IMDataCoreHistoryCoverage coverage,
            out string errorMessage)
        {
            coverage = CreateUnknownCoverage(
                scopeKind,
                scopeIdentifier,
                capabilityToken,
                minimumRevision);
            errorMessage = string.Empty;
            if (state == null || state.CapabilitySets == null ||
                state.Transitions == null || state.AnchorCheckpointSequences == null)
            {
                errorMessage = "The selected structured coverage state is missing.";
                return false;
            }

            IMDataCoreBackendCoverage backendCoverage;
            if (!TryBuildBackendCoverage(
                    state,
                    out backendCoverage,
                    out errorMessage))
            {
                return false;
            }
            if (backendCoverage.Knownness != IMDataCoreHistoryKnownness.Complete ||
                backendCoverage.FirstBoundary == null)
            {
                return true;
            }

            Dictionary<string, LightweightCoverageCapabilitySetRecord> descriptors =
                new Dictionary<string, LightweightCoverageCapabilitySetRecord>(
                    StringComparer.Ordinal);
            bool sawScopeDescriptor = false;
            bool sawRequestedCapabilityDescriptor = false;
            for (int index = 0; index < state.CapabilitySets.Count; index++)
            {
                LightweightCoverageCapabilitySetRecord descriptor =
                    state.CapabilitySets[index];
                if (descriptor == null ||
                    !string.Equals(
                        descriptor.ScopeKind,
                        scopeKind,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        descriptor.ScopeIdentifier,
                        scopeIdentifier,
                        StringComparison.Ordinal))
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(requiredOwnerStableId) &&
                    !string.Equals(
                        descriptor.OwnerStableId,
                        requiredOwnerStableId,
                        StringComparison.Ordinal))
                {
                    continue;
                }
                sawScopeDescriptor = true;
                descriptors[descriptor.CapabilitySetId ?? string.Empty] = descriptor;
                if (DescriptorSupportsCapability(
                        descriptor,
                        capabilityToken,
                        minimumRevision))
                {
                    sawRequestedCapabilityDescriptor = true;
                }
            }

            List<LightweightCoverageTransitionRecord> scopeTransitions =
                new List<LightweightCoverageTransitionRecord>();
            for (int index = 0; index < state.Transitions.Count; index++)
            {
                LightweightCoverageTransitionRecord transition = state.Transitions[index];
                if (transition != null &&
                    string.Equals(
                        transition.ScopeKind,
                        scopeKind,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        transition.ScopeIdentifier,
                        scopeIdentifier,
                        StringComparison.Ordinal))
                {
                    scopeTransitions.Add(transition);
                }
            }
            scopeTransitions.Sort(CompareCoverageTransitionsBySequence);
            if (scopeTransitions.Count == 0)
            {
                return true;
            }

            bool anyActive = false;
            bool anyGap = false;
            for (int index = 0; index < scopeTransitions.Count; index++)
            {
                LightweightCoverageTransitionRecord transition =
                    scopeTransitions[index];
                IMDataCoreHistoryBoundary startBoundary;
                if (!TryCreateCoverageBoundary(
                        transition,
                        state.AnchorCheckpointSequences,
                        out startBoundary,
                        out errorMessage))
                {
                    return false;
                }

                IMDataCoreHistoryBoundary endBoundary = null;
                if (index + 1 < scopeTransitions.Count &&
                    !TryCreateCoverageBoundary(
                        scopeTransitions[index + 1],
                        state.AnchorCheckpointSequences,
                        out endBoundary,
                        out errorMessage))
                {
                    return false;
                }

                bool active = false;
                if (string.Equals(
                        transition.State,
                        LightweightCoverageSchema.StateActive,
                        StringComparison.Ordinal))
                {
                    LightweightCoverageCapabilitySetRecord descriptor;
                    if (!descriptors.TryGetValue(
                            transition.CapabilitySetId ?? string.Empty,
                            out descriptor) ||
                        descriptor == null)
                    {
                        errorMessage =
                            "A selected coverage transition references an unavailable capability descriptor.";
                        return false;
                    }
                    active = DescriptorSupportsCapability(
                        descriptor,
                        capabilityToken,
                        minimumRevision);
                }
                else if (!string.Equals(
                    transition.State,
                    LightweightCoverageSchema.StateGap,
                    StringComparison.Ordinal))
                {
                    errorMessage =
                        "A selected coverage transition has an unsupported state.";
                    return false;
                }

                IMDataCoreCoverageInterval interval =
                    new IMDataCoreCoverageInterval
                    {
                        StartBoundary = startBoundary,
                        EndBoundary = endBoundary,
                        CapabilitySetId = transition.CapabilitySetId ?? string.Empty,
                        State = active
                            ? IMDataCoreCoverageIntervalState.Active
                            : IMDataCoreCoverageIntervalState.Gap,
                        Origin = transition.Origin ?? string.Empty,
                        Reason = transition.Reason ?? string.Empty
                    };
                coverage.Intervals.Add(interval);
                anyActive |= active;
                anyGap |= !active;
            }

            if (!anyActive)
            {
                coverage.Knownness = sawScopeDescriptor &&
                        !sawRequestedCapabilityDescriptor
                    ? IMDataCoreHistoryKnownness.NotApplicable
                    : IMDataCoreHistoryKnownness.Unknown;
                return true;
            }

            IMDataCoreCoverageInterval firstInterval = coverage.Intervals[0];
            bool beginsWithBackend =
                firstInterval.State == IMDataCoreCoverageIntervalState.Active &&
                (firstInterval.StartBoundary.Sequence <=
                        backendCoverage.FirstBoundary.Sequence ||
                    BoundariesShareExactAnchor(
                        firstInterval.StartBoundary,
                        backendCoverage.FirstBoundary) ||
                    IsNativeCareerStartBootstrap(
                        firstInterval,
                        backendCoverage));
            coverage.Knownness = beginsWithBackend && !anyGap
                ? IMDataCoreHistoryKnownness.Complete
                : IMDataCoreHistoryKnownness.Partial;
            if (coverage.Knownness == IMDataCoreHistoryKnownness.Complete)
            {
                coverage.SemanticStartBoundary = backendCoverage.FirstBoundary;
            }
            return true;
        }

        private static bool IsNativeCareerStartBootstrap(
            IMDataCoreCoverageInterval firstInterval,
            IMDataCoreBackendCoverage backendCoverage)
        {
            if (firstInterval == null ||
                firstInterval.StartBoundary == null ||
                backendCoverage == null ||
                backendCoverage.FirstBoundary == null ||
                backendCoverage.Origin != IMDataCoreCoverageOrigin.CareerStart ||
                firstInterval.State != IMDataCoreCoverageIntervalState.Active ||
                !string.Equals(
                    firstInterval.Origin,
                    LightweightCoverageSchema.OriginBuiltInActivation,
                    StringComparison.Ordinal) ||
                firstInterval.StartBoundary.HasAnchorCheckpoint ||
                backendCoverage.FirstBoundary.HasAnchorCheckpoint ||
                backendCoverage.FirstBoundary.Sequence == long.MaxValue)
            {
                return false;
            }

            // Native bootstrap emits the backend CareerStart origin and current
            // built-in descriptor back-to-back before ordinary capture can run.
            // Consecutive shared sequences prove that no event/custom/coverage
            // mutation can exist in the semantic interval between them.
            return firstInterval.StartBoundary.Sequence ==
                    backendCoverage.FirstBoundary.Sequence + 1L &&
                firstInterval.StartBoundary.GameDateTime ==
                    backendCoverage.FirstBoundary.GameDateTime;
        }

        private static bool DescriptorSupportsCapability(
            LightweightCoverageCapabilitySetRecord descriptor,
            string capabilityToken,
            int minimumRevision)
        {
            if (descriptor == null || descriptor.Capabilities == null)
            {
                return false;
            }
            for (int index = 0; index < descriptor.Capabilities.Count; index++)
            {
                LightweightCoverageCapabilityRevisionRecord capability =
                    descriptor.Capabilities[index];
                if (capability != null &&
                    string.Equals(
                        capability.Token,
                        capabilityToken,
                        StringComparison.Ordinal) &&
                    capability.Revision >= minimumRevision)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool TryCreateCoverageBoundary(
            LightweightCoverageTransitionRecord transition,
            IDictionary<string, long> anchorSequences,
            out IMDataCoreHistoryBoundary boundary,
            out string errorMessage)
        {
            boundary = null;
            errorMessage = string.Empty;
            if (transition == null || transition.Sequence <= 0L)
            {
                errorMessage = "A selected coverage transition has an invalid sequence.";
                return false;
            }
            DateTime gameDate;
            if (!DateTime.TryParseExact(
                    transition.GameDateTime ?? string.Empty,
                    CoreConstants.RoundTripDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out gameDate))
            {
                errorMessage =
                    "A selected coverage transition has an invalid game date.";
                return false;
            }

            string anchorKey = transition.AnchorCheckpointKey ?? string.Empty;
            long semanticAnchorSequence = -1L;
            if (!string.IsNullOrEmpty(anchorKey))
            {
                if (anchorSequences == null ||
                    !anchorSequences.TryGetValue(
                        anchorKey,
                        out semanticAnchorSequence) ||
                    semanticAnchorSequence < 0L ||
                    semanticAnchorSequence >= transition.Sequence)
                {
                    errorMessage =
                        "A selected coverage transition has an unresolved or invalid exact checkpoint anchor.";
                    return false;
                }
            }

            boundary = new IMDataCoreHistoryBoundary(
                transition.Sequence,
                gameDate,
                anchorKey);
            boundary.SemanticAnchorSequence = semanticAnchorSequence;
            return true;
        }

        private static bool BoundariesShareExactAnchor(
            IMDataCoreHistoryBoundary left,
            IMDataCoreHistoryBoundary right)
        {
            return left != null && right != null &&
                left.HasAnchorCheckpoint && right.HasAnchorCheckpoint &&
                string.Equals(
                    left.AnchorCheckpointKey,
                    right.AnchorCheckpointKey,
                    StringComparison.Ordinal);
        }

        private static int CompareCoverageTransitionsBySequence(
            LightweightCoverageTransitionRecord left,
            LightweightCoverageTransitionRecord right)
        {
            if (ReferenceEquals(left, right))
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
            return left.Sequence.CompareTo(right.Sequence);
        }

        private static bool TryAssessCoverageRange(
            IMDataCoreHistoryCoverage coverage,
            IMDataCoreHistoryBoundary startBoundary,
            IMDataCoreHistoryBoundary endBoundary,
            out IMDataCoreCoverageAssessment assessment,
            out string errorMessage)
        {
            assessment = null;
            errorMessage = string.Empty;
            if (coverage == null || startBoundary == null || endBoundary == null)
            {
                errorMessage = "Coverage assessment requires non-null coverage and range boundaries.";
                return false;
            }
            if (startBoundary.Sequence < 0L ||
                endBoundary.Sequence <= startBoundary.Sequence)
            {
                errorMessage =
                    "Coverage assessment requires an increasing half-open shared-sequence range.";
                return false;
            }

            assessment = new IMDataCoreCoverageAssessment
            {
                Knownness = coverage.Knownness == IMDataCoreHistoryKnownness.NotApplicable
                    ? IMDataCoreHistoryKnownness.NotApplicable
                    : IMDataCoreHistoryKnownness.Unknown,
                StartBoundary = startBoundary,
                EndBoundary = endBoundary,
                IntersectingIntervals = new List<IMDataCoreCoverageInterval>(),
                IntersectingGaps = new List<IMDataCoreCoverageInterval>()
            };

            // Until the live v6 transition source is connected, the only truthful
            // current result is Unknown/NotApplicable. This branch intentionally
            // cannot manufacture Complete from an empty interval list. Once a
            // selected-branch interval model is supplied, the same evaluator below
            // can classify exact ranges without consulting event-row count.
            if (coverage.Knownness == IMDataCoreHistoryKnownness.Unknown ||
                coverage.Knownness == IMDataCoreHistoryKnownness.NotApplicable ||
                coverage.Intervals == null ||
                coverage.Intervals.Count == 0)
            {
                return true;
            }

            List<IMDataCoreCoverageInterval> activeIntervals =
                new List<IMDataCoreCoverageInterval>();
            for (int index = 0; index < coverage.Intervals.Count; index++)
            {
                IMDataCoreCoverageInterval interval = coverage.Intervals[index];
                if (interval == null || interval.StartBoundary == null)
                {
                    errorMessage =
                        "Coverage contains an interval without a start boundary.";
                    assessment = null;
                    return false;
                }

                long intervalStart = GetSemanticCoverageSequence(
                    interval.StartBoundary);
                long intervalEnd = interval.EndBoundary != null
                    ? GetSemanticCoverageSequence(interval.EndBoundary)
                    : long.MaxValue;
                if (intervalStart < 0L || intervalEnd <= intervalStart)
                {
                    errorMessage =
                        "Coverage contains an invalid half-open interval.";
                    assessment = null;
                    return false;
                }
                if (intervalStart >= endBoundary.Sequence ||
                    intervalEnd <= startBoundary.Sequence)
                {
                    // Native CareerStart emits the built-in activation one issued
                    // sequence later. That exact consecutive bootstrap is the only
                    // non-checkpoint semantic-start adjustment still needed here;
                    // load/F9 anchors were already resolved above to their actual
                    // checkpoint sequence.
                    if (!StartsAtSemanticCareerStartBoundary(
                            coverage,
                            interval,
                            startBoundary))
                    {
                        continue;
                    }
                }

                if (interval.State == IMDataCoreCoverageIntervalState.Gap)
                {
                    assessment.IntersectingGaps.Add(interval);
                    continue;
                }
                if (interval.State != IMDataCoreCoverageIntervalState.Active)
                {
                    errorMessage = "Coverage contains an unsupported interval state.";
                    assessment = null;
                    return false;
                }

                assessment.IntersectingIntervals.Add(interval);
                activeIntervals.Add(interval);
            }

            if (activeIntervals.Count == 0)
            {
                return true;
            }

            activeIntervals.Sort(CompareCoverageIntervalsByStartSequence);
            long coveredThrough = startBoundary.Sequence;
            bool hasCoveredPortion = false;
            for (int index = 0; index < activeIntervals.Count; index++)
            {
                IMDataCoreCoverageInterval interval = activeIntervals[index];
                long intervalStart = GetSemanticCoverageSequence(
                    interval.StartBoundary);
                long intervalEnd = interval.EndBoundary != null
                    ? GetSemanticCoverageSequence(interval.EndBoundary)
                    : long.MaxValue;

                if (StartsAtSemanticCareerStartBoundary(
                        coverage,
                        interval,
                        startBoundary))
                {
                    intervalStart = startBoundary.Sequence;
                }
                if (intervalEnd <= startBoundary.Sequence ||
                    intervalStart >= endBoundary.Sequence)
                {
                    continue;
                }
                if (intervalStart > coveredThrough)
                {
                    // There is an uncovered part of the requested range. Later
                    // active intervals can make the answer Partial, never Complete.
                    hasCoveredPortion = true;
                    break;
                }

                hasCoveredPortion = true;
                if (intervalEnd > coveredThrough)
                {
                    coveredThrough = intervalEnd;
                }
                if (coveredThrough >= endBoundary.Sequence)
                {
                    assessment.Knownness = IMDataCoreHistoryKnownness.Complete;
                    return true;
                }
            }

            assessment.Knownness = hasCoveredPortion
                ? IMDataCoreHistoryKnownness.Partial
                : IMDataCoreHistoryKnownness.Unknown;
            return true;
        }

        private static long GetSemanticCoverageSequence(
            IMDataCoreHistoryBoundary boundary)
        {
            if (boundary == null)
            {
                return long.MaxValue;
            }
            return boundary.HasAnchorCheckpoint &&
                    boundary.SemanticAnchorSequence >= 0L
                ? boundary.SemanticAnchorSequence
                : boundary.Sequence;
        }

        private static bool StartsAtSemanticCareerStartBoundary(
            IMDataCoreHistoryCoverage coverage,
            IMDataCoreCoverageInterval interval,
            IMDataCoreHistoryBoundary requestedStart)
        {
            if (coverage == null ||
                interval == null ||
                interval.StartBoundary == null ||
                requestedStart == null ||
                coverage.Knownness != IMDataCoreHistoryKnownness.Complete ||
                coverage.Intervals == null ||
                coverage.Intervals.Count == 0 ||
                !ReferenceEquals(coverage.Intervals[0], interval) ||
                coverage.SemanticStartBoundary == null ||
                coverage.SemanticStartBoundary.HasAnchorCheckpoint ||
                interval.StartBoundary.HasAnchorCheckpoint ||
                coverage.SemanticStartBoundary.Sequence == long.MaxValue ||
                interval.StartBoundary.Sequence !=
                    coverage.SemanticStartBoundary.Sequence + 1L)
            {
                return false;
            }

            return requestedStart.Sequence ==
                coverage.SemanticStartBoundary.Sequence;
        }

        private static int CompareCoverageIntervalsByStartSequence(
            IMDataCoreCoverageInterval left,
            IMDataCoreCoverageInterval right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }
            if (left == null || left.StartBoundary == null)
            {
                return -1;
            }
            if (right == null || right.StartBoundary == null)
            {
                return 1;
            }
            return left.StartBoundary.Sequence.CompareTo(
                right.StartBoundary.Sequence);
        }
    }
}
