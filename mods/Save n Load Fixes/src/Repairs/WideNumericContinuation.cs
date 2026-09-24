using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// A33.2-A33.6 producer/consumer implementations. Public vanilla signatures
    /// remain unchanged; methods ending in Compatibility return only a clamped ABI
    /// mirror while the corresponding Int64 helper remains authoritative.
    /// </summary>
    internal static class WideNumericContinuation
    {
        private const long ExactSingleIntegerBoundary = 16777216L;
        private static readonly object PendingSync = new object();
        private static readonly Dictionary<int, TheaterDayPreflight> PendingTheaterDays =
            new Dictionary<int, TheaterDayPreflight>();
        private static readonly Dictionary<int, CafeRenderPreflight> PendingCafeDailyProfits =
            new Dictionary<int, CafeRenderPreflight>();
        [ThreadStatic]
        private static CafeRenderPreflight activeCafeRender;

        private static readonly MethodInfo TheaterGetAppealCoeff = AccessTools.Method(
            typeof(Theaters._theater), "GetAppealCoeff",
            new Type[] { typeof(Theaters._theater._subscriber) });
        private static readonly MethodInfo TheaterGetDayOffCoeff = AccessTools.Method(
            typeof(Theaters._theater), "GetDayOffCoeff", Type.EmptyTypes);
        private static readonly MethodInfo TheaterGetManzaiPenalty = AccessTools.Method(
            typeof(Theaters._theater), "GetManzaiPenalty", Type.EmptyTypes);
        private static readonly MethodInfo TheaterGetPriceCoeff = AccessTools.Method(
            typeof(Theaters._theater), "GetPriceCoeff", new Type[] { typeof(int) });
        private static readonly FieldInfo ResourcesDailyFansChanged = AccessTools.Field(
            typeof(resources), "onDailyFansChange");
        private static readonly FieldInfo ResourcesFansChanged = AccessTools.Field(
            typeof(resources), "onFansChange");
        private static readonly FieldInfo ResourcesResourceChanged = AccessTools.Field(
            typeof(resources), "onResourceChange");
        private static readonly FieldInfo ResourcesScandalPointsChanged = AccessTools.Field(
            typeof(resources), "onScandalPointsChange");
        private static readonly MethodInfo SinglesFameNewFansBaseCoeff = AccessTools.Method(
            typeof(singles), "GetFameNewFansBaseCoeff", Type.EmptyTypes);
        private static readonly MethodInfo ShowsGetBaseAudience = AccessTools.Method(
            typeof(Shows._show), "GetBaseAudience", Type.EmptyTypes);
        private static readonly MethodInfo ResourceDisplayOnResourceChangeLong = AccessTools.Method(
            typeof(ResourceDisplay), "OnResourceChange", new Type[] { typeof(long) });

        internal static void AddResource(
            resources.type type,
            long delta,
            resources.money onMoneyChange,
            resources.fans onFansChange,
            resources.scandalPoints onScandalPointsChange,
            resources.fame onFameChange,
            resources.resourceChanged onResourceChange)
        {
            delta = BuffMeWideNumericInterop.ApplyResourceDelta(type, delta);
            if (delta == 0L) return;
            long value = WideNumericRepair.Add(
                resources.Get(type, false), delta, "resources._Add(" + type + ")");
            if (value < 0L && (type == resources.type.buzz ||
                type == resources.type.fans || type == resources.type.fame ||
                type == resources.type.scandalPoints)) value = 0L;
            if (type == resources.type.buzz && value > 1000L) value = 1000L;
            long scandalTotal = type == resources.type.scandalPoints
                ? GetScandalPointsTotal(value) : 0L;
            resources.resource[(int)type] = value;
            switch (type)
            {
                case resources.type.money:
                    if (value < 0L) Stats.data.money.had_negative_cash_balance = true;
                    onMoneyChange(value);
                    break;
                case resources.type.fans:
                    BuffMeWideNumericInterop.BeginResourceFanDistribution();
                    try
                    {
                        resources.AddFans(delta, null, null, null);
                    }
                    finally
                    {
                        BuffMeWideNumericInterop.EndResourceFanDistribution();
                    }
                    onFansChange(value);
                    break;
                case resources.type.scandalPoints:
                    onScandalPointsChange(WideNumericMath.ClampToInt32(
                        scandalTotal));
                    break;
                case resources.type.fame:
                    onFameChange(WideNumericMath.ClampToInt32(value));
                    break;
            }
            onResourceChange();
        }

        internal static void SetResource(
            resources.type type,
            long value,
            resources.money onMoneySet,
            resources.fans onFansSet,
            resources.scandalPoints onScandalPointsSet,
            resources.fame onFameSet,
            resources.resourceChanged onResourceChange)
        {
            long scandalTotal = type == resources.type.scandalPoints
                ? GetScandalPointsTotal(value) : 0L;
            resources.resource[(int)type] = value;
            switch (type)
            {
                case resources.type.money:
                    onMoneySet(value);
                    break;
                case resources.type.fans:
                    onFansSet(value);
                    break;
                case resources.type.scandalPoints:
                    onScandalPointsSet(WideNumericMath.ClampToInt32(
                        scandalTotal));
                    break;
                case resources.type.fame:
                    onFameSet(WideNumericMath.ClampToInt32(value));
                    break;
            }
            onResourceChange();
        }

        internal static long GetScandalPointsTotal()
        {
            return GetScandalPointsTotal(
                resources.resource[(int)resources.type.scandalPoints]);
        }

        private static long GetScandalPointsTotal(long resourceValue)
        {
            long total = resourceValue;
            foreach (data_girls.girls girl in data_girls.girl)
            {
                if (girl.status == data_girls._status.graduated) continue;
                total = WideNumericRepair.Add(
                    total,
                    Mathf.RoundToInt(
                        girl.getParam(data_girls._paramType.scandalPoints).val),
                    "resources.GetScandalPointsTotal");
            }
            return total;
        }

        internal static void ReportScandalPointsChanged()
        {
            resources owner = Camera.main.GetComponent<mainScript>().Data.GetComponent<resources>();
            resources.scandalPoints callback = ResourcesScandalPointsChanged == null
                ? null : ResourcesScandalPointsChanged.GetValue(owner) as resources.scandalPoints;
            if (callback == null)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "resources.UpdateScandalPointsCounter could not bind its audited observer.");
                throw new MissingFieldException(typeof(resources).FullName,
                    "onScandalPointsChange");
            }
            callback(WideNumericMath.ClampToInt32(GetScandalPointsTotal()));
        }

        internal static int GetScandalPointsCompatibility()
        {
            return WideNumericMath.ClampToInt32(GetScandalPointsTotal());
        }

        internal static string GetScandalPointsTooltip()
        {
            string text = Language.Data["SCANDAL_POINTS"] + "\n" +
                mainScript.separator_no_linebreaks;
            bool hasGirlPoints = false;
            foreach (data_girls.girls girl in data_girls.girl)
            {
                if (girl.status == data_girls._status.graduated ||
                    girl.GetScandalPoints() == 0) continue;
                hasGirlPoints = true;
                text = string.Concat(new string[]
                {
                    text, "\n", girl.GetName(true), ": ",
                    ExtensionMethods.color(
                        ExtensionMethods.formatNumber(
                            girl.GetScandalPoints(), false, false),
                        mainScript.red)
                });
            }

            long groupPoints = resources.Get(resources.type.scandalPoints, false);
            if (groupPoints > 0L && hasGirlPoints) text += mainScript.separator;
            else if (groupPoints > 0L) text += "\n";
            if (groupPoints > 0L)
            {
                text = text + Language.Data["SP__GROUP_POINTS"] + ": " +
                    ExtensionMethods.color(
                        ExtensionMethods.formatNumber(groupPoints, false, false),
                        mainScript.red);
            }
            if (groupPoints > 0L || hasGirlPoints) text += mainScript.separator;
            else text += "\n";

            long exactTotal = GetScandalPointsTotal();
            text = text + Language.Data["TOTAL"] + ": " +
                ExtensionMethods.color(
                    ExtensionMethods.formatNumber(exactTotal, false, false),
                    exactTotal > 0L ? mainScript.red : mainScript.green);

            // These APIs are intentionally Int32 threshold tables. A monotonic clamp
            // preserves every stock result and keeps an out-of-range total on the
            // corresponding terminal side of each table.
            int compatibilityTotal = WideNumericMath.ClampToInt32(exactTotal);
            string effects = string.Empty;
            if (ScandalPoints.GetSingleSalesCoeff(compatibilityTotal) != 1f)
            {
                int number = Mathf.RoundToInt(
                    (1f - ScandalPoints.GetSingleSalesCoeff(compatibilityTotal)) * 100f);
                effects = effects + Language.Data["SP__SINGLES"] + ": " +
                    ExtensionMethods.color("-" +
                        ExtensionMethods.formatNumber(number, false, false) + "%",
                        mainScript.red);
            }
            if (ScandalPoints.GetBonusFansCoeff(compatibilityTotal) != 1f)
            {
                if (effects != string.Empty) effects += "\n";
                int number = Mathf.RoundToInt(
                    (1f - ScandalPoints.GetBonusFansCoeff(compatibilityTotal)) * 100f);
                effects = effects + Language.Data["SP__BONUS_FANS"] + ": " +
                    ExtensionMethods.color("-" +
                        ExtensionMethods.formatNumber(number, false, false) + "%",
                        mainScript.red);
            }
            if (ScandalPoints.GetConcertAttendance(compatibilityTotal) != 1f)
            {
                if (effects != string.Empty) effects += "\n";
                int number = Mathf.RoundToInt(
                    (1f - ScandalPoints.GetConcertAttendance(compatibilityTotal)) * 100f);
                effects = effects + Language.Data["SP__CONCERTS"] + ": " +
                    ExtensionMethods.color("-" +
                        ExtensionMethods.formatNumber(number, false, false) + "%",
                        mainScript.red);
            }
            if (ScandalPoints.GetLiabilityCoeff(compatibilityTotal) != 1m)
            {
                if (effects != string.Empty) effects += mainScript.separator;
                int number = (int)decimal.Round(
                    ScandalPoints.GetLiabilityCoeff(compatibilityTotal) * 100m);
                effects = effects + Language.Data["BIZP__LIABILITY"] + ": " +
                    ExtensionMethods.color("+" +
                        ExtensionMethods.formatNumber(number, false, false) + "%",
                        mainScript.red);
                effects = effects + "\n" + Language.Data["SP__LIAB_PENALTY"];
            }
            if (!ScandalPoints.Audition_CanGetGold(compatibilityTotal) ||
                !ScandalPoints.Audition_CanGetPlat(compatibilityTotal))
            {
                if (effects != string.Empty) effects += mainScript.separator;
                if (!ScandalPoints.Audition_CanGetGold(compatibilityTotal))
                    effects += ExtensionMethods.color(
                        Language.Data["SP__NO_GOLD"], mainScript.red);
                else if (!ScandalPoints.Audition_CanGetPlat(compatibilityTotal))
                    effects += ExtensionMethods.color(
                        Language.Data["SP__NO_PLAT"], mainScript.red);
            }
            if (effects != string.Empty) text = text + mainScript.separator + effects;
            return text;
        }

        internal static void CorrectScandalPointsLine(ScandalPoints_Line view)
        {
            if (view == null || view.PointsData == null || !view.PointsData.Group) return;
            long remaining = WideNumericRepair.Subtract(
                resources.Get(resources.type.scandalPoints, false),
                view.PointsData.ToRemove,
                "ScandalPoints_Line.Set group points");
            ExtensionMethods.SetText(view.Points,
                ExtensionMethods.color(
                    ExtensionMethods.formatNumber(remaining, false, false),
                    remaining == 0L ? mainScript.green : mainScript.red));
        }

        internal static bool ForwardExactResourceDisplay(ResourceDisplay view)
        {
            if (view == null ||
                (view.type != resources.type.scandalPoints &&
                 view.type != resources.type.fame)) return false;
            if (ResourceDisplayOnResourceChangeLong == null)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "ResourceDisplay.OnResourceChange(Int64) no longer matches the audited binding.");
                throw new MissingMethodException(
                    typeof(ResourceDisplay).FullName, "OnResourceChange(Int64)");
            }
            long exact = view.type == resources.type.scandalPoints
                ? GetScandalPointsTotal()
                : resources.Get(view.type, true);
            ResourceDisplayOnResourceChangeLong.Invoke(view, new object[] { exact });
            return true;
        }

        internal static void RunDatingJobSearch()
        {
            long fanChance = resources.GetFansTotal(null) / 10000L;
            if (fanChance > 90L) fanChance = 90L;
            long scandalChance = WideNumericRepair.Multiply(
                resources.Get(resources.type.scandalPoints, true), 10L,
                "Dating.JobSearch_1 scandal chance");

            bool success = mainScript.chance((float)fanChance);
            variables.Set("he_first_job_ent_search", success ? "good" : "bad");
            success = scandalChance < 100L &&
                !mainScript.chance(WideNumericMath.ClampToInt32(scandalChance));
            variables.Set("he_first_job_out_search", success ? "good" : "bad");
            string message = Date_GroupTalk.GetMessage(
                Date_GroupTalk._message._category.bad_job,
                Date_GroupTalk._message._category.NONE,
                Date_GroupTalk._message._category.NONE,
                Date_GroupTalk._message._category.NONE,
                Date_GroupTalk._message._category.NONE);
            variables.Set("bad_job", message);
        }

        internal static float GetTourFamePenalty(int level)
        {
            long fame = resources.Get(resources.type.fame, true);
            long required = WideNumericRepair.Multiply(
                level, 2L, "SEvent_Tour.GetFamePenalty required fame");
            long deficit = WideNumericRepair.Subtract(
                required, fame, "SEvent_Tour.GetFamePenalty deficit");
            if (fame == 0L || deficit >= 9L) return 0.1f;
            if (deficit <= 0L) return 1f;
            float result = 1f - 0.12f * (float)deficit;
            return result <= 0f ? 0.1f : result;
        }

        internal static void CorrectShowReleaseFame(Show_Release view)
        {
            if (view == null || view.Fame_Stars == null) return;
            view.Fame_Stars.GetComponent<Stars>().SetValueWithEmpty(
                WideNumericMath.ClampToInt32(
                    resources.Get(resources.type.fame, true)), 10);
        }
        private static readonly FieldInfo TourPopupCountryTour = AccessTools.Field(
            typeof(Tour_Popup_Country), "Tour");
        private static readonly FieldInfo TourPopupCountryCountry = AccessTools.Field(
            typeof(Tour_Popup_Country), "Country");
        private static readonly MethodInfo TourPopupAttendanceString = AccessTools.Method(
            typeof(Tour_Popup_Country), "AttendanceString",
            new Type[] { typeof(int), typeof(int) });
        private static readonly FieldInfo TourStarCountry = AccessTools.Field(
            typeof(Tour_Star), "TourCountry");
        private static readonly MethodInfo SummerGamesSetCheckbox = AccessTools.Method(
            typeof(Summer_Games_Button), "SetCheckbox",
            new Type[] { typeof(GameObject), typeof(bool?) });
        private static readonly MethodInfo SummerGamesSetTitle = AccessTools.Method(
            typeof(Summer_Games_Button), "SetTitle",
            new Type[] { typeof(GameObject), typeof(bool), typeof(string) });
        private static readonly FieldInfo AgencyRoomIdCounter = AccessTools.Field(
            typeof(agency), "roomIDCounter");
        private static readonly MethodInfo RelationshipAddToQueue = AccessTools.Method(
            typeof(Relationships_Player), "AddToQueue",
            new Type[] { typeof(Relationships_Player._type), typeof(data_girls.girls),
                typeof(int), typeof(int) });

        internal static long GetRentPerFloor(int floorId)
        {
            agency agency = Camera.main.GetComponent<mainScript>().Data.GetComponent<agency>();
            agency._floor floor = agency.GetFloor(floorId);
            if (floor == null)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "resources.GetRentPerFloor could not resolve floor " +
                    floorId.ToString(CultureInfo.InvariantCulture) + ".");
                throw new InvalidOperationException("Unknown floor ID.");
            }
            if (floor.FirstFloor) return 0L;

            long start = staticVars.rentPerBlock;
            if (staticVars.IsHard() && floorId > 2)
                start = WideNumericRepair.Multiply(start, 2L, "resources.GetRentPerFloor hard coefficient");
            if (floorId > 10 && !staticVars.IsEasy())
                start = WideNumericRepair.Multiply(start, 3L, "resources.GetRentPerFloor high-floor coefficient");
            else if (floorId > 5 && !staticVars.IsEasy())
                start = WideNumericRepair.Multiply(start, 2L, "resources.GetRentPerFloor mid-floor coefficient");

            if (floorId <= 0) return 0L;
            long previous = 0L;
            long current = start;
            for (int index = 1; index < floorId; index++)
            {
                long next = WideNumericRepair.Add(previous, current,
                    "resources.GetRentPerFloor Fibonacci");
                previous = current;
                current = next;
            }
            return current;
        }

        internal static long GetActivityPerformanceReward(
            int level, bool forceHard, float bonus)
        {
            return FloorSingleCompatible(
                Activities.GetPerformanceMoneyPerLevel(level, forceHard), bonus,
                "Activities performance reward");
        }

        internal static long GetActivityPromotionReward(float bonus)
        {
            int level = Activities.GetActivityLevel(Activity._type.promotion);
            return FloorSingleCompatible(
                Activities.GetPromotionFansPerLevel(level, false), bonus,
                "Activities promotion reward");
        }

        internal static bool ActivityPerformanceNeedsWidePath(Activities owner)
        {
            int basis = Activities.GetPerformanceMoneyPerLevel(
                owner.GetActivity(Activity._type.performance).lvl, false);
            long exact = FloorSingleCompatible(basis, Activities.GetBonus(true),
                "Activities.Performance path selection");
            return basis < -ExactSingleIntegerBoundary ||
                basis > ExactSingleIntegerBoundary ||
                exact < int.MinValue || exact > int.MaxValue;
        }

        internal static bool ActivityPromotionNeedsWidePath(Activities owner)
        {
            int basis = Activities.GetPromotionFansPerLevel(
                owner.GetActivity(Activity._type.promotion).lvl, false);
            long exact = FloorSingleCompatible(basis, Activities.GetBonus(false),
                "Activities.Promotion path selection");
            return basis < -ExactSingleIntegerBoundary ||
                basis > ExactSingleIntegerBoundary ||
                exact < int.MinValue || exact > int.MaxValue;
        }

        internal static void PerformActivityWide(Activities owner)
        {
            PreflightActivityCounters(Activity._type.performance);
            long reward = GetActivityPerformanceReward(
                owner.GetActivity(Activity._type.performance).lvl, false,
                Activities.GetBonus(true));
            List<data_girls.girls> active = data_girls.GetActiveGirls(null);
            long idolShare = 0L;
            if (active.Count > 0)
            {
                idolShare = reward >= int.MinValue && reward <= int.MaxValue
                    ? Mathf.RoundToInt((float)reward / active.Count)
                    : WideNumericRepair.DivideRoundToEven(reward, active.Count,
                        "Activities.Performance idol earnings share");
            }
            WideNumericRepair.Add(resources.Money(),
                BuffMeWideNumericInterop.PreviewResourceDelta(resources.type.money, reward),
                "Activities.Performance resource preflight");
            foreach (data_girls.girls girl in active)
                WideNumericRepair.Add(girl.Earnings_CurrentMonth, idolShare,
                    "Activities.Performance idol earnings preflight");

            Stats.data.activities.performance++;
            Stats.data.activities.performance_year++;
            Stats.AddActivity(Activity._type.performance);
            resources.Add(resources.type.money, reward);
            owner.LockButtons(1, 3f, true);
            owner.GetComponent<Floats>().Create(Floats.type.money, reward, Input.mousePosition);
            if (active.Count == 0) return;
            float stamina = owner.GetStaminaCost();
            foreach (data_girls.girls girl in active)
            {
                float value = stamina;
                if (girl.trait == traits._trait._type.Weak_vocal_chords) value *= 2f;
                girl.addParam(data_girls._paramType.physicalStamina, value, false);
                girl.Earn(idolShare);
            }
        }

        internal static void PromoteActivityWide(Activities owner)
        {
            PreflightActivityCounters(Activity._type.promotion);
            long reward = GetActivityPromotionReward(Activities.GetBonus(false));
            // Wide fan allocation completes all numeric planning before its first
            // bucket commit, so the activity counters/history remain unchanged on failure.
            AddFansEqually(reward, (List<data_girls.girls>)null);
            Stats.data.activities.promotion++;
            Stats.data.activities.promotion_year++;
            Stats.AddActivity(Activity._type.promotion);
            owner.LockButtons(1, 3f, true);
            owner.GetComponent<Floats>().Create("+" + reward.ToString(CultureInfo.InvariantCulture),
                mainScript.green32, Input.mousePosition, 1f, 0f, 16);
            data_girls.AddStamina(-3f, null, false);
        }

        internal static int GetActivityFansCompatibility(float bonus)
        {
            return WideNumericMath.ClampToInt32(GetActivityPromotionReward(bonus));
        }

        internal static void CorrectActivityTooltip(
            Activities owner, Activity._type type, ref string result)
        {
            if (string.IsNullOrEmpty(result)) return;
            if (type == Activity._type.performance && Activities.GetBonus(true) > 1f)
            {
                int basis = Activities.GetPerformanceMoneyPerLevel(
                    owner.GetActivity(type).lvl, false);
                int vanilla = unchecked(Mathf.FloorToInt((float)basis *
                    Activities.GetBonus(true)) - basis);
                long exact = WideNumericRepair.Subtract(
                    GetActivityPerformanceReward(owner.GetActivity(type).lvl, false,
                        Activities.GetBonus(true)), basis,
                    "Activities.GetTooltip performance bonus");
                result = result.Replace(
                    ExtensionMethods.color(ExtensionMethods.formatMoney(vanilla, false, false),
                        mainScript.green),
                    ExtensionMethods.color(ExtensionMethods.formatMoney(exact, false, false),
                        mainScript.green));
            }
            else if (type == Activity._type.promotion && Activities.GetBonus(false) > 1f)
            {
                int basis = Activities.GetPromotionFansPerLevel(
                    owner.GetActivity(type).lvl, false);
                int vanilla = unchecked(Mathf.FloorToInt((float)basis *
                    Activities.GetBonus(false)) - basis);
                long exact = WideNumericRepair.Subtract(
                    GetActivityPromotionReward(Activities.GetBonus(false)), basis,
                    "Activities.GetTooltip promotion bonus");
                result = result.Replace(
                    ExtensionMethods.color(ExtensionMethods.formatNumber(vanilla, false, false),
                        mainScript.green),
                    ExtensionMethods.color(ExtensionMethods.formatNumber(exact, false, false),
                        mainScript.green));
            }
        }

        internal static string GetActivityLevelUpText(Activities._activity activity)
        {
            if (activity.type == Activity._type.performance)
                return "+" + FormatMoneyExact(
                    GetActivityPerformanceReward(activity.lvl, false,
                        Activities.GetBonus(true)));
            if (activity.type == Activity._type.promotion)
            {
                int basis = Activities.GetPromotionFansPerLevel(activity.lvl, false);
                if (basis == 1)
                    return "+" + ExtensionMethods.formatNumber(1, false, true) + " " +
                        Language.Data["FAN"].ToLower();
                long reward = FloorSingleCompatible(
                    basis,
                    Activities.GetBonus(false), "Activities._activity.GetLevelUp");
                return "+" + ExtensionMethods.formatNumber(reward, false, true) + " " +
                    Language.Data["FANS"].ToLower();
            }
            return "+" + Activities.GetSpaHeal() + Language.Data["PT"] + " " +
                Language.Data["PHYSICAL_STAMINA"].ToLower();
        }

        private static long FloorSingleCompatible(long value, float coefficient, string context)
        {
            float product = (float)value * coefficient;
            if (value >= -ExactSingleIntegerBoundary && value <= ExactSingleIntegerBoundary &&
                !float.IsNaN(product) && !float.IsInfinity(product) &&
                product >= int.MinValue && product <= int.MaxValue)
                return Mathf.FloorToInt(product);
            return WideNumericRepair.FloorSingleProduct(value, context, coefficient);
        }

        internal static long GetRoomRent(agency._type type, int floorId)
        {
            if (agency.isEmptyRoom(type)) return 0L;
            return WideNumericRepair.Multiply(
                GetRentPerFloor(floorId), agency.roomSpace(type),
                "resources.GetRoomRent");
        }

        internal static long GetTotalRent(bool addFloat)
        {
            agency agency = Camera.main.GetComponent<mainScript>().Data.GetComponent<agency>();
            long total = 0L;
            List<agency._room> floatRooms = addFloat
                ? new List<agency._room>()
                : null;
            foreach (agency._floor floor in agency.floors)
            {
                long perFloor = GetRentPerFloor(floor.FloorID);
                if (perFloor == 0L) continue;
                foreach (agency._room room in floor.floor)
                {
                    if (agency.isEmptyRoom(room.type)) continue;
                    long roomRent = WideNumericRepair.Multiply(
                        perFloor, agency.roomSpace(room.type), "resources.Money_Rent room");
                    total = WideNumericRepair.Add(total, roomRent, "resources.Money_Rent total");
                    if (addFloat) floatRooms.Add(room);
                }
            }
            if (variables.Get("FUJI_3_RENT") == "true")
            {
                total = RoundSingleCompatible(total, 0.8f, "resources.Money_Rent discount");
            }
            if (addFloat)
                foreach (agency._room room in floatRooms)
                    room.addFloat(Floats.type.icon_money, "", false, null, 0f, 1f, 0f, null);
            return total;
        }

        internal static long GetStaffSalary()
        {
            long total = 0L;
            foreach (staff._staff person in staff.Staff)
                total = WideNumericRepair.Add(total, person.GetSalary(), "resources.Money_StaffSalary");
            return total;
        }

        internal static long GetStaffSeverance(staff._staff person)
        {
            return WideNumericRepair.CalculateStaffSeverance(person);
        }

        internal static bool CanFireStaffWithSeverance(staff._staff person)
        {
            if (person == null) return false;
            if (person.UniqueType != staff._staff._unique_type.NONE ||
                person.type == staff._type.player ||
                person.type == staff._type.player_female) return false;
            long severance = GetStaffSeverance(person);
            return staticVars.IsEasy() || severance <= resources.Get(resources.type.money, true);
        }

        internal static void FireStaffWithSeverance(staff._staff person)
        {
            if (!CanFireStaffWithSeverance(person)) return;
            long severance = GetStaffSeverance(person);
            long debit = WideNumericRepair.Subtract(0L, severance,
                "staff._staff.Fire_Severance debit");
            WideNumericRepair.Add(resources.Money(),
                BuffMeWideNumericInterop.PreviewResourceDelta(resources.type.money, debit),
                "staff._staff.Fire_Severance money preflight");
            if (person.room != null) person.room.RemoveStaffer();
            staff.Staff.Remove(person);
            resources.Add(resources.type.money, debit);
            if (person.Update != null) person.Update();
        }

        internal static string GetStaffFireSeveranceTooltip(staff._staff person)
        {
            if (person == null ||
                person.UniqueType != staff._staff._unique_type.NONE ||
                person.type == staff._type.player ||
                person.type == staff._type.player_female) return string.Empty;
            long severance = GetStaffSeverance(person);
            string text;
            if (severance == 0L)
            {
                text = Language.Insert("STAFF__DAYS_TO_FIRE", new string[]
                {
                    (30 - (staticVars.dateTime - person.HireDate).Days).ToString(
                        CultureInfo.InvariantCulture)
                });
            }
            else
            {
                text = Language.Data["STAFF__SEVERANCE"] + ": " +
                    FormatMoneyExact(severance);
            }
            text += mainScript.separator;
            return text + ExtensionMethods.color(
                Language.Data["STAFF__FIRE_HINT"], mainScript.grey_light);
        }

        internal static void RenderOfficeSeveranceExact(
            ContextMenu_Office view, ContextMenuController controller)
        {
            if (view == null || view.Fire_Severance == null || controller == null ||
                controller.room == null || controller.room.staffer == null) return;
            ExtensionMethods.SetText(
                view.Fire_Severance.GetComponent<ContextMenuButton>().text,
                FormatMoneyExact(GetStaffSeverance(controller.room.staffer)));
        }

        internal static void RenderDanceSeveranceExact(CM_Dance view, staff._staff person)
        {
            if (view == null || view.Fire_Severance == null || person == null) return;
            ExtensionMethods.SetText(
                view.Fire_Severance.GetComponent<ContextMenuButton>().text,
                FormatMoneyExact(GetStaffSeverance(person)));
        }

        internal static long PreflightStaffFireDialoguePayment()
        {
            if (!Staff_Fire.Accepted || !Staff_Fire.IsSeverance) return 0L;
            long exact = WideNumericRepair.CalculateStaffFireSeverance();
            int compatibility = WideNumericRepair.CalculateSeveranceCompatibility();
            long remainder = WideNumericRepair.Subtract(exact, compatibility,
                "Staff_Fire.DoComplete compatibility remainder");
            if (remainder == 0L) return 0L;
            long debit = WideNumericRepair.Subtract(0L, exact,
                "Staff_Fire.DoComplete exact debit");
            WideNumericRepair.Add(resources.Money(),
                BuffMeWideNumericInterop.PreviewResourceDelta(resources.type.money, debit),
                "Staff_Fire.DoComplete exact money preflight");
            return remainder;
        }

        internal static void CompleteStaffFireDialoguePayment(long remainder)
        {
            if (remainder == 0L) return;
            resources.Add(resources.type.money, WideNumericRepair.Subtract(
                0L, remainder, "Staff_Fire.DoComplete remainder debit"));
        }

        internal static void CorrectStaffFireDialogueAmount(
            string formula, ActiveDialogueController controller)
        {
            if (!string.Equals(formula, "staff_firing_mid3", StringComparison.Ordinal) ||
                !Staff_Fire.IsSeverance || controller == null ||
                controller.activeNode == null) return;
            long exact = WideNumericRepair.CalculateStaffFireSeverance();
            int compatibility = WideNumericRepair.CalculateSeveranceCompatibility();
            if (exact == compatibility) return;
            string narrow = FormatMoneyExact(compatibility);
            string wide = FormatMoneyExact(exact);
            controller.activeNode.val = controller.activeNode.val.Replace(narrow, wide);
        }

        internal static long GetGirlSalary()
        {
            long total = 0L;
            foreach (data_girls.girls girl in data_girls.girl)
                if (girl.status != data_girls._status.graduated)
                    total = WideNumericRepair.Add(total, girl.salary, "resources.Money_GirlsSalary");
            return total;
        }

        internal static long GetWeeklyExpenses()
        {
            return GetWeeklyExpenses(true);
        }

        private static long GetWeeklyExpenses(bool emitRentFloats)
        {
            long total = GetTotalRent(false);
            total = WideNumericRepair.Add(total, GetStaffSalary(), "resources.Money_WeeklyExpenses staff");
            total = WideNumericRepair.Add(total, GetGirlSalary(), "resources.Money_WeeklyExpenses idols");
            total = WideNumericRepair.Add(total, GetTotalLoanPayment(), "resources.Money_WeeklyExpenses loans");
            if (emitRentFloats) EmitRentFloats();
            return total;
        }

        private static void EmitRentFloats()
        {
            agency owner = Camera.main.GetComponent<mainScript>().Data.GetComponent<agency>();
            foreach (agency._floor floor in owner.floors)
                foreach (agency._room room in floor.floor)
                    if (!agency.isEmptyRoom(room.type))
                        room.addFloat(Floats.type.icon_money, "", false, null, 0f, 1f, 0f, null);
        }

        internal static void SetGeneratedBusinessProposalPayment(
            business._proposal proposal, int basePayment, float generationCoefficient)
        {
            if (proposal == null) return;
            long exact = FloorSingleCompatible(
                basePayment,
                generationCoefficient,
                "business.GenerateProposal payment");
            WideNumericState.SetBusinessProposalBasePayment(proposal, exact);
        }

        internal static void SetGeneratedBusinessProposalFans(
            business._proposal proposal, int baseFans, float generationCoefficient)
        {
            if (proposal == null) return;
            long exact = FloorSingleCompatible(
                baseFans,
                generationCoefficient,
                "business.GenerateProposal new fans");
            WideNumericState.SetBusinessProposalBaseFans(proposal, exact);
        }

        internal static long GetBusinessProposalPayment(business._proposal proposal)
        {
            if (proposal == null) return 0L;
            long basis = WideNumericState.GetBusinessProposalBasePayment(proposal);
            float girlCoefficient = proposal.girl == null ? 1f : proposal.GetGirlCoeff(proposal.girl);
            return RoundSingleProductCompatible(
                basis,
                "business._proposal.payment",
                girlCoefficient,
                proposal.negotiationCoeff);
        }

        internal static int GetBusinessProposalPaymentCompatibility(business._proposal proposal)
        {
            return WideNumericMath.ClampToInt32(GetBusinessProposalPayment(proposal));
        }

        internal static void SetBusinessProposalPaymentCompatibility(
            business._proposal proposal, int value)
        {
            WideNumericState.SetBusinessProposalBasePayment(proposal, value);
        }

        internal static long GetBusinessProposalFans(business._proposal proposal)
        {
            if (proposal == null) return 0L;
            long basis = WideNumericState.GetBusinessProposalBaseFans(proposal);
            float girlCoefficient = proposal.girl == null ? 1f : proposal.GetGirlCoeff(proposal.girl);
            return RoundSingleProductCompatible(
                basis,
                "business._proposal.newFans",
                girlCoefficient,
                proposal.negotiationCoeff);
        }

        internal static int GetBusinessProposalFansCompatibility(business._proposal proposal)
        {
            return WideNumericMath.ClampToInt32(GetBusinessProposalFans(proposal));
        }

        internal static void SetBusinessProposalFansCompatibility(
            business._proposal proposal, int value)
        {
            WideNumericState.SetBusinessProposalBaseFans(proposal, value);
        }

        internal static void CompleteBusinessActiveProposalAdd(
            business owner, business._proposal source, int priorCount)
        {
            if (owner == null || source == null || owner.ActiveProposals == null ||
                owner.ActiveProposals.Count != priorCount + 1)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "business.AddActiveProposal did not append exactly one active contract.");
                return;
            }
            business.active_proposal active = owner.ActiveProposals[owner.ActiveProposals.Count - 1];
            if (active == null || active.Girl != source.girl || active.Type != source.type ||
                active.Skill != source.skill)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "business.AddActiveProposal appended a contract that does not match its source proposal.");
                return;
            }
            WideNumericState.SetBusinessContractPayment(active, GetBusinessProposalPayment(source));
            WideNumericState.SetBusinessContractFans(active, GetBusinessProposalFans(source));
        }

        internal static long GetBusinessWeeklyProfit(business instance)
        {
            long total = 0L;
            foreach (business.active_proposal proposal in instance.ActiveProposals)
                total = WideNumericRepair.Add(total,
                    WideNumericState.GetBusinessContractPayment(proposal),
                    "business.GetTotalWeeklyProfit");
            return total;
        }

        internal static long GetBusinessWeeklyBuzz(business instance)
        {
            long total = 0L;
            foreach (business.active_proposal proposal in instance.ActiveProposals)
                total = WideNumericRepair.Add(total, proposal.Buzz_per_week,
                    "business.GetTotalWeeklyBuzz");
            return total;
        }

        internal static long GetBusinessWeeklyFame(business instance)
        {
            long total = 0L;
            foreach (business.active_proposal proposal in instance.ActiveProposals)
                total = WideNumericRepair.Add(total, proposal.Fame_per_week,
                    "business.GetTotalWeeklyFame");
            return total;
        }

        internal static long GetBusinessDailyBuzz(business instance)
        {
            return DivideBusinessWeeklyValue(GetBusinessWeeklyBuzz(instance),
                "resources.Buzz_Daily");
        }

        internal static long GetBusinessDailyFame(business instance)
        {
            return DivideBusinessWeeklyValue(GetBusinessWeeklyFame(instance),
                "resources.Fame_Daily");
        }

        private static long DivideBusinessWeeklyValue(long value, string context)
        {
            if (value >= -ExactSingleIntegerBoundary && value <= ExactSingleIntegerBoundary)
                return Mathf.RoundToInt((float)value / 7f);
            return WideNumericRepair.DivideRoundToEven(value, 7L, context);
        }

        internal static long GetDailyBusinessProfit(resources instance)
        {
            long weekly = instance.Money_BusinessContracts();
            if (weekly >= -ExactSingleIntegerBoundary && weekly <= ExactSingleIntegerBoundary)
                return Mathf.RoundToInt((float)weekly / 7f);
            return WideNumericRepair.DivideRoundToEven(weekly, 7L,
                "resources.Money_DailyProfit");
        }

        internal static int DailyBusinessProfitCompatibility(resources instance)
        {
            return WideNumericMath.ClampToInt32(GetDailyBusinessProfit(instance));
        }

        internal static void ResourcesOnNewDay(resources instance)
        {
            long money = GetDailyBusinessProfit(instance);
            business business = Camera.main.GetComponent<mainScript>().Data.GetComponent<business>();
            long buzz = WideNumericRepair.Subtract(GetBusinessDailyBuzz(business),
                instance.GetDailyBuzzReduction(), "resources.OnNewDay buzz reduction");
            long fame = GetBusinessDailyFame(business);
            // Preflight every resource mutation before changing the first one.
            WideNumericRepair.Add(resources.Money(),
                BuffMeWideNumericInterop.PreviewResourceDelta(resources.type.money, money),
                "resources.OnNewDay money preflight");
            WideNumericRepair.Add(resources.Get(resources.type.buzz, false), buzz,
                "resources.OnNewDay buzz preflight");
            WideNumericRepair.Add(resources.Get(resources.type.fame, false), fame,
                "resources.OnNewDay fame preflight");
            // Fan Attrition intentionally carries yesterday's churn in FansChange until
            // its OnNewDay Postfix consumes it. Its vanilla transpiler cannot affect
            // this replacement body, so preserve the value when that mod is present.
            if (!TelModLibraryInterop.FanAttritionLoaded)
                resources.FansChange = 0L;
            instance.AddMoney(money);
            resources.Add(resources.type.buzz, buzz);
            resources.Add(resources.type.fame, fame);
        }

        internal static void ResourcesOnNewWeek(resources instance)
        {
            long expenses = GetWeeklyExpenses(false);
            long delta = WideNumericRepair.Subtract(0L, expenses,
                "resources.OnNewWeek expense negation");
            WideNumericRepair.Add(resources.Money(),
                BuffMeWideNumericInterop.PreviewResourceDelta(resources.type.money, delta),
                "resources.OnNewWeek money preflight");
            EmitRentFloats();
            instance.AddMoney(delta);
            if (resources.GetFansTotal(resources.fanType.hardcore) >= 1000L)
                Achievements.Unlock(Achievements.ID.ACH_THOUSAND_FANS);
        }

        internal static void ResourcesDailyFansChange(resources instance)
        {
            float fame = (float)resources.GetFameLevel() + resources.GetFameProgress();
            if (fame > 10f) fame = 10f;
            else if (fame < 0.05f) fame = 0.05f;
            float buzz = (float)resources.Get(resources.type.buzz, true);
            float change = (buzz / 10f - fame * 2f) / 10f;
            const float divisor = 40f;
            long before = resources.GetFansTotal(null);
            FanMutationPlan plan = new FanMutationPlan();
            foreach (resources._fan fan in resources.Fans)
            {
                long people = fan.GetNumberOfPeople();
                if (people == 0L) continue;
                float opinion = fan.GetOpinion() * 2f - 1f;
                float coefficient = (change + opinion) / divisor;
                if (coefficient > 0f) continue;
                long delta = people >= -ExactSingleIntegerBoundary &&
                        people <= ExactSingleIntegerBoundary
                    ? Mathf.CeilToInt((float)people * coefficient)
                    : WideNumericRepair.CeilingSingleProduct(people,
                        "resources.DailyFansChange", coefficient);
                plan.Add(fan, delta,
                    "resources.DailyFansChange bucket preflight");
            }
            long after = 0L;
            foreach (resources._fan fan in resources.Fans)
                after = WideNumericRepair.Add(after, plan.GetProjected(fan),
                    "resources.DailyFansChange projected total");
            long fanChange = WideNumericRepair.Subtract(after, before,
                "resources.DailyFansChange total");
            plan.CommitMutations();
            resources.FansChange = fanChange;
            resources.resourceChanged daily = ResourcesDailyFansChanged == null
                ? null : ResourcesDailyFansChanged.GetValue(instance) as resources.resourceChanged;
            resources.fans changed = ResourcesFansChanged == null
                ? null : ResourcesFansChanged.GetValue(instance) as resources.fans;
            if (daily != null) daily();
            if (changed != null) changed(after);
        }

        internal static void PreflightBusinessWeeklyEarnings(business instance)
        {
            Dictionary<data_girls.girls, long> projected =
                new Dictionary<data_girls.girls, long>();
            foreach (business.active_proposal proposal in instance.ActiveProposals)
            {
                if (proposal == null || proposal.Girl == null) continue;
                long current;
                if (!projected.TryGetValue(proposal.Girl, out current))
                    current = proposal.Girl.Earnings_CurrentMonth;
                projected[proposal.Girl] = WideNumericRepair.Add(current,
                    WideNumericState.GetBusinessContractPayment(proposal),
                    "business.AddWeeklyEarnings");
            }
        }

        internal static void AddBusinessWeeklyEarnings(business instance)
        {
            PreflightBusinessWeeklyEarnings(instance);
            foreach (business.active_proposal proposal in instance.ActiveProposals)
            {
                if (proposal == null || proposal.Girl == null) continue;
                proposal.Girl.Earn(WideNumericState.GetBusinessContractPayment(proposal));
            }
        }

        internal static void CorrectBusinessPopupPayment(Business_Popup view, business._proposal proposal)
        {
            if (view == null || view.rightCol == null || proposal == null) return;
            long exact = GetBusinessProposalPayment(proposal);
            int mirror = WideNumericMath.ClampToInt32(exact);
            if (exact == mirror) return;
            TMPro.TextMeshProUGUI text = view.rightCol.GetComponent<TMPro.TextMeshProUGUI>();
            if (text == null || string.IsNullOrEmpty(text.text)) return;
            string before = ExtensionMethods.color(
                mainScript.yen + ExtensionMethods.formatNumber(mirror, false, false),
                mainScript.green);
            string after = ExtensionMethods.color(
                mainScript.yen + ExtensionMethods.formatNumber(exact, false, false),
                mainScript.green);
            int index = text.text.IndexOf(before, StringComparison.Ordinal);
            if (index >= 0)
                text.text = text.text.Substring(0, index) + after +
                    text.text.Substring(index + before.Length);
        }

        internal static void CorrectBusinessContractPaymentLine(
            Contracts_Line view, business.active_proposal proposal)
        {
            if (view == null || view.Payment == null || proposal == null) return;
            ExtensionMethods.SetText(view.Payment, ExtensionMethods.formatMoney(
                WideNumericState.GetBusinessContractPayment(proposal), false, false));
        }

        internal static void CorrectBusinessPopupFans(Business_Popup view, business._proposal proposal)
        {
            if (view == null || view.rightCol == null || proposal == null) return;
            long exact = GetBusinessProposalFans(proposal);
            int mirror = WideNumericMath.ClampToInt32(exact);
            if (exact == mirror) return;
            TMPro.TextMeshProUGUI text = view.rightCol.GetComponent<TMPro.TextMeshProUGUI>();
            if (text == null || string.IsNullOrEmpty(text.text)) return;
            string before = ExtensionMethods.color(
                ExtensionMethods.formatNumber(mirror, false, false), mainScript.green);
            string after = ExtensionMethods.color(
                ExtensionMethods.formatNumber(exact, false, false), mainScript.green);
            int index = text.text.IndexOf(before, StringComparison.Ordinal);
            if (index >= 0)
                text.text = text.text.Substring(0, index) + after +
                    text.text.Substring(index + before.Length);
        }

        internal static void CorrectBusinessContractFansLine(
            Contracts_Line view, business.active_proposal proposal)
        {
            if (view == null || view.NewFans == null || proposal == null) return;
            ExtensionMethods.SetText(view.NewFans, ExtensionMethods.formatNumber(
                WideNumericState.GetBusinessContractFans(proposal), false, false));
        }

        internal static void AddBusinessProposalFans(business._proposal proposal)
        {
            if (proposal == null || proposal.girl == null) return;
            long exactFans = GetBusinessProposalFans(proposal);
            if (exactFans <= 0L) return;
            resources.fanType dominant = business.GetDominantDemographic(proposal.skill);
            resources.fanType other = business.GetOtherDemographic(dominant);
            long first = RoundSingleCompatible(exactFans, 0.75f,
                "business.AddFans proposal dominant");
            long second = RoundSingleCompatible(exactFans, 0.25f,
                "business.AddFans proposal secondary");
            FanMutationPlan plan = new FanMutationPlan();
            PlanGirlFans(plan, proposal.girl, first, dominant);
            PlanGirlFans(plan, proposal.girl, second, other);
            plan.CommitMutations();
            plan.RunCallbacks();
        }

        internal static void AddBusinessWeeklyFans(business.active_proposal proposal)
        {
            if (proposal == null || proposal.Girl == null) return;
            long exactFans = WideNumericState.GetBusinessContractFans(proposal);
            if (exactFans <= 0L) return;
            resources.fanType dominant = business.GetDominantDemographic(proposal.Skill);
            resources.fanType other = business.GetOtherDemographic(dominant);
            long first = RoundSingleCompatible(exactFans, 0.75f,
                "business.AddFans active proposal dominant");
            long second = RoundSingleCompatible(exactFans, 0.25f,
                "business.AddFans active proposal secondary");
            FanMutationPlan plan = new FanMutationPlan();
            PlanGirlFans(plan, proposal.Girl, first, dominant);
            PlanGirlFans(plan, proposal.Girl, second, other);
            plan.CommitMutations();
            plan.RunCallbacks();
        }

        internal static bool BusinessWeeklyFansNeedWidePath(business instance)
        {
            if (instance == null || instance.ActiveProposals == null) return false;
            foreach (business.active_proposal proposal in instance.ActiveProposals)
            {
                if (proposal == null) continue;
                long exact = WideNumericState.GetBusinessContractFans(proposal);
                if (exact != proposal.Fans_per_week || NeedsWideFanPath(exact))
                    return true;
            }
            return false;
        }

        internal static void DoBusinessWeeklyFansWide(business instance)
        {
            if (instance == null || instance.ActiveProposals == null) return;
            foreach (business.active_proposal proposal in instance.ActiveProposals)
            {
                if (proposal == null || proposal.Girl == null) continue;
                long exactFans = WideNumericState.GetBusinessContractFans(proposal);
                if (exactFans > 0L)
                    AddGirlFans(proposal.Girl, exactFans, null);
                if (proposal.Fame_per_week > 0)
                    proposal.Girl.addParam(data_girls._paramType.famePoints,
                        (float)proposal.Fame_per_week, false);
                proposal.Girl.AddTrainingPoints(proposal.Skill, 1f);
            }
        }

        internal static void CorrectReleasedShowFans(Show_Released_Button view)
        {
            if (view == null || view.Show == null || view.param_fans == null) return;
            List<long> fans = WideNumericState.GetShowFans(view.Show);
            if (fans == null || fans.Count == 0) return;
            int episode = view.Show.episodeCount - 1;
            if (episode < 0) episode = 0;
            if (episode >= fans.Count) episode = fans.Count - 1;
            view.param_fans.GetComponent<Show_Param>().SetVal(
                "+" + ExtensionMethods.formatNumber(fans[episode], false, false));
        }

        internal static int GetSingleFanSatisfaction(singles._single single)
        {
            if (single == null || resources.GetFansTotal(null) == 0L) return 0;
            long satisfied = 0L;
            long unsatisfied = 0L;
            foreach (resources._fan fan in resources.Fans)
            {
                long people = fan.GetNumberOfPeople();
                if (single.IsSatisfied(fan.gender, fan.hardcoreness, fan.age))
                    satisfied = WideNumericRepair.Add(satisfied, people,
                        "singles.ReleaseData_FanSatisfaction satisfied");
                else
                    unsatisfied = WideNumericRepair.Add(unsatisfied, people,
                        "singles.ReleaseData_FanSatisfaction unsatisfied");
            }
            long total = WideNumericRepair.Add(satisfied, unsatisfied,
                "singles.ReleaseData_FanSatisfaction total");
            if (total <= 0L) return 0;
            decimal value = ((decimal)satisfied * 100m) / (decimal)total;
            int rounded = (int)Math.Round(value, 0, MidpointRounding.ToEven);
            if (rounded < 0) return 0;
            if (rounded > 100) return 100;
            return rounded;
        }

        internal static string GetBusinessLiabilityPreview(business._data data, bool nextLevel)
        {
            if (data == null || !data.liability) return string.Empty;
            int level = data.GetLevel(true) - 1;
            if (data.payment == null || level < 0 || level >= data.payment.Count)
                return string.Empty;
            Func<int, long> liability = index =>
            {
                long value = WideNumericRepair.Multiply((long)data.payment[index], 2L,
                    "business._data.StringLiability doubled payment");
                if (data.duration != 0)
                {
                    value = WideNumericRepair.Multiply(value, (long)data.duration,
                        "business._data.StringLiability duration");
                    value = WideNumericRepair.Multiply(value, 4L,
                        "business._data.StringLiability weeks per month");
                }
                return value;
            };
            long current = liability(level);
            string result = Language.Data["BIZ__LIABILITY"] + ": " +
                ExtensionMethods.color(ExtensionMethods.formatMoney(current, false, false),
                    mainScript.red);
            if (!nextLevel || level + 1 >= data.payment.Count ||
                data.payment[level] == data.payment[level + 1])
                return result + "\n";
            long next = liability(level + 1);
            return result + " -> " +
                ExtensionMethods.color(ExtensionMethods.formatMoney(next, false, false),
                    mainScript.red) + "\n";
        }

        internal static void RecordBusinessTopPayment(business._proposal proposal)
        {
            if (proposal == null) return;
            WideNumericState.RecordBusinessTopPayment(proposal,
                GetBusinessProposalPayment(proposal));
        }

        internal static long RoundRivalGrowth(long value, float coefficient)
        {
            return RoundSingleCompatible(value, coefficient, "Rivals.OnNewMonth growth");
        }

        internal static void PreflightRivalMonth()
        {
            for (int index = 0; index < Rivals.Groups.Count; index++)
            {
                Rivals._group group = Rivals.Groups[index];
                if (group == null || group.IsDead) continue;
                if (index >= 3 && !group.IsRival &&
                    !(group.IsRising && group.Fans >= 1000000L))
                    RoundSingleCompatible(group.Fans, 2f,
                        "Rivals.OnNewMonth maximum fan growth preflight");
                if (!group.IsRival && !group.IsPhantasm)
                    RoundSingleCompatible(group.Fans, 1.2f,
                        "Rivals.OnNewMonth maximum sales growth preflight");
            }
        }

        internal static long GetTotalLoanPayment()
        {
            long total = 0L;
            foreach (loans._loan loan in loans.Loans)
                if (loan.Active)
                    total = WideNumericRepair.Add(total, WideNumericState.GetLoanPayment(loan),
                        "loans.GetTotalPaymentPerWeek");
            return total;
        }

        internal static long GetLoanInterest(loans._loan loan)
        {
            float coefficient = (float)loan.InterestRate / 100f;
            return RoundSingleCompatible(loan.Amount, coefficient, "loans._loan.GetInterest");
        }

        internal static long GetLoanTotalAmount(loans._loan loan)
        {
            return WideNumericRepair.Add(loan.Amount, GetLoanInterest(loan),
                "loans._loan.GetTotalAmount");
        }

        internal static long GetLoanDebt(loans._loan loan)
        {
            if (staticVars.dateTime > loan.EndDate || !loan.Active) return 0L;
            int totalDays = (loan.EndDate - loan.StartDate).Days;
            int elapsedDays = (staticVars.dateTime - loan.StartDate).Days;
            if (totalDays <= 0)
            {
                WideNumericRepair.LatchInvariantFailure("loans._loan.GetDebt has a nonpositive duration.");
                throw new InvalidOperationException("Loan duration is nonpositive.");
            }
            long remainingDays = (long)totalDays - elapsedDays;
            return WideNumericMath.RoundRatioToEven(
                GetLoanTotalAmount(loan), remainingDays, totalDays);
        }

        internal static void RecalculateLoanPayment(loans._loan loan)
        {
            int weeks = loan.GetNumberOfWeeks();
            if (weeks <= 0)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "loans._loan.RecalcPaymentPerWeek has a nonpositive week count.");
                throw new InvalidOperationException("Loan week count is nonpositive.");
            }
            long payment = WideNumericRepair.DivideRoundToEven(
                GetLoanTotalAmount(loan), weeks, "loans._loan.RecalcPaymentPerWeek");
            WideNumericState.SetLoanPayment(loan, payment);
        }

        internal static void RenderLoanLinePayment(Loans_Line view, loans._loan loan)
        {
            if (view == null || loan == null) return;
            ExtensionMethods.SetText(view.Weekly_Payment,
                ExtensionMethods.formatMoney(WideNumericState.GetLoanPayment(loan),
                    false, false, false));
        }

        internal static void RenderLoanPopupDetails(Loans_Popup view)
        {
            if (view == null || view.Loan == null) return;
            loans._loan loan = view.Loan;
            string value = Language.Data["LOANS__CURRENT_BALANCE"] + ": " +
                ExtensionMethods.formatMoney(resources.Money(), false, false, true);
            value += mainScript.separator;
            value = value + Language.Data["LOANS__DAYS_TO_GET"] + ": " +
                ExtensionMethods.color(
                    ExtensionMethods.formatNumber(loan.GetDaysToDevelop(), false, false),
                    mainScript.green);
            value = string.Concat(value, "\n", Language.Data["LOANS__INTEREST_RATE"],
                ": ", ExtensionMethods.color(loan.GetInterestRate() + "%", mainScript.green));
            value = string.Concat(value, "\n", Language.Data["LOANS__INTEREST"],
                ": ", ExtensionMethods.color(
                    ExtensionMethods.formatMoney(GetLoanInterest(loan), false, false, false),
                    mainScript.red));
            value = string.Concat(value, "\n", Language.Data["LOANS__WEEKLY_PAYMENT"],
                ": ", ExtensionMethods.color(
                    ExtensionMethods.formatMoney(WideNumericState.GetLoanPayment(loan),
                        false, false, false), mainScript.red));
            ExtensionMethods.SetText(view.Details, value);
        }

        internal static string AddWeeklyTooltipLine(
            string languageKey,
            long value,
            ref long total)
        {
            total = WideNumericRepair.Add(total, value,
                "tooltip_money weekly aggregate");
            return Language.Data[languageKey] + ": " + FormatWeeklyMoney(value);
        }

        /// <summary>
        /// Formats an already-authoritative Int64 money value without any float or
        /// Int32 round-trip.  This deliberately handles Int64.MinValue as well,
        /// unlike vanilla formatMoney(long), whose `number *= -1` cannot represent
        /// the positive magnitude of Int64.MinValue.
        /// </summary>
        internal static string FormatMoneyExact(long value, bool color = false)
        {
            bool negative = value < 0L;
            ulong magnitude = negative
                ? (ulong)(-(value + 1L)) + 1UL
                : (ulong)value;
            string text = (negative ? "-" : string.Empty) + mainScript.yen +
                magnitude.ToString("N0", CultureInfo.InvariantCulture);
            if (!color) return text;
            return ExtensionMethods.color(text, negative ? mainScript.red : mainScript.green);
        }

        private static string FormatWeeklyMoney(long value)
        {
            string text = value > 0L ? "+" : string.Empty;
            text += FormatMoneyExact(value) + " " + Language.Data["PER_WEEK"];
            if (value > 0L) return ExtensionMethods.color(text, mainScript.green);
            if (value < 0L) return ExtensionMethods.color(text, mainScript.red);
            return text;
        }

        internal static long GetTheaterSubscriptionRevenue(Theaters._theater theater)
        {
            return WideNumericRepair.Multiply(
                WideNumericState.GetTheaterSubscribers(theater), theater.Subscription_Price,
                "Theaters._theater.GetSubRevenue");
        }

        internal static long GetTheaterLastWeekEarning()
        {
            long total = 0L;
            foreach (Theaters._theater theater in Theaters.Theaters_)
            {
                int count = 0;
                for (int index = theater.Stats.Count - 1; index >= 0 && count < 6; index--, count++)
                    total = WideNumericRepair.Add(total, theater.Stats[index].Revenue,
                        "Theaters.GetLastWeekEarning");
            }
            return total;
        }

        /// <summary>
        /// Money tooltip value for theaters.  Vanilla GetLastWeekEarning only sees
        /// ticket-sale stat rows, while streaming is credited separately on day 1
        /// of each month.  The rest of the money tooltip is normalized to a weekly
        /// cadence, and Idol Manager itself treats a month as four weeks when it
        /// turns monthly idol earnings into weekly averages, so normalize current
        /// theater subscription income by the same /4 convention.
        /// </summary>
        internal static long GetTheaterWeeklyIncomeForDisplay()
        {
            // Unofficial Patch owns the effective weekly-tooltip contract: seven
            // ticket days plus subscription revenue normalized by 4.35 weeks/month.
            // Calling the public method lets its Postfix compose with SNLF's wide
            // six-day base and with Stale Theater Shows' GetSubRevenue Postfix.
            if (TelModLibraryInterop.UnofficialPatchLoaded)
                return Theaters.GetLastWeekEarning();

            long ticketIncome = GetTheaterLastWeekEarning();
            long monthlyStreamingIncome = 0L;
            foreach (Theaters._theater theater in Theaters.Theaters_)
            {
                if (theater == null || !theater.AreSubsUnlocked()) continue;
                monthlyStreamingIncome = WideNumericRepair.Add(
                    monthlyStreamingIncome, theater.GetSubRevenue(),
                    "tooltip_money.Theater monthly streaming aggregate");
            }
            long weeklyStreamingIncome = WideNumericRepair.DivideRoundToEven(
                monthlyStreamingIncome, 4L,
                "tooltip_money.Theater monthly-to-weekly streaming normalization");
            return WideNumericRepair.Add(ticketIncome, weeklyStreamingIncome,
                "tooltip_money.Theater weekly ticket+streaming total");
        }

        internal static long GetTheaterAverageRevenue(Theaters._theater theater)
        {
            return GetTheaterAverageRevenueCore(theater, false);
        }

        internal static long GetEffectiveTheaterAverageRevenue(Theaters._theater theater)
        {
            return GetTheaterAverageRevenueCore(
                theater, TelModLibraryInterop.UnofficialPatchLoaded);
        }

        private static long GetTheaterAverageRevenueCore(
            Theaters._theater theater, bool ignoreDaysOff)
        {
            if (theater == null || theater.Stats == null || theater.Stats.Count == 0)
                return 0L;
            int daysToCheck = Math.Min(7, theater.Stats.Count);
            bool safe = true;
            float vanillaTotal = 0f;
            long exactTotal = 0L;
            int counted = 0;
            for (int index = theater.Stats.Count - 1;
                index >= theater.Stats.Count - daysToCheck; index--)
            {
                Theaters._theater._stat stat = theater.Stats[index];
                if (ignoreDaysOff && stat.Schedule.Type ==
                    Theaters._theater._schedule._type.day_off)
                    continue;
                long revenue = stat.Revenue;
                if (revenue < -ExactSingleIntegerBoundary || revenue > ExactSingleIntegerBoundary)
                    safe = false;
                vanillaTotal += (float)revenue;
                exactTotal = WideNumericRepair.Add(exactTotal, revenue,
                    "Theaters._theater.GetAvgRevenue");
                counted++;
            }
            if (counted == 0) return 0L;
            if (safe && exactTotal >= -ExactSingleIntegerBoundary &&
                exactTotal <= ExactSingleIntegerBoundary)
                return Mathf.RoundToInt(vanillaTotal / (float)counted);
            return WideNumericRepair.DivideRoundToEven(exactTotal, counted,
                "Theaters._theater.GetAvgRevenue");
        }

        internal static void RenderTheaterPricing(Theater_Popup view)
        {
            Theaters._theater theater = Theater_Popup.Theater;
            if (view == null || theater == null) return;
            ExtensionMethods.SetText(view.Pricing_Revenue,
                ExtensionMethods.formatMoney(GetEffectiveTheaterAverageRevenue(theater),
                    false, false, false));
            if (theater.AreSubsUnlocked())
            {
                ExtensionMethods.SetText(view.Pricing_Subs,
                    ExtensionMethods.formatNumber(
                        WideNumericState.GetTheaterSubscribers(theater), false, false));
                ExtensionMethods.SetText(view.Pricing_Sub_Revenue,
                    ExtensionMethods.formatMoney(
                        theater.GetSubRevenue(), false, false, false));
            }
        }

        internal static void CorrectAgencyRoomTooltip(
            agency owner,
            agency._type type,
            ref string result)
        {
            if (owner == null || result == null) return;
            agency._floor floor = owner.GetSelectedFloor();
            if (floor == null || floor.FirstFloor) return;
            long rent = GetRoomRent(type, floor.FloorID);
            if (rent >= int.MinValue && rent <= int.MaxValue) return;
            string label = Language.Data["AGENCY__RENT"] + ": ";
            int index = result.LastIndexOf(label, StringComparison.Ordinal);
            if (index < 0)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "agency.GetRoomTooltip no longer contains its audited rent suffix.");
                throw new InvalidOperationException("Room tooltip rent suffix is missing.");
            }
            result = result.Substring(0, index) + label + ExtensionMethods.color(
                ExtensionMethods.formatMoney(rent, false, false, false) + " " +
                    Language.Data["PER_WEEK"], mainScript.red);
        }

        internal static int GetTheaterVisitors(Theaters._theater theater)
        {
            float coefficient;
            Groups._group group = theater.GetGroup();
            Theaters._theater._schedule schedule = theater.GetSchedule();
            long fans = schedule.FanType_Everyone
                ? WideNumericRepair.CalculateGroupFansByType(group, (resources.fanType?)null)
                : WideNumericRepair.CalculateGroupFansByType(group,
                    new resources.fanType?(schedule.FanType));
            if (!TelModLibraryInterop.TryGetStaleTheaterAttendanceMultiplier(
                theater, out coefficient))
            {
                coefficient = 1f;
                if (schedule.FanType_Everyone &&
                    theater.Doing_Now == Theaters._theater._schedule._type.manzai)
                    coefficient = 0.1f;
                else if (schedule.FanType_Everyone) coefficient = 0.2f;
                else if (schedule.FanType == resources.fanType.casual) coefficient = 0.3f;
                else if (schedule.FanType == resources.fanType.teen) coefficient = 0.5f;
                else if (schedule.FanType != resources.fanType.hardcore) coefficient = 0.75f;
                if (!schedule.FanType_Everyone &&
                    theater.Doing_Now == Theaters._theater._schedule._type.manzai &&
                    schedule.FanType != resources.fanType.hardcore)
                    coefficient *= 0.75f;
            }
            if (theater.Ticket_Price > 40000) return 0;
            if (TheaterGetPriceCoeff == null)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Theaters._theater.GetPriceCoeff no longer matches the audited shape.");
                throw new MissingMethodException(typeof(Theaters._theater).FullName,
                    "GetPriceCoeff");
            }
            coefficient *= (float)TheaterGetPriceCoeff.Invoke(theater,
                new object[] { theater.Ticket_Price });
            long eligible = RoundSingleCompatible(fans, coefficient,
                "Theaters._theater.GetNumberOfVisitors eligible fans");
            long visitors;
            if (eligible >= -ExactSingleIntegerBoundary && eligible <= ExactSingleIntegerBoundary)
            {
                LinearFunction._function function = new LinearFunction._function();
                function.Init(0f, 0f, 20000f, 100f);
                visitors = Mathf.RoundToInt(function.GetY((float)eligible));
            }
            else
            {
                visitors = WideNumericRepair.DivideRoundToEven(eligible, 200L,
                    "Theaters._theater.GetNumberOfVisitors attendance curve");
            }
            if (visitors > theater.GetCapacity()) return theater.GetCapacity();
            if (visitors < 0L) return 0;
            return WideNumericRepair.ToInt32Exact(visitors,
                "Theaters._theater.GetNumberOfVisitors result");
        }

        internal static void PreflightTheaterDay()
        {
            long total = 0L;
            lock (PendingSync) PendingTheaterDays.Clear();
            foreach (Theaters._theater theater in Theaters.Theaters_)
            {
                if (theater == null || theater.Stats == null || theater.Stats.Count > 90)
                {
                    WideNumericRepair.LatchInvariantFailure(
                        "Theaters.CompleteDay encountered an invalid theater/stat history.");
                    throw new InvalidOperationException("Invalid theater stat history.");
                }
                TheaterDayPreflight preflight = new TheaterDayPreflight
                {
                    StatCount = theater.Stats.Count,
                    PreviousLastStat = theater.Stats.Count == 0
                        ? null : theater.Stats[theater.Stats.Count - 1],
                    ExpectedDate = ExtensionMethods.ToDataString(staticVars.dateTime)
                };
                for (int index = 0; index < theater.Stats.Count; index++)
                {
                    Theaters._theater._stat stat = theater.Stats[index];
                    if (stat == null)
                    {
                        WideNumericRepair.LatchInvariantFailure(
                            "Theaters.CompleteDay encountered a null historical stat row.");
                        throw new InvalidOperationException("Null theater stat row.");
                    }
                    preflight.ExistingStatSubscribers.Add(
                        WideNumericState.GetTheaterStatSubscribers(theater, index, stat));
                }
                if (theater.Doing_Now == Theaters._theater._schedule._type.manzai ||
                    theater.Doing_Now == Theaters._theater._schedule._type.performance)
                    total = WideNumericRepair.Add(total,
                        WideNumericRepair.CalculateTicketSales(theater),
                        "Theaters.CompleteDay ticket total");
                if (staticVars.dateTime.Day == 1)
                    total = WideNumericRepair.Add(total, theater.GetSubRevenue(),
                        "Theaters.CompleteDay subscription total");
                if (theater.AreSubsUnlocked())
                    preflight.SubscriberPlan = CalculateTheaterSubscriberPlan(theater);
                lock (PendingSync) PendingTheaterDays.Add(theater.ID, preflight);
            }
            if (total > 0L)
                WideNumericRepair.Add(resources.Money(),
                    BuffMeWideNumericInterop.PreviewResourceDelta(resources.type.money, total),
                    "Theaters.CompleteDay resource-credit preflight");
        }

        private sealed class TheaterSubscriberUpdate
        {
            internal Theaters._theater._subscriber Subscriber;
            internal int Ordinal;
            internal long Value;
        }

        private sealed class TheaterSubscriberPlan
        {
            internal readonly List<TheaterSubscriberUpdate> Updates =
                new List<TheaterSubscriberUpdate>();
            internal long TotalDelta;
        }

        private sealed class TheaterDayPreflight
        {
            internal int StatCount;
            internal Theaters._theater._stat PreviousLastStat;
            internal string ExpectedDate;
            internal readonly List<long> ExistingStatSubscribers = new List<long>();
            internal TheaterSubscriberPlan SubscriberPlan;
            internal bool SubscriberPlanApplied;
        }

        internal static int AddTheaterSubscribers(Theaters._theater theater)
        {
            TheaterDayPreflight preflight;
            lock (PendingSync) PendingTheaterDays.TryGetValue(theater.ID, out preflight);
            TheaterSubscriberPlan plan = preflight == null
                ? CalculateTheaterSubscriberPlan(theater)
                : preflight.SubscriberPlan;
            if (plan == null || (preflight != null && preflight.SubscriberPlanApplied))
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Theaters._theater.GetNewSubscribers did not match its A33 daily preflight.");
                throw new InvalidOperationException(
                    "Theater subscriber update/preflight mismatch.");
            }
            foreach (TheaterSubscriberUpdate update in plan.Updates)
                WideNumericState.SetTheaterSubscriber(theater, update.Subscriber,
                    update.Ordinal, update.Value);
            if (preflight != null)
            {
                lock (PendingSync) preflight.SubscriberPlanApplied = true;
            }
            return WideNumericMath.ClampToInt32(plan.TotalDelta);
        }

        private static TheaterSubscriberPlan CalculateTheaterSubscriberPlan(
            Theaters._theater theater)
        {
            if (TheaterGetAppealCoeff == null || TheaterGetDayOffCoeff == null ||
                TheaterGetManzaiPenalty == null || TheaterGetPriceCoeff == null)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Theater subscriber helper methods no longer match the audited shape.");
                throw new MissingMethodException("Theater subscriber helper method.");
            }
            float dayOff = (float)TheaterGetDayOffCoeff.Invoke(theater, null);
            float manzai = (float)TheaterGetManzaiPenalty.Invoke(theater, null);
            float price = (float)TheaterGetPriceCoeff.Invoke(theater,
                new object[] { theater.Subscription_Price });
            TheaterSubscriberPlan plan = new TheaterSubscriberPlan();
            Groups._group group = theater.GetGroup();
            for (int index = 0; index < theater.Subscribers.Count; index++)
            {
                Theaters._theater._subscriber subscriber = theater.Subscribers[index];
                float coefficient = subscriber.hardcoreness == resources.fanType.hardcore ? 0.1f : 0.01f;
                coefficient *= (float)TheaterGetAppealCoeff.Invoke(theater, new object[] { subscriber });
                if (subscriber.age == resources.fanType.adult) coefficient *= 1.2f;
                else if (subscriber.age == resources.fanType.teen) coefficient *= 0.8f;
                coefficient *= dayOff;
                if (subscriber.hardcoreness == resources.fanType.casual) coefficient *= manzai;
                coefficient *= price;

                long groupFans = WideNumericRepair.CalculateGroupFansByType(
                    group, subscriber.gender, subscriber.hardcoreness, subscriber.age);
                long target = RoundSingleCompatible(groupFans, coefficient,
                    "Theaters._theater.GetNewSubscribers target");
                long current = WideNumericState.GetTheaterSubscriber(theater, subscriber, index);
                if (current < 0L)
                {
                    WideNumericRepair.LatchInvariantFailure(
                        "Theaters._theater.GetNewSubscribers encountered a negative subscriber bucket.");
                    throw new InvalidOperationException("Theater subscriber bucket is negative.");
                }
                long difference = WideNumericRepair.Subtract(target, current,
                    "Theaters._theater.GetNewSubscribers target-current");
                long delta = RoundSingleCompatible(difference, 0.035f,
                    "Theaters._theater.GetNewSubscribers step");
                if (delta == 0L && difference > 0L) delta = 1L;
                else if (delta == 0L && difference < 0L) delta = -1L;
                if (delta < -current) delta = -current;
                long next = WideNumericRepair.Add(current, delta,
                    "Theaters._theater.GetNewSubscribers bucket");
                plan.TotalDelta = WideNumericRepair.Add(plan.TotalDelta, delta,
                    "Theaters._theater.GetNewSubscribers total");
                plan.Updates.Add(new TheaterSubscriberUpdate
                {
                    Subscriber = subscriber,
                    Ordinal = index,
                    Value = next
                });
            }
            return plan;
        }

        internal static void AssociateTheaterStatsAfterDay()
        {
            Dictionary<int, TheaterDayPreflight> pending;
            lock (PendingSync)
            {
                pending = new Dictionary<int, TheaterDayPreflight>(PendingTheaterDays);
                PendingTheaterDays.Clear();
            }
            if (pending.Count != Theaters.Theaters_.Count)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Theaters.CompleteDay changed the theater set after its A33 preflight.");
                return;
            }
            foreach (Theaters._theater theater in Theaters.Theaters_)
            {
                TheaterDayPreflight preflight;
                if (theater == null || !pending.TryGetValue(theater.ID, out preflight))
                {
                    WideNumericRepair.LatchInvariantFailure(
                        "Theaters.CompleteDay could not match a theater to its A33 preflight.");
                    return;
                }
                if (preflight.SubscriberPlan != null && !preflight.SubscriberPlanApplied)
                {
                    WideNumericRepair.LatchInvariantFailure(
                        "Theaters.CompleteDay skipped its preflighted subscriber update.");
                    return;
                }
                int expectedCount = preflight.StatCount >= 90
                    ? preflight.StatCount : preflight.StatCount + 1;
                if (theater.Stats == null || theater.Stats.Count != expectedCount ||
                    theater.Stats.Count == 0)
                {
                    WideNumericRepair.LatchInvariantFailure(
                        "Theaters.CompleteDay did not produce its preflighted stat-list shape.");
                    return;
                }
                int ordinal = theater.Stats.Count - 1;
                Theaters._theater._stat stat = theater.Stats[ordinal];
                long delta = preflight.SubscriberPlan == null
                    ? 0L : preflight.SubscriberPlan.TotalDelta;
                if (stat == null || ReferenceEquals(stat, preflight.PreviousLastStat) ||
                    !string.Equals(ExtensionMethods.ToDataString(stat.Date),
                        preflight.ExpectedDate, StringComparison.Ordinal) ||
                    stat.Subscribers != WideNumericMath.ClampToInt32(delta))
                {
                    WideNumericRepair.LatchInvariantFailure(
                        "Theaters.CompleteDay appended a stat row that failed its A33 identity witness.");
                    return;
                }
                List<long> finalSeries = new List<long>();
                int first = preflight.StatCount >= 90 ? 1 : 0;
                for (int index = first;
                    index < preflight.ExistingStatSubscribers.Count; index++)
                    finalSeries.Add(preflight.ExistingStatSubscribers[index]);
                finalSeries.Add(delta);
                WideNumericState.ReplaceTheaterStatSeries(theater, finalSeries);
            }
        }

        internal static void StatsOnNewWeek()
        {
            long money = resources.Money();
            long moneyChange = Stats.data.money.total_money_per_week.Count == 0
                ? money
                : WideNumericRepair.Subtract(money,
                    Stats.data.money.total_money_per_week[Stats.data.money.total_money_per_week.Count - 1],
                    "Stats.OnNewWeek money change");
            long fans = resources.GetFansTotal(null);
            List<long> totals = WideNumericState.GetStatsTotals();
            long fanChange = totals.Count == 0
                ? fans
                : WideNumericRepair.Subtract(fans, totals[totals.Count - 1],
                    "Stats.OnNewWeek fan change");

            // All arithmetic is complete before any list is changed.
            Stats.data.money.money_change_per_week.Add(moneyChange);
            Stats.data.money.total_money_per_week.Add(money);
            WideNumericState.AppendStats(fans, fanChange);
        }

        internal static long GetStatsTotalIncome(int numberOfWeeks)
        {
            long total = 0L;
            int weeks = numberOfWeeks;
            if (weeks > Stats.data.money.money_change_per_week.Count)
                weeks = Stats.data.money.money_change_per_week.Count;
            for (int index = Stats.data.money.money_change_per_week.Count - 1;
                index >= 0 && weeks > 0; index--, weeks--)
            {
                long value = Stats.data.money.money_change_per_week[index];
                total = WideNumericRepair.Add(total, value, "Stats.data.money.GetTotalIncome");
            }
            return total;
        }

        internal static void SetStoryCh3Target()
        {
            long fans = resources.GetFansTotal(null);
            long target;
            if (fans < 100000L) target = 100000L;
            else if (fans < 500000L) target = 500000L;
            else if (fans < 1000000L) target = 1000000L;
            else
            {
                long additions = WideNumericRepair.Multiply(
                    GetActivityPromotionReward(1f), 30L,
                    "tasks._story_data.Set_Ch3_Aya_Fans monthly additions");
                target = WideNumericRepair.Add(fans,
                    WideNumericRepair.Multiply(additions, 3L,
                        "tasks._story_data.Set_Ch3_Aya_Fans addition coefficient"),
                    "tasks._story_data.Set_Ch3_Aya_Fans target");
                target = RoundToMultiple(target, 100000L,
                    "tasks._story_data.Set_Ch3_Aya_Fans rounding");
            }
            WideNumericState.SetStoryCh3(target);
        }

        internal static void AddSummerGamesTask(string taskId)
        {
            if (string.Equals(taskId, "ch4_1", StringComparison.Ordinal))
            {
                tasks.Story_Data.ch4_display_task = true;
                tasks.Story_Data.ch4_deadline = ExtensionMethods.ToDataString(
                    staticVars.dateTime.AddDays(60.0));
            }
            else if (string.Equals(taskId, "ch4_2", StringComparison.Ordinal))
            {
                WideNumericState.SetStoryCh4Scandal(
                    resources.Get(resources.type.scandalPoints, true));
            }
            else if (string.Equals(taskId, "ch4_3", StringComparison.Ordinal))
            {
                tasks.Story_Data.ch4_chart_unlocked = true;
            }
            else if (string.Equals(taskId, "ch4_4", StringComparison.Ordinal))
            {
                long target = RoundSingleCompatible(resources.GetFansTotal(null), 1.2f,
                    "tasks.AddTask_SummerGames ch4_4 fan target");
                WideNumericState.SetStoryCh4(target);
            }
            tasks.OnTaskAdded();
            tasks.RenderWidget();
        }

        internal static bool StoryCh4DidQualify(tasks._story_data story)
        {
            int count = 0;
            if (story.ch4_song_finished) count++;
            if (story.ch4_chart_topped) count++;
            if (WideNumericState.GetStoryCh4Scandal() >=
                resources.Get(resources.type.scandalPoints, true)) count++;
            if (WideNumericState.GetStoryCh4() <= resources.GetFansTotal(null)) count++;
            return (staticVars.IsHard() && count >= 4) ||
                (staticVars.IsNormal() && count >= 3) ||
                (staticVars.IsEasy() && count >= 2);
        }

        internal static string GetStoryTaskDescription(tasks._task task)
        {
            return Language.Insert("TASK__CH3_AYA_3", new string[]
            {
                ExtensionMethods.formatNumber(WideNumericState.GetStoryCh3(), false, false)
            });
        }

        internal static void SynchronizeStoryVnVariable(string formula)
        {
            if (!string.Equals(formula, "story_chapter_3_aya_init_2",
                    StringComparison.Ordinal)) return;
            long target = WideNumericState.GetStoryCh3();
            if (target >= int.MinValue && target <= int.MaxValue) return;
            variables.Set("aya_fans",
                ExtensionMethods.formatNumber(target, false, false));
        }

        internal static void RenderSummerGamesTargets(Summer_Games_Button view)
        {
            if (view == null || SummerGamesSetCheckbox == null || SummerGamesSetTitle == null)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Summer_Games_Button target UI bindings no longer match the audited shape.");
                throw new MissingMethodException(typeof(Summer_Games_Button).FullName,
                    "SetCheckbox/SetTitle");
            }
            long scandalTarget = WideNumericState.GetStoryCh4Scandal();
            if (scandalTarget == -1L)
            {
                SummerGamesSetCheckbox.Invoke(view,
                    new object[] { view.Checkbox_Scandals, null });
                SummerGamesSetTitle.Invoke(view,
                    new object[] { view.Title_Scandals, false, string.Empty });
                view.Tip_Scandals.SetActive(false);
            }
            else
            {
                SummerGamesSetCheckbox.Invoke(view, new object[]
                {
                    view.Checkbox_Scandals,
                    new bool?(scandalTarget >=
                        resources.Get(resources.type.scandalPoints, true))
                });
                SummerGamesSetTitle.Invoke(view,
                    new object[] { view.Title_Scandals, true,
                        "PROPOSAL__AVOID_SCANDALS" });
                view.Tip_Scandals.SetActive(true);
                view.Tip_Scandals.GetComponent<ButtonDefault>().SetTooltip(
                    Language.Insert("PROPOSAL__TIP_SCANDAL", new string[]
                    {
                        scandalTarget.ToString(CultureInfo.InvariantCulture)
                    }));
            }

            long fanTarget = WideNumericState.GetStoryCh4();
            if (fanTarget == -1L)
            {
                SummerGamesSetCheckbox.Invoke(view,
                    new object[] { view.Checkbox_Fans, null });
                SummerGamesSetTitle.Invoke(view,
                    new object[] { view.Title_Fans, false, string.Empty });
                view.Tip_Fans.SetActive(false);
                return;
            }
            SummerGamesSetCheckbox.Invoke(view, new object[]
            {
                view.Checkbox_Fans,
                new bool?(fanTarget <= resources.GetFansTotal(null))
            });
            SummerGamesSetTitle.Invoke(view,
                new object[] { view.Title_Fans, true, "PROPOSAL__FANBASE" });
            view.Tip_Fans.SetActive(true);
            view.Tip_Fans.GetComponent<ButtonDefault>().SetTooltip(
                Language.Insert("PROPOSAL__TIP_FANS", new string[]
                {
                    ExtensionMethods.formatNumber(fanTarget, false, false)
                }));
        }

        internal static void SynchronizeDebugStoryTargets()
        {
            if (!tasks.Story_Data.ch4_display_task) return;
            WideNumericState.SetStoryCh4Scandal(0L);
            WideNumericState.SetStoryCh4(1L);
            tasks.Trigger_CH4_Update();
        }

        internal static long GetCafeFansNeeded(int cafeCount)
        {
            if (cafeCount == -1) cafeCount = Cafes.CountCafes();
            switch (cafeCount)
            {
                case 0: return 0L;
                case 1: return 50000L;
                case 2: return 150000L;
                case 3: return 500000L;
                case 4: return 1000000L;
                default:
                    return WideNumericRepair.Multiply((long)cafeCount - 4L, 5000000L,
                        "Cafes.GetFansNeededToBuild");
            }
        }

        internal static long GetCafeLastWeekEarning()
        {
            long total = 0L;
            foreach (Cafes._cafe cafe in Cafes.Cafes_)
            {
                int count = 0;
                for (int index = cafe.Stats.Count - 1; index >= 0 && count < 6; index--, count++)
                    total = WideNumericRepair.Add(total,
                        WideNumericState.GetCafeProfit(cafe, index, cafe.Stats[index]),
                        "Cafes.GetLastWeekEarning");
            }
            return total;
        }

        internal static long GetCafeWeeklyIncomeForDisplay()
        {
            int days = TelModLibraryInterop.UnofficialPatchLoaded ? 7 : 6;
            long total = 0L;
            foreach (Cafes._cafe cafe in Cafes.Cafes_)
            {
                int count = 0;
                for (int index = cafe.Stats.Count - 1;
                    index >= 0 && count < days; index--, count++)
                {
                    total = WideNumericRepair.Add(total,
                        WideNumericState.GetCafeProfit(cafe, index, cafe.Stats[index]),
                        "tooltip_money.Cafe weekly income");
                }
            }
            return total;
        }

        private static List<agency._room> GetCafeRooms()
        {
            List<agency._room> result = new List<agency._room>();
            agency owner = Camera.main == null ? null :
                Camera.main.GetComponent<mainScript>().Data.GetComponent<agency>();
            if (owner == null) return result;
            foreach (agency._floor floor in owner.floors)
                foreach (agency._room room in floor.floor)
                    if (room.type == agency._type.cafeAndShop)
                        result.Add(room);
            return result;
        }

        internal static int GetCafeOccupancy(List<agency._room> cafes)
        {
            if (cafes == null || cafes.Count == 0) return 0;
            long workerFans = 0L;
            foreach (agency._room room in cafes)
                foreach (data_girls.girls girl in room.roomObj.GetComponent<Room_Cafe>().GetGirls())
                    workerFans = WideNumericRepair.Add(workerFans, girl.GetFans_Total(null),
                        "Cafes occupancy worker fans");
            long previous = GetCafeFansNeeded(cafes.Count - 1);
            long next = GetCafeFansNeeded(cafes.Count);
            long totalFans = WideNumericRepair.Add(resources.GetFansTotal(null), workerFans,
                "Cafes occupancy total fans");
            long missing = WideNumericRepair.Subtract(next, totalFans,
                "Cafes occupancy missing fans");
            if (missing <= 0L) return 100;
            long range = WideNumericRepair.Subtract(next, previous,
                "Cafes occupancy range");
            if (range <= 0L)
            {
                WideNumericRepair.LatchInvariantFailure("Cafes occupancy has a nonpositive threshold range.");
                throw new InvalidOperationException("Invalid café threshold range.");
            }
            long percentage;
            if (next >= -ExactSingleIntegerBoundary && next <= ExactSingleIntegerBoundary &&
                previous >= -ExactSingleIntegerBoundary && previous <= ExactSingleIntegerBoundary &&
                totalFans >= -ExactSingleIntegerBoundary && totalFans <= ExactSingleIntegerBoundary)
            {
                float fPrevious = (float)previous;
                float fNext = (float)next;
                float fMissing = fNext - (float)totalFans;
                percentage = Mathf.RoundToInt(fMissing / (fNext - fPrevious) * 100f);
            }
            else
            {
                percentage = WideNumericRepair.DivideRoundToEven(
                    WideNumericRepair.Multiply(missing, 100L,
                        "Cafes occupancy percentage numerator"),
                    range, "Cafes occupancy percentage");
            }
            return WideNumericMath.ClampToInt32(
                WideNumericRepair.Subtract(100L, percentage, "Cafes occupancy"));
        }

        internal static long GetCafeFloorMoney(List<agency._room> cafes, int occupancy)
        {
            if (cafes == null || cafes.Count == 0) return 0L;
            long units = WideNumericRepair.Add(
                WideNumericRepair.Multiply(100L, cafes.Count - 1L,
                    "Cafes floor-equivalent full floors"),
                occupancy, "Cafes floor-equivalent occupancy");
            int basis = Activities.GetPerformanceMoneyPerLevel(
                Activities.GetActivityLevel(Activity._type.promotion), false);
            if (units >= -ExactSingleIntegerBoundary && units <= ExactSingleIntegerBoundary &&
                basis >= -ExactSingleIntegerBoundary && basis <= ExactSingleIntegerBoundary)
                return Mathf.RoundToInt((float)basis * ((float)units / 100f));
            return WideNumericRepair.DivideRoundToEven(
                WideNumericRepair.Multiply(basis, units, "Cafes floor income numerator"),
                100L, "Cafes floor income");
        }

        internal static long GetCafeMoneyPerDay()
        {
            List<agency._room> cafes = GetCafeRooms();
            return cafes.Count == 0 ? 0L : GetCafeFloorMoney(cafes, GetCafeOccupancy(cafes));
        }

        internal static void RenderCafeRooms(bool addMoney)
        {
            List<agency._room> cafes = GetCafeRooms();
            if (cafes.Count == 0) return;
            int occupancy = GetCafeOccupancy(cafes);
            long money = 0L;
            long each = 0L;
            if (addMoney)
            {
                money = GetCafeFloorMoney(cafes, occupancy);
                WideNumericRepair.Add(resources.Money(),
                    BuffMeWideNumericInterop.PreviewResourceDelta(resources.type.money, money),
                    "Cafes.RenderRooms money preflight");
                each = money >= -ExactSingleIntegerBoundary &&
                        money <= ExactSingleIntegerBoundary
                    ? Mathf.RoundToInt((float)money / (float)cafes.Count)
                    : WideNumericRepair.DivideRoundToEven(money, cafes.Count,
                        "Cafes.RenderRooms float amount");
            }
            for (int index = 0; index < cafes.Count; index++)
                cafes[index].roomObj.GetComponent<Room_Cafe>().Render(
                    index == cafes.Count - 1 ? occupancy : 100);
            if (!addMoney) return;
            resources.Add(resources.type.money, money);
            foreach (agency._room room in cafes)
                room.addFloat(Floats.type.text, ExtensionMethods.formatMoney(each, false, false, false),
                    true, room.roomObj.GetComponent<Room_Cafe>().GetObject(
                        Room_Cafe.Room_Sprite._type.cashier), 0f, 1f, 0f, null);
        }

        internal static string GetCafeTooltip()
        {
            List<agency._room> cafes = GetCafeRooms();
            if (cafes.Count == 0) return string.Empty;
            int occupancy = GetCafeOccupancy(cafes);
            long units = WideNumericRepair.Add(
                WideNumericRepair.Multiply(100L, cafes.Count - 1L,
                    "Cafes.GetTooltip full floors"), occupancy,
                "Cafes.GetTooltip occupancy units");
            long percent = WideNumericRepair.DivideRoundToEven(units, cafes.Count,
                "Cafes.GetTooltip occupancy percent");
            long income = GetCafeFloorMoney(cafes, occupancy);
            string text = cafes.Count == 1
                ? Language.Data["CAFE__1_FLOOR"]
                : Language.Insert("CAFE__FLOORS", new string[]
                    { ExtensionMethods.formatNumber(cafes.Count, false, false) });
            text += "\n" + Language.Data["CAFE__OCCUPANCY"] + ": " +
                percent.ToString(CultureInfo.InvariantCulture) + "%\n";
            text += Language.Data["CAFE__TOTAL_INCOME"] + ": " +
                ExtensionMethods.formatMoney(income, false, false, false) + "\n";
            text += Language.Data["CAFE__NEW_FANS"] + ": " +
                ExtensionMethods.formatNumber(GetActivityPromotionReward(1f), false, false);
            text += mainScript.separator;
            return text + ExtensionMethods.color(Language.Data["CAFE__TIP"], mainScript.grey_light);
        }

        internal static long GetCafeMoneyToAdd(Cafes._cafe cafe)
        {
            Cafes._cafe._dish dish = cafe.GetCurrentDish();
            if (dish == null) return 0L;
            int level = Activities.GetActivityLevel(Activity._type.performance);
            float levelCoefficient = 1f;
            if (level < 3) level = 3;
            if (level > 7)
            {
                if (level == 8) levelCoefficient += 0.1f;
                else if (level == 9) levelCoefficient += 0.2f;
                else levelCoefficient += 0.3f;
                level = 7;
            }
            int basis = Activities.GetPerformanceMoneyPerLevel(level, true);
            return RoundSingleProductCompatible(basis, "Cafes._cafe.GetMoneyToAdd",
                levelCoefficient, dish.GetQualityCoeff(), dish.GetNoveltyCoeff());
        }

        internal static long GetCafeFansToAdd(Cafes._cafe cafe)
        {
            if (cafe == null) throw new ArgumentNullException(nameof(cafe));
            if (cafe.WorkingGirls.Count == 0) return 0L;
            float workerCoefficient = 0f;
            foreach (data_girls.girls girl in cafe.WorkingGirls)
            {
                data_girls.girls.param highest = girl.getHighestParam();
                workerCoefficient += highest.val * 0.3f * 0.01f;
            }
            int level = Activities.GetActivityLevel(Activity._type.promotion);
            float levelCoefficient = 1f;
            if (level < 3) level = 3;
            if (level > 7)
            {
                if (level == 8) levelCoefficient += 0.1f;
                else if (level == 9) levelCoefficient += 0.2f;
                else levelCoefficient += 0.3f;
                level = 7;
            }
            int basis = Activities.GetPromotionFansPerLevel(level, true);
            long result = RoundSingleProductCompatible(
                basis,
                "Cafes._cafe.GetFansToAdd",
                levelCoefficient,
                workerCoefficient,
                0.3f);
            if (result < cafe.WorkingGirls.Count && result > 0L)
                result = cafe.WorkingGirls.Count;
            return result;
        }

        internal static string GetCafeAverageProfit(
            Cafes._cafe cafe,
            Cafes._cafe._dish dish)
        {
            long average;
            if (!TryGetCafeStatAverage(cafe, dish, false, out average))
                return Language.Data["SSK__NA"];
            return ExtensionMethods.formatMoney(average, false, false, false);
        }

        internal static string GetCafeAverageNewFans(
            Cafes._cafe cafe,
            Cafes._cafe._dish dish)
        {
            long average;
            if (!TryGetCafeStatAverage(cafe, dish, true, out average))
                return Language.Data["SSK__NA"];
            return ExtensionMethods.formatNumber(average, false, false);
        }

        private static bool TryGetCafeStatAverage(
            Cafes._cafe cafe,
            Cafes._cafe._dish dish,
            bool fans,
            out long average)
        {
            if (cafe == null) throw new ArgumentNullException(nameof(cafe));
            if (dish == null) throw new ArgumentNullException(nameof(dish));
            long total = 0L;
            int count = 0;
            bool vanillaAccumulatorSafe = true;
            for (int ordinal = 0; ordinal < cafe.Stats.Count; ordinal++)
            {
                Cafes._cafe._stat stat = cafe.Stats[ordinal];
                if (stat.Dish_ID != dish.ID) continue;
                long value = fans
                    ? WideNumericState.GetCafeNewFans(cafe, ordinal, stat)
                    : WideNumericState.GetCafeProfit(cafe, ordinal, stat);
                total = WideNumericRepair.Add(total, value,
                    fans ? "Cafes._cafe.GetAverageNewFans" :
                        "Cafes._cafe.GetAverageProfit");
                if (value < int.MinValue || value > int.MaxValue ||
                    total < int.MinValue || total > int.MaxValue)
                    vanillaAccumulatorSafe = false;
                count++;
            }
            if (count == 0)
            {
                average = 0L;
                return false;
            }
            average = vanillaAccumulatorSafe
                ? Mathf.RoundToInt((float)(int)total / (float)count)
                : WideNumericRepair.DivideRoundToEven(total, count,
                    fans ? "Cafes._cafe.GetAverageNewFans average" :
                        "Cafes._cafe.GetAverageProfit average");
            return true;
        }

        internal static void PreflightCafeRender(Cafes._cafe cafe)
        {
            if (cafe == null) throw new ArgumentNullException(nameof(cafe));
            if (cafe.Stats == null || cafe.Stats.Count > 90)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Cafes.RenderCafe encountered an invalid stat history.");
                throw new InvalidOperationException("Invalid café stat history.");
            }
            long exact = GetCafeMoneyToAdd(cafe);
            int mirror = WideNumericMath.ClampToInt32(exact);
            long credited = Mathf.RoundToInt((float)mirror);
            long effectiveCredited = BuffMeWideNumericInterop.PreviewResourceDelta(
                resources.type.money, credited);
            long afterCredit = WideNumericRepair.Add(resources.Money(), effectiveCredited,
                "Cafes.RenderCafe compatibility credit preflight");
            long correction = BuffMeWideNumericInterop.CalculateExactResourceCorrection(
                resources.type.money, exact, credited,
                "Cafes.RenderCafe BuffMe-aware compatibility correction preflight");
            WideNumericRepair.Add(afterCredit, correction,
                "Cafes.RenderCafe exact money preflight");

            long exactFans = GetCafeFansToAdd(cafe);
            int fanMirror = WideNumericMath.ClampToInt32(exactFans);
            long exactFansPerGirl = 0L;
            int creditedFansPerGirl = 0;
            if (exactFans > 0L && cafe.WorkingGirls.Count > 0)
            {
                exactFansPerGirl = exactFans / cafe.WorkingGirls.Count;
                creditedFansPerGirl = Mathf.RoundToInt(
                    (float)(fanMirror / cafe.WorkingGirls.Count));
                resources.fanType type = cafe.GetFanTypeToAdd();
                Dictionary<resources._fan, long> projectedFans =
                    new Dictionary<resources._fan, long>();
                foreach (data_girls.girls girl in cafe.WorkingGirls)
                    PreflightGirlFanAdditionWithoutRng(
                        girl, exactFansPerGirl, type, projectedFans);
            }

            Cafes._cafe._stat previousLast = cafe.Stats.Count == 0
                ? null : cafe.Stats[cafe.Stats.Count - 1];
            List<long> existingProfits = new List<long>();
            List<long> existingNewFans = new List<long>();
            for (int index = 0; index < cafe.Stats.Count; index++)
            {
                Cafes._cafe._stat stat = cafe.Stats[index];
                if (stat == null)
                {
                    WideNumericRepair.LatchInvariantFailure(
                        "Cafes.RenderCafe encountered a null historical stat row.");
                    throw new InvalidOperationException("Null café stat row.");
                }
                existingProfits.Add(WideNumericState.GetCafeProfit(cafe, index, stat));
                existingNewFans.Add(WideNumericState.GetCafeNewFans(cafe, index, stat));
            }
            Cafes._cafe._dish currentDish = cafe.GetCurrentDish();
            lock (PendingSync)
            {
                if (activeCafeRender != null)
                {
                    WideNumericRepair.LatchInvariantFailure(
                        "Cafes.RenderCafe entered recursively across an active A33 boundary.");
                    throw new InvalidOperationException("Recursive café render boundary.");
                }
                activeCafeRender = new CafeRenderPreflight
                {
                    ExactMoney = exact,
                    CompatibilityMirror = mirror,
                    CreditedMoney = credited,
                    StatCount = cafe.Stats.Count,
                    PreviousLastStat = previousLast,
                    ExpectedDishId = currentDish == null ? -1 : currentDish.ID,
                    ExistingProfits = existingProfits,
                    ExactNewFans = exactFans,
                    CompatibilityFanMirror = fanMirror,
                    ExactFansPerGirl = exactFansPerGirl,
                    CreditedFansPerGirl = creditedFansPerGirl,
                    FanType = cafe.GetFanTypeToAdd(),
                    WorkingGirls = new List<data_girls.girls>(cafe.WorkingGirls),
                    ExistingNewFans = existingNewFans
                };
                PendingCafeDailyProfits[cafe.ID] = activeCafeRender;
            }
        }

        internal static void CompleteCafeRender(Cafes._cafe cafe)
        {
            CafeRenderPreflight preflight;
            lock (PendingSync)
            {
                if (!PendingCafeDailyProfits.TryGetValue(cafe.ID, out preflight))
                {
                    WideNumericRepair.LatchInvariantFailure(
                        "Cafes.RenderCafe completed without its A33 preflight value.");
                    return;
                }
                PendingCafeDailyProfits.Remove(cafe.ID);
                if (ReferenceEquals(activeCafeRender, preflight)) activeCafeRender = null;
            }
            if (preflight.ExactNewFans > 0L && preflight.WorkingGirls.Count > 0 &&
                preflight.AppliedCafeWorkers.Count != preflight.WorkingGirls.Count)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Cafes.RenderCafe did not route every preflighted fan mutation through A33.");
                return;
            }
            int expectedCount = preflight.StatCount >= 90
                ? preflight.StatCount : preflight.StatCount + 1;
            if (cafe.Stats.Count != expectedCount || cafe.Stats.Count == 0)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Cafes.RenderCafe did not produce its preflighted stat-list shape.");
                return;
            }
            int ordinal = cafe.Stats.Count - 1;
            Cafes._cafe._stat stat = cafe.Stats[ordinal];
            if (stat == null || ReferenceEquals(stat, preflight.PreviousLastStat) ||
                stat.Profit != preflight.CompatibilityMirror ||
                stat.New_Fans != preflight.CompatibilityFanMirror ||
                stat.Dish_ID != preflight.ExpectedDishId)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Cafes.RenderCafe appended a stat row that did not match its " +
                    "preflighted compatibility witness.");
                return;
            }
            List<long> finalSeries = new List<long>();
            int first = preflight.StatCount >= 90 ? 1 : 0;
            for (int index = first; index < preflight.ExistingProfits.Count; index++)
                finalSeries.Add(preflight.ExistingProfits[index]);
            finalSeries.Add(preflight.ExactMoney);
            List<long> finalFanSeries = new List<long>();
            for (int index = first; index < preflight.ExistingNewFans.Count; index++)
                finalFanSeries.Add(preflight.ExistingNewFans[index]);
            finalFanSeries.Add(preflight.ExactNewFans);

            WideNumericState.ReplaceCafeStatSeries(cafe, finalSeries, finalFanSeries);
            long correction = BuffMeWideNumericInterop.CalculateExactResourceCorrection(
                resources.type.money, preflight.ExactMoney, preflight.CreditedMoney,
                "Cafes.RenderCafe BuffMe-aware compatibility correction");
            if (correction != 0L)
            {
                BuffMeWideNumericInterop.BeginResourceMultiplierSuppression();
                try
                {
                    resources.Add(resources.type.money, correction);
                }
                finally
                {
                    BuffMeWideNumericInterop.EndResourceMultiplierSuppression();
                }
            }
        }

        internal static bool TryResolveCafeFanAddition(
            data_girls.girls girl,
            long requested,
            resources.fanType? fanType,
            out long exact)
        {
            exact = requested;
            CafeRenderPreflight preflight = activeCafeRender;
            if (preflight == null || preflight.ExactNewFans <= 0L ||
                preflight.WorkingGirls.Count == 0 ||
                !preflight.WorkingGirls.Contains(girl)) return false;
            if (!fanType.HasValue || fanType.Value != preflight.FanType ||
                requested != preflight.CreditedFansPerGirl ||
                !preflight.AppliedCafeWorkers.Add(girl))
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Cafes.RenderCafe fan mutation did not match its A33 preflight witness.");
                throw new InvalidOperationException("Café fan mutation/preflight mismatch.");
            }
            exact = preflight.ExactFansPerGirl;
            return true;
        }

        private static void PreflightGirlFanAdditionWithoutRng(
            data_girls.girls girl,
            long value,
            resources.fanType? fanType,
            Dictionary<resources._fan, long> projected)
        {
            if (girl == null) throw new ArgumentNullException(nameof(girl));
            if (projected == null) throw new ArgumentNullException(nameof(projected));
            if (value <= 0L) return;
            if (girl.HasAward(Awards._type.best_debut_idol))
                value = WideNumericRepair.Multiply(
                    value, 2L, "Cafes.RenderCafe fan award preflight");
            else if (girl.HasNomination(Awards._type.best_debut_idol))
                value = WideNumericMath.RoundRatioToEven(value, 5L, 4L);
            if (girl.Fans == null || girl.Fans.Count == 0)
            {
                // Vanilla creates an all-zero fan grid immediately before allocation.
                // Any one new bucket is therefore bounded by the already-checked total.
                return;
            }
            girl.RecalcFanAppeal();
            float appealTotal = girl.GetAppeal_Total();
            if (appealTotal == 0f) return;

            List<resources._fan> recipients = new List<resources._fan>();
            List<long> baseDeltas = new List<long>();
            long allocated = 0L;
            foreach (resources._fan fan in girl.Fans)
            {
                if (!fan.IsType(fanType)) continue;
                long delta = WideNumericRepair.FloorSingleProduct(
                    value,
                    "Cafes.RenderCafe fan allocation preflight",
                    fan.GetTotalAppeal() / appealTotal);
                recipients.Add(fan);
                baseDeltas.Add(delta);
                allocated = WideNumericRepair.Add(
                    allocated, delta, "Cafes.RenderCafe allocated-fan preflight");
            }
            if (recipients.Count == 0) return;
            long remainder = WideNumericRepair.Subtract(
                value, allocated, "Cafes.RenderCafe fan-remainder preflight");
            long quotient = remainder / recipients.Count;
            long tail = remainder % recipients.Count;
            long tailMaximum = tail > 0L ? 1L : 0L;
            for (int index = 0; index < recipients.Count; index++)
            {
                long maximumDelta = WideNumericRepair.Add(
                    baseDeltas[index], quotient,
                    "Cafes.RenderCafe fan-residue preflight");
                if (tailMaximum != 0L)
                    maximumDelta = WideNumericRepair.Add(
                        maximumDelta, tailMaximum,
                        "Cafes.RenderCafe fan-tail preflight");
                long before;
                if (!projected.TryGetValue(recipients[index], out before))
                    before = recipients[index].people;
                projected[recipients[index]] = WideNumericRepair.Add(
                    before, maximumDelta,
                    "Cafes.RenderCafe fan-bucket preflight");
            }
        }

        internal static void LeaveCafeRenderBoundary(
            Cafes._cafe cafe,
            Exception exception)
        {
            CafeRenderPreflight preflight = activeCafeRender;
            if (preflight != null) activeCafeRender = null;
            if (exception == null || cafe == null) return;
            lock (PendingSync) PendingCafeDailyProfits.Remove(cafe.ID);
        }

        internal static bool TryLaunchCafeFloatsWide(
            Room_Cafe room,
            Cafes._cafe._stat stat)
        {
            if (room == null || stat == null) return false;
            CafeRenderPreflight preflight = null;
            foreach (Cafes._cafe cafe in Cafes.Cafes_)
            {
                if (cafe == null || cafe.Stats == null || cafe.Stats.Count == 0 ||
                    !ReferenceEquals(cafe.Stats[cafe.Stats.Count - 1], stat)) continue;
                lock (PendingSync)
                    PendingCafeDailyProfits.TryGetValue(cafe.ID, out preflight);
                break;
            }
            if (preflight == null) return false;
            if (preflight.ExactMoney >= int.MinValue &&
                preflight.ExactMoney <= int.MaxValue &&
                preflight.ExactNewFans >= int.MinValue &&
                preflight.ExactNewFans <= int.MaxValue)
                return false;
            if (preflight.ExactMoney == 0L && preflight.ExactNewFans == 0L) return true;
            string value = string.Empty;
            if (preflight.ExactMoney > 0L)
                value = ExtensionMethods.formatMoney(
                    preflight.ExactMoney, false, false, false);
            if (preflight.ExactMoney > 0L && preflight.ExactNewFans > 0L)
                value += ExtensionMethods.color("  |  ", mainScript.grey_light);
            if (preflight.ExactNewFans > 0L)
                value = value + Language.Data["FANS"] + ": + " +
                    ExtensionMethods.formatNumber(preflight.ExactNewFans, false, false);
            Vector3 position = Camera.main.WorldToScreenPoint(room.transform.position);
            Camera.main.GetComponent<mainScript>().Data.GetComponent<Floats>().Create(
                value, mainScript.green32, position, 3f, 0f, 16);
            return true;
        }

        private sealed class CafeRenderPreflight
        {
            internal long ExactMoney;
            internal int CompatibilityMirror;
            internal long CreditedMoney;
            internal int StatCount;
            internal Cafes._cafe._stat PreviousLastStat;
            internal int ExpectedDishId;
            internal List<long> ExistingProfits;
            internal long ExactNewFans;
            internal int CompatibilityFanMirror;
            internal long ExactFansPerGirl;
            internal int CreditedFansPerGirl;
            internal resources.fanType FanType;
            internal List<data_girls.girls> WorkingGirls;
            internal List<long> ExistingNewFans;
            internal readonly HashSet<data_girls.girls> AppliedCafeWorkers =
                new HashSet<data_girls.girls>();
        }

        internal static int GetFanRank(data_girls.girls girl)
        {
            long fans = girl.GetFans_Total(null);
            int rank = 0;
            foreach (data_girls.girls candidate in data_girls.girl)
                if (candidate.GetFans_Total(null) < fans) rank++;
            return rank;
        }

        internal static long GetConcertSoldTickets(
            SEvent_Concerts._concert._projectedValues projected)
        {
            return RoundSingleCompatible(
                SEvent_Concerts.GetVenueCapacity(projected.Parent.Venue),
                projected.Attendance,
                "SEvent_Concerts._projectedValues.GetNumberOfSoldTickets");
        }

        internal static long GetConcertRevenue(
            SEvent_Concerts._concert._projectedValues projected)
        {
            long baseRevenue = WideNumericRepair.Multiply(
                GetConcertSoldTickets(projected), projected.TicketPrice,
                "SEvent_Concerts._projectedValues.GetRevenue tickets*price");
            float hype;
            if (!TelModLibraryInterop.TryGetUnofficialPatchConcertHype(projected, out hype))
                hype = projected.GetHype();
            return RoundSingleCompatible(baseRevenue, hype,
                "SEvent_Concerts._projectedValues.GetRevenue hype");
        }

        internal static long GetConcertActualProfit(
            SEvent_Concerts._concert._projectedValues projected)
        {
            return WideNumericRepair.Subtract(projected.Actual_Revenue,
                projected.GetProductionCost(),
                "SEvent_Concerts._projectedValues.GetActualProfit");
        }

        internal static bool ConcertNeedsWideAttendancePath(
            SEvent_Concerts._concert._projectedValues projected)
        {
            long hardcore = resources.FansByType(null,
                new resources.fanType?(resources.fanType.hardcore), null);
            long casual = resources.FansByType(null,
                new resources.fanType?(resources.fanType.casual), null);
            return NeedsWideFanPath(hardcore) || NeedsWideFanPath(casual);
        }

        internal static void SetConcertAttendanceWide(
            SEvent_Concerts._concert._projectedValues projected)
        {
            float attendance = projected.GetAttendanceOfDemo() *
                ScandalPoints.GetConcertAttendance(-1);
            float casualAttendance = attendance / 5f;
            long hardcore = resources.FansByType(null,
                new resources.fanType?(resources.fanType.hardcore), null);
            long casual = resources.FansByType(null,
                new resources.fanType?(resources.fanType.casual), null);
            int capacity = projected.Parent.GetCapacity();
            double audience = (double)hardcore * attendance +
                (double)casual * casualAttendance;
            if (double.IsNaN(audience) || double.IsInfinity(audience))
            {
                WideNumericRepair.LatchInvariantFailure(
                    "SEvent_Concerts._projectedValues.SetAttendance produced a non-finite audience.");
                throw new OverflowException("Concert attendance audience is non-finite.");
            }
            projected.Attendance = audience >= capacity ? 1f :
                (capacity <= 0 ? 0f : (float)(audience / capacity));
            projected.Actual_Attendance = projected.Attendance;
        }

        internal static long GetSskProductionCost(SEvent_SSK._SSK ssk)
        {
            long total = 0L;
            if (ssk.Single != null && !ssk.Single.IsDigital())
                total = WideNumericRepair.Add(total,
                    RoundSingleCompatible(ssk.Single.GetProductionCost(), 0.1f,
                        "SEvent_SSK._SSK.GetProductionCost single"),
                    "SEvent_SSK._SSK.GetProductionCost");
            if (ssk.Concert != null)
                total = WideNumericRepair.Add(total,
                    RoundSingleCompatible(ssk.Concert.ProjectedValues.GetProductionCost(), 0.5f,
                        "SEvent_SSK._SSK.GetProductionCost concert"),
                    "SEvent_SSK._SSK.GetProductionCost");
            long broadcast = 0L;
            switch (ssk.Broadcast)
            {
                case SEvent_SSK._broadcast.liveBlog: broadcast = 100000L; break;
                case SEvent_SSK._broadcast.webStream: broadcast = 1000000L; break;
                case SEvent_SSK._broadcast.localTV: broadcast = 10000000L; break;
                case SEvent_SSK._broadcast.nationalTV: broadcast = 100000000L; break;
            }
            total = WideNumericRepair.Add(total, broadcast,
                "SEvent_SSK._SSK.GetProductionCost broadcast");
            float coefficient = 1f;
            if (Awards.HasAward(Awards._type.most_prolific_group)) coefficient = 0.5f;
            else if (Awards.HasNomination(Awards._type.most_prolific_group)) coefficient = 0.75f;
            if (variables.Get("CHEAPER_EVENTS") == "true" &&
                staticVars.PlayerData.Chapter < tasks._chapter.chapter_5)
                coefficient *= 0.75f;
            return RoundSingleCompatible(total, coefficient,
                "SEvent_SSK._SSK.GetProductionCost coefficient");
        }

        internal static long GetSskTotalProductionCost(SEvent_SSK._SSK ssk)
        {
            long total = ssk.GetProductionCost();
            if (ssk.Single != null)
                total = WideNumericRepair.Add(total, ssk.Single.productionCost,
                    "SEvent_SSK._SSK.TotalProductionCost single");
            if (ssk.Concert != null)
                total = WideNumericRepair.Add(total, ssk.Concert.ProjectedValues.GetProductionCost(),
                    "SEvent_SSK._SSK.TotalProductionCost concert");
            return total;
        }

        internal static bool SskNeedsWideResultsPath(SEvent_SSK._SSK ssk)
        {
            if (ssk == null || ssk.Single == null || ssk.Single.ReleaseData == null)
                return false;
            if (NeedsWideFanPath(ssk.Single.ReleaseData.Sales)) return true;
            foreach (data_girls.girls girl in data_girls.girl)
            {
                if (!girl.CanParticipateInSSK()) continue;
                if (NeedsWideFanPath(girl.GetFan_Count(resources.fanType.hardcore)) ||
                    NeedsWideFanPath(girl.GetFan_Count(resources.fanType.casual))) return true;
            }
            return false;
        }

        internal static void GenerateSskResultsWide(SEvent_SSK._SSK ssk)
        {
            List<SskTempResult> values = new List<SskTempResult>();
            long sales = ssk.Single.ReleaseData.Sales;
            if (sales == 0L) sales = 10000L;
            long totalWeightedFans = 0L;
            foreach (data_girls.girls girl in data_girls.girl)
            {
                if (!girl.CanParticipateInSSK()) continue;
                float random = (float)UnityEngine.Random.Range(80, 120) / 100f;
                long famePoints = (long)Mathf.Round(girl.GetFamePoints() * random);
                values.Add(new SskTempResult(girl, famePoints));
                long hardcore = WideNumericRepair.Multiply(
                    girl.GetFan_Count(resources.fanType.hardcore), 4L,
                    "SEvent_SSK._SSK.GenerateResults weighted hardcore total");
                long casual = RoundSingleCompatible(
                    girl.GetFan_Count(resources.fanType.casual), 0.25f,
                    "SEvent_SSK._SSK.GenerateResults weighted casual total");
                totalWeightedFans = WideNumericRepair.Add(totalWeightedFans, hardcore,
                    "SEvent_SSK._SSK.GenerateResults total fans");
                totalWeightedFans = WideNumericRepair.Add(totalWeightedFans, casual,
                    "SEvent_SSK._SSK.GenerateResults total fans");
            }
            if (totalWeightedFans <= 0L)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "SEvent_SSK._SSK.GenerateResults has no positive weighted fan denominator.");
                throw new InvalidOperationException("SSK weighted fan denominator is nonpositive.");
            }
            foreach (SskTempResult value in values)
            {
                float hardcoreCoefficient = 1f;
                float casualCoefficient = 1f;
                int scandal = value.Girl.GetScandalPoints();
                if (scandal > 0)
                {
                    float penalty = -0.1f;
                    policies._value policy = policies.GetSelectedPolicyValue(
                        policies._type.image).Value;
                    if (policy == policies._value.image_orthodox) penalty = -0.2f;
                    else if (policy == policies._value.image_rebellious) penalty = -0.05f;
                    float aggregate = penalty * scandal;
                    hardcoreCoefficient = Mathf.Clamp(1f + aggregate, 0.1f, 1f);
                    casualCoefficient = Mathf.Clamp(1f + aggregate / 4f, 0.1f, 1f);
                }
                long hardcoreVotes = WideNumericRepair.RoundProductRatioWithSingleProductsToEven(
                    value.Girl.GetFan_Count(resources.fanType.hardcore), sales,
                    totalWeightedFans, "SEvent_SSK._SSK.GenerateResults hardcore votes",
                    4f, hardcoreCoefficient);
                long casualVotes = WideNumericRepair.RoundProductRatioWithSingleProductsToEven(
                    value.Girl.GetFan_Count(resources.fanType.casual), sales,
                    totalWeightedFans, "SEvent_SSK._SSK.GenerateResults casual votes",
                    0.25f, casualCoefficient);
                value.Votes = WideNumericRepair.Add(hardcoreVotes, casualVotes,
                    "SEvent_SSK._SSK.GenerateResults votes");
            }
            values.Sort((left, right) => right.FamePoints.CompareTo(left.FamePoints));
            List<SskExpectedPlace> expectedPlaces = new List<SskExpectedPlace>();
            foreach (data_girls.girls girl in data_girls.girl)
            {
                if (!girl.CanParticipateInSSK()) continue;
                int index = values.FindIndex(item => item.Girl == girl);
                int place = index + 1 + UnityEngine.Random.Range(9, 12) - 10;
                if (place < 1) place = 1;
                expectedPlaces.Add(new SskExpectedPlace(girl, place));
            }
            values.Sort((left, right) => right.Votes.CompareTo(left.Votes));
            ssk.RecalcFameBonus();
            List<SEvent_SSK._SSK._result> plannedResults =
                new List<SEvent_SSK._SSK._result>();
            int resultLimit = TelModLibraryInterop.GetExtendedSskResultLimit(10);
            resultLimit = Math.Min(resultLimit, ssk.FameBonus.Count);
            for (int index = 0; index < resultLimit && index < values.Count; index++)
            {
                plannedResults.Add(new SEvent_SSK._SSK._result
                {
                    Girl = values[index].Girl,
                    Place = index + 1,
                    Votes = values[index].Votes,
                    FamePoints = ssk.FameBonus[index]
                });
            }
            ssk.Results.Clear();
            ssk.ExpectedResults.Clear();
            foreach (SskExpectedPlace expected in expectedPlaces)
                expected.Girl.SSK_SetExpectedPlace(expected.Place);
            ssk.Results.AddRange(plannedResults);
        }

        internal static void BuyResearchPoints(Research.category category)
        {
            long current = Research.Buying_Cost;
            long next = RoundSingleCompatible(current, 1.2f,
                "Research.category.Buy_Points next cost");
            long debit = WideNumericMath.Negate(current);
            WideNumericRepair.Add(resources.Money(),
                BuffMeWideNumericInterop.PreviewResourceDelta(resources.type.money, debit),
                "Research.category.Buy_Points money preflight");
            resources.Add(resources.type.money, debit);
            Research.Buying_Cost = next;
            category.AddPoints(5000f);
            mainScript.playAudioClip("coin_drop", 0f);
        }

        internal static bool CheckMoneyRequirement(data_dialogues._action requirement)
        {
            return vn_requirements.CheckValue(requirement.formula, resources.Money());
        }

        internal static bool IsWideVnResourceFormula(string formula)
        {
            if (string.Equals(formula, "concert_tickets", StringComparison.Ordinal)) return true;
            if (string.IsNullOrEmpty(formula)) return false;
            string[] parts = formula.Split(new string[] { " " }, StringSplitOptions.None);
            return parts.Length >= 2 && string.Equals(parts[0],
                "last_single_production", StringComparison.Ordinal);
        }

        internal static long ParseVnResourceWide(vn_actions instance, string formula)
        {
            if (!string.Equals(formula, "concert_tickets", StringComparison.Ordinal))
            {
                string[] parts = formula.Split(new string[] { " " }, StringSplitOptions.None);
                if (parts.Length >= 2 && string.Equals(parts[0],
                    "last_single_production", StringComparison.Ordinal))
                {
                    singles._single latest = singles.GetLatestReleasedSingle(true, null);
                    if (latest == null) return -50000L;
                    int percent = int.Parse(parts[1], CultureInfo.InvariantCulture);
                    long gross = WideNumericRepair.Multiply(latest.ReleaseData.Sales,
                        latest.GetOneCDCost(), "vn_actions.ParseResource last_single_production");
                    return RoundSingleCompatible(gross, (float)percent / 100f,
                        "vn_actions.ParseResource last_single_production percent");
                }
                return long.Parse(formula, NumberStyles.Integer, CultureInfo.InvariantCulture);
            }
            SEvent_Concerts component = instance.GetComponent<SEvent_Concerts>();
            SEvent_Concerts._concert concert = component == null ? null : component.Concert;
            if (concert == null) return -50000L;
            long grossTickets = WideNumericRepair.Multiply(concert.GetCapacity(),
                concert.ProjectedValues.TicketPrice,
                "vn_actions.ParseResource concert_tickets");
            long amount = RoundSingleCompatible(grossTickets, 0.1f,
                "vn_actions.ParseResource concert_tickets percent");
            return WideNumericRepair.Subtract(0L, amount,
                "vn_actions.ParseResource concert_tickets sign");
        }

        internal static void DoWideVnResource(vn_actions instance, string parameter, string formula)
        {
            resources.type type = (resources.type)Enum.Parse(typeof(resources.type), parameter);
            long value = ParseVnResourceWide(instance, formula);
            WideNumericRepair.Add(resources.Get(type, false), value,
                "vn_actions.DoResource " + formula);
            resources.Add(type, value);
        }

        internal static long GetExpectedSalary(data_girls.girls girl)
        {
            int fame = girl.GetFameLevel();
            if (fame < 1) return 0L;
            float value = (531435.2f + -531429.7f /
                (1f + Mathf.Pow((float)fame / 43.12236f, 6.423545f))) * 10000f;
            if (float.IsNaN(value) || float.IsInfinity(value) || (double)value > long.MaxValue ||
                (double)value < long.MinValue)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "data_girls.girls.GetExpectedSalary produced a non-finite/out-of-Int64 result.");
                throw new OverflowException("Expected salary is outside Int64.");
            }
            return (long)Math.Floor((double)value);
        }

        internal static long GetAverageEarnings(data_girls.girls girl)
        {
            if (girl.Earnings_History.Count == 0 && girl.Earnings_CurrentMonth == 0L)
                return 0L;
            if (girl.Earnings_History.Count >= 3)
            {
                long total = 0L;
                for (int offset = 1; offset <= 3; offset++)
                    total = WideNumericRepair.Add(total,
                        girl.Earnings_History[girl.Earnings_History.Count - offset],
                        "data_girls.girls.GetAverageEarnings");
                return total / 12L;
            }
            long aggregate = girl.Earnings_CurrentMonth;
            foreach (long value in girl.Earnings_History)
                aggregate = WideNumericRepair.Add(aggregate, value,
                    "data_girls.girls.GetAverageEarnings");
            long denominator = WideNumericRepair.Multiply(
                girl.Earnings_History.Count + 1L, 4L,
                "data_girls.girls.GetAverageEarnings denominator");
            return WideNumericRepair.DivideRoundToEven(aggregate, denominator,
                "data_girls.girls.GetAverageEarnings");
        }

        internal static void RenderSalaryLineExact(Salary_Line view)
        {
            if (view == null || view.Girl == null) return;
            ExtensionMethods.SetText(view.Salary,
                FormatMoneyExact(view.Girl.salary) + " " + Language.Data["PER_WEEK"]);
            ExtensionMethods.SetText(view.Relationship,
                FormatMoneyExact(GetAverageEarnings(view.Girl)) + " " +
                Language.Data["PER_WEEK"]);
        }

        internal static string GetGirlEarningsStringExact(data_girls.girls girl)
        {
            if (girl == null) return string.Empty;
            long average = GetAverageEarnings(girl);
            string clr = average < girl.salary ? mainScript.red : mainScript.green;
            return Language.Data["IDOL__AVG_EARNINGS"] + ": " +
                ExtensionMethods.color(FormatMoneyExact(average) + " " +
                    Language.Data["PER_WEEK"], clr) + "\n" +
                Language.Data["IDOL__TOTAL_EARNINGS"] + ": " +
                ExtensionMethods.color(FormatMoneyExact(GetGirlTotalEarnings(girl)),
                    mainScript.green) +
                ExtensionMethods.size(mainScript.separator, 15) +
                ExtensionMethods.size(ExtensionMethods.color(
                    Language.Data["IDOL__EARNINGS_TIP"], mainScript.grey_light), 15);
        }

        internal static long GetExpectedSalaryTotal(data_girls.girls girl)
        {
            long expected = GetExpectedSalary(girl);
            long earnings = GetAverageEarnings(girl);
            long upper = WideNumericRepair.Multiply(expected, 2L,
                "data_girls.girls.GetExpectedSalary_Total upper");
            if (earnings > upper) return upper;
            long lower = expected / 2L;
            if (earnings < lower) return lower;
            return earnings;
        }

        internal static void IncreaseSalary(data_girls.girls girl)
        {
            long previous = girl.salary;
            long basis = previous == 0L ? 1000L : previous;
            long hundreds = RoundSingleProductCompatible(
                basis, "data_girls.girls.IncreaseSalary", 0.01f, 1.05f);
            long next = WideNumericRepair.Multiply(hundreds, 100L,
                "data_girls.girls.IncreaseSalary hundreds");
            if (next <= previous)
                next = WideNumericRepair.Add(previous, 100L,
                    "data_girls.girls.IncreaseSalary minimum step");
            if (next > 2000000000L) next = 2000000000L;
            girl.salary = next;
        }

        internal static void LowerSalary(data_girls.girls girl)
        {
            long previous = girl.salary;
            long hundreds = RoundSingleProductCompatible(
                previous, "data_girls.girls.LowerSalary", 0.01f, 0.95f);
            long next = WideNumericRepair.Multiply(hundreds, 100L,
                "data_girls.girls.LowerSalary hundreds");
            if (next >= previous)
                next = WideNumericRepair.Subtract(previous, 100L,
                    "data_girls.girls.LowerSalary minimum step");
            if (next < 1000L) next = 1000L;
            girl.salary = next;
        }

        internal static long GetGirlTotalEarnings(data_girls.girls girl)
        {
            long total = girl.Earnings_CurrentMonth;
            foreach (long value in girl.Earnings_History)
                total = WideNumericRepair.Add(total, value,
                    "data_girls.girls.GetTotalEarnings");
            return total;
        }

        internal static long GetSingleTotalSales(singles._single single)
        {
            long total = 0L;
            foreach (singles._single._sales sale in single.sales)
                total = WideNumericRepair.Add(total, sale.sales, "singles._single.GetTotalSales");
            return total;
        }

        internal static long ValueAfterSingleMarketing(
            long sales,
            float mainCoefficient,
            float secondaryCoefficient,
            resources._fan fan,
            singles._param._special_type type,
            Single_Marketing_Roll._result result)
        {
            if (mainCoefficient == 0f) return sales;
            bool success = result == Single_Marketing_Roll._result.success ||
                result == Single_Marketing_Roll._result.success_crit;
            float coefficient = 0f;
            bool apply = false;
            if (type == singles._param._special_type.lewd_pv)
            {
                if (success)
                {
                    if (fan.gender == resources.fanType.male)
                    { coefficient = mainCoefficient; apply = true; }
                    else if (fan.hardcoreness == resources.fanType.hardcore)
                    { coefficient = secondaryCoefficient; apply = true; }
                }
                else if (fan.gender == resources.fanType.female)
                { coefficient = mainCoefficient; apply = true; }
                else if (fan.hardcoreness == resources.fanType.casual)
                { coefficient = secondaryCoefficient; apply = true; }
            }
            else if (type == singles._param._special_type.edgy_pv)
            {
                if (success)
                {
                    if (fan.gender == resources.fanType.teen)
                    { coefficient = mainCoefficient; apply = true; }
                    else if (fan.hardcoreness == resources.fanType.female)
                    { coefficient = secondaryCoefficient; apply = true; }
                }
                else if (fan.gender == resources.fanType.adult)
                { coefficient = mainCoefficient; apply = true; }
                else if (fan.hardcoreness == resources.fanType.youngAdult)
                { coefficient = secondaryCoefficient; apply = true; }
            }
            else if (type == singles._param._special_type.artsy_pv)
            {
                if (success)
                {
                    if (fan.gender == resources.fanType.adult)
                    { coefficient = mainCoefficient; apply = true; }
                    else if (fan.hardcoreness == resources.fanType.female)
                    { coefficient = secondaryCoefficient; apply = true; }
                }
                else if (fan.gender == resources.fanType.teen)
                { coefficient = mainCoefficient; apply = true; }
                else if (fan.hardcoreness == resources.fanType.male)
                { coefficient = secondaryCoefficient; apply = true; }
            }
            else if (type == singles._param._special_type.ad_campaign ||
                type == singles._param._special_type.viral_campaign ||
                (type == singles._param._special_type.fake_scandal && success))
            {
                coefficient = mainCoefficient;
                apply = true;
            }
            return apply ? RoundSingleCompatible(sales, coefficient,
                "singles.ValueAfterMarketing") : sales;
        }

        internal static bool SingleNeedsWideSalesPath(singles owner, singles._single single)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (single == null) throw new ArgumentNullException(nameof(single));
            if (SinglesFameNewFansBaseCoeff == null)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "singles.GetFameNewFansBaseCoeff no longer matches the audited shape.");
                throw new MissingMethodException(typeof(singles).FullName,
                    "GetFameNewFansBaseCoeff");
            }

            // GenerateSales recalculates this derived cache before consuming it. Do the
            // same before choosing the path so mod-expanded appeal coefficients cannot
            // hide an unsafe Int32/Single intermediate from the gate.
            single.RecalcFanAppeal(single.GetSenbatsuStats(), true);
            bool digital = single.IsDigital();
            float fame = single.GetFameOfTheSenbatsu();
            float fameSales = 1f + fame * 0.05f;
            float fameNewFans = (float)SinglesFameNewFansBaseCoeff.Invoke(owner, null);
            if (fameSales > 1f + fameNewFans) fameSales = 1f + fameNewFans;
            float recentFameCoefficient = 1f;
            float recentFame = 0f;
            List<singles._single> recent = singles.GetLatestReleasedSingles(3);
            if (recent.Count > 0)
            {
                foreach (singles._single prior in recent)
                    recentFame += Math.Min(prior.ReleaseData.Fame_Of_The_Senbatsu, 10f);
                recentFame /= recent.Count;
            }
            if (recentFame > fame)
                recentFameCoefficient = 1f - (recentFame - fame) / 10f;
            float saturation = singles.GetSaturationCoeff(single.GetGroup());
            float award = Awards.HasAward(Awards._type.best_single) ? 2f :
                (Awards.HasNomination(Awards._type.best_single) ? 1.25f : 1f);
            Groups._group group = single.GetGroup();
            singles._param risky = single.GetRiskyMarketing();
            float marketingMain = 0f;
            float marketingSecondary = 0f;
            if (risky != null)
            {
                marketingMain = 1f + risky.GetSuccessModifier(
                    single.Marketing_Result_Status, false) / 100f;
                marketingSecondary = 1f + risky.GetSuccessModifier(
                    single.Marketing_Result_Status, true) / 100f;
            }
            foreach (resources._fan fan in resources.Fans)
            {
                long population = group == null
                    ? fan.GetNumberOfPeople()
                    : WideNumericRepair.CalculateGroupFansByType(group,
                        fan.gender, fan.hardcoreness, fan.age);
                if (group != null && group.IsMain())
                {
                    foreach (data_girls.girls girl in single.girls)
                        if (girl != null && girl.GetGroup() != group)
                            population = WideNumericRepair.Add(population,
                                girl.GetFan_Count(fan.gender, fan.hardcoreness, fan.age),
                                "singles.GenerateSales crossover fans");
                }
                if (NeedsWideFanPath(population)) return true;

                float appeal = fan.GetAppeal(single) / 2f;
                float originalAppeal = appeal;
                if (digital && appeal > 1f) appeal = 1f;
                float salesCoefficient = appeal * fameSales * recentFameCoefficient *
                    saturation * ScandalPoints.GetSingleSalesCoeff(-1);
                if (fan.hardcoreness == resources.fanType.hardcore && !digital &&
                    !single.IsGroupHS() && !single.IsIndividualHS())
                {
                    if (staticVars.IsNormal()) salesCoefficient *= 0.75f;
                    else if (staticVars.IsHard()) salesCoefficient *= 0.55f;
                }
                if (single.id == tasks.Story_Data.substory_agnostic_single)
                    salesCoefficient *= 10f;
                if (SingleProductLeavesExactDomain(population, salesCoefficient))
                    return true;

                long units = RoundSingleCompatible(population, salesCoefficient,
                    "singles.GenerateSales path selection");
                if (units < 0L) units = 0L;
                if (staticVars.IsHard())
                {
                    units = ReduceSingleSalesAbove(units, 10000L, 2L);
                    units = ReduceSingleSalesAbove(units, 30000L, 3L);
                    units = ReduceSingleSalesAbove(units, 50000L, 5L);
                    units = ReduceSingleSalesAbove(units, 100000L, 10L);
                }
                long threshold = fan.hardcoreness == resources.fanType.hardcore
                    ? 50000L : 200000L;
                if (units > threshold)
                {
                    long excess = units - threshold;
                    float endpoint = fan.hardcoreness == resources.fanType.hardcore
                        ? (single.IsGroupHS() ? 75000f : 50000f) +
                            (single.IsIndividualHS() ? 30000f : 0f)
                        : 200000f;
                    float curve = excess >= (long)endpoint ? 1f :
                        10f - 9f * ((float)excess / endpoint);
                    if (curve < 1f) curve = 1f;
                    units = threshold + RoundSingleCompatible(excess, curve / 10f,
                        "singles.GenerateSales path-selection diminishing sales");
                }
                long sales = risky == null ? units : ValueAfterSingleMarketing(
                    units, marketingMain, marketingSecondary, fan,
                    risky.Special_Type, single.Marketing_Result_Status);
                if (NeedsWideFanPath(sales)) return true;
                if (group == null || group.IsMain())
                {
                    if (SingleProductLeavesExactDomain(
                        units, originalAppeal, fameNewFans, award)) return true;
                    long newFans = RoundSingleProductCompatible(
                        units, "singles.GenerateSales path-selection new fans",
                        originalAppeal, fameNewFans, award);
                    if (risky != null)
                        newFans = ValueAfterSingleMarketing(newFans, marketingMain,
                            marketingSecondary, fan, risky.Special_Type,
                            single.Marketing_Result_Status);
                    if (NeedsWideFanPath(newFans)) return true;
                }
            }
            return false;
        }

        private static bool SingleProductLeavesExactDomain(
            long value,
            params float[] coefficients)
        {
            if (value < -ExactSingleIntegerBoundary ||
                value > ExactSingleIntegerBoundary) return true;
            float product = (float)value;
            if (coefficients == null) return false;
            foreach (float coefficient in coefficients)
            {
                product *= coefficient;
                if (float.IsNaN(product) || float.IsInfinity(product) ||
                    product < -ExactSingleIntegerBoundary ||
                    product > ExactSingleIntegerBoundary) return true;
            }
            return false;
        }

        internal static void GenerateSingleSalesWide(singles owner, singles._single single)
        {
            if (SinglesFameNewFansBaseCoeff == null)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "singles.GetFameNewFansBaseCoeff no longer matches the audited shape.");
                throw new MissingMethodException(typeof(singles).FullName,
                    "GetFameNewFansBaseCoeff");
            }
            single.RecalcFanAppeal(single.GetSenbatsuStats(), true);
            List<singles._single._sales> plannedSales =
                new List<singles._single._sales>();
            bool startAgnosticDialogue =
                single.id == tasks.Story_Data.substory_agnostic_single &&
                !tasks.Story_Data.substory_agnostic_last_event_started;
            float saturation = singles.GetSaturationCoeff(single.GetGroup());
            bool digital = single.IsDigital();
            float fame = single.GetFameOfTheSenbatsu();
            float fameSales = 1f + fame * 0.05f;
            float fameNewFans = (float)SinglesFameNewFansBaseCoeff.Invoke(owner, null);
            if (fameSales > 1f + fameNewFans) fameSales = 1f + fameNewFans;
            float recentFameCoefficient = 1f;
            float recentFame = 0f;
            List<singles._single> recent = singles.GetLatestReleasedSingles(3);
            if (recent.Count > 0)
            {
                foreach (singles._single prior in recent)
                    recentFame += Math.Min(prior.ReleaseData.Fame_Of_The_Senbatsu, 10f);
                recentFame /= recent.Count;
            }
            if (recentFame > fame) recentFameCoefficient = 1f - (recentFame - fame) / 10f;
            float diminishingEnd = 50000f;
            if (single.IsGroupHS()) diminishingEnd += 25000f;
            if (single.IsIndividualHS()) diminishingEnd += 30000f;
            float award = Awards.HasAward(Awards._type.best_single) ? 2f :
                (Awards.HasNomination(Awards._type.best_single) ? 1.25f : 1f);
            Groups._group group = single.GetGroup();
            singles._param risky = single.GetRiskyMarketing();
            float marketingMain = 0f, marketingSecondary = 0f;
            if (risky != null)
            {
                marketingMain = 1f + risky.GetSuccessModifier(
                    single.Marketing_Result_Status, false) / 100f;
                marketingSecondary = 1f + risky.GetSuccessModifier(
                    single.Marketing_Result_Status, true) / 100f;
            }
            foreach (resources._fan fan in resources.Fans)
            {
                singles._single._sales sale = new singles._single._sales { fan = fan };
                float appeal = fan.GetAppeal(single) / 2f;
                float originalAppeal = appeal;
                long population = group == null ? fan.GetNumberOfPeople() :
                    WideNumericRepair.CalculateGroupFansByType(group,
                        fan.gender, fan.hardcoreness, fan.age);
                if (group != null && group.IsMain())
                    foreach (data_girls.girls girl in single.girls)
                        if (girl != null && girl.GetGroup() != group)
                            population = WideNumericRepair.Add(population,
                                girl.GetFan_Count(fan.gender, fan.hardcoreness, fan.age),
                                "singles.GenerateSales crossover fans");
                if (digital && appeal > 1f) appeal = 1f;
                float salesCoefficient = appeal * fameSales * recentFameCoefficient *
                    saturation * ScandalPoints.GetSingleSalesCoeff(-1);
                if (fan.hardcoreness == resources.fanType.hardcore && !digital &&
                    !single.IsGroupHS() && !single.IsIndividualHS())
                {
                    if (staticVars.IsNormal()) salesCoefficient *= 0.75f;
                    else if (staticVars.IsHard()) salesCoefficient *= 0.55f;
                }
                if (single.id == tasks.Story_Data.substory_agnostic_single)
                {
                    salesCoefficient *= 10f;
                }
                long units = RoundSingleCompatible(population, salesCoefficient,
                    "singles.GenerateSales base sales");
                if (units < 0L) units = 0L;
                if (staticVars.IsHard())
                {
                    units = ReduceSingleSalesAbove(units, 10000L, 2L);
                    units = ReduceSingleSalesAbove(units, 30000L, 3L);
                    units = ReduceSingleSalesAbove(units, 50000L, 5L);
                    units = ReduceSingleSalesAbove(units, 100000L, 10L);
                }
                long threshold = fan.hardcoreness == resources.fanType.hardcore
                    ? 50000L : 200000L;
                if (units > threshold)
                {
                    long excess = WideNumericRepair.Subtract(units, threshold,
                        "singles.GenerateSales diminishing excess");
                    float endpoint = fan.hardcoreness == resources.fanType.hardcore
                        ? diminishingEnd : 200000f;
                    float curve = excess >= (long)endpoint ? 1f :
                        10f - 9f * ((float)excess / endpoint);
                    if (curve < 1f) curve = 1f;
                    excess = RoundSingleCompatible(excess, curve / 10f,
                        "singles.GenerateSales diminishing sales");
                    units = WideNumericRepair.Add(threshold, excess,
                        "singles.GenerateSales diminished total");
                }
                sale.sales = risky == null ? units : ValueAfterSingleMarketing(
                    units, marketingMain, marketingSecondary, fan,
                    risky.Special_Type, single.Marketing_Result_Status);
                long newFans;
                if (group != null && !group.IsMain())
                    newFans = GetSisterGroupNewFans(group, fan.gender,
                        fan.hardcoreness, fan.age);
                else
                    newFans = RoundSingleProductCompatible(
                        units, "singles.GenerateSales new fans",
                        originalAppeal, fameNewFans, award);
                sale.new_fans = risky == null ? newFans : ValueAfterSingleMarketing(
                    newFans, marketingMain, marketingSecondary, fan,
                    risky.Special_Type, single.Marketing_Result_Status);
                plannedSales.Add(sale);
            }
            RedistributePhysicalSingleSales(single, plannedSales, digital);

            single.sales.Clear();
            single.sales.AddRange(plannedSales);
            if (startAgnosticDialogue)
            {
                tasks.Story_Data.substory_agnostic_last_event_started = true;
                Substories_Manager.StartDialogue("agnostic_40",
                    staticVars.dateTime.AddDays(1.0), false);
                tasks.Story_Data.substory_agnostic_girl = -1;
            }
        }

        private static long ReduceSingleSalesAbove(long value, long threshold, long divisor)
        {
            if (value <= threshold) return value;
            long excess = WideNumericRepair.Subtract(value, threshold,
                "singles.GenerateSales hard-mode excess");
            return WideNumericRepair.Add(threshold,
                WideNumericRepair.DivideRoundToEven(excess, divisor,
                    "singles.GenerateSales hard-mode reduction"),
                "singles.GenerateSales hard-mode total");
        }

        private static long GetSisterGroupNewFans(Groups._group group,
            resources.fanType gender, resources.fanType hardcoreness, resources.fanType age)
        {
            int points = group.GetPoints(gender, hardcoreness, age);
            int lower, upper, lowerPoints, upperPoints;
            if (points <= 5) { lower = 0; upper = 100; lowerPoints = 0; upperPoints = 5; }
            else if (points <= 10) { lower = 100; upper = 200; lowerPoints = 5; upperPoints = 10; }
            else if (points <= 15) { lower = 200; upper = 300; lowerPoints = 10; upperPoints = 15; }
            else if (points <= 20) { lower = 300; upper = 500; lowerPoints = 15; upperPoints = 20; }
            else if (points <= 25) { lower = 500; upper = 800; lowerPoints = 20; upperPoints = 25; }
            else if (points <= 30) { lower = 800; upper = 1300; lowerPoints = 25; upperPoints = 30; }
            else if (points <= 35) { lower = 1300; upper = 2000; lowerPoints = 30; upperPoints = 35; }
            else if (points <= 40) { lower = 2000; upper = 3000; lowerPoints = 35; upperPoints = 40; }
            else if (points <= 45) { lower = 3000; upper = 4500; lowerPoints = 40; upperPoints = 45; }
            else if (points <= 50) { lower = 4500; upper = 7000; lowerPoints = 45; upperPoints = 50; }
            else if (points <= 65) { lower = 7000; upper = 10000; lowerPoints = 50; upperPoints = 65; }
            else if (points <= 80) { lower = 10000; upper = 15000; lowerPoints = 65; upperPoints = 80; }
            else if (points <= 90) { lower = 15000; upper = 25000; lowerPoints = 80; upperPoints = 90; }
            else return 0L;
            float step = (float)(upper - lower) / (float)(upperPoints - lowerPoints);
            int baseValue = lower + Mathf.RoundToInt(step * (float)(points - lowerPoints));
            if (hardcoreness == resources.fanType.casual)
                baseValue = checked(baseValue * 4);
            long exactFans = WideNumericRepair.CalculateGroupFansByType(
                group, gender, hardcoreness, age);
            int maxFans = Groups.GetMaxFans(gender, hardcoreness, age);
            long result;
            if (exactFans <= maxFans || maxFans <= 0)
            {
                result = baseValue;
            }
            else if (exactFans >= WideNumericRepair.Multiply(maxFans, 2L,
                "Groups._group.GetNewFansPerSingle saturation"))
            {
                result = RoundSingleCompatible(baseValue, 0.01f,
                    "Groups._group.GetNewFansPerSingle minimum");
            }
            else
            {
                long numerator = WideNumericRepair.Subtract(
                    WideNumericRepair.Multiply(maxFans, 2L,
                        "Groups._group.GetNewFansPerSingle coefficient"),
                    exactFans, "Groups._group.GetNewFansPerSingle coefficient");
                result = WideNumericMath.RoundRatioToEven(baseValue, numerator, maxFans);
            }
            return TbsBalancePatchWideNumericInterop.ApplySisterGroupMultiplier(result);
        }

        private static long SumSingleSales(List<singles._single._sales> sales)
        {
            long total = 0L;
            foreach (singles._single._sales sale in sales)
                total = WideNumericRepair.Add(total, sale.sales,
                    "singles.GenerateSales planned total");
            return total;
        }

        private static void RedistributePhysicalSingleSales(
            singles._single single,
            List<singles._single._sales> sales,
            bool digital)
        {
            long total = SumSingleSales(sales);
            if (digital || total <= single.qty) return;
            foreach (singles._single._sales sale in sales)
                sale.sales = WideNumericRepair.RoundProductRatioWithSingleProductsToEven(
                    sale.sales, single.qty, total,
                    "singles.GenerateSales physical redistribution");
            long excess = WideNumericRepair.Subtract(SumSingleSales(sales),
                single.qty, "singles.GenerateSales redistribution excess");
            while (excess > 0L)
            {
                bool changed = false;
                foreach (singles._single._sales sale in sales)
                {
                    if (excess <= 0L) break;
                    if (sale.sales <= 0L) continue;
                    sale.sales = WideNumericRepair.Subtract(sale.sales, 1L,
                        "singles.GenerateSales redistribution residue");
                    excess--;
                    changed = true;
                }
                if (!changed)
                {
                    WideNumericRepair.LatchInvariantFailure(
                        "singles.GenerateSales could not conserve physical quantity.");
                    throw new InvalidOperationException("Physical single redistribution stalled.");
                }
            }
            if ((long)single.qty - SumSingleSales(sales) == 1L)
            {
                singles._single._sales target = sales.Find(item =>
                    item.fan != null && item.fan.gender == resources.fanType.male &&
                    item.fan.hardcoreness == resources.fanType.hardcore &&
                    item.fan.age == resources.fanType.adult);
                if (target == null)
                {
                    WideNumericRepair.LatchInvariantFailure(
                        "singles.GenerateSales could not resolve its final redistribution demographic.");
                    throw new InvalidOperationException(
                        "Single redistribution target demographic is missing.");
                }
                target.sales = WideNumericRepair.Add(target.sales, 1L,
                    "singles.GenerateSales redistribution final unit");
            }
        }

        private sealed class TourCalculation
        {
            internal int Stamina;
            internal long ProductionCost;
            internal long ExpectedRevenue;
            internal long Saving;
            internal readonly HashSet<SEvent_Tour.country> DiscountedCountries =
                new HashSet<SEvent_Tour.country>();
        }

        internal static void SelectTourCountry(
            SEvent_Tour.tour tour,
            SEvent_Tour.country country,
            int level)
        {
            if (tour == null) throw new ArgumentNullException(nameof(tour));
            if (country == null) throw new ArgumentNullException(nameof(country));
            if (tour.SelectedCountries == null)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "SEvent_Tour.tour.SelectCountry has a null selected-country set.");
                throw new InvalidOperationException("Tour selected-country set is null.");
            }

            SEvent_Tour.tour.selectedCountry current = tour.GetCountry(country);
            List<SEvent_Tour.tour.selectedCountry> projected =
                new List<SEvent_Tour.tour.selectedCountry>(tour.SelectedCountries.Count + 1);
            foreach (SEvent_Tour.tour.selectedCountry selected in tour.SelectedCountries)
            {
                if (selected == null || selected.Country == null)
                {
                    WideNumericRepair.LatchInvariantFailure(
                        "SEvent_Tour.tour.SelectCountry encountered a null selected country.");
                    throw new InvalidOperationException("Tour contains a null selected country.");
                }
                if (selected == current)
                {
                    if (selected.Level != level)
                    {
                        projected.Add(new SEvent_Tour.tour.selectedCountry
                        {
                            Country = selected.Country,
                            Level = level
                        });
                    }
                    continue;
                }
                projected.Add(selected);
            }
            if (current == null)
            {
                projected.Add(new SEvent_Tour.tour.selectedCountry
                {
                    Country = country,
                    Level = level
                });
            }

            TourCalculation calculation = CalculateTour(projected);

            if (current == null)
            {
                tour.SelectedCountries.Add(new SEvent_Tour.tour.selectedCountry
                {
                    Country = country,
                    Level = level
                });
            }
            else if (current.Level == level)
            {
                tour.SelectedCountries.Remove(current);
            }
            else
            {
                current.Level = level;
            }
            ApplyTourCalculation(tour, calculation);
        }

        internal static void RecalculateTour(SEvent_Tour.tour tour)
        {
            if (tour == null) throw new ArgumentNullException(nameof(tour));
            TourCalculation calculation = CalculateTour(tour.SelectedCountries);
            ApplyTourCalculation(tour, calculation);
        }

        private static TourCalculation CalculateTour(
            IList<SEvent_Tour.tour.selectedCountry> selectedCountries)
        {
            if (selectedCountries == null) throw new ArgumentNullException(nameof(selectedCountries));
            long rawProduction = 0L;
            long expectedRevenue = 0L;
            long stamina = 0L;
            HashSet<SEvent_Tour.country> identities = new HashSet<SEvent_Tour.country>();
            TourCalculation result = new TourCalculation();

            for (int index = 0; index < selectedCountries.Count; index++)
            {
                SEvent_Tour.tour.selectedCountry selected = selectedCountries[index];
                if (selected == null || selected.Country == null)
                {
                    WideNumericRepair.LatchInvariantFailure(
                        "SEvent_Tour.tour calculation encountered a null selected country.");
                    throw new InvalidOperationException("Tour contains a null selected country.");
                }
                if (!identities.Add(selected.Country))
                {
                    WideNumericRepair.LatchInvariantFailure(
                        "SEvent_Tour.tour contains the same country more than once.");
                    throw new InvalidOperationException("Tour country identity is duplicated.");
                }
                stamina = WideNumericRepair.Add(stamina,
                    selected.Country.GetStaminaCost(),
                    "SEvent_Tour.tour.RecalcStamina");
                long cost = WideNumericRepair.Multiply(
                    selected.Country.Cost,
                    SEvent_Tour.GetCostCoeff(selected.Level),
                    "SEvent_Tour.tour.RecalcProductionCost country cost");
                rawProduction = WideNumericRepair.Add(rawProduction, cost,
                    "SEvent_Tour.tour.RecalcProductionCost aggregate");

                long grossRevenue = WideNumericRepair.Multiply(
                    SEvent_Tour.GetCapacity(selected.Level),
                    selected.Country.TicketPrice,
                    "SEvent_Tour.country.GetRevenueByLevel base");
                long countryRevenue = RoundSingleProductCompatible(
                    grossRevenue,
                    "SEvent_Tour.country.GetRevenueByLevel coefficients",
                    SEvent_Tour.GetFamePenalty(selected.Level),
                    selected.Country.GetFatiguePenalty());
                expectedRevenue = WideNumericRepair.Add(expectedRevenue, countryRevenue,
                    "SEvent_Tour.tour.RecalcExpectedRevenue");
            }

            float coefficient = 1f;
            if (Awards.HasAward(Awards._type.most_prolific_group)) coefficient = 0.5f;
            else if (Awards.HasNomination(Awards._type.most_prolific_group)) coefficient = 0.75f;
            if (variables.Get("CHEAPER_EVENTS") == "true" &&
                staticVars.PlayerData.Chapter < tasks._chapter.chapter_5)
                coefficient *= 0.75f;
            result.ProductionCost = RoundSingleCompatible(rawProduction, coefficient,
                "SEvent_Tour.tour.RecalcProductionCost coefficient");
            result.ExpectedRevenue = expectedRevenue;
            result.Stamina = WideNumericRepair.ToInt32Exact(stamina,
                "SEvent_Tour.tour.RecalcStamina result");

            long saving = 0L;
            foreach (SEvent_Tour._area area in SEvent_Tour.Const_Area)
            {
                long areaTotal = 0L;
                long largest = 0L;
                SEvent_Tour.country discounted = null;
                foreach (SEvent_Tour.tour.selectedCountry selected in selectedCountries)
                {
                    if (selected.Country.Area != area) continue;
                    long cost = GetTourCountryCost(selected.Country, selected.Level);
                    long savedCost = WideNumericRepair.Subtract(
                        cost,
                        RoundSingleCompatible(cost, area.Saving,
                            "SEvent_Tour.country.GetSaving"),
                        "SEvent_Tour.country.GetSaving difference");
                    areaTotal = WideNumericRepair.Add(areaTotal, savedCost,
                        "SEvent_Tour.tour.GetSaving area total");
                    if (savedCost > largest)
                    {
                        largest = savedCost;
                        discounted = selected.Country;
                    }
                }
                saving = WideNumericRepair.Add(saving,
                    WideNumericRepair.Subtract(areaTotal, largest,
                        "SEvent_Tour.tour.GetSaving largest discount"),
                    "SEvent_Tour.tour.GetSaving aggregate");
                if (discounted != null) result.DiscountedCountries.Add(discounted);
            }
            result.Saving = saving;
            return result;
        }

        private static void ApplyTourCalculation(
            SEvent_Tour.tour tour,
            TourCalculation calculation)
        {
            tour.Stamina = calculation.Stamina;
            foreach (SEvent_Tour.tour.selectedCountry selected in tour.SelectedCountries)
                selected.Discount = calculation.DiscountedCountries.Contains(selected.Country);
            WideNumericState.SetTourComputed(tour, calculation.ProductionCost,
                calculation.ExpectedRevenue, calculation.Saving);
        }

        internal static int GetTourAttendance(
            SEvent_Tour.tour tour,
            SEvent_Tour.tour.selectedCountry selected)
        {
            if (tour == null) throw new ArgumentNullException(nameof(tour));
            if (selected == null || selected.Country == null)
                throw new ArgumentNullException(nameof(selected));
            int capacity = SEvent_Tour.GetCapacity(selected.Level);
            float randomCoefficient =
                (100f - (float)UnityEngine.Random.Range(0,
                    tour.GetTourAttendanceCoeff())) / 100f;
            float famePenalty = SEvent_Tour.GetFamePenalty(selected.Level);
            float fatiguePenalty = selected.Country.GetFatiguePenalty();
            float vanilla = (float)capacity * randomCoefficient *
                famePenalty * fatiguePenalty;
            if (capacity >= -ExactSingleIntegerBoundary &&
                capacity <= ExactSingleIntegerBoundary &&
                !float.IsNaN(vanilla) && !float.IsInfinity(vanilla) &&
                vanilla >= -ExactSingleIntegerBoundary &&
                vanilla <= ExactSingleIntegerBoundary)
                return Mathf.RoundToInt(vanilla);

            long exact = WideNumericRepair.RoundSingleProductToEven(
                capacity, "SEvent_Tour.tour.GetAttendance",
                randomCoefficient, famePenalty, fatiguePenalty);
            return WideNumericRepair.ToInt32Exact(exact,
                "SEvent_Tour.tour.GetAttendance result");
        }

        internal static int GetTourNewFansByAttendance(int attendance)
        {
            float coefficient = UnityEngine.Random.Range(1f, 20f) / 100f;
            float vanilla = (float)attendance * coefficient;
            if (attendance >= -ExactSingleIntegerBoundary &&
                attendance <= ExactSingleIntegerBoundary &&
                !float.IsNaN(vanilla) && !float.IsInfinity(vanilla) &&
                vanilla >= -ExactSingleIntegerBoundary &&
                vanilla <= ExactSingleIntegerBoundary)
                return Mathf.RoundToInt(vanilla);
            long exact = WideNumericRepair.RoundSingleProductToEven(
                attendance, "SEvent_Tour.tour.GetNewFansByAttendance", coefficient);
            return WideNumericRepair.ToInt32Exact(exact,
                "SEvent_Tour.tour.GetNewFansByAttendance result");
        }

        internal static long GetTourCountryCost(SEvent_Tour.country country, int level)
        {
            if (country == null) throw new ArgumentNullException(nameof(country));
            return WideNumericRepair.Multiply(country.Cost,
                SEvent_Tour.GetCostCoeff(level),
                "SEvent_Tour.country.GetCostByLevel");
        }

        internal static long GetTourCountryRevenueByLevel(
            SEvent_Tour.country country,
            int level)
        {
            if (country == null) throw new ArgumentNullException(nameof(country));
            long gross = WideNumericRepair.Multiply(
                SEvent_Tour.GetCapacity(level), country.TicketPrice,
                "SEvent_Tour.country.GetRevenueByLevel base");
            return RoundSingleProductCompatible(gross,
                "SEvent_Tour.country.GetRevenueByLevel coefficients",
                SEvent_Tour.GetFamePenalty(level), country.GetFatiguePenalty());
        }

        internal static long GetTourCountryRevenueByAttendance(
            SEvent_Tour.country country,
            int attendance)
        {
            if (country == null) throw new ArgumentNullException(nameof(country));
            return WideNumericRepair.Multiply(attendance, country.TicketPrice,
                "SEvent_Tour.country.GetRevenueByCapacity");
        }

        internal static long GetTourCountrySaving(SEvent_Tour.country country, int level)
        {
            long cost = GetTourCountryCost(country, level);
            return WideNumericRepair.Subtract(cost,
                RoundSingleCompatible(cost, country.Area.Saving,
                    "SEvent_Tour.country.GetSaving coefficient"),
                "SEvent_Tour.country.GetSaving");
        }

        internal static int GetTourTotalAudienceCompatibility(SEvent_Tour.tour tour)
        {
            return WideNumericMath.ClampToInt32(WideNumericState.GetTourTotalAudience(tour));
        }

        internal static int GetTourCountryFansCompatibility(SEvent_Tour.tour tour)
        {
            return WideNumericMath.ClampToInt32(WideNumericState.GetTourTotalCountryFans(tour));
        }

        internal static void RenderTourBar(Tour_Popup popup)
        {
            if (popup == null || popup.Tour == null) return;
            long fans = WideNumericState.GetTourNewFans(popup.Tour);
            long revenue = WideNumericState.GetTourRevenue(popup.Tour);
            long profit = WideNumericState.GetTourProfit(popup.Tour);
            ExtensionMethods.SetText(popup.NewFans,
                ExtensionMethods.formatNumber(fans, false, false));
            ExtensionMethods.SetText(popup.Revenue,
                ExtensionMethods.formatMoney(revenue, false, false, false));
            ExtensionMethods.SetText(popup.Profit,
                ExtensionMethods.formatMoney(profit, false, false, false));
            ExtensionMethods.SetColor(popup.Profit,
                profit < 0L ? mainScript.red32 : mainScript.green32);
        }

        internal static void CorrectNewTourPopup(Tour_New_Popup popup)
        {
            if (popup == null || popup.Tour == null) return;
            long production = WideNumericState.GetTourProductionCost(popup.Tour);
            long saving = WideNumericState.GetTourSaving(popup.Tour);
            long expected = WideNumericState.GetTourExpectedRevenue(popup.Tour);
            if (production == popup.Tour.ProductionCost &&
                saving == popup.Tour.Saving &&
                expected == popup.Tour.ExpectedRevenue)
                return;

            if (saving > 0L)
                ExtensionMethods.SetText(popup.ProductionTitle,
                    Language.Insert("TOUR__SAVING", new string[]
                    {
                        ExtensionMethods.color(
                            ExtensionMethods.formatMoney(saving, false, false),
                            mainScript.green)
                    }));
            else
                ExtensionMethods.SetText(popup.ProductionTitle,
                    Language.Data["TOUR__PROD_COST"]);
            ExtensionMethods.SetText(popup.ProductionCost,
                ExtensionMethods.formatMoney(
                    WideNumericState.GetTourNetProductionCost(popup.Tour), false, false));
            ExtensionMethods.SetText(popup.ExpectedRevenue,
                ExtensionMethods.formatMoney(expected, false, false));
            long effectiveProduction = WideNumericState.GetTourNetProductionCost(popup.Tour);
            ExtensionMethods.SetColor(popup.ExpectedRevenue,
                expected < effectiveProduction ? mainScript.red32 : mainScript.green32);
        }

        internal static void CorrectTourProjectButton(SEvent_Button_Tour view)
        {
            if (view == null || view.Tour == null || view.Subtitle == null) return;
            long production = WideNumericState.GetTourProductionCost(view.Tour);
            if (production == view.Tour.ProductionCost) return;
            ExtensionMethods.SetText(view.Subtitle, string.Concat(new string[]
            {
                Language.Data["COST"], ": ",
                ExtensionMethods.formatMoney(production, false, false), " | ",
                Language.Data["STAMINA"], ": ",
                ExtensionMethods.formatNumber(view.Tour.Stamina, false, false),
                Language.Data["PT"]
            }));
        }

        internal static bool TryAnimateTourAttendanceWide(
            Tour_Popup_Country popup,
            int attendance)
        {
            SEvent_Tour.tour tour;
            SEvent_Tour.tour.selectedCountry selected;
            ResolveTourPopupCountry(popup, out tour, out selected);
            long revenue = GetTourCountryRevenueByAttendance(selected.Country, attendance);
            if (revenue >= int.MinValue && revenue <= int.MaxValue) return false;

            if (TourPopupAttendanceString == null)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Tour_Popup_Country.AttendanceString no longer matches the audited shape.");
                throw new MissingMethodException(typeof(Tour_Popup_Country).FullName,
                    "AttendanceString");
            }

            int ordinal = tour.SelectedCountries.IndexOf(selected);
            if (ordinal < 0)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Tour_Popup_Country could not locate its selected-country identity.");
                throw new InvalidOperationException("Tour popup country is orphaned.");
            }
            int capacity = SEvent_Tour.GetCapacity(selected.Level);
            if (capacity <= 0)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Tour_Popup_Country has a nonpositive venue capacity.");
                throw new InvalidOperationException("Tour country capacity is nonpositive.");
            }
            int attendancePercent = Mathf.RoundToInt(
                (float)attendance / (float)capacity * 100f);
            long existingFans = WideNumericState.GetTourCountryFans(tour, ordinal);
            long adjustedRevenue =
                TbsBalancePatchWideNumericInterop.ApplyTourRevenueMultiplier(revenue);
            WideNumericRepair.Add(WideNumericState.GetTourRevenue(tour), adjustedRevenue,
                "Tour_Popup_Country.AnimateAttendance tour revenue preflight");

            selected.Attendance = attendancePercent;
            WideNumericState.SetTourCountryValues(tour, ordinal, attendance,
                existingFans, revenue);
            WideNumericState.AddTourRevenue(tour, adjustedRevenue);

            string attendanceText = (string)TourPopupAttendanceString.Invoke(
                popup, new object[] { attendance, selected.Level });
            ExtensionMethods.SetText(popup.Attendance, attendanceText);
            ExtensionMethods.SetText(popup.Revenue,
                ExtensionMethods.formatMoney(revenue, false, false, false));
            ExtensionMethods.SetColor(popup.Revenue,
                revenue > GetTourCountryCost(selected.Country, selected.Level)
                    ? mainScript.green32 : mainScript.red32);
            return true;
        }

        internal static void SynchronizeTourPopupCountry(Tour_Popup_Country popup)
        {
            SEvent_Tour.tour tour;
            SEvent_Tour.tour.selectedCountry selected;
            ResolveTourPopupCountry(popup, out tour, out selected);
            int ordinal = tour.SelectedCountries.IndexOf(selected);
            if (ordinal < 0) return;
            WideNumericState.SetTourCountryValues(tour, ordinal,
                selected.Audience, selected.NewFans, selected.Revenue);
        }

        private static void ResolveTourPopupCountry(
            Tour_Popup_Country popup,
            out SEvent_Tour.tour tour,
            out SEvent_Tour.tour.selectedCountry selected)
        {
            if (popup == null || TourPopupCountryTour == null ||
                TourPopupCountryCountry == null)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Tour_Popup_Country private bindings no longer match the audited shape.");
                throw new MissingFieldException(typeof(Tour_Popup_Country).FullName,
                    "Tour/Country");
            }
            tour = TourPopupCountryTour.GetValue(popup) as SEvent_Tour.tour;
            selected = TourPopupCountryCountry.GetValue(popup) as
                SEvent_Tour.tour.selectedCountry;
            if (tour == null || selected == null || selected.Country == null)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Tour_Popup_Country has no live tour/country binding.");
                throw new InvalidOperationException("Tour popup country is not bound.");
            }
        }

        internal static void RenderTourStarTooltip(Tour_Star star)
        {
            Tour_Country countryView = TourStarCountry == null
                ? null : TourStarCountry.GetValue(star) as Tour_Country;
            if (countryView == null || countryView.Country == null)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Tour_Star.TourCountry no longer matches the audited shape.");
                throw new MissingFieldException(typeof(Tour_Star).FullName, "TourCountry");
            }
            SEvent_Tour.country country = countryView.Country;
            int level = star.ID + 1;
            long cost = GetTourCountryCost(country, level);
            long revenue = GetTourCountryRevenueByLevel(country, level);
            int fatigue = country.GetFatiguePercent();
            string revenueColor = revenue < cost ? mainScript.red : mainScript.green;
            string fatigueColor = mainScript.green;
            if (fatigue < 50 && fatigue > 20) fatigueColor = mainScript.blue;
            else if (fatigue >= 50) fatigueColor = mainScript.red;
            string value = string.Concat(
                Language.Data["STAMINA"], ": <color=", mainScript.red, ">-",
                country.GetStaminaCost(), Language.Data["PT"], "</color>\n",
                Language.Data["COST"], ": ", mainScript.yen,
                ExtensionMethods.formatNumber(cost, false, false), "\n",
                Language.Data["SHOW__REVENUE"], ": <color=", revenueColor, ">",
                mainScript.yen, ExtensionMethods.formatNumber(revenue, false, false),
                "</color>", mainScript.separator,
                Language.Data["TOUR__FATIGUE"], ": <color=", fatigueColor, ">",
                fatigue, "%</color>");
            star.GetComponent<ButtonDefault>().SetTooltip(value);
        }

        internal static void RenderFinishedTour(
            SEvent_Button_Tour_Finished view,
            SEvent_Tour.tour tour)
        {
            ExtensionMethods.SetText(view.Countries,
                ExtensionMethods.formatNumber(tour.SelectedCountries.Count, false, false));
            ExtensionMethods.SetText(view.Attendance,
                ExtensionMethods.formatNumber(tour.GetAverageAttendance(), false, false) + "%");
            ExtensionMethods.SetText(view.Audience,
                ExtensionMethods.formatNumber(WideNumericState.GetTourTotalAudience(tour),
                    false, false));
            ExtensionMethods.SetText(view.NewFans,
                ExtensionMethods.formatNumber(WideNumericState.GetTourTotalCountryFans(tour),
                    false, false));
            long profit = WideNumericState.GetTourProfit(tour);
            string profitText = ExtensionMethods.formatMoney(profit, false, false, false);
            if (profit < 0L) profitText = ExtensionMethods.color(profitText, mainScript.red);
            ExtensionMethods.SetText(view.Profit, profitText);
            ExtensionMethods.SetText(view.Date,
                ExtensionMethods.ToString_Loc(tour.FinishDate, "DATETIME__LONG"));
        }

        internal static void FinishTour(SEvent_Tour instance)
        {
            SEvent_Tour.tour tour = instance.Tour;
            if (tour == null) return;
            long profit = WideNumericState.GetTourProfit(tour);
            long fans = WideNumericState.GetTourNewFans(tour);
            long effectiveProfit = BuffMeWideNumericInterop.PreviewResourceDelta(
                resources.type.money, profit);
            long effectiveFans = BuffMeWideNumericInterop.PreviewResourceDelta(
                resources.type.fans, fans);
            List<data_girls.girls> activeGirls = data_girls.GetActiveGirls(null);
            long earning = activeGirls.Count == 0 ? 0L : profit / activeGirls.Count;
            resources resourceOwner = instance.GetComponent<resources>();
            bool planFanCredit = NeedsWideFanPath(effectiveFans) ||
                FanDistributionMayOverflow(effectiveFans, null, null, null);
            FanMutationPlan fanPlan = null;
            long fanObserverTotal = 0L;
            if (planFanCredit)
            {
                fanObserverTotal = WideNumericRepair.Add(
                    resources.GetFansTotal(null), effectiveFans,
                    "SEvent_Tour.FinishTour fan observer preflight");
                fanPlan = new FanMutationPlan();
                BuffMeWideNumericInterop.BeginResourceFanDistribution();
                try
                {
                    PlanFansWeighted(fanPlan, effectiveFans, null, null, null);
                }
                finally
                {
                    BuffMeWideNumericInterop.EndResourceFanDistribution();
                }
            }

            // Preflight all authoritative mutations before closing UI or changing status.
            WideNumericRepair.Add(resources.Money(), effectiveProfit,
                "SEvent_Tour.FinishTour money");
            WideNumericRepair.Add(resources.GetFansTotal(null), effectiveFans,
                "SEvent_Tour.FinishTour aggregate fans");
            foreach (data_girls.girls girl in activeGirls)
                WideNumericRepair.Add(girl.Earnings_CurrentMonth, earning,
                    "SEvent_Tour.FinishTour idol earnings");

            FieldInfo popupField = AccessTools.Field(typeof(SEvent_Tour), "popupManager");
            PopupManager popup = popupField == null ? null : popupField.GetValue(instance) as PopupManager;
            if (popup != null) popup.Close();
            tour.Status = SEvent_Tour.tour._status.finished;
            tour.FinishDate = staticVars.dateTime;
            SpecialEvents_Manager.GetEvent(SpecialEvents_Manager._type.WorldTour).Status =
                SpecialEvents_Manager._specialEvent._status.normal;
            instance.UseStamina();
            resourceOwner.AddMoney(profit);
            if (fanPlan == null)
            {
                resources.Add(resources.type.fans, fans);
            }
            else
            {
                CommitResourceFanPlan(resourceOwner, fanPlan, fanObserverTotal);
            }
            foreach (data_girls.girls girl in activeGirls) girl.Earn(earning);
            tasks.OnWorldTour(tour);
            Achievements.CheckWorldTour();
            instance.Tour = null;
            instance.GetComponent<SpecialEvents_Manager>().RenderTab();
            Widget_Details.Render_Waiting();
        }

        private static void CommitResourceFanPlan(
            resources owner,
            FanMutationPlan plan,
            long observerTotal)
        {
            if (owner == null || ResourcesFansChanged == null ||
                ResourcesResourceChanged == null)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "resources fan-event bindings no longer match the audited shape.");
                throw new MissingFieldException(typeof(resources).FullName,
                    "onFansChange/onResourceChange");
            }
            resources.resource[(int)resources.type.fans] = observerTotal;
            plan.CommitMutations();
            plan.RunCallbacks();
            resources.fans fansChanged =
                ResourcesFansChanged.GetValue(owner) as resources.fans;
            resources.resourceChanged resourceChanged =
                ResourcesResourceChanged.GetValue(owner) as resources.resourceChanged;
            if (fansChanged != null) fansChanged(observerTotal);
            if (resourceChanged != null) resourceChanged();
        }

        /// <summary>
        /// Exact single-production expense shown by the new-single popup. Vanilla
        /// converts the already-wide base cost to Single before applying the award
        /// coefficient, which loses whole-yen precision once the cost grows beyond
        /// Single's exact integer range. Keep the complete calculation in checked
        /// Int64/rational arithmetic instead.
        /// </summary>
        internal static long GetSinglePopupProductionCost(Single_Popup popup)
        {
            if (popup == null || popup.Single_ == null) return 0L;
            singles._single single = popup.Single_;
            long raw = WideNumericRepair.Add(
                WideNumericRepair.Multiply(single.qty, single.GetOneCDCost(),
                    "Single_Popup.CalculateProductionCost disc copies"),
                single.GetOtherExpenses(),
                "Single_Popup.CalculateProductionCost base plus marketing");
            if (Awards.HasAward(Awards._type.most_prolific_group))
                return WideNumericRepair.DivideRoundToEven(raw, 2L,
                    "Single_Popup.CalculateProductionCost award half");
            if (Awards.HasNomination(Awards._type.most_prolific_group))
                return WideNumericRepair.DivideRoundToEven(
                    WideNumericRepair.Multiply(raw, 3L,
                        "Single_Popup.CalculateProductionCost nomination numerator"),
                    4L, "Single_Popup.CalculateProductionCost nomination three quarters");
            return raw;
        }

        internal static long GetSingleTotalNewFans(singles._single single)
        {
            long total = 0L;
            foreach (singles._single._sales sale in single.sales)
                total = WideNumericRepair.Add(total, sale.new_fans, "singles._single.GetTotalNewFans");
            return total;
        }

        internal static long GetSingleMoney(singles._single single)
        {
            return WideNumericRepair.Multiply(GetSingleTotalSales(single),
                single.GetOneCDRevenue(), "singles._single.GetMoney");
        }

        internal static void CreditSingleMoney(singles._single single)
        {
            long gross = GetSingleMoney(single);
            long profit = WideNumericRepair.Subtract(gross, single.GetProductionCost(),
                "singles.AddMoney profit");
            WideNumericRepair.Add(resources.Money(),
                BuffMeWideNumericInterop.PreviewResourceDelta(resources.type.money, profit),
                "singles.AddMoney resource preflight");

            Dictionary<data_girls.girls, long> earnings =
                new Dictionary<data_girls.girls, long>();
            if (profit >= 0L)
            {
                int populatedRows = 0;
                for (int row = 0; row < 5; row++)
                    if (single.GetGirlsOfARow(row).Count > 0) populatedRows++;
                if (populatedRows > 0)
                {
                    long rowShare = profit / populatedRows;
                    foreach (data_girls.girls girl in single.girls)
                    {
                        if (girl == null) continue;
                        long value = rowShare / (single.GetRowOfAGirl(girl) + 1L);
                        long prior;
                        earnings.TryGetValue(girl, out prior);
                        earnings[girl] = WideNumericRepair.Add(prior, value,
                            "singles.AddMoney repeated idol share");
                    }
                    foreach (KeyValuePair<data_girls.girls, long> pair in earnings)
                        WideNumericRepair.Add(pair.Key.Earnings_CurrentMonth, pair.Value,
                            "singles.AddMoney idol earnings preflight");
                }
            }

            single.ReleaseData.Profit = profit;
            resources.Add(resources.type.money, profit);
            if (profit < 0L || earnings.Count == 0) return;
            // Preserve vanilla's actual row-weighted per-slot credit order. The
            // dictionary above is only the cumulative preflight witness.
            int vanillaRows = 0;
            for (int row = 0; row < 5; row++)
                if (single.GetGirlsOfARow(row).Count > 0) vanillaRows++;
            long committedShare = profit / vanillaRows;
            foreach (data_girls.girls girl in single.girls)
                if (girl != null)
                    girl.Earn(committedShare / (single.GetRowOfAGirl(girl) + 1L));
        }

        internal static bool PrepareSingleBonusFans(singles._single single)
        {
            long ordinary = GetSingleTotalNewFans(single);
            if (!NeedsWideFanPath(ordinary)) return false;

            long hardcore = 0L;
            long casual = 0L;
            bool mostPopularGenre = single.ReleaseData.MostPopular_Genre;
            bool mostPopularChoreo = single.ReleaseData.MostPopular_Choreo;
            bool mostPopularLyrics = single.ReleaseData.MostPopular_Lyrics;
            Groups._group group = single.GetGroup();
            if (group == null || group.IsMain())
            {
                mostPopularGenre = Rivals.IsMostPopular(
                    single.genre, singles._param._type.genre);
                mostPopularChoreo = Rivals.IsMostPopular(
                    single.genre, singles._param._type.choreography);
                mostPopularLyrics = Rivals.IsMostPopular(
                    single.genre, singles._param._type.lyrics);
                float coefficient = Rivals.GetSinglesCoeff(single);
                bool hardcoreBonus = coefficient <= 0f;
                if (hardcoreBonus) coefficient *= -1f;
                long bonus = RoundSingleProductCompatible(
                    ordinary,
                    "singles.AddBonusFans rival/scandal coefficient",
                    coefficient,
                    ScandalPoints.GetBonusFansCoeff(-1));
                if (hardcoreBonus) hardcore = bonus;
                else casual = bonus;
                MonoBehaviour.print("TOT NEW FANS: " + FormatWide(bonus));
            }
            single.ReleaseData.MostPopular_Genre = mostPopularGenre;
            single.ReleaseData.MostPopular_Choreo = mostPopularChoreo;
            single.ReleaseData.MostPopular_Lyrics = mostPopularLyrics;
            WideNumericState.SetSingleReleaseFans(single, ordinary, hardcore, casual);
            return true;
        }

        internal static void SynchronizeSingleBonusFans(singles._single single)
        {
            if (single == null || single.ReleaseData == null) return;
            WideNumericState.SetSingleReleaseFans(single,
                single.ReleaseData.NewFans,
                single.ReleaseData.NewHardcoreFans,
                single.ReleaseData.NewCasualFans);
        }

        internal static bool SingleNeedsWideFanCredit(singles._single single)
        {
            WideSingleRuntime state = WideNumericState.GetSingleReleaseFans(single);
            return NeedsWideFanPath(state.Ordinary) || NeedsWideFanPath(state.Hardcore) ||
                NeedsWideFanPath(state.Casual) || NeedsWideFanPath(GetSingleTotalNewFans(single));
        }

        internal static void CreditSingleFans(singles._single single)
        {
            List<List<data_girls.girls>> rows = new List<List<data_girls.girls>>();
            for (int row = 0; row < 5; row++) rows.Add(single.GetGirlsOfARow(row));
            int populatedRows = 0;
            foreach (List<data_girls.girls> row in rows) if (row.Count > 0) populatedRows++;
            if (populatedRows == 0) return;
            WideSingleRuntime release = WideNumericState.GetSingleReleaseFans(single);
            bool isMain = single.GetGroup() == null || single.GetGroup().IsMain();
            FanMutationPlan plan = new FanMutationPlan();

            foreach (singles._single._sales sale in single.sales)
            {
                foreach (List<data_girls.girls> row in rows)
                {
                    if (row.Count == 0) continue;
                    long amount = sale.new_fans;
                    if (isMain && sale.fan.hardcoreness == resources.fanType.hardcore &&
                        release.Hardcore > 0L)
                    {
                        amount = WideNumericRepair.Add(amount,
                            WideNumericRepair.DivideRoundToEven(release.Hardcore, 6L,
                                "singles.AddFans hardcore bonus"),
                            "singles.AddFans amount");
                    }
                    else if (isMain && sale.fan.hardcoreness == resources.fanType.casual &&
                        release.Casual > 0L)
                    {
                        amount = WideNumericRepair.Add(amount,
                            WideNumericRepair.DivideRoundToEven(release.Casual, 6L,
                                "singles.AddFans casual bonus"),
                            "singles.AddFans amount");
                    }
                    long denominator = WideNumericRepair.Multiply(populatedRows, row.Count,
                        "singles.AddFans divisor");
                    long perGirl = WideNumericRepair.DivideRoundToEven(amount, denominator,
                        "singles.AddFans per idol");
                    if (perGirl < 1L) continue;
                    foreach (data_girls.girls girl in row)
                        PlanGirlFansDemographic(plan, girl, sale.fan.gender,
                            sale.fan.hardcoreness, sale.fan.age, perGirl);
                }
            }
            plan.CommitMutations();
            plan.RunCallbacks();
        }

        private static string FormatWide(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        internal static long SumShowFans(Shows._show show)
        {
            long total = 0L;
            foreach (long value in WideNumericState.GetShowFans(show))
                total = WideNumericRepair.Add(total, value, "Shows._show.GetAllNewFans");
            return total;
        }

        internal static long GetShowEpisodeFans(Shows._show show, int? episodeNumber)
        {
            List<long> values = WideNumericState.GetShowFans(show);
            int index = episodeNumber == null
                ? show.episodeCount - 1
                : episodeNumber.Value - 1;
            if (index >= values.Count) return values[values.Count - 1];
            return values[index];
        }

        internal static int SumShowFansCompatibility(Shows._show show)
        {
            return WideNumericMath.ClampToInt32(SumShowFans(show));
        }

        internal static bool ShowNeedsWideSalesPath(Shows._show show)
        {
            if (show == null) throw new ArgumentNullException(nameof(show));
            long historicalFans = SumShowFans(show);
            long fame = WideNumericRepair.Add(
                (long)resources.GetFameLevel(),
                (long)show.fame[show.fame.Count - 1],
                "Shows._show.SetSales path-selection fame");
            if (fame < 1L) fame = 1L;
            if (NeedsWideFanPath(historicalFans) || NeedsWideFanPath(fame)) return true;

            int baseAudience = GetShowBaseAudience(show);
            float baseValue = (float)fame * 0.5f * 0.1f * baseAudience;
            baseValue = (baseValue + (float)historicalFans) / 12f;
            if (SingleValueLeavesExactDomain(baseValue)) return true;
            float fanAttritionAudienceCoefficient =
                TelModLibraryInterop.GetFanAttritionAudienceCoefficient(show);
            baseValue *= fanAttritionAudienceCoefficient;
            if (SingleValueLeavesExactDomain(baseValue)) return true;

            float awardCoefficient = 1f;
            if (show.castType != Shows._show._castType.entireGroup)
            {
                foreach (data_girls.girls girl in show.GetCast())
                {
                    if (girl != null && girl.HasAward(Awards._type.variety_queen))
                        awardCoefficient = 2f;
                    else if (girl != null && girl.HasNomination(Awards._type.variety_queen))
                        awardCoefficient = 1.25f;
                }
            }
            if (variables.Get("VIEWERS_BONUS") == "true" &&
                staticVars.PlayerData.Chapter < tasks._chapter.chapter_5)
                awardCoefficient *= 1.5f;

            bool internet = show.medium.media_type == Shows._param._media_type.internet;
            float maximumRandomCoefficient = internet ? 1.14f : 1.09f;
            float fatigueCoefficient = 1f - show.GetFatigue(null) / 100f;
            long totalNewFans = 0L;
            long viralBaseNewFans = 0L;
            foreach (resources._fan fan in resources.Fans)
            {
                float audienceValue = baseValue;
                audienceValue *= show.GetFanAppeal(fan);
                audienceValue *= maximumRandomCoefficient;
                audienceValue *= awardCoefficient;
                if (SingleValueLeavesExactDomain(audienceValue)) return true;
                long audience = (long)Mathf.Round(audienceValue);
                audience = TelModLibraryInterop.ApplyFanAttritionMcAudience(show, audience);
                if (NeedsWideFanPath(audience)) return true;
                if (audience < 0L) audience = 0L;
                if (internet && audience < 1L) audience = 1L;

                float newFansValue = (float)audience / 1000f;
                newFansValue *= fatigueCoefficient;
                if (internet) newFansValue *= 1.2f;
                if (SingleValueLeavesExactDomain(newFansValue)) return true;
                long newFans = (long)Mathf.Round(newFansValue);
                if (internet && newFans < 1L) newFans = 1L;
                if (show.episodeCount == 1 || show.episodeCount == 0)
                    newFans = WideNumericRepair.Multiply(newFans, 2L,
                        "Shows._show.SetSales path-selection premiere fans");
                if (newFans > 0L)
                    viralBaseNewFans = WideNumericRepair.Add(viralBaseNewFans, newFans,
                        "Going Viral show path-selection base fans");
                if (NeedsWideFanPath(newFans)) return true;
                totalNewFans = WideNumericRepair.Add(totalNewFans, newFans,
                    "Shows._show.SetSales path-selection total new fans");
                if (totalNewFans < int.MinValue || totalNewFans > int.MaxValue)
                    return true;
            }
            long viralTarget;
            if (TelModLibraryInterop.TryGetGoingViralTarget(viralBaseNewFans, out viralTarget) &&
                (viralTarget < int.MinValue || viralTarget > int.MaxValue))
                return true;
            return false;
        }

        private static bool SingleValueLeavesExactDomain(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ||
                value < -ExactSingleIntegerBoundary ||
                value > ExactSingleIntegerBoundary;
        }

        private static int GetShowBaseAudience(Shows._show show)
        {
            if (ShowsGetBaseAudience == null)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Shows._show.GetBaseAudience no longer matches the audited shape.");
                throw new MissingMethodException(typeof(Shows._show).FullName,
                    "GetBaseAudience");
            }
            return (int)ShowsGetBaseAudience.Invoke(show, null);
        }

        internal static void SetShowSalesWide(Shows._show show)
        {
            long historicalFans = SumShowFans(show);
            long fame = WideNumericRepair.Add(
                (long)resources.GetFameLevel(),
                (long)show.fame[show.fame.Count - 1],
                "Shows._show.SetSales fame");
            if (fame < 1L) fame = 1L;
            int baseAudience = GetShowBaseAudience(show);
            float awardCoefficient = 1f;
            if (show.castType != Shows._show._castType.entireGroup)
            {
                foreach (data_girls.girls girl in show.GetCast())
                {
                    if (girl != null && girl.HasAward(Awards._type.variety_queen))
                        awardCoefficient = 2f;
                    else if (girl != null && girl.HasNomination(Awards._type.variety_queen))
                        awardCoefficient = 1.25f;
                }
            }
            if (variables.Get("VIEWERS_BONUS") == "true" &&
                staticVars.PlayerData.Chapter < tasks._chapter.chapter_5)
                awardCoefficient *= 1.5f;

            float fanAttritionAudienceCoefficient =
                TelModLibraryInterop.GetFanAttritionAudienceCoefficient(show);
            List<data_girls.girls> cast = show.GetCast();
            List<KeyValuePair<resources._fan, long>> viralCasualBuckets =
                new List<KeyValuePair<resources._fan, long>>();
            long totalNewFans = 0L;
            long viralBaseNewFans = 0L;
            long totalAudience = 0L;
            List<singles._single._sales> plannedSales =
                new List<singles._single._sales>();
            FanMutationPlan fanPlan = new FanMutationPlan();
            foreach (resources._fan fan in resources.Fans)
            {
                float randomCoefficient;
                bool internet = show.medium.media_type == Shows._param._media_type.internet;
                if (internet)
                    randomCoefficient = (float)(100 + UnityEngine.Random.Range(0, 15)) / 100f;
                else
                    randomCoefficient = (float)(90 + UnityEngine.Random.Range(0, 20)) / 100f;

                // ((fame * baseAudience / 20) + historicalFans) / 12
                long audience = WideNumericRepair.RoundLinearCombinationWithSingleProductsToEven(
                    fame,
                    baseAudience,
                    historicalFans,
                    20L,
                    240L,
                    "Shows._show.SetSales audience",
                    show.GetFanAppeal(fan),
                    randomCoefficient,
                    awardCoefficient,
                    fanAttritionAudienceCoefficient);
                audience = TelModLibraryInterop.ApplyFanAttritionMcAudience(show, audience);
                if (audience < 0L) audience = 0L;
                if (internet && audience < 1L) audience = 1L;
                singles._single._sales sale = new singles._single._sales
                {
                    fan = fan,
                    sales = audience
                };
                plannedSales.Add(sale);
                totalAudience = WideNumericRepair.Add(totalAudience, audience,
                    "Shows._show.SetSales total audience");

                float fatigueCoefficient = 1f - show.GetFatigue(null) / 100f;
                long newFans = WideNumericRepair.RoundRatioWithSingleProductsToEven(
                    audience,
                    1000L,
                    "Shows._show.SetSales new fans",
                    fatigueCoefficient,
                    internet ? 1.2f : 1f);
                if (internet && newFans < 1L) newFans = 1L;
                if (show.episodeCount == 1 || show.episodeCount == 0)
                    newFans = WideNumericRepair.Multiply(newFans, 2L,
                        "Shows._show.SetSales premiere fans");
                PlanFansEquallyDemographic(fanPlan, newFans, fan, cast,
                    !NeedsWideFanPath(newFans));
                if (newFans > 0L)
                {
                    viralBaseNewFans = WideNumericRepair.Add(viralBaseNewFans, newFans,
                        "Going Viral wide show base fans");
                    if (fan != null && fan.IsType(resources.fanType.casual))
                        viralCasualBuckets.Add(
                            new KeyValuePair<resources._fan, long>(fan, newFans));
                }
                totalNewFans = WideNumericRepair.Add(totalNewFans, newFans,
                    "Shows._show.SetSales total new fans");
            }

            // Going Viral normally records AddFans_Equally calls inside SetSales and
            // injects the trending bonus from SetNewFans. The wide replacement bypasses
            // both call sites, so carry the same bonus into the preflighted fan plan.
            long displayedNewFans = totalNewFans;
            long viralTarget;
            if (TelModLibraryInterop.TryGetGoingViralTarget(viralBaseNewFans, out viralTarget))
            {
                // Going Viral's SetNewFans Prefix replaces the displayed episode-fan
                // value with its positive recorded base even when no bonus can be
                // assigned. Preserve that contract separately from actual fan mutations.
                displayedNewFans = viralBaseNewFans;
                long bonus = WideNumericRepair.Subtract(viralTarget, viralBaseNewFans,
                    "Going Viral wide show bonus");
                if (bonus > 0L && cast != null && cast.Count > 0 &&
                    viralCasualBuckets.Count > 0)
                {
                    long casualBase = 0L;
                    foreach (KeyValuePair<resources._fan, long> bucket in viralCasualBuckets)
                        casualBase = WideNumericRepair.Add(casualBase, Math.Max(0L, bucket.Value),
                            "Going Viral wide show casual base");

                    long assigned = 0L;
                    for (int index = 0; index < viralCasualBuckets.Count; index++)
                    {
                        KeyValuePair<resources._fan, long> bucket = viralCasualBuckets[index];
                        long remaining = WideNumericRepair.Subtract(bonus, assigned,
                            "Going Viral wide show remaining bonus");
                        long share;
                        if (index == viralCasualBuckets.Count - 1)
                        {
                            share = remaining;
                        }
                        else if (casualBase > 0L)
                        {
                            double scaled = bonus *
                                (double)Math.Max(0L, bucket.Value) / (double)casualBase;
                            if (double.IsNaN(scaled) || double.IsInfinity(scaled) ||
                                scaled > long.MaxValue || scaled < long.MinValue)
                            {
                                WideNumericRepair.LatchInvariantFailure(
                                    "Going Viral wide show bonus allocation became non-finite.");
                                throw new OverflowException(
                                    "Going Viral wide show bonus allocation overflowed.");
                            }
                            share = (long)Math.Round(scaled, MidpointRounding.AwayFromZero);
                            if (share > remaining) share = remaining;
                        }
                        else
                        {
                            share = remaining / (viralCasualBuckets.Count - index);
                        }

                        if (share > 0L && bucket.Key != null)
                        {
                            PlanFansEquallyDemographic(fanPlan, share, bucket.Key, cast,
                                !NeedsWideFanPath(share));
                            assigned = WideNumericRepair.Add(assigned, share,
                                "Going Viral wide show assigned bonus");
                        }
                    }
                    displayedNewFans = WideNumericRepair.Add(viralBaseNewFans, assigned,
                        "Going Viral wide show displayed fans");
                }
            }

            show.sales.Clear();
            show.sales.AddRange(plannedSales);
            fanPlan.CommitMutations();
            fanPlan.RunCallbacks();
            show.SetAudience(totalAudience);
            WideNumericState.AppendShowFans(show, displayedNewFans);
        }

        internal static void SetShowRevenueWide(Shows._show show)
        {
            long revenue = WideNumericRepair.Multiply(show.GetAudience(null), 3L,
                "Shows._show.SetRevenue");
            List<data_girls.girls> cast = show.GetCast();
            long earnings = 0L;
            if (cast.Count > 0)
            {
                earnings = WideNumericRepair.Subtract(revenue, show.GetBudget(),
                    "Shows._show.SetRevenue net") / cast.Count;
                if (earnings > 0L)
                {
                    Dictionary<data_girls.girls, long> cumulative =
                        new Dictionary<data_girls.girls, long>();
                    foreach (data_girls.girls girl in cast)
                    {
                        if (girl == null) continue;
                        long prior;
                        cumulative.TryGetValue(girl, out prior);
                        cumulative[girl] = WideNumericRepair.Add(prior, earnings,
                            "Shows._show.SetRevenue repeated cast share");
                    }
                    foreach (KeyValuePair<data_girls.girls, long> pair in cumulative)
                        WideNumericRepair.Add(pair.Key.Earnings_CurrentMonth, pair.Value,
                            "Shows._show.SetRevenue idol earnings preflight");
                }
            }
            show.revenue.Add(revenue);
            if (earnings <= 0L) return;
            foreach (data_girls.girls girl in cast)
                if (girl != null) girl.Earn(earnings);
        }

        internal static bool TryRenderSingleReleaseSalesWide(Single_Release view)
        {
            if (view == null) return false;
            if (view.Type == Single_Release._type.single)
            {
                long units = GetSingleTotalSales(view.single);
                if (!NeedsWideFanPath(units)) return false;
                long profit = WideNumericRepair.Subtract(GetSingleMoney(view.single),
                    view.single.productionCost, "Single_Release.AnimateSales profit");
                if (profit > 0L)
                {
                    ExtensionMethods.SetColor(view.Sales_Money, mainScript.green32);
                    ExtensionMethods.SetColor(view.Sales_Units, mainScript.green32);
                }
                string unitsText = ExtensionMethods.formatNumber(units, false, false);
                if (!view.single.IsDigital())
                    unitsText += " / " +
                        ExtensionMethods.formatNumber(view.single.qty, false, false);
                ExtensionMethods.SetText(view.Sales_Units, unitsText);
                ExtensionMethods.SetText(view.Sales_Money,
                    ExtensionMethods.formatMoney(profit, false, false, false));
                view.Step = Single_Release._step.transition;
                if (units == view.single.qty)
                    Achievements.Unlock(Achievements.ID.ACH_SOLD_OUT);
                return true;
            }
            if (view.Type == Single_Release._type.show)
            {
                long units = view.show.GetTotalSales();
                if (!NeedsWideFanPath(units)) return false;
                long revenue = units / 2L;
                ExtensionMethods.SetColor(view.Sales_Units, mainScript.green32);
                if (revenue > view.show.cost)
                    ExtensionMethods.SetColor(view.Sales_Money, mainScript.green32);
                ExtensionMethods.SetText(view.Sales_Units,
                    ExtensionMethods.formatNumber(units, false, false));
                ExtensionMethods.SetText(view.Sales_Money,
                    ExtensionMethods.formatMoney(revenue, false, false, false));
                view.Step = Single_Release._step.transition;
                return true;
            }
            return false;
        }

        internal static bool TryRenderSingleReleaseNewFansWide(Single_Release view)
        {
            if (view == null || view.Type != Single_Release._type.show || view.show == null)
                return false;
            long fans = GetShowEpisodeFans(view.show, null);
            if (fans >= int.MinValue && fans <= int.MaxValue) return false;
            ExtensionMethods.SetText(view.NewFans_Value,
                ExtensionMethods.formatNumber(fans, false, false));
            return true;
        }

        internal static bool TryRenderSingleReleaseBonusWide(Single_Release view)
        {
            if (view == null || view.Type != Single_Release._type.single || view.single == null)
                return false;
            WideSingleRuntime release = WideNumericState.GetSingleReleaseFans(view.single);
            long total = WideNumericRepair.Add(release.Casual, release.Hardcore,
                "Single_Release.AnimateNewFansBonus");
            if (total >= int.MinValue && total <= int.MaxValue &&
                release.Casual >= int.MinValue && release.Casual <= int.MaxValue &&
                release.Hardcore >= int.MinValue && release.Hardcore <= int.MaxValue)
                return false;
            if (release.Casual > release.Hardcore)
            {
                ExtensionMethods.SetText(view.NewFansBonus_Title,
                    Language.Data["SINGLE__TRENDINESS"]);
                view.NewFansBonus_Info.GetComponent<ButtonDefault>().SetTooltip(
                    Language.Data["SINGLE__NEW_C_FANS"]);
            }
            else
            {
                ExtensionMethods.SetText(view.NewFansBonus_Title,
                    Language.Data["SINGLE__OBSCURITY"]);
                view.NewFansBonus_Info.GetComponent<ButtonDefault>().SetTooltip(
                    Language.Data["SINGLE__NEW_HC_FANS"]);
            }
            ExtensionMethods.SetText(view.NewFansBonus_Value,
                ExtensionMethods.formatNumber(total, false, false));
            return true;
        }

        internal static bool TryRenderShowReleaseSalesWide(Show_Release view)
        {
            if (view == null || view.show == null) return false;
            long units = view.show.GetTotalSales();
            if (!NeedsWideFanPath(units)) return false;
            long newFans = units / 500L;
            long revenue = units / 2L;
            ExtensionMethods.SetText(view.Sales_Units,
                ExtensionMethods.formatNumber(units, false, false));
            ExtensionMethods.SetText(view.Sales_New_Fans, string.Concat(
                Language.Data["SHOW__AUDIENCE"], " | ",
                Language.Data["SHOW__NEW_FANS"], ": ",
                ExtensionMethods.color(
                    ExtensionMethods.formatNumber(newFans, false, false),
                    mainScript.green)));
            if (revenue > view.show.cost)
                ExtensionMethods.SetColor(view.Sales_Money, mainScript.green32);
            ExtensionMethods.SetText(view.Sales_Money,
                ExtensionMethods.formatMoney(revenue, false, false, false));
            return true;
        }

        internal static long AverageShowLongValues(List<long> values)
        {
            if (values == null || values.Count == 0) return 0L;
            long total = WideNumericRepair.SumShowLongValues(values);
            return WideNumericRepair.DivideRoundToEven(total, values.Count,
                "Shows._show.GetAverageParam(List<Int64>)");
        }

        internal static int SumShowIntValues(List<int> values)
        {
            long total = 0L;
            foreach (int value in values)
                total = WideNumericRepair.Add(total, value, "Shows._show.GetTotalParam(List<Int32>)");
            return WideNumericMath.ClampToInt32(total);
        }

        internal static int AverageShowIntValues(List<int> values)
        {
            if (values == null || values.Count == 0) return 0;
            long total = 0L;
            foreach (int value in values)
                total = WideNumericRepair.Add(total, value, "Shows._show.GetAverageParam(List<Int32>)");
            return WideNumericMath.ClampToInt32(
                WideNumericRepair.DivideRoundToEven(total, values.Count,
                    "Shows._show.GetAverageParam(List<Int32>)"));
        }

        internal static void CorrectShowButtonTooltip(
            Shows._show show,
            ref string result)
        {
            if (show == null || string.IsNullOrEmpty(result) ||
                (show.status != Shows._show._status.released &&
                 show.status != Shows._show._status.relaunching &&
                 show.status != Shows._show._status.relaunching_working &&
                 show.status != Shows._show._status.canceled))
                return;

            List<long> exact = WideNumericState.GetShowFans(show);
            if (show.fans == null || exact.Count != show.fans.Count)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Shows._show.GetButtonTooltip found a show-fan sidecar/live count mismatch.");
                return;
            }

            bool needsCorrection = false;
            for (int index = 0; index < exact.Count; index++)
            {
                if (exact[index] != show.fans[index])
                {
                    needsCorrection = true;
                    break;
                }
            }
            if (!needsCorrection) return;

            long total = WideNumericRepair.SumShowLongValues(exact);
            long average = exact.Count == 0 ? 0L :
                WideNumericRepair.DivideRoundToEven(total, exact.Count,
                    "Shows._show.GetButtonTooltip average fans");
            string label = Language.Data["SHOW__NEW_FANS"] + ": ";
            string averageText = ExtensionMethods.color(
                ExtensionMethods.formatNumber(average, false, false), mainScript.green);
            string totalText = ExtensionMethods.color(
                ExtensionMethods.formatNumber(total, false, false), mainScript.green);
            if (!ReplaceLabeledLineValue(ref result, label, 0, averageText) ||
                !ReplaceLabeledLineValue(ref result, label, 1, totalText))
                WideNumericRepair.LatchInvariantFailure(
                    "Shows._show.GetButtonTooltip no longer contains the two audited fan rows.");
        }

        internal static void CorrectTheaterStatLine(
            Theater_Stats_Line view,
            Theaters._theater._stat stat)
        {
            Theaters._theater theater = Theater_Popup.Theater;
            if (view == null || stat == null || theater == null || theater.Stats == null)
                return;
            int ordinal = theater.Stats.IndexOf(stat);
            if (ordinal < 0)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Theater_Stats_Line.Set received a stat outside the active theater history.");
                return;
            }
            long exact = WideNumericState.GetTheaterStatSubscribers(theater, ordinal, stat);
            if (exact == stat.Subscribers) return;
            if (exact == 0L)
            {
                ExtensionMethods.SetText(view.Subscribers, "0");
                ExtensionMethods.SetColor(view.Subscribers, mainScript.blue32);
            }
            else if (exact > 0L)
            {
                ExtensionMethods.SetText(view.Subscribers, "+" +
                    ExtensionMethods.formatNumber(exact, false, false));
                ExtensionMethods.SetColor(view.Subscribers, mainScript.green32);
            }
            else
            {
                ExtensionMethods.SetText(view.Subscribers,
                    ExtensionMethods.formatNumber(exact, false, false));
                ExtensionMethods.SetColor(view.Subscribers, mainScript.red32);
            }
        }

        internal static void CorrectCafeStatLine(
            Cafe_Stat view,
            Cafes._cafe cafe,
            Cafes._cafe._stat stat)
        {
            if (view == null || cafe == null || stat == null || cafe.Stats == null)
                return;
            int ordinal = cafe.Stats.IndexOf(stat);
            if (ordinal < 0)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Cafe_Stat.Set received a stat outside the supplied café history.");
                return;
            }

            long exactProfit = WideNumericState.GetCafeProfit(cafe, ordinal, stat);
            long exactFans = WideNumericState.GetCafeNewFans(cafe, ordinal, stat);
            if (exactProfit != stat.Profit)
                ExtensionMethods.SetText(view.Profit,
                    ExtensionMethods.formatMoney(exactProfit, false, false));
            if (exactFans == stat.New_Fans) return;
            if (exactFans == 0L)
            {
                ExtensionMethods.SetColor(view.New_Fans, mainScript.red32);
                ExtensionMethods.SetText(view.New_Fans, "0");
                ExtensionMethods.SetText(view.Fan_Type, string.Empty);
            }
            else
            {
                ExtensionMethods.SetText(view.New_Fans, "+" +
                    ExtensionMethods.formatNumber(exactFans, false, false));
                ExtensionMethods.SetText(view.Fan_Type,
                    resources.GetFanTitle(stat.Fan_Type));
            }
        }

        internal static bool TryRenderTheaterSubscriberWide(
            Group_Appeal_Fan view,
            Theaters._theater._subscriber subscriber)
        {
            Theaters._theater theater = Theater_Popup.Theater;
            if (view == null || subscriber == null || theater == null ||
                theater.Subscribers == null)
                return false;
            int ordinal = theater.Subscribers.IndexOf(subscriber);
            if (ordinal < 0)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Group_Appeal_Fan.SetSub received a subscriber outside the active theater.");
                return false;
            }

            long exactSubscribers = WideNumericState.GetTheaterSubscriber(
                theater, subscriber, ordinal);
            Groups._group group = theater.GetGroup();
            long exactFans = WideNumericRepair.CalculateGroupFansByType(
                group, subscriber.gender, subscriber.hardcoreness, subscriber.age);
            if (exactSubscribers == subscriber.People &&
                exactFans >= int.MinValue && exactFans <= int.MaxValue)
                return false;
            if (exactFans <= 0L && exactSubscribers != 0L)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "Theater subscriber UI found nonzero subscribers for an empty demographic.");
                return false;
            }

            long percentage = exactFans == 0L ? 0L :
                WideNumericMath.RoundRatioToEven(exactSubscribers, 100L, exactFans);
            if (percentage > 100L) percentage = 100L;
            float progress = exactFans == 0L ? 0f :
                (float)((double)exactSubscribers / (double)exactFans);
            string text = ExtensionMethods.color(string.Concat(new string[]
            {
                resources.GetFanTitle(subscriber.hardcoreness), " ",
                resources.GetFanTitle(subscriber.age), " ",
                resources.GetFanTitle(subscriber.gender)
            }), mainScript.grey_light);
            text += mainScript.separator;
            text = string.Concat(new string[]
            {
                text, Language.Data["GROUPS__TOTAL_FANS"], ": ",
                ExtensionMethods.color(
                    ExtensionMethods.formatNumber(exactFans, false, false), mainScript.green),
                "\n"
            });
            text = text + Language.Data["THEATER__SUBS"] + ": " +
                ExtensionMethods.color(
                    ExtensionMethods.formatNumber(exactSubscribers, false, false) + " (" +
                    ExtensionMethods.formatNumber(percentage, false, false) + "%)",
                    mainScript.green);
            view.GetComponent<ButtonDefault>().SetTooltip(text);
            view.Bar.GetComponent<RectTransform>().localScale = new Vector2(1f, progress);
            view.Portrait.GetComponent<UnityEngine.UI.Image>().fillAmount = progress;
            ExtensionMethods.SetText(view.Value,
                ExtensionMethods.formatNumber(exactSubscribers, false, false));
            return true;
        }

        internal static void CorrectGroupAppealPopup(Group_Appeal_Popup view)
        {
            if (view == null || view.Group == null || view.Fans == null) return;
            foreach (Group_Appeal_Popup._FanObj fan in view.Fans)
            {
                if (fan == null) continue;
                long exact = WideNumericRepair.CalculateGroupFansByType(
                    view.Group, fan.Gender, fan.Hardcoreness, fan.Age);
                if (exact >= int.MinValue && exact <= int.MaxValue) continue;

                int maxFans = Groups.GetMaxFans(fan.Gender, fan.Hardcoreness, fan.Age);
                int newFans = view.Group.GetNewFansPerSingle(
                    fan.Gender, fan.Hardcoreness, fan.Age);
                if (view.SelectedTab == Group_Appeal_Popup._tab.total_fans)
                    ExtensionMethods.SetText(fan.Number,
                        ExtensionMethods.formatNumber(exact, true, false));

                Group_Appeal_Fan row = fan.Portrait.GetComponent<Group_Appeal_Fan>();
                string tooltip = ExtensionMethods.color(string.Concat(new string[]
                {
                    resources.GetFanTitle(fan.Hardcoreness), " ",
                    resources.GetFanTitle(fan.Age), " ",
                    resources.GetFanTitle(fan.Gender)
                }), mainScript.grey_light);
                tooltip += mainScript.separator;
                tooltip = tooltip + Language.Data["GROUPS__NEW_FANS"] + ": " +
                    ExtensionMethods.color(
                        ExtensionMethods.formatNumber(newFans, false, false), mainScript.green);
                tooltip += mainScript.separator;
                tooltip = string.Concat(new string[]
                {
                    tooltip, Language.Data["GROUPS__TOTAL_FANS"], ": ",
                    ExtensionMethods.color(
                        ExtensionMethods.formatNumber(exact, false, false), mainScript.green),
                    "\n"
                });
                tooltip = tooltip + Language.Data["GROUPS__MAX_FANS"] + ": " +
                    ExtensionMethods.color(
                        ExtensionMethods.formatNumber(maxFans, false, false), mainScript.green);
                row.GetComponent<ButtonDefault>().SetTooltip(tooltip);
                float progress = exact > maxFans ? 1f : maxFans > 0
                    ? (float)((double)exact / (double)maxFans) : 0f;
                row.Bar.GetComponent<RectTransform>().localScale = new Vector2(1f, progress);
                row.Portrait.GetComponent<UnityEngine.UI.Image>().fillAmount = progress;
            }
        }

        private static bool ReplaceLabeledLineValue(
            ref string text,
            string label,
            int occurrence,
            string replacement)
        {
            int searchFrom = 0;
            int labelStart = -1;
            for (int index = 0; index <= occurrence; index++)
            {
                labelStart = text.IndexOf(label, searchFrom, StringComparison.Ordinal);
                if (labelStart < 0) return false;
                searchFrom = labelStart + label.Length;
            }
            int lineEnd = text.IndexOf('\n', searchFrom);
            if (lineEnd < 0) return false;
            text = text.Substring(0, searchFrom) + replacement + text.Substring(lineEnd);
            return true;
        }

        internal static long RoundSingleCompatible(long value, float coefficient, string context)
        {
            float product = (float)value * coefficient;
            if (value >= -ExactSingleIntegerBoundary && value <= ExactSingleIntegerBoundary &&
                !float.IsNaN(product) && !float.IsInfinity(product) &&
                product >= -ExactSingleIntegerBoundary &&
                product <= ExactSingleIntegerBoundary)
                return (long)Mathf.Round(product);
            return WideNumericRepair.RoundSingleProductToEven(value, context, coefficient);
        }

        internal static long RoundSingleProductCompatible(
            long value,
            string context,
            params float[] coefficients)
        {
            float product = (float)value;
            bool vanillaCompatible =
                value >= -ExactSingleIntegerBoundary &&
                value <= ExactSingleIntegerBoundary;
            if (coefficients != null)
            {
                foreach (float coefficient in coefficients)
                {
                    product *= coefficient;
                    if (float.IsNaN(product) || float.IsInfinity(product) ||
                        product < -ExactSingleIntegerBoundary ||
                        product > ExactSingleIntegerBoundary)
                    {
                        vanillaCompatible = false;
                    }
                }
            }
            if (vanillaCompatible) return (long)Mathf.Round(product);
            return WideNumericRepair.RoundSingleProductToEven(
                value, context, coefficients ?? new float[0]);
        }

        internal static long RoundToMultiple(long value, long multiple, string context)
        {
            if (multiple <= 0L) throw new ArgumentOutOfRangeException(nameof(multiple));
            long quotient = value / multiple;
            long remainder = value % multiple;
            long absoluteRemainder = remainder < 0L ? -remainder : remainder;
            bool increment = absoluteRemainder * 2L > multiple ||
                (absoluteRemainder * 2L == multiple && (quotient & 1L) != 0L);
            if (increment) quotient = WideNumericRepair.Add(quotient,
                value < 0L ? -1L : 1L, context);
            return WideNumericRepair.Multiply(quotient, multiple, context);
        }

        internal static void GuardNextId(int current, string context)
        {
            if (current == int.MaxValue) WideNumericRepair.RefuseIdentityExhaustion(context);
        }

        internal static void GuardCounterIncrement(int current, int delta, string context)
        {
            if ((delta > 0 && current > int.MaxValue - delta) ||
                (delta < 0 && current < int.MinValue - delta))
                WideNumericRepair.RefuseCounterExhaustion(context, current, delta);
        }

        internal static int GetNextGroupId()
        {
            int maximum = -1;
            foreach (Groups._group group in Groups.Groups_)
            {
                if (group.ID == int.MaxValue)
                    WideNumericRepair.RefuseIdentityExhaustion("Groups.GetNextGroupID");
                if (group.ID > maximum) maximum = group.ID;
            }
            return maximum + 1;
        }

        internal static int GetNextTheaterId()
        {
            int next = 0;
            foreach (Theaters._theater theater in Theaters.Theaters_)
            {
                if (theater.ID == int.MaxValue)
                    WideNumericRepair.RefuseIdentityExhaustion("Theaters.GetNextTheaterID");
                if (theater.ID >= next) next = theater.ID + 1;
            }
            return next;
        }

        internal static int GetNextCafeId()
        {
            int next = 0;
            foreach (Cafes._cafe cafe in Cafes.Cafes_)
            {
                if (cafe.ID == int.MaxValue)
                    WideNumericRepair.RefuseIdentityExhaustion("Cafes.GetNextTheaterID");
                if (cafe.ID >= next) next = cafe.ID + 1;
            }
            return next;
        }

        internal static int GetNextDishId(Cafes._cafe cafe)
        {
            if (cafe.Dishes.Count == 0) return 0;
            int maximum = 0;
            foreach (Cafes._cafe._dish dish in cafe.Dishes)
            {
                if (dish.ID == int.MaxValue)
                    WideNumericRepair.RefuseIdentityExhaustion("Cafes._cafe.GetNewDishID");
                if (dish.ID > maximum) maximum = dish.ID;
            }
            return maximum + 1;
        }

        internal static void PreflightFloorAddRoom(agency._type type)
        {
            int counter = GetRoomIdCounter();
            GuardIdentityCapacity(counter, 1, "agency._floor.addRoom room ID");
            PreflightRoomOwnedIdentity(type);
        }

        internal static void PreflightAgencyAddRoom(agency owner, int type, bool build)
        {
            if (owner == null || owner.floors == null || owner.floors.Count == 0 ||
                owner.selectedFloor < 0 || owner.selectedFloor >= owner.floors.Count)
                return;

            agency._type roomType = (agency._type)type;
            List<agency._type> selected = ProjectFloorTypes(
                owner.floors[owner.selectedFloor], true, roomType, owner);
            List<agency._type> first = owner.selectedFloor == 0
                ? selected : ProjectFloorTypes(owner.floors[0], false, roomType, owner);
            List<agency._type> last = owner.selectedFloor == owner.floors.Count - 1
                ? selected : ProjectFloorTypes(owner.floors[owner.floors.Count - 1],
                    false, roomType, owner);

            int addedFloors = (WouldExpandFloor(last) ? 1 : 0) +
                (WouldExpandFloor(first) ? 1 : 0);
            int addedRooms = 1 + (OccupiedSpace(selected) < 5 ? 1 : 0) + addedFloors;
            GuardIdentityCapacity(GetRoomIdCounter(), addedRooms,
                "agency.addRoom room IDs");

            int maximumFloorId = owner.GetBiggestFloorID();
            GuardIdentityCapacity(maximumFloorId, addedFloors,
                "agency.addRoom floor IDs");
            PreflightRoomOwnedIdentity(roomType);
            if (build)
            {
                long debit = WideNumericRepair.Subtract(0L, owner.roomCost(roomType),
                    "agency.addRoom construction debit");
                WideNumericRepair.Add(resources.Money(),
                    BuffMeWideNumericInterop.PreviewResourceDelta(resources.type.money, debit),
                    "agency.addRoom construction resource preflight");
            }
        }

        private static int GetRoomIdCounter()
        {
            if (AgencyRoomIdCounter == null)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "A33.6 could not bind agency.roomIDCounter.");
                throw new MissingFieldException(typeof(agency).FullName, "roomIDCounter");
            }
            return (int)AgencyRoomIdCounter.GetValue(null);
        }

        private static void GuardIdentityCapacity(int current, int required, string context)
        {
            if (required < 0 || current > int.MaxValue - required)
                WideNumericRepair.RefuseIdentityExhaustion(context);
        }

        private static void PreflightRoomOwnedIdentity(agency._type type)
        {
            if (type == agency._type.theatre) GetNextTheaterId();
            else if (type == agency._type.cafeAndShop) GetNextCafeId();
        }

        private static List<agency._type> ProjectFloorTypes(
            agency._floor floor, bool selected, agency._type added, agency owner)
        {
            List<agency._type> result = new List<agency._type>();
            foreach (agency._room room in floor.floor) result.Add(room.type);
            if (!selected) return result;
            if (result.Count > 0 && agency.isEmptyRoom(result[result.Count - 1]))
                result.RemoveAt(result.Count - 1);
            result.Add(added);
            if (OccupiedSpace(result) < 5) result.Add(agency._type.emptyRoom_1);
            return result;
        }

        private static int OccupiedSpace(List<agency._type> rooms)
        {
            int result = 0;
            foreach (agency._type type in rooms)
                if (!agency.isEmptyRoom(type)) result += agency.roomSpace(type);
            return result;
        }

        private static bool WouldExpandFloor(List<agency._type> rooms)
        {
            return rooms.Count == 2 || rooms.Count == 1 &&
                (rooms[0] == agency._type.cafeAndShop || rooms[0] == agency._type.theatre);
        }

        internal static void PreflightActivityCounters(Activity._type type)
        {
            if (type == Activity._type.performance)
            {
                GuardCounterIncrement(Stats.data.activities.performance, 1,
                    "Activities.Performance lifetime counter");
                GuardCounterIncrement(Stats.data.activities.performance_year, 1,
                    "Activities.Performance yearly counter");
            }
            else if (type == Activity._type.promotion)
            {
                GuardCounterIncrement(Stats.data.activities.promotion, 1,
                    "Activities.Promotion lifetime counter");
                GuardCounterIncrement(Stats.data.activities.promotion_year, 1,
                    "Activities.Promotion yearly counter");
            }
            else if (type == Activity._type.spa_treatment)
            {
                GuardCounterIncrement(Stats.data.activities.spa, 1,
                    "Activities.SpaTreatment lifetime counter");
                GuardCounterIncrement(Stats.data.activities.spa_year, 1,
                    "Activities.SpaTreatment yearly counter");
            }
        }

        internal static void PreflightBusinessAcceptCounters(business._proposal proposal)
        {
            if (proposal == null || proposal.girl == null) return;
            data_girls.girls._stat stats = proposal.girl.Stats;
            data_girls.girls._stat yearly = stats.GetYearly();
            if (proposal.skill == data_girls._paramType.sexy)
            {
                GuardCounterIncrement(stats.Proposals_Sexy, 1,
                    "business.Accept idol sexy-proposal counter");
                GuardCounterIncrement(yearly.Proposals_Sexy, 1,
                    "business.Accept yearly sexy-proposal counter");
                foreach (data_girls.girls girl in proposal.Girls)
                    if (girl != null && girl != proposal.girl)
                        GuardCounterIncrement(girl.Stats.Sexy_Proposals_Declined, 1,
                            "business.Accept peer declined-sexy counter");
            }
            else if (proposal.skill == data_girls._paramType.pretty)
            {
                GuardCounterIncrement(stats.Proposals_Pretty, 1,
                    "business.Accept idol pretty-proposal counter");
                GuardCounterIncrement(yearly.Proposals_Pretty, 1,
                    "business.Accept yearly pretty-proposal counter");
            }
            else if (proposal.type == business._type.variety)
            {
                GuardCounterIncrement(stats.Variety_Appearences, 1,
                    "business.Accept idol variety counter");
                GuardCounterIncrement(yearly.Variety_Appearences, 1,
                    "business.Accept yearly variety counter");
            }
            PreflightBusinessStatsCounter(proposal);
        }

        internal static void PreflightBusinessDeclineCounters(business._proposal proposal)
        {
            if (proposal == null || proposal.skill != data_girls._paramType.sexy) return;
            foreach (data_girls.girls girl in proposal.Girls)
                if (girl != null)
                    GuardCounterIncrement(girl.Stats.Sexy_Proposals_Declined, 1,
                        "business.Decline declined-sexy counter");
        }

        internal static void PreflightBusinessStatsCounter(business._proposal proposal)
        {
            if (proposal == null) return;
            if (proposal.type == business._type.photoshoot)
                GuardCounterIncrement(Stats.data.business.photoshoot_counter, 1,
                    "Stats.OnBusinessProposalAccepted photoshoot counter");
            else if (proposal.type == business._type.ad)
                GuardCounterIncrement(Stats.data.business.ad_counter, 1,
                    "Stats.OnBusinessProposalAccepted ad counter");
            else if (proposal.type == business._type.tv_drama)
                GuardCounterIncrement(Stats.data.business.tv_drama_counter, 1,
                    "Stats.OnBusinessProposalAccepted TV-drama counter");
        }

        internal static void AddRelationshipPoints(
            Relationships_Player._type type, data_girls.girls girl, int points)
        {
            if (girl == null) throw new ArgumentNullException(nameof(girl));
            long effective = points;
            if (type == Relationships_Player._type.Influence && points > 0 &&
                staff.GetPlayer().LevelledUp)
                effective = WideNumericRepair.Multiply(points, 4L,
                    "Relationships_Player.AddPoints influence bonus");
            int before;
            if (type == Relationships_Player._type.Friendship) before = girl.Rel_Friendship_Points;
            else if (type == Relationships_Player._type.Influence) before = girl.Rel_Influence_Points;
            else if (type == Relationships_Player._type.Romance) before = girl.Rel_Romance_Points;
            else { girl.UpdateButton(); return; }
            long exact = WideNumericRepair.Add(before, effective,
                "Relationships_Player.AddPoints");
            int after = exact > 512L ? 512 : exact < -388L ? -388 : (int)exact;
            if (type == Relationships_Player._type.Friendship) girl.Rel_Friendship_Points = after;
            else if (type == Relationships_Player._type.Influence) girl.Rel_Influence_Points = after;
            else girl.Rel_Romance_Points = after;
            if (before != after)
            {
                if (RelationshipAddToQueue == null)
                {
                    WideNumericRepair.LatchInvariantFailure(
                        "A33.6 could not bind Relationships_Player.AddToQueue.");
                    throw new MissingMethodException(typeof(Relationships_Player).FullName,
                        "AddToQueue");
                }
                RelationshipAddToQueue.Invoke(Relationships_Player.GetThis(),
                    new object[] { type, girl, before, after });
            }
            girl.UpdateButton();
        }

        internal static bool NeedsWideFanPath(long value)
        {
            return value < -ExactSingleIntegerBoundary || value > ExactSingleIntegerBoundary;
        }

        internal static bool GirlNeedsWideFanPath(data_girls.girls girl, resources.fanType? type)
        {
            if (girl == null || girl.Fans == null) return false;
            foreach (resources._fan fan in girl.Fans)
                if (fan.IsType(type) && NeedsWideFanPath(fan.people)) return true;
            return false;
        }

        internal static bool FanDistributionMayOverflow(
            long total,
            resources.fanType? type,
            List<data_girls.girls> girls,
            data_girls.girls exception)
        {
            if (total <= 0L) return false;
            if (girls == null) girls = data_girls.girl;
            foreach (data_girls.girls girl in girls)
            {
                if (girl == null || girl == exception ||
                    girl.status == data_girls._status.graduated) continue;
                if (GirlFanAdditionMayOverflow(girl, total, type)) return true;
            }
            return false;
        }

        internal static bool FanDemographicDistributionMayOverflow(
            long total,
            resources._fan demographic,
            List<data_girls.girls> girls)
        {
            if (total <= 0L || demographic == null) return false;
            if (girls == null) girls = data_girls.girl;
            foreach (data_girls.girls girl in girls)
            {
                if (girl == null || girl.status == data_girls._status.graduated) continue;
                resources._fan fan = girl.GetFan(demographic.gender,
                    demographic.hardcoreness, demographic.age);
                if (fan == null) continue;
                long adjusted = MaximumGirlFanCredit(girl, total);
                if (fan.people > long.MaxValue - adjusted) return true;
            }
            return false;
        }

        internal static bool GirlFanAdditionMayOverflow(
            data_girls.girls girl,
            long value,
            resources.fanType? type)
        {
            if (girl == null || value <= 0L || girl.Fans == null) return false;
            long adjusted = MaximumGirlFanCredit(girl, value);
            foreach (resources._fan fan in girl.Fans)
                if (fan.IsType(type) && fan.people > long.MaxValue - adjusted)
                    return true;
            return false;
        }

        internal static bool GirlDemographicAdditionMayOverflow(
            data_girls.girls girl,
            long value,
            resources.fanType gender,
            resources.fanType hardcoreness,
            resources.fanType age)
        {
            if (girl == null || value <= 0L) return false;
            resources._fan fan = girl.GetFan(gender, hardcoreness, age);
            if (fan == null) return false;
            long adjusted = MaximumGirlFanCredit(girl, value);
            return fan.people > long.MaxValue - adjusted;
        }

        private static long MaximumGirlFanCredit(data_girls.girls girl, long value)
        {
            if (girl.HasAward(Awards._type.best_debut_idol))
            {
                if (value > long.MaxValue / 2L) return long.MaxValue;
                return value * 2L;
            }
            if (girl.HasNomination(Awards._type.best_debut_idol))
            {
                if (value > long.MaxValue / 4L * 3L) return long.MaxValue;
                return WideNumericMath.RoundRatioToEven(value, 5L, 4L);
            }
            return value;
        }

        private sealed class FanMutation
        {
            internal readonly resources._fan Fan;
            internal readonly long Delta;

            internal FanMutation(resources._fan fan, long delta)
            {
                Fan = fan;
                Delta = delta;
            }
        }

        private enum FanCallback
        {
            ResourceChanged,
            DisplayChanged
        }

        private sealed class FanMutationPlan
        {
            private readonly List<FanMutation> mutations = new List<FanMutation>();
            private readonly Dictionary<resources._fan, long> projected =
                new Dictionary<resources._fan, long>();
            private readonly List<FanCallback> callbacks = new List<FanCallback>();

            internal void Add(resources._fan fan, long delta, string context)
            {
                if (fan == null) throw new ArgumentNullException(nameof(fan));
                long before;
                if (!projected.TryGetValue(fan, out before)) before = fan.people;
                long exact = WideNumericRepair.Add(before, delta, context);
                projected[fan] = exact < 0L ? 0L : exact;
                mutations.Add(new FanMutation(fan, delta));
            }

            internal void ScheduleResourceChanged()
            {
                callbacks.Add(FanCallback.ResourceChanged);
            }

            internal void ScheduleDisplayChanged()
            {
                callbacks.Add(FanCallback.DisplayChanged);
            }

            internal long GetProjected(resources._fan fan)
            {
                long value;
                return projected.TryGetValue(fan, out value) ? value : fan.people;
            }

            internal void CommitMutations()
            {
                foreach (FanMutation mutation in mutations)
                    mutation.Fan.AddPeople(mutation.Delta);
            }

            internal void RunCallbacks()
            {
                foreach (FanCallback callback in callbacks)
                {
                    if (callback == FanCallback.ResourceChanged) resources._OnFansChange();
                    else ReportFansChanged();
                }
            }
        }

        internal static void AddFansEqually(
            long total,
            resources._fan demographic,
            List<data_girls.girls> girls)
        {
            FanMutationPlan plan = new FanMutationPlan();
            PlanFansEquallyDemographic(plan, total, demographic, girls, false);
            plan.CommitMutations();
            plan.RunCallbacks();
        }

        private static void PlanFansEquallyDemographic(
            FanMutationPlan plan,
            long total,
            resources._fan demographic,
            List<data_girls.girls> girls,
            bool vanillaCompatible)
        {
            if (total == 0L) return;
            if (girls == null) girls = data_girls.girl;
            List<data_girls.girls> shuffled = new List<data_girls.girls>();
            foreach (data_girls.girls girl in girls)
                if (girl.status != data_girls._status.graduated) shuffled.Add(girl);
            ExtensionMethods.Shuffle(shuffled);

            if (shuffled.Count == 0)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "data_girls.AddFans_Equally has no eligible demographic recipient.");
                throw new InvalidOperationException("No eligible fan recipient.");
            }

            if (vanillaCompatible)
            {
                long allocated = 0L;
                long delta = (long)Mathf.Ceil((float)total / (float)shuffled.Count);
                foreach (data_girls.girls girl in shuffled)
                {
                    long remaining = WideNumericRepair.Subtract(total, allocated,
                        "data_girls.AddFans_Equally compatible remainder");
                    if ((total > 0L && remaining < delta) ||
                        (total < 0L && remaining > delta))
                        delta = remaining;
                    resources._fan fan = demographic == null ? null :
                        girl.GetFan(demographic.gender, demographic.hardcoreness,
                            demographic.age);
                    if (fan != null)
                    {
                        plan.Add(fan, delta,
                            "data_girls.AddFans_Equally compatible preflight");
                        allocated = WideNumericRepair.Add(allocated, delta,
                            "data_girls.AddFans_Equally compatible allocated");
                    }
                    if (allocated == total)
                    {
                        plan.ScheduleDisplayChanged();
                        return;
                    }
                }
                plan.ScheduleDisplayChanged();
                return;
            }

            List<resources._fan> recipients = new List<resources._fan>();
            foreach (data_girls.girls girl in shuffled)
            {
                resources._fan fan = demographic == null ? null :
                    girl.GetFan(demographic.gender, demographic.hardcoreness, demographic.age);
                if (fan != null) recipients.Add(fan);
            }
            if (recipients.Count == 0)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "data_girls.AddFans_Equally has no eligible demographic recipient.");
                throw new InvalidOperationException("No eligible fan recipient.");
            }

            long quotient = total / recipients.Count;
            long remainder = total % recipients.Count;
            List<long> deltas = BalancedDeltas(quotient, remainder, recipients.Count);
            for (int index = 0; index < recipients.Count; index++)
                plan.Add(recipients[index], deltas[index],
                    "data_girls.AddFans_Equally preflight");
            plan.ScheduleDisplayChanged();
        }

        internal static void AddFansEqually(long total, List<data_girls.girls> girls)
        {
            FanMutationPlan plan = new FanMutationPlan();
            PlanFansEqually(plan, total, girls);
            plan.CommitMutations();
            plan.RunCallbacks();
        }

        internal static void AddFansWeighted(
            long total,
            resources.fanType? fanType,
            List<data_girls.girls> girls,
            data_girls.girls exception)
        {
            FanMutationPlan plan = new FanMutationPlan();
            PlanFansWeighted(plan, total, fanType, girls, exception);
            plan.CommitMutations();
            plan.RunCallbacks();
        }

        private static void PlanFansEqually(
            FanMutationPlan plan,
            long total,
            List<data_girls.girls> girls)
        {
            if (total == 0L) return;
            if (girls == null) girls = data_girls.girl;
            List<data_girls.girls> recipients = new List<data_girls.girls>();
            foreach (data_girls.girls girl in girls)
                if (girl.status != data_girls._status.graduated) recipients.Add(girl);
            if (recipients.Count == 0)
            {
                WideNumericRepair.LatchInvariantFailure(
                    "data_girls.AddFans_Equally has no eligible idol recipient.");
                throw new InvalidOperationException("No eligible idol recipient.");
            }
            ExtensionMethods.Shuffle(recipients);
            long quotient = total / recipients.Count;
            long remainder = total % recipients.Count;
            List<long> deltas = BalancedDeltas(quotient, remainder, recipients.Count);
            for (int index = 0; index < recipients.Count; index++)
                PlanGirlFans(plan, recipients[index], deltas[index], null);
            plan.ScheduleDisplayChanged();
        }

        private static void PlanFansWeighted(
            FanMutationPlan plan,
            long total,
            resources.fanType? fanType,
            List<data_girls.girls> girls,
            data_girls.girls exception)
        {
            if (total == 0L) return;
            if (girls == null) girls = data_girls.girl;
            List<data_girls.girls> recipients = new List<data_girls.girls>();
            float fameTotal = 0f;
            foreach (data_girls.girls girl in girls)
            {
                if (girl.status == data_girls._status.graduated || girl == exception) continue;
                recipients.Add(girl);
                fameTotal += girl.GetFamePoints();
            }
            if (recipients.Count == 0)
            {
                WideNumericRepair.LatchInvariantFailure("data_girls.AddFans has no eligible recipient.");
                throw new InvalidOperationException("No eligible idol recipient.");
            }
            if (fameTotal < 1f)
            {
                PlanFansEqually(plan, total, recipients);
                return;
            }
            ExtensionMethods.Shuffle(recipients);

            List<long> deltas = new List<long>();
            long allocated = 0L;
            for (int index = 0; index < recipients.Count; index++)
            {
                long value;
                if (index == recipients.Count - 1)
                {
                    value = WideNumericRepair.Subtract(total, allocated,
                        "data_girls.AddFans final remainder");
                }
                else
                {
                    float coefficient = recipients[index].GetFamePoints() / fameTotal;
                    value = RoundSingleCompatible(total, coefficient,
                        "data_girls.AddFans fame allocation");
                    long remaining = WideNumericRepair.Subtract(total, allocated,
                        "data_girls.AddFans remaining");
                    if ((total >= 0L && value > remaining) || (total < 0L && value < remaining))
                        value = remaining;
                }
                allocated = WideNumericRepair.Add(allocated, value,
                    "data_girls.AddFans allocated");
                deltas.Add(value);
            }
            for (int index = 0; index < recipients.Count; index++)
                PlanGirlFans(plan, recipients[index], deltas[index], fanType);
            plan.ScheduleDisplayChanged();
        }

        internal static void AddGirlFans(
            data_girls.girls girl,
            long value,
            resources.fanType? fanType)
        {
            FanMutationPlan plan = new FanMutationPlan();
            PlanGirlFans(plan, girl, value, fanType);
            plan.CommitMutations();
            plan.RunCallbacks();
        }

        private static void PlanGirlFans(
            FanMutationPlan plan,
            data_girls.girls girl,
            long value,
            resources.fanType? fanType)
        {
            value = BuffMeWideNumericInterop.ApplyDirectGirlFanDelta(value);
            if (value == 0L) return;
            if (girl.Fans.Count == 0) girl.CreateFans();
            girl.RecalcFanAppeal();
            float appealTotal = girl.GetAppeal_Total();
            if (appealTotal == 0f) return;
            if (value > 0L && girl.HasAward(Awards._type.best_debut_idol))
                value = WideNumericRepair.Multiply(value, 2L, "data_girls.girls.AddFans award");
            else if (value > 0L && girl.HasNomination(Awards._type.best_debut_idol))
                value = WideNumericMath.RoundRatioToEven(value, 5L, 4L);

            List<resources._fan> recipients = new List<resources._fan>();
            List<long> deltas = new List<long>();
            long allocated = 0L;
            foreach (resources._fan fan in girl.Fans)
            {
                if (!fan.IsType(fanType)) continue;
                long delta = WideNumericRepair.FloorSingleProduct(
                    value, "data_girls.girls.AddFans appeal allocation",
                    fan.GetTotalAppeal() / appealTotal);
                recipients.Add(fan);
                deltas.Add(delta);
                allocated = WideNumericRepair.Add(allocated, delta,
                    "data_girls.girls.AddFans allocated");
            }
            if (recipients.Count == 0) return;
            long remainder = WideNumericRepair.Subtract(value, allocated,
                "data_girls.girls.AddFans remainder");
            List<int> order = new List<int>();
            for (int index = 0; index < recipients.Count; index++) order.Add(index);
            ExtensionMethods.Shuffle(order);
            long quotient = remainder / recipients.Count;
            long tail = remainder % recipients.Count;
            List<long> additions = BalancedDeltas(quotient, tail, recipients.Count);
            for (int index = 0; index < order.Count; index++)
                deltas[order[index]] = WideNumericRepair.Add(deltas[order[index]], additions[index],
                    "data_girls.girls.AddFans residue");

            for (int index = 0; index < recipients.Count; index++)
                plan.Add(recipients[index], deltas[index],
                    "data_girls.girls.AddFans preflight");
            plan.ScheduleResourceChanged();
        }

        internal static void AddGirlFansDemographic(
            data_girls.girls girl,
            resources.fanType gender,
            resources.fanType hardcore,
            resources.fanType age,
            long value)
        {
            FanMutationPlan plan = new FanMutationPlan();
            PlanGirlFansDemographic(plan, girl, gender, hardcore, age, value);
            plan.CommitMutations();
            plan.RunCallbacks();
        }

        private static void PlanGirlFansDemographic(
            FanMutationPlan plan,
            data_girls.girls girl,
            resources.fanType gender,
            resources.fanType hardcore,
            resources.fanType age,
            long value)
        {
            if (girl.Fans.Count == 0) girl.CreateFans();
            if (value > 0L && girl.HasAward(Awards._type.best_debut_idol))
                value = WideNumericRepair.Multiply(value, 2L,
                    "data_girls.girls.AddFans demographic award");
            else if (value > 0L && girl.HasNomination(Awards._type.best_debut_idol))
                value = WideNumericMath.RoundRatioToEven(value, 5L, 4L);
            resources._fan fan = girl.GetFan(gender, hardcore, age);
            if (fan != null)
                plan.Add(fan, value,
                    "data_girls.girls.AddFans demographic preflight");
        }

        internal static long GetGirlFansToAdd(data_girls.girls girl, long original, float coefficient)
        {
            long result = RoundSingleProductCompatible(
                original, "data_girls.girls.GetFansToAdd", coefficient, 0.1f);
            if (result > 0L && girl.HasAward(Awards._type.best_debut_idol))
                result = WideNumericRepair.Multiply(result, 2L,
                    "data_girls.girls.GetFansToAdd award");
            else if (result > 0L && girl.HasNomination(Awards._type.best_debut_idol))
                result = WideNumericMath.RoundRatioToEven(result, 5L, 4L);
            if (result == 0L && original > 0L && coefficient > 0f) return 1L;
            if (result == 0L && original > 0L && coefficient < 0f) return -1L;
            return result;
        }

        internal static void AddGirlFansByCoefficient(
            data_girls.girls girl,
            resources.fanType? fanType,
            float value,
            bool oshihen)
        {
            if (value < 0f && staticVars.IsHard())
            {
                value *= UnityEngine.Random.Range(1, 5);
                if (value < -9.5f) value = -9.5f;
            }
            if (girl.Fans.Count == 0) girl.CreateFans();
            float casualOshihen = (float)UnityEngine.Random.Range(5, 40) / 100f;
            float hardcoreOshihen = (float)UnityEngine.Random.Range(50, 90) / 100f;
            List<resources._fan> recipients = new List<resources._fan>();
            List<long> deltas = new List<long>();
            FanMutationPlan plan = new FanMutationPlan();
            long total = 0L;
            long quitHardcore = 0L, movedHardcore = 0L, quitCasual = 0L, movedCasual = 0L;
            foreach (resources._fan fan in girl.Fans)
            {
                if (!fan.IsType(fanType)) continue;
                long delta = GetGirlFansToAdd(girl, fan.GetNumberOfPeople(), value);
                if (delta < -fan.people) delta = -fan.people;
                plan.Add(fan, delta,
                    "data_girls.girls.AddFans(coefficient) preflight");
                recipients.Add(fan);
                deltas.Add(delta);
                total = WideNumericRepair.Add(total, delta,
                    "data_girls.girls.AddFans(coefficient) total");
                if (!oshihen) continue;
                if (fan.hardcoreness == resources.fanType.hardcore)
                {
                    movedHardcore = WideNumericRepair.Subtract(movedHardcore,
                        RoundSingleCompatible(delta, hardcoreOshihen,
                            "data_girls.girls.AddFans hardcore oshihen"),
                        "data_girls.girls.AddFans hardcore moved");
                    quitHardcore = WideNumericRepair.Subtract(quitHardcore,
                        RoundSingleCompatible(delta, 1f - hardcoreOshihen,
                            "data_girls.girls.AddFans hardcore quit"),
                        "data_girls.girls.AddFans hardcore quit total");
                }
                else if (fan.hardcoreness == resources.fanType.casual)
                {
                    movedCasual = WideNumericRepair.Subtract(movedCasual,
                        RoundSingleCompatible(delta, casualOshihen,
                            "data_girls.girls.AddFans casual oshihen"),
                        "data_girls.girls.AddFans casual moved");
                    quitCasual = WideNumericRepair.Subtract(quitCasual,
                        RoundSingleCompatible(delta, 1f - casualOshihen,
                            "data_girls.girls.AddFans casual quit"),
                        "data_girls.girls.AddFans casual quit total");
                }
            }
            if (oshihen)
            {
                PlanFansWeighted(plan, movedCasual, resources.fanType.casual, null, girl);
                PlanFansWeighted(plan, movedHardcore, resources.fanType.hardcore, null, girl);
            }
            plan.CommitMutations();
            girl.FansAdded = total;
            if (fanType == resources.fanType.hardcore)
            {
                girl.Fans_Quit_Hardcore = quitHardcore;
                girl.Fans_Oshihened_Hardcore = movedHardcore;
            }
            if (fanType == resources.fanType.casual)
            {
                girl.Fans_Quit_Casual = quitCasual;
                girl.Fans_Oshihened_Casual = movedCasual;
            }
            plan.RunCallbacks();
        }

        internal static void Oshihen(long total, data_girls.girls girl)
        {
            if (girl == null || total == 0L) return;
            long currentTotal = girl.GetFans_Total(null);
            if (currentTotal == 0L) return;
            List<long> removals = new List<long>();
            long allocated = 0L;
            for (int index = 0; index < girl.Fans.Count; index++)
            {
                long share = index == girl.Fans.Count - 1
                    ? WideNumericRepair.Subtract(total, allocated,
                        "data_girls.AddFans_Oshihen final remainder")
                    : WideNumericMath.RoundRatioToEven(total, girl.Fans[index].people, currentTotal);
                long remaining = WideNumericRepair.Subtract(total, allocated,
                    "data_girls.AddFans_Oshihen remaining");
                if ((total >= 0L && share > remaining) || (total < 0L && share < remaining)) share = remaining;
                if (share > girl.Fans[index].people) share = girl.Fans[index].people;
                removals.Add(share);
                allocated = WideNumericRepair.Add(allocated, share,
                    "data_girls.AddFans_Oshihen allocated");
            }
            long hardcore = 0L, casual = 0L;
            FanMutationPlan plan = new FanMutationPlan();
            for (int index = 0; index < girl.Fans.Count; index++)
            {
                resources._fan fan = girl.Fans[index];
                long removal = removals[index];
                long delta = WideNumericRepair.Subtract(0L, removal,
                    "data_girls.AddFans_Oshihen removal");
                plan.Add(fan, delta,
                    "data_girls.AddFans_Oshihen preflight");
                if (fan.hardcoreness == resources.fanType.hardcore)
                    hardcore = WideNumericRepair.Add(hardcore,
                        WideNumericMath.RoundRatioToEven(removal, 3L, 4L),
                        "data_girls.AddFans_Oshihen hardcore");
                else if (fan.hardcoreness == resources.fanType.casual)
                    casual = WideNumericRepair.Add(casual,
                        WideNumericMath.RoundRatioToEven(removal, 1L, 4L),
                        "data_girls.AddFans_Oshihen casual");
            }
            PlanFansWeighted(plan, casual, resources.fanType.casual, null, girl);
            PlanFansWeighted(plan, hardcore, resources.fanType.hardcore, null, girl);
            plan.CommitMutations();
            plan.RunCallbacks();
        }

        private static List<long> BalancedDeltas(long quotient, long remainder, int count)
        {
            List<long> values = new List<long>(count);
            long sign = remainder < 0L ? -1L : 1L;
            long extra = remainder < 0L ? -remainder : remainder;
            for (int index = 0; index < count; index++)
                values.Add(index < extra ? WideNumericRepair.Add(quotient, sign,
                    "wide fan balanced remainder") : quotient);
            return values;
        }

        private static void ReportFansChanged()
        {
            MethodInfo method = AccessTools.Method(typeof(data_girls), "UpdateFansDisplay", Type.EmptyTypes);
            if (method != null) method.Invoke(null, null);
            else resources._OnFansChange();
        }

        private sealed class SskTempResult
        {
            internal readonly data_girls.girls Girl;
            internal readonly long FamePoints;
            internal long Votes;

            internal SskTempResult(data_girls.girls girl, long famePoints)
            {
                Girl = girl;
                FamePoints = famePoints;
            }
        }

        private sealed class SskExpectedPlace
        {
            internal readonly data_girls.girls Girl;
            internal readonly int Place;

            internal SskExpectedPlace(data_girls.girls girl, int place)
            {
                Girl = girl;
                Place = place;
            }
        }
    }

    internal static class WideNumericContinuationPatchHealth
    {
        private static readonly object Sync = new object();
        private static readonly HashSet<string> Expected = new HashSet<string>(StringComparer.Ordinal)
        {
            "resources.Set(type,Int64)", "resources.GetScandalPointsTotal()",
            "resources.UpdateScandalPointsCounter()",
            "ScandalPoints.GetScandalPoints()", "ScandalPoints.GetPointsTooltip()",
            "ScandalPoints_Line.Set(_points)",
            "ResourceDisplay.OnResourceSet(Int32)",
            "ResourceDisplay.OnResourceChange(Int32)", "Dating.JobSearch_1()",
            "SEvent_Tour.GetFamePenalty(Int32)",
            "Show_Release.SetFame()",
            "resources.GetRentPerFloor(Int32)", "resources.GetRoomRent(_type,Int32)",
            "resources.Money_Rent(Boolean)", "resources.Money_StaffSalary()",
            "resources.Money_GirlsSalary()", "resources.Money_WeeklyExpenses()",
            "resources.Money_DailyProfit()", "resources.OnNewDay()", "resources.OnNewWeek()",
            "resources.Buzz_Daily()", "resources.Fame_Daily()", "resources.DailyFansChange()",
            "business.GetTotalWeeklyProfit()", "business.GetTotalWeeklyBuzz()",
            "business.GetTotalWeeklyFame()", "business.AddWeeklyEarnings()",
            "business._proposal.get_payment()", "business._proposal.set_payment(Int32)",
            "business._proposal.get_newFans()", "business._proposal.set_newFans(Int32)",
            "business.GenerateProposal(_data,_staff,Int32)",
            "business.GenerateProposal(_data,_staff,Int32)-fans",
            "business.AddActiveProposal(_proposal)", "Business_Popup.Set(_proposal)",
            "Contracts_Line.Set(active_proposal)", "business.DoWeeklyFans()",
            "business.AddFans(_proposal)", "business.AddFans(active_proposal)",
            "Rivals.OnNewMonth(Boolean)",
            "loans.GetTotalPaymentPerWeek()",
            "loans._loan.GetInterest()", "loans._loan.GetTotalAmount()",
            "loans._loan.GetDebt()", "loans._loan.RecalcPaymentPerWeek()",
            "loans.GetNewLoanID()", "Theaters._theater.GetSubscribers()",
            "Theaters._theater.GetSubRevenue()", "Theaters._theater.GetNewSubscribers()",
            "Theaters._theater.GetAvgRevenue()", "Theaters._theater.GetNumberOfVisitors()",
            "Theaters.GetLastWeekEarning()", "Theaters.CompleteDay()",
            "Stats.OnNewWeek()", "Stats.data.money.GetTotalIncome(Int32)",
            "tasks._story_data.Set_Ch3_Aya_Fans()", "tasks.AddTask_SummerGames(String)",
            "tasks._story_data.Ch4_Did_Qualify()", "Cafes.GetFansNeededToBuild(Int32)",
            "Cafes.GetLastWeekEarning()", "Cafes.RenderRooms(Boolean)",
            "Cafes.GetMoneyPerDay()", "Cafes.GetTooltip()",
            "singles._single.GetTotalSales()",
            "singles._single.GetTotalNewFans()", "singles._single.GetMoney()",
            "singles.GenerateSales(_single)",
            "singles.ValueAfterMarketing(Int64,Single,Single,_fan,_special_type,_result)",
            "Shows._show.GetAverageParam(List<Int64>)", "Shows._show.GetAverageParam(List<Int32>)",
            "Shows._show.GetTotalParam(List<Int32>)", "Shows._show.GetAllNewFans()",
            "Groups._group.GetFansOfType(demographic)", "Groups._group.GetFansOfType(Nullable)",
            "SEvent_Tour.tour.GetProfit()", "SEvent_Tour.tour.AddRevenue(Int32)",
            "SEvent_Tour.tour.AddFans(Int32)", "SEvent_Tour.tour.SelectCountry(country,Int32)",
            "SEvent_Tour.tour.GetAttendance(selectedCountry)",
            "SEvent_Tour.tour.GetNewFansByAttendance(Int32)",
            "SEvent_Tour.tour.GetProductionCost()", "SEvent_Tour.tour.IsEnoughMoney()",
            "SEvent_Tour.FinishTour()", "SEvent_Tour.tour.Initiate()",
            "singles.GetNewSingleID()", "Shows.GetNewShowID()",
            "SEvent_Tour.LoadFunction()", "singles.LoadFunction()", "Shows.LoadFunction()",
            "Theaters.LoadFunction()", "Stats.LoadFunction()", "tasks.LoadFunction()",
            "loans.LoadFunction()", "Cafes.LoadFunction()", "business.LoadFunction()",
            "data_girls.AddFans_Equally(Int64,_fan,List)",
            "data_girls.AddFans_Equally(Int64,List)",
            "data_girls.AddFans(Int64,Nullable,List,girl)",
            "data_girls.AddFans_Oshihen(Int64,girl)",
            "data_girls.girls.AddFans(Int64,Nullable)",
            "data_girls.girls.AddFans(demographic,Int64)",
            "data_girls.girls.GetFansToAdd(Int64,Single)",
            "data_girls.girls.AddFans(Nullable,Single,Boolean)",
            "singles.AddBonusFans(_single)", "singles.AddNewFans(_single)",
            "Shows._show.SetNewFans(Int32)", "Shows._show.SetSales()",
            "Cafes._cafe.GetMoneyToAdd()", "Cafes._cafe.GetFansToAdd()",
            "Cafes._cafe.GetAverageProfit(_dish)",
            "Cafes._cafe.GetAverageNewFans(_dish)",
            "Cafes.RenderCafe(_room,_cafe)", "Room_Cafe.LaunchFloats(_stat)",
            "Cafes.popular-selector-2", "Cafes.popular-selector-3",
            "Cafes.popular-selector-6", "Cafes.popular-selector-7",
            "SEvent_Concerts._projectedValues.GetNumberOfSoldTickets()",
            "SEvent_Concerts._projectedValues.GetRevenue()",
            "SEvent_Concerts._projectedValues.GetActualProfit()",
            "SEvent_Concerts._projectedValues.SetAttendance()",
            "SEvent_SSK._SSK.GetProductionCost()", "SEvent_SSK._SSK.TotalProductionCost()",
            "SEvent_SSK._SSK.GenerateResults()", "Research.category.Buy_Points()",
            "Event_Requirements.Check(_action)", "vn_actions.DoResource(String,String,_activeEvent)",
            "data_girls.girls.GetExpectedSalary()",
            "data_girls.girls.GetExpectedSalary_Total()",
            "data_girls.girls.IncreaseSalary()", "data_girls.girls.LowerSalary()",
            "data_girls.girls.Earn(Int64)", "data_girls.girls.GetTotalEarnings()",
            "agency.GetRoomRent(_type,Int32)", "agency.GetRoomTooltip(_type)",
            "staff._staff.Severance()", "staff._staff.CanFire_Severance()",
            "staff._staff.Fire_Severance()", "staff._staff.GetTooltip_Fire_Severance()",
            "ContextMenu_Office.SetFireColor()", "CM_Dance.SetFireColor()",
            "Staff_Fire.DoComplete()",
            "ExtensionMethods.formatMoney(Int64,Boolean,Boolean,Boolean)",
            "Salary_Line.Render()", "data_girls.girls.GetEarningsString()",
            "tooltip_money.BusinessContracts()", "tooltip_money.Media()",
            "tooltip_money.Cafe()", "tooltip_money.Theater()",
            "tooltip_money.IdolSalaries()", "tooltip_money.StaffSalaries()",
            "tooltip_money.Rent()", "tooltip_money.Loans()",
            "Loans_Line.Set(_loan)", "Loans_Popup.RenderDetails()",
            "Theater_Popup.Render_Pricing()", "tasks.OnNewDay()",
            "tasks._task.GetDescription_Custom()", "Summer_Games_Button.Render()",
            "Debug_Popup.CompleteAllTasks()",
            "vn_actions.DoCustom(String)",
            "SEvent_Tour.country.GetCostByLevel(Int32)",
            "SEvent_Tour.country.GetRevenueByLevel(Int32)",
            "SEvent_Tour.country.GetRevenueByCapacity(Int32)",
            "SEvent_Tour.country.GetSaving(Int32)",
            "SEvent_Tour.tour.GetSaving()", "SEvent_Tour.tour.GetTotalAudience()",
            "SEvent_Tour.tour.GetNewFans()", "Tour_Popup.UpdateBar()",
            "Tour_New_Popup.Render()", "SEvent_Button_Tour.UpdateData_Tour()",
            "Tour_Popup_Country.AnimateAttendance(Int32)",
            "Tour_Popup_Country.AnimateFans(Int32)", "Tour_Star.SetTooltip()",
            "SEvent_Button_Tour_Finished.Set(tour)",
            "Shows._show.GetFans(Nullable<Int32>)", "Shows._show.SetRevenue()",
            "Show_Released_Button.UpdateParams()",
            "singles._single.ReleaseData_FanSatisfaction()",
            "business._data.StringLiability(Boolean)",
            "singles.AddMoney(_single)", "Single_Popup.CalculateProductionCost()",
            "Single_Release.AnimateSales(Single)",
            "Single_Release.AnimateNewFans(Single)",
            "Single_Release.AnimateNewFansBonus(Single)",
            "Show_Release.AnimateSales(Single)",
            "Shows._show.GetButtonTooltip()", "Theater_Stats_Line.Set(_stat)",
            "Group_Appeal_Fan.SetSub(_subscriber)", "Group_Appeal_Popup.RenderTab()",
            "Cafe_Stat.Set(_cafe,_stat,DateTime)",
            "data_girls.GetNewGirlID()", "staff.GetNewStaffID()",
            "Groups.GetNextGroupID()", "Theaters.GetNextTheaterID()",
            "Cafes.GetNextTheaterID()", "Cafes._cafe.GetNewDishID()",
            "agency._floor.addRoom(_type)", "agency.addRoom(Int32,Boolean)",
            "SEvent_Concerts._concert.Initiate()", "SEvent_SSK._SSK.Initiate()",
            "Activities.Performance()", "Activities.Promotion()",
            "Activities.SpaTreatment()", "Activities.GetFansToAdd(Single)",
            "Activities.GetTooltipForWidget(ActivityType)", "Activities._activity.GetLevelUp()",
            "business.Decline()",
            "Stats.OnBusinessProposalAccepted(_proposal)",
            "Stats.OnBusinessProposalAccepted(_proposal)-wide-top-payment",
            "Stats.Reset()-wide-top-payment",
            "data_girls.girls.Set_Injured()", "data_girls.girls.Set_Depressed()",
            "Date_Flirt.DoFlirt(girl)",
            "Relationships_Player.AddPoints(_type,girl,Int32)",
            "Concert_Popup.TriggerAccident(Boolean)",
            "Tour_Popup_Country.Set(tour,selectedCountry)",
            "girls_trivia._data.Use()", "Graduation_Trivia._trivia.Use()",
            "Shows._show.NewEpisode()", "Shows._show.OnRelaunchFinish()"
        };
        private static readonly HashSet<string> Resolved = new HashSet<string>(StringComparer.Ordinal);
        private static string failure = string.Empty;

        internal static int ExpectedTargetMethodCount { get { return Expected.Count; } }
        internal static int ResolvedTargetMethodCount { get { lock (Sync) { return Resolved.Count; } } }
        internal static bool IsHealthy
        {
            get { lock (Sync) { return string.IsNullOrEmpty(failure) && Resolved.Count == Expected.Count; } }
        }
        internal static string Failure { get { lock (Sync) { return failure; } } }

        internal static MethodBase Resolve(string id, Type type, string name,
            Type[] parameters, Type result, bool isStatic)
        {
            MethodInfo method = AccessTools.Method(type, name, parameters);
            if (method == null || method.DeclaringType != type || method.ReturnType != result ||
                method.IsStatic != isStatic)
            {
                ReportFailure("A33.2-A33.6 could not resolve frozen target " + id + ".");
                throw new MissingMethodException(type.FullName, name);
            }
            lock (Sync)
            {
                if (!Expected.Contains(id))
                {
                    failure = "A33.2-A33.6 resolved unrecognized target " + id + ".";
                }
                else
                {
                    Resolved.Add(id);
                }
            }
            return method;
        }

        internal static void ReportFailure(string value)
        {
            lock (Sync) { if (string.IsNullOrEmpty(failure)) failure = value ?? "unknown continuation patch failure"; }
        }
    }
}
