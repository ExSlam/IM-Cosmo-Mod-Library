using System;
using System.Collections.Generic;
using HarmonyLib;

namespace IMDataCore
{
    /// <summary>
    /// Room task attribution patches.  Prefixes run first so Assistant Manager's replacement
    /// production prefixes are observed as well as vanilla room implementations.
    /// </summary>
    [HarmonyPatch(typeof(agency._room), nameof(agency._room.assign), new[] { typeof(singles._single) })]
    internal static class AgencyRoomAssignSingle_IMDataCoreRoomWork_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(agency._room __instance, singles._single _single)
        {
            if (__instance != null && __instance.single == _single && __instance.status == agency._room._status.singleProduction)
            {
                IMDataCoreController.Instance.TrackRoomWorkAssignment(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.assign), new[] { typeof(Shows._show) })]
    internal static class AgencyRoomAssignShow_IMDataCoreRoomWork_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(agency._room __instance, Shows._show _show)
        {
            if (__instance != null && __instance.show == _show && __instance.status == agency._room._status.showProduction)
            {
                IMDataCoreController.Instance.TrackRoomWorkAssignment(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.assign), new[] { typeof(SEvent_SSK._SSK) })]
    internal static class AgencyRoomAssignElection_IMDataCoreRoomWork_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(agency._room __instance, SEvent_SSK._SSK _SSK)
        {
            if (__instance != null && __instance.SSK == _SSK && __instance.status == agency._room._status.SSKProduction)
            {
                IMDataCoreController.Instance.TrackRoomWorkAssignment(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.assign), new[] { typeof(SEvent_Tour.tour) })]
    internal static class AgencyRoomAssignTour_IMDataCoreRoomWork_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(agency._room __instance, SEvent_Tour.tour _Tour)
        {
            if (__instance != null && __instance.tour == _Tour && __instance.status == agency._room._status.tourProduction)
            {
                IMDataCoreController.Instance.TrackRoomWorkAssignment(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.assign), new[] { typeof(SEvent_Concerts._concert) })]
    internal static class AgencyRoomAssignConcert_IMDataCoreRoomWork_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(agency._room __instance, SEvent_Concerts._concert _Tour)
        {
            if (__instance != null && __instance.concert == _Tour && __instance.status == agency._room._status.concertProduction)
            {
                IMDataCoreController.Instance.TrackRoomWorkAssignment(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.assign), new[] { typeof(data_girls.girls), typeof(Nullable<data_girls._paramType>) })]
    internal static class AgencyRoomAssignTraining_IMDataCoreRoomWork_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(agency._room __instance, data_girls.girls _girl)
        {
            if (__instance != null && __instance.girl == _girl && __instance.status == agency._room._status.girlTraining)
            {
                IMDataCoreController.Instance.TrackRoomWorkAssignment(__instance);
                IMDataCoreController.Instance.CaptureRoomWorkAssigned(__instance, CoreConstants.EventSourceRoomWorkAssignedPatch);
            }
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.assign_treatment))]
    internal static class AgencyRoomAssignTreatment_IMDataCoreRoomWork_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(agency._room __instance, data_girls.girls _girl)
        {
            if (__instance != null && __instance.girl == _girl &&
                (__instance.status == agency._room._status.injury_treatment ||
                 __instance.status == agency._room._status.depression_treatment))
            {
                IMDataCoreController.Instance.TrackRoomWorkAssignment(__instance);
                IMDataCoreController.Instance.CaptureRoomWorkAssigned(__instance, CoreConstants.EventSourceRoomTreatmentAssignedPatch);
            }
        }
    }

    [HarmonyPatch(typeof(agency._room), "DoSingleProduction")]
    internal static class AgencyRoomDoSingleProduction_IMDataCoreRoomWork_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(agency._room __instance, out RoomWorkCompletionSnapshot __state)
        {
            singles._single single = __instance != null ? __instance.single : null;
            __state = IMDataCoreController.Instance.CreateRoomWorkCompletionSnapshot(
                __instance,
                "single",
                single != null ? single.id.ToString() : string.Empty,
                single != null ? single.title : string.Empty,
                single != null ? __instance.singleParamType(single).ToString() : string.Empty,
                RoomWorkPatchUtility.GetIdolIds(single != null ? single.girls : null));
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(RoomWorkCompletionSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureRoomWorkCompleted(__state, CoreConstants.EventSourceRoomWorkCompletedPatch);
        }
    }

    [HarmonyPatch(typeof(agency._room), "DoShowProduction")]
    internal static class AgencyRoomDoShowProduction_IMDataCoreRoomWork_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(agency._room __instance, out RoomWorkCompletionSnapshot __state)
        {
            Shows._show show = __instance != null ? __instance.show : null;
            __state = IMDataCoreController.Instance.CreateRoomWorkCompletionSnapshot(
                __instance,
                "show",
                show != null ? show.id.ToString() : string.Empty,
                show != null ? show.title : string.Empty,
                show != null ? __instance.showParamType(show).ToString() : string.Empty,
                RoomWorkPatchUtility.GetIdolIds(show != null ? show.GetCast() : null));
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(RoomWorkCompletionSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureRoomWorkCompleted(__state, CoreConstants.EventSourceRoomWorkCompletedPatch);
        }
    }

    [HarmonyPatch(typeof(agency._room), "DoSSKProduction")]
    internal static class AgencyRoomDoElectionProduction_IMDataCoreRoomWork_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(agency._room __instance, out RoomWorkCompletionSnapshot __state)
        {
            SEvent_SSK._SSK election = __instance != null ? __instance.SSK : null;
            __state = IMDataCoreController.Instance.CreateRoomWorkCompletionSnapshot(
                __instance,
                "ssk",
                election != null ? election.ID.ToString() : string.Empty,
                election != null ? election.GetTitle() : string.Empty,
                election != null ? __instance.SSKParamType(election).ToString() : string.Empty,
                RoomWorkPatchUtility.GetActiveIdolIds());
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(RoomWorkCompletionSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureRoomWorkCompleted(__state, CoreConstants.EventSourceRoomWorkCompletedPatch);
        }
    }

    [HarmonyPatch(typeof(agency._room), "DoTourProduction")]
    internal static class AgencyRoomDoTourProduction_IMDataCoreRoomWork_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(agency._room __instance, out RoomWorkCompletionSnapshot __state)
        {
            SEvent_Tour.tour tour = __instance != null ? __instance.tour : null;
            __state = IMDataCoreController.Instance.CreateRoomWorkCompletionSnapshot(
                __instance,
                "tour",
                tour != null ? tour.ID.ToString() : string.Empty,
                tour != null ? string.Concat("Tour #", tour.ID.ToString()) : string.Empty,
                tour != null ? __instance.TourParamType(tour).ToString() : string.Empty,
                RoomWorkPatchUtility.GetActiveIdolIds());
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(RoomWorkCompletionSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureRoomWorkCompleted(__state, CoreConstants.EventSourceRoomWorkCompletedPatch);
        }
    }

    [HarmonyPatch(typeof(agency._room), "DoConcertProduction")]
    internal static class AgencyRoomDoConcertProduction_IMDataCoreRoomWork_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(agency._room __instance, out RoomWorkCompletionSnapshot __state)
        {
            SEvent_Concerts._concert concert = __instance != null ? __instance.concert : null;
            __state = IMDataCoreController.Instance.CreateRoomWorkCompletionSnapshot(
                __instance,
                "concert",
                concert != null ? concert.ID.ToString() : string.Empty,
                concert != null ? concert.Title : string.Empty,
                concert != null ? __instance.concertParamType(concert).ToString() : string.Empty,
                RoomWorkPatchUtility.GetIdolIds(concert != null ? concert.GetGirls(true) : null));
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(RoomWorkCompletionSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureRoomWorkCompleted(__state, CoreConstants.EventSourceRoomWorkCompletedPatch);
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.FinishPractice))]
    internal static class AgencyRoomFinishPractice_IMDataCoreRoomWork_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(agency._room __instance, bool force, out RoomPracticeCaptureSnapshot __state)
        {
            __state = new RoomPracticeCaptureSnapshot();
            if (force && !RoomCancelJobContext.IsActive)
            {
                __state.ForcedCancellation = IMDataCoreController.Instance.CreateRoomWorkCancellationSnapshot(__instance);
            }
            else
            {
                __state.Completion = IMDataCoreController.Instance.CreateTrainingCompletionSnapshot(__instance, false);
            }
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(RoomPracticeCaptureSnapshot __state)
        {
            if (__state == null)
            {
                return;
            }
            if (__state.ForcedCancellation != null &&
                IMDataCoreController.Instance.HasRoomWorkEnded(__state.ForcedCancellation))
            {
                IMDataCoreController.Instance.CaptureRoomWorkCancelled(
                    __state.ForcedCancellation,
                    CoreConstants.EventSourceRoomTrainingCancelledPatch);
                IMDataCoreController.Instance.ClearRoomWorkAssignment(__state.ForcedCancellation.Room);
                return;
            }
            IMDataCoreController.Instance.CaptureTrainingCompleted(__state.Completion);
        }
    }

    /// <summary>
    /// Makes the doctor who completed a treatment available to nested Heal/SendOnHiatus event
    /// hooks.  This covers both successful healing and a reduced-hiatus treatment result.
    /// </summary>
    [HarmonyPatch(typeof(agency._room), "DoTreatment")]
    internal static class AgencyRoomDoTreatment_IMDataCoreMedicalAttribution_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(agency._room __instance, out TreatmentCaptureSnapshot __state)
        {
            StaffAttributionSnapshot attribution = null;
            if (__instance != null &&
                __instance.type == agency._type.doctorsOffice &&
                __instance.girl != null &&
                __instance.staffer != null &&
                (__instance.status == agency._room._status.injury_treatment ||
                 __instance.status == agency._room._status.depression_treatment) &&
                __instance.finishTime < staticVars.dateTime)
            {
                attribution = IMDataCoreController.CreateStaffAttribution(__instance.staffer);
            }

            __state = new TreatmentCaptureSnapshot
            {
                Completion = IMDataCoreController.Instance.CreateTreatmentCompletionSnapshot(__instance),
                PreviousMedicalStaff = MedicalStaffAttributionContext.Push(attribution)
            };
            TreatmentCompletionContext.Enter();
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(TreatmentCaptureSnapshot __state)
        {
            try
            {
                if (__state != null)
                {
                    IMDataCoreController.Instance.CaptureRoomWorkCompleted(
                        __state.Completion,
                        CoreConstants.EventSourceRoomTreatmentCompletedPatch);
                }
            }
            finally
            {
                TreatmentCompletionContext.Exit();
                MedicalStaffAttributionContext.Restore(__state != null ? __state.PreviousMedicalStaff : null);
            }
        }

        [HarmonyPriority(Priority.Last)]
        private static Exception Finalizer(Exception __exception, TreatmentCaptureSnapshot __state)
        {
            TreatmentCompletionContext.Exit();
            MedicalStaffAttributionContext.Restore(__state != null ? __state.PreviousMedicalStaff : null);
            return __exception;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.CancelTreatment))]
    internal static class AgencyRoomCancelTreatment_IMDataCoreRoomWork_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(agency._room __instance, out RoomWorkCompletionSnapshot __state)
        {
            __state = TreatmentCompletionContext.IsActive || RoomCancelJobContext.IsActive
                ? null
                : IMDataCoreController.Instance.CreateRoomWorkCancellationSnapshot(__instance);
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(RoomWorkCompletionSnapshot __state)
        {
            if (__state != null && IMDataCoreController.Instance.HasRoomWorkEnded(__state))
            {
                IMDataCoreController.Instance.CaptureRoomWorkCancelled(
                    __state,
                    CoreConstants.EventSourceRoomTreatmentCancelledPatch);
                IMDataCoreController.Instance.ClearRoomWorkAssignment(__state.Room);
            }
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.CancelJob))]
    internal static class AgencyRoomCancelJob_IMDataCoreRoomWork_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(agency._room __instance, out RoomCancelJobCaptureSnapshot __state)
        {
            RoomCancelJobContext.Enter();
            __state = new RoomCancelJobCaptureSnapshot
            {
                RoomWork = IMDataCoreController.Instance.CreateRoomWorkCancellationSnapshot(__instance),
                DevelopingLoan = IMDataCoreController.Instance.CreateDevelopingLoanCancellationSnapshot(__instance)
            };
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(agency._room __instance, RoomCancelJobCaptureSnapshot __state)
        {
            try
            {
                if (__state != null)
                {
                    IMDataCoreController.Instance.CaptureRoomWorkCancelled(__state.RoomWork);
                    IMDataCoreController.Instance.CaptureDevelopingLoanCancelled(__state.DevelopingLoan);
                }
                IMDataCoreController.Instance.ClearRoomWorkAssignment(__instance);
            }
            finally
            {
                RoomCancelJobContext.Exit();
            }
        }

        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        private static Exception Finalizer(Exception __exception)
        {
            RoomCancelJobContext.Exit();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Auditions), nameof(Auditions.GenerateAudition), new[] { typeof(Auditions.data), typeof(bool) })]
    internal static class AuditionsGenerateAudition_IMDataCoreAttribution_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Auditions.data _data)
        {
            IMDataCoreController.Instance.TrackAuditionGeneration(_data);
        }
    }

    [HarmonyPatch(typeof(Audition_Data_Card), nameof(Audition_Data_Card.Hire))]
    internal static class AuditionDataCardHire_IMDataCoreAttribution_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Audition_Data_Card __instance)
        {
            if (__instance == null || __instance.Girl == null || __instance.Girl.girl == null)
            {
                return;
            }

            Popup_Audition popup = __instance.GetComponentInParent<Popup_Audition>();
            Auditions.data audition = popup != null
                ? Traverse.Create(popup).Field("Data").GetValue<Auditions.data>()
                : null;
            IMDataCoreController.Instance.TrackAuditionHireCandidate(audition, __instance.Girl.girl);
        }
    }

    internal static class RoomCancelJobContext
    {
        [ThreadStatic]
        private static int depth;

        internal static bool IsActive
        {
            get { return depth > 0; }
        }

        internal static void Enter()
        {
            depth++;
        }

        internal static void Exit()
        {
            if (depth > 0)
            {
                depth--;
            }
        }
    }

    internal static class TreatmentCompletionContext
    {
        [ThreadStatic]
        private static int depth;

        internal static bool IsActive
        {
            get { return depth > 0; }
        }

        internal static void Enter()
        {
            depth++;
        }

        internal static void Exit()
        {
            if (depth > 0)
            {
                depth--;
            }
        }
    }

    internal static class RoomWorkPatchUtility
    {
        internal static List<int> GetIdolIds(List<data_girls.girls> idols)
        {
            List<int> identifiers = new List<int>();
            if (idols == null)
            {
                return identifiers;
            }

            HashSet<int> uniqueIdentifiers = new HashSet<int>();
            for (int index = 0; index < idols.Count; index++)
            {
                data_girls.girls idol = idols[index];
                if (idol != null && idol.id >= CoreConstants.MinimumValidIdolIdentifier && uniqueIdentifiers.Add(idol.id))
                {
                    identifiers.Add(idol.id);
                }
            }

            return identifiers;
        }

        internal static List<int> GetActiveIdolIds()
        {
            return GetIdolIds(data_girls.GetActiveGirls(null));
        }
    }
}
