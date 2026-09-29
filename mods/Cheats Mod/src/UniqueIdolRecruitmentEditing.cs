using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CheatsMod
{
    internal static partial class UniqueIdolRecruitmentPopup
    {
        private const int MaximumRecruitmentAge = 120;
        private const float MinimumRecruitmentStat = 0f;
        private const float MaximumRecruitmentStat = 100f;
        private const float EditButtonSize = 24f;
        private const float EditButtonGap = 8f;
        private const float StatRowHeight = 28f;
        private const string EditNameKey = "ui.unique_idols.edit_name";
        private const string EditAgeKey = "ui.unique_idols.edit_age";
        private const string EditStatKey = "ui.unique_idols.edit_stat";
        private const string StatLabelPrefix = "Stat_";
        private static Transform recruitmentPanel;

        private static void CreateNameLabel(Transform parent, UniqueIdolEntry entry,
            data_girls.girls girl, bool canRecruit)
        {
            entry.NameText = CreateEditableLabel(parent, NameObjectName, girl.GetName(true),
                NameTop, NameHeight, NameFontSize, GetCardPrimaryTextColor(entry, canRecruit));
            entry.NameText.richText = false;
            AddEditButton(parent, entry, NameTop, CheatUi.Text(EditNameKey), delegate { EditName(entry); });
        }

        private static void CreateAgeLabel(Transform parent, UniqueIdolEntry entry,
            data_girls.girls girl, bool canRecruit)
        {
            entry.AgeText = CreateEditableLabel(parent, AgeObjectName, FormatAge(girl),
                AgeTop, AgeHeight, AgeFontSize, GetCardSecondaryTextColor(entry, canRecruit));
            AddEditButton(parent, entry, AgeTop, CheatUi.Text(EditAgeKey), delegate { EditAge(entry); });
        }

        private static void CreateStatsLabels(Transform parent, UniqueIdolEntry entry,
            data_girls.girls girl, bool canRecruit)
        {
            for (int index = FirstItemIndex; index < DisplayedStatTypes.Length; index++)
            {
                data_girls._paramType stat = DisplayedStatTypes[index];
                float top = StatsTop - index * StatRowHeight;
                entry.StatLabels[stat] = CreateEditableLabel(parent, StatLabelPrefix + stat,
                    FormatStat(girl, stat), top, StatRowHeight, StatFontSize,
                    GetCardPrimaryTextColor(entry, canRecruit));
                string title = string.Format(CheatUi.Culture, CheatUi.Text(EditStatKey), GetStatLabel(stat));
                AddEditButton(parent, entry, top, title, delegate { EditStat(entry, stat); });
            }
        }

        private static TextMeshProUGUI CreateEditableLabel(Transform parent, string name, string text,
            float top, float height, int fontSize, Color32 color)
        {
            TextMeshProUGUI label = CreateText(parent, name, text, fontSize, TextAlignmentOptions.Left, color);
            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(BaseAnchor, EdgeAnchor);
            rect.anchorMax = new Vector2(EdgeAnchor, EdgeAnchor);
            rect.pivot = new Vector2(BaseAnchor, EdgeAnchor);
            rect.offsetMin = new Vector2(InnerCardInset, top - height);
            rect.offsetMax = new Vector2(-InnerCardInset - EditButtonSize - EditButtonGap, top);
            label.enableAutoSizing = true;
            label.fontSizeMin = SelectorLabelFontSize;
            label.fontSizeMax = fontSize;
            label.enableWordWrapping = true;
            return label;
        }

        private static void AddEditButton(Transform parent, UniqueIdolEntry entry, float top,
            string tooltip, UnityAction callback)
        {
            Button edit = CheatUi.NumericButton(parent, CheatUi.NumericAction.Edit, callback);
            RectTransform rect = edit.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(EdgeAnchor, EdgeAnchor);
            rect.sizeDelta = new Vector2(EditButtonSize, EditButtonSize);
            rect.anchoredPosition = new Vector2(-InnerCardInset, top);
            ButtonDefault native = edit.GetComponent<ButtonDefault>();
            if (native != null) { native.DefaultTooltip = tooltip; native.SetTooltip(tooltip); }
            CheatUi.SetButtonInteractable(edit, CanEditPreview(entry));
            entry.EditButtons.Add(edit);
        }

        private static bool CanEditPreview(UniqueIdolEntry entry)
        {
            return entry != null && entry.PreviewGirl != null && !entry.Recruited
                && CanRecruitUniqueAsset(entry.Asset);
        }

        private static void EditName(UniqueIdolEntry entry)
        {
            if (!CanEditPreview(entry) || recruitmentPanel == null) return;
            CheatNameEditor.Show(recruitmentPanel, entry.PreviewGirl.firstName, entry.PreviewGirl.lastName,
                delegate(string first, string last)
                {
                    if (!CanEditPreview(entry)) return;
                    entry.PreviewGirl.firstName = first;
                    entry.PreviewGirl.lastName = last;
                    entry.Name = entry.PreviewGirl.GetName(true);
                    RefreshEditableCard(entry);
                });
        }

        private static void EditAge(UniqueIdolEntry entry)
        {
            if (!CanEditPreview(entry) || recruitmentPanel == null) return;
            int minimumAge = Mathf.Min(entry.OriginalAge, FallbackIdolMinimumAge);
            CheatNumericEditor.Show(recruitmentPanel, CheatUi.Text(EditAgeKey), entry.PreviewGirl.GetAge(),
                minimumAge, MaximumRecruitmentAge, true, delegate(float value)
                {
                    if (!CanEditPreview(entry)) return;
                    int age = Mathf.Clamp(Mathf.RoundToInt(value), minimumAge, MaximumRecruitmentAge);
                    DateTime today = staticVars.dateTime.Date;
                    DateTime previousBirthday = entry.PreviewGirl.birthday;
                    int birthYear = today.Year - age;
                    // Preserve the month/day and the game's completed-birthdays age calculation.
                    if (previousBirthday.Month > today.Month
                        || (previousBirthday.Month == today.Month && previousBirthday.Day > today.Day))
                        birthYear--;
                    int birthDay = Math.Min(previousBirthday.Day, DateTime.DaysInMonth(birthYear, previousBirthday.Month));
                    entry.PreviewGirl.birthday = new DateTime(birthYear, previousBirthday.Month, birthDay);
                    // February 29 can clamp to February 28 across leap years. Keep the requested
                    // age exact when retaining the original month/day is impossible.
                    if (entry.PreviewGirl.GetAge() != age)
                        entry.PreviewGirl.birthday = today.AddYears(-age);
                    RefreshEditableCard(entry);
                });
        }

        private static void EditStat(UniqueIdolEntry entry, data_girls._paramType stat)
        {
            if (!CanEditPreview(entry) || recruitmentPanel == null) return;
            data_girls.girls.param parameter = entry.PreviewGirl.getParam(stat);
            if (parameter == null) return;
            string title = string.Format(CheatUi.Culture, CheatUi.Text(EditStatKey), GetStatLabel(stat));
            CheatNumericEditor.Show(recruitmentPanel, title, parameter.val,
                MinimumRecruitmentStat, MaximumRecruitmentStat, false, delegate(float value)
                {
                    if (!CanEditPreview(entry)) return;
                    parameter.val = Mathf.Clamp(value, MinimumRecruitmentStat, MaximumRecruitmentStat);
                    RefreshEditableCard(entry);
                });
        }

        private static string FormatAge(data_girls.girls girl)
        {
            return string.Format(CheatUi.Culture, GetLocalized(AgeFormatKey, AgeFormatFallback), girl.GetAge());
        }

        private static string FormatStat(data_girls.girls girl, data_girls._paramType stat)
        {
            data_girls.girls.param parameter = girl.getParam(stat);
            float value = parameter != null ? parameter.val : MinimumRecruitmentStat;
            int potential = parameter != null
                ? Mathf.Clamp(parameter.GetPotential(), ZeroCount, MaximumDisplayedPotential) : ZeroCount;
            return string.Format(CheatUi.Culture, GetLocalized(StatFormatKey, StatFormatFallback),
                GetStatLabel(stat), value.ToString(CheatUi.NumberFormat, CheatUi.Culture), potential);
        }

        private static void RefreshEditableCard(UniqueIdolEntry entry)
        {
            if (entry.NameText != null) entry.NameText.text = entry.PreviewGirl.GetName(true);
            if (entry.AgeText != null) entry.AgeText.text = FormatAge(entry.PreviewGirl);
            foreach (var stat in entry.StatLabels)
                if (stat.Value != null) stat.Value.text = FormatStat(entry.PreviewGirl, stat.Key);
            bool canEdit = CanEditPreview(entry);
            foreach (Button button in entry.EditButtons)
                if (button != null) CheatUi.SetButtonInteractable(button, canEdit);
        }
    }
}
