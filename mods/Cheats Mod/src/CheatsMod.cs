using System;
using System.Collections.Generic;
using UnityEngine;

namespace CheatsMod
{
    internal static class CheatAmounts
    {
        internal const long YenGrant = 1000000000L;
        internal const long FanGrant = 10000L;
        internal const long FameGrant = 10000L;
        internal const long BuzzGrant = 100L;
        internal const long GeneralScandalPointDelta = 1L;
        internal const float IdolScandalPointDelta = 1f;
        internal const float IdolParameterMaximumValue = 100f;
        internal const float VisibleResearchPointGrant = 1000f;
        internal const float ResearchStorageMultiplier = 10f;
        internal const float RawResearchPointGrant = VisibleResearchPointGrant * ResearchStorageMultiplier;
        internal const float StaffExperienceGrant = 25000f;
        internal const int RelationshipLevelIncrement = 1;
        internal const int MaximumPositiveRelationshipLevel = 5;
        internal const int IdolFameLevelIncrement = 1;
        internal const int MaximumIdolFameLevel = 10;
        internal const int TargetResearchLevel = 10;
        internal const int TargetStaffLevel = 20;
        internal const int MinimumRelationshipPointIncrease = 1;
        internal const int MinimumRelationshipPointDelta = 0;
        internal const int ZeroCount = 0;
        internal const int ZeroPoints = 0;
    }

    internal static class CheatLocalizationKeys
    {
        internal const string NotificationAddMoney = "notification.add_money";
        internal const string NotificationAddFans = "notification.add_fans";
        internal const string NotificationAddFame = "notification.add_fame";
        internal const string NotificationAddBuzz = "notification.add_buzz";
        internal const string NotificationResetSpecialEventCooldowns = "notification.reset_special_event_cooldowns";
        internal const string NotificationRefreshPhysicalStamina = "notification.refresh_physical_stamina";
        internal const string NotificationRefreshMentalStamina = "notification.refresh_mental_stamina";
        internal const string NotificationReduceGeneralScandalPoints = "notification.reduce_general_scandal_points";
        internal const string NotificationAddGeneralScandalPoints = "notification.add_general_scandal_points";
        internal const string NotificationAddIdolScandalPoints = "notification.add_idol_scandal_points";
        internal const string NotificationClearScandalPoints = "notification.clear_scandal_points";
        internal const string NotificationIncreaseInfluence = "notification.increase_influence";
        internal const string NotificationIncreaseFriendship = "notification.increase_friendship";
        internal const string NotificationIncreaseRomance = "notification.increase_romance";
        internal const string NotificationAddStaffExperience = "notification.add_staff_experience";
        internal const string NotificationAddResearchPoints = "notification.add_research_points";
        internal const string NotificationSetIdolStats = "notification.set_idol_stats";
        internal const string NotificationIncreaseIdolFame = "notification.increase_idol_fame";
        internal const string NotificationRevealFriends = "notification.reveal_friends";
        internal const string NotificationRevealBestFriends = "notification.reveal_best_friends";
        internal const string NotificationRevealDislikedIdols = "notification.reveal_disliked_idols";
        internal const string NotificationRevealCliques = "notification.reveal_cliques";
        internal const string NotificationRevealBullies = "notification.reveal_bullies";
        internal const string NotificationMaxResearch = "notification.max_research";
        internal const string NotificationMaxStaffLevels = "notification.max_staff_levels";
        internal const string NotificationRevealDatingStatus = "notification.reveal_dating_status";
        internal const string NotificationRevealDatingPreference = "notification.reveal_dating_preference";
        internal const string NotificationHealAndEndHiatus = "notification.heal_and_end_hiatus";
        internal const string NotificationMaxPlayerRelationships = "notification.max_player_relationships";
        internal const string NotificationNoActiveIdols = "notification.no_active_idols";
        internal const string NotificationNoIdols = "notification.no_idols";
        internal const string NotificationNoStaff = "notification.no_staff";
        internal const string NotificationNoResearch = "notification.no_research";
        internal const string NotificationNoRelationships = "notification.no_relationships";
        internal const string NotificationNoCliques = "notification.no_cliques";
        internal const string NotificationNoBullying = "notification.no_bullying";
        internal const string NotificationGameUnavailable = "notification.game_unavailable";
        internal const string NotificationCheatFailed = "notification.cheat_failed";
    }

    internal static class CheatFallbackText
    {
        internal const string NotificationAddMoney = "Added 1B yen.";
        internal const string NotificationAddFans = "Added 10k fans.";
        internal const string NotificationAddFame = "Added 10k fame.";
        internal const string NotificationAddBuzz = "Added 100 buzz.";
        internal const string NotificationResetSpecialEventCooldowns = "Special event cooldowns reset.";
        internal const string NotificationRefreshPhysicalStamina = "Active idol physical stamina set to 100.";
        internal const string NotificationRefreshMentalStamina = "Active idol mental stamina set to 100.";
        internal const string NotificationReduceGeneralScandalPoints = "General scandal points reduced by 1.";
        internal const string NotificationAddGeneralScandalPoints = "General scandal points increased by 1.";
        internal const string NotificationAddIdolScandalPoints = "Added 1 scandal point to all idols.";
        internal const string NotificationClearScandalPoints = "All scandal points set to 0.";
        internal const string NotificationIncreaseInfluence = "Active idol influence increased.";
        internal const string NotificationIncreaseFriendship = "Active idol friendship increased.";
        internal const string NotificationIncreaseRomance = "Active idol romance increased.";
        internal const string NotificationAddStaffExperience = "Added 25k EXP to all staff.";
        internal const string NotificationAddResearchPoints = "Added 1k points to every research type.";
        internal const string NotificationSetIdolStats = "All idol stats set to 100.";
        internal const string NotificationIncreaseIdolFame = "Idol fame increased by one level.";
        internal const string NotificationRevealFriends = "Friend relationships revealed.";
        internal const string NotificationRevealBestFriends = "Best friend relationships revealed.";
        internal const string NotificationRevealDislikedIdols = "Disliked idol relationships revealed.";
        internal const string NotificationRevealCliques = "Cliques revealed.";
        internal const string NotificationRevealBullies = "Bullying targets revealed.";
        internal const string NotificationMaxResearch = "All research unlocked and set to level 10.";
        internal const string NotificationMaxStaffLevels = "All staff levels set to 20.";
        internal const string NotificationRevealDatingStatus = "Dating statuses revealed.";
        internal const string NotificationRevealDatingPreference = "Dating preferences revealed.";
        internal const string NotificationHealAndEndHiatus = "All idols healed and returned from hiatus.";
        internal const string NotificationMaxPlayerRelationships = "Active idol influence, friendship, and romance maxed.";
        internal const string NotificationNoActiveIdols = "No active idols found.";
        internal const string NotificationNoIdols = "No idols found.";
        internal const string NotificationNoStaff = "No staff found.";
        internal const string NotificationNoResearch = "Research categories are not available yet.";
        internal const string NotificationNoRelationships = "No matching relationships found.";
        internal const string NotificationNoCliques = "No cliques found.";
        internal const string NotificationNoBullying = "No bullying targets found.";
        internal const string NotificationGameUnavailable = "Game data is not available yet.";
        internal const string NotificationCheatFailed = "Cheat action failed.";
    }

    internal static class CheatLogMessages
    {
        internal const string ExecutionFailedFormat = "[CheatsMod] Cheat action failed: {0}";
        internal const string NotificationFailedFormat = "[CheatsMod] Notification failed: {0}";
    }

    public static class Cheats
    {
        private static readonly Research.type[] ResearchTypes = new Research.type[]
        {
            Research.type.player,
            Research.type.office,
            Research.type.dance,
            Research.type.vocal
        };

        private static readonly data_girls._paramType[] IdolCoreStatTypes = new data_girls._paramType[]
        {
            data_girls._paramType.cute,
            data_girls._paramType.cool,
            data_girls._paramType.sexy,
            data_girls._paramType.pretty,
            data_girls._paramType.dance,
            data_girls._paramType.vocal,
            data_girls._paramType.funny,
            data_girls._paramType.smart
        };

        private static readonly Relationships._relationship._status[] FriendRelationshipStatuses = new Relationships._relationship._status[]
        {
            Relationships._relationship._status.friends
        };

        private static readonly Relationships._relationship._status[] BestFriendRelationshipStatuses = new Relationships._relationship._status[]
        {
            Relationships._relationship._status.best_friends
        };

        private static readonly Relationships._relationship._status[] DislikedRelationshipStatuses = new Relationships._relationship._status[]
        {
            Relationships._relationship._status.dislikes,
            Relationships._relationship._status.hates
        };

        private static readonly Relationships_Player._type[] PlayerRelationshipTypes = new Relationships_Player._type[]
        {
            Relationships_Player._type.Influence,
            Relationships_Player._type.Friendship,
            Relationships_Player._type.Romance
        };

        public static void AddOneBillionYen()
        {
            Execute(AddOneBillionYenCore);
        }

        public static void AddTenThousandFans()
        {
            Execute(AddTenThousandFansCore);
        }

        public static void AddTenThousandFame()
        {
            Execute(AddTenThousandFameCore);
        }

        public static void AddOneHundredBuzz()
        {
            Execute(AddOneHundredBuzzCore);
        }

        public static void ResetSpecialEventCooldowns()
        {
            Execute(ResetSpecialEventCooldownsCore);
        }

        public static void RefreshActiveIdolPhysicalStamina()
        {
            Execute(RefreshActiveIdolPhysicalStaminaCore);
        }

        public static void RefreshActiveIdolMentalStamina()
        {
            Execute(RefreshActiveIdolMentalStaminaCore);
        }

        public static void ReduceGeneralScandalPoints()
        {
            Execute(ReduceGeneralScandalPointsCore);
        }

        public static void AddGeneralScandalPoints()
        {
            Execute(AddGeneralScandalPointsCore);
        }

        public static void AddOneScandalPointToAllIdols()
        {
            Execute(AddOneScandalPointToAllIdolsCore);
        }

        public static void ClearAllScandalPoints()
        {
            Execute(ClearAllScandalPointsCore);
        }

        public static void IncreaseActiveIdolInfluence()
        {
            Execute(IncreaseActiveIdolInfluenceCore);
        }

        public static void IncreaseActiveIdolFriendship()
        {
            Execute(IncreaseActiveIdolFriendshipCore);
        }

        public static void IncreaseActiveIdolRomance()
        {
            Execute(IncreaseActiveIdolRomanceCore);
        }

        public static void AddStaffExperience()
        {
            Execute(AddStaffExperienceCore);
        }

        public static void AddResearchPoints()
        {
            Execute(AddResearchPointsCore);
        }

        public static void SetIdolStatsToMaximum()
        {
            Execute(SetIdolStatsToMaximumCore);
        }

        public static void IncreaseIdolFameLevel()
        {
            Execute(IncreaseIdolFameLevelCore);
        }

        public static void RevealFriends()
        {
            Execute(RevealFriendsCore);
        }

        public static void RevealBestFriends()
        {
            Execute(RevealBestFriendsCore);
        }

        public static void RevealDislikedIdols()
        {
            Execute(RevealDislikedIdolsCore);
        }

        public static void RevealCliques()
        {
            Execute(RevealCliquesCore);
        }

        public static void RevealBullies()
        {
            Execute(RevealBulliesCore);
        }

        public static void MaxOutResearch()
        {
            Execute(MaxOutResearchCore);
        }

        public static void MaxOutStaffLevels()
        {
            Execute(MaxOutStaffLevelsCore);
        }

        public static void RevealDatingStatus()
        {
            Execute(RevealDatingStatusCore);
        }

        public static void RevealDatingPreference()
        {
            Execute(RevealDatingPreferenceCore);
        }

        public static void HealAndEndHiatus()
        {
            Execute(HealAndEndHiatusCore);
        }

        public static void MaxOutActiveIdolPlayerRelationships()
        {
            Execute(MaxOutActiveIdolPlayerRelationshipsCore);
        }

        private static void AddOneBillionYenCore()
        {
            AddResource(
                resources.type.money,
                CheatAmounts.YenGrant,
                CheatLocalizationKeys.NotificationAddMoney,
                CheatFallbackText.NotificationAddMoney);
        }

        private static void AddTenThousandFansCore()
        {
            AddResource(
                resources.type.fans,
                CheatAmounts.FanGrant,
                CheatLocalizationKeys.NotificationAddFans,
                CheatFallbackText.NotificationAddFans);
        }

        private static void AddTenThousandFameCore()
        {
            AddResource(
                resources.type.fame,
                CheatAmounts.FameGrant,
                CheatLocalizationKeys.NotificationAddFame,
                CheatFallbackText.NotificationAddFame);
        }

        private static void AddOneHundredBuzzCore()
        {
            AddResource(
                resources.type.buzz,
                CheatAmounts.BuzzGrant,
                CheatLocalizationKeys.NotificationAddBuzz,
                CheatFallbackText.NotificationAddBuzz);
        }

        private static void ResetSpecialEventCooldownsCore()
        {
            if (!RequireGameData())
            {
                return;
            }

            SpecialEvents_Manager.ForceResetCooldowns();
            RefreshSpecialEventsUi();
            NotifySuccess(
                CheatLocalizationKeys.NotificationResetSpecialEventCooldowns,
                CheatFallbackText.NotificationResetSpecialEventCooldowns,
                NotificationManager._notification._type.other);
        }

        private static void RefreshActiveIdolPhysicalStaminaCore()
        {
            SetActiveIdolParameter(
                data_girls._paramType.physicalStamina,
                CheatAmounts.IdolParameterMaximumValue,
                CheatLocalizationKeys.NotificationRefreshPhysicalStamina,
                CheatFallbackText.NotificationRefreshPhysicalStamina);
        }

        private static void RefreshActiveIdolMentalStaminaCore()
        {
            SetActiveIdolParameter(
                data_girls._paramType.mentalStamina,
                CheatAmounts.IdolParameterMaximumValue,
                CheatLocalizationKeys.NotificationRefreshMentalStamina,
                CheatFallbackText.NotificationRefreshMentalStamina);
        }

        private static void ReduceGeneralScandalPointsCore()
        {
            if (!RequireGameData())
            {
                return;
            }

            long currentPoints = resources.Get(resources.type.scandalPoints, false);
            long updatedPoints = Math.Max(CheatAmounts.ZeroPoints, currentPoints - CheatAmounts.GeneralScandalPointDelta);
            resources.Set_(resources.type.scandalPoints, updatedPoints);
            resources.UpdateScandalPointsCounter();
            NotifySuccess(
                CheatLocalizationKeys.NotificationReduceGeneralScandalPoints,
                CheatFallbackText.NotificationReduceGeneralScandalPoints,
                NotificationManager._notification._type.resource_change);
        }

        private static void AddGeneralScandalPointsCore()
        {
            AddResource(
                resources.type.scandalPoints,
                CheatAmounts.GeneralScandalPointDelta,
                CheatLocalizationKeys.NotificationAddGeneralScandalPoints,
                CheatFallbackText.NotificationAddGeneralScandalPoints);
        }

        private static void AddOneScandalPointToAllIdolsCore()
        {
            if (!RequireGameData())
            {
                return;
            }

            int appliedCount = ApplyToAllIdols(delegate(data_girls.girls idol)
            {
                idol.addParam(data_girls._paramType.scandalPoints, CheatAmounts.IdolScandalPointDelta, true);
            });

            if (appliedCount == CheatAmounts.ZeroCount)
            {
                NotifyWarning(CheatLocalizationKeys.NotificationNoIdols, CheatFallbackText.NotificationNoIdols);
                return;
            }

            RefreshIdolList();
            NotifySuccess(
                CheatLocalizationKeys.NotificationAddIdolScandalPoints,
                CheatFallbackText.NotificationAddIdolScandalPoints,
                NotificationManager._notification._type.idol_stat_change);
        }

        private static void ClearAllScandalPointsCore()
        {
            if (!RequireGameData())
            {
                return;
            }

            resources.Set_(resources.type.scandalPoints, CheatAmounts.ZeroPoints);
            ApplyToAllIdols(delegate(data_girls.girls idol)
            {
                idol.setParam(data_girls._paramType.scandalPoints, CheatAmounts.ZeroPoints);
            });

            resources.UpdateScandalPointsCounter();
            RefreshIdolList();
            NotifySuccess(
                CheatLocalizationKeys.NotificationClearScandalPoints,
                CheatFallbackText.NotificationClearScandalPoints,
                NotificationManager._notification._type.resource_change);
        }

        private static void IncreaseActiveIdolInfluenceCore()
        {
            IncreaseActiveIdolRelationship(
                Relationships_Player._type.Influence,
                CheatLocalizationKeys.NotificationIncreaseInfluence,
                CheatFallbackText.NotificationIncreaseInfluence);
        }

        private static void IncreaseActiveIdolFriendshipCore()
        {
            IncreaseActiveIdolRelationship(
                Relationships_Player._type.Friendship,
                CheatLocalizationKeys.NotificationIncreaseFriendship,
                CheatFallbackText.NotificationIncreaseFriendship);
        }

        private static void IncreaseActiveIdolRomanceCore()
        {
            IncreaseActiveIdolRelationship(
                Relationships_Player._type.Romance,
                CheatLocalizationKeys.NotificationIncreaseRomance,
                CheatFallbackText.NotificationIncreaseRomance);
        }

        private static void AddStaffExperienceCore()
        {
            if (!RequireGameData())
            {
                return;
            }

            int appliedCount = ApplyToAllStaff(delegate(staff._staff staffMember)
            {
                if (staffMember.skills == null)
                {
                    return;
                }

                for (int skillIndex = 0; skillIndex < staffMember.skills.Count; skillIndex++)
                {
                    staff._staff._skill skill = staffMember.skills[skillIndex];
                    if (skill != null)
                    {
                        skill.AddExp(CheatAmounts.StaffExperienceGrant);
                    }
                }
            });

            if (appliedCount == CheatAmounts.ZeroCount)
            {
                NotifyWarning(CheatLocalizationKeys.NotificationNoStaff, CheatFallbackText.NotificationNoStaff);
                return;
            }

            RefreshStaffList();
            NotifySuccess(
                CheatLocalizationKeys.NotificationAddStaffExperience,
                CheatFallbackText.NotificationAddStaffExperience,
                NotificationManager._notification._type.staff_stat_change);
        }

        private static void AddResearchPointsCore()
        {
            if (!RequireGameData())
            {
                return;
            }

            int appliedCount = CheatAmounts.ZeroCount;
            for (int typeIndex = 0; typeIndex < ResearchTypes.Length; typeIndex++)
            {
                Research.category category = Research.GetCategory(ResearchTypes[typeIndex]);
                if (category == null)
                {
                    continue;
                }

                category.AddPoints(CheatAmounts.RawResearchPointGrant);
                if (category.Button != null)
                {
                    category.RenderButton();
                }

                appliedCount++;
            }

            if (appliedCount == CheatAmounts.ZeroCount)
            {
                NotifyWarning(CheatLocalizationKeys.NotificationNoResearch, CheatFallbackText.NotificationNoResearch);
                return;
            }

            RefreshResearchUi();
            NotifySuccess(
                CheatLocalizationKeys.NotificationAddResearchPoints,
                CheatFallbackText.NotificationAddResearchPoints,
                NotificationManager._notification._type.resource_change);
        }

        private static void SetIdolStatsToMaximumCore()
        {
            if (!RequireGameData())
            {
                return;
            }

            int appliedCount = ApplyToAllIdols(delegate(data_girls.girls idol)
            {
                for (int statIndex = 0; statIndex < IdolCoreStatTypes.Length; statIndex++)
                {
                    idol.setParam(IdolCoreStatTypes[statIndex], CheatAmounts.IdolParameterMaximumValue);
                }
            });

            if (appliedCount == CheatAmounts.ZeroCount)
            {
                NotifyWarning(CheatLocalizationKeys.NotificationNoIdols, CheatFallbackText.NotificationNoIdols);
                return;
            }

            RefreshIdolList();
            NotifySuccess(
                CheatLocalizationKeys.NotificationSetIdolStats,
                CheatFallbackText.NotificationSetIdolStats,
                NotificationManager._notification._type.idol_stat_change);
        }

        private static void IncreaseIdolFameLevelCore()
        {
            if (!RequireGameData())
            {
                return;
            }

            int appliedCount = ApplyToAllIdols(delegate(data_girls.girls idol)
            {
                int currentLevel = idol.GetFameLevel();
                int targetLevel = Math.Min(
                    CheatAmounts.MaximumIdolFameLevel,
                    currentLevel + CheatAmounts.IdolFameLevelIncrement);
                float currentPoints = idol.GetFamePoints();
                float targetPoints = resources.FameLevelToPoints(targetLevel);
                float pointDelta = Math.Max(CheatAmounts.ZeroPoints, targetPoints - currentPoints);
                if (pointDelta > CheatAmounts.ZeroPoints)
                {
                    idol.addParam(data_girls._paramType.famePoints, pointDelta, true);
                }
            });

            if (appliedCount == CheatAmounts.ZeroCount)
            {
                NotifyWarning(CheatLocalizationKeys.NotificationNoIdols, CheatFallbackText.NotificationNoIdols);
                return;
            }

            RefreshIdolList();
            NotifySuccess(
                CheatLocalizationKeys.NotificationIncreaseIdolFame,
                CheatFallbackText.NotificationIncreaseIdolFame,
                NotificationManager._notification._type.idol_stat_change);
        }

        private static void RevealFriendsCore()
        {
            RevealRelationshipsByStatus(
                FriendRelationshipStatuses,
                CheatLocalizationKeys.NotificationRevealFriends,
                CheatFallbackText.NotificationRevealFriends);
        }

        private static void RevealBestFriendsCore()
        {
            RevealRelationshipsByStatus(
                BestFriendRelationshipStatuses,
                CheatLocalizationKeys.NotificationRevealBestFriends,
                CheatFallbackText.NotificationRevealBestFriends);
        }

        private static void RevealDislikedIdolsCore()
        {
            RevealRelationshipsByStatus(
                DislikedRelationshipStatuses,
                CheatLocalizationKeys.NotificationRevealDislikedIdols,
                CheatFallbackText.NotificationRevealDislikedIdols);
        }

        private static void RevealCliquesCore()
        {
            if (!RequireGameData())
            {
                return;
            }

            if (Relationships.Cliques == null || Relationships.Cliques.Count == CheatAmounts.ZeroCount)
            {
                NotifyWarning(CheatLocalizationKeys.NotificationNoCliques, CheatFallbackText.NotificationNoCliques);
                return;
            }

            int appliedCount = CheatAmounts.ZeroCount;
            for (int cliqueIndex = 0; cliqueIndex < Relationships.Cliques.Count; cliqueIndex++)
            {
                Relationships._clique clique = Relationships.Cliques[cliqueIndex];
                if (clique == null)
                {
                    continue;
                }

                clique.Known = true;
                appliedCount++;
            }

            if (appliedCount == CheatAmounts.ZeroCount)
            {
                NotifyWarning(CheatLocalizationKeys.NotificationNoCliques, CheatFallbackText.NotificationNoCliques);
                return;
            }

            RefreshIdolList();
            NotifySuccess(
                CheatLocalizationKeys.NotificationRevealCliques,
                CheatFallbackText.NotificationRevealCliques,
                NotificationManager._notification._type.idol_relationship_change);
        }

        private static void RevealBulliesCore()
        {
            if (!RequireGameData())
            {
                return;
            }

            if (Relationships.Cliques == null || Relationships.Cliques.Count == CheatAmounts.ZeroCount)
            {
                NotifyWarning(CheatLocalizationKeys.NotificationNoBullying, CheatFallbackText.NotificationNoBullying);
                return;
            }

            int appliedCount = CheatAmounts.ZeroCount;
            for (int cliqueIndex = 0; cliqueIndex < Relationships.Cliques.Count; cliqueIndex++)
            {
                Relationships._clique clique = Relationships.Cliques[cliqueIndex];
                if (clique == null || clique.Bullied_Girls == null)
                {
                    continue;
                }

                for (int targetIndex = 0; targetIndex < clique.Bullied_Girls.Count; targetIndex++)
                {
                    data_girls.girls target = clique.Bullied_Girls[targetIndex];
                    if (target == null)
                    {
                        continue;
                    }

                    if (clique.KnownBulliedGirls == null)
                    {
                        clique.KnownBulliedGirls = new List<data_girls.girls>();
                    }

                    if (!clique.KnownBulliedGirls.Contains(target))
                    {
                        clique.AddKnownBulliedGirl(target);
                    }

                    appliedCount++;
                }
            }

            if (appliedCount == CheatAmounts.ZeroCount)
            {
                NotifyWarning(CheatLocalizationKeys.NotificationNoBullying, CheatFallbackText.NotificationNoBullying);
                return;
            }

            RefreshIdolList();
            NotifySuccess(
                CheatLocalizationKeys.NotificationRevealBullies,
                CheatFallbackText.NotificationRevealBullies,
                NotificationManager._notification._type.idol_relationship_change);
        }

        private static void MaxOutResearchCore()
        {
            if (!RequireGameData())
            {
                return;
            }

            List<singles._param> researchParameters = GetResearchParameters();
            if (researchParameters.Count == CheatAmounts.ZeroCount)
            {
                NotifyWarning(CheatLocalizationKeys.NotificationNoResearch, CheatFallbackText.NotificationNoResearch);
                return;
            }

            float targetExperience = resources.FameLevelToPoints(CheatAmounts.TargetResearchLevel);
            for (int parameterIndex = 0; parameterIndex < researchParameters.Count; parameterIndex++)
            {
                singles._param parameter = researchParameters[parameterIndex];
                if (parameter == null)
                {
                    continue;
                }

                parameter.unlocked = true;
                if (parameter.max_level < CheatAmounts.TargetResearchLevel)
                {
                    parameter.max_level = CheatAmounts.TargetResearchLevel;
                }

                if (parameter.exp < targetExperience)
                {
                    parameter.exp = targetExperience;
                }

                Research.category category = parameter.GetResearchCategory();
                if (category != null && category.Button != null)
                {
                    category.RenderButton();
                }
            }

            RefreshResearchUi();
            NotifySuccess(
                CheatLocalizationKeys.NotificationMaxResearch,
                CheatFallbackText.NotificationMaxResearch,
                NotificationManager._notification._type.resource_change);
        }

        private static void MaxOutStaffLevelsCore()
        {
            if (!RequireGameData())
            {
                return;
            }

            int appliedCount = ApplyToAllStaff(delegate(staff._staff staffMember)
            {
                if (staffMember.skills == null)
                {
                    return;
                }

                for (int skillIndex = 0; skillIndex < staffMember.skills.Count; skillIndex++)
                {
                    staff._staff._skill skill = staffMember.skills[skillIndex];
                    if (skill != null)
                    {
                        skill.SetLevel(CheatAmounts.TargetStaffLevel);
                    }
                }
            });

            if (appliedCount == CheatAmounts.ZeroCount)
            {
                NotifyWarning(CheatLocalizationKeys.NotificationNoStaff, CheatFallbackText.NotificationNoStaff);
                return;
            }

            RefreshStaffList();
            NotifySuccess(
                CheatLocalizationKeys.NotificationMaxStaffLevels,
                CheatFallbackText.NotificationMaxStaffLevels,
                NotificationManager._notification._type.staff_stat_change);
        }

        private static void RevealDatingStatusCore()
        {
            if (!RequireGameData())
            {
                return;
            }

            int appliedCount = ApplyToAllIdols(delegate(data_girls.girls idol)
            {
                if (idol.DatingData == null)
                {
                    return;
                }

                idol.DatingData.Is_Partner_Status_Known = true;
                idol.DatingData.Partner_Status_Known_To_Player = idol.DatingData.Partner_Status;
                idol.DatingData.Used_Goods = true;
            });

            if (appliedCount == CheatAmounts.ZeroCount)
            {
                NotifyWarning(CheatLocalizationKeys.NotificationNoIdols, CheatFallbackText.NotificationNoIdols);
                return;
            }

            RefreshIdolList();
            NotifySuccess(
                CheatLocalizationKeys.NotificationRevealDatingStatus,
                CheatFallbackText.NotificationRevealDatingStatus,
                NotificationManager._notification._type.idol_relationship_change);
        }

        private static void RevealDatingPreferenceCore()
        {
            if (!RequireGameData())
            {
                return;
            }

            int appliedCount = ApplyToAllIdols(delegate(data_girls.girls idol)
            {
                if (idol.DatingData != null)
                {
                    idol.DatingData.Is_Sexuality_Known = true;
                }
            });

            if (appliedCount == CheatAmounts.ZeroCount)
            {
                NotifyWarning(CheatLocalizationKeys.NotificationNoIdols, CheatFallbackText.NotificationNoIdols);
                return;
            }

            RefreshIdolList();
            NotifySuccess(
                CheatLocalizationKeys.NotificationRevealDatingPreference,
                CheatFallbackText.NotificationRevealDatingPreference,
                NotificationManager._notification._type.idol_relationship_change);
        }

        private static void HealAndEndHiatusCore()
        {
            if (!RequireGameData())
            {
                return;
            }

            int appliedCount = ApplyToAllIdols(delegate(data_girls.girls idol)
            {
                idol.setParam(data_girls._paramType.physicalStamina, CheatAmounts.IdolParameterMaximumValue);
                idol.setParam(data_girls._paramType.mentalStamina, CheatAmounts.IdolParameterMaximumValue);

                if (idol.status == data_girls._status.hiatus)
                {
                    idol.FinishHiatus(false);
                }
                else if (idol.status == data_girls._status.depressed || idol.status == data_girls._status.injured)
                {
                    idol.Heal();
                }

                idol.setParam(data_girls._paramType.physicalStamina, CheatAmounts.IdolParameterMaximumValue);
                idol.setParam(data_girls._paramType.mentalStamina, CheatAmounts.IdolParameterMaximumValue);
            });

            if (appliedCount == CheatAmounts.ZeroCount)
            {
                NotifyWarning(CheatLocalizationKeys.NotificationNoIdols, CheatFallbackText.NotificationNoIdols);
                return;
            }

            RefreshIdolList();
            NotifySuccess(
                CheatLocalizationKeys.NotificationHealAndEndHiatus,
                CheatFallbackText.NotificationHealAndEndHiatus,
                NotificationManager._notification._type.idol_status_change);
        }

        private static void MaxOutActiveIdolPlayerRelationshipsCore()
        {
            if (!RequireGameData())
            {
                return;
            }

            int appliedCount = ApplyToActiveIdols(delegate(data_girls.girls idol)
            {
                for (int relationshipIndex = 0; relationshipIndex < PlayerRelationshipTypes.Length; relationshipIndex++)
                {
                    SetPlayerRelationshipToMaximum(idol, PlayerRelationshipTypes[relationshipIndex]);
                }
            });

            if (appliedCount == CheatAmounts.ZeroCount)
            {
                NotifyWarning(CheatLocalizationKeys.NotificationNoActiveIdols, CheatFallbackText.NotificationNoActiveIdols);
                return;
            }

            RefreshIdolList();
            NotifySuccess(
                CheatLocalizationKeys.NotificationMaxPlayerRelationships,
                CheatFallbackText.NotificationMaxPlayerRelationships,
                NotificationManager._notification._type.idol_relationship_change);
        }

        private static void Execute(Action cheatAction)
        {
            if (cheatAction == null)
            {
                return;
            }

            try
            {
                cheatAction();
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(CheatLogMessages.ExecutionFailedFormat, ex.Message));
                NotifyWarning(CheatLocalizationKeys.NotificationCheatFailed, CheatFallbackText.NotificationCheatFailed);
            }
        }

        private static void AddResource(
            resources.type resourceType,
            long amount,
            string notificationKey,
            string notificationFallback)
        {
            if (!RequireGameData())
            {
                return;
            }

            resources.Add(resourceType, amount);
            NotifySuccess(notificationKey, notificationFallback, NotificationManager._notification._type.resource_change);
        }

        private static void SetActiveIdolParameter(
            data_girls._paramType parameterType,
            float value,
            string notificationKey,
            string notificationFallback)
        {
            if (!RequireGameData())
            {
                return;
            }

            int appliedCount = ApplyToActiveIdols(delegate(data_girls.girls idol)
            {
                idol.setParam(parameterType, value);
            });

            if (appliedCount == CheatAmounts.ZeroCount)
            {
                NotifyWarning(CheatLocalizationKeys.NotificationNoActiveIdols, CheatFallbackText.NotificationNoActiveIdols);
                return;
            }

            RefreshIdolList();
            NotifySuccess(notificationKey, notificationFallback, NotificationManager._notification._type.idol_stat_change);
        }

        private static void IncreaseActiveIdolRelationship(
            Relationships_Player._type relationshipType,
            string notificationKey,
            string notificationFallback)
        {
            if (!RequireGameData())
            {
                return;
            }

            int appliedCount = ApplyToActiveIdols(delegate(data_girls.girls idol)
            {
                int pointDelta = GetRelationshipIncreaseDelta(idol, relationshipType);
                Relationships_Player.AddPoints(relationshipType, idol, pointDelta);
            });

            if (appliedCount == CheatAmounts.ZeroCount)
            {
                NotifyWarning(CheatLocalizationKeys.NotificationNoActiveIdols, CheatFallbackText.NotificationNoActiveIdols);
                return;
            }

            RefreshIdolList();
            NotifySuccess(notificationKey, notificationFallback, NotificationManager._notification._type.idol_relationship_change);
        }

        private static int GetRelationshipIncreaseDelta(
            data_girls.girls idol,
            Relationships_Player._type relationshipType)
        {
            if (idol == null)
            {
                return CheatAmounts.MinimumRelationshipPointIncrease;
            }

            int currentPoints = idol.GetRelationshipWithPlayer_Points(relationshipType);
            int currentLevel = Relationships_Player.GetLevelByPoints(currentPoints);
            int targetLevel = Math.Min(
                CheatAmounts.MaximumPositiveRelationshipLevel,
                currentLevel + CheatAmounts.RelationshipLevelIncrement);
            int targetPoints = Relationships_Player.GetPointsByLevel(targetLevel);
            int pointDelta = targetPoints - currentPoints;
            return Math.Max(CheatAmounts.MinimumRelationshipPointIncrease, pointDelta);
        }

        private static void SetPlayerRelationshipToMaximum(
            data_girls.girls idol,
            Relationships_Player._type relationshipType)
        {
            if (idol == null)
            {
                return;
            }

            int currentPoints = idol.GetRelationshipWithPlayer_Points(relationshipType);
            int targetPoints = Relationships_Player.GetPointsByLevel(CheatAmounts.MaximumPositiveRelationshipLevel);
            int pointDelta = Math.Max(CheatAmounts.MinimumRelationshipPointDelta, targetPoints - currentPoints);
            if (pointDelta > CheatAmounts.MinimumRelationshipPointDelta)
            {
                Relationships_Player.AddPoints(relationshipType, idol, pointDelta);
            }
        }

        private static void RevealRelationshipsByStatus(
            Relationships._relationship._status[] relationshipStatuses,
            string notificationKey,
            string notificationFallback)
        {
            if (!RequireGameData())
            {
                return;
            }

            if (Relationships.RelationshipsData == null || relationshipStatuses == null)
            {
                NotifyWarning(CheatLocalizationKeys.NotificationNoRelationships, CheatFallbackText.NotificationNoRelationships);
                return;
            }

            int appliedCount = CheatAmounts.ZeroCount;
            for (int relationshipIndex = 0; relationshipIndex < Relationships.RelationshipsData.Count; relationshipIndex++)
            {
                Relationships._relationship relationship = Relationships.RelationshipsData[relationshipIndex];
                if (relationship == null || !RelationshipStatusMatches(relationship.Status, relationshipStatuses))
                {
                    continue;
                }

                appliedCount += MarkRelationshipKnown(relationship);
            }

            if (appliedCount == CheatAmounts.ZeroCount)
            {
                NotifyWarning(CheatLocalizationKeys.NotificationNoRelationships, CheatFallbackText.NotificationNoRelationships);
                return;
            }

            RefreshIdolList();
            NotifySuccess(notificationKey, notificationFallback, NotificationManager._notification._type.idol_relationship_change);
        }

        private static bool RelationshipStatusMatches(
            Relationships._relationship._status currentStatus,
            Relationships._relationship._status[] targetStatuses)
        {
            for (int statusIndex = 0; statusIndex < targetStatuses.Length; statusIndex++)
            {
                if (currentStatus == targetStatuses[statusIndex])
                {
                    return true;
                }
            }

            return false;
        }

        private static int MarkRelationshipKnown(Relationships._relationship relationship)
        {
            if (relationship == null || relationship.Girls == null || relationship.Girls.Count < 2)
            {
                return CheatAmounts.ZeroCount;
            }

            int appliedCount = CheatAmounts.ZeroCount;
            for (int girlIndex = 0; girlIndex < relationship.Girls.Count; girlIndex++)
            {
                data_girls.girls idol = relationship.Girls[girlIndex];
                if (idol == null)
                {
                    continue;
                }

                idol.RelationshipsKnown = true;
                appliedCount++;
            }

            return appliedCount;
        }

        private static List<singles._param> GetResearchParameters()
        {
            List<singles._param> researchParameters = new List<singles._param>();
            AddResearchParameters(researchParameters, singles.Genres);
            AddResearchParameters(researchParameters, singles.Lyrics);
            AddResearchParameters(researchParameters, singles.Choreography);
            AddResearchParameters(researchParameters, singles.Marketing);

            if (Research.Categories != null)
            {
                for (int categoryIndex = 0; categoryIndex < Research.Categories.Count; categoryIndex++)
                {
                    Research.category category = Research.Categories[categoryIndex];
                    if (category != null && category.Param != null && !researchParameters.Contains(category.Param))
                    {
                        researchParameters.Add(category.Param);
                    }
                }
            }

            return researchParameters;
        }

        private static void AddResearchParameters(List<singles._param> target, List<singles._param> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            for (int parameterIndex = 0; parameterIndex < source.Count; parameterIndex++)
            {
                singles._param parameter = source[parameterIndex];
                if (parameter != null && !target.Contains(parameter))
                {
                    target.Add(parameter);
                }
            }
        }

        private static int ApplyToActiveIdols(Action<data_girls.girls> action)
        {
            List<data_girls.girls> idols = data_girls.GetActiveGirls(null);
            return ApplyToIdolList(idols, action);
        }

        private static int ApplyToAllIdols(Action<data_girls.girls> action)
        {
            List<data_girls.girls> idols = data_girls.GetHiredGirls(false);
            if (idols == null)
            {
                idols = data_girls.girl;
            }

            return ApplyToIdolList(idols, action);
        }

        private static int ApplyToIdolList(List<data_girls.girls> idols, Action<data_girls.girls> action)
        {
            if (idols == null || action == null)
            {
                return CheatAmounts.ZeroCount;
            }

            int appliedCount = CheatAmounts.ZeroCount;
            for (int idolIndex = 0; idolIndex < idols.Count; idolIndex++)
            {
                data_girls.girls idol = idols[idolIndex];
                if (idol == null)
                {
                    continue;
                }

                action(idol);
                appliedCount++;
            }

            return appliedCount;
        }

        private static int ApplyToAllStaff(Action<staff._staff> action)
        {
            if (staff.Staff == null || action == null)
            {
                return CheatAmounts.ZeroCount;
            }

            int appliedCount = CheatAmounts.ZeroCount;
            for (int staffIndex = 0; staffIndex < staff.Staff.Count; staffIndex++)
            {
                staff._staff staffMember = staff.Staff[staffIndex];
                if (staffMember == null)
                {
                    continue;
                }

                action(staffMember);
                appliedCount++;
            }

            return appliedCount;
        }

        private static bool RequireGameData()
        {
            if (GetMainScriptDataObject() != null)
            {
                return true;
            }

            NotifyWarning(CheatLocalizationKeys.NotificationGameUnavailable, CheatFallbackText.NotificationGameUnavailable);
            return false;
        }

        private static GameObject GetMainScriptDataObject()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return null;
            }

            mainScript main = camera.GetComponent<mainScript>();
            return main != null ? main.Data : null;
        }

        private static T GetDataComponent<T>() where T : Component
        {
            GameObject dataObject = GetMainScriptDataObject();
            return dataObject != null ? dataObject.GetComponent<T>() : null;
        }

        private static void RefreshIdolList()
        {
            data_girls dataGirls = GetDataComponent<data_girls>();
            if (dataGirls != null)
            {
                dataGirls.UpdateList(true);
            }
        }

        private static void RefreshStaffList()
        {
            staff staffData = GetDataComponent<staff>();
            if (staffData != null)
            {
                staffData.UpdateList(true);
            }
        }

        private static void RefreshResearchUi()
        {
            Research research = GetDataComponent<Research>();
            if (research != null)
            {
                research.RenderPoints();
            }
        }

        private static void RefreshSpecialEventsUi()
        {
            SpecialEvents_Manager manager = GetDataComponent<SpecialEvents_Manager>();
            if (manager != null)
            {
                manager.RenderTab();
            }
        }

        private static void NotifySuccess(
            string localizationKey,
            string fallback,
            NotificationManager._notification._type notificationType)
        {
            Notify(localizationKey, fallback, mainScript.green32, notificationType);
        }

        private static void NotifyWarning(string localizationKey, string fallback)
        {
            Notify(localizationKey, fallback, mainScript.red32, NotificationManager._notification._type.other);
        }

        private static void Notify(
            string localizationKey,
            string fallback,
            Color32 color,
            NotificationManager._notification._type notificationType)
        {
            string message = ModLocalization.Get(localizationKey, fallback);
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            try
            {
                NotificationManager.AddNotification(message, color, notificationType);
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(CheatLogMessages.NotificationFailedFormat, ex.Message));
            }
        }
    }
}
