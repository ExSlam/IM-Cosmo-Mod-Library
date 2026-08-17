extern alias UiFrameworkReference;

using System;
using System.Collections.Generic;
using HarmonyLib;
using IMDataCore;
using ModLocalizationSystem;
using Michsky.UI.ModernUIPack;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using IMUiKit = UiFrameworkReference::IMUiFramework.IMUiKit;
using PopupScaffold = UiFrameworkReference::IMUiFramework.PopupScaffold;
using VanillaMuipFontRole = UiFrameworkReference::IMUiFramework.VanillaMuipFontRole;
using VanillaUiFonts = UiFrameworkReference::IMUiFramework.VanillaUiFonts;
using VanillaUiPrefabCatalog = UiFrameworkReference::IMUiFramework.VanillaUiPrefabCatalog;
using VanillaUiResources = UiFrameworkReference::IMUiFramework.VanillaUiResources;
using VanillaUiThemeSettings = UiFrameworkReference::IMUiFramework.VanillaUiThemeSettings;

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
        internal const string SearchRowObjectName = "MonthlyLedger_Search";
        internal const string SearchInputObjectName = "MonthlyLedger_SearchInput";
        internal const string ScrollbarGutterObjectName = "MonthlyLedger_ScrollbarGutter";
        internal const string ResultsRootObjectName = "MonthlyLedger_Results";
        internal const string SummaryRowObjectName = "MonthlyLedger_Summary";
        internal const string IncomeSectionObjectName = "MonthlyLedger_Income";
        internal const string ExpenseSectionObjectName = "MonthlyLedger_Expenses";
        internal const string EmptyStateObjectName = "MonthlyLedger_Empty";
        internal const string WarningObjectName = "MonthlyLedger_Warning";
        internal const string CategoryObjectPrefix = "MonthlyLedger_Category_";
        internal const string CategoryHeaderObjectName = "CategoryHeader";
        internal const string RecordRowObjectName = "Record";
        internal const string CategoryRecordsObjectName = "Records";
        internal const string CategoryCollapseObjectName = "Collapse";
        internal const string CategoryCollapseOpenObjectName = "Open";
        internal const string CategoryCollapseClosedObjectName = "Closed";
        internal const string SummaryCardObjectPrefix = "SummaryCard_";
        internal const string LabelObjectName = "Label";
        internal const string ValueObjectName = "Value";
        internal const string DetailObjectName = "Detail";
        internal const string DateObjectName = "Date";
        internal const string PreviousArrowText = "<";
        internal const string NextArrowText = ">";
        internal const string FallbackExpandedGlyph = "▼";
        internal const string FallbackCollapsedGlyph = "▶";
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
        internal const string KeySearchPlaceholder = "ui.search_placeholder";
        internal const string KeyNoSearchResults = "state.no_search_results";
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
        internal const string FallbackSearchPlaceholder = "Search transactions...";
        internal const string FallbackNoSearchResults = "No transactions match this search.";
        internal const string FallbackNoCompleteMonth = "No fully recorded completed month is available yet.";
        internal const string FallbackNoTransactions = "No transactions were recorded for this month.";
        internal const string FallbackTruncated = "This month contains more records than can be displayed. Totals shown are incomplete.";
        internal const string FallbackLoadFailed = "Monthly ledger data could not be loaded: {0}";
        internal const string FallbackDependencyError = "Monthly Ledger requires an active, compatible installation of {0}.";
        internal const string FallbackCoverageFailed = "Exact monthly ledger coverage has not started for this save.";
        internal const string FallbackOther = "Other";
        internal const string DataCoreDependencyDisplayName = "IM Data Core 2.0.0+";
        internal const string MinimumDataCoreAssemblyVersionText = "2.0.0.0";

        internal const int PopupTypeValue = 997;
        internal const int FirstDayOfMonth = 1;
        internal const int FullMonthOffsetAfterCoverage = 1;
        internal const int PreviousMonthOffset = -1;
        internal const int NextMonthOffset = 1;
        internal const int MaximumTransactionCount = 10000;
        internal const int ZeroIndex = 0;
        internal const int TitleFontSize = 22;
        internal const int SummaryLabelFontSize = 16;
        internal const int SummaryValueFontSize = 21;
        internal const int SectionFontSize = 22;
        internal const int CategoryFontSize = 18;
        internal const int RecordFontSize = 15;
        internal const int StateFontSize = 20;
        internal const int PopupTitleFontSize = 27;
        internal const int CloseButtonFontSize = 18;
        internal const int ArrowButtonFontSize = 22;
        internal const int CollapseGlyphFontSize = 16;
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
        internal const float PopupViewportScrollbarGutter = -24f;
        internal const int ContentPaddingLeft = 24;
        internal const int ContentPaddingRight = 8;
        internal const float SearchRowHeight = 42f;
        internal const float SearchInputWidth = 520f;
        internal const float SearchInputHeight = 36f;
        internal const float PopupCornerRadius = 6f;
        internal const float SurfaceCornerRadius = 5f;
        internal const float CardCornerRadius = 5f;
        internal const float SearchCornerRadius = 4f;
        internal const float CategoryCornerRadius = 4f;
        internal const float VanillaScrollbarWidth = 20f;
        internal const float VanillaScrollbarRightOffset = -3f;
        internal const float VanillaScrollbarSpacing = 4f;
        internal const float ScrollbarGutterWidth = 16f;
        internal const float ScrollbarGutterRightOffset = -2f;
        internal const float ScrollbarGutterVerticalInset = 8f;
        internal const float CloseButtonWidth = 150f;
        internal const float CloseButtonHeight = 36f;
        internal const float CloseButtonBottomOffset = 10f;
        internal const float NavigationHeight = 40f;
        internal const float ArrowButtonWidth = 48f;
        internal const float ArrowButtonHeight = 32f;
        internal const float MonthLabelWidth = 300f;
        internal const float SummaryHeight = 80f;
        internal const float SummaryCardWidth = 245f;
        internal const float SectionHeadingHeight = 32f;
        internal const float CategoryHeaderHeight = 30f;
        internal const float CategoryCollapseWidth = 28f;
        internal const float RecordHeight = 27f;
        internal const float DetailedRecordLineHeight = 22f;
        internal const float DetailedRecordVerticalPadding = 8f;
        internal const float RecordDateWidth = 118f;
        internal const float RecordDetailWidth = 445f;
        internal const float RecordAmountWidth = 175f;
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
        internal static string SearchPlaceholder { get { return ModLocalization.Get(MonthlyLedgerConstants.KeySearchPlaceholder, MonthlyLedgerConstants.FallbackSearchPlaceholder); } }
        internal static string NoSearchResults { get { return ModLocalization.Get(MonthlyLedgerConstants.KeyNoSearchResults, MonthlyLedgerConstants.FallbackNoSearchResults); } }
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
        private static readonly HashSet<string> CollapsedCategoryKeys = new HashSet<string>(StringComparer.Ordinal);
        private static GameObject vanillaGroupCollapseTemplate;
        private static List<IMDataCoreMoneyTransaction> currentTransactions = new List<IMDataCoreMoneyTransaction>();
        private static bool currentWasTruncated;
        private static string searchQuery = string.Empty;
        private static Transform resultsRoot;

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
                    // Keep the framework/game button's normal 36px height instead of the old
                    // ledger-specific 32px squeeze. Width is normalized to the vanilla scaffold.
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
                ApplyGameFont(scaffold.Root);
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

            VerticalLayoutGroup contentLayout = scaffold.ContentRoot != null ? scaffold.ContentRoot.GetComponent<VerticalLayoutGroup>() : null;
            if (contentLayout != null)
            {
                contentLayout.padding = new RectOffset(
                    MonthlyLedgerConstants.ContentPaddingLeft,
                    MonthlyLedgerConstants.ContentPaddingRight,
                    MonthlyLedgerConstants.LayoutPadding,
                    MonthlyLedgerConstants.LayoutPadding);
            }

            ApplyRoundedPopupChrome();
            EnsureVanillaScrollbar();
            ApplyGameFont(scaffold.Root);
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

            currentTransactions = transactions ?? new List<IMDataCoreMoneyTransaction>();
            currentWasTruncated = wasTruncated;
            CreateSearchBar();

            long incomeTotal = MonthlyLedgerConstants.ZeroMoney;
            long expenseTotal = MonthlyLedgerConstants.ZeroMoney;
            for (int transactionIndex = MonthlyLedgerConstants.ZeroIndex; transactionIndex < currentTransactions.Count; transactionIndex++)
            {
                long amount = currentTransactions[transactionIndex].Amount;
                if (amount > MonthlyLedgerConstants.ZeroMoney) incomeTotal += amount;
                else expenseTotal += amount;
            }
            CreateSummaryRow(incomeTotal, expenseTotal, incomeTotal + expenseTotal);

            GameObject body = IMUiKit.CreateVerticalLayoutContainer(
                scaffold.ContentRoot, MonthlyLedgerConstants.ResultsRootObjectName, MonthlyLedgerConstants.SmallLayoutSpacing,
                0, 0, 0, 0, true, false, true);
            resultsRoot = body.transform;
            RenderFilteredResults();
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
            HorizontalLayoutGroup navigationLayout = row.GetComponent<HorizontalLayoutGroup>();
            if (navigationLayout != null)
            {
                navigationLayout.childAlignment = TextAnchor.MiddleCenter;
                navigationLayout.spacing = 12f;
            }

            Button previous = CreateVanillaMonthButton(
                row.transform,
                MonthlyLedgerConstants.PreviousButtonObjectName,
                MonthlyLedgerConstants.PreviousArrowText,
                ShowPreviousMonth);
            TextMeshProUGUI month = IMUiKit.CreateText(
                row.transform,
                MonthlyLedgerConstants.MonthLabelObjectName,
                ExtensionMethods.ToString_Loc(selectedMonth, MonthlyLedgerConstants.DateFormatMonth),
                MonthlyLedgerConstants.TitleFontSize,
                TextAlignmentOptions.Center,
                mainScript.black32);
            SetPreferredSize(month.gameObject, MonthlyLedgerConstants.MonthLabelWidth, MonthlyLedgerConstants.NavigationHeight);
            Button next = CreateVanillaMonthButton(
                row.transform,
                MonthlyLedgerConstants.NextButtonObjectName,
                MonthlyLedgerConstants.NextArrowText,
                ShowNextMonth);
            SetButtonActive(previous, selectedMonth > earliestMonth);
            SetButtonActive(next, selectedMonth < latestMonth);
        }

        private static void CreateSearchBar()
        {
            GameObject row = IMUiKit.CreateHorizontalLayoutContainer(
                scaffold.ContentRoot, MonthlyLedgerConstants.SearchRowObjectName, 0f, 0, 0, 0, 0, false, false, false);
            SetPreferredSize(row, MonthlyLedgerConstants.UnconstrainedSize, MonthlyLedgerConstants.SearchRowHeight);
            HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            if (rowLayout != null) rowLayout.childAlignment = TextAnchor.MiddleCenter;

            GameObject instance;
            CustomInputField manager;
            if (VanillaUiResources.TryCreateInputField(
                row.transform, MonthlyLedgerConstants.SearchInputObjectName, out instance, out manager,
                VanillaUiPrefabCatalog.InputField.Input_Field_Standard_Middle,
                null,
                delegate(VanillaUiThemeSettings theme)
                {
                    TMP_FontAsset gameFont = ResolveGameTmpFont();
                    if (gameFont != null) theme.inputFieldFont = gameFont;
                    theme.inputFieldFontSize = MonthlyLedgerConstants.RecordFontSize;
                    theme.inputFieldColor = mainScript.black32;
                }))
            {
                RectTransform rect = instance.GetComponent<RectTransform>();
                if (rect != null) rect.sizeDelta = new Vector2(MonthlyLedgerConstants.SearchInputWidth, MonthlyLedgerConstants.SearchInputHeight);
                SetPreferredSize(instance, MonthlyLedgerConstants.SearchInputWidth, MonthlyLedgerConstants.SearchInputHeight);
                ApplyRoundedImage(instance, SummaryCardColor, MonthlyLedgerConstants.SearchCornerRadius).raycastTarget = true;

                TMP_InputField input = instance.GetComponent<TMP_InputField>();
                if (input == null) input = instance.GetComponentInChildren<TMP_InputField>(true);
                if (input != null)
                {
                    input.text = searchQuery;
                    input.caretColor = mainScript.blue32;
                    Color selection = mainScript.blue32;
                    selection.a = 0.28f;
                    input.selectionColor = selection;
                    TMP_Text text = input.textComponent;
                    if (text != null)
                    {
                        text.color = mainScript.black32;
                        text.fontSize = MonthlyLedgerConstants.RecordFontSize;
                        TMP_FontAsset font = ResolveGameTmpFont();
                        if (font != null) text.font = font;
                    }
                    Transform placeholderTransform = instance.transform.Find("Placeholder");
                    TMP_Text placeholder = placeholderTransform != null ? placeholderTransform.GetComponent<TMP_Text>() : null;
                    if (placeholder != null)
                    {
                        placeholder.text = MonthlyLedgerText.SearchPlaceholder;
                        placeholder.color = mainScript.grey_light32;
                        placeholder.fontSize = MonthlyLedgerConstants.RecordFontSize;
                        TMP_FontAsset font = ResolveGameTmpFont();
                        if (font != null) placeholder.font = font;
                    }
                    input.onValueChanged = new TMP_InputField.OnChangeEvent();
                    input.onValueChanged.AddListener(OnSearchChanged);
                }
            }
        }

        private static void OnSearchChanged(string value)
        {
            searchQuery = (value ?? string.Empty).Trim();
            RenderFilteredResults();
        }

        private static void RenderFilteredResults()
        {
            if (resultsRoot == null) return;
            IMUiKit.ClearChildren(resultsRoot);
            if (currentWasTruncated) CreateWarningText(resultsRoot, MonthlyLedgerText.Truncated);
            if (currentTransactions == null || currentTransactions.Count == 0)
            {
                CreateStateText(resultsRoot, MonthlyLedgerText.NoTransactions);
                ApplyGameFont(resultsRoot.gameObject);
                IMUiKit.RebuildLayout(scaffold.ContentRoot);
                return;
            }

            List<IMDataCoreMoneyTransaction> filtered = new List<IMDataCoreMoneyTransaction>();
            for (int i = 0; i < currentTransactions.Count; i++)
            {
                if (TransactionMatchesSearch(currentTransactions[i], searchQuery)) filtered.Add(currentTransactions[i]);
            }
            if (filtered.Count == 0)
            {
                CreateStateText(resultsRoot, MonthlyLedgerText.NoSearchResults);
                ApplyGameFont(resultsRoot.gameObject);
                IMUiKit.RebuildLayout(scaffold.ContentRoot);
                return;
            }

            List<LedgerCategoryGroup> groups = BuildGroups(filtered);
            CreateSection(resultsRoot, MonthlyLedgerText.Income, groups, true, MonthlyLedgerConstants.IncomeSectionObjectName);
            CreateSection(resultsRoot, MonthlyLedgerText.Expenses, groups, false, MonthlyLedgerConstants.ExpenseSectionObjectName);
            ApplyGameFont(resultsRoot.gameObject);
            IMUiKit.RebuildLayout(scaffold.ContentRoot);
        }

        private static bool TransactionMatchesSearch(IMDataCoreMoneyTransaction transaction, string query)
        {
            if (string.IsNullOrEmpty(query)) return true;
            string category = MonthlyLedgerText.Category(transaction.CategoryCode ?? string.Empty);
            string detail = MonthlyLedgerDetailText.Format(transaction);
            string amount = FormatSignedMoney(transaction.Amount);
            string date = transaction.GameDateTime ?? string.Empty;
            DateTime parsed;
            if (DateTime.TryParse(transaction.GameDateTime, out parsed)) date += " " + ExtensionMethods.ToString_Loc(parsed, MonthlyLedgerConstants.DateFormatRecord);
            return ContainsIgnoreCase(category, query) || ContainsIgnoreCase(detail, query) || ContainsIgnoreCase(amount, query)
                || ContainsIgnoreCase(date, query) || ContainsIgnoreCase(transaction.CategoryCode, query) || ContainsIgnoreCase(transaction.DetailCode, query);
        }

        private static bool ContainsIgnoreCase(string value, string query)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
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
            ApplyRoundedImage(card, SummaryCardColor, MonthlyLedgerConstants.CardCornerRadius);
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

        private static void CreateSection(Transform parent, string heading, List<LedgerCategoryGroup> groups, bool income, string objectName)
        {
            GameObject section = IMUiKit.CreateVerticalLayoutContainer(
                parent,
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
            string stateKey = GetCategoryStateKey(group);
            bool expanded = !CollapsedCategoryKeys.Contains(stateKey);

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
                false,
                false,
                false);
            Image headerImage = ApplyRoundedImage(header, CategoryHeaderColor, MonthlyLedgerConstants.CategoryCornerRadius);
            SetPreferredSize(header, MonthlyLedgerConstants.UnconstrainedSize, MonthlyLedgerConstants.CategoryHeaderHeight);

            GameObject collapseIndicator = CreateVanillaCollapseIndicator(header.transform, expanded);
            TextMeshProUGUI label = IMUiKit.CreateText(header.transform, MonthlyLedgerConstants.LabelObjectName, MonthlyLedgerText.Category(group.CategoryCode), MonthlyLedgerConstants.CategoryFontSize, TextAlignmentOptions.Left, mainScript.black32);
            SetFlexibleWidth(label.gameObject);
            TextMeshProUGUI total = IMUiKit.CreateText(header.transform, MonthlyLedgerConstants.ValueObjectName, FormatSignedMoney(group.Total), MonthlyLedgerConstants.CategoryFontSize, TextAlignmentOptions.Right, group.IsIncome ? mainScript.green32 : mainScript.red32);
            SetPreferredSize(total.gameObject, MonthlyLedgerConstants.RecordAmountWidth, MonthlyLedgerConstants.CategoryHeaderHeight);

            GameObject records = IMUiKit.CreateVerticalLayoutContainer(
                category.transform,
                MonthlyLedgerConstants.CategoryRecordsObjectName,
                MonthlyLedgerConstants.SmallLayoutSpacing,
                MonthlyLedgerConstants.ZeroIndex,
                MonthlyLedgerConstants.ZeroIndex,
                MonthlyLedgerConstants.ZeroIndex,
                MonthlyLedgerConstants.ZeroIndex,
                true,
                false,
                true);
            for (int recordIndex = MonthlyLedgerConstants.ZeroIndex; recordIndex < group.Records.Count; recordIndex++)
            {
                CreateRecord(records.transform, group.Records[recordIndex], group.IsIncome);
            }
            records.SetActive(expanded);

            // The vanilla idol list stores a Showing flag on the group and rebuilds the list
            // when Group_Title.OnCollapse toggles it. Monthly Ledger mirrors that model with a
            // per-category state key while keeping the category total visible in the header.
            Button headerButton = header.AddComponent<Button>();
            headerButton.targetGraphic = headerImage;
            headerButton.onClick.AddListener(delegate
            {
                ToggleCategory(stateKey, records, collapseIndicator);
            });
        }

        private static string GetCategoryStateKey(LedgerCategoryGroup group)
        {
            return (group.IsIncome ? MonthlyLedgerConstants.SectionIncome : MonthlyLedgerConstants.SectionExpense)
                + MonthlyLedgerConstants.CategorySignSeparator
                + group.CategoryCode;
        }

        private static void ToggleCategory(string stateKey, GameObject records, GameObject collapseIndicator)
        {
            if (records == null)
            {
                return;
            }

            bool expanded = !records.activeSelf;
            records.SetActive(expanded);
            if (expanded)
            {
                CollapsedCategoryKeys.Remove(stateKey);
            }
            else
            {
                CollapsedCategoryKeys.Add(stateKey);
            }

            UpdateCollapseIndicator(collapseIndicator, expanded);
            if (scaffold != null && scaffold.ContentRoot != null)
            {
                IMUiKit.RebuildLayout(scaffold.ContentRoot);
            }
        }

        private static GameObject CreateVanillaCollapseIndicator(Transform parent, bool expanded)
        {
            GameObject template = GetVanillaGroupCollapseTemplate();
            if (template != null)
            {
                GameObject clone = UnityEngine.Object.Instantiate(template, parent, false);
                clone.name = MonthlyLedgerConstants.CategoryCollapseObjectName;
                IMUiKit.ApplyLayerRecursively(clone, parent.gameObject.layer);

                // The source control has a persistent Group_Title.OnCollapse callback. This
                // clone is visual-only; the category header owns the click behavior.
                Button sourceButton = clone.GetComponent<Button>();
                if (sourceButton != null)
                {
                    sourceButton.onClick = new Button.ButtonClickedEvent();
                    sourceButton.enabled = false;
                }
                ButtonDefault sourceDefault = clone.GetComponent<ButtonDefault>();
                if (sourceDefault != null)
                {
                    sourceDefault.enabled = false;
                }
                Graphic[] graphics = clone.GetComponentsInChildren<Graphic>(true);
                for (int graphicIndex = MonthlyLedgerConstants.ZeroIndex; graphicIndex < graphics.Length; graphicIndex++)
                {
                    if (graphics[graphicIndex] != null)
                    {
                        graphics[graphicIndex].raycastTarget = false;
                    }
                }

                SetPreferredSize(clone, MonthlyLedgerConstants.CategoryCollapseWidth, MonthlyLedgerConstants.CategoryHeaderHeight);
                UpdateCollapseIndicator(clone, expanded);
                return clone;
            }

            TextMeshProUGUI fallback = IMUiKit.CreateText(
                parent,
                MonthlyLedgerConstants.CategoryCollapseObjectName,
                expanded ? MonthlyLedgerConstants.FallbackExpandedGlyph : MonthlyLedgerConstants.FallbackCollapsedGlyph,
                MonthlyLedgerConstants.CollapseGlyphFontSize,
                TextAlignmentOptions.Center,
                mainScript.grey_light32);
            SetPreferredSize(fallback.gameObject, MonthlyLedgerConstants.CategoryCollapseWidth, MonthlyLedgerConstants.CategoryHeaderHeight);
            return fallback.gameObject;
        }

        private static GameObject GetVanillaGroupCollapseTemplate()
        {
            if (vanillaGroupCollapseTemplate != null)
            {
                return vanillaGroupCollapseTemplate;
            }

            if (Camera.main == null)
            {
                return null;
            }
            mainScript main = Camera.main.GetComponent<mainScript>();
            if (main == null || main.Data == null)
            {
                return null;
            }
            data_girls girls = main.Data.GetComponent<data_girls>();
            if (girls == null || girls.prefab_group == null)
            {
                return null;
            }

            Group_Title groupTitle = girls.prefab_group.GetComponent<Group_Title>();
            if (groupTitle == null)
            {
                groupTitle = girls.prefab_group.GetComponentInChildren<Group_Title>(true);
            }
            if (groupTitle == null)
            {
                return null;
            }

            GameObject open = groupTitle.Collapse_Opened;
            GameObject closed = groupTitle.Collapse_Closed;
            Transform collapse = open != null && open.transform.parent != null
                ? open.transform.parent
                : (closed != null ? closed.transform.parent : null);
            if (collapse != null)
            {
                vanillaGroupCollapseTemplate = collapse.gameObject;
            }
            return vanillaGroupCollapseTemplate;
        }

        private static void UpdateCollapseIndicator(GameObject indicator, bool expanded)
        {
            if (indicator == null)
            {
                return;
            }

            Transform open = indicator.transform.Find(MonthlyLedgerConstants.CategoryCollapseOpenObjectName);
            if (open == null) open = indicator.transform.Find("Opened");
            Transform closed = indicator.transform.Find(MonthlyLedgerConstants.CategoryCollapseClosedObjectName);
            if (open != null || closed != null)
            {
                if (open != null)
                {
                    open.gameObject.SetActive(expanded);
                }
                if (closed != null)
                {
                    closed.gameObject.SetActive(!expanded);
                }
                return;
            }

            TextMeshProUGUI fallback = indicator.GetComponent<TextMeshProUGUI>();
            if (fallback != null)
            {
                fallback.text = expanded ? MonthlyLedgerConstants.FallbackExpandedGlyph : MonthlyLedgerConstants.FallbackCollapsedGlyph;
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

        private static void CreateStateText(string message) { CreateStateText(scaffold.ContentRoot, message); }
        private static void CreateStateText(Transform parent, string message)
        {
            TextMeshProUGUI text = IMUiKit.CreateText(parent, MonthlyLedgerConstants.EmptyStateObjectName, message, MonthlyLedgerConstants.StateFontSize, TextAlignmentOptions.Center, mainScript.grey_light32, ResolveGameTmpFont());
            text.enableWordWrapping = true;
        }

        private static void CreateWarningText(string message) { CreateWarningText(scaffold.ContentRoot, message); }
        private static void CreateWarningText(Transform parent, string message)
        {
            TextMeshProUGUI text = IMUiKit.CreateText(parent, MonthlyLedgerConstants.WarningObjectName, message, MonthlyLedgerConstants.RecordFontSize, TextAlignmentOptions.Center, mainScript.red32, ResolveGameTmpFont());
            text.enableWordWrapping = true;
        }

        private static void FinishRender()
        {
            ApplyGameFont(scaffold.Root);
            IMUiKit.RebuildLayout(scaffold.ContentRoot);
            if (scaffold.ScrollRect != null)
            {
                scaffold.ScrollRect.verticalNormalizedPosition = MonthlyLedgerConstants.ScrollTopPosition;
            }
        }

        private static Button CreateVanillaMonthButton(Transform parent, string objectName, string label, UnityAction onClick)
        {
            GameObject instance;
            ButtonManagerBasic manager;
            if (VanillaUiResources.TryCreateBasicButton(
                parent,
                objectName,
                out instance,
                out manager,
                VanillaUiPrefabCatalog.Button.basic_Pink,
                delegate(ButtonManagerBasic control)
                {
                    control.buttonText = label;
                    control.useCustomContent = false;
                    control.UpdateUI();
                }))
            {
                RectTransform rect = instance.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(MonthlyLedgerConstants.ArrowButtonWidth, MonthlyLedgerConstants.ArrowButtonHeight);
                }
                SetPreferredSize(instance, MonthlyLedgerConstants.ArrowButtonWidth, MonthlyLedgerConstants.ArrowButtonHeight);

                TextMeshProUGUI buttonText = instance.GetComponentInChildren<TextMeshProUGUI>(true);
                if (buttonText != null)
                {
                    buttonText.text = label;
                    buttonText.fontSize = MonthlyLedgerConstants.ArrowButtonFontSize;
                    buttonText.alignment = TextAlignmentOptions.Center;
                    TMP_FontAsset gameFont = ResolveGameTmpFont();
                    if (gameFont != null)
                    {
                        VanillaUiFonts.Apply(buttonText, gameFont);
                    }
                }

                Button button = instance.GetComponent<Button>();
                if (button == null)
                {
                    button = instance.GetComponentInChildren<Button>(true);
                }
                if (button != null)
                {
                    button.onClick = new Button.ButtonClickedEvent();
                    if (onClick != null)
                    {
                        button.onClick.AddListener(onClick);
                    }
                    return button;
                }

                UnityEngine.Object.Destroy(instance);
            }

            // Resource failure is not fatal; retain the framework's old safe fallback.
            return IMUiKit.CreateStyledButton(
                parent,
                objectName,
                label,
                MonthlyLedgerConstants.ArrowButtonWidth,
                MonthlyLedgerConstants.ArrowButtonHeight,
                onClick);
        }

        private static void EnsureVanillaScrollbar()
        {
            if (scaffold == null || scaffold.ScrollRect == null) return;

            IMUiKit.ApplyVanillaScrollDefaults(scaffold.ScrollRect);
            Scrollbar scrollbar = scaffold.ScrollRect.verticalScrollbar;
            bool hasVanillaHierarchy = scrollbar != null
                && scrollbar.transform.Find("Background") != null
                && scrollbar.transform.Find("Sliding Area/Handle") != null;

            if (!hasVanillaHierarchy)
            {
                GameObject scrollbarObject;
                Scrollbar vanillaScrollbar;
                if (VanillaUiResources.TryCreateScrollbar(
                    scaffold.ScrollRect.transform,
                    "Scrollbar",
                    out scrollbarObject,
                    out vanillaScrollbar,
                    null,
                    delegate(VanillaUiThemeSettings theme)
                    {
                        theme.scrollbarColor = mainScript.blue32;
                        Color track = mainScript.grey_light32;
                        track.a = 0.22f;
                        theme.scrollbarBackgroundColor = track;
                    }))
                {
                    if (scrollbar != null && scrollbar.gameObject != scrollbarObject)
                    {
                        UnityEngine.Object.Destroy(scrollbar.gameObject);
                    }
                    scrollbar = vanillaScrollbar;
                    scaffold.ScrollRect.verticalScrollbar = vanillaScrollbar;
                }
            }

            EnsureScrollbarGutter();
            scaffold.ScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scaffold.ScrollRect.verticalScrollbarSpacing = MonthlyLedgerConstants.VanillaScrollbarSpacing;

            RectTransform scrollbarRect = scrollbar != null ? scrollbar.GetComponent<RectTransform>() : null;
            if (scrollbarRect != null)
            {
                scrollbarRect.anchorMin = new Vector2(1f, 0f);
                scrollbarRect.anchorMax = new Vector2(1f, 1f);
                scrollbarRect.pivot = new Vector2(1f, 1f);
                scrollbarRect.sizeDelta = new Vector2(MonthlyLedgerConstants.VanillaScrollbarWidth, 0f);
                scrollbarRect.anchoredPosition = new Vector2(MonthlyLedgerConstants.VanillaScrollbarRightOffset, 0f);
                scrollbar.direction = Scrollbar.Direction.BottomToTop;
                scrollbarRect.SetAsLastSibling();
            }

            if (scrollbar != null)
            {
                UIManagerScrollbar manager = scrollbar.GetComponent<UIManagerScrollbar>();
                Image handleImage = null;
                Image backgroundImage = null;
                if (manager != null)
                {
                    handleImage = manager.bar;
                    backgroundImage = manager.background;
                    manager.enabled = false;
                }
                if (handleImage == null)
                {
                    Transform handle = scrollbar.transform.Find("Sliding Area/Handle");
                    handleImage = handle != null ? handle.GetComponent<Image>() : null;
                }
                if (backgroundImage == null)
                {
                    Transform background = scrollbar.transform.Find("Background");
                    backgroundImage = background != null ? background.GetComponent<Image>() : null;
                }

                if (handleImage != null) handleImage.color = mainScript.blue32;
                if (backgroundImage != null)
                {
                    Color track = mainScript.grey_light32;
                    track.a = 0.24f;
                    backgroundImage.color = track;
                }
            }
        }

        private static void EnsureScrollbarGutter()
        {
            Transform root = scaffold.ScrollRect.transform;
            Transform existing = root.Find(MonthlyLedgerConstants.ScrollbarGutterObjectName);
            GameObject gutter = existing != null ? existing.gameObject : IMUiKit.CreateUiObject(MonthlyLedgerConstants.ScrollbarGutterObjectName, root);
            Image image = gutter.GetComponent<Image>();
            if (image == null) image = gutter.AddComponent<Image>();
            Color gutterColor = mainScript.black32;
            gutterColor.a = 0.055f;
            image.color = gutterColor;
            image.raycastTarget = false;

            RectTransform rect = gutter.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(
                MonthlyLedgerConstants.ScrollbarGutterWidth,
                -(MonthlyLedgerConstants.ScrollbarGutterVerticalInset * 2f));
            rect.anchoredPosition = new Vector2(MonthlyLedgerConstants.ScrollbarGutterRightOffset, 0f);
            rect.SetAsLastSibling();
        }

        private static void ApplyRoundedPopupChrome()
        {
            if (scaffold == null) return;
            if (scaffold.PanelRect != null)
            {
                Image panelImage = scaffold.PanelRect.GetComponent<Image>();
                Color panelColor = panelImage != null ? panelImage.color : new Color32(232, 230, 230, 255);
                ApplyRoundedImage(scaffold.PanelRect.gameObject, panelColor, MonthlyLedgerConstants.PopupCornerRadius);
            }
            if (scaffold.ScrollRect != null)
            {
                Image scrollImage = scaffold.ScrollRect.GetComponent<Image>();
                Color innerColor = scrollImage != null ? scrollImage.color : Color.white;
                ApplyRoundedImage(scaffold.ScrollRect.gameObject, innerColor, MonthlyLedgerConstants.SurfaceCornerRadius);
                if (scaffold.ScrollRect.viewport != null)
                {
                    Image viewportImage = scaffold.ScrollRect.viewport.GetComponent<Image>();
                    if (viewportImage != null)
                    {
                        ApplyRoundedImage(viewportImage.gameObject, viewportImage.color, MonthlyLedgerConstants.SurfaceCornerRadius);
                    }
                }
            }
        }

        private static Image ApplyRoundedImage(GameObject target, Color color, float radius)
        {
            Image image = target.GetComponent<Image>();
            if (image == null) image = target.AddComponent<Image>();
            image.color = color;

            if (!IMUiKit.TryApplyVanillaRoundedCorners(image, radius))
            {
                // The rounded-corner shader ships with Idol Manager. If it is unexpectedly unavailable,
                // prefer a clean rectangle over the oversized rounded-button sprite used in 0.2.1/0.2.2.
                image.sprite = null;
                image.material = null;
                image.type = Image.Type.Simple;
            }
            return image;
        }

        private static void ApplyGameFont(GameObject root)
        {
            if (root == null) return;
            VanillaUiFonts.ApplyGameFont(root, true);
        }

        private static TMP_FontAsset ResolveGameTmpFont()
        {
            return VanillaUiFonts.GetGameSelectedTmpFont();
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
