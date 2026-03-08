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
    /// Captures concert creation/setup events.
    /// </summary>
    [HarmonyPatch(typeof(SEvent_Concerts), nameof(SEvent_Concerts.SetConcert))]
    internal static class SEvent_Concerts_SetConcert_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Records one concert-created event after the concert has been assigned and initialized.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEvent_Concerts._concert __0)
        {
            IMDataCoreController.Instance.CaptureConcertCreated(__0);
        }
    }

    /// <summary>
    /// Captures concert start events.
    /// </summary>
    [HarmonyPatch(typeof(SEvent_Concerts), nameof(SEvent_Concerts.StartConcert))]
    internal static class SEvent_Concerts_StartConcert_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Records one concert-started event after launch costs and UI flow are applied.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEvent_Concerts __instance)
        {
            SEvent_Concerts._concert activeConcert = __instance != null ? __instance.Concert : null;
            IMDataCoreController.Instance.CaptureConcertStarted(activeConcert);
        }
    }

    /// <summary>
    /// Captures concert cancellation lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(SEvent_Concerts), nameof(SEvent_Concerts.CancelConcert))]
    internal static class SEvent_Concerts_CancelConcert_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(SEvent_Concerts __instance, out SEvent_Concerts._concert __state)
        {
            __state = __instance != null ? __instance.Concert : null;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEvent_Concerts._concert __state)
        {
            IMDataCoreController.Instance.CaptureConcertCancelled(__state);
        }
    }

    /// <summary>
    /// Captures concert cast-change events when one idol is removed from setlist items.
    /// </summary>
    [HarmonyPatch(typeof(SEvent_Concerts._concert), nameof(SEvent_Concerts._concert.RemoveGirl))]
    internal static class SEvent_Concerts_concert_RemoveGirl_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures concert cast snapshot before setlist removal mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(SEvent_Concerts._concert __instance, out ConcertCastChangeSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateConcertCastChangeSnapshot(__instance);
        }

        /// <summary>
        /// Records concert cast-change event after setlist removal logic completes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEvent_Concerts._concert __instance, data_girls.girls __0, ConcertCastChangeSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureConcertCastChanged(__instance, __0, __state);
        }
    }

    /// <summary>
    /// Captures concert status transitions.
    /// </summary>
    [HarmonyPatch(typeof(SEvent_Concerts._concert), nameof(SEvent_Concerts._concert.SetStatus))]
    internal static class SEvent_Concerts_concert_SetStatus_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures previous concert status before mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(SEvent_Concerts._concert __instance, out SEvent_Tour.tour._status __state)
        {
            __state = __instance != null ? __instance.Status : SEvent_Tour.tour._status.normal;
        }

        /// <summary>
        /// Records one concert status change after mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEvent_Concerts._concert __instance, SEvent_Tour.tour._status newStatus, SEvent_Tour.tour._status __state)
        {
            SEvent_Tour.tour._status finalStatus = __instance != null ? __instance.Status : __state;
            IMDataCoreController.Instance.CaptureConcertStatusTransition(__instance, __state, finalStatus);
        }
    }

    /// <summary>
    /// Captures concert finish events and per-idol participation records.
    /// </summary>
    [HarmonyPatch(typeof(SEvent_Concerts._concert), nameof(SEvent_Concerts._concert.Finish))]
    internal static class SEvent_Concerts_concert_Finish_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Records one concert-finished event after all concert finalization logic completes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEvent_Concerts._concert __instance)
        {
            IMDataCoreController.Instance.CaptureConcertFinished(__instance);
        }
    }

    /// <summary>
    /// Captures baseline snapshots when editing an existing concert in popup UI.
    /// </summary>
    [HarmonyPatch(typeof(Concert_New_Popup), nameof(Concert_New_Popup.Reset))]
    internal static class Concert_New_Popup_Reset_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Registers one baseline snapshot for existing-concert popup edit sessions.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(SEvent_Concerts._concert _Concert)
        {
            IMDataCoreController.Instance.RegisterConcertEditBaseline(_Concert);
        }
    }

    /// <summary>
    /// Captures committed concert edit changes from popup UI.
    /// </summary>
    [HarmonyPatch(typeof(Concert_New_Popup), nameof(Concert_New_Popup.OnContinue))]
    internal static class Concert_New_Popup_OnContinue_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Records one concert cast/configuration change event after popup continue commits edits.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Concert_New_Popup __instance)
        {
            if (__instance == null)
            {
                return;
            }

            IMDataCoreController.Instance.CaptureConcertCastChangedFromPopup(__instance.Concert);
        }
    }

    /// <summary>
    /// Captures generated awards nomination rows.
    /// </summary>
    [HarmonyPatch(typeof(Awards), nameof(Awards.GenerateTempNominations))]
    internal static class Awards_GenerateTempNominations_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Records nomination events after nomination candidates are rebuilt.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            IMDataCoreController.Instance.CaptureAwardNominations();
        }
    }

    /// <summary>
    /// Captures finalized awards outcomes.
    /// </summary>
    [HarmonyPatch(typeof(Awards), nameof(Awards.SetWins))]
    internal static class Awards_SetWins_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Records award result events after win/loss evaluation is complete.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            IMDataCoreController.Instance.CaptureAwardResults();
        }
    }

}
