using System;
using System.Collections.Generic;
using HarmonyLib;

namespace UnavailableIdolsFix
{
    internal sealed class BusinessProposalFilterState
    {
        internal bool Active;
        internal bool SkipOriginal;
        internal BusinessProposalFilterState Previous;
    }

    internal static class BusinessProposalFilterScope
    {
        [ThreadStatic]
        private static BusinessProposalFilterState current;

        internal static bool Active
        {
            get { return current != null && current.Active; }
        }

        internal static void Enter(BusinessProposalFilterState state)
        {
            state.Previous = current;
            current = state;
        }

        internal static void Exit(BusinessProposalFilterState state)
        {
            if (current == state)
            {
                current = state.Previous;
            }
        }
    }

    [HarmonyPatch(typeof(business), "GenerateProposal")]
    internal static class PersistentBusinessProposalScopePatch
    {
        private static bool Prefix(business._data dat, out BusinessProposalFilterState __state)
        {
            bool persistentContract = dat != null && (dat.business_type == business._type.ad || dat.business_type == business._type.tv_drama);
            __state = new BusinessProposalFilterState
            {
                Active = persistentContract
            };
            BusinessProposalFilterScope.Enter(__state);

            if (!persistentContract)
            {
                return true;
            }

            bool hasEligibleIdol = false;
            if (data_girls.girl != null)
            {
                foreach (data_girls.girls girl in data_girls.girl)
                {
                    if (girl != null && girl.status != data_girls._status.announced_graduation && girl.IsActive())
                    {
                        hasEligibleIdol = true;
                        break;
                    }
                }
            }

            __state.SkipOriginal = !hasEligibleIdol;
            return hasEligibleIdol;
        }

        private static void Postfix(BusinessProposalFilterState __state)
        {
            BusinessProposalFilterScope.Exit(__state);
        }

        private static Exception Finalizer(Exception __exception, BusinessProposalFilterState __state)
        {
            BusinessProposalFilterScope.Exit(__state);
            return __exception;
        }
    }

    [HarmonyPatch(typeof(data_girls), nameof(data_girls.GetActiveGirls))]
    internal static class BusinessActiveGirlCandidatePatch
    {
        private static void Postfix(ref List<data_girls.girls> __result)
        {
            if (!BusinessProposalFilterScope.Active || __result == null)
            {
                return;
            }

            __result.RemoveAll(girl => girl == null || girl.status == data_girls._status.announced_graduation);
        }
    }

    [HarmonyPatch(typeof(Pushes), nameof(Pushes.GetRandomPushedGirl))]
    internal static class BusinessPushedGirlCandidatePatch
    {
        private static void Postfix(ref data_girls.girls __result)
        {
            if (BusinessProposalFilterScope.Active && __result != null && __result.status == data_girls._status.announced_graduation)
            {
                __result = null;
            }
        }
    }
}
