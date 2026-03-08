using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IMUiFramework
{
    [HarmonyPatch(typeof(PopupManager), PatchMethodStart)]
    internal static class PopupManager_Start_Patch
    {
        private const string PatchMethodStart = "Start";

        private static void Postfix(PopupManager __instance)
        {
            IMUiKit.Initialize(__instance);
            if (IMUiFrameworkConfig.EnableBridgeShowcase)
            {
                IMUiBridgeRuntime.TryInitialize();
            }
            IMUiRuntimeRecovery.Ensure(__instance);
        }
    }

    [HarmonyPatch(typeof(PopupManager))]
    internal static class PopupManager_Close_Patch
    {
        private const string CloseMethodNamePascal = "Close";
        private const string CloseMethodNameLower = "close";

        private static MethodInfo cachedCloseMethod;

        private static bool Prepare()
        {
            return TryResolveTargetMethod(out MethodInfo ignored);
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
                    string.Equals(method.Name, CloseMethodNamePascal, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(method.Name, CloseMethodNameLower, StringComparison.OrdinalIgnoreCase);
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

                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(Action))
                {
                    actionCallbackCandidate = method;
                    continue;
                }

                if (parameters.Length == 0 && noArgsCandidate == null)
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
            IMUiRuntimeRecovery.NotifyPopupClosed();
            IMUiKit.OnPopupManagerClose(__instance);
            if (IMUiFrameworkConfig.EnableBridgeShowcase)
            {
                IMUiBridgeRuntime.OnPopupManagerClose();
            }
        }
    }

    [HarmonyPatch(typeof(Popup))]
    internal static class Popup_Hide_Patch
    {
        private const string HideMethodName = "Hide";

        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo[] methods = typeof(Popup).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method == null)
                {
                    continue;
                }

                if (!string.Equals(method.Name, HideMethodName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return method;
            }
        }

        private static void Postfix()
        {
            IMUiRuntimeRecovery.NotifyPopupHidden();
        }
    }

    internal sealed class IMUiRuntimeRecovery : MonoBehaviour
    {
        private const float RecoveryIntervalSeconds = 0.5f;
        private const float PopupHideReconcileDelaySeconds = 0.7f;
        private const float PopupCloseReconcileDelaySeconds = 0.2f;
        private const float StaleQueueRecoveryDelaySeconds = 1.2f;
        private const float InitialRecoveryDelaySeconds = 0.35f;
        private const float UninitializedStaleQueueTime = -1f;
        private const int UninitializedStaleQueueCount = -1;
        private const string BlurComponentTypePrimary = "SuperBlur";
        private const string BlurComponentTypeFallback = "SuperBlurFast";
        private const string BehaviourEnabledPropertyName = "enabled";
        private static bool pendingHideReconcile;
        private static float pendingHideReconcileAt;
        private static bool pendingCloseReconcile;
        private static float pendingCloseReconcileAt;
        private float nextRecoverAt;
        private float staleQueueSince = UninitializedStaleQueueTime;
        private int staleQueueCount = UninitializedStaleQueueCount;

        internal static void NotifyPopupHidden()
        {
            pendingHideReconcile = true;
            pendingHideReconcileAt = Time.unscaledTime + PopupHideReconcileDelaySeconds;
        }

        internal static void NotifyPopupClosed()
        {
            pendingCloseReconcile = true;
            pendingCloseReconcileAt = Time.unscaledTime + PopupCloseReconcileDelaySeconds;
        }

        internal static void Ensure(PopupManager manager)
        {
            if (manager == null)
            {
                return;
            }

            if (manager.GetComponent<IMUiRuntimeRecovery>() != null)
            {
                return;
            }

            manager.gameObject.AddComponent<IMUiRuntimeRecovery>();
        }

        private void OnEnable()
        {
            nextRecoverAt = Time.unscaledTime + InitialRecoveryDelaySeconds;
            staleQueueSince = UninitializedStaleQueueTime;
            staleQueueCount = UninitializedStaleQueueCount;
            pendingHideReconcile = false;
            pendingCloseReconcile = false;
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            bool runPendingHideReconcile = pendingHideReconcile && now >= pendingHideReconcileAt;
            bool runPendingCloseReconcile = pendingCloseReconcile && now >= pendingCloseReconcileAt;
            if (now < nextRecoverAt && !runPendingHideReconcile && !runPendingCloseReconcile)
            {
                return;
            }

            nextRecoverAt = now + RecoveryIntervalSeconds;
            if (runPendingHideReconcile)
            {
                pendingHideReconcile = false;
            }
            if (runPendingCloseReconcile)
            {
                pendingCloseReconcile = false;
            }

            if (ActiveDialogueController.ShowingDialogue)
            {
                return;
            }

            PopupManager manager;
            if (!IMUiKit.TryGetPopupManager(out manager) || manager == null)
            {
                return;
            }

            if (manager.queue != null && manager.queue.Count > 0)
            {
                if (!TryRecoverStaleQueue(manager, now))
                {
                    return;
                }
            }
            else
            {
                ResetStaleQueueTracking();
            }

            if (IMUiKit.HasManagedPopupOpenOrActiveUnsafe(manager, true))
            {
                return;
            }

            bool hasBackdrop = HasBackdropVisible(manager);
            if (!hasBackdrop && !runPendingHideReconcile && !runPendingCloseReconcile)
            {
                return;
            }

            try
            {
                IMUiKit.OnPopupManagerClose(manager);
                IMUiBridgeRuntime.OnPopupManagerClose();
            }
            catch
            {
            }
        }

        private bool TryRecoverStaleQueue(PopupManager manager, float now)
        {
            if (manager == null || manager.queue == null || manager.queue.Count == 0)
            {
                ResetStaleQueueTracking();
                return false;
            }

            if (IMUiKit.HasVisibleManagedPopupUnsafe(manager))
            {
                ResetStaleQueueTracking();
                return false;
            }

            int count = manager.queue.Count;
            if (staleQueueSince < 0f || staleQueueCount != count)
            {
                staleQueueSince = now;
                staleQueueCount = count;
                return false;
            }

            if (now - staleQueueSince < StaleQueueRecoveryDelaySeconds)
            {
                return false;
            }

            manager.queue.Clear();
            ResetStaleQueueTracking();
            return true;
        }

        private void ResetStaleQueueTracking()
        {
            staleQueueSince = UninitializedStaleQueueTime;
            staleQueueCount = UninitializedStaleQueueCount;
        }

        private static bool HasBackdropVisible(PopupManager manager)
        {
            if (manager != null)
            {
                if (manager.BG != null && manager.BG.activeSelf)
                {
                    return true;
                }

                if (manager.BGImage != null && manager.BGImage.activeSelf)
                {
                    return true;
                }
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                return false;
            }

            return IsBlurComponentEnabled(cam.gameObject, BlurComponentTypePrimary) ||
                   IsBlurComponentEnabled(cam.gameObject, BlurComponentTypeFallback);
        }

        private static bool IsBlurComponentEnabled(GameObject host, string typeName)
        {
            if (host == null || string.IsNullOrEmpty(typeName))
            {
                return false;
            }

            Type blurType = AccessTools.TypeByName(typeName);
            if (blurType == null)
            {
                return false;
            }

            Component component = host.GetComponent(blurType);
            if (component == null)
            {
                return false;
            }

            PropertyInfo enabledProperty = blurType.GetProperty(BehaviourEnabledPropertyName, BindingFlags.Public | BindingFlags.Instance);
            if (enabledProperty != null)
            {
                object value = enabledProperty.GetValue(component, null);
                if (value is bool)
                {
                    return (bool)value;
                }
            }

            Behaviour behaviour = component as Behaviour;
            return behaviour != null && behaviour.enabled;
        }
    }

    internal static class IMUiFrameworkConfig
    {
        private const string ConfigFileName = "IMUiFramework.config.ini";
        private const string ConfigEnableBridgeShowcaseKey = "enable_bridge_showcase";
        private const string ConfigLinePrefixHash = "#";
        private const string ConfigLinePrefixSemicolon = ";";
        private const string ConfigLinePrefixDoubleSlash = "//";
        private const string BoolTrueNumeric = "1";
        private const string BoolTrueWord = "true";
        private const string BoolTrueYes = "yes";
        private const string BoolTrueShort = "y";
        private const string BoolTrueOn = "on";
        private const string BoolFalseNumeric = "0";
        private const string BoolFalseWord = "false";
        private const string BoolFalseNo = "no";
        private const string BoolFalseShort = "n";
        private const string BoolFalseOff = "off";
        private static readonly string[] DefaultConfigLines = new[]
        {
            "# IMUiFramework runtime options",
            "# Set to true to inject the built-in 'UI Bridge' helper button and showcase popup.",
            "# Keep false for normal dependency/runtime usage.",
            "enable_bridge_showcase=false"
        };
        private const bool DefaultEnableBridgeShowcase = false;

        private static bool loaded;
        private static bool enableBridgeShowcase = DefaultEnableBridgeShowcase;

        internal static bool EnableBridgeShowcase
        {
            get
            {
                EnsureLoaded();
                return enableBridgeShowcase;
            }
        }

        private static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;
            enableBridgeShowcase = DefaultEnableBridgeShowcase;

            string path = GetConfigPath();
            try
            {
                EnsureConfigFile(path);
            }
            catch
            {
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string raw = lines[i];
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        continue;
                    }

                    string line = raw.Trim();
                    if (line.StartsWith(ConfigLinePrefixHash) ||
                        line.StartsWith(ConfigLinePrefixSemicolon) ||
                        line.StartsWith(ConfigLinePrefixDoubleSlash))
                    {
                        continue;
                    }

                    int separator = line.IndexOf('=');
                    if (separator <= 0 || separator >= line.Length - 1)
                    {
                        continue;
                    }

                    string key = line.Substring(0, separator).Trim().ToLowerInvariant();
                    string value = line.Substring(separator + 1).Trim();
                    if (key == ConfigEnableBridgeShowcaseKey)
                    {
                        enableBridgeShowcase = ParseBool(value, DefaultEnableBridgeShowcase);
                    }
                }
            }
            catch
            {
            }
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

            File.WriteAllLines(path, DefaultConfigLines);
        }

        private static bool ParseBool(string raw, bool fallback)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return fallback;
            }

            string value = raw.Trim();
            if (string.Equals(value, BoolTrueNumeric, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolTrueWord, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolTrueYes, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolTrueShort, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolTrueOn, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(value, BoolFalseNumeric, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolFalseWord, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolFalseNo, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolFalseShort, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolFalseOff, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return fallback;
        }
    }

    internal static class IMUiBridgeRuntime
    {
        private const string BridgeButtonName = "IMUiFramework_BridgeButton";
        private const string BridgePopupName = "IMUiFramework_BridgePopup";
        private const string BridgeButtonLabelLocalizationKey = "bridge.button_label";
        private const string BridgeButtonLabelFallback = "UI Bridge";
        private const string BridgeButtonTooltipLocalizationKey = "bridge.button_tooltip";
        private const string BridgeButtonTooltipFallback = "Open UI bridge/helper showcase";
        private const string BridgePopupTitleLocalizationKey = "bridge.popup_title";
        private const string BridgePopupTitleFallback = "UI Bridge Helpers";
        private const float BridgePopupWidth = 940f;
        private const float BridgePopupHeight = 610f;
        private static readonly string BridgeButtonLabel = ModLocalization.Get(BridgeButtonLabelLocalizationKey, BridgeButtonLabelFallback);
        private static readonly string BridgeButtonTooltip = ModLocalization.Get(BridgeButtonTooltipLocalizationKey, BridgeButtonTooltipFallback);

        private static bool creating;
        private static GameObject bridgeButton;
        private static PopupScaffold bridgeScaffold;

        public static void TryInitialize()
        {
            if (creating)
            {
                return;
            }

            mainScript main;
            if (!IMUiKit.TryGetMain(out main) || main == null || !main.IsGameScene)
            {
                return;
            }

            creating = true;
            try
            {
                if (bridgeButton != null && bridgeButton.transform.parent == null)
                {
                    bridgeButton = null;
                }

                if (bridgeScaffold != null && !bridgeScaffold.IsValid)
                {
                    bridgeScaffold = null;
                }

                if (bridgeButton == null)
                {
                    IMUiKit.TryAddTopMenuButton(
                        BridgeButtonName,
                        BridgeButtonLabel,
                        BridgeButtonTooltip,
                        ToggleBridgePopup,
                        out bridgeButton);
                }

                if (bridgeScaffold == null)
                {
                    PopupScaffold scaffold;
                    GameObject showcaseRoot;
                    if (IMUiBridges.TryCreateBridgeShowcasePopup(
                        BridgePopupName,
                        ModLocalization.Get(BridgePopupTitleLocalizationKey, BridgePopupTitleFallback),
                        new Vector2(BridgePopupWidth, BridgePopupHeight),
                        Camera.main,
                        out scaffold,
                        out showcaseRoot))
                    {
                        bridgeScaffold = scaffold;
                        if (bridgeScaffold.Root != null)
                        {
                            bridgeScaffold.Root.SetActive(false);
                        }
                    }
                }
            }
            finally
            {
                creating = false;
            }
        }

        public static void OnPopupManagerClose()
        {
            if (bridgeScaffold == null)
            {
                return;
            }

            if (bridgeScaffold.Root == null)
            {
                bridgeScaffold = null;
            }
        }

        private static void ToggleBridgePopup()
        {
            if (bridgeScaffold == null || !bridgeScaffold.IsValid)
            {
                TryInitialize();
            }

            if (bridgeScaffold == null || !bridgeScaffold.IsValid)
            {
                return;
            }

            if (bridgeScaffold.Root.activeSelf)
            {
                bridgeScaffold.Hide();
            }
            else
            {
                bridgeScaffold.Show();
            }
        }
    }

    internal sealed class IMUiFrameworkMarker : MonoBehaviour
    {
    }

    public sealed class PopupScaffold
    {
        public GameObject Root;
        public Popup Popup;
        public RectTransform PanelRect;
        public TextMeshProUGUI TitleText;
        public Transform ContentRoot;
        public ScrollRect ScrollRect;
        public Button CloseButton;

        public bool IsValid
        {
            get { return Root != null && Popup != null && ContentRoot != null; }
        }

        public void Show()
        {
            if (Root != null)
            {
                Root.SetActive(true);
            }
        }

        public void Hide(Action onComplete = null)
        {
            if (Popup != null)
            {
                Popup.Hide(onComplete);
                return;
            }

            if (Root != null)
            {
                Root.SetActive(false);
            }

            if (onComplete != null)
            {
                onComplete();
            }
        }
    }

    public sealed class StagedVariablesState
    {
        private const string DefaultFloatFormat = "0.###";
        private const string BoolTrueNumeric = "1";
        private const string BoolFalseNumeric = "0";
        private const string BoolTrueWord = "true";
        private const string BoolFalseWord = "false";
        private const string BoolTrueYes = "yes";
        private const string BoolFalseNo = "no";
        private const string BoolTrueOn = "on";
        private const string BoolFalseOff = "off";
        private const string EmptyValue = "";
        private readonly Dictionary<string, string> stagedValues = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> baselineValues = new Dictionary<string, string>(StringComparer.Ordinal);

        public int PendingCount
        {
            get { return stagedValues.Count; }
        }

        public bool HasPendingChanges
        {
            get { return stagedValues.Count > 0; }
        }

        public void StageString(string variableId, string value)
        {
            if (string.IsNullOrEmpty(variableId))
            {
                return;
            }

            EnsureTracked(variableId);
            string safe = value ?? string.Empty;
            string baseline;
            baselineValues.TryGetValue(variableId, out baseline);
            if (string.Equals(safe, baseline ?? string.Empty, StringComparison.Ordinal))
            {
                stagedValues.Remove(variableId);
                return;
            }

            stagedValues[variableId] = safe;
        }

        public void StageInt(string variableId, int value)
        {
            StageString(variableId, value.ToString(CultureInfo.InvariantCulture));
        }

        public void StageFloat(string variableId, float value, string format = DefaultFloatFormat)
        {
            string safeFormat = string.IsNullOrEmpty(format) ? DefaultFloatFormat : format;
            StageString(variableId, value.ToString(safeFormat, CultureInfo.InvariantCulture));
        }

        public void StageBool(string variableId, bool value, bool oneZero = true)
        {
            StageString(variableId, oneZero
                ? (value ? BoolTrueNumeric : BoolFalseNumeric)
                : (value ? BoolTrueWord : BoolFalseWord));
        }

        public bool TryGetStagedString(string variableId, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(variableId))
            {
                return false;
            }

            return stagedValues.TryGetValue(variableId, out value);
        }

        public string GetCurrentString(string variableId, string fallback = EmptyValue)
        {
            if (string.IsNullOrEmpty(variableId))
            {
                return fallback ?? string.Empty;
            }

            string staged;
            if (stagedValues.TryGetValue(variableId, out staged))
            {
                return staged ?? string.Empty;
            }

            EnsureTracked(variableId);
            string baseline;
            if (!baselineValues.TryGetValue(variableId, out baseline))
            {
                return fallback ?? string.Empty;
            }

            if (!string.IsNullOrEmpty(baseline))
            {
                return baseline;
            }

            return fallback ?? string.Empty;
        }

        public int GetCurrentInt(string variableId, int fallback = 0)
        {
            int parsed;
            return int.TryParse(GetCurrentString(variableId, fallback.ToString(CultureInfo.InvariantCulture)), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                ? parsed
                : fallback;
        }

        public float GetCurrentFloat(string variableId, float fallback = 0f)
        {
            string raw = GetCurrentString(variableId, fallback.ToString(CultureInfo.InvariantCulture));
            float parsed;
            return TryParseFloat(raw, out parsed) ? parsed : fallback;
        }

        public bool GetCurrentBool(string variableId, bool fallback = false)
        {
            return ParseBool(GetCurrentString(variableId, fallback ? BoolTrueNumeric : BoolFalseNumeric), fallback);
        }

        public void Reset()
        {
            stagedValues.Clear();
            baselineValues.Clear();
        }

        public int Apply()
        {
            int applied = 0;
            foreach (KeyValuePair<string, string> kv in stagedValues)
            {
                if (string.IsNullOrEmpty(kv.Key))
                {
                    continue;
                }

                try
                {
                    variables.Set(kv.Key, kv.Value ?? string.Empty);
                    applied++;
                }
                catch
                {
                }
            }

            Reset();
            return applied;
        }

        private void EnsureTracked(string variableId)
        {
            if (string.IsNullOrEmpty(variableId) || baselineValues.ContainsKey(variableId))
            {
                return;
            }

            baselineValues[variableId] = ReadVariable(variableId);
        }

        private static string ReadVariable(string variableId)
        {
            if (string.IsNullOrEmpty(variableId))
            {
                return EmptyValue;
            }

            try
            {
                string value = variables.Get(variableId);
                return value ?? EmptyValue;
            }
            catch
            {
                return EmptyValue;
            }
        }

        private static bool TryParseFloat(string raw, out float value)
        {
            value = 0f;
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                return true;
            }

            return float.TryParse(raw, out value);
        }

        private static bool ParseBool(string raw, bool fallback)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return fallback;
            }

            string value = raw.Trim();
            if (string.Equals(value, BoolTrueNumeric, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolTrueWord, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolTrueYes, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolTrueOn, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(value, BoolFalseNumeric, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolFalseWord, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolFalseNo, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolFalseOff, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            int intValue;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue))
            {
                return intValue != 0;
            }

            float floatValue;
            if (TryParseFloat(value, out floatValue))
            {
                return Mathf.Abs(floatValue) > float.Epsilon;
            }

            return fallback;
        }
    }

    public static class IMUiKit
    {
        private const string LogPrefix = "[IMUiFramework]";
        private const string RuntimeInitializedLogMessage = "Initialized.";
        private const string DefaultAwardsLabel = "Awards";
        private const string AwardsLocalizationKey = "AWARDS";
        private const string AwardsNameToken = "award";
        private const string PopupCloseLocalizationKey = "POPUP__CLOSE";
        private const string CloseFallbackLocalizationKey = "common.close";
        private const string CloseFallbackText = "Close";
        private const string BlurComponentTypePrimary = "SuperBlur";
        private const string BlurComponentTypeFallback = "SuperBlurFast";
        private const string BlurInterpolationPropertyName = "interpolation";
        private const string LogMessageAddTopMenuButtonMissingAwards = "TryAddTopMenuButton failed: awards button not found.";
        private const string LogMessageCreatePopupScaffoldMissingParent = "TryCreatePopupScaffold failed: popup parent not found.";
        private const string LogMessageCreateSliderMissingTemplate = "TryCreateSettingsSlider failed: no Settings_Slider template found.";
        private const string LogMessageCreateCheckboxMissingTemplate = "TryCreateSettingsCheckbox failed: no Checkbox_Text template found.";
        private const string TopMenuButtonDefaultObjectName = "IMUiFramework_Button";
        private const string PopupDefaultObjectName = "IMUiFramework_Popup";
        private const string PopupPanelObjectName = "Panel";
        private const string PopupTitleObjectName = "Title";
        private const string PopupDefaultTitleText = "Custom Popup";
        private const string SliderDefaultObjectPrefix = "IMUiFramework_Slider_";
        private const string CheckboxDefaultObjectPrefix = "IMUiFramework_Checkbox_";
        private const string ButtonDefaultObjectName = "Button";
        private const string ButtonTextObjectName = "Text";
        private const string ProfileExtraDefaultObjectName = "IMUiFramework_Extra";
        private const string VerticalContainerDefaultObjectName = "VerticalContainer";
        private const string HorizontalContainerDefaultObjectName = "HorizontalContainer";
        private const string DividerDefaultObjectName = "Divider";
        private const string ProfileTextDefaultObjectName = "ProfileText";
        private const string ProfileDividerDefaultObjectName = "ProfileDivider";
        private const string ScrollViewObjectName = "ScrollView";
        private const string ScrollViewportObjectName = "Viewport";
        private const string ScrollContentObjectName = "Content";
        private const string ScrollbarObjectName = "Scrollbar";
        private const string ScrollbarHandleObjectName = "Handle";
        private const string CloseButtonObjectName = "Close";
        private const string LabelValueSeparator = ": ";
        private const string SpaceSeparator = " ";
        private const string EmptyText = "";
        private const string BoolTrueNumeric = "1";
        private const string BoolFalseNumeric = "0";
        private const string BoolTrueWord = "true";
        private const string BoolFalseWord = "false";
        private const string BoolTrueYes = "yes";
        private const string BoolFalseNo = "no";
        private const string BoolTrueOn = "on";
        private const string BoolFalseOff = "off";
        private const string FloatValueFormat = "0.###";
        private const float BackdropSyncDuration = 0.1f;
        private const float PopupGhostAlphaThreshold = 0.001f;
        private const float DefaultButtonWidth = 180f;
        private const float DefaultButtonHeight = 36f;
        private const float DefaultLayoutSpacing = 6f;
        private const int DefaultButtonFontSize = 20;
        private const float DefaultCloseButtonWidth = 150f;
        private const float DefaultCloseButtonHeight = 36f;
        private const float DefaultPopupWidth = 860f;
        private const float DefaultPopupHeight = 520f;
        private const int DefaultPopupTitleFontSize = 34;
        private const float DefaultPopupTitleHorizontalPadding = 40f;
        private const float DefaultPopupTitleHeight = 40f;
        private const float DefaultPopupTitleTopOffset = -16f;
        private const float DefaultPopupScrollOffsetMinX = 16f;
        private const float DefaultPopupScrollOffsetMinY = 56f;
        private const float DefaultPopupScrollOffsetMaxX = -32f;
        private const float DefaultPopupScrollOffsetMaxY = -90f;
        private const float DefaultCloseButtonBottomOffset = 12f;
        private const float DefaultScrollSensitivity = 30f;
        private const int DefaultScrollContentPadding = 4;
        private const float DefaultScrollbarWidth = 12f;
        private const float DefaultScrollbarRightOffset = -4f;
        private const float DefaultScrollbarSpacing = 6f;
        private const float DefaultMinButtonMeasureWidth = 900f;
        private const float DefaultMeasuredButtonHorizontalPadding = 64f;
        private const float DefaultTextCharWidthFallback = 9f;
        private const float DefaultPreferredTextMinWidth = 120f;
        private const float DefaultPreferredTextMaxWidth = 560f;
        private const float DefaultPreferredTextHorizontalPadding = 48f;
        private const int DefaultProfileTextFontSize = 18;
        private const float DefaultDividerHeight = 2f;
        private const float MinColorChannelForTitle = 20f;
        private const float LumaCoefficientRed = 0.2126f;
        private const float LumaCoefficientGreen = 0.7152f;
        private const float LumaCoefficientBlue = 0.0722f;

        private static bool initialized;
        private static bool triedThemeCapture;

        private static TMP_FontAsset defaultTmpFont;
        private static Font defaultLegacyFont;
        private static GameObject defaultButtonTemplate;
        private static Scrollbar defaultScrollbarTemplate;
        private static Settings_Slider defaultSettingsSliderTemplate;
        private static Checkbox_Text defaultCheckboxTemplate;

        private static Color32 panelOuter = new Color32(235, 234, 233, 255);
        private static Color32 panelInner = new Color32(254, 254, 254, 255);
        private static Color32 buttonBackground = new Color32(241, 122, 170, 255);
        private static Color32 buttonTextColor = new Color32(255, 255, 255, 255);
        private static Color32 titleColor = new Color32(61, 42, 80, 255);
        private static readonly Color32 ScrollbarTrackColor = new Color32(210, 210, 210, 190);
        private static readonly Vector2 AnchorBottomLeft = new Vector2(0f, 0f);
        private static readonly Vector2 AnchorTopRight = new Vector2(1f, 1f);
        private static readonly Vector2 AnchorCentered = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 AnchorTopCenter = new Vector2(0.5f, 1f);
        private static readonly Vector2 AnchorBottomRight = new Vector2(1f, 0f);

        public static bool IsInitialized
        {
            get { return initialized; }
        }

        public static void Initialize(PopupManager manager)
        {
            if (manager == null)
            {
                return;
            }

            if (initialized)
            {
                TryCaptureTheme();
                return;
            }

            initialized = true;
            TryCaptureTheme();
            Log(RuntimeInitializedLogMessage);
        }

        public static void OnPopupManagerClose()
        {
            if (!initialized)
            {
                return;
            }

            if (ActiveDialogueController.ShowingDialogue)
            {
                return;
            }

            PopupManager manager;
            if (!TryGetPopupManager(out manager))
            {
                return;
            }

            OnPopupManagerClose(manager);
        }

        public static void OnPopupManagerClose(PopupManager manager)
        {
            if (!initialized || manager == null)
            {
                return;
            }

            if (ActiveDialogueController.ShowingDialogue)
            {
                return;
            }

            if (manager.queue != null && manager.queue.Count > 0)
            {
                TryRepairStaleQueue(manager);
                if (manager.queue != null && manager.queue.Count > 0)
                {
                    return;
                }
            }

            if (TrySyncBackdropWithActiveManagedPopups(manager))
            {
                return;
            }

            TryRunPopupBackdropSafetyNet(manager, true, true);
        }

        public static bool TrySyncBackdropWithActiveManagedPopups(PopupManager manager)
        {
            bool hasActive;
            bool requiresBlur;
            bool requiresDarken;
            RenderTexture activeRenderTexture;
            GetManagedPopupBackdropState(manager, out hasActive, out requiresBlur, out requiresDarken, out activeRenderTexture);
            if (!hasActive)
            {
                return false;
            }

            ApplyBackdropState(manager, requiresBlur, requiresDarken, activeRenderTexture);
            return true;
        }

        public static bool TryRunPopupBackdropSafetyNet(
            PopupManager manager,
            bool resetPopupCounter = true,
            bool resumeTime = true)
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

            if (HasManagedPopupOpenOrActive(manager, true))
            {
                return false;
            }

            ClearBackdropState(manager, resetPopupCounter);

            if (resumeTime && ShouldResumeTimeAfterPopupClose())
            {
                mainScript main = Camera.main != null ? Camera.main.GetComponent<mainScript>() : null;
                if (main != null)
                {
                    main.Time_Resume();
                }
            }

            return true;
        }

        private static void GetManagedPopupBackdropState(
            PopupManager manager,
            out bool hasActive,
            out bool requiresBlur,
            out bool requiresDarken,
            out RenderTexture renderTexture)
        {
            hasActive = false;
            requiresBlur = false;
            requiresDarken = false;
            renderTexture = null;

            if (manager == null || manager.popups == null)
            {
                return;
            }

            bool queueBusy = manager.queue != null && manager.queue.Count > 0;
            bool allowRepair = !queueBusy;
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

                if (renderTexture == null && entry.BGRenderTexture != null)
                {
                    renderTexture = entry.BGRenderTexture;
                }
            }
        }

        private static void ApplyBackdropState(
            PopupManager manager,
            bool requiresBlur,
            bool requiresDarken,
            RenderTexture renderTexture)
        {
            if (manager == null)
            {
                return;
            }

            try
            {
                manager.BGBlur(requiresBlur, BackdropSyncDuration);
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
                        bgGroup.alpha = 1f;
                    }
                }
                else
                {
                    if (bgGroup != null)
                    {
                        bgGroup.alpha = 0f;
                    }

                    manager.BG.SetActive(false);
                }
            }

            if (manager.BGImage != null)
            {
                CanvasGroup bgImageGroup = manager.BGImage.GetComponent<CanvasGroup>();
                RawImage bgImage = manager.BGImage.GetComponent<RawImage>();
                if (renderTexture != null)
                {
                    manager.BGImage.SetActive(true);
                    if (bgImage != null)
                    {
                        bgImage.texture = renderTexture;
                    }

                    if (bgImageGroup != null)
                    {
                        bgImageGroup.alpha = 1f;
                    }
                }
                else
                {
                    if (bgImageGroup != null)
                    {
                        bgImageGroup.alpha = 0f;
                    }

                    manager.BGImage.SetActive(false);
                }
            }

            manager.BlockInput(true);
        }

        private static void ClearBackdropState(PopupManager manager, bool resetPopupCounter = true)
        {
            if (manager == null)
            {
                return;
            }

            try
            {
                manager.BGBlur(false, BackdropSyncDuration);
            }
            catch
            {
            }

            ForceDisableCameraBlur(manager);

            if (manager.BG != null)
            {
                CanvasGroup bgGroup = manager.BG.GetComponent<CanvasGroup>();
                if (bgGroup != null)
                {
                    bgGroup.alpha = 0f;
                }

                manager.BG.SetActive(false);
            }

            if (manager.BGImage != null)
            {
                CanvasGroup bgImageGroup = manager.BGImage.GetComponent<CanvasGroup>();
                if (bgImageGroup != null)
                {
                    bgImageGroup.alpha = 0f;
                }

                manager.BGImage.SetActive(false);
            }

            manager.BlockInput(false);
            if (resetPopupCounter && PopupManager.PopupCounter > 0)
            {
                PopupManager.PopupCounter = 0;
            }
        }

        private static void TryRepairStaleQueue(PopupManager manager)
        {
            if (manager == null || manager.queue == null || manager.queue.Count == 0)
            {
                return;
            }

            if (HasVisibleManagedPopup(manager))
            {
                return;
            }

            manager.queue.Clear();
        }

        internal static bool HasManagedPopupOpenOrActiveUnsafe(PopupManager manager, bool repairStaleState)
        {
            return HasManagedPopupOpenOrActive(manager, repairStaleState);
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

        internal static bool HasVisibleManagedPopupUnsafe(PopupManager manager)
        {
            return HasVisibleManagedPopup(manager);
        }

        private static bool HasVisibleManagedPopup(PopupManager manager)
        {
            if (manager == null || manager.popups == null)
            {
                return false;
            }

            bool queueBusy = manager.queue != null && manager.queue.Count > 0;
            bool allowRepair = !queueBusy;
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

            DisableBlurComponent(targetCamera.gameObject, BlurComponentTypePrimary);
            DisableBlurComponent(targetCamera.gameObject, BlurComponentTypeFallback);
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
                    interpolationProperty.SetValue(component, 0f, null);
                }
            }
            catch
            {
            }
        }

        public static bool TryGetMain(out mainScript main)
        {
            main = null;
            if (Camera.main == null)
            {
                return false;
            }

            main = Camera.main.GetComponent<mainScript>();
            return main != null;
        }

        public static bool TryGetPopupManager(out PopupManager manager)
        {
            manager = null;
            mainScript main;
            if (!TryGetMain(out main) || main.Data == null)
            {
                return false;
            }

            manager = main.Data.GetComponent<PopupManager>();
            return manager != null;
        }

        public static Transform GetPopupParent()
        {
            PopupManager manager;
            if (!TryGetPopupManager(out manager))
            {
                return null;
            }

            try
            {
                GameObject awardsPopup = PopupManager.GetObject(PopupManager._type.awards);
                if (awardsPopup != null && awardsPopup.transform.parent != null)
                {
                    return awardsPopup.transform.parent;
                }
            }
            catch
            {
            }

            if (manager.popups == null)
            {
                return null;
            }

            for (int i = 0; i < manager.popups.Length; i++)
            {
                PopupManager._popup entry = manager.popups[i];
                if (entry != null && entry.obj != null && entry.obj.transform.parent != null)
                {
                    return entry.obj.transform.parent;
                }
            }

            return null;
        }

        public static bool TryAddTopMenuButton(
            string objectName,
            string label,
            string tooltip,
            UnityAction onClick,
            out GameObject createdButton)
        {
            createdButton = null;
            TryCaptureTheme();

            GameObject awardsButton = FindAwardsButton();
            if (awardsButton == null || awardsButton.transform.parent == null)
            {
                Log(LogMessageAddTopMenuButtonMissingAwards);
                return false;
            }

            GameObject clone = UnityEngine.Object.Instantiate(awardsButton, awardsButton.transform.parent, false);
            clone.name = string.IsNullOrEmpty(objectName) ? TopMenuButtonDefaultObjectName : objectName;
            clone.AddComponent<IMUiFrameworkMarker>();
            clone.transform.SetSiblingIndex(awardsButton.transform.GetSiblingIndex());

            ClearLocalizationComponents(clone);
            RebindButtonClick(clone, onClick);
            SetButtonLabel(clone, label);
            SetTooltip(clone, tooltip);
            EnsureButtonDefaultState(clone);

            createdButton = clone;
            return true;
        }

        public static bool TryCreatePopupScaffold(
            string popupName,
            string title,
            Vector2 panelSize,
            out PopupScaffold scaffold)
        {
            scaffold = null;
            TryCaptureTheme();

            Transform parent = GetPopupParent();
            if (parent == null)
            {
                Log(LogMessageCreatePopupScaffoldMissingParent);
                return false;
            }

            GameObject root = new GameObject(
                string.IsNullOrEmpty(popupName) ? PopupDefaultObjectName : popupName,
                typeof(RectTransform),
                typeof(CanvasGroup));

            root.transform.SetParent(parent, false);
            SetLayerRecursively(root, parent.gameObject.layer);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            CanvasGroup rootGroup = root.GetComponent<CanvasGroup>();
            rootGroup.alpha = 0f;
            rootGroup.blocksRaycasts = true;
            rootGroup.interactable = true;
            root.SetActive(false);

            Popup popup = root.AddComponent<Popup>();
            popup.ShowAnimation = true;
            popup.HideAnimation = true;
            popup.HideFast = false;
            popup.Increase_Popup_Counter = true;

            GameObject panel = CreateUiObject(PopupPanelObjectName, root.transform);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = panelOuter;

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = AnchorCentered;
            panelRect.anchorMax = AnchorCentered;
            panelRect.pivot = AnchorCentered;
            panelRect.sizeDelta = panelSize.x > 0f && panelSize.y > 0f ? panelSize : new Vector2(DefaultPopupWidth, DefaultPopupHeight);

            TextMeshProUGUI titleText = CreateText(
                panel.transform,
                PopupTitleObjectName,
                string.IsNullOrEmpty(title) ? PopupDefaultTitleText : title,
                DefaultPopupTitleFontSize,
                TextAlignmentOptions.Center,
                titleColor);

            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = AnchorTopCenter;
            titleRect.anchorMax = AnchorTopCenter;
            titleRect.pivot = AnchorTopCenter;
            titleRect.sizeDelta = new Vector2(panelRect.sizeDelta.x - DefaultPopupTitleHorizontalPadding, DefaultPopupTitleHeight);
            titleRect.anchoredPosition = new Vector2(0f, DefaultPopupTitleTopOffset);

            ScrollRect scrollRect;
            Transform contentRoot;
            CreateStyledScrollView(
                panel.transform,
                new Vector2(DefaultPopupScrollOffsetMinX, DefaultPopupScrollOffsetMinY),
                new Vector2(DefaultPopupScrollOffsetMaxX, DefaultPopupScrollOffsetMaxY),
                out scrollRect,
                out contentRoot);

            Button closeButton = CreateBottomCloseButton(panel.transform, popup);

            scaffold = new PopupScaffold
            {
                Root = root,
                Popup = popup,
                PanelRect = panelRect,
                TitleText = titleText,
                ContentRoot = contentRoot,
                ScrollRect = scrollRect,
                CloseButton = closeButton
            };

            return true;
        }

        public static bool TryRegisterPopup(
            PopupManager._type type,
            GameObject popupRoot,
            bool blurBackground,
            bool darkenBackground)
        {
            if (popupRoot == null)
            {
                return false;
            }

            PopupManager manager;
            if (!TryGetPopupManager(out manager))
            {
                return false;
            }

            PopupManager._popup existing = manager.GetByType(type);
            if (existing != null)
            {
                existing.obj = popupRoot;
                existing.BGBlur = blurBackground;
                existing.BGDarken = darkenBackground;
                return true;
            }

            PopupManager._popup newPopup = new PopupManager._popup
            {
                type = type,
                obj = popupRoot,
                BGBlur = blurBackground,
                BGDarken = darkenBackground
            };

            Array.Resize(ref manager.popups, manager.popups.Length + 1);
            manager.popups[manager.popups.Length - 1] = newPopup;
            return true;
        }

        public static StagedVariablesState CreateStagedVariablesState()
        {
            return new StagedVariablesState();
        }

        public static bool BindStagedApplyCancelButtons(
            StagedVariablesState stagedState,
            Button applyButton,
            Button cancelButton,
            Action onApplied = null,
            Action onCanceled = null,
            Popup popupToHide = null,
            bool replaceExistingListeners = true)
        {
            if (stagedState == null)
            {
                return false;
            }

            bool bound = false;
            if (applyButton != null)
            {
                if (replaceExistingListeners)
                {
                    applyButton.onClick = new Button.ButtonClickedEvent();
                }

                applyButton.onClick.AddListener(delegate
                {
                    stagedState.Apply();
                    if (onApplied != null)
                    {
                        onApplied();
                    }

                    if (popupToHide != null)
                    {
                        popupToHide.Hide(null);
                    }
                });

                bound = true;
            }

            if (cancelButton != null)
            {
                if (replaceExistingListeners)
                {
                    cancelButton.onClick = new Button.ButtonClickedEvent();
                }

                cancelButton.onClick.AddListener(delegate
                {
                    stagedState.Reset();
                    if (onCanceled != null)
                    {
                        onCanceled();
                    }

                    if (popupToHide != null)
                    {
                        popupToHide.Hide(null);
                    }
                });

                bound = true;
            }

            return bound;
        }

        public static string ResolveLanguageDataText(string languageKeyOrText, string fallback = null)
        {
            return ResolveLanguageDataTextInternal(languageKeyOrText, fallback);
        }

        public static void BindLanguageData(
            GameObject root,
            string languageKey,
            string fallbackText = null,
            string tooltipLanguageKey = null,
            string tooltipFallbackText = null)
        {
            if (root == null)
            {
                return;
            }

            string text = ResolveLanguageDataTextInternal(languageKey, fallbackText);
            string tooltip = ResolveLanguageDataTextInternal(tooltipLanguageKey, tooltipFallbackText);

            Lang_Button[] langButtons = root.GetComponentsInChildren<Lang_Button>(true);
            for (int i = 0; i < langButtons.Length; i++)
            {
                Lang_Button lang = langButtons[i];
                if (lang == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(languageKey))
                {
                    lang.Constant = languageKey;
                }

                if (!string.IsNullOrEmpty(tooltipLanguageKey))
                {
                    lang.Tooltip = tooltipLanguageKey;
                }
            }

            TextMeshProUGUI[] tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < tmps.Length; i++)
            {
                TextMeshProUGUI tmp = tmps[i];
                if (tmp != null && !string.IsNullOrEmpty(text))
                {
                    tmp.text = text;
                }
            }

            Text[] legacyTexts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < legacyTexts.Length; i++)
            {
                Text legacy = legacyTexts[i];
                if (legacy != null && !string.IsNullOrEmpty(text))
                {
                    legacy.text = text;
                }
            }

            if (!string.IsNullOrEmpty(tooltip))
            {
                ButtonDefault[] defaults = root.GetComponentsInChildren<ButtonDefault>(true);
                for (int i = 0; i < defaults.Length; i++)
                {
                    ButtonDefault bd = defaults[i];
                    if (bd == null)
                    {
                        continue;
                    }

                    bd.DefaultTooltip = tooltip;
                    bd.SetTooltip(tooltip);
                }
            }
        }

        public static bool TryCreateSettingsSlider(
            Transform parent,
            string objectName,
            string variableId,
            string labelId,
            float minValue,
            float maxValue,
            float defaultValue,
            StagedVariablesState stagedState,
            out GameObject sliderObject,
            bool roundToWholeNumber = true)
        {
            sliderObject = null;
            if (parent == null || string.IsNullOrEmpty(variableId))
            {
                return false;
            }

            float min = minValue;
            float max = maxValue;
            if (max < min)
            {
                float temp = min;
                min = max;
                max = temp;
            }

            float safeDefault = Mathf.Clamp(defaultValue, min, max);
            Settings_Slider template;
            if (!TryFindSettingsSliderTemplate(out template) || template == null)
            {
                Log(LogMessageCreateSliderMissingTemplate);
                return false;
            }

            sliderObject = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
            sliderObject.name = string.IsNullOrEmpty(objectName) ? (SliderDefaultObjectPrefix + variableId) : objectName;
            sliderObject.SetActive(true);
            SetLayerRecursively(sliderObject, parent.gameObject.layer);

            Settings_Slider settingsSlider = sliderObject.GetComponent<Settings_Slider>();
            if (settingsSlider == null)
            {
                settingsSlider = sliderObject.GetComponentInChildren<Settings_Slider>(true);
            }

            if (settingsSlider == null)
            {
                UnityEngine.Object.Destroy(sliderObject);
                sliderObject = null;
                return false;
            }

            StagedSliderBinding binding = AddOrGetComponent<StagedSliderBinding>(sliderObject);
            binding.Setup(stagedState, variableId, labelId, min, max, safeDefault, roundToWholeNumber, settingsSlider);
            return true;
        }

        public static bool TryCreateSettingsCheckbox(
            Transform parent,
            string objectName,
            string variableId,
            string labelId,
            bool defaultValue,
            StagedVariablesState stagedState,
            out GameObject checkboxObject)
        {
            checkboxObject = null;
            if (parent == null || string.IsNullOrEmpty(variableId))
            {
                return false;
            }

            Checkbox_Text template;
            if (!TryFindCheckboxTemplate(out template) || template == null)
            {
                Log(LogMessageCreateCheckboxMissingTemplate);
                return false;
            }

            checkboxObject = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
            checkboxObject.name = string.IsNullOrEmpty(objectName) ? (CheckboxDefaultObjectPrefix + variableId) : objectName;
            checkboxObject.SetActive(true);
            SetLayerRecursively(checkboxObject, parent.gameObject.layer);

            Checkbox_Text checkbox = checkboxObject.GetComponent<Checkbox_Text>();
            if (checkbox == null)
            {
                checkbox = checkboxObject.GetComponentInChildren<Checkbox_Text>(true);
            }

            if (checkbox == null)
            {
                UnityEngine.Object.Destroy(checkboxObject);
                checkboxObject = null;
                return false;
            }

            StagedCheckboxBinding binding = AddOrGetComponent<StagedCheckboxBinding>(checkboxObject);
            binding.Setup(stagedState, variableId, labelId, defaultValue, checkbox);
            return true;
        }

        public static Button CloneStyledButton(
            GameObject template,
            Transform parent,
            string name,
            string label,
            string tooltip,
            UnityAction onClick,
            float width = 0f,
            float height = 0f)
        {
            if (template == null)
            {
                return CreateStyledButton(
                    parent,
                    name,
                    label,
                    width <= 0f ? DefaultButtonWidth : width,
                    height <= 0f ? DefaultButtonHeight : height,
                    onClick);
            }

            GameObject obj = UnityEngine.Object.Instantiate(template, parent, false);
            obj.name = string.IsNullOrEmpty(name) ? ButtonDefaultObjectName : name;
            obj.SetActive(true);
            SetLayerRecursively(obj, parent.gameObject.layer);

            ClearLocalizationComponents(obj);
            RebindButtonClick(obj, onClick);
            SetButtonLabel(obj, label);
            SetTooltip(obj, tooltip);
            EnsureButtonDefaultState(obj);

            if (width > 0f || height > 0f)
            {
                RectTransform rect = obj.GetComponent<RectTransform>();
                if (rect != null)
                {
                    Vector2 targetSize = rect.sizeDelta;
                    if (width > 0f)
                    {
                        targetSize.x = width;
                    }
                    if (height > 0f)
                    {
                        targetSize.y = height;
                    }

                    rect.sizeDelta = targetSize;
                }

                LayoutElement layout = obj.GetComponent<LayoutElement>();
                if (layout == null)
                {
                    layout = obj.AddComponent<LayoutElement>();
                }

                if (width > 0f)
                {
                    layout.preferredWidth = width;
                }
                if (height > 0f)
                {
                    layout.preferredHeight = height;
                }
            }

            Button button = obj.GetComponent<Button>();
            if (button == null)
            {
                button = obj.GetComponentInChildren<Button>(true);
            }
            return button;
        }

        public static Button CreateStyledButton(
            Transform parent,
            string name,
            string label,
            float width,
            float height,
            UnityAction onClick)
        {
            GameObject obj = CreateUiObject(string.IsNullOrEmpty(name) ? ButtonDefaultObjectName : name, parent);
            Image image = obj.AddComponent<Image>();
            image.color = buttonBackground;

            Button button = obj.AddComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            ButtonDefault buttonDefault = obj.AddComponent<ButtonDefault>();

            TextMeshProUGUI text = CreateText(obj.transform, ButtonTextObjectName, label, DefaultButtonFontSize, TextAlignmentOptions.Center, buttonTextColor);
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

        public static bool TryAppendProfileExtra(Profile_Popup profilePopup, string text, bool addDivider = true)
        {
            if (profilePopup == null || profilePopup.Extras_Container == null || string.IsNullOrEmpty(text))
            {
                return false;
            }

            if (addDivider && profilePopup.prefab_divider != null)
            {
                UnityEngine.Object.Instantiate(profilePopup.prefab_divider).transform.SetParent(profilePopup.Extras_Container.transform, false);
            }

            if (profilePopup.prefab_text != null)
            {
                GameObject txtObject = UnityEngine.Object.Instantiate(profilePopup.prefab_text);
                ExtensionMethods.SetText(txtObject, text);
                txtObject.transform.SetParent(profilePopup.Extras_Container.transform, false);
            }
            else
            {
                TextMeshProUGUI tmp = CreateText(
                    profilePopup.Extras_Container.transform,
                    ProfileExtraDefaultObjectName,
                    text,
                    DefaultProfileTextFontSize,
                    TextAlignmentOptions.TopLeft,
                    mainScript.black32);
                tmp.enableWordWrapping = true;
            }

            RebuildLayout(profilePopup.Extras_Container.transform);
            return true;
        }

        public static GameObject CreateUiObject(string name, Transform parent)
        {
            return CreateUiObject(name, parent, null);
        }

        public static GameObject CreateUiObject(string name, Transform parent, params Type[] additionalComponents)
        {
            List<Type> components = new List<Type>();
            components.Add(typeof(RectTransform));
            if (additionalComponents != null)
            {
                for (int i = 0; i < additionalComponents.Length; i++)
                {
                    Type componentType = additionalComponents[i];
                    if (componentType == null || componentType == typeof(RectTransform))
                    {
                        continue;
                    }

                    if (!components.Contains(componentType))
                    {
                        components.Add(componentType);
                    }
                }
            }

            GameObject obj = new GameObject(name, components.ToArray());
            obj.transform.SetParent(parent, false);
            if (parent != null)
            {
                obj.layer = parent.gameObject.layer;
            }

            return obj;
        }

        public static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string text,
            int fontSize,
            TextAlignmentOptions alignment,
            Color color)
        {
            EnsureDefaultFonts();

            GameObject obj = CreateUiObject(name, parent);
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = ResolveLocalizedText(text);
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            if (defaultTmpFont != null)
            {
                tmp.font = defaultTmpFont;
            }

            return tmp;
        }

        public static Text CreateLegacyText(
            Transform parent,
            string name,
            string text,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            EnsureDefaultFonts();

            GameObject obj = CreateUiObject(name, parent);
            Text uiText = obj.AddComponent<Text>();
            uiText.text = ResolveLocalizedText(text);
            uiText.fontSize = fontSize;
            uiText.alignment = alignment;
            uiText.color = color;
            uiText.raycastTarget = false;
            uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            if (defaultLegacyFont != null)
            {
                uiText.font = defaultLegacyFont;
            }

            return uiText;
        }

        public static Button CreateButtonFromTemplateOrStyle(
            GameObject template,
            Transform parent,
            string name,
            string label,
            string tooltip,
            UnityAction onClick,
            float width = 0f,
            float height = 0f)
        {
            if (template != null)
            {
                return CloneStyledButton(template, parent, name, label, tooltip, onClick, width, height);
            }

            float resolvedWidth = width <= 0f ? DefaultButtonWidth : width;
            float resolvedHeight = height <= 0f ? DefaultButtonHeight : height;
            return CreateStyledButton(parent, name, label, resolvedWidth, resolvedHeight, onClick);
        }

        public static GameObject CreateVerticalLayoutContainer(
            Transform parent,
            string name,
            float spacing = DefaultLayoutSpacing,
            int paddingLeft = 0,
            int paddingRight = 0,
            int paddingTop = 0,
            int paddingBottom = 0,
            bool forceExpandWidth = true,
            bool forceExpandHeight = false,
            bool addContentSizeFitter = true)
        {
            GameObject container = CreateUiObject(string.IsNullOrEmpty(name) ? VerticalContainerDefaultObjectName : name, parent);
            VerticalLayoutGroup layout = AddOrGetComponent<VerticalLayoutGroup>(container);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = forceExpandWidth;
            layout.childForceExpandHeight = forceExpandHeight;

            if (addContentSizeFitter)
            {
                ContentSizeFitter fitter = AddOrGetComponent<ContentSizeFitter>(container);
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            return container;
        }

        public static GameObject CreateHorizontalLayoutContainer(
            Transform parent,
            string name,
            float spacing = DefaultLayoutSpacing,
            int paddingLeft = 0,
            int paddingRight = 0,
            int paddingTop = 0,
            int paddingBottom = 0,
            bool forceExpandWidth = false,
            bool forceExpandHeight = false,
            bool addContentSizeFitter = false)
        {
            GameObject container = CreateUiObject(string.IsNullOrEmpty(name) ? HorizontalContainerDefaultObjectName : name, parent);
            HorizontalLayoutGroup layout = AddOrGetComponent<HorizontalLayoutGroup>(container);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = forceExpandWidth;
            layout.childForceExpandHeight = forceExpandHeight;

            if (addContentSizeFitter)
            {
                ContentSizeFitter fitter = AddOrGetComponent<ContentSizeFitter>(container);
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            return container;
        }

        public static GameObject CreateDivider(
            Transform parent,
            string name = DividerDefaultObjectName,
            float height = DefaultDividerHeight,
            Color? color = null)
        {
            GameObject divider = CreateUiObject(string.IsNullOrEmpty(name) ? DividerDefaultObjectName : name, parent);
            Image image = AddOrGetComponent<Image>(divider);
            image.color = color ?? panelOuter;

            LayoutElement layout = AddOrGetComponent<LayoutElement>(divider);
            float resolvedHeight = height <= 0f ? DefaultDividerHeight : height;
            layout.minHeight = resolvedHeight;
            layout.preferredHeight = resolvedHeight;
            layout.flexibleHeight = 0f;

            return divider;
        }

        public static bool TryCreateProfileText(
            Profile_Popup profilePopup,
            Transform parent,
            string objectName,
            string text,
            int fallbackFontSize,
            out GameObject createdObject)
        {
            createdObject = null;
            if (parent == null)
            {
                return false;
            }

            if (profilePopup != null && profilePopup.prefab_text != null)
            {
                createdObject = UnityEngine.Object.Instantiate(profilePopup.prefab_text, parent, false);
                createdObject.name = string.IsNullOrEmpty(objectName) ? ProfileTextDefaultObjectName : objectName;
                SetText(createdObject, text, true);
                createdObject.SetActive(true);
                return true;
            }

            int fontSize = fallbackFontSize <= 0 ? DefaultProfileTextFontSize : fallbackFontSize;
            TextMeshProUGUI tmp = CreateText(
                parent,
                string.IsNullOrEmpty(objectName) ? ProfileTextDefaultObjectName : objectName,
                text,
                fontSize,
                TextAlignmentOptions.TopLeft,
                mainScript.black32);
            if (tmp != null)
            {
                tmp.enableWordWrapping = true;
                createdObject = tmp.gameObject;
            }

            return createdObject != null;
        }

        public static bool TryCreateProfileDivider(
            Profile_Popup profilePopup,
            Transform parent,
            string objectName,
            float fallbackHeight,
            out GameObject createdObject)
        {
            createdObject = null;
            if (parent == null)
            {
                return false;
            }

            if (profilePopup != null && profilePopup.prefab_divider != null)
            {
                createdObject = UnityEngine.Object.Instantiate(profilePopup.prefab_divider, parent, false);
                createdObject.name = string.IsNullOrEmpty(objectName) ? ProfileDividerDefaultObjectName : objectName;
                createdObject.SetActive(true);
                return true;
            }

            Color fallbackColor = profilePopup != null ? (Color)profilePopup.Color_Secondary : (Color)panelOuter;
            createdObject = CreateDivider(
                parent,
                string.IsNullOrEmpty(objectName) ? ProfileDividerDefaultObjectName : objectName,
                fallbackHeight <= 0f ? DefaultDividerHeight : fallbackHeight,
                fallbackColor);
            return createdObject != null;
        }

        public static void SetText(GameObject root, string text, bool enableWordWrap = true)
        {
            if (root == null)
            {
                return;
            }

            string resolved = ResolveLocalizedText(text);
            try
            {
                ExtensionMethods.SetText(root, resolved);
            }
            catch
            {
            }

            TextMeshProUGUI[] tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < tmps.Length; i++)
            {
                TextMeshProUGUI tmp = tmps[i];
                if (tmp == null)
                {
                    continue;
                }

                tmp.text = resolved;
                tmp.enableWordWrapping = enableWordWrap;
            }

            Text[] texts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text legacy = texts[i];
                if (legacy == null)
                {
                    continue;
                }

                legacy.text = resolved;
                legacy.horizontalOverflow = enableWordWrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            }
        }

        public static float MeasurePreferredTextWidth(
            GameObject root,
            float minWidth = DefaultPreferredTextMinWidth,
            float maxWidth = DefaultPreferredTextMaxWidth,
            float horizontalPadding = DefaultPreferredTextHorizontalPadding)
        {
            float normalizedMinWidth = Mathf.Max(0f, minWidth);
            float normalizedMaxWidth = Mathf.Max(normalizedMinWidth, maxWidth);
            if (root == null)
            {
                return normalizedMinWidth;
            }

            float maxLabelWidth = 0f;
            TextMeshProUGUI[] tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < tmps.Length; i++)
            {
                TextMeshProUGUI tmp = tmps[i];
                if (tmp == null)
                {
                    continue;
                }

                tmp.enableWordWrapping = false;
                string raw = tmp.text ?? string.Empty;
                if (raw.Length == 0)
                {
                    continue;
                }

                Vector2 preferred = tmp.GetPreferredValues(raw);
                maxLabelWidth = Mathf.Max(maxLabelWidth, preferred.x);
            }

            Text[] texts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text textComponent = texts[i];
                if (textComponent == null)
                {
                    continue;
                }

                string raw = textComponent.text ?? string.Empty;
                if (raw.Length == 0)
                {
                    continue;
                }

                float preferred = textComponent.preferredWidth;
                if (preferred <= 0f)
                {
                    preferred = raw.Length * DefaultTextCharWidthFallback;
                }

                maxLabelWidth = Mathf.Max(maxLabelWidth, preferred);
            }

            if (maxLabelWidth <= 0f)
            {
                return normalizedMinWidth;
            }

            return Mathf.Clamp(maxLabelWidth + Mathf.Max(0f, horizontalPadding), normalizedMinWidth, normalizedMaxWidth);
        }

        public static void ConfigureButtonLayout(
            GameObject buttonRoot,
            Transform parent,
            float preferredHeight = DefaultButtonHeight,
            float minWidth = 0f,
            float maxWidth = 0f)
        {
            if (buttonRoot == null)
            {
                return;
            }

            if (parent != null)
            {
                SetLayerRecursively(buttonRoot, parent.gameObject.layer);
            }

            LayoutElement layout = AddOrGetComponent<LayoutElement>(buttonRoot);
            if (preferredHeight > 0f)
            {
                if (layout.preferredHeight <= 0f)
                {
                    layout.preferredHeight = preferredHeight;
                }

                if (layout.minHeight <= 0f)
                {
                    layout.minHeight = preferredHeight;
                }
            }

            if (parent != null && parent.GetComponent<HorizontalLayoutGroup>() != null)
            {
                float normalizedMinWidth = Mathf.Max(0f, minWidth);
                float normalizedMaxWidth = maxWidth > 0f
                    ? Mathf.Max(normalizedMinWidth, maxWidth)
                    : Mathf.Max(normalizedMinWidth, DefaultMinButtonMeasureWidth);
                float measuredWidth = MeasurePreferredTextWidth(
                    buttonRoot,
                    normalizedMinWidth,
                    normalizedMaxWidth,
                    DefaultMeasuredButtonHorizontalPadding);
                if (measuredWidth > 0f)
                {
                    layout.preferredWidth = measuredWidth;
                }

                if (minWidth > 0f)
                {
                    layout.minWidth = normalizedMinWidth;
                }
            }

            if (parent != null && parent.GetComponent<VerticalLayoutGroup>() != null)
            {
                if (minWidth > 0f)
                {
                    layout.minWidth = Mathf.Max(layout.minWidth, minWidth);
                }

                layout.flexibleWidth = 1f;
            }
        }

        public static void RebindAllButtons(GameObject root, UnityAction onClick)
        {
            RebindButtonClick(root, onClick);
        }

        public static void ActivateButtonDefaults(GameObject root)
        {
            EnsureButtonDefaultState(root);
        }

        public static void ApplyLayerRecursively(GameObject root, int layer)
        {
            SetLayerRecursively(root, layer);
        }

        public static void RebuildLayout(Transform root)
        {
            if (root == null)
            {
                return;
            }

            RectTransform rect = root as RectTransform;
            if (rect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            }
        }

        public static void ClearChildren(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child != null)
                {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
            }
        }

        public static void SetButtonLabel(GameObject buttonRoot, string label)
        {
            if (buttonRoot == null)
            {
                return;
            }

            string resolved = ResolveLanguageDataTextInternal(label, label);

            TextMeshProUGUI[] tmps = buttonRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < tmps.Length; i++)
            {
                TextMeshProUGUI tmp = tmps[i];
                if (tmp == null)
                {
                    continue;
                }

                tmp.text = resolved;
                tmp.color = buttonTextColor;
                tmp.enableWordWrapping = false;
                tmp.alignment = TextAlignmentOptions.Center;
                if (defaultTmpFont != null)
                {
                    tmp.font = defaultTmpFont;
                }
            }

            Text[] texts = buttonRoot.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null)
                {
                    continue;
                }

                text.text = resolved;
                text.color = buttonTextColor;
            }
        }

        public static void SetTooltip(GameObject buttonRoot, string tooltip)
        {
            if (buttonRoot == null)
            {
                return;
            }

            string safe = ResolveLanguageDataTextInternal(tooltip, tooltip);
            ButtonDefault[] defaults = buttonRoot.GetComponentsInChildren<ButtonDefault>(true);
            for (int i = 0; i < defaults.Length; i++)
            {
                ButtonDefault bd = defaults[i];
                if (bd == null)
                {
                    continue;
                }

                bd.DefaultTooltip = safe;
                bd.SetTooltip(safe);
            }
        }

        public static void ClearLocalizationComponents(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            Lang_Button[] langButtons = root.GetComponentsInChildren<Lang_Button>(true);
            for (int i = 0; i < langButtons.Length; i++)
            {
                Lang_Button lb = langButtons[i];
                if (lb == null)
                {
                    continue;
                }

                lb.Constant = string.Empty;
                lb.Tooltip = string.Empty;
            }
        }

        private static void EnsureButtonDefaultState(GameObject buttonRoot)
        {
            if (buttonRoot == null)
            {
                return;
            }

            ButtonDefault[] defaults = buttonRoot.GetComponentsInChildren<ButtonDefault>(true);
            for (int i = 0; i < defaults.Length; i++)
            {
                ButtonDefault bd = defaults[i];
                if (bd == null)
                {
                    continue;
                }

                bd.Activate(true, false);
            }
        }

        private static void RebindButtonClick(GameObject root, UnityAction onClick)
        {
            if (root == null)
            {
                return;
            }

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                {
                    continue;
                }

                button.onClick = new Button.ButtonClickedEvent();
                if (onClick != null)
                {
                    button.onClick.AddListener(onClick);
                }
            }
        }

        private static void CreateStyledScrollView(
            Transform panel,
            Vector2 offsetMin,
            Vector2 offsetMax,
            out ScrollRect scrollRect,
            out Transform contentRoot)
        {
            GameObject scrollObj = CreateUiObject(ScrollViewObjectName, panel);
            RectTransform scrollRectTransform = scrollObj.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = AnchorBottomLeft;
            scrollRectTransform.anchorMax = AnchorTopRight;
            scrollRectTransform.offsetMin = offsetMin;
            scrollRectTransform.offsetMax = offsetMax;

            Image scrollImage = scrollObj.AddComponent<Image>();
            scrollImage.color = panelInner;

            scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = DefaultScrollSensitivity;

            GameObject viewport = CreateUiObject(ScrollViewportObjectName, scrollObj.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = AnchorBottomLeft;
            viewportRect.anchorMax = AnchorTopRight;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = panelInner;
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject content = CreateUiObject(ScrollContentObjectName, viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(AnchorBottomLeft.x, AnchorTopRight.y);
            contentRect.anchorMax = AnchorTopRight;
            contentRect.pivot = AnchorTopCenter;
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.padding = new RectOffset(
                DefaultScrollContentPadding,
                DefaultScrollContentPadding,
                DefaultScrollContentPadding,
                DefaultScrollContentPadding);
            layout.spacing = DefaultLayoutSpacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            AddScrollbar(scrollObj.transform, scrollRect);

            contentRoot = content.transform;
        }

        private static Button CreateBottomCloseButton(Transform panel, Popup popup)
        {
            Button button = null;
            GameObject template = GetDefaultButtonTemplate();
            if (template != null)
            {
                button = CloneStyledButton(template, panel, CloseButtonObjectName, GetCloseLabel(), GetCloseLabel(), delegate
                {
                    if (popup != null)
                    {
                        popup.Hide(null);
                    }
                }, DefaultCloseButtonWidth, DefaultCloseButtonHeight);
            }
            else
            {
                button = CreateStyledButton(panel, CloseButtonObjectName, GetCloseLabel(), DefaultCloseButtonWidth, DefaultCloseButtonHeight, delegate
                {
                    if (popup != null)
                    {
                        popup.Hide(null);
                    }
                });
            }

            if (button != null)
            {
                RectTransform closeRect = button.GetComponent<RectTransform>();
                if (closeRect != null)
                {
                    closeRect.anchorMin = new Vector2(AnchorCentered.x, AnchorBottomLeft.y);
                    closeRect.anchorMax = new Vector2(AnchorCentered.x, AnchorBottomLeft.y);
                    closeRect.pivot = new Vector2(AnchorCentered.x, AnchorBottomLeft.y);
                    closeRect.anchoredPosition = new Vector2(0f, DefaultCloseButtonBottomOffset);
                }
            }

            return button;
        }

        private static void AddScrollbar(Transform parent, ScrollRect target)
        {
            if (parent == null || target == null)
            {
                return;
            }

            Scrollbar template = GetDefaultScrollbarTemplate();
            GameObject scrollbarObj;
            Scrollbar scrollbar;
            if (template != null)
            {
                scrollbarObj = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
                scrollbarObj.name = ScrollbarObjectName;
                scrollbar = scrollbarObj.GetComponent<Scrollbar>();
            }
            else
            {
                scrollbarObj = CreateUiObject(ScrollbarObjectName, parent);
                Image trackImage = scrollbarObj.AddComponent<Image>();
                trackImage.color = ScrollbarTrackColor;

                scrollbar = scrollbarObj.AddComponent<Scrollbar>();
                GameObject handleObj = CreateUiObject(ScrollbarHandleObjectName, scrollbarObj.transform);
                RectTransform handleRect = handleObj.GetComponent<RectTransform>();
                handleRect.anchorMin = Vector2.zero;
                handleRect.anchorMax = Vector2.one;
                handleRect.offsetMin = Vector2.zero;
                handleRect.offsetMax = Vector2.zero;

                Image handleImage = handleObj.AddComponent<Image>();
                handleImage.color = buttonBackground;
                scrollbar.targetGraphic = handleImage;
                scrollbar.handleRect = handleRect;
            }

            RectTransform scrollbarRect = scrollbarObj.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = AnchorBottomRight;
            scrollbarRect.anchorMax = AnchorTopRight;
            scrollbarRect.pivot = AnchorTopRight;
            scrollbarRect.sizeDelta = new Vector2(DefaultScrollbarWidth, 0f);
            scrollbarRect.anchoredPosition = new Vector2(DefaultScrollbarRightOffset, 0f);

            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            target.verticalScrollbar = scrollbar;
            target.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            target.verticalScrollbarSpacing = DefaultScrollbarSpacing;
        }

        private static GameObject FindAwardsButton()
        {
            try
            {
                GameObject awardsPopup = PopupManager.GetObject(PopupManager._type.awards);
                if (awardsPopup != null)
                {
                    ButtonDefault[] popupDefaults = awardsPopup.transform.parent != null
                        ? awardsPopup.transform.parent.GetComponentsInChildren<ButtonDefault>(true)
                        : awardsPopup.GetComponentsInChildren<ButtonDefault>(true);

                    GameObject fromDefaults = FindAwardsButtonByDefaults(popupDefaults);
                    if (fromDefaults != null)
                    {
                        return fromDefaults;
                    }
                }
            }
            catch
            {
            }

            ButtonDefault[] allDefaults = UnityEngine.Object.FindObjectsOfType<ButtonDefault>();
            GameObject found = FindAwardsButtonByDefaults(allDefaults);
            if (found != null)
            {
                return found;
            }

            Button[] buttons = UnityEngine.Object.FindObjectsOfType<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null || !button.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (LooksLikeAwardsButton(button.gameObject))
                {
                    return button.gameObject;
                }
            }

            return null;
        }

        private static GameObject FindAwardsButtonByDefaults(ButtonDefault[] defaults)
        {
            if (defaults == null)
            {
                return null;
            }

            string localized = GetAwardsLabel();
            for (int i = 0; i < defaults.Length; i++)
            {
                ButtonDefault bd = defaults[i];
                if (bd == null || bd.gameObject == null || !bd.gameObject.activeInHierarchy)
                {
                    continue;
                }

                string tooltip = bd.DefaultTooltip ?? string.Empty;
                if (StringEqualsInvariant(tooltip, localized) || StringEqualsInvariant(tooltip, AwardsLocalizationKey))
                {
                    return bd.gameObject;
                }

                if (LooksLikeAwardsButton(bd.gameObject))
                {
                    return bd.gameObject;
                }
            }

            return null;
        }

        private static bool LooksLikeAwardsButton(GameObject go)
        {
            if (go == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(go.name) && go.name.IndexOf(AwardsNameToken, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            string localized = GetAwardsLabel();

            TextMeshProUGUI[] tmps = go.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < tmps.Length; i++)
            {
                TextMeshProUGUI tmp = tmps[i];
                if (tmp != null && StringEqualsInvariant(tmp.text, localized))
                {
                    return true;
                }
            }

            Text[] texts = go.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text != null && StringEqualsInvariant(text.text, localized))
                {
                    return true;
                }
            }

            Lang_Button[] langButtons = go.GetComponentsInChildren<Lang_Button>(true);
            for (int i = 0; i < langButtons.Length; i++)
            {
                Lang_Button lb = langButtons[i];
                if (lb == null)
                {
                    continue;
                }

                if (StringEqualsInvariant(lb.Constant, AwardsLocalizationKey) || StringEqualsInvariant(lb.Tooltip, AwardsLocalizationKey))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindSettingsSliderTemplate(out Settings_Slider template)
        {
            if (defaultSettingsSliderTemplate != null)
            {
                template = defaultSettingsSliderTemplate;
                return true;
            }

            PopupManager._type[] preference =
            {
                PopupManager._type.main_menu_settings,
                PopupManager._type.settings_difficulty
            };

            if (TryFindPopupTemplate(preference, out template))
            {
                defaultSettingsSliderTemplate = template;
                return true;
            }

            template = UnityEngine.Object.FindObjectOfType<Settings_Slider>();
            if (template != null)
            {
                defaultSettingsSliderTemplate = template;
                return true;
            }

            return false;
        }

        private static bool TryFindCheckboxTemplate(out Checkbox_Text template)
        {
            if (defaultCheckboxTemplate != null)
            {
                template = defaultCheckboxTemplate;
                return true;
            }

            PopupManager._type[] preference =
            {
                PopupManager._type.main_menu_settings,
                PopupManager._type.settings_difficulty
            };

            if (TryFindPopupTemplate(preference, out template))
            {
                defaultCheckboxTemplate = template;
                return true;
            }

            template = UnityEngine.Object.FindObjectOfType<Checkbox_Text>();
            if (template != null)
            {
                defaultCheckboxTemplate = template;
                return true;
            }

            return false;
        }

        private static bool TryFindPopupTemplate<T>(PopupManager._type[] preference, out T template) where T : Component
        {
            template = null;
            if (preference != null)
            {
                for (int i = 0; i < preference.Length; i++)
                {
                    try
                    {
                        GameObject popup = PopupManager.GetObject(preference[i]);
                        if (popup == null)
                        {
                            continue;
                        }

                        T found = popup.GetComponentInChildren<T>(true);
                        if (found != null)
                        {
                            template = found;
                            return true;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            PopupManager manager;
            if (TryGetPopupManager(out manager) && manager != null && manager.popups != null)
            {
                for (int i = 0; i < manager.popups.Length; i++)
                {
                    PopupManager._popup entry = manager.popups[i];
                    if (entry == null || entry.obj == null)
                    {
                        continue;
                    }

                    T found = entry.obj.GetComponentInChildren<T>(true);
                    if (found != null)
                    {
                        template = found;
                        return true;
                    }
                }
            }

            return false;
        }

        private static string ReadVariableValue(string variableId, string fallback = EmptyText)
        {
            if (string.IsNullOrEmpty(variableId))
            {
                return fallback ?? string.Empty;
            }

            try
            {
                string value = variables.Get(variableId);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            catch
            {
            }

            return fallback ?? string.Empty;
        }

        private static void WriteVariableValue(string variableId, string value)
        {
            if (string.IsNullOrEmpty(variableId))
            {
                return;
            }

            try
            {
                variables.Set(variableId, value ?? string.Empty);
            }
            catch
            {
            }
        }

        private static bool TryParseFloatValue(string raw, out float value)
        {
            value = 0f;
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                return true;
            }

            return float.TryParse(raw, out value);
        }

        private static bool ParseBoolValue(string raw, bool fallback)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return fallback;
            }

            string value = raw.Trim();
            if (string.Equals(value, BoolTrueNumeric, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolTrueWord, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolTrueYes, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolTrueOn, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(value, BoolFalseNumeric, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolFalseWord, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolFalseNo, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BoolFalseOff, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            int intValue;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue))
            {
                return intValue != 0;
            }

            float floatValue;
            if (TryParseFloatValue(value, out floatValue))
            {
                return Mathf.Abs(floatValue) > float.Epsilon;
            }

            return fallback;
        }

        private static float ResolveCurrentFloatValue(StagedVariablesState stagedState, string variableId, float defaultValue)
        {
            if (stagedState != null)
            {
                return stagedState.GetCurrentFloat(variableId, defaultValue);
            }

            float parsed;
            return TryParseFloatValue(ReadVariableValue(variableId, defaultValue.ToString(CultureInfo.InvariantCulture)), out parsed)
                ? parsed
                : defaultValue;
        }

        private static bool ResolveCurrentBoolValue(StagedVariablesState stagedState, string variableId, bool defaultValue)
        {
            if (stagedState != null)
            {
                return stagedState.GetCurrentBool(variableId, defaultValue);
            }

            return ParseBoolValue(ReadVariableValue(variableId, defaultValue ? BoolTrueNumeric : BoolFalseNumeric), defaultValue);
        }

        private static float NormalizeSliderValue(float value, float minValue, float maxValue)
        {
            if (maxValue <= minValue)
            {
                return 0f;
            }

            return Mathf.Clamp01((value - minValue) / (maxValue - minValue));
        }

        private static float ResolveSliderValue(float normalizedValue, float minValue, float maxValue, bool roundToWholeNumber)
        {
            if (maxValue <= minValue)
            {
                return minValue;
            }

            float clampedNormalized = Mathf.Clamp01(normalizedValue);
            float value = minValue + (clampedNormalized * (maxValue - minValue));
            if (roundToWholeNumber)
            {
                value = Mathf.Round(value);
            }

            return Mathf.Clamp(value, minValue, maxValue);
        }

        private static string FormatFloatValue(float value, bool roundToWholeNumber)
        {
            if (roundToWholeNumber)
            {
                int rounded = Mathf.RoundToInt(value);
                return rounded.ToString(CultureInfo.InvariantCulture);
            }

            return value.ToString(FloatValueFormat, CultureInfo.InvariantCulture);
        }

        private static void UpdateSettingsSliderVisual(Settings_Slider settingsSlider, string labelId, float value, bool roundToWholeNumber)
        {
            if (settingsSlider == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(labelId))
            {
                settingsSlider.Title_Text = labelId;
            }

            if (settingsSlider.Title == null)
            {
                return;
            }

            BindLanguageData(settingsSlider.Title, labelId, labelId);
            TextMeshProUGUI label = settingsSlider.Title.GetComponent<TextMeshProUGUI>();
            if (label == null)
            {
                return;
            }

            string title = ResolveLanguageDataTextInternal(labelId, labelId);
            string formatted = FormatFloatValue(value, roundToWholeNumber);
            label.text = string.IsNullOrEmpty(title) ? formatted : (title + LabelValueSeparator + formatted);
        }

        private sealed class StagedSliderBinding : MonoBehaviour
        {
            private StagedVariablesState stagedState;
            private string variableId;
            private string labelId;
            private float minValue;
            private float maxValue;
            private float defaultValue;
            private bool roundToWholeNumber;
            private Settings_Slider settingsSlider;
            private Slider slider;
            private bool suppressEvents;
            private bool configured;

            internal void Setup(
                StagedVariablesState state,
                string id,
                string label,
                float min,
                float max,
                float fallback,
                bool round,
                Settings_Slider sliderComponent)
            {
                stagedState = state;
                variableId = id ?? string.Empty;
                labelId = label ?? string.Empty;
                minValue = min;
                maxValue = max;
                defaultValue = fallback;
                roundToWholeNumber = round;
                settingsSlider = sliderComponent;

                if (settingsSlider != null)
                {
                    settingsSlider.Min_Value = minValue;
                    settingsSlider.Max_Value = maxValue;
                    if (!string.IsNullOrEmpty(labelId))
                    {
                        settingsSlider.Title_Text = labelId;
                    }
                }

                if (slider == null)
                {
                    if (settingsSlider != null && settingsSlider.Slider_Obj != null)
                    {
                        slider = settingsSlider.Slider_Obj.GetComponent<Slider>();
                    }

                    if (slider == null)
                    {
                        slider = gameObject.GetComponentInChildren<Slider>();
                    }
                }

                if (slider != null)
                {
                    slider.onValueChanged = new Slider.SliderEvent();
                    slider.onValueChanged.AddListener(OnSliderValueChanged);
                }

                configured = true;
                RefreshFromState();
            }

            private void OnEnable()
            {
                if (configured)
                {
                    RefreshFromState();
                }
            }

            private void OnSliderValueChanged(float normalizedValue)
            {
                if (suppressEvents)
                {
                    return;
                }

                float resolved = ResolveSliderValue(normalizedValue, minValue, maxValue, roundToWholeNumber);
                if (stagedState != null)
                {
                    stagedState.StageFloat(variableId, resolved);
                }
                else
                {
                    WriteVariableValue(variableId, FormatFloatValue(resolved, roundToWholeNumber));
                }

                UpdateSettingsSliderVisual(settingsSlider, labelId, resolved, roundToWholeNumber);
            }

            private void RefreshFromState()
            {
                float currentValue = ResolveCurrentFloatValue(stagedState, variableId, defaultValue);
                currentValue = Mathf.Clamp(currentValue, minValue, maxValue);
                float normalized = NormalizeSliderValue(currentValue, minValue, maxValue);

                if (slider != null)
                {
                    suppressEvents = true;
                    slider.SetValueWithoutNotify(normalized);
                    suppressEvents = false;
                }

                UpdateSettingsSliderVisual(settingsSlider, labelId, currentValue, roundToWholeNumber);
            }
        }

        private sealed class StagedCheckboxBinding : MonoBehaviour
        {
            private StagedVariablesState stagedState;
            private string variableId;
            private string labelId;
            private bool defaultValue;
            private Checkbox_Text checkbox;
            private Button toggleButton;
            private bool currentValue;
            private bool configured;

            internal void Setup(
                StagedVariablesState state,
                string id,
                string label,
                bool fallback,
                Checkbox_Text checkboxComponent)
            {
                stagedState = state;
                variableId = id ?? string.Empty;
                labelId = label ?? string.Empty;
                defaultValue = fallback;
                checkbox = checkboxComponent;

                if (checkbox != null && checkbox.Title != null)
                {
                    BindLanguageData(checkbox.Title, labelId, labelId);
                }

                if (toggleButton == null)
                {
                    toggleButton = gameObject.GetComponentInChildren<Button>();
                }

                if (toggleButton != null)
                {
                    toggleButton.onClick = new Button.ButtonClickedEvent();
                    toggleButton.onClick.AddListener(OnToggleClicked);
                }

                configured = true;
                RefreshFromState();
            }

            private void OnEnable()
            {
                if (configured)
                {
                    RefreshFromState();
                }
            }

            private void OnToggleClicked()
            {
                currentValue = !currentValue;
                ApplyVisual();

                if (stagedState != null)
                {
                    stagedState.StageBool(variableId, currentValue);
                }
                else
                {
                    WriteVariableValue(variableId, currentValue ? BoolTrueNumeric : BoolFalseNumeric);
                }
            }

            private void RefreshFromState()
            {
                currentValue = ResolveCurrentBoolValue(stagedState, variableId, defaultValue);
                ApplyVisual();
            }

            private void ApplyVisual()
            {
                if (checkbox != null)
                {
                    checkbox.SetCheck(currentValue);
                }
            }
        }

        private static GameObject GetDefaultButtonTemplate()
        {
            if (defaultButtonTemplate != null)
            {
                return defaultButtonTemplate;
            }

            try
            {
                GameObject popup = PopupManager.GetObject(PopupManager._type.single_new);
                if (popup == null)
                {
                    popup = PopupManager.GetObject(PopupManager._type.single_senbatsu);
                }

                if (popup != null)
                {
                    Single_Popup single = popup.GetComponent<Single_Popup>();
                    if (single != null)
                    {
                        if (single.Button_Continue != null)
                        {
                            defaultButtonTemplate = single.Button_Continue;
                            return defaultButtonTemplate;
                        }

                        if (single.prefab_button != null)
                        {
                            defaultButtonTemplate = single.prefab_button;
                            return defaultButtonTemplate;
                        }
                    }
                }
            }
            catch
            {
            }

            return defaultButtonTemplate;
        }

        private static Scrollbar GetDefaultScrollbarTemplate()
        {
            if (defaultScrollbarTemplate != null)
            {
                return defaultScrollbarTemplate;
            }

            PopupManager._type[] preference =
            {
                PopupManager._type.producer_salaries,
                PopupManager._type.producer_contracts,
                PopupManager._type.producer_loans,
                PopupManager._type.notifications,
                PopupManager._type.single_release,
                PopupManager._type.single_senbatsu,
                PopupManager._type.single_chart
            };

            for (int i = 0; i < preference.Length; i++)
            {
                try
                {
                    GameObject popup = PopupManager.GetObject(preference[i]);
                    if (popup == null)
                    {
                        continue;
                    }

                    Scrollbar sb = popup.GetComponentInChildren<Scrollbar>(true);
                    if (sb != null)
                    {
                        defaultScrollbarTemplate = sb;
                        return defaultScrollbarTemplate;
                    }
                }
                catch
                {
                }
            }

            return defaultScrollbarTemplate;
        }

        private static void TryCaptureTheme()
        {
            if (triedThemeCapture)
            {
                return;
            }

            triedThemeCapture = true;
            EnsureDefaultFonts();

            GameObject buttonTemplate = GetDefaultButtonTemplate();
            if (buttonTemplate != null)
            {
                Image image = buttonTemplate.GetComponent<Image>();
                if (image == null)
                {
                    image = buttonTemplate.GetComponentInChildren<Image>(true);
                }

                if (image != null)
                {
                    buttonBackground = image.color;
                }

                TextMeshProUGUI tmp = buttonTemplate.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null)
                {
                    buttonTextColor = tmp.color;
                    if (tmp.font != null)
                    {
                        defaultTmpFont = tmp.font;
                    }
                }
            }

            TryCapturePanelColorsFromSinglesPopup();
        }

        private static void TryCapturePanelColorsFromSinglesPopup()
        {
            try
            {
                GameObject popup = PopupManager.GetObject(PopupManager._type.single_senbatsu);
                if (popup == null)
                {
                    popup = PopupManager.GetObject(PopupManager._type.single_new);
                }

                if (popup == null)
                {
                    return;
                }

                Image[] images = popup.GetComponentsInChildren<Image>(true);
                if (images == null || images.Length == 0)
                {
                    return;
                }

                Color32 darkest = panelOuter;
                Color32 lightest = panelInner;
                float darkestLuma = float.MaxValue;
                float lightestLuma = float.MinValue;

                for (int i = 0; i < images.Length; i++)
                {
                    Image image = images[i];
                    if (image == null)
                    {
                        continue;
                    }

                    Color32 c = image.color;
                    float luma = (LumaCoefficientRed * c.r) + (LumaCoefficientGreen * c.g) + (LumaCoefficientBlue * c.b);
                    if (luma < darkestLuma)
                    {
                        darkestLuma = luma;
                        darkest = c;
                    }
                    if (luma > lightestLuma)
                    {
                        lightestLuma = luma;
                        lightest = c;
                    }
                }

                panelOuter = darkest;
                panelInner = lightest;
                titleColor = new Color32(
                    (byte)Mathf.Max(MinColorChannelForTitle, darkest.r / 2),
                    (byte)Mathf.Max(MinColorChannelForTitle, darkest.g / 2),
                    (byte)Mathf.Max(MinColorChannelForTitle, darkest.b / 2),
                    255);
            }
            catch
            {
            }
        }

        private static void EnsureDefaultFonts()
        {
            if (defaultTmpFont == null)
            {
                TextMeshProUGUI[] tmps = UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>();
                for (int i = 0; i < tmps.Length; i++)
                {
                    TextMeshProUGUI tmp = tmps[i];
                    if (tmp != null && tmp.font != null)
                    {
                        defaultTmpFont = tmp.font;
                        break;
                    }
                }
            }

            if (defaultLegacyFont == null)
            {
                Text[] texts = UnityEngine.Object.FindObjectsOfType<Text>();
                for (int i = 0; i < texts.Length; i++)
                {
                    Text text = texts[i];
                    if (text != null && text.font != null)
                    {
                        defaultLegacyFont = text.font;
                        break;
                    }
                }
            }
        }

        private static string GetAwardsLabel()
        {
            string value;
            if (Language.Data != null && Language.Data.TryGetValue(AwardsLocalizationKey, out value) && !string.IsNullOrEmpty(value))
            {
                return value;
            }

            return DefaultAwardsLabel;
        }

        private static string GetCloseLabel()
        {
            string value;
            if (Language.Data != null && Language.Data.TryGetValue(PopupCloseLocalizationKey, out value) && !string.IsNullOrEmpty(value))
            {
                return value;
            }

            return ModLocalization.Get(CloseFallbackLocalizationKey, CloseFallbackText);
        }

        private static string ResolveLanguageDataTextInternal(string languageKeyOrText, string fallback = null)
        {
            string value;
            if (!string.IsNullOrEmpty(languageKeyOrText) &&
                Language.Data != null &&
                Language.Data.TryGetValue(languageKeyOrText, out value) &&
                !string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (!string.IsNullOrEmpty(fallback) &&
                Language.Data != null &&
                Language.Data.TryGetValue(fallback, out value) &&
                !string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (!string.IsNullOrEmpty(languageKeyOrText))
            {
                string fromModLocalization = ModLocalization.Get(languageKeyOrText, languageKeyOrText);
                if (!string.IsNullOrEmpty(fromModLocalization))
                {
                    return fromModLocalization;
                }
            }

            if (!string.IsNullOrEmpty(fallback))
            {
                string fromFallback = ModLocalization.Get(fallback, fallback);
                if (!string.IsNullOrEmpty(fromFallback))
                {
                    return fromFallback;
                }
            }

            if (!string.IsNullOrEmpty(languageKeyOrText))
            {
                return languageKeyOrText;
            }

            return fallback ?? string.Empty;
        }

        private static string ResolveLocalizedText(string value)
        {
            return ResolveLanguageDataTextInternal(value, value);
        }

        private static bool StringEqualsInvariant(string left, string right)
        {
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
            {
                return false;
            }

            return string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null)
            {
                return;
            }

            root.layer = layer;
            for (int i = 0; i < root.transform.childCount; i++)
            {
                Transform child = root.transform.GetChild(i);
                if (child != null)
                {
                    SetLayerRecursively(child.gameObject, layer);
                }
            }
        }

        private static T AddOrGetComponent<T>(GameObject root) where T : Component
        {
            if (root == null)
            {
                return null;
            }

            T component = root.GetComponent<T>();
            if (component == null)
            {
                component = root.AddComponent<T>();
            }

            return component;
        }

        private static void Log(string message)
        {
            Debug.Log(LogPrefix + SpaceSeparator + message);
        }
    }
}
