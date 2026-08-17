using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GraduationCalendar.EmbeddedIMUiFramework
{
    /// <summary>
    /// Options for cloning any serialized UI hierarchy from the currently loaded vanilla scene.
    /// Use a full hierarchy path whenever a name is not unique.
    /// </summary>
    public sealed class VanillaSceneTemplateOptions
    {
        public string HierarchyPathOrName;
        public int OccurrenceIndex;
        public Transform Parent;
        public string ObjectName;
        public VanillaUiCloneMode CloneMode = VanillaUiCloneMode.Template;
        public bool Active = true;
        public bool ApplyGameFont = true;
    }

    /// <summary>
    /// Small ergonomic wrapper over an arbitrary scene-native clone. It intentionally does not impose
    /// a layout model: every vanilla row, card, header, tab, list, popup child, HUD block, or composite
    /// control can be addressed through its exact hierarchy and then configured with a few common calls.
    /// </summary>
    public sealed class VanillaSceneTemplateHandle
    {
        public GameObject Root;
        public GameObject Source;
        public string SourceHierarchyPath = string.Empty;
        public VanillaUiCloneMode CloneMode;

        public bool IsValid
        {
            get { return Root != null; }
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
                return direct != null ? direct : IMUiCompat.GetComponentInChildren<T>(Root);
            }

            Transform target = Find(relativePathOrName);
            if (target == null)
            {
                return null;
            }

            T component = target.GetComponent<T>();
            return component != null ? component : IMUiCompat.GetComponentInChildren<T>(target);
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
                button = IMUiCompat.GetComponentInChildren<Button>(target);
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

        public void SetActive(bool active)
        {
            if (Root != null)
            {
                Root.SetActive(active);
            }
        }
    }

    /// <summary>
    /// High-level v3 scene-template factory. VanillaUiSceneCatalog is the exhaustive resolver/index;
    /// this class is the terse creation layer intended for normal mod code.
    /// </summary>
    public static class VanillaUiSceneFactory
    {
        public static bool TryCreate(
            string hierarchyPathOrName,
            Transform parent,
            string objectName,
            out VanillaSceneTemplateHandle handle)
        {
            return TryCreate(hierarchyPathOrName, 0, parent, objectName, out handle);
        }

        public static bool TryCreate(
            string hierarchyPathOrName,
            int occurrenceIndex,
            Transform parent,
            string objectName,
            out VanillaSceneTemplateHandle handle)
        {
            VanillaSceneTemplateOptions options = new VanillaSceneTemplateOptions();
            options.HierarchyPathOrName = hierarchyPathOrName;
            options.OccurrenceIndex = occurrenceIndex;
            options.Parent = parent;
            options.ObjectName = objectName;
            return TryCreate(options, out handle);
        }

        public static bool TryCreate(VanillaSceneTemplateOptions options, out VanillaSceneTemplateHandle handle)
        {
            handle = null;
            if (options == null || string.IsNullOrEmpty(options.HierarchyPathOrName))
            {
                return false;
            }

            GameObject source;
            if (!VanillaUiSceneCatalog.TryFindSceneObject(options.HierarchyPathOrName, options.OccurrenceIndex, out source) || source == null)
            {
                return false;
            }

            GameObject clone;
            if (!VanillaUiSceneCatalog.TryCloneSceneObject(
                options.HierarchyPathOrName,
                options.OccurrenceIndex,
                options.Parent,
                options.ObjectName,
                options.CloneMode,
                false,
                out clone) || clone == null)
            {
                return false;
            }

            if (options.ApplyGameFont)
            {
                VanillaUiFonts.ApplyGameFont(clone, true);
            }

            VanillaSceneTemplateHandle created = new VanillaSceneTemplateHandle();
            created.Root = clone;
            created.Source = source;
            created.SourceHierarchyPath = VanillaUiSceneCatalog.GetHierarchyPath(source.transform);
            created.CloneMode = options.CloneMode;

            clone.SetActive(options.Active);
            handle = created;
            return true;
        }
    }
}
