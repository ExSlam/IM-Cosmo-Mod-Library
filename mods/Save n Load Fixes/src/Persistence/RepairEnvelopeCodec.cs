using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace SaveNLoadFixes.Persistence
{
    internal sealed class RepairEnvelopeLoadState
    {
        internal bool Present;
        internal bool Valid;
        internal RepairEnvelopeV1 Envelope;
        internal string Error = string.Empty;
        internal string PhysicalPath = string.Empty;
    }

    /// <summary>
    /// Injects/extracts the SNLF root without teaching vanilla SavedData about the
    /// repair schema. With SNLF present the reader always strips the root before
    /// JsonUtility deserializes SaveManager.SavedData.
    /// </summary>
    internal static class RepairEnvelopeCodec
    {
        internal static bool TryInject(
            string vanillaJson,
            RepairEnvelopeV1 envelope,
            out string mergedJson,
            out string error)
        {
            mergedJson = string.Empty;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(vanillaJson) || envelope == null)
            {
                error = "Vanilla JSON or repair envelope is unavailable.";
                return false;
            }

            int rootOpen = SkipWhitespace(vanillaJson, 0);
            if (rootOpen >= vanillaJson.Length || vanillaJson[rootOpen] != '{')
            {
                error = "Vanilla SavedData JSON is not a root object.";
                return false;
            }

            string envelopeJson;
            try
            {
                envelopeJson = SerializeEnvelope(envelope);
            }
            catch (Exception exception)
            {
                error = "Repair envelope serialization failed: " + exception.Message;
                return false;
            }

            if (!TryValidateSerializedEnvelope(
                    envelopeJson,
                    envelope,
                    out error))
            {
                return false;
            }

            int firstContent = SkipWhitespace(vanillaJson, rootOpen + 1);
            bool emptyRoot = firstContent < vanillaJson.Length &&
                vanillaJson[firstContent] == '}';
            string property = "\"" + RepairEnvelopeConstants.RootKey + "\":" + envelopeJson;
            string insertion = emptyRoot ? property : property + ",";

            mergedJson = vanillaJson.Insert(rootOpen + 1, insertion);
            return true;
        }

        internal static bool TryExtractAndStrip(
            string fullJson,
            out string vanillaJson,
            out RepairEnvelopeLoadState state,
            out string fatalError)
        {
            vanillaJson = fullJson ?? string.Empty;
            state = new RepairEnvelopeLoadState
            {
                Present = false,
                Valid = true,
                Envelope = null,
                Error = string.Empty
            };
            fatalError = string.Empty;

            if (string.IsNullOrWhiteSpace(fullJson))
            {
                fatalError = "SavedData JSON is empty.";
                return false;
            }

            int propertyStart;
            int propertyEnd;
            int valueStart;
            int valueEnd;
            bool found;
            if (!TryFindRootProperty(
                    fullJson,
                    RepairEnvelopeConstants.RootKey,
                    out found,
                    out propertyStart,
                    out propertyEnd,
                    out valueStart,
                    out valueEnd,
                    out fatalError))
            {
                return false;
            }

            if (!found)
            {
                return true;
            }

            state.Present = true;
            string envelopeJson = fullJson.Substring(valueStart, valueEnd - valueStart);
            RepairEnvelopeV1 envelope = null;
            string shapeError;
            if (!ValidateRawEnvelopeShape(envelopeJson, out shapeError))
            {
                state.Valid = false;
                state.Error = shapeError;
            }

            if (state.Valid)
            {
                string deserializeError;
                if (!TryDeserializeEnvelope(
                        envelopeJson,
                        out envelope,
                        out deserializeError))
                {
                    state.Valid = false;
                    state.Error = "Repair envelope JSON could not be deserialized: " +
                        deserializeError;
                }
            }

            if (state.Valid)
            {
                string validationError;
                if (!ValidateEnvelope(envelope, out validationError))
                {
                    state.Valid = false;
                    state.Error = validationError;
                }
                else
                {
                    state.Envelope = envelope;
                }
            }

            int removalStart = propertyStart;
            int removalEnd = propertyEnd;

            int previous = PreviousNonWhitespace(fullJson, propertyStart - 1);
            int next = SkipWhitespace(fullJson, propertyEnd);
            if (previous >= 0 && fullJson[previous] == ',')
            {
                removalStart = previous;
            }
            else if (next < fullJson.Length && fullJson[next] == ',')
            {
                removalEnd = next + 1;
            }

            vanillaJson = fullJson.Remove(removalStart, removalEnd - removalStart);
            return true;
        }

        private static bool TryValidateSerializedEnvelope(
            string envelopeJson,
            RepairEnvelopeV1 expected,
            out string error)
        {
            error = string.Empty;

            if (!ValidateRawEnvelopeShape(envelopeJson, out error))
            {
                error = "Serialized repair envelope is incomplete: " + error;
                return false;
            }

            RepairEnvelopeV1 roundTrip;
            if (!TryDeserializeEnvelope(envelopeJson, out roundTrip, out error))
            {
                error = "Serialized repair envelope could not be read back: " + error;
                return false;
            }

            if (!ValidateEnvelope(roundTrip, out error))
            {
                error = "Serialized repair envelope failed read-back validation: " + error;
                return false;
            }

            string mismatch;
            if (!AreEnvelopeValuesEqual(expected, roundTrip, "$", out mismatch))
            {
                error = "Serialized repair envelope changed state during read-back at " + mismatch + ".";
                return false;
            }

            return true;
        }

        private static bool ValidateRawEnvelopeShape(
            string envelopeJson,
            out string error)
        {
            error = string.Empty;
            int propertyStart;
            int propertyEnd;
            int valueStart;
            int valueEnd;
            bool found;
            if (!TryFindRootProperty(
                    envelopeJson,
                    "records",
                    out found,
                    out propertyStart,
                    out propertyEnd,
                    out valueStart,
                    out valueEnd,
                    out error))
            {
                return false;
            }

            if (!found || valueStart >= valueEnd || envelopeJson[valueStart] != '{')
            {
                error = "Repair envelope raw JSON is missing the required records object.";
                return false;
            }

            string recordsJson = envelopeJson.Substring(valueStart, valueEnd - valueStart);
            if (!TryFindRootProperty(
                    recordsJson,
                    "relationship_dynamics",
                    out found,
                    out propertyStart,
                    out propertyEnd,
                    out valueStart,
                    out valueEnd,
                    out error))
            {
                return false;
            }

            if (!found || valueStart >= valueEnd || recordsJson[valueStart] != '[')
            {
                error = "Repair envelope raw records are missing relationship_dynamics.";
                return false;
            }

            return true;
        }

        private static bool ValidateEnvelope(
            RepairEnvelopeV1 envelope,
            out string error)
        {
            error = string.Empty;
            if (envelope == null)
            {
                error = "Repair envelope is null.";
                return false;
            }

            if (!string.Equals(
                    envelope.format_name,
                    RepairEnvelopeConstants.FormatName,
                    StringComparison.Ordinal))
            {
                error = "Repair envelope format_name is not recognized.";
                return false;
            }

            if (envelope.format_version != RepairEnvelopeConstants.FormatVersion)
            {
                error = "Repair envelope format_version is not supported.";
                return false;
            }

            Guid checkpoint;
            if (string.IsNullOrEmpty(envelope.checkpoint_id) ||
                !Guid.TryParse(envelope.checkpoint_id, out checkpoint))
            {
                error = "Repair envelope checkpoint_id is missing or invalid.";
                return false;
            }

            if (envelope.records == null ||
                envelope.records.relationship_dynamics == null)
            {
                error = "Repair envelope relationship_dynamics section is missing.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// The repair root is deliberately serialized without Unity's type-layout
        /// serializer. Unity can silently omit a supported-looking nested DTO, which
        /// produced the observed header-only v0.52 checkpoint. This writer supports
        /// the envelope's finite public-field tree and rejects unsupported/cyclic data.
        /// </summary>
        private static string SerializeEnvelope(RepairEnvelopeV1 envelope)
        {
            StringBuilder builder = new StringBuilder(4096);
            HashSet<object> active = new HashSet<object>(ReferenceComparer.Instance);
            AppendJsonValue(builder, envelope, active);
            return builder.ToString();
        }

        private static void AppendJsonValue(
            StringBuilder builder,
            object value,
            HashSet<object> active)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            Type type = value.GetType();
            if (type == typeof(string) || type == typeof(char))
            {
                AppendJsonString(builder, Convert.ToString(value, CultureInfo.InvariantCulture));
                return;
            }

            if (type == typeof(bool))
            {
                builder.Append((bool)value ? "true" : "false");
                return;
            }

            if (type.IsEnum)
            {
                builder.Append(Convert.ToInt64(value, CultureInfo.InvariantCulture)
                    .ToString(CultureInfo.InvariantCulture));
                return;
            }

            if (type == typeof(float))
            {
                float number = (float)value;
                if (float.IsNaN(number) || float.IsInfinity(number))
                {
                    throw new InvalidOperationException("Repair envelope contains a non-finite Single value.");
                }

                builder.Append(number.ToString("R", CultureInfo.InvariantCulture));
                return;
            }

            if (type == typeof(double))
            {
                double number = (double)value;
                if (double.IsNaN(number) || double.IsInfinity(number))
                {
                    throw new InvalidOperationException("Repair envelope contains a non-finite Double value.");
                }

                builder.Append(number.ToString("R", CultureInfo.InvariantCulture));
                return;
            }

            if (type == typeof(decimal))
            {
                builder.Append(((decimal)value).ToString("G29", CultureInfo.InvariantCulture));
                return;
            }

            if (type.IsPrimitive)
            {
                builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                return;
            }

            IList list = value as IList;
            if (list != null)
            {
                EnterValue(value, active);
                try
                {
                    builder.Append('[');
                    for (int index = 0; index < list.Count; index++)
                    {
                        if (index != 0)
                        {
                            builder.Append(',');
                        }

                        AppendJsonValue(builder, list[index], active);
                    }
                    builder.Append(']');
                }
                finally
                {
                    active.Remove(value);
                }
                return;
            }

            if (!type.IsSerializable)
            {
                throw new InvalidOperationException(
                    "Repair envelope contains unsupported type " + type.FullName + ".");
            }

            EnterValue(value, active);
            try
            {
                FieldInfo[] fields = GetSerializableFields(type);
                builder.Append('{');
                for (int index = 0; index < fields.Length; index++)
                {
                    if (index != 0)
                    {
                        builder.Append(',');
                    }

                    AppendJsonString(builder, fields[index].Name);
                    builder.Append(':');
                    AppendJsonValue(builder, fields[index].GetValue(value), active);
                }
                builder.Append('}');
            }
            finally
            {
                active.Remove(value);
            }
        }

        private static void EnterValue(object value, HashSet<object> active)
        {
            if (!active.Add(value))
            {
                throw new InvalidOperationException("Repair envelope contains an object-composition cycle.");
            }
        }

        private static FieldInfo[] GetSerializableFields(Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            Array.Sort(
                fields,
                delegate(FieldInfo left, FieldInfo right)
                {
                    return left.MetadataToken.CompareTo(right.MetadataToken);
                });
            return fields;
        }

        private static void AppendJsonString(StringBuilder builder, string value)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('"');
            for (int index = 0; index < value.Length; index++)
            {
                char current = value[index];
                switch (current)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (current < ' ')
                        {
                            builder.Append("\\u");
                            builder.Append(((int)current).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(current);
                        }
                        break;
                }
            }
            builder.Append('"');
        }

        /// <summary>
        /// Reads the envelope with the same finite, type-strict contract used by the
        /// writer. JsonUtility is intentionally not used here: the exact game build
        /// was observed to deserialize a populated relationship_dynamics array as an
        /// empty list, which made every otherwise-valid save fail the read-back gate.
        /// </summary>
        private static bool TryDeserializeEnvelope(
            string json,
            out RepairEnvelopeV1 envelope,
            out string error)
        {
            envelope = null;
            error = string.Empty;

            FiniteJsonValue root;
            if (!FiniteJsonParser.TryParse(json, out root, out error))
            {
                return false;
            }

            object materialized;
            try
            {
                if (!TryMaterializeJsonValue(
                        root,
                        typeof(RepairEnvelopeV1),
                        "$",
                        out materialized,
                        out error))
                {
                    return false;
                }
            }
            catch (Exception exception)
            {
                error = "Repair envelope materialization failed: " + exception.Message;
                return false;
            }

            envelope = materialized as RepairEnvelopeV1;
            if (envelope == null)
            {
                error = "Repair envelope root did not materialize as RepairEnvelopeV1.";
                return false;
            }
            return true;
        }

        private static bool TryMaterializeJsonValue(
            FiniteJsonValue node,
            Type targetType,
            string path,
            out object value,
            out string error)
        {
            value = null;
            error = string.Empty;
            if (node == null || targetType == null)
            {
                error = path + " has no JSON node or target type.";
                return false;
            }

            if (node.Kind == FiniteJsonKind.Null)
            {
                if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
                {
                    return true;
                }

                error = path + " is null but " + targetType.FullName + " is a value type.";
                return false;
            }

            if (targetType == typeof(string))
            {
                if (node.Kind != FiniteJsonKind.String)
                {
                    return MaterializationTypeMismatch(path, "string", node, out error);
                }
                value = node.Text;
                return true;
            }

            if (targetType == typeof(char))
            {
                if (node.Kind != FiniteJsonKind.String || node.Text == null || node.Text.Length != 1)
                {
                    return MaterializationTypeMismatch(path, "one-character string", node, out error);
                }
                value = node.Text[0];
                return true;
            }

            if (targetType == typeof(bool))
            {
                if (node.Kind != FiniteJsonKind.Boolean)
                {
                    return MaterializationTypeMismatch(path, "boolean", node, out error);
                }
                value = node.Boolean;
                return true;
            }

            if (targetType.IsEnum)
            {
                long enumValue;
                if (!TryReadInt64(node, path, out enumValue, out error))
                {
                    return false;
                }
                value = Enum.ToObject(targetType, enumValue);
                return true;
            }

            if (targetType == typeof(sbyte))
            {
                sbyte parsed;
                if (!TryReadInteger(node, path, sbyte.TryParse, out parsed, out error)) return false;
                value = parsed;
                return true;
            }
            if (targetType == typeof(byte))
            {
                byte parsed;
                if (!TryReadInteger(node, path, byte.TryParse, out parsed, out error)) return false;
                value = parsed;
                return true;
            }
            if (targetType == typeof(short))
            {
                short parsed;
                if (!TryReadInteger(node, path, short.TryParse, out parsed, out error)) return false;
                value = parsed;
                return true;
            }
            if (targetType == typeof(ushort))
            {
                ushort parsed;
                if (!TryReadInteger(node, path, ushort.TryParse, out parsed, out error)) return false;
                value = parsed;
                return true;
            }
            if (targetType == typeof(int))
            {
                int parsed;
                if (!TryReadInteger(node, path, int.TryParse, out parsed, out error)) return false;
                value = parsed;
                return true;
            }
            if (targetType == typeof(uint))
            {
                uint parsed;
                if (!TryReadInteger(node, path, uint.TryParse, out parsed, out error)) return false;
                value = parsed;
                return true;
            }
            if (targetType == typeof(long))
            {
                long parsed;
                if (!TryReadInteger(node, path, long.TryParse, out parsed, out error)) return false;
                value = parsed;
                return true;
            }
            if (targetType == typeof(ulong))
            {
                ulong parsed;
                if (!TryReadInteger(node, path, ulong.TryParse, out parsed, out error)) return false;
                value = parsed;
                return true;
            }

            if (targetType == typeof(float))
            {
                float parsed;
                if (node.Kind != FiniteJsonKind.Number ||
                    !float.TryParse(
                        node.Text,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out parsed) ||
                    float.IsNaN(parsed) ||
                    float.IsInfinity(parsed))
                {
                    return MaterializationTypeMismatch(path, "finite Single", node, out error);
                }
                value = parsed;
                return true;
            }

            if (targetType == typeof(double))
            {
                double parsed;
                if (node.Kind != FiniteJsonKind.Number ||
                    !double.TryParse(
                        node.Text,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out parsed) ||
                    double.IsNaN(parsed) ||
                    double.IsInfinity(parsed))
                {
                    return MaterializationTypeMismatch(path, "finite Double", node, out error);
                }
                value = parsed;
                return true;
            }

            if (targetType == typeof(decimal))
            {
                decimal parsed;
                if (node.Kind != FiniteJsonKind.Number ||
                    !decimal.TryParse(
                        node.Text,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out parsed))
                {
                    return MaterializationTypeMismatch(path, "Decimal", node, out error);
                }
                value = parsed;
                return true;
            }

            if (typeof(IList).IsAssignableFrom(targetType))
            {
                if (node.Kind != FiniteJsonKind.Array ||
                    !targetType.IsGenericType ||
                    targetType.GetGenericArguments().Length != 1)
                {
                    return MaterializationTypeMismatch(path, "typed array", node, out error);
                }

                IList list = Activator.CreateInstance(targetType, true) as IList;
                if (list == null)
                {
                    error = path + " target list type could not be constructed.";
                    return false;
                }

                Type elementType = targetType.GetGenericArguments()[0];
                for (int index = 0; index < node.ArrayValues.Count; index++)
                {
                    object element;
                    if (!TryMaterializeJsonValue(
                            node.ArrayValues[index],
                            elementType,
                            path + "[" + index.ToString(CultureInfo.InvariantCulture) + "]",
                            out element,
                            out error))
                    {
                        return false;
                    }
                    list.Add(element);
                }
                value = list;
                return true;
            }

            if (node.Kind != FiniteJsonKind.Object || !targetType.IsSerializable)
            {
                return MaterializationTypeMismatch(path, "serializable object", node, out error);
            }

            object instance = Activator.CreateInstance(targetType, true);
            FieldInfo[] fields = GetSerializableFields(targetType);
            for (int index = 0; index < fields.Length; index++)
            {
                FieldInfo field = fields[index];
                FiniteJsonValue fieldNode;
                if (!node.ObjectValues.TryGetValue(field.Name, out fieldNode))
                {
                    // Preserve field initializers for sections introduced after an
                    // older V1 envelope was written. Their version marker remains 0.
                    continue;
                }

                object fieldValue;
                if (!TryMaterializeJsonValue(
                        fieldNode,
                        field.FieldType,
                        path + "." + field.Name,
                        out fieldValue,
                        out error))
                {
                    return false;
                }
                field.SetValue(instance, fieldValue);
            }

            value = instance;
            return true;
        }

        private delegate bool IntegerParser<T>(
            string text,
            NumberStyles styles,
            IFormatProvider provider,
            out T value);

        private static bool TryReadInteger<T>(
            FiniteJsonValue node,
            string path,
            IntegerParser<T> parser,
            out T value,
            out string error)
        {
            value = default(T);
            error = string.Empty;
            if (node.Kind == FiniteJsonKind.Number &&
                parser(
                    node.Text,
                    NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture,
                    out value))
            {
                return true;
            }

            return MaterializationTypeMismatch(path, typeof(T).Name, node, out error);
        }

        private static bool TryReadInt64(
            FiniteJsonValue node,
            string path,
            out long value,
            out string error)
        {
            return TryReadInteger(node, path, long.TryParse, out value, out error);
        }

        private static bool MaterializationTypeMismatch(
            string path,
            string expected,
            FiniteJsonValue actual,
            out string error)
        {
            error = path + " must be a JSON " + expected + ", not " +
                (actual == null ? "<missing>" : actual.Kind.ToString()) + ".";
            return false;
        }

        private enum FiniteJsonKind
        {
            Object,
            Array,
            String,
            Number,
            Boolean,
            Null
        }

        private sealed class FiniteJsonValue
        {
            internal FiniteJsonKind Kind;
            internal Dictionary<string, FiniteJsonValue> ObjectValues;
            internal List<FiniteJsonValue> ArrayValues;
            internal string Text;
            internal bool Boolean;
        }

        private sealed class FiniteJsonParser
        {
            private const int MaxDepth = 64;
            private const int MaxNodeCount = 2000000;

            private readonly string json;
            private int index;
            private int nodeCount;
            private string error = string.Empty;

            private FiniteJsonParser(string json)
            {
                this.json = json ?? string.Empty;
            }

            internal static bool TryParse(
                string json,
                out FiniteJsonValue value,
                out string error)
            {
                value = null;
                error = string.Empty;
                if (string.IsNullOrWhiteSpace(json))
                {
                    error = "Repair envelope JSON is empty.";
                    return false;
                }

                FiniteJsonParser parser = new FiniteJsonParser(json);
                parser.SkipWhitespace();
                if (!parser.TryParseValue(0, out value))
                {
                    error = parser.error;
                    return false;
                }
                parser.SkipWhitespace();
                if (parser.index != parser.json.Length)
                {
                    error = "Repair envelope JSON has trailing content at offset " +
                        parser.index.ToString(CultureInfo.InvariantCulture) + ".";
                    value = null;
                    return false;
                }
                return true;
            }

            private bool TryParseValue(int depth, out FiniteJsonValue value)
            {
                value = null;
                if (depth > MaxDepth)
                {
                    return Fail("Repair envelope JSON exceeds the finite depth limit.");
                }
                if (++nodeCount > MaxNodeCount)
                {
                    return Fail("Repair envelope JSON exceeds the finite node limit.");
                }
                if (index >= json.Length)
                {
                    return Fail("Repair envelope JSON ends before a value.");
                }

                char current = json[index];
                if (current == '{') return TryParseObject(depth, out value);
                if (current == '[') return TryParseArray(depth, out value);
                if (current == '"')
                {
                    string parsed;
                    if (!TryParseString(out parsed)) return false;
                    value = new FiniteJsonValue
                    {
                        Kind = FiniteJsonKind.String,
                        Text = parsed
                    };
                    return true;
                }
                if (current == 't') return TryParseLiteral("true", FiniteJsonKind.Boolean, true, out value);
                if (current == 'f') return TryParseLiteral("false", FiniteJsonKind.Boolean, false, out value);
                if (current == 'n') return TryParseLiteral("null", FiniteJsonKind.Null, false, out value);
                if (current == '-' || IsDigit(current)) return TryParseNumber(out value);
                return Fail("Repair envelope JSON has an invalid value at offset " +
                    index.ToString(CultureInfo.InvariantCulture) + ".");
            }

            private bool TryParseObject(int depth, out FiniteJsonValue value)
            {
                value = new FiniteJsonValue
                {
                    Kind = FiniteJsonKind.Object,
                    ObjectValues = new Dictionary<string, FiniteJsonValue>(StringComparer.Ordinal)
                };
                index++;
                SkipWhitespace();
                if (Consume('}')) return true;

                while (true)
                {
                    string key;
                    if (!TryParseString(out key))
                    {
                        value = null;
                        return false;
                    }
                    if (value.ObjectValues.ContainsKey(key))
                    {
                        value = null;
                        return Fail("Repair envelope JSON contains duplicate object key '" + key + "'.");
                    }
                    SkipWhitespace();
                    if (!Consume(':'))
                    {
                        value = null;
                        return Fail("Repair envelope JSON object key is missing a colon.");
                    }
                    SkipWhitespace();

                    FiniteJsonValue child;
                    if (!TryParseValue(depth + 1, out child))
                    {
                        value = null;
                        return false;
                    }
                    value.ObjectValues.Add(key, child);
                    SkipWhitespace();
                    if (Consume('}')) return true;
                    if (!Consume(','))
                    {
                        value = null;
                        return Fail("Repair envelope JSON object is not comma-separated.");
                    }
                    SkipWhitespace();
                }
            }

            private bool TryParseArray(int depth, out FiniteJsonValue value)
            {
                value = new FiniteJsonValue
                {
                    Kind = FiniteJsonKind.Array,
                    ArrayValues = new List<FiniteJsonValue>()
                };
                index++;
                SkipWhitespace();
                if (Consume(']')) return true;

                while (true)
                {
                    FiniteJsonValue child;
                    if (!TryParseValue(depth + 1, out child))
                    {
                        value = null;
                        return false;
                    }
                    value.ArrayValues.Add(child);
                    SkipWhitespace();
                    if (Consume(']')) return true;
                    if (!Consume(','))
                    {
                        value = null;
                        return Fail("Repair envelope JSON array is not comma-separated.");
                    }
                    SkipWhitespace();
                }
            }

            private bool TryParseString(out string value)
            {
                value = string.Empty;
                if (!Consume('"'))
                {
                    return Fail("Repair envelope JSON string was expected at offset " +
                        index.ToString(CultureInfo.InvariantCulture) + ".");
                }

                StringBuilder builder = new StringBuilder();
                while (index < json.Length)
                {
                    char current = json[index++];
                    if (current == '"')
                    {
                        value = builder.ToString();
                        return true;
                    }
                    if (current < ' ')
                    {
                        return Fail("Repair envelope JSON string contains an unescaped control character.");
                    }
                    if (current != '\\')
                    {
                        builder.Append(current);
                        continue;
                    }
                    if (index >= json.Length)
                    {
                        return Fail("Repair envelope JSON string ends after an escape prefix.");
                    }

                    char escaped = json[index++];
                    switch (escaped)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        case 'u':
                            int codeUnit;
                            if (!TryParseHexQuad(out codeUnit)) return false;
                            builder.Append((char)codeUnit);
                            break;
                        default:
                            return Fail("Repair envelope JSON string contains an invalid escape sequence.");
                    }
                }
                return Fail("Repair envelope JSON string is unterminated.");
            }

            private bool TryParseHexQuad(out int value)
            {
                value = 0;
                if (index + 4 > json.Length)
                {
                    return Fail("Repair envelope JSON Unicode escape is truncated.");
                }
                for (int offset = 0; offset < 4; offset++)
                {
                    int digit = HexValue(json[index++]);
                    if (digit < 0)
                    {
                        return Fail("Repair envelope JSON Unicode escape contains a non-hex character.");
                    }
                    value = (value << 4) | digit;
                }
                return true;
            }

            private bool TryParseNumber(out FiniteJsonValue value)
            {
                value = null;
                int start = index;
                Consume('-');
                if (Consume('0'))
                {
                    if (index < json.Length && IsDigit(json[index]))
                    {
                        return Fail("Repair envelope JSON number contains a leading zero.");
                    }
                }
                else
                {
                    if (index >= json.Length || json[index] < '1' || json[index] > '9')
                    {
                        return Fail("Repair envelope JSON number has no integer digits.");
                    }
                    while (index < json.Length && IsDigit(json[index])) index++;
                }

                if (Consume('.'))
                {
                    if (index >= json.Length || !IsDigit(json[index]))
                    {
                        return Fail("Repair envelope JSON number has an empty fraction.");
                    }
                    while (index < json.Length && IsDigit(json[index])) index++;
                }

                if (index < json.Length && (json[index] == 'e' || json[index] == 'E'))
                {
                    index++;
                    if (index < json.Length && (json[index] == '+' || json[index] == '-')) index++;
                    if (index >= json.Length || !IsDigit(json[index]))
                    {
                        return Fail("Repair envelope JSON number has an empty exponent.");
                    }
                    while (index < json.Length && IsDigit(json[index])) index++;
                }

                value = new FiniteJsonValue
                {
                    Kind = FiniteJsonKind.Number,
                    Text = json.Substring(start, index - start)
                };
                return true;
            }

            private bool TryParseLiteral(
                string literal,
                FiniteJsonKind kind,
                bool boolean,
                out FiniteJsonValue value)
            {
                value = null;
                if (index + literal.Length > json.Length ||
                    !string.Equals(
                        json.Substring(index, literal.Length),
                        literal,
                        StringComparison.Ordinal))
                {
                    return Fail("Repair envelope JSON contains an invalid literal.");
                }
                index += literal.Length;
                value = new FiniteJsonValue
                {
                    Kind = kind,
                    Boolean = boolean
                };
                return true;
            }

            private void SkipWhitespace()
            {
                while (index < json.Length && char.IsWhiteSpace(json[index])) index++;
            }

            private bool Consume(char expected)
            {
                if (index >= json.Length || json[index] != expected) return false;
                index++;
                return true;
            }

            private bool Fail(string message)
            {
                if (string.IsNullOrEmpty(error)) error = message;
                return false;
            }

            private static bool IsDigit(char value)
            {
                return value >= '0' && value <= '9';
            }

            private static int HexValue(char value)
            {
                if (value >= '0' && value <= '9') return value - '0';
                if (value >= 'a' && value <= 'f') return value - 'a' + 10;
                if (value >= 'A' && value <= 'F') return value - 'A' + 10;
                return -1;
            }
        }

        private static bool AreEnvelopeValuesEqual(
            object expected,
            object actual,
            string path,
            out string mismatch)
        {
            mismatch = path;
            if (ReferenceEquals(expected, actual))
            {
                return true;
            }

            if (expected == null || actual == null)
            {
                return false;
            }

            Type expectedType = expected.GetType();
            if (expectedType != actual.GetType())
            {
                return false;
            }

            if (expectedType.IsPrimitive || expectedType.IsEnum ||
                expectedType == typeof(string) || expectedType == typeof(decimal))
            {
                return expected.Equals(actual);
            }

            IList expectedList = expected as IList;
            IList actualList = actual as IList;
            if (expectedList != null || actualList != null)
            {
                if (expectedList == null || actualList == null ||
                    expectedList.Count != actualList.Count)
                {
                    return false;
                }

                for (int index = 0; index < expectedList.Count; index++)
                {
                    if (!AreEnvelopeValuesEqual(
                            expectedList[index],
                            actualList[index],
                            path + "[" + index.ToString(CultureInfo.InvariantCulture) + "]",
                            out mismatch))
                    {
                        return false;
                    }
                }
                return true;
            }

            FieldInfo[] fields = GetSerializableFields(expectedType);
            for (int index = 0; index < fields.Length; index++)
            {
                FieldInfo field = fields[index];
                if (!AreEnvelopeValuesEqual(
                        field.GetValue(expected),
                        field.GetValue(actual),
                        path + "." + field.Name,
                        out mismatch))
                {
                    return false;
                }
            }
            return true;
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            internal static readonly ReferenceComparer Instance = new ReferenceComparer();

            bool IEqualityComparer<object>.Equals(object left, object right)
            {
                return ReferenceEquals(left, right);
            }

            int IEqualityComparer<object>.GetHashCode(object value)
            {
                return RuntimeHelpers.GetHashCode(value);
            }
        }

        private static bool TryFindRootProperty(
            string json,
            string targetKey,
            out bool found,
            out int propertyStart,
            out int propertyEnd,
            out int valueStart,
            out int valueEnd,
            out string error)
        {
            found = false;
            propertyStart = -1;
            propertyEnd = -1;
            valueStart = -1;
            valueEnd = -1;
            error = string.Empty;

            int index = SkipWhitespace(json, 0);
            if (index >= json.Length || json[index] != '{')
            {
                error = "SavedData JSON root is not an object.";
                return false;
            }

            index++;
            while (true)
            {
                index = SkipWhitespace(json, index);
                if (index >= json.Length)
                {
                    error = "SavedData JSON root object is unterminated.";
                    return false;
                }

                if (json[index] == '}')
                {
                    return true;
                }

                if (json[index] == ',')
                {
                    index++;
                    index = SkipWhitespace(json, index);
                }

                int currentPropertyStart = index;
                string key;
                int afterKey;
                if (!TryReadJsonString(json, index, out key, out afterKey))
                {
                    error = "SavedData JSON contains an invalid root property name.";
                    return false;
                }

                index = SkipWhitespace(json, afterKey);
                if (index >= json.Length || json[index] != ':')
                {
                    error = "SavedData JSON root property is missing a colon.";
                    return false;
                }

                index = SkipWhitespace(json, index + 1);
                int currentValueStart = index;
                int currentValueEnd;
                if (!TrySkipJsonValue(json, index, out currentValueEnd))
                {
                    error = "SavedData JSON contains an unterminated root property value.";
                    return false;
                }

                if (string.Equals(key, targetKey, StringComparison.Ordinal))
                {
                    found = true;
                    propertyStart = currentPropertyStart;
                    propertyEnd = currentValueEnd;
                    valueStart = currentValueStart;
                    valueEnd = currentValueEnd;
                    return true;
                }

                index = SkipWhitespace(json, currentValueEnd);
                if (index >= json.Length)
                {
                    error = "SavedData JSON root object is unterminated after a property value.";
                    return false;
                }

                if (json[index] == '}')
                {
                    return true;
                }

                if (json[index] != ',')
                {
                    error = "SavedData JSON root properties are not comma-separated.";
                    return false;
                }
            }
        }

        private static bool TryReadJsonString(
            string json,
            int start,
            out string value,
            out int nextIndex)
        {
            value = string.Empty;
            nextIndex = start;
            if (start >= json.Length || json[start] != '"')
            {
                return false;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            bool escaped = false;
            for (int index = start + 1; index < json.Length; index++)
            {
                char current = json[index];
                if (escaped)
                {
                    // Root keys produced by JsonUtility/SimpleJSON are ordinary ASCII.
                    // Preserve escaped characters distinctly so an escaped lookalike cannot
                    // accidentally match SNLF's reserved root key.
                    builder.Append('\\');
                    builder.Append(current);
                    escaped = false;
                    continue;
                }

                if (current == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (current == '"')
                {
                    value = builder.ToString();
                    nextIndex = index + 1;
                    return true;
                }

                builder.Append(current);
            }

            return false;
        }

        private static bool TrySkipJsonValue(
            string json,
            int start,
            out int endExclusive)
        {
            endExclusive = start;
            if (start >= json.Length)
            {
                return false;
            }

            char first = json[start];
            if (first == '"')
            {
                string ignored;
                return TryReadJsonString(json, start, out ignored, out endExclusive);
            }

            if (first == '{' || first == '[')
            {
                char open = first;
                char close = first == '{' ? '}' : ']';
                int depth = 0;
                bool inString = false;
                bool escaped = false;

                for (int index = start; index < json.Length; index++)
                {
                    char current = json[index];
                    if (inString)
                    {
                        if (escaped)
                        {
                            escaped = false;
                        }
                        else if (current == '\\')
                        {
                            escaped = true;
                        }
                        else if (current == '"')
                        {
                            inString = false;
                        }
                        continue;
                    }

                    if (current == '"')
                    {
                        inString = true;
                    }
                    else if (current == open)
                    {
                        depth++;
                    }
                    else if (current == close)
                    {
                        depth--;
                        if (depth == 0)
                        {
                            endExclusive = index + 1;
                            return true;
                        }
                    }
                }

                return false;
            }

            int primitive = start;
            while (primitive < json.Length &&
                   json[primitive] != ',' &&
                   json[primitive] != '}' &&
                   json[primitive] != ']')
            {
                primitive++;
            }

            endExclusive = primitive;
            return primitive > start;
        }

        private static int SkipWhitespace(string value, int index)
        {
            while (index < value.Length && char.IsWhiteSpace(value[index]))
            {
                index++;
            }
            return index;
        }

        private static int PreviousNonWhitespace(string value, int index)
        {
            while (index >= 0 && char.IsWhiteSpace(value[index]))
            {
                index--;
            }
            return index;
        }
    }
}
