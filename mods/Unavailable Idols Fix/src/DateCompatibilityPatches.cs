using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace UnavailableIdolsFix
{
    internal static class DateCompatibilityOwners
    {
        internal const string TelWorkerRights = "com.tel.workerrights";
        internal const string TelNeverGraduate = "com.tel.nevergraduate";
        internal const string TelTraitsExpansion = "com.tel.traitsexpansion";
        internal const string GraduationRebalances = "com.cosmo.graduationrebalances";
    }

    internal static class DateCompatibilityConstants
    {
        internal const int InjuryGraduationMonths = -6;
        internal const int DepressionGraduationMonths = -12;
        internal const int BestFriendGraduationMonths = -12;
        internal const int CliqueGraduationMonths = -3;
        internal const int AnnounceDays = 90;
        internal const int WorkerRightsSalaryLow = 20;
        internal const int WorkerRightsSalaryMid = 50;
        internal const int WorkerRightsPenalty = -9;
        internal const int WorkerRightsSeverePenalty = -27;
        internal const int ProposalVeryHigh = 360;
        internal const int ProposalHigh = 180;
        internal const int ProposalMedium = 120;
        internal const int ProposalLow = 90;
        internal const int ProposalVeryLow = 60;
        internal const int ProposalInvalid = -1;
        internal const int ProposalRecentDays = 60;
        internal const int ShowCap = 3;
        internal const int CenterRow = 0;
        internal const int SecondRow = 1;
        internal const int UnrankedRow = 5;
        internal const int DatingCap = 4;
        internal const int DatingAge = 18;
        internal const int NoDelta = 0;
    }

    internal struct GirlDateCorrectionState
    {
        internal bool HasValue;
        internal data_girls._status InitialStatus;
        internal DateTime Before;
    }

    internal struct GraduationDateUpdateCorrectionState
    {
        internal bool HasValue;
        internal DateTime Before;
        internal bool GraduationRebalancesPresent;
        internal bool WorkerRightsPresent;
        internal bool ShouldApplyVanillaDelta;
        internal int VanillaDeltaDays;
    }

    internal sealed class RelatedGraduationDateState
    {
        internal readonly List<RelatedGraduationDateTarget> Targets = new List<RelatedGraduationDateTarget>();
    }

    internal struct RelatedGraduationDateTarget
    {
        internal data_girls.girls Girl;
        internal DateTime Before;
        internal int Months;
    }

    internal static class ImmutableDateCorrectionRuntime
    {
        private static readonly Dictionary<int, DateTime> DateBeforeWorkerRightsPostfix = new Dictionary<int, DateTime>();

        internal static bool HasPatchOwner(MethodBase method, string owner)
        {
            if (method == null || string.IsNullOrEmpty(owner))
            {
                return false;
            }

            Patches patches = Harmony.GetPatchInfo(method);
            if (patches == null || patches.Owners == null)
            {
                return false;
            }

            foreach (string patchOwner in patches.Owners)
            {
                if (string.Equals(patchOwner, owner, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static MethodBase GraduationDateUpdateMethod()
        {
            return AccessTools.Method(typeof(data_girls.girls), nameof(data_girls.girls.Graduation_Date_Update));
        }

        internal static MethodBase UpdateGraduationDatesMethod()
        {
            return AccessTools.Method(typeof(data_girls), nameof(data_girls.UpdateGraduationDates));
        }

        internal static bool IsNeverGraduatePresent()
        {
            return HasPatchOwner(UpdateGraduationDatesMethod(), DateCompatibilityOwners.TelNeverGraduate);
        }

        internal static bool ShouldApplyDateCorrections()
        {
            return !IsNeverGraduatePresent();
        }

        internal static void CaptureBeforeWorkerRights(data_girls.girls girl)
        {
            if (girl == null)
            {
                return;
            }

            DateBeforeWorkerRightsPostfix[girl.id] = girl.Graduation_Date;
        }

        internal static bool TryPopBeforeWorkerRights(data_girls.girls girl, out DateTime before)
        {
            before = default(DateTime);
            if (girl == null)
            {
                return false;
            }

            if (!DateBeforeWorkerRightsPostfix.TryGetValue(girl.id, out before))
            {
                return false;
            }

            DateBeforeWorkerRightsPostfix.Remove(girl.id);
            return true;
        }

        internal static bool SameDate(DateTime left, DateTime right)
        {
            return left == right;
        }

        internal static void ApplyMonthsIfStillUnchanged(data_girls.girls girl, DateTime before, int months)
        {
            if (ShouldApplyDateCorrections()
                && girl != null
                && SameDate(girl.Graduation_Date, before)
                && months != DateCompatibilityConstants.NoDelta)
            {
                girl.Graduation_Date = before.AddMonths(months);
            }
        }

        internal static void ApplyDaysIfStillUnchanged(data_girls.girls girl, DateTime before, int days)
        {
            if (ShouldApplyDateCorrections()
                && girl != null
                && SameDate(girl.Graduation_Date, before)
                && days != DateCompatibilityConstants.NoDelta)
            {
                girl.Graduation_Date = before.AddDays(days);
            }
        }

        internal static bool ShouldVanillaGraduationDateUpdateApply(data_girls.girls girl)
        {
            if (girl == null || girl.status == data_girls._status.announced_graduation)
            {
                return false;
            }

            return (girl.Graduation_Date - staticVars.dateTime).Days >= DateCompatibilityConstants.AnnounceDays;
        }

        internal static int ComputeVanillaGraduationDateUpdateDelta(data_girls.girls girl)
        {
            if (girl == null)
            {
                return DateCompatibilityConstants.NoDelta;
            }

            float delta = 0f;
            float physicalStamina = girl.getParam(data_girls._paramType.physicalStamina).val;
            float mentalStamina = girl.getParam(data_girls._paramType.mentalStamina).val;
            if (physicalStamina < 25f)
            {
                delta -= 1f;
            }

            if (mentalStamina < 25f)
            {
                delta -= 2f;
            }
            else if (mentalStamina < 50f)
            {
                delta -= 1f;
            }

            float friendshipRatio = girl.GetRelationshipWithPlayer(Relationships_Player._type.Friendship).Ratio;
            if (friendshipRatio < 40f)
            {
                delta -= 1f;
            }
            else if (friendshipRatio > 85f)
            {
                delta += 1f;
            }

            if (girl.DatingData.Success_Counter > DateCompatibilityConstants.DatingCap)
            {
                delta += DateCompatibilityConstants.DatingCap;
            }
            else
            {
                delta += girl.DatingData.Success_Counter;
            }

            int age = girl.GetAge();
            if (policies.GetSelectedPolicyValue(policies._type.dating).Value != policies._value.dating_allowed
                && girl.DatingData.Partner_Status == data_girls.girls._dating_data._partner_status.free
                && age >= DateCompatibilityConstants.DatingAge)
            {
                delta -= 2f;
            }

            int salarySatisfaction = girl.GetSalarySatisfaction_Percentage();
            if (salarySatisfaction < 20)
            {
                delta -= 3f;
            }
            else if (salarySatisfaction < 50)
            {
                delta -= 1f;
            }
            else if (salarySatisfaction > 150)
            {
                delta += 3f;
            }
            else if (salarySatisfaction > 100)
            {
                delta += 2f;
            }
            else if (salarySatisfaction > 75)
            {
                delta += 1f;
            }

            if (girl.status == data_girls._status.injured)
            {
                delta -= 2f;
            }
            else if (girl.status == data_girls._status.depressed)
            {
                delta -= 7f;
            }
            else if (girl.status == data_girls._status.hiatus)
            {
                delta += 2f;
            }

            int daysSinceLastProposal = business.GetDaysSinceLastProposal(girl);
            if (daysSinceLastProposal > DateCompatibilityConstants.ProposalVeryHigh)
            {
                delta -= 7f;
            }
            else if (daysSinceLastProposal > DateCompatibilityConstants.ProposalHigh)
            {
                delta -= 5f;
            }
            else if (daysSinceLastProposal > DateCompatibilityConstants.ProposalMedium)
            {
                delta -= 4f;
            }
            else if (daysSinceLastProposal > DateCompatibilityConstants.ProposalLow)
            {
                delta -= 3f;
            }
            else if (daysSinceLastProposal > DateCompatibilityConstants.ProposalVeryLow)
            {
                delta -= 1f;
            }
            else if (daysSinceLastProposal != DateCompatibilityConstants.ProposalInvalid)
            {
                delta += business.GetProposalsInTheLastDays(girl, DateCompatibilityConstants.ProposalRecentDays);
            }

            int showCount = Shows.GetShowsWithGirl(girl, false).Count;
            if (showCount > DateCompatibilityConstants.ShowCap)
            {
                showCount = DateCompatibilityConstants.ShowCap;
            }

            delta += showCount;

            singles._single latestReleasedSingle = singles.GetLatestReleasedSingle(false, null);
            if (latestReleasedSingle != null)
            {
                int row = latestReleasedSingle.GetRowOfAGirl(girl);
                if (row == DateCompatibilityConstants.CenterRow)
                {
                    delta += 4f;
                }
                else if (row == DateCompatibilityConstants.SecondRow)
                {
                    delta += 2f;
                }
                else if (row != DateCompatibilityConstants.UnrankedRow)
                {
                    delta += 1f;
                }
            }

            if (girl.Is_Pushed())
            {
                delta += 2f;
            }

            // Preserve vanilla's actual branch order. This only corrects the immutable DateTime no-op.
            if (age > 20)
            {
                delta -= 1f;
            }
            else if (age > 25)
            {
                delta -= 3f;
            }
            else if (age > 30)
            {
                delta -= 7f;
            }

            return Mathf.RoundToInt(delta);
        }

        internal static int ComputeWorkerRightsDelta(data_girls.girls girl)
        {
            if (girl == null
                || staticVars.IsEasy()
                || girl.status == data_girls._status.announced_graduation
                || policies.GetSelectedPolicyValue(policies._type.salary).Value != policies._value.salary_manual)
            {
                return DateCompatibilityConstants.NoDelta;
            }

            int salarySatisfaction = girl.GetSalarySatisfaction_Percentage();
            if (salarySatisfaction < DateCompatibilityConstants.WorkerRightsSalaryLow)
            {
                return DateCompatibilityConstants.WorkerRightsSeverePenalty;
            }

            if (salarySatisfaction < DateCompatibilityConstants.WorkerRightsSalaryMid)
            {
                return DateCompatibilityConstants.WorkerRightsPenalty;
            }

            return DateCompatibilityConstants.NoDelta;
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Set_Injured))]
    [HarmonyAfter(new[] { DateCompatibilityOwners.TelTraitsExpansion, DateCompatibilityOwners.GraduationRebalances })]
    internal static class InjuryImmutableDatePatch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(data_girls.girls __instance, out GirlDateCorrectionState __state)
        {
            __state = new GirlDateCorrectionState();
            if (__instance == null)
            {
                return;
            }

            __state.HasValue = true;
            __state.InitialStatus = __instance.status;
            __state.Before = __instance.Graduation_Date;
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls __instance, GirlDateCorrectionState __state)
        {
            if (!__state.HasValue
                || __state.InitialStatus == data_girls._status.announced_graduation
                || __instance == null
                || __instance.status != data_girls._status.injured)
            {
                return;
            }

            ImmutableDateCorrectionRuntime.ApplyMonthsIfStillUnchanged(
                __instance,
                __state.Before,
                DateCompatibilityConstants.InjuryGraduationMonths);
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Set_Depressed))]
    [HarmonyAfter(new[] { DateCompatibilityOwners.TelTraitsExpansion, DateCompatibilityOwners.GraduationRebalances })]
    internal static class DepressionImmutableDatePatch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(data_girls.girls __instance, out GirlDateCorrectionState __state)
        {
            __state = new GirlDateCorrectionState();
            if (__instance == null)
            {
                return;
            }

            __state.HasValue = true;
            __state.InitialStatus = __instance.status;
            __state.Before = __instance.Graduation_Date;
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls __instance, GirlDateCorrectionState __state)
        {
            if (!__state.HasValue
                || __state.InitialStatus == data_girls._status.announced_graduation
                || __instance == null
                || __instance.status != data_girls._status.depressed)
            {
                return;
            }

            ImmutableDateCorrectionRuntime.ApplyMonthsIfStillUnchanged(
                __instance,
                __state.Before,
                DateCompatibilityConstants.DepressionGraduationMonths);
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Graduate))]
    [HarmonyAfter(new[] { DateCompatibilityOwners.TelTraitsExpansion, DateCompatibilityOwners.GraduationRebalances })]
    internal static class GraduationRelatedImmutableDatePatch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(data_girls.girls __instance, out RelatedGraduationDateState __state)
        {
            __state = new RelatedGraduationDateState();
            if (__instance == null)
            {
                return;
            }

            data_girls.girls bestFriend = Relationships.GetBestFriend_Girl(__instance);
            AddTarget(__state, bestFriend, DateCompatibilityConstants.BestFriendGraduationMonths);

            Relationships._clique clique = __instance.GetClique();
            if (clique == null || clique.Members == null)
            {
                return;
            }

            foreach (data_girls.girls member in clique.Members)
            {
                if (member != null && member != __instance && member != bestFriend)
                {
                    AddTarget(__state, member, DateCompatibilityConstants.CliqueGraduationMonths);
                }
            }
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls __instance, RelatedGraduationDateState __state)
        {
            if (__instance == null || __instance.status != data_girls._status.graduated || __state == null)
            {
                return;
            }

            foreach (RelatedGraduationDateTarget target in __state.Targets)
            {
                ImmutableDateCorrectionRuntime.ApplyMonthsIfStillUnchanged(target.Girl, target.Before, target.Months);
            }
        }

        private static void AddTarget(RelatedGraduationDateState state, data_girls.girls girl, int months)
        {
            if (state == null || girl == null)
            {
                return;
            }

            state.Targets.Add(new RelatedGraduationDateTarget
            {
                Girl = girl,
                Before = girl.Graduation_Date,
                Months = months
            });
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Graduation_Date_Update))]
    internal static class GraduationDateUpdateWorkerRightsSnapshotPatch
    {
        [HarmonyPriority(Priority.High)]
        [HarmonyAfter(new[] { DateCompatibilityOwners.GraduationRebalances })]
        [HarmonyBefore(new[] { DateCompatibilityOwners.TelWorkerRights })]
        private static void Postfix(data_girls.girls __instance)
        {
            MethodBase method = ImmutableDateCorrectionRuntime.GraduationDateUpdateMethod();
            if (!ImmutableDateCorrectionRuntime.HasPatchOwner(method, DateCompatibilityOwners.TelWorkerRights))
            {
                return;
            }

            ImmutableDateCorrectionRuntime.CaptureBeforeWorkerRights(__instance);
        }
    }

    [HarmonyPatch(typeof(data_girls.girls), nameof(data_girls.girls.Graduation_Date_Update))]
    internal static class GraduationDateUpdateImmutableDatePatch
    {
        [HarmonyPriority(Priority.First)]
        [HarmonyBefore(new[] { DateCompatibilityOwners.GraduationRebalances })]
        private static void Prefix(data_girls.girls __instance, out GraduationDateUpdateCorrectionState __state)
        {
            __state = new GraduationDateUpdateCorrectionState();
            if (__instance == null)
            {
                return;
            }

            MethodBase method = ImmutableDateCorrectionRuntime.GraduationDateUpdateMethod();
            __state.HasValue = true;
            __state.Before = __instance.Graduation_Date;
            __state.GraduationRebalancesPresent = ImmutableDateCorrectionRuntime.HasPatchOwner(method, DateCompatibilityOwners.GraduationRebalances);
            __state.WorkerRightsPresent = ImmutableDateCorrectionRuntime.HasPatchOwner(method, DateCompatibilityOwners.TelWorkerRights);
            __state.ShouldApplyVanillaDelta = !__state.GraduationRebalancesPresent
                && ImmutableDateCorrectionRuntime.ShouldVanillaGraduationDateUpdateApply(__instance);
            __state.VanillaDeltaDays = __state.ShouldApplyVanillaDelta
                ? ImmutableDateCorrectionRuntime.ComputeVanillaGraduationDateUpdateDelta(__instance)
                : DateCompatibilityConstants.NoDelta;
        }

        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter(new[] { DateCompatibilityOwners.TelWorkerRights, DateCompatibilityOwners.TelTraitsExpansion, DateCompatibilityOwners.GraduationRebalances })]
        private static void Postfix(data_girls.girls __instance, GraduationDateUpdateCorrectionState __state)
        {
            if (!__state.HasValue || __instance == null)
            {
                return;
            }

            if (__state.GraduationRebalancesPresent)
            {
                ApplyWorkerRightsDeltaAfterGraduationRebalances(__instance, GetCurrentWorkerRightsDelta(__instance, __state));
                return;
            }

            ApplyVanillaAndWorkerRightsDeltas(__instance, __state, GetCurrentWorkerRightsDelta(__instance, __state));
        }

        private static int GetCurrentWorkerRightsDelta(data_girls.girls girl, GraduationDateUpdateCorrectionState state)
        {
            return state.WorkerRightsPresent
                ? ImmutableDateCorrectionRuntime.ComputeWorkerRightsDelta(girl)
                : DateCompatibilityConstants.NoDelta;
        }

        private static void ApplyWorkerRightsDeltaAfterGraduationRebalances(data_girls.girls girl, int workerRightsDeltaDays)
        {
            if (workerRightsDeltaDays == DateCompatibilityConstants.NoDelta)
            {
                ImmutableDateCorrectionRuntime.TryPopBeforeWorkerRights(girl, out _);
                return;
            }

            DateTime beforeWorkerRights;
            if (!ImmutableDateCorrectionRuntime.TryPopBeforeWorkerRights(girl, out beforeWorkerRights))
            {
                return;
            }

            ImmutableDateCorrectionRuntime.ApplyDaysIfStillUnchanged(girl, beforeWorkerRights, workerRightsDeltaDays);
        }

        private static void ApplyVanillaAndWorkerRightsDeltas(
            data_girls.girls girl,
            GraduationDateUpdateCorrectionState state,
            int workerRightsDelta)
        {
            int vanillaDelta = state.ShouldApplyVanillaDelta ? state.VanillaDeltaDays : DateCompatibilityConstants.NoDelta;
            if (vanillaDelta == DateCompatibilityConstants.NoDelta && workerRightsDelta == DateCompatibilityConstants.NoDelta)
            {
                ImmutableDateCorrectionRuntime.TryPopBeforeWorkerRights(girl, out _);
                return;
            }

            DateTime before = state.Before;
            DateTime current = girl.Graduation_Date;
            DateTime vanillaTarget = before.AddDays(vanillaDelta);
            DateTime workerRightsTarget = before.AddDays(workerRightsDelta);
            DateTime combinedTarget = before.AddDays(vanillaDelta + workerRightsDelta);

            if (ImmutableDateCorrectionRuntime.SameDate(current, combinedTarget))
            {
                ImmutableDateCorrectionRuntime.TryPopBeforeWorkerRights(girl, out _);
                return;
            }

            if (ImmutableDateCorrectionRuntime.SameDate(current, before))
            {
                ImmutableDateCorrectionRuntime.ApplyDaysIfStillUnchanged(girl, before, vanillaDelta + workerRightsDelta);
                ImmutableDateCorrectionRuntime.TryPopBeforeWorkerRights(girl, out _);
                return;
            }

            if (vanillaDelta != DateCompatibilityConstants.NoDelta
                && workerRightsDelta != DateCompatibilityConstants.NoDelta
                && ImmutableDateCorrectionRuntime.SameDate(current, vanillaTarget))
            {
                ImmutableDateCorrectionRuntime.ApplyDaysIfStillUnchanged(girl, vanillaTarget, workerRightsDelta);
                ImmutableDateCorrectionRuntime.TryPopBeforeWorkerRights(girl, out _);
                return;
            }

            if (vanillaDelta != DateCompatibilityConstants.NoDelta
                && workerRightsDelta != DateCompatibilityConstants.NoDelta
                && ImmutableDateCorrectionRuntime.SameDate(current, workerRightsTarget))
            {
                ImmutableDateCorrectionRuntime.ApplyDaysIfStillUnchanged(girl, workerRightsTarget, vanillaDelta);
            }

            ImmutableDateCorrectionRuntime.TryPopBeforeWorkerRights(girl, out _);
        }
    }
}
