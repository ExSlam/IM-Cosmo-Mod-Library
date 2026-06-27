using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using ModLocalizationSystem;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IdolCareerDiary
{
    /// <summary>
    /// Central constants for Idol Career Diary.
    /// </summary>
    internal static class C
    {
        internal const string HarmonyId = "com.cosmo.idolcareerdiary";
        internal const string HarmonyIdImDataCore = "com.cosmo.imdatacore";
        internal const string HarmonyIdImUiFramework = "com.cosmo.imuiframework";
        internal const string HarmonyIdGraduationRebalances = "com.cosmo.graduationrebalances";
        internal const string HarmonyIdUiRecovery = "com.cosmo.uirecovery";
        internal const string HarmonyIdGraduationCalendar = "com.cosmo.graduationcalendar";
        internal const string HarmonyIdGraduationDetails = "com.cosmo.graduationdetails";
        internal const string HarmonyIdDivorceFix = "com.cosmo.divorcefix";
        internal const string HarmonyIdModMenus = "com.tel.modmenus";
        internal const string LogPrefix = "[IdolCareerDiary] ";

        internal const string CoreNamespace = "com.cosmo.idol_career_diary";
        internal const string CoreSelectedEventKeyPrefix = "profile_last_selected_event_";
        internal const string CoreSelectedEventJsonField = "event_id";
        internal const int CoreRegistrationRetrySeconds = 5;
        internal const int DependencyLogRetrySeconds = 15;
        internal const string ConfigFileName = "IdolCareerDiary.config.ini";
        internal const string ConfigKeyShowUnknownSocialParticipants = "show_unknown_social_participants";
        internal const string ModMenuVarShowUnknownSocialParticipants = "IdolCareerDiary_ShowUnknownSocialParticipants";
        internal const bool DefaultShowUnknownSocialParticipants = false;
        internal const char ConfigKeyValueSeparator = '=';
        internal const string ConfigCommentPrefixHash = "#";
        internal const string ConfigCommentPrefixSemicolon = ";";
        internal const string ConfigCommentPrefixSlashSlash = "//";
        internal const string CustomEntriesFolderName = "Idol Career Diary";
        internal const string CustomEntriesFolderNameCompact = "IdolCareerDiary";
        internal const string CustomEntriesArrayField = "entries";
        internal const string CustomEntryEventTypeField = "event_type";
        internal const string CustomEntryEventTypesField = "event_types";
        internal const string CustomEntryEntityKindField = "entity_kind";
        internal const string CustomEntryEntityIdField = "entity_id";
        internal const string CustomEntryEntityIdsField = "entity_ids";
        internal const string CustomEntrySubstoryIdField = "substory_id";
        internal const string CustomEntrySubstoryIdsField = "substory_ids";
        internal const string CustomEntrySubstoryIdPrefixField = "substory_id_prefix";
        internal const string CustomEntrySubstoryIdPrefixesField = "substory_id_prefixes";
        internal const string CustomEntryTitleField = "title";
        internal const string CustomEntryWithWhomField = "with_whom";
        internal const string CustomEntryDescriptionField = "description";
        internal const string CustomEntryDetailsField = "details";
        internal const string CustomEntryOutcomeLinesField = "outcome_lines";
        internal const string CustomTokenIdols = "{idols}";
        internal const string CustomTokenIdol = "{idol}";
        internal const string CustomTokenFocusedIdol = "{focused_idol}";
        internal const string CustomTokenStory = "{story}";
        internal const string CustomTokenSubstory = "{substory}";
        internal const string CustomTokenParentStory = "{parent_story}";
        internal const string CustomTokenAction = "{action}";
        internal const string InfoJsonFileName = "info.json";
        internal const string InfoTitleField = "Title";
        internal const string InfoHarmonyIdField = "HarmonyID";
        internal const string InternalEventLedgerNamespace = "data";
        internal static readonly string LabelModSourcePrefix = ModLocalization.Get("LabelModSourcePrefix", "From mod:");
        internal static readonly string ConfigCommentHeader = ModLocalization.Get("ConfigCommentHeader", "# IdolCareerDiary configuration");
        internal static readonly string ConfigCommentShowUnknownSocialParticipants = ModLocalization.Get("ConfigCommentShowUnknownSocialParticipants", "# Show social-event participants even when producer does not know them.");
        internal static readonly string[] DefaultConfigTemplateLines = new[]
        {
            ConfigCommentHeader,
            ConfigCommentShowUnknownSocialParticipants,
            "show_unknown_social_participants=false"
        };
        internal static readonly string MessageCoreDependencyDetected = ModLocalization.Get("MessageCoreDependencyDetected", "Detected required dependency: IMDataCore.");
        internal static readonly string MessageUiFrameworkDependencyDetected = ModLocalization.Get("MessageUiFrameworkDependencyDetected", "Detected required dependency: IMUIFramework.");
        internal static readonly string MessageCoreDependencyMissingPrefix = ModLocalization.Get("MessageCoreDependencyMissingPrefix", "Hard dependency missing: IMDataCore is required for IdolCareerDiary. Details: ");
        internal static readonly string MessageUiFrameworkDependencyMissingPrefix = ModLocalization.Get("MessageUiFrameworkDependencyMissingPrefix", "Hard dependency missing: IMUIFramework is required for IdolCareerDiary. Details: ");

        internal const string MethodRenderHeader = "RenderHeader";
        internal const string MethodSetTab = "SetTab";

        internal const string DiaryTabButtonName = "CareerDiary_TabButton";
        internal const string DiaryPanelName = "CareerDiary_TabPanel";
        internal const string DiaryDetailPopupName = "CareerDiary_DetailPopup";
        internal const string TimelineListName = "CareerDiary_TimelineList";
        internal const string ActionRowName = "CareerDiary_ActionRow";
        internal const string SingleSourceSectionName = "CareerDiary_SingleSourceSection";
        internal const string SingleReleaseSnapshotCardName = "CareerDiary_SingleReleaseSnapshotCard";
        internal const string SingleSourceChartActionRowName = "CareerDiary_SingleSourceChartActionRow";
        internal const string SingleSourceActionsRowName = "CareerDiary_SingleSourceActionsRow";
        internal const string SingleSenbatsuGridName = "CareerDiary_SingleSenbatsuGrid";
        internal const string SingleSenbatsuCardNamePrefix = "CareerDiary_SingleSenbatsuCard_";
        internal const string SingleSenbatsuRowNamePrefix = "CareerDiary_SingleSenbatsuRow_";

        internal static readonly string DiaryTabLabel = ModLocalization.Get("DiaryTabLabel", "Career Diary");
        internal static readonly string TitleDiary = ModLocalization.Get("TitleDiary", "Career Diary");
        internal static readonly string TitleOverview = ModLocalization.Get("TitleOverview", "Overview");
        internal static readonly string TitleTimeline = ModLocalization.Get("TitleTimeline", "Timeline");
        internal static readonly string TitleDetails = ModLocalization.Get("TitleDetails", "Event Details");
        internal static readonly string TitleDetailsSubtitle = ModLocalization.Get("TitleDetailsSubtitle", "Event Detail");
        internal static readonly string TitleSingleSourceDetails = ModLocalization.Get("TitleSingleSourceDetails", "Single Source Details");
        internal static readonly string TitleSingleParticipationRecordedDetails = ModLocalization.Get("TitleSingleParticipationRecordedDetails", "Event: Single Participation Recorded");
        internal static readonly string TitleSingleReleaseSnapshot = ModLocalization.Get("TitleSingleReleaseSnapshot", "Singles Release");
        internal static readonly string TitleSingleReleaseEconomics = ModLocalization.Get("TitleSingleReleaseEconomics", "Release Economics");
        internal static readonly string TitleSingleFanAppeal = ModLocalization.Get("TitleSingleFanAppeal", "Fan Approval");
        internal static readonly string TitleSingleFanSegmentSales = ModLocalization.Get("TitleSingleFanSegmentSales", "Sales by Audience");
        internal static readonly string TitleSingleFanSegmentNewFans = ModLocalization.Get("TitleSingleFanSegmentNewFans", "New Fans by Audience");
        internal static readonly string TitleSingleSenbatsu = ModLocalization.Get("TitleSingleSenbatsu", "Senbatsu");
        internal static readonly string TitleSingleSenbatsuStats = ModLocalization.Get("TitleSingleSenbatsuStats", "Senbatsu Stats");
        internal static readonly string LabelBackToTimeline = ModLocalization.Get("LabelBackToTimeline", "Back to Timeline");
        internal static readonly string LabelOpenSource = ModLocalization.Get("LabelOpenSource", "Open Related Record");
        internal static readonly string LabelOpenSingleFormation = ModLocalization.Get("LabelOpenSingleFormation", "Open Formation Viewer");
        internal static readonly string LabelOpenSingleChart = ModLocalization.Get("LabelOpenSingleChart", "Open Sales Chart");
        internal static readonly string LabelRefresh = ModLocalization.Get("LabelRefresh", "Refresh Timeline");
        internal static readonly string LabelShowMore = ModLocalization.Get("LabelShowMore", "Show More (+100)");
        internal static readonly string LabelClearFilters = ModLocalization.Get("LabelClearFilters", "Clear Filters");
        internal static readonly string LabelNoEventsAfterFilters = ModLocalization.Get("LabelNoEventsAfterFilters", "No timeline entries match current filter toggles.");
        internal static readonly string LabelEventsShownPrefix = ModLocalization.Get("LabelEventsShownPrefix", "Events shown: ");
        internal static readonly string LabelNoEvents = ModLocalization.Get("LabelNoEvents", "No IM Data Core events recorded for this idol yet.");
        internal static readonly string LabelSelectEvent = ModLocalization.Get("LabelSelectEvent", "Select a timeline entry to open detail popup.");
        internal static readonly string LabelOpenProfile = ModLocalization.Get("LabelOpenProfile", "Open Profile");
        internal static readonly string LabelDatingContextTitle = ModLocalization.Get("LabelDatingContextTitle", "Other Idol Involved");
        internal static readonly string LabelRelatedIdolsContextTitle = ModLocalization.Get("LabelRelatedIdolsContextTitle", "Idols Involved");
        internal static readonly string LabelClique = ModLocalization.Get("LabelClique", "Clique");
        internal static readonly string LabelRelationship = ModLocalization.Get("LabelRelationship", "Relationship");
        internal static readonly string LabelBullying = ModLocalization.Get("LabelBullying", "Bullying");
        internal static readonly string LabelStory = ModLocalization.Get("LabelStory", "Story");
        internal static readonly string LabelSingleStatus = ModLocalization.Get("LabelSingleStatus", "Single");
        internal static readonly string LabelNotKnownToProducer = ModLocalization.Get("LabelNotKnownToProducer", "[Not known to Producer]");
        internal static readonly string LabelCastThisEpisodePrefix = ModLocalization.Get("LabelCastThisEpisodePrefix", "Cast this episode: ");
        internal static readonly string LabelEpisodesReleasedPrefix = ModLocalization.Get("LabelEpisodesReleasedPrefix", "Episodes Released This Week: ");
        internal static readonly string LabelEpisodeReleasedPrefix = ModLocalization.Get("LabelEpisodeReleasedPrefix", "Episode Released This Week: ");
        internal static readonly string LabelTotalEpisodesReleasedPrefix = ModLocalization.Get("LabelTotalEpisodesReleasedPrefix", "Total Episodes Released: ");
        internal static readonly string LabelCastTypePrefix = ModLocalization.Get("LabelCastTypePrefix", "Cast: ");
        internal static readonly string LabelStatusPrefix = ModLocalization.Get("LabelStatusPrefix", "Status: ");
        internal static readonly string LabelAverageAudiencePrefix = ModLocalization.Get("LabelAverageAudiencePrefix", "Average audience: ");
        internal static readonly string LabelAverageRevenuePrefix = ModLocalization.Get("LabelAverageRevenuePrefix", "Average revenue: ");
        internal static readonly string LabelAverageNewFansPrefix = ModLocalization.Get("LabelAverageNewFansPrefix", "Average new fans: ");
        internal static readonly string LabelAverageBuzzPrefix = ModLocalization.Get("LabelAverageBuzzPrefix", "Average buzz: ");
        internal static readonly string LabelDatePrefix = ModLocalization.Get("LabelDatePrefix", "Date: ");
        internal static readonly string LabelUnknown = ModLocalization.Get("LabelUnknown", "Unknown");
        internal static readonly string LabelNoSource = ModLocalization.Get("LabelNoSource", "No related popup is available for this event.");
        internal static readonly string LabelMainSystemsUnavailable = ModLocalization.Get("LabelMainSystemsUnavailable", "Main systems unavailable.");
        internal static readonly string OutcomeLinePrefix = ModLocalization.Get("OutcomeLinePrefix", " - ");
        internal static readonly string ListJoinSeparator = ModLocalization.Get("ListJoinSeparator", ", ");
        internal static readonly string MetadataPipeSeparator = ModLocalization.Get("MetadataPipeSeparator", " | ");
        internal static readonly string LabelSingleTitlePrefix = ModLocalization.Get("LabelSingleTitlePrefix", "Title: ");
        internal static readonly string LabelSingleGroupPrefix = ModLocalization.Get("LabelSingleGroupPrefix", "Group: ");
        internal static readonly string LabelSingleReleaseDatePrefix = ModLocalization.Get("LabelSingleReleaseDatePrefix", "Release date: ");
        internal static readonly string LabelSingleSalesPrefix = ModLocalization.Get("LabelSingleSalesPrefix", "Sales: ");
        internal static readonly string LabelSingleChartPrefix = ModLocalization.Get("LabelSingleChartPrefix", "Chart position: ");
        internal static readonly string LabelSingleDidNotChart = ModLocalization.Get("LabelSingleDidNotChart", "Did not chart");
        internal static readonly string LabelSingleQualityPrefix = ModLocalization.Get("LabelSingleQualityPrefix", "Quality: ");
        internal static readonly string LabelSingleFanSatisfactionPrefix = ModLocalization.Get("LabelSingleFanSatisfactionPrefix", "Fan satisfaction: ");
        internal static readonly string LabelSingleFanBuzzPrefix = ModLocalization.Get("LabelSingleFanBuzzPrefix", "Fan buzz: ");
        internal static readonly string LabelSingleNewFansPrefix = ModLocalization.Get("LabelSingleNewFansPrefix", "New fans: ");
        internal static readonly string LabelSingleNewHardcoreFansPrefix = ModLocalization.Get("LabelSingleNewHardcoreFansPrefix", "New hardcore fans: ");
        internal static readonly string LabelSingleNewCasualFansPrefix = ModLocalization.Get("LabelSingleNewCasualFansPrefix", "New casual fans: ");
        internal static readonly string LabelSingleQuantityPrefix = ModLocalization.Get("LabelSingleQuantityPrefix", "Copies produced: ");
        internal static readonly string LabelSingleProductionCostPrefix = ModLocalization.Get("LabelSingleProductionCostPrefix", "Production cost: ");
        internal static readonly string LabelSingleGrossRevenuePrefix = ModLocalization.Get("LabelSingleGrossRevenuePrefix", "Gross revenue: ");
        internal static readonly string LabelSingleOtherExpensesPrefix = ModLocalization.Get("LabelSingleOtherExpensesPrefix", "Other expenses: ");
        internal static readonly string LabelSingleProfitPrefix = ModLocalization.Get("LabelSingleProfitPrefix", "Profit: ");
        internal static readonly string LabelSingleOneCdCostPrefix = ModLocalization.Get("LabelSingleOneCdCostPrefix", "Cost per CD: ");
        internal static readonly string LabelSingleOneCdRevenuePrefix = ModLocalization.Get("LabelSingleOneCdRevenuePrefix", "Revenue per CD: ");
        internal static readonly string LabelSingleSalesPerFanPrefix = ModLocalization.Get("LabelSingleSalesPerFanPrefix", "Sales per fan: ");
        internal static readonly string LabelSingleFamePointsAwardedPrefix = ModLocalization.Get("LabelSingleFamePointsAwardedPrefix", "Fame points awarded: ");
        internal static readonly string LabelSingleGroupHandshakePrefix = ModLocalization.Get("LabelSingleGroupHandshakePrefix", "Group handshake: ");
        internal static readonly string LabelSingleIndividualHandshakePrefix = ModLocalization.Get("LabelSingleIndividualHandshakePrefix", "Individual handshake: ");
        internal static readonly string LabelSingleMarketingResultPrefix = ModLocalization.Get("LabelSingleMarketingResultPrefix", "Marketing result: ");
        internal static readonly string LabelSingleMarketingResultStatusPrefix = ModLocalization.Get("LabelSingleMarketingResultStatusPrefix", "Marketing status: ");
        internal static readonly string LabelSingleMostPopularGenrePrefix = ModLocalization.Get("LabelSingleMostPopularGenrePrefix", "Most popular genre: ");
        internal static readonly string LabelSingleMostPopularLyricsPrefix = ModLocalization.Get("LabelSingleMostPopularLyricsPrefix", "Most popular lyrics: ");
        internal static readonly string LabelSingleMostPopularChoreographyPrefix = ModLocalization.Get("LabelSingleMostPopularChoreographyPrefix", "Most popular choreography: ");
        internal static readonly string LabelSingleFanAppealMalePrefix = ModLocalization.Get("LabelSingleFanAppealMalePrefix", "Male: ");
        internal static readonly string LabelSingleFanAppealFemalePrefix = ModLocalization.Get("LabelSingleFanAppealFemalePrefix", "Female: ");
        internal static readonly string LabelSingleFanAppealCasualPrefix = ModLocalization.Get("LabelSingleFanAppealCasualPrefix", "Casual: ");
        internal static readonly string LabelSingleFanAppealHardcorePrefix = ModLocalization.Get("LabelSingleFanAppealHardcorePrefix", "Hardcore: ");
        internal static readonly string LabelSingleFanAppealTeenPrefix = ModLocalization.Get("LabelSingleFanAppealTeenPrefix", "Teen: ");
        internal static readonly string LabelSingleFanAppealYoungAdultPrefix = ModLocalization.Get("LabelSingleFanAppealYoungAdultPrefix", "Young adult: ");
        internal static readonly string LabelSingleFanAppealAdultPrefix = ModLocalization.Get("LabelSingleFanAppealAdultPrefix", "Adult: ");
        internal static readonly string LabelSingleCohortHardcoreFansPrefix = ModLocalization.Get("LabelSingleCohortHardcoreFansPrefix", "Cohort hardcore fans: ");
        internal static readonly string LabelSingleCohortCasualFansPrefix = ModLocalization.Get("LabelSingleCohortCasualFansPrefix", "Cohort casual fans: ");
        internal static readonly string LabelSingleCohortOtherFansPrefix = ModLocalization.Get("LabelSingleCohortOtherFansPrefix", "Cohort other fans: ");
        internal static readonly string LabelSingleNewFansDeltaPrefix = ModLocalization.Get("LabelSingleNewFansDeltaPrefix", "Release/cohort difference: ");
        internal static readonly string LabelSingleCohortBreakdownUnavailable = ModLocalization.Get("LabelSingleCohortBreakdownUnavailable", "Cohort breakdown unavailable in this event payload.");
        internal static readonly string TextSingleCohortEstimatedFromFanAppeal = ModLocalization.Get("TextSingleCohortEstimatedFromFanAppeal", "Cohort split estimated from fan-appeal weights because detailed segment data was empty.");
        internal static readonly string TextReleaseDataTag = ModLocalization.Get("TextReleaseDataTag", " (release data)");
        internal static readonly string TextCohortSumTag = ModLocalization.Get("TextCohortSumTag", " (cohort sum)");
        internal static readonly string TextSingleFanAppealAxesNote = ModLocalization.Get("TextSingleFanAppealAxesNote", "Fan appeal uses separate axes (gender/intensity/age), so percentages do not add into one total.");
        internal static readonly string LabelSingleSegmentSummaryEmpty = ModLocalization.Get("LabelSingleSegmentSummaryEmpty", "No segment data captured.");
        internal static readonly string LabelSingleGenrePrefix = ModLocalization.Get("LabelSingleGenrePrefix", "Genre: ");
        internal static readonly string LabelSingleLyricsPrefix = ModLocalization.Get("LabelSingleLyricsPrefix", "Lyrics: ");
        internal static readonly string LabelSingleChoreographyPrefix = ModLocalization.Get("LabelSingleChoreographyPrefix", "Choreography: ");
        internal static readonly string LabelSingleMarketingPrefix = ModLocalization.Get("LabelSingleMarketingPrefix", "Marketing: ");
        internal static readonly string LabelSingleTotalSalesPrefix = ModLocalization.Get("LabelSingleTotalSalesPrefix", "Total Sales: ");
        internal static readonly string LabelSingleTotalNewFansPrefix = ModLocalization.Get("LabelSingleTotalNewFansPrefix", "Total New Fans: ");
        internal static readonly string LabelSingleRosterEmpty = ModLocalization.Get("LabelSingleRosterEmpty", "No senbatsu members available in this record.");
        internal static readonly string LabelSingleFormationOpened = ModLocalization.Get("LabelSingleFormationOpened", "Opened formation viewer for this single.");
        internal static readonly string LabelSingleFormationPopupUnavailable = ModLocalization.Get("LabelSingleFormationPopupUnavailable", "Single formation popup unavailable.");
        internal static readonly string LabelSingleFormationComponentUnavailable = ModLocalization.Get("LabelSingleFormationComponentUnavailable", "Single formation component unavailable.");
        internal static readonly string LabelSingleChartOpened = ModLocalization.Get("LabelSingleChartOpened", "Opened chart popup for this single.");
        internal static readonly string LabelSingleChartMonthUnknown = ModLocalization.Get("LabelSingleChartMonthUnknown", "Unable to locate chart month for this release.");
        internal static readonly string LabelSingleChartPopupUnavailable = ModLocalization.Get("LabelSingleChartPopupUnavailable", "Single chart popup unavailable.");
        internal static readonly string LabelSingleChartComponentUnavailable = ModLocalization.Get("LabelSingleChartComponentUnavailable", "Single chart component unavailable.");
        internal static readonly string LabelPopupOpenTransitionFailed = ModLocalization.Get("LabelPopupOpenTransitionFailed", "Popup transition did not complete.");
        internal static readonly string LabelSingleNotFound = ModLocalization.Get("LabelSingleNotFound", "Single source record unavailable.");
        internal static readonly string LabelSingleInvalidId = ModLocalization.Get("LabelSingleInvalidId", "Single source id is invalid.");
        internal static readonly string LabelSingleMemberPrefix = ModLocalization.Get("LabelSingleMemberPrefix", "Member: ");
        internal static readonly string LabelSingleSenbatsuRowPrefix = ModLocalization.Get("LabelSingleSenbatsuRowPrefix", "Row ");
        internal static readonly string LabelSingleSenbatsuPositionPrefix = ModLocalization.Get("LabelSingleSenbatsuPositionPrefix", "Position ");

        internal static readonly string LabelIdolPrefix = ModLocalization.Get("LabelIdolPrefix", "Idol: ");
        internal static readonly string LabelEventCountPrefix = ModLocalization.Get("LabelEventCountPrefix", "Events loaded: ");
        internal static readonly string LabelDataWarningPrefix = ModLocalization.Get("LabelDataWarningPrefix", "Data warning: ");
        internal static readonly string LabelActionPrefix = ModLocalization.Get("LabelActionPrefix", "Last action: ");

        internal static readonly string LabelWhenPrefix = ModLocalization.Get("LabelWhenPrefix", "When: ");
        internal static readonly string LabelWhatPrefix = ModLocalization.Get("LabelWhatPrefix", "What: ");
        internal static readonly string LabelWithWhomPrefix = ModLocalization.Get("LabelWithWhomPrefix", "With whom: ");
        internal static readonly string LabelShow = ModLocalization.Get("LabelShow", "Show");
        internal static readonly string LabelOutcomePrefix = ModLocalization.Get("LabelOutcomePrefix", "Outcome change: ");
        internal static readonly string LabelSourcePrefix = ModLocalization.Get("LabelSourcePrefix", "Related record: ");
        internal static readonly string LabelNamespacePrefix = ModLocalization.Get("LabelNamespacePrefix", "Event Namespace: ");
        internal static readonly string LabelPatchPrefix = ModLocalization.Get("LabelPatchPrefix", "Capture Hook: ");
        internal static readonly string LabelPayloadPrefix = ModLocalization.Get("LabelPayloadPrefix", "Raw Event Data: ");
        internal static readonly bool ShowTechnicalEventMetadata = false;

        internal static readonly string OpenIdolPrefix = ModLocalization.Get("OpenIdolPrefix", "Open ");
        internal static readonly string OpenIdolSuffix = ModLocalization.Get("OpenIdolSuffix", " profile");

        internal static readonly string DateFormatUi = ModLocalization.Get("DateFormatUi", "ddd, dd MMM yyyy");
        internal static readonly string[] EnglishAbbreviatedMonthTokens =
        {
            "Jan", "Feb", "Mar", "Apr", "May", "Jun",
            "Jul", "Aug", "Sep", "Sept", "Oct", "Nov", "Dec"
        };
        internal static readonly string TextDurationUnitDaySingular = ModLocalization.Get("TextDurationUnitDaySingular", "day");
        internal static readonly string TextDurationUnitDayPlural = ModLocalization.Get("TextDurationUnitDayPlural", "days");
        internal static readonly string TextDurationUnitWeekSingular = ModLocalization.Get("TextDurationUnitWeekSingular", "week");
        internal static readonly string TextDurationUnitWeekPlural = ModLocalization.Get("TextDurationUnitWeekPlural", "weeks");
        internal static readonly string TextDurationUnitMonthSingular = ModLocalization.Get("TextDurationUnitMonthSingular", "month");
        internal static readonly string TextDurationUnitMonthPlural = ModLocalization.Get("TextDurationUnitMonthPlural", "months");
        internal static readonly string TextDurationUnitYearSingular = ModLocalization.Get("TextDurationUnitYearSingular", "year");
        internal static readonly string TextDurationUnitYearPlural = ModLocalization.Get("TextDurationUnitYearPlural", "years");
        internal const string DateFormatRoundTrip = "o";
        internal const string OutcomeLinesJoinSeparator = "\n";

        internal const int MinId = 0;
        internal const int InvalidId = -1;
        internal const long InvalidEventId = -1L;

        internal const int MaxEventsRequest = 1000;
        internal const int MaxEventsRender = 300;
        internal const int MaxFilterControlButtons = 12;
        internal const int TimelineActionButtonsPerRow = 2;
        internal const int TimelineToolbarActionsPerRow = 3;
        internal const int TimelineFilterButtonsPerRow = 3;
        internal const int EventsRenderStep = 100;
        internal const int MaxNamesInOutcomeSummary = 5;
        internal const int MaxPayloadPreviewChars = 280;
        internal const int MaxTimelineRowChars = 140;

        internal const int ZeroIndex = 0;
        internal const int LastFromCount = 1;
        internal const int DetailPopupSiblingOffset = 2;

        internal const float TimelineRowHeight = 30f;
        internal const float TimelineStaffPortraitSize = 24f;
        internal const float TimelineSpacing = 4f;
        internal const int TimelineParticipantCardsPerRow = 4;
        internal const float TimelineParticipantSectionSpacing = 4f;
        internal const float TimelineParticipantRowSpacing = 6f;
        internal const float TimelineParticipantCardWidth = 92f;
        internal const float TimelineParticipantCardHeight = 118f;
        internal const float TimelineParticipantPortraitSize = 60f;
        internal const float TimelineParticipantNameLineHeight = 44f;
        internal const int TimelineParticipantNameFontSize = 12;
        internal const float TimelineParticipantSectionTopPadding = 2f;
        internal const float TimelineParticipantSectionBottomPadding = 4f;
        internal const float ActionSpacing = 8f;
        internal const float ActionButtonHeight = 36f;
        internal const float ActionRowMinimumHeight = 44f;
        internal const float ActionButtonHorizontalPadding = 64f;
        internal const float CustomActionButtonTextHorizontalInset = 24f;
        internal const float CustomActionButtonTextVerticalInset = -2f;
        internal const float ActionButtonMinimumWidth = 210f;
        internal const float ActionButtonMaximumWidth = 460f;
        internal const float ActionButtonLegacyCharWidthApprox = 9f;
        internal const float TimelineToolbarButtonMinimumWidth = 220f;
        internal const float TimelineToolbarButtonMaximumWidth = 360f;
        internal const float TimelineActionToolbarButtonMinimumWidth = 160f;
        internal const float TimelineActionToolbarButtonMaximumWidth = 220f;
        internal const float TimelineFilterButtonMinimumWidth = 220f;
        internal const float TimelineFilterButtonMaximumWidth = 560f;
        internal const int TimelineToolbarButtonFontSize = 12;
        internal const float DividerHeight = 2f;
        internal const float HeaderButtonHorizontalSpacing = 16f;
        internal const float HeaderButtonMinimumWidth = 220f;
        internal const float HeaderButtonWidthScale = 1.8f;
        internal const float BlurCleanupDurationSeconds = 0.1f;
        internal const float PopupGhostAlphaThreshold = 0.001f;
        internal const float SingleSenbatsuGridSpacingX = 8f;
        internal const float SingleSenbatsuGridSpacingY = 8f;
        internal const float SingleSenbatsuRowSpacing = 3f;
        internal const float SingleSenbatsuRowLabelHeight = 22f;
        internal const float SingleSenbatsuIconSize = 56f;
        internal const float SingleSenbatsuIconCardWidth = 64f;
        internal const float SingleSenbatsuIconCardHeight = 86f;
        internal const float SingleSenbatsuIconMetaLineHeight = 18f;
        internal const float SingleSenbatsuCardWidth = 136f;
        internal const float SingleSenbatsuCardHeight = 188f;
        internal const float SingleSenbatsuPortraitSize = 96f;
        internal const float SingleSenbatsuCardSpacing = 4f;
        internal const float SingleSenbatsuCardTextLineHeight = 24f;
        internal const float SingleSnapshotCardSpacing = 4f;
        internal const int SingleSnapshotCardPaddingHorizontal = 12;
        internal const int SingleSnapshotCardPaddingVertical = 10;
        internal const int SingleSnapshotBodyFontSize = 12;
        internal const int SingleSnapshotHeaderFontSize = 13;
        internal const float RelatedIdolCardWidth = 178f;
        internal const float RelatedIdolCardHeight = 224f;
        internal const float RelatedIdolPortraitSize = 108f;
        internal const float RelatedIdolCardSpacing = 8f;
        internal const float RelatedIdolCardNameLineHeight = 26f;
        internal const float RelatedIdolCardButtonHeight = 32f;
        internal const float RelatedIdolSectionTopPadding = 8f;
        internal const float RelatedIdolSectionBottomPadding = 2f;

        internal const int CloseButtonNameMatchScore = 120;
        internal const int CloseButtonMethodMatchScore = 90;
        internal const int CloseButtonPopupManagerTargetScore = 80;
        internal const int CloseButtonActiveHierarchyScore = 40;
        internal const int CloseButtonInProfileHierarchyScore = 40;
        internal const int CloseButtonInteractableScore = 20;
        internal const int CloseButtonExactLabelMatchScore = 240;
        internal const int CloseButtonLocalizationKeyMatchScore = 220;
        internal const int CloseButtonPreferredNameMatchScore = 120;
        internal const int CloseButtonTextContainsCloseTokenScore = 60;
        internal const int CloseButtonTextMatchScore = 50;
        internal const int CloseButtonTooltipMatchScore = 30;
        internal const int CloseButtonMinimumAcceptableScore = 60;
        internal const int SenbatsuMaximumSlots = 15;
        internal const int SingleSenbatsuGridColumnCount = 5;
        internal const int UnknownChartPosition = 0;
        internal const int UnknownGraduationYear = 1900;
        internal const int InvalidMonthId = -1;
        internal const int ReleaseDateChartMonthOffset = 1;
        internal const int SenbatsuDisplayIndexOffset = 1;
        internal const int SingleCardNameFontSize = 14;
        internal const int SingleCardMetaFontSize = 13;
        internal const int ActionRowTopPadding = 10;
        internal const int ActionRowBottomPadding = 2;

        internal const string CloseTokenLower = "close";
        internal const string CloseActionButtonObjectName = "OK";
        internal const string CloseMethodToken = "Close";
        internal const string CloseGlyphUpper = "X";
        internal const string PopupManagerTypeToken = "PopupManager";
        internal const string LanguageKeyClose = "CLOSE";
        internal const string LanguageKeyButtonClose = "BUTTON__CLOSE";
        internal const string LanguageKeyPopupClose = "POPUP__CLOSE";
        internal const string LanguageKeySunday = "SUNDAY";
        internal const string LanguageKeyMonday = "MONDAY";
        internal const string LanguageKeyTuesday = "TUESDAY";
        internal const string LanguageKeyWednesday = "WEDNESDAY";
        internal const string LanguageKeyThursday = "THURSDAY";
        internal const string LanguageKeyFriday = "FRIDAY";
        internal const string LanguageKeySaturday = "SATURDAY";
        internal const string LanguageKeyShow = "SHOW";
        internal const string LanguageKeySingle = "SINGLE";
        internal const string LanguageKeyLoan = "LOAN";
        internal const string LanguageKeyBusiness = "BIZ";
        internal const string LanguageKeyBusinessAd = "BIZ_AD";
        internal const string LanguageKeyBusinessVariety = "BIZ__VARIETY";
        internal const string LanguageKeyBusinessTv = "BIZ__TV";
        internal const string LanguageKeyActivityPromotion = "ACTIVITIES__PROMOTION";
        internal const string LanguageKeyActivitySpaTreatment = "ACTIVITIES__SPATREATMENT";
        internal const string LanguageKeySingleGenre = "SINGLE__GENRE";
        internal const string LanguageKeySingleLyrics = "SINGLE__LYRICS";
        internal const string LanguageKeySingleSong = "SINGLE__SONG";
        internal const string LanguageKeySingleChoreography = "SINGLE__CHOREO";
        internal const string LanguageKeySingleMarketing = "SINGLE__MARKETING";
        internal const string LanguageKeyMediaPreproduction = "MEDIA__PREPROD";
        internal const string LanguageKeyMediaConcept = "MEDIA__CONCEPT";
        internal const string LanguageKeySingleReleased = "SINGLES__RELEASED";
        internal const string LanguageKeyProfileBestFriends = "PROFILE__BEST_FRIENDS";
        internal const string LanguageKeyProfileFriends = "PROFILE__FRIENDS";
        internal const string LanguageKeyProfileDislikes = "PROFILE__DISLIKES";
        internal const string LanguageKeyProfileHates = "PROFILE__HATES";
        internal const string LanguageKeyShowRadio = "SHOW__RADIO";
        internal const string LanguageKeyShowInternet = "SHOW__INTERNET";
        internal const string LanguageKeyShowTv = "SHOW__TV";
        internal const string LanguageKeyShowEntireGroup = "SHOW__ENTIRE_GROUP";
        internal const string LanguageKeyShowRotatingCast = "SHOW__ROTATING_CAST";
        internal const string LanguageKeyShowPermanentCast = "SHOW__PERMANENT_CAST";
        internal const string LanguageKeyResearchProducer = "RESEARCH__PRODUCER";
        internal const string LanguageKeyLoanFujimoto = "LOANS__FUJIMOTO";
        internal const string LanguageKeyLoanBank = "LOANS__BANK";
        internal const string LanguageKeyLoanInvestor = "LOANS__INVESTOR";
        internal const string LanguageKeyLoanOneMonth = "LOANS__1_MONTH";
        internal const string LanguageKeyLoanThreeMonths = "LOANS__3_MONTHS";
        internal const string LanguageKeyLoanSixMonths = "LOANS__6_MONTHS";
        internal const string LanguageKeyTheaterPerformance = "THEATER__PERFORMANCE";
        internal const string LanguageKeyTheaterManzai = "THEATER__MANZAI";
        internal const string LanguageKeyTheaterDayOff = "THEATER__DAY_OFF";
        internal const string LanguageKeyTheaterEveryone = "THEATER__EVERYONE";
        internal const string LanguageKeyCafeAuto = "CAFE__AUTO";
        internal const string LanguageKeyCafeHighestStamina = "CAFE__HIGHEST_STAMINA";
        internal const string LanguageKeyCafeBestStats = "CAFE__BEST_STATS";
        internal const string LanguageKeyPopular = "POPULAR";
        internal const string LanguageKeyUnpopular = "UNPOPULAR";
        internal static readonly string LabelCloseFallback = ModLocalization.Get("LabelCloseFallback", "Close");
        internal const string MethodChartPopupRender = "Render";

        internal const string EventSingleReleased = "single_released";
        internal const string EventSingleParticipationRecorded = "single_participation_recorded";
        internal const string EventSingleCreated = "single_created";
        internal const string EventSingleCancelled = "single_cancelled";
        internal const string EventSingleStatusChanged = "single_status_changed";
        internal const string EventSingleCastChanged = "single_cast_changed";
        internal const string EventSingleGroupChanged = "single_group_changed";
        internal const string EventStatusChanged = "idol_status_changed";
        internal const string EventStatusChangedLegacy = "status_changed";
        internal const string EventStatusStarted = "status_started";
        internal const string EventStatusEnded = "status_ended";
        internal const string EventDatingPartnerStatusChanged = "dating_partner_status_changed";
        internal const string EventIdolDatingStatusChanged = "idol_dating_status_changed";
        internal const string EventIdolDatingStarted = "idol_dating_started";
        internal const string EventIdolDatingEnded = "idol_dating_ended";
        internal const string EventIdolRelationshipStatusChanged = "idol_relationship_status_changed";
        internal const string EventBullyingStarted = "bullying_started";
        internal const string EventBullyingEnded = "bullying_ended";
        internal const string EventContractActivated = "contract_activated";
        internal const string EventContractWindowOpened = "contract_window_opened";
        internal const string EventContractAccepted = "contract_accepted";
        internal const string EventContractCancelled = "contract_cancelled";
        internal const string EventContractCanceled = "contract_canceled";
        internal const string EventContractBroken = "contract_broken";
        internal const string EventContractFinished = "contract_finished";
        internal const string EventContractWeeklyEarningsApplied = "contract_weekly_earnings_applied";
        internal const string EventContractWeeklyBenefitsApplied = "contract_weekly_benefits_applied";
        internal const string EventShowCreated = "show_created";
        internal const string EventShowReleased = "show_released";
        internal const string EventShowCancelled = "show_cancelled";
        internal const string EventShowStatusChanged = "show_status_changed";
        internal const string EventShowEpisodeReleased = "show_episode_released";
        internal const string EventShowEpisode = "show_episode";
        internal const string EventShowCastChanged = "show_cast_changed";
        internal const string EventShowConfigurationChanged = "show_configuration_changed";
        internal const string EventShowRelaunchStarted = "show_relaunch_started";
        internal const string EventShowRelaunchFinished = "show_relaunch_finished";
        internal const string EventTourCreated = "tour_created";
        internal const string EventTourStarted = "tour_started";
        internal const string EventTourFinished = "tour_finished";
        internal const string EventTourCancelled = "tour_cancelled";
        internal const string EventTourCountryResult = "tour_country_result";
        internal const string EventTourParticipation = "tour_participation";
        internal const string EventTourStatusChanged = "tour_status_changed";
        internal const string EventElectionCreated = "election_created";
        internal const string EventElectionStarted = "election_started";
        internal const string EventElectionFinished = "election_finished";
        internal const string EventElectionCancelled = "election_cancelled";
        internal const string EventElectionResultsGenerated = "election_results_generated";
        internal const string EventElectionPlaceAdjusted = "election_place_adjusted";
        internal const string EventElectionStatusChanged = "election_status_changed";
        internal const string EventElectionResultRecorded = "election_result_recorded";
        internal const string EventScandalPointsChanged = "scandal_points_changed";
        internal const string EventScandalMitigated = "scandal_mitigated";
        internal const string EventMedicalInjury = "medical_injury";
        internal const string EventMedicalDepression = "medical_depression";
        internal const string EventMedicalHiatusStarted = "medical_hiatus_started";
        internal const string EventMedicalHealed = "medical_healed";
        internal const string EventMedicalHiatusFinished = "medical_hiatus_finished";
        internal const string EventTheaterCreated = "theater_created";
        internal const string EventTheaterDestroyed = "theater_destroyed";
        internal const string EventTheaterDailyResult = "theater_daily_result";
        internal const string EventActivityPerformance = "activity_performance";
        internal const string EventActivityPromotion = "activity_promotion";
        internal const string EventActivitySpaTreatment = "activity_spa_treatment";
        internal const string EventResearchPointsAccrued = "research_points_accrued";
        internal const string EventResearchParamAssigned = "research_param_assigned";
        internal const string EventResearchPointsPurchased = "research_points_purchased";
        internal const string EventResearchParamLevelUp = "research_param_level_up";
        internal const string EventEconomyDailyTick = "economy_daily_tick";
        internal const string EventEconomyWeeklyExpenseApplied = "economy_weekly_expense_applied";
        internal const string EventPolicyDecisionSelected = "policy_decision_selected";
        internal const string EventRivalTrendsUpdated = "rival_trends_updated";
        internal const string EventRivalMonthlyRecalculated = "rival_monthly_recalculated";
        internal const string EventIdolEarningsRecorded = "idol_earnings_recorded";
        internal const string EventIdolHired = "idol_hired";
        internal const string EventIdolGraduationAnnounced = "idol_graduation_announced";
        internal const string EventIdolGraduated = "idol_graduated";
        internal const string EventIdolBirthday = "idol_birthday";
        internal const string EventIdolGroupTransferred = "idol_group_transferred";
        internal const string EventIdolSalaryChanged = "idol_salary_changed";
        internal const string EventGroupCreated = "group_created";
        internal const string EventGroupDisbanded = "group_disbanded";
        internal const string EventGroupParamPointsChanged = "group_param_points_changed";
        internal const string EventGroupAppealPointsSpent = "group_appeal_points_spent";
        internal const string EventPushWindowStarted = "push_window_started";
        internal const string EventPushWindowEnded = "push_window_ended";
        internal const string EventPushWindowDayIncrement = "push_window_day_increment";
        internal const string EventPlayerRelationshipChanged = "player_relationship_changed";
        internal const string EventPlayerDateInteraction = "player_date_interaction";
        internal const string EventPlayerMarriageOutcome = "player_marriage_outcome";
        internal const string EventRandomEventStarted = "random_event_started";
        internal const string EventRandomEventConcluded = "random_event_concluded";
        internal const string EventSubstoryStarted = "substory_started";
        internal const string EventSubstoryDelayed = "substory_delayed";
        internal const string EventSubstoryCompleted = "substory_completed";
        internal const string EventTaskAdded = "task_added";
        internal const string EventTaskCompleted = "task_completed";
        internal const string EventTaskFailed = "task_failed";
        internal const string EventTaskDone = "task_done";
        internal const string EventIdolOutfitChanged = "idol_outfit_changed";
        internal const string EventWishGenerated = "wish_generated";
        internal const string EventWishFulfilled = "wish_fulfilled";
        internal const string EventWishDone = "wish_done";
        internal const string EventInfluenceBlackmailQueued = "influence_blackmail_queued";
        internal const string EventInfluenceBlackmailTriggered = "influence_blackmail_triggered";
        internal const string EventAuditionStarted = "audition_started";
        internal const string EventAuditionCostPaid = "audition_cost_paid";
        internal const string EventAuditionCooldownReset = "audition_cooldown_reset";
        internal const string EventAuditionFailureTriggered = "audition_failure_triggered";
        internal const string EventAwardNominated = "award_nominated";
        internal const string EventAwardResult = "award_result";
        internal const string EventBankruptcyCheck = "bankruptcy_check";
        internal const string EventBankruptcyDangerSet = "bankruptcy_danger_set";
        internal const string EventCafeCreated = "cafe_created";
        internal const string EventCafeDailyResult = "cafe_daily_result";
        internal const string EventCafeDestroyed = "cafe_destroyed";
        internal const string EventLoanAdded = "loan_added";
        internal const string EventLoanInitialized = "loan_initialized";
        internal const string EventLoanPaidOff = "loan_paid_off";
        internal const string EventMentorshipEnded = "mentorship_ended";
        internal const string EventMentorshipStarted = "mentorship_started";
        internal const string EventMentorshipWeeklyTick = "mentorship_weekly_tick";
        internal const string EventScandalCheck = "scandal_check";
        internal const string EventStoryRouteLocked = "story_route_locked";
        internal const string EventSummerGamesFinalized = "summer_games_finalized";
        internal const string EventCliqueJoined = "clique_joined";
        internal const string EventCliqueLeft = "clique_left";
        internal const string EventConcertCreated = "concert_created";
        internal const string EventConcertStarted = "concert_started";
        internal const string EventConcertFinished = "concert_finished";
        internal const string EventConcertCancelled = "concert_cancelled";
        internal const string EventConcertParticipation = "concert_participation";
        internal const string EventConcertCastChanged = "concert_cast_changed";
        internal const string EventConcertStatusChanged = "concert_status_changed";
        internal const string EventConcertConfigurationChanged = "concert_configuration_changed";
        internal const string EventConcertCardUsed = "concert_card_used";
        internal const string EventConcertCrisisDecision = "concert_crisis_decision";
        internal const string EventConcertCrisisApplied = "concert_crisis_applied";
        internal const string EventConcertFinalResolved = "concert_final_resolved";
        internal const string EventAgencyRoomBuilt = "agency_room_built";
        internal const string EventAgencyRoomDestroyed = "agency_room_destroyed";
        internal const string EventAgencyRoomCostPaid = "agency_room_cost_paid";
        internal const string EventRoomWorkCompleted = "room_work_completed";
        internal const string EventStaffHired = "staff_hired";
        internal const string EventStaffFired = "staff_fired";
        internal const string EventStaffFiredSeverance = "staff_fired_severance";
        internal const string EventStaffLevelUp = "staff_level_up";

        internal const string KindSingle = "single";
        internal const string KindGroup = "group";
        internal const string KindIdol = "idol";
        internal const string KindStatus = "idol_status";
        internal const string KindDatingRelationship = "dating_relationship";
        internal const string KindIdolDatingState = "idol_dating_state";
        internal const string KindContract = "contract";
        internal const string KindShow = "show";
        internal const string KindTour = "tour";
        internal const string KindElection = "election";
        internal const string KindScandal = "scandal";
        internal const string KindMedical = "medical";
        internal const string KindRelationship = "relationship";
        internal const string KindBullying = "bullying";
        internal const string KindClique = "clique";
        internal const string KindTheater = "theater";
        internal const string KindActivity = "activity";
        internal const string KindLoan = "loan";
        internal const string KindBankruptcy = "bankruptcy";
        internal const string KindCafe = "cafe";
        internal const string KindMentorship = "mentorship";
        internal const string KindStory = "story";
        internal const string KindSummerGames = "summer_games";

        internal const string JsonSingleTitle = "single_title";
        internal const string JsonRowIndex = "row_index";
        internal const string JsonPositionIndex = "position_index";
        internal const string JsonIsCenter = "is_center";
        internal const string JsonTotalSales = "total_sales";
        internal const string JsonQuality = "quality";
        internal const string JsonFanSatisfaction = "fan_satisfaction";
        internal const string JsonFanBuzz = "fan_buzz";
        internal const string JsonNewFans = "new_fans";
        internal const string JsonNewHardcoreFans = "new_hardcore_fans";
        internal const string JsonNewCasualFans = "new_casual_fans";
        internal const string JsonSingleQuantity = "single_quantity";
        internal const string JsonSingleProductionCost = "single_production_cost";
        internal const string JsonSingleMarketingResult = "single_marketing_result";
        internal const string JsonSingleMarketingResultStatus = "single_marketing_result_status";
        internal const string JsonSingleGrossRevenue = "single_gross_revenue";
        internal const string JsonSingleOneCdCost = "single_one_cd_cost";
        internal const string JsonSingleOneCdRevenue = "single_one_cd_revenue";
        internal const string JsonSingleOtherExpenses = "single_other_expenses";
        internal const string JsonSingleIsGroupHandshake = "single_is_group_handshake";
        internal const string JsonSingleIsIndividualHandshake = "single_is_individual_handshake";
        internal const string JsonSingleFamePointsAwarded = "single_fame_points_awarded";
        internal const string JsonSingleProfit = "single_profit";
        internal const string JsonSingleSalesPerFan = "single_sales_per_fan";
        internal const string JsonSingleMostPopularGenre = "single_most_popular_genre";
        internal const string JsonSingleMostPopularLyrics = "single_most_popular_lyrics";
        internal const string JsonSingleMostPopularChoreo = "single_most_popular_choreo";
        internal const string JsonSingleFanAppealMale = "single_fan_appeal_male";
        internal const string JsonSingleFanAppealFemale = "single_fan_appeal_female";
        internal const string JsonSingleFanAppealCasual = "single_fan_appeal_casual";
        internal const string JsonSingleFanAppealHardcore = "single_fan_appeal_hardcore";
        internal const string JsonSingleFanAppealTeen = "single_fan_appeal_teen";
        internal const string JsonSingleFanAppealYoungAdult = "single_fan_appeal_young_adult";
        internal const string JsonSingleFanAppealAdult = "single_fan_appeal_adult";
        internal const string JsonSingleFanSegmentSalesSummary = "single_fan_segment_sales_summary";
        internal const string JsonSingleFanSegmentNewFansSummary = "single_fan_segment_new_fans_summary";
        internal const string JsonChartPosition = "chart_position";
        internal const string JsonSingleFameOfSenbatsu = "single_fame_of_senbatsu";
        internal const string JsonSingleSenbatsuStatsSnapshot = "single_senbatsu_stats_snapshot";
        internal const string JsonPrevStatus = "previous_status";
        internal const string JsonNewStatus = "new_status";
        internal const string JsonDatingRoute = "dating_route";
        internal const string JsonDatingRouteStage = "dating_route_stage";
        internal const string JsonPrevPartnerStatus = "previous_partner_status";
        internal const string JsonNewPartnerStatus = "new_partner_status";
        internal const string JsonDatingHadScandal = "dating_had_scandal";
        internal const string JsonDatingHadScandalEver = "dating_had_scandal_ever";
        internal const string JsonDatingUsedGoods = "dating_used_goods";
        internal const string JsonDatingDatedIdol = "dating_dated_idol";
        internal const string JsonIdolId = "idol_id";
        internal const string JsonStaffId = "staff_id";
        internal const string JsonStaffName = "staff_name";
        internal const string JsonStaffRole = "staff_role";
        internal const string JsonStaffTypeRaw = "staff_type_raw";
        internal const string JsonStaffUniqueTypeRaw = "staff_unique_type_raw";
        internal const string JsonStaffIsPro = "staff_is_pro";
        internal const string JsonStaffIsProducer = "staff_is_producer";
        internal const string JsonRoomWorkKind = "room_work_kind";
        internal const string JsonRoomWorkStage = "room_work_stage";
        internal const string JsonRoomWorkEntityId = "room_work_entity_id";
        internal const string JsonRoomWorkTitle = "room_work_title";
        internal const string JsonAssignedStaffId = "assigned_staff_id";
        internal const string JsonAssignedStaffName = "assigned_staff_name";
        internal const string JsonAssignedStaffRole = "assigned_staff_role";
        internal const string JsonCompletedStaffId = "completed_staff_id";
        internal const string JsonCompletedStaffName = "completed_staff_name";
        internal const string JsonCompletedStaffRole = "completed_staff_role";
        internal const string JsonContractType = "contract_type";
        internal const string JsonContractSkill = "contract_skill";
        internal const string JsonContractStartDate = "contract_start_date";
        internal const string JsonContractWeeklyPayment = "contract_weekly_payment";
        internal const string JsonContractWeeklyBuzz = "contract_weekly_buzz";
        internal const string JsonContractWeeklyFame = "contract_weekly_fame";
        internal const string JsonContractWeeklyFans = "contract_weekly_fans";
        internal const string JsonContractWeeklyStamina = "contract_weekly_stamina";
        internal const string JsonContractWeeklyTrainingPoints = "contract_weekly_training_points";
        internal const string JsonContractIsGroup = "contract_is_group";
        internal const string JsonContractWeeklyAction = "contract_weekly_action";
        internal const string JsonContractLiability = "contract_liability";
        internal const string JsonContractAgentName = "contract_agent_name";
        internal const string JsonContractProductName = "contract_product_name";
        internal const string JsonContractEndDate = "contract_end_date";
        internal const string JsonContractDurationMonths = "contract_duration_months";
        internal const string JsonContractIsImmediate = "contract_is_immediate";
        internal const string JsonContractDamagesApplied = "contract_damages_applied";
        internal const string JsonContractTotalBrokenLiability = "contract_total_broken_liability";
        internal const string JsonContractBreakContext = "contract_break_context";
        internal const string JsonSalaryAction = "salary_action";
        internal const string JsonSalaryBefore = "salary_before";
        internal const string JsonSalaryAfter = "salary_after";
        internal const string JsonSalaryDelta = "salary_delta";
        internal const string JsonSalarySatisfactionBefore = "salary_satisfaction_before";
        internal const string JsonSalarySatisfactionAfter = "salary_satisfaction_after";
        internal const string JsonShowTitle = "show_title";
        internal const string JsonShowPrevStatus = "show_previous_status";
        internal const string JsonShowNewStatus = "show_new_status";
        internal const string JsonShowCastType = "show_cast_type";
        internal const string JsonShowEpisodeCount = "show_episode_count";
        internal const string JsonShowEpisodeDate = "show_episode_date";
        internal const string JsonShowCastCount = "show_cast_count";
        internal const string JsonShowCastIdList = "show_cast_id_list";
        internal const string JsonShowAudience = "show_latest_audience";
        internal const string JsonShowRevenue = "show_latest_revenue";
        internal const string JsonShowNewFans = "show_latest_new_fans";
        internal const string JsonShowBuzz = "show_latest_buzz";
        internal const string JsonTourPrevStatus = "tour_previous_status";
        internal const string JsonTourNewStatus = "tour_new_status";
        internal const string JsonTourCountryCount = "tour_selected_country_count";
        internal const string JsonTourAudience = "tour_total_audience";
        internal const string JsonTourRevenue = "tour_total_revenue";
        internal const string JsonTourNewFans = "tour_total_new_fans";
        internal const string JsonTourCost = "tour_production_cost";
        internal const string JsonTourExpectedRevenue = "tour_expected_revenue";
        internal const string JsonTourSaving = "tour_saving";
        internal const string JsonTourFinishDate = "tour_finish_date";
        internal const string JsonElectionPrevStatus = "election_previous_status";
        internal const string JsonElectionNewStatus = "election_new_status";
        internal const string JsonElectionBroadcast = "election_broadcast_type";
        internal const string JsonElectionResultCount = "election_result_count";
        internal const string JsonElectionFinishDate = "election_finish_date";
        internal const string JsonElectionPlace = "election_place";
        internal const string JsonElectionVotes = "election_votes";
        internal const string JsonElectionFamePoints = "election_fame_points";
        internal const string JsonScandalPrev = "scandal_previous_points";
        internal const string JsonScandalNew = "scandal_new_points";
        internal const string JsonScandalDelta = "scandal_delta_points";
        internal const string JsonScandalSource = "scandal_mutation_source";
        internal const string JsonMedicalType = "medical_event_type";
        internal const string JsonMedicalPrevStatus = "medical_previous_status";
        internal const string JsonMedicalCurrentStatus = "medical_current_status";
        internal const string JsonMedicalHiatusEnd = "medical_hiatus_end_date";
        internal const string JsonMedicalForced = "medical_finish_was_forced";
        internal const string JsonMedicalInjuryCounter = "medical_injury_counter";
        internal const string JsonMedicalDepressionCounter = "medical_depression_counter";
        internal const string JsonRelationshipIdolAId = "relationship_idol_a_id";
        internal const string JsonRelationshipIdolBId = "relationship_idol_b_id";
        internal const string JsonRelationshipStatus = "relationship_status";
        internal const string JsonRelationshipDynamic = "relationship_dynamic";
        internal const string JsonRelationshipIsDating = "relationship_is_dating";
        internal const string JsonRelationshipKnownToPlayer = "relationship_known_to_player";
        internal const string JsonRelationshipPreviousStatus = "relationship_previous_status";
        internal const string JsonRelationshipNewStatus = "relationship_new_status";
        internal const string JsonRelationshipRatio = "relationship_ratio";
        internal const string JsonBullyingTargetId = "bullying_target_id";
        internal const string JsonBullyingLeaderId = "bullying_leader_id";
        internal const string JsonBullyingKnownToPlayer = "bullying_known_to_player";
        internal const string JsonGroupId = "group_id";
        internal const string JsonTheaterTitle = "theater_title";
        internal const string JsonTheaterScheduleType = "schedule_type";
        internal const string JsonTheaterAttendance = "attendance";
        internal const string JsonTheaterRevenue = "revenue";
        internal const string JsonTheaterSubscribersDelta = "subscribers_delta";
        internal const string JsonTheaterSubscribersTotal = "subscribers_total";
        internal const string JsonTheaterAverageAttendance = "avg_attendance_7d";
        internal const string JsonTheaterAverageRevenue = "avg_revenue_7d";
        internal const string JsonActivityType = "activity_type";
        internal const string JsonActivityMoneyDelta = "money_delta";
        internal const string JsonActivityFansDelta = "fans_delta";
        internal const string JsonActivityPerIdolEarnings = "per_idol_earnings";
        internal const string JsonActivityStaminaCost = "stamina_cost";
        internal const string JsonActivitySpaHeal = "spa_heal";
        internal const string JsonTourParticipantIdList = "tour_participant_id_list";
        internal const string JsonConcertParticipantIdList = "concert_participant_id_list";
        internal const string JsonStaffedIdolIdList = "staffed_idol_id_list";
        internal const string JsonStaffType = "staff_type";
        internal const string JsonLazyCreatorRuntimeTypeName = "JSONLazyCreator";
        internal const string PatchTokenSetStatus = ".SetStatus.";
        internal const string SourcePatchIdolBirthday = "IdolCareerDiary.Birthday";

        internal static readonly string[] RelatedIdFields = new[]
        {
            "idol_id",
            "idol_a_id",
            "idol_b_id",
            "partner_id",
            "other_id",
            "girl_id",
            "target_id",
            "spy_id",
            "task_girl_id",
            "push_previous_idol_id",
            "push_current_idol_id",
            "clique_leader_id",
            "mentor_id",
            "kohai_id",
            "single_removed_idol_id",
            "show_removed_idol_id",
            "concert_removed_idol_id",
            JsonRelationshipIdolAId,
            JsonRelationshipIdolBId,
            JsonBullyingTargetId,
            JsonBullyingLeaderId
        };

        internal static readonly string[] RelatedIdListFields = new[]
        {
            "single_cast_id_list",
            "single_cast_id_list_before",
            "single_cast_id_list_after",
            "single_cast_id_list_added",
            "single_cast_id_list_removed",
            JsonShowCastIdList,
            "show_cast_id_list_before",
            "show_cast_id_list_after",
            "show_cast_id_list_added",
            "show_cast_id_list_removed",
            JsonTourParticipantIdList,
            JsonConcertParticipantIdList,
            "concert_participant_id_list_before",
            "concert_participant_id_list_after",
            "concert_participant_id_list_added",
            "concert_participant_id_list_removed",
            JsonStaffedIdolIdList,
            "group_member_id_list",
            "clique_member_id_list",
            "clique_member_id_list_before",
            "clique_member_id_list_after",
            "clique_member_id_list_added",
            "clique_member_id_list_removed"
        };
    
        
        internal const string IdImDataCoreImDataCoreApi = "IMDataCore.IMDataCoreApi";
        internal const string IdImDataCoreImDataCoreSession = "IMDataCore.IMDataCoreSession";
        internal const string IdImDataCoreImDataCoreEvent = "IMDataCore.IMDataCoreEvent";
        internal static readonly string TextNamespaceIsEmpty = ModLocalization.Get("TextNamespaceIsEmpty", "Namespace is empty.");
        internal static readonly string TextImDataCoreReturnedAnEmptySession = ModLocalization.Get("TextImDataCoreReturnedAnEmptySession", "IM Data Core returned an empty session.");
        internal static readonly string TextImDataCoreSessionIsUnavailable = ModLocalization.Get("TextImDataCoreSessionIsUnavailable", "IM Data Core session is unavailable.");
        internal static readonly string TextImDataCoreBridgeIsRetrying = ModLocalization.Get("TextImDataCoreBridgeIsRetrying", "IM Data Core bridge is retrying.");
        internal static readonly string TextImDataCorePatchesArePresentButApiAssemblyCouldNotBeResolved = ModLocalization.Get("TextImDataCorePatchesArePresentButApiAssemblyCouldNotBeResolved", "IM Data Core patches are present but API assembly could not be resolved.");
        internal static readonly string TextImDataCoreModIsNotLoaded = ModLocalization.Get("TextImDataCoreModIsNotLoaded", "IM Data Core mod is not loaded.");
        internal static readonly string TextImDataCoreApiTypesWereNotFoundInAssembly = ModLocalization.Get("TextImDataCoreApiTypesWereNotFoundInAssembly", "IM Data Core API types were not found in assembly.");
        internal const string MemberNameIsReady = "IsReady";
        internal const string MemberNameTryRegisterNamespace = "TryRegisterNamespace";
        internal const string MemberNameTryGetCustomJson = "TryGetCustomJson";
        internal const string MemberNameTrySetCustomJson = "TrySetCustomJson";
        internal const string MemberNameTryAppendCustomEvent = "TryAppendCustomEvent";
        internal const string MemberNameTryReadRecentEventsForIdol = "TryReadRecentEventsForIdol";
        internal static readonly string TextImDataCoreApiMethodSignatureMismatch = ModLocalization.Get("TextImDataCoreApiMethodSignatureMismatch", "IM Data Core API method signature mismatch.");
        internal static readonly string TextImDataCoreAppendCustomEventMethodIsUnavailable = ModLocalization.Get("TextImDataCoreAppendCustomEventMethodIsUnavailable", "IM Data Core append-custom-event method is unavailable.");
        internal static readonly string TextBirthdayEventAppendFailedPrefix = ModLocalization.Get("TextBirthdayEventAppendFailedPrefix", "Failed to append birthday event: ");
        internal const string MemberNameEventId = "EventId";
        internal const string MemberNameGameDateKey = "GameDateKey";
        internal const string MemberNameGameDateTime = "GameDateTime";
        internal const string MemberNameIdolId = "IdolId";
        internal const string MemberNameEntityKind = "EntityKind";
        internal const string MemberNameEntityId = "EntityId";
        internal const string MemberNameEventType = "EventType";
        internal const string MemberNameSourcePatch = "SourcePatch";
        internal const string MemberNamePayloadJson = "PayloadJson";
        internal const string MemberNameNamespaceId = "NamespaceId";
        internal static readonly string TextImDataCoreEventPropertySignatureMismatch = ModLocalization.Get("TextImDataCoreEventPropertySignatureMismatch", "IM Data Core event property signature mismatch.");
        internal static readonly string TextImDataCoreBridgeMethodIsUnavailable = ModLocalization.Get("TextImDataCoreBridgeMethodIsUnavailable", "IM Data Core bridge method is unavailable.");
        internal const string IdImUiFrameworkImUiKit = "IMUiFramework.IMUiKit";
        internal static readonly string TextPopupManagerIsNull = ModLocalization.Get("TextPopupManagerIsNull", "PopupManager is null.");
        internal static readonly string TextImUiFrameworkReturnedNullUiObject = ModLocalization.Get("TextImUiFrameworkReturnedNullUiObject", "IM UI Framework returned null UI object.");
        internal static readonly string TextImUiFrameworkReturnedNullStyledButton = ModLocalization.Get("TextImUiFrameworkReturnedNullStyledButton", "IM UI Framework returned null styled button.");
        internal static readonly string TextImUiFrameworkReturnedNullClonedButton = ModLocalization.Get("TextImUiFrameworkReturnedNullClonedButton", "IM UI Framework returned null cloned button.");
        internal static readonly string TextImUiFrameworkBridgeIsRetrying = ModLocalization.Get("TextImUiFrameworkBridgeIsRetrying", "IM UI Framework bridge is retrying.");
        internal static readonly string TextImUiFrameworkPatchesArePresentButApiAssemblyCouldNotBeResolved = ModLocalization.Get("TextImUiFrameworkPatchesArePresentButApiAssemblyCouldNotBeResolved", "IM UI Framework patches are present but API assembly could not be resolved.");
        internal static readonly string TextImUiFrameworkModIsNotLoaded = ModLocalization.Get("TextImUiFrameworkModIsNotLoaded", "IM UI Framework mod is not loaded.");
        internal static readonly string TextImUiFrameworkImUiKitTypeWasNotFoundInAssembly = ModLocalization.Get("TextImUiFrameworkImUiKitTypeWasNotFoundInAssembly", "IM UI Framework IMUiKit type was not found in assembly.");
        internal const string MemberNameInitialize = "Initialize";
        internal const string MemberNameCreateUiObject = "CreateUiObject";
        internal const string MemberNameClearChildren = "ClearChildren";
        internal const string MemberNameRebuildLayout = "RebuildLayout";
        internal const string MemberNameCreateStyledButton = "CreateStyledButton";
        internal const string MemberNameCloneStyledButton = "CloneStyledButton";
        internal static readonly string TextImUiFrameworkApiMethodSignatureMismatch = ModLocalization.Get("TextImUiFrameworkApiMethodSignatureMismatch", "IM UI Framework API method signature mismatch.");
        internal static readonly string TextImUiFrameworkBridgeMethodIsUnavailable = ModLocalization.Get("TextImUiFrameworkBridgeMethodIsUnavailable", "IM UI Framework bridge method is unavailable.");
        internal static readonly string TextImDataCoreErrorPrefix = ModLocalization.Get("TextImDataCoreErrorPrefix", "IMDataCore: ");
        internal static readonly string TextUnknownError = ModLocalization.Get("TextUnknownError", "unknown error");
        internal static readonly string TextImUiFrameworkErrorPrefix = ModLocalization.Get("TextImUiFrameworkErrorPrefix", " | IMUIFramework: ");
        internal static readonly string TextImDataCoreUnknownDependencyResolutionError = ModLocalization.Get("TextImDataCoreUnknownDependencyResolutionError", "IMDataCore unknown dependency resolution error.");
        internal static readonly string TextImUiFrameworkUnknownDependencyResolutionError = ModLocalization.Get("TextImUiFrameworkUnknownDependencyResolutionError", "IMUIFramework unknown dependency resolution error.");
        internal static readonly string TextUnknownDependencyResolutionError = ModLocalization.Get("TextUnknownDependencyResolutionError", "Unknown dependency resolution error.");
        internal static readonly string TextEnsureModImDataCore = ModLocalization.Get("TextEnsureModImDataCore", " Ensure mod 'IMDataCore' (");
        internal static readonly string TextIsInstalledAndEnabled = ModLocalization.Get("TextIsInstalledAndEnabled", ") is installed and enabled.");
        internal static readonly string TextEnsureModImUiFramework = ModLocalization.Get("TextEnsureModImUiFramework", " Ensure mod 'IMUIFramework' (");
        internal static readonly string TextUnableToPersistSelectedEventStatePrefix = ModLocalization.Get("TextUnableToPersistSelectedEventStatePrefix", "Unable to persist selected event state: ");
        internal static readonly string TextRequiredDependenciesAreMissing = ModLocalization.Get("TextRequiredDependenciesAreMissing", "Required dependencies are missing.");
        internal static readonly string TextRetryWindowActive = ModLocalization.Get("TextRetryWindowActive", "Retry window active.");
        internal static readonly string TextImDataCoreNotReady = ModLocalization.Get("TextImDataCoreNotReady", "IM Data Core not ready.");
        internal static readonly string TextCoreNamespaceRegistrationFailedPrefix = ModLocalization.Get("TextCoreNamespaceRegistrationFailedPrefix", "Core namespace registration failed: ");
        internal static readonly string TextImDataCoreSessionInitialized = ModLocalization.Get("TextImDataCoreSessionInitialized", "IM Data Core session initialized.");
        internal static readonly string TextCareerDiaryCloseButtonAnchorNotFoundDeferringButtonPlacement = ModLocalization.Get("TextCareerDiaryCloseButtonAnchorNotFoundDeferringButtonPlacement", "Career Diary close button anchor not found; deferring button placement.");
        internal static readonly string TextCareerDiaryButtonParentUnavailable = ModLocalization.Get("TextCareerDiaryButtonParentUnavailable", "Career Diary button parent unavailable.");
        internal static readonly string TextCareerDiaryPanelTemplateUnavailable = ModLocalization.Get("TextCareerDiaryPanelTemplateUnavailable", "Career Diary panel template unavailable.");
        internal static readonly string TextCareerDiaryDetailPopupTemplateUnavailable = ModLocalization.Get("TextCareerDiaryDetailPopupTemplateUnavailable", "Career Diary detail popup template unavailable.");
        internal static readonly string TextActiveIdolContextUnavailable = ModLocalization.Get("TextActiveIdolContextUnavailable", "Active idol context unavailable.");
        internal static readonly string TextTimelineQueryFailed = ModLocalization.Get("TextTimelineQueryFailed", "Timeline query failed.");
        internal static readonly string TextTourCreated = ModLocalization.Get("TextTourCreated", "Tour Created");
        internal static readonly string TextTourStarted = ModLocalization.Get("TextTourStarted", "Tour Started");
        internal static readonly string TextTourFinished = ModLocalization.Get("TextTourFinished", "Tour Finished");
        internal static readonly string TextTourCancelled = ModLocalization.Get("TextTourCancelled", "Tour Cancelled");
        internal static readonly string TextTourCountryResult = ModLocalization.Get("TextTourCountryResult", "Tour Country Result");
        internal static readonly string TextTourParticipationRecorded = ModLocalization.Get("TextTourParticipationRecorded", "Tour Participation Recorded");
        internal static readonly string TextTour = ModLocalization.Get("TextTour", "Tour #");
        internal static readonly string TextTourStatus = ModLocalization.Get("TextTourStatus", "Tour Status");
        internal const string KeyTourStatus = "tour_status";
        internal static readonly string LabelAction = ModLocalization.Get("LabelAction", "Action");
        internal const string KeyTourLifecycleAction = "tour_lifecycle_action";
        internal static readonly string TextParticipantCount = ModLocalization.Get("TextParticipantCount", "Participant Count");
        internal const string KeyTourParticipantCount = "tour_participant_count";
        internal static readonly string TextParticipants = ModLocalization.Get("TextParticipants", "Participants: ");
        internal static readonly string TextCountryCount = ModLocalization.Get("TextCountryCount", "Country Count");
        internal static readonly string LabelAudience = ModLocalization.Get("LabelAudience", "Audience");
        internal static readonly string TextNewFans = ModLocalization.Get("TextNewFans", "New Fans");
        internal static readonly string TextProductionCost = ModLocalization.Get("TextProductionCost", "Production Cost");
        internal static readonly string LabelProfit = ModLocalization.Get("LabelProfit", "Profit");
        internal const string KeyTourProfit = "tour_profit";
        internal static readonly string TextStartDate = ModLocalization.Get("TextStartDate", "Start Date");
        internal const string KeyTourStartDate = "tour_start_date";
        internal static readonly string TextFinishDate = ModLocalization.Get("TextFinishDate", "Finish Date");
        internal static readonly string LabelCountry = ModLocalization.Get("LabelCountry", "Country");
        internal const string KeyTourCountryCode = "tour_country_code";
        internal static readonly string TextCountryLevel = ModLocalization.Get("TextCountryLevel", "Country Level");
        internal const string KeyTourCountryLevel = "tour_country_level";
        internal static readonly string TextCountryAttendance = ModLocalization.Get("TextCountryAttendance", "Country Attendance");
        internal const string KeyTourCountryAttendance = "tour_country_attendance";
        internal static readonly string TextCountryAudience = ModLocalization.Get("TextCountryAudience", "Country Audience");
        internal const string KeyTourCountryAudience = "tour_country_audience";
        internal static readonly string TextCountryRevenue = ModLocalization.Get("TextCountryRevenue", "Country Revenue");
        internal const string KeyTourCountryRevenue = "tour_country_revenue";
        internal static readonly string TextCountryNewFans = ModLocalization.Get("TextCountryNewFans", "Country New Fans");
        internal const string KeyTourCountryNewFans = "tour_country_new_fans";
        internal static readonly string TextElectionCreated = ModLocalization.Get("TextElectionCreated", "Election Created");
        internal static readonly string TextElectionStarted = ModLocalization.Get("TextElectionStarted", "Election Started");
        internal static readonly string TextElectionFinished = ModLocalization.Get("TextElectionFinished", "Election Finished");
        internal static readonly string TextElectionCancelled = ModLocalization.Get("TextElectionCancelled", "Election Cancelled");
        internal static readonly string TextElectionResultsGenerated = ModLocalization.Get("TextElectionResultsGenerated", "Election Results Generated");
        internal static readonly string TextElectionPlaceAdjusted = ModLocalization.Get("TextElectionPlaceAdjusted", "Election Place Adjusted");
        internal static readonly string TextElection = ModLocalization.Get("TextElection", "Election #");
        internal static readonly string LabelStatus = ModLocalization.Get("LabelStatus", "Status");
        internal static readonly string LabelBroadcast = ModLocalization.Get("LabelBroadcast", "Broadcast");
        internal static readonly string LabelPlace = ModLocalization.Get("LabelPlace", "Place");
        internal const string KeyElectionPlaceBefore = "election_place_before";
        internal const string KeyElectionPlaceAfter = "election_place_after";
        internal static readonly string LabelVotes = ModLocalization.Get("LabelVotes", "Votes");
        internal static readonly string TextFamePoints = ModLocalization.Get("TextFamePoints", "Fame Points");
        internal static readonly string TextResultCount = ModLocalization.Get("TextResultCount", "Ranked Idols");
        internal static readonly string TextConcertCreated = ModLocalization.Get("TextConcertCreated", "Concert Created");
        internal static readonly string TextConcertStarted = ModLocalization.Get("TextConcertStarted", "Concert Started");
        internal static readonly string TextConcertFinished = ModLocalization.Get("TextConcertFinished", "Concert Finished");
        internal static readonly string TextConcertCancelled = ModLocalization.Get("TextConcertCancelled", "Concert Cancelled");
        internal static readonly string TextConcertParticipationRecorded = ModLocalization.Get("TextConcertParticipationRecorded", "Concert Participation Recorded");
        internal static readonly string TextConcertCastUpdated = ModLocalization.Get("TextConcertCastUpdated", "Concert Cast Updated");
        internal static readonly string TextConcertStatusUpdated = ModLocalization.Get("TextConcertStatusUpdated", "Concert Status Updated");
        internal static readonly string TextConcertConfigurationUpdated = ModLocalization.Get("TextConcertConfigurationUpdated", "Concert Configuration Updated");
        internal static readonly string TextConcertCardUsed = ModLocalization.Get("TextConcertCardUsed", "Concert Card Used");
        internal static readonly string TextConcertCrisisDecision = ModLocalization.Get("TextConcertCrisisDecision", "Concert Crisis Decision");
        internal static readonly string TextConcertCrisisApplied = ModLocalization.Get("TextConcertCrisisApplied", "Concert Crisis Applied");
        internal static readonly string TextConcertFinalOutcome = ModLocalization.Get("TextConcertFinalOutcome", "Concert Final Outcome");
        internal const string KeyConcertTitle = "concert_title";
        internal static readonly string TextConcert = ModLocalization.Get("TextConcert", "Concert #");
        internal static readonly string LabelVenue = ModLocalization.Get("LabelVenue", "Venue");
        internal const string KeyConcertVenue = "concert_venue";
        internal const string KeyConcertPreviousStatus = "concert_previous_status";
        internal const string KeyConcertNewStatus = "concert_new_status";
        internal const string KeyConcertStatus = "concert_status";
        internal static readonly string LabelSongs = ModLocalization.Get("LabelSongs", "Songs");
        internal const string KeyConcertSongCount = "concert_song_count";
        internal static readonly string LabelParticipants = ModLocalization.Get("LabelParticipants", "Participants");
        internal const string KeyConcertParticipantCount = "concert_participant_count";
        internal const string KeyConcertParticipantCountBefore = "concert_participant_count_before";
        internal const string KeyConcertParticipantCountAfter = "concert_participant_count_after";
        internal static readonly string TextLineup = ModLocalization.Get("TextLineup", "Lineup: ");
        internal const string KeyConcertParticipantIdListAdded = "concert_participant_id_list_added";
        internal static readonly string TextAddedParticipants = ModLocalization.Get("TextAddedParticipants", "Added Participants: ");
        internal const string KeyConcertParticipantIdListRemoved = "concert_participant_id_list_removed";
        internal static readonly string TextRemovedParticipants = ModLocalization.Get("TextRemovedParticipants", "Removed Participants: ");
        internal const string KeyConcertRemovedIdolId = "concert_removed_idol_id";
        internal static readonly string TextRemovedIdol = ModLocalization.Get("TextRemovedIdol", "Removed Idol: ");
        internal static readonly string TextProjectedAudience = ModLocalization.Get("TextProjectedAudience", "Projected Audience");
        internal const string KeyConcertProjectedAudience = "concert_projected_audience";
        internal static readonly string TextActualAudience = ModLocalization.Get("TextActualAudience", "Actual Audience");
        internal const string KeyConcertActualAudience = "concert_actual_audience";
        internal static readonly string TextProjectedRevenue = ModLocalization.Get("TextProjectedRevenue", "Projected Revenue");
        internal const string KeyConcertProjectedRevenue = "concert_projected_revenue";
        internal static readonly string TextActualRevenue = ModLocalization.Get("TextActualRevenue", "Actual Revenue");
        internal const string KeyConcertActualRevenue = "concert_actual_revenue";
        internal const string KeyConcertProductionCost = "concert_production_cost";
        internal const string KeyConcertProfit = "concert_profit";
        internal static readonly string TextTicketPrice = ModLocalization.Get("TextTicketPrice", "Ticket Price");
        internal const string KeyConcertTicketPrice = "concert_ticket_price";
        internal const string KeyConcertTicketPriceBefore = "concert_ticket_price_before";
        internal const string KeyConcertTicketPriceAfter = "concert_ticket_price_after";
        internal static readonly string TextCardType = ModLocalization.Get("TextCardType", "Card Type");
        internal const string KeyCardType = "card_type";
        internal static readonly string TextCardLevel = ModLocalization.Get("TextCardLevel", "Card Level");
        internal const string KeyCardLevel = "card_level";
        internal static readonly string TextCardEffect = ModLocalization.Get("TextCardEffect", "Card Effect");
        internal const string KeyCardEffectValue = "card_effect_value";
        internal static readonly string TextCardsRemaining = ModLocalization.Get("TextCardsRemaining", "Cards Remaining");
        internal const string KeyCardsBefore = "cards_before";
        internal const string KeyCardsAfter = "cards_after";
        internal static readonly string LabelCrisis = ModLocalization.Get("LabelCrisis", "Crisis");
        internal const string KeyAccidentTitle = "accident_title";
        internal static readonly string LabelChoice = ModLocalization.Get("LabelChoice", "Choice");
        internal const string KeyChoiceType = "choice_type";
        internal static readonly string LabelResult = ModLocalization.Get("LabelResult", "Result");
        internal const string KeyResultType = "result_type";
        internal static readonly string TextHypeDelta = ModLocalization.Get("TextHypeDelta", "Hype Change");
        internal const string KeyHypeDeltaApplied = "hype_delta_applied";
        internal const string KeyResultHypeDelta = "result_hype_delta";
        internal static readonly string TextExpectedHypeDelta = ModLocalization.Get("TextExpectedHypeDelta", "Expected Hype Change");
        internal const string KeyExpectedHypeDelta = "expected_hype_delta";
        internal static readonly string TextSuccessChance = ModLocalization.Get("TextSuccessChance", "Success Chance");
        internal const string KeyAccidentSuccessChance = "accident_success_chance";
        internal static readonly string TextFinalRevenue = ModLocalization.Get("TextFinalRevenue", "Final Revenue");
        internal const string KeyActualRevenue = "actual_revenue";
        internal static readonly string TextFinalProfit = ModLocalization.Get("TextFinalProfit", "Final Profit");
        internal const string KeyActualProfit = "actual_profit";
        internal static readonly string TextIdolPayout = ModLocalization.Get("TextIdolPayout", "Total Idol Concert Earnings");
        internal const string KeyIdolPayoutTotal = "idol_payout_total";
        internal static readonly string LabelAccidents = ModLocalization.Get("LabelAccidents", "Accidents");
        internal const string KeyUsedAccidentTitles = "used_accident_titles";
        internal const string KeyConcertFinishDate = "concert_finish_date";
        internal static readonly string LabelDate = ModLocalization.Get("LabelDate", "Date");
        internal const string KeyEventDate = "event_date";
        internal static readonly string TextStaffHired = ModLocalization.Get("TextStaffHired", "Staff Hired");
        internal static readonly string TextStaffFired = ModLocalization.Get("TextStaffFired", "Staff Fired");
        internal static readonly string TextStaffFiredWithSeverance = ModLocalization.Get("TextStaffFiredWithSeverance", "Staff Fired With Severance");
        internal static readonly string TextStaffLeveledUp = ModLocalization.Get("TextStaffLeveledUp", "Staff Leveled Up");
        internal const string KeyStaffName = "staff_name";
        internal static readonly string LabelStaff = ModLocalization.Get("LabelStaff", "Staff");
        internal static readonly string LabelCreditedStaff = ModLocalization.Get("LabelCreditedStaff", "Credited Staff");
        internal static readonly string LabelAssignedBy = ModLocalization.Get("LabelAssignedBy", "Assigned By");
        internal static readonly string LabelCompletedBy = ModLocalization.Get("LabelCompletedBy", "Completed By");
        internal static readonly string TextHiredBy = ModLocalization.Get("TextHiredBy", "Hired by: ");
        internal static readonly string TextCreditedTo = ModLocalization.Get("TextCreditedTo", "Credited to: ");
        internal static readonly string TextAssignedAndCompletedBy = ModLocalization.Get("TextAssignedAndCompletedBy", "Assigned and completed by: ");
        internal static readonly string TextTrainingPracticeCompleted = ModLocalization.Get("TextTrainingPracticeCompleted", "Training Practice Completed");
        internal static readonly string TextStaminaRecoveryCompleted = ModLocalization.Get("TextStaminaRecoveryCompleted", "Stamina Recovery Completed");
        internal static readonly string TextSinglePreparationCompleted = ModLocalization.Get("TextSinglePreparationCompleted", "Single Preparation Completed");
        internal static readonly string TextShowPreparationCompleted = ModLocalization.Get("TextShowPreparationCompleted", "Show Preparation Completed");
        internal static readonly string TextConcertPreparationCompleted = ModLocalization.Get("TextConcertPreparationCompleted", "Concert Preparation Completed");
        internal static readonly string TextEventPreparationCompleted = ModLocalization.Get("TextEventPreparationCompleted", "Event Preparation Completed");
        internal static readonly string TextWorkStage = ModLocalization.Get("TextWorkStage", "Stage: ");
        internal static readonly string LabelRole = ModLocalization.Get("LabelRole", "Role");
        internal const string KeyStaffAction = "staff_action";
        internal static readonly string LabelSalary = ModLocalization.Get("LabelSalary", "Salary");
        internal const string KeyStaffSalary = "staff_salary";
        internal static readonly string LabelLevel = ModLocalization.Get("LabelLevel", "Level");
        internal const string KeyStaffLevelBefore = "staff_level_before";
        internal const string KeyStaffLevelAfter = "staff_level_after";
        internal static readonly string TextSeveranceCost = ModLocalization.Get("TextSeveranceCost", "Severance Cost");
        internal const string KeySeveranceCost = "severance_cost";
        internal static readonly string TextMoneyChange = ModLocalization.Get("TextMoneyChange", "Money Change");
        internal static readonly string LabelRoom = ModLocalization.Get("LabelRoom", "Room");
        internal const string KeyRoomType = "room_type";
        internal const string KeyFireForced = "fire_forced";
        internal static readonly string TextForcedFiring = ModLocalization.Get("TextForcedFiring", "Forced Firing: ");
        internal static readonly string TextLoanAdded = ModLocalization.Get("TextLoanAdded", "Loan Added");
        internal static readonly string TextLoanInitialized = ModLocalization.Get("TextLoanInitialized", "Loan Initialized");
        internal static readonly string TextLoanPaidOff = ModLocalization.Get("TextLoanPaidOff", "Loan Paid Off");
        internal static readonly string TextLoan = ModLocalization.Get("TextLoan", "Loan #");
        internal static readonly string TextLoanId = ModLocalization.Get("TextLoanId", "Loan ID");
        internal const string KeyLoanId = "loan_id";
        internal const string KeyLoanLifecycleAction = "loan_lifecycle_action";
        internal static readonly string TextLoanType = ModLocalization.Get("TextLoanType", "Lender");
        internal const string KeyLoanType = "loan_type";
        internal static readonly string TextLoanDuration = ModLocalization.Get("TextLoanDuration", "Repayment Term");
        internal const string KeyLoanDuration = "loan_duration";
        internal static readonly string TextLoanActive = ModLocalization.Get("TextLoanActive", "Loan Active");
        internal const string KeyLoanActiveBefore = "loan_active_before";
        internal const string KeyLoanActiveAfter = "loan_active_after";
        internal static readonly string TextLoanAmount = ModLocalization.Get("TextLoanAmount", "Loan Amount");
        internal const string KeyLoanAmount = "loan_amount";
        internal static readonly string TextLoanPaymentPerWeek = ModLocalization.Get("TextLoanPaymentPerWeek", "Loan Payment Per Week");
        internal const string KeyLoanPaymentPerWeek = "loan_payment_per_week";
        internal static readonly string TextLoanInterestRate = ModLocalization.Get("TextLoanInterestRate", "Loan Interest Rate");
        internal const string KeyLoanInterestRate = "loan_interest_rate";
        internal const string KeyLoanStartDate = "loan_start_date";
        internal const string KeyLoanEndDate = "loan_end_date";
        internal static readonly string TextLoanDebt = ModLocalization.Get("TextLoanDebt", "Loan Debt");
        internal const string KeyLoanDebtBefore = "loan_debt_before";
        internal const string KeyLoanDebtAfter = "loan_debt_after";
        internal static readonly string TextLoanCanPayOff = ModLocalization.Get("TextLoanCanPayOff", "Loan Can Pay Off");
        internal const string KeyLoanCanPayOffAfter = "loan_can_pay_off_after";
        internal static readonly string TextLoanInDevelopment = ModLocalization.Get("TextLoanInDevelopment", "Loan In Development");
        internal const string KeyLoanInDevelopmentAfter = "loan_in_development_after";
        internal static readonly string TextLoanDaysToDevelop = ModLocalization.Get("TextLoanDaysToDevelop", "Loan Days To Develop");
        internal const string KeyLoanDaysToDevelop = "loan_days_to_develop";
        internal static readonly string TextActiveLoanCount = ModLocalization.Get("TextActiveLoanCount", "Active Loan Count");
        internal const string KeyLoanCountActive = "loan_count_active";
        internal static readonly string TextTotalLoanCount = ModLocalization.Get("TextTotalLoanCount", "Total Loan Count");
        internal const string KeyLoanCountTotal = "loan_count_total";
        internal static readonly string TextTotalLoanPaymentPerWeek = ModLocalization.Get("TextTotalLoanPaymentPerWeek", "Total Loan Payment Per Week");
        internal const string KeyLoanTotalPaymentPerWeek = "loan_total_payment_per_week";
        internal static readonly string TextTotalDebt = ModLocalization.Get("TextTotalDebt", "Total Debt");
        internal const string KeyLoanTotalDebt = "loan_total_debt";
        internal const string KeyMoneyBefore = "money_before";
        internal const string KeyMoneyAfter = "money_after";
        internal static readonly string TextMoneyOnHand = ModLocalization.Get("TextMoneyOnHand", "Money On Hand");
        internal static readonly string TextBankruptcyDangerSet = ModLocalization.Get("TextBankruptcyDangerSet", "Bankruptcy Danger Set");
        internal static readonly string TextBankruptcyCheck = ModLocalization.Get("TextBankruptcyCheck", "Bankruptcy Check");
        internal static readonly string TextBankruptcy = ModLocalization.Get("TextBankruptcy", "Bankruptcy");
        internal static readonly string TextRequestedValue = ModLocalization.Get("TextRequestedValue", "Requested Value");
        internal const string KeyRequestedValue = "requested_value";
        internal static readonly string TextBankruptcyDanger = ModLocalization.Get("TextBankruptcyDanger", "Bankruptcy Danger");
        internal const string KeyBankruptcyDangerBefore = "bankruptcy_danger_before";
        internal const string KeyBankruptcyDangerAfter = "bankruptcy_danger_after";
        internal static readonly string TextBankruptcyDate = ModLocalization.Get("TextBankruptcyDate", "Bankruptcy Date");
        internal const string KeyBankruptcyDateBefore = "bankruptcy_date_before";
        internal const string KeyBankruptcyDateAfter = "bankruptcy_date_after";
        internal static readonly string TextBankruptcyDaysRemaining = ModLocalization.Get("TextBankruptcyDaysRemaining", "Bankruptcy Days Remaining");
        internal const string KeyBankruptcyDaysRemainingBefore = "bankruptcy_days_remaining_before";
        internal const string KeyBankruptcyDaysRemainingAfter = "bankruptcy_days_remaining_after";
        internal const string KeyTotalDebtBefore = "total_debt_before";
        internal const string KeyTotalDebtAfter = "total_debt_after";
        internal static readonly string TextBailoutUsed = ModLocalization.Get("TextBailoutUsed", "Bailout Used");
        internal const string KeyBailoutUsedBefore = "bailout_used_before";
        internal const string KeyBailoutUsedAfter = "bailout_used_after";
        internal static readonly string TextStoryRecruitUsed = ModLocalization.Get("TextStoryRecruitUsed", "Story Recruit Used");
        internal const string KeyStoryRecruitUsedBefore = "story_recruit_used_before";
        internal const string KeyStoryRecruitUsedAfter = "story_recruit_used_after";
        internal static readonly string TextBankruptcyGameOverUsed = ModLocalization.Get("TextBankruptcyGameOverUsed", "Bankruptcy Game Over Used");
        internal const string KeyGameOverBankruptcyUsedBefore = "game_over_bankruptcy_used_before";
        internal const string KeyGameOverBankruptcyUsedAfter = "game_over_bankruptcy_used_after";
        internal static readonly string TextTriggeredDialogue = ModLocalization.Get("TextTriggeredDialogue", "Triggered Story Event");
        internal const string KeyTriggeredDialogue = "triggered_dialogue";
        internal const string CodeStoryBankruptcyBailOut = "bankruptcy_bail_out";
        internal const string CodeStoryRecruit = "story_recruit";
        internal const string CodeStoryGameOverBankruptcy = "game_over_bankruptcy";
        internal const string CodeStoryFirstScandalPoints = "first_scandal_points";
        internal const string CodeStoryGameOverWarningScandalPoints = "game_over_warning_scandal_points";
        internal const string CodeStoryScandalPointsParents = "scandal_points_parents";
        internal const string CodeStoryGameOverScandalPoints = "game_over_scandal_points";
        internal static readonly string TextCafeCreated = ModLocalization.Get("TextCafeCreated", "Cafe Created");
        internal static readonly string TextCafeDestroyed = ModLocalization.Get("TextCafeDestroyed", "Cafe Destroyed");
        internal static readonly string TextCafeDailyResult = ModLocalization.Get("TextCafeDailyResult", "Cafe Daily Result");
        internal static readonly string TextCafe = ModLocalization.Get("TextCafe", "Cafe #");
        internal static readonly string TextCafeId = ModLocalization.Get("TextCafeId", "Cafe ID");
        internal const string KeyCafeId = "cafe_id";
        internal const string KeyCafeLifecycleAction = "cafe_lifecycle_action";
        internal const string KeyCafeTitle = "cafe_title";
        internal static readonly string TextGroupId = ModLocalization.Get("TextGroupId", "Group ID");
        internal const string KeyRoomTheaterId = "room_theater_id";
        internal static readonly string TextRoomTheaterId = ModLocalization.Get("TextRoomTheaterId", "Room Theater ID");
        internal const string KeyWaitStaffCount = "wait_staff_count";
        internal static readonly string TextWaitStaffCount = ModLocalization.Get("TextWaitStaffCount", "Wait Staff Count");
        internal const string KeyWorkingStaffCount = "working_staff_count";
        internal static readonly string TextWorkingStaffCount = ModLocalization.Get("TextWorkingStaffCount", "Working Staff Count");
        internal const string KeyCafePriority = "cafe_priority";
        internal static readonly string TextCafePriority = ModLocalization.Get("TextCafePriority", "Cafe Focus");
        internal const string KeyStaffPriority = "staff_priority";
        internal static readonly string TextStaffPriority = ModLocalization.Get("TextStaffPriority", "Staff Assignment Priority");
        internal const string KeyMenuSummary = "menu_summary";
        internal static readonly string TextMenuSummary = ModLocalization.Get("TextMenuSummary", "Weekly Menu");
        internal static readonly string TextMenuDayEntry = ModLocalization.Get("TextMenuDayEntry", "{0}: {1}");
        internal static readonly string TextUnknownDishFormat = ModLocalization.Get("TextUnknownDishFormat", "Dish #{0}");
        internal static readonly string TextNoCafeDishesSet = ModLocalization.Get("TextNoCafeDishesSet", "No dishes set");
        internal static readonly string TextWeekdayMonday = ModLocalization.Get("TextWeekdayMonday", "Monday");
        internal static readonly string TextWeekdayTuesday = ModLocalization.Get("TextWeekdayTuesday", "Tuesday");
        internal static readonly string TextWeekdayWednesday = ModLocalization.Get("TextWeekdayWednesday", "Wednesday");
        internal static readonly string TextWeekdayThursday = ModLocalization.Get("TextWeekdayThursday", "Thursday");
        internal static readonly string TextWeekdayFriday = ModLocalization.Get("TextWeekdayFriday", "Friday");
        internal static readonly string TextWeekdaySaturday = ModLocalization.Get("TextWeekdaySaturday", "Saturday");
        internal static readonly string TextWeekdaySunday = ModLocalization.Get("TextWeekdaySunday", "Sunday");
        internal const int WeekdayIndexMonday = 0;
        internal const int WeekdayIndexTuesday = 1;
        internal const int WeekdayIndexWednesday = 2;
        internal const int WeekdayIndexThursday = 3;
        internal const int WeekdayIndexFriday = 4;
        internal const int WeekdayIndexSaturday = 5;
        internal const int WeekdayIndexSunday = 6;
        internal const int CafeMenuEntryFieldCount = 2;
        internal const int CafeMenuDayIndexField = 0;
        internal const int CafeMenuDishIdField = 1;
        internal const string KeyLifecycleDate = "lifecycle_date";
        internal const string KeyDishId = "dish_id";
        internal static readonly string TextDishId = ModLocalization.Get("TextDishId", "Dish ID");
        internal const string KeyDishType = "dish_type";
        internal static readonly string TextDishType = ModLocalization.Get("TextDishType", "Dish Type");
        internal const string KeyDishTitle = "dish_title";
        internal static readonly string TextDishTitle = ModLocalization.Get("TextDishTitle", "Dish Title");
        internal const string KeyCafeProfit = "profit";
        internal static readonly string TextCafeProfit = ModLocalization.Get("TextCafeProfit", "Cafe Profit");
        internal const string KeyFanType = "fan_type";
        internal static readonly string TextFanType = ModLocalization.Get("TextFanType", "Target Fan Segment");
        internal const string KeyStaffedIdolCount = "staffed_idol_count";
        internal static readonly string TextStaffedIdolCount = ModLocalization.Get("TextStaffedIdolCount", "Staffed Idol Count");
        internal static readonly string TextStaffedIdols = ModLocalization.Get("TextStaffedIdols", "Staffed Idols: ");
        internal const string KeyResultDate = "result_date";
        internal const string KeyTotalMoneyBefore = "total_money_before";
        internal const string KeyTotalMoneyAfter = "total_money_after";
        internal const string KeyTotalMoneyDelta = "total_money_delta";
        internal static readonly string TextMentorshipStarted = ModLocalization.Get("TextMentorshipStarted", "Mentorship Started");
        internal static readonly string TextMentorshipEnded = ModLocalization.Get("TextMentorshipEnded", "Mentorship Ended");
        internal static readonly string TextMentorshipWeeklyTick = ModLocalization.Get("TextMentorshipWeeklyTick", "Mentorship Weekly Tick");
        internal static readonly string TextMentorship = ModLocalization.Get("TextMentorship", "Mentorship");
        internal const string KeyMentorshipAction = "mentorship_action";
        internal const string KeyMentorId = "mentor_id";
        internal const string KeyKohaiId = "kohai_id";
        internal static readonly string TextMentor = ModLocalization.Get("TextMentor", "Mentor: ");
        internal static readonly string TextKohai = ModLocalization.Get("TextKohai", "Kohai: ");
        internal const string KeyActiveMentorCount = "active_mentor_count";
        internal static readonly string TextActiveMentorCount = ModLocalization.Get("TextActiveMentorCount", "Active Mentor Count");
        internal const string KeySenpaiToKohaiRatioBefore = "senpai_to_kohai_ratio_before";
        internal const string KeySenpaiToKohaiRatioAfter = "senpai_to_kohai_ratio_after";
        internal static readonly string TextSenpaiToKohaiRatio = ModLocalization.Get("TextSenpaiToKohaiRatio", "Senpai -> Kohai Ratio");
        internal const string KeyKohaiToSenpaiRatioBefore = "kohai_to_senpai_ratio_before";
        internal const string KeyKohaiToSenpaiRatioAfter = "kohai_to_senpai_ratio_after";
        internal static readonly string TextKohaiToSenpaiRatio = ModLocalization.Get("TextKohaiToSenpaiRatio", "Kohai -> Senpai Ratio");
        internal const string KeyMentorPairsSummary = "mentor_pairs_summary";
        internal static readonly string TextMentorPairsSummary = ModLocalization.Get("TextMentorPairsSummary", "Active Mentorships");
        internal static readonly string TextMentorPairFormat = ModLocalization.Get("TextMentorPairFormat", "{0} -> {1}");
        internal const int MentorPairEntryFieldCount = 2;
        internal const int MentorPairMentorIdField = 0;
        internal const int MentorPairKohaiIdField = 1;
        internal static readonly string TextScandalCheck = ModLocalization.Get("TextScandalCheck", "Scandal Check");
        internal const string KeyTestGameOver = "test_go";
        internal static readonly string TextTestGameOver = ModLocalization.Get("TextTestGameOver", "Test Game Over");
        internal const string KeyScandalPointsTotalBefore = "scandal_points_total_before";
        internal const string KeyScandalPointsTotalAfter = "scandal_points_total_after";
        internal const string KeyScandalThreshold = "scandal_threshold";
        internal static readonly string TextScandalThreshold = ModLocalization.Get("TextScandalThreshold", "Scandal Threshold");
        internal const string KeyFirstScandalUsedBefore = "first_scandal_used_before";
        internal const string KeyFirstScandalUsedAfter = "first_scandal_used_after";
        internal static readonly string TextFirstScandalUsed = ModLocalization.Get("TextFirstScandalUsed", "First Scandal Used");
        internal const string KeyWarningUsedBefore = "warning_used_before";
        internal const string KeyWarningUsedAfter = "warning_used_after";
        internal static readonly string TextWarningUsed = ModLocalization.Get("TextWarningUsed", "Warning Used");
        internal const string KeyParentsUsedBefore = "parents_used_before";
        internal const string KeyParentsUsedAfter = "parents_used_after";
        internal static readonly string TextParentsUsed = ModLocalization.Get("TextParentsUsed", "Parents Used");
        internal const string KeyGameOverUsedBefore = "game_over_used_before";
        internal const string KeyGameOverUsedAfter = "game_over_used_after";
        internal static readonly string TextGameOverUsed = ModLocalization.Get("TextGameOverUsed", "Game Over Used");
        internal const string KeyScandalParentCooldownBefore = "scandal_parent_cooldown_before";
        internal const string KeyScandalParentCooldownAfter = "scandal_parent_cooldown_after";
        internal static readonly string TextScandalParentCooldown = ModLocalization.Get("TextScandalParentCooldown", "Scandal Parent Cooldown");
        internal const string KeyAuditionFailureBefore = "audition_failure_before";
        internal const string KeyAuditionFailureAfter = "audition_failure_after";
        internal static readonly string TextAuditionFailure = ModLocalization.Get("TextAuditionFailure", "Audition Failure");
        internal const string KeyActiveIdolCountBefore = "active_idol_count_before";
        internal const string KeyActiveIdolCountAfter = "active_idol_count_after";
        internal static readonly string TextActiveIdolCount = ModLocalization.Get("TextActiveIdolCount", "Active Idol Count");
        internal static readonly string TextStoryRouteLocked = ModLocalization.Get("TextStoryRouteLocked", "Story Route Locked");
        internal const string KeyRouteBefore = "route_before";
        internal const string KeyRouteAfter = "route_after";
        internal const string KeyActiveTaskCountBefore = "active_task_count_before";
        internal const string KeyActiveTaskCountAfter = "active_task_count_after";
        internal static readonly string TextActiveTaskCount = ModLocalization.Get("TextActiveTaskCount", "Active Task Count");
        internal const string KeyRemovedTaskCount = "removed_task_count";
        internal static readonly string TextRemovedTaskCount = ModLocalization.Get("TextRemovedTaskCount", "Removed Task Count");
        internal const string KeyRemovedTaskCustomList = "removed_task_custom_list";
        internal static readonly string TextRemovedTaskCustomList = ModLocalization.Get("TextRemovedTaskCustomList", "Removed Task Customs");
        internal static readonly string TextSummerGamesFinalized = ModLocalization.Get("TextSummerGamesFinalized", "Summer Games Finalized");
        internal static readonly string TextSummerGames = ModLocalization.Get("TextSummerGames", "Summer Games");
        internal const string KeySelectedSingleId = "selected_single_id";
        internal static readonly string TextSelectedSingleId = ModLocalization.Get("TextSelectedSingleId", "Selected Single ID");
        internal const string KeyGenreId = "genre_id";
        internal static readonly string TextGenreId = ModLocalization.Get("TextGenreId", "Genre ID");
        internal const string KeyLyricsId = "lyrics_id";
        internal static readonly string TextLyricsId = ModLocalization.Get("TextLyricsId", "Lyrics ID");
        internal const string KeyChoreographyId = "choreography_id";
        internal static readonly string TextChoreographyId = ModLocalization.Get("TextChoreographyId", "Choreography ID");
        internal const string KeyGenreCost = "genre_cost";
        internal static readonly string TextGenreCost = ModLocalization.Get("TextGenreCost", "Genre Cost");
        internal const string KeyLyricsCost = "lyrics_cost";
        internal static readonly string TextLyricsCost = ModLocalization.Get("TextLyricsCost", "Lyrics Cost");
        internal const string KeyChoreographyCost = "choreography_cost";
        internal static readonly string TextChoreographyCost = ModLocalization.Get("TextChoreographyCost", "Choreography Cost");
        internal const string KeyTotalCost = "total_cost";
        internal static readonly string TextTotalCost = ModLocalization.Get("TextTotalCost", "Total Cost");
        internal const string KeyWasFinalizedBefore = "was_finalized_before";
        internal const string KeyIsFinalizedAfter = "is_finalized_after";
        internal static readonly string TextFinalized = ModLocalization.Get("TextFinalized", "Finalized");
        internal const string KeyVocalPointsBefore = "vocal_points_before";
        internal const string KeyVocalPointsAfter = "vocal_points_after";
        internal const string KeyVocalPointsDelta = "vocal_points_delta";
        internal static readonly string TextVocalPoints = ModLocalization.Get("TextVocalPoints", "Vocal Points");
        internal const string KeyPlayerPointsDelta = "player_points_delta";
        internal static readonly string TextPlayerPoints = ModLocalization.Get("TextPlayerPoints", "Player Points");
        internal const string KeyDancePointsBefore = "dance_points_before";
        internal const string KeyDancePointsAfter = "dance_points_after";
        internal const string KeyDancePointsDelta = "dance_points_delta";
        internal static readonly string TextDancePoints = ModLocalization.Get("TextDancePoints", "Dance Points");
        internal const string KeyHappyMemberCount = "happy_member_count";
        internal static readonly string TextHappyMemberCount = ModLocalization.Get("TextHappyMemberCount", "Happy Member Count");
        internal static readonly string TextRandomEventStarted = ModLocalization.Get("TextRandomEventStarted", "Random Event Started");
        internal static readonly string TextRandomEventConcluded = ModLocalization.Get("TextRandomEventConcluded", "Random Event Concluded");
        internal const string KeyRandomEventTitle = "random_event_title";
        internal const string KeyRandomEventId = "random_event_id";
        internal static readonly string TextEventState = ModLocalization.Get("TextEventState", "Event Progress");
        internal const string KeyRandomEventStateBefore = "random_event_state_before";
        internal const string KeyRandomEventStateAfter = "random_event_state_after";
        internal const string KeyRandomEventState = "random_event_state";
        internal const string KeyReplyText = "reply_text";
        internal const string KeyReplyDescription = "reply_description";
        internal static readonly string LabelEffects = ModLocalization.Get("LabelEffects", "Effects");
        internal const string KeyReplyEffectSummary = "reply_effect_summary";
        internal const string KeyReplyEffectEntries = "reply_effect_entries";
        internal const string KeyReplyEffectTarget = "target";
        internal const string KeyReplyEffectParameter = "parameter";
        internal const string KeyReplyEffectFormula = "formula";
        internal const string KeyReplyEffectSpecial = "special";
        internal static readonly string TextRandomEffectResourceChange = ModLocalization.Get("TextRandomEffectResourceChange", "{0}: {1}");
        internal static readonly string TextRandomEffectActorParameterChange = ModLocalization.Get("TextRandomEffectActorParameterChange", "{0}: {1} {2}");
        internal static readonly string TextRandomEffectFanOpinionChange = ModLocalization.Get("TextRandomEffectFanOpinionChange", "{0} Audience Opinion: {1}");
        internal static readonly string TextRandomEffectTriggeredStoryEvent = ModLocalization.Get("TextRandomEffectTriggeredStoryEvent", "Triggers Story Event: {0}");
        internal static readonly string TextRandomEffectAddedTask = ModLocalization.Get("TextRandomEffectAddedTask", "Adds Task: {0}");
        internal static readonly string TextRandomEffectAddedFollowUpTask = ModLocalization.Get("TextRandomEffectAddedFollowUpTask", "Adds Follow-Up Task: {0}");
        internal static readonly string TextRandomEffectSetPolicy = ModLocalization.Get("TextRandomEffectSetPolicy", "Sets Policy: {0}");
        internal const string CodeRandomEffectTargetResource = "resource";
        internal const string CodeRandomEffectTargetFans = "fans";
        internal const string CodeRandomEffectTargetMeta = "meta";
        internal const string CodeRandomEffectTargetGroup = "group";
        internal const string CodeRandomEffectParameterStartDialogue = "start_dialogue";
        internal const string CodeRandomEffectParameterStartDialogueSameActor = "start_dialogue_same_actor";
        internal const string CodeRandomEffectParameterAddTask = "add_task";
        internal const string CodeRandomEffectParameterAddTaskSub = "add_task_sub";
        internal const string CodeRandomEffectParameterAddTaskContinue = "add_task_continue";
        internal const string CodeRandomEffectParameterAddTaskContinueSub = "add_task_continue_sub";
        internal const string CodeRandomEffectParameterSetPolicy = "set_policy";
        internal const string CodeRandomEffectActorParameterPrefix = "param ";
        internal const int ReplyEffectEntryMinimumFieldCount = 4;
        internal const int ReplyEffectTargetField = 0;
        internal const int ReplyEffectParameterField = 1;
        internal const int ReplyEffectFormulaField = 2;
        internal const int ReplyEffectSpecialField = 3;
        internal const int ActorSummaryFieldCount = 4;
        internal const int ActorSummaryKindField = 0;
        internal const int ActorSummaryIdField = 1;
        internal const int ActorSummaryTokenField = 2;
        internal const int ActorSummaryDisplayNameField = 3;
        internal const string KeyActorsSummary = "actors_summary";
        internal static readonly string TextInvolvedIdols = ModLocalization.Get("TextInvolvedIdols", "Involved Idols: ");
        internal static readonly string TextFansChange = ModLocalization.Get("TextFansChange", "Fans Change");
        internal static readonly string TextFameChange = ModLocalization.Get("TextFameChange", "Fame Change");
        internal const string KeyFameDelta = "fame_delta";
        internal static readonly string TextBuzzChange = ModLocalization.Get("TextBuzzChange", "Buzz Change");
        internal const string KeyBuzzDelta = "buzz_delta";
        internal static readonly string TextSubstoryStarted = ModLocalization.Get("TextSubstoryStarted", "Substory Started");
        internal static readonly string TextSubstoryDelayed = ModLocalization.Get("TextSubstoryDelayed", "Substory Delayed");
        internal static readonly string TextSubstoryCompleted = ModLocalization.Get("TextSubstoryCompleted", "Substory Completed");
        internal const string KeySubstoryParentId = "substory_parent_id";
        internal const string KeySubstoryParentDisplayName = "substory_parent_display_name";
        internal const string KeySubstoryId = "substory_id";
        internal const string KeySubstoryDisplayName = "substory_display_name";
        internal static readonly string TextSubstoryType = ModLocalization.Get("TextSubstoryType", "Story Event Type");
        internal const string KeySubstoryType = "substory_type";
        internal const string KeySubstoryLifecycleAction = "substory_lifecycle_action";
        internal static readonly string TextParentStory = ModLocalization.Get("TextParentStory", "Parent Story");
        internal static readonly string TextScheduledTime = ModLocalization.Get("TextScheduledTime", "Scheduled Time");
        internal const string KeyScheduledLaunchTime = "scheduled_launch_time";
        internal static readonly string TextQueueCount = ModLocalization.Get("TextQueueCount", "Ready Substories");
        internal const string KeyQueueCountBefore = "queue_count_before";
        internal const string KeyQueueCountAfter = "queue_count_after";
        internal static readonly string TextDelayedQueue = ModLocalization.Get("TextDelayedQueue", "Delayed Substories");
        internal const string KeyDelayedQueueCountBefore = "delayed_queue_count_before";
        internal const string KeyDelayedQueueCountAfter = "delayed_queue_count_after";
        internal static readonly string TextTaskCompleted = ModLocalization.Get("TextTaskCompleted", "Task Completed");
        internal static readonly string TextTaskFailed = ModLocalization.Get("TextTaskFailed", "Task Failed");
        internal static readonly string TextTaskClosed = ModLocalization.Get("TextTaskClosed", "Task Closed");
        internal static readonly string TextTaskAdded = ModLocalization.Get("TextTaskAdded", "Task Added");
        internal const string KeyTaskCustom = "task_custom";
        internal const string KeyTaskTitle = "task_title";
        internal const string KeyTaskDescription = "task_description";
        internal const string KeyTaskType = "task_type";
        internal static readonly string TextTaskType = ModLocalization.Get("TextTaskType", "Task Category");
        internal static readonly string TextTaskDescription = ModLocalization.Get("TextTaskDescription", "Task Description");
        internal static readonly string TextTaskSummary = ModLocalization.Get("TextTaskSummary", "Task Summary");
        internal static readonly string TextTaskSummaryTypePart = ModLocalization.Get("TextTaskSummaryTypePart", "category {0}");
        internal static readonly string TextTaskSummaryGoalPart = ModLocalization.Get("TextTaskSummaryGoalPart", "goal {0}");
        internal static readonly string TextTaskSummaryRoutePart = ModLocalization.Get("TextTaskSummaryRoutePart", "{0} route");
        internal static readonly string TextTaskSummarySkillPart = ModLocalization.Get("TextTaskSummarySkillPart", "requires {0}");
        internal static readonly string TextTaskSummaryIdolPart = ModLocalization.Get("TextTaskSummaryIdolPart", "assigned to {0}");
        internal static readonly string TextTaskSummaryAgentPart = ModLocalization.Get("TextTaskSummaryAgentPart", "handled by {0}");
        internal static readonly string TextTaskSummaryStoryPart = ModLocalization.Get("TextTaskSummaryStoryPart", "story {0}");
        internal static readonly string LabelGoal = ModLocalization.Get("LabelGoal", "Goal");
        internal const string KeyTaskGoal = "task_goal";
        internal static readonly string LabelRoute = ModLocalization.Get("LabelRoute", "Route");
        internal const string KeyTaskRoute = "route";
        internal const string KeyTaskRouteLegacy = "Route";
        internal const string KeyTaskSubstory = "task_substory";
        internal static readonly string LabelSkill = ModLocalization.Get("LabelSkill", "Skill");
        internal const string KeyTaskSkill = "task_skill";
        internal static readonly string LabelAgent = ModLocalization.Get("LabelAgent", "Agent");
        internal const string KeyTaskAgentName = "task_agent_name";
        internal const string KeyTaskGirlId = "task_girl_id";
        internal static readonly string TextAssignedIdol = ModLocalization.Get("TextAssignedIdol", "Assigned Idol: ");
        internal static readonly string LabelFulfilled = ModLocalization.Get("LabelFulfilled", "Fulfilled");
        internal const string KeyFulfilledBefore = "fulfilled_before";
        internal const string KeyFulfilledAfter = "fulfilled_after";
        internal static readonly string LabelActive = ModLocalization.Get("LabelActive", "Active");
        internal const string KeyActiveBefore = "active_before";
        internal const string KeyActiveAfter = "active_after";
        internal static readonly string TextAvailableFrom = ModLocalization.Get("TextAvailableFrom", "Available From");
        internal const string KeyAvailableFrom = "available_from";
        internal static readonly string TextOutfitChanged = ModLocalization.Get("TextOutfitChanged", "Outfit Changed");
        internal static readonly string TextOutfitChangeAction = ModLocalization.Get("TextOutfitChangeAction", "Outfit Change");
        internal const string KeyOutfitChangeAction = "outfit_change_action";
        internal const string KeyPreviousOutfitPartId = "previous_outfit_part_id";
        internal const string KeyNewOutfitPartId = "new_outfit_part_id";
        internal const string KeyPreviousOutfitAssetId = "previous_outfit_asset_id";
        internal const string KeyNewOutfitAssetId = "new_outfit_asset_id";
        internal static readonly string TextPreviousOutfitId = ModLocalization.Get("TextPreviousOutfitId", "Previous Outfit ID");
        internal static readonly string TextNewOutfitId = ModLocalization.Get("TextNewOutfitId", "New Outfit ID");
        internal static readonly string TextResearchAssigned = ModLocalization.Get("TextResearchAssigned", "Research Assigned");
        internal static readonly string TextResearchPointsPurchased = ModLocalization.Get("TextResearchPointsPurchased", "Research Points Purchased");
        internal static readonly string TextResearchParameterLeveledUp = ModLocalization.Get("TextResearchParameterLeveledUp", "Research Parameter Leveled Up");
        internal const string KeyParamTitle = "param_title";
        internal const string KeyResearchType = "research_type";
        internal static readonly string TextResearchType = ModLocalization.Get("TextResearchType", "Research Area");
        internal static readonly string TextParameterType = ModLocalization.Get("TextParameterType", "Affected Parameter");
        internal const string KeyParamType = "param_type";
        internal static readonly string LabelParameter = ModLocalization.Get("LabelParameter", "Parameter");
        internal const string KeyLevelBefore = "level_before";
        internal const string KeyLevelAfter = "level_after";
        internal static readonly string TextSavedPoints = ModLocalization.Get("TextSavedPoints", "Saved Points");
        internal const string KeySavedPointsBefore = "saved_points_before";
        internal const string KeySavedPointsAfter = "saved_points_after";
        internal static readonly string LabelPoints = ModLocalization.Get("LabelPoints", "Points");
        internal const string KeyPointsBefore = "points_before";
        internal const string KeyPointsAfter = "points_after";
        internal static readonly string TextPointsDelta = ModLocalization.Get("TextPointsDelta", "Point Change");
        internal const string KeyPointsDelta = "points_delta";
        internal static readonly string TextRivalTrendsUpdated = ModLocalization.Get("TextRivalTrendsUpdated", "Rival Trends Updated");
        internal static readonly string TextRivalMonthlyRecalculated = ModLocalization.Get("TextRivalMonthlyRecalculated", "Rival Monthly Recalculated");
        internal static readonly string TextRivalOffice = ModLocalization.Get("TextRivalOffice", "Rival Office");
        internal static readonly string TextMonthIndex = ModLocalization.Get("TextMonthIndex", "Rival Market Month Number");
        internal const string KeyMonthIndexBefore = "month_index_before";
        internal const string KeyMonthIndexAfter = "month_index_after";
        internal static readonly string TextActiveGroups = ModLocalization.Get("TextActiveGroups", "Active Groups");
        internal const string KeyActiveGroupCountBefore = "active_group_count_before";
        internal const string KeyActiveGroupCountAfter = "active_group_count_after";
        internal static readonly string TextRisingGroups = ModLocalization.Get("TextRisingGroups", "Rising Groups");
        internal const string KeyRisingGroupCountBefore = "rising_group_count_before";
        internal const string KeyRisingGroupCountAfter = "rising_group_count_after";
        internal static readonly string TextDisbandedGroups = ModLocalization.Get("TextDisbandedGroups", "Disbanded Groups");
        internal const string KeyDeadGroupCountBefore = "dead_group_count_before";
        internal const string KeyDeadGroupCountAfter = "dead_group_count_after";
        internal static readonly string TextTrendUpdateCost = ModLocalization.Get("TextTrendUpdateCost", "Trend Update Cost");
        internal const string KeyTrendUpdateCost = "trend_update_cost";
        internal static readonly string TextGenreTrends = ModLocalization.Get("TextGenreTrends", "Genre Trends");
        internal static readonly string TextGenreTrendsRising = ModLocalization.Get("TextGenreTrendsRising", "Rising Genres");
        internal static readonly string TextGenreTrendsFalling = ModLocalization.Get("TextGenreTrendsFalling", "Falling Genres");
        internal const string KeyTrendsGenreSummary = "trends_genre_summary";
        internal static readonly string TextLyricsTrends = ModLocalization.Get("TextLyricsTrends", "Lyrics Trends");
        internal static readonly string TextLyricsTrendsRising = ModLocalization.Get("TextLyricsTrendsRising", "Rising Lyrics");
        internal static readonly string TextLyricsTrendsFalling = ModLocalization.Get("TextLyricsTrendsFalling", "Falling Lyrics");
        internal const string KeyTrendsLyricsSummary = "trends_lyrics_summary";
        internal static readonly string TextChoreoTrends = ModLocalization.Get("TextChoreoTrends", "Choreo Trends");
        internal static readonly string TextChoreoTrendsRising = ModLocalization.Get("TextChoreoTrendsRising", "Rising Choreography");
        internal static readonly string TextChoreoTrendsFalling = ModLocalization.Get("TextChoreoTrendsFalling", "Falling Choreography");
        internal static readonly string TextTrendEntryWithPoints = ModLocalization.Get("TextTrendEntryWithPoints", "{0} ({1} points)");
        internal const string KeyTrendsChoreoSummary = "trends_choreo_summary";
        internal const string CodeRising = "rising";
        internal const string CodeFalling = "falling";
        internal const int TrendSummaryMinimumFieldCount = 2;
        internal const int TrendSummaryTitleField = 0;
        internal const int TrendSummaryDirectionField = 1;
        internal const int TrendSummaryPointsField = 2;
        internal static readonly string TextPushSlotStarted = ModLocalization.Get("TextPushSlotStarted", "Push Slot Started");
        internal static readonly string TextPushSlotEnded = ModLocalization.Get("TextPushSlotEnded", "Push Slot Ended");
        internal static readonly string TextPushSlotProgressed = ModLocalization.Get("TextPushSlotProgressed", "Push Slot Progressed");
        internal const string KeyPushCurrentIdolId = "push_current_idol_id";
        internal const string KeyPushPreviousIdolId = "push_previous_idol_id";
        internal static readonly string TextSlotIndex = ModLocalization.Get("TextSlotIndex", "Push Slot Number");
        internal const string KeyPushSlotIndex = "push_slot_index";
        internal const string SeparatorArrow = " -> ";
        internal static readonly string TextDaysInSlot = ModLocalization.Get("TextDaysInSlot", "Days in Push Slot");
        internal const string KeyPushDaysInSlot = "push_days_in_slot";
        internal const string KeyPushLifecycleAction = "push_lifecycle_action";
        internal static readonly string TextWishGenerated = ModLocalization.Get("TextWishGenerated", "Wish Generated");
        internal static readonly string TextWishFulfilled = ModLocalization.Get("TextWishFulfilled", "Wish Fulfilled");
        internal static readonly string TextWishClosed = ModLocalization.Get("TextWishClosed", "Wish Closed");
        internal static readonly string TextWishType = ModLocalization.Get("TextWishType", "Wish Type");
        internal const string KeyWishTypeBefore = "wish_type_before";
        internal const string KeyWishTypeAfter = "wish_type_after";
        internal static readonly string TextWishFormula = ModLocalization.Get("TextWishFormula", "Wish Goal");
        internal const string KeyWishFormulaBefore = "wish_formula_before";
        internal const string KeyWishFormulaAfter = "wish_formula_after";
        internal const string KeyWishFulfilledBefore = "wish_fulfilled_before";
        internal const string KeyWishFulfilledAfter = "wish_fulfilled_after";
        internal static readonly string TextInfluenceDelta = ModLocalization.Get("TextInfluenceDelta", "Influence Change");
        internal const string KeyInfluencePointsDelta = "influence_points_delta";
        internal static readonly string TextMentalStaminaDelta = ModLocalization.Get("TextMentalStaminaDelta", "Mental Stamina Change");
        internal const string KeyMentalStaminaDelta = "mental_stamina_delta";
        internal static readonly string TextBlackmailQueued = ModLocalization.Get("TextBlackmailQueued", "Blackmail Queued");
        internal static readonly string TextBlackmailTriggered = ModLocalization.Get("TextBlackmailTriggered", "Blackmail Triggered");
        internal const string KeyTargetId = "target_id";
        internal const string KeySpyId = "spy_id";
        internal static readonly string TextSpyIdol = ModLocalization.Get("TextSpyIdol", "Spy Idol: ");
        internal static readonly string TextDaysUntilReport = ModLocalization.Get("TextDaysUntilReport", "Days Until Blackmail Report");
        internal const string KeyDaysUntilReport = "days_until_report";
        internal static readonly string TextQueueSize = ModLocalization.Get("TextQueueSize", "Queue Size");
        internal const string KeyQueueSizeAfter = "queue_size_after";
        internal static readonly string TextSuccessTier = ModLocalization.Get("TextSuccessTier", "Blackmail Result Tier");
        internal const string KeySuccessTier = "success_tier";
        internal static readonly string TextInfluenceAward = ModLocalization.Get("TextInfluenceAward", "Influence Award");
        internal const string KeyInfluenceAward = "influence_award";
        internal static readonly string TextReportDate = ModLocalization.Get("TextReportDate", "Report Date");
        internal const string KeyReportDate = "report_date";
        internal static readonly string TextScandalMitigated = ModLocalization.Get("TextScandalMitigated", "Scandal Mitigated");
        internal static readonly string TextScandalPoints = ModLocalization.Get("TextScandalPoints", "Scandal Points");
        internal const string KeyScandalPointsBefore = "scandal_points_before";
        internal const string KeyScandalPointsAfter = "scandal_points_after";
        internal static readonly string TextPointsRemoved = ModLocalization.Get("TextPointsRemoved", "Points Removed");
        internal const string KeyScandalPointsRemoved = "scandal_points_removed";
        internal static readonly string TextGroupPointsRemoved = ModLocalization.Get("TextGroupPointsRemoved", "Group Points Removed");
        internal const string KeyScandalGroupPointsRemoved = "scandal_group_points_removed";
        internal static readonly string TextGroupPointsRemaining = ModLocalization.Get("TextGroupPointsRemaining", "Group Points Remaining");
        internal const string KeyScandalGroupPointsRemaining = "scandal_group_points_remaining";
        internal static readonly string TextAuditionStarted = ModLocalization.Get("TextAuditionStarted", "Audition Started");
        internal static readonly string TextAuditionCostPaid = ModLocalization.Get("TextAuditionCostPaid", "Audition Cost Paid");
        internal static readonly string TextAuditionCooldownReset = ModLocalization.Get("TextAuditionCooldownReset", "Audition Cooldown Reset");
        internal static readonly string TextAuditionFailureTriggered = ModLocalization.Get("TextAuditionFailureTriggered", "Audition Failure Triggered");
        internal static readonly string LabelAuditions = ModLocalization.Get("LabelAuditions", "Auditions");
        internal static readonly string TextAuditionType = ModLocalization.Get("TextAuditionType", "Audition Type");
        internal const string KeyAuditionType = "audition_type";
        internal static readonly string TextResetType = ModLocalization.Get("TextResetType", "Cooldown Reset");
        internal const string KeyResetType = "reset_type";
        internal static readonly string LabelCost = ModLocalization.Get("LabelCost", "Cost");
        internal const string KeyCost = "Cost";
        internal static readonly string TextResetCost = ModLocalization.Get("TextResetCost", "Reset Cost");
        internal const string KeyResetCost = "reset_cost";
        internal static readonly string TextGeneratedCandidates = ModLocalization.Get("TextGeneratedCandidates", "Generated Candidates");
        internal const string KeyGeneratedCandidateCount = "generated_candidate_count";
        internal static readonly string TextAwardNominated = ModLocalization.Get("TextAwardNominated", "Award Nominated");
        internal static readonly string TextAwardResult = ModLocalization.Get("TextAwardResult", "Award Result");
        internal static readonly string LabelAgency = ModLocalization.Get("LabelAgency", "Agency");
        internal static readonly string LabelAward = ModLocalization.Get("LabelAward", "Award");
        internal const string KeyAwardType = "award_type";
        internal static readonly string LabelYear = ModLocalization.Get("LabelYear", "Year");
        internal const string KeyAwardYear = "award_year";
        internal const string KeyAwardIsNomination = "award_is_nomination";
        internal static readonly string TextIsNomination = ModLocalization.Get("TextIsNomination", "Is Nomination: ");
        internal const string KeyAwardWon = "award_won";
        internal static readonly string TextWon = ModLocalization.Get("TextWon", "Won: ");
        internal static readonly string TextAgencyRoomBuilt = ModLocalization.Get("TextAgencyRoomBuilt", "Agency Room Built");
        internal static readonly string TextAgencyRoomCostPaid = ModLocalization.Get("TextAgencyRoomCostPaid", "Agency Room Cost Paid");
        internal static readonly string TextRoomId = ModLocalization.Get("TextRoomId", "Room ID");
        internal const string KeyRoomId = "room_id";
        internal static readonly string LabelFloor = ModLocalization.Get("LabelFloor", "Floor");
        internal const string KeyFloorIndex = "floor_index";
        internal static readonly string TextRoomSpace = ModLocalization.Get("TextRoomSpace", "Room Space");
        internal const string KeyRoomSpace = "room_space";
        internal static readonly string TextRoomCost = ModLocalization.Get("TextRoomCost", "Room Cost");
        internal const string KeyRoomCost = "room_cost";
        internal static readonly string TextIdolJoinedAgency = ModLocalization.Get("TextIdolJoinedAgency", "Idol Joined Agency");
        internal static readonly string TextGraduationAnnounced = ModLocalization.Get("TextGraduationAnnounced", "Graduation Announced");
        internal static readonly string TextIdolGraduated = ModLocalization.Get("TextIdolGraduated", "Idol Graduated");
        internal static readonly string TextIdolBirthday = ModLocalization.Get("TextIdolBirthday", "Birthday");
        internal static readonly string TextIdolGroupTransfer = ModLocalization.Get("TextIdolGroupTransfer", "Idol Group Transfer");
        internal static readonly string TextFromGroup = ModLocalization.Get("TextFromGroup", "From Group");
        internal const string KeyFromGroupTitle = "from_group_title";
        internal static readonly string TextToGroup = ModLocalization.Get("TextToGroup", "To Group");
        internal const string KeyToGroupTitle = "to_group_title";
        internal static readonly string TextFromGroupStatus = ModLocalization.Get("TextFromGroupStatus", "From Group Status");
        internal const string KeyFromGroupStatus = "from_group_status";
        internal static readonly string TextToGroupStatus = ModLocalization.Get("TextToGroupStatus", "To Group Status");
        internal const string KeyToGroupStatus = "to_group_status";
        internal static readonly string TextTransferDate = ModLocalization.Get("TextTransferDate", "Transfer Date");
        internal const string KeyTransferDate = "transfer_date";
        internal static readonly string LabelType = ModLocalization.Get("LabelType", "Type");
        internal const string KeyIdolType = "idol_type";
        internal static readonly string LabelAge = ModLocalization.Get("LabelAge", "Age");
        internal const string KeyIdolAge = "idol_age";
        internal const string KeyIdolBirthdayDate = "idol_birthday_date";
        internal static readonly string TextHiringDate = ModLocalization.Get("TextHiringDate", "Hiring Date");
        internal const string KeyIdolHiringDate = "idol_hiring_date";
        internal static readonly string TextGraduationDate = ModLocalization.Get("TextGraduationDate", "Graduation Date");
        internal const string KeyIdolGraduationDate = "idol_graduation_date";
        internal static readonly string LabelTrivia = ModLocalization.Get("LabelTrivia", "Trivia");
        internal const string KeyIdolCustomTrivia = "idol_custom_trivia";
        internal const string KeyIdolTrivia = "idol_trivia";
        internal const string KeyIdolGraduationWithDialogue = "idol_graduation_with_dialogue";
        internal static readonly string TextGraduationDialogue = ModLocalization.Get("TextGraduationDialogue", "Graduation Dialogue: ");
        internal static readonly string TextStatusStarted = ModLocalization.Get("TextStatusStarted", "Status Started");
        internal static readonly string TextStatusEnded = ModLocalization.Get("TextStatusEnded", "Status Ended");
        internal static readonly string TextStatusUpdated = ModLocalization.Get("TextStatusUpdated", "Status Updated");
        internal static readonly string TextDailyAgencyUpdate = ModLocalization.Get("TextDailyAgencyUpdate", "Daily Agency Update");
        internal static readonly string TextExpectedDailyProfit = ModLocalization.Get("TextExpectedDailyProfit", "Expected Daily Profit");
        internal const string KeyExpectedDailyProfit = "expected_daily_profit";
        internal static readonly string TextExpectedDailyFame = ModLocalization.Get("TextExpectedDailyFame", "Expected Daily Fame");
        internal const string KeyExpectedDailyFameGain = "expected_daily_fame_gain";
        internal static readonly string TextExpectedDailyBuzz = ModLocalization.Get("TextExpectedDailyBuzz", "Expected Daily Buzz");
        internal const string KeyExpectedDailyBuzzGain = "expected_daily_buzz_gain";
        internal static readonly string TextWeeklyExpensesApplied = ModLocalization.Get("TextWeeklyExpensesApplied", "Weekly Expenses Applied");
        internal static readonly string TextWeeklyExpense = ModLocalization.Get("TextWeeklyExpense", "Weekly Expense");
        internal const string KeyWeeklyExpense = "weekly_expense";
        internal static readonly string TextTotalFans = ModLocalization.Get("TextTotalFans", "Total Fans");
        internal const string KeyFansTotal = "fans_total";
        internal const string KeyFamePoints = "fame_points";
        internal static readonly string TextBuzzPoints = ModLocalization.Get("TextBuzzPoints", "Buzz Points");
        internal const string KeyBuzzPoints = "buzz_points";
        internal static readonly string TextPolicyDecisionSelected = ModLocalization.Get("TextPolicyDecisionSelected", "Policy Decision Selected");
        internal static readonly string TextAgencyPolicy = ModLocalization.Get("TextAgencyPolicy", "Agency Policy");
        internal static readonly string LabelPolicy = ModLocalization.Get("LabelPolicy", "Policy");
        internal const string KeyPolicyType = "policy_type";
        internal static readonly string LabelSelection = ModLocalization.Get("LabelSelection", "Selection");
        internal const string KeyPreviousValue = "previous_value";
        internal const string KeyNewValue = "new_value";
        internal static readonly string TextPolicyCost = ModLocalization.Get("TextPolicyCost", "Policy Cost");
        internal const string KeyPolicyCost = "policy_cost";
        internal const string KeyFreeSelection = "free_selection";
        internal static readonly string TextFreeSelection = ModLocalization.Get("TextFreeSelection", "Free Selection: ");
        internal static readonly string TextDecisionDate = ModLocalization.Get("TextDecisionDate", "Decision Date");
        internal const string KeyDecisionDate = "decision_date";
        internal static readonly string TextProducerRelationshipUpdated = ModLocalization.Get("TextProducerRelationshipUpdated", "Producer Relationship Updated");
        internal static readonly string TextRelationshipType = ModLocalization.Get("TextRelationshipType", "Producer Relationship");
        internal const string KeyPlayerRelationshipType = "player_relationship_type";
        internal static readonly string TextRelationshipPoints = ModLocalization.Get("TextRelationshipPoints", "Relationship Points");
        internal const string KeyPlayerPointsBefore = "player_points_before";
        internal const string KeyPlayerPointsAfter = "player_points_after";
        internal static readonly string TextAppliedChange = ModLocalization.Get("TextAppliedChange", "Applied Change");
        internal const string KeyPlayerPointsAppliedDelta = "player_points_applied_delta";
        internal static readonly string TextInfluenceLevel = ModLocalization.Get("TextInfluenceLevel", "Influence Level");
        internal const string KeyPlayerLevelBefore = "player_level_before";
        internal const string KeyPlayerLevelAfter = "player_level_after";
        internal static readonly string TextProducerDateInteraction = ModLocalization.Get("TextProducerDateInteraction", "Producer Date Interaction");
        internal static readonly string LabelInteraction = ModLocalization.Get("LabelInteraction", "Interaction");
        internal const string KeyDateInteractionType = "date_interaction_type";
        internal const string KeyDateRouteBefore = "date_route_before";
        internal const string KeyDateRouteAfter = "date_route_after";
        internal static readonly string LabelStage = ModLocalization.Get("LabelStage", "Stage");
        internal const string KeyDateStageBefore = "date_stage_before";
        internal const string KeyDateStageAfter = "date_stage_after";
        internal static readonly string TextDateStatus = ModLocalization.Get("TextDateStatus", "Dating Status");
        internal const string KeyDateStatusBefore = "date_status_before";
        internal const string KeyDateStatusAfter = "date_status_after";
        internal const string KeyDateResultToken = "date_result_token";
        internal const string KeyDateResultSummaryCode = "date_result_summary_code";
        internal static readonly string TextDateOutcome = ModLocalization.Get("TextDateOutcome", "Date Outcome");
        internal const string CodeDateResultTokenPublic = "pub";
        internal const string CodeDateResultTokenGeneric = "generic";
        internal const string CodeDateResultTokenDeferred = "deferred";
        internal const string CodeDateResultTokenNone = "none";
        internal const string CodeDateResultDialogueFollowup = "dialogue_followup";
        internal const string CodeDateResultNoSpecialResult = "no_special_result";
        internal const string CodeDateResultPublicDate = "public_date";
        internal const string CodeDateResultRoutineDate = "routine_date";
        internal const string CodeDateResultMultiResult = "multi_result";
        internal static readonly string LabelCaught = ModLocalization.Get("LabelCaught", "Caught");
        internal const string KeyDateCaughtBefore = "date_caught_before";
        internal const string KeyDateCaughtAfter = "date_caught_after";
        internal static readonly string TextRelationshipLevel = ModLocalization.Get("TextRelationshipLevel", "Relationship Level");
        internal const string KeyDateRelationshipLevelBefore = "date_relationship_level_before";
        internal const string KeyDateRelationshipLevelAfter = "date_relationship_level_after";
        internal static readonly string TextMarriageOutcome = ModLocalization.Get("TextMarriageOutcome", "Marriage Outcome");
        internal const string KeyMarriageRoute = "marriage_route";
        internal const string KeyMarriageStage = "marriage_stage";
        internal static readonly string TextPartnerStatus = ModLocalization.Get("TextPartnerStatus", "Marriage Status");
        internal const string KeyMarriagePartnerStatus = "marriage_partner_status";
        internal const string KeyMarriageGoodOutcome = "marriage_good_outcome";
        internal static readonly string TextGoodOutcome = ModLocalization.Get("TextGoodOutcome", "Good Outcome: ");
        internal static readonly string LabelKids = ModLocalization.Get("LabelKids", "Kids");
        internal const string KeyMarriageKidsString = "marriage_kids_string";
        internal static readonly string TextCareerOutcomeOne = ModLocalization.Get("TextCareerOutcomeOne", "Career Outcome 1");
        internal const string KeyMarriageCareerStringOne = "marriage_career_string_one";
        internal static readonly string TextCareerOutcomeTwo = ModLocalization.Get("TextCareerOutcomeTwo", "Career Outcome 2");
        internal const string KeyMarriageCareerStringTwo = "marriage_career_string_two";
        internal static readonly string TextRelationshipOutcome = ModLocalization.Get("TextRelationshipOutcome", "Relationship Outcome");
        internal const string KeyMarriageRelationshipOutcomeString = "marriage_relationship_outcome_string";
        internal static readonly string TextJoinedClique = ModLocalization.Get("TextJoinedClique", "Joined Clique");
        internal static readonly string TextLeftClique = ModLocalization.Get("TextLeftClique", "Left Clique");
        internal const string KeyCliqueLeaderId = "clique_leader_id";
        internal const string KeyCliqueLeaderIdBefore = "clique_leader_id_before";
        internal const string KeyCliqueLeaderIdAfter = "clique_leader_id_after";
        internal const string KeyCliqueMemberIdList = "clique_member_id_list";
        internal const string KeyCliqueMemberIdListBefore = "clique_member_id_list_before";
        internal const string KeyCliqueMemberIdListAfter = "clique_member_id_list_after";
        internal const string KeyCliqueMemberIdListAdded = "clique_member_id_list_added";
        internal const string KeyCliqueMemberIdListRemoved = "clique_member_id_list_removed";
        internal static readonly string TextCliqueLeader = ModLocalization.Get("TextCliqueLeader", "Clique Leader: ");
        internal static readonly string TextCliqueMembers = ModLocalization.Get("TextCliqueMembers", "Clique Members");
        internal const string KeyCliqueMemberCount = "clique_member_count";
        internal const string KeyCliqueQuitWasViolent = "clique_quit_was_violent";
        internal static readonly string TextViolentExit = ModLocalization.Get("TextViolentExit", "Kicked Out of Clique: ");
        internal static readonly string TextSingleCreated = ModLocalization.Get("TextSingleCreated", "Single Created");
        internal static readonly string TextSingleCancelled = ModLocalization.Get("TextSingleCancelled", "Single Cancelled");
        internal static readonly string TextSingleStatusUpdated = ModLocalization.Get("TextSingleStatusUpdated", "Single Status Updated");
        internal static readonly string TextSingleSenbatsuUpdated = ModLocalization.Get("TextSingleSenbatsuUpdated", "Single Senbatsu Updated");
        internal static readonly string TextSingleGroupAssignmentUpdated = ModLocalization.Get("TextSingleGroupAssignmentUpdated", "Single Group Assignment Updated");
        internal const string KeySinglePreviousStatus = "single_previous_status";
        internal const string KeySingleNewStatus = "single_new_status";
        internal static readonly string TextCastMembers = ModLocalization.Get("TextCastMembers", "Cast Members");
        internal const string KeySingleCastCount = "single_cast_count";
        internal static readonly string TextCastSize = ModLocalization.Get("TextCastSize", "Cast Size");
        internal const string KeySingleCastCountBefore = "single_cast_count_before";
        internal const string KeySingleCastCountAfter = "single_cast_count_after";
        internal const string KeySingleCastIdListAdded = "single_cast_id_list_added";
        internal static readonly string TextAddedMembers = ModLocalization.Get("TextAddedMembers", "Added Members: ");
        internal const string KeySingleCastIdListRemoved = "single_cast_id_list_removed";
        internal static readonly string TextRemovedMembers = ModLocalization.Get("TextRemovedMembers", "Removed Members: ");
        internal const string KeySingleRemovedIdolId = "single_removed_idol_id";
        internal static readonly string TextReleaseDate = ModLocalization.Get("TextReleaseDate", "Release Date");
        internal const string KeySingleReleaseDate = "single_release_date";
        internal static readonly string LabelDistribution = ModLocalization.Get("LabelDistribution", "Distribution");
        internal const string KeySingleIsDigital = "single_is_digital";
        internal const string CodeDigital = "digital";
        internal const string CodePhysical = "physical";
        internal const string CodeReleased = "released";
        internal const string CodeConcept = "concept";
        internal const string KeySingleLinkedElectionId = "single_linked_election_id";
        internal static readonly string TextLinkedElection = ModLocalization.Get("TextLinkedElection", "Linked Election: #");
        internal static readonly string TextTvShowCreated = ModLocalization.Get("TextTvShowCreated", "TV Show Created");
        internal static readonly string TextTvShowReleased = ModLocalization.Get("TextTvShowReleased", "TV Show Released");
        internal static readonly string TextTvShowCancelled = ModLocalization.Get("TextTvShowCancelled", "TV Show Cancelled");
        internal static readonly string TextTvShowCastUpdated = ModLocalization.Get("TextTvShowCastUpdated", "TV Show Cast Updated");
        internal static readonly string TextTvShowConfigurationUpdated = ModLocalization.Get("TextTvShowConfigurationUpdated", "TV Show Configuration Updated");
        internal static readonly string TextTvShowRelaunchStarted = ModLocalization.Get("TextTvShowRelaunchStarted", "TV Show Relaunch Started");
        internal static readonly string TextTvShowRelaunchFinished = ModLocalization.Get("TextTvShowRelaunchFinished", "TV Show Relaunch Finished");
        internal static readonly string TextShowCreated = ModLocalization.Get("TextShowCreated", "Show Created");
        internal static readonly string TextShowReleased = ModLocalization.Get("TextShowReleased", "Show Released");
        internal static readonly string TextShowCancelled = ModLocalization.Get("TextShowCancelled", "Show Cancelled");
        internal static readonly string TextShowCastUpdated = ModLocalization.Get("TextShowCastUpdated", "Show Cast Updated");
        internal static readonly string TextShowConfigurationUpdated = ModLocalization.Get("TextShowConfigurationUpdated", "Show Configuration Updated");
        internal static readonly string TextShowRelaunchStarted = ModLocalization.Get("TextShowRelaunchStarted", "Show Relaunch Started");
        internal static readonly string TextShowRelaunchFinished = ModLocalization.Get("TextShowRelaunchFinished", "Show Relaunch Finished");
        internal static readonly string TextShowActionCreated = ModLocalization.Get("TextShowActionCreated", "Created");
        internal static readonly string TextShowActionReleased = ModLocalization.Get("TextShowActionReleased", "Released");
        internal static readonly string TextShowActionCancelled = ModLocalization.Get("TextShowActionCancelled", "Cancelled");
        internal static readonly string TextShowActionCastUpdated = ModLocalization.Get("TextShowActionCastUpdated", "Cast Updated");
        internal static readonly string TextShowActionConfigurationUpdated = ModLocalization.Get("TextShowActionConfigurationUpdated", "Configuration Updated");
        internal static readonly string TextShowActionRelaunchStarted = ModLocalization.Get("TextShowActionRelaunchStarted", "Relaunch Started");
        internal static readonly string TextShowActionRelaunchFinished = ModLocalization.Get("TextShowActionRelaunchFinished", "Relaunch Finished");
        internal static readonly string TextShowActionEpisodeReleased = ModLocalization.Get("TextShowActionEpisodeReleased", "Episode Released");
        internal static readonly string TextShowActionStatusUpdated = ModLocalization.Get("TextShowActionStatusUpdated", "Status Updated");
        internal static readonly string TextShowEventWithMediumFormat = ModLocalization.Get("TextShowEventWithMediumFormat", "{0} {1}");
        internal static readonly string TextShowTypeWithMediumFormat = ModLocalization.Get("TextShowTypeWithMediumFormat", "{0} Show");
        internal static readonly string TextShowTitleWithMediumPrefixFormat = ModLocalization.Get("TextShowTitleWithMediumPrefixFormat", "{0}: ");
        internal static readonly string TextShowIdWithMediumPrefixFormat = ModLocalization.Get("TextShowIdWithMediumPrefixFormat", "{0} #");
        internal static readonly string LabelMedium = ModLocalization.Get("LabelMedium", "Medium");
        internal const string KeyShowMediumTitle = "show_medium_title";
        internal static readonly string LabelGenre = ModLocalization.Get("LabelGenre", "Genre");
        internal const string KeyShowGenreTitle = "show_genre_title";
        internal static readonly string LabelCast = ModLocalization.Get("LabelCast", "Cast");
        internal static readonly string TextCastType = ModLocalization.Get("TextCastType", "Casting Style");
        internal const string KeyShowCastTypeBefore = "show_cast_type_before";
        internal const string KeyShowCastTypeAfter = "show_cast_type_after";
        internal static readonly string TextShowTitle = ModLocalization.Get("TextShowTitle", "Show Title");
        internal const string KeyShowTitleBefore = "show_title_before";
        internal const string KeyShowTitleAfter = "show_title_after";
        internal static readonly string LabelMc = ModLocalization.Get("LabelMc", "MC");
        internal const string KeyShowMcTitleBefore = "show_mc_title_before";
        internal const string KeyShowMcTitleAfter = "show_mc_title_after";
        internal static readonly string TextTotalEpisodesReleased = ModLocalization.Get("TextTotalEpisodesReleased", "Total Episodes Released");
        internal const string KeyShowCastCountBefore = "show_cast_count_before";
        internal const string KeyShowCastCountAfter = "show_cast_count_after";
        internal const string KeyShowCastIdListBefore = "show_cast_id_list_before";
        internal const string KeyShowCastIdListAfter = "show_cast_id_list_after";
        internal const string KeyShowCastIdListAdded = "show_cast_id_list_added";
        internal static readonly string TextAddedCast = ModLocalization.Get("TextAddedCast", "Added Cast: ");
        internal const string KeyShowCastIdListRemoved = "show_cast_id_list_removed";
        internal static readonly string TextRemovedCast = ModLocalization.Get("TextRemovedCast", "Removed Cast: ");
        internal const string KeyShowRemovedIdolId = "show_removed_idol_id";
        internal static readonly string TextAverageAudience = ModLocalization.Get("TextAverageAudience", "Average Audience");
        internal static readonly string TextAverageRevenue = ModLocalization.Get("TextAverageRevenue", "Average Revenue");
        internal static readonly string TextAverageNewFans = ModLocalization.Get("TextAverageNewFans", "Average New Fans");
        internal static readonly string TextAverageBuzz = ModLocalization.Get("TextAverageBuzz", "Average Buzz");
        internal static readonly string TextRelaunchCount = ModLocalization.Get("TextRelaunchCount", "Times Relaunched");
        internal const string KeyShowRelaunchCount = "show_relaunch_count";
        internal static readonly string TextLaunchDate = ModLocalization.Get("TextLaunchDate", "Launch Date");
        internal const string KeyShowLaunchDate = "show_launch_date";
        internal const string KeyShowProductionCostBefore = "show_production_cost_before";
        internal const string KeyShowProductionCostAfter = "show_production_cost_after";
        internal static readonly string TextProductionCostPrefix = ModLocalization.Get("TextProductionCostPrefix", "Production Cost: ");
        internal const string FormatNumberNoDecimal = "N0";
        internal static readonly string TextContractOfferOpened = ModLocalization.Get("TextContractOfferOpened", "Contract Offer Opened");
        internal static readonly string TextContractAccepted = ModLocalization.Get("TextContractAccepted", "Contract Accepted");
        internal static readonly string TextContractCancelled = ModLocalization.Get("TextContractCancelled", "Contract Cancelled");
        internal static readonly string LabelProduct = ModLocalization.Get("LabelProduct", "Product");
        internal static readonly string TextContractType = ModLocalization.Get("TextContractType", "Contract Type");
        internal static readonly string TextContractFocus = ModLocalization.Get("TextContractFocus", "Contract Focus");
        internal static readonly string TextScope = ModLocalization.Get("TextScope", "Contract Applies To: ");
        internal static readonly string TextGroupContract = ModLocalization.Get("TextGroupContract", "Entire Group");
        internal static readonly string TextIdolContract = ModLocalization.Get("TextIdolContract", "One Idol");
        internal static readonly string TextEndDate = ModLocalization.Get("TextEndDate", "End Date");
        internal static readonly string TextDurationMonths = ModLocalization.Get("TextDurationMonths", "Duration (Months)");
        internal static readonly string TextActivation = ModLocalization.Get("TextActivation", "Activation: ");
        internal static readonly string LabelImmediate = ModLocalization.Get("LabelImmediate", "Immediate");
        internal static readonly string LabelScheduled = ModLocalization.Get("LabelScheduled", "Scheduled");
        internal static readonly string TextWeeklyPay = ModLocalization.Get("TextWeeklyPay", "Weekly Pay");
        internal static readonly string TextWeeklyFans = ModLocalization.Get("TextWeeklyFans", "Weekly Fans");
        internal static readonly string TextWeeklyBuzz = ModLocalization.Get("TextWeeklyBuzz", "Weekly Buzz");
        internal static readonly string TextWeeklyFame = ModLocalization.Get("TextWeeklyFame", "Weekly Fame");
        internal static readonly string TextWeeklyStamina = ModLocalization.Get("TextWeeklyStamina", "Weekly Stamina");
        internal static readonly string TextLiability = ModLocalization.Get("TextLiability", "Liability: ");
        internal static readonly string TextGroupCreated = ModLocalization.Get("TextGroupCreated", "Group Created");
        internal static readonly string TextGroupDisbanded = ModLocalization.Get("TextGroupDisbanded", "Group Disbanded");
        internal static readonly string TextGroupParameterPointsUpdated = ModLocalization.Get("TextGroupParameterPointsUpdated", "Group Parameter Points Updated");
        internal static readonly string TextGroupAppealPointsSpent = ModLocalization.Get("TextGroupAppealPointsSpent", "Group Appeal Points Spent");
        internal static readonly string TextGroupStatus = ModLocalization.Get("TextGroupStatus", "Group Status");
        internal const string KeyGroupStatus = "group_status";
        internal const string KeyGroupEventDate = "group_event_date";
        internal static readonly string LabelMembers = ModLocalization.Get("LabelMembers", "Members");
        internal const string KeyGroupMemberCount = "group_member_count";
        internal static readonly string TextReleasedSingles = ModLocalization.Get("TextReleasedSingles", "Released Singles");
        internal const string KeyGroupSingleCount = "group_single_count";
        internal static readonly string TextUnreleasedSingles = ModLocalization.Get("TextUnreleasedSingles", "Unreleased Singles");
        internal const string KeyGroupNonReleasedSingleCount = "group_non_released_single_count";
        internal static readonly string TextTargetGender = ModLocalization.Get("TextTargetGender", "Target Gender");
        internal const string KeyGroupAppealGender = "group_appeal_gender";
        internal static readonly string TextTargetIntensity = ModLocalization.Get("TextTargetIntensity", "Target Intensity");
        internal const string KeyGroupAppealHardcoreness = "group_appeal_hardcoreness";
        internal static readonly string TextTargetAge = ModLocalization.Get("TextTargetAge", "Target Age");
        internal const string KeyGroupAppealAge = "group_appeal_age";
        internal static readonly string TextSourceParameter = ModLocalization.Get("TextSourceParameter", "Stat Used For This Change");
        internal const string KeyGroupSourceParamType = "group_source_param_type";
        internal static readonly string TextTargetFanSegment = ModLocalization.Get("TextTargetFanSegment", "Target Fan Segment");
        internal const string KeyGroupTargetFanType = "group_target_fan_type";
        internal static readonly string TextParameterPoints = ModLocalization.Get("TextParameterPoints", "Parameter Points");
        internal const string KeyGroupPointsBefore = "group_points_before";
        internal const string KeyGroupPointsAfter = "group_points_after";
        internal static readonly string TextParameterDelta = ModLocalization.Get("TextParameterDelta", "Parameter Point Change");
        internal const string KeyGroupPointsDelta = "group_points_delta";
        internal static readonly string TextAvailablePoints = ModLocalization.Get("TextAvailablePoints", "Available Points");
        internal const string KeyGroupAvailablePointsBefore = "group_available_points_before";
        internal const string KeyGroupAvailablePointsAfter = "group_available_points_after";
        internal static readonly string TextSpentPoints = ModLocalization.Get("TextSpentPoints", "Spent Points");
        internal const string KeyGroupPointsSpentBefore = "group_points_spent_before";
        internal const string KeyGroupPointsSpentAfter = "group_points_spent_after";
        internal static readonly string TextTargetPoints = ModLocalization.Get("TextTargetPoints", "Target Points");
        internal const string KeyGroupTargetPointsBefore = "group_target_points_before";
        internal const string KeyGroupTargetPointsAfter = "group_target_points_after";
        internal static readonly string TextRequestedPoints = ModLocalization.Get("TextRequestedPoints", "Requested Points");
        internal const string KeyGroupPointsRequested = "group_points_requested";
        internal static readonly string TextAppliedPoints = ModLocalization.Get("TextAppliedPoints", "Applied Points");
        internal const string KeyGroupPointsApplied = "group_points_applied";
        internal const string SeparatorPipe = "|";
        internal const char SeparatorPipeCharacter = '|';
        internal const char SeparatorColonCharacter = ':';
        internal const char SeparatorCommaCharacter = ',';
        internal const string SeparatorSpaceSlashSpace = " / ";
        internal static readonly string TextTimelineLimitedToLatestPrefix = ModLocalization.Get("TextTimelineLimitedToLatestPrefix", "Timeline limited to latest ");
        internal static readonly string TextVisibleEventsForUiPerformanceSuffix = ModLocalization.Get("TextVisibleEventsForUiPerformanceSuffix", " visible events for UI performance.");
        internal const string UiSuffixTimeline = "_Timeline";
        internal const string UiNameCareerDiaryTimelineAction = "CareerDiary_TimelineAction";
        internal const string UiNameCareerDiaryActionButtonRefreshTimeline = "CareerDiary_ActionButton_RefreshTimeline";
        internal const string UiNameCareerDiaryActionButtonShowMoreTimeline = "CareerDiary_ActionButton_ShowMoreTimeline";
        internal const string UiNameCareerDiaryActionButtonClearTimelineFilters = "CareerDiary_ActionButton_ClearTimelineFilters";
        internal const string UiRowSeparator = "_Row_";
        internal const string UiNameCareerDiaryTimelineItemPrefix = "CareerDiary_TimelineItem_";
        internal const string UiSuffixTimelineFilters = "_TimelineFilters";
        internal static readonly string TextShowPrefix = ModLocalization.Get("TextShowPrefix", "Show ");
        internal static readonly string TextHidePrefix = ModLocalization.Get("TextHidePrefix", "Hide ");
        internal const string UiNameCareerDiaryFilterToggle = "CareerDiary_FilterToggle";
        internal const string UiNameCareerDiaryFilterTogglePrefix = "CareerDiary_FilterToggle_";
        internal const string UiSuffixDetail = "_Detail";
        internal const string UiNameCareerDiaryActionButtonOpenSource = "CareerDiary_ActionButton_OpenSource";
        internal const string UiNameCareerDiaryActionButtonIdolPrefix = "CareerDiary_ActionButton_Idol_";
        internal const string UiSuffixFooter = "_Footer";
        internal const string UiNameCareerDiaryActionButtonBackToTimeline = "CareerDiary_ActionButton_BackToTimeline";
        internal static readonly string TextOpenFormationViewerForSingleSenbatsu = ModLocalization.Get("TextOpenFormationViewerForSingleSenbatsu", "Open the formation viewer to inspect this single's senbatsu using the base-game UI.");
        internal const string UiNameCareerDiaryActionButtonOpenSingleFormation = "CareerDiary_ActionButton_OpenSingleFormation";
        internal const string UiNameCareerDiaryActionButtonOpenSingleChart = "CareerDiary_ActionButton_OpenSingleChart";
        internal const string UiNameCareerDiaryTimelineParticipantRowPrefix = "CareerDiary_TimelineParticipants_Row_";
        internal const string UiNameCareerDiaryTimelineParticipantCardPrefix = "CareerDiary_TimelineParticipants_Card_";
        internal const string UiNameCareerDiaryRelatedIdolRowPrefix = "CareerDiary_RelatedIdolRow_";
        internal const string UiNameCareerDiaryRelatedIdolCardPrefix = "CareerDiary_RelatedIdolCard_";
        internal const string UiNamePortrait = "Portrait";
        internal const string SeparatorHash = "#";
        internal const string SeparatorSpaceOpenParen = " (";
        internal const string SeparatorSlash = "/";
        internal const string SeparatorCloseParen = ")";
        internal const string UiSuffixIcons = "_Icons";
        internal const string SeparatorUnderscore = "_";
        internal const string UiSuffixPortrait = "_Portrait";
        internal const string SeparatorColonSpace = ": ";
        internal const string SeparatorSlashOneHundred = "/100";
        internal static readonly string TextFameOfSenbatsu = ModLocalization.Get("TextFameOfSenbatsu", "Fame of Senbatsu: ");
        internal const string SeparatorSlashTen = "/10";
        internal const string FormatSingleMetricOneDecimal = "0.#";
        internal const string UiNameCareerDiarySingleCardTextPrefix = "CareerDiary_SingleCardText_";
        internal const string FormatGuidCompact = "N";
        internal const string SeparatorDash = "-";
        internal const string SeparatorSpace = " ";
        internal static readonly string TextSourceEntityOpened = ModLocalization.Get("TextSourceEntityOpened", "Related record opened.");
        internal static readonly string TextUnableToOpenSourceEntity = ModLocalization.Get("TextUnableToOpenSourceEntity", "Unable to open related record: ");
        internal static readonly string TextEventIsNull = ModLocalization.Get("TextEventIsNull", "Event is null.");
        internal static readonly string TextInvalidSingleId = ModLocalization.Get("TextInvalidSingleId", "Invalid single id.");
        internal static readonly string TextSingleNotFound = ModLocalization.Get("TextSingleNotFound", "Single not found.");
        internal static readonly string TextSingleReleasePopupUnavailable = ModLocalization.Get("TextSingleReleasePopupUnavailable", "Single release popup unavailable.");
        internal static readonly string TextSingleReleaseComponentUnavailable = ModLocalization.Get("TextSingleReleaseComponentUnavailable", "Single release component unavailable.");
        internal static readonly string TextInvalidShowId = ModLocalization.Get("TextInvalidShowId", "Invalid show id.");
        internal static readonly string TextShowNotFound = ModLocalization.Get("TextShowNotFound", "Show not found.");
        internal static readonly string TextShowResultPopupUnavailable = ModLocalization.Get("TextShowResultPopupUnavailable", "Show result popup unavailable.");
        internal static readonly string TextShowResultComponentUnavailable = ModLocalization.Get("TextShowResultComponentUnavailable", "Show result component unavailable.");
        internal static readonly string TextInvalidTourId = ModLocalization.Get("TextInvalidTourId", "Invalid tour id.");
        internal static readonly string TextTourNotFound = ModLocalization.Get("TextTourNotFound", "Tour not found.");
        internal static readonly string TextTourPopupUnavailable = ModLocalization.Get("TextTourPopupUnavailable", "Tour popup unavailable.");
        internal static readonly string TextTourComponentUnavailable = ModLocalization.Get("TextTourComponentUnavailable", "Tour component unavailable.");
        internal static readonly string TextInvalidElectionId = ModLocalization.Get("TextInvalidElectionId", "Invalid election id.");
        internal static readonly string TextElectionNotFound = ModLocalization.Get("TextElectionNotFound", "Election not found.");
        internal static readonly string TextElectionPopupUnavailable = ModLocalization.Get("TextElectionPopupUnavailable", "Election popup unavailable.");
        internal static readonly string TextElectionComponentUnavailable = ModLocalization.Get("TextElectionComponentUnavailable", "Election component unavailable.");
        internal static readonly string TextSingleParticipationRecorded = ModLocalization.Get("TextSingleParticipationRecorded", "Single Participation Recorded");
        internal static readonly string TextSingleReleased = ModLocalization.Get("TextSingleReleased", "Single Released");
        internal static readonly string TextSingle = ModLocalization.Get("TextSingle", "Single #");
        internal static readonly string TextSenbatsuRow = ModLocalization.Get("TextSenbatsuRow", "Senbatsu Row: ");
        internal static readonly string TextPosition = ModLocalization.Get("TextPosition", "Position: ");
        internal static readonly string TextCenter = ModLocalization.Get("TextCenter", "Center: ");
        internal static readonly string TextChartPosition = ModLocalization.Get("TextChartPosition", "Chart Position: #");
        internal static readonly string TextIdolStatusUpdated = ModLocalization.Get("TextIdolStatusUpdated", "Idol Status Updated");
        internal static readonly string TextDatingStatusUpdated = ModLocalization.Get("TextDatingStatusUpdated", "Dating Status Updated");
        internal static readonly string TextRoute = ModLocalization.Get("TextRoute", "Route: ");
        internal static readonly string TextStage = ModLocalization.Get("TextStage", "Stage: ");
        internal static readonly string TextDatingPartnerStatusUpdated = ModLocalization.Get("TextDatingPartnerStatusUpdated", "Dating Partner Status Updated");
        internal static readonly string TextDatingHistoryHasDatedAnotherIdol = ModLocalization.Get("TextDatingHistoryHasDatedAnotherIdol", "Dating History: Has dated another idol");
        internal static readonly string TextScandalHistoryYes = ModLocalization.Get("TextScandalHistoryYes", "Scandal History: Yes");
        internal static readonly string TextUsedGoodsStatusYes = ModLocalization.Get("TextUsedGoodsStatusYes", "Known Dating History: Yes");
        internal static readonly string TextStartedDatingAnotherIdol = ModLocalization.Get("TextStartedDatingAnotherIdol", "Started Dating Another Idol");
        internal static readonly string TextDatingEnded = ModLocalization.Get("TextDatingEnded", "Dating Ended");
        internal static readonly string TextRelationshipStatusUpdated = ModLocalization.Get("TextRelationshipStatusUpdated", "Relationship Status Updated");
        internal static readonly string TextDynamic = ModLocalization.Get("TextDynamic", "Relationship Trend: ");
        internal static readonly string TextPairStateDating = ModLocalization.Get("TextPairStateDating", "Pair State: Dating");
        internal static readonly string TextRelationshipScore = ModLocalization.Get("TextRelationshipScore", "Relationship Score: ");
        internal const string FormatSingleMetricTwoDecimals = "0.##";
        internal static readonly string TextBullyingStarted = ModLocalization.Get("TextBullyingStarted", "Bullying Started");
        internal static readonly string TextBullyingEnded = ModLocalization.Get("TextBullyingEnded", "Bullying Ended");
        internal static readonly string TextTargetIdol = ModLocalization.Get("TextTargetIdol", "Target Idol: ");
        internal static readonly string TextLeaderIdol = ModLocalization.Get("TextLeaderIdol", "Leader Idol: ");
        internal static readonly string TextKnownToProducer = ModLocalization.Get("TextKnownToProducer", "Known to Producer: ");
        internal static readonly string TextContractSigned = ModLocalization.Get("TextContractSigned", "Contract Signed");
        internal static readonly string TextContractBroken = ModLocalization.Get("TextContractBroken", "Contract Broken");
        internal static readonly string TextContractCompleted = ModLocalization.Get("TextContractCompleted", "Contract Completed");
        internal static readonly string TextAgent = ModLocalization.Get("TextAgent", "Agent: ");
        internal static readonly string TextProduct = ModLocalization.Get("TextProduct", "Product: ");
        internal static readonly string TextAgency = ModLocalization.Get("TextAgency", "Agency: ");
        internal static readonly string TextContractTypePrefix = ModLocalization.Get("TextContractTypePrefix", "Contract Type: ");
        internal static readonly string TextContractFocusPrefix = ModLocalization.Get("TextContractFocusPrefix", "Contract Focus: ");
        internal static readonly string TextScopeGroupContract = ModLocalization.Get("TextScopeGroupContract", "Contract Applies To: Entire Group");
        internal static readonly string TextStartDatePrefix = ModLocalization.Get("TextStartDatePrefix", "Start Date: ");
        internal static readonly string TextEndDatePrefix = ModLocalization.Get("TextEndDatePrefix", "End Date: ");
        internal static readonly string TextDuration = ModLocalization.Get("TextDuration", "Duration: ");
        internal static readonly string TextMonthS = ModLocalization.Get("TextMonthS", " month(s)");
        internal static readonly string TextActivationImmediate = ModLocalization.Get("TextActivationImmediate", "Activation: Immediate");
        internal static readonly string TextWeeklyPayPrefix = ModLocalization.Get("TextWeeklyPayPrefix", "Weekly Pay: ");
        internal static readonly string TextWeeklyBuzzPrefix = ModLocalization.Get("TextWeeklyBuzzPrefix", "Weekly Buzz: ");
        internal static readonly string TextWeeklyFamePrefix = ModLocalization.Get("TextWeeklyFamePrefix", "Weekly Fame: ");
        internal static readonly string TextWeeklyFansPrefix = ModLocalization.Get("TextWeeklyFansPrefix", "Weekly Fans: ");
        internal static readonly string TextWeeklyStaminaPrefix = ModLocalization.Get("TextWeeklyStaminaPrefix", "Weekly Stamina: ");
        internal static readonly string TextDamagesPaid = ModLocalization.Get("TextDamagesPaid", "Damages Paid: ");
        internal static readonly string TextBreakContext = ModLocalization.Get("TextBreakContext", "Contract Break Trigger: ");
        internal static readonly string TextDamagesAppliedYes = ModLocalization.Get("TextDamagesAppliedYes", "Damages Applied: Yes");
        internal static readonly string TextCompletionContractReachedItsEndDate = ModLocalization.Get("TextCompletionContractReachedItsEndDate", "Completion: Contract reached its end date.");
        internal static readonly string TextWeeklyContractBenefitsApplied = ModLocalization.Get("TextWeeklyContractBenefitsApplied", "Weekly Contract Benefits Applied");
        internal static readonly string TextWeeklyContractPaymentReceived = ModLocalization.Get("TextWeeklyContractPaymentReceived", "Weekly Contract Payment Received");
        internal static readonly string TextFans = ModLocalization.Get("TextFans", "Fans: ");
        internal static readonly string TextBuzz = ModLocalization.Get("TextBuzz", "Buzz: ");
        internal static readonly string TextFame = ModLocalization.Get("TextFame", "Fame: ");
        internal static readonly string TextStamina = ModLocalization.Get("TextStamina", "Stamina: ");
        internal static readonly string TextTrainingPoints = ModLocalization.Get("TextTrainingPoints", "Training Points: ");
        internal static readonly string TextContractEnd = ModLocalization.Get("TextContractEnd", "Contract End: ");
        internal static readonly string TextSalaryTitlePrefix = ModLocalization.Get("TextSalaryTitlePrefix", "Salary ");
        internal static readonly string TextSalaryPrefix = ModLocalization.Get("TextSalaryPrefix", "Salary: ");
        internal static readonly string TextChange = ModLocalization.Get("TextChange", "Change: ");
        internal static readonly string TextSalarySatisfaction = ModLocalization.Get("TextSalarySatisfaction", "Salary Satisfaction: ");
        internal static readonly string TextSatisfactionChange = ModLocalization.Get("TextSatisfactionChange", "Satisfaction Change: ");
        internal static readonly string TextTvShowEpisodeReleased = ModLocalization.Get("TextTvShowEpisodeReleased", "TV Show Episode Released");
        internal static readonly string TextTvShowStatusUpdated = ModLocalization.Get("TextTvShowStatusUpdated", "TV Show Status Updated");
        internal static readonly string TextTvShow = ModLocalization.Get("TextTvShow", "TV Show #");
        internal static readonly string TextShowEpisodeReleased = ModLocalization.Get("TextShowEpisodeReleased", "Show Episode Released");
        internal static readonly string TextShowStatusUpdated = ModLocalization.Get("TextShowStatusUpdated", "Show Status Updated");
        internal static readonly string TextShow = ModLocalization.Get("TextShow", "Show #");
        internal static readonly string TextTheaterDailyPerformance = ModLocalization.Get("TextTheaterDailyPerformance", "Theater Daily Performance");
        internal static readonly string TextTheaterOpened = ModLocalization.Get("TextTheaterOpened", "Theater Opened");
        internal static readonly string TextTheaterClosed = ModLocalization.Get("TextTheaterClosed", "Theater Closed");
        internal static readonly string TextTheater = ModLocalization.Get("TextTheater", "Theater #");
        internal static readonly string TextProgramType = ModLocalization.Get("TextProgramType", "Theater Schedule: {0}");
        internal static readonly string TextAttendance = ModLocalization.Get("TextAttendance", "Attendance: ");
        internal static readonly string TextRevenue = ModLocalization.Get("TextRevenue", "Revenue: ");
        internal static readonly string TextSubscriberChange = ModLocalization.Get("TextSubscriberChange", "Subscriber Change: ");
        internal static readonly string TextSubscribers = ModLocalization.Get("TextSubscribers", "Subscribers: ");
        internal static readonly string TextSevenDayAverageAttendance = ModLocalization.Get("TextSevenDayAverageAttendance", "7-Day Average Attendance: ");
        internal static readonly string TextSevenDayAverageRevenue = ModLocalization.Get("TextSevenDayAverageRevenue", "7-Day Average Revenue: ");
        internal static readonly string TextAgencyActivityPerformance = ModLocalization.Get("TextAgencyActivityPerformance", "Agency Activity: Performance");
        internal static readonly string TextAgencyActivityPromotion = ModLocalization.Get("TextAgencyActivityPromotion", "Agency Activity: Promotion");
        internal static readonly string TextAgencyActivitySpaTreatment = ModLocalization.Get("TextAgencyActivitySpaTreatment", "Agency Activity: Spa Treatment");
        internal static readonly string TextAgencyWide = ModLocalization.Get("TextAgencyWide", "Agency-Wide");
        internal static readonly string TextMoneyChangePrefix = ModLocalization.Get("TextMoneyChangePrefix", "Money Change: ");
        internal static readonly string TextFanChange = ModLocalization.Get("TextFanChange", "Fan Change: ");
        internal static readonly string TextPerIdolEarnings = ModLocalization.Get("TextPerIdolEarnings", "Per-Idol Earnings: ");
        internal static readonly string TextStaminaCost = ModLocalization.Get("TextStaminaCost", "Stamina Cost: ");
        internal static readonly string TextSpaRecovery = ModLocalization.Get("TextSpaRecovery", "Spa Recovery: ");
        internal static readonly string TextTourStatusUpdated = ModLocalization.Get("TextTourStatusUpdated", "Tour Status Updated");
        internal static readonly string TextCountries = ModLocalization.Get("TextCountries", "Countries: ");
        internal static readonly string TextAudience = ModLocalization.Get("TextAudience", "Audience: ");
        internal static readonly string TextNewFansPrefix = ModLocalization.Get("TextNewFansPrefix", "New Fans: ");
        internal static readonly string TextElectionStatusUpdated = ModLocalization.Get("TextElectionStatusUpdated", "Election Status Updated");
        internal static readonly string TextElectionResultRecorded = ModLocalization.Get("TextElectionResultRecorded", "Election Result Recorded");
        internal static readonly string TextBroadcast = ModLocalization.Get("TextBroadcast", "Broadcast: ");
        internal static readonly string TextFinalPlace = ModLocalization.Get("TextFinalPlace", "Final Place: #");
        internal static readonly string TextScandalPointsUpdated = ModLocalization.Get("TextScandalPointsUpdated", "Scandal Points Updated");
        internal static readonly string TextScandalPointsPrefix = ModLocalization.Get("TextScandalPointsPrefix", "Scandal Points: ");
        internal static readonly string TextDelta = ModLocalization.Get("TextDelta", "Scandal Point Change: ");
        internal static readonly string TextMedicalInjury = ModLocalization.Get("TextMedicalInjury", "Medical Injury");
        internal static readonly string TextMedicalDepression = ModLocalization.Get("TextMedicalDepression", "Medical Depression");
        internal static readonly string TextMedicalHiatusStarted = ModLocalization.Get("TextMedicalHiatusStarted", "Medical Hiatus Started");
        internal static readonly string TextMedicalRecovery = ModLocalization.Get("TextMedicalRecovery", "Medical Recovery");
        internal static readonly string TextMedicalHiatusFinished = ModLocalization.Get("TextMedicalHiatusFinished", "Medical Hiatus Finished");
        internal static readonly string TextHiatusEnd = ModLocalization.Get("TextHiatusEnd", "Hiatus End: ");
        internal static readonly string TextTimelineEntryRecorded = ModLocalization.Get("TextTimelineEntryRecorded", "Timeline entry recorded.");
        internal static readonly string TextOtherIdol = ModLocalization.Get("TextOtherIdol", "Other Idol: ");
        internal static readonly string TextStatusUpdatedSentence = ModLocalization.Get("TextStatusUpdatedSentence", "Status updated.");
        internal static readonly string LabelShowCastContextTitle = ModLocalization.Get("LabelShowCastContextTitle", "Show Cast");
        internal static readonly string TextShowCastMembersCapturedPrefix = ModLocalization.Get("TextShowCastMembersCapturedPrefix", "Idols involved across this show's run: ");
        internal const string KeyPartnerId = "partner_id";
        internal const string KeyOtherId = "other_id";
        internal const string KeyIdolAId = "idol_a_id";
        internal const string KeyIdolBId = "idol_b_id";
        internal const string SeparatorDoubleSpace = "  ";
        internal const string KeyGroupTitle = "group_title";
        internal const string SuffixExtraCount = " +";
        internal const string FormatEightDigits = "D8";
        internal const string FormatDateKeyYyyyMMdd = "yyyyMMdd";
        internal static readonly string TextTvShowTitlePrefix = ModLocalization.Get("TextTvShowTitlePrefix", "TV Show: ");
        internal static readonly string TextShowTitlePrefix = ModLocalization.Get("TextShowTitlePrefix", "Show: ");
        internal static readonly string TextSingleTitlePrefix = ModLocalization.Get("TextSingleTitlePrefix", "Single: ");
        internal static readonly string TextRelationshipWith = ModLocalization.Get("TextRelationshipWith", "Relationship with {0}");
        internal static readonly string TextBullying = ModLocalization.Get("TextBullying", "Bullying: ");
        internal static readonly string TextRelationshipUnknownToProducer = ModLocalization.Get("TextRelationshipUnknownToProducer", "Relationship unknown to producer.");
        internal static readonly string TextBullyingParticipantsUnknownToProducer = ModLocalization.Get("TextBullyingParticipantsUnknownToProducer", "Bullying participants unknown to producer.");
        internal static readonly string TextTheaterTitlePrefix = ModLocalization.Get("TextTheaterTitlePrefix", "Theater: ");
        internal static readonly string TextAgencyActivity = ModLocalization.Get("TextAgencyActivity", "Agency Activity: ");
        internal static readonly string TextAgencyActivityFallback = ModLocalization.Get("TextAgencyActivityFallback", "Agency Activity");
        internal static readonly string TextContract = ModLocalization.Get("TextContract", "Contract: ");
        internal const string SeparatorSpaceHash = " #";
        internal const string CodeLabelLocalizationKeyPrefix = "CodeLabel.";
        internal const string CodeEntireGroup = "entiregroup";
        internal static readonly string TextEntireGroup = ModLocalization.Get("TextEntireGroup", "Entire Group");
        internal const string CodeRotatingCast = "rotatingcast";
        internal static readonly string TextRotatingCast = ModLocalization.Get("TextRotatingCast", "Rotating Cast");
        internal const string CodePermanentCast = "permanentcast";
        internal static readonly string TextPermanentCast = ModLocalization.Get("TextPermanentCast", "Permanent Cast");
        internal const string CodeActivityPrefix = "activity.";
        internal const string CodeEveryone = "everyone";
        internal const string CodePlayer = "player";
        internal const string CodeBusiness = "business";
        internal const string CodeStamina = "stamina";
        internal const string CodeStats = "stats";
        internal const string KeyTakenIdol = "taken_idol";
        internal static readonly string TextDatingAnotherIdol = ModLocalization.Get("TextDatingAnotherIdol", "Dating Another Idol");
        internal const string KeyTakenPlayer = "taken_player";
        internal static readonly string TextDatingProducer = ModLocalization.Get("TextDatingProducer", "Dating Producer");
        internal const string KeyTakenOutsideBf = "taken_outside_bf";
        internal static readonly string TextDatingNonIdolBoyfriend = ModLocalization.Get("TextDatingNonIdolBoyfriend", "Dating Non-Idol (Boyfriend)");
        internal const string KeyTakenOutsideGf = "taken_outside_gf";
        internal static readonly string TextDatingNonIdolGirlfriend = ModLocalization.Get("TextDatingNonIdolGirlfriend", "Dating Non-Idol (Girlfriend)");
        internal const string CodeFree = "free";
        internal const string CodeTv = "Tv";
        internal const string CodeSsk = "Ssk";
        internal const string CodeIncreased = "increased";
        internal const string CodeDecreased = "decreased";
        internal const string CodeLowered = "lowered";
        internal static readonly string LabelFree = ModLocalization.Get("LabelFree","Free");
        internal static readonly string LabelTV = ModLocalization.Get("LabelTV","TV");
        internal static readonly string LabelSSK = ModLocalization.Get("LabelSSK","Idol Election");
        internal static readonly string LabelIncreased = ModLocalization.Get("LabelIncreased", "Increased");
        internal static readonly string LabelDecreased = ModLocalization.Get("LabelDecreased","Decreased");
        internal static readonly string LabelLowered = ModLocalization.Get("LabelLowered","Lowered");
        internal static readonly string LabelReduced = ModLocalization.Get("LabelReduced", "Reduced");
        internal const string KeyManualSet = "manual_set";
        internal static readonly string LabelSet = ModLocalization.Get("LabelSet","Set");
        internal static readonly string LabelSaved = ModLocalization.Get("LabelSaved","Saved");
        internal const string CodeSet = "set";
        internal const string CodeSaved = "saved";
        internal static readonly string LabelUpdated = ModLocalization.Get("LabelUpdated", "Updated");
        internal static readonly string LabelYes = ModLocalization.Get("LabelYes", "Yes");
        internal static readonly string LabelNo = ModLocalization.Get("LabelNo", "No");
        internal const string FormatZeroZeroZero = "+#,0;-#,0;0";
        internal static readonly string TextGroup = ModLocalization.Get("TextGroup", "Group #");
        internal static readonly string TextFilterModeInclude = ModLocalization.Get("TextFilterModeInclude", "Mode: Include");
        internal static readonly string TextFilterModeExclude = ModLocalization.Get("TextFilterModeExclude", "Mode: Exclude");
        internal const string UiNameCareerDiaryTitlePrefix = "CareerDiary_Title_";
        internal const string UiNameCareerDiaryTextPrefix = "CareerDiary_Text_";
        internal const string UiNameCareerDiaryDividerPrefix = "CareerDiary_Divider_";
        internal const string TypeNameSuperBlur = "SuperBlur";
        internal const string TypeNameSuperBlurFast = "SuperBlurFast";
        internal const string MemberNameInterpolation = "interpolation";
        internal const string MemberNameStart = "Start";
        internal static readonly string TextImUiFrameworkInitializationCallFailed = ModLocalization.Get("TextImUiFrameworkInitializationCallFailed", "IMUIFramework initialization call failed: ");
        internal const string MemberNameLoadSenbatsu = "LoadSenbatsu";

        // Literal constants used for numeric cleanup.
        internal const int ImDataCoreCustomJsonMethodParameterCount = 4;
        internal const int ImDataCoreAppendCustomEventMethodParameterCount = 8;
        internal const int ImDataCoreRecentEventsMethodParameterCount = 4;
        internal const int ShowCastSummaryMaxNames = 4;
        internal const int RelationshipMetadataMinimumFieldCount = 4;
        internal const long LongZero = 0L;
        internal const int UiFrameworkCreateStyledButtonMethodParameterCount = 6;
        internal const int UiFrameworkCloneStyledButtonMethodParameterCount = 8;
        internal const int HumanizeCodeBufferPadding = 8;
        internal const float FloatOne = 1f;
        internal const float FloatZero = 0f;
        internal const float SingleMetricPercentMax = 100f;
        internal const float SingleMetricScoreMax = 10f;
        internal const float SingleMetricRoundingTolerance = 0.01f;
        internal const int FallbackBodyTextFontSize = 18;
        internal const float FallbackStyledButtonWidth = 180f;
        internal const float HeaderPlacementHalfScale = 0.5f;
        internal const float PopupRecoveryTickIntervalSeconds = 0.5f;
        internal const float PopupRecoveryInitialDelaySeconds = 0.35f;
}

    /// <summary>
    /// Stable logger helper.
    /// </summary>
    internal static class Log
    {
        /// <summary>
        /// Writes info logs.
        /// </summary>
        internal static void Info(string message)
        {
            Debug.Log(C.LogPrefix + message);
        }

        /// <summary>
        /// Writes warning logs.
        /// </summary>
        internal static void Warn(string message)
        {
            Debug.LogWarning(C.LogPrefix + message);
        }

        /// <summary>
        /// Writes error logs.
        /// </summary>
        internal static void Error(string message)
        {
            Debug.LogError(C.LogPrefix + message);
        }
    }

    /// <summary>
    /// Opaque IM Data Core session wrapper. Backed by reflected runtime type.
    /// </summary>
    internal sealed class IMDataCoreSession
    {
        internal IMDataCoreSession(object rawSession)
        {
            RawSession = rawSession;
        }

        internal object RawSession { get; private set; }
    }

    /// <summary>
    /// Local event DTO mapped from IM Data Core reflected event objects.
    /// </summary>
    public sealed class IMDataCoreEvent
    {
        public long EventId { get; internal set; }
        public int GameDateKey { get; internal set; }
        public string GameDateTime { get; internal set; }
        public int IdolId { get; internal set; }
        public string EntityKind { get; internal set; }
        public string EntityId { get; internal set; }
        public string EventType { get; internal set; }
        public string SourcePatch { get; internal set; }
        public string PayloadJson { get; internal set; }
        public string NamespaceId { get; internal set; }
    }

    /// <summary>
    /// Reflection bridge to required IM Data Core mod API.
    /// </summary>
    internal static class IMDataCoreApi
    {
        private const string AssemblyNameImDataCore = C.HarmonyIdImDataCore;
        private const string TypeNameApi = C.IdImDataCoreImDataCoreApi;
        private const string TypeNameApiAlias = C.IdImDataCoreImDataCoreApi;
        private const string TypeNameSession = C.IdImDataCoreImDataCoreSession;
        private const string TypeNameEvent = C.IdImDataCoreImDataCoreEvent;
        private const int ResolveRetrySeconds = C.CoreRegistrationRetrySeconds;

        private static readonly object Sync = new object();
        private static DateTime nextResolveAttemptUtc = DateTime.MinValue;
        private static string lastResolveError = string.Empty;

        private static Type apiType;
        private static Type sessionType;
        private static Type eventType;

        private static MethodInfo methodIsReady;
        private static MethodInfo methodTryRegisterNamespace;
        private static MethodInfo methodTryGetCustomJson;
        private static MethodInfo methodTrySetCustomJson;
        private static MethodInfo methodTryAppendCustomEvent;
        private static MethodInfo methodTryReadRecentEventsForIdol;

        private static PropertyInfo propertyEventId;
        private static PropertyInfo propertyGameDateKey;
        private static PropertyInfo propertyGameDateTime;
        private static PropertyInfo propertyIdolId;
        private static PropertyInfo propertyEntityKind;
        private static PropertyInfo propertyEntityId;
        private static PropertyInfo propertyEventType;
        private static PropertyInfo propertySourcePatch;
        private static PropertyInfo propertyPayloadJson;
        private static PropertyInfo propertyNamespaceId;

        internal static bool TryResolveDependency(out string errorMessage)
        {
            return TryEnsureBridgeReady(out errorMessage);
        }

        internal static bool IsReady()
        {
            string errorMessage;
            if (!TryEnsureBridgeReady(out errorMessage))
            {
                return false;
            }

            object result;
            if (!TryInvokeBool(methodIsReady, null, out result, out errorMessage))
            {
                return false;
            }

            return result is bool && (bool)result;
        }

        internal static bool TryRegisterNamespace(string namespaceIdentifier, out IMDataCoreSession session, out string errorMessage)
        {
            session = null;
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(namespaceIdentifier))
            {
                errorMessage = C.TextNamespaceIsEmpty;
                return false;
            }

            if (!TryEnsureBridgeReady(out errorMessage))
            {
                return false;
            }

            object[] args = new object[] { namespaceIdentifier, null, string.Empty };
            object invokeResult;
            if (!TryInvokeBool(methodTryRegisterNamespace, args, out invokeResult, out errorMessage))
            {
                return false;
            }

            bool success = invokeResult is bool && (bool)invokeResult;
            if (!success)
            {
                errorMessage = CoalesceOutString(args, C.TimelineActionButtonsPerRow, errorMessage);
                return false;
            }

            if (args[C.LastFromCount] == null)
            {
                errorMessage = C.TextImDataCoreReturnedAnEmptySession;
                return false;
            }

            session = new IMDataCoreSession(args[C.LastFromCount]);
            errorMessage = CoalesceOutString(args, C.TimelineActionButtonsPerRow, string.Empty);
            return true;
        }

        internal static bool TryGetCustomJson(IMDataCoreSession session, string dataKey, out string jsonValue, out string errorMessage)
        {
            jsonValue = string.Empty;
            errorMessage = string.Empty;

            if (session == null || session.RawSession == null)
            {
                errorMessage = C.TextImDataCoreSessionIsUnavailable;
                return false;
            }

            if (!TryEnsureBridgeReady(out errorMessage))
            {
                return false;
            }

            object[] args = new object[] { session.RawSession, dataKey, null, string.Empty };
            object invokeResult;
            if (!TryInvokeBool(methodTryGetCustomJson, args, out invokeResult, out errorMessage))
            {
                return false;
            }

            bool success = invokeResult is bool && (bool)invokeResult;
            jsonValue = args[C.TimelineActionButtonsPerRow] as string ?? string.Empty;
            if (!success)
            {
                errorMessage = CoalesceOutString(args, C.TimelineFilterButtonsPerRow, errorMessage);
                return false;
            }

            errorMessage = CoalesceOutString(args, C.TimelineFilterButtonsPerRow, string.Empty);
            return true;
        }

        internal static bool TrySetCustomJson(IMDataCoreSession session, string dataKey, string jsonValue, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (session == null || session.RawSession == null)
            {
                errorMessage = C.TextImDataCoreSessionIsUnavailable;
                return false;
            }

            if (!TryEnsureBridgeReady(out errorMessage))
            {
                return false;
            }

            object[] args = new object[] { session.RawSession, dataKey, jsonValue, string.Empty };
            object invokeResult;
            if (!TryInvokeBool(methodTrySetCustomJson, args, out invokeResult, out errorMessage))
            {
                return false;
            }

            bool success = invokeResult is bool && (bool)invokeResult;
            if (!success)
            {
                errorMessage = CoalesceOutString(args, C.TimelineFilterButtonsPerRow, errorMessage);
                return false;
            }

            errorMessage = CoalesceOutString(args, C.TimelineFilterButtonsPerRow, string.Empty);
            return true;
        }

        internal static bool TryAppendCustomEvent(
            IMDataCoreSession session,
            int idolId,
            string entityKind,
            string entityId,
            string eventType,
            string payloadJson,
            string sourcePatch,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (session == null || session.RawSession == null)
            {
                errorMessage = C.TextImDataCoreSessionIsUnavailable;
                return false;
            }

            if (!TryEnsureBridgeReady(out errorMessage))
            {
                return false;
            }

            if (methodTryAppendCustomEvent == null)
            {
                errorMessage = C.TextImDataCoreAppendCustomEventMethodIsUnavailable;
                return false;
            }

            object[] args = new object[]
            {
                session.RawSession,
                idolId,
                entityKind ?? string.Empty,
                entityId ?? string.Empty,
                eventType ?? string.Empty,
                payloadJson ?? "{}",
                sourcePatch ?? string.Empty,
                string.Empty
            };

            object invokeResult;
            if (!TryInvokeBool(methodTryAppendCustomEvent, args, out invokeResult, out errorMessage))
            {
                return false;
            }

            bool success = invokeResult is bool && (bool)invokeResult;
            if (!success)
            {
                errorMessage = CoalesceOutString(args, args.Length - C.LastFromCount, errorMessage);
                return false;
            }

            errorMessage = CoalesceOutString(args, args.Length - C.LastFromCount, string.Empty);
            return true;
        }

        internal static bool TryReadRecentEventsForIdol(int idolId, int maxCount, out List<IMDataCoreEvent> events, out string errorMessage)
        {
            events = new List<IMDataCoreEvent>();
            errorMessage = string.Empty;

            if (!TryEnsureBridgeReady(out errorMessage))
            {
                return false;
            }

            object[] args = new object[] { idolId, maxCount, null, string.Empty };
            object invokeResult;
            if (!TryInvokeBool(methodTryReadRecentEventsForIdol, args, out invokeResult, out errorMessage))
            {
                return false;
            }

            bool success = invokeResult is bool && (bool)invokeResult;
            if (!success)
            {
                errorMessage = CoalesceOutString(args, C.TimelineFilterButtonsPerRow, errorMessage);
                return false;
            }

            IEnumerable<object> mapped = EnumerateObjects(args[C.TimelineActionButtonsPerRow]);
            foreach (object rawEvent in mapped)
            {
                IMDataCoreEvent mappedEvent = MapEvent(rawEvent);
                if (mappedEvent != null)
                {
                    events.Add(mappedEvent);
                }
            }

            errorMessage = CoalesceOutString(args, C.TimelineFilterButtonsPerRow, string.Empty);
            return true;
        }

        private static bool TryEnsureBridgeReady(out string errorMessage)
        {
            lock (Sync)
            {
                if (apiType != null && methodIsReady != null)
                {
                    errorMessage = string.Empty;
                    return true;
                }

                if (DateTime.UtcNow < nextResolveAttemptUtc)
                {
                    errorMessage = string.IsNullOrEmpty(lastResolveError) ? C.TextImDataCoreBridgeIsRetrying : lastResolveError;
                    return false;
                }

                if (!TryResolveBridge(out errorMessage))
                {
                    lastResolveError = errorMessage;
                    nextResolveAttemptUtc = DateTime.UtcNow.AddSeconds(ResolveRetrySeconds);
                    return false;
                }

                lastResolveError = string.Empty;
                nextResolveAttemptUtc = DateTime.MinValue;
                return true;
            }
        }

        private static bool TryResolveBridge(out string errorMessage)
        {
            errorMessage = string.Empty;

            Assembly targetAssembly = FindLoadedAssembly(AssemblyNameImDataCore);
            if (targetAssembly == null)
            {
                try
                {
                    targetAssembly = Assembly.Load(AssemblyNameImDataCore);
                }
                catch
                {
                    targetAssembly = null;
                }
            }

            if (targetAssembly == null)
            {
                bool hasCorePatches = false;
                try
                {
                    hasCorePatches = Harmony.HasAnyPatches(C.HarmonyIdImDataCore);
                }
                catch
                {
                    hasCorePatches = false;
                }

                errorMessage = hasCorePatches
                    ? C.TextImDataCorePatchesArePresentButApiAssemblyCouldNotBeResolved
                    : C.TextImDataCoreModIsNotLoaded;
                return false;
            }

            apiType = targetAssembly.GetType(TypeNameApi, false);
            if (apiType == null)
            {
                apiType = targetAssembly.GetType(TypeNameApiAlias, false);
            }
            sessionType = targetAssembly.GetType(TypeNameSession, false);
            eventType = targetAssembly.GetType(TypeNameEvent, false);

            if (apiType == null || sessionType == null || eventType == null)
            {
                errorMessage = C.TextImDataCoreApiTypesWereNotFoundInAssembly;
                return false;
            }

            methodIsReady = FindMethod(apiType, C.MemberNameIsReady, C.MinId);
            methodTryRegisterNamespace = FindMethod(apiType, C.MemberNameTryRegisterNamespace, C.TimelineFilterButtonsPerRow);
            methodTryGetCustomJson = FindMethod(apiType, C.MemberNameTryGetCustomJson, C.ImDataCoreCustomJsonMethodParameterCount);
            methodTrySetCustomJson = FindMethod(apiType, C.MemberNameTrySetCustomJson, C.ImDataCoreCustomJsonMethodParameterCount);
            methodTryAppendCustomEvent = FindMethod(apiType, C.MemberNameTryAppendCustomEvent, C.ImDataCoreAppendCustomEventMethodParameterCount);
            methodTryReadRecentEventsForIdol = FindMethod(apiType, C.MemberNameTryReadRecentEventsForIdol, C.ImDataCoreRecentEventsMethodParameterCount);

            if (methodIsReady == null ||
                methodTryRegisterNamespace == null ||
                methodTryGetCustomJson == null ||
                methodTrySetCustomJson == null ||
                methodTryReadRecentEventsForIdol == null)
            {
                errorMessage = C.TextImDataCoreApiMethodSignatureMismatch;
                return false;
            }

            propertyEventId = eventType.GetProperty(C.MemberNameEventId, BindingFlags.Public | BindingFlags.Instance);
            propertyGameDateKey = eventType.GetProperty(C.MemberNameGameDateKey, BindingFlags.Public | BindingFlags.Instance);
            propertyGameDateTime = eventType.GetProperty(C.MemberNameGameDateTime, BindingFlags.Public | BindingFlags.Instance);
            propertyIdolId = eventType.GetProperty(C.MemberNameIdolId, BindingFlags.Public | BindingFlags.Instance);
            propertyEntityKind = eventType.GetProperty(C.MemberNameEntityKind, BindingFlags.Public | BindingFlags.Instance);
            propertyEntityId = eventType.GetProperty(C.MemberNameEntityId, BindingFlags.Public | BindingFlags.Instance);
            propertyEventType = eventType.GetProperty(C.MemberNameEventType, BindingFlags.Public | BindingFlags.Instance);
            propertySourcePatch = eventType.GetProperty(C.MemberNameSourcePatch, BindingFlags.Public | BindingFlags.Instance);
            propertyPayloadJson = eventType.GetProperty(C.MemberNamePayloadJson, BindingFlags.Public | BindingFlags.Instance);
            propertyNamespaceId = eventType.GetProperty(C.MemberNameNamespaceId, BindingFlags.Public | BindingFlags.Instance);

            if (propertyEventId == null ||
                propertyGameDateKey == null ||
                propertyGameDateTime == null ||
                propertyIdolId == null ||
                propertyEntityKind == null ||
                propertyEntityId == null ||
                propertyEventType == null ||
                propertySourcePatch == null ||
                propertyPayloadJson == null)
            {
                errorMessage = C.TextImDataCoreEventPropertySignatureMismatch;
                return false;
            }

            return true;
        }

        private static Assembly FindLoadedAssembly(string assemblyName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = C.ZeroIndex; i < assemblies.Length; i++)
            {
                Assembly asm = assemblies[i];
                if (asm == null)
                {
                    continue;
                }

                AssemblyName name = asm.GetName();
                if (name != null && string.Equals(name.Name, assemblyName, StringComparison.OrdinalIgnoreCase))
                {
                    return asm;
                }
            }

            return null;
        }

        private static MethodInfo FindMethod(Type type, string methodName, int parameterCount)
        {
            if (type == null || string.IsNullOrEmpty(methodName))
            {
                return null;
            }

            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
            for (int i = C.ZeroIndex; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method == null)
                {
                    continue;
                }

                if (!string.Equals(method.Name, methodName, StringComparison.Ordinal))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters != null && parameters.Length == parameterCount)
                {
                    return method;
                }
            }

            return null;
        }

        private static bool TryInvokeBool(MethodInfo method, object[] args, out object result, out string errorMessage)
        {
            result = false;
            errorMessage = string.Empty;

            if (method == null)
            {
                errorMessage = C.TextImDataCoreBridgeMethodIsUnavailable;
                return false;
            }

            try
            {
                result = method.Invoke(null, args);
                return true;
            }
            catch (TargetInvocationException exception)
            {
                Exception inner = exception.InnerException ?? exception;
                errorMessage = inner.Message;
                return false;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        private static IEnumerable<object> EnumerateObjects(object listObject)
        {
            if (listObject == null)
            {
                yield break;
            }

            System.Collections.IEnumerable enumerable = listObject as System.Collections.IEnumerable;
            if (enumerable == null)
            {
                yield break;
            }

            foreach (object current in enumerable)
            {
                yield return current;
            }
        }

        private static IMDataCoreEvent MapEvent(object rawEvent)
        {
            if (rawEvent == null)
            {
                return null;
            }

            IMDataCoreEvent mapped = new IMDataCoreEvent();
            mapped.EventId = ReadLong(rawEvent, propertyEventId);
            mapped.GameDateKey = ReadInt(rawEvent, propertyGameDateKey);
            mapped.GameDateTime = ReadString(rawEvent, propertyGameDateTime);
            mapped.IdolId = ReadInt(rawEvent, propertyIdolId);
            mapped.EntityKind = ReadString(rawEvent, propertyEntityKind);
            mapped.EntityId = ReadString(rawEvent, propertyEntityId);
            mapped.EventType = ReadString(rawEvent, propertyEventType);
            mapped.SourcePatch = ReadString(rawEvent, propertySourcePatch);
            mapped.PayloadJson = ReadString(rawEvent, propertyPayloadJson);
            mapped.NamespaceId = ReadString(rawEvent, propertyNamespaceId);
            return mapped;
        }

        private static long ReadLong(object source, PropertyInfo property)
        {
            if (source == null || property == null)
            {
                return C.LongZero;
            }

            try
            {
                object value = property.GetValue(source, null);
                if (value == null)
                {
                    return C.LongZero;
                }

                if (value is long)
                {
                    return (long)value;
                }

                if (value is int)
                {
                    return (int)value;
                }

                long parsed;
                if (long.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                {
                    return parsed;
                }

                return C.LongZero;
            }
            catch
            {
                return C.LongZero;
            }
        }

        private static int ReadInt(object source, PropertyInfo property)
        {
            if (source == null || property == null)
            {
                return C.InvalidId;
            }

            try
            {
                object value = property.GetValue(source, null);
                if (value == null)
                {
                    return C.InvalidId;
                }

                if (value is int)
                {
                    return (int)value;
                }

                if (value is long)
                {
                    long longValue = (long)value;
                    if (longValue > int.MaxValue || longValue < int.MinValue)
                    {
                        return C.InvalidId;
                    }

                    return (int)longValue;
                }

                int parsed;
                if (int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                {
                    return parsed;
                }

                return C.InvalidId;
            }
            catch
            {
                return C.InvalidId;
            }
        }

        private static string ReadString(object source, PropertyInfo property)
        {
            if (source == null || property == null)
            {
                return string.Empty;
            }

            try
            {
                object value = property.GetValue(source, null);
                return value == null ? string.Empty : value.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string CoalesceOutString(object[] args, int index, string fallback)
        {
            if (args != null && index >= C.MinId && index < args.Length)
            {
                string value = args[index] as string;
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return fallback ?? string.Empty;
        }
    }

    /// <summary>
    /// Reflection bridge to required IM UI Framework mod API.
    /// </summary>
    internal static class IMUiFrameworkApi
    {
        private const string AssemblyNameImUiFramework = C.HarmonyIdImUiFramework;
        private const string TypeNameUiKit = C.IdImUiFrameworkImUiKit;
        private const int ResolveRetrySeconds = C.CoreRegistrationRetrySeconds;

        private static readonly object Sync = new object();
        private static DateTime nextResolveAttemptUtc = DateTime.MinValue;
        private static string lastResolveError = string.Empty;

        private static Type uiKitType;
        private static MethodInfo methodInitialize;
        private static MethodInfo methodCreateUiObject;
        private static MethodInfo methodClearChildren;
        private static MethodInfo methodRebuildLayout;
        private static MethodInfo methodCreateStyledButton;
        private static MethodInfo methodCloneStyledButton;

        internal static bool TryResolveDependency(out string errorMessage)
        {
            return TryEnsureBridgeReady(out errorMessage);
        }

        internal static bool TryInitialize(PopupManager manager, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (manager == null)
            {
                errorMessage = C.TextPopupManagerIsNull;
                return false;
            }

            if (!TryEnsureBridgeReady(out errorMessage))
            {
                return false;
            }

            return TryInvokeVoid(methodInitialize, new object[] { manager }, out errorMessage);
        }

        internal static bool TryCreateUiObject(string name, Transform parent, out GameObject createdObject, out string errorMessage)
        {
            createdObject = null;
            errorMessage = string.Empty;

            if (!TryEnsureBridgeReady(out errorMessage))
            {
                return false;
            }

            object result;
            if (!TryInvoke(methodCreateUiObject, new object[] { name, parent }, out result, out errorMessage))
            {
                return false;
            }

            createdObject = result as GameObject;
            if (createdObject == null)
            {
                errorMessage = C.TextImUiFrameworkReturnedNullUiObject;
                return false;
            }

            return true;
        }

        internal static bool TryClearChildren(Transform root, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (!TryEnsureBridgeReady(out errorMessage))
            {
                return false;
            }

            return TryInvokeVoid(methodClearChildren, new object[] { root }, out errorMessage);
        }

        internal static bool TryRebuildLayout(Transform root, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (!TryEnsureBridgeReady(out errorMessage))
            {
                return false;
            }

            return TryInvokeVoid(methodRebuildLayout, new object[] { root }, out errorMessage);
        }

        internal static bool TryCreateStyledButton(
            Transform parent,
            string name,
            string label,
            float width,
            float height,
            UnityAction onClick,
            out Button button,
            out string errorMessage)
        {
            button = null;
            errorMessage = string.Empty;

            if (!TryEnsureBridgeReady(out errorMessage))
            {
                return false;
            }

            object result;
            if (!TryInvoke(methodCreateStyledButton, new object[] { parent, name, label, width, height, onClick }, out result, out errorMessage))
            {
                return false;
            }

            button = result as Button;
            if (button == null)
            {
                errorMessage = C.TextImUiFrameworkReturnedNullStyledButton;
                return false;
            }

            return true;
        }

        internal static bool TryCloneStyledButton(
            GameObject template,
            Transform parent,
            string name,
            string label,
            string tooltip,
            UnityAction onClick,
            float width,
            float height,
            out Button button,
            out string errorMessage)
        {
            button = null;
            errorMessage = string.Empty;

            if (!TryEnsureBridgeReady(out errorMessage))
            {
                return false;
            }

            object result;
            if (!TryInvoke(
                methodCloneStyledButton,
                new object[] { template, parent, name, label, tooltip, onClick, width, height },
                out result,
                out errorMessage))
            {
                return false;
            }

            button = result as Button;
            if (button == null)
            {
                errorMessage = C.TextImUiFrameworkReturnedNullClonedButton;
                return false;
            }

            return true;
        }

        private static bool TryEnsureBridgeReady(out string errorMessage)
        {
            lock (Sync)
            {
                if (uiKitType != null &&
                    methodInitialize != null &&
                    methodCreateUiObject != null &&
                    methodClearChildren != null &&
                    methodRebuildLayout != null &&
                    methodCreateStyledButton != null &&
                    methodCloneStyledButton != null)
                {
                    errorMessage = string.Empty;
                    return true;
                }

                if (DateTime.UtcNow < nextResolveAttemptUtc)
                {
                    errorMessage = string.IsNullOrEmpty(lastResolveError) ? C.TextImUiFrameworkBridgeIsRetrying : lastResolveError;
                    return false;
                }

                if (!TryResolveBridge(out errorMessage))
                {
                    lastResolveError = errorMessage;
                    nextResolveAttemptUtc = DateTime.UtcNow.AddSeconds(ResolveRetrySeconds);
                    return false;
                }

                lastResolveError = string.Empty;
                nextResolveAttemptUtc = DateTime.MinValue;
                return true;
            }
        }

        private static bool TryResolveBridge(out string errorMessage)
        {
            errorMessage = string.Empty;

            Assembly targetAssembly = FindLoadedAssembly(AssemblyNameImUiFramework);
            if (targetAssembly == null)
            {
                try
                {
                    targetAssembly = Assembly.Load(AssemblyNameImUiFramework);
                }
                catch
                {
                    targetAssembly = null;
                }
            }

            if (targetAssembly == null)
            {
                bool hasUiFrameworkPatches = false;
                try
                {
                    hasUiFrameworkPatches = Harmony.HasAnyPatches(C.HarmonyIdImUiFramework);
                }
                catch
                {
                    hasUiFrameworkPatches = false;
                }

                errorMessage = hasUiFrameworkPatches
                    ? C.TextImUiFrameworkPatchesArePresentButApiAssemblyCouldNotBeResolved
                    : C.TextImUiFrameworkModIsNotLoaded;
                return false;
            }

            uiKitType = targetAssembly.GetType(TypeNameUiKit, false);
            if (uiKitType == null)
            {
                errorMessage = C.TextImUiFrameworkImUiKitTypeWasNotFoundInAssembly;
                return false;
            }

            methodInitialize = FindMethod(uiKitType, C.MemberNameInitialize, C.LastFromCount);
            methodCreateUiObject = FindMethod(uiKitType, C.MemberNameCreateUiObject, C.TimelineActionButtonsPerRow);
            methodClearChildren = FindMethod(uiKitType, C.MemberNameClearChildren, C.LastFromCount);
            methodRebuildLayout = FindMethod(uiKitType, C.MemberNameRebuildLayout, C.LastFromCount);
            methodCreateStyledButton = FindMethod(uiKitType, C.MemberNameCreateStyledButton, C.UiFrameworkCreateStyledButtonMethodParameterCount);
            methodCloneStyledButton = FindMethod(uiKitType, C.MemberNameCloneStyledButton, C.UiFrameworkCloneStyledButtonMethodParameterCount);

            if (methodInitialize == null ||
                methodCreateUiObject == null ||
                methodClearChildren == null ||
                methodRebuildLayout == null ||
                methodCreateStyledButton == null ||
                methodCloneStyledButton == null)
            {
                errorMessage = C.TextImUiFrameworkApiMethodSignatureMismatch;
                return false;
            }

            return true;
        }

        private static Assembly FindLoadedAssembly(string assemblyName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = C.ZeroIndex; i < assemblies.Length; i++)
            {
                Assembly asm = assemblies[i];
                if (asm == null)
                {
                    continue;
                }

                AssemblyName name = asm.GetName();
                if (name != null && string.Equals(name.Name, assemblyName, StringComparison.OrdinalIgnoreCase))
                {
                    return asm;
                }
            }

            return null;
        }

        private static MethodInfo FindMethod(Type type, string methodName, int parameterCount)
        {
            if (type == null || string.IsNullOrEmpty(methodName))
            {
                return null;
            }

            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
            for (int i = C.ZeroIndex; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method == null)
                {
                    continue;
                }

                if (!string.Equals(method.Name, methodName, StringComparison.Ordinal))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters != null && parameters.Length == parameterCount)
                {
                    return method;
                }
            }

            return null;
        }

        private static bool TryInvoke(MethodInfo method, object[] args, out object result, out string errorMessage)
        {
            result = null;
            errorMessage = string.Empty;

            if (method == null)
            {
                errorMessage = C.TextImUiFrameworkBridgeMethodIsUnavailable;
                return false;
            }

            try
            {
                result = method.Invoke(null, args);
                return true;
            }
            catch (TargetInvocationException exception)
            {
                Exception inner = exception.InnerException ?? exception;
                errorMessage = inner.Message;
                return false;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        private static bool TryInvokeVoid(MethodInfo method, object[] args, out string errorMessage)
        {
            errorMessage = string.Empty;
            object ignoredResult;
            if (!TryInvoke(method, args, out ignoredResult, out errorMessage))
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Shared IM Data Core session and lightweight UI state persistence.
    /// </summary>
    internal static class Runtime
    {
        private static readonly object Sync = new object();
        private static IMDataCoreSession session;
        private static DateTime nextRetryUtc = DateTime.MinValue;
        private static bool dependencySatisfied;
        private static DateTime nextDependencyLogUtc = DateTime.MinValue;
        private static string lastDependencyError = string.Empty;

        /// <summary>
        /// Checks required dependencies availability and logs actionable failure details.
        /// </summary>
        internal static bool EnsureHardDependencyAvailable()
        {
            lock (Sync)
            {
                if (dependencySatisfied)
                {
                    return true;
                }

                string coreDependencyError;
                bool coreAvailable = IMDataCoreApi.TryResolveDependency(out coreDependencyError);

                string uiFrameworkDependencyError;
                bool uiFrameworkAvailable = IMUiFrameworkApi.TryResolveDependency(out uiFrameworkDependencyError);

                if (coreAvailable && uiFrameworkAvailable)
                {
                    dependencySatisfied = true;
                    nextDependencyLogUtc = DateTime.MinValue;
                    lastDependencyError = string.Empty;
                    Log.Info(C.MessageCoreDependencyDetected);
                    Log.Info(C.MessageUiFrameworkDependencyDetected);
                    return true;
                }

                string dependencyError = string.Empty;
                if (!coreAvailable && !uiFrameworkAvailable)
                {
                    dependencyError =
                        C.TextImDataCoreErrorPrefix + (string.IsNullOrEmpty(coreDependencyError) ? C.TextUnknownError : coreDependencyError) +
                        C.TextImUiFrameworkErrorPrefix + (string.IsNullOrEmpty(uiFrameworkDependencyError) ? C.TextUnknownError : uiFrameworkDependencyError);
                }
                else if (!coreAvailable)
                {
                    dependencyError = string.IsNullOrEmpty(coreDependencyError) ? C.TextImDataCoreUnknownDependencyResolutionError : coreDependencyError;
                }
                else
                {
                    dependencyError = string.IsNullOrEmpty(uiFrameworkDependencyError) ? C.TextImUiFrameworkUnknownDependencyResolutionError : uiFrameworkDependencyError;
                }

                bool shouldLog =
                    DateTime.UtcNow >= nextDependencyLogUtc ||
                    !string.Equals(lastDependencyError, dependencyError, StringComparison.Ordinal);

                if (shouldLog)
                {
                    if (!coreAvailable)
                    {
                        Log.Error(
                            C.MessageCoreDependencyMissingPrefix +
                            (string.IsNullOrEmpty(coreDependencyError) ? C.TextUnknownDependencyResolutionError : coreDependencyError) +
                            C.TextEnsureModImDataCore +
                            C.HarmonyIdImDataCore +
                            C.TextIsInstalledAndEnabled);
                    }

                    if (!uiFrameworkAvailable)
                    {
                        Log.Error(
                            C.MessageUiFrameworkDependencyMissingPrefix +
                            (string.IsNullOrEmpty(uiFrameworkDependencyError) ? C.TextUnknownDependencyResolutionError : uiFrameworkDependencyError) +
                            C.TextEnsureModImUiFramework +
                            C.HarmonyIdImUiFramework +
                            C.TextIsInstalledAndEnabled);
                    }

                    nextDependencyLogUtc = DateTime.UtcNow.AddSeconds(C.DependencyLogRetrySeconds);
                    lastDependencyError = dependencyError;
                }

                return false;
            }
        }

        /// <summary>
        /// Attempts session bootstrapping.
        /// </summary>
        internal static void BootstrapSessionIfNeeded()
        {
            if (!EnsureHardDependencyAvailable())
            {
                return;
            }

            IMDataCoreSession ignoredSession;
            string ignoredError;
            TryGetSession(out ignoredSession, out ignoredError);
        }

        /// <summary>
        /// Loads last selected event id from core custom storage.
        /// </summary>
        internal static bool TryLoadSelectedEventId(int idolId, out long eventId)
        {
            eventId = C.InvalidEventId;
            if (idolId < C.MinId)
            {
                return false;
            }

            IMDataCoreSession coreSession;
            string error;
            if (!TryGetSession(out coreSession, out error))
            {
                return false;
            }

            string json;
            if (!IMDataCoreApi.TryGetCustomJson(coreSession, BuildSelectedEventKey(idolId), out json, out error))
            {
                return false;
            }

            JSONNode node = ParseJson(json);
            if (node == null)
            {
                return false;
            }

            JSONNode eventNode = node[C.CoreSelectedEventJsonField];
            long parsedEventId;
            if (!TryReadLongNode(eventNode, out parsedEventId))
            {
                return false;
            }

            eventId = parsedEventId;
            return true;
        }

        /// <summary>
        /// Saves last selected event id to core custom storage.
        /// </summary>
        internal static void TrySaveSelectedEventId(int idolId, long eventId)
        {
            if (idolId < C.MinId || eventId <= C.InvalidEventId)
            {
                return;
            }

            IMDataCoreSession coreSession;
            string error;
            if (!TryGetSession(out coreSession, out error))
            {
                return;
            }

            JSONClass payload = new JSONClass();
            payload[C.CoreSelectedEventJsonField].AsDouble = eventId;

            if (!IMDataCoreApi.TrySetCustomJson(coreSession, BuildSelectedEventKey(idolId), payload.ToString(), out error))
            {
                Log.Warn(C.TextUnableToPersistSelectedEventStatePrefix + error);
            }
        }

        /// <summary>
        /// Appends one idol birthday event into IM Data Core timeline storage.
        /// </summary>
        internal static void TryAppendBirthdayEvent(data_girls.girls girl)
        {
            if (girl == null || girl.id < C.MinId)
            {
                return;
            }

            IMDataCoreSession coreSession;
            string error;
            if (!TryGetSession(out coreSession, out error))
            {
                return;
            }

            JSONClass payload = new JSONClass();
            payload[C.JsonIdolId].AsInt = girl.id;
            payload[C.KeyIdolAge].AsInt = Mathf.Max(C.ZeroIndex, girl.GetAge());
            payload[C.KeyIdolBirthdayDate] = staticVars.dateTime.ToString(C.DateFormatRoundTrip, CultureInfo.InvariantCulture);

            if (!IMDataCoreApi.TryAppendCustomEvent(
                coreSession,
                girl.id,
                C.KindIdol,
                girl.id.ToString(CultureInfo.InvariantCulture),
                C.EventIdolBirthday,
                payload.ToString(),
                C.SourcePatchIdolBirthday,
                out error))
            {
                if (!string.IsNullOrEmpty(error))
                {
                    Log.Warn(C.TextBirthdayEventAppendFailedPrefix + error);
                }
            }
        }

        /// <summary>
        /// Returns cached core session or tries registration.
        /// </summary>
        private static bool TryGetSession(out IMDataCoreSession coreSession, out string error)
        {
            if (!EnsureHardDependencyAvailable())
            {
                coreSession = null;
                error = C.TextRequiredDependenciesAreMissing;
                return false;
            }

            lock (Sync)
            {
                coreSession = session;
                error = string.Empty;
                if (coreSession != null)
                {
                    return true;
                }

                if (DateTime.UtcNow < nextRetryUtc)
                {
                    error = C.TextRetryWindowActive;
                    return false;
                }

                nextRetryUtc = DateTime.UtcNow.AddSeconds(C.CoreRegistrationRetrySeconds);
                if (!IMDataCoreApi.IsReady())
                {
                    error = C.TextImDataCoreNotReady;
                    return false;
                }

                if (!IMDataCoreApi.TryRegisterNamespace(C.CoreNamespace, out coreSession, out error))
                {
                    Log.Warn(C.TextCoreNamespaceRegistrationFailedPrefix + error);
                    return false;
                }

                session = coreSession;
                Log.Info(C.TextImDataCoreSessionInitialized);
                return true;
            }
        }

        /// <summary>
        /// Builds selected-event key for one idol.
        /// </summary>
        private static string BuildSelectedEventKey(int idolId)
        {
            return C.CoreSelectedEventKeyPrefix + idolId.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parses json safely.
        /// </summary>
        private static JSONNode ParseJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                return JSON.Parse(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Reads one long from a JSON node using invariant numeric parsing.
        /// </summary>
        private static bool TryReadLongNode(JSONNode node, out long value)
        {
            value = C.InvalidEventId;
            if (node == null || IsLazyCreatorNode(node))
            {
                return false;
            }

            string raw = node.Value ?? string.Empty;
            if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            double parsedDouble;
            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedDouble))
            {
                value = (long)parsedDouble;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Detects SimpleJSON lazy-placeholder nodes without referencing inaccessible internal types.
        /// </summary>
        private static bool IsLazyCreatorNode(JSONNode node)
        {
            return node != null && string.Equals(node.GetType().Name, C.JsonLazyCreatorRuntimeTypeName, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Runtime settings backed by one mod-local config file and optional ModMenus checkbox variable.
    /// </summary>
    internal static class DiarySettings
    {
        private static readonly object Sync = new object();
        private static bool loaded;
        private static bool modMenusProbeDone;
        private static bool modMenusAvailable;
        private static bool showUnknownSocialParticipants = C.DefaultShowUnknownSocialParticipants;

        /// <summary>
        /// Ensures settings are initialized and config file exists.
        /// </summary>
        internal static void EnsureLoaded()
        {
            lock (Sync)
            {
                if (loaded)
                {
                    return;
                }

                LoadConfigUnsafe();
                loaded = true;
                if (IsModMenusAvailableUnsafe())
                {
                    SyncModMenuVariableUnsafe();
                }
            }
        }

        /// <summary>
        /// Returns whether hidden social-event participants should be shown.
        /// </summary>
        internal static bool ShouldShowUnknownSocialParticipants()
        {
            lock (Sync)
            {
                if (!loaded)
                {
                    LoadConfigUnsafe();
                    loaded = true;
                    if (IsModMenusAvailableUnsafe())
                    {
                        SyncModMenuVariableUnsafe();
                    }
                }

                if (!IsModMenusAvailableUnsafe())
                {
                    return showUnknownSocialParticipants;
                }

                bool modMenuValue;
                if (TryReadModMenuToggleUnsafe(out modMenuValue))
                {
                    if (modMenuValue != showUnknownSocialParticipants)
                    {
                        showUnknownSocialParticipants = modMenuValue;
                        WriteConfigUnsafe();
                    }

                    return modMenuValue;
                }

                return showUnknownSocialParticipants;
            }
        }

        /// <summary>
        /// Returns true when Tel's ModMenus patch is active.
        /// </summary>
        private static bool IsModMenusAvailableUnsafe()
        {
            if (modMenusProbeDone)
            {
                return modMenusAvailable;
            }

            modMenusProbeDone = true;
            try
            {
                modMenusAvailable = Harmony.HasAnyPatches(C.HarmonyIdModMenus);
            }
            catch
            {
                modMenusAvailable = false;
            }

            return modMenusAvailable;
        }

        /// <summary>
        /// Loads config from disk; preserves defaults when file or parse errors occur.
        /// </summary>
        private static void LoadConfigUnsafe()
        {
            showUnknownSocialParticipants = C.DefaultShowUnknownSocialParticipants;

            string path = GetConfigPath();
            try
            {
                EnsureConfigFileExists(path);
                string[] lines = File.ReadAllLines(path);
                for (int i = C.ZeroIndex; i < lines.Length; i++)
                {
                    string raw = lines[i];
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        continue;
                    }

                    string line = raw.Trim();
                    if (line.StartsWith(C.ConfigCommentPrefixHash, StringComparison.Ordinal) ||
                        line.StartsWith(C.ConfigCommentPrefixSemicolon, StringComparison.Ordinal) ||
                        line.StartsWith(C.ConfigCommentPrefixSlashSlash, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    int separatorIndex = line.IndexOf(C.ConfigKeyValueSeparator);
                    if (separatorIndex <= C.ZeroIndex || separatorIndex >= line.Length - C.LastFromCount)
                    {
                        continue;
                    }

                    string key = line.Substring(C.ZeroIndex, separatorIndex).Trim();
                    string value = line.Substring(separatorIndex + C.LastFromCount).Trim();
                    if (!string.Equals(key, C.ConfigKeyShowUnknownSocialParticipants, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    bool parsed;
                    if (TryParseBool(value, out parsed))
                    {
                        showUnknownSocialParticipants = parsed;
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Warn("Settings load failed: " + exception.Message);
            }
        }

        /// <summary>
        /// Writes current settings back to config file.
        /// </summary>
        private static void WriteConfigUnsafe()
        {
            string path = GetConfigPath();
            try
            {
                EnsureConfigFileExists(path);
                string[] lines = new[]
                {
                    C.DefaultConfigTemplateLines[C.ZeroIndex],
                    C.DefaultConfigTemplateLines[C.LastFromCount],
                    BuildConfigAssignment(C.ConfigKeyShowUnknownSocialParticipants, showUnknownSocialParticipants ? "true" : "false")
                };
                File.WriteAllLines(path, lines);
            }
            catch (Exception exception)
            {
                Log.Warn("Settings save failed: " + exception.Message);
            }
        }

        /// <summary>
        /// Pushes config value into ModMenus variable when variable has not been initialized yet.
        /// </summary>
        private static void SyncModMenuVariableUnsafe()
        {
            bool ignoredValue;
            if (TryReadModMenuToggleUnsafe(out ignoredValue))
            {
                return;
            }

            try
            {
                variables.Set(C.ModMenuVarShowUnknownSocialParticipants, showUnknownSocialParticipants ? "1" : "0");
            }
            catch
            {
            }
        }

        /// <summary>
        /// Reads ModMenus checkbox value if available.
        /// </summary>
        private static bool TryReadModMenuToggleUnsafe(out bool value)
        {
            value = false;
            try
            {
                string raw = variables.Get(C.ModMenuVarShowUnknownSocialParticipants);
                if (string.IsNullOrEmpty(raw))
                {
                    return false;
                }

                return TryParseBool(raw, out value);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Resolves the config file path next to this mod DLL.
        /// </summary>
        private static string GetConfigPath()
        {
            try
            {
                string dllPath = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(dllPath))
                {
                    string directory = Path.GetDirectoryName(dllPath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        return Path.Combine(directory, C.ConfigFileName);
                    }
                }
            }
            catch
            {
            }

            return Path.Combine(Application.dataPath, C.ConfigFileName);
        }

        /// <summary>
        /// Creates default config file when missing.
        /// </summary>
        private static void EnsureConfigFileExists(string path)
        {
            if (string.IsNullOrEmpty(path) || File.Exists(path))
            {
                return;
            }

            File.WriteAllLines(path, C.DefaultConfigTemplateLines);
        }

        /// <summary>
        /// Parses boolean values from common string forms.
        /// </summary>
        private static bool TryParseBool(string raw, out bool value)
        {
            value = false;
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            string normalized = raw.Trim();
            if (string.Equals(normalized, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "yes", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "y", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }

            if (string.Equals(normalized, "0", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "false", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "no", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "n", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }

            return bool.TryParse(normalized, out value);
        }

        /// <summary>
        /// Builds one key/value assignment line for config writes.
        /// </summary>
        private static string BuildConfigAssignment(string key, string value)
        {
            return key + C.ConfigKeyValueSeparator + value;
        }
    }

    /// <summary>
    /// Timeline/detail presentation data.
    /// </summary>
    internal sealed class Presentation
    {
        internal string Date = string.Empty;
        internal string Title = string.Empty;
        internal string WithWhom = string.Empty;
        internal string Outcome = string.Empty;
        internal string Source = string.Empty;
        internal string ModSource = string.Empty;
        internal string PayloadPreview = string.Empty;
        internal List<int> RelatedIdols = new List<int>();
        internal StaffCreditInfo StaffCredit;
    }

    /// <summary>
    /// Portable staff identity from IM Data Core's event payload.  It supports rendering a
    /// portrait after the staff member has been fired, including Assistant Manager textures.
    /// </summary>
    internal sealed class StaffCreditInfo
    {
        internal int StaffId = C.InvalidId;
        internal string Name = string.Empty;
        internal string Role = string.Empty;
        internal int TypeRaw = C.InvalidId;
        internal int UniqueTypeRaw = C.InvalidId;
        internal bool IsPro;
        internal bool IsProducer;

        internal bool HasIdentity
        {
            get { return !string.IsNullOrEmpty(Name); }
        }

        internal string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(Name))
                {
                    return Name;
                }

                return IsProducer ? "Producer" : C.LabelUnknown;
            }
        }
    }

    /// <summary>
    /// Optional player-facing diary text supplied by JSON-only content mods.
    /// </summary>
    internal sealed class CustomDiaryEntry
    {
        internal readonly List<string> EventTypes = new List<string>();
        internal readonly List<string> EntityIds = new List<string>();
        internal readonly List<string> SubstoryIdPrefixes = new List<string>();
        internal string EntityKind = string.Empty;
        internal string Title = string.Empty;
        internal string WithWhom = string.Empty;
        internal string Description = string.Empty;
        internal string SourceModTitle = string.Empty;
        internal readonly List<string> OutcomeLines = new List<string>();

        internal bool Matches(IMDataCoreEvent ev, JSONNode payload)
        {
            if (ev == null)
            {
                return false;
            }

            if (EventTypes.Count > C.ZeroIndex && !ContainsOrdinal(EventTypes, ev.EventType ?? string.Empty))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(EntityKind) && !string.Equals(EntityKind, ev.EntityKind ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (EntityIds.Count == C.ZeroIndex && SubstoryIdPrefixes.Count == C.ZeroIndex)
            {
                return true;
            }

            if (ContainsOrdinal(EntityIds, ev.EntityId ?? string.Empty))
            {
                return true;
            }

            string substoryId = payload != null ? ReadJsonField(payload, C.KeySubstoryId) : string.Empty;
            if (ContainsOrdinal(EntityIds, substoryId))
            {
                return true;
            }

            return StartsWithAny(SubstoryIdPrefixes, ev.EntityId ?? string.Empty)
                || StartsWithAny(SubstoryIdPrefixes, substoryId);
        }

        private static string ReadJsonField(JSONNode payload, string field)
        {
            if (payload == null || string.IsNullOrEmpty(field))
            {
                return string.Empty;
            }

            JSONNode node = payload[field];
            if (node == null || IsLazyCreatorNode(node))
            {
                return string.Empty;
            }

            return node.Value ?? string.Empty;
        }

        private static bool IsLazyCreatorNode(JSONNode node)
        {
            return node != null && string.Equals(node.GetType().Name, C.JsonLazyCreatorRuntimeTypeName, StringComparison.Ordinal);
        }

        private static bool ContainsOrdinal(List<string> values, string candidate)
        {
            if (values == null || string.IsNullOrEmpty(candidate))
            {
                return false;
            }

            for (int i = C.ZeroIndex; i < values.Count; i++)
            {
                if (string.Equals(values[i], candidate, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool StartsWithAny(List<string> values, string candidate)
        {
            if (values == null || string.IsNullOrEmpty(candidate))
            {
                return false;
            }

            for (int i = C.ZeroIndex; i < values.Count; i++)
            {
                string prefix = values[i];
                if (!string.IsNullOrEmpty(prefix) && candidate.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Basic metadata discovered from one installed mod folder.
    /// </summary>
    internal sealed class ModInfoEntry
    {
        internal string RootPath = string.Empty;
        internal string Title = string.Empty;
        internal string HarmonyId = string.Empty;
        internal string FolderName = string.Empty;
        internal readonly List<string> DllNames = new List<string>();
    }

    /// <summary>
    /// Scans installed mod folders and resolves player-facing source names.
    /// </summary>
    internal static class ModInfoCatalog
    {
        private static readonly object Sync = new object();
        private static readonly List<ModInfoEntry> Entries = new List<ModInfoEntry>();
        private static bool loaded;

        internal static void EnsureLoaded()
        {
            lock (Sync)
            {
                if (loaded)
                {
                    return;
                }

                loaded = true;
                LoadUnsafe();
            }
        }

        internal static List<string> GetCandidateModRoots()
        {
            EnsureLoaded();
            List<string> roots = new List<string>();
            lock (Sync)
            {
                for (int i = C.ZeroIndex; i < Entries.Count; i++)
                {
                    AddDirectory(roots, Entries[i].RootPath);
                }
            }

            return roots;
        }

        internal static string ResolveTitleForRoot(string modRoot)
        {
            EnsureLoaded();
            string normalizedRoot = NormalizePath(modRoot);
            if (string.IsNullOrEmpty(normalizedRoot))
            {
                return string.Empty;
            }

            lock (Sync)
            {
                for (int i = C.ZeroIndex; i < Entries.Count; i++)
                {
                    if (string.Equals(Entries[i].RootPath, normalizedRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        return Entries[i].Title;
                    }
                }
            }

            return string.Empty;
        }

        internal static string ResolveEventSourceTitle(IMDataCoreEvent ev)
        {
            if (ev == null)
            {
                return string.Empty;
            }

            EnsureLoaded();
            string namespaceId = NormalizeInfoToken(ev.NamespaceId);
            string sourcePatch = NormalizeInfoToken(ev.SourcePatch);

            // "data" is an internal event-ledger namespace, not a player
            // installed mod.  Do not expose it as a misleading source label.
            if (string.Equals(namespaceId, C.InternalEventLedgerNamespace, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            lock (Sync)
            {
                string title = ResolveByExactTokenUnsafe(namespaceId);
                if (!string.IsNullOrEmpty(title))
                {
                    return title;
                }

                title = ResolveByContainedTokenUnsafe(sourcePatch);
                if (!string.IsNullOrEmpty(title))
                {
                    return title;
                }

            }

            return string.Empty;
        }

        private static void LoadUnsafe()
        {
            List<string> roots = ResolveCandidateRootsUnsafe();
            for (int i = C.ZeroIndex; i < roots.Count; i++)
            {
                ModInfoEntry entry = ReadModInfoUnsafe(roots[i]);
                if (entry != null)
                {
                    Entries.Add(entry);
                }
            }
        }

        private static List<string> ResolveCandidateRootsUnsafe()
        {
            List<string> roots = new List<string>();
            string assemblyDirectory = GetAssemblyDirectory();
            AddDirectory(roots, assemblyDirectory);

            string parent = SafeGetParent(assemblyDirectory);
            if (!string.IsNullOrEmpty(parent))
            {
                AddDirectory(roots, parent);
                AddChildDirectories(roots, parent);
            }

            string grandParent = SafeGetParent(parent);
            if (!string.IsNullOrEmpty(grandParent))
            {
                AddChildDirectories(roots, grandParent);
            }

            try
            {
                AddChildDirectories(roots, Path.Combine(Application.persistentDataPath, "Mods"));
            }
            catch
            {
            }

            return roots;
        }

        private static ModInfoEntry ReadModInfoUnsafe(string root)
        {
            string normalizedRoot = NormalizePath(root);
            if (string.IsNullOrEmpty(normalizedRoot) || !Directory.Exists(normalizedRoot))
            {
                return null;
            }

            ModInfoEntry entry = new ModInfoEntry();
            entry.RootPath = normalizedRoot;
            entry.FolderName = Path.GetFileName(normalizedRoot) ?? string.Empty;
            entry.Title = entry.FolderName;

            string infoPath = Path.Combine(normalizedRoot, C.InfoJsonFileName);
            if (File.Exists(infoPath))
            {
                try
                {
                    JSONNode info = ParseJson(File.ReadAllText(infoPath));
                    string title = NormalizeInfoText(ReadJsonField(info, C.InfoTitleField));
                    if (!string.IsNullOrEmpty(title))
                    {
                        entry.Title = title;
                    }

                    entry.HarmonyId = NormalizeInfoText(ReadJsonField(info, C.InfoHarmonyIdField));
                }
                catch
                {
                }
            }

            AddDllNames(entry, normalizedRoot);
            return entry;
        }

        private static void AddDllNames(ModInfoEntry entry, string root)
        {
            if (entry == null || string.IsNullOrEmpty(root))
            {
                return;
            }

            string[] dlls;
            try
            {
                dlls = Directory.GetFiles(root, "*.dll", SearchOption.TopDirectoryOnly);
            }
            catch
            {
                return;
            }

            for (int i = C.ZeroIndex; i < dlls.Length; i++)
            {
                string name = Path.GetFileNameWithoutExtension(dlls[i]);
                if (!string.IsNullOrEmpty(name))
                {
                    entry.DllNames.Add(name);
                }
            }
        }

        private static string ResolveByExactTokenUnsafe(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return string.Empty;
            }

            for (int i = C.ZeroIndex; i < Entries.Count; i++)
            {
                ModInfoEntry entry = Entries[i];
                if (TokenEquals(entry.HarmonyId, token)
                    || TokenEquals(entry.FolderName, token)
                    || TokenEquals(entry.Title, token)
                    || ListContainsToken(entry.DllNames, token))
                {
                    return entry.Title;
                }
            }

            return string.Empty;
        }

        private static string ResolveByContainedTokenUnsafe(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return string.Empty;
            }

            for (int i = C.ZeroIndex; i < Entries.Count; i++)
            {
                ModInfoEntry entry = Entries[i];
                if (IsInternalFrameworkEntry(entry))
                {
                    continue;
                }

                if (TokenContains(token, entry.HarmonyId)
                    || TokenContains(token, entry.Title)
                    || TokenContains(token, entry.FolderName)
                    || ListTokenContainedIn(token, entry.DllNames))
                {
                    return entry.Title;
                }
            }

            return string.Empty;
        }

        private static bool IsInternalFrameworkEntry(ModInfoEntry entry)
        {
            if (entry == null)
            {
                return false;
            }

            return TokenEquals(entry.HarmonyId, C.HarmonyIdImDataCore)
                || TokenEquals(entry.HarmonyId, C.HarmonyIdImUiFramework)
                || TokenEquals(entry.HarmonyId, C.HarmonyId);
        }

        private static bool ListContainsToken(List<string> values, string token)
        {
            if (values == null)
            {
                return false;
            }

            for (int i = C.ZeroIndex; i < values.Count; i++)
            {
                if (TokenEquals(values[i], token))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ListTokenContainedIn(string haystack, List<string> values)
        {
            if (values == null)
            {
                return false;
            }

            for (int i = C.ZeroIndex; i < values.Count; i++)
            {
                if (TokenContains(haystack, values[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TokenEquals(string left, string right)
        {
            string normalizedLeft = NormalizeInfoToken(left);
            string normalizedRight = NormalizeInfoToken(right);
            return !string.IsNullOrEmpty(normalizedLeft)
                && !string.IsNullOrEmpty(normalizedRight)
                && string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TokenContains(string haystack, string needle)
        {
            string normalizedHaystack = NormalizeInfoToken(haystack);
            string normalizedNeedle = NormalizeInfoToken(needle);
            return !string.IsNullOrEmpty(normalizedHaystack)
                && !string.IsNullOrEmpty(normalizedNeedle)
                && normalizedNeedle.Length >= 4
                && normalizedHaystack.IndexOf(normalizedNeedle, StringComparison.OrdinalIgnoreCase) >= C.ZeroIndex;
        }

        private static string NormalizeInfoToken(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            char[] chars = value.ToLowerInvariant().ToCharArray();
            StringBuilder builder = new StringBuilder(chars.Length);
            for (int i = C.ZeroIndex; i < chars.Length; i++)
            {
                char c = chars[i];
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        private static string NormalizeInfoText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim();
        }

        private static JSONNode ParseJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                return JSON.Parse(json);
            }
            catch
            {
                return null;
            }
        }

        private static string ReadJsonField(JSONNode payload, string field)
        {
            if (payload == null || string.IsNullOrEmpty(field))
            {
                return string.Empty;
            }

            JSONNode node = payload[field];
            if (node == null || IsLazyCreatorNode(node))
            {
                return string.Empty;
            }

            return node.Value ?? string.Empty;
        }

        private static bool IsLazyCreatorNode(JSONNode node)
        {
            return node != null && string.Equals(node.GetType().Name, C.JsonLazyCreatorRuntimeTypeName, StringComparison.Ordinal);
        }

        private static void AddChildDirectories(List<string> destination, string root)
        {
            if (destination == null || string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                return;
            }

            string[] directories;
            try
            {
                directories = Directory.GetDirectories(root);
            }
            catch
            {
                return;
            }

            for (int i = C.ZeroIndex; i < directories.Length; i++)
            {
                AddDirectory(destination, directories[i]);
            }
        }

        private static void AddDirectory(List<string> destination, string path)
        {
            string normalizedPath = NormalizePath(path);
            if (destination == null || string.IsNullOrEmpty(normalizedPath) || !Directory.Exists(normalizedPath))
            {
                return;
            }

            for (int i = C.ZeroIndex; i < destination.Count; i++)
            {
                if (string.Equals(destination[i], normalizedPath, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            destination.Add(normalizedPath);
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return path;
            }
        }

        private static string GetAssemblyDirectory()
        {
            try
            {
                string path = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(path))
                {
                    return Path.GetDirectoryName(path);
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string SafeGetParent(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            try
            {
                DirectoryInfo parent = Directory.GetParent(path);
                return parent != null ? parent.FullName : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Reuses exact English player-facing values from installed mods, then asks
    /// Mod Localization System for that owning mod's selected translation. This
    /// keeps stored IM Data Core text such as Assistant Manager staff roles from
    /// remaining English in Career Diary.
    /// </summary>
    internal static class ExternalModLocalizationCatalog
    {
        private sealed class Entry
        {
            internal ModLocalizer Localizer;
            internal string Key = string.Empty;
            internal string EnglishValue = string.Empty;
        }

        private static readonly object Sync = new object();
        private static readonly List<Entry> Entries = new List<Entry>();
        private static bool loaded;

        internal static string ResolveEnglishText(string rawText)
        {
            string candidate = (rawText ?? string.Empty).Trim();
            if (candidate.Length == C.ZeroIndex)
            {
                return rawText ?? string.Empty;
            }

            EnsureLoaded();
            lock (Sync)
            {
                for (int index = C.ZeroIndex; index < Entries.Count; index++)
                {
                    Entry entry = Entries[index];
                    if (!string.Equals(entry.EnglishValue, candidate, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string localized = entry.Localizer.Get(entry.Key, candidate);
                    if (!string.IsNullOrEmpty(localized) &&
                        !string.Equals(localized, candidate, StringComparison.Ordinal))
                    {
                        return localized;
                    }
                }
            }

            return rawText ?? string.Empty;
        }

        private static void EnsureLoaded()
        {
            lock (Sync)
            {
                if (loaded)
                {
                    return;
                }

                loaded = true;
                List<string> roots = ModInfoCatalog.GetCandidateModRoots();
                for (int rootIndex = C.ZeroIndex; rootIndex < roots.Count; rootIndex++)
                {
                    LoadRoot(roots[rootIndex]);
                }
            }
        }

        private static void LoadRoot(string root)
        {
            if (string.IsNullOrEmpty(root))
            {
                return;
            }

            string modDirectory = root;
            string englishPath = Path.Combine(root, "Localization", "en", "strings.txt");
            if (!File.Exists(englishPath))
            {
                modDirectory = Path.Combine(root, "assets");
                englishPath = Path.Combine(modDirectory, "Localization", "en", "strings.txt");
            }

            if (!File.Exists(englishPath))
            {
                return;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(englishPath);
            }
            catch
            {
                return;
            }

            ModLocalizer localizer = ModLocalization.ForDirectory(modDirectory);
            for (int lineIndex = C.ZeroIndex; lineIndex < lines.Length; lineIndex++)
            {
                string rawLine = lines[lineIndex] ?? string.Empty;
                string trimmedLine = rawLine.Trim();
                if (trimmedLine.Length == C.ZeroIndex || trimmedLine.StartsWith("#") ||
                    trimmedLine.StartsWith(";") || trimmedLine.StartsWith("//"))
                {
                    continue;
                }

                int separator = rawLine.IndexOf('=');
                if (separator <= C.ZeroIndex)
                {
                    continue;
                }

                string key = rawLine.Substring(C.ZeroIndex, separator).Trim();
                string englishValue = rawLine.Substring(separator + C.LastFromCount).Trim();
                if (key.Length == C.ZeroIndex || englishValue.Length < 4 ||
                    englishValue.IndexOf('{') >= C.ZeroIndex || englishValue.IndexOf('@') >= C.ZeroIndex)
                {
                    continue;
                }

                Entries.Add(new Entry
                {
                    Localizer = localizer,
                    Key = key,
                    EnglishValue = englishValue
                });
            }
        }
    }

    /// <summary>
    /// Loads optional diary text from content mod folders.
    /// </summary>
    internal static class CustomDiaryCatalog
    {
        private static readonly object Sync = new object();
        private static readonly List<CustomDiaryEntry> Entries = new List<CustomDiaryEntry>();
        private static bool loaded;

        internal static void EnsureLoaded()
        {
            lock (Sync)
            {
                if (loaded)
                {
                    return;
                }

                loaded = true;
                LoadUnsafe();
            }
        }

        internal static bool TryFind(IMDataCoreEvent ev, JSONNode payload, out CustomDiaryEntry entry)
        {
            EnsureLoaded();
            lock (Sync)
            {
                for (int i = C.ZeroIndex; i < Entries.Count; i++)
                {
                    if (Entries[i].Matches(ev, payload))
                    {
                        entry = Entries[i];
                        return true;
                    }
                }
            }

            entry = null;
            return false;
        }

        private static void LoadUnsafe()
        {
            HashSet<string> scannedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> modRoots = ModInfoCatalog.GetCandidateModRoots();
            for (int i = C.ZeroIndex; i < modRoots.Count; i++)
            {
                LoadFromModRootUnsafe(modRoots[i], scannedFolders);
            }

            if (Entries.Count > C.ZeroIndex)
            {
                Log.Info("Loaded custom diary entries: " + Entries.Count.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static List<string> ResolveCandidateModRoots()
        {
            List<string> roots = new List<string>();
            string assemblyDirectory = GetAssemblyDirectory();
            AddDirectory(roots, assemblyDirectory);

            string parent = SafeGetParent(assemblyDirectory);
            if (!string.IsNullOrEmpty(parent))
            {
                AddDirectory(roots, parent);
                AddChildDirectories(roots, parent);
            }

            string grandParent = SafeGetParent(parent);
            if (!string.IsNullOrEmpty(grandParent))
            {
                AddChildDirectories(roots, grandParent);
            }

            try
            {
                AddChildDirectories(roots, Path.Combine(Application.persistentDataPath, "Mods"));
            }
            catch
            {
            }

            return roots;
        }

        private static void LoadFromModRootUnsafe(string modRoot, HashSet<string> scannedFolders)
        {
            if (string.IsNullOrEmpty(modRoot) || scannedFolders == null)
            {
                return;
            }

            string sourceModTitle = ModInfoCatalog.ResolveTitleForRoot(modRoot);
            LoadFromFolderUnsafe(Path.Combine(modRoot, C.CustomEntriesFolderName), scannedFolders, sourceModTitle);
            LoadFromFolderUnsafe(Path.Combine(modRoot, C.CustomEntriesFolderNameCompact), scannedFolders, sourceModTitle);
        }

        private static void LoadFromFolderUnsafe(string folder, HashSet<string> scannedFolders, string sourceModTitle)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                return;
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(folder);
            }
            catch
            {
                fullPath = folder;
            }

            if (!scannedFolders.Add(fullPath))
            {
                return;
            }

            string[] files;
            try
            {
                files = Directory.GetFiles(fullPath, "*.json", SearchOption.TopDirectoryOnly);
            }
            catch (Exception exception)
            {
                Log.Warn("Custom diary folder scan failed: " + exception.Message);
                return;
            }

            for (int i = C.ZeroIndex; i < files.Length; i++)
            {
                LoadFileUnsafe(files[i], sourceModTitle);
            }
        }

        private static void LoadFileUnsafe(string path, string sourceModTitle)
        {
            try
            {
                JSONNode root = ParseJson(File.ReadAllText(path));
                JSONArray entries = root != null ? root[C.CustomEntriesArrayField].AsArray : null;
                if (entries == null && root is JSONArray)
                {
                    entries = root.AsArray;
                }

                if (entries == null)
                {
                    return;
                }

                for (int i = C.ZeroIndex; i < entries.Count; i++)
                {
                    CustomDiaryEntry entry = ParseEntry(entries[i]);
                    if (entry != null)
                    {
                        entry.SourceModTitle = sourceModTitle ?? string.Empty;
                        Entries.Add(entry);
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Warn("Custom diary file load failed: " + path + " " + exception.Message);
            }
        }

        private static CustomDiaryEntry ParseEntry(JSONNode node)
        {
            if (node == null)
            {
                return null;
            }

            CustomDiaryEntry entry = new CustomDiaryEntry();
            AddStringOrArray(entry.EventTypes, node, C.CustomEntryEventTypeField);
            AddStringOrArray(entry.EventTypes, node, C.CustomEntryEventTypesField);
            AddStringOrArray(entry.EntityIds, node, C.CustomEntryEntityIdField);
            AddStringOrArray(entry.EntityIds, node, C.CustomEntryEntityIdsField);
            AddStringOrArray(entry.EntityIds, node, C.CustomEntrySubstoryIdField);
            AddStringOrArray(entry.EntityIds, node, C.CustomEntrySubstoryIdsField);
            AddStringOrArray(entry.SubstoryIdPrefixes, node, C.CustomEntrySubstoryIdPrefixField);
            AddStringOrArray(entry.SubstoryIdPrefixes, node, C.CustomEntrySubstoryIdPrefixesField);
            AddStringOrArray(entry.OutcomeLines, node, C.CustomEntryOutcomeLinesField);
            entry.EntityKind = NormalizeEntryText(ReadJsonField(node, C.CustomEntryEntityKindField));
            if (entry.EntityKind == C.LabelUnknown)
            {
                entry.EntityKind = string.Empty;
            }

            entry.Title = NormalizeEntryText(ReadJsonField(node, C.CustomEntryTitleField));
            entry.WithWhom = NormalizeEntryText(ReadJsonField(node, C.CustomEntryWithWhomField));
            entry.Description = NormalizeEntryText(ReadJsonField(node, C.CustomEntryDescriptionField));
            if (entry.Description == C.LabelUnknown)
            {
                entry.Description = NormalizeEntryText(ReadJsonField(node, C.CustomEntryDetailsField));
            }

            if (entry.EventTypes.Count == C.ZeroIndex && entry.EntityIds.Count == C.ZeroIndex && entry.SubstoryIdPrefixes.Count == C.ZeroIndex)
            {
                return null;
            }

            return entry;
        }

        private static void AddStringOrArray(List<string> destination, JSONNode node, string field)
        {
            if (destination == null || node == null || string.IsNullOrEmpty(field))
            {
                return;
            }

            JSONNode valueNode = node[field];
            if (valueNode == null || IsLazyCreatorNode(valueNode))
            {
                return;
            }

            JSONArray array = valueNode.AsArray;
            if (array != null)
            {
                for (int i = C.ZeroIndex; i < array.Count; i++)
                {
                    AddValue(destination, ReadNodeString(array[i]));
                }

                return;
            }

            AddValue(destination, ReadNodeString(valueNode));
        }

        private static void AddValue(List<string> destination, string value)
        {
            string normalized = NormalizeEntryText(value);
            if (normalized == C.LabelUnknown)
            {
                return;
            }

            destination.Add(normalized);
        }

        private static JSONNode ParseJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                return JSON.Parse(json);
            }
            catch
            {
                return null;
            }
        }

        private static string ReadJsonField(JSONNode payload, string field)
        {
            if (payload == null || string.IsNullOrEmpty(field))
            {
                return string.Empty;
            }

            JSONNode node = payload[field];
            if (node == null || IsLazyCreatorNode(node))
            {
                return string.Empty;
            }

            return ReadNodeString(node);
        }

        private static string ReadNodeString(JSONNode node)
        {
            if (node == null || IsLazyCreatorNode(node))
            {
                return string.Empty;
            }

            return node.Value ?? string.Empty;
        }

        private static string NormalizeEntryText(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return C.LabelUnknown;
            }

            string normalized = raw.Trim();
            return normalized.Length == C.ZeroIndex ? C.LabelUnknown : normalized;
        }

        private static bool IsLazyCreatorNode(JSONNode node)
        {
            return node != null && string.Equals(node.GetType().Name, C.JsonLazyCreatorRuntimeTypeName, StringComparison.Ordinal);
        }

        private static void AddChildDirectories(List<string> destination, string root)
        {
            if (destination == null || string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                return;
            }

            string[] directories;
            try
            {
                directories = Directory.GetDirectories(root);
            }
            catch
            {
                return;
            }

            for (int i = C.ZeroIndex; i < directories.Length; i++)
            {
                AddDirectory(destination, directories[i]);
            }
        }

        private static void AddDirectory(List<string> destination, string path)
        {
            if (destination == null || string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return;
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(path);
            }
            catch
            {
                fullPath = path;
            }

            for (int i = C.ZeroIndex; i < destination.Count; i++)
            {
                if (string.Equals(destination[i], fullPath, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            destination.Add(fullPath);
        }

        private static string GetAssemblyDirectory()
        {
            try
            {
                string path = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(path))
                {
                    return Path.GetDirectoryName(path);
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string SafeGetParent(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            try
            {
                DirectoryInfo parent = Directory.GetParent(path);
                return parent != null ? parent.FullName : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Profile popup controller for injected Career Diary UI.
    /// </summary>
    internal sealed class CareerDiaryController : MonoBehaviour
    {
        private Profile_Popup profile;
        private data_girls.girls idol;

        private GameObject tabTemplate;
        private GameObject panelTemplate;

        private GameObject diaryTabButtonObject;
        private Button diaryTabButton;
        private GameObject diaryPanelObject;
        private Transform diaryContentRoot;
        private GameObject diaryDetailPopupObject;
        private Transform diaryDetailContentRoot;
        private static MethodInfo chartPopupRenderMethod;

        private bool diaryVisible;
        private bool injectionInProgress;

        private readonly List<IMDataCoreEvent> cachedEvents = new List<IMDataCoreEvent>();
        private readonly HashSet<string> selectedFilterEventTypes = new HashSet<string>(StringComparer.Ordinal);
        private bool filterModeIsExclude = true;
        private int maxEventsRenderCurrent = C.MaxEventsRender;
        private long selectedEventId = C.InvalidEventId;

        private string loadWarning = string.Empty;
        private string interactionMessage = string.Empty;
        private static readonly string[] TimelinePayloadDateFieldPriority = new[]
        {
            C.KeyEventDate,
            C.KeySingleReleaseDate,
            C.JsonShowEpisodeDate,
            C.KeyGroupEventDate,
            C.KeyReportDate,
            C.KeyTransferDate,
            C.KeyIdolHiringDate,
            C.KeyIdolGraduationDate,
            C.KeyDecisionDate,
            C.KeyTourStartDate,
            C.JsonTourFinishDate,
            C.JsonElectionFinishDate,
            C.KeyConcertFinishDate,
            C.JsonMedicalHiatusEnd,
            C.JsonContractStartDate,
            C.JsonContractEndDate
        };

        private sealed class TimelineSortEntry
        {
            internal IMDataCoreEvent Event;
            internal bool HasDate;
            internal DateTime DateValue;
        }

        /// <summary>
        /// Restores one-time viewer-only senbatsu UI tweaks after popup closes.
        /// </summary>
        private sealed class DiarySingleSenbatsuViewerModeTracker : MonoBehaviour
        {
            private GameObject continueButton;
            private bool continueWasActive;
            private bool pendingRestore;
            internal bool ViewerModeActive { get; private set; }

            internal void HideContinueTemporarily(GameObject continueObject)
            {
                Restore();

                continueButton = continueObject;
                if (continueButton == null)
                {
                    return;
                }

                continueWasActive = continueButton.activeSelf;
                pendingRestore = true;
                ViewerModeActive = true;
                continueButton.SetActive(false);
            }

            private void OnDisable()
            {
                Restore();
            }

            private void OnDestroy()
            {
                Restore();
            }

            private void Restore()
            {
                if (!pendingRestore)
                {
                    return;
                }

                if (continueButton != null)
                {
                    continueButton.SetActive(continueWasActive);
                }

                continueButton = null;
                pendingRestore = false;
                ViewerModeActive = false;
            }
        }

        /// <summary>
        /// Returns true when native senbatsu popup is currently in diary viewer mode.
        /// </summary>
        internal static bool IsDiarySingleSenbatsuViewerActive(SinglePopup_Senbatsu popup)
        {
            if (popup == null || popup.gameObject == null)
            {
                return false;
            }

            DiarySingleSenbatsuViewerModeTracker tracker = popup.gameObject.GetComponent<DiarySingleSenbatsuViewerModeTracker>();
            return tracker != null && tracker.ViewerModeActive;
        }

        /// <summary>
        /// Initializes or refreshes UI context for current profile idol.
        /// </summary>
        internal void Initialize(Profile_Popup popup, data_girls.girls currentIdol)
        {
            if (popup == null)
            {
                return;
            }

            int previousIdolId = idol != null ? idol.id : C.InvalidId;
            int incomingIdolId = currentIdol != null
                ? currentIdol.id
                : (popup.Girl != null ? popup.Girl.id : C.InvalidId);

            profile = popup;
            idol = currentIdol ?? popup.Girl;
            if (previousIdolId != incomingIdolId)
            {
                selectedFilterEventTypes.Clear();
                maxEventsRenderCurrent = C.MaxEventsRender;
            }

            EnsureInjected();
            if (diaryVisible)
            {
                ReloadAndRender();
            }
        }

        /// <summary>
        /// Retries injection once the profile popup becomes active in hierarchy.
        /// </summary>
        private void OnEnable()
        {
            EnsureInjected();
        }

        /// <summary>
        /// Runs close safety-net when profile popup root gets disabled unexpectedly.
        /// </summary>
        private void OnDisable()
        {
            ForceHideDiaryPanels();
            TryRunLifecycleBackdropCleanup();
        }

        /// <summary>
        /// Runs close safety-net when profile popup root gets destroyed.
        /// </summary>
        private void OnDestroy()
        {
            ForceHideDiaryPanels();
            TryRunLifecycleBackdropCleanup();
        }

        /// <summary>
        /// Attempts one conservative backdrop cleanup after lifecycle close events.
        /// </summary>
        private static void TryRunLifecycleBackdropCleanup()
        {
            mainScript main;
            PopupManager popup;
            if (TryGetMainAndPopup(out main, out popup))
            {
                TryRunPopupBackdropSafetyNet(popup, true);
            }
        }

        /// <summary>
        /// Handles fallback when base tab is selected.
        /// </summary>
        internal void OnBaseTabSelected()
        {
            if (!diaryVisible)
            {
                return;
            }

            HideDiary(false);
        }

        /// <summary>
        /// Injects button and panel once.
        /// </summary>
        private void EnsureInjected()
        {
            if (profile == null || injectionInProgress)
            {
                return;
            }

            injectionInProgress = true;
            try
            {
                ResolveTemplates();
                EnsureTabButton();
                EnsurePanel();
                EnsureDetailPopup();
            }
            finally
            {
                injectionInProgress = false;
            }
        }

        /// <summary>
        /// Resolves source templates from existing profile tabs.
        /// </summary>
        private void ResolveTemplates()
        {
            if (profile == null || profile.Tabs == null)
            {
                return;
            }

            if (tabTemplate == null)
            {
                for (int i = C.ZeroIndex; i < profile.Tabs.Count; i++)
                {
                    Profile_Popup._tab tab = profile.Tabs[i];
                    if (tab != null && tab.Button != null)
                    {
                        tabTemplate = tab.Button;
                        break;
                    }
                }
            }

            if (panelTemplate == null)
            {
                for (int i = C.ZeroIndex; i < profile.Tabs.Count; i++)
                {
                    Profile_Popup._tab tab = profile.Tabs[i];
                    if (tab != null && tab.Type == Profile_Popup._tabs.bonds && tab.Obj != null)
                    {
                        panelTemplate = tab.Obj;
                        break;
                    }
                }

                if (panelTemplate == null)
                {
                    for (int i = C.ZeroIndex; i < profile.Tabs.Count; i++)
                    {
                        Profile_Popup._tab tab = profile.Tabs[i];
                        if (tab != null && tab.Obj != null)
                        {
                            panelTemplate = tab.Obj;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Ensures the header-level Career Diary button exists near close control.
        /// </summary>
        private void EnsureTabButton()
        {
            Button closeButton = FindCloseButton(profile);

            if (closeButton == null)
            {
                if (diaryTabButtonObject != null)
                {
                    diaryTabButtonObject.SetActive(false);
                }

                Log.Warn(C.TextCareerDiaryCloseButtonAnchorNotFoundDeferringButtonPlacement);
                return;
            }

            if (diaryTabButtonObject == null)
            {
                Transform root = profile != null ? profile.transform : null;
                Transform existing = FindChildByName(root, C.DiaryTabButtonName);
                if (existing == null && profile != null && profile.transform != null && profile.transform.parent != null)
                {
                    existing = FindChildByName(profile.transform.parent, C.DiaryTabButtonName);
                }

                if (existing != null)
                {
                    diaryTabButtonObject = existing.gameObject;
                }
            }

            if (diaryTabButtonObject == null)
            {
                Transform parent = closeButton.transform.parent;

                if (parent == null)
                {
                    Log.Warn(C.TextCareerDiaryButtonParentUnavailable);
                    return;
                }

                diaryTabButtonObject = UnityEngine.Object.Instantiate(closeButton.gameObject, parent, false);
                diaryTabButtonObject.name = C.DiaryTabButtonName;
                diaryTabButtonObject.SetActive(true);

                StripTabBehavior(diaryTabButtonObject);
            }

            if (diaryTabButtonObject != null)
            {
                diaryTabButtonObject.name = C.DiaryTabButtonName;
                diaryTabButtonObject.SetActive(true);
            }

            ApplyButtonLabel(diaryTabButtonObject, C.DiaryTabLabel);
            PlaceButtonNearCloseControl(diaryTabButtonObject, closeButton);

            if (diaryTabButtonObject != null && diaryTabButtonObject.transform.parent != null)
            {
                SetLayerRecursively(diaryTabButtonObject, diaryTabButtonObject.transform.parent.gameObject.layer);
            }

            CanvasGroup buttonCanvasGroup = diaryTabButtonObject.GetComponent<CanvasGroup>();
            if (buttonCanvasGroup != null)
            {
                buttonCanvasGroup.alpha = C.FloatOne;
                buttonCanvasGroup.interactable = true;
                buttonCanvasGroup.blocksRaycasts = true;
            }

            diaryTabButton = diaryTabButtonObject.GetComponent<Button>();
            if (diaryTabButton == null)
            {
                diaryTabButton = diaryTabButtonObject.AddComponent<Button>();
            }

            diaryTabButton.interactable = true;

            diaryTabButton.onClick.RemoveListener(OnDiaryButtonClicked);
            diaryTabButton.onClick.AddListener(OnDiaryButtonClicked);

            ButtonDefault bd = diaryTabButtonObject.GetComponent<ButtonDefault>();
            if (bd != null)
            {
                bd.DefaultTooltip = C.DiaryTabLabel;
                bd.SetTooltip(C.DiaryTabLabel);
                bd.Activate(true, false);
            }

            RebuildLayout(diaryTabButtonObject.transform.parent);
            UpdateDiaryButtonColor(false);
        }

        /// <summary>
        /// Ensures the custom diary panel exists.
        /// </summary>
        private void EnsurePanel()
        {
            if (diaryPanelObject == null)
            {
                Transform existing = FindChildByName(profile.transform, C.DiaryPanelName);
                if (existing != null)
                {
                    diaryPanelObject = existing.gameObject;
                }
            }

            if (diaryPanelObject == null)
            {
                if (panelTemplate == null || panelTemplate.transform.parent == null)
                {
                    Log.Warn(C.TextCareerDiaryPanelTemplateUnavailable);
                    return;
                }

                diaryPanelObject = UnityEngine.Object.Instantiate(panelTemplate, panelTemplate.transform.parent, false);
                diaryPanelObject.name = C.DiaryPanelName;
                diaryPanelObject.SetActive(false);
                diaryPanelObject.transform.SetSiblingIndex(panelTemplate.transform.GetSiblingIndex() + C.LastFromCount);
            }

            diaryContentRoot = ResolveContentRoot(diaryPanelObject, panelTemplate, profile.Bonds_Container);
            if (diaryContentRoot == null)
            {
                diaryContentRoot = diaryPanelObject.transform;
            }

            EnsureVerticalContentLayout(diaryContentRoot);
            ClearChildren(diaryContentRoot);
        }

        /// <summary>
        /// Ensures dedicated event detail popup panel exists.
        /// </summary>
        private void EnsureDetailPopup()
        {
            if (diaryDetailPopupObject == null)
            {
                Transform existing = FindChildByName(profile.transform, C.DiaryDetailPopupName);
                if (existing != null)
                {
                    diaryDetailPopupObject = existing.gameObject;
                }
            }

            if (diaryDetailPopupObject == null)
            {
                if (panelTemplate == null || panelTemplate.transform.parent == null)
                {
                    Log.Warn(C.TextCareerDiaryDetailPopupTemplateUnavailable);
                    return;
                }

                diaryDetailPopupObject = UnityEngine.Object.Instantiate(panelTemplate, panelTemplate.transform.parent, false);
                diaryDetailPopupObject.name = C.DiaryDetailPopupName;
                diaryDetailPopupObject.SetActive(false);
                diaryDetailPopupObject.transform.SetSiblingIndex(panelTemplate.transform.GetSiblingIndex() + C.DetailPopupSiblingOffset);
            }

            diaryDetailContentRoot = ResolveContentRoot(diaryDetailPopupObject, panelTemplate, profile.Bonds_Container);
            if (diaryDetailContentRoot == null)
            {
                diaryDetailContentRoot = diaryDetailPopupObject.transform;
            }

            EnsureVerticalContentLayout(diaryDetailContentRoot);
            ClearChildren(diaryDetailContentRoot);
        }

        /// <summary>
        /// Handles custom diary button click.
        /// </summary>
        private void OnDiaryButtonClicked()
        {
            if (diaryVisible)
            {
                HideDiary(true);
                return;
            }

            ShowDiary();
        }

        /// <summary>
        /// Shows diary panel.
        /// </summary>
        private void ShowDiary()
        {
            if (profile == null || diaryPanelObject == null)
            {
                return;
            }

            HideBaseTabs(profile);
            diaryPanelObject.SetActive(true);
            if (diaryDetailPopupObject != null)
            {
                diaryDetailPopupObject.SetActive(false);
            }

            diaryVisible = true;
            UpdateDiaryButtonColor(true);

            ReloadAndRender();
        }

        /// <summary>
        /// Hides diary and detail popup.
        /// </summary>
        private void HideDiary(bool ignoredRestoreSelected)
        {
            ForceHideDiaryPanels();
            RestoreSelectedTab(profile);
        }

        /// <summary>
        /// Hard-disables diary and detail panels to avoid invisible raycast blockers.
        /// </summary>
        private void ForceHideDiaryPanels()
        {
            if (diaryPanelObject != null)
            {
                diaryPanelObject.SetActive(false);
            }

            if (diaryDetailPopupObject != null)
            {
                diaryDetailPopupObject.SetActive(false);
            }

            diaryVisible = false;
            UpdateDiaryButtonColor(false);
        }

        /// <summary>
        /// Reloads timeline from core and renders UI.
        /// </summary>
        private void ReloadAndRender()
        {
            LoadEvents();
            RenderDiary();
        }

        /// <summary>
        /// Loads idol events from IM Data Core.
        /// </summary>
        private void LoadEvents()
        {
            cachedEvents.Clear();
            loadWarning = string.Empty;

            if (idol == null || idol.id < C.MinId)
            {
                loadWarning = C.TextActiveIdolContextUnavailable;
                return;
            }

            List<IMDataCoreEvent> events;
            string error;
            if (!IMDataCoreApi.TryReadRecentEventsForIdol(idol.id, C.MaxEventsRequest, out events, out error))
            {
                loadWarning = string.IsNullOrEmpty(error) ? C.TextTimelineQueryFailed : error;
                return;
            }

            if (events != null)
            {
                HashSet<string> supersedingEventKeys = BuildSupersedingEventKeys(events);
                HashSet<string> singleReleaseIdentityKeys = new HashSet<string>(StringComparer.Ordinal);
                HashSet<string> showCancelledIdentityKeys = new HashSet<string>(StringComparer.Ordinal);
                HashSet<string> idolDatingStartMomentKeys = BuildIdolDatingLifecycleMomentKeys(events, C.EventIdolDatingStarted);
                HashSet<string> idolDatingEndMomentKeys = BuildIdolDatingLifecycleMomentKeys(events, C.EventIdolDatingEnded);
                for (int i = C.ZeroIndex; i < events.Count; i++)
                {
                    IMDataCoreEvent ev = events[i];
                    if (ev == null)
                    {
                        continue;
                    }

                    if (IsSuppressedSetStatusEvent(ev) || IsSuppressedTimelineNoiseEvent(ev))
                    {
                        continue;
                    }

                    if (!IsRelevantToCurrentIdol(ev))
                    {
                        continue;
                    }

                    if (IsSuppressedIdolDatingStatusStub(ev, idolDatingStartMomentKeys, idolDatingEndMomentKeys))
                    {
                        continue;
                    }

                    if (IsSupersededDuplicateEvent(ev, supersedingEventKeys))
                    {
                        continue;
                    }

                    if (IsDuplicateSingleReleaseEvent(ev, singleReleaseIdentityKeys))
                    {
                        continue;
                    }

                    if (IsDuplicateShowCancelledEvent(ev, showCancelledIdentityKeys))
                    {
                        continue;
                    }

                    cachedEvents.Add(ev);
                }
            }

            SortEventsForTimeline(cachedEvents);

            if (selectedEventId <= C.InvalidEventId)
            {
                long persisted;
                if (Runtime.TryLoadSelectedEventId(idol.id, out persisted))
                {
                    selectedEventId = persisted;
                }
            }

            if (!ContainsEventId(selectedEventId) && cachedEvents.Count > C.MinId)
            {
                selectedEventId = cachedEvents[C.ZeroIndex].EventId;
            }
        }

        /// <summary>
        /// Returns true when one technical SetStatus event should be hidden from diary timeline.
        /// </summary>
        private static bool IsSuppressedSetStatusEvent(IMDataCoreEvent ev)
        {
            if (ev == null)
            {
                return false;
            }

            if (string.Equals(ev.EventType, C.EventStatusChanged, StringComparison.Ordinal))
            {
                return true;
            }

            string sourcePatch = ev.SourcePatch ?? string.Empty;
            return sourcePatch.IndexOf(C.PatchTokenSetStatus, StringComparison.OrdinalIgnoreCase) >= C.ZeroIndex;
        }

        /// <summary>
        /// Returns true when one high-volume/non-diary event should be hidden from idol timelines.
        /// </summary>
        private static bool IsSuppressedTimelineNoiseEvent(IMDataCoreEvent ev)
        {
            if (ev == null)
            {
                return false;
            }

            string eventType = ev.EventType ?? string.Empty;
            if (string.Equals(eventType, C.EventResearchPointsAccrued, StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(eventType, C.EventIdolEarningsRecorded, StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(eventType, C.EventAgencyRoomDestroyed, StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(eventType, C.EventStaffHired, StringComparison.Ordinal) ||
                string.Equals(eventType, C.EventStaffFired, StringComparison.Ordinal) ||
                string.Equals(eventType, C.EventStaffFiredSeverance, StringComparison.Ordinal))
            {
                JSONNode payload = ParsePayload(ev.PayloadJson);
                return !IsIdolStaffLifecycleEvent(payload);
            }

            return false;
        }

        /// <summary>
        /// Builds explicit output for tour lifecycle and participation events.
        /// </summary>
        private static void BuildTourPresentation(string type, IMDataCoreEvent ev, JSONNode payload, Presentation p, List<string> lines)
        {
            if (type == C.EventTourCreated)
            {
                p.Title = C.TextTourCreated;
            }
            else if (type == C.EventTourStarted)
            {
                p.Title = C.TextTourStarted;
            }
            else if (type == C.EventTourFinished)
            {
                p.Title = C.TextTourFinished;
            }
            else if (type == C.EventTourCancelled)
            {
                p.Title = C.TextTourCancelled;
            }
            else if (type == C.EventTourCountryResult)
            {
                p.Title = C.TextTourCountryResult;
            }
            else
            {
                p.Title = C.TextTourParticipationRecorded;
            }

            p.WithWhom = C.TextTour + Normalize(ev != null ? ev.EntityId : string.Empty);
            AddCodeLineIfKnown(lines, C.TextTourStatus, ReadStr(payload, C.KeyTourStatus));
            AddCodeLineIfKnown(lines, C.LabelAction, ReadStr(payload, C.KeyTourLifecycleAction));
            AddIntLineIfPresent(lines, C.TextParticipantCount, payload, C.KeyTourParticipantCount);
            string participants = BuildIdolNameSummaryFromCsv(ReadStr(payload, C.JsonTourParticipantIdList), C.MaxNamesInOutcomeSummary);
            if (!string.IsNullOrEmpty(participants))
            {
                lines.Add(C.TextParticipants + participants);
            }

            AddIntLineIfPresent(lines, C.TextCountryCount, payload, C.JsonTourCountryCount);
            AddLongLineIfNonZero(lines, C.LabelAudience, ReadLong(payload, C.JsonTourAudience));
            AddLongLineIfNonZero(lines, C.JsonTheaterRevenue, ReadLong(payload, C.JsonTourRevenue));
            AddLongLineIfNonZero(lines, C.TextNewFans, ReadLong(payload, C.JsonTourNewFans));
            AddLongLineIfNonZero(lines, C.TextProductionCost, ReadLong(payload, C.JsonTourCost));
            AddLongLineIfNonZero(lines, C.LabelProfit, ReadLong(payload, C.KeyTourProfit));
            AddDateLineIfKnown(lines, C.TextStartDate, ReadStr(payload, C.KeyTourStartDate));
            AddDateLineIfKnown(lines, C.TextFinishDate, ReadStr(payload, C.JsonTourFinishDate));

            if (type == C.EventTourCountryResult)
            {
                AddCodeLineIfKnown(lines, C.LabelCountry, ReadStr(payload, C.KeyTourCountryCode));
                AddIntLineIfPresent(lines, C.TextCountryLevel, payload, C.KeyTourCountryLevel);
                AddLongLineIfNonZero(lines, C.TextCountryAttendance, ReadLong(payload, C.KeyTourCountryAttendance));
                AddLongLineIfNonZero(lines, C.TextCountryAudience, ReadLong(payload, C.KeyTourCountryAudience));
                AddLongLineIfNonZero(lines, C.TextCountryRevenue, ReadLong(payload, C.KeyTourCountryRevenue));
                AddLongLineIfNonZero(lines, C.TextCountryNewFans, ReadLong(payload, C.KeyTourCountryNewFans));
            }
        }

        /// <summary>
        /// Builds explicit output for election lifecycle events.
        /// </summary>
        private static void BuildElectionPresentation(string type, IMDataCoreEvent ev, JSONNode payload, Presentation p, List<string> lines)
        {
            if (type == C.EventElectionCreated)
            {
                p.Title = C.TextElectionCreated;
            }
            else if (type == C.EventElectionStarted)
            {
                p.Title = C.TextElectionStarted;
            }
            else if (type == C.EventElectionFinished)
            {
                p.Title = C.TextElectionFinished;
            }
            else if (type == C.EventElectionCancelled)
            {
                p.Title = C.TextElectionCancelled;
            }
            else if (type == C.EventElectionResultsGenerated)
            {
                p.Title = C.TextElectionResultsGenerated;
            }
            else
            {
                p.Title = C.TextElectionPlaceAdjusted;
            }

            p.WithWhom = C.TextElection + Normalize(ev != null ? ev.EntityId : string.Empty);
            AddTransitionLine(lines, C.LabelStatus, ReadStr(payload, C.JsonElectionPrevStatus), ReadStr(payload, C.JsonElectionNewStatus));
            AddCodeLineIfKnown(lines, C.LabelBroadcast, ReadStr(payload, C.JsonElectionBroadcast));
            AddLongTransitionLine(lines, C.LabelPlace, payload, C.KeyElectionPlaceBefore, C.KeyElectionPlaceAfter);
            AddIntLineIfPresent(lines, C.LabelPlace, payload, C.JsonElectionPlace);
            AddLongLineIfNonZero(lines, C.LabelVotes, ReadLong(payload, C.JsonElectionVotes));
            AddLongLineIfNonZero(lines, C.TextFamePoints, ReadLong(payload, C.JsonElectionFamePoints));
            AddIntLineIfPresent(lines, C.TextResultCount, payload, C.JsonElectionResultCount);
            AddDateLineIfKnown(lines, C.TextFinishDate, ReadStr(payload, C.JsonElectionFinishDate));
        }

        /// <summary>
        /// Builds explicit output for concert and concert-adjacent events.
        /// </summary>
        private static void BuildConcertPresentation(string type, IMDataCoreEvent ev, JSONNode payload, Presentation p, List<string> lines)
        {
            if (type == C.EventConcertCreated)
            {
                p.Title = C.TextConcertCreated;
            }
            else if (type == C.EventConcertStarted)
            {
                p.Title = C.TextConcertStarted;
            }
            else if (type == C.EventConcertFinished)
            {
                p.Title = C.TextConcertFinished;
            }
            else if (type == C.EventConcertCancelled)
            {
                p.Title = C.TextConcertCancelled;
            }
            else if (type == C.EventConcertParticipation)
            {
                p.Title = C.TextConcertParticipationRecorded;
            }
            else if (type == C.EventConcertCastChanged)
            {
                p.Title = C.TextConcertCastUpdated;
            }
            else if (type == C.EventConcertStatusChanged)
            {
                p.Title = C.TextConcertStatusUpdated;
            }
            else if (type == C.EventConcertConfigurationChanged)
            {
                p.Title = C.TextConcertConfigurationUpdated;
            }
            else if (type == C.EventConcertCardUsed)
            {
                p.Title = C.TextConcertCardUsed;
            }
            else if (type == C.EventConcertCrisisDecision)
            {
                p.Title = C.TextConcertCrisisDecision;
            }
            else if (type == C.EventConcertCrisisApplied)
            {
                p.Title = C.TextConcertCrisisApplied;
            }
            else
            {
                p.Title = C.TextConcertFinalOutcome;
            }

            string title = NormalizeRawText(ReadStr(payload, C.KeyConcertTitle));
            p.WithWhom = title != C.LabelUnknown ? title : (C.TextConcert + Normalize(ev != null ? ev.EntityId : string.Empty));
            AddCodeLineIfKnown(lines, C.LabelVenue, ReadStr(payload, C.KeyConcertVenue));
            AddTransitionLine(lines, C.LabelStatus, ReadStr(payload, C.KeyConcertPreviousStatus), ReadStr(payload, C.KeyConcertNewStatus));
            AddCodeLineIfKnown(lines, C.LabelStatus, ReadStr(payload, C.KeyConcertStatus));
            AddIntLineIfPresent(lines, C.LabelSongs, payload, C.KeyConcertSongCount);
            AddIntLineIfPresent(lines, C.LabelParticipants, payload, C.KeyConcertParticipantCount);
            AddIntTransitionLine(lines, C.LabelParticipants, payload, C.KeyConcertParticipantCountBefore, C.KeyConcertParticipantCountAfter);

            string participants = BuildIdolNameSummaryFromCsv(ReadStr(payload, C.JsonConcertParticipantIdList), C.MaxNamesInOutcomeSummary);
            if (!string.IsNullOrEmpty(participants))
            {
                lines.Add(C.TextLineup + participants);
            }

            string added = BuildIdolNameSummaryFromCsv(ReadStr(payload, C.KeyConcertParticipantIdListAdded), C.MaxNamesInOutcomeSummary);
            if (!string.IsNullOrEmpty(added))
            {
                lines.Add(C.TextAddedParticipants + added);
            }

            string removed = BuildIdolNameSummaryFromCsv(ReadStr(payload, C.KeyConcertParticipantIdListRemoved), C.MaxNamesInOutcomeSummary);
            if (!string.IsNullOrEmpty(removed))
            {
                lines.Add(C.TextRemovedParticipants + removed);
            }

            int removedId = ReadId(payload, C.KeyConcertRemovedIdolId);
            if (removedId >= C.MinId)
            {
                lines.Add(C.TextRemovedIdol + ResolveIdolNameById(removedId));
            }

            AddLongLineIfNonZero(lines, C.TextProjectedAudience, ReadLong(payload, C.KeyConcertProjectedAudience));
            AddLongLineIfNonZero(lines, C.TextActualAudience, ReadLong(payload, C.KeyConcertActualAudience));
            AddLongLineIfNonZero(lines, C.TextProjectedRevenue, ReadLong(payload, C.KeyConcertProjectedRevenue));
            AddLongLineIfNonZero(lines, C.TextActualRevenue, ReadLong(payload, C.KeyConcertActualRevenue));
            AddLongLineIfNonZero(lines, C.TextProductionCost, ReadLong(payload, C.KeyConcertProductionCost));
            AddLongLineIfNonZero(lines, C.LabelProfit, ReadLong(payload, C.KeyConcertProfit));
            AddLongLineIfNonZero(lines, C.TextTicketPrice, ReadLong(payload, C.KeyConcertTicketPrice));
            AddLongTransitionLine(lines, C.TextTicketPrice, payload, C.KeyConcertTicketPriceBefore, C.KeyConcertTicketPriceAfter);
            AddCodeLineIfKnown(lines, C.TextCardType, ReadStr(payload, C.KeyCardType));
            AddIntLineIfPresent(lines, C.TextCardLevel, payload, C.KeyCardLevel);
            AddLongLineIfNonZero(lines, C.TextCardEffect, ReadLong(payload, C.KeyCardEffectValue));
            AddIntTransitionLine(lines, C.TextCardsRemaining, payload, C.KeyCardsBefore, C.KeyCardsAfter);
            AddRawLineIfKnown(lines, C.LabelCrisis, ReadStr(payload, C.KeyAccidentTitle));
            AddCodeLineIfKnown(lines, C.LabelChoice, ReadStr(payload, C.KeyChoiceType));
            AddCodeLineIfKnown(lines, C.LabelResult, ReadStr(payload, C.KeyResultType));
            AddSignedLineIfNonZero(lines, C.TextHypeDelta, ReadLong(payload, C.KeyHypeDeltaApplied));
            AddSignedLineIfNonZero(lines, C.TextHypeDelta, ReadLong(payload, C.KeyResultHypeDelta));
            AddSignedLineIfNonZero(lines, C.TextExpectedHypeDelta, ReadLong(payload, C.KeyExpectedHypeDelta));
            AddIntLineIfPresent(lines, C.TextSuccessChance, payload, C.KeyAccidentSuccessChance);
            AddLongLineIfNonZero(lines, C.TextFinalRevenue, ReadLong(payload, C.KeyActualRevenue));
            AddLongLineIfNonZero(lines, C.TextFinalProfit, ReadLong(payload, C.KeyActualProfit));
            AddLongLineIfNonZero(lines, C.TextIdolPayout, ReadLong(payload, C.KeyIdolPayoutTotal));
            AddRawLineIfKnown(lines, C.LabelAccidents, ReadStr(payload, C.KeyUsedAccidentTitles));
            AddDateLineIfKnown(lines, C.TextFinishDate, ReadStr(payload, C.KeyConcertFinishDate));
            AddDateLineIfKnown(lines, C.LabelDate, ReadStr(payload, C.KeyEventDate));
        }

        /// <summary>
        /// Builds explicit output for staff lifecycle events.
        /// </summary>
        private static void BuildStaffPresentation(string type, IMDataCoreEvent ev, JSONNode payload, Presentation p, List<string> lines)
        {
            if (type == C.EventStaffHired)
            {
                p.Title = C.TextStaffHired;
            }
            else if (type == C.EventStaffFired)
            {
                p.Title = C.TextStaffFired;
            }
            else if (type == C.EventStaffFiredSeverance)
            {
                p.Title = C.TextStaffFiredWithSeverance;
            }
            else
            {
                p.Title = C.TextStaffLeveledUp;
            }

            int idolId = ReadId(payload, C.JsonIdolId);
            if (idolId < C.MinId)
            {
                idolId = ResolveIdFromEvent(ev);
            }

            string idolName = ResolveIdolNameById(idolId);
            string staffName = NormalizeRawText(ReadStr(payload, C.KeyStaffName));
            p.WithWhom = idolName != C.LabelUnknown ? idolName : staffName;
            AddRawLineIfKnown(lines, C.LabelStaff, ReadStr(payload, C.KeyStaffName));
            AddCodeLineIfKnown(lines, C.LabelRole, ReadStr(payload, C.JsonStaffType));
            AddCodeLineIfKnown(lines, C.LabelAction, ReadStr(payload, C.KeyStaffAction));
            AddLongLineIfNonZero(lines, C.LabelSalary, ReadLong(payload, C.KeyStaffSalary));
            AddLongTransitionLine(lines, C.LabelLevel, payload, C.KeyStaffLevelBefore, C.KeyStaffLevelAfter);
            AddLongLineIfNonZero(lines, C.TextSeveranceCost, ReadLong(payload, C.KeySeveranceCost));
            AddSignedLineIfNonZero(lines, C.TextMoneyChange, ReadLong(payload, C.JsonActivityMoneyDelta));
            AddCodeLineIfKnown(lines, C.LabelRoom, ReadStr(payload, C.KeyRoomType));
            if (HasPayloadValue(payload, C.KeyFireForced))
            {
                lines.Add(C.TextForcedFiring + YesNo(ReadBool(payload, C.KeyFireForced)));
            }
        }

        /// <summary>
        /// Builds explicit output for loan lifecycle events.
        /// </summary>
        private static void BuildLoanPresentation(string type, IMDataCoreEvent ev, JSONNode payload, Presentation p, List<string> lines)
        {
            if (type == C.EventLoanAdded)
            {
                p.Title = C.TextLoanAdded;
            }
            else if (type == C.EventLoanInitialized)
            {
                p.Title = C.TextLoanInitialized;
            }
            else
            {
                p.Title = C.TextLoanPaidOff;
            }

            int loanId = ReadId(payload, C.KeyLoanId);
            if (loanId < C.MinId && ev != null)
            {
                int parsedLoanId;
                if (TryParseInt(ev.EntityId, out parsedLoanId))
                {
                    loanId = parsedLoanId;
                }
            }

            p.WithWhom = loanId >= C.MinId
                ? C.TextLoan + loanId.ToString(CultureInfo.InvariantCulture)
                : C.LabelAgency;

            AddIntLineIfPresent(lines, C.TextLoanId, payload, C.KeyLoanId);
            AddCodeLineIfKnown(lines, C.LabelAction, ReadStr(payload, C.KeyLoanLifecycleAction));
            AddCodeLineIfKnown(lines, C.TextLoanType, ReadStr(payload, C.KeyLoanType));
            AddCodeLineIfKnown(lines, C.TextLoanDuration, ReadStr(payload, C.KeyLoanDuration));
            AddBoolTransitionLine(lines, C.TextLoanActive, payload, C.KeyLoanActiveBefore, C.KeyLoanActiveAfter);
            AddLongLineIfPresent(lines, C.TextLoanAmount, payload, C.KeyLoanAmount);
            AddIntLineIfPresent(lines, C.TextLoanPaymentPerWeek, payload, C.KeyLoanPaymentPerWeek);
            AddIntLineIfPresent(lines, C.TextLoanInterestRate, payload, C.KeyLoanInterestRate);
            AddDateLineIfKnown(lines, C.TextStartDate, ReadStr(payload, C.KeyLoanStartDate));
            AddDateLineIfKnown(lines, C.TextEndDate, ReadStr(payload, C.KeyLoanEndDate));
            AddLongTransitionLine(lines, C.TextLoanDebt, payload, C.KeyLoanDebtBefore, C.KeyLoanDebtAfter);
            AddBoolLineIfPresent(lines, C.TextLoanCanPayOff, payload, C.KeyLoanCanPayOffAfter);
            AddBoolLineIfPresent(lines, C.TextLoanInDevelopment, payload, C.KeyLoanInDevelopmentAfter);
            AddIntLineIfPresent(lines, C.TextLoanDaysToDevelop, payload, C.KeyLoanDaysToDevelop);
            AddIntLineIfPresent(lines, C.TextActiveLoanCount, payload, C.KeyLoanCountActive);
            AddIntLineIfPresent(lines, C.TextTotalLoanCount, payload, C.KeyLoanCountTotal);
            AddIntLineIfPresent(lines, C.TextTotalLoanPaymentPerWeek, payload, C.KeyLoanTotalPaymentPerWeek);
            AddLongLineIfPresent(lines, C.TextTotalDebt, payload, C.KeyLoanTotalDebt);
            AddLongTransitionLine(lines, C.TextMoneyOnHand, payload, C.KeyMoneyBefore, C.KeyMoneyAfter);
            AddSignedLineIfNonZero(lines, C.TextMoneyChange, ReadLong(payload, C.JsonActivityMoneyDelta));
        }

        /// <summary>
        /// Builds explicit output for bankruptcy lifecycle/check events.
        /// </summary>
        private static void BuildBankruptcyPresentation(string type, JSONNode payload, Presentation p, List<string> lines)
        {
            p.Title = type == C.EventBankruptcyDangerSet ? C.TextBankruptcyDangerSet : C.TextBankruptcyCheck;
            p.WithWhom = C.TextBankruptcy;

            AddBoolTransitionLine(lines, C.TextBankruptcyDanger, payload, C.KeyBankruptcyDangerBefore, C.KeyBankruptcyDangerAfter);
            if (type == C.EventBankruptcyDangerSet)
            {
                AddBoolLineIfPresent(lines, C.TextRequestedValue, payload, C.KeyRequestedValue);
                AddDateTransitionLineIfKnown(lines, C.TextBankruptcyDate, ReadStr(payload, C.KeyBankruptcyDateBefore), ReadStr(payload, C.KeyBankruptcyDateAfter));
                AddIntLineIfPresent(lines, C.TextBankruptcyDaysRemaining, payload, C.KeyBankruptcyDaysRemainingAfter);
                AddLongLineIfPresent(lines, C.TextMoneyOnHand, payload, C.KeyMoneyAfter);
                AddLongLineIfPresent(lines, C.TextTotalDebt, payload, C.KeyTotalDebtAfter);
                return;
            }

            AddIntTransitionLine(lines, C.TextBankruptcyDaysRemaining, payload, C.KeyBankruptcyDaysRemainingBefore, C.KeyBankruptcyDaysRemainingAfter);
            AddLongTransitionLine(lines, C.TextMoneyOnHand, payload, C.KeyMoneyBefore, C.KeyMoneyAfter);
            AddLongTransitionLine(lines, C.TextTotalDebt, payload, C.KeyTotalDebtBefore, C.KeyTotalDebtAfter);
            AddBoolTransitionLine(lines, C.TextBailoutUsed, payload, C.KeyBailoutUsedBefore, C.KeyBailoutUsedAfter);
            AddBoolTransitionLine(lines, C.TextStoryRecruitUsed, payload, C.KeyStoryRecruitUsedBefore, C.KeyStoryRecruitUsedAfter);
            AddBoolTransitionLine(lines, C.TextBankruptcyGameOverUsed, payload, C.KeyGameOverBankruptcyUsedBefore, C.KeyGameOverBankruptcyUsedAfter);
            if (C.ShowTechnicalEventMetadata)
            {
                AddCodeLineIfKnown(lines, C.TextTriggeredDialogue, ReadStr(payload, C.KeyTriggeredDialogue));
            }
        }

        /// <summary>
        /// Builds explicit output for cafe lifecycle and daily result events.
        /// </summary>
        private static void BuildCafePresentation(string type, IMDataCoreEvent ev, JSONNode payload, Presentation p, List<string> lines)
        {
            if (type == C.EventCafeCreated)
            {
                p.Title = C.TextCafeCreated;
            }
            else if (type == C.EventCafeDestroyed)
            {
                p.Title = C.TextCafeDestroyed;
            }
            else
            {
                p.Title = C.TextCafeDailyResult;
            }

            int cafeId = ReadId(payload, C.KeyCafeId);
            if (cafeId < C.MinId && ev != null)
            {
                int parsedCafeId;
                if (TryParseInt(ev.EntityId, out parsedCafeId))
                {
                    cafeId = parsedCafeId;
                }
            }

            string cafeTitle = NormalizeRawText(ReadStr(payload, C.KeyCafeTitle));
            p.WithWhom = cafeTitle != C.LabelUnknown
                ? cafeTitle
                : (cafeId >= C.MinId ? C.TextCafe + cafeId.ToString(CultureInfo.InvariantCulture) : C.LabelAgency);

            AddIntLineIfPresent(lines, C.TextCafeId, payload, C.KeyCafeId);
            AddIntLineIfPresent(lines, C.TextGroupId, payload, C.JsonGroupId);
            if (type == C.EventCafeDailyResult)
            {
                AddIntLineIfPresent(lines, C.TextDishId, payload, C.KeyDishId);
                AddCodeLineIfKnown(lines, C.TextDishType, ReadStr(payload, C.KeyDishType));
                AddRawLineIfKnown(lines, C.TextDishTitle, ReadStr(payload, C.KeyDishTitle));
                AddLongLineIfPresent(lines, C.TextCafeProfit, payload, C.KeyCafeProfit);
                AddLongLineIfPresent(lines, C.TextNewFans, payload, C.JsonNewFans);
                AddCodeLineIfKnown(lines, C.TextFanType, ReadStr(payload, C.KeyFanType));
                AddIntLineIfPresent(lines, C.TextStaffedIdolCount, payload, C.KeyStaffedIdolCount);

                string staffedIdols = BuildIdolNameSummaryFromCsv(ReadStr(payload, C.JsonStaffedIdolIdList), C.MaxNamesInOutcomeSummary);
                if (!string.IsNullOrEmpty(staffedIdols))
                {
                    lines.Add(C.TextStaffedIdols + staffedIdols);
                }

                AddLongTransitionLine(lines, C.TextMoneyOnHand, payload, C.KeyTotalMoneyBefore, C.KeyTotalMoneyAfter);
                AddSignedLineIfNonZero(lines, C.TextMoneyChange, ReadLong(payload, C.KeyTotalMoneyDelta));
                AddDateLineIfKnown(lines, C.LabelDate, ReadStr(payload, C.KeyResultDate));
                return;
            }

            AddCodeLineIfKnown(lines, C.LabelAction, ReadStr(payload, C.KeyCafeLifecycleAction));
            AddIntLineIfPresent(lines, C.TextRoomTheaterId, payload, C.KeyRoomTheaterId);
            AddIntLineIfPresent(lines, C.TextWaitStaffCount, payload, C.KeyWaitStaffCount);
            AddIntLineIfPresent(lines, C.TextWorkingStaffCount, payload, C.KeyWorkingStaffCount);
            AddCodeLineIfKnown(lines, C.TextCafePriority, ReadStr(payload, C.KeyCafePriority));
            AddCodeLineIfKnown(lines, C.TextStaffPriority, ReadStr(payload, C.KeyStaffPriority));
            string menuSummary = BuildCafeMenuDisplaySummary(cafeId, ReadStr(payload, C.KeyMenuSummary));
            if (!string.IsNullOrEmpty(menuSummary))
            {
                lines.Add(C.TextMenuSummary + C.SeparatorColonSpace + menuSummary);
            }

            if (C.ShowTechnicalEventMetadata)
            {
                AddRawLineIfKnown(lines, C.TextMenuSummary, ReadStr(payload, C.KeyMenuSummary));
            }

            AddDateLineIfKnown(lines, C.LabelDate, ReadStr(payload, C.KeyLifecycleDate));
        }

        /// <summary>
        /// Builds explicit output for mentorship lifecycle and weekly-tick events.
        /// </summary>
        private static void BuildMentorshipPresentation(string type, JSONNode payload, Presentation p, List<string> lines)
        {
            if (type == C.EventMentorshipStarted)
            {
                p.Title = C.TextMentorshipStarted;
            }
            else if (type == C.EventMentorshipEnded)
            {
                p.Title = C.TextMentorshipEnded;
            }
            else
            {
                p.Title = C.TextMentorshipWeeklyTick;
            }

            int mentorId = ReadId(payload, C.KeyMentorId);
            int kohaiId = ReadId(payload, C.KeyKohaiId);
            string mentorName = ResolveIdolNameById(mentorId);
            string kohaiName = ResolveIdolNameById(kohaiId);
            if (mentorName == C.LabelUnknown && kohaiName == C.LabelUnknown)
            {
                p.WithWhom = C.TextMentorship;
            }
            else if (mentorName == C.LabelUnknown)
            {
                p.WithWhom = kohaiName;
            }
            else if (kohaiName == C.LabelUnknown)
            {
                p.WithWhom = mentorName;
            }
            else
            {
                p.WithWhom = mentorName + C.SeparatorArrow + kohaiName;
            }

            AddCodeLineIfKnown(lines, C.LabelAction, ReadStr(payload, C.KeyMentorshipAction));
            if (mentorId >= C.MinId)
            {
                lines.Add(C.TextMentor + mentorName);
            }

            if (kohaiId >= C.MinId)
            {
                lines.Add(C.TextKohai + kohaiName);
            }

            AddIntLineIfPresent(lines, C.TextActiveMentorCount, payload, C.KeyActiveMentorCount);
            AddFloatTransitionLine(lines, C.TextSenpaiToKohaiRatio, payload, C.KeySenpaiToKohaiRatioBefore, C.KeySenpaiToKohaiRatioAfter);
            AddFloatTransitionLine(lines, C.TextKohaiToSenpaiRatio, payload, C.KeyKohaiToSenpaiRatioBefore, C.KeyKohaiToSenpaiRatioAfter);
            string mentorPairsSummary = BuildMentorPairsDisplaySummary(ReadStr(payload, C.KeyMentorPairsSummary));
            if (!string.IsNullOrEmpty(mentorPairsSummary) && !IsMentorPairsSummaryRedundant(ReadStr(payload, C.KeyMentorPairsSummary), mentorId, kohaiId))
            {
                lines.Add(C.TextMentorPairsSummary + C.SeparatorColonSpace + mentorPairsSummary);
            }

            if (C.ShowTechnicalEventMetadata)
            {
                AddRawLineIfKnown(lines, C.TextMentorPairsSummary, ReadStr(payload, C.KeyMentorPairsSummary));
            }

            AddDateLineIfKnown(lines, C.LabelDate, ReadStr(payload, C.KeyEventDate));
        }

        /// <summary>
        /// Builds explicit output for scandal-check trigger events.
        /// </summary>
        private static void BuildScandalCheckPresentation(JSONNode payload, Presentation p, List<string> lines)
        {
            p.Title = C.TextScandalCheck;
            p.WithWhom = C.LabelAgency;

            AddBoolLineIfPresent(lines, C.TextTestGameOver, payload, C.KeyTestGameOver);
            AddLongTransitionLine(lines, C.TextScandalPoints, payload, C.KeyScandalPointsTotalBefore, C.KeyScandalPointsTotalAfter);
            AddIntLineIfPresent(lines, C.TextScandalThreshold, payload, C.KeyScandalThreshold);
            AddBoolTransitionLine(lines, C.TextFirstScandalUsed, payload, C.KeyFirstScandalUsedBefore, C.KeyFirstScandalUsedAfter);
            AddBoolTransitionLine(lines, C.TextWarningUsed, payload, C.KeyWarningUsedBefore, C.KeyWarningUsedAfter);
            AddBoolTransitionLine(lines, C.TextParentsUsed, payload, C.KeyParentsUsedBefore, C.KeyParentsUsedAfter);
            AddBoolTransitionLine(lines, C.TextGameOverUsed, payload, C.KeyGameOverUsedBefore, C.KeyGameOverUsedAfter);
            AddBoolTransitionLine(lines, C.TextScandalParentCooldown, payload, C.KeyScandalParentCooldownBefore, C.KeyScandalParentCooldownAfter);
            AddBoolTransitionLine(lines, C.TextAuditionFailure, payload, C.KeyAuditionFailureBefore, C.KeyAuditionFailureAfter);
            AddIntTransitionLine(lines, C.TextActiveIdolCount, payload, C.KeyActiveIdolCountBefore, C.KeyActiveIdolCountAfter);
            if (C.ShowTechnicalEventMetadata)
            {
                AddCodeLineIfKnown(lines, C.TextTriggeredDialogue, ReadStr(payload, C.KeyTriggeredDialogue));
            }
        }

        /// <summary>
        /// Builds explicit output for story route lock events.
        /// </summary>
        private static void BuildStoryRouteLockedPresentation(JSONNode payload, Presentation p, List<string> lines)
        {
            p.Title = C.TextStoryRouteLocked;
            string routeAfter = HumanizeUnknown(ReadStr(payload, C.KeyRouteAfter));
            string routeBefore = HumanizeUnknown(ReadStr(payload, C.KeyRouteBefore));
            p.WithWhom = routeAfter != C.LabelUnknown
                ? routeAfter
                : (routeBefore != C.LabelUnknown ? routeBefore : C.LabelStory);

            AddTransitionLine(lines, C.LabelRoute, ReadStr(payload, C.KeyRouteBefore), ReadStr(payload, C.KeyRouteAfter));
            AddIntTransitionLine(lines, C.TextActiveTaskCount, payload, C.KeyActiveTaskCountBefore, C.KeyActiveTaskCountAfter);
            AddIntLineIfPresent(lines, C.TextRemovedTaskCount, payload, C.KeyRemovedTaskCount);

            string removedTasks = BuildHumanizedCodeListSummary(ReadStr(payload, C.KeyRemovedTaskCustomList), C.SeparatorPipe[0], C.MaxNamesInOutcomeSummary);
            if (!string.IsNullOrEmpty(removedTasks))
            {
                lines.Add(C.TextRemovedTaskCustomList + C.SeparatorColonSpace + removedTasks);
            }

            AddDateLineIfKnown(lines, C.LabelDate, ReadStr(payload, C.KeyEventDate));
        }

        /// <summary>
        /// Builds explicit output for summer-games finalization events.
        /// </summary>
        private static void BuildSummerGamesPresentation(JSONNode payload, Presentation p, List<string> lines)
        {
            p.Title = C.TextSummerGamesFinalized;

            int selectedSingleId = ReadId(payload, C.KeySelectedSingleId);
            string selectedSingleTitle = ResolveSingleTitleById(selectedSingleId);
            if (selectedSingleTitle != C.LabelUnknown)
            {
                p.WithWhom = selectedSingleTitle;
            }
            else if (selectedSingleId >= C.MinId)
            {
                p.WithWhom = C.TextSingle + selectedSingleId.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                p.WithWhom = C.TextSummerGames;
            }

            AddIntLineIfPresent(lines, C.TextSelectedSingleId, payload, C.KeySelectedSingleId);
            AddIntLineIfPresent(lines, C.TextGenreId, payload, C.KeyGenreId);
            AddIntLineIfPresent(lines, C.TextLyricsId, payload, C.KeyLyricsId);
            AddIntLineIfPresent(lines, C.TextChoreographyId, payload, C.KeyChoreographyId);
            AddLongLineIfPresent(lines, C.TextGenreCost, payload, C.KeyGenreCost);
            AddLongLineIfPresent(lines, C.TextLyricsCost, payload, C.KeyLyricsCost);
            AddLongLineIfPresent(lines, C.TextChoreographyCost, payload, C.KeyChoreographyCost);
            AddLongLineIfPresent(lines, C.TextTotalCost, payload, C.KeyTotalCost);
            AddBoolTransitionLine(lines, C.TextFinalized, payload, C.KeyWasFinalizedBefore, C.KeyIsFinalizedAfter);
            AddLongTransitionLine(lines, C.TextVocalPoints, payload, C.KeyVocalPointsBefore, C.KeyVocalPointsAfter);
            AddSignedLineIfNonZero(lines, C.TextVocalPoints, ReadLong(payload, C.KeyVocalPointsDelta));
            AddLongTransitionLine(lines, C.TextPlayerPoints, payload, C.KeyPlayerPointsBefore, C.KeyPlayerPointsAfter);
            AddSignedLineIfNonZero(lines, C.TextPlayerPoints, ReadLong(payload, C.KeyPlayerPointsDelta));
            AddLongTransitionLine(lines, C.TextDancePoints, payload, C.KeyDancePointsBefore, C.KeyDancePointsAfter);
            AddSignedLineIfNonZero(lines, C.TextDancePoints, ReadLong(payload, C.KeyDancePointsDelta));
            AddIntLineIfPresent(lines, C.TextHappyMemberCount, payload, C.KeyHappyMemberCount);
            AddDateLineIfKnown(lines, C.LabelDate, ReadStr(payload, C.KeyEventDate));
        }

        /// <summary>
        /// Builds explicit output for random events, substories, and tasks.
        /// </summary>
        private void BuildNarrativePresentation(string type, IMDataCoreEvent ev, JSONNode payload, Presentation p, List<string> lines)
        {
            if (type == C.EventRandomEventStarted || type == C.EventRandomEventConcluded)
            {
                p.Title = type == C.EventRandomEventStarted ? C.TextRandomEventStarted : C.TextRandomEventConcluded;
                string randomTitle = NormalizeRawText(ReadStr(payload, C.KeyRandomEventTitle));
                if (randomTitle == C.LabelUnknown)
                {
                    randomTitle = HumanizeUnknown(ReadStr(payload, C.KeyRandomEventId));
                }

                p.WithWhom = randomTitle;
                AddTransitionLine(lines, C.TextEventState, ReadStr(payload, C.KeyRandomEventStateBefore), ReadStr(payload, C.KeyRandomEventStateAfter));
                AddCodeLineIfKnown(lines, C.TextEventState, ReadStr(payload, C.KeyRandomEventState));
                AddRawLineIfKnown(lines, C.LabelChoice, ReadStr(payload, C.KeyReplyText));
                AddRawLineIfKnown(lines, C.LabelResult, ReadStr(payload, C.KeyReplyDescription));

                List<string> effectLines = BuildRandomEventEffectLines(payload);
                for (int effectIndex = C.ZeroIndex; effectIndex < effectLines.Count; effectIndex++)
                {
                    lines.Add(effectLines[effectIndex]);
                }

                if (C.ShowTechnicalEventMetadata)
                {
                    AddRawLineIfKnown(lines, C.LabelEffects, ReadStr(payload, C.KeyReplyEffectSummary));
                }

                string actors = BuildActorSummary(ReadStr(payload, C.KeyActorsSummary));
                if (!string.IsNullOrEmpty(actors))
                {
                    lines.Add(C.TextInvolvedIdols + actors);
                }

                AddSignedLineIfNonZero(lines, C.TextMoneyChange, ReadLong(payload, C.JsonActivityMoneyDelta));
                AddSignedLineIfNonZero(lines, C.TextFansChange, ReadLong(payload, C.JsonActivityFansDelta));
                AddSignedLineIfNonZero(lines, C.TextFameChange, ReadLong(payload, C.KeyFameDelta));
                AddSignedLineIfNonZero(lines, C.TextBuzzChange, ReadLong(payload, C.KeyBuzzDelta));
                return;
            }

            if (type == C.EventSubstoryStarted || type == C.EventSubstoryDelayed || type == C.EventSubstoryCompleted)
            {
                if (type == C.EventSubstoryStarted)
                {
                    p.Title = C.TextSubstoryStarted;
                }
                else if (type == C.EventSubstoryDelayed)
                {
                    p.Title = C.TextSubstoryDelayed;
                }
                else
                {
                    p.Title = C.TextSubstoryCompleted;
                }

                string substoryDisplayName = ResolveSubstoryDisplayName(payload, C.KeySubstoryId, C.KeySubstoryDisplayName);
                string parentDisplayName = ResolveSubstoryDisplayName(payload, C.KeySubstoryParentId, C.KeySubstoryParentDisplayName);
                string involvedIdols = BuildActorSummary(ReadStr(payload, C.KeyActorsSummary));

                CustomDiaryEntry customEntry;
                if (CustomDiaryCatalog.TryFind(ev, payload, out customEntry))
                {
                    ApplyCustomDiaryEntry(customEntry, payload, p, lines, substoryDisplayName, parentDisplayName, involvedIdols);
                    return;
                }

                p.WithWhom = !string.IsNullOrEmpty(involvedIdols)
                    ? involvedIdols
                    : (substoryDisplayName != C.LabelUnknown
                        ? substoryDisplayName
                        : (parentDisplayName != C.LabelUnknown ? parentDisplayName : C.LabelStory));

                if (substoryDisplayName != C.LabelUnknown)
                {
                    lines.Add(C.LabelStory + C.SeparatorColonSpace + substoryDisplayName);
                }

                if (parentDisplayName != C.LabelUnknown && !string.Equals(parentDisplayName, substoryDisplayName, StringComparison.OrdinalIgnoreCase))
                {
                    lines.Add(C.TextParentStory + C.SeparatorColonSpace + parentDisplayName);
                }

                if (!string.IsNullOrEmpty(involvedIdols) && !string.Equals(involvedIdols, p.WithWhom, StringComparison.OrdinalIgnoreCase))
                {
                    lines.Add(C.TextInvolvedIdols + involvedIdols);
                }

                if (C.ShowTechnicalEventMetadata)
                {
                    AddCodeLineIfKnown(lines, C.TextSubstoryType, ReadStr(payload, C.KeySubstoryType));
                    AddCodeLineIfKnown(lines, C.LabelAction, ReadStr(payload, C.KeySubstoryLifecycleAction));
                    AddDateLineIfKnown(lines, C.TextScheduledTime, ReadStr(payload, C.KeyScheduledLaunchTime));
                    AddIntTransitionLine(lines, C.TextQueueCount, payload, C.KeyQueueCountBefore, C.KeyQueueCountAfter);
                    AddIntTransitionLine(lines, C.TextDelayedQueue, payload, C.KeyDelayedQueueCountBefore, C.KeyDelayedQueueCountAfter);
                }

                return;
            }

            p.Title = type == C.EventTaskAdded
                ? C.TextTaskAdded
                : (type == C.EventTaskCompleted
                    ? C.TextTaskCompleted
                    : (type == C.EventTaskFailed ? C.TextTaskFailed : C.TextTaskClosed));
            string taskSummary = BuildTaskSummary(payload);
            string taskTitle = NormalizeRawText(ReadStr(payload, C.KeyTaskTitle));
            string taskName = NormalizeRawText(ReadStr(payload, C.KeyTaskCustom));
            p.WithWhom = taskTitle != C.LabelUnknown
                ? taskTitle
                : (taskName != C.LabelUnknown
                    ? HumanizeUnknown(taskName)
                    : (!string.IsNullOrEmpty(taskSummary) ? taskSummary : HumanizeUnknown(ReadStr(payload, C.KeyTaskType))));
            AddRawLineIfKnown(lines, C.TextTaskDescription, ReadStr(payload, C.KeyTaskDescription));
            if (!string.IsNullOrEmpty(taskSummary))
            {
                lines.Add(C.TextTaskSummary + C.SeparatorColonSpace + taskSummary);
            }

            AddBoolTransitionLine(lines, C.LabelFulfilled, payload, C.KeyFulfilledBefore, C.KeyFulfilledAfter);
            AddBoolTransitionLine(lines, C.LabelActive, payload, C.KeyActiveBefore, C.KeyActiveAfter);
            AddDateLineIfKnown(lines, C.TextAvailableFrom, ReadStr(payload, C.KeyAvailableFrom));
        }

        /// <summary>
        /// Applies JSON-supplied player-facing diary text to one presentation.
        /// </summary>
        private void ApplyCustomDiaryEntry(
            CustomDiaryEntry entry,
            JSONNode payload,
            Presentation p,
            List<string> lines,
            string substoryDisplayName,
            string parentDisplayName,
            string involvedIdols)
        {
            if (entry == null || p == null || lines == null)
            {
                return;
            }

            string title = FormatCustomDiaryTemplate(entry.Title, substoryDisplayName, parentDisplayName, involvedIdols, payload);
            if (title != C.LabelUnknown)
            {
                p.Title = title;
            }

            if (!string.IsNullOrEmpty(entry.SourceModTitle))
            {
                p.ModSource = entry.SourceModTitle;
            }

            string withWhom = FormatCustomDiaryTemplate(entry.WithWhom, substoryDisplayName, parentDisplayName, involvedIdols, payload);
            if (withWhom != C.LabelUnknown)
            {
                p.WithWhom = withWhom;
            }
            else if (!string.IsNullOrEmpty(involvedIdols))
            {
                p.WithWhom = involvedIdols;
            }
            else if (substoryDisplayName != C.LabelUnknown)
            {
                p.WithWhom = substoryDisplayName;
            }

            string description = FormatCustomDiaryTemplate(entry.Description, substoryDisplayName, parentDisplayName, involvedIdols, payload);
            if (description != C.LabelUnknown)
            {
                lines.Add(description);
            }

            for (int i = C.ZeroIndex; i < entry.OutcomeLines.Count; i++)
            {
                string line = FormatCustomDiaryTemplate(entry.OutcomeLines[i], substoryDisplayName, parentDisplayName, involvedIdols, payload);
                if (line != C.LabelUnknown)
                {
                    lines.Add(line);
                }
            }

            if (lines.Count == C.ZeroIndex)
            {
                if (substoryDisplayName != C.LabelUnknown)
                {
                    lines.Add(C.LabelStory + C.SeparatorColonSpace + substoryDisplayName);
                }

                if (C.ShowTechnicalEventMetadata)
                {
                    AddCodeLineIfKnown(lines, C.LabelAction, ReadStr(payload, C.KeySubstoryLifecycleAction));
                }
            }
        }

        /// <summary>
        /// Expands simple runtime tokens for JSON-supplied diary text.
        /// </summary>
        private string FormatCustomDiaryTemplate(
            string template,
            string substoryDisplayName,
            string parentDisplayName,
            string involvedIdols,
            JSONNode payload)
        {
            string value = NormalizeRawText(template);
            if (value == C.LabelUnknown)
            {
                return C.LabelUnknown;
            }

            string focusedName = ResolveIdolName(idol);
            string storyName = substoryDisplayName != C.LabelUnknown ? substoryDisplayName : C.LabelStory;
            string parentName = parentDisplayName != C.LabelUnknown ? parentDisplayName : storyName;
            string action = HumanizeUnknown(ReadStr(payload, C.KeySubstoryLifecycleAction));
            string idolNames = !string.IsNullOrEmpty(involvedIdols)
                ? involvedIdols
                : (focusedName != C.LabelUnknown ? focusedName : C.LabelUnknown);

            value = value.Replace(C.CustomTokenIdols, idolNames);
            value = value.Replace(C.CustomTokenIdol, focusedName);
            value = value.Replace(C.CustomTokenFocusedIdol, focusedName);
            value = value.Replace(C.CustomTokenStory, storyName);
            value = value.Replace(C.CustomTokenSubstory, storyName);
            value = value.Replace(C.CustomTokenParentStory, parentName);
            value = value.Replace(C.CustomTokenAction, action);
            value = ReplaceCustomDiaryActorTokens(value, ReadStr(payload, C.KeyActorsSummary));
            return NormalizeRawText(value);
        }

        /// <summary>
        /// Expands actor-tag and positional-idol tokens in JSON-supplied diary text.
        /// </summary>
        private static string ReplaceCustomDiaryActorTokens(string template, string actorsSummary)
        {
            string value = NormalizeRawText(template);
            if (value == C.LabelUnknown)
            {
                return template;
            }

            string normalizedSummary = NormalizeRawText(actorsSummary);
            if (normalizedSummary == C.LabelUnknown)
            {
                return value;
            }

            string[] entries = normalizedSummary.Split(new[] { C.SeparatorPipeCharacter }, StringSplitOptions.RemoveEmptyEntries);
            int idolIndex = C.ZeroIndex;
            for (int entryIndex = C.ZeroIndex; entryIndex < entries.Length; entryIndex++)
            {
                string[] fields = entries[entryIndex].Split(new[] { C.SeparatorColonCharacter }, StringSplitOptions.None);
                if (fields.Length < C.ActorSummaryFieldCount)
                {
                    continue;
                }

                string actorToken = NormalizeCodeToken(fields[C.ActorSummaryTokenField]);
                string actorDisplayName = NormalizeRawText(fields[C.ActorSummaryDisplayNameField]);
                if (string.IsNullOrEmpty(actorToken) || actorDisplayName == C.LabelUnknown)
                {
                    continue;
                }

                value = ReplaceActorTokenFamily(value, actorToken, actorDisplayName);
                value = ReplaceActorTokenFamily(value, "actor:" + actorToken, actorDisplayName);

                if (string.Equals(fields[C.ActorSummaryKindField], C.KindIdol, StringComparison.OrdinalIgnoreCase))
                {
                    idolIndex += C.LastFromCount;
                    string positionalToken = "idol" + idolIndex.ToString(CultureInfo.InvariantCulture);
                    value = ReplaceActorTokenFamily(value, positionalToken, actorDisplayName);
                    value = ReplaceActorTokenFamily(value, "idol_" + idolIndex.ToString(CultureInfo.InvariantCulture), actorDisplayName);
                }
            }

            return value;
        }

        /// <summary>
        /// Replaces one actor token and its common name/possessive variants.
        /// </summary>
        private static string ReplaceActorTokenFamily(string template, string token, string actorDisplayName)
        {
            if (string.IsNullOrEmpty(template) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(actorDisplayName))
            {
                return template;
            }

            string possessiveName = FormatPossessiveName(actorDisplayName);
            string value = template;
            value = value.Replace("{" + token + "}", actorDisplayName);
            value = value.Replace("{" + token + "_name}", actorDisplayName);
            value = value.Replace("{" + token + "_possessive}", possessiveName);
            value = value.Replace("{" + token + "'s}", possessiveName);
            if (token.StartsWith("actor:", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Replace("{" + token + ":name}", actorDisplayName);
                value = value.Replace("{" + token + ":possessive}", possessiveName);
            }

            return value;
        }

        /// <summary>
        /// Formats a display name as an English possessive for custom diary prose.
        /// </summary>
        private static string FormatPossessiveName(string name)
        {
            string normalized = NormalizeRawText(name);
            if (normalized == C.LabelUnknown)
            {
                return C.LabelUnknown;
            }

            return normalized.EndsWith("s", StringComparison.OrdinalIgnoreCase)
                ? normalized + "'"
                : normalized + "'s";
        }

        /// <summary>
        /// Builds player-facing output for idol outfit/body appearance changes captured from external mods.
        /// </summary>
        private void BuildOutfitPresentation(IMDataCoreEvent ev, JSONNode payload, Presentation p, List<string> lines)
        {
            p.Title = C.TextOutfitChanged;

            string actionDisplay = HumanizeUnknown(ReadStr(payload, C.KeyOutfitChangeAction));
            int idolId = ReadId(payload, C.JsonIdolId);
            if (idolId < C.MinId)
            {
                idolId = ResolveIdFromEvent(ev);
            }

            p.WithWhom = actionDisplay != C.LabelUnknown
                ? actionDisplay
                : ResolveIdolNameById(idolId);

            AddCodeLineIfKnown(lines, C.TextOutfitChangeAction, ReadStr(payload, C.KeyOutfitChangeAction));

            if (!C.ShowTechnicalEventMetadata)
            {
                return;
            }

            string previousOutfitId = NormalizeRawText(ReadStr(payload, C.KeyPreviousOutfitAssetId));
            if (previousOutfitId == C.LabelUnknown)
            {
                int previousPartId = ReadInt(payload, C.KeyPreviousOutfitPartId);
                if (previousPartId >= C.MinId)
                {
                    previousOutfitId = previousPartId.ToString(CultureInfo.InvariantCulture);
                }
            }

            string newOutfitId = NormalizeRawText(ReadStr(payload, C.KeyNewOutfitAssetId));
            if (newOutfitId == C.LabelUnknown)
            {
                int newPartId = ReadInt(payload, C.KeyNewOutfitPartId);
                if (newPartId >= C.MinId)
                {
                    newOutfitId = newPartId.ToString(CultureInfo.InvariantCulture);
                }
            }

            if (previousOutfitId != C.LabelUnknown)
            {
                lines.Add(C.TextPreviousOutfitId + C.SeparatorColonSpace + previousOutfitId);
            }

            if (newOutfitId != C.LabelUnknown)
            {
                lines.Add(C.TextNewOutfitId + C.SeparatorColonSpace + newOutfitId);
            }
        }

        /// <summary>
        /// Builds explicit output for research, rival, push, wish, influence, and scandal events.
        /// </summary>
        private void BuildProgressionPresentation(string type, IMDataCoreEvent ev, JSONNode payload, Presentation p, List<string> lines)
        {
            if (type == C.EventResearchParamAssigned || type == C.EventResearchPointsPurchased || type == C.EventResearchParamLevelUp)
            {
                p.Title = type == C.EventResearchParamAssigned
                    ? C.TextResearchAssigned
                    : (type == C.EventResearchPointsPurchased ? C.TextResearchPointsPurchased : C.TextResearchParameterLeveledUp);
                string paramTitle = NormalizeRawText(ReadStr(payload, C.KeyParamTitle));
                p.WithWhom = paramTitle != C.LabelUnknown ? paramTitle : HumanizeUnknown(ReadStr(payload, C.KeyResearchType));
                AddCodeLineIfKnown(lines, C.TextResearchType, ReadStr(payload, C.KeyResearchType));
                AddCodeLineIfKnown(lines, C.TextParameterType, ReadStr(payload, C.KeyParamType));
                AddRawLineIfKnown(lines, C.LabelParameter, ReadStr(payload, C.KeyParamTitle));
                AddLongTransitionLine(lines, C.LabelLevel, payload, C.KeyLevelBefore, C.KeyLevelAfter);
                AddLongTransitionLine(lines, C.TextSavedPoints, payload, C.KeySavedPointsBefore, C.KeySavedPointsAfter);
                AddLongTransitionLine(lines, C.LabelPoints, payload, C.KeyPointsBefore, C.KeyPointsAfter);
                AddSignedLineIfNonZero(lines, C.TextPointsDelta, ReadLong(payload, C.KeyPointsDelta));
                AddDateLineIfKnown(lines, C.LabelDate, ReadStr(payload, C.KeyEventDate));
                return;
            }

            if (type == C.EventRivalTrendsUpdated || type == C.EventRivalMonthlyRecalculated)
            {
                p.Title = type == C.EventRivalTrendsUpdated ? C.TextRivalTrendsUpdated : C.TextRivalMonthlyRecalculated;
                p.WithWhom = C.TextRivalOffice;
                AddLongTransitionLine(lines, C.TextMonthIndex, payload, C.KeyMonthIndexBefore, C.KeyMonthIndexAfter);
                AddLongTransitionLine(lines, C.TextActiveGroups, payload, C.KeyActiveGroupCountBefore, C.KeyActiveGroupCountAfter);
                AddLongTransitionLine(lines, C.TextRisingGroups, payload, C.KeyRisingGroupCountBefore, C.KeyRisingGroupCountAfter);
                AddLongTransitionLine(lines, C.TextDisbandedGroups, payload, C.KeyDeadGroupCountBefore, C.KeyDeadGroupCountAfter);
                AddSignedLineIfNonZero(lines, C.TextTrendUpdateCost, ReadLong(payload, C.KeyTrendUpdateCost));
                string risingGenres = BuildTrendSummaryByDirection(ReadStr(payload, C.KeyTrendsGenreSummary), C.CodeRising);
                string fallingGenres = BuildTrendSummaryByDirection(ReadStr(payload, C.KeyTrendsGenreSummary), C.CodeFalling);
                string risingLyrics = BuildTrendSummaryByDirection(ReadStr(payload, C.KeyTrendsLyricsSummary), C.CodeRising);
                string fallingLyrics = BuildTrendSummaryByDirection(ReadStr(payload, C.KeyTrendsLyricsSummary), C.CodeFalling);
                string risingChoreo = BuildTrendSummaryByDirection(ReadStr(payload, C.KeyTrendsChoreoSummary), C.CodeRising);
                string fallingChoreo = BuildTrendSummaryByDirection(ReadStr(payload, C.KeyTrendsChoreoSummary), C.CodeFalling);
                if (!string.IsNullOrEmpty(risingGenres))
                {
                    lines.Add(C.TextGenreTrendsRising + C.SeparatorColonSpace + risingGenres);
                }

                if (!string.IsNullOrEmpty(fallingGenres))
                {
                    lines.Add(C.TextGenreTrendsFalling + C.SeparatorColonSpace + fallingGenres);
                }

                if (!string.IsNullOrEmpty(risingLyrics))
                {
                    lines.Add(C.TextLyricsTrendsRising + C.SeparatorColonSpace + risingLyrics);
                }

                if (!string.IsNullOrEmpty(fallingLyrics))
                {
                    lines.Add(C.TextLyricsTrendsFalling + C.SeparatorColonSpace + fallingLyrics);
                }

                if (!string.IsNullOrEmpty(risingChoreo))
                {
                    lines.Add(C.TextChoreoTrendsRising + C.SeparatorColonSpace + risingChoreo);
                }

                if (!string.IsNullOrEmpty(fallingChoreo))
                {
                    lines.Add(C.TextChoreoTrendsFalling + C.SeparatorColonSpace + fallingChoreo);
                }

                if (C.ShowTechnicalEventMetadata)
                {
                    AddRawLineIfKnown(lines, C.TextGenreTrends, ReadStr(payload, C.KeyTrendsGenreSummary));
                    AddRawLineIfKnown(lines, C.TextLyricsTrends, ReadStr(payload, C.KeyTrendsLyricsSummary));
                    AddRawLineIfKnown(lines, C.TextChoreoTrends, ReadStr(payload, C.KeyTrendsChoreoSummary));
                }

                return;
            }

            if (type == C.EventPushWindowStarted || type == C.EventPushWindowEnded || type == C.EventPushWindowDayIncrement)
            {
                p.Title = type == C.EventPushWindowStarted
                    ? C.TextPushSlotStarted
                    : (type == C.EventPushWindowEnded ? C.TextPushSlotEnded : C.TextPushSlotProgressed);
                int currentId = ReadId(payload, C.KeyPushCurrentIdolId);
                int previousId = ReadId(payload, C.KeyPushPreviousIdolId);
                int withWhomId = currentId >= C.MinId ? currentId : previousId;
                p.WithWhom = ResolveIdolNameById(withWhomId);
                AddIntLineIfPresent(lines, C.TextSlotIndex, payload, C.KeyPushSlotIndex);
                if (previousId >= C.MinId || currentId >= C.MinId)
                {
                    lines.Add(C.LabelIdolPrefix + ResolveIdolNameById(previousId) + C.SeparatorArrow + ResolveIdolNameById(currentId));
                }

                AddIntLineIfPresent(lines, C.TextDaysInSlot, payload, C.KeyPushDaysInSlot);
                AddCodeLineIfKnown(lines, C.LabelAction, ReadStr(payload, C.KeyPushLifecycleAction));
                return;
            }

            if (type == C.EventWishGenerated || type == C.EventWishFulfilled || type == C.EventWishDone)
            {
                p.Title = type == C.EventWishGenerated
                    ? C.TextWishGenerated
                    : (type == C.EventWishFulfilled ? C.TextWishFulfilled : C.TextWishClosed);
                int idolId = ReadId(payload, C.JsonIdolId);
                p.WithWhom = ResolveIdolNameById(idolId);
                AddTransitionLine(lines, C.TextWishType, ReadStr(payload, C.KeyWishTypeBefore), ReadStr(payload, C.KeyWishTypeAfter));
                AddRawTransitionLine(
                    lines,
                    C.TextWishFormula,
                    BuildWishGoalText(ReadStr(payload, C.KeyWishTypeBefore), ReadStr(payload, C.KeyWishFormulaBefore)),
                    BuildWishGoalText(ReadStr(payload, C.KeyWishTypeAfter), ReadStr(payload, C.KeyWishFormulaAfter)));
                AddBoolTransitionLine(lines, C.LabelFulfilled, payload, C.KeyWishFulfilledBefore, C.KeyWishFulfilledAfter);
                AddSignedLineIfNonZero(lines, C.TextInfluenceDelta, ReadLong(payload, C.KeyInfluencePointsDelta));
                AddSignedLineIfNonZero(lines, C.TextMentalStaminaDelta, ReadLong(payload, C.KeyMentalStaminaDelta));
                AddDateLineIfKnown(lines, C.LabelDate, ReadStr(payload, C.KeyEventDate));
                return;
            }

            if (type == C.EventInfluenceBlackmailQueued || type == C.EventInfluenceBlackmailTriggered)
            {
                p.Title = type == C.EventInfluenceBlackmailQueued ? C.TextBlackmailQueued : C.TextBlackmailTriggered;
                int targetId = ReadId(payload, C.KeyTargetId);
                int spyId = ReadId(payload, C.KeySpyId);
                p.WithWhom = ResolveIdolNameById(targetId);
                if (spyId >= C.MinId)
                {
                    lines.Add(C.TextSpyIdol + ResolveIdolNameById(spyId));
                }

                AddIntLineIfPresent(lines, C.TextDaysUntilReport, payload, C.KeyDaysUntilReport);
                AddIntLineIfPresent(lines, C.TextQueueSize, payload, C.KeyQueueSizeAfter);
                AddIntLineIfPresent(lines, C.TextSuccessTier, payload, C.KeySuccessTier);
                AddIntLineIfPresent(lines, C.TextInfluenceAward, payload, C.KeyInfluenceAward);
                AddDateLineIfKnown(lines, C.TextReportDate, ReadStr(payload, C.KeyReportDate));
                return;
            }

            p.Title = C.TextScandalMitigated;
            int scandalIdolId = ReadId(payload, C.JsonIdolId);
            p.WithWhom = ResolveIdolNameById(scandalIdolId);
            AddLongTransitionLine(lines, C.TextScandalPoints, payload, C.KeyScandalPointsBefore, C.KeyScandalPointsAfter);
            AddSignedLineIfNonZero(lines, C.TextPointsRemoved, ReadLong(payload, C.KeyScandalPointsRemoved));
            AddSignedLineIfNonZero(lines, C.TextGroupPointsRemoved, ReadLong(payload, C.KeyScandalGroupPointsRemoved));
            AddLongLineIfNonZero(lines, C.TextGroupPointsRemaining, ReadLong(payload, C.KeyScandalGroupPointsRemaining));
        }

        /// <summary>
        /// Builds explicit output for audition events.
        /// </summary>
        private static void BuildAuditionPresentation(string type, JSONNode payload, Presentation p, List<string> lines)
        {
            if (type == C.EventAuditionStarted)
            {
                p.Title = C.TextAuditionStarted;
            }
            else if (type == C.EventAuditionCostPaid)
            {
                p.Title = C.TextAuditionCostPaid;
            }
            else if (type == C.EventAuditionCooldownReset)
            {
                p.Title = C.TextAuditionCooldownReset;
            }
            else
            {
                p.Title = C.TextAuditionFailureTriggered;
            }

            p.WithWhom = C.LabelAuditions;
            AddCodeLineIfKnown(lines, C.TextAuditionType, ReadStr(payload, C.KeyAuditionType));
            AddCodeLineIfKnown(lines, C.TextResetType, ReadStr(payload, C.KeyResetType));
            AddLongLineIfNonZero(lines, C.LabelCost, ReadLong(payload, C.KeyCost));
            AddLongLineIfNonZero(lines, C.TextResetCost, ReadLong(payload, C.KeyResetCost));
            AddSignedLineIfNonZero(lines, C.TextMoneyChange, ReadLong(payload, C.JsonActivityMoneyDelta));
            AddIntLineIfPresent(lines, C.TextGeneratedCandidates, payload, C.KeyGeneratedCandidateCount);
            AddDateLineIfKnown(lines, C.LabelDate, ReadStr(payload, C.KeyEventDate));
        }

        /// <summary>
        /// Builds explicit output for award events.
        /// </summary>
        private static void BuildAwardPresentation(string type, JSONNode payload, Presentation p, List<string> lines)
        {
            p.Title = type == C.EventAwardNominated ? C.TextAwardNominated : C.TextAwardResult;
            int idolId = ReadId(payload, C.JsonIdolId);
            p.WithWhom = idolId >= C.MinId ? ResolveIdolNameById(idolId) : C.LabelAgency;
            AddCodeLineIfKnown(lines, C.LabelAward, ReadStr(payload, C.KeyAwardType));
            AddIntLineIfPresent(lines, C.LabelYear, payload, C.KeyAwardYear);
            if (HasPayloadValue(payload, C.KeyAwardIsNomination))
            {
                lines.Add(C.TextIsNomination + YesNo(ReadBool(payload, C.KeyAwardIsNomination)));
            }

            if (HasPayloadValue(payload, C.KeyAwardWon))
            {
                lines.Add(C.TextWon + YesNo(ReadBool(payload, C.KeyAwardWon)));
            }
        }

        /// <summary>
        /// Builds explicit output for agency room lifecycle events.
        /// </summary>
        private static void BuildAgencyRoomPresentation(string type, JSONNode payload, Presentation p, List<string> lines)
        {
            p.Title = type == C.EventAgencyRoomBuilt ? C.TextAgencyRoomBuilt : C.TextAgencyRoomCostPaid;
            p.WithWhom = HumanizeUnknown(ReadStr(payload, C.KeyRoomType));
            AddIntLineIfPresent(lines, C.TextRoomId, payload, C.KeyRoomId);
            AddIntLineIfPresent(lines, C.LabelFloor, payload, C.KeyFloorIndex);
            AddIntLineIfPresent(lines, C.TextRoomSpace, payload, C.KeyRoomSpace);
            AddLongLineIfNonZero(lines, C.TextRoomCost, ReadLong(payload, C.KeyRoomCost));
            AddSignedLineIfNonZero(lines, C.TextMoneyChange, ReadLong(payload, C.JsonActivityMoneyDelta));
            AddDateLineIfKnown(lines, C.LabelDate, ReadStr(payload, C.KeyEventDate));
        }

        /// <summary>
        /// Builds explicit output for idol lifecycle and transfer events.
        /// </summary>
        private void BuildIdolLifecyclePresentation(string type, IMDataCoreEvent ev, JSONNode payload, Presentation p, List<string> lines)
        {
            int payloadIdolId = ReadId(payload, C.JsonIdolId);
            int resolvedIdolId = payloadIdolId >= C.MinId ? payloadIdolId : ResolveIdFromEvent(ev);
            p.WithWhom = ResolveIdolNameById(resolvedIdolId);

            if (type == C.EventIdolHired)
            {
                p.Title = C.TextIdolJoinedAgency;
            }
            else if (type == C.EventIdolGraduationAnnounced)
            {
                p.Title = C.TextGraduationAnnounced;
            }
            else if (type == C.EventIdolGraduated)
            {
                p.Title = C.TextIdolGraduated;
            }
            else
            {
                p.Title = C.TextIdolGroupTransfer;
            }

            if (type == C.EventIdolGroupTransferred)
            {
                AddRawLineIfKnown(lines, C.TextFromGroup, ReadStr(payload, C.KeyFromGroupTitle));
                AddRawLineIfKnown(lines, C.TextToGroup, ReadStr(payload, C.KeyToGroupTitle));
                AddCodeLineIfKnown(lines, C.TextFromGroupStatus, ReadStr(payload, C.KeyFromGroupStatus));
                AddCodeLineIfKnown(lines, C.TextToGroupStatus, ReadStr(payload, C.KeyToGroupStatus));
                AddDateLineIfKnown(lines, C.TextTransferDate, ReadStr(payload, C.KeyTransferDate));
            }
            else
            {
                AddCodeLineIfKnown(lines, C.LabelStatus, ReadStr(payload, C.KindStatus));
                AddCodeLineIfKnown(lines, C.LabelType, ReadStr(payload, C.KeyIdolType));
                AddIntLineIfPositive(lines, C.LabelAge, ReadInt(payload, C.KeyIdolAge));
                AddDateLineIfKnown(lines, C.TextHiringDate, ReadStr(payload, C.KeyIdolHiringDate));
                if (type == C.EventIdolHired)
                {
                    AddGraduationDateLine(lines, ReadStr(payload, C.KeyIdolGraduationDate));
                }
                else
                {
                    AddDateLineIfKnown(lines, C.TextGraduationDate, ReadStr(payload, C.KeyIdolGraduationDate));
                }
                AddRawLineIfKnown(lines, C.LabelTrivia, ReadStr(payload, C.KeyIdolCustomTrivia));
                AddRawLineIfKnown(lines, C.LabelTrivia, ReadStr(payload, C.KeyIdolTrivia));

                if (HasPayloadValue(payload, C.KeyIdolGraduationWithDialogue))
                {
                    lines.Add(C.TextGraduationDialogue + YesNo(ReadBool(payload, C.KeyIdolGraduationWithDialogue)));
                }
            }
        }

        /// <summary>
        /// Builds explicit output for legacy status aliases.
        /// </summary>
        private void BuildLegacyStatusPresentation(string type, IMDataCoreEvent ev, JSONNode payload, Presentation p, List<string> lines)
        {
            p.Title = type == C.EventStatusStarted
                ? C.TextStatusStarted
                : (type == C.EventStatusEnded ? C.TextStatusEnded : C.TextStatusUpdated);
            p.WithWhom = ResolveIdolNameById(ResolveIdFromEvent(ev));
            AddTransitionLine(lines, C.LabelStatus, ReadStr(payload, C.JsonPrevStatus), ReadStr(payload, C.JsonNewStatus));
        }

        /// <summary>
        /// Builds explicit output for economy summary events.
        /// </summary>
        private void BuildEconomyPresentation(string type, IMDataCoreEvent ev, JSONNode payload, Presentation p, List<string> lines)
        {
            p.WithWhom = C.LabelAgency;
            if (type == C.EventEconomyDailyTick)
            {
                p.Title = C.TextDailyAgencyUpdate;
                AddSignedLineIfNonZero(lines, C.TextMoneyChange, ReadLong(payload, C.JsonActivityMoneyDelta));
                AddSignedLineIfNonZero(lines, C.TextFansChange, ReadLong(payload, C.JsonActivityFansDelta));
                AddSignedLineIfNonZero(lines, C.TextFameChange, ReadLong(payload, C.KeyFameDelta));
                AddSignedLineIfNonZero(lines, C.TextBuzzChange, ReadLong(payload, C.KeyBuzzDelta));
                AddSignedLineIfNonZero(lines, C.TextExpectedDailyProfit, ReadLong(payload, C.KeyExpectedDailyProfit));
                AddSignedLineIfNonZero(lines, C.TextExpectedDailyFame, ReadLong(payload, C.KeyExpectedDailyFameGain));
                AddSignedLineIfNonZero(lines, C.TextExpectedDailyBuzz, ReadLong(payload, C.KeyExpectedDailyBuzzGain));
            }
            else
            {
                p.Title = C.TextWeeklyExpensesApplied;
                AddLongLineIfNonZero(lines, C.TextWeeklyExpense, ReadLong(payload, C.KeyWeeklyExpense));
                AddSignedLineIfNonZero(lines, C.TextMoneyChange, ReadLong(payload, C.JsonActivityMoneyDelta));
                AddLongLineIfNonZero(lines, C.TextTotalFans, ReadLong(payload, C.KeyFansTotal));
                AddLongLineIfNonZero(lines, C.TextFamePoints, ReadLong(payload, C.KeyFamePoints));
                AddLongLineIfNonZero(lines, C.TextBuzzPoints, ReadLong(payload, C.KeyBuzzPoints));
            }
        }

        /// <summary>
        /// Builds explicit output for policy selections.
        /// </summary>
        private static void BuildPolicyPresentation(JSONNode payload, Presentation p, List<string> lines)
        {
            p.Title = C.TextPolicyDecisionSelected;
            p.WithWhom = C.TextAgencyPolicy;
            AddCodeLineIfKnown(lines, C.LabelPolicy, ReadStr(payload, C.KeyPolicyType));
            AddTransitionLine(lines, C.LabelSelection, ReadStr(payload, C.KeyPreviousValue), ReadStr(payload, C.KeyNewValue));
            AddLongLineIfNonZero(lines, C.TextPolicyCost, ReadLong(payload, C.KeyPolicyCost));
            if (HasPayloadValue(payload, C.KeyFreeSelection))
            {
                lines.Add(C.TextFreeSelection + YesNo(ReadBool(payload, C.KeyFreeSelection)));
            }

            AddDateLineIfKnown(lines, C.TextDecisionDate, ReadStr(payload, C.KeyDecisionDate));
        }

        /// <summary>
        /// Builds explicit output for producer relationship/date events.
        /// </summary>
        private void BuildPlayerPresentation(string type, IMDataCoreEvent ev, JSONNode payload, Presentation p, List<string> lines)
        {
            int idolId = ReadId(payload, C.JsonIdolId);
            if (idolId < C.MinId)
            {
                idolId = ResolveIdFromEvent(ev);
            }

            p.WithWhom = ResolveIdolNameById(idolId);
            if (type == C.EventPlayerRelationshipChanged)
            {
                p.Title = C.TextProducerRelationshipUpdated;
                AddCodeLineIfKnown(lines, C.TextRelationshipType, ReadStr(payload, C.KeyPlayerRelationshipType));
                AddLongTransitionLine(lines, C.TextRelationshipPoints, payload, C.KeyPlayerPointsBefore, C.KeyPlayerPointsAfter);
                AddSignedLineIfNonZero(lines, C.TextAppliedChange, ReadLong(payload, C.KeyPlayerPointsAppliedDelta));
                AddLongTransitionLine(lines, C.TextInfluenceLevel, payload, C.KeyPlayerLevelBefore, C.KeyPlayerLevelAfter);
            }
            else if (type == C.EventPlayerDateInteraction)
            {
                p.Title = C.TextProducerDateInteraction;
                AddCodeLineIfKnown(lines, C.LabelInteraction, ReadStr(payload, C.KeyDateInteractionType));
                AddTransitionLine(lines, C.LabelRoute, ReadStr(payload, C.KeyDateRouteBefore), ReadStr(payload, C.KeyDateRouteAfter));
                AddTransitionLine(lines, C.LabelStage, ReadStr(payload, C.KeyDateStageBefore), ReadStr(payload, C.KeyDateStageAfter));
                AddTransitionLine(lines, C.TextDateStatus, ReadStr(payload, C.KeyDateStatusBefore), ReadStr(payload, C.KeyDateStatusAfter));
                AddCodeLineIfKnown(lines, C.TextDateOutcome, ResolveDateOutcomeCode(payload));
                AddBoolTransitionLine(lines, C.LabelCaught, payload, C.KeyDateCaughtBefore, C.KeyDateCaughtAfter);
                AddLongTransitionLine(lines, C.TextRelationshipLevel, payload, C.KeyDateRelationshipLevelBefore, C.KeyDateRelationshipLevelAfter);
            }
            else
            {
                p.Title = C.TextMarriageOutcome;
                AddCodeLineIfKnown(lines, C.LabelRoute, ReadStr(payload, C.KeyMarriageRoute));
                AddCodeLineIfKnown(lines, C.LabelStage, ReadStr(payload, C.KeyMarriageStage));
                AddCodeLineIfKnown(lines, C.TextPartnerStatus, ReadStr(payload, C.KeyMarriagePartnerStatus));
                if (HasPayloadValue(payload, C.KeyMarriageGoodOutcome))
                {
                    lines.Add(C.TextGoodOutcome + YesNo(ReadBool(payload, C.KeyMarriageGoodOutcome)));
                }

                AddRawLineIfKnown(lines, C.LabelKids, ReadStr(payload, C.KeyMarriageKidsString));
                AddRawLineIfKnown(lines, C.TextCareerOutcomeOne, ReadStr(payload, C.KeyMarriageCareerStringOne));
                AddRawLineIfKnown(lines, C.TextCareerOutcomeTwo, ReadStr(payload, C.KeyMarriageCareerStringTwo));
                AddRawLineIfKnown(lines, C.TextRelationshipOutcome, ReadStr(payload, C.KeyMarriageRelationshipOutcomeString));
            }
        }

        /// <summary>
        /// Builds explicit output for clique events.
        /// </summary>
        private void BuildCliquePresentation(string type, IMDataCoreEvent ev, JSONNode payload, Presentation p, List<string> lines)
        {
            p.Title = type == C.EventCliqueJoined ? C.TextJoinedClique : C.TextLeftClique;
            int idolId = ReadId(payload, C.JsonIdolId);
            if (idolId < C.MinId)
            {
                idolId = ResolveIdFromEvent(ev);
            }

            p.WithWhom = ResolveVisibleSocialParticipantName(ev, payload, idolId);
            if (p.WithWhom == C.LabelUnknown)
            {
                p.WithWhom = C.LabelNotKnownToProducer;
            }

            int leaderId = ReadId(payload, C.KeyCliqueLeaderId);
            string leaderName = ResolveVisibleSocialParticipantName(ev, payload, leaderId);
            if (leaderName != C.LabelUnknown)
            {
                lines.Add(C.TextCliqueLeader + leaderName);
            }

            AddIntLineIfPresent(lines, C.TextCliqueMembers, payload, C.KeyCliqueMemberCount);
            if (HasPayloadValue(payload, C.KeyCliqueQuitWasViolent))
            {
                lines.Add(C.TextViolentExit + YesNo(ReadBool(payload, C.KeyCliqueQuitWasViolent)));
            }
        }

        /// <summary>
        /// Builds explicit output for single lifecycle and cast change events.
        /// </summary>
        private void BuildSingleLifecyclePresentation(string type, IMDataCoreEvent ev, JSONNode payload, Presentation p, List<string> lines)
        {
            p.WithWhom = ResolveSingleTitle(ev, payload);
            if (type == C.EventSingleCreated)
            {
                p.Title = C.TextSingleCreated;
            }
            else if (type == C.EventSingleCancelled)
            {
                p.Title = C.TextSingleCancelled;
            }
            else if (type == C.EventSingleStatusChanged)
            {
                p.Title = C.TextSingleStatusUpdated;
            }
            else if (type == C.EventSingleCastChanged)
            {
                p.Title = C.TextSingleSenbatsuUpdated;
            }
            else
            {
                p.Title = C.TextSingleGroupAssignmentUpdated;
            }

            AddTransitionLine(lines, C.LabelStatus, ReadStr(payload, C.KeySinglePreviousStatus), ReadStr(payload, C.KeySingleNewStatus));
            AddIntLineIfPresent(lines, C.TextCastMembers, payload, C.KeySingleCastCount);
            AddIntTransitionLine(lines, C.TextCastSize, payload, C.KeySingleCastCountBefore, C.KeySingleCastCountAfter);

            string added = BuildIdolNameSummaryFromCsv(ReadStr(payload, C.KeySingleCastIdListAdded), C.MaxNamesInOutcomeSummary);
            if (!string.IsNullOrEmpty(added))
            {
                lines.Add(C.TextAddedMembers + added);
            }

            string removed = BuildIdolNameSummaryFromCsv(ReadStr(payload, C.KeySingleCastIdListRemoved), C.MaxNamesInOutcomeSummary);
            if (!string.IsNullOrEmpty(removed))
            {
                lines.Add(C.TextRemovedMembers + removed);
            }

            int removedId = ReadId(payload, C.KeySingleRemovedIdolId);
            if (removedId >= C.MinId)
            {
                lines.Add(C.TextRemovedIdol + ResolveIdolNameById(removedId));
            }

            AddDateLineIfKnown(lines, C.TextReleaseDate, ReadStr(payload, C.KeySingleReleaseDate));
            AddCodeLineIfKnown(lines, C.LabelDistribution, HasPayloadValue(payload, C.KeySingleIsDigital)
                ? (ReadBool(payload, C.KeySingleIsDigital) ? C.CodeDigital : C.CodePhysical)
                : string.Empty);

            int linkedElectionId;
            if (TryReadIntField(payload, C.KeySingleLinkedElectionId, out linkedElectionId) && linkedElectionId >= C.MinId)
            {
                lines.Add(C.TextLinkedElection + linkedElectionId.ToString(CultureInfo.InvariantCulture));
            }

            if (type == C.EventSingleGroupChanged)
            {
                AddRawLineIfKnown(lines, C.TextFromGroup, ReadStr(payload, C.KeyFromGroupTitle));
                AddRawLineIfKnown(lines, C.TextToGroup, ReadStr(payload, C.KeyToGroupTitle));
                AddDateLineIfKnown(lines, C.TextTransferDate, ReadStr(payload, C.KeyTransferDate));
            }
        }

        /// <summary>
        /// Builds explicit output for show lifecycle/configuration events.
        /// </summary>
        private void BuildShowLifecyclePresentation(string type, IMDataCoreEvent ev, JSONNode payload, Presentation p, List<string> lines)
        {
            p.WithWhom = ResolveShowTitle(ev, payload);
            if (type == C.EventShowCreated)
            {
                p.Title = BuildShowEventTitle(payload, C.TextShowCreated, C.TextShowActionCreated);
            }
            else if (type == C.EventShowReleased)
            {
                p.Title = BuildShowEventTitle(payload, C.TextShowReleased, C.TextShowActionReleased);
            }
            else if (type == C.EventShowCancelled)
            {
                p.Title = BuildShowEventTitle(payload, C.TextShowCancelled, C.TextShowActionCancelled);
            }
            else if (type == C.EventShowCastChanged)
            {
                p.Title = BuildShowEventTitle(payload, C.TextShowCastUpdated, C.TextShowActionCastUpdated);
            }
            else if (type == C.EventShowConfigurationChanged)
            {
                p.Title = BuildShowEventTitle(payload, C.TextShowConfigurationUpdated, C.TextShowActionConfigurationUpdated);
            }
            else if (type == C.EventShowRelaunchStarted)
            {
                p.Title = BuildShowEventTitle(payload, C.TextShowRelaunchStarted, C.TextShowActionRelaunchStarted);
            }
            else
            {
                p.Title = BuildShowEventTitle(payload, C.TextShowRelaunchFinished, C.TextShowActionRelaunchFinished);
            }

            AddTransitionLine(lines, C.LabelStatus, ReadStr(payload, C.JsonShowPrevStatus), ReadStr(payload, C.JsonShowNewStatus));
            AddCodeLineIfKnown(lines, C.LabelMedium, ReadStr(payload, C.KeyShowMediumTitle));
            AddCodeLineIfKnown(lines, C.LabelGenre, ReadStr(payload, C.KeyShowGenreTitle));
            AddCodeLineIfKnown(lines, C.LabelCast, ReadStr(payload, C.JsonShowCastType));
            AddTransitionLine(lines, C.TextCastType, ReadStr(payload, C.KeyShowCastTypeBefore), ReadStr(payload, C.KeyShowCastTypeAfter));
            AddRawTransitionLine(lines, C.TextShowTitle, ReadStr(payload, C.KeyShowTitleBefore), ReadStr(payload, C.KeyShowTitleAfter));
            AddRawTransitionLine(lines, C.LabelMc, ReadStr(payload, C.KeyShowMcTitleBefore), ReadStr(payload, C.KeyShowMcTitleAfter));
            AddIntLineIfPresent(lines, C.TextTotalEpisodesReleased, payload, C.JsonShowEpisodeCount);
            AddIntLineIfPresent(lines, C.TextCastMembers, payload, C.JsonShowCastCount);
            AddIntTransitionLine(lines, C.TextCastSize, payload, C.KeyShowCastCountBefore, C.KeyShowCastCountAfter);

            string added = BuildIdolNameSummaryFromCsv(ReadStr(payload, C.KeyShowCastIdListAdded), C.MaxNamesInOutcomeSummary);
            if (!string.IsNullOrEmpty(added))
            {
                lines.Add(C.TextAddedCast + added);
            }

            string removed = BuildIdolNameSummaryFromCsv(ReadStr(payload, C.KeyShowCastIdListRemoved), C.MaxNamesInOutcomeSummary);
            if (!string.IsNullOrEmpty(removed))
            {
                lines.Add(C.TextRemovedCast + removed);
            }

            int removedId = ReadId(payload, C.KeyShowRemovedIdolId);
            if (removedId >= C.MinId)
            {
                lines.Add(C.TextRemovedIdol + ResolveIdolNameById(removedId));
            }

            AddLongLineIfNonZero(lines, C.TextAverageAudience, ReadLong(payload, C.JsonShowAudience));
            AddLongLineIfNonZero(lines, C.TextAverageRevenue, ReadLong(payload, C.JsonShowRevenue));
            AddIntLineIfPresent(lines, C.TextAverageNewFans, payload, C.JsonShowNewFans);
            AddIntLineIfPresent(lines, C.TextAverageBuzz, payload, C.JsonShowBuzz);
            AddIntLineIfPresent(lines, C.TextRelaunchCount, payload, C.KeyShowRelaunchCount);
            AddDateLineIfKnown(lines, C.TextLaunchDate, ReadStr(payload, C.KeyShowLaunchDate));

            long productionCostBefore;
            long productionCostAfter;
            if (TryReadLongField(payload, C.KeyShowProductionCostBefore, out productionCostBefore) &&
                TryReadLongField(payload, C.KeyShowProductionCostAfter, out productionCostAfter))
            {
                lines.Add(
                    C.TextProductionCostPrefix +
                    productionCostBefore.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture) +
                    C.SeparatorArrow +
                    productionCostAfter.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Builds explicit output for contract offer-stage events.
        /// </summary>
        private void BuildContractOfferPresentation(string type, IMDataCoreEvent ev, JSONNode payload, Presentation p, List<string> lines)
        {
            p.Title = type == C.EventContractWindowOpened
                ? C.TextContractOfferOpened
                : (type == C.EventContractAccepted ? C.TextContractAccepted : C.TextContractCancelled);

            int contractIdolId = ResolveIdFromEvent(ev);
            int payloadIdolId = ReadId(payload, C.JsonIdolId);
            if (payloadIdolId >= C.MinId)
            {
                contractIdolId = payloadIdolId;
            }

            string idolName = ResolveIdolNameById(contractIdolId);
            string productName = NormalizeRawText(ReadStr(payload, C.JsonContractProductName));
            string agencyName = NormalizeRawText(ReadStr(payload, C.JsonContractAgentName));
            p.WithWhom = idolName != C.LabelUnknown
                ? idolName
                : (productName != C.LabelUnknown ? productName : agencyName);

            AddRawLineIfKnown(lines, C.LabelProduct, ReadStr(payload, C.JsonContractProductName));
            AddRawLineIfKnown(lines, C.LabelAgency, ReadStr(payload, C.JsonContractAgentName));
            AddCodeLineIfKnown(lines, C.TextContractType, ReadStr(payload, C.JsonContractType));
            AddCodeLineIfKnown(lines, C.TextContractFocus, ReadStr(payload, C.JsonContractSkill));
            if (HasPayloadValue(payload, C.JsonContractIsGroup))
            {
                lines.Add(BuildContractScopeText(ReadBool(payload, C.JsonContractIsGroup)));
            }

            AddDateLineIfKnown(lines, C.TextStartDate, ReadStr(payload, C.JsonContractStartDate));
            AddDateLineIfKnown(lines, C.TextEndDate, ReadStr(payload, C.JsonContractEndDate));
            AddIntLineIfPositive(lines, C.TextDurationMonths, ReadInt(payload, C.JsonContractDurationMonths));

            if (HasPayloadValue(payload, C.JsonContractIsImmediate))
            {
                lines.Add(C.TextActivation + (ReadBool(payload, C.JsonContractIsImmediate) ? C.LabelImmediate : C.LabelScheduled));
            }

            AddSignedLineIfNonZero(lines, C.TextWeeklyPay, ReadLong(payload, C.JsonContractWeeklyPayment));
            AddSignedLineIfNonZero(lines, C.TextWeeklyFans, ReadLong(payload, C.JsonContractWeeklyFans));
            AddSignedLineIfNonZero(lines, C.TextWeeklyBuzz, ReadLong(payload, C.JsonContractWeeklyBuzz));
            AddSignedLineIfNonZero(lines, C.TextWeeklyFame, ReadLong(payload, C.JsonContractWeeklyFame));
            AddSignedLineIfNonZero(lines, C.TextWeeklyStamina, ReadLong(payload, C.JsonContractWeeklyStamina));

            long liability = ReadLong(payload, C.JsonContractLiability);
            if (liability > C.LongZero)
            {
                lines.Add(C.TextLiability + liability.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Builds explicit output for group-level event types.
        /// </summary>
        private void BuildGroupPresentation(string type, IMDataCoreEvent ev, JSONNode payload, Presentation p, List<string> lines)
        {
            p.WithWhom = ResolveGroupTitle(ev, payload);
            if (type == C.EventGroupCreated)
            {
                p.Title = C.TextGroupCreated;
            }
            else if (type == C.EventGroupDisbanded)
            {
                p.Title = C.TextGroupDisbanded;
            }
            else if (type == C.EventGroupParamPointsChanged)
            {
                p.Title = C.TextGroupParameterPointsUpdated;
            }
            else
            {
                p.Title = C.TextGroupAppealPointsSpent;
            }

            AddCodeLineIfKnown(lines, C.TextGroupStatus, ReadStr(payload, C.KeyGroupStatus));
            AddDateLineIfKnown(lines, C.LabelDate, ReadStr(payload, C.KeyGroupEventDate));
            AddIntLineIfPresent(lines, C.LabelMembers, payload, C.KeyGroupMemberCount);
            AddIntLineIfPresent(lines, C.TextReleasedSingles, payload, C.KeyGroupSingleCount);
            AddIntLineIfPresent(lines, C.TextUnreleasedSingles, payload, C.KeyGroupNonReleasedSingleCount);
            AddCodeLineIfKnown(lines, C.TextTargetGender, ReadStr(payload, C.KeyGroupAppealGender));
            AddCodeLineIfKnown(lines, C.TextTargetIntensity, ReadStr(payload, C.KeyGroupAppealHardcoreness));
            AddCodeLineIfKnown(lines, C.TextTargetAge, ReadStr(payload, C.KeyGroupAppealAge));
            AddCodeLineIfKnown(lines, C.TextSourceParameter, ReadStr(payload, C.KeyGroupSourceParamType));
            AddCodeLineIfKnown(lines, C.TextTargetFanSegment, ReadStr(payload, C.KeyGroupTargetFanType));

            AddIntTransitionLine(lines, C.TextParameterPoints, payload, C.KeyGroupPointsBefore, C.KeyGroupPointsAfter);
            AddSignedLineIfNonZero(lines, C.TextParameterDelta, ReadLong(payload, C.KeyGroupPointsDelta));
            AddIntTransitionLine(lines, C.TextAvailablePoints, payload, C.KeyGroupAvailablePointsBefore, C.KeyGroupAvailablePointsAfter);
            AddIntTransitionLine(lines, C.TextSpentPoints, payload, C.KeyGroupPointsSpentBefore, C.KeyGroupPointsSpentAfter);
            AddIntTransitionLine(lines, C.TextTargetPoints, payload, C.KeyGroupTargetPointsBefore, C.KeyGroupTargetPointsAfter);
            AddIntLineIfPresent(lines, C.TextRequestedPoints, payload, C.KeyGroupPointsRequested);
            AddIntLineIfPresent(lines, C.TextAppliedPoints, payload, C.KeyGroupPointsApplied);
        }

        /// <summary>
        /// Returns true when staff payload indicates an idol-based staff lifecycle row.
        /// </summary>
        private static bool IsIdolStaffLifecycleEvent(JSONNode payload)
        {
            string staffType = ReadStr(payload, C.JsonStaffType);
            return staffType.IndexOf(C.KindIdol, StringComparison.OrdinalIgnoreCase) >= C.ZeroIndex;
        }

        /// <summary>
        /// Returns canonical timeline type for event aliases that should be merged.
        /// </summary>
        private static string CanonicalizeTimelineEventType(string eventType)
        {
            string type = eventType ?? string.Empty;
            if (string.Equals(type, C.EventSingleParticipationRecorded, StringComparison.Ordinal))
            {
                return C.EventSingleReleased;
            }

            if (string.Equals(type, C.EventShowEpisode, StringComparison.Ordinal))
            {
                return C.EventShowEpisodeReleased;
            }

            if (string.Equals(type, C.EventContractCanceled, StringComparison.Ordinal))
            {
                return C.EventContractCancelled;
            }

            if (string.Equals(type, C.EventStatusChangedLegacy, StringComparison.Ordinal))
            {
                return C.EventStatusChanged;
            }

            return type;
        }

        /// <summary>
        /// Builds canonical keys for event rows that supersede legacy aliases.
        /// </summary>
        private static HashSet<string> BuildSupersedingEventKeys(List<IMDataCoreEvent> events)
        {
            HashSet<string> supersedingKeys = new HashSet<string>(StringComparer.Ordinal);
            if (events == null)
            {
                return supersedingKeys;
            }

            for (int i = C.ZeroIndex; i < events.Count; i++)
            {
                IMDataCoreEvent ev = events[i];
                if (ev == null)
                {
                    continue;
                }

                string type = ev.EventType ?? string.Empty;
                string canonicalType = CanonicalizeTimelineEventType(type);
                if (!string.IsNullOrEmpty(canonicalType) && string.Equals(type, canonicalType, StringComparison.Ordinal))
                {
                    supersedingKeys.Add(BuildCanonicalEventKey(ev, canonicalType));
                }
            }

            return supersedingKeys;
        }

        /// <summary>
        /// Returns true when one legacy event row is superseded by a newer canonical alias row.
        /// </summary>
        private static bool IsSupersededDuplicateEvent(IMDataCoreEvent ev, HashSet<string> supersedingEventKeys)
        {
            if (ev == null || supersedingEventKeys == null || supersedingEventKeys.Count == C.ZeroIndex)
            {
                return false;
            }

            string type = ev.EventType ?? string.Empty;
            string canonicalType = CanonicalizeTimelineEventType(type);
            if (string.IsNullOrEmpty(canonicalType) || string.Equals(type, canonicalType, StringComparison.Ordinal))
            {
                return false;
            }

            return supersedingEventKeys.Contains(BuildCanonicalEventKey(ev, canonicalType));
        }

        /// <summary>
        /// Returns true when one repeated single-release snapshot should be collapsed in timeline.
        /// </summary>
        private static bool IsDuplicateSingleReleaseEvent(IMDataCoreEvent ev, HashSet<string> singleReleaseIdentityKeys)
        {
            if (ev == null || singleReleaseIdentityKeys == null)
            {
                return false;
            }

            string canonicalType = CanonicalizeTimelineEventType(ev.EventType ?? string.Empty);
            if (!string.Equals(canonicalType, C.EventSingleReleased, StringComparison.Ordinal) ||
                !string.Equals(ev.EntityKind, C.KindSingle, StringComparison.Ordinal))
            {
                return false;
            }

            string identityKey = BuildSingleReleaseIdentityKey(ev);
            if (singleReleaseIdentityKeys.Contains(identityKey))
            {
                return true;
            }

            singleReleaseIdentityKeys.Add(identityKey);
            return false;
        }

        /// <summary>
        /// Returns true when one repeated show-cancelled row should be collapsed for one lifecycle cycle.
        /// </summary>
        private static bool IsDuplicateShowCancelledEvent(IMDataCoreEvent ev, HashSet<string> showCancelledIdentityKeys)
        {
            if (ev == null || showCancelledIdentityKeys == null)
            {
                return false;
            }

            string canonicalType = CanonicalizeTimelineEventType(ev.EventType ?? string.Empty);
            if (!string.Equals(canonicalType, C.EventShowCancelled, StringComparison.Ordinal) ||
                !string.Equals(ev.EntityKind, C.KindShow, StringComparison.Ordinal))
            {
                return false;
            }

            JSONNode payload = ParsePayload(ev.PayloadJson);
            int relaunchCount = ReadInt(payload, C.KeyShowRelaunchCount);
            string identityKey = BuildShowCancelledIdentityKey(ev, relaunchCount);
            if (showCancelledIdentityKeys.Contains(identityKey))
            {
                return true;
            }

            showCancelledIdentityKeys.Add(identityKey);
            return false;
        }

        /// <summary>
        /// Returns true when idol-dating status row is a technical duplicate of a narrative idol-idol lifecycle event.
        /// </summary>
        private static bool IsSuppressedIdolDatingStatusStub(
            IMDataCoreEvent ev,
            HashSet<string> idolDatingStartMomentKeys,
            HashSet<string> idolDatingEndMomentKeys)
        {
            if (ev == null)
            {
                return false;
            }

            string canonicalType = CanonicalizeTimelineEventType(ev.EventType ?? string.Empty);
            bool isTechnicalStatusEvent =
                string.Equals(canonicalType, C.EventIdolDatingStatusChanged, StringComparison.Ordinal) ||
                string.Equals(canonicalType, C.EventDatingPartnerStatusChanged, StringComparison.Ordinal);
            if (!isTechnicalStatusEvent)
            {
                return false;
            }

            string momentKey = BuildIdolDatingMomentKey(ev);
            if (string.IsNullOrEmpty(momentKey))
            {
                return false;
            }

            JSONNode payload = ParsePayload(ev.PayloadJson);
            string previousStatus = NormalizeLifecycleStatusCode(ReadStr(payload, C.JsonPrevPartnerStatus));
            string newStatus = NormalizeLifecycleStatusCode(ReadStr(payload, C.JsonNewPartnerStatus));
            if (string.IsNullOrEmpty(previousStatus))
            {
                previousStatus = NormalizeLifecycleStatusCode(ReadStr(payload, C.JsonPrevStatus));
            }

            if (string.IsNullOrEmpty(newStatus))
            {
                newStatus = NormalizeLifecycleStatusCode(ReadStr(payload, C.JsonNewStatus));
            }

            if (string.Equals(newStatus, C.KeyTakenIdol, StringComparison.Ordinal))
            {
                return idolDatingStartMomentKeys != null && idolDatingStartMomentKeys.Contains(momentKey);
            }

            if (string.Equals(previousStatus, C.KeyTakenIdol, StringComparison.Ordinal) &&
                string.Equals(newStatus, C.CodeFree, StringComparison.Ordinal))
            {
                return idolDatingEndMomentKeys != null && idolDatingEndMomentKeys.Contains(momentKey);
            }

            return false;
        }

        /// <summary>
        /// Builds idol+moment keys for one idol-idol lifecycle event type.
        /// </summary>
        private static HashSet<string> BuildIdolDatingLifecycleMomentKeys(List<IMDataCoreEvent> events, string lifecycleEventType)
        {
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            if (events == null || string.IsNullOrEmpty(lifecycleEventType))
            {
                return keys;
            }

            for (int i = C.ZeroIndex; i < events.Count; i++)
            {
                IMDataCoreEvent ev = events[i];
                if (ev == null)
                {
                    continue;
                }

                string canonicalType = CanonicalizeTimelineEventType(ev.EventType ?? string.Empty);
                if (!string.Equals(canonicalType, lifecycleEventType, StringComparison.Ordinal))
                {
                    continue;
                }

                string key = BuildIdolDatingMomentKey(ev);
                if (!string.IsNullOrEmpty(key))
                {
                    keys.Add(key);
                }
            }

            return keys;
        }

        /// <summary>
        /// Builds one stable idol+moment key from event metadata.
        /// </summary>
        private static string BuildIdolDatingMomentKey(IMDataCoreEvent ev)
        {
            if (ev == null || ev.IdolId < C.MinId)
            {
                return string.Empty;
            }

            string moment = string.IsNullOrEmpty(ev.GameDateTime)
                ? string.Empty
                : ev.GameDateTime.Trim();
            if (string.Equals(moment, C.LabelUnknown, StringComparison.OrdinalIgnoreCase))
            {
                moment = string.Empty;
            }
            if (string.IsNullOrEmpty(moment) && ev.GameDateKey > C.ZeroIndex)
            {
                moment = ev.GameDateKey.ToString(CultureInfo.InvariantCulture);
            }

            if (string.IsNullOrEmpty(moment))
            {
                return string.Empty;
            }

            return string.Concat(
                ev.IdolId.ToString(CultureInfo.InvariantCulture),
                C.SeparatorPipe,
                moment);
        }

        /// <summary>
        /// Builds one stable key for show-cancelled identity across repeated no-op cancel calls.
        /// </summary>
        private static string BuildShowCancelledIdentityKey(IMDataCoreEvent ev, int relaunchCount)
        {
            if (ev == null)
            {
                return string.Empty;
            }

            return string.Concat(
                C.EventShowCancelled,
                C.SeparatorPipe,
                ev.EntityKind ?? string.Empty,
                C.SeparatorPipe,
                ev.EntityId ?? string.Empty,
                C.SeparatorPipe,
                relaunchCount.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Builds one stable key for single-release timeline identity across chart backfill snapshots.
        /// </summary>
        private static string BuildSingleReleaseIdentityKey(IMDataCoreEvent ev)
        {
            if (ev == null)
            {
                return string.Empty;
            }

            return string.Concat(
                C.EventSingleReleased,
                C.SeparatorPipe,
                ev.IdolId.ToString(CultureInfo.InvariantCulture),
                C.SeparatorPipe,
                ev.EntityKind ?? string.Empty,
                C.SeparatorPipe,
                ev.EntityId ?? string.Empty);
        }

        /// <summary>
        /// Builds one stable dedupe key independent of legacy/new alias type names.
        /// </summary>
        private static string BuildCanonicalEventKey(IMDataCoreEvent ev, string canonicalType)
        {
            if (ev == null)
            {
                return string.Empty;
            }

            return string.Concat(
                canonicalType ?? string.Empty,
                C.SeparatorPipe,
                ev.GameDateKey.ToString(CultureInfo.InvariantCulture),
                C.SeparatorPipe,
                ev.IdolId.ToString(CultureInfo.InvariantCulture),
                C.SeparatorPipe,
                ev.EntityKind ?? string.Empty,
                C.SeparatorPipe,
                ev.EntityId ?? string.Empty);
        }

        /// <summary>
        /// Returns true when event should appear on current idol timeline.
        /// </summary>
        private bool IsRelevantToCurrentIdol(IMDataCoreEvent ev)
        {
            if (ev == null || idol == null || idol.id < C.MinId)
            {
                return false;
            }

            if (ev.IdolId == idol.id)
            {
                return true;
            }

            JSONNode payload = ParsePayload(ev.PayloadJson);
            if (PayloadMentionsIdol(payload, idol.id))
            {
                return true;
            }

            int groupId = ResolveCurrentIdolGroupId();
            if (groupId >= C.MinId && PayloadMentionsGroup(payload, groupId))
            {
                return true;
            }

            string entityKind = ev.EntityKind ?? string.Empty;
            if (string.Equals(entityKind, C.KindActivity, StringComparison.Ordinal))
            {
                return true;
            }

            if (groupId >= C.MinId && string.Equals(entityKind, C.KindGroup, StringComparison.Ordinal))
            {
                int eventGroupId;
                if (TryParseInt(ev.EntityId, out eventGroupId) && eventGroupId == groupId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Resolves current group id for selected idol.
        /// </summary>
        private int ResolveCurrentIdolGroupId()
        {
            if (idol == null)
            {
                return C.InvalidId;
            }

            Groups._group group = idol.GetGroup();
            return group != null ? group.ID : C.InvalidId;
        }

        /// <summary>
        /// Checks payload scalar/list fields for one idol id reference.
        /// </summary>
        private static bool PayloadMentionsIdol(JSONNode payload, int idolId)
        {
            if (payload == null || idolId < C.MinId)
            {
                return false;
            }

            for (int i = C.ZeroIndex; i < C.RelatedIdFields.Length; i++)
            {
                if (ReadId(payload, C.RelatedIdFields[i]) == idolId)
                {
                    return true;
                }
            }

            for (int i = C.ZeroIndex; i < C.RelatedIdListFields.Length; i++)
            {
                List<int> ids = ParseIdList(ReadStr(payload, C.RelatedIdListFields[i]));
                for (int idIndex = C.ZeroIndex; idIndex < ids.Count; idIndex++)
                {
                    if (ids[idIndex] == idolId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks payload for group-level relevance.
        /// </summary>
        private static bool PayloadMentionsGroup(JSONNode payload, int groupId)
        {
            return payload != null && groupId >= C.MinId && ReadId(payload, C.JsonGroupId) == groupId;
        }

        /// <summary>
        /// Renders full diary content.
        /// </summary>
        private void RenderDiary()
        {
            if (diaryContentRoot == null)
            {
                return;
            }

            ClearChildren(diaryContentRoot);
            List<IMDataCoreEvent> visibleEvents = BuildVisibleTimelineEvents();

            AddTitle(C.TitleDiary);
            AddTitle(C.TitleOverview);
            AddText(C.LabelIdolPrefix + ResolveIdolName(idol));
            AddText(C.LabelEventCountPrefix + cachedEvents.Count.ToString(CultureInfo.InvariantCulture));
            AddText(
                C.LabelEventsShownPrefix +
                visibleEvents.Count.ToString(CultureInfo.InvariantCulture) +
                C.SeparatorSpaceSlashSpace +
                cachedEvents.Count.ToString(CultureInfo.InvariantCulture));

            if (!string.IsNullOrEmpty(loadWarning))
            {
                AddText(C.LabelDataWarningPrefix + loadWarning);
            }

            if (!string.IsNullOrEmpty(interactionMessage))
            {
                AddText(C.LabelActionPrefix + interactionMessage);
            }

            if (visibleEvents.Count > maxEventsRenderCurrent)
            {
                AddText(
                    C.TextTimelineLimitedToLatestPrefix +
                    maxEventsRenderCurrent.ToString(CultureInfo.InvariantCulture) +
                    C.TextVisibleEventsForUiPerformanceSuffix);
            }

            AddDivider();
            AddTitle(C.TitleTimeline);
            Transform timelineActionRows = CreateWrappedTimelineButtonContainer(
                diaryContentRoot,
                C.ActionRowName + C.UiSuffixTimeline);
            int timelineButtonIndex = C.ZeroIndex;

            CreateWrappedTimelineButton(
                timelineActionRows,
                C.UiNameCareerDiaryTimelineAction,
                ref timelineButtonIndex,
                C.TimelineToolbarActionsPerRow,
                C.UiNameCareerDiaryActionButtonRefreshTimeline,
                C.LabelRefresh,
                delegate
                {
                    ReloadAndRender();
                },
                false,
                C.TimelineActionToolbarButtonMinimumWidth,
                C.TimelineActionToolbarButtonMaximumWidth);

            CreateWrappedTimelineButton(
                timelineActionRows,
                C.UiNameCareerDiaryTimelineAction,
                ref timelineButtonIndex,
                C.TimelineToolbarActionsPerRow,
                C.UiNameCareerDiaryActionButtonShowMoreTimeline,
                C.LabelShowMore,
                delegate
                {
                    maxEventsRenderCurrent += C.EventsRenderStep;
                    RenderDiary();
                },
                false,
                C.TimelineActionToolbarButtonMinimumWidth,
                C.TimelineActionToolbarButtonMaximumWidth);

            Button clearFiltersButton = CreateWrappedTimelineButton(
                timelineActionRows,
                C.UiNameCareerDiaryTimelineAction,
                ref timelineButtonIndex,
                C.TimelineToolbarActionsPerRow,
                C.UiNameCareerDiaryActionButtonClearTimelineFilters,
                C.LabelClearFilters,
                delegate
                {
                    if (selectedFilterEventTypes.Count == C.ZeroIndex)
                    {
                        return;
                    }

                    selectedFilterEventTypes.Clear();
                    RenderDiary();
                },
                false,
                C.TimelineActionToolbarButtonMinimumWidth,
                C.TimelineActionToolbarButtonMaximumWidth);
            if (clearFiltersButton != null)
            {
                bool hasActiveFilters = selectedFilterEventTypes.Count > C.ZeroIndex;
                clearFiltersButton.interactable = hasActiveFilters;
                SetButtonSelected(clearFiltersButton, hasActiveFilters);
            }

            Button filterModeButton = CreateWrappedTimelineButton(
                timelineActionRows,
                C.UiNameCareerDiaryTimelineAction,
                ref timelineButtonIndex,
                C.TimelineToolbarActionsPerRow,
                "CareerDiary_ActionButton_ToggleFilterMode",
                filterModeIsExclude ? C.TextFilterModeExclude : C.TextFilterModeInclude,
                delegate
                {
                    filterModeIsExclude = !filterModeIsExclude;
                    RenderDiary();
                },
                false,
                C.TimelineActionToolbarButtonMinimumWidth,
                C.TimelineActionToolbarButtonMaximumWidth);
            if (filterModeButton != null)
            {
                SetButtonSelected(filterModeButton, !filterModeIsExclude);
            }

            RenderTimelineFilterControls();

            AddText(C.LabelSelectEvent);
            RenderTimeline(visibleEvents);

            RebuildLayout(diaryContentRoot);
        }

        /// <summary>
        /// Creates one wrapped container used by timeline top controls.
        /// </summary>
        private static Transform CreateWrappedTimelineButtonContainer(Transform parent, string name)
        {
            GameObject container = CreateUiObject(name, parent);
            VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = C.ActionSpacing;
            layout.padding = new RectOffset(C.MinId, C.MinId, C.ActionRowTopPadding, C.ActionRowBottomPadding);

            ContentSizeFitter fitter = container.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return container.transform;
        }

        /// <summary>
        /// Adds one timeline control button into wrapped rows.
        /// </summary>
        private Button CreateWrappedTimelineButton(
            Transform rowsContainer,
            string rowPrefix,
            ref int buttonIndex,
            int buttonsPerRow,
            string buttonName,
            string label,
            UnityAction onClick,
            bool preferFrameworkCreatedButton = false,
            float minButtonWidth = C.TimelineToolbarButtonMinimumWidth,
            float maxButtonWidth = C.TimelineToolbarButtonMaximumWidth)
        {
            if (rowsContainer == null)
            {
                return null;
            }

            int normalizedButtonsPerRow = Mathf.Max(C.LastFromCount, buttonsPerRow);
            int rowIndex = buttonIndex / normalizedButtonsPerRow;
            string rowName = rowPrefix + C.UiRowSeparator + rowIndex.ToString(CultureInfo.InvariantCulture);

            Transform row = rowsContainer.Find(rowName);
            if (row == null)
            {
                GameObject rowObject = CreateUiObject(rowName, rowsContainer);
                HorizontalLayoutGroup rowLayout = rowObject.AddComponent<HorizontalLayoutGroup>();
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = false;
                rowLayout.spacing = C.ActionSpacing;

                LayoutElement rowElement = rowObject.AddComponent<LayoutElement>();
                rowElement.minHeight = C.ActionRowMinimumHeight;
                row = rowObject.transform;
            }

            Button button = preferFrameworkCreatedButton
                ? CreateFrameworkStyledButton(row, buttonName, label, onClick)
                : CreateStyledButton(row, buttonName, label, onClick);
            ConfigureTimelineToolbarButtonVisuals(button, label, minButtonWidth, maxButtonWidth);
            buttonIndex++;
            return button;
        }

        /// <summary>
        /// Normalizes top timeline toolbar button widths so labels are readable and rows stay on-screen.
        /// </summary>
        private static void ConfigureTimelineToolbarButtonVisuals(
            Button button,
            string label,
            float minButtonWidth,
            float maxButtonWidth)
        {
            if (button == null)
            {
                return;
            }

            float normalizedMinWidth = Mathf.Max(C.FloatZero, minButtonWidth);
            float normalizedMaxWidth = Mathf.Max(normalizedMinWidth, maxButtonWidth);

            LayoutElement layout = button.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = button.gameObject.AddComponent<LayoutElement>();
            }

            float existingWidth = layout.preferredWidth > C.FloatZero ? layout.preferredWidth : C.FloatZero;
            float measuredFromButton = MeasurePreferredButtonWidth(button.gameObject, normalizedMinWidth, normalizedMaxWidth);
            float measuredFromLabel = MeasurePreferredButtonWidthFromLabel(label, normalizedMinWidth, normalizedMaxWidth);
            float preferredWidth = Mathf.Max(existingWidth, Mathf.Max(measuredFromButton, measuredFromLabel));
            if (preferredWidth <= C.FloatZero)
            {
                preferredWidth = normalizedMinWidth;
            }

            layout.preferredWidth = Mathf.Clamp(
                preferredWidth,
                normalizedMinWidth,
                normalizedMaxWidth);
            layout.ignoreLayout = false;
            layout.minWidth = C.FloatZero;
            layout.flexibleWidth = C.FloatZero;
            layout.preferredHeight = C.ActionButtonHeight;
            layout.minHeight = C.ActionButtonHeight;
            layout.flexibleHeight = C.FloatZero;

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(layout.preferredWidth, C.ActionButtonHeight);
            }

            TextMeshProUGUI[] tmps = button.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = C.ZeroIndex; i < tmps.Length; i++)
            {
                TextMeshProUGUI tmp = tmps[i];
                if (tmp == null)
                {
                    continue;
                }

                tmp.enableAutoSizing = false;
                tmp.fontSize = C.TimelineToolbarButtonFontSize;
                tmp.enableWordWrapping = false;
                tmp.alignment = TextAlignmentOptions.Center;
            }

            Text[] texts = button.GetComponentsInChildren<Text>(true);
            for (int i = C.ZeroIndex; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null)
                {
                    continue;
                }

                text.resizeTextForBestFit = false;
                text.fontSize = C.TimelineToolbarButtonFontSize;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.alignment = TextAnchor.MiddleCenter;
            }
        }

        /// <summary>
        /// Renders timeline row buttons.
        /// </summary>
        private void RenderTimeline(List<IMDataCoreEvent> timelineEvents)
        {
            if (timelineEvents == null || timelineEvents.Count == C.ZeroIndex)
            {
                AddText(selectedFilterEventTypes.Count > C.ZeroIndex ? C.LabelNoEventsAfterFilters : C.LabelNoEvents);
                return;
            }

            GameObject listObject = CreateUiObject(C.TimelineListName, diaryContentRoot);
            VerticalLayoutGroup vlg = listObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = C.TimelineSpacing;

            ContentSizeFitter fitter = listObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            int count = Mathf.Min(timelineEvents.Count, maxEventsRenderCurrent);
            for (int i = C.ZeroIndex; i < count; i++)
            {
                IMDataCoreEvent ev = timelineEvents[i];
                Presentation p = BuildPresentation(ev);
                p.Date = ResolveTimelineRowDate(ev);
                string rowLabel = BuildRowText(p);
                int indexCapture = i;

                Button row = CreateStyledButton(listObject.transform, C.UiNameCareerDiaryTimelineItemPrefix + i.ToString(CultureInfo.InvariantCulture), rowLabel, delegate
                {
                    SelectEvent(timelineEvents[indexCapture]);
                });

                LayoutElement le = row != null ? row.GetComponent<LayoutElement>() : null;
                if (row != null && le == null)
                {
                    le = row.gameObject.AddComponent<LayoutElement>();
                }

                if (le != null)
                {
                    le.preferredHeight = C.TimelineRowHeight;
                }

                if (p.StaffCredit != null && p.StaffCredit.HasIdentity)
                {
                    AddTimelineStaffPortrait(row.transform, p.StaffCredit, i);
                }

                bool selected = ev != null && ev.EventId == selectedEventId;
                SetButtonSelected(row, selected);
            }
        }

        /// <summary>
        /// Returns timeline-related idol ids visible under current social-visibility rules.
        /// </summary>
        private List<int> GetVisibleTimelineRelatedIdols(IMDataCoreEvent ev, JSONNode payload, List<int> sourceIds)
        {
            List<int> visible = new List<int>();
            if (sourceIds == null)
            {
                return visible;
            }

            for (int i = C.ZeroIndex; i < sourceIds.Count; i++)
            {
                int relatedId = sourceIds[i];
                if (!ShouldRevealSocialParticipant(ev, payload, relatedId))
                {
                    continue;
                }

                if (visible.Contains(relatedId))
                {
                    continue;
                }

                visible.Add(relatedId);
            }

            return visible;
        }

        /// <summary>
        /// Creates one wrapped row for timeline participant cards.
        /// </summary>
        private static Transform CreateTimelineParticipantRow(Transform parent, int rowIndex, int sectionRowIndex)
        {
            GameObject row = CreateUiObject(
                C.UiNameCareerDiaryTimelineParticipantRowPrefix +
                rowIndex.ToString(CultureInfo.InvariantCulture) +
                C.SeparatorUnderscore +
                sectionRowIndex.ToString(CultureInfo.InvariantCulture),
                parent);
            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.spacing = C.TimelineParticipantRowSpacing;
            rowLayout.childAlignment = TextAnchor.UpperLeft;
            return row.transform;
        }

        /// <summary>
        /// Creates one compact timeline participant card with portrait and name.
        /// </summary>
        private void AddTimelineParticipantCard(Transform parent, data_girls.girls girl, int rowIndex, int cardIndex)
        {
            if (parent == null || girl == null)
            {
                return;
            }

            string cardName =
                C.UiNameCareerDiaryTimelineParticipantCardPrefix +
                rowIndex.ToString(CultureInfo.InvariantCulture) +
                C.SeparatorUnderscore +
                cardIndex.ToString(CultureInfo.InvariantCulture) +
                C.SeparatorUnderscore +
                girl.id.ToString(CultureInfo.InvariantCulture);

            GameObject card = CreateUiObject(cardName, parent);
            VerticalLayoutGroup cardLayout = card.AddComponent<VerticalLayoutGroup>();
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = false;
            cardLayout.childForceExpandHeight = false;
            cardLayout.spacing = C.TimelineParticipantSectionSpacing;
            cardLayout.childAlignment = TextAnchor.UpperCenter;

            LayoutElement cardElement = card.AddComponent<LayoutElement>();
            cardElement.preferredWidth = C.TimelineParticipantCardWidth;
            cardElement.preferredHeight = C.TimelineParticipantCardHeight;
            cardElement.minWidth = C.TimelineParticipantCardWidth;
            cardElement.minHeight = C.TimelineParticipantCardHeight;
            cardElement.flexibleWidth = C.FloatZero;
            cardElement.flexibleHeight = C.FloatZero;

            GameObject portraitObject = CreateUiObject(cardName + C.UiSuffixPortrait, card.transform);
            Image portraitImage = portraitObject.AddComponent<Image>();
            portraitImage.preserveAspect = true;
            LayoutElement portraitElement = portraitObject.AddComponent<LayoutElement>();
            portraitElement.preferredWidth = C.TimelineParticipantPortraitSize;
            portraitElement.preferredHeight = C.TimelineParticipantPortraitSize;
            portraitElement.minWidth = C.TimelineParticipantPortraitSize;
            portraitElement.minHeight = C.TimelineParticipantPortraitSize;
            ApplyRelatedIdolHeadshot(portraitImage, girl);

            GameObject nameObject = AddCardText(card.transform, ResolveIdolName(girl), false);
            if (nameObject != null)
            {
                LayoutElement nameLayout = nameObject.GetComponent<LayoutElement>();
                if (nameLayout == null)
                {
                    nameLayout = nameObject.AddComponent<LayoutElement>();
                }

                nameLayout.preferredHeight = C.TimelineParticipantNameLineHeight;
                nameLayout.flexibleWidth = C.FloatZero;

                TextMeshProUGUI[] tmps = nameObject.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int i = C.ZeroIndex; i < tmps.Length; i++)
                {
                    TextMeshProUGUI tmp = tmps[i];
                    if (tmp == null)
                    {
                        continue;
                    }

                    tmp.fontSize = C.TimelineParticipantNameFontSize;
                    tmp.enableWordWrapping = true;
                    tmp.alignment = TextAlignmentOptions.Center;
                }

                Text[] texts = nameObject.GetComponentsInChildren<Text>(true);
                for (int i = C.ZeroIndex; i < texts.Length; i++)
                {
                    Text text = texts[i];
                    if (text == null)
                    {
                        continue;
                    }

                    text.fontSize = C.TimelineParticipantNameFontSize;
                    text.alignment = TextAnchor.MiddleCenter;
                }
            }

            int idolId = girl.id;
            Button cardButton = card.AddComponent<Button>();
            Image cardBackground = card.AddComponent<Image>();
            cardBackground.color = new Color(C.FloatZero, C.FloatZero, C.FloatZero, C.FloatZero);
            cardButton.targetGraphic = cardBackground;
            cardButton.transition = Selectable.Transition.ColorTint;
            cardButton.onClick.AddListener(delegate
            {
                OpenIdolProfile(idolId);
            });
        }

        /// <summary>
        /// Sorts timeline events by canonical timeline date descending, then by event id descending.
        /// </summary>
        private static void SortEventsForTimeline(List<IMDataCoreEvent> events)
        {
            if (events == null || events.Count <= C.LastFromCount)
            {
                return;
            }

            List<TimelineSortEntry> sortable = new List<TimelineSortEntry>(events.Count);
            for (int i = C.ZeroIndex; i < events.Count; i++)
            {
                IMDataCoreEvent ev = events[i];
                if (ev == null)
                {
                    continue;
                }

                DateTime parsed;
                sortable.Add(new TimelineSortEntry
                {
                    Event = ev,
                    HasDate = TryResolveTimelineDate(ev, out parsed),
                    DateValue = parsed
                });
            }

            sortable.Sort(delegate (TimelineSortEntry left, TimelineSortEntry right)
            {
                if (left == null || left.Event == null)
                {
                    return right == null || right.Event == null ? C.ZeroIndex : C.LastFromCount;
                }

                if (right == null || right.Event == null)
                {
                    return -C.LastFromCount;
                }

                if (left.HasDate != right.HasDate)
                {
                    return left.HasDate ? -C.LastFromCount : C.LastFromCount;
                }

                if (left.HasDate)
                {
                    int byDate = right.DateValue.CompareTo(left.DateValue);
                    if (byDate != C.ZeroIndex)
                    {
                        return byDate;
                    }
                }

                int byDateKey = right.Event.GameDateKey.CompareTo(left.Event.GameDateKey);
                if (byDateKey != C.ZeroIndex)
                {
                    return byDateKey;
                }

                int byEventId = right.Event.EventId.CompareTo(left.Event.EventId);
                if (byEventId != C.ZeroIndex)
                {
                    return byEventId;
                }

                return string.Compare(right.Event.EventType ?? string.Empty, left.Event.EventType ?? string.Empty, StringComparison.Ordinal);
            });

            events.Clear();
            for (int i = C.ZeroIndex; i < sortable.Count; i++)
            {
                if (sortable[i] != null && sortable[i].Event != null)
                {
                    events.Add(sortable[i].Event);
                }
            }
        }

        /// <summary>
        /// Builds explicit output for idol birthday events.
        /// </summary>
        private void BuildIdolBirthdayPresentation(IMDataCoreEvent ev, JSONNode payload, Presentation p, List<string> lines)
        {
            int payloadIdolId = ReadId(payload, C.JsonIdolId);
            int resolvedIdolId = payloadIdolId >= C.MinId ? payloadIdolId : ResolveIdFromEvent(ev);
            p.Title = C.TextIdolBirthday;
            p.WithWhom = ResolveIdolNameById(resolvedIdolId);

            AddIntLineIfPositive(lines, C.LabelAge, ReadInt(payload, C.KeyIdolAge));
            AddDateLineIfKnown(lines, C.LabelDate, ReadStr(payload, C.KeyIdolBirthdayDate));
        }

        /// <summary>
        /// Builds visible timeline list after applying user event-type exclusions.
        /// </summary>
        private List<IMDataCoreEvent> BuildVisibleTimelineEvents()
        {
            List<IMDataCoreEvent> visible = new List<IMDataCoreEvent>();
            for (int i = C.ZeroIndex; i < cachedEvents.Count; i++)
            {
                IMDataCoreEvent ev = cachedEvents[i];
                if (ev == null || IsExcludedByUserFilters(ev))
                {
                    continue;
                }

                visible.Add(ev);
            }

            return visible;
        }

        /// <summary>
        /// Renders one filter row under timeline action controls with toggleable event-type exclusions.
        /// </summary>
        private void RenderTimelineFilterControls()
        {
            List<string> filterTypes = BuildFilterControlEventTypes();
            if (filterTypes.Count == C.ZeroIndex)
            {
                return;
            }

            Transform filterRows = CreateWrappedTimelineButtonContainer(
                diaryContentRoot,
                C.ActionRowName + C.UiSuffixTimelineFilters);
            int filterButtonIndex = C.ZeroIndex;

            for (int i = C.ZeroIndex; i < filterTypes.Count; i++)
            {
                string eventType = filterTypes[i];
                bool isSelected = selectedFilterEventTypes.Contains(eventType);
                string buttonLabel = HumanizeCode(eventType);
                string capturedType = eventType;

                Button filterButton = CreateWrappedTimelineButton(
                    filterRows,
                    C.UiNameCareerDiaryFilterToggle,
                    ref filterButtonIndex,
                    C.TimelineFilterButtonsPerRow,
                    C.UiNameCareerDiaryFilterTogglePrefix + eventType,
                    buttonLabel,
                    delegate
                    {
                        ToggleEventTypeFilter(capturedType);
                    },
                    true,
                    C.TimelineFilterButtonMinimumWidth,
                    C.TimelineFilterButtonMaximumWidth);

                SetButtonSelected(filterButton, isSelected);
            }
        }

        /// <summary>
        /// Returns ranked event types to expose as toggle controls.
        /// </summary>
        private List<string> BuildFilterControlEventTypes()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = C.ZeroIndex; i < cachedEvents.Count; i++)
            {
                IMDataCoreEvent ev = cachedEvents[i];
                if (ev == null || string.IsNullOrEmpty(ev.EventType))
                {
                    continue;
                }

                string canonicalType = CanonicalizeTimelineEventType(ev.EventType);
                if (string.IsNullOrEmpty(canonicalType))
                {
                    continue;
                }

                int count;
                if (!counts.TryGetValue(canonicalType, out count))
                {
                    counts.Add(canonicalType, C.LastFromCount);
                }
                else
                {
                    counts[canonicalType] = count + C.LastFromCount;
                }
            }

            List<KeyValuePair<string, int>> ranked = new List<KeyValuePair<string, int>>(counts);
            ranked.Sort(delegate (KeyValuePair<string, int> a, KeyValuePair<string, int> b)
            {
                int byCount = b.Value.CompareTo(a.Value);
                if (byCount != C.ZeroIndex)
                {
                    return byCount;
                }

                return string.Compare(a.Key, b.Key, StringComparison.Ordinal);
            });

            List<string> result = new List<string>(ranked.Count + selectedFilterEventTypes.Count);
            for (int i = C.ZeroIndex; i < ranked.Count; i++)
            {
                result.Add(ranked[i].Key);
            }

            // Keep any manually selected legacy types visible even if current timeline has none.
            if (selectedFilterEventTypes.Count > C.ZeroIndex)
            {
                List<string> extras = new List<string>();
                foreach (string excludedType in selectedFilterEventTypes)
                {
                    string canonicalExcludedType = CanonicalizeTimelineEventType(excludedType);
                    if (string.IsNullOrEmpty(canonicalExcludedType) || counts.ContainsKey(canonicalExcludedType))
                    {
                        continue;
                    }

                    if (!extras.Contains(canonicalExcludedType))
                    {
                        extras.Add(canonicalExcludedType);
                    }
                }

                extras.Sort(StringComparer.Ordinal);
                for (int i = C.ZeroIndex; i < extras.Count; i++)
                {
                    result.Add(extras[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns true when event is excluded by user-controlled timeline filters.
        /// </summary>
        private bool IsExcludedByUserFilters(IMDataCoreEvent ev)
        {
            if (ev == null || string.IsNullOrEmpty(ev.EventType))
            {
                return false;
            }

            // When no categories are selected, nothing is filtered.
            if (selectedFilterEventTypes.Count == C.ZeroIndex)
            {
                return false;
            }

            string canonicalType = CanonicalizeTimelineEventType(ev.EventType);
            bool isSelected = selectedFilterEventTypes.Contains(canonicalType);

            if (filterModeIsExclude)
            {
                // Exclude mode: Hide the event if its type is selected
                return isSelected;
            }
            else
            {
                // Include mode: Hide the event if its type is NOT selected
                return !isSelected;
            }
        }

        /// <summary>
        /// Toggles one event type exclusion and rerenders timeline.
        /// </summary>
        private void ToggleEventTypeFilter(string eventType)
        {
            string canonicalType = CanonicalizeTimelineEventType(eventType);
            if (string.IsNullOrEmpty(canonicalType))
            {
                return;
            }

            if (!selectedFilterEventTypes.Add(canonicalType))
            {
                selectedFilterEventTypes.Remove(canonicalType);
            }

            RenderDiary();
        }

        /// <summary>
        /// Handles timeline selection.
        /// </summary>
        private void SelectEvent(IMDataCoreEvent ev)
        {
            if (ev == null)
            {
                return;
            }

            selectedEventId = ev.EventId;
            if (idol != null)
            {
                Runtime.TrySaveSelectedEventId(idol.id, selectedEventId);
            }

            RenderDetailPopup(ev);
            if (diaryDetailPopupObject != null)
            {
                if (diaryPanelObject != null)
                {
                    diaryPanelObject.SetActive(false);
                }

                diaryDetailPopupObject.SetActive(true);
            }
        }

        /// <summary>
        /// Renders one dedicated event detail popup with formatted fields.
        /// </summary>
        private void RenderDetailPopup(IMDataCoreEvent ev)
        {
            if (diaryDetailContentRoot == null)
            {
                return;
            }

            ClearChildren(diaryDetailContentRoot);

            if (ev == null)
            {
                AddTitle(diaryDetailContentRoot, C.TitleDetailsSubtitle);
                AddText(diaryDetailContentRoot, C.LabelSelectEvent);
                return;
            }

            Presentation p = BuildPresentation(ev);
            JSONNode payload = ParsePayload(ev.PayloadJson);
            bool suppressGenericDetailsForEvent = ShouldSuppressGenericEventDetailSection(ev);
            AddTitle(diaryDetailContentRoot, C.TitleDetailsSubtitle);
            AddDivider(diaryDetailContentRoot);

            if (!suppressGenericDetailsForEvent)
            {
                AddText(diaryDetailContentRoot, C.LabelWhenPrefix + p.Date);
                AddText(diaryDetailContentRoot, C.LabelWhatPrefix + p.Title);
                AddText(diaryDetailContentRoot, C.LabelWithWhomPrefix + p.WithWhom);
                AddText(diaryDetailContentRoot, C.LabelSourcePrefix + p.Source);
                if (!string.IsNullOrEmpty(p.ModSource))
                {
                    AddText(diaryDetailContentRoot, C.LabelModSourcePrefix + C.SeparatorSpace + p.ModSource);
                }

                if (C.ShowTechnicalEventMetadata)
                {
                    AddText(diaryDetailContentRoot, C.LabelNamespacePrefix + Normalize(ev.NamespaceId));
                    AddText(diaryDetailContentRoot, C.LabelPatchPrefix + Normalize(ev.SourcePatch));
                }

                if (!string.IsNullOrEmpty(p.Outcome))
                {
                    AddText(diaryDetailContentRoot, C.LabelOutcomePrefix);
                    AddFormattedOutcomeLines(p.Outcome);
                }
            }

            RenderStaffCreditContext(p.StaffCredit);

            RenderSingleSourceDetails(ev);
            RenderDatingPartnerContext(ev, payload);
            RenderShowCastContext(ev, payload);
            RenderSocialRelationshipContext(ev, payload, p);

            GameObject actionRow = CreateUiObject(C.ActionRowName + C.UiSuffixDetail, diaryDetailContentRoot);
            VerticalLayoutGroup actionLayout = actionRow.AddComponent<VerticalLayoutGroup>();
            actionLayout.childControlWidth = true;
            actionLayout.childControlHeight = false;
            actionLayout.childForceExpandWidth = true;
            actionLayout.childForceExpandHeight = false;
            actionLayout.spacing = C.ActionSpacing;
            actionLayout.padding = new RectOffset(C.MinId, C.MinId, C.ActionRowTopPadding, C.ActionRowBottomPadding);

            CreateStyledButton(actionRow.transform, C.UiNameCareerDiaryActionButtonOpenSource, C.LabelOpenSource, delegate
            {
                OpenSource(ev);
            });

            for (int i = C.ZeroIndex; i < p.RelatedIdols.Count; i++)
            {
                int relatedId = p.RelatedIdols[i];
                data_girls.girls relatedGirl = data_girls.GetGirlByID(relatedId);
                if (relatedGirl == null)
                {
                    continue;
                }

                string label = C.OpenIdolPrefix + ResolveIdolName(relatedGirl) + C.OpenIdolSuffix;
                CreateStyledButton(actionRow.transform, C.UiNameCareerDiaryActionButtonIdolPrefix + relatedId.ToString(CultureInfo.InvariantCulture), label, delegate
                {
                    OpenIdolProfile(relatedId);
                });
            }

            GameObject footerRow = CreateUiObject(C.ActionRowName + C.UiSuffixFooter, diaryDetailContentRoot);
            VerticalLayoutGroup footerLayout = footerRow.AddComponent<VerticalLayoutGroup>();
            footerLayout.childControlWidth = true;
            footerLayout.childControlHeight = false;
            footerLayout.childForceExpandWidth = true;
            footerLayout.childForceExpandHeight = false;
            footerLayout.spacing = C.ActionSpacing;
            footerLayout.padding = new RectOffset(C.MinId, C.MinId, C.ActionRowTopPadding, C.ActionRowBottomPadding);

            CreateStyledButton(footerRow.transform, C.UiNameCareerDiaryActionButtonBackToTimeline, C.LabelBackToTimeline, delegate
            {
                if (diaryDetailPopupObject != null)
                {
                    diaryDetailPopupObject.SetActive(false);
                }

                if (diaryPanelObject != null)
                {
                    diaryPanelObject.SetActive(true);
                }
            });

            RebuildLayout(diaryDetailContentRoot);
        }

        /// <summary>
        /// Renders the portable staff credit with a native small portrait when available.
        /// </summary>
        private void RenderStaffCreditContext(StaffCreditInfo credit)
        {
            if (diaryDetailContentRoot == null || credit == null || !credit.HasIdentity)
            {
                return;
            }

            AddDivider(diaryDetailContentRoot);
            AddTitle(diaryDetailContentRoot, C.LabelCreditedStaff);

            GameObject row = CreateUiObject("CareerDiary_StaffCredit", diaryDetailContentRoot);
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = C.ActionSpacing;
            layout.childAlignment = TextAnchor.MiddleLeft;

            GameObject portraitObject = CreateUiObject("CareerDiary_StaffCreditPortrait", row.transform);
            Image portraitImage = portraitObject.AddComponent<Image>();
            portraitImage.color = mainScript.white32;
            portraitImage.preserveAspect = true;
            LayoutElement portraitLayout = portraitObject.AddComponent<LayoutElement>();
            portraitLayout.preferredWidth = C.TimelineParticipantPortraitSize;
            portraitLayout.preferredHeight = C.TimelineParticipantPortraitSize;
            portraitLayout.minWidth = C.TimelineParticipantPortraitSize;
            portraitLayout.minHeight = C.TimelineParticipantPortraitSize;
            ApplyStaffPortrait(portraitImage, credit);

            AddText(row.transform, FormatStaffCredit(credit));
        }

        /// <summary>
        /// Adds one outcome line per value chunk for better readability.
        /// </summary>
        private void AddFormattedOutcomeLines(string outcome)
        {
            if (string.IsNullOrEmpty(outcome))
            {
                AddText(diaryDetailContentRoot, C.LabelUnknown);
                return;
            }

            string[] chunks = outcome.Split(new[] { C.OutcomeLinesJoinSeparator }, StringSplitOptions.None);
            if (chunks.Length == C.LastFromCount && outcome.IndexOf(C.ListJoinSeparator, StringComparison.Ordinal) >= C.ZeroIndex)
            {
                chunks = outcome.Split(new[] { C.ListJoinSeparator }, StringSplitOptions.None);
            }

            for (int i = C.ZeroIndex; i < chunks.Length; i++)
            {
                string chunk = chunks[i];
                if (!string.IsNullOrEmpty(chunk))
                {
                    AddText(diaryDetailContentRoot, C.OutcomeLinePrefix + chunk.Trim());
                }
            }
        }

        /// <summary>
        /// Renders single-release specific source section with metadata, senbatsu, and chart action.
        /// </summary>
        private void RenderSingleSourceDetails(IMDataCoreEvent ev)
        {
            if (!IsSingleReleaseEvent(ev))
            {
                return;
            }

            string sectionTitle =
                string.Equals(ev.EventType, C.EventSingleParticipationRecorded, StringComparison.Ordinal)
                    ? C.TitleSingleParticipationRecordedDetails
                    : C.TitleSingleSourceDetails;

            JSONNode payload = ParsePayload(ev.PayloadJson);
            singles._single single;
            string errorMessage;
            bool hasLiveSingle = TryResolveSingleFromEvent(ev, out single, out errorMessage);

            AddDivider(diaryDetailContentRoot);
            AddTitle(diaryDetailContentRoot, sectionTitle);
            RenderSingleReleaseSnapshotCard(single, payload);

            if (hasLiveSingle)
            {
                RenderSingleSenbatsuGrid(single);
            }

            RenderSingleParticipationSenbatsuStats(hasLiveSingle ? single : null, payload);
            if (!hasLiveSingle)
            {
                AddText(diaryDetailContentRoot, errorMessage);
                return;
            }

            AddText(diaryDetailContentRoot, C.TextOpenFormationViewerForSingleSenbatsu);

            GameObject actionRow = CreateUiObject(C.SingleSourceActionsRowName, diaryDetailContentRoot);
            HorizontalLayoutGroup actionLayout = actionRow.AddComponent<HorizontalLayoutGroup>();
            actionLayout.childControlWidth = false;
            actionLayout.childControlHeight = false;
            actionLayout.childForceExpandWidth = false;
            actionLayout.childForceExpandHeight = false;
            actionLayout.spacing = C.ActionSpacing;
            actionLayout.padding = new RectOffset(C.MinId, C.MinId, C.ActionRowTopPadding, C.ActionRowBottomPadding);
            LayoutElement actionLayoutElement = actionRow.AddComponent<LayoutElement>();
            actionLayoutElement.minHeight = C.ActionRowMinimumHeight;

            CreateCustomActionButton(actionRow.transform, C.UiNameCareerDiaryActionButtonOpenSingleFormation, C.LabelOpenSingleFormation, delegate
            {
                OpenSingleSenbatsuForSingle(single, payload);
            });

            CreateCustomActionButton(actionRow.transform, C.UiNameCareerDiaryActionButtonOpenSingleChart, C.LabelOpenSingleChart, delegate
            {
                OpenSingleChartForSingle(single);
            });
        }

        /// <summary>
        /// Renders dating context cards with partner portrait/name when resolvable.
        /// </summary>
        private void RenderDatingPartnerContext(IMDataCoreEvent ev, JSONNode payload)
        {
            if (ev == null)
            {
                return;
            }

            if (!string.Equals(ev.EventType, C.EventDatingPartnerStatusChanged, StringComparison.Ordinal) &&
                !string.Equals(ev.EventType, C.EventIdolDatingStatusChanged, StringComparison.Ordinal) &&
                !string.Equals(ev.EventType, C.EventIdolDatingStarted, StringComparison.Ordinal) &&
                !string.Equals(ev.EventType, C.EventIdolDatingEnded, StringComparison.Ordinal) &&
                !string.Equals(ev.EventType, C.EventIdolRelationshipStatusChanged, StringComparison.Ordinal))
            {
                return;
            }

            int partnerId = C.InvalidId;
            if (string.Equals(ev.EventType, C.EventIdolDatingStarted, StringComparison.Ordinal) ||
                string.Equals(ev.EventType, C.EventIdolDatingEnded, StringComparison.Ordinal) ||
                string.Equals(ev.EventType, C.EventIdolRelationshipStatusChanged, StringComparison.Ordinal))
            {
                partnerId = ResolveOtherRelationshipIdolId(ev, payload);
            }
            else if (!TryResolveDatingPartnerId(ev, payload, out partnerId))
            {
                return;
            }

            data_girls.girls partnerGirl = data_girls.GetGirlByID(partnerId);
            if (partnerGirl == null || !ShouldRevealSocialParticipant(ev, payload, partnerId))
            {
                return;
            }

            AddDivider(diaryDetailContentRoot);
            AddTitle(diaryDetailContentRoot, C.LabelDatingContextTitle);
            List<int> partnerIds = new List<int> { partnerId };
            AddRelatedIdolCompactGrid(
                diaryDetailContentRoot,
                partnerIds,
                ev,
                payload,
                C.UiNameCareerDiaryRelatedIdolRowPrefix + C.LabelDatingContextTitle);
        }

        /// <summary>
        /// Renders show-cast context cards for show terminal lifecycle and episode timeline entries.
        /// </summary>
        private void RenderShowCastContext(IMDataCoreEvent ev, JSONNode payload)
        {
            if (ev == null)
            {
                return;
            }

            bool isEpisodeEvent = IsShowEpisodeTimelineContextEvent(ev.EventType);
            bool isTerminalShowEvent = IsShowCancelledOrEndedTimelineEvent(ev, payload);
            if (!isEpisodeEvent && !isTerminalShowEvent)
            {
                return;
            }

            List<int> castIds = ResolveShowCastIdsForTimelineEvent(ev, payload, isTerminalShowEvent);
            if (castIds.Count == C.ZeroIndex)
            {
                return;
            }

            AddDivider(diaryDetailContentRoot);
            AddTitle(diaryDetailContentRoot, C.LabelShowCastContextTitle);
            AddText(
                diaryDetailContentRoot,
                isEpisodeEvent
                    ? C.LabelCastThisEpisodePrefix + castIds.Count.ToString(CultureInfo.InvariantCulture)
                    : C.TextShowCastMembersCapturedPrefix + castIds.Count.ToString(CultureInfo.InvariantCulture));

            string castType = HumanizeUnknown(ReadStr(payload, C.JsonShowCastType));
            if (castType == C.LabelUnknown)
            {
                castType = HumanizeUnknown(ReadStr(payload, C.KeyShowCastTypeAfter));
            }

            if (castType != C.LabelUnknown)
            {
                AddText(diaryDetailContentRoot, C.LabelCastTypePrefix + castType);
            }

            AddRelatedIdolCompactGrid(
                diaryDetailContentRoot,
                castIds,
                ev,
                payload,
                C.UiNameCareerDiaryRelatedIdolRowPrefix + C.LabelShowCastContextTitle);
        }

        /// <summary>
        /// Returns true for show timeline entries that represent terminal cancelled/ended states.
        /// </summary>
        private static bool IsShowCancelledOrEndedTimelineEvent(IMDataCoreEvent ev, JSONNode payload)
        {
            if (ev == null)
            {
                return false;
            }

            string canonicalType = CanonicalizeTimelineEventType(ev.EventType ?? string.Empty);
            if (string.Equals(canonicalType, C.EventShowCancelled, StringComparison.Ordinal))
            {
                return true;
            }

            if (!string.Equals(canonicalType, C.EventShowStatusChanged, StringComparison.Ordinal))
            {
                return false;
            }

            string newStatus = NormalizeLifecycleStatusCode(ReadStr(payload, C.JsonShowNewStatus));
            return IsShowTerminalStatusCode(newStatus);
        }

        /// <summary>
        /// Returns true when one normalized show status code is terminal (ended/cancelled).
        /// </summary>
        private static bool IsShowTerminalStatusCode(string normalizedStatus)
        {
            if (string.IsNullOrEmpty(normalizedStatus))
            {
                return false;
            }

            return normalizedStatus.IndexOf("cancel", StringComparison.Ordinal) >= C.ZeroIndex ||
                normalizedStatus.IndexOf("end", StringComparison.Ordinal) >= C.ZeroIndex ||
                normalizedStatus.IndexOf("finish", StringComparison.Ordinal) >= C.ZeroIndex;
        }

        /// <summary>
        /// Renders one social-event related-idol grid for non-dating social entries (clique/bullying/etc.).
        /// </summary>
        private void RenderSocialRelationshipContext(IMDataCoreEvent ev, JSONNode payload, Presentation p)
        {
            if (ev == null || p == null || p.RelatedIdols == null || p.RelatedIdols.Count == C.ZeroIndex)
            {
                return;
            }

            string canonicalType = CanonicalizeTimelineEventType(ev.EventType ?? string.Empty);
            if (!IsSocialRelationshipEventType(canonicalType) || IsDatingContextEventType(canonicalType))
            {
                return;
            }

            List<int> visibleIds = GetVisibleTimelineRelatedIdols(ev, payload, p.RelatedIdols);
            if (visibleIds.Count == C.ZeroIndex)
            {
                return;
            }

            AddDivider(diaryDetailContentRoot);
            AddTitle(diaryDetailContentRoot, C.LabelRelatedIdolsContextTitle);
            AddRelatedIdolCompactGrid(
                diaryDetailContentRoot,
                visibleIds,
                ev,
                payload,
                C.UiNameCareerDiaryRelatedIdolRowPrefix + C.LabelRelatedIdolsContextTitle);
        }

        /// <summary>
        /// Returns true when one social event already has dedicated dating-context rendering.
        /// </summary>
        private static bool IsDatingContextEventType(string canonicalType)
        {
            return string.Equals(canonicalType, C.EventDatingPartnerStatusChanged, StringComparison.Ordinal) ||
                string.Equals(canonicalType, C.EventIdolDatingStatusChanged, StringComparison.Ordinal) ||
                string.Equals(canonicalType, C.EventIdolDatingStarted, StringComparison.Ordinal) ||
                string.Equals(canonicalType, C.EventIdolDatingEnded, StringComparison.Ordinal) ||
                string.Equals(canonicalType, C.EventIdolRelationshipStatusChanged, StringComparison.Ordinal);
        }

        /// <summary>
        /// Renders compact idol cards in one wrapped grid (four cards per row).
        /// </summary>
        private int AddRelatedIdolCompactGrid(
            Transform parent,
            List<int> idolIds,
            IMDataCoreEvent ev,
            JSONNode payload,
            string sectionObjectName)
        {
            if (parent == null || idolIds == null || idolIds.Count == C.ZeroIndex)
            {
                return C.ZeroIndex;
            }

            List<int> visibleIds = GetVisibleTimelineRelatedIdols(ev, payload, idolIds);
            if (visibleIds.Count == C.ZeroIndex)
            {
                return C.ZeroIndex;
            }

            GameObject section = CreateUiObject(sectionObjectName, parent);
            VerticalLayoutGroup sectionLayout = section.AddComponent<VerticalLayoutGroup>();
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = true;
            sectionLayout.childForceExpandWidth = false;
            sectionLayout.childForceExpandHeight = false;
            sectionLayout.spacing = C.TimelineParticipantRowSpacing;
            sectionLayout.padding = new RectOffset(
                C.MinId,
                C.MinId,
                (int)C.TimelineParticipantSectionTopPadding,
                (int)C.TimelineParticipantSectionBottomPadding);

            ContentSizeFitter sectionFitter = section.AddComponent<ContentSizeFitter>();
            sectionFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sectionFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            int cardsPerRow = Mathf.Max(C.LastFromCount, C.TimelineParticipantCardsPerRow);
            Transform currentRow = null;
            int rendered = C.ZeroIndex;
            for (int i = C.ZeroIndex; i < visibleIds.Count; i++)
            {
                data_girls.girls relatedGirl = data_girls.GetGirlByID(visibleIds[i]);
                if (relatedGirl == null)
                {
                    continue;
                }

                if (rendered % cardsPerRow == C.ZeroIndex || currentRow == null)
                {
                    currentRow = CreateTimelineParticipantRow(section.transform, C.ZeroIndex, rendered / cardsPerRow);
                }

                AddTimelineParticipantCard(currentRow, relatedGirl, C.ZeroIndex, rendered);
                rendered += C.LastFromCount;
            }

            return rendered;
        }

        /// <summary>
        /// Resolves show cast ids from payload and optional same-show history/live-state fallbacks.
        /// </summary>
        private List<int> ResolveShowCastIdsForTimelineEvent(IMDataCoreEvent selectedEvent, JSONNode selectedPayload, bool includeHistoryAndLiveState)
        {
            HashSet<int> uniqueCastIds = new HashSet<int>();
            AddShowCastIdsFromPayload(uniqueCastIds, selectedPayload);
            if (includeHistoryAndLiveState)
            {
                AddShowCastIdsFromTimelineHistory(uniqueCastIds, selectedEvent);
                AddShowCastIdsFromLiveShow(uniqueCastIds, selectedEvent);
            }

            List<int> orderedCastIds = new List<int>(uniqueCastIds);
            orderedCastIds.Sort(delegate (int leftId, int rightId)
            {
                string leftName = ResolveIdolNameById(leftId);
                string rightName = ResolveIdolNameById(rightId);
                int byName = string.Compare(leftName, rightName, StringComparison.Ordinal);
                if (byName != C.ZeroIndex)
                {
                    return byName;
                }

                return leftId.CompareTo(rightId);
            });

            return orderedCastIds;
        }

        /// <summary>
        /// Adds cast ids from show payload fields used across lifecycle/status/episode/config events.
        /// </summary>
        private void AddShowCastIdsFromPayload(HashSet<int> destination, JSONNode payload)
        {
            if (destination == null || payload == null)
            {
                return;
            }

            AddShowCastIdsFromCsv(destination, ReadStr(payload, C.JsonShowCastIdList));
            AddShowCastIdsFromCsv(destination, ReadStr(payload, C.KeyShowCastIdListBefore));
            AddShowCastIdsFromCsv(destination, ReadStr(payload, C.KeyShowCastIdListAfter));
            AddShowCastIdsFromCsv(destination, ReadStr(payload, C.KeyShowCastIdListAdded));
            AddShowCastIdsFromCsv(destination, ReadStr(payload, C.KeyShowCastIdListRemoved));
            AddShowCastId(destination, ReadId(payload, C.KeyShowRemovedIdolId));
        }

        /// <summary>
        /// Adds cast ids by scanning cached timeline rows for the same show entity.
        /// </summary>
        private void AddShowCastIdsFromTimelineHistory(HashSet<int> destination, IMDataCoreEvent selectedEvent)
        {
            if (destination == null || selectedEvent == null)
            {
                return;
            }

            string showEntityId = selectedEvent.EntityId ?? string.Empty;
            if (string.IsNullOrEmpty(showEntityId))
            {
                return;
            }

            for (int i = C.ZeroIndex; i < cachedEvents.Count; i++)
            {
                IMDataCoreEvent candidate = cachedEvents[i];
                if (candidate == null ||
                    !string.Equals(candidate.EntityKind, C.KindShow, StringComparison.Ordinal) ||
                    !string.Equals(candidate.EntityId ?? string.Empty, showEntityId, StringComparison.Ordinal))
                {
                    continue;
                }

                AddShowCastIdsFromPayload(destination, ParsePayload(candidate.PayloadJson));
            }
        }

        /// <summary>
        /// Adds live show cast ids when show object is still available.
        /// </summary>
        private static void AddShowCastIdsFromLiveShow(HashSet<int> destination, IMDataCoreEvent selectedEvent)
        {
            if (destination == null || selectedEvent == null)
            {
                return;
            }

            int showId;
            if (!TryParseInt(selectedEvent.EntityId, out showId))
            {
                return;
            }

            Shows._show show = Shows.GetShowByID(showId);
            if (show == null)
            {
                return;
            }

            List<data_girls.girls> liveCast = show.GetCast();
            if (liveCast == null)
            {
                return;
            }

            for (int i = C.ZeroIndex; i < liveCast.Count; i++)
            {
                data_girls.girls castGirl = liveCast[i];
                if (castGirl == null)
                {
                    continue;
                }

                AddShowCastId(destination, castGirl.id);
            }
        }

        /// <summary>
        /// Adds show cast ids from one comma-separated payload list.
        /// </summary>
        private void AddShowCastIdsFromCsv(HashSet<int> destination, string rawList)
        {
            if (destination == null)
            {
                return;
            }

            List<int> ids = ParseIdList(rawList);
            for (int i = C.ZeroIndex; i < ids.Count; i++)
            {
                AddShowCastId(destination, ids[i]);
            }
        }

        /// <summary>
        /// Adds one show cast idol id when valid and resolvable.
        /// </summary>
        private static void AddShowCastId(HashSet<int> destination, int idolId)
        {
            if (destination == null || idolId < C.MinId)
            {
                return;
            }

            if (data_girls.GetGirlByID(idolId) == null)
            {
                return;
            }

            destination.Add(idolId);
        }

        /// <summary>
        /// Adds one idol portrait card with direct profile-open action.
        /// </summary>
        private void AddRelatedIdolProfileCard(Transform parent, data_girls.girls girl)
        {
            if (parent == null || girl == null)
            {
                return;
            }

            GameObject row = CreateUiObject(C.UiNameCareerDiaryRelatedIdolRowPrefix + girl.id.ToString(CultureInfo.InvariantCulture), parent);
            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.spacing = C.RelatedIdolCardSpacing;
            rowLayout.padding = new RectOffset(C.MinId, C.MinId, (int)C.RelatedIdolSectionTopPadding, (int)C.RelatedIdolSectionBottomPadding);
            LayoutElement rowElement = row.AddComponent<LayoutElement>();
            rowElement.minHeight = C.RelatedIdolCardHeight + C.RelatedIdolSectionTopPadding + C.RelatedIdolSectionBottomPadding;
            rowElement.preferredHeight = rowElement.minHeight;

            GameObject card = CreateUiObject(C.UiNameCareerDiaryRelatedIdolCardPrefix + girl.id.ToString(CultureInfo.InvariantCulture), row.transform);
            VerticalLayoutGroup cardLayout = card.AddComponent<VerticalLayoutGroup>();
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = false;
            cardLayout.childForceExpandHeight = false;
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.spacing = C.RelatedIdolCardSpacing;
            LayoutElement cardElement = card.AddComponent<LayoutElement>();
            cardElement.preferredWidth = C.RelatedIdolCardWidth;
            cardElement.preferredHeight = C.RelatedIdolCardHeight;
            cardElement.minHeight = C.RelatedIdolCardHeight;

            GameObject portraitObject = CreateUiObject(C.UiNamePortrait, card.transform);
            Image portraitImage = portraitObject.AddComponent<Image>();
            portraitImage.color = Color.white;
            portraitImage.preserveAspect = true;
            LayoutElement portraitElement = portraitObject.AddComponent<LayoutElement>();
            portraitElement.preferredWidth = C.RelatedIdolPortraitSize;
            portraitElement.preferredHeight = C.RelatedIdolPortraitSize;
            ApplyRelatedIdolHeadshot(portraitImage, girl);

            GameObject nameText = AddCardText(card.transform, ResolveIdolName(girl), true);
            LayoutElement nameLayout = nameText != null ? nameText.GetComponent<LayoutElement>() : null;
            if (nameLayout != null)
            {
                nameLayout.preferredHeight = C.RelatedIdolCardNameLineHeight;
            }

            int idolIdCapture = girl.id;
            Button cardButton = card.AddComponent<Button>();
            Image cardBackground = card.AddComponent<Image>();
            cardBackground.color = new Color(C.FloatZero, C.FloatZero, C.FloatZero, C.FloatZero);
            cardButton.targetGraphic = cardBackground;
            cardButton.transition = Selectable.Transition.ColorTint;
            cardButton.onClick.AddListener(delegate
            {
                OpenIdolProfile(idolIdCapture);
            });
        }

        /// <summary>
        /// Applies headshot-style sprite used by base-game idol cards.
        /// </summary>
        private static void ApplyRelatedIdolHeadshot(Image portraitImage, data_girls.girls girl)
        {
            if (portraitImage == null || girl == null)
            {
                return;
            }

            Sprite headshot = TryResolveRelatedIdolHeadshot(girl);
            if (headshot == null)
            {
                data_girls_textures.AddToQueue(girl, null);
                portraitImage.color = mainScript.transparent32;
                portraitImage.sprite = null;
                return;
            }

            portraitImage.color = mainScript.white32;
            portraitImage.sprite = headshot;
        }

        /// <summary>
        /// Adds a small responsible-staff portrait to a timeline row.  The stored raw staff type
        /// allows Assistant Manager's portrait Harmony patches to supply their mod texture even
        /// after that staffer is no longer in the active staff list.
        /// </summary>
        private static void AddTimelineStaffPortrait(Transform parent, StaffCreditInfo credit, int rowIndex)
        {
            if (parent == null || credit == null || !credit.HasIdentity)
            {
                return;
            }

            GameObject portraitObject = CreateUiObject(
                C.UiNameCareerDiaryTimelineItemPrefix + rowIndex.ToString(CultureInfo.InvariantCulture) + "_StaffPortrait",
                parent);
            Image portraitImage = portraitObject.AddComponent<Image>();
            portraitImage.color = mainScript.white32;
            portraitImage.preserveAspect = true;

            RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
            if (portraitRect != null)
            {
                portraitRect.anchorMin = new Vector2(C.FloatOne, 0.5f);
                portraitRect.anchorMax = new Vector2(C.FloatOne, 0.5f);
                portraitRect.pivot = new Vector2(C.FloatOne, 0.5f);
                portraitRect.anchoredPosition = new Vector2(-4f, 0f);
                portraitRect.sizeDelta = new Vector2(C.TimelineStaffPortraitSize, C.TimelineStaffPortraitSize);
            }

            ApplyStaffPortrait(portraitImage, credit);
        }

        /// <summary>
        /// Uses the game staff portrait loader so vanilla, unique-staff, and Assistant Manager
        /// portraits all follow their native image-selection rules.
        /// </summary>
        private static void ApplyStaffPortrait(Image portraitImage, StaffCreditInfo credit)
        {
            if (portraitImage == null || credit == null)
            {
                return;
            }

            try
            {
                mainScript main = Camera.main != null ? Camera.main.GetComponent<mainScript>() : null;
                staff staffData = main != null && main.Data != null ? main.Data.GetComponent<staff>() : null;
                if (staffData == null)
                {
                    portraitImage.enabled = false;
                    return;
                }

                staff._staff liveStaff = credit.StaffId >= C.MinId ? staff.GetStaffByID(credit.StaffId) : null;
                if (liveStaff != null)
                {
                    staffData.LoadPortrait_Small(liveStaff, portraitImage);
                    return;
                }

                if (credit.TypeRaw < C.ZeroIndex)
                {
                    portraitImage.enabled = false;
                    return;
                }

                staffData.LoadPortrait_Small(
                    (staff._type)credit.TypeRaw,
                    portraitImage,
                    credit.IsPro,
                    (staff._staff._unique_type)credit.UniqueTypeRaw);
            }
            catch
            {
                portraitImage.enabled = false;
            }
        }

        /// <summary>
        /// Resolves one headshot sprite from existing idol texture cache.
        /// </summary>
        private static Sprite TryResolveRelatedIdolHeadshot(data_girls.girls girl)
        {
            if (girl == null || girl.texture == null)
            {
                return null;
            }

            if (girl.texture.middle != null)
            {
                return girl.texture.middle;
            }

            if (girl.texture.small != null)
            {
                return girl.texture.small;
            }

            return null;
        }

        /// <summary>
        /// Determines whether current event should render single-release source details.
        /// </summary>
        private static bool IsSingleReleaseEvent(IMDataCoreEvent ev)
        {
            return ev != null &&
                string.Equals(ev.EntityKind, C.KindSingle, StringComparison.Ordinal) &&
                (string.Equals(ev.EventType, C.EventSingleReleased, StringComparison.Ordinal)
                || string.Equals(ev.EventType, C.EventSingleParticipationRecorded, StringComparison.Ordinal));
        }

        /// <summary>
        /// Returns true when generic Event Detail block should be omitted in favor of dedicated source section.
        /// </summary>
        private static bool ShouldSuppressGenericEventDetailSection(IMDataCoreEvent ev)
        {
            return IsSingleReleaseEvent(ev);
        }

        /// <summary>
        /// Resolves the backing single object from event entity id.
        /// </summary>
        private static bool TryResolveSingleFromEvent(IMDataCoreEvent ev, out singles._single single, out string errorMessage)
        {
            single = null;
            errorMessage = C.LabelSingleNotFound;
            if (ev == null)
            {
                return false;
            }

            int singleId;
            if (!TryParseInt(ev.EntityId, out singleId))
            {
                errorMessage = C.LabelSingleInvalidId;
                return false;
            }

            single = singles.GetSingleByID(singleId);
            if (single == null)
            {
                errorMessage = C.LabelSingleNotFound;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Renders reconstructed single-release snapshot details using standard diary text styling.
        /// </summary>
        private void RenderSingleReleaseSnapshotCard(singles._single single, JSONNode payload)
        {
            Transform card = diaryDetailContentRoot;
            if (card == null)
            {
                return;
            }

            string singleTitle = HumanizeUnknown(ReadStr(payload, C.JsonSingleTitle));
            if (singleTitle == C.LabelUnknown && single != null)
            {
                singleTitle = Normalize(single.title);
            }

            string groupName = C.LabelUnknown;
            if (single != null)
            {
                Groups._group singleGroup = single.GetGroup();
                if (singleGroup != null)
                {
                    groupName = Normalize(singleGroup.Title);
                }
            }

            string releaseDateText = FormatRoundTripDateForUi(ReadStr(payload, C.KeySingleReleaseDate));
            if (releaseDateText == C.LabelUnknown && single != null && single.ReleaseData != null)
            {
                DateTime releaseDate = single.ReleaseData.ReleaseDate;
                if (releaseDate.Year > DateTime.MinValue.Year)
                {
                    releaseDateText = FormatUiDate(releaseDate);
                }
            }

            int chartPositionFallback = single != null && single.ReleaseData != null ? single.ReleaseData.Chart_Position : C.UnknownChartPosition;
            int chartPosition = ReadSingleIntValue(payload, C.JsonChartPosition, chartPositionFallback);
            string chartText = chartPosition > C.UnknownChartPosition
                ? C.SeparatorHash + chartPosition.ToString(CultureInfo.InvariantCulture)
                : (chartPosition == C.UnknownChartPosition ? C.LabelSingleDidNotChart : C.LabelUnknown);

            int qualityFallback = single != null && single.ReleaseData != null ? single.ReleaseData.Quality : C.ZeroIndex;
            int fanSatisfactionFallback = single != null && single.ReleaseData != null ? single.ReleaseData.Fan_Satisfaction : C.ZeroIndex;
            long salesFallback = single != null && single.ReleaseData != null ? single.ReleaseData.Sales : (single != null ? single.GetTotalSales() : C.LongZero);
            int quantityFallback = single != null ? single.qty : C.ZeroIndex;
            long totalSales = ReadSingleLongValue(payload, C.JsonTotalSales, salesFallback);
            int singleQuantity = ReadSingleIntValue(payload, C.JsonSingleQuantity, quantityFallback);
            string salesSummary = C.LabelSingleSalesPrefix + totalSales.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture);
            if (singleQuantity > C.ZeroIndex)
            {
                salesSummary += C.SeparatorSpaceSlashSpace + singleQuantity.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture);
            }

            AddSingleSnapshotTextLine(card, C.TitleSingleReleaseSnapshot, true);
            AddSingleSnapshotTextLine(card, C.LabelSingleTitlePrefix + singleTitle, false);
            AddSingleSnapshotTextLine(card, C.LabelSingleGroupPrefix + groupName, false);
            AddSingleSnapshotTextLine(card, C.LabelSingleReleaseDatePrefix + releaseDateText, false);
            AddSingleSnapshotTextLine(card, salesSummary, false);
            AddSingleSnapshotTextLine(card, C.LabelSingleChartPrefix + chartText, false);
            AddSingleSnapshotTextLine(card, C.LabelSingleQualityPrefix + ReadSingleIntValue(payload, C.JsonQuality, qualityFallback).ToString(CultureInfo.InvariantCulture), false);
            AddSingleSnapshotTextLine(card, C.LabelSingleFanSatisfactionPrefix + ReadSingleIntValue(payload, C.JsonFanSatisfaction, fanSatisfactionFallback).ToString(CultureInfo.InvariantCulture), false);
            AddSingleSnapshotIntLineFromPayload(card, C.LabelSingleFanBuzzPrefix, payload, C.JsonFanBuzz, false);
            AddSingleSnapshotIntLineFromPayload(card, C.LabelSingleNewFansPrefix, payload, C.JsonNewFans, true);
            AddSingleSnapshotIntLineFromPayload(card, C.LabelSingleNewHardcoreFansPrefix, payload, C.JsonNewHardcoreFans, true);
            AddSingleSnapshotIntLineFromPayload(card, C.LabelSingleNewCasualFansPrefix, payload, C.JsonNewCasualFans, true);
            AddSingleSnapshotTextLine(card, C.LabelSingleGenrePrefix + ResolveSingleParamTitle(single != null ? single.genre : null), false);
            AddSingleSnapshotTextLine(card, C.LabelSingleLyricsPrefix + ResolveSingleParamTitle(single != null ? single.lyrics : null), false);
            AddSingleSnapshotTextLine(card, C.LabelSingleChoreographyPrefix + ResolveSingleParamTitle(single != null ? single.choreography : null), false);
            AddSingleSnapshotTextLine(card, C.LabelSingleMarketingPrefix + BuildSingleMarketingSummary(single), false);

            AddSingleSnapshotTextLine(card, C.TitleSingleReleaseEconomics, true);
            AddSingleSnapshotIntLineFromPayload(card, C.LabelSingleQuantityPrefix, payload, C.JsonSingleQuantity, false);
            AddSingleSnapshotLongLineFromPayload(card, C.LabelSingleProductionCostPrefix, payload, C.JsonSingleProductionCost);
            AddSingleSnapshotLongLineFromPayload(card, C.LabelSingleGrossRevenuePrefix, payload, C.JsonSingleGrossRevenue);
            AddSingleSnapshotLongLineFromPayload(card, C.LabelSingleOtherExpensesPrefix, payload, C.JsonSingleOtherExpenses);
            AddSingleSnapshotLongLineFromPayload(card, C.LabelSingleProfitPrefix, payload, C.JsonSingleProfit);
            AddSingleSnapshotIntLineFromPayload(card, C.LabelSingleOneCdCostPrefix, payload, C.JsonSingleOneCdCost, false);
            AddSingleSnapshotIntLineFromPayload(card, C.LabelSingleOneCdRevenuePrefix, payload, C.JsonSingleOneCdRevenue, false);
            AddSingleSnapshotFloatLineFromPayload(card, C.LabelSingleSalesPerFanPrefix, payload, C.JsonSingleSalesPerFan, C.FormatSingleMetricTwoDecimals, string.Empty);
            AddSingleSnapshotIntLineFromPayload(card, C.LabelSingleFamePointsAwardedPrefix, payload, C.JsonSingleFamePointsAwarded, true);
            AddSingleSnapshotBoolLineFromPayload(card, C.LabelSingleGroupHandshakePrefix, payload, C.JsonSingleIsGroupHandshake);
            AddSingleSnapshotBoolLineFromPayload(card, C.LabelSingleIndividualHandshakePrefix, payload, C.JsonSingleIsIndividualHandshake);
            AddSingleSnapshotFloatLineFromPayload(card, C.LabelSingleMarketingResultPrefix, payload, C.JsonSingleMarketingResult, C.FormatSingleMetricTwoDecimals, string.Empty);
            AddSingleSnapshotCodeLineFromPayload(card, C.LabelSingleMarketingResultStatusPrefix, payload, C.JsonSingleMarketingResultStatus);
            AddSingleSnapshotBoolLineFromPayload(card, C.LabelSingleMostPopularGenrePrefix, payload, C.JsonSingleMostPopularGenre);
            AddSingleSnapshotBoolLineFromPayload(card, C.LabelSingleMostPopularLyricsPrefix, payload, C.JsonSingleMostPopularLyrics);
            AddSingleSnapshotBoolLineFromPayload(card, C.LabelSingleMostPopularChoreographyPrefix, payload, C.JsonSingleMostPopularChoreo);
        }

        /// <summary>
        /// Creates one white snapshot card container used for reconstructed release details.
        /// </summary>
        private static Transform CreateSingleReleaseSnapshotCard(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            GameObject card = CreateUiObject(
                C.SingleReleaseSnapshotCardName + C.SeparatorUnderscore + Guid.NewGuid().ToString(C.FormatGuidCompact, CultureInfo.InvariantCulture),
                parent);
            VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = C.SingleSnapshotCardSpacing;
            layout.padding = new RectOffset(
                C.SingleSnapshotCardPaddingHorizontal,
                C.SingleSnapshotCardPaddingHorizontal,
                C.SingleSnapshotCardPaddingVertical,
                C.SingleSnapshotCardPaddingVertical);

            LayoutElement cardLayout = card.AddComponent<LayoutElement>();
            cardLayout.flexibleWidth = C.FloatOne;

            ContentSizeFitter fitter = card.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Image background = card.AddComponent<Image>();
            background.color = new Color(0.98f, 0.98f, 0.98f, C.FloatOne);

            return card.transform;
        }

        /// <summary>
        /// Adds one single-snapshot line using standard diary typography.
        /// </summary>
        private void AddSingleSnapshotTextLine(Transform parent, string text, bool header)
        {
            if (parent == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            if (header)
            {
                AddTitle(parent, text);
                return;
            }

            AddText(parent, text);
        }

        /// <summary>
        /// Applies compact dark text styling inside white snapshot cards.
        /// </summary>
        private static void ApplySingleSnapshotTextStyle(GameObject textObject, bool header)
        {
            if (textObject == null)
            {
                return;
            }

            Color lineColor = new Color(0.12f, 0.14f, 0.18f, C.FloatOne);
            int fontSize = header ? C.SingleSnapshotHeaderFontSize : C.SingleSnapshotBodyFontSize;

            TextMeshProUGUI[] tmps = textObject.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = C.ZeroIndex; i < tmps.Length; i++)
            {
                TextMeshProUGUI tmp = tmps[i];
                if (tmp == null)
                {
                    continue;
                }

                tmp.color = lineColor;
                tmp.fontSize = fontSize;
                tmp.fontStyle = header ? FontStyles.Bold : FontStyles.Normal;
                tmp.alignment = TextAlignmentOptions.Left;
                tmp.enableWordWrapping = true;
            }

            Text[] texts = textObject.GetComponentsInChildren<Text>(true);
            for (int i = C.ZeroIndex; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null)
                {
                    continue;
                }

                text.color = lineColor;
                text.fontSize = fontSize;
                text.fontStyle = header ? FontStyle.Bold : FontStyle.Normal;
                text.alignment = TextAnchor.MiddleLeft;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Overflow;
            }
        }

        /// <summary>
        /// Adds one long metric line when payload contains the requested field.
        /// </summary>
        private void AddSingleSnapshotLongLineFromPayload(Transform parent, string labelPrefix, JSONNode payload, string field)
        {
            long value;
            if (!TryReadLongField(payload, field, out value))
            {
                return;
            }

            AddSingleSnapshotTextLine(parent, labelPrefix + value.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture), false);
        }

        /// <summary>
        /// Adds one int metric line when payload contains the requested field.
        /// </summary>
        private void AddSingleSnapshotIntLineFromPayload(Transform parent, string labelPrefix, JSONNode payload, string field, bool signed)
        {
            int value;
            if (!TryReadIntField(payload, field, out value))
            {
                return;
            }

            string valueText = signed
                ? FormatSignedNumber(value)
                : value.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture);
            AddSingleSnapshotTextLine(parent, labelPrefix + valueText, false);
        }

        /// <summary>
        /// Adds one bool metric line when payload contains the requested field.
        /// </summary>
        private void AddSingleSnapshotBoolLineFromPayload(Transform parent, string labelPrefix, JSONNode payload, string field)
        {
            bool value;
            if (!TryReadBoolField(payload, field, out value))
            {
                return;
            }

            AddSingleSnapshotTextLine(parent, labelPrefix + YesNo(value), false);
        }

        /// <summary>
        /// Adds one numeric float metric line when payload contains the requested field.
        /// </summary>
        private void AddSingleSnapshotFloatLineFromPayload(
            Transform parent,
            string labelPrefix,
            JSONNode payload,
            string field,
            string format,
            string suffix)
        {
            float value;
            if (!TryReadSingleFloatField(payload, field, out value))
            {
                return;
            }

            AddSingleSnapshotTextLine(
                parent,
                labelPrefix + value.ToString(format, CultureInfo.InvariantCulture) + suffix,
                false);
        }

        /// <summary>
        /// Adds one percentage metric line when payload contains the requested field.
        /// </summary>
        private void AddSingleSnapshotPercentLineFromPayload(Transform parent, string labelPrefix, JSONNode payload, string field)
        {
            float value;
            if (!TryReadSingleFloatField(payload, field, out value))
            {
                return;
            }

            AddSingleSnapshotTextLine(parent, labelPrefix + FormatSingleFanAppealPercent(value), false);
        }

        /// <summary>
        /// Adds one normalized code line when payload contains the requested field.
        /// </summary>
        private void AddSingleSnapshotCodeLineFromPayload(Transform parent, string labelPrefix, JSONNode payload, string field)
        {
            string rawValue = NormalizeRawText(ReadStr(payload, field));
            if (rawValue == C.LabelUnknown)
            {
                return;
            }

            AddSingleSnapshotTextLine(parent, labelPrefix + HumanizeUnknown(rawValue), false);
        }

        /// <summary>
        /// Reads one integer payload value with fallback.
        /// </summary>
        private static int ReadSingleIntValue(JSONNode payload, string field, int fallbackValue)
        {
            int value;
            return TryReadIntField(payload, field, out value) ? value : fallbackValue;
        }

        /// <summary>
        /// Reads one long payload value with fallback.
        /// </summary>
        private static long ReadSingleLongValue(JSONNode payload, string field, long fallbackValue)
        {
            long value;
            return TryReadLongField(payload, field, out value) ? value : fallbackValue;
        }

        /// <summary>
        /// Tries to read one finite float payload value.
        /// </summary>
        private static bool TryReadSingleFloatField(JSONNode payload, string field, out float value)
        {
            value = C.FloatZero;
            if (!HasPayloadValue(payload, field))
            {
                return false;
            }

            value = ReadFloat(payload, field);
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        /// <summary>
        /// Formats one fan-appeal ratio to percentage text.
        /// </summary>
        private static string FormatSingleFanAppealPercent(float ratio)
        {
            if (float.IsNaN(ratio) || float.IsInfinity(ratio))
            {
                return C.LabelUnknown;
            }

            float percent = Mathf.Abs(ratio) <= 1.01f ? ratio * 100f : ratio;
            return percent.ToString(C.FormatSingleMetricOneDecimal, CultureInfo.InvariantCulture) + "%";
        }

        /// <summary>
        /// Renders one fan-segment summary payload as readable lines.
        /// </summary>
        private void RenderSingleFanSegmentSummaryLines(Transform parent, string rawSummary)
        {
            List<KeyValuePair<string, long>> parsedSummary = ParseSingleFanSegmentSummary(rawSummary);
            if (parsedSummary.Count == C.ZeroIndex)
            {
                AddSingleSnapshotTextLine(parent, C.LabelSingleSegmentSummaryEmpty, false);
                return;
            }

            for (int i = C.ZeroIndex; i < parsedSummary.Count; i++)
            {
                KeyValuePair<string, long> row = parsedSummary[i];
                AddSingleSnapshotTextLine(
                    parent,
                    row.Key + C.SeparatorColonSpace + row.Value.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture),
                    false);
            }
        }

        /// <summary>
        /// Parses compact fan-segment summaries in `segment_key:value|segment_key:value` format.
        /// </summary>
        private static List<KeyValuePair<string, long>> ParseSingleFanSegmentSummary(string rawSummary)
        {
            List<KeyValuePair<string, long>> parsed = new List<KeyValuePair<string, long>>();
            string normalizedSummary = NormalizeRawText(rawSummary);
            if (normalizedSummary == C.LabelUnknown)
            {
                return parsed;
            }

            string[] entries = normalizedSummary.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = C.ZeroIndex; i < entries.Length; i++)
            {
                string entry = entries[i];
                if (string.IsNullOrEmpty(entry))
                {
                    continue;
                }

                int separatorIndex = entry.LastIndexOf(':');
                if (separatorIndex <= C.ZeroIndex || separatorIndex >= entry.Length - C.LastFromCount)
                {
                    continue;
                }

                string rawSegmentKey = entry.Substring(C.ZeroIndex, separatorIndex).Trim();
                string rawValue = entry.Substring(separatorIndex + C.LastFromCount).Trim();
                long value;
                if (!long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                {
                    continue;
                }

                string segmentLabel = HumanizeSingleFanSegmentKey(rawSegmentKey);
                parsed.Add(new KeyValuePair<string, long>(segmentLabel, value));
            }

            parsed.Sort(delegate (KeyValuePair<string, long> left, KeyValuePair<string, long> right)
            {
                int byValue = right.Value.CompareTo(left.Value);
                if (byValue != C.ZeroIndex)
                {
                    return byValue;
                }

                return string.Compare(left.Key, right.Key, StringComparison.Ordinal);
            });

            return parsed;
        }

        /// <summary>
        /// Humanizes one compact fan-segment key.
        /// </summary>
        private static string HumanizeSingleFanSegmentKey(string rawSegmentKey)
        {
            string normalized = NormalizeRawText(rawSegmentKey);
            if (normalized == C.LabelUnknown)
            {
                return C.LabelUnknown;
            }

            string[] dimensions = normalized.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> humanized = new List<string>();
            for (int i = C.ZeroIndex; i < dimensions.Length; i++)
            {
                string dimension = NormalizeRawText(dimensions[i]);
                if (dimension == C.LabelUnknown)
                {
                    continue;
                }

                humanized.Add(HumanizeUnknown(dimension));
            }

            if (humanized.Count == C.ZeroIndex)
            {
                return normalized;
            }

            return string.Join(C.SeparatorSpaceSlashSpace, humanized.ToArray());
        }

        /// <summary>
        /// Adds single metadata lines in reader-friendly format.
        /// </summary>
        private void AddSingleSourceMetadata(singles._single single)
        {
            if (single == null)
            {
                AddText(diaryDetailContentRoot, C.LabelSingleNotFound);
                return;
            }

            string groupName = C.LabelUnknown;
            Groups._group group = single.GetGroup();
            if (group != null)
            {
                groupName = Normalize(group.Title);
            }

            DateTime releaseDate = single.ReleaseData != null ? single.ReleaseData.ReleaseDate : DateTime.MinValue;
            string releaseDateText = releaseDate.Year > DateTime.MinValue.Year
                ? FormatUiDate(releaseDate)
                : C.LabelUnknown;

            int chartPosition = single.ReleaseData != null ? single.ReleaseData.Chart_Position : C.UnknownChartPosition;
            string chartText = chartPosition > C.UnknownChartPosition
                ? C.SeparatorHash + chartPosition.ToString(CultureInfo.InvariantCulture)
                : (chartPosition == C.UnknownChartPosition ? C.LabelSingleDidNotChart : C.LabelUnknown);

            int quality = single.ReleaseData != null ? single.ReleaseData.Quality : C.ZeroIndex;
            int fanSatisfaction = single.ReleaseData != null ? single.ReleaseData.Fan_Satisfaction : C.ZeroIndex;
            long totalSales = single.ReleaseData != null ? single.ReleaseData.Sales : single.GetTotalSales();
            string totalSalesText = totalSales.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture);

            AddText(diaryDetailContentRoot, C.LabelSingleTitlePrefix + Normalize(single.title));
            AddText(diaryDetailContentRoot, C.LabelSingleGroupPrefix + groupName);
            AddText(diaryDetailContentRoot, C.LabelSingleReleaseDatePrefix + releaseDateText);
            AddText(diaryDetailContentRoot, C.LabelSingleSalesPrefix + totalSalesText);
            AddText(diaryDetailContentRoot, C.LabelSingleChartPrefix + chartText);
            AddText(diaryDetailContentRoot, C.LabelSingleQualityPrefix + quality.ToString(CultureInfo.InvariantCulture));
            AddText(diaryDetailContentRoot, C.LabelSingleFanSatisfactionPrefix + fanSatisfaction.ToString(CultureInfo.InvariantCulture));
            AddText(diaryDetailContentRoot, C.LabelSingleGenrePrefix + ResolveSingleParamTitle(single.genre));
            AddText(diaryDetailContentRoot, C.LabelSingleLyricsPrefix + ResolveSingleParamTitle(single.lyrics));
            AddText(diaryDetailContentRoot, C.LabelSingleChoreographyPrefix + ResolveSingleParamTitle(single.choreography));
            AddText(diaryDetailContentRoot, C.LabelSingleMarketingPrefix + BuildSingleMarketingSummary(single));
        }

        /// <summary>
        /// Resolves one single parameter title with fallback.
        /// </summary>
        private static string ResolveSingleParamTitle(singles._param param)
        {
            if (param == null)
            {
                return C.LabelUnknown;
            }

            return Normalize(param.GetTitle());
        }

        /// <summary>
        /// Builds comma-separated marketing summary for one single.
        /// </summary>
        private static string BuildSingleMarketingSummary(singles._single single)
        {
            if (single == null || single.marketing == null || single.marketing.Count == C.ZeroIndex)
            {
                return C.LabelUnknown;
            }

            List<string> marketingTitles = new List<string>();
            for (int i = C.ZeroIndex; i < single.marketing.Count; i++)
            {
                singles._param param = single.marketing[i];
                if (param != null)
                {
                    marketingTitles.Add(ResolveSingleParamTitle(param));
                }
            }

            if (marketingTitles.Count == C.ZeroIndex)
            {
                return C.LabelUnknown;
            }

            return string.Join(C.ListJoinSeparator, marketingTitles.ToArray());
        }

        /// <summary>
        /// One sortable senbatsu member entry used by grouped row rendering.
        /// </summary>
        private sealed class SingleSenbatsuMemberRenderInfo
        {
            internal data_girls.girls Girl;
            internal int RowIndex;
            internal int PositionIndex;
        }

        /// <summary>
        /// Renders senbatsu member icons grouped by row from back to front.
        /// </summary>
        private void RenderSingleSenbatsuGrid(singles._single single)
        {
            AddTitle(diaryDetailContentRoot, C.TitleSingleSenbatsu);

            List<SingleSenbatsuMemberRenderInfo> members = BuildSingleSenbatsuMemberRenderInfos(single);
            if (members.Count == C.ZeroIndex)
            {
                AddText(diaryDetailContentRoot, C.LabelSingleRosterEmpty);
                return;
            }

            GameObject gridObject = CreateUiObject(C.SingleSenbatsuGridName, diaryDetailContentRoot);
            VerticalLayoutGroup rowsLayout = gridObject.AddComponent<VerticalLayoutGroup>();
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = true;
            rowsLayout.childForceExpandWidth = false;
            rowsLayout.childForceExpandHeight = false;
            rowsLayout.spacing = C.SingleSenbatsuGridSpacingY;
            rowsLayout.childAlignment = TextAnchor.UpperLeft;

            ContentSizeFitter fitter = gridObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            int currentRowIndex = int.MinValue;
            Transform currentRowContainer = null;
            int renderedCount = C.ZeroIndex;
            for (int i = C.ZeroIndex; i < members.Count; i++)
            {
                SingleSenbatsuMemberRenderInfo member = members[i];
                if (member == null || member.Girl == null)
                {
                    continue;
                }

                if (currentRowContainer == null || member.RowIndex != currentRowIndex)
                {
                    currentRowIndex = member.RowIndex;
                    int filledInRow = CountSingleSenbatsuMembersInRow(members, currentRowIndex);
                    currentRowContainer = CreateSingleSenbatsuRow(gridObject.transform, currentRowIndex, filledInRow);
                }

                CreateSingleSenbatsuCard(currentRowContainer, member.Girl, member.RowIndex, member.PositionIndex);
                renderedCount++;
            }

            if (renderedCount == C.ZeroIndex)
            {
                UnityEngine.Object.Destroy(gridObject);
                AddText(diaryDetailContentRoot, C.LabelSingleRosterEmpty);
            }
        }

        /// <summary>
        /// Counts rendered members for one row index.
        /// </summary>
        private static int CountSingleSenbatsuMembersInRow(List<SingleSenbatsuMemberRenderInfo> members, int rowIndex)
        {
            if (members == null || members.Count == C.ZeroIndex)
            {
                return C.ZeroIndex;
            }

            int count = C.ZeroIndex;
            for (int i = C.ZeroIndex; i < members.Count; i++)
            {
                SingleSenbatsuMemberRenderInfo member = members[i];
                if (member != null && member.RowIndex == rowIndex && member.Girl != null)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Collects and sorts senbatsu members by row (descending) then position (ascending).
        /// </summary>
        private static List<SingleSenbatsuMemberRenderInfo> BuildSingleSenbatsuMemberRenderInfos(singles._single single)
        {
            List<SingleSenbatsuMemberRenderInfo> members = new List<SingleSenbatsuMemberRenderInfo>();
            if (single == null || single.girls == null || single.girls.Count == C.ZeroIndex)
            {
                return members;
            }

            int memberCount = Mathf.Min(single.girls.Count, C.SenbatsuMaximumSlots);
            for (int i = C.ZeroIndex; i < memberCount; i++)
            {
                data_girls.girls girl = single.girls[i];
                if (girl == null)
                {
                    continue;
                }

                members.Add(new SingleSenbatsuMemberRenderInfo
                {
                    Girl = girl,
                    RowIndex = singles.GetGirlsRowInSenbatsu(single.girls, girl),
                    PositionIndex = singles.GetGirlsPositionInSenbatsu(single.girls, girl)
                });
            }

            members.Sort(delegate (SingleSenbatsuMemberRenderInfo left, SingleSenbatsuMemberRenderInfo right)
            {
                if (left == null && right == null)
                {
                    return C.ZeroIndex;
                }

                if (left == null)
                {
                    return C.LastFromCount;
                }

                if (right == null)
                {
                    return -C.LastFromCount;
                }

                int rowComparison = right.RowIndex.CompareTo(left.RowIndex);
                if (rowComparison != C.ZeroIndex)
                {
                    return rowComparison;
                }

                int positionComparison = left.PositionIndex.CompareTo(right.PositionIndex);
                if (positionComparison != C.ZeroIndex)
                {
                    return positionComparison;
                }

                int leftId = left.Girl != null ? left.Girl.id : C.InvalidId;
                int rightId = right.Girl != null ? right.Girl.id : C.InvalidId;
                return leftId.CompareTo(rightId);
            });

            return members;
        }

        /// <summary>
        /// Creates one senbatsu row container and returns the icon strip transform.
        /// </summary>
        private Transform CreateSingleSenbatsuRow(Transform parent, int rowIndex, int filledInRow)
        {
            int displayRow = rowIndex + C.SenbatsuDisplayIndexOffset;
            string rowName = C.SingleSenbatsuRowNamePrefix + displayRow.ToString(CultureInfo.InvariantCulture);
            GameObject rowObject = CreateUiObject(rowName, parent);

            VerticalLayoutGroup rowLayout = rowObject.AddComponent<VerticalLayoutGroup>();
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.spacing = C.SingleSenbatsuRowSpacing;
            rowLayout.childAlignment = TextAnchor.UpperLeft;

            LayoutElement rowElement = rowObject.AddComponent<LayoutElement>();
            rowElement.flexibleWidth = C.FloatOne;

            GameObject rowLabel = AddCardText(
                rowObject.transform,
                C.LabelSingleSenbatsuRowPrefix +
                displayRow.ToString(CultureInfo.InvariantCulture) +
                C.SeparatorSpaceOpenParen +
                Mathf.Clamp(filledInRow, C.ZeroIndex, displayRow).ToString(CultureInfo.InvariantCulture) +
                C.SeparatorSlash +
                displayRow.ToString(CultureInfo.InvariantCulture) +
                C.SeparatorCloseParen,
                false);
            LayoutElement rowLabelLayout = rowLabel != null ? rowLabel.GetComponent<LayoutElement>() : null;
            if (rowLabelLayout != null)
            {
                rowLabelLayout.preferredHeight = C.SingleSenbatsuRowLabelHeight;
            }

            GameObject iconStrip = CreateUiObject(rowName + C.UiSuffixIcons, rowObject.transform);
            HorizontalLayoutGroup iconStripLayout = iconStrip.AddComponent<HorizontalLayoutGroup>();
            iconStripLayout.childControlWidth = true;
            iconStripLayout.childControlHeight = true;
            iconStripLayout.childForceExpandWidth = false;
            iconStripLayout.childForceExpandHeight = false;
            iconStripLayout.spacing = C.SingleSenbatsuGridSpacingX;
            iconStripLayout.childAlignment = TextAnchor.UpperLeft;

            ContentSizeFitter iconStripFitter = iconStrip.AddComponent<ContentSizeFitter>();
            iconStripFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            iconStripFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return iconStrip.transform;
        }

        /// <summary>
        /// Creates one compact senbatsu member icon with profile-open action.
        /// </summary>
        private void CreateSingleSenbatsuCard(
            Transform parent,
            data_girls.girls girl,
            int rowIndex,
            int positionIndex)
        {
            if (parent == null || girl == null)
            {
                return;
            }

            string cardName =
                C.SingleSenbatsuCardNamePrefix +
                rowIndex.ToString(CultureInfo.InvariantCulture) +
                C.SeparatorUnderscore +
                girl.id.ToString(CultureInfo.InvariantCulture);
            GameObject card = CreateUiObject(cardName, parent);
            VerticalLayoutGroup cardLayout = card.AddComponent<VerticalLayoutGroup>();
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = false;
            cardLayout.childForceExpandHeight = false;
            cardLayout.spacing = C.SingleSenbatsuCardSpacing;
            cardLayout.childAlignment = TextAnchor.UpperCenter;

            LayoutElement cardElement = card.AddComponent<LayoutElement>();
            cardElement.preferredWidth = C.SingleSenbatsuIconCardWidth;
            cardElement.preferredHeight = C.SingleSenbatsuIconCardHeight;
            cardElement.minWidth = C.SingleSenbatsuIconCardWidth;
            cardElement.minHeight = C.SingleSenbatsuIconCardHeight;
            cardElement.flexibleWidth = C.FloatZero;
            cardElement.flexibleHeight = C.FloatZero;

            GameObject portraitObject = CreateUiObject(cardName + C.UiSuffixPortrait, card.transform);
            Image portraitImage = portraitObject.AddComponent<Image>();
            portraitImage.color = mainScript.white32;
            portraitImage.preserveAspect = true;

            LayoutElement portraitElement = portraitObject.AddComponent<LayoutElement>();
            portraitElement.preferredWidth = C.SingleSenbatsuIconSize;
            portraitElement.preferredHeight = C.SingleSenbatsuIconSize;

            int displayPosition = positionIndex + C.SenbatsuDisplayIndexOffset;
            ApplyRelatedIdolHeadshot(portraitImage, girl);

            string positionLabel = C.SeparatorHash + displayPosition.ToString(CultureInfo.InvariantCulture);
            GameObject posTextObject = AddCardText(card.transform, positionLabel, false);
            LayoutElement posLayout = posTextObject != null ? posTextObject.GetComponent<LayoutElement>() : null;
            if (posLayout != null)
            {
                posLayout.preferredHeight = C.SingleSenbatsuIconMetaLineHeight;
                posLayout.flexibleWidth = C.FloatZero;
            }

            Button cardButton = card.AddComponent<Button>();
            Image cardBackground = card.AddComponent<Image>();
            cardBackground.color = new Color(C.FloatZero, C.FloatZero, C.FloatZero, C.FloatZero);
            cardButton.targetGraphic = cardBackground;
            cardButton.transition = Selectable.Transition.ColorTint;
            int idolId = girl.id;
            cardButton.onClick.AddListener(delegate
            {
                OpenIdolProfile(idolId);
            });

            RebuildLayout(posTextObject != null ? posTextObject.transform.parent : null);
        }

        /// <summary>
        /// Renders senbatsu stat values (C.MinId-C.EventsRenderStep) for single participation entries.
        /// </summary>
        private void RenderSingleParticipationSenbatsuStats(singles._single single, JSONNode payload)
        {
            List<data_girls.girls.param> stats;
            bool hasStats = TryResolveSingleParticipationSenbatsuStats(single, payload, out stats);
            bool hasSnapshotMetrics = HasSingleParticipationSnapshotMetrics(payload);
            if (!hasStats && !hasSnapshotMetrics)
            {
                return;
            }

            AddTitle(diaryDetailContentRoot, C.TitleSingleSenbatsuStats);
            AddDivider(diaryDetailContentRoot);
            if (hasStats)
            {
                for (int i = C.ZeroIndex; i < stats.Count; i++)
                {
                    data_girls.girls.param stat = stats[i];
                    if (stat == null)
                    {
                        continue;
                    }

                    string statName = Normalize(data_girls.GetParamName(stat.type));
                    string statValue = FormatSingleSenbatsuStatValue(stat.val);
                    AddText(diaryDetailContentRoot, statName + C.SeparatorColonSpace + statValue + C.SeparatorSlashOneHundred);
                }
            }

            float fameSnapshot = ReadFloat(payload, C.JsonSingleFameOfSenbatsu);
            if (!float.IsNaN(fameSnapshot) && !float.IsInfinity(fameSnapshot) && fameSnapshot >= C.FloatZero)
            {
                AddText(diaryDetailContentRoot, C.TextFameOfSenbatsu + FormatSingleSenbatsuFameValue(fameSnapshot) + C.SeparatorSlashTen);
            }

            RenderSingleParticipationSnapshotMetrics(single, payload);
        }

        /// <summary>
        /// Returns true when payload contains release-time fan/sales snapshot metrics.
        /// </summary>
        private static bool HasSingleParticipationSnapshotMetrics(JSONNode payload)
        {
            return payload != null
                && (HasPayloadValue(payload, C.JsonTotalSales)
                || HasPayloadValue(payload, C.JsonSingleQuantity)
                || HasPayloadValue(payload, C.JsonSingleProfit)
                || HasPayloadValue(payload, C.JsonFanSatisfaction)
                || HasPayloadValue(payload, C.JsonFanBuzz)
                || HasPayloadValue(payload, C.JsonNewFans)
                || HasPayloadValue(payload, C.JsonNewHardcoreFans)
                || HasPayloadValue(payload, C.JsonNewCasualFans)
                || HasPayloadValue(payload, C.JsonSingleFanAppealMale)
                || HasPayloadValue(payload, C.JsonSingleFanAppealFemale)
                || HasPayloadValue(payload, C.JsonSingleFanAppealCasual)
                || HasPayloadValue(payload, C.JsonSingleFanAppealHardcore)
                || HasPayloadValue(payload, C.JsonSingleFanAppealTeen)
                || HasPayloadValue(payload, C.JsonSingleFanAppealYoungAdult)
                || HasPayloadValue(payload, C.JsonSingleFanAppealAdult)
                || HasPayloadValue(payload, C.JsonSingleFanSegmentSalesSummary)
                || HasPayloadValue(payload, C.JsonSingleFanSegmentNewFansSummary));
        }

        /// <summary>
        /// Adds release fan approval/new fan/cohort sales details under senbatsu snapshot section.
        /// </summary>
        private void RenderSingleParticipationSnapshotMetrics(singles._single single, JSONNode payload)
        {
            long totalSales = C.LongZero;
            bool hasTotalSales = TryReadLongField(payload, C.JsonTotalSales, out totalSales);
            if (hasTotalSales)
            {
                int singleQuantity;
                if (TryReadIntField(payload, C.JsonSingleQuantity, out singleQuantity) && singleQuantity > C.ZeroIndex)
                {
                    AddText(
                        diaryDetailContentRoot,
                        C.LabelSingleSalesPrefix +
                        totalSales.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture) +
                        C.SeparatorSpaceSlashSpace +
                        singleQuantity.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
                }
                else
                {
                    AddText(diaryDetailContentRoot, C.LabelSingleSalesPrefix + totalSales.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
                }
            }

            long singleProfit;
            if (TryReadLongField(payload, C.JsonSingleProfit, out singleProfit))
            {
                AddText(diaryDetailContentRoot, C.LabelSingleProfitPrefix + singleProfit.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
            }

            int fanSatisfaction;
            if (TryReadIntField(payload, C.JsonFanSatisfaction, out fanSatisfaction))
            {
                AddText(diaryDetailContentRoot, C.LabelSingleFanSatisfactionPrefix + fanSatisfaction.ToString(CultureInfo.InvariantCulture));
            }

            int fanBuzz;
            if (TryReadIntField(payload, C.JsonFanBuzz, out fanBuzz))
            {
                AddText(diaryDetailContentRoot, C.LabelSingleFanBuzzPrefix + fanBuzz.ToString(CultureInfo.InvariantCulture));
            }

            int newFans = C.ZeroIndex;
            bool hasNewFans = TryReadIntField(payload, C.JsonNewFans, out newFans);
            if (hasNewFans)
            {
                AddText(diaryDetailContentRoot, C.LabelSingleNewFansPrefix + FormatSignedNumber(newFans));
            }

            int newHardcoreFans;
            if (TryReadIntField(payload, C.JsonNewHardcoreFans, out newHardcoreFans))
            {
                AddText(diaryDetailContentRoot, C.LabelSingleNewHardcoreFansPrefix + FormatSignedNumber(newHardcoreFans));
            }

            int newCasualFans;
            if (TryReadIntField(payload, C.JsonNewCasualFans, out newCasualFans))
            {
                AddText(diaryDetailContentRoot, C.LabelSingleNewCasualFansPrefix + FormatSignedNumber(newCasualFans));
            }

            Dictionary<string, long> salesByAudience = ParseSingleAudienceSegmentValues(ReadStr(payload, C.JsonSingleFanSegmentSalesSummary));
            bool usedSalesFanAppealFallback = false;
            if (!HasAnyNonZeroAudienceValues(salesByAudience))
            {
                Dictionary<string, long> liveSalesByAudience = BuildSingleAudienceSegmentValuesFromLiveSingle(single, true);
                if (HasAnyNonZeroAudienceValues(liveSalesByAudience))
                {
                    salesByAudience = liveSalesByAudience;
                }
            }
            if (!HasAnyNonZeroAudienceValues(salesByAudience) && hasTotalSales && totalSales > C.LongZero)
            {
                Dictionary<string, long> fanAppealSalesByAudience = BuildSingleAudienceSegmentValuesFromFanAppeal(payload, totalSales);
                if (HasAnyNonZeroAudienceValues(fanAppealSalesByAudience))
                {
                    salesByAudience = fanAppealSalesByAudience;
                    usedSalesFanAppealFallback = true;
                }
                else
                {
                    salesByAudience.Clear();
                }
            }
            RenderSingleAudienceBreakdownSection(C.TitleSingleFanSegmentSales, salesByAudience, hasTotalSales ? totalSales : C.LongZero);

            Dictionary<string, long> newFansByAudience = ParseSingleAudienceSegmentValues(ReadStr(payload, C.JsonSingleFanSegmentNewFansSummary));
            bool usedNewFansFanAppealFallback = false;
            if (!HasAnyNonZeroAudienceValues(newFansByAudience))
            {
                Dictionary<string, long> liveNewFansByAudience = BuildSingleAudienceSegmentValuesFromLiveSingle(single, false);
                if (HasAnyNonZeroAudienceValues(liveNewFansByAudience))
                {
                    newFansByAudience = liveNewFansByAudience;
                }
            }
            if (!HasAnyNonZeroAudienceValues(newFansByAudience) && hasNewFans && newFans > C.ZeroIndex)
            {
                Dictionary<string, long> fanAppealNewFansByAudience = BuildSingleAudienceSegmentValuesFromFanAppeal(payload, newFans);
                if (HasAnyNonZeroAudienceValues(fanAppealNewFansByAudience))
                {
                    newFansByAudience = fanAppealNewFansByAudience;
                    usedNewFansFanAppealFallback = true;
                }
                else
                {
                    newFansByAudience.Clear();
                }
            }
            long totalNewFans = hasNewFans ? newFans : C.LongZero;
            RenderSingleAudienceBreakdownSection(C.TitleSingleFanSegmentNewFans, newFansByAudience, totalNewFans);
            if (salesByAudience.Count == C.ZeroIndex && newFansByAudience.Count == C.ZeroIndex)
            {
                AddText(diaryDetailContentRoot, C.LabelSingleCohortBreakdownUnavailable);
            }
            else if (usedSalesFanAppealFallback || usedNewFansFanAppealFallback)
            {
                AddText(diaryDetailContentRoot, C.TextSingleCohortEstimatedFromFanAppeal);
            }

            bool hasAnyFanAppeal =
                HasPayloadValue(payload, C.JsonSingleFanAppealMale) ||
                HasPayloadValue(payload, C.JsonSingleFanAppealFemale) ||
                HasPayloadValue(payload, C.JsonSingleFanAppealCasual) ||
                HasPayloadValue(payload, C.JsonSingleFanAppealHardcore) ||
                HasPayloadValue(payload, C.JsonSingleFanAppealTeen) ||
                HasPayloadValue(payload, C.JsonSingleFanAppealYoungAdult) ||
                HasPayloadValue(payload, C.JsonSingleFanAppealAdult);
            if (hasAnyFanAppeal)
            {
                AddTitle(diaryDetailContentRoot, C.TitleSingleFanAppeal);
                AddDivider(diaryDetailContentRoot);
            }

            AddSingleParticipationFanAppealLine(payload, C.LabelSingleFanAppealMalePrefix, C.JsonSingleFanAppealMale);
            AddSingleParticipationFanAppealLine(payload, C.LabelSingleFanAppealFemalePrefix, C.JsonSingleFanAppealFemale);
            AddSingleParticipationFanAppealLine(payload, C.LabelSingleFanAppealCasualPrefix, C.JsonSingleFanAppealCasual);
            AddSingleParticipationFanAppealLine(payload, C.LabelSingleFanAppealHardcorePrefix, C.JsonSingleFanAppealHardcore);
            AddSingleParticipationFanAppealLine(payload, C.LabelSingleFanAppealTeenPrefix, C.JsonSingleFanAppealTeen);
            AddSingleParticipationFanAppealLine(payload, C.LabelSingleFanAppealYoungAdultPrefix, C.JsonSingleFanAppealYoungAdult);
            AddSingleParticipationFanAppealLine(payload, C.LabelSingleFanAppealAdultPrefix, C.JsonSingleFanAppealAdult);
        }

        /// <summary>
        /// Adds one fan appeal line using payload percentage ratio.
        /// </summary>
        private void AddSingleParticipationFanAppealLine(JSONNode payload, string labelPrefix, string field)
        {
            float value;
            if (!TryReadSingleFloatField(payload, field, out value))
            {
                return;
            }

            AddText(diaryDetailContentRoot, labelPrefix + FormatSingleFanAppealPercent(value));
        }

        /// <summary>
        /// Renders one ordered 12-cohort audience section with values and percentages.
        /// </summary>
        private void RenderSingleAudienceBreakdownSection(string title, Dictionary<string, long> valuesByAudience, long totalReference)
        {
            if (valuesByAudience == null || valuesByAudience.Count == C.ZeroIndex)
            {
                return;
            }

            AddTitle(diaryDetailContentRoot, title);
            AddDivider(diaryDetailContentRoot);

            long denominator = totalReference;
            if (denominator <= C.LongZero)
            {
                foreach (KeyValuePair<string, long> entry in valuesByAudience)
                {
                    denominator += entry.Value;
                }
            }

            bool isSalesByAudience = string.Equals(title, C.TitleSingleFanSegmentSales, StringComparison.Ordinal);
            if (isSalesByAudience)
            {
                long totalSales = totalReference > C.LongZero ? totalReference : denominator;
                AddText(
                    diaryDetailContentRoot,
                    C.LabelSingleTotalSalesPrefix + totalSales.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
            }
            else if (string.Equals(title, C.TitleSingleFanSegmentNewFans, StringComparison.Ordinal))
            {
                long totalNewFans = totalReference != C.LongZero ? totalReference : denominator;
                AddText(
                    diaryDetailContentRoot,
                    C.LabelSingleTotalNewFansPrefix + FormatSignedNumber(totalNewFans));
            }

            string[] genders = { "male", "female" };
            string[] intensities = { "casual", "hardcore" };
            string[] ages = { "teen", "youngadult", "adult" };
            for (int genderIndex = C.ZeroIndex; genderIndex < genders.Length; genderIndex++)
            {
                string gender = genders[genderIndex];
                for (int intensityIndex = C.ZeroIndex; intensityIndex < intensities.Length; intensityIndex++)
                {
                    string intensity = intensities[intensityIndex];
                    for (int ageIndex = C.ZeroIndex; ageIndex < ages.Length; ageIndex++)
                    {
                        string age = ages[ageIndex];
                        string key = BuildSingleAudienceSegmentKey(gender, intensity, age);
                        long value;
                        valuesByAudience.TryGetValue(key, out value);
                        float percent = denominator > C.LongZero
                            ? ((float)value * 100f) / denominator
                            : C.FloatZero;

                        string label = string.Concat(
                            HumanizeCode(gender),
                            C.SeparatorSpace,
                            HumanizeCode(intensity),
                            C.SeparatorSpace,
                            HumanizeCode(age));
                        AddText(
                            diaryDetailContentRoot,
                            label +
                            C.SeparatorColonSpace +
                            value.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture) +
                            C.SeparatorSpaceOpenParen +
                            percent.ToString(C.FormatSingleMetricOneDecimal, CultureInfo.InvariantCulture) +
                            "%" +
                            C.SeparatorCloseParen);
                    }
                }
            }
        }

        /// <summary>
        /// Parses compact audience summaries into canonical cohort keys.
        /// </summary>
        private static Dictionary<string, long> ParseSingleAudienceSegmentValues(string rawSummary)
        {
            Dictionary<string, long> valuesByAudience = new Dictionary<string, long>(StringComparer.Ordinal);
            string normalizedSummary = NormalizeRawText(rawSummary);
            if (normalizedSummary == C.LabelUnknown)
            {
                return valuesByAudience;
            }

            string[] entries = normalizedSummary.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = C.ZeroIndex; i < entries.Length; i++)
            {
                string entry = entries[i];
                if (string.IsNullOrEmpty(entry))
                {
                    continue;
                }

                int separatorIndex = entry.LastIndexOf(':');
                if (separatorIndex <= C.ZeroIndex || separatorIndex >= entry.Length - C.LastFromCount)
                {
                    separatorIndex = entry.LastIndexOf('=');
                }

                if (separatorIndex <= C.ZeroIndex || separatorIndex >= entry.Length - C.LastFromCount)
                {
                    continue;
                }

                string segmentKey = entry.Substring(C.ZeroIndex, separatorIndex).Trim();
                string rawValue = entry.Substring(separatorIndex + C.LastFromCount).Trim();
                long value;
                if (!long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                {
                    continue;
                }

                string gender;
                string intensity;
                string age;
                if (!TryParseSingleAudienceSegmentKey(segmentKey, out gender, out intensity, out age))
                {
                    continue;
                }

                string key = BuildSingleAudienceSegmentKey(gender, intensity, age);
                long existingValue;
                if (valuesByAudience.TryGetValue(key, out existingValue))
                {
                    valuesByAudience[key] = existingValue + value;
                }
                else
                {
                    valuesByAudience.Add(key, value);
                }
            }

            return valuesByAudience;
        }

        /// <summary>
        /// Returns true when at least one audience cohort value is non-zero.
        /// </summary>
        private static bool HasAnyNonZeroAudienceValues(Dictionary<string, long> valuesByAudience)
        {
            if (valuesByAudience == null || valuesByAudience.Count == C.ZeroIndex)
            {
                return false;
            }

            foreach (KeyValuePair<string, long> entry in valuesByAudience)
            {
                if (entry.Value != C.LongZero)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Builds cohort values from live single sales data as fallback when payload summaries are stale/empty.
        /// </summary>
        private static Dictionary<string, long> BuildSingleAudienceSegmentValuesFromLiveSingle(singles._single single, bool includeSales)
        {
            Dictionary<string, long> valuesByAudience = new Dictionary<string, long>(StringComparer.Ordinal);
            if (single == null)
            {
                return valuesByAudience;
            }

            resources.fanType[] genders = { resources.fanType.male, resources.fanType.female };
            resources.fanType[] hardcorenessValues = { resources.fanType.casual, resources.fanType.hardcore };
            resources.fanType[] ages = { resources.fanType.teen, resources.fanType.youngAdult, resources.fanType.adult };

            for (int genderIndex = C.ZeroIndex; genderIndex < genders.Length; genderIndex++)
            {
                resources.fanType gender = genders[genderIndex];
                for (int hardcorenessIndex = C.ZeroIndex; hardcorenessIndex < hardcorenessValues.Length; hardcorenessIndex++)
                {
                    resources.fanType hardcoreness = hardcorenessValues[hardcorenessIndex];
                    for (int ageIndex = C.ZeroIndex; ageIndex < ages.Length; ageIndex++)
                    {
                        resources.fanType age = ages[ageIndex];
                        singles._single._sales sale = single.GetFanSales(gender, hardcoreness, age);
                        if (sale == null)
                        {
                            continue;
                        }

                        string normalizedGender = NormalizeAudienceDimension(gender.ToString(), C.ZeroIndex);
                        string normalizedHardcoreness = NormalizeAudienceDimension(hardcoreness.ToString(), C.LastFromCount);
                        string normalizedAge = NormalizeAudienceDimension(age.ToString(), 2);
                        if (string.IsNullOrEmpty(normalizedGender) ||
                            string.IsNullOrEmpty(normalizedHardcoreness) ||
                            string.IsNullOrEmpty(normalizedAge))
                        {
                            continue;
                        }

                        long metricValue = includeSales ? sale.sales : sale.new_fans;
                        valuesByAudience[BuildSingleAudienceSegmentKey(normalizedGender, normalizedHardcoreness, normalizedAge)] = metricValue;
                    }
                }
            }

            return valuesByAudience;
        }

        /// <summary>
        /// Builds cohort values from fan-appeal axes when detailed segment payload fields are empty.
        /// </summary>
        private static Dictionary<string, long> BuildSingleAudienceSegmentValuesFromFanAppeal(JSONNode payload, long totalMetric)
        {
            Dictionary<string, long> valuesByAudience = new Dictionary<string, long>(StringComparer.Ordinal);
            if (payload == null || totalMetric <= C.LongZero)
            {
                return valuesByAudience;
            }

            float maleWeight = ResolveSingleAudienceWeight(payload, C.JsonSingleFanAppealMale);
            float femaleWeight = ResolveSingleAudienceWeight(payload, C.JsonSingleFanAppealFemale);
            float casualWeight = ResolveSingleAudienceWeight(payload, C.JsonSingleFanAppealCasual);
            float hardcoreWeight = ResolveSingleAudienceWeight(payload, C.JsonSingleFanAppealHardcore);
            float teenWeight = ResolveSingleAudienceWeight(payload, C.JsonSingleFanAppealTeen);
            float youngAdultWeight = ResolveSingleAudienceWeight(payload, C.JsonSingleFanAppealYoungAdult);
            float adultWeight = ResolveSingleAudienceWeight(payload, C.JsonSingleFanAppealAdult);

            if (maleWeight + femaleWeight <= Mathf.Epsilon)
            {
                maleWeight = C.FloatOne;
                femaleWeight = C.FloatOne;
            }

            if (casualWeight + hardcoreWeight <= Mathf.Epsilon)
            {
                casualWeight = C.FloatOne;
                hardcoreWeight = C.FloatOne;
            }

            if (teenWeight + youngAdultWeight + adultWeight <= Mathf.Epsilon)
            {
                teenWeight = C.FloatOne;
                youngAdultWeight = C.FloatOne;
                adultWeight = C.FloatOne;
            }

            string[] genders = { "male", "female" };
            string[] intensities = { "casual", "hardcore" };
            string[] ages = { "teen", "youngadult", "adult" };
            float[] genderWeights = { maleWeight, femaleWeight };
            float[] intensityWeights = { casualWeight, hardcoreWeight };
            float[] ageWeights = { teenWeight, youngAdultWeight, adultWeight };

            List<string> orderedKeys = new List<string>();
            List<double> fractionalRemainders = new List<double>();
            List<long> baseValues = new List<long>();
            double totalWeight = 0d;

            for (int genderIndex = C.ZeroIndex; genderIndex < genders.Length; genderIndex++)
            {
                for (int intensityIndex = C.ZeroIndex; intensityIndex < intensities.Length; intensityIndex++)
                {
                    for (int ageIndex = C.ZeroIndex; ageIndex < ages.Length; ageIndex++)
                    {
                        orderedKeys.Add(BuildSingleAudienceSegmentKey(genders[genderIndex], intensities[intensityIndex], ages[ageIndex]));
                        double segmentWeight = genderWeights[genderIndex] * intensityWeights[intensityIndex] * ageWeights[ageIndex];
                        if (segmentWeight < 0d)
                        {
                            segmentWeight = 0d;
                        }

                        fractionalRemainders.Add(segmentWeight);
                        totalWeight += segmentWeight;
                    }
                }
            }

            if (orderedKeys.Count == C.ZeroIndex)
            {
                return valuesByAudience;
            }

            if (totalWeight <= double.Epsilon)
            {
                totalWeight = orderedKeys.Count;
                for (int i = C.ZeroIndex; i < fractionalRemainders.Count; i++)
                {
                    fractionalRemainders[i] = 1d;
                }
            }

            long allocated = C.LongZero;
            for (int i = C.ZeroIndex; i < orderedKeys.Count; i++)
            {
                double exactValue = (totalMetric * fractionalRemainders[i]) / totalWeight;
                long baseValue = (long)Math.Floor(exactValue);
                if (baseValue < C.LongZero)
                {
                    baseValue = C.LongZero;
                }

                baseValues.Add(baseValue);
                fractionalRemainders[i] = exactValue - baseValue;
                allocated += baseValue;
            }

            long remaining = totalMetric - allocated;
            while (remaining > C.LongZero)
            {
                int bestIndex = C.InvalidId;
                double bestFraction = -1d;
                for (int i = C.ZeroIndex; i < fractionalRemainders.Count; i++)
                {
                    if (fractionalRemainders[i] <= bestFraction)
                    {
                        continue;
                    }

                    bestFraction = fractionalRemainders[i];
                    bestIndex = i;
                }

                if (bestIndex < C.ZeroIndex)
                {
                    bestIndex = C.ZeroIndex;
                }

                baseValues[bestIndex] += C.LastFromCount;
                fractionalRemainders[bestIndex] = -1d;
                remaining--;
            }

            for (int i = C.ZeroIndex; i < orderedKeys.Count; i++)
            {
                valuesByAudience[orderedKeys[i]] = baseValues[i];
            }

            return valuesByAudience;
        }

        /// <summary>
        /// Reads one fan-appeal weight for synthetic cohort fallback.
        /// </summary>
        private static float ResolveSingleAudienceWeight(JSONNode payload, string field)
        {
            float value;
            if (!TryReadSingleFloatField(payload, field, out value))
            {
                return C.FloatOne;
            }

            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return C.FloatOne;
            }

            return Mathf.Max(C.FloatZero, Mathf.Abs(value));
        }

        /// <summary>
        /// Parses one audience segment key into canonical gender/intensity/age dimensions.
        /// </summary>
        private static bool TryParseSingleAudienceSegmentKey(string segmentKey, out string gender, out string intensity, out string age)
        {
            gender = string.Empty;
            intensity = string.Empty;
            age = string.Empty;

            string normalizedKey = NormalizeRawText(segmentKey);
            if (normalizedKey == C.LabelUnknown)
            {
                return false;
            }

            string[] dimensions = normalizedKey.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (dimensions.Length < 3)
            {
                dimensions = normalizedKey.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            }

            for (int i = C.ZeroIndex; i < dimensions.Length; i++)
            {
                string token = dimensions[i];
                if (string.IsNullOrEmpty(gender))
                {
                    gender = NormalizeAudienceDimension(token, C.ZeroIndex);
                }

                if (string.IsNullOrEmpty(intensity))
                {
                    intensity = NormalizeAudienceDimension(token, C.LastFromCount);
                }

                if (string.IsNullOrEmpty(age))
                {
                    age = NormalizeAudienceDimension(token, 2);
                }
            }

            if ((string.IsNullOrEmpty(gender) || string.IsNullOrEmpty(intensity) || string.IsNullOrEmpty(age))
                && dimensions.Length >= 3)
            {
                if (string.IsNullOrEmpty(gender))
                {
                    gender = NormalizeAudienceDimension(dimensions[C.ZeroIndex], C.ZeroIndex);
                }

                if (string.IsNullOrEmpty(intensity))
                {
                    intensity = NormalizeAudienceDimension(dimensions[C.LastFromCount], C.LastFromCount);
                }

                if (string.IsNullOrEmpty(age))
                {
                    age = NormalizeAudienceDimension(dimensions[2], 2);
                }
            }

            return !string.IsNullOrEmpty(gender) && !string.IsNullOrEmpty(intensity) && !string.IsNullOrEmpty(age);
        }

        /// <summary>
        /// Normalizes one cohort dimension token into canonical keys.
        /// </summary>
        private static string NormalizeAudienceDimension(string rawToken, int dimensionIndex)
        {
            string token = (rawToken ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Replace(C.SeparatorUnderscore, string.Empty)
                .Replace(C.SeparatorDash, string.Empty)
                .Replace(C.SeparatorSpace, string.Empty);
            if (token.Length == C.ZeroIndex)
            {
                return string.Empty;
            }

            if (dimensionIndex == C.ZeroIndex)
            {
                if (token.Contains("female"))
                {
                    return "female";
                }

                if (token.Contains("male"))
                {
                    return "male";
                }

                return string.Empty;
            }

            if (dimensionIndex == C.LastFromCount)
            {
                if (token.Contains("hardcore"))
                {
                    return "hardcore";
                }

                if (token.Contains("casual"))
                {
                    return "casual";
                }

                return string.Empty;
            }

            if (token.Contains("youngadult"))
            {
                return "youngadult";
            }

            if (token.Contains("teen"))
            {
                return "teen";
            }

            if (token.Contains("adult"))
            {
                return "adult";
            }

            return string.Empty;
        }

        /// <summary>
        /// Builds canonical dictionary key for one audience cohort.
        /// </summary>
        private static string BuildSingleAudienceSegmentKey(string gender, string intensity, string age)
        {
            return string.Concat(gender, C.SeparatorPipe, intensity, C.SeparatorPipe, age);
        }

        /// <summary>
        /// Resolves senbatsu stats from payload snapshot first, then current single fallback.
        /// </summary>
        private static bool TryResolveSingleParticipationSenbatsuStats(
            singles._single single,
            JSONNode payload,
            out List<data_girls.girls.param> stats)
        {
            stats = new List<data_girls.girls.param>();
            if (payload != null && TryReadSingleSenbatsuSnapshotStats(payload, out stats) && stats.Count > C.ZeroIndex)
            {
                return true;
            }

            if (single == null)
            {
                return false;
            }

            List<data_girls.girls> girls = BuildSingleSenbatsuGirls(single);
            if (girls.Count == C.ZeroIndex)
            {
                return false;
            }

            List<data_girls.girls.param> fallbackStats = single.GetSenbatsuStats(girls, single.GetGroup());
            if (fallbackStats == null || fallbackStats.Count == C.ZeroIndex)
            {
                return false;
            }

            stats = fallbackStats;
            return true;
        }

        /// <summary>
        /// Formats one senbatsu stat value with integer preference and invariant decimals.
        /// </summary>
        private static string FormatSingleSenbatsuStatValue(float value)
        {
            return FormatSingleMetricValue(value, C.SingleMetricPercentMax);
        }

        /// <summary>
        /// Formats one senbatsu fame value (C.MinId-C.ActionRowTopPadding range).
        /// </summary>
        private static string FormatSingleSenbatsuFameValue(float value)
        {
            return FormatSingleMetricValue(value, C.SingleMetricScoreMax);
        }

        /// <summary>
        /// Formats one bounded metric value with integer preference and invariant decimals.
        /// </summary>
        private static string FormatSingleMetricValue(float value, float maxValue)
        {
            float clamped = Mathf.Clamp(value, C.FloatZero, maxValue);
            float rounded = Mathf.Round(clamped);
            if (Mathf.Abs(clamped - rounded) <= C.SingleMetricRoundingTolerance)
            {
                return ((int)rounded).ToString(CultureInfo.InvariantCulture);
            }

            return clamped.ToString(C.FormatSingleMetricOneDecimal, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Adds one compact text line inside single senbatsu card.
        /// </summary>
        private GameObject AddCardText(Transform parent, string text, bool primaryLine)
        {
            if (parent == null)
            {
                return null;
            }

            GameObject textObject = null;
            if (profile != null && profile.prefab_text != null)
            {
                textObject = UnityEngine.Object.Instantiate(profile.prefab_text, parent, false);
            }
            else
            {
                textObject = CreateUiObject(C.UiNameCareerDiarySingleCardTextPrefix + Guid.NewGuid().ToString(C.FormatGuidCompact, CultureInfo.InvariantCulture), parent);
                Text legacy = textObject.AddComponent<Text>();
                legacy.color = mainScript.green32;
            }

            SetText(textObject, text);
            LayoutElement lineLayout = textObject.GetComponent<LayoutElement>();
            if (lineLayout == null)
            {
                lineLayout = textObject.AddComponent<LayoutElement>();
            }

            lineLayout.preferredHeight = C.SingleSenbatsuCardTextLineHeight;
            lineLayout.flexibleWidth = C.FloatOne;

            TextMeshProUGUI[] tmps = textObject.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = C.ZeroIndex; i < tmps.Length; i++)
            {
                if (tmps[i] != null)
                {
                    tmps[i].enableWordWrapping = true;
                    tmps[i].alignment = TextAlignmentOptions.Center;
                    tmps[i].fontSize = primaryLine ? C.SingleCardNameFontSize : C.SingleCardMetaFontSize;
                }
            }

            Text[] texts = textObject.GetComponentsInChildren<Text>(true);
            for (int i = C.ZeroIndex; i < texts.Length; i++)
            {
                if (texts[i] != null)
                {
                    texts[i].alignment = TextAnchor.MiddleCenter;
                    texts[i].fontSize = primaryLine ? C.SingleCardNameFontSize : C.SingleCardMetaFontSize;
                }
            }

            return textObject;
        }

        /// <summary>
        /// Opens base-game single formation popup in read-only viewer mode.
        /// </summary>
        private void OpenSingleSenbatsuForSingle(singles._single single, JSONNode payload)
        {
            if (single == null)
            {
                interactionMessage = C.LabelSingleNotFound;
                if (diaryDetailPopupObject != null)
                {
                    diaryDetailPopupObject.SetActive(false);
                }

                if (diaryPanelObject != null)
                {
                    diaryPanelObject.SetActive(true);
                }

                RenderDiary();
                return;
            }

            mainScript main;
            PopupManager popup;
            if (!TryGetMainAndPopup(out main, out popup))
            {
                interactionMessage = C.LabelMainSystemsUnavailable;
                if (diaryDetailPopupObject != null)
                {
                    diaryDetailPopupObject.SetActive(false);
                }

                if (diaryPanelObject != null)
                {
                    diaryPanelObject.SetActive(true);
                }

                RenderDiary();
                return;
            }

            PopupManager._popup senbatsuPopupEntry = popup.GetByType(PopupManager._type.single_senbatsu);
            if (senbatsuPopupEntry == null || senbatsuPopupEntry.obj == null)
            {
                interactionMessage = C.LabelSingleFormationPopupUnavailable;
                if (diaryDetailPopupObject != null)
                {
                    diaryDetailPopupObject.SetActive(false);
                }

                if (diaryPanelObject != null)
                {
                    diaryPanelObject.SetActive(true);
                }

                RenderDiary();
                return;
            }

            SinglePopup_Senbatsu senbatsuPopup = senbatsuPopupEntry.obj.GetComponent<SinglePopup_Senbatsu>();
            if (senbatsuPopup == null)
            {
                interactionMessage = C.LabelSingleFormationComponentUnavailable;
                if (diaryDetailPopupObject != null)
                {
                    diaryDetailPopupObject.SetActive(false);
                }

                if (diaryPanelObject != null)
                {
                    diaryPanelObject.SetActive(true);
                }

                RenderDiary();
                return;
            }

            bool opened = OpenPopupReplacingCurrent(
                popup,
                PopupManager._type.single_senbatsu,
                delegate
                {
                    senbatsuPopup.SetSingle(single, false);
                    ApplySingleSenbatsuSnapshotIfAvailable(senbatsuPopup, single, payload);
                    ConfigureSingleSenbatsuViewerMode(senbatsuPopup);
                });

            if (!opened)
            {
                interactionMessage = C.LabelPopupOpenTransitionFailed;
                if (diaryDetailPopupObject != null)
                {
                    diaryDetailPopupObject.SetActive(false);
                }

                if (diaryPanelObject != null)
                {
                    diaryPanelObject.SetActive(true);
                }

                RenderDiary();
                return;
            }

            interactionMessage = C.LabelSingleFormationOpened;
            HideDiary(false);
        }

        /// <summary>
        /// Configures native senbatsu popup for diary viewing only.
        /// </summary>
        private static void ConfigureSingleSenbatsuViewerMode(SinglePopup_Senbatsu popup)
        {
            if (popup == null)
            {
                return;
            }

            if (popup.Continue != null)
            {
                DiarySingleSenbatsuViewerModeTracker tracker = popup.gameObject.GetComponent<DiarySingleSenbatsuViewerModeTracker>();
                if (tracker == null)
                {
                    tracker = popup.gameObject.AddComponent<DiarySingleSenbatsuViewerModeTracker>();
                }

                tracker.HideContinueTemporarily(popup.Continue);
            }

            if (popup.Button_Cancel != null)
            {
                ButtonDefault cancelDefault = popup.Button_Cancel.GetComponent<ButtonDefault>();
                if (cancelDefault != null)
                {
                    string closeLabel = GetLocalizedCloseLabel();
                    cancelDefault.SetText(closeLabel);
                    cancelDefault.DefaultTooltip = closeLabel;
                    cancelDefault.SetTooltip(closeLabel);
                    cancelDefault.Activate(true, false);
                }
            }
        }

        /// <summary>
        /// Applies release-time senbatsu stat snapshots to base formation popup when payload data is available.
        /// </summary>
        private static void ApplySingleSenbatsuSnapshotIfAvailable(SinglePopup_Senbatsu popup, singles._single single, JSONNode payload)
        {
            if (popup == null || single == null || payload == null)
            {
                return;
            }

            List<data_girls.girls.param> snapshotStats;
            if (!TryReadSingleSenbatsuSnapshotStats(payload, out snapshotStats))
            {
                return;
            }

            List<data_girls.girls> senbatsuGirls = BuildSingleSenbatsuGirls(single);
            if (senbatsuGirls.Count == C.ZeroIndex)
            {
                return;
            }

            popup.SetStats(senbatsuGirls, snapshotStats);

            float fameSnapshot = ReadFloat(payload, C.JsonSingleFameOfSenbatsu);
            if (!float.IsNaN(fameSnapshot) && !float.IsInfinity(fameSnapshot) && fameSnapshot >= C.FloatZero && popup.fame != null)
            {
                Stars stars = popup.fame.GetComponent<Stars>();
                if (stars != null)
                {
                    stars.SetValue_Partial(fameSnapshot);
                }
            }
        }

        /// <summary>
        /// Parses serialized senbatsu stat snapshot payload into concrete parameter rows.
        /// </summary>
        private static bool TryReadSingleSenbatsuSnapshotStats(JSONNode payload, out List<data_girls.girls.param> stats)
        {
            stats = new List<data_girls.girls.param>();
            string serializedStats = ReadStr(payload, C.JsonSingleSenbatsuStatsSnapshot);
            if (string.IsNullOrEmpty(serializedStats))
            {
                return false;
            }

            string[] entries = serializedStats.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = C.ZeroIndex; i < entries.Length; i++)
            {
                string entry = entries[i];
                if (string.IsNullOrEmpty(entry))
                {
                    continue;
                }

                int separatorIndex = entry.IndexOf(':');
                if (separatorIndex <= C.ZeroIndex || separatorIndex >= entry.Length - C.LastFromCount)
                {
                    continue;
                }

                string parameterCode = entry.Substring(C.ZeroIndex, separatorIndex).Trim();
                string rawValue = entry.Substring(separatorIndex + C.LastFromCount).Trim();

                data_girls._paramType parameterType;
                if (!TryParseSnapshotParamType(parameterCode, out parameterType))
                {
                    continue;
                }

                float parameterValue;
                if (!float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out parameterValue))
                {
                    continue;
                }

                data_girls.girls.param parameter = new data_girls.girls.param();
                parameter.type = parameterType;
                parameter.val = parameterValue;
                stats.Add(parameter);
            }

            return stats.Count > C.ZeroIndex;
        }

        /// <summary>
        /// Resolves one parameter enum from serialized snapshot token.
        /// </summary>
        private static bool TryParseSnapshotParamType(string parameterCode, out data_girls._paramType parameterType)
        {
            if (Enum.TryParse(parameterCode ?? string.Empty, true, out parameterType))
            {
                return true;
            }

            string normalizedInput = (parameterCode ?? string.Empty)
                .Replace(C.SeparatorUnderscore, string.Empty)
                .Replace(C.SeparatorDash, string.Empty)
                .Replace(C.SeparatorSpace, string.Empty);
            if (normalizedInput.Length == C.ZeroIndex)
            {
                parameterType = data_girls._paramType.scandal;
                return false;
            }

            Array values = Enum.GetValues(typeof(data_girls._paramType));
            for (int i = C.ZeroIndex; i < values.Length; i++)
            {
                data_girls._paramType candidate = (data_girls._paramType)values.GetValue(i);
                string normalizedCandidate = candidate
                    .ToString()
                    .Replace(C.SeparatorUnderscore, string.Empty)
                    .Replace(C.SeparatorDash, string.Empty)
                    .Replace(C.SeparatorSpace, string.Empty);

                if (string.Equals(normalizedCandidate, normalizedInput, StringComparison.OrdinalIgnoreCase))
                {
                    parameterType = candidate;
                    return true;
                }
            }

            parameterType = data_girls._paramType.scandal;
            return false;
        }

        /// <summary>
        /// Builds clean senbatsu girl list for popup stat rendering.
        /// </summary>
        private static List<data_girls.girls> BuildSingleSenbatsuGirls(singles._single single)
        {
            List<data_girls.girls> girls = new List<data_girls.girls>();
            if (single == null || single.girls == null)
            {
                return girls;
            }

            for (int i = C.ZeroIndex; i < single.girls.Count; i++)
            {
                data_girls.girls girl = single.girls[i];
                if (girl != null)
                {
                    girls.Add(girl);
                }
            }

            return girls;
        }

        /// <summary>
        /// Opens single chart popup at month containing selected player single.
        /// </summary>
        private void OpenSingleChartForSingle(singles._single single)
        {
            if (single == null)
            {
                interactionMessage = C.LabelSingleNotFound;
                if (diaryDetailPopupObject != null)
                {
                    diaryDetailPopupObject.SetActive(false);
                }

                if (diaryPanelObject != null)
                {
                    diaryPanelObject.SetActive(true);
                }

                RenderDiary();
                return;
            }

            mainScript main;
            PopupManager popup;
            if (!TryGetMainAndPopup(out main, out popup))
            {
                interactionMessage = C.LabelMainSystemsUnavailable;
                if (diaryDetailPopupObject != null)
                {
                    diaryDetailPopupObject.SetActive(false);
                }

                if (diaryPanelObject != null)
                {
                    diaryPanelObject.SetActive(true);
                }

                RenderDiary();
                return;
            }

            PopupManager._popup chartPopupEntry = popup.GetByType(PopupManager._type.single_chart);
            if (chartPopupEntry == null || chartPopupEntry.obj == null)
            {
                interactionMessage = C.LabelSingleChartPopupUnavailable;
                if (diaryDetailPopupObject != null)
                {
                    diaryDetailPopupObject.SetActive(false);
                }

                if (diaryPanelObject != null)
                {
                    diaryPanelObject.SetActive(true);
                }

                RenderDiary();
                return;
            }

            Chart_Popup chartPopup = chartPopupEntry.obj.GetComponent<Chart_Popup>();
            if (chartPopup == null)
            {
                interactionMessage = C.LabelSingleChartComponentUnavailable;
                if (diaryDetailPopupObject != null)
                {
                    diaryDetailPopupObject.SetActive(false);
                }

                if (diaryPanelObject != null)
                {
                    diaryPanelObject.SetActive(true);
                }

                RenderDiary();
                return;
            }

            int monthId;
            if (!TryResolveChartMonthIdForSingle(single, out monthId))
            {
                interactionMessage = C.LabelSingleChartMonthUnknown;
                if (diaryDetailPopupObject != null)
                {
                    diaryDetailPopupObject.SetActive(false);
                }

                if (diaryPanelObject != null)
                {
                    diaryPanelObject.SetActive(true);
                }

                RenderDiary();
                return;
            }

            bool opened = OpenPopupReplacingCurrent(
                popup,
                PopupManager._type.single_chart,
                delegate
                {
                    chartPopup.MonthID = monthId;
                    TryRefreshChartPopup(chartPopup);
                });

            if (!opened)
            {
                interactionMessage = C.LabelPopupOpenTransitionFailed;
                if (diaryDetailPopupObject != null)
                {
                    diaryDetailPopupObject.SetActive(false);
                }

                if (diaryPanelObject != null)
                {
                    diaryPanelObject.SetActive(true);
                }

                RenderDiary();
                return;
            }

            interactionMessage = C.LabelSingleChartOpened;
            HideDiary(false);
        }

        /// <summary>
        /// Closes current popup context once and opens target popup after close animation settles.
        /// </summary>
        private static bool OpenPopupReplacingCurrent(PopupManager popup, PopupManager._type targetType, Action configureAfterOpen)
        {
            if (popup == null)
            {
                return false;
            }

            TryRepairStaleQueueBeforePopupReplace(popup);

            Action openTargetOnce = null;
            bool opened = false;
            openTargetOnce = delegate
            {
                if (opened)
                {
                    return;
                }

                popup.Open(targetType, true);
                PopupManager._popup targetEntry = popup.GetByType(targetType);
                bool active = targetEntry != null && targetEntry.obj != null && targetEntry.obj.activeInHierarchy;
                if (!active)
                {
                    return;
                }

                if (configureAfterOpen != null)
                {
                    configureAfterOpen();
                }

                opened = true;
            };

            bool hasVisiblePopup = HasVisibleManagedPopup(popup, true);
            bool shouldCloseFirst = hasVisiblePopup || popup.IsThereAnOpenPopup() || (popup.queue != null && popup.queue.Count > C.ZeroIndex);
            if (!shouldCloseFirst)
            {
                openTargetOnce();
                return opened;
            }

            popup.Close(openTargetOnce);
            TryRepairStaleQueueBeforePopupReplace(popup);

            if (!opened && !popup.IsThereAnOpenPopup() && (popup.queue == null || popup.queue.Count == C.ZeroIndex))
            {
                openTargetOnce();
            }

            if (opened)
            {
                TrySyncBackdropWithActiveManagedPopups(popup);
            }
            else
            {
                TryRunPopupBackdropSafetyNet(popup, true);
            }

            return opened;
        }

        /// <summary>
        /// Repairs queue-only stuck state before replacing popup contexts.
        /// </summary>
        private static void TryRepairStaleQueueBeforePopupReplace(PopupManager popup)
        {
            if (popup == null || popup.queue == null || popup.queue.Count <= C.ZeroIndex)
            {
                return;
            }

            bool hasVisiblePopup = HasVisibleManagedPopup(popup, true);
            bool hasManagedOpenOrActive = HasManagedPopupOpenOrActive(popup, true);
            if (!hasVisiblePopup && !hasManagedOpenOrActive)
            {
                popup.queue.Clear();
            }
        }

        /// <summary>
        /// Resolves chart popup month id for one player single.
        /// </summary>
        private static bool TryResolveChartMonthIdForSingle(singles._single single, out int monthId)
        {
            monthId = C.InvalidMonthId;
            if (single == null || Rivals.Date_To_Month == null)
            {
                return false;
            }

            for (int i = C.ZeroIndex; i < Rivals.Date_To_Month.Count; i++)
            {
                Rivals._date_to_month_id monthData = Rivals.Date_To_Month[i];
                if (monthData == null)
                {
                    continue;
                }

                List<Rivals._group._single> chartSingles = Rivals.GetSingles(monthData.ID);
                if (chartSingles == null)
                {
                    continue;
                }

                for (int j = C.ZeroIndex; j < chartSingles.Count; j++)
                {
                    Rivals._group._single chartSingle = chartSingles[j];
                    if (chartSingle != null && chartSingle.Player && chartSingle.SingleID == single.id)
                    {
                        monthId = monthData.ID;
                        return true;
                    }
                }
            }

            DateTime expectedChartMonthDate = DateTime.MinValue;
            bool hasExpectedChartMonth = single.ReleaseData != null && single.ReleaseData.ReleaseDate.Year > DateTime.MinValue.Year;
            if (hasExpectedChartMonth)
            {
                expectedChartMonthDate = single.ReleaseData.ReleaseDate.AddMonths(C.ReleaseDateChartMonthOffset);
            }

            for (int i = C.ZeroIndex; i < Rivals.Date_To_Month.Count; i++)
            {
                Rivals._date_to_month_id monthData = Rivals.Date_To_Month[i];
                if (hasExpectedChartMonth &&
                    monthData != null &&
                    monthData.Date.Year == expectedChartMonthDate.Year &&
                    monthData.Date.Month == expectedChartMonthDate.Month)
                {
                    monthId = monthData.ID;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Forces chart popup to refresh after month reassignment.
        /// </summary>
        private static bool TryRefreshChartPopup(Chart_Popup popup)
        {
            if (popup == null)
            {
                return false;
            }

            if (chartPopupRenderMethod == null)
            {
                chartPopupRenderMethod = typeof(Chart_Popup).GetMethod(
                    C.MethodChartPopupRender,
                    BindingFlags.Instance | BindingFlags.NonPublic);
            }

            if (chartPopupRenderMethod == null)
            {
                popup.gameObject.SendMessage(C.MethodChartPopupRender, null, SendMessageOptions.DontRequireReceiver);
                return false;
            }

            try
            {
                chartPopupRenderMethod.Invoke(popup, null);
                return true;
            }
            catch
            {
                popup.gameObject.SendMessage(C.MethodChartPopupRender, null, SendMessageOptions.DontRequireReceiver);
                return false;
            }
        }

        /// <summary>
        /// Attempts to open selected source entity.
        /// </summary>
        private void OpenSource(IMDataCoreEvent ev)
        {
            string message;
            if (TryOpenSource(ev, out message))
            {
                interactionMessage = C.TextSourceEntityOpened;
                HideDiary(false);
                return;
            }
            else
            {
                interactionMessage = C.TextUnableToOpenSourceEntity + (string.IsNullOrEmpty(message) ? C.LabelNoSource : message);
                if (diaryDetailPopupObject != null)
                {
                    diaryDetailPopupObject.SetActive(false);
                }

                if (diaryPanelObject != null)
                {
                    diaryPanelObject.SetActive(true);
                }
            }

            RenderDiary();
        }

        /// <summary>
        /// Opens source popup based on event entity kind.
        /// </summary>
        private bool TryOpenSource(IMDataCoreEvent ev, out string message)
        {
            message = string.Empty;
            if (ev == null)
            {
                message = C.TextEventIsNull;
                return false;
            }

            mainScript main;
            PopupManager popup;
            if (!TryGetMainAndPopup(out main, out popup))
            {
                message = C.LabelMainSystemsUnavailable;
                return false;
            }

            string kind = ev.EntityKind ?? string.Empty;
            JSONNode payload = ParsePayload(ev.PayloadJson);

            if (kind == C.KindRelationship || kind == C.KindDatingRelationship || kind == C.KindIdolDatingState)
            {
                int otherId = ResolveOtherRelationshipIdolId(ev, payload);
                if (otherId >= C.MinId && !ShouldRevealSocialParticipant(ev, payload, otherId))
                {
                    message = C.TextRelationshipUnknownToProducer;
                    return false;
                }
            }

            if (kind == C.KindBullying)
            {
                int targetId = ReadId(payload, C.JsonBullyingTargetId);
                int leaderId = ReadId(payload, C.JsonBullyingLeaderId);
                bool hiddenTarget = targetId >= C.MinId && !ShouldRevealSocialParticipant(ev, payload, targetId);
                bool hiddenLeader = leaderId >= C.MinId && !ShouldRevealSocialParticipant(ev, payload, leaderId);
                if (hiddenTarget || hiddenLeader)
                {
                    message = C.TextBullyingParticipantsUnknownToProducer;
                    return false;
                }
            }

            if (kind == C.KindDatingRelationship || kind == C.KindIdolDatingState)
            {
                int idolId = ResolveIdFromEvent(ev);
                if (idolId >= C.MinId && ShouldRevealSocialParticipant(ev, payload, idolId))
                {
                    return OpenIdolProfile(idolId);
                }

                message = C.TextRelationshipUnknownToProducer;
                return false;
            }

            if (kind == C.KindIdol || kind == C.KindStatus || kind == C.KindScandal || kind == C.KindMedical)
            {
                int idolId = ResolveIdFromEvent(ev);
                return OpenIdolProfile(idolId);
            }

            if (kind == C.KindSingle)
            {
                int singleId;
                if (!TryParseInt(ev.EntityId, out singleId))
                {
                    message = C.TextInvalidSingleId;
                    return false;
                }

                singles._single single = singles.GetSingleByID(singleId);
                if (single == null)
                {
                    message = C.TextSingleNotFound;
                    return false;
                }

                PopupManager._popup releasePopup = popup.GetByType(PopupManager._type.single_release);
                if (releasePopup == null || releasePopup.obj == null)
                {
                    message = C.TextSingleReleasePopupUnavailable;
                    return false;
                }

                Single_Release sr = releasePopup.obj.GetComponent<Single_Release>();
                if (sr == null)
                {
                    message = C.TextSingleReleaseComponentUnavailable;
                    return false;
                }

                bool opened = OpenPopupReplacingCurrent(
                    popup,
                    PopupManager._type.single_release,
                    delegate
                    {
                        sr.SetSingle(single);
                    });
                if (!opened)
                {
                    message = C.LabelPopupOpenTransitionFailed;
                    return false;
                }

                return true;
            }

            if (kind == C.KindShow)
            {
                int showId;
                if (!TryParseInt(ev.EntityId, out showId))
                {
                    message = C.TextInvalidShowId;
                    return false;
                }

                Shows._show show = Shows.GetShowByID(showId);
                if (show == null)
                {
                    message = C.TextShowNotFound;
                    return false;
                }

                PopupManager._popup releasePopup = popup.GetByType(PopupManager._type.single_release);
                if (releasePopup == null || releasePopup.obj == null)
                {
                    message = C.TextShowResultPopupUnavailable;
                    return false;
                }

                Single_Release sr = releasePopup.obj.GetComponent<Single_Release>();
                if (sr == null)
                {
                    message = C.TextShowResultComponentUnavailable;
                    return false;
                }

                bool opened = OpenPopupReplacingCurrent(
                    popup,
                    PopupManager._type.single_release,
                    delegate
                    {
                        sr.SetShow(show);
                    });
                if (!opened)
                {
                    message = C.LabelPopupOpenTransitionFailed;
                    return false;
                }

                return true;
            }

            if (kind == C.KindTour)
            {
                int tourId;
                if (!TryParseInt(ev.EntityId, out tourId))
                {
                    message = C.TextInvalidTourId;
                    return false;
                }

                SEvent_Tour.tour tour = SEvent_Tour.GetTourByID(tourId);
                if (tour == null)
                {
                    message = C.TextTourNotFound;
                    return false;
                }

                PopupManager._popup tourPopupEntry = popup.GetByType(PopupManager._type.sevent_tour);
                if (tourPopupEntry == null || tourPopupEntry.obj == null)
                {
                    message = C.TextTourPopupUnavailable;
                    return false;
                }

                Tour_Popup tourPopup = tourPopupEntry.obj.GetComponent<Tour_Popup>();
                if (tourPopup == null)
                {
                    message = C.TextTourComponentUnavailable;
                    return false;
                }

                bool opened = OpenPopupReplacingCurrent(
                    popup,
                    PopupManager._type.sevent_tour,
                    delegate
                    {
                        tourPopup.Reset();
                        tourPopup.SetTour(tour);
                    });
                if (!opened)
                {
                    message = C.LabelPopupOpenTransitionFailed;
                    return false;
                }

                return true;
            }

            if (kind == C.KindElection)
            {
                int electionId;
                if (!TryParseInt(ev.EntityId, out electionId))
                {
                    message = C.TextInvalidElectionId;
                    return false;
                }

                SEvent_SSK._SSK election = SEvent_SSK.GetSSKByID(electionId);
                if (election == null)
                {
                    message = C.TextElectionNotFound;
                    return false;
                }

                PopupManager._popup sskPopupEntry = popup.GetByType(PopupManager._type.sevent_SSK);
                if (sskPopupEntry == null || sskPopupEntry.obj == null)
                {
                    message = C.TextElectionPopupUnavailable;
                    return false;
                }

                SSK_Popup sskPopup = sskPopupEntry.obj.GetComponent<SSK_Popup>();
                if (sskPopup == null)
                {
                    message = C.TextElectionComponentUnavailable;
                    return false;
                }

                bool opened = OpenPopupReplacingCurrent(
                    popup,
                    PopupManager._type.sevent_SSK,
                    delegate
                    {
                        sskPopup.Reset();
                        sskPopup.StartSSK(election);
                    });
                if (!opened)
                {
                    message = C.LabelPopupOpenTransitionFailed;
                    return false;
                }

                return true;
            }

            message = C.LabelNoSource;
            return false;
        }

        /// <summary>
        /// Builds a display presentation from one core event.
        /// </summary>
        private Presentation BuildPresentation(IMDataCoreEvent ev)
        {
            Presentation p = new Presentation();
            if (ev == null)
            {
                p.Date = C.LabelUnknown;
                p.Title = C.LabelUnknown;
                p.WithWhom = C.LabelUnknown;
                p.Outcome = C.LabelUnknown;
                p.Source = C.LabelUnknown;
                p.ModSource = string.Empty;
                return p;
            }

            JSONNode payload = ParsePayload(ev.PayloadJson);
            p.Date = ResolveDate(ev);
            p.Source = ResolveSourceLabel(ev, payload);
            p.ModSource = ModInfoCatalog.ResolveEventSourceTitle(ev);
            p.PayloadPreview = BuildPayloadPreview(ev.PayloadJson);
            List<string> outcomeLines = new List<string>();

            string type = ev.EventType ?? string.Empty;
            CustomDiaryEntry customEntry;
            bool customEntryApplied = TryApplyCustomDiaryEntry(ev, payload, p, outcomeLines, out customEntry);

            if (customEntryApplied)
            {
                // Presentation supplied by custom diary JSON.
            }
            else if (type == C.EventSingleReleased || type == C.EventSingleParticipationRecorded)
            {
                p.Title = C.TextSingleReleased;
                p.WithWhom = ResolveSingleTitle(ev, payload);

                outcomeLines.Add(C.TextSenbatsuRow + (ReadInt(payload, C.JsonRowIndex) + C.LastFromCount).ToString(CultureInfo.InvariantCulture));
                outcomeLines.Add(C.TextPosition + (ReadInt(payload, C.JsonPositionIndex) + C.LastFromCount).ToString(CultureInfo.InvariantCulture));
                outcomeLines.Add(C.TextCenter + YesNo(ReadBool(payload, C.JsonIsCenter)));

                int chartPosition = ReadInt(payload, C.JsonChartPosition);
                if (chartPosition > C.UnknownChartPosition)
                {
                    outcomeLines.Add(C.TextChartPosition + chartPosition.ToString(CultureInfo.InvariantCulture));
                }
                else if (chartPosition == C.UnknownChartPosition)
                {
                    outcomeLines.Add(C.LabelSingleChartPrefix + C.LabelSingleDidNotChart);
                }

                long totalSales = ReadLong(payload, C.JsonTotalSales);
                if (totalSales > C.LongZero)
                {
                    outcomeLines.Add(C.LabelSingleSalesPrefix + totalSales.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
                }

                int newFans = ReadInt(payload, C.JsonNewFans);
                if (newFans != C.ZeroIndex)
                {
                    outcomeLines.Add(C.LabelSingleNewFansPrefix + FormatSignedNumber(newFans));
                }
            }
            else if (type == C.EventStatusChanged)
            {
                p.Title = C.TextIdolStatusUpdated;
                p.WithWhom = ResolveIdolName(idol);
                string transition = BuildStatusTransitionText(ReadStr(payload, C.JsonPrevStatus), ReadStr(payload, C.JsonNewStatus));
                if (!string.IsNullOrEmpty(transition))
                {
                    outcomeLines.Add(C.LabelStatusPrefix + transition);
                }
            }
            else if (type == C.EventDatingPartnerStatusChanged)
            {
                p.Title = C.TextDatingStatusUpdated;
                string visibleIdolName = ResolveVisibleSocialParticipantName(ev, payload, ev.IdolId);
                p.WithWhom = visibleIdolName != C.LabelUnknown ? visibleIdolName : C.LabelNotKnownToProducer;

                string transition = BuildStatusTransitionText(ReadStr(payload, C.JsonPrevStatus), ReadStr(payload, C.JsonNewStatus));
                if (!string.IsNullOrEmpty(transition))
                {
                    outcomeLines.Add(C.LabelStatusPrefix + transition);
                }

                string route = HumanizeUnknown(ReadStr(payload, C.JsonDatingRoute));
                if (route != C.LabelUnknown)
                {
                    outcomeLines.Add(C.TextRoute + route);
                }

                string stage = HumanizeUnknown(ReadStr(payload, C.JsonDatingRouteStage));
                if (stage != C.LabelUnknown)
                {
                    outcomeLines.Add(C.TextStage + stage);
                }
            }
            else if (type == C.EventIdolDatingStatusChanged)
            {
                p.Title = C.TextDatingPartnerStatusUpdated;
                string visibleIdolName = ResolveVisibleSocialParticipantName(ev, payload, ev.IdolId);
                p.WithWhom = visibleIdolName != C.LabelUnknown ? visibleIdolName : C.LabelNotKnownToProducer;

                string transition = BuildStatusTransitionText(ReadStr(payload, C.JsonPrevPartnerStatus), ReadStr(payload, C.JsonNewPartnerStatus));
                if (!string.IsNullOrEmpty(transition))
                {
                    outcomeLines.Add(C.LabelStatusPrefix + transition);
                }

                if (ReadBool(payload, C.JsonDatingDatedIdol))
                {
                    outcomeLines.Add(C.TextDatingHistoryHasDatedAnotherIdol);
                }

                if (ReadBool(payload, C.JsonDatingHadScandalEver))
                {
                    outcomeLines.Add(C.TextScandalHistoryYes);
                }

                if (ReadBool(payload, C.JsonDatingUsedGoods))
                {
                    outcomeLines.Add(C.TextUsedGoodsStatusYes);
                }
            }
            else if (type == C.EventIdolDatingStarted || type == C.EventIdolDatingEnded || type == C.EventIdolRelationshipStatusChanged)
            {
                int otherId = ResolveOtherRelationshipIdolId(ev, payload);
                string otherName = ResolveVisibleSocialParticipantName(ev, payload, otherId);
                p.Title = type == C.EventIdolDatingStarted
                    ? C.TextStartedDatingAnotherIdol
                    : (type == C.EventIdolDatingEnded ? C.TextDatingEnded : C.TextRelationshipStatusUpdated);
                p.WithWhom = otherName != C.LabelUnknown ? otherName : C.LabelNotKnownToProducer;

                string relationshipStatusTransition = BuildStatusTransitionText(
                    ReadStr(payload, C.JsonRelationshipPreviousStatus),
                    ReadStr(payload, C.JsonRelationshipNewStatus));
                if (!string.IsNullOrEmpty(relationshipStatusTransition))
                {
                    outcomeLines.Add(C.LabelStatusPrefix + relationshipStatusTransition);
                }

                string relationshipStatus = HumanizeUnknown(ReadStr(payload, C.JsonRelationshipStatus));
                if (relationshipStatus != C.LabelUnknown && string.IsNullOrEmpty(relationshipStatusTransition))
                {
                    outcomeLines.Add(C.LabelStatusPrefix + relationshipStatus);
                }

                string dynamicText = HumanizeUnknown(ReadStr(payload, C.JsonRelationshipDynamic));
                if (dynamicText != C.LabelUnknown)
                {
                    outcomeLines.Add(C.TextDynamic + dynamicText);
                }

                if (ReadBool(payload, C.JsonRelationshipIsDating))
                {
                    outcomeLines.Add(C.TextPairStateDating);
                }

                float relationshipRatio = ReadFloat(payload, C.JsonRelationshipRatio);
                if (Mathf.Abs(relationshipRatio) > Mathf.Epsilon)
                {
                    outcomeLines.Add(C.TextRelationshipScore + relationshipRatio.ToString(C.FormatSingleMetricTwoDecimals, CultureInfo.InvariantCulture));
                }
            }
            else if (type == C.EventBullyingStarted || type == C.EventBullyingEnded)
            {
                int targetId = ReadId(payload, C.JsonBullyingTargetId);
                int leaderId = ReadId(payload, C.JsonBullyingLeaderId);
                string targetName = ResolveVisibleSocialParticipantName(ev, payload, targetId);
                string leaderName = ResolveVisibleSocialParticipantName(ev, payload, leaderId);

                p.Title = type == C.EventBullyingStarted ? C.TextBullyingStarted : C.TextBullyingEnded;
                p.WithWhom = targetName;
                if (p.WithWhom == C.LabelUnknown)
                {
                    p.WithWhom = leaderName;
                }

                if (p.WithWhom == C.LabelUnknown)
                {
                    p.WithWhom = C.LabelNotKnownToProducer;
                }

                if (targetName != C.LabelUnknown)
                {
                    outcomeLines.Add(C.TextTargetIdol + targetName);
                }

                if (leaderName != C.LabelUnknown)
                {
                    outcomeLines.Add(C.TextLeaderIdol + leaderName);
                }

                outcomeLines.Add(C.TextKnownToProducer + YesNo(ReadBool(payload, C.JsonBullyingKnownToPlayer)));
            }
            else if (type == C.EventContractActivated ||
                     type == C.EventContractCancelled ||
                     type == C.EventContractBroken ||
                     type == C.EventContractFinished)
            {
                int contractIdolId = ResolveIdFromEvent(ev);
                int payloadIdolId = ReadId(payload, C.JsonIdolId);
                if (payloadIdolId >= C.MinId)
                {
                    contractIdolId = payloadIdolId;
                }

                string contractIdolName = ResolveIdolNameById(contractIdolId);
                string agentName = HumanizeUnknown(ReadStr(payload, C.JsonContractAgentName));
                p.Title = type == C.EventContractActivated
                    ? C.TextContractSigned
                    : (type == C.EventContractCancelled
                        ? C.TextContractCancelled
                        : (type == C.EventContractBroken ? C.TextContractBroken : C.TextContractCompleted));
                p.WithWhom = contractIdolName != C.LabelUnknown ? contractIdolName : (C.TextAgent + agentName);

                string productName = HumanizeUnknown(ReadStr(payload, C.JsonContractProductName));
                if (productName != C.LabelUnknown)
                {
                    outcomeLines.Add(C.TextProduct + productName);
                }

                if (agentName != C.LabelUnknown)
                {
                    outcomeLines.Add(C.TextAgency + agentName);
                }

                string contractType = HumanizeUnknown(ReadStr(payload, C.JsonContractType));
                if (contractType != C.LabelUnknown)
                {
                    outcomeLines.Add(C.TextContractTypePrefix + contractType);
                }

                string contractSkill = HumanizeUnknown(ReadStr(payload, C.JsonContractSkill));
                if (contractSkill != C.LabelUnknown)
                {
                    outcomeLines.Add(C.TextContractFocusPrefix + contractSkill);
                }

                if (HasPayloadValue(payload, C.JsonContractIsGroup))
                {
                    outcomeLines.Add(BuildContractScopeText(ReadBool(payload, C.JsonContractIsGroup)));
                }

                string startDate = FormatRoundTripDateForUi(ReadStr(payload, C.JsonContractStartDate));
                if (startDate != C.LabelUnknown)
                {
                    outcomeLines.Add(C.TextStartDatePrefix + startDate);
                }

                string endDate = FormatRoundTripDateForUi(ReadStr(payload, C.JsonContractEndDate));
                if (endDate != C.LabelUnknown)
                {
                    outcomeLines.Add(C.TextEndDatePrefix + endDate);
                }

                int durationMonths = ReadInt(payload, C.JsonContractDurationMonths);
                if (durationMonths > C.ZeroIndex)
                {
                    outcomeLines.Add(C.TextDuration + durationMonths.ToString(CultureInfo.InvariantCulture) + C.TextMonthS);
                }

                if (ReadBool(payload, C.JsonContractIsImmediate))
                {
                    outcomeLines.Add(C.TextActivationImmediate);
                }

                int weeklyPayment = ReadInt(payload, C.JsonContractWeeklyPayment);
                if (weeklyPayment != C.ZeroIndex)
                {
                    outcomeLines.Add(C.TextWeeklyPayPrefix + FormatSignedNumber(weeklyPayment));
                }

                int weeklyBuzz = ReadInt(payload, C.JsonContractWeeklyBuzz);
                if (weeklyBuzz != C.ZeroIndex)
                {
                    outcomeLines.Add(C.TextWeeklyBuzzPrefix + FormatSignedNumber(weeklyBuzz));
                }

                int weeklyFame = ReadInt(payload, C.JsonContractWeeklyFame);
                if (weeklyFame != C.ZeroIndex)
                {
                    outcomeLines.Add(C.TextWeeklyFamePrefix + FormatSignedNumber(weeklyFame));
                }

                int weeklyFans = ReadInt(payload, C.JsonContractWeeklyFans);
                if (weeklyFans != C.ZeroIndex)
                {
                    outcomeLines.Add(C.TextWeeklyFansPrefix + FormatSignedNumber(weeklyFans));
                }

                int weeklyStamina = ReadInt(payload, C.JsonContractWeeklyStamina);
                if (weeklyStamina != C.ZeroIndex)
                {
                    outcomeLines.Add(C.TextWeeklyStaminaPrefix + FormatSignedNumber(weeklyStamina));
                }

                long liability = ReadLong(payload, C.JsonContractLiability);
                if (liability > C.LongZero)
                {
                    outcomeLines.Add(C.TextLiability + liability.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
                }

                long brokenLiability = ReadLong(payload, C.JsonContractTotalBrokenLiability);
                if (brokenLiability > C.LongZero)
                {
                    outcomeLines.Add(C.TextDamagesPaid + brokenLiability.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
                }

                string breakContext = HumanizeUnknown(ReadStr(payload, C.JsonContractBreakContext));
                if (breakContext != C.LabelUnknown)
                {
                    outcomeLines.Add(C.TextBreakContext + breakContext);
                }

                if (ReadBool(payload, C.JsonContractDamagesApplied))
                {
                    outcomeLines.Add(C.TextDamagesAppliedYes);
                }

                if (type == C.EventContractFinished)
                {
                    outcomeLines.Add(C.TextCompletionContractReachedItsEndDate);
                }
            }
            else if (type == C.EventContractWeeklyEarningsApplied || type == C.EventContractWeeklyBenefitsApplied)
            {
                bool isBenefitsEvent = string.Equals(type, C.EventContractWeeklyBenefitsApplied, StringComparison.Ordinal);
                int contractIdolId = ResolveIdFromEvent(ev);
                int payloadIdolId = ReadId(payload, C.JsonIdolId);
                if (payloadIdolId >= C.MinId)
                {
                    contractIdolId = payloadIdolId;
                }

                p.Title = isBenefitsEvent ? C.TextWeeklyContractBenefitsApplied : C.TextWeeklyContractPaymentReceived;
                p.WithWhom = ResolveIdolNameById(contractIdolId);

                string contractType = HumanizeUnknown(ReadStr(payload, C.JsonContractType));
                if (contractType != C.LabelUnknown)
                {
                    outcomeLines.Add(C.TextContractTypePrefix + contractType);
                }

                string contractSkill = HumanizeUnknown(ReadStr(payload, C.JsonContractSkill));
                if (contractSkill != C.LabelUnknown)
                {
                    outcomeLines.Add(C.TextContractFocusPrefix + contractSkill);
                }

                if (HasPayloadValue(payload, C.JsonContractIsGroup))
                {
                    outcomeLines.Add(BuildContractScopeText(ReadBool(payload, C.JsonContractIsGroup)));
                }

                int weeklyPayment = ReadInt(payload, C.JsonContractWeeklyPayment);
                if (weeklyPayment != C.ZeroIndex)
                {
                    outcomeLines.Add(C.TextWeeklyPayPrefix + FormatSignedNumber(weeklyPayment));
                }

                int weeklyFans = ReadInt(payload, C.JsonContractWeeklyFans);
                if (weeklyFans != C.ZeroIndex)
                {
                    outcomeLines.Add(C.TextFans + FormatSignedNumber(weeklyFans));
                }

                int weeklyBuzz = ReadInt(payload, C.JsonContractWeeklyBuzz);
                if (weeklyBuzz != C.ZeroIndex)
                {
                    outcomeLines.Add(C.TextBuzz + FormatSignedNumber(weeklyBuzz));
                }

                int weeklyFame = ReadInt(payload, C.JsonContractWeeklyFame);
                if (weeklyFame != C.ZeroIndex)
                {
                    outcomeLines.Add(C.TextFame + FormatSignedNumber(weeklyFame));
                }

                int weeklyStamina = ReadInt(payload, C.JsonContractWeeklyStamina);
                if (weeklyStamina != C.ZeroIndex)
                {
                    outcomeLines.Add(C.TextStamina + FormatSignedNumber(weeklyStamina));
                }

                int weeklyTrainingPoints = ReadInt(payload, C.JsonContractWeeklyTrainingPoints);
                if (weeklyTrainingPoints != C.ZeroIndex)
                {
                    outcomeLines.Add(C.TextTrainingPoints + FormatSignedNumber(weeklyTrainingPoints));
                }

                string endDate = FormatRoundTripDateForUi(ReadStr(payload, C.JsonContractEndDate));
                if (endDate != C.LabelUnknown)
                {
                    outcomeLines.Add(C.TextContractEnd + endDate);
                }
            }
            else if (type == C.EventIdolSalaryChanged)
            {
                int salaryIdolId = ResolveIdFromEvent(ev);
                int payloadIdolId = ReadId(payload, C.JsonIdolId);
                if (payloadIdolId >= C.MinId)
                {
                    salaryIdolId = payloadIdolId;
                }

                string salaryAction = HumanizeSalaryAction(ReadStr(payload, C.JsonSalaryAction));
                p.Title = C.TextSalaryTitlePrefix + salaryAction;
                p.WithWhom = ResolveIdolNameById(salaryIdolId);

                long salaryBefore = ReadLong(payload, C.JsonSalaryBefore);
                long salaryAfter = ReadLong(payload, C.JsonSalaryAfter);
                long salaryDelta = ReadLong(payload, C.JsonSalaryDelta);
                if (salaryDelta == C.LongZero && (salaryBefore != C.LongZero || salaryAfter != C.LongZero))
                {
                    salaryDelta = salaryAfter - salaryBefore;
                }

                if (salaryBefore != C.LongZero || salaryAfter != C.LongZero)
                {
                    outcomeLines.Add(
                        C.TextSalaryPrefix +
                        salaryBefore.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture) +
                        C.SeparatorArrow +
                        salaryAfter.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
                }

                if (salaryDelta != C.LongZero)
                {
                    outcomeLines.Add(C.TextChange + FormatSignedNumber(salaryDelta));
                }

                int salarySatisfactionBefore = ReadInt(payload, C.JsonSalarySatisfactionBefore);
                int salarySatisfactionAfter = ReadInt(payload, C.JsonSalarySatisfactionAfter);
                if (salarySatisfactionBefore != C.ZeroIndex || salarySatisfactionAfter != C.ZeroIndex)
                {
                    outcomeLines.Add(
                        C.TextSalarySatisfaction +
                        salarySatisfactionBefore.ToString(CultureInfo.InvariantCulture) +
                        C.SeparatorArrow +
                        salarySatisfactionAfter.ToString(CultureInfo.InvariantCulture));

                    int salarySatisfactionDelta = salarySatisfactionAfter - salarySatisfactionBefore;
                    if (salarySatisfactionDelta != C.ZeroIndex)
                    {
                        outcomeLines.Add(C.TextSatisfactionChange + FormatSignedNumber(salarySatisfactionDelta));
                    }
                }
            }
            else if (type == C.EventShowStatusChanged || type == C.EventShowEpisodeReleased || type == C.EventShowEpisode)
            {
                bool isEpisodeEvent = type == C.EventShowEpisodeReleased || type == C.EventShowEpisode;
                p.Title = isEpisodeEvent
                    ? BuildShowEventTitle(payload, C.TextShowEpisodeReleased, C.TextShowActionEpisodeReleased)
                    : BuildShowEventTitle(payload, C.TextShowStatusUpdated, C.TextShowActionStatusUpdated);
                p.WithWhom = HumanizeUnknown(ReadStr(payload, C.JsonShowTitle));
                if (p.WithWhom == C.LabelUnknown)
                {
                    p.WithWhom = BuildShowEntityLabel(ev, payload);
                }

                string statusTransition = BuildStatusTransitionText(ReadStr(payload, C.JsonShowPrevStatus), ReadStr(payload, C.JsonShowNewStatus));
                if (!string.IsNullOrEmpty(statusTransition))
                {
                    outcomeLines.Add(C.LabelStatusPrefix + statusTransition);
                }

                string castType = HumanizeUnknown(ReadStr(payload, C.JsonShowCastType));
                if (castType != C.LabelUnknown)
                {
                    outcomeLines.Add(C.LabelCastTypePrefix + castType);
                }

                if (isEpisodeEvent)
                {
                    string releaseRange = BuildShowEpisodeReleaseText(ev, payload);
                    if (!string.IsNullOrEmpty(releaseRange))
                    {
                        outcomeLines.Add(releaseRange);
                    }
                }
                else
                {
                    int episodeCount = ReadInt(payload, C.JsonShowEpisodeCount);
                    if (episodeCount > C.ZeroIndex)
                    {
                        outcomeLines.Add(C.LabelTotalEpisodesReleasedPrefix + episodeCount.ToString(CultureInfo.InvariantCulture));
                    }
                }

                long audience = ReadLong(payload, C.JsonShowAudience);
                if (audience > C.LongZero)
                {
                    outcomeLines.Add(C.LabelAverageAudiencePrefix + audience.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
                }

                long revenue = ReadLong(payload, C.JsonShowRevenue);
                if (revenue > C.LongZero)
                {
                    outcomeLines.Add(C.LabelAverageRevenuePrefix + revenue.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
                }

                int newFans = ReadInt(payload, C.JsonShowNewFans);
                if (newFans > C.ZeroIndex)
                {
                    outcomeLines.Add(C.LabelAverageNewFansPrefix + newFans.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
                }

                int buzz = ReadInt(payload, C.JsonShowBuzz);
                if (buzz > C.ZeroIndex)
                {
                    outcomeLines.Add(C.LabelAverageBuzzPrefix + buzz.ToString(CultureInfo.InvariantCulture));
                }

                string castSummary = BuildShowCastNameSummary(ev, payload, C.ShowCastSummaryMaxNames);
                if (!string.IsNullOrEmpty(castSummary))
                {
                    outcomeLines.Add(C.LabelCastThisEpisodePrefix + castSummary);
                }

                string episodeDate = FormatRoundTripDateForUi(ReadStr(payload, C.JsonShowEpisodeDate));
                if (episodeDate != C.LabelUnknown)
                {
                    outcomeLines.Add(C.LabelDatePrefix + episodeDate);
                }
            }
            else if (type == C.EventTheaterDailyResult || type == C.EventTheaterCreated || type == C.EventTheaterDestroyed)
            {
                p.Title = type == C.EventTheaterDailyResult
                    ? C.TextTheaterDailyPerformance
                    : (type == C.EventTheaterCreated ? C.TextTheaterOpened : C.TextTheaterClosed);
                p.WithWhom = HumanizeUnknown(ReadStr(payload, C.JsonTheaterTitle));
                if (p.WithWhom == C.LabelUnknown)
                {
                    p.WithWhom = C.TextTheater + Normalize(ev.EntityId);
                }

                string scheduleType = HumanizeUnknown(ReadStr(payload, C.JsonTheaterScheduleType));
                if (scheduleType != C.LabelUnknown)
                {
                    outcomeLines.Add(FormatLocalizedText(C.TextProgramType, scheduleType));
                }

                int attendance = ReadInt(payload, C.JsonTheaterAttendance);
                if (attendance > C.ZeroIndex)
                {
                    outcomeLines.Add(C.TextAttendance + attendance.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
                }

                long theaterRevenue = ReadLong(payload, C.JsonTheaterRevenue);
                if (theaterRevenue != C.LongZero)
                {
                    outcomeLines.Add(C.TextRevenue + theaterRevenue.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
                }

                int subscribersDelta = ReadInt(payload, C.JsonTheaterSubscribersDelta);
                if (subscribersDelta != C.ZeroIndex)
                {
                    outcomeLines.Add(C.TextSubscriberChange + subscribersDelta.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
                }

                long subscribersTotal = ReadLong(payload, C.JsonTheaterSubscribersTotal);
                if (subscribersTotal > C.LongZero)
                {
                    outcomeLines.Add(C.TextSubscribers + subscribersTotal.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
                }

                int averageAttendance = ReadInt(payload, C.JsonTheaterAverageAttendance);
                if (averageAttendance > C.ZeroIndex)
                {
                    outcomeLines.Add(C.TextSevenDayAverageAttendance + averageAttendance.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
                }

                int averageRevenue = ReadInt(payload, C.JsonTheaterAverageRevenue);
                if (averageRevenue > C.ZeroIndex)
                {
                    outcomeLines.Add(C.TextSevenDayAverageRevenue + averageRevenue.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
                }
            }
            else if (type == C.EventActivityPerformance || type == C.EventActivityPromotion || type == C.EventActivitySpaTreatment)
            {
                p.Title = type == C.EventActivityPerformance
                    ? C.TextAgencyActivityPerformance
                    : (type == C.EventActivityPromotion ? C.TextAgencyActivityPromotion : C.TextAgencyActivitySpaTreatment);
                p.WithWhom = C.TextAgencyWide;

                long moneyDelta = ReadLong(payload, C.JsonActivityMoneyDelta);
                if (moneyDelta != C.LongZero)
                {
                    outcomeLines.Add(C.TextMoneyChangePrefix + moneyDelta.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
                }

                long fansDelta = ReadLong(payload, C.JsonActivityFansDelta);
                if (fansDelta != C.LongZero)
                {
                    outcomeLines.Add(C.TextFanChange + fansDelta.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
                }

                int perIdolEarnings = ReadInt(payload, C.JsonActivityPerIdolEarnings);
                if (perIdolEarnings != C.ZeroIndex)
                {
                    outcomeLines.Add(C.TextPerIdolEarnings + perIdolEarnings.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
                }

                float staminaCost = ReadFloat(payload, C.JsonActivityStaminaCost);
                if (Mathf.Abs(staminaCost) > Mathf.Epsilon)
                {
                    outcomeLines.Add(C.TextStaminaCost + staminaCost.ToString(C.FormatSingleMetricTwoDecimals, CultureInfo.InvariantCulture));
                }

                int spaHeal = ReadInt(payload, C.JsonActivitySpaHeal);
                if (spaHeal > C.ZeroIndex)
                {
                    outcomeLines.Add(C.TextSpaRecovery + spaHeal.ToString(CultureInfo.InvariantCulture));
                }
            }
            else if (type == C.EventTourStatusChanged)
            {
                p.Title = C.TextTourStatusUpdated;
                p.WithWhom = C.TextTour + Normalize(ev.EntityId);

                string transition = BuildStatusTransitionText(ReadStr(payload, C.JsonTourPrevStatus), ReadStr(payload, C.JsonTourNewStatus));
                if (!string.IsNullOrEmpty(transition))
                {
                    outcomeLines.Add(C.LabelStatusPrefix + transition);
                }

                outcomeLines.Add(C.TextCountries + ReadInt(payload, C.JsonTourCountryCount).ToString(CultureInfo.InvariantCulture));
                outcomeLines.Add(C.TextAudience + ReadLong(payload, C.JsonTourAudience).ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
                outcomeLines.Add(C.TextRevenue + ReadLong(payload, C.JsonTourRevenue).ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
                outcomeLines.Add(C.TextNewFansPrefix + ReadLong(payload, C.JsonTourNewFans).ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
                outcomeLines.Add(C.TextProductionCostPrefix + ReadInt(payload, C.JsonTourCost).ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
            }
            else if (type == C.EventElectionStatusChanged || type == C.EventElectionResultRecorded)
            {
                p.Title = type == C.EventElectionStatusChanged ? C.TextElectionStatusUpdated : C.TextElectionResultRecorded;
                p.WithWhom = C.TextElection + Normalize(ev.EntityId);

                string transition = BuildStatusTransitionText(ReadStr(payload, C.JsonElectionPrevStatus), ReadStr(payload, C.JsonElectionNewStatus));
                if (!string.IsNullOrEmpty(transition))
                {
                    outcomeLines.Add(C.LabelStatusPrefix + transition);
                }

                string broadcast = HumanizeUnknown(ReadStr(payload, C.JsonElectionBroadcast));
                if (broadcast != C.LabelUnknown)
                {
                    outcomeLines.Add(C.TextBroadcast + broadcast);
                }

                int place = ReadInt(payload, C.JsonElectionPlace);
                if (place > C.ZeroIndex)
                {
                    outcomeLines.Add(C.TextFinalPlace + place.ToString(CultureInfo.InvariantCulture));
                }
            }
            else if (type == C.EventScandalPointsChanged)
            {
                p.Title = C.TextScandalPointsUpdated;
                p.WithWhom = ResolveIdolNameById(ev.IdolId);
                outcomeLines.Add(C.TextScandalPointsPrefix + ReadInt(payload, C.JsonScandalPrev).ToString(CultureInfo.InvariantCulture)
                    + C.SeparatorArrow + ReadInt(payload, C.JsonScandalNew).ToString(CultureInfo.InvariantCulture));
                outcomeLines.Add(C.TextDelta + ReadInt(payload, C.JsonScandalDelta).ToString(CultureInfo.InvariantCulture));
            }
            else if (type == C.EventMedicalInjury || type == C.EventMedicalDepression || type == C.EventMedicalHiatusStarted || type == C.EventMedicalHealed || type == C.EventMedicalHiatusFinished)
            {
                p.Title = type == C.EventMedicalInjury ? C.TextMedicalInjury :
                    (type == C.EventMedicalDepression ? C.TextMedicalDepression :
                    (type == C.EventMedicalHiatusStarted ? C.TextMedicalHiatusStarted :
                    (type == C.EventMedicalHealed ? C.TextMedicalRecovery : C.TextMedicalHiatusFinished)));
                p.WithWhom = ResolveIdolNameById(ev.IdolId);

                string transition = BuildStatusTransitionText(ReadStr(payload, C.JsonMedicalPrevStatus), ReadStr(payload, C.JsonMedicalCurrentStatus));
                if (!string.IsNullOrEmpty(transition))
                {
                    outcomeLines.Add(C.LabelStatusPrefix + transition);
                }

                string hiatusEnd = FormatRoundTripDateForUi(ReadStr(payload, C.JsonMedicalHiatusEnd));
                if (hiatusEnd != C.LabelUnknown)
                {
                    outcomeLines.Add(C.TextHiatusEnd + hiatusEnd);
                }
            }
            else if (TryBuildExtendedPresentation(type, ev, payload, p, outcomeLines))
            {
            }
            else
            {
                p.Title = HumanizeCode(ev.EventType);
                int payloadIdolId = ReadId(payload, C.JsonIdolId);
                int resolvedIdolId = payloadIdolId >= C.MinId ? payloadIdolId : ev.IdolId;
                p.WithWhom = ResolveIdolNameById(resolvedIdolId);
                outcomeLines.Add(C.TextTimelineEntryRecorded);
            }

            StaffCreditInfo primaryStaff = ReadStaffCredit(payload, string.Empty);
            if (primaryStaff != null && primaryStaff.HasIdentity)
            {
                p.StaffCredit = primaryStaff;
                if (type == C.EventRoomWorkCompleted)
                {
                    AddRoomWorkStaffCreditLines(payload, primaryStaff, outcomeLines);
                }
                else if (type == C.EventIdolHired)
                {
                    outcomeLines.Add(C.TextHiredBy + C.SeparatorSpace + FormatStaffCredit(primaryStaff));
                }
                else
                {
                    outcomeLines.Add(C.TextCreditedTo + C.SeparatorSpace + FormatStaffCredit(primaryStaff));
                }
            }

            AddRelatedLifecycleStartDateLineIfKnown(ev, payload, outcomeLines);
            AddRelatedIdols(p.RelatedIdols, ev, payload);
            if (type == C.EventDatingPartnerStatusChanged || type == C.EventIdolDatingStatusChanged)
            {
                int partnerId;
                if (TryResolveDatingPartnerId(ev, payload, out partnerId))
                {
                    if (ShouldRevealSocialParticipant(ev, payload, partnerId))
                    {
                        AddRelatedId(p.RelatedIdols, partnerId);
                        data_girls.girls partnerGirl = data_girls.GetGirlByID(partnerId);
                        if (partnerGirl != null)
                        {
                            outcomeLines.Add(C.TextOtherIdol + ResolveIdolName(partnerGirl));
                        }
                    }
                }
            }
            else if (type == C.EventIdolDatingStarted || type == C.EventIdolDatingEnded || type == C.EventIdolRelationshipStatusChanged)
            {
                int otherId = ResolveOtherRelationshipIdolId(ev, payload);
                if (ShouldRevealSocialParticipant(ev, payload, otherId))
                {
                    AddRelatedId(p.RelatedIdols, otherId);
                    outcomeLines.Add(C.TextOtherIdol + ResolveIdolNameById(otherId));
                }
            }
            else if (type == C.EventBullyingStarted || type == C.EventBullyingEnded)
            {
                int targetId = ReadId(payload, C.JsonBullyingTargetId);
                int leaderId = ReadId(payload, C.JsonBullyingLeaderId);
                if (ShouldRevealSocialParticipant(ev, payload, targetId))
                {
                    AddRelatedId(p.RelatedIdols, targetId);
                }

                if (ShouldRevealSocialParticipant(ev, payload, leaderId))
                {
                    AddRelatedId(p.RelatedIdols, leaderId);
                }
            }

            if (outcomeLines.Count == C.ZeroIndex &&
                (type == C.EventStatusChanged ||
                 type == C.EventDatingPartnerStatusChanged ||
                 type == C.EventIdolDatingStatusChanged ||
                 type == C.EventShowStatusChanged ||
                 type == C.EventShowEpisodeReleased ||
                 type == C.EventShowEpisode ||
                 type == C.EventIdolDatingStarted ||
                 type == C.EventIdolDatingEnded ||
                 type == C.EventIdolRelationshipStatusChanged))
            {
                outcomeLines.Add(C.TextStatusUpdatedSentence);
            }

            if (string.IsNullOrEmpty(p.WithWhom))
            {
                p.WithWhom = C.LabelUnknown;
            }

            p.Outcome = outcomeLines.Count > C.ZeroIndex
                ? string.Join(C.OutcomeLinesJoinSeparator, outcomeLines.ToArray())
                : string.Empty;

            if (string.IsNullOrEmpty(p.Outcome))
            {
                p.Outcome = C.LabelUnknown;
            }

            return p;
        }

        /// <summary>
        /// Reads portable staff attribution.  Passing an empty prefix reads the primary staff;
        /// "assigned_" and "completed_" read the room-work role-specific fields.
        /// </summary>
        private static StaffCreditInfo ReadStaffCredit(JSONNode payload, string prefix)
        {
            if (payload == null)
            {
                return null;
            }

            string normalizedPrefix = prefix ?? string.Empty;
            string name = ReadStr(payload, normalizedPrefix + "staff_name");
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            StaffCreditInfo credit = new StaffCreditInfo
            {
                StaffId = ReadInt(payload, normalizedPrefix + "staff_id"),
                Name = name,
                Role = ExternalModLocalizationCatalog.ResolveEnglishText(
                    ReadStr(payload, normalizedPrefix + "staff_role")),
                TypeRaw = ReadInt(payload, normalizedPrefix + "staff_type_raw"),
                UniqueTypeRaw = ReadInt(payload, normalizedPrefix + "staff_unique_type_raw"),
                IsPro = ReadBool(payload, normalizedPrefix + "staff_is_pro"),
                IsProducer = ReadBool(payload, normalizedPrefix + "staff_is_producer")
            };

            return credit;
        }

        private static string FormatStaffCredit(StaffCreditInfo credit)
        {
            if (credit == null)
            {
                return C.LabelUnknown;
            }

            string name = credit.DisplayName;
            return string.IsNullOrEmpty(credit.Role) ? name : name + C.MetadataPipeSeparator + credit.Role;
        }

        private static void AddRoomWorkStaffCreditLines(JSONNode payload, StaffCreditInfo primaryStaff, List<string> outcomeLines)
        {
            StaffCreditInfo assigned = ReadStaffCredit(payload, "assigned_");
            StaffCreditInfo completed = ReadStaffCredit(payload, "completed_");
            if (assigned != null && assigned.HasIdentity && completed != null && completed.HasIdentity &&
                string.Equals(assigned.Name, completed.Name, StringComparison.Ordinal))
            {
                outcomeLines.Add(C.TextAssignedAndCompletedBy + C.SeparatorSpace + FormatStaffCredit(completed));
                return;
            }

            if (assigned != null && assigned.HasIdentity)
            {
                outcomeLines.Add(C.LabelAssignedBy + C.SeparatorColonSpace + FormatStaffCredit(assigned));
            }

            if (completed != null && completed.HasIdentity)
            {
                outcomeLines.Add(C.LabelCompletedBy + C.SeparatorColonSpace + FormatStaffCredit(completed));
                return;
            }

            outcomeLines.Add(C.TextCreditedTo + C.SeparatorSpace + FormatStaffCredit(primaryStaff));
        }

        /// <summary>
        /// Applies one JSON-supplied diary presentation before built-in event formatting.
        /// </summary>
        private bool TryApplyCustomDiaryEntry(IMDataCoreEvent ev, JSONNode payload, Presentation p, List<string> outcomeLines, out CustomDiaryEntry customEntry)
        {
            customEntry = null;
            if (ev == null || p == null || outcomeLines == null)
            {
                return false;
            }

            if (!CustomDiaryCatalog.TryFind(ev, payload, out customEntry))
            {
                return false;
            }

            string substoryDisplayName = ResolveSubstoryDisplayName(payload, C.KeySubstoryId, C.KeySubstoryDisplayName);
            string parentDisplayName = ResolveSubstoryDisplayName(payload, C.KeySubstoryParentId, C.KeySubstoryParentDisplayName);
            string involvedIdols = BuildActorSummary(ReadStr(payload, C.KeyActorsSummary));
            ApplyCustomDiaryEntry(customEntry, payload, p, outcomeLines, substoryDisplayName, parentDisplayName, involvedIdols);
            return true;
        }

        /// <summary>
        /// Adds ordered lifecycle dates (start/duration/end) for terminal/cancelled events when a related earlier start event exists.
        /// </summary>
        private void AddRelatedLifecycleStartDateLineIfKnown(IMDataCoreEvent terminalEvent, JSONNode terminalPayload, List<string> outcomeLines)
        {
            if (terminalEvent == null || outcomeLines == null)
            {
                return;
            }

            string terminalType = CanonicalizeTimelineEventType(terminalEvent.EventType ?? string.Empty);
            if (!IsTerminalLifecycleEventType(terminalType))
            {
                return;
            }

            DateTime startDate;
            if (!TryResolveRelatedLifecycleStartDate(terminalEvent, terminalPayload, out startDate) &&
                !TryExtractStartDateFromOutcomeLines(outcomeLines, out startDate))
            {
                return;
            }

            DateTime endDate;
            if (!TryResolveTerminalLifecycleDate(terminalType, terminalEvent, terminalPayload, out endDate) &&
                !TryExtractEndDateFromOutcomeLines(outcomeLines, terminalType, out endDate))
            {
                return;
            }

            if (endDate < startDate)
            {
                return;
            }

            InsertOrReplaceLifecycleDateBlock(outcomeLines, terminalType, startDate, endDate);
        }

        /// <summary>
        /// Resolves one related lifecycle start date by finding the closest earlier matching start event.
        /// </summary>
        private bool TryResolveRelatedLifecycleStartDate(IMDataCoreEvent terminalEvent, JSONNode terminalPayload, out DateTime startDate)
        {
            startDate = DateTime.MinValue;
            if (terminalEvent == null)
            {
                return false;
            }

            string terminalType = CanonicalizeTimelineEventType(terminalEvent.EventType ?? string.Empty);
            if (!IsTerminalLifecycleEventType(terminalType))
            {
                return false;
            }

            int terminalIndex = FindEventIndexById(terminalEvent.EventId);
            if (terminalIndex < C.ZeroIndex)
            {
                return false;
            }

            DateTime terminalDate;
            bool hasTerminalDate = TryResolveTerminalLifecycleDate(terminalType, terminalEvent, terminalPayload, out terminalDate);
            DateTime bestDate = DateTime.MinValue;
            bool found = false;

            for (int i = terminalIndex + C.LastFromCount; i < cachedEvents.Count; i++)
            {
                IMDataCoreEvent candidate = cachedEvents[i];
                if (candidate == null)
                {
                    continue;
                }

                string candidateType = CanonicalizeTimelineEventType(candidate.EventType ?? string.Empty);
                if (!IsMatchingStartEventTypeForTerminal(terminalType, candidateType))
                {
                    continue;
                }

                JSONNode candidatePayload = ParsePayload(candidate.PayloadJson);
                if (!AreLifecycleEventsRelated(terminalType, terminalEvent, terminalPayload, candidate, candidatePayload))
                {
                    continue;
                }

                DateTime candidateDate;
                if (!TryResolveEventOccurrenceDate(candidate, out candidateDate))
                {
                    continue;
                }

                if (hasTerminalDate && candidateDate >= terminalDate)
                {
                    continue;
                }

                if (!found || candidateDate > bestDate)
                {
                    bestDate = candidateDate;
                    found = true;
                }
            }

            if (!found)
            {
                return false;
            }

            startDate = bestDate;
            return true;
        }

        /// <summary>
        /// Returns true when one event type is one terminal lifecycle event that can infer a related start date.
        /// </summary>
        private static bool IsTerminalLifecycleEventType(string terminalType)
        {
            switch (terminalType)
            {
                case C.EventMedicalHiatusFinished:
                case C.EventIdolDatingEnded:
                case C.EventIdolRelationshipStatusChanged:
                case C.EventBullyingEnded:
                case C.EventCliqueLeft:
                case C.EventShowCancelled:
                case C.EventShowRelaunchFinished:
                case C.EventContractCancelled:
                case C.EventContractBroken:
                case C.EventContractFinished:
                case C.EventStatusEnded:
                case C.EventMentorshipEnded:
                case C.EventPushWindowEnded:
                case C.EventTourFinished:
                case C.EventTourCancelled:
                case C.EventElectionFinished:
                case C.EventElectionCancelled:
                case C.EventConcertFinished:
                case C.EventConcertCancelled:
                case C.EventRandomEventConcluded:
                case C.EventSubstoryCompleted:
                case C.EventTaskCompleted:
                case C.EventTaskFailed:
                case C.EventTaskDone:
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true when candidate type can serve as start event for the provided terminal type.
        /// </summary>
        private static bool IsMatchingStartEventTypeForTerminal(string terminalType, string candidateType)
        {
            switch (terminalType)
            {
                case C.EventMedicalHiatusFinished:
                    return string.Equals(candidateType, C.EventMedicalInjury, StringComparison.Ordinal);

                case C.EventIdolDatingEnded:
                    return string.Equals(candidateType, C.EventIdolDatingStarted, StringComparison.Ordinal);

                case C.EventIdolRelationshipStatusChanged:
                    return string.Equals(candidateType, C.EventIdolRelationshipStatusChanged, StringComparison.Ordinal);

                case C.EventBullyingEnded:
                    return string.Equals(candidateType, C.EventBullyingStarted, StringComparison.Ordinal);

                case C.EventCliqueLeft:
                    return string.Equals(candidateType, C.EventCliqueJoined, StringComparison.Ordinal);

                case C.EventShowCancelled:
                    return string.Equals(candidateType, C.EventShowReleased, StringComparison.Ordinal) ||
                        string.Equals(candidateType, C.EventShowCreated, StringComparison.Ordinal);

                case C.EventShowRelaunchFinished:
                    return string.Equals(candidateType, C.EventShowRelaunchStarted, StringComparison.Ordinal);

                case C.EventContractCancelled:
                case C.EventContractBroken:
                case C.EventContractFinished:
                    return string.Equals(candidateType, C.EventContractActivated, StringComparison.Ordinal) ||
                        string.Equals(candidateType, C.EventContractAccepted, StringComparison.Ordinal);

                case C.EventStatusEnded:
                    return string.Equals(candidateType, C.EventStatusStarted, StringComparison.Ordinal);

                case C.EventMentorshipEnded:
                    return string.Equals(candidateType, C.EventMentorshipStarted, StringComparison.Ordinal);

                case C.EventPushWindowEnded:
                    return string.Equals(candidateType, C.EventPushWindowStarted, StringComparison.Ordinal);

                case C.EventTourFinished:
                case C.EventTourCancelled:
                    return string.Equals(candidateType, C.EventTourStarted, StringComparison.Ordinal) ||
                        string.Equals(candidateType, C.EventTourCreated, StringComparison.Ordinal);

                case C.EventElectionFinished:
                case C.EventElectionCancelled:
                    return string.Equals(candidateType, C.EventElectionStarted, StringComparison.Ordinal) ||
                        string.Equals(candidateType, C.EventElectionCreated, StringComparison.Ordinal);

                case C.EventConcertFinished:
                case C.EventConcertCancelled:
                    return string.Equals(candidateType, C.EventConcertStarted, StringComparison.Ordinal) ||
                        string.Equals(candidateType, C.EventConcertCreated, StringComparison.Ordinal);

                case C.EventRandomEventConcluded:
                    return string.Equals(candidateType, C.EventRandomEventStarted, StringComparison.Ordinal);

                case C.EventSubstoryCompleted:
                    return string.Equals(candidateType, C.EventSubstoryStarted, StringComparison.Ordinal);

                case C.EventTaskCompleted:
                case C.EventTaskFailed:
                case C.EventTaskDone:
                    return string.Equals(candidateType, C.EventTaskAdded, StringComparison.Ordinal);
            }

            return false;
        }

        /// <summary>
        /// Returns true when terminal and candidate events refer to the same lifecycle thread.
        /// </summary>
        private bool AreLifecycleEventsRelated(string terminalType, IMDataCoreEvent terminalEvent, JSONNode terminalPayload, IMDataCoreEvent candidateEvent, JSONNode candidatePayload)
        {
            if (terminalEvent == null || candidateEvent == null)
            {
                return false;
            }

            if (string.Equals(terminalType, C.EventIdolDatingEnded, StringComparison.Ordinal))
            {
                int terminalPairLeft;
                int terminalPairRight;
                int candidatePairLeft;
                int candidatePairRight;
                if (TryResolveRelationshipPair(terminalEvent, terminalPayload, out terminalPairLeft, out terminalPairRight) &&
                    TryResolveRelationshipPair(candidateEvent, candidatePayload, out candidatePairLeft, out candidatePairRight))
                {
                    return terminalPairLeft == candidatePairLeft && terminalPairRight == candidatePairRight;
                }

                return HasSameEntityReference(terminalEvent, candidateEvent);
            }

            if (string.Equals(terminalType, C.EventIdolRelationshipStatusChanged, StringComparison.Ordinal))
            {
                return AreRelationshipStatusEventsRelated(terminalEvent, terminalPayload, candidateEvent, candidatePayload);
            }

            if (string.Equals(terminalType, C.EventBullyingEnded, StringComparison.Ordinal))
            {
                return AreBullyingEventsRelated(terminalEvent, terminalPayload, candidateEvent, candidatePayload);
            }

            if (string.Equals(terminalType, C.EventCliqueLeft, StringComparison.Ordinal))
            {
                return AreCliqueEventsRelated(terminalEvent, terminalPayload, candidateEvent, candidatePayload);
            }

            if (string.Equals(terminalType, C.EventContractCancelled, StringComparison.Ordinal) ||
                string.Equals(terminalType, C.EventContractBroken, StringComparison.Ordinal) ||
                string.Equals(terminalType, C.EventContractFinished, StringComparison.Ordinal))
            {
                return AreContractEventsRelated(terminalEvent, terminalPayload, candidateEvent, candidatePayload);
            }

            if (HasSameEntityReference(terminalEvent, candidateEvent))
            {
                return true;
            }

            int terminalIdolId = ResolveIdolIdFromEventPayload(terminalEvent, terminalPayload);
            int candidateIdolId = ResolveIdolIdFromEventPayload(candidateEvent, candidatePayload);
            return terminalIdolId >= C.MinId && terminalIdolId == candidateIdolId;
        }

        /// <summary>
        /// Matches relationship-status transitions by pair identity and status segment continuity.
        /// </summary>
        private bool AreRelationshipStatusEventsRelated(
            IMDataCoreEvent terminalEvent,
            JSONNode terminalPayload,
            IMDataCoreEvent candidateEvent,
            JSONNode candidatePayload)
        {
            int terminalPairLeft;
            int terminalPairRight;
            int candidatePairLeft;
            int candidatePairRight;
            bool hasTerminalPair = TryResolveRelationshipPair(terminalEvent, terminalPayload, out terminalPairLeft, out terminalPairRight);
            bool hasCandidatePair = TryResolveRelationshipPair(candidateEvent, candidatePayload, out candidatePairLeft, out candidatePairRight);
            if (hasTerminalPair && hasCandidatePair &&
                (terminalPairLeft != candidatePairLeft || terminalPairRight != candidatePairRight))
            {
                return false;
            }

            string terminalPreviousStatus = NormalizeLifecycleStatusCode(ReadStr(terminalPayload, C.JsonRelationshipPreviousStatus));
            if (string.IsNullOrEmpty(terminalPreviousStatus))
            {
                return false;
            }

            string candidateNewStatus = NormalizeLifecycleStatusCode(ReadStr(candidatePayload, C.JsonRelationshipNewStatus));
            if (string.IsNullOrEmpty(candidateNewStatus))
            {
                candidateNewStatus = NormalizeLifecycleStatusCode(ReadStr(candidatePayload, C.JsonRelationshipStatus));
            }

            if (!string.Equals(terminalPreviousStatus, candidateNewStatus, StringComparison.Ordinal))
            {
                return false;
            }

            if (hasTerminalPair && hasCandidatePair)
            {
                return true;
            }

            if (HasSameEntityReference(terminalEvent, candidateEvent))
            {
                return true;
            }

            int terminalIdolId = ResolveIdolIdFromEventPayload(terminalEvent, terminalPayload);
            int candidateIdolId = ResolveIdolIdFromEventPayload(candidateEvent, candidatePayload);
            return terminalIdolId >= C.MinId && terminalIdolId == candidateIdolId;
        }

        /// <summary>
        /// Matches bullying lifecycle rows by target/leader identity.
        /// </summary>
        private static bool AreBullyingEventsRelated(
            IMDataCoreEvent terminalEvent,
            JSONNode terminalPayload,
            IMDataCoreEvent candidateEvent,
            JSONNode candidatePayload)
        {
            if (HasSameEntityReference(terminalEvent, candidateEvent))
            {
                return true;
            }

            int terminalTargetId = ReadId(terminalPayload, C.JsonBullyingTargetId);
            int candidateTargetId = ReadId(candidatePayload, C.JsonBullyingTargetId);
            if (terminalTargetId >= C.MinId && candidateTargetId >= C.MinId && terminalTargetId != candidateTargetId)
            {
                return false;
            }

            int terminalLeaderId = ReadId(terminalPayload, C.JsonBullyingLeaderId);
            int candidateLeaderId = ReadId(candidatePayload, C.JsonBullyingLeaderId);
            if (terminalLeaderId >= C.MinId && candidateLeaderId >= C.MinId && terminalLeaderId != candidateLeaderId)
            {
                return false;
            }

            if (terminalTargetId >= C.MinId && candidateTargetId >= C.MinId)
            {
                return true;
            }

            int terminalIdolId = ResolveIdolIdFromEventPayload(terminalEvent, terminalPayload);
            int candidateIdolId = ResolveIdolIdFromEventPayload(candidateEvent, candidatePayload);
            return terminalIdolId >= C.MinId && terminalIdolId == candidateIdolId;
        }

        /// <summary>
        /// Matches clique membership lifecycle rows by idol identity plus optional leader continuity.
        /// </summary>
        private static bool AreCliqueEventsRelated(
            IMDataCoreEvent terminalEvent,
            JSONNode terminalPayload,
            IMDataCoreEvent candidateEvent,
            JSONNode candidatePayload)
        {
            if (HasSameEntityReference(terminalEvent, candidateEvent))
            {
                return true;
            }

            int terminalIdolId = ResolveIdolIdFromEventPayload(terminalEvent, terminalPayload);
            int candidateIdolId = ResolveIdolIdFromEventPayload(candidateEvent, candidatePayload);
            if (terminalIdolId < C.MinId || terminalIdolId != candidateIdolId)
            {
                return false;
            }

            int terminalLeaderId = ReadId(terminalPayload, C.KeyCliqueLeaderIdBefore);
            if (terminalLeaderId < C.MinId)
            {
                terminalLeaderId = ReadId(terminalPayload, C.KeyCliqueLeaderId);
            }

            int candidateLeaderId = ReadId(candidatePayload, C.KeyCliqueLeaderIdAfter);
            if (candidateLeaderId < C.MinId)
            {
                candidateLeaderId = ReadId(candidatePayload, C.KeyCliqueLeaderId);
            }

            return terminalLeaderId < C.MinId || candidateLeaderId < C.MinId || terminalLeaderId == candidateLeaderId;
        }

        /// <summary>
        /// Normalizes status code for stable lifecycle matching.
        /// </summary>
        private static string NormalizeLifecycleStatusCode(string statusCode)
        {
            return (statusCode ?? string.Empty).Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Matches contract events by stable contract identity first, then by idol/type fallback.
        /// </summary>
        private static bool AreContractEventsRelated(IMDataCoreEvent terminalEvent, JSONNode terminalPayload, IMDataCoreEvent candidateEvent, JSONNode candidatePayload)
        {
            if (terminalEvent == null || candidateEvent == null)
            {
                return false;
            }

            if (HasSameEntityReference(terminalEvent, candidateEvent))
            {
                return true;
            }

            int terminalEntityIdolId;
            string terminalEntityTypeCode;
            int terminalEntityEndDateKey;
            int candidateEntityIdolId;
            string candidateEntityTypeCode;
            int candidateEntityEndDateKey;
            if (TryParseContractEntityIdentifier(terminalEvent.EntityId, out terminalEntityIdolId, out terminalEntityTypeCode, out terminalEntityEndDateKey) &&
                TryParseContractEntityIdentifier(candidateEvent.EntityId, out candidateEntityIdolId, out candidateEntityTypeCode, out candidateEntityEndDateKey) &&
                terminalEntityIdolId == candidateEntityIdolId &&
                string.Equals(terminalEntityTypeCode, candidateEntityTypeCode, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            int terminalIdolId = ResolveIdolIdFromEventPayload(terminalEvent, terminalPayload);
            int candidateIdolId = ResolveIdolIdFromEventPayload(candidateEvent, candidatePayload);
            if (terminalIdolId < C.MinId || terminalIdolId != candidateIdolId)
            {
                return false;
            }

            string terminalType = ReadStr(terminalPayload, C.JsonContractType);
            string candidateType = ReadStr(candidatePayload, C.JsonContractType);
            if (!string.IsNullOrEmpty(terminalType) && !string.IsNullOrEmpty(candidateType))
            {
                return string.Equals(terminalType, candidateType, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }

        /// <summary>
        /// Resolves one sorted relationship pair key from event/payload.
        /// </summary>
        private bool TryResolveRelationshipPair(IMDataCoreEvent ev, JSONNode payload, out int leftId, out int rightId)
        {
            leftId = C.InvalidId;
            rightId = C.InvalidId;

            int idolAId = ReadId(payload, C.JsonRelationshipIdolAId);
            int idolBId = ReadId(payload, C.JsonRelationshipIdolBId);
            if (idolAId < C.MinId || idolBId < C.MinId)
            {
                idolAId = ReadId(payload, C.KeyIdolAId);
                idolBId = ReadId(payload, C.KeyIdolBId);
            }

            if (idolAId < C.MinId || idolBId < C.MinId)
            {
                int resolvedPrimaryId = ResolveIdolIdFromEventPayload(ev, payload);
                int resolvedOtherId = ResolveOtherRelationshipIdolId(ev, payload);
                if (resolvedPrimaryId >= C.MinId && resolvedOtherId >= C.MinId)
                {
                    idolAId = resolvedPrimaryId;
                    idolBId = resolvedOtherId;
                }
            }

            if (idolAId < C.MinId || idolBId < C.MinId || idolAId == idolBId)
            {
                return false;
            }

            leftId = Mathf.Min(idolAId, idolBId);
            rightId = Mathf.Max(idolAId, idolBId);
            return true;
        }

        /// <summary>
        /// Resolves idol id from payload when available, then falls back to event-level id.
        /// </summary>
        private static int ResolveIdolIdFromEventPayload(IMDataCoreEvent ev, JSONNode payload)
        {
            int payloadIdolId = ReadId(payload, C.JsonIdolId);
            if (payloadIdolId >= C.MinId)
            {
                return payloadIdolId;
            }

            return ResolveIdFromEvent(ev);
        }

        /// <summary>
        /// Returns true when both events share the same entity kind/id identity.
        /// </summary>
        private static bool HasSameEntityReference(IMDataCoreEvent left, IMDataCoreEvent right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            string leftKind = left.EntityKind ?? string.Empty;
            string rightKind = right.EntityKind ?? string.Empty;
            string leftId = left.EntityId ?? string.Empty;
            string rightId = right.EntityId ?? string.Empty;
            if (string.IsNullOrEmpty(leftKind) || string.IsNullOrEmpty(rightKind) || string.IsNullOrEmpty(leftId) || string.IsNullOrEmpty(rightId))
            {
                return false;
            }

            return string.Equals(leftKind, rightKind, StringComparison.Ordinal) &&
                string.Equals(leftId, rightId, StringComparison.Ordinal);
        }

        /// <summary>
        /// Resolves terminal event end date using end-specific fields before falling back to event timeline date.
        /// </summary>
        private static bool TryResolveTerminalLifecycleDate(string terminalType, IMDataCoreEvent terminalEvent, JSONNode terminalPayload, out DateTime terminalDate)
        {
            terminalDate = DateTime.MinValue;
            string rawDate = string.Empty;
            if (string.Equals(terminalType, C.EventMedicalHiatusFinished, StringComparison.Ordinal))
            {
                rawDate = ReadStr(terminalPayload, C.JsonMedicalHiatusEnd);
            }
            else if (string.Equals(terminalType, C.EventContractCancelled, StringComparison.Ordinal) ||
                     string.Equals(terminalType, C.EventContractBroken, StringComparison.Ordinal) ||
                     string.Equals(terminalType, C.EventContractFinished, StringComparison.Ordinal))
            {
                rawDate = ReadStr(terminalPayload, C.JsonContractEndDate);
                if (TryParseTimelineDateValue(rawDate, out terminalDate))
                {
                    return true;
                }

                int parsedEntityIdolId;
                string parsedEntityTypeCode;
                int parsedEntityEndDateKey;
                if (TryParseContractEntityIdentifier(terminalEvent != null ? terminalEvent.EntityId : string.Empty, out parsedEntityIdolId, out parsedEntityTypeCode, out parsedEntityEndDateKey) &&
                    TryParseDateKeyForUi(parsedEntityEndDateKey, out terminalDate))
                {
                    return true;
                }
            }
            else if (string.Equals(terminalType, C.EventTourFinished, StringComparison.Ordinal) ||
                     string.Equals(terminalType, C.EventTourCancelled, StringComparison.Ordinal))
            {
                rawDate = ReadStr(terminalPayload, C.JsonTourFinishDate);
            }
            else if (string.Equals(terminalType, C.EventElectionFinished, StringComparison.Ordinal) ||
                     string.Equals(terminalType, C.EventElectionCancelled, StringComparison.Ordinal))
            {
                rawDate = ReadStr(terminalPayload, C.JsonElectionFinishDate);
            }
            else if (string.Equals(terminalType, C.EventConcertFinished, StringComparison.Ordinal) ||
                     string.Equals(terminalType, C.EventConcertCancelled, StringComparison.Ordinal))
            {
                rawDate = ReadStr(terminalPayload, C.KeyConcertFinishDate);
            }

            if (TryParseTimelineDateValue(rawDate, out terminalDate))
            {
                return true;
            }

            if (TryResolveEventOccurrenceDate(terminalEvent, out terminalDate))
            {
                return true;
            }

            return TryResolveTimelineDate(terminalEvent, out terminalDate);
        }

        /// <summary>
        /// Resolves raw event occurrence date from GameDateTime/GameDateKey fields.
        /// </summary>
        private static bool TryResolveEventOccurrenceDate(IMDataCoreEvent ev, out DateTime parsed)
        {
            parsed = DateTime.MinValue;
            if (ev == null)
            {
                return false;
            }

            if (TryParseEventDate(ev.GameDateTime, out parsed))
            {
                return true;
            }

            return TryParseDateKeyForUi(ev.GameDateKey, out parsed);
        }

        /// <summary>
        /// Inserts one normalized lifecycle date block: start date, computed duration, end date.
        /// </summary>
        private static void InsertOrReplaceLifecycleDateBlock(List<string> lines, string terminalType, DateTime startDate, DateTime endDate)
        {
            if (lines == null)
            {
                return;
            }

            string endLabel = ResolveLifecycleEndDateLabel(terminalType);
            string[] removePrefixes = new[]
            {
                C.TextStartDatePrefix,
                C.TextEndDatePrefix,
                C.TextContractEnd,
                C.TextHiatusEnd,
                (C.TextStartDate ?? string.Empty) + C.SeparatorColonSpace,
                (C.TextEndDate ?? string.Empty) + C.SeparatorColonSpace,
                C.TextDuration
            };

            int insertIndex = RemoveLinesStartingWithAnyPrefix(lines, removePrefixes);
            if (insertIndex < C.ZeroIndex || insertIndex > lines.Count)
            {
                insertIndex = lines.Count;
            }

            string startLine = C.TextStartDatePrefix + FormatUiDate(startDate);
            string durationLine = C.TextDuration + FormatLifecycleDuration(startDate, endDate);
            string endLine = endLabel + FormatUiDate(endDate);

            lines.Insert(insertIndex, startLine);
            lines.Insert(insertIndex + C.LastFromCount, durationLine);
            lines.Insert(insertIndex + 2, endLine);
        }

        /// <summary>
        /// Removes lines matching known prefixes and returns earliest removed index, or -1 if none removed.
        /// </summary>
        private static int RemoveLinesStartingWithAnyPrefix(List<string> lines, string[] prefixes)
        {
            if (lines == null || prefixes == null)
            {
                return C.InvalidId;
            }

            int firstRemovedIndex = C.InvalidId;
            for (int i = lines.Count - C.LastFromCount; i >= C.ZeroIndex; i--)
            {
                string line = lines[i] ?? string.Empty;
                if (!StartsWithAnyPrefix(line, prefixes))
                {
                    continue;
                }

                firstRemovedIndex = firstRemovedIndex == C.InvalidId ? i : Mathf.Min(firstRemovedIndex, i);
                lines.RemoveAt(i);
            }

            return firstRemovedIndex;
        }

        /// <summary>
        /// Returns true when input starts with any non-empty prefix.
        /// </summary>
        private static bool StartsWithAnyPrefix(string input, string[] prefixes)
        {
            string value = input ?? string.Empty;
            for (int i = C.ZeroIndex; i < prefixes.Length; i++)
            {
                string prefix = prefixes[i] ?? string.Empty;
                if (!string.IsNullOrEmpty(prefix) && value.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Resolves the preferred end-date label for terminal lifecycle event types.
        /// </summary>
        private static string ResolveLifecycleEndDateLabel(string terminalType)
        {
            if (string.Equals(terminalType, C.EventMedicalHiatusFinished, StringComparison.Ordinal))
            {
                return C.TextHiatusEnd;
            }

            if (string.Equals(terminalType, C.EventContractCancelled, StringComparison.Ordinal) ||
                string.Equals(terminalType, C.EventContractBroken, StringComparison.Ordinal) ||
                string.Equals(terminalType, C.EventContractFinished, StringComparison.Ordinal))
            {
                return C.TextEndDatePrefix;
            }

            if (string.Equals(terminalType, C.EventTourFinished, StringComparison.Ordinal) ||
                string.Equals(terminalType, C.EventTourCancelled, StringComparison.Ordinal) ||
                string.Equals(terminalType, C.EventElectionFinished, StringComparison.Ordinal) ||
                string.Equals(terminalType, C.EventElectionCancelled, StringComparison.Ordinal) ||
                string.Equals(terminalType, C.EventConcertFinished, StringComparison.Ordinal) ||
                string.Equals(terminalType, C.EventConcertCancelled, StringComparison.Ordinal))
            {
                return (C.TextFinishDate ?? string.Empty) + C.SeparatorColonSpace;
            }

            return C.TextEndDatePrefix;
        }

        /// <summary>
        /// Attempts to read start date from already-composed outcome lines.
        /// </summary>
        private static bool TryExtractStartDateFromOutcomeLines(List<string> lines, out DateTime parsed)
        {
            string[] prefixes = new[]
            {
                C.TextStartDatePrefix,
                (C.TextStartDate ?? string.Empty) + C.SeparatorColonSpace
            };

            return TryExtractDateFromOutcomeLines(lines, prefixes, out parsed);
        }

        /// <summary>
        /// Attempts to read end date from already-composed outcome lines.
        /// </summary>
        private static bool TryExtractEndDateFromOutcomeLines(List<string> lines, string terminalType, out DateTime parsed)
        {
            string[] prefixes = new[]
            {
                ResolveLifecycleEndDateLabel(terminalType),
                C.TextEndDatePrefix,
                C.TextContractEnd,
                C.TextHiatusEnd,
                (C.TextFinishDate ?? string.Empty) + C.SeparatorColonSpace
            };

            return TryExtractDateFromOutcomeLines(lines, prefixes, out parsed);
        }

        /// <summary>
        /// Attempts to parse first date found in lines matching provided prefixes.
        /// </summary>
        private static bool TryExtractDateFromOutcomeLines(List<string> lines, string[] prefixes, out DateTime parsed)
        {
            parsed = DateTime.MinValue;
            if (lines == null || prefixes == null)
            {
                return false;
            }

            for (int i = C.ZeroIndex; i < lines.Count; i++)
            {
                string line = lines[i] ?? string.Empty;
                for (int prefixIndex = C.ZeroIndex; prefixIndex < prefixes.Length; prefixIndex++)
                {
                    string prefix = prefixes[prefixIndex] ?? string.Empty;
                    if (string.IsNullOrEmpty(prefix) || !line.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string dateText = line.Substring(prefix.Length).Trim();
                    if (TryParseUiDateText(dateText, out parsed) || TryParseTimelineDateValue(dateText, out parsed))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Formats one UI date using the mod pattern and game-localized weekday names.
        /// </summary>
        private static string FormatUiDate(DateTime value)
        {
            return value.ToString(C.DateFormatUi, GetUiDateCulture());
        }

        /// <summary>
        /// Creates a date culture that keeps invariant parsing behavior while adopting game weekday labels.
        /// </summary>
        private static CultureInfo GetUiDateCulture()
        {
            CultureInfo gameLanguageCulture = GetGameLanguageDateCulture();
            if (gameLanguageCulture != null)
            {
                return gameLanguageCulture;
            }

            CultureInfo culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            DateTimeFormatInfo invariantFormat = CultureInfo.InvariantCulture.DateTimeFormat;

            string sunday = GetGameLocalization(C.LanguageKeySunday);
            string monday = GetGameLocalization(C.LanguageKeyMonday);
            string tuesday = GetGameLocalization(C.LanguageKeyTuesday);
            string wednesday = GetGameLocalization(C.LanguageKeyWednesday);
            string thursday = GetGameLocalization(C.LanguageKeyThursday);
            string friday = GetGameLocalization(C.LanguageKeyFriday);
            string saturday = GetGameLocalization(C.LanguageKeySaturday);

            culture.DateTimeFormat.DayNames = new[]
            {
                string.IsNullOrEmpty(sunday) ? invariantFormat.DayNames[(int)DayOfWeek.Sunday] : sunday,
                string.IsNullOrEmpty(monday) ? invariantFormat.DayNames[(int)DayOfWeek.Monday] : monday,
                string.IsNullOrEmpty(tuesday) ? invariantFormat.DayNames[(int)DayOfWeek.Tuesday] : tuesday,
                string.IsNullOrEmpty(wednesday) ? invariantFormat.DayNames[(int)DayOfWeek.Wednesday] : wednesday,
                string.IsNullOrEmpty(thursday) ? invariantFormat.DayNames[(int)DayOfWeek.Thursday] : thursday,
                string.IsNullOrEmpty(friday) ? invariantFormat.DayNames[(int)DayOfWeek.Friday] : friday,
                string.IsNullOrEmpty(saturday) ? invariantFormat.DayNames[(int)DayOfWeek.Saturday] : saturday
            };

            // The base game only exposes one localized label per weekday key, so use it for both ddd and dddd.
            culture.DateTimeFormat.AbbreviatedDayNames = new[]
            {
                string.IsNullOrEmpty(sunday) ? invariantFormat.AbbreviatedDayNames[(int)DayOfWeek.Sunday] : sunday,
                string.IsNullOrEmpty(monday) ? invariantFormat.AbbreviatedDayNames[(int)DayOfWeek.Monday] : monday,
                string.IsNullOrEmpty(tuesday) ? invariantFormat.AbbreviatedDayNames[(int)DayOfWeek.Tuesday] : tuesday,
                string.IsNullOrEmpty(wednesday) ? invariantFormat.AbbreviatedDayNames[(int)DayOfWeek.Wednesday] : wednesday,
                string.IsNullOrEmpty(thursday) ? invariantFormat.AbbreviatedDayNames[(int)DayOfWeek.Thursday] : thursday,
                string.IsNullOrEmpty(friday) ? invariantFormat.AbbreviatedDayNames[(int)DayOfWeek.Friday] : friday,
                string.IsNullOrEmpty(saturday) ? invariantFormat.AbbreviatedDayNames[(int)DayOfWeek.Saturday] : saturday
            };
            culture.DateTimeFormat.ShortestDayNames = (string[])culture.DateTimeFormat.AbbreviatedDayNames.Clone();
            return culture;
        }

        /// <summary>
        /// Uses a language-native date culture matching Idol Manager's selected
        /// language so both month and weekday names are localized consistently.
        /// </summary>
        private static CultureInfo GetGameLanguageDateCulture()
        {
            string selectedLanguage = (ModLocalization.GetEffectiveLanguageCode() ?? string.Empty).Trim().ToLowerInvariant();
            string cultureName;
            switch (selectedLanguage)
            {
                case "en":
                case "english":
                    cultureName = "en";
                    break;
                case "kr":
                case "ko":
                case "ko-kr":
                case "korean":
                    cultureName = "ko-KR";
                    break;
                case "fr":
                case "fr-fr":
                case "french":
                    cultureName = "fr-FR";
                    break;
                case "cn":
                case "zh":
                case "zh-cn":
                case "zh-hans":
                case "schinese":
                    cultureName = "zh-CN";
                    break;
                case "jp":
                case "ja":
                case "ja-jp":
                case "japanese":
                    cultureName = "ja-JP";
                    break;
                case "ru":
                case "ru-ru":
                case "russian":
                    cultureName = "ru-RU";
                    break;
                case "ptbr":
                case "pt-br":
                case "brazilian":
                    cultureName = "pt-BR";
                    break;
                default:
                    return null;
            }

            try
            {
                return (CultureInfo)CultureInfo.GetCultureInfo(cultureName).Clone();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Reads one localization value from the game's active language dictionary.
        /// </summary>
        private static string GetGameLocalization(string key)
        {
            if (Language.Data != null)
            {
                string value;
                if (Language.Data.TryGetValue(key, out value) && !string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Inserts values into one base-game localization string when the key exists.
        /// </summary>
        private static string InsertGameLocalization(string key, params string[] values)
        {
            if (string.IsNullOrEmpty(GetGameLocalization(key)))
            {
                return string.Empty;
            }

            try
            {
                return Language.Insert(key, values);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds one player-facing contract scope line.
        /// </summary>
        private static string BuildContractScopeText(bool isGroup)
        {
            return C.TextScope + (isGroup ? C.TextGroupContract : C.TextIdolContract);
        }

        /// <summary>
        /// Converts one raw wish type/formula pair into player-facing text.
        /// </summary>
        private static string BuildWishGoalText(string wishTypeCode, string wishFormula)
        {
            string type = (wishTypeCode ?? string.Empty).Trim().ToLowerInvariant();
            string formula = (wishFormula ?? string.Empty).Trim();
            if (type.Length == C.ZeroIndex || type == C.LabelUnknown.ToLowerInvariant())
            {
                return string.Empty;
            }

            try
            {
                switch (type)
                {
                    case "hiatus":
                        return GetGameLocalization("WISH__HIATUS");
                    case "ssk_rank":
                        if (formula.Length == C.ZeroIndex)
                        {
                            return string.Empty;
                        }

                        if (formula == "1")
                        {
                            return GetGameLocalization("WISH__SSK_1");
                        }

                        if (formula == "10")
                        {
                            return GetGameLocalization("WISH__SSK_ANY");
                        }

                        if (formula == "2")
                        {
                            return GetGameLocalization("WISH__SSK_2");
                        }

                        if (formula == "3")
                        {
                            return GetGameLocalization("WISH__SSK_3");
                        }

                        return InsertGameLocalization("WISH__SSK_OTHER", formula);
                    case "concert":
                        if (formula.Length == C.ZeroIndex)
                        {
                            return string.Empty;
                        }

                        if (formula == "1")
                        {
                            return GetGameLocalization("WISH__CONCERT");
                        }

                        return InsertGameLocalization("WISH__CONCERT_OTHER", formula);
                    case "biz":
                    {
                        string[] tokens = formula.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length < 2)
                        {
                            return string.Empty;
                        }

                        int businessTypeId;
                        int paramTypeId;
                        if (!TryParseInt(tokens[0], out businessTypeId) || !TryParseInt(tokens[1], out paramTypeId))
                        {
                            return string.Empty;
                        }

                        string typeTitle = business.GetTypeTitle((business._type)businessTypeId);
                        string paramTitle = data_girls.GetParamName((data_girls._paramType)paramTypeId);
                        if (string.IsNullOrEmpty(typeTitle) || string.IsNullOrEmpty(paramTitle))
                        {
                            return string.Empty;
                        }

                        return InsertGameLocalization("WISH__JOB", paramTitle, typeTitle);
                    }
                    case "show":
                    {
                        if (formula == "-1 -1 -1")
                        {
                            return GetGameLocalization("WISH__SHOW");
                        }

                        string[] tokens = formula.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length < 3)
                        {
                            return string.Empty;
                        }

                        string genreTitle = string.Empty;
                        string mediumTitle = string.Empty;
                        string cohostText = string.Empty;
                        int parsedId;
                        if (tokens[1] != "-1" && TryParseInt(tokens[1], out parsedId))
                        {
                            Shows._param genre = Shows.GetParamByID(Shows.Genre, parsedId);
                            if (genre != null)
                            {
                                genreTitle = (genre.GetTitle() ?? string.Empty).ToLowerInvariant();
                            }
                        }

                        if (tokens[0] != "-1" && TryParseInt(tokens[0], out parsedId))
                        {
                            Shows._param medium = Shows.GetParamByID(Shows.Medium, parsedId);
                            if (medium != null)
                            {
                                mediumTitle = (medium.GetTitle() ?? string.Empty).ToLowerInvariant();
                            }
                        }

                        if (tokens[2] != "-1" && TryParseInt(tokens[2], out parsedId))
                        {
                            Shows._param mc = Shows.GetParamByID(Shows.MC, parsedId);
                            if (mc != null)
                            {
                                string mcTitle = (mc.GetTitle() ?? string.Empty).ToLowerInvariant();
                                cohostText = InsertGameLocalization("WISH__SHOW_DETAILS_COHOST", mcTitle);
                            }
                        }

                        string wishText = InsertGameLocalization("WISH__SHOW_DETAILS", genreTitle, mediumTitle, cohostText);
                        return string.IsNullOrEmpty(wishText) ? string.Empty : ExtensionMethods.RemoveDoubleSpaces(wishText).Trim();
                    }
                    case "single":
                    {
                        string[] tokens = formula.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length < 4)
                        {
                            return string.Empty;
                        }

                        if (tokens[0] == "-1" || tokens[0] == "4")
                        {
                            return GetGameLocalization("WISH__SINGLE_ANY");
                        }

                        if (tokens[0] == "3")
                        {
                            return GetGameLocalization("WISH__SINGLE_4");
                        }

                        if (tokens[0] == "2")
                        {
                            return GetGameLocalization("WISH__SINGLE_3");
                        }

                        if (tokens[0] == "1")
                        {
                            return GetGameLocalization("WISH__SINGLE_2");
                        }

                        if (tokens[0] == "0" && tokens[1] == "-1" && tokens[2] == "-1" && tokens[3] == "-1")
                        {
                            return GetGameLocalization("WISH__SINGLE_CENTER");
                        }

                        string genreTitle = string.Empty;
                        string lyricsText = string.Empty;
                        string choreographyText = string.Empty;
                        int parsedId;
                        if (tokens[1] != "-1" && TryParseInt(tokens[1], out parsedId))
                        {
                            singles._param genre = singles.GetParamByID(singles._param._type.genre, parsedId);
                            if (genre != null)
                            {
                                genreTitle = (genre.GetTitle() ?? string.Empty).ToLowerInvariant();
                            }
                        }

                        if (tokens[2] != "-1" && TryParseInt(tokens[2], out parsedId))
                        {
                            singles._param lyrics = singles.GetParamByID(singles._param._type.lyrics, parsedId);
                            if (lyrics != null)
                            {
                                lyricsText = InsertGameLocalization("WISH__SINGLE_DETAILS_LYRICS", (lyrics.GetTitle() ?? string.Empty).ToLowerInvariant());
                            }
                        }

                        if (tokens[3] != "-1" && TryParseInt(tokens[3], out parsedId))
                        {
                            singles._param choreography = singles.GetParamByID(singles._param._type.choreography, parsedId);
                            if (choreography != null)
                            {
                                choreographyText = InsertGameLocalization("WISH__SINGLE_DETAILS_CHOREO", (choreography.GetTitle() ?? string.Empty).ToLowerInvariant());
                            }
                        }

                        string wishText = InsertGameLocalization("WISH__SINGLE_DETAILS", genreTitle, lyricsText, choreographyText);
                        return string.IsNullOrEmpty(wishText) ? string.Empty : ExtensionMethods.RemoveDoubleSpaces(wishText).Trim();
                    }
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        /// <summary>
        /// Parses one UI-formatted date line value.
        /// </summary>
        private static bool TryParseUiDateText(string dateText, out DateTime parsed)
        {
            return DateTime.TryParseExact(dateText ?? string.Empty, C.DateFormatUi, GetUiDateCulture(), DateTimeStyles.None, out parsed);
        }

        /// <summary>
        /// Formats lifecycle duration in days/weeks/months/years buckets.
        /// </summary>
        private static string FormatLifecycleDuration(DateTime startDate, DateTime endDate)
        {
            int totalDays = Mathf.Max(C.ZeroIndex, (endDate.Date - startDate.Date).Days);
            if (totalDays < 7)
            {
                return FormatDurationComponent(totalDays, C.TextDurationUnitDaySingular, C.TextDurationUnitDayPlural);
            }

            if (totalDays < 30)
            {
                int weeks = Mathf.Max(C.LastFromCount, totalDays / 7);
                return FormatDurationComponent(weeks, C.TextDurationUnitWeekSingular, C.TextDurationUnitWeekPlural);
            }

            if (totalDays < 365)
            {
                int months;
                DateTime cursor;
                CountCalendarMonths(startDate.Date, endDate.Date, out months, out cursor);
                int weeks = Mathf.Max(C.ZeroIndex, (endDate.Date - cursor).Days / 7);
                return FormatDurationComponent(months, C.TextDurationUnitMonthSingular, C.TextDurationUnitMonthPlural) + C.SeparatorSpace +
                    FormatDurationComponent(weeks, C.TextDurationUnitWeekSingular, C.TextDurationUnitWeekPlural);
            }

            int years;
            DateTime yearCursor;
            CountCalendarYears(startDate.Date, endDate.Date, out years, out yearCursor);
            int remainingMonths;
            DateTime monthCursor;
            CountCalendarMonths(yearCursor, endDate.Date, out remainingMonths, out monthCursor);
            int remainingWeeks = Mathf.Max(C.ZeroIndex, (endDate.Date - monthCursor).Days / 7);
            return FormatDurationComponent(years, C.TextDurationUnitYearSingular, C.TextDurationUnitYearPlural) + C.SeparatorSpace +
                FormatDurationComponent(remainingMonths, C.TextDurationUnitMonthSingular, C.TextDurationUnitMonthPlural) + C.SeparatorSpace +
                FormatDurationComponent(remainingWeeks, C.TextDurationUnitWeekSingular, C.TextDurationUnitWeekPlural);
        }

        /// <summary>
        /// Counts full calendar years between two dates.
        /// </summary>
        private static void CountCalendarYears(DateTime startDate, DateTime endDate, out int years, out DateTime cursor)
        {
            years = C.ZeroIndex;
            cursor = startDate;
            while (cursor.AddYears(C.LastFromCount) <= endDate)
            {
                cursor = cursor.AddYears(C.LastFromCount);
                years += C.LastFromCount;
            }
        }

        /// <summary>
        /// Counts full calendar months between two dates.
        /// </summary>
        private static void CountCalendarMonths(DateTime startDate, DateTime endDate, out int months, out DateTime cursor)
        {
            months = C.ZeroIndex;
            cursor = startDate;
            while (cursor.AddMonths(C.LastFromCount) <= endDate)
            {
                cursor = cursor.AddMonths(C.LastFromCount);
                months += C.LastFromCount;
            }
        }

        /// <summary>
        /// Formats one duration unit with pluralization.
        /// </summary>
        private static string FormatDurationComponent(int value, string unitSingular, string unitPlural)
        {
            string unit = value == C.LastFromCount ? unitSingular : unitPlural;
            return value.ToString(CultureInfo.InvariantCulture) + C.SeparatorSpace + unit;
        }

        /// <summary>
        /// Builds explicit player-facing output for extended IM Data Core event types.
        /// </summary>
        private bool TryBuildExtendedPresentation(string type, IMDataCoreEvent ev, JSONNode payload, Presentation p, List<string> outcomeLines)
        {
            switch (type)
            {
                case C.EventSingleCreated:
                case C.EventSingleCancelled:
                case C.EventSingleStatusChanged:
                case C.EventSingleCastChanged:
                case C.EventSingleGroupChanged:
                    BuildSingleLifecyclePresentation(type, ev, payload, p, outcomeLines);
                    return true;

                case C.EventShowCreated:
                case C.EventShowReleased:
                case C.EventShowCancelled:
                case C.EventShowCastChanged:
                case C.EventShowConfigurationChanged:
                case C.EventShowRelaunchStarted:
                case C.EventShowRelaunchFinished:
                    BuildShowLifecyclePresentation(type, ev, payload, p, outcomeLines);
                    return true;

                case C.EventContractWindowOpened:
                case C.EventContractAccepted:
                case C.EventContractCanceled:
                    BuildContractOfferPresentation(type, ev, payload, p, outcomeLines);
                    return true;

                case C.EventGroupCreated:
                case C.EventGroupDisbanded:
                case C.EventGroupParamPointsChanged:
                case C.EventGroupAppealPointsSpent:
                    BuildGroupPresentation(type, ev, payload, p, outcomeLines);
                    return true;

                case C.EventIdolHired:
                case C.EventIdolGraduationAnnounced:
                case C.EventIdolGraduated:
                case C.EventIdolGroupTransferred:
                    BuildIdolLifecyclePresentation(type, ev, payload, p, outcomeLines);
                    return true;

                case C.EventIdolBirthday:
                    BuildIdolBirthdayPresentation(ev, payload, p, outcomeLines);
                    return true;

                case C.EventStatusChangedLegacy:
                case C.EventStatusStarted:
                case C.EventStatusEnded:
                    BuildLegacyStatusPresentation(type, ev, payload, p, outcomeLines);
                    return true;

                case C.EventEconomyDailyTick:
                case C.EventEconomyWeeklyExpenseApplied:
                    BuildEconomyPresentation(type, ev, payload, p, outcomeLines);
                    return true;

                case C.EventPolicyDecisionSelected:
                    BuildPolicyPresentation(payload, p, outcomeLines);
                    return true;

                case C.EventLoanAdded:
                case C.EventLoanInitialized:
                case C.EventLoanPaidOff:
                    BuildLoanPresentation(type, ev, payload, p, outcomeLines);
                    return true;

                case C.EventBankruptcyDangerSet:
                case C.EventBankruptcyCheck:
                    BuildBankruptcyPresentation(type, payload, p, outcomeLines);
                    return true;

                case C.EventCafeCreated:
                case C.EventCafeDestroyed:
                case C.EventCafeDailyResult:
                    BuildCafePresentation(type, ev, payload, p, outcomeLines);
                    return true;

                case C.EventMentorshipStarted:
                case C.EventMentorshipEnded:
                case C.EventMentorshipWeeklyTick:
                    BuildMentorshipPresentation(type, payload, p, outcomeLines);
                    return true;

                case C.EventPlayerRelationshipChanged:
                case C.EventPlayerDateInteraction:
                case C.EventPlayerMarriageOutcome:
                    BuildPlayerPresentation(type, ev, payload, p, outcomeLines);
                    return true;

                case C.EventCliqueJoined:
                case C.EventCliqueLeft:
                    BuildCliquePresentation(type, ev, payload, p, outcomeLines);
                    return true;

                case C.EventRandomEventStarted:
                case C.EventRandomEventConcluded:
                case C.EventSubstoryStarted:
                case C.EventSubstoryDelayed:
                case C.EventSubstoryCompleted:
                case C.EventTaskAdded:
                case C.EventTaskCompleted:
                case C.EventTaskFailed:
                case C.EventTaskDone:
                    BuildNarrativePresentation(type, ev, payload, p, outcomeLines);
                    return true;

                case C.EventIdolOutfitChanged:
                    BuildOutfitPresentation(ev, payload, p, outcomeLines);
                    return true;

                case C.EventStoryRouteLocked:
                    BuildStoryRouteLockedPresentation(payload, p, outcomeLines);
                    return true;

                case C.EventResearchParamAssigned:
                case C.EventResearchPointsPurchased:
                case C.EventResearchParamLevelUp:
                case C.EventRivalTrendsUpdated:
                case C.EventRivalMonthlyRecalculated:
                case C.EventPushWindowStarted:
                case C.EventPushWindowEnded:
                case C.EventPushWindowDayIncrement:
                case C.EventWishGenerated:
                case C.EventWishFulfilled:
                case C.EventWishDone:
                case C.EventInfluenceBlackmailQueued:
                case C.EventInfluenceBlackmailTriggered:
                case C.EventScandalMitigated:
                    BuildProgressionPresentation(type, ev, payload, p, outcomeLines);
                    return true;

                case C.EventScandalCheck:
                    BuildScandalCheckPresentation(payload, p, outcomeLines);
                    return true;

                case C.EventSummerGamesFinalized:
                    BuildSummerGamesPresentation(payload, p, outcomeLines);
                    return true;

                case C.EventAuditionStarted:
                case C.EventAuditionCostPaid:
                case C.EventAuditionCooldownReset:
                case C.EventAuditionFailureTriggered:
                    BuildAuditionPresentation(type, payload, p, outcomeLines);
                    return true;

                case C.EventAwardNominated:
                case C.EventAwardResult:
                    BuildAwardPresentation(type, payload, p, outcomeLines);
                    return true;

                case C.EventAgencyRoomBuilt:
                case C.EventAgencyRoomCostPaid:
                    BuildAgencyRoomPresentation(type, payload, p, outcomeLines);
                    return true;

                case C.EventRoomWorkCompleted:
                    BuildRoomWorkPresentation(payload, p, outcomeLines);
                    return true;

                case C.EventTourCreated:
                case C.EventTourStarted:
                case C.EventTourFinished:
                case C.EventTourCancelled:
                case C.EventTourCountryResult:
                case C.EventTourParticipation:
                    BuildTourPresentation(type, ev, payload, p, outcomeLines);
                    return true;

                case C.EventElectionCreated:
                case C.EventElectionStarted:
                case C.EventElectionFinished:
                case C.EventElectionCancelled:
                case C.EventElectionResultsGenerated:
                case C.EventElectionPlaceAdjusted:
                    BuildElectionPresentation(type, ev, payload, p, outcomeLines);
                    return true;

                case C.EventConcertCreated:
                case C.EventConcertStarted:
                case C.EventConcertFinished:
                case C.EventConcertCancelled:
                case C.EventConcertParticipation:
                case C.EventConcertCastChanged:
                case C.EventConcertStatusChanged:
                case C.EventConcertConfigurationChanged:
                case C.EventConcertCardUsed:
                case C.EventConcertCrisisDecision:
                case C.EventConcertCrisisApplied:
                case C.EventConcertFinalResolved:
                    BuildConcertPresentation(type, ev, payload, p, outcomeLines);
                    return true;

                case C.EventStaffHired:
                case C.EventStaffFired:
                case C.EventStaffFiredSeverance:
                case C.EventStaffLevelUp:
                    BuildStaffPresentation(type, ev, payload, p, outcomeLines);
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Builds the diary entry for a completed room preparation or training task.
        /// </summary>
        private static void BuildRoomWorkPresentation(JSONNode payload, Presentation p, List<string> outcomeLines)
        {
            string kind = ReadStr(payload, C.JsonRoomWorkKind);
            string title = NormalizeRawText(ReadStr(payload, C.JsonRoomWorkTitle));
            string stage = HumanizeUnknown(ReadStr(payload, C.JsonRoomWorkStage));

            if (string.Equals(kind, "medical_recovery", StringComparison.OrdinalIgnoreCase))
            {
                p.Title = C.TextStaminaRecoveryCompleted;
                p.WithWhom = title != C.LabelUnknown ? HumanizeUnknown(title) : C.LabelUnknown;
            }
            else if (string.Equals(kind, "training", StringComparison.OrdinalIgnoreCase))
            {
                p.Title = C.TextTrainingPracticeCompleted;
                p.WithWhom = title != C.LabelUnknown ? HumanizeUnknown(title) : C.LabelUnknown;
            }
            else if (string.Equals(kind, "single", StringComparison.OrdinalIgnoreCase))
            {
                p.Title = C.TextSinglePreparationCompleted;
                p.WithWhom = title != C.LabelUnknown ? title : C.LabelUnknown;
            }
            else if (string.Equals(kind, "show", StringComparison.OrdinalIgnoreCase))
            {
                p.Title = C.TextShowPreparationCompleted;
                p.WithWhom = title != C.LabelUnknown ? title : C.LabelUnknown;
            }
            else if (string.Equals(kind, "concert", StringComparison.OrdinalIgnoreCase))
            {
                p.Title = C.TextConcertPreparationCompleted;
                p.WithWhom = title != C.LabelUnknown ? title : C.LabelUnknown;
            }
            else
            {
                p.Title = C.TextEventPreparationCompleted;
                p.WithWhom = title != C.LabelUnknown ? title : C.LabelUnknown;
            }

            if (stage != C.LabelUnknown)
            {
                outcomeLines.Add(C.TextWorkStage + C.SeparatorSpace + stage);
            }
        }

        /// <summary>
        /// Adds related idol identifiers from event and payload.
        /// </summary>
        private void AddRelatedIdols(List<int> destination, IMDataCoreEvent ev, JSONNode payload)
        {
            if (destination == null)
            {
                return;
            }

            AddRelatedId(destination, ev != null ? ev.IdolId : C.InvalidId);
            if (payload != null)
            {
                for (int i = C.ZeroIndex; i < C.RelatedIdFields.Length; i++)
                {
                    AddRelatedId(destination, ReadId(payload, C.RelatedIdFields[i]));
                }

                for (int fieldIndex = C.ZeroIndex; fieldIndex < C.RelatedIdListFields.Length; fieldIndex++)
                {
                    List<int> listIds = ParseIdList(ReadStr(payload, C.RelatedIdListFields[fieldIndex]));
                    for (int idIndex = C.ZeroIndex; idIndex < listIds.Count; idIndex++)
                    {
                        AddRelatedId(destination, listIds[idIndex]);
                    }
                }

                AddRelatedIdolsFromActorSummary(destination, ReadStr(payload, C.KeyActorsSummary));
            }

            AddCliqueRelatedIdolsForTimeline(destination, ev, payload);
            AddShowCastRelatedIdolsForTimeline(destination, ev, payload);
            FilterHiddenSocialRelatedIds(destination, ev, payload);
        }

        /// <summary>
        /// Adds idol ids from narrative actor-summary payloads.
        /// </summary>
        private void AddRelatedIdolsFromActorSummary(List<int> destination, string actorsSummary)
        {
            if (destination == null)
            {
                return;
            }

            string raw = NormalizeRawText(actorsSummary);
            if (raw == C.LabelUnknown)
            {
                return;
            }

            string[] entries = raw.Split(new[] { C.SeparatorPipeCharacter }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = C.ZeroIndex; i < entries.Length; i++)
            {
                string[] fields = entries[i].Split(new[] { C.SeparatorColonCharacter }, StringSplitOptions.None);
                if (fields.Length < C.ActorSummaryFieldCount)
                {
                    continue;
                }

                if (!string.Equals(fields[C.ActorSummaryKindField], C.KindIdol, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int idolId;
                if (TryParseInt(fields[C.ActorSummaryIdField], out idolId))
                {
                    AddRelatedId(destination, idolId);
                }
            }
        }

        /// <summary>
        /// Adds clique participant ids from clique payload lists and clique entity id fallback.
        /// </summary>
        private void AddCliqueRelatedIdolsForTimeline(List<int> destination, IMDataCoreEvent ev, JSONNode payload)
        {
            if (destination == null || ev == null || !IsCliqueTimelineContextEvent(ev.EventType))
            {
                return;
            }

            AddRelatedIdsFromCsv(destination, ReadStr(payload, C.KeyCliqueMemberIdList));
            AddRelatedIdsFromCsv(destination, ReadStr(payload, C.KeyCliqueMemberIdListBefore));
            AddRelatedIdsFromCsv(destination, ReadStr(payload, C.KeyCliqueMemberIdListAfter));
            AddRelatedIdsFromCsv(destination, ReadStr(payload, C.KeyCliqueMemberIdListAdded));
            AddRelatedIdsFromCsv(destination, ReadStr(payload, C.KeyCliqueMemberIdListRemoved));

            List<int> memberIdsFromEntity = ParseCliqueMemberIdsFromEntityId(ev.EntityId);
            for (int i = C.ZeroIndex; i < memberIdsFromEntity.Count; i++)
            {
                AddRelatedId(destination, memberIdsFromEntity[i]);
            }
        }

        /// <summary>
        /// Adds participant ids from one comma-separated id list.
        /// </summary>
        private void AddRelatedIdsFromCsv(List<int> destination, string rawList)
        {
            if (destination == null || string.IsNullOrEmpty(rawList))
            {
                return;
            }

            List<int> ids = ParseIdList(rawList);
            for (int i = C.ZeroIndex; i < ids.Count; i++)
            {
                AddRelatedId(destination, ids[i]);
            }
        }

        /// <summary>
        /// Parses clique entity-id member ids from numeric chunks (for example: 17-18-20-24).
        /// </summary>
        private static List<int> ParseCliqueMemberIdsFromEntityId(string entityId)
        {
            List<int> ids = new List<int>();
            if (string.IsNullOrEmpty(entityId))
            {
                return ids;
            }

            System.Text.StringBuilder numberBuffer = new System.Text.StringBuilder();
            for (int i = C.ZeroIndex; i < entityId.Length; i++)
            {
                char current = entityId[i];
                if (current >= '0' && current <= '9')
                {
                    numberBuffer.Append(current);
                    continue;
                }

                if (numberBuffer.Length == C.ZeroIndex)
                {
                    continue;
                }

                int parsedId;
                if (TryParseInt(numberBuffer.ToString(), out parsedId))
                {
                    ids.Add(parsedId);
                }

                numberBuffer.Length = C.ZeroIndex;
            }

            if (numberBuffer.Length > C.ZeroIndex)
            {
                int parsedId;
                if (TryParseInt(numberBuffer.ToString(), out parsedId))
                {
                    ids.Add(parsedId);
                }
            }

            return ids;
        }

        /// <summary>
        /// Adds one related idol id if valid and unique.
        /// </summary>
        private void AddRelatedId(List<int> destination, int id)
        {
            if (destination == null || id < C.MinId)
            {
                return;
            }

            if (data_girls.GetGirlByID(id) == null)
            {
                return;
            }

            if (idol != null && idol.id == id)
            {
                return;
            }

            for (int i = C.ZeroIndex; i < destination.Count; i++)
            {
                if (destination[i] == id)
                {
                    return;
                }
            }

            destination.Add(id);
        }

        /// <summary>
        /// Adds show cast ids for show lifecycle/status/episode timeline rows so the row can render cast portraits.
        /// </summary>
        private void AddShowCastRelatedIdolsForTimeline(List<int> destination, IMDataCoreEvent ev, JSONNode payload)
        {
            if (destination == null || ev == null || !IsShowCastTimelineContextEvent(ev.EventType))
            {
                return;
            }

            bool includeHistoryAndLiveState = !IsShowEpisodeTimelineContextEvent(ev.EventType);
            List<int> showCastIds = ResolveShowCastIdsForTimelineEvent(ev, payload, includeHistoryAndLiveState);
            for (int i = C.ZeroIndex; i < showCastIds.Count; i++)
            {
                AddRelatedId(destination, showCastIds[i]);
            }
        }

        /// <summary>
        /// Returns true when timeline event type represents one show episode release entry.
        /// </summary>
        private static bool IsShowEpisodeTimelineContextEvent(string eventType)
        {
            string canonicalType = CanonicalizeTimelineEventType(eventType ?? string.Empty);
            return string.Equals(canonicalType, C.EventShowEpisodeReleased, StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns true when timeline event type is clique join/leave.
        /// </summary>
        private static bool IsCliqueTimelineContextEvent(string eventType)
        {
            string canonicalType = CanonicalizeTimelineEventType(eventType ?? string.Empty);
            return string.Equals(canonicalType, C.EventCliqueJoined, StringComparison.Ordinal) ||
                string.Equals(canonicalType, C.EventCliqueLeft, StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns true when timeline event type should include show-cast participant portraits.
        /// </summary>
        private static bool IsShowCastTimelineContextEvent(string eventType)
        {
            string canonicalType = CanonicalizeTimelineEventType(eventType ?? string.Empty);
            switch (canonicalType)
            {
                case C.EventShowCreated:
                case C.EventShowReleased:
                case C.EventShowCancelled:
                case C.EventShowStatusChanged:
                case C.EventShowEpisodeReleased:
                case C.EventShowCastChanged:
                case C.EventShowConfigurationChanged:
                case C.EventShowRelaunchStarted:
                case C.EventShowRelaunchFinished:
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Removes related idols that are hidden by social visibility rules.
        /// </summary>
        private void FilterHiddenSocialRelatedIds(List<int> destination, IMDataCoreEvent ev, JSONNode payload)
        {
            if (destination == null || destination.Count == C.ZeroIndex || !IsSocialRelationshipEventType(ev != null ? ev.EventType : string.Empty))
            {
                return;
            }

            for (int i = destination.Count - C.LastFromCount; i >= C.ZeroIndex; i--)
            {
                if (!ShouldRevealSocialParticipant(ev, payload, destination[i]))
                {
                    destination.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Returns one participant name if visible under current social visibility rules.
        /// </summary>
        private string ResolveVisibleSocialParticipantName(IMDataCoreEvent ev, JSONNode payload, int participantId)
        {
            if (participantId < C.MinId || !ShouldRevealSocialParticipant(ev, payload, participantId))
            {
                return C.LabelUnknown;
            }

            return ResolveIdolNameById(participantId);
        }

        /// <summary>
        /// Returns true when one related idol participant should be visible.
        /// </summary>
        private bool ShouldRevealSocialParticipant(IMDataCoreEvent ev, JSONNode payload, int participantId)
        {
            data_girls.girls participantGirl = data_girls.GetGirlByID(participantId);
            if (participantId < C.MinId || participantGirl == null)
            {
                return false;
            }

            if (!IsSocialRelationshipEventType(ev != null ? ev.EventType : string.Empty))
            {
                return true;
            }

            if (idol != null && idol.id == participantId)
            {
                return true;
            }

            if (DiarySettings.ShouldShowUnknownSocialParticipants())
            {
                return true;
            }

            if (IsSocialParticipantKnownToPlayer(participantGirl))
            {
                return true;
            }

            bool knownToProducer;
            if (TryResolveSocialKnownToProducer(payload, out knownToProducer))
            {
                if (!knownToProducer)
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true when one idol is known enough to the player for social-detail disclosure.
        /// </summary>
        private bool IsSocialParticipantKnownToPlayer(data_girls.girls participantGirl)
        {
            if (participantGirl == null)
            {
                return false;
            }

            if (participantGirl.RelationshipsKnown)
            {
                return true;
            }

            Relationships._clique participantClique = participantGirl.GetClique();
            if (participantClique != null)
            {
                if (participantClique.Known)
                {
                    return true;
                }

                if (ContainsGirlById(participantClique.KnownBulliedGirls, participantGirl.id))
                {
                    return true;
                }
            }

            if (idol != null && idol.id >= C.MinId)
            {
                Relationships._relationship relationship = Relationships.GetRelationship(idol, participantGirl);
                if (relationship != null && (relationship.IsRelationshipKnown() || relationship.IsDatingAndKnown()))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true when one idol list contains the requested idol id.
        /// </summary>
        private static bool ContainsGirlById(List<data_girls.girls> girls, int idolId)
        {
            if (girls == null || idolId < C.MinId)
            {
                return false;
            }

            for (int i = C.ZeroIndex; i < girls.Count; i++)
            {
                data_girls.girls girl = girls[i];
                if (girl != null && girl.id == idolId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true when payload explicitly carries social-knowledge visibility.
        /// </summary>
        private static bool TryResolveSocialKnownToProducer(JSONNode payload, out bool knownToProducer)
        {
            knownToProducer = true;
            if (payload == null)
            {
                return false;
            }

            if (HasPayloadValue(payload, C.JsonRelationshipKnownToPlayer))
            {
                knownToProducer = ReadBool(payload, C.JsonRelationshipKnownToPlayer);
                return true;
            }

            if (HasPayloadValue(payload, C.JsonBullyingKnownToPlayer))
            {
                knownToProducer = ReadBool(payload, C.JsonBullyingKnownToPlayer);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true when event type belongs to social/relationship systems.
        /// </summary>
        private static bool IsSocialRelationshipEventType(string eventType)
        {
            string canonicalType = CanonicalizeTimelineEventType(eventType ?? string.Empty);
            switch (canonicalType)
            {
                case C.EventDatingPartnerStatusChanged:
                case C.EventIdolDatingStatusChanged:
                case C.EventIdolDatingStarted:
                case C.EventIdolDatingEnded:
                case C.EventIdolRelationshipStatusChanged:
                case C.EventBullyingStarted:
                case C.EventBullyingEnded:
                case C.EventCliqueJoined:
                case C.EventCliqueLeft:
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Resolves the other idol in one relationship payload relative to current idol.
        /// </summary>
        private int ResolveOtherRelationshipIdolId(IMDataCoreEvent ev, JSONNode payload)
        {
            int currentIdolId = idol != null && idol.id >= C.MinId ? idol.id : ResolveIdFromEvent(ev);
            int idolAId = ReadId(payload, C.JsonRelationshipIdolAId);
            int idolBId = ReadId(payload, C.JsonRelationshipIdolBId);

            if (idolAId >= C.MinId && idolBId >= C.MinId)
            {
                if (idolAId == currentIdolId)
                {
                    return idolBId;
                }

                if (idolBId == currentIdolId)
                {
                    return idolAId;
                }
            }

            int[] candidates = new[]
            {
                idolAId,
                idolBId,
                ReadId(payload, C.JsonBullyingTargetId),
                ReadId(payload, C.JsonBullyingLeaderId),
                ReadId(payload, C.KeyPartnerId),
                ReadId(payload, C.KeyOtherId),
                ReadId(payload, C.KeyIdolAId),
                ReadId(payload, C.KeyIdolBId)
            };

            for (int i = C.ZeroIndex; i < candidates.Length; i++)
            {
                int candidate = candidates[i];
                if (candidate < C.MinId || candidate == currentIdolId)
                {
                    continue;
                }

                if (data_girls.GetGirlByID(candidate) != null)
                {
                    return candidate;
                }
            }

            return C.InvalidId;
        }

        /// <summary>
        /// Resolves dating partner idol id from payload first, then from live relationship state.
        /// </summary>
        private bool TryResolveDatingPartnerId(IMDataCoreEvent ev, JSONNode payload, out int partnerId)
        {
            partnerId = C.InvalidId;
            int primaryIdolId = ResolveIdFromEvent(ev);
            if (primaryIdolId < C.MinId && idol != null)
            {
                primaryIdolId = idol.id;
            }

            if (payload != null)
            {
                int[] candidates = new[]
                {
                    ReadId(payload, C.KeyPartnerId),
                    ReadId(payload, C.KeyOtherId),
                    ReadId(payload, C.KeyIdolAId),
                    ReadId(payload, C.KeyIdolBId),
                    ReadId(payload, C.JsonRelationshipIdolAId),
                    ReadId(payload, C.JsonRelationshipIdolBId)
                };

                for (int i = C.ZeroIndex; i < candidates.Length; i++)
                {
                    int candidate = candidates[i];
                    if (candidate < C.MinId || candidate == primaryIdolId)
                    {
                        continue;
                    }

                    if (data_girls.GetGirlByID(candidate) != null)
                    {
                        partnerId = candidate;
                        return true;
                    }
                }
            }

            return TryResolveCurrentDatingPartnerId(primaryIdolId, out partnerId);
        }

        /// <summary>
        /// Resolves current dating partner from live relationship data.
        /// </summary>
        private static bool TryResolveCurrentDatingPartnerId(int idolId, out int partnerId)
        {
            partnerId = C.InvalidId;
            if (idolId < C.MinId)
            {
                return false;
            }

            data_girls.girls currentGirl = data_girls.GetGirlByID(idolId);
            if (currentGirl == null)
            {
                return false;
            }

            List<Relationships._relationship> relationships = Relationships.GetAllRelationships(currentGirl, false);
            if (relationships == null)
            {
                return false;
            }

            for (int i = C.ZeroIndex; i < relationships.Count; i++)
            {
                Relationships._relationship relationship = relationships[i];
                if (relationship == null || !relationship.Dating)
                {
                    continue;
                }

                data_girls.girls otherGirl = relationship.GetOtherGirl(currentGirl);
                if (otherGirl != null && otherGirl.id >= C.MinId)
                {
                    partnerId = otherGirl.id;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Builds one compact timeline row label.
        /// </summary>
        private static string BuildRowText(Presentation p)
        {
            if (p == null)
            {
                return C.LabelUnknown;
            }

            string text = p.Date + C.SeparatorDoubleSpace + p.Title;
            if (!string.IsNullOrEmpty(p.WithWhom))
            {
                text += C.MetadataPipeSeparator + p.WithWhom;
            }

            if (text.Length > C.MaxTimelineRowChars)
            {
                text = text.Substring(C.ZeroIndex, C.MaxTimelineRowChars);
            }

            return text;
        }

        /// <summary>
        /// Resolves single title display text.
        /// </summary>
        private static string ResolveSingleTitle(IMDataCoreEvent ev, JSONNode payload)
        {
            string title = HumanizeUnknown(ReadStr(payload, C.JsonSingleTitle));
            return title != C.LabelUnknown ? title : (C.TextSingle + Normalize(ev != null ? ev.EntityId : string.Empty));
        }

        /// <summary>
        /// Resolves the humanized show medium label from payload.
        /// </summary>
        private static string ResolveShowMediumLabel(JSONNode payload)
        {
            return HumanizeUnknown(ReadStr(payload, C.KeyShowMediumTitle));
        }

        /// <summary>
        /// Resolves show type wording, including medium when known.
        /// </summary>
        private static string ResolveShowTypeLabel(JSONNode payload)
        {
            string medium = ResolveShowMediumLabel(payload);
            if (medium == C.LabelUnknown)
            {
                return C.LabelShow;
            }

            return string.Format(CultureInfo.InvariantCulture, C.TextShowTypeWithMediumFormat, medium);
        }

        /// <summary>
        /// Builds one show event title that includes medium when known.
        /// </summary>
        private static string BuildShowEventTitle(JSONNode payload, string fallbackTitle, string actionTitle)
        {
            string showType = ResolveShowTypeLabel(payload);
            if (showType == C.LabelShow)
            {
                return fallbackTitle;
            }

            return string.Format(CultureInfo.InvariantCulture, C.TextShowEventWithMediumFormat, showType, actionTitle);
        }

        /// <summary>
        /// Builds one fallback show label using title-less entity id.
        /// </summary>
        private static string BuildShowEntityLabel(IMDataCoreEvent ev, JSONNode payload)
        {
            string showId = Normalize(ev != null ? ev.EntityId : string.Empty);
            string showType = ResolveShowTypeLabel(payload);
            if (showType == C.LabelShow)
            {
                return C.TextShow + showId;
            }

            return string.Format(CultureInfo.InvariantCulture, C.TextShowIdWithMediumPrefixFormat, showType) + showId;
        }

        /// <summary>
        /// Builds show title prefix, including medium when known.
        /// </summary>
        private static string BuildShowTitlePrefix(JSONNode payload)
        {
            string showType = ResolveShowTypeLabel(payload);
            if (showType == C.LabelShow)
            {
                return C.TextShowTitlePrefix;
            }

            return string.Format(CultureInfo.InvariantCulture, C.TextShowTitleWithMediumPrefixFormat, showType);
        }

        /// <summary>
        /// Resolves show title display text.
        /// </summary>
        private static string ResolveShowTitle(IMDataCoreEvent ev, JSONNode payload)
        {
            string title = NormalizeRawText(ReadStr(payload, C.JsonShowTitle));
            if (title == C.LabelUnknown)
            {
                title = NormalizeRawText(ReadStr(payload, C.KeyShowTitleAfter));
            }

            if (title == C.LabelUnknown)
            {
                title = NormalizeRawText(ReadStr(payload, C.KeyShowTitleBefore));
            }

            return title != C.LabelUnknown ? title : BuildShowEntityLabel(ev, payload);
        }

        /// <summary>
        /// Resolves group title display text.
        /// </summary>
        private static string ResolveGroupTitle(IMDataCoreEvent ev, JSONNode payload)
        {
            string title = NormalizeRawText(ReadStr(payload, C.KeyGroupTitle));
            if (title != C.LabelUnknown)
            {
                return title;
            }

            int groupId = ReadId(payload, C.JsonGroupId);
            if (groupId < C.MinId && ev != null)
            {
                int parsedGroupId;
                if (TryParseInt(ev.EntityId, out parsedGroupId))
                {
                    groupId = parsedGroupId;
                }
            }

            return groupId >= C.MinId ? ResolveGroupNameById(groupId) : C.LabelUnknown;
        }

        /// <summary>
        /// Adds one humanized code line if value is present.
        /// </summary>
        private static void AddCodeLineIfKnown(List<string> lines, string label, string rawCode)
        {
            string value = HumanizeUnknown(rawCode);
            if (value == C.LabelUnknown)
            {
                return;
            }

            lines.Add(label + C.SeparatorColonSpace + value);
        }

        /// <summary>
        /// Adds one raw text line if value is present.
        /// </summary>
        private static void AddRawLineIfKnown(List<string> lines, string label, string rawText)
        {
            string value = NormalizeRawText(rawText);
            if (value == C.LabelUnknown)
            {
                return;
            }

            lines.Add(label + C.SeparatorColonSpace + value);
        }

        /// <summary>
        /// Adds one transition line from code-like values.
        /// </summary>
        private static void AddTransitionLine(List<string> lines, string label, string beforeCode, string afterCode)
        {
            string transition = BuildStatusTransitionText(beforeCode, afterCode);
            if (string.IsNullOrEmpty(transition))
            {
                return;
            }

            lines.Add(label + C.SeparatorColonSpace + transition);
        }

        /// <summary>
        /// Adds one transition line from raw values.
        /// </summary>
        private static void AddRawTransitionLine(List<string> lines, string label, string beforeRaw, string afterRaw)
        {
            string before = NormalizeRawText(beforeRaw);
            string after = NormalizeRawText(afterRaw);
            if (before == C.LabelUnknown && after == C.LabelUnknown)
            {
                return;
            }

            if (before == C.LabelUnknown)
            {
                lines.Add(label + C.SeparatorColonSpace + after);
                return;
            }

            if (after == C.LabelUnknown)
            {
                lines.Add(label + C.SeparatorColonSpace + before);
                return;
            }

            if (string.Equals(before, after, StringComparison.OrdinalIgnoreCase))
            {
                lines.Add(label + C.SeparatorColonSpace + after);
                return;
            }

            lines.Add(label + C.SeparatorColonSpace + before + C.SeparatorArrow + after);
        }

        /// <summary>
        /// Adds one date line when payload contains a valid round-trip timestamp.
        /// </summary>
        private static void AddDateLineIfKnown(List<string> lines, string label, string roundTripDate)
        {
            string date = FormatRoundTripDateForUi(roundTripDate);
            if (date == C.LabelUnknown)
            {
                return;
            }

            lines.Add(label + C.SeparatorColonSpace + date);
        }

        /// <summary>
        /// Adds a graduation date for an idol joining the agency.  A missing
        /// date and the game's 1900-01-01 sentinel both mean the schedule is
        /// unknown, but the field should remain visible to the player.
        /// </summary>
        private static void AddGraduationDateLine(List<string> lines, string roundTripDate)
        {
            DateTime parsed;
            if (TryParseEventDate(roundTripDate, out parsed) && parsed.Year != C.UnknownGraduationYear)
            {
                lines.Add(C.TextGraduationDate + C.SeparatorColonSpace + FormatUiDate(parsed));
                return;
            }

            int dateKey;
            if (TryParseInt(roundTripDate, out dateKey) && TryParseDateKeyForUi(dateKey, out parsed) &&
                parsed.Year != C.UnknownGraduationYear)
            {
                lines.Add(C.TextGraduationDate + C.SeparatorColonSpace + FormatUiDate(parsed));
                return;
            }

            lines.Add(C.TextGraduationDate + C.SeparatorColonSpace + C.LabelUnknown);
        }

        /// <summary>
        /// Adds one date transition line from round-trip date values.
        /// </summary>
        private static void AddDateTransitionLineIfKnown(List<string> lines, string label, string beforeRoundTripDate, string afterRoundTripDate)
        {
            string before = FormatRoundTripDateForUi(beforeRoundTripDate);
            string after = FormatRoundTripDateForUi(afterRoundTripDate);
            bool beforeUnknown = before == C.LabelUnknown;
            bool afterUnknown = after == C.LabelUnknown;
            if (beforeUnknown && afterUnknown)
            {
                return;
            }

            if (beforeUnknown)
            {
                lines.Add(label + C.SeparatorColonSpace + after);
                return;
            }

            if (afterUnknown)
            {
                lines.Add(label + C.SeparatorColonSpace + before);
                return;
            }

            if (string.Equals(before, after, StringComparison.Ordinal))
            {
                lines.Add(label + C.SeparatorColonSpace + after);
                return;
            }

            lines.Add(label + C.SeparatorColonSpace + before + C.SeparatorArrow + after);
        }

        /// <summary>
        /// Adds one integer line when payload field exists.
        /// </summary>
        private static void AddIntLineIfPresent(List<string> lines, string label, JSONNode payload, string field)
        {
            int value;
            if (!TryReadIntField(payload, field, out value))
            {
                return;
            }

            lines.Add(label + C.SeparatorColonSpace + value.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Adds one long line when payload field exists.
        /// </summary>
        private static void AddLongLineIfPresent(List<string> lines, string label, JSONNode payload, string field)
        {
            long value;
            if (!TryReadLongField(payload, field, out value))
            {
                return;
            }

            lines.Add(label + C.SeparatorColonSpace + value.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Adds one bool line when payload field exists.
        /// </summary>
        private static void AddBoolLineIfPresent(List<string> lines, string label, JSONNode payload, string field)
        {
            bool value;
            if (!TryReadBoolField(payload, field, out value))
            {
                return;
            }

            lines.Add(label + C.SeparatorColonSpace + YesNo(value));
        }

        /// <summary>
        /// Adds one integer line when value is positive.
        /// </summary>
        private static void AddIntLineIfPositive(List<string> lines, string label, int value)
        {
            if (value <= C.ZeroIndex)
            {
                return;
            }

            lines.Add(label + C.SeparatorColonSpace + value.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Adds one long line when value is non-zero.
        /// </summary>
        private static void AddLongLineIfNonZero(List<string> lines, string label, long value)
        {
            if (value == C.LongZero)
            {
                return;
            }

            lines.Add(label + C.SeparatorColonSpace + value.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Adds one signed line when value is non-zero.
        /// </summary>
        private static void AddSignedLineIfNonZero(List<string> lines, string label, long value)
        {
            if (value == C.LongZero)
            {
                return;
            }

            lines.Add(label + C.SeparatorColonSpace + FormatSignedNumber(value));
        }

        /// <summary>
        /// Adds one integer transition line when both values exist.
        /// </summary>
        private static void AddIntTransitionLine(List<string> lines, string label, JSONNode payload, string beforeField, string afterField)
        {
            int beforeValue;
            int afterValue;
            if (!TryReadIntField(payload, beforeField, out beforeValue) || !TryReadIntField(payload, afterField, out afterValue))
            {
                return;
            }

            lines.Add(
                label + C.SeparatorColonSpace +
                beforeValue.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture) +
                C.SeparatorArrow +
                afterValue.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Adds one long transition line when both values exist.
        /// </summary>
        private static void AddLongTransitionLine(List<string> lines, string label, JSONNode payload, string beforeField, string afterField)
        {
            long beforeValue;
            long afterValue;
            if (!TryReadLongField(payload, beforeField, out beforeValue) || !TryReadLongField(payload, afterField, out afterValue))
            {
                return;
            }

            lines.Add(
                label + C.SeparatorColonSpace +
                beforeValue.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture) +
                C.SeparatorArrow +
                afterValue.ToString(C.FormatNumberNoDecimal, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Adds one float transition line when both values exist.
        /// </summary>
        private static void AddFloatTransitionLine(List<string> lines, string label, JSONNode payload, string beforeField, string afterField)
        {
            float beforeValue;
            float afterValue;
            if (!TryReadFloatField(payload, beforeField, out beforeValue) || !TryReadFloatField(payload, afterField, out afterValue))
            {
                return;
            }

            lines.Add(
                label + C.SeparatorColonSpace +
                beforeValue.ToString(C.FormatSingleMetricTwoDecimals, CultureInfo.InvariantCulture) +
                C.SeparatorArrow +
                afterValue.ToString(C.FormatSingleMetricTwoDecimals, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Adds one bool transition line when both values exist.
        /// </summary>
        private static void AddBoolTransitionLine(List<string> lines, string label, JSONNode payload, string beforeField, string afterField)
        {
            bool beforeValue;
            bool afterValue;
            if (!TryReadBoolField(payload, beforeField, out beforeValue) || !TryReadBoolField(payload, afterField, out afterValue))
            {
                return;
            }

            if (beforeValue == afterValue)
            {
                lines.Add(label + C.SeparatorColonSpace + YesNo(afterValue));
                return;
            }

            lines.Add(label + C.SeparatorColonSpace + YesNo(beforeValue) + C.SeparatorArrow + YesNo(afterValue));
        }

        /// <summary>
        /// Tries to read one integer field while preserving missing-field state.
        /// </summary>
        private static bool TryReadIntField(JSONNode payload, string field, out int value)
        {
            value = C.MinId;
            if (payload == null || string.IsNullOrEmpty(field))
            {
                return false;
            }

            JSONNode node = payload[field];
            if (IsMissingNode(node))
            {
                return false;
            }

            int parsed;
            if (int.TryParse(node.Value ?? string.Empty, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                value = parsed;
                return true;
            }

            value = node.AsInt;
            return true;
        }

        /// <summary>
        /// Tries to read one long field while preserving missing-field state.
        /// </summary>
        private static bool TryReadLongField(JSONNode payload, string field, out long value)
        {
            value = C.LongZero;
            if (payload == null || string.IsNullOrEmpty(field))
            {
                return false;
            }

            JSONNode node = payload[field];
            if (IsMissingNode(node))
            {
                return false;
            }

            return TryParseNodeLong(node, out value);
        }

        /// <summary>
        /// Tries to read one float field while preserving missing-field state.
        /// </summary>
        private static bool TryReadFloatField(JSONNode payload, string field, out float value)
        {
            value = 0f;
            if (payload == null || string.IsNullOrEmpty(field))
            {
                return false;
            }

            JSONNode node = payload[field];
            if (IsMissingNode(node))
            {
                return false;
            }

            string raw = node.Value ?? string.Empty;
            float parsedFloat;
            if (float.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out parsedFloat))
            {
                value = parsedFloat;
                return true;
            }

            value = node.AsFloat;
            return true;
        }

        /// <summary>
        /// Tries to read one bool field while preserving missing-field state.
        /// </summary>
        private static bool TryReadBoolField(JSONNode payload, string field, out bool value)
        {
            value = false;
            if (payload == null || string.IsNullOrEmpty(field))
            {
                return false;
            }

            JSONNode node = payload[field];
            if (IsMissingNode(node))
            {
                return false;
            }

            string raw = node.Value ?? string.Empty;
            bool parsedBool;
            if (bool.TryParse(raw, out parsedBool))
            {
                value = parsedBool;
                return true;
            }

            int parsedInt;
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedInt))
            {
                value = parsedInt != C.MinId;
                return true;
            }

            value = node.AsBool;
            return true;
        }

        /// <summary>
        /// Returns true when payload field exists and contains text.
        /// </summary>
        private static bool HasPayloadValue(JSONNode payload, string field)
        {
            if (payload == null || string.IsNullOrEmpty(field))
            {
                return false;
            }

            JSONNode node = payload[field];
            if (IsMissingNode(node))
            {
                return false;
            }

            string value = node.Value ?? string.Empty;
            return value.Length > C.ZeroIndex;
        }

        /// <summary>
        /// Normalizes raw text values while preserving original casing.
        /// </summary>
        private static string NormalizeRawText(string rawText)
        {
            if (string.IsNullOrEmpty(rawText))
            {
                return C.LabelUnknown;
            }

            string trimmed = rawText.Trim();
            if (trimmed.Length == C.ZeroIndex)
            {
                return C.LabelUnknown;
            }

            string localizedDate;
            if (TryLocalizeImDataCoreAbbreviatedMonthDate(trimmed, out localizedDate))
            {
                return localizedDate;
            }

            if (string.Equals(trimmed, C.LabelUnknown, StringComparison.OrdinalIgnoreCase))
            {
                return C.LabelUnknown;
            }

            // --- NEW LOCALIZATION PATCH START ---
            // Generates a safe key from the raw text for ModLocalization lookup.
            // e.g., "Broken Heel" becomes "RawText.Broken_Heel"
            char[] keyBuffer = trimmed.ToCharArray();
            for (int i = 0; i < keyBuffer.Length; i++)
            {
                char c = keyBuffer[i];
                // ModLocalization.NormalizeKey only allows letters, digits, ., _, and -
                if (!char.IsLetterOrDigit(c) && c != '.' && c != '_' && c != '-')
                {
                    keyBuffer[i] = '_';
                }
            }
            
            string safeKey = "RawText." + new string(keyBuffer);
            string localizedString = ModLocalization.Get(safeKey, string.Empty);
            
            if (!string.IsNullOrEmpty(localizedString))
            {
                return localizedString;
            }
            // --- NEW LOCALIZATION PATCH END ---

            string externalModText = ExternalModLocalizationCatalog.ResolveEnglishText(trimmed);
            if (!string.Equals(externalModText, trimmed, StringComparison.Ordinal))
            {
                return externalModText;
            }

            return trimmed;
        }

        /// <summary>
        /// Converts legacy IM Data Core date strings containing English month
        /// abbreviations before they are shown as otherwise-raw payload text.
        /// Dedicated date fields already pass through FormatRoundTripDateForUi;
        /// this covers dates stored in generic text fields by older Core data.
        /// </summary>
        private static bool TryLocalizeImDataCoreAbbreviatedMonthDate(string rawText, out string localizedDate)
        {
            localizedDate = string.Empty;
            if (!ContainsEnglishAbbreviatedMonthToken(rawText) || CountNumericRuns(rawText) < 2)
            {
                return false;
            }

            DateTime parsed;
            if (!TryParseEventDate(rawText, out parsed))
            {
                return false;
            }

            localizedDate = FormatUiDate(parsed);
            return true;
        }

        /// <summary>
        /// Detects a standalone English abbreviated month without matching a
        /// title that merely starts with the same letters.
        /// </summary>
        private static bool ContainsEnglishAbbreviatedMonthToken(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (int tokenIndex = C.ZeroIndex; tokenIndex < C.EnglishAbbreviatedMonthTokens.Length; tokenIndex++)
            {
                string token = C.EnglishAbbreviatedMonthTokens[tokenIndex];
                int searchIndex = C.ZeroIndex;
                while (searchIndex < value.Length)
                {
                    int matchIndex = value.IndexOf(token, searchIndex, StringComparison.OrdinalIgnoreCase);
                    if (matchIndex < C.ZeroIndex)
                    {
                        break;
                    }

                    int endIndex = matchIndex + token.Length;
                    bool hasLeftBoundary = matchIndex == C.ZeroIndex || !char.IsLetter(value[matchIndex - C.LastFromCount]);
                    bool hasRightBoundary = endIndex >= value.Length || !char.IsLetter(value[endIndex]);
                    if (hasLeftBoundary && hasRightBoundary)
                    {
                        return true;
                    }

                    searchIndex = endIndex;
                }
            }

            return false;
        }

        /// <summary>
        /// Counts numeric components so names such as "May" are not mistaken
        /// for dates. Core display dates contain at least day and year numbers.
        /// </summary>
        private static int CountNumericRuns(string value)
        {
            int count = C.ZeroIndex;
            bool inNumericRun = false;
            for (int index = C.ZeroIndex; index < (value ?? string.Empty).Length; index++)
            {
                bool isDigit = char.IsDigit(value[index]);
                if (isDigit && !inNumericRun)
                {
                    count++;
                }

                inNumericRun = isDigit;
            }

            return count;
        }

        /// <summary>
        /// Builds idol name summary from one CSV id list.
        /// </summary>
        private static string BuildIdolNameSummaryFromCsv(string rawList, int maxNames)
        {
            List<int> ids = ParseIdList(rawList);
            if (ids.Count == C.ZeroIndex)
            {
                return string.Empty;
            }

            List<string> names = new List<string>();
            for (int i = C.ZeroIndex; i < ids.Count; i++)
            {
                string name = ResolveIdolNameById(ids[i]);
                if (!string.IsNullOrEmpty(name) && name != C.LabelUnknown)
                {
                    names.Add(name);
                }
            }

            if (names.Count == C.ZeroIndex)
            {
                return string.Empty;
            }

            int limit = Mathf.Max(C.LastFromCount, maxNames);
            if (names.Count <= limit)
            {
                return string.Join(C.ListJoinSeparator, names.ToArray());
            }

            List<string> compact = new List<string>();
            for (int i = C.ZeroIndex; i < limit; i++)
            {
                compact.Add(names[i]);
            }

            return string.Join(C.ListJoinSeparator, compact.ToArray()) + C.SuffixExtraCount + (names.Count - limit).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Extracts idol names from random-event actor summary strings.
        /// </summary>
        private static string BuildActorSummary(string actorsSummary)
        {
            string raw = NormalizeRawText(actorsSummary);
            if (raw == C.LabelUnknown)
            {
                return string.Empty;
            }

            string[] entries = raw.Split(new[] { C.SeparatorPipeCharacter }, StringSplitOptions.RemoveEmptyEntries);
            List<string> names = new List<string>();
            for (int i = C.ZeroIndex; i < entries.Length; i++)
            {
                string entry = entries[i].Trim();
                if (entry.Length == C.ZeroIndex)
                {
                    continue;
                }

                string[] fields = entry.Split(new[] { C.SeparatorColonCharacter }, StringSplitOptions.None);
                if (fields.Length < C.ActorSummaryFieldCount)
                {
                    continue;
                }

                if (string.Equals(fields[C.ActorSummaryKindField], C.KindIdol, StringComparison.OrdinalIgnoreCase))
                {
                    string parsedName = NormalizeRawText(fields[C.ActorSummaryDisplayNameField]);
                    if (parsedName != C.LabelUnknown)
                    {
                        names.Add(parsedName);
                    }
                }
            }

            if (names.Count == C.ZeroIndex)
            {
                return string.Empty;
            }

            if (names.Count <= C.MaxNamesInOutcomeSummary)
            {
                return string.Join(C.ListJoinSeparator, names.ToArray());
            }

            List<string> compact = new List<string>();
            for (int i = C.ZeroIndex; i < C.MaxNamesInOutcomeSummary; i++)
            {
                compact.Add(names[i]);
            }

            return string.Join(C.ListJoinSeparator, compact.ToArray()) + C.SuffixExtraCount + (names.Count - C.MaxNamesInOutcomeSummary).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Builds a compact humanized summary from one delimited code/token list.
        /// </summary>
        private static string BuildHumanizedCodeListSummary(string rawList, char delimiter, int maxItems)
        {
            string raw = NormalizeRawText(rawList);
            if (raw == C.LabelUnknown)
            {
                return string.Empty;
            }

            string[] tokens = raw.Split(new[] { delimiter, ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> values = new List<string>();
            for (int i = C.ZeroIndex; i < tokens.Length; i++)
            {
                string token = tokens[i].Trim();
                if (token.Length == C.ZeroIndex)
                {
                    continue;
                }

                string value = HumanizeUnknown(token);
                if (value == C.LabelUnknown)
                {
                    continue;
                }

                values.Add(value);
            }

            if (values.Count == C.ZeroIndex)
            {
                return string.Empty;
            }

            int clampedMax = Mathf.Max(C.LastFromCount, maxItems);
            if (values.Count <= clampedMax)
            {
                return string.Join(C.ListJoinSeparator, values.ToArray());
            }

            List<string> compact = new List<string>();
            for (int i = C.ZeroIndex; i < clampedMax; i++)
            {
                compact.Add(values[i]);
            }

            return string.Join(C.ListJoinSeparator, compact.ToArray()) + C.SuffixExtraCount + (values.Count - clampedMax).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Reads one preferred payload field with one legacy fallback field.
        /// </summary>
        private static string ReadStrWithFallback(JSONNode payload, string field, string fallbackField)
        {
            string value = ReadStr(payload, field);
            return !string.IsNullOrEmpty(value)
                ? value
                : ReadStr(payload, fallbackField);
        }

        /// <summary>
        /// Normalizes one code/token value for comparisons.
        /// </summary>
        private static string NormalizeCodeToken(string rawCode)
        {
            if (string.IsNullOrEmpty(rawCode))
            {
                return string.Empty;
            }

            return rawCode.Trim().ToLowerInvariant().Replace(C.SeparatorDash, C.SeparatorUnderscore);
        }

        /// <summary>
        /// Resolves localized weekday text for cafe menu summaries.
        /// </summary>
        private static string ResolveWeekdayLabelByIndex(int dayIndex)
        {
            switch (dayIndex)
            {
                case C.WeekdayIndexMonday:
                    return string.IsNullOrEmpty(GetGameLocalization(C.LanguageKeyMonday)) ? C.TextWeekdayMonday : GetGameLocalization(C.LanguageKeyMonday);
                case C.WeekdayIndexTuesday:
                    return string.IsNullOrEmpty(GetGameLocalization(C.LanguageKeyTuesday)) ? C.TextWeekdayTuesday : GetGameLocalization(C.LanguageKeyTuesday);
                case C.WeekdayIndexWednesday:
                    return string.IsNullOrEmpty(GetGameLocalization(C.LanguageKeyWednesday)) ? C.TextWeekdayWednesday : GetGameLocalization(C.LanguageKeyWednesday);
                case C.WeekdayIndexThursday:
                    return string.IsNullOrEmpty(GetGameLocalization(C.LanguageKeyThursday)) ? C.TextWeekdayThursday : GetGameLocalization(C.LanguageKeyThursday);
                case C.WeekdayIndexFriday:
                    return string.IsNullOrEmpty(GetGameLocalization(C.LanguageKeyFriday)) ? C.TextWeekdayFriday : GetGameLocalization(C.LanguageKeyFriday);
                case C.WeekdayIndexSaturday:
                    return string.IsNullOrEmpty(GetGameLocalization(C.LanguageKeySaturday)) ? C.TextWeekdaySaturday : GetGameLocalization(C.LanguageKeySaturday);
                case C.WeekdayIndexSunday:
                    return string.IsNullOrEmpty(GetGameLocalization(C.LanguageKeySunday)) ? C.TextWeekdaySunday : GetGameLocalization(C.LanguageKeySunday);
                default:
                    return C.LabelUnknown;
            }
        }

        /// <summary>
        /// Resolves one cafe dish title by id with localized fallback text.
        /// </summary>
        private static string ResolveCafeDishTitle(int cafeId, int dishId)
        {
            if (dishId < C.MinId)
            {
                return C.TextNoCafeDishesSet;
            }

            Cafes._cafe cafe = Cafes.GetCafe(cafeId);
            Cafes._cafe._dish dish = cafe != null ? cafe.GetDishByID(dishId) : null;
            string dishTitle = NormalizeRawText(dish != null ? dish.Title : string.Empty);
            return dishTitle != C.LabelUnknown
                ? dishTitle
                : FormatLocalizedText(C.TextUnknownDishFormat, dishId.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Parses one compact cafe-menu summary into readable weekday -> dish text.
        /// </summary>
        private static string BuildCafeMenuDisplaySummary(int cafeId, string menuSummary)
        {
            string rawSummary = NormalizeRawText(menuSummary);
            if (rawSummary == C.LabelUnknown)
            {
                return string.Empty;
            }

            string[] entries = rawSummary.Split(new[] { C.SeparatorPipeCharacter }, StringSplitOptions.RemoveEmptyEntries);
            List<string> displayEntries = new List<string>();
            for (int entryIndex = C.ZeroIndex; entryIndex < entries.Length; entryIndex++)
            {
                string[] fields = entries[entryIndex].Split(new[] { C.SeparatorColonCharacter }, StringSplitOptions.None);
                if (fields.Length < C.CafeMenuEntryFieldCount)
                {
                    continue;
                }

                int dayIndex;
                int dishId;
                if (!TryParseInt(fields[C.CafeMenuDayIndexField], out dayIndex) || !TryParseInt(fields[C.CafeMenuDishIdField], out dishId))
                {
                    continue;
                }

                if (dishId < C.MinId)
                {
                    continue;
                }

                string weekdayLabel = ResolveWeekdayLabelByIndex(dayIndex);
                if (weekdayLabel == C.LabelUnknown)
                {
                    continue;
                }

                string dishTitle = ResolveCafeDishTitle(cafeId, dishId);
                displayEntries.Add(FormatLocalizedText(C.TextMenuDayEntry, weekdayLabel, dishTitle));
            }

            return displayEntries.Count > C.ZeroIndex
                ? string.Join(C.ListJoinSeparator, displayEntries.ToArray())
                : C.TextNoCafeDishesSet;
        }

        /// <summary>
        /// Parses active mentorship pairs into readable idol-name text.
        /// </summary>
        private static string BuildMentorPairsDisplaySummary(string mentorPairsSummary)
        {
            string rawSummary = NormalizeRawText(mentorPairsSummary);
            if (rawSummary == C.LabelUnknown)
            {
                return string.Empty;
            }

            string[] entries = rawSummary.Split(new[] { C.SeparatorPipeCharacter }, StringSplitOptions.RemoveEmptyEntries);
            List<string> displayPairs = new List<string>();
            for (int entryIndex = C.ZeroIndex; entryIndex < entries.Length; entryIndex++)
            {
                string[] fields = entries[entryIndex].Split(new[] { C.SeparatorColonCharacter }, StringSplitOptions.None);
                if (fields.Length < C.MentorPairEntryFieldCount)
                {
                    continue;
                }

                int mentorId;
                int kohaiId;
                if (!TryParseInt(fields[C.MentorPairMentorIdField], out mentorId) || !TryParseInt(fields[C.MentorPairKohaiIdField], out kohaiId))
                {
                    continue;
                }

                string mentorName = ResolveIdolNameById(mentorId);
                string kohaiName = ResolveIdolNameById(kohaiId);
                if (mentorName == C.LabelUnknown || kohaiName == C.LabelUnknown)
                {
                    continue;
                }

                displayPairs.Add(FormatLocalizedText(C.TextMentorPairFormat, mentorName, kohaiName));
            }

            return displayPairs.Count > C.ZeroIndex
                ? string.Join(C.ListJoinSeparator, displayPairs.ToArray())
                : string.Empty;
        }

        /// <summary>
        /// Returns true when one mentorship summary would only restate the focused pair.
        /// </summary>
        private static bool IsMentorPairsSummaryRedundant(string mentorPairsSummary, int mentorId, int kohaiId)
        {
            string rawSummary = NormalizeRawText(mentorPairsSummary);
            if (rawSummary == C.LabelUnknown)
            {
                return true;
            }

            string[] entries = rawSummary.Split(new[] { C.SeparatorPipeCharacter }, StringSplitOptions.RemoveEmptyEntries);
            if (entries.Length != C.LastFromCount)
            {
                return false;
            }

            string[] fields = entries[C.ZeroIndex].Split(new[] { C.SeparatorColonCharacter }, StringSplitOptions.None);
            if (fields.Length < C.MentorPairEntryFieldCount)
            {
                return false;
            }

            int parsedMentorId;
            int parsedKohaiId;
            return TryParseInt(fields[C.MentorPairMentorIdField], out parsedMentorId)
                && TryParseInt(fields[C.MentorPairKohaiIdField], out parsedKohaiId)
                && parsedMentorId == mentorId
                && parsedKohaiId == kohaiId;
        }

        /// <summary>
        /// Resolves readable story-event text from one substory identifier.
        /// </summary>
        private static string ResolveSubstoryDisplayNameFromId(string substoryId)
        {
            string normalizedId = NormalizeRawText(substoryId);
            if (normalizedId == C.LabelUnknown)
            {
                return C.LabelUnknown;
            }

            data_dialogues._dialogue dialogue = data_dialogues.GetDialogueByID(substoryId);
            if (dialogue != null)
            {
                string dialogueDisplayName = HumanizeUnknown(dialogue.id);
                if (dialogueDisplayName != C.LabelUnknown)
                {
                    return dialogueDisplayName;
                }
            }

            return HumanizeUnknown(substoryId);
        }

        /// <summary>
        /// Resolves one substory display name from optional payload display text or fallback id.
        /// </summary>
        private static string ResolveSubstoryDisplayName(JSONNode payload, string idField, string displayNameField)
        {
            string displayName = NormalizeRawText(ReadStr(payload, displayNameField));
            return displayName != C.LabelUnknown
                ? displayName
                : ResolveSubstoryDisplayNameFromId(ReadStr(payload, idField));
        }

        /// <summary>
        /// Builds one localized task summary from captured task fields.
        /// </summary>
        private static string BuildTaskSummary(JSONNode payload)
        {
            if (payload == null)
            {
                return string.Empty;
            }

            List<string> summaryParts = new List<string>();
            string customTask = NormalizeRawText(ReadStr(payload, C.KeyTaskCustom));
            if (customTask != C.LabelUnknown)
            {
                summaryParts.Add(HumanizeUnknown(customTask));
            }
            else
            {
                string taskSubstory = ResolveSubstoryDisplayNameFromId(ReadStr(payload, C.KeyTaskSubstory));
                if (taskSubstory != C.LabelUnknown)
                {
                    summaryParts.Add(FormatLocalizedText(C.TextTaskSummaryStoryPart, taskSubstory));
                }
            }

            string taskType = HumanizeUnknown(ReadStr(payload, C.KeyTaskType));
            if (taskType != C.LabelUnknown)
            {
                summaryParts.Add(FormatLocalizedText(C.TextTaskSummaryTypePart, taskType));
            }

            string taskGoal = HumanizeUnknown(ReadStr(payload, C.KeyTaskGoal));
            if (taskGoal != C.LabelUnknown)
            {
                summaryParts.Add(FormatLocalizedText(C.TextTaskSummaryGoalPart, taskGoal));
            }

            string taskRoute = HumanizeUnknown(ReadStrWithFallback(payload, C.KeyTaskRoute, C.KeyTaskRouteLegacy));
            if (taskRoute != C.LabelUnknown)
            {
                summaryParts.Add(FormatLocalizedText(C.TextTaskSummaryRoutePart, taskRoute));
            }

            string taskSkill = HumanizeUnknown(ReadStr(payload, C.KeyTaskSkill));
            if (taskSkill != C.LabelUnknown)
            {
                summaryParts.Add(FormatLocalizedText(C.TextTaskSummarySkillPart, taskSkill));
            }

            int taskIdolId = ReadId(payload, C.KeyTaskGirlId);
            if (taskIdolId >= C.MinId)
            {
                summaryParts.Add(FormatLocalizedText(C.TextTaskSummaryIdolPart, ResolveIdolNameById(taskIdolId)));
            }

            string taskAgentName = NormalizeRawText(ReadStr(payload, C.KeyTaskAgentName));
            if (taskAgentName != C.LabelUnknown)
            {
                summaryParts.Add(FormatLocalizedText(C.TextTaskSummaryAgentPart, taskAgentName));
            }

            return summaryParts.Count > C.ZeroIndex
                ? string.Join(C.ListJoinSeparator, summaryParts.ToArray())
                : string.Empty;
        }

        /// <summary>
        /// Builds readable rival-trend text for one rising/falling direction.
        /// </summary>
        private static string BuildTrendSummaryByDirection(string rawSummary, string directionCode)
        {
            string normalizedSummary = NormalizeRawText(rawSummary);
            if (normalizedSummary == C.LabelUnknown)
            {
                return string.Empty;
            }

            string normalizedDirection = NormalizeCodeToken(directionCode);
            string[] entries = normalizedSummary.Split(new[] { C.SeparatorPipeCharacter }, StringSplitOptions.RemoveEmptyEntries);
            List<string> displayEntries = new List<string>();
            for (int entryIndex = C.ZeroIndex; entryIndex < entries.Length; entryIndex++)
            {
                string[] fields = entries[entryIndex].Split(new[] { C.SeparatorColonCharacter }, StringSplitOptions.None);
                if (fields.Length < C.TrendSummaryMinimumFieldCount)
                {
                    continue;
                }

                string entryDirection = NormalizeCodeToken(fields[C.TrendSummaryDirectionField]);
                if (!string.Equals(entryDirection, normalizedDirection, StringComparison.Ordinal))
                {
                    continue;
                }

                string title = NormalizeRawText(fields[C.TrendSummaryTitleField]);
                if (title == C.LabelUnknown)
                {
                    continue;
                }

                int points;
                if (fields.Length > C.TrendSummaryPointsField && TryParseInt(fields[C.TrendSummaryPointsField], out points))
                {
                    displayEntries.Add(FormatLocalizedText(C.TextTrendEntryWithPoints, title, points.ToString(CultureInfo.InvariantCulture)));
                    continue;
                }

                displayEntries.Add(title);
            }

            return displayEntries.Count > C.ZeroIndex
                ? string.Join(C.ListJoinSeparator, displayEntries.ToArray())
                : string.Empty;
        }

        /// <summary>
        /// Resolves normalized player-date outcome code from new or legacy payload fields.
        /// </summary>
        private static string ResolveDateOutcomeCode(JSONNode payload)
        {
            string summaryCode = NormalizeCodeToken(ReadStr(payload, C.KeyDateResultSummaryCode));
            if (!string.IsNullOrEmpty(summaryCode))
            {
                return summaryCode;
            }

            string legacyToken = ReadStr(payload, C.KeyDateResultToken);
            string normalizedToken = NormalizeCodeToken(legacyToken);
            if (string.IsNullOrEmpty(normalizedToken))
            {
                return string.Empty;
            }

            if (normalizedToken.IndexOf(C.SeparatorPipe, StringComparison.Ordinal) >= C.ZeroIndex)
            {
                return C.CodeDateResultMultiResult;
            }

            switch (normalizedToken)
            {
                case C.CodeDateResultTokenPublic:
                    return C.CodeDateResultPublicDate;
                case C.CodeDateResultTokenGeneric:
                    return C.CodeDateResultRoutineDate;
                case C.CodeDateResultTokenDeferred:
                    return C.CodeDateResultDialogueFollowup;
                case C.CodeDateResultTokenNone:
                    return C.CodeDateResultNoSpecialResult;
                default:
                    return normalizedToken;
            }
        }

        /// <summary>
        /// Returns first token from one space-delimited payload string.
        /// </summary>
        private static string GetFirstToken(string rawValue)
        {
            string normalized = NormalizeRawText(rawValue);
            if (normalized == C.LabelUnknown)
            {
                return string.Empty;
            }

            int separatorIndex = normalized.IndexOf(C.SeparatorSpace, StringComparison.Ordinal);
            return separatorIndex > C.ZeroIndex
                ? normalized.Substring(C.ZeroIndex, separatorIndex)
                : normalized;
        }

        /// <summary>
        /// Builds actor-token lookup for random-event effect rendering.
        /// </summary>
        private static Dictionary<string, string> BuildRandomEventActorLookup(string actorsSummary)
        {
            Dictionary<string, string> actorLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string normalizedSummary = NormalizeRawText(actorsSummary);
            if (normalizedSummary == C.LabelUnknown)
            {
                return actorLookup;
            }

            string[] entries = normalizedSummary.Split(new[] { C.SeparatorPipeCharacter }, StringSplitOptions.RemoveEmptyEntries);
            for (int entryIndex = C.ZeroIndex; entryIndex < entries.Length; entryIndex++)
            {
                string[] fields = entries[entryIndex].Split(new[] { C.SeparatorColonCharacter }, StringSplitOptions.None);
                if (fields.Length < C.ActorSummaryFieldCount)
                {
                    continue;
                }

                string actorToken = NormalizeCodeToken(fields[C.ActorSummaryTokenField]);
                string actorDisplayName = NormalizeRawText(fields[C.ActorSummaryDisplayNameField]);
                if (string.IsNullOrEmpty(actorToken) || actorDisplayName == C.LabelUnknown)
                {
                    continue;
                }

                actorLookup[actorToken] = actorDisplayName;
            }

            return actorLookup;
        }

        /// <summary>
        /// Returns true when one resource effect already has a better dedicated delta row.
        /// </summary>
        private static bool ShouldSkipAggregateRandomResourceCode(string resourceCode)
        {
            string normalizedCode = NormalizeCodeToken(resourceCode);
            return normalizedCode == "money"
                || normalizedCode == "fans"
                || normalizedCode == "fame"
                || normalizedCode == "buzz";
        }

        /// <summary>
        /// Adds one humanized random-event effect line when supported by current parser rules.
        /// </summary>
        private static bool TryBuildRandomEffectLine(
            Dictionary<string, string> actorLookup,
            string target,
            string parameter,
            string formula,
            out string line)
        {
            line = string.Empty;
            string normalizedTarget = NormalizeCodeToken(target);
            string normalizedParameter = NormalizeCodeToken(parameter);
            long parsedAmount;

            if (normalizedTarget == C.CodeRandomEffectTargetResource)
            {
                if (ShouldSkipAggregateRandomResourceCode(normalizedParameter) || !long.TryParse(formula ?? string.Empty, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedAmount))
                {
                    return false;
                }

                string resourceName = HumanizeUnknown(parameter);
                if (resourceName == C.LabelUnknown)
                {
                    return false;
                }

                line = FormatLocalizedText(C.TextRandomEffectResourceChange, resourceName, FormatSignedNumber(parsedAmount));
                return true;
            }

            if (normalizedTarget == C.CodeRandomEffectTargetFans)
            {
                if (!long.TryParse(formula ?? string.Empty, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedAmount))
                {
                    return false;
                }

                string fanSegment = HumanizeUnknown(parameter);
                if (fanSegment == C.LabelUnknown)
                {
                    return false;
                }

                line = FormatLocalizedText(C.TextRandomEffectFanOpinionChange, fanSegment, FormatSignedNumber(parsedAmount));
                return true;
            }

            if (normalizedTarget == C.CodeRandomEffectTargetMeta)
            {
                string firstToken = GetFirstToken(formula);
                if (normalizedParameter == C.CodeRandomEffectParameterStartDialogue || normalizedParameter == C.CodeRandomEffectParameterStartDialogueSameActor)
                {
                    string storyName = ResolveSubstoryDisplayNameFromId(firstToken);
                    if (storyName == C.LabelUnknown)
                    {
                        return false;
                    }

                    line = FormatLocalizedText(C.TextRandomEffectTriggeredStoryEvent, storyName);
                    return true;
                }

                if (normalizedParameter == C.CodeRandomEffectParameterAddTask || normalizedParameter == C.CodeRandomEffectParameterAddTaskSub)
                {
                    string taskName = HumanizeUnknown(firstToken);
                    if (taskName == C.LabelUnknown)
                    {
                        return false;
                    }

                    line = FormatLocalizedText(C.TextRandomEffectAddedTask, taskName);
                    return true;
                }

                if (normalizedParameter == C.CodeRandomEffectParameterAddTaskContinue || normalizedParameter == C.CodeRandomEffectParameterAddTaskContinueSub)
                {
                    string taskName = HumanizeUnknown(firstToken);
                    if (taskName == C.LabelUnknown)
                    {
                        return false;
                    }

                    line = FormatLocalizedText(C.TextRandomEffectAddedFollowUpTask, taskName);
                    return true;
                }

                if (normalizedParameter == C.CodeRandomEffectParameterSetPolicy)
                {
                    string policyValue = HumanizeUnknown(formula);
                    if (policyValue == C.LabelUnknown)
                    {
                        return false;
                    }

                    line = FormatLocalizedText(C.TextRandomEffectSetPolicy, policyValue);
                    return true;
                }

                return false;
            }

            if ((parameter ?? string.Empty).StartsWith(C.CodeRandomEffectActorParameterPrefix, StringComparison.OrdinalIgnoreCase)
                && long.TryParse(formula ?? string.Empty, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedAmount))
            {
                string actorName;
                string normalizedActorTarget = NormalizeCodeToken(target);
                if (string.IsNullOrEmpty(normalizedActorTarget) || !actorLookup.TryGetValue(normalizedActorTarget, out actorName))
                {
                    actorName = HumanizeUnknown(target);
                }

                string parameterCode = parameter.Substring(C.CodeRandomEffectActorParameterPrefix.Length);
                string parameterName = HumanizeUnknown(parameterCode);
                if (actorName == C.LabelUnknown || parameterName == C.LabelUnknown)
                {
                    return false;
                }

                line = FormatLocalizedText(C.TextRandomEffectActorParameterChange, actorName, parameterName, FormatSignedNumber(parsedAmount));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Builds player-facing random-event effect lines from structured or legacy payload fields.
        /// </summary>
        private static List<string> BuildRandomEventEffectLines(JSONNode payload)
        {
            List<string> effectLines = new List<string>();
            if (payload == null)
            {
                return effectLines;
            }

            Dictionary<string, string> actorLookup = BuildRandomEventActorLookup(ReadStr(payload, C.KeyActorsSummary));
            JSONNode structuredEntries = payload[C.KeyReplyEffectEntries];
            if (!IsMissingNode(structuredEntries) && structuredEntries.Count > C.ZeroIndex)
            {
                for (int entryIndex = C.ZeroIndex; entryIndex < structuredEntries.Count; entryIndex++)
                {
                    JSONNode entry = structuredEntries[entryIndex];
                    string line;
                    if (!TryBuildRandomEffectLine(
                        actorLookup,
                        ReadStr(entry, C.KeyReplyEffectTarget),
                        ReadStr(entry, C.KeyReplyEffectParameter),
                        ReadStr(entry, C.KeyReplyEffectFormula),
                        out line))
                    {
                        continue;
                    }

                    if (!effectLines.Contains(line))
                    {
                        effectLines.Add(line);
                    }
                }

                return effectLines;
            }

            string legacySummary = NormalizeRawText(ReadStr(payload, C.KeyReplyEffectSummary));
            if (legacySummary == C.LabelUnknown)
            {
                return effectLines;
            }

            string[] entries = legacySummary.Split(new[] { C.SeparatorPipeCharacter }, StringSplitOptions.RemoveEmptyEntries);
            for (int entryIndex = C.ZeroIndex; entryIndex < entries.Length; entryIndex++)
            {
                string[] fields = entries[entryIndex].Split(new[] { C.SeparatorColonCharacter }, StringSplitOptions.None);
                if (fields.Length < C.ReplyEffectEntryMinimumFieldCount)
                {
                    continue;
                }

                string line;
                if (!TryBuildRandomEffectLine(
                    actorLookup,
                    fields[C.ReplyEffectTargetField],
                    fields[C.ReplyEffectParameterField],
                    fields[C.ReplyEffectFormulaField],
                    out line))
                {
                    continue;
                }

                if (!effectLines.Contains(line))
                {
                    effectLines.Add(line);
                }
            }

            return effectLines;
        }

        /// <summary>
        /// Returns selected event from cache.
        /// </summary>
        private IMDataCoreEvent FindSelectedEvent()
        {
            if (selectedEventId <= C.InvalidEventId)
            {
                return null;
            }

            for (int i = C.ZeroIndex; i < cachedEvents.Count; i++)
            {
                IMDataCoreEvent ev = cachedEvents[i];
                if (ev != null && ev.EventId == selectedEventId)
                {
                    return ev;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if cache contains event id.
        /// </summary>
        private bool ContainsEventId(long eventId)
        {
            if (eventId <= C.InvalidEventId)
            {
                return false;
            }

            for (int i = C.ZeroIndex; i < cachedEvents.Count; i++)
            {
                IMDataCoreEvent ev = cachedEvents[i];
                if (ev != null && ev.EventId == eventId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Opens idol profile by id.
        /// </summary>
        private bool OpenIdolProfile(int idolId)
        {
            if (idolId < C.MinId)
            {
                return false;
            }

            data_girls.girls girl = data_girls.GetGirlByID(idolId);
            if (girl == null)
            {
                return false;
            }

            mainScript main;
            PopupManager popup;
            if (!TryGetMainAndPopup(out main, out popup))
            {
                return false;
            }

            PopupManager._popup profilePopupEntry = popup.GetByType(PopupManager._type.girl_profile);
            if (profilePopupEntry == null || profilePopupEntry.obj == null)
            {
                return false;
            }

            Profile_Popup pp = profilePopupEntry.obj.GetComponent<Profile_Popup>();
            if (pp == null)
            {
                return false;
            }

            bool profileAlreadyActive = profilePopupEntry.open || profilePopupEntry.obj.activeInHierarchy;
            if (profileAlreadyActive)
            {
                TrimQueuedProfilePopupDuplicates(popup);
                EnsureProfilePopupEntryOpenWhenVisible(profilePopupEntry);
                pp.Set(girl);
                TrySyncBackdropWithActiveManagedPopups(popup);
                return true;
            }

            popup.Open(PopupManager._type.girl_profile, true);
            pp.Set(girl);
            return true;
        }

        /// <summary>
        /// Removes duplicate queued profile entries so switching idols does not stack stale profile transitions.
        /// </summary>
        private static void TrimQueuedProfilePopupDuplicates(PopupManager popup)
        {
            if (popup == null || popup.queue == null || popup.queue.Count <= C.LastFromCount)
            {
                return;
            }

            if (popup.queue[C.ZeroIndex] != PopupManager._type.girl_profile)
            {
                return;
            }

            for (int i = popup.queue.Count - C.LastFromCount; i >= C.LastFromCount; i--)
            {
                if (popup.queue[i] == PopupManager._type.girl_profile)
                {
                    popup.queue.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Repairs one visible profile popup entry that is active but not marked open.
        /// </summary>
        private static void EnsureProfilePopupEntryOpenWhenVisible(PopupManager._popup profilePopupEntry)
        {
            if (profilePopupEntry == null || profilePopupEntry.obj == null || profilePopupEntry.open || !profilePopupEntry.obj.activeInHierarchy)
            {
                return;
            }

            CanvasGroup group = profilePopupEntry.obj.GetComponent<CanvasGroup>();
            if (group != null && group.alpha <= C.PopupGhostAlphaThreshold)
            {
                return;
            }

            profilePopupEntry.open = true;
        }

        /// <summary>
        /// Resolves one canonical timeline row date from payload-first date sources.
        /// </summary>
        private static string ResolveTimelineRowDate(IMDataCoreEvent ev)
        {
            if (ev == null)
            {
                return C.LabelUnknown;
            }

            DateTime parsed;
            if (TryResolveTimelineDate(ev, out parsed))
            {
                return FormatUiDate(parsed);
            }

            if (ev.GameDateKey > C.ZeroIndex)
            {
                return ev.GameDateKey.ToString(CultureInfo.InvariantCulture);
            }

            return C.LabelUnknown;
        }

        /// <summary>
        /// Resolves one canonical timeline date for timeline ordering/labels.
        /// </summary>
        private static bool TryResolveTimelineDate(IMDataCoreEvent ev, out DateTime parsed)
        {
            parsed = DateTime.MinValue;
            if (ev == null)
            {
                return false;
            }

            JSONNode payload = ParsePayload(ev.PayloadJson);
            if (TryResolveTimelineDateFromPayload(payload, out parsed))
            {
                return true;
            }

            if (TryResolveSingleReleaseDateFromLiveData(ev, out parsed))
            {
                return true;
            }

            if (TryParseDateKeyForUi(ev.GameDateKey, out parsed))
            {
                return true;
            }

            return TryParseEventDate(ev.GameDateTime, out parsed);
        }

        /// <summary>
        /// Resolves release date from live single data when payload does not include a date field.
        /// </summary>
        private static bool TryResolveSingleReleaseDateFromLiveData(IMDataCoreEvent ev, out DateTime parsed)
        {
            parsed = DateTime.MinValue;
            if (!IsSingleReleaseEvent(ev))
            {
                return false;
            }

            int singleId;
            if (!TryParseInt(ev.EntityId, out singleId))
            {
                return false;
            }

            singles._single single = singles.GetSingleByID(singleId);
            if (single == null || single.ReleaseData == null)
            {
                return false;
            }

            DateTime releaseDate = single.ReleaseData.ReleaseDate;
            if (releaseDate.Year <= DateTime.MinValue.Year)
            {
                return false;
            }

            parsed = releaseDate;
            return true;
        }

        /// <summary>
        /// Attempts payload date extraction using a stable priority list of known event date fields.
        /// </summary>
        private static bool TryResolveTimelineDateFromPayload(JSONNode payload, out DateTime parsed)
        {
            parsed = DateTime.MinValue;
            if (payload == null)
            {
                return false;
            }

            for (int i = C.ZeroIndex; i < TimelinePayloadDateFieldPriority.Length; i++)
            {
                string raw = ReadStr(payload, TimelinePayloadDateFieldPriority[i]);
                if (TryParseTimelineDateValue(raw, out parsed))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Parses one timeline date value from round-trip or yyyymmdd key forms.
        /// </summary>
        private static bool TryParseTimelineDateValue(string rawDate, out DateTime parsed)
        {
            if (TryParseEventDate(rawDate, out parsed))
            {
                return true;
            }

            int parsedDateKey;
            return TryParseInt(rawDate, out parsedDateKey) && TryParseDateKeyForUi(parsedDateKey, out parsed);
        }

        /// <summary>
        /// Resolves date text from event datetime.
        /// </summary>
        private static string ResolveDate(IMDataCoreEvent ev)
        {
            if (ev == null)
            {
                return C.LabelUnknown;
            }

            DateTime dt;
            if (TryParseEventDate(ev.GameDateTime, out dt))
            {
                return FormatUiDate(dt);
            }

            if (TryParseDateKeyForUi(ev.GameDateKey, out dt))
            {
                return FormatUiDate(dt);
            }

            return ev.GameDateKey.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parses one payload/event date value using round-trip first, then generic fallback.
        /// </summary>
        private static bool TryParseEventDate(string rawDate, out DateTime parsed)
        {
            parsed = DateTime.MinValue;
            string candidate = rawDate ?? string.Empty;
            if (string.IsNullOrEmpty(candidate))
            {
                return false;
            }

            if (DateTime.TryParseExact(candidate, C.DateFormatRoundTrip, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed))
            {
                return true;
            }

            if (DateTime.TryParse(candidate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed))
            {
                return true;
            }

            return DateTime.TryParse(candidate, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
        }

        /// <summary>
        /// Parses game date key values in yyyymmdd shape.
        /// </summary>
        private static bool TryParseDateKeyForUi(int dateKey, out DateTime parsed)
        {
            parsed = DateTime.MinValue;
            if (dateKey <= C.ZeroIndex)
            {
                return false;
            }

            string candidate = dateKey.ToString(C.FormatEightDigits, CultureInfo.InvariantCulture);
            return DateTime.TryParseExact(candidate, C.FormatDateKeyYyyyMMdd, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
        }

        /// <summary>
        /// Resolves source label from kind/id with player-friendly wording.
        /// </summary>
        private string ResolveSourceLabel(IMDataCoreEvent ev, JSONNode payload)
        {
            if (ev == null)
            {
                return C.LabelUnknown;
            }

            string kind = ev.EntityKind ?? string.Empty;
            if (kind == C.KindShow)
            {
                string showTitle = HumanizeUnknown(ReadStr(payload, C.JsonShowTitle));
                return showTitle != C.LabelUnknown
                    ? BuildShowTitlePrefix(payload) + showTitle
                    : BuildShowEntityLabel(ev, payload);
            }

            if (kind == C.KindSingle)
            {
                string singleTitle = HumanizeUnknown(ReadStr(payload, C.JsonSingleTitle));
                return singleTitle != C.LabelUnknown
                    ? C.TextSingleTitlePrefix + singleTitle
                    : C.TextSingle + Normalize(ev.EntityId);
            }

            if (kind == C.KindGroup)
            {
                int groupId;
                if (TryParseInt(ev.EntityId, out groupId))
                {
                    return C.LabelSingleGroupPrefix + ResolveGroupNameById(groupId);
                }
            }

            if (kind == C.KindDatingRelationship || kind == C.KindIdolDatingState)
            {
                int idolId = ResolveIdFromEvent(ev);
                if (idolId >= C.MinId && ShouldRevealSocialParticipant(ev, payload, idolId))
                {
                    return C.LabelIdolPrefix + ResolveIdolNameById(idolId);
                }

                return C.LabelNotKnownToProducer;
            }

            if (kind == C.KindIdol || kind == C.KindStatus || kind == C.KindScandal || kind == C.KindMedical)
            {
                int idolId = ResolveIdFromEvent(ev);
                return C.LabelIdolPrefix + ResolveIdolNameById(idolId);
            }

            if (kind == C.KindRelationship)
            {
                int otherId = ResolveOtherRelationshipIdolId(ev, payload);
                if (otherId >= C.MinId && ShouldRevealSocialParticipant(ev, payload, otherId))
                {
                    return FormatLocalizedText(C.TextRelationshipWith, ResolveIdolNameById(otherId));
                }

                return C.LabelNotKnownToProducer;
            }

            if (kind == C.KindBullying)
            {
                int targetId = ReadId(payload, C.JsonBullyingTargetId);
                int leaderId = ReadId(payload, C.JsonBullyingLeaderId);
                string visibleLeaderName = ResolveVisibleSocialParticipantName(ev, payload, leaderId);
                string visibleTargetName = ResolveVisibleSocialParticipantName(ev, payload, targetId);
                bool hasVisibleLeader = visibleLeaderName != C.LabelUnknown;
                bool hasVisibleTarget = visibleTargetName != C.LabelUnknown;
                if (hasVisibleLeader && hasVisibleTarget)
                {
                    return C.TextBullying + visibleLeaderName + C.SeparatorArrow + visibleTargetName;
                }

                if (hasVisibleLeader)
                {
                    return C.TextBullying + visibleLeaderName;
                }

                if (hasVisibleTarget)
                {
                    return C.TextBullying + visibleTargetName;
                }

                return C.LabelNotKnownToProducer;
            }

            if (kind == C.KindTheater)
            {
                string theaterTitle = HumanizeUnknown(ReadStr(payload, C.JsonTheaterTitle));
                return theaterTitle != C.LabelUnknown
                    ? C.TextTheaterTitlePrefix + theaterTitle
                    : C.TextTheater + Normalize(ev.EntityId);
            }

            if (kind == C.KindActivity)
            {
                string activityType = HumanizeUnknown(ReadStr(payload, C.JsonActivityType));
                return activityType != C.LabelUnknown
                    ? C.TextAgencyActivity + activityType
                    : C.TextAgencyActivityFallback;
            }

            if (kind == C.KindLoan)
            {
                return C.TextLoan + Normalize(ev.EntityId);
            }

            if (kind == C.KindBankruptcy)
            {
                return C.TextBankruptcy;
            }

            if (kind == C.KindCafe)
            {
                string cafeTitle = HumanizeUnknown(ReadStr(payload, C.KeyCafeTitle));
                return cafeTitle != C.LabelUnknown
                    ? C.TextCafe + cafeTitle
                    : C.TextCafe + Normalize(ev.EntityId);
            }

            if (kind == C.KindMentorship)
            {
                int mentorId = ReadId(payload, C.KeyMentorId);
                int kohaiId = ReadId(payload, C.KeyKohaiId);
                if (mentorId >= C.MinId || kohaiId >= C.MinId)
                {
                    return C.TextMentorship + C.SeparatorColonSpace + ResolveIdolNameById(mentorId) + C.SeparatorArrow + ResolveIdolNameById(kohaiId);
                }
            }

            if (kind == C.KindSummerGames)
            {
                int singleId = ReadId(payload, C.KeySelectedSingleId);
                string singleTitle = ResolveSingleTitleById(singleId);
                return singleTitle != C.LabelUnknown
                    ? C.TextSummerGames + C.SeparatorColonSpace + singleTitle
                    : C.TextSummerGames;
            }

            if (kind == C.KindTour)
            {
                return C.TextTour + Normalize(ev.EntityId);
            }

            if (kind == C.KindElection)
            {
                return C.TextElection + Normalize(ev.EntityId);
            }

            if (kind == C.KindContract)
            {
                string product = HumanizeUnknown(ReadStr(payload, C.JsonContractProductName));
                if (product != C.LabelUnknown)
                {
                    return C.TextContract + product;
                }

                int contractIdolId;
                string contractTypeCode;
                int contractEndDateKey;
                if (TryParseContractEntityIdentifier(ev.EntityId, out contractIdolId, out contractTypeCode, out contractEndDateKey))
                {
                    string contractType = HumanizeUnknown(contractTypeCode);
                    string contractIdolName = ResolveIdolNameById(contractIdolId);
                    if (contractIdolName != C.LabelUnknown)
                    {
                        return C.TextContract + contractType + C.SeparatorSpaceOpenParen + contractIdolName + C.SeparatorCloseParen;
                    }

                    return C.TextContract + contractType;
                }
            }

            return HumanizeCode(kind) + C.SeparatorSpaceHash + Normalize(ev.EntityId);
        }

        /// <summary>
        /// Builds payload preview text.
        /// </summary>
        private static string BuildPayloadPreview(string payload)
        {
            if (string.IsNullOrEmpty(payload))
            {
                return string.Empty;
            }

            string compact = payload.Replace(Environment.NewLine, C.SeparatorSpace).Replace('\n', ' ').Replace('\r', ' ');
            if (compact.Length > C.MaxPayloadPreviewChars)
            {
                compact = compact.Substring(C.ZeroIndex, C.MaxPayloadPreviewChars);
            }
            return compact;
        }

        /// <summary>
        /// Reads json payload safely.
        /// </summary>
        private static JSONNode ParsePayload(string payload)
        {
            if (string.IsNullOrEmpty(payload))
            {
                return null;
            }

            try
            {
                return JSON.Parse(payload);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Reads string field from payload.
        /// </summary>
        private static string ReadStr(JSONNode payload, string field)
        {
            if (payload == null || string.IsNullOrEmpty(field))
            {
                return string.Empty;
            }

            JSONNode n = payload[field];
            return IsMissingNode(n) ? string.Empty : (n.Value ?? string.Empty);
        }

        /// <summary>
        /// Reads int field from payload.
        /// </summary>
        private static int ReadInt(JSONNode payload, string field)
        {
            if (payload == null || string.IsNullOrEmpty(field))
            {
                return C.MinId;
            }

            JSONNode n = payload[field];
            return IsMissingNode(n) ? C.MinId : n.AsInt;
        }

        /// <summary>
        /// Reads identifier field from payload, returning InvalidId when missing.
        /// </summary>
        private static int ReadId(JSONNode payload, string field)
        {
            if (payload == null || string.IsNullOrEmpty(field))
            {
                return C.InvalidId;
            }

            JSONNode n = payload[field];
            if (IsMissingNode(n))
            {
                return C.InvalidId;
            }

            int parsedValue;
            if (int.TryParse(n.Value ?? string.Empty, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedValue))
            {
                return parsedValue;
            }

            return n.AsInt;
        }

        /// <summary>
        /// Reads long field from payload.
        /// </summary>
        private static long ReadLong(JSONNode payload, string field)
        {
            if (payload == null || string.IsNullOrEmpty(field))
            {
                return C.LongZero;
            }

            JSONNode n = payload[field];
            long parsedValue;
            if (!TryParseNodeLong(n, out parsedValue))
            {
                return C.LongZero;
            }

            return parsedValue;
        }

        /// <summary>
        /// Reads float field from payload.
        /// </summary>
        private static float ReadFloat(JSONNode payload, string field)
        {
            if (payload == null || string.IsNullOrEmpty(field))
            {
                return C.FloatZero;
            }

            JSONNode n = payload[field];
            if (IsMissingNode(n))
            {
                return C.FloatZero;
            }

            float parsedValue;
            if (float.TryParse(n.Value ?? string.Empty, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue))
            {
                return parsedValue;
            }

            return n.AsFloat;
        }

        /// <summary>
        /// Reads bool field from payload.
        /// </summary>
        private static bool ReadBool(JSONNode payload, string field)
        {
            if (payload == null || string.IsNullOrEmpty(field))
            {
                return false;
            }

            JSONNode n = payload[field];
            if (IsMissingNode(n))
            {
                return false;
            }

            bool parsedBool;
            if (bool.TryParse(n.Value ?? string.Empty, out parsedBool))
            {
                return parsedBool;
            }

            int parsedInt;
            if (int.TryParse(n.Value ?? string.Empty, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedInt))
            {
                return parsedInt != C.MinId;
            }

            return n.AsBool;
        }

        /// <summary>
        /// Returns true when a JSON field was not present in the payload.
        /// </summary>
        private static bool IsMissingNode(JSONNode node)
        {
            return node == null || string.Equals(node.GetType().Name, C.JsonLazyCreatorRuntimeTypeName, StringComparison.Ordinal);
        }

        /// <summary>
        /// Parses a JSON node as long using invariant culture and numeric fallback.
        /// </summary>
        private static bool TryParseNodeLong(JSONNode node, out long value)
        {
            value = C.LongZero;
            if (IsMissingNode(node))
            {
                return false;
            }

            string rawValue = node.Value ?? string.Empty;
            if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            double parsedDouble;
            if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedDouble))
            {
                value = (long)parsedDouble;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resolves one code-like token through base-game localized labels when the game already exposes that wording.
        /// </summary>
        private static bool TryResolveGameCodeLabel(string rawCode, out string label)
        {
            string normalized = NormalizeCodeToken(rawCode);
            if (normalized.Length == C.ZeroIndex)
            {
                label = string.Empty;
                return false;
            }

            if (TryResolveGameFanCodeLabel(normalized, out label) ||
                TryResolveGameParameterCodeLabel(normalized, out label) ||
                TryResolveGameActivityCodeLabel(normalized, out label) ||
                TryResolveGameShowCodeLabel(normalized, out label) ||
                TryResolveGameLoanCodeLabel(normalized, out label) ||
                TryResolveGameBroadcastCodeLabel(normalized, out label) ||
                TryResolveGameCountryCodeLabel(normalized, out label) ||
                TryResolveGameRelationshipCodeLabel(normalized, out label) ||
                TryResolveGamePriorityCodeLabel(normalized, out label) ||
                TryResolveGameBusinessCodeLabel(normalized, out label))
            {
                return !string.IsNullOrEmpty(label);
            }

            label = string.Empty;
            return false;
        }

        /// <summary>
        /// Resolves base-game localized fan segment labels.
        /// </summary>
        private static bool TryResolveGameFanCodeLabel(string normalizedCode, out string label)
        {
            if (normalizedCode == NormalizeCodeToken(nameof(resources.fanType.male)))
            {
                label = resources.GetFanTitle(resources.fanType.male);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(resources.fanType.female)))
            {
                label = resources.GetFanTitle(resources.fanType.female);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(resources.fanType.casual)))
            {
                label = resources.GetFanTitle(resources.fanType.casual);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(resources.fanType.hardcore)))
            {
                label = resources.GetFanTitle(resources.fanType.hardcore);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(resources.fanType.teen)))
            {
                label = resources.GetFanTitle(resources.fanType.teen);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(resources.fanType.youngAdult)))
            {
                label = resources.GetFanTitle(resources.fanType.youngAdult);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(resources.fanType.adult)))
            {
                label = resources.GetFanTitle(resources.fanType.adult);
                return true;
            }

            if (normalizedCode == C.CodeEveryone)
            {
                label = GetGameLocalization(C.LanguageKeyTheaterEveryone);
                return true;
            }

            label = string.Empty;
            return false;
        }

        /// <summary>
        /// Resolves base-game localized idol and single parameter labels.
        /// </summary>
        private static bool TryResolveGameParameterCodeLabel(string normalizedCode, out string label)
        {
            if (normalizedCode == NormalizeCodeToken(nameof(data_girls._paramType.cute)))
            {
                label = data_girls.GetParamName(data_girls._paramType.cute);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(data_girls._paramType.cool)))
            {
                label = data_girls.GetParamName(data_girls._paramType.cool);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(data_girls._paramType.sexy)))
            {
                label = data_girls.GetParamName(data_girls._paramType.sexy);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(data_girls._paramType.pretty)))
            {
                label = data_girls.GetParamName(data_girls._paramType.pretty);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(data_girls._paramType.dance)))
            {
                label = data_girls.GetParamName(data_girls._paramType.dance);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(data_girls._paramType.vocal)))
            {
                label = data_girls.GetParamName(data_girls._paramType.vocal);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(data_girls._paramType.funny)))
            {
                label = data_girls.GetParamName(data_girls._paramType.funny);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(data_girls._paramType.smart)))
            {
                label = data_girls.GetParamName(data_girls._paramType.smart);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(data_girls._paramType.physicalStamina)))
            {
                label = data_girls.GetParamName(data_girls._paramType.physicalStamina);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(data_girls._paramType.mentalStamina)))
            {
                label = data_girls.GetParamName(data_girls._paramType.mentalStamina);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(data_girls._paramType.famePoints)))
            {
                label = data_girls.GetParamName(data_girls._paramType.famePoints);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(data_girls._paramType.teamChemistry)))
            {
                label = data_girls.GetParamName(data_girls._paramType.teamChemistry);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(data_girls._paramType.scandalPoints)))
            {
                label = data_girls.GetParamName(data_girls._paramType.scandalPoints);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(singles._param._type.genre)))
            {
                label = GetGameLocalization(C.LanguageKeySingleGenre);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(singles._param._type.lyrics)))
            {
                label = GetGameLocalization(C.LanguageKeySingleLyrics);
                return true;
            }

            if (normalizedCode == "song")
            {
                label = GetGameLocalization(C.LanguageKeySingleSong);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(singles._param._type.choreography)))
            {
                label = GetGameLocalization(C.LanguageKeySingleChoreography);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(singles._param._type.marketing)))
            {
                label = GetGameLocalization(C.LanguageKeySingleMarketing);
                return true;
            }

            if (normalizedCode == C.CodeReleased)
            {
                label = GetGameLocalization(C.LanguageKeySingleReleased);
                return true;
            }

            if (normalizedCode == C.CodeConcept)
            {
                label = GetGameLocalization(C.LanguageKeyMediaConcept);
                return true;
            }

            label = string.Empty;
            return false;
        }

        /// <summary>
        /// Resolves base-game localized activity and theater schedule labels.
        /// </summary>
        private static bool TryResolveGameActivityCodeLabel(string normalizedCode, out string label)
        {
            string performanceCode = NormalizeCodeToken(nameof(Activity._type.performance));
            if (normalizedCode == performanceCode || normalizedCode == C.CodeActivityPrefix + performanceCode)
            {
                label = GetGameLocalization(C.LanguageKeyTheaterPerformance);
                return true;
            }

            string promotionCode = NormalizeCodeToken(nameof(Activity._type.promotion));
            if (normalizedCode == promotionCode || normalizedCode == C.CodeActivityPrefix + promotionCode)
            {
                label = GetGameLocalization(C.LanguageKeyActivityPromotion);
                return true;
            }

            string spaTreatmentCode = NormalizeCodeToken(nameof(Activity._type.spa_treatment));
            if (normalizedCode == spaTreatmentCode || normalizedCode == C.CodeActivityPrefix + spaTreatmentCode)
            {
                label = GetGameLocalization(C.LanguageKeyActivitySpaTreatment);
                return true;
            }

            if (normalizedCode == "preproduction")
            {
                label = GetGameLocalization(C.LanguageKeyMediaPreproduction);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(Theaters._theater._schedule._type.auto)))
            {
                label = GetGameLocalization(C.LanguageKeyCafeAuto);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(Theaters._theater._schedule._type.manzai)))
            {
                label = GetGameLocalization(C.LanguageKeyTheaterManzai);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(Theaters._theater._schedule._type.day_off)))
            {
                label = GetGameLocalization(C.LanguageKeyTheaterDayOff);
                return true;
            }

            label = string.Empty;
            return false;
        }

        /// <summary>
        /// Resolves base-game localized show, cast, and medium labels.
        /// </summary>
        private static bool TryResolveGameShowCodeLabel(string normalizedCode, out string label)
        {
            if (normalizedCode == C.CodeEntireGroup)
            {
                label = GetGameLocalization(C.LanguageKeyShowEntireGroup);
                return true;
            }

            if (normalizedCode == C.CodeRotatingCast)
            {
                label = GetGameLocalization(C.LanguageKeyShowRotatingCast);
                return true;
            }

            if (normalizedCode == C.CodePermanentCast)
            {
                label = GetGameLocalization(C.LanguageKeyShowPermanentCast);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(Shows._param._media_type.radio)))
            {
                label = GetGameLocalization(C.LanguageKeyShowRadio);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(Shows._param._media_type.internet)))
            {
                label = GetGameLocalization(C.LanguageKeyShowInternet);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(Shows._param._media_type.tv)))
            {
                label = GetGameLocalization(C.LanguageKeyShowTv);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(tasks._task._goal.show)))
            {
                label = GetGameLocalization(C.LanguageKeyShow);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(tasks._task._goal.single)))
            {
                label = GetGameLocalization(C.LanguageKeySingle);
                return true;
            }

            label = string.Empty;
            return false;
        }

        /// <summary>
        /// Resolves base-game localized loan labels.
        /// </summary>
        private static bool TryResolveGameLoanCodeLabel(string normalizedCode, out string label)
        {
            if (normalizedCode == NormalizeCodeToken(nameof(tasks._task._type.fujimoto_loan)))
            {
                label = GetGameLocalization(C.LanguageKeyLoan);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(loans._loan._type.fujimoto)))
            {
                label = GetGameLocalization(C.LanguageKeyLoanFujimoto);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(loans._loan._type.bank)))
            {
                label = GetGameLocalization(C.LanguageKeyLoanBank);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(loans._loan._type.investor)))
            {
                label = GetGameLocalization(C.LanguageKeyLoanInvestor);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(loans._loan._duration.one_month)))
            {
                label = GetGameLocalization(C.LanguageKeyLoanOneMonth);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(loans._loan._duration.three_months)))
            {
                label = GetGameLocalization(C.LanguageKeyLoanThreeMonths);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(loans._loan._duration.six_months)))
            {
                label = GetGameLocalization(C.LanguageKeyLoanSixMonths);
                return true;
            }

            label = string.Empty;
            return false;
        }

        /// <summary>
        /// Resolves base-game localized election broadcast labels.
        /// </summary>
        private static bool TryResolveGameBroadcastCodeLabel(string normalizedCode, out string label)
        {
            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_SSK._broadcast.liveBlog)))
            {
                label = SEvent_SSK.GetBroadcastTitle(SEvent_SSK._broadcast.liveBlog);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_SSK._broadcast.webStream)))
            {
                label = SEvent_SSK.GetBroadcastTitle(SEvent_SSK._broadcast.webStream);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_SSK._broadcast.localTV)))
            {
                label = SEvent_SSK.GetBroadcastTitle(SEvent_SSK._broadcast.localTV);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_SSK._broadcast.nationalTV)))
            {
                label = SEvent_SSK.GetBroadcastTitle(SEvent_SSK._broadcast.nationalTV);
                return true;
            }

            label = string.Empty;
            return false;
        }

        /// <summary>
        /// Resolves base-game localized world-tour country labels.
        /// </summary>
        private static bool TryResolveGameCountryCodeLabel(string normalizedCode, out string label)
        {
            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_Tour._country.russia)))
            {
                label = SEvent_Tour.GetCountryTitle(SEvent_Tour._country.russia);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_Tour._country.china)))
            {
                label = SEvent_Tour.GetCountryTitle(SEvent_Tour._country.china);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_Tour._country.southKorea)))
            {
                label = SEvent_Tour.GetCountryTitle(SEvent_Tour._country.southKorea);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_Tour._country.india)))
            {
                label = SEvent_Tour.GetCountryTitle(SEvent_Tour._country.india);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_Tour._country.thailand)))
            {
                label = SEvent_Tour.GetCountryTitle(SEvent_Tour._country.thailand);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_Tour._country.philippines)))
            {
                label = SEvent_Tour.GetCountryTitle(SEvent_Tour._country.philippines);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_Tour._country.indonesia)))
            {
                label = SEvent_Tour.GetCountryTitle(SEvent_Tour._country.indonesia);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_Tour._country.australia)))
            {
                label = SEvent_Tour.GetCountryTitle(SEvent_Tour._country.australia);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_Tour._country.southAfrica)))
            {
                label = SEvent_Tour.GetCountryTitle(SEvent_Tour._country.southAfrica);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_Tour._country.canada)))
            {
                label = SEvent_Tour.GetCountryTitle(SEvent_Tour._country.canada);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_Tour._country.US)))
            {
                label = SEvent_Tour.GetCountryTitle(SEvent_Tour._country.US);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_Tour._country.brazil)))
            {
                label = SEvent_Tour.GetCountryTitle(SEvent_Tour._country.brazil);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_Tour._country.argentina)))
            {
                label = SEvent_Tour.GetCountryTitle(SEvent_Tour._country.argentina);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_Tour._country.UK)))
            {
                label = SEvent_Tour.GetCountryTitle(SEvent_Tour._country.UK);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_Tour._country.france)))
            {
                label = SEvent_Tour.GetCountryTitle(SEvent_Tour._country.france);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_Tour._country.germany)))
            {
                label = SEvent_Tour.GetCountryTitle(SEvent_Tour._country.germany);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_Tour._country.italy)))
            {
                label = SEvent_Tour.GetCountryTitle(SEvent_Tour._country.italy);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(SEvent_Tour._country.spain)))
            {
                label = SEvent_Tour.GetCountryTitle(SEvent_Tour._country.spain);
                return true;
            }

            label = string.Empty;
            return false;
        }

        /// <summary>
        /// Resolves base-game localized producer-relationship labels.
        /// </summary>
        private static bool TryResolveGameRelationshipCodeLabel(string normalizedCode, out string label)
        {
            if (normalizedCode == "best_friends")
            {
                label = GetGameLocalization(C.LanguageKeyProfileBestFriends);
                return true;
            }

            if (normalizedCode == "friends")
            {
                label = GetGameLocalization(C.LanguageKeyProfileFriends);
                return true;
            }

            if (normalizedCode == "dislikes")
            {
                label = GetGameLocalization(C.LanguageKeyProfileDislikes);
                return true;
            }

            if (normalizedCode == "hates")
            {
                label = GetGameLocalization(C.LanguageKeyProfileHates);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(Relationships_Player._type.Friendship)))
            {
                label = Relationships_Player.GetEventName(Relationships_Player._type.Friendship);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(Relationships_Player._type.Influence)))
            {
                label = Relationships_Player.GetEventName(Relationships_Player._type.Influence);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(Relationships_Player._type.Romance)))
            {
                label = Relationships_Player.GetEventName(Relationships_Player._type.Romance);
                return true;
            }

            label = string.Empty;
            return false;
        }

        /// <summary>
        /// Resolves base-game localized cafe priority labels.
        /// </summary>
        private static bool TryResolveGamePriorityCodeLabel(string normalizedCode, out string label)
        {
            if (normalizedCode == NormalizeCodeToken(nameof(agency._room._cafe_priority.popular)))
            {
                label = GetGameLocalization(C.LanguageKeyPopular);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(agency._room._cafe_priority.unpopular)))
            {
                label = GetGameLocalization(C.LanguageKeyUnpopular);
                return true;
            }

            if (normalizedCode == C.CodeStamina)
            {
                label = GetGameLocalization(C.LanguageKeyCafeHighestStamina);
                return true;
            }

            if (normalizedCode == C.CodeStats)
            {
                label = GetGameLocalization(C.LanguageKeyCafeBestStats);
                return true;
            }

            label = string.Empty;
            return false;
        }

        /// <summary>
        /// Resolves base-game localized business and producer labels.
        /// </summary>
        private static bool TryResolveGameBusinessCodeLabel(string normalizedCode, out string label)
        {
            if (normalizedCode == C.CodePlayer || normalizedCode == "producer")
            {
                label = GetGameLocalization(C.LanguageKeyResearchProducer);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(tasks._task._goal.ad)))
            {
                label = GetGameLocalization(C.LanguageKeyBusinessAd);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(tasks._task._goal.variety)))
            {
                label = GetGameLocalization(C.LanguageKeyBusinessVariety);
                return true;
            }

            if (normalizedCode == NormalizeCodeToken(nameof(tasks._task._goal.drama)))
            {
                label = GetGameLocalization(C.LanguageKeyBusinessTv);
                return true;
            }

            if (normalizedCode == C.CodeBusiness)
            {
                label = GetGameLocalization(C.LanguageKeyBusiness);
                return true;
            }

            label = string.Empty;
            return false;
        }

        /// <summary>
        /// Resolves optional localization override for one code-like token.
        /// </summary>
        private static string ResolveCodeLabel(string rawCode)
        {
            if (string.IsNullOrEmpty(rawCode))
            {
                return string.Empty;
            }

            // Mod Localization System follows Idol Manager's selected language,
            // so reuse the game's terminology for concepts it already owns.
            string gameLabel;
            if (TryResolveGameCodeLabel(rawCode, out gameLabel))
            {
                return gameLabel;
            }

            string normalized = rawCode.Trim().ToLowerInvariant()
                .Replace(C.SeparatorDash, C.SeparatorUnderscore)
                .Replace(".", C.SeparatorUnderscore);
            if (normalized.Length == C.ZeroIndex)
            {
                return string.Empty;
            }

            return ModLocalization.Get(C.CodeLabelLocalizationKeyPrefix + normalized, string.Empty);
        }

        /// <summary>
        /// Converts runtime enum/string code values to end-user labels.
        /// </summary>
        private static string HumanizeCode(string rawCode)
        {
            if (string.IsNullOrEmpty(rawCode))
            {
                return C.LabelUnknown;
            }

            string code = rawCode.Trim();
            if (code.Length == C.ZeroIndex)
            {
                return C.LabelUnknown;
            }

            string lower = code.ToLowerInvariant();
            if (lower == C.LabelUnknown.ToLowerInvariant())
            {
                return C.LabelUnknown;
            }

            string localizedCode = ResolveCodeLabel(lower);
            if (!string.IsNullOrEmpty(localizedCode))
            {
                return localizedCode;
            }

            switch (lower)
            {
                case C.CodeEntireGroup:
                    return C.TextEntireGroup;
                case C.CodeRotatingCast:
                    return C.TextRotatingCast;
                case C.CodePermanentCast:
                    return C.TextPermanentCast;
                case C.KeyTakenIdol:
                    return C.TextDatingAnotherIdol;
                case C.KeyTakenPlayer:
                    return C.TextDatingProducer;
                case C.KeyTakenOutsideBf:
                    return C.TextDatingNonIdolBoyfriend;
                case C.KeyTakenOutsideGf:
                    return C.TextDatingNonIdolGirlfriend;
                case C.CodeFree:
                    return C.LabelSingleStatus;
            }

            string withSpaces = code.Replace(C.SeparatorUnderscore, C.SeparatorSpace);
            System.Text.StringBuilder spacedBuilder = new System.Text.StringBuilder(withSpaces.Length + C.HumanizeCodeBufferPadding);
            for (int i = C.ZeroIndex; i < withSpaces.Length; i++)
            {
                char c = withSpaces[i];
                if (i > C.ZeroIndex && char.IsUpper(c) && char.IsLower(withSpaces[i - C.LastFromCount]))
                {
                    spacedBuilder.Append(' ');
                }

                spacedBuilder.Append(c);
            }

            string readable = spacedBuilder.ToString().Trim().ToLowerInvariant();
            if (readable.Length == C.ZeroIndex)
            {
                return C.LabelUnknown;
            }

            readable = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(readable);
            readable = readable.Replace(C.CodeTv, C.LabelTV).Replace(C.LabelMc, C.LabelMc).Replace(C.CodeSsk, C.LabelSSK);
            return readable;
        }

        /// <summary>
        /// Converts unknown-like values to Unknown while preserving friendly tokens.
        /// </summary>
        private static string HumanizeUnknown(string rawValue)
        {
            string humanized = HumanizeCode(rawValue);
            return string.IsNullOrEmpty(humanized) ? C.LabelUnknown : humanized;
        }

        /// <summary>
        /// Converts raw salary action code to short player-facing wording.
        /// </summary>
        private static string HumanizeSalaryAction(string salaryActionCode)
        {
            string action = (salaryActionCode ?? string.Empty).Trim().ToLowerInvariant();
            switch (action)
            {
                case C.CodeIncreased:
                    return C.LabelIncreased;
                case C.CodeDecreased:
                case C.CodeLowered:
                    return C.LabelReduced;
                case C.KeyManualSet:
                case C.CodeSet:
                case C.CodeSaved:
                    return C.LabelUpdated;
            }

            string fallback = HumanizeUnknown(salaryActionCode);
            return fallback == C.LabelUnknown ? C.LabelUpdated : fallback;
        }

        /// <summary>
        /// Builds one status transition string; omits all-unknown transitions.
        /// </summary>
        private static string BuildStatusTransitionText(string previousCode, string newCode)
        {
            string previous = HumanizeUnknown(previousCode);
            string next = HumanizeUnknown(newCode);

            bool previousUnknown = previous == C.LabelUnknown;
            bool nextUnknown = next == C.LabelUnknown;
            if (previousUnknown && nextUnknown)
            {
                return string.Empty;
            }

            if (previousUnknown)
            {
                return next;
            }

            if (nextUnknown)
            {
                return previous;
            }

            if (string.Equals(previous, next, StringComparison.OrdinalIgnoreCase))
            {
                return next;
            }

            return previous + C.SeparatorArrow + next;
        }

        /// <summary>
        /// Parses one comma-separated idol id list string.
        /// </summary>
        private static List<int> ParseIdList(string rawList)
        {
            List<int> ids = new List<int>();
            if (string.IsNullOrEmpty(rawList))
            {
                return ids;
            }

            string[] tokens = rawList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = C.ZeroIndex; i < tokens.Length; i++)
            {
                int parsedId;
                if (TryParseInt(tokens[i], out parsedId))
                {
                    ids.Add(parsedId);
                }
            }

            return ids;
        }

        /// <summary>
        /// Builds readable cast summary from payload cast-id list.
        /// </summary>
        private string BuildShowCastNameSummary(IMDataCoreEvent ev, JSONNode payload, int maxNames)
        {
            if (payload == null)
            {
                return string.Empty;
            }

            List<int> ids = ParseIdList(ReadStr(payload, C.JsonShowCastIdList));
            if (ids.Count == C.ZeroIndex)
            {
                return string.Empty;
            }

            int currentIdolId = ResolveIdFromEvent(ev);
            List<string> names = new List<string>();
            for (int i = C.ZeroIndex; i < ids.Count; i++)
            {
                int candidateId = ids[i];
                if (candidateId < C.MinId || candidateId == currentIdolId)
                {
                    continue;
                }

                data_girls.girls girl = data_girls.GetGirlByID(candidateId);
                if (girl == null)
                {
                    continue;
                }

                names.Add(ResolveIdolName(girl));
            }

            if (names.Count == C.ZeroIndex)
            {
                return string.Empty;
            }

            int clampedMax = Mathf.Max(C.LastFromCount, maxNames);
            if (names.Count <= clampedMax)
            {
                return string.Join(C.ListJoinSeparator, names.ToArray());
            }

            List<string> compact = new List<string>();
            for (int i = C.ZeroIndex; i < clampedMax; i++)
            {
                compact.Add(names[i]);
            }

            return string.Join(C.ListJoinSeparator, compact.ToArray()) + C.SuffixExtraCount + (names.Count - clampedMax).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Resolves episode release range text by comparing previous cached show event count.
        /// </summary>
        private string BuildShowEpisodeReleaseText(IMDataCoreEvent ev, JSONNode payload)
        {
            int currentEpisodeCount = ReadInt(payload, C.JsonShowEpisodeCount);
            if (currentEpisodeCount <= C.ZeroIndex)
            {
                return string.Empty;
            }

            int selectedIndex = FindEventIndexById(ev != null ? ev.EventId : C.InvalidEventId);
            int previousEpisodeCount = FindPreviousShowEpisodeCount(ev, selectedIndex, currentEpisodeCount);

            if (previousEpisodeCount > C.ZeroIndex && previousEpisodeCount < currentEpisodeCount)
            {
                int startEpisode = previousEpisodeCount + C.LastFromCount;
                if (startEpisode == currentEpisodeCount)
                {
                    return C.LabelEpisodeReleasedPrefix + currentEpisodeCount.ToString(CultureInfo.InvariantCulture);
                }

                return C.LabelEpisodesReleasedPrefix +
                    startEpisode.ToString(CultureInfo.InvariantCulture) +
                    C.SeparatorDash +
                    currentEpisodeCount.ToString(CultureInfo.InvariantCulture);
            }

            return C.LabelTotalEpisodesReleasedPrefix + currentEpisodeCount.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Finds selected event index in cached timeline list.
        /// </summary>
        private int FindEventIndexById(long eventId)
        {
            if (eventId <= C.InvalidEventId)
            {
                return C.InvalidId;
            }

            for (int i = C.ZeroIndex; i < cachedEvents.Count; i++)
            {
                IMDataCoreEvent candidate = cachedEvents[i];
                if (candidate != null && candidate.EventId == eventId)
                {
                    return i;
                }
            }

            return C.InvalidId;
        }

        /// <summary>
        /// Finds previous known episode count for same show from older cached entries.
        /// </summary>
        private int FindPreviousShowEpisodeCount(IMDataCoreEvent selectedEvent, int selectedIndex, int currentEpisodeCount)
        {
            if (selectedEvent == null || selectedIndex < C.ZeroIndex)
            {
                return C.ZeroIndex;
            }

            string selectedEntityId = selectedEvent.EntityId ?? string.Empty;
            for (int i = selectedIndex + C.LastFromCount; i < cachedEvents.Count; i++)
            {
                IMDataCoreEvent older = cachedEvents[i];
                if (older == null)
                {
                    continue;
                }

                if (!string.Equals(older.EntityKind, C.KindShow, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.Equals(older.EntityId ?? string.Empty, selectedEntityId, StringComparison.Ordinal))
                {
                    continue;
                }

                JSONNode olderPayload = ParsePayload(older.PayloadJson);
                int olderEpisodeCount = ReadInt(olderPayload, C.JsonShowEpisodeCount);
                if (olderEpisodeCount > C.ZeroIndex && olderEpisodeCount <= currentEpisodeCount)
                {
                    return olderEpisodeCount;
                }
            }

            return C.ZeroIndex;
        }

        /// <summary>
        /// Converts bool to yes/no.
        /// </summary>
        private static string YesNo(bool value)
        {
            return value ? C.LabelYes : C.LabelNo;
        }

        /// <summary>
        /// Formats signed numeric values with explicit + / - prefix.
        /// </summary>
        private static string FormatSignedNumber(long value)
        {
            return value.ToString(C.FormatZeroZeroZero, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formats one localized label with a single value while tolerating legacy non-placeholder translations.
        /// </summary>
        private static string FormatLocalizedText(string template, string value)
        {
            return FormatLocalizedText(template, new[] { value });
        }

        /// <summary>
        /// Formats one localized string with placeholder support and legacy concat fallback.
        /// </summary>
        private static string FormatLocalizedText(string template, params string[] values)
        {
            string safeTemplate = template ?? string.Empty;
            string[] safeValues = values ?? new string[C.ZeroIndex];
            if (safeTemplate.Length == C.ZeroIndex)
            {
                return safeValues.Length > C.ZeroIndex
                    ? string.Join(C.ListJoinSeparator, safeValues)
                    : string.Empty;
            }

            try
            {
                string formatted = string.Format(CultureInfo.InvariantCulture, safeTemplate, safeValues);
                if (!string.Equals(formatted, safeTemplate, StringComparison.Ordinal))
                {
                    return formatted;
                }
            }
            catch
            {
            }

            string joinedValues = safeValues.Length > C.ZeroIndex
                ? string.Join(C.ListJoinSeparator, safeValues)
                : string.Empty;
            if (joinedValues.Length == C.ZeroIndex)
            {
                return safeTemplate;
            }

            return safeTemplate.EndsWith(C.SeparatorSpace, StringComparison.Ordinal)
                ? safeTemplate + joinedValues
                : safeTemplate + C.SeparatorSpace + joinedValues;
        }

        /// <summary>
        /// Formats payload datetime values into diary UI date text.
        /// </summary>
        private static string FormatRoundTripDateForUi(string roundTrip)
        {
            DateTime parsed;
            if (TryParseEventDate(roundTrip, out parsed))
            {
                return FormatUiDate(parsed);
            }

            int parsedDateKey;
            if (TryParseInt(roundTrip, out parsedDateKey) && TryParseDateKeyForUi(parsedDateKey, out parsed))
            {
                return FormatUiDate(parsed);
            }

            return C.LabelUnknown;
        }

        /// <summary>
        /// Parses contract entity id in format idolId_contractType_endDateKey.
        /// </summary>
        private static bool TryParseContractEntityIdentifier(string entityId, out int idolId, out string contractTypeCode, out int endDateKey)
        {
            idolId = C.InvalidId;
            contractTypeCode = string.Empty;
            endDateKey = C.ZeroIndex;

            string raw = entityId ?? string.Empty;
            int firstSeparator = raw.IndexOf('_');
            int lastSeparator = raw.LastIndexOf('_');
            if (firstSeparator <= C.ZeroIndex || lastSeparator <= firstSeparator + C.ZeroIndex)
            {
                return false;
            }

            int parsedIdolId;
            if (!TryParseInt(raw.Substring(C.ZeroIndex, firstSeparator), out parsedIdolId))
            {
                return false;
            }

            string parsedTypeCode = raw.Substring(firstSeparator + C.LastFromCount, lastSeparator - firstSeparator - C.LastFromCount);
            if (string.IsNullOrEmpty(parsedTypeCode))
            {
                return false;
            }

            int parsedEndDateKey;
            if (!TryParseInt(raw.Substring(lastSeparator + C.LastFromCount), out parsedEndDateKey))
            {
                return false;
            }

            idolId = parsedIdolId;
            contractTypeCode = parsedTypeCode;
            endDateKey = parsedEndDateKey;
            return true;
        }

        /// <summary>
        /// Normalizes display text.
        /// </summary>
        private static string Normalize(string value)
        {
            return string.IsNullOrEmpty(value) ? C.LabelUnknown : value;
        }

        /// <summary>
        /// Resolves idol id from event.
        /// </summary>
        private static int ResolveIdFromEvent(IMDataCoreEvent ev)
        {
            if (ev == null)
            {
                return C.InvalidId;
            }

            if (ev.IdolId >= C.MinId)
            {
                return ev.IdolId;
            }

            int parsed;
            return TryParseInt(ev.EntityId, out parsed) ? parsed : C.InvalidId;
        }

        /// <summary>
        /// Parses int in invariant culture.
        /// </summary>
        private static bool TryParseInt(string raw, out int value)
        {
            return int.TryParse(raw ?? string.Empty, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>
        /// Resolves idol display name.
        /// </summary>
        private static string ResolveIdolName(data_girls.girls girl)
        {
            return girl == null ? C.LabelUnknown : Normalize(girl.GetName(true));
        }

        /// <summary>
        /// Resolves idol display name by id.
        /// </summary>
        private static string ResolveIdolNameById(int idolId)
        {
            if (idolId < C.MinId)
            {
                return C.LabelUnknown;
            }

            return ResolveIdolName(data_girls.GetGirlByID(idolId));
        }

        /// <summary>
        /// Resolves group display name by id.
        /// </summary>
        private static string ResolveGroupNameById(int groupId)
        {
            if (groupId < C.MinId)
            {
                return C.LabelUnknown;
            }

            Groups._group group = Groups.GetGroup(groupId);
            return group != null ? Normalize(group.Title) : (C.TextGroup + groupId.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Resolves single display title by identifier.
        /// </summary>
        private static string ResolveSingleTitleById(int singleId)
        {
            if (singleId < C.MinId)
            {
                return C.LabelUnknown;
            }

            singles._single single = singles.GetSingleByID(singleId);
            if (single == null)
            {
                return C.LabelUnknown;
            }

            return Normalize(single.title);
        }

        /// <summary>
        /// Creates one title line.
        /// </summary>
        private void AddTitle(string text)
        {
            AddTitle(diaryContentRoot, text);
        }

        /// <summary>
        /// Creates one title line in provided container.
        /// </summary>
        private void AddTitle(Transform parent, string text)
        {
            Transform targetParent = parent ?? diaryContentRoot;
            if (targetParent == null)
            {
                return;
            }

            GameObject obj = null;
            if (profile != null && profile.prefab_Bonds_Title != null)
            {
                obj = UnityEngine.Object.Instantiate(profile.prefab_Bonds_Title, targetParent, false);
            }
            else if (profile != null && profile.prefab_text != null)
            {
                obj = UnityEngine.Object.Instantiate(profile.prefab_text, targetParent, false);
            }
            else
            {
                obj = CreateUiObject(C.UiNameCareerDiaryTitlePrefix + Guid.NewGuid().ToString(C.FormatGuidCompact, CultureInfo.InvariantCulture), targetParent);
                Text t = obj.AddComponent<Text>();
                t.color = mainScript.green32;
                t.fontSize = C.CloseButtonInteractableScore;
                t.alignment = TextAnchor.MiddleLeft;
            }

            SetText(obj, text);
            obj.SetActive(true);
        }

        /// <summary>
        /// Creates one body text line.
        /// </summary>
        private void AddText(string text)
        {
            AddText(diaryContentRoot, text);
        }

        /// <summary>
        /// Creates one body text line in provided container.
        /// </summary>
        private void AddText(Transform parent, string text)
        {
            Transform targetParent = parent ?? diaryContentRoot;
            if (targetParent == null)
            {
                return;
            }

            GameObject obj = null;
            if (profile != null && profile.prefab_text != null)
            {
                obj = UnityEngine.Object.Instantiate(profile.prefab_text, targetParent, false);
            }
            else
            {
                obj = CreateUiObject(C.UiNameCareerDiaryTextPrefix + Guid.NewGuid().ToString(C.FormatGuidCompact, CultureInfo.InvariantCulture), targetParent);
                Text t = obj.AddComponent<Text>();
                t.color = mainScript.green32;
                t.fontSize = C.FallbackBodyTextFontSize;
                t.alignment = TextAnchor.UpperLeft;
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.verticalOverflow = VerticalWrapMode.Overflow;
            }

            SetText(obj, text);
            obj.SetActive(true);
        }

        /// <summary>
        /// Adds divider line.
        /// </summary>
        private void AddDivider()
        {
            AddDivider(diaryContentRoot);
        }

        /// <summary>
        /// Adds divider line in provided container.
        /// </summary>
        private void AddDivider(Transform parent)
        {
            Transform targetParent = parent ?? diaryContentRoot;
            if (targetParent == null)
            {
                return;
            }

            GameObject obj = null;
            if (profile != null && profile.prefab_divider != null)
            {
                obj = UnityEngine.Object.Instantiate(profile.prefab_divider, targetParent, false);
            }
            else
            {
                obj = CreateUiObject(C.UiNameCareerDiaryDividerPrefix + Guid.NewGuid().ToString(C.FormatGuidCompact, CultureInfo.InvariantCulture), targetParent);
                Image image = obj.AddComponent<Image>();
                image.color = profile != null ? profile.Color_Secondary : mainScript.green32;
                LayoutElement le = obj.AddComponent<LayoutElement>();
                le.preferredHeight = C.DividerHeight;
            }

            obj.SetActive(true);
        }

        /// <summary>
        /// Creates styled button by cloning template.
        /// </summary>
        private Button CreateStyledButton(Transform parent, string name, string label, UnityAction onClick)
        {
            GameObject obj = null;
            Button frameworkButton;
            string frameworkError;

            if (tabTemplate != null)
            {
                if (IMUiFrameworkApi.TryCloneStyledButton(
                    tabTemplate,
                    parent,
                    name,
                    label,
                    label,
                    onClick,
                    C.FloatZero,
                    C.FloatZero,
                    out frameworkButton,
                    out frameworkError))
                {
                    ConfigureActionButtonVisuals(frameworkButton != null ? frameworkButton.gameObject : null, parent);
                    return frameworkButton;
                }

                obj = UnityEngine.Object.Instantiate(tabTemplate, parent, false);
                obj.name = name;
                obj.SetActive(true);
                StripTabBehavior(obj);
                ApplyButtonLabel(obj, label);
            }
            else
            {
                if (IMUiFrameworkApi.TryCreateStyledButton(parent, name, label, C.FallbackStyledButtonWidth, C.ActionButtonHeight, onClick, out frameworkButton, out frameworkError))
                {
                    ConfigureActionButtonVisuals(frameworkButton != null ? frameworkButton.gameObject : null, parent);
                    return frameworkButton;
                }

                obj = CreateUiObject(name, parent);
                Image image = obj.AddComponent<Image>();
                image.color = profile != null ? profile.Color_Secondary : mainScript.green32;
            }

            Button b = obj.GetComponent<Button>();
            if (b == null)
            {
                b = obj.AddComponent<Button>();
            }

            if (onClick != null)
            {
                b.onClick.RemoveListener(onClick);
                b.onClick.AddListener(onClick);
            }

            ButtonDefault bd = obj.GetComponent<ButtonDefault>();
            if (bd != null)
            {
                bd.DefaultTooltip = label;
                bd.SetTooltip(label);
                bd.Activate(true, false);
            }

            ConfigureActionButtonVisuals(obj, parent);
            return b;
        }

        /// <summary>
        /// Creates a custom Unity action button sized to label text, avoiding profile-tab prefab templates.
        /// </summary>
        private Button CreateCustomActionButton(Transform parent, string name, string label, UnityAction onClick)
        {
            GameObject buttonObject = CreateUiObject(name, parent);
            if (buttonObject == null)
            {
                return null;
            }

            buttonObject.SetActive(true);
            if (parent != null)
            {
                SetLayerRecursively(buttonObject, parent.gameObject.layer);
            }

            StripTabBehavior(buttonObject);

            Image background = buttonObject.GetComponent<Image>();
            if (background == null)
            {
                background = buttonObject.AddComponent<Image>();
            }
            background.color = profile != null ? profile.Color_Secondary : mainScript.green32;

            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObject.AddComponent<Button>();
            }
            button.onClick = new Button.ButtonClickedEvent();
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            ButtonDefault buttonDefault = buttonObject.GetComponent<ButtonDefault>();
            if (buttonDefault != null)
            {
                buttonDefault.DefaultTooltip = label;
                buttonDefault.SetTooltip(label);
                buttonDefault.Activate(true, false);
            }

            GameObject labelObject = CreateUiObject(name + C.SeparatorUnderscore + "Label", buttonObject.transform);
            if (labelObject != null)
            {
                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                if (labelRect != null)
                {
                    float horizontalInset = C.CustomActionButtonTextHorizontalInset;
                    float verticalInset = C.CustomActionButtonTextVerticalInset;
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.pivot = new Vector2(C.HeaderPlacementHalfScale, C.HeaderPlacementHalfScale);
                    labelRect.offsetMin = new Vector2(horizontalInset, verticalInset);
                    labelRect.offsetMax = new Vector2(-horizontalInset, -verticalInset);
                }

                GameObject styleSource = tabTemplate != null
                    ? tabTemplate
                    : (profile != null ? profile.prefab_text : null);

                TextMeshProUGUI sourceTmp = styleSource != null
                    ? styleSource.GetComponentInChildren<TextMeshProUGUI>(true)
                    : null;
                Text sourceLegacy = sourceTmp == null && styleSource != null
                    ? styleSource.GetComponentInChildren<Text>(true)
                    : null;

                if (sourceTmp != null)
                {
                    TextMeshProUGUI labelTmp = labelObject.GetComponent<TextMeshProUGUI>();
                    if (labelTmp == null)
                    {
                        labelTmp = labelObject.AddComponent<TextMeshProUGUI>();
                    }

                    labelTmp.text = label ?? string.Empty;
                    labelTmp.font = sourceTmp.font;
                    labelTmp.fontSharedMaterial = sourceTmp.fontSharedMaterial;
                    labelTmp.fontStyle = sourceTmp.fontStyle;
                    labelTmp.fontSize = sourceTmp.fontSize;
                    labelTmp.characterSpacing = sourceTmp.characterSpacing;
                    labelTmp.wordSpacing = sourceTmp.wordSpacing;
                    labelTmp.lineSpacing = sourceTmp.lineSpacing;
                    labelTmp.alignment = sourceTmp.alignment;
                    labelTmp.enableAutoSizing = false;
                    labelTmp.enableWordWrapping = false;
                    labelTmp.overflowMode = TextOverflowModes.Overflow;
                    labelTmp.raycastTarget = false;
                    labelTmp.color = Color.white;
                }
                else
                {
                    Text labelText = labelObject.GetComponent<Text>();
                    if (labelText == null)
                    {
                        labelText = labelObject.AddComponent<Text>();
                    }

                    labelText.text = label ?? string.Empty;
                    labelText.color = Color.white;
                    labelText.fontStyle = sourceLegacy != null ? sourceLegacy.fontStyle : FontStyle.Normal;
                    labelText.fontSize = sourceLegacy != null ? sourceLegacy.fontSize : C.TimelineToolbarButtonFontSize;
                    labelText.alignment = sourceLegacy != null ? sourceLegacy.alignment : TextAnchor.MiddleCenter;
                    labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
                    labelText.verticalOverflow = VerticalWrapMode.Truncate;
                    labelText.resizeTextForBestFit = false;
                    labelText.raycastTarget = false;

                    Font font = sourceLegacy != null ? sourceLegacy.font : ResolveFallbackLegacyFont();
                    if (font != null)
                    {
                        labelText.font = font;
                    }
                }
            }

            float measuredWidth = MeasurePreferredButtonWidth(buttonObject, C.ActionButtonMinimumWidth, C.ActionButtonMaximumWidth);
            float estimatedWidth = MeasurePreferredButtonWidthFromLabel(label, C.ActionButtonMinimumWidth, C.ActionButtonMaximumWidth);
            float preferredWidth = Mathf.Clamp(
                Mathf.Max(measuredWidth, estimatedWidth),
                C.ActionButtonMinimumWidth,
                C.ActionButtonMaximumWidth);

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = buttonObject.AddComponent<LayoutElement>();
            }

            layout.minWidth = C.FloatZero;
            layout.preferredWidth = preferredWidth;
            layout.flexibleWidth = C.FloatZero;
            layout.minHeight = C.ActionButtonHeight;
            layout.preferredHeight = C.ActionButtonHeight;
            layout.flexibleHeight = C.FloatZero;

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                buttonRect.sizeDelta = new Vector2(preferredWidth, C.ActionButtonHeight);
            }

            return button;
        }

        /// <summary>
        /// Resolves a safe fallback legacy UI font for custom action buttons.
        /// </summary>
        private static Font ResolveFallbackLegacyFont()
        {
            try
            {
                return Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a timeline control button using IM UI Framework CreateStyledButton directly, avoiding tab template clone sizing.
        /// </summary>
        private Button CreateFrameworkStyledButton(Transform parent, string name, string label, UnityAction onClick)
        {
            float measuredWidth = Mathf.Clamp(
                MeasurePreferredButtonWidthFromLabel(
                    label,
                    C.TimelineFilterButtonMinimumWidth,
                    C.TimelineFilterButtonMaximumWidth),
                C.TimelineFilterButtonMinimumWidth,
                C.TimelineFilterButtonMaximumWidth);

            Button frameworkButton;
            string frameworkError;
            if (IMUiFrameworkApi.TryCreateStyledButton(parent, name, label, measuredWidth, C.ActionButtonHeight, onClick, out frameworkButton, out frameworkError))
            {
                ConfigureActionButtonVisuals(frameworkButton != null ? frameworkButton.gameObject : null, parent);
                return frameworkButton;
            }

            return CreateStyledButton(parent, name, label, onClick);
        }

        /// <summary>
        /// Applies selected/non-selected color.
        /// </summary>
        private void SetButtonSelected(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = selected ? mainScript.green32 : (profile != null ? profile.Color_Secondary : mainScript.green32);
            }
        }

        /// <summary>
        /// Updates diary tab button color.
        /// </summary>
        private void UpdateDiaryButtonColor(bool selected)
        {
            SetButtonSelected(diaryTabButton, selected);
        }

        /// <summary>
        /// Clears transient diary UI when enclosing popup closes.
        /// </summary>
        internal void OnPopupClosedExternal()
        {
            HideDiary(false);
            TryRunLifecycleBackdropCleanup();
        }

        /// <summary>
        /// Finds the most probable close button in current popup hierarchy.
        /// </summary>
        private static Button FindCloseButton(Profile_Popup popup)
        {
            if (popup == null)
            {
                return null;
            }

            Transform primaryRoot = popup.transform;
            if (primaryRoot == null)
            {
                return null;
            }

            Transform[] searchRoots;
            if (primaryRoot.parent != null)
            {
                searchRoots = new[] { primaryRoot, primaryRoot.parent };
            }
            else
            {
                searchRoots = new[] { primaryRoot };
            }

            Button best = null;
            int bestScore = int.MinValue;
            float bestX = float.MinValue;
            for (int rootIndex = C.ZeroIndex; rootIndex < searchRoots.Length; rootIndex++)
            {
                Transform root = searchRoots[rootIndex];
                if (root == null)
                {
                    continue;
                }

                Button[] buttons = root.GetComponentsInChildren<Button>(true);
                for (int i = C.ZeroIndex; i < buttons.Length; i++)
                {
                    Button candidate = buttons[i];
                    if (candidate == null)
                    {
                        continue;
                    }

                    int score = ScoreCloseButtonCandidate(candidate, popup);
                    float candidateX = candidate.transform.position.x;
                    if (score > bestScore || (score == bestScore && candidateX > bestX))
                    {
                        bestScore = score;
                        bestX = candidateX;
                        best = candidate;
                    }
                }

                if (bestScore >= C.CloseButtonMinimumAcceptableScore)
                {
                    return best;
                }
            }

            return null;
        }

        /// <summary>
        /// Scores one button for close-button likelihood.
        /// </summary>
        private static int ScoreCloseButtonCandidate(Button candidate, Profile_Popup popup)
        {
            if (candidate == null)
            {
                return int.MinValue;
            }

            if (!candidate.gameObject.activeInHierarchy)
            {
                return int.MinValue;
            }

            if (IsOversizedCloseButtonCandidate(candidate, popup))
            {
                return int.MinValue;
            }

            int score = C.ZeroIndex;
            score += C.CloseButtonActiveHierarchyScore;
            if (popup != null && popup.transform != null && candidate.transform.IsChildOf(popup.transform))
            {
                score += C.CloseButtonInProfileHierarchyScore;
            }

            if (candidate.interactable)
            {
                score += C.CloseButtonInteractableScore;
            }

            bool hasCloseIndicator = false;
            string localizedCloseLabel = GetLocalizedCloseLabel();
            string nameLower = (candidate.gameObject.name ?? string.Empty).ToLowerInvariant();
            if (popup != null &&
                candidate.transform.parent == popup.transform &&
                string.Equals(candidate.gameObject.name, C.CloseActionButtonObjectName, StringComparison.OrdinalIgnoreCase))
            {
                hasCloseIndicator = true;
                score += C.CloseButtonPreferredNameMatchScore;
            }

            if (nameLower.Contains(C.CloseTokenLower))
            {
                hasCloseIndicator = true;
                score += C.CloseButtonNameMatchScore;
            }

            ButtonDefault buttonDefault = candidate.GetComponent<ButtonDefault>();
            if (buttonDefault != null)
            {
                string tooltip = buttonDefault.DefaultTooltip ?? string.Empty;
                if (StringEqualsInvariant(tooltip, localizedCloseLabel))
                {
                    hasCloseIndicator = true;
                    score += C.CloseButtonExactLabelMatchScore;
                }
                else if (tooltip.ToLowerInvariant().Contains(C.CloseTokenLower))
                {
                    hasCloseIndicator = true;
                    score += C.CloseButtonTooltipMatchScore;
                }
            }

            if (HasCloseLocalizationMarker(candidate.gameObject))
            {
                hasCloseIndicator = true;
                score += C.CloseButtonLocalizationKeyMatchScore;
            }

            string displayText = GetButtonDisplayText(candidate.gameObject);
            if (StringEqualsInvariant(displayText, localizedCloseLabel))
            {
                hasCloseIndicator = true;
                score += C.CloseButtonExactLabelMatchScore;
            }
            else if (!string.IsNullOrEmpty(displayText) && displayText.ToLowerInvariant().Contains(C.CloseTokenLower))
            {
                hasCloseIndicator = true;
                score += C.CloseButtonTextContainsCloseTokenScore;
            }

            if (string.Equals(displayText, C.CloseGlyphUpper, StringComparison.OrdinalIgnoreCase))
            {
                hasCloseIndicator = true;
                score += C.CloseButtonTextMatchScore;
            }

            string buttonName = candidate.gameObject.name ?? string.Empty;
            if (buttonName.IndexOf(C.CloseMethodToken, StringComparison.OrdinalIgnoreCase) >= C.ZeroIndex)
            {
                hasCloseIndicator = true;
                score += C.CloseButtonMethodMatchScore;
            }

            if (!hasCloseIndicator)
            {
                return int.MinValue;
            }

            return score;
        }

        /// <summary>
        /// Rejects full-popup overlay buttons that are not useful as visible close anchors.
        /// </summary>
        private static bool IsOversizedCloseButtonCandidate(Button candidate, Profile_Popup popup)
        {
            if (candidate == null || popup == null)
            {
                return false;
            }

            RectTransform candidateRect = candidate.GetComponent<RectTransform>();
            RectTransform popupRect = popup.GetComponent<RectTransform>();
            if (candidateRect == null || popupRect == null)
            {
                return false;
            }

            Rect candidateBounds = candidateRect.rect;
            Rect popupBounds = popupRect.rect;
            if (candidateBounds.width <= C.FloatZero || candidateBounds.height <= C.FloatZero ||
                popupBounds.width <= C.FloatZero || popupBounds.height <= C.FloatZero)
            {
                return false;
            }

            bool stretchesAcrossParent =
                candidateRect.anchorMin.x <= 0.001f &&
                candidateRect.anchorMin.y <= 0.001f &&
                candidateRect.anchorMax.x >= 0.999f &&
                candidateRect.anchorMax.y >= 0.999f;

            if (!stretchesAcrossParent)
            {
                return false;
            }

            float widthRatio = candidateBounds.width / popupBounds.width;
            float heightRatio = candidateBounds.height / popupBounds.height;
            return widthRatio >= 0.9f && heightRatio >= 0.9f;
        }

        /// <summary>
        /// Detects localization markers that explicitly identify a button as a close action.
        /// </summary>
        private static bool HasCloseLocalizationMarker(GameObject buttonObject)
        {
            if (buttonObject == null)
            {
                return false;
            }

            Lang_Button[] langButtons = buttonObject.GetComponentsInChildren<Lang_Button>(true);
            for (int i = C.ZeroIndex; i < langButtons.Length; i++)
            {
                Lang_Button langButton = langButtons[i];
                if (langButton == null)
                {
                    continue;
                }

                if (MatchesCloseLocalizationKey(langButton.Constant) || MatchesCloseLocalizationKey(langButton.Tooltip))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether a localization key maps to the game's standard close labels.
        /// </summary>
        private static bool MatchesCloseLocalizationKey(string key)
        {
            return string.Equals(key, C.LanguageKeyClose, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, C.LanguageKeyButtonClose, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, C.LanguageKeyPopupClose, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Resolves localized close label with fallback.
        /// </summary>
        private static string GetLocalizedCloseLabel()
        {
            if (Language.Data != null)
            {
                string value;
                if (Language.Data.TryGetValue(C.LanguageKeyClose, out value) && !string.IsNullOrEmpty(value))
                {
                    return value;
                }

                if (Language.Data.TryGetValue(C.LanguageKeyButtonClose, out value) && !string.IsNullOrEmpty(value))
                {
                    return value;
                }

                if (Language.Data.TryGetValue(C.LanguageKeyPopupClose, out value) && !string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return C.LabelCloseFallback;
        }

        /// <summary>
        /// Places Career Diary button immediately right of the close button.
        /// </summary>
        private static void PlaceButtonNearCloseControl(GameObject buttonObject, Button closeButton)
        {
            if (buttonObject == null)
            {
                return;
            }

            if (closeButton == null)
            {
                return;
            }

            if (buttonObject.transform.parent != closeButton.transform.parent)
            {
                buttonObject.transform.SetParent(closeButton.transform.parent, false);
            }

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            if (buttonRect == null || closeRect == null)
            {
                return;
            }

            int targetSiblingIndex = Mathf.Min(
                closeButton.transform.GetSiblingIndex() + C.LastFromCount,
                buttonObject.transform.parent.childCount - C.LastFromCount);
            buttonObject.transform.SetSiblingIndex(targetSiblingIndex);

            buttonRect.anchorMin = closeRect.anchorMin;
            buttonRect.anchorMax = closeRect.anchorMax;
            buttonRect.pivot = closeRect.pivot;
            buttonRect.localScale = closeRect.localScale;
            buttonRect.localRotation = closeRect.localRotation;

            float closeWidth = closeRect.rect.width > C.FloatZero ? closeRect.rect.width : closeRect.sizeDelta.x;
            float closeHeight = closeRect.rect.height > C.FloatZero ? closeRect.rect.height : closeRect.sizeDelta.y;
            float targetWidth = closeWidth;
            LayoutElement buttonLayoutElement = buttonObject.GetComponent<LayoutElement>();

            LayoutGroup parentLayout = closeButton.transform.parent != null
                ? closeButton.transform.parent.GetComponent<LayoutGroup>()
                : null;
            if (parentLayout != null)
            {
                LayoutElement closeLayoutElement = closeButton.GetComponent<LayoutElement>();
                if (buttonLayoutElement == null)
                {
                    buttonLayoutElement = buttonObject.AddComponent<LayoutElement>();
                }

                buttonLayoutElement.ignoreLayout = false;
                if (closeLayoutElement != null)
                {
                    buttonLayoutElement.minWidth = closeLayoutElement.minWidth;
                    buttonLayoutElement.preferredWidth = closeLayoutElement.preferredWidth;
                    buttonLayoutElement.flexibleWidth = closeLayoutElement.flexibleWidth;
                    buttonLayoutElement.minHeight = closeLayoutElement.minHeight;
                    buttonLayoutElement.preferredHeight = closeLayoutElement.preferredHeight;
                    buttonLayoutElement.flexibleHeight = closeLayoutElement.flexibleHeight;
                }
                else
                {
                    buttonLayoutElement.preferredWidth = targetWidth;
                    buttonLayoutElement.preferredHeight = closeHeight;
                }

                return;
            }

            if (buttonLayoutElement != null)
            {
                buttonLayoutElement.ignoreLayout = true;
            }

            buttonRect.sizeDelta = new Vector2(targetWidth, closeHeight);

            float offsetX = ((closeWidth + targetWidth) * C.HeaderPlacementHalfScale) + C.HeaderButtonHorizontalSpacing;
            buttonRect.anchoredPosition = new Vector2(closeRect.anchoredPosition.x + offsetX, closeRect.anchoredPosition.y);
        }

        /// <summary>
        /// Reads visible button text from TMP/Text components.
        /// </summary>
        private static string GetButtonDisplayText(GameObject buttonObject)
        {
            if (buttonObject == null)
            {
                return string.Empty;
            }

            TextMeshProUGUI tmp = buttonObject.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null && !string.IsNullOrEmpty(tmp.text))
            {
                return tmp.text.Trim();
            }

            Text legacy = buttonObject.GetComponentInChildren<Text>(true);
            if (legacy != null && !string.IsNullOrEmpty(legacy.text))
            {
                return legacy.text.Trim();
            }

            return string.Empty;
        }

        /// <summary>
        /// Compares UI labels after trim using case-insensitive ordinal rules.
        /// </summary>
        private static bool StringEqualsInvariant(string left, string right)
        {
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
            {
                return false;
            }

            return string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Strips tab behavior and localization components.
        /// </summary>
        private static void StripTabBehavior(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            Profile_Tab_Button ptb = obj.GetComponent<Profile_Tab_Button>();
            if (ptb != null)
            {
                UnityEngine.Object.DestroyImmediate(ptb);
            }

            Lang_Button[] langButtons = obj.GetComponentsInChildren<Lang_Button>(true);
            for (int i = C.ZeroIndex; i < langButtons.Length; i++)
            {
                Lang_Button lb = langButtons[i];
                if (lb != null)
                {
                    UnityEngine.Object.DestroyImmediate(lb);
                }
            }

            Button[] buttons = obj.GetComponentsInChildren<Button>(true);
            for (int i = C.ZeroIndex; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    buttons[i].onClick = new Button.ButtonClickedEvent();
                }
            }
        }

        /// <summary>
        /// Sets custom button label.
        /// </summary>
        private static void ApplyButtonLabel(GameObject obj, string label)
        {
            if (obj == null)
            {
                return;
            }

            SetText(obj, label);
        }

        /// <summary>
        /// Sets text on TMP/Text nodes under object.
        /// </summary>
        private static void SetText(GameObject obj, string text)
        {
            if (obj == null)
            {
                return;
            }

            ExtensionMethods.SetText(obj, text ?? string.Empty);

            TextMeshProUGUI[] tmps = obj.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = C.ZeroIndex; i < tmps.Length; i++)
            {
                if (tmps[i] != null)
                {
                    tmps[i].text = text ?? string.Empty;
                    tmps[i].enableWordWrapping = true;
                }
            }

            Text[] texts = obj.GetComponentsInChildren<Text>(true);
            for (int i = C.ZeroIndex; i < texts.Length; i++)
            {
                if (texts[i] != null)
                {
                    texts[i].text = text ?? string.Empty;
                }
            }
        }

        /// <summary>
        /// Normalizes cloned action button layer + layout so full button and text remain visible inside rows.
        /// </summary>
        private static void ConfigureActionButtonVisuals(GameObject buttonObject, Transform parent)
        {
            if (buttonObject == null)
            {
                return;
            }

            if (parent != null)
            {
                SetLayerRecursively(buttonObject, parent.gameObject.layer);
            }

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = buttonObject.AddComponent<LayoutElement>();
            }

            if (layout.preferredHeight <= C.FloatZero)
            {
                layout.preferredHeight = C.ActionButtonHeight;
            }
            if (layout.minHeight <= C.FloatZero)
            {
                layout.minHeight = C.ActionButtonHeight;
            }

            if (parent != null && parent.GetComponent<HorizontalLayoutGroup>() != null)
            {
                float preferredWidth = MeasurePreferredButtonWidth(buttonObject);
                if (preferredWidth > C.FloatZero)
                {
                    layout.preferredWidth = preferredWidth;
                    layout.minWidth = C.FloatZero;
                }
            }

            if (parent != null && parent.GetComponent<VerticalLayoutGroup>() != null)
            {
                layout.minWidth = C.FloatZero;
                layout.flexibleWidth = C.FloatOne;
            }
        }

        /// <summary>
        /// Measures one button width from current text components.
        /// </summary>
        private static float MeasurePreferredButtonWidth(
            GameObject buttonObject,
            float minWidth = C.ActionButtonMinimumWidth,
            float maxWidth = C.ActionButtonMaximumWidth)
        {
            if (buttonObject == null)
            {
                return C.FloatZero;
            }

            float normalizedMinWidth = Mathf.Max(C.FloatZero, minWidth);
            float normalizedMaxWidth = Mathf.Max(normalizedMinWidth, maxWidth);

            float maxLabelWidth = C.FloatZero;
            TextMeshProUGUI[] tmps = buttonObject.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = C.ZeroIndex; i < tmps.Length; i++)
            {
                TextMeshProUGUI tmp = tmps[i];
                if (tmp == null)
                {
                    continue;
                }

                tmp.enableWordWrapping = false;
                string text = tmp.text ?? string.Empty;
                if (text.Length == C.MinId)
                {
                    continue;
                }

                Vector2 preferred = tmp.GetPreferredValues(text);
                maxLabelWidth = Mathf.Max(maxLabelWidth, preferred.x);
            }

            Text[] texts = buttonObject.GetComponentsInChildren<Text>(true);
            for (int i = C.ZeroIndex; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null)
                {
                    continue;
                }

                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                string raw = text.text ?? string.Empty;
                if (raw.Length == C.MinId)
                {
                    continue;
                }

                float preferred = text.preferredWidth;
                if (preferred <= C.FloatZero)
                {
                    preferred = raw.Length * C.ActionButtonLegacyCharWidthApprox;
                }

                maxLabelWidth = Mathf.Max(maxLabelWidth, preferred);
            }

            if (maxLabelWidth <= C.FloatZero)
            {
                return normalizedMinWidth;
            }

            return Mathf.Clamp(
                maxLabelWidth + C.ActionButtonHorizontalPadding,
                normalizedMinWidth,
                normalizedMaxWidth);
        }

        /// <summary>
        /// Estimates preferred width for labels before button objects exist.
        /// </summary>
        private static float MeasurePreferredButtonWidthFromLabel(
            string label,
            float minWidth = C.ActionButtonMinimumWidth,
            float maxWidth = C.ActionButtonMaximumWidth)
        {
            string raw = label ?? string.Empty;
            float normalizedMinWidth = Mathf.Max(C.FloatZero, minWidth);
            float normalizedMaxWidth = Mathf.Max(normalizedMinWidth, maxWidth);
            if (raw.Length == C.MinId)
            {
                return normalizedMinWidth;
            }

            return Mathf.Clamp(
                (raw.Length * C.ActionButtonLegacyCharWidthApprox) + C.ActionButtonHorizontalPadding,
                normalizedMinWidth,
                normalizedMaxWidth);
        }

        /// <summary>
        /// Creates generic rect UI object.
        /// </summary>
        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject frameworkObject;
            string frameworkError;
            if (IMUiFrameworkApi.TryCreateUiObject(name, parent, out frameworkObject, out frameworkError) && frameworkObject != null)
            {
                return frameworkObject;
            }

            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            if (parent != null)
            {
                obj.layer = parent.gameObject.layer;
            }
            return obj;
        }

        /// <summary>
        /// Finds descendant by name.
        /// </summary>
        private static Transform FindChildByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (int i = C.ZeroIndex; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                Transform found = FindChildByName(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Clears all children.
        /// </summary>
        private static void ClearChildren(Transform root)
        {
            if (root == null)
            {
                return;
            }

            string frameworkError;
            if (IMUiFrameworkApi.TryClearChildren(root, out frameworkError))
            {
                return;
            }

            for (int i = root.childCount - C.LastFromCount; i >= C.ZeroIndex; i--)
            {
                Transform child = root.GetChild(i);
                if (child != null)
                {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
            }
        }

        /// <summary>
        /// Rebuilds layout when available.
        /// </summary>
        private static void RebuildLayout(Transform root)
        {
            string frameworkError;
            if (IMUiFrameworkApi.TryRebuildLayout(root, out frameworkError))
            {
                return;
            }

            RectTransform rt = root as RectTransform;
            if (rt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
        }

        /// <summary>
        /// Applies layer recursively to object and descendants.
        /// </summary>
        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null)
            {
                return;
            }

            root.layer = layer;
            for (int i = C.ZeroIndex; i < root.transform.childCount; i++)
            {
                Transform child = root.transform.GetChild(i);
                if (child != null)
                {
                    SetLayerRecursively(child.gameObject, layer);
                }
            }
        }

        /// <summary>
        /// Ensures vertical content layout for dynamic rendering.
        /// </summary>
        private static void EnsureVerticalContentLayout(Transform root)
        {
            if (root == null)
            {
                return;
            }

            VerticalLayoutGroup vlg = root.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
            {
                vlg = root.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter fitter = root.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        /// <summary>
        /// Resolves content root inside cloned panel.
        /// </summary>
        private static Transform ResolveContentRoot(GameObject clonedPanel, GameObject sourcePanel, GameObject sourceContent)
        {
            if (clonedPanel == null)
            {
                return null;
            }

            if (sourcePanel == null || sourceContent == null)
            {
                return clonedPanel.transform;
            }

            Transform byName = FindChildByName(clonedPanel.transform, sourceContent.name);
            return byName ?? clonedPanel.transform;
        }

        /// <summary>
        /// Hides base tab panels.
        /// </summary>
        private static void HideBaseTabs(Profile_Popup popup)
        {
            if (popup == null || popup.Tabs == null)
            {
                return;
            }

            for (int i = C.ZeroIndex; i < popup.Tabs.Count; i++)
            {
                Profile_Popup._tab tab = popup.Tabs[i];
                if (tab == null)
                {
                    continue;
                }

                if (tab.Obj != null)
                {
                    tab.Obj.SetActive(false);
                }

                Image image = tab.Button != null ? tab.Button.GetComponent<Image>() : null;
                if (image != null)
                {
                    image.color = popup.Color_Secondary;
                }
            }
        }

        /// <summary>
        /// Restores selected base tab visibility.
        /// </summary>
        private static void RestoreSelectedTab(Profile_Popup popup)
        {
            if (popup == null || popup.Tabs == null)
            {
                return;
            }

            for (int i = C.ZeroIndex; i < popup.Tabs.Count; i++)
            {
                Profile_Popup._tab tab = popup.Tabs[i];
                if (tab == null)
                {
                    continue;
                }

                bool selected = tab.Type == popup.SelectedTab;
                if (tab.Obj != null)
                {
                    tab.Obj.SetActive(selected);
                }

                Image image = tab.Button != null ? tab.Button.GetComponent<Image>() : null;
                if (image != null)
                {
                    image.color = selected ? mainScript.green32 : popup.Color_Secondary;
                }
            }
        }

        /// <summary>
        /// Synchronizes blur/backdrop state to currently active popup entries.
        /// </summary>
        internal static bool TrySyncBackdropWithActiveManagedPopups(PopupManager manager)
        {
            if (manager == null || manager.popups == null)
            {
                return false;
            }

            bool queueBusy = manager.queue != null && manager.queue.Count > C.ZeroIndex;
            bool allowRepair = !queueBusy;
            bool hasActive = false;
            bool requiresBlur = false;
            bool requiresDarken = false;
            RenderTexture activeRenderTexture = null;

            for (int i = C.ZeroIndex; i < manager.popups.Length; i++)
            {
                PopupManager._popup entry = manager.popups[i];
                if (entry == null)
                {
                    continue;
                }

                bool activeInHierarchy = entry.obj != null && entry.obj.activeInHierarchy;
                if (allowRepair && entry.open && !activeInHierarchy)
                {
                    entry.open = false;
                }

                if (allowRepair && TryRepairGhostManagedPopupEntry(manager, entry, ref activeInHierarchy, queueBusy))
                {
                    entry.open = false;
                }

                if (!entry.open && !activeInHierarchy)
                {
                    continue;
                }

                hasActive = true;
                if (entry.BGBlur)
                {
                    requiresBlur = true;
                }

                if (entry.BGDarken)
                {
                    requiresDarken = true;
                }

                if (activeRenderTexture == null && entry.BGRenderTexture != null)
                {
                    activeRenderTexture = entry.BGRenderTexture;
                }
            }

            if (!hasActive)
            {
                return false;
            }

            try
            {
                manager.BGBlur(requiresBlur, C.BlurCleanupDurationSeconds);
            }
            catch
            {
            }

            if (manager.BG != null)
            {
                CanvasGroup bgGroup = manager.BG.GetComponent<CanvasGroup>();
                if (requiresDarken)
                {
                    manager.BG.SetActive(true);
                    if (bgGroup != null)
                    {
                        bgGroup.alpha = C.FloatOne;
                    }
                }
                else
                {
                    if (bgGroup != null)
                    {
                        bgGroup.alpha = C.FloatZero;
                    }

                    manager.BG.SetActive(false);
                }
            }

            if (manager.BGImage != null)
            {
                CanvasGroup bgImageGroup = manager.BGImage.GetComponent<CanvasGroup>();
                RawImage raw = manager.BGImage.GetComponent<RawImage>();
                if (activeRenderTexture != null)
                {
                    manager.BGImage.SetActive(true);
                    if (raw != null)
                    {
                        raw.texture = activeRenderTexture;
                    }

                    if (bgImageGroup != null)
                    {
                        bgImageGroup.alpha = C.FloatOne;
                    }
                }
                else
                {
                    if (bgImageGroup != null)
                    {
                        bgImageGroup.alpha = C.FloatZero;
                    }

                    manager.BGImage.SetActive(false);
                }
            }

            manager.BlockInput(true);
            return true;
        }

        /// <summary>
        /// Repairs stale popup state and clears blur/backdrop only when no popup is truly active.
        /// </summary>
        internal static bool TryRunPopupBackdropSafetyNet(PopupManager manager, bool resetPopupCounter)
        {
            if (manager == null)
            {
                return false;
            }

            if (ActiveDialogueController.ShowingDialogue)
            {
                return false;
            }

            if (manager.queue != null && manager.queue.Count > C.ZeroIndex)
            {
                return false;
            }

            if (HasManagedPopupOpenOrActive(manager, true))
            {
                return false;
            }

            if (manager.BGImage != null)
            {
                CanvasGroup bgImageGroup = manager.BGImage.GetComponent<CanvasGroup>();
                if (bgImageGroup != null)
                {
                    bgImageGroup.alpha = C.FloatZero;
                }

                manager.BGImage.SetActive(false);
            }

            if (manager.BG != null)
            {
                CanvasGroup bgGroup = manager.BG.GetComponent<CanvasGroup>();
                if (bgGroup != null)
                {
                    bgGroup.alpha = C.FloatZero;
                }

                manager.BG.SetActive(false);
            }

            try
            {
                manager.BGBlur(false, C.BlurCleanupDurationSeconds);
            }
            catch
            {
            }

            ForceDisableCameraBlur(manager);

            manager.BlockInput(false);

            if (resetPopupCounter && PopupManager.PopupCounter > C.ZeroIndex)
            {
                PopupManager.PopupCounter = C.ZeroIndex;
            }

            mainScript main = Camera.main != null ? Camera.main.GetComponent<mainScript>() : null;
            if (main != null && ShouldResumeTimeAfterPopupClose())
            {
                main.Time_Resume();
            }

            return true;
        }

        /// <summary>
        /// Returns true when popup manager still has a visible/active popup.
        /// </summary>
        private static bool HasManagedPopupOpenOrActive(PopupManager manager, bool repairStaleState)
        {
            if (manager == null || manager.popups == null)
            {
                return false;
            }

            bool queueBusy = manager.queue != null && manager.queue.Count > C.ZeroIndex;
            bool allowRepair = repairStaleState && !queueBusy;
            for (int i = C.ZeroIndex; i < manager.popups.Length; i++)
            {
                PopupManager._popup entry = manager.popups[i];
                if (entry == null)
                {
                    continue;
                }

                bool activeInHierarchy = entry.obj != null && entry.obj.activeInHierarchy;
                if (allowRepair && entry.open && !activeInHierarchy)
                {
                    entry.open = false;
                }

                if (allowRepair && TryRepairGhostManagedPopupEntry(manager, entry, ref activeInHierarchy, queueBusy))
                {
                    entry.open = false;
                }

                if (entry.open || activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true when at least one managed popup is visibly active.
        /// </summary>
        private static bool HasVisibleManagedPopup(PopupManager manager, bool repairStaleState)
        {
            if (manager == null || manager.popups == null)
            {
                return false;
            }

            bool queueBusy = manager.queue != null && manager.queue.Count > C.ZeroIndex;
            bool allowRepair = repairStaleState && !queueBusy;
            for (int i = C.ZeroIndex; i < manager.popups.Length; i++)
            {
                PopupManager._popup entry = manager.popups[i];
                if (entry == null)
                {
                    continue;
                }

                bool activeInHierarchy = entry.obj != null && entry.obj.activeInHierarchy;
                if (allowRepair && entry.open && !activeInHierarchy)
                {
                    entry.open = false;
                }

                if (allowRepair && TryRepairGhostManagedPopupEntry(manager, entry, ref activeInHierarchy, queueBusy))
                {
                    entry.open = false;
                }

                if (!entry.open && !activeInHierarchy)
                {
                    continue;
                }

                if (!activeInHierarchy || entry.obj == null)
                {
                    continue;
                }

                CanvasGroup canvasGroup = entry.obj.GetComponent<CanvasGroup>();
                if (canvasGroup == null || canvasGroup.alpha > C.PopupGhostAlphaThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Repairs hidden ghost popup objects left active after interrupted hide transitions.
        /// </summary>
        private static bool TryRepairGhostManagedPopupEntry(PopupManager manager, PopupManager._popup entry, ref bool activeInHierarchy, bool queueBusy)
        {
            if (manager == null || entry == null || entry.obj == null || !activeInHierarchy)
            {
                return false;
            }

            Popup popupComponent = entry.obj.GetComponent<Popup>();
            CanvasGroup canvasGroup = entry.obj.GetComponent<CanvasGroup>();
            if (popupComponent == null)
            {
                return false;
            }

            bool hidden = canvasGroup != null && canvasGroup.alpha <= C.PopupGhostAlphaThreshold;
            bool nonInteractive = canvasGroup != null && (!canvasGroup.blocksRaycasts || !canvasGroup.interactable);
            bool staleClosedEntry = !entry.open && (!queueBusy || nonInteractive);
            if (!hidden && !staleClosedEntry)
            {
                return false;
            }

            if (canvasGroup == null && entry.open && queueBusy)
            {
                return false;
            }

            entry.obj.SetActive(false);
            activeInHierarchy = false;
            if (popupComponent.Increase_Popup_Counter && PopupManager.PopupCounter > C.ZeroIndex)
            {
                PopupManager.PopupCounter--;
            }
            return true;
        }

        private static void ForceDisableCameraBlur(PopupManager manager)
        {
            Camera targetCamera = null;
            if (manager != null && manager.MainCamera != null)
            {
                targetCamera = manager.MainCamera.GetComponent<Camera>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                return;
            }

            DisableBlurComponent(targetCamera.gameObject, C.TypeNameSuperBlur);
            DisableBlurComponent(targetCamera.gameObject, C.TypeNameSuperBlurFast);
        }

        private static void DisableBlurComponent(GameObject host, string typeName)
        {
            if (host == null || string.IsNullOrEmpty(typeName))
            {
                return;
            }

            Type blurType = AccessTools.TypeByName(typeName);
            if (blurType == null)
            {
                return;
            }

            Component component = host.GetComponent(blurType);
            if (component == null)
            {
                return;
            }

            Behaviour behaviour = component as Behaviour;
            if (behaviour != null)
            {
                behaviour.enabled = false;
            }

            try
            {
                PropertyInfo interpolationProperty = blurType.GetProperty(C.MemberNameInterpolation, BindingFlags.Public | BindingFlags.Instance);
                if (interpolationProperty != null && interpolationProperty.CanWrite)
                {
                    interpolationProperty.SetValue(component, C.FloatZero, null);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Mirrors base-game guards for safely resuming time after popup close.
        /// </summary>
        private static bool ShouldResumeTimeAfterPopupClose()
        {
            try
            {
                if (data_girls.new_girls != null && data_girls.new_girls.Count > C.ZeroIndex)
                {
                    return false;
                }

                if (Substories_Manager.IntroGirls != null && Substories_Manager.IntroGirls.Count > C.ZeroIndex)
                {
                    return false;
                }
            }
            catch
            {
            }

            return true;
        }

        /// <summary>
        /// Gets main script and popup manager.
        /// </summary>
        internal static bool TryGetMainAndPopup(out mainScript main, out PopupManager popup)
        {
            main = null;
            popup = null;

            Camera cam = Camera.main;
            if (cam == null)
            {
                return false;
            }

            main = cam.GetComponent<mainScript>();
            if (main == null || main.Data == null)
            {
                return false;
            }

            popup = main.Data.GetComponent<PopupManager>();
            return popup != null;
        }
    }

    /// <summary>
    /// Bootstraps core session at popup manager start.
    /// </summary>
    [HarmonyPatch(typeof(PopupManager), C.MemberNameStart)]
    [HarmonyAfter(new string[] { C.HarmonyIdImDataCore, C.HarmonyIdImUiFramework, C.HarmonyIdUiRecovery, C.HarmonyIdGraduationCalendar })]
    internal static class PopupManagerStartPatch
    {
        /// <summary>
        /// Initializes runtime core session.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(PopupManager __instance)
        {
            if (!Runtime.EnsureHardDependencyAvailable())
            {
                return;
            }

            string uiFrameworkError;
            if (!IMUiFrameworkApi.TryInitialize(__instance, out uiFrameworkError) && !string.IsNullOrEmpty(uiFrameworkError))
            {
                Log.Warn(C.TextImUiFrameworkInitializationCallFailed + uiFrameworkError);
            }

            Runtime.BootstrapSessionIfNeeded();
            DiarySettings.EnsureLoaded();
            ModInfoCatalog.EnsureLoaded();
            CustomDiaryCatalog.EnsureLoaded();
        }
    }

    internal sealed class CareerDiaryPopupRecoveryTicker : MonoBehaviour
    {
        private const float TickIntervalSeconds = C.PopupRecoveryTickIntervalSeconds;
        private float nextTickAt;

        internal static void Ensure(PopupManager manager)
        {
            if (manager == null)
            {
                return;
            }

            if (manager.GetComponent<CareerDiaryPopupRecoveryTicker>() != null)
            {
                return;
            }

            manager.gameObject.AddComponent<CareerDiaryPopupRecoveryTicker>();
        }

        private void OnEnable()
        {
            nextTickAt = Time.unscaledTime + C.PopupRecoveryInitialDelaySeconds;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextTickAt)
            {
                return;
            }

            nextTickAt = Time.unscaledTime + TickIntervalSeconds;

            mainScript main;
            PopupManager popup;
            if (!CareerDiaryController.TryGetMainAndPopup(out main, out popup))
            {
                return;
            }

            if (popup.queue != null && popup.queue.Count > C.ZeroIndex)
            {
                return;
            }

            if (CareerDiaryController.TrySyncBackdropWithActiveManagedPopups(popup))
            {
                return;
            }

            CareerDiaryController.TryRunPopupBackdropSafetyNet(popup, true);
        }
    }

    /// <summary>
    /// Enforces UI overlay cleanup after popup close to avoid stuck blur/backdrop.
    /// </summary>
    [HarmonyPatch(typeof(PopupManager))]
    [HarmonyAfter(new string[] { C.HarmonyIdImUiFramework, C.HarmonyIdUiRecovery, C.HarmonyIdGraduationCalendar, C.HarmonyIdGraduationDetails })]
    internal static class PopupManagerClosePatch
    {
        private static MethodInfo cachedCloseMethod;
        private static bool hasLoggedMissingCloseMethod;

        private static bool Prepare()
        {
            MethodInfo ignored;
            return TryResolveTargetMethod(out ignored);
        }

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            MethodInfo target;
            if (!TryResolveTargetMethod(out target))
            {
                return null;
            }

            return target;
        }

        private static bool TryResolveTargetMethod(out MethodInfo target)
        {
            if (cachedCloseMethod != null)
            {
                target = cachedCloseMethod;
                return true;
            }

            MethodInfo actionCallbackCandidate = null;
            MethodInfo noArgsCandidate = null;
            MethodInfo fallbackCandidate = null;

            MethodInfo[] methods = typeof(PopupManager).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = C.ZeroIndex; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method == null)
                {
                    continue;
                }

                bool isCloseName =
                    string.Equals(method.Name, C.CloseMethodToken, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(method.Name, C.CloseTokenLower, StringComparison.OrdinalIgnoreCase);
                if (!isCloseName)
                {
                    continue;
                }

                if (fallbackCandidate == null)
                {
                    fallbackCandidate = method;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters == null)
                {
                    continue;
                }

                if (parameters.Length == C.LastFromCount && parameters[C.ZeroIndex].ParameterType == typeof(Action))
                {
                    actionCallbackCandidate = method;
                    continue;
                }

                if (parameters.Length == C.ZeroIndex && noArgsCandidate == null)
                {
                    noArgsCandidate = method;
                }
            }

            cachedCloseMethod = actionCallbackCandidate ?? noArgsCandidate ?? fallbackCandidate;
            target = cachedCloseMethod;

            if (target == null && !hasLoggedMissingCloseMethod)
            {
                hasLoggedMissingCloseMethod = true;
                Log.Warn("PopupManager close patch skipped: could not resolve PopupManager.Close overload.");
            }

            return target != null;
        }

        /// <summary>
        /// Applies final cleanup once queue is empty and no popup remains open.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(PopupManager __instance)
        {
            if (__instance == null)
            {
                return;
            }

            if (__instance.queue != null && __instance.queue.Count > C.ZeroIndex)
            {
                return;
            }

            if (CareerDiaryController.TrySyncBackdropWithActiveManagedPopups(__instance))
            {
                return;
            }

            if (!CareerDiaryController.TryRunPopupBackdropSafetyNet(__instance, true))
            {
                return;
            }

            CareerDiaryController[] controllers = UnityEngine.Object.FindObjectsOfType<CareerDiaryController>();
            for (int i = C.ZeroIndex; i < controllers.Length; i++)
            {
                CareerDiaryController controller = controllers[i];
                if (controller != null)
                {
                    controller.OnPopupClosedExternal();
                }
            }
        }
    }

    /// <summary>
    /// Injects diary UI on profile set.
    /// </summary>
    [HarmonyPatch(typeof(Profile_Popup), nameof(Profile_Popup.Set))]
    [HarmonyAfter(new string[] { C.HarmonyIdGraduationDetails, C.HarmonyIdGraduationCalendar, C.HarmonyIdGraduationRebalances, C.HarmonyIdDivorceFix })]
    internal static class ProfilePopupSetPatch
    {
        /// <summary>
        /// Ensures controller exists and initializes it.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Profile_Popup __instance, data_girls.girls _Girl)
        {
            if (__instance == null)
            {
                return;
            }

            if (!Runtime.EnsureHardDependencyAvailable())
            {
                return;
            }

            CareerDiaryController controller = __instance.gameObject.GetComponent<CareerDiaryController>();
            if (controller == null)
            {
                controller = __instance.gameObject.AddComponent<CareerDiaryController>();
            }

            controller.Initialize(__instance, _Girl);
        }
    }

    /// <summary>
    /// Fallback UI injection on header render.
    /// </summary>
    [HarmonyPatch(typeof(Profile_Popup), C.MethodRenderHeader)]
    [HarmonyAfter(new string[] { C.HarmonyIdGraduationDetails, C.HarmonyIdGraduationCalendar, C.HarmonyIdGraduationRebalances, C.HarmonyIdDivorceFix })]
    internal static class ProfilePopupRenderHeaderPatch
    {
        /// <summary>
        /// Reinitializes controller on header redraw.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Profile_Popup __instance)
        {
            if (__instance == null)
            {
                return;
            }

            if (!Runtime.EnsureHardDependencyAvailable())
            {
                return;
            }

            CareerDiaryController controller = __instance.gameObject.GetComponent<CareerDiaryController>();
            if (controller == null)
            {
                controller = __instance.gameObject.AddComponent<CareerDiaryController>();
            }

            controller.Initialize(__instance, __instance.Girl);
        }
    }

    /// <summary>
    /// Synchronizes diary state with base tab changes.
    /// </summary>
    [HarmonyPatch(typeof(Profile_Popup), C.MethodSetTab)]
    [HarmonyAfter(new string[] { C.HarmonyIdGraduationDetails, C.HarmonyIdGraduationCalendar, C.HarmonyIdGraduationRebalances, C.HarmonyIdDivorceFix })]
    internal static class ProfilePopupSetTabPatch
    {
        /// <summary>
        /// Hides diary if player switched base tab.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Profile_Popup __instance)
        {
            if (__instance == null)
            {
                return;
            }

            if (!Runtime.EnsureHardDependencyAvailable())
            {
                return;
            }

            CareerDiaryController controller = __instance.gameObject.GetComponent<CareerDiaryController>();
            if (controller != null)
            {
                controller.OnBaseTabSelected();
            }
        }
    }

    /// <summary>
    /// Appends birthday milestones into IM Data Core so they appear in diary timeline.
    /// </summary>
    [HarmonyPatch(typeof(Birthday), nameof(Birthday.DoBirthday))]
    [HarmonyAfter(new string[] { C.HarmonyIdImDataCore })]
    internal static class BirthdayDoBirthdayPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(data_girls.girls Girl)
        {
            Runtime.TryAppendBirthdayEvent(Girl);
        }
    }

    /// <summary>
    /// In diary viewer mode, keeps released senbatsu members visible even if they later graduated.
    /// </summary>
    [HarmonyPatch(typeof(SinglePopup_Senbatsu), C.MemberNameLoadSenbatsu)]
    internal static class SinglePopupSenbatsuLoadPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(SinglePopup_Senbatsu __instance, singles._single _Single)
        {
            if (__instance == null || _Single == null || _Single.girls == null || __instance.Senbatsu == null)
            {
                return;
            }

            if (!CareerDiaryController.IsDiarySingleSenbatsuViewerActive(__instance))
            {
                return;
            }

            int slotCount = Mathf.Min(__instance.Senbatsu.Length, _Single.girls.Count);
            for (int i = C.ZeroIndex; i < slotCount; i++)
            {
                SinglePopup_Senbatsu._senbatsu slot = __instance.Senbatsu[i];
                if (slot == null)
                {
                    continue;
                }

                slot.SetGirl(_Single.girls[i], null);
                if (slot.reciever == null)
                {
                    continue;
                }

                SelfDestroy[] lingering = slot.reciever.GetComponentsInChildren<SelfDestroy>();
                for (int j = C.ZeroIndex; j < lingering.Length; j++)
                {
                    lingering[j].DestroyNow();
                }
            }
        }
    }
}



