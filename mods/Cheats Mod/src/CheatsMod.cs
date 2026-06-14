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
        internal const int MinimumRelationshipPointIncrease = 1;
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
        internal const string NotificationNoActiveIdols = "notification.no_active_idols";
        internal const string NotificationNoIdols = "notification.no_idols";
        internal const string NotificationNoStaff = "notification.no_staff";
        internal const string NotificationNoResearch = "notification.no_research";
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
        internal const string NotificationNoActiveIdols = "No active idols found.";
        internal const string NotificationNoIdols = "No idols found.";
        internal const string NotificationNoStaff = "No staff found.";
        internal const string NotificationNoResearch = "Research categories are not available yet.";
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
