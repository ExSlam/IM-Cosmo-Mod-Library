using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using ModLocalizationSystem;
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
        private const float RowHeight = 88f;
        private const float RowSpacing = 10f;
        private const float ContentPaddingTop = 12f;
        private const float ContentPaddingBottom = 12f;
        private const float RowHorizontalPadding = 12f;
        private const float ActionButtonWidth = 155f;
        private const float ActionButtonHeight = 42f;
        private const float ScrollbarWidth = 12f;
        private const float ScrollbarSpacing = 8f;
        private const float ScrollbarReserve = 28f;
        private const float ScrollSensitivity = 34f;
        private const float SearchTextPadding = 12f;
        private const float CloseButtonWidth = 160f;
        private const float CloseButtonHeight = 38f;

        private const string ButtonKey = "cheat.button.eroevents_cheats";
        private const string ButtonFallback = "EroEvents cheats";
        private const string TooltipKey = "cheat.tooltip.eroevents_cheats";
        private const string TooltipFallback = "Open optional EroEvents progression and grind shortcuts.";
        private const string TitleKey = "ui.eroevents_cheats.title";
        private const string TitleFallback = "EroEvents Cheats";
        private const string SearchKey = "ui.eroevents_cheats.search_placeholder";
        private const string SearchFallback = "Search EroEvents cheats";
        private const string ApplyKey = "ui.eroevents_cheats.apply";
        private const string ApplyFallback = "Apply";
        private const string ChooseIdolKey = "ui.eroevents_cheats.choose_idol";
        private const string ChooseIdolFallback = "Choose idol";
        private const string CloseKey = "ui.eroevents_cheats.close";
        private const string CloseFallback = "Close";
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
        private static TextMeshProUGUI defaultFontSource;

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
            Transform parent = GetPopupParent(manager);
            if (parent == null || entries == null)
            {
                return false;
            }

            DestroyExistingRoot();

            GameObject root = new GameObject(
                "CheatsModEroEventsCheatPopup",
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
                GetLocalized(TitleKey, TitleFallback),
                28,
                TextAlignmentOptions.Center,
                mainScript.black32);
            SetRect(title.rectTransform, Margin, -14f, PanelWidth - (Margin * 2f), TitleHeight, true);

            ScrollRect scrollRect;
            RectTransform contentRect;
            CreateScrollArea(panel.transform, out scrollRect, out contentRect);

            EroEventsCheatPopupController controller = root.AddComponent<EroEventsCheatPopupController>();
            List<CheatRow> rows = new List<CheatRow>();
            for (int index = 0; index < entries.Count; index++)
            {
                rows.Add(CreateCheatRow(contentRect.transform, entries[index], index));
            }

            controller.Initialize(rows, scrollRect, contentRect);
            controller.ApplyFilter(string.Empty);
            CreateSearchBar(panel.transform, controller);
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
            return true;
        }

        private static void CreateSearchBar(Transform panel, EroEventsCheatPopupController controller)
        {
            GameObject searchObject = CreateUIObject("SearchInput", panel);
            RectTransform searchRect = searchObject.GetComponent<RectTransform>();
            SetRect(searchRect, Margin, -SearchTop, PanelWidth - (Margin * 2f), SearchHeight, true);

            Image searchBackground = searchObject.AddComponent<Image>();
            searchBackground.color = mainScript.white32;
            searchBackground.raycastTarget = true;

            TMP_InputField searchInput = searchObject.AddComponent<TMP_InputField>();
            searchInput.targetGraphic = searchBackground;
            searchInput.textViewport = searchRect;
            searchInput.contentType = TMP_InputField.ContentType.Standard;
            searchInput.lineType = TMP_InputField.LineType.SingleLine;

            TextMeshProUGUI searchText = CreateText(
                searchObject.transform,
                "SearchText",
                string.Empty,
                16,
                TextAlignmentOptions.MidlineLeft,
                mainScript.black32);
            ConfigureSearchTextRect(searchText.rectTransform);
            searchInput.textComponent = searchText;

            TextMeshProUGUI placeholder = CreateText(
                searchObject.transform,
                "SearchPlaceholder",
                GetLocalized(SearchKey, SearchFallback),
                16,
                TextAlignmentOptions.MidlineLeft,
                new Color32(92, 92, 92, 255));
            ConfigureSearchTextRect(placeholder.rectTransform);
            searchInput.placeholder = placeholder;
            searchInput.text = string.Empty;
            searchInput.onValueChanged = new TMP_InputField.OnChangeEvent();
            searchInput.onValueChanged.AddListener(controller.ApplyFilter);
        }

        private static void ConfigureSearchTextRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(SearchTextPadding, 3f);
            rect.offsetMax = new Vector2(-SearchTextPadding, -3f);
        }

        private static void CreateScrollArea(
            Transform panel,
            out ScrollRect scrollRect,
            out RectTransform contentRect)
        {
            GameObject scrollObject = CreateUIObject("ScrollView", panel);
            RectTransform scrollObjectRect = scrollObject.GetComponent<RectTransform>();
            SetRect(
                scrollObjectRect,
                Margin,
                -BodyTop,
                PanelWidth - (Margin * 2f),
                PanelHeight - BodyTop - BodyBottom,
                true);
            Image scrollBackground = scrollObject.AddComponent<Image>();
            scrollBackground.color = new Color32(229, 229, 229, 255);
            scrollBackground.raycastTarget = true;

            scrollRect = scrollObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = ScrollSensitivity;

            GameObject viewport = CreateUIObject("Viewport", scrollObject.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(-ScrollbarReserve, 0f);
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = Color.white;
            viewportImage.raycastTarget = true;
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject content = CreateUIObject("Content", viewport.transform);
            contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            CreateScrollbar(scrollObject.transform, scrollRect);
        }

        private static CheatRow CreateCheatRow(Transform parent, CheatEntry entry, int index)
        {
            GameObject rowObject = CreateUIObject("EroEventsCheat_" + entry.Id, parent);
            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.sizeDelta = new Vector2(-(RowHorizontalPadding * 2f), RowHeight);

            Image rowBackground = rowObject.AddComponent<Image>();
            rowBackground.color = new Color32(250, 250, 250, 255);
            rowBackground.raycastTarget = true;

            TextMeshProUGUI title = CreateText(
                rowObject.transform,
                "Title",
                GetLocalized(entry.TitleKey, entry.TitleFallback),
                18,
                TextAlignmentOptions.TopLeft,
                mainScript.black32);
            title.enableWordWrapping = false;
            SetRect(title.rectTransform, 14f, -10f, 620f, 26f, true);

            TextMeshProUGUI description = CreateText(
                rowObject.transform,
                "Description",
                GetLocalized(entry.DescriptionKey, entry.DescriptionFallback),
                13,
                TextAlignmentOptions.TopLeft,
                new Color32(70, 70, 70, 255));
            description.enableWordWrapping = true;
            SetRect(description.rectTransform, 14f, -38f, 620f, 42f, true);

            Button applyButton = CreateButton(
                rowObject.transform,
                "Apply",
                GetLocalized(
                    string.IsNullOrEmpty(entry.ActionLabelKey) ? ApplyKey : entry.ActionLabelKey,
                    string.IsNullOrEmpty(entry.ActionLabelFallback) ? ApplyFallback : entry.ActionLabelFallback),
                ActionButtonWidth,
                ActionButtonHeight,
                mainScript.blue32,
                delegate { ApplyCheat(entry); });
            RectTransform applyRect = applyButton.GetComponent<RectTransform>();
            applyRect.anchorMin = new Vector2(1f, 0.5f);
            applyRect.anchorMax = new Vector2(1f, 0.5f);
            applyRect.pivot = new Vector2(1f, 0.5f);
            applyRect.sizeDelta = new Vector2(ActionButtonWidth, ActionButtonHeight);
            applyRect.anchoredPosition = new Vector2(-14f, 0f);

            return new CheatRow
            {
                Entry = entry,
                Root = rowObject,
                Rect = rowRect,
                SearchText = string.Concat(
                    title.text,
                    " ",
                    description.text,
                    " ",
                    entry.Id)
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
                        string.Format(CultureInfo.CurrentCulture, format, title),
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

        private static void CreateScrollbar(Transform parent, ScrollRect target)
        {
            Scrollbar template = GetScrollbarTemplate();
            GameObject scrollbarObject;
            Scrollbar scrollbar;
            if (template != null)
            {
                scrollbarObject = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
                scrollbarObject.name = "Scrollbar";
                SetLayerRecursively(scrollbarObject, parent.gameObject.layer);
                scrollbarObject.SetActive(true);
                scrollbar = scrollbarObject.GetComponent<Scrollbar>();
                if (scrollbar == null)
                {
                    scrollbar = scrollbarObject.AddComponent<Scrollbar>();
                }

                CanvasGroup group = scrollbarObject.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    group.alpha = 1f;
                    group.interactable = true;
                    group.blocksRaycasts = true;
                }
            }
            else
            {
                scrollbarObject = CreateUIObject("Scrollbar", parent);
                Image track = scrollbarObject.AddComponent<Image>();
                track.color = new Color32(229, 229, 229, 180);
                track.raycastTarget = true;
                scrollbar = scrollbarObject.AddComponent<Scrollbar>();

                GameObject handleObject = CreateUIObject("Handle", scrollbarObject.transform);
                RectTransform handleRect = handleObject.GetComponent<RectTransform>();
                Stretch(handleRect);
                Image handleImage = handleObject.AddComponent<Image>();
                handleImage.color = mainScript.blue32;
                scrollbar.handleRect = handleRect;
                scrollbar.targetGraphic = handleImage;
            }

            RectTransform rect = scrollbarObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(ScrollbarWidth, 0f);
            rect.anchoredPosition = new Vector2(-5f, 0f);
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.onValueChanged = new Scrollbar.ScrollEvent();
            scrollbar.onValueChanged.AddListener(delegate(float value)
            {
                target.verticalNormalizedPosition = value;
            });
            target.verticalScrollbar = scrollbar;
            target.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            target.verticalScrollbarSpacing = ScrollbarSpacing;
        }

        private static Scrollbar GetScrollbarTemplate()
        {
            PopupManager._type[] preferred = new PopupManager._type[]
            {
                PopupManager._type.producer_salaries,
                PopupManager._type.producer_contracts,
                PopupManager._type.producer_loans,
                PopupManager._type.notifications,
                PopupManager._type.awards,
                PopupManager._type.single_release,
                PopupManager._type.single_senbatsu,
                PopupManager._type.single_chart,
                PopupManager._type.SNS
            };

            PopupManager manager = GetPopupManager();
            if (manager != null)
            {
                for (int index = 0; index < preferred.Length; index++)
                {
                    PopupManager._popup popup = manager.GetByType(preferred[index]);
                    if (popup == null || popup.obj == null)
                    {
                        continue;
                    }

                    Scrollbar[] candidates = popup.obj.GetComponentsInChildren<Scrollbar>(true);
                    if (candidates != null && candidates.Length > 0)
                    {
                        return candidates[0];
                    }
                }
            }

            Scrollbar[] all = UnityEngine.Object.FindObjectsOfType<Scrollbar>();
            return all != null && all.Length > 0 ? all[0] : null;
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

            for (int index = 0; index < manager.popups.Length; index++)
            {
                PopupManager._popup popup = manager.popups[index];
                if (popup != null && popup.obj != null && popup.obj.transform.parent != null)
                {
                    return popup.obj.transform.parent;
                }
            }

            return null;
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
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            CaptureDefaultFont();
            if (defaultFontSource != null && defaultFontSource.font != null)
            {
                tmp.font = defaultFontSource.font;
            }

            return tmp;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            float width,
            float height,
            Color32 color,
            UnityAction onClick)
        {
            GameObject obj = CreateUIObject(name, parent);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            Image image = obj.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            Button button = obj.AddComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            TextMeshProUGUI text = CreateText(
                obj.transform,
                "Text",
                label,
                17,
                TextAlignmentOptions.Center,
                mainScript.white32);
            text.enableWordWrapping = false;
            Stretch(text.rectTransform);
            return button;
        }

        private static void CaptureDefaultFont()
        {
            if (defaultFontSource != null)
            {
                return;
            }

            TextMeshProUGUI[] all = UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>();
            if (all == null)
            {
                return;
            }

            for (int index = 0; index < all.Length; index++)
            {
                if (all[index] != null && all[index].font != null)
                {
                    defaultFontSource = all[index];
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
            bool topAnchored)
        {
            if (topAnchored)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
            }
            else
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = Vector2.zero;
            }

            rect.anchoredPosition = new Vector2(left, top);
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
