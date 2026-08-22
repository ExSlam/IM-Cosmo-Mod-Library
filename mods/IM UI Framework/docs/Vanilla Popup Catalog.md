# Vanilla Popup Catalog

IM UI Framework 3.0 exposes every `PopupManager._type` defined by the shipped game. The hierarchy paths below were recovered from the supplied AssetRipper `main.unity` and `Main Menu.unity` scenes and validated against those scene hierarchies.

Runtime cloning does **not** require the popup to be open. `PopupManager` serialized references are used first, and the catalog path is a scene-local fallback. A template can only be cloned in a scene where its hierarchy is actually loaded.

| Value | PopupManager type | Gameplay scene | Main menu scene |
| ---: | --- | --- | --- |
| 0 | `single_senbatsu` | `AgencyPopups/Single_Senbatsu` | — |
| 1 | `single_new` | `AgencyPopups/Single_New` | — |
| 2 | `audition` | `AgencyPopups/Audition` | — |
| 3 | `show_new` | `AgencyPopups/Show_New` | — |
| 4 | `show_release` | — | — |
| 5 | `single_release` | `AgencyPopups/Single_Release` | — |
| 6 | `agency_newRoom` | `AgencyPopups/Room_Build` | — |
| 7 | `business_proposal` | `AgencyPopups/Business_Proposal` | — |
| 8 | `random_event` | `AgencyPopups/Event` | — |
| 9 | `debug_launchDialogue` | `AgencyPopups/DEBUG/Launch A Quest` | — |
| 10 | `sevent_concert_new` | `AgencyPopups/SEvents_Concert_New` | — |
| 11 | `sevent_concert` | `AgencyPopups/SEvents_Concert` | — |
| 12 | `sevent_SSK_new` | `AgencyPopups/SEvents_SSK_New` | — |
| 13 | `sevent_SSK` | `AgencyPopups/SEvents_SSK` | — |
| 14 | `sevent_tour_new` | `AgencyPopups/SEvents_Tour_New` | — |
| 15 | `sevent_tour` | `AgencyPopups/SEvents_Tour` | — |
| 16 | `girl_select` | `AgencyPopups/Girl_Select` | — |
| 17 | `girl_view` | `AgencyPopups/Producer_Girl DELETE` | — |
| 18 | `staff_hire` | `AgencyPopups/Staff_Hire` | — |
| 19 | `static_event` | `AgencyPopups/Event_Static` | — |
| 20 | `producer_salaries` | `AgencyPopups/Producer_Salaries` | — |
| 21 | `special_events` | `AgencyPopups/Special_Events` | — |
| 22 | `producer_loans` | `AgencyPopups/Producer_Loans` | — |
| 23 | `staff_nickname` | `AgencyPopups/Staff_Nickname` | — |
| 24 | `notifications` | `AgencyPopups/Notifications` | — |
| 25 | `producer_contracts` | `AgencyPopups/Producer_Contracts` | — |
| 26 | `save` | `AgencyPopups/Menu_Save DELETE` | — |
| 27 | `load` | `AgencyPopups/Menu_Load DELETE` | — |
| 30 | `main_menu_new_game` | — | `GUI_Popups/Popups/New Game` |
| 31 | `main_menu_settings` | `AgencyPopups/Settings_Popup_New` | `GUI_Popups/Popups/Settings_Popup_New` |
| 32 | `main_menu_load` | — | — |
| 33 | `girl_profile` | `AgencyPopups/Idol_Profile` | — |
| 34 | `girl_date` | `AgencyPopups/Idol_Date` | — |
| 35 | `main_menu_warning` | — | `GUI_Popups/Popups/Warning` |
| 36 | `SNS` | `AgencyPopups/SNS` | — |
| 37 | `main_menu_mods` | — | `GUI_Popups/Popups/Menu_Mods` |
| 38 | `girl_birthday` | `AgencyPopups/Idol_Birthday` | — |
| 39 | `girl_styling` | `AgencyPopups/Idol_Styling` | — |
| 40 | `single_chart` | `AgencyPopups/Single_Chart` | — |
| 41 | `awards` | `AgencyPopups/Awards` | — |
| 42 | `awards_speeches` | `AgencyPopups/Awards_Speeches` | — |
| 43 | `awards_no_win` | `AgencyPopups/Awards_First_Time` | — |
| 44 | `settings_difficulty` | `AgencyPopups/Settings_Difficulty` | `GUI_Popups/Popups/Settings_Difficulty` |
| 45 | `scandal_points` | `AgencyPopups/Scandal_Points` | — |
| 46 | `group_new` | `AgencyPopups/Group_New` | — |
| 47 | `group_appeal` | `AgencyPopups/Group_Appeal` | — |
| 48 | `theater` | `AgencyPopups/Theater` | — |
| 49 | `agency_destroy` | `AgencyPopups/Delete Room` | — |
| 50 | `cafe` | `AgencyPopups/Cafe` | — |
| 51 | `route_lock` | `AgencyPopups/Route Lock` | — |
| 52 | `main_menu_rival` | — | `GUI_Popups/Popups/Settings_Rival` |
| 53 | `main_menu_load_story` | `AgencyPopups/Menu_Load_Story` | `GUI_Popups/Popups/Menu_Load_Story` |
| 54 | `summer_games_committee` | `AgencyPopups/Summer Games` | — |
| 55 | `summer_games_fail` | `AgencyPopups/Summer Games Fail` | — |
| 56 | `main_menu_stats` | — | `GUI_Popups/Popups/Gallery_Stats` |
| 57 | `main_menu_cgs` | — | `GUI_Popups/Popups/Gallery_CGs` |
| 58 | `group_disband` | `AgencyPopups/Disband Group` | — |
| 59 | `demo_complete` | `AgencyPopups/Demo Completed` | — |
| 60 | `main_menu_music` | — | `GUI_Popups/Popups/Gallery_Music` |
| 61 | `message` | `AgencyPopups/MESSAGE` | — |

## Unmaterialized enum slots

`show_release` (`4`) and `main_menu_load` (`32`) are real enum members, but neither supplied vanilla scene serializes a popup root for them. The framework catalogs them intentionally as `None` rather than inventing a fake template. All other enum members have at least one validated scene hierarchy.

## API

```csharp
VanillaPopupTemplateDefinition definition;
VanillaPopupTemplateCatalog.TryGet(PopupManager._type.producer_contracts, out definition);

GameObject root;
VanillaUiSceneCatalog.TryGetPopupRoot(PopupManager._type.producer_contracts, out root);

VanillaPopupHandle popup;
VanillaPopupBuilder
    .From(PopupManager._type.producer_contracts)
    .Named("MyModPopup")
    .WithTitle("My Mod")
    .ContentAt("Panel/Container", true)
    .RegisterAs((PopupManager._type)9300)
    .Build(out popup);
```

For exact visual inspection or intentionally retaining all original controllers/listeners, use `VanillaUiCloneMode.Exact`. Normal mod UI should use the default `Template` mode so game-specific controller scripts are removed while safe internal UI wiring is retained.
