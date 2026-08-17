using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IMUiFramework
{
    /// <summary>
    /// Describes a vanilla UI GameObject reached through a serialized field on a live scene component.
    /// This is the runtime-safe way to expose Idol Manager's many non-Resources prefab references
    /// (prefab_line, prefab_button, prefab_stat, and similar) without pretending AssetRipper export
    /// folders are Resources.Load paths.
    /// </summary>
    public sealed class VanillaUiReferenceTemplateDescriptor
    {
        public string SceneName = string.Empty;
        public string OwnerHierarchyPath = string.Empty;
        public int OwnerHierarchyOccurrenceIndex;
        public string OwnerComponentType = string.Empty;
        public string OwnerComponentFullType = string.Empty;
        public string FieldName = string.Empty;
        public int ElementIndex = -1;
        public string SourceName = string.Empty;
        public GameObject Source;
        public bool SourceIsSceneObject;
        public string SourceHierarchyPath = string.Empty;
        public bool IsUi;
    }

    /// <summary>
    /// Resolves UI prefabs and composite templates held in serialized GameObject/Component fields on
    /// Idol Manager scene behaviours. A large portion of the game's repeated UI (rows, stat cells,
    /// girl buttons, notification items, SNS items, award pieces, etc.) lives this way rather than in
    /// Resources. Version 3 exposes those references directly and clones them through the same safe
    /// Exact/Template/VisualOnly pipeline used by scene-native templates.
    /// </summary>
    public static class VanillaUiReferenceTemplates
    {
        private const BindingFlags DeclaredInstanceFields =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        /// <summary>
        /// Finds a serialized UI GameObject reference on a component attached to the named scene object.
        /// componentTypeName can be a short type name (Contracts_Popup) or full name. Set it to null/empty
        /// to search every MonoBehaviour on the owner. elementIndex is -1 for scalar references, or an
        /// array/list index for serialized collections.
        /// </summary>
        public static bool TryGetTemplate(
            string ownerHierarchyPathOrName,
            string componentTypeName,
            string fieldName,
            int elementIndex,
            out GameObject source)
        {
            return TryGetTemplate(
                ownerHierarchyPathOrName,
                0,
                componentTypeName,
                fieldName,
                elementIndex,
                out source);
        }

        public static bool TryGetTemplate(
            string ownerHierarchyPathOrName,
            int ownerOccurrenceIndex,
            string componentTypeName,
            string fieldName,
            int elementIndex,
            out GameObject source)
        {
            source = null;
            if (string.IsNullOrEmpty(ownerHierarchyPathOrName) || string.IsNullOrEmpty(fieldName))
            {
                return false;
            }

            GameObject owner;
            if (!VanillaUiSceneCatalog.TryFindSceneObject(ownerHierarchyPathOrName, ownerOccurrenceIndex, out owner) || owner == null)
            {
                return false;
            }

            return TryGetTemplate(owner, componentTypeName, fieldName, elementIndex, out source);
        }

        /// <summary>
        /// Finds a serialized UI template somewhere inside a closed or open vanilla popup. This is useful
        /// for fields such as Loans_Popup.prefab_line and other row/item prefabs. The popup is never opened.
        /// </summary>
        public static bool TryGetPopupTemplate(
            PopupManager._type popupType,
            string componentTypeName,
            string fieldName,
            int elementIndex,
            out GameObject source)
        {
            source = null;
            if (string.IsNullOrEmpty(fieldName))
            {
                return false;
            }

            GameObject popupRoot;
            if (!VanillaUiSceneCatalog.TryGetPopupRoot(popupType, out popupRoot) || popupRoot == null)
            {
                return false;
            }

            MonoBehaviour[] behaviours = popupRoot.GetComponentsInChildren<MonoBehaviour>(true);
            return TryGetTemplate(behaviours, componentTypeName, fieldName, elementIndex, out source);
        }

        public static bool TryCloneTemplate(
            string ownerHierarchyPathOrName,
            string componentTypeName,
            string fieldName,
            int elementIndex,
            Transform parent,
            string objectName,
            VanillaUiCloneMode cloneMode,
            bool active,
            out GameObject instance)
        {
            instance = null;
            GameObject source;
            if (!TryGetTemplate(ownerHierarchyPathOrName, componentTypeName, fieldName, elementIndex, out source) || source == null)
            {
                return false;
            }

            return VanillaUiSceneCatalog.TryCloneSourceObject(source, parent, objectName, cloneMode, active, out instance);
        }

        public static bool TryCloneTemplate(
            string ownerHierarchyPathOrName,
            int ownerOccurrenceIndex,
            string componentTypeName,
            string fieldName,
            int elementIndex,
            Transform parent,
            string objectName,
            VanillaUiCloneMode cloneMode,
            bool active,
            out GameObject instance)
        {
            instance = null;
            GameObject source;
            if (!TryGetTemplate(
                    ownerHierarchyPathOrName,
                    ownerOccurrenceIndex,
                    componentTypeName,
                    fieldName,
                    elementIndex,
                    out source) || source == null)
            {
                return false;
            }

            return VanillaUiSceneCatalog.TryCloneSourceObject(source, parent, objectName, cloneMode, active, out instance);
        }

        public static bool TryClonePopupTemplate(
            PopupManager._type popupType,
            string componentTypeName,
            string fieldName,
            int elementIndex,
            Transform parent,
            string objectName,
            VanillaUiCloneMode cloneMode,
            bool active,
            out GameObject instance)
        {
            instance = null;
            GameObject source;
            if (!TryGetPopupTemplate(popupType, componentTypeName, fieldName, elementIndex, out source) || source == null)
            {
                return false;
            }

            return VanillaUiSceneCatalog.TryCloneSourceObject(source, parent, objectName, cloneMode, active, out instance);
        }

        /// <summary>
        /// Returns every serialized UI GameObject/Component reference found beneath a scene hierarchy.
        /// This is a discovery/diagnostic API, so it is intentionally evaluated on demand rather than
        /// scanning the whole 9k+ RectTransform gameplay scene at framework startup.
        /// </summary>
        public static IList<VanillaUiReferenceTemplateDescriptor> DescribeCurrentSerializedUiTemplates(
            string rootHierarchyPathOrName = null,
            bool includeSceneObjects = true)
        {
            List<VanillaUiReferenceTemplateDescriptor> result = new List<VanillaUiReferenceTemplateDescriptor>();

            GameObject root = null;
            if (!string.IsNullOrEmpty(rootHierarchyPathOrName))
            {
                VanillaUiSceneCatalog.TryFindSceneObject(rootHierarchyPathOrName, out root);
                if (root == null)
                {
                    return result;
                }
            }

            MonoBehaviour[] behaviours;
            if (root != null)
            {
                behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            }
            else
            {
                List<MonoBehaviour> all = new List<MonoBehaviour>();
                Scene scene = SceneManager.GetActiveScene();
                GameObject[] roots = scene.IsValid() ? scene.GetRootGameObjects() : new GameObject[0];
                for (int i = 0; i < roots.Length; i++)
                {
                    GameObject sceneRoot = roots[i];
                    if (sceneRoot == null)
                    {
                        continue;
                    }
                    all.AddRange(sceneRoot.GetComponentsInChildren<MonoBehaviour>(true));
                }
                behaviours = all.ToArray();
            }

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }
                AddBehaviourDescriptors(behaviour, includeSceneObjects, result);
            }

            return result;
        }

        /// <summary>
        /// Returns serialized UI templates specifically referenced from a vanilla popup hierarchy without
        /// requiring that popup to be open. Handy for exploring the popup's row/item/button prefab vocabulary.
        /// </summary>
        public static IList<VanillaUiReferenceTemplateDescriptor> DescribePopupSerializedUiTemplates(
            PopupManager._type popupType,
            bool includeSceneObjects = true)
        {
            GameObject root;
            if (!VanillaUiSceneCatalog.TryGetPopupRoot(popupType, out root) || root == null)
            {
                return new List<VanillaUiReferenceTemplateDescriptor>();
            }
            return DescribeCurrentSerializedUiTemplates(VanillaUiSceneCatalog.GetHierarchyPath(root.transform), includeSceneObjects);
        }

        /// <summary>
        /// Source-object overload. This also works when owner is itself an external serialized prefab
        /// reference, enabling recursive discovery through nested vanilla prefab graphs.
        /// </summary>
        public static bool TryGetTemplate(
            GameObject owner,
            string componentTypeName,
            string fieldName,
            int elementIndex,
            out GameObject source)
        {
            source = null;
            if (owner == null)
            {
                return false;
            }

            MonoBehaviour[] behaviours = owner.GetComponents<MonoBehaviour>();
            return TryGetTemplate(behaviours, componentTypeName, fieldName, elementIndex, out source);
        }

        public static bool TryCloneTemplate(
            GameObject owner,
            string componentTypeName,
            string fieldName,
            int elementIndex,
            Transform parent,
            string objectName,
            VanillaUiCloneMode cloneMode,
            bool active,
            out GameObject instance)
        {
            instance = null;
            GameObject source;
            if (!TryGetTemplate(owner, componentTypeName, fieldName, elementIndex, out source) || source == null)
            {
                return false;
            }
            return VanillaUiSceneCatalog.TryCloneSourceObject(source, parent, objectName, cloneMode, active, out instance);
        }

        /// <summary>
        /// Discovers serialized UI references on a supplied scene object or external vanilla prefab
        /// reference. Pass a referenced prefab here to walk its own serialized UI vocabulary recursively.
        /// </summary>
        public static IList<VanillaUiReferenceTemplateDescriptor> DescribeSerializedUiTemplates(
            GameObject ownerRoot,
            bool includeSceneObjects = true)
        {
            List<VanillaUiReferenceTemplateDescriptor> result = new List<VanillaUiReferenceTemplateDescriptor>();
            if (ownerRoot == null)
            {
                return result;
            }

            MonoBehaviour[] behaviours = ownerRoot.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour != null)
                {
                    AddBehaviourDescriptors(behaviour, includeSceneObjects, result);
                }
            }
            return result;
        }

        private static bool TryGetTemplate(
            MonoBehaviour[] behaviours,
            string componentTypeName,
            string fieldName,
            int elementIndex,
            out GameObject source)
        {
            source = null;
            if (behaviours == null)
            {
                return false;
            }

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || !MatchesTypeName(behaviour.GetType(), componentTypeName))
                {
                    continue;
                }

                FieldInfo field = FindSerializedField(behaviour.GetType(), fieldName);
                if (field == null)
                {
                    continue;
                }

                object value;
                try
                {
                    value = field.GetValue(behaviour);
                }
                catch
                {
                    continue;
                }

                GameObject candidate;
                if (!TryExtractGameObject(value, elementIndex, out candidate) || candidate == null || !IsUiGameObject(candidate))
                {
                    continue;
                }

                source = candidate;
                return true;
            }

            return false;
        }

        private static void AddBehaviourDescriptors(
            MonoBehaviour behaviour,
            bool includeSceneObjects,
            List<VanillaUiReferenceTemplateDescriptor> result)
        {
            if (behaviour == null || result == null)
            {
                return;
            }

            Type type = behaviour.GetType();
            List<FieldInfo> fields = GetSerializedFields(type);
            for (int fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
            {
                FieldInfo field = fields[fieldIndex];
                object value;
                try
                {
                    value = field.GetValue(behaviour);
                }
                catch
                {
                    continue;
                }

                if (value == null)
                {
                    continue;
                }

                GameObject scalar;
                if (TryConvertToGameObject(value, out scalar))
                {
                    AddDescriptor(behaviour, field, scalar, -1, includeSceneObjects, result);
                    continue;
                }

                IList list = value as IList;
                if (list == null)
                {
                    continue;
                }

                for (int i = 0; i < list.Count; i++)
                {
                    GameObject item;
                    if (TryConvertToGameObject(list[i], out item))
                    {
                        AddDescriptor(behaviour, field, item, i, includeSceneObjects, result);
                    }
                }
            }
        }

        private static void AddDescriptor(
            MonoBehaviour behaviour,
            FieldInfo field,
            GameObject source,
            int elementIndex,
            bool includeSceneObjects,
            List<VanillaUiReferenceTemplateDescriptor> result)
        {
            if (behaviour == null || field == null || source == null || !IsUiGameObject(source))
            {
                return;
            }

            bool sourceIsSceneObject = IsSceneObject(source);
            if (!includeSceneObjects && sourceIsSceneObject)
            {
                return;
            }

            VanillaUiReferenceTemplateDescriptor descriptor = new VanillaUiReferenceTemplateDescriptor();
            descriptor.SceneName = SceneManager.GetActiveScene().name ?? string.Empty;
            descriptor.OwnerHierarchyPath = VanillaUiSceneCatalog.GetHierarchyPath(behaviour.transform);
            descriptor.OwnerHierarchyOccurrenceIndex = GetScenePathOccurrenceIndex(behaviour.transform);
            descriptor.OwnerComponentType = behaviour.GetType().Name ?? string.Empty;
            descriptor.OwnerComponentFullType = behaviour.GetType().FullName ?? descriptor.OwnerComponentType;
            descriptor.FieldName = field.Name ?? string.Empty;
            descriptor.ElementIndex = elementIndex;
            descriptor.SourceName = source.name ?? string.Empty;
            descriptor.Source = source;
            descriptor.SourceIsSceneObject = sourceIsSceneObject;
            descriptor.SourceHierarchyPath = sourceIsSceneObject
                ? VanillaUiSceneCatalog.GetHierarchyPath(source.transform)
                : string.Empty;
            descriptor.IsUi = true;
            result.Add(descriptor);
        }

        private static int GetScenePathOccurrenceIndex(Transform transform)
        {
            if (transform == null)
            {
                return 0;
            }
            string path = VanillaUiSceneCatalog.GetHierarchyPath(transform);
            IList<GameObject> matches = VanillaUiSceneCatalog.FindSceneObjectsByPath(path);
            for (int i = 0; i < matches.Count; i++)
            {
                if (matches[i] == transform.gameObject)
                {
                    return i;
                }
            }
            return 0;
        }

        private static bool TryExtractGameObject(object value, int elementIndex, out GameObject source)
        {
            source = null;
            if (elementIndex < 0)
            {
                return TryConvertToGameObject(value, out source);
            }

            IList list = value as IList;
            if (list == null || elementIndex < 0 || elementIndex >= list.Count)
            {
                return false;
            }
            return TryConvertToGameObject(list[elementIndex], out source);
        }

        private static bool TryConvertToGameObject(object value, out GameObject source)
        {
            source = null;
            if (value == null)
            {
                return false;
            }

            GameObject gameObject = value as GameObject;
            if (gameObject != null)
            {
                source = gameObject;
                return true;
            }

            Component component = value as Component;
            if (component != null && component.gameObject != null)
            {
                source = component.gameObject;
                return true;
            }

            return false;
        }

        private static bool IsUiGameObject(GameObject source)
        {
            if (source == null)
            {
                return false;
            }
            if (source.GetComponent<RectTransform>() != null)
            {
                return true;
            }
            return source.GetComponentInChildren<RectTransform>(true) != null;
        }

        private static bool IsSceneObject(GameObject source)
        {
            if (source == null)
            {
                return false;
            }
            try
            {
                Scene scene = source.scene;
                return scene.IsValid() && scene.isLoaded;
            }
            catch
            {
                return false;
            }
        }

        private static bool MatchesTypeName(Type type, string requested)
        {
            if (type == null)
            {
                return false;
            }
            if (string.IsNullOrEmpty(requested))
            {
                return true;
            }
            return string.Equals(type.Name, requested, StringComparison.Ordinal) ||
                   string.Equals(type.FullName, requested, StringComparison.Ordinal);
        }

        private static FieldInfo FindSerializedField(Type type, string fieldName)
        {
            if (type == null || string.IsNullOrEmpty(fieldName))
            {
                return null;
            }

            Type cursor = type;
            while (cursor != null && cursor != typeof(object))
            {
                FieldInfo field = cursor.GetField(fieldName, DeclaredInstanceFields);
                if (field != null && IsSerializedReferenceField(field))
                {
                    return field;
                }
                cursor = cursor.BaseType;
            }
            return null;
        }

        private static List<FieldInfo> GetSerializedFields(Type type)
        {
            List<FieldInfo> result = new List<FieldInfo>();
            Type cursor = type;
            while (cursor != null && cursor != typeof(object))
            {
                FieldInfo[] fields;
                try
                {
                    fields = cursor.GetFields(DeclaredInstanceFields);
                }
                catch
                {
                    fields = new FieldInfo[0];
                }
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    if (IsSerializedReferenceField(field))
                    {
                        result.Add(field);
                    }
                }
                cursor = cursor.BaseType;
            }
            return result;
        }

        private static bool IsSerializedReferenceField(FieldInfo field)
        {
            if (field == null || field.IsStatic || field.IsNotSerialized)
            {
                return false;
            }
            if (!field.IsPublic && !field.IsDefined(typeof(SerializeField), true))
            {
                return false;
            }

            Type fieldType = field.FieldType;
            if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            {
                return true;
            }

            Type elementType = null;
            if (fieldType.IsArray)
            {
                elementType = fieldType.GetElementType();
            }
            else if (fieldType.IsGenericType)
            {
                Type genericDefinition = fieldType.GetGenericTypeDefinition();
                if (genericDefinition == typeof(List<>))
                {
                    Type[] arguments = fieldType.GetGenericArguments();
                    if (arguments.Length == 1)
                    {
                        elementType = arguments[0];
                    }
                }
            }

            return elementType != null && typeof(UnityEngine.Object).IsAssignableFrom(elementType);
        }
    }
}
