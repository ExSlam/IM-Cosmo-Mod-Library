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
    /// <summary>
    /// Thread-scoped bridge that tags idol earnings with the currently executing activity source.
    /// </summary>
    internal static class ActivityEarningsSourceContext
    {
        [ThreadStatic]
        private static string currentSourceCode;

        internal static void Set(string sourceCode)
        {
            currentSourceCode = sourceCode ?? CoreConstants.EarningsSourceUnknown;
        }

        internal static string Get()
        {
            return string.IsNullOrEmpty(currentSourceCode)
                ? CoreConstants.EarningsSourceUnknown
                : currentSourceCode;
        }

        internal static void Clear()
        {
            currentSourceCode = string.Empty;
        }
    }

    /// <summary>
    /// Thread-scoped bridge between crisis decision and close hooks.
    /// </summary>
    internal static class ConcertCrisisChoiceContext
    {
        [ThreadStatic]
        private static ConcertCrisisChoiceSnapshot currentSnapshot;

        internal static void Set(ConcertCrisisChoiceSnapshot snapshot)
        {
            currentSnapshot = snapshot;
        }

        internal static ConcertCrisisChoiceSnapshot Get()
        {
            return currentSnapshot;
        }

        internal static void Clear()
        {
            currentSnapshot = null;
        }
    }

    /// <summary>
    /// Captures performance activity outcomes and earnings source attribution.
    /// </summary>
    [HarmonyPatch(typeof(Activities), nameof(Activities.Performance))]
    internal static class Activities_Performance_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(out ActivityActionSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateActivityActionSnapshot();
            ActivityEarningsSourceContext.Set(CoreConstants.EarningsSourceActivitiesPerformance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Activities __instance, ActivityActionSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureActivityPerformance(__instance, __state);
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
    /// Captures promotion activity outcomes.
    /// </summary>
    [HarmonyPatch(typeof(Activities), nameof(Activities.Promotion))]
    internal static class Activities_Promotion_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(out ActivityActionSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateActivityActionSnapshot();
            ActivityEarningsSourceContext.Set(CoreConstants.EarningsSourceActivitiesPromotion);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Activities __instance, ActivityActionSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureActivityPromotion(__instance, __state);
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
    /// Captures spa treatment activity outcomes.
    /// </summary>
    [HarmonyPatch(typeof(Activities), nameof(Activities.SpaTreatment))]
    internal static class Activities_SpaTreatment_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(out ActivityActionSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateActivityActionSnapshot();
            ActivityEarningsSourceContext.Set(CoreConstants.EarningsSourceActivitiesSpaTreatment);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ActivityActionSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureActivitySpaTreatment(__state);
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
    /// Captures idol earnings mutations with source attribution.
    /// </summary>
    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Earn))]
    internal static class data_girls_girls_Earn_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls __instance, out IdolEarningsSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateIdolEarningsSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls __instance, long val, IdolEarningsSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureIdolEarnings(
                __instance,
                val,
                __state,
                ActivityEarningsSourceContext.Get());
        }
    }

    /// <summary>
    /// Captures theater creation lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(Theaters), nameof(Theaters.NewTheater))]
    internal static class Theaters_NewTheater_IMDataCoreCapture_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(agency._room Room)
        {
            IMDataCoreController.Instance.CaptureTheaterCreated(Room);
        }
    }

    /// <summary>
    /// Captures theater destruction lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(Theaters), nameof(Theaters.DestroyTheater))]
    internal static class Theaters_DestroyTheater_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(int TheaterID, out TheaterLifecycleSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateTheaterDestroySnapshot(TheaterID);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(TheaterLifecycleSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureTheaterDestroyed(__state);
        }
    }

    /// <summary>
    /// Captures per-theater daily completion outcomes.
    /// </summary>
    [HarmonyPatch(typeof(Theaters), CoreConstants.HarmonyTheatersCompleteDayMethodName)]
    internal static class Theaters_CompleteDay_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(out TheaterCompleteDaySnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateTheaterCompleteDaySnapshot();
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(TheaterCompleteDaySnapshot __state)
        {
            IMDataCoreController.Instance.CaptureTheaterCompleteDay(__state);
        }
    }

    /// <summary>
    /// Captures cafe creation lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(Cafes), nameof(Cafes.NewCafe))]
    internal static class Cafes_NewCafe_IMDataCoreCapture_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(agency._room Room)
        {
            IMDataCoreController.Instance.CaptureCafeCreated(Room);
        }
    }

    /// <summary>
    /// Captures cafe destruction lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(Cafes), nameof(Cafes.DestroyCafe))]
    internal static class Cafes_DestroyCafe_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(int CafeID, out CafeLifecycleSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateCafeDestroySnapshot(CafeID);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(CafeLifecycleSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureCafeDestroyed(__state);
        }
    }

    /// <summary>
    /// Captures cafe daily render outcomes.
    /// </summary>
    [HarmonyPatch(typeof(Cafes), CoreConstants.HarmonyCafesRenderCafeMethodName, new Type[] { typeof(agency._room), typeof(Cafes._cafe) })]
    internal static class Cafes_RenderCafe_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Cafes._cafe Cafe, out CafeRenderSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateCafeRenderSnapshot(Cafe);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(agency._room Room, Cafes._cafe Cafe, CafeRenderSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureCafeRenderResult(Room, Cafe, __state);
        }
    }

    /// <summary>
    /// Captures staff hire lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(staff), nameof(staff.Hire))]
    internal static class staff_Hire_IMDataCoreCapture_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(staff._staff Staffer)
        {
            IMDataCoreController.Instance.CaptureStaffHired(Staffer);
        }
    }

    /// <summary>
    /// Captures staff firing lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(staff._staff), nameof(staff._staff.Fire))]
    internal static class staff_staff_Fire_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(staff._staff __instance, out StaffLifecycleSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateStaffLifecycleSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(staff._staff __instance, bool Force, StaffLifecycleSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureStaffFired(__instance, Force, __state);
        }
    }

    /// <summary>
    /// Captures staff severance firing lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(staff._staff), nameof(staff._staff.Fire_Severance))]
    internal static class staff_staff_Fire_Severance_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(staff._staff __instance, out StaffLifecycleSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateStaffLifecycleSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(staff._staff __instance, StaffLifecycleSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureStaffFiredSeverance(__instance, __state);
        }
    }

    /// <summary>
    /// Captures staff promotion lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(staff._staff), nameof(staff._staff.LevelUp))]
    internal static class staff_staff_LevelUp_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(staff._staff __instance, out StaffLifecycleSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateStaffLifecycleSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(staff._staff __instance, StaffLifecycleSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureStaffLevelUp(__instance, __state);
        }
    }

    /// <summary>
    /// Captures research category parameter assignment intent.
    /// </summary>
    [HarmonyPatch(typeof(Research.category), nameof(Research.category.SetParam))]
    internal static class Research_category_SetParam_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Research.category __instance, out ResearchSetParamSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateResearchSetParamSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Research.category __instance, singles._param prm, ResearchSetParamSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureResearchParamAssigned(__instance, prm, __state);
        }
    }

    /// <summary>
    /// Captures research points purchase progression events.
    /// </summary>
    [HarmonyPatch(typeof(Research.category), nameof(Research.category.Buy_Points))]
    internal static class Research_category_Buy_Points_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Research.category __instance, out ResearchBuyPointsSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateResearchBuyPointsSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Research.category __instance, ResearchBuyPointsSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureResearchPointsPurchased(__instance, __state);
        }
    }

    /// <summary>
    /// Captures passive/active research point accrual ticks.
    /// </summary>
    [HarmonyPatch(typeof(Research.category), nameof(Research.category.AddPoints))]
    internal static class Research_category_AddPoints_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Research.category __instance, out ResearchAddPointsSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateResearchAddPointsSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Research.category __instance, float val, ResearchAddPointsSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureResearchPointsAccrued(__instance, val, __state);
        }
    }

    /// <summary>
    /// Captures research-backed single parameter level-up progression.
    /// </summary>
    [HarmonyPatch(typeof(singles._param), nameof(singles._param.LevelUp))]
    internal static class singles_param_LevelUp_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(singles._param __instance, out ResearchParamLevelUpSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateResearchParamLevelUpSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(singles._param __instance, ResearchParamLevelUpSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureResearchParamLevelUp(__instance, __state);
        }
    }

    /// <summary>
    /// Captures story route lock outcomes.
    /// </summary>
    [HarmonyPatch(typeof(tasks), nameof(tasks.LockRoute))]
    internal static class tasks_LockRoute_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(tasks._route Route, out RouteLockSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateRouteLockSnapshot();
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(tasks._route Route, RouteLockSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureRouteLocked(Route, __state);
        }
    }

    /// <summary>
    /// Captures story task completion outcomes.
    /// </summary>
    [HarmonyPatch(typeof(tasks._task), nameof(tasks._task.OnComplete))]
    internal static class tasks_task_OnComplete_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(tasks._task __instance, out TaskLifecycleSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateTaskLifecycleSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(tasks._task __instance, TaskLifecycleSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureTaskCompleted(__instance, __state);
        }
    }

    /// <summary>
    /// Captures story task failure outcomes.
    /// </summary>
    [HarmonyPatch(typeof(tasks._task), nameof(tasks._task.OnFail))]
    internal static class tasks_task_OnFail_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(tasks._task __instance, out TaskLifecycleSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateTaskLifecycleSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(tasks._task __instance, TaskLifecycleSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureTaskFailed(__instance, __state);
        }
    }

    /// <summary>
    /// Captures story task done outcomes.
    /// </summary>
    [HarmonyPatch(typeof(tasks._task), nameof(tasks._task.Done))]
    internal static class tasks_task_Done_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(tasks._task __instance, out TaskLifecycleSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateTaskLifecycleSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(tasks._task __instance, bool SkipLock, TaskLifecycleSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureTaskDone(__instance, SkipLock, __state);
        }
    }

    /// <summary>
    /// Captures tactical concert card usage outcomes.
    /// </summary>
    [HarmonyPatch(typeof(SEvent_Concerts), nameof(SEvent_Concerts.UseCard))]
    internal static class SEvent_Concerts_UseCard_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(SEvent_Concerts __instance, out ConcertCardUseSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateConcertCardUseSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEvent_Concerts __instance, SEvent_Concerts._card card, ConcertCardUseSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureConcertCardUsed(__instance, card, __state);
        }
    }

    /// <summary>
    /// Captures concert crisis decision details (safe/risky, result type, expected hype delta).
    /// </summary>
    [HarmonyPatch(typeof(Concert_CrisisPopup), CoreConstants.HarmonyConcertCrisisOptionMethodName, new Type[] { typeof(bool) })]
    internal static class Concert_CrisisPopup_Option_IMDataCoreCapture_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Concert_CrisisPopup __instance, bool Safe)
        {
            ConcertCrisisChoiceSnapshot decisionSnapshot = IMDataCoreController.Instance.CaptureConcertCrisisDecision(__instance, Safe);
            ConcertCrisisChoiceContext.Set(decisionSnapshot);
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        private static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                ConcertCrisisChoiceContext.Clear();
            }

            return __exception;
        }
    }

    /// <summary>
    /// Captures concert crisis close/application details after hype is applied.
    /// </summary>
    [HarmonyPatch(typeof(Concert_CrisisPopup), CoreConstants.HarmonyConcertCrisisCloseMethodName)]
    internal static class Concert_CrisisPopup_Close_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Concert_CrisisPopup __instance, out ConcertCrisisAppliedSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateConcertCrisisAppliedSnapshot(__instance, ConcertCrisisChoiceContext.Get());
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Concert_CrisisPopup __instance, ConcertCrisisAppliedSnapshot __state)
        {
            try
            {
                IMDataCoreController.Instance.CaptureConcertCrisisApplied(__instance, __state);
            }
            finally
            {
                ConcertCrisisChoiceContext.Clear();
            }
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        private static Exception Finalizer(Exception __exception)
        {
            ConcertCrisisChoiceContext.Clear();
            return __exception;
        }
    }

    /// <summary>
    /// Captures final concert tactical/economic resolution outcomes.
    /// </summary>
    [HarmonyPatch(typeof(Concert_Popup), nameof(Concert_Popup.FinishConcert))]
    internal static class Concert_Popup_FinishConcert_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Concert_Popup __instance, out ConcertFinishSnapshot __state)
        {
            ActivityEarningsSourceContext.Set(CoreConstants.EarningsSourceConcertFinish);
            __state = IMDataCoreController.Instance.CreateConcertFinishSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Concert_Popup __instance, ConcertFinishSnapshot __state)
        {
            try
            {
                IMDataCoreController.Instance.CaptureConcertFinalResolved(__instance, __state);
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
    /// Captures explicit election started milestones.
    /// </summary>
    [HarmonyPatch(typeof(SEvent_SSK), nameof(SEvent_SSK.StartSSK))]
    internal static class SEvent_SSK_StartSSK_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(SEvent_SSK __instance, out ElectionStartSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateElectionStartSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SEvent_SSK __instance, ElectionStartSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureElectionStarted(__instance != null ? __instance.SSK : null, __state);
        }
    }

    /// <summary>
    /// Captures idol wish generation events.
    /// </summary>
    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Wish_Generate))]
    internal static class data_girls_girls_Wish_Generate_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls __instance, out WishLifecycleSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateWishLifecycleSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls __instance, WishLifecycleSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureWishGenerated(__instance, __state);
        }
    }

    /// <summary>
    /// Captures idol wish fulfilled events.
    /// </summary>
    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Wish_Fullfill))]
    internal static class data_girls_girls_Wish_Fullfill_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls __instance, out WishLifecycleSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateWishLifecycleSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls __instance, WishLifecycleSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureWishFulfilled(__instance, __state);
        }
    }

    /// <summary>
    /// Captures idol wish completion events.
    /// </summary>
    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Wish_Done))]
    internal static class data_girls_girls_Wish_Done_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls __instance, out WishLifecycleSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateWishLifecycleSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls __instance, WishLifecycleSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureWishDone(__instance, __state);
        }
    }

    /// <summary>
    /// Captures scandal-point mutations from low-level parameter `add` operations.
    /// </summary>
    [HarmonyPatch(typeof(data_girls.girls.param), nameof(data_girls.girls.param.add))]
    internal static class data_girls_girls_param_add_IMDataCoreCapture_Patch
    {
        [ThreadStatic]
        private static bool isScandalParameterAddMutationInProgress;

        /// <summary>
        /// Returns true while one scandal `param.add` mutation is executing on the current thread.
        /// </summary>
        internal static bool IsScandalParameterAddMutationInProgress()
        {
            return isScandalParameterAddMutationInProgress;
        }

        /// <summary>
        /// Captures previous scandal points and marks add-mutation context for duplicate suppression.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls.param __instance, out float __state)
        {
            bool isScandalParameter = __instance != null && __instance.type == data_girls._paramType.scandalPoints;
            isScandalParameterAddMutationInProgress = isScandalParameter;

            if (!isScandalParameter)
            {
                __state = CoreConstants.ScandalPointsRawStateUnavailableValue;
                return;
            }

            __state = __instance.val;
        }

        /// <summary>
        /// Records scandal-point mutation after parameter add completes and clears add-mutation context.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls.param __instance, float __state)
        {
            try
            {
                if (__instance == null || __instance.type != data_girls._paramType.scandalPoints)
                {
                    return;
                }

                IMDataCoreController.Instance.CaptureScandalPointsMutation(
                    __instance.Parent,
                    __state,
                    CoreConstants.ScandalMutationSourceParameterAdd,
                    CoreConstants.EventSourceScandalParameterAddPatch);
            }
            finally
            {
                isScandalParameterAddMutationInProgress = false;
            }
        }

        /// <summary>
        /// Ensures add-mutation context is cleared even if game code throws during execution.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static Exception Finalizer(Exception __exception)
        {
            isScandalParameterAddMutationInProgress = false;
            return __exception;
        }
    }

    /// <summary>
    /// Captures scandal-point mutations from low-level parameter value setter operations.
    /// </summary>
    [HarmonyPatch(typeof(data_girls.girls.param), CoreConstants.HarmonyDataGirlsParameterValuePropertyName, MethodType.Setter)]
    internal static class data_girls_girls_param_val_Setter_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures previous scandal points before parameter value setter mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls.param __instance, out float __state)
        {
            if (__instance == null || __instance.type != data_girls._paramType.scandalPoints)
            {
                __state = CoreConstants.ScandalPointsRawStateUnavailableValue;
                return;
            }

            __state = __instance.val;
        }

        /// <summary>
        /// Records scandal-point mutation after parameter value setter mutation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls.param __instance, float __state)
        {
            if (__instance == null || __instance.type != data_girls._paramType.scandalPoints)
            {
                return;
            }

            if (data_girls_girls_param_add_IMDataCoreCapture_Patch.IsScandalParameterAddMutationInProgress())
            {
                return;
            }

            IMDataCoreController.Instance.CaptureScandalPointsMutation(
                __instance.Parent,
                __state,
                CoreConstants.ScandalMutationSourceParameterValueSetter,
                CoreConstants.EventSourceScandalParameterValueSetterPatch);
        }
    }

    /// <summary>
    /// Captures injury medical events.
    /// </summary>
    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Set_Injured))]
    internal static class data_girls_girls_Set_Injured_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures previous idol status before injury handling.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls __instance, out data_girls._status __state)
        {
            __state = __instance != null ? __instance.status : data_girls._status.normal;
        }

        /// <summary>
        /// Records injury medical event after game logic finishes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls __instance, data_girls._status __state)
        {
            IMDataCoreController.Instance.CaptureMedicalLifecycleEvent(
                __instance,
                CoreConstants.MedicalEventTypeInjury,
                false,
                __state,
                CoreConstants.EventSourceMedicalInjuryPatch);
        }
    }

    /// <summary>
    /// Captures depression medical events.
    /// </summary>
    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Set_Depressed))]
    internal static class data_girls_girls_Set_Depressed_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures previous idol status before depression handling.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls __instance, out data_girls._status __state)
        {
            __state = __instance != null ? __instance.status : data_girls._status.normal;
        }

        /// <summary>
        /// Records depression medical event after game logic finishes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls __instance, data_girls._status __state)
        {
            IMDataCoreController.Instance.CaptureMedicalLifecycleEvent(
                __instance,
                CoreConstants.MedicalEventTypeDepression,
                false,
                __state,
                CoreConstants.EventSourceMedicalDepressionPatch);
        }
    }

    /// <summary>
    /// Captures hiatus-start medical events.
    /// </summary>
    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.SendOnHiatus))]
    internal static class data_girls_girls_SendOnHiatus_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures previous idol status before hiatus handling.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls __instance, out data_girls._status __state)
        {
            __state = __instance != null ? __instance.status : data_girls._status.normal;
        }

        /// <summary>
        /// Records hiatus-start medical event after game logic finishes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls __instance, data_girls._status __state)
        {
            IMDataCoreController.Instance.CaptureMedicalLifecycleEvent(
                __instance,
                CoreConstants.MedicalEventTypeHiatusStarted,
                false,
                __state,
                CoreConstants.EventSourceMedicalHiatusStartPatch);
        }
    }

    /// <summary>
    /// Captures healed medical events.
    /// </summary>
    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Heal))]
    internal static class data_girls_girls_Heal_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures previous idol status before heal handling.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls __instance, out data_girls._status __state)
        {
            __state = __instance != null ? __instance.status : data_girls._status.normal;
        }

        /// <summary>
        /// Records healed medical event after game logic finishes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls __instance, data_girls._status __state)
        {
            IMDataCoreController.Instance.CaptureMedicalLifecycleEvent(
                __instance,
                CoreConstants.MedicalEventTypeHealed,
                false,
                __state,
                CoreConstants.EventSourceMedicalHealPatch);
        }
    }

    /// <summary>
    /// Captures hiatus-finish medical events.
    /// </summary>
    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.FinishHiatus))]
    internal static class data_girls_girls_FinishHiatus_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures previous idol status before hiatus-finish handling.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls __instance, out data_girls._status __state)
        {
            __state = __instance != null ? __instance.status : data_girls._status.normal;
        }

        /// <summary>
        /// Records hiatus-finish medical event after game logic finishes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls __instance, bool force, data_girls._status __state)
        {
            IMDataCoreController.Instance.CaptureMedicalLifecycleEvent(
                __instance,
                CoreConstants.MedicalEventTypeHiatusFinished,
                force,
                __state,
                CoreConstants.EventSourceMedicalHiatusFinishPatch);
        }
    }

    /// <summary>
    /// Captures idol hire lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(data_girls), nameof(data_girls.Hire))]
    internal static class data_girls_Hire_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Records one idol-hired event after hire logic completes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls _girl)
        {
            IMDataCoreController.Instance.CaptureIdolHired(_girl);
        }
    }

    /// <summary>
    /// Captures idol salary-increase events.
    /// </summary>
    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.IncreaseSalary))]
    internal static class data_girls_girls_IncreaseSalary_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures pre-mutation salary state before raise calculation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls __instance, out SalaryChangeSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateSalaryChangeSnapshot(__instance);
        }

        /// <summary>
        /// Records salary-change event after raise calculation completes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls __instance, SalaryChangeSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureIdolSalaryChanged(
                __instance,
                __state,
                CoreConstants.SalaryChangeActionIncreased,
                CoreConstants.EventSourceDataGirlsIncreaseSalaryPatch);
        }
    }

    /// <summary>
    /// Captures idol salary-lowering events.
    /// </summary>
    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.LowerSalary))]
    internal static class data_girls_girls_LowerSalary_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures pre-mutation salary state before reduction calculation.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls __instance, out SalaryChangeSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateSalaryChangeSnapshot(__instance);
        }

        /// <summary>
        /// Records salary-change event after reduction calculation completes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls __instance, SalaryChangeSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureIdolSalaryChanged(
                __instance,
                __state,
                CoreConstants.SalaryChangeActionLowered,
                CoreConstants.EventSourceDataGirlsLowerSalaryPatch);
        }
    }

    /// <summary>
    /// Captures manual salary-set events from salary popup saves.
    /// </summary>
    [HarmonyPatch(typeof(Salary_Manual), nameof(Salary_Manual.OnSave))]
    internal static class Salary_Manual_OnSave_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Captures pre-mutation salary state before manual save applies.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Salary_Manual __instance, out SalaryChangeSnapshot __state)
        {
            data_girls.girls idol = __instance != null ? __instance.Girl : null;
            __state = IMDataCoreController.Instance.CreateSalaryChangeSnapshot(idol);
        }

        /// <summary>
        /// Records salary-change event after manual save applies.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Salary_Manual __instance, SalaryChangeSnapshot __state)
        {
            data_girls.girls idol = __instance != null ? __instance.Girl : null;
            IMDataCoreController.Instance.CaptureIdolSalaryChanged(
                idol,
                __state,
                CoreConstants.SalaryChangeActionManualSet,
                CoreConstants.EventSourceSalaryManualOnSavePatch);
        }
    }

    /// <summary>
    /// Captures idol graduation-announcement lifecycle milestones.
    /// </summary>
    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Graduation_Announce_Confirm))]
    internal static class data_girls_girls_Graduation_Announce_Confirm_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Records one graduation-announced event after confirmation flow completes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls __instance)
        {
            IMDataCoreController.Instance.CaptureIdolGraduationAnnounced(__instance);
        }
    }

    /// <summary>
    /// Captures idol graduation completion lifecycle milestones.
    /// </summary>
    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Graduate))]
    internal static class data_girls_girls_Graduate_IMDataCoreCapture_Patch
    {
        /// <summary>
        /// Records one idol-graduated event after graduation logic completes.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls __instance, bool dialogue, string custom_trivia)
        {
            IMDataCoreController.Instance.CaptureIdolGraduated(__instance, dialogue, custom_trivia);
        }
    }

    /// <summary>
    /// Thread-scoped bridge between blackmail trigger and success roll patches.
    /// </summary>
    internal static class BlackmailTriggerContext
    {
        [ThreadStatic]
        private static bool inTrigger;

        [ThreadStatic]
        private static int successTier;

        internal static void Begin()
        {
            inTrigger = true;
            successTier = CoreConstants.InvalidIdValue;
        }

        internal static void CaptureResult(int result)
        {
            if (!inTrigger)
            {
                return;
            }

            successTier = result;
        }

        internal static int ConsumeResult()
        {
            int result = successTier;
            inTrigger = false;
            successTier = CoreConstants.InvalidIdValue;
            return result;
        }

        internal static void Reset()
        {
            inTrigger = false;
            successTier = CoreConstants.InvalidIdValue;
        }
    }

    /// <summary>
    /// Captures agency room build lifecycle and capex spend events.
    /// </summary>
    [HarmonyPatch(typeof(agency), CoreConstants.HarmonyAgencyAddRoomMethodName, new Type[] { typeof(int), typeof(bool) })]
    internal static class agency_addRoom_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(int type_, bool build, out AgencyRoomBuildSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateAgencyRoomBuildSnapshot(type_, build);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(agency __instance, int type_, bool build, AgencyRoomBuildSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureAgencyRoomBuilt(__instance, type_, build, __state);
        }
    }

    /// <summary>
    /// Captures agency room destruction lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(agency), CoreConstants.HarmonyAgencyDestroyRoomMethodName, new Type[] { typeof(agency._room) })]
    internal static class agency_DestroyRoom_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(agency __instance, agency._room room_to_destroy, out AgencyRoomDestroySnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateAgencyRoomDestroySnapshot(__instance, room_to_destroy);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(AgencyRoomDestroySnapshot __state)
        {
            IMDataCoreController.Instance.CaptureAgencyRoomDestroyed(__state);
        }
    }

    /// <summary>
    /// Captures audition start and spend lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(Auditions), CoreConstants.HarmonyAuditionsGenerateMethodName, new Type[] { typeof(Auditions.data), typeof(bool) })]
    internal static class Auditions_GenerateAudition_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Auditions.data _data, bool ShouldPay, out AuditionStartSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateAuditionStartSnapshot(_data);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Auditions.data _data, bool ShouldPay, AuditionStartSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureAuditionStarted(_data, ShouldPay, __state);
        }
    }

    /// <summary>
    /// Captures audition cooldown reset requests and spend outcomes.
    /// </summary>
    [HarmonyPatch(typeof(Auditions), CoreConstants.HarmonyAuditionsResetCooldownMethodName, new Type[] { typeof(CM_Player_Audition_Cooldown._type) })]
    internal static class Auditions_ResetCooldown_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(CM_Player_Audition_Cooldown._type _type, out AuditionCooldownResetSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateAuditionCooldownResetSnapshot(_type);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(CM_Player_Audition_Cooldown._type _type, AuditionCooldownResetSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureAuditionCooldownReset(_type, __state);
        }
    }

    /// <summary>
    /// Captures random event scheduling and actor assignment context.
    /// </summary>
    [HarmonyPatch(typeof(Event_Manager), nameof(Event_Manager.StartEvent), new Type[] { typeof(Event_Manager._randomEvent), typeof(DateTime), typeof(bool), typeof(data_girls.girls) })]
    internal static class Event_Manager_StartEvent_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Event_Manager __instance, out RandomEventStartSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateRandomEventStartSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(
            Event_Manager __instance,
            Event_Manager._randomEvent randomEvent,
            DateTime dateTime,
            bool force,
            RandomEventStartSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureRandomEventStarted(__instance, randomEvent, dateTime, force, __state);
        }
    }

    /// <summary>
    /// Captures random event conclusion and reply effect context.
    /// </summary>
    [HarmonyPatch(typeof(Event_Manager), nameof(Event_Manager.ConcludeEvent))]
    internal static class Event_Manager_ConcludeEvent_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Event_Manager __instance, Event_Manager._randomEvent._reply reply, out RandomEventConcludeSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateRandomEventConcludeSnapshot(__instance, reply);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Event_Manager __instance, Event_Manager._randomEvent._reply reply, RandomEventConcludeSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureRandomEventConcluded(__instance, reply, __state);
        }
    }

    /// <summary>
    /// Captures substory queueing outcomes (started vs delayed).
    /// </summary>
    [HarmonyPatch(typeof(Substories_Manager), CoreConstants.HarmonySubstoriesStartDialogueMethodName, new Type[] { typeof(data_dialogues._dialogue), typeof(DateTime), typeof(bool), typeof(Action) })]
    internal static class Substories_Manager_StartDialogue_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_dialogues._dialogue dialogue, out SubstoryStartSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateSubstoryStartSnapshot(dialogue);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_dialogues._dialogue dialogue, DateTime launchTime, bool debug, Action BeforeStart, SubstoryStartSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureSubstoryStartOrDelay(dialogue, launchTime, debug, BeforeStart, __state);
        }
    }

    /// <summary>
    /// Captures substory completion when active dialogue is closed.
    /// </summary>
    [HarmonyPatch(typeof(ActiveDialogueController), nameof(ActiveDialogueController.Hide))]
    internal static class ActiveDialogueController_Hide_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(ActiveDialogueController __instance, out SubstoryCompletionSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateSubstoryCompletionSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SubstoryCompletionSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureSubstoryCompleted(__state);
        }
    }

    /// <summary>
    /// Captures weekly passive economy expense ticks.
    /// </summary>
    [HarmonyPatch(typeof(resources), CoreConstants.HarmonyResourcesOnNewWeekMethodName)]
    internal static class resources_OnNewWeek_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(out EconomyTickSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateEconomyTickSnapshot();
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(resources __instance, EconomyTickSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureEconomyWeeklyExpense(__instance, __state);
        }
    }

    /// <summary>
    /// Captures daily passive economy deltas.
    /// </summary>
    [HarmonyPatch(typeof(resources), CoreConstants.HarmonyResourcesOnNewDayMethodName)]
    internal static class resources_OnNewDay_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(out EconomyTickSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateEconomyTickSnapshot();
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(resources __instance, EconomyTickSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureEconomyDailyTick(__instance, __state);
        }
    }

    /// <summary>
    /// Captures influence blackmail queue additions.
    /// </summary>
    [HarmonyPatch(typeof(Date_Influence), nameof(Date_Influence.AddBlackmail))]
    internal static class Date_Influence_AddBlackmail_IMDataCoreCapture_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls Spy, data_girls.girls Target)
        {
            IMDataCoreController.Instance.CaptureInfluenceBlackmailQueued(Spy, Target);
        }
    }

    /// <summary>
    /// Captures blackmail success-tier roll while blackmail trigger context is active.
    /// </summary>
    [HarmonyPatch(typeof(Date_Influence), nameof(Date_Influence.Blackmail_IsSuccess))]
    internal static class Date_Influence_Blackmail_IsSuccess_IMDataCoreCapture_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(int __result)
        {
            BlackmailTriggerContext.CaptureResult(__result);
        }
    }

    /// <summary>
    /// Captures influence blackmail trigger outcomes.
    /// </summary>
    [HarmonyPatch(typeof(Date_Influence), CoreConstants.HarmonyDateInfluenceBlackmailTriggerMethodName, new Type[] { typeof(Date_Influence._blackmail) })]
    internal static class Date_Influence_Blackmail_Trigger_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix()
        {
            BlackmailTriggerContext.Begin();
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Date_Influence._blackmail BL)
        {
            int successTier = BlackmailTriggerContext.ConsumeResult();
            IMDataCoreController.Instance.CaptureInfluenceBlackmailTriggered(BL, successTier);
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        private static Exception Finalizer(Exception __exception)
        {
            BlackmailTriggerContext.Reset();
            return __exception;
        }
    }

    /// <summary>
    /// Captures mentorship-start lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(Girls_Mentors), nameof(Girls_Mentors.AddKohai))]
    internal static class Girls_Mentors_AddKohai_IMDataCoreCapture_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls Senpai, data_girls.girls Kohai)
        {
            IMDataCoreController.Instance.CaptureMentorshipStarted(Senpai, Kohai);
        }
    }

    /// <summary>
    /// Captures mentorship-end lifecycle events.
    /// </summary>
    [HarmonyPatch(typeof(Girls_Mentors), nameof(Girls_Mentors.RemoveKohai))]
    internal static class Girls_Mentors_RemoveKohai_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(data_girls.girls Senpai, data_girls.girls Kohai, out MentorshipRemoveSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateMentorshipRemoveSnapshot(Senpai, Kohai);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(MentorshipRemoveSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureMentorshipEnded(__state);
        }
    }

    /// <summary>
    /// Captures mentorship weekly tick events.
    /// </summary>
    [HarmonyPatch(typeof(Girls_Mentors), CoreConstants.HarmonyGirlsMentorsOnNewWeekMethodName)]
    internal static class Girls_Mentors_OnNewWeek_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(out MentorshipWeeklySnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateMentorshipWeeklySnapshot();
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(MentorshipWeeklySnapshot __state)
        {
            IMDataCoreController.Instance.CaptureMentorshipWeeklyTick(__state);
        }
    }

    /// <summary>
    /// Captures rival trend update context.
    /// </summary>
    [HarmonyPatch(typeof(Rivals), CoreConstants.HarmonyRivalsUpdateTrendsMethodName)]
    internal static class Rivals_UpdateTrends_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(out RivalMarketSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateRivalMarketSnapshot();
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(RivalMarketSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureRivalTrendsUpdated(__state);
        }
    }

    /// <summary>
    /// Captures rival monthly recalculation context.
    /// </summary>
    [HarmonyPatch(typeof(Rivals), CoreConstants.HarmonyRivalsOnNewMonthMethodName)]
    internal static class Rivals_OnNewMonth_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(out RivalMarketSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateRivalMarketSnapshot();
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(bool ShowPopup, RivalMarketSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureRivalMonthlyRecalculated(ShowPopup, __state);
        }
    }

    /// <summary>
    /// Captures summer games finalization spend outcomes.
    /// </summary>
    [HarmonyPatch(typeof(Summer_Games._data), CoreConstants.HarmonySummerGamesProceedMethodName)]
    internal static class Summer_Games_data_OnProceed_IMDataCoreCapture_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Summer_Games._data __instance, out SummerGamesFinalizeSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateSummerGamesFinalizeSnapshot(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Summer_Games._data __instance, SummerGamesFinalizeSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureSummerGamesFinalized(__instance, __state);
        }
    }
}
