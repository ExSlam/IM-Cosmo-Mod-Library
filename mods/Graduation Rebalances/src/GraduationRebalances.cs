using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace GraduationRebalances
{
    internal static class HarmonyTargetMethodNames
    {
        internal const string GraduationSetDefaultDate = "Graduation_Set_Default_Date";
        internal const string GraduationDateUpdate = "Graduation_Date_Update";
        internal const string NotificationsShowOptions = "Show_Options";
        internal const string NotificationOptionCheckboxStart = "Start";
    }

    internal static class C
    {
        // Sentinel used by caches before their first valid key is computed.
        internal const int UnsetKey = -1;

        internal const int MinYears = 4;
        internal const int MinAge = 18;
        internal const int PeakBuffer = 3;
        internal const int AnnounceDays = 90;
        internal const int Ym = 12;
        internal const int DaysInShortMonth = 30;
        internal const int DaysPerWeek = 7;
        internal const int ShowEpisodeSpacingDays = DaysPerWeek;
        internal const int ShowFallbackDaysAgo = DaysPerWeek;
        internal const int MondayOffset = 6;
        internal const int ProposalRecentDays = 60;
        internal const int DayKeyY = 10000;
        internal const int DayKeyM = 100;
        internal const int MinSupportedYear = 1900;
        internal const int NotificationNoneOnlyCount = 1;
        internal const int MinOptionCountForSpacing = 2;
        internal const int MinPositiveInt = 1;

        internal const float DaysPerMonth = 30.4375f;
        internal const float DaysPerYear = 365.25f;
        internal const float MinPositiveFloat = 1f;
        internal const float HostingPowerMetricCount = 4f;
        internal const float OptionSpacingDefault = 80f;
        internal const float OptionSpacingTolerance = 1f;

        internal const long MinPositiveLong = 1L;

        internal const string ModNotifOptionCloneName = "GraduationRebalances_NotificationOption";
        internal static readonly string UnitMonth = ModLocalization.Get("notification.unit_month", "month");
        internal static readonly string UnitDay = ModLocalization.Get("notification.unit_day", "day");

        internal const float PhysLow = 25f;
        internal const float PhysPen = -1f;
        internal const float MentVeryLow = 25f;
        internal const float MentVeryPen = -2f;
        internal const float MentLow = 50f;
        internal const float MentPen = -1f;

        internal const float FriendLow = 40f;
        internal const float FriendHigh = 85f;
        internal const float FriendPen = -1f;
        internal const float FriendBonus = 1f;

        internal const int DatingCap = 4;
        internal const float DatingBonusCap = 4f;
        internal const int DatingAge = 18;
        internal const float DatingPolicyPen = -2f;

        internal const int SalaryLow = 20;
        internal const int SalaryMid = 50;
        internal const int SalaryH1 = 75;
        internal const int SalaryH2 = 100;
        internal const int SalaryH3 = 150;
        internal const float SalaryLowPen = -3f;
        internal const float SalaryMidPen = -1f;
        internal const float SalaryB1 = 1f;
        internal const float SalaryB2 = 2f;
        internal const float SalaryB3 = 3f;

        internal const float Injured = -2f;
        internal const float Depressed = -7f;
        internal const float Hiatus = 2f;

        internal const int PropVHigh = 360;
        internal const int PropHigh = 180;
        internal const int PropMed = 120;
        internal const int PropLow = 90;
        internal const int PropVLow = 60;
        internal const int PropInvalid = -1;
        internal const float PropPenVHigh = -7f;
        internal const float PropPenHigh = -5f;
        internal const float PropPenMed = -4f;
        internal const float PropPenLow = -3f;
        internal const float PropPenVLow = -1f;

        internal const int ShowCap = 3;

        internal const int CenterRow = 0;
        internal const int SecondRow = 1;
        internal const int UnrankedRow = 5;
        internal const float CenterBonus = 4f;
        internal const float SecondBonus = 2f;
        internal const float OtherBonus = 1f;
        internal const float PushBonus = 2f;

        internal const int A1 = 20;
        internal const int A2 = 25;
        internal const int A3 = 30;
        internal const int A4 = 40;
        internal const int A5 = 55;
        internal const int A6 = 70;
        internal const float AS1 = 1f;
        internal const float AS2 = 2f;
        internal const float AS3 = 3f;
        internal const float AS4 = 3f;
        internal const float AS5 = 3f;
        internal const float AS6 = 4f;

        internal const float WSingle = 1.2f;
        internal const float WCenter = 1.5f;
        internal const float WShow = 1.5f;
        internal const float WConcert = 2f;
        internal const float ExpBase = 2f;
        internal const float ExpPerYear = 3f;

        internal const float FansMax = 250000f;
        internal const float WeeklyMax = 200000f;
        internal const float TotalMax = 2000000f;
        internal const float FameMax = 8f;

        internal const float RecencyMaxMonths = 24f;
        internal const float InactiveGrace = 6f;
        internal const float InactiveMax = 24f;
        internal const float InactivePenMax = -10f;
        internal const float NoHistYears = 1f;
        internal const float NoHistPen = -2f;

        internal const float ShieldAct = 6f;
        internal const float ShieldCareer = 8f;
        internal const float ShieldRecent = 4f;

        internal const float KeepAct = 5f;
        internal const float KeepCareer = 3f;
        internal const float KeepRecent = 2f;
        internal const float KeepMax = 10f;

        internal const float RampYears = 0.75f;
        internal const int RampEvents = 2;
        internal const float RampCap = 1.5f;

        internal const float MActMin = 0.35f;
        internal const float MInactiveMax = 9f;
        internal const int MEventsMin = 2;
        internal const float MPowerMin = 0.20f;
        internal const float MRecentMin = 0.10f;

        internal const int HostAgeStart = 24;
        internal const float HostAgeRange = 20f;
        internal const float HostTenureMax = 8f;
        internal const float HostAgeWeight = 0.15f;
        internal const float HostTenureWeight = 0.20f;
        internal const float HostExpWeight = 0.45f;
        internal const float HostRecentWeight = 0.20f;
        internal const float HostNoHistScale = 0.08f;
        internal const float HostNewScale = 0.35f;
        internal const float HostRadioMax = 0.12f;
        internal const float HostTvMax = 0.30f;
        internal const float HostFansFactor = 0.85f;
        internal const float HostFameRadio = 0.75f;
        internal const float HostFameTv = 1.25f;
        internal const float HostBuzzRadio = 0.65f;
        internal const float HostBuzzTv = 1.10f;

        internal const int MExtMonths = 4;
        internal const int CenterStep = 5;
        internal const int CenterBonusMonths = 4;

        internal const float Opinion = 0.6f;
        internal const float OpinionNeutral = 0.5f;
        internal const long FansSignal = 50000L;
        internal const int FameSignal = 4;
        internal const float AppealSignal = 6f;
        internal const float WeeklyRatio = 1.25f;
        internal const float WeeklyFallback = 50000f;
        internal const long TotalSignal = 500000L;
        internal const int NeedSignals = 3;

        internal const float PotH = 90f;
        internal const float PotM = 80f;
        internal const float PotL = 70f;
        internal const int PotHY = 3;
        internal const int PotMY = 2;
        internal const int PotLY = 1;

        internal const string DateFmt = "DATETIME__MONTH";
        internal const string QuantityUnitSeparator = " ";
        internal const string QuantitySegmentSeparator = ", ";
        internal const int SingularQuantity = 1;
        internal const string PluralSuffix = "s";
        internal const string NotificationReasonSuffixFormatLocalizationKey = "notification.reason_suffix_format";
        internal const string NotificationReasonSuffixFormatFallback = " ({0})";
        internal const string NotificationMessageFormatLocalizationKey = "notification.message_format";
        internal const string NotificationMessageFormatFallback = "{0}{1}{2}{3}. New date: {4}";
        internal const string NotificationOptionsListEmptyLogMessage = "[GraduationRebalances] Notification options list is empty.";
        internal const string NotificationTemplateNotFoundLogMessage = "[GraduationRebalances] Notification options template not found.";
        internal const string NotificationCloneMissingCheckboxLogMessage = "[GraduationRebalances] Notification option clone missing checkbox component.";
        internal const string NotificationOptionInjectedLogFormat = "[GraduationRebalances] Injected notification option: {0}";
        internal static readonly string NotePrefix = ModLocalization.Get("notification.note_prefix", " graduation extended by ");
        internal static readonly string ReasonMonthly = ModLocalization.Get("notification.reason_monthly", "monthly performance");
        internal static readonly string ReasonCenter = ModLocalization.Get("notification.reason_center", "center milestone");
        internal static readonly string ReasonWeekly = ModLocalization.Get("notification.reason_weekly", "weekly factors");
        internal static readonly string ReasonWeeklyAnnounced = ModLocalization.Get("notification.reason_weekly_announced", "weekly factors (announced)");

        // Custom enum values for a mod-only notification category and its one-time init marker.
        internal const int ModNotifTypeRaw = 10001;
        internal const int ModNotifInitMarkerRaw = 10002;
        internal static readonly string ModNotifLabel = ModLocalization.Get("notification.option_label", "Graduation day postponing");
        internal static readonly NotificationManager._notification._type ModNotifType =
            (NotificationManager._notification._type)ModNotifTypeRaw;
        internal static readonly NotificationManager._notification._type ModNotifInitMarkerType =
            (NotificationManager._notification._type)ModNotifInitMarkerRaw;
    }

    internal struct Career
    {
        internal float Years;
        internal int Singles;
        internal int Centers;
        internal int Shows;
        internal int Concerts;
        internal int Events;
        internal float Activity;
        internal float Power;
        internal float Recency;
        internal float MonthsSince;
    }

    internal static class S
    {
        // Tracks weekly rebalance execution per idol so announced idols (which are checked daily by the game)
        // are not adjusted every day.
        internal static readonly Dictionary<int, int> LastWeekly = new Dictionary<int, int>();

        // Tracks monthly extension execution per idol to prevent duplicate monthly bonuses.
        internal static readonly Dictionary<int, int> LastMonthly = new Dictionary<int, int>();

        // Tracks center milestone level per idol so each milestone grants its bonus once.
        internal static readonly Dictionary<int, int> LastCenter = new Dictionary<int, int>();

        // Daily cache for computed career profiles.
        internal static readonly Dictionary<int, Career> Profiles = new Dictionary<int, Career>();
        internal static int ProfileDateKey = C.UnsetKey;
    }

    [HarmonyPatch(typeof(data_girls), nameof(data_girls.LoadFunction))]
    internal static class data_girls_LoadFunction_Patch
    {
        // Resets all transient caches whenever girl data is loaded or reloaded.
        private static void Postfix()
        {
            S.LastWeekly.Clear();
            S.LastMonthly.Clear();
            S.LastCenter.Clear();
            S.Profiles.Clear();
            S.ProfileDateKey = C.UnsetKey;
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), HarmonyTargetMethodNames.GraduationSetDefaultDate)]
    internal static class Graduation_Set_Default_Date_Patch
    {
        // Replaces vanilla default graduation scheduling with the rebalance model.
        private static bool Prefix(data_girls.girls __instance)
        {
            if (__instance == null)
            {
                return false;
            }

            int age = __instance.GetAge();
            int minYears = Math.Max(C.MinYears, C.MinAge - age);
            int peakYears = __instance.peakAge > age ? (__instance.peakAge + C.PeakBuffer - age) : minYears;
            int years = Math.Max(minYears, peakYears) + H.GetPotentialBonusYears(__instance);

            __instance.Will_Graduate_At_18 = false;
            __instance.Graduation_Date = staticVars.dateTime
                .AddYears(years)
                .AddMonths(UnityEngine.Random.Range(0, C.Ym))
                .AddDays(UnityEngine.Random.Range(0, C.DaysInShortMonth));

            return false;
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), HarmonyTargetMethodNames.GraduationDateUpdate)]
    internal static class Graduation_Date_Update_Patch
    {
        // Runs the full graduation rebalance update and suppresses vanilla handling.
        private static bool Prefix(data_girls.girls __instance, ref bool __result)
        {
            // Always take over vanilla logic. This lets the rebalance stay deterministic and avoids mixing
            // base-game date updates with modded updates.
            if (__instance == null)
            {
                __result = false;
                return false;
            }

            bool isAnnounced = __instance.status == data_girls._status.announced_graduation;

            // Keep vanilla "graduate when date has passed" behavior for announced idols.
            // This check must stay on every call because announced idols are checked daily.
            if (isAnnounced)
            {
                if (__instance.Graduation_Date < staticVars.dateTime)
                {
                    __instance.Graduate(true, "");
                    __result = true;
                    return false;
                }

                // Important: announced idols continue below so weekly/monthly extensions can still delay graduation.
            }
            else if ((__instance.Graduation_Date - staticVars.dateTime).Days < C.AnnounceDays)
            {
                // Keep vanilla announcement trigger for non-announced idols.
                __instance.Graduation_Announce(true);
                __result = true;
                return false;
            }

            // Run weekly factors once per calendar week per idol.
            // This is required because announced idols are evaluated daily by base game.
            int days = 0;
            if (ShouldRunWeekly(__instance))
            {
                days = ApplyWeeklyAdjustment(__instance);
            }

            // Positive weekly adjustments notify players so they can verify that the delay logic fired.
            // Announced idols use a distinct reason label for easier debugging in live saves.
            if (days > 0)
            {
                H.NotifyExtension(__instance, 0, days, isAnnounced ? C.ReasonWeeklyAnnounced : C.ReasonWeekly);
            }

            // Monthly extensions are independently gated to once per month per idol.
            ApplyMonthlyExtension(__instance);
            __result = false;
            return false;
        }

        // Aggregates the weekly model and applies the resulting day delta to the graduation date.
        // Positive values delay graduation; negative values move it closer.
        private static int ApplyWeeklyAdjustment(data_girls.girls girl)
        {
            float delta = 0f;
            ApplyWellbeing(girl, ref delta);
            ApplyRelationshipAndDating(girl, ref delta);
            ApplySalaryAndStatus(girl, ref delta);
            ApplyProposalAndWeeklyActivity(girl, ref delta);
            ApplyCareerModel(girl, ref delta);

            int days = Mathf.RoundToInt(delta);
            if (days != 0)
            {
                girl.Graduation_Date = girl.Graduation_Date.AddDays(days);
            }
            return days;
        }

        // Applies stamina-based pressure adjustments.
        private static void ApplyWellbeing(data_girls.girls girl, ref float delta)
        {
            float physical = girl.getParam(data_girls._paramType.physicalStamina).val;
            float mental = girl.getParam(data_girls._paramType.mentalStamina).val;
            if (physical < C.PhysLow)
            {
                delta += C.PhysPen;
            }
            if (mental < C.MentVeryLow)
            {
                delta += C.MentVeryPen;
            }
            else if (mental < C.MentLow)
            {
                delta += C.MentPen;
            }
        }

        // Applies relationship quality and dating-policy effects.
        private static void ApplyRelationshipAndDating(data_girls.girls girl, ref float delta)
        {
            float friendship = girl.GetRelationshipWithPlayer(Relationships_Player._type.Friendship).Ratio;
            if (friendship < C.FriendLow)
            {
                delta += C.FriendPen;
            }
            else if (friendship > C.FriendHigh)
            {
                delta += C.FriendBonus;
            }

            if (girl.DatingData.Success_Counter > C.DatingCap)
            {
                delta += C.DatingBonusCap;
            }
            else
            {
                delta += girl.DatingData.Success_Counter;
            }

            int age = girl.GetAge();
            if (policies.GetSelectedPolicyValue(policies._type.dating).Value != policies._value.dating_allowed
                && girl.DatingData.Partner_Status == data_girls.girls._dating_data._partner_status.free
                && age >= C.DatingAge)
            {
                delta += C.DatingPolicyPen;
            }
        }

        // Applies satisfaction thresholds and temporary status modifiers.
        private static void ApplySalaryAndStatus(data_girls.girls girl, ref float delta)
        {
            int sat = girl.GetSalarySatisfaction_Percentage();
            if (sat < C.SalaryLow)
            {
                delta += C.SalaryLowPen;
            }
            else if (sat < C.SalaryMid)
            {
                delta += C.SalaryMidPen;
            }
            else if (sat > C.SalaryH3)
            {
                delta += C.SalaryB3;
            }
            else if (sat > C.SalaryH2)
            {
                delta += C.SalaryB2;
            }
            else if (sat > C.SalaryH1)
            {
                delta += C.SalaryB1;
            }

            if (girl.status == data_girls._status.injured)
            {
                delta += C.Injured;
            }
            else if (girl.status == data_girls._status.depressed)
            {
                delta += C.Depressed;
            }
            else if (girl.status == data_girls._status.hiatus)
            {
                delta += C.Hiatus;
            }
        }

        // Applies proposal recency and weekly activity contributions.
        private static void ApplyProposalAndWeeklyActivity(data_girls.girls girl, ref float delta)
        {
            int d = business.GetDaysSinceLastProposal(girl);
            if (d > C.PropVHigh)
            {
                delta += C.PropPenVHigh;
            }
            else if (d > C.PropHigh)
            {
                delta += C.PropPenHigh;
            }
            else if (d > C.PropMed)
            {
                delta += C.PropPenMed;
            }
            else if (d > C.PropLow)
            {
                delta += C.PropPenLow;
            }
            else if (d > C.PropVLow)
            {
                delta += C.PropPenVLow;
            }
            else if (d != C.PropInvalid)
            {
                delta += business.GetProposalsInTheLastDays(girl, C.ProposalRecentDays);
            }

            int showCount = Shows.GetShowsWithGirl(girl, false).Count;
            if (showCount > C.ShowCap)
            {
                showCount = C.ShowCap;
            }
            delta += showCount;

            singles._single single = singles.GetLatestReleasedSingle(false, null);
            if (single != null)
            {
                int row = single.GetRowOfAGirl(girl);
                if (row == C.CenterRow)
                {
                    delta += C.CenterBonus;
                }
                else if (row == C.SecondRow)
                {
                    delta += C.SecondBonus;
                }
                else if (row != C.UnrankedRow)
                {
                    delta += C.OtherBonus;
                }
            }

            if (girl.Is_Pushed())
            {
                delta += C.PushBonus;
            }
        }

        // Applies career longevity shielding and inactivity pressure.
        private static void ApplyCareerModel(data_girls.girls girl, ref float delta)
        {
            Career p = H.GetCareer(girl);
            float agePressure = H.GetAgePressure(girl.GetAge());

            float shield = p.Activity * C.ShieldAct + p.Power * C.ShieldCareer + p.Recency * C.ShieldRecent;
            delta -= Mathf.Max(0f, agePressure - shield);

            float keep = p.Activity * C.KeepAct + p.Power * C.KeepCareer + p.Recency * C.KeepRecent;
            keep = Mathf.Min(keep, C.KeepMax);
            if (p.Years < C.RampYears && p.Events < C.RampEvents)
            {
                keep = Mathf.Min(keep, C.RampCap);
            }
            delta += keep;

            if (p.Years >= C.NoHistYears && p.Events == 0)
            {
                delta += C.NoHistPen;
            }

            if (p.MonthsSince > C.InactiveGrace)
            {
                float span = Mathf.Max(C.MinPositiveFloat, C.InactiveMax - C.InactiveGrace);
                float ratio = Mathf.Clamp01((p.MonthsSince - C.InactiveGrace) / span);
                delta += C.InactivePenMax * ratio;
            }
        }

        // Applies monthly bonus extensions and emits corresponding notices.
        private static void ApplyMonthlyExtension(data_girls.girls girl)
        {
            if (!ShouldRunMonthly(girl))
            {
                return;
            }

            int perfMonths = MeetsMonthlyPerformance(girl) ? C.MExtMonths : 0;
            int centerMonths = GetCenterMilestoneMonths(girl);
            int total = perfMonths + centerMonths;
            if (total > 0)
            {
                girl.Graduation_Date = girl.Graduation_Date.AddMonths(total);
            }

            if (perfMonths > 0)
            {
                H.NotifyExtension(girl, perfMonths, 0, C.ReasonMonthly);
            }
            if (centerMonths > 0)
            {
                H.NotifyExtension(girl, centerMonths, 0, C.ReasonCenter);
            }
        }

        // Guard to ensure the weekly date model runs once per calendar week per idol.
        // This prevents announced idols from receiving seven times the intended adjustment.
        private static bool ShouldRunWeekly(data_girls.girls girl)
        {
            if (girl == null)
            {
                return false;
            }
            int key = H.GetWeekAnchorKey(staticVars.dateTime);
            int last;
            if (S.LastWeekly.TryGetValue(girl.id, out last) && last == key)
            {
                return false;
            }
            S.LastWeekly[girl.id] = key;
            return true;
        }

        // Guard to ensure monthly extensions run once per calendar month per idol.
        private static bool ShouldRunMonthly(data_girls.girls girl)
        {
            if (girl == null)
            {
                return false;
            }
            int key = staticVars.dateTime.Year * C.Ym + staticVars.dateTime.Month;
            int last;
            if (S.LastMonthly.TryGetValue(girl.id, out last) && last == key)
            {
                return false;
            }
            S.LastMonthly[girl.id] = key;
            return true;
        }

        // Evaluates whether an idol qualifies for the monthly postponement bonus.
        private static bool MeetsMonthlyPerformance(data_girls.girls girl)
        {
            Career p = H.GetCareer(girl);
            if (p.Activity < C.MActMin || p.MonthsSince > C.MInactiveMax || p.Events < C.MEventsMin || p.Power < C.MPowerMin || p.Recency < C.MRecentMin)
            {
                return false;
            }

            int signals = 0;
            if (H.GetWeightedFanOpinion(girl) >= C.Opinion) signals++;
            if (girl.GetFans_Total(null) >= C.FansSignal) signals++;
            if (girl.GetFameLevel() >= C.FameSignal) signals++;
            if (girl.GetAppeal_Total() >= C.AppealSignal) signals++;

            float weekly = girl.GetAverageEarnings();
            long expected = girl.GetExpectedSalary_Total();
            if (expected > 0)
            {
                if (weekly >= expected * C.WeeklyRatio) signals++;
            }
            else if (weekly >= C.WeeklyFallback)
            {
                signals++;
            }

            if (girl.GetTotalEarnings() >= C.TotalSignal) signals++;
            return signals >= C.NeedSignals;
        }

        // Converts center count milestones into one-time monthly extension grants.
        private static int GetCenterMilestoneMonths(data_girls.girls girl)
        {
            if (girl == null || girl.RowInSenbatsu == null || girl.RowInSenbatsu.Count == 0)
            {
                return 0;
            }

            int centers = 0;
            foreach (int row in girl.RowInSenbatsu)
            {
                if (row == C.CenterRow) centers++;
            }

            int milestone = centers / C.CenterStep;
            if (milestone <= 0)
            {
                return 0;
            }

            int last;
            if (!S.LastCenter.TryGetValue(girl.id, out last))
            {
                S.LastCenter[girl.id] = milestone;
                return 0;
            }
            if (milestone <= last)
            {
                return 0;
            }

            S.LastCenter[girl.id] = milestone;
            return (milestone - last) * C.CenterBonusMonths;
        }
    }

    [HarmonyPatch(typeof(Shows._show), nameof(Shows._show.NewEpisode))]
    internal static class Shows_NewEpisode_HostingBonus_Patch
    {
        // Adds hosting-performance rewards to each newly produced show episode.
        private static void Postfix(Shows._show __instance)
        {
            if (__instance == null || __instance.episodeCount <= 0 || __instance.medium == null)
            {
                return;
            }

            Shows._param._media_type? t = __instance.medium.media_type;
            if (t == null)
            {
                return;
            }

            float max;
            float fameFactor;
            float buzzFactor;
            if (t.Value == Shows._param._media_type.radio)
            {
                max = C.HostRadioMax;
                fameFactor = C.HostFameRadio;
                buzzFactor = C.HostBuzzRadio;
            }
            else if (t.Value == Shows._param._media_type.tv)
            {
                max = C.HostTvMax;
                fameFactor = C.HostFameTv;
                buzzFactor = C.HostBuzzTv;
            }
            else
            {
                return;
            }

            List<data_girls.girls> cast = __instance.GetCast();
            if (cast == null || cast.Count == 0)
            {
                return;
            }

            float score = 0f;
            int count = 0;
            foreach (data_girls.girls girl in cast)
            {
                if (girl == null)
                {
                    continue;
                }
                score += H.GetHostingScore(girl);
                count++;
            }
            if (count == 0)
            {
                return;
            }

            float ratio = max * Mathf.Clamp01(score / count);
            if (ratio <= 0f)
            {
                return;
            }

            int ep = __instance.episodeCount - 1;
            if (!HasEpisodeData(__instance, ep))
            {
                return;
            }

            int oldFans = __instance.fans[ep];
            int deltaFans = ScaleInt(oldFans, ratio * C.HostFansFactor);
            if (deltaFans > 0)
            {
                __instance.fans[ep] = oldFans + deltaFans;
                data_girls.AddFans_Equally(deltaFans, cast);
            }

            long oldRevenue = __instance.revenue[ep];
            long deltaRevenue = ScaleLong(oldRevenue, ratio);
            if (deltaRevenue > 0L)
            {
                long newRevenue = oldRevenue + deltaRevenue;
                __instance.revenue[ep] = newRevenue;
                AddExtraEarnings(cast, __instance.cost, oldRevenue, newRevenue);
            }

            int oldFame = __instance.famePoints[ep];
            int deltaFame = ScaleInt(oldFame, ratio * fameFactor);
            if (deltaFame > 0)
            {
                __instance.famePoints[ep] = oldFame + deltaFame;
                AddExtraFame(cast, deltaFame);
            }

            int oldBuzz = __instance.buzz[ep];
            int deltaBuzz = ScaleInt(oldBuzz, ratio * buzzFactor);
            if (deltaBuzz > 0)
            {
                __instance.buzz[ep] = oldBuzz + deltaBuzz;
            }
        }

        // Validates all episode stat arrays before writing back boosted values.
        private static bool HasEpisodeData(Shows._show show, int ep)
        {
            return show.fans != null && show.revenue != null && show.famePoints != null && show.buzz != null
                && ep >= 0
                && ep < show.fans.Count
                && ep < show.revenue.Count
                && ep < show.famePoints.Count
                && ep < show.buzz.Count;
        }

        // Scales integer metrics while preserving a minimum +1 gain when applicable.
        private static int ScaleInt(int value, float ratio)
        {
            if (value <= 0 || ratio <= 0f)
            {
                return 0;
            }
            int delta = Mathf.RoundToInt(value * ratio);
            return delta <= 0 ? C.MinPositiveInt : delta;
        }

        // Scales long metrics while preserving a minimum +1 gain when applicable.
        private static long ScaleLong(long value, float ratio)
        {
            if (value <= 0 || ratio <= 0f)
            {
                return 0L;
            }
            long delta = (long)Math.Round(value * ratio);
            return delta <= 0L ? C.MinPositiveLong : delta;
        }

        // Distributes additional show profits to the cast based on boosted revenue.
        private static void AddExtraEarnings(List<data_girls.girls> cast, int budget, long oldRevenue, long newRevenue)
        {
            if (cast == null || cast.Count == 0)
            {
                return;
            }
            long oldShare = RevenueShare(oldRevenue, budget, cast.Count);
            long newShare = RevenueShare(newRevenue, budget, cast.Count);
            long delta = newShare - oldShare;
            if (delta <= 0)
            {
                return;
            }
            foreach (data_girls.girls girl in cast)
            {
                if (girl != null) girl.Earn(delta);
            }
        }

        // Computes per-idol profit share after production costs.
        private static long RevenueShare(long revenue, int budget, int castCount)
        {
            if (castCount <= 0)
            {
                return 0L;
            }
            long profit = revenue - budget;
            if (profit <= 0)
            {
                return 0L;
            }
            return profit / castCount;
        }

        // Splits extra fame points equally across all cast members.
        private static void AddExtraFame(List<data_girls.girls> cast, int fame)
        {
            if (cast == null || cast.Count == 0 || fame <= 0)
            {
                return;
            }
            float perGirl = (float)fame / cast.Count;
            foreach (data_girls.girls girl in cast)
            {
                if (girl != null) girl.addParam(data_girls._paramType.famePoints, perGirl, false);
            }
        }
    }

    [HarmonyPatch(typeof(staticVars._settings), nameof(staticVars._settings.Initialization))]
    internal static class Settings_Initialization_CustomNotification_Patch
    {
        // Ensures custom notification defaults are present in settings after init.
        private static void Postfix(staticVars._settings __instance)
        {
            H.EnsureCustomNotificationDefaults(__instance);
        }
    }

    [HarmonyPatch(typeof(NotificationManager), nameof(NotificationManager.GetOptionText))]
    internal static class NotificationManager_GetOptionText_CustomNotification_Patch
    {
        // Returns a user-facing label for the custom notification type in UI lists.
        private static void Postfix(NotificationManager._notification._type Type, ref string __result)
        {
            if (Type == C.ModNotifType)
            {
                __result = C.ModNotifLabel;
            }
        }
    }

    [HarmonyPatch(typeof(Notifications_Popup), nameof(Notifications_Popup.OnClick_Options))]
    internal static class Notifications_Popup_OnClick_Options_CustomCheckbox_Patch
    {
        // Injects the custom options row before options panel is displayed.
        private static void Prefix(Notifications_Popup __instance)
        {
            H.EnsureCustomNotificationOption(__instance);
        }
    }

    [HarmonyPatch(typeof(Notifications_Popup), HarmonyTargetMethodNames.NotificationsShowOptions)]
    internal static class Notifications_Popup_Show_Options_CustomCheckbox_Patch
    {
        // Reinforces injection after options panel animation/setup.
        private static void Postfix(Notifications_Popup __instance)
        {
            H.EnsureCustomNotificationOption(__instance);
        }
    }

    [HarmonyPatch(typeof(Notification_Option_Checkbox), HarmonyTargetMethodNames.NotificationOptionCheckboxStart)]
    internal static class Notification_Option_Checkbox_Start_CustomCheckbox_Patch
    {
        // Injects the custom row when native checkbox components initialize.
        private static void Postfix(Notification_Option_Checkbox __instance)
        {
            if (__instance == null)
            {
                return;
            }

            Notifications_Popup popup = __instance.GetComponentInParent<Notifications_Popup>();
            if (popup != null)
            {
                H.EnsureCustomNotificationOption(popup);
            }
        }
    }

    [HarmonyPatch(typeof(Notifications_Popup), nameof(Notifications_Popup.Render))]
    internal static class Notifications_Popup_Render_CustomCheckbox_Patch
    {
        // Reinforces injection when notification popup log redraws.
        private static void Postfix(Notifications_Popup __instance)
        {
            H.EnsureCustomNotificationOption(__instance);
        }
    }

    internal static class H
    {
        // Stat set used for potential averaging in default graduation scheduling.
        private static readonly data_girls._paramType[] StatParams = new data_girls._paramType[]
        {
            data_girls._paramType.cute,
            data_girls._paramType.cool,
            data_girls._paramType.sexy,
            data_girls._paramType.pretty,
            data_girls._paramType.vocal,
            data_girls._paramType.dance,
            data_girls._paramType.funny,
            data_girls._paramType.smart
        };

        // Maps average hidden potential to extra years before graduation pressure.
        internal static int GetPotentialBonusYears(data_girls.girls girl)
        {
            float avg = GetAveragePotential(girl);
            if (avg >= C.PotH) return C.PotHY;
            if (avg >= C.PotM) return C.PotMY;
            if (avg >= C.PotL) return C.PotLY;
            return 0;
        }

        // Computes cumulative age pressure used by the weekly model.
        internal static float GetAgePressure(int age)
        {
            float p = 0f;
            if (age >= C.A1) p += C.AS1;
            if (age >= C.A2) p += C.AS2;
            if (age >= C.A3) p += C.AS3;
            if (age >= C.A4) p += C.AS4;
            if (age >= C.A5) p += C.AS5;
            if (age >= C.A6) p += C.AS6;
            return p;
        }

        // Scores idol suitability for radio/TV hosting boosts.
        internal static float GetHostingScore(data_girls.girls girl)
        {
            if (girl == null)
            {
                return 0f;
            }

            Career p = GetCareer(girl);
            float ageScore = Mathf.Clamp01((girl.GetAge() - C.HostAgeStart) / C.HostAgeRange);
            float tenureScore = Mathf.Clamp01(p.Years / C.HostTenureMax);
            float score = ageScore * C.HostAgeWeight
                + tenureScore * C.HostTenureWeight
                + p.Activity * C.HostExpWeight
                + p.Recency * C.HostRecentWeight;

            if (p.Events == 0)
            {
                score *= C.HostNoHistScale;
            }
            else if (p.Events < C.RampEvents || p.Years < C.RampYears)
            {
                score *= C.HostNewScale;
            }

            return Mathf.Clamp01(score);
        }

        // Returns cached career profile, rebuilding once per in-game day as needed.
        internal static Career GetCareer(data_girls.girls girl)
        {
            if (girl == null)
            {
                return default(Career);
            }

            RefreshProfileCacheIfNeeded();
            Career p;
            if (S.Profiles.TryGetValue(girl.id, out p))
            {
                return p;
            }

            p = BuildCareer(girl);
            S.Profiles[girl.id] = p;
            return p;
        }

        // Clears daily career cache when in-game date changes.
        private static void RefreshProfileCacheIfNeeded()
        {
            int key = staticVars.dateTime.Year * C.DayKeyY + staticVars.dateTime.Month * C.DayKeyM + staticVars.dateTime.Day;
            if (key == S.ProfileDateKey)
            {
                return;
            }

            S.ProfileDateKey = key;
            S.Profiles.Clear();
        }

        // Returns a stable week key based on the Monday of the current week.
        // This avoids locale-specific week-number differences and makes gating deterministic.
        internal static int GetWeekAnchorKey(DateTime date)
        {
            DateTime d = date.Date;
            int offset = ((int)d.DayOfWeek + C.MondayOffset) % C.DaysPerWeek; // Convert to Monday-based offset.
            DateTime monday = d.AddDays(-offset);
            return monday.Year * C.DayKeyY + monday.Month * C.DayKeyM + monday.Day;
        }

        // Builds career profile metrics from released activities and recency.
        private static Career BuildCareer(data_girls.girls girl)
        {
            Career p = new Career();
            p.Years = GetYearsWithAgency(girl);

            DateTime latestSingle;
            DateTime latestShow;
            DateTime latestConcert;
            p.Singles = CountReleasedSingles(girl, out p.Centers, out latestSingle);
            p.Shows = CountReleasedShows(girl, out latestShow);
            p.Concerts = CountFinishedConcerts(girl, out latestConcert);
            p.Events = p.Singles + p.Shows + p.Concerts;

            float weighted = p.Singles * C.WSingle + p.Centers * C.WCenter + p.Shows * C.WShow + p.Concerts * C.WConcert;
            float target = C.ExpBase + p.Years * C.ExpPerYear;
            if (target < C.MinPositiveFloat)
            {
                target = C.MinPositiveFloat;
            }
            p.Activity = Mathf.Clamp01(weighted / target);

            float fansPower = Normalize(girl.GetFans_Total(null), C.FansMax);
            float weeklyPower = Normalize(girl.GetAverageEarnings(), C.WeeklyMax);
            float totalPower = Normalize(girl.GetTotalEarnings(), C.TotalMax);
            float famePower = Normalize(girl.GetFameLevel(), C.FameMax);
            p.Power = Mathf.Clamp01((fansPower + weeklyPower + totalPower + famePower) / C.HostingPowerMetricCount);

            DateTime latest = latestSingle;
            if (latestShow > latest) latest = latestShow;
            if (latestConcert > latest) latest = latestConcert;

            if (latest == DateTime.MinValue)
            {
                p.MonthsSince = C.RecencyMaxMonths;
                p.Recency = 0f;
            }
            else
            {
                float months = (float)(staticVars.dateTime - latest).TotalDays / C.DaysPerMonth;
                if (months < 0f)
                {
                    months = 0f;
                }
                p.MonthsSince = months;
                p.Recency = 1f - Mathf.Clamp01(months / C.RecencyMaxMonths);
            }

            return p;
        }

        // Averages hidden potential across all core training stats.
        private static float GetAveragePotential(data_girls.girls girl)
        {
            if (girl == null)
            {
                return 0f;
            }

            float sum = 0f;
            int count = 0;
            foreach (data_girls._paramType type in StatParams)
            {
                data_girls.girls.param p = girl.getParam(type);
                if (p == null) continue;
                sum += p.GetPotential();
                count++;
            }

            return count == 0 ? 0f : sum / count;
        }

        // Converts hiring date into years with agency while handling invalid dates.
        private static float GetYearsWithAgency(data_girls.girls girl)
        {
            if (girl == null)
            {
                return 0f;
            }
            DateTime hire = girl.Hiring_Date;
            if (hire <= DateTime.MinValue || hire.Year < C.MinSupportedYear)
            {
                return 0f;
            }
            float years = (float)(staticVars.dateTime - hire).TotalDays / C.DaysPerYear;
            return years < 0f ? 0f : years;
        }

        // Counts released singles with the idol in ranking and tracks latest release.
        private static int CountReleasedSingles(data_girls.girls girl, out int centers, out DateTime latest)
        {
            centers = 0;
            latest = DateTime.MinValue;
            if (girl == null || singles.Singles == null)
            {
                return 0;
            }

            int count = 0;
            foreach (singles._single sng in singles.Singles)
            {
                if (sng == null || sng.status != singles._single._status.released)
                {
                    continue;
                }

                int row = sng.GetRowOfAGirl(girl);
                if (row == C.UnrankedRow)
                {
                    continue;
                }

                count++;
                if (row == C.CenterRow)
                {
                    centers++;
                }
                if (sng.ReleaseData != null && sng.ReleaseData.ReleaseDate > latest)
                {
                    latest = sng.ReleaseData.ReleaseDate;
                }
            }

            return count;
        }

        // Counts released/canceled-complete shows involving the idol.
        private static int CountReleasedShows(data_girls.girls girl, out DateTime latest)
        {
            latest = DateTime.MinValue;
            if (girl == null || Shows.shows == null)
            {
                return 0;
            }

            int count = 0;
            foreach (Shows._show show in Shows.shows)
            {
                if (show == null || show.status == Shows._show._status.canceled || show.status == Shows._show._status.normal || show.status == Shows._show._status.working || show.episodeCount <= 0)
                {
                    continue;
                }

                if (!IsGirlInShow(girl, show))
                {
                    continue;
                }

                count++;
                DateTime d = LastShowDate(show);
                if (d > latest)
                {
                    latest = d;
                }
            }

            return count;
        }

        // Resolves cast membership for both explicit and entire-group show casts.
        private static bool IsGirlInShow(data_girls.girls girl, Shows._show show)
        {
            if (girl == null || show == null)
            {
                return false;
            }
            if (show.castType == Shows._show._castType.entireGroup)
            {
                return girl.status != data_girls._status.graduated;
            }
            if (show.girls == null)
            {
                return false;
            }
            foreach (data_girls.girls castGirl in show.girls)
            {
                if (castGirl == girl) return true;
            }
            return false;
        }

        // Returns best-known date of the most recent episode for a show.
        private static DateTime LastShowDate(Shows._show show)
        {
            if (show == null || show.episodeCount <= 0)
            {
                return DateTime.MinValue;
            }
            DateTime launch = show.LaunchDate;
            if (launch <= DateTime.MinValue || launch.Year < C.MinSupportedYear)
            {
                return staticVars.dateTime.AddDays(-C.ShowFallbackDaysAgo);
            }
            return launch.AddDays((show.episodeCount - C.MinPositiveInt) * C.ShowEpisodeSpacingDays);
        }

        // Counts finished concerts that included the idol and tracks latest finish date.
        private static int CountFinishedConcerts(data_girls.girls girl, out DateTime latest)
        {
            latest = DateTime.MinValue;
            if (girl == null || SEvent_Concerts.Concerts == null)
            {
                return 0;
            }

            int count = 0;
            foreach (SEvent_Concerts._concert concert in SEvent_Concerts.Concerts)
            {
                if (concert == null || concert.Status != SEvent_Tour.tour._status.finished)
                {
                    continue;
                }

                List<data_girls.girls> girls = concert.GetGirls(true);
                if (girls == null || !girls.Contains(girl))
                {
                    continue;
                }

                count++;
                if (concert.FinishDate > latest)
                {
                    latest = concert.FinishDate;
                }
            }

            return count;
        }

        // Normalizes float values into [0..1] with zero-safe protection.
        private static float Normalize(float value, float max)
        {
            if (max <= 0f) return 0f;
            return Mathf.Clamp01(value / max);
        }

        // Normalizes long values into [0..1] with zero-safe protection.
        private static float Normalize(long value, float max)
        {
            if (max <= 0f) return 0f;
            return Mathf.Clamp01(value / max);
        }

        // Computes fan-opinion weighted by fan count.
        internal static float GetWeightedFanOpinion(data_girls.girls girl)
        {
            if (girl == null || girl.Fans == null || girl.Fans.Count == 0)
            {
                return C.OpinionNeutral;
            }

            long total = girl.GetFans_Total(null);
            if (total <= 0)
            {
                return C.OpinionNeutral;
            }

            double sum = 0d;
            foreach (resources._fan f in girl.Fans)
            {
                if (f != null) sum += f.GetOpinion() * f.GetNumberOfPeople();
            }
            return (float)(sum / total);
        }

        // Adds custom notification defaults once while preserving opt-out states.
        internal static void EnsureCustomNotificationDefaults(staticVars._settings settings)
        {
            if (settings == null || settings.notification_enabled == null)
            {
                return;
            }

            if (HasNotificationType(settings, C.ModNotifInitMarkerType))
            {
                return;
            }

            bool notificationsFullyDisabled =
                settings.notification_enabled.Count == C.NotificationNoneOnlyCount &&
                settings.notification_enabled[0] == NotificationManager._notification._type.none;

            if (!notificationsFullyDisabled)
            {
                settings.Notification_AddType(C.ModNotifType);
            }

            if (!HasNotificationType(settings, C.ModNotifInitMarkerType))
            {
                settings.notification_enabled.Add(C.ModNotifInitMarkerType);
            }
        }

        // Injects a dedicated custom checkbox into notification options UI.
        internal static void EnsureCustomNotificationOption(Notifications_Popup popup)
        {
            if (popup == null)
            {
                return;
            }

            if (popup.Options_Checkboxes == null)
            {
                popup.Options_Checkboxes = new List<GameObject>();
            }

            if (popup.Options_Checkboxes.Count == 0)
            {
                Notification_Option_Checkbox[] discovered = popup.GetComponentsInChildren<Notification_Option_Checkbox>(true);
                if (discovered != null)
                {
                    foreach (Notification_Option_Checkbox item in discovered)
                    {
                        if (item != null && item.gameObject != null && !popup.Options_Checkboxes.Contains(item.gameObject))
                        {
                            popup.Options_Checkboxes.Add(item.gameObject);
                        }
                    }
                }
            }

            if (popup.Options_Checkboxes.Count == 0)
            {
                Debug.Log(C.NotificationOptionsListEmptyLogMessage);
                return;
            }

            Notification_Option_Checkbox existing = FindNotificationOption(popup, C.ModNotifType);
            if (existing != null)
            {
                existing.Render();
                return;
            }

            GameObject template = null;
            foreach (GameObject option in popup.Options_Checkboxes)
            {
                if (option != null && option.GetComponent<Notification_Option_Checkbox>() != null)
                {
                    template = option;
                    break;
                }
            }
            if (template == null)
            {
                Debug.Log(C.NotificationTemplateNotFoundLogMessage);
                return;
            }

            Transform parent = template.transform.parent;
            GameObject clone = UnityEngine.Object.Instantiate(template, parent, false);
            clone.name = C.ModNotifOptionCloneName;

            Notification_Option_Checkbox checkbox = clone.GetComponent<Notification_Option_Checkbox>();
            if (checkbox == null)
            {
                UnityEngine.Object.Destroy(clone);
                Debug.Log(C.NotificationCloneMissingCheckboxLogMessage);
                return;
            }

            checkbox.Type = C.ModNotifType;
            popup.Options_Checkboxes.Add(clone);
            clone.transform.SetAsLastSibling();
            clone.SetActive(true);

            Checkbox_Text text = clone.GetComponent<Checkbox_Text>();
            if (text != null)
            {
                text.SetText(C.ModNotifLabel);
            }

            checkbox.Render();
            PositionNotificationOptionClone(clone, popup);
            Debug.Log(string.Format(C.NotificationOptionInjectedLogFormat, C.ModNotifLabel));
        }

        // Formats and submits the postponement notification message.
        internal static void NotifyExtension(data_girls.girls girl, int months, int days, string reason)
        {
            if (girl == null || (months <= 0 && days <= 0))
            {
                return;
            }

            string amount;
            string monthAmount = months + C.QuantityUnitSeparator + Plural(C.UnitMonth, months);
            string dayAmount = days + C.QuantityUnitSeparator + Plural(C.UnitDay, days);
            if (months > 0 && days > 0)
            {
                amount = monthAmount + C.QuantitySegmentSeparator + dayAmount;
            }
            else if (months > 0)
            {
                amount = monthAmount;
            }
            else
            {
                amount = dayAmount;
            }

            string date = ExtensionMethods.ToString_Loc(girl.Graduation_Date, C.DateFmt);
            string reasonSuffixFormat = ModLocalization.Get(
                C.NotificationReasonSuffixFormatLocalizationKey,
                C.NotificationReasonSuffixFormatFallback);
            string suffix = string.IsNullOrEmpty(reason)
                ? string.Empty
                : string.Format(reasonSuffixFormat, reason);
            string notificationMessageFormat = ModLocalization.Get(
                C.NotificationMessageFormatLocalizationKey,
                C.NotificationMessageFormatFallback);
            string msg = string.Format(
                notificationMessageFormat,
                girl.GetName(true),
                C.NotePrefix,
                amount,
                suffix,
                date);
            NotificationManager.AddNotification(msg, mainScript.lightBlue32, C.ModNotifType);
        }

        // Returns singular/plural unit text for a counted quantity.
        private static string Plural(string unit, int n)
        {
            return n == C.SingularQuantity ? unit : unit + C.PluralSuffix;
        }

        // Locates an existing checkbox object for a specific notification type.
        private static Notification_Option_Checkbox FindNotificationOption(
            Notifications_Popup popup,
            NotificationManager._notification._type type)
        {
            if (popup == null || popup.Options_Checkboxes == null)
            {
                return null;
            }

            foreach (GameObject option in popup.Options_Checkboxes)
            {
                if (option == null)
                {
                    continue;
                }

                Notification_Option_Checkbox checkbox = option.GetComponent<Notification_Option_Checkbox>();
                if (checkbox != null && checkbox.Type == type)
                {
                    return checkbox;
                }
            }
            return null;
        }

        // Checks whether settings currently include a target notification type.
        private static bool HasNotificationType(staticVars._settings settings, NotificationManager._notification._type type)
        {
            if (settings == null || settings.notification_enabled == null)
            {
                return false;
            }

            foreach (NotificationManager._notification._type current in settings.notification_enabled)
            {
                if (current == type)
                {
                    return true;
                }
            }
            return false;
        }

        // Repositions the injected checkbox when parent layout components are missing.
        private static void PositionNotificationOptionClone(GameObject clone, Notifications_Popup popup)
        {
            if (clone == null)
            {
                return;
            }

            Transform parent = clone.transform.parent;
            RectTransform parentRect = parent as RectTransform;
            if (parentRect == null)
            {
                return;
            }

            if (parent.GetComponent<VerticalLayoutGroup>() != null
                || parent.GetComponent<HorizontalLayoutGroup>() != null
                || parent.GetComponent<GridLayoutGroup>() != null
                || parent.GetComponent<ContentSizeFitter>() != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
                return;
            }

            RectTransform cloneRect = clone.GetComponent<RectTransform>();
            if (cloneRect == null)
            {
                return;
            }

            int sibling = clone.transform.GetSiblingIndex();
            if (sibling <= 0)
            {
                return;
            }

            RectTransform prev = parent.GetChild(sibling - 1) as RectTransform;
            if (prev == null)
            {
                return;
            }

            float spacing = C.OptionSpacingDefault;
            if (sibling > C.MinPositiveInt)
            {
                RectTransform prev2 = parent.GetChild(sibling - C.MinOptionCountForSpacing) as RectTransform;
                if (prev2 != null)
                {
                    float step = Mathf.Abs(prev.anchoredPosition.y - prev2.anchoredPosition.y);
                    if (step > C.OptionSpacingTolerance)
                    {
                        spacing = step;
                    }
                }
            }
            else if (popup != null)
            {
                spacing = GetCheckboxSpacingFromList(popup);
            }

            cloneRect.anchoredPosition = new Vector2(cloneRect.anchoredPosition.x, prev.anchoredPosition.y - spacing);
            cloneRect.localPosition = new Vector3(cloneRect.localPosition.x, prev.localPosition.y - spacing, cloneRect.localPosition.z);
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }

        // Estimates vertical spacing between existing options to place injected rows cleanly.
        private static float GetCheckboxSpacingFromList(Notifications_Popup popup)
        {
            if (popup == null || popup.Options_Checkboxes == null || popup.Options_Checkboxes.Count < C.MinOptionCountForSpacing)
            {
                return C.OptionSpacingDefault;
            }

            for (int i = 1; i < popup.Options_Checkboxes.Count; i++)
            {
                GameObject aObj = popup.Options_Checkboxes[i - 1];
                GameObject bObj = popup.Options_Checkboxes[i];
                if (aObj == null || bObj == null)
                {
                    continue;
                }

                RectTransform a = aObj.GetComponent<RectTransform>();
                RectTransform b = bObj.GetComponent<RectTransform>();
                if (a == null || b == null)
                {
                    continue;
                }

                float spacing = Mathf.Abs(a.anchoredPosition.y - b.anchoredPosition.y);
                if (spacing > C.OptionSpacingTolerance)
                {
                    return spacing;
                }
            }
            return C.OptionSpacingDefault;
        }
    }
}
