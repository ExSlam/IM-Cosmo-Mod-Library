using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GraduationCalendar
{
    internal static class HarmonyTargetMethodNames
    {
        internal const string ProfileRenderTabExtras = "RenderTab_Extras";
        internal const string PopupManagerStart = "Start";
        internal const string PopupManagerClose = "Close";
    }

    [HarmonyPatch(typeof(Profile_Popup), HarmonyTargetMethodNames.ProfileRenderTabExtras)]
    internal static class Profile_Popup_RenderTab_Extras_Patch
    {
        private const int GraduationAgeMonths = 216;
        private const int MonthsPerYear = 12;
        private const int UnknownGraduationYear = 1900;
        private const string GraduationLabelLocalizationKey = "profile.extras.graduation_label";
        private const string GraduationLabelFallback = "Graduation: ";
        private const string UnknownLocalizationKey = "common.unknown";
        private const string UnknownFallbackLabel = "Unknown";
        private const string GraduationDateLocalizationFormat = "DATETIME__MONTH";

        private static void Postfix(Profile_Popup __instance)
        {
            if (__instance == null || __instance.Girl == null)
            {
                return;
            }

            data_girls.girls girl = __instance.Girl;
            if (!ShouldShowForGirl(girl))
            {
                return;
            }

            if (__instance.Extras_Container == null || __instance.prefab_text == null || __instance.prefab_divider == null)
            {
                return;
            }

            string graduationText = BuildGraduationText(girl);
            if (string.IsNullOrEmpty(graduationText))
            {
                return;
            }

            AddDivider(__instance);
            AddText(__instance, graduationText);
            LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.Extras_Container.GetComponent<RectTransform>());
        }

        private static bool ShouldShowForGirl(data_girls.girls girl)
        {
            if (girl == null)
            {
                return false;
            }
            return girl.status != data_girls._status.graduated;
        }

        private static string BuildGraduationText(data_girls.girls girl)
        {
            DateTime graduationDate;
            bool known;
            GetGraduationDate(girl, out graduationDate, out known);
            string label = ExtensionMethods.color(
                ModLocalization.Get(GraduationLabelLocalizationKey, GraduationLabelFallback),
                mainScript.blue);
            if (!known)
            {
                return label + ModLocalization.Get(UnknownLocalizationKey, UnknownFallbackLabel);
            }
            string dateText = GetGraduationDateString(graduationDate);
            if (string.IsNullOrEmpty(dateText))
            {
                return label + ModLocalization.Get(UnknownLocalizationKey, UnknownFallbackLabel);
            }
            return label + dateText;
        }

        private static void GetGraduationDate(data_girls.girls girl, out DateTime date, out bool known)
        {
            date = girl.Graduation_Date;
            if (girl.Will_Graduate_At_18)
            {
                int months = GraduationAgeMonths - girl.GetAge() * MonthsPerYear;
                date = staticVars.dateTime.AddMonths(months);
            }
            known = date.Year != UnknownGraduationYear;
        }

        private static string GetGraduationDateString(DateTime date)
        {
            return ExtensionMethods.ToString_Loc(date, GraduationDateLocalizationFormat);
        }

        private static void AddText(Profile_Popup popup, string text)
        {
            GameObject obj = UnityEngine.Object.Instantiate(popup.prefab_text);
            ExtensionMethods.SetText(obj, text);
            obj.transform.SetParent(popup.Extras_Container.transform, false);
        }

        private static void AddDivider(Profile_Popup popup)
        {
            UnityEngine.Object.Instantiate(popup.prefab_divider).transform.SetParent(popup.Extras_Container.transform, false);
        }
    }

    [HarmonyPatch(typeof(PopupManager), HarmonyTargetMethodNames.PopupManagerStart)]
    internal static class PopupManager_Start_Patch
    {
        private static void Postfix(PopupManager __instance)
        {
            GraduationCalendarUI.EnsureBootstrap(__instance);
        }
    }

    [HarmonyPatch(typeof(PopupManager))]
    internal static class PopupManager_Close_Patch
    {
        private const int ActionCallbackParameterCount = 1;
        private const int NoArgumentParameterCount = 0;

        private static MethodInfo cachedCloseMethod;

        private static bool Prepare()
        {
            MethodInfo ignored;
            return TryResolveTargetMethod(out ignored);
        }

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            MethodInfo target;
            if (!TryResolveTargetMethod(out target))
            {
                return null;
            }

            return target;
        }

        private static bool TryResolveTargetMethod(out MethodInfo target)
        {
            if (cachedCloseMethod != null)
            {
                target = cachedCloseMethod;
                return true;
            }

            MethodInfo actionCallbackCandidate = null;
            MethodInfo noArgsCandidate = null;
            MethodInfo fallbackCandidate = null;

            MethodInfo[] methods = typeof(PopupManager).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method == null)
                {
                    continue;
                }

                bool isCloseName =
                    string.Equals(method.Name, HarmonyTargetMethodNames.PopupManagerClose, StringComparison.OrdinalIgnoreCase);
                if (!isCloseName)
                {
                    continue;
                }

                if (fallbackCandidate == null)
                {
                    fallbackCandidate = method;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters == null)
                {
                    continue;
                }

                if (parameters.Length == ActionCallbackParameterCount && parameters[0].ParameterType == typeof(Action))
                {
                    actionCallbackCandidate = method;
                    continue;
                }

                if (parameters.Length == NoArgumentParameterCount && noArgsCandidate == null)
                {
                    noArgsCandidate = method;
                }
            }

            cachedCloseMethod = actionCallbackCandidate ?? noArgsCandidate ?? fallbackCandidate;
            target = cachedCloseMethod;
            return target != null;
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(PopupManager __instance)
        {
            GraduationCalendarUI.OnPopupManagerClose(__instance);
        }
    }

    internal sealed class GraduationCalendarBootstrap : MonoBehaviour
    {
        private const float RetryIntervalSeconds = 2f;
        private float nextTryTime;

        private void Update()
        {
            if (Time.unscaledTime < nextTryTime)
            {
                return;
            }
            nextTryTime = Time.unscaledTime + RetryIntervalSeconds;
            GraduationCalendarUI.TryInitialize();
        }
    }

    internal sealed class GraduationCalendarPopupRecoveryTicker : MonoBehaviour
    {
        private const float TickIntervalSeconds = 0.5f;
        private const float InitialDelaySeconds = 0.35f;
        private float nextTickAt;

        internal static void Ensure(PopupManager manager)
        {
            if (manager == null)
            {
                return;
            }

            if (manager.GetComponent<GraduationCalendarPopupRecoveryTicker>() != null)
            {
                return;
            }

            manager.gameObject.AddComponent<GraduationCalendarPopupRecoveryTicker>();
        }

        private void OnEnable()
        {
            nextTickAt = Time.unscaledTime + InitialDelaySeconds;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextTickAt)
            {
                return;
            }

            nextTickAt = Time.unscaledTime + TickIntervalSeconds;
            GraduationCalendarUI.RunBackdropRecoveryTick();
        }
    }

    internal sealed class GraduationCalendarMarker : MonoBehaviour
    {
    }

    internal static class GraduationCalendarUI
    {
        private const string ButtonName = "GraduationCalendar_Button";
        private const string PopupName = "GraduationCalendar_Popup";
        private const string MenuButtonTooltipLocalizationKey = "menu.button_tooltip";
        private const string MenuButtonTooltipFallbackLabel = "Graduation Calendar";
        private const string MenuButtonLabelLocalizationKey = "menu.button_label";
        private const string MenuButtonLabelFallbackText = "Calendar";
        private const string ModTitleLocalizationKey = "ui.title";
        private const string ModTitleFallbackLabel = "Graduation Calendar";
        private static readonly string ButtonTooltip = ModLocalization.Get(MenuButtonTooltipLocalizationKey, MenuButtonTooltipFallbackLabel);
        private static readonly string ButtonLabel = ModLocalization.Get(MenuButtonLabelLocalizationKey, MenuButtonLabelFallbackText);
        private const string LogPrefix = "[GraduationCalendar]";
        private static readonly string ModTitle = ModLocalization.Get(ModTitleLocalizationKey, ModTitleFallbackLabel);
        private const string ModFolderFallback = "Graduation Calendar";
        private const int GridColumns = 6;
        private const float GridCellWidth = 110f;
        private const float GridCellHeight = 84f;
        private const float PortraitSize = 64f;
        private const float NameHeight = 12f;
        private const int CalendarColumns = 7;
        private const float CalendarCellWidth = 96f;
        private const float CalendarCellHeight = 86f;
        private const float CalendarHeaderHeight = 22f;
        private const float CalendarPortraitSize = 22f;
        private const int SelectorFontSize = 22;
        private const float SelectorLabelHeight = 28f;
        private const float ArrowButtonWidth = 32f;
        private const float ArrowButtonHeight = 28f;
        private const float BackdropSyncDurationSeconds = 0.1f;
        private const float PopupGhostAlphaThreshold = 0.001f;
        private const string SuperBlurTypeName = "SuperBlur";
        private const string SuperBlurFastTypeName = "SuperBlurFast";
        private const string InterpolationPropertyName = "interpolation";
        private const int MonthsInYear = 12;
        private const int FirstMonthOfYear = 1;
        private const int UnknownGraduationYear = 1900;
        private const int GraduationAgeMonths = 216;
        private const int DateKeyYearMultiplier = 100;
        private const float VisibleAlpha = 1f;
        private const float HiddenAlpha = 0f;
        private const float DisabledBlurInterpolationValue = 0f;
        private const float VisibleCanvasAlphaThreshold = 0.99f;
        private const float ScrollTopNormalizedPosition = 1f;
        private const string EmptyString = "";
        private const string SpaceSeparator = " ";
        private const string HierarchyPathSeparator = "/";
        private const string LogTimeFormat = "HH:mm:ss";
        private const string BepInExFolderName = "BepInEx";
        private const string LogFileName = "GraduationCalendar.log";
        private const string EnsureBootstrapLogMessage = "EnsureBootstrap invoked.";
        private const string AwardsButtonNotFoundReason = "Awards button not found";
        private const string CreateMenuButtonMissingParentLogMessage = "CreateMenuButton skipped (awards button missing parent).";
        private const string LogDiagnosticsDumpSuffix = ". Dumping button candidates for troubleshooting.";
        private const string LogAwardsLabelResolvedFormat = "Awards label resolved as: '{0}'.";
        private const string LogLangButtonCountFormat = "Lang_Button count: {0}.";
        private const string LogLangButtonDetailFormat = "Lang_Button: name='{0}', constant='{1}', tooltip='{2}', active={3}, path='{4}'.";
        private const string LogButtonDefaultCountFormat = "ButtonDefault count: {0}.";
        private const string LogButtonDefaultDetailFormat = "ButtonDefault: name='{0}', tooltip='{1}', label='{2}', active={3}, path='{4}'.";
        private const string LogButtonCountFormat = "Button count: {0}.";
        private const string LogButtonDetailFormat = "Button: name='{0}', label='{1}', active={2}, path='{3}'.";
        private const string LogDiagnosticsFailedFormat = "Diagnostics failed: {0}";
        private const string LogCreatedMenuButtonFormat = "Created menu button clone from '{0}' -> '{1}'.";
        private const string LogPopupOpenStateFormat = "Open: popupRoot active={0}, layer={1}.";
        private const string LogRenderYearNoIdolsFormat = "RenderYear {0}: no active idols found.";
        private const string LogRenderYearSummaryFormat = "RenderYear {0}: totalGirls={1}, unknown={2}, contentChildren={3}.";
        private const string LogMenuButtonParentLayoutFormat = "Menu button parent layout: {0}.";
        private const string LogMenuButtonManualPositionFormat = "Menu button positioned manually. AwardsPos={0}, ClonePos={1}.";
        private const string LogCalendarIconNotFoundFormat = "Calendar icon not found. Expected at '{0}'.";
        private const string LogCalendarIconTargetMissing = "Calendar icon target image not found.";
        private const string LogCalendarIconRunnerMissing = "Calendar icon load failed: no coroutine runner.";
        private const string LogCalendarIconLoadingFormat = "Calendar icon loading from '{0}'.";
        private const string AwardsLocalizationDictionaryKey = "AWARDS";
        private const string AwardsFallbackLabel = "Awards";
        private const string AwardsNameKeyword = "award";
        private const string UnknownLocalizationDictionaryKey = "UNKNOWN";
        private const string UnknownLocalizationKey = "common.unknown";
        private const string UnknownFallbackLabel = "Unknown";
        private const string CloseLocalizationDictionaryKey = "CLOSE";
        private const string BackLocalizationDictionaryKey = "BACK";
        private const string CloseLocalizationKey = "common.close";
        private const string CloseFallbackLabel = "Close";
        private const string ViewCalendarLocalizationKey = "view.calendar";
        private const string ViewCalendarFallbackLabel = "Calendar View";
        private const string ViewGraduationsOnlyLocalizationKey = "view.graduations_only";
        private const string ViewGraduationsOnlyFallbackLabel = "Graduations Only";
        private const string ViewYearlyLocalizationKey = "view.yearly";
        private const string ViewYearlyFallbackLabel = "Yearly View";
        private const string ViewMonthlyLocalizationKey = "view.monthly";
        private const string ViewMonthlyFallbackLabel = "Monthly View";
        private const string NoIdolsLocalizationKey = "empty.no_idols";
        private const string NoIdolsFallbackLabel = "No idols available";
        private const string NoGraduationsLocalizationKey = "empty.no_graduations";
        private const string NoGraduationsFallbackLabel = "No graduations";
        private const string NoGraduationsScheduledLocalizationKey = "empty.no_graduations_scheduled";
        private const string NoGraduationsScheduledFallbackLabel = "No graduations scheduled";
        private const string MonthNameFormat = "MMMM";
        private const string LegacyFontFallbackName = "Arial.ttf";
        private const string TextObjectName = "Text";
        private const string ScrollbarObjectName = "Scrollbar";
        private const string ScrollbarHandleObjectName = "Handle";
        private const string ArrowNextLabel = ">";
        private const string ArrowPreviousLabel = "<";
        private const string CalendarIconObjectName = "CalendarIcon";
        private const string CalendarIconFileName = "calendar.png";
        private const string PluginsFolderName = "Plugins";
        private const string PluginsLowercaseFolderName = "plugins";
        private const string TexturesFolderName = "Textures";
        private const string UiFolderName = "UI";
        private const string IconsFolderName = "Icons";
        private const string IconKeyword = "icon";
        private const string CalendarKeyword = "calendar";
        private const string ArrowKeyword = "arrow";
        private const string LeftKeyword = "left";
        private const string RightKeyword = "right";
        private const string NextKeyword = "next";
        private const string PreviousKeyword = "prev";
        private const string RoundKeyword = "round";
        private const string CircleKeyword = "circle";
        private const string ButtonKeyword = "button";
        private const string BackgroundShortKeyword = "bg";
        private const string BackgroundKeyword = "background";
        private const string RedKeyword = "red";
        private const string PinkKeyword = "pink";
        private const string CancelKeyword = "cancel";
        private const string ImageKeyword = "image";

        private static bool initialized;
        private static bool initializing;
        private static bool popupOpen;
        private static bool popupClosing;
        private static bool backdropApplied;
        private static bool loggedFindAwardsFailure;
        private static bool loggedBootstrap;
        private static string logPath;
        private enum ViewMode
        {
            Monthly,
            CalendarGrid,
            GraduationsOnly,
            Yearly
        }

        private static ViewMode viewMode = ViewMode.Monthly;

        private static GameObject buttonObject;
        private static GameObject popupRoot;
        private static Popup popupComponent;
        private static TextMeshProUGUI yearLabel;
        private static GameObject yearRowObject;
        private static TextMeshProUGUI monthLabel;
        private static GameObject monthRowObject;
        private static GameObject graduationsOnlyButtonObject;
        private static Transform contentRoot;
        private static ScrollRect scrollRect;
        private static TMP_FontAsset defaultFont;
        private static Font defaultLegacyFont;
        private static int selectedYear;
        private static int selectedMonth;
        private static Sprite calendarSprite;

        internal static bool IsInitialized
        {
            get { return initialized; }
        }

        internal static void EnsureBootstrap(PopupManager manager)
        {
            if (manager == null)
            {
                return;
            }
            if (!loggedBootstrap)
            {
                loggedBootstrap = true;
                Log(EnsureBootstrapLogMessage);
            }
            if (manager.GetComponent<GraduationCalendarBootstrap>() == null)
            {
                manager.gameObject.AddComponent<GraduationCalendarBootstrap>();
            }

            GraduationCalendarPopupRecoveryTicker.Ensure(manager);
            TryInitialize();
        }

        internal static void TryInitialize()
        {
            if (initializing)
            {
                return;
            }
            if (Camera.main == null)
            {
                return;
            }
            mainScript main = Camera.main.GetComponent<mainScript>();
            if (main == null || !main.IsGameScene)
            {
                return;
            }
            if (initialized && HasValidUI())
            {
                // Avoid re-running setup every bootstrap tick once UI is healthy.
                return;
            }
            if (initialized && !HasValidUI())
            {
                initialized = false;
                buttonObject = null;
            }
            initializing = true;
            if (buttonObject == null)
            {
                buttonObject = FindExistingMenuButton();
            }
            if (buttonObject != null)
            {
                GameObject awardsButton = FindAwardsButton();
                EnsureMenuButtonSetup(buttonObject, awardsButton);
            }
            if (popupRoot == null)
            {
                CreatePopup();
            }
            if (buttonObject == null)
            {
                GameObject awardsButton = FindAwardsButton();
                if (awardsButton == null)
                {
                    LogFindAwardsDiagnostics(AwardsButtonNotFoundReason);
                    initializing = false;
                    return;
                }
                CreateMenuButton(awardsButton);
            }
            initialized = buttonObject != null && popupRoot != null;
            initializing = false;
        }

        private static bool HasValidUI()
        {
            if (buttonObject == null || popupRoot == null)
            {
                return false;
            }
            if (buttonObject.GetComponent<GraduationCalendarMarker>() == null)
            {
                return false;
            }
            if (buttonObject.transform.parent == null)
            {
                return false;
            }
            return true;
        }

        private static GameObject FindExistingMenuButton()
        {
            GraduationCalendarMarker[] markers = UnityEngine.Object.FindObjectsOfType<GraduationCalendarMarker>();
            foreach (GraduationCalendarMarker marker in markers)
            {
                if (marker != null)
                {
                    return marker.gameObject;
                }
            }
            GameObject named = GameObject.Find(ButtonName);
            if (named != null)
            {
                return named;
            }
            return null;
        }

        private static void EnsureMenuButtonSetup(GameObject button, GameObject awardsButton)
        {
            if (button == null)
            {
                return;
            }
            if (button.GetComponent<GraduationCalendarMarker>() == null)
            {
                button.AddComponent<GraduationCalendarMarker>();
            }
            CaptureDefaultFontFrom(awardsButton != null ? awardsButton : button);
            foreach (Lang_Button lang in button.GetComponentsInChildren<Lang_Button>(true))
            {
                lang.Constant = EmptyString;
                lang.Tooltip = EmptyString;
            }
            Button btn = button.GetComponent<Button>();
            if (btn == null)
            {
                btn = button.GetComponentInChildren<Button>(true);
            }
            if (btn != null)
            {
                btn.onClick = new Button.ButtonClickedEvent();
                btn.onClick.AddListener(Toggle);
            }
            ButtonDefault buttonDefault = button.GetComponent<ButtonDefault>();
            if (buttonDefault != null)
            {
                buttonDefault.DefaultTooltip = ButtonTooltip;
                buttonDefault.SetTooltip(ButtonTooltip);
                buttonDefault.Activate(true, false);
            }
            SetButtonLabel(button, ButtonLabel);
            ApplyCalendarIcon(button);
            if (awardsButton != null)
            {
                EnsureMenuButtonVisible(awardsButton, button);
            }
        }

        internal static void Toggle()
        {
            if (!initialized)
            {
                TryInitialize();
            }
            if (!initialized)
            {
                return;
            }
            if (popupOpen || popupClosing)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        internal static void OnPopupManagerClose(PopupManager manager)
        {
            if (popupOpen && !popupClosing && IsCalendarPopupVisiblyActive())
            {
                Close();
            }
        }

        internal static void RunBackdropRecoveryTick()
        {
            // Backdrop safety logic is intentionally disabled to avoid interfering
            // with base-game popup queue and time flow.
        }

        internal static void CloseIfOpen()
        {
            if (popupOpen || popupClosing)
            {
                Close();
            }
        }

        internal static bool NeedsSafetyNetRestore()
        {
            return false;
        }

        internal static void SafetyNetRestore()
        {
            backdropApplied = false;
        }

        private static void Open()
        {
            if (popupOpen || popupClosing || popupRoot == null)
            {
                return;
            }
            if (ActiveDialogueController.ShowingDialogue || PopupManager.IsThereAnOpenPopup_())
            {
                return;
            }
            selectedYear = staticVars.dateTime.Year;
            selectedMonth = staticVars.dateTime.Month;
            UpdateGraduationsOnlyUI();
            UpdateYearLabel();
            UpdateMonthLabel();
            RenderYear();
            popupRoot.SetActive(true);
            EnsurePopupVisible();
            popupOpen = true;
            popupClosing = false;
            Log(string.Format(LogPopupOpenStateFormat, popupRoot.activeSelf, popupRoot.layer));
        }

        private static void Close()
        {
            if ((!popupOpen && !popupClosing) || popupRoot == null)
            {
                return;
            }
            if (popupClosing)
            {
                return;
            }

            popupClosing = true;
            if (popupComponent != null && popupRoot.activeSelf)
            {
                popupComponent.Hide(HandlePopupHidden);
                return;
            }

            popupRoot.SetActive(false);
            HandlePopupHidden();
        }

        private static void HandlePopupHidden()
        {
            popupOpen = false;
            popupClosing = false;
            if (popupRoot != null && popupRoot.activeSelf)
            {
                popupRoot.SetActive(false);
            }

            backdropApplied = false;
        }

        private static void ApplyBackdrop()
        {
            backdropApplied = false;
        }

        private static void RestoreBackdrop()
        {
            popupOpen = false;
            popupClosing = false;
            backdropApplied = false;
        }

        private static void ApplyCalendarBackdropState(PopupManager manager)
        {
            if (manager == null)
            {
                return;
            }

            if (manager.BG != null)
            {
                manager.BG.SetActive(true);
                CanvasGroup group = manager.BG.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    group.alpha = VisibleAlpha;
                }
            }

            try
            {
                manager.BGBlur(true, BackdropSyncDurationSeconds);
            }
            catch
            {
            }

            manager.BlockInput(true);
            backdropApplied = true;
        }

        internal static bool TrySyncBackdropWithActiveManagedPopups(PopupManager manager)
        {
            if (manager == null)
            {
                return false;
            }

            if (IsCalendarPopupVisibleOrClosing())
            {
                ApplyCalendarBackdropState(manager);
                return true;
            }

            if (manager.popups == null)
            {
                return false;
            }

            bool queueBusy = manager.queue != null && manager.queue.Count > 0;
            bool allowRepair = !queueBusy;
            bool hasActive = false;
            bool requiresBlur = false;
            bool requiresDarken = false;
            RenderTexture activeRenderTexture = null;

            for (int i = 0; i < manager.popups.Length; i++)
            {
                PopupManager._popup entry = manager.popups[i];
                if (entry == null)
                {
                    continue;
                }

                bool activeInHierarchy = entry.obj != null && entry.obj.activeInHierarchy;
                if (allowRepair && entry.open && !activeInHierarchy)
                {
                    entry.open = false;
                }

                if (allowRepair && TryRepairGhostManagedPopupEntry(manager, entry, ref activeInHierarchy, queueBusy))
                {
                    entry.open = false;
                }

                if (!entry.open && !activeInHierarchy)
                {
                    continue;
                }

                hasActive = true;
                if (entry.BGBlur)
                {
                    requiresBlur = true;
                }

                if (entry.BGDarken)
                {
                    requiresDarken = true;
                }

                if (activeRenderTexture == null && entry.BGRenderTexture != null)
                {
                    activeRenderTexture = entry.BGRenderTexture;
                }
            }

            if (!hasActive)
            {
                return false;
            }

            try
            {
                manager.BGBlur(requiresBlur, BackdropSyncDurationSeconds);
            }
            catch
            {
            }

            if (manager.BG != null)
            {
                CanvasGroup bgGroup = manager.BG.GetComponent<CanvasGroup>();
                if (requiresDarken)
                {
                    manager.BG.SetActive(true);
                    if (bgGroup != null)
                    {
                        bgGroup.alpha = VisibleAlpha;
                    }
                }
                else
                {
                    if (bgGroup != null)
                    {
                        bgGroup.alpha = HiddenAlpha;
                    }

                    manager.BG.SetActive(false);
                }
            }

            if (manager.BGImage != null)
            {
                CanvasGroup bgImageGroup = manager.BGImage.GetComponent<CanvasGroup>();
                RawImage raw = manager.BGImage.GetComponent<RawImage>();
                if (activeRenderTexture != null)
                {
                    manager.BGImage.SetActive(true);
                    if (raw != null)
                    {
                        raw.texture = activeRenderTexture;
                    }

                    if (bgImageGroup != null)
                    {
                        bgImageGroup.alpha = VisibleAlpha;
                    }
                }
                else
                {
                    if (bgImageGroup != null)
                    {
                        bgImageGroup.alpha = HiddenAlpha;
                    }

                    manager.BGImage.SetActive(false);
                }
            }

            manager.BlockInput(true);
            backdropApplied = true;
            return true;
        }

        internal static bool TryRunPopupBackdropSafetyNet(PopupManager manager, bool resetPopupCounter)
        {
            if (manager == null)
            {
                return false;
            }

            if (ActiveDialogueController.ShowingDialogue)
            {
                return false;
            }

            if (manager.queue != null && manager.queue.Count > 0)
            {
                return false;
            }

            if (IsCalendarPopupVisibleOrClosing())
            {
                return false;
            }

            if (HasManagedPopupOpenOrActive(manager, true))
            {
                return false;
            }

            if (manager.BGImage != null)
            {
                CanvasGroup bgImageGroup = manager.BGImage.GetComponent<CanvasGroup>();
                if (bgImageGroup != null)
                {
                    bgImageGroup.alpha = HiddenAlpha;
                }

                manager.BGImage.SetActive(false);
            }

            if (manager.BG != null)
            {
                CanvasGroup bgGroup = manager.BG.GetComponent<CanvasGroup>();
                if (bgGroup != null)
                {
                    bgGroup.alpha = HiddenAlpha;
                }

                manager.BG.SetActive(false);
            }

            try
            {
                manager.BGBlur(false, BackdropSyncDurationSeconds);
            }
            catch
            {
            }

            ForceDisableCameraBlur(manager);
            manager.BlockInput(false);

            if (resetPopupCounter && PopupManager.PopupCounter > 0)
            {
                PopupManager.PopupCounter = 0;
            }

            mainScript main = Camera.main != null ? Camera.main.GetComponent<mainScript>() : null;
            if (main != null && ShouldResumeTimeAfterPopupClose())
            {
                main.Time_Resume();
            }

            backdropApplied = false;
            return true;
        }

        private static bool HasManagedPopupOpenOrActive(PopupManager manager, bool repairStaleState)
        {
            if (manager == null || manager.popups == null)
            {
                return false;
            }

            bool queueBusy = manager.queue != null && manager.queue.Count > 0;
            bool allowRepair = repairStaleState && !queueBusy;
            for (int i = 0; i < manager.popups.Length; i++)
            {
                PopupManager._popup entry = manager.popups[i];
                if (entry == null)
                {
                    continue;
                }

                bool activeInHierarchy = entry.obj != null && entry.obj.activeInHierarchy;
                if (allowRepair && entry.open && !activeInHierarchy)
                {
                    entry.open = false;
                }

                if (allowRepair && TryRepairGhostManagedPopupEntry(manager, entry, ref activeInHierarchy, queueBusy))
                {
                    entry.open = false;
                }

                if (entry.open || activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryRepairGhostManagedPopupEntry(
            PopupManager manager,
            PopupManager._popup entry,
            ref bool activeInHierarchy,
            bool queueBusy)
        {
            if (manager == null || entry == null || entry.obj == null || !activeInHierarchy)
            {
                return false;
            }

            Popup popup = entry.obj.GetComponent<Popup>();
            CanvasGroup canvasGroup = entry.obj.GetComponent<CanvasGroup>();
            if (popup == null)
            {
                return false;
            }

            bool hidden = canvasGroup != null && canvasGroup.alpha <= PopupGhostAlphaThreshold;
            bool nonInteractive = canvasGroup != null && (!canvasGroup.blocksRaycasts || !canvasGroup.interactable);
            bool staleClosedEntry = !entry.open && (!queueBusy || nonInteractive);
            if (!hidden && !staleClosedEntry)
            {
                return false;
            }

            if (canvasGroup == null && entry.open && queueBusy)
            {
                return false;
            }

            entry.obj.SetActive(false);
            activeInHierarchy = false;
            if (popup.Increase_Popup_Counter && PopupManager.PopupCounter > 0)
            {
                PopupManager.PopupCounter--;
            }

            return true;
        }

        private static void ForceDisableCameraBlur(PopupManager manager)
        {
            Camera targetCamera = null;
            if (manager != null && manager.MainCamera != null)
            {
                targetCamera = manager.MainCamera.GetComponent<Camera>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                return;
            }

            DisableBlurComponent(targetCamera.gameObject, SuperBlurTypeName);
            DisableBlurComponent(targetCamera.gameObject, SuperBlurFastTypeName);
        }

        private static void DisableBlurComponent(GameObject host, string typeName)
        {
            if (host == null || string.IsNullOrEmpty(typeName))
            {
                return;
            }

            Type blurType = AccessTools.TypeByName(typeName);
            if (blurType == null)
            {
                return;
            }

            Component component = host.GetComponent(blurType);
            if (component == null)
            {
                return;
            }

            Behaviour behaviour = component as Behaviour;
            if (behaviour != null)
            {
                behaviour.enabled = false;
            }

            try
            {
                PropertyInfo interpolationProperty = blurType.GetProperty(InterpolationPropertyName, BindingFlags.Public | BindingFlags.Instance);
                if (interpolationProperty != null && interpolationProperty.CanWrite)
                {
                    interpolationProperty.SetValue(component, DisabledBlurInterpolationValue, null);
                }
            }
            catch
            {
            }
        }

        private static bool TryGetMainAndPopup(out mainScript main, out PopupManager popup)
        {
            main = null;
            popup = null;
            if (Camera.main == null)
            {
                return false;
            }

            main = Camera.main.GetComponent<mainScript>();
            if (main == null || main.Data == null)
            {
                return false;
            }

            popup = main.Data.GetComponent<PopupManager>();
            return popup != null;
        }

        private static PopupManager GetPopupManager()
        {
            mainScript main;
            PopupManager popup;
            if (!TryGetMainAndPopup(out main, out popup))
            {
                return null;
            }

            return popup;
        }

        private static bool IsCalendarPopupVisibleOrClosing()
        {
            if (popupClosing)
            {
                return true;
            }

            return IsCalendarPopupVisiblyActive();
        }

        private static bool IsCalendarPopupVisiblyActive()
        {
            if (popupRoot == null || !popupRoot.activeInHierarchy)
            {
                return false;
            }

            CanvasGroup group = popupRoot.GetComponent<CanvasGroup>();
            if (group == null)
            {
                return true;
            }

            return group.alpha > PopupGhostAlphaThreshold;
        }

        private static bool ShouldResumeTimeAfterPopupClose()
        {
            try
            {
                if (data_girls.new_girls != null && data_girls.new_girls.Count > 0)
                {
                    return false;
                }
                if (Substories_Manager.IntroGirls != null && Substories_Manager.IntroGirls.Count > 0)
                {
                    return false;
                }
            }
            catch
            {
            }
            return true;
        }

        private static void Log(string message)
        {
            Debug.Log(LogPrefix + SpaceSeparator + message);
            try
            {
                string path = GetLogPath();
                if (!string.IsNullOrEmpty(path))
                {
                    File.AppendAllText(
                        path,
                        DateTime.Now.ToString(LogTimeFormat) + SpaceSeparator + LogPrefix + SpaceSeparator + message + Environment.NewLine);
                }
            }
            catch
            {
            }
        }

        private static string GetLogPath()
        {
            if (!string.IsNullOrEmpty(logPath))
            {
                return logPath;
            }
            try
            {
                string dataPath = Application.dataPath;
                if (string.IsNullOrEmpty(dataPath))
                {
                    return EmptyString;
                }
                string root = Directory.GetParent(dataPath).FullName;
                if (string.IsNullOrEmpty(root))
                {
                    return EmptyString;
                }
                logPath = Path.Combine(root, BepInExFolderName, LogFileName);
                return logPath;
            }
            catch
            {
                return EmptyString;
            }
        }

        private static void LogFindAwardsDiagnostics(string reason)
        {
            if (loggedFindAwardsFailure)
            {
                return;
            }
            loggedFindAwardsFailure = true;
            Log(reason + LogDiagnosticsDumpSuffix);
            try
            {
                string awardsLabel = GetAwardsLabel();
                Log(string.Format(LogAwardsLabelResolvedFormat, awardsLabel));

                Lang_Button[] langButtons = UnityEngine.Object.FindObjectsOfType<Lang_Button>();
                Log(string.Format(LogLangButtonCountFormat, langButtons.Length));
                foreach (Lang_Button lang in langButtons)
                {
                    if (lang == null)
                    {
                        continue;
                    }
                    string constant = lang.Constant ?? EmptyString;
                    string tooltip = lang.Tooltip ?? EmptyString;
                    if (constant.Length == 0 && tooltip.Length == 0)
                    {
                        continue;
                    }
                    GameObject go = lang.gameObject;
                    Log(string.Format(
                        LogLangButtonDetailFormat,
                        go.name,
                        constant,
                        tooltip,
                        go.activeInHierarchy,
                        GetHierarchyPath(go)));
                }

                ButtonDefault[] defaults = UnityEngine.Object.FindObjectsOfType<ButtonDefault>();
                Log(string.Format(LogButtonDefaultCountFormat, defaults.Length));
                foreach (ButtonDefault buttonDefault in defaults)
                {
                    if (buttonDefault == null)
                    {
                        continue;
                    }
                    GameObject go = buttonDefault.gameObject;
                    string tooltip = buttonDefault.DefaultTooltip ?? EmptyString;
                    string label = GetButtonLabel(go);
                    if (tooltip.Length == 0 && label.Length == 0 && go.name.IndexOf(AwardsNameKeyword, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                    Log(string.Format(
                        LogButtonDefaultDetailFormat,
                        go.name,
                        tooltip,
                        label,
                        go.activeInHierarchy,
                        GetHierarchyPath(go)));
                }

                Button[] buttons = UnityEngine.Object.FindObjectsOfType<Button>();
                Log(string.Format(LogButtonCountFormat, buttons.Length));
                foreach (Button button in buttons)
                {
                    if (button == null)
                    {
                        continue;
                    }
                    GameObject go = button.gameObject;
                    string label = GetButtonLabel(go);
                    if (label.Length == 0 && go.name.IndexOf(AwardsNameKeyword, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                    Log(string.Format(LogButtonDetailFormat, go.name, label, go.activeInHierarchy, GetHierarchyPath(go)));
                }
            }
            catch (Exception ex)
            {
                Log(string.Format(LogDiagnosticsFailedFormat, ex));
            }
        }

        private static GameObject FindAwardsButton()
        {
            Lang_Button[] buttons = UnityEngine.Object.FindObjectsOfType<Lang_Button>();
            foreach (Lang_Button langButton in buttons)
            {
                if (langButton == null)
                {
                    continue;
                }
                if (!string.Equals(langButton.Constant, AwardsLocalizationDictionaryKey, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(langButton.Tooltip, AwardsLocalizationDictionaryKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                GameObject go = langButton.gameObject;
                if (!go.activeInHierarchy)
                {
                    continue;
                }
                Transform current = go.transform;
                while (current != null)
                {
                    if (current.GetComponent<GraduationCalendarMarker>() != null)
                    {
                        break;
                    }
                    if (current.GetComponent<Button>() != null || current.GetComponent<ButtonDefault>() != null)
                    {
                        return current.gameObject;
                    }
                    current = current.parent;
                }
            }
            return FindAwardsButtonFallback();
        }

        private static GameObject FindAwardsButtonFallback()
        {
            string awardsLabel = GetAwardsLabel();
            ButtonDefault[] buttonDefaults = UnityEngine.Object.FindObjectsOfType<ButtonDefault>();
            foreach (ButtonDefault buttonDefault in buttonDefaults)
            {
                if (buttonDefault == null)
                {
                    continue;
                }
                GameObject go = buttonDefault.gameObject;
                if (go == null || !go.activeInHierarchy)
                {
                    continue;
                }
                if (MatchesAwardsButton(go, awardsLabel, buttonDefault))
                {
                    return go;
                }
            }
            Button[] buttons = UnityEngine.Object.FindObjectsOfType<Button>();
            foreach (Button button in buttons)
            {
                if (button == null)
                {
                    continue;
                }
                GameObject go = button.gameObject;
                if (go == null || !go.activeInHierarchy)
                {
                    continue;
                }
                if (MatchesAwardsButton(go, awardsLabel, null))
                {
                    return go;
                }
            }
            return null;
        }

        private static bool MatchesAwardsButton(GameObject go, string awardsLabel, ButtonDefault buttonDefault)
        {
            if (buttonDefault != null)
            {
                if (!string.IsNullOrEmpty(buttonDefault.DefaultTooltip)
                    && StringEquals(buttonDefault.DefaultTooltip, awardsLabel))
                {
                    return true;
                }
                if (!string.IsNullOrEmpty(buttonDefault.DefaultTooltip)
                    && StringEquals(buttonDefault.DefaultTooltip, AwardsLocalizationDictionaryKey))
                {
                    return true;
                }
            }
            Lang_Button[] langButtons = go.GetComponentsInChildren<Lang_Button>();
            foreach (Lang_Button lang in langButtons)
            {
                if (lang == null)
                {
                    continue;
                }
                if (StringEquals(lang.Constant, AwardsLocalizationDictionaryKey)
                    || StringEquals(lang.Tooltip, AwardsLocalizationDictionaryKey))
                {
                    return true;
                }
            }
            TextMeshProUGUI[] tmpTexts = go.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (TextMeshProUGUI tmp in tmpTexts)
            {
                if (tmp == null)
                {
                    continue;
                }
                if (StringEquals(tmp.text, awardsLabel))
                {
                    return true;
                }
            }
            Text[] texts = go.GetComponentsInChildren<Text>();
            foreach (Text text in texts)
            {
                if (text == null)
                {
                    continue;
                }
                if (StringEquals(text.text, awardsLabel))
                {
                    return true;
                }
            }
            if (!string.IsNullOrEmpty(go.name) && go.name.IndexOf(AwardsNameKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
            return false;
        }

        private static string GetAwardsLabel()
        {
            if (Language.Data != null && Language.Data.ContainsKey(AwardsLocalizationDictionaryKey))
            {
                return Language.Data[AwardsLocalizationDictionaryKey];
            }
            return AwardsFallbackLabel;
        }

        private static bool StringEquals(string left, string right)
        {
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
            {
                return false;
            }
            return string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static void CreateMenuButton(GameObject awardsButton)
        {
            if (awardsButton == null || awardsButton.transform.parent == null)
            {
                Log(CreateMenuButtonMissingParentLogMessage);
                return;
            }
            if (buttonObject != null)
            {
                return;
            }
            GameObject clone = UnityEngine.Object.Instantiate(awardsButton, awardsButton.transform.parent, false);
            clone.name = ButtonName;
            clone.AddComponent<GraduationCalendarMarker>();
            int index = awardsButton.transform.GetSiblingIndex();
            clone.transform.SetSiblingIndex(index);
            CaptureDefaultFontFrom(awardsButton);

            foreach (Lang_Button lang in clone.GetComponentsInChildren<Lang_Button>())
            {
                lang.Constant = EmptyString;
                lang.Tooltip = EmptyString;
            }

            Button button = clone.GetComponent<Button>();
            if (button == null)
            {
                button = clone.GetComponentInChildren<Button>();
            }
            if (button != null)
            {
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(Toggle);
            }

            ButtonDefault buttonDefault = clone.GetComponent<ButtonDefault>();
            if (buttonDefault != null)
            {
                buttonDefault.DefaultTooltip = ButtonTooltip;
                buttonDefault.SetTooltip(ButtonTooltip);
            }

            TextMeshProUGUI tmp = clone.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = ButtonLabel;
            }
            else
            {
                Text text = clone.GetComponentInChildren<Text>();
                if (text != null)
                {
                    text.text = ButtonLabel;
                }
            }
            buttonObject = clone;
            ApplyCalendarIcon(clone);
            EnsureMenuButtonVisible(awardsButton, clone);
            Log(string.Format(LogCreatedMenuButtonFormat, GetHierarchyPath(awardsButton), GetHierarchyPath(clone)));
        }

        private static void CreatePopup()
        {
            const string panelObjectName = "Panel";
            const string titleObjectName = "Title";
            const string yearRowName = "YearRow";
            const string previousYearButtonName = "PrevYear";
            const string yearLabelObjectName = "YearLabel";
            const string nextYearButtonName = "NextYear";
            const string monthRowName = "MonthRow";
            const string previousMonthButtonName = "PrevMonth";
            const string monthLabelObjectName = "MonthLabel";
            const string nextMonthButtonName = "NextMonth";
            const string graduationsOnlyButtonName = "GraduationsOnly";
            const string scrollViewObjectName = "ScrollView";
            const string viewportObjectName = "Viewport";
            const string contentObjectName = "Content";
            const string closeButtonName = "Close";
            const string titleLocalizationKey = "ui.title";
            const string titleFallbackText = "Graduation Calendar";
            const int titleFontSize = 36;
            const float centerAnchor = 0.5f;
            const float edgeAnchor = 1f;
            const float zeroAnchor = 0f;
            const float fallbackPanelWidth = 860f;
            const float fallbackPanelHeight = 520f;
            const float panelVerticalOffset = -6f;
            const float titleWidth = 900f;
            const float titleHeight = 40f;
            const float titleVerticalOffset = -18f;
            const float yearRowWidth = 360f;
            const float rowHeight = 40f;
            const float yearRowVerticalOffset = -56f;
            const float rowButtonSpacing = 10f;
            const float yearLabelWidth = 120f;
            const float monthRowWidth = 460f;
            const float monthRowVerticalOffset = -96f;
            const float monthLabelWidth = 200f;
            const float graduationsOnlyButtonWidth = 190f;
            const float graduationsOnlyButtonHeight = 32f;
            const float graduationsOnlyOffsetX = -20f;
            const float graduationsOnlyOffsetY = -18f;
            const float scrollOffsetLeft = 16f;
            const float scrollOffsetBottom = 56f;
            const float scrollOffsetRight = -32f;
            const float scrollOffsetTop = -92f;
            const float scrollSensitivity = 30f;
            const int contentPaddingLeft = 4;
            const int contentPaddingRight = 4;
            const int contentPaddingTop = 4;
            const int contentPaddingBottom = 4;
            const float contentSpacing = 1f;
            const float closeButtonWidth = 150f;
            const float closeButtonHeight = 36f;
            const float closeButtonOffsetY = 12f;

            Transform parent = GetPopupParent();
            if (parent == null || popupRoot != null)
            {
                return;
            }

            GameObject root = new GameObject(PopupName);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            root.transform.SetParent(parent, false);
            SetLayerRecursively(root, parent.gameObject.layer);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            canvasGroup.alpha = HiddenAlpha;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            root.SetActive(false);

            popupComponent = null;

            GameObject panel = CreateUIObject(panelObjectName, root.transform);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(centerAnchor, centerAnchor);
            panelRect.anchorMax = new Vector2(centerAnchor, centerAnchor);
            panelRect.pivot = new Vector2(centerAnchor, centerAnchor);
            Vector2 panelSize = GetSinglesPanelSize();
            if (panelSize.x > zeroAnchor && panelSize.y > zeroAnchor)
            {
                panelRect.sizeDelta = panelSize;
            }
            else
            {
                panelRect.sizeDelta = new Vector2(fallbackPanelWidth, fallbackPanelHeight);
            }
            panelRect.anchoredPosition = new Vector2(zeroAnchor, panelVerticalOffset);
            Image panelImage = panel.AddComponent<Image>();
            Color32 outerColor;
            Color32 innerColor;
            GetSinglesPanelColors(out outerColor, out innerColor);
            panelImage.color = outerColor;

            TextMeshProUGUI title = CreateText(
                panel.transform,
                titleObjectName,
                ModLocalization.Get(titleLocalizationKey, titleFallbackText),
                titleFontSize,
                TextAlignmentOptions.Center,
                GetPurpleTextColor());
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(centerAnchor, edgeAnchor);
            titleRect.anchorMax = new Vector2(centerAnchor, edgeAnchor);
            titleRect.pivot = new Vector2(centerAnchor, edgeAnchor);
            titleRect.sizeDelta = new Vector2(titleWidth, titleHeight);
            titleRect.anchoredPosition = new Vector2(zeroAnchor, titleVerticalOffset);

            GameObject yearRow = CreateUIObject(yearRowName, panel.transform);
            RectTransform yearRect = yearRow.GetComponent<RectTransform>();
            yearRect.anchorMin = new Vector2(centerAnchor, edgeAnchor);
            yearRect.anchorMax = new Vector2(centerAnchor, edgeAnchor);
            yearRect.pivot = new Vector2(centerAnchor, edgeAnchor);
            yearRect.sizeDelta = new Vector2(yearRowWidth, rowHeight);
            yearRect.anchoredPosition = new Vector2(zeroAnchor, yearRowVerticalOffset);
            HorizontalLayoutGroup yearLayout = yearRow.AddComponent<HorizontalLayoutGroup>();
            yearLayout.childAlignment = TextAnchor.MiddleCenter;
            yearLayout.spacing = rowButtonSpacing;
            yearLayout.childControlHeight = true;
            yearLayout.childControlWidth = true;
            yearLayout.childForceExpandHeight = false;
            yearLayout.childForceExpandWidth = false;

            Button prevButton = CreateArrowButton(yearRow.transform, previousYearButtonName, false, OnPrevYear);
            yearLabel = CreateText(
                yearRow.transform,
                yearLabelObjectName,
                selectedYear.ToString(),
                SelectorFontSize,
                TextAlignmentOptions.Center,
                GetPurpleTextColor());
            RectTransform yearLabelRect = yearLabel.GetComponent<RectTransform>();
            yearLabelRect.sizeDelta = new Vector2(yearLabelWidth, SelectorLabelHeight);
            Button nextButton = CreateArrowButton(yearRow.transform, nextYearButtonName, true, OnNextYear);
            yearRowObject = yearRow;

            GameObject monthRow = CreateUIObject(monthRowName, panel.transform);
            RectTransform monthRect = monthRow.GetComponent<RectTransform>();
            monthRect.anchorMin = new Vector2(centerAnchor, edgeAnchor);
            monthRect.anchorMax = new Vector2(centerAnchor, edgeAnchor);
            monthRect.pivot = new Vector2(centerAnchor, edgeAnchor);
            monthRect.sizeDelta = new Vector2(monthRowWidth, rowHeight);
            monthRect.anchoredPosition = new Vector2(zeroAnchor, monthRowVerticalOffset);
            HorizontalLayoutGroup monthLayout = monthRow.AddComponent<HorizontalLayoutGroup>();
            monthLayout.childAlignment = TextAnchor.MiddleCenter;
            monthLayout.spacing = rowButtonSpacing;
            monthLayout.childControlHeight = true;
            monthLayout.childControlWidth = true;
            monthLayout.childForceExpandHeight = false;
            monthLayout.childForceExpandWidth = false;

            Button prevMonthButton = CreateArrowButton(monthRow.transform, previousMonthButtonName, false, OnPrevMonth);
            monthLabel = CreateText(
                monthRow.transform,
                monthLabelObjectName,
                EmptyString,
                SelectorFontSize,
                TextAlignmentOptions.Center,
                GetPurpleTextColor());
            RectTransform monthLabelRect = monthLabel.GetComponent<RectTransform>();
            monthLabelRect.sizeDelta = new Vector2(monthLabelWidth, SelectorLabelHeight);
            Button nextMonthButton = CreateArrowButton(monthRow.transform, nextMonthButtonName, true, OnNextMonth);
            monthRowObject = monthRow;

            Button gradOnlyButton = CreateButtonFromTemplate(
                panel.transform,
                graduationsOnlyButtonName,
                GetGraduationsOnlyLabel(),
                graduationsOnlyButtonWidth,
                graduationsOnlyButtonHeight,
                ToggleGraduationsOnly,
                GetSinglesPrimaryButtonTemplate(),
                false);
            RectTransform gradRect = gradOnlyButton.GetComponent<RectTransform>();
            gradRect.anchorMin = new Vector2(edgeAnchor, edgeAnchor);
            gradRect.anchorMax = new Vector2(edgeAnchor, edgeAnchor);
            gradRect.pivot = new Vector2(edgeAnchor, edgeAnchor);
            gradRect.anchoredPosition = new Vector2(graduationsOnlyOffsetX, graduationsOnlyOffsetY);
            graduationsOnlyButtonObject = gradOnlyButton.gameObject;
            SetButtonBackgroundColor(graduationsOnlyButtonObject, GetButtonBackgroundColor());

            GameObject scrollView = CreateUIObject(scrollViewObjectName, panel.transform);
            RectTransform scrollRectTransform = scrollView.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(zeroAnchor, zeroAnchor);
            scrollRectTransform.anchorMax = new Vector2(edgeAnchor, edgeAnchor);
            scrollRectTransform.offsetMin = new Vector2(scrollOffsetLeft, scrollOffsetBottom);
            scrollRectTransform.offsetMax = new Vector2(scrollOffsetRight, scrollOffsetTop);
            Image scrollImage = scrollView.AddComponent<Image>();
            scrollImage.color = innerColor;
            scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = scrollSensitivity;

            GameObject viewport = CreateUIObject(viewportObjectName, scrollView.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = innerColor;
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject content = CreateUIObject(contentObjectName, viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(zeroAnchor, edgeAnchor);
            contentRect.anchorMax = new Vector2(edgeAnchor, edgeAnchor);
            contentRect.pivot = new Vector2(centerAnchor, edgeAnchor);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(zeroAnchor, zeroAnchor);

            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.padding = new RectOffset(contentPaddingLeft, contentPaddingRight, contentPaddingTop, contentPaddingBottom);
            contentLayout.spacing = contentSpacing;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            CreateScrollbar(scrollView.transform, scrollRect);

            contentRoot = content.transform;
            popupRoot = root;

            Button closeButton = CreateButtonFromTemplate(
                panel.transform,
                closeButtonName,
                GetCloseLabel(),
                closeButtonWidth,
                closeButtonHeight,
                Close,
                GetSinglesPrimaryButtonTemplate(),
                false);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(centerAnchor, zeroAnchor);
            closeRect.anchorMax = new Vector2(centerAnchor, zeroAnchor);
            closeRect.pivot = new Vector2(centerAnchor, zeroAnchor);
            closeRect.anchoredPosition = new Vector2(zeroAnchor, closeButtonOffsetY);
            SetButtonBackgroundColor(closeButton != null ? closeButton.gameObject : null, GetButtonBackgroundColor());
        }

        private static Transform GetPopupParent()
        {
            PopupManager manager = GetPopupManager();
            if (manager != null && manager.popups != null)
            {
                GameObject awardsPopup = PopupManager.GetObject(PopupManager._type.awards);
                if (awardsPopup != null && awardsPopup.transform.parent != null)
                {
                    return awardsPopup.transform.parent;
                }
                foreach (PopupManager._popup popup in manager.popups)
                {
                    if (popup != null && popup.obj != null && popup.obj.transform.parent != null)
                    {
                        return popup.obj.transform.parent;
                    }
                }
            }
            return null;
        }

        private static GameObject GetSinglesPrimaryButtonTemplate()
        {
            try
            {
                GameObject popup = PopupManager.GetObject(PopupManager._type.single_new);
                if (popup == null)
                {
                    popup = PopupManager.GetObject(PopupManager._type.single_senbatsu);
                }
                if (popup == null)
                {
                    return null;
                }
                Single_Popup singlePopup = popup.GetComponent<Single_Popup>();
                if (singlePopup != null)
                {
                    if (singlePopup.Button_Continue != null)
                    {
                        return singlePopup.Button_Continue;
                    }
                    if (singlePopup.prefab_button != null)
                    {
                        return singlePopup.prefab_button;
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        private static void GetSinglesPanelColors(out Color32 outerColor, out Color32 innerColor)
        {
            const byte outerRed = 235;
            const byte outerGreen = 234;
            const byte outerBlue = 233;
            const byte outerAlpha = 255;
            const byte innerRed = 254;
            const byte innerGreen = 254;
            const byte innerBlue = 254;
            const byte innerAlpha = 255;

            outerColor = new Color32(outerRed, outerGreen, outerBlue, outerAlpha);
            innerColor = new Color32(innerRed, innerGreen, innerBlue, innerAlpha);
            try
            {
                Image panelImage = FindSinglesPanelImage();
                Image innerImage = FindSinglesInnerImage();
                if (panelImage != null)
                {
                    outerColor = panelImage.color;
                }
                if (innerImage != null)
                {
                    innerColor = innerImage.color;
                }
                if (GetColorLuma(outerColor) > GetColorLuma(innerColor))
                {
                    Color32 swap = outerColor;
                    outerColor = innerColor;
                    innerColor = swap;
                }
            }
            catch
            {
            }
        }

        private static float GetColorLuma(Color32 color)
        {
            const float redLumaWeight = 0.2126f;
            const float greenLumaWeight = 0.7152f;
            const float blueLumaWeight = 0.0722f;
            return color.r * redLumaWeight + color.g * greenLumaWeight + color.b * blueLumaWeight;
        }

        private static Vector2 GetSinglesPanelSize()
        {
            try
            {
                Image panelImage = FindSinglesPanelImage();
                if (panelImage != null)
                {
                    RectTransform rect = panelImage.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        return rect.rect.size;
                    }
                }
            }
            catch
            {
            }
            return Vector2.zero;
        }

        private static Image FindSinglesPanelImage()
        {
            GameObject popup = PopupManager.GetObject(PopupManager._type.single_senbatsu);
            if (popup == null)
            {
                popup = PopupManager.GetObject(PopupManager._type.single_new);
            }
            if (popup == null)
            {
                return null;
            }
            return FindPanelImage(popup.transform);
        }

        private static Image FindSinglesInnerImage()
        {
            GameObject popup = PopupManager.GetObject(PopupManager._type.single_senbatsu);
            if (popup == null)
            {
                popup = PopupManager.GetObject(PopupManager._type.single_new);
            }
            if (popup == null)
            {
                return null;
            }
            return FindSecondLargestImage(popup.transform);
        }

        private static Image FindPanelImage(Transform root)
        {
            if (root == null)
            {
                return null;
            }
            const float maxPanelScreenCoverage = 0.95f;
            const float minimumPanelDimension = 0f;
            const float minimumArea = 0f;
            float maxW = Screen.width * maxPanelScreenCoverage;
            float maxH = Screen.height * maxPanelScreenCoverage;
            Image best = null;
            float bestArea = minimumArea;
            List<Image> images = GetComponentsInChildrenAll<Image>(root);
            foreach (Image image in images)
            {
                if (image == null)
                {
                    continue;
                }
                RectTransform rect = image.GetComponent<RectTransform>();
                if (rect == null)
                {
                    continue;
                }
                float width = rect.rect.width;
                float height = rect.rect.height;
                if (width <= minimumPanelDimension || height <= minimumPanelDimension)
                {
                    continue;
                }
                if (width > maxW || height > maxH)
                {
                    continue;
                }
                float area = width * height;
                if (area > bestArea)
                {
                    bestArea = area;
                    best = image;
                }
            }
            if (best != null)
            {
                return best;
            }
            return FindLargestImage(root);
        }

        private static Image FindLargestImage(Transform root)
        {
            if (root == null)
            {
                return null;
            }
            const float minimumArea = 0f;
            Image best = null;
            float bestArea = minimumArea;
            List<Image> images = GetComponentsInChildrenAll<Image>(root);
            foreach (Image image in images)
            {
                if (image == null)
                {
                    continue;
                }
                RectTransform rect = image.GetComponent<RectTransform>();
                if (rect == null)
                {
                    continue;
                }
                float area = rect.rect.width * rect.rect.height;
                if (area <= bestArea)
                {
                    continue;
                }
                bestArea = area;
                best = image;
            }
            return best;
        }

        private static Image FindSecondLargestImage(Transform root)
        {
            if (root == null)
            {
                return null;
            }
            const float minimumArea = 0f;
            Image largest = null;
            Image second = null;
            float largestArea = minimumArea;
            float secondArea = minimumArea;
            List<Image> images = GetComponentsInChildrenAll<Image>(root);
            foreach (Image image in images)
            {
                if (image == null)
                {
                    continue;
                }
                RectTransform rect = image.GetComponent<RectTransform>();
                if (rect == null)
                {
                    continue;
                }
                float area = rect.rect.width * rect.rect.height;
                if (area <= minimumArea)
                {
                    continue;
                }
                if (area > largestArea)
                {
                    second = largest;
                    secondArea = largestArea;
                    largest = image;
                    largestArea = area;
                }
                else if (area > secondArea)
                {
                    second = image;
                    secondArea = area;
                }
            }
            return second != null ? second : largest;
        }

        private static Image FindSmallestSpriteImage(Transform root)
        {
            if (root == null)
            {
                return null;
            }
            const float minimumArea = 0f;
            Image smallest = null;
            float smallestArea = float.MaxValue;
            List<Image> images = GetComponentsInChildrenAll<Image>(root);
            foreach (Image image in images)
            {
                if (image == null || image.sprite == null)
                {
                    continue;
                }
                RectTransform rect = image.GetComponent<RectTransform>();
                if (rect == null)
                {
                    continue;
                }
                float area = rect.rect.width * rect.rect.height;
                if (area <= minimumArea)
                {
                    continue;
                }
                if (area < smallestArea)
                {
                    smallest = image;
                    smallestArea = area;
                }
            }
            return smallest;
        }

        private static GameObject GetSinglesArrowButtonTemplate(bool next)
        {
            const float noScore = -1f;
            const float zeroScore = 0f;
            const float arrowSpriteScoreBonus = 2f;
            const float directionNameScoreBonus = 1.5f;
            const float roundSpriteScoreBonus = 3f;
            const float maxAspectScore = 3f;
            const float aspectNormalizationDivisor = 5f;
            const float compactArrowButtonMaxSize = 40f;
            const float minimumArrowButtonSize = 0f;

            GameObject popup = PopupManager.GetObject(PopupManager._type.single_senbatsu);
            if (popup == null)
            {
                popup = PopupManager.GetObject(PopupManager._type.single_release);
            }
            if (popup == null)
            {
                popup = PopupManager.GetObject(PopupManager._type.single_new);
            }
            if (popup == null)
            {
                popup = PopupManager.GetObject(PopupManager._type.single_chart);
            }
            if (popup == null)
            {
                return GetChartButtonTemplate(next);
            }
            GameObject best = null;
            float bestScore = noScore;
            List<Button> buttons = GetComponentsInChildrenAll<Button>(popup.transform);
            foreach (Button button in buttons)
            {
                if (button == null)
                {
                    continue;
                }
                GameObject go = button.gameObject;
                RectTransform rect = go.GetComponent<RectTransform>();
                if (rect == null)
                {
                    continue;
                }
                float width = rect.rect.width;
                float height = rect.rect.height;
                if (width <= minimumArrowButtonSize || height <= minimumArrowButtonSize)
                {
                    continue;
                }
                bool arrowSprite = HasArrowSprite(go.transform, next);
                string nameLower = go.name != null ? go.name.ToLowerInvariant() : EmptyString;
                bool nameMatches = next
                    ? (nameLower.Contains(NextKeyword) || nameLower.Contains(RightKeyword))
                    : (nameLower.Contains(PreviousKeyword) || nameLower.Contains(LeftKeyword));
                if (!arrowSprite)
                {
                    continue;
                }
                float score = zeroScore;
                if (arrowSprite)
                {
                    score += arrowSpriteScoreBonus;
                }
                if (nameMatches)
                {
                    score += directionNameScoreBonus;
                }
                float aspect = Mathf.Abs(width - height);
                score += Mathf.Clamp(maxAspectScore - aspect / aspectNormalizationDivisor, zeroScore, maxAspectScore);
                if (width <= compactArrowButtonMaxSize && height <= compactArrowButtonMaxSize)
                {
                    score += arrowSpriteScoreBonus;
                }
                string spriteName = GetButtonSpriteName(button);
                if (!string.IsNullOrEmpty(spriteName)
                    && (spriteName.Contains(RoundKeyword) || spriteName.Contains(CircleKeyword)))
                {
                    score += roundSpriteScoreBonus;
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    best = go;
                }
            }
            if (best != null)
            {
                return best;
            }
            return GetChartButtonTemplate(next);
        }

        private static string GetButtonSpriteName(Button button)
        {
            if (button == null)
            {
                return EmptyString;
            }
            Image target = button.targetGraphic as Image;
            if (target != null && target.sprite != null && !string.IsNullOrEmpty(target.sprite.name))
            {
                return target.sprite.name.ToLowerInvariant();
            }
            List<Image> images = GetComponentsInChildrenAll<Image>(button.transform);
            foreach (Image image in images)
            {
                if (image == null || image.sprite == null)
                {
                    continue;
                }
                string name = image.sprite.name;
                if (!string.IsNullOrEmpty(name))
                {
                    return name.ToLowerInvariant();
                }
            }
            return EmptyString;
        }

        private static Color32 GetPurpleTextColor()
        {
            return mainScript.lightBlue32;
        }

        private static Color32 GetMutedTextColor()
        {
            return mainScript.grey_light32;
        }

        private static Color32 GetButtonTextColor()
        {
            return mainScript.white32;
        }

        private static Color32 GetButtonBackgroundColor()
        {
            return mainScript.blue32;
        }

        private static GameObject GetChartButtonTemplate(bool next)
        {
            Chart_Popup chart = GetChartPopup();
            if (chart == null)
            {
                return null;
            }
            return next ? chart.Button_NextMonth : chart.Button_PrevMonth;
        }

        private static Chart_Popup GetChartPopup()
        {
            try
            {
                GameObject popup = PopupManager.GetObject(PopupManager._type.single_chart);
                if (popup == null)
                {
                    PopupManager manager = GetPopupManager();
                    if (manager != null && manager.popups != null)
                    {
                        foreach (PopupManager._popup entry in manager.popups)
                        {
                            if (entry != null && entry.type == PopupManager._type.single_chart && entry.obj != null)
                            {
                                popup = entry.obj;
                                break;
                            }
                        }
                    }
                    if (popup == null)
                    {
                        return null;
                    }
                }
                return popup.GetComponent<Chart_Popup>();
            }
            catch
            {
                return null;
            }
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

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize, TextAlignmentOptions alignment, Color32 color)
        {
            EnsureDefaultFont();
            GameObject obj = CreateUIObject(name, parent);
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            if (defaultFont != null)
            {
                tmp.font = defaultFont;
            }
            return tmp;
        }

        private static Text CreateLegacyText(Transform parent, string name, string text, int fontSize, TextAnchor alignment, Color32 color)
        {
            EnsureDefaultLegacyFont();
            GameObject obj = CreateUIObject(name, parent);
            Text uiText = obj.AddComponent<Text>();
            uiText.text = text;
            uiText.fontSize = fontSize;
            uiText.alignment = alignment;
            uiText.color = color;
            uiText.raycastTarget = false;
            uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            if (defaultLegacyFont != null)
            {
                uiText.font = defaultLegacyFont;
            }
            return uiText;
        }

        private static void CreateScrollbar(Transform parent, ScrollRect target)
        {
            const float scrollbarEdgeAnchor = 1f;
            const float scrollbarBaseAnchor = 0f;
            const float scrollbarWidth = 12f;
            const float scrollbarOffsetX = -4f;
            const float scrollbarSpacing = 6f;

            if (parent == null || target == null)
            {
                return;
            }
            Scrollbar template = GetScrollbarTemplate();
            GameObject scrollbarObj;
            Scrollbar scrollbar;
            if (template != null)
            {
                scrollbarObj = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
                scrollbarObj.name = ScrollbarObjectName;
                SetLayerRecursively(scrollbarObj, parent.gameObject.layer);
                scrollbarObj.SetActive(true);
                scrollbar = scrollbarObj.GetComponent<Scrollbar>();
                if (scrollbar == null)
                {
                    scrollbar = scrollbarObj.AddComponent<Scrollbar>();
                }
                CanvasGroup group = scrollbarObj.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    group.alpha = VisibleAlpha;
                    group.interactable = true;
                    group.blocksRaycasts = true;
                }
            }
            else
            {
                scrollbarObj = CreateUIObject(ScrollbarObjectName, parent);
                Image trackImage = scrollbarObj.AddComponent<Image>();
                trackImage.color = GetScrollbarTrackColor();
                trackImage.raycastTarget = true;
                scrollbar = scrollbarObj.AddComponent<Scrollbar>();

                GameObject handleObj = CreateUIObject(ScrollbarHandleObjectName, scrollbarObj.transform);
                RectTransform handleRect = handleObj.GetComponent<RectTransform>();
                handleRect.anchorMin = Vector2.zero;
                handleRect.anchorMax = Vector2.one;
                handleRect.offsetMin = Vector2.zero;
                handleRect.offsetMax = Vector2.zero;
                Image handleImage = handleObj.AddComponent<Image>();
                handleImage.color = GetButtonBackgroundColor();

                Sprite handleSprite = GetScrollbarHandleSprite();
                if (handleSprite != null)
                {
                    handleImage.sprite = handleSprite;
                    handleImage.type = Image.Type.Sliced;
                }

                scrollbar.targetGraphic = handleImage;
                scrollbar.handleRect = handleRect;
            }

            RectTransform scrollbarRect = scrollbarObj.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(scrollbarEdgeAnchor, scrollbarBaseAnchor);
            scrollbarRect.anchorMax = new Vector2(scrollbarEdgeAnchor, scrollbarEdgeAnchor);
            scrollbarRect.pivot = new Vector2(scrollbarEdgeAnchor, scrollbarEdgeAnchor);
            scrollbarRect.sizeDelta = new Vector2(scrollbarWidth, scrollbarBaseAnchor);
            scrollbarRect.anchoredPosition = new Vector2(scrollbarOffsetX, scrollbarBaseAnchor);

            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            target.verticalScrollbar = scrollbar;
            target.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            target.verticalScrollbarSpacing = scrollbarSpacing;
        }

        private static Color32 GetScrollbarTrackColor()
        {
            const byte scrollbarTrackAlpha = 180;
            Color32 baseColor = mainScript.grey_light32;
            baseColor.a = scrollbarTrackAlpha;
            return baseColor;
        }

        private static Sprite GetScrollbarHandleSprite()
        {
            try
            {
                Scrollbar template = GetScrollbarTemplate();
                if (template != null)
                {
                    if (template.handleRect != null)
                    {
                        Image img = template.handleRect.GetComponent<Image>();
                        if (img != null && img.sprite != null)
                        {
                            return img.sprite;
                        }
                    }
                    Image target = template.targetGraphic as Image;
                    if (target != null && target.sprite != null)
                    {
                        return target.sprite;
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        private static Scrollbar GetScrollbarTemplate()
        {
            const int firstTemplateIndex = 0;
            try
            {
                Scrollbar preferred = FindScrollbarInPopup(PopupManager._type.producer_salaries);
                if (preferred != null)
                {
                    return preferred;
                }
                preferred = FindScrollbarInPopup(PopupManager._type.producer_contracts);
                if (preferred != null)
                {
                    return preferred;
                }
                preferred = FindScrollbarInPopup(PopupManager._type.producer_loans);
                if (preferred != null)
                {
                    return preferred;
                }
                preferred = FindScrollbarInPopup(PopupManager._type.notifications);
                if (preferred != null)
                {
                    return preferred;
                }
                preferred = FindScrollbarInPopup(PopupManager._type.awards);
                if (preferred != null)
                {
                    return preferred;
                }
                preferred = FindScrollbarInPopup(PopupManager._type.single_release);
                if (preferred != null)
                {
                    return preferred;
                }
                preferred = FindScrollbarInPopup(PopupManager._type.single_senbatsu);
                if (preferred != null)
                {
                    return preferred;
                }
                preferred = FindScrollbarInPopup(PopupManager._type.single_chart);
                if (preferred != null)
                {
                    return preferred;
                }
                preferred = FindScrollbarInPopup(PopupManager._type.SNS);
                if (preferred != null)
                {
                    return preferred;
                }
                Scrollbar[] all = UnityEngine.Object.FindObjectsOfType<Scrollbar>();
                if (all.Length > 0)
                {
                    return all[firstTemplateIndex];
                }
            }
            catch
            {
            }
            return null;
        }

        private static Scrollbar FindScrollbarInPopup(PopupManager._type type)
        {
            const int firstScrollbarIndex = 0;
            GameObject popup = PopupManager.GetObject(type);
            if (popup == null)
            {
                PopupManager manager = GetPopupManager();
                if (manager != null && manager.popups != null)
                {
                    foreach (PopupManager._popup entry in manager.popups)
                    {
                        if (entry != null && entry.type == type && entry.obj != null)
                        {
                            popup = entry.obj;
                            break;
                        }
                    }
                }
            }
            if (popup == null)
            {
                return null;
            }
            List<Scrollbar> scrollbars = GetComponentsInChildrenAll<Scrollbar>(popup.transform);
            if (scrollbars.Count > 0)
            {
                return scrollbars[firstScrollbarIndex];
            }
            return null;
        }

        private static Button CreateButton(Transform parent, string name, string label, float width, float height, Action onClick)
        {
            const int buttonFontSize = 20;

            GameObject obj = CreateUIObject(name, parent);
            Image image = obj.AddComponent<Image>();
            image.color = GetButtonBackgroundColor();
            Button button = obj.AddComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null)
            {
                button.onClick.AddListener(delegate
                {
                    onClick();
                });
            }
            ButtonDefault buttonDefault = obj.AddComponent<ButtonDefault>();
            TextMeshProUGUI text = CreateText(
                obj.transform,
                TextObjectName,
                label,
                buttonFontSize,
                TextAlignmentOptions.Center,
                GetButtonTextColor());
            text.enableWordWrapping = false;
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            buttonDefault.Text = text.gameObject;
            LayoutElement layout = obj.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            return button;
        }

        private static Button CreateButtonFromTemplate(Transform parent, string name, string label, float width, float height, Action onClick, GameObject template, bool preserveLangButtons = false, bool keepTemplateSize = false)
        {
            if (template == null)
            {
                return CreateButton(parent, name, label ?? EmptyString, width, height, onClick);
            }
            GameObject obj = UnityEngine.Object.Instantiate(template, parent, false);
            obj.name = name;
            SetLayerRecursively(obj, parent.gameObject.layer);
            obj.SetActive(true);
            if (!preserveLangButtons)
            {
                ClearLangButtons(obj);
            }
            List<Button> buttons = GetComponentsInChildrenAll<Button>(obj.transform);
            foreach (Button existing in buttons)
            {
                if (existing != null)
                {
                    existing.onClick = new Button.ButtonClickedEvent();
                }
            }
            Button button = buttons.Count > 0 ? buttons[0] : null;
            if (button == null)
            {
                button = obj.AddComponent<Button>();
            }
            if (button.targetGraphic == null)
            {
                Graphic graphic = obj.GetComponent<Graphic>();
                if (graphic != null)
                {
                    button.targetGraphic = graphic;
                }
            }
            if (onClick != null)
            {
                button.onClick.AddListener(delegate
                {
                    onClick();
                });
            }
            ButtonDefault buttonDefault = obj.GetComponent<ButtonDefault>();
            if (buttonDefault != null)
            {
                buttonDefault.DefaultTooltip = EmptyString;
                buttonDefault.SetTooltip(EmptyString);
                buttonDefault.Activate(true, false);
            }
            if (!string.IsNullOrEmpty(label) && !preserveLangButtons)
            {
                SetButtonLabel(obj, label);
            }
            if (!keepTemplateSize)
            {
                SetButtonSize(obj, width, height);
            }
            CaptureDefaultFontFrom(obj);
            return button;
        }

        private static Button CreateArrowButton(Transform parent, string name, bool next, Action onClick)
        {
            GameObject template = GetChartButtonTemplate(next);
            if (template == null)
            {
                template = GetSinglesArrowButtonTemplate(next);
            }
            if (template == null)
            {
                string label = next ? ArrowNextLabel : ArrowPreviousLabel;
                return CreateButton(parent, name, label, ArrowButtonWidth, ArrowButtonHeight, onClick);
            }
            const float keepTemplateSizeValue = 0f;
            Button button = CreateButtonFromTemplate(
                parent,
                name,
                null,
                keepTemplateSizeValue,
                keepTemplateSizeValue,
                onClick,
                template,
                true,
                true);
            ApplyArrowButtonStyle(button != null ? button.gameObject : null, next);
            EnsureArrowButtonSize(button);
            return button;
        }

        private static void EnsureArrowButtonSize(Button button)
        {
            if (button == null)
            {
                return;
            }
            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }
            rect.sizeDelta = new Vector2(ArrowButtonWidth, ArrowButtonHeight);
            LayoutElement layout = button.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = button.gameObject.AddComponent<LayoutElement>();
            }
            layout.preferredWidth = ArrowButtonWidth;
            layout.preferredHeight = ArrowButtonHeight;
        }

        private static void SetButtonSize(GameObject obj, float width, float height)
        {
            if (obj == null)
            {
                return;
            }
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(width, height);
            }
            LayoutElement layout = obj.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = obj.AddComponent<LayoutElement>();
            }
            layout.preferredWidth = width;
            layout.preferredHeight = height;
        }

        private static void SetButtonLabel(GameObject buttonObject, string label)
        {
            if (buttonObject == null)
            {
                return;
            }
            EnsureDefaultFont();
            bool set = false;
            List<TextMeshProUGUI> tmps = GetComponentsInChildrenAll<TextMeshProUGUI>(buttonObject.transform);
            foreach (TextMeshProUGUI tmp in tmps)
            {
                if (tmp == null)
                {
                    continue;
                }
                tmp.text = label;
                tmp.enableWordWrapping = false;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = GetButtonTextColor();
                if (defaultFont != null && tmp.font != defaultFont)
                {
                    tmp.font = defaultFont;
                }
                set = true;
            }
            List<Text> texts = GetComponentsInChildrenAll<Text>(buttonObject.transform);
            foreach (Text text in texts)
            {
                if (text == null)
                {
                    continue;
                }
                text.text = label;
                text.color = GetButtonTextColor();
                set = true;
            }
            if (!set)
            {
                const int buttonFontSize = 20;
                TextMeshProUGUI tmp = CreateText(
                    buttonObject.transform,
                    TextObjectName,
                    label,
                    buttonFontSize,
                    TextAlignmentOptions.Center,
                    GetButtonTextColor());
                RectTransform rect = tmp.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                ButtonDefault buttonDefault = buttonObject.GetComponent<ButtonDefault>();
                if (buttonDefault != null)
                {
                    buttonDefault.Text = tmp.gameObject;
                }
            }
        }

        private static void ClearLangButtons(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }
            List<Lang_Button> langs = GetComponentsInChildrenAll<Lang_Button>(obj.transform);
            foreach (Lang_Button lang in langs)
            {
                if (lang == null)
                {
                    continue;
                }
                lang.Constant = EmptyString;
                lang.Tooltip = EmptyString;
            }
        }

        private static T GetFirstComponentInChildren<T>(GameObject root) where T : Component
        {
            const int firstComponentIndex = 0;
            if (root == null)
            {
                return null;
            }
            List<T> list = GetComponentsInChildrenAll<T>(root.transform);
            if (list.Count > 0)
            {
                return list[firstComponentIndex];
            }
            return null;
        }

        private static List<T> GetComponentsInChildrenAll<T>(Transform root) where T : Component
        {
            List<T> results = new List<T>();
            if (root == null)
            {
                return results;
            }
            Stack<Transform> stack = new Stack<Transform>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                Transform current = stack.Pop();
                if (current == null)
                {
                    continue;
                }
                T component = current.GetComponent<T>();
                if (component != null)
                {
                    results.Add(component);
                }
                for (int i = 0; i < current.childCount; i++)
                {
                    stack.Push(current.GetChild(i));
                }
            }
            return results;
        }

        private static void OnPrevYear()
        {
            selectedYear--;
            UpdateYearLabel();
            UpdateMonthLabel();
            RenderYear();
        }

        private static void OnNextYear()
        {
            selectedYear++;
            UpdateYearLabel();
            UpdateMonthLabel();
            RenderYear();
        }

        private static void OnPrevMonth()
        {
            selectedMonth--;
            if (selectedMonth < FirstMonthOfYear)
            {
                selectedMonth = MonthsInYear;
                selectedYear--;
            }
            UpdateMonthLabel();
            UpdateYearLabel();
            RenderYear();
        }

        private static void OnNextMonth()
        {
            selectedMonth++;
            if (selectedMonth > MonthsInYear)
            {
                selectedMonth = FirstMonthOfYear;
                selectedYear++;
            }
            UpdateMonthLabel();
            UpdateYearLabel();
            RenderYear();
        }

        private static void UpdateYearLabel()
        {
            if (yearLabel != null)
            {
                yearLabel.text = selectedYear.ToString();
            }
        }

        private static void UpdateMonthLabel()
        {
            if (monthLabel != null)
            {
                DateTime date = new DateTime(selectedYear, Mathf.Clamp(selectedMonth, FirstMonthOfYear, MonthsInYear), FirstMonthOfYear);
                monthLabel.text = GetMonthNameOnly(date);
            }
        }

        private static void ToggleGraduationsOnly()
        {
            switch (viewMode)
            {
                case ViewMode.Monthly:
                    viewMode = ViewMode.CalendarGrid;
                    break;
                case ViewMode.CalendarGrid:
                    viewMode = ViewMode.GraduationsOnly;
                    break;
                case ViewMode.GraduationsOnly:
                    viewMode = ViewMode.Yearly;
                    break;
                default:
                    viewMode = ViewMode.Monthly;
                    break;
            }
            UpdateGraduationsOnlyUI();
            RenderYear();
        }

        private static void UpdateGraduationsOnlyUI()
        {
            if (yearRowObject != null)
            {
                yearRowObject.SetActive(viewMode == ViewMode.Monthly || viewMode == ViewMode.CalendarGrid);
            }
            if (monthRowObject != null)
            {
                monthRowObject.SetActive(viewMode == ViewMode.CalendarGrid);
            }
            if (graduationsOnlyButtonObject != null)
            {
                SetButtonLabel(graduationsOnlyButtonObject, GetGraduationsOnlyLabel());
            }
            UpdateMonthLabel();
            UpdateHeaderLayout();
        }

        private static string GetGraduationsOnlyLabel()
        {
            switch (viewMode)
            {
                case ViewMode.Monthly:
                    return ModLocalization.Get(ViewCalendarLocalizationKey, ViewCalendarFallbackLabel);
                case ViewMode.CalendarGrid:
                    return ModLocalization.Get(ViewGraduationsOnlyLocalizationKey, ViewGraduationsOnlyFallbackLabel);
                case ViewMode.GraduationsOnly:
                    return ModLocalization.Get(ViewYearlyLocalizationKey, ViewYearlyFallbackLabel);
                default:
                    return ModLocalization.Get(ViewMonthlyLocalizationKey, ViewMonthlyFallbackLabel);
            }
        }

        private static void UpdateHeaderLayout()
        {
            const float headerCenterX = 0f;
            const float yearlySelectorY = -56f;
            const float monthlySelectorY = -96f;
            const float calendarGridScrollTopOffset = -140f;
            const float listViewScrollTopOffset = -92f;

            if (yearRowObject != null)
            {
                RectTransform rect = yearRowObject.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(headerCenterX, yearlySelectorY);
                }
            }
            if (monthRowObject != null)
            {
                RectTransform rect = monthRowObject.GetComponent<RectTransform>();
                if (rect != null)
                {
                    float y = viewMode == ViewMode.CalendarGrid ? monthlySelectorY : yearlySelectorY;
                    rect.anchoredPosition = new Vector2(headerCenterX, y);
                }
            }
            if (scrollRect != null)
            {
                RectTransform rect = scrollRect.GetComponent<RectTransform>();
                if (rect != null)
                {
                    float top = viewMode == ViewMode.CalendarGrid ? calendarGridScrollTopOffset : listViewScrollTopOffset;
                    rect.offsetMax = new Vector2(rect.offsetMax.x, top);
                }
            }
        }

        private sealed class GirlEntry
        {
            public data_girls.girls Girl;
            public DateTime Date;
            public string Name;
        }

        private static void RenderYear()
        {
            if (contentRoot == null)
            {
                return;
            }
            if (viewMode == ViewMode.CalendarGrid)
            {
                RenderCalendarGrid();
                return;
            }
            if (viewMode == ViewMode.GraduationsOnly)
            {
                RenderGraduationsOnly();
                return;
            }
            if (viewMode == ViewMode.Yearly)
            {
                RenderYearly();
                return;
            }
            ExtensionMethods.destroyChildren(contentRoot);
            List<GirlEntry>[] months = new List<GirlEntry>[MonthsInYear];
            for (int i = 0; i < months.Length; i++)
            {
                months[i] = new List<GirlEntry>();
            }
            List<GirlEntry> unknown = new List<GirlEntry>();
            int totalGirls = 0;
            if (data_girls.girl != null)
            {
                foreach (data_girls.girls girl in data_girls.girl)
                {
                    if (girl == null || girl.status == data_girls._status.graduated)
                    {
                        continue;
                    }
                    totalGirls++;
                    DateTime date;
                    if (TryGetGraduationDate(girl, out date))
                    {
                        if (date.Year == selectedYear)
                        {
                            months[date.Month - 1].Add(new GirlEntry
                            {
                                Girl = girl,
                                Date = date,
                                Name = girl.GetName(true)
                            });
                        }
                    }
                    else
                    {
                        unknown.Add(new GirlEntry
                        {
                            Girl = girl,
                            Name = girl.GetName(true)
                        });
                    }
                }
            }
            if (totalGirls == 0)
            {
                AddEmptyMessage(ModLocalization.Get(NoIdolsLocalizationKey, NoIdolsFallbackLabel));
                Log(string.Format(LogRenderYearNoIdolsFormat, selectedYear));
                return;
            }

            for (int month = FirstMonthOfYear; month <= MonthsInYear; month++)
            {
                List<GirlEntry> list = months[month - FirstMonthOfYear];
                list.Sort(CompareByDateAndName);
            }
            unknown.Sort(CompareByName);

            bool hasGraduations = false;
            for (int month = FirstMonthOfYear; month <= MonthsInYear; month++)
            {
                List<GirlEntry> list = months[month - FirstMonthOfYear];
                if (list.Count == 0)
                {
                    continue;
                }
                hasGraduations = true;
                Transform grid = AddSectionGrid(GetMonthName(month), list.Count);
                foreach (GirlEntry entry in list)
                {
                    AddGirlTile(grid, entry.Girl);
                }
            }

            if (!hasGraduations)
            {
                AddEmptyMessage(ModLocalization.Get(NoGraduationsLocalizationKey, NoGraduationsFallbackLabel));
            }

            if (unknown.Count > 0)
            {
                Transform grid = AddSectionGrid(GetUnknownLabel(), unknown.Count);
                foreach (GirlEntry entry in unknown)
                {
                    AddGirlTile(grid, entry.Girl);
                }
            }

            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = ScrollTopNormalizedPosition;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot.GetComponent<RectTransform>());
            Log(string.Format(LogRenderYearSummaryFormat, selectedYear, totalGirls, unknown.Count, contentRoot.childCount));
        }

        private static void RenderGraduationsOnly()
        {
            ExtensionMethods.destroyChildren(contentRoot);
            Dictionary<int, List<GirlEntry>> groups = new Dictionary<int, List<GirlEntry>>();
            List<GirlEntry> unknown = new List<GirlEntry>();
            if (data_girls.girl != null)
            {
                foreach (data_girls.girls girl in data_girls.girl)
                {
                    if (girl == null || girl.status == data_girls._status.graduated)
                    {
                        continue;
                    }
                    DateTime date;
                    if (TryGetGraduationDate(girl, out date))
                    {
                        int key = date.Year * DateKeyYearMultiplier + date.Month;
                        List<GirlEntry> list;
                        if (!groups.TryGetValue(key, out list))
                        {
                            list = new List<GirlEntry>();
                            groups[key] = list;
                        }
                        list.Add(new GirlEntry
                        {
                            Girl = girl,
                            Date = date,
                            Name = girl.GetName(true)
                        });
                    }
                    else
                    {
                        unknown.Add(new GirlEntry
                        {
                            Girl = girl,
                            Name = girl.GetName(true)
                        });
                    }
                }
            }

            List<int> keys = new List<int>(groups.Keys);
            keys.Sort();
            foreach (int key in keys)
            {
                List<GirlEntry> list = groups[key];
                if (list == null || list.Count == 0)
                {
                    continue;
                }
                list.Sort(CompareByDateAndName);
                const int firstGroupedEntryIndex = 0;
                DateTime date = list[firstGroupedEntryIndex].Date;
                string header = GetMonthNameOnly(date) + SpaceSeparator + date.Year;
                Transform grid = AddSectionGrid(header, list.Count);
                foreach (GirlEntry entry in list)
                {
                    AddGirlTile(grid, entry.Girl);
                }
            }

            if (unknown.Count > 0)
            {
                unknown.Sort(CompareByName);
                Transform grid = AddSectionGrid(GetUnknownLabel(), unknown.Count);
                foreach (GirlEntry entry in unknown)
                {
                    AddGirlTile(grid, entry.Girl);
                }
            }

            if (keys.Count == 0 && unknown.Count == 0)
            {
                AddEmptyMessage(ModLocalization.Get(NoGraduationsScheduledLocalizationKey, NoGraduationsScheduledFallbackLabel));
            }

            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = ScrollTopNormalizedPosition;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot.GetComponent<RectTransform>());
        }

        private static void RenderYearly()
        {
            ExtensionMethods.destroyChildren(contentRoot);
            Dictionary<int, List<GirlEntry>> groups = new Dictionary<int, List<GirlEntry>>();
            List<GirlEntry> unknown = new List<GirlEntry>();
            if (data_girls.girl != null)
            {
                foreach (data_girls.girls girl in data_girls.girl)
                {
                    if (girl == null || girl.status == data_girls._status.graduated)
                    {
                        continue;
                    }
                    DateTime date;
                    if (TryGetGraduationDate(girl, out date))
                    {
                        List<GirlEntry> list;
                        if (!groups.TryGetValue(date.Year, out list))
                        {
                            list = new List<GirlEntry>();
                            groups[date.Year] = list;
                        }
                        list.Add(new GirlEntry
                        {
                            Girl = girl,
                            Date = date,
                            Name = girl.GetName(true)
                        });
                    }
                    else
                    {
                        unknown.Add(new GirlEntry
                        {
                            Girl = girl,
                            Name = girl.GetName(true)
                        });
                    }
                }
            }

            List<int> years = new List<int>(groups.Keys);
            years.Sort();
            foreach (int year in years)
            {
                List<GirlEntry> list = groups[year];
                if (list == null || list.Count == 0)
                {
                    continue;
                }
                list.Sort(CompareByDateAndName);
                Transform grid = AddSectionGrid(year.ToString(), list.Count);
                foreach (GirlEntry entry in list)
                {
                    AddGirlTile(grid, entry.Girl);
                }
            }

            if (unknown.Count > 0)
            {
                unknown.Sort(CompareByName);
                Transform grid = AddSectionGrid(GetUnknownLabel(), unknown.Count);
                foreach (GirlEntry entry in unknown)
                {
                    AddGirlTile(grid, entry.Girl);
                }
            }

            if (years.Count == 0 && unknown.Count == 0)
            {
                AddEmptyMessage(ModLocalization.Get(NoGraduationsScheduledLocalizationKey, NoGraduationsScheduledFallbackLabel));
            }

            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = ScrollTopNormalizedPosition;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot.GetComponent<RectTransform>());
        }

        private static void RenderCalendarGrid()
        {
            const float dayGridSpacing = 4f;
            const float dayHeaderSpacingY = 2f;
            const string weekdaysRowObjectName = "Weekdays";
            const string dayLabelObjectName = "DayLabel";
            const int dayLabelFontSize = 18;
            const string calendarGridObjectName = "CalendarGrid";

            ExtensionMethods.destroyChildren(contentRoot);
            if (selectedYear <= 0)
            {
                selectedYear = staticVars.dateTime.Year;
            }
            if (selectedMonth < FirstMonthOfYear || selectedMonth > MonthsInYear)
            {
                selectedMonth = staticVars.dateTime.Month;
            }
            DateTime firstDay = new DateTime(selectedYear, selectedMonth, FirstMonthOfYear);
            int daysInMonth = DateTime.DaysInMonth(selectedYear, selectedMonth);
            System.Globalization.CultureInfo culture = staticVars.GetCulture() as System.Globalization.CultureInfo
                ?? System.Globalization.CultureInfo.CurrentCulture;
            DayOfWeek firstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek;
            string[] dayNames = culture.DateTimeFormat.AbbreviatedDayNames;
            int offset = ((int)firstDay.DayOfWeek - (int)firstDayOfWeek + CalendarColumns) % CalendarColumns;

            Dictionary<int, List<data_girls.girls>> byDay = new Dictionary<int, List<data_girls.girls>>();
            if (data_girls.girl != null)
            {
                foreach (data_girls.girls girl in data_girls.girl)
                {
                    if (girl == null || girl.status == data_girls._status.graduated)
                    {
                        continue;
                    }
                    DateTime date;
                    if (TryGetGraduationDate(girl, out date))
                    {
                        if (date.Year == selectedYear && date.Month == selectedMonth)
                        {
                            List<data_girls.girls> list;
                            if (!byDay.TryGetValue(date.Day, out list))
                            {
                                list = new List<data_girls.girls>();
                                byDay[date.Day] = list;
                            }
                            list.Add(girl);
                        }
                    }
                }
            }

            float calendarWidth = CalendarColumns * CalendarCellWidth + (CalendarColumns - 1) * dayGridSpacing;

            GameObject headerRow = CreateUIObject(weekdaysRowObjectName, contentRoot);
            GridLayoutGroup headerGrid = headerRow.AddComponent<GridLayoutGroup>();
            headerGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            headerGrid.constraintCount = CalendarColumns;
            headerGrid.cellSize = new Vector2(CalendarCellWidth, CalendarHeaderHeight);
            headerGrid.spacing = new Vector2(dayGridSpacing, dayHeaderSpacingY);
            headerGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            headerGrid.childAlignment = TextAnchor.MiddleCenter;
            LayoutElement headerLayout = headerRow.AddComponent<LayoutElement>();
            headerLayout.preferredWidth = calendarWidth;
            headerLayout.preferredHeight = CalendarHeaderHeight;
            for (int i = 0; i < CalendarColumns; i++)
            {
                int idx = ((int)firstDayOfWeek + i) % CalendarColumns;
                string dayLabel = dayNames[idx];
                TextMeshProUGUI dayText = CreateText(
                    headerRow.transform,
                    dayLabelObjectName,
                    dayLabel,
                    dayLabelFontSize,
                    TextAlignmentOptions.Center,
                    GetPurpleTextColor());
                dayText.enableWordWrapping = false;
                dayText.overflowMode = TextOverflowModes.Ellipsis;
                LayoutElement layout = dayText.gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = CalendarHeaderHeight;
            }

            GameObject gridObj = CreateUIObject(calendarGridObjectName, contentRoot);
            GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = CalendarColumns;
            grid.cellSize = new Vector2(CalendarCellWidth, CalendarCellHeight);
            grid.spacing = new Vector2(dayGridSpacing, dayGridSpacing);
            grid.childAlignment = TextAnchor.UpperLeft;
            LayoutElement gridLayout = gridObj.AddComponent<LayoutElement>();
            gridLayout.preferredWidth = calendarWidth;

            int totalSlots = offset + daysInMonth;
            totalSlots = ((totalSlots + CalendarColumns - 1) / CalendarColumns) * CalendarColumns;
            int rows = totalSlots / CalendarColumns;
            gridLayout.preferredHeight = rows * CalendarCellHeight + (rows - 1) * dayGridSpacing;
            for (int slot = 0; slot < totalSlots; slot++)
            {
                int day = slot - offset + FirstMonthOfYear;
                if (day < FirstMonthOfYear || day > daysInMonth)
                {
                    CreateEmptyCalendarCell(gridObj.transform);
                    continue;
                }
                List<data_girls.girls> list;
                byDay.TryGetValue(day, out list);
                CreateCalendarDayCell(gridObj.transform, day, list);
            }

            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = ScrollTopNormalizedPosition;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot.GetComponent<RectTransform>());
        }

        private static void CreateEmptyCalendarCell(Transform parent)
        {
            const string dayCellObjectName = "DayCell";
            GameObject cell = CreateUIObject(dayCellObjectName, parent);
            AddCalendarCellBorder(cell);
            LayoutElement layout = cell.AddComponent<LayoutElement>();
            layout.preferredWidth = CalendarCellWidth;
            layout.preferredHeight = CalendarCellHeight;
        }

        private static void CreateCalendarDayCell(Transform parent, int day, List<data_girls.girls> girls)
        {
            const string dayCellObjectName = "DayCell";
            const string dayBadgeObjectName = "DayBadge";
            const string dayTextRootObjectName = "DayText";
            const string digitObjectName = "Digit";
            const string portraitsObjectName = "Portraits";
            const string moreIndicatorObjectName = "More";
            const float cellPaddingLeft = 4f;
            const float cellPaddingRight = 4f;
            const float cellPaddingTop = 24f;
            const float cellPaddingBottom = 4f;
            const float cellSpacing = 4f;
            const float dayBadgeOffsetX = 4f;
            const float dayBadgeOffsetY = -2f;
            const float dayBadgeWidth = 30f;
            const float dayBadgeHeight = 18f;
            const float dayBadgeLeftAnchor = 0f;
            const float dayBadgeTopAnchor = 1f;
            const float dayTextCharacterSpacing = 0f;
            const byte dayBadgeColorChannel = 255;
            const byte dayBadgeAlpha = 140;
            const int dayDigitFontSize = 14;
            const float dayDigitWidth = 10f;
            const float dayDigitHeight = 18f;
            const int portraitsPerRow = 4;
            const float portraitGridSpacing = 2f;
            const int visiblePortraitRows = 2;
            const int maxVisiblePortraits = 7;
            const int moreIndicatorFontSize = 12;
            const string moreIndicatorPrefix = "+";

            GameObject cell = CreateUIObject(dayCellObjectName, parent);
            AddCalendarCellBorder(cell);
            VerticalLayoutGroup layout = cell.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.padding = new RectOffset(
                Mathf.RoundToInt(cellPaddingLeft),
                Mathf.RoundToInt(cellPaddingRight),
                Mathf.RoundToInt(cellPaddingTop),
                Mathf.RoundToInt(cellPaddingBottom));
            layout.spacing = cellSpacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            LayoutElement cellLayout = cell.AddComponent<LayoutElement>();
            cellLayout.preferredWidth = CalendarCellWidth;
            cellLayout.preferredHeight = CalendarCellHeight;

            GameObject dayBadge = CreateUIObject(dayBadgeObjectName, cell.transform);
            RectTransform badgeRect = dayBadge.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(dayBadgeLeftAnchor, dayBadgeTopAnchor);
            badgeRect.anchorMax = new Vector2(dayBadgeLeftAnchor, dayBadgeTopAnchor);
            badgeRect.pivot = new Vector2(dayBadgeLeftAnchor, dayBadgeTopAnchor);
            badgeRect.anchoredPosition = new Vector2(dayBadgeOffsetX, dayBadgeOffsetY);
            badgeRect.sizeDelta = new Vector2(dayBadgeWidth, dayBadgeHeight);
            Image badgeImage = dayBadge.AddComponent<Image>();
            badgeImage.color = new Color32(dayBadgeColorChannel, dayBadgeColorChannel, dayBadgeColorChannel, dayBadgeAlpha);
            badgeImage.raycastTarget = false;
            LayoutElement badgeLayout = dayBadge.AddComponent<LayoutElement>();
            badgeLayout.preferredHeight = dayBadgeHeight;
            badgeLayout.preferredWidth = dayBadgeWidth;
            badgeLayout.ignoreLayout = true;

            GameObject dayTextRoot = CreateUIObject(dayTextRootObjectName, dayBadge.transform);
            RectTransform dayRect = dayTextRoot.GetComponent<RectTransform>();
            dayRect.anchorMin = Vector2.zero;
            dayRect.anchorMax = Vector2.one;
            dayRect.offsetMin = Vector2.zero;
            dayRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup dayLayout = dayTextRoot.AddComponent<HorizontalLayoutGroup>();
            dayLayout.childAlignment = TextAnchor.MiddleCenter;
            dayLayout.childControlWidth = true;
            dayLayout.childControlHeight = true;
            dayLayout.childForceExpandWidth = false;
            dayLayout.childForceExpandHeight = false;
            dayLayout.spacing = dayTextCharacterSpacing;

            string dayText = day.ToString();
            foreach (char c in dayText)
            {
                Text digit = CreateLegacyText(
                    dayTextRoot.transform,
                    digitObjectName,
                    c.ToString(),
                    dayDigitFontSize,
                    TextAnchor.MiddleCenter,
                    GetPurpleTextColor());
                RectTransform digitRect = digit.GetComponent<RectTransform>();
                digitRect.sizeDelta = new Vector2(dayDigitWidth, dayDigitHeight);
                LayoutElement digitLayout = digit.gameObject.AddComponent<LayoutElement>();
                digitLayout.preferredWidth = dayDigitWidth;
                digitLayout.preferredHeight = dayDigitHeight;
            }

            if (girls == null || girls.Count == 0)
            {
                dayBadge.transform.SetAsLastSibling();
                return;
            }

            GameObject portraitsObj = CreateUIObject(portraitsObjectName, cell.transform);
            GridLayoutGroup portraitsGrid = portraitsObj.AddComponent<GridLayoutGroup>();
            portraitsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            portraitsGrid.constraintCount = portraitsPerRow;
            portraitsGrid.cellSize = new Vector2(CalendarPortraitSize, CalendarPortraitSize);
            portraitsGrid.spacing = new Vector2(portraitGridSpacing, portraitGridSpacing);
            portraitsGrid.childAlignment = TextAnchor.UpperLeft;
            LayoutElement portraitLayout = portraitsObj.AddComponent<LayoutElement>();
            portraitLayout.preferredHeight = CalendarPortraitSize * visiblePortraitRows + portraitGridSpacing;

            int count = Mathf.Min(girls.Count, maxVisiblePortraits);
            for (int i = 0; i < count; i++)
            {
                AddCalendarPortrait(portraitsObj.transform, girls[i]);
            }
            if (girls.Count > maxVisiblePortraits)
            {
                TextMeshProUGUI more = CreateText(
                    portraitsObj.transform,
                    moreIndicatorObjectName,
                    moreIndicatorPrefix + (girls.Count - maxVisiblePortraits).ToString(),
                    moreIndicatorFontSize,
                    TextAlignmentOptions.Center,
                    GetMutedTextColor());
                more.enableWordWrapping = false;
            }
            dayBadge.transform.SetAsLastSibling();
        }

        private static void AddCalendarCellBorder(GameObject cell)
        {
            const byte borderTransparentChannel = 255;
            const byte borderTransparentAlpha = 0;
            const byte outlineColorChannel = 180;
            const byte outlineAlpha = 160;
            const float outlineOffsetX = 1f;
            const float outlineOffsetY = -1f;

            if (cell == null)
            {
                return;
            }
            Image image = cell.GetComponent<Image>();
            if (image == null)
            {
                image = cell.AddComponent<Image>();
            }
            image.color = new Color32(
                borderTransparentChannel,
                borderTransparentChannel,
                borderTransparentChannel,
                borderTransparentAlpha);
            Outline outline = cell.GetComponent<Outline>();
            if (outline == null)
            {
                outline = cell.AddComponent<Outline>();
            }
            outline.effectColor = new Color32(
                outlineColorChannel,
                outlineColorChannel,
                outlineColorChannel,
                outlineAlpha);
            outline.effectDistance = new Vector2(outlineOffsetX, outlineOffsetY);
            outline.useGraphicAlpha = false;
        }

        private static void AddCalendarPortrait(Transform parent, data_girls.girls girl)
        {
            const string portraitObjectName = "Portrait";
            const byte transparentColorChannel = 0;
            const byte transparentColorAlpha = 0;
            GameObject portraitObj = CreateUIObject(portraitObjectName, parent);
            Image portrait = portraitObj.AddComponent<Image>();
            portrait.preserveAspect = true;
            RectTransform rect = portraitObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(CalendarPortraitSize, CalendarPortraitSize);
            LayoutElement layout = portraitObj.AddComponent<LayoutElement>();
            layout.preferredWidth = CalendarPortraitSize;
            layout.preferredHeight = CalendarPortraitSize;
            Sprite spr = GetPortraitSprite(girl);
            GirlProfileOnHover hover = portraitObj.AddComponent<GirlProfileOnHover>();
            hover.ShouldShowTooltip = true;
            if (spr != null)
            {
                portrait.sprite = spr;
                portrait.color = mainScript.white32;
                hover.Set(girl, true);
            }
            else
            {
                portrait.color = new Color32(
                    transparentColorChannel,
                    transparentColorChannel,
                    transparentColorChannel,
                    transparentColorAlpha);
                hover.Set(girl, false);
                data_girls_textures.AddToQueue(girl, portraitObj);
            }

            Button button = portraitObj.AddComponent<Button>();
            button.targetGraphic = portrait;
            button.onClick.AddListener(delegate
            {
                OpenProfileFromCalendar(girl);
            });
        }

        private static int CompareByDateAndName(GirlEntry a, GirlEntry b)
        {
            if (a == null && b == null)
            {
                return 0;
            }
            if (a == null)
            {
                return 1;
            }
            if (b == null)
            {
                return -1;
            }
            int cmp = a.Date.Month.CompareTo(b.Date.Month);
            if (cmp != 0)
            {
                return cmp;
            }
            cmp = a.Date.Day.CompareTo(b.Date.Day);
            if (cmp != 0)
            {
                return cmp;
            }
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareByName(GirlEntry a, GirlEntry b)
        {
            if (a == null && b == null)
            {
                return 0;
            }
            if (a == null)
            {
                return 1;
            }
            if (b == null)
            {
                return -1;
            }
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetGraduationDate(data_girls.girls girl, out DateTime date)
        {
            date = girl.Graduation_Date;
            if (girl.Will_Graduate_At_18)
            {
                int months = GraduationAgeMonths - girl.GetAge() * MonthsInYear;
                date = staticVars.dateTime.AddMonths(months);
                return true;
            }
            return date.Year != UnknownGraduationYear;
        }

        private static string GetMonthName(int month)
        {
            DateTime date = new DateTime(selectedYear, month, FirstMonthOfYear);
            return GetMonthNameOnly(date);
        }

        private static string GetMonthNameOnly(DateTime date)
        {
            return date.ToString(MonthNameFormat, staticVars.GetCulture());
        }

        private static string GetUnknownLabel()
        {
            if (Language.Data != null && Language.Data.ContainsKey(UnknownLocalizationDictionaryKey))
            {
                return Language.Data[UnknownLocalizationDictionaryKey];
            }
            return ModLocalization.Get(UnknownLocalizationKey, UnknownFallbackLabel);
        }

        private static void AddSectionHeader(string text)
        {
            const string sectionHeaderObjectName = "SectionHeader";
            const int sectionHeaderFontSize = 24;
            const float sectionHeaderHeight = 22f;
            TextMeshProUGUI header = CreateText(
                contentRoot,
                sectionHeaderObjectName,
                text,
                sectionHeaderFontSize,
                TextAlignmentOptions.Left,
                GetPurpleTextColor());
            header.enableWordWrapping = false;
            LayoutElement layout = header.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = sectionHeaderHeight;
        }

        private static void AddEmptyMessage(string text)
        {
            const string emptyMessageObjectName = "Empty";
            const int emptyMessageFontSize = 24;
            const float emptyMessageHeight = 40f;
            TextMeshProUGUI empty = CreateText(
                contentRoot,
                emptyMessageObjectName,
                text,
                emptyMessageFontSize,
                TextAlignmentOptions.Center,
                GetMutedTextColor());
            empty.enableWordWrapping = false;
            LayoutElement layout = empty.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = emptyMessageHeight;
        }

        private static void AddMonthEmptyMessage(string text)
        {
            const string monthEmptyMessageObjectName = "MonthEmpty";
            const int monthEmptyMessageFontSize = 16;
            const float monthEmptyMessageHeight = 18f;
            TextMeshProUGUI empty = CreateText(
                contentRoot,
                monthEmptyMessageObjectName,
                text,
                monthEmptyMessageFontSize,
                TextAlignmentOptions.Left,
                GetMutedTextColor());
            empty.enableWordWrapping = false;
            LayoutElement layout = empty.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = monthEmptyMessageHeight;
        }

        private static Transform AddSectionGrid(string headerText, int itemCount)
        {
            const string monthGridObjectName = "MonthGrid";
            const float monthGridSpacingX = 10f;
            const float monthGridSpacingY = 1f;
            const int monthGridPaddingLeft = 4;
            const int monthGridPaddingRight = 4;
            const int monthGridPaddingTop = 0;
            const int monthGridPaddingBottom = 0;

            AddSectionHeader(headerText);
            GameObject grid = CreateUIObject(monthGridObjectName, contentRoot);
            GridLayoutGroup gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(GridCellWidth, GridCellHeight);
            gridLayout.spacing = new Vector2(monthGridSpacingX, monthGridSpacingY);
            gridLayout.padding = new RectOffset(
                monthGridPaddingLeft,
                monthGridPaddingRight,
                monthGridPaddingTop,
                monthGridPaddingBottom);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = GridColumns;

            ContentSizeFitter fitter = grid.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            LayoutElement layout = grid.AddComponent<LayoutElement>();
            layout.preferredHeight = CalculateGridHeight(itemCount, gridLayout);
            return grid.transform;
        }

        private static float CalculateGridHeight(int itemCount, GridLayoutGroup grid)
        {
            const float noGridHeight = 0f;
            if (grid == null)
            {
                return noGridHeight;
            }
            int columns = Mathf.Max(FirstMonthOfYear, grid.constraintCount);
            int rows = Mathf.Max(FirstMonthOfYear, Mathf.CeilToInt(itemCount / (float)columns));
            float height = rows * grid.cellSize.y;
            height += (rows - FirstMonthOfYear) * grid.spacing.y;
            height += grid.padding.top + grid.padding.bottom;
            return height;
        }

        private static void AddGirlTile(Transform grid, data_girls.girls girl)
        {
            const string girlTileObjectName = "GirlTile";
            const string portraitObjectName = "Portrait";
            const string nameObjectName = "Name";
            const int idolNameFontSize = 13;
            const float idolNameMinFontSize = 10f;
            const float idolNameMaxFontSize = 13f;
            const float tileVerticalSpacing = 0f;
            const byte transparentColorChannel = 0;
            const byte transparentColorAlpha = 0;

            if (girl == null || grid == null)
            {
                return;
            }
            GameObject tile = CreateUIObject(girlTileObjectName, grid);
            VerticalLayoutGroup tileLayout = tile.AddComponent<VerticalLayoutGroup>();
            tileLayout.childAlignment = TextAnchor.UpperCenter;
            tileLayout.spacing = tileVerticalSpacing;
            tileLayout.childControlWidth = true;
            tileLayout.childControlHeight = true;
            tileLayout.childForceExpandHeight = false;
            tileLayout.childForceExpandWidth = false;
            LayoutElement tileElement = tile.AddComponent<LayoutElement>();
            tileElement.preferredWidth = GridCellWidth;
            tileElement.preferredHeight = GridCellHeight;

            GameObject portraitObj = CreateUIObject(portraitObjectName, tile.transform);
            Image portrait = portraitObj.AddComponent<Image>();
            portrait.preserveAspect = true;
            RectTransform portraitRect = portraitObj.GetComponent<RectTransform>();
            portraitRect.sizeDelta = new Vector2(PortraitSize, PortraitSize);
            LayoutElement portraitLayout = portraitObj.AddComponent<LayoutElement>();
            portraitLayout.preferredWidth = PortraitSize;
            portraitLayout.preferredHeight = PortraitSize;
            Button portraitButton = portraitObj.GetComponent<Button>();
            if (portraitButton == null)
            {
                portraitButton = portraitObj.AddComponent<Button>();
            }
            portraitButton.targetGraphic = portrait;
            portraitButton.onClick = new Button.ButtonClickedEvent();
            portraitButton.onClick.AddListener(delegate
            {
                OpenProfileFromCalendar(girl);
            });
            GirlProfileOnHover hover = portraitObj.AddComponent<GirlProfileOnHover>();
            hover.ShouldShowTooltip = false;
            Sprite spr = GetPortraitSprite(girl);
            if (spr != null)
            {
                portrait.sprite = spr;
                portrait.color = mainScript.white32;
                hover.Set(girl, true);
            }
            else
            {
                portrait.color = new Color32(
                    transparentColorChannel,
                    transparentColorChannel,
                    transparentColorChannel,
                    transparentColorAlpha);
                hover.Set(girl, false);
                data_girls_textures.AddToQueue(girl, portraitObj);
            }

            TextMeshProUGUI name = CreateText(
                tile.transform,
                nameObjectName,
                girl.GetName(true),
                idolNameFontSize,
                TextAlignmentOptions.Center,
                mainScript.green32);
            name.enableWordWrapping = false;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.maxVisibleLines = FirstMonthOfYear;
            name.enableAutoSizing = true;
            name.fontSizeMin = idolNameMinFontSize;
            name.fontSizeMax = idolNameMaxFontSize;
            RectTransform nameRect = name.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(GridCellWidth, NameHeight);
            LayoutElement nameLayout = name.gameObject.AddComponent<LayoutElement>();
            nameLayout.preferredHeight = NameHeight;
        }

        private static Sprite GetPortraitSprite(data_girls.girls girl)
        {
            if (girl == null || girl.texture == null)
            {
                return null;
            }
            if (girl.texture.small != null)
            {
                return girl.texture.small;
            }
            if (girl.texture.middle != null)
            {
                return girl.texture.middle;
            }
            return null;
        }

        private static void EnsureDefaultFont()
        {
            if (defaultFont != null)
            {
                return;
            }
            try
            {
                if (Camera.main != null)
                {
                    mainScript main = Camera.main.GetComponent<mainScript>();
                    if (main != null && main.Data != null)
                    {
                        Fonts fonts = main.Data.GetComponent<Fonts>();
                        if (fonts != null && fonts.FontAsset != null)
                        {
                            defaultFont = fonts.FontAsset;
                            return;
                        }
                    }
                }
                TextMeshProUGUI[] fontsInScene = UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>();
                foreach (TextMeshProUGUI item in fontsInScene)
                {
                    if (item != null && item.font != null)
                    {
                        defaultFont = item.font;
                        return;
                    }
                }
            }
            catch
            {
            }
        }

        private static void EnsureDefaultLegacyFont()
        {
            if (defaultLegacyFont != null)
            {
                return;
            }
            try
            {
                if (Camera.main != null)
                {
                    mainScript main = Camera.main.GetComponent<mainScript>();
                    if (main != null && main.Data != null)
                    {
                        Fonts fonts = main.Data.GetComponent<Fonts>();
                        if (fonts != null)
                        {
                            Font font = fonts.GetFont();
                            if (font != null)
                            {
                                defaultLegacyFont = font;
                                return;
                            }
                        }
                    }
                }
                Text[] texts = UnityEngine.Object.FindObjectsOfType<Text>();
                foreach (Text text in texts)
                {
                    if (text != null && text.font != null)
                    {
                        defaultLegacyFont = text.font;
                        return;
                    }
                }
                defaultLegacyFont = Resources.GetBuiltinResource<Font>(LegacyFontFallbackName);
            }
            catch
            {
            }
        }

        private static void CaptureDefaultFontFrom(GameObject source)
        {
            if (defaultFont != null || source == null)
            {
                return;
            }
            TextMeshProUGUI tmp = source.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null && tmp.font != null)
            {
                defaultFont = tmp.font;
            }
        }

        private static void OpenProfileFromCalendar(data_girls.girls girl)
        {
            if (girl == null)
            {
                return;
            }
            PopupManager manager = GetPopupManager();
            if (manager == null)
            {
                return;
            }
            if (popupComponent != null && popupOpen)
            {
                popupClosing = true;
                popupComponent.Hide(delegate
                {
                    popupOpen = false;
                    popupClosing = false;
                    OpenProfilePopup(manager, girl);
                    RestoreBackdrop();
                });
                return;
            }
            Close();
            OpenProfilePopup(manager, girl);
        }

        private static void OpenProfilePopup(PopupManager manager, data_girls.girls girl)
        {
            if (manager == null || girl == null)
            {
                return;
            }
            PopupManager._popup popup = manager.GetByType(PopupManager._type.girl_profile);
            if (popup == null || popup.obj == null)
            {
                return;
            }
            manager.Open(PopupManager._type.girl_profile, true);
            Profile_Popup profile = popup.obj.GetComponent<Profile_Popup>();
            if (profile != null)
            {
                profile.Set(girl);
            }
        }

        private static string GetCloseLabel()
        {
            if (Language.Data != null)
            {
                if (Language.Data.ContainsKey(CloseLocalizationDictionaryKey))
                {
                    return Language.Data[CloseLocalizationDictionaryKey];
                }
                if (Language.Data.ContainsKey(BackLocalizationDictionaryKey))
                {
                    return Language.Data[BackLocalizationDictionaryKey];
                }
            }
            return ModLocalization.Get(CloseLocalizationKey, CloseFallbackLabel);
        }

        private static void ApplyCalendarIcon(GameObject buttonObject)
        {
            if (buttonObject == null)
            {
                return;
            }
            string iconPath = GetCalendarIconPath();
            if (string.IsNullOrEmpty(iconPath) || !File.Exists(iconPath))
            {
                Log(string.Format(LogCalendarIconNotFoundFormat, iconPath));
                return;
            }
            Image iconImage = FindIconImage(buttonObject);
            if (iconImage == null)
            {
                iconImage = CreateIconImage(buttonObject);
            }
            if (iconImage == null)
            {
                Log(LogCalendarIconTargetMissing);
                return;
            }
            if (calendarSprite != null)
            {
                iconImage.sprite = calendarSprite;
                iconImage.color = mainScript.white32;
                HideButtonText(buttonObject);
                return;
            }
            MonoBehaviour runner = Camera.main != null ? Camera.main.GetComponent<mainScript>() : null;
            if (runner == null)
            {
                Log(LogCalendarIconRunnerMissing);
                return;
            }
            runner.StartCoroutine(LoadTexture.LoadSprite(iconPath, iconImage, delegate
            {
                if (iconImage != null && iconImage.sprite != null)
                {
                    calendarSprite = iconImage.sprite;
                    iconImage.color = mainScript.white32;
                    HideButtonText(buttonObject);
                }
            }));
            Log(string.Format(LogCalendarIconLoadingFormat, iconPath));
        }

        private static string GetCalendarIconPath()
        {
            string assemblyPath = GetAssemblyDirectory();
            if (!string.IsNullOrEmpty(assemblyPath))
            {
                string fromAssembly = Path.Combine(assemblyPath, CalendarIconFileName);
                if (File.Exists(fromAssembly))
                {
                    return fromAssembly;
                }
            }
            Mods._mod mod = FindModByTitle(ModTitle);
            if (mod == null)
            {
                mod = Mods.GetMod(ModFolderFallback);
            }
            if (mod == null)
            {
                return EmptyString;
            }
            string root = mod.GetPath();
            if (string.IsNullOrEmpty(root))
            {
                return EmptyString;
            }
            string[] candidates = new string[]
            {
                Path.Combine(root, CalendarIconFileName),
                Path.Combine(root, PluginsFolderName, CalendarIconFileName),
                Path.Combine(root, PluginsLowercaseFolderName, CalendarIconFileName),
                Path.Combine(root, TexturesFolderName, CalendarIconFileName),
                Path.Combine(root, TexturesFolderName, UiFolderName, CalendarIconFileName),
                Path.Combine(root, TexturesFolderName, IconsFolderName, CalendarIconFileName)
            };
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            const int firstCandidateIndex = 0;
            return candidates[firstCandidateIndex];
        }

        private static Mods._mod FindModByTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return null;
            }
            foreach (Mods._mod mod in Mods._Mods)
            {
                if (mod != null && string.Equals(mod.Title, title, StringComparison.OrdinalIgnoreCase))
                {
                    return mod;
                }
            }
            return null;
        }

        private static Image FindIconImage(GameObject buttonObject)
        {
            Button button = buttonObject.GetComponent<Button>();
            Graphic targetGraphic = button != null ? button.targetGraphic : null;
            Image[] images = buttonObject.GetComponentsInChildren<Image>();
            foreach (Image image in images)
            {
                if (image == null || image == targetGraphic)
                {
                    continue;
                }
                string name = image.gameObject.name;
                if (!string.IsNullOrEmpty(name) && name.IndexOf(IconKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return image;
                }
            }
            foreach (Image image in images)
            {
                if (image == null || image == targetGraphic)
                {
                    continue;
                }
                return image;
            }
            return targetGraphic as Image;
        }

        private static Image CreateIconImage(GameObject buttonObject)
        {
            const float centeredAnchor = 0.5f;
            const float fallbackIconSize = 32f;
            const float iconSizeToButtonRatio = 0.6f;
            const float minimumIconSize = 24f;
            const float maximumIconSize = 40f;
            const float minimumParentDimension = 0f;

            if (buttonObject == null)
            {
                return null;
            }
            RectTransform parentRect = buttonObject.GetComponent<RectTransform>();
            GameObject iconObj = new GameObject(CalendarIconObjectName, typeof(RectTransform));
            iconObj.transform.SetParent(buttonObject.transform, false);
            iconObj.layer = buttonObject.layer;
            RectTransform rect = iconObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(centeredAnchor, centeredAnchor);
            rect.anchorMax = new Vector2(centeredAnchor, centeredAnchor);
            rect.pivot = new Vector2(centeredAnchor, centeredAnchor);
            rect.anchoredPosition = Vector2.zero;
            float size = fallbackIconSize;
            if (parentRect != null)
            {
                float min = Mathf.Min(parentRect.rect.width, parentRect.rect.height);
                if (min > minimumParentDimension)
                {
                    size = Mathf.Clamp(min * iconSizeToButtonRatio, minimumIconSize, maximumIconSize);
                }
            }
            rect.sizeDelta = new Vector2(size, size);
            Image image = iconObj.AddComponent<Image>();
            image.preserveAspect = true;
            image.color = mainScript.white32;
            return image;
        }

        private static void ApplyArrowButtonStyle(GameObject buttonObject, bool next)
        {
            if (buttonObject == null)
            {
                return;
            }
            bool hasArrow = HasArrowSprite(buttonObject.transform, next);
            if (hasArrow)
            {
                HideButtonText(buttonObject);
            }
            SetArrowButtonColors(buttonObject);
        }

        private static void SetArrowButtonColors(GameObject buttonObject)
        {
            if (buttonObject == null)
            {
                return;
            }
            Color32 bg = GetButtonBackgroundColor();
            Color32 fg = GetButtonTextColor();
            Image smallest = FindSmallestSpriteImage(buttonObject.transform);
            Image largest = FindLargestImage(buttonObject.transform);
            List<Image> images = GetComponentsInChildrenAll<Image>(buttonObject.transform);
            foreach (Image image in images)
            {
                if (image == null)
                {
                    continue;
                }
                if (image == smallest)
                {
                    image.color = fg;
                    continue;
                }
                if (image == largest)
                {
                    image.color = bg;
                    continue;
                }
                if (image.sprite == null)
                {
                    image.color = bg;
                    continue;
                }
                string spriteName = image.sprite.name.ToLowerInvariant();
                bool arrow = spriteName.Contains(ArrowKeyword)
                    || spriteName.Contains(LeftKeyword)
                    || spriteName.Contains(RightKeyword);
                image.color = arrow ? fg : bg;
            }
        }

        private static bool HasArrowSprite(Transform root, bool next)
        {
            if (root == null)
            {
                return false;
            }
            List<Image> images = GetComponentsInChildrenAll<Image>(root);
            foreach (Image image in images)
            {
                if (image == null || image.sprite == null)
                {
                    continue;
                }
                string spriteName = image.sprite.name.ToLowerInvariant();
                if (spriteName.Length == 0)
                {
                    continue;
                }
                bool hasArrow = spriteName.Contains(ArrowKeyword)
                    || spriteName.Contains(LeftKeyword)
                    || spriteName.Contains(RightKeyword);
                if (!hasArrow)
                {
                    continue;
                }
                if (spriteName.Contains(LeftKeyword) || spriteName.Contains(PreviousKeyword))
                {
                    return !next;
                }
                if (spriteName.Contains(RightKeyword) || spriteName.Contains(NextKeyword))
                {
                    return next;
                }
                return true;
            }
            return false;
        }

        private static void SetButtonBackgroundColor(GameObject buttonObject, Color32 color)
        {
            if (buttonObject == null)
            {
                return;
            }
            Button button = buttonObject.GetComponent<Button>();
            List<Image> images = GetComponentsInChildrenAll<Image>(buttonObject.transform);
            foreach (Image image in images)
            {
                if (image == null)
                {
                    continue;
                }
                string spriteName = image.sprite != null ? image.sprite.name.ToLowerInvariant() : EmptyString;
                string objName = image.gameObject.name != null ? image.gameObject.name.ToLowerInvariant() : EmptyString;
                bool isArrow = spriteName.Contains(ArrowKeyword) || objName.Contains(ArrowKeyword);
                bool isIcon = spriteName.Contains(IconKeyword) || objName.Contains(IconKeyword) || objName.Contains(CalendarKeyword);
                if (isArrow || isIcon)
                {
                    continue;
                }
                image.color = color;
                if (image.sprite != null)
                {
                    bool looksLikeBackground = spriteName.Contains(ButtonKeyword)
                        || spriteName.Contains(BackgroundShortKeyword)
                        || spriteName.Contains(BackgroundKeyword)
                        || spriteName.Contains(RedKeyword)
                        || spriteName.Contains(PinkKeyword)
                        || spriteName.Contains(CancelKeyword)
                        || objName.Contains(BackgroundShortKeyword)
                        || objName.Contains(BackgroundKeyword)
                        || objName.Contains(ImageKeyword);
                    if (looksLikeBackground)
                    {
                        image.sprite = null;
                    }
                }
            }
        }

        private static void HideButtonText(GameObject buttonObject)
        {
            List<TextMeshProUGUI> tmps = GetComponentsInChildrenAll<TextMeshProUGUI>(buttonObject.transform);
            foreach (TextMeshProUGUI tmp in tmps)
            {
                if (tmp == null)
                {
                    continue;
                }
                tmp.text = EmptyString;
                tmp.gameObject.SetActive(false);
            }
            List<Text> texts = GetComponentsInChildrenAll<Text>(buttonObject.transform);
            foreach (Text text in texts)
            {
                if (text == null)
                {
                    continue;
                }
                text.text = EmptyString;
                text.gameObject.SetActive(false);
            }
        }

        private static string GetAssemblyDirectory()
        {
            try
            {
                string location = Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrEmpty(location))
                {
                    return EmptyString;
                }
                return Path.GetDirectoryName(location);
            }
            catch
            {
                return EmptyString;
            }
        }

        private static void EnsurePopupVisible()
        {
            if (popupRoot == null)
            {
                return;
            }
            CanvasGroup group = popupRoot.GetComponent<CanvasGroup>();
            if (group != null && group.alpha < VisibleCanvasAlphaThreshold)
            {
                group.alpha = VisibleAlpha;
                group.blocksRaycasts = true;
                group.interactable = true;
            }
            RectTransform rect = popupRoot.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
            }
        }

        private static void EnsureMenuButtonVisible(GameObject awardsButton, GameObject clone)
        {
            if (awardsButton == null || clone == null)
            {
                return;
            }
            clone.SetActive(true);
            CanvasGroup group = clone.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = VisibleAlpha;
                group.interactable = true;
                group.blocksRaycasts = true;
            }

            RectTransform parentRect = clone.transform.parent as RectTransform;
            RectTransform cloneRect = clone.GetComponent<RectTransform>();
            RectTransform awardsRect = awardsButton.GetComponent<RectTransform>();
            LayoutGroup layoutGroup = parentRect != null ? parentRect.GetComponent<LayoutGroup>() : null;
            if (layoutGroup != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
                Canvas.ForceUpdateCanvases();
                Log(string.Format(LogMenuButtonParentLayoutFormat, layoutGroup.GetType().Name));
                return;
            }
            if (cloneRect != null && awardsRect != null)
            {
                const float manualButtonSpacing = 8f;
                const float minimumButtonWidth = 0f;
                Vector2 awardsPos = awardsRect.anchoredPosition;
                float width = awardsRect.rect.width > minimumButtonWidth ? awardsRect.rect.width : awardsRect.sizeDelta.x;
                cloneRect.anchoredPosition = new Vector2(awardsPos.x - width - manualButtonSpacing, awardsPos.y);
                cloneRect.sizeDelta = awardsRect.sizeDelta;
                cloneRect.localScale = awardsRect.localScale;
                Log(string.Format(LogMenuButtonManualPositionFormat, awardsPos, cloneRect.anchoredPosition));
            }
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

        private static string GetButtonLabel(GameObject go)
        {
            if (go == null)
            {
                return EmptyString;
            }
            TextMeshProUGUI tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null && !string.IsNullOrEmpty(tmp.text))
            {
                return tmp.text.Trim();
            }
            Text text = go.GetComponentInChildren<Text>();
            if (text != null && !string.IsNullOrEmpty(text.text))
            {
                return text.text.Trim();
            }
            return EmptyString;
        }

        private static string GetHierarchyPath(GameObject go)
        {
            if (go == null)
            {
                return EmptyString;
            }
            List<string> parts = new List<string>();
            Transform current = go.transform;
            while (current != null)
            {
                parts.Add(current.name);
                current = current.parent;
            }
            parts.Reverse();
            return string.Join(HierarchyPathSeparator, parts.ToArray());
        }
    }
}
