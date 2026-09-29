using System;
using System.Collections.Generic;
using System.Globalization;
using ModLocalizationSystem;
using CheatsMod.EmbeddedIMUiFramework;
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

        private const float PanelWidth = 1050f;
        private const float PanelHeight = 700f;
        private const float Margin = 24f;
        private const float TitleHeight = 44f;
        private const float LeftWidth = 520f;
        private const float ColumnGap = 22f;
        private const float RightWidth = PanelWidth - (Margin * 2f) - LeftWidth - ColumnGap;
        private const float BodyTop = 82f;
        private const float BodyBottom = 70f;
        private const float PickerLabelHeight = 30f;
        private const float ControlRowHeight = 46f;
        private const float ControlSpacing = 12f;
        private const float ArrowWidth = 44f;
        private const float ActionButtonHeight = 42f;
        private const float CloseButtonWidth = 160f;
        private const float CloseButtonHeight = 38f;

        private const string CancelTitleKey = "ui.graduation.cancel.title";
        private const string ChangeTitleKey = "ui.graduation.date.title";
        private const string SelectIdolKey = "ui.graduation.select_idol";
        private const string SelectedIdolKey = "ui.graduation.selected_idol";
        private const string SelectedIdolFallback = "Selected idol: {0}";
        private const string CurrentDateKey = "ui.graduation.current_date";
        private const string CurrentDateFallback = "Current graduation date: {0}";
        private const string NewDateKey = "ui.graduation.new_date";
        private const string NewDateFallback = "New graduation date: {0}";
        private const string MinimumDateKey = "ui.graduation.minimum_date";
        private const string DayKey = "ui.graduation.day";
        private const string MonthKey = "ui.graduation.month";
        private const string YearKey = "ui.graduation.year";
        private const string CloseKey = "ui.graduation.close";
        private const string CancelActionKey = "ui.graduation.cancel.action";
        private const string ApplyDateKey = "ui.graduation.date.apply";

        private const string NoAnnouncementsKey = "notification.no_announced_graduations";
        private const string NoAnnouncementsFallback = "No idols have announced a graduation.";
        private const string NoEditableIdolsKey = "notification.no_editable_graduation_dates";
        private const string NoEditableIdolsFallback = "No idols have an editable graduation date.";
        private const string CancelledKey = "notification.graduation_cancelled";
        private const string CancelledFallback = "{0}'s graduation announcement was cancelled. New date: {1}.";
        private const string DateChangedKey = "notification.graduation_date_changed";
        private const string DateChangedFallback = "{0}'s graduation date was changed to {1}.";
        private const string InvalidDateKey = "notification.invalid_graduation_date";
        private const string InvalidDateFallback = "Graduation dates must be at least three months from the current game date.";
        private const string NoSelectionKey = "notification.no_graduation_idol_selected";
        private const string NoSelectionFallback = "Select an idol first.";
        private const string FailedKey = "notification.graduation_cheat_failed";
        private const string FailedFallback = "Graduation cheat action failed.";

        private const string PopupName = "CheatsModGraduationPopup";
        private const string TitleName = "Title";
        private const string PickerName = "IdolScroll";
        private const float NameHeight = 54f;
        private const float CurrentDateOffset = 58f;
        private const float MinimumDateOffset = 112f;
        private const float NewDateOffset = 170f;
        private const float DateRowsOffset = 230f;
        private const float DateCaptionWidth = 96f;
        private const float DateValueWidth = 116f;
        private const float DateControlGap = 10f;
        private const float ActionBottomInset = 124f;
        private const int FirstCalendarDay = 1;
        private const int LastCalendarMonth = 12;
        private const int DateStep = 1;
        private const int MissingDateYear = 1900;
        private const string DateFormat = "d";
        private const string MissingDateKey = "ui.editor.no_date";
        private static readonly Dictionary<int, GameObject> selectionHighlights = new Dictionary<int, GameObject>();
        private static GameObject popupRoot;
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

        private static bool CreatePopup(PopupManager manager, List<data_girls.girls> eligible,
            GameObject girlButtonPrefab)
        {
            DestroyExistingRoot();
            selectionHighlights.Clear();
            Transform panel;
            popupRoot = CheatUi.CreateShell(manager, PopupName, new Vector2(PanelWidth, PanelHeight), out panel);
            string titleKey = currentMode == Mode.CancelAnnouncement ? CancelTitleKey : ChangeTitleKey;
            TextMeshProUGUI title = CreateText(panel, TitleName, CheatUi.Text(titleKey), CheatUi.TitleFontSize,
                TextAlignmentOptions.Center, mainScript.black32);
            CheatUi.Place(title.rectTransform, Margin, CheatUi.TitleInset, PanelWidth - Margin * 2f, CheatUi.TitleHeight);
            CreateGirlPicker(panel, eligible, girlButtonPrefab);
            CreateControls(panel);
            CreateCloseButton(panel);
            if (!TryRegisterPopup(manager, popupRoot)) { DestroyExistingRoot(); return false; }
            RefreshSelectionUI();
            return true;
        }

        private static void CreateGirlPicker(Transform panel, List<data_girls.girls> eligible,
            GameObject girlButtonPrefab)
        {
            CheatUi.LabelAt(panel, SelectIdolKey, Margin, BodyTop, LeftWidth, PickerLabelHeight);
            CheatUi.Picker(panel, PickerName, Margin, BodyTop + PickerLabelHeight,
                LeftWidth, PanelHeight - BodyTop - BodyBottom - PickerLabelHeight, eligible,
                girlButtonPrefab, SelectGirl, delegate(GameObject card, data_girls.girls girl)
                { selectionHighlights[girl.id] = CheatUi.Highlight(card); });
        }

        private static void CreateControls(Transform panel)
        {
            float left = Margin + LeftWidth + ColumnGap;
            selectedIdolText = CheatUi.LabelAt(panel, SelectedIdolKey, left, BodyTop, RightWidth, NameHeight);
            currentDateText = CheatUi.LabelAt(panel, CurrentDateKey, left, BodyTop + CurrentDateOffset, RightWidth, NameHeight);
            TextMeshProUGUI minimum = CheatUi.LabelAt(panel, MinimumDateKey, left,
                BodyTop + MinimumDateOffset, RightWidth, NameHeight, CheatUi.SmallFontSize);
            minimum.text = string.Format(CheatUi.Culture, minimum.text, FormatDate(minimumDate));
            newDateText = CheatUi.LabelAt(panel, NewDateKey, left, BodyTop + NewDateOffset, RightWidth, NameHeight);
            float top = BodyTop + DateRowsOffset;
            dayValueText = CreateDateRow(panel, left, top, CheatUi.Text(DayKey), DatePart.Day);
            monthValueText = CreateDateRow(panel, left, top + ControlRowHeight + ControlSpacing,
                CheatUi.Text(MonthKey), DatePart.Month);
            yearValueText = CreateDateRow(panel, left, top + (ControlRowHeight + ControlSpacing) * 2f,
                CheatUi.Text(YearKey), DatePart.Year);
            string actionKey = currentMode == Mode.CancelAnnouncement ? CancelActionKey : ApplyDateKey;
            actionButton = CreateButton(panel, actionKey, CheatUi.Text(actionKey), RightWidth,
                ActionButtonHeight,
                delegate { if (currentMode == Mode.CancelAnnouncement) CancelGraduation(); else ApplyDateChange(); });
            CheatUi.Place(actionButton.GetComponent<RectTransform>(), left, PanelHeight - ActionBottomInset,
                RightWidth, ActionButtonHeight);
        }

        private enum DatePart
        {
            Day,
            Month,
            Year
        }

        private static TextMeshProUGUI CreateDateRow(Transform panel, float left, float top,
            string label, DatePart part)
        {
            TextMeshProUGUI caption = CreateText(panel, part.ToString(), label, CheatUi.BodyFontSize,
                TextAlignmentOptions.MidlineLeft, mainScript.black32);
            CheatUi.Place(caption.rectTransform, left, top, DateCaptionWidth, ControlRowHeight);
            float cursor = left + DateCaptionWidth + DateControlGap;
            Button previous = CheatUi.NumericButton(panel, CheatUi.NumericAction.Decrease,
                delegate { AdjustDate(part, -DateStep); });
            CheatUi.Place(previous.GetComponent<RectTransform>(), cursor, top, ArrowWidth, ControlRowHeight);
            cursor += ArrowWidth + DateControlGap;
            TextMeshProUGUI value = CreateText(panel, part.ToString(), string.Empty, CheatUi.BodyFontSize,
                TextAlignmentOptions.Center, mainScript.black32);
            CheatUi.Place(value.rectTransform, cursor, top, DateValueWidth, ControlRowHeight);
            cursor += DateValueWidth + DateControlGap;
            Button next = CheatUi.NumericButton(panel, CheatUi.NumericAction.Increase,
                delegate { AdjustDate(part, DateStep); });
            CheatUi.Place(next.GetComponent<RectTransform>(), cursor, top, ArrowWidth, ControlRowHeight);
            cursor += ArrowWidth + DateControlGap;
            Button edit = CheatUi.NumericButton(panel, CheatUi.NumericAction.Edit, delegate
            {
                int current = part == DatePart.Day ? selectedDate.Day : part == DatePart.Month ? selectedDate.Month : selectedDate.Year;
                int minimum = part == DatePart.Year ? minimumDate.Year : FirstCalendarDay;
                int maximum = part == DatePart.Day ? DateTime.DaysInMonth(selectedDate.Year, selectedDate.Month)
                    : part == DatePart.Month ? LastCalendarMonth : maximumDate.Year;
                CheatNumericEditor.Show(panel, label, current, minimum, maximum, true,
                    delegate(float entered) { SetDatePart(part, Mathf.RoundToInt(entered)); });
            });
            CheatUi.Place(edit.GetComponent<RectTransform>(), cursor, top, ArrowWidth, ControlRowHeight);
            return value;
        }

        private static void SetDatePart(DatePart part, int value)
        {
            int year = part == DatePart.Year ? value : selectedDate.Year;
            int month = part == DatePart.Month ? value : selectedDate.Month;
            int day = part == DatePart.Day ? value : selectedDate.Day;
            day = Mathf.Min(day, DateTime.DaysInMonth(year, month));
            selectedDate = ClampDate(new DateTime(year, month, day));
            RefreshSelectionUI();
        }

        private static void CreateCloseButton(Transform panel)
        {
            Button close = CreateButton(panel, CloseKey, CheatUi.Text(CloseKey), CloseButtonWidth,
                CloseButtonHeight, Close);
            CheatUi.Place(close.GetComponent<RectTransform>(), (PanelWidth - CloseButtonWidth) * CheatUi.Center,
                PanelHeight - CheatUi.FooterInset, CloseButtonWidth, CloseButtonHeight);
        }

        private static void SelectGirl(data_girls.girls girl)
        {
            if (girl == null)
            {
                return;
            }

            selectedGirl = girl;
            selectedDate = GetInitialDate(girl);

            RefreshSelectionUI();
        }

        private static void RefreshSelectionUI()
        {
            if (selectedIdolText == null || selectedGirl == null)
            {
                return;
            }

            selectedIdolText.text = string.Format(
                CheatUi.Culture,
                GetLocalized(SelectedIdolKey, SelectedIdolFallback),
                SafeGirlName(selectedGirl));

            if (currentDateText != null)
            {
                currentDateText.text = string.Format(
                    CheatUi.Culture,
                    GetLocalized(CurrentDateKey, CurrentDateFallback),
                    FormatDate(selectedGirl.Graduation_Date));
            }

            foreach (KeyValuePair<int, GameObject> entry in selectionHighlights)
                if (entry.Value != null) entry.Value.SetActive(entry.Key == selectedGirl.id);

            selectedDate = ClampDate(selectedDate);
            if (newDateText != null)
            {
                newDateText.text = string.Format(
                    CheatUi.Culture,
                    GetLocalized(NewDateKey, NewDateFallback),
                    FormatDate(selectedDate));
            }

            if (dayValueText != null)
            {
                dayValueText.text = selectedDate.Day.ToString(CheatUi.Culture);
            }
            if (monthValueText != null)
            {
                monthValueText.text = selectedDate.Month.ToString(CheatUi.Culture);
            }
            if (yearValueText != null)
            {
                yearValueText.text = selectedDate.Year.ToString(CheatUi.Culture);
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

            if (selectedDate < GetMinimumDate())
            {
                NotifyWarning(InvalidDateKey, InvalidDateFallback);
                return;
            }
            string idolName = SafeGirlName(selectedGirl);
            data_girls._status restoredStatus = GetRestoredStatus(selectedGirl.previous_status);
            selectedGirl.previous_status = data_girls._status.announced_graduation;
            selectedGirl.status = restoredStatus;
            selectedGirl.Graduation_Date = ClampDate(selectedDate);
            selectedGirl.Will_Graduate_At_18 = false;
            CancelPendingRetiringSubstory(selectedGirl);
            RefreshGirlAndList(selectedGirl);

            NotifySuccess(
                string.Format(
                    CheatUi.Culture,
                    GetLocalized(CancelledKey, CancelledFallback),
                    idolName, FormatDate(selectedGirl.Graduation_Date)));
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
                    CheatUi.Culture,
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
            if (girl == null || girl.Graduation_Date.Year <= MissingDateYear)
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
            return date.Year <= MissingDateYear ? CheatUi.Text(MissingDateKey) : date.ToString(DateFormat, CheatUi.Culture);
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
            CheatUi.Initialize(manager);
            return CheatUi.Register(PopupTypeValue, root);
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

        private static Button CreateButton(Transform parent, string name, string label,
            float width, float height, UnityAction action)
        {
            return CheatUi.Button(parent, name, label, width, height, action);
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

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text,
            int fontSize, TextAlignmentOptions alignment, Color32 color)
        {
            return CheatUi.Label(parent, name, text, fontSize, alignment, color);
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
