using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    [HarmonyPatch]
    internal static class WideNumeric_ResourcesSet_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "resources.Set(type,Int64)", typeof(resources), nameof(resources.Set),
            new Type[] { typeof(resources.type), typeof(long) }, typeof(void), false); }
        [HarmonyAfter(TelModLibraryInterop.GoingViralOwner)]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(
            resources.type __0,
            long __1,
            resources.money ___onMoneySet,
            resources.fans ___onFansSet,
            resources.scandalPoints ___onScandalPointsSet,
            resources.fame ___onFameSet,
            resources.resourceChanged ___onResourceChange)
        {
            WideNumericContinuation.SetResource(
                __0, __1, ___onMoneySet, ___onFansSet, ___onScandalPointsSet,
                ___onFameSet, ___onResourceChange);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ScandalTotal_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "resources.GetScandalPointsTotal()", typeof(resources),
            nameof(resources.GetScandalPointsTotal), Type.EmptyTypes, typeof(long), true); }
        private static bool Prefix(ref long __result)
        { __result = WideNumericContinuation.GetScandalPointsTotal(); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ScandalCounter_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "resources.UpdateScandalPointsCounter()", typeof(resources),
            nameof(resources.UpdateScandalPointsCounter), Type.EmptyTypes,
            typeof(void), true); }
        private static bool Prefix()
        {
            WideNumericContinuation.ReportScandalPointsChanged();
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ScandalCompatibility_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "ScandalPoints.GetScandalPoints()", typeof(ScandalPoints),
            nameof(ScandalPoints.GetScandalPoints), Type.EmptyTypes, typeof(int), true); }
        private static bool Prefix(ref int __result)
        { __result = WideNumericContinuation.GetScandalPointsCompatibility(); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ScandalTooltip_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "ScandalPoints.GetPointsTooltip()", typeof(ScandalPoints),
            nameof(ScandalPoints.GetPointsTooltip), Type.EmptyTypes, typeof(string), true); }
        private static bool Prefix(ref string __result)
        { __result = WideNumericContinuation.GetScandalPointsTooltip(); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ScandalLine_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "ScandalPoints_Line.Set(_points)", typeof(ScandalPoints_Line),
            nameof(ScandalPoints_Line.Set),
            new Type[] { typeof(ScandalPoints_Popup._points) }, typeof(void), false); }
        private static void Postfix(ScandalPoints_Line __instance)
        { WideNumericContinuation.CorrectScandalPointsLine(__instance); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ResourceDisplaySetInt_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "ResourceDisplay.OnResourceSet(Int32)", typeof(ResourceDisplay),
            "OnResourceSet", new Type[] { typeof(int) }, typeof(void), false); }
        private static bool Prefix(ResourceDisplay __instance)
        { return !WideNumericContinuation.ForwardExactResourceDisplay(__instance); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ResourceDisplayChangeInt_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "ResourceDisplay.OnResourceChange(Int32)", typeof(ResourceDisplay),
            "OnResourceChange", new Type[] { typeof(int) }, typeof(void), false); }
        private static bool Prefix(ResourceDisplay __instance)
        { return !WideNumericContinuation.ForwardExactResourceDisplay(__instance); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_DatingJobSearch_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Dating.JobSearch_1()", typeof(Dating), "JobSearch_1",
            Type.EmptyTypes, typeof(void), true); }
        private static bool Prefix()
        { WideNumericContinuation.RunDatingJobSearch(); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourFamePenalty_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Tour.GetFamePenalty(Int32)", typeof(SEvent_Tour),
            nameof(SEvent_Tour.GetFamePenalty), new Type[] { typeof(int) },
            typeof(float), true); }
        private static bool Prefix(int __0, ref float __result)
        { __result = WideNumericContinuation.GetTourFamePenalty(__0); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ShowReleaseFame_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Show_Release.SetFame()", typeof(Show_Release), "SetFame",
            Type.EmptyTypes, typeof(void), false); }
        private static void Postfix(Show_Release __instance)
        { WideNumericContinuation.CorrectShowReleaseFame(__instance); }
    }

    internal static class WideNumericTargets
    {
        internal static MethodBase Resolve(string id, Type type, string name,
            Type[] parameters, Type result, bool isStatic)
        {
            return WideNumericContinuationPatchHealth.Resolve(
                id, type, name, parameters, result, isStatic);
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_RentPerFloor_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "resources.GetRentPerFloor(Int32)", typeof(resources), nameof(resources.GetRentPerFloor),
            new Type[] { typeof(int) }, typeof(int), true); }
        private static bool Prefix(int __0, ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericContinuation.GetRentPerFloor(__0)); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_RoomRent_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "resources.GetRoomRent(_type,Int32)", typeof(resources), nameof(resources.GetRoomRent),
            new Type[] { typeof(agency._type), typeof(int) }, typeof(int), true); }
        private static bool Prefix(agency._type __0, int __1, ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericContinuation.GetRoomRent(__0, __1)); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_MoneyRent_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "resources.Money_Rent(Boolean)", typeof(resources), nameof(resources.Money_Rent),
            new Type[] { typeof(bool) }, typeof(int), false); }
        private static bool Prefix(bool __0, ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericContinuation.GetTotalRent(__0)); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_StaffSalary_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "resources.Money_StaffSalary()", typeof(resources), nameof(resources.Money_StaffSalary),
            Type.EmptyTypes, typeof(long), false); }
        private static bool Prefix(ref long __result)
        { __result = WideNumericContinuation.GetStaffSalary(); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GirlSalary_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "resources.Money_GirlsSalary()", typeof(resources), nameof(resources.Money_GirlsSalary),
            Type.EmptyTypes, typeof(long), false); }
        private static bool Prefix(ref long __result)
        { __result = WideNumericContinuation.GetGirlSalary(); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_WeeklyExpenses_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "resources.Money_WeeklyExpenses()", typeof(resources), nameof(resources.Money_WeeklyExpenses),
            Type.EmptyTypes, typeof(long), false); }
        private static bool Prefix(ref long __result)
        { __result = WideNumericContinuation.GetWeeklyExpenses(); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_DailyBusinessProfit_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "resources.Money_DailyProfit()", typeof(resources), nameof(resources.Money_DailyProfit),
            Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(resources __instance, ref int __result)
        { __result = WideNumericContinuation.DailyBusinessProfitCompatibility(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ResourcesOnNewDay_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "resources.OnNewDay()", typeof(resources), nameof(resources.OnNewDay),
            Type.EmptyTypes, typeof(void), false); }
        private static bool Prefix(resources __instance)
        { WideNumericContinuation.ResourcesOnNewDay(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ResourcesOnNewWeek_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "resources.OnNewWeek()", typeof(resources), nameof(resources.OnNewWeek),
            Type.EmptyTypes, typeof(void), false); }
        private static bool Prefix(resources __instance)
        { WideNumericContinuation.ResourcesOnNewWeek(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessWeeklyProfit_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "business.GetTotalWeeklyProfit()", typeof(business), nameof(business.GetTotalWeeklyProfit),
            Type.EmptyTypes, typeof(long), false); }
        private static bool Prefix(business __instance, ref long __result)
        { __result = WideNumericContinuation.GetBusinessWeeklyProfit(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessWeeklyBuzz_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "business.GetTotalWeeklyBuzz()", typeof(business), nameof(business.GetTotalWeeklyBuzz),
            Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(business __instance, ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericContinuation.GetBusinessWeeklyBuzz(__instance)); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessWeeklyFame_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "business.GetTotalWeeklyFame()", typeof(business), nameof(business.GetTotalWeeklyFame),
            Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(business __instance, ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericContinuation.GetBusinessWeeklyFame(__instance)); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessWeeklyEarnings_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "business.AddWeeklyEarnings()", typeof(business), nameof(business.AddWeeklyEarnings),
            Type.EmptyTypes, typeof(void), false); }
        private static bool Prefix(business __instance)
        { WideNumericContinuation.AddBusinessWeeklyEarnings(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessProposalPaymentGetter_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "business._proposal.get_payment()", typeof(business._proposal), "get_payment",
            Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(business._proposal __instance, ref int __result)
        { __result = WideNumericContinuation.GetBusinessProposalPaymentCompatibility(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessProposalPaymentSetter_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "business._proposal.set_payment(Int32)", typeof(business._proposal), "set_payment",
            new Type[] { typeof(int) }, typeof(void), false); }
        private static void Postfix(business._proposal __instance, int __0)
        { WideNumericContinuation.SetBusinessProposalPaymentCompatibility(__instance, __0); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessProposalFansGetter_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "business._proposal.get_newFans()", typeof(business._proposal), "get_newFans",
            Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(business._proposal __instance, ref int __result)
        { __result = WideNumericContinuation.GetBusinessProposalFansCompatibility(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessProposalFansSetter_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "business._proposal.set_newFans(Int32)", typeof(business._proposal), "set_newFans",
            new Type[] { typeof(int) }, typeof(void), false); }
        private static void Postfix(business._proposal __instance, int __0)
        { WideNumericContinuation.SetBusinessProposalFansCompatibility(__instance, __0); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessGenerateProposalPayment_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "business.GenerateProposal(_data,_staff,Int32)", typeof(business), "GenerateProposal",
            new Type[] { typeof(business._data), typeof(staff._staff), typeof(int) },
            typeof(void), false); }

        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            MethodInfo floor = AccessTools.Method(typeof(Mathf), nameof(Mathf.FloorToInt),
                new Type[] { typeof(float) });
            MethodInfo setter = AccessTools.PropertySetter(typeof(business._proposal), "payment");
            MethodInfo helper = AccessTools.Method(typeof(WideNumericContinuation),
                nameof(WideNumericContinuation.SetGeneratedBusinessProposalPayment));
            if (floor == null || setter == null || helper == null)
                throw new MissingMethodException(
                    "A33.4 business proposal payment widening could not resolve its audited methods.");

            int replacements = 0;
            for (int i = 3; i + 1 < code.Count; i++)
            {
                if (!code[i].Calls(floor) || !code[i + 1].Calls(setter) ||
                    code[i - 1].opcode != OpCodes.Mul ||
                    code[i - 3].opcode != OpCodes.Conv_R4 ||
                    !IsLoadLocal(code[i - 2].opcode))
                    continue;

                int start = i - 3;
                int end = i + 1;
                CodeInstruction coefficientLoad = new CodeInstruction(
                    code[i - 2].opcode, code[i - 2].operand);
                for (int j = start; j <= end; j++)
                {
                    coefficientLoad.labels.AddRange(code[j].labels);
                    coefficientLoad.blocks.AddRange(code[j].blocks);
                }
                code.RemoveRange(start, end - start + 1);
                code.Insert(start, coefficientLoad);
                code.Insert(start + 1, new CodeInstruction(OpCodes.Call, helper));
                replacements++;
                i = start + 1;
            }

            if (replacements != 1)
                throw new InvalidOperationException(
                    "A33.4 expected exactly one business proposal payment FloorToInt/set_payment sequence, found " +
                    replacements + ".");
            return code;
        }

        private static bool IsLoadLocal(OpCode opcode)
        {
            return opcode == OpCodes.Ldloc || opcode == OpCodes.Ldloc_S ||
                opcode == OpCodes.Ldloc_0 || opcode == OpCodes.Ldloc_1 ||
                opcode == OpCodes.Ldloc_2 || opcode == OpCodes.Ldloc_3;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessGenerateProposalFans_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "business.GenerateProposal(_data,_staff,Int32)-fans", typeof(business), "GenerateProposal",
            new Type[] { typeof(business._data), typeof(staff._staff), typeof(int) },
            typeof(void), false); }

        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            MethodInfo floor = AccessTools.Method(typeof(Mathf), nameof(Mathf.FloorToInt),
                new Type[] { typeof(float) });
            MethodInfo setter = AccessTools.PropertySetter(typeof(business._proposal), "newFans");
            MethodInfo helper = AccessTools.Method(typeof(WideNumericContinuation),
                nameof(WideNumericContinuation.SetGeneratedBusinessProposalFans));
            if (floor == null || setter == null || helper == null)
                throw new MissingMethodException(
                    "A33.7 business proposal fan widening could not resolve its audited methods.");

            int replacements = 0;
            for (int i = 3; i + 1 < code.Count; i++)
            {
                if (!code[i].Calls(floor) || !code[i + 1].Calls(setter) ||
                    code[i - 1].opcode != OpCodes.Mul ||
                    code[i - 3].opcode != OpCodes.Conv_R4 ||
                    !IsLoadLocal(code[i - 2].opcode))
                    continue;

                int start = i - 3;
                int end = i + 1;
                CodeInstruction coefficientLoad = new CodeInstruction(
                    code[i - 2].opcode, code[i - 2].operand);
                for (int j = start; j <= end; j++)
                {
                    coefficientLoad.labels.AddRange(code[j].labels);
                    coefficientLoad.blocks.AddRange(code[j].blocks);
                }
                code.RemoveRange(start, end - start + 1);
                code.Insert(start, coefficientLoad);
                code.Insert(start + 1, new CodeInstruction(OpCodes.Call, helper));
                replacements++;
                i = start + 1;
            }

            if (replacements != 1)
                throw new InvalidOperationException(
                    "A33.7 expected exactly one business proposal fan FloorToInt/set_newFans sequence, found " +
                    replacements + ".");
            return code;
        }

        private static bool IsLoadLocal(OpCode opcode)
        {
            return opcode == OpCodes.Ldloc || opcode == OpCodes.Ldloc_S ||
                opcode == OpCodes.Ldloc_0 || opcode == OpCodes.Ldloc_1 ||
                opcode == OpCodes.Ldloc_2 || opcode == OpCodes.Ldloc_3;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessAddActiveProposal_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "business.AddActiveProposal(_proposal)", typeof(business), nameof(business.AddActiveProposal),
            new Type[] { typeof(business._proposal) }, typeof(void), false); }
        private static void Prefix(business __instance, out int __state)
        { __state = __instance == null || __instance.ActiveProposals == null ? -1 : __instance.ActiveProposals.Count; }
        private static void Postfix(business __instance, business._proposal __0, int __state)
        { if (__state >= 0) WideNumericContinuation.CompleteBusinessActiveProposalAdd(__instance, __0, __state); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessPopupPayment_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Business_Popup.Set(_proposal)", typeof(Business_Popup), nameof(Business_Popup.Set),
            new Type[] { typeof(business._proposal) }, typeof(void), false); }
        private static void Postfix(Business_Popup __instance, business._proposal __0)
        {
            WideNumericContinuation.CorrectBusinessPopupPayment(__instance, __0);
            WideNumericContinuation.CorrectBusinessPopupFans(__instance, __0);
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ContractsLinePayment_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Contracts_Line.Set(active_proposal)", typeof(Contracts_Line), nameof(Contracts_Line.Set),
            new Type[] { typeof(business.active_proposal) }, typeof(void), false); }
        private static void Postfix(Contracts_Line __instance, business.active_proposal __0)
        {
            WideNumericContinuation.CorrectBusinessContractPaymentLine(__instance, __0);
            WideNumericContinuation.CorrectBusinessContractFansLine(__instance, __0);
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessProposalFans_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "business.AddFans(_proposal)", typeof(business), "AddFans",
            new Type[] { typeof(business._proposal) }, typeof(void), false); }
        private static bool Prefix(business._proposal __0)
        {
            if (__0 == null) return true;
            long exact = WideNumericContinuation.GetBusinessProposalFans(__0);
            if (exact <= 16777216L) return true;
            WideNumericContinuation.AddBusinessProposalFans(__0); return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessActiveProposalFans_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "business.AddFans(active_proposal)", typeof(business), "AddFans",
            new Type[] { typeof(business.active_proposal) }, typeof(void), false); }
        private static bool Prefix(business.active_proposal __0)
        {
            if (__0 == null) return true;
            long exact = WideNumericState.GetBusinessContractFans(__0);
            if (exact <= 16777216L) return true;
            WideNumericContinuation.AddBusinessWeeklyFans(__0); return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessDoWeeklyFans_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "business.DoWeeklyFans()", typeof(business), nameof(business.DoWeeklyFans),
            Type.EmptyTypes, typeof(void), false); }
        private static bool Prefix(business __instance)
        {
            if (!WideNumericContinuation.BusinessWeeklyFansNeedWidePath(__instance)) return true;
            WideNumericContinuation.DoBusinessWeeklyFansWide(__instance);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_RivalsMonthlyGrowth_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Rivals.OnNewMonth(Boolean)", typeof(Rivals), "OnNewMonth",
            new Type[] { typeof(bool) }, typeof(void), false); }

        private static void Prefix()
        {
            WideNumericContinuation.PreflightRivalMonth();
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            MethodInfo vanillaRound = AccessTools.Method(typeof(Mathf), nameof(Mathf.RoundToInt),
                new Type[] { typeof(float) });
            MethodInfo wideRound = AccessTools.Method(typeof(WideNumericContinuation),
                nameof(WideNumericContinuation.RoundRivalGrowth),
                new Type[] { typeof(long), typeof(float) });
            int replacements = 0;
            for (int index = 0; index + 4 < codes.Count; index++)
            {
                if (codes[index].opcode != OpCodes.Conv_R4 ||
                    codes[index + 2].opcode != OpCodes.Mul ||
                    !codes[index + 3].Calls(vanillaRound) ||
                    codes[index + 4].opcode != OpCodes.Conv_I8)
                    continue;
                codes[index].opcode = OpCodes.Nop;
                codes[index].operand = null;
                codes[index + 2].opcode = OpCodes.Call;
                codes[index + 2].operand = wideRound;
                codes[index + 3].opcode = OpCodes.Nop;
                codes[index + 3].operand = null;
                codes[index + 4].opcode = OpCodes.Nop;
                codes[index + 4].operand = null;
                replacements++;
            }
            if (replacements != 2)
            {
                WideNumericContinuationPatchHealth.ReportFailure(
                    "A33.3 expected exactly two Rivals.OnNewMonth Int64-to-Single growth sites, found " +
                    replacements + ".");
                throw new InvalidOperationException("Unexpected Rivals.OnNewMonth IL shape.");
            }
            return codes;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ResourcesDailyBuzz_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "resources.Buzz_Daily()", typeof(resources), nameof(resources.Buzz_Daily),
            Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(resources __instance, ref int __result)
        {
            business owner = Camera.main.GetComponent<mainScript>().Data.GetComponent<business>();
            __result = WideNumericMath.ClampToInt32(WideNumericContinuation.GetBusinessDailyBuzz(owner));
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ResourcesDailyFame_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "resources.Fame_Daily()", typeof(resources), nameof(resources.Fame_Daily),
            Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(resources __instance, ref int __result)
        {
            business owner = Camera.main.GetComponent<mainScript>().Data.GetComponent<business>();
            __result = WideNumericMath.ClampToInt32(WideNumericContinuation.GetBusinessDailyFame(owner));
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ResourcesDailyFansChange_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "resources.DailyFansChange()", typeof(resources), "DailyFansChange",
            Type.EmptyTypes, typeof(void), false); }
        private static bool Prefix(resources __instance)
        { WideNumericContinuation.ResourcesDailyFansChange(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_LoanWeeklyPayment_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "loans.GetTotalPaymentPerWeek()", typeof(loans), nameof(loans.GetTotalPaymentPerWeek),
            Type.EmptyTypes, typeof(int), true); }
        private static bool Prefix(ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericContinuation.GetTotalLoanPayment()); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_LoanInterest_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "loans._loan.GetInterest()", typeof(loans._loan), nameof(loans._loan.GetInterest),
            Type.EmptyTypes, typeof(long), false); }
        private static bool Prefix(loans._loan __instance, ref long __result)
        { __result = WideNumericContinuation.GetLoanInterest(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_LoanTotalAmount_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "loans._loan.GetTotalAmount()", typeof(loans._loan), nameof(loans._loan.GetTotalAmount),
            Type.EmptyTypes, typeof(long), false); }
        private static bool Prefix(loans._loan __instance, ref long __result)
        { __result = WideNumericContinuation.GetLoanTotalAmount(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_LoanDebt_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "loans._loan.GetDebt()", typeof(loans._loan), nameof(loans._loan.GetDebt),
            Type.EmptyTypes, typeof(long), false); }
        private static bool Prefix(loans._loan __instance, ref long __result)
        { __result = WideNumericContinuation.GetLoanDebt(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_LoanRecalculatePayment_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "loans._loan.RecalcPaymentPerWeek()", typeof(loans._loan), nameof(loans._loan.RecalcPaymentPerWeek),
            Type.EmptyTypes, typeof(void), false); }
        private static bool Prefix(loans._loan __instance)
        { WideNumericContinuation.RecalculateLoanPayment(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_LoanIdGuard_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "loans.GetNewLoanID()", typeof(loans), nameof(loans.GetNewLoanID),
            Type.EmptyTypes, typeof(int), true); }
        private static void Prefix() { WideNumericContinuation.GuardNextId(loans.LastLoanID, "loans.GetNewLoanID"); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TheaterSubscribers_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Theaters._theater.GetSubscribers()", typeof(Theaters._theater), nameof(Theaters._theater.GetSubscribers),
            Type.EmptyTypes, typeof(long), false); }
        private static bool Prefix(Theaters._theater __instance, ref long __result)
        { __result = WideNumericState.GetTheaterSubscribers(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TheaterSubRevenue_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Theaters._theater.GetSubRevenue()", typeof(Theaters._theater), nameof(Theaters._theater.GetSubRevenue),
            Type.EmptyTypes, typeof(long), false); }
        private static bool Prefix(Theaters._theater __instance, ref long __result)
        { __result = WideNumericContinuation.GetTheaterSubscriptionRevenue(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TheaterNewSubscribers_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Theaters._theater.GetNewSubscribers()", typeof(Theaters._theater), nameof(Theaters._theater.GetNewSubscribers),
            Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(Theaters._theater __instance, ref int __result)
        { __result = WideNumericContinuation.AddTheaterSubscribers(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TheaterLastWeek_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Theaters.GetLastWeekEarning()", typeof(Theaters), nameof(Theaters.GetLastWeekEarning),
            Type.EmptyTypes, typeof(long), true); }
        private static bool Prefix(ref long __result)
        { __result = WideNumericContinuation.GetTheaterLastWeekEarning(); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TheaterAverageRevenue_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Theaters._theater.GetAvgRevenue()", typeof(Theaters._theater),
            nameof(Theaters._theater.GetAvgRevenue), Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(Theaters._theater __instance, ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericContinuation.GetTheaterAverageRevenue(__instance)); return false; }

        [HarmonyAfter(TelModLibraryInterop.UnofficialPatchOwner)]
        private static void Postfix(Theaters._theater __instance, ref int __result)
        {
            if (!TelModLibraryInterop.UnofficialPatchLoaded) return;
            __result = WideNumericMath.ClampToInt32(
                WideNumericContinuation.GetEffectiveTheaterAverageRevenue(__instance));
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TheaterVisitors_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Theaters._theater.GetNumberOfVisitors()", typeof(Theaters._theater),
            nameof(Theaters._theater.GetNumberOfVisitors), Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(Theaters._theater __instance, ref int __result)
        { __result = WideNumericContinuation.GetTheaterVisitors(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TheaterCompleteDay_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Theaters.CompleteDay()", typeof(Theaters), "CompleteDay",
            Type.EmptyTypes, typeof(void), false); }
        private static void Prefix() { WideNumericContinuation.PreflightTheaterDay(); }
        private static void Postfix() { WideNumericContinuation.AssociateTheaterStatsAfterDay(); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_StatsOnNewWeek_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Stats.OnNewWeek()", typeof(Stats), "OnNewWeek", Type.EmptyTypes, typeof(void), false); }
        private static bool Prefix() { WideNumericContinuation.StatsOnNewWeek(); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_StatsIncome_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Stats.data.money.GetTotalIncome(Int32)", typeof(Stats.data.money), nameof(Stats.data.money.GetTotalIncome),
            new Type[] { typeof(int) }, typeof(long), true); }
        private static bool Prefix(int __0, ref long __result)
        { __result = WideNumericContinuation.GetStatsTotalIncome(__0); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_StoryCh3_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "tasks._story_data.Set_Ch3_Aya_Fans()", typeof(tasks._story_data), nameof(tasks._story_data.Set_Ch3_Aya_Fans),
            Type.EmptyTypes, typeof(void), false); }
        private static bool Prefix() { WideNumericContinuation.SetStoryCh3Target(); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_StoryCh4Create_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "tasks.AddTask_SummerGames(String)", typeof(tasks),
            nameof(tasks.AddTask_SummerGames), new Type[] { typeof(string) },
            typeof(void), true); }
        private static bool Prefix(string __0)
        { WideNumericContinuation.AddSummerGamesTask(__0); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_StoryCh4Qualify_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "tasks._story_data.Ch4_Did_Qualify()", typeof(tasks._story_data), nameof(tasks._story_data.Ch4_Did_Qualify),
            Type.EmptyTypes, typeof(bool), false); }
        private static bool Prefix(tasks._story_data __instance, ref bool __result)
        { __result = WideNumericContinuation.StoryCh4DidQualify(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_CafeFansNeeded_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Cafes.GetFansNeededToBuild(Int32)", typeof(Cafes), nameof(Cafes.GetFansNeededToBuild),
            new Type[] { typeof(int) }, typeof(int), true); }
        private static bool Prefix(int __0, ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericContinuation.GetCafeFansNeeded(__0)); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_CafeLastWeek_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Cafes.GetLastWeekEarning()", typeof(Cafes), nameof(Cafes.GetLastWeekEarning),
            Type.EmptyTypes, typeof(int), true); }
        private static bool Prefix(ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericContinuation.GetCafeLastWeekEarning()); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_CafeRenderRooms_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Cafes.RenderRooms(Boolean)", typeof(Cafes), nameof(Cafes.RenderRooms),
            new Type[] { typeof(bool) }, typeof(void), true); }
        private static bool Prefix(bool __0)
        { WideNumericContinuation.RenderCafeRooms(__0); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_CafeMoneyPerDay_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Cafes.GetMoneyPerDay()", typeof(Cafes), nameof(Cafes.GetMoneyPerDay),
            Type.EmptyTypes, typeof(int), true); }
        private static bool Prefix(ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericContinuation.GetCafeMoneyPerDay()); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_CafeTooltip_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Cafes.GetTooltip()", typeof(Cafes), nameof(Cafes.GetTooltip),
            Type.EmptyTypes, typeof(string), true); }
        private static bool Prefix(ref string __result)
        {
            if (Cafes.CountCafes() == 0) return true;
            __result = WideNumericContinuation.GetCafeTooltip(); return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_SingleTotalSales_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "singles._single.GetTotalSales()", typeof(singles._single), nameof(singles._single.GetTotalSales),
            Type.EmptyTypes, typeof(long), false); }
        private static bool Prefix(singles._single __instance, ref long __result)
        { __result = WideNumericContinuation.GetSingleTotalSales(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_SingleTotalNewFans_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "singles._single.GetTotalNewFans()", typeof(singles._single), nameof(singles._single.GetTotalNewFans),
            Type.EmptyTypes, typeof(long), false); }
        private static bool Prefix(singles._single __instance, ref long __result)
        { __result = WideNumericContinuation.GetSingleTotalNewFans(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_SingleMoney_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "singles._single.GetMoney()", typeof(singles._single), nameof(singles._single.GetMoney),
            Type.EmptyTypes, typeof(long), false); }
        private static bool Prefix(singles._single __instance, ref long __result)
        { __result = WideNumericContinuation.GetSingleMoney(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_SingleGenerateSales_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "singles.GenerateSales(_single)", typeof(singles), nameof(singles.GenerateSales),
            new Type[] { typeof(singles._single) }, typeof(void), false); }
        [HarmonyAfter(TelModLibraryInterop.MbtiOwner, TelModLibraryInterop.SisterGroupsOwner)]
        private static bool Prefix(singles __instance, singles._single __0)
        {
            if (!WideNumericContinuation.SingleNeedsWideSalesPath(__instance, __0)) return true;
            WideNumericContinuation.GenerateSingleSalesWide(__instance, __0); return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_SingleMarketing_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "singles.ValueAfterMarketing(Int64,Single,Single,_fan,_special_type,_result)",
            typeof(singles), "ValueAfterMarketing",
            new Type[] { typeof(long), typeof(float), typeof(float), typeof(resources._fan),
                typeof(singles._param._special_type), typeof(Single_Marketing_Roll._result) },
            typeof(long), false); }
        private static bool Prefix(long __0, float __1, float __2, resources._fan __3,
            singles._param._special_type __4, Single_Marketing_Roll._result __5,
            ref long __result)
        {
            __result = WideNumericContinuation.ValueAfterSingleMarketing(
                __0, __1, __2, __3, __4, __5);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ShowAverageLong_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Shows._show.GetAverageParam(List<Int64>)", typeof(Shows._show), nameof(Shows._show.GetAverageParam),
            new Type[] { typeof(List<long>) }, typeof(long), false); }
        private static bool Prefix(List<long> __0, ref long __result)
        { __result = WideNumericContinuation.AverageShowLongValues(__0); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ShowAverageInt_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Shows._show.GetAverageParam(List<Int32>)", typeof(Shows._show), nameof(Shows._show.GetAverageParam),
            new Type[] { typeof(List<int>) }, typeof(int), false); }
        private static bool Prefix(List<int> __0, ref int __result)
        { __result = WideNumericContinuation.AverageShowIntValues(__0); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ShowTotalInt_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Shows._show.GetTotalParam(List<Int32>)", typeof(Shows._show), nameof(Shows._show.GetTotalParam),
            new Type[] { typeof(List<int>) }, typeof(int), false); }
        private static bool Prefix(List<int> __0, ref int __result)
        { __result = WideNumericContinuation.SumShowIntValues(__0); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ShowAllNewFans_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Shows._show.GetAllNewFans()", typeof(Shows._show), "GetAllNewFans",
            Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(Shows._show __instance, ref int __result)
        { __result = WideNumericContinuation.SumShowFansCompatibility(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GroupFansDemographic_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Groups._group.GetFansOfType(demographic)", typeof(Groups._group), nameof(Groups._group.GetFansOfType),
            new Type[] { typeof(resources.fanType), typeof(resources.fanType), typeof(resources.fanType) },
            typeof(int), false); }
        private static bool Prefix(Groups._group __instance, resources.fanType __0,
            resources.fanType __1, resources.fanType __2, ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericRepair.CalculateGroupFansByType(__instance, __0, __1, __2)); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GroupFansNullable_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Groups._group.GetFansOfType(Nullable)", typeof(Groups._group), nameof(Groups._group.GetFansOfType),
            new Type[] { typeof(resources.fanType?) }, typeof(int), false); }
        private static bool Prefix(Groups._group __instance, resources.fanType? __0, ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericRepair.CalculateGroupFansByType(__instance, __0)); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourProfit_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Tour.tour.GetProfit()", typeof(SEvent_Tour.tour), nameof(SEvent_Tour.tour.GetProfit),
            Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(SEvent_Tour.tour __instance, ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericState.GetTourProfit(__instance)); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourAddRevenue_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Tour.tour.AddRevenue(Int32)", typeof(SEvent_Tour.tour), nameof(SEvent_Tour.tour.AddRevenue),
            new Type[] { typeof(int) }, typeof(void), false); }
        private static bool Prefix(SEvent_Tour.tour __instance, int __0)
        {
            long adjusted = TbsBalancePatchWideNumericInterop.ApplyTourRevenueMultiplier(__0);
            WideNumericState.AddTourRevenue(__instance, adjusted);
            if (__instance.Update != null) __instance.Update();
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourAddFans_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Tour.tour.AddFans(Int32)", typeof(SEvent_Tour.tour), nameof(SEvent_Tour.tour.AddFans),
            new Type[] { typeof(int) }, typeof(void), false); }
        private static bool Prefix(SEvent_Tour.tour __instance, int __0)
        { WideNumericState.AddTourFans(__instance, __0); if (__instance.Update != null) __instance.Update(); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourSelectCountry_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Tour.tour.SelectCountry(country,Int32)", typeof(SEvent_Tour.tour), nameof(SEvent_Tour.tour.SelectCountry),
            new Type[] { typeof(SEvent_Tour.country), typeof(int) }, typeof(void), false); }
        [HarmonyAfter(TelModLibraryInterop.TourStaminaOwner)]
        private static bool Prefix(SEvent_Tour.tour __instance, SEvent_Tour.country __0, int __1)
        { WideNumericContinuation.SelectTourCountry(__instance, __0, __1); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourAttendance_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Tour.tour.GetAttendance(selectedCountry)", typeof(SEvent_Tour.tour),
            nameof(SEvent_Tour.tour.GetAttendance),
            new Type[] { typeof(SEvent_Tour.tour.selectedCountry) }, typeof(int), false); }
        private static bool Prefix(SEvent_Tour.tour __instance,
            SEvent_Tour.tour.selectedCountry __0, ref int __result)
        { __result = WideNumericContinuation.GetTourAttendance(__instance, __0); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourNewFansByAttendance_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Tour.tour.GetNewFansByAttendance(Int32)", typeof(SEvent_Tour.tour),
            nameof(SEvent_Tour.tour.GetNewFansByAttendance),
            new Type[] { typeof(int) }, typeof(int), false); }
        private static bool Prefix(int __0, ref int __result)
        { __result = WideNumericContinuation.GetTourNewFansByAttendance(__0); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourProductionCost_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Tour.tour.GetProductionCost()", typeof(SEvent_Tour.tour), nameof(SEvent_Tour.tour.GetProductionCost),
            Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(SEvent_Tour.tour __instance, ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericState.GetTourNetProductionCost(__instance)); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourEnoughMoney_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Tour.tour.IsEnoughMoney()", typeof(SEvent_Tour.tour), nameof(SEvent_Tour.tour.IsEnoughMoney),
            Type.EmptyTypes, typeof(bool), false); }
        private static bool Prefix(SEvent_Tour.tour __instance, ref bool __result)
        { long cost = WideNumericState.GetTourProductionCost(__instance); __result = staticVars.IsEasy() || cost == 0L || cost <= resources.Money(); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourFinish_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Tour.FinishTour()", typeof(SEvent_Tour), nameof(SEvent_Tour.FinishTour),
            Type.EmptyTypes, typeof(void), false); }
        [HarmonyAfter(TelModLibraryInterop.TraitsFixOwner)]
        private static bool Prefix(SEvent_Tour __instance)
        { WideNumericContinuation.FinishTour(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourIdGuard_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Tour.tour.Initiate()", typeof(SEvent_Tour.tour), nameof(SEvent_Tour.tour.Initiate),
            Type.EmptyTypes, typeof(void), false); }
        private static void Prefix() { WideNumericContinuation.GuardNextId(SEvent_Tour.Last_Tour_ID, "SEvent_Tour.tour.Initiate"); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_AddFansEquallyDemographic_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "data_girls.AddFans_Equally(Int64,_fan,List)", typeof(data_girls), nameof(data_girls.AddFans_Equally),
            new Type[] { typeof(long), typeof(resources._fan), typeof(List<data_girls.girls>) },
            typeof(void), true); }
        [HarmonyAfter(TelModLibraryInterop.GoingViralOwner)]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(long __0, resources._fan __1, List<data_girls.girls> __2)
        {
            if (!WideNumericContinuation.NeedsWideFanPath(__0) &&
                !WideNumericContinuation.FanDemographicDistributionMayOverflow(
                    __0, __1, __2)) return true;
            WideNumericContinuation.AddFansEqually(__0, __1, __2); return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_AddFansEqually_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "data_girls.AddFans_Equally(Int64,List)", typeof(data_girls), nameof(data_girls.AddFans_Equally),
            new Type[] { typeof(long), typeof(List<data_girls.girls>) }, typeof(void), true); }
        private static bool Prefix(long __0, List<data_girls.girls> __1)
        {
            if (!WideNumericContinuation.NeedsWideFanPath(__0) &&
                !WideNumericContinuation.FanDistributionMayOverflow(
                    __0, null, __1, null)) return true;
            WideNumericContinuation.AddFansEqually(__0, __1); return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_AddFansWeighted_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "data_girls.AddFans(Int64,Nullable,List,girl)", typeof(data_girls), nameof(data_girls.AddFans),
            new Type[] { typeof(long), typeof(resources.fanType?), typeof(List<data_girls.girls>),
                typeof(data_girls.girls) }, typeof(void), true); }
        [HarmonyAfter(TelModLibraryInterop.GoingViralOwner)]
        private static bool Prefix(long __0, resources.fanType? __1,
            List<data_girls.girls> __2, data_girls.girls __3)
        {
            if (!WideNumericContinuation.NeedsWideFanPath(__0) &&
                !WideNumericContinuation.FanDistributionMayOverflow(
                    __0, __1, __2, __3)) return true;
            WideNumericContinuation.AddFansWeighted(__0, __1, __2, __3); return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_AddFansOshihen_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "data_girls.AddFans_Oshihen(Int64,girl)", typeof(data_girls), nameof(data_girls.AddFans_Oshihen),
            new Type[] { typeof(long), typeof(data_girls.girls) }, typeof(void), true); }
        private static bool Prefix(long __0, data_girls.girls __1)
        {
            if (!WideNumericContinuation.NeedsWideFanPath(__0) &&
                !WideNumericContinuation.FanDistributionMayOverflow(
                    __0, null, null, __1)) return true;
            WideNumericContinuation.Oshihen(__0, __1); return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GirlAddFans_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "data_girls.girls.AddFans(Int64,Nullable)", typeof(data_girls.girls), nameof(data_girls.girls.AddFans),
            new Type[] { typeof(long), typeof(resources.fanType?) }, typeof(void), false); }
        [HarmonyAfter(TelModLibraryInterop.GoingViralOwner)]
        private static bool Prefix(data_girls.girls __instance, ref long __0, resources.fanType? __1)
        {
            long cafeExact;
            if (WideNumericContinuation.TryResolveCafeFanAddition(
                    __instance, __0, __1, out cafeExact))
            {
                WideNumericContinuation.AddGirlFans(__instance, cafeExact, __1);
                return false;
            }

            if (!WideNumericContinuation.NeedsWideFanPath(__0) &&
                !WideNumericContinuation.GirlFanAdditionMayOverflow(
                    __instance, __0, __1))
            {
                long effective = BuffMeWideNumericInterop.PreviewDirectGirlFanDelta(__0);
                if (!WideNumericContinuation.NeedsWideFanPath(effective) &&
                    !WideNumericContinuation.GirlFanAdditionMayOverflow(
                        __instance, effective, __1))
                {
                    __0 = BuffMeWideNumericInterop.ApplyDirectGirlFanDelta(__0);
                    return true;
                }
            }

            WideNumericContinuation.AddGirlFans(__instance, __0, __1);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GirlAddFansDemographic_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "data_girls.girls.AddFans(demographic,Int64)", typeof(data_girls.girls), nameof(data_girls.girls.AddFans),
            new Type[] { typeof(resources.fanType), typeof(resources.fanType), typeof(resources.fanType), typeof(long) },
            typeof(void), false); }
        private static bool Prefix(data_girls.girls __instance, resources.fanType __0,
            resources.fanType __1, resources.fanType __2, long __3)
        {
            if (!WideNumericContinuation.NeedsWideFanPath(__3) &&
                !WideNumericContinuation.GirlDemographicAdditionMayOverflow(
                    __instance, __3, __0, __1, __2)) return true;
            WideNumericContinuation.AddGirlFansDemographic(__instance, __0, __1, __2, __3); return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GirlGetFansToAdd_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "data_girls.girls.GetFansToAdd(Int64,Single)", typeof(data_girls.girls), nameof(data_girls.girls.GetFansToAdd),
            new Type[] { typeof(long), typeof(float) }, typeof(long), false); }
        private static bool Prefix(data_girls.girls __instance, long __0, float __1, ref long __result)
        {
            if (!WideNumericContinuation.NeedsWideFanPath(__0)) return true;
            __result = WideNumericContinuation.GetGirlFansToAdd(__instance, __0, __1); return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GirlAddFansCoefficient_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "data_girls.girls.AddFans(Nullable,Single,Boolean)", typeof(data_girls.girls), nameof(data_girls.girls.AddFans),
            new Type[] { typeof(resources.fanType?), typeof(float), typeof(bool) }, typeof(void), false); }
        private static bool Prefix(data_girls.girls __instance, resources.fanType? __0,
            float __1, bool __2)
        {
            if (!WideNumericContinuation.GirlNeedsWideFanPath(__instance, __0)) return true;
            WideNumericContinuation.AddGirlFansByCoefficient(__instance, __0, __1, __2); return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_SingleBonusFans_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "singles.AddBonusFans(_single)", typeof(singles), "AddBonusFans",
            new Type[] { typeof(singles._single) }, typeof(void), false); }
        private static bool Prefix(singles._single __0, ref bool __state)
        {
            __state = WideNumericContinuation.PrepareSingleBonusFans(__0);
            return !__state;
        }
        private static void Postfix(singles._single __0, bool __state)
        {
            if (!__state) WideNumericContinuation.SynchronizeSingleBonusFans(__0);
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_SingleCreditFans_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "singles.AddNewFans(_single)", typeof(singles), "AddNewFans",
            new Type[] { typeof(singles._single) }, typeof(void), false); }
        private static bool Prefix(singles._single __0)
        {
            if (!WideNumericContinuation.SingleNeedsWideFanCredit(__0)) return true;
            WideNumericContinuation.CreditSingleFans(__0); return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ShowSetNewFans_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Shows._show.SetNewFans(Int32)", typeof(Shows._show), "SetNewFans",
            new Type[] { typeof(int) }, typeof(void), false); }
        [HarmonyAfter(TelModLibraryInterop.GoingViralOwner)]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(Shows._show __instance, int __0)
        { WideNumericState.AppendShowFans(__instance, __0); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ShowSetSales_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Shows._show.SetSales()", typeof(Shows._show), "SetSales",
            Type.EmptyTypes, typeof(void), false); }
        [HarmonyAfter(TelModLibraryInterop.GoingViralOwner)]
        private static bool Prefix(Shows._show __instance)
        {
            if (!WideNumericContinuation.ShowNeedsWideSalesPath(__instance)) return true;
            WideNumericContinuation.SetShowSalesWide(__instance); return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_CafeMoneyToAdd_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Cafes._cafe.GetMoneyToAdd()", typeof(Cafes._cafe), nameof(Cafes._cafe.GetMoneyToAdd),
            Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(Cafes._cafe __instance, ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericContinuation.GetCafeMoneyToAdd(__instance)); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_CafeFansToAdd_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Cafes._cafe.GetFansToAdd()", typeof(Cafes._cafe), nameof(Cafes._cafe.GetFansToAdd),
            Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(Cafes._cafe __instance, ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericContinuation.GetCafeFansToAdd(__instance)); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_CafeAverageProfit_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Cafes._cafe.GetAverageProfit(_dish)", typeof(Cafes._cafe),
            nameof(Cafes._cafe.GetAverageProfit), new Type[] { typeof(Cafes._cafe._dish) },
            typeof(string), false); }
        private static bool Prefix(Cafes._cafe __instance, Cafes._cafe._dish __0, ref string __result)
        { __result = WideNumericContinuation.GetCafeAverageProfit(__instance, __0); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_CafeAverageNewFans_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Cafes._cafe.GetAverageNewFans(_dish)", typeof(Cafes._cafe),
            nameof(Cafes._cafe.GetAverageNewFans), new Type[] { typeof(Cafes._cafe._dish) },
            typeof(string), false); }
        private static bool Prefix(Cafes._cafe __instance, Cafes._cafe._dish __0, ref string __result)
        { __result = WideNumericContinuation.GetCafeAverageNewFans(__instance, __0); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_CafeRender_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Cafes.RenderCafe(_room,_cafe)", typeof(Cafes), "RenderCafe",
            new Type[] { typeof(agency._room), typeof(Cafes._cafe) }, typeof(void), false); }
        private static void Prefix(Cafes._cafe __1) { WideNumericContinuation.PreflightCafeRender(__1); }
        private static void Postfix(Cafes._cafe __1) { WideNumericContinuation.CompleteCafeRender(__1); }
        private static Exception Finalizer(Cafes._cafe __1, Exception __exception)
        { WideNumericContinuation.LeaveCafeRenderBoundary(__1, __exception); return __exception; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_CafeFloats_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Room_Cafe.LaunchFloats(_stat)", typeof(Room_Cafe), nameof(Room_Cafe.LaunchFloats),
            new Type[] { typeof(Cafes._cafe._stat) }, typeof(void), false); }
        private static bool Prefix(Room_Cafe __instance, Cafes._cafe._stat __0)
        { return !WideNumericContinuation.TryLaunchCafeFloatsWide(__instance, __0); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_CafeFanSelectors_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            Type closure = AccessTools.Inner(typeof(Cafes), "<>c");
            if (closure == null)
            {
                WideNumericContinuationPatchHealth.ReportFailure(
                    "A33.3 could not resolve Cafes.<>c fan selector container.");
                throw new MissingMemberException(typeof(Cafes).FullName, "<>c");
            }
            string[] suffixes = new string[] { "2", "3", "6", "7" };
            foreach (string suffix in suffixes)
            {
                string name = "<RenderCafe>b__14_" + suffix;
                yield return WideNumericTargets.Resolve(
                    "Cafes.popular-selector-" + suffix,
                    closure, name, new Type[] { typeof(data_girls.girls) }, typeof(int), false);
            }
        }
        private static void Postfix(data_girls.girls __0, ref int __result)
        { __result = WideNumericContinuation.GetFanRank(__0); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ConcertSoldTickets_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Concerts._projectedValues.GetNumberOfSoldTickets()",
            typeof(SEvent_Concerts._concert._projectedValues),
            nameof(SEvent_Concerts._concert._projectedValues.GetNumberOfSoldTickets),
            Type.EmptyTypes, typeof(long), false); }
        private static bool Prefix(SEvent_Concerts._concert._projectedValues __instance, ref long __result)
        { __result = WideNumericContinuation.GetConcertSoldTickets(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ConcertRevenue_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Concerts._projectedValues.GetRevenue()",
            typeof(SEvent_Concerts._concert._projectedValues),
            nameof(SEvent_Concerts._concert._projectedValues.GetRevenue),
            Type.EmptyTypes, typeof(long), false); }
        private static bool Prefix(SEvent_Concerts._concert._projectedValues __instance, ref long __result)
        { __result = WideNumericContinuation.GetConcertRevenue(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ConcertActualProfit_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Concerts._projectedValues.GetActualProfit()",
            typeof(SEvent_Concerts._concert._projectedValues),
            nameof(SEvent_Concerts._concert._projectedValues.GetActualProfit),
            Type.EmptyTypes, typeof(long), false); }
        private static bool Prefix(SEvent_Concerts._concert._projectedValues __instance, ref long __result)
        { __result = WideNumericContinuation.GetConcertActualProfit(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ConcertAttendance_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Concerts._projectedValues.SetAttendance()",
            typeof(SEvent_Concerts._concert._projectedValues), "SetAttendance",
            Type.EmptyTypes, typeof(void), false); }
        private static bool Prefix(SEvent_Concerts._concert._projectedValues __instance)
        {
            if (!WideNumericContinuation.ConcertNeedsWideAttendancePath(__instance)) return true;
            WideNumericContinuation.SetConcertAttendanceWide(__instance); return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_SskProductionCost_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_SSK._SSK.GetProductionCost()", typeof(SEvent_SSK._SSK),
            nameof(SEvent_SSK._SSK.GetProductionCost), Type.EmptyTypes, typeof(long), false); }
        private static bool Prefix(SEvent_SSK._SSK __instance, ref long __result)
        { __result = WideNumericContinuation.GetSskProductionCost(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_SskTotalProductionCost_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_SSK._SSK.TotalProductionCost()", typeof(SEvent_SSK._SSK),
            nameof(SEvent_SSK._SSK.TotalProductionCost), Type.EmptyTypes, typeof(long), false); }
        private static bool Prefix(SEvent_SSK._SSK __instance, ref long __result)
        { __result = WideNumericContinuation.GetSskTotalProductionCost(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_SskGenerateResults_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_SSK._SSK.GenerateResults()", typeof(SEvent_SSK._SSK),
            nameof(SEvent_SSK._SSK.GenerateResults), Type.EmptyTypes, typeof(void), false); }
        [HarmonyAfter(TelModLibraryInterop.MbtiOwner, TelModLibraryInterop.TraitsExpansionOwner)]
        private static bool Prefix(SEvent_SSK._SSK __instance)
        {
            if (!WideNumericContinuation.SskNeedsWideResultsPath(__instance)) return true;
            WideNumericContinuation.GenerateSskResultsWide(__instance); return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ResearchBuyPoints_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Research.category.Buy_Points()", typeof(Research.category), nameof(Research.category.Buy_Points),
            Type.EmptyTypes, typeof(void), false); }
        private static bool Prefix(Research.category __instance)
        { WideNumericContinuation.BuyResearchPoints(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_EventMoneyRequirement_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Event_Requirements.Check(_action)", typeof(Event_Requirements), nameof(Event_Requirements.Check),
            new Type[] { typeof(data_dialogues._action) }, typeof(bool), false); }
        private static bool Prefix(data_dialogues._action __0, ref bool __result)
        {
            if (__0 == null || !string.IsNullOrEmpty(__0.target) && __0.target != "legacy" ||
                __0.parameter != "money") return true;
            __result = WideNumericContinuation.CheckMoneyRequirement(__0); return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_VnResource_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "vn_actions.DoResource(String,String,_activeEvent)", typeof(vn_actions), "DoResource",
            new Type[] { typeof(string), typeof(string), typeof(Event_Manager._activeEvent) },
            typeof(void), false); }
        private static bool Prefix(vn_actions __instance, string __0, string __1,
            Event_Manager._activeEvent __2)
        {
            if (!WideNumericContinuation.IsWideVnResourceFormula(__1)) return true;
            resources.type type = (resources.type)Enum.Parse(typeof(resources.type), __0);
            if (type == resources.type.scandalPoints) return true;
            WideNumericContinuation.DoWideVnResource(__instance, __0, __1); return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ShowReleasedFansDisplay_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Show_Released_Button.UpdateParams()", typeof(Show_Released_Button), "UpdateParams",
            Type.EmptyTypes, typeof(void), false); }
        private static void Postfix(Show_Released_Button __instance)
        { WideNumericContinuation.CorrectReleasedShowFans(__instance); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_SingleFanSatisfaction_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "singles._single.ReleaseData_FanSatisfaction()", typeof(singles._single),
            "ReleaseData_FanSatisfaction", Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(singles._single __instance, ref int __result)
        {
            if (resources.GetFansTotal(null) <= 16777216L) return true;
            __result = WideNumericContinuation.GetSingleFanSatisfaction(__instance);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessLiabilityPreview_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "business._data.StringLiability(Boolean)", typeof(business._data), "StringLiability",
            new Type[] { typeof(bool) }, typeof(string), false); }
        private static bool Prefix(business._data __instance, bool __0, ref string __result)
        {
            if (__instance == null) return true;
            __result = WideNumericContinuation.GetBusinessLiabilityPreview(__instance, __0);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_StatsBusinessTopPayment_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Stats.OnBusinessProposalAccepted(_proposal)-wide-top-payment", typeof(Stats),
            nameof(Stats.OnBusinessProposalAccepted), new Type[] { typeof(business._proposal) },
            typeof(void), true); }
        private static void Postfix(business._proposal __0)
        { WideNumericContinuation.RecordBusinessTopPayment(__0); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_StatsResetTopPayments_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Stats.Reset()-wide-top-payment", typeof(Stats), "Reset",
            Type.EmptyTypes, typeof(void), false); }
        private static void Postfix()
        { WideNumericState.ResetBusinessTopPayments(); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GirlExpectedSalary_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "data_girls.girls.GetExpectedSalary()", typeof(data_girls.girls),
            nameof(data_girls.girls.GetExpectedSalary), Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(data_girls.girls __instance, ref int __result)
        {
            long exact = WideNumericContinuation.GetExpectedSalary(__instance);
            if (exact >= int.MinValue && exact <= int.MaxValue) return true;
            __result = WideNumericMath.ClampToInt32(exact); return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GirlExpectedSalaryTotal_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "data_girls.girls.GetExpectedSalary_Total()", typeof(data_girls.girls),
            nameof(data_girls.girls.GetExpectedSalary_Total), Type.EmptyTypes, typeof(long), false); }
        private static bool Prefix(data_girls.girls __instance, ref long __result)
        { __result = WideNumericContinuation.GetExpectedSalaryTotal(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GirlIncreaseSalary_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "data_girls.girls.IncreaseSalary()", typeof(data_girls.girls),
            nameof(data_girls.girls.IncreaseSalary), Type.EmptyTypes, typeof(void), false); }
        private static bool Prefix(data_girls.girls __instance)
        {
            if (__instance.salary >= -16777216L && __instance.salary <= 16777216L) return true;
            WideNumericContinuation.IncreaseSalary(__instance); return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GirlLowerSalary_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "data_girls.girls.LowerSalary()", typeof(data_girls.girls),
            nameof(data_girls.girls.LowerSalary), Type.EmptyTypes, typeof(void), false); }
        private static bool Prefix(data_girls.girls __instance)
        {
            if (__instance.salary >= -16777216L && __instance.salary <= 16777216L) return true;
            WideNumericContinuation.LowerSalary(__instance); return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GirlEarn_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "data_girls.girls.Earn(Int64)", typeof(data_girls.girls),
            nameof(data_girls.girls.Earn), new Type[] { typeof(long) }, typeof(void), false); }
        private static void Prefix(data_girls.girls __instance, long __0)
        { WideNumericRepair.Add(__instance.Earnings_CurrentMonth, __0, "data_girls.girls.Earn"); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GirlTotalEarnings_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "data_girls.girls.GetTotalEarnings()", typeof(data_girls.girls),
            nameof(data_girls.girls.GetTotalEarnings), Type.EmptyTypes, typeof(long), false); }
        private static bool Prefix(data_girls.girls __instance, ref long __result)
        { __result = WideNumericContinuation.GetGirlTotalEarnings(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_SingleIdGuard_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "singles.GetNewSingleID()", typeof(singles), nameof(singles.GetNewSingleID),
            Type.EmptyTypes, typeof(int), false); }
        private static void Prefix() { WideNumericContinuation.GuardNextId(singles.LastSingleID, "singles.GetNewSingleID"); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ShowIdGuard_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Shows.GetNewShowID()", typeof(Shows), nameof(Shows.GetNewShowID),
            Type.EmptyTypes, typeof(int), false); }
        private static void Prefix() { WideNumericContinuation.GuardNextId(Shows.LastShowID, "Shows.GetNewShowID"); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GirlIdGuard_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "data_girls.GetNewGirlID()", typeof(data_girls), nameof(data_girls.GetNewGirlID),
            Type.EmptyTypes, typeof(int), true); }
        private static void Prefix() { WideNumericContinuation.GuardNextId(
            data_girls.LastGirlID, "data_girls.GetNewGirlID"); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_StaffIdGuard_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "staff.GetNewStaffID()", typeof(staff), nameof(staff.GetNewStaffID),
            Type.EmptyTypes, typeof(int), true); }
        private static void Prefix() { WideNumericContinuation.GuardNextId(
            staff.LastStaffID, "staff.GetNewStaffID"); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GroupIdGuard_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Groups.GetNextGroupID()", typeof(Groups), nameof(Groups.GetNextGroupID),
            Type.EmptyTypes, typeof(int), true); }
        private static bool Prefix(ref int __result)
        { __result = WideNumericContinuation.GetNextGroupId(); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TheaterIdGuard_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Theaters.GetNextTheaterID()", typeof(Theaters), "GetNextTheaterID",
            Type.EmptyTypes, typeof(int), true); }
        private static bool Prefix(ref int __result)
        { __result = WideNumericContinuation.GetNextTheaterId(); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_CafeIdGuard_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Cafes.GetNextTheaterID()", typeof(Cafes), "GetNextTheaterID",
            Type.EmptyTypes, typeof(int), true); }
        private static bool Prefix(ref int __result)
        { __result = WideNumericContinuation.GetNextCafeId(); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_DishIdGuard_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Cafes._cafe.GetNewDishID()", typeof(Cafes._cafe), nameof(Cafes._cafe.GetNewDishID),
            Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(Cafes._cafe __instance, ref int __result)
        { __result = WideNumericContinuation.GetNextDishId(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_FloorRoomIdGuard_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "agency._floor.addRoom(_type)", typeof(agency._floor), nameof(agency._floor.addRoom),
            new Type[] { typeof(agency._type) }, typeof(void), false); }
        private static void Prefix(agency._type __0)
        { WideNumericContinuation.PreflightFloorAddRoom(__0); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_AgencyAddRoomGuard_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "agency.addRoom(Int32,Boolean)", typeof(agency), nameof(agency.addRoom),
            new Type[] { typeof(int), typeof(bool) }, typeof(void), false); }
        private static void Prefix(agency __instance, int __0, bool __1)
        { WideNumericContinuation.PreflightAgencyAddRoom(__instance, __0, __1); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ConcertIdGuard_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Concerts._concert.Initiate()", typeof(SEvent_Concerts._concert),
            nameof(SEvent_Concerts._concert.Initiate), Type.EmptyTypes, typeof(void), false); }
        private static void Prefix(SEvent_Concerts._concert __instance)
        { if (__instance.ID == -1) WideNumericContinuation.GuardNextId(
            SEvent_Concerts.Last_Concert_ID, "SEvent_Concerts._concert.Initiate"); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_SskIdGuard_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_SSK._SSK.Initiate()", typeof(SEvent_SSK._SSK),
            nameof(SEvent_SSK._SSK.Initiate), Type.EmptyTypes, typeof(void), false); }
        private static void Prefix(SEvent_SSK._SSK __instance)
        { if (__instance.ID == -1) WideNumericContinuation.GuardNextId(
            SEvent_SSK.Last_SSK_ID, "SEvent_SSK._SSK.Initiate"); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ActivityPerformanceCounter_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Activities.Performance()", typeof(Activities), nameof(Activities.Performance),
            Type.EmptyTypes, typeof(void), false); }
        private static bool Prefix(Activities __instance)
        {
            WideNumericContinuation.PreflightActivityCounters(Activity._type.performance);
            if (!WideNumericContinuation.ActivityPerformanceNeedsWidePath(__instance)) return true;
            WideNumericContinuation.PerformActivityWide(__instance);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ActivityPromotionCounter_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Activities.Promotion()", typeof(Activities), nameof(Activities.Promotion),
            Type.EmptyTypes, typeof(void), false); }
        private static bool Prefix(Activities __instance)
        {
            WideNumericContinuation.PreflightActivityCounters(Activity._type.promotion);
            if (!WideNumericContinuation.ActivityPromotionNeedsWidePath(__instance)) return true;
            WideNumericContinuation.PromoteActivityWide(__instance);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ActivitySpaCounter_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Activities.SpaTreatment()", typeof(Activities), nameof(Activities.SpaTreatment),
            Type.EmptyTypes, typeof(void), false); }
        private static void Prefix()
        { WideNumericContinuation.PreflightActivityCounters(Activity._type.spa_treatment); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ActivityFansCompatibility_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Activities.GetFansToAdd(Single)", typeof(Activities), nameof(Activities.GetFansToAdd),
            new Type[] { typeof(float) }, typeof(int), true); }
        private static bool Prefix(float __0, ref int __result)
        { __result = WideNumericContinuation.GetActivityFansCompatibility(__0); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ActivityTooltip_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Activities.GetTooltipForWidget(ActivityType)", typeof(Activities), nameof(Activities.GetTooltipForWidget),
            new Type[] { typeof(Activity._type) }, typeof(string), false); }
        private static void Postfix(Activities __instance, Activity._type __0, ref string __result)
        { WideNumericContinuation.CorrectActivityTooltip(__instance, __0, ref __result); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ActivityLevelUpUi_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Activities._activity.GetLevelUp()", typeof(Activities._activity),
            nameof(Activities._activity.GetLevelUp), Type.EmptyTypes, typeof(string), false); }
        private static bool Prefix(Activities._activity __instance, ref string __result)
        { __result = WideNumericContinuation.GetActivityLevelUpText(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessDeclineCounter_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "business.Decline()", typeof(business), nameof(business.Decline),
            Type.EmptyTypes, typeof(void), false); }
        private static void Prefix(business __instance)
        { WideNumericContinuation.PreflightBusinessDeclineCounters(__instance.ActiveProposal); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessStatsCounter_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Stats.OnBusinessProposalAccepted(_proposal)", typeof(Stats),
            nameof(Stats.OnBusinessProposalAccepted), new Type[] { typeof(business._proposal) },
            typeof(void), true); }
        private static void Prefix(business._proposal __0)
        { WideNumericContinuation.PreflightBusinessStatsCounter(__0); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_InjuryCounter_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "data_girls.girls.Set_Injured()", typeof(data_girls.girls),
            nameof(data_girls.girls.Set_Injured), Type.EmptyTypes, typeof(void), false); }
        private static void Prefix(data_girls.girls __instance)
        { WideNumericContinuation.GuardCounterIncrement(__instance.Injury_Counter, 1,
            "data_girls.girls.Set_Injured"); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_DepressionCounter_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "data_girls.girls.Set_Depressed()", typeof(data_girls.girls),
            nameof(data_girls.girls.Set_Depressed), Type.EmptyTypes, typeof(void), false); }
        private static void Prefix(data_girls.girls __instance)
        { WideNumericContinuation.GuardCounterIncrement(__instance.Depression_Counter, 1,
            "data_girls.girls.Set_Depressed"); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_DatingSuccessCounter_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Date_Flirt.DoFlirt(girl)", typeof(Date_Flirt), nameof(Date_Flirt.DoFlirt),
            new Type[] { typeof(data_girls.girls) }, typeof(List<string>), true); }
        private static void Prefix(data_girls.girls __0)
        {
            if (__0 != null) WideNumericContinuation.GuardCounterIncrement(
                __0.DatingData.Success_Counter, Dating.DEBUG ? 10 : 3,
                "Date_Flirt.DoFlirt maximum success increment");
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_RelationshipPoints_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Relationships_Player.AddPoints(_type,girl,Int32)", typeof(Relationships_Player),
            nameof(Relationships_Player.AddPoints), new Type[] { typeof(Relationships_Player._type),
                typeof(data_girls.girls), typeof(int) }, typeof(void), true); }
        [HarmonyAfter(TelModLibraryInterop.MbtiOwner)]
        private static bool Prefix(Relationships_Player._type __0, data_girls.girls __1, int __2)
        { WideNumericContinuation.AddRelationshipPoints(__0, __1, __2); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ConcertNoAccidentCounter_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Concert_Popup.TriggerAccident(Boolean)", typeof(Concert_Popup),
            nameof(Concert_Popup.TriggerAccident), new Type[] { typeof(bool) },
            typeof(void), false); }
        private static void Prefix(Concert_Popup __instance, bool __0)
        { if (!__0 && __instance.Concert != null) WideNumericContinuation.GuardCounterIncrement(
            __instance.Concert.No_Accident_Counter, 1, "Concert_Popup.TriggerAccident"); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourCountryConcertCounter_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Tour_Popup_Country.Set(tour,selectedCountry)", typeof(Tour_Popup_Country),
            nameof(Tour_Popup_Country.Set), new Type[] { typeof(SEvent_Tour.tour),
                typeof(SEvent_Tour.tour.selectedCountry) }, typeof(void), false); }
        private static void Prefix(SEvent_Tour.tour.selectedCountry __1)
        { if (__1 != null && __1.Country != null) WideNumericContinuation.GuardCounterIncrement(
            __1.Country.ConcertCount, 1, "Tour_Popup_Country.Set country concert counter"); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GirlsTriviaCounter_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "girls_trivia._data.Use()", typeof(girls_trivia._data), nameof(girls_trivia._data.Use),
            Type.EmptyTypes, typeof(void), false); }
        private static void Prefix(girls_trivia._data __instance)
        { WideNumericContinuation.GuardCounterIncrement(__instance.Counter, 1,
            "girls_trivia._data.Use"); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GraduationTriviaCounter_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Graduation_Trivia._trivia.Use()", typeof(Graduation_Trivia._trivia),
            nameof(Graduation_Trivia._trivia.Use), Type.EmptyTypes, typeof(void), false); }
        private static void Prefix(Graduation_Trivia._trivia __instance)
        { WideNumericContinuation.GuardCounterIncrement(__instance.Counter, 1,
            "Graduation_Trivia._trivia.Use"); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ShowEpisodeCounter_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Shows._show.NewEpisode()", typeof(Shows._show), nameof(Shows._show.NewEpisode),
            Type.EmptyTypes, typeof(void), false); }
        private static void Prefix(Shows._show __instance)
        { WideNumericContinuation.GuardCounterIncrement(__instance.episodeCount, 1,
            "Shows._show.NewEpisode"); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ShowRelaunchCounter_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Shows._show.OnRelaunchFinish()", typeof(Shows._show),
            nameof(Shows._show.OnRelaunchFinish), Type.EmptyTypes, typeof(void), false); }
        private static void Prefix(Shows._show __instance)
        { WideNumericContinuation.GuardCounterIncrement(__instance.NumberOfRelaunches, 1,
            "Shows._show.OnRelaunchFinish"); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_AgencyRoomRent_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "agency.GetRoomRent(_type,Int32)", typeof(agency), nameof(agency.GetRoomRent),
            new Type[] { typeof(agency._type), typeof(int) }, typeof(int), false); }
        private static bool Prefix(agency._type __0, int __1, ref int __result)
        {
            __result = WideNumericMath.ClampToInt32(
                WideNumericContinuation.GetRoomRent(__0, __1));
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_AgencyRoomTooltip_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "agency.GetRoomTooltip(_type)", typeof(agency), nameof(agency.GetRoomTooltip),
            new Type[] { typeof(agency._type) }, typeof(string), false); }
        private static void Postfix(agency __instance, agency._type __0, ref string __result)
        { WideNumericContinuation.CorrectAgencyRoomTooltip(__instance, __0, ref __result); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_StaffSeveranceCompatibility_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "staff._staff.Severance()", typeof(staff._staff), nameof(staff._staff.Severance),
            Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(staff._staff __instance, ref int __result)
        {
            __result = WideNumericMath.ClampToInt32(
                WideNumericContinuation.GetStaffSeverance(__instance));
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_StaffCanFireSeverance_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "staff._staff.CanFire_Severance()", typeof(staff._staff),
            nameof(staff._staff.CanFire_Severance), Type.EmptyTypes, typeof(bool), false); }
        private static bool Prefix(staff._staff __instance, ref bool __result)
        {
            __result = WideNumericContinuation.CanFireStaffWithSeverance(__instance);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_StaffFireSeverance_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "staff._staff.Fire_Severance()", typeof(staff._staff),
            nameof(staff._staff.Fire_Severance), Type.EmptyTypes, typeof(void), false); }
        private static bool Prefix(staff._staff __instance)
        {
            WideNumericContinuation.FireStaffWithSeverance(__instance);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_StaffFireSeveranceTooltip_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "staff._staff.GetTooltip_Fire_Severance()", typeof(staff._staff),
            nameof(staff._staff.GetTooltip_Fire_Severance), Type.EmptyTypes,
            typeof(string), false); }
        private static bool Prefix(staff._staff __instance, ref string __result)
        {
            __result = WideNumericContinuation.GetStaffFireSeveranceTooltip(__instance);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_OfficeSeveranceUi_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "ContextMenu_Office.SetFireColor()", typeof(ContextMenu_Office),
            "SetFireColor", Type.EmptyTypes, typeof(void), false); }
        private static void Postfix(ContextMenu_Office __instance, ContextMenuController ___cmc)
        { WideNumericContinuation.RenderOfficeSeveranceExact(__instance, ___cmc); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_DanceSeveranceUi_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "CM_Dance.SetFireColor()", typeof(CM_Dance),
            "SetFireColor", Type.EmptyTypes, typeof(void), false); }
        private static void Postfix(CM_Dance __instance, staff._staff ___Staffer)
        { WideNumericContinuation.RenderDanceSeveranceExact(__instance, ___Staffer); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_StaffFireDialoguePayment_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Staff_Fire.DoComplete()", typeof(Staff_Fire), nameof(Staff_Fire.DoComplete),
            Type.EmptyTypes, typeof(void), true); }
        private static void Prefix(out long __state)
        { __state = WideNumericContinuation.PreflightStaffFireDialoguePayment(); }
        private static void Postfix(long __state)
        { WideNumericContinuation.CompleteStaffFireDialoguePayment(__state); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ExactMoneyFormatter_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "ExtensionMethods.formatMoney(Int64,Boolean,Boolean,Boolean)",
            typeof(ExtensionMethods), nameof(ExtensionMethods.formatMoney),
            new Type[] { typeof(long), typeof(bool), typeof(bool), typeof(bool) },
            typeof(string), true); }
        private static bool Prefix(long __0, bool __1, bool __2, bool __3, ref string __result)
        {
            // Preserve intentionally abbreviated UI.  Every full-width money label,
            // including calls routed through the Int32 overload, gets an exact whole
            // Int64 rendering with a safe Int64.MinValue path.
            if (__1 || __2) return true;
            __result = WideNumericContinuation.FormatMoneyExact(__0, __3);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_SalaryLineMoneyUi_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Salary_Line.Render()", typeof(Salary_Line), "Render",
            Type.EmptyTypes, typeof(void), false); }
        private static void Postfix(Salary_Line __instance)
        { WideNumericContinuation.RenderSalaryLineExact(__instance); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GirlEarningsStringMoneyUi_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "data_girls.girls.GetEarningsString()", typeof(data_girls.girls),
            nameof(data_girls.girls.GetEarningsString), Type.EmptyTypes,
            typeof(string), false); }
        private static bool Prefix(data_girls.girls __instance, ref string __result)
        {
            __result = WideNumericContinuation.GetGirlEarningsStringExact(__instance);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TooltipBusiness_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "tooltip_money.BusinessContracts()", typeof(tooltip_money), "BusinessContracts",
            Type.EmptyTypes, typeof(string), false); }
        private static bool Prefix(ref long ___Total, ref string __result)
        {
            __result = WideNumericContinuation.AddWeeklyTooltipLine("TIP__CONTRACTS",
                WideNumericContinuation.GetBusinessWeeklyProfit(
                    Camera.main.GetComponent<mainScript>().Data.GetComponent<business>()),
                ref ___Total);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TooltipMedia_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "tooltip_money.Media()", typeof(tooltip_money), "Media",
            Type.EmptyTypes, typeof(string), false); }
        private static bool Prefix(ref long ___Total, ref string __result)
        {
            __result = WideNumericContinuation.AddWeeklyTooltipLine("MEDIA",
                Shows.GetTotalProfit(), ref ___Total);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TooltipCafe_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "tooltip_money.Cafe()", typeof(tooltip_money), "Cafe",
            Type.EmptyTypes, typeof(string), false); }
        private static bool Prefix(ref long ___Total, ref string __result)
        {
            __result = WideNumericContinuation.AddWeeklyTooltipLine("TIP__CAFE",
                WideNumericContinuation.GetCafeWeeklyIncomeForDisplay(), ref ___Total);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TooltipTheater_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "tooltip_money.Theater()", typeof(tooltip_money), "Theater",
            Type.EmptyTypes, typeof(string), false); }
        private static bool Prefix(ref long ___Total, ref string __result)
        {
            __result = WideNumericContinuation.AddWeeklyTooltipLine("TIP__THEATER",
                WideNumericContinuation.GetTheaterWeeklyIncomeForDisplay(), ref ___Total);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TooltipIdolSalaries_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "tooltip_money.IdolSalaries()", typeof(tooltip_money), "IdolSalaries",
            Type.EmptyTypes, typeof(string), false); }
        private static bool Prefix(ref long ___Total, ref string __result)
        {
            long value = WideNumericRepair.Subtract(0L,
                WideNumericContinuation.GetGirlSalary(), "tooltip_money.IdolSalaries");
            __result = WideNumericContinuation.AddWeeklyTooltipLine(
                "TIP__IDOL_SALARIES", value, ref ___Total);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TooltipStaffSalaries_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "tooltip_money.StaffSalaries()", typeof(tooltip_money), "StaffSalaries",
            Type.EmptyTypes, typeof(string), false); }
        private static bool Prefix(ref long ___Total, ref string __result)
        {
            long value = WideNumericRepair.Subtract(0L,
                WideNumericContinuation.GetStaffSalary(), "tooltip_money.StaffSalaries");
            __result = WideNumericContinuation.AddWeeklyTooltipLine(
                "TIP__STAFF_SALARIES", value, ref ___Total);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TooltipRent_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "tooltip_money.Rent()", typeof(tooltip_money), "Rent",
            Type.EmptyTypes, typeof(string), false); }
        private static bool Prefix(ref long ___Total, ref string __result)
        {
            long value = WideNumericRepair.Subtract(0L,
                WideNumericContinuation.GetTotalRent(false), "tooltip_money.Rent");
            __result = WideNumericContinuation.AddWeeklyTooltipLine(
                "TIP__RENT", value, ref ___Total);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TooltipLoans_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "tooltip_money.Loans()", typeof(tooltip_money), "Loans",
            Type.EmptyTypes, typeof(string), false); }
        private static bool Prefix(ref long ___Total, ref string __result)
        {
            long value = WideNumericRepair.Subtract(0L,
                WideNumericContinuation.GetTotalLoanPayment(), "tooltip_money.Loans");
            __result = WideNumericContinuation.AddWeeklyTooltipLine(
                "LOANS", value, ref ___Total);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_LoanLineUi_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Loans_Line.Set(_loan)", typeof(Loans_Line), nameof(Loans_Line.Set),
            new Type[] { typeof(loans._loan) }, typeof(void), false); }
        private static void Postfix(Loans_Line __instance, loans._loan __0)
        { WideNumericContinuation.RenderLoanLinePayment(__instance, __0); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_LoanPopupUi_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Loans_Popup.RenderDetails()", typeof(Loans_Popup), "RenderDetails",
            Type.EmptyTypes, typeof(void), false); }
        private static void Postfix(Loans_Popup __instance)
        { WideNumericContinuation.RenderLoanPopupDetails(__instance); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TheaterPricingUi_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Theater_Popup.Render_Pricing()", typeof(Theater_Popup), "Render_Pricing",
            Type.EmptyTypes, typeof(void), false); }
        private static void Postfix(Theater_Popup __instance)
        { WideNumericContinuation.RenderTheaterPricing(__instance); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_StoryDailyComparison_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "tasks.OnNewDay()", typeof(tasks), nameof(tasks.OnNewDay),
            Type.EmptyTypes, typeof(void), false); }
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            FieldInfo story = AccessTools.Field(typeof(tasks), nameof(tasks.Story_Data));
            FieldInfo target = AccessTools.Field(typeof(tasks._story_data), "ch3_aya_fans");
            MethodInfo wide = AccessTools.Method(typeof(WideNumericState),
                nameof(WideNumericState.GetStoryCh3), Type.EmptyTypes);
            int replacements = 0;
            for (int index = 0; index + 2 < codes.Count; index++)
            {
                if (codes[index].opcode != OpCodes.Ldsfld || !Equals(codes[index].operand, story) ||
                    codes[index + 1].opcode != OpCodes.Ldfld ||
                    !Equals(codes[index + 1].operand, target) ||
                    codes[index + 2].opcode != OpCodes.Conv_I8) continue;
                codes[index].opcode = OpCodes.Call;
                codes[index].operand = wide;
                codes[index + 1].opcode = OpCodes.Nop;
                codes[index + 1].operand = null;
                codes[index + 2].opcode = OpCodes.Nop;
                codes[index + 2].operand = null;
                replacements++;
            }
            if (replacements != 1)
            {
                WideNumericContinuationPatchHealth.ReportFailure(
                    "A33.5 expected one tasks.OnNewDay chapter-3 fan-target read, found " +
                    replacements + ".");
                throw new InvalidOperationException("Unexpected tasks.OnNewDay IL shape.");
            }
            return codes;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_StoryDescription_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "tasks._task.GetDescription_Custom()", typeof(tasks._task),
            "GetDescription_Custom", Type.EmptyTypes, typeof(string), false); }
        private static bool Prefix(tasks._task __instance, ref string __result)
        {
            if (!string.Equals(__instance.Custom, "ch3_aya_3", StringComparison.Ordinal))
                return true;
            __result = WideNumericContinuation.GetStoryTaskDescription(__instance);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_StorySummerGamesUi_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Summer_Games_Button.Render()", typeof(Summer_Games_Button),
            nameof(Summer_Games_Button.Render), Type.EmptyTypes, typeof(void), false); }
        private static void Postfix(Summer_Games_Button __instance)
        { WideNumericContinuation.RenderSummerGamesTargets(__instance); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_DebugStoryTargets_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Debug_Popup.CompleteAllTasks()", typeof(Debug_Popup),
            nameof(Debug_Popup.CompleteAllTasks), Type.EmptyTypes, typeof(void), false); }
        private static void Postfix()
        { WideNumericContinuation.SynchronizeDebugStoryTargets(); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_StoryVnVariable_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "vn_actions.DoCustom(String)", typeof(vn_actions), "DoCustom",
            new Type[] { typeof(string) }, typeof(void), false); }
        private static void Postfix(string __0, ActiveDialogueController ___ADC)
        {
            WideNumericContinuation.SynchronizeStoryVnVariable(__0);
            WideNumericContinuation.CorrectStaffFireDialogueAmount(__0, ___ADC);
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourCountryCost_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Tour.country.GetCostByLevel(Int32)", typeof(SEvent_Tour.country),
            nameof(SEvent_Tour.country.GetCostByLevel), new Type[] { typeof(int) },
            typeof(int), false); }
        private static bool Prefix(SEvent_Tour.country __instance, int __0, ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericContinuation.GetTourCountryCost(__instance, __0)); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourCountryRevenueLevel_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Tour.country.GetRevenueByLevel(Int32)", typeof(SEvent_Tour.country),
            nameof(SEvent_Tour.country.GetRevenueByLevel), new Type[] { typeof(int) },
            typeof(int), false); }
        private static bool Prefix(SEvent_Tour.country __instance, int __0, ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericContinuation.GetTourCountryRevenueByLevel(__instance, __0)); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourCountryRevenueCapacity_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Tour.country.GetRevenueByCapacity(Int32)", typeof(SEvent_Tour.country),
            nameof(SEvent_Tour.country.GetRevenueByCapacity), new Type[] { typeof(int) },
            typeof(int), false); }
        private static bool Prefix(SEvent_Tour.country __instance, int __0, ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericContinuation.GetTourCountryRevenueByAttendance(__instance, __0)); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourCountrySaving_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Tour.country.GetSaving(Int32)", typeof(SEvent_Tour.country),
            nameof(SEvent_Tour.country.GetSaving), new Type[] { typeof(int) },
            typeof(int), false); }
        private static bool Prefix(SEvent_Tour.country __instance, int __0, ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericContinuation.GetTourCountrySaving(__instance, __0)); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourSaving_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Tour.tour.GetSaving()", typeof(SEvent_Tour.tour),
            nameof(SEvent_Tour.tour.GetSaving), Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(SEvent_Tour.tour __instance, ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericState.GetTourSaving(__instance)); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourTotalAudience_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Tour.tour.GetTotalAudience()", typeof(SEvent_Tour.tour),
            nameof(SEvent_Tour.tour.GetTotalAudience), Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(SEvent_Tour.tour __instance, ref int __result)
        { __result = WideNumericContinuation.GetTourTotalAudienceCompatibility(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourCountryFans_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Tour.tour.GetNewFans()", typeof(SEvent_Tour.tour),
            nameof(SEvent_Tour.tour.GetNewFans), Type.EmptyTypes, typeof(int), false); }
        private static bool Prefix(SEvent_Tour.tour __instance, ref int __result)
        { __result = WideNumericContinuation.GetTourCountryFansCompatibility(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourPopupBar_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Tour_Popup.UpdateBar()", typeof(Tour_Popup), "UpdateBar",
            Type.EmptyTypes, typeof(void), false); }
        private static bool Prefix(Tour_Popup __instance)
        { WideNumericContinuation.RenderTourBar(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_NewTourPopup_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Tour_New_Popup.Render()", typeof(Tour_New_Popup), nameof(Tour_New_Popup.Render),
            Type.EmptyTypes, typeof(void), false); }
        private static void Postfix(Tour_New_Popup __instance)
        { WideNumericContinuation.CorrectNewTourPopup(__instance); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourProjectButton_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Button_Tour.UpdateData_Tour()", typeof(SEvent_Button_Tour),
            "UpdateData_Tour", Type.EmptyTypes, typeof(void), false); }
        private static void Postfix(SEvent_Button_Tour __instance)
        { WideNumericContinuation.CorrectTourProjectButton(__instance); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourPopupAttendance_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Tour_Popup_Country.AnimateAttendance(Int32)", typeof(Tour_Popup_Country),
            "AnimateAttendance", new Type[] { typeof(int) }, typeof(void), false); }
        private static bool Prefix(Tour_Popup_Country __instance, int __0)
        { return !WideNumericContinuation.TryAnimateTourAttendanceWide(__instance, __0); }
        private static void Postfix(Tour_Popup_Country __instance)
        { WideNumericContinuation.SynchronizeTourPopupCountry(__instance); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourPopupFans_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Tour_Popup_Country.AnimateFans(Int32)", typeof(Tour_Popup_Country),
            "AnimateFans", new Type[] { typeof(int) }, typeof(void), false); }
        private static void Postfix(Tour_Popup_Country __instance)
        { WideNumericContinuation.SynchronizeTourPopupCountry(__instance); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourStarTooltip_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Tour_Star.SetTooltip()", typeof(Tour_Star), nameof(Tour_Star.SetTooltip),
            Type.EmptyTypes, typeof(void), false); }
        [HarmonyAfter(TelModLibraryInterop.TourStaminaOwner)]
        private static bool Prefix(Tour_Star __instance)
        { WideNumericContinuation.RenderTourStarTooltip(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_FinishedTourUi_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Button_Tour_Finished.Set(tour)", typeof(SEvent_Button_Tour_Finished),
            nameof(SEvent_Button_Tour_Finished.Set), new Type[] { typeof(SEvent_Tour.tour) },
            typeof(void), false); }
        private static bool Prefix(SEvent_Button_Tour_Finished __instance, SEvent_Tour.tour __0)
        { WideNumericContinuation.RenderFinishedTour(__instance, __0); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ShowFansGetter_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Shows._show.GetFans(Nullable<Int32>)", typeof(Shows._show),
            nameof(Shows._show.GetFans), new Type[] { typeof(int?) }, typeof(int), false); }
        private static bool Prefix(Shows._show __instance, int? __0, ref int __result)
        { __result = WideNumericMath.ClampToInt32(WideNumericContinuation.GetShowEpisodeFans(__instance, __0)); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ShowRevenueMutation_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Shows._show.SetRevenue()", typeof(Shows._show), "SetRevenue",
            Type.EmptyTypes, typeof(void), false); }
        private static bool Prefix(Shows._show __instance)
        { WideNumericContinuation.SetShowRevenueWide(__instance); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_SingleCreditMoney_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "singles.AddMoney(_single)", typeof(singles), "AddMoney",
            new Type[] { typeof(singles._single) }, typeof(void), false); }
        private static bool Prefix(singles._single __0)
        { WideNumericContinuation.CreditSingleMoney(__0); return false; }
    }

    [HarmonyPatch]
    internal static class WideNumeric_SinglePopupProductionCost_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Single_Popup.CalculateProductionCost()", typeof(Single_Popup),
            "CalculateProductionCost", Type.EmptyTypes, typeof(long), false); }
        private static bool Prefix(Single_Popup __instance, ref long __result)
        {
            __result = WideNumericContinuation.GetSinglePopupProductionCost(__instance);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class WideNumeric_SingleReleaseSalesUi_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Single_Release.AnimateSales(Single)", typeof(Single_Release), "AnimateSales",
            new Type[] { typeof(float) }, typeof(void), false); }
        private static bool Prefix(Single_Release __instance)
        { return !WideNumericContinuation.TryRenderSingleReleaseSalesWide(__instance); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_SingleReleaseFansUi_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Single_Release.AnimateNewFans(Single)", typeof(Single_Release), "AnimateNewFans",
            new Type[] { typeof(float) }, typeof(void), false); }
        private static bool Prefix(Single_Release __instance)
        { return !WideNumericContinuation.TryRenderSingleReleaseNewFansWide(__instance); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_SingleReleaseBonusUi_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Single_Release.AnimateNewFansBonus(Single)", typeof(Single_Release),
            "AnimateNewFansBonus", new Type[] { typeof(float) }, typeof(void), false); }
        private static bool Prefix(Single_Release __instance)
        { return !WideNumericContinuation.TryRenderSingleReleaseBonusWide(__instance); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ShowReleaseSalesUi_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Show_Release.AnimateSales(Single)", typeof(Show_Release), "AnimateSales",
            new Type[] { typeof(float) }, typeof(void), false); }
        private static bool Prefix(Show_Release __instance)
        { return !WideNumericContinuation.TryRenderShowReleaseSalesWide(__instance); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_ShowButtonTooltip_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Shows._show.GetButtonTooltip()", typeof(Shows._show),
            nameof(Shows._show.GetButtonTooltip), Type.EmptyTypes, typeof(string), false); }
        private static void Postfix(Shows._show __instance, ref string __result)
        { WideNumericContinuation.CorrectShowButtonTooltip(__instance, ref __result); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TheaterStatLine_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Theater_Stats_Line.Set(_stat)", typeof(Theater_Stats_Line),
            nameof(Theater_Stats_Line.Set), new Type[] { typeof(Theaters._theater._stat) },
            typeof(void), false); }
        private static void Postfix(Theater_Stats_Line __instance,
            Theaters._theater._stat __0)
        { WideNumericContinuation.CorrectTheaterStatLine(__instance, __0); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_CafeStatLine_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Cafe_Stat.Set(_cafe,_stat,DateTime)", typeof(Cafe_Stat),
            nameof(Cafe_Stat.Set), new Type[] { typeof(Cafes._cafe),
                typeof(Cafes._cafe._stat), typeof(DateTime) }, typeof(void), false); }
        private static void Postfix(Cafe_Stat __instance, Cafes._cafe __0,
            Cafes._cafe._stat __1)
        { WideNumericContinuation.CorrectCafeStatLine(__instance, __0, __1); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TheaterSubscriberUi_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Group_Appeal_Fan.SetSub(_subscriber)", typeof(Group_Appeal_Fan),
            nameof(Group_Appeal_Fan.SetSub),
            new Type[] { typeof(Theaters._theater._subscriber) }, typeof(void), false); }
        private static bool Prefix(Group_Appeal_Fan __instance,
            Theaters._theater._subscriber __0)
        { return !WideNumericContinuation.TryRenderTheaterSubscriberWide(__instance, __0); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_GroupAppealPopup_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Group_Appeal_Popup.RenderTab()", typeof(Group_Appeal_Popup),
            nameof(Group_Appeal_Popup.RenderTab), Type.EmptyTypes, typeof(void), false); }
        private static void Postfix(Group_Appeal_Popup __instance)
        { WideNumericContinuation.CorrectGroupAppealPopup(__instance); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_TourLoad_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "SEvent_Tour.LoadFunction()", typeof(SEvent_Tour), nameof(SEvent_Tour.LoadFunction), Type.EmptyTypes, typeof(void), false); }
        private static void Postfix() { WideNumericState.RestoreToursAfterVanillaLoad(); }
    }
    [HarmonyPatch]
    internal static class WideNumeric_SingleLoad_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "singles.LoadFunction()", typeof(singles), nameof(singles.LoadFunction), Type.EmptyTypes, typeof(void), false); }
        private static void Postfix() { WideNumericState.RestoreSinglesAfterVanillaLoad(); }
    }
    [HarmonyPatch]
    internal static class WideNumeric_ShowLoad_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Shows.LoadFunction()", typeof(Shows), nameof(Shows.LoadFunction), Type.EmptyTypes, typeof(void), false); }
        private static void Postfix() { WideNumericState.RestoreShowsAfterVanillaLoad(); }
    }
    [HarmonyPatch]
    internal static class WideNumeric_TheaterLoad_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Theaters.LoadFunction()", typeof(Theaters), nameof(Theaters.LoadFunction), Type.EmptyTypes, typeof(void), false); }
        private static void Postfix() { WideNumericState.RestoreTheatersAfterVanillaLoad(); }
    }
    [HarmonyPatch]
    internal static class WideNumeric_StatsLoad_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Stats.LoadFunction()", typeof(Stats), nameof(Stats.LoadFunction), Type.EmptyTypes, typeof(void), false); }
        private static void Postfix() { WideNumericState.RestoreStatsAfterVanillaLoad(); }
    }
    [HarmonyPatch]
    internal static class WideNumeric_StoryLoad_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "tasks.LoadFunction()", typeof(tasks), nameof(tasks.LoadFunction), Type.EmptyTypes, typeof(void), false); }
        private static void Postfix() { WideNumericState.RestoreStoryAfterVanillaLoad(); }
    }
    [HarmonyPatch]
    internal static class WideNumeric_LoanLoad_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "loans.LoadFunction()", typeof(loans), nameof(loans.LoadFunction), Type.EmptyTypes, typeof(void), false); }
        private static void Postfix() { WideNumericState.RestoreLoansAfterVanillaLoad(); }
    }
    [HarmonyPatch]
    internal static class WideNumeric_CafeLoad_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "Cafes.LoadFunction()", typeof(Cafes), nameof(Cafes.LoadFunction), Type.EmptyTypes, typeof(void), false); }
        private static void Postfix() { WideNumericState.RestoreCafesAfterVanillaLoad(); }
    }

    [HarmonyPatch]
    internal static class WideNumeric_BusinessLoad_Patch
    {
        private static MethodBase TargetMethod() { return WideNumericTargets.Resolve(
            "business.LoadFunction()", typeof(business), nameof(business.LoadFunction), Type.EmptyTypes, typeof(void), false); }
        private static void Postfix() { WideNumericState.RestoreBusinessContractsAfterVanillaLoad(); }
    }
}
