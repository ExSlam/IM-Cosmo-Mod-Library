using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace IMDataCore
{
    /// <summary>
    /// Creates a detached SavedData graph for standalone IMDC save calls.
    ///
    /// Unity's JSON round trip remains the preferred path because it mirrors the
    /// fields vanilla itself persists. If deserialization fails after serialization
    /// succeeded, IMDC falls back to cloning only Unity-serialized fields, then
    /// reserializes the clone and requires byte-for-byte compact JSON equivalence
    /// whenever the original compact JSON was available.
    /// </summary>
    internal static class CoreStableSavedDataSnapshot
    {
        internal static bool TryCreate(
            SaveManager.SavedData source,
            out SaveManager.SavedData snapshot,
            out string compactJson,
            out string fallbackDetail,
            out string errorMessage)
        {
            snapshot = null;
            compactJson = string.Empty;
            fallbackDetail = string.Empty;
            errorMessage = string.Empty;

            if (source == null)
            {
                errorMessage = "Vanilla SavedData is null.";
                return false;
            }

            string sourceJson = null;
            Exception jsonRoundTripException = null;

            try
            {
                sourceJson = UnityEngine.JsonUtility.ToJson(source, false);
                SaveManager.SavedData jsonSnapshot =
                    UnityEngine.JsonUtility.FromJson<SaveManager.SavedData>(
                        sourceJson);
                if (jsonSnapshot != null)
                {
                    snapshot = jsonSnapshot;
                    compactJson = sourceJson ?? string.Empty;
                    return true;
                }

                jsonRoundTripException = new InvalidOperationException(
                    "Unity JsonUtility returned a null SavedData snapshot.");
            }
            catch (Exception exception)
            {
                jsonRoundTripException = exception;
            }

            try
            {
                Dictionary<object, object> visited =
                    new Dictionary<object, object>(
                        ReferenceIdentityComparer.Instance);
                SaveManager.SavedData fieldSnapshot =
                    CloneValue(source, visited) as SaveManager.SavedData;
                if (fieldSnapshot == null || ReferenceEquals(fieldSnapshot, source))
                {
                    throw new InvalidOperationException(
                        "Serialized-field clone did not produce a detached " +
                        "SavedData instance.");
                }

                string fieldSnapshotJson = UnityEngine.JsonUtility.ToJson(
                    fieldSnapshot,
                    false);
                if (sourceJson != null &&
                    !string.Equals(
                        sourceJson,
                        fieldSnapshotJson,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Serialized-field clone produced a non-equivalent " +
                        "SavedData JSON graph.");
                }

                snapshot = fieldSnapshot;
                compactJson = sourceJson ?? fieldSnapshotJson ?? string.Empty;
                fallbackDetail =
                    "Recovered the detached save graph with the " +
                    "Unity-serialized-field clone after JsonUtility cloning " +
                    "failed.";
                return true;
            }
            catch (Exception fieldCloneException)
            {
                errorMessage =
                    BuildFailureDetail(
                        "JSON clone failed",
                        jsonRoundTripException) +
                    "; serialized-field clone failed: " +
                    fieldCloneException.Message;
                return false;
            }
        }

        private static object CloneValue(
            object source,
            Dictionary<object, object> visited)
        {
            if (source == null)
            {
                return null;
            }

            Type type = source.GetType();
            if (IsImmutable(type))
            {
                return source;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                // Unity object references are engine-owned identities rather than
                // mutable SavedData DTO subgraphs. JsonUtility persists them using
                // Unity's own reference semantics, so preserve the reference.
                return source;
            }

            if (!type.IsValueType)
            {
                object existing;
                if (visited.TryGetValue(source, out existing))
                {
                    return existing;
                }
            }

            if (type.IsArray)
            {
                return CloneArray((Array)source, visited);
            }

            IList sourceList = source as IList;
            if (sourceList != null && !type.IsArray)
            {
                return CloneList(sourceList, type, visited);
            }

            if (source is IDictionary)
            {
                // Unity JsonUtility does not serialize dictionaries. Keeping the
                // reference is safe for SavedData identity because it cannot affect
                // the vanilla JSON payload or its fingerprint.
                return source;
            }

            object clone = type.IsValueType
                ? Activator.CreateInstance(type)
                : FormatterServices.GetUninitializedObject(type);

            if (!type.IsValueType)
            {
                visited[source] = clone;
            }

            foreach (FieldInfo field in GetUnitySerializedFields(type))
            {
                object fieldValue = field.GetValue(source);
                object clonedValue = CloneValue(fieldValue, visited);
                field.SetValue(clone, clonedValue);
            }

            return clone;
        }

        private static object CloneArray(
            Array source,
            Dictionary<object, object> visited)
        {
            Type elementType = source.GetType().GetElementType();
            int rank = source.Rank;
            int[] lengths = new int[rank];
            for (int dimension = 0; dimension < rank; dimension++)
            {
                lengths[dimension] = source.GetLength(dimension);
            }

            Array clone = Array.CreateInstance(elementType, lengths);
            visited[source] = clone;

            int[] indices = new int[rank];
            CloneArrayDimension(source, clone, visited, indices, 0);
            return clone;
        }

        private static void CloneArrayDimension(
            Array source,
            Array clone,
            Dictionary<object, object> visited,
            int[] indices,
            int dimension)
        {
            int length = source.GetLength(dimension);
            for (int index = 0; index < length; index++)
            {
                indices[dimension] = index;
                if (dimension + 1 < source.Rank)
                {
                    CloneArrayDimension(
                        source,
                        clone,
                        visited,
                        indices,
                        dimension + 1);
                    continue;
                }

                object value = source.GetValue(indices);
                clone.SetValue(CloneValue(value, visited), indices);
            }
        }

        private static object CloneList(
            IList source,
            Type type,
            Dictionary<object, object> visited)
        {
            IList clone;
            try
            {
                clone = (IList)Activator.CreateInstance(type);
            }
            catch (Exception)
            {
                clone = (IList)FormatterServices.GetUninitializedObject(type);
            }

            visited[source] = clone;
            for (int index = 0; index < source.Count; index++)
            {
                clone.Add(CloneValue(source[index], visited));
            }

            return clone;
        }

        private static IEnumerable<FieldInfo> GetUnitySerializedFields(Type type)
        {
            for (Type current = type;
                 current != null && current != typeof(object);
                 current = current.BaseType)
            {
                FieldInfo[] fields = current.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

                for (int index = 0; index < fields.Length; index++)
                {
                    FieldInfo field = fields[index];
                    if (field.IsStatic || field.IsInitOnly || field.IsNotSerialized)
                    {
                        continue;
                    }

                    if (field.IsPublic || HasUnitySerializeAttribute(field))
                    {
                        yield return field;
                    }
                }
            }
        }

        private static bool HasUnitySerializeAttribute(FieldInfo field)
        {
            object[] attributes = field.GetCustomAttributes(false);
            for (int index = 0; index < attributes.Length; index++)
            {
                Type attributeType = attributes[index] != null
                    ? attributes[index].GetType()
                    : null;
                string fullName = attributeType != null
                    ? attributeType.FullName
                    : string.Empty;
                if (string.Equals(
                        fullName,
                        "UnityEngine.SerializeField",
                        StringComparison.Ordinal) ||
                    string.Equals(
                        fullName,
                        "UnityEngine.SerializeReference",
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsImmutable(Type type)
        {
            return type.IsPrimitive ||
                type.IsEnum ||
                type == typeof(string) ||
                type == typeof(decimal) ||
                type == typeof(DateTime) ||
                type == typeof(TimeSpan) ||
                type == typeof(Guid) ||
                type == typeof(Type);
        }

        private static string BuildFailureDetail(
            string prefix,
            Exception exception)
        {
            return prefix + ": " +
                (exception != null
                    ? exception.Message
                    : "unknown failure");
        }

        private sealed class ReferenceIdentityComparer : IEqualityComparer<object>
        {
            internal static readonly ReferenceIdentityComparer Instance =
                new ReferenceIdentityComparer();

            bool IEqualityComparer<object>.Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            int IEqualityComparer<object>.GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
