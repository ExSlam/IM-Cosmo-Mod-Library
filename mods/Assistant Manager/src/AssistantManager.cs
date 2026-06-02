using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
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
        internal const string AssistantManagerHireButtonObjectName = "AssistantManager_StaffHireButton";
        internal const string MalePlayerPortraitFolderName = "Player_Male";
        internal const string FemalePlayerPortraitFolderName = "Player_Female";

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
                    "Assistant Manager Office");
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
        private static Dictionary<int, Dictionary<int, DateTime>> roomGirlCooldowns = new Dictionary<int, Dictionary<int, DateTime>>();

        internal static int GetCooldownDays(int roomId, int girlId)
        {
            if (roomGirlCooldowns.TryGetValue(roomId, out var girlCooldowns))
            {
                if (girlCooldowns.TryGetValue(girlId, out DateTime cooldownDate))
                {
                    int daysLeft = Mathf.CeilToInt((float)(cooldownDate - staticVars.dateTime).TotalDays);
                    return Mathf.Max(0, daysLeft);
                }
            }
            return 0;
        }

        internal static void AddDate(int roomId, int girlId)
        {
            if (!roomGirlCooldowns.ContainsKey(roomId))
            {
                roomGirlCooldowns[roomId] = new Dictionary<int, DateTime>();
            }
            roomGirlCooldowns[roomId][girlId] = staticVars.dateTime.AddDays(7.0);
        }
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

        internal static void EnsureBuildMenuButton(agency agencyInstance)
        {
            if (agencyInstance == null || agencyInstance.newRoomPopup == null)
            {
                return;
            }

            BuildRoomButton[] buttons = agencyInstance.newRoomPopup.GetComponentsInChildren<BuildRoomButton>(true);
            BuildRoomButton managerOfficeBuildButton = null;
            BuildRoomButton theaterBuildButton = null;

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;

                if (buttons[i].type == AssistantManagerConstants.AssistantManagerOfficeRoomType)
                {
                    ApplyAssistantManagerOfficeBuildButtonPresentation(buttons[i], agencyInstance);
                    return;
                }

                if (buttons[i].type == agency._type.yourOffice) managerOfficeBuildButton = buttons[i];
                if (buttons[i].type == agency._type.theatre) theaterBuildButton = buttons[i];
            }

            if (managerOfficeBuildButton == null)
            {
                return;
            }

            GameObject clone = UnityEngine.Object.Instantiate<GameObject>(
                managerOfficeBuildButton.gameObject,
                managerOfficeBuildButton.transform.parent,
                false);
            clone.name = AssistantManagerConstants.AssistantManagerOfficeBuildButtonObjectName;
            BuildRoomButton buildRoomButton = clone.GetComponent<BuildRoomButton>();
            if (buildRoomButton == null)
            {
                return;
            }

            if (theaterBuildButton != null)
            {
                clone.transform.SetParent(theaterBuildButton.transform.parent, false);
                clone.transform.SetSiblingIndex(theaterBuildButton.transform.GetSiblingIndex() + 1);
            }
            else
            {
                clone.transform.SetAsLastSibling();
            }

            ApplyAssistantManagerOfficeBuildButtonPresentation(buildRoomButton, agencyInstance);
        }

        internal static void EnsureStaffHireButton(Staff_Hire_Popup popup)
        {
            if (popup == null || popup.Container == null) return;

            Staff_Hire_Button[] buttons = popup.Container.GetComponentsInChildren<Staff_Hire_Button>(true);
            Staff_Hire_Button cloneSource = null;
            bool foundType1 = false;
            bool foundType2 = false;

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;

                if (buttons[i].Type == AssistantManagerConstants.AssistantManagerStaffType)
                {
                    buttons[i].Set(popup.Expertise);
                    ApplyAssistantManagerHireButtonPresentation(buttons[i]);
                    foundType1 = true;
                }
                else if (buttons[i].Type == AssistantManagerConstants.AssistantManagerStaffType2)
                {
                    buttons[i].Set(popup.Expertise);
                    ApplyAssistantManagerHireButtonPresentation(buttons[i]);
                    foundType2 = true;
                }
                else if (buttons[i].Type == staff._type.production_manager)
                {
                    cloneSource = buttons[i];
                }
            }

            if (foundType1 && foundType2) return;
            if (cloneSource == null) return;

            Transform sourceRow = cloneSource.transform.parent;
            Transform existingRow = sourceRow.parent.Find("AssistantManagerRow");
            GameObject newRow;
            
            if (existingRow != null)
            {
                newRow = existingRow.gameObject;
            }
            else
            {
                newRow = UnityEngine.Object.Instantiate<GameObject>(sourceRow.gameObject, sourceRow.parent, false);
                newRow.name = "AssistantManagerRow";
                newRow.transform.SetAsLastSibling();
            }

            Staff_Hire_Button[] newButtons = newRow.GetComponentsInChildren<Staff_Hire_Button>(true);
            
            // We expect standard cloned rows to have at least 2 buttons (like production & sales manager).
            if (newButtons.Length >= 2)
            {
                Staff_Hire_Button hireButton1 = newButtons[0];
                Staff_Hire_Button hireButton2 = newButtons[1];
                
                // Ensure only 2 variants exist
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

        internal static void ApplyAssistantManagerOfficeBuildButtonPresentation(BuildRoomButton buildRoomButton, agency agencyInstance)
        {
            if (buildRoomButton == null)
            {
                return;
            }

            buildRoomButton.type = AssistantManagerConstants.AssistantManagerOfficeRoomType;
            buildRoomButton.available = true;
            SetFirstTextComponent(buildRoomButton.gameObject, AssistantManagerText.AssistantManagerOfficeTitle);
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
                if (footerCanvasGroup != null && !canHire)
                {
                    footerCanvasGroup.alpha = 0f;
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

            TextMeshProUGUI tmpText = root.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmpText != null)
            {
                tmpText.text = text;
                return;
            }

            Text unityText = root.GetComponentInChildren<Text>(true);
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

        // --- Context Menu UI Extractor & Injector Helper Methods ---
        
        internal static GameObject GetContextMenuGameObject(ContextMenuController._ContextMenu contextMenu)
        {
            if (contextMenu == null) return null;
            
            string[] fieldNames = { "obj", "gameObject", "menu", "Container", "panel" };
            foreach (string fieldName in fieldNames)
            {
                FieldInfo field = typeof(ContextMenuController._ContextMenu).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    object val = field.GetValue(contextMenu);
                    if (val is GameObject go) return go;
                    if (val is Component comp) return comp.gameObject;
                }
            }
            return null;
        }

        internal static void InjectFireButtons(ContextMenuController._ContextMenu yourOfficeMenu, ContextMenuController._ContextMenu officeMenu)
        {
            GameObject yourOfficeGo = GetContextMenuGameObject(yourOfficeMenu);
            GameObject officeGo = GetContextMenuGameObject(officeMenu);

            if (yourOfficeGo == null || officeGo == null) return;

            ButtonDefault[] yourOfficeButtons = yourOfficeGo.GetComponentsInChildren<ButtonDefault>(true);
            Transform targetParent = yourOfficeGo.transform;
            if (yourOfficeButtons.Length > 0)
            {
                targetParent = yourOfficeButtons[yourOfficeButtons.Length - 1].transform.parent;
            }

            Transform existingFire = targetParent.Find("AssistantManager_FireButton");
            if (existingFire != null) return;

            ButtonDefault[] officeButtons = officeGo.GetComponentsInChildren<ButtonDefault>(true);
            if (officeButtons.Length >= 2)
            {
                // We grab the last two standard buttons in the Office menu: Fire and Fire(Severance)
                ButtonDefault fireBtnSrc = officeButtons[officeButtons.Length - 2];
                ButtonDefault fireSevBtnSrc = officeButtons[officeButtons.Length - 1];

                GameObject cloneFire = UnityEngine.Object.Instantiate(fireBtnSrc.gameObject, targetParent, false);
                cloneFire.name = "AssistantManager_FireButton";
                cloneFire.transform.SetAsLastSibling();

                GameObject cloneSev = UnityEngine.Object.Instantiate(fireSevBtnSrc.gameObject, targetParent, false);
                cloneSev.name = "AssistantManager_FireSeveranceButton";
                cloneSev.transform.SetAsLastSibling();
            }
        }

        internal static void ToggleInjectedFireButtons(ContextMenuController controller, bool show)
        {
            if (controller == null || controller.ContextMenu == null) return;

            ContextMenuController._ContextMenu yourOfficeMenu = null;
            for (int i = 0; i < controller.ContextMenu.Count; i++)
            {
                if (controller.ContextMenu[i] != null && controller.ContextMenu[i].type == agency._type.yourOffice)
                {
                    yourOfficeMenu = controller.ContextMenu[i];
                    break;
                }
            }

            GameObject yourOfficeGo = GetContextMenuGameObject(yourOfficeMenu);
            if (yourOfficeGo != null)
            {
                Transform fireBtn = FindDeepChild(yourOfficeGo.transform, "AssistantManager_FireButton");
                Transform fireSevBtn = FindDeepChild(yourOfficeGo.transform, "AssistantManager_FireSeveranceButton");

                if (fireBtn != null) fireBtn.gameObject.SetActive(show);
                if (fireSevBtn != null) fireSevBtn.gameObject.SetActive(show);
            }
        }

        private static Transform FindDeepChild(Transform parent, string name)
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
            AssistantManagerRules.RegisterAssistantManagerOfficePrefab(__instance);
            AssistantManagerRules.EnsureBuildMenuButton(__instance);
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
            if (__instance == null || __instance.type != AssistantManagerConstants.AssistantManagerOfficeRoomType)
            {
                return;
            }

            AssistantManagerRules.ApplyAssistantManagerOfficeBuildButtonPresentation(__instance, null);
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

            ContextMenuController._ContextMenu yourOfficeMenu = null;
            ContextMenuController._ContextMenu officeMenu = null;

            for (int i = 0; i < __instance.ContextMenu.Count; i++)
            {
                ContextMenuController._ContextMenu contextMenu = __instance.ContextMenu[i];
                if (contextMenu == null) continue;

                if (contextMenu.type == agency._type.yourOffice) yourOfficeMenu = contextMenu;
                if (contextMenu.type == agency._type.office) officeMenu = contextMenu;
            }

            // Return the yourOffice menu, but inject the fire buttons if they aren't there yet.
            if (yourOfficeMenu != null)
            {
                AssistantManagerRules.InjectFireButtons(yourOfficeMenu, officeMenu);
                __result = yourOfficeMenu;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(ContextMenuController), "Show", new Type[] { typeof(agency._room) })]
    internal static class ContextMenuControllerShowPatch
    {
        private static void Postfix(ContextMenuController __instance, agency._room __0)
        {
            if (__instance == null || __0 == null) return;
            
            bool isAssistant = AssistantManagerRules.IsAssistantManagerOfficeRoomType(__0.type);
            bool isManager = __0.type == agency._type.yourOffice;

            if (isAssistant || isManager)
            {
                // This targets the yourOffice menu that we injected the fire buttons into and safely toggles them.
                AssistantManagerRules.ToggleInjectedFireButtons(__instance, isAssistant);
            }
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.canAssign), new Type[] { typeof(data_girls.girls), typeof(Nullable<data_girls._paramType>) })]
    internal static class RoomCanAssignGirlPatch
    {
        private static bool Prefix(agency._room __instance, data_girls.girls _girl, ref bool __result)
        {
            if (!AssistantManagerRules.IsAssistantManagerOffice(__instance))
            {
                return true;
            }

            __result = __instance.staffer != null &&
                       _girl != null &&
                       __instance.girl == null &&
                       __instance.status == agency._room._status.normal &&
                       (Dating.DEBUG || AssistantManagerDateTracking.GetCooldownDays(__instance.id, _girl.id) <= 0);
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
            if (!AssistantManagerRules.IsAssistantManagerOffice(__instance))
            {
                return true;
            }

            if (_girl == null || __instance.staffer == null)
            {
                return false;
            }

            __instance.assign_date(_girl);
            AssistantManagerRules.PlayManagerOfficeDateVoice(_girl);
            return false;
        }
    }

    [HarmonyPatch(typeof(agency._room), nameof(agency._room.GoOnDate), new Type[0])]
    internal static class RoomGoOnDatePatch
    {
        private static bool Prefix(agency._room __instance)
        {
            if (AssistantManagerRules.IsAssistantManagerOffice(__instance))
            {
                PopupManager component = Camera.main.GetComponent<mainScript>().Data.GetComponent<PopupManager>();
                Date_Popup component2 = component.GetByType(PopupManager._type.girl_date).obj.GetComponent<Date_Popup>();
                component.Open(PopupManager._type.girl_date, true);
                component2.Set(__instance.girl);

                if (__instance.staffer.LevelledUp)
                {
                    __instance.girl.addParam(data_girls._paramType.physicalStamina, 5f, false);
                    int relationshipLevel = __instance.girl.GetRelationshipLevel(Relationships_Player._type.Friendship);
                    if (relationshipLevel > 0)
                    {
                        __instance.girl.addParam(data_girls._paramType.mentalStamina, (float)relationshipLevel, false);
                    }
                }
                
                // Track date cooldown locally instead of the global .AddDate() trigger
                AssistantManagerDateTracking.AddDate(__instance.id, __instance.girl.id);

                __instance.girl = null;
                __instance.status = agency._room._status.normal;
                __instance.staffer.StopWorking(StatusButton._state.normal);
                return false;
            }
            return true;
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

            loans.Loans.Add(__0);
            if (__0.GetDaysToDevelop() == 0)
            {
                __0.Initialize();
                return false;
            }

            agency._room targetRoom = AssistantManagerRules.FindFirstManagerOffice(true, true);
            if (targetRoom == null)
            {
                targetRoom = AssistantManagerRules.FindFirstManagerOffice(true, false);
            }

            if (targetRoom != null)
            {
                targetRoom.assign(__0);
            }

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