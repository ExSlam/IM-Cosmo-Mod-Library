using System;
using System.Collections.Generic;
using System.Globalization;

namespace IMDataCore
{
    /// <summary>
    /// Runtime correlation and historical capture for transient business proposals.
    /// Proposal occurrence IDs are history-only correlation tokens. They do not
    /// restore gameplay state and are intentionally discarded across load/F9.
    /// </summary>
    internal sealed partial class IMDataCoreController
    {
        private const string BusinessProposalOccurrencePrefix = "p:";

        private readonly Dictionary<business._proposal, string> businessProposalOccurrenceByReference =
            new Dictionary<business._proposal, string>();

        private static string CreateBusinessProposalOccurrenceId()
        {
            return BusinessProposalOccurrencePrefix + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Captures the proposal at the actual presentation boundary. SetProposal is
        /// called only after generation has completed and immediately opens the popup.
        /// Re-presenting the same object does not synthesize a second birth occurrence.
        /// </summary>
        internal void CaptureBusinessProposalGenerated(business._proposal proposal)
        {
            if (proposal == null)
            {
                return;
            }

            string occurrenceId;
            bool newlyGenerated = false;
            lock (runtimeLock)
            {
                if (!businessProposalOccurrenceByReference.TryGetValue(proposal, out occurrenceId) ||
                    string.IsNullOrEmpty(occurrenceId))
                {
                    occurrenceId = CreateBusinessProposalOccurrenceId();
                    businessProposalOccurrenceByReference[proposal] = occurrenceId;
                    newlyGenerated = true;
                }
            }

            if (newlyGenerated)
            {
                CaptureBusinessProposalOccurrence(
                    proposal,
                    occurrenceId,
                    CoreConstants.EventTypeBusinessProposalGenerated,
                    CoreConstants.EventSourceBusinessProposalGeneratedPatch,
                    "generated",
                    false,
                    string.Empty);
            }

            // SetProposal is the source-proven UI presentation boundary. Keep the
            // legacy event name but give it truthful presentation semantics rather
            // than duplicating contract activation. Re-presenting the same proposal
            // can legitimately produce another window-open row with the same p: id.
            CaptureBusinessProposalOccurrence(
                proposal,
                occurrenceId,
                CoreConstants.EventTypeContractWindowOpened,
                CoreConstants.EventSourceBusinessProposalGeneratedPatch,
                "presented",
                false,
                string.Empty);
        }

        internal BusinessProposalTerminalSnapshot CreateBusinessProposalTerminalSnapshot(
            business businessSystem,
            bool accepted)
        {
            BusinessProposalTerminalSnapshot snapshot = new BusinessProposalTerminalSnapshot
            {
                Accepted = accepted,
                EventDate = staticVars.dateTime
            };
            if (businessSystem == null || businessSystem.ActiveProposal == null)
            {
                return snapshot;
            }

            business._proposal sourceProposal = businessSystem.ActiveProposal;
            business._proposal proposalSnapshot = CloneBusinessProposalForHistory(sourceProposal);
            if (proposalSnapshot == null)
            {
                return snapshot;
            }

            string occurrenceId;
            lock (runtimeLock)
            {
                if (!businessProposalOccurrenceByReference.TryGetValue(sourceProposal, out occurrenceId) ||
                    string.IsNullOrEmpty(occurrenceId))
                {
                    // Direct/modded assignment can bypass SetProposal. Preserve the
                    // terminal occurrence without fabricating a generated event.
                    occurrenceId = CreateBusinessProposalOccurrenceId();
                    businessProposalOccurrenceByReference[sourceProposal] = occurrenceId;
                }
            }

            snapshot.SourceProposalReference = sourceProposal;
            snapshot.Proposal = proposalSnapshot;
            snapshot.ProposalOccurrenceId = occurrenceId;
            return snapshot;
        }

        internal void CaptureBusinessProposalAccepted(
            BusinessProposalTerminalSnapshot proposalSnapshot,
            ContractAcceptedSnapshot contractSnapshot)
        {
            if (proposalSnapshot == null || proposalSnapshot.Proposal == null)
            {
                return;
            }

            string resultingContractEntityId = string.Empty;
            bool resultingContractKnown = false;
            business._proposal proposal = proposalSnapshot.Proposal;
            if (proposal.duration > CoreConstants.ZeroBasedListStartIndex)
            {
                int idolId = proposal.girl != null
                    ? proposal.girl.id
                    : CoreConstants.InvalidIdValue;
                DateTime startDate = contractSnapshot != null
                    ? contractSnapshot.AcceptedDate
                    : proposalSnapshot.EventDate;
                DateTime endDate = startDate.AddMonths(proposal.duration);
                string contractTypeCode = CoreEnumNameMapping.ToBusinessContractTypeCode(proposal.type);
                resultingContractEntityId = ResolveAcceptedContractHistoryEntityIdentifier(
                    contractSnapshot,
                    idolId,
                    contractTypeCode,
                    endDate);
                resultingContractKnown = !string.IsNullOrEmpty(resultingContractEntityId);
            }

            CaptureBusinessProposalOccurrence(
                proposal,
                proposalSnapshot.ProposalOccurrenceId,
                CoreConstants.EventTypeBusinessProposalAccepted,
                CoreConstants.EventSourceBusinessProposalAcceptedPatch,
                "accepted",
                resultingContractKnown,
                resultingContractEntityId);
            ForgetBusinessProposalOccurrence(proposalSnapshot);
        }

        internal void CaptureBusinessProposalDeclined(BusinessProposalTerminalSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Proposal == null)
            {
                return;
            }

            CaptureBusinessProposalOccurrence(
                snapshot.Proposal,
                snapshot.ProposalOccurrenceId,
                CoreConstants.EventTypeBusinessProposalDeclined,
                CoreConstants.EventSourceBusinessProposalDeclinedPatch,
                "declined",
                false,
                string.Empty);
            ForgetBusinessProposalOccurrence(snapshot);
        }

        private void CaptureBusinessProposalOccurrence(
            business._proposal proposal,
            string occurrenceId,
            string eventType,
            string sourcePatch,
            string lifecycleAction,
            bool resultingContractKnown,
            string resultingContractEntityId)
        {
            if (proposal == null || string.IsNullOrEmpty(occurrenceId))
            {
                return;
            }

            BusinessProposalEventPayload payload = BuildBusinessProposalPayload(
                proposal,
                occurrenceId,
                lifecycleAction,
                resultingContractKnown,
                resultingContractEntityId);
            int selectedIdolId = proposal.girl != null
                ? proposal.girl.id
                : CoreConstants.InvalidIdValue;

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    selectedIdolId,
                    CoreConstants.EventEntityKindBusinessProposal,
                    occurrenceId,
                    eventType,
                    sourcePatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));
                FlushAfterCaptureLocked();
            }
        }

        private static BusinessProposalEventPayload BuildBusinessProposalPayload(
            business._proposal proposal,
            string occurrenceId,
            string lifecycleAction,
            bool resultingContractKnown,
            string resultingContractEntityId)
        {
            List<data_girls.girls> candidates = proposal.Girls ?? new List<data_girls.girls>();
            BusinessProposalCandidatePayload[] candidatePayloads =
                new BusinessProposalCandidatePayload[candidates.Count];
            List<string> candidateIdParts = new List<string>();
            int selectedIdolId = proposal.girl != null
                ? proposal.girl.id
                : CoreConstants.InvalidIdValue;

            for (int index = CoreConstants.ZeroBasedListStartIndex; index < candidates.Count; index++)
            {
                data_girls.girls candidate = candidates[index];
                int idolId = candidate != null
                    ? candidate.id
                    : CoreConstants.InvalidIdValue;
                candidateIdParts.Add(idolId.ToString(CultureInfo.InvariantCulture));
                candidatePayloads[index] = new BusinessProposalCandidatePayload
                {
                    candidate_ordinal = index,
                    idol_id = idolId,
                    idol_name = candidate != null ? candidate.girlName(true) ?? string.Empty : string.Empty,
                    selected = candidate != null && ReferenceEquals(candidate, proposal.girl)
                };
            }

            int staffId = CoreConstants.InvalidIdValue;
            string staffName = string.Empty;
            string staffRole = string.Empty;
            if (proposal.staffer != null)
            {
                staffId = proposal.staffer.id;
                staffName = proposal.staffer.GetName(true, false) ?? string.Empty;
                staffRole = proposal.staffer.GetJobTitle() ?? string.Empty;
            }

            return new BusinessProposalEventPayload
            {
                proposal_occurrence_id = occurrenceId ?? string.Empty,
                proposal_lifecycle_action = lifecycleAction ?? string.Empty,
                proposal_type = CoreEnumNameMapping.ToBusinessContractTypeCode(proposal.type),
                proposal_skill = CoreEnumNameMapping.ToIdolParameterCode(proposal.skill),
                proposal_is_group = proposal.isGroup,
                proposal_audience = proposal.audience,
                candidate_count = candidates.Count,
                candidate_idol_id_list = string.Join(CoreConstants.IdentifierListSeparator, candidateIdParts.ToArray()),
                candidates = candidatePayloads,
                selected_idol_id = selectedIdolId,
                base_payment = proposal._payment,
                base_buzz = proposal._buzz,
                base_new_fans = proposal._newFans,
                base_fame = proposal._fame,
                effective_payment = proposal.girl != null ? proposal.payment : proposal._payment,
                effective_buzz = proposal.girl != null ? proposal.buzz : proposal._buzz,
                effective_new_fans = proposal.girl != null ? proposal.newFans : proposal._newFans,
                effective_fame = proposal.girl != null ? proposal.fame : proposal._fame,
                stamina = proposal.stamina,
                liability = proposal.girl != null ? proposal.liability : CoreConstants.ZeroLongValue,
                duration_months = proposal.duration,
                agent_name = proposal.agentName ?? string.Empty,
                product_name = proposal.productName ?? string.Empty,
                description = proposal.description ?? string.Empty,
                staff_id = staffId,
                staff_name = staffName,
                staff_role = staffRole,
                negotiation_known = proposal.negotiation.HasValue,
                negotiation_succeeded = proposal.negotiation.GetValueOrDefault(),
                negotiation_coefficient = proposal.negotiationCoeff,
                negotiation_attempts = proposal.negotiation_attempts,
                resulting_contract_reference_known = resultingContractKnown,
                resulting_contract_entity_id = resultingContractEntityId ?? string.Empty,
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };
        }

        internal static business._proposal CloneBusinessProposalForHistory(business._proposal source)
        {
            if (source == null)
            {
                return null;
            }

            business._proposal clone = source.Clone();
            if (clone != null)
            {
                clone.Girls = source.Girls != null
                    ? new List<data_girls.girls>(source.Girls)
                    : new List<data_girls.girls>();
            }
            return clone;
        }

        private void ForgetBusinessProposalOccurrence(BusinessProposalTerminalSnapshot snapshot)
        {
            if (snapshot == null || snapshot.SourceProposalReference == null)
            {
                return;
            }

            lock (runtimeLock)
            {
                businessProposalOccurrenceByReference.Remove(snapshot.SourceProposalReference);
            }
        }

        private void ResetBusinessProposalRuntimeStateLocked()
        {
            businessProposalOccurrenceByReference.Clear();
        }
    }

    internal sealed class BusinessProposalTerminalSnapshot
    {
        internal business._proposal SourceProposalReference;
        internal business._proposal Proposal;
        internal string ProposalOccurrenceId = string.Empty;
        internal bool Accepted;
        internal DateTime EventDate = default(DateTime);
    }

    [Serializable]
    internal sealed class BusinessProposalCandidatePayload
    {
        public int candidate_ordinal;
        public int idol_id = CoreConstants.InvalidIdValue;
        public string idol_name = string.Empty;
        public bool selected;
    }

    [Serializable]
    internal sealed class BusinessProposalEventPayload
    {
        public string proposal_occurrence_id = string.Empty;
        public string proposal_lifecycle_action = string.Empty;
        public string proposal_type = string.Empty;
        public string proposal_skill = string.Empty;
        public bool proposal_is_group;
        public int proposal_audience;
        public int candidate_count;
        public string candidate_idol_id_list = string.Empty;
        public BusinessProposalCandidatePayload[] candidates;
        public int selected_idol_id = CoreConstants.InvalidIdValue;
        public int base_payment;
        public int base_buzz;
        public int base_new_fans;
        public int base_fame;
        public int effective_payment;
        public int effective_buzz;
        public int effective_new_fans;
        public int effective_fame;
        public int stamina;
        public long liability;
        public int duration_months;
        public string agent_name = string.Empty;
        public string product_name = string.Empty;
        public string description = string.Empty;
        public int staff_id = CoreConstants.InvalidIdValue;
        public string staff_name = string.Empty;
        public string staff_role = string.Empty;
        public bool negotiation_known;
        public bool negotiation_succeeded;
        public float negotiation_coefficient;
        public int negotiation_attempts;
        public bool resulting_contract_reference_known;
        public string resulting_contract_entity_id = string.Empty;
        public string event_date = string.Empty;
    }
}
