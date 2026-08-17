using System;
using System.Collections.Generic;

namespace GraduationCalendar.EmbeddedIMUiFramework
{
    [Flags]
    public enum VanillaPopupSceneAvailability
    {
        None = 0,
        Gameplay = 1,
        MainMenu = 2,
        Both = Gameplay | MainMenu
    }

    public sealed class VanillaPopupTemplateDefinition
    {
        public readonly PopupManager._type Type;
        public readonly int NumericValue;
        public readonly string GameplayHierarchyPath;
        public readonly string MainMenuHierarchyPath;

        internal VanillaPopupTemplateDefinition(PopupManager._type type, int numericValue, string gameplayPath, string mainMenuPath)
        {
            Type = type;
            NumericValue = numericValue;
            GameplayHierarchyPath = gameplayPath;
            MainMenuHierarchyPath = mainMenuPath;
        }

        public VanillaPopupSceneAvailability Availability
        {
            get
            {
                VanillaPopupSceneAvailability result = VanillaPopupSceneAvailability.None;
                if (!string.IsNullOrEmpty(GameplayHierarchyPath)) result |= VanillaPopupSceneAvailability.Gameplay;
                if (!string.IsNullOrEmpty(MainMenuHierarchyPath)) result |= VanillaPopupSceneAvailability.MainMenu;
                return result;
            }
        }

        public bool IsMaterialized
        {
            get { return Availability != VanillaPopupSceneAvailability.None; }
        }
    }

    /// <summary>
    /// Complete PopupManager template table recovered from IM_Scenes. It covers every enum member,
    /// including the two enum slots that have no serialized popup root in either supplied vanilla scene.
    /// A path is documentation/discovery metadata; cloning at runtime still resolves the current manager's
    /// serialized object first so scene refactors do not silently redirect a mod to the wrong hierarchy.
    /// </summary>
    public static class VanillaPopupTemplateCatalog
    {
        private static readonly VanillaPopupTemplateDefinition[] definitions = new VanillaPopupTemplateDefinition[]
        {
            new VanillaPopupTemplateDefinition(PopupManager._type.single_senbatsu, 0, "AgencyPopups/Single_Senbatsu", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.single_new, 1, "AgencyPopups/Single_New", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.audition, 2, "AgencyPopups/Audition", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.show_new, 3, "AgencyPopups/Show_New", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.show_release, 4, null, null),
            new VanillaPopupTemplateDefinition(PopupManager._type.single_release, 5, "AgencyPopups/Single_Release", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.agency_newRoom, 6, "AgencyPopups/Room_Build", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.business_proposal, 7, "AgencyPopups/Business_Proposal", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.random_event, 8, "AgencyPopups/Event", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.debug_launchDialogue, 9, "AgencyPopups/DEBUG/Launch A Quest", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.sevent_concert_new, 10, "AgencyPopups/SEvents_Concert_New", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.sevent_concert, 11, "AgencyPopups/SEvents_Concert", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.sevent_SSK_new, 12, "AgencyPopups/SEvents_SSK_New", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.sevent_SSK, 13, "AgencyPopups/SEvents_SSK", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.sevent_tour_new, 14, "AgencyPopups/SEvents_Tour_New", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.sevent_tour, 15, "AgencyPopups/SEvents_Tour", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.girl_select, 16, "AgencyPopups/Girl_Select", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.girl_view, 17, "AgencyPopups/Producer_Girl DELETE", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.staff_hire, 18, "AgencyPopups/Staff_Hire", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.static_event, 19, "AgencyPopups/Event_Static", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.producer_salaries, 20, "AgencyPopups/Producer_Salaries", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.special_events, 21, "AgencyPopups/Special_Events", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.producer_loans, 22, "AgencyPopups/Producer_Loans", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.staff_nickname, 23, "AgencyPopups/Staff_Nickname", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.notifications, 24, "AgencyPopups/Notifications", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.producer_contracts, 25, "AgencyPopups/Producer_Contracts", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.save, 26, "AgencyPopups/Menu_Save DELETE", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.load, 27, "AgencyPopups/Menu_Load DELETE", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.main_menu_new_game, 30, null, "GUI_Popups/Popups/New Game"),
            new VanillaPopupTemplateDefinition(PopupManager._type.main_menu_settings, 31, "AgencyPopups/Settings_Popup_New", "GUI_Popups/Popups/Settings_Popup_New"),
            new VanillaPopupTemplateDefinition(PopupManager._type.main_menu_load, 32, null, null),
            new VanillaPopupTemplateDefinition(PopupManager._type.girl_profile, 33, "AgencyPopups/Idol_Profile", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.girl_date, 34, "AgencyPopups/Idol_Date", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.main_menu_warning, 35, null, "GUI_Popups/Popups/Warning"),
            new VanillaPopupTemplateDefinition(PopupManager._type.SNS, 36, "AgencyPopups/SNS", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.main_menu_mods, 37, null, "GUI_Popups/Popups/Menu_Mods"),
            new VanillaPopupTemplateDefinition(PopupManager._type.girl_birthday, 38, "AgencyPopups/Idol_Birthday", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.girl_styling, 39, "AgencyPopups/Idol_Styling", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.single_chart, 40, "AgencyPopups/Single_Chart", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.awards, 41, "AgencyPopups/Awards", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.awards_speeches, 42, "AgencyPopups/Awards_Speeches", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.awards_no_win, 43, "AgencyPopups/Awards_First_Time", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.settings_difficulty, 44, "AgencyPopups/Settings_Difficulty", "GUI_Popups/Popups/Settings_Difficulty"),
            new VanillaPopupTemplateDefinition(PopupManager._type.scandal_points, 45, "AgencyPopups/Scandal_Points", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.group_new, 46, "AgencyPopups/Group_New", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.group_appeal, 47, "AgencyPopups/Group_Appeal", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.theater, 48, "AgencyPopups/Theater", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.agency_destroy, 49, "AgencyPopups/Delete Room", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.cafe, 50, "AgencyPopups/Cafe", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.route_lock, 51, "AgencyPopups/Route Lock", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.main_menu_rival, 52, null, "GUI_Popups/Popups/Settings_Rival"),
            new VanillaPopupTemplateDefinition(PopupManager._type.main_menu_load_story, 53, "AgencyPopups/Menu_Load_Story", "GUI_Popups/Popups/Menu_Load_Story"),
            new VanillaPopupTemplateDefinition(PopupManager._type.summer_games_committee, 54, "AgencyPopups/Summer Games", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.summer_games_fail, 55, "AgencyPopups/Summer Games Fail", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.main_menu_stats, 56, null, "GUI_Popups/Popups/Gallery_Stats"),
            new VanillaPopupTemplateDefinition(PopupManager._type.main_menu_cgs, 57, null, "GUI_Popups/Popups/Gallery_CGs"),
            new VanillaPopupTemplateDefinition(PopupManager._type.group_disband, 58, "AgencyPopups/Disband Group", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.demo_complete, 59, "AgencyPopups/Demo Completed", null),
            new VanillaPopupTemplateDefinition(PopupManager._type.main_menu_music, 60, null, "GUI_Popups/Popups/Gallery_Music"),
            new VanillaPopupTemplateDefinition(PopupManager._type.message, 61, "AgencyPopups/MESSAGE", null),
        };

        public static IList<VanillaPopupTemplateDefinition> All
        {
            get { return new List<VanillaPopupTemplateDefinition>(definitions); }
        }

        public static bool TryGet(PopupManager._type type, out VanillaPopupTemplateDefinition definition)
        {
            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i].Type.Equals(type))
                {
                    definition = definitions[i];
                    return true;
                }
            }
            definition = null;
            return false;
        }
    }
}
