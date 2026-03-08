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
    /// Captures loan-addition lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(loans), nameof(loans.AddLoan))]
    internal static class loans_AddLoan_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(loans._loan __0, out LoanMutationSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateLoanMutationSnapshot(__0);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(loans._loan __0, LoanMutationSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureLoanAdded(__0, __state);
        }
    }

    /// <summary>
    /// Captures loan initialization lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(loans._loan), nameof(loans._loan.Initialize))]
    internal static class loans_loan_Initialize_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(loans._loan __instance, out LoanMutationSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateLoanMutationSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(loans._loan __instance, LoanMutationSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureLoanInitialized(__instance, __state);
        }
    }

    /// <summary>
    /// Captures loan payoff lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(loans._loan), nameof(loans._loan.PayOff))]
    internal static class loans_loan_PayOff_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(loans._loan __instance, out LoanMutationSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateLoanMutationSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(loans._loan __instance, LoanMutationSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureLoanPaidOff(__instance, __state);
        }
    }

    /// <summary>
    /// Captures bankruptcy danger threshold state toggles.
    /// </summary>
    [HarmonyPatch(typeof(loans), CoreConstants.HarmonyLoansSetBankruptcyDangerMethodName)]
    internal static class loans_SetBankruptcyDanger_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(out BankruptcyDangerSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateBankruptcyDangerSnapshot();
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(bool val, BankruptcyDangerSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureBankruptcyDangerSet(val, __state);
        }
    }

    /// <summary>
    /// Captures bankruptcy run-fail check triggers.
    /// </summary>
    [HarmonyPatch(typeof(Bankruptcy), CoreConstants.HarmonyBankruptcyCheckBankruptcyMethodName)]
    internal static class Bankruptcy_CheckBankruptcy_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(out BankruptcyCheckSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateBankruptcyCheckSnapshot();
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(BankruptcyCheckSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureBankruptcyCheck(__state);
        }
    }

    /// <summary>
    /// Captures scandal run-fail check triggers.
    /// </summary>
    [HarmonyPatch(typeof(Bankruptcy), CoreConstants.HarmonyBankruptcyCheckScandalMethodName, new Type[] { typeof(bool) })]
    internal static class Bankruptcy_CheckScandal_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Bankruptcy __instance, bool Test_GO, out ScandalCheckSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateScandalCheckSnapshot(__instance, Test_GO);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ScandalCheckSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureScandalCheck(__state);
        }
    }

    /// <summary>
    /// Captures audition failure hard-fail lifecycle milestone.
    /// </summary>
    [HarmonyPatch(typeof(Bankruptcy), nameof(Bankruptcy.TriggerAuditionFailure))]
    internal static class Bankruptcy_TriggerAuditionFailure_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(out AuditionFailureSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateAuditionFailureSnapshot();
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(AuditionFailureSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureAuditionFailureTriggered(__state);
        }
    }

    /// <summary>
    /// Captures policy decision selections.
    /// </summary>
    [HarmonyPatch(typeof(policies.value), CoreConstants.HarmonyPoliciesValueSelectMethodName, new Type[] { typeof(bool) })]
    internal static class policies_value_Select_Internal_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(policies.value __instance, out PolicySelectionSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreatePolicySelectionSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(policies.value __instance, bool free, PolicySelectionSnapshot __state)
        {
            IMDataCoreController.Instance.CapturePolicyDecision(__instance, free, __state);
        }
    }

}
