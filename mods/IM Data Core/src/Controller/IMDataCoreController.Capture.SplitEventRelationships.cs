using System;
using System.Collections.Generic;
using System.Globalization;

namespace IMDataCore
{
    internal sealed partial class IMDataCoreController
    {
        private readonly Dictionary<Event_Manager._activeEvent, string> randomEventOccurrenceByReference =
            new Dictionary<Event_Manager._activeEvent, string>();
        private readonly Dictionary<Event_Manager._activeEvent, SelectedContractHistoryContext> randomEventSelectedContractByReference =
            new Dictionary<Event_Manager._activeEvent, SelectedContractHistoryContext>();
        private readonly HashSet<Event_Manager._activeEvent> randomEventTerminalCapturedByReference =
            new HashSet<Event_Manager._activeEvent>();
        private readonly Dictionary<Event_Templates._active_template, string> templateOccurrenceByReference =
            new Dictionary<Event_Templates._active_template, string>();
        private readonly Dictionary<Event_Templates._active_template, List<Event_Templates._template._reply>> templatePresentedRepliesByReference =
            new Dictionary<Event_Templates._active_template, List<Event_Templates._template._reply>>();
        private readonly HashSet<Event_Templates._active_template> templatePresentedCapturedByReference =
            new HashSet<Event_Templates._active_template>();
        private readonly HashSet<Event_Templates._active_template> templateTerminalCapturedByReference =
            new HashSet<Event_Templates._active_template>();

        /// <summary>
        /// Clears transient correlation bindings. The durable occurrence rows remain
        /// history-only and are never promoted into save/current-state persistence.
        /// </summary>
        private void ResetSplitEventRelationshipHistoryRuntimeStateLocked()
        {
            randomEventOccurrenceByReference.Clear();
            randomEventSelectedContractByReference.Clear();
            randomEventTerminalCapturedByReference.Clear();
            templateOccurrenceByReference.Clear();
            templatePresentedRepliesByReference.Clear();
            templatePresentedCapturedByReference.Clear();
            templateTerminalCapturedByReference.Clear();
        }

        /// <summary>
        /// Returns whether vanilla already owns a durable relationship object for the
        /// exact pair, without calling GetRelationship and therefore without creating it.
        /// </summary>
        internal bool RelationshipPairExistsBefore(data_girls.girls girlA, data_girls.girls girlB)
        {
            if (girlA == null || girlB == null || ReferenceEquals(girlA, girlB) || Relationships.RelationshipsData == null)
            {
                return false;
            }

            for (int index = CoreConstants.ZeroBasedListStartIndex; index < Relationships.RelationshipsData.Count; index++)
            {
                Relationships._relationship relationship = Relationships.RelationshipsData[index];
                if (relationship == null || relationship.Girls == null)
                {
                    continue;
                }

                if (relationship.Girls.Contains(girlA) && relationship.Girls.Contains(girlB))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Captures the initial randomized Dynamic only when vanilla truly inserts a new
        /// runtime relationship row. Load/bootstrap reconstruction is deliberately ignored.
        /// </summary>
        internal void CaptureIdolRelationshipCreated(Relationships._relationship relationship, bool existedBefore)
        {
            if (relationship == null || existedBefore || Relationships.RelationshipsData == null)
            {
                return;
            }

            bool containedByReference = false;
            for (int index = CoreConstants.ZeroBasedListStartIndex; index < Relationships.RelationshipsData.Count; index++)
            {
                if (ReferenceEquals(Relationships.RelationshipsData[index], relationship))
                {
                    containedByReference = true;
                    break;
                }
            }

            if (!containedByReference)
            {
                return;
            }

            int idolAId;
            int idolBId;
            if (!TryResolveRelationshipPairIdolIdentifiers(relationship, out idolAId, out idolBId))
            {
                return;
            }

            string pairKey = BuildRelationshipPairEntityIdentifier(idolAId, idolBId);
            IdolRelationshipLifecyclePayload payload = new IdolRelationshipLifecyclePayload
            {
                IdolAId = idolAId,
                IdolBId = idolBId,
                RelationshipStatus = CoreEnumNameMapping.ToRelationshipStatusCode(relationship.Status),
                RelationshipDynamic = CoreEnumNameMapping.ToRelationshipDynamicCode(relationship.Dynamic),
                RelationshipKnownToPlayer = relationship.IsRelationshipKnown(),
                RelationshipPairKey = pairKey,
                RelationshipBreakReason = string.Empty,
                RelationshipIsDating = relationship.Dating
            };

            lock (runtimeLock)
            {
                if (saveLoadPreparationActive)
                {
                    return;
                }

                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                EnqueueEventRecordLocked(
                    staticVars.dateTime,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindRelationship,
                    pairKey,
                    CoreConstants.EventTypeIdolRelationshipCreated,
                    CoreConstants.EventSourceIdolRelationshipCreatedPatch,
                    CoreJsonUtility.SerializeIdolRelationshipLifecyclePayload(payload));
                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Adds a history-only occurrence key and exact selected business contract context
        /// to a successful random-event start. Caller holds runtimeLock.
        /// </summary>
        private void PrepareRandomEventStartedHistoryLocked(
            Event_Manager._activeEvent activeEvent,
            Event_Manager._randomEvent randomEvent,
            RandomEventStartedEventPayload payload)
        {
            if (activeEvent == null || payload == null)
            {
                return;
            }

            string occurrenceId;
            if (!randomEventOccurrenceByReference.TryGetValue(activeEvent, out occurrenceId) || string.IsNullOrEmpty(occurrenceId))
            {
                occurrenceId = string.Concat("re:", Guid.NewGuid().ToString("N"));
                randomEventOccurrenceByReference[activeEvent] = occurrenceId;
            }

            payload.random_event_occurrence_id = occurrenceId;

            SelectedContractHistoryContext context = BuildSelectedContractHistoryContextLocked(randomEvent);
            if (context != null)
            {
                // Freeze even an explicit "business event but no selected contract"
                // result so a later unrelated global pointer cannot contaminate this run.
                randomEventSelectedContractByReference[activeEvent] = context;
                CopySelectedContractContextToPayload(context, payload);
            }
        }

        /// <summary>
        /// Captures occurrence/contract context before reply effects can remove a contract.
        /// </summary>
        private void PrepareRandomEventConcludeSnapshot(RandomEventConcludeSnapshot snapshot)
        {
            if (snapshot == null || snapshot.ActiveEventBefore == null)
            {
                return;
            }

            lock (runtimeLock)
            {
                snapshot.RandomEventOccurrenceId = ResolveRandomEventOccurrenceIdLocked(snapshot.ActiveEventBefore);
                SelectedContractHistoryContext context;
                if (!randomEventSelectedContractByReference.TryGetValue(snapshot.ActiveEventBefore, out context) || context == null)
                {
                    context = BuildSelectedContractHistoryContextLocked(snapshot.ActiveEventBefore.data);
                }

                snapshot.SelectedContractContext = context;
            }
        }

        /// <summary>
        /// Applies pre-mutation correlation to the normal ConcludeEvent payload.
        /// Caller holds runtimeLock.
        /// </summary>
        private void PrepareRandomEventConcludedHistoryLocked(
            Event_Manager._activeEvent activeEvent,
            RandomEventConcludeSnapshot snapshot,
            RandomEventConcludedEventPayload payload)
        {
            if (activeEvent == null || payload == null)
            {
                return;
            }

            payload.random_event_occurrence_id = snapshot != null && !string.IsNullOrEmpty(snapshot.RandomEventOccurrenceId)
                ? snapshot.RandomEventOccurrenceId
                : ResolveRandomEventOccurrenceIdLocked(activeEvent);
            payload.terminal_path = "reply";
            payload.reply_effects_applied = true;
            payload.resource_delta_known = true;
            CopySelectedContractContextToPayload(snapshot != null ? snapshot.SelectedContractContext : null, payload);
        }

        private string ResolveRandomEventOccurrenceIdLocked(Event_Manager._activeEvent activeEvent)
        {
            if (activeEvent == null)
            {
                return string.Empty;
            }

            string occurrenceId;
            if (randomEventOccurrenceByReference.TryGetValue(activeEvent, out occurrenceId) && !string.IsNullOrEmpty(occurrenceId))
            {
                return occurrenceId;
            }

            string randomEventId = activeEvent.data != null ? (activeEvent.data.id ?? string.Empty) : string.Empty;
            if (storageEngine != null &&
                storageEngine.TryFindLatestOpenRandomEventOccurrence(randomEventId, out occurrenceId) &&
                !string.IsNullOrEmpty(occurrenceId))
            {
                randomEventOccurrenceByReference[activeEvent] = occurrenceId;
                return occurrenceId;
            }

            occurrenceId = string.Concat("re:", Guid.NewGuid().ToString("N"));
            randomEventOccurrenceByReference[activeEvent] = occurrenceId;
            return occurrenceId;
        }

        private SelectedContractHistoryContext BuildSelectedContractHistoryContextLocked(Event_Manager._randomEvent randomEvent)
        {
            if (!RandomEventRequiresBusinessProposal(randomEvent))
            {
                return null;
            }

            business.active_proposal selected = Event_Manager.VARIABLE__BIZ_PROPOSAL;
            if (selected == null)
            {
                return new SelectedContractHistoryContext { Present = false };
            }

            int idolId = selected.Girl != null ? selected.Girl.id : CoreConstants.InvalidIdValue;
            string contractType = CoreEnumNameMapping.ToBusinessContractTypeCode(selected.Type);
            string instanceId = ResolveContractHistoryEntityIdentifierLocked(selected, idolId, contractType, selected.EndDate);
            return new SelectedContractHistoryContext
            {
                Present = true,
                InstanceId = instanceId ?? string.Empty,
                TargetIdolId = idolId,
                ContractType = contractType ?? string.Empty,
                AgentName = selected.Agent_Name ?? string.Empty,
                ProductName = selected.Product_Name ?? string.Empty,
                EndDate = CoreDateTimeUtility.ToRoundTripString(selected.EndDate)
            };
        }

        private static bool RandomEventRequiresBusinessProposal(Event_Manager._randomEvent randomEvent)
        {
            // Vanilla chooses VARIABLE__BIZ_PROPOSAL inside Event_Manager.SetVariables,
            // which walks each actor definition's requirement list rather than the
            // event-level condition list. Mirror that exact ownership boundary.
            if (randomEvent == null || randomEvent.actor == null)
            {
                return false;
            }

            for (int actorIndex = CoreConstants.ZeroBasedListStartIndex; actorIndex < randomEvent.actor.Count; actorIndex++)
            {
                Event_Manager._randomEvent._actor actor = randomEvent.actor[actorIndex];
                if (actor == null || actor.requirement == null)
                {
                    continue;
                }

                for (int requirementIndex = CoreConstants.ZeroBasedListStartIndex; requirementIndex < actor.requirement.Count; requirementIndex++)
                {
                    Event_Manager._randomEvent._actor._requirement requirement = actor.requirement[requirementIndex];
                    if (requirement != null && string.Equals(requirement.parameter, "business", StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void CopySelectedContractContextToPayload(SelectedContractHistoryContext context, RandomEventStartedEventPayload payload)
        {
            if (payload == null || context == null)
            {
                return;
            }

            payload.selected_contract_present = context.Present;
            payload.selected_contract_instance_id = context.InstanceId ?? string.Empty;
            payload.selected_contract_target_idol_id = context.TargetIdolId;
            payload.selected_contract_type = context.ContractType ?? string.Empty;
            payload.selected_contract_agent_name = context.AgentName ?? string.Empty;
            payload.selected_contract_product_name = context.ProductName ?? string.Empty;
            payload.selected_contract_end_date = context.EndDate ?? string.Empty;
        }

        private static void CopySelectedContractContextToPayload(SelectedContractHistoryContext context, RandomEventConcludedEventPayload payload)
        {
            if (payload == null || context == null)
            {
                return;
            }

            payload.selected_contract_present = context.Present;
            payload.selected_contract_instance_id = context.InstanceId ?? string.Empty;
            payload.selected_contract_target_idol_id = context.TargetIdolId;
            payload.selected_contract_type = context.ContractType ?? string.Empty;
            payload.selected_contract_agent_name = context.AgentName ?? string.Empty;
            payload.selected_contract_product_name = context.ProductName ?? string.Empty;
            payload.selected_contract_end_date = context.EndDate ?? string.Empty;
        }

        /// <summary>
        /// Completes an SNS-only random event. Vanilla never calls ConcludeEvent for this
        /// branch, so this observer closes the history occurrence without mutating state.
        /// </summary>
        internal void CaptureRandomEventSnsOnlyTerminal(Event_Manager._activeEvent activeEvent)
        {
            if (activeEvent == null || activeEvent.data == null || !activeEvent.IsSNS())
            {
                return;
            }

            Event_Manager._randomEvent randomEvent = activeEvent.data;
            Event_Manager._randomEvent._reply reply = randomEvent.reply != null && randomEvent.reply.Count > CoreConstants.ZeroBasedListStartIndex
                ? randomEvent.reply[CoreConstants.ZeroBasedListStartIndex]
                : null;

            List<int> idolIds = ResolveDistinctRandomEventIdolIdentifiers(activeEvent.actors);
            long money = resources.Money();
            long fans = resources.GetFansTotal(null);
            int fame = (int)resources.Get(resources.type.fame, true);
            int buzz = (int)resources.Get(resources.type.buzz, true);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                if (randomEventTerminalCapturedByReference.Contains(activeEvent))
                {
                    return;
                }

                SelectedContractHistoryContext contractContext;
                if (!randomEventSelectedContractByReference.TryGetValue(activeEvent, out contractContext) || contractContext == null)
                {
                    contractContext = BuildSelectedContractHistoryContextLocked(randomEvent);
                }

                RandomEventConcludedEventPayload payload = new RandomEventConcludedEventPayload
                {
                    random_event_id = randomEvent.id ?? string.Empty,
                    random_event_occurrence_id = ResolveRandomEventOccurrenceIdLocked(activeEvent),
                    random_event_title = randomEvent.title ?? string.Empty,
                    random_event_state_before = "active",
                    random_event_state_after = ResolveRandomEventStateCode(activeEvent.state),
                    terminal_path = "sns_only",
                    reply_effects_applied = false,
                    resource_delta_known = false,
                    reply_index = reply != null ? CoreConstants.ZeroBasedListStartIndex : CoreConstants.InvalidIdValue,
                    reply_text = reply != null ? (reply.text ?? string.Empty) : string.Empty,
                    reply_description = reply != null ? (reply.description ?? string.Empty) : string.Empty,
                    reply_effect_count = reply != null && reply.Effects != null ? reply.Effects.Count : CoreConstants.ZeroBasedListStartIndex,
                    reply_effect_summary = reply != null ? BuildDialogueActionSummary(reply.Effects) : string.Empty,
                    reply_effect_entries = reply != null ? BuildDialogueActionEntries(reply.Effects) : new RandomEventReplyEffectEntry[0],
                    random_event_actor_id_list = BuildDelimitedIdentifierList(idolIds),
                    actors_summary = BuildRandomEventActorSummary(activeEvent.actors),
                    estimated_liability = CoreConstants.ZeroBasedListStartIndex,
                    money_before = money,
                    money_after = money,
                    money_delta = CoreConstants.ZeroBasedListStartIndex,
                    fans_before = fans,
                    fans_after = fans,
                    fans_delta = CoreConstants.ZeroBasedListStartIndex,
                    fame_before = fame,
                    fame_after = fame,
                    fame_delta = CoreConstants.ZeroBasedListStartIndex,
                    buzz_before = buzz,
                    buzz_after = buzz,
                    buzz_delta = CoreConstants.ZeroBasedListStartIndex,
                    event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
                };
                CopySelectedContractContextToPayload(contractContext, payload);

                EnqueueNarrativeEventForIdolsOrGlobalLocked(
                    staticVars.dateTime,
                    idolIds,
                    CoreConstants.EventEntityKindRandomEvent,
                    payload.random_event_id,
                    CoreConstants.EventTypeRandomEventConcluded,
                    CoreConstants.EventSourceEventManagerSnsOnlyTerminalPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                randomEventTerminalCapturedByReference.Add(activeEvent);
                randomEventSelectedContractByReference.Remove(activeEvent);
                randomEventOccurrenceByReference.Remove(activeEvent);
                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Begins passive collection of the exact reply variants Event_Popup will render.
        /// </summary>
        internal void BeginTemplatePopupPresentation(Event_Templates._active_template activeTemplate)
        {
            if (activeTemplate == null)
            {
                return;
            }

            lock (runtimeLock)
            {
                ResolveTemplateOccurrenceIdLocked(activeTemplate);
                templatePresentedRepliesByReference[activeTemplate] = new List<Event_Templates._template._reply>();
            }
        }

        /// <summary>
        /// Records the concrete reply variant selected by vanilla GetReplies(). This method
        /// does not call GetReplies itself, avoiding an extra random selection.
        /// </summary>
        internal void RecordTemplatePresentedReply(
            Event_Templates._template._reply reply,
            Event_Templates._active_template activeTemplate)
        {
            if (reply == null || activeTemplate == null)
            {
                return;
            }

            lock (runtimeLock)
            {
                if (templatePresentedCapturedByReference.Contains(activeTemplate))
                {
                    return;
                }

                List<Event_Templates._template._reply> replies;
                if (templatePresentedRepliesByReference.TryGetValue(activeTemplate, out replies) && replies != null)
                {
                    replies.Add(reply);
                }
            }
        }

        /// <summary>
        /// Emits one template-event presentation after Event_Popup has built its buttons.
        /// </summary>
        internal void CaptureTemplateEventPresented(Event_Templates._active_template activeTemplate)
        {
            if (activeTemplate == null || activeTemplate.Template == null)
            {
                return;
            }

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                if (templatePresentedCapturedByReference.Contains(activeTemplate))
                {
                    return;
                }

                List<Event_Templates._template._reply> replies;
                if (!templatePresentedRepliesByReference.TryGetValue(activeTemplate, out replies) || replies == null)
                {
                    replies = new List<Event_Templates._template._reply>();
                }

                string occurrenceId = ResolveTemplateOccurrenceIdLocked(activeTemplate);
                TemplateEventLifecyclePayload payload = BuildTemplateEventPayload(activeTemplate, replies, null, occurrenceId, "presented");
                List<int> idolIds = ResolveDistinctRandomEventIdolIdentifiers(activeTemplate.Actors);
                EnqueueNarrativeEventForIdolsOrGlobalLocked(
                    staticVars.dateTime,
                    idolIds,
                    CoreConstants.EventEntityKindTemplateEvent,
                    payload.template_id,
                    CoreConstants.EventTypeTemplateEventPresented,
                    CoreConstants.EventSourceTemplatePopupSetPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                templatePresentedCapturedByReference.Add(activeTemplate);
                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Takes a pre-action template snapshot so conclusion reflects the exact chosen reply
        /// and the semantic variables that existed before reply actions mutate them.
        /// </summary>
        internal TemplateEventConcludeSnapshot CreateTemplateEventConcludeSnapshot(Event_Templates._template._reply reply)
        {
            Event_Templates._active_template activeTemplate = Event_Templates.Active_Template;
            TemplateEventConcludeSnapshot snapshot = new TemplateEventConcludeSnapshot
            {
                ActiveTemplate = activeTemplate,
                Reply = reply
            };

            if (activeTemplate == null)
            {
                return snapshot;
            }

            lock (runtimeLock)
            {
                snapshot.OccurrenceId = ResolveTemplateOccurrenceIdLocked(activeTemplate);
                List<Event_Templates._template._reply> replies;
                if (templatePresentedRepliesByReference.TryGetValue(activeTemplate, out replies) && replies != null)
                {
                    snapshot.PresentedReplies = new List<Event_Templates._template._reply>(replies);
                }
                snapshot.VariablesBefore = BuildTemplateVariableEntries(activeTemplate.Variables);
            }
            return snapshot;
        }

        /// <summary>
        /// Emits one template terminal after vanilla applies the selected reply actions.
        /// </summary>
        internal void CaptureTemplateEventConcluded(TemplateEventConcludeSnapshot snapshot)
        {
            if (snapshot == null || snapshot.ActiveTemplate == null || snapshot.ActiveTemplate.Template == null || snapshot.Reply == null)
            {
                return;
            }

            Event_Templates._active_template activeTemplate = snapshot.ActiveTemplate;
            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                if (templateTerminalCapturedByReference.Contains(activeTemplate))
                {
                    return;
                }

                TemplateEventLifecyclePayload payload = BuildTemplateEventPayload(
                    activeTemplate,
                    snapshot.PresentedReplies,
                    snapshot.Reply,
                    snapshot.OccurrenceId,
                    "concluded");
                payload.variables_before = snapshot.VariablesBefore ?? new TemplateVariableEntry[0];
                payload.variables_after = BuildTemplateVariableEntries(activeTemplate.Variables);
                payload.reply_action_summary = BuildDialogueActionSummary(snapshot.Reply.Actions);
                payload.reply_action_count = snapshot.Reply.Actions != null ? snapshot.Reply.Actions.Count : CoreConstants.ZeroBasedListStartIndex;

                List<int> idolIds = ResolveDistinctRandomEventIdolIdentifiers(activeTemplate.Actors);
                EnqueueNarrativeEventForIdolsOrGlobalLocked(
                    staticVars.dateTime,
                    idolIds,
                    CoreConstants.EventEntityKindTemplateEvent,
                    payload.template_id,
                    CoreConstants.EventTypeTemplateEventConcluded,
                    CoreConstants.EventSourceTemplateConcludePatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));

                templateTerminalCapturedByReference.Add(activeTemplate);
                templatePresentedRepliesByReference.Remove(activeTemplate);
                FlushAfterCaptureLocked();
            }
        }

        private string ResolveTemplateOccurrenceIdLocked(Event_Templates._active_template activeTemplate)
        {
            string occurrenceId;
            if (activeTemplate != null && templateOccurrenceByReference.TryGetValue(activeTemplate, out occurrenceId) && !string.IsNullOrEmpty(occurrenceId))
            {
                return occurrenceId;
            }

            occurrenceId = string.Concat("te:", Guid.NewGuid().ToString("N"));
            if (activeTemplate != null)
            {
                templateOccurrenceByReference[activeTemplate] = occurrenceId;
            }
            return occurrenceId;
        }

        private static TemplateEventLifecyclePayload BuildTemplateEventPayload(
            Event_Templates._active_template activeTemplate,
            IList<Event_Templates._template._reply> presentedReplies,
            Event_Templates._template._reply selectedReply,
            string occurrenceId,
            string stage)
        {
            TemplateEventLifecyclePayload payload = new TemplateEventLifecyclePayload
            {
                template_event_occurrence_id = occurrenceId ?? string.Empty,
                template_id = activeTemplate != null && activeTemplate.Template != null ? (activeTemplate.Template.ID ?? string.Empty) : string.Empty,
                event_stage = stage ?? string.Empty,
                actors_summary = activeTemplate != null ? BuildRandomEventActorSummary(activeTemplate.Actors) : string.Empty,
                selected_part_values = BuildTemplatePartEntries(activeTemplate != null ? activeTemplate.Parts : null),
                presented_replies = BuildTemplateReplyEntries(presentedReplies),
                semantic_variables = activeTemplate != null ? BuildTemplateVariableEntries(activeTemplate.Variables) : new TemplateVariableEntry[0],
                selected_reply_id = selectedReply != null ? (selectedReply.ID ?? string.Empty) : string.Empty,
                selected_reply_text = selectedReply != null ? (selectedReply.Text ?? string.Empty) : string.Empty,
                selected_reply_index = ResolveTemplatePresentedReplyIndex(presentedReplies, selectedReply),
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };
            List<int> idolIds = activeTemplate != null
                ? ResolveDistinctRandomEventIdolIdentifiers(activeTemplate.Actors)
                : new List<int>();
            payload.template_event_actor_id_list = BuildDelimitedIdentifierList(idolIds);
            return payload;
        }

        private static TemplatePartValueEntry[] BuildTemplatePartEntries(IList<Event_Templates._template._part._value> parts)
        {
            if (parts == null || parts.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return new TemplatePartValueEntry[0];
            }

            TemplatePartValueEntry[] entries = new TemplatePartValueEntry[parts.Count];
            for (int index = CoreConstants.ZeroBasedListStartIndex; index < parts.Count; index++)
            {
                Event_Templates._template._part._value value = parts[index];
                entries[index] = new TemplatePartValueEntry
                {
                    part_id = value != null && value.Parent != null ? (value.Parent.ID ?? string.Empty) : string.Empty,
                    value_id = value != null ? (value.ID ?? string.Empty) : string.Empty,
                    value_text = value != null ? (value.Text ?? string.Empty) : string.Empty
                };
            }
            return entries;
        }

        private static TemplateReplyEntry[] BuildTemplateReplyEntries(IList<Event_Templates._template._reply> replies)
        {
            if (replies == null || replies.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return new TemplateReplyEntry[0];
            }

            TemplateReplyEntry[] entries = new TemplateReplyEntry[replies.Count];
            for (int index = CoreConstants.ZeroBasedListStartIndex; index < replies.Count; index++)
            {
                Event_Templates._template._reply reply = replies[index];
                entries[index] = new TemplateReplyEntry
                {
                    reply_id = reply != null ? (reply.ID ?? string.Empty) : string.Empty,
                    reply_text = reply != null ? (reply.Text ?? string.Empty) : string.Empty,
                    action_count = reply != null && reply.Actions != null ? reply.Actions.Count : CoreConstants.ZeroBasedListStartIndex,
                    action_summary = reply != null ? BuildDialogueActionSummary(reply.Actions) : string.Empty
                };
            }
            return entries;
        }

        private static TemplateVariableEntry[] BuildTemplateVariableEntries(IDictionary<string, int> variables)
        {
            if (variables == null || variables.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return new TemplateVariableEntry[0];
            }

            List<string> keys = new List<string>(variables.Keys);
            keys.Sort(StringComparer.Ordinal);
            TemplateVariableEntry[] entries = new TemplateVariableEntry[keys.Count];
            for (int index = CoreConstants.ZeroBasedListStartIndex; index < keys.Count; index++)
            {
                string key = keys[index] ?? string.Empty;
                int value;
                variables.TryGetValue(key, out value);
                entries[index] = new TemplateVariableEntry { name = key, value = value };
            }
            return entries;
        }

        private static int ResolveTemplatePresentedReplyIndex(
            IList<Event_Templates._template._reply> replies,
            Event_Templates._template._reply selectedReply)
        {
            if (replies == null || selectedReply == null)
            {
                return CoreConstants.InvalidIdValue;
            }
            for (int index = CoreConstants.ZeroBasedListStartIndex; index < replies.Count; index++)
            {
                if (ReferenceEquals(replies[index], selectedReply))
                {
                    return index;
                }
            }
            return CoreConstants.InvalidIdValue;
        }

        /// <summary>
        /// Captures player-forced-breakup intent before SNLF/vanilla may mutate either side.
        /// No repair is performed here.
        /// </summary>
        internal PlayerForcedBreakupSnapshot CreatePlayerForcedBreakupSnapshot(data_girls.girls target)
        {
            PlayerForcedBreakupSnapshot snapshot = new PlayerForcedBreakupSnapshot
            {
                Target = target,
                TargetIdolId = target != null ? target.id : CoreConstants.InvalidIdValue,
                InfluenceBefore = ResolvePlayerRelationshipPoints(Relationships_Player._type.Influence, target)
            };
            if (target == null || target.DatingData == null)
            {
                return snapshot;
            }

            snapshot.TargetPartnerStatusBefore = CoreEnumNameMapping.ToIdolDatingPartnerStatusCode(target.DatingData.Partner_Status);
            snapshot.PartnerKindBefore = ResolveIdolPartnerKind(target.DatingData.Partner_Status);

            if (target.DatingData.Partner_Status != data_girls.girls._dating_data._partner_status.taken_idol)
            {
                return snapshot;
            }

            Relationships._relationship resolved = null;
            data_girls.girls resolvedPartner = null;
            int candidateCount = CoreConstants.ZeroBasedListStartIndex;
            if (Relationships.RelationshipsData != null)
            {
                for (int index = CoreConstants.ZeroBasedListStartIndex; index < Relationships.RelationshipsData.Count; index++)
                {
                    Relationships._relationship relationship = Relationships.RelationshipsData[index];
                    if (relationship == null || !relationship.Dating || relationship.Girls == null || !relationship.Girls.Contains(target))
                    {
                        continue;
                    }

                    data_girls.girls other = null;
                    for (int girlIndex = CoreConstants.ZeroBasedListStartIndex; girlIndex < relationship.Girls.Count; girlIndex++)
                    {
                        data_girls.girls girl = relationship.Girls[girlIndex];
                        if (girl != null && !ReferenceEquals(girl, target))
                        {
                            other = girl;
                            break;
                        }
                    }

                    if (other == null)
                    {
                        continue;
                    }
                    candidateCount++;
                    resolved = relationship;
                    resolvedPartner = other;
                }
            }

            if (candidateCount == CoreConstants.MinimumNonEmptyCollectionCount && resolved != null && resolvedPartner != null)
            {
                snapshot.Relationship = resolved;
                snapshot.ResolvedPartner = resolvedPartner;
                snapshot.PartnerIdolId = resolvedPartner.id;
                snapshot.RelationshipWasDatingBefore = resolved.Dating;
                snapshot.RelationshipStatusBefore = CoreEnumNameMapping.ToRelationshipStatusCode(resolved.Status);
                snapshot.RelationshipDynamicBefore = CoreEnumNameMapping.ToRelationshipDynamicCode(resolved.Dynamic);
                snapshot.PartnerPartnerStatusBefore = resolvedPartner.DatingData != null
                    ? CoreEnumNameMapping.ToIdolDatingPartnerStatusCode(resolvedPartner.DatingData.Partner_Status)
                    : CoreConstants.StatusCodeUnknown;
            }

            return snapshot;
        }

        /// <summary>
        /// Emits exactly one semantic action row after the force-breakup button completes.
        /// The payload reports the observed post-state, including an unrepaired vanilla state
        /// when Save n Load Fixes is absent.
        /// </summary>
        internal void CapturePlayerForcedBreakup(PlayerForcedBreakupSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Target == null || snapshot.TargetIdolId < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            data_girls.girls target = snapshot.Target;
            int influenceAfter = ResolvePlayerRelationshipPoints(Relationships_Player._type.Influence, target);
            PlayerForcedBreakupPayload payload = new PlayerForcedBreakupPayload
            {
                target_idol_id = snapshot.TargetIdolId,
                partner_kind_before = snapshot.PartnerKindBefore ?? CoreConstants.StatusCodeUnknown,
                partner_idol_id = snapshot.PartnerIdolId,
                target_partner_status_before = snapshot.TargetPartnerStatusBefore ?? CoreConstants.StatusCodeUnknown,
                target_partner_status_after = target.DatingData != null
                    ? CoreEnumNameMapping.ToIdolDatingPartnerStatusCode(target.DatingData.Partner_Status)
                    : CoreConstants.StatusCodeUnknown,
                partner_partner_status_before = snapshot.PartnerPartnerStatusBefore ?? CoreConstants.StatusCodeUnknown,
                partner_partner_status_after = snapshot.ResolvedPartner != null && snapshot.ResolvedPartner.DatingData != null
                    ? CoreEnumNameMapping.ToIdolDatingPartnerStatusCode(snapshot.ResolvedPartner.DatingData.Partner_Status)
                    : CoreConstants.StatusCodeUnknown,
                relationship_was_dating_before = snapshot.RelationshipWasDatingBefore,
                relationship_is_dating_after = snapshot.Relationship != null && snapshot.Relationship.Dating,
                relationship_status_before = snapshot.RelationshipStatusBefore ?? CoreConstants.StatusCodeUnknown,
                relationship_status_after = snapshot.Relationship != null
                    ? CoreEnumNameMapping.ToRelationshipStatusCode(snapshot.Relationship.Status)
                    : CoreConstants.StatusCodeUnknown,
                relationship_dynamic_before = snapshot.RelationshipDynamicBefore ?? CoreConstants.StatusCodeUnknown,
                relationship_dynamic_after = snapshot.Relationship != null
                    ? CoreEnumNameMapping.ToRelationshipDynamicCode(snapshot.Relationship.Dynamic)
                    : CoreConstants.StatusCodeUnknown,
                influence_before = snapshot.InfluenceBefore,
                influence_after = influenceAfter,
                influence_delta = influenceAfter - snapshot.InfluenceBefore,
                influence_cost_points = Math.Max(CoreConstants.ZeroBasedListStartIndex, snapshot.InfluenceBefore - influenceAfter),
                event_date = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime)
            };

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
                    snapshot.TargetIdolId,
                    CoreConstants.EventEntityKindDatingRelationship,
                    snapshot.TargetIdolId.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypePlayerForcedBreakup,
                    CoreConstants.EventSourceDatePopupForceBreakupPatch,
                    CoreJsonUtility.SerializeObjectPayload(payload));
                FlushAfterCaptureLocked();
            }
        }

        private static string ResolveIdolPartnerKind(data_girls.girls._dating_data._partner_status status)
        {
            switch (status)
            {
                case data_girls.girls._dating_data._partner_status.taken_idol:
                    return "idol";
                case data_girls.girls._dating_data._partner_status.taken_outside_bf:
                case data_girls.girls._dating_data._partner_status.taken_outside_gf:
                    return "outside";
                case data_girls.girls._dating_data._partner_status.taken_player:
                    return "player";
                case data_girls.girls._dating_data._partner_status.free:
                    return "free";
                default:
                    return CoreConstants.StatusCodeUnknown;
            }
        }
    }

    internal sealed class SelectedContractHistoryContext
    {
        internal bool Present;
        internal string InstanceId = string.Empty;
        internal int TargetIdolId = CoreConstants.InvalidIdValue;
        internal string ContractType = string.Empty;
        internal string AgentName = string.Empty;
        internal string ProductName = string.Empty;
        internal string EndDate = string.Empty;
    }

    [Serializable]
    internal sealed class TemplatePartValueEntry
    {
        public string part_id = string.Empty;
        public string value_id = string.Empty;
        public string value_text = string.Empty;
    }

    [Serializable]
    internal sealed class TemplateReplyEntry
    {
        public string reply_id = string.Empty;
        public string reply_text = string.Empty;
        public int action_count;
        public string action_summary = string.Empty;
    }

    [Serializable]
    internal sealed class TemplateVariableEntry
    {
        public string name = string.Empty;
        public int value;
    }

    [Serializable]
    internal sealed class TemplateEventLifecyclePayload
    {
        public string template_event_occurrence_id = string.Empty;
        public string template_id = string.Empty;
        public string event_stage = string.Empty;
        public string template_event_actor_id_list = string.Empty;
        public string actors_summary = string.Empty;
        public TemplatePartValueEntry[] selected_part_values;
        public TemplateReplyEntry[] presented_replies;
        public TemplateVariableEntry[] semantic_variables;
        public string selected_reply_id = string.Empty;
        public string selected_reply_text = string.Empty;
        public int selected_reply_index = CoreConstants.InvalidIdValue;
        public int reply_action_count;
        public string reply_action_summary = string.Empty;
        public TemplateVariableEntry[] variables_before;
        public TemplateVariableEntry[] variables_after;
        public string event_date = string.Empty;
    }

    internal sealed class TemplateEventConcludeSnapshot
    {
        internal Event_Templates._active_template ActiveTemplate;
        internal Event_Templates._template._reply Reply;
        internal string OccurrenceId = string.Empty;
        internal List<Event_Templates._template._reply> PresentedReplies = new List<Event_Templates._template._reply>();
        internal TemplateVariableEntry[] VariablesBefore = new TemplateVariableEntry[0];
    }

    internal sealed class PlayerForcedBreakupSnapshot
    {
        internal data_girls.girls Target;
        internal data_girls.girls ResolvedPartner;
        internal Relationships._relationship Relationship;
        internal int TargetIdolId = CoreConstants.InvalidIdValue;
        internal int PartnerIdolId = CoreConstants.InvalidIdValue;
        internal string PartnerKindBefore = CoreConstants.StatusCodeUnknown;
        internal string TargetPartnerStatusBefore = CoreConstants.StatusCodeUnknown;
        internal string PartnerPartnerStatusBefore = CoreConstants.StatusCodeUnknown;
        internal bool RelationshipWasDatingBefore;
        internal string RelationshipStatusBefore = CoreConstants.StatusCodeUnknown;
        internal string RelationshipDynamicBefore = CoreConstants.StatusCodeUnknown;
        internal int InfluenceBefore;
    }

    [Serializable]
    internal sealed class PlayerForcedBreakupPayload
    {
        public int target_idol_id = CoreConstants.InvalidIdValue;
        public string partner_kind_before = string.Empty;
        public int partner_idol_id = CoreConstants.InvalidIdValue;
        public string target_partner_status_before = string.Empty;
        public string target_partner_status_after = string.Empty;
        public string partner_partner_status_before = string.Empty;
        public string partner_partner_status_after = string.Empty;
        public bool relationship_was_dating_before;
        public bool relationship_is_dating_after;
        public string relationship_status_before = string.Empty;
        public string relationship_status_after = string.Empty;
        public string relationship_dynamic_before = string.Empty;
        public string relationship_dynamic_after = string.Empty;
        public int influence_before;
        public int influence_after;
        public int influence_delta;
        public int influence_cost_points;
        public string event_date = string.Empty;
    }
}
