using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using SaveNLoadFixes.Persistence;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// N02: preserves the exact business.active_proposal selected by a pending
    /// Event_Manager random event. Vanilla persists the event and the proposal list
    /// independently but drops the cross-system pointer VARIABLE__BIZ_PROPOSAL.
    ///
    /// Active proposals have no durable ID. The repair therefore stores the exact
    /// serialized proposal ordinal plus a full structural witness, and correlates it
    /// to the exact pending active-event ordinal plus event/actor requirement witness.
    /// Restore never searches for a "close enough" proposal: the ordinal candidate
    /// must still match every persisted witness field before the pointer is rebound.
    /// </summary>
    internal static class SelectedBusinessProposalRepair
    {
        internal const int SectionVersion = 1;

        private static long captureFailureCount;
        private static long staleClearCount;
        private static long restoredCount;
        private static long restoredNullCount;
        private static long legacyMissingCount;
        private static long invalidSectionCount;
        private static long associationMissingCount;
        private static string lastDiagnostic = string.Empty;

        internal static bool IsImplemented { get { return SelectedBusinessProposalPatchHealth.IsHealthy; } }
        internal static long CaptureFailureCount { get { return Interlocked.Read(ref captureFailureCount); } }
        internal static long StaleClearCount { get { return Interlocked.Read(ref staleClearCount); } }
        internal static long RestoredCount { get { return Interlocked.Read(ref restoredCount); } }
        internal static long RestoredNullCount { get { return Interlocked.Read(ref restoredNullCount); } }
        internal static long LegacyMissingCount { get { return Interlocked.Read(ref legacyMissingCount); } }
        internal static long InvalidSectionCount { get { return Interlocked.Read(ref invalidSectionCount); } }
        internal static long AssociationMissingCount { get { return Interlocked.Read(ref associationMissingCount); } }
        internal static string LastDiagnostic { get { return lastDiagnostic; } }

        internal static bool TryCaptureForEnvelope(
            SaveManager.SavedData dataToSave,
            out SelectedBusinessProposalRecordV1 record,
            out string error)
        {
            record = null;
            error = string.Empty;

            if (dataToSave == null || dataToSave.Event_Manager__activeEvents == null ||
                dataToSave.business__ActiveProposalsData == null)
            {
                return CaptureFailed("N02 target SavedData event/proposal rows are unavailable.", out error);
            }

            mainScript main = Camera.main == null ? null : Camera.main.GetComponent<mainScript>();
            if (main == null || main.Data == null)
            {
                return CaptureFailed("N02 cannot resolve mainScript/Data during caller-thread capture.", out error);
            }

            Event_Manager events = main.Data.GetComponent<Event_Manager>();
            business businessManager = main.Data.GetComponent<business>();
            if (events == null || businessManager == null || events.activeEvents == null ||
                businessManager.ActiveProposals == null)
            {
                return CaptureFailed("N02 live Event_Manager/business registries are unavailable.", out error);
            }

            if (events.activeEvents.Count != dataToSave.Event_Manager__activeEvents.Count ||
                businessManager.ActiveProposals.Count != dataToSave.business__ActiveProposalsData.Count)
            {
                return CaptureFailed("N02 serialized/live event or proposal list counts diverge at checkpoint capture.", out error);
            }

            int eventOrdinal;
            int actorOrdinal;
            business._type requiredType;
            if (!TryFindLatestBusinessEventOwner(events.activeEvents, out eventOrdinal, out actorOrdinal, out requiredType))
            {
                record = EmptyRecord();
                return true;
            }

            Event_Manager._activeEvent liveEvent = events.activeEvents[eventOrdinal];
            if (liveEvent == null || liveEvent.state == Event_Manager._activeEvent._state.complete)
            {
                record = EmptyRecord();
                return true;
            }

            if (liveEvent.state != Event_Manager._activeEvent._state.waiting)
            {
                return CaptureFailed("N02 refuses to freeze a business-event pointer outside the audited waiting checkpoint state.", out error);
            }

            business.active_proposal selected = Event_Manager.VARIABLE__BIZ_PROPOSAL;
            if (selected == null)
            {
                return CaptureFailed("N02 pending business event has no selected proposal pointer.", out error);
            }

            Event_Manager._activeEvent._actor ownerActor = liveEvent.actors[actorOrdinal];
            if (!ProposalQualifiesForActor(selected, ownerActor, requiredType))
            {
                return CaptureFailed("N02 selected proposal no longer satisfies the pending event's final business requirement.", out error);
            }

            int proposalOrdinal = FindExactReferenceOrdinal(businessManager.ActiveProposals, selected);
            if (proposalOrdinal < 0)
            {
                return CaptureFailed("N02 selected proposal is not present exactly once in business.ActiveProposals.", out error);
            }

            Event_Manager.ActiveEventData savedEvent = dataToSave.Event_Manager__activeEvents[eventOrdinal];
            business.active_proposal_data savedProposal = dataToSave.business__ActiveProposalsData[proposalOrdinal];
            if (!SavedEventMatchesLive(savedEvent, liveEvent, actorOrdinal, requiredType) ||
                !SavedProposalMatchesLive(savedProposal, selected))
            {
                return CaptureFailed("N02 serialized event/proposal witnesses do not exactly match live checkpoint state.", out error);
            }

            record = BuildRecord(eventOrdinal, savedEvent, actorOrdinal, requiredType, proposalOrdinal, savedProposal);
            return true;
        }

        internal static void ClearBeforeVanillaEventLoad()
        {
            Event_Manager.VARIABLE__BIZ_PROPOSAL = null;
            Interlocked.Increment(ref staleClearCount);
            lastDiagnostic = "N02 cleared stale VARIABLE__BIZ_PROPOSAL before Event_Manager reconstruction.";
        }

        internal static void RestoreAfterCareerLoad(SaveManager manager)
        {
            SaveManager.SavedData target = manager == null ? null : manager.Data;
            if (target == null)
            {
                return;
            }

            RepairEnvelopeLoadState state;
            if (!RepairEnvelopeTransport.TryGetLoadState(target, out state))
            {
                Interlocked.Increment(ref associationMissingCount);
                lastDiagnostic = "N02 failed closed because adopted SavedData has no repair-envelope association.";
                Debug.LogError(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
                return;
            }

            Event_Manager.VARIABLE__BIZ_PROPOSAL = null;

            if (!state.Present || state.Envelope == null || state.Envelope.records == null ||
                state.Envelope.records.selected_business_proposal_version == 0)
            {
                Interlocked.Increment(ref legacyMissingCount);
                lastDiagnostic = "N02 legacy/pre-section save leaves selected business proposal unknown and null.";
                return;
            }

            if (!state.Valid || state.Envelope.records.selected_business_proposal_version != SectionVersion ||
                state.Envelope.records.selected_business_proposal == null)
            {
                RecordInvalid("N02 repair section is invalid, unsupported, or missing its record.");
                return;
            }

            SelectedBusinessProposalRecordV1 record = state.Envelope.records.selected_business_proposal;
            if (!record.has_value)
            {
                if (!IsStrictEmptyRecord(record))
                {
                    RecordInvalid("N02 null-selection record contains non-empty locator/witness state.");
                    return;
                }
                Interlocked.Increment(ref restoredNullCount);
                lastDiagnostic = "N02 restored an exact null selected-business-proposal state.";
                return;
            }

            if (target.Event_Manager__activeEvents == null || target.business__ActiveProposalsData == null ||
                record.active_event_ordinal < 0 || record.active_event_ordinal >= target.Event_Manager__activeEvents.Count ||
                record.proposal_ordinal < 0 || record.proposal_ordinal >= target.business__ActiveProposalsData.Count)
            {
                RecordInvalid("N02 persisted event/proposal ordinal is outside the target DTO lists.");
                return;
            }

            mainScript main = Camera.main == null ? null : Camera.main.GetComponent<mainScript>();
            if (main == null || main.Data == null)
            {
                RecordInvalid("N02 cannot resolve mainScript/Data after career load.");
                return;
            }

            Event_Manager events = main.Data.GetComponent<Event_Manager>();
            business businessManager = main.Data.GetComponent<business>();
            if (events == null || businessManager == null || events.activeEvents == null || businessManager.ActiveProposals == null ||
                events.activeEvents.Count != target.Event_Manager__activeEvents.Count ||
                businessManager.ActiveProposals.Count != target.business__ActiveProposalsData.Count ||
                record.active_event_ordinal >= events.activeEvents.Count || record.proposal_ordinal >= businessManager.ActiveProposals.Count)
            {
                RecordInvalid("N02 reconstructed event/proposal lists do not exactly match target DTO cardinality.");
                return;
            }

            Event_Manager.ActiveEventData savedEvent = target.Event_Manager__activeEvents[record.active_event_ordinal];
            Event_Manager._activeEvent liveEvent = events.activeEvents[record.active_event_ordinal];
            business.active_proposal_data savedProposal = target.business__ActiveProposalsData[record.proposal_ordinal];
            business.active_proposal liveProposal = businessManager.ActiveProposals[record.proposal_ordinal];

            if (!RecordMatchesSavedEvent(record, savedEvent) || !RecordMatchesLiveEvent(record, liveEvent) ||
                !RecordMatchesSavedProposal(record, savedProposal) || !RecordMatchesLiveProposal(record, liveProposal))
            {
                RecordInvalid("N02 event/proposal ordinal candidate failed its persisted structural witness.");
                return;
            }

            int latestEventOrdinal;
            int latestActorOrdinal;
            business._type requiredType;
            if (!TryFindLatestBusinessEventOwner(events.activeEvents, out latestEventOrdinal, out latestActorOrdinal, out requiredType) ||
                latestEventOrdinal != record.active_event_ordinal || latestActorOrdinal != record.business_actor_ordinal ||
                (int)requiredType != record.required_proposal_type ||
                !ProposalQualifiesForActor(liveProposal, liveEvent.actors[latestActorOrdinal], requiredType))
            {
                RecordInvalid("N02 reconstructed pending event is no longer the exact owner of the selected-proposal variable.");
                return;
            }

            Event_Manager.VARIABLE__BIZ_PROPOSAL = liveProposal;
            Interlocked.Increment(ref restoredCount);
            lastDiagnostic = "N02 rebound exact business proposal ordinal " + record.proposal_ordinal +
                " to pending event ordinal " + record.active_event_ordinal + ".";
        }

        private static SelectedBusinessProposalRecordV1 EmptyRecord()
        {
            return new SelectedBusinessProposalRecordV1
            {
                liability_decimal = "0"
            };
        }

        private static bool TryFindLatestBusinessEventOwner(
            List<Event_Manager._activeEvent> events,
            out int eventOrdinal,
            out int actorOrdinal,
            out business._type requiredType)
        {
            eventOrdinal = -1;
            actorOrdinal = -1;
            requiredType = business._type.DEFAULT;
            if (events == null)
            {
                return false;
            }

            for (int eventIndex = 0; eventIndex < events.Count; eventIndex++)
            {
                Event_Manager._activeEvent activeEvent = events[eventIndex];
                if (activeEvent == null || activeEvent.actors == null)
                {
                    continue;
                }

                int eventActor = -1;
                business._type eventType = business._type.DEFAULT;
                for (int actorIndex = 0; actorIndex < activeEvent.actors.Count; actorIndex++)
                {
                    Event_Manager._activeEvent._actor actor = activeEvent.actors[actorIndex];
                    if (actor == null || actor.data == null || actor.data.requirement == null)
                    {
                        continue;
                    }

                    for (int requirementIndex = 0; requirementIndex < actor.data.requirement.Count; requirementIndex++)
                    {
                        Event_Manager._randomEvent._actor._requirement requirement = actor.data.requirement[requirementIndex];
                        if (requirement == null || requirement.parameter != "business")
                        {
                            continue;
                        }

                        eventActor = actorIndex;
                        eventType = RequirementType(requirement.formula);
                    }
                }

                if (eventActor >= 0)
                {
                    eventOrdinal = eventIndex;
                    actorOrdinal = eventActor;
                    requiredType = eventType;
                }
            }

            return eventOrdinal >= 0;
        }

        private static business._type RequirementType(string formula)
        {
            if (formula == "ad")
            {
                return business._type.ad;
            }
            if (formula == "tv")
            {
                return business._type.tv_drama;
            }
            return business._type.DEFAULT;
        }

        private static bool ProposalQualifiesForActor(
            business.active_proposal proposal,
            Event_Manager._activeEvent._actor actor,
            business._type requiredType)
        {
            if (proposal == null || actor == null)
            {
                return false;
            }
            if (!object.ReferenceEquals(proposal.Girl, actor.girl))
            {
                return false;
            }
            return requiredType == business._type.DEFAULT || proposal.Type == requiredType;
        }

        private static int FindExactReferenceOrdinal(List<business.active_proposal> proposals, business.active_proposal selected)
        {
            int found = -1;
            for (int index = 0; index < proposals.Count; index++)
            {
                if (!object.ReferenceEquals(proposals[index], selected))
                {
                    continue;
                }
                if (found >= 0)
                {
                    return -1;
                }
                found = index;
            }
            return found;
        }

        private static bool SavedEventMatchesLive(Event_Manager.ActiveEventData saved, Event_Manager._activeEvent live, int actorOrdinal, business._type requiredType)
        {
            if (saved == null || live == null || live.data == null || saved.data != live.data.id ||
                saved.state != live.state || saved.date != ExtensionMethods.ToDataString(live.date) ||
                saved.actors == null || live.actors == null || saved.actors.Count != live.actors.Count ||
                actorOrdinal < 0 || actorOrdinal >= saved.actors.Count)
            {
                return false;
            }

            Event_Manager.ActiveEventData.ActorData savedActor = saved.actors[actorOrdinal];
            Event_Manager._activeEvent._actor liveActor = live.actors[actorOrdinal];
            int liveGirlId = liveActor != null && liveActor.girl != null ? liveActor.girl.id : -1;
            int liveStaffId = liveActor != null && liveActor.staff != null ? liveActor.staff.id : -1;
            return savedActor != null && savedActor.girl == liveGirlId && savedActor.staff == liveStaffId &&
                FindFinalBusinessType(liveActor) == requiredType;
        }

        private static business._type FindFinalBusinessType(Event_Manager._activeEvent._actor actor)
        {
            business._type result = business._type.DEFAULT;
            bool found = false;
            if (actor != null && actor.data != null && actor.data.requirement != null)
            {
                for (int index = 0; index < actor.data.requirement.Count; index++)
                {
                    Event_Manager._randomEvent._actor._requirement requirement = actor.data.requirement[index];
                    if (requirement != null && requirement.parameter == "business")
                    {
                        result = RequirementType(requirement.formula);
                        found = true;
                    }
                }
            }
            return found ? result : (business._type)(-1);
        }

        private static bool SavedProposalMatchesLive(business.active_proposal_data saved, business.active_proposal live)
        {
            if (saved == null || live == null)
            {
                return false;
            }
            int girlId = live.Girl == null ? -1 : live.Girl.id;
            return saved.isGroup == live.isGroup && saved.Girl == girlId && saved.Skill == live.Skill &&
                saved.Type == live.Type && saved.Payment_per_week == live.Payment_per_week &&
                saved.Buzz_per_week == live.Buzz_per_week && saved.Fame_per_week == live.Fame_per_week &&
                saved.Stamina_per_week == live.Stamina_per_week && saved.Fans_per_week == live.Fans_per_week &&
                saved.Agent_Name == live.Agent_Name && saved.Product_Name == live.Product_Name &&
                saved.Liability == live.Liability && saved.EndDate == ExtensionMethods.ToDataString(live.EndDate);
        }

        private static SelectedBusinessProposalRecordV1 BuildRecord(
            int eventOrdinal,
            Event_Manager.ActiveEventData savedEvent,
            int actorOrdinal,
            business._type requiredType,
            int proposalOrdinal,
            business.active_proposal_data savedProposal)
        {
            Event_Manager.ActiveEventData.ActorData actor = savedEvent.actors[actorOrdinal];
            return new SelectedBusinessProposalRecordV1
            {
                has_value = true,
                active_event_ordinal = eventOrdinal,
                event_id = savedEvent.data ?? string.Empty,
                event_date = savedEvent.date ?? string.Empty,
                event_state = (int)savedEvent.state,
                business_actor_ordinal = actorOrdinal,
                actor_girl_id = actor == null ? -1 : actor.girl,
                actor_staff_id = actor == null ? -1 : actor.staff,
                required_proposal_type = (int)requiredType,
                proposal_ordinal = proposalOrdinal,
                proposal_is_group = savedProposal.isGroup,
                proposal_girl_id = savedProposal.Girl,
                proposal_skill = (int)savedProposal.Skill,
                proposal_type = (int)savedProposal.Type,
                payment_per_week = savedProposal.Payment_per_week,
                buzz_per_week = savedProposal.Buzz_per_week,
                fame_per_week = savedProposal.Fame_per_week,
                stamina_per_week = savedProposal.Stamina_per_week,
                fans_per_week = savedProposal.Fans_per_week,
                agent_name = savedProposal.Agent_Name ?? string.Empty,
                product_name = savedProposal.Product_Name ?? string.Empty,
                liability = savedProposal.Liability,
                liability_decimal = savedProposal.Liability.ToString(CultureInfo.InvariantCulture),
                end_date = savedProposal.EndDate ?? string.Empty
            };
        }

        private static bool RecordMatchesSavedEvent(SelectedBusinessProposalRecordV1 record, Event_Manager.ActiveEventData saved)
        {
            if (saved == null || saved.actors == null || record.business_actor_ordinal < 0 ||
                record.business_actor_ordinal >= saved.actors.Count)
            {
                return false;
            }
            Event_Manager.ActiveEventData.ActorData actor = saved.actors[record.business_actor_ordinal];
            return saved.data == record.event_id && saved.date == record.event_date &&
                (int)saved.state == record.event_state && saved.state == Event_Manager._activeEvent._state.waiting &&
                actor != null && actor.girl == record.actor_girl_id && actor.staff == record.actor_staff_id;
        }

        private static bool RecordMatchesLiveEvent(SelectedBusinessProposalRecordV1 record, Event_Manager._activeEvent live)
        {
            if (live == null || live.data == null || live.actors == null || record.business_actor_ordinal < 0 ||
                record.business_actor_ordinal >= live.actors.Count || live.state != Event_Manager._activeEvent._state.waiting)
            {
                return false;
            }
            Event_Manager._activeEvent._actor actor = live.actors[record.business_actor_ordinal];
            int girlId = actor != null && actor.girl != null ? actor.girl.id : -1;
            int staffId = actor != null && actor.staff != null ? actor.staff.id : -1;
            return live.data.id == record.event_id && ExtensionMethods.ToDataString(live.date) == record.event_date &&
                (int)live.state == record.event_state && girlId == record.actor_girl_id && staffId == record.actor_staff_id &&
                (int)FindFinalBusinessType(actor) == record.required_proposal_type;
        }

        private static bool RecordMatchesSavedProposal(SelectedBusinessProposalRecordV1 record, business.active_proposal_data saved)
        {
            return saved != null && saved.isGroup == record.proposal_is_group && saved.Girl == record.proposal_girl_id &&
                (int)saved.Skill == record.proposal_skill && (int)saved.Type == record.proposal_type &&
                saved.Payment_per_week == record.payment_per_week && saved.Buzz_per_week == record.buzz_per_week &&
                saved.Fame_per_week == record.fame_per_week && saved.Stamina_per_week == record.stamina_per_week &&
                saved.Fans_per_week == record.fans_per_week && (saved.Agent_Name ?? string.Empty) == record.agent_name &&
                (saved.Product_Name ?? string.Empty) == record.product_name && saved.Liability == record.liability &&
                (saved.EndDate ?? string.Empty) == record.end_date;
        }

        private static bool RecordMatchesLiveProposal(SelectedBusinessProposalRecordV1 record, business.active_proposal live)
        {
            if (live == null)
            {
                return false;
            }
            int girlId = live.Girl == null ? -1 : live.Girl.id;
            return live.isGroup == record.proposal_is_group && girlId == record.proposal_girl_id &&
                (int)live.Skill == record.proposal_skill && (int)live.Type == record.proposal_type &&
                live.Payment_per_week == record.payment_per_week && live.Buzz_per_week == record.buzz_per_week &&
                live.Fame_per_week == record.fame_per_week && live.Stamina_per_week == record.stamina_per_week &&
                live.Fans_per_week == record.fans_per_week && (live.Agent_Name ?? string.Empty) == record.agent_name &&
                (live.Product_Name ?? string.Empty) == record.product_name && live.Liability == record.liability &&
                ExtensionMethods.ToDataString(live.EndDate) == record.end_date;
        }

        private static bool IsStrictEmptyRecord(SelectedBusinessProposalRecordV1 record)
        {
            return record.active_event_ordinal == -1 && record.business_actor_ordinal == -1 && record.proposal_ordinal == -1 &&
                record.actor_girl_id == -1 && record.actor_staff_id == -1 && record.required_proposal_type == -1 &&
                record.proposal_girl_id == -1 && record.event_state == 0 && record.proposal_skill == 0 &&
                record.proposal_type == 0 && record.payment_per_week == 0 && record.buzz_per_week == 0 &&
                record.fame_per_week == 0 && record.stamina_per_week == 0 && record.fans_per_week == 0 &&
                record.liability == 0L && !record.proposal_is_group &&
                (string.IsNullOrEmpty(record.liability_decimal) || record.liability_decimal == "0") &&
                string.IsNullOrEmpty(record.event_id) && string.IsNullOrEmpty(record.event_date) &&
                string.IsNullOrEmpty(record.agent_name) && string.IsNullOrEmpty(record.product_name) &&
                string.IsNullOrEmpty(record.end_date);
        }

        private static bool CaptureFailed(string diagnostic, out string error)
        {
            error = diagnostic ?? "unknown N02 capture failure";
            Interlocked.Increment(ref captureFailureCount);
            lastDiagnostic = error;
            return false;
        }

        private static void RecordInvalid(string diagnostic)
        {
            Interlocked.Increment(ref invalidSectionCount);
            lastDiagnostic = diagnostic ?? "unknown N02 repair failure";
            Debug.LogError(SaveNLoadFixesConstants.LogPrefix + lastDiagnostic);
        }
    }

    internal static class SelectedBusinessProposalPatchHealth
    {
        internal const int ExpectedTargetMethodCount = 3;
        private static readonly object Sync = new object();
        private static int resolvedTargetMethodCount;
        private static string failure = string.Empty;

        internal static bool IsHealthy
        {
            get { lock (Sync) { return resolvedTargetMethodCount == ExpectedTargetMethodCount && string.IsNullOrEmpty(failure); } }
        }
        internal static int ResolvedTargetMethodCount { get { lock (Sync) { return resolvedTargetMethodCount; } } }
        internal static string Failure { get { lock (Sync) { return failure; } } }
        internal static void ReportTargetResolved()
        {
            lock (Sync)
            {
                resolvedTargetMethodCount++;
                if (resolvedTargetMethodCount > ExpectedTargetMethodCount)
                {
                    failure = "N02 resolved more patch targets than the frozen three-method manifest.";
                }
            }
        }
        internal static void ReportFailure(string diagnostic)
        {
            lock (Sync) { failure = diagnostic ?? "unknown N02 patch failure"; }
        }
    }
}
