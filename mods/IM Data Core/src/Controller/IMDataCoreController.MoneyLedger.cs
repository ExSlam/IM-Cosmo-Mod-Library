using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace IMDataCore
{
    internal static class MoneyLedgerConstants
    {
        internal const string EventTypeTransaction = CoreConstants.EventTypeMoneyTransaction;
        internal const string EventTypeCoverageStarted = CoreConstants.EventTypeMoneyLedgerCoverageStarted;
        internal const string EntityKind = "money_ledger";
        internal const string EntityIdTransaction = "transaction";
        internal const string EntityIdCoverage = "coverage";
        internal const string SourcePatch = "patch.resources._Add.Postfix";
        internal const string HarmonyTargetMethodName = "_Add";
        internal const string GuidFormat = "N";
        internal const string UnknownSource = "unknown";
        internal const string AssemblyCSharpName = "Assembly-CSharp";
        internal const string HarmonyAssemblyPrefix = "0Harmony";
        internal const string DataCoreAssemblyName = "com.cosmo.imdatacore";
        internal const string MethodOnNewWeek = "OnNewWeek";
        internal const string MethodOnNewDay = "OnNewDay";
        internal const string MethodAdd = "Add";
        internal const string MethodAddMoney = "AddMoney";
        internal const string SectionIncome = "income";
        internal const string SectionExpense = "expense";
        internal const string DetailKindContract = "contract";
        internal const string DetailKindSingle = "single";
        internal const string DetailKindShow = "show";
        internal const string DetailKindIdolSalary = "idol_salary";
        internal const string DetailKindStaffSalary = "staff_salary";

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
        internal const string CategoryLoans = "loans";
        internal const string CategoryEvents = "events";
        internal const string CategoryStory = "story";
        internal const string CategoryIdolSalaries = "idol_salaries";
        internal const string CategoryStaffSalaries = "staff_salaries";
        internal const string CategoryRent = "rent";
        internal const string CategoryExternal = "external_adjustments";
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
        internal const string DetailAgencyRoom = "agency_room";
        internal const string DetailAudition = "audition";
        internal const string DetailResearch = "research";
        internal const string DetailStaffing = "staffing";
        internal const string DetailLoan = "loan";
        internal const string DetailEvent = "event";
        internal const string DetailStory = "story";
        internal const string DetailIdolSalaries = "idol_salaries";
        internal const string DetailStaffSalaries = "staff_salaries";
        internal const string DetailRent = "rent";
        internal const string DetailLoanPayment = "loan_payment";
        internal const string DetailWeeklyRemainder = "weekly_expense_remainder";
        internal const string DetailExternal = "external_adjustment";
        internal const string DetailOther = "other";

        internal const int MinimumReadCount = 1;
        internal const int MaximumReadCount = 10000;
        internal const int StackFrameStartIndex = 0;
        internal const int CollectionStartIndex = 0;
        internal const int SubstringMatchMinimumIndex = 0;
        internal const long ZeroMoney = 0L;
        internal const int InvalidResourceType = -1;
        internal const string MessageInvalidDateRange = "Money transaction date range must have an end after its start.";
        internal const string MessageInvalidReadCount = "Money transaction read count must be positive.";
        internal const string MessageCoverageUnavailable = "Money ledger coverage has not started for this save.";
    }

    [Serializable]
    internal sealed class MoneyLedgerTransactionPayload
    {
        public long amount;
        public long balance_before;
        public long balance_after;
        public string category_code = string.Empty;
        public string detail_code = string.Empty;
        public string section_code = string.Empty;
        public string detail_json = string.Empty;
        public string transaction_group = string.Empty;
        public string source_assembly = string.Empty;
        public string source_type = string.Empty;
        public string source_method = string.Empty;
    }

    [Serializable]
    internal sealed class MoneyLedgerStaffSkillPayload
    {
        public string code = string.Empty;
        public int level;
        public float progress;
    }

    [Serializable]
    internal sealed class MoneyLedgerDetailPayload
    {
        public string kind = string.Empty;
        public string contract_type_code = string.Empty;
        public string contractor_name = string.Empty;
        public string product_name = string.Empty;
        public long payment_amount;
        public int stamina_cost;
        public long liability_amount;
        public int idol_id = CoreConstants.InvalidIdValue;
        public string idol_name = string.Empty;
        public float multiplier;
        public int negotiations;
        public string single_title = string.Empty;
        public string single_group_name = string.Empty;
        public string single_genre_token = string.Empty;
        public string single_lyrics_token = string.Empty;
        public string single_choreography_token = string.Empty;
        public List<string> single_marketing_tokens = new List<string>();
        public List<string> participant_names = new List<string>();
        public long gross_revenue;
        public long production_cost;
        public string show_title = string.Empty;
        public string show_medium_token = string.Empty;
        public string show_genre_token = string.Empty;
        public string show_host_token = string.Empty;
        public int show_episode_number;
        public long show_audience;
        public bool has_fan_audience;
        public long show_fan_audience;
        public float show_fatigue;
        public long show_weekly_budget;
        public string staff_name = string.Empty;
        public long salary_amount;
        public int idol_fame;
        public int idol_scandal_points;
        public List<MoneyLedgerStaffSkillPayload> staff_skills = new List<MoneyLedgerStaffSkillPayload>();
    }

    public sealed class IMDataCoreMoneyTransactionStaffSkill
    {
        public string Code { get; internal set; }
        public int Level { get; internal set; }
        public float Progress { get; internal set; }
    }

    public sealed class IMDataCoreMoneyTransactionDetail
    {
        public string Kind { get; internal set; }
        public string ContractTypeCode { get; internal set; }
        public string ContractorName { get; internal set; }
        public string ProductName { get; internal set; }
        public long PaymentAmount { get; internal set; }
        public int StaminaCost { get; internal set; }
        public long LiabilityAmount { get; internal set; }
        public int IdolId { get; internal set; }
        public string IdolName { get; internal set; }
        public float Multiplier { get; internal set; }
        public int Negotiations { get; internal set; }
        public string SingleTitle { get; internal set; }
        public string SingleGroupName { get; internal set; }
        public string SingleGenreToken { get; internal set; }
        public string SingleLyricsToken { get; internal set; }
        public string SingleChoreographyToken { get; internal set; }
        public List<string> SingleMarketingTokens { get; internal set; }
        public List<string> ParticipantNames { get; internal set; }
        public long GrossRevenue { get; internal set; }
        public long ProductionCost { get; internal set; }
        public string ShowTitle { get; internal set; }
        public string ShowMediumToken { get; internal set; }
        public string ShowGenreToken { get; internal set; }
        public string ShowHostToken { get; internal set; }
        public int ShowEpisodeNumber { get; internal set; }
        public long ShowAudience { get; internal set; }
        public bool HasFanAudience { get; internal set; }
        public long ShowFanAudience { get; internal set; }
        public float ShowFatigue { get; internal set; }
        public long ShowWeeklyBudget { get; internal set; }
        public string StaffName { get; internal set; }
        public long SalaryAmount { get; internal set; }
        public int IdolFame { get; internal set; }
        public int IdolScandalPoints { get; internal set; }
        public List<IMDataCoreMoneyTransactionStaffSkill> StaffSkills { get; internal set; }
    }

    public sealed class IMDataCoreMoneyTransaction
    {
        public long EventId { get; internal set; }
        public int GameDateKey { get; internal set; }
        public string GameDateTime { get; internal set; }
        public long Amount { get; internal set; }
        public long BalanceBefore { get; internal set; }
        public long BalanceAfter { get; internal set; }
        public string CategoryCode { get; internal set; }
        public string DetailCode { get; internal set; }
        public string SectionCode { get; internal set; }
        public IMDataCoreMoneyTransactionDetail Details { get; internal set; }
        public string TransactionGroup { get; internal set; }
        public string SourceAssembly { get; internal set; }
        public string SourceType { get; internal set; }
        public string SourceMethod { get; internal set; }
    }

    internal sealed class MoneyMutationSnapshot
    {
        internal bool IsMoney;
        internal long BalanceBefore;
        internal DateTime GameDate;
        internal string CategoryCode = MoneyLedgerConstants.CategoryOther;
        internal string DetailCode = MoneyLedgerConstants.DetailOther;
        internal string SourceAssembly = MoneyLedgerConstants.UnknownSource;
        internal string SourceType = MoneyLedgerConstants.UnknownSource;
        internal string SourceMethod = MoneyLedgerConstants.UnknownSource;
        internal bool IsWeeklyExpense;
        internal bool IsDailyContractIncome;
        internal bool AdjustLastAllocationForReconciliation;
        internal readonly List<MoneyLedgerAllocationSnapshot> Allocations = new List<MoneyLedgerAllocationSnapshot>();
        internal MoneyLedgerDetailPayload Details;
    }

    internal sealed class MoneyLedgerAllocationSnapshot
    {
        internal long Amount;
        internal string CategoryCode = MoneyLedgerConstants.CategoryOther;
        internal string DetailCode = MoneyLedgerConstants.DetailOther;
        internal string SectionCode = string.Empty;
        internal bool IncludeWhenZero;
        internal MoneyLedgerDetailPayload Details;
    }

    internal static class MoneyLedgerPayloadUtility
    {
        internal static bool TryParse(string payloadJson, out MoneyLedgerTransactionPayload payload)
        {
            payload = null;
            if (string.IsNullOrEmpty(payloadJson))
            {
                return false;
            }

            try
            {
                payload = JsonUtility.FromJson<MoneyLedgerTransactionPayload>(payloadJson);
                return payload != null;
            }
            catch
            {
                payload = null;
                return false;
            }
        }

        internal static IMDataCoreMoneyTransaction ToPublicModel(IMDataCoreEvent sourceEvent)
        {
            if (sourceEvent == null)
            {
                return null;
            }

            MoneyLedgerTransactionPayload payload;
            if (!TryParse(sourceEvent.PayloadJson, out payload))
            {
                return null;
            }

            return new IMDataCoreMoneyTransaction
            {
                EventId = sourceEvent.EventId,
                GameDateKey = sourceEvent.GameDateKey,
                GameDateTime = sourceEvent.GameDateTime ?? string.Empty,
                Amount = payload.amount,
                BalanceBefore = payload.balance_before,
                BalanceAfter = payload.balance_after,
                CategoryCode = payload.category_code ?? string.Empty,
                DetailCode = payload.detail_code ?? string.Empty,
                SectionCode = payload.section_code ?? string.Empty,
                Details = ParsePublicDetails(payload.detail_json),
                TransactionGroup = payload.transaction_group ?? string.Empty,
                SourceAssembly = payload.source_assembly ?? string.Empty,
                SourceType = payload.source_type ?? string.Empty,
                SourceMethod = payload.source_method ?? string.Empty
            };
        }

        private static IMDataCoreMoneyTransactionDetail ParsePublicDetails(string detailJson)
        {
            if (string.IsNullOrEmpty(detailJson))
            {
                return null;
            }

            MoneyLedgerDetailPayload payload;
            try
            {
                payload = JsonUtility.FromJson<MoneyLedgerDetailPayload>(detailJson);
            }
            catch
            {
                return null;
            }

            if (payload == null || string.IsNullOrEmpty(payload.kind))
            {
                return null;
            }

            List<IMDataCoreMoneyTransactionStaffSkill> skills = new List<IMDataCoreMoneyTransactionStaffSkill>();
            if (payload.staff_skills != null)
            {
                for (int skillIndex = MoneyLedgerConstants.CollectionStartIndex; skillIndex < payload.staff_skills.Count; skillIndex++)
                {
                    MoneyLedgerStaffSkillPayload skill = payload.staff_skills[skillIndex];
                    if (skill == null)
                    {
                        continue;
                    }

                    skills.Add(new IMDataCoreMoneyTransactionStaffSkill
                    {
                        Code = skill.code ?? string.Empty,
                        Level = skill.level,
                        Progress = skill.progress
                    });
                }
            }

            return new IMDataCoreMoneyTransactionDetail
            {
                Kind = payload.kind ?? string.Empty,
                ContractTypeCode = payload.contract_type_code ?? string.Empty,
                ContractorName = payload.contractor_name ?? string.Empty,
                ProductName = payload.product_name ?? string.Empty,
                PaymentAmount = payload.payment_amount,
                StaminaCost = payload.stamina_cost,
                LiabilityAmount = payload.liability_amount,
                IdolId = payload.idol_id,
                IdolName = payload.idol_name ?? string.Empty,
                Multiplier = payload.multiplier,
                Negotiations = payload.negotiations,
                SingleTitle = payload.single_title ?? string.Empty,
                SingleGroupName = payload.single_group_name ?? string.Empty,
                SingleGenreToken = payload.single_genre_token ?? string.Empty,
                SingleLyricsToken = payload.single_lyrics_token ?? string.Empty,
                SingleChoreographyToken = payload.single_choreography_token ?? string.Empty,
                SingleMarketingTokens = payload.single_marketing_tokens ?? new List<string>(),
                ParticipantNames = payload.participant_names ?? new List<string>(),
                GrossRevenue = payload.gross_revenue,
                ProductionCost = payload.production_cost,
                ShowTitle = payload.show_title ?? string.Empty,
                ShowMediumToken = payload.show_medium_token ?? string.Empty,
                ShowGenreToken = payload.show_genre_token ?? string.Empty,
                ShowHostToken = payload.show_host_token ?? string.Empty,
                ShowEpisodeNumber = payload.show_episode_number,
                ShowAudience = payload.show_audience,
                HasFanAudience = payload.has_fan_audience,
                ShowFanAudience = payload.show_fan_audience,
                ShowFatigue = payload.show_fatigue,
                ShowWeeklyBudget = payload.show_weekly_budget,
                StaffName = payload.staff_name ?? string.Empty,
                SalaryAmount = payload.salary_amount,
                IdolFame = payload.idol_fame,
                IdolScandalPoints = payload.idol_scandal_points,
                StaffSkills = skills
            };
        }
    }

    internal static class MoneyLedgerSourceResolver
    {
        internal static void Resolve(MoneyMutationSnapshot snapshot)
        {
            StackTrace trace = new StackTrace(false);
            StackFrame[] frames = trace.GetFrames();
            if (frames == null)
            {
                return;
            }

            for (int frameIndex = MoneyLedgerConstants.StackFrameStartIndex; frameIndex < frames.Length; frameIndex++)
            {
                MethodBase method = frames[frameIndex].GetMethod();
                Type declaringType = method != null ? method.DeclaringType : null;
                if (method == null || declaringType == null)
                {
                    continue;
                }

                string assemblyName = declaringType.Assembly.GetName().Name ?? string.Empty;
                string typeName = declaringType.Name ?? string.Empty;
                string methodName = method.Name ?? string.Empty;
                if (IsInfrastructureFrame(assemblyName, typeName, methodName))
                {
                    continue;
                }

                snapshot.SourceAssembly = assemblyName;
                snapshot.SourceType = declaringType.FullName ?? typeName;
                snapshot.SourceMethod = methodName;

                if (TryClassifyVanillaFrame(snapshot, declaringType, methodName))
                {
                    return;
                }

                if (!string.Equals(assemblyName, MoneyLedgerConstants.AssemblyCSharpName, StringComparison.Ordinal))
                {
                    snapshot.CategoryCode = MoneyLedgerConstants.CategoryExternal;
                    snapshot.DetailCode = MoneyLedgerConstants.DetailExternal;
                    return;
                }

                snapshot.CategoryCode = MoneyLedgerConstants.CategoryOther;
                snapshot.DetailCode = MoneyLedgerConstants.DetailOther;
                return;
            }
        }

        private static bool IsInfrastructureFrame(string assemblyName, string typeName, string methodName)
        {
            if (string.Equals(assemblyName, MoneyLedgerConstants.DataCoreAssemblyName, StringComparison.Ordinal)
                || assemblyName.StartsWith(MoneyLedgerConstants.HarmonyAssemblyPrefix, StringComparison.Ordinal))
            {
                return true;
            }

            return string.Equals(typeName, nameof(resources), StringComparison.Ordinal)
                && (MethodNameMatches(methodName, MoneyLedgerConstants.HarmonyTargetMethodName)
                    || MethodNameMatches(methodName, MoneyLedgerConstants.MethodAdd)
                    || MethodNameMatches(methodName, MoneyLedgerConstants.MethodAddMoney));
        }

        private static bool TryClassifyVanillaFrame(MoneyMutationSnapshot snapshot, Type declaringType, string methodName)
        {
            if (IsTypeOrNestedIn(declaringType, typeof(resources)))
            {
                if (MethodNameMatches(methodName, MoneyLedgerConstants.MethodOnNewWeek))
                {
                    snapshot.IsWeeklyExpense = true;
                    return true;
                }

                if (MethodNameMatches(methodName, MoneyLedgerConstants.MethodOnNewDay))
                {
                    snapshot.IsDailyContractIncome = true;
                    SetCategory(snapshot, MoneyLedgerConstants.CategoryContracts, MoneyLedgerConstants.DetailBusinessContracts);
                    return true;
                }
            }

            if (IsTypeOrNestedIn(declaringType, typeof(singles))) return SetCategory(snapshot, MoneyLedgerConstants.CategorySingles, MoneyLedgerConstants.DetailSingleRelease);
            if (IsTypeOrNestedIn(declaringType, typeof(Shows))) return SetCategory(snapshot, MoneyLedgerConstants.CategoryShows, MoneyLedgerConstants.DetailShowEpisode);
            if (IsTypeOrNestedIn(declaringType, typeof(Cafes))) return SetCategory(snapshot, MoneyLedgerConstants.CategoryCafes, MoneyLedgerConstants.DetailCafe);
            if (IsTypeOrNestedIn(declaringType, typeof(Theaters))) return SetCategory(snapshot, MoneyLedgerConstants.CategoryTheaters, MoneyLedgerConstants.DetailTheater);
            if (IsTypeOrNestedIn(declaringType, typeof(Concert_Popup)) || IsTypeOrNestedIn(declaringType, typeof(SEvent_Concerts))) return SetCategory(snapshot, MoneyLedgerConstants.CategoryConcerts, MoneyLedgerConstants.DetailConcert);
            if (IsTypeOrNestedIn(declaringType, typeof(SEvent_Tour))) return SetCategory(snapshot, MoneyLedgerConstants.CategoryTours, MoneyLedgerConstants.DetailTour);
            if (IsTypeOrNestedIn(declaringType, typeof(SEvent_SSK))) return SetCategory(snapshot, MoneyLedgerConstants.CategoryElections, MoneyLedgerConstants.DetailElection);
            if (IsTypeOrNestedIn(declaringType, typeof(Activities))) return SetCategory(snapshot, MoneyLedgerConstants.CategoryActivities, methodName.IndexOf(MoneyLedgerConstants.DetailSpa, StringComparison.OrdinalIgnoreCase) >= MoneyLedgerConstants.SubstringMatchMinimumIndex ? MoneyLedgerConstants.DetailSpa : MoneyLedgerConstants.DetailPerformance);
            if (IsTypeOrNestedIn(declaringType, typeof(data_girls))) return SetCategory(snapshot, MoneyLedgerConstants.CategoryActivities, MoneyLedgerConstants.DetailStreamingIncome);
            if (IsTypeOrNestedIn(declaringType, typeof(agency))) return SetCategory(snapshot, MoneyLedgerConstants.CategoryAgency, MoneyLedgerConstants.DetailAgencyRoom);
            if (IsTypeOrNestedIn(declaringType, typeof(Auditions))) return SetCategory(snapshot, MoneyLedgerConstants.CategoryAuditions, MoneyLedgerConstants.DetailAudition);
            if (IsTypeOrNestedIn(declaringType, typeof(Research))) return SetCategory(snapshot, MoneyLedgerConstants.CategoryResearch, MoneyLedgerConstants.DetailResearch);
            if (IsTypeOrNestedIn(declaringType, typeof(staff)) || IsTypeOrNestedIn(declaringType, typeof(Staff_Fire)) || IsTypeOrNestedIn(declaringType, typeof(Staff_Hire_Button))) return SetCategory(snapshot, MoneyLedgerConstants.CategoryStaffing, MoneyLedgerConstants.DetailStaffing);
            if (IsTypeOrNestedIn(declaringType, typeof(loans))) return SetCategory(snapshot, MoneyLedgerConstants.CategoryLoans, MoneyLedgerConstants.DetailLoan);
            if (IsTypeOrNestedIn(declaringType, typeof(business))) return SetCategory(snapshot, MoneyLedgerConstants.CategoryContracts, MoneyLedgerConstants.DetailBusinessContracts);
            if (IsTypeOrNestedIn(declaringType, typeof(Event_Templates))) return SetCategory(snapshot, MoneyLedgerConstants.CategoryEvents, MoneyLedgerConstants.DetailEvent);
            if (IsTypeOrNestedIn(declaringType, typeof(vn_actions)) || IsTypeOrNestedIn(declaringType, typeof(tasks))) return SetCategory(snapshot, MoneyLedgerConstants.CategoryStory, MoneyLedgerConstants.DetailStory);
            if (IsTypeOrNestedIn(declaringType, typeof(Controls))) return SetCategory(snapshot, MoneyLedgerConstants.CategoryExternal, MoneyLedgerConstants.DetailExternal);

            return false;
        }

        private static bool MethodNameMatches(string actualMethodName, string expectedMethodName)
        {
            return string.Equals(actualMethodName, expectedMethodName, StringComparison.Ordinal)
                || (!string.IsNullOrEmpty(actualMethodName)
                    && !string.IsNullOrEmpty(expectedMethodName)
                    && actualMethodName.IndexOf(expectedMethodName, StringComparison.Ordinal) >= MoneyLedgerConstants.SubstringMatchMinimumIndex);
        }

        private static bool IsTypeOrNestedIn(Type candidate, Type expectedOuterType)
        {
            Type current = candidate;
            while (current != null)
            {
                if (current == expectedOuterType)
                {
                    return true;
                }

                current = current.DeclaringType;
            }

            return false;
        }

        private static bool SetCategory(MoneyMutationSnapshot snapshot, string categoryCode, string detailCode)
        {
            snapshot.CategoryCode = categoryCode;
            snapshot.DetailCode = detailCode;
            return true;
        }
    }

    [HarmonyPatch(typeof(resources), MoneyLedgerConstants.HarmonyTargetMethodName)]
    internal static class resources_Add_IMDataCoreMoneyLedger_Patch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(resources __instance, resources.type _type, out MoneyMutationSnapshot __state)
        {
            __state = IMDataCoreController.Instance.CreateMoneyMutationSnapshot(__instance, _type);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(MoneyMutationSnapshot __state)
        {
            IMDataCoreController.Instance.CaptureMoneyMutation(__state);
        }
    }

    internal sealed partial class IMDataCoreController
    {
        internal MoneyMutationSnapshot CreateMoneyMutationSnapshot(resources resourceManager, resources.type resourceType)
        {
            MoneyMutationSnapshot snapshot = new MoneyMutationSnapshot
            {
                IsMoney = resourceType == resources.type.money,
                GameDate = staticVars.dateTime
            };

            if (!snapshot.IsMoney)
            {
                return snapshot;
            }

            snapshot.BalanceBefore = resources.Get(resources.type.money, false);
            MoneyLedgerSourceResolver.Resolve(snapshot);
            MoneyLedgerCaptureDetails.PopulateSnapshot(snapshot, resourceManager);

            return snapshot;
        }

        internal void CaptureMoneyMutation(MoneyMutationSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.IsMoney)
            {
                return;
            }

            long balanceAfter = resources.Get(resources.type.money, false);
            long actualDelta = balanceAfter - snapshot.BalanceBefore;
            if (actualDelta == MoneyLedgerConstants.ZeroMoney
                && snapshot.Allocations.Count == MoneyLedgerConstants.CollectionStartIndex
                && snapshot.Details == null)
            {
                return;
            }

            lock (runtimeLock)
            {
                string initializationError;
                if (!EnsureInitializedLocked(out initializationError))
                {
                    CoreLog.Warn(initializationError);
                    return;
                }

                EnsureMoneyLedgerCoverageMarkerLocked(snapshot.GameDate);
                string transactionGroup = Guid.NewGuid().ToString(MoneyLedgerConstants.GuidFormat);
                if (snapshot.Allocations.Count > MoneyLedgerConstants.CollectionStartIndex)
                {
                    CaptureDetailedAllocationsLocked(snapshot, actualDelta, transactionGroup);
                }
                else
                {
                    EnqueueMoneyTransactionLocked(
                        snapshot.GameDate,
                        actualDelta,
                        snapshot.BalanceBefore,
                        balanceAfter,
                        snapshot.CategoryCode,
                        snapshot.DetailCode,
                        ResolveSectionCode(actualDelta),
                        transactionGroup,
                        snapshot,
                        snapshot.Details);
                }

                FlushAfterCaptureLocked();
            }
        }

        internal bool TryReadMoneyTransactions(
            DateTime startInclusive,
            DateTime endExclusive,
            int maxCount,
            out List<IMDataCoreMoneyTransaction> transactions,
            out bool wasTruncated,
            out string errorMessage)
        {
            transactions = new List<IMDataCoreMoneyTransaction>();
            wasTruncated = false;
            errorMessage = string.Empty;

            if (endExclusive <= startInclusive)
            {
                errorMessage = MoneyLedgerConstants.MessageInvalidDateRange;
                return false;
            }

            if (maxCount < MoneyLedgerConstants.MinimumReadCount)
            {
                errorMessage = MoneyLedgerConstants.MessageInvalidReadCount;
                return false;
            }

            lock (runtimeLock)
            {
                if (!EnsureInitializedLocked(out errorMessage) || !FlushLocked(true, out errorMessage))
                {
                    return false;
                }

                int boundedCount = Math.Min(maxCount, MoneyLedgerConstants.MaximumReadCount);
                return storageEngine.TryReadMoneyTransactions(
                    activeSaveKey,
                    startInclusive,
                    endExclusive,
                    boundedCount,
                    out transactions,
                    out wasTruncated,
                    out errorMessage);
            }
        }

        internal bool TryGetMoneyLedgerCoverageStart(out DateTime coverageStart, out string errorMessage)
        {
            coverageStart = DateTime.MinValue;
            errorMessage = string.Empty;
            lock (runtimeLock)
            {
                if (!EnsureInitializedLocked(out errorMessage) || !FlushLocked(true, out errorMessage))
                {
                    return false;
                }

                if (storageEngine.TryGetMoneyLedgerCoverageStart(activeSaveKey, out coverageStart, out errorMessage))
                {
                    return true;
                }

                if (string.IsNullOrEmpty(errorMessage))
                {
                    errorMessage = MoneyLedgerConstants.MessageCoverageUnavailable;
                }

                return false;
            }
        }

        private void EnsureMoneyLedgerCoverageMarkerLocked(DateTime gameDate)
        {
            DateTime existingCoverage;
            string ignoredError;
            if (storageEngine.TryGetMoneyLedgerCoverageStart(activeSaveKey, out existingCoverage, out ignoredError))
            {
                return;
            }

            for (int eventIndex = CoreConstants.ZeroBasedListStartIndex; eventIndex < bufferedEvents.Count; eventIndex++)
            {
                PendingEvent pending = bufferedEvents[eventIndex];
                if (pending != null && string.Equals(pending.EventType, MoneyLedgerConstants.EventTypeCoverageStarted, StringComparison.Ordinal))
                {
                    return;
                }
            }

            EnqueueEventRecordLocked(
                gameDate,
                CoreConstants.InvalidIdValue,
                MoneyLedgerConstants.EntityKind,
                MoneyLedgerConstants.EntityIdCoverage,
                MoneyLedgerConstants.EventTypeCoverageStarted,
                MoneyLedgerConstants.SourcePatch,
                CoreConstants.EmptyJsonObject);
        }

        private void CaptureDetailedAllocationsLocked(MoneyMutationSnapshot snapshot, long actualDelta, string transactionGroup)
        {
            long runningBalance = snapshot.BalanceBefore;
            long allocatedDelta = MoneyLedgerConstants.ZeroMoney;
            long requestedDelta = MoneyLedgerConstants.ZeroMoney;
            for (int allocationIndex = MoneyLedgerConstants.CollectionStartIndex; allocationIndex < snapshot.Allocations.Count; allocationIndex++)
            {
                requestedDelta += snapshot.Allocations[allocationIndex].Amount;
            }

            long reconciliation = actualDelta - requestedDelta;
            if (snapshot.AdjustLastAllocationForReconciliation
                && reconciliation != MoneyLedgerConstants.ZeroMoney
                && snapshot.Allocations.Count > MoneyLedgerConstants.CollectionStartIndex)
            {
                MoneyLedgerAllocationSnapshot finalAllocation = snapshot.Allocations[snapshot.Allocations.Count - MoneyLedgerConstants.MinimumReadCount];
                finalAllocation.Amount += reconciliation;
            }

            for (int allocationIndex = MoneyLedgerConstants.CollectionStartIndex; allocationIndex < snapshot.Allocations.Count; allocationIndex++)
            {
                MoneyLedgerAllocationSnapshot allocation = snapshot.Allocations[allocationIndex];
                allocatedDelta += EnqueueDetailedAllocationLocked(snapshot, allocation, ref runningBalance, transactionGroup);
            }

            long remainder = actualDelta - allocatedDelta;
            if (remainder != MoneyLedgerConstants.ZeroMoney)
            {
                MoneyLedgerAllocationSnapshot remainderAllocation = new MoneyLedgerAllocationSnapshot
                {
                    Amount = remainder,
                    CategoryCode = snapshot.IsWeeklyExpense ? MoneyLedgerConstants.CategoryOther : snapshot.CategoryCode,
                    DetailCode = snapshot.IsWeeklyExpense ? MoneyLedgerConstants.DetailWeeklyRemainder : MoneyLedgerConstants.DetailOther,
                    SectionCode = ResolveSectionCode(remainder)
                };
                EnqueueDetailedAllocationLocked(snapshot, remainderAllocation, ref runningBalance, transactionGroup);
            }
        }

        private long EnqueueDetailedAllocationLocked(
            MoneyMutationSnapshot snapshot,
            MoneyLedgerAllocationSnapshot allocation,
            ref long runningBalance,
            string transactionGroup)
        {
            if (allocation == null || (allocation.Amount == MoneyLedgerConstants.ZeroMoney && !allocation.IncludeWhenZero))
            {
                return MoneyLedgerConstants.ZeroMoney;
            }

            long balanceBefore = runningBalance;
            runningBalance += allocation.Amount;
            EnqueueMoneyTransactionLocked(
                snapshot.GameDate,
                allocation.Amount,
                balanceBefore,
                runningBalance,
                allocation.CategoryCode,
                allocation.DetailCode,
                string.IsNullOrEmpty(allocation.SectionCode) ? ResolveSectionCode(allocation.Amount) : allocation.SectionCode,
                transactionGroup,
                snapshot,
                allocation.Details);
            return allocation.Amount;
        }

        private void EnqueueMoneyTransactionLocked(
            DateTime gameDate,
            long amount,
            long balanceBefore,
            long balanceAfter,
            string categoryCode,
            string detailCode,
            string sectionCode,
            string transactionGroup,
            MoneyMutationSnapshot snapshot,
            MoneyLedgerDetailPayload details)
        {
            MoneyLedgerTransactionPayload payload = new MoneyLedgerTransactionPayload
            {
                amount = amount,
                balance_before = balanceBefore,
                balance_after = balanceAfter,
                category_code = categoryCode ?? MoneyLedgerConstants.CategoryOther,
                detail_code = detailCode ?? MoneyLedgerConstants.DetailOther,
                section_code = sectionCode ?? ResolveSectionCode(amount),
                detail_json = details != null ? JsonUtility.ToJson(details) : string.Empty,
                transaction_group = transactionGroup ?? string.Empty,
                source_assembly = snapshot.SourceAssembly ?? MoneyLedgerConstants.UnknownSource,
                source_type = snapshot.SourceType ?? MoneyLedgerConstants.UnknownSource,
                source_method = snapshot.SourceMethod ?? MoneyLedgerConstants.UnknownSource
            };

            EnqueueEventRecordLocked(
                gameDate,
                CoreConstants.InvalidIdValue,
                MoneyLedgerConstants.EntityKind,
                MoneyLedgerConstants.EntityIdTransaction,
                MoneyLedgerConstants.EventTypeTransaction,
                MoneyLedgerConstants.SourcePatch,
                CoreJsonUtility.SerializeObjectPayload(payload));
        }

        private static string ResolveSectionCode(long amount)
        {
            return amount >= MoneyLedgerConstants.ZeroMoney
                ? MoneyLedgerConstants.SectionIncome
                : MoneyLedgerConstants.SectionExpense;
        }
    }
}
