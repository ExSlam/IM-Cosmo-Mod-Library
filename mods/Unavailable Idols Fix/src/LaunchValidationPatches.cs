using System.Collections.Generic;
using HarmonyLib;

namespace UnavailableIdolsFix
{
    internal sealed class BlockedProjectLaunchException : System.Exception
    {
    }

    [HarmonyPatch(typeof(singles._single), nameof(singles._single.CanLaunch))]
    internal static class SingleCanLaunchAvailabilityPatch
    {
        private static void Postfix(singles._single __instance, ref bool __result)
        {
            if (AvailabilityRules.GetUnavailable(AvailabilityRules.GetSingleCast(__instance)).Count > 0)
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(Shows._show), nameof(Shows._show.CanLaunch))]
    internal static class ShowCanLaunchAvailabilityPatch
    {
        private static void Postfix(Shows._show __instance, ref bool __result)
        {
            if (AvailabilityRules.GetUnavailable(AvailabilityRules.GetShowCast(__instance)).Count > 0)
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(SEvent_Concerts._concert), nameof(SEvent_Concerts._concert.CanLaunch))]
    internal static class ConcertCanLaunchAvailabilityPatch
    {
        private static void Postfix(SEvent_Concerts._concert __instance, ref bool __result)
        {
            List<data_girls.girls> unavailable = AvailabilityRules.GetUnavailable(AvailabilityRules.GetConcertCast(__instance));
            if (unavailable.Count > 0 || AvailabilityRules.ConcertHasIncompleteCast(__instance))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(singles), nameof(singles.ReleaseSingle))]
    [HarmonyPriority(Priority.First)]
    internal static class SingleReleaseEntryValidationPatch
    {
        private static void Prefix(singles._single single)
        {
            if (single == null)
            {
                throw new BlockedProjectLaunchException();
            }

            bool canLaunch = single.CanLaunch();
            if (!canLaunch && AvailabilityRules.GetUnavailable(AvailabilityRules.GetSingleCast(single)).Count > 0)
            {
                LocalizedNotifications.NotifySingleBlocked(single);
            }

            if (!canLaunch)
            {
                // Throwing here lets Harmony bypass every release postfix. A boolean prefix
                // would still run IM Data Core's postfix and record a release that never occurred.
                throw new BlockedProjectLaunchException();
            }
        }

        private static System.Exception Finalizer(System.Exception __exception)
        {
            return __exception is BlockedProjectLaunchException ? null : __exception;
        }
    }

    [HarmonyPatch(typeof(Shows), nameof(Shows.ReleaseShow))]
    [HarmonyPriority(Priority.First)]
    internal static class ShowReleaseEntryValidationPatch
    {
        private static void Prefix(Shows._show __0)
        {
            // The game's argument is literally named "__show". Harmony reserves names
            // beginning with two underscores for injected patch parameters, so bind it by
            // position instead of copying the original name.
            Shows._show show = __0;
            if (show == null)
            {
                throw new BlockedProjectLaunchException();
            }

            bool canLaunch = show.CanLaunch();
            if (!canLaunch && AvailabilityRules.GetUnavailable(AvailabilityRules.GetShowCast(show)).Count > 0)
            {
                LocalizedNotifications.NotifyShowBlocked(show);
            }

            if (!canLaunch)
            {
                throw new BlockedProjectLaunchException();
            }
        }

        private static System.Exception Finalizer(System.Exception __exception)
        {
            return __exception is BlockedProjectLaunchException ? null : __exception;
        }
    }

    [HarmonyPatch(typeof(SEvent_Concerts), nameof(SEvent_Concerts.StartConcert))]
    [HarmonyPriority(Priority.First)]
    internal static class ConcertStartEntryValidationPatch
    {
        private static void Prefix(SEvent_Concerts __instance)
        {
            SEvent_Concerts._concert concert = __instance == null ? null : __instance.Concert;
            if (concert == null)
            {
                throw new BlockedProjectLaunchException();
            }

            bool canLaunch = concert.CanLaunch(false);
            if (!canLaunch && AvailabilityRules.GetUnavailable(AvailabilityRules.GetConcertCast(concert)).Count > 0)
            {
                LocalizedNotifications.NotifyConcertBlocked(concert);
            }

            if (!canLaunch && concert.SSK != null)
            {
                // CheckQueue removes the due entry after calling StartConcert even when a
                // Harmony prefix blocks the launch. Leave one future retry so the linked
                // election concert is delayed instead of silently discarded.
                bool hasFutureRetry = false;
                if (SpecialEvents_Manager.SSKQueue != null)
                {
                    foreach (System.DateTime queuedDate in SpecialEvents_Manager.SSKQueue)
                    {
                        if (queuedDate > staticVars.dateTime)
                        {
                            hasFutureRetry = true;
                            break;
                        }
                    }

                    if (!hasFutureRetry)
                    {
                        SpecialEvents_Manager.SSKQueue.Add(staticVars.dateTime.AddDays(1.0));
                    }
                }
            }

            if (!canLaunch)
            {
                throw new BlockedProjectLaunchException();
            }
        }

        private static System.Exception Finalizer(System.Exception __exception)
        {
            return __exception is BlockedProjectLaunchException ? null : __exception;
        }
    }
}
