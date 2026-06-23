using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AssitantManagerMod
{
    internal static class AssistantManagerConstants
    {
        internal const staff._type AssistantManagerStaffType = (staff._type)12010;
        internal const agency._type AssistantManagerOfficeRoomType = (agency._type)12011;
        internal const staff._type AssistantManagerStaffType2 = (staff._type)12012;
        
        internal const int MaximumAssistantManagersPerAgency = 2;
        internal const int AssistantManagerOfficeRoomCost = 500000;
        internal const int AssistantManagerOfficeRoomSpace = 1;
        
        // Type 1 (Production Focused)
        internal const int AssistantManagerNoviceProductionLevel = 2;
        internal const int AssistantManagerProfessionalProductionLevel = 4;
        internal const int AssistantManagerExpertProductionLevel = 6;
        internal const int AssistantManagerNoviceInfluenceLevel = 0;
        internal const int AssistantManagerProfessionalInfluenceLevel = 1;
        internal const int AssistantManagerExpertInfluenceLevel = 2;

        // Type 2 (Influence Focused)
        internal const int AssistantManagerType2NoviceProductionLevel = 0;
        internal const int AssistantManagerType2ProfessionalProductionLevel = 1;
        internal const int AssistantManagerType2ExpertProductionLevel = 2;
        internal const int AssistantManagerType2NoviceInfluenceLevel = 2;
        internal const int AssistantManagerType2ProfessionalInfluenceLevel = 4;
        internal const int AssistantManagerType2ExpertInfluenceLevel = 6;

        internal const string AssistantManagerOfficeBuildButtonObjectName = "AssistantManagerOffice_BuildRoomButton";
        internal const string AssistantManagerOfficeBuildRowObjectName = "AssistantManagerOffice_BuildMenuRow";
        internal const string AssistantManagerOfficeBuildHeaderObjectName = "AssistantManagerOffice_BuildMenuHeader";
        internal const string AssistantManagerHireButtonObjectName = "AssistantManager_StaffHireButton";
        internal const string MalePlayerPortraitFolderName = "Player_Male";
        internal const string FemalePlayerPortraitFolderName = "Player_Female";
        internal const string PortraitTexturesDirectoryName = "Textures";
        internal const string MalePortraitFileName = "AssistantManager_Male.png";
        internal const string FemalePortraitFileName = "AssistantManager_Female.png";
        internal const string MaleProPortraitFileName = "AssistantManager_Male_Pro.png";
        internal const string FemaleProPortraitFileName = "AssistantManager_Female_Pro.png";

        internal const string KeyAssistantManagerTitle = "staff.assistant_manager.title";
        internal const string KeyAssistantManagerTooltip = "staff.assistant_manager.tooltip";
        internal const string KeyAssistantManagerLimitReached = "staff.assistant_manager.limit_reached";
        internal const string KeyAssistantManagerOfficeTitle = "room.assistant_manager_office.title";
        internal const string KeyAssistantManagerOfficeRequires = "room.assistant_manager_office.requires";
        internal const string KeyAssistantManagerOfficeLimit = "room.assistant_manager_office.limit";
        internal const string KeyAssistantManagerOfficeNoAvailableLoanRoom = "room.assistant_manager_office.no_available_loan_room";
    }

    internal static class AssistantManagerText
    {
        internal static string AssistantManagerTitle
        {
            get
            {
                return ModLocalization.Get(AssistantManagerConstants.KeyAssistantManagerTitle, "Assistant Manager");
            }
        }

        internal static string AssistantManagerTooltip
        {
            get
            {
                return ModLocalization.Get(
                    AssistantManagerConstants.KeyAssistantManagerTooltip,
                    "Can staff Assistant Manager Offices.");
            }
        }

        internal static string AssistantManagerLimitReached
        {
            get
            {
                return ModLocalization.Get(
                    AssistantManagerConstants.KeyAssistantManagerLimitReached,
                    "Assistant Manager limit reached.");
            }
        }

        internal static string AssistantManagerOfficeTitle
        {
            get
            {
                return ModLocalization.Get(
                    AssistantManagerConstants.KeyAssistantManagerOfficeTitle,
                    "Assistant Manager's Office");
            }
        }

        internal static string AssistantManagerOfficeRequires
        {
            get
            {
                return ModLocalization.Get(
                    AssistantManagerConstants.KeyAssistantManagerOfficeRequires,
                    "Requires: Assistant Manager staff");
            }
        }

        internal static string AssistantManagerOfficeLimit
        {
            get
            {
                return ModLocalization.Get(
                    AssistantManagerConstants.KeyAssistantManagerOfficeLimit,
                    "Maximum per agency: 2");
            }
        }

        internal static string NoAvailableLoanRoom
        {
            get
            {
                return ModLocalization.Get(
                    AssistantManagerConstants.KeyAssistantManagerOfficeNoAvailableLoanRoom,
                    "Requires an available Manager Office or Assistant Manager Office.");
            }
        }
    }

    internal static class AssistantManagerDateTracking
    {
        private const double DateCooldownDays = 7.0;
        private static Dictionary<int, Dictionary<int, DateTime>> roomGirlCooldowns = new Dictionary<int, Dictionary<int, DateTime>>();

        internal struct CooldownEntry
        {
            internal int RoomId;
            internal int GirlId;
            internal DateTime Until;
        }

        internal static void Reset()
        {
            roomGirlCooldowns.Clear();
        }

        internal static int GetCooldownDays(int roomId, int girlId)
        {
            if (roomGirlCooldowns.TryGetValue(roomId, out var girlCooldowns))
            {
                if (girlCooldowns.TryGetValue(girlId, out DateTime cooldownDate))
                {
                    if (cooldownDate <= staticVars.dateTime)
                    {
                        girlCooldowns.Remove(girlId);
                        if (girlCooldowns.Count == 0)
                        {
                            roomGirlCooldowns.Remove(roomId);
                        }
                        return 0;
                    }

                    int daysLeft = Mathf.CeilToInt((float)(cooldownDate - staticVars.dateTime).TotalDays);
                    return Mathf.Max(0, daysLeft);
                }
            }
            return 0;
        }

        internal static void AddDate(agency._room room, data_girls.girls girl)
        {
            if (room == null || girl == null)
            {
                return;
            }

            int cooldownDays = DateCooldownDaysInt;
            if (room.staffer != null)
            {
                staff._staff._skill influence = room.staffer.GetSkill(staff._skill_type.influence);
                if (influence != null)
                {
                    cooldownDays = Mathf.Max(0, cooldownDays - influence.GetLevel() / 2);
                }
            }

            int roomId = room.id;
            int girlId = girl.id;
            if (!roomGirlCooldowns.ContainsKey(roomId))
            {
                roomGirlCooldowns[roomId] = new Dictionary<int, DateTime>();
            }
            roomGirlCooldowns[roomId][girlId] = staticVars.dateTime.AddDays(cooldownDays);
            AssistantManagerStatePersistence.MarkDirty();
        }

        internal static List<CooldownEntry> ExportEntries()
        {
            List<CooldownEntry> entries = new List<CooldownEntry>();
            foreach (KeyValuePair<int, Dictionary<int, DateTime>> roomEntry in roomGirlCooldowns)
            {
                foreach (KeyValuePair<int, DateTime> girlEntry in roomEntry.Value)
                {
                    if (girlEntry.Value > staticVars.dateTime)
                    {
                        entries.Add(new CooldownEntry
                        {
                            RoomId = roomEntry.Key,
                            GirlId = girlEntry.Key,
                            Until = girlEntry.Value
                        });
                    }
                }
            }
            return entries;
        }

        internal static void ImportEntries(List<CooldownEntry> entries)
        {
            roomGirlCooldowns.Clear();
            if (entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                CooldownEntry entry = entries[i];
                if (entry.Until <= staticVars.dateTime)
                {
                    continue;
                }

                Dictionary<int, DateTime> girlCooldowns;
                if (!roomGirlCooldowns.TryGetValue(entry.RoomId, out girlCooldowns))
                {
                    girlCooldowns = new Dictionary<int, DateTime>();
                    roomGirlCooldowns[entry.RoomId] = girlCooldowns;
                }
                girlCooldowns[entry.GirlId] = entry.Until;
            }
        }

        private static int DateCooldownDaysInt
        {
            get { return Mathf.RoundToInt((float)DateCooldownDays); }
        }
    }

    /// <summary>
    /// The base game stores regional and nationwide audition cooldowns in two global fields and
    /// hands every office the same mutable Auditions.data instance.  That is safe for one
    /// producer office, but not for multiple Assistant Manager offices.  Keep the cooldown and
    /// active audition data scoped to the room that owns the work.
    /// </summary>
    internal static class AssistantManagerAuditionTracking
    {
        private static readonly Dictionary<int, Dictionary<Auditions.type, DateTime>> cooldownsByRoom =
            new Dictionary<int, Dictionary<Auditions.type, DateTime>>();

        private static readonly Dictionary<Auditions.data, int> ownerRoomByAudition =
            new Dictionary<Auditions.data, int>();

        internal struct CooldownEntry
        {
            internal int RoomId;
            internal Auditions.type Type;
            internal DateTime LastAudition;
        }

        internal struct OwnerEntry
        {
            internal int RoomId;
            internal Auditions.type Type;
            internal float Progress;
        }

        internal struct GenerateState
        {
            internal bool IsTracked;
            internal bool ShouldRecordCooldown;
            internal int RoomId;
            internal Auditions.type Type;
            internal Auditions.data Audition;
            internal DateTime RegionalDate;
            internal DateTime NationwideDate;
        }

        internal static void Reset()
        {
            cooldownsByRoom.Clear();
            ownerRoomByAudition.Clear();
        }

        internal static bool CanProduce(agency._room room, Auditions.type type)
        {
            return type == Auditions.type.local || GetDaysTillCanProduce(room, type) <= 0;
        }

        internal static int GetDaysTillCanProduce(agency._room room, Auditions.type type)
        {
            if (room == null || type == Auditions.type.local)
            {
                return 0;
            }

            Dictionary<Auditions.type, DateTime> cooldowns;
            DateTime lastAudition;
            if (!cooldownsByRoom.TryGetValue(room.id, out cooldowns) ||
                !cooldowns.TryGetValue(type, out lastAudition))
            {
                return 0;
            }

            int cooldownDays = GetCooldownLengthDays(type);
            if (cooldownDays <= 0)
            {
                return 0;
            }
            return Mathf.Max(0, cooldownDays - (staticVars.dateTime - lastAudition).Days);
        }

        internal static bool IsOnCooldown(agency._room room)
        {
            return !CanProduce(room, Auditions.type.regional) ||
                   !CanProduce(room, Auditions.type.nationwide);
        }

        internal static int GetCooldownCost(agency._room room, CM_Player_Audition_Cooldown._type type)
        {
            int regionalDays = GetDaysTillCanProduce(room, Auditions.type.regional);
            int nationwideDays = GetDaysTillCanProduce(room, Auditions.type.nationwide);
            if (type == CM_Player_Audition_Cooldown._type.money)
            {
                return 15000 * regionalDays + 50000 * nationwideDays;
            }

            return 20 * regionalDays + 30 * nationwideDays;
        }

        internal static bool EnoughResourcesForCooldown(agency._room room, CM_Player_Audition_Cooldown._type type)
        {
            int cost = GetCooldownCost(room, type);
            if (type == CM_Player_Audition_Cooldown._type.money)
            {
                return resources.Money() >= (long)cost;
            }

            return Research.GetCategory(Research.type.player).GetPoints() >= (long)cost;
        }

        internal static bool CanResetCooldown(agency._room room, CM_Player_Audition_Cooldown._type type)
        {
            return IsOnCooldown(room) && EnoughResourcesForCooldown(room, type);
        }

        internal static void ResetCooldown(agency._room room, CM_Player_Audition_Cooldown._type type)
        {
            if (!CanResetCooldown(room, type))
            {
                return;
            }

            int cost = GetCooldownCost(room, type);
            if (type == CM_Player_Audition_Cooldown._type.money)
            {
                resources.Add(resources.type.money, -(long)cost);
            }
            else
            {
                Research.GetCategory(Research.type.player).AddPoints(-(float)(cost * 10));
            }

            cooldownsByRoom.Remove(room.id);
            AssistantManagerStatePersistence.MarkDirty();
        }

        internal static Auditions.data CreateRoomAudition(agency._room room, Auditions.data template)
        {
            if (room == null || template == null)
            {
                return null;
            }

            Auditions.data audition = new Auditions.data
            {
                Type = template.Type,
                Title = template.Title,
                Progress = 0f,
                Girls = new List<Auditions.data._girl>()
            };
            ownerRoomByAudition[audition] = room.id;
            AssistantManagerStatePersistence.MarkDirty();
            return audition;
        }

        internal static bool IsRoomAudition(Auditions.data audition)
        {
            return audition != null && ownerRoomByAudition.ContainsKey(audition);
        }

        internal static void BeginGenerate(Auditions.data audition, out GenerateState state)
        {
            state = new GenerateState
            {
                IsTracked = false,
                ShouldRecordCooldown = false,
                RoomId = -1,
                Type = Auditions.type.local,
                Audition = audition,
                RegionalDate = Auditions.Regional_Date,
                NationwideDate = Auditions.Nationwide_Date
            };

            if (audition == null)
            {
                return;
            }

            int roomId;
            if (!ownerRoomByAudition.TryGetValue(audition, out roomId))
            {
                return;
            }

            state.IsTracked = true;
            state.RoomId = roomId;
            state.Type = audition.Type;
            state.ShouldRecordCooldown = CanGenerateAudition();
        }

        internal static void EndGenerate(GenerateState state)
        {
            if (!state.IsTracked)
            {
                return;
            }

            if (state.ShouldRecordCooldown &&
                (state.Type == Auditions.type.regional || state.Type == Auditions.type.nationwide))
            {
                Dictionary<Auditions.type, DateTime> cooldowns;
                if (!cooldownsByRoom.TryGetValue(state.RoomId, out cooldowns))
                {
                    cooldowns = new Dictionary<Auditions.type, DateTime>();
                    cooldownsByRoom[state.RoomId] = cooldowns;
                }
                cooldowns[state.Type] = staticVars.dateTime;
            }

            // Prevent the base global fields from leaking one manager's cooldown to another.
            Auditions.Regional_Date = state.RegionalDate;
            Auditions.Nationwide_Date = state.NationwideDate;
            ForgetOwner(state.Audition);
        }

        internal static List<CooldownEntry> ExportCooldownEntries()
        {
            List<CooldownEntry> entries = new List<CooldownEntry>();
            foreach (KeyValuePair<int, Dictionary<Auditions.type, DateTime>> roomEntry in cooldownsByRoom)
            {
                foreach (KeyValuePair<Auditions.type, DateTime> cooldownEntry in roomEntry.Value)
                {
                    if (cooldownEntry.Value <= staticVars.dateTime)
                    {
                        continue;
                    }
                    entries.Add(new CooldownEntry
                    {
                        RoomId = roomEntry.Key,
                        Type = cooldownEntry.Key,
                        LastAudition = cooldownEntry.Value
                    });
                }
            }
            return entries;
        }

        internal static void ForgetOwner(Auditions.data audition)
        {
            if (audition != null && ownerRoomByAudition.Remove(audition))
            {
                AssistantManagerStatePersistence.MarkDirty();
            }
        }

        internal static List<OwnerEntry> ExportOwnerEntries()
        {
            List<OwnerEntry> entries = new List<OwnerEntry>();
            foreach (KeyValuePair<Auditions.data, int> entry in ownerRoomByAudition)
            {
                if (entry.Key == null)
                {
                    continue;
                }
                entries.Add(new OwnerEntry
                {
                    RoomId = entry.Value,
                    Type = entry.Key.Type,
                    Progress = entry.Key.Progress
                });
            }
            return entries;
        }

        internal static void ImportCooldownEntries(List<CooldownEntry> entries)
        {
            cooldownsByRoom.Clear();
            if (entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                CooldownEntry entry = entries[i];
                if (GetCooldownLengthDays(entry.Type) <= 0 ||
                    entry.LastAudition.AddDays(GetCooldownLengthDays(entry.Type)) <= staticVars.dateTime)
                {
                    continue;
                }

                Dictionary<Auditions.type, DateTime> roomCooldowns;
                if (!cooldownsByRoom.TryGetValue(entry.RoomId, out roomCooldowns))
                {
                    roomCooldowns = new Dictionary<Auditions.type, DateTime>();
                    cooldownsByRoom[entry.RoomId] = roomCooldowns;
                }
                roomCooldowns[entry.Type] = entry.LastAudition;
            }
        }

        internal static void RestoreOwners(agency agencyInstance, List<OwnerEntry> entries)
        {
            ownerRoomByAudition.Clear();
            if (agencyInstance == null)
            {
                return;
            }

            List<agency._room> rooms = agencyInstance.allRooms(true, true);
            for (int i = 0; i < rooms.Count; i++)
            {
                agency._room room = rooms[i];
                if (!AssistantManagerRules.IsManagerOffice(room) || room.auditionData == null)
                {
                    continue;
                }

                bool hasPersistedOwner = false;
                if (entries != null)
                {
                    for (int j = 0; j < entries.Count; j++)
                    {
                        if (entries[j].RoomId == room.id && entries[j].Type == room.auditionData.Type)
                        {
                            hasPersistedOwner = true;
                            room.auditionData.Progress = entries[j].Progress;
                            break;
                        }
                    }
                }

                if (hasPersistedOwner ||
                    room.status == agency._room._status.audition ||
                    room.status == agency._room._status.waiting)
                {
                    ownerRoomByAudition[room.auditionData] = room.id;
                }
            }
        }

        private static int GetCooldownLengthDays(Auditions.type type)
        {
            if (type == Auditions.type.regional)
            {
                return 30;
            }
            if (type == Auditions.type.nationwide)
            {
                return 90;
            }
            return 0;
        }

        private static bool CanGenerateAudition()
        {
            try
            {
                return tasks.Story_Data == null || !tasks.Story_Data.Scandal_Auditions_No_More;
            }
            catch
            {
                return false;
            }
        }

        internal static agency._room GetCurrentManagerOffice()
        {
            try
            {
                ContextMenuController menu = Camera.main.GetComponent<mainScript>().Data.GetComponent<ContextMenuController>();
                return menu != null && menu.open_mainMenu != null && AssistantManagerRules.IsManagerOffice(menu.room)
                    ? menu.room
                    : null;
            }
            catch
            {
                return null;
            }
        }

        internal static bool IsAuditionPopupBusy()
        {
            try
            {
                PopupManager popupManager = Camera.main.GetComponent<mainScript>().Data.GetComponent<PopupManager>();
                PopupManager._popup auditionPopup = popupManager.GetByType(PopupManager._type.audition);
                return (auditionPopup != null && auditionPopup.open) || popupManager.queue.Contains(PopupManager._type.audition);
            }
            catch
            {
                return true;
            }
        }
    }

    internal sealed class AssistantManagerDataCoreSession
    {
        internal AssistantManagerDataCoreSession(object rawSession)
        {
            RawSession = rawSession;
        }

        internal object RawSession { get; private set; }
    }

    /// <summary>
    /// Optional late-bound bridge.  Assistant Manager must remain loadable when IM Data Core is
    /// not installed, so it cannot take a CLR assembly reference to the core mod.
    /// </summary>
    internal static class AssistantManagerDataCoreApi
    {
        private const string AssemblyName = "com.cosmo.imdatacore";
        private const string ApiTypeName = "IMDataCore.IMDataCoreApi";
        private const string SessionTypeName = "IMDataCore.IMDataCoreSession";

        private static readonly object Sync = new object();
        private static Type apiType;
        private static Type sessionType;
        private static MethodInfo isReadyMethod;
        private static MethodInfo registerNamespaceMethod;
        private static MethodInfo getCustomJsonMethod;
        private static MethodInfo setCustomJsonMethod;
        private static AssistantManagerDataCoreSession session;
        private static DateTime nextResolveAttemptUtc = DateTime.MinValue;

        internal static bool TryGetSession(out AssistantManagerDataCoreSession result)
        {
            result = null;
            lock (Sync)
            {
                if (session != null && session.RawSession != null)
                {
                    result = session;
                    return true;
                }

                if (!TryEnsureReady())
                {
                    return false;
                }

                object[] args = new object[] { "com.cosmo.assistantmanager", null, string.Empty };
                object invocationResult;
                if (!TryInvoke(registerNamespaceMethod, args, out invocationResult) ||
                    !(invocationResult is bool) || !(bool)invocationResult || args[1] == null)
                {
                    return false;
                }

                session = new AssistantManagerDataCoreSession(args[1]);
                result = session;
                return true;
            }
        }

        internal static bool TryGetCustomJson(AssistantManagerDataCoreSession activeSession, string key, out string json)
        {
            json = string.Empty;
            if (activeSession == null || activeSession.RawSession == null || !TryEnsureReady())
            {
                return false;
            }

            object[] args = new object[] { activeSession.RawSession, key, null, string.Empty };
            object invocationResult;
            if (!TryInvoke(getCustomJsonMethod, args, out invocationResult) ||
                !(invocationResult is bool) || !(bool)invocationResult)
            {
                return false;
            }

            json = args[2] as string ?? string.Empty;
            return true;
        }

        internal static bool TrySetCustomJson(AssistantManagerDataCoreSession activeSession, string key, string json)
        {
            if (activeSession == null || activeSession.RawSession == null || !TryEnsureReady())
            {
                return false;
            }

            object[] args = new object[] { activeSession.RawSession, key, json ?? "{}", string.Empty };
            object invocationResult;
            return TryInvoke(setCustomJsonMethod, args, out invocationResult) &&
                   invocationResult is bool &&
                   (bool)invocationResult;
        }

        private static bool TryEnsureReady()
        {
            lock (Sync)
            {
                if (apiType == null || isReadyMethod == null || registerNamespaceMethod == null ||
                    getCustomJsonMethod == null || setCustomJsonMethod == null)
                {
                    if (DateTime.UtcNow < nextResolveAttemptUtc || !TryResolve())
                    {
                        return false;
                    }
                }

                object result;
                return TryInvoke(isReadyMethod, null, out result) && result is bool && (bool)result;
            }
        }

        private static bool TryResolve()
        {
            Assembly coreAssembly = null;
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < loadedAssemblies.Length; i++)
            {
                Assembly assembly = loadedAssemblies[i];
                if (assembly != null && assembly.GetName() != null &&
                    string.Equals(assembly.GetName().Name, AssemblyName, StringComparison.OrdinalIgnoreCase))
                {
                    coreAssembly = assembly;
                    break;
                }
            }

            if (coreAssembly == null)
            {
                try
                {
                    coreAssembly = Assembly.Load(AssemblyName);
                }
                catch
                {
                    nextResolveAttemptUtc = DateTime.UtcNow.AddSeconds(5d);
                    return false;
                }
            }

            Type resolvedApiType = coreAssembly.GetType(ApiTypeName, false);
            Type resolvedSessionType = coreAssembly.GetType(SessionTypeName, false);
            if (resolvedApiType == null || resolvedSessionType == null)
            {
                nextResolveAttemptUtc = DateTime.UtcNow.AddSeconds(5d);
                return false;
            }

            MethodInfo resolvedIsReady = FindStaticMethod(resolvedApiType, "IsReady", 0);
            MethodInfo resolvedRegister = FindStaticMethod(resolvedApiType, "TryRegisterNamespace", 3);
            MethodInfo resolvedGet = FindStaticMethod(resolvedApiType, "TryGetCustomJson", 4);
            MethodInfo resolvedSet = FindStaticMethod(resolvedApiType, "TrySetCustomJson", 4);
            if (resolvedIsReady == null || resolvedRegister == null || resolvedGet == null || resolvedSet == null)
            {
                nextResolveAttemptUtc = DateTime.UtcNow.AddSeconds(5d);
                return false;
            }

            apiType = resolvedApiType;
            sessionType = resolvedSessionType;
            isReadyMethod = resolvedIsReady;
            registerNamespaceMethod = resolvedRegister;
            getCustomJsonMethod = resolvedGet;
            setCustomJsonMethod = resolvedSet;
            nextResolveAttemptUtc = DateTime.MinValue;
            return true;
        }

        private static MethodInfo FindStaticMethod(Type type, string name, int parameterCount)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == name && methods[i].GetParameters().Length == parameterCount)
                {
                    return methods[i];
                }
            }
            return null;
        }

        private static bool TryInvoke(MethodInfo method, object[] arguments, out object result)
        {
            result = null;
            try
            {
                result = method.Invoke(null, arguments);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    internal static class AssistantManagerStatePersistence
    {
        private const string StateKey = "assistant_manager_office_state_v1";
        private const string DateCooldownsKey = "date_cooldowns";
        private const string AuditionCooldownsKey = "audition_cooldowns";
        private const string AuditionOwnersKey = "audition_owners";

        private static bool restoreRequested;
        private static bool dirty;
        private static bool applyingRestore;

        internal static void RequestRestore()
        {
            restoreRequested = true;
            dirty = false;
        }

        internal static void MarkDirty()
        {
            if (applyingRestore)
            {
                return;
            }

            dirty = true;
        }

        internal static void Synchronize(agency agencyInstance)
        {
            if (restoreRequested)
            {
                TryRestore(agencyInstance);
            }

            if (dirty)
            {
                TryPersist();
            }
        }

        private static void TryRestore(agency agencyInstance)
        {
            AssistantManagerDataCoreSession session;
            if (!AssistantManagerDataCoreApi.TryGetSession(out session))
            {
                return;
            }

            string json;
            bool foundState = AssistantManagerDataCoreApi.TryGetCustomJson(session, StateKey, out json);
            applyingRestore = true;
            try
            {
                if (foundState && !string.IsNullOrEmpty(json))
                {
                    JSONNode root = JSON.Parse(json);
                    AssistantManagerDateTracking.ImportEntries(ReadDateCooldowns(root));
                    AssistantManagerAuditionTracking.ImportCooldownEntries(ReadAuditionCooldowns(root));
                    AssistantManagerAuditionTracking.RestoreOwners(agencyInstance, ReadAuditionOwners(root));
                }
                else
                {
                    AssistantManagerDateTracking.ImportEntries(null);
                    AssistantManagerAuditionTracking.ImportCooldownEntries(null);
                    AssistantManagerAuditionTracking.RestoreOwners(agencyInstance, null);
                }
            }
            catch
            {
                // Keep the in-memory state empty when a future schema or corrupt record cannot be read.
            }
            finally
            {
                applyingRestore = false;
            }

            restoreRequested = false;
            dirty = false;
        }

        private static void TryPersist()
        {
            AssistantManagerDataCoreSession session;
            if (!AssistantManagerDataCoreApi.TryGetSession(out session))
            {
                return;
            }

            JSONClass root = new JSONClass();
            root["version"].AsInt = 1;
            root[DateCooldownsKey] = WriteDateCooldowns();
            root[AuditionCooldownsKey] = WriteAuditionCooldowns();
            root[AuditionOwnersKey] = WriteAuditionOwners();
            if (AssistantManagerDataCoreApi.TrySetCustomJson(session, StateKey, root.ToString()))
            {
                dirty = false;
            }
        }

        private static JSONArray WriteDateCooldowns()
        {
            JSONArray array = new JSONArray();
            List<AssistantManagerDateTracking.CooldownEntry> entries = AssistantManagerDateTracking.ExportEntries();
            for (int i = 0; i < entries.Count; i++)
            {
                JSONClass entry = new JSONClass();
                entry["room_id"].AsInt = entries[i].RoomId;
                entry["girl_id"].AsInt = entries[i].GirlId;
                entry["until"] = ExtensionMethods.ToDataString(entries[i].Until);
                array.Add(entry);
            }
            return array;
        }

        private static JSONArray WriteAuditionCooldowns()
        {
            JSONArray array = new JSONArray();
            List<AssistantManagerAuditionTracking.CooldownEntry> entries = AssistantManagerAuditionTracking.ExportCooldownEntries();
            for (int i = 0; i < entries.Count; i++)
            {
                JSONClass entry = new JSONClass();
                entry["room_id"].AsInt = entries[i].RoomId;
                entry["type"].AsInt = (int)entries[i].Type;
                entry["last_audition"] = ExtensionMethods.ToDataString(entries[i].LastAudition);
                array.Add(entry);
            }
            return array;
        }

        private static JSONArray WriteAuditionOwners()
        {
            JSONArray array = new JSONArray();
            List<AssistantManagerAuditionTracking.OwnerEntry> entries = AssistantManagerAuditionTracking.ExportOwnerEntries();
            for (int i = 0; i < entries.Count; i++)
            {
                JSONClass entry = new JSONClass();
                entry["room_id"].AsInt = entries[i].RoomId;
                entry["type"].AsInt = (int)entries[i].Type;
                entry["progress"].AsFloat = entries[i].Progress;
                array.Add(entry);
            }
            return array;
        }

        private static List<AssistantManagerDateTracking.CooldownEntry> ReadDateCooldowns(JSONNode root)
        {
            List<AssistantManagerDateTracking.CooldownEntry> entries = new List<AssistantManagerDateTracking.CooldownEntry>();
            if (root == null || root[DateCooldownsKey] == null)
            {
                return entries;
            }

            foreach (JSONNode node in root[DateCooldownsKey].AsArray)
            {
                DateTime until;
                if (node == null || !TryParseDate(node["until"], out until))
                {
                    continue;
                }
                entries.Add(new AssistantManagerDateTracking.CooldownEntry
                {
                    RoomId = node["room_id"].AsInt,
                    GirlId = node["girl_id"].AsInt,
                    Until = until
                });
            }
            return entries;
        }

        private static List<AssistantManagerAuditionTracking.CooldownEntry> ReadAuditionCooldowns(JSONNode root)
        {
            List<AssistantManagerAuditionTracking.CooldownEntry> entries = new List<AssistantManagerAuditionTracking.CooldownEntry>();
            if (root == null || root[AuditionCooldownsKey] == null)
            {
                return entries;
            }

            foreach (JSONNode node in root[AuditionCooldownsKey].AsArray)
            {
                DateTime lastAudition;
                if (node == null || !TryParseDate(node["last_audition"], out lastAudition))
                {
                    continue;
                }
                entries.Add(new AssistantManagerAuditionTracking.CooldownEntry
                {
                    RoomId = node["room_id"].AsInt,
                    Type = (Auditions.type)node["type"].AsInt,
                    LastAudition = lastAudition
                });
            }
            return entries;
        }

        private static List<AssistantManagerAuditionTracking.OwnerEntry> ReadAuditionOwners(JSONNode root)
        {
            List<AssistantManagerAuditionTracking.OwnerEntry> entries = new List<AssistantManagerAuditionTracking.OwnerEntry>();
            if (root == null || root[AuditionOwnersKey] == null)
            {
                return entries;
            }

            foreach (JSONNode node in root[AuditionOwnersKey].AsArray)
            {
                if (node == null)
                {
                    continue;
                }
                entries.Add(new AssistantManagerAuditionTracking.OwnerEntry
                {
                    RoomId = node["room_id"].AsInt,
                    Type = (Auditions.type)node["type"].AsInt,
                    Progress = node["progress"].AsFloat
                });
            }
            return entries;
        }

        private static bool TryParseDate(JSONNode node, out DateTime value)
        {
            value = DateTime.MinValue;
            try
            {
                string serialized = node == null ? string.Empty : node.Value;
                if (string.IsNullOrEmpty(serialized))
                {
                    return false;
                }
                value = ExtensionMethods.ToDateTime(serialized);
                return value != DateTime.MinValue;
            }
            catch
            {
                return false;
            }
        }
    }

    internal sealed class AssistantManagerBuildMenuRowMarker : MonoBehaviour
    {
    }

    internal static class AssistantManagerRules
    {
        private const float ManagerOfficeSecondaryTaskPointMultiplier = 0.2f;

        private static readonly MethodInfo BuildRoomButtonActivateMethod =
            AccessTools.Method(typeof(BuildRoomButton), "Activate");

        private static readonly MethodInfo AddStaffFloatMethod =
            AccessTools.Method(typeof(agency._room), "AddStaffFloat");

        private static readonly MethodInfo OnTaskCompleteMethod =
            AccessTools.Method(typeof(agency._room), "OnTaskComplete");

        private static Sprite customMaleSprite = null;
        private static Sprite customFemaleSprite = null;
        private static Sprite customMaleProSprite = null;
        private static Sprite customFemaleProSprite = null;
        private static bool attemptedLoadMale = false;
        private static bool attemptedLoadFemale = false;
        private static bool attemptedLoadMalePro = false;
        private static bool attemptedLoadFemalePro = false;
        private static bool isEnsuringBuildMenuButton = false;

        internal static bool TryApplyAssistantManagerPortrait(staff._type staffType, Image target, bool pro, Action callback)
        {
            if (!IsAssistantManagerStaffType(staffType) || target == null)
            {
                return false;
            }

            Sprite portrait = GetCustomPortrait(IsPlayerMale(), pro);
            if (portrait == null)
            {
                return false;
            }

            target.overrideSprite = null;
            target.sprite = portrait;
            target.preserveAspect = true;
            if (callback != null)
            {
                callback();
            }

            return true;
        }

        private static Sprite GetCustomPortrait(bool isMale, bool pro)
        {
            if (pro)
            {
                if (isMale)
                {
                    if (!attemptedLoadMalePro)
                    {
                        attemptedLoadMalePro = true;
                        customMaleProSprite = LoadCustomPortrait(true, true);
                    }

                    return customMaleProSprite ?? GetCustomPortrait(true, false);
                }

                if (!attemptedLoadFemalePro)
                {
                    attemptedLoadFemalePro = true;
                    customFemaleProSprite = LoadCustomPortrait(false, true);
                }

                return customFemaleProSprite ?? GetCustomPortrait(false, false);
            }

            if (isMale)
            {
                if (attemptedLoadMale)
                {
                    return customMaleSprite;
                }
                attemptedLoadMale = true;
                customMaleSprite = LoadCustomPortrait(true, false);
                return customMaleSprite;
            }

            if (attemptedLoadFemale)
            {
                return customFemaleSprite;
            }

            attemptedLoadFemale = true;
            customFemaleSprite = LoadCustomPortrait(false, false);
            return customFemaleSprite;
        }

        private static Sprite LoadCustomPortrait(bool isMale, bool pro)
        {
            try
            {
                string fileName;
                if (pro)
                {
                    fileName = isMale
                        ? AssistantManagerConstants.MaleProPortraitFileName
                        : AssistantManagerConstants.FemaleProPortraitFileName;
                }
                else
                {
                    fileName = isMale
                        ? AssistantManagerConstants.MalePortraitFileName
                        : AssistantManagerConstants.FemalePortraitFileName;
                }
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                string assemblyDir = string.IsNullOrEmpty(assemblyPath)
                    ? string.Empty
                    : System.IO.Path.GetDirectoryName(assemblyPath);
                string imagePath = string.IsNullOrEmpty(assemblyDir)
                    ? string.Empty
                    : System.IO.Path.Combine(
                        assemblyDir,
                        AssistantManagerConstants.PortraitTexturesDirectoryName,
                        fileName);
                if (string.IsNullOrEmpty(imagePath) || !System.IO.File.Exists(imagePath))
                {
                    return null;
                }

                byte[] fileData = System.IO.File.ReadAllBytes(imagePath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(tex, fileData))
                {
                    UnityEngine.Object.Destroy(tex);
                    return null;
                }

                tex.name = "AssistantManagerPortrait_" + (isMale ? "Male" : "Female") + (pro ? "_Pro" : string.Empty);
                tex.wrapMode = TextureWrapMode.Clamp;
                Sprite sprite = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect);

                return sprite;
            }
            catch
            {
                return null;
            }
        }

        internal static bool IsAssistantManagerStaffType(staff._type staffType)
        {
            return staffType == AssistantManagerConstants.AssistantManagerStaffType ||
                   staffType == AssistantManagerConstants.AssistantManagerStaffType2;
        }

        internal static bool IsAssistantManager(staff._staff staffMember)
        {
            return staffMember != null && IsAssistantManagerStaffType(staffMember.type);
        }

        internal static bool IsAssistantManagerOfficeRoomType(agency._type roomType)
        {
            return roomType == AssistantManagerConstants.AssistantManagerOfficeRoomType;
        }

        internal static bool IsAssistantManagerOffice(agency._room room)
        {
            return room != null && IsAssistantManagerOfficeRoomType(room.type);
        }

        internal static bool IsManagerOfficeRoomType(agency._type roomType)
        {
            return roomType == agency._type.yourOffice || IsAssistantManagerOfficeRoomType(roomType);
        }

        internal static bool IsManagerOffice(agency._room room)
        {
            return room != null && IsManagerOfficeRoomType(room.type);
        }

        internal static int CountAssistantManagers()
        {
            int count = 0;
            if (staff.Staff == null)
            {
                return count;
            }

            for (int i = 0; i < staff.Staff.Count; i++)
            {
                if (IsAssistantManager(staff.Staff[i]))
                {
                    count++;
                }
            }

            return count;
        }

        internal static bool CanHireAssistantManager()
        {
            return CountAssistantManagers() < AssistantManagerConstants.MaximumAssistantManagersPerAgency;
        }

        internal static int CountAssistantManagerOffices(agency agencyInstance)
        {
            int count = 0;
            if (agencyInstance == null)
            {
                return count;
            }

            List<agency._room> rooms = agencyInstance.allRooms(true, true);
            for (int i = 0; i < rooms.Count; i++)
            {
                if (IsAssistantManagerOffice(rooms[i]))
                {
                    count++;
                }
            }

            return count;
        }

        internal static bool CanBuildAssistantManagerOffice(agency agencyInstance)
        {
            return CountAssistantManagerOffices(agencyInstance) < AssistantManagerConstants.MaximumAssistantManagersPerAgency;
        }

        internal static int GetAssistantManagerProductionSkillLevel(staff._type type, staff._expertise expertise)
        {
            if (type == AssistantManagerConstants.AssistantManagerStaffType2)
            {
                if (expertise == staff._expertise.professional) return AssistantManagerConstants.AssistantManagerType2ProfessionalProductionLevel;
                if (expertise == staff._expertise.expert) return AssistantManagerConstants.AssistantManagerType2ExpertProductionLevel;
                return AssistantManagerConstants.AssistantManagerType2NoviceProductionLevel;
            }

            if (expertise == staff._expertise.professional) return AssistantManagerConstants.AssistantManagerProfessionalProductionLevel;
            if (expertise == staff._expertise.expert) return AssistantManagerConstants.AssistantManagerExpertProductionLevel;
            return AssistantManagerConstants.AssistantManagerNoviceProductionLevel;
        }

        internal static int GetAssistantManagerInfluenceSkillLevel(staff._type type, staff._expertise expertise)
        {
            if (type == AssistantManagerConstants.AssistantManagerStaffType2)
            {
                if (expertise == staff._expertise.professional) return AssistantManagerConstants.AssistantManagerType2ProfessionalInfluenceLevel;
                if (expertise == staff._expertise.expert) return AssistantManagerConstants.AssistantManagerType2ExpertInfluenceLevel;
                return AssistantManagerConstants.AssistantManagerType2NoviceInfluenceLevel;
            }

            if (expertise == staff._expertise.professional) return AssistantManagerConstants.AssistantManagerProfessionalInfluenceLevel;
            if (expertise == staff._expertise.expert) return AssistantManagerConstants.AssistantManagerExpertInfluenceLevel;
            return AssistantManagerConstants.AssistantManagerNoviceInfluenceLevel;
        }

        internal static List<staff._staff._skill> CreateAssistantManagerSkills(staff._type type, staff._expertise expertise)
        {
            List<staff._staff._skill> skills = new List<staff._staff._skill>();
            skills.Add(new staff._staff._skill
            {
                skill_type = staff._skill_type.production,
                level = GetAssistantManagerProductionSkillLevel(type, expertise),
                primary = true
            });
            skills.Add(new staff._staff._skill
            {
                skill_type = staff._skill_type.influence,
                level = GetAssistantManagerInfluenceSkillLevel(type, expertise)
            });
            return skills;
        }

        internal static staff._staff GenerateAssistantManagerStaff(staff staffFactory, staff._type type, staff._expertise expertise)
        {
            if (staffFactory == null)
            {
                return null;
            }

            staff._staff generatedStaff = new staff._staff();
            generatedStaff.firstName = nameGenerator.firstName(!IsPlayerMale());
            generatedStaff.lastName = nameGenerator.lastName();
            generatedStaff.id = staff.GetNewStaffID();
            generatedStaff.type = type;
            generatedStaff.skills = staffFactory.GetSkillsByType(generatedStaff.type, expertise, false);
            generatedStaff.SetParentRefs();
            return generatedStaff;
        }

        internal static string GetPlayerPortraitFolderName()
        {
            try
            {
                if (staticVars.PlayerData != null && staticVars.PlayerData.IsFemale())
                {
                    return AssistantManagerConstants.FemalePlayerPortraitFolderName;
                }
            }
            catch
            {
            }

            return AssistantManagerConstants.MalePlayerPortraitFolderName;
        }

        internal static bool IsPlayerMale()
        {
            try
            {
                return staticVars.PlayerData != null && staticVars.PlayerData.IsMale();
            }
            catch
            {
                return false;
            }
        }

        internal static Animations._animationType GetPlayerOfficeAnimation()
        {
            try
            {
                if (staticVars.PlayerData != null && staticVars.PlayerData.IsMale())
                {
                    return Animations._animationType.player_office_work;
                }
            }
            catch
            {
            }

            return Animations._animationType.player_office_work_female;
        }

        internal static void RegisterAssistantManagerOfficePrefab(agency agencyInstance)
        {
            if (agencyInstance == null || agencyInstance.roomPrefabs == null)
            {
                return;
            }

            agency._roomPrefab managerOfficePrefab = null;
            for (int i = 0; i < agencyInstance.roomPrefabs.Length; i++)
            {
                agency._roomPrefab roomPrefab = agencyInstance.roomPrefabs[i];
                if (roomPrefab == null)
                {
                    continue;
                }

                if (roomPrefab.type == AssistantManagerConstants.AssistantManagerOfficeRoomType)
                {
                    return;
                }

                if (roomPrefab.type == agency._type.yourOffice)
                {
                    managerOfficePrefab = roomPrefab;
                }
            }

            if (managerOfficePrefab == null || managerOfficePrefab.obj == null)
            {
                return;
            }

            agency._roomPrefab assistantOfficePrefab = new agency._roomPrefab
            {
                type = AssistantManagerConstants.AssistantManagerOfficeRoomType,
                obj = managerOfficePrefab.obj,
                RoomSprites = new List<agency._roomPrefab._roomSprite>()
            };

            if (managerOfficePrefab.RoomSprites != null)
            {
                for (int i = 0; i < managerOfficePrefab.RoomSprites.Count; i++)
                {
                    agency._roomPrefab._roomSprite sourceSprite = managerOfficePrefab.RoomSprites[i];
                    if (sourceSprite == null)
                    {
                        continue;
                    }

                    assistantOfficePrefab.RoomSprites.Add(new agency._roomPrefab._roomSprite
                    {
                        Type = sourceSprite.Type,
                        _Sprite = sourceSprite._Sprite
                    });
                }
            }

            agency._roomPrefab[] newPrefabs = new agency._roomPrefab[agencyInstance.roomPrefabs.Length + 1];
            Array.Copy(agencyInstance.roomPrefabs, newPrefabs, agencyInstance.roomPrefabs.Length);
            newPrefabs[newPrefabs.Length - 1] = assistantOfficePrefab;
            agencyInstance.roomPrefabs = newPrefabs;
        }

        internal static Transform GetRoomUIBlock(BuildRoomButton button)
        {
            if (button == null)
            {
                return null;
            }

            Transform current = button.transform;
            while (current.parent != null)
            {
                Transform parent = current.parent;
                if (parent.GetComponent<BuildRoom_Popup>() != null)
                {
                    return current;
                }

                BuildRoomButton[] buttonsInParent = parent.GetComponentsInChildren<BuildRoomButton>(true);
                if (buttonsInParent.Length != 1)
                {
                    return current;
                }

                current = parent;
            }

            return current;
        }

        private static Transform FindBuildMenuHeader(Transform entry)
        {
            if (entry == null || entry.parent == null)
            {
                return null;
            }

            int entrySiblingIndex = entry.GetSiblingIndex();
            for (int i = entrySiblingIndex - 1; i >= 0; i--)
            {
                Transform candidate = entry.parent.GetChild(i);
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.GetComponentsInChildren<BuildRoomButton>(true).Length > 0)
                {
                    break;
                }

                if (candidate.GetComponentsInChildren<RoomTitle>(true).Length > 0 ||
                    candidate.GetComponentsInChildren<TextMeshProUGUI>(true).Length > 0 ||
                    candidate.GetComponentsInChildren<Text>(true).Length > 0)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static Transform EnsureAssistantManagerBuildMenuHeader(Transform assistantRow, Transform headerTemplate)
        {
            if (assistantRow == null || assistantRow.parent == null)
            {
                return null;
            }

            Transform parent = assistantRow.parent;
            Transform existingHeader = parent.Find(AssistantManagerConstants.AssistantManagerOfficeBuildHeaderObjectName);
            if (existingHeader != null)
            {
                existingHeader.SetSiblingIndex(assistantRow.GetSiblingIndex());
                ApplyAssistantManagerBuildMenuHeaderPresentation(existingHeader.gameObject);
                return existingHeader;
            }

            if (headerTemplate == null)
            {
                return null;
            }

            GameObject header = UnityEngine.Object.Instantiate<GameObject>(headerTemplate.gameObject, parent, false);
            header.name = AssistantManagerConstants.AssistantManagerOfficeBuildHeaderObjectName;
            header.transform.SetSiblingIndex(assistantRow.GetSiblingIndex());

            RectTransform headerRect = header.GetComponent<RectTransform>();
            float headerHeight = GetBuildMenuHeaderHeight(headerRect);
            LayoutElement headerLayout = header.GetComponent<LayoutElement>();
            if (headerLayout == null)
            {
                headerLayout = header.AddComponent<LayoutElement>();
            }
            if (headerLayout.minHeight <= 0f)
            {
                headerLayout.minHeight = headerHeight;
            }
            if (headerLayout.preferredHeight <= 0f)
            {
                headerLayout.preferredHeight = headerHeight;
            }

            if (!UsesAutomaticBuildMenuLayout(parent))
            {
                PositionBuildMenuRowBelowPreviousSibling(headerRect, parent, header.transform.GetSiblingIndex(), headerHeight);
                ShiftFollowingBuildMenuEntries(parent, header.transform, headerHeight);
            }

            ExpandBuildMenuOverlay(parent, headerHeight);
            ApplyAssistantManagerBuildMenuHeaderPresentation(header);
            return header.transform;
        }

        private static void ApplyAssistantManagerBuildMenuHeaderPresentation(GameObject header)
        {
            if (header == null)
            {
                return;
            }

            Lang_Button[] languageBindings = header.GetComponentsInChildren<Lang_Button>(true);
            for (int i = 0; i < languageBindings.Length; i++)
            {
                if (languageBindings[i] == null)
                {
                    continue;
                }

                languageBindings[i].Constant = string.Empty;
                languageBindings[i].Tooltip = string.Empty;
            }

            RoomTitle[] roomTitles = header.GetComponentsInChildren<RoomTitle>(true);
            for (int i = 0; i < roomTitles.Length; i++)
            {
                if (roomTitles[i] != null)
                {
                    roomTitles[i].SetText(AssistantManagerText.AssistantManagerOfficeTitle);
                }
            }

            TextMeshProUGUI[] tmpTexts = header.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < tmpTexts.Length; i++)
            {
                if (tmpTexts[i] != null)
                {
                    tmpTexts[i].text = AssistantManagerText.AssistantManagerOfficeTitle;
                }
            }

            Text[] legacyTexts = header.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < legacyTexts.Length; i++)
            {
                if (legacyTexts[i] != null)
                {
                    legacyTexts[i].text = AssistantManagerText.AssistantManagerOfficeTitle;
                }
            }
        }

        private static Transform EnsureAssistantManagerBuildMenuRow(Transform entry)
        {
            if (entry == null)
            {
                return null;
            }

            AssistantManagerBuildMenuRowMarker existingMarker = entry.GetComponentInParent<AssistantManagerBuildMenuRowMarker>();
            if (existingMarker != null)
            {
                return existingMarker.transform;
            }

            Transform parent = entry.parent;
            if (parent == null)
            {
                return entry;
            }

            RectTransform entryRect = entry as RectTransform;
            Vector2 anchorMin = entryRect != null ? entryRect.anchorMin : Vector2.zero;
            Vector2 anchorMax = entryRect != null ? entryRect.anchorMax : Vector2.one;
            Vector2 anchoredPosition = entryRect != null ? entryRect.anchoredPosition : Vector2.zero;
            Vector2 sizeDelta = entryRect != null ? entryRect.sizeDelta : Vector2.zero;
            Vector2 pivot = entryRect != null ? entryRect.pivot : new Vector2(0.5f, 0.5f);
            float entryHeight = GetBuildMenuEntryHeight(entryRect);
            int siblingIndex = entry.GetSiblingIndex();

            GameObject rowObject = new GameObject(
                AssistantManagerConstants.AssistantManagerOfficeBuildRowObjectName,
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(AssistantManagerBuildMenuRowMarker));
            rowObject.transform.SetParent(parent, false);
            rowObject.transform.SetSiblingIndex(siblingIndex);

            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.anchorMin = anchorMin;
            rowRect.anchorMax = anchorMax;
            rowRect.anchoredPosition = anchoredPosition;
            rowRect.sizeDelta = sizeDelta;
            rowRect.pivot = pivot;

            LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = entryHeight;
            layoutElement.preferredHeight = entryHeight;

            if (!UsesAutomaticBuildMenuLayout(parent))
            {
                PositionBuildMenuRowBelowPreviousSibling(rowRect, parent, siblingIndex, entryHeight);
                ShiftFollowingBuildMenuEntries(parent, rowObject.transform, entryHeight);
            }

            entry.SetParent(rowObject.transform, false);
            if (entryRect != null)
            {
                entryRect.anchorMin = Vector2.zero;
                entryRect.anchorMax = Vector2.one;
                entryRect.offsetMin = Vector2.zero;
                entryRect.offsetMax = Vector2.zero;
                entryRect.pivot = new Vector2(0.5f, 0.5f);
            }

            ExpandBuildMenuOverlay(parent, entryHeight);
            return rowObject.transform;
        }

        private static bool UsesAutomaticBuildMenuLayout(Transform contentRoot)
        {
            return contentRoot != null &&
                   (contentRoot.GetComponent<VerticalLayoutGroup>() != null ||
                    contentRoot.GetComponent<GridLayoutGroup>() != null);
        }

        private static void PositionBuildMenuRowBelowPreviousSibling(
            RectTransform rowRect,
            Transform parent,
            int siblingIndex,
            float rowHeight)
        {
            if (rowRect == null || parent == null || siblingIndex <= 0)
            {
                return;
            }

            RectTransform previousRect = parent.GetChild(siblingIndex - 1) as RectTransform;
            if (previousRect == null)
            {
                return;
            }

            float previousHeight = GetBuildMenuEntryHeight(previousRect);
            float previousBottom = previousRect.anchoredPosition.y - previousHeight * (1f - previousRect.pivot.y);
            float rowY = previousBottom - rowHeight * rowRect.pivot.y - 4f;
            AlignBuildMenuRowLeftEdge(rowRect, parent, previousRect);
            rowRect.anchoredPosition = new Vector2(rowRect.anchoredPosition.x, rowY);
        }

        private static void AlignBuildMenuRowLeftEdge(RectTransform rowRect, Transform parent, RectTransform referenceRect)
        {
            if (rowRect == null || parent == null || referenceRect == null)
            {
                return;
            }

            Vector3[] referenceCorners = new Vector3[4];
            referenceRect.GetWorldCorners(referenceCorners);
            float referenceLeft = parent.InverseTransformPoint(referenceCorners[0]).x;

            float rowWidth = Mathf.Abs(rowRect.rect.width);
            if (rowWidth <= 0f)
            {
                rowWidth = Mathf.Abs(rowRect.sizeDelta.x);
            }

            rowRect.anchorMin = new Vector2(0f, rowRect.anchorMin.y);
            rowRect.anchorMax = new Vector2(0f, rowRect.anchorMax.y);
            rowRect.pivot = new Vector2(0f, rowRect.pivot.y);
            rowRect.anchoredPosition = new Vector2(referenceLeft, rowRect.anchoredPosition.y);
            rowRect.sizeDelta = new Vector2(rowWidth, rowRect.sizeDelta.y);
        }

        private static void ShiftFollowingBuildMenuEntries(Transform parent, Transform row, float rowHeight)
        {
            if (parent == null || row == null)
            {
                return;
            }

            int rowSiblingIndex = row.GetSiblingIndex();
            for (int i = rowSiblingIndex + 1; i < parent.childCount; i++)
            {
                Transform sibling = parent.GetChild(i);
                if (sibling == null)
                {
                    continue;
                }

                RectTransform siblingRect = sibling as RectTransform;
                if (siblingRect != null)
                {
                    siblingRect.anchoredPosition -= new Vector2(0f, rowHeight + 4f);
                }
            }
        }

        private static float GetBuildMenuEntryHeight(RectTransform entryRect)
        {
            if (entryRect != null)
            {
                float height = Mathf.Abs(entryRect.rect.height);
                if (height > 0f)
                {
                    return height;
                }

                height = Mathf.Abs(entryRect.sizeDelta.y);
                if (height > 0f)
                {
                    return height;
                }
            }

            return 90f;
        }

        private static float GetBuildMenuHeaderHeight(RectTransform headerRect)
        {
            if (headerRect != null)
            {
                float height = Mathf.Abs(headerRect.rect.height);
                if (height > 0f)
                {
                    return height;
                }

                height = Mathf.Abs(headerRect.sizeDelta.y);
                if (height > 0f)
                {
                    return height;
                }
            }

            return 40f;
        }

        private static void ExpandBuildMenuOverlay(Transform contentRoot, float entryHeight)
        {
            if (contentRoot == null)
            {
                return;
            }

            RectTransform contentRect = contentRoot as RectTransform;
            bool contentUsesSizeFitter = contentRoot.GetComponent<ContentSizeFitter>() != null;
            if (!contentUsesSizeFitter && contentRect != null)
            {
                contentRect.sizeDelta = new Vector2(
                    contentRect.sizeDelta.x,
                    contentRect.sizeDelta.y + entryHeight);
            }

            if (contentRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            }

            ScrollRect scrollRect = contentRoot.GetComponentInParent<ScrollRect>();
            if (scrollRect != null && scrollRect.content != null && scrollRect.content != contentRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            }
        }

        internal static void EnsureBuildMenuButton(agency agencyInstance)
        {
            if (isEnsuringBuildMenuButton || agencyInstance == null || agencyInstance.newRoomPopup == null)
            {
                return;
            }

            isEnsuringBuildMenuButton = true;
            try
            {
                BuildRoomButton[] buttons = agencyInstance.newRoomPopup.GetComponentsInChildren<BuildRoomButton>(true);
                BuildRoomButton assistantOfficeButton = null;
                BuildRoomButton managerOfficeButton = null;
                BuildRoomButton theaterButton = null;

                for (int i = 0; i < buttons.Length; i++)
                {
                    BuildRoomButton button = buttons[i];
                    if (button == null)
                    {
                        continue;
                    }

                    if (button.type == AssistantManagerConstants.AssistantManagerOfficeRoomType)
                    {
                        assistantOfficeButton = button;
                    }
                    else if (button.type == agency._type.yourOffice)
                    {
                        managerOfficeButton = button;
                    }
                    else if (button.type == agency._type.theatre)
                    {
                        theaterButton = button;
                    }
                }

                if (assistantOfficeButton != null)
                {
                    Transform existingEntry = GetRoomUIBlock(assistantOfficeButton);
                    Transform assistantRow = EnsureAssistantManagerBuildMenuRow(existingEntry);
                    Transform managerOfficeEntry = GetRoomUIBlock(managerOfficeButton);
                    Transform headerTemplate = FindBuildMenuHeader(managerOfficeEntry) ?? FindBuildMenuHeader(GetRoomUIBlock(theaterButton));
                    EnsureAssistantManagerBuildMenuHeader(assistantRow, headerTemplate);
                    ApplyAssistantManagerOfficeBuildButtonPresentation(assistantOfficeButton, agencyInstance);
                    return;
                }

                if (managerOfficeButton == null || theaterButton == null)
                {
                    return;
                }

                Transform theaterEntry = GetRoomUIBlock(theaterButton);
                Transform managerOfficeEntryForClone = GetRoomUIBlock(managerOfficeButton);
                if (theaterEntry == null || theaterEntry.parent == null || managerOfficeEntryForClone == null)
                {
                    return;
                }

                GameObject clone = UnityEngine.Object.Instantiate<GameObject>(managerOfficeEntryForClone.gameObject, theaterEntry.parent, false);
                clone.name = AssistantManagerConstants.AssistantManagerOfficeBuildButtonObjectName + "_Entry";
                clone.transform.SetSiblingIndex(theaterEntry.GetSiblingIndex() + 1);

                BuildRoomButton clonedButton = clone.GetComponentsInChildren<BuildRoomButton>(true).FirstOrDefault();
                if (clonedButton == null)
                {
                    UnityEngine.Object.Destroy(clone);
                    return;
                }

                clonedButton.type = AssistantManagerConstants.AssistantManagerOfficeRoomType;
                Transform assistantEntry = EnsureAssistantManagerBuildMenuRow(clone.transform);
                Transform headerTemplateForClone = FindBuildMenuHeader(managerOfficeEntryForClone) ?? FindBuildMenuHeader(theaterEntry);
                EnsureAssistantManagerBuildMenuHeader(assistantEntry, headerTemplateForClone);
                ApplyAssistantManagerOfficeBuildButtonPresentation(clonedButton, agencyInstance);
            }
            finally
            {
                isEnsuringBuildMenuButton = false;
            }
        }

        internal static void EnsureStaffHireButton(Staff_Hire_Popup popup)
        {
            if (popup == null || popup.Container == null) return;

            Staff_Hire_Button[] allButtons = popup.Container.GetComponentsInChildren<Staff_Hire_Button>(true);
            Staff_Hire_Button cloneSource = null;
            int lastVanillaRowIndex = -1;

            for (int i = 0; i < allButtons.Length; i++)
            {
                if (allButtons[i] == null) continue;

                bool isAssistant = (allButtons[i].Type == AssistantManagerConstants.AssistantManagerStaffType ||
                                    allButtons[i].Type == AssistantManagerConstants.AssistantManagerStaffType2);

                if (isAssistant)
                {
                    allButtons[i].Set(popup.Expertise);
                    ApplyAssistantManagerHireButtonPresentation(allButtons[i]);
                }
                else
                {
                    Transform row = allButtons[i].transform;
                    while (row.parent != null && row.parent != popup.Container.transform)
                    {
                        row = row.parent;
                    }
                    if (row.parent == popup.Container.transform)
                    {
                        lastVanillaRowIndex = Math.Max(lastVanillaRowIndex, row.GetSiblingIndex());
                    }

                    if (allButtons[i].Type == staff._type.production_manager)
                    {
                        cloneSource = allButtons[i];
                    }
                }
            }

            if (cloneSource == null) return;

            Transform directChildOfContainer = cloneSource.transform;
            while (directChildOfContainer.parent != null && directChildOfContainer.parent != popup.Container.transform)
            {
                directChildOfContainer = directChildOfContainer.parent;
            }

            Transform headerToClone = null;
            int cloneSourceSiblingIndex = directChildOfContainer.GetSiblingIndex();
            
            if (cloneSourceSiblingIndex > 0)
            {
                Transform prevSibling = popup.Container.transform.GetChild(cloneSourceSiblingIndex - 1);
                if (prevSibling.GetComponentsInChildren<Staff_Hire_Button>(true).Length == 0)
                {
                    headerToClone = prevSibling;
                }
            }

            int targetSiblingIndex = lastVanillaRowIndex != -1 ? lastVanillaRowIndex + 1 : popup.Container.transform.childCount;

            Transform existingHeader = popup.Container.transform.Find("AssistantManagerHeader");
            GameObject newHeader = existingHeader != null ? existingHeader.gameObject : null;
            
            if (headerToClone != null && newHeader == null)
            {
                newHeader = UnityEngine.Object.Instantiate(headerToClone.gameObject, popup.Container.transform, false);
                newHeader.name = "AssistantManagerHeader";
            }
            
            if (newHeader != null)
            {
                newHeader.transform.SetSiblingIndex(targetSiblingIndex);
                targetSiblingIndex = newHeader.transform.GetSiblingIndex() + 1;
                ApplyAssistantManagerStaffHeaderPresentation(newHeader);
            }

            Transform existingRow = popup.Container.transform.Find("AssistantManagerRow");
            GameObject newRow = existingRow != null ? existingRow.gameObject : null;
            bool isNewRow = false;

            if (newRow == null)
            {
                newRow = UnityEngine.Object.Instantiate(directChildOfContainer.gameObject, popup.Container.transform, false);
                newRow.name = "AssistantManagerRow";
                isNewRow = true;
            }

            newRow.transform.SetSiblingIndex(targetSiblingIndex);

            if (isNewRow)
            {
                Staff_Hire_Button[] newButtons = newRow.GetComponentsInChildren<Staff_Hire_Button>(true);
                
                if (newButtons.Length >= 2)
                {
                    Staff_Hire_Button hireButton1 = newButtons[0];
                    Staff_Hire_Button hireButton2 = newButtons[1];
                    
                    for (int i = 2; i < newButtons.Length; i++)
                    {
                        UnityEngine.Object.Destroy(newButtons[i].gameObject);
                    }

                    hireButton1.name = AssistantManagerConstants.AssistantManagerHireButtonObjectName + "_1";
                    hireButton1.Type = AssistantManagerConstants.AssistantManagerStaffType;
                    hireButton1.Set(popup.Expertise);
                    ApplyAssistantManagerHireButtonPresentation(hireButton1);

                    hireButton2.name = AssistantManagerConstants.AssistantManagerHireButtonObjectName + "_2";
                    hireButton2.Type = AssistantManagerConstants.AssistantManagerStaffType2;
                    hireButton2.Set(popup.Expertise);
                    ApplyAssistantManagerHireButtonPresentation(hireButton2);
                }
            }
        }

        internal static void ApplyAssistantManagerHeaderPresentation(GameObject obj)
        {
            ApplyAssistantManagerHeaderPresentation(obj, AssistantManagerText.AssistantManagerOfficeTitle);
        }

        internal static void ApplyAssistantManagerStaffHeaderPresentation(GameObject obj)
        {
            ApplyAssistantManagerHeaderPresentation(obj, AssistantManagerText.AssistantManagerTitle);
        }

        private static void ApplyAssistantManagerHeaderPresentation(GameObject obj, string title)
        {
            if (obj == null) return;

            MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < scripts.Length; i++)
            {
                if (scripts[i] == null) continue;
                
                string name = scripts[i].GetType().Name.ToLower();
                if (name.Contains("local") || name.Contains("lang") || name.Contains("trans"))
                {
                    UnityEngine.Object.Destroy(scripts[i]);
                }
            }

            TextMeshProUGUI[] tmpTexts = obj.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < tmpTexts.Length; i++)
            {
                if (tmpTexts[i] != null && tmpTexts[i].GetComponentInParent<Staff_Hire_Button>() == null)
                {
                    tmpTexts[i].text = title;
                }
            }

            Text[] unityTexts = obj.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < unityTexts.Length; i++)
            {
                if (unityTexts[i] != null && unityTexts[i].GetComponentInParent<Staff_Hire_Button>() == null)
                {
                    unityTexts[i].text = title;
                }
            }
        }

        internal static void ApplyAssistantManagerOfficeBuildButtonPresentation(BuildRoomButton buildRoomButton, agency agencyInstance)
        {
            if (buildRoomButton == null)
            {
                return;
            }

            buildRoomButton.type = AssistantManagerConstants.AssistantManagerOfficeRoomType;
            buildRoomButton.available = true;
            
            Transform block = GetRoomUIBlock(buildRoomButton);
            if (block == null)
            {
                return;
            }
            
            RoomTitle roomTitle = block.GetComponentsInChildren<RoomTitle>(true).FirstOrDefault();
            if (roomTitle != null)
            {
                roomTitle.gameObject.name = AssistantManagerConstants.AssistantManagerOfficeBuildHeaderObjectName;
                roomTitle.SetText(AssistantManagerText.AssistantManagerOfficeTitle);
            }
            else
            {
                SetFirstTextComponent(block.gameObject, AssistantManagerText.AssistantManagerOfficeTitle);
            }

            SetBuildRoomButtonAvailability(buildRoomButton, agencyInstance);
        }

        internal static void ApplyAssistantManagerHireButtonPresentation(Staff_Hire_Button hireButton)
        {
            if (hireButton == null || !IsAssistantManagerStaffType(hireButton.Type))
            {
                return;
            }

            ReplaceKnownStaffRoleText(hireButton.gameObject, AssistantManagerText.AssistantManagerTitle);
            SetStaffHireButtonAvailability(hireButton);
        }

        internal static void SetStaffHireButtonAvailability(Staff_Hire_Button hireButton)
        {
            if (hireButton == null || !IsAssistantManagerStaffType(hireButton.Type))
            {
                return;
            }

            bool canHire = CanHireAssistantManager();
            ButtonDefault buttonDefault = hireButton.GetComponent<ButtonDefault>();
            if (buttonDefault != null)
            {
                buttonDefault.Activate(canHire, true);
                if (!canHire)
                {
                    buttonDefault.SetTooltip(ExtensionMethods.color(AssistantManagerText.AssistantManagerLimitReached, mainScript.red));
                }
            }

            CanvasGroup canvasGroup = hireButton.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = canHire ? 1f : 0.3f;
            }

            if (hireButton.Footer != null)
            {
                CanvasGroup footerCanvasGroup = hireButton.Footer.GetComponent<CanvasGroup>();
                if (footerCanvasGroup != null)
                {
                    bool canShowFooter = canHire && (hireButton.Staffer == null || hireButton.Staffer.CanHire());
                    footerCanvasGroup.alpha = canShowFooter ? 1f : 0f;
                }
            }
        }

        internal static void SetBuildRoomButtonAvailability(BuildRoomButton buildRoomButton, agency agencyInstance)
        {
            if (buildRoomButton == null || buildRoomButton.type != AssistantManagerConstants.AssistantManagerOfficeRoomType)
            {
                return;
            }

            if (agencyInstance == null)
            {
                agencyInstance = GetAgency();
            }

            bool available = buildRoomButton.available;
            bool canBuild = available && CanBuildAssistantManagerOffice(agencyInstance);
            if (canBuild && agencyInstance != null)
            {
                canBuild = agencyInstance.CanBuild_IsEnoughSpace(AssistantManagerConstants.AssistantManagerOfficeRoomType) &&
                           agencyInstance.CanBuild_IsEnoughMoney(AssistantManagerConstants.AssistantManagerOfficeRoomType);
            }

            ButtonDefault buttonDefault = buildRoomButton.GetComponent<ButtonDefault>();
            if (buttonDefault != null)
            {
                buttonDefault.SetTooltip(BuildAssistantManagerOfficeTooltip(agencyInstance, available));
            }

            if (BuildRoomButtonActivateMethod != null)
            {
                BuildRoomButtonActivateMethod.Invoke(buildRoomButton, new object[] { canBuild });
            }
            else if (buttonDefault != null)
            {
                buttonDefault.Activate(canBuild, true);
            }
        }

        internal static string BuildAssistantManagerOfficeTooltip(agency agencyInstance, bool available)
        {
            string text = "";
            if (!available)
            {
                text = ExtensionMethods.color(Language.Data["MSG__NOT_AVAILABLE"], mainScript.red) + mainScript.separator;
            }
            else if (agencyInstance != null && !CanBuildAssistantManagerOffice(agencyInstance))
            {
                text = ExtensionMethods.color(AssistantManagerText.AssistantManagerOfficeLimit, mainScript.red) + mainScript.separator;
            }
            else if (agencyInstance != null && !agencyInstance.CanBuild_IsEnoughSpace(AssistantManagerConstants.AssistantManagerOfficeRoomType))
            {
                text = ExtensionMethods.color(Language.Data["MSG__NO_SPACE"], mainScript.red) + mainScript.separator;
            }
            else if (agencyInstance != null && !agencyInstance.CanBuild_IsEnoughMoney(AssistantManagerConstants.AssistantManagerOfficeRoomType))
            {
                text = ExtensionMethods.color(Language.Data["MSG__NOT_ENOUGH_MONEY"], mainScript.red) + mainScript.separator;
            }

            if (available)
            {
                text += BuildAssistantManagerOfficeRoomTooltip(agencyInstance);
            }

            return text;
        }

        internal static string BuildAssistantManagerOfficeRoomTooltip(agency agencyInstance)
        {
            string text = "";
            text = text + Language.Data["IDOLS"].ToUpper() + ":\n";
            text = text + "- " + Language.Data["AUDITIONS"] + "\n";
            text = text + "- " + Language.Data["AUDITION__HINT"];
            text += mainScript.separator;
            text = text + Language.Data["SINGLES"].ToUpper() + ":\n";
            text = text + "- " + Language.Data["SINGLE__LYRICS"];
            text += mainScript.separator;
            text = text + Language.Data["MEDIA"].ToUpper() + ":\n";
            text = text + "- " + Language.Data["MEDIA__CONCEPT"];
            text += mainScript.separator;
            text = text + Language.Data["SEVENTS"].ToUpper() + ":\n";
            text = text + "- " + Language.Data["AGENCY__PRODUCTION"];
            text += mainScript.separator;
            text += AssistantManagerText.AssistantManagerOfficeRequires + "\n";
            text += AssistantManagerText.AssistantManagerOfficeLimit;
            text += mainScript.separator;

            string costColor = mainScript.blue;
            if ((long)AssistantManagerConstants.AssistantManagerOfficeRoomCost > resources.Money() && !staticVars.IsEasy())
            {
                costColor = mainScript.red;
            }

            text = string.Concat(new string[]
            {
                text,
                Language.Data["COST"],
                ": ",
                ExtensionMethods.color(ExtensionMethods.formatMoney(AssistantManagerConstants.AssistantManagerOfficeRoomCost, false, false), costColor),
                "\n"
            });

            text = text + Language.Data["AGENCY__RENT"] + ": ";
            agency._floor selectedFloor = agencyInstance == null ? null : agencyInstance.GetSelectedFloor();
            if (selectedFloor == null || selectedFloor.FirstFloor)
            {
                text += ExtensionMethods.color(Language.Data["AGENCY__FLOOR_NO_RENT"], mainScript.green);
            }
            else
            {
                int rent = agencyInstance.GetRoomRent(AssistantManagerConstants.AssistantManagerOfficeRoomType, selectedFloor.FloorID);
                text += ExtensionMethods.color(ExtensionMethods.formatMoney(rent, false, false) + " " + Language.Data["PER_WEEK"], mainScript.red);
            }

            return text;
        }

        internal static agency._room FindFirstManagerOffice(bool requireStaffed, bool requireNormalStatus)
        {
            agency agencyInstance = GetAgency();
            if (agencyInstance == null)
            {
                return null;
            }

            List<agency._room> rooms = agencyInstance.allRooms(false, true);
            for (int i = 0; i < rooms.Count; i++)
            {
                agency._room room = rooms[i];
                if (!IsManagerOffice(room))
                {
                    continue;
                }

                if (requireStaffed && room.staffer == null)
                {
                    continue;
                }

                if (requireNormalStatus && room.status != agency._room._status.normal)
                {
                    continue;
                }

                return room;
            }

            return null;
        }

        internal static agency GetAgency()
        {
            try
            {
                return Camera.main.GetComponent<mainScript>().Data.GetComponent<agency>();
            }
            catch
            {
                return null;
            }
        }

        internal static void SetFirstTextComponent(GameObject root, string text)
        {
            if (root == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            TextMeshProUGUI tmpText = root.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault();
            if (tmpText != null)
            {
                tmpText.text = text;
                return;
            }

            Text unityText = root.GetComponentsInChildren<Text>(true).FirstOrDefault();
            if (unityText != null)
            {
                unityText.text = text;
            }
        }

        internal static void ReplaceKnownStaffRoleText(GameObject root, string replacementText)
        {
            if (root == null || string.IsNullOrEmpty(replacementText))
            {
                return;
            }

            string productionManagerTitle = staff.GetJobTitleString(staff._type.production_manager);
            string salesManagerTitle = staff.GetJobTitleString(staff._type.sales_manager);
            string producerTitle = staff.GetJobTitleString(staff._type.player);

            TextMeshProUGUI[] tmpTexts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < tmpTexts.Length; i++)
            {
                if (tmpTexts[i] != null && IsKnownStaffRoleLabel(tmpTexts[i].text, productionManagerTitle, salesManagerTitle, producerTitle))
                {
                    tmpTexts[i].text = replacementText;
                }
            }

            Text[] unityTexts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < unityTexts.Length; i++)
            {
                if (unityTexts[i] != null && IsKnownStaffRoleLabel(unityTexts[i].text, productionManagerTitle, salesManagerTitle, producerTitle))
                {
                    unityTexts[i].text = replacementText;
                }
            }
        }

        private static bool IsKnownStaffRoleLabel(string currentText, string productionManagerTitle, string salesManagerTitle, string producerTitle)
        {
            if (string.IsNullOrEmpty(currentText))
            {
                return false;
            }

            string normalizedText = currentText.Trim();
            return string.Equals(normalizedText, productionManagerTitle, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(normalizedText, salesManagerTitle, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(normalizedText, producerTitle, StringComparison.OrdinalIgnoreCase);
        }

        internal static singles._single._param._type GetManagerOfficeSingleParamType(singles._single single)
        {
            if (single == null)
            {
                return singles._single._param._type.lyrics;
            }

            if (single.GetParam(singles._single._param._type.lyrics).val == 0f)
            {
                return singles._single._param._type.lyrics;
            }

            if (single.GetParam(singles._single._param._type.marketing).val == 0f)
            {
                return singles._single._param._type.marketing;
            }

            if (single.GetParam(singles._single._param._type.song).val == 0f)
            {
                return singles._single._param._type.song;
            }

            if (single.GetParam(singles._single._param._type.choreography).val == 0f)
            {
                return singles._single._param._type.choreography;
            }

            return singles._single._param._type.lyrics;
        }

        internal static Shows._show._progressable._type GetManagerOfficeShowParamType(Shows._show show)
        {
            if (show == null || show.GetParam(Shows._show._progressable._type.concept).val == 0f)
            {
                return Shows._show._progressable._type.concept;
            }

            return Shows._show._progressable._type.preproduction;
        }

        internal static SEvent_SSK._SSK._progressable._type GetManagerOfficeSskParamType(SEvent_SSK._SSK ssk)
        {
            if (ssk == null || ssk.GetParam(SEvent_SSK._SSK._progressable._type.production).val == 0f)
            {
                return SEvent_SSK._SSK._progressable._type.production;
            }

            return SEvent_SSK._SSK._progressable._type.logistics;
        }

        internal static SEvent_Tour.tour._progressable._type GetManagerOfficeTourParamType(SEvent_Tour.tour tour)
        {
            if (tour == null || tour.GetParam(SEvent_Tour.tour._progressable._type.production).val == 0f)
            {
                return SEvent_Tour.tour._progressable._type.production;
            }

            return SEvent_Tour.tour._progressable._type.logistics;
        }

        internal static SEvent_Concerts._concert._progressable._type GetManagerOfficeConcertParamType(SEvent_Concerts._concert concert)
        {
            if (concert == null)
            {
                return SEvent_Concerts._concert._progressable._type.production;
            }

            if (concert.GetParam(SEvent_Concerts._concert._progressable._type.production).val == 0f)
            {
                return SEvent_Concerts._concert._progressable._type.production;
            }

            if (concert.GetParam(SEvent_Concerts._concert._progressable._type.logistics).val == 0f)
            {
                return SEvent_Concerts._concert._progressable._type.logistics;
            }

            return SEvent_Concerts._concert._progressable._type.rehearsals;
        }

        internal static float GetManagerOfficeTaskPoints(agency._room room, staff._skill_type skillType)
        {
            if (room == null || room.staffer == null)
            {
                return 0f;
            }

            staff._staff._skill skill = room.staffer.GetSkill(skillType);
            if (skill == null)
            {
                return 0f;
            }

            return (float)skill.GetPoints();
        }

        internal static float ApplyManagerOfficeSecondaryTaskPenalty(float points)
        {
            return points * ManagerOfficeSecondaryTaskPointMultiplier;
        }

        internal static void AddStaffFloat(agency._room room, string text, bool levelUp, Color32? color)
        {
            if (room == null || AddStaffFloatMethod == null)
            {
                return;
            }

            AddStaffFloatMethod.Invoke(room, new object[] { text, levelUp, color });
        }

        internal static void OnTaskComplete(agency._room room)
        {
            if (room == null || OnTaskCompleteMethod == null)
            {
                return;
            }

            OnTaskCompleteMethod.Invoke(room, new object[0]);
        }

        internal static void PlayManagerOfficeDateVoice(data_girls.girls girl)
        {
            if (girl == null)
            {
                return;
            }

            int relationshipLevel = girl.GetRelationshipLevel(Relationships_Player._type.Friendship);
            Dating._partner datingData = girl.GetDatingData();
            if (datingData != null)
            {
                if (!datingData.IsDatingNow())
                {
                    VO.Play(VO.GetRandom(VO._random.love), girl);
                    return;
                }

                VO.Play(VO.GetRandom(VO._random.shocked), girl);
                return;
            }

            if (relationshipLevel < -2)
            {
                VO.Play(VO.GetRandom(VO._random.hate), girl);
                return;
            }

            if (relationshipLevel < 0)
            {
                VO.Play(VO.GetRandom(VO._random.shocked), girl);
                return;
            }

            if (relationshipLevel < 3)
            {
                VO.Play(VO.GetRandom(VO._random.affirmation), girl);
                return;
            }

            VO.Play(VO.GetRandom(VO._random.joy), girl);
        }

        internal static bool EnsureAssistantManagerOfficeContextMenu(ContextMenuController controller)
        {
            if (controller == null || controller.ContextMenu == null)
            {
                return false;
            }

            ContextMenuController._ContextMenu playerOfficeMenu = null;
            for (int i = 0; i < controller.ContextMenu.Count; i++)
            {
                ContextMenuController._ContextMenu contextMenu = controller.ContextMenu[i];
                if (contextMenu == null)
                {
                    continue;
                }

                if (contextMenu.type == AssistantManagerConstants.AssistantManagerOfficeRoomType)
                {
                    return true;
                }

                if (contextMenu.type == agency._type.yourOffice)
                {
                    playerOfficeMenu = contextMenu;
                }
            }

            if (playerOfficeMenu == null || playerOfficeMenu.prefab == null)
            {
                return false;
            }

            // ContextMenuController instantiates this prefab for every open menu.  A separate
            // mapping therefore gives Assistant Manager offices their own UI instance and state.
            controller.ContextMenu.Add(new ContextMenuController._ContextMenu
            {
                type = AssistantManagerConstants.AssistantManagerOfficeRoomType,
                prefab = playerOfficeMenu.prefab
            });
            return true;
        }

        internal static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                Transform result = FindDeepChild(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(agency), "Start", new Type[0])]
    internal static class AgencyStartPatch
    {
        private static void Postfix(agency __instance)
        {
            AssistantManagerDateTracking.Reset();
            AssistantManagerAuditionTracking.Reset();
            AssistantManagerRules.RegisterAssistantManagerOfficePrefab(__instance);
            AssistantManagerRules.EnsureBuildMenuButton(__instance);
        }
    }

    [HarmonyPatch(typeof(agency), nameof(agency.LoadFunction), new Type[0])]
    internal static class AgencyLoadFunctionStateRestorePatch
    {
        private static void Postfix()
        {
            AssistantManagerStatePersistence.RequestRestore();
        }
    }

    [HarmonyPatch(typeof(agency), nameof(agency.renderFloors), new Type[0])]
    internal static class AgencyRenderFloorsStateRestorePatch
    {
        private static void Postfix(agency __instance)
        {
            AssistantManagerStatePersistence.Synchronize(__instance);
        }
    }

    [HarmonyPatch(typeof(agency), "Update", new Type[0])]
    internal static class AgencyUpdateStatePersistencePatch
    {
        private static void Postfix(agency __instance)
        {
            AssistantManagerStatePersistence.Synchronize(__instance);
        }
    }

    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.CallSaveEvent), new Type[0])]
    internal static class SaveManagerCallSaveEventStatePersistencePatch
    {
        private static void Prefix()
        {
            AssistantManagerStatePersistence.Synchronize(AssistantManagerRules.GetAgency());
        }
    }

    [HarmonyPatch(typeof(ContextMenuController), "Start", new Type[0])]
    internal static class ContextMenuControllerStartPatch
    {
        private static void Postfix(ContextMenuController __instance)
        {
            AssistantManagerRules.EnsureAssistantManagerOfficeContextMenu(__instance);
        }
    }

    [HarmonyPatch(typeof(agency), nameof(agency.GetRoomSprite), new Type[] { typeof(Scenes.type), typeof(agency._type) })]
    internal static class AgencyGetRoomSpritePatch
    {
        private static bool Prefix(agency __instance, Scenes.type SceneType, agency._type RoomType, ref Sprite __result)
        {
            if (!AssistantManagerRules.IsAssistantManagerOfficeRoomType(RoomType))
            {
                return true;
            }

            __result = __instance.GetRoomSprite(SceneType, agency._type.yourOffice);
            return false;
        }
    }

    [HarmonyPatch(typeof(agency), nameof(agency.roomSpace), new Type[] { typeof(agency._type) })]
    internal static class AgencyRoomSpacePatch
    {
        private static bool Prefix(agency._type type, ref int __result)
        {
            if (!AssistantManagerRules.IsAssistantManagerOfficeRoomType(type))
            {
                return true;
            }

            __result = AssistantManagerConstants.AssistantManagerOfficeRoomSpace;
            return false;
        }
    }

    [HarmonyPatch(typeof(agency), nameof(agency.roomCost), new Type[] { typeof(agency._type) })]
    internal static class AgencyRoomCostPatch
    {
        private static bool Prefix(agency._type type, ref int __result)
        {
            if (!AssistantManagerRules.IsAssistantManagerOfficeRoomType(type))
            {
                return true;
            }

            __result = AssistantManagerConstants.AssistantManagerOfficeRoomCost;
            return false;
        }
    }

    [HarmonyPatch(typeof(agency), nameof(agency.CanBuild_IsUnique), new Type[] { typeof(agency._type) })]
    internal static class AgencyCanBuildIsUniquePatch
    {
        private static bool Prefix(agency __instance, agency._type type, ref bool __result)
        {
            if (!AssistantManagerRules.IsAssistantManagerOfficeRoomType(type))
            {
                return true;
            }

            __result = AssistantManagerRules.CanBuildAssistantManagerOffice(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(agency), nameof(agency.GetRoomTooltip), new Type[] { typeof(agency._type) })]
    internal static class AgencyGetRoomTooltipPatch
    {
        private static bool Prefix(agency __instance, agency._type type, ref string __result)
        {
            if (!AssistantManagerRules.IsAssistantManagerOfficeRoomType(type))
            {
                return true;
            }

            __result = AssistantManagerRules.BuildAssistantManagerOfficeRoomTooltip(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(BuildRoomButton), "SetTooltip", new Type[0])]
    internal static class BuildRoomButtonSetTooltipPatch
    {
        private static void Postfix(BuildRoomButton __instance)
        {
            if (__instance == null || __instance.type != AssistantManagerConstants.AssistantManagerOfficeRoomType)
            {
                return;
            }

            AssistantManagerRules.ApplyAssistantManagerOfficeBuildButtonPresentation(__instance, null);
        }
    }

    [HarmonyPatch(typeof(BuildRoomButton), "OnEnable", new Type[0])]
    internal static class BuildRoomButtonOnEnablePatch
    {
        private static void Postfix(BuildRoomButton __instance)
        {
            if (__instance == null)
            {
                return;
            }

            if (__instance.type == AssistantManagerConstants.AssistantManagerOfficeRoomType)
            {
                AssistantManagerRules.ApplyAssistantManagerOfficeBuildButtonPresentation(__instance, null);
                return;
            }

            if (__instance.type == agency._type.yourOffice)
            {
                AssistantManagerRules.EnsureBuildMenuButton(AssistantManagerRules.GetAgency());
            }
        }
    }

    [HarmonyPatch(typeof(BuildRoomButton), "onClick", new Type[0])]
    internal static class BuildRoomButtonOnClickPatch
    {
        private static bool Prefix(BuildRoomButton __instance)
        {
            if (__instance == null || __instance.type != AssistantManagerConstants.AssistantManagerOfficeRoomType)
            {
                return true;
            }

            agency agencyInstance = AssistantManagerRules.GetAgency();
            return agencyInstance != null &&
                   __instance.available &&
                   AssistantManagerRules.CanBuildAssistantManagerOffice(agencyInstance) &&
                   agencyInstance.CanBuild_IsEnoughSpace(AssistantManagerConstants.AssistantManagerOfficeRoomType) &&
                   agencyInstance.CanBuild_IsEnoughMoney(AssistantManagerConstants.AssistantManagerOfficeRoomType);
        }
    }

    [HarmonyPatch(typeof(Staff_Hire_Popup), "Start", new Type[0])]
    internal static class StaffHirePopupStartPatch
    {
        private static void Postfix(Staff_Hire_Popup __instance)
        {
            AssistantManagerRules.EnsureStaffHireButton(__instance);
        }
    }

    [HarmonyPatch(typeof(Staff_Hire_Popup), nameof(Staff_Hire_Popup.Reset), new Type[0])]
    internal static class StaffHirePopupResetPatch
    {
        private static void Prefix(Staff_Hire_Popup __instance)
        {
            AssistantManagerRules.EnsureStaffHireButton(__instance);
        }

        private static void Postfix(Staff_Hire_Popup __instance)
        {
            if (__instance == null || __instance.Container == null)
            {
                return;
            }

            Staff_Hire_Button[] buttons = __instance.Container.GetComponentsInChildren<Staff_Hire_Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                AssistantManagerRules.SetStaffHireButtonAvailability(buttons[i]);
            }

            Transform amHeader = __instance.Container.transform.Find("AssistantManagerHeader");
            if (amHeader != null)
            {
                AssistantManagerRules.ApplyAssistantManagerStaffHeaderPresentation(amHeader.gameObject);
            }
            
            Transform amRow = __instance.Container.transform.Find("AssistantManagerRow");
            if (amRow != null)
            {
                AssistantManagerRules.ApplyAssistantManagerStaffHeaderPresentation(amRow.gameObject);
            }
        }
    }

    [HarmonyPatch(typeof(Staff_Hire_Button), nameof(Staff_Hire_Button.Set), new Type[] { typeof(staff._expertise) })]
    internal static class StaffHireButtonSetPatch
    {
        private static void Postfix(Staff_Hire_Button __instance)
        {
            AssistantManagerRules.ApplyAssistantManagerHireButtonPresentation(__instance);
        }
    }

    [HarmonyPatch(typeof(Staff_Hire_Button), nameof(Staff_Hire_Button.OnClick), new Type[0])]
    internal static class StaffHireButtonOnClickPatch
    {
        private static bool Prefix(Staff_Hire_Button __instance)
        {
            if (__instance == null || !AssistantManagerRules.IsAssistantManagerStaffType(__instance.Type))
            {
                return true;
            }

            return AssistantManagerRules.CanHireAssistantManager();
        }
    }

    [HarmonyPatch(typeof(staff), "TypeToFolderName", new Type[] { typeof(staff._type) })]
    internal static class StaffTypeToFolderNamePatch
    {
        private static bool Prefix(staff._type type, ref string __result)
        {
            if (!AssistantManagerRules.IsAssistantManagerStaffType(type))
            {
                return true;
            }

            __result = AssistantManagerRules.GetPlayerPortraitFolderName();
            return false;
        }
    }

    [HarmonyPatch(typeof(staff), nameof(staff.GetJobTitleString), new Type[] { typeof(staff._type) })]
    internal static class StaffGetJobTitleStringPatch
    {
        private static bool Prefix(staff._type type, ref string __result)
        {
            if (!AssistantManagerRules.IsAssistantManagerStaffType(type))
            {
                return true;
            }

            __result = AssistantManagerText.AssistantManagerTitle;
            return false;
        }
    }

    [HarmonyPatch(typeof(staff), nameof(staff.GetSkillsByType), new Type[] { typeof(staff._type), typeof(staff._expertise), typeof(bool) })]
    internal static class StaffGetSkillsByTypePatch
    {
        private static bool Prefix(staff._type type, staff._expertise expertise, ref List<staff._staff._skill> __result)
        {
            if (!AssistantManagerRules.IsAssistantManagerStaffType(type))
            {
                return true;
            }

            __result = AssistantManagerRules.CreateAssistantManagerSkills(type, expertise);
            return false;
        }
    }

    [HarmonyPatch(typeof(staff), nameof(staff.Generate), new Type[] { typeof(staff._type), typeof(staff._expertise), typeof(bool) })]
    internal static class StaffGeneratePatch
    {
        private static bool Prefix(staff __instance, staff._type type, staff._expertise expertise, ref staff._staff __result)
        {
            if (!AssistantManagerRules.IsAssistantManagerStaffType(type))
            {
                return true;
            }

            __result = AssistantManagerRules.GenerateAssistantManagerStaff(__instance, type, expertise);
            return false;
        }
    }

    [HarmonyPatch(typeof(staff), nameof(staff.LoadPortrait_Big), new Type[] { typeof(staff._type), typeof(Image), typeof(bool), typeof(Action), typeof(staff._staff._unique_type) })]
    internal static class StaffLoadPortraitBigPatch
    {
        private static bool Prefix(staff._type Type, Image target, bool Pro, Action callback)
        {
            return !AssistantManagerRules.TryApplyAssistantManagerPortrait(Type, target, Pro, callback);
        }
    }

    [HarmonyPatch(typeof(staff), nameof(staff.LoadPortrait_Small), new Type[] { typeof(staff._type), typeof(Image), typeof(bool), typeof(staff._staff._unique_type) })]
    internal static class StaffLoadPortraitSmallPatch
    {
        private static bool Prefix(staff._type Type, Image target, bool Pro)
        {
            return !AssistantManagerRules.TryApplyAssistantManagerPortrait(Type, target, Pro, null);
        }
    }

    [HarmonyPatch(typeof(staff._staff), nameof(staff._staff.Is_Male), new Type[0])]
    internal static class StaffMemberIsMalePatch
    {
        private static bool Prefix(staff._staff __instance, ref bool __result)
        {
            if (!AssistantManagerRules.IsAssistantManager(__instance))
            {
                return true;
            }

            __result = AssistantManagerRules.IsPlayerMale();
            return false;
        }
    }

    [HarmonyPatch(typeof(staff._staff), nameof(staff._staff.GetRoomType), new Type[0])]
    internal static class StaffMemberGetRoomTypePatch
    {
        private static bool Prefix(staff._staff __instance, ref agency._type? __result)
        {
            if (!AssistantManagerRules.IsAssistantManager(__instance))
            {
                return true;
            }

            __result = AssistantManagerConstants.AssistantManagerOfficeRoomType;
            return false;
        }
    }

    [HarmonyPatch(typeof(staff._staff), nameof(staff._staff.GetIdleAnimationType), new Type[0])]
    internal static class StaffMemberGetIdleAnimationTypePatch
    {
        private static bool Prefix(staff._staff __instance, ref Animations._animationType __result)
        {
            if (!AssistantManagerRules.IsAssistantManager(__instance))
            {
                return true;
            }

            __result = AssistantManagerRules.GetPlayerOfficeAnimation();
            return false;
        }
    }

    [HarmonyPatch(typeof(staff._staff), nameof(staff._staff.GetLevelUpTooltip), new Type[0])]
    internal static class StaffMemberGetLevelUpTooltipPatch
    {
        private static bool Prefix(staff._staff __instance, ref string __result)
        {
            if (!AssistantManagerRules.IsAssistantManager(__instance))
            {
                return true;
            }

            string text = "";
            if (!__instance.LevelledUp)
            {
                if (__instance.CanBeLevelledUp())
                {
                    text = ExtensionMethods.color(Language.Data["STAFF__READY_FOR_PROMO"], mainScript.green);
                }
                else
                {
                    text = ExtensionMethods.color(Language.Data["STAFF__CAN_PROMOTE_WHEN"], mainScript.red);
                }

                text += mainScript.separator;
            }

            text += Language.Insert("STAFF__SENIOR_JOB_TITLE_SKILLS", new string[] { __instance.GetJobTitle() });
            text += Language.Data["STAFF__INFLUENCE_PERK"];
            text += Language.Data["STAFF__FIRING_PERK"];
            __result = text;
            return false;
        }
    }

    [HarmonyPatch(typeof(staff._staff), nameof(staff._staff.GetTooltipText), new Type[0])]
    internal static class StaffMemberGetTooltipTextPatch
    {
        private static void Postfix(staff._staff __instance, ref string __result)
        {
            if (!AssistantManagerRules.IsAssistantManager(__instance))
            {
                return;
            }

            __result += mainScript.separator;
            __result += ExtensionMethods.color(AssistantManagerText.AssistantManagerTooltip, mainScript.grey_light);
        }
    }

    [HarmonyPatch(typeof(Research), nameof(Research.GetCategoryTypeByStaff), new Type[] { typeof(staff._type) })]
    internal static class ResearchGetCategoryTypeByStaffPatch
    {
        private static bool Prefix(staff._type StaffType, ref Research.type __result)
        {
            if (!AssistantManagerRules.IsAssistantManagerStaffType(StaffType))
            {
                return true;
            }

            __result = Research.type.player;
            return false;
        }
    }

    [HarmonyPatch(typeof(Research), nameof(Research.IsStaffInCategory), new Type[] { typeof(Research.type), typeof(staff._type) })]
    internal static class ResearchIsStaffInCategoryPatch
    {
        private static void Postfix(Research.type Type, staff._type StaffType, ref bool __result)
        {
            if (Type == Research.type.player && AssistantManagerRules.IsAssistantManagerStaffType(StaffType))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(ContextMenuController), "GetContextMenu", new Type[] { typeof(agency._type) })]
    internal static class ContextMenuControllerGetContextMenuPatch
    {
        private static bool Prefix(ContextMenuController __instance, agency._type type, ref ContextMenuController._ContextMenu __result)
        {
            if (!AssistantManagerRules.IsAssistantManagerOfficeRoomType(type) || __instance == null || __instance.ContextMenu == null)
            {
                return true;
            }

            if (AssistantManagerRules.EnsureAssistantManagerOfficeContextMenu(__instance))
            {
                // Let the original lookup return the dedicated Assistant Manager entry.
                return true;
            }

            // Older save/load orders can request a room menu before ContextMenuController.Start.
            // Retain a safe fallback rather than failing to open the room menu.
            for (int i = 0; i < __instance.ContextMenu.Count; i++)
            {
                ContextMenuController._ContextMenu contextMenu = __instance.ContextMenu[i];
                if (contextMenu != null && contextMenu.type == agency._type.yourOffice)
                {
                    __result = contextMenu;
                    return false;
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(CM_Player), "Start", new Type[0])]
    internal static class CMPlayerStartPatch
    {
        private static void Postfix(CM_Player __instance)
        {
            if (__instance == null) return;

            FieldInfo cmcField = typeof(CM_Player).GetField("cmc", BindingFlags.NonPublic | BindingFlags.Instance);
            if (cmcField == null) return;

            ContextMenuController cmc = cmcField.GetValue(__instance) as ContextMenuController;
            if (cmc == null || cmc.room == null) return;

            bool isAssistant = AssistantManagerRules.IsAssistantManagerOfficeRoomType(cmc.room.type);
            
            if (isAssistant)
            {
                Transform existingFire = AssistantManagerRules.FindDeepChild(__instance.transform, "AssistantManager_FireButtonContainer");
                if (existingFire == null && cmc.prefab_staffCM != null)
                {
                    CM_Staff cmStaff = cmc.prefab_staffCM.GetComponent<CM_Staff>();
                    if (cmStaff != null && cmStaff.Fire != null)
                    {
                        ButtonDefault templateBtn = __instance.GetComponentsInChildren<ButtonDefault>(true).FirstOrDefault();
                        Transform targetParent = templateBtn != null ? templateBtn.transform.parent : __instance.transform;

                        GameObject cloneFire = UnityEngine.Object.Instantiate(cmStaff.Fire, targetParent, false);
                        cloneFire.name = "AssistantManager_FireButtonContainer";
                        cloneFire.transform.SetAsLastSibling();
                        
                        existingFire = cloneFire.transform;
                    }
                }

                if (existingFire != null)
                {
                    bool isStaffed = cmc.room.staffer != null;
                    existingFire.gameObject.SetActive(isStaffed);
                }
            }
            else 
            {
                Transform existingFire = AssistantManagerRules.FindDeepChild(__instance.transform, "AssistantManager_FireButtonContainer");
                if (existingFire != null)
                {
                    existingFire.gameObject.SetActive(false);
                }
            }
        }
    }

    [HarmonyPatch(typeof(CM_Player), "Render", new Type[0])]
    internal static class CMPlayerRenderPatch
    {
        private static void Postfix(CM_Player __instance)
        {
            if (__instance == null) return;

            FieldInfo cmcField = typeof(CM_Player).GetField("cmc", BindingFlags.NonPublic | BindingFlags.Instance);
            if (cmcField != null)
            {
                ContextMenuController cmc = cmcField.GetValue(__instance) as ContextMenuController;
                if (cmc != null && cmc.room != null)
                {
                    bool isAssistant = AssistantManagerRules.IsAssistantManagerOfficeRoomType(cmc.room.type);
                    bool isStaffed = cmc.room.staffer != null;

                    Transform fireBtnContainer = AssistantManagerRules.FindDeepChild(__instance.transform, "AssistantManager_FireButtonContainer");
                    if (fireBtnContainer != null)
                    {
                        fireBtnContainer.gameObject.SetActive(isAssistant && isStaffed);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.canAssign), new Type[] { typeof(data_girls.girls), typeof(Nullable<data_girls._paramType>) })]
    internal static class RoomCanAssignGirlPatch
    {
        private static bool Prefix(agency._room __instance, data_girls.girls _girl, ref bool __result)
        {
            if (!AssistantManagerRules.IsManagerOffice(__instance))
            {
                return true;
            }

            __result = __instance.staffer != null &&
                       _girl != null &&
                       __instance.girl == null &&
                       __instance.status == agency._room._status.normal &&
                       _girl.status == data_girls._status.normal &&
                       (Dating.DEBUG ||
                        (AssistantManagerRules.IsAssistantManagerOffice(__instance)
                            ? AssistantManagerDateTracking.GetCooldownDays(__instance.id, _girl.id) <= 0
                            : Dating.GetDaysCooldown(_girl) <= 0));
            return false;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.canTrain), new Type[] { typeof(data_girls.girls), typeof(Nullable<data_girls._paramType>) })]
    internal static class RoomCanTrainGirlPatch
    {
        private static bool Prefix(agency._room __instance, ref bool __result)
        {
            if (!AssistantManagerRules.IsAssistantManagerOffice(__instance))
            {
                return true;
            }

            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.assign), new Type[] { typeof(data_girls.girls), typeof(Nullable<data_girls._paramType>) })]
    internal static class RoomAssignGirlPatch
    {
        private static bool Prefix(agency._room __instance, data_girls.girls _girl)
        {
            if (!AssistantManagerRules.IsManagerOffice(__instance))
            {
                return true;
            }

            // assign() can be reached directly by a room click, bypassing drag-and-drop's
            // canAssign check.  Recheck here so two office assignments cannot win a frame race.
            if (!__instance.canAssign(_girl, null))
            {
                return false;
            }

            if (!AssistantManagerRules.IsAssistantManagerOffice(__instance))
            {
                return true;
            }

            __instance.assign_date(_girl);
            AssistantManagerRules.PlayManagerOfficeDateVoice(_girl);
            return false;
        }
    }

    [HarmonyPatch(typeof(agency._room), "assign_date", new Type[] { typeof(data_girls.girls) })]
    internal static class RoomAssignDatePatch
    {
        private static void Postfix(agency._room __instance, data_girls.girls _girl)
        {
            if (!AssistantManagerRules.IsManagerOffice(__instance) || _girl == null || __instance.girl != _girl)
            {
                return;
            }

            // The game does not provide a dedicated "on date" idol status.  Practice is the
            // standard unavailable state and therefore prevents training, scenes, or a second
            // office from using this idol until the date is resolved.
            _girl.SetStatus(data_girls._status.practice);
            _girl.room = __instance;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.GoOnDate), new Type[0])]
    internal static class RoomGoOnDatePatch
    {
        private static bool Prefix(agency._room __instance, out data_girls.girls __state)
        {
            __state = AssistantManagerRules.IsManagerOffice(__instance) ? __instance.girl : null;
            if (AssistantManagerRules.IsAssistantManagerOffice(__instance))
            {
                if (__instance.girl == null || __instance.staffer == null)
                {
                    return false;
                }

                data_girls.girls girl = __instance.girl;
                PopupManager component = Camera.main.GetComponent<mainScript>().Data.GetComponent<PopupManager>();
                Date_Popup component2 = component.GetByType(PopupManager._type.girl_date).obj.GetComponent<Date_Popup>();
                DateTime producerLastDate = girl.LastDate;
                component.Open(PopupManager._type.girl_date, true);
                component2.Set(girl);
                // Date_Popup.Set writes LastDate, which is the Producer's vanilla cooldown key.
                // Assistant Manager dates use the room-scoped tracker instead, so restore it.
                girl.LastDate = producerLastDate;

                if (__instance.staffer.LevelledUp)
                {
                    girl.addParam(data_girls._paramType.physicalStamina, 5f, false);
                    int relationshipLevel = girl.GetRelationshipLevel(Relationships_Player._type.Friendship);
                    if (relationshipLevel > 0)
                    {
                        girl.addParam(data_girls._paramType.mentalStamina, (float)relationshipLevel, false);
                    }
                }
                
                AssistantManagerDateTracking.AddDate(__instance, girl);
                girl.AddDate();

                __instance.girl = null;
                __instance.status = agency._room._status.normal;
                __instance.staffer.StopWorking(StatusButton._state.normal);
                girl.SetStatus(data_girls._status.normal);
                girl.room = null;
                return false;
            }
            return true;
        }

        private static void Postfix(agency._room __instance, data_girls.girls __state)
        {
            if (AssistantManagerRules.IsAssistantManagerOffice(__instance) || __state == null)
            {
                return;
            }

            if (AssistantManagerRules.IsManagerOffice(__instance))
            {
                __state.SetStatus(data_girls._status.normal);
                if (__state.room == __instance)
                {
                    __state.room = null;
                }
            }
        }

    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.CancelJob), new Type[0])]
    internal static class RoomCancelJobDatePatch
    {
        private static void Prefix(agency._room __instance, out data_girls.girls __state)
        {
            __state = AssistantManagerRules.IsManagerOffice(__instance) &&
                      __instance.status == agency._room._status.date
                ? __instance.girl
                : null;
        }

        private static void Postfix(agency._room __instance, data_girls.girls __state)
        {
            if (__state == null)
            {
                return;
            }

            __state.SetStatus(data_girls._status.normal);
            if (__state.room == __instance)
            {
                __state.room = null;
            }
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.CancelJob), new Type[0])]
    internal static class RoomCancelJobAuditionOwnerPatch
    {
        private static void Prefix(agency._room __instance, out Auditions.data __state)
        {
            __state = AssistantManagerRules.IsManagerOffice(__instance) ? __instance.auditionData : null;
        }

        private static void Postfix(Auditions.data __state)
        {
            AssistantManagerAuditionTracking.ForgetOwner(__state);
        }
    }

    [HarmonyPatch(typeof(Room), nameof(Room.SetTitle_Date), new Type[] { typeof(data_girls.girls) })]
    internal static class RoomSetTitleDatePatch
    {
        private static bool Prefix(Room __instance, data_girls.girls Girl)
        {
            if (__instance == null || !AssistantManagerRules.IsAssistantManagerOffice(__instance.room))
            {
                return true;
            }

            int daysCooldown = AssistantManagerDateTracking.GetCooldownDays(__instance.room.id, Girl.id);
            string str;
            if (daysCooldown > 1)
            {
                str = Language.Insert("AGENCY__DAYS_LEFT", new string[] { daysCooldown.ToString() });
            }
            else
            {
                str = Language.Data["AGENCY__DAY_LEFT"];
            }
            
            __instance.ShowTitle(ExtensionMethods.color(str, mainScript.red), null);
            return false;
        }
    }

    [HarmonyPatch(typeof(DragAndDropManager), nameof(DragAndDropManager.OnMouseDown), new Type[] { typeof(data_girls.girls), typeof(staticVars.DragAndDrop) })]
    internal static class DragAndDropManagerDateCooldownTitlePatch
    {
        private static void Postfix(data_girls.girls girl, staticVars.DragAndDrop type)
        {
            if (girl == null || type != staticVars.DragAndDrop.room)
            {
                return;
            }

            agency agencyInstance = AssistantManagerRules.GetAgency();
            if (agencyInstance == null)
            {
                return;
            }

            List<agency._room> rooms = agencyInstance.allRooms(true, true);
            for (int i = 0; i < rooms.Count; i++)
            {
                agency._room room = rooms[i];
                if (!AssistantManagerRules.IsAssistantManagerOffice(room) ||
                    room.status != agency._room._status.normal ||
                    AssistantManagerDateTracking.GetCooldownDays(room.id, girl.id) <= 0 ||
                    room.roomObj == null)
                {
                    continue;
                }

                Room roomView = room.roomObj.GetComponent<Room>();
                if (roomView != null)
                {
                    roomView.SetTitle_Date(girl);
                }
            }
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.Auditions), new Type[] { typeof(Auditions.data) })]
    internal static class RoomAuditionsPatch
    {
        private static bool Prefix(agency._room __instance, ref Auditions.data _data)
        {
            if (!AssistantManagerRules.IsManagerOffice(__instance))
            {
                return true;
            }

            if (__instance.staffer == null ||
                __instance.status != agency._room._status.normal ||
                _data == null ||
                !AssistantManagerAuditionTracking.CanProduce(__instance, _data.Type))
            {
                return false;
            }

            // The menu's Auditions.data is a shared template.  Each office must own a separate
            // instance so progress and generated candidates cannot overwrite another office.
            _data = AssistantManagerAuditionTracking.CreateRoomAudition(__instance, _data);
            return _data != null;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.ShowAuditions), new Type[0])]
    internal static class RoomShowAuditionsPatch
    {
        private static bool Prefix(agency._room __instance)
        {
            if (!AssistantManagerRules.IsManagerOffice(__instance))
            {
                return true;
            }

            if (__instance.auditionData == null || !__instance.auditionData.CanLaunch())
            {
                return false;
            }

            // PopupManager has one audition popup and one blur/input-lock state.  Leave later
            // completed auditions ready on their own office rather than enqueueing a second UI.
            return !AssistantManagerAuditionTracking.IsAuditionPopupBusy();
        }
    }

    [HarmonyPatch(typeof(Auditions), nameof(Auditions.GenerateAudition), new Type[] { typeof(Auditions.data), typeof(bool) })]
    internal static class AuditionsGenerateAuditionPatch
    {
        private static void Prefix(Auditions.data _data, out AssistantManagerAuditionTracking.GenerateState __state)
        {
            AssistantManagerAuditionTracking.BeginGenerate(_data, out __state);
        }

        private static void Postfix(AssistantManagerAuditionTracking.GenerateState __state)
        {
            AssistantManagerAuditionTracking.EndGenerate(__state);
        }
    }

    [HarmonyPatch(typeof(Auditions), nameof(Auditions.CanProduce), new Type[] { typeof(Auditions.type) })]
    internal static class AuditionsCanProducePatch
    {
        private static bool Prefix(Auditions.type _type, ref bool __result)
        {
            agency._room room = AssistantManagerAuditionTracking.GetCurrentManagerOffice();
            if (room == null)
            {
                return true;
            }

            __result = AssistantManagerAuditionTracking.CanProduce(room, _type);
            return false;
        }
    }

    [HarmonyPatch(typeof(Auditions), nameof(Auditions.GetDaysTillCanProduce), new Type[] { typeof(Auditions.type) })]
    internal static class AuditionsGetDaysTillCanProducePatch
    {
        private static bool Prefix(Auditions.type _type, ref int __result)
        {
            agency._room room = AssistantManagerAuditionTracking.GetCurrentManagerOffice();
            if (room == null)
            {
                return true;
            }

            __result = AssistantManagerAuditionTracking.GetDaysTillCanProduce(room, _type);
            return false;
        }
    }

    [HarmonyPatch(typeof(Auditions), nameof(Auditions.GetCooldownCost), new Type[] { typeof(CM_Player_Audition_Cooldown._type) })]
    internal static class AuditionsGetCooldownCostPatch
    {
        private static bool Prefix(CM_Player_Audition_Cooldown._type _type, ref int __result)
        {
            agency._room room = AssistantManagerAuditionTracking.GetCurrentManagerOffice();
            if (room == null)
            {
                return true;
            }

            __result = AssistantManagerAuditionTracking.GetCooldownCost(room, _type);
            return false;
        }
    }

    [HarmonyPatch(typeof(Auditions), nameof(Auditions.EnoughResourcesForCooldown), new Type[] { typeof(CM_Player_Audition_Cooldown._type) })]
    internal static class AuditionsEnoughResourcesForCooldownPatch
    {
        private static bool Prefix(CM_Player_Audition_Cooldown._type _type, ref bool __result)
        {
            agency._room room = AssistantManagerAuditionTracking.GetCurrentManagerOffice();
            if (room == null)
            {
                return true;
            }

            __result = AssistantManagerAuditionTracking.EnoughResourcesForCooldown(room, _type);
            return false;
        }
    }

    [HarmonyPatch(typeof(Auditions), nameof(Auditions.IsOnCooldown), new Type[0])]
    internal static class AuditionsIsOnCooldownPatch
    {
        private static bool Prefix(ref bool __result)
        {
            agency._room room = AssistantManagerAuditionTracking.GetCurrentManagerOffice();
            if (room == null)
            {
                return true;
            }

            __result = AssistantManagerAuditionTracking.IsOnCooldown(room);
            return false;
        }
    }

    [HarmonyPatch(typeof(Auditions), nameof(Auditions.CanResetCooldown), new Type[] { typeof(CM_Player_Audition_Cooldown._type) })]
    internal static class AuditionsCanResetCooldownPatch
    {
        private static bool Prefix(CM_Player_Audition_Cooldown._type _type, ref bool __result)
        {
            agency._room room = AssistantManagerAuditionTracking.GetCurrentManagerOffice();
            if (room == null)
            {
                return true;
            }

            __result = AssistantManagerAuditionTracking.CanResetCooldown(room, _type);
            return false;
        }
    }

    [HarmonyPatch(typeof(Auditions), nameof(Auditions.ResetCooldown), new Type[] { typeof(CM_Player_Audition_Cooldown._type) })]
    internal static class AuditionsResetCooldownPatch
    {
        private static bool Prefix(CM_Player_Audition_Cooldown._type _type)
        {
            agency._room room = AssistantManagerAuditionTracking.GetCurrentManagerOffice();
            if (room == null)
            {
                return true;
            }

            AssistantManagerAuditionTracking.ResetCooldown(room, _type);
            return false;
        }
    }

    [HarmonyPatch(typeof(CM_Player_Audition_Button), "UpdateData", new Type[0])]
    internal static class CMAuditionButtonUpdateDataPatch
    {
        private static bool Prefix(CM_Player_Audition_Button __instance)
        {
            agency._room room = AssistantManagerAuditionTracking.GetCurrentManagerOffice();
            if (room == null)
            {
                return true;
            }

            float progress = 0f;
            if (room.auditionData != null && room.auditionData.Type == __instance.level)
            {
                progress = room.auditionData.Progress;
            }
            __instance.GetComponent<ContextMenuButton>().SetFill(progress);
            return false;
        }
    }

    [HarmonyPatch(typeof(agency._room), "DrawSpriteGirl", new Type[] { typeof(bool) })]
    internal static class RoomDrawSpriteGirlPatch
    {
        private static bool Prefix(agency._room __instance)
        {
            return !AssistantManagerRules.IsAssistantManagerOffice(__instance);
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.singleParamType), new Type[] { typeof(singles._single) })]
    internal static class RoomSingleParamTypePatch
    {
        private static bool Prefix(agency._room __instance, singles._single _single, ref singles._single._param._type __result)
        {
            if (!AssistantManagerRules.IsAssistantManagerOffice(__instance))
            {
                return true;
            }

            __result = AssistantManagerRules.GetManagerOfficeSingleParamType(_single);
            return false;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.showParamType), new Type[] { typeof(Shows._show) })]
    internal static class RoomShowParamTypePatch
    {
        private static bool Prefix(agency._room __instance, Shows._show __0, ref Shows._show._progressable._type __result)
        {
            if (!AssistantManagerRules.IsAssistantManagerOffice(__instance))
            {
                return true;
            }

            __result = AssistantManagerRules.GetManagerOfficeShowParamType(__0);
            return false;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.SSKParamType), new Type[] { typeof(SEvent_SSK._SSK) })]
    internal static class RoomSskParamTypePatch
    {
        private static bool Prefix(agency._room __instance, SEvent_SSK._SSK __0, ref SEvent_SSK._SSK._progressable._type __result)
        {
            if (!AssistantManagerRules.IsAssistantManagerOffice(__instance))
            {
                return true;
            }

            __result = AssistantManagerRules.GetManagerOfficeSskParamType(__0);
            return false;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.TourParamType), new Type[] { typeof(SEvent_Tour.tour) })]
    internal static class RoomTourParamTypePatch
    {
        private static bool Prefix(agency._room __instance, SEvent_Tour.tour __0, ref SEvent_Tour.tour._progressable._type __result)
        {
            if (!AssistantManagerRules.IsAssistantManagerOffice(__instance))
            {
                return true;
            }

            __result = AssistantManagerRules.GetManagerOfficeTourParamType(__0);
            return false;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.concertParamType), new Type[] { typeof(SEvent_Concerts._concert) })]
    internal static class RoomConcertParamTypePatch
    {
        private static bool Prefix(agency._room __instance, SEvent_Concerts._concert __0, ref SEvent_Concerts._concert._progressable._type __result)
        {
            if (!AssistantManagerRules.IsAssistantManagerOffice(__instance))
            {
                return true;
            }

            __result = AssistantManagerRules.GetManagerOfficeConcertParamType(__0);
            return false;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.GetTitle_Tour), new Type[0])]
    internal static class RoomGetTitleTourPatch
    {
        private static bool Prefix(agency._room __instance, ref string __result)
        {
            if (!AssistantManagerRules.IsAssistantManagerOffice(__instance))
            {
                return true;
            }

            __result = Language.Data["AGENCY__PRODUCTION"];
            return false;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.GetTitle_Tour), new Type[] { typeof(SEvent_SSK._SSK) })]
    internal static class RoomGetTitleTourSskPatch
    {
        private static bool Prefix(agency._room __instance, SEvent_SSK._SSK __0, ref string __result)
        {
            if (!AssistantManagerRules.IsAssistantManagerOffice(__instance))
            {
                return true;
            }

            SEvent_SSK._SSK._progressable._type type = AssistantManagerRules.GetManagerOfficeSskParamType(__0);
            __result = type == SEvent_SSK._SSK._progressable._type.production
                ? Language.Data["AGENCY__PRODUCTION"]
                : Language.Data["AGENCY__LOGISTICS"];
            return false;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.GetTitle_Tour), new Type[] { typeof(SEvent_Tour.tour) })]
    internal static class RoomGetTitleTourTourPatch
    {
        private static bool Prefix(agency._room __instance, SEvent_Tour.tour __0, ref string __result)
        {
            if (!AssistantManagerRules.IsAssistantManagerOffice(__instance))
            {
                return true;
            }

            SEvent_Tour.tour._progressable._type type = AssistantManagerRules.GetManagerOfficeTourParamType(__0);
            __result = type == SEvent_Tour.tour._progressable._type.production
                ? Language.Data["AGENCY__PRODUCTION"]
                : Language.Data["AGENCY__LOGISTICS"];
            return false;
        }
    }

    [HarmonyPatch(typeof(agency._room), "DoSingleProduction", new Type[0])]
    internal static class RoomDoSingleProductionPatch
    {
        private static bool Prefix(agency._room __instance)
        {
            if (!AssistantManagerRules.IsAssistantManagerOffice(__instance))
            {
                return true;
            }

            singles._single._param._type type = __instance.singleParamType(__instance.single);
            __instance.single.SetParamProgress(type, __instance.Progress);
            if (__instance.finishTime < staticVars.dateTime)
            {
                __instance.status = agency._room._status.normal;
                float points = __instance.pointsToAdd;
                if (type != singles._single._param._type.lyrics)
                {
                    points = AssistantManagerRules.ApplyManagerOfficeSecondaryTaskPenalty(points);
                }

                __instance.single.AddToParam(type, points);
                __instance.single.SetParamProgress(type, 0f);
                __instance.single.SetStatus(singles._single._status.normal);
                Event_Overlord.OnSingleWorkComplete(__instance.single, type);
                __instance.single = null;
                AssistantManagerRules.AddStaffFloat(__instance, __instance.singleStateComplete(type), true, null);
                if (__instance.staffer != null)
                {
                    __instance.staffer.StopWorking(StatusButton._state.normal);
                }

                __instance.ResumeTraining();
                AssistantManagerRules.OnTaskComplete(__instance);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(agency._room), "DoShowProduction", new Type[0])]
    internal static class RoomDoShowProductionPatch
    {
        private static bool Prefix(agency._room __instance)
        {
            if (!AssistantManagerRules.IsAssistantManagerOffice(__instance))
            {
                return true;
            }

            Shows._show._progressable._type type = __instance.showParamType(__instance.show);
            __instance.show.SetParamProgress(type, __instance.Progress);
            if (__instance.finishTime < staticVars.dateTime)
            {
                __instance.status = agency._room._status.normal;
                float points = __instance.pointsToAdd;
                if (type != Shows._show._progressable._type.concept)
                {
                    points = AssistantManagerRules.ApplyManagerOfficeSecondaryTaskPenalty(points);
                }

                __instance.show.AddToParam(type, points);
                __instance.show.SetParamProgress(type, 0f);
                if (__instance.show.status == Shows._show._status.relaunching_working)
                {
                    __instance.show.SetStatus(Shows._show._status.relaunching);
                }
                else
                {
                    __instance.show.SetStatus(Shows._show._status.normal);
                }

                __instance.show = null;
                AssistantManagerRules.AddStaffFloat(__instance, __instance.showStateComplete(type), true, null);
                if (__instance.staffer != null)
                {
                    __instance.staffer.StopWorking(StatusButton._state.normal);
                }

                AssistantManagerRules.OnTaskComplete(__instance);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(agency._room), "DoSSKProduction", new Type[0])]
    internal static class RoomDoSskProductionPatch
    {
        private static bool Prefix(agency._room __instance)
        {
            if (!AssistantManagerRules.IsAssistantManagerOffice(__instance))
            {
                return true;
            }

            SEvent_SSK._SSK._progressable._type type = __instance.SSKParamType(__instance.SSK);
            __instance.SSK.SetParamProgress(type, __instance.Progress);
            if (__instance.finishTime < staticVars.dateTime)
            {
                __instance.status = agency._room._status.normal;
                float points = __instance.pointsToAdd;
                if (type != SEvent_SSK._SSK._progressable._type.production)
                {
                    points = AssistantManagerRules.ApplyManagerOfficeSecondaryTaskPenalty(points);
                }

                __instance.SSK.AddToParam(type, points);
                __instance.SSK.SetParamProgress(type, 0f);
                __instance.SSK.SetStatus(SEvent_Tour.tour._status.normal);
                __instance.SSK = null;
                AssistantManagerRules.AddStaffFloat(__instance, __instance.SSKStateComplete(type), true, null);
                if (__instance.staffer != null)
                {
                    __instance.staffer.StopWorking(StatusButton._state.normal);
                }

                AssistantManagerRules.OnTaskComplete(__instance);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(agency._room), "DoTourProduction", new Type[0])]
    internal static class RoomDoTourProductionPatch
    {
        private static bool Prefix(agency._room __instance)
        {
            if (!AssistantManagerRules.IsAssistantManagerOffice(__instance))
            {
                return true;
            }

            SEvent_Tour.tour._progressable._type type = __instance.TourParamType(__instance.tour);
            __instance.tour.SetParamProgress(type, __instance.Progress);
            if (__instance.finishTime < staticVars.dateTime)
            {
                __instance.status = agency._room._status.normal;
                float points = __instance.pointsToAdd;
                if (type != SEvent_Tour.tour._progressable._type.production)
                {
                    points = AssistantManagerRules.ApplyManagerOfficeSecondaryTaskPenalty(points);
                }

                __instance.tour.AddToParam(type, points);
                __instance.tour.SetParamProgress(type, 0f);
                __instance.tour.SetStatus(SEvent_Tour.tour._status.normal);
                __instance.tour = null;
                AssistantManagerRules.AddStaffFloat(__instance, __instance.TourStateComplete(type), true, null);
                if (__instance.staffer != null)
                {
                    __instance.staffer.StopWorking(StatusButton._state.normal);
                }

                AssistantManagerRules.OnTaskComplete(__instance);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(agency._room), "DoConcertProduction", new Type[0])]
    internal static class RoomDoConcertProductionPatch
    {
        private static bool Prefix(agency._room __instance)
        {
            if (!AssistantManagerRules.IsAssistantManagerOffice(__instance))
            {
                return true;
            }

            SEvent_Concerts._concert._progressable._type type = __instance.concertParamType(__instance.concert);
            __instance.concert.SetParamProgress(type, __instance.Progress);
            if (__instance.finishTime < staticVars.dateTime)
            {
                __instance.status = agency._room._status.normal;
                float points = __instance.pointsToAdd;
                if (type != SEvent_Concerts._concert._progressable._type.production)
                {
                    points = AssistantManagerRules.ApplyManagerOfficeSecondaryTaskPenalty(points);
                }

                __instance.concert.AddToParam(type, points);
                __instance.concert.SetParamProgress(type, 0f);
                __instance.concert.SetStatus(SEvent_Tour.tour._status.normal);
                Event_Overlord.OnConcertWorkComplete(__instance.concert);
                __instance.concert = null;
                AssistantManagerRules.AddStaffFloat(__instance, __instance.ConcertStateComplete(type), true, null);
                if (__instance.staffer != null)
                {
                    __instance.staffer.StopWorking(StatusButton._state.normal);
                }

                __instance.ResumeTraining();
                AssistantManagerRules.OnTaskComplete(__instance);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(singles._single), nameof(singles._single.AssignableRoomType), new Type[] { typeof(agency._type) })]
    internal static class SingleAssignableRoomTypePatch
    {
        private static void Postfix(agency._type roomType, ref bool __result)
        {
            if (AssistantManagerRules.IsAssistantManagerOfficeRoomType(roomType))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Shows._show), nameof(Shows._show.AssignableRoomType), new Type[] { typeof(agency._type) })]
    internal static class ShowAssignableRoomTypePatch
    {
        private static void Postfix(agency._type roomType, ref bool __result)
        {
            if (AssistantManagerRules.IsAssistantManagerOfficeRoomType(roomType))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(SEvent_SSK._SSK), nameof(SEvent_SSK._SSK.AssignableRoomType), new Type[] { typeof(agency._type) })]
    internal static class SskAssignableRoomTypePatch
    {
        private static void Postfix(agency._type roomType, ref bool __result)
        {
            if (AssistantManagerRules.IsAssistantManagerOfficeRoomType(roomType))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(SEvent_Tour.tour), nameof(SEvent_Tour.tour.AssignableRoomType), new Type[] { typeof(agency._type) })]
    internal static class TourAssignableRoomTypePatch
    {
        private static void Postfix(agency._type roomType, ref bool __result)
        {
            if (AssistantManagerRules.IsAssistantManagerOfficeRoomType(roomType))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(SEvent_Concerts._concert), nameof(SEvent_Concerts._concert.AssignableRoomType), new Type[] { typeof(agency._type) })]
    internal static class ConcertAssignableRoomTypePatch
    {
        private static void Postfix(agency._type roomType, ref bool __result)
        {
            if (AssistantManagerRules.IsAssistantManagerOfficeRoomType(roomType))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Room), nameof(Room.SetTitle_Single), new Type[] { typeof(bool), typeof(singles._single) })]
    internal static class RoomSetTitleSinglePatch
    {
        private static bool Prefix(Room __instance, bool __0, singles._single __1)
        {
            if (__instance == null || !AssistantManagerRules.IsAssistantManagerOffice(__instance.room))
            {
                return true;
            }

            string text = __instance.room.GetTitle_Single(__1);
            if (__instance.room.staffer != null)
            {
                float points = AssistantManagerRules.GetManagerOfficeTaskPoints(__instance.room, staff._skill_type.production);
                if (__1 != null && __1.GetParam(singles._single._param._type.lyrics).val != 0f)
                {
                    points = AssistantManagerRules.ApplyManagerOfficeSecondaryTaskPenalty(points);
                }

                text = text + ": " + ExtensionMethods.formatNumber(Mathf.RoundToInt(points), false, false);
            }

            __instance.ShowTitle(ExtensionMethods.color(text, __0 ? mainScript.red : mainScript.green), null);
            return false;
        }
    }

    [HarmonyPatch(typeof(Room), nameof(Room.SetTitle_Show), new Type[] { typeof(bool), typeof(Shows._show) })]
    internal static class RoomSetTitleShowPatch
    {
        private static bool Prefix(Room __instance, bool __0, Shows._show __1)
        {
            if (__instance == null || !AssistantManagerRules.IsAssistantManagerOffice(__instance.room))
            {
                return true;
            }

            string text = __instance.room.GetTitle_Show(__1);
            if (__instance.room.staffer != null)
            {
                float points = AssistantManagerRules.GetManagerOfficeTaskPoints(__instance.room, staff._skill_type.production);
                if (__1 != null && __1.GetParam(Shows._show._progressable._type.concept).val != 0f)
                {
                    points = AssistantManagerRules.ApplyManagerOfficeSecondaryTaskPenalty(points);
                }

                text = text + ": " + ExtensionMethods.formatNumber(Mathf.RoundToInt(points), false, false);
            }

            __instance.ShowTitle(ExtensionMethods.color(text, __0 ? mainScript.red : mainScript.green), null);
            return false;
        }
    }

    [HarmonyPatch(typeof(Room), nameof(Room.SetTitle_Tour), new Type[] { typeof(bool), typeof(SEvent_SSK._SSK), typeof(SEvent_Tour.tour) })]
    internal static class RoomSetTitleTourPatch
    {
        private static bool Prefix(Room __instance, bool __0, SEvent_SSK._SSK __1, SEvent_Tour.tour __2)
        {
            if (__instance == null || !AssistantManagerRules.IsAssistantManagerOffice(__instance.room))
            {
                return true;
            }

            string text;
            if (__2 != null)
            {
                text = __instance.room.GetTitle_Tour(__2);
            }
            else if (__1 != null)
            {
                text = __instance.room.GetTitle_Tour(__1);
            }
            else
            {
                text = __instance.room.GetTitle_Tour();
            }

            if (__instance.room.staffer != null)
            {
                float points = AssistantManagerRules.GetManagerOfficeTaskPoints(__instance.room, staff._skill_type.production);
                if (__1 != null && __1.GetParam(SEvent_SSK._SSK._progressable._type.production).val != 0f)
                {
                    points = AssistantManagerRules.ApplyManagerOfficeSecondaryTaskPenalty(points);
                }

                if (__2 != null && __2.GetParam(SEvent_Tour.tour._progressable._type.production).val != 0f)
                {
                    points = AssistantManagerRules.ApplyManagerOfficeSecondaryTaskPenalty(points);
                }

                text = text + ": " + ExtensionMethods.formatNumber(Mathf.RoundToInt(points), false, false);
            }

            __instance.ShowTitle(ExtensionMethods.color(text, __0 ? mainScript.red : mainScript.green), null);
            return false;
        }
    }

    [HarmonyPatch(typeof(Room), nameof(Room.SetTitle_Concert), new Type[] { typeof(bool), typeof(SEvent_Concerts._concert) })]
    internal static class RoomSetTitleConcertPatch
    {
        private static bool Prefix(Room __instance, bool __0, SEvent_Concerts._concert __1)
        {
            if (__instance == null || !AssistantManagerRules.IsAssistantManagerOffice(__instance.room))
            {
                return true;
            }

            string text = __instance.room.GetTitle_Concert(__1);
            if (__instance.room.staffer != null)
            {
                float points = AssistantManagerRules.GetManagerOfficeTaskPoints(__instance.room, staff._skill_type.production);
                if (__1 != null && __1.GetParam(SEvent_Concerts._concert._progressable._type.production).val != 0f)
                {
                    points = AssistantManagerRules.ApplyManagerOfficeSecondaryTaskPenalty(points);
                }

                text = text + ": " + ExtensionMethods.formatNumber(Mathf.RoundToInt(points), false, false);
            }

            __instance.ShowTitle(ExtensionMethods.color(text, __0 ? mainScript.red : mainScript.green), null);
            return false;
        }
    }

    [HarmonyPatch(typeof(loans), nameof(loans.AddLoan), new Type[] { typeof(loans._loan) })]
    internal static class LoansAddLoanPatch
    {
        private static bool Prefix(loans._loan __0)
        {
            if (__0 == null)
            {
                return false;
            }

            if (__0.GetDaysToDevelop() == 0)
            {
                loans.Loans.Add(__0);
                __0.Initialize();
                return false;
            }

            agency._room targetRoom = AssistantManagerRules.FindFirstManagerOffice(true, true);
            if (targetRoom == null)
            {
                // Developing a loan must not cancel an unrelated office task.  The loan popup
                // already requires an idle office, and direct callers now fail safely as well.
                return false;
            }

            loans.Loans.Add(__0);
            targetRoom.assign(__0);

            NotificationManager.AddNotification(
                ExtensionMethods.color(Language.Data["LOANS"].ToUpper(), mainScript.green) + "\n" + Language.Data["LOANS__IN_DEV"],
                mainScript.green32,
                NotificationManager._notification._type.other);
            return false;
        }
    }

    [HarmonyPatch(typeof(Loans_Popup), "Validate", new Type[0])]
    internal static class LoansPopupValidatePatch
    {
        private static void Postfix(Loans_Popup __instance)
        {
            if (__instance == null || __instance.Loan == null || __instance.Button_Take == null)
            {
                return;
            }

            if (__instance.Loan.GetDaysToDevelop() == 0)
            {
                return;
            }

            agency._room availableRoom = AssistantManagerRules.FindFirstManagerOffice(true, true);
            if (availableRoom == null)
            {
                __instance.Button_Take.GetComponent<ButtonDefault>().Activate(false, false);
                if (__instance.Warning != null)
                {
                    ExtensionMethods.SetText(__instance.Warning, ExtensionMethods.color(AssistantManagerText.NoAvailableLoanRoom, mainScript.red));
                }
                return;
            }

            bool blockedFujimotoLoan =
                __instance.Loan.Type == loans._loan._type.fujimoto &&
                (variables.Get("NO_FUJI_LOANS") == "true" || tasks.AreThereUnfinishedFujimotoLoanTasks());

            if (__instance.Loan.Amount > 0L && !blockedFujimotoLoan)
            {
                __instance.Button_Take.GetComponent<ButtonDefault>().Activate(true, false);
            }
        }
    }
}
