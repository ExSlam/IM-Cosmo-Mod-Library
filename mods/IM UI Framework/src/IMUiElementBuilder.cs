using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IMUiFramework
{
    public enum IMUiElementSourceKind
    {
        PopupChild,
        SceneObject,
        Resource,
        ResourceControl
    }

    /// <summary>
    /// A generic handle for any composable v3 element, whether it came from a closed vanilla popup,
    /// another scene object, or Michsky/Resources. This is the universal custom-UI building block.
    /// </summary>
    public sealed class IMUiElementHandle
    {
        public GameObject Root;
        public GameObject Source;
        public IMUiElementSourceKind SourceKind;

        public bool IsValid
        {
            get { return Root != null; }
        }

        public T Get<T>() where T : Component
        {
            if (Root == null) return null;
            T component = Root.GetComponent<T>();
            return component != null ? component : Root.GetComponentInChildren<T>(true);
        }

        public Transform Find(string relativePathOrName)
        {
            return VanillaUiPopupFactory.FindInHierarchy(Root != null ? Root.transform : null, relativePathOrName);
        }

        public void SetText(string value)
        {
            if (Root == null) return;
            IMUiKit.ClearLocalizationComponents(Root);
            IMUiKit.SetText(Root, value ?? string.Empty, true);
        }

        public void BindClick(UnityAction action, bool replaceExistingListeners = true)
        {
            Button button = Get<Button>();
            if (button == null) return;
            if (replaceExistingListeners) button.onClick = new Button.ButtonClickedEvent();
            if (action != null) button.onClick.AddListener(action);
        }

        public void ApplyTheme(IMUiTheme theme, IMUiThemeApplication application)
        {
            if (Root == null || theme == null) return;
            IMUiStyle.ApplyTheme(Root, theme, application, true);
        }
    }

    /// <summary>
    /// Fluent component-level cloning. Unlike a whole-popup scaffold, this lets a custom popup borrow
    /// only the vanilla pieces it wants, keep their exact sprites/geometry/hover behavior, then override
    /// text, colors, size, callbacks, or font.
    /// </summary>
    public sealed class IMUiElementBuilder
    {
        private IMUiElementSourceKind sourceKind;
        private PopupManager._type popupType;
        private string popupPath;
        private string scenePath;
        private int sceneOccurrence;
        private string resourcePath;
        private VanillaControlType controlType;

        private Transform parent;
        private string objectName;
        private string text;
        private bool hasText;
        private bool active = true;
        private bool applyGameFont = true;
        private VanillaUiCloneMode cloneMode = VanillaUiCloneMode.Template;
        private IMUiTheme theme;
        private IMUiThemeApplication themeApplication = IMUiThemeApplication.None;
        private float width = -1f;
        private float height = -1f;
        private float fontSize = -1f;
        private UnityAction onClick;
        private Action<GameObject> configure;
        private readonly List<GraphicColorOverride> graphicColorOverrides = new List<GraphicColorOverride>();

        private sealed class GraphicColorOverride
        {
            public string PathOrName;
            public bool UseRole;
            public IMUiColorRole Role;
            public Color32 Color;
        }

        private IMUiElementBuilder()
        {
        }

        public static IMUiElementBuilder FromPopup(PopupManager._type type, string relativePath)
        {
            IMUiElementBuilder builder = new IMUiElementBuilder();
            builder.sourceKind = IMUiElementSourceKind.PopupChild;
            builder.popupType = type;
            builder.popupPath = relativePath;
            return builder;
        }

        public static IMUiElementBuilder FromScene(string hierarchyPathOrName, int occurrenceIndex = 0)
        {
            IMUiElementBuilder builder = new IMUiElementBuilder();
            builder.sourceKind = IMUiElementSourceKind.SceneObject;
            builder.scenePath = hierarchyPathOrName;
            builder.sceneOccurrence = Mathf.Max(0, occurrenceIndex);
            return builder;
        }

        public static IMUiElementBuilder FromResource(string path)
        {
            IMUiElementBuilder builder = new IMUiElementBuilder();
            builder.sourceKind = IMUiElementSourceKind.Resource;
            builder.resourcePath = path;
            return builder;
        }

        public static IMUiElementBuilder FromControl(VanillaControlType type)
        {
            IMUiElementBuilder builder = new IMUiElementBuilder();
            builder.sourceKind = IMUiElementSourceKind.ResourceControl;
            builder.controlType = type;
            return builder;
        }

        public IMUiElementBuilder Parent(Transform value)
        {
            parent = value;
            return this;
        }

        public IMUiElementBuilder Named(string value)
        {
            objectName = value;
            return this;
        }

        public IMUiElementBuilder Text(string value)
        {
            text = value;
            hasText = true;
            return this;
        }

        public IMUiElementBuilder Active(bool value)
        {
            active = value;
            return this;
        }

        public IMUiElementBuilder CloneMode(VanillaUiCloneMode value)
        {
            cloneMode = value;
            return this;
        }

        public IMUiElementBuilder Theme(IMUiTheme value, IMUiThemeApplication application = IMUiThemeApplication.Interactive)
        {
            theme = value;
            themeApplication = application;
            return this;
        }

        public IMUiElementBuilder ApplyGameFont(bool value)
        {
            applyGameFont = value;
            return this;
        }

        public IMUiElementBuilder Size(float targetWidth, float targetHeight)
        {
            width = targetWidth;
            height = targetHeight;
            return this;
        }

        public IMUiElementBuilder FontSize(float value)
        {
            fontSize = value;
            return this;
        }

        public IMUiElementBuilder OnClick(UnityAction action)
        {
            onClick = action;
            return this;
        }

        public IMUiElementBuilder Configure(Action<GameObject> action)
        {
            configure = action;
            return this;
        }

        /// <summary>
        /// Applies an explicit color to a Graphic on the cloned element after broad semantic theming.
        /// This is useful when a custom UI borrows a complex vanilla scene piece but wants one
        /// particular child (for example Background, Fill, Header, or Label) to use a different role.
        /// </summary>
        public IMUiElementBuilder GraphicColor(string relativePathOrName, IMUiColorRole role)
        {
            GraphicColorOverride item = new GraphicColorOverride();
            item.PathOrName = relativePathOrName;
            item.UseRole = true;
            item.Role = role;
            graphicColorOverrides.Add(item);
            return this;
        }

        public IMUiElementBuilder GraphicColor(string relativePathOrName, Color32 color)
        {
            GraphicColorOverride item = new GraphicColorOverride();
            item.PathOrName = relativePathOrName;
            item.UseRole = false;
            item.Color = color;
            graphicColorOverrides.Add(item);
            return this;
        }

        public bool Build(out IMUiElementHandle handle)
        {
            handle = null;
            GameObject instance = null;
            GameObject source = null;

            switch (sourceKind)
            {
                case IMUiElementSourceKind.PopupChild:
                    Transform popupSource;
                    VanillaUiSceneCatalog.TryFindPopupChild(popupType, popupPath, out popupSource);
                    source = popupSource != null ? popupSource.gameObject : null;
                    if (!VanillaUiSceneCatalog.TryClonePopupChild(
                        popupType,
                        popupPath,
                        parent,
                        objectName,
                        cloneMode,
                        false,
                        out instance))
                    {
                        return false;
                    }
                    break;

                case IMUiElementSourceKind.SceneObject:
                    VanillaUiSceneCatalog.TryFindSceneObject(scenePath, sceneOccurrence, out source);
                    if (!VanillaUiSceneCatalog.TryCloneSceneObject(
                        scenePath,
                        sceneOccurrence,
                        parent,
                        objectName,
                        cloneMode,
                        false,
                        out instance))
                    {
                        return false;
                    }
                    break;

                case IMUiElementSourceKind.Resource:
                    VanillaUiThemeSettings resourceTheme = theme != null ? IMUiMuipThemeBridge.CreateSettings(theme) : null;
                    instance = resourceTheme == null
                        ? VanillaUiResources.InstantiatePrefab(resourcePath, parent, objectName, false, (Michsky.UI.ModernUIPack.UIManager)null)
                        : VanillaUiResources.InstantiatePrefab(resourcePath, parent, objectName, false, resourceTheme);
                    if (instance == null) return false;
                    source = VanillaUiResources.LoadPrefab(resourcePath);
                    break;

                case IMUiElementSourceKind.ResourceControl:
                    string path = VanillaUiControlFactory.GetDefaultResourcePath(controlType);
                    VanillaUiThemeSettings controlTheme = theme != null ? IMUiMuipThemeBridge.CreateSettings(theme) : null;
                    instance = controlTheme == null
                        ? VanillaUiResources.InstantiatePrefab(path, parent, objectName, false, (Michsky.UI.ModernUIPack.UIManager)null)
                        : VanillaUiResources.InstantiatePrefab(path, parent, objectName, false, controlTheme);
                    if (instance == null) return false;
                    source = VanillaUiResources.LoadPrefab(path);
                    break;
            }

            if (instance == null) return false;

            if (hasText)
            {
                IMUiKit.ClearLocalizationComponents(instance);
                IMUiKit.SetText(instance, text ?? string.Empty, true);
            }

            Button button = instance.GetComponent<Button>();
            if (button == null) button = instance.GetComponentInChildren<Button>(true);
            if (button != null && onClick != null)
            {
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(onClick);
            }

            IMUiStyleOptions style = new IMUiStyleOptions();
            style.Theme = theme;
            style.ThemeApplication = themeApplication;
            style.ApplyGameFont = applyGameFont;
            style.Width = width;
            style.Height = height;
            style.FontSize = fontSize;
            IMUiStyle.Apply(instance, style);
            ApplyGraphicColorOverrides(instance);

            if (configure != null) configure(instance);

            instance.SetActive(active);
            IMUiElementHandle created = new IMUiElementHandle();
            created.Root = instance;
            created.Source = source;
            created.SourceKind = sourceKind;
            handle = created;
            return true;
        }

        private void ApplyGraphicColorOverrides(GameObject instance)
        {
            if (instance == null || graphicColorOverrides.Count == 0) return;
            IMUiTheme resolvedTheme = theme ?? IMUiTheme.Vanilla();
            for (int i = 0; i < graphicColorOverrides.Count; i++)
            {
                GraphicColorOverride item = graphicColorOverrides[i];
                if (item == null) continue;
                Transform target = string.IsNullOrEmpty(item.PathOrName)
                    ? instance.transform
                    : VanillaUiPopupFactory.FindInHierarchy(instance.transform, item.PathOrName);
                if (target == null) continue;
                Graphic graphic = target.GetComponent<Graphic>();
                if (graphic == null) graphic = target.GetComponentInChildren<Graphic>(true);
                if (graphic == null) continue;
                graphic.color = item.UseRole ? resolvedTheme.GetColor(item.Role) : item.Color;
            }
        }
    }
}
