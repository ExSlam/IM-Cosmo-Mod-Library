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
            ActivityEarningsSourceContext.Set(CoreConstants.EarningsSourceBusinessAccept);
            __state = IMDataCoreController.Instance.CreateContractAcceptedSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ContractAcceptedSnapshot __state)
        {
            try
            {
                IMDataCoreController.Instance.CaptureContractAccepted(__state);
            }
            finally
            {
                ActivityEarningsSourceContext.Clear();
            }
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        private static Exception Finalizer(Exception __exception)
        {
            ActivityEarningsSourceContext.Clear();
            return __exception;
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
            ActivityEarningsSourceContext.Set(CoreConstants.EarningsSourceBusinessWeekly);
        }

        /// <summary>
        /// Records one payment-accrual event per active contract after weekly payout handling.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(business __instance)
        {
            try
            {
                IMDataCoreController.Instance.CaptureContractWeeklyEarnings(__instance);
            }
            finally
            {
                ActivityEarningsSourceContext.Clear();
            }
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        private static Exception Finalizer(Exception __exception)
        {
            ActivityEarningsSourceContext.Clear();
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
        /// Records cancellation before the contract row is removed.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(business.active_proposal _Proposal, bool Damages)
        {
            IMDataCoreController.Instance.CaptureContractCancelled(_Proposal, Damages);
        }
    }

    /// <summary>
    /// Captures `BreakContracts(data_girls.girls)` liability removal events.
    /// </summary>
    [HarmonyPatch(typeof(business), nameof(business.BreakContracts), new Type[] { typeof(data_girls.girls) })]
    internal static class business_BreakContracts_Idol_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures total broken liability before the game removes active contracts.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(business __instance, data_girls.girls _girl, out long __state)
        {
            __state = IMDataCoreController.Instance.CreateContractBreakLiabilitySnapshotForIdol(__instance, _girl);
        }

        /// <summary>
        /// Records contract-break event after liabilities are applied by game code.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls _girl, long __state)
        {
            IMDataCoreController.Instance.CaptureContractBrokenForIdol(
                _girl,
                __state,
                CoreConstants.ContractBreakContextSingleIdol,
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
        /// Captures per-idol liability snapshot before contract rows are removed.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(
            business __instance,
            List<Event_Manager._activeEvent._actor> Actors,
            out Dictionary<int, long> __state)
        {
            __state = IMDataCoreController.Instance.CreateContractBreakLiabilitySnapshotForActors(__instance, Actors);
        }

        /// <summary>
        /// Records per-idol contract-break events after liabilities are applied.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Dictionary<int, long> __state)
        {
            IMDataCoreController.Instance.CaptureContractBrokenForActorBatch(
                __state,
                CoreConstants.ContractBreakContextEventActors,
                CoreConstants.EventSourceContractBreakEventActorsPatch);
        }
    }

    /// <summary>
}
