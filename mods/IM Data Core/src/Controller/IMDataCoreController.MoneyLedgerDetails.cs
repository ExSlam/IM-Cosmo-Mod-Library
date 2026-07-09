using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace IMDataCore
{
    internal static class MoneyLedgerDetailConstants
    {
        internal const string MethodAccept = "Accept";
        internal const string MethodAddActiveProposal = "AddActiveProposal";
        internal const string MethodCancelContract = "CancelContract";
        internal const string MethodBreakContracts = "BreakContracts";
        internal const string MethodSingleAddMoney = "AddMoney";
        internal const string MethodShowGetProfit = "GetProfit";
        internal const string MethodShowRelease = "ReleaseShow";
        internal const string MethodShowOnNewDay = "OnNewDay";
        internal const string TheaterIncomeAttendance = "attendance";
        internal const string TheaterIncomeStreaming = "streaming";
        internal const string SourceTypeTheaters = "Theaters";
        internal const string SourceTypeCafes = "Cafes";
        internal const string SourceTypeSingles = "singles";
        internal const string SourceTypeShows = "Shows";
        internal const string SourceTypeBusiness = "business";
        internal const string SourceTypeConcertPopup = "Concert_Popup";
        internal const string SourceTypeConcertSystem = "SEvent_Concerts";
        internal const string SourceMethodTheaterCompleteDay = "CompleteDay";
        internal const string SourceMethodCafeRender = "RenderCafe";
        internal const string SourceMethodConcertStart = "StartConcert";
        internal const string SourceMethodConcertFinish = "FinishConcert";
        internal const string ConcertResultSuccess = "success";
        internal const string ConcertResultCriticalFailure = "critical_failure";
        internal const string FansWatchHarmonyId = "com.tbs.fanswatch";
        internal const string FansWatchAssemblyName = "com.tbs.fanswatch";
        internal const string FansWatchAudiencePatchType = "FansWatchShows.GetAudiencePatch";
        internal const string FansWatchBonusMethod = "GetBonusFans";
        internal const int LastCollectionIndexOffset = 1;
        internal const long MinimumPaidSalary = 1L;
        internal const float DaysPerWeek = 7f;
        internal const float ZeroMultiplier = 0f;
        internal const double ContractMetadataLookbackDays = -120d;
        internal const double ContractMetadataEndDateOffsetDays = 1d;
        internal const int ContractMetadataReadCount = 10000;
        internal const int FirstDayOfMonth = 1;
        internal const float PercentageScale = 100f;
    }

    internal sealed class MoneyLedgerContractRuntimeMetadata
    {
        internal float Multiplier;
        internal int Negotiations;
    }

    internal sealed class MoneyLedgerAmbientCapture
    {
        internal MoneyLedgerDetailPayload Details;
        internal readonly List<MoneyLedgerAllocationSnapshot> Allocations = new List<MoneyLedgerAllocationSnapshot>();
        internal Action PrepareForCapture;
        internal string ExpectedSourceType = string.Empty;
        internal string ExpectedSourceMethod = string.Empty;
    }

    internal sealed class MoneyLedgerConcertOutcomeCounts
    {
        internal int Successes;
        internal int Failures;
        internal int CriticalFailures;
    }

    internal static class MoneyLedgerConcertOutcomeTracker
    {
        private static readonly Dictionary<int, MoneyLedgerConcertOutcomeCounts> CountsByConcertId =
            new Dictionary<int, MoneyLedgerConcertOutcomeCounts>();

        internal static void Record(int concertId, string resultTypeCode)
        {
            if (concertId < CoreConstants.MinimumValidIdolIdentifier)
            {
                return;
            }

            MoneyLedgerConcertOutcomeCounts counts;
            if (!CountsByConcertId.TryGetValue(concertId, out counts))
            {
                counts = new MoneyLedgerConcertOutcomeCounts();
                CountsByConcertId[concertId] = counts;
            }

            if (string.Equals(resultTypeCode, MoneyLedgerDetailConstants.ConcertResultSuccess, StringComparison.Ordinal))
            {
                counts.Successes++;
            }
            else if (string.Equals(resultTypeCode, MoneyLedgerDetailConstants.ConcertResultCriticalFailure, StringComparison.Ordinal))
            {
                counts.CriticalFailures++;
            }
            else
            {
                counts.Failures++;
            }
        }

        internal static MoneyLedgerConcertOutcomeCounts Get(int concertId)
        {
            MoneyLedgerConcertOutcomeCounts counts;
            return CountsByConcertId.TryGetValue(concertId, out counts)
                ? counts
                : new MoneyLedgerConcertOutcomeCounts();
        }

        internal static void Remove(int concertId)
        {
            CountsByConcertId.Remove(concertId);
        }
    }

    internal static class MoneyLedgerAmbientContext
    {
        private static readonly Dictionary<business.active_proposal, MoneyLedgerContractRuntimeMetadata> ContractMetadata =
            new Dictionary<business.active_proposal, MoneyLedgerContractRuntimeMetadata>();
        private static MoneyLedgerAmbientCapture current;

        internal static void Set(MoneyLedgerAmbientCapture capture)
        {
            current = capture;
        }

        internal static MoneyLedgerAmbientCapture Consume()
        {
            MoneyLedgerAmbientCapture capture = current;
            current = null;
            if (capture != null && capture.PrepareForCapture != null)
            {
                capture.PrepareForCapture();
            }

            return capture;
        }

        internal static MoneyLedgerAmbientCapture Consume(string sourceType, string sourceMethod)
        {
            if (current == null)
            {
                return null;
            }

            if ((!string.IsNullOrEmpty(current.ExpectedSourceType)
                    && !string.Equals(current.ExpectedSourceType, sourceType, StringComparison.Ordinal))
                || (!string.IsNullOrEmpty(current.ExpectedSourceMethod)
                    && !SourceMethodMatches(current.ExpectedSourceMethod, sourceMethod)))
            {
                return null;
            }

            return Consume();
        }

        private static bool SourceMethodMatches(string expectedSourceMethod, string actualSourceMethod)
        {
            return string.Equals(expectedSourceMethod, actualSourceMethod, StringComparison.Ordinal)
                || (!string.IsNullOrEmpty(expectedSourceMethod)
                    && !string.IsNullOrEmpty(actualSourceMethod)
                    && actualSourceMethod.IndexOf(expectedSourceMethod, StringComparison.Ordinal)
                        >= CoreConstants.ZeroBasedListStartIndex);
        }

        internal static void Clear()
        {
            current = null;
        }

        internal static void RegisterContract(business.active_proposal activeContract, business._proposal proposal)
        {
            if (activeContract == null || proposal == null)
            {
                return;
            }

            ContractMetadata[activeContract] = new MoneyLedgerContractRuntimeMetadata
            {
                Multiplier = ResolveProposalMultiplier(proposal),
                Negotiations = proposal.negotiation_attempts
            };
        }

        internal static bool TryGetContractMetadata(business.active_proposal activeContract, out MoneyLedgerContractRuntimeMetadata metadata)
        {
            metadata = null;
            return activeContract != null && ContractMetadata.TryGetValue(activeContract, out metadata);
        }

        internal static void RegisterContract(business.active_proposal activeContract, float multiplier, int negotiations)
        {
            if (activeContract == null)
            {
                return;
            }

            ContractMetadata[activeContract] = new MoneyLedgerContractRuntimeMetadata
            {
                Multiplier = multiplier,
                Negotiations = negotiations
            };
        }

        internal static float ResolveProposalMultiplier(business._proposal proposal)
        {
            if (proposal == null || proposal.girl == null)
            {
                return MoneyLedgerDetailConstants.ZeroMultiplier;
            }

            return proposal.GetGirlCoeff(proposal.girl) * proposal.negotiationCoeff;
        }
    }

    internal static class MoneyLedgerCaptureDetails
    {
        private static bool fansWatchLookupCompleted;
        private static MethodInfo fansWatchBonusMethod;

        internal static void PopulateSnapshot(MoneyMutationSnapshot snapshot, resources resourceManager)
        {
            if (snapshot == null)
            {
                return;
            }

            MoneyLedgerAmbientCapture ambient = MoneyLedgerAmbientContext.Consume(
                snapshot.SourceType,
                snapshot.SourceMethod);
            if (ambient != null)
            {
                snapshot.Details = ambient.Details;
                snapshot.Allocations.AddRange(ambient.Allocations);
                return;
            }

            if (snapshot.IsWeeklyExpense && resourceManager != null)
            {
                PopulateWeeklyExpenseAllocations(snapshot, resourceManager);
                return;
            }

            if (snapshot.IsDailyContractIncome)
            {
                PopulateDailyContractAllocations(snapshot);
            }
        }

        internal static MoneyLedgerAmbientCapture BuildProposalCapture(business._proposal proposal)
        {
            return new MoneyLedgerAmbientCapture
            {
                Details = BuildProposalDetail(proposal),
                ExpectedSourceType = MoneyLedgerDetailConstants.SourceTypeBusiness,
                ExpectedSourceMethod = MoneyLedgerDetailConstants.MethodAccept
            };
        }

        internal static MoneyLedgerAmbientCapture BuildActiveContractCapture(business.active_proposal activeContract)
        {
            return new MoneyLedgerAmbientCapture
            {
                Details = BuildActiveContractDetail(activeContract),
                ExpectedSourceType = MoneyLedgerDetailConstants.SourceTypeBusiness,
                ExpectedSourceMethod = MoneyLedgerDetailConstants.MethodCancelContract
            };
        }

        internal static MoneyLedgerAmbientCapture BuildBrokenContractCapture(IEnumerable<business.active_proposal> contracts)
        {
            MoneyLedgerAmbientCapture capture = new MoneyLedgerAmbientCapture
            {
                ExpectedSourceType = MoneyLedgerDetailConstants.SourceTypeBusiness,
                ExpectedSourceMethod = MoneyLedgerDetailConstants.MethodBreakContracts
            };
            if (contracts == null)
            {
                return capture;
            }

            foreach (business.active_proposal activeContract in contracts)
            {
                if (activeContract == null)
                {
                    continue;
                }

                MoneyLedgerDetailPayload details = BuildActiveContractDetail(activeContract);
                capture.Allocations.Add(new MoneyLedgerAllocationSnapshot
                {
                    Amount = -activeContract.Liability,
                    CategoryCode = MoneyLedgerConstants.CategoryContracts,
                    DetailCode = MoneyLedgerConstants.DetailBusinessContracts,
                    SectionCode = MoneyLedgerConstants.SectionExpense,
                    Details = details
                });
            }

            return capture;
        }

        internal static MoneyLedgerAmbientCapture BuildSingleCapture(singles._single single)
        {
            MoneyLedgerAmbientCapture capture = new MoneyLedgerAmbientCapture
            {
                ExpectedSourceType = MoneyLedgerDetailConstants.SourceTypeSingles,
                ExpectedSourceMethod = MoneyLedgerDetailConstants.MethodSingleAddMoney
            };
            if (single == null)
            {
                return capture;
            }

            MoneyLedgerDetailPayload details = BuildSingleDetail(single);
            capture.Allocations.Add(new MoneyLedgerAllocationSnapshot
            {
                Amount = details.gross_revenue,
                CategoryCode = MoneyLedgerConstants.CategorySingles,
                DetailCode = MoneyLedgerConstants.DetailSingleRelease,
                SectionCode = MoneyLedgerConstants.SectionIncome,
                IncludeWhenZero = true,
                Details = details
            });
            capture.Allocations.Add(new MoneyLedgerAllocationSnapshot
            {
                Amount = -details.production_cost,
                CategoryCode = MoneyLedgerConstants.CategorySingles,
                DetailCode = MoneyLedgerConstants.DetailSingleRelease,
                SectionCode = MoneyLedgerConstants.SectionExpense,
                IncludeWhenZero = true,
                Details = details
            });
            return capture;
        }

        internal static MoneyLedgerAmbientCapture BuildShowCapture(Shows._show show)
        {
            MoneyLedgerAmbientCapture capture = new MoneyLedgerAmbientCapture
            {
                ExpectedSourceType = MoneyLedgerDetailConstants.SourceTypeShows
            };
            if (show == null)
            {
                return capture;
            }

            MoneyLedgerDetailPayload details = BuildShowDetail(show);
            capture.Allocations.Add(new MoneyLedgerAllocationSnapshot
            {
                Amount = details.gross_revenue,
                CategoryCode = MoneyLedgerConstants.CategoryShows,
                DetailCode = MoneyLedgerConstants.DetailShowEpisode,
                SectionCode = MoneyLedgerConstants.SectionIncome,
                IncludeWhenZero = true,
                Details = details
            });
            capture.Allocations.Add(new MoneyLedgerAllocationSnapshot
            {
                Amount = -details.show_weekly_budget,
                CategoryCode = MoneyLedgerConstants.CategoryShows,
                DetailCode = MoneyLedgerConstants.DetailShowEpisode,
                SectionCode = MoneyLedgerConstants.SectionExpense,
                IncludeWhenZero = true,
                Details = details
            });
            return capture;
        }

        internal static MoneyLedgerAmbientCapture BuildTheaterCapture()
        {
            MoneyLedgerAmbientCapture capture = new MoneyLedgerAmbientCapture
            {
                ExpectedSourceType = MoneyLedgerDetailConstants.SourceTypeTheaters,
                ExpectedSourceMethod = MoneyLedgerDetailConstants.SourceMethodTheaterCompleteDay
            };
            if (Theaters.Theaters_ == null)
            {
                return capture;
            }

            for (int theaterIndex = CoreConstants.ZeroBasedListStartIndex; theaterIndex < Theaters.Theaters_.Count; theaterIndex++)
            {
                Theaters._theater theater = Theaters.Theaters_[theaterIndex];
                if (theater == null)
                {
                    continue;
                }

                Theaters._theater._schedule performedSchedule = ResolvePerformedTheaterSchedule(theater);
                bool hasAttendanceIncome =
                    theater.Doing_Now == Theaters._theater._schedule._type.performance
                    || theater.Doing_Now == Theaters._theater._schedule._type.manzai;
                long attendanceIncome = hasAttendanceIncome ? theater.GetTicketSales() : MoneyLedgerConstants.ZeroMoney;
                capture.Allocations.Add(new MoneyLedgerAllocationSnapshot
                {
                    Amount = attendanceIncome,
                    CategoryCode = MoneyLedgerConstants.CategoryTheaters,
                    DetailCode = MoneyLedgerConstants.DetailTheater,
                    SectionCode = MoneyLedgerConstants.SectionIncome,
                    IncludeWhenZero = true,
                    Details = new MoneyLedgerDetailPayload
                    {
                        kind = MoneyLedgerConstants.DetailKindTheaterAttendance,
                        theater_id = theater.ID,
                        theater_title = theater.GetTitle() ?? string.Empty,
                        theater_income_type = MoneyLedgerDetailConstants.TheaterIncomeAttendance,
                        theater_ticket_price = theater.Ticket_Price,
                        theater_performance_type = CoreEnumNameMapping.ToTheaterScheduleTypeCode(theater.Doing_Now),
                        theater_audience_type = ResolveTheaterAudienceType(performedSchedule),
                        theater_attendance = hasAttendanceIncome ? theater.GetAttendance() : CoreConstants.ZeroBasedListStartIndex,
                        theater_subscription_price = theater.Subscription_Price,
                        theater_subscriber_total = theater.GetSubscribers(),
                        gross_revenue = attendanceIncome
                    }
                });

                if (staticVars.dateTime.Day == MoneyLedgerDetailConstants.FirstDayOfMonth)
                {
                    long streamingIncome = theater.GetSubRevenue();
                    capture.Allocations.Add(new MoneyLedgerAllocationSnapshot
                    {
                        Amount = streamingIncome,
                        CategoryCode = MoneyLedgerConstants.CategoryTheaters,
                        DetailCode = MoneyLedgerConstants.DetailStreamingIncome,
                        SectionCode = MoneyLedgerConstants.SectionIncome,
                        IncludeWhenZero = true,
                        Details = new MoneyLedgerDetailPayload
                        {
                            kind = MoneyLedgerConstants.DetailKindTheaterStreaming,
                            theater_id = theater.ID,
                            theater_title = theater.GetTitle() ?? string.Empty,
                            theater_income_type = MoneyLedgerDetailConstants.TheaterIncomeStreaming,
                            theater_subscription_price = theater.Subscription_Price,
                            theater_subscriber_total = theater.GetSubscribers(),
                            gross_revenue = streamingIncome
                        }
                    });
                }
            }

            capture.PrepareForCapture = delegate { FinalizeTheaterCapture(capture); };
            return capture;
        }

        internal static MoneyLedgerAmbientCapture BuildCafeCapture(Cafes._cafe cafe)
        {
            MoneyLedgerAmbientCapture capture = new MoneyLedgerAmbientCapture
            {
                ExpectedSourceType = MoneyLedgerDetailConstants.SourceTypeCafes,
                ExpectedSourceMethod = MoneyLedgerDetailConstants.SourceMethodCafeRender
            };
            if (cafe == null)
            {
                return capture;
            }

            Cafes._cafe._dish dish = cafe.GetCurrentDish();
            int amount = cafe.GetMoneyToAdd();
            capture.Allocations.Add(new MoneyLedgerAllocationSnapshot
            {
                Amount = amount,
                CategoryCode = MoneyLedgerConstants.CategoryCafes,
                DetailCode = MoneyLedgerConstants.DetailCafe,
                SectionCode = amount < CoreConstants.ZeroBasedListStartIndex
                    ? MoneyLedgerConstants.SectionExpense
                    : MoneyLedgerConstants.SectionIncome,
                IncludeWhenZero = true,
                Details = new MoneyLedgerDetailPayload
                {
                    kind = MoneyLedgerConstants.DetailKindCafeDaily,
                    cafe_id = cafe.ID,
                    cafe_title = cafe.GetTitle() ?? string.Empty,
                    cafe_dish_title = dish != null ? dish.Title ?? string.Empty : string.Empty,
                    cafe_dish_type = dish != null
                        ? CoreEnumNameMapping.ToCafeDishTypeCode(dish.Type)
                        : CoreConstants.StatusCodeUnknown,
                    cafe_staff_names = ResolveIdolNames(cafe.WorkingGirls),
                    cafe_new_fans = cafe.GetFansToAdd(),
                    cafe_appeal_type = CoreEnumNameMapping.ToFanTypeCode(cafe.GetFanTypeToAdd()),
                    gross_revenue = amount
                }
            });
            return capture;
        }

        internal static MoneyLedgerAmbientCapture BuildConcertCapture(SEvent_Concerts._concert concert, bool finished)
        {
            return new MoneyLedgerAmbientCapture
            {
                Details = BuildConcertDetail(concert, finished),
                ExpectedSourceType = finished
                    ? MoneyLedgerDetailConstants.SourceTypeConcertPopup
                    : MoneyLedgerDetailConstants.SourceTypeConcertSystem,
                ExpectedSourceMethod = finished
                    ? MoneyLedgerDetailConstants.SourceMethodConcertFinish
                    : MoneyLedgerDetailConstants.SourceMethodConcertStart
            };
        }

        internal static void CapturePendingZeroAllocations(string sourceType, string sourceMethod)
        {
            MoneyLedgerAmbientCapture capture = MoneyLedgerAmbientContext.Consume();
            if (capture == null || capture.Allocations.Count == CoreConstants.ZeroBasedListStartIndex)
            {
                return;
            }

            long allocationTotal = MoneyLedgerConstants.ZeroMoney;
            for (int allocationIndex = CoreConstants.ZeroBasedListStartIndex; allocationIndex < capture.Allocations.Count; allocationIndex++)
            {
                allocationTotal += capture.Allocations[allocationIndex].Amount;
            }

            if (allocationTotal != MoneyLedgerConstants.ZeroMoney)
            {
                return;
            }

            MoneyMutationSnapshot snapshot = new MoneyMutationSnapshot
            {
                IsMoney = true,
                BalanceBefore = resources.Money(),
                GameDate = staticVars.dateTime,
                SourceAssembly = MoneyLedgerConstants.AssemblyCSharpName,
                SourceType = sourceType ?? MoneyLedgerConstants.UnknownSource,
                SourceMethod = sourceMethod ?? MoneyLedgerConstants.UnknownSource
            };
            snapshot.Allocations.AddRange(capture.Allocations);
            IMDataCoreController.Instance.CaptureMoneyMutation(snapshot);
        }

        private static string ResolveTheaterAudienceType(Theaters._theater._schedule schedule)
        {
            if (schedule == null)
            {
                return CoreConstants.StatusCodeUnknown;
            }

            return schedule.FanType_Everyone
                ? CoreConstants.TheaterScheduleFanTypeEveryone
                : CoreEnumNameMapping.ToFanTypeCode(schedule.FanType);
        }

        private static Theaters._theater._schedule ResolvePerformedTheaterSchedule(Theaters._theater theater)
        {
            if (theater != null
                && theater.Stats != null
                && theater.Stats.Count > CoreConstants.ZeroBasedListStartIndex)
            {
                Theaters._theater._stat latestStat = theater.Stats[theater.Stats.Count - MoneyLedgerDetailConstants.LastCollectionIndexOffset];
                if (latestStat != null
                    && latestStat.Schedule != null
                    && latestStat.Schedule.Type == theater.Doing_Now)
                {
                    return latestStat.Schedule;
                }
            }

            return theater != null ? theater.GetSchedule() : null;
        }

        private static void FinalizeTheaterCapture(MoneyLedgerAmbientCapture capture)
        {
            if (capture == null)
            {
                return;
            }

            for (int allocationIndex = CoreConstants.ZeroBasedListStartIndex; allocationIndex < capture.Allocations.Count; allocationIndex++)
            {
                MoneyLedgerDetailPayload details = capture.Allocations[allocationIndex].Details;
                if (details == null || details.theater_id < CoreConstants.MinimumValidIdolIdentifier)
                {
                    continue;
                }

                Theaters._theater theater = Theaters.GetTheater(details.theater_id);
                if (theater == null)
                {
                    continue;
                }

                details.theater_subscriber_total = theater.GetSubscribers();
                if (string.Equals(details.kind, MoneyLedgerConstants.DetailKindTheaterStreaming, StringComparison.Ordinal)
                    && theater.Stats != null
                    && theater.Stats.Count > CoreConstants.ZeroBasedListStartIndex)
                {
                    Theaters._theater._stat latestStat = theater.Stats[theater.Stats.Count - MoneyLedgerDetailConstants.LastCollectionIndexOffset];
                    details.theater_subscriber_delta = latestStat != null
                        ? latestStat.Subscribers
                        : CoreConstants.ZeroBasedListStartIndex;
                }
            }
        }

        private static MoneyLedgerDetailPayload BuildConcertDetail(SEvent_Concerts._concert concert, bool finished)
        {
            if (concert == null)
            {
                return null;
            }

            SEvent_Concerts._concert._projectedValues projected = concert.ProjectedValues;
            MoneyLedgerConcertOutcomeCounts outcomes = MoneyLedgerConcertOutcomeTracker.Get(concert.ID);
            return new MoneyLedgerDetailPayload
            {
                kind = MoneyLedgerConstants.DetailKindConcert,
                concert_id = concert.ID,
                concert_title = concert.GetTitle() ?? string.Empty,
                concert_venue = CoreEnumNameMapping.ToConcertVenueCode(concert.Venue),
                concert_ticket_price = projected != null ? projected.TicketPrice : CoreConstants.ZeroBasedListStartIndex,
                concert_projected_attendance = projected != null ? projected.GetNumberOfSoldTickets() : MoneyLedgerConstants.ZeroMoney,
                concert_projected_hype = projected != null
                    ? Mathf.RoundToInt(projected.GetHype() * MoneyLedgerDetailConstants.PercentageScale)
                    : CoreConstants.ZeroBasedListStartIndex,
                production_cost = projected != null ? projected.GetProductionCost() : MoneyLedgerConstants.ZeroMoney,
                concert_finished = finished,
                concert_finished_hype = finished && projected != null
                    ? projected.Actual_Hype
                    : CoreConstants.ZeroBasedListStartIndex,
                concert_finished_revenue = finished && projected != null
                    ? projected.Actual_Revenue
                    : MoneyLedgerConstants.ZeroMoney,
                concert_finished_profit = finished && projected != null
                    ? projected.GetActualProfit()
                    : MoneyLedgerConstants.ZeroMoney,
                concert_accident_count = concert.UsedAccidents != null
                    ? concert.UsedAccidents.Count
                    : CoreConstants.ZeroBasedListStartIndex,
                concert_accident_successes = outcomes.Successes,
                concert_accident_failures = outcomes.Failures,
                concert_accident_critical_failures = outcomes.CriticalFailures,
                concert_setlist = BuildConcertSetlist(concert)
            };
        }

        private static List<MoneyLedgerConcertSetlistItemPayload> BuildConcertSetlist(SEvent_Concerts._concert concert)
        {
            List<MoneyLedgerConcertSetlistItemPayload> setlist = new List<MoneyLedgerConcertSetlistItemPayload>();
            if (concert == null || concert.SetListItems == null)
            {
                return setlist;
            }

            for (int itemIndex = CoreConstants.ZeroBasedListStartIndex; itemIndex < concert.SetListItems.Count; itemIndex++)
            {
                SEvent_Concerts._concert.ISetlistItem item = concert.SetListItems[itemIndex];
                if (item == null)
                {
                    continue;
                }

                SEvent_Concerts._concert._song song = item as SEvent_Concerts._concert._song;
                setlist.Add(new MoneyLedgerConcertSetlistItemPayload
                {
                    is_talk = item.isMC(),
                    title = item.GetTitle() ?? string.Empty,
                    center_name = song != null && song.Center != null
                        ? song.Center.GetName(true) ?? string.Empty
                        : string.Empty,
                    idol_names = ResolveIdolNames(item.GetGirls(true))
                });
            }

            return setlist;
        }

        internal static bool IsShowMoneyCall()
        {
            StackTrace trace = new StackTrace(false);
            StackFrame[] frames = trace.GetFrames();
            if (frames == null)
            {
                return false;
            }

            for (int frameIndex = CoreConstants.ZeroBasedListStartIndex; frameIndex < frames.Length; frameIndex++)
            {
                MethodBase method = frames[frameIndex].GetMethod();
                Type declaringType = method != null ? method.DeclaringType : null;
                if (declaringType != typeof(Shows))
                {
                    continue;
                }

                string methodName = method.Name ?? string.Empty;
                if (methodName.IndexOf(MoneyLedgerDetailConstants.MethodShowRelease, StringComparison.Ordinal) >= CoreConstants.ZeroBasedListStartIndex
                    || methodName.IndexOf(MoneyLedgerDetailConstants.MethodShowOnNewDay, StringComparison.Ordinal) >= CoreConstants.ZeroBasedListStartIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private static void PopulateWeeklyExpenseAllocations(MoneyMutationSnapshot snapshot, resources resourceManager)
        {
            for (int idolIndex = CoreConstants.ZeroBasedListStartIndex; idolIndex < data_girls.girl.Count; idolIndex++)
            {
                data_girls.girls idol = data_girls.girl[idolIndex];
                if (idol == null || idol.status == data_girls._status.graduated)
                {
                    continue;
                }

                MoneyLedgerDetailPayload details = new MoneyLedgerDetailPayload
                {
                    kind = MoneyLedgerConstants.DetailKindIdolSalary,
                    idol_id = idol.id,
                    idol_name = idol.GetName(true) ?? string.Empty,
                    salary_amount = idol.salary,
                    idol_fame = idol.GetFameLevel(),
                    idol_scandal_points = idol.GetScandalPoints()
                };
                snapshot.Allocations.Add(new MoneyLedgerAllocationSnapshot
                {
                    Amount = -idol.salary,
                    CategoryCode = MoneyLedgerConstants.CategoryIdolSalaries,
                    DetailCode = MoneyLedgerConstants.DetailIdolSalaries,
                    SectionCode = MoneyLedgerConstants.SectionExpense,
                    IncludeWhenZero = true,
                    Details = details
                });
            }

            for (int staffIndex = CoreConstants.ZeroBasedListStartIndex; staffIndex < staff.Staff.Count; staffIndex++)
            {
                staff._staff staffer = staff.Staff[staffIndex];
                if (staffer == null || staffer.GetSalary() < MoneyLedgerDetailConstants.MinimumPaidSalary)
                {
                    continue;
                }

                long salary = staffer.GetSalary();
                MoneyLedgerDetailPayload details = new MoneyLedgerDetailPayload
                {
                    kind = MoneyLedgerConstants.DetailKindStaffSalary,
                    staff_name = staffer.GetName(true, false) ?? string.Empty,
                    staff_role_code = CoreEnumNameMapping.ToStaffTypeCode(staffer.type),
                    salary_amount = salary,
                    staff_skills = BuildStaffSkillDetails(staffer)
                };
                snapshot.Allocations.Add(new MoneyLedgerAllocationSnapshot
                {
                    Amount = -salary,
                    CategoryCode = MoneyLedgerConstants.CategoryStaffSalaries,
                    DetailCode = MoneyLedgerConstants.DetailStaffSalaries,
                    SectionCode = MoneyLedgerConstants.SectionExpense,
                    Details = details
                });
            }

            snapshot.Allocations.Add(new MoneyLedgerAllocationSnapshot
            {
                Amount = -resourceManager.Money_Rent(false),
                CategoryCode = MoneyLedgerConstants.CategoryRent,
                DetailCode = MoneyLedgerConstants.DetailRent,
                SectionCode = MoneyLedgerConstants.SectionExpense
            });
            snapshot.Allocations.Add(new MoneyLedgerAllocationSnapshot
            {
                Amount = -loans.GetTotalPaymentPerWeek(),
                CategoryCode = MoneyLedgerConstants.CategoryLoans,
                DetailCode = MoneyLedgerConstants.DetailLoanPayment,
                SectionCode = MoneyLedgerConstants.SectionExpense
            });
        }

        private static void PopulateDailyContractAllocations(MoneyMutationSnapshot snapshot)
        {
            business businessManager = ResolveBusinessManager();
            if (businessManager == null || businessManager.ActiveProposals == null)
            {
                return;
            }

            for (int contractIndex = CoreConstants.ZeroBasedListStartIndex; contractIndex < businessManager.ActiveProposals.Count; contractIndex++)
            {
                business.active_proposal activeContract = businessManager.ActiveProposals[contractIndex];
                if (activeContract == null)
                {
                    continue;
                }

                long dailyPayment = Mathf.RoundToInt((float)activeContract.Payment_per_week / MoneyLedgerDetailConstants.DaysPerWeek);
                MoneyLedgerDetailPayload details = BuildActiveContractDetail(activeContract);
                snapshot.Allocations.Add(new MoneyLedgerAllocationSnapshot
                {
                    Amount = dailyPayment,
                    CategoryCode = MoneyLedgerConstants.CategoryContracts,
                    DetailCode = MoneyLedgerConstants.DetailBusinessContracts,
                    SectionCode = MoneyLedgerConstants.SectionIncome,
                    Details = details
                });
            }

            snapshot.AdjustLastAllocationForReconciliation = true;
        }

        private static MoneyLedgerDetailPayload BuildProposalDetail(business._proposal proposal)
        {
            if (proposal == null)
            {
                return null;
            }

            return new MoneyLedgerDetailPayload
            {
                kind = MoneyLedgerConstants.DetailKindContract,
                contract_type_code = CoreEnumNameMapping.ToBusinessContractTypeCode(proposal.type),
                contractor_name = proposal.agentName ?? string.Empty,
                product_name = proposal.productName ?? string.Empty,
                payment_amount = proposal.payment,
                stamina_cost = proposal.stamina,
                liability_amount = proposal.liability,
                idol_id = proposal.girl != null ? proposal.girl.id : CoreConstants.InvalidIdValue,
                idol_name = proposal.girl != null ? proposal.girl.GetName(true) : string.Empty,
                multiplier = MoneyLedgerAmbientContext.ResolveProposalMultiplier(proposal),
                negotiations = proposal.negotiation_attempts
            };
        }

        private static MoneyLedgerDetailPayload BuildActiveContractDetail(business.active_proposal activeContract)
        {
            if (activeContract == null)
            {
                return null;
            }

            MoneyLedgerContractRuntimeMetadata metadata;
            bool hasMetadata = MoneyLedgerAmbientContext.TryGetContractMetadata(activeContract, out metadata);
            if (!hasMetadata)
            {
                hasMetadata = TryRestoreContractMetadata(activeContract, out metadata);
            }

            return new MoneyLedgerDetailPayload
            {
                kind = MoneyLedgerConstants.DetailKindContract,
                contract_type_code = CoreEnumNameMapping.ToBusinessContractTypeCode(activeContract.Type),
                contractor_name = activeContract.Agent_Name ?? string.Empty,
                product_name = activeContract.Product_Name ?? string.Empty,
                payment_amount = activeContract.Payment_per_week,
                stamina_cost = activeContract.Stamina_per_week,
                liability_amount = activeContract.Liability,
                idol_id = activeContract.Girl != null ? activeContract.Girl.id : CoreConstants.InvalidIdValue,
                idol_name = activeContract.Girl != null ? activeContract.Girl.GetName(true) : string.Empty,
                multiplier = hasMetadata ? metadata.Multiplier : MoneyLedgerDetailConstants.ZeroMultiplier,
                negotiations = hasMetadata ? metadata.Negotiations : CoreConstants.ZeroBasedListStartIndex
            };
        }

        private static bool TryRestoreContractMetadata(
            business.active_proposal activeContract,
            out MoneyLedgerContractRuntimeMetadata metadata)
        {
            metadata = null;
            if (activeContract == null)
            {
                return false;
            }

            DateTime endExclusive = staticVars.dateTime.AddDays(MoneyLedgerDetailConstants.ContractMetadataEndDateOffsetDays);
            DateTime startInclusive = endExclusive.AddDays(MoneyLedgerDetailConstants.ContractMetadataLookbackDays);
            List<IMDataCoreMoneyTransaction> transactions;
            bool wasTruncated;
            string errorMessage;
            if (!IMDataCoreApi.TryReadMoneyTransactions(
                startInclusive,
                endExclusive,
                MoneyLedgerDetailConstants.ContractMetadataReadCount,
                out transactions,
                out wasTruncated,
                out errorMessage))
            {
                return false;
            }

            string contractTypeCode = CoreEnumNameMapping.ToBusinessContractTypeCode(activeContract.Type);
            int idolId = activeContract.Girl != null ? activeContract.Girl.id : CoreConstants.InvalidIdValue;
            for (int transactionIndex = transactions.Count - MoneyLedgerDetailConstants.LastCollectionIndexOffset;
                transactionIndex >= CoreConstants.ZeroBasedListStartIndex;
                transactionIndex--)
            {
                IMDataCoreMoneyTransactionDetail details = transactions[transactionIndex].Details;
                if (details == null
                    || !string.Equals(details.Kind, MoneyLedgerConstants.DetailKindContract, StringComparison.Ordinal)
                    || details.Multiplier <= MoneyLedgerDetailConstants.ZeroMultiplier
                    || details.IdolId != idolId
                    || !string.Equals(details.ContractTypeCode, contractTypeCode, StringComparison.Ordinal)
                    || !string.Equals(details.ContractorName, activeContract.Agent_Name, StringComparison.Ordinal)
                    || !string.Equals(details.ProductName, activeContract.Product_Name, StringComparison.Ordinal))
                {
                    continue;
                }

                metadata = new MoneyLedgerContractRuntimeMetadata
                {
                    Multiplier = details.Multiplier,
                    Negotiations = details.Negotiations
                };
                MoneyLedgerAmbientContext.RegisterContract(activeContract, metadata.Multiplier, metadata.Negotiations);
                return true;
            }

            return false;
        }

        private static MoneyLedgerDetailPayload BuildSingleDetail(singles._single single)
        {
            MoneyLedgerDetailPayload details = new MoneyLedgerDetailPayload
            {
                kind = MoneyLedgerConstants.DetailKindSingle,
                single_title = single.title ?? string.Empty,
                single_group_name = single.GetGroup() != null ? single.GetGroup().Title ?? string.Empty : string.Empty,
                single_genre_token = ResolveSingleParameterToken(single.genre),
                single_lyrics_token = ResolveSingleParameterToken(single.lyrics),
                single_choreography_token = ResolveSingleParameterToken(single.choreography),
                single_marketing_tokens = ResolveSingleMarketingTokens(single.marketing),
                participant_names = ResolveIdolNames(single.girls),
                gross_revenue = single.GetMoney(),
                production_cost = single.GetProductionCost()
            };
            return details;
        }

        private static MoneyLedgerDetailPayload BuildShowDetail(Shows._show show)
        {
            long fanAudience;
            bool hasFanAudience = TryGetFansWatchAudience(show, out fanAudience);
            return new MoneyLedgerDetailPayload
            {
                kind = MoneyLedgerConstants.DetailKindShow,
                show_title = show.title ?? string.Empty,
                show_medium_token = ResolveSingleParameterToken(show.medium),
                show_genre_token = ResolveSingleParameterToken(show.genre),
                show_host_token = ResolveSingleParameterToken(show.mc),
                show_episode_number = show.episodeCount,
                show_audience = show.GetAudience(null),
                has_fan_audience = hasFanAudience,
                show_fan_audience = fanAudience,
                show_fatigue = show.GetFatigue(null),
                show_weekly_budget = show.GetBudget(),
                participant_names = ResolveIdolNames(show.GetCast()),
                gross_revenue = show.GetRevenue(null)
            };
        }

        private static List<MoneyLedgerStaffSkillPayload> BuildStaffSkillDetails(staff._staff staffer)
        {
            List<MoneyLedgerStaffSkillPayload> skills = new List<MoneyLedgerStaffSkillPayload>();
            if (staffer == null || staffer.skills == null)
            {
                return skills;
            }

            for (int skillIndex = CoreConstants.ZeroBasedListStartIndex; skillIndex < staffer.skills.Count; skillIndex++)
            {
                staff._staff._skill skill = staffer.skills[skillIndex];
                if (skill == null)
                {
                    continue;
                }

                skills.Add(new MoneyLedgerStaffSkillPayload
                {
                    code = skill.skill_type.ToString().ToLowerInvariant(),
                    level = skill.level,
                    progress = skill.progress
                });
            }

            return skills;
        }

        private static List<string> ResolveSingleMarketingTokens(List<singles._param> marketing)
        {
            List<string> tokens = new List<string>();
            if (marketing == null)
            {
                return tokens;
            }

            for (int marketingIndex = CoreConstants.ZeroBasedListStartIndex; marketingIndex < marketing.Count; marketingIndex++)
            {
                string token = ResolveSingleParameterToken(marketing[marketingIndex]);
                if (!string.IsNullOrEmpty(token))
                {
                    tokens.Add(token);
                }
            }

            return tokens;
        }

        private static string ResolveSingleParameterToken(singles._param parameter)
        {
            return parameter != null ? parameter.title ?? string.Empty : string.Empty;
        }

        private static List<string> ResolveIdolNames(IEnumerable<data_girls.girls> idols)
        {
            List<string> names = new List<string>();
            if (idols == null)
            {
                return names;
            }

            foreach (data_girls.girls idol in idols)
            {
                if (idol != null)
                {
                    names.Add(idol.GetName(true) ?? string.Empty);
                }
            }

            return names;
        }

        private static business ResolveBusinessManager()
        {
            if (Camera.main == null)
            {
                return null;
            }

            mainScript main = Camera.main.GetComponent<mainScript>();
            return main != null && main.Data != null ? main.Data.GetComponent<business>() : null;
        }

        private static bool TryGetFansWatchAudience(Shows._show show, out long fanAudience)
        {
            fanAudience = MoneyLedgerConstants.ZeroMoney;
            if (show == null || !Harmony.HasAnyPatches(MoneyLedgerDetailConstants.FansWatchHarmonyId))
            {
                return false;
            }

            if (!fansWatchLookupCompleted)
            {
                fansWatchLookupCompleted = true;
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int assemblyIndex = CoreConstants.ZeroBasedListStartIndex; assemblyIndex < assemblies.Length; assemblyIndex++)
                {
                    Assembly assembly = assemblies[assemblyIndex];
                    if (assembly == null || !string.Equals(assembly.GetName().Name, MoneyLedgerDetailConstants.FansWatchAssemblyName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Type patchType = assembly.GetType(MoneyLedgerDetailConstants.FansWatchAudiencePatchType, false);
                    fansWatchBonusMethod = patchType != null
                        ? patchType.GetMethod(MoneyLedgerDetailConstants.FansWatchBonusMethod, BindingFlags.Public | BindingFlags.Static)
                        : null;
                    break;
                }
            }

            if (fansWatchBonusMethod == null)
            {
                return false;
            }

            try
            {
                object result = fansWatchBonusMethod.Invoke(null, new object[] { show });
                fanAudience = Convert.ToInt64(result);
                return true;
            }
            catch
            {
                fanAudience = MoneyLedgerConstants.ZeroMoney;
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(business), MoneyLedgerDetailConstants.MethodAccept)]
    internal static class business_Accept_IMDataCoreMoneyLedgerDetails_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(business __instance)
        {
            MoneyLedgerAmbientContext.Set(MoneyLedgerCaptureDetails.BuildProposalCapture(__instance != null ? __instance.ActiveProposal : null));
        }

        [HarmonyFinalizer]
        private static void Finalizer()
        {
            MoneyLedgerAmbientContext.Clear();
        }
    }

    [HarmonyPatch(typeof(business), MoneyLedgerDetailConstants.MethodAddActiveProposal)]
    internal static class business_AddActiveProposal_IMDataCoreMoneyLedgerDetails_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(business __instance, business._proposal prop)
        {
            if (__instance == null || prop == null || __instance.ActiveProposals == null || __instance.ActiveProposals.Count <= CoreConstants.ZeroBasedListStartIndex)
            {
                return;
            }

            business.active_proposal activeContract = __instance.ActiveProposals[__instance.ActiveProposals.Count - MoneyLedgerDetailConstants.LastCollectionIndexOffset];
            MoneyLedgerAmbientContext.RegisterContract(activeContract, prop);
        }
    }

    [HarmonyPatch(typeof(business), MoneyLedgerDetailConstants.MethodCancelContract, new Type[] { typeof(business.active_proposal), typeof(bool) })]
    internal static class business_CancelContract_IMDataCoreMoneyLedgerDetails_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(business.active_proposal _Proposal, bool Damages)
        {
            MoneyLedgerAmbientContext.Set(Damages ? MoneyLedgerCaptureDetails.BuildActiveContractCapture(_Proposal) : null);
        }

        [HarmonyFinalizer]
        private static void Finalizer()
        {
            MoneyLedgerAmbientContext.Clear();
        }
    }

    [HarmonyPatch(typeof(business), MoneyLedgerDetailConstants.MethodBreakContracts, new Type[] { typeof(data_girls.girls) })]
    internal static class business_BreakContractsGirl_IMDataCoreMoneyLedgerDetails_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(business __instance, data_girls.girls _girl)
        {
            List<business.active_proposal> matching = new List<business.active_proposal>();
            if (__instance != null && __instance.ActiveProposals != null)
            {
                for (int contractIndex = CoreConstants.ZeroBasedListStartIndex; contractIndex < __instance.ActiveProposals.Count; contractIndex++)
                {
                    business.active_proposal contract = __instance.ActiveProposals[contractIndex];
                    if (contract != null && contract.Girl == _girl)
                    {
                        matching.Add(contract);
                    }
                }
            }

            MoneyLedgerAmbientContext.Set(MoneyLedgerCaptureDetails.BuildBrokenContractCapture(matching));
        }

        [HarmonyFinalizer]
        private static void Finalizer()
        {
            MoneyLedgerAmbientContext.Clear();
        }
    }

    [HarmonyPatch(typeof(business), MoneyLedgerDetailConstants.MethodBreakContracts, new Type[] { typeof(List<Event_Manager._activeEvent._actor>) })]
    internal static class business_BreakContractsActors_IMDataCoreMoneyLedgerDetails_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(business __instance, List<Event_Manager._activeEvent._actor> Actors)
        {
            List<business.active_proposal> matching = new List<business.active_proposal>();
            if (__instance != null && __instance.ActiveProposals != null && Actors != null)
            {
                for (int contractIndex = CoreConstants.ZeroBasedListStartIndex; contractIndex < __instance.ActiveProposals.Count; contractIndex++)
                {
                    business.active_proposal contract = __instance.ActiveProposals[contractIndex];
                    if (contract == null || contract.Girl == null)
                    {
                        continue;
                    }

                    for (int actorIndex = CoreConstants.ZeroBasedListStartIndex; actorIndex < Actors.Count; actorIndex++)
                    {
                        Event_Manager._activeEvent._actor actor = Actors[actorIndex];
                        if (actor != null && actor.girl == contract.Girl)
                        {
                            matching.Add(contract);
                            break;
                        }
                    }
                }
            }

            MoneyLedgerAmbientContext.Set(MoneyLedgerCaptureDetails.BuildBrokenContractCapture(matching));
        }

        [HarmonyFinalizer]
        private static void Finalizer()
        {
            MoneyLedgerAmbientContext.Clear();
        }
    }

    [HarmonyPatch(typeof(singles), MoneyLedgerDetailConstants.MethodSingleAddMoney)]
    internal static class singles_AddMoney_IMDataCoreMoneyLedgerDetails_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(singles._single single)
        {
            MoneyLedgerAmbientContext.Set(MoneyLedgerCaptureDetails.BuildSingleCapture(single));
        }

        [HarmonyFinalizer]
        private static void Finalizer()
        {
            MoneyLedgerAmbientContext.Clear();
        }
    }

    [HarmonyPatch(typeof(Shows._show), MoneyLedgerDetailConstants.MethodShowGetProfit)]
    internal static class Shows_show_GetProfit_IMDataCoreMoneyLedgerDetails_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(Shows._show __instance)
        {
            if (MoneyLedgerCaptureDetails.IsShowMoneyCall())
            {
                MoneyLedgerAmbientContext.Set(MoneyLedgerCaptureDetails.BuildShowCapture(__instance));
            }
        }
    }

    [HarmonyPatch(typeof(Shows), MoneyLedgerDetailConstants.MethodShowRelease)]
    internal static class Shows_ReleaseShow_IMDataCoreMoneyLedgerContextCleanup_Patch
    {
        [HarmonyFinalizer]
        private static void Finalizer()
        {
            MoneyLedgerAmbientContext.Clear();
        }
    }

    [HarmonyPatch(typeof(Shows), MoneyLedgerDetailConstants.MethodShowOnNewDay)]
    internal static class Shows_OnNewDay_IMDataCoreMoneyLedgerContextCleanup_Patch
    {
        [HarmonyFinalizer]
        private static void Finalizer()
        {
            MoneyLedgerAmbientContext.Clear();
        }
    }
}
