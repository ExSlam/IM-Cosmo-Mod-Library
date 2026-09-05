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
    internal sealed class SingleCancellationReferenceSnapshot
    {
        internal int LinkedElectionId = CoreConstants.InvalidIdValue;
        internal bool LinkedElectionReferenceKnown;
    }

    internal sealed class SingleCancellationElectionContextFrame
    {
        internal singles._single Single;
        internal int LinkedElectionId = CoreConstants.InvalidIdValue;
        internal bool LinkedElectionReferenceKnown;
    }

    internal static class SingleCancellationElectionContext
    {
        [ThreadStatic]
        private static List<SingleCancellationElectionContextFrame> frames;

        internal static SingleCancellationElectionContextFrame Push(singles._single single)
        {
            SingleCancellationElectionContextFrame frame = new SingleCancellationElectionContextFrame
            {
                Single = single,
                LinkedElectionReferenceKnown = single != null
            };
            if (single != null && single.IsElectionSingle)
            {
                SEvent_SSK._SSK linkedElection = single.GetParentSSK();
                frame.LinkedElectionId = linkedElection != null
                    ? linkedElection.ID
                    : CoreConstants.InvalidIdValue;
            }

            if (frames == null)
            {
                frames = new List<SingleCancellationElectionContextFrame>();
            }
            frames.Add(frame);
            return frame;
        }

        internal static void Pop(SingleCancellationElectionContextFrame frame)
        {
            if (frame == null || frames == null)
            {
                return;
            }

            for (int index = frames.Count - 1; index >= CoreConstants.ZeroBasedListStartIndex; index--)
            {
                if (ReferenceEquals(frames[index], frame))
                {
                    frames.RemoveAt(index);
                    break;
                }
            }
            if (frames.Count == CoreConstants.ZeroBasedListStartIndex)
            {
                frames = null;
            }
        }

        internal static bool TryResolve(
            singles._single single,
            out int linkedElectionId,
            out bool referenceKnown)
        {
            linkedElectionId = CoreConstants.InvalidIdValue;
            referenceKnown = false;
            if (single == null || frames == null)
            {
                return false;
            }

            for (int index = frames.Count - 1; index >= CoreConstants.ZeroBasedListStartIndex; index--)
            {
                SingleCancellationElectionContextFrame frame = frames[index];
                if (frame == null || !ReferenceEquals(frame.Single, single))
                {
                    continue;
                }

                linkedElectionId = frame.LinkedElectionId;
                referenceKnown = frame.LinkedElectionReferenceKnown;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Captures single-creation lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(singles), nameof(singles.AddNewSingle))]
    internal static class singles_AddNewSingle_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Records one single-created event after production row is added.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(singles._single single)
        {
            IMDataCoreController.Instance.CaptureSingleCreated(single);
        }
    }

    /// <summary>
    /// Captures final single-release state after all modded postfix changes complete.
    /// </summary>
    [HarmonyPatch(typeof(singles), nameof(singles.ReleaseSingle))]
    internal static class singles_ReleaseSingle_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Preserves the final ordered senbatsu before vanilla removes graduated
        /// idols from the released single's live formation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(
            singles._single single,
            out SingleReleaseSnapshot __state)
        {
            __state = null;
            try
            {
                __state =
                    IMDataCoreController.Instance.CreateSingleReleaseSnapshot(single);
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "Pre-release single senbatsu snapshot failed without " +
                    "blocking gameplay: " + exception.Message);
            }
        }

        /// <summary>
        /// Records the shared release result after release calculations finish.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(
            singles._single single,
            SingleReleaseSnapshot __state)
        {
            try
            {
                IMDataCoreController.Instance.CaptureSingleReleased(single, __state);
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "Post-release single observation failed without blocking " +
                    "gameplay: " + exception.Message);
            }
        }
    }

    /// <summary>
    /// Captures chart-position resolution when chart popup renders ranked rows.
    /// </summary>
    [HarmonyPatch(typeof(Chart_Song), nameof(Chart_Song.Set))]
    internal static class Chart_Song_Set_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Records chart-position backfill for one player single.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Rivals._group._single Single, int Number)
        {
            if (Single == null || !Single.Player || Number <= CoreConstants.ZeroBasedListStartIndex)
            {
                return;
            }

            singles._single playerSingle = Single.GetSingle();
            if (playerSingle == null)
            {
                return;
            }

            try
            {
                IMDataCoreController.Instance.CaptureSingleChartPositionResolved(
                    playerSingle,
                    Number,
                    CoreConstants.EventSourceSingleChartPopupPatch);
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "Single chart observation failed without blocking gameplay: " +
                    exception.Message);
            }
        }
    }

    /// <summary>
    /// Tags idol earnings generated by single-release payout flow.
    /// </summary>
    [HarmonyPatch(typeof(singles), CoreConstants.HarmonySinglesAddMoneyMethodName, new Type[] { typeof(singles._single) })]
    internal static class singles_AddMoney_IMDataCoreEarningsSource_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix()
        {
            ActivityEarningsSourceContext.Push(CoreConstants.EarningsSourceSingleRelease);
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
    /// Preserves the election-release parent before the UI clears ReleaseSingle.
    /// The frame is thread-local, keyed to the exact single, nesting-safe, and
    /// always removed by a Harmony finalizer.
    /// </summary>
    [HarmonyPatch(typeof(SingleInDevelopmentButton), nameof(SingleInDevelopmentButton.OnCancel))]
    internal static class SingleInDevelopmentButton_OnCancel_IMDataCoreReference_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(
            SingleInDevelopmentButton __instance,
            out SingleCancellationElectionContextFrame __state)
        {
            __state = SingleCancellationElectionContext.Push(
                __instance != null ? __instance.single : null);
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        private static Exception Finalizer(
            Exception __exception,
            SingleCancellationElectionContextFrame __state)
        {
            SingleCancellationElectionContext.Pop(__state);
            return __exception;
        }
    }

    /// <summary>
    /// Captures single-cancellation lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(singles), nameof(singles.CancelSingle))]
    internal static class singles_CancelSingle_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(
            singles._single __0,
            out SingleCancellationReferenceSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateSingleCancellationReferenceSnapshot(__0);
        }

        /// <summary>
        /// Records one single-cancel event after cancellation logic completes.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(
            singles._single __0,
            SingleCancellationReferenceSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureSingleCancelled(__0, __state);
        }
    }

    /// <summary>
    /// Captures single status transitions.
    /// </summary>
    [HarmonyPatch(typeof(singles._single), nameof(singles._single.SetStatus))]
    internal static class singles_single_SetStatus_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures previous single status before mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(singles._single __instance, out singles._single._status __state)
        {
            __state = __instance != null ? __instance.status : singles._single._status.normal;
        }

        /// <summary>
        /// Records single status-change event after mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(singles._single __instance, singles._single._status __state)
        {
            singles._single._status finalStatus = __instance != null ? __instance.status : __state;
            IMDataCoreController.Instance.CaptureSingleStatusTransition(__instance, __state, finalStatus);
        }
    }

    /// <summary>
    /// Captures single cast-change events when one idol is removed from single cast.
    /// </summary>
    [HarmonyPatch(typeof(singles._single), nameof(singles._single.RemoveGirl))]
    [HarmonyBefore(new[] { "com.cosmo.unavailableidolsfix" })]
    internal static class singles_single_RemoveGirl_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures single cast snapshot before removal mutation.
        /// </summary>
        [HarmonyPriority(Priority.First)]
        private static void Prefix(singles._single __instance, out SingleCastChangeSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateSingleCastChangeSnapshot(__instance);
        }

        /// <summary>
        /// Records cast-change event after removal logic completes. Missing Harmony
        /// state means an earlier Prefix prevented the pre-state snapshot, so no
        /// historical transition can be inferred safely.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(singles._single __instance, data_girls.girls __0, SingleCastChangeSnapshot __state)
        {
            if (__state == null)
            {
                return;
            }

            IMDataCoreController.Instance.CaptureSingleCastChanged(__instance, __0, __state);
        }
    }

    /// <summary>
    /// Captures single cast changes committed from the single senbatsu popup.
    /// </summary>
    [HarmonyPatch(typeof(SinglePopup_Senbatsu), nameof(SinglePopup_Senbatsu.OnConfirm))]
    internal static class SinglePopup_Senbatsu_OnConfirm_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures pre-commit single cast state for existing single edits.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(SinglePopup_Senbatsu __instance, out SingleCastChangeSnapshot __state)
        {
            if (__instance == null || __instance.is_new || __instance.single == null)
            {
                __state = null;
                return;
            }

            __state = IMDataCoreController.Instance.CreateSingleCastChangeSnapshot(__instance.single);
        }

        /// <summary>
        /// Records one single cast-change event after popup commit applies.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SinglePopup_Senbatsu __instance, SingleCastChangeSnapshot __state)
        {
            if (__instance == null || __instance.is_new || __state == null)
            {
                return;
            }

            IMDataCoreController.Instance.CaptureSingleCastChangedFromPopup(__instance.single, __state);
            IMDataCoreController.Instance.CaptureSingleGroupChangedFromPopup(__instance.single, __state);
        }
    }

}
