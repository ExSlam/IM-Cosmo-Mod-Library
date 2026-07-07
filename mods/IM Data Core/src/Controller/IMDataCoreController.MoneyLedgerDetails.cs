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
            return capture;
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

            MoneyLedgerAmbientCapture ambient = MoneyLedgerAmbientContext.Consume();
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
                Details = BuildProposalDetail(proposal)
            };
        }

        internal static MoneyLedgerAmbientCapture BuildActiveContractCapture(business.active_proposal activeContract)
        {
            return new MoneyLedgerAmbientCapture
            {
                Details = BuildActiveContractDetail(activeContract)
            };
        }

        internal static MoneyLedgerAmbientCapture BuildBrokenContractCapture(IEnumerable<business.active_proposal> contracts)
        {
            MoneyLedgerAmbientCapture capture = new MoneyLedgerAmbientCapture();
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
            MoneyLedgerAmbientCapture capture = new MoneyLedgerAmbientCapture();
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
            MoneyLedgerAmbientCapture capture = new MoneyLedgerAmbientCapture();
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
}
