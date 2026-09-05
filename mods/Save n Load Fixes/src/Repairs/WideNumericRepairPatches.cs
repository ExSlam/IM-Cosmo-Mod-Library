using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    [HarmonyPatch]
    internal static class WideNumeric_ResourcesAdd_Patch
    {
        private static MethodBase TargetMethod()
        {
            return WideNumericPatchHealth.ResolveTarget(
                "resources._Add(resources.type,Int64)",
                typeof(resources),
                "_Add",
                new[] { typeof(resources.type), typeof(long) },
                typeof(void),
                false);
        }

        /// <summary>
        /// Precompute the exact current-value plus delta before any mutation, then reproduce
        /// vanilla's nonnegative/buzz clamps, fan distribution, statistics, and observer
        /// order with the checked Int64 result as the authoritative value.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(
            resources.type __0,
            long __1,
            resources.money ___onMoneyChange,
            resources.fans ___onFansChange,
            resources.scandalPoints ___onScandalPointsChange,
            resources.fame ___onFameChange,
            resources.resourceChanged ___onResourceChange)
        {
            WideNumericContinuation.AddResource(
                __0,
                __1,
                ___onMoneyChange,
                ___onFansChange,
                ___onScandalPointsChange,
                ___onFameChange,
                ___onResourceChange);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_FanAddPeople_Patch
    {
        private static MethodBase TargetMethod()
        {
            return WideNumericPatchHealth.ResolveTarget(
                "resources._fan.AddPeople(Int64)",
                typeof(resources._fan),
                nameof(resources._fan.AddPeople),
                new[] { typeof(long) },
                typeof(void),
                false);
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(resources._fan __instance, long __0)
        {
            WideNumericRepair.ValidateFanPeopleAdd(__instance, __0);
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_FansByType_Patch
    {
        private static MethodBase TargetMethod()
        {
            Type nullableFanType = typeof(Nullable<resources.fanType>);
            return WideNumericPatchHealth.ResolveTarget(
                "resources.FansByType(Nullable,Nullable,Nullable)",
                typeof(resources),
                nameof(resources.FansByType),
                new[] { nullableFanType, nullableFanType, nullableFanType },
                typeof(long),
                true);
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(
            resources.fanType? __0,
            resources.fanType? __1,
            resources.fanType? __2,
            ref long __result)
        {
            __result = WideNumericRepair.CalculateFansByType(__0, __1, __2);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GetLegacyFansTotal_Patch
    {
        private static MethodBase TargetMethod()
        {
            return WideNumericPatchHealth.ResolveTarget(
                "resources.GetFansTotal_Legacy()",
                typeof(resources),
                nameof(resources.GetFansTotal_Legacy),
                Type.EmptyTypes,
                typeof(long),
                true);
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(ref long __result)
        {
            __result = WideNumericRepair.CalculateLegacyFanTotal();
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GetFansTotal_Patch
    {
        private static MethodBase TargetMethod()
        {
            return WideNumericPatchHealth.ResolveTarget(
                "resources.GetFansTotal(Nullable)",
                typeof(resources),
                nameof(resources.GetFansTotal),
                new[] { typeof(Nullable<resources.fanType>) },
                typeof(long),
                true);
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(resources.fanType? __0, ref long __result)
        {
            __result = WideNumericRepair.CalculateActiveIdolFanTotal(__0);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TheaterTicketSales_Patch
    {
        private static MethodBase TargetMethod()
        {
            return WideNumericPatchHealth.ResolveTarget(
                "Theaters._theater.GetTicketSales()",
                typeof(Theaters._theater),
                nameof(Theaters._theater.GetTicketSales),
                Type.EmptyTypes,
                typeof(long),
                false);
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(Theaters._theater __instance, ref long __result)
        {
            __result = WideNumericRepair.CalculateTicketSales(__instance);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ShowLongTotal_Patch
    {
        private static MethodBase TargetMethod()
        {
            return WideNumericPatchHealth.ResolveTarget(
                "Shows._show.GetTotalParam(List<Int64>)",
                typeof(Shows._show),
                nameof(Shows._show.GetTotalParam),
                new[] { typeof(List<long>) },
                typeof(long),
                false);
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(List<long> __0, ref long __result)
        {
            __result = WideNumericRepair.SumShowLongValues(__0);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ShowTotalProfit_Patch
    {
        private static MethodBase TargetMethod()
        {
            return WideNumericPatchHealth.ResolveTarget(
                "Shows._show.GetTotalProfit()",
                typeof(Shows._show),
                nameof(Shows._show.GetTotalProfit),
                Type.EmptyTypes,
                typeof(long),
                false);
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(Shows._show __instance, ref long __result)
        {
            __result = WideNumericRepair.CalculateShowTotalProfit(__instance);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_LoanTotalAvailable_Patch
    {
        private static MethodBase TargetMethod()
        {
            return WideNumericPatchHealth.ResolveTarget(
                "loans.GetTotalAvailableAmount(_type)",
                typeof(loans),
                nameof(loans.GetTotalAvailableAmount),
                new[] { typeof(loans._loan._type) },
                typeof(long),
                true);
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(loans._loan._type __0, ref long __result)
        {
            __result = WideNumericRepair.CalculateTotalAvailableLoanAmount(__0);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_LoanAmountAvailable_Patch
    {
        private static MethodBase TargetMethod()
        {
            return WideNumericPatchHealth.ResolveTarget(
                "loans.GetAmountAvailableForLoan(_type)",
                typeof(loans),
                nameof(loans.GetAmountAvailableForLoan),
                new[] { typeof(loans._loan._type) },
                typeof(long),
                true);
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(loans._loan._type __0, ref long __result)
        {
            __result = WideNumericRepair.CalculateAmountAvailableForLoan(__0);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_LoanTotalDebt_Patch
    {
        private static MethodBase TargetMethod()
        {
            return WideNumericPatchHealth.ResolveTarget(
                "loans.GetTotalDebt()",
                typeof(loans),
                nameof(loans.GetTotalDebt),
                Type.EmptyTypes,
                typeof(long),
                true);
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(ref long __result)
        {
            __result = WideNumericRepair.CalculateTotalDebt();
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessMoneyEarned_Patch
    {
        private static MethodBase TargetMethod()
        {
            return WideNumericPatchHealth.ResolveTarget(
                "business.active_proposal.GetMoneyEarned()",
                typeof(business.active_proposal),
                nameof(business.active_proposal.GetMoneyEarned),
                Type.EmptyTypes,
                typeof(long),
                false);
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(
            business.active_proposal __instance,
            ref long __result)
        {
            __result = WideNumericRepair.CalculateBusinessMoneyEarned(__instance);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessLiability_Patch
    {
        private static MethodBase TargetMethod()
        {
            return WideNumericPatchHealth.ResolveTarget(
                "business._proposal.GetLiability()",
                typeof(business._proposal),
                "GetLiability",
                Type.EmptyTypes,
                typeof(long),
                false);
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(business._proposal __instance, ref long __result)
        {
            __result = WideNumericRepair.CalculateBusinessLiability(__instance);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessAcceptHistory_Patch
    {
        private static MethodBase TargetMethod()
        {
            return WideNumericPatchHealth.ResolveTarget(
                "business.Accept()",
                typeof(business),
                nameof(business.Accept),
                Type.EmptyTypes,
                typeof(void),
                false);
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(
            business __instance,
            out WideBusinessAcceptState __state)
        {
            // Preflight the complete wide value before Accept performs any mutation.
            __state = WideNumericRepair.PrepareBusinessAccept(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.First)]
        private static void Postfix(WideBusinessAcceptState __state)
        {
            WideNumericRepair.CompleteBusinessAccept(__state);
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GroupBuzz_Patch
    {
        private static MethodBase TargetMethod()
        {
            return WideNumericPatchHealth.ResolveTarget(
                "data_girls.AddBuzz()",
                typeof(data_girls),
                nameof(data_girls.AddBuzz),
                Type.EmptyTypes,
                typeof(void),
                false);
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix()
        {
            WideNumericRepair.AddGroupBuzz();
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ConcertProductionCost_Patch
    {
        private static MethodBase TargetMethod()
        {
            return WideNumericPatchHealth.ResolveTarget(
                "SEvent_Concerts._concert._projectedValues.GetProductionCost()",
                typeof(SEvent_Concerts._concert._projectedValues),
                nameof(SEvent_Concerts._concert._projectedValues.GetProductionCost),
                Type.EmptyTypes,
                typeof(long),
                false);
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(
            SEvent_Concerts._concert._projectedValues __instance,
            ref long __result)
        {
            __result = WideNumericRepair.CalculateConcertProductionCost(__instance);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_StaffSeverance_Patch
    {
        private static MethodBase TargetMethod()
        {
            return WideNumericPatchHealth.ResolveTarget(
                "Staff_Fire.GetSeverance()",
                typeof(Staff_Fire),
                nameof(Staff_Fire.GetSeverance),
                Type.EmptyTypes,
                typeof(int),
                true);
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(ref int __result)
        {
            __result = WideNumericRepair.CalculateSeveranceCompatibility();
            return false;
        }
    }
}
