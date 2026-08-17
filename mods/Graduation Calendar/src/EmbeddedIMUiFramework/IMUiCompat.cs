using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GraduationCalendar.EmbeddedIMUiFramework
{
    /// <summary>
    /// Compatibility shims for Idol Manager's older Unity API surface.
    /// The game ships API assemblies where several newer convenience overloads/members are absent even
    /// though the underlying runtime data still exists. Keep those version differences isolated here.
    /// </summary>
    internal static class IMUiCompat
    {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static T GetComponentInChildren<T>(GameObject root) where T : Component
        {
            if (root == null) return null;
            T[] values = root.GetComponentsInChildren<T>(true);
            return values != null && values.Length > 0 ? values[0] : null;
        }

        public static T GetComponentInChildren<T>(Component root) where T : Component
        {
            return root == null ? null : GetComponentInChildren<T>(root.gameObject);
        }

        public static T[] FindAllLoadedObjects<T>() where T : UnityEngine.Object
        {
            try
            {
                MethodInfo method = typeof(Resources).GetMethod(
                    "FindObjectsOfTypeAll",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new Type[] { typeof(Type) },
                    null);
                if (method != null)
                {
                    Array values = method.Invoke(null, new object[] { typeof(T) }) as Array;
                    if (values != null)
                    {
                        List<T> matches = new List<T>(values.Length);
                        for (int i = 0; i < values.Length; i++)
                        {
                            T value = values.GetValue(i) as T;
                            if (value != null) matches.Add(value);
                        }
                        return matches.ToArray();
                    }
                }
            }
            catch { }

            try
            {
                MethodInfo method = typeof(UnityEngine.Object).GetMethod(
                    "FindObjectsOfType",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new Type[] { typeof(Type) },
                    null);
                if (method != null)
                {
                    Array values = method.Invoke(null, new object[] { typeof(T) }) as Array;
                    if (values != null)
                    {
                        List<T> matches = new List<T>(values.Length);
                        for (int i = 0; i < values.Length; i++)
                        {
                            T value = values.GetValue(i) as T;
                            if (value != null) matches.Add(value);
                        }
                        return matches.ToArray();
                    }
                }
            }
            catch { }

            return new T[0];
        }

        public static GameObject[] GetCurrentSceneRoots()
        {
            Transform[] all = FindAllLoadedObjects<Transform>();
            List<GameObject> roots = new List<GameObject>();
            HashSet<int> seen = new HashSet<int>();
            int preferredHandle = GetCurrentSceneHandle(all);

            for (int i = 0; i < all.Length; i++)
            {
                Transform transform = all[i];
                if (transform == null || transform.parent != null || transform.gameObject == null) continue;

                int handle;
                bool loaded;
                if (!TryGetSceneIdentity(transform.gameObject, out handle, out loaded) || !loaded) continue;
                if (preferredHandle != int.MinValue && handle != preferredHandle) continue;

                int id = transform.gameObject.GetInstanceID();
                if (seen.Add(id)) roots.Add(transform.gameObject);
            }

            // If scene reflection is unavailable on an older runtime, retain a conservative active-root
            // fallback so the framework still functions rather than returning an empty catalog.
            if (roots.Count == 0)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    Transform transform = all[i];
                    if (transform == null || transform.parent != null || transform.gameObject == null) continue;
                    if (!transform.gameObject.activeInHierarchy) continue;
                    int id = transform.gameObject.GetInstanceID();
                    if (seen.Add(id)) roots.Add(transform.gameObject);
                }
            }

            return roots.ToArray();
        }

        public static int GetCurrentSceneHandle()
        {
            // Camera.main is available in both Idol Manager scenes and gives us a cheap scene token,
            // avoiding a full Resources scan on every template lookup after the index is built.
            try
            {
                Camera camera = Camera.main;
                if (camera != null && camera.gameObject != null)
                {
                    int handle;
                    bool loaded;
                    if (TryGetSceneIdentity(camera.gameObject, out handle, out loaded) && loaded) return handle;
                }
            }
            catch { }

            return GetCurrentSceneHandle(FindAllLoadedObjects<Transform>());
        }

        private static int GetCurrentSceneHandle(Transform[] all)
        {
            if (all == null) return int.MinValue;

            // Prefer an active object. Loaded prefab assets returned by Resources.FindObjectsOfTypeAll are
            // not active in a scene, while gameplay/main-menu roots are.
            for (int i = 0; i < all.Length; i++)
            {
                Transform transform = all[i];
                if (transform == null || transform.gameObject == null || !transform.gameObject.activeInHierarchy) continue;
                int handle;
                bool loaded;
                if (TryGetSceneIdentity(transform.gameObject, out handle, out loaded) && loaded) return handle;
            }

            // If the entire UI root is temporarily inactive, any loaded scene object is still preferable
            // to an asset/prefab object.
            for (int i = 0; i < all.Length; i++)
            {
                Transform transform = all[i];
                if (transform == null || transform.gameObject == null) continue;
                int handle;
                bool loaded;
                if (TryGetSceneIdentity(transform.gameObject, out handle, out loaded) && loaded) return handle;
            }

            return int.MinValue;
        }

        public static string GetCurrentSceneName()
        {
            // Application.loadedLevelName exists on the older API line Idol Manager targets, but resolve it
            // by reflection so this source also compiles against newer/stripped Unity reference assemblies.
            try
            {
                PropertyInfo property = typeof(Application).GetProperty("loadedLevelName", AnyStatic);
                if (property != null)
                {
                    string value = property.GetValue(null, null) as string;
                    if (!string.IsNullOrEmpty(value)) return value;
                }
            }
            catch { }

            Transform[] all = FindAllLoadedObjects<Transform>();
            for (int i = 0; i < all.Length; i++)
            {
                Transform transform = all[i];
                if (transform == null || transform.gameObject == null || !transform.gameObject.activeInHierarchy) continue;
                string sceneName;
                if (TryGetSceneName(transform.gameObject, out sceneName) && !string.IsNullOrEmpty(sceneName)) return sceneName;
            }

            return string.Empty;
        }

        public static bool IsSceneObject(GameObject source)
        {
            if (source == null) return false;
            int handle;
            bool loaded;
            return TryGetSceneIdentity(source, out handle, out loaded) && loaded;
        }

        private static bool TryGetSceneIdentity(GameObject source, out int handle, out bool loaded)
        {
            handle = int.MinValue;
            loaded = false;
            if (source == null) return false;

            try
            {
                PropertyInfo sceneProperty = source.GetType().GetProperty("scene", AnyInstance);
                if (sceneProperty == null) return false;
                object scene = sceneProperty.GetValue(source, null);
                if (scene == null) return false;

                Type sceneType = scene.GetType();
                PropertyInfo loadedProperty = sceneType.GetProperty("isLoaded", AnyInstance);
                PropertyInfo handleProperty = sceneType.GetProperty("handle", AnyInstance);
                FieldInfo loadedField = loadedProperty == null ? sceneType.GetField("isLoaded", AnyInstance) : null;
                FieldInfo handleField = handleProperty == null ? sceneType.GetField("handle", AnyInstance) : null;

                object loadedValue = loadedProperty != null ? loadedProperty.GetValue(scene, null) : (loadedField != null ? loadedField.GetValue(scene) : null);
                object handleValue = handleProperty != null ? handleProperty.GetValue(scene, null) : (handleField != null ? handleField.GetValue(scene) : null);

                if (loadedValue is bool) loaded = (bool)loadedValue;
                if (handleValue is int) handle = (int)handleValue;
                return loadedValue is bool || handleValue is int;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetSceneName(GameObject source, out string name)
        {
            name = string.Empty;
            if (source == null) return false;
            try
            {
                PropertyInfo sceneProperty = source.GetType().GetProperty("scene", AnyInstance);
                if (sceneProperty == null) return false;
                object scene = sceneProperty.GetValue(source, null);
                if (scene == null) return false;
                PropertyInfo nameProperty = scene.GetType().GetProperty("name", AnyInstance);
                if (nameProperty == null) return false;
                name = nameProperty.GetValue(scene, null) as string ?? string.Empty;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void SetColorBlockColor(ref ColorBlock block, string state, Color color)
        {
            string fieldName;
            switch (state)
            {
                case "normal": fieldName = "m_NormalColor"; break;
                case "highlighted": fieldName = "m_HighlightedColor"; break;
                case "pressed": fieldName = "m_PressedColor"; break;
                case "selected": fieldName = "m_SelectedColor"; break;
                case "disabled": fieldName = "m_DisabledColor"; break;
                default: return;
            }

            object boxed = block;
            try
            {
                FieldInfo field = typeof(ColorBlock).GetField(fieldName, AnyInstance);
                if (field != null)
                {
                    field.SetValue(boxed, color);
                    block = (ColorBlock)boxed;
                    return;
                }

                // Some Unity versions expose public setters instead of serialized backing fields.
                PropertyInfo property = typeof(ColorBlock).GetProperty(state + "Color", AnyInstance | BindingFlags.IgnoreCase);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(boxed, color, null);
                    block = (ColorBlock)boxed;
                }
            }
            catch { }
        }

        public static bool TryGetPersistentTargets(UnityEventBase unityEvent, out List<UnityEngine.Object> targets)
        {
            targets = new List<UnityEngine.Object>();
            if (unityEvent == null) return false;

            // Newer Unity API path, invoked by reflection because Idol Manager's compile-time UnityEventBase
            // omits these convenience methods.
            try
            {
                MethodInfo countMethod = typeof(UnityEventBase).GetMethod("GetPersistentEventCount", AnyInstance, null, Type.EmptyTypes, null);
                MethodInfo targetMethod = typeof(UnityEventBase).GetMethod("GetPersistentTarget", AnyInstance, null, new Type[] { typeof(int) }, null);
                if (countMethod != null && targetMethod != null)
                {
                    object countValue = countMethod.Invoke(unityEvent, null);
                    int count = countValue is int ? (int)countValue : 0;
                    for (int i = 0; i < count; i++)
                    {
                        UnityEngine.Object target = targetMethod.Invoke(unityEvent, new object[] { i }) as UnityEngine.Object;
                        targets.Add(target);
                    }
                    return true;
                }
            }
            catch
            {
                targets.Clear();
            }

            // Idol Manager's UnityEngine.UI build stores the same information in UnityEventBase's serialized
            // m_PersistentCalls -> m_Calls -> m_Target graph. Reading it is safe and lets Template clones keep
            // internal ScrollRect/Slider wiring while rejecting listeners that point outside the clone.
            try
            {
                FieldInfo persistentField = FindField(unityEvent.GetType(), "m_PersistentCalls");
                if (persistentField == null) return false;
                object persistentGroup = persistentField.GetValue(unityEvent);
                if (persistentGroup == null) return true;

                FieldInfo callsField = FindField(persistentGroup.GetType(), "m_Calls");
                if (callsField == null) return false;
                IList calls = callsField.GetValue(persistentGroup) as IList;
                if (calls == null) return false;

                for (int i = 0; i < calls.Count; i++)
                {
                    object call = calls[i];
                    if (call == null)
                    {
                        targets.Add(null);
                        continue;
                    }
                    FieldInfo targetField = FindField(call.GetType(), "m_Target");
                    UnityEngine.Object target = targetField != null ? targetField.GetValue(call) as UnityEngine.Object : null;
                    targets.Add(target);
                }
                return true;
            }
            catch
            {
                targets.Clear();
                return false;
            }
        }

        private static FieldInfo FindField(Type type, string name)
        {
            Type cursor = type;
            while (cursor != null && cursor != typeof(object))
            {
                FieldInfo field = cursor.GetField(name, AnyInstance | BindingFlags.DeclaredOnly);
                if (field != null) return field;
                cursor = cursor.BaseType;
            }
            return null;
        }
    }
}
