using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IMUiFramework
{
    public sealed class VanillaPopupOptions
    {
        public PopupManager._type TemplateType = PopupManager._type.producer_contracts;
        public Transform Parent;
        public string ObjectName = "IMUiFramework_VanillaPopup";
        public string Title;
        public string TitlePath;
        public string PanelPath;
        public string ContentPath;
        public string ScrollRectPath;
        public string CloseButtonPath;
        public bool ClearContent;
        public bool ApplyGameFont = true;
        public bool AutoBindClose = true;
        public bool RegisterWithPopupManager;
        public PopupManager._type RegistrationType;
        public bool InheritBackdrop = true;
        public bool BlurBackground = true;
        public bool DarkenBackground = true;
        public bool ActivateOnCreate;
        public VanillaUiCloneMode CloneMode = VanillaUiCloneMode.Template;
    }

    /// <summary>
    /// Handle returned by the v3 popup factory. It exposes both conventional slots and arbitrary
    /// path/component access so a mod can stay terse without losing access to the exact cloned hierarchy.
    /// </summary>
    public sealed class VanillaPopupHandle
    {
        internal bool RegisteredWithPopupManager;

        public PopupManager._type TemplateType;
        public GameObject Root;
        public Popup Popup;
        public RectTransform PanelRect;
        public Transform ContentRoot;
        public ScrollRect ScrollRect;
        public Button CloseButton;
        public TMP_Text TitleTmp;
        public Text TitleLegacy;
        public VanillaPopupDescriptor Source;

        public bool IsValid
        {
            get { return Root != null && Popup != null; }
        }

        public bool IsRegistered
        {
            get { return RegisteredWithPopupManager; }
        }

        public Transform Find(string relativePathOrName)
        {
            return VanillaUiPopupFactory.FindInHierarchy(Root != null ? Root.transform : null, relativePathOrName);
        }

        public T FindComponent<T>(string relativePathOrName = null) where T : Component
        {
            if (Root == null)
            {
                return null;
            }
            if (string.IsNullOrEmpty(relativePathOrName))
            {
                T direct = Root.GetComponent<T>();
                return direct != null ? direct : Root.GetComponentInChildren<T>(true);
            }
            Transform transform = Find(relativePathOrName);
            if (transform == null)
            {
                return null;
            }
            T component = transform.GetComponent<T>();
            return component != null ? component : transform.GetComponentInChildren<T>(true);
        }

        public bool SetText(string relativePathOrName, string text, bool enableWordWrap = true)
        {
            Transform target = Find(relativePathOrName);
            if (target == null)
            {
                return false;
            }
            IMUiKit.ClearLocalizationComponents(target.gameObject);
            IMUiKit.SetText(target.gameObject, text ?? string.Empty, enableWordWrap);
            return true;
        }

        public bool BindButton(string relativePathOrName, UnityAction action, bool replaceExistingListeners = true)
        {
            Transform target = Find(relativePathOrName);
            if (target == null)
            {
                return false;
            }

            Button button = target.GetComponent<Button>();
            if (button == null)
            {
                button = target.GetComponentInChildren<Button>(true);
            }
            if (button == null)
            {
                return false;
            }

            if (replaceExistingListeners)
            {
                button.onClick = new Button.ButtonClickedEvent();
            }
            if (action != null)
            {
                button.onClick.AddListener(action);
            }
            return true;
        }

        public bool Clear(string relativePathOrName)
        {
            Transform target = Find(relativePathOrName);
            if (target == null)
            {
                return false;
            }
            IMUiKit.ClearChildren(target);
            return true;
        }

        /// <summary>
        /// Creates a shipped Modern UI Pack control directly inside this popup's detected content
        /// slot (or panel when the template has no content slot).
        /// </summary>
        public bool TryAddControl(VanillaControlType type, string objectName, out GameObject instance)
        {
            instance = null;
            Transform parent = ContentRoot != null
                ? ContentRoot
                : (PanelRect != null ? PanelRect : (Root != null ? Root.transform : null));
            return parent != null && VanillaUiControlFactory.TryCreate(type, parent, objectName, out instance);
        }

        /// <summary>
        /// Creates one exact resource prefab variant inside this popup without requiring separate
        /// theme/font setup code.
        /// </summary>
        public bool TryAddResource(string resourcePath, string objectName, out GameObject instance)
        {
            instance = null;
            Transform parent = ContentRoot != null
                ? ContentRoot
                : (PanelRect != null ? PanelRect : (Root != null ? Root.transform : null));
            return parent != null && VanillaUiControlFactory.TryCreateResource(resourcePath, parent, objectName, true, out instance);
        }

        /// <summary>
        /// Clones any scene-native vanilla widget into this popup's content slot. This is useful for
        /// rows, headers, tab strips and other composites that do not exist as Resources prefabs.
        /// </summary>
        public bool TryAddSceneTemplate(string hierarchyPathOrName, string objectName, out VanillaSceneTemplateHandle template)
        {
            return TryAddSceneTemplate(hierarchyPathOrName, 0, objectName, out template);
        }

        public bool TryAddSceneTemplate(string hierarchyPathOrName, int occurrenceIndex, string objectName, out VanillaSceneTemplateHandle template)
        {
            Transform parent = ContentRoot != null
                ? ContentRoot
                : (PanelRect != null ? PanelRect : (Root != null ? Root.transform : null));
            return VanillaUiSceneFactory.TryCreate(hierarchyPathOrName, occurrenceIndex, parent, objectName, out template);
        }

        /// <summary>
        /// Clones a vanilla UI prefab held in a serialized field of any popup controller. This covers
        /// repeated UI pieces that are neither scene children nor Resources prefabs, such as prefab_line,
        /// prefab_button, prefab_stat and popup-specific item prefabs. The source popup stays closed.
        /// </summary>
        public bool TryAddPopupReferencedTemplate(
            PopupManager._type sourcePopupType,
            string componentTypeName,
            string fieldName,
            int elementIndex,
            string objectName,
            out GameObject instance)
        {
            instance = null;
            Transform parent = ContentRoot != null
                ? ContentRoot
                : (PanelRect != null ? PanelRect : (Root != null ? Root.transform : null));
            if (parent == null)
            {
                return false;
            }

            return VanillaUiReferenceTemplates.TryClonePopupTemplate(
                sourcePopupType,
                componentTypeName,
                fieldName,
                elementIndex,
                parent,
                objectName,
                VanillaUiCloneMode.Template,
                true,
                out instance);
        }

        /// <summary>
        /// Convenience overload for scalar prefab fields on this handle's source popup type.
        /// </summary>
        public bool TryAddReferencedTemplate(
            string componentTypeName,
            string fieldName,
            string objectName,
            out GameObject instance)
        {
            return TryAddPopupReferencedTemplate(
                TemplateType,
                componentTypeName,
                fieldName,
                -1,
                objectName,
                out instance);
        }

        public void SetTitle(string title)
        {
            string resolved = title ?? string.Empty;
            if (TitleTmp != null)
            {
                IMUiKit.ClearLocalizationComponents(TitleTmp.gameObject);
                TitleTmp.text = resolved;
            }
            if (TitleLegacy != null)
            {
                IMUiKit.ClearLocalizationComponents(TitleLegacy.gameObject);
                TitleLegacy.text = resolved;
            }
        }

        public void ClearContent()
        {
            if (ContentRoot != null)
            {
                IMUiKit.ClearChildren(ContentRoot);
            }
        }

        public void Show()
        {
            if (RegisteredWithPopupManager)
            {
                try
                {
                    PopupManager.OpenPopup(RegistrationType);
                    return;
                }
                catch
                {
                }
            }
            if (Root != null)
            {
                Root.SetActive(true);
            }
        }

        internal PopupManager._type RegistrationType;

        public void Close(Action onComplete = null)
        {
            if (RegisteredWithPopupManager)
            {
                try
                {
                    PopupManager.Close_(onComplete);
                    return;
                }
                catch
                {
                }
            }

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

    /// <summary>
    /// Fluent convenience wrapper around VanillaUiPopupFactory. It is intentionally thin: every option
    /// maps directly to VanillaPopupOptions, so mods can start concise and still drop to the lower level.
    /// </summary>
    public sealed class VanillaPopupBuilder
    {
        private readonly VanillaPopupOptions options = new VanillaPopupOptions();

        private VanillaPopupBuilder(PopupManager._type templateType)
        {
            options.TemplateType = templateType;
        }

        public static VanillaPopupBuilder From(PopupManager._type templateType)
        {
            return new VanillaPopupBuilder(templateType);
        }

        public VanillaPopupBuilder Named(string objectName)
        {
            options.ObjectName = objectName;
            return this;
        }

        public VanillaPopupBuilder WithTitle(string title)
        {
            options.Title = title;
            return this;
        }

        public VanillaPopupBuilder TitleAt(string relativePath)
        {
            options.TitlePath = relativePath;
            return this;
        }

        public VanillaPopupBuilder Under(Transform parent)
        {
            options.Parent = parent;
            return this;
        }

        public VanillaPopupBuilder ContentAt(string relativePath, bool clearExisting)
        {
            options.ContentPath = relativePath;
            options.ClearContent = clearExisting;
            return this;
        }

        public VanillaPopupBuilder ScrollAt(string relativePath)
        {
            options.ScrollRectPath = relativePath;
            return this;
        }

        public VanillaPopupBuilder CloseAt(string relativePath)
        {
            options.CloseButtonPath = relativePath;
            return this;
        }

        public VanillaPopupBuilder PanelAt(string relativePath)
        {
            options.PanelPath = relativePath;
            return this;
        }

        public VanillaPopupBuilder RegisterAs(PopupManager._type registrationType)
        {
            options.RegisterWithPopupManager = true;
            options.RegistrationType = registrationType;
            return this;
        }

        public VanillaPopupBuilder Backdrop(bool blur, bool darken)
        {
            options.InheritBackdrop = false;
            options.BlurBackground = blur;
            options.DarkenBackground = darken;
            return this;
        }

        public VanillaPopupBuilder Exact()
        {
            options.CloneMode = VanillaUiCloneMode.Exact;
            return this;
        }

        public VanillaPopupBuilder VisualOnly()
        {
            options.CloneMode = VanillaUiCloneMode.VisualOnly;
            return this;
        }

        public VanillaPopupBuilder ActivateOnCreate(bool activate)
        {
            options.ActivateOnCreate = activate;
            return this;
        }

        public VanillaPopupBuilder ClearDetectedContent(bool clear = true)
        {
            options.ClearContent = clear;
            return this;
        }

        public VanillaPopupBuilder UseGameFont(bool useGameFont = true)
        {
            options.ApplyGameFont = useGameFont;
            return this;
        }

        public VanillaPopupBuilder AutoClose(bool autoBind = true)
        {
            options.AutoBindClose = autoBind;
            return this;
        }

        public bool Build(out VanillaPopupHandle handle)
        {
            return VanillaUiPopupFactory.TryCreate(options, out handle);
        }
    }

    /// <summary>
    /// Creates mod popups by cloning the actual serialized vanilla popup hierarchy. This is the v3 answer
    /// to large hand-built scaffold methods: dimensions, sprites, masks, anchors, layout, animation settings,
    /// and scene-specific composite controls start life exactly as Idol Manager serialized them.
    /// </summary>
    public static class VanillaUiPopupFactory
    {
        private static readonly string[] TitleTokens = new string[] { "title", "header", "heading" };
        private static readonly string[] ContentTokens = new string[] { "content", "container", "grid", "list" };
        private static readonly string[] CloseTokens = new string[] { "close", "ok", "back", "cancel" };
        private static readonly string[] PanelTokens = new string[] { "panel", "window", "body" };

        public static bool TryCreate(VanillaPopupOptions options, out VanillaPopupHandle handle)
        {
            handle = null;
            if (options == null)
            {
                return false;
            }

            VanillaPopupDescriptor source;
            if (!VanillaUiSceneCatalog.TryDescribePopup(options.TemplateType, out source) || source == null || !source.HasRoot)
            {
                return false;
            }

            Transform parent = options.Parent;
            if (parent == null && source.Root != null && source.Root.transform.parent != null)
            {
                // Prefer the source popup's own serialized parent. This works for gameplay and
                // main-menu popup canvases even when IMUiKit.GetPopupParent cannot reach a manager.
                parent = source.Root.transform.parent;
            }
            if (parent == null)
            {
                parent = IMUiKit.GetPopupParent();
            }
            GameObject root;
            if (!VanillaUiSceneCatalog.TryClonePopup(
                options.TemplateType,
                parent,
                options.ObjectName,
                options.CloneMode,
                false,
                out root) || root == null)
            {
                return false;
            }

            VanillaPopupHandle created = new VanillaPopupHandle();
            created.TemplateType = options.TemplateType;
            created.Root = root;
            created.Source = source;
            created.Popup = root.GetComponent<Popup>();
            if (created.Popup == null)
            {
                created.Popup = root.AddComponent<Popup>();
                created.Popup.OnOpen = new UnityEvent();
                created.Popup.ShowAnimation = true;
                created.Popup.HideAnimation = true;
                created.Popup.Increase_Popup_Counter = true;
            }
            else if (options.CloneMode != VanillaUiCloneMode.Exact)
            {
                created.Popup.OnOpen = new UnityEvent();
            }

            created.PanelRect = ResolveRect(root.transform, options.PanelPath, PanelTokens);
            created.ScrollRect = ResolveScrollRect(root.transform, options.ScrollRectPath);
            created.ContentRoot = ResolveContentRoot(root.transform, options.ContentPath, created.ScrollRect);
            created.CloseButton = ResolveButton(root.transform, options.CloseButtonPath, CloseTokens);
            ResolveTitle(root.transform, options.TitlePath, out created.TitleTmp, out created.TitleLegacy);

            if (!string.IsNullOrEmpty(options.Title))
            {
                created.SetTitle(options.Title);
            }
            if (options.ClearContent && created.ContentRoot != null)
            {
                created.ClearContent();
            }
            if (options.ApplyGameFont)
            {
                VanillaUiFonts.ApplyGameFont(root, true);
            }
            if (options.AutoBindClose && created.CloseButton != null)
            {
                created.CloseButton.onClick = new Button.ButtonClickedEvent();
                created.CloseButton.onClick.AddListener(delegate
                {
                    created.Close(null);
                });
            }

            if (options.RegisterWithPopupManager)
            {
                bool blur = options.InheritBackdrop ? source.BlurBackground : options.BlurBackground;
                bool darken = options.InheritBackdrop ? source.DarkenBackground : options.DarkenBackground;
                if (!IMUiKit.TryRegisterPopup(options.RegistrationType, root, blur, darken))
                {
                    UnityEngine.Object.Destroy(root);
                    return false;
                }
                created.RegisteredWithPopupManager = true;
                created.RegistrationType = options.RegistrationType;
            }

            root.SetActive(options.ActivateOnCreate);
            handle = created;
            return true;
        }

        /// <summary>
        /// Concise path for normal mod popups: clone one vanilla popup, register under a mod-reserved
        /// PopupManager enum value, inherit its backdrop, and optionally clear the detected content slot.
        /// </summary>
        public static bool TryCreateModPopup(
            PopupManager._type templateType,
            PopupManager._type registrationType,
            string objectName,
            string title,
            bool clearContent,
            out VanillaPopupHandle handle)
        {
            VanillaPopupOptions options = new VanillaPopupOptions();
            options.TemplateType = templateType;
            options.RegistrationType = registrationType;
            options.RegisterWithPopupManager = true;
            options.ObjectName = objectName;
            options.Title = title;
            options.ClearContent = clearContent;
            options.CloneMode = VanillaUiCloneMode.Template;
            return TryCreate(options, out handle);
        }

        public static Transform FindInHierarchy(Transform root, string relativePathOrName)
        {
            if (root == null || string.IsNullOrEmpty(relativePathOrName))
            {
                return null;
            }

            string normalized = relativePathOrName.Replace('\\', '/').Trim().Trim('/');
            Transform direct = root.Find(normalized);
            if (direct != null)
            {
                return direct;
            }

            return FindFirstByName(root, normalized);
        }

        private static RectTransform ResolveRect(Transform root, string path, string[] tokens)
        {
            if (!string.IsNullOrEmpty(path))
            {
                Transform requested = FindInHierarchy(root, path);
                if (requested != null)
                {
                    RectTransform requestedRect = requested as RectTransform;
                    if (requestedRect != null)
                    {
                        return requestedRect;
                    }
                }
            }

            Transform byToken = FindFirstByTokens(root, tokens, true);
            RectTransform rect = byToken as RectTransform;
            if (rect != null)
            {
                return rect;
            }
            return root as RectTransform;
        }

        private static ScrollRect ResolveScrollRect(Transform root, string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                Transform requested = FindInHierarchy(root, path);
                if (requested != null)
                {
                    ScrollRect scroll = requested.GetComponent<ScrollRect>();
                    if (scroll == null)
                    {
                        scroll = requested.GetComponentInChildren<ScrollRect>(true);
                    }
                    if (scroll != null)
                    {
                        return scroll;
                    }
                }
            }
            return root.GetComponentInChildren<ScrollRect>(true);
        }

        private static Transform ResolveContentRoot(Transform root, string path, ScrollRect scrollRect)
        {
            if (!string.IsNullOrEmpty(path))
            {
                Transform requested = FindInHierarchy(root, path);
                if (requested != null)
                {
                    return requested;
                }
            }

            if (scrollRect != null && scrollRect.content != null)
            {
                return scrollRect.content;
            }

            Transform byToken = FindFirstByTokens(root, ContentTokens, false);
            // Never fall back to the popup root. A caller asking to clear content must not be able
            // to erase the entire cloned popup merely because this template has no conventional
            // content container. Use an explicit ContentPath for unusual layouts.
            return byToken;
        }

        private static Button ResolveButton(Transform root, string path, string[] tokens)
        {
            if (!string.IsNullOrEmpty(path))
            {
                Transform requested = FindInHierarchy(root, path);
                if (requested != null)
                {
                    Button button = requested.GetComponent<Button>();
                    if (button == null)
                    {
                        button = requested.GetComponentInChildren<Button>(true);
                    }
                    if (button != null)
                    {
                        return button;
                    }
                }
            }

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int tokenIndex = 0; tokenIndex < tokens.Length; tokenIndex++)
            {
                string token = tokens[tokenIndex];
                for (int i = 0; i < buttons.Length; i++)
                {
                    Button button = buttons[i];
                    if (button != null && ContainsToken(button.gameObject.name, token))
                    {
                        return button;
                    }
                }
            }
            return null;
        }

        private static void ResolveTitle(Transform root, string path, out TMP_Text tmp, out Text legacy)
        {
            tmp = null;
            legacy = null;

            if (!string.IsNullOrEmpty(path))
            {
                Transform requested = FindInHierarchy(root, path);
                if (requested != null)
                {
                    tmp = requested.GetComponent<TMP_Text>();
                    if (tmp == null)
                    {
                        tmp = requested.GetComponentInChildren<TMP_Text>(true);
                    }
                    if (tmp != null)
                    {
                        return;
                    }

                    legacy = requested.GetComponent<Text>();
                    if (legacy == null)
                    {
                        legacy = requested.GetComponentInChildren<Text>(true);
                    }
                    if (legacy != null)
                    {
                        return;
                    }
                }
            }

            TMP_Text[] tmpTexts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int tokenIndex = 0; tokenIndex < TitleTokens.Length && tmp == null; tokenIndex++)
            {
                for (int i = 0; i < tmpTexts.Length; i++)
                {
                    if (tmpTexts[i] != null && ContainsToken(tmpTexts[i].gameObject.name, TitleTokens[tokenIndex]))
                    {
                        tmp = tmpTexts[i];
                        break;
                    }
                }
            }
            if (tmp != null)
            {
                return;
            }

            Text[] legacyTexts = root.GetComponentsInChildren<Text>(true);
            for (int tokenIndex = 0; tokenIndex < TitleTokens.Length && legacy == null; tokenIndex++)
            {
                for (int i = 0; i < legacyTexts.Length; i++)
                {
                    if (legacyTexts[i] != null && ContainsToken(legacyTexts[i].gameObject.name, TitleTokens[tokenIndex]))
                    {
                        legacy = legacyTexts[i];
                        break;
                    }
                }
            }
        }

        private static Transform FindFirstByTokens(Transform root, string[] tokens, bool directChildrenFirst)
        {
            if (root == null || tokens == null)
            {
                return null;
            }

            if (directChildrenFirst)
            {
                for (int tokenIndex = 0; tokenIndex < tokens.Length; tokenIndex++)
                {
                    for (int i = 0; i < root.childCount; i++)
                    {
                        Transform child = root.GetChild(i);
                        if (child != null && ContainsToken(child.name, tokens[tokenIndex]))
                        {
                            return child;
                        }
                    }
                }
            }

            for (int tokenIndex = 0; tokenIndex < tokens.Length; tokenIndex++)
            {
                Transform found = FindFirstByTokenRecursive(root, tokens[tokenIndex]);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static Transform FindFirstByTokenRecursive(Transform root, string token)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                {
                    continue;
                }
                if (ContainsToken(child.name, token))
                {
                    return child;
                }
                Transform nested = FindFirstByTokenRecursive(child, token);
                if (nested != null)
                {
                    return nested;
                }
            }
            return null;
        }

        private static Transform FindFirstByName(Transform root, string name)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                {
                    continue;
                }
                if (string.Equals(child.name, name, StringComparison.Ordinal))
                {
                    return child;
                }
                Transform nested = FindFirstByName(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }
            return null;
        }

        private static bool ContainsToken(string value, string token)
        {
            return !string.IsNullOrEmpty(value) &&
                   !string.IsNullOrEmpty(token) &&
                   value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
