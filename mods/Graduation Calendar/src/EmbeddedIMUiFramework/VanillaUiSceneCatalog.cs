using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GraduationCalendar.EmbeddedIMUiFramework
{
    /// <summary>
    /// Controls how aggressively a scene-native template is detached from the original game's logic.
    /// Exact preserves every serialized component and listener. Template keeps the visual/UI machinery,
    /// removes popup-specific Assembly-CSharp behaviours, and prunes unsafe/external listeners while
    /// preserving safe internal UI wiring. VisualOnly
    /// additionally disables Selectables so the clone behaves as inert chrome until a mod configures it.
    /// </summary>
    public enum VanillaUiCloneMode
    {
        Exact = 0,
        Template = 1,
        VisualOnly = 2
    }

    public sealed class VanillaPopupDescriptor
    {
        public PopupManager._type Type;
        public PopupManager._popup Entry;
        public GameObject Root;
        public string SceneName = string.Empty;
        public string HierarchyPath = string.Empty;
        public string RootName = string.Empty;
        public bool HasSerializedEntry;
        public bool HasRoot;
        public bool BlurBackground;
        public bool DarkenBackground;
        public bool IsOpen;
    }

    public sealed class VanillaSceneUiDescriptor
    {
        public string SceneName = string.Empty;
        public string HierarchyPath = string.Empty;
        public int HierarchyOccurrenceIndex;
        public string Name = string.Empty;
        public bool ActiveSelf;
        public bool ActiveInHierarchy;
        public int Depth;
        public string[] ComponentTypes = new string[0];
    }

    /// <summary>
    /// Version 3 scene index. Unlike GameObject.Find, the index is built from all loaded scene roots,
    /// so inactive vanilla UI is available without opening it. This is the universal scene-native layer:
    /// callers can resolve or clone any RectTransform hierarchy in the currently-loaded Idol Manager scene,
    /// not only the handful of controls that happen to have dedicated framework helpers.
    /// </summary>
    public static class VanillaUiSceneCatalog
    {
        private static readonly Dictionary<string, Transform> pathIndex = new Dictionary<string, Transform>(StringComparer.Ordinal);
        private static readonly Dictionary<string, List<Transform>> pathMatches = new Dictionary<string, List<Transform>>(StringComparer.Ordinal);
        private static readonly Dictionary<string, List<Transform>> nameIndex = new Dictionary<string, List<Transform>>(StringComparer.Ordinal);
        private static readonly List<Transform> allIndexedTransforms = new List<Transform>();
        private static int indexedSceneHandle = int.MinValue;
        private static int indexedRootCount = -1;

        private static readonly HashSet<string> SafeGameUiBehaviourNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Popup",
            "ButtonDefault",
            "ScrollRectDefault",
            "SliderDefault",
            "Font_Replacer"
        };

        public static void InvalidateSceneIndex()
        {
            indexedSceneHandle = int.MinValue;
            indexedRootCount = -1;
            pathIndex.Clear();
            pathMatches.Clear();
            nameIndex.Clear();
            allIndexedTransforms.Clear();
        }

        public static IList<VanillaPopupDescriptor> DescribeCurrentPopups()
        {
            List<VanillaPopupDescriptor> result = new List<VanillaPopupDescriptor>();
            PopupManager manager;
            IMUiKit.TryGetPopupManager(out manager);

            Array values = Enum.GetValues(typeof(PopupManager._type));
            for (int i = 0; i < values.Length; i++)
            {
                PopupManager._type type = (PopupManager._type)values.GetValue(i);
                VanillaPopupDescriptor descriptor;
                if (!TryDescribePopup(manager, type, out descriptor))
                {
                    descriptor = new VanillaPopupDescriptor();
                    descriptor.Type = type;
                    descriptor.SceneName = IMUiCompat.GetCurrentSceneName();
                }
                result.Add(descriptor);
            }

            return result;
        }

        public static bool TryDescribePopup(PopupManager._type type, out VanillaPopupDescriptor descriptor)
        {
            PopupManager manager;
            IMUiKit.TryGetPopupManager(out manager);
            return TryDescribePopup(manager, type, out descriptor);
        }

        private static bool TryDescribePopup(PopupManager manager, PopupManager._type type, out VanillaPopupDescriptor descriptor)
        {
            descriptor = new VanillaPopupDescriptor();
            descriptor.Type = type;
            descriptor.SceneName = IMUiCompat.GetCurrentSceneName();
            // PopupManager._popup defaults these to true. Use the same defaults when the current
            // scene contains a known template hierarchy but its PopupManager entry is absent.
            descriptor.BlurBackground = true;
            descriptor.DarkenBackground = true;

            PopupManager._popup entry = null;
            if (manager != null && manager.popups != null)
            {
                entry = manager.GetByType(type);
            }

            descriptor.Entry = entry;
            descriptor.HasSerializedEntry = entry != null;
            if (entry != null)
            {
                descriptor.BlurBackground = entry.BGBlur;
                descriptor.DarkenBackground = entry.BGDarken;
                descriptor.IsOpen = entry.open;
                descriptor.Root = entry.obj;
            }

            // IM_Scenes proves a few popup hierarchies exist even when a manager reference is not
            // useful to a caller. Fall back to the complete v3 path catalog, still without opening
            // or activating anything. This also makes scene-template discovery resilient to a null
            // PopupManager during early/main-menu initialization.
            if (descriptor.Root == null)
            {
                GameObject catalogRoot;
                if (TryResolveCatalogPopupRoot(type, out catalogRoot))
                {
                    descriptor.Root = catalogRoot;
                }
            }

            descriptor.HasRoot = descriptor.Root != null;
            if (descriptor.Root != null)
            {
                descriptor.RootName = descriptor.Root.name ?? string.Empty;
                descriptor.HierarchyPath = GetHierarchyPath(descriptor.Root.transform);
            }

            return descriptor.HasSerializedEntry || descriptor.HasRoot;
        }

        private static bool TryResolveCatalogPopupRoot(PopupManager._type type, out GameObject root)
        {
            root = null;
            VanillaPopupTemplateDefinition definition;
            if (!VanillaPopupTemplateCatalog.TryGet(type, out definition) || definition == null)
            {
                return false;
            }

            // Probe both known scene paths. Only a path actually present in the currently loaded
            // scene can resolve, so this does not require scene-name guessing and works for either
            // gameplay or main-menu instances of templates shared by both scenes.
            if (!string.IsNullOrEmpty(definition.GameplayHierarchyPath) &&
                TryFindSceneObject(definition.GameplayHierarchyPath, out root) && root != null)
            {
                return true;
            }
            if (!string.IsNullOrEmpty(definition.MainMenuHierarchyPath) &&
                TryFindSceneObject(definition.MainMenuHierarchyPath, out root) && root != null)
            {
                return true;
            }

            root = null;
            return false;
        }

        /// <summary>
        /// Resolves a serialized PopupManager root while it is inactive/closed.
        /// </summary>
        public static bool TryGetPopupRoot(PopupManager._type type, out GameObject root)
        {
            root = null;
            VanillaPopupDescriptor descriptor;
            if (!TryDescribePopup(type, out descriptor) || descriptor == null || !descriptor.HasRoot)
            {
                return false;
            }
            root = descriptor.Root;
            return root != null;
        }

        public static bool TryFindPopupChild(PopupManager._type type, string relativePath, out Transform child)
        {
            child = null;
            GameObject root;
            if (!TryGetPopupRoot(type, out root) || root == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(relativePath) || relativePath == ".")
            {
                child = root.transform;
                return true;
            }

            child = root.transform.Find(NormalizePath(relativePath));
            return child != null;
        }

        public static bool TryClonePopup(
            PopupManager._type type,
            Transform parent,
            string objectName,
            VanillaUiCloneMode cloneMode,
            bool active,
            out GameObject instance)
        {
            instance = null;
            GameObject source;
            if (!TryGetPopupRoot(type, out source) || source == null)
            {
                return false;
            }

            return TryCloneObject(source, parent, objectName, cloneMode, active, out instance);
        }

        public static bool TryClonePopupChild(
            PopupManager._type type,
            string relativePath,
            Transform parent,
            string objectName,
            VanillaUiCloneMode cloneMode,
            bool active,
            out GameObject instance)
        {
            instance = null;
            Transform source;
            if (!TryFindPopupChild(type, relativePath, out source) || source == null)
            {
                return false;
            }

            return TryCloneObject(source.gameObject, parent, objectName, cloneMode, active, out instance);
        }

        /// <summary>
        /// Resolves any GameObject in the active scene by full hierarchy path. Paths are rooted at a
        /// scene root, for example "AgencyPopups/Producer_Contracts/Panel/Slider". If only a name is
        /// supplied, the first matching scene object is returned. Inactive objects are indexed too.
        /// </summary>
        public static bool TryFindSceneObject(string hierarchyPathOrName, out GameObject gameObject)
        {
            return TryFindSceneObject(hierarchyPathOrName, 0, out gameObject);
        }

        /// <summary>
        /// Resolves a scene object by hierarchy path or name and occurrence. Occurrence is normally 0.
        /// It exists because vanilla main.unity contains a small number of same-name sibling paths;
        /// v3 indexes every one rather than silently making the later siblings unreachable.
        /// </summary>
        public static bool TryFindSceneObject(string hierarchyPathOrName, int occurrenceIndex, out GameObject gameObject)
        {
            gameObject = null;
            if (string.IsNullOrEmpty(hierarchyPathOrName) || occurrenceIndex < 0)
            {
                return false;
            }

            EnsureSceneIndex();
            string normalized = NormalizePath(hierarchyPathOrName);
            List<Transform> matches;
            if (normalized.IndexOf('/') >= 0)
            {
                if (!pathMatches.TryGetValue(normalized, out matches) || matches == null || occurrenceIndex >= matches.Count)
                {
                    return false;
                }
            }
            else
            {
                if (!nameIndex.TryGetValue(normalized, out matches) || matches == null || occurrenceIndex >= matches.Count)
                {
                    return false;
                }
            }

            Transform transform = matches[occurrenceIndex];
            if (transform == null)
            {
                return false;
            }
            gameObject = transform.gameObject;
            return gameObject != null;
        }

        public static IList<GameObject> FindSceneObjectsByPath(string hierarchyPath)
        {
            List<GameObject> result = new List<GameObject>();
            if (string.IsNullOrEmpty(hierarchyPath))
            {
                return result;
            }

            EnsureSceneIndex();
            List<Transform> matches;
            if (!pathMatches.TryGetValue(NormalizePath(hierarchyPath), out matches) || matches == null)
            {
                return result;
            }

            for (int i = 0; i < matches.Count; i++)
            {
                if (matches[i] != null)
                {
                    result.Add(matches[i].gameObject);
                }
            }
            return result;
        }

        public static IList<GameObject> FindSceneObjectsByName(string objectName)
        {
            List<GameObject> result = new List<GameObject>();
            if (string.IsNullOrEmpty(objectName))
            {
                return result;
            }

            EnsureSceneIndex();
            List<Transform> matches;
            if (!nameIndex.TryGetValue(objectName, out matches) || matches == null)
            {
                return result;
            }

            for (int i = 0; i < matches.Count; i++)
            {
                Transform transform = matches[i];
                if (transform != null)
                {
                    result.Add(transform.gameObject);
                }
            }
            return result;
        }

        /// <summary>
        /// Clones a GameObject reference that was obtained from Idol Manager itself, including serialized
        /// prefab references that are not scene hierarchy objects and are not Resources assets. This is the
        /// common cloning/sanitization path used by the v3 serialized-reference template layer.
        /// </summary>
        public static bool TryCloneSourceObject(
            GameObject source,
            Transform parent,
            string objectName,
            VanillaUiCloneMode cloneMode,
            bool active,
            out GameObject instance)
        {
            return TryCloneObject(source, parent, objectName, cloneMode, active, out instance);
        }

        public static bool TryCloneSceneObject(
            string hierarchyPathOrName,
            Transform parent,
            string objectName,
            VanillaUiCloneMode cloneMode,
            bool active,
            out GameObject instance)
        {
            return TryCloneSceneObject(
                hierarchyPathOrName,
                0,
                parent,
                objectName,
                cloneMode,
                active,
                out instance);
        }

        public static bool TryCloneSceneObject(
            string hierarchyPathOrName,
            int occurrenceIndex,
            Transform parent,
            string objectName,
            VanillaUiCloneMode cloneMode,
            bool active,
            out GameObject instance)
        {
            instance = null;
            GameObject source;
            if (!TryFindSceneObject(hierarchyPathOrName, occurrenceIndex, out source) || source == null)
            {
                return false;
            }
            return TryCloneObject(source, parent, objectName, cloneMode, active, out instance);
        }

        /// <summary>
        /// Finds an inactive or active scene component without touching Resources assets. This is useful
        /// for game-specific widgets that have no Resources prefab. The optional predicate can select a
        /// particular serialized variant.
        /// </summary>
        public static bool TryFindSceneComponent<T>(Predicate<T> predicate, out T component) where T : Component
        {
            component = null;
            EnsureSceneIndex();
            for (int transformIndex = 0; transformIndex < allIndexedTransforms.Count; transformIndex++)
            {
                Transform transform = allIndexedTransforms[transformIndex];
                if (transform == null)
                {
                    continue;
                }

                T candidate = transform.GetComponent<T>();
                if (candidate == null)
                {
                    continue;
                }
                if (predicate == null || predicate(candidate))
                {
                    component = candidate;
                    return true;
                }
            }
            return false;
        }

        public static bool TryCloneSceneComponentTemplate<T>(
            Transform parent,
            string objectName,
            Predicate<T> predicate,
            VanillaUiCloneMode cloneMode,
            bool active,
            out GameObject instance,
            out T component) where T : Component
        {
            instance = null;
            component = null;
            T source;
            if (!TryFindSceneComponent(predicate, out source) || source == null)
            {
                return false;
            }

            if (!TryCloneObject(source.gameObject, parent, objectName, cloneMode, active, out instance) || instance == null)
            {
                return false;
            }

            component = instance.GetComponent<T>();
            if (component == null)
            {
                component = IMUiCompat.GetComponentInChildren<T>(instance);
            }
            if (component == null)
            {
                UnityEngine.Object.Destroy(instance);
                instance = null;
                return false;
            }
            return true;
        }

        public static IList<VanillaSceneUiDescriptor> DescribeCurrentSceneUi(string rootPath = null)
        {
            List<VanillaSceneUiDescriptor> result = new List<VanillaSceneUiDescriptor>();
            EnsureSceneIndex();
            string normalizedRoot = string.IsNullOrEmpty(rootPath) ? string.Empty : NormalizePath(rootPath);

            for (int transformIndex = 0; transformIndex < allIndexedTransforms.Count; transformIndex++)
            {
                Transform transform = allIndexedTransforms[transformIndex];
                string path = transform != null ? GetHierarchyPath(transform) : string.Empty;
                if (transform == null || transform.GetComponent<RectTransform>() == null)
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(normalizedRoot) &&
                    !string.Equals(path, normalizedRoot, StringComparison.Ordinal) &&
                    !path.StartsWith(normalizedRoot + "/", StringComparison.Ordinal))
                {
                    continue;
                }

                Component[] components = transform.GetComponents<Component>();
                List<string> componentNames = new List<string>();
                for (int i = 0; i < components.Length; i++)
                {
                    Component item = components[i];
                    if (item != null)
                    {
                        componentNames.Add(item.GetType().FullName);
                    }
                }

                VanillaSceneUiDescriptor descriptor = new VanillaSceneUiDescriptor();
                int occurrenceIndex = 0;
                List<Transform> samePath;
                if (pathMatches.TryGetValue(path, out samePath) && samePath != null)
                {
                    int foundIndex = samePath.IndexOf(transform);
                    if (foundIndex >= 0)
                    {
                        occurrenceIndex = foundIndex;
                    }
                }

                descriptor.SceneName = IMUiCompat.GetCurrentSceneName();
                descriptor.HierarchyPath = path;
                descriptor.HierarchyOccurrenceIndex = occurrenceIndex;
                descriptor.Name = transform.name ?? string.Empty;
                descriptor.ActiveSelf = transform.gameObject.activeSelf;
                descriptor.ActiveInHierarchy = transform.gameObject.activeInHierarchy;
                descriptor.Depth = CountPathDepth(path);
                descriptor.ComponentTypes = componentNames.ToArray();
                result.Add(descriptor);
            }

            result.Sort(delegate(VanillaSceneUiDescriptor a, VanillaSceneUiDescriptor b)
            {
                return string.Compare(a.HierarchyPath, b.HierarchyPath, StringComparison.Ordinal);
            });
            return result;
        }

        public static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            List<string> segments = new List<string>();
            Transform current = transform;
            while (current != null)
            {
                segments.Add(current.name);
                current = current.parent;
            }
            segments.Reverse();
            return string.Join("/", segments.ToArray());
        }

        /// <summary>
        /// Detaches a clone from the original popup's game-specific data/controller logic while retaining
        /// Unity UI, TMPro, Modern UI Pack, third-party visual components, and a small whitelist of Idol
        /// Manager's generic UI behaviours. Exact clones are intentionally untouched.
        /// </summary>
        public static int SanitizeClone(GameObject root, VanillaUiCloneMode cloneMode)
        {
            if (root == null || cloneMode == VanillaUiCloneMode.Exact)
            {
                return 0;
            }

            int changed = 0;
            bool wasActive = root.activeSelf;
            root.SetActive(false);

            // Template clones retain serialized event links that stay entirely inside the clone and
            // point only at generic UI machinery. This is crucial for vanilla composite controls such
            // as the Contracts/Salaries/Loans ScrollRect <-> SliderDefault bridge. VisualOnly clones,
            // by contrast, intentionally discard every interaction.
            if (cloneMode == VanillaUiCloneMode.VisualOnly)
            {
                ClearInteractiveEvents(root);
            }
            else
            {
                PruneInteractiveEvents(root);
            }

            Assembly gameAssembly = typeof(PopupManager).Assembly;
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                Type type = behaviour.GetType();
                bool thirdPartyUi = IsThirdPartyUiNamespace(type.Namespace);
                bool remove = false;
                if (type.Assembly == gameAssembly && !thirdPartyUi && !SafeGameUiBehaviourNames.Contains(type.Name))
                {
                    remove = true;
                }

                if (cloneMode == VanillaUiCloneMode.VisualOnly)
                {
                    Selectable selectable = behaviour as Selectable;
                    if (selectable != null)
                    {
                        selectable.interactable = false;
                    }
                }

                if (remove)
                {
                    behaviour.enabled = false;
                    try
                    {
                        // The clone is inactive and disposable at this point. Immediate removal keeps
                        // a stripped controller from receiving Awake when the sanitized hierarchy is
                        // activated later in the same frame.
                        UnityEngine.Object.DestroyImmediate(behaviour);
                    }
                    catch
                    {
                        UnityEngine.Object.Destroy(behaviour);
                    }
                    changed++;
                }
                else if (cloneMode == VanillaUiCloneMode.Template &&
                         (type.Assembly == gameAssembly || thirdPartyUi))
                {
                    PruneUnityEventFields(behaviour, root);
                }
            }

            Popup popup = root.GetComponent<Popup>();
            if (popup != null)
            {
                popup.OnOpen = new UnityEvent();
            }

            if (wasActive)
            {
                root.SetActive(true);
            }

            return changed;
        }

        /// <summary>
        /// Removes serialized event wiring that reaches outside a clone or targets a popup-specific
        /// game controller, while preserving internal generic UI wiring. This is what lets Template
        /// mode keep vanilla composite behaviour without carrying the source popup's business logic.
        /// </summary>
        public static void PruneInteractiveEvents(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (!IsPersistentEventSafe(buttons[i].onClick, root))
                {
                    buttons[i].onClick = new Button.ButtonClickedEvent();
                }
            }

            Toggle[] toggles = root.GetComponentsInChildren<Toggle>(true);
            for (int i = 0; i < toggles.Length; i++)
            {
                if (!IsPersistentEventSafe(toggles[i].onValueChanged, root))
                {
                    toggles[i].onValueChanged = new Toggle.ToggleEvent();
                }
            }

            Slider[] sliders = root.GetComponentsInChildren<Slider>(true);
            for (int i = 0; i < sliders.Length; i++)
            {
                if (!IsPersistentEventSafe(sliders[i].onValueChanged, root))
                {
                    sliders[i].onValueChanged = new Slider.SliderEvent();
                }
            }

            Scrollbar[] scrollbars = root.GetComponentsInChildren<Scrollbar>(true);
            for (int i = 0; i < scrollbars.Length; i++)
            {
                if (!IsPersistentEventSafe(scrollbars[i].onValueChanged, root))
                {
                    scrollbars[i].onValueChanged = new Scrollbar.ScrollEvent();
                }
            }

            Dropdown[] dropdowns = root.GetComponentsInChildren<Dropdown>(true);
            for (int i = 0; i < dropdowns.Length; i++)
            {
                if (!IsPersistentEventSafe(dropdowns[i].onValueChanged, root))
                {
                    dropdowns[i].onValueChanged = new Dropdown.DropdownEvent();
                }
            }

            ScrollRect[] scrollRects = root.GetComponentsInChildren<ScrollRect>(true);
            for (int i = 0; i < scrollRects.Length; i++)
            {
                if (!IsPersistentEventSafe(scrollRects[i].onValueChanged, root))
                {
                    scrollRects[i].onValueChanged = new ScrollRect.ScrollRectEvent();
                }
            }

            InputField[] inputFields = root.GetComponentsInChildren<InputField>(true);
            for (int i = 0; i < inputFields.Length; i++)
            {
                if (!IsPersistentEventSafe(inputFields[i].onValueChanged, root))
                {
                    inputFields[i].onValueChanged = new InputField.OnChangeEvent();
                }
                if (!IsPersistentEventSafe(inputFields[i].onEndEdit, root))
                {
                    inputFields[i].onEndEdit = new InputField.SubmitEvent();
                }
            }

            TMP_InputField[] tmpInputFields = root.GetComponentsInChildren<TMP_InputField>(true);
            for (int i = 0; i < tmpInputFields.Length; i++)
            {
                if (!IsPersistentEventSafe(tmpInputFields[i].onValueChanged, root))
                {
                    tmpInputFields[i].onValueChanged = new TMP_InputField.OnChangeEvent();
                }
                if (!IsPersistentEventSafe(tmpInputFields[i].onEndEdit, root))
                {
                    tmpInputFields[i].onEndEdit = new TMP_InputField.SubmitEvent();
                }
            }

            EventTrigger[] triggers = root.GetComponentsInChildren<EventTrigger>(true);
            for (int i = 0; i < triggers.Length; i++)
            {
                if (triggers[i].triggers == null)
                {
                    continue;
                }
                for (int entryIndex = triggers[i].triggers.Count - 1; entryIndex >= 0; entryIndex--)
                {
                    EventTrigger.Entry entry = triggers[i].triggers[entryIndex];
                    if (entry == null || entry.callback == null || !IsPersistentEventSafe(entry.callback, root))
                    {
                        triggers[i].triggers.RemoveAt(entryIndex);
                    }
                }
            }
        }

        public static void ClearInteractiveEvents(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].onClick = new Button.ButtonClickedEvent();
            }

            Toggle[] toggles = root.GetComponentsInChildren<Toggle>(true);
            for (int i = 0; i < toggles.Length; i++)
            {
                toggles[i].onValueChanged = new Toggle.ToggleEvent();
            }

            Slider[] sliders = root.GetComponentsInChildren<Slider>(true);
            for (int i = 0; i < sliders.Length; i++)
            {
                sliders[i].onValueChanged = new Slider.SliderEvent();
            }

            Scrollbar[] scrollbars = root.GetComponentsInChildren<Scrollbar>(true);
            for (int i = 0; i < scrollbars.Length; i++)
            {
                scrollbars[i].onValueChanged = new Scrollbar.ScrollEvent();
            }

            Dropdown[] dropdowns = root.GetComponentsInChildren<Dropdown>(true);
            for (int i = 0; i < dropdowns.Length; i++)
            {
                dropdowns[i].onValueChanged = new Dropdown.DropdownEvent();
            }

            ScrollRect[] scrollRects = root.GetComponentsInChildren<ScrollRect>(true);
            for (int i = 0; i < scrollRects.Length; i++)
            {
                scrollRects[i].onValueChanged = new ScrollRect.ScrollRectEvent();
            }

            InputField[] inputFields = root.GetComponentsInChildren<InputField>(true);
            for (int i = 0; i < inputFields.Length; i++)
            {
                inputFields[i].onValueChanged = new InputField.OnChangeEvent();
                inputFields[i].onEndEdit = new InputField.SubmitEvent();
            }

            TMP_InputField[] tmpInputFields = root.GetComponentsInChildren<TMP_InputField>(true);
            for (int i = 0; i < tmpInputFields.Length; i++)
            {
                tmpInputFields[i].onValueChanged = new TMP_InputField.OnChangeEvent();
                tmpInputFields[i].onEndEdit = new TMP_InputField.SubmitEvent();
            }

            EventTrigger[] triggers = root.GetComponentsInChildren<EventTrigger>(true);
            for (int i = 0; i < triggers.Length; i++)
            {
                if (triggers[i].triggers != null)
                {
                    triggers[i].triggers.Clear();
                }
            }
        }

        private static bool TryCloneObject(
            GameObject source,
            Transform parent,
            string objectName,
            VanillaUiCloneMode cloneMode,
            bool active,
            out GameObject instance)
        {
            instance = null;
            if (source == null)
            {
                return false;
            }

            Transform resolvedParent = parent;
            if (resolvedParent == null)
            {
                resolvedParent = IMUiKit.GetPopupParent();
            }

            if (cloneMode == VanillaUiCloneMode.Exact)
            {
                instance = resolvedParent == null
                    ? UnityEngine.Object.Instantiate(source)
                    : UnityEngine.Object.Instantiate(source, resolvedParent, false);
            }
            else
            {
                // A Template/VisualOnly source can itself be active (for example a menu widget).
                // Clone under an inactive staging parent so popup-specific OnEnable/Awake-style UI
                // work cannot run before SanitizeClone has removed its controllers and unsafe events.
                GameObject staging = new GameObject("__IMUI_V3_CLONE_STAGING__");
                staging.SetActive(false);
                instance = UnityEngine.Object.Instantiate(source, staging.transform, false);
                if (instance != null)
                {
                    instance.SetActive(false);
                    instance.transform.SetParent(resolvedParent, false);
                }
                UnityEngine.Object.Destroy(staging);
            }
            if (instance == null)
            {
                return false;
            }

            instance.SetActive(false);
            if (!string.IsNullOrEmpty(objectName))
            {
                instance.name = objectName;
            }
            if (resolvedParent != null)
            {
                IMUiKit.ApplyLayerRecursively(instance, resolvedParent.gameObject.layer);
            }

            SanitizeClone(instance, cloneMode);
            instance.SetActive(active);
            return true;
        }

        private static void EnsureSceneIndex()
        {
            int sceneHandle = IMUiCompat.GetCurrentSceneHandle();
            if (sceneHandle == indexedSceneHandle && pathIndex.Count > 0)
            {
                return;
            }

            GameObject[] roots = IMUiCompat.GetCurrentSceneRoots();
            indexedSceneHandle = sceneHandle;
            indexedRootCount = roots.Length;
            pathIndex.Clear();
            pathMatches.Clear();
            nameIndex.Clear();
            allIndexedTransforms.Clear();

            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root != null)
                {
                    IndexTransform(root.transform, root.name);
                }
            }
        }

        private static void IndexTransform(Transform transform, string path)
        {
            if (transform == null)
            {
                return;
            }

            string normalized = NormalizePath(path);
            if (!pathIndex.ContainsKey(normalized))
            {
                pathIndex.Add(normalized, transform);
            }

            List<Transform> byPath;
            if (!pathMatches.TryGetValue(normalized, out byPath))
            {
                byPath = new List<Transform>();
                pathMatches.Add(normalized, byPath);
            }
            byPath.Add(transform);
            allIndexedTransforms.Add(transform);

            List<Transform> byName;
            if (!nameIndex.TryGetValue(transform.name, out byName))
            {
                byName = new List<Transform>();
                nameIndex.Add(transform.name, byName);
            }
            byName.Add(transform);

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child != null)
                {
                    IndexTransform(child, normalized + "/" + child.name);
                }
            }
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }
            return path.Replace('\\', '/').Trim().Trim('/');
        }

        private static int CountPathDepth(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return 0;
            }
            int depth = 1;
            for (int i = 0; i < path.Length; i++)
            {
                if (path[i] == '/')
                {
                    depth++;
                }
            }
            return depth;
        }

        private static bool IsThirdPartyUiNamespace(string namespaceName)
        {
            if (string.IsNullOrEmpty(namespaceName))
            {
                return false;
            }

            return namespaceName.StartsWith("Michsky.", StringComparison.Ordinal) ||
                   namespaceName.StartsWith("UnityEngine.UI.Extensions", StringComparison.Ordinal) ||
                   namespaceName.StartsWith("UnityEngine.UI.Michsky", StringComparison.Ordinal) ||
                   namespaceName.StartsWith("Coffee.", StringComparison.Ordinal) ||
                   namespaceName.StartsWith("Nobi.", StringComparison.Ordinal);
        }

        private static bool IsPersistentEventSafe(UnityEventBase unityEvent, GameObject root)
        {
            if (unityEvent == null || root == null)
            {
                return false;
            }

            List<UnityEngine.Object> persistentTargets;
            if (!IMUiCompat.TryGetPersistentTargets(unityEvent, out persistentTargets))
            {
                return false;
            }

            // No serialized listener means there is no vanilla wiring to preserve. Replacing such
            // events also guarantees a source object's runtime-only listeners are not carried over.
            if (persistentTargets.Count == 0)
            {
                return false;
            }

            Assembly gameAssembly = typeof(PopupManager).Assembly;
            for (int i = 0; i < persistentTargets.Count; i++)
            {
                UnityEngine.Object target = persistentTargets[i];

                if (target == null)
                {
                    return false;
                }

                Component targetComponent = target as Component;
                GameObject targetGameObject = target as GameObject;
                Transform targetTransform = null;
                Type targetType = target.GetType();

                if (targetComponent != null)
                {
                    targetTransform = targetComponent.transform;
                    targetType = targetComponent.GetType();
                }
                else if (targetGameObject != null)
                {
                    targetTransform = targetGameObject.transform;
                }

                if (targetTransform == null ||
                    (targetTransform != root.transform && !targetTransform.IsChildOf(root.transform)))
                {
                    return false;
                }

                // Internal listeners are still unsafe if they call a popup-specific Idol Manager
                // controller that Template mode is about to remove.
                string namespaceName = targetType.Namespace;
                bool thirdPartyUi = IsThirdPartyUiNamespace(namespaceName);
                if (targetType.Assembly == gameAssembly &&
                    !thirdPartyUi &&
                    !SafeGameUiBehaviourNames.Contains(targetType.Name) &&
                    targetComponent != null)
                {
                    return false;
                }
            }

            return true;
        }

        private static void PruneUnityEventFields(MonoBehaviour behaviour, GameObject root)
        {
            if (behaviour == null || root == null)
            {
                return;
            }

            Type current = behaviour.GetType();
            while (current != null && current != typeof(MonoBehaviour))
            {
                FieldInfo[] fields;
                try
                {
                    fields = current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                }
                catch
                {
                    break;
                }

                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    if (field == null || field.IsInitOnly || !typeof(UnityEventBase).IsAssignableFrom(field.FieldType))
                    {
                        continue;
                    }

                    try
                    {
                        UnityEventBase existing = field.GetValue(behaviour) as UnityEventBase;
                        if (existing != null && IsPersistentEventSafe(existing, root))
                        {
                            continue;
                        }

                        object replacement = Activator.CreateInstance(field.FieldType);
                        field.SetValue(behaviour, replacement);
                    }
                    catch
                    {
                    }
                }
                current = current.BaseType;
            }
        }
    }
}
