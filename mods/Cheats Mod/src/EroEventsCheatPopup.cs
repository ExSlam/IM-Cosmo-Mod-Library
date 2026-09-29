using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using ModLocalizationSystem;
using CheatsMod.EmbeddedIMUiFramework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CheatsMod
{
    internal static class EroEventsCheatPopup
    {
        private const int PopupTypeValue = 1431194195;
        private const string EroEventsAssemblyName = "com.seraph.eroevents";
        private const string EroEventsProbeTypeName = "EroEvents.Harmony_Checker";

        private const float PanelWidth = 930f;
        private const float PanelHeight = 640f;
        private const float Margin = 24f;
        private const float TitleHeight = 44f;
        private const float SearchTop = 70f;
        private const float SearchHeight = 40f;
        private const float BodyTop = 124f;
        private const float BodyBottom = 70f;
        private const float RowHeight = 124f;
        private const float RowSpacing = 10f;
        private const float ContentPaddingTop = 12f;
        private const float ContentPaddingBottom = 12f;
        private const float RowHorizontalPadding = 12f;
        private const float ActionButtonWidth = 155f;
        private const float ActionButtonHeight = 42f;
        private const float CloseButtonWidth = 160f;
        private const float CloseButtonHeight = 38f;

        private const string ButtonKey = "cheat.button.eroevents_cheats";
        private const string ButtonFallback = "EroEvents cheats";
        private const string TooltipKey = "cheat.tooltip.eroevents_cheats";
        private const string TooltipFallback = "Open optional EroEvents progression and grind shortcuts.";
        private const string TitleKey = "ui.eroevents_cheats.title";
        private const string TitleFallback = "EroEvents Cheats";
        private const string ApplyKey = "ui.eroevents_cheats.apply";
        private const string ChooseIdolKey = "ui.eroevents_cheats.choose_idol";
        private const string ChooseIdolFallback = "Choose idol";
        private const string CloseKey = "ui.eroevents_cheats.close";
        private const string AppliedKey = "notification.eroevents_cheats.applied";
        private const string AppliedFallback = "EroEvents cheat applied: {0}";
        private const string UnavailableKey = "notification.eroevents_cheats.unavailable";
        private const string UnavailableFallback = "EroEvents is not enabled.";
        private const string FailedKey = "notification.eroevents_cheats.failed";
        private const string FailedFallback = "EroEvents cheat action failed.";
        private const string CultureMaxKey = "notification.eroevents_cheats.culture_max";
        private const string CultureMaxFallback = "EroEvents Culture is already at its highest normal level.";
        private const string NoActiveTrainingKey = "notification.eroevents_cheats.no_active_training";
        private const string NoActiveTrainingFallback = "No active EroEvents training session was found.";
        private const string NoSexyTrainingKey = "notification.eroevents_cheats.no_sexy_training";
        private const string NoSexyTrainingFallback = "The active EroEvents training session is not Sexy training.";
        private const string NoCuteTrainingKey = "notification.eroevents_cheats.no_cute_training";
        private const string NoCuteTrainingFallback = "The active EroEvents training session is not Cute training.";

        private static GameObject popupRoot;
        private const string PopupName = "CheatsModEroEventsCheatPopup";
        private const string TitleName = "Title";
        private const string DescriptionName = "Description";
        private const string SearchName = "SearchInput";
        private const string ScrollName = "ScrollView";
        private const string RowNamePrefix = "EroEventsCheat_";
        private const string SearchSeparator = " ";
        private const float RowInset = 14f;
        private const float RowTitleTop = 8f;
        private const float RowTitleHeight = 44f;
        private const float RowDescriptionTop = 56f;
        private const float RowDescriptionHeight = 60f;
        private const int RowTitleFontSize = 18;
        private const int RowDescriptionFontSize = 14;
        private static readonly Color32 RowDescriptionColor = new Color32(70, 70, 70, 255);

        internal static string GetButtonLabel()
        {
            return GetLocalized(ButtonKey, ButtonFallback);
        }

        internal static string GetButtonTooltip()
        {
            return GetLocalized(TooltipKey, TooltipFallback);
        }

        internal static void Open()
        {
            try
            {
                if (!IsEroEventsAvailable())
                {
                    NotifyWarning(UnavailableKey, UnavailableFallback);
                    return;
                }

                PopupManager manager = GetPopupManager();
                if (manager == null)
                {
                    NotifyWarning(CheatLocalizationKeys.NotificationGameUnavailable, CheatFallbackText.NotificationGameUnavailable);
                    return;
                }

                List<CheatEntry> entries = BuildEntries();
                if (!CreatePopup(manager, entries))
                {
                    NotifyWarning(FailedKey, FailedFallback);
                    return;
                }

                PopupManager.OpenPopup((PopupManager._type)PopupTypeValue);
            }
            catch (Exception ex)
            {
                Debug.LogError("[CheatsMod] EroEvents cheat popup failed: " + ex);
                NotifyWarning(FailedKey, FailedFallback);
            }
        }

        private static List<CheatEntry> BuildEntries()
        {
            return new List<CheatEntry>
            {
                Entry("add_advancement", "Add 1 advancement point", "Add one EroEvents advancement point without skipping the next Culture-up scene.", AddAdvancementPoint),
                Entry("meet_culture_requirement", "Meet next Culture requirement", "Raise advancement points only as far as needed for the next normal Culture advancement.", MeetNextCultureRequirement),
                Entry("culture_0", "Set Culture to 0", "Set EroEvents Culture to 0 and reset advancement points, matching the built-in EE debug action.", delegate { SetCulture(0); }),
                Entry("culture_1", "Set Culture to 1", "Set EroEvents Culture to 1 and reset advancement points, matching the built-in EE debug action.", delegate { SetCulture(1); }),
                Entry("culture_2", "Set Culture to 2", "Set EroEvents Culture to 2 and reset advancement points, matching the built-in EE debug action.", delegate { SetCulture(2); }),
                Entry("culture_3", "Set Culture to 3", "Set EroEvents Culture to 3 and reset advancement points, matching the built-in EE debug action.", delegate { SetCulture(3); }),
                Entry("reset_chapter_lock", "Reset chapter lock", "Clear the EroEvents chapter cooldown and help-busy lock so another chapter event can proceed.", ResetChapterLock),
                Entry("unlock_swimsuits", "Unlock swimsuits", "Unlock the first EroEvents swimsuit tier without lowering a higher swimsuit unlock.", UnlockSwimsuits),
                Entry("unlock_microbikinis", "Unlock microbikinis", "Unlock both EroEvents swimsuit tiers, including microbikinis.", UnlockMicrobikinis),
                Entry("unlock_all_outfits", "Unlock all EroEvents outfits", "Enable the EroEvents all-outfits override used by its built-in debug menu.", UnlockAllOutfits),
                Entry("unlock_vip", "Unlock VIP beach invitation", "Mark Emmeline's introduction as complete so the VIP Club option can appear when other requirements are met.", UnlockVipInvitation),
                Entry("beach_treasures", "Get all beach treasures", "Grant the glass, wood, and shell beach treasures, matching the built-in EE debug action.", GetAllBeachTreasures),
                Entry("complete_training_breakthrough", "Complete current training breakthrough", "Set the active training progress to 100% so the normal summary can award the breakthrough and perform its cleanup.", CompleteTrainingBreakthrough),
                Entry("clear_training_gift_cooldown", "Clear training gift cooldown", "Set the shared EroEvents training gift cooldown to zero.", ClearTrainingGiftCooldown),
                Entry("unlock_sexy_upgrades", "Unlock current Sexy homework upgrades", "Unlock all three optional Sexy homework upgrades for the active Sexy training session.", UnlockSexyTrainingUpgrades),
                Entry("unlock_cute_upgrades", "Unlock current Cute homework upgrades", "Unlock all three optional Cute homework upgrades for the active Cute training session.", UnlockCuteTrainingUpgrades),
                IdolEntry("idol_culture1", "Mark idol Culture 1 complete", "Choose an idol and mark only her persistent EroEvents Culture 1 progression flag.", OpenCulture1IdolPicker),
                IdolEntry("idol_culture2", "Mark idol Culture 2 complete", "Choose an idol and mark only her persistent EroEvents Culture 2 progression flag.", OpenCulture2IdolPicker),
                IdolEntry("idol_trained_sexy", "Mark idol Sexy training complete", "Choose an idol and mark her persistent EroEvents training-complete and Sexy-trained flags. Active training targets are protected.", OpenSexyTrainingIdolPicker),
                IdolEntry("idol_trained_cute", "Mark idol Cute training complete", "Choose an idol and mark her persistent EroEvents training-complete and Cute-trained flags. Active training targets are protected.", OpenCuteTrainingIdolPicker)
            };
        }

        private static CheatEntry Entry(string id, string titleFallback, string descriptionFallback, Action action)
        {
            return new CheatEntry
            {
                Id = id,
                TitleKey = "ui.eroevents_cheats." + id + ".title",
                TitleFallback = titleFallback,
                DescriptionKey = "ui.eroevents_cheats." + id + ".description",
                DescriptionFallback = descriptionFallback,
                Apply = action
            };
        }

        private static CheatEntry IdolEntry(string id, string titleFallback, string descriptionFallback, Action action)
        {
            CheatEntry entry = Entry(id, titleFallback, descriptionFallback, action);
            entry.ActionLabelKey = ChooseIdolKey;
            entry.ActionLabelFallback = ChooseIdolFallback;
            entry.NotifyOnInvoke = false;
            return entry;
        }

        private static bool CreatePopup(PopupManager manager, List<CheatEntry> entries)
        {
            if (entries == null) return false;
            DestroyExistingRoot();
            Transform panel;
            popupRoot = CheatUi.CreateShell(manager, PopupName, new Vector2(PanelWidth, PanelHeight), out panel);
            TextMeshProUGUI title = CreateText(panel, TitleName, GetLocalized(TitleKey, TitleFallback),
                CheatUi.TitleFontSize, TextAlignmentOptions.Center, mainScript.black32);
            CheatUi.Place(title.rectTransform, Margin, CheatUi.TitleInset, PanelWidth - Margin * 2f, TitleHeight);
            ScrollRect scroll;
            RectTransform content;
            CreateScrollArea(panel, out scroll, out content);
            EroEventsCheatPopupController controller = popupRoot.AddComponent<EroEventsCheatPopupController>();
            List<CheatRow> rows = new List<CheatRow>();
            for (int index = 0; index < entries.Count; index++)
                rows.Add(CreateCheatRow(content, entries[index], index));
            controller.Initialize(rows, scroll, content);
            controller.ApplyFilter(string.Empty);
            CreateSearchBar(panel, controller);
            CreateCloseButton(panel);
            if (!TryRegisterPopup(manager, popupRoot)) { DestroyExistingRoot(); return false; }
            return true;
        }

        private static void CreateSearchBar(Transform panel, EroEventsCheatPopupController controller)
        {
            GameObject input = CheatUi.SearchInput(panel, SearchName, controller.ApplyFilter);
            CheatUi.Place(input.GetComponent<RectTransform>(), Margin, SearchTop,
                PanelWidth - Margin * 2f, SearchHeight);
        }

        private static void CreateScrollArea(Transform panel, out ScrollRect scrollRect, out RectTransform contentRect)
        {
            IMUiScrollViewHandle handle = CheatUi.Scroll(panel, ScrollName,
                new Vector2(Margin, BodyBottom), new Vector2(-Margin, -BodyTop));
            scrollRect = handle.ScrollRect;
            contentRect = handle.ScrollRect.content;
        }

        private static CheatRow CreateCheatRow(Transform parent, CheatEntry entry, int index)
        {
            GameObject row = IMUiPrimitives.CreateCard(parent, RowNamePrefix + entry.Id,
                Vector2.zero, IMUiTheme.Vanilla());
            RectTransform rect = row.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(CheatUi.Center, 1f);
            rect.sizeDelta = new Vector2(-RowHorizontalPadding * 2f, RowHeight);
            float textWidth = PanelWidth - Margin * 2f - CheatUi.ScrollGutter - RowHorizontalPadding * 2f
                - RowInset * 3f - ActionButtonWidth;
            TextMeshProUGUI title = CreateText(row.transform, TitleName,
                GetLocalized(entry.TitleKey, entry.TitleFallback), RowTitleFontSize,
                TextAlignmentOptions.TopLeft, mainScript.black32);
            CheatUi.Place(title.rectTransform, RowInset, RowTitleTop, textWidth, RowTitleHeight);
            TextMeshProUGUI description = CreateText(row.transform, DescriptionName,
                GetLocalized(entry.DescriptionKey, entry.DescriptionFallback), RowDescriptionFontSize,
                TextAlignmentOptions.TopLeft, RowDescriptionColor);
            CheatUi.Place(description.rectTransform, RowInset, RowDescriptionTop, textWidth, RowDescriptionHeight);
            string actionKey = string.IsNullOrEmpty(entry.ActionLabelKey) ? ApplyKey : entry.ActionLabelKey;
            Button button = CheatUi.Button(row.transform, actionKey, CheatUi.Text(actionKey),
                ActionButtonWidth, ActionButtonHeight, delegate { ApplyCheat(entry); });
            RectTransform actionRect = button.GetComponent<RectTransform>();
            actionRect.anchorMin = actionRect.anchorMax = actionRect.pivot = new Vector2(1f, CheatUi.Center);
            actionRect.anchoredPosition = new Vector2(-RowInset, 0f);
            return new CheatRow
            {
                Entry = entry, Root = row, Rect = rect,
                SearchText = string.Concat(title.text, SearchSeparator, description.text, SearchSeparator, entry.Id)
            };
        }

        private static void ApplyCheat(CheatEntry entry)
        {
            if (entry == null || entry.Apply == null)
            {
                return;
            }

            try
            {
                if (!IsEroEventsAvailable())
                {
                    NotifyWarning(UnavailableKey, UnavailableFallback);
                    return;
                }

                entry.Apply();
                if (entry.NotifyOnInvoke)
                {
                    string format = GetLocalized(AppliedKey, AppliedFallback);
                    string title = GetLocalized(entry.TitleKey, entry.TitleFallback);
                    NotificationManager.AddNotification(
                        string.Format(CheatUi.Culture, format, title),
                        mainScript.green32,
                        NotificationManager._notification._type.other);
                }
            }
            catch (CheatNotApplicableException ex)
            {
                NotifyWarning(ex.LocalizationKey, ex.Fallback);
            }
            catch (Exception ex)
            {
                Debug.LogError("[CheatsMod] EroEvents cheat action failed: " + ex);
                NotifyWarning(FailedKey, FailedFallback);
            }
        }

        private static void AddAdvancementPoint()
        {
            SetInt("ee_advance", GetInt("ee_advance") + 1);
        }

        private static void MeetNextCultureRequirement()
        {
            int culture = GetInt("ee_culture");
            int required;
            switch (culture)
            {
                case 0:
                    required = 2;
                    break;
                case 1:
                    required = 3;
                    break;
                case 2:
                    required = 2;
                    break;
                default:
                    throw new CheatNotApplicableException(CultureMaxKey, CultureMaxFallback);
            }

            SetInt("ee_advance", Math.Max(GetInt("ee_advance"), required));
        }

        private static void SetCulture(int culture)
        {
            SetInt("ee_culture", culture);
            SetInt("ee_advance", 0);
        }

        private static void ResetChapterLock()
        {
            SetInt("ee_chapter_cd", 0);
            SetInt("ee_help_busy", 0);
        }

        private static void UnlockSwimsuits()
        {
            SetInt("ee_swimsuit_unlock", Math.Max(GetInt("ee_swimsuit_unlock"), 1));
        }

        private static void UnlockMicrobikinis()
        {
            SetInt("ee_swimsuit_unlock", Math.Max(GetInt("ee_swimsuit_unlock"), 2));
        }

        private static void UnlockAllOutfits()
        {
            SetInt("ee_outfit_unlock", 1);
        }

        private static void UnlockVipInvitation()
        {
            SetInt("emmeline_intro", 1);
        }

        private static void GetAllBeachTreasures()
        {
            SetInt("ee_treasure_glass", 1);
            SetInt("ee_treasure_wood", 1);
            SetInt("ee_treasure_shell", 1);
        }

        private static void CompleteTrainingBreakthrough()
        {
            RequireActiveTraining();
            SetInt("ee_training_percent", 10);
        }

        private static void ClearTrainingGiftCooldown()
        {
            SetInt("ee_training_gift_cd", 0);
        }

        private static void UnlockSexyTrainingUpgrades()
        {
            RequireActiveTraining();
            if (GetInt("ee_training_sexy") <= 0)
            {
                throw new CheatNotApplicableException(NoSexyTrainingKey, NoSexyTrainingFallback);
            }

            SetInt("ee_training_sexy_unlock1", 1);
            SetInt("ee_training_sexy_unlock2", 1);
            SetInt("ee_training_sexy_unlock3", 1);
        }

        private static void UnlockCuteTrainingUpgrades()
        {
            RequireActiveTraining();
            if (GetInt("ee_training_cute") <= 0)
            {
                throw new CheatNotApplicableException(NoCuteTrainingKey, NoCuteTrainingFallback);
            }

            SetInt("ee_training_cute_unlock1", 1);
            SetInt("ee_training_cute_unlock2", 1);
            SetInt("ee_training_cute_unlock3", 1);
        }

        private static void OpenCulture1IdolPicker()
        {
            OpenIdolPicker(IdolTargetCheatPopup.TryOpenEroEventsCulture1);
        }

        private static void OpenCulture2IdolPicker()
        {
            OpenIdolPicker(IdolTargetCheatPopup.TryOpenEroEventsCulture2);
        }

        private static void OpenSexyTrainingIdolPicker()
        {
            OpenIdolPicker(IdolTargetCheatPopup.TryOpenEroEventsTrainedSexy);
        }

        private static void OpenCuteTrainingIdolPicker()
        {
            OpenIdolPicker(IdolTargetCheatPopup.TryOpenEroEventsTrainedCute);
        }

        private static void OpenIdolPicker(Func<bool> openPicker)
        {
            if (openPicker == null || !openPicker())
            {
                return;
            }

            PopupManager.Close_();
        }

        private static void RequireActiveTraining()
        {
            if (GetInt("ee_training_active") != 1)
            {
                throw new CheatNotApplicableException(NoActiveTrainingKey, NoActiveTrainingFallback);
            }
        }

        private static int GetInt(string name)
        {
            int value;
            return int.TryParse(variables.Get(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                ? value
                : 0;
        }

        private static void SetInt(string name, int value)
        {
            variables.Set(name, value.ToString(CultureInfo.InvariantCulture));
        }

        private static bool IsEroEventsAvailable()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int index = 0; index < assemblies.Length; index++)
            {
                Assembly assembly = assemblies[index];
                if (assembly == null)
                {
                    continue;
                }

                AssemblyName name;
                try
                {
                    name = assembly.GetName();
                }
                catch (Exception)
                {
                    continue;
                }

                if (name == null
                    || !string.Equals(name.Name, EroEventsAssemblyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    return assembly.GetType(EroEventsProbeTypeName, false) != null;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return false;
        }

        private static void CreateCloseButton(Transform panel)
        {
            Button close = CheatUi.Button(panel, CloseKey, CheatUi.Text(CloseKey),
                CloseButtonWidth, CloseButtonHeight, Close);
            CheatUi.Place(close.GetComponent<RectTransform>(), (PanelWidth - CloseButtonWidth) * CheatUi.Center,
                PanelHeight - CheatUi.FooterInset, CloseButtonWidth, CloseButtonHeight);
        }

        private static bool TryRegisterPopup(PopupManager manager, GameObject root)
        {
            return CheatUi.Register(PopupTypeValue, root);
        }

        private static PopupManager GetPopupManager()
        {
            GameObject data = GetMainScriptDataObject();
            return data == null ? null : data.GetComponent<PopupManager>();
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

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text,
            int fontSize, TextAlignmentOptions alignment, Color32 color)
        {
            return CheatUi.Label(parent, name, text, fontSize, alignment, color);
        }

        private static void Close()
        {
            PopupManager.Close_();
        }

        private static void DestroyExistingRoot()
        {
            if (popupRoot == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(popupRoot);
            popupRoot = null;
        }

        private static void NotifyWarning(string key, string fallback)
        {
            NotificationManager.AddNotification(
                GetLocalized(key, fallback),
                mainScript.red32,
                NotificationManager._notification._type.other);
        }

        private static string GetLocalized(string key, string fallback)
        {
            return ModLocalization.Get(key, fallback);
        }

        internal sealed class CheatEntry
        {
            internal string Id;
            internal string TitleKey;
            internal string TitleFallback;
            internal string DescriptionKey;
            internal string DescriptionFallback;
            internal string ActionLabelKey;
            internal string ActionLabelFallback;
            internal bool NotifyOnInvoke = true;
            internal Action Apply;
        }

        internal sealed class CheatRow
        {
            internal CheatEntry Entry;
            internal GameObject Root;
            internal RectTransform Rect;
            internal string SearchText;
        }

        private sealed class CheatNotApplicableException : Exception
        {
            internal readonly string LocalizationKey;
            internal readonly string Fallback;

            internal CheatNotApplicableException(string localizationKey, string fallback)
            {
                LocalizationKey = localizationKey;
                Fallback = fallback;
            }
        }

        internal static float GetContentHeight(int visibleCount)
        {
            if (visibleCount <= 0)
            {
                return ContentPaddingTop + ContentPaddingBottom;
            }

            return ContentPaddingTop
                + ContentPaddingBottom
                + (visibleCount * RowHeight)
                + ((visibleCount - 1) * RowSpacing);
        }

        internal static void PositionRow(CheatRow row, int visibleIndex)
        {
            if (row == null || row.Rect == null)
            {
                return;
            }

            float y = -(ContentPaddingTop + (visibleIndex * (RowHeight + RowSpacing)));
            row.Rect.anchoredPosition = new Vector2(0f, y);
        }
    }

    internal sealed class EroEventsCheatPopupController : MonoBehaviour
    {
        private List<EroEventsCheatPopup.CheatRow> rows;
        private ScrollRect scrollRect;
        private RectTransform contentRect;

        internal void Initialize(
            List<EroEventsCheatPopup.CheatRow> targetRows,
            ScrollRect targetScrollRect,
            RectTransform targetContentRect)
        {
            rows = targetRows;
            scrollRect = targetScrollRect;
            contentRect = targetContentRect;
        }

        internal void ApplyFilter(string query)
        {
            if (rows == null || scrollRect == null || contentRect == null)
            {
                return;
            }

            string normalized = string.IsNullOrEmpty(query) ? string.Empty : query.Trim();
            int visibleIndex = 0;
            for (int index = 0; index < rows.Count; index++)
            {
                EroEventsCheatPopup.CheatRow row = rows[index];
                bool visible = string.IsNullOrEmpty(normalized)
                    || (!string.IsNullOrEmpty(row.SearchText)
                        && row.SearchText.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) >= 0);
                if (row.Root != null)
                {
                    row.Root.SetActive(visible);
                }

                if (!visible)
                {
                    continue;
                }

                EroEventsCheatPopup.PositionRow(row, visibleIndex);
                visibleIndex++;
            }

            contentRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                EroEventsCheatPopup.GetContentHeight(visibleIndex));
            scrollRect.StopMovement();
            contentRect.anchoredPosition = Vector2.zero;
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }
}
