using System;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using IMDataCore;
using ModLocalizationSystem;

namespace MonthlyLedger
{
    internal static class MonthlyLedgerDetailConstants
    {
        internal const string KindContract = "contract";
        internal const string KindSingle = "single";
        internal const string KindShow = "show";
        internal const string KindIdolSalary = "idol_salary";
        internal const string KindStaffSalary = "staff_salary";
        internal const string KindTheaterAttendance = "theater_attendance";
        internal const string KindTheaterStreaming = "theater_streaming";
        internal const string KindCafeDaily = "cafe_daily";
        internal const string KindConcert = "concert";

        internal const string TheaterPerformance = "performance";
        internal const string TheaterManzai = "manzai";
        internal const string TheaterDayOff = "day_off";
        internal const string TheaterAuto = "auto";
        internal const string TheaterEveryone = "everyone";

        internal const string FanMale = "male";
        internal const string FanFemale = "female";
        internal const string FanCasual = "casual";
        internal const string FanHardcore = "hardcore";
        internal const string FanTeen = "teen";
        internal const string FanYoungAdult = "youngadult";
        internal const string FanAdult = "adult";

        internal const string VenueClub = "club";
        internal const string VenueConcertHall = "concerthall";
        internal const string VenueOpenAirStage = "openairstage";
        internal const string VenueStadium = "stadium";
        internal const string VenueTokyoColiseum = "tokyocoliseum";

        internal const string ContractPhotoshoot = "photoshoot";
        internal const string ContractAdvertisement = "ad";
        internal const string ContractTelevisionDrama = "tv_drama";
        internal const string ContractVariety = "variety";

        internal const string SkillSpeed = "speed";
        internal const string SkillCoaching = "coaching";
        internal const string SkillProduction = "production";
        internal const string SkillDeals = "deals";
        internal const string SkillPhysicalHealth = "physical_health";
        internal const string SkillMentalHealth = "mental_health";
        internal const string SkillCutePretty = "cute_pretty";
        internal const string SkillCoolSexy = "cool_sexy";
        internal const string SkillInfluence = "influence";

        internal const string StaffRoleDanceCoach = "dance_coach";
        internal const string StaffRoleChoreographer = "choreographer";
        internal const string StaffRoleVoiceCoach = "voice_coach";
        internal const string StaffRoleMusicProducer = "music_producer";
        internal const string StaffRoleProductionManager = "production_manager";
        internal const string StaffRoleSalesManager = "sales_manager";
        internal const string StaffRolePhysician = "physician";
        internal const string StaffRolePsychiatrist = "psychiatrist";
        internal const string StaffRoleStylistCutePretty = "stylist_cute_pretty";
        internal const string StaffRoleStylistCoolSexy = "stylist_cool_sexy";
        internal const string StaffRolePlayer = "player";
        internal const string StaffRolePlayerFemale = "player_female";
        internal const string StaffRoleAssistantManagerPrimary = "12010";
        internal const string StaffRoleAssistantManagerSecondary = "12012";
        internal const string AssistantManagerHarmonyId = "com.cosmo.assistantmanager";

        internal const string VanillaContractPhotoshoot = "BIZ__PHOTOSHOOT";
        internal const string VanillaContractAdvertisement = "BIZ__AD_SHORT";
        internal const string VanillaContractTelevisionDrama = "BIZ__DRAMA_SHORT";
        internal const string VanillaContractVariety = "BIZ__VARIETY";
        internal const string VanillaPayment = "BIZ__PAYMENT";
        internal const string VanillaStamina = "STAMINA";
        internal const string VanillaLiability = "BIZ__LIABILITY";
        internal const string VanillaNegotiations = "BIZ__NEGOTIATIONS";
        internal const string VanillaGroup = "GROUP";
        internal const string VanillaIdols = "IDOLS";
        internal const string VanillaSingleMarketing = "SINGLE__MARKETING";
        internal const string VanillaSingleGenre = "SINGLE__GENRE";
        internal const string VanillaSingleLyrics = "SINGLE__LYRICS";
        internal const string VanillaSingleChoreography = "SINGLE__CHOREO";
        internal const string VanillaSingleProductionCost = "SINGLE__PROD_COST";
        internal const string VanillaSingleRevenue = "SINGLE__REVENUE";
        internal const string VanillaShowAudience = "SHOW__AUDIENCE";
        internal const string VanillaShowGenre = "SHOW__GENRE";
        internal const string VanillaShowHost = "SHOW__HOST";
        internal const string VanillaShowCast = "SHOW__CAST";
        internal const string VanillaShowFatigue = "SHOW__FATIGUE";
        internal const string VanillaShowWeeklyBudget = "SHOW__WEEKLY_BUDGET";
        internal const string VanillaShowEpisode = "SHOW__EPISODE";
        internal const string VanillaShowRevenue = "SHOW__REVENUE";
        internal const string VanillaSalary = "SALARY";
        internal const string VanillaFame = "FAME";
        internal const string VanillaScandalPoints = "SCANDAL_POINTS";
        internal const string VanillaSkillSpeed = "STAFF__SPEED";
        internal const string VanillaSkillCoaching = "STAFF__COACHING";
        internal const string VanillaSkillProduction = "STAFF__PRODUCTION";
        internal const string VanillaSkillDeals = "STAFF__DEALS";
        internal const string VanillaSkillPhysicalHealth = "STAFF__PHYS_HEALTH";
        internal const string VanillaSkillMentalHealth = "STAFF__MENT_HEALTH";
        internal const string VanillaSkillInfluence = "STAFF__INFLUENCE";
        internal const string VanillaLevel = "LEVEL";
        internal const string VanillaRoleDanceCoach = "STAFF__DANCE_COACH";
        internal const string VanillaRoleChoreographer = "STAFF__CHOREO";
        internal const string VanillaRoleVoiceCoach = "STAFF__VOICE";
        internal const string VanillaRoleMusicProducer = "STAFF__MUSIC";
        internal const string VanillaRoleProductionManager = "STAFF__PROD_MANAGER";
        internal const string VanillaRoleSalesManager = "STAFF__SALES_MANAGER";
        internal const string VanillaRolePhysician = "STAFF__PHYSICIAN";
        internal const string VanillaRolePsychiatrist = "STAFF__PSYCHIATRIST";
        internal const string VanillaRoleStylist = "STAFF__STYLIST";
        internal const string VanillaRoleProducer = "STAFF__PRODUCER";
        internal const string VanillaCute = "CUTE";
        internal const string VanillaPretty = "PRETTY";
        internal const string VanillaCool = "COOL";
        internal const string VanillaSexy = "SEXY";
        internal const string VanillaTheaterPerformance = "THEATER__PERFORMANCE";
        internal const string VanillaTheaterManzai = "THEATER__MANZAI";
        internal const string VanillaTheaterDayOff = "THEATER__DAY_OFF";
        internal const string VanillaTheaterAuto = "CAFE__AUTO";
        internal const string VanillaTheaterEveryone = "THEATER__EVERYONE";
        internal const string VanillaTheaterStreaming = "THEATER__STREAMING";
        internal const string VanillaTheaterSubscribers = "THEATER__SUBS";
        internal const string VanillaTheaterSubscriptionPrice = "THEATER__SUB_PRICE";
        internal const string VanillaTicketPrice = "CONCERT__TICKET_PRICE";
        internal const string VanillaCafeDishOfDay = "CAFE__DISH_OF_THE_DAY";
        internal const string VanillaCafeNewFans = "CAFE__NEW_FANS";
        internal const string VanillaStaff = "LP_TITLE__STAFF";
        internal const string VanillaConcertVenue = "CONCERT__VENUE";
        internal const string VanillaConcertProjectedHype = "CONCERT__PROJECTED_HYPE";
        internal const string VanillaConcertProjectedAttendance = "CONCERT__PROJECTED_ATTENDANCE";
        internal const string VanillaConcertProductionCost = "CONCERT__PRODUCTION_COST";
        internal const string VanillaConcertHype = "CONCERT__HYPE";
        internal const string VanillaConcertRevenue = "CONCERT__REVENUE";
        internal const string VanillaConcertProfit = "CONCERT__PROFIT";
        internal const string VanillaConcertSetlist = "CONCERT__SETLIST";
        internal const string VanillaConcertCenter = "CONCERT__CENTER";
        internal const string VanillaConcertTalkBreak = "CONCERT__TALK_BREAK";
        internal const string VanillaVenueClub = "CONCERT__CLUB";
        internal const string VanillaVenueConcertHall = "CONCERT__CONCERT_HALL";
        internal const string VanillaVenueOpenAir = "CONCERT__OPEN_AIR";
        internal const string VanillaVenueStadium = "CONCERT__STADIUM";
        internal const string VanillaVenueColiseum = "CONCERT__COLISEUM";
        internal const string VanillaFanMale = "FAN_M";
        internal const string VanillaFanFemale = "FAN_F";
        internal const string VanillaFanCasual = "FAN_C";
        internal const string VanillaFanHardcore = "FAN_HC";
        internal const string VanillaFanTeen = "FAN_T";
        internal const string VanillaFanYoungAdult = "FAN_YA";
        internal const string VanillaFanAdult = "FAN_A";

        internal const string KeyContractor = "detail.label.contractor";
        internal const string KeyProduct = "detail.label.product";
        internal const string KeyIdol = "detail.label.idol";
        internal const string KeyMultiplier = "detail.label.multiplier";
        internal const string KeyMedium = "detail.label.medium";
        internal const string KeyFanAudience = "detail.label.fan_audience";
        internal const string KeySkills = "detail.label.skills";
        internal const string KeyRole = "detail.label.role";
        internal const string KeyProgress = "detail.label.progress";
        internal const string KeyAssistantManagerRole = "detail.role.assistant_manager";
        internal const string KeyMultiplierValue = "detail.format.multiplier";
        internal const string KeySkillValue = "detail.format.skill";
        internal const string KeyDailyAmount = "detail.label.daily_amount";
        internal const string KeyMonthlyAmount = "detail.label.monthly_amount";
        internal const string KeyPerformanceType = "detail.label.performance_type";
        internal const string KeyAudienceType = "detail.label.audience_type";
        internal const string KeyAttendance = "detail.label.attendance";
        internal const string KeySubscriberChange = "detail.label.subscriber_change";
        internal const string KeyProfitLoss = "detail.label.profit_loss";
        internal const string KeyAppeal = "detail.label.appeal";
        internal const string KeyUnstaffed = "detail.value.unstaffed";
        internal const string KeyAccidents = "detail.label.accidents";
        internal const string KeyAccidentSuccesses = "detail.label.accident_successes";
        internal const string KeyAccidentFailures = "detail.label.accident_failures";
        internal const string KeyAccidentCriticalFailures = "detail.label.accident_critical_failures";
        internal const string KeyTrack = "detail.label.track";
        internal const string KeyFinished = "detail.label.finished";

        internal const string FallbackContractor = "Contractor";
        internal const string FallbackProduct = "Product";
        internal const string FallbackIdol = "Idol";
        internal const string FallbackIdols = "Idols";
        internal const string FallbackMultiplier = "Multiplier";
        internal const string FallbackMedium = "Medium";
        internal const string FallbackFanAudience = "Fan audience";
        internal const string FallbackSkills = "Skills";
        internal const string FallbackRole = "Role";
        internal const string FallbackProgress = "Progress";
        internal const string FallbackAssistantManager = "Assistant Manager";
        internal const string FallbackPhotoshoot = "Photoshoot";
        internal const string FallbackAdvertisement = "Advertisement";
        internal const string FallbackTelevisionDrama = "TV Drama";
        internal const string FallbackVariety = "Variety Show";
        internal const string FallbackPayment = "Payment";
        internal const string FallbackStamina = "Stamina";
        internal const string FallbackLiability = "Liability";
        internal const string FallbackNegotiations = "Negotiations";
        internal const string FallbackGroup = "Group";
        internal const string FallbackMarketing = "Marketing";
        internal const string FallbackGenre = "Genre";
        internal const string FallbackLyrics = "Lyrics";
        internal const string FallbackChoreography = "Choreography";
        internal const string FallbackProductionCost = "Production cost";
        internal const string FallbackRevenue = "Revenue";
        internal const string FallbackAudience = "Audience";
        internal const string FallbackHost = "Host";
        internal const string FallbackCast = "Cast";
        internal const string FallbackFatigue = "Fatigue";
        internal const string FallbackWeeklyBudget = "Weekly budget";
        internal const string FallbackEpisode = "Episode";
        internal const string FallbackSalary = "Salary";
        internal const string FallbackLevel = "Level";
        internal const string FallbackFame = "Fame";
        internal const string FallbackScandalPoints = "Scandal points";
        internal const string FallbackSpeed = "Speed";
        internal const string FallbackCoaching = "Coaching";
        internal const string FallbackProduction = "Production";
        internal const string FallbackDeals = "Deals";
        internal const string FallbackPhysicalHealth = "Physical health";
        internal const string FallbackMentalHealth = "Mental health";
        internal const string FallbackInfluence = "Influence";
        internal const string FallbackDanceCoach = "Dance Coach";
        internal const string FallbackChoreographer = "Choreographer";
        internal const string FallbackVoiceCoach = "Voice Coach";
        internal const string FallbackMusicProducer = "Music Producer";
        internal const string FallbackProductionManager = "Production Manager";
        internal const string FallbackSalesManager = "Sales Manager";
        internal const string FallbackPhysician = "Physician";
        internal const string FallbackPsychiatrist = "Psychiatrist";
        internal const string FallbackStylist = "Stylist";
        internal const string FallbackProducer = "Producer";
        internal const string FallbackCute = "Cute";
        internal const string FallbackPretty = "Pretty";
        internal const string FallbackCool = "Cool";
        internal const string FallbackSexy = "Sexy";
        internal const string FallbackMultiplierValue = "{0}x";
        internal const string FallbackSkillValue = "{0} {1} ({2}%)";
        internal const string FallbackDailyAmount = "Daily amount";
        internal const string FallbackMonthlyAmount = "Monthly amount";
        internal const string FallbackPerformanceType = "Performance type";
        internal const string FallbackAudienceType = "Audience type";
        internal const string FallbackAttendance = "Attendance";
        internal const string FallbackSubscriberChange = "Subscriber gain/loss";
        internal const string FallbackProfitLoss = "Profit/loss";
        internal const string FallbackAppeal = "Appeal";
        internal const string FallbackUnstaffed = "Unstaffed";
        internal const string FallbackAccidents = "Accidents";
        internal const string FallbackAccidentSuccesses = "Successes";
        internal const string FallbackAccidentFailures = "Failures";
        internal const string FallbackAccidentCriticalFailures = "Critical failures";
        internal const string FallbackTrack = "Track";
        internal const string FallbackFinished = "Finished";
        internal const string FallbackAuto = "Auto";
        internal const string FallbackManzai = "Manzai";
        internal const string FallbackDayOff = "Day off";
        internal const string FallbackEveryone = "Everyone";
        internal const string FallbackMale = "Male";
        internal const string FallbackFemale = "Female";
        internal const string FallbackCasual = "Casual";
        internal const string FallbackHardcore = "Hardcore";
        internal const string FallbackTeen = "Teen";
        internal const string FallbackYoungAdult = "Young adult";
        internal const string FallbackAdult = "Adult";
        internal const string FallbackClub = "Club";
        internal const string FallbackConcertHall = "Concert Hall";
        internal const string FallbackOpenAir = "Open-Air Stage";
        internal const string FallbackStadium = "Stadium";
        internal const string FallbackColiseum = "Tokyo Coliseum";
        internal const string FallbackVenue = "Venue";
        internal const string FallbackTicketPrice = "Ticket price";
        internal const string FallbackSubscriptionPrice = "Subscription price";
        internal const string FallbackDishOfDay = "Dish of the day";
        internal const string FallbackSubscribers = "Subscribers";
        internal const string FallbackNewFans = "New fans";
        internal const string FallbackProjectedHype = "Projected hype";
        internal const string FallbackProjectedAttendance = "Projected attendance";
        internal const string FallbackHype = "Hype";
        internal const string FallbackSetlist = "Setlist";

        internal const string FieldSeparator = ": ";
        internal const string ItemSeparator = "  •  ";
        internal const string ListSeparator = ", ";
        internal const string CombinedSkillSeparator = " / ";
        internal const string LineSeparator = "\n";
        internal const string MultiplierNumberFormat = "0.##";
        internal const string ProgressNumberFormat = "0";
        internal const string PercentageSuffix = "%";
        internal const string SignedPositivePrefix = "+";
        internal const int MinimumStructuredLineCount = 1;
        internal const int AssistantManagerPrimaryStaffTypeValue = 12010;
        internal const int AssistantManagerSecondaryStaffTypeValue = 12012;
        internal const float PercentageMultiplier = 100f;
    }

    internal static class MonthlyLedgerDetailText
    {
        internal static bool HasStructuredDetails(IMDataCoreMoneyTransaction transaction)
        {
            return transaction != null && transaction.Details != null && !string.IsNullOrEmpty(transaction.Details.Kind);
        }

        internal static int CountLines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return MonthlyLedgerDetailConstants.MinimumStructuredLineCount;
            }

            return text.Split(new string[] { MonthlyLedgerDetailConstants.LineSeparator }, StringSplitOptions.None).Length;
        }

        internal static string Format(IMDataCoreMoneyTransaction transaction)
        {
            if (!HasStructuredDetails(transaction))
            {
                return MonthlyLedgerText.Detail(transaction != null ? transaction.DetailCode : string.Empty, transaction != null ? transaction.CategoryCode : string.Empty);
            }

            IMDataCoreMoneyTransactionDetail details = transaction.Details;
            switch (details.Kind)
            {
                case MonthlyLedgerDetailConstants.KindContract: return FormatContract(details);
                case MonthlyLedgerDetailConstants.KindSingle: return FormatSingle(details);
                case MonthlyLedgerDetailConstants.KindShow: return FormatShow(details);
                case MonthlyLedgerDetailConstants.KindIdolSalary: return FormatIdolSalary(details);
                case MonthlyLedgerDetailConstants.KindStaffSalary: return FormatStaffSalary(details);
                case MonthlyLedgerDetailConstants.KindTheaterAttendance: return FormatTheaterAttendance(details);
                case MonthlyLedgerDetailConstants.KindTheaterStreaming: return FormatTheaterStreaming(details);
                case MonthlyLedgerDetailConstants.KindCafeDaily: return FormatCafe(details);
                case MonthlyLedgerDetailConstants.KindConcert: return FormatConcert(details);
                default: return MonthlyLedgerText.Detail(transaction.DetailCode, transaction.CategoryCode);
            }
        }

        private static string FormatContract(IMDataCoreMoneyTransactionDetail details)
        {
            List<string> lines = new List<string>();
            lines.Add(JoinFields(
                ContractType(details.ContractTypeCode),
                Field(Custom(MonthlyLedgerDetailConstants.KeyContractor, MonthlyLedgerDetailConstants.FallbackContractor), details.ContractorName),
                Field(Custom(MonthlyLedgerDetailConstants.KeyProduct, MonthlyLedgerDetailConstants.FallbackProduct), details.ProductName),
                Field(Custom(MonthlyLedgerDetailConstants.KeyIdol, MonthlyLedgerDetailConstants.FallbackIdol), details.IdolName)));

            List<string> financialFields = new List<string>
            {
                Field(Vanilla(MonthlyLedgerDetailConstants.VanillaPayment, MonthlyLedgerDetailConstants.FallbackPayment), Money(details.PaymentAmount)),
                Field(Vanilla(MonthlyLedgerDetailConstants.VanillaStamina, MonthlyLedgerDetailConstants.FallbackStamina), Number(details.StaminaCost)),
                Field(Vanilla(MonthlyLedgerDetailConstants.VanillaLiability, MonthlyLedgerDetailConstants.FallbackLiability), Money(details.LiabilityAmount))
            };
            if (details.Multiplier > MonthlyLedgerConstants.ZeroSize)
            {
                string multiplier = string.Format(
                    CultureInfo.CurrentCulture,
                    Custom(MonthlyLedgerDetailConstants.KeyMultiplierValue, MonthlyLedgerDetailConstants.FallbackMultiplierValue),
                    details.Multiplier.ToString(MonthlyLedgerDetailConstants.MultiplierNumberFormat, CultureInfo.CurrentCulture));
                financialFields.Add(Field(Custom(MonthlyLedgerDetailConstants.KeyMultiplier, MonthlyLedgerDetailConstants.FallbackMultiplier), multiplier));
            }

            financialFields.Add(Field(Vanilla(MonthlyLedgerDetailConstants.VanillaNegotiations, MonthlyLedgerDetailConstants.FallbackNegotiations), Number(details.Negotiations)));
            lines.Add(JoinFields(financialFields.ToArray()));
            return JoinLines(lines);
        }

        private static string FormatSingle(IMDataCoreMoneyTransactionDetail details)
        {
            List<string> lines = new List<string>
            {
                JoinFields(details.SingleTitle, Field(Vanilla(MonthlyLedgerDetailConstants.VanillaGroup, MonthlyLedgerDetailConstants.FallbackGroup), details.SingleGroupName)),
                Field(Vanilla(MonthlyLedgerDetailConstants.VanillaIdols, MonthlyLedgerDetailConstants.FallbackIdols), JoinValues(details.ParticipantNames)),
                JoinFields(
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaSingleMarketing, MonthlyLedgerDetailConstants.FallbackMarketing), JoinLocalizedTokens(details.SingleMarketingTokens)),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaSingleGenre, MonthlyLedgerDetailConstants.FallbackGenre), MonthlyLedgerText.LocalizedToken(details.SingleGenreToken)),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaSingleLyrics, MonthlyLedgerDetailConstants.FallbackLyrics), MonthlyLedgerText.LocalizedToken(details.SingleLyricsToken)),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaSingleChoreography, MonthlyLedgerDetailConstants.FallbackChoreography), MonthlyLedgerText.LocalizedToken(details.SingleChoreographyToken))),
                JoinFields(
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaSingleRevenue, MonthlyLedgerDetailConstants.FallbackRevenue), Money(details.GrossRevenue)),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaSingleProductionCost, MonthlyLedgerDetailConstants.FallbackProductionCost), Money(details.ProductionCost)))
            };
            return JoinLines(lines);
        }

        private static string FormatShow(IMDataCoreMoneyTransactionDetail details)
        {
            List<string> lines = new List<string>
            {
                JoinFields(
                    details.ShowTitle,
                    Field(Custom(MonthlyLedgerDetailConstants.KeyMedium, MonthlyLedgerDetailConstants.FallbackMedium), MonthlyLedgerText.LocalizedToken(details.ShowMediumToken)),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaShowGenre, MonthlyLedgerDetailConstants.FallbackGenre), MonthlyLedgerText.LocalizedToken(details.ShowGenreToken)),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaShowEpisode, MonthlyLedgerDetailConstants.FallbackEpisode), Number(details.ShowEpisodeNumber))),
                JoinFields(
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaShowHost, MonthlyLedgerDetailConstants.FallbackHost), MonthlyLedgerText.LocalizedToken(details.ShowHostToken)),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaShowCast, MonthlyLedgerDetailConstants.FallbackCast), JoinValues(details.ParticipantNames))),
                JoinFields(
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaShowAudience, MonthlyLedgerDetailConstants.FallbackAudience), Number(details.ShowAudience)),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaShowRevenue, MonthlyLedgerDetailConstants.FallbackRevenue), Money(details.GrossRevenue)),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaShowWeeklyBudget, MonthlyLedgerDetailConstants.FallbackWeeklyBudget), Money(details.ShowWeeklyBudget)))
            };
            if (details.HasFanAudience)
            {
                lines.Add(JoinFields(
                    Field(Custom(MonthlyLedgerDetailConstants.KeyFanAudience, MonthlyLedgerDetailConstants.FallbackFanAudience), Number(details.ShowFanAudience)),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaShowFatigue, MonthlyLedgerDetailConstants.FallbackFatigue), details.ShowFatigue.ToString(MonthlyLedgerDetailConstants.MultiplierNumberFormat, CultureInfo.CurrentCulture))));
            }

            return JoinLines(lines);
        }

        private static string FormatIdolSalary(IMDataCoreMoneyTransactionDetail details)
        {
            return JoinLines(new List<string>
            {
                details.IdolName,
                JoinFields(
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaSalary, MonthlyLedgerDetailConstants.FallbackSalary), Money(details.SalaryAmount)),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaFame, MonthlyLedgerDetailConstants.FallbackFame), Number(details.IdolFame)),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaScandalPoints, MonthlyLedgerDetailConstants.FallbackScandalPoints), Number(details.IdolScandalPoints)))
            });
        }

        private static string FormatStaffSalary(IMDataCoreMoneyTransactionDetail details)
        {
            List<string> lines = new List<string>
            {
                JoinFields(
                    details.StaffName,
                    string.IsNullOrEmpty(details.StaffRoleCode)
                        ? string.Empty
                        : Field(Custom(MonthlyLedgerDetailConstants.KeyRole, MonthlyLedgerDetailConstants.FallbackRole), StaffRole(details.StaffRoleCode)),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaSalary, MonthlyLedgerDetailConstants.FallbackSalary), Money(details.SalaryAmount)))
            };
            AppendStaffSkills(lines, details.StaffSkills);
            return JoinLines(lines);
        }

        private static string FormatTheaterAttendance(IMDataCoreMoneyTransactionDetail details)
        {
            return JoinLines(new List<string>
            {
                JoinFields(
                    details.TheaterTitle,
                    TheaterPerformanceType(details.TheaterPerformanceType)),
                JoinFields(
                    Field(Custom(MonthlyLedgerDetailConstants.KeyPerformanceType, MonthlyLedgerDetailConstants.FallbackPerformanceType), TheaterPerformanceType(details.TheaterPerformanceType)),
                    Field(Custom(MonthlyLedgerDetailConstants.KeyAudienceType, MonthlyLedgerDetailConstants.FallbackAudienceType), FanType(details.TheaterAudienceType))),
                JoinFields(
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaTicketPrice, MonthlyLedgerDetailConstants.FallbackTicketPrice), Money(details.TheaterTicketPrice)),
                    Field(Custom(MonthlyLedgerDetailConstants.KeyAttendance, MonthlyLedgerDetailConstants.FallbackAttendance), Percentage(details.TheaterAttendance)),
                    Field(Custom(MonthlyLedgerDetailConstants.KeyDailyAmount, MonthlyLedgerDetailConstants.FallbackDailyAmount), Money(details.GrossRevenue)))
            });
        }

        private static string FormatTheaterStreaming(IMDataCoreMoneyTransactionDetail details)
        {
            return JoinLines(new List<string>
            {
                JoinFields(
                    details.TheaterTitle,
                    Vanilla(MonthlyLedgerDetailConstants.VanillaTheaterStreaming, MonthlyLedgerConstants.FallbackTheater)),
                JoinFields(
                    Field(Custom(MonthlyLedgerDetailConstants.KeyMonthlyAmount, MonthlyLedgerDetailConstants.FallbackMonthlyAmount), Money(details.GrossRevenue)),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaTheaterSubscriptionPrice, MonthlyLedgerDetailConstants.FallbackSubscriptionPrice), Money(details.TheaterSubscriptionPrice))),
                JoinFields(
                    Field(Custom(MonthlyLedgerDetailConstants.KeySubscriberChange, MonthlyLedgerDetailConstants.FallbackSubscriberChange), SignedNumber(details.TheaterSubscriberDelta)),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaTheaterSubscribers, MonthlyLedgerDetailConstants.FallbackSubscribers), Number(details.TheaterSubscriberTotal)))
            });
        }

        private static string FormatCafe(IMDataCoreMoneyTransactionDetail details)
        {
            string staff = details.CafeStaffNames != null && details.CafeStaffNames.Count > MonthlyLedgerConstants.ZeroIndex
                ? JoinValues(details.CafeStaffNames)
                : Custom(MonthlyLedgerDetailConstants.KeyUnstaffed, MonthlyLedgerDetailConstants.FallbackUnstaffed);
            return JoinLines(new List<string>
            {
                JoinFields(
                    details.CafeTitle,
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaCafeDishOfDay, MonthlyLedgerDetailConstants.FallbackDishOfDay), details.CafeDishTitle)),
                Field(Vanilla(MonthlyLedgerDetailConstants.VanillaStaff, MonthlyLedgerConstants.FallbackStaff), staff),
                JoinFields(
                    Field(Custom(MonthlyLedgerDetailConstants.KeyProfitLoss, MonthlyLedgerDetailConstants.FallbackProfitLoss), Money(details.GrossRevenue)),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaCafeNewFans, MonthlyLedgerDetailConstants.FallbackNewFans), SignedNumber(details.CafeNewFans)),
                    Field(Custom(MonthlyLedgerDetailConstants.KeyAppeal, MonthlyLedgerDetailConstants.FallbackAppeal), FanType(details.CafeAppealType)))
            });
        }

        private static string FormatConcert(IMDataCoreMoneyTransactionDetail details)
        {
            List<string> lines = new List<string>
            {
                JoinFields(
                    details.ConcertTitle,
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaConcertVenue, MonthlyLedgerDetailConstants.FallbackVenue), ConcertVenue(details.ConcertVenue))),
                JoinFields(
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaConcertProductionCost, MonthlyLedgerDetailConstants.FallbackProductionCost), Money(details.ProductionCost)),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaTicketPrice, MonthlyLedgerDetailConstants.FallbackTicketPrice), Money(details.ConcertTicketPrice))),
                JoinFields(
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaConcertProjectedHype, MonthlyLedgerDetailConstants.FallbackProjectedHype), Percentage(details.ConcertProjectedHype)),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaConcertProjectedAttendance, MonthlyLedgerDetailConstants.FallbackProjectedAttendance), Number(details.ConcertProjectedAttendance)))
            };

            if (details.ConcertFinished)
            {
                lines.Add(JoinFields(
                    Custom(MonthlyLedgerDetailConstants.KeyFinished, MonthlyLedgerDetailConstants.FallbackFinished),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaConcertHype, MonthlyLedgerDetailConstants.FallbackHype), Percentage(details.ConcertFinishedHype)),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaConcertRevenue, MonthlyLedgerDetailConstants.FallbackRevenue), Money(details.ConcertFinishedRevenue)),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaConcertProfit, MonthlyLedgerDetailConstants.FallbackProfitLoss), Money(details.ConcertFinishedProfit))));
                lines.Add(JoinFields(
                    Field(Custom(MonthlyLedgerDetailConstants.KeyAccidents, MonthlyLedgerDetailConstants.FallbackAccidents), Number(details.ConcertAccidentCount)),
                    Field(Custom(MonthlyLedgerDetailConstants.KeyAccidentSuccesses, MonthlyLedgerDetailConstants.FallbackAccidentSuccesses), Number(details.ConcertAccidentSuccesses)),
                    Field(Custom(MonthlyLedgerDetailConstants.KeyAccidentFailures, MonthlyLedgerDetailConstants.FallbackAccidentFailures), Number(details.ConcertAccidentFailures)),
                    Field(Custom(MonthlyLedgerDetailConstants.KeyAccidentCriticalFailures, MonthlyLedgerDetailConstants.FallbackAccidentCriticalFailures), Number(details.ConcertAccidentCriticalFailures))));
            }

            lines.Add(Vanilla(MonthlyLedgerDetailConstants.VanillaConcertSetlist, MonthlyLedgerDetailConstants.FallbackSetlist));
            AppendConcertSetlist(lines, details.ConcertSetlist);
            return JoinLines(lines);
        }

        private static void AppendConcertSetlist(
            List<string> lines,
            List<IMDataCoreMoneyTransactionConcertSetlistItem> setlist)
        {
            if (setlist == null)
            {
                return;
            }

            int songNumber = MonthlyLedgerConstants.ZeroIndex;
            for (int itemIndex = MonthlyLedgerConstants.ZeroIndex; itemIndex < setlist.Count; itemIndex++)
            {
                IMDataCoreMoneyTransactionConcertSetlistItem item = setlist[itemIndex];
                if (item == null)
                {
                    continue;
                }

                if (item.IsTalk)
                {
                    lines.Add(JoinFields(
                        item.Title,
                        Field(Vanilla(MonthlyLedgerDetailConstants.VanillaIdols, MonthlyLedgerDetailConstants.FallbackIdols), JoinValues(item.IdolNames))));
                }
                else
                {
                    songNumber++;
                    lines.Add(JoinFields(
                        Custom(MonthlyLedgerDetailConstants.KeyTrack, MonthlyLedgerDetailConstants.FallbackTrack)
                            + MonthlyLedgerDetailConstants.FieldSeparator + Number(songNumber),
                        item.Title,
                        Field(Vanilla(MonthlyLedgerDetailConstants.VanillaConcertCenter, MonthlyLedgerDetailConstants.FallbackIdol), item.CenterName)));
                }
            }
        }

        private static void AppendStaffSkills(
            List<string> lines,
            List<IMDataCoreMoneyTransactionStaffSkill> skills)
        {
            if (skills == null || skills.Count == MonthlyLedgerConstants.ZeroIndex)
            {
                return;
            }

            lines.Add(Custom(MonthlyLedgerDetailConstants.KeySkills, MonthlyLedgerDetailConstants.FallbackSkills));
            for (int index = MonthlyLedgerConstants.ZeroIndex; index < skills.Count; index++)
            {
                IMDataCoreMoneyTransactionStaffSkill skill = skills[index];
                if (skill == null)
                {
                    continue;
                }

                lines.Add(JoinFields(
                    SkillName(skill.Code),
                    Field(Vanilla(MonthlyLedgerDetailConstants.VanillaLevel, MonthlyLedgerDetailConstants.FallbackLevel), Number(skill.Level)),
                    Field(
                        Custom(MonthlyLedgerDetailConstants.KeyProgress, MonthlyLedgerDetailConstants.FallbackProgress),
                        (skill.Progress * MonthlyLedgerDetailConstants.PercentageMultiplier).ToString(
                            MonthlyLedgerDetailConstants.ProgressNumberFormat,
                            CultureInfo.CurrentCulture) + MonthlyLedgerDetailConstants.PercentageSuffix)));
            }
        }

        private static string ContractType(string code)
        {
            switch (code)
            {
                case MonthlyLedgerDetailConstants.ContractPhotoshoot: return Vanilla(MonthlyLedgerDetailConstants.VanillaContractPhotoshoot, MonthlyLedgerDetailConstants.FallbackPhotoshoot);
                case MonthlyLedgerDetailConstants.ContractAdvertisement: return Vanilla(MonthlyLedgerDetailConstants.VanillaContractAdvertisement, MonthlyLedgerDetailConstants.FallbackAdvertisement);
                case MonthlyLedgerDetailConstants.ContractTelevisionDrama: return Vanilla(MonthlyLedgerDetailConstants.VanillaContractTelevisionDrama, MonthlyLedgerDetailConstants.FallbackTelevisionDrama);
                case MonthlyLedgerDetailConstants.ContractVariety: return Vanilla(MonthlyLedgerDetailConstants.VanillaContractVariety, MonthlyLedgerDetailConstants.FallbackVariety);
                default: return MonthlyLedgerText.Detail(MonthlyLedgerConstants.DetailBusinessContracts, MonthlyLedgerConstants.CategoryContracts);
            }
        }

        private static string SkillName(string code)
        {
            switch (code)
            {
                case MonthlyLedgerDetailConstants.SkillSpeed: return Vanilla(MonthlyLedgerDetailConstants.VanillaSkillSpeed, MonthlyLedgerDetailConstants.FallbackSpeed);
                case MonthlyLedgerDetailConstants.SkillCoaching: return Vanilla(MonthlyLedgerDetailConstants.VanillaSkillCoaching, MonthlyLedgerDetailConstants.FallbackCoaching);
                case MonthlyLedgerDetailConstants.SkillProduction: return Vanilla(MonthlyLedgerDetailConstants.VanillaSkillProduction, MonthlyLedgerDetailConstants.FallbackProduction);
                case MonthlyLedgerDetailConstants.SkillDeals: return Vanilla(MonthlyLedgerDetailConstants.VanillaSkillDeals, MonthlyLedgerDetailConstants.FallbackDeals);
                case MonthlyLedgerDetailConstants.SkillPhysicalHealth: return Vanilla(MonthlyLedgerDetailConstants.VanillaSkillPhysicalHealth, MonthlyLedgerDetailConstants.FallbackPhysicalHealth);
                case MonthlyLedgerDetailConstants.SkillMentalHealth: return Vanilla(MonthlyLedgerDetailConstants.VanillaSkillMentalHealth, MonthlyLedgerDetailConstants.FallbackMentalHealth);
                case MonthlyLedgerDetailConstants.SkillInfluence: return Vanilla(MonthlyLedgerDetailConstants.VanillaSkillInfluence, MonthlyLedgerDetailConstants.FallbackInfluence);
                case MonthlyLedgerDetailConstants.SkillCutePretty:
                    return Vanilla(MonthlyLedgerDetailConstants.VanillaCute, MonthlyLedgerDetailConstants.FallbackCute)
                        + MonthlyLedgerDetailConstants.CombinedSkillSeparator
                        + Vanilla(MonthlyLedgerDetailConstants.VanillaPretty, MonthlyLedgerDetailConstants.FallbackPretty);
                case MonthlyLedgerDetailConstants.SkillCoolSexy:
                    return Vanilla(MonthlyLedgerDetailConstants.VanillaCool, MonthlyLedgerDetailConstants.FallbackCool)
                        + MonthlyLedgerDetailConstants.CombinedSkillSeparator
                        + Vanilla(MonthlyLedgerDetailConstants.VanillaSexy, MonthlyLedgerDetailConstants.FallbackSexy);
                default: return code ?? string.Empty;
            }
        }

        private static string StaffRole(string code)
        {
            switch (code)
            {
                case MonthlyLedgerDetailConstants.StaffRoleDanceCoach: return Vanilla(MonthlyLedgerDetailConstants.VanillaRoleDanceCoach, MonthlyLedgerDetailConstants.FallbackDanceCoach);
                case MonthlyLedgerDetailConstants.StaffRoleChoreographer: return Vanilla(MonthlyLedgerDetailConstants.VanillaRoleChoreographer, MonthlyLedgerDetailConstants.FallbackChoreographer);
                case MonthlyLedgerDetailConstants.StaffRoleVoiceCoach: return Vanilla(MonthlyLedgerDetailConstants.VanillaRoleVoiceCoach, MonthlyLedgerDetailConstants.FallbackVoiceCoach);
                case MonthlyLedgerDetailConstants.StaffRoleMusicProducer: return Vanilla(MonthlyLedgerDetailConstants.VanillaRoleMusicProducer, MonthlyLedgerDetailConstants.FallbackMusicProducer);
                case MonthlyLedgerDetailConstants.StaffRoleProductionManager: return Vanilla(MonthlyLedgerDetailConstants.VanillaRoleProductionManager, MonthlyLedgerDetailConstants.FallbackProductionManager);
                case MonthlyLedgerDetailConstants.StaffRoleSalesManager: return Vanilla(MonthlyLedgerDetailConstants.VanillaRoleSalesManager, MonthlyLedgerDetailConstants.FallbackSalesManager);
                case MonthlyLedgerDetailConstants.StaffRolePhysician: return Vanilla(MonthlyLedgerDetailConstants.VanillaRolePhysician, MonthlyLedgerDetailConstants.FallbackPhysician);
                case MonthlyLedgerDetailConstants.StaffRolePsychiatrist: return Vanilla(MonthlyLedgerDetailConstants.VanillaRolePsychiatrist, MonthlyLedgerDetailConstants.FallbackPsychiatrist);
                case MonthlyLedgerDetailConstants.StaffRoleStylistCutePretty:
                case MonthlyLedgerDetailConstants.StaffRoleStylistCoolSexy:
                    return Vanilla(MonthlyLedgerDetailConstants.VanillaRoleStylist, MonthlyLedgerDetailConstants.FallbackStylist);
                case MonthlyLedgerDetailConstants.StaffRolePlayer:
                case MonthlyLedgerDetailConstants.StaffRolePlayerFemale:
                    return Vanilla(MonthlyLedgerDetailConstants.VanillaRoleProducer, MonthlyLedgerDetailConstants.FallbackProducer);
                case MonthlyLedgerDetailConstants.StaffRoleAssistantManagerPrimary:
                    return AssistantManagerRole(MonthlyLedgerDetailConstants.AssistantManagerPrimaryStaffTypeValue);
                case MonthlyLedgerDetailConstants.StaffRoleAssistantManagerSecondary:
                    return AssistantManagerRole(MonthlyLedgerDetailConstants.AssistantManagerSecondaryStaffTypeValue);
                default: return code ?? string.Empty;
            }
        }

        private static string AssistantManagerRole(int staffTypeValue)
        {
            if (Harmony.HasAnyPatches(MonthlyLedgerDetailConstants.AssistantManagerHarmonyId))
            {
                string patchedTitle = staff.GetJobTitleString((staff._type)staffTypeValue);
                if (!string.IsNullOrEmpty(patchedTitle))
                {
                    return patchedTitle;
                }
            }

            return Custom(
                MonthlyLedgerDetailConstants.KeyAssistantManagerRole,
                MonthlyLedgerDetailConstants.FallbackAssistantManager);
        }

        private static string TheaterPerformanceType(string code)
        {
            switch (code)
            {
                case MonthlyLedgerDetailConstants.TheaterPerformance:
                    return Vanilla(MonthlyLedgerDetailConstants.VanillaTheaterPerformance, MonthlyLedgerDetailConstants.FallbackPerformanceType);
                case MonthlyLedgerDetailConstants.TheaterManzai:
                    return Vanilla(MonthlyLedgerDetailConstants.VanillaTheaterManzai, MonthlyLedgerDetailConstants.FallbackManzai);
                case MonthlyLedgerDetailConstants.TheaterDayOff:
                    return Vanilla(MonthlyLedgerDetailConstants.VanillaTheaterDayOff, MonthlyLedgerDetailConstants.FallbackDayOff);
                case MonthlyLedgerDetailConstants.TheaterAuto:
                    return Vanilla(MonthlyLedgerDetailConstants.VanillaTheaterAuto, MonthlyLedgerDetailConstants.FallbackAuto);
                default:
                    return code ?? string.Empty;
            }
        }

        private static string FanType(string code)
        {
            switch (code)
            {
                case MonthlyLedgerDetailConstants.TheaterEveryone: return Vanilla(MonthlyLedgerDetailConstants.VanillaTheaterEveryone, MonthlyLedgerDetailConstants.FallbackEveryone);
                case MonthlyLedgerDetailConstants.FanMale: return Vanilla(MonthlyLedgerDetailConstants.VanillaFanMale, MonthlyLedgerDetailConstants.FallbackMale);
                case MonthlyLedgerDetailConstants.FanFemale: return Vanilla(MonthlyLedgerDetailConstants.VanillaFanFemale, MonthlyLedgerDetailConstants.FallbackFemale);
                case MonthlyLedgerDetailConstants.FanCasual: return Vanilla(MonthlyLedgerDetailConstants.VanillaFanCasual, MonthlyLedgerDetailConstants.FallbackCasual);
                case MonthlyLedgerDetailConstants.FanHardcore: return Vanilla(MonthlyLedgerDetailConstants.VanillaFanHardcore, MonthlyLedgerDetailConstants.FallbackHardcore);
                case MonthlyLedgerDetailConstants.FanTeen: return Vanilla(MonthlyLedgerDetailConstants.VanillaFanTeen, MonthlyLedgerDetailConstants.FallbackTeen);
                case MonthlyLedgerDetailConstants.FanYoungAdult: return Vanilla(MonthlyLedgerDetailConstants.VanillaFanYoungAdult, MonthlyLedgerDetailConstants.FallbackYoungAdult);
                case MonthlyLedgerDetailConstants.FanAdult: return Vanilla(MonthlyLedgerDetailConstants.VanillaFanAdult, MonthlyLedgerDetailConstants.FallbackAdult);
                default: return code ?? string.Empty;
            }
        }

        private static string ConcertVenue(string code)
        {
            switch (code)
            {
                case MonthlyLedgerDetailConstants.VenueClub: return Vanilla(MonthlyLedgerDetailConstants.VanillaVenueClub, MonthlyLedgerDetailConstants.FallbackClub);
                case MonthlyLedgerDetailConstants.VenueConcertHall: return Vanilla(MonthlyLedgerDetailConstants.VanillaVenueConcertHall, MonthlyLedgerDetailConstants.FallbackConcertHall);
                case MonthlyLedgerDetailConstants.VenueOpenAirStage: return Vanilla(MonthlyLedgerDetailConstants.VanillaVenueOpenAir, MonthlyLedgerDetailConstants.FallbackOpenAir);
                case MonthlyLedgerDetailConstants.VenueStadium: return Vanilla(MonthlyLedgerDetailConstants.VanillaVenueStadium, MonthlyLedgerDetailConstants.FallbackStadium);
                case MonthlyLedgerDetailConstants.VenueTokyoColiseum: return Vanilla(MonthlyLedgerDetailConstants.VanillaVenueColiseum, MonthlyLedgerDetailConstants.FallbackColiseum);
                default: return code ?? string.Empty;
            }
        }

        private static string JoinLocalizedTokens(List<string> tokens)
        {
            if (tokens == null)
            {
                return string.Empty;
            }

            List<string> localized = new List<string>();
            for (int index = MonthlyLedgerConstants.ZeroIndex; index < tokens.Count; index++)
            {
                localized.Add(MonthlyLedgerText.LocalizedToken(tokens[index]));
            }

            return JoinValues(localized);
        }

        private static string Field(string label, string value)
        {
            return (label ?? string.Empty) + MonthlyLedgerDetailConstants.FieldSeparator + (value ?? string.Empty);
        }

        private static string JoinFields(params string[] fields)
        {
            List<string> present = new List<string>();
            for (int index = MonthlyLedgerConstants.ZeroIndex; index < fields.Length; index++)
            {
                if (!string.IsNullOrEmpty(fields[index]))
                {
                    present.Add(fields[index]);
                }
            }

            return string.Join(MonthlyLedgerDetailConstants.ItemSeparator, present.ToArray());
        }

        private static string JoinLines(List<string> lines)
        {
            return string.Join(MonthlyLedgerDetailConstants.LineSeparator, lines.ToArray());
        }

        private static string JoinValues(List<string> values)
        {
            return values == null ? string.Empty : string.Join(MonthlyLedgerDetailConstants.ListSeparator, values.ToArray());
        }

        private static string Money(long amount)
        {
            return ExtensionMethods.formatMoney(amount, false, false, false);
        }

        private static string Number(long value)
        {
            return ExtensionMethods.formatNumber(value, false, false);
        }

        private static string SignedNumber(long value)
        {
            return value > MonthlyLedgerConstants.ZeroMoney
                ? MonthlyLedgerDetailConstants.SignedPositivePrefix + Number(value)
                : Number(value);
        }

        private static string Percentage(long value)
        {
            return Number(value) + MonthlyLedgerDetailConstants.PercentageSuffix;
        }

        private static string Vanilla(string vanillaKey, string fallback)
        {
            return MonthlyLedgerText.Label(vanillaKey, string.Empty, fallback);
        }

        private static string Custom(string key, string fallback)
        {
            return ModLocalization.Get(key, fallback);
        }
    }
}
