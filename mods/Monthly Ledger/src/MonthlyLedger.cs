extern alias UiFrameworkReference;

using System;
using System.Collections.Generic;
using HarmonyLib;
using IMDataCore;
using ModLocalizationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IMUiKit = UiFrameworkReference::IMUiFramework.IMUiKit;
using PopupScaffold = UiFrameworkReference::IMUiFramework.PopupScaffold;

namespace MonthlyLedger
{
    internal static class MonthlyLedgerConstants
    {
        internal const string HarmonyId = "com.cosmo.monthlyledger";
        internal const string DataCoreHarmonyId = "com.cosmo.imdatacore";
        internal const string UiFrameworkHarmonyId = "com.cosmo.imuiframework";
        internal const string ModButtonsHarmonyId = "com.cosmo.modbuttons";
        internal const string PopupManagerStartMethodName = "Start";
        internal const string PopupObjectName = "MonthlyLedger_Popup";
        internal const string NavigationRowObjectName = "MonthlyLedger_Navigation";
        internal const string PreviousButtonObjectName = "MonthlyLedger_PreviousMonth";
        internal const string NextButtonObjectName = "MonthlyLedger_NextMonth";
        internal const string MonthLabelObjectName = "MonthlyLedger_Month";
        internal const string SummaryRowObjectName = "MonthlyLedger_Summary";
        internal const string IncomeSectionObjectName = "MonthlyLedger_Income";
        internal const string ExpenseSectionObjectName = "MonthlyLedger_Expenses";
        internal const string EmptyStateObjectName = "MonthlyLedger_Empty";
        internal const string WarningObjectName = "MonthlyLedger_Warning";
        internal const string CategoryObjectPrefix = "MonthlyLedger_Category_";
        internal const string CategoryHeaderObjectName = "CategoryHeader";
        internal const string RecordRowObjectName = "Record";
        internal const string SummaryCardObjectPrefix = "SummaryCard_";
        internal const string LabelObjectName = "Label";
        internal const string ValueObjectName = "Value";
        internal const string DetailObjectName = "Detail";
        internal const string DateObjectName = "Date";
        internal const string PreviousArrowText = "<";
        internal const string NextArrowText = ">";
        internal const string DateFormatMonth = "DATETIME__MONTH";
        internal const string DateFormatRecord = "DATETIME__SHORT";
        internal const string PositivePrefix = "+";
        internal const string NegativePrefix = "-";
        internal const string CategorySignSeparator = "|";
        internal const string CategoryLocalizationPrefix = "category.";
        internal const string DetailLocalizationPrefix = "detail.";
        internal const string LogPrefix = "[MonthlyLedger] ";

        internal const string CategoryContracts = "contracts";
        internal const string CategorySingles = "singles";
        internal const string CategoryShows = "shows";
        internal const string CategoryCafes = "cafes";
        internal const string CategoryTheaters = "theaters";
        internal const string CategoryConcerts = "concerts";
        internal const string CategoryTours = "tours";
        internal const string CategoryElections = "elections";
        internal const string CategoryActivities = "activities";
        internal const string CategoryAgency = "agency";
        internal const string CategoryAuditions = "auditions";
        internal const string CategoryResearch = "research";
        internal const string CategoryStaffing = "staffing";
        internal const string CategoryIdolSalaries = "idol_salaries";
        internal const string CategoryStaffSalaries = "staff_salaries";
        internal const string CategoryRent = "rent";
        internal const string CategoryLoans = "loans";
        internal const string CategoryEvents = "events";
        internal const string CategoryStory = "story";
        internal const string CategoryExternalAdjustments = "external_adjustments";
        internal const string CategoryOther = "other";

        internal const string DetailBusinessContracts = "business_contracts";
        internal const string DetailSingleRelease = "single_release";
        internal const string DetailShowEpisode = "show_episode";
        internal const string DetailCafe = "cafe";
        internal const string DetailTheater = "theater";
        internal const string DetailConcert = "concert";
        internal const string DetailTour = "tour";
        internal const string DetailElection = "election";
        internal const string DetailPerformance = "performance";
        internal const string DetailStreamingIncome = "streaming_income";
        internal const string DetailSpa = "spa";
        internal const string DetailAudition = "audition";
        internal const string DetailLoan = "loan";
        internal const string DetailIdolSalaries = "idol_salaries";
        internal const string DetailStaffSalaries = "staff_salaries";
        internal const string DetailRent = "rent";
        internal const string DetailLoanPayment = "loan_payment";
        internal const string SectionIncome = "income";
        internal const string SectionExpense = "expense";

        internal const string FallbackContracts = "Contracts";
        internal const string FallbackSingles = "Singles";
        internal const string FallbackShows = "Shows";
        internal const string FallbackCafe = "Cafe";
        internal const string FallbackTheater = "Theater";
        internal const string FallbackConcerts = "Concerts";
        internal const string FallbackTours = "Tours";
        internal const string FallbackElections = "Elections";
        internal const string FallbackActivities = "Activities";
        internal const string FallbackAgency = "Agency";
        internal const string FallbackAuditions = "Auditions";
        internal const string FallbackResearch = "Research";
        internal const string FallbackStaff = "Staff";
        internal const string FallbackLoans = "Loans";
        internal const string FallbackIdolSalaries = "Idol salaries";
        internal const string FallbackStaffSalaries = "Staff salaries";
        internal const string FallbackRent = "Rent";
        internal const string FallbackEvents = "Events";
        internal const string FallbackStory = "Story";
        internal const string FallbackExternalAdjustments = "External adjustments";

        internal const string KeyTitle = "ui.title";
        internal const string KeyIncome = "ui.income";
        internal const string KeyExpenses = "ui.expenses";
        internal const string KeyNetRevenue = "ui.net_revenue";
        internal const string KeyNoCompleteMonth = "state.no_complete_month";
        internal const string KeyNoTransactions = "state.no_transactions";
        internal const string KeyTruncated = "state.truncated";
        internal const string KeyLoadFailed = "state.load_failed";
        internal const string KeyDependencyError = "state.dependency_error";
        internal const string KeyCoverageFailed = "state.coverage_failed";

        internal const string VanillaContractsKey = "TIP__CONTRACTS";
        internal const string VanillaSinglesKey = "SINGLES";
        internal const string VanillaShowsKey = "SHOWS";
        internal const string VanillaCafeKey = "TIP__CAFE";
        internal const string VanillaTheaterKey = "TIP__THEATER";
        internal const string VanillaConcertKey = "SEVENT__CONCERT";
        internal const string VanillaTourKey = "SEVENT__WORLD_TOUR";
        internal const string VanillaElectionKey = "SEVENT__ELECTION";
        internal const string VanillaActivitiesKey = "LP_TITLE__ACTIVITIES";
        internal const string VanillaAuditionsKey = "AUDITIONS";
        internal const string VanillaResearchKey = "LP_TITLE__RESEARCH";
        internal const string VanillaStaffKey = "LP_TITLE__STAFF";
        internal const string VanillaLoansKey = "LOANS";
        internal const string VanillaIdolSalariesKey = "TIP__IDOL_SALARIES";
        internal const string VanillaStaffSalariesKey = "TIP__STAFF_SALARIES";
        internal const string VanillaRentKey = "TIP__RENT";
        internal const string VanillaEventsKey = "STATS__EVENTS";
        internal const string VanillaStoryKey = "STORY";
        internal const string VanillaOtherKey = "NOTIF__OTHER";
        internal const string VanillaSingleReleaseKey = "ACTIVITIES__RELEASEASINGLE";
        internal const string VanillaShowEpisodeKey = "SHOW__EPISODE";
        internal const string VanillaCafeIncomeKey = "CAFE__TOTAL_INCOME";
        internal const string VanillaStreamingKey = "THEATER__STREAMING";
        internal const string VanillaPerformanceKey = "ACTIVITIES__PERFORMANCE";
        internal const string VanillaSpaTreatmentKey = "ACTIVITIES__SPATREATMENT";
        internal const string VanillaAuditionKey = "AUDITION";
        internal const string VanillaLoanKey = "LOAN";
        internal const string VanillaLoanPaymentKey = "LOANS__WEEKLY_PAYMENT";

        internal const string FallbackTitle = "Monthly Ledger";
        internal const string FallbackIncome = "Income";
        internal const string FallbackExpenses = "Expenses";
        internal const string FallbackNetRevenue = "Net revenue";
        internal const string FallbackNoCompleteMonth = "No fully recorded completed month is available yet.";
        internal const string FallbackNoTransactions = "No transactions were recorded for this month.";
        internal const string FallbackTruncated = "This month contains more records than can be displayed. Totals shown are incomplete.";
        internal const string FallbackLoadFailed = "Monthly ledger data could not be loaded: {0}";
        internal const string FallbackDependencyError = "Monthly Ledger requires an active, compatible installation of {0}.";
        internal const string FallbackCoverageFailed = "Exact monthly ledger coverage has not started for this save.";
        internal const string FallbackOther = "Other";
        internal const string DataCoreDependencyDisplayName = "IM Data Core 1.2.0+";
        internal const string MinimumDataCoreAssemblyVersionText = "1.2.0.0";

        internal const int PopupTypeValue = 997;
        internal const int FirstDayOfMonth = 1;
        internal const int FullMonthOffsetAfterCoverage = 1;
        internal const int PreviousMonthOffset = -1;
        internal const int NextMonthOffset = 1;
        internal const int MaximumTransactionCount = 10000;
        internal const int ZeroIndex = 0;
        internal const int TitleFontSize = 24;
        internal const int SummaryLabelFontSize = 17;
        internal const int SummaryValueFontSize = 23;
        internal const int SectionFontSize = 24;
        internal const int CategoryFontSize = 19;
        internal const int RecordFontSize = 16;
        internal const int StateFontSize = 20;
        internal const int PopupTitleFontSize = 28;
        internal const int CloseButtonFontSize = 18;
        internal const int LayoutPadding = 8;
        internal const int CategoryPadding = 6;
        internal const int RecordLeftPadding = 18;
        internal const float PopupWidth = 920f;
        internal const float PopupHeight = 480f;
        internal const float PopupTitleHeight = 38f;
        internal const float PopupTitleHorizontalPadding = 48f;
        internal const float PopupTitleTopOffset = -8f;
        internal const float PopupScrollLeftInset = 16f;
        internal const float PopupScrollRightInset = -16f;
        internal const float PopupScrollBottomInset = 56f;
        internal const float PopupScrollTopInset = -58f;
        internal const float PopupViewportScrollbarGutter = -30f;
        internal const float PopupScrollbarWidth = 12f;
        internal const float PopupScrollbarRightOffset = -2f;
        internal const float CloseButtonWidth = 140f;
        internal const float CloseButtonHeight = 32f;
        internal const float CloseButtonBottomOffset = 10f;
        internal const float NavigationHeight = 44f;
        internal const float ArrowButtonWidth = 52f;
        internal const float ArrowButtonHeight = 34f;
        internal const float MonthLabelWidth = 620f;
        internal const float SummaryHeight = 86f;
        internal const float SummaryCardWidth = 260f;
        internal const float SectionHeadingHeight = 34f;
        internal const float CategoryHeaderHeight = 32f;
        internal const float RecordHeight = 27f;
        internal const float DetailedRecordLineHeight = 22f;
        internal const float DetailedRecordVerticalPadding = 8f;
        internal const float RecordDateWidth = 130f;
        internal const float RecordDetailWidth = 480f;
        internal const float RecordAmountWidth = 200f;
        internal const float LayoutSpacing = 7f;
        internal const float SmallLayoutSpacing = 3f;
        internal const float ScrollTopPosition = 1f;
        internal const float UnconstrainedSize = -1f;
        internal const float ZeroSize = 0f;
        internal const float FlexibleWidth = 1f;
        internal const long ZeroMoney = 0L;
        internal const byte CardColorRed = 246;
        internal const byte CardColorGreen = 246;
        internal const byte CardColorBlue = 246;
        internal const byte HeaderColorRed = 228;
        internal const byte HeaderColorGreen = 231;
        internal const byte HeaderColorBlue = 238;
        internal const byte OpaqueAlpha = 255;

        internal static readonly string[] CategoryOrder =
        {
            CategoryContracts, CategorySingles, CategoryShows, CategoryCafes, CategoryTheaters, CategoryConcerts, CategoryTours, CategoryElections,
            CategoryActivities, CategoryAgency, CategoryAuditions, CategoryResearch, CategoryStaffing, CategoryIdolSalaries, CategoryStaffSalaries,
            CategoryRent, CategoryLoans, CategoryEvents, CategoryStory, CategoryExternalAdjustments, CategoryOther
        };

        internal static readonly Version MinimumDataCoreAssemblyVersion = new Version(MinimumDataCoreAssemblyVersionText);
    }

    internal static class MonthlyLedgerText
    {
        internal static string Title { get { return ModLocalization.Get(MonthlyLedgerConstants.KeyTitle, MonthlyLedgerConstants.FallbackTitle); } }
        internal static string Income { get { return ModLocalization.Get(MonthlyLedgerConstants.KeyIncome, MonthlyLedgerConstants.FallbackIncome); } }
        internal static string Expenses { get { return ModLocalization.Get(MonthlyLedgerConstants.KeyExpenses, MonthlyLedgerConstants.FallbackExpenses); } }
        internal static string NetRevenue { get { return ModLocalization.Get(MonthlyLedgerConstants.KeyNetRevenue, MonthlyLedgerConstants.FallbackNetRevenue); } }
        internal static string NoCompleteMonth { get { return ModLocalization.Get(MonthlyLedgerConstants.KeyNoCompleteMonth, MonthlyLedgerConstants.FallbackNoCompleteMonth); } }
        internal static string NoTransactions { get { return ModLocalization.Get(MonthlyLedgerConstants.KeyNoTransactions, MonthlyLedgerConstants.FallbackNoTransactions); } }
        internal static string Truncated { get { return ModLocalization.Get(MonthlyLedgerConstants.KeyTruncated, MonthlyLedgerConstants.FallbackTruncated); } }
        internal static string CoverageFailed { get { return ModLocalization.Get(MonthlyLedgerConstants.KeyCoverageFailed, MonthlyLedgerConstants.FallbackCoverageFailed); } }

        internal static string LoadFailed(string error)
        {
            return string.Format(ModLocalization.Get(MonthlyLedgerConstants.KeyLoadFailed, MonthlyLedgerConstants.FallbackLoadFailed), error ?? string.Empty);
        }

        internal static string DependencyError(string dependencyName)
        {
            return string.Format(ModLocalization.Get(MonthlyLedgerConstants.KeyDependencyError, MonthlyLedgerConstants.FallbackDependencyError), dependencyName ?? string.Empty);
        }

        internal static string Category(string categoryCode)
        {
            string vanillaKey;
            string fallback;
            ResolveCategoryFallback(categoryCode, out vanillaKey, out fallback);
            return ResolveVanillaOrMod(vanillaKey, MonthlyLedgerConstants.CategoryLocalizationPrefix + categoryCode, fallback);
        }

        internal static string Detail(string detailCode, string categoryCode)
        {
            string fallback = Category(categoryCode);
            string vanillaKey = ResolveDetailVanillaKey(detailCode);
            return ResolveVanillaOrMod(vanillaKey, MonthlyLedgerConstants.DetailLocalizationPrefix + detailCode, fallback);
        }

        internal static string Label(string vanillaKey, string modKey, string fallback)
        {
            return ResolveVanillaOrMod(vanillaKey, modKey, fallback);
        }

        internal static string LocalizedToken(string token)
        {
            string value;
            if (!string.IsNullOrEmpty(token)
                && Language.Data != null
                && Language.Data.TryGetValue(token, out value)
                && !string.IsNullOrEmpty(value))
            {
                return value;
            }

            return token ?? string.Empty;
        }

        private static void ResolveCategoryFallback(string categoryCode, out string vanillaKey, out string fallback)
        {
            vanillaKey = string.Empty;
            fallback = MonthlyLedgerConstants.FallbackOther;
            switch (categoryCode)
            {
                case MonthlyLedgerConstants.CategoryContracts: vanillaKey = MonthlyLedgerConstants.VanillaContractsKey; fallback = MonthlyLedgerConstants.FallbackContracts; break;
                case MonthlyLedgerConstants.CategorySingles: vanillaKey = MonthlyLedgerConstants.VanillaSinglesKey; fallback = MonthlyLedgerConstants.FallbackSingles; break;
                case MonthlyLedgerConstants.CategoryShows: vanillaKey = MonthlyLedgerConstants.VanillaShowsKey; fallback = MonthlyLedgerConstants.FallbackShows; break;
                case MonthlyLedgerConstants.CategoryCafes: vanillaKey = MonthlyLedgerConstants.VanillaCafeKey; fallback = MonthlyLedgerConstants.FallbackCafe; break;
                case MonthlyLedgerConstants.CategoryTheaters: vanillaKey = MonthlyLedgerConstants.VanillaTheaterKey; fallback = MonthlyLedgerConstants.FallbackTheater; break;
                case MonthlyLedgerConstants.CategoryConcerts: vanillaKey = MonthlyLedgerConstants.VanillaConcertKey; fallback = MonthlyLedgerConstants.FallbackConcerts; break;
                case MonthlyLedgerConstants.CategoryTours: vanillaKey = MonthlyLedgerConstants.VanillaTourKey; fallback = MonthlyLedgerConstants.FallbackTours; break;
                case MonthlyLedgerConstants.CategoryElections: vanillaKey = MonthlyLedgerConstants.VanillaElectionKey; fallback = MonthlyLedgerConstants.FallbackElections; break;
                case MonthlyLedgerConstants.CategoryActivities: vanillaKey = MonthlyLedgerConstants.VanillaActivitiesKey; fallback = MonthlyLedgerConstants.FallbackActivities; break;
                case MonthlyLedgerConstants.CategoryAgency: fallback = MonthlyLedgerConstants.FallbackAgency; break;
                case MonthlyLedgerConstants.CategoryAuditions: vanillaKey = MonthlyLedgerConstants.VanillaAuditionsKey; fallback = MonthlyLedgerConstants.FallbackAuditions; break;
                case MonthlyLedgerConstants.CategoryResearch: vanillaKey = MonthlyLedgerConstants.VanillaResearchKey; fallback = MonthlyLedgerConstants.FallbackResearch; break;
                case MonthlyLedgerConstants.CategoryStaffing: vanillaKey = MonthlyLedgerConstants.VanillaStaffKey; fallback = MonthlyLedgerConstants.FallbackStaff; break;
                case MonthlyLedgerConstants.CategoryLoans: vanillaKey = MonthlyLedgerConstants.VanillaLoansKey; fallback = MonthlyLedgerConstants.FallbackLoans; break;
                case MonthlyLedgerConstants.CategoryIdolSalaries: vanillaKey = MonthlyLedgerConstants.VanillaIdolSalariesKey; fallback = MonthlyLedgerConstants.FallbackIdolSalaries; break;
                case MonthlyLedgerConstants.CategoryStaffSalaries: vanillaKey = MonthlyLedgerConstants.VanillaStaffSalariesKey; fallback = MonthlyLedgerConstants.FallbackStaffSalaries; break;
                case MonthlyLedgerConstants.CategoryRent: vanillaKey = MonthlyLedgerConstants.VanillaRentKey; fallback = MonthlyLedgerConstants.FallbackRent; break;
                case MonthlyLedgerConstants.CategoryEvents: vanillaKey = MonthlyLedgerConstants.VanillaEventsKey; fallback = MonthlyLedgerConstants.FallbackEvents; break;
                case MonthlyLedgerConstants.CategoryStory: vanillaKey = MonthlyLedgerConstants.VanillaStoryKey; fallback = MonthlyLedgerConstants.FallbackStory; break;
                case MonthlyLedgerConstants.CategoryExternalAdjustments: fallback = MonthlyLedgerConstants.FallbackExternalAdjustments; break;
                case MonthlyLedgerConstants.CategoryOther: vanillaKey = MonthlyLedgerConstants.VanillaOtherKey; break;
            }
        }

        private static string ResolveDetailVanillaKey(string detailCode)
        {
            switch (detailCode)
            {
                case MonthlyLedgerConstants.DetailBusinessContracts: return MonthlyLedgerConstants.VanillaContractsKey;
                case MonthlyLedgerConstants.DetailSingleRelease: return MonthlyLedgerConstants.VanillaSingleReleaseKey;
                case MonthlyLedgerConstants.DetailShowEpisode: return MonthlyLedgerConstants.VanillaShowEpisodeKey;
                case MonthlyLedgerConstants.DetailCafe: return MonthlyLedgerConstants.VanillaCafeIncomeKey;
                case MonthlyLedgerConstants.DetailTheater: return MonthlyLedgerConstants.VanillaTheaterKey;
                case MonthlyLedgerConstants.DetailConcert: return MonthlyLedgerConstants.VanillaConcertKey;
                case MonthlyLedgerConstants.DetailTour: return MonthlyLedgerConstants.VanillaTourKey;
                case MonthlyLedgerConstants.DetailElection: return MonthlyLedgerConstants.VanillaElectionKey;
                case MonthlyLedgerConstants.DetailPerformance: return MonthlyLedgerConstants.VanillaPerformanceKey;
                case MonthlyLedgerConstants.DetailStreamingIncome: return MonthlyLedgerConstants.VanillaStreamingKey;
                case MonthlyLedgerConstants.DetailSpa: return MonthlyLedgerConstants.VanillaSpaTreatmentKey;
                case MonthlyLedgerConstants.DetailAudition: return MonthlyLedgerConstants.VanillaAuditionKey;
                case MonthlyLedgerConstants.DetailLoan: return MonthlyLedgerConstants.VanillaLoanKey;
                case MonthlyLedgerConstants.DetailIdolSalaries: return MonthlyLedgerConstants.VanillaIdolSalariesKey;
                case MonthlyLedgerConstants.DetailStaffSalaries: return MonthlyLedgerConstants.VanillaStaffSalariesKey;
                case MonthlyLedgerConstants.DetailRent: return MonthlyLedgerConstants.VanillaRentKey;
                case MonthlyLedgerConstants.DetailLoanPayment: return MonthlyLedgerConstants.VanillaLoanPaymentKey;
                default: return string.Empty;
            }
        }

        private static string ResolveVanillaOrMod(string vanillaKey, string modKey, string fallback)
        {
            string value;
            if (!string.IsNullOrEmpty(vanillaKey)
                && Language.Data != null
                && Language.Data.TryGetValue(vanillaKey, out value)
                && !string.IsNullOrEmpty(value))
            {
                return value;
            }

            return ModLocalization.Get(modKey, fallback);
        }
    }

    public static class MonthlyLedgerActions
    {
        public static void OpenLedger()
        {
            MonthlyLedgerRuntime.Open();
        }
    }

    [HarmonyPatch(typeof(PopupManager), MonthlyLedgerConstants.PopupManagerStartMethodName)]
    [HarmonyAfter(new string[] { MonthlyLedgerConstants.DataCoreHarmonyId, MonthlyLedgerConstants.UiFrameworkHarmonyId, MonthlyLedgerConstants.ModButtonsHarmonyId })]
    internal static class PopupManager_Start_MonthlyLedger_Patch
    {
        private static void Postfix(PopupManager __instance)
        {
            MonthlyLedgerRuntime.Initialize(__instance);
        }
    }

    internal sealed class LedgerCategoryGroup
    {
        internal string CategoryCode = string.Empty;
        internal bool IsIncome;
        internal long Total;
        internal readonly List<IMDataCoreMoneyTransaction> Records = new List<IMDataCoreMoneyTransaction>();
    }

    internal static class MonthlyLedgerRuntime
    {
        private static readonly PopupManager._type PopupType = (PopupManager._type)MonthlyLedgerConstants.PopupTypeValue;
        private static readonly Color SummaryCardColor = new Color32(MonthlyLedgerConstants.CardColorRed, MonthlyLedgerConstants.CardColorGreen, MonthlyLedgerConstants.CardColorBlue, MonthlyLedgerConstants.OpaqueAlpha);
        private static readonly Color CategoryHeaderColor = new Color32(MonthlyLedgerConstants.HeaderColorRed, MonthlyLedgerConstants.HeaderColorGreen, MonthlyLedgerConstants.HeaderColorBlue, MonthlyLedgerConstants.OpaqueAlpha);

        private static PopupManager popupManager;
        private static PopupScaffold scaffold;
        private static DateTime earliestMonth;
        private static DateTime latestMonth;
        private static DateTime selectedMonth;

        internal static void Initialize(PopupManager manager)
        {
            if (manager == null)
            {
                return;
            }

            popupManager = manager;
            if (scaffold != null && scaffold.IsValid)
            {
                return;
            }

            IMUiKit.Initialize(manager);
            PopupManager._popup existing = manager.GetByType(PopupType);
            if (existing != null)
            {
                Debug.LogError(MonthlyLedgerConstants.LogPrefix + MonthlyLedgerText.LoadFailed(MonthlyLedgerConstants.PopupObjectName));
                return;
            }

            PopupScaffold created;
            if (!IMUiKit.TryCreateRegisteredPopupScaffold(
                PopupType,
                MonthlyLedgerConstants.PopupObjectName,
                MonthlyLedgerText.Title,
                new Vector2(MonthlyLedgerConstants.PopupWidth, MonthlyLedgerConstants.PopupHeight),
                true,
                true,
                out created))
            {
                return;
            }

            scaffold = created;
            ApplyPopupGeometry();
            scaffold.Popup.OnOpen.AddListener(OnPopupOpened);
        }

        private static void ApplyPopupGeometry()
        {
            RectTransform titleRect = scaffold.TitleText != null ? scaffold.TitleText.rectTransform : null;
            if (titleRect != null)
            {
                scaffold.TitleText.fontSize = MonthlyLedgerConstants.PopupTitleFontSize;
                titleRect.sizeDelta = new Vector2(
                    scaffold.PanelRect.sizeDelta.x - MonthlyLedgerConstants.PopupTitleHorizontalPadding,
                    MonthlyLedgerConstants.PopupTitleHeight);
                titleRect.anchoredPosition = new Vector2(MonthlyLedgerConstants.ZeroSize, MonthlyLedgerConstants.PopupTitleTopOffset);
            }

            if (scaffold.CloseButton != null)
            {
                RectTransform closeRect = scaffold.CloseButton.GetComponent<RectTransform>();
                if (closeRect != null)
                {
                    closeRect.sizeDelta = new Vector2(MonthlyLedgerConstants.CloseButtonWidth, MonthlyLedgerConstants.CloseButtonHeight);
                    closeRect.anchoredPosition = new Vector2(MonthlyLedgerConstants.ZeroSize, MonthlyLedgerConstants.CloseButtonBottomOffset);
                }

                TextMeshProUGUI closeText = scaffold.CloseButton.gameObject.GetComponentInChildren<TextMeshProUGUI>(true);
                if (closeText != null)
                {
                    closeText.fontSize = MonthlyLedgerConstants.CloseButtonFontSize;
                }
            }

            if (scaffold.ScrollRect == null)
            {
                return;
            }

            RectTransform scrollRect = scaffold.ScrollRect.GetComponent<RectTransform>();
            if (scrollRect != null)
            {
                scrollRect.offsetMin = new Vector2(MonthlyLedgerConstants.PopupScrollLeftInset, MonthlyLedgerConstants.PopupScrollBottomInset);
                scrollRect.offsetMax = new Vector2(MonthlyLedgerConstants.PopupScrollRightInset, MonthlyLedgerConstants.PopupScrollTopInset);
            }

            RectTransform viewportRect = scaffold.ScrollRect.viewport;
            if (viewportRect != null)
            {
                viewportRect.offsetMin = Vector2.zero;
                viewportRect.offsetMax = new Vector2(MonthlyLedgerConstants.PopupViewportScrollbarGutter, MonthlyLedgerConstants.ZeroSize);
            }

            Scrollbar scrollbar = scaffold.ScrollRect.verticalScrollbar;
            RectTransform scrollbarRect = scrollbar != null ? scrollbar.GetComponent<RectTransform>() : null;
            if (scrollbarRect != null)
            {
                scrollbarRect.sizeDelta = new Vector2(MonthlyLedgerConstants.PopupScrollbarWidth, MonthlyLedgerConstants.ZeroSize);
                scrollbarRect.anchoredPosition = new Vector2(MonthlyLedgerConstants.PopupScrollbarRightOffset, MonthlyLedgerConstants.ZeroSize);
            }
        }

        internal static void Open()
        {
            if (scaffold == null || !scaffold.IsValid)
            {
                PopupManager manager = popupManager;
                if (manager == null && Camera.main != null && Camera.main.GetComponent<mainScript>() != null)
                {
                    mainScript main = Camera.main.GetComponent<mainScript>();
                    if (main.Data != null)
                    {
                        manager = main.Data.GetComponent<PopupManager>();
                    }
                }

                Initialize(manager);
            }

            if (scaffold == null || !scaffold.IsValid || !IMUiKit.TryOpenRegisteredPopup(PopupType))
            {
                Debug.LogError(MonthlyLedgerConstants.LogPrefix + MonthlyLedgerText.LoadFailed(MonthlyLedgerConstants.PopupObjectName));
            }
        }

        private static void OnPopupOpened()
        {
            scaffold.TitleText.text = MonthlyLedgerText.Title;
            if (!IsDataCoreCompatible())
            {
                RenderState(MonthlyLedgerText.DependencyError(MonthlyLedgerConstants.DataCoreDependencyDisplayName));
                return;
            }

            DateTime coverageStart;
            string errorMessage;
            if (!TryLoadCoverageStart(out coverageStart, out errorMessage))
            {
                RenderState(string.IsNullOrEmpty(errorMessage) ? MonthlyLedgerText.CoverageFailed : MonthlyLedgerText.LoadFailed(errorMessage));
                return;
            }

            DateTime coverageMonth = new DateTime(coverageStart.Year, coverageStart.Month, MonthlyLedgerConstants.FirstDayOfMonth);
            earliestMonth = coverageMonth.AddMonths(MonthlyLedgerConstants.FullMonthOffsetAfterCoverage);
            DateTime currentMonth = new DateTime(staticVars.dateTime.Year, staticVars.dateTime.Month, MonthlyLedgerConstants.FirstDayOfMonth);
            latestMonth = currentMonth.AddMonths(MonthlyLedgerConstants.PreviousMonthOffset);
            if (earliestMonth > latestMonth)
            {
                RenderState(MonthlyLedgerText.NoCompleteMonth);
                return;
            }

            selectedMonth = latestMonth;
            RenderSelectedMonth();
        }

        private static bool IsDataCoreCompatible()
        {
            if (!Harmony.HasAnyPatches(MonthlyLedgerConstants.DataCoreHarmonyId))
            {
                return false;
            }

            Version installedVersion = typeof(IMDataCoreApi).Assembly.GetName().Version;
            return installedVersion != null && installedVersion >= MonthlyLedgerConstants.MinimumDataCoreAssemblyVersion;
        }

        private static bool TryLoadCoverageStart(out DateTime coverageStart, out string errorMessage)
        {
            return IMDataCoreApi.TryGetMoneyLedgerCoverageStart(out coverageStart, out errorMessage);
        }

        private static void RenderSelectedMonth()
        {
            IMUiKit.ClearChildren(scaffold.ContentRoot);
            CreateNavigationRow();

            List<IMDataCoreMoneyTransaction> transactions;
            bool wasTruncated;
            string errorMessage;
            DateTime endExclusive = selectedMonth.AddMonths(MonthlyLedgerConstants.NextMonthOffset);
            if (!IMDataCoreApi.TryReadMoneyTransactions(
                selectedMonth,
                endExclusive,
                MonthlyLedgerConstants.MaximumTransactionCount,
                out transactions,
                out wasTruncated,
                out errorMessage))
            {
                CreateStateText(MonthlyLedgerText.LoadFailed(errorMessage));
                FinishRender();
                return;
            }

            long incomeTotal = MonthlyLedgerConstants.ZeroMoney;
            long expenseTotal = MonthlyLedgerConstants.ZeroMoney;
            for (int transactionIndex = MonthlyLedgerConstants.ZeroIndex; transactionIndex < transactions.Count; transactionIndex++)
            {
                long amount = transactions[transactionIndex].Amount;
                if (amount > MonthlyLedgerConstants.ZeroMoney)
                {
                    incomeTotal += amount;
                }
                else
                {
                    expenseTotal += amount;
                }
            }

            CreateSummaryRow(incomeTotal, expenseTotal, incomeTotal + expenseTotal);
            if (wasTruncated)
            {
                CreateWarningText(MonthlyLedgerText.Truncated);
            }

            if (transactions.Count == MonthlyLedgerConstants.ZeroIndex)
            {
                CreateStateText(MonthlyLedgerText.NoTransactions);
                FinishRender();
                return;
            }

            List<LedgerCategoryGroup> groups = BuildGroups(transactions);
            CreateSection(MonthlyLedgerText.Income, groups, true, MonthlyLedgerConstants.IncomeSectionObjectName);
            CreateSection(MonthlyLedgerText.Expenses, groups, false, MonthlyLedgerConstants.ExpenseSectionObjectName);
            FinishRender();
        }

        private static void RenderState(string message)
        {
            IMUiKit.ClearChildren(scaffold.ContentRoot);
            CreateStateText(message);
            FinishRender();
        }

        private static void CreateNavigationRow()
        {
            GameObject row = IMUiKit.CreateHorizontalLayoutContainer(
                scaffold.ContentRoot,
                MonthlyLedgerConstants.NavigationRowObjectName,
                MonthlyLedgerConstants.LayoutSpacing,
                MonthlyLedgerConstants.LayoutPadding,
                MonthlyLedgerConstants.LayoutPadding,
                MonthlyLedgerConstants.ZeroIndex,
                MonthlyLedgerConstants.ZeroIndex,
                false,
                false,
                false);
            SetPreferredSize(row, MonthlyLedgerConstants.UnconstrainedSize, MonthlyLedgerConstants.NavigationHeight);

            Button previous = IMUiKit.CreateStyledButton(
                row.transform,
                MonthlyLedgerConstants.PreviousButtonObjectName,
                MonthlyLedgerConstants.PreviousArrowText,
                MonthlyLedgerConstants.ArrowButtonWidth,
                MonthlyLedgerConstants.ArrowButtonHeight,
                ShowPreviousMonth);
            TextMeshProUGUI month = IMUiKit.CreateText(
                row.transform,
                MonthlyLedgerConstants.MonthLabelObjectName,
                ExtensionMethods.ToString_Loc(selectedMonth, MonthlyLedgerConstants.DateFormatMonth),
                MonthlyLedgerConstants.TitleFontSize,
                TextAlignmentOptions.Center,
                mainScript.black32);
            SetPreferredSize(month.gameObject, MonthlyLedgerConstants.MonthLabelWidth, MonthlyLedgerConstants.NavigationHeight);
            Button next = IMUiKit.CreateStyledButton(
                row.transform,
                MonthlyLedgerConstants.NextButtonObjectName,
                MonthlyLedgerConstants.NextArrowText,
                MonthlyLedgerConstants.ArrowButtonWidth,
                MonthlyLedgerConstants.ArrowButtonHeight,
                ShowNextMonth);
            SetButtonActive(previous, selectedMonth > earliestMonth);
            SetButtonActive(next, selectedMonth < latestMonth);
        }

        private static void CreateSummaryRow(long income, long expenses, long netRevenue)
        {
            GameObject row = IMUiKit.CreateHorizontalLayoutContainer(
                scaffold.ContentRoot,
                MonthlyLedgerConstants.SummaryRowObjectName,
                MonthlyLedgerConstants.LayoutSpacing,
                MonthlyLedgerConstants.ZeroIndex,
                MonthlyLedgerConstants.ZeroIndex,
                MonthlyLedgerConstants.ZeroIndex,
                MonthlyLedgerConstants.ZeroIndex,
                true,
                false,
                false);
            SetPreferredSize(row, MonthlyLedgerConstants.UnconstrainedSize, MonthlyLedgerConstants.SummaryHeight);
            CreateSummaryCard(row.transform, MonthlyLedgerText.Income, income, mainScript.green32, MonthlyLedgerConstants.KeyIncome);
            CreateSummaryCard(row.transform, MonthlyLedgerText.Expenses, expenses, mainScript.red32, MonthlyLedgerConstants.KeyExpenses);
            CreateSummaryCard(row.transform, MonthlyLedgerText.NetRevenue, netRevenue, netRevenue >= MonthlyLedgerConstants.ZeroMoney ? mainScript.green32 : mainScript.red32, MonthlyLedgerConstants.KeyNetRevenue);
        }

        private static void CreateSummaryCard(Transform parent, string label, long value, Color valueColor, string objectSuffix)
        {
            GameObject card = IMUiKit.CreateVerticalLayoutContainer(
                parent,
                MonthlyLedgerConstants.SummaryCardObjectPrefix + objectSuffix,
                MonthlyLedgerConstants.SmallLayoutSpacing,
                MonthlyLedgerConstants.LayoutPadding,
                MonthlyLedgerConstants.LayoutPadding,
                MonthlyLedgerConstants.LayoutPadding,
                MonthlyLedgerConstants.LayoutPadding,
                true,
                false,
                false);
            card.AddComponent<Image>().color = SummaryCardColor;
            SetPreferredSize(card, MonthlyLedgerConstants.SummaryCardWidth, MonthlyLedgerConstants.SummaryHeight);
            IMUiKit.CreateText(card.transform, MonthlyLedgerConstants.LabelObjectName, label, MonthlyLedgerConstants.SummaryLabelFontSize, TextAlignmentOptions.Center, mainScript.black32);
            IMUiKit.CreateText(card.transform, MonthlyLedgerConstants.ValueObjectName, FormatSignedMoney(value), MonthlyLedgerConstants.SummaryValueFontSize, TextAlignmentOptions.Center, valueColor);
        }

        private static List<LedgerCategoryGroup> BuildGroups(List<IMDataCoreMoneyTransaction> transactions)
        {
            Dictionary<string, LedgerCategoryGroup> byKey = new Dictionary<string, LedgerCategoryGroup>(StringComparer.Ordinal);
            for (int transactionIndex = MonthlyLedgerConstants.ZeroIndex; transactionIndex < transactions.Count; transactionIndex++)
            {
                IMDataCoreMoneyTransaction transaction = transactions[transactionIndex];
                bool isIncome = string.Equals(transaction.SectionCode, MonthlyLedgerConstants.SectionIncome, StringComparison.Ordinal)
                    || (!string.Equals(transaction.SectionCode, MonthlyLedgerConstants.SectionExpense, StringComparison.Ordinal)
                        && transaction.Amount > MonthlyLedgerConstants.ZeroMoney);
                string categoryCode = string.IsNullOrEmpty(transaction.CategoryCode) ? MonthlyLedgerConstants.CategoryOther : transaction.CategoryCode;
                string key = (isIncome ? MonthlyLedgerConstants.PositivePrefix : MonthlyLedgerConstants.NegativePrefix) + MonthlyLedgerConstants.CategorySignSeparator + categoryCode;
                LedgerCategoryGroup group;
                if (!byKey.TryGetValue(key, out group))
                {
                    group = new LedgerCategoryGroup { CategoryCode = categoryCode, IsIncome = isIncome };
                    byKey[key] = group;
                }

                group.Total += transaction.Amount;
                group.Records.Add(transaction);
            }

            List<LedgerCategoryGroup> groups = new List<LedgerCategoryGroup>(byKey.Values);
            groups.Sort(CompareGroups);
            return groups;
        }

        private static int CompareGroups(LedgerCategoryGroup left, LedgerCategoryGroup right)
        {
            int leftOrder = CategoryOrder(left.CategoryCode);
            int rightOrder = CategoryOrder(right.CategoryCode);
            int orderComparison = leftOrder.CompareTo(rightOrder);
            return orderComparison != MonthlyLedgerConstants.ZeroIndex ? orderComparison : string.Compare(left.CategoryCode, right.CategoryCode, StringComparison.Ordinal);
        }

        private static int CategoryOrder(string categoryCode)
        {
            for (int categoryIndex = MonthlyLedgerConstants.ZeroIndex; categoryIndex < MonthlyLedgerConstants.CategoryOrder.Length; categoryIndex++)
            {
                if (string.Equals(MonthlyLedgerConstants.CategoryOrder[categoryIndex], categoryCode, StringComparison.Ordinal))
                {
                    return categoryIndex;
                }
            }

            return MonthlyLedgerConstants.CategoryOrder.Length;
        }

        private static void CreateSection(string heading, List<LedgerCategoryGroup> groups, bool income, string objectName)
        {
            GameObject section = IMUiKit.CreateVerticalLayoutContainer(
                scaffold.ContentRoot,
                objectName,
                MonthlyLedgerConstants.SmallLayoutSpacing,
                MonthlyLedgerConstants.ZeroIndex,
                MonthlyLedgerConstants.ZeroIndex,
                MonthlyLedgerConstants.ZeroIndex,
                MonthlyLedgerConstants.ZeroIndex,
                true,
                false,
                true);
            TextMeshProUGUI headingText = IMUiKit.CreateText(section.transform, MonthlyLedgerConstants.LabelObjectName, heading, MonthlyLedgerConstants.SectionFontSize, TextAlignmentOptions.Left, income ? mainScript.green32 : mainScript.red32);
            SetPreferredSize(headingText.gameObject, MonthlyLedgerConstants.UnconstrainedSize, MonthlyLedgerConstants.SectionHeadingHeight);
            IMUiKit.CreateDivider(section.transform);

            for (int groupIndex = MonthlyLedgerConstants.ZeroIndex; groupIndex < groups.Count; groupIndex++)
            {
                LedgerCategoryGroup group = groups[groupIndex];
                if (group.IsIncome == income)
                {
                    CreateCategory(section.transform, group);
                }
            }
        }

        private static void CreateCategory(Transform parent, LedgerCategoryGroup group)
        {
            GameObject category = IMUiKit.CreateVerticalLayoutContainer(
                parent,
                MonthlyLedgerConstants.CategoryObjectPrefix + group.CategoryCode,
                MonthlyLedgerConstants.SmallLayoutSpacing,
                MonthlyLedgerConstants.ZeroIndex,
                MonthlyLedgerConstants.ZeroIndex,
                MonthlyLedgerConstants.ZeroIndex,
                MonthlyLedgerConstants.CategoryPadding,
                true,
                false,
                true);
            GameObject header = IMUiKit.CreateHorizontalLayoutContainer(
                category.transform,
                MonthlyLedgerConstants.CategoryHeaderObjectName,
                MonthlyLedgerConstants.LayoutSpacing,
                MonthlyLedgerConstants.CategoryPadding,
                MonthlyLedgerConstants.CategoryPadding,
                MonthlyLedgerConstants.ZeroIndex,
                MonthlyLedgerConstants.ZeroIndex,
                true,
                false,
                false);
            header.AddComponent<Image>().color = CategoryHeaderColor;
            SetPreferredSize(header, MonthlyLedgerConstants.UnconstrainedSize, MonthlyLedgerConstants.CategoryHeaderHeight);
            TextMeshProUGUI label = IMUiKit.CreateText(header.transform, MonthlyLedgerConstants.LabelObjectName, MonthlyLedgerText.Category(group.CategoryCode), MonthlyLedgerConstants.CategoryFontSize, TextAlignmentOptions.Left, mainScript.black32);
            SetFlexibleWidth(label.gameObject);
            TextMeshProUGUI total = IMUiKit.CreateText(header.transform, MonthlyLedgerConstants.ValueObjectName, FormatSignedMoney(group.Total), MonthlyLedgerConstants.CategoryFontSize, TextAlignmentOptions.Right, group.IsIncome ? mainScript.green32 : mainScript.red32);
            SetPreferredSize(total.gameObject, MonthlyLedgerConstants.RecordAmountWidth, MonthlyLedgerConstants.CategoryHeaderHeight);

            for (int recordIndex = MonthlyLedgerConstants.ZeroIndex; recordIndex < group.Records.Count; recordIndex++)
            {
                CreateRecord(category.transform, group.Records[recordIndex], group.IsIncome);
            }
        }

        private static void CreateRecord(Transform parent, IMDataCoreMoneyTransaction transaction, bool income)
        {
            GameObject row = IMUiKit.CreateHorizontalLayoutContainer(
                parent,
                MonthlyLedgerConstants.RecordRowObjectName,
                MonthlyLedgerConstants.LayoutSpacing,
                MonthlyLedgerConstants.RecordLeftPadding,
                MonthlyLedgerConstants.CategoryPadding,
                MonthlyLedgerConstants.ZeroIndex,
                MonthlyLedgerConstants.ZeroIndex,
                false,
                false,
                false);
            string detailText = MonthlyLedgerDetailText.Format(transaction);
            float rowHeight = MonthlyLedgerDetailText.HasStructuredDetails(transaction)
                ? MonthlyLedgerDetailText.CountLines(detailText) * MonthlyLedgerConstants.DetailedRecordLineHeight + MonthlyLedgerConstants.DetailedRecordVerticalPadding
                : MonthlyLedgerConstants.RecordHeight;
            SetPreferredSize(row, MonthlyLedgerConstants.UnconstrainedSize, rowHeight);
            DateTime gameDate;
            string dateText = DateTime.TryParse(transaction.GameDateTime, out gameDate)
                ? ExtensionMethods.ToString_Loc(gameDate, MonthlyLedgerConstants.DateFormatRecord)
                : transaction.GameDateTime;
            TextMeshProUGUI date = IMUiKit.CreateText(row.transform, MonthlyLedgerConstants.DateObjectName, dateText, MonthlyLedgerConstants.RecordFontSize, TextAlignmentOptions.TopLeft, mainScript.grey_light32);
            SetPreferredSize(date.gameObject, MonthlyLedgerConstants.RecordDateWidth, rowHeight);
            TextMeshProUGUI detail = IMUiKit.CreateText(row.transform, MonthlyLedgerConstants.DetailObjectName, detailText, MonthlyLedgerConstants.RecordFontSize, TextAlignmentOptions.TopLeft, mainScript.black32);
            detail.enableWordWrapping = true;
            float preferredDetailHeight = detail.GetPreferredValues(
                detailText,
                MonthlyLedgerConstants.RecordDetailWidth,
                MonthlyLedgerConstants.ZeroSize).y + MonthlyLedgerConstants.DetailedRecordVerticalPadding;
            rowHeight = Mathf.Max(rowHeight, preferredDetailHeight);
            SetPreferredSize(row, MonthlyLedgerConstants.UnconstrainedSize, rowHeight);
            SetPreferredSize(date.gameObject, MonthlyLedgerConstants.RecordDateWidth, rowHeight);
            SetPreferredSize(detail.gameObject, MonthlyLedgerConstants.RecordDetailWidth, rowHeight);
            TextMeshProUGUI amount = IMUiKit.CreateText(row.transform, MonthlyLedgerConstants.ValueObjectName, FormatSignedMoney(transaction.Amount), MonthlyLedgerConstants.RecordFontSize, TextAlignmentOptions.TopRight, income ? mainScript.green32 : mainScript.red32);
            SetPreferredSize(amount.gameObject, MonthlyLedgerConstants.RecordAmountWidth, rowHeight);
        }

        private static void CreateStateText(string message)
        {
            TextMeshProUGUI text = IMUiKit.CreateText(scaffold.ContentRoot, MonthlyLedgerConstants.EmptyStateObjectName, message, MonthlyLedgerConstants.StateFontSize, TextAlignmentOptions.Center, mainScript.grey_light32);
            text.enableWordWrapping = true;
        }

        private static void CreateWarningText(string message)
        {
            TextMeshProUGUI text = IMUiKit.CreateText(scaffold.ContentRoot, MonthlyLedgerConstants.WarningObjectName, message, MonthlyLedgerConstants.RecordFontSize, TextAlignmentOptions.Center, mainScript.red32);
            text.enableWordWrapping = true;
        }

        private static void FinishRender()
        {
            IMUiKit.RebuildLayout(scaffold.ContentRoot);
            if (scaffold.ScrollRect != null)
            {
                scaffold.ScrollRect.verticalNormalizedPosition = MonthlyLedgerConstants.ScrollTopPosition;
            }
        }

        private static void ShowPreviousMonth()
        {
            if (selectedMonth <= earliestMonth)
            {
                return;
            }

            selectedMonth = selectedMonth.AddMonths(MonthlyLedgerConstants.PreviousMonthOffset);
            RenderSelectedMonth();
        }

        private static void ShowNextMonth()
        {
            if (selectedMonth >= latestMonth)
            {
                return;
            }

            selectedMonth = selectedMonth.AddMonths(MonthlyLedgerConstants.NextMonthOffset);
            RenderSelectedMonth();
        }

        private static string FormatSignedMoney(long value)
        {
            string formatted = ExtensionMethods.formatMoney(value, false, false, false);
            return value > MonthlyLedgerConstants.ZeroMoney ? MonthlyLedgerConstants.PositivePrefix + formatted : formatted;
        }

        private static void SetButtonActive(Button button, bool active)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = active;
            ButtonDefault buttonDefault = button.GetComponent<ButtonDefault>();
            if (buttonDefault != null)
            {
                buttonDefault.Activate(active, false);
            }
        }

        private static void SetPreferredSize(GameObject obj, float width, float height)
        {
            LayoutElement layout = obj.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = obj.AddComponent<LayoutElement>();
            }

            if (width > MonthlyLedgerConstants.ZeroSize)
            {
                layout.preferredWidth = width;
            }

            if (height > MonthlyLedgerConstants.ZeroSize)
            {
                layout.preferredHeight = height;
            }
        }

        private static void SetFlexibleWidth(GameObject obj)
        {
            LayoutElement layout = obj.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = obj.AddComponent<LayoutElement>();
            }

            layout.flexibleWidth = MonthlyLedgerConstants.FlexibleWidth;
        }
    }
}
