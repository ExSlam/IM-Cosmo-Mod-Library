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
    internal static class GraduationCheatPopup
    {
        private const int PopupTypeValue = 1431194193;
        private const int MinimumMonthsAhead = 3;
        private const int FirstIndex = 0;
        private const string RetiringDialogueId = "retiring";
        private const string RetiringPrimaryActorTag = "girl1";

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
        private const float ControlSpacing = 12f;
        private const float ArrowWidth = 44f;
        private const float ActionButtonWidth = 270f;
        private const float ActionButtonHeight = 42f;
        private const float CloseButtonWidth = 160f;
        private const float CloseButtonHeight = 38f;

        private const string CancelTitleKey = "ui.graduation.cancel.title";
        private const string CancelTitleFallback = "Cancel Graduation Announcement";
        private const string ChangeTitleKey = "ui.graduation.date.title";
        private const string ChangeTitleFallback = "Change Graduation Date";
        private const string SelectIdolKey = "ui.graduation.select_idol";
        private const string SelectIdolFallback = "Select an idol";
        private const string SelectedIdolKey = "ui.graduation.selected_idol";
        private const string SelectedIdolFallback = "Selected idol: {0}";
        private const string CurrentDateKey = "ui.graduation.current_date";
        private const string CurrentDateFallback = "Current graduation date: {0}";
        private const string NewDateKey = "ui.graduation.new_date";
        private const string NewDateFallback = "New graduation date: {0}";
        private const string MinimumDateKey = "ui.graduation.minimum_date";
        private const string MinimumDateFallback = "Earliest allowed date: {0}";
        private const string DayKey = "ui.graduation.day";
        private const string DayFallback = "Day";
        private const string MonthKey = "ui.graduation.month";
        private const string MonthFallback = "Month";
        private const string YearKey = "ui.graduation.year";
        private const string YearFallback = "Year";
        private const string CloseKey = "ui.graduation.close";
        private const string CloseFallback = "Close";
        private const string CancelActionKey = "ui.graduation.cancel.action";
        private const string CancelActionFallback = "Cancel graduation";
        private const string ApplyDateKey = "ui.graduation.date.apply";
        private const string ApplyDateFallback = "Set graduation date";

        private const string NoAnnouncementsKey = "notification.no_announced_graduations";
        private const string NoAnnouncementsFallback = "No idols have announced a graduation.";
        private const string NoEditableIdolsKey = "notification.no_editable_graduation_dates";
        private const string NoEditableIdolsFallback = "No idols have an editable graduation date.";
        private const string CancelledKey = "notification.graduation_cancelled";
        private const string CancelledFallback = "{0}'s graduation announcement was cancelled.";
        private const string DateChangedKey = "notification.graduation_date_changed";
        private const string DateChangedFallback = "{0}'s graduation date was changed to {1}.";
        private const string InvalidDateKey = "notification.invalid_graduation_date";
        private const string InvalidDateFallback = "Graduation dates must be at least three months from the current game date.";
        private const string NoSelectionKey = "notification.no_graduation_idol_selected";
        private const string NoSelectionFallback = "Select an idol first.";
        private const string FailedKey = "notification.graduation_cheat_failed";
        private const string FailedFallback = "Graduation cheat action failed.";

        private static GameObject popupRoot;
        private static TextMeshProUGUI defaultFontSource;
        private static Mode currentMode;
        private static data_girls.girls selectedGirl;
        private static DateTime selectedDate;
        private static DateTime minimumDate;
        private static DateTime maximumDate;
        private static TextMeshProUGUI selectedIdolText;
        private static TextMeshProUGUI currentDateText;
        private static TextMeshProUGUI newDateText;
        private static TextMeshProUGUI dayValueText;
        private static TextMeshProUGUI monthValueText;
        private static TextMeshProUGUI yearValueText;
        private static Button actionButton;

        private enum Mode
        {
            CancelAnnouncement,
            ChangeDate
        }

        internal static void OpenCancelAnnouncement()
        {
            Open(Mode.CancelAnnouncement);
        }

        internal static void OpenChangeDate()
        {
            Open(Mode.ChangeDate);
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
                        mode == Mode.CancelAnnouncement ? NoAnnouncementsKey : NoEditableIdolsKey,
                        mode == Mode.CancelAnnouncement ? NoAnnouncementsFallback : NoEditableIdolsFallback);
                    return;
                }

                GameObject girlButtonPrefab = GetStylistGirlButtonPrefab(manager);
                if (girlButtonPrefab == null)
                {
                    NotifyWarning(FailedKey, FailedFallback);
                    return;
                }

                currentMode = mode;
                minimumDate = GetMinimumDate();
                maximumDate = DateTime.MaxValue.Date;
                selectedGirl = eligible[FirstIndex];
                selectedDate = GetInitialDate(selectedGirl);

                if (!CreatePopup(manager, eligible, girlButtonPrefab))
                {
                    NotifyWarning(FailedKey, FailedFallback);
                    return;
                }

                PopupManager.OpenPopup((PopupManager._type)PopupTypeValue);
            }
            catch (Exception exception)
            {
                Debug.LogError("[CheatsMod] Graduation cheat popup failed: " + exception);
                NotifyWarning(FailedKey, FailedFallback);
            }
        }

        private static List<data_girls.girls> BuildEligibleGirls(Mode mode)
        {
            List<data_girls.girls> result = new List<data_girls.girls>();
            foreach (data_girls.girls girl in data_girls.girl)
            {
                if (girl == null || girl.Type != data_girls.girls._type.NORMAL)
                {
                    continue;
                }

                if (mode == Mode.CancelAnnouncement)
                {
                    if (girl.status == data_girls._status.announced_graduation)
                    {
                        result.Add(girl);
                    }
                }
                else if (girl.status != data_girls._status.graduated
                    && girl.status != data_girls._status.announced_graduation)
                {
                    result.Add(girl);
                }
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
                "CheatsModGraduationPopup",
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
                GetLocalized(
                    currentMode == Mode.CancelAnnouncement ? CancelTitleKey : ChangeTitleKey,
                    currentMode == Mode.CancelAnnouncement ? CancelTitleFallback : ChangeTitleFallback),
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
                item.name = "GraduationIdol_" + girl.id.ToString(CultureInfo.InvariantCulture);
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
            SetRect(selectedIdolText.rectTransform, left, top, RightWidth, 54f, true);

            currentDateText = CreateText(
                panel,
                "CurrentDate",
                string.Empty,
                16,
                TextAlignmentOptions.TopLeft,
                mainScript.black32);
            SetRect(currentDateText.rectTransform, left, top - 58f, RightWidth, 48f, true);

            if (currentMode == Mode.ChangeDate)
            {
                TextMeshProUGUI minimum = CreateText(
                    panel,
                    "MinimumDate",
                    string.Format(
                        CultureInfo.CurrentCulture,
                        GetLocalized(MinimumDateKey, MinimumDateFallback),
                        FormatDate(minimumDate)),
                    15,
                    TextAlignmentOptions.TopLeft,
                    mainScript.grey_light32);
                SetRect(minimum.rectTransform, left, top - 106f, RightWidth, 44f, true);

                newDateText = CreateText(
                    panel,
                    "NewDate",
                    string.Empty,
                    18,
                    TextAlignmentOptions.TopLeft,
                    mainScript.black32);
                SetRect(newDateText.rectTransform, left, top - 152f, RightWidth, 42f, true);

                float rowTop = top - 208f;
                dayValueText = CreateDateRow(panel, left, rowTop, GetLocalized(DayKey, DayFallback), DatePart.Day);
                monthValueText = CreateDateRow(
                    panel,
                    left,
                    rowTop - (ControlRowHeight + ControlSpacing),
                    GetLocalized(MonthKey, MonthFallback),
                    DatePart.Month);
                yearValueText = CreateDateRow(
                    panel,
                    left,
                    rowTop - ((ControlRowHeight + ControlSpacing) * 2f),
                    GetLocalized(YearKey, YearFallback),
                    DatePart.Year);

                actionButton = CreateButton(
                    panel,
                    "ApplyDate",
                    GetLocalized(ApplyDateKey, ApplyDateFallback),
                    ActionButtonWidth,
                    ActionButtonHeight,
                    mainScript.blue32,
                    ApplyDateChange);
                SetRect(
                    actionButton.GetComponent<RectTransform>(),
                    left + ((RightWidth - ActionButtonWidth) / 2f),
                    -PanelHeight + BodyBottom + ActionButtonHeight + 22f,
                    ActionButtonWidth,
                    ActionButtonHeight,
                    true);
            }
            else
            {
                actionButton = CreateButton(
                    panel,
                    "CancelGraduation",
                    GetLocalized(CancelActionKey, CancelActionFallback),
                    ActionButtonWidth,
                    ActionButtonHeight,
                    mainScript.red32,
                    CancelGraduation);
                SetRect(
                    actionButton.GetComponent<RectTransform>(),
                    left + ((RightWidth - ActionButtonWidth) / 2f),
                    top - 172f,
                    ActionButtonWidth,
                    ActionButtonHeight,
                    true);
            }
        }

        private enum DatePart
        {
            Day,
            Month,
            Year
        }

        private static TextMeshProUGUI CreateDateRow(
            Transform panel,
            float left,
            float top,
            string label,
            DatePart part)
        {
            TextMeshProUGUI caption = CreateText(
                panel,
                part.ToString() + "Label",
                label,
                16,
                TextAlignmentOptions.MidlineLeft,
                mainScript.black32);
            SetRect(caption.rectTransform, left, top, 92f, ControlRowHeight, true);

            Button previous = CreateButton(
                panel,
                part.ToString() + "Previous",
                "<",
                ArrowWidth,
                ControlRowHeight,
                mainScript.grey_light32,
                delegate { AdjustDate(part, -1); });
            SetRect(previous.GetComponent<RectTransform>(), left + 96f, top, ArrowWidth, ControlRowHeight, true);

            TextMeshProUGUI value = CreateText(
                panel,
                part.ToString() + "Value",
                string.Empty,
                19,
                TextAlignmentOptions.Center,
                mainScript.black32);
            SetRect(value.rectTransform, left + 146f, top, 104f, ControlRowHeight, true);

            Button next = CreateButton(
                panel,
                part.ToString() + "Next",
                ">",
                ArrowWidth,
                ControlRowHeight,
                mainScript.grey_light32,
                delegate { AdjustDate(part, 1); });
            SetRect(next.GetComponent<RectTransform>(), left + 256f, top, ArrowWidth, ControlRowHeight, true);

            return value;
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
            if (currentMode == Mode.ChangeDate)
            {
                selectedDate = GetInitialDate(girl);
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

            if (currentDateText != null)
            {
                currentDateText.text = string.Format(
                    CultureInfo.CurrentCulture,
                    GetLocalized(CurrentDateKey, CurrentDateFallback),
                    FormatDate(selectedGirl.Graduation_Date));
            }

            if (currentMode != Mode.ChangeDate)
            {
                return;
            }

            selectedDate = ClampDate(selectedDate);
            if (newDateText != null)
            {
                newDateText.text = string.Format(
                    CultureInfo.CurrentCulture,
                    GetLocalized(NewDateKey, NewDateFallback),
                    FormatDate(selectedDate));
            }

            if (dayValueText != null)
            {
                dayValueText.text = selectedDate.Day.ToString(CultureInfo.CurrentCulture);
            }
            if (monthValueText != null)
            {
                monthValueText.text = selectedDate.Month.ToString(CultureInfo.CurrentCulture);
            }
            if (yearValueText != null)
            {
                yearValueText.text = selectedDate.Year.ToString(CultureInfo.CurrentCulture);
            }
        }

        private static void AdjustDate(DatePart part, int delta)
        {
            DateTime next = selectedDate;
            try
            {
                switch (part)
                {
                    case DatePart.Day:
                        next = next.AddDays(delta);
                        break;
                    case DatePart.Month:
                        next = next.AddMonths(delta);
                        break;
                    case DatePart.Year:
                        next = next.AddYears(delta);
                        break;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                next = delta < 0 ? minimumDate : maximumDate;
            }

            selectedDate = ClampDate(next);
            RefreshSelectionUI();
        }

        private static void CancelGraduation()
        {
            if (selectedGirl == null)
            {
                NotifyWarning(NoSelectionKey, NoSelectionFallback);
                return;
            }

            if (selectedGirl.status != data_girls._status.announced_graduation)
            {
                NotifyWarning(NoAnnouncementsKey, NoAnnouncementsFallback);
                return;
            }

            string idolName = SafeGirlName(selectedGirl);
            data_girls._status restoredStatus = GetRestoredStatus(selectedGirl.previous_status);
            selectedGirl.previous_status = data_girls._status.announced_graduation;
            selectedGirl.status = restoredStatus;
            selectedGirl.Graduation_Set_Default_Date();
            selectedGirl.Will_Graduate_At_18 = false;
            CancelPendingRetiringSubstory(selectedGirl);
            RefreshGirlAndList(selectedGirl);

            NotifySuccess(
                string.Format(
                    CultureInfo.CurrentCulture,
                    GetLocalized(CancelledKey, CancelledFallback),
                    idolName));
            Close();
        }

        private static data_girls._status GetRestoredStatus(data_girls._status previousStatus)
        {
            if (previousStatus == data_girls._status.announced_graduation
                || previousStatus == data_girls._status.graduated
                || previousStatus == data_girls._status.scene
                || previousStatus == data_girls._status.practice)
            {
                return data_girls._status.normal;
            }

            return previousStatus;
        }

        private static void CancelPendingRetiringSubstory(data_girls.girls girl)
        {
            if (girl == null || Substories_Manager.dialogueQueue == null)
            {
                return;
            }

            Substories_Manager._substoryData retiringData = Substories_Manager.GetSubstoryData(RetiringDialogueId);
            Substories_Manager._substoryData._actor primaryActor = retiringData == null
                ? null
                : retiringData.GetActor(RetiringPrimaryActorTag);
            if (primaryActor == null || primaryActor.girl != girl)
            {
                return;
            }

            bool removedQueuedDialogue = false;
            for (int queueIndex = Substories_Manager.dialogueQueue.Count - 1; queueIndex >= FirstIndex; queueIndex--)
            {
                Substories_Manager._dialogueQueue queued = Substories_Manager.dialogueQueue[queueIndex];
                if (queued != null
                    && queued.dialogue != null
                    && string.Equals(queued.dialogue.id, RetiringDialogueId, StringComparison.Ordinal))
                {
                    Substories_Manager.dialogueQueue.RemoveAt(queueIndex);
                    removedQueuedDialogue = true;
                }
            }

            if (!removedQueuedDialogue)
            {
                return;
            }

            if (Substories_Manager.substoryData != null)
            {
                for (int dataIndex = Substories_Manager.substoryData.Count - 1; dataIndex >= FirstIndex; dataIndex--)
                {
                    Substories_Manager._substoryData data = Substories_Manager.substoryData[dataIndex];
                    if (data != null && string.Equals(data.id, RetiringDialogueId, StringComparison.Ordinal))
                    {
                        Substories_Manager.substoryData.RemoveAt(dataIndex);
                    }
                }
            }

            if (Substories_Manager.UsedSubstories != null)
            {
                Substories_Manager.UsedSubstories.Remove(RetiringDialogueId);
            }
            if (Substories_Manager.Delayed_Queue != null)
            {
                Substories_Manager.Delayed_Queue.Remove(RetiringDialogueId);
            }

            data_dialogues._dialogue dialogue = data_dialogues.GetDialogueByID(RetiringDialogueId);
            if (dialogue != null)
            {
                dialogue.Date_LastTriggered = null;
            }
        }

        private static void ApplyDateChange()
        {
            if (selectedGirl == null)
            {
                NotifyWarning(NoSelectionKey, NoSelectionFallback);
                return;
            }

            if (selectedGirl.status == data_girls._status.graduated
                || selectedGirl.status == data_girls._status.announced_graduation)
            {
                NotifyWarning(NoEditableIdolsKey, NoEditableIdolsFallback);
                return;
            }

            DateTime target = ClampDate(selectedDate);
            if (target < GetMinimumDate())
            {
                NotifyWarning(InvalidDateKey, InvalidDateFallback);
                return;
            }

            selectedGirl.Graduation_Date = target;
            selectedGirl.Will_Graduate_At_18 = false;
            RefreshGirlAndList(selectedGirl);

            NotifySuccess(
                string.Format(
                    CultureInfo.CurrentCulture,
                    GetLocalized(DateChangedKey, DateChangedFallback),
                    SafeGirlName(selectedGirl),
                    FormatDate(target)));
            Close();
        }

        private static DateTime GetMinimumDate()
        {
            DateTime gameDate = staticVars.dateTime.Date;
            try
            {
                return gameDate.AddMonths(MinimumMonthsAhead);
            }
            catch (ArgumentOutOfRangeException)
            {
                return DateTime.MaxValue.Date;
            }
        }

        private static DateTime GetInitialDate(data_girls.girls girl)
        {
            if (girl == null || girl.Graduation_Date.Year <= 1900)
            {
                return minimumDate;
            }

            return ClampDate(girl.Graduation_Date.Date);
        }

        private static DateTime ClampDate(DateTime value)
        {
            DateTime date = value.Date;
            if (date < minimumDate)
            {
                return minimumDate;
            }
            if (date > maximumDate)
            {
                return maximumDate;
            }
            return date;
        }

        private static string FormatDate(DateTime date)
        {
            if (date.Year <= 1900)
            {
                return "-";
            }

            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
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

        private static void NotifySuccess(string message)
        {
            NotificationManager.AddNotification(
                message,
                mainScript.green32,
                NotificationManager._notification._type.idol_status_change);
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
