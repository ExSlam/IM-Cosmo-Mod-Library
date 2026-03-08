using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace UIRecoveryTools
{
    internal static class PatchTargets
    {
        internal const string PopupManagerStartMethodName = "Start";
    }

    [HarmonyPatch(typeof(PopupManager), PatchTargets.PopupManagerStartMethodName)]
    internal static class PopupManager_Start_Patch
    {
        private static void Postfix(PopupManager __instance)
        {
            RecoveryController.Ensure(__instance);
        }
    }

    internal sealed class RecoveryController : MonoBehaviour
    {
        private const string LogPrefix = "[UIRecoveryTools]";
        private const string LogMessageSeparator = " ";
        private const int MaxEntries = 20;
        private const string ConfigFileName = "UIRecoveryTools.config.ini";
        private const KeyCode DefaultClearBlurKey = KeyCode.KeypadDivide;
        private const KeyCode DefaultToggleOverlayKey = KeyCode.KeypadMultiply;
        private const bool DefaultAutoRecoverEnabled = true;
        private const float DefaultAutoRecoverIntervalSeconds = 1f;
        private const float MinAutoRecoverIntervalSeconds = 0.25f;
        private const float MaxAutoRecoverIntervalSeconds = 15f;
        private const float AutoQueueRepairDelaySeconds = 1.2f;
        private const float PopupGhostAlphaThreshold = 0.001f;
        private const float InitialAutoRecoverDelaySeconds = 0.25f;
        private const float HiddenAlpha = 0f;
        private const float VisibleAlpha = 1f;
        private const float StaleQueueUnsetSinceValue = -1f;
        private const int StaleQueueUnsetCountValue = -1;
        private const int ZeroPopupCount = 0;
        private const int FirstCollectionIndex = 0;
        private const int NextIndexOffset = 1;
        private const string EntryTimestampFormat = "HH:mm:ss";
        private const int MaxEntryLength = 220;
        private const string EntryOverflowSuffix = "...";
        private const string EntryTextFormat = "{0} [{1}] {2}";
        private const string EntryStackFrameFormat = " ({0})";
        private const char StackTraceLineSeparator = '\n';
        private const string OverlayLineSeparator = "\n";
        private const string OverlayRootObjectName = "UIRecoveryOverlay";
        private const string OverlayPanelObjectName = "Panel";
        private const string OverlayHeaderObjectName = "Header";
        private const string OverlayHeaderTextObjectName = "HeaderText";
        private const string OverlayBodyObjectName = "Body";
        private const string OverlayViewportObjectName = "Viewport";
        private const string OverlayContentObjectName = "Content";
        private const string OverlayBodyTextObjectName = "BodyText";
        private const string OverlayButtonRowObjectName = "Buttons";
        private const string OverlayClearBlurButtonObjectName = "ClearBlur";
        private const string OverlayClearLogButtonObjectName = "ClearLog";
        private const string OverlayCloseButtonObjectName = "Close";
        private const string OverlayButtonLabelObjectName = "Label";
        private const int OverlayCanvasSortingOrder = 5000;
        private const int OverlayHeaderFontSize = 16;
        private const int OverlayBodyFontSize = 14;
        private const int OverlayButtonFontSize = 13;
        private const float OverlayScrollSensitivity = 20f;
        private const float OverlayButtonSpacing = 6f;
        private const float OverlayButtonPreferredWidth = 90f;
        private const float OverlayButtonPreferredHeight = 24f;
        private const string OverlayHeaderLocalizationKey = "overlay.header_format";
        private const string OverlayHeaderFallbackText = "Errors ({0} overlay, {1} clear blur)";
        private const string OverlayNoMessagesLocalizationKey = "overlay.no_messages";
        private const string OverlayNoMessagesFallbackText = "No messages to show.";
        private const string OverlayClearBlurLocalizationKey = "overlay.clear_blur_format";
        private const string OverlayClearBlurFallbackText = "Clear Blur ({0})";
        private const string OverlayClearLogLocalizationKey = "overlay.clear_log";
        private const string OverlayClearLogFallbackText = "Clear Log";
        private const string OverlayCloseLocalizationKey = "overlay.close_format";
        private const string OverlayCloseFallbackText = "Close ({0})";
        private const string ClearBlurSkippedLogPrefix = "Clear blur skipped: ";
        private const string ClearBlurAppliedLogMessage = "Clear blur applied.";
        private const string AutoRecoverAppliedLogMessage = "Auto-recovered stale blur state.";
        private const string CanClearReasonPopupManagerUnavailable = "popup manager unavailable";
        private const string CanClearReasonDialogueOpen = "dialogue open";
        private const string CanClearReasonGraduationCalendarOpen = "graduation calendar open";
        private const string CanClearReasonQueueNotEmpty = "popup queue not empty";
        private const string CanClearReasonPopupOpen = "popup open";
        private const string SuperBlurTypeName = "SuperBlur";
        private const string SuperBlurFastTypeName = "SuperBlurFast";
        private const string BlurInterpolationPropertyName = "interpolation";
        private const string EnabledPropertyName = "enabled";
        private const string GraduationCalendarPopupObjectName = "GraduationCalendar_Popup";
        private const string ConfigWriteFailedLogPrefix = "Config write failed: ";
        private const string ConfigReadFailedLogPrefix = "Config read failed: ";
        private const string ConfigLoadedLogFormat = "Config loaded: clear={0}, overlay={1}, auto_recover={2}, interval={3}s";
        private const string IntervalLogValueFormat = "0.##";
        private const string ConfigCommentPrefixHash = "#";
        private const string ConfigCommentPrefixSemicolon = ";";
        private const string ConfigCommentPrefixSlashSlash = "//";
        private const char ConfigKeyValueSeparator = '=';
        private const string ConfigKeyValueSeparatorText = "=";
        private const string ConfigKeyClearBlurKey = "clear_blur_key";
        private const string ConfigKeyToggleOverlayKey = "toggle_overlay_key";
        private const string ConfigKeyAutoRecoverEnabled = "auto_recover_enabled";
        private const string ConfigKeyAutoRecoverIntervalSeconds = "auto_recover_interval_seconds";
        private const string ConfigTemplateTitleComment = "# UIRecoveryTools configuration";
        private const string ConfigTemplateBlankComment = "#";
        private const string ConfigTemplateHotkeyInfoComment = "# Set each hotkey to a single Unity KeyCode name, for example: Home, End, BackQuote, Insert.";
        private const string ConfigTemplateDisableInfoComment = "# Set to NONE to disable that hotkey.";
        private const string ConfigTemplateDefaultChoiceInfoComment = "# Default choices below are single-key and are not used by Idol Manager base-game hotkeys.";
        private const string ConfigTemplateAutoRecoverInfoComment = "# Auto clear stale blur/backdrop when no popup is active (helps broken saves).";
        private const string ConfigTemplateIntervalInfoComment = "# Auto recover interval in seconds (0.25 to 15).";
        private const string ConfigTemplateAutoRecoverEnabledDefaultValue = "true";
        private const string ConfigTemplateAutoRecoverIntervalDefaultValue = "1.0";
        private const string KeyCodeDisabledValueNone = "none";
        private const string KeyCodeDisabledValueDisabled = "disabled";
        private const string BoolTrueNumericValue = "1";
        private const string BoolTrueYesValue = "yes";
        private const string BoolTrueShortValue = "y";
        private const string BoolFalseNumericValue = "0";
        private const string BoolFalseNoValue = "no";
        private const string BoolFalseShortValue = "n";
        private const string DisabledLocalizationKey = "common.disabled";
        private const string DisabledLocalizationFallbackText = "Disabled";
        private const string BuiltInFallbackFontName = "Arial.ttf";
        private static readonly Vector2 OverlayReferenceResolution = new Vector2(1920f, 1080f);
        private static readonly Color32 OverlayPanelColor = new Color32(20, 20, 20, 220);
        private static readonly Color32 OverlayViewportMaskColor = new Color32(255, 255, 255, 1);
        private static readonly Color32 OverlayButtonColor = new Color32(80, 80, 80, 240);
        private static readonly Vector2 OverlayPanelAnchor = new Vector2(1f, 0f);
        private static readonly Vector2 OverlayPanelSize = new Vector2(420f, 240f);
        private static readonly Vector2 OverlayPanelPosition = new Vector2(-20f, 20f);
        private static readonly Vector2 OverlayHeaderAnchorMin = new Vector2(0f, 1f);
        private static readonly Vector2 OverlayHeaderAnchorMax = new Vector2(1f, 1f);
        private static readonly Vector2 OverlayHeaderPivot = new Vector2(0f, 1f);
        private static readonly Vector2 OverlayHeaderSize = new Vector2(0f, 26f);
        private static readonly Vector2 OverlayHeaderPosition = new Vector2(8f, -6f);
        private static readonly Vector2 OverlayBodyAnchorMin = new Vector2(0f, 0f);
        private static readonly Vector2 OverlayBodyAnchorMax = new Vector2(1f, 1f);
        private static readonly Vector2 OverlayBodyPivot = new Vector2(0f, 0f);
        private static readonly Vector2 OverlayBodyOffsetMin = new Vector2(8f, 40f);
        private static readonly Vector2 OverlayBodyOffsetMax = new Vector2(-8f, -36f);
        private static readonly Vector2 OverlayContentAnchorMin = new Vector2(0f, 1f);
        private static readonly Vector2 OverlayContentAnchorMax = new Vector2(1f, 1f);
        private static readonly Vector2 OverlayContentPivot = new Vector2(0f, 1f);
        private static readonly Vector2 OverlayButtonRowAnchor = new Vector2(1f, 0f);
        private static readonly Vector2 OverlayButtonRowSize = new Vector2(300f, 28f);
        private static readonly Vector2 OverlayButtonRowPosition = new Vector2(-8f, 8f);
        private static readonly string[] DefaultConfigTemplateLines =
        {
            ConfigTemplateTitleComment,
            ConfigTemplateBlankComment,
            ConfigTemplateHotkeyInfoComment,
            ConfigTemplateDisableInfoComment,
            ConfigTemplateBlankComment,
            ConfigTemplateDefaultChoiceInfoComment,
            BuildConfigAssignmentLine(ConfigKeyClearBlurKey, nameof(KeyCode.KeypadDivide)),
            BuildConfigAssignmentLine(ConfigKeyToggleOverlayKey, nameof(KeyCode.KeypadMultiply)),
            ConfigTemplateAutoRecoverInfoComment,
            BuildConfigAssignmentLine(ConfigKeyAutoRecoverEnabled, ConfigTemplateAutoRecoverEnabledDefaultValue),
            ConfigTemplateIntervalInfoComment,
            BuildConfigAssignmentLine(ConfigKeyAutoRecoverIntervalSeconds, ConfigTemplateAutoRecoverIntervalDefaultValue)
        };

        private static RecoveryController instance;
        private readonly List<string> entries = new List<string>();

        private GameObject overlayRoot;
        private CanvasGroup overlayGroup;
        private Text overlayText;
        private bool overlayVisible;
        private Font defaultFont;
        private KeyCode clearBlurKey = DefaultClearBlurKey;
        private KeyCode toggleOverlayKey = DefaultToggleOverlayKey;
        private bool autoRecoverEnabled = DefaultAutoRecoverEnabled;
        private float autoRecoverIntervalSeconds = DefaultAutoRecoverIntervalSeconds;
        private float nextAutoRecoverAt;
        private float staleQueueSince = StaleQueueUnsetSinceValue;
        private int staleQueueCount = StaleQueueUnsetCountValue;

        internal static void Ensure(PopupManager manager)
        {
            if (manager == null)
            {
                return;
            }
            if (instance != null)
            {
                return;
            }
            instance = manager.gameObject.AddComponent<RecoveryController>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }
            instance = this;
            staleQueueSince = StaleQueueUnsetSinceValue;
            staleQueueCount = StaleQueueUnsetCountValue;
            LoadConfig();
            nextAutoRecoverAt = Time.unscaledTime + InitialAutoRecoverDelaySeconds;
            Application.logMessageReceived += OnLog;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
            Application.logMessageReceived -= OnLog;
        }

        private void Update()
        {
            HandleHotkeys();
            TryAutoRecoverBlur();
        }

        private void HandleHotkeys()
        {
            if (clearBlurKey != KeyCode.None && Input.GetKeyDown(clearBlurKey))
            {
                ForceClearUI();
            }
            if (toggleOverlayKey != KeyCode.None && Input.GetKeyDown(toggleOverlayKey))
            {
                ToggleOverlay();
            }
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                return;
            }
            string entry = BuildEntry(condition, stackTrace, type);
            entries.Add(entry);
            while (entries.Count > MaxEntries)
            {
                entries.RemoveAt(FirstCollectionIndex);
            }
            UpdateOverlayText();
        }

        private static string BuildEntry(string condition, string stackTrace, LogType type)
        {
            string text = string.Format(EntryTextFormat, DateTime.Now.ToString(EntryTimestampFormat), type, condition);
            if (!string.IsNullOrEmpty(stackTrace))
            {
                string[] lines = stackTrace.Split(new[] { StackTraceLineSeparator }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > ZeroPopupCount)
                {
                    text += string.Format(EntryStackFrameFormat, lines[FirstCollectionIndex].Trim());
                }
            }
            if (text.Length > MaxEntryLength)
            {
                text = text.Substring(FirstCollectionIndex, MaxEntryLength) + EntryOverflowSuffix;
            }
            return text;
        }

        private void ToggleOverlay()
        {
            EnsureOverlay();
            overlayVisible = !overlayVisible;
            overlayRoot.SetActive(overlayVisible);
            if (overlayGroup != null)
            {
                overlayGroup.alpha = overlayVisible ? VisibleAlpha : HiddenAlpha;
                overlayGroup.blocksRaycasts = overlayVisible;
                overlayGroup.interactable = overlayVisible;
            }
            UpdateOverlayText();
        }

        private void EnsureOverlay()
        {
            if (overlayRoot != null)
            {
                return;
            }

            overlayRoot = new GameObject(OverlayRootObjectName, typeof(RectTransform));
            overlayRoot.transform.SetParent(transform, false);
            overlayRoot.layer = gameObject.layer;

            Canvas canvas = overlayRoot.AddComponent<Canvas>();
            canvas.sortingOrder = OverlayCanvasSortingOrder;
            overlayRoot.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = overlayRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = OverlayReferenceResolution;

            overlayGroup = overlayRoot.AddComponent<CanvasGroup>();
            overlayGroup.alpha = HiddenAlpha;
            overlayGroup.blocksRaycasts = false;
            overlayGroup.interactable = false;

            GameObject panel = CreateUIObject(OverlayPanelObjectName, overlayRoot.transform);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = OverlayPanelColor;

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = OverlayPanelAnchor;
            panelRect.anchorMax = OverlayPanelAnchor;
            panelRect.pivot = OverlayPanelAnchor;
            panelRect.sizeDelta = OverlayPanelSize;
            panelRect.anchoredPosition = OverlayPanelPosition;

            GameObject header = CreateUIObject(OverlayHeaderObjectName, panel.transform);
            Text headerText = CreateLegacyText(
                header.transform,
                OverlayHeaderTextObjectName,
                string.Format(
                    ModLocalization.Get(OverlayHeaderLocalizationKey, OverlayHeaderFallbackText),
                    GetHotkeyLabel(toggleOverlayKey),
                    GetHotkeyLabel(clearBlurKey)),
                OverlayHeaderFontSize,
                TextAnchor.MiddleLeft,
                Color.white);
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = OverlayHeaderAnchorMin;
            headerRect.anchorMax = OverlayHeaderAnchorMax;
            headerRect.pivot = OverlayHeaderPivot;
            headerRect.sizeDelta = OverlayHeaderSize;
            headerRect.anchoredPosition = OverlayHeaderPosition;

            GameObject body = CreateUIObject(OverlayBodyObjectName, panel.transform);
            RectTransform bodyRect = body.GetComponent<RectTransform>();
            bodyRect.anchorMin = OverlayBodyAnchorMin;
            bodyRect.anchorMax = OverlayBodyAnchorMax;
            bodyRect.pivot = OverlayBodyPivot;
            bodyRect.offsetMin = OverlayBodyOffsetMin;
            bodyRect.offsetMax = OverlayBodyOffsetMax;

            ScrollRect scrollRect = body.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = OverlayScrollSensitivity;
            scrollRect.inertia = false;

            GameObject viewport = CreateUIObject(OverlayViewportObjectName, body.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = OverlayViewportMaskColor;
            Mask viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            GameObject content = CreateUIObject(OverlayContentObjectName, viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = OverlayContentAnchorMin;
            contentRect.anchorMax = OverlayContentAnchorMax;
            contentRect.pivot = OverlayContentPivot;
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            overlayText = CreateLegacyText(
                content.transform,
                OverlayBodyTextObjectName,
                ModLocalization.Get(OverlayNoMessagesLocalizationKey, OverlayNoMessagesFallbackText),
                OverlayBodyFontSize,
                TextAnchor.UpperLeft,
                Color.white);
            RectTransform textRect = overlayText.GetComponent<RectTransform>();
            textRect.anchorMin = OverlayContentAnchorMin;
            textRect.anchorMax = OverlayContentAnchorMax;
            textRect.pivot = OverlayContentPivot;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            GameObject buttonRow = CreateUIObject(OverlayButtonRowObjectName, panel.transform);
            RectTransform buttonRect = buttonRow.GetComponent<RectTransform>();
            buttonRect.anchorMin = OverlayButtonRowAnchor;
            buttonRect.anchorMax = OverlayButtonRowAnchor;
            buttonRect.pivot = OverlayButtonRowAnchor;
            buttonRect.sizeDelta = OverlayButtonRowSize;
            buttonRect.anchoredPosition = OverlayButtonRowPosition;

            HorizontalLayoutGroup layout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.spacing = OverlayButtonSpacing;
            layout.childControlHeight = true;
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;

            CreateButton(
                buttonRow.transform,
                OverlayClearBlurButtonObjectName,
                string.Format(ModLocalization.Get(OverlayClearBlurLocalizationKey, OverlayClearBlurFallbackText), GetHotkeyLabel(clearBlurKey)),
                ForceClearUI);
            CreateButton(
                buttonRow.transform,
                OverlayClearLogButtonObjectName,
                ModLocalization.Get(OverlayClearLogLocalizationKey, OverlayClearLogFallbackText),
                ClearLog);
            CreateButton(
                buttonRow.transform,
                OverlayCloseButtonObjectName,
                string.Format(ModLocalization.Get(OverlayCloseLocalizationKey, OverlayCloseFallbackText), GetHotkeyLabel(toggleOverlayKey)),
                ToggleOverlay);

            overlayRoot.SetActive(false);
        }

        private void UpdateOverlayText()
        {
            if (overlayText == null)
            {
                return;
            }
            if (entries.Count == ZeroPopupCount)
            {
                overlayText.text = ModLocalization.Get(OverlayNoMessagesLocalizationKey, OverlayNoMessagesFallbackText);
                return;
            }
            overlayText.text = string.Join(OverlayLineSeparator, entries.ToArray());
        }

        private void ClearLog()
        {
            entries.Clear();
            UpdateOverlayText();
        }

        private void ForceClearUI()
        {
            PopupManager manager = GetPopupManager();
            if (manager == null)
            {
                return;
            }

            string skipReason;
            if (!CanClearBackdrop(manager, true, out skipReason))
            {
                Log(ClearBlurSkippedLogPrefix + skipReason);
                return;
            }

            ApplyBackdropClear(manager);
            Log(ClearBlurAppliedLogMessage);
        }

        private void TryAutoRecoverBlur()
        {
            if (!autoRecoverEnabled)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now < nextAutoRecoverAt)
            {
                return;
            }

            nextAutoRecoverAt = now + autoRecoverIntervalSeconds;
            PopupManager manager = GetPopupManager();
            if (manager == null)
            {
                return;
            }

            if (!HasVisibleBackdrop(manager))
            {
                return;
            }

            string skipReason;
            if (!CanClearBackdrop(manager, false, out skipReason))
            {
                return;
            }

            ApplyBackdropClear(manager);
            Log(AutoRecoverAppliedLogMessage);
        }

        private bool CanClearBackdrop(PopupManager manager, bool allowQueueRepair, out string reason)
        {
            reason = string.Empty;
            if (manager == null)
            {
                reason = CanClearReasonPopupManagerUnavailable;
                return false;
            }

            if (ActiveDialogueController.ShowingDialogue)
            {
                reason = CanClearReasonDialogueOpen;
                return false;
            }

            if (IsGraduationCalendarOpen())
            {
                reason = CanClearReasonGraduationCalendarOpen;
                return false;
            }

            if (manager.queue != null && manager.queue.Count > ZeroPopupCount)
            {
                if (allowQueueRepair && !manager.IsThereAnOpenPopup())
                {
                    manager.queue.Clear();
                    ResetStaleQueueTracking();
                }

                if (manager.queue != null && manager.queue.Count > ZeroPopupCount)
                {
                    bool visiblePopupWithQueue = HasVisibleManagedPopup(manager, allowQueueRepair);
                    if (visiblePopupWithQueue)
                    {
                        ResetStaleQueueTracking();
                        reason = CanClearReasonQueueNotEmpty;
                        return false;
                    }

                    if (!allowQueueRepair && !IsAutoQueueRecoveryReady(manager))
                    {
                        reason = CanClearReasonQueueNotEmpty;
                        return false;
                    }

                    manager.queue.Clear();
                    ResetStaleQueueTracking();
                }
            }
            else
            {
                ResetStaleQueueTracking();
            }

            if (HasManagedPopupOpenOrActive(manager, allowQueueRepair))
            {
                reason = CanClearReasonPopupOpen;
                return false;
            }

            return true;
        }

        private bool IsAutoQueueRecoveryReady(PopupManager manager)
        {
            if (manager == null || manager.queue == null || manager.queue.Count == ZeroPopupCount)
            {
                ResetStaleQueueTracking();
                return false;
            }

            int currentCount = manager.queue.Count;
            float now = Time.unscaledTime;
            if (staleQueueSince == StaleQueueUnsetSinceValue || staleQueueCount != currentCount)
            {
                staleQueueSince = now;
                staleQueueCount = currentCount;
                return false;
            }

            return now - staleQueueSince >= AutoQueueRepairDelaySeconds;
        }

        private void ResetStaleQueueTracking()
        {
            staleQueueSince = StaleQueueUnsetSinceValue;
            staleQueueCount = StaleQueueUnsetCountValue;
        }

        private static void ApplyBackdropClear(PopupManager manager)
        {
            if (manager == null)
            {
                return;
            }

            try
            {
                manager.BGBlur(false, HiddenAlpha);
            }
            catch
            {
            }

            ForceDisableCameraBlur(manager);

            if (manager.BGImage != null)
            {
                CanvasGroup group = manager.BGImage.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    group.alpha = HiddenAlpha;
                }
                manager.BGImage.SetActive(false);
            }
            if (manager.BG != null)
            {
                CanvasGroup group = manager.BG.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    group.alpha = HiddenAlpha;
                }
                manager.BG.SetActive(false);
            }
            manager.BlockInput(false);
            mainScript main = Camera.main != null ? Camera.main.GetComponent<mainScript>() : null;
            if (main != null && ShouldResumeTimeAfterPopupClose())
            {
                main.Time_Resume();
            }
            if (PopupManager.PopupCounter > ZeroPopupCount)
            {
                PopupManager.PopupCounter = ZeroPopupCount;
            }
        }

        private static bool HasManagedPopupOpenOrActive(PopupManager manager, bool repairStaleState)
        {
            if (manager == null || manager.popups == null)
            {
                return false;
            }

            bool queueBusy = manager.queue != null && manager.queue.Count > ZeroPopupCount;
            bool allowRepair = repairStaleState && !queueBusy;
            for (int i = FirstCollectionIndex; i < manager.popups.Length; i++)
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

                if (allowRepair && TryRepairGhostManagedPopupEntry(entry, ref activeInHierarchy, queueBusy))
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

        private static bool HasVisibleManagedPopup(PopupManager manager, bool repairStaleState)
        {
            if (manager == null || manager.popups == null)
            {
                return false;
            }

            bool queueBusy = manager.queue != null && manager.queue.Count > ZeroPopupCount;
            bool allowRepair = repairStaleState && !queueBusy;
            for (int i = FirstCollectionIndex; i < manager.popups.Length; i++)
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

                if (allowRepair && TryRepairGhostManagedPopupEntry(entry, ref activeInHierarchy, queueBusy))
                {
                    entry.open = false;
                }

                if (!entry.open && !activeInHierarchy)
                {
                    continue;
                }

                if (!activeInHierarchy || entry.obj == null)
                {
                    continue;
                }

                CanvasGroup canvasGroup = entry.obj.GetComponent<CanvasGroup>();
                if (canvasGroup == null || canvasGroup.alpha > PopupGhostAlphaThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryRepairGhostManagedPopupEntry(PopupManager._popup entry, ref bool activeInHierarchy, bool queueBusy)
        {
            if (entry == null || entry.obj == null || !activeInHierarchy)
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
            if (popup.Increase_Popup_Counter && PopupManager.PopupCounter > ZeroPopupCount)
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
                PropertyInfo interpolationProperty = blurType.GetProperty(BlurInterpolationPropertyName, BindingFlags.Public | BindingFlags.Instance);
                if (interpolationProperty != null && interpolationProperty.CanWrite)
                {
                    interpolationProperty.SetValue(component, HiddenAlpha, null);
                }
            }
            catch
            {
            }
        }

        private static bool HasVisibleBackdrop(PopupManager manager)
        {
            if (manager == null)
            {
                return false;
            }

            bool visibleBg = manager.BG != null && manager.BG.activeSelf;
            bool visibleBgImage = manager.BGImage != null && manager.BGImage.activeSelf;
            if (visibleBg || visibleBgImage)
            {
                return true;
            }

            // SuperBlur component is from plugin, so detect by name to avoid hard dependency.
            if (Camera.main != null)
            {
                Type superBlurType = AccessTools.TypeByName(SuperBlurTypeName);
                Component superBlur = superBlurType != null ? Camera.main.GetComponent(superBlurType) : null;
                if (superBlur != null)
                {
                    PropertyInfo enabledProp = superBlur.GetType().GetProperty(EnabledPropertyName);
                    if (enabledProp != null)
                    {
                        object value = enabledProp.GetValue(superBlur, null);
                        if (value is bool && (bool)value)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool IsGraduationCalendarOpen()
        {
            GameObject popup = GameObject.Find(GraduationCalendarPopupObjectName);
            return popup != null && popup.activeInHierarchy;
        }

        private static PopupManager GetPopupManager()
        {
            if (Camera.main == null)
            {
                return null;
            }
            mainScript main = Camera.main.GetComponent<mainScript>();
            if (main == null || main.Data == null)
            {
                return null;
            }
            return main.Data.GetComponent<PopupManager>();
        }

        private static bool ShouldResumeTimeAfterPopupClose()
        {
            try
            {
                if (data_girls.new_girls != null && data_girls.new_girls.Count > ZeroPopupCount)
                {
                    return false;
                }
                if (Substories_Manager.IntroGirls != null && Substories_Manager.IntroGirls.Count > ZeroPopupCount)
                {
                    return false;
                }
            }
            catch
            {
            }
            return true;
        }

        private void Log(string message)
        {
            Debug.Log(LogPrefix + LogMessageSeparator + message);
        }

        private GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            obj.layer = gameObject.layer;
            return obj;
        }

        private Text CreateLegacyText(Transform parent, string name, string text, int fontSize, TextAnchor alignment, Color color)
        {
            EnsureFont();
            GameObject obj = CreateUIObject(name, parent);
            Text uiText = obj.AddComponent<Text>();
            uiText.text = text;
            uiText.fontSize = fontSize;
            uiText.alignment = alignment;
            uiText.color = color;
            uiText.raycastTarget = false;
            uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            if (defaultFont != null)
            {
                uiText.font = defaultFont;
            }
            return uiText;
        }

        private Button CreateButton(Transform parent, string name, string label, Action onClick)
        {
            GameObject obj = CreateUIObject(name, parent);
            Image image = obj.AddComponent<Image>();
            image.color = OverlayButtonColor;
            Button button = obj.AddComponent<Button>();
            if (onClick != null)
            {
                button.onClick.AddListener(delegate
                {
                    onClick();
                });
            }
            Text text = CreateLegacyText(obj.transform, OverlayButtonLabelObjectName, label, OverlayButtonFontSize, TextAnchor.MiddleCenter, Color.white);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            LayoutElement layout = obj.AddComponent<LayoutElement>();
            layout.preferredWidth = OverlayButtonPreferredWidth;
            layout.preferredHeight = OverlayButtonPreferredHeight;
            return button;
        }

        private void LoadConfig()
        {
            clearBlurKey = DefaultClearBlurKey;
            toggleOverlayKey = DefaultToggleOverlayKey;
            autoRecoverEnabled = DefaultAutoRecoverEnabled;
            autoRecoverIntervalSeconds = DefaultAutoRecoverIntervalSeconds;

            string path = GetConfigPath();
            try
            {
                EnsureConfigFile(path);
            }
            catch (Exception ex)
            {
                Log(ConfigWriteFailedLogPrefix + ex.Message);
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(path);
                for (int i = FirstCollectionIndex; i < lines.Length; i++)
                {
                    string raw = lines[i];
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        continue;
                    }

                    string line = raw.Trim();
                    if (line.StartsWith(ConfigCommentPrefixHash) ||
                        line.StartsWith(ConfigCommentPrefixSemicolon) ||
                        line.StartsWith(ConfigCommentPrefixSlashSlash))
                    {
                        continue;
                    }

                    int separator = line.IndexOf(ConfigKeyValueSeparator);
                    if (separator <= ZeroPopupCount || separator >= line.Length - NextIndexOffset)
                    {
                        continue;
                    }

                    string key = line.Substring(FirstCollectionIndex, separator).Trim().ToLowerInvariant();
                    string value = line.Substring(separator + NextIndexOffset).Trim();
                    if (key == ConfigKeyClearBlurKey)
                    {
                        clearBlurKey = ParseKeyCode(value, DefaultClearBlurKey);
                    }
                    else if (key == ConfigKeyToggleOverlayKey)
                    {
                        toggleOverlayKey = ParseKeyCode(value, DefaultToggleOverlayKey);
                    }
                    else if (key == ConfigKeyAutoRecoverEnabled)
                    {
                        autoRecoverEnabled = ParseBool(value, DefaultAutoRecoverEnabled);
                    }
                    else if (key == ConfigKeyAutoRecoverIntervalSeconds)
                    {
                        autoRecoverIntervalSeconds = ParseFloat(
                            value,
                            DefaultAutoRecoverIntervalSeconds,
                            MinAutoRecoverIntervalSeconds,
                            MaxAutoRecoverIntervalSeconds);
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ConfigReadFailedLogPrefix + ex.Message);
            }

            Log(string.Format(
                ConfigLoadedLogFormat,
                GetHotkeyLabel(clearBlurKey),
                GetHotkeyLabel(toggleOverlayKey),
                autoRecoverEnabled.ToString(),
                autoRecoverIntervalSeconds.ToString(IntervalLogValueFormat, CultureInfo.InvariantCulture)));
        }

        private static string GetConfigPath()
        {
            try
            {
                string dllPath = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(dllPath))
                {
                    string directory = Path.GetDirectoryName(dllPath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        return Path.Combine(directory, ConfigFileName);
                    }
                }
            }
            catch
            {
            }

            return Path.Combine(Application.dataPath, ConfigFileName);
        }

        private static void EnsureConfigFile(string path)
        {
            if (File.Exists(path))
            {
                return;
            }

            File.WriteAllLines(path, DefaultConfigTemplateLines);
        }

        private static string BuildConfigAssignmentLine(string key, string value)
        {
            return key + ConfigKeyValueSeparatorText + value;
        }

        private static KeyCode ParseKeyCode(string raw, KeyCode fallback)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return fallback;
            }

            string value = raw.Trim();
            if (string.Equals(value, KeyCodeDisabledValueNone, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, KeyCodeDisabledValueDisabled, StringComparison.OrdinalIgnoreCase))
            {
                return KeyCode.None;
            }

            KeyCode key;
            if (Enum.TryParse(value, true, out key))
            {
                return key;
            }

            return fallback;
        }

        private static bool ParseBool(string raw, bool fallback)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return fallback;
            }

            string value = raw.Trim();
            if (string.Equals(value, BoolTrueNumericValue, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolTrueYesValue, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolTrueShortValue, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(value, BoolFalseNumericValue, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolFalseNoValue, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolFalseShortValue, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            bool parsed;
            if (bool.TryParse(value, out parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private static float ParseFloat(string raw, float fallback, float min, float max)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return fallback;
            }

            float parsed;
            if (!float.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                return fallback;
            }

            return Mathf.Clamp(parsed, min, max);
        }

        private static string GetHotkeyLabel(KeyCode key)
        {
            return key == KeyCode.None ? ModLocalization.Get(DisabledLocalizationKey, DisabledLocalizationFallbackText) : key.ToString();
        }

        private void EnsureFont()
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
                        if (fonts != null)
                        {
                            Font font = fonts.GetFont();
                            if (font != null)
                            {
                                defaultFont = font;
                                return;
                            }
                        }
                    }
                }
                defaultFont = Resources.GetBuiltinResource<Font>(BuiltInFallbackFontName);
            }
            catch
            {
                defaultFont = Resources.GetBuiltinResource<Font>(BuiltInFallbackFontName);
            }
        }
    }
}
