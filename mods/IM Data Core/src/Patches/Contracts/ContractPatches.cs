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
    /// Captures accepted business proposals.
    /// </summary>
    [HarmonyPatch(typeof(business), nameof(business.Accept))]
    internal static class business_Accept_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures proposal snapshot before acceptance mutation.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(business __instance, out ContractAcceptedSnapshot __state)
        {
            ActivityEarningsSourceContext.Push(CoreConstants.EarningsSourceBusinessAccept);
            __state = IMDataCoreController.Instance.CreateContractAcceptedSnapshot(__instance);
            if (__state != null)
            {
                __state.SemanticScope = IMDataCoreController.Instance.BeginSemanticCaptureScope();
            }
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ContractAcceptedSnapshot __state)
        {
            SemanticCaptureScope scope = __state != null ? __state.SemanticScope : null;
            IMDataCoreController.Instance.EndSemanticCaptureScope(scope);
            try
            {
                IMDataCoreController.Instance.CaptureContractAccepted(__state);
                IMDataCoreController.Instance.CaptureBusinessProposalAccepted(
                    __state != null ? __state.ProposalOccurrence : null,
                    __state);
                IMDataCoreController.Instance.CommitSemanticCaptureScope(scope);
            }
            finally
            {
                IMDataCoreController.Instance.CompleteContractAcceptanceIdentity(__state);
            }
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        private static Exception Finalizer(Exception __exception, ContractAcceptedSnapshot __state)
        {
            if (__exception != null)
            {
                IMDataCoreController.Instance.AbortSemanticCaptureScope(__state != null ? __state.SemanticScope : null);
            }
            IMDataCoreController.Instance.CompleteContractAcceptanceIdentity(__state);
            ActivityEarningsSourceContext.Restore();
            return __exception;
        }
    }


    /// <summary>
    /// Captures proposal birth at the exact post-generation popup boundary.
    /// </summary>
    [HarmonyPatch(typeof(business), CoreConstants.HarmonyBusinessSetProposalMethodName, new Type[] { typeof(business._proposal) })]
    internal static class business_SetProposal_IMDataCoreCapture_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(business._proposal __0)
        {
            IMDataCoreController.Instance.CaptureBusinessProposalGenerated(__0);
        }
    }

    /// <summary>
    /// Captures the complete transient proposal before decline discards its history.
    /// </summary>
    [HarmonyPatch(typeof(business), nameof(business.Decline))]
    internal static class business_Decline_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(business __instance, out BusinessProposalTerminalSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateBusinessProposalTerminalSnapshot(
                __instance,
                false);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(BusinessProposalTerminalSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureBusinessProposalDeclined(__state);
        }
    }

    /// <summary>
    /// Captures contract activation events.
    /// </summary>
    [HarmonyPatch(typeof(business), nameof(business.AddActiveProposal))]
    internal static class business_AddActiveProposal_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures active-proposal count before insertion so the new row can be identified reliably.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(business __instance, out int __state)
        {
            __state = __instance != null && __instance.ActiveProposals != null
                ? __instance.ActiveProposals.Count
                : CoreConstants.ZeroBasedListStartIndex;
        }

        /// <summary>
        /// Records contract activation after business system appends active proposal.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(business __instance, business._proposal prop, int __state)
        {
            business.active_proposal addedActiveProposal = ResolveAddedActiveProposal(__instance, __state, prop);
            IMDataCoreController.Instance.BindContractActivationIdentity(
                __instance,
                addedActiveProposal,
                prop);
            IMDataCoreController.Instance.CaptureContractActivated(addedActiveProposal, prop);
        }

        /// <summary>
        /// Resolves the newly-added active proposal using before/after count and source proposal fallback.
        /// </summary>
        private static business.active_proposal ResolveAddedActiveProposal(business businessSystem, int previousActiveProposalCount, business._proposal sourceProposal)
        {
            if (businessSystem == null || businessSystem.ActiveProposals == null || businessSystem.ActiveProposals.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return null;
            }

            List<business.active_proposal> activeProposals = businessSystem.ActiveProposals;
            List<business.active_proposal> appendedCandidates = ResolveAppendedProposalCandidates(activeProposals, previousActiveProposalCount);
            if (appendedCandidates.Count == CoreConstants.MinimumNonEmptyCollectionCount)
            {
                business.active_proposal singleAppendedCandidate = appendedCandidates[CoreConstants.ZeroBasedListStartIndex];
                if (sourceProposal == null || IsRelaxedSourceProposalMatch(singleAppendedCandidate, sourceProposal))
                {
                    return singleAppendedCandidate;
                }
            }

            business.active_proposal sourceMatchedAppendedCandidate = ResolveUniqueSourceMatchedCandidate(appendedCandidates, sourceProposal);
            if (sourceMatchedAppendedCandidate != null)
            {
                return sourceMatchedAppendedCandidate;
            }

            return null;
        }

        /// <summary>
        /// Returns non-null active proposals that were appended after prefix snapshot count.
        /// </summary>
        private static List<business.active_proposal> ResolveAppendedProposalCandidates(List<business.active_proposal> activeProposals, int previousActiveProposalCount)
        {
            List<business.active_proposal> appendedCandidates = new List<business.active_proposal>();
            if (activeProposals == null || activeProposals.Count < CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return appendedCandidates;
            }

            int startIndex = previousActiveProposalCount;
            if (startIndex < CoreConstants.ZeroBasedListStartIndex)
            {
                startIndex = CoreConstants.ZeroBasedListStartIndex;
            }

            if (startIndex > activeProposals.Count)
            {
                startIndex = activeProposals.Count;
            }

            for (int proposalIndex = startIndex; proposalIndex < activeProposals.Count; proposalIndex++)
            {
                business.active_proposal proposalCandidate = activeProposals[proposalIndex];
                if (proposalCandidate != null)
                {
                    appendedCandidates.Add(proposalCandidate);
                }
            }

            return appendedCandidates;
        }

        /// <summary>
        /// Resolves a unique active proposal candidate that matches source proposal data.
        /// </summary>
        private static business.active_proposal ResolveUniqueSourceMatchedCandidate(IList<business.active_proposal> candidateProposals, business._proposal sourceProposal)
        {
            if (candidateProposals == null || candidateProposals.Count < CoreConstants.MinimumNonEmptyCollectionCount || sourceProposal == null)
            {
                return null;
            }

            List<business.active_proposal> strictMatches = new List<business.active_proposal>();
            for (int candidateIndex = CoreConstants.ZeroBasedListStartIndex; candidateIndex < candidateProposals.Count; candidateIndex++)
            {
                business.active_proposal candidateProposal = candidateProposals[candidateIndex];
                if (IsStrictSourceProposalMatch(candidateProposal, sourceProposal))
                {
                    strictMatches.Add(candidateProposal);
                }
            }

            if (strictMatches.Count == CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return strictMatches[CoreConstants.ZeroBasedListStartIndex];
            }

            if (strictMatches.Count > CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return null;
            }

            List<business.active_proposal> relaxedMatches = new List<business.active_proposal>();
            for (int candidateIndex = CoreConstants.ZeroBasedListStartIndex; candidateIndex < candidateProposals.Count; candidateIndex++)
            {
                business.active_proposal candidateProposal = candidateProposals[candidateIndex];
                if (IsRelaxedSourceProposalMatch(candidateProposal, sourceProposal))
                {
                    relaxedMatches.Add(candidateProposal);
                }
            }

            if (relaxedMatches.Count == CoreConstants.MinimumNonEmptyCollectionCount)
            {
                return relaxedMatches[CoreConstants.ZeroBasedListStartIndex];
            }

            return null;
        }

        /// <summary>
        /// Returns true when candidate values exactly match source proposal fields.
        /// </summary>
        private static bool IsStrictSourceProposalMatch(business.active_proposal candidateProposal, business._proposal sourceProposal)
        {
            if (!IsRelaxedSourceProposalMatch(candidateProposal, sourceProposal))
            {
                return false;
            }

            if (candidateProposal.Payment_per_week != sourceProposal.payment
                || candidateProposal.Buzz_per_week != sourceProposal.buzz
                || candidateProposal.Fame_per_week != sourceProposal.fame
                || candidateProposal.Fans_per_week != sourceProposal.newFans
                || candidateProposal.Stamina_per_week != sourceProposal.stamina
                || candidateProposal.Liability != sourceProposal.liability)
            {
                return false;
            }

            if (!string.Equals(candidateProposal.Agent_Name ?? string.Empty, sourceProposal.agentName ?? string.Empty, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.Equals(candidateProposal.Product_Name ?? string.Empty, sourceProposal.productName ?? string.Empty, StringComparison.Ordinal))
            {
                return false;
            }

            DateTime expectedEndDate = staticVars.dateTime.AddMonths(sourceProposal.duration);
            return candidateProposal.EndDate == expectedEndDate;
        }

        /// <summary>
        /// Returns true when candidate values match stable identity fields from source proposal.
        /// </summary>
        private static bool IsRelaxedSourceProposalMatch(business.active_proposal candidateProposal, business._proposal sourceProposal)
        {
            if (candidateProposal == null || sourceProposal == null)
            {
                return false;
            }

            if (candidateProposal.Type != sourceProposal.type
                || candidateProposal.Skill != sourceProposal.skill
                || candidateProposal.isGroup != sourceProposal.isGroup)
            {
                return false;
            }

            if (sourceProposal.girl != null && candidateProposal.Girl != sourceProposal.girl)
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Captures naturally finished contracts removed by daily proposal maintenance.
    /// </summary>
    [HarmonyPatch(typeof(business), CoreConstants.HarmonyBusinessCheckActiveProposalsMethodName)]
    internal static class business_CheckActiveProposals_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures contracts that are about to expire and be removed.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(business __instance, out List<business.active_proposal> __state)
        {
            __state = IMDataCoreController.Instance.CreateContractsNaturalCompletionSnapshot(__instance);
        }

        /// <summary>
        /// Records one contract-finished event per naturally expired proposal.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(List<business.active_proposal> __state)
        {
            IMDataCoreController.Instance.CaptureContractsNaturallyFinished(__state);
        }
    }

    /// <summary>
    /// Captures weekly contract payment accrual events.
    /// </summary>
    [HarmonyPatch(typeof(business), nameof(business.AddWeeklyEarnings))]
    internal static class business_AddWeeklyEarnings_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix()
        {
            ActivityEarningsSourceContext.Push(CoreConstants.EarningsSourceBusinessWeekly);
        }

        /// <summary>
        /// Records one payment-accrual event per active contract after weekly payout handling.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(business __instance)
        {
            IMDataCoreController.Instance.CaptureContractWeeklyEarnings(__instance);
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        private static Exception Finalizer(Exception __exception)
        {
            ActivityEarningsSourceContext.Restore();
            return __exception;
        }
    }

    /// <summary>
    /// Captures weekly contract fan/fame/training accrual events.
    /// </summary>
    [HarmonyPatch(typeof(business), nameof(business.DoWeeklyFans))]
    internal static class business_DoWeeklyFans_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Records one benefits-accrual event per active contract after weekly fan/fame handling.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(business __instance)
        {
            IMDataCoreController.Instance.CaptureContractWeeklyBenefits(__instance);
        }
    }

    /// <summary>
    /// Captures contract cancellation events.
    /// </summary>
    [HarmonyPatch(typeof(business), nameof(business.CancelContract))]
    internal static class business_CancelContract_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures membership and immutable identity/payload facts before vanilla mutates the list.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(
            business __instance,
            business.active_proposal _Proposal,
            bool Damages,
            out ContractCancellationSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateContractCancellationSnapshot(
                __instance,
                _Proposal,
                Damages);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ContractCancellationSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureContractCancelled(__state);
            IMDataCoreController.Instance.RetireContractIdentityIfRemoved(
                __state != null ? __state.BusinessSystem : null,
                __state != null ? __state.ActiveContract : null);
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        private static Exception Finalizer(
            Exception __exception,
            business __instance,
            business.active_proposal _Proposal)
        {
            if (__exception != null)
            {
                IMDataCoreController.Instance.RetireContractIdentityIfRemoved(
                    __instance,
                    _Proposal);
            }

            return __exception;
        }
    }

    /// <summary>
    /// Captures `BreakContracts(data_girls.girls)` liability removal events.
    /// </summary>
    [HarmonyPatch(typeof(business), nameof(business.BreakContracts), new Type[] { typeof(data_girls.girls) })]
    internal static class business_BreakContracts_Idol_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures every contract before the game removes active rows.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(
            business __instance,
            data_girls.girls _girl,
            out List<ContractBreakSnapshot> __state)
        {
            __state = IMDataCoreController.Instance.CreateContractBreakSnapshotsForIdol(
                __instance,
                _girl,
                CoreConstants.ContractBreakContextSingleIdol);
        }

        /// <summary>
        /// Records contract-break event after liabilities are applied by game code.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(List<ContractBreakSnapshot> __state)
        {
            IMDataCoreController.Instance.CaptureContractBreakSnapshots(
                __state,
                CoreConstants.EventSourceContractBreakSingleIdolPatch);
        }
    }

    /// <summary>
    /// Captures `BreakContracts(List<actor>)` liability removal events.
    /// </summary>
    [HarmonyPatch(typeof(business), nameof(business.BreakContracts), new Type[] { typeof(List<Event_Manager._activeEvent._actor>) })]
    internal static class business_BreakContracts_Actors_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures every actor contract before active rows are removed.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(
            business __instance,
            List<Event_Manager._activeEvent._actor> Actors,
            out List<ContractBreakSnapshot> __state)
        {
            __state = IMDataCoreController.Instance.CreateContractBreakSnapshotsForActors(
                __instance,
                Actors,
                CoreConstants.ContractBreakContextEventActors);
        }

        /// <summary>
        /// Records per-idol contract-break events after liabilities are applied.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(List<ContractBreakSnapshot> __state)
        {
            IMDataCoreController.Instance.CaptureContractBreakSnapshots(
                __state,
                CoreConstants.EventSourceContractBreakEventActorsPatch);
        }
    }

    /// <summary>
    /// Rebinds staged v6 contract generations after vanilla reconstructs its
    /// active-proposal list in serialized ordinal order.
    /// </summary>
    [HarmonyPatch(typeof(business), nameof(business.LoadFunction))]
    internal static class business_LoadFunction_IMDataCoreContractIdentity_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(business __instance)
        {
            IMDataCoreController.Instance.AssociateLoadedContractIdentities(__instance);
        }
    }

}
