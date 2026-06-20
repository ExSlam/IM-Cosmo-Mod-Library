using HarmonyLib;
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.ModernUIPack;
using SimpleJSON;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine.Events;

namespace ModButtons
{
    // --- 1. HARMONY PATCHES ---

    [HarmonyPatch(typeof(PopupManager), "Start")]
    public class PopupManager_Start
    {
        public static void Postfix()
        {
            ModButtonsUtils.GenerateButtonsPopup();
            ModButtonsBootstrap.EnsureButtonInstalled();
        }
    }

    [HarmonyPatch(typeof(Tabs_Manager), "Awake")]
    public class Tabs_Manager_Awake { public static void Postfix() => ModButtonsBootstrap.EnsureButtonInstalled(); }

    [HarmonyPatch(typeof(Tabs_Manager), nameof(Tabs_Manager.OpenTab))]
    public class Tabs_Manager_OpenTab
    {
        public static void Postfix(Tabs_Manager._tab._type __0)
        {
            if (__0 == Tabs_Manager._tab._type.settings) ModButtonsBootstrap.EnsureButtonInstalled();
        }
    }

    // --- 2. BOOTSTRAPPER ---
    public sealed class ModButtonsBootstrap : MonoBehaviour
    {
        private const int MaxInstallAttempts = 240;
        private const float RetryIntervalSeconds = 0.10f;
        
        private static ModButtonsBootstrap instance;
        private int attempts;
        private float nextAttemptAt;

        public static void EnsureButtonInstalled()
        {
            if (ModButtonsUtils.TryInstallActionHubButton()) { DestroyInstance(); return; }
            if (instance != null) return;
            
            Camera camera = Camera.main;
            if (camera == null) return;

            instance = camera.gameObject.GetComponent<ModButtonsBootstrap>() ?? camera.gameObject.AddComponent<ModButtonsBootstrap>();
            instance.attempts = 0;
            instance.nextAttemptAt = Time.unscaledTime;
        }

        private static void DestroyInstance()
        {
            if (instance != null) UnityEngine.Object.Destroy(instance);
            instance = null;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextAttemptAt) return;
            nextAttemptAt = Time.unscaledTime + RetryIntervalSeconds;
            attempts++;
            if (ModButtonsUtils.TryInstallActionHubButton() || attempts >= MaxInstallAttempts) DestroyInstance();
        }
    }

    // --- 3. MULTI-MOD LOCALIZATION MANAGER ---
    internal static class ModButtonsLocalization
    {
        private const string LocalizationDirectoryName = "Localization";
        private const string EnglishFolderName = "en";
        private const string StringsFileName = "strings.txt";
        private const int MaxLocalizationEntries = 4096;
        private const int MaxLineLength = 8192;
        private const int MaxValueLength = 4096;
        
        private static readonly Dictionary<string, Dictionary<string, string>> ModDictionaries = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        internal static string Get(string modPath, string key, string fallback)
        {
            if (string.IsNullOrEmpty(modPath) || string.IsNullOrEmpty(key)) return fallback;
            EnsureLoaded(modPath);

            if (ModDictionaries.TryGetValue(modPath, out var modDict))
            {
                if (modDict.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            return fallback;
        }

        internal static void EnsureLoaded(string modPath)
        {
            if (string.IsNullOrEmpty(modPath) || ModDictionaries.ContainsKey(modPath)) return;

            Dictionary<string, string> dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ModDictionaries[modPath] = dict;

            try
            {
                string localizationDir = Path.Combine(modPath, LocalizationDirectoryName);
                if (!Directory.Exists(localizationDir)) return;

                LoadFile(Path.Combine(localizationDir, EnglishFolderName, StringsFileName), dict);
                LoadFile(Path.Combine(localizationDir, StringsFileName), dict); 

                string configuredLang = GetConfiguredLanguageCode();
                if (!string.IsNullOrEmpty(configuredLang) && configuredLang != EnglishFolderName)
                {
                    string alias = GetAlias(configuredLang);
                    LoadFile(Path.Combine(localizationDir, configuredLang, StringsFileName), dict);
                    if (alias != configuredLang) LoadFile(Path.Combine(localizationDir, alias, StringsFileName), dict);
                }
            }
            catch { }
        }

        private static void LoadFile(string path, Dictionary<string, string> dict)
        {
            if (!File.Exists(path)) return;

            try
            {
                string[] lines = File.ReadAllLines(path);
                foreach (string raw in lines)
                {
                    if (dict.Count >= MaxLocalizationEntries) return;
                    if (string.IsNullOrEmpty(raw) || raw.Length > MaxLineLength) continue;

                    string trimmed = raw.Trim();
                    if (trimmed.Length == 0 || trimmed.StartsWith("#") || trimmed.StartsWith(";") || trimmed.StartsWith("//")) continue;

                    int sepIndex = raw.IndexOf('=');
                    if (sepIndex <= 0) continue;

                    string key = raw.Substring(0, sepIndex).Trim();
                    string value = raw.Substring(sepIndex + 1);
                    if (!string.IsNullOrEmpty(key))
                    {
                        dict[key] = SanitizeValue(value.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t"));
                    }
                }
            }
            catch { }
        }

        private static string GetConfiguredLanguageCode()
        {
            try
            {
                if (staticVars.Settings != null && !string.IsNullOrEmpty(staticVars.Settings.Language))
                {
                    return staticVars.Settings.Language.Trim().ToLowerInvariant();
                }
            }
            catch { }
            return string.Empty;
        }

        private static string GetAlias(string code)
        {
            switch (code)
            {
                case "english": case "enus": case "enusutf8": return "en";
                case "japanese": case "ja": return "jp";
                case "schinese": case "tchinese": case "zh": case "zhcn": return "cn";
                case "russian": case "ukrainian": case "ua": return "ru";
                case "portuguese": case "brazilian": case "pt": case "pt-br": case "pt_br": return "ptbr";
                default: return code;
            }
        }

        private static string SanitizeValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.Length > MaxValueLength) value = value.Substring(0, MaxValueLength);
            return value.Replace('\0', ' ').Replace('<', '＜').Replace('>', '＞');
        }
    }

    // --- 4. CORE LOGIC ---
    class ModButtonsUtils
    {
        // --- CONSTANTS ---
        private const string SettingsContainerPath = "ScrollRect/Container";
        private const string HubButtonObjName = "ModButtonsHubButton";
        private const string HubButtonLabel = "Action Hub";
        private const string TargetModMenuButton = "ModMenuButton";
        private const string FallbackSettingsName = "Settings";
        private const string FallbackMainMenuName = "Main Menu";
        
        private const string PopupObjName = "ModButtonsPopup";
        private const string UIPanelName = "Panel";
        private const string UISettingsContainerName = "Settings_Container";
        private const string UICancelButtonName = "Cancel";
        private const string UIApplyButtonName = "Apply";
        private const string UIScrollRectName = "ActionHubScrollRect";
        private const string UIViewportName = "Viewport";
        private const string UIVerticalContainerName = "VerticalContainer";
        private const string UIScrollbarName = "Scrollbar";
        private const string UIScrollbarSlidingAreaName = "Sliding Area";
        private const string UIScrollbarHandleName = "Handle";
        private const string UIDividerName = "Divider";
        private const string UILineName = "Line";
        private const string UIIconName = "Icon";
        private const string UITextName = "Text";
        private const string UIFallbackCancelLabel = "Cancel";
        
        private const string TargetDirectory = "ModButtons";
        private const string JsonFileName = "buttons.json";
        
        private const string JsonKeyLabel = "label";
        private const string JsonKeyCodeLabel = "codeLabel"; 
        private const string JsonKeyTooltip = "tooltip";
        private const string JsonKeyCodeTooltip = "codeTooltip";
        private const string JsonKeyIcon = "icon";
        private const string JsonKeyAssembly = "assembly";
        private const string JsonKeyClass = "class";
        private const string JsonKeyMethod = "method";
        private const string JsonKeyInputs = "inputs";
        private const string JsonKeyInputId = "id";
        private const string JsonKeyInputType = "type";
        private const string JsonKeyInputValueType = "valueType";
        private const string JsonKeyInputDefault = "default";
        private const string JsonKeyInputMinimum = "min";
        private const string JsonKeyInputMaximum = "max";
        private const string JsonKeyInputOptions = "options";
        private const string JsonKeyInputValue = "value";
        private const string JsonKeyInputPlaceholder = "placeholder";
        private const string JsonKeyInputCodePlaceholder = "codePlaceholder";
        private const string LocKeyModTitle = "mod.title";
        
        private const string PrefixTitle = "Title_";
        private const string PrefixGrid = "Grid_";
        private const string PrefixCell = "Cell_";
        private const string PrefixButton = "Btn_";
        private const string PrefixForm = "Form_";
        private const string PrefixInput = "Input_";
        private const string DotSeparator = ".";
        private const string LogErrorPrefix = "[ModButtons] Execution failed:";
        private static readonly string[] PreferredButtonTemplateNames = { FallbackSettingsName, FallbackMainMenuName };
        
        private const int CustomPopupID = 998;
        private const int NotFoundIndex = -1;
        private const int TitleFontSize = 24;
        private const int MinimumButtonCount = 1;
        private const int ScrollbarButtonCountThreshold = 12;
        
        private const float MaxCellSize = 72f; 
        private const float MinButtonSize = 64f;
        private const float MaxButtonSize = 72f;

        private const float GridCellSpacing = 10f;
        private const float VerticalLayoutSpacing = 15f;
        private const float ContentTopPadding = 12f;
        private const float ContentBottomPadding = 4f;
        private const float DividerHeight = 1f;
        private const float DividerWidth = 420f;
        private const float SectionTitleHeight = 44f;
        private const float FallbackBtnMinWidth = 200f;
        private const float FallbackBtnMinHeight = 40f;
        private const float FormPreferredWidth = 440f;
        private const float FormActionButtonHeight = 38f;
        private const float FormActionButtonWidth = 150f;
        private const float FormFieldLabelHeight = 18f;
        private const float FormControlHeight = 32f;
        private const float FormFieldHeight = 54f;
        private const float FormFieldWidth = 200f;
        private const float FormSpacing = 8f;
        private const float FormValidationHeight = 18f;
        private const int FormInputsPerRow = 2;
        private const int IconButtonsPerRow = 2;
        private const int FallbackTextButtonsPerRow = 2;
        private const float PopupMinimumWidth = 520f;
        private const float PopupMinimumHeight = 360f;
        private const float PopupMaximumScreenWidthRatio = 0.86f;
        private const float PopupMaximumScreenHeightRatio = 0.72f;
        private const float PopupScrollableMaximumScreenHeightRatio = 0.58f;
        private const float PopupScrollableMaximumHeight = 560f;
        private const float PopupHorizontalPadding = 48f;
        private const float PopupScrollbarRightPadding = 12f;
        private const float PopupTopPadding = 58f;
        private const float PopupBottomPadding = 96f;
        private const float ScrollbarWidth = 14f;
        private const float ScrollbarSpacing = 18f;
        private const float ScrollbarInset = 2f;
        private const float ScrollbarRightOffset = -6f;
        private const float ScrollbarMinimumHandleSize = 0.12f;
        private const float ScrollSensitivity = 36f;
        private const float CancelButtonBottomOffset = 24f;
        private const float CancelButtonHeight = 48f;
        private const float CancelButtonPreferredWidth = 360f;
        private const float CancelButtonHorizontalPadding = 48f;
        private const float OpaqueAlpha = 1f;
        private const float TransparentAlpha = 0f;

        private static readonly Color DividerColor = new Color(0.7215686f, 0.6784314f, 0.6509804f, 1f);
        private static readonly Color ScrollbarTrackColor = new Color(0.7215686f, 0.6784314f, 0.6509804f, 0.35f);
        private static readonly Color ScrollbarHandleColor = new Color(0.4078f, 0.4118f, 0.6706f, 0.85f);
        private static readonly Color HiddenMaskColor = new Color(1f, 1f, 1f, OpaqueAlpha);

        // This is the purple color assigned to the action buttons
        private static readonly Color GeneratedButtonColor = new Color(0.4078f, 0.4118f, 0.6706f, 1f);

        private sealed class ButtonSectionLayout
        {
            internal Mods._mod Mod;
            internal List<ActionDefinition> Actions;
            internal string LocalizedTitle;
            internal bool HasFallbackTextButtons;
            internal int ButtonCount;
            internal int SimpleButtonCount;
            internal int FormActionCount;
            internal int ColumnCount;
            internal int RowCount;
            internal float CellWidth;
            internal float CellHeight;
            internal float GridWidth;
            internal float GridHeight;
            internal float FormHeight;
        }

        private enum ActionInputKind
        {
            Text,
            Integer,
            Float,
            Slider,
            Dropdown
        }

        private enum ActionValueKind
        {
            String,
            Integer,
            Float
        }

        private sealed class ActionInputOption
        {
            internal string Label;
            internal string Value;
        }

        private sealed class ActionInputDefinition
        {
            internal string Id;
            internal string Label;
            internal string Placeholder;
            internal ActionInputKind InputKind;
            internal ActionValueKind ValueKind;
            internal string DefaultValue;
            internal float Minimum;
            internal float Maximum;
            internal bool HasMinimum;
            internal bool HasMaximum;
            internal List<ActionInputOption> Options = new List<ActionInputOption>();
        }

        private sealed class ActionDefinition
        {
            internal string Label;
            internal string Tooltip;
            internal string IconPath;
            internal string AssemblyName;
            internal string ClassName;
            internal string MethodName;
            internal List<ActionInputDefinition> Inputs = new List<ActionInputDefinition>();

            internal bool HasInputs
            {
                get { return Inputs != null && Inputs.Count > 0; }
            }
        }

        private sealed class ActionInputBinding
        {
            internal ActionInputDefinition Definition;
            internal TMP_InputField TextInput;
            internal Slider Slider;
            internal CustomDropdown Dropdown;
            internal int DropdownIndex;
        }

        private sealed class PopupLayoutMetrics
        {
            internal float ContentWidth;
            internal float ContentHeight;
            internal float PanelWidth;
            internal float PanelHeight;
            internal float ViewportWidth;
            internal float ViewportHeight;
            internal float DividerWidth;
            internal float RightPadding;
            internal bool RequiresScrollbar;
        }

        public static bool TryInstallActionHubButton()
        {
            mainScript main = Camera.main?.GetComponent<mainScript>();
            if (main?.Data == null) return false;

            Tabs_Manager tabsManager = main.Data.GetComponent<Tabs_Manager>();
            Tabs_Manager._tab settingsTab = tabsManager?.GetTab(Tabs_Manager._tab._type.settings);
            if (settingsTab?.Tab == null) return false;

            Transform settingsRoot = settingsTab.Tab.transform;
            Transform settingsContainer = FindSettingsContainer(settingsRoot);
            if (settingsContainer == null) return false;

            Transform existingButton = FindNamedChild(settingsRoot, HubButtonObjName);
            if (existingButton != null)
            {
                ConfigureHubButton(existingButton.gameObject);
                PositionHubButton(existingButton, settingsRoot, settingsContainer);
                return true;
            }

            GameObject templateButton = FindButtonTemplate(settingsContainer);
            if (templateButton == null)
            {
                templateButton = FindButtonTemplate(settingsRoot);
                if (templateButton == null || templateButton.transform.parent == null) return false;
                settingsContainer = templateButton.transform.parent;
            }
            if (templateButton == null) return false;

            GameObject modActionButton = CloneMainButton(templateButton, settingsContainer, HubButtonObjName, HubButtonLabel);
            if (modActionButton == null) return false;

            ConfigureHubButton(modActionButton);
            PositionHubButton(modActionButton.transform, settingsRoot, settingsContainer);
            return true;
        }

        private static Transform FindSettingsContainer(Transform settingsRoot)
        {
            if (settingsRoot == null) return null;

            Transform container = settingsRoot.Find(SettingsContainerPath);
            if (container != null) return container;

            Transform[] descendants = settingsRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                Transform candidate = descendants[i];
                if (candidate == null || !string.Equals(candidate.name, "Container", StringComparison.Ordinal)) continue;
                if (candidate.GetComponent<VerticalLayoutGroup>() != null || candidate.GetComponent<GridLayoutGroup>() != null)
                {
                    return candidate;
                }
            }

            Button[] buttons = settingsRoot.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button candidate = buttons[i];
                if (candidate == null || candidate.transform.parent == null) continue;
                if (FindText(candidate.transform) == null) continue;

                Transform parent = candidate.transform.parent;
                if (parent.GetComponent<LayoutGroup>() != null)
                {
                    return parent;
                }
            }

            return null;
        }

        private static Transform FindNamedChild(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName)) return null;

            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                Transform candidate = descendants[i];
                if (candidate != null && string.Equals(candidate.name, childName, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static GameObject FindButtonTemplate(Transform settingsContainer)
        {
            if (settingsContainer == null) return null;

            for (int i = 0; i < PreferredButtonTemplateNames.Length; i++)
            {
                foreach (Transform child in settingsContainer)
                {
                    if (child != null && child.name != HubButtonObjName && ButtonMatches(child, PreferredButtonTemplateNames[i]))
                    {
                        return child.gameObject;
                    }
                }
            }

            Button[] buttons = settingsContainer.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button candidate = buttons[i];
                if (candidate == null || candidate.gameObject == null) continue;
                if (IsNamedOrChildOf(candidate.transform, HubButtonObjName)) continue;
                if (IsNamedOrChildOf(candidate.transform, UICancelButtonName)) continue;
                if (FindText(candidate.transform) != null)
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }

        private static GameObject FindActionButtonTemplate()
        {
            mainScript main = Camera.main?.GetComponent<mainScript>();
            Tabs_Manager tabsManager = main?.Data?.GetComponent<Tabs_Manager>();
            Tabs_Manager._tab settingsTab = tabsManager?.GetTab(Tabs_Manager._tab._type.settings);
            Transform settingsRoot = settingsTab?.Tab?.transform;
            if (settingsRoot == null) return null;

            Transform settingsContainer = FindSettingsContainer(settingsRoot);
            GameObject templateButton = FindButtonTemplate(settingsContainer);
            if (templateButton != null) return templateButton;

            return FindButtonTemplate(settingsRoot);
        }

        private static bool IsNamedOrChildOf(Transform candidate, string name)
        {
            if (candidate == null || string.IsNullOrEmpty(name)) return false;

            Transform current = candidate;
            while (current != null)
            {
                if (string.Equals(current.name, name, StringComparison.Ordinal)) return true;
                current = current.parent;
            }

            return false;
        }

        private static void ConfigureHubButton(GameObject hubButton)
        {
            if (hubButton == null) return;

            hubButton.name = HubButtonObjName;
            hubButton.SetActive(true);

            TextMeshProUGUI text = hubButton.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault();
            if (text != null)
            {
                Lang_Button languageBinding = text.GetComponent<Lang_Button>();
                if (languageBinding != null)
                {
                    languageBinding.Constant = HubButtonLabel;
                }

                text.text = HubButtonLabel;
            }

            Button btn = hubButton.GetComponent<Button>();
            if (btn == null) return;

            btn.onClick = new Button.ButtonClickedEvent();
            btn.onClick.AddListener(() =>
            {
                GenerateButtonsPopup();
                PopupManager.OpenPopup((PopupManager._type)CustomPopupID);
            });
        }

        private static void PositionHubButton(Transform hubButton, Transform settingsRoot, Transform settingsContainer)
        {
            if (hubButton == null) return;

            Transform modMenusButton = FindNamedChild(settingsRoot, TargetModMenuButton);
            Transform parent = settingsContainer;
            if (modMenusButton != null && modMenusButton.parent != null)
            {
                parent = modMenusButton.parent;
            }

            if (parent == null) return;
            if (hubButton.parent != parent)
            {
                hubButton.SetParent(parent, false);
            }

            if (modMenusButton != null && modMenusButton.parent == parent)
            {
                hubButton.SetSiblingIndex(Mathf.Min(modMenusButton.GetSiblingIndex() + 1, parent.childCount - 1));
            }
            else
            {
                Transform standardSettingsBtn = parent.Cast<Transform>().FirstOrDefault(t => ButtonMatches(t, FallbackSettingsName));
                if (standardSettingsBtn != null)
                {
                    hubButton.SetSiblingIndex(Mathf.Min(standardSettingsBtn.GetSiblingIndex() + 1, parent.childCount - 1));
                }
                else
                {
                    hubButton.SetSiblingIndex(Mathf.Max(0, parent.childCount - 2));
                }
            }

            RectTransform rect = parent as RectTransform;
            if (rect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            }

            Canvas.ForceUpdateCanvases();
        }

        private static bool ButtonMatches(Transform candidate, string text)
        {
            if (candidate == null || string.IsNullOrEmpty(text)) return false;
            if (candidate.name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) return true;

            TextMeshProUGUI label = FindText(candidate);
            return label != null && !string.IsNullOrEmpty(label.text) && label.text.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static TextMeshProUGUI FindText(Transform root)
        {
            if (root == null) return null;
            return root.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault();
        }

        public static GameObject GenerateButtonsPopup()
        {
            PopupManager pm = Camera.main?.GetComponent<mainScript>()?.Data?.GetComponent<PopupManager>();
            if (pm == null) return null;

            RemoveExistingActionHubPopup(pm);

            GameObject originalPopup = pm.GetByType(PopupManager._type.settings_difficulty)?.obj;
            if (originalPopup == null) return null;

            GameObject actionTemplateButton = FindActionButtonTemplate();
            List<ButtonSectionLayout> sections = CollectButtonSections();
            PopupLayoutMetrics metrics = CalculatePopupLayoutMetrics(sections);

            Scrollbar scrollbarTemplate = FindScrollbarTemplate(originalPopup);
            GameObject modButtonsObj = CreateActionHubPopupRoot(originalPopup);
            Transform panel = CreateActionHubPanel(originalPopup, modButtonsObj.transform, metrics);
            GameObject cancelButton = CreateCancelButton(originalPopup, actionTemplateButton, panel);

            if (cancelButton != null)
            {
                Button btn = cancelButton.GetComponent<Button>();
                if (btn == null)
                {
                    btn = cancelButton.AddComponent<Button>();
                }
                btn.onClick = new Button.ButtonClickedEvent();
                btn.onClick.AddListener(() => PopupManager.Close_());
                PositionCancelButton(cancelButton, metrics);
            }

            ScrollRect scrollRect;
            RectTransform contentRect;
            CreateScrollArea(panel, metrics, scrollbarTemplate, out scrollRect, out contentRect);

            PopulateCustomButtons(contentRect, actionTemplateButton, sections, metrics.DividerWidth);
            FinalizeScrollLayout(contentRect, scrollRect, metrics);

            PopupManager._popup newPopup = new() { type = (PopupManager._type)CustomPopupID, obj = modButtonsObj, BGBlur = true, BGDarken = true };
            Array.Resize(ref pm.popups, pm.popups.Length + 1);
            pm.popups[pm.popups.Length - 1] = newPopup;

            return modButtonsObj;
        }

        private static GameObject CreateActionHubPopupRoot(GameObject sourcePopup)
        {
            GameObject popupRoot = new GameObject(PopupObjName, typeof(RectTransform), typeof(CanvasGroup));
            popupRoot.transform.SetParent(sourcePopup.transform.parent, false);
            popupRoot.SetActive(false);

            RectTransform sourceRect = sourcePopup.GetComponent<RectTransform>();
            RectTransform targetRect = popupRoot.GetComponent<RectTransform>();
            CopyRectTransform(sourceRect, targetRect);

            CanvasGroup canvasGroup = popupRoot.GetComponent<CanvasGroup>();
            canvasGroup.alpha = TransparentAlpha;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            Popup sourcePopupComponent = sourcePopup.GetComponent<Popup>();
            Popup targetPopupComponent = popupRoot.AddComponent<Popup>();
            if (sourcePopupComponent != null)
            {
                targetPopupComponent.ShowAnimation = sourcePopupComponent.ShowAnimation;
                targetPopupComponent.HideAnimation = sourcePopupComponent.HideAnimation;
                targetPopupComponent.HideFast = sourcePopupComponent.HideFast;
                targetPopupComponent.Increase_Popup_Counter = sourcePopupComponent.Increase_Popup_Counter;
            }
            targetPopupComponent.OnOpen = new UnityEvent();

            return popupRoot;
        }

        private static Transform CreateActionHubPanel(GameObject sourcePopup, Transform parent, PopupLayoutMetrics metrics)
        {
            GameObject panelObj = new GameObject(UIPanelName, typeof(RectTransform), typeof(Image));
            panelObj.transform.SetParent(parent, false);

            Transform sourcePanel = sourcePopup != null ? sourcePopup.transform.Find(UIPanelName) : null;
            Image sourceImage = sourcePanel != null ? sourcePanel.GetComponent<Image>() : null;
            Image panelImage = panelObj.GetComponent<Image>();
            CopyImageStyle(sourceImage, panelImage);

            ApplyPopupPanelLayout(panelObj.transform, metrics);
            return panelObj.transform;
        }

        private static GameObject CreateCancelButton(GameObject sourcePopup, GameObject actionTemplateButton, Transform panel)
        {
            if (panel == null) return null;

            Transform sourceCancel = sourcePopup != null ? sourcePopup.transform.Find(UICancelButtonName) : null;
            GameObject cancelButton = null;
            if (sourceCancel != null)
            {
                cancelButton = UnityEngine.Object.Instantiate(sourceCancel.gameObject, panel, false);
            }
            else if (actionTemplateButton != null)
            {
                cancelButton = UnityEngine.Object.Instantiate(actionTemplateButton, panel, false);
            }
            else
            {
                cancelButton = new GameObject(UICancelButtonName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(ButtonDefault));
                cancelButton.transform.SetParent(panel, false);
            }

            cancelButton.name = UICancelButtonName;
            cancelButton.SetActive(true);
            SetLayerRecursively(cancelButton, panel.gameObject.layer);
            SetCanvasGroupsVisible(cancelButton);
            ResetActionButtonLanguageBindings(cancelButton, UIFallbackCancelLabel);

            TextMeshProUGUI text = cancelButton.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault();
            if (text == null)
            {
                text = CreateActionButtonText(cancelButton);
            }
            if (text != null)
            {
                text.gameObject.SetActive(true);
                text.text = UIFallbackCancelLabel;
            }

            ButtonDefault buttonDefault = cancelButton.GetComponent<ButtonDefault>();
            if (buttonDefault != null)
            {
                buttonDefault.DefaultTooltip = string.Empty;
                buttonDefault.SetTooltip(string.Empty);
                buttonDefault.Activate(true, false);
            }

            return cancelButton;
        }

        private static void CopyRectTransform(RectTransform source, RectTransform target)
        {
            if (target == null) return;

            if (source != null)
            {
                target.anchorMin = source.anchorMin;
                target.anchorMax = source.anchorMax;
                target.pivot = source.pivot;
                target.anchoredPosition = source.anchoredPosition;
                target.sizeDelta = source.sizeDelta;
                target.localScale = source.localScale;
                target.localRotation = source.localRotation;
                return;
            }

            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.pivot = new Vector2(0.5f, 0.5f);
            target.anchoredPosition = Vector2.zero;
            target.sizeDelta = Vector2.zero;
            target.localScale = Vector3.one;
            target.localRotation = Quaternion.identity;
        }

        private static void CopyImageStyle(Image source, Image target)
        {
            if (target == null) return;

            if (source == null)
            {
                target.color = Color.white;
                target.raycastTarget = true;
                return;
            }

            target.sprite = source.sprite;
            target.overrideSprite = source.overrideSprite;
            target.type = source.type;
            target.preserveAspect = source.preserveAspect;
            target.fillCenter = source.fillCenter;
            target.fillMethod = source.fillMethod;
            target.fillOrigin = source.fillOrigin;
            target.fillAmount = source.fillAmount;
            target.fillClockwise = source.fillClockwise;
            target.color = source.color;
            target.material = source.material;
            target.raycastTarget = source.raycastTarget;
            target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
        }

        private static void RemoveExistingActionHubPopup(PopupManager manager)
        {
            if (manager == null || manager.popups == null) return;

            int existingIndex = NotFoundIndex;
            for (int i = 0; i < manager.popups.Length; i++)
            {
                PopupManager._popup popup = manager.popups[i];
                if (popup != null && popup.type == (PopupManager._type)CustomPopupID)
                {
                    existingIndex = i;
                    if (popup.obj != null)
                    {
                        UnityEngine.Object.DestroyImmediate(popup.obj);
                    }
                    break;
                }
            }

            if (existingIndex == NotFoundIndex) return;

            List<PopupManager._popup> retainedPopups = new List<PopupManager._popup>();
            for (int i = 0; i < manager.popups.Length; i++)
            {
                if (i != existingIndex)
                {
                    retainedPopups.Add(manager.popups[i]);
                }
            }

            manager.popups = retainedPopups.ToArray();
        }

        private static List<ButtonSectionLayout> CollectButtonSections()
        {
            List<ButtonSectionLayout> sections = new List<ButtonSectionLayout>();
            string relativePath = Path.Combine(TargetDirectory, JsonFileName);

            foreach (Mods._mod mod in Mods._Mods)
            {
                if (!mod.IsEnabled()) continue;

                string filepath = Path.Combine(mod.Path, relativePath).Replace("\\", "/");
                if (!File.Exists(filepath)) continue;

                JSONNode parsed;
                try
                {
                    parsed = JSON.Parse(File.ReadAllText(filepath));
                }
                catch
                {
                    continue;
                }

                JSONArray jsonArray = parsed?.AsArray;
                if (jsonArray == null || jsonArray.Count == 0) continue;

                ModButtonsLocalization.EnsureLoaded(mod.Path);
                string localizedTitle = ModButtonsLocalization.Get(mod.Path, LocKeyModTitle, mod.Title);

                List<ActionDefinition> actions = new List<ActionDefinition>();
                for (int i = 0; i < jsonArray.Count; i++)
                {
                    ActionDefinition action = ParseActionDefinition(mod.Path, jsonArray[i]);
                    if (action != null)
                    {
                        actions.Add(action);
                    }
                }

                if (actions.Count == 0) continue;

                int simpleButtonCount = actions.Count(action => action != null && !action.HasInputs);
                int formActionCount = actions.Count(action => action != null && action.HasInputs);
                bool hasFallbackTextButtons = HasFallbackTextButtons(actions);
                int columnCount = simpleButtonCount > 0
                    ? Math.Min(hasFallbackTextButtons ? FallbackTextButtonsPerRow : IconButtonsPerRow, Math.Max(MinimumButtonCount, simpleButtonCount))
                    : 0;
                float cellWidth = hasFallbackTextButtons ? FallbackBtnMinWidth : MaxCellSize;
                float cellHeight = hasFallbackTextButtons ? FallbackBtnMinHeight : MaxCellSize;
                int rowCount = columnCount > 0 ? Mathf.CeilToInt((float)simpleButtonCount / (float)columnCount) : 0;
                float gridWidth = columnCount > 0
                    ? columnCount * cellWidth + Math.Max(0, columnCount - 1) * GridCellSpacing
                    : 0f;
                float gridHeight = rowCount > 0
                    ? rowCount * cellHeight + Math.Max(0, rowCount - 1) * GridCellSpacing
                    : 0f;
                float formHeight = 0f;
                for (int i = 0; i < actions.Count; i++)
                {
                    ActionDefinition action = actions[i];
                    if (action != null && action.HasInputs)
                    {
                        formHeight += GetActionFormHeight(action);
                    }
                }
                if (formActionCount > 1)
                {
                    formHeight += (formActionCount - 1) * VerticalLayoutSpacing;
                }

                sections.Add(new ButtonSectionLayout
                {
                    Mod = mod,
                    Actions = actions,
                    LocalizedTitle = localizedTitle,
                    HasFallbackTextButtons = hasFallbackTextButtons,
                    ButtonCount = actions.Count,
                    SimpleButtonCount = simpleButtonCount,
                    FormActionCount = formActionCount,
                    ColumnCount = columnCount,
                    RowCount = rowCount,
                    CellWidth = cellWidth,
                    CellHeight = cellHeight,
                    GridWidth = gridWidth,
                    GridHeight = gridHeight,
                    FormHeight = formHeight
                });
            }

            return sections;
        }

        private static ActionDefinition ParseActionDefinition(string modPath, JSONNode item)
        {
            if (item == null)
            {
                return null;
            }

            string fallbackLabel = item[JsonKeyLabel];
            string codeLabel = item[JsonKeyCodeLabel];
            string fallbackTooltip = item[JsonKeyTooltip];
            string codeTooltip = item[JsonKeyCodeTooltip];
            string targetAssembly = item[JsonKeyAssembly];
            string targetClass = item[JsonKeyClass];
            string targetMethod = item[JsonKeyMethod];

            if (string.IsNullOrEmpty(targetAssembly) || string.IsNullOrEmpty(targetClass) || string.IsNullOrEmpty(targetMethod))
            {
                return null;
            }

            if (string.IsNullOrEmpty(codeLabel)) codeLabel = targetClass + DotSeparator + targetMethod;
            if (string.IsNullOrEmpty(fallbackLabel)) fallbackLabel = targetMethod;
            if (string.IsNullOrEmpty(fallbackTooltip)) fallbackTooltip = fallbackLabel;
            if (string.IsNullOrEmpty(codeTooltip)) codeTooltip = codeLabel + "_tooltip";

            ActionDefinition action = new ActionDefinition
            {
                Label = ModButtonsLocalization.Get(modPath, codeLabel, fallbackLabel),
                Tooltip = ModButtonsLocalization.Get(modPath, codeTooltip, fallbackTooltip),
                IconPath = string.IsNullOrEmpty(item[JsonKeyIcon])
                    ? string.Empty
                    : Path.Combine(modPath, TargetDirectory, item[JsonKeyIcon]),
                AssemblyName = targetAssembly,
                ClassName = targetClass,
                MethodName = targetMethod
            };

            JSONArray inputs = item[JsonKeyInputs].AsArray;
            if (inputs == null)
            {
                return action;
            }

            for (int i = 0; i < inputs.Count; i++)
            {
                ActionInputDefinition input = ParseActionInputDefinition(modPath, inputs[i], i);
                if (input != null)
                {
                    action.Inputs.Add(input);
                }
            }

            return action;
        }

        private static ActionInputDefinition ParseActionInputDefinition(string modPath, JSONNode item, int index)
        {
            if (item == null)
            {
                return null;
            }

            string rawType = item[JsonKeyInputType];
            rawType = (rawType ?? string.Empty).Trim().ToLowerInvariant();
            ActionInputKind inputKind;
            switch (rawType)
            {
                case "text":
                case "string":
                    inputKind = ActionInputKind.Text;
                    break;
                case "integer":
                case "int":
                    inputKind = ActionInputKind.Integer;
                    break;
                case "float":
                case "number":
                    inputKind = ActionInputKind.Float;
                    break;
                case "slider":
                    inputKind = ActionInputKind.Slider;
                    break;
                case "dropdown":
                case "select":
                    inputKind = ActionInputKind.Dropdown;
                    break;
                default:
                    return null;
            }

            string id = item[JsonKeyInputId];
            if (string.IsNullOrEmpty(id))
            {
                id = "arg" + index.ToString(CultureInfo.InvariantCulture);
            }

            string fallbackLabel = item[JsonKeyLabel];
            string codeLabel = item[JsonKeyCodeLabel];
            if (string.IsNullOrEmpty(fallbackLabel)) fallbackLabel = id;
            if (string.IsNullOrEmpty(codeLabel)) codeLabel = id;

            string fallbackPlaceholder = item[JsonKeyInputPlaceholder];
            string codePlaceholder = item[JsonKeyInputCodePlaceholder];
            if (string.IsNullOrEmpty(codePlaceholder)) codePlaceholder = fallbackPlaceholder;

            ActionInputDefinition definition = new ActionInputDefinition
            {
                Id = id,
                Label = ModButtonsLocalization.Get(modPath, codeLabel, fallbackLabel),
                Placeholder = ModButtonsLocalization.Get(modPath, codePlaceholder, fallbackPlaceholder),
                InputKind = inputKind,
                ValueKind = ParseActionValueKind(item[JsonKeyInputValueType], inputKind),
                DefaultValue = item[JsonKeyInputDefault]
            };

            definition.HasMinimum = TryParseFloat(item[JsonKeyInputMinimum], out definition.Minimum);
            definition.HasMaximum = TryParseFloat(item[JsonKeyInputMaximum], out definition.Maximum);
            if (definition.HasMinimum && definition.HasMaximum && definition.Maximum < definition.Minimum)
            {
                float temp = definition.Minimum;
                definition.Minimum = definition.Maximum;
                definition.Maximum = temp;
            }

            JSONArray options = item[JsonKeyInputOptions].AsArray;
            if (inputKind == ActionInputKind.Dropdown && options != null)
            {
                for (int i = 0; i < options.Count; i++)
                {
                    JSONNode optionNode = options[i];
                    if (optionNode == null) continue;

                    string optionLabel = optionNode[JsonKeyLabel];
                    string optionValue = optionNode[JsonKeyInputValue];
                    if (string.IsNullOrEmpty(optionLabel)) optionLabel = optionNode.Value;
                    if (string.IsNullOrEmpty(optionValue)) optionValue = optionNode.Value;
                    if (string.IsNullOrEmpty(optionValue)) optionValue = optionLabel;
                    if (string.IsNullOrEmpty(optionLabel)) optionLabel = optionValue;
                    definition.Options.Add(new ActionInputOption { Label = optionLabel, Value = optionValue });
                }
            }

            if (inputKind == ActionInputKind.Dropdown && definition.Options.Count == 0)
            {
                return null;
            }

            return definition;
        }

        private static ActionValueKind ParseActionValueKind(string rawValueType, ActionInputKind inputKind)
        {
            string normalized = (rawValueType ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized == "int" || normalized == "integer") return ActionValueKind.Integer;
            if (normalized == "float" || normalized == "number") return ActionValueKind.Float;
            if (normalized == "string" || normalized == "text") return ActionValueKind.String;

            switch (inputKind)
            {
                case ActionInputKind.Integer: return ActionValueKind.Integer;
                case ActionInputKind.Float: return ActionValueKind.Float;
                case ActionInputKind.Slider: return ActionValueKind.Float;
                default: return ActionValueKind.String;
            }
        }

        private static bool TryParseFloat(string raw, out float value)
        {
            value = 0f;
            if (string.IsNullOrEmpty(raw)) return false;
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                float.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
                float.TryParse(raw, out value);
        }

        private static bool HasFallbackTextButtons(List<ActionDefinition> actions)
        {
            if (actions == null) return true;

            for (int i = 0; i < actions.Count; i++)
            {
                ActionDefinition action = actions[i];
                if (action == null || action.HasInputs) continue;
                if (string.IsNullOrEmpty(action.IconPath) || !File.Exists(action.IconPath)) return true;
            }

            return false;
        }

        private static float GetActionFormHeight(ActionDefinition action)
        {
            if (action == null || !action.HasInputs) return 0f;
            const float FormVerticalPadding = 12f;
            if (action.Inputs.Count == 1) return FormVerticalPadding + FormFieldHeight + FormSpacing + FormValidationHeight;

            int rows = Mathf.CeilToInt((float)action.Inputs.Count / FormInputsPerRow);
            return FormVerticalPadding + FormActionButtonHeight + FormSpacing +
                rows * FormFieldHeight + Mathf.Max(0, rows - 1) * FormSpacing +
                FormSpacing + FormValidationHeight;
        }

        private static PopupLayoutMetrics CalculatePopupLayoutMetrics(List<ButtonSectionLayout> sections)
        {
            float contentWidth = DividerWidth;
            float contentHeight = ContentTopPadding + ContentBottomPadding;
            int childCount = 0;
            int buttonCount = 0;

            if (sections != null)
            {
                for (int i = 0; i < sections.Count; i++)
                {
                    ButtonSectionLayout section = sections[i];
                    if (section == null) continue;

                    contentWidth = Mathf.Max(contentWidth, Mathf.Max(section.GridWidth, section.FormActionCount > 0 ? FormPreferredWidth : 0f));
                    contentHeight += SectionTitleHeight + section.GridHeight + section.FormHeight + DividerHeight;
                    childCount += 2 + (section.GridHeight > 0f ? 1 : 0) + section.FormActionCount;
                    buttonCount += section.ButtonCount;
                }
            }

            if (childCount > 1)
            {
                contentHeight += (childCount - 1) * VerticalLayoutSpacing;
            }

            float maxPanelWidth = Screen.width * PopupMaximumScreenWidthRatio;
            float maxPanelHeight = Screen.height * PopupMaximumScreenHeightRatio;
            if (maxPanelWidth <= 0f) maxPanelWidth = PopupMinimumWidth;
            if (maxPanelHeight <= 0f) maxPanelHeight = PopupMinimumHeight;
            if (buttonCount > ScrollbarButtonCountThreshold)
            {
                float scrollableMaxPanelHeight = Screen.height * PopupScrollableMaximumScreenHeightRatio;
                if (scrollableMaxPanelHeight > PopupScrollableMaximumHeight)
                {
                    scrollableMaxPanelHeight = PopupScrollableMaximumHeight;
                }
                if (scrollableMaxPanelHeight > PopupMinimumHeight)
                {
                    maxPanelHeight = Mathf.Min(maxPanelHeight, scrollableMaxPanelHeight);
                }
            }

            float unconstrainedPanelHeight = contentHeight + PopupTopPadding + PopupBottomPadding;
            float panelHeight = Mathf.Clamp(unconstrainedPanelHeight, PopupMinimumHeight, maxPanelHeight);
            float viewportHeight = Mathf.Max(0f, panelHeight - PopupTopPadding - PopupBottomPadding);
            bool requiresScrollbar = contentHeight > viewportHeight || buttonCount > ScrollbarButtonCountThreshold;
            float rightPadding = requiresScrollbar ? PopupScrollbarRightPadding : PopupHorizontalPadding;
            float scrollbarReserve = requiresScrollbar ? ScrollbarWidth + ScrollbarSpacing : 0f;

            float unconstrainedPanelWidth = contentWidth + PopupHorizontalPadding + rightPadding + scrollbarReserve;
            float panelWidth = Mathf.Clamp(unconstrainedPanelWidth, PopupMinimumWidth, maxPanelWidth);
            float viewportWidth = Mathf.Max(0f, panelWidth - PopupHorizontalPadding - rightPadding - scrollbarReserve);

            return new PopupLayoutMetrics
            {
                ContentWidth = contentWidth,
                ContentHeight = contentHeight,
                PanelWidth = panelWidth,
                PanelHeight = panelHeight,
                ViewportWidth = viewportWidth,
                ViewportHeight = viewportHeight,
                DividerWidth = Mathf.Min(DividerWidth, Mathf.Max(contentWidth, viewportWidth)),
                RightPadding = rightPadding,
                RequiresScrollbar = requiresScrollbar
            };
        }

        private static void ApplyPopupPanelLayout(Transform panel, PopupLayoutMetrics metrics)
        {
            RectTransform panelRect = panel as RectTransform;
            if (panelRect == null || metrics == null) return;

            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(metrics.PanelWidth, metrics.PanelHeight);
        }

        private static void PositionCancelButton(GameObject cancelButton, PopupLayoutMetrics metrics)
        {
            if (cancelButton == null || metrics == null) return;

            RectTransform rect = cancelButton.GetComponent<RectTransform>();
            if (rect == null) return;

            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, CancelButtonBottomOffset);

            float width = Mathf.Min(CancelButtonPreferredWidth, Mathf.Max(0f, metrics.PanelWidth - CancelButtonHorizontalPadding * 2f));
            rect.sizeDelta = new Vector2(width, CancelButtonHeight);
        }

        private static void HideApplyButton(Transform popupRoot)
        {
            if (popupRoot == null) return;

            Transform namedApplyButton = FindNamedChild(popupRoot, UIApplyButtonName);
            if (namedApplyButton != null)
            {
                namedApplyButton.gameObject.SetActive(false);
                return;
            }

            Button[] buttons = popupRoot.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null) continue;
                if (ButtonMatches(button.transform, UIApplyButtonName))
                {
                    button.gameObject.SetActive(false);
                    return;
                }
            }
        }

        private static void CreateScrollArea(
            Transform panel,
            PopupLayoutMetrics metrics,
            Scrollbar scrollbarTemplate,
            out ScrollRect scrollRect,
            out RectTransform contentRect)
        {
            GameObject scrollObj = new GameObject(UIScrollRectName, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollObj.transform.SetParent(panel, false);

            RectTransform scrollAreaRect = scrollObj.GetComponent<RectTransform>();
            scrollAreaRect.anchorMin = Vector2.zero;
            scrollAreaRect.anchorMax = Vector2.one;
            scrollAreaRect.offsetMin = new Vector2(PopupHorizontalPadding, PopupBottomPadding);
            scrollAreaRect.offsetMax = new Vector2(-(metrics != null ? metrics.RightPadding : PopupHorizontalPadding), -PopupTopPadding);

            Image scrollImage = scrollObj.GetComponent<Image>();
            scrollImage.color = new Color(1f, 1f, 1f, TransparentAlpha);
            scrollImage.raycastTarget = true;

            scrollRect = scrollObj.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = metrics != null && metrics.RequiresScrollbar;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = ScrollSensitivity;

            GameObject viewportObj = new GameObject(UIViewportName, typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObj.transform.SetParent(scrollObj.transform, false);

            RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = metrics != null && metrics.RequiresScrollbar
                ? new Vector2(-(ScrollbarWidth + ScrollbarSpacing), 0f)
                : Vector2.zero;

            Image viewportImage = viewportObj.GetComponent<Image>();
            viewportImage.color = HiddenMaskColor;
            viewportImage.raycastTarget = true;
            Mask viewportMask = viewportObj.GetComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            GameObject contentObj = new GameObject(UIVerticalContainerName, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(viewportObj.transform, false);

            contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, metrics != null ? metrics.ContentHeight : 0f);

            VerticalLayoutGroup vLayout = contentObj.GetComponent<VerticalLayoutGroup>();
            vLayout.spacing = VerticalLayoutSpacing;
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlHeight = false;
            vLayout.childControlWidth = false;
            vLayout.childForceExpandHeight = false;
            vLayout.childForceExpandWidth = false;
            vLayout.padding = new RectOffset(0, 0, Mathf.RoundToInt(ContentTopPadding), Mathf.RoundToInt(ContentBottomPadding));

            ContentSizeFitter fitter = contentObj.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            if (metrics != null && metrics.RequiresScrollbar)
            {
                CreateScrollbar(scrollObj.transform, scrollRect, metrics, scrollbarTemplate);
            }
        }

        private static Scrollbar CreateScrollbar(Transform parent, ScrollRect target, PopupLayoutMetrics metrics, Scrollbar template)
        {
            GameObject scrollbarObj;
            Scrollbar scrollbar;
            if (template != null)
            {
                scrollbarObj = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
                scrollbarObj.name = UIScrollbarName;
                SetLayerRecursively(scrollbarObj, parent.gameObject.layer);
                scrollbarObj.SetActive(true);
                scrollbar = scrollbarObj.GetComponent<Scrollbar>() ?? scrollbarObj.AddComponent<Scrollbar>();

                CanvasGroup canvasGroup = scrollbarObj.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
            }
            else
            {
                scrollbarObj = new GameObject(UIScrollbarName, typeof(RectTransform), typeof(Image), typeof(Scrollbar));
                scrollbar = scrollbarObj.GetComponent<Scrollbar>();
            }

            scrollbarObj.transform.SetParent(parent, false);

            RectTransform scrollbarRect = scrollbarObj.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 1f);
            scrollbarRect.sizeDelta = new Vector2(ScrollbarWidth, 0f);
            scrollbarRect.anchoredPosition = new Vector2(ScrollbarRightOffset, 0f);

            Image trackImage = scrollbarObj.GetComponent<Image>();
            if (trackImage != null)
            {
                trackImage.color = template != null ? trackImage.color : ScrollbarTrackColor;
                trackImage.raycastTarget = true;
            }

            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            if (scrollbar.handleRect == null)
            {
                GameObject slidingAreaObj = new GameObject(UIScrollbarSlidingAreaName, typeof(RectTransform));
                slidingAreaObj.transform.SetParent(scrollbarObj.transform, false);

                RectTransform slidingAreaRect = slidingAreaObj.GetComponent<RectTransform>();
                slidingAreaRect.anchorMin = Vector2.zero;
                slidingAreaRect.anchorMax = Vector2.one;
                slidingAreaRect.offsetMin = new Vector2(ScrollbarInset, ScrollbarInset);
                slidingAreaRect.offsetMax = new Vector2(-ScrollbarInset, -ScrollbarInset);

                GameObject handleObj = new GameObject(UIScrollbarHandleName, typeof(RectTransform), typeof(Image));
                handleObj.transform.SetParent(slidingAreaObj.transform, false);

                RectTransform handleRect = handleObj.GetComponent<RectTransform>();
                handleRect.anchorMin = Vector2.zero;
                handleRect.anchorMax = Vector2.one;
                handleRect.offsetMin = Vector2.zero;
                handleRect.offsetMax = Vector2.zero;

                Image handleImage = handleObj.GetComponent<Image>();
                handleImage.color = ScrollbarHandleColor;

                scrollbar.targetGraphic = handleImage;
                scrollbar.handleRect = handleRect;
            }

            if (scrollbar.targetGraphic == null && scrollbar.handleRect != null)
            {
                scrollbar.targetGraphic = scrollbar.handleRect.GetComponent<Graphic>();
            }

            if (metrics != null && metrics.ContentHeight > 0f)
            {
                scrollbar.size = Mathf.Clamp(metrics.ViewportHeight / metrics.ContentHeight, ScrollbarMinimumHandleSize, 1f);
            }

            target.verticalScrollbar = scrollbar;
            target.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            target.verticalScrollbarSpacing = ScrollbarSpacing;

            return scrollbar;
        }

        private static Scrollbar FindScrollbarTemplate(GameObject sourcePopup)
        {
            Scrollbar template = FindScrollbarInRoot(sourcePopup);
            if (template != null) return template;

            PopupManager._type[] preferredPopupTypes = new PopupManager._type[]
            {
                PopupManager._type.settings_difficulty,
                PopupManager._type.notifications,
                PopupManager._type.awards,
                PopupManager._type.single_senbatsu,
                PopupManager._type.single_chart
            };

            for (int i = 0; i < preferredPopupTypes.Length; i++)
            {
                GameObject popup = PopupManager.GetObject(preferredPopupTypes[i]);
                template = FindScrollbarInRoot(popup);
                if (template != null) return template;
            }

            Scrollbar[] sceneScrollbars = UnityEngine.Object.FindObjectsOfType<Scrollbar>();
            for (int i = 0; i < sceneScrollbars.Length; i++)
            {
                Scrollbar scrollbar = sceneScrollbars[i];
                if (scrollbar != null && scrollbar.gameObject.activeInHierarchy)
                {
                    return scrollbar;
                }
            }

            return null;
        }

        private static Scrollbar FindScrollbarInRoot(GameObject root)
        {
            if (root == null) return null;

            Scrollbar[] scrollbars = root.GetComponentsInChildren<Scrollbar>(true);
            for (int i = 0; i < scrollbars.Length; i++)
            {
                Scrollbar scrollbar = scrollbars[i];
                if (scrollbar != null && scrollbar.handleRect != null)
                {
                    return scrollbar;
                }
            }

            return scrollbars.Length > 0 ? scrollbars[0] : null;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null) return;

            root.layer = layer;
            Transform transform = root.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                SetLayerRecursively(transform.GetChild(i).gameObject, layer);
            }
        }

        private static void SetCanvasGroupsVisible(GameObject root)
        {
            if (root == null) return;

            CanvasGroup[] canvasGroups = root.GetComponentsInChildren<CanvasGroup>(true);
            for (int i = 0; i < canvasGroups.Length; i++)
            {
                CanvasGroup canvasGroup = canvasGroups[i];
                if (canvasGroup == null) continue;

                canvasGroup.alpha = OpaqueAlpha;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        private static void FinalizeScrollLayout(RectTransform contentRect, ScrollRect scrollRect, PopupLayoutMetrics metrics)
        {
            if (contentRect == null) return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            if (metrics != null)
            {
                contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, metrics.ContentHeight);
            }

            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
            {
                scrollRect.StopMovement();
                scrollRect.verticalNormalizedPosition = 1f;
                if (scrollRect.verticalScrollbar != null)
                {
                    scrollRect.verticalScrollbar.value = 1f;
                }
            }

            Canvas.ForceUpdateCanvases();
        }

        private static void PopulateCustomButtons(
            Transform verticalContainer,
            GameObject templateButton,
            List<ButtonSectionLayout> sections,
            float dividerWidth)
        {
            if (verticalContainer == null || sections == null) return;

            for (int sectionIndex = 0; sectionIndex < sections.Count; sectionIndex++)
            {
                ButtonSectionLayout section = sections[sectionIndex];
                if (section == null || section.Mod == null || section.Actions == null) continue;

                Mods._mod mod = section.Mod;
                List<ActionDefinition> actions = section.Actions;
                float sectionTitleWidth = Mathf.Max(
                    Mathf.Max(section.GridWidth, section.FormActionCount > 0 ? FormPreferredWidth : 0f),
                    dividerWidth);

                GameObject titleObj = new GameObject(PrefixTitle + mod.Title, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
                titleObj.transform.SetParent(verticalContainer, false);

                RectTransform titleRect = titleObj.GetComponent<RectTransform>();
                titleRect.sizeDelta = new Vector2(sectionTitleWidth, SectionTitleHeight);

                LayoutElement titleLayout = titleObj.GetComponent<LayoutElement>();
                titleLayout.minHeight = SectionTitleHeight;
                titleLayout.preferredHeight = SectionTitleHeight;
                titleLayout.minWidth = sectionTitleWidth;
                titleLayout.preferredWidth = sectionTitleWidth;

                TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
                titleText.text = section.LocalizedTitle;
                titleText.fontSize = TitleFontSize;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.color = Color.black;

                if (section.SimpleButtonCount > 0)
                {
                    GameObject gridContainer = new GameObject(PrefixGrid + mod.Title, typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
                    gridContainer.transform.SetParent(verticalContainer, false);

                    RectTransform gridRect = gridContainer.GetComponent<RectTransform>();
                    gridRect.sizeDelta = new Vector2(section.GridWidth, section.GridHeight);
                    
                    GridLayoutGroup grid = gridContainer.GetComponent<GridLayoutGroup>();
                    grid.cellSize = new Vector2(section.CellWidth, section.CellHeight);
                    grid.spacing = new Vector2(GridCellSpacing, GridCellSpacing);
                    grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                    grid.childAlignment = TextAnchor.UpperCenter;
                    grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    grid.constraintCount = section.ColumnCount;

                    LayoutElement gridLayout = gridContainer.GetComponent<LayoutElement>();
                    gridLayout.minWidth = section.GridWidth;
                    gridLayout.minHeight = section.GridHeight;
                    gridLayout.preferredWidth = section.GridWidth;
                    gridLayout.preferredHeight = section.GridHeight;

                    for (int i = 0; i < actions.Count; i++)
                    {
                        ActionDefinition action = actions[i];
                        if (action == null || action.HasInputs) continue;
                        CreateActionButton(
                            gridContainer.transform,
                            templateButton,
                            action,
                            section.CellWidth,
                            section.CellHeight);
                    }
                }

                for (int i = 0; i < actions.Count; i++)
                {
                    ActionDefinition action = actions[i];
                    if (action == null || !action.HasInputs) continue;
                    CreateActionForm(verticalContainer, templateButton, action);
                }

                GameObject divider = new GameObject(UIDividerName, typeof(RectTransform), typeof(LayoutElement));
                divider.transform.SetParent(verticalContainer, false);

                RectTransform divRect = divider.GetComponent<RectTransform>();
                divRect.sizeDelta = new Vector2(0f, DividerHeight);

                LayoutElement divLayout = divider.GetComponent<LayoutElement>();
                divLayout.minHeight = DividerHeight;
                divLayout.preferredHeight = DividerHeight;
                divLayout.preferredWidth = dividerWidth;

                GameObject dividerLine = new GameObject(UILineName, typeof(RectTransform), typeof(Image));
                dividerLine.transform.SetParent(divider.transform, false);
                dividerLine.GetComponent<Image>().color = DividerColor;

                RectTransform lineRect = dividerLine.GetComponent<RectTransform>();
                lineRect.anchorMin = new Vector2(0.5f, 0.5f);
                lineRect.anchorMax = new Vector2(0.5f, 0.5f);
                lineRect.pivot = new Vector2(0.5f, 0.5f);
                lineRect.anchoredPosition = Vector2.zero;
                lineRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, dividerWidth);
                lineRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, DividerHeight);
            }
        }

        private static bool HasFallbackTextButtons(string modPath, JSONArray jsonArray)
        {
            for (int i = 0; i < jsonArray.Count; i++)
            {
                string iconName = jsonArray[i][JsonKeyIcon];
                if (string.IsNullOrEmpty(iconName)) return true;

                string iconPath = Path.Combine(modPath, TargetDirectory, iconName);
                if (!File.Exists(iconPath)) return true;
            }

            return false;
        }

        private static void CreateActionButton(
            Transform parentGrid,
            GameObject templateBtn,
            ActionDefinition action,
            float cellWidth,
            float cellHeight)
        {
            if (action == null) return;

            string label = action.Label;
            string tooltip = action.Tooltip;
            string iconPath = action.IconPath;
            bool hasIcon = !string.IsNullOrEmpty(iconPath) && File.Exists(iconPath);

            GameObject cellObj = new GameObject(PrefixCell + label, typeof(RectTransform));
            cellObj.transform.SetParent(parentGrid, false);

            RectTransform cellRect = cellObj.GetComponent<RectTransform>();
            cellRect.anchorMin = new Vector2(0.5f, 0.5f);
            cellRect.anchorMax = new Vector2(0.5f, 0.5f);
            cellRect.pivot = new Vector2(0.5f, 0.5f);
            cellRect.sizeDelta = new Vector2(cellWidth, cellHeight);

            GameObject btnObj = templateBtn != null
                ? UnityEngine.Object.Instantiate(templateBtn, cellObj.transform, false)
                : new GameObject(PrefixButton + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(ButtonDefault));
            if (templateBtn == null) btnObj.transform.SetParent(cellObj.transform, false);
            btnObj.name = PrefixButton + label;
            btnObj.SetActive(true);
            SetLayerRecursively(btnObj, parentGrid.gameObject.layer);
            SetCanvasGroupsVisible(btnObj);

            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            // Apply purple tint
            Image bgImage = btnObj.GetComponent<Image>();
            if (bgImage != null) bgImage.raycastTarget = true;
            //if (bgImage != null) bgImage.color = GeneratedButtonColor;

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null) btn.onClick = new Button.ButtonClickedEvent();

            // Set Independent Tooltip
            ButtonDefault btnDef = btnObj.GetComponent<ButtonDefault>();
            if (btnDef != null)
            {
                if (btnDef.OnHover == null) btnDef.OnHover = new ButtonDefault.MyEventType();
                btnDef.DefaultTooltip = tooltip;
                btnDef.SetTooltip(tooltip);
                btnDef.Activate(true, false);
            }

            TextMeshProUGUI textComp = btnObj.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault();
            ResetActionButtonLanguageBindings(btnObj, label);

            if (hasIcon)
            {
                if (textComp != null) textComp.gameObject.SetActive(false);

                GameObject iconObj = new GameObject(UIIconName, typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(btnObj.transform, false);
                
                RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(8, 8);
                iconRect.offsetMax = new Vector2(-8, -8);
                
                Image img = iconObj.GetComponent<Image>();
                img.preserveAspect = true;

                rect.sizeDelta = new Vector2(MinButtonSize, MinButtonSize);

                mainScript runner = Camera.main?.GetComponent<mainScript>();
                if (runner != null)
                {
                    runner.StartCoroutine(LoadTexture.LoadSprite(iconPath, img, delegate 
                    { 
                        if (img.sprite != null && img.sprite.texture != null)
                        {
                            float w = Mathf.Clamp(img.sprite.texture.width, MinButtonSize, MaxButtonSize);
                            float h = Mathf.Clamp(img.sprite.texture.height, MinButtonSize, MaxButtonSize);
                            rect.sizeDelta = new Vector2(w, h);
                        }
                    }));
                }
            }
            else
            {
                if (textComp == null) textComp = CreateActionButtonText(btnObj);
                if (textComp != null)
                {
                    textComp.gameObject.SetActive(true);
                    textComp.text = label;
                    Lang_Button lb = textComp.GetComponent<Lang_Button>();
                    if (lb != null) lb.Constant = label;
                }
                rect.sizeDelta = new Vector2(FallbackBtnMinWidth, FallbackBtnMinHeight);
            }

            LayoutElement layoutElement = btnObj.GetComponent<LayoutElement>() ?? btnObj.AddComponent<LayoutElement>();
            layoutElement.minWidth = hasIcon ? MinButtonSize : FallbackBtnMinWidth;
            layoutElement.minHeight = hasIcon ? MinButtonSize : FallbackBtnMinHeight;
            layoutElement.ignoreLayout = true;

            DisableChildRaycastTargets(btnObj);

            btn?.onClick.AddListener(() => TryInvokeAction(action, null, null));
        }

        private static void CreateActionForm(
            Transform parent,
            GameObject templateButton,
            ActionDefinition action)
        {
            if (parent == null || action == null || !action.HasInputs) return;

            GameObject form = new GameObject(
                PrefixForm + action.Label,
                typeof(RectTransform),
                typeof(Image),
                typeof(VerticalLayoutGroup),
                typeof(LayoutElement));
            form.transform.SetParent(parent, false);
            SetLayerRecursively(form, parent.gameObject.layer);

            Image background = form.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.14f);
            background.raycastTarget = false;

            LayoutElement formLayout = form.GetComponent<LayoutElement>();
            formLayout.minWidth = FormPreferredWidth;
            formLayout.preferredWidth = FormPreferredWidth;
            float formHeight = GetActionFormHeight(action);
            formLayout.minHeight = formHeight;
            formLayout.preferredHeight = formHeight;

            VerticalLayoutGroup formGroup = form.GetComponent<VerticalLayoutGroup>();
            formGroup.padding = new RectOffset(8, 8, 6, 6);
            formGroup.spacing = FormSpacing;
            formGroup.childAlignment = TextAnchor.UpperCenter;
            formGroup.childControlWidth = false;
            formGroup.childControlHeight = false;
            formGroup.childForceExpandWidth = false;
            formGroup.childForceExpandHeight = false;

            List<ActionInputBinding> bindings = new List<ActionInputBinding>();
            Button actionButton;
            if (action.Inputs.Count == 1)
            {
                GameObject row = CreateFormRow(form.transform, "ActionRow", FormFieldHeight);
                HorizontalLayoutGroup rowGroup = row.AddComponent<HorizontalLayoutGroup>();
                rowGroup.spacing = FormSpacing;
                rowGroup.childAlignment = TextAnchor.MiddleCenter;
                rowGroup.childControlWidth = false;
                rowGroup.childControlHeight = false;
                rowGroup.childForceExpandWidth = false;
                rowGroup.childForceExpandHeight = false;

                actionButton = CreateFormActionButton(row.transform, templateButton, action, FormActionButtonWidth, FormActionButtonHeight);
                ActionInputBinding binding = CreateActionInputControl(
                    row.transform,
                    templateButton,
                    action.Inputs[0],
                    FormPreferredWidth - FormActionButtonWidth - FormSpacing - 16f);
                if (binding != null) bindings.Add(binding);
            }
            else
            {
                actionButton = CreateFormActionButton(
                    form.transform,
                    templateButton,
                    action,
                    FormPreferredWidth - 16f,
                    FormActionButtonHeight);

                GameObject grid = CreateFormRow(
                    form.transform,
                    "Inputs",
                    Mathf.CeilToInt((float)action.Inputs.Count / FormInputsPerRow) * FormFieldHeight +
                    Mathf.Max(0, Mathf.CeilToInt((float)action.Inputs.Count / FormInputsPerRow) - 1) * FormSpacing);
                GridLayoutGroup gridLayout = grid.AddComponent<GridLayoutGroup>();
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = FormInputsPerRow;
                gridLayout.cellSize = new Vector2((FormPreferredWidth - 16f - FormSpacing) / FormInputsPerRow, FormFieldHeight);
                gridLayout.spacing = new Vector2(FormSpacing, FormSpacing);
                gridLayout.childAlignment = TextAnchor.UpperCenter;

                for (int i = 0; i < action.Inputs.Count; i++)
                {
                    ActionInputBinding binding = CreateActionInputControl(
                        grid.transform,
                        templateButton,
                        action.Inputs[i],
                        gridLayout.cellSize.x);
                    if (binding != null) bindings.Add(binding);
                }
            }

            TextMeshProUGUI validation = CreateFormValidationText(form.transform);
            if (actionButton != null)
            {
                actionButton.onClick.AddListener(() => TryInvokeAction(action, bindings, validation));
            }
        }

        private static GameObject CreateFormRow(Transform parent, string name, float height)
        {
            GameObject row = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.minWidth = FormPreferredWidth - 16f;
            layout.preferredWidth = FormPreferredWidth - 16f;
            layout.minHeight = height;
            layout.preferredHeight = height;
            return row;
        }

        private static Button CreateFormActionButton(
            Transform parent,
            GameObject templateButton,
            ActionDefinition action,
            float width,
            float height)
        {
            GameObject buttonObject = templateButton != null
                ? UnityEngine.Object.Instantiate(templateButton, parent, false)
                : new GameObject(PrefixButton + action.Label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(ButtonDefault));
            if (templateButton == null) buttonObject.transform.SetParent(parent, false);

            buttonObject.name = PrefixButton + action.Label;
            buttonObject.SetActive(true);
            SetLayerRecursively(buttonObject, parent.gameObject.layer);
            SetCanvasGroupsVisible(buttonObject);

            Button button = buttonObject.GetComponent<Button>() ?? buttonObject.GetComponentInChildren<Button>(true);
            if (button != null)
            {
                button.onClick = new Button.ButtonClickedEvent();
            }

            ButtonDefault buttonDefault = buttonObject.GetComponent<ButtonDefault>();
            if (buttonDefault != null)
            {
                buttonDefault.DefaultTooltip = action.Tooltip;
                buttonDefault.SetTooltip(action.Tooltip);
                buttonDefault.Activate(true, false);
            }

            ResetActionButtonLanguageBindings(buttonObject, action.Label);
            TextMeshProUGUI text = buttonObject.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault();
            if (text == null) text = CreateActionButtonText(buttonObject);
            if (text != null)
            {
                text.gameObject.SetActive(true);
                text.text = action.Label;
                text.enableWordWrapping = false;
                text.overflowMode = TextOverflowModes.Ellipsis;
            }

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(width, height);
            }

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>() ?? buttonObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = false;
            layout.minWidth = width;
            layout.preferredWidth = width;
            layout.minHeight = height;
            layout.preferredHeight = height;
            DisableChildRaycastTargets(buttonObject);
            return button;
        }

        private static ActionInputBinding CreateActionInputControl(
            Transform parent,
            GameObject templateButton,
            ActionInputDefinition definition,
            float width)
        {
            if (parent == null || definition == null) return null;

            GameObject group = new GameObject(
                PrefixInput + definition.Id,
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(LayoutElement));
            group.transform.SetParent(parent, false);
            SetLayerRecursively(group, parent.gameObject.layer);

            LayoutElement groupLayout = group.GetComponent<LayoutElement>();
            groupLayout.minWidth = width;
            groupLayout.preferredWidth = width;
            groupLayout.minHeight = FormFieldHeight;
            groupLayout.preferredHeight = FormFieldHeight;

            VerticalLayoutGroup layout = group.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 2f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI label = CreateFormLabel(group.transform, definition.Label, width);
            ActionInputBinding binding = new ActionInputBinding { Definition = definition };

            switch (definition.InputKind)
            {
                case ActionInputKind.Slider:
                    binding.Slider = CreateSliderControl(group.transform, definition, width);
                    break;
                case ActionInputKind.Dropdown:
                    binding.Dropdown = CreateDropdownControl(group.transform, definition, width, binding, templateButton);
                    break;
                default:
                    binding.TextInput = CreateTextInputControl(group.transform, definition, width);
                    break;
            }

            if (binding.TextInput == null && binding.Slider == null && binding.Dropdown == null &&
                definition.InputKind != ActionInputKind.Dropdown)
            {
                UnityEngine.Object.Destroy(group);
                return null;
            }

            return binding;
        }

        private static TextMeshProUGUI CreateFormLabel(Transform parent, string value, float width)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = value ?? string.Empty;
            label.fontSize = 14f;
            label.alignment = TextAlignmentOptions.BottomLeft;
            label.color = Color.black;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            LayoutElement layout = labelObject.GetComponent<LayoutElement>();
            layout.minWidth = width;
            layout.preferredWidth = width;
            layout.minHeight = FormFieldLabelHeight;
            layout.preferredHeight = FormFieldLabelHeight;
            return label;
        }

        private static TextMeshProUGUI CreateFormValidationText(Transform parent)
        {
            GameObject statusObject = new GameObject("Validation", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            statusObject.transform.SetParent(parent, false);
            TextMeshProUGUI status = statusObject.GetComponent<TextMeshProUGUI>();
            status.text = string.Empty;
            status.fontSize = 12f;
            status.alignment = TextAlignmentOptions.Center;
            status.color = new Color(0.65f, 0.05f, 0.05f, 1f);
            status.enableWordWrapping = false;
            status.overflowMode = TextOverflowModes.Ellipsis;
            status.raycastTarget = false;
            LayoutElement layout = statusObject.GetComponent<LayoutElement>();
            layout.minWidth = FormPreferredWidth - 16f;
            layout.preferredWidth = FormPreferredWidth - 16f;
            layout.minHeight = FormValidationHeight;
            layout.preferredHeight = FormValidationHeight;
            return status;
        }

        private static TMP_InputField CreateTextInputControl(
            Transform parent,
            ActionInputDefinition definition,
            float width)
        {
            TMP_InputField template = FindTemplate<TMP_InputField>();
            GameObject inputObject;
            TMP_InputField input;
            if (template != null)
            {
                inputObject = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
                input = inputObject.GetComponent<TMP_InputField>() ?? inputObject.GetComponentInChildren<TMP_InputField>(true);
            }
            else
            {
                inputObject = CreateFallbackTextInput(parent, out input);
            }

            if (inputObject == null || input == null) return null;

            inputObject.name = "Text";
            inputObject.SetActive(true);
            SetLayerRecursively(inputObject, parent.gameObject.layer);
            input.onValueChanged = new TMP_InputField.OnChangeEvent();
            input.onEndEdit = new TMP_InputField.SubmitEvent();
            input.contentType = definition.InputKind == ActionInputKind.Integer
                ? TMP_InputField.ContentType.IntegerNumber
                : definition.InputKind == ActionInputKind.Float
                    ? TMP_InputField.ContentType.DecimalNumber
                    : TMP_InputField.ContentType.Standard;
            input.text = definition.DefaultValue ?? string.Empty;
            if (input.placeholder is TextMeshProUGUI)
            {
                ((TextMeshProUGUI)input.placeholder).text = definition.Placeholder ?? string.Empty;
            }

            ConfigureControlLayout(inputObject, width, FormControlHeight);
            return input;
        }

        private static GameObject CreateFallbackTextInput(Transform parent, out TMP_InputField input)
        {
            GameObject root = new GameObject("TextInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            root.transform.SetParent(parent, false);
            Image background = root.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.92f);
            input = root.GetComponent<TMP_InputField>();
            input.targetGraphic = background;

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(root.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 2f);
            textRect.offsetMax = new Vector2(-8f, -2f);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = 16f;
            text.color = Color.black;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            input.textComponent = text;

            GameObject placeholderObject = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderObject.transform.SetParent(root.transform, false);
            RectTransform placeholderRect = placeholderObject.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(8f, 2f);
            placeholderRect.offsetMax = new Vector2(-8f, -2f);
            TextMeshProUGUI placeholder = placeholderObject.GetComponent<TextMeshProUGUI>();
            placeholder.fontSize = 16f;
            placeholder.color = new Color(0.35f, 0.35f, 0.35f, 0.8f);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            input.placeholder = placeholder;
            return root;
        }

        private static Slider CreateSliderControl(Transform parent, ActionInputDefinition definition, float width)
        {
            Slider template = FindTemplate<Slider>();
            GameObject sliderObject;
            Slider slider;
            if (template != null)
            {
                sliderObject = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
                slider = sliderObject.GetComponent<Slider>() ?? sliderObject.GetComponentInChildren<Slider>(true);
                Settings_Slider settingsSlider = sliderObject.GetComponent<Settings_Slider>() ?? sliderObject.GetComponentInChildren<Settings_Slider>(true);
                if (settingsSlider != null) settingsSlider.enabled = false;
            }
            else
            {
                sliderObject = CreateFallbackSlider(parent, out slider);
            }

            if (sliderObject == null || slider == null) return null;

            float minimum = definition.HasMinimum ? definition.Minimum : 0f;
            float maximum = definition.HasMaximum ? definition.Maximum : 100f;
            if (maximum < minimum) maximum = minimum;
            float defaultValue;
            if (!TryParseFloat(definition.DefaultValue, out defaultValue)) defaultValue = minimum;

            sliderObject.name = "Slider";
            sliderObject.SetActive(true);
            SetLayerRecursively(sliderObject, parent.gameObject.layer);
            slider.minValue = minimum;
            slider.maxValue = maximum;
            slider.wholeNumbers = definition.ValueKind == ActionValueKind.Integer;
            slider.onValueChanged = new Slider.SliderEvent();
            slider.value = Mathf.Clamp(defaultValue, minimum, maximum);
            ConfigureControlLayout(sliderObject, width, FormControlHeight);
            return slider;
        }

        private static GameObject CreateFallbackSlider(Transform parent, out Slider slider)
        {
            GameObject root = new GameObject("Slider", typeof(RectTransform), typeof(Image), typeof(Slider));
            root.transform.SetParent(parent, false);
            root.GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f, 1f);
            slider = root.GetComponent<Slider>();

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(root.transform, false);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = GeneratedButtonColor;
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.sizeDelta = new Vector2(8f, 0f);
            slider.fillRect = fillRect;

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(root.transform, false);
            Image handleImage = handle.GetComponent<Image>();
            handleImage.color = GeneratedButtonColor;
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(14f, 24f);
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            return root;
        }

        private static CustomDropdown CreateDropdownControl(
            Transform parent,
            ActionInputDefinition definition,
            float width,
            ActionInputBinding binding,
            GameObject templateButton)
        {
            CustomDropdown template = FindTemplate<CustomDropdown>();
            if (template == null)
            {
                CreateDropdownFallback(parent, definition, width, binding, templateButton);
                return null;
            }

            GameObject dropdownObject = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
            CustomDropdown dropdown = dropdownObject.GetComponent<CustomDropdown>() ?? dropdownObject.GetComponentInChildren<CustomDropdown>(true);
            if (dropdown == null)
            {
                UnityEngine.Object.Destroy(dropdownObject);
                return null;
            }

            dropdownObject.name = "Dropdown";
            dropdownObject.SetActive(true);
            SetLayerRecursively(dropdownObject, parent.gameObject.layer);
            dropdown.saveSelected = false;
            dropdown.invokeAtStart = false;
            dropdown.dropdownItems.Clear();
            for (int i = 0; i < definition.Options.Count; i++)
            {
                dropdown.CreateNewItemFast(definition.Options[i].Label, null);
            }

            binding.DropdownIndex = GetDefaultDropdownIndex(definition);
            dropdown.dropdownEvent = new CustomDropdown.DropdownEvent();
            dropdown.dropdownEvent.AddListener(index => binding.DropdownIndex = Mathf.Clamp(index, 0, definition.Options.Count - 1));
            dropdown.SetupDropdown();
            dropdown.ChangeDropdownInfo(binding.DropdownIndex);
            dropdown.selectedItemIndex = binding.DropdownIndex;
            ConfigureControlLayout(dropdownObject, width, FormControlHeight);
            return dropdown;
        }

        private static void CreateDropdownFallback(
            Transform parent,
            ActionInputDefinition definition,
            float width,
            ActionInputBinding binding,
            GameObject templateButton)
        {
            GameObject selectorObject = templateButton != null
                ? UnityEngine.Object.Instantiate(templateButton, parent, false)
                : new GameObject("Dropdown", typeof(RectTransform), typeof(Image), typeof(Button));
            if (templateButton == null) selectorObject.transform.SetParent(parent, false);
            selectorObject.name = "Dropdown";
            selectorObject.SetActive(true);
            SetLayerRecursively(selectorObject, parent.gameObject.layer);
            binding.DropdownIndex = GetDefaultDropdownIndex(definition);

            Button selector = selectorObject.GetComponent<Button>() ?? selectorObject.GetComponentInChildren<Button>(true);
            if (selector == null) return;
            selector.onClick = new Button.ButtonClickedEvent();
            TextMeshProUGUI text = selectorObject.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault();
            if (text == null) text = CreateActionButtonText(selectorObject);
            Action refresh = () =>
            {
                if (text != null && definition.Options.Count > 0)
                {
                    text.text = definition.Options[binding.DropdownIndex].Label;
                }
            };
            selector.onClick.AddListener(() =>
            {
                binding.DropdownIndex = (binding.DropdownIndex + 1) % definition.Options.Count;
                refresh();
            });
            refresh();
            ConfigureControlLayout(selectorObject, width, FormControlHeight);
        }

        private static int GetDefaultDropdownIndex(ActionInputDefinition definition)
        {
            if (definition == null || definition.Options == null || definition.Options.Count == 0) return 0;
            for (int i = 0; i < definition.Options.Count; i++)
            {
                if (string.Equals(definition.Options[i].Value, definition.DefaultValue, StringComparison.Ordinal)) return i;
            }

            return 0;
        }

        private static void ConfigureControlLayout(GameObject control, float width, float height)
        {
            if (control == null) return;
            RectTransform rect = control.GetComponent<RectTransform>();
            if (rect != null) rect.sizeDelta = new Vector2(width, height);
            LayoutElement layout = control.GetComponent<LayoutElement>() ?? control.AddComponent<LayoutElement>();
            layout.ignoreLayout = false;
            layout.minWidth = width;
            layout.preferredWidth = width;
            layout.minHeight = height;
            layout.preferredHeight = height;
        }

        private static T FindTemplate<T>() where T : Component
        {
            PopupManager manager = Camera.main?.GetComponent<mainScript>()?.Data?.GetComponent<PopupManager>();
            if (manager != null && manager.popups != null)
            {
                for (int i = 0; i < manager.popups.Length; i++)
                {
                    PopupManager._popup popup = manager.popups[i];
                    if (popup == null || popup.obj == null) continue;
                    T found = popup.obj.GetComponentInChildren<T>(true);
                    if (found != null) return found;
                }
            }

            return UnityEngine.Object.FindObjectOfType<T>();
        }

        private static bool TryInvokeAction(
            ActionDefinition action,
            List<ActionInputBinding> bindings,
            TextMeshProUGUI validationText)
        {
            if (action == null) return false;

            object[] arguments;
            string error;
            if (!TryReadArguments(action, bindings, out arguments, out error))
            {
                SetValidationMessage(validationText, error);
                return false;
            }

            try
            {
                Assembly targetAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(
                    assembly => assembly != null && string.Equals(assembly.GetName().Name, action.AssemblyName, StringComparison.Ordinal));
                if (targetAssembly == null)
                {
                    SetValidationMessage(validationText, "Target assembly is not loaded.");
                    return false;
                }

                Type targetType = targetAssembly.GetType(action.ClassName, false);
                if (targetType == null)
                {
                    SetValidationMessage(validationText, "Target class was not found.");
                    return false;
                }

                MethodInfo method = FindActionMethod(targetType, action.MethodName, arguments);
                if (method == null)
                {
                    SetValidationMessage(validationText, "No matching public static method was found.");
                    return false;
                }

                method.Invoke(null, arguments);
                CloseActionPopup();
                return true;
            }
            catch (TargetInvocationException exception)
            {
                Exception inner = exception.InnerException ?? exception;
                SetValidationMessage(validationText, inner.Message);
                Debug.LogError($"{LogErrorPrefix} {action.ClassName}.{action.MethodName}: {inner.Message}");
                return false;
            }
            catch (Exception exception)
            {
                SetValidationMessage(validationText, exception.Message);
                Debug.LogError($"{LogErrorPrefix} {action.ClassName}.{action.MethodName}: {exception.Message}");
                return false;
            }
        }

        private static bool TryReadArguments(
            ActionDefinition action,
            List<ActionInputBinding> bindings,
            out object[] arguments,
            out string error)
        {
            arguments = new object[0];
            error = string.Empty;
            if (!action.HasInputs)
            {
                return true;
            }

            if (bindings == null || bindings.Count != action.Inputs.Count)
            {
                error = "The action inputs could not be created.";
                return false;
            }

            arguments = new object[bindings.Count];
            for (int i = 0; i < bindings.Count; i++)
            {
                object value;
                if (!TryReadInputValue(bindings[i], out value, out error))
                {
                    return false;
                }

                arguments[i] = value;
            }

            return true;
        }

        private static bool TryReadInputValue(ActionInputBinding binding, out object value, out string error)
        {
            value = null;
            error = string.Empty;
            if (binding == null || binding.Definition == null)
            {
                error = "An action input is unavailable.";
                return false;
            }

            string raw;
            if (binding.Dropdown != null || binding.Definition.InputKind == ActionInputKind.Dropdown)
            {
                List<ActionInputOption> options = binding.Definition.Options;
                if (options == null || options.Count == 0)
                {
                    error = binding.Definition.Label + " has no options.";
                    return false;
                }

                int index = Mathf.Clamp(binding.DropdownIndex, 0, options.Count - 1);
                raw = options[index].Value;
            }
            else if (binding.Slider != null)
            {
                raw = binding.Slider.value.ToString(CultureInfo.InvariantCulture);
            }
            else if (binding.TextInput != null)
            {
                raw = binding.TextInput.text ?? string.Empty;
            }
            else
            {
                error = binding.Definition.Label + " is unavailable.";
                return false;
            }

            switch (binding.Definition.ValueKind)
            {
                case ActionValueKind.Integer:
                    int integerValue;
                    if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out integerValue) &&
                        !int.TryParse(raw, NumberStyles.Integer, CultureInfo.CurrentCulture, out integerValue))
                    {
                        error = binding.Definition.Label + " must be a whole number.";
                        return false;
                    }
                    if (binding.Definition.HasMinimum && integerValue < Mathf.CeilToInt(binding.Definition.Minimum))
                    {
                        error = binding.Definition.Label + " is below the minimum.";
                        return false;
                    }
                    if (binding.Definition.HasMaximum && integerValue > Mathf.FloorToInt(binding.Definition.Maximum))
                    {
                        error = binding.Definition.Label + " is above the maximum.";
                        return false;
                    }
                    value = integerValue;
                    return true;

                case ActionValueKind.Float:
                    float floatValue;
                    if (!TryParseFloat(raw, out floatValue))
                    {
                        error = binding.Definition.Label + " must be a number.";
                        return false;
                    }
                    if (binding.Definition.HasMinimum && floatValue < binding.Definition.Minimum)
                    {
                        error = binding.Definition.Label + " is below the minimum.";
                        return false;
                    }
                    if (binding.Definition.HasMaximum && floatValue > binding.Definition.Maximum)
                    {
                        error = binding.Definition.Label + " is above the maximum.";
                        return false;
                    }
                    value = floatValue;
                    return true;

                default:
                    value = raw;
                    return true;
            }
        }

        private static MethodInfo FindActionMethod(Type targetType, string methodName, object[] arguments)
        {
            if (targetType == null || string.IsNullOrEmpty(methodName)) return null;
            MethodInfo[] methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo candidate = methods[i];
                if (candidate == null || !string.Equals(candidate.Name, methodName, StringComparison.Ordinal)) continue;
                ParameterInfo[] parameters = candidate.GetParameters();
                if (parameters.Length != arguments.Length) continue;

                bool matches = true;
                for (int parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
                {
                    if (arguments[parameterIndex] == null || parameters[parameterIndex].ParameterType != arguments[parameterIndex].GetType())
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches) return candidate;
            }

            return null;
        }

        private static void SetValidationMessage(TextMeshProUGUI validationText, string message)
        {
            if (validationText != null)
            {
                validationText.text = message ?? string.Empty;
            }
        }

        private static void DisableChildRaycastTargets(GameObject button)
        {
            if (button == null) return;

            Graphic[] graphics = button.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null || graphic.gameObject == button) continue;
                graphic.raycastTarget = false;
            }
        }

        private static void ResetActionButtonLanguageBindings(GameObject root, string label)
        {
            if (root == null) return;

            Lang_Button[] languageBindings = root.GetComponentsInChildren<Lang_Button>(true);
            for (int i = 0; i < languageBindings.Length; i++)
            {
                Lang_Button binding = languageBindings[i];
                if (binding == null) continue;

                binding.Tooltip = string.Empty;
                binding.Constant = binding.GetComponent<TextMeshProUGUI>() != null ? label : string.Empty;
            }
        }

        private static TextMeshProUGUI CreateActionButtonText(GameObject button)
        {
            if (button == null) return null;

            GameObject textObj = new GameObject(UITextName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(button.transform, false);

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
            text.fontSize = 20f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            ButtonDefault buttonDefault = button.GetComponent<ButtonDefault>();
            if (buttonDefault != null && buttonDefault.Text == null) buttonDefault.Text = textObj;

            return text;
        }

        private static void CloseActionPopup()
        {
            try
            {
                PopupManager.Close_();
            }
            catch { }
        }

        private static GameObject CloneMainButton(GameObject oldButton, Transform parent, string name, string label)
        {
            GameObject newBtn = UnityEngine.Object.Instantiate(oldButton, parent);
            newBtn.name = name;
            TextMeshProUGUI text = newBtn.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault();
            if (text != null) text.text = label;
            
            Button btn = newBtn.GetComponent<Button>();
            btn.onClick = new Button.ButtonClickedEvent();
            btn.onClick.AddListener(() => PopupManager.OpenPopup((PopupManager._type)CustomPopupID));
            return newBtn;
        }
    }
}
