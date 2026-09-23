using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Optional, reflection-only integration with current-head Tel Mod Library mods.
    /// SNLF must remain fully functional when none of these assemblies are installed.
    /// Gameplay coefficients/settings remain owned by the originating mod wherever it
    /// exposes a callable helper; SNLF only carries them across its replacement paths.
    /// </summary>
    internal static class TelModLibraryInterop
    {
        internal const string GoingViralOwner = "com.tel.goingviral";
        internal const string FanAttritionOwner = "com.tel.fanattrition";
        internal const string StaleTheaterOwner = "com.tel.staletheater";
        internal const string ExtendedSskOwner = "com.tel.extendedssk";
        internal const string UnofficialPatchOwner = "com.tel.unofficialpatch";
        internal const string MbtiOwner = "com.tel.mbtipersonalities";
        internal const string SisterGroupsOwner = "com.tel.sistergroups";
        internal const string TourStaminaOwner = "com.tel.tourstamina";
        internal const string TraitsFixOwner = "com.tel.traitsfix";
        internal const string TraitsExpansionOwner = "com.tel.traitsexpansion";

        private static readonly object Sync = new object();
        private static bool warnedStaleTheater;
        private static bool warnedExtendedSsk;
        private static bool warnedUnofficialConcert;
        private static bool warnedFanAttrition;
        private static bool warnedGoingViral;

        internal static bool IsLoaded(string owner)
        {
            return !string.IsNullOrEmpty(owner) && Harmony.HasAnyPatches(owner);
        }

        internal static bool FanAttritionLoaded
        {
            get { return IsLoaded(FanAttritionOwner); }
        }

        internal static bool UnofficialPatchLoaded
        {
            get { return IsLoaded(UnofficialPatchOwner); }
        }

        internal static bool TryGetStaleTheaterAttendanceMultiplier(
            Theaters._theater theater,
            out float multiplier)
        {
            multiplier = 0f;
            if (!IsLoaded(StaleTheaterOwner)) return false;
            try
            {
                Type type = AccessTools.TypeByName(
                    "StaleTheater.Theaters__theater_GetNumberOfVisitors");
                MethodInfo method = type == null ? null : AccessTools.Method(type, "Infix",
                    new Type[] { typeof(Theaters._theater) });
                if (method == null) throw new MissingMethodException(
                    "StaleTheater.Theaters__theater_GetNumberOfVisitors.Infix");
                multiplier = (float)method.Invoke(null, new object[] { theater });
                return true;
            }
            catch (Exception ex)
            {
                WarnOnce(ref warnedStaleTheater,
                    "Stale Theater Shows attendance helper could not be invoked; " +
                    "falling back to vanilla attendance coefficients.", ex);
                return false;
            }
        }

        internal static int GetExtendedSskResultLimit(int vanillaLimit)
        {
            if (!IsLoaded(ExtendedSskOwner)) return vanillaLimit;
            try
            {
                Type type = AccessTools.TypeByName("ExtendedSSK.SSK_GenerateResultsPatch");
                MethodInfo method = type == null ? null : AccessTools.Method(type, "Infix",
                    new Type[] { typeof(int) });
                if (method == null) throw new MissingMethodException(
                    "ExtendedSSK.SSK_GenerateResultsPatch.Infix");
                int limit = (int)method.Invoke(null, new object[] { vanillaLimit });
                return limit < 0 ? 0 : limit;
            }
            catch (Exception ex)
            {
                WarnOnce(ref warnedExtendedSsk,
                    "Extended SSK result-limit helper could not be invoked; using vanilla top 10.",
                    ex);
                return vanillaLimit;
            }
        }

        internal static bool TryGetUnofficialPatchConcertHype(
            SEvent_Concerts._concert._projectedValues projected,
            out float hype)
        {
            hype = 0f;
            if (!IsLoaded(UnofficialPatchOwner)) return false;
            try
            {
                Type type = AccessTools.TypeByName(
                    "UnofficialPatch.SEvent_Concerts__concert__projectedValues_GetRevenue");
                MethodInfo method = type == null ? null : AccessTools.Method(type, "Infix",
                    new Type[] { typeof(SEvent_Concerts._concert._projectedValues) });
                if (method == null) throw new MissingMethodException(
                    "UnofficialPatch concert GetRevenue.Infix");
                hype = (float)method.Invoke(null, new object[] { projected });
                return true;
            }
            catch (Exception ex)
            {
                WarnOnce(ref warnedUnofficialConcert,
                    "Unofficial Patch concert-hype helper could not be invoked; " +
                    "using the game's GetHype result.", ex);
                return false;
            }
        }

        /// <summary>
        /// Fan Attrition modifies the pre-demographic show audience base by a fatigue
        /// coefficient. Calling its helper with 1 obtains that coefficient without
        /// copying its difficulty/fatigue formula into SNLF.
        /// </summary>
        internal static float GetFanAttritionAudienceCoefficient(Shows._show show)
        {
            if (!FanAttritionLoaded) return 1f;
            try
            {
                Type type = AccessTools.TypeByName(
                    "FanAttrition.Shows__show_SetSales_Fatigue");
                MethodInfo method = type == null ? null : AccessTools.Method(type, "Infix",
                    new Type[] { typeof(Shows._show), typeof(float) });
                if (method == null) throw new MissingMethodException(
                    "FanAttrition.Shows__show_SetSales_Fatigue.Infix");
                float value = (float)method.Invoke(null, new object[] { show, 1f });
                return value;
            }
            catch (Exception ex)
            {
                WarnOnce(ref warnedFanAttrition,
                    "Fan Attrition show-fatigue helper could not be invoked; " +
                    "using the vanilla show audience coefficient.", ex);
                return 1f;
            }
        }

        /// <summary>
        /// Fan Attrition's current MC helper accepts Int32, while SNLF's replacement
        /// can legitimately produce an Int64 audience. Keep the mod's formula but do
        /// the final multiply through SNLF's wide numeric implementation.
        /// </summary>
        internal static long ApplyFanAttritionMcAudience(Shows._show show, long audience)
        {
            if (!FanAttritionLoaded || show == null || show.mc == null) return audience;
            try
            {
                float maximumFameBonus = 5f;
                Type utility = AccessTools.TypeByName("FanAttrition.Utility");
                FieldInfo field = utility == null ? null : AccessTools.Field(
                    utility, "MC_MAX_FAME_BONUS");
                if (field != null)
                    maximumFameBonus = Convert.ToSingle(field.GetValue(null));

                float fame = show.mc.fame;
                float coefficient = Mathf.Max(1f, 1f + fame * fame / 10f);
                if (fame >= 10f) coefficient += maximumFameBonus;
                return WideNumericRepair.RoundSingleProductToEven(
                    audience,
                    "Fan Attrition show MC audience modifier",
                    coefficient);
            }
            catch (Exception ex)
            {
                WarnOnce(ref warnedFanAttrition,
                    "Fan Attrition MC modifier could not be carried into SNLF's wide show path; " +
                    "using the unmodified audience.", ex);
                return audience;
            }
        }

        internal static bool TryGetGoingViralTarget(long baseFans, out long targetFans)
        {
            targetFans = baseFans;
            if (!IsLoaded(GoingViralOwner) || baseFans <= 0L) return false;
            try
            {
                Type type = AccessTools.TypeByName("GoingViral.TrendingManager");
                if (type == null) throw new TypeLoadException("GoingViral.TrendingManager");
                FieldInfo trendingField = AccessTools.Field(type, "trending");
                MethodInfo coeffMethod = AccessTools.Method(type, "GetTrendingCoeff",
                    Type.EmptyTypes);
                MethodInfo scaleMethod = AccessTools.Method(type, "ScaleLong",
                    new Type[] { typeof(long), typeof(float) });
                if (trendingField == null || coeffMethod == null || scaleMethod == null)
                    throw new MissingMemberException("Going Viral trending helpers");

                long trending = Convert.ToInt64(trendingField.GetValue(null));
                if (trending <= 0L) return false;
                float coefficient = (float)coeffMethod.Invoke(null, null);
                targetFans = (long)scaleMethod.Invoke(null,
                    new object[] { baseFans, coefficient });
                if (targetFans < baseFans) targetFans = baseFans;
                return true;
            }
            catch (Exception ex)
            {
                WarnOnce(ref warnedGoingViral,
                    "Going Viral helpers could not be invoked; SNLF will not synthesize a " +
                    "trending bonus on its wide show path.", ex);
                targetFans = baseFans;
                return false;
            }
        }

        private static void WarnOnce(ref bool flag, string message, Exception ex)
        {
            lock (Sync)
            {
                if (flag) return;
                flag = true;
            }
            Debug.LogWarning("[Save n Load Fixes] " + message +
                (ex == null ? string.Empty : " " + ex.GetType().Name + ": " + ex.Message));
        }
    }
}
