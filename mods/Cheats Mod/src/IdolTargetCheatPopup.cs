using System;
using System.Collections.Generic;
using System.Globalization;
using ModLocalizationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CheatsMod
{
    internal static class IdolTargetCheatPopup
    {
        private const int PopupTypeValue = 1431194194;
        private const int FirstIndex = 0;
        private const int MinimumFameLevel = 1;
        private const int MaximumFameLevel = 10;
        private const int MaximumRelationshipLevel = 5;
        private const float MaximumStatValue = 100f;
        private const string DatingScandalTemplateId = "dating_scandal";

        private const float PanelWidth = 930f;
        private const float PanelHeight = 640f;
        private const float Margin = 24f;
        private const float TitleHeight = 44f;
        private const float LeftWidth = 520f;
        private const float ColumnGap = 22f;
        private const float RightWidth = PanelWidth - (Margin * 2f) - LeftWidth - ColumnGap;
        private const float BodyTop = 82f;
        private const float BodyBottom = 70f;
        private const float PickerLabelHeight = 30f;
        private const float PickerCellWidth = 225f;
        private const float PickerCellHeight = 123f;
        private const float PickerSpacing = 10f;
        private const float ControlRowHeight = 46f;
        private const float ArrowWidth = 44f;
        private const float ActionButtonWidth = 270f;
        private const float ActionButtonHeight = 42f;
        private const float CloseButtonWidth = 160f;
        private const float CloseButtonHeight = 38f;

        private const string SelectIdolKey = "ui.idol_cheat.select_idol";
        private const string SelectIdolFallback = "Select an idol";
        private const string SelectedIdolKey = "ui.idol_cheat.selected_idol";
        private const string SelectedIdolFallback = "Selected idol: {0}";
        private const string CloseKey = "ui.idol_cheat.close";
        private const string CloseFallback = "Close";

        private const string StatsTitleKey = "ui.idol_cheat.stats.title";
        private const string StatsTitleFallback = "Set Idol Stats to 100";
        private const string StatsDescriptionKey = "ui.idol_cheat.stats.description";
        private const string StatsDescriptionFallback = "Set Cute, Cool, Sexy, Pretty, Dance, Vocal, Funny, and Smart to 100.";
        private const string StatsActionKey = "ui.idol_cheat.stats.action";
        private const string StatsActionFallback = "Set stats to 100";

        private const string FameTitleKey = "ui.idol_cheat.fame.title";
        private const string FameTitleFallback = "Set Idol Fame";
        private const string FameCurrentKey = "ui.idol_cheat.fame.current";
        private const string FameCurrentFallback = "Current fame level: {0}";
        private const string FameTargetKey = "ui.idol_cheat.fame.target";
        private const string FameTargetFallback = "Target fame level";
        private const string FameActionKey = "ui.idol_cheat.fame.action";
        private const string FameActionFallback = "Set fame level";

        private const string ScandalTitleKey = "ui.idol_cheat.scandal.title";
        private const string ScandalTitleFallback = "Generate Random Scandal";
        private const string ScandalDescriptionKey = "ui.idol_cheat.scandal.description";
        private const string ScandalDescriptionFallback = "Generate a random dating scandal event starring the selected idol.";
        private const string ScandalActionKey = "ui.idol_cheat.scandal.action";
        private const string ScandalActionFallback = "Generate scandal";

        private const string BreakupTitleKey = "ui.idol_cheat.breakup.title";
        private const string BreakupTitleFallback = "Break Up Idol Relationship";
        private const string BreakupDescriptionKey = "ui.idol_cheat.breakup.description";
        private const string BreakupDescriptionFallback = "End the selected idol's relationship with an idol or outside partner. Producer relationships are not changed.";
        private const string BreakupActionKey = "ui.idol_cheat.breakup.action";
        private const string BreakupActionFallback = "Break up relationship";

        private const string RelationshipsTitleKey = "ui.idol_cheat.relationships.title";
        private const string RelationshipsTitleFallback = "Max Idol Relationships";
        private const string RelationshipsCurrentKey = "ui.idol_cheat.relationships.current";
        private const string RelationshipsCurrentFallback = "Influence {0}   Friendship {1}   Romance {2}";
        private const string RelationshipsActionKey = "ui.idol_cheat.relationships.action";
        private const string RelationshipsActionFallback = "Max relationships";

        private const string NoSelectionKey = "notification.no_selected_idol";
        private const string NoSelectionFallback = "Select an idol first.";
        private const string NoSelectableIdolsKey = "notification.no_selectable_idols";
        private const string NoSelectableIdolsFallback = "No idols are available for this cheat.";
        private const string NoScandalEligibleIdolsKey = "notification.no_scandal_eligible_idols";
        private const string NoScandalEligibleIdolsFallback = "No active idol is eligible for a generated scandal.";
        private const string StatsAppliedKey = "notification.selected_idol_stats_100";
        private const string StatsAppliedFallback = "{0}'s core stats were set to 100.";
        private const string FameAppliedKey = "notification.selected_idol_fame";
        private const string FameAppliedFallback = "{0}'s fame was set to level {1}.";
        private const string ScandalFailedKey = "notification.selected_idol_scandal_failed";
        private const string ScandalFailedFallback = "A scandal could not be generated for the selected idol.";
        private const string BreakupAppliedKey = "notification.selected_idol_breakup";
        private const string BreakupAppliedFallback = "{0}'s non-producer dating relationship was ended.";
        private const string NoDatingKey = "notification.selected_idol_no_dating";
        private const string NoDatingFallback = "The selected idol is not dating a non-producer partner.";
        private const string RelationshipsAppliedKey = "notification.selected_idol_max_relationships";
        private const string RelationshipsAppliedFallback = "{0}'s influence, friendship, and romance with the producer were maxed.";
        private const string FailedKey = "notification.selected_idol_cheat_failed";
        private const string FailedFallback = "Selected-idol cheat action failed.";

        private static readonly data_girls._paramType[] CoreStatTypes = new data_girls._paramType[]
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

        private static GameObject popupRoot;
        private static TextMeshProUGUI defaultFontSource;
        private static Mode currentMode;
        private static data_girls.girls selectedGirl;
        private static int targetFameLevel = MinimumFameLevel;
        private static TextMeshProUGUI selectedIdolText;
        private static TextMeshProUGUI detailText;
        private static TextMeshProUGUI fameValueText;
        private static Button actionButton;

        private enum Mode
        {
            Stats100,
            Fame,
            RandomScandal,
            BreakupDating,
            MaxPlayerRelationships
        }

        internal static void OpenStats100()
        {
            Open(Mode.Stats100);
        }

        internal static void OpenFame()
        {
            Open(Mode.Fame);
        }

        internal static void OpenRandomScandal()
        {
            Open(Mode.RandomScandal);
        }

        internal static void OpenBreakupDating()
        {
            Open(Mode.BreakupDating);
        }

        internal static void OpenMaxPlayerRelationships()
        {
            Open(Mode.MaxPlayerRelationships);
        }

        private static void Open(Mode mode)
        {
            try
            {
                PopupManager manager = GetPopupManager();
                data_girls dataGirls = GetDataComponent<data_girls>();
                if (manager == null || dataGirls == null || data_girls.girl == null)
                {
                    NotifyWarning(CheatLocalizationKeys.NotificationGameUnavailable, CheatFallbackText.NotificationGameUnavailable);
                    return;
                }

                List<data_girls.girls> eligible = BuildEligibleGirls(mode);
                if (eligible.Count == 0)
                {
                    NotifyWarning(
                        mode == Mode.RandomScandal ? NoScandalEligibleIdolsKey : NoSelectableIdolsKey,
                        mode == Mode.RandomScandal ? NoScandalEligibleIdolsFallback : NoSelectableIdolsFallback);
                    return;
                }

                if (mode == Mode.RandomScandal && Event_Templates.GetTemplate(DatingScandalTemplateId) == null)
                {
                    NotifyWarning(ScandalFailedKey, ScandalFailedFallback);
                    return;
                }

                GameObject girlButtonPrefab = GetStylistGirlButtonPrefab(manager);
                if (girlButtonPrefab == null)
                {
                    NotifyWarning(FailedKey, FailedFallback);
                    return;
                }

                currentMode = mode;
                selectedGirl = eligible[FirstIndex];
                targetFameLevel = GetInitialFameLevel(selectedGirl);

                if (!CreatePopup(manager, eligible, girlButtonPrefab))
                {
                    NotifyWarning(FailedKey, FailedFallback);
                    return;
                }

                PopupManager.OpenPopup((PopupManager._type)PopupTypeValue);
            }
            catch (Exception exception)
            {
                Debug.LogError("[CheatsMod] Selected-idol cheat popup failed: " + exception);
                NotifyWarning(FailedKey, FailedFallback);
            }
        }

        private static List<data_girls.girls> BuildEligibleGirls(Mode mode)
        {
            List<data_girls.girls> result = new List<data_girls.girls>();
            foreach (data_girls.girls girl in data_girls.girl)
            {
                if (girl == null
                    || girl.Type != data_girls.girls._type.NORMAL
                    || girl.status == data_girls._status.graduated)
                {
                    continue;
                }

                if (mode == Mode.RandomScandal && !CanGenerateScandal(girl))
                {
                    continue;
                }

                result.Add(girl);
            }

            result.Sort(delegate(data_girls.girls left, data_girls.girls right)
            {
                return string.Compare(
                    SafeGirlName(left),
                    SafeGirlName(right),
                    StringComparison.CurrentCultureIgnoreCase);
            });
            return result;
        }

        private static bool CanGenerateScandal(data_girls.girls girl)
        {
            if (girl == null || !girl.IsActive() || girl.DatingData == null || girl.GetAge() < 16)
            {
                return false;
            }

            return !mainScript.IsCensored() || girl.Is_AOC();
        }

        private static bool CreatePopup(
            PopupManager manager,
            List<data_girls.girls> eligible,
            GameObject girlButtonPrefab)
        {
            Transform parent = GetPopupParent(manager);
            if (parent == null)
            {
                return false;
            }

            DestroyExistingRoot();

            GameObject root = new GameObject(
                "CheatsModSelectedIdolPopup",
                typeof(RectTransform),
                typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            root.transform.SetAsLastSibling();
            SetLayerRecursively(root, parent.gameObject.layer);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);
            CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            root.SetActive(false);

            GameObject panel = CreateUIObject("Panel", root.transform);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            panelRect.anchoredPosition = Vector2.zero;
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color32(245, 245, 245, 255);
            panelImage.raycastTarget = true;

            TextMeshProUGUI title = CreateText(
                panel.transform,
                "Title",
                GetModeTitle(),
                28,
                TextAlignmentOptions.Center,
                mainScript.black32);
            SetRect(title.rectTransform, Margin, -14f, PanelWidth - (Margin * 2f), TitleHeight, true);

            CreateGirlPicker(panel.transform, eligible, girlButtonPrefab);
            CreateControls(panel.transform);
            CreateCloseButton(panel.transform);

            Popup popup = root.AddComponent<Popup>();
            popup.ShowAnimation = true;
            popup.HideAnimation = true;
            popup.HideFast = false;
            popup.Increase_Popup_Counter = true;
            popup.OnOpen = new UnityEvent();

            if (!TryRegisterPopup(manager, root))
            {
                UnityEngine.Object.Destroy(root);
                return false;
            }

            popupRoot = root;
            RefreshSelectionUI();
            return true;
        }

        private static void CreateGirlPicker(
            Transform panel,
            List<data_girls.girls> eligible,
            GameObject girlButtonPrefab)
        {
            TextMeshProUGUI label = CreateText(
                panel,
                "PickerLabel",
                GetLocalized(SelectIdolKey, SelectIdolFallback),
                19,
                TextAlignmentOptions.MidlineLeft,
                mainScript.black32);
            SetRect(label.rectTransform, Margin, -BodyTop, LeftWidth, PickerLabelHeight, true);

            GameObject scrollObject = CreateUIObject("IdolScroll", panel);
            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            float scrollHeight = PanelHeight - BodyTop - BodyBottom - PickerLabelHeight;
            SetRect(
                scrollRectTransform,
                Margin,
                -(BodyTop + PickerLabelHeight),
                LeftWidth,
                scrollHeight,
                true);
            Image scrollBackground = scrollObject.AddComponent<Image>();
            scrollBackground.color = new Color32(229, 229, 229, 255);

            ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewport = CreateUIObject("Viewport", scrollObject.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            Stretch(viewportRect);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = Color.white;
            viewportImage.raycastTarget = true;

            GameObject content = CreateUIObject("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(PickerCellWidth, PickerCellHeight);
            grid.spacing = new Vector2(PickerSpacing, PickerSpacing);
            grid.padding = new RectOffset(12, 12, 12, 12);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            int rows = Mathf.CeilToInt(eligible.Count / 2f);
            float contentHeight = 24f + (rows * PickerCellHeight) + (Mathf.Max(0, rows - 1) * PickerSpacing);
            contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.verticalNormalizedPosition = 1f;

            for (int index = 0; index < eligible.Count; index++)
            {
                data_girls.girls girl = eligible[index];
                GameObject item = UnityEngine.Object.Instantiate<GameObject>(girlButtonPrefab);
                item.name = "CheatIdol_" + girl.id.ToString(CultureInfo.InvariantCulture);
                item.transform.SetParent(content.transform, false);
                SetLayerRecursively(item, panel.gameObject.layer);

                GirlButtonSmall girlButton = item.GetComponent<GirlButtonSmall>();
                if (girlButton != null)
                {
                    girlButton.DontDisableIfTraining = true;
                    girlButton.DontDisableIfHiatus = true;
                    girlButton.SetGirl(girl, false);
                }

                Button button = item.GetComponent<Button>();
                if (button != null)
                {
                    data_girls.girls capturedGirl = girl;
                    button.onClick = new Button.ButtonClickedEvent();
                    button.onClick.AddListener(new UnityAction(delegate
                    {
                        SelectGirl(capturedGirl);
                    }));
                    button.interactable = true;
                }
            }
        }

        private static void CreateControls(Transform panel)
        {
            float left = Margin + LeftWidth + ColumnGap;
            float top = -BodyTop;

            selectedIdolText = CreateText(
                panel,
                "SelectedIdol",
                string.Empty,
                19,
                TextAlignmentOptions.TopLeft,
                mainScript.black32);
            SetRect(selectedIdolText.rectTransform, left, top, RightWidth, 58f, true);

            detailText = CreateText(
                panel,
                "Detail",
                string.Empty,
                16,
                TextAlignmentOptions.TopLeft,
                mainScript.black32);
            SetRect(detailText.rectTransform, left, top - 72f, RightWidth, 125f, true);

            if (currentMode == Mode.Fame)
            {
                CreateFameRow(panel, left, top - 214f);
            }

            actionButton = CreateButton(
                panel,
                "ApplyCheat",
                GetModeActionLabel(),
                ActionButtonWidth,
                ActionButtonHeight,
                currentMode == Mode.BreakupDating ? mainScript.red32 : mainScript.blue32,
                ApplyCurrentCheat);
            SetRect(
                actionButton.GetComponent<RectTransform>(),
                left + ((RightWidth - ActionButtonWidth) / 2f),
                currentMode == Mode.Fame ? top - 310f : top - 226f,
                ActionButtonWidth,
                ActionButtonHeight,
                true);
        }

        private static void CreateFameRow(Transform panel, float left, float top)
        {
            TextMeshProUGUI caption = CreateText(
                panel,
                "FameTargetLabel",
                GetLocalized(FameTargetKey, FameTargetFallback),
                16,
                TextAlignmentOptions.MidlineLeft,
                mainScript.black32);
            SetRect(caption.rectTransform, left, top, 128f, ControlRowHeight, true);

            Button previous = CreateButton(
                panel,
                "FamePrevious",
                "<",
                ArrowWidth,
                ControlRowHeight,
                mainScript.grey_light32,
                delegate { AdjustFame(-1); });
            SetRect(previous.GetComponent<RectTransform>(), left + 132f, top, ArrowWidth, ControlRowHeight, true);

            fameValueText = CreateText(
                panel,
                "FameValue",
                string.Empty,
                21,
                TextAlignmentOptions.Center,
                mainScript.black32);
            SetRect(fameValueText.rectTransform, left + 182f, top, 62f, ControlRowHeight, true);

            Button next = CreateButton(
                panel,
                "FameNext",
                ">",
                ArrowWidth,
                ControlRowHeight,
                mainScript.grey_light32,
                delegate { AdjustFame(1); });
            SetRect(next.GetComponent<RectTransform>(), left + 250f, top, ArrowWidth, ControlRowHeight, true);
        }

        private static void CreateCloseButton(Transform panel)
        {
            Button close = CreateButton(
                panel,
                "Close",
                GetLocalized(CloseKey, CloseFallback),
                CloseButtonWidth,
                CloseButtonHeight,
                mainScript.grey_light32,
                Close);
            SetRect(
                close.GetComponent<RectTransform>(),
                (PanelWidth - CloseButtonWidth) / 2f,
                -PanelHeight + 50f,
                CloseButtonWidth,
                CloseButtonHeight,
                true);
        }

        private static void SelectGirl(data_girls.girls girl)
        {
            if (girl == null)
            {
                return;
            }

            selectedGirl = girl;
            if (currentMode == Mode.Fame)
            {
                targetFameLevel = GetInitialFameLevel(girl);
            }
            RefreshSelectionUI();
        }

        private static void RefreshSelectionUI()
        {
            if (selectedIdolText == null || selectedGirl == null)
            {
                return;
            }

            selectedIdolText.text = string.Format(
                CultureInfo.CurrentCulture,
                GetLocalized(SelectedIdolKey, SelectedIdolFallback),
                SafeGirlName(selectedGirl));

            if (detailText != null)
            {
                detailText.text = GetModeDetailText();
            }

            if (fameValueText != null)
            {
                fameValueText.text = targetFameLevel.ToString(CultureInfo.CurrentCulture);
            }
        }

        private static string GetModeDetailText()
        {
            switch (currentMode)
            {
                case Mode.Stats100:
                    return GetLocalized(StatsDescriptionKey, StatsDescriptionFallback);
                case Mode.Fame:
                    return string.Format(
                        CultureInfo.CurrentCulture,
                        GetLocalized(FameCurrentKey, FameCurrentFallback),
                        selectedGirl.GetFameLevel());
                case Mode.RandomScandal:
                    return GetLocalized(ScandalDescriptionKey, ScandalDescriptionFallback);
                case Mode.BreakupDating:
                    return GetLocalized(BreakupDescriptionKey, BreakupDescriptionFallback);
                case Mode.MaxPlayerRelationships:
                    return string.Format(
                        CultureInfo.CurrentCulture,
                        GetLocalized(RelationshipsCurrentKey, RelationshipsCurrentFallback),
                        selectedGirl.GetRelationshipLevel(Relationships_Player._type.Influence),
                        selectedGirl.GetRelationshipLevel(Relationships_Player._type.Friendship),
                        selectedGirl.GetRelationshipLevel(Relationships_Player._type.Romance));
                default:
                    return string.Empty;
            }
        }

        private static void AdjustFame(int delta)
        {
            targetFameLevel = Mathf.Clamp(
                targetFameLevel + delta,
                MinimumFameLevel,
                MaximumFameLevel);
            RefreshSelectionUI();
        }

        private static int GetInitialFameLevel(data_girls.girls girl)
        {
            if (girl == null)
            {
                return MinimumFameLevel;
            }

            return Mathf.Clamp(girl.GetFameLevel(), MinimumFameLevel, MaximumFameLevel);
        }

        private static string GetModeTitle()
        {
            switch (currentMode)
            {
                case Mode.Stats100:
                    return GetLocalized(StatsTitleKey, StatsTitleFallback);
                case Mode.Fame:
                    return GetLocalized(FameTitleKey, FameTitleFallback);
                case Mode.RandomScandal:
                    return GetLocalized(ScandalTitleKey, ScandalTitleFallback);
                case Mode.BreakupDating:
                    return GetLocalized(BreakupTitleKey, BreakupTitleFallback);
                case Mode.MaxPlayerRelationships:
                    return GetLocalized(RelationshipsTitleKey, RelationshipsTitleFallback);
                default:
                    return string.Empty;
            }
        }

        private static string GetModeActionLabel()
        {
            switch (currentMode)
            {
                case Mode.Stats100:
                    return GetLocalized(StatsActionKey, StatsActionFallback);
                case Mode.Fame:
                    return GetLocalized(FameActionKey, FameActionFallback);
                case Mode.RandomScandal:
                    return GetLocalized(ScandalActionKey, ScandalActionFallback);
                case Mode.BreakupDating:
                    return GetLocalized(BreakupActionKey, BreakupActionFallback);
                case Mode.MaxPlayerRelationships:
                    return GetLocalized(RelationshipsActionKey, RelationshipsActionFallback);
                default:
                    return string.Empty;
            }
        }

        private static void ApplyCurrentCheat()
        {
            if (selectedGirl == null)
            {
                NotifyWarning(NoSelectionKey, NoSelectionFallback);
                return;
            }

            try
            {
                switch (currentMode)
                {
                    case Mode.Stats100:
                        ApplyStats100();
                        break;
                    case Mode.Fame:
                        ApplyFame();
                        break;
                    case Mode.RandomScandal:
                        ApplyRandomScandal();
                        break;
                    case Mode.BreakupDating:
                        ApplyBreakup();
                        break;
                    case Mode.MaxPlayerRelationships:
                        ApplyMaxPlayerRelationships();
                        break;
                }
            }
            catch (Exception exception)
            {
                if (currentMode == Mode.RandomScandal)
                {
                    Event_Templates.Active_Template = null;
                }
                Debug.LogError("[CheatsMod] Selected-idol cheat action failed: " + exception);
                NotifyWarning(FailedKey, FailedFallback);
            }
        }

        private static void ApplyStats100()
        {
            string idolName = SafeGirlName(selectedGirl);
            for (int statIndex = 0; statIndex < CoreStatTypes.Length; statIndex++)
            {
                selectedGirl.setParam(CoreStatTypes[statIndex], MaximumStatValue);
            }

            RefreshGirlAndList(selectedGirl);
            NotifySuccess(string.Format(
                CultureInfo.CurrentCulture,
                GetLocalized(StatsAppliedKey, StatsAppliedFallback),
                idolName),
                NotificationManager._notification._type.idol_stat_change);
            Close();
        }

        private static void ApplyFame()
        {
            string idolName = SafeGirlName(selectedGirl);
            int level = Mathf.Clamp(targetFameLevel, MinimumFameLevel, MaximumFameLevel);
            selectedGirl.setParam(data_girls._paramType.famePoints, resources.FameLevelToPoints(level));
            RefreshGirlAndList(selectedGirl);
            NotifySuccess(string.Format(
                CultureInfo.CurrentCulture,
                GetLocalized(FameAppliedKey, FameAppliedFallback),
                idolName,
                level),
                NotificationManager._notification._type.idol_stat_change);
            Close();
        }

        private static void ApplyRandomScandal()
        {
            if (!CanGenerateScandal(selectedGirl))
            {
                NotifyWarning(NoScandalEligibleIdolsKey, NoScandalEligibleIdolsFallback);
                return;
            }

            Event_Templates eventTemplates = GetDataComponent<Event_Templates>();
            if (eventTemplates == null)
            {
                NotifyWarning(ScandalFailedKey, ScandalFailedFallback);
                return;
            }

            if (!BuildTargetedScandal(selectedGirl))
            {
                NotifyWarning(ScandalFailedKey, ScandalFailedFallback);
                return;
            }

            Event_Overlord.Set_Event();
            Close();
            eventTemplates._OpenPopup();
        }

        private static bool BuildTargetedScandal(data_girls.girls targetGirl)
        {
            Event_Templates._template template = Event_Templates.GetTemplate(DatingScandalTemplateId);
            if (template == null || targetGirl == null)
            {
                return false;
            }

            Event_Templates._active_template active = new Event_Templates._active_template();
            active.Template = template;
            Event_Templates.Active_Template = active;
            bool assignedTarget = false;

            foreach (Event_Templates._template._actor actorDefinition in template.Actors)
            {
                if (!active.CheckRequirements(actorDefinition.Requirements))
                {
                    continue;
                }

                Event_Manager._activeEvent._actor actor = new Event_Manager._activeEvent._actor();
                if (actorDefinition.Type == Event_Templates._template._actor._type.idol)
                {
                    if (!assignedTarget)
                    {
                        actor.girl = targetGirl;
                        assignedTarget = true;
                    }
                    else
                    {
                        List<data_girls.girls> otherGirls = data_girls.GetActiveGirls(null);
                        otherGirls.Remove(targetGirl);
                        actor.girl = otherGirls.Count == 0
                            ? targetGirl
                            : ExtensionMethods.GetRandomElement<data_girls.girls>(otherGirls);
                    }
                }
                else if (actorDefinition.Type == Event_Templates._template._actor._type.staff)
                {
                    if (staff.Staff == null || staff.Staff.Count == 0)
                    {
                        Event_Templates.Active_Template = null;
                        return false;
                    }
                    actor.staff = ExtensionMethods.GetRandomElement<staff._staff>(staff.Staff);
                }
                else
                {
                    bool female = GetGeneratedActorGender(actorDefinition, active);
                    actor.name = Language.GetVariableName(
                        nameGenerator.firstName(female),
                        nameGenerator.lastName(),
                        female,
                        "default",
                        true);
                    actor.female = female;
                }

                active.Actors.Add(actor);
                active.SetActions(actorDefinition.Actions);
            }

            if (!assignedTarget)
            {
                Event_Templates.Active_Template = null;
                return false;
            }

            foreach (Event_Templates._template._part part in template.Parts)
            {
                if (active.CheckRequirements(part.Requirements) && mainScript.chance(part.Chance))
                {
                    active.SetActions(part.Actions);
                    if (part.Values.Count != 0)
                    {
                        Event_Templates._template._part._value randomValue = part.GetRandomValue();
                        if (randomValue != null)
                        {
                            active.SetActions(randomValue.Actions);
                            active.Parts.Add(randomValue);
                        }
                    }
                }
            }

            foreach (Event_Templates._template._reply reply in template.Replies)
            {
                if (active.CheckRequirements(reply.Requirements))
                {
                    active.Replies.Add(reply);
                }
            }

            if (active.Replies.Count == 0)
            {
                Event_Templates.Active_Template = null;
                return false;
            }

            targetGirl.LastDatingScandal = staticVars.dateTime;
            return true;
        }

        private static bool GetGeneratedActorGender(
            Event_Templates._template._actor actorDefinition,
            Event_Templates._active_template active)
        {
            bool female = true;
            if (actorDefinition.Type == Event_Templates._template._actor._type.male)
            {
                return false;
            }
            if (actorDefinition.Type != Event_Templates._template._actor._type.male_or_female)
            {
                return female;
            }

            if (actorDefinition.Special_Params == "love_interest"
                && active.Actors.Count > 0
                && active.Actors[0] != null
                && active.Actors[0].girl != null)
            {
                data_girls.girls girl = active.Actors[0].girl;
                if (girl.DatingData.Partner_Status == data_girls.girls._dating_data._partner_status.taken_outside_bf)
                {
                    return false;
                }
                if (girl.DatingData.Partner_Status == data_girls.girls._dating_data._partner_status.taken_outside_gf)
                {
                    return true;
                }
                if (girl.sexuality == data_girls.girls._sexuality.straight)
                {
                    return false;
                }
                if (girl.sexuality == data_girls.girls._sexuality.lesbian)
                {
                    return true;
                }
                return !mainScript.chance(80);
            }

            return !mainScript.chance(50);
        }

        private static void ApplyBreakup()
        {
            string idolName = SafeGirlName(selectedGirl);
            bool brokeUp = false;

            if (Relationships.RelationshipsData != null)
            {
                for (int relationshipIndex = 0; relationshipIndex < Relationships.RelationshipsData.Count; relationshipIndex++)
                {
                    Relationships._relationship relationship = Relationships.RelationshipsData[relationshipIndex];
                    if (relationship == null
                        || !relationship.Dating
                        || relationship.Girls == null
                        || relationship.Girls.Count < 2
                        || relationship.Girls[0] == null
                        || relationship.Girls[1] == null
                        || !relationship.Girls.Contains(selectedGirl))
                    {
                        continue;
                    }

                    data_girls.girls firstGirl = relationship.Girls[0];
                    data_girls.girls secondGirl = relationship.Girls[1];
                    bool firstKnown = firstGirl != null
                        && firstGirl.DatingData != null
                        && firstGirl.DatingData.Is_Partner_Status_Known;
                    bool secondKnown = secondGirl != null
                        && secondGirl.DatingData != null
                        && secondGirl.DatingData.Is_Partner_Status_Known;
                    relationship.BreakUp();
                    RestoreDatingStatusKnowledge(firstGirl, firstKnown);
                    RestoreDatingStatusKnowledge(secondGirl, secondKnown);
                    brokeUp = true;
                    break;
                }
            }

            if (!brokeUp && selectedGirl.DatingData != null)
            {
                data_girls.girls._dating_data._partner_status status = selectedGirl.DatingData.Partner_Status;
                if (status == data_girls.girls._dating_data._partner_status.taken_outside_bf
                    || status == data_girls.girls._dating_data._partner_status.taken_outside_gf
                    || status == data_girls.girls._dating_data._partner_status.taken_idol)
                {
                    bool statusWasKnown = selectedGirl.DatingData.Is_Partner_Status_Known;
                    selectedGirl.DatingData.SetDatingStatus(data_girls.girls._dating_data._partner_status.free);
                    RestoreDatingStatusKnowledge(selectedGirl, statusWasKnown);
                    selectedGirl.getParam(data_girls._paramType.mentalStamina).add(-30f, false);
                    brokeUp = true;
                }
            }

            if (!brokeUp)
            {
                NotifyWarning(NoDatingKey, NoDatingFallback);
                return;
            }

            RefreshGirlAndList(selectedGirl);
            NotifySuccess(string.Format(
                CultureInfo.CurrentCulture,
                GetLocalized(BreakupAppliedKey, BreakupAppliedFallback),
                idolName),
                NotificationManager._notification._type.idol_relationship_change);
            Close();
        }

        private static void RestoreDatingStatusKnowledge(data_girls.girls girl, bool statusWasKnown)
        {
            if (girl == null || girl.DatingData == null)
            {
                return;
            }

            girl.DatingData.Is_Partner_Status_Known = statusWasKnown;
            if (statusWasKnown)
            {
                girl.DatingData.Partner_Status_Known_To_Player =
                    data_girls.girls._dating_data._partner_status.free;
            }
        }

        private static void ApplyMaxPlayerRelationships()
        {
            string idolName = SafeGirlName(selectedGirl);
            int maximumPoints = Relationships_Player.GetPointsByLevel(MaximumRelationshipLevel);
            selectedGirl.Rel_Influence_Points = maximumPoints;
            selectedGirl.Rel_Friendship_Points = maximumPoints;
            selectedGirl.Rel_Romance_Points = maximumPoints;
            selectedGirl.UpdateButton();
            RefreshGirlAndList(selectedGirl);

            NotifySuccess(string.Format(
                CultureInfo.CurrentCulture,
                GetLocalized(RelationshipsAppliedKey, RelationshipsAppliedFallback),
                idolName),
                NotificationManager._notification._type.idol_relationship_change);
            Close();
        }

        private static void RefreshGirlAndList(data_girls.girls girl)
        {
            if (girl != null && girl.Update != null)
            {
                girl.Update();
            }

            data_girls dataGirls = GetDataComponent<data_girls>();
            if (dataGirls != null)
            {
                dataGirls.UpdateList(true);
            }
        }

        private static GameObject GetStylistGirlButtonPrefab(PopupManager manager)
        {
            PopupManager._popup stylingEntry = manager.GetByType(PopupManager._type.girl_styling);
            if (stylingEntry == null || stylingEntry.obj == null)
            {
                return null;
            }

            Styling_Popup stylingPopup = stylingEntry.obj.GetComponent<Styling_Popup>();
            return stylingPopup == null ? null : stylingPopup.prefab_girl_button;
        }

        private static bool TryRegisterPopup(PopupManager manager, GameObject root)
        {
            PopupManager._type type = (PopupManager._type)PopupTypeValue;
            PopupManager._popup existing = manager.GetByType(type);
            if (existing != null)
            {
                if (existing.obj != null && existing.obj != root)
                {
                    UnityEngine.Object.Destroy(existing.obj);
                }
                existing.obj = root;
                existing.open = false;
                existing.BGBlur = true;
                existing.BGDarken = true;
                existing.BGRenderTexture = null;
                return true;
            }

            PopupManager._popup popup = new PopupManager._popup
            {
                type = type,
                obj = root,
                open = false,
                BGBlur = true,
                BGDarken = true
            };

            if (manager.popups == null)
            {
                manager.popups = new PopupManager._popup[] { popup };
                return true;
            }

            Array.Resize(ref manager.popups, manager.popups.Length + 1);
            manager.popups[manager.popups.Length - 1] = popup;
            return true;
        }

        private static Transform GetPopupParent(PopupManager manager)
        {
            if (manager == null || manager.popups == null)
            {
                return null;
            }

            PopupManager._popup awardsPopup = manager.GetByType(PopupManager._type.awards);
            if (awardsPopup != null && awardsPopup.obj != null && awardsPopup.obj.transform.parent != null)
            {
                return awardsPopup.obj.transform.parent;
            }

            foreach (PopupManager._popup popup in manager.popups)
            {
                if (popup != null && popup.obj != null && popup.obj.transform.parent != null)
                {
                    return popup.obj.transform.parent;
                }
            }

            return null;
        }

        private static PopupManager GetPopupManager()
        {
            GameObject dataObject = GetMainScriptDataObject();
            return dataObject == null ? null : dataObject.GetComponent<PopupManager>();
        }

        private static T GetDataComponent<T>() where T : Component
        {
            GameObject dataObject = GetMainScriptDataObject();
            return dataObject == null ? null : dataObject.GetComponent<T>();
        }

        private static GameObject GetMainScriptDataObject()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return null;
            }

            mainScript main = camera.GetComponent<mainScript>();
            return main == null ? null : main.Data;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            float width,
            float height,
            Color32 background,
            UnityAction action)
        {
            GameObject buttonObject = CreateUIObject(name, parent);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            Image image = buttonObject.AddComponent<Image>();
            image.color = background;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick = new Button.ButtonClickedEvent();
            if (action != null)
            {
                button.onClick.AddListener(action);
            }

            TextMeshProUGUI text = CreateText(
                buttonObject.transform,
                "Text",
                label,
                17,
                TextAlignmentOptions.Center,
                mainScript.white32);
            Stretch(text.rectTransform);
            return button;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            if (parent != null)
            {
                obj.layer = parent.gameObject.layer;
            }
            return obj;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string text,
            int fontSize,
            TextAlignmentOptions alignment,
            Color32 color)
        {
            GameObject obj = CreateUIObject(name, parent);
            TextMeshProUGUI label = obj.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.enableWordWrapping = true;
            label.raycastTarget = false;
            CaptureDefaultFont();
            if (defaultFontSource != null && defaultFontSource.font != null)
            {
                label.font = defaultFontSource.font;
            }
            return label;
        }

        private static void CaptureDefaultFont()
        {
            if (defaultFontSource != null)
            {
                return;
            }

            TextMeshProUGUI[] labels = UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>();
            if (labels == null)
            {
                return;
            }

            foreach (TextMeshProUGUI label in labels)
            {
                if (label != null && label.font != null)
                {
                    defaultFontSource = label;
                    return;
                }
            }
        }

        private static void SetRect(
            RectTransform rect,
            float left,
            float top,
            float width,
            float height,
            bool anchoredFromTopLeft)
        {
            if (anchoredFromTopLeft)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(left, top);
            }
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null)
            {
                return;
            }

            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                if (child != null)
                {
                    SetLayerRecursively(child.gameObject, layer);
                }
            }
        }

        private static void DestroyExistingRoot()
        {
            if (popupRoot != null)
            {
                UnityEngine.Object.Destroy(popupRoot);
                popupRoot = null;
            }
        }

        private static void Close()
        {
            PopupManager.Close_();
        }

        private static string SafeGirlName(data_girls.girls girl)
        {
            if (girl == null)
            {
                return string.Empty;
            }

            try
            {
                return girl.GetName(true);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetLocalized(string key, string fallback)
        {
            return ModLocalization.Get(key, fallback);
        }

        private static void NotifySuccess(string message, NotificationManager._notification._type type)
        {
            NotificationManager.AddNotification(message, mainScript.green32, type);
        }

        private static void NotifyWarning(string key, string fallback)
        {
            NotificationManager.AddNotification(
                GetLocalized(key, fallback),
                mainScript.red32,
                NotificationManager._notification._type.other);
        }
    }
}
