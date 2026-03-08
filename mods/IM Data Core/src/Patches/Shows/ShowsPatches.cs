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
    /// Captures show-creation events when a new show is added.
    /// </summary>
    [HarmonyPatch(typeof(Shows), nameof(Shows.AddNewShow))]
    internal static class Shows_AddNewShow_IMDataCoreCapture_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Shows._show __0)
        {
            IMDataCoreController.Instance.CaptureShowCreated(__0);
        }
    }

    /// <summary>
    /// Captures show-release lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(Shows), nameof(Shows.ReleaseShow))]
    internal static class Shows_ReleaseShow_IMDataCoreCapture_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Shows._show __0)
        {
            IMDataCoreController.Instance.CaptureShowReleased(__0);
        }
    }

    /// <summary>
    /// Captures show cancellation lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(Shows), nameof(Shows.CancelShow))]
    internal static class Shows_CancelShow_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures previous status so repeated cancel calls on already-canceled shows are ignored.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Shows._show __0, out Shows._show._status __state)
        {
            __state = __0 != null ? __0.status : Shows._show._status.normal;
        }

        /// <summary>
        /// Records one show-cancelled event after cancellation logic completes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Shows._show __0, Shows._show._status __state)
        {
            if (__0 == null || __state == Shows._show._status.canceled || __0.status != Shows._show._status.canceled)
            {
                return;
            }

            IMDataCoreController.Instance.CaptureShowCancelled(__0);
        }
    }

    /// <summary>
    /// Captures direct show cancellation calls to cover auto-cancel paths.
    /// </summary>
    [HarmonyPatch(typeof(Shows._show), nameof(Shows._show.Cancel))]
    internal static class Shows_show_Cancel_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures previous status so no-op cancel loops do not emit duplicate lifecycle rows.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Shows._show __instance, out Shows._show._status __state)
        {
            __state = __instance != null ? __instance.status : Shows._show._status.normal;
        }

        /// <summary>
        /// Records one show-cancelled event after direct show cancel logic completes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Shows._show __instance, Shows._show._status __state)
        {
            if (__instance == null || __state == Shows._show._status.canceled || __instance.status != Shows._show._status.canceled)
            {
                return;
            }

            IMDataCoreController.Instance.CaptureShowCancelled(__instance, CoreConstants.EventSourceShowCancelMethodPatch);
        }
    }

    /// <summary>
    /// Captures show relaunch-start lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(Shows._show), nameof(Shows._show.OnRelaunchStart))]
    internal static class Shows_show_OnRelaunchStart_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Records one show-relaunch-start event after relaunch status is applied.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Shows._show __instance)
        {
            IMDataCoreController.Instance.CaptureShowRelaunchStarted(__instance);
        }
    }

    /// <summary>
    /// Captures show relaunch-finish lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(Shows._show), nameof(Shows._show.OnRelaunchFinish))]
    internal static class Shows_show_OnRelaunchFinish_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Records one show-relaunch-finish event after relaunch counters are applied.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Shows._show __instance)
        {
            IMDataCoreController.Instance.CaptureShowRelaunchFinished(__instance);
        }
    }

    /// <summary>
    /// Captures show status transitions.
    /// </summary>
    [HarmonyPatch(typeof(Shows._show), nameof(Shows._show.SetStatus))]
    internal static class Shows_show_SetStatus_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures previous show status before mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Shows._show __instance, out Shows._show._status __state)
        {
            __state = __instance != null ? __instance.status : Shows._show._status.normal;
        }

        /// <summary>
        /// Records show status change after mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Shows._show __instance, Shows._show._status newStatus, Shows._show._status __state)
        {
            Shows._show._status finalStatus = __instance != null ? __instance.status : __state;
            IMDataCoreController.Instance.CaptureShowStatusTransition(__instance, __state, finalStatus);
        }
    }

    /// <summary>
    /// Captures one completed show episode.
    /// </summary>
    [HarmonyPatch(typeof(Shows._show), nameof(Shows._show.NewEpisode))]
    internal static class Shows_show_NewEpisode_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Records one show-episode event after all episode calculations complete.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Shows._show __instance)
        {
            IMDataCoreController.Instance.CaptureShowEpisodeReleased(__instance);
        }
    }

    /// <summary>
    /// Tags idol earnings generated by show revenue payout flow.
    /// </summary>
    [HarmonyPatch(typeof(Shows._show), CoreConstants.HarmonyShowsShowSetRevenueMethodName)]
    internal static class Shows_show_SetRevenue_IMDataCoreEarningsSource_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix()
        {
            ActivityEarningsSourceContext.Set(CoreConstants.EarningsSourceShowRevenue);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            ActivityEarningsSourceContext.Clear();
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
    /// Captures show cast-change events when one idol is removed from a show cast.
    /// </summary>
    [HarmonyPatch(typeof(Shows._show), nameof(Shows._show.RemoveGirl))]
    internal static class Shows_show_RemoveGirl_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures show cast snapshot before removal mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Shows._show __instance, out ShowCastChangeSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateShowCastChangeSnapshot(__instance);
        }

        /// <summary>
        /// Records cast-change event after removal logic completes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Shows._show __instance, data_girls.girls __0, ShowCastChangeSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureShowCastChanged(__instance, __0, __state);
        }
    }

    /// <summary>
    /// Captures show cast edits committed from show popup saves.
    /// </summary>
    [HarmonyPatch(typeof(Show_Popup), nameof(Show_Popup.OnContinue))]
    internal static class Show_Popup_OnContinue_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures pre-commit show cast state for existing show edits.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Show_Popup __instance, out ShowCastChangeSnapshot __state)
        {
            Shows._show editingShow = __instance != null ? __instance._Show : null;
            if (editingShow == null)
            {
                __state = null;
                return;
            }

            __state = IMDataCoreController.Instance.CreateShowCastChangeSnapshot(editingShow);
        }

        /// <summary>
        /// Records one show cast-change event after popup commit applies.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Show_Popup __instance, ShowCastChangeSnapshot __state)
        {
            Shows._show editingShow = __instance != null ? __instance._Show : null;
            if (editingShow == null || __state == null)
            {
                return;
            }

            IMDataCoreController.Instance.CaptureShowCastChangedFromPopup(editingShow, __state);
            IMDataCoreController.Instance.CaptureShowConfigurationChangedFromPopup(editingShow, __state);
        }
    }

    /// <summary>
}
