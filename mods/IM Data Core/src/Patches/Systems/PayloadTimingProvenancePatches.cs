using System;
using System.Collections.Generic;
using HarmonyLib;

namespace IMDataCore
{
    internal sealed class IdolDepartureProvenanceState
    {
        internal string Cause = string.Empty;
        internal string Source = string.Empty;
    }

    /// <summary>
    /// Thread-scoped, history-only attribution for the shared girls.Graduate terminal.
    /// Narrow semantic callers push a controlled cause while Graduate executes.
    /// </summary>
    internal static class IdolDepartureProvenanceContext
    {
        [ThreadStatic]
        private static IdolDepartureProvenanceState current;

        internal static string CurrentCause
        {
            get { return current != null ? current.Cause : string.Empty; }
        }

        internal static string CurrentSource
        {
            get { return current != null ? current.Source : string.Empty; }
        }

        internal static IdolDepartureProvenanceState Push(string cause, string source)
        {
            IdolDepartureProvenanceState previous = current;
            current = new IdolDepartureProvenanceState
            {
                Cause = cause ?? CoreConstants.ProvenanceUnknown,
                Source = source ?? CoreConstants.ProvenanceUnknown
            };
            return previous;
        }

        internal static void Restore(IdolDepartureProvenanceState previous)
        {
            current = previous;
        }
    }

    internal sealed class IdolStatusProvenanceState
    {
        internal string Cause = string.Empty;
        internal string SourceKind = string.Empty;
    }

    /// <summary>
    /// Thread-scoped source attribution for the shared girls.SetStatus sink.
    /// This does not alter gameplay state; it only annotates transitions that already occur.
    /// </summary>
    internal static class IdolStatusProvenanceContext
    {
        [ThreadStatic]
        private static IdolStatusProvenanceState current;

        internal static string CurrentCause
        {
            get { return current != null ? current.Cause : string.Empty; }
        }

        internal static string CurrentSourceKind
        {
            get { return current != null ? current.SourceKind : string.Empty; }
        }

        internal static IdolStatusProvenanceState Push(string cause, string sourceKind)
        {
            IdolStatusProvenanceState previous = current;
            current = new IdolStatusProvenanceState
            {
                Cause = cause ?? CoreConstants.ProvenanceUnknown,
                SourceKind = sourceKind ?? CoreConstants.ProvenanceUnknown
            };
            return previous;
        }

        internal static void Restore(IdolStatusProvenanceState previous)
        {
            current = previous;
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Graduation_Date_Update))]
    internal static class data_girls_GraduationDateUpdate_IMDataCoreDepartureContext_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(out IdolDepartureProvenanceState __state)
        {
            __state = IdolDepartureProvenanceContext.Push(
                CoreConstants.IdolDepartureCauseScheduledGraduation,
                "data_girls.girls.Graduation_Date_Update");
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, IdolDepartureProvenanceState __state)
        {
            IdolDepartureProvenanceContext.Restore(__state);
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Date_Popup), CoreConstants.HarmonyDatePopupFireImmediatelyMethodName)]
    internal static class DatePopup_FireImmediately_IMDataCoreDepartureContext_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(out IdolDepartureProvenanceState __state)
        {
            __state = IdolDepartureProvenanceContext.Push(
                CoreConstants.IdolDepartureCausePlayerFired,
                "Date_Popup.OnClick_Fire_Immediately");
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, IdolDepartureProvenanceState __state)
        {
            IdolDepartureProvenanceContext.Restore(__state);
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Dating), nameof(Dating.AfterMarriage))]
    internal static class Dating_AfterMarriage_IMDataCoreDepartureContext_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(out IdolDepartureProvenanceState __state)
        {
            __state = IdolDepartureProvenanceContext.Push(
                CoreConstants.IdolDepartureCauseMarriage,
                "Dating.AfterMarriage");
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, IdolDepartureProvenanceState __state)
        {
            IdolDepartureProvenanceContext.Restore(__state);
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Bankruptcy), nameof(Bankruptcy.TriggerAuditionFailure))]
    internal static class Bankruptcy_TriggerAuditionFailure_IMDataCoreDepartureContext_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(out IdolDepartureProvenanceState __state)
        {
            __state = IdolDepartureProvenanceContext.Push(
                CoreConstants.IdolDepartureCauseBankruptcy,
                "Bankruptcy.TriggerAuditionFailure");
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, IdolDepartureProvenanceState __state)
        {
            IdolDepartureProvenanceContext.Restore(__state);
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Bankruptcy), nameof(Bankruptcy.Fire_All_Idols))]
    internal static class Bankruptcy_FireAllIdols_IMDataCoreDepartureContext_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(out IdolDepartureProvenanceState __state)
        {
            __state = IdolDepartureProvenanceContext.Push(
                CoreConstants.IdolDepartureCauseBankruptcy,
                "Bankruptcy.Fire_All_Idols");
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, IdolDepartureProvenanceState __state)
        {
            IdolDepartureProvenanceContext.Restore(__state);
            return __exception;
        }
    }

    [HarmonyPatch(typeof(vn_actions), nameof(vn_actions.Do),
        new Type[] { typeof(data_dialogues._action), typeof(Event_Manager._activeEvent) })]
    internal static class vn_actions_Do_IMDataCoreProvenanceContext_Patch
    {
        internal sealed class State
        {
            internal IdolDepartureProvenanceState Departure;
            internal IdolStatusProvenanceState Status;
        }

        [HarmonyPrefix]
        private static void Prefix(out State __state)
        {
            __state = new State
            {
                Departure = IdolDepartureProvenanceContext.Push(
                    CoreConstants.IdolDepartureCauseStory,
                    "vn_actions.Do"),
                Status = IdolStatusProvenanceContext.Push(
                    CoreConstants.StatusCauseStory,
                    CoreConstants.StatusSourceKindStory)
            };
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, State __state)
        {
            if (__state != null)
            {
                IdolStatusProvenanceContext.Restore(__state.Status);
                IdolDepartureProvenanceContext.Restore(__state.Departure);
            }
            return __exception;
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Set_Injured))]
    internal static class data_girls_SetInjured_IMDataCoreStatusContext_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(out IdolStatusProvenanceState __state)
        {
            __state = IdolStatusProvenanceContext.Push(CoreConstants.StatusCauseInjury, CoreConstants.StatusSourceKindMedical);
        }
        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, IdolStatusProvenanceState __state)
        {
            IdolStatusProvenanceContext.Restore(__state); return __exception;
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Set_Depressed))]
    internal static class data_girls_SetDepressed_IMDataCoreStatusContext_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(out IdolStatusProvenanceState __state)
        {
            __state = IdolStatusProvenanceContext.Push(CoreConstants.StatusCauseDepression, CoreConstants.StatusSourceKindMedical);
        }
        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, IdolStatusProvenanceState __state)
        {
            IdolStatusProvenanceContext.Restore(__state); return __exception;
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.SendOnHiatus))]
    internal static class data_girls_SendOnHiatus_IMDataCoreStatusContext_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(out IdolStatusProvenanceState __state)
        {
            __state = IdolStatusProvenanceContext.Push(CoreConstants.StatusCauseHiatus, CoreConstants.StatusSourceKindMedical);
        }
        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, IdolStatusProvenanceState __state)
        {
            IdolStatusProvenanceContext.Restore(__state); return __exception;
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.FinishHiatus))]
    internal static class data_girls_FinishHiatus_IMDataCoreStatusContext_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(out IdolStatusProvenanceState __state)
        {
            __state = IdolStatusProvenanceContext.Push(CoreConstants.StatusCauseHiatusReturn, CoreConstants.StatusSourceKindMedical);
        }
        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, IdolStatusProvenanceState __state)
        {
            IdolStatusProvenanceContext.Restore(__state); return __exception;
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Graduation_Announce_Confirm))]
    internal static class data_girls_GraduationAnnounceConfirm_IMDataCoreStatusContext_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(out IdolStatusProvenanceState __state)
        {
            __state = IdolStatusProvenanceContext.Push(CoreConstants.StatusCauseGraduationAnnounced, CoreConstants.StatusSourceKindGraduation);
        }
        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, IdolStatusProvenanceState __state)
        {
            IdolStatusProvenanceContext.Restore(__state); return __exception;
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Graduate))]
    internal static class data_girls_Graduate_IMDataCoreStatusContext_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(out IdolStatusProvenanceState __state)
        {
            string cause = IdolDepartureProvenanceContext.CurrentCause;
            if (string.IsNullOrEmpty(cause))
            {
                cause = CoreConstants.StatusCauseGraduated;
            }
            __state = IdolStatusProvenanceContext.Push(cause, CoreConstants.StatusSourceKindGraduation);
        }
        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, IdolStatusProvenanceState __state)
        {
            IdolStatusProvenanceContext.Restore(__state); return __exception;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.assign),
        new Type[] { typeof(data_girls.girls), typeof(Nullable<data_girls._paramType>) })]
    internal static class agency_room_assignGirl_IMDataCoreStatusContext_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(agency._room __instance, out IdolStatusProvenanceState __state)
        {
            bool cafe = __instance != null && __instance.type == agency._type.cafeAndShop;
            __state = IdolStatusProvenanceContext.Push(
                cafe ? CoreConstants.StatusCauseCafeWork : CoreConstants.StatusCauseRoomPractice,
                cafe ? CoreConstants.StatusSourceKindCafe : CoreConstants.StatusSourceKindRoom);
        }
        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, IdolStatusProvenanceState __state)
        {
            IdolStatusProvenanceContext.Restore(__state); return __exception;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.FinishPractice))]
    internal static class agency_room_FinishPractice_IMDataCoreStatusContext_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(out IdolStatusProvenanceState __state)
        {
            __state = IdolStatusProvenanceContext.Push(CoreConstants.StatusCauseRoomPracticeComplete, CoreConstants.StatusSourceKindRoom);
        }
        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, IdolStatusProvenanceState __state)
        {
            IdolStatusProvenanceContext.Restore(__state); return __exception;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.Cafe_Remove))]
    internal static class agency_room_CafeRemove_IMDataCoreStatusContext_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(out IdolStatusProvenanceState __state)
        {
            __state = IdolStatusProvenanceContext.Push(CoreConstants.StatusCauseCafeWorkComplete, CoreConstants.StatusSourceKindCafe);
        }
        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, IdolStatusProvenanceState __state)
        {
            IdolStatusProvenanceContext.Restore(__state); return __exception;
        }
    }

    [HarmonyPatch(typeof(Cafes), nameof(Cafes.RemoveGirl))]
    internal static class Cafes_RemoveGirl_IMDataCoreStatusContext_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(out IdolStatusProvenanceState __state)
        {
            __state = IdolStatusProvenanceContext.Push(CoreConstants.StatusCauseCafeWorkComplete, CoreConstants.StatusSourceKindCafe);
        }
        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, IdolStatusProvenanceState __state)
        {
            IdolStatusProvenanceContext.Restore(__state); return __exception;
        }
    }

    [HarmonyPatch(typeof(Cafes._cafe), nameof(Cafes._cafe.StopWorking))]
    internal static class Cafes_cafe_StopWorking_IMDataCoreStatusContext_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(out IdolStatusProvenanceState __state)
        {
            __state = IdolStatusProvenanceContext.Push(CoreConstants.StatusCauseCafeWorkComplete, CoreConstants.StatusSourceKindCafe);
        }
        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, IdolStatusProvenanceState __state)
        {
            IdolStatusProvenanceContext.Restore(__state); return __exception;
        }
    }

    [HarmonyPatch(typeof(Cafes), CoreConstants.HarmonyCafesRenderCafeMethodName,
        new Type[] { typeof(agency._room), typeof(Cafes._cafe) })]
    internal static class Cafes_RenderCafe_IMDataCoreStatusContext_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(out IdolStatusProvenanceState __state)
        {
            __state = IdolStatusProvenanceContext.Push(CoreConstants.StatusCauseCafeWork, CoreConstants.StatusSourceKindCafe);
        }
        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, IdolStatusProvenanceState __state)
        {
            IdolStatusProvenanceContext.Restore(__state); return __exception;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.assign),
        new Type[] { typeof(Scenes.type), typeof(List<data_girls.girls>) })]
    internal static class agency_room_assignScene_IMDataCoreStatusContext_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(out IdolStatusProvenanceState __state)
        {
            __state = IdolStatusProvenanceContext.Push(CoreConstants.StatusCauseSceneAssignment, CoreConstants.StatusSourceKindScene);
        }
        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, IdolStatusProvenanceState __state)
        {
            IdolStatusProvenanceContext.Restore(__state); return __exception;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.FinishScene))]
    internal static class agency_room_FinishScene_IMDataCoreStatusContext_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(out IdolStatusProvenanceState __state)
        {
            __state = IdolStatusProvenanceContext.Push(CoreConstants.StatusCauseSceneCleanup, CoreConstants.StatusSourceKindScene);
        }
        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, IdolStatusProvenanceState __state)
        {
            IdolStatusProvenanceContext.Restore(__state); return __exception;
        }
    }

    [HarmonyPatch(typeof(agency._room), CoreConstants.HarmonyAgencyRoomDoTreatmentMethodName)]
    internal static class agency_room_DoTreatment_IMDataCoreStatusContext_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(out IdolStatusProvenanceState __state)
        {
            __state = IdolStatusProvenanceContext.Push(CoreConstants.StatusCauseMedicalTreatment, CoreConstants.StatusSourceKindMedical);
        }
        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, IdolStatusProvenanceState __state)
        {
            IdolStatusProvenanceContext.Restore(__state); return __exception;
        }
    }
}
