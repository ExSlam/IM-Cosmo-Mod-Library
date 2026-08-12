using System;
using HarmonyLib;

namespace IMDataCore
{
    /// <summary>
    /// Final observers run after known Cosmo cast-mutating fixes. A small nested
    /// settlement scope prevents an inner method such as RemoveGirl from emitting
    /// history while an outer graduation/unavailability transaction is still
    /// mutating the same show state.
    ///
    /// The observer never changes gameplay and never suppresses an original
    /// exception.
    /// </summary>
    internal static class PostModShowSettlementScope
    {
        internal static void Begin()
        {
            try
            {
                IMDataCoreController.Instance.BeginPostModShowSettlement();
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "Post-mod show settlement scope begin failed without " +
                    "blocking gameplay: " + exception.Message);
            }
        }

        internal static bool End()
        {
            try
            {
                return IMDataCoreController.Instance.EndPostModShowSettlement();
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "Post-mod show settlement scope end failed without " +
                    "blocking gameplay: " + exception.Message);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Shows._show), nameof(Shows._show.NewEpisode))]
    internal static class Shows_NewEpisode_IMDataCorePostModObservation_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix()
        {
            PostModShowSettlementScope.Begin();
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter(
            "com.cosmo.unavailableidolsfix",
            "com.cosmo.showcastassignmentfix")]
        private static Exception Finalizer(
            Shows._show __instance,
            Exception __exception)
        {
            bool outermost = PostModShowSettlementScope.End();
            if (__exception == null && outermost)
            {
                try
                {
                    IMDataCoreController.Instance.ObservePostModShowEpisode(
                        __instance);
                }
                catch (Exception exception)
                {
                    CoreLog.Warn(
                        "Post-mod show episode observation failed without " +
                        "blocking gameplay: " + exception.Message);
                }
            }
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Shows._show), nameof(Shows._show.RemoveGirl))]
    internal static class Shows_RemoveGirl_IMDataCorePostModObservation_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix()
        {
            PostModShowSettlementScope.Begin();
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter(
            "com.cosmo.unavailableidolsfix",
            "com.cosmo.showcastassignmentfix")]
        private static Exception Finalizer(
            Shows._show __instance,
            Exception __exception)
        {
            bool outermost = PostModShowSettlementScope.End();
            if (__exception == null && outermost)
            {
                Observe(
                    __instance,
                    CorePayloadCompaction.CanonicalShowCastSourcePrefix +
                        "remove_girl");
            }
            return __exception;
        }

        private static void Observe(Shows._show show, string source)
        {
            try
            {
                IMDataCoreController.Instance.ObservePostModShowMutation(
                    show,
                    source);
            }
            catch (Exception exception)
            {
                CoreLog.Warn(
                    "Post-mod show cast observation failed without blocking " +
                    "gameplay: " + exception.Message);
            }
        }
    }

    [HarmonyPatch(typeof(Show_Popup), "SaveShow")]
    internal static class ShowPopup_SaveShow_IMDataCorePostModObservation_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix()
        {
            PostModShowSettlementScope.Begin();
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter(
            "com.cosmo.unavailableidolsfix",
            "com.cosmo.showcastassignmentfix")]
        private static Exception Finalizer(
            Show_Popup __instance,
            Exception __exception)
        {
            bool outermost = PostModShowSettlementScope.End();
            if (__exception == null && outermost && __instance != null)
            {
                try
                {
                    IMDataCoreController.Instance.ObservePostModShowMutation(
                        __instance._Show,
                        CorePayloadCompaction.CanonicalShowCastSourcePrefix +
                            "editor");
                }
                catch (Exception exception)
                {
                    CoreLog.Warn(
                        "Post-mod show editor observation failed without blocking " +
                        "gameplay: " + exception.Message);
                }
            }
            return __exception;
        }
    }

    /// <summary>
    /// Unavailable Idols Fix may settle launched permanent-show casts as a result
    /// of medical/hiatus/graduation lifecycle mutations. Observe after the outer
    /// lifecycle operation has completed instead of predicting UIF's behavior or
    /// recording nested intermediate RemoveGirl states.
    /// </summary>
    internal static class PostModShowLifecycleObservation
    {
        internal static void Begin()
        {
            PostModShowSettlementScope.Begin();
        }

        internal static Exception ObserveAfterLifecycle(Exception exception)
        {
            bool outermost = PostModShowSettlementScope.End();
            if (exception == null && outermost)
            {
                try
                {
                    IMDataCoreController.Instance.ReconcileAllPostModShows(
                        CorePayloadCompaction.CanonicalShowCastSourcePrefix +
                            "idol_lifecycle");
                }
                catch (Exception observerException)
                {
                    CoreLog.Warn(
                        "Post-mod idol lifecycle show reconciliation failed without " +
                        "blocking gameplay: " + observerException.Message);
                }
            }
            return exception;
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Set_Injured))]
    internal static class IdolSetInjured_IMDataCorePostModShows_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix()
        {
            PostModShowLifecycleObservation.Begin();
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter("com.cosmo.unavailableidolsfix")]
        private static Exception Finalizer(Exception __exception)
        {
            return PostModShowLifecycleObservation.ObserveAfterLifecycle(__exception);
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Set_Depressed))]
    internal static class IdolSetDepressed_IMDataCorePostModShows_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix()
        {
            PostModShowLifecycleObservation.Begin();
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter("com.cosmo.unavailableidolsfix")]
        private static Exception Finalizer(Exception __exception)
        {
            return PostModShowLifecycleObservation.ObserveAfterLifecycle(__exception);
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.SendOnHiatus))]
    internal static class IdolSendOnHiatus_IMDataCorePostModShows_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix()
        {
            PostModShowLifecycleObservation.Begin();
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter("com.cosmo.unavailableidolsfix")]
        private static Exception Finalizer(Exception __exception)
        {
            return PostModShowLifecycleObservation.ObserveAfterLifecycle(__exception);
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Heal))]
    internal static class IdolHeal_IMDataCorePostModShows_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix()
        {
            PostModShowLifecycleObservation.Begin();
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter("com.cosmo.unavailableidolsfix")]
        private static Exception Finalizer(Exception __exception)
        {
            return PostModShowLifecycleObservation.ObserveAfterLifecycle(__exception);
        }
    }

    [HarmonyPatch(
        typeof(data_girls.girls),
        nameof(data_girls.girls.FinishHiatus),
        new Type[] { typeof(bool) })]
    internal static class IdolFinishHiatus_IMDataCorePostModShows_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix()
        {
            PostModShowLifecycleObservation.Begin();
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter("com.cosmo.unavailableidolsfix")]
        private static Exception Finalizer(Exception __exception)
        {
            return PostModShowLifecycleObservation.ObserveAfterLifecycle(__exception);
        }
    }

    [HarmonyPatch(
        typeof(data_girls.girls),
        nameof(data_girls.girls.Graduate),
        new Type[] { typeof(bool), typeof(string) })]
    internal static class IdolGraduate_IMDataCorePostModShows_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix()
        {
            PostModShowLifecycleObservation.Begin();
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter("com.cosmo.unavailableidolsfix")]
        private static Exception Finalizer(Exception __exception)
        {
            return PostModShowLifecycleObservation.ObserveAfterLifecycle(__exception);
        }
    }
}
