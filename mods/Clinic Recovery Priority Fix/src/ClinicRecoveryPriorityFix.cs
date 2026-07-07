using System;
using System.Collections.Generic;
using HarmonyLib;

namespace ClinicRecoveryPriorityFix
{
    /// <summary>
    /// Vanilla ticks rooms in floor/build order. Each auto-practice room immediately claims an
    /// eligible idle idol, so a studio or salon encountered before a clinic can take a low-stamina
    /// idol before the clinic sorts its candidates. Track one agency tick and run every clinic
    /// before the first competing room without replacing the rest of agency.onTimeTick.
    /// </summary>
    internal static class ClinicPriorityScheduler
    {
        [ThreadStatic]
        private static agency activeAgency;

        [ThreadStatic]
        private static HashSet<agency._room> processedClinics;

        [ThreadStatic]
        private static agency._room forcedClinic;

        internal static void BeginAgencyTick(agency agencyInstance)
        {
            activeAgency = agencyInstance;
            processedClinics = new HashSet<agency._room>();
            forcedClinic = null;
        }

        internal static void EndAgencyTick()
        {
            forcedClinic = null;
            processedClinics = null;
            activeAgency = null;
        }

        internal static bool BeforeRoomTick(agency._room room)
        {
            if (activeAgency == null || processedClinics == null || room == null)
            {
                return true;
            }

            if (room.type == agency._type.doctorsOffice)
            {
                if (room == forcedClinic)
                {
                    processedClinics.Add(room);
                    return true;
                }

                // The outer vanilla loop will still encounter a clinic that was ticked early.
                // Skip that second call so progress and completion are calculated only once.
                if (processedClinics.Contains(room))
                {
                    return false;
                }

                processedClinics.Add(room);
                return true;
            }

            ProcessRemainingClinics();
            return true;
        }

        internal static void AfterRoomTick(agency._room room)
        {
            if (activeAgency == null || processedClinics == null || room == null ||
                room.type != agency._type.doctorsOffice || !processedClinics.Contains(room) ||
                room.status != agency._room._status.normal)
            {
                return;
            }

            // A completed recovery leaves the clinic normal until its next vanilla tick. Refill
            // it now so later auto-practice rooms in this same tick cannot take the next patient.
            // AutoActivitiesCheck still respects all vanilla recovery toggles and eligibility.
            room.AutoActivitiesCheck();
        }

        private static void ProcessRemainingClinics()
        {
            List<agency._room> clinics = activeAgency.allRooms(agency._type.doctorsOffice);
            for (int i = 0; i < clinics.Count; i++)
            {
                agency._room clinic = clinics[i];
                if (clinic == null || processedClinics.Contains(clinic))
                {
                    continue;
                }

                forcedClinic = clinic;
                try
                {
                    clinic.OnTimeTick();
                }
                finally
                {
                    forcedClinic = null;
                }
            }
        }
    }

    [HarmonyPatch(typeof(agency._room), "CanAutoTrain", new Type[] { typeof(data_girls.girls) })]
    internal static class ClinicRecoveryEligibilityPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(agency._room __instance, data_girls.girls _girl, ref bool __result)
        {
            if (__instance == null || __instance.type != agency._type.doctorsOffice)
            {
                return true;
            }

            // Vanilla reuses training-specialization eligibility for clinic recovery. That
            // excludes every idol whose preference is don't train, vocal, dance, or styling,
            // even though medical recovery is not training. The outer DoAutoPractice filter
            // still requires normal status, an available staffed clinic, a recoverable stat,
            // and stamina below 80. Preserve sickness and the temporary manual-cancel ban here.
            __result = _girl != null &&
                       !_girl.IsSick() &&
                       !agency.GirlsBannedFromAutoTasks.Contains(_girl);
            return false;
        }
    }

    [HarmonyPatch(typeof(agency), "onTimeTick", new Type[0])]
    internal static class AgencyTimeTickPriorityScopePatch
    {
        [HarmonyPrefix]
        private static void Prefix(agency __instance)
        {
            ClinicPriorityScheduler.BeginAgencyTick(__instance);
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception)
        {
            ClinicPriorityScheduler.EndAgencyTick();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.OnTimeTick), new Type[0])]
    internal static class RoomTimeTickClinicPriorityPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(agency._room __instance)
        {
            return ClinicPriorityScheduler.BeforeRoomTick(__instance);
        }

        [HarmonyPostfix]
        private static void Postfix(agency._room __instance)
        {
            ClinicPriorityScheduler.AfterRoomTick(__instance);
        }
    }
}
