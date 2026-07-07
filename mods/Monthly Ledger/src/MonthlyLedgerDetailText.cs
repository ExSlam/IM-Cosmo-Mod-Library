using System;
using System.Collections.Generic;
using System.Globalization;
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
        internal const string VanillaCute = "CUTE";
        internal const string VanillaPretty = "PRETTY";
        internal const string VanillaCool = "COOL";
        internal const string VanillaSexy = "SEXY";

        internal const string KeyContractor = "detail.label.contractor";
        internal const string KeyProduct = "detail.label.product";
        internal const string KeyIdol = "detail.label.idol";
        internal const string KeyMultiplier = "detail.label.multiplier";
        internal const string KeyMedium = "detail.label.medium";
        internal const string KeyFanAudience = "detail.label.fan_audience";
        internal const string KeySkills = "detail.label.skills";
        internal const string KeyMultiplierValue = "detail.format.multiplier";
        internal const string KeySkillValue = "detail.format.skill";

        internal const string FallbackContractor = "Contractor";
        internal const string FallbackProduct = "Product";
        internal const string FallbackIdol = "Idol";
        internal const string FallbackIdols = "Idols";
        internal const string FallbackMultiplier = "Multiplier";
        internal const string FallbackMedium = "Medium";
        internal const string FallbackFanAudience = "Fan audience";
        internal const string FallbackSkills = "Skills";
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
        internal const string FallbackFame = "Fame";
        internal const string FallbackScandalPoints = "Scandal points";
        internal const string FallbackSpeed = "Speed";
        internal const string FallbackCoaching = "Coaching";
        internal const string FallbackProduction = "Production";
        internal const string FallbackDeals = "Deals";
        internal const string FallbackPhysicalHealth = "Physical health";
        internal const string FallbackMentalHealth = "Mental health";
        internal const string FallbackInfluence = "Influence";
        internal const string FallbackCute = "Cute";
        internal const string FallbackPretty = "Pretty";
        internal const string FallbackCool = "Cool";
        internal const string FallbackSexy = "Sexy";
        internal const string FallbackMultiplierValue = "{0}x";
        internal const string FallbackSkillValue = "{0} {1} ({2}%)";

        internal const string FieldSeparator = ": ";
        internal const string ItemSeparator = "  •  ";
        internal const string ListSeparator = ", ";
        internal const string CombinedSkillSeparator = " / ";
        internal const string LineSeparator = "\n";
        internal const string MultiplierNumberFormat = "0.##";
        internal const string ProgressNumberFormat = "0";
        internal const int MinimumStructuredLineCount = 1;
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
            return JoinLines(new List<string>
            {
                JoinFields(details.StaffName, Field(Vanilla(MonthlyLedgerDetailConstants.VanillaSalary, MonthlyLedgerDetailConstants.FallbackSalary), Money(details.SalaryAmount))),
                Field(Custom(MonthlyLedgerDetailConstants.KeySkills, MonthlyLedgerDetailConstants.FallbackSkills), FormatSkills(details.StaffSkills))
            });
        }

        private static string FormatSkills(List<IMDataCoreMoneyTransactionStaffSkill> skills)
        {
            if (skills == null || skills.Count == MonthlyLedgerConstants.ZeroIndex)
            {
                return string.Empty;
            }

            List<string> values = new List<string>();
            for (int index = MonthlyLedgerConstants.ZeroIndex; index < skills.Count; index++)
            {
                IMDataCoreMoneyTransactionStaffSkill skill = skills[index];
                if (skill == null)
                {
                    continue;
                }

                values.Add(string.Format(
                    CultureInfo.CurrentCulture,
                    Custom(MonthlyLedgerDetailConstants.KeySkillValue, MonthlyLedgerDetailConstants.FallbackSkillValue),
                    SkillName(skill.Code),
                    Number(skill.Level),
                    (skill.Progress * MonthlyLedgerDetailConstants.PercentageMultiplier).ToString(MonthlyLedgerDetailConstants.ProgressNumberFormat, CultureInfo.CurrentCulture)));
            }

            return JoinValues(values);
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
