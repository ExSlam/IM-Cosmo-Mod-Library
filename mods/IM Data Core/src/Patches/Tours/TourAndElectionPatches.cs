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
    /// Captures world-tour creation lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(SEvent_Tour), nameof(SEvent_Tour.SetTour))]
    internal static class SEvent_Tour_SetTour_IMDataCoreCapture_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEvent_Tour.tour __0)
        {
            IMDataCoreController.Instance.CaptureTourCreated(__0);
        }
    }

    /// <summary>
    /// Captures world-tour start lifecycle events and participant snapshots.
    /// </summary>
    [HarmonyPatch(typeof(SEvent_Tour), nameof(SEvent_Tour.StartTour))]
    internal static class SEvent_Tour_StartTour_IMDataCoreCapture_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEvent_Tour __instance)
        {
            if (__instance == null)
            {
                return;
            }

            IMDataCoreController.Instance.CaptureTourStarted(__instance.Tour);
        }
    }

    /// <summary>
    /// Captures world-tour finish lifecycle events, country outcomes, and participant rows.
    /// </summary>
    [HarmonyPatch(typeof(SEvent_Tour), nameof(SEvent_Tour.FinishTour))]
    internal static class SEvent_Tour_FinishTour_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(SEvent_Tour __instance, out TourFinishSnapshot __state)
        {
            ActivityEarningsSourceContext.Set(CoreConstants.EarningsSourceTourFinish);
            __state = IMDataCoreController.Instance.CreateTourFinishSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(TourFinishSnapshot __state)
        {
            try
            {
                IMDataCoreController.Instance.CaptureTourFinished(__state);
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
    /// Captures world-tour cancellation lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(SEvent_Tour), nameof(SEvent_Tour.CancelTour))]
    internal static class SEvent_Tour_CancelTour_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(SEvent_Tour __instance, out SEvent_Tour.tour __state)
        {
            __state = __instance != null ? __instance.Tour : null;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEvent_Tour.tour __state)
        {
            IMDataCoreController.Instance.CaptureTourCancelled(__state);
        }
    }

    /// <summary>
    /// Captures world-tour status transitions.
    /// </summary>
    [HarmonyPatch(typeof(SEvent_Tour.tour), nameof(SEvent_Tour.tour.SetStatus))]
    internal static class SEvent_Tour_tour_SetStatus_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures previous tour status before mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(SEvent_Tour.tour __instance, out SEvent_Tour.tour._status __state)
        {
            __state = __instance != null ? __instance.Status : SEvent_Tour.tour._status.normal;
        }

        /// <summary>
        /// Records one tour status change after mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEvent_Tour.tour __instance, SEvent_Tour.tour._status newStatus, SEvent_Tour.tour._status __state)
        {
            SEvent_Tour.tour._status finalStatus = __instance != null ? __instance.Status : __state;
            IMDataCoreController.Instance.CaptureTourStatusTransition(__instance, __state, finalStatus);
        }
    }

    /// <summary>
    /// Captures election creation lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(SEvent_SSK), nameof(SEvent_SSK.SetSSK))]
    internal static class SEvent_SSK_SetSSK_IMDataCoreCapture_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEvent_SSK._SSK __0)
        {
            IMDataCoreController.Instance.CaptureElectionCreated(__0);
        }
    }

    /// <summary>
    /// Captures election cancellation lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(SEvent_SSK), nameof(SEvent_SSK.CancelSSK))]
    internal static class SEvent_SSK_CancelSSK_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(SEvent_SSK __instance, out SEvent_SSK._SSK __state)
        {
            __state = __instance != null ? __instance.SSK : null;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEvent_SSK._SSK __state)
        {
            IMDataCoreController.Instance.CaptureElectionCancelled(__state);
        }
    }

    /// <summary>
    /// Captures generated election result snapshots.
    /// </summary>
    [HarmonyPatch(typeof(SEvent_SSK._SSK), nameof(SEvent_SSK._SSK.GenerateResults))]
    internal static class SEvent_SSK_SSK_GenerateResults_IMDataCoreCapture_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEvent_SSK._SSK __instance)
        {
            IMDataCoreController.Instance.CaptureElectionResultsGenerated(__instance);
        }
    }

    /// <summary>
    /// Captures manual election place adjustments.
    /// </summary>
    [HarmonyPatch(typeof(SEvent_SSK._SSK), nameof(SEvent_SSK._SSK.SetPlace))]
    internal static class SEvent_SSK_SSK_SetPlace_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(SEvent_SSK._SSK __instance, int Place, out ElectionPlaceAdjustmentSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateElectionPlaceAdjustmentSnapshot(__instance, Place);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEvent_SSK._SSK __instance, ElectionPlaceAdjustmentSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureElectionPlaceAdjusted(__instance, __state);
        }
    }

    /// <summary>
    /// Captures election status transitions.
    /// </summary>
    [HarmonyPatch(typeof(SEvent_SSK._SSK), nameof(SEvent_SSK._SSK.SetStatus))]
    internal static class SEvent_SSK_SSK_SetStatus_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures previous election status before mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(SEvent_SSK._SSK __instance, out SEvent_Tour.tour._status __state)
        {
            __state = __instance != null ? __instance.Status : SEvent_Tour.tour._status.normal;
        }

        /// <summary>
        /// Records one election status change after mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEvent_SSK._SSK __instance, SEvent_Tour.tour._status newStatus, SEvent_Tour.tour._status __state)
        {
            SEvent_Tour.tour._status finalStatus = __instance != null ? __instance.Status : __state;
            IMDataCoreController.Instance.CaptureElectionStatusTransition(__instance, __state, finalStatus);
        }
    }

    /// <summary>
    /// Captures final election results.
    /// </summary>
    [HarmonyPatch(typeof(SEvent_SSK._SSK), nameof(SEvent_SSK._SSK.Finish))]
    internal static class SEvent_SSK_SSK_Finish_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Records per-idol election result rows after election finish logic.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEvent_SSK._SSK __instance)
        {
            IMDataCoreController.Instance.CaptureElectionResults(__instance);
        }
    }

    /// <summary>
}
