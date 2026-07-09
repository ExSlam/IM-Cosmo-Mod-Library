using System;
using System.Collections.Generic;
using System.Globalization;
using ModLocalizationSystem;
using UnityEngine;

namespace UnavailableIdolsFix
{
    internal static class AvailabilityRules
    {
        internal static bool IsTemporarilyUnavailable(data_girls.girls girl)
        {
            if (girl == null)
            {
                return false;
            }

            return girl.status == data_girls._status.injured
                || girl.status == data_girls._status.depressed
                || girl.status == data_girls._status.hiatus;
        }

        internal static List<data_girls.girls> GetUnavailable(IEnumerable<data_girls.girls> girls)
        {
            List<data_girls.girls> unavailable = new List<data_girls.girls>();
            if (girls == null)
            {
                return unavailable;
            }

            foreach (data_girls.girls girl in girls)
            {
                if (IsTemporarilyUnavailable(girl) && !unavailable.Contains(girl))
                {
                    unavailable.Add(girl);
                }
            }

            return unavailable;
        }

        internal static IEnumerable<data_girls.girls> GetSingleCast(singles._single single)
        {
            return single == null ? null : single.girls;
        }

        internal static IEnumerable<data_girls.girls> GetShowCast(Shows._show show)
        {
            if (show == null || show.castType != Shows._show._castType.permanentCast)
            {
                return null;
            }

            return show.girls;
        }

        internal static List<data_girls.girls> GetConcertCast(SEvent_Concerts._concert concert)
        {
            List<data_girls.girls> cast = new List<data_girls.girls>();
            if (concert == null || concert.SetListItems == null)
            {
                return cast;
            }

            foreach (SEvent_Concerts._concert.ISetlistItem item in concert.SetListItems)
            {
                if (item == null)
                {
                    continue;
                }

                List<data_girls.girls> itemGirls = item.GetGirls(true);
                if (itemGirls == null)
                {
                    continue;
                }

                foreach (data_girls.girls girl in itemGirls)
                {
                    if (girl != null && !cast.Contains(girl))
                    {
                        cast.Add(girl);
                    }
                }
            }

            return cast;
        }

        internal static bool SingleContains(singles._single single, data_girls.girls girl)
        {
            return single != null && single.girls != null && girl != null && single.girls.Contains(girl);
        }

        internal static bool ShowContains(Shows._show show, data_girls.girls girl)
        {
            if (show == null || show.girls == null || girl == null || show.castType != Shows._show._castType.permanentCast)
            {
                return false;
            }

            for (int i = 0; i < show.girls.Length; i++)
            {
                if (show.girls[i] == girl)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool ConcertContains(SEvent_Concerts._concert concert, data_girls.girls girl)
        {
            return concert != null && girl != null && GetConcertCast(concert).Contains(girl);
        }

        internal static bool ConcertHasIncompleteCast(SEvent_Concerts._concert concert)
        {
            if (concert == null || concert.SetListItems == null || concert.SetListItems.Count == 0)
            {
                return true;
            }

            foreach (SEvent_Concerts._concert.ISetlistItem item in concert.SetListItems)
            {
                if (item == null || item.GetGirls(true) == null || item.GetGirls(true).Count == 0)
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal static class LocalizedNotifications
    {
        private const string UnknownProjectFallback = "Untitled project";

        internal static void NotifySingleBlocked(singles._single single)
        {
            NotifyBlocked(
                "notification.blocked.single.singular",
                "notification.blocked.single.plural",
                "Single \"{0}\" is unavailable for release until {1} is removed from the cast or recovers.",
                "Single \"{0}\" is unavailable for release until {1} are removed from the cast or recover.",
                GetTitle(single == null ? null : single.title),
                AvailabilityRules.GetUnavailable(AvailabilityRules.GetSingleCast(single)));
        }

        internal static void NotifyShowBlocked(Shows._show show)
        {
            NotifyBlocked(
                "notification.blocked.show.singular",
                "notification.blocked.show.plural",
                "Show \"{0}\" is unavailable for release until {1} is removed from the cast or recovers.",
                "Show \"{0}\" is unavailable for release until {1} are removed from the cast or recover.",
                GetTitle(show == null ? null : show.title),
                AvailabilityRules.GetUnavailable(AvailabilityRules.GetShowCast(show)));
        }

        internal static void NotifyConcertBlocked(SEvent_Concerts._concert concert)
        {
            NotifyBlocked(
                "notification.blocked.concert.singular",
                "notification.blocked.concert.plural",
                "Concert \"{0}\" cannot launch until {1} is removed from the cast or recovers.",
                "Concert \"{0}\" cannot launch until {1} are removed from the cast or recover.",
                GetTitle(concert == null ? null : concert.GetTitle()),
                AvailabilityRules.GetUnavailable(AvailabilityRules.GetConcertCast(concert)));
        }

        internal static void NotifyProjectsBlockedBy(data_girls.girls changedGirl)
        {
            if (!AvailabilityRules.IsTemporarilyUnavailable(changedGirl))
            {
                return;
            }

            if (singles.Singles != null)
            {
                foreach (singles._single single in singles.Singles)
                {
                    if (single != null && single.status != singles._single._status.released && AvailabilityRules.SingleContains(single, changedGirl))
                    {
                        NotifySingleBlocked(single);
                    }
                }
            }

            if (Shows.shows != null)
            {
                foreach (Shows._show show in Shows.shows)
                {
                    if (show != null && show.status != Shows._show._status.canceled && show.episodeCount == 0 && AvailabilityRules.ShowContains(show, changedGirl))
                    {
                        NotifyShowBlocked(show);
                    }
                }
            }

            if (SEvent_Concerts.Concerts != null)
            {
                foreach (SEvent_Concerts._concert concert in SEvent_Concerts.Concerts)
                {
                    if (concert != null && concert.Status != SEvent_Tour.tour._status.finished && AvailabilityRules.ConcertContains(concert, changedGirl))
                    {
                        NotifyConcertBlocked(concert);
                    }
                }
            }
        }

        internal static void NotifySingleRemoved(data_girls.girls girl, singles._single single)
        {
            NotifyFormatted("notification.removed.single", "{0} was removed from the unreleased single \"{1}\".", GirlName(girl), GetTitle(single == null ? null : single.title));
        }

        internal static void NotifyShowRemoved(data_girls.girls girl, Shows._show show, bool running)
        {
            NotifyFormatted(
                running ? "notification.removed.show.running" : "notification.removed.show.unreleased",
                running ? "{0} was removed from the running show \"{1}\"." : "{0} was removed from the unreleased show \"{1}\".",
                GirlName(girl),
                GetTitle(show == null ? null : show.title));
        }

        internal static void NotifyConcertRemoved(data_girls.girls girl, SEvent_Concerts._concert concert)
        {
            NotifyFormatted("notification.removed.concert", "{0} was removed from the in-production concert \"{1}\".", GirlName(girl), GetTitle(concert == null ? null : concert.GetTitle()));
        }

        internal static void NotifyMentorshipRemoved(data_girls.girls girl, data_girls.girls other)
        {
            NotifyFormatted("notification.removed.mentorship", "{0} was removed from the mentorship with {1}.", GirlName(girl), GirlName(other));
        }

        internal static void NotifyPushRemoved(data_girls.girls girl)
        {
            NotifyFormatted("notification.removed.push", "{0} was removed from a promotional push slot.", GirlName(girl));
        }

        private static void NotifyBlocked(string singularKey, string pluralKey, string singularFallback, string pluralFallback, string projectTitle, List<data_girls.girls> unavailable)
        {
            if (unavailable == null || unavailable.Count == 0)
            {
                return;
            }

            List<string> names = new List<string>();
            foreach (data_girls.girls girl in unavailable)
            {
                names.Add(GirlName(girl));
            }

            bool plural = names.Count > 1;
            string template = ModLocalization.Get(plural ? pluralKey : singularKey, plural ? pluralFallback : singularFallback);
            AddNotification(string.Format(CultureInfo.CurrentCulture, template, projectTitle, string.Join(", ", names.ToArray())), mainScript.red32);
        }

        private static void NotifyFormatted(string key, string fallback, params object[] args)
        {
            string template = ModLocalization.Get(key, fallback);
            AddNotification(string.Format(CultureInfo.CurrentCulture, template, args), mainScript.red32);
        }

        private static string GirlName(data_girls.girls girl)
        {
            if (girl == null)
            {
                return ModLocalization.Get("label.unknown_idol", "Unknown idol");
            }

            try
            {
                return girl.GetName(true);
            }
            catch
            {
                return ModLocalization.Get("label.unknown_idol", "Unknown idol");
            }
        }

        private static string GetTitle(string title)
        {
            return string.IsNullOrEmpty(title) ? ModLocalization.Get("label.untitled_project", UnknownProjectFallback) : title;
        }

        private static void AddNotification(string message, Color32 color)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            try
            {
                NotificationManager.AddNotification(message, color, NotificationManager._notification._type.idol_status_change);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Unavailable Idols Fix] Unable to show notification: " + exception.Message);
            }
        }
    }
}
