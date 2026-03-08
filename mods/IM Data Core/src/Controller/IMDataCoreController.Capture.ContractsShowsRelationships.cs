using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace IMDataCore
{
    internal sealed partial class IMDataCoreController
    {
        /// <summary>
        /// Captures one accepted business proposal as a contract acceptance event.
        /// </summary>
        internal void CaptureContractAccepted(ContractAcceptedSnapshot snapshot)
        {
            if (snapshot == null || snapshot.AcceptedProposal == null)
            {
                return;
            }

            business._proposal acceptedProposal = snapshot.AcceptedProposal;
            DateTime contractStartDate = snapshot.AcceptedDate;
            DateTime contractEndDate = acceptedProposal.duration > CoreConstants.ZeroBasedListStartIndex
                ? contractStartDate.AddMonths(acceptedProposal.duration)
                : contractStartDate;
            string contractTypeCode = CoreEnumNameMapping.ToBusinessContractTypeCode(acceptedProposal.type);

            List<int> targetIdolIdentifiers = snapshot.TargetIdolIdentifiers ?? new List<int>();
            if (targetIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                targetIdolIdentifiers = ResolveProposalTargetIdolIdentifiers(acceptedProposal);
            }

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                if (targetIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    ContractLifecyclePayload payload = BuildContractLifecyclePayloadFromProposal(
                        acceptedProposal,
                        CoreConstants.InvalidIdValue,
                        contractStartDate,
                        contractEndDate);

                    EnqueueEventRecordLocked(
                        gameDate,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindContract,
                        BuildContractEntityIdentifier(CoreConstants.InvalidIdValue, contractTypeCode, contractEndDate),
                        CoreConstants.EventTypeContractAccepted,
                        CoreConstants.EventSourceContractAcceptPatch,
                        CoreJsonUtility.SerializeContractLifecyclePayload(payload));
                }
                else
                {
                    for (int idolIndex = CoreConstants.ZeroBasedListStartIndex; idolIndex < targetIdolIdentifiers.Count; idolIndex++)
                    {
                        int idolId = targetIdolIdentifiers[idolIndex];
                        ContractLifecyclePayload payload = BuildContractLifecyclePayloadFromProposal(
                            acceptedProposal,
                            idolId,
                            contractStartDate,
                            contractEndDate);

                        EnqueueEventRecordLocked(
                            gameDate,
                            idolId,
                            CoreConstants.EventEntityKindContract,
                            BuildContractEntityIdentifier(idolId, contractTypeCode, contractEndDate),
                            CoreConstants.EventTypeContractAccepted,
                            CoreConstants.EventSourceContractAcceptPatch,
                            CoreJsonUtility.SerializeContractLifecyclePayload(payload));
                    }
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one newly-activated business contract.
        /// </summary>
        internal void CaptureContractActivated(business.active_proposal activeContract, business._proposal sourceProposal)
        {
            if (activeContract == null)
            {
                return;
            }

            DateTime gameDate = staticVars.dateTime;
            DateTime contractEndDate = activeContract.EndDate;
            string contractTypeCode = CoreEnumNameMapping.ToBusinessContractTypeCode(activeContract.Type);
            List<int> targetIdolIdentifiers = ResolveContractTargetIdolIdentifiers(activeContract, sourceProposal);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                if (targetIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    ContractLifecyclePayload payload = BuildContractLifecyclePayloadFromActiveContract(
                        activeContract,
                        CoreConstants.InvalidIdValue,
                        false,
                        CoreConstants.ZeroLongValue,
                        string.Empty);

                    string contractEntityIdentifier = BuildContractEntityIdentifier(CoreConstants.InvalidIdValue, contractTypeCode, contractEndDate);
                    EnqueueEventRecordLocked(
                        gameDate,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindContract,
                        contractEntityIdentifier,
                        CoreConstants.EventTypeContractActivated,
                        CoreConstants.EventSourceContractActivationPatch,
                        CoreJsonUtility.SerializeContractLifecyclePayload(payload));
                    EnqueueEventRecordLocked(
                        gameDate,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindContract,
                        contractEntityIdentifier,
                        CoreConstants.EventTypeContractWindowOpened,
                        CoreConstants.EventSourceContractActivationPatch,
                        CoreJsonUtility.SerializeContractLifecyclePayload(payload));
                }
                else
                {
                    for (int idolIndex = CoreConstants.ZeroBasedListStartIndex; idolIndex < targetIdolIdentifiers.Count; idolIndex++)
                    {
                        int idolId = targetIdolIdentifiers[idolIndex];
                        ContractLifecyclePayload payload = BuildContractLifecyclePayloadFromActiveContract(
                            activeContract,
                            idolId,
                            false,
                            CoreConstants.ZeroLongValue,
                            string.Empty);

                        string contractEntityIdentifier = BuildContractEntityIdentifier(idolId, contractTypeCode, contractEndDate);
                        EnqueueEventRecordLocked(
                            gameDate,
                            idolId,
                            CoreConstants.EventEntityKindContract,
                            contractEntityIdentifier,
                            CoreConstants.EventTypeContractActivated,
                            CoreConstants.EventSourceContractActivationPatch,
                            CoreJsonUtility.SerializeContractLifecyclePayload(payload));
                        EnqueueEventRecordLocked(
                            gameDate,
                            idolId,
                            CoreConstants.EventEntityKindContract,
                            contractEntityIdentifier,
                            CoreConstants.EventTypeContractWindowOpened,
                            CoreConstants.EventSourceContractActivationPatch,
                            CoreJsonUtility.SerializeContractLifecyclePayload(payload));
                    }
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one explicit contract cancellation event.
        /// </summary>
        internal void CaptureContractCancelled(business.active_proposal activeContract, bool damagesApplied)
        {
            if (activeContract == null)
            {
                return;
            }

            int idolId = activeContract.Girl != null ? activeContract.Girl.id : CoreConstants.InvalidIdValue;
            DateTime gameDate = staticVars.dateTime;
            string contractTypeCode = CoreEnumNameMapping.ToBusinessContractTypeCode(activeContract.Type);
            DateTime contractEndDate = activeContract.EndDate;
            ContractLifecyclePayload payload = BuildContractLifecyclePayloadFromActiveContract(
                activeContract,
                idolId,
                damagesApplied,
                CoreConstants.ZeroLongValue,
                string.Empty);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                string contractEntityIdentifier = BuildContractEntityIdentifier(idolId, contractTypeCode, contractEndDate);
                EnqueueEventRecordLocked(
                    gameDate,
                    idolId,
                    CoreConstants.EventEntityKindContract,
                    contractEntityIdentifier,
                    CoreConstants.EventTypeContractCancelled,
                    CoreConstants.EventSourceContractCancellationPatch,
                    CoreJsonUtility.SerializeContractLifecyclePayload(payload));
                EnqueueEventRecordLocked(
                    gameDate,
                    idolId,
                    CoreConstants.EventEntityKindContract,
                    contractEntityIdentifier,
                    CoreConstants.EventTypeContractCanceled,
                    CoreConstants.EventSourceContractCancellationPatch,
                    CoreJsonUtility.SerializeContractLifecyclePayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures weekly contract earnings accrual events.
        /// </summary>
        internal void CaptureContractWeeklyEarnings(business businessSystem)
        {
            CaptureContractWeeklyAccrual(
                businessSystem,
                CoreConstants.EventTypeContractWeeklyEarningsApplied,
                CoreConstants.EventSourceContractWeeklyEarningsPatch,
                CoreConstants.ContractWeeklyAccrualActionEarningsApplied,
                CoreConstants.ZeroBasedListStartIndex);
        }

        /// <summary>
        /// Captures weekly contract fan/fame/training accrual events.
        /// </summary>
        internal void CaptureContractWeeklyBenefits(business businessSystem)
        {
            CaptureContractWeeklyAccrual(
                businessSystem,
                CoreConstants.EventTypeContractWeeklyBenefitsApplied,
                CoreConstants.EventSourceContractWeeklyBenefitsPatch,
                CoreConstants.ContractWeeklyAccrualActionBenefitsApplied,
                CoreConstants.ContractWeeklyAccrualTrainingPointsPerTick);
        }

        /// <summary>
        /// Captures one weekly contract accrual event per active contract row.
        /// </summary>
        private void CaptureContractWeeklyAccrual(
            business businessSystem,
            string eventTypeCode,
            string sourcePatchCode,
            string weeklyActionCode,
            int weeklyTrainingPoints)
        {
            if (businessSystem == null
                || businessSystem.ActiveProposals == null
                || businessSystem.ActiveProposals.Count < CoreConstants.MinimumNonEmptyCollectionCount)
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

                DateTime gameDate = staticVars.dateTime;
                for (int proposalIndex = CoreConstants.ZeroBasedListStartIndex; proposalIndex < businessSystem.ActiveProposals.Count; proposalIndex++)
                {
                    business.active_proposal activeContract = businessSystem.ActiveProposals[proposalIndex];
                    if (activeContract == null)
                    {
                        continue;
                    }

                    int idolId = activeContract.Girl != null ? activeContract.Girl.id : CoreConstants.InvalidIdValue;
                    string contractTypeCode = CoreEnumNameMapping.ToBusinessContractTypeCode(activeContract.Type);
                    ContractWeeklyAccrualPayload payload = BuildContractWeeklyAccrualPayload(
                        activeContract,
                        idolId,
                        weeklyActionCode,
                        weeklyTrainingPoints);

                    EnqueueEventRecordLocked(
                        gameDate,
                        idolId,
                        CoreConstants.EventEntityKindContract,
                        BuildContractEntityIdentifier(idolId, contractTypeCode, activeContract.EndDate),
                        eventTypeCode,
                        sourcePatchCode,
                        CoreJsonUtility.SerializeContractWeeklyAccrualPayload(payload));
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Creates a snapshot of contracts that will naturally expire in the current `CheckActiveProposals` pass.
        /// </summary>
        internal List<business.active_proposal> CreateContractsNaturalCompletionSnapshot(business businessSystem)
        {
            List<business.active_proposal> expiringContracts = new List<business.active_proposal>();
            if (businessSystem == null || businessSystem.ActiveProposals == null)
            {
                return expiringContracts;
            }

            DateTime gameDate = staticVars.dateTime;
            for (int proposalIndex = CoreConstants.ZeroBasedListStartIndex; proposalIndex < businessSystem.ActiveProposals.Count; proposalIndex++)
            {
                business.active_proposal activeContract = businessSystem.ActiveProposals[proposalIndex];
                if (activeContract == null)
                {
                    continue;
                }

                if (activeContract.EndDate < gameDate)
                {
                    expiringContracts.Add(activeContract);
                }
            }

            return expiringContracts;
        }

        /// <summary>
        /// Captures one contract-finished lifecycle event for each naturally expired active proposal.
        /// </summary>
        internal void CaptureContractsNaturallyFinished(List<business.active_proposal> naturallyFinishedContracts)
        {
            if (naturallyFinishedContracts == null || naturallyFinishedContracts.Count < CoreConstants.MinimumNonEmptyCollectionCount)
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

                DateTime gameDate = staticVars.dateTime;
                for (int contractIndex = CoreConstants.ZeroBasedListStartIndex; contractIndex < naturallyFinishedContracts.Count; contractIndex++)
                {
                    business.active_proposal activeContract = naturallyFinishedContracts[contractIndex];
                    if (activeContract == null)
                    {
                        continue;
                    }

                    int idolId = activeContract.Girl != null ? activeContract.Girl.id : CoreConstants.InvalidIdValue;
                    string contractTypeCode = CoreEnumNameMapping.ToBusinessContractTypeCode(activeContract.Type);
                    ContractLifecyclePayload payload = BuildContractLifecyclePayloadFromActiveContract(
                        activeContract,
                        idolId,
                        false,
                        CoreConstants.ZeroLongValue,
                        string.Empty);

                    EnqueueEventRecordLocked(
                        gameDate,
                        idolId,
                        CoreConstants.EventEntityKindContract,
                        BuildContractEntityIdentifier(idolId, contractTypeCode, activeContract.EndDate),
                        CoreConstants.EventTypeContractFinished,
                        CoreConstants.EventSourceContractNaturalCompletionPatch,
                        CoreJsonUtility.SerializeContractLifecyclePayload(payload));
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Computes total liability that will be removed by `BreakContracts(data_girls.girls)`.
        /// </summary>
        internal long CreateContractBreakLiabilitySnapshotForIdol(business businessSystem, data_girls.girls idol)
        {
            if (businessSystem == null || idol == null || businessSystem.ActiveProposals == null)
            {
                return CoreConstants.ZeroLongValue;
            }

            long totalLiability = CoreConstants.ZeroLongValue;
            for (int i = CoreConstants.ZeroBasedListStartIndex; i < businessSystem.ActiveProposals.Count; i++)
            {
                business.active_proposal activeProposal = businessSystem.ActiveProposals[i];
                if (activeProposal != null && activeProposal.Girl == idol)
                {
                    totalLiability += activeProposal.Liability;
                }
            }

            return totalLiability;
        }

        /// <summary>
        /// Computes per-idol liability that will be removed by `BreakContracts(List<actor>)`.
        /// </summary>
        internal Dictionary<int, long> CreateContractBreakLiabilitySnapshotForActors(
            business businessSystem,
            List<Event_Manager._activeEvent._actor> actors)
        {
            Dictionary<int, long> totalLiabilityByIdolId = new Dictionary<int, long>();
            if (businessSystem == null || actors == null || actors.Count < CoreConstants.MinimumNonEmptyCollectionCount || businessSystem.ActiveProposals == null)
            {
                return totalLiabilityByIdolId;
            }

            HashSet<data_girls.girls> targetIdols = new HashSet<data_girls.girls>();
            for (int actorIndex = CoreConstants.ZeroBasedListStartIndex; actorIndex < actors.Count; actorIndex++)
            {
                Event_Manager._activeEvent._actor actor = actors[actorIndex];
                if (actor != null && actor.girl != null)
                {
                    targetIdols.Add(actor.girl);
                }
            }

            if (targetIdols.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return totalLiabilityByIdolId;
            }

            for (int proposalIndex = CoreConstants.ZeroBasedListStartIndex; proposalIndex < businessSystem.ActiveProposals.Count; proposalIndex++)
            {
                business.active_proposal activeProposal = businessSystem.ActiveProposals[proposalIndex];
                if (activeProposal == null || activeProposal.Girl == null || !targetIdols.Contains(activeProposal.Girl))
                {
                    continue;
                }

                int idolId = activeProposal.Girl.id;
                if (idolId < CoreConstants.MinimumValidIdolIdentifier)
                {
                    continue;
                }

                long previousLiability;
                totalLiabilityByIdolId.TryGetValue(idolId, out previousLiability);
                totalLiabilityByIdolId[idolId] = previousLiability + activeProposal.Liability;
            }

            return totalLiabilityByIdolId;
        }

        /// <summary>
        /// Captures one idol-scoped contract-break event.
        /// </summary>
        internal void CaptureContractBrokenForIdol(data_girls.girls idol, long totalBrokenLiability, string breakContextCode, string sourcePatchCode)
        {
            if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier || totalBrokenLiability <= CoreConstants.ZeroLongValue)
            {
                return;
            }

            ContractLifecyclePayload payload = new ContractLifecyclePayload
            {
                IdolId = idol.id,
                ContractType = CoreConstants.StatusCodeUnknown,
                ContractSkill = CoreConstants.StatusCodeUnknown,
                IsGroupContract = false,
                WeeklyPayment = CoreConstants.ZeroBasedListStartIndex,
                WeeklyBuzz = CoreConstants.ZeroBasedListStartIndex,
                WeeklyFame = CoreConstants.ZeroBasedListStartIndex,
                WeeklyFans = CoreConstants.ZeroBasedListStartIndex,
                WeeklyStamina = CoreConstants.ZeroBasedListStartIndex,
                ContractLiability = totalBrokenLiability,
                AgentName = string.Empty,
                ProductName = string.Empty,
                ContractEndDate = string.Empty,
                DamagesApplied = true,
                TotalBrokenLiability = totalBrokenLiability,
                ContractBreakContext = breakContextCode ?? string.Empty
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    idol.id,
                    CoreConstants.EventEntityKindContract,
                    idol.id.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypeContractBroken,
                    sourcePatchCode ?? CoreConstants.EventSourceContractBreakSingleIdolPatch,
                    CoreJsonUtility.SerializeContractLifecyclePayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures multiple idol-scoped contract-break events in one pass.
        /// </summary>
        internal void CaptureContractBrokenForActorBatch(
            Dictionary<int, long> idolLiabilityById,
            string breakContextCode,
            string sourcePatchCode)
        {
            if (idolLiabilityById == null || idolLiabilityById.Count < CoreConstants.MinimumNonEmptyCollectionCount)
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

                DateTime gameDate = staticVars.dateTime;
                foreach (KeyValuePair<int, long> liabilityByIdEntry in idolLiabilityById)
                {
                    if (liabilityByIdEntry.Key < CoreConstants.MinimumValidIdolIdentifier || liabilityByIdEntry.Value <= CoreConstants.ZeroLongValue)
                    {
                        continue;
                    }

                    ContractLifecyclePayload payload = new ContractLifecyclePayload
                    {
                        IdolId = liabilityByIdEntry.Key,
                        ContractType = CoreConstants.StatusCodeUnknown,
                        ContractSkill = CoreConstants.StatusCodeUnknown,
                        IsGroupContract = false,
                        WeeklyPayment = CoreConstants.ZeroBasedListStartIndex,
                        WeeklyBuzz = CoreConstants.ZeroBasedListStartIndex,
                        WeeklyFame = CoreConstants.ZeroBasedListStartIndex,
                        WeeklyFans = CoreConstants.ZeroBasedListStartIndex,
                        WeeklyStamina = CoreConstants.ZeroBasedListStartIndex,
                        ContractLiability = liabilityByIdEntry.Value,
                        AgentName = string.Empty,
                        ProductName = string.Empty,
                        ContractEndDate = string.Empty,
                        DamagesApplied = true,
                        TotalBrokenLiability = liabilityByIdEntry.Value,
                        ContractBreakContext = breakContextCode ?? string.Empty
                    };

                    EnqueueEventRecordLocked(
                        gameDate,
                        liabilityByIdEntry.Key,
                        CoreConstants.EventEntityKindContract,
                        liabilityByIdEntry.Key.ToString(CultureInfo.InvariantCulture),
                        CoreConstants.EventTypeContractBroken,
                        sourcePatchCode ?? CoreConstants.EventSourceContractBreakEventActorsPatch,
                        CoreJsonUtility.SerializeContractLifecyclePayload(payload));
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one show-creation lifecycle event.
        /// </summary>
        internal void CaptureShowCreated(Shows._show show)
        {
            CaptureShowLifecycleEvent(
                show,
                CoreConstants.EventTypeShowCreated,
                CoreConstants.EventSourceShowAddNewPatch,
                CoreConstants.ShowLifecycleActionCreated);
        }

        /// <summary>
        /// Captures one show-release lifecycle event.
        /// </summary>
        internal void CaptureShowReleased(Shows._show show)
        {
            CaptureShowLifecycleEvent(
                show,
                CoreConstants.EventTypeShowReleased,
                CoreConstants.EventSourceShowReleasePatch,
                CoreConstants.ShowLifecycleActionReleased);
        }

        /// <summary>
        /// Captures one show-cancel lifecycle event.
        /// </summary>
        internal void CaptureShowCancelled(Shows._show show)
        {
            CaptureShowCancelled(show, CoreConstants.EventSourceShowCancelPatch);
        }

        /// <summary>
        /// Captures one show-cancel lifecycle event using the specified source patch code.
        /// </summary>
        internal void CaptureShowCancelled(Shows._show show, string sourcePatchCode)
        {
            CaptureShowLifecycleEvent(
                show,
                CoreConstants.EventTypeShowCancelled,
                sourcePatchCode,
                CoreConstants.ShowLifecycleActionCancelled);
        }

        /// <summary>
        /// Captures one show-relaunch-start lifecycle event.
        /// </summary>
        internal void CaptureShowRelaunchStarted(Shows._show show)
        {
            CaptureShowLifecycleEvent(
                show,
                CoreConstants.EventTypeShowRelaunchStarted,
                CoreConstants.EventSourceShowRelaunchStartPatch,
                CoreConstants.ShowLifecycleActionRelaunchStarted);
        }

        /// <summary>
        /// Captures one show-relaunch-finish lifecycle event.
        /// </summary>
        internal void CaptureShowRelaunchFinished(Shows._show show)
        {
            CaptureShowLifecycleEvent(
                show,
                CoreConstants.EventTypeShowRelaunchFinished,
                CoreConstants.EventSourceShowRelaunchFinishPatch,
                CoreConstants.ShowLifecycleActionRelaunchFinished);
        }

        /// <summary>
        /// Captures one show lifecycle event and emits one row per cast idol when available.
        /// </summary>
        private void CaptureShowLifecycleEvent(
            Shows._show show,
            string lifecycleEventTypeCode,
            string sourcePatchCode,
            string lifecycleActionCode)
        {
            if (show == null)
            {
                return;
            }

            List<int> castIdolIdentifiers = ResolveDistinctShowCastIdolIdentifiers(show);
            ShowLifecyclePayload payload = BuildShowLifecyclePayload(show, castIdolIdentifiers, lifecycleActionCode);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                string showEntityIdentifier = show.id.ToString(CultureInfo.InvariantCulture);
                string idempotencyKey = BuildShowLifecycleIdempotencyKey(lifecycleEventTypeCode, showEntityIdentifier);
                if (!TryReserveIdempotencyKeyLocked(gameDate, idempotencyKey))
                {
                    return;
                }

                string eventPayloadJson = CoreJsonUtility.SerializeShowLifecyclePayload(payload);

                if (castIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    EnqueueEventRecordLocked(
                        gameDate,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindShow,
                        showEntityIdentifier,
                        lifecycleEventTypeCode,
                        sourcePatchCode,
                        eventPayloadJson);
                }
                else
                {
                    for (int castIndex = CoreConstants.ZeroBasedListStartIndex; castIndex < castIdolIdentifiers.Count; castIndex++)
                    {
                        EnqueueEventRecordLocked(
                            gameDate,
                            castIdolIdentifiers[castIndex],
                            CoreConstants.EventEntityKindShow,
                            showEntityIdentifier,
                            lifecycleEventTypeCode,
                            sourcePatchCode,
                            eventPayloadJson);
                    }
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures show status transitions for each cast idol.
        /// </summary>
        internal void CaptureShowStatusTransition(Shows._show show, Shows._show._status previousStatus, Shows._show._status newStatus)
        {
            if (show == null)
            {
                return;
            }

            string previousStatusCode = CoreEnumNameMapping.ToShowStatusCode(previousStatus);
            string newStatusCode = CoreEnumNameMapping.ToShowStatusCode(newStatus);
            if (string.Equals(previousStatusCode, newStatusCode, StringComparison.Ordinal))
            {
                return;
            }

            List<int> castIdolIdentifiers = ResolveDistinctShowCastIdolIdentifiers(show);
            ShowStatusPayload payload = new ShowStatusPayload
            {
                ShowId = show.id,
                ShowTitle = show.title ?? string.Empty,
                PreviousShowStatus = previousStatusCode,
                NewShowStatus = newStatusCode,
                ShowCastType = CoreEnumNameMapping.ToShowCastTypeCode(show.castType),
                ShowEpisodeCount = show.episodeCount,
                ShowCastCount = castIdolIdentifiers.Count,
                ShowLatestAudience = ResolveLatestLongMetric(show.audience),
                ShowLatestRevenue = ResolveLatestLongMetric(show.revenue),
                ShowLatestNewFans = ResolveLatestIntMetric(show.fans),
                ShowLatestBuzz = ResolveLatestIntMetric(show.buzz)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                string eventPayloadJson = CoreJsonUtility.SerializeShowStatusPayload(payload);
                string showEntityIdentifier = show.id.ToString(CultureInfo.InvariantCulture);

                if (castIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    EnqueueEventRecordLocked(
                        gameDate,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindShow,
                        showEntityIdentifier,
                        CoreConstants.EventTypeShowStatusChanged,
                        CoreConstants.EventSourceShowStatusPatch,
                        eventPayloadJson);
                }
                else
                {
                    for (int castIndex = CoreConstants.ZeroBasedListStartIndex; castIndex < castIdolIdentifiers.Count; castIndex++)
                    {
                        EnqueueEventRecordLocked(
                            gameDate,
                            castIdolIdentifiers[castIndex],
                            CoreConstants.EventEntityKindShow,
                            showEntityIdentifier,
                            CoreConstants.EventTypeShowStatusChanged,
                            CoreConstants.EventSourceShowStatusPatch,
                            eventPayloadJson);
                    }
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one completed show-episode event for each cast idol.
        /// </summary>
        internal void CaptureShowEpisodeReleased(Shows._show show)
        {
            if (show == null)
            {
                return;
            }

            List<int> castIdolIdentifiers = ResolveDistinctShowCastIdolIdentifiers(show);
            long previousAudience = ResolvePreviousLongMetric(show.audience);
            long latestAudience = ResolveLatestLongMetric(show.audience);
            long previousRevenue = ResolvePreviousLongMetric(show.revenue);
            long latestRevenue = ResolveLatestLongMetric(show.revenue);
            int showEpisodeBudget = show.GetBudget();
            long previousProfit = previousRevenue - showEpisodeBudget;
            long latestProfit = latestRevenue - showEpisodeBudget;
            int previousNewFans = ResolvePreviousIntMetric(show.fans);
            int latestNewFans = ResolveLatestIntMetric(show.fans);
            int previousBuzz = ResolvePreviousIntMetric(show.buzz);
            int latestBuzz = ResolveLatestIntMetric(show.buzz);
            float previousFatigue = ResolvePreviousFloatMetric(show.fatigue);
            float latestFatigue = ResolveLatestFloatMetric(show.fatigue);
            int previousFame = ResolvePreviousIntMetric(show.fame);
            int latestFame = ResolveLatestIntMetric(show.fame);
            int previousFamePoints = ResolvePreviousIntMetric(show.famePoints);
            int latestFamePoints = ResolveLatestIntMetric(show.famePoints);
            ShowEpisodePayload payload = new ShowEpisodePayload
            {
                ShowId = show.id,
                ShowTitle = show.title ?? string.Empty,
                ShowCastType = CoreEnumNameMapping.ToShowCastTypeCode(show.castType),
                ShowEpisodeCount = show.episodeCount,
                ShowEpisodeDate = CoreDateTimeUtility.ToRoundTripString(staticVars.dateTime),
                ShowCastCount = castIdolIdentifiers.Count,
                ShowCastIdList = BuildDelimitedIdentifierList(castIdolIdentifiers),
                ShowPreviousAudience = previousAudience,
                ShowLatestAudience = latestAudience,
                ShowAudienceDelta = latestAudience - previousAudience,
                ShowPreviousRevenue = previousRevenue,
                ShowLatestRevenue = latestRevenue,
                ShowRevenueDelta = latestRevenue - previousRevenue,
                ShowPreviousProfit = previousProfit,
                ShowLatestProfit = latestProfit,
                ShowProfitDelta = latestProfit - previousProfit,
                ShowPreviousNewFans = previousNewFans,
                ShowLatestNewFans = latestNewFans,
                ShowNewFansDelta = latestNewFans - previousNewFans,
                ShowPreviousBuzz = previousBuzz,
                ShowLatestBuzz = latestBuzz,
                ShowBuzzDelta = latestBuzz - previousBuzz,
                ShowPreviousFatigue = previousFatigue,
                ShowLatestFatigue = latestFatigue,
                ShowFatigueDelta = latestFatigue - previousFatigue,
                ShowPreviousFame = previousFame,
                ShowLatestFame = latestFame,
                ShowFameDelta = latestFame - previousFame,
                ShowPreviousFamePoints = previousFamePoints,
                ShowLatestFamePoints = latestFamePoints,
                ShowFamePointsDelta = latestFamePoints - previousFamePoints,
                ShowEpisodeBudget = showEpisodeBudget,
                ShowStaminaCost = show.GetStaminaCost()
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                string eventPayloadJson = CoreJsonUtility.SerializeShowEpisodePayload(payload);
                string showEntityIdentifier = show.id.ToString(CultureInfo.InvariantCulture);

                if (castIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    EnqueueEventRecordLocked(
                        gameDate,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindShow,
                        showEntityIdentifier,
                        CoreConstants.EventTypeShowEpisodeReleased,
                        CoreConstants.EventSourceShowEpisodePatch,
                        eventPayloadJson);
                    EnqueueEventRecordLocked(
                        gameDate,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindShow,
                        showEntityIdentifier,
                        CoreConstants.EventTypeShowEpisode,
                        CoreConstants.EventSourceShowEpisodePatch,
                        eventPayloadJson);
                }
                else
                {
                    for (int castIndex = CoreConstants.ZeroBasedListStartIndex; castIndex < castIdolIdentifiers.Count; castIndex++)
                    {
                        EnqueueEventRecordLocked(
                            gameDate,
                            castIdolIdentifiers[castIndex],
                            CoreConstants.EventEntityKindShow,
                            showEntityIdentifier,
                            CoreConstants.EventTypeShowEpisodeReleased,
                            CoreConstants.EventSourceShowEpisodePatch,
                            eventPayloadJson);
                        EnqueueEventRecordLocked(
                            gameDate,
                            castIdolIdentifiers[castIndex],
                            CoreConstants.EventEntityKindShow,
                            showEntityIdentifier,
                            CoreConstants.EventTypeShowEpisode,
                            CoreConstants.EventSourceShowEpisodePatch,
                            eventPayloadJson);
                    }
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures pre-mutation show cast state for remove-girl hooks.
        /// </summary>
        internal ShowCastChangeSnapshot CreateShowCastChangeSnapshot(Shows._show show)
        {
            ShowCastChangeSnapshot snapshot = new ShowCastChangeSnapshot();
            if (show == null)
            {
                return snapshot;
            }

            snapshot.ShowCastIdolIdentifiersBefore = ResolveDistinctShowCastIdolIdentifiers(show);
            snapshot.ShowCastTypeBefore = show.castType;
            snapshot.ShowStatusBefore = show.status;
            snapshot.ShowTitleBefore = show.title ?? string.Empty;
            snapshot.ShowMcCodeBefore = ResolveShowParameterCode(show.mc);
            snapshot.ShowMcTitleBefore = ResolveShowParameterTitle(show.mc);
            snapshot.ShowProductionCostBefore = show.cost;
            snapshot.ShowFanAppealSummaryBefore = BuildFanAppealSummary(show.FanAppeal);
            return snapshot;
        }

        /// <summary>
        /// Captures one show cast-change event after `RemoveGirl` mutates cast state.
        /// </summary>
        internal void CaptureShowCastChanged(Shows._show show, data_girls.girls removedIdol, ShowCastChangeSnapshot snapshot)
        {
            CaptureShowCastChangedCore(
                show,
                removedIdol,
                snapshot,
                CoreConstants.EventSourceShowRemoveGirlPatch);
        }

        /// <summary>
        /// Captures one show cast-change event committed from show popup edits.
        /// </summary>
        internal void CaptureShowCastChangedFromPopup(Shows._show show, ShowCastChangeSnapshot snapshot)
        {
            CaptureShowCastChangedCore(
                show,
                null,
                snapshot,
                CoreConstants.EventSourceShowPopupContinuePatch);
        }

        /// <summary>
        /// Captures one show-configuration change event committed from show popup edits.
        /// </summary>
        internal void CaptureShowConfigurationChangedFromPopup(Shows._show show, ShowCastChangeSnapshot snapshot)
        {
            if (show == null)
            {
                return;
            }

            ShowCastChangeSnapshot previousState = snapshot ?? new ShowCastChangeSnapshot();
            string titleAfter = show.title ?? string.Empty;
            string mcCodeAfter = ResolveShowParameterCode(show.mc);
            string mcTitleAfter = ResolveShowParameterTitle(show.mc);
            int productionCostAfter = show.cost;
            string fanAppealSummaryAfter = BuildFanAppealSummary(show.FanAppeal);
            bool configurationChanged =
                !string.Equals(previousState.ShowTitleBefore ?? string.Empty, titleAfter, StringComparison.Ordinal)
                || !string.Equals(previousState.ShowMcCodeBefore ?? string.Empty, mcCodeAfter, StringComparison.Ordinal)
                || !string.Equals(previousState.ShowMcTitleBefore ?? string.Empty, mcTitleAfter, StringComparison.Ordinal)
                || previousState.ShowProductionCostBefore != productionCostAfter
                || !string.Equals(previousState.ShowFanAppealSummaryBefore ?? string.Empty, fanAppealSummaryAfter, StringComparison.Ordinal);
            if (!configurationChanged)
            {
                return;
            }

            List<int> castIdolIdentifiersBefore = previousState.ShowCastIdolIdentifiersBefore ?? new List<int>();
            List<int> castIdolIdentifiersAfter = ResolveDistinctShowCastIdolIdentifiers(show);
            ShowConfigurationChangePayload payload = BuildShowConfigurationChangePayload(
                show,
                previousState,
                castIdolIdentifiersAfter,
                titleAfter,
                mcCodeAfter,
                mcTitleAfter,
                productionCostAfter,
                fanAppealSummaryAfter);
            string eventPayloadJson = CoreJsonUtility.SerializeShowConfigurationChangePayload(payload);
            List<int> impactedIdolIdentifiers = ResolveDistinctUnionIdentifiers(
                castIdolIdentifiersBefore,
                castIdolIdentifiersAfter,
                CoreConstants.InvalidIdValue);
            string showEntityIdentifier = show.id.ToString(CultureInfo.InvariantCulture);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                if (impactedIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    EnqueueEventRecordLocked(
                        gameDate,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindShow,
                        showEntityIdentifier,
                        CoreConstants.EventTypeShowConfigurationChanged,
                        CoreConstants.EventSourceShowPopupContinuePatch,
                        eventPayloadJson);
                }
                else
                {
                    for (int idolIndex = CoreConstants.ZeroBasedListStartIndex; idolIndex < impactedIdolIdentifiers.Count; idolIndex++)
                    {
                        EnqueueEventRecordLocked(
                            gameDate,
                            impactedIdolIdentifiers[idolIndex],
                            CoreConstants.EventEntityKindShow,
                            showEntityIdentifier,
                            CoreConstants.EventTypeShowConfigurationChanged,
                            CoreConstants.EventSourceShowPopupContinuePatch,
                            eventPayloadJson);
                    }
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one show cast-change event for any source that mutates show cast state.
        /// </summary>
        private void CaptureShowCastChangedCore(
            Shows._show show,
            data_girls.girls removedIdol,
            ShowCastChangeSnapshot snapshot,
            string sourcePatchCode)
        {
            if (show == null)
            {
                return;
            }

            ShowCastChangeSnapshot previousState = snapshot ?? new ShowCastChangeSnapshot();
            List<int> castIdolIdentifiersBefore = previousState.ShowCastIdolIdentifiersBefore ?? new List<int>();
            List<int> castIdolIdentifiersAfter = ResolveDistinctShowCastIdolIdentifiers(show);
            List<int> addedCastIdolIdentifiers = ResolveAddedIdentifiers(castIdolIdentifiersBefore, castIdolIdentifiersAfter);
            List<int> removedCastIdolIdentifiers = ResolveRemovedIdentifiers(castIdolIdentifiersBefore, castIdolIdentifiersAfter);
            bool castTypeChanged = previousState.ShowCastTypeBefore != show.castType;
            bool statusChanged = previousState.ShowStatusBefore != show.status;

            if (!castTypeChanged
                && !statusChanged
                && addedCastIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount
                && removedCastIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return;
            }

            ShowCastChangePayload payload = BuildShowCastChangePayload(
                show,
                previousState,
                castIdolIdentifiersAfter,
                addedCastIdolIdentifiers,
                removedCastIdolIdentifiers,
                removedIdol);
            string eventPayloadJson = CoreJsonUtility.SerializeShowCastChangePayload(payload);
            List<int> impactedIdolIdentifiers = ResolveDistinctUnionIdentifiers(
                castIdolIdentifiersBefore,
                castIdolIdentifiersAfter,
                removedIdol != null ? removedIdol.id : CoreConstants.InvalidIdValue);
            string showEntityIdentifier = show.id.ToString(CultureInfo.InvariantCulture);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                if (impactedIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    EnqueueEventRecordLocked(
                        gameDate,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindShow,
                        showEntityIdentifier,
                        CoreConstants.EventTypeShowCastChanged,
                        sourcePatchCode,
                        eventPayloadJson);
                }
                else
                {
                    for (int idolIndex = CoreConstants.ZeroBasedListStartIndex; idolIndex < impactedIdolIdentifiers.Count; idolIndex++)
                    {
                        EnqueueEventRecordLocked(
                            gameDate,
                            impactedIdolIdentifiers[idolIndex],
                            CoreConstants.EventEntityKindShow,
                            showEntityIdentifier,
                            CoreConstants.EventTypeShowCastChanged,
                            sourcePatchCode,
                            eventPayloadJson);
                    }
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one concert-creation lifecycle event.
        /// </summary>
        internal void CaptureConcertCreated(SEvent_Concerts._concert concert)
        {
            CaptureConcertLifecycleEvent(concert, CoreConstants.EventTypeConcertCreated, CoreConstants.EventSourceConcertSetPatch, false);
        }

        /// <summary>
        /// Captures one concert-start lifecycle event.
        /// </summary>
        internal void CaptureConcertStarted(SEvent_Concerts._concert concert)
        {
            CaptureConcertLifecycleEvent(concert, CoreConstants.EventTypeConcertStarted, CoreConstants.EventSourceConcertStartPatch, false);
        }

        /// <summary>
        /// Captures one concert-finish lifecycle event and per-idol participation rows.
        /// </summary>
        internal void CaptureConcertFinished(SEvent_Concerts._concert concert)
        {
            CaptureConcertLifecycleEvent(concert, CoreConstants.EventTypeConcertFinished, CoreConstants.EventSourceConcertFinishPatch, true);
        }

        /// <summary>
        /// Captures one concert-cancel lifecycle event and per-idol participation rows.
        /// </summary>
        internal void CaptureConcertCancelled(SEvent_Concerts._concert concert)
        {
            CaptureConcertLifecycleEvent(concert, CoreConstants.EventTypeConcertCancelled, CoreConstants.EventSourceConcertCancelPatch, true);
        }

        /// <summary>
        /// Captures one concert status-change event after status mutation completes.
        /// </summary>
        internal void CaptureConcertStatusTransition(
            SEvent_Concerts._concert concert,
            SEvent_Tour.tour._status previousStatus,
            SEvent_Tour.tour._status newStatus)
        {
            if (concert == null || previousStatus == newStatus)
            {
                return;
            }

            List<int> participantIdolIdentifiers = ResolveDistinctConcertParticipantIdolIdentifiers(concert);
            ConcertStatusPayload payload = BuildConcertStatusPayload(concert, previousStatus, newStatus, participantIdolIdentifiers);
            string eventPayloadJson = CoreJsonUtility.SerializeConcertStatusPayload(payload);
            string concertEntityIdentifier = concert.ID.ToString(CultureInfo.InvariantCulture);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindConcert,
                    concertEntityIdentifier,
                    CoreConstants.EventTypeConcertStatusChanged,
                    CoreConstants.EventSourceConcertStatusPatch,
                    eventPayloadJson);

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Registers one baseline snapshot for an existing concert being edited in popup UI.
        /// </summary>
        internal void RegisterConcertEditBaseline(SEvent_Concerts._concert concert)
        {
            if (concert == null || concert.ID < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            ConcertCastChangeSnapshot baselineSnapshot = CreateConcertCastChangeSnapshot(concert);
            lock (runtimeLock)
            {
                concertEditBaselineByConcertId[concert.ID] = baselineSnapshot;
            }
        }

        /// <summary>
        /// Captures one concert cast/configuration change event when popup edits are committed.
        /// </summary>
        internal void CaptureConcertCastChangedFromPopup(SEvent_Concerts._concert concert)
        {
            if (concert == null || concert.ID < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            ConcertCastChangeSnapshot baselineSnapshot = null;
            lock (runtimeLock)
            {
                if (concertEditBaselineByConcertId.TryGetValue(concert.ID, out baselineSnapshot))
                {
                    concertEditBaselineByConcertId.Remove(concert.ID);
                }
            }

            if (baselineSnapshot == null)
            {
                return;
            }

            CaptureConcertCastChangedCore(
                concert,
                null,
                baselineSnapshot,
                CoreConstants.EventSourceConcertNewPopupContinuePatch);

            CaptureConcertConfigurationChangedFromPopup(concert, baselineSnapshot);
        }

        /// <summary>
        /// Captures pre-mutation concert cast state for remove-girl hooks.
        /// </summary>
        internal ConcertCastChangeSnapshot CreateConcertCastChangeSnapshot(SEvent_Concerts._concert concert)
        {
            ConcertCastChangeSnapshot snapshot = new ConcertCastChangeSnapshot();
            if (concert == null)
            {
                return snapshot;
            }

            snapshot.ConcertParticipantIdolIdentifiersBefore = ResolveDistinctConcertParticipantIdolIdentifiers(concert);
            snapshot.ConcertStatusBefore = concert.Status;
            snapshot.ConcertSongCountBefore = concert.GetNumberOfSongs();
            snapshot.ConcertSetlistSummaryBefore = BuildConcertSetlistSummary(concert);
            snapshot.ConcertTitleBefore = concert.GetTitle() ?? string.Empty;
            snapshot.ConcertRawTitleBefore = concert.Title ?? string.Empty;
            snapshot.ConcertVenueBefore = concert.Venue;
            snapshot.ConcertTicketPriceBefore = ResolveConcertTicketPrice(concert);
            return snapshot;
        }

        /// <summary>
        /// Captures one concert-configuration change event committed from concert popup edits.
        /// </summary>
        private void CaptureConcertConfigurationChangedFromPopup(SEvent_Concerts._concert concert, ConcertCastChangeSnapshot snapshot)
        {
            if (concert == null)
            {
                return;
            }

            ConcertCastChangeSnapshot previousState = snapshot ?? new ConcertCastChangeSnapshot();
            string titleAfter = concert.GetTitle() ?? string.Empty;
            string rawTitleAfter = concert.Title ?? string.Empty;
            string venueBeforeCode = CoreEnumNameMapping.ToConcertVenueCode(previousState.ConcertVenueBefore);
            string venueAfterCode = CoreEnumNameMapping.ToConcertVenueCode(concert.Venue);
            int ticketPriceAfter = ResolveConcertTicketPrice(concert);
            bool configurationChanged =
                !string.Equals(previousState.ConcertTitleBefore ?? string.Empty, titleAfter, StringComparison.Ordinal)
                || !string.Equals(previousState.ConcertRawTitleBefore ?? string.Empty, rawTitleAfter, StringComparison.Ordinal)
                || !string.Equals(venueBeforeCode, venueAfterCode, StringComparison.Ordinal)
                || previousState.ConcertTicketPriceBefore != ticketPriceAfter;
            if (!configurationChanged)
            {
                return;
            }

            List<int> participantsBefore = previousState.ConcertParticipantIdolIdentifiersBefore ?? new List<int>();
            List<int> participantsAfter = ResolveDistinctConcertParticipantIdolIdentifiers(concert);
            int songCountAfter = concert.GetNumberOfSongs();
            string setlistSummaryAfter = BuildConcertSetlistSummary(concert);
            ConcertConfigurationChangePayload payload = BuildConcertConfigurationChangePayload(
                concert,
                previousState,
                participantsAfter,
                songCountAfter,
                setlistSummaryAfter,
                titleAfter,
                rawTitleAfter,
                venueAfterCode,
                ticketPriceAfter);
            string eventPayloadJson = CoreJsonUtility.SerializeConcertConfigurationChangePayload(payload);
            List<int> impactedIdolIdentifiers = ResolveDistinctUnionIdentifiers(
                participantsBefore,
                participantsAfter,
                CoreConstants.InvalidIdValue);
            string concertEntityIdentifier = concert.ID.ToString(CultureInfo.InvariantCulture);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                if (impactedIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    EnqueueEventRecordLocked(
                        gameDate,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindConcert,
                        concertEntityIdentifier,
                        CoreConstants.EventTypeConcertConfigurationChanged,
                        CoreConstants.EventSourceConcertNewPopupContinuePatch,
                        eventPayloadJson);
                }
                else
                {
                    for (int idolIndex = CoreConstants.ZeroBasedListStartIndex; idolIndex < impactedIdolIdentifiers.Count; idolIndex++)
                    {
                        EnqueueEventRecordLocked(
                            gameDate,
                            impactedIdolIdentifiers[idolIndex],
                            CoreConstants.EventEntityKindConcert,
                            concertEntityIdentifier,
                            CoreConstants.EventTypeConcertConfigurationChanged,
                            CoreConstants.EventSourceConcertNewPopupContinuePatch,
                            eventPayloadJson);
                    }
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one concert cast-change event after `RemoveGirl` mutates setlist state.
        /// </summary>
        internal void CaptureConcertCastChanged(
            SEvent_Concerts._concert concert,
            data_girls.girls removedIdol,
            ConcertCastChangeSnapshot snapshot)
        {
            CaptureConcertCastChangedCore(
                concert,
                removedIdol,
                snapshot,
                CoreConstants.EventSourceConcertRemoveGirlPatch);
        }

        /// <summary>
        /// Captures one concert cast-change event for any source that mutates setlist state.
        /// </summary>
        private void CaptureConcertCastChangedCore(
            SEvent_Concerts._concert concert,
            data_girls.girls removedIdol,
            ConcertCastChangeSnapshot snapshot,
            string sourcePatchCode)
        {
            if (concert == null)
            {
                return;
            }

            ConcertCastChangeSnapshot previousState = snapshot ?? new ConcertCastChangeSnapshot();
            List<int> participantsBefore = previousState.ConcertParticipantIdolIdentifiersBefore ?? new List<int>();
            List<int> participantsAfter = ResolveDistinctConcertParticipantIdolIdentifiers(concert);
            List<int> addedParticipantIds = ResolveAddedIdentifiers(participantsBefore, participantsAfter);
            List<int> removedParticipantIds = ResolveRemovedIdentifiers(participantsBefore, participantsAfter);
            int songCountAfter = concert.GetNumberOfSongs();
            string setlistSummaryAfter = BuildConcertSetlistSummary(concert);
            bool statusChanged = previousState.ConcertStatusBefore != concert.Status;

            if (!statusChanged
                && previousState.ConcertSongCountBefore == songCountAfter
                && string.Equals(previousState.ConcertSetlistSummaryBefore ?? string.Empty, setlistSummaryAfter ?? string.Empty, StringComparison.Ordinal)
                && addedParticipantIds.Count < CoreConstants.MinimumNonEmptyCollectionCount
                && removedParticipantIds.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return;
            }

            ConcertCastChangePayload payload = BuildConcertCastChangePayload(
                concert,
                previousState,
                participantsAfter,
                addedParticipantIds,
                removedParticipantIds,
                songCountAfter,
                setlistSummaryAfter,
                removedIdol);
            string eventPayloadJson = CoreJsonUtility.SerializeConcertCastChangePayload(payload);
            List<int> impactedIdolIdentifiers = ResolveDistinctUnionIdentifiers(
                participantsBefore,
                participantsAfter,
                removedIdol != null ? removedIdol.id : CoreConstants.InvalidIdValue);
            string concertEntityIdentifier = concert.ID.ToString(CultureInfo.InvariantCulture);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                if (impactedIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    EnqueueEventRecordLocked(
                        gameDate,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindConcert,
                        concertEntityIdentifier,
                        CoreConstants.EventTypeConcertCastChanged,
                        sourcePatchCode,
                        eventPayloadJson);
                }
                else
                {
                    for (int idolIndex = CoreConstants.ZeroBasedListStartIndex; idolIndex < impactedIdolIdentifiers.Count; idolIndex++)
                    {
                        EnqueueEventRecordLocked(
                            gameDate,
                            impactedIdolIdentifiers[idolIndex],
                            CoreConstants.EventEntityKindConcert,
                            concertEntityIdentifier,
                            CoreConstants.EventTypeConcertCastChanged,
                            sourcePatchCode,
                            eventPayloadJson);
                    }
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one concert lifecycle record and optional per-idol participation records.
        /// </summary>
        private void CaptureConcertLifecycleEvent(
            SEvent_Concerts._concert concert,
            string lifecycleEventTypeCode,
            string sourcePatchCode,
            bool includeParticipationRows)
        {
            if (concert == null)
            {
                return;
            }

            List<int> participantIdolIdentifiers = ResolveDistinctConcertParticipantIdolIdentifiers(concert);
            ConcertLifecyclePayload lifecyclePayload = BuildConcertLifecyclePayload(
                concert,
                CoreConstants.InvalidIdValue,
                participantIdolIdentifiers);
            string concertEntityIdentifier = concert.ID.ToString(CultureInfo.InvariantCulture);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    CoreConstants.InvalidIdValue,
                    CoreConstants.EventEntityKindConcert,
                    concertEntityIdentifier,
                    lifecycleEventTypeCode,
                    sourcePatchCode,
                    CoreJsonUtility.SerializeConcertLifecyclePayload(lifecyclePayload));

                if (includeParticipationRows && participantIdolIdentifiers.Count >= CoreConstants.MinimumNonEmptyCollectionCount)
                {
                    for (int participantIndex = CoreConstants.ZeroBasedListStartIndex; participantIndex < participantIdolIdentifiers.Count; participantIndex++)
                    {
                        int participantIdolId = participantIdolIdentifiers[participantIndex];
                        ConcertLifecyclePayload participationPayload = BuildConcertLifecyclePayload(
                            concert,
                            participantIdolId,
                            participantIdolIdentifiers);

                        EnqueueEventRecordLocked(
                            gameDate,
                            participantIdolId,
                            CoreConstants.EventEntityKindConcert,
                            concertEntityIdentifier,
                            CoreConstants.EventTypeConcertParticipation,
                            sourcePatchCode,
                            CoreJsonUtility.SerializeConcertLifecyclePayload(participationPayload));
                    }
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures generated award nominations as discrete timeline events.
        /// </summary>
        internal void CaptureAwardNominations()
        {
            if (Awards.TempNominations == null || Awards.TempNominations.Count < CoreConstants.MinimumNonEmptyCollectionCount)
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

                DateTime gameDate = staticVars.dateTime;
                for (int nominationIndex = CoreConstants.ZeroBasedListStartIndex; nominationIndex < Awards.TempNominations.Count; nominationIndex++)
                {
                    Awards._award nomination = Awards.TempNominations[nominationIndex];
                    if (nomination == null)
                    {
                        continue;
                    }

                    int idolId = nomination.Girl != null ? nomination.Girl.id : CoreConstants.InvalidIdValue;
                    int singleId = nomination.Single != null ? nomination.Single.id : CoreConstants.InvalidIdValue;
                    string awardTypeCode = CoreEnumNameMapping.ToAwardTypeCode(nomination.Type);
                    string awardEntityIdentifier = BuildAwardEntityIdentifier(awardTypeCode, nomination.Year, idolId, singleId);
                    string idempotencyKey = BuildAwardIdempotencyKey(
                        CoreConstants.EventTypeAwardNominated,
                        awardEntityIdentifier,
                        false,
                        nomination.IsNomination);

                    if (!TryReserveIdempotencyKeyLocked(gameDate, idempotencyKey))
                    {
                        continue;
                    }

                    AwardLifecyclePayload payload = new AwardLifecyclePayload
                    {
                        IdolId = idolId,
                        AwardType = awardTypeCode,
                        AwardYear = nomination.Year,
                        AwardIsNomination = true,
                        AwardWon = false,
                        AwardSingleId = singleId
                    };

                    EnqueueEventRecordLocked(
                        gameDate,
                        idolId,
                        CoreConstants.EventEntityKindAward,
                        awardEntityIdentifier,
                        CoreConstants.EventTypeAwardNominated,
                        CoreConstants.EventSourceAwardNominationsPatch,
                        CoreJsonUtility.SerializeAwardLifecyclePayload(payload));
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures finalized award outcomes for all temporary nominations.
        /// </summary>
        internal void CaptureAwardResults()
        {
            if (Awards.TempNominations == null || Awards.TempNominations.Count < CoreConstants.MinimumNonEmptyCollectionCount)
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

                DateTime gameDate = staticVars.dateTime;
                for (int nominationIndex = CoreConstants.ZeroBasedListStartIndex; nominationIndex < Awards.TempNominations.Count; nominationIndex++)
                {
                    Awards._award nomination = Awards.TempNominations[nominationIndex];
                    if (nomination == null)
                    {
                        continue;
                    }

                    int idolId = nomination.Girl != null ? nomination.Girl.id : CoreConstants.InvalidIdValue;
                    int singleId = nomination.Single != null ? nomination.Single.id : CoreConstants.InvalidIdValue;
                    bool awardWon = nomination.IsWin();
                    string awardTypeCode = CoreEnumNameMapping.ToAwardTypeCode(nomination.Type);
                    string awardEntityIdentifier = BuildAwardEntityIdentifier(awardTypeCode, nomination.Year, idolId, singleId);
                    string idempotencyKey = BuildAwardIdempotencyKey(
                        CoreConstants.EventTypeAwardResult,
                        awardEntityIdentifier,
                        awardWon,
                        nomination.IsNomination);

                    if (!TryReserveIdempotencyKeyLocked(gameDate, idempotencyKey))
                    {
                        continue;
                    }

                    AwardLifecyclePayload payload = new AwardLifecyclePayload
                    {
                        IdolId = idolId,
                        AwardType = awardTypeCode,
                        AwardYear = nomination.Year,
                        AwardIsNomination = nomination.IsNomination,
                        AwardWon = awardWon,
                        AwardSingleId = singleId
                    };

                    EnqueueEventRecordLocked(
                        gameDate,
                        idolId,
                        CoreConstants.EventEntityKindAward,
                        awardEntityIdentifier,
                        CoreConstants.EventTypeAwardResult,
                        CoreConstants.EventSourceAwardResultsPatch,
                        CoreJsonUtility.SerializeAwardLifecyclePayload(payload));
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Creates a push-slot snapshot for change-detection in Harmony prefix patches.
        /// </summary>
        internal List<int> CreatePushSlotSnapshot(IList<data_girls.girls> pushGirls)
        {
            return ResolvePushSlotIdolIdentifiers(pushGirls);
        }

        /// <summary>
        /// Creates one push-removal snapshot before the game mutates push slot state.
        /// </summary>
        internal PushRemovalSnapshot CreatePushRemovalSnapshot(data_girls.girls idol)
        {
            if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier || Pushes.Girls == null)
            {
                return null;
            }

            int removedSlotIndex = Pushes.Girls.IndexOf(idol);
            if (removedSlotIndex < CoreConstants.ZeroBasedListStartIndex)
            {
                return null;
            }

            int daysInSlot = CoreConstants.ZeroBasedListStartIndex;
            if (Pushes.Days != null && removedSlotIndex < Pushes.Days.Count)
            {
                daysInSlot = Pushes.Days[removedSlotIndex];
            }

            return new PushRemovalSnapshot
            {
                SlotIndex = removedSlotIndex,
                IdolId = idol.id,
                DaysInSlot = daysInSlot
            };
        }

        /// <summary>
        /// Captures push-slot start/end transitions by comparing previous and current slot assignments.
        /// </summary>
        internal void CapturePushSetAssignments(
            IList<int> previousSlotIdolIdentifiers,
            IList<data_girls.girls> currentPushGirls,
            IList<int> currentPushDays)
        {
            List<int> currentSlotIdolIdentifiers = ResolvePushSlotIdolIdentifiers(currentPushGirls);
            List<int> currentSlotDayCounts = ResolvePushSlotDayCounts(currentPushDays, currentSlotIdolIdentifiers.Count);
            int previousSlotCount = previousSlotIdolIdentifiers != null ? previousSlotIdolIdentifiers.Count : CoreConstants.ZeroBasedListStartIndex;
            int currentSlotCount = currentSlotIdolIdentifiers.Count;
            int slotCount = Math.Max(previousSlotCount, currentSlotCount);

            if (slotCount < CoreConstants.MinimumNonEmptyCollectionCount)
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

                DateTime gameDate = staticVars.dateTime;
                for (int slotIndex = CoreConstants.ZeroBasedListStartIndex; slotIndex < slotCount; slotIndex++)
                {
                    int previousIdolId = previousSlotIdolIdentifiers != null && slotIndex < previousSlotIdolIdentifiers.Count
                        ? previousSlotIdolIdentifiers[slotIndex]
                        : CoreConstants.InvalidIdValue;
                    int currentIdolId = slotIndex < currentSlotIdolIdentifiers.Count
                        ? currentSlotIdolIdentifiers[slotIndex]
                        : CoreConstants.InvalidIdValue;
                    int currentDaysInSlot = slotIndex < currentSlotDayCounts.Count
                        ? currentSlotDayCounts[slotIndex]
                        : CoreConstants.ZeroBasedListStartIndex;

                    if (previousIdolId == currentIdolId)
                    {
                        continue;
                    }

                    if (previousIdolId >= CoreConstants.MinimumValidIdolIdentifier)
                    {
                        PushLifecyclePayload pushEndedPayload = BuildPushLifecyclePayload(
                            previousIdolId,
                            slotIndex,
                            previousIdolId,
                            currentIdolId,
                            currentDaysInSlot,
                            CoreConstants.PushLifecycleActionEnded);

                        EnqueueEventRecordLocked(
                            gameDate,
                            previousIdolId,
                            CoreConstants.EventEntityKindPush,
                            BuildPushEntityIdentifier(slotIndex),
                            CoreConstants.EventTypePushWindowEnded,
                            CoreConstants.EventSourcePushSetPushesPatch,
                            CoreJsonUtility.SerializePushLifecyclePayload(pushEndedPayload));
                    }

                    if (currentIdolId >= CoreConstants.MinimumValidIdolIdentifier)
                    {
                        PushLifecyclePayload pushStartedPayload = BuildPushLifecyclePayload(
                            currentIdolId,
                            slotIndex,
                            previousIdolId,
                            currentIdolId,
                            currentDaysInSlot,
                            CoreConstants.PushLifecycleActionStarted);

                        EnqueueEventRecordLocked(
                            gameDate,
                            currentIdolId,
                            CoreConstants.EventEntityKindPush,
                            BuildPushEntityIdentifier(slotIndex),
                            CoreConstants.EventTypePushWindowStarted,
                            CoreConstants.EventSourcePushSetPushesPatch,
                            CoreJsonUtility.SerializePushLifecyclePayload(pushStartedPayload));
                    }
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one push end event from `Pushes.RemovePush` pre-mutation snapshot data.
        /// </summary>
        internal void CapturePushRemovalSnapshot(PushRemovalSnapshot removalSnapshot)
        {
            if (removalSnapshot == null
                || removalSnapshot.IdolId < CoreConstants.MinimumValidIdolIdentifier
                || removalSnapshot.SlotIndex < CoreConstants.ZeroBasedListStartIndex)
            {
                return;
            }

            PushLifecyclePayload payload = BuildPushLifecyclePayload(
                removalSnapshot.IdolId,
                removalSnapshot.SlotIndex,
                removalSnapshot.IdolId,
                CoreConstants.InvalidIdValue,
                removalSnapshot.DaysInSlot,
                CoreConstants.PushLifecycleActionEnded);

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    removalSnapshot.IdolId,
                    CoreConstants.EventEntityKindPush,
                    BuildPushEntityIdentifier(removalSnapshot.SlotIndex),
                    CoreConstants.EventTypePushWindowEnded,
                    CoreConstants.EventSourcePushRemovePatch,
                    CoreJsonUtility.SerializePushLifecyclePayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures per-day push aging events for currently assigned push slots.
        /// </summary>
        internal void CapturePushDayIncrements()
        {
            if (Pushes.Girls == null || Pushes.Days == null)
            {
                return;
            }

            List<int> currentSlotIdolIdentifiers = ResolvePushSlotIdolIdentifiers(Pushes.Girls);
            List<int> currentSlotDayCounts = ResolvePushSlotDayCounts(Pushes.Days, currentSlotIdolIdentifiers.Count);
            if (currentSlotIdolIdentifiers.Count < CoreConstants.MinimumNonEmptyCollectionCount)
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

                DateTime gameDate = staticVars.dateTime;
                for (int slotIndex = CoreConstants.ZeroBasedListStartIndex; slotIndex < currentSlotIdolIdentifiers.Count; slotIndex++)
                {
                    int currentIdolId = currentSlotIdolIdentifiers[slotIndex];
                    if (currentIdolId < CoreConstants.MinimumValidIdolIdentifier)
                    {
                        continue;
                    }

                    int currentDaysInSlot = slotIndex < currentSlotDayCounts.Count
                        ? currentSlotDayCounts[slotIndex]
                        : CoreConstants.ZeroBasedListStartIndex;

                    string idempotencyKey = string.Concat(
                        CoreConstants.PushDayIncrementIdempotencyPrefix,
                        CoreConstants.SaveKeyJoinSeparator,
                        slotIndex.ToString(CultureInfo.InvariantCulture),
                        CoreConstants.SaveKeyJoinSeparator,
                        currentIdolId.ToString(CultureInfo.InvariantCulture),
                        CoreConstants.SaveKeyJoinSeparator,
                        currentDaysInSlot.ToString(CultureInfo.InvariantCulture));

                    if (!TryReserveIdempotencyKeyLocked(gameDate, idempotencyKey))
                    {
                        continue;
                    }

                    PushLifecyclePayload payload = BuildPushLifecyclePayload(
                        currentIdolId,
                        slotIndex,
                        currentIdolId,
                        currentIdolId,
                        currentDaysInSlot,
                        CoreConstants.PushLifecycleActionDayIncrement);

                    EnqueueEventRecordLocked(
                        gameDate,
                        currentIdolId,
                        CoreConstants.EventEntityKindPush,
                        BuildPushEntityIdentifier(slotIndex),
                        CoreConstants.EventTypePushWindowDayIncrement,
                        CoreConstants.EventSourcePushOnNewDayPatch,
                        CoreJsonUtility.SerializePushLifecyclePayload(payload));
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Creates one pre-mutation snapshot for idol-idol dating state.
        /// </summary>
        internal bool CreateRelationshipDatingSnapshot(Relationships._relationship relationship)
        {
            return relationship != null && relationship.Dating;
        }

        /// <summary>
        /// Creates one pre-mutation snapshot for idol-idol relationship status.
        /// </summary>
        internal Relationships._relationship._status CreateRelationshipStatusSnapshot(Relationships._relationship relationship)
        {
            return relationship != null
                ? relationship.Status
                : Relationships._relationship._status.NONE;
        }

        /// <summary>
        /// Captures one idol-idol dating-start event.
        /// </summary>
        internal void CaptureIdolDatingStarted(Relationships._relationship relationship, bool wasDatingBefore)
        {
            if (relationship == null || wasDatingBefore || !relationship.Dating)
            {
                return;
            }

            int idolAId;
            int idolBId;
            if (!TryResolveRelationshipPairIdolIdentifiers(relationship, out idolAId, out idolBId))
            {
                return;
            }

            string relationshipPairEntityIdentifier = BuildRelationshipPairEntityIdentifier(idolAId, idolBId);
            IdolRelationshipLifecyclePayload payload = new IdolRelationshipLifecyclePayload
            {
                IdolAId = idolAId,
                IdolBId = idolBId,
                RelationshipStatus = CoreEnumNameMapping.ToRelationshipStatusCode(relationship.Status),
                RelationshipDynamic = CoreEnumNameMapping.ToRelationshipDynamicCode(relationship.Dynamic),
                RelationshipKnownToPlayer = relationship.IsRelationshipKnown(),
                RelationshipPairKey = relationshipPairEntityIdentifier,
                RelationshipBreakReason = string.Empty,
                RelationshipIsDating = relationship.Dating
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                string payloadJson = CoreJsonUtility.SerializeIdolRelationshipLifecyclePayload(payload);
                EnqueueEventRecordLocked(
                    gameDate,
                    idolAId,
                    CoreConstants.EventEntityKindRelationship,
                    relationshipPairEntityIdentifier,
                    CoreConstants.EventTypeIdolDatingStarted,
                    CoreConstants.EventSourceIdolRelationshipStartPatch,
                    payloadJson);

                if (idolBId != idolAId)
                {
                    EnqueueEventRecordLocked(
                        gameDate,
                        idolBId,
                        CoreConstants.EventEntityKindRelationship,
                        relationshipPairEntityIdentifier,
                        CoreConstants.EventTypeIdolDatingStarted,
                        CoreConstants.EventSourceIdolRelationshipStartPatch,
                        payloadJson);
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one idol-idol dating-end event.
        /// </summary>
        internal void CaptureIdolDatingEnded(Relationships._relationship relationship, bool wasDatingBefore)
        {
            if (relationship == null || !wasDatingBefore || relationship.Dating)
            {
                return;
            }

            int idolAId;
            int idolBId;
            if (!TryResolveRelationshipPairIdolIdentifiers(relationship, out idolAId, out idolBId))
            {
                return;
            }

            string relationshipPairEntityIdentifier = BuildRelationshipPairEntityIdentifier(idolAId, idolBId);
            IdolRelationshipLifecyclePayload payload = new IdolRelationshipLifecyclePayload
            {
                IdolAId = idolAId,
                IdolBId = idolBId,
                RelationshipStatus = CoreEnumNameMapping.ToRelationshipStatusCode(relationship.Status),
                RelationshipDynamic = CoreEnumNameMapping.ToRelationshipDynamicCode(relationship.Dynamic),
                RelationshipKnownToPlayer = relationship.IsRelationshipKnown(),
                RelationshipPairKey = relationshipPairEntityIdentifier,
                RelationshipBreakReason = CoreConstants.RelationshipBreakReasonGeneric,
                RelationshipIsDating = relationship.Dating
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                string payloadJson = CoreJsonUtility.SerializeIdolRelationshipLifecyclePayload(payload);
                EnqueueEventRecordLocked(
                    gameDate,
                    idolAId,
                    CoreConstants.EventEntityKindRelationship,
                    relationshipPairEntityIdentifier,
                    CoreConstants.EventTypeIdolDatingEnded,
                    CoreConstants.EventSourceIdolRelationshipBreakPatch,
                    payloadJson);

                if (idolBId != idolAId)
                {
                    EnqueueEventRecordLocked(
                        gameDate,
                        idolBId,
                        CoreConstants.EventEntityKindRelationship,
                        relationshipPairEntityIdentifier,
                        CoreConstants.EventTypeIdolDatingEnded,
                        CoreConstants.EventSourceIdolRelationshipBreakPatch,
                        payloadJson);
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures idol-idol relationship status transitions driven by relationship score updates.
        /// </summary>
        internal void CaptureIdolRelationshipStatusChanged(
            Relationships._relationship relationship,
            Relationships._relationship._status previousStatus,
            float requestedDelta)
        {
            if (relationship == null)
            {
                return;
            }

            Relationships._relationship._status newStatus = relationship.Status;
            if (previousStatus == newStatus)
            {
                return;
            }

            int idolAId;
            int idolBId;
            if (!TryResolveRelationshipPairIdolIdentifiers(relationship, out idolAId, out idolBId))
            {
                return;
            }

            string relationshipPairEntityIdentifier = BuildRelationshipPairEntityIdentifier(idolAId, idolBId);
            IdolRelationshipStatusChangePayload payload = new IdolRelationshipStatusChangePayload
            {
                IdolAId = idolAId,
                IdolBId = idolBId,
                RelationshipPreviousStatus = CoreEnumNameMapping.ToRelationshipStatusCode(previousStatus),
                RelationshipNewStatus = CoreEnumNameMapping.ToRelationshipStatusCode(newStatus),
                RelationshipDynamic = CoreEnumNameMapping.ToRelationshipDynamicCode(relationship.Dynamic),
                RelationshipKnownToPlayer = relationship.IsRelationshipKnown(),
                RelationshipPairKey = relationshipPairEntityIdentifier,
                RelationshipIsDating = relationship.Dating,
                RelationshipRequestedDelta = requestedDelta,
                RelationshipRatio = relationship.Ratio
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                string payloadJson = CoreJsonUtility.SerializeIdolRelationshipStatusChangePayload(payload);
                EnqueueEventRecordLocked(
                    gameDate,
                    idolAId,
                    CoreConstants.EventEntityKindRelationship,
                    relationshipPairEntityIdentifier,
                    CoreConstants.EventTypeIdolRelationshipStatusChanged,
                    CoreConstants.EventSourceIdolRelationshipAddPatch,
                    payloadJson);

                if (idolBId != idolAId)
                {
                    EnqueueEventRecordLocked(
                        gameDate,
                        idolBId,
                        CoreConstants.EventEntityKindRelationship,
                        relationshipPairEntityIdentifier,
                        CoreConstants.EventTypeIdolRelationshipStatusChanged,
                        CoreConstants.EventSourceIdolRelationshipAddPatch,
                        payloadJson);
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one clique-join event.
        /// </summary>
        internal void CaptureCliqueMemberJoined(Relationships._clique clique, data_girls.girls joinedIdol)
        {
            if (clique == null || joinedIdol == null || joinedIdol.id < CoreConstants.MinimumValidIdolIdentifier || clique.Members == null || !clique.Members.Contains(joinedIdol))
            {
                return;
            }

            int leaderId = ResolveCliqueLeaderIdOrInvalid(clique);
            string cliqueSignature = BuildCliqueSignature(clique);
            string cliqueEntityIdentifier = !string.IsNullOrEmpty(cliqueSignature)
                ? cliqueSignature
                : CoreConstants.UnknownCliqueEntityIdentifier;
            CliqueLifecyclePayload payload = new CliqueLifecyclePayload
            {
                IdolId = joinedIdol.id,
                CliqueLeaderId = leaderId,
                CliqueLeaderIdBefore = leaderId,
                CliqueLeaderIdAfter = leaderId,
                CliqueMemberCount = clique.Members.Count,
                CliqueSignature = cliqueSignature,
                CliqueQuitWasViolent = false
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    joinedIdol.id,
                    CoreConstants.EventEntityKindClique,
                    cliqueEntityIdentifier,
                    CoreConstants.EventTypeCliqueJoined,
                    CoreConstants.EventSourceCliqueAddMemberPatch,
                    CoreJsonUtility.SerializeCliqueLifecyclePayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Creates one clique-quit snapshot before member-removal mutation.
        /// </summary>
        internal CliqueQuitSnapshot CreateCliqueQuitSnapshot(Relationships._clique clique, data_girls.girls leavingIdol)
        {
            CliqueQuitSnapshot snapshot = new CliqueQuitSnapshot();
            if (clique == null || leavingIdol == null || clique.Members == null)
            {
                return snapshot;
            }

            snapshot.WasMemberBeforeQuit = clique.Members.Contains(leavingIdol);
            snapshot.PreviousLeaderId = ResolveCliqueLeaderIdOrInvalid(clique);
            return snapshot;
        }

        /// <summary>
        /// Captures one clique-leave event.
        /// </summary>
        internal void CaptureCliqueMemberLeft(
            Relationships._clique clique,
            data_girls.girls leavingIdol,
            bool wasViolent,
            CliqueQuitSnapshot quitSnapshot)
        {
            if (quitSnapshot == null
                || !quitSnapshot.WasMemberBeforeQuit
                || leavingIdol == null
                || leavingIdol.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            if (clique != null && clique.Members != null && clique.Members.Contains(leavingIdol))
            {
                return;
            }

            int leaderIdAfter = ResolveCliqueLeaderIdOrInvalid(clique);
            string cliqueSignature = BuildCliqueSignature(clique);
            string cliqueEntityIdentifier = !string.IsNullOrEmpty(cliqueSignature)
                ? cliqueSignature
                : CoreConstants.UnknownCliqueEntityIdentifier;
            int cliqueMemberCount = clique != null && clique.Members != null ? clique.Members.Count : CoreConstants.ZeroBasedListStartIndex;
            CliqueLifecyclePayload payload = new CliqueLifecyclePayload
            {
                IdolId = leavingIdol.id,
                CliqueLeaderId = leaderIdAfter,
                CliqueLeaderIdBefore = quitSnapshot.PreviousLeaderId,
                CliqueLeaderIdAfter = leaderIdAfter,
                CliqueMemberCount = cliqueMemberCount,
                CliqueSignature = cliqueSignature,
                CliqueQuitWasViolent = wasViolent
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    leavingIdol.id,
                    CoreConstants.EventEntityKindClique,
                    cliqueEntityIdentifier,
                    CoreConstants.EventTypeCliqueLeft,
                    CoreConstants.EventSourceCliqueQuitPatch,
                    CoreJsonUtility.SerializeCliqueLifecyclePayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one bullying-start event.
        /// </summary>
        internal void CaptureBullyingStarted(Relationships._clique clique, data_girls.girls targetIdol, bool wasBulliedBefore)
        {
            if (clique == null
                || targetIdol == null
                || targetIdol.id < CoreConstants.MinimumValidIdolIdentifier
                || wasBulliedBefore
                || !clique.IsBullied(targetIdol))
            {
                return;
            }

            int leaderId = ResolveCliqueLeaderIdOrInvalid(clique);
            string bullyingEntityIdentifier = BuildBullyingEntityIdentifier(leaderId, targetIdol.id);
            if (string.IsNullOrEmpty(bullyingEntityIdentifier))
            {
                bullyingEntityIdentifier = CoreConstants.UnknownBullyingEntityIdentifier;
            }

            BullyingLifecyclePayload payload = new BullyingLifecyclePayload
            {
                IdolId = targetIdol.id,
                BullyingTargetId = targetIdol.id,
                BullyingLeaderId = leaderId,
                BullyingKnownToPlayer = clique.KnownBulliedGirls != null && clique.KnownBulliedGirls.Contains(targetIdol),
                CliqueMemberCount = clique.Members != null ? clique.Members.Count : CoreConstants.ZeroBasedListStartIndex,
                CliqueSignature = BuildCliqueSignature(clique)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    targetIdol.id,
                    CoreConstants.EventEntityKindBullying,
                    bullyingEntityIdentifier,
                    CoreConstants.EventTypeBullyingStarted,
                    CoreConstants.EventSourceCliqueAddBulliedPatch,
                    CoreJsonUtility.SerializeBullyingLifecyclePayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one bullying-end event.
        /// </summary>
        internal void CaptureBullyingEnded(Relationships._clique clique, data_girls.girls targetIdol, bool wasBulliedBefore)
        {
            CaptureBullyingEnded(clique, targetIdol, wasBulliedBefore, CoreConstants.EventSourceCliqueStopBullyingPatch);
        }

        /// <summary>
        /// Captures one bullying-end event with explicit source patch code for multi-hook compatibility.
        /// </summary>
        internal void CaptureBullyingEnded(
            Relationships._clique clique,
            data_girls.girls targetIdol,
            bool wasBulliedBefore,
            string sourcePatchCode)
        {
            if (clique == null
                || targetIdol == null
                || targetIdol.id < CoreConstants.MinimumValidIdolIdentifier
                || !wasBulliedBefore
                || clique.IsBullied(targetIdol))
            {
                return;
            }

            int leaderId = ResolveCliqueLeaderIdOrInvalid(clique);
            string bullyingEntityIdentifier = BuildBullyingEntityIdentifier(leaderId, targetIdol.id);
            if (string.IsNullOrEmpty(bullyingEntityIdentifier))
            {
                bullyingEntityIdentifier = CoreConstants.UnknownBullyingEntityIdentifier;
            }

            BullyingLifecyclePayload payload = new BullyingLifecyclePayload
            {
                IdolId = targetIdol.id,
                BullyingTargetId = targetIdol.id,
                BullyingLeaderId = leaderId,
                BullyingKnownToPlayer = clique.KnownBulliedGirls != null && clique.KnownBulliedGirls.Contains(targetIdol),
                CliqueMemberCount = clique.Members != null ? clique.Members.Count : CoreConstants.ZeroBasedListStartIndex,
                CliqueSignature = BuildCliqueSignature(clique)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                string idempotencyKey = BuildBullyingLifecycleIdempotencyKey(
                    CoreConstants.EventTypeBullyingEnded,
                    bullyingEntityIdentifier,
                    targetIdol.id);
                if (!TryReserveIdempotencyKeyLocked(gameDate, idempotencyKey))
                {
                    return;
                }

                EnqueueEventRecordLocked(
                    gameDate,
                    targetIdol.id,
                    CoreConstants.EventEntityKindBullying,
                    bullyingEntityIdentifier,
                    CoreConstants.EventTypeBullyingEnded,
                    sourcePatchCode ?? CoreConstants.EventSourceCliqueStopBullyingPatch,
                    CoreJsonUtility.SerializeBullyingLifecyclePayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Creates one pre-mutation snapshot for relationship-level stop-bullying logic.
        /// </summary>
        internal RelationshipStopBullyingSnapshot CreateRelationshipStopBullyingSnapshot(Relationships._relationship relationship)
        {
            RelationshipStopBullyingSnapshot snapshot = new RelationshipStopBullyingSnapshot();
            if (relationship == null || relationship.Girls == null || relationship.Girls.Count < CoreConstants.MinimumRelationshipPairMemberCount)
            {
                return snapshot;
            }

            data_girls.girls firstIdol = relationship.Girls[CoreConstants.ZeroBasedListStartIndex];
            data_girls.girls secondIdol = relationship.Girls[CoreConstants.ZeroBasedListStartIndex + CoreConstants.LastElementOffsetFromCount];
            if (firstIdol != null && secondIdol != null)
            {
                snapshot.FirstClique = firstIdol.GetClique();
                snapshot.FirstTarget = secondIdol;
                snapshot.FirstWasBullied = snapshot.FirstClique != null && snapshot.FirstClique.IsBullied(secondIdol);

                snapshot.SecondClique = secondIdol.GetClique();
                snapshot.SecondTarget = firstIdol;
                snapshot.SecondWasBullied = snapshot.SecondClique != null && snapshot.SecondClique.IsBullied(firstIdol);
            }

            return snapshot;
        }

        /// <summary>
        /// Captures bullying-end events emitted by relationship-level stop-bullying flows.
        /// </summary>
        internal void CaptureRelationshipStopBullying(RelationshipStopBullyingSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            if (snapshot.FirstWasBullied)
            {
                CaptureBullyingEnded(
                    snapshot.FirstClique,
                    snapshot.FirstTarget,
                    true,
                    CoreConstants.EventSourceRelationshipStopBullyingPatch);
            }

            if (snapshot.SecondWasBullied)
            {
                CaptureBullyingEnded(
                    snapshot.SecondClique,
                    snapshot.SecondTarget,
                    true,
                    CoreConstants.EventSourceRelationshipStopBullyingPatch);
            }
        }

        /// <summary>
        /// Creates one scandal mitigation snapshot from popup selection data before mutation.
        /// </summary>
        internal ScandalMitigationSnapshot CreateScandalMitigationSnapshot()
        {
            ScandalMitigationSnapshot mitigationSnapshot = new ScandalMitigationSnapshot
            {
                PointsAvailableBefore = ScandalPoints_Popup.PointsAvailable
            };

            if (scandalPointsPopupPointsField == null)
            {
                return mitigationSnapshot;
            }

            IList<ScandalPoints_Popup._points> selectedPoints = scandalPointsPopupPointsField.GetValue(null) as IList<ScandalPoints_Popup._points>;
            if (selectedPoints == null)
            {
                return mitigationSnapshot;
            }

            for (int selectionIndex = CoreConstants.ZeroBasedListStartIndex; selectionIndex < selectedPoints.Count; selectionIndex++)
            {
                ScandalPoints_Popup._points selectedPoint = selectedPoints[selectionIndex];
                if (selectedPoint == null || selectedPoint.ToRemove <= CoreConstants.ZeroBasedListStartIndex)
                {
                    continue;
                }

                if (selectedPoint.Group)
                {
                    mitigationSnapshot.GroupPointsToRemove += selectedPoint.ToRemove;
                    continue;
                }

                if (selectedPoint.Girl == null || selectedPoint.Girl.id < CoreConstants.MinimumValidIdolIdentifier)
                {
                    continue;
                }

                int existingPointsToRemove;
                if (!mitigationSnapshot.IdolPointsToRemoveById.TryGetValue(selectedPoint.Girl.id, out existingPointsToRemove))
                {
                    existingPointsToRemove = CoreConstants.ZeroBasedListStartIndex;
                }

                mitigationSnapshot.IdolPointsToRemoveById[selectedPoint.Girl.id] = existingPointsToRemove + selectedPoint.ToRemove;
            }

            return mitigationSnapshot;
        }

        /// <summary>
        /// Captures scandal mitigation outcomes for both group points and idol-specific point removals.
        /// </summary>
        internal void CaptureScandalMitigation(ScandalMitigationSnapshot mitigationSnapshot)
        {
            if (mitigationSnapshot == null)
            {
                return;
            }

            bool hasGroupMitigation = mitigationSnapshot.GroupPointsToRemove > CoreConstants.ZeroBasedListStartIndex;
            bool hasIdolMitigation = mitigationSnapshot.IdolPointsToRemoveById != null
                && mitigationSnapshot.IdolPointsToRemoveById.Count >= CoreConstants.MinimumNonEmptyCollectionCount;
            if (!hasGroupMitigation && !hasIdolMitigation)
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

                DateTime gameDate = staticVars.dateTime;
                if (hasGroupMitigation)
                {
                    long currentGroupScandalPointsLong = resources.Get(resources.type.scandalPoints, false);
                    int currentGroupScandalPoints = ClampLongToInt(currentGroupScandalPointsLong);
                    ScandalMitigationPayload groupPayload = new ScandalMitigationPayload
                    {
                        IdolId = CoreConstants.InvalidIdValue,
                        ScandalPointsAvailableBefore = mitigationSnapshot.PointsAvailableBefore,
                        ScandalPointsRemoved = CoreConstants.ZeroBasedListStartIndex,
                        ScandalPointsBefore = CoreConstants.ZeroBasedListStartIndex,
                        ScandalPointsAfter = CoreConstants.ZeroBasedListStartIndex,
                        ScandalGroupPointsRemoved = mitigationSnapshot.GroupPointsToRemove,
                        ScandalGroupPointsRemaining = currentGroupScandalPoints
                    };

                    EnqueueEventRecordLocked(
                        gameDate,
                        CoreConstants.InvalidIdValue,
                        CoreConstants.EventEntityKindScandal,
                        CoreConstants.ScandalPointsPopupGroupEntityIdentifier,
                        CoreConstants.EventTypeScandalMitigated,
                        CoreConstants.EventSourceScandalPopupContinuePatch,
                        CoreJsonUtility.SerializeScandalMitigationPayload(groupPayload));
                }

                if (hasIdolMitigation)
                {
                    foreach (KeyValuePair<int, int> idolMitigationEntry in mitigationSnapshot.IdolPointsToRemoveById)
                    {
                        int idolId = idolMitigationEntry.Key;
                        int pointsRemoved = idolMitigationEntry.Value;
                        if (idolId < CoreConstants.MinimumValidIdolIdentifier || pointsRemoved <= CoreConstants.ZeroBasedListStartIndex)
                        {
                            continue;
                        }

                        data_girls.girls idol = data_girls.GetGirlByID(idolId);
                        int scandalPointsAfter = idol != null ? idol.GetScandalPoints() : CoreConstants.ZeroBasedListStartIndex;
                        int scandalPointsBefore = scandalPointsAfter + pointsRemoved;
                        ScandalMitigationPayload idolPayload = new ScandalMitigationPayload
                        {
                            IdolId = idolId,
                            ScandalPointsAvailableBefore = mitigationSnapshot.PointsAvailableBefore,
                            ScandalPointsRemoved = pointsRemoved,
                            ScandalPointsBefore = scandalPointsBefore,
                            ScandalPointsAfter = scandalPointsAfter,
                            ScandalGroupPointsRemoved = mitigationSnapshot.GroupPointsToRemove,
                            ScandalGroupPointsRemaining = CoreConstants.ZeroBasedListStartIndex
                        };

                        EnqueueEventRecordLocked(
                            gameDate,
                            idolId,
                            CoreConstants.EventEntityKindScandal,
                            idolId.ToString(CultureInfo.InvariantCulture),
                            CoreConstants.EventTypeScandalMitigated,
                            CoreConstants.EventSourceScandalPopupContinuePatch,
                            CoreJsonUtility.SerializeScandalMitigationPayload(idolPayload));
                    }
                }

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Creates one pre-mutation points snapshot for player-idol relationship updates.
        /// </summary>
        internal int CreatePlayerRelationshipPointsSnapshot(Relationships_Player._type relationshipType, data_girls.girls idol)
        {
            return ResolvePlayerRelationshipPoints(relationshipType, idol);
        }

        /// <summary>
        /// Captures one player-idol relationship points change event.
        /// </summary>
        internal void CapturePlayerRelationshipChanged(
            Relationships_Player._type relationshipType,
            data_girls.girls idol,
            int requestedPointsDelta,
            int previousPoints)
        {
            if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            int currentPoints = ResolvePlayerRelationshipPoints(relationshipType, idol);
            if (currentPoints == previousPoints)
            {
                return;
            }

            int levelBefore = Relationships_Player.GetLevelByPoints(previousPoints);
            int levelAfter = Relationships_Player.GetLevelByPoints(currentPoints);
            int appliedPointsDelta = currentPoints - previousPoints;
            string relationshipTypeCode = CoreEnumNameMapping.ToPlayerRelationshipTypeCode(relationshipType);
            PlayerRelationshipDeltaPayload payload = new PlayerRelationshipDeltaPayload
            {
                IdolId = idol.id,
                PlayerRelationshipType = relationshipTypeCode,
                PlayerPointsRequestedDelta = requestedPointsDelta,
                PlayerPointsAppliedDelta = appliedPointsDelta,
                PlayerPointsBefore = previousPoints,
                PlayerPointsAfter = currentPoints,
                PlayerLevelBefore = levelBefore,
                PlayerLevelAfter = levelAfter,
                PlayerLevelChanged = levelBefore != levelAfter
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    idol.id,
                    CoreConstants.EventEntityKindPlayerRelationship,
                    BuildPlayerRelationshipEntityIdentifier(idol.id, relationshipTypeCode),
                    CoreConstants.EventTypePlayerRelationshipChanged,
                    CoreConstants.EventSourcePlayerRelationshipAddPointsPatch,
                    CoreJsonUtility.SerializePlayerRelationshipDeltaPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Creates one pre-interaction snapshot for player date actions.
        /// </summary>
        internal PlayerDateInteractionSnapshot CreatePlayerDateInteractionSnapshot(data_girls.girls idol)
        {
            PlayerDateInteractionSnapshot interactionSnapshot = new PlayerDateInteractionSnapshot();
            if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return interactionSnapshot;
            }

            Dating._partner partner = idol.GetDatingData();
            interactionSnapshot.RouteBefore = partner != null
                ? CoreEnumNameMapping.ToDatingPartnerRouteCode(partner.Route)
                : CoreConstants.StatusCodeUnknown;
            interactionSnapshot.StageBefore = partner != null
                ? CoreEnumNameMapping.ToDatingPartnerRouteStageCode(partner.Progress)
                : CoreConstants.StatusCodeUnknown;
            interactionSnapshot.StatusBefore = partner != null
                ? CoreEnumNameMapping.ToDatingPartnerStatusCode(partner.Status)
                : CoreConstants.StatusCodeUnknown;
            interactionSnapshot.DateCaughtBefore = Dating.Data != null && Dating.Data.Caught;
            interactionSnapshot.RelationshipLevelBefore = idol.GetRelationshipLevel(Relationships_Player._type.Romance);
            return interactionSnapshot;
        }

        /// <summary>
        /// Captures one `Dating.GoOnDate` interaction event.
        /// </summary>
        internal void CapturePlayerDateInteractionFromGoOnDate(
            data_girls.girls idol,
            PlayerDateInteractionSnapshot interactionSnapshot,
            List<string> resultTokens)
        {
            CapturePlayerDateInteraction(
                idol,
                interactionSnapshot,
                CoreConstants.DateInteractionTypeGoOnDate,
                resultTokens,
                CoreConstants.EventSourcePlayerDateGoOnDatePatch);
        }

        /// <summary>
        /// Captures one `Dating.GoOnSpecificDate` interaction event.
        /// </summary>
        internal void CapturePlayerDateInteractionFromGoOnSpecificDate(
            data_girls.girls idol,
            PlayerDateInteractionSnapshot interactionSnapshot)
        {
            CapturePlayerDateInteraction(
                idol,
                interactionSnapshot,
                CoreConstants.DateInteractionTypeGoOnSpecificDate,
                null,
                CoreConstants.EventSourcePlayerDateGoOnSpecificDatePatch);
        }

        /// <summary>
        /// Captures one marriage-outcome snapshot after `Dating.Marriage_Girl_Quits`.
        /// </summary>
        internal void CapturePlayerMarriageOutcomeFromGirlQuits(data_girls.girls idol)
        {
            CapturePlayerMarriageOutcome(idol, CoreConstants.PlayerMarriageStageGirlQuits, CoreConstants.EventSourcePlayerMarriageGirlQuitsPatch);
        }

        /// <summary>
        /// Captures one marriage-outcome snapshot after `Dating.AfterMarriage`.
        /// </summary>
        internal void CapturePlayerMarriageOutcomeFromAfterMarriage(data_girls.girls idol)
        {
            CapturePlayerMarriageOutcome(idol, CoreConstants.PlayerMarriageStageAfterMarriage, CoreConstants.EventSourcePlayerMarriageAfterPatch);
        }

        /// <summary>
        /// Captures one player date interaction using pre/post partner state and result tokens.
        /// </summary>
        private void CapturePlayerDateInteraction(
            data_girls.girls idol,
            PlayerDateInteractionSnapshot interactionSnapshot,
            string interactionTypeCode,
            List<string> resultTokens,
            string sourcePatchCode)
        {
            if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            PlayerDateInteractionSnapshot safeSnapshot = interactionSnapshot ?? new PlayerDateInteractionSnapshot();
            Dating._partner partnerAfter = idol.GetDatingData();
            PlayerDateInteractionPayload payload = new PlayerDateInteractionPayload
            {
                IdolId = idol.id,
                DateInteractionType = interactionTypeCode ?? string.Empty,
                DateRouteBefore = safeSnapshot.RouteBefore ?? CoreConstants.StatusCodeUnknown,
                DateRouteAfter = partnerAfter != null
                    ? CoreEnumNameMapping.ToDatingPartnerRouteCode(partnerAfter.Route)
                    : CoreConstants.StatusCodeUnknown,
                DateStageBefore = safeSnapshot.StageBefore ?? CoreConstants.StatusCodeUnknown,
                DateStageAfter = partnerAfter != null
                    ? CoreEnumNameMapping.ToDatingPartnerRouteStageCode(partnerAfter.Progress)
                    : CoreConstants.StatusCodeUnknown,
                DateStatusBefore = safeSnapshot.StatusBefore ?? CoreConstants.StatusCodeUnknown,
                DateStatusAfter = partnerAfter != null
                    ? CoreEnumNameMapping.ToDatingPartnerStatusCode(partnerAfter.Status)
                    : CoreConstants.StatusCodeUnknown,
                DateResultToken = ResolveDateInteractionResultToken(resultTokens),
                DateCaughtBefore = safeSnapshot.DateCaughtBefore,
                DateCaughtAfter = Dating.Data != null && Dating.Data.Caught,
                DateRelationshipLevelBefore = safeSnapshot.RelationshipLevelBefore,
                DateRelationshipLevelAfter = idol.GetRelationshipLevel(Relationships_Player._type.Romance)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    idol.id,
                    CoreConstants.EventEntityKindPlayerDating,
                    idol.id.ToString(CultureInfo.InvariantCulture),
                    CoreConstants.EventTypePlayerDateInteraction,
                    sourcePatchCode,
                    CoreJsonUtility.SerializePlayerDateInteractionPayload(payload));

                FlushAfterCaptureLocked();
            }
        }

        /// <summary>
        /// Captures one player marriage outcome snapshot.
        /// </summary>
        private void CapturePlayerMarriageOutcome(data_girls.girls idol, string marriageStageCode, string sourcePatchCode)
        {
            if (idol == null || idol.id < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            Dating._partner partner = idol.GetDatingData();
            PlayerMarriageOutcomePayload payload = new PlayerMarriageOutcomePayload
            {
                IdolId = idol.id,
                MarriageStage = marriageStageCode ?? string.Empty,
                MarriageRoute = partner != null
                    ? CoreEnumNameMapping.ToDatingPartnerRouteCode(partner.Route)
                    : CoreConstants.StatusCodeUnknown,
                MarriagePartnerStatus = partner != null
                    ? CoreEnumNameMapping.ToDatingPartnerStatusCode(partner.Status)
                    : CoreConstants.StatusCodeUnknown,
                MarriageGoodOutcome = Dating.Good_Outcome,
                MarriageGirlQuitTriggered = Dating.Girl_Quit_Triggered,
                MarriageKidsString = ResolveVariableValueOrEmpty(CoreConstants.VariablesKeyKidsString),
                MarriageCareerStringOne = ResolveVariableValueOrEmpty(CoreConstants.VariablesKeyCareerStringOne),
                MarriageCareerStringTwo = ResolveVariableValueOrEmpty(CoreConstants.VariablesKeyCareerStringTwo),
                MarriageRelationshipOutcomeString = ResolveVariableValueOrEmpty(CoreConstants.VariablesKeyRelationshipOutcomeString),
                MarriageCustodyString = ResolveVariableValueOrEmpty(CoreConstants.VariablesKeyCustodyString),
                MarriageGraduationTrivia = idol.Graduation_Trivia_Text ?? string.Empty,
                MarriageIdolStatus = CoreStatusMapping.ToStatusCode(idol.status)
            };

            lock (runtimeLock)
            {
                string errorMessage;
                if (!EnsureInitializedLocked(out errorMessage))
                {
                    CoreLog.Warn(errorMessage);
                    return;
                }

                DateTime gameDate = staticVars.dateTime;
                EnqueueEventRecordLocked(
                    gameDate,
                    idol.id,
                    CoreConstants.EventEntityKindPlayerMarriage,
                    CoreConstants.PlayerMarriageEntityIdentifier,
                    CoreConstants.EventTypePlayerMarriageOutcome,
                    sourcePatchCode,
                    CoreJsonUtility.SerializePlayerMarriageOutcomePayload(payload));

                FlushAfterCaptureLocked();
            }
        }

    }
}
