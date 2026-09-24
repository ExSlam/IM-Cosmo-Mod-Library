using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using SaveNLoadFixes.Repairs;
using UnityEngine;

namespace SaveNLoadFixes.Persistence
{
    internal sealed class RepairEnvelopeLoadState
    {
        internal bool Present;
        internal bool Valid;
        internal bool RecoveredFromSimpleJsonRewrite;
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
        private static readonly CultureInfo DotGroupSimpleJsonCulture =
            CultureInfo.GetCultureInfo("de-DE");

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
                    bool recognizedSimpleJsonRewrite;
                    string recoveryError;
                    if (TryDeserializeSimpleJsonRewrittenEnvelope(
                            envelopeJson,
                            out envelope,
                            out recognizedSimpleJsonRewrite,
                            out recoveryError))
                    {
                        state.RecoveredFromSimpleJsonRewrite = true;
                    }
                    else
                    {
                        state.Valid = false;
                        state.Error = "Repair envelope JSON could not be deserialized: " +
                            deserializeError;
                        if (recognizedSimpleJsonRewrite &&
                            !string.IsNullOrEmpty(recoveryError))
                        {
                            state.Error += " The vanilla SimpleJSON rewrite signature was present, " +
                                "but exact recovery was rejected: " + recoveryError;
                        }
                    }
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

            FiniteJsonValue root;
            if (!FiniteJsonParser.TryParse(envelopeJson, out root, out error) ||
                root == null || root.Kind != FiniteJsonKind.Object)
            {
                return false;
            }
            FiniteJsonValue records;
            if (!root.ObjectValues.TryGetValue("records", out records) ||
                records == null || records.Kind != FiniteJsonKind.Object)
            {
                error = "Repair envelope raw JSON is missing the required records object.";
                return false;
            }
            FiniteJsonValue marker;
            if (!records.ObjectValues.TryGetValue("wide_numeric_state_version", out marker))
            {
                // Envelopes written before A33 remain valid legacy input.
                return true;
            }
            if (marker == null ||
                (marker.Kind != FiniteJsonKind.Number && marker.Kind != FiniteJsonKind.String) ||
                (!string.Equals(marker.Text, "1", StringComparison.Ordinal) &&
                 !string.Equals(marker.Text, "2", StringComparison.Ordinal) &&
                 !string.Equals(marker.Text, "3", StringComparison.Ordinal)))
            {
                // The typed materializer/section validator reports zero or unsupported
                // marker values. Only a claimed current section needs the raw presence proof.
                return true;
            }
            FiniteJsonValue wide;
            if (!records.ObjectValues.TryGetValue("wide_numeric_state", out wide) ||
                wide == null || wide.Kind != FiniteJsonKind.Object)
            {
                error = "A33 wide_numeric_state marker is present but its raw state object is missing.";
                return false;
            }
            int wideVersion = int.Parse(marker.Text, CultureInfo.InvariantCulture);
            return TryRequireCompleteRawSchema(
                wide,
                typeof(WideNumericStateRecordV1),
                "$.records.wide_numeric_state",
                wideVersion,
                out error);
        }

        private static bool TryRequireCompleteRawSchema(
            FiniteJsonValue node,
            Type targetType,
            string path,
            int wideNumericVersion,
            out string error)
        {
            error = string.Empty;
            if (node == null || targetType == null)
            {
                error = path + " is missing from the raw A33 section.";
                return false;
            }
            if (typeof(IList).IsAssignableFrom(targetType))
            {
                if (node.Kind != FiniteJsonKind.Array || !targetType.IsGenericType)
                {
                    // The ordinary typed materializer supplies the exact type error.
                    return true;
                }
                Type elementType = targetType.GetGenericArguments()[0];
                for (int index = 0; index < node.ArrayValues.Count; index++)
                {
                    if (!TryRequireCompleteRawSchema(
                            node.ArrayValues[index],
                            elementType,
                            path + "[" + index.ToString(CultureInfo.InvariantCulture) + "]",
                            wideNumericVersion,
                            out error))
                    {
                        return false;
                    }
                }
                return true;
            }
            if (targetType == typeof(string) || targetType == typeof(char) ||
                targetType.IsPrimitive || targetType.IsEnum ||
                targetType == typeof(decimal))
            {
                return true;
            }
            if (node.Kind != FiniteJsonKind.Object || !targetType.IsSerializable)
            {
                // The ordinary typed materializer supplies the exact type error.
                return true;
            }
            FieldInfo[] fields = GetSerializableFields(targetType);
            for (int index = 0; index < fields.Length; index++)
            {
                FieldInfo field = fields[index];
                FiniteJsonValue child;
                if (!node.ObjectValues.TryGetValue(field.Name, out child))
                {
                    if (targetType == typeof(WideNumericStateRecordV1) &&
                        ((wideNumericVersion == 1 &&
                          (string.Equals(field.Name, "has_story_ch4_scandal_points", StringComparison.Ordinal) ||
                           string.Equals(field.Name, "story_ch4_scandal_points", StringComparison.Ordinal))) ||
                         (wideNumericVersion < 3 &&
                          string.Equals(field.Name, "business_contract_payments", StringComparison.Ordinal))))
                    {
                        continue;
                    }
                    error = path + "." + field.Name +
                        " is missing from a marker-present A33 section.";
                    return false;
                }
                if (!TryRequireCompleteRawSchema(
                        child,
                        field.FieldType,
                        path + "." + field.Name,
                        wideNumericVersion,
                        out error))
                {
                    return false;
                }
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

            if (!string.IsNullOrEmpty(envelope.imdc_content_fingerprint) &&
                !RepairEnvelopeContentFingerprint.IsValid(
                    envelope.imdc_content_fingerprint))
            {
                error =
                    "Repair envelope imdc_content_fingerprint is not a canonical " +
                    "lower-case SHA-256 checkpoint witness.";
                return false;
            }

            if (envelope.imdc_persistence_state <
                    RepairEnvelopeConstants.IMDataCorePersistenceUnknown ||
                envelope.imdc_persistence_state >
                    RepairEnvelopeConstants.IMDataCorePersistenceFailed)
            {
                error = "Repair envelope imdc_persistence_state is not recognized.";
                return false;
            }

            if (envelope.imdc_persistence_state ==
                    RepairEnvelopeConstants.IMDataCorePersistenceDurable &&
                !RepairEnvelopeContentFingerprint.IsValid(
                    envelope.imdc_content_fingerprint))
            {
                error =
                    "Repair envelope claims durable IM Data Core persistence without " +
                    "a canonical checkpoint witness.";
                return false;
            }

            if (envelope.imdc_persistence_state ==
                    RepairEnvelopeConstants.IMDataCorePersistenceFailed &&
                !string.IsNullOrEmpty(envelope.imdc_content_fingerprint))
            {
                error =
                    "Repair envelope claims failed IM Data Core persistence while " +
                    "also carrying a checkpoint witness.";
                return false;
            }

            if (envelope.records == null ||
                envelope.records.relationship_dynamics == null)
            {
                error = "Repair envelope relationship_dynamics section is missing.";
                return false;
            }

            string nullStringPath;
            if (!TryFindNullStringField(
                    envelope,
                    "$",
                    new HashSet<object>(ReferenceComparer.Instance),
                    out nullStringPath))
            {
                error = nullStringPath + " is a null string. SNLF requires non-null " +
                    "string fields so the literal text 'null' remains distinguishable " +
                    "from SimpleJSON's rewrite of a JSON null.";
                return false;
            }

            if (!WideNumericState.TryValidateEnvelopeRecord(envelope.records, out error))
            {
                error = "Invalid A33 wide_numeric_state: " + error;
                return false;
            }

            return true;
        }

        private static bool TryFindNullStringField(
            object value,
            string path,
            HashSet<object> active,
            out string nullPath)
        {
            nullPath = string.Empty;
            if (value == null)
            {
                return true;
            }

            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || type == typeof(decimal) ||
                type == typeof(string) || type == typeof(char))
            {
                return true;
            }

            if (!active.Add(value))
            {
                nullPath = path + " contains an object-composition cycle";
                return false;
            }

            try
            {
                IList list = value as IList;
                if (list != null)
                {
                    for (int index = 0; index < list.Count; index++)
                    {
                        if (!TryFindNullStringField(
                                list[index],
                                path + "[" + index.ToString(CultureInfo.InvariantCulture) + "]",
                                active,
                                out nullPath))
                        {
                            return false;
                        }
                    }
                    return true;
                }

                FieldInfo[] fields = GetSerializableFields(type);
                for (int index = 0; index < fields.Length; index++)
                {
                    FieldInfo field = fields[index];
                    object fieldValue = field.GetValue(value);
                    string fieldPath = path + "." + field.Name;
                    if (field.FieldType == typeof(string) && fieldValue == null)
                    {
                        nullPath = fieldPath;
                        return false;
                    }
                    if (!TryFindNullStringField(
                            fieldValue,
                            fieldPath,
                            active,
                            out nullPath))
                    {
                        return false;
                    }
                }
                return true;
            }
            finally
            {
                active.Remove(value);
            }
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

        internal static void AppendJsonString(StringBuilder builder, string value)
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

            if (!TryValidateExactInt64Witnesses(envelope, out error))
            {
                envelope = null;
                return false;
            }
            return true;
        }

        /// <summary>
        /// SaveManager.FixSaveFile parses SavedData through the game's bundled
        /// SimpleJSON implementation and writes JSONData.ToString(). In this exact
        /// build JSONData.ToString() quotes every scalar even when Numberize tagged it
        /// as a number or Boolean. Recover only that uniform, schema-directed shape;
        /// mixed coercion remains invalid and the ordinary reader stays type-strict.
        /// </summary>
        private static bool TryDeserializeSimpleJsonRewrittenEnvelope(
            string json,
            out RepairEnvelopeV1 envelope,
            out bool recognizedRewrite,
            out string error)
        {
            envelope = null;
            recognizedRewrite = false;
            error = string.Empty;

            FiniteJsonValue root;
            if (!FiniteJsonParser.TryParse(json, out root, out error) ||
                root == null ||
                root.Kind != FiniteJsonKind.Object)
            {
                return false;
            }

            FiniteJsonValue formatName;
            FiniteJsonValue formatVersion;
            if (!root.ObjectValues.TryGetValue("format_name", out formatName) ||
                formatName == null ||
                formatName.Kind != FiniteJsonKind.String ||
                !string.Equals(
                    formatName.Text,
                    RepairEnvelopeConstants.FormatName,
                    StringComparison.Ordinal) ||
                !root.ObjectValues.TryGetValue("format_version", out formatVersion) ||
                formatVersion == null ||
                formatVersion.Kind != FiniteJsonKind.String)
            {
                error = string.Empty;
                return false;
            }

            recognizedRewrite = true;
            SimpleJsonRecoveryContext recovery = new SimpleJsonRecoveryContext();
            if (!TryRequireUniformSimpleJsonScalarShape(root, "$", out error) ||
                !TryValidateSimpleJsonRecoverySchema(
                    root,
                    typeof(RepairEnvelopeV1),
                    "$",
                    recovery,
                    out error) ||
                !TryPrepareSimpleJsonInt64Witnesses(root, recovery, out error))
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
                        recovery,
                        out materialized,
                        out error))
                {
                    return false;
                }
            }
            catch (Exception exception)
            {
                error = "Repair envelope compatibility materialization failed: " +
                    exception.Message;
                return false;
            }

            if (recovery.ConvertedScalarCount == 0)
            {
                error = "No uniformly stringified numeric or Boolean field was recovered.";
                return false;
            }

            envelope = materialized as RepairEnvelopeV1;
            if (envelope == null)
            {
                error = "Recovered root did not materialize as RepairEnvelopeV1.";
                return false;
            }

            if (!TryValidateExactInt64Witnesses(envelope, out error))
            {
                envelope = null;
                return false;
            }

            string normalizedJson;
            try
            {
                normalizedJson = SerializeEnvelope(envelope);
            }
            catch (Exception exception)
            {
                error = "Recovered repair envelope could not be normalized: " +
                    exception.Message;
                envelope = null;
                return false;
            }

            if (!TryValidateSerializedEnvelope(normalizedJson, envelope, out error))
            {
                error = "Recovered repair envelope failed canonical read-back: " + error;
                envelope = null;
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
            return TryMaterializeJsonValue(
                node,
                targetType,
                path,
                null,
                out value,
                out error);
        }

        private static bool TryMaterializeJsonValue(
            FiniteJsonValue node,
            Type targetType,
            string path,
            SimpleJsonRecoveryContext recovery,
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

            if (recovery != null &&
                node.Kind == FiniteJsonKind.String &&
                string.Equals(node.Text, "null", StringComparison.Ordinal) &&
                targetType != typeof(string) &&
                targetType != typeof(char) &&
                (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null))
            {
                recovery.ConvertedScalarCount++;
                return true;
            }

            if (node.Kind == FiniteJsonKind.Null)
            {
                if (recovery != null)
                {
                    error = path + " is a raw JSON null, not the uniformly stringified " +
                        "SimpleJSON startup-rewrite form.";
                    return false;
                }

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
                if (recovery != null &&
                    string.Equals(node.Text, "null", StringComparison.Ordinal))
                {
                    error = path + " is the ambiguous string 'null'; FixSaveFile makes " +
                        "an original JSON null indistinguishable from that literal text.";
                    return false;
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
                if (recovery == null)
                {
                    if (node.Kind != FiniteJsonKind.Boolean)
                    {
                        return MaterializationTypeMismatch(path, "boolean", node, out error);
                    }
                    value = node.Boolean;
                    return true;
                }

                if (node.Kind == FiniteJsonKind.String &&
                    (string.Equals(node.Text, "true", StringComparison.Ordinal) ||
                     string.Equals(node.Text, "false", StringComparison.Ordinal)))
                {
                    value = string.Equals(node.Text, "true", StringComparison.Ordinal);
                    recovery.ConvertedScalarCount++;
                    return true;
                }

                return MaterializationTypeMismatch(
                    path,
                    "uniformly stringified Boolean",
                    node,
                    out error);
            }

            if (targetType.IsEnum)
            {
                long enumValue;
                if (!TryReadInt64(node, path, recovery, out enumValue, out error))
                {
                    return false;
                }
                value = Enum.ToObject(targetType, enumValue);
                return true;
            }

            if (targetType == typeof(sbyte))
            {
                sbyte parsed;
                if (!TryReadInteger(node, path, recovery, sbyte.TryParse, out parsed, out error)) return false;
                value = parsed;
                return true;
            }
            if (targetType == typeof(byte))
            {
                byte parsed;
                if (!TryReadInteger(node, path, recovery, byte.TryParse, out parsed, out error)) return false;
                value = parsed;
                return true;
            }
            if (targetType == typeof(short))
            {
                short parsed;
                if (!TryReadInteger(node, path, recovery, short.TryParse, out parsed, out error)) return false;
                value = parsed;
                return true;
            }
            if (targetType == typeof(ushort))
            {
                ushort parsed;
                if (!TryReadInteger(node, path, recovery, ushort.TryParse, out parsed, out error)) return false;
                value = parsed;
                return true;
            }
            if (targetType == typeof(int))
            {
                int parsed;
                if (!TryReadInteger(node, path, recovery, int.TryParse, out parsed, out error)) return false;
                value = parsed;
                return true;
            }
            if (targetType == typeof(uint))
            {
                uint parsed;
                if (!TryReadInteger(node, path, recovery, uint.TryParse, out parsed, out error)) return false;
                value = parsed;
                return true;
            }
            if (targetType == typeof(long))
            {
                long parsed;
                if (!TryReadInteger(node, path, recovery, long.TryParse, out parsed, out error)) return false;
                value = parsed;
                return true;
            }
            if (targetType == typeof(ulong))
            {
                ulong parsed;
                if (!TryReadInteger(node, path, recovery, ulong.TryParse, out parsed, out error)) return false;
                value = parsed;
                return true;
            }

            if (targetType == typeof(float))
            {
                float parsed;
                if (recovery == null)
                {
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

                if (node.Kind != FiniteJsonKind.String ||
                    !TryRecoverUniqueSimpleJsonSingle(
                        node.Text,
                        !recovery.HasSafeInvariantSingleEvidence,
                        out parsed))
                {
                    error = path + " does not have a provably unique finite Single " +
                        "preimage through FixSaveFile's Double conversion.";
                    return false;
                }
                recovery.ConvertedScalarCount++;
                value = parsed;
                return true;
            }

            if (targetType == typeof(double))
            {
                double parsed;
                FiniteJsonKind expectedKind = recovery == null
                    ? FiniteJsonKind.Number
                    : FiniteJsonKind.String;
                if (node.Kind != expectedKind ||
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
                if (recovery != null)
                {
                    string canonical = parsed.ToString("R", CultureInfo.InvariantCulture);
                    if (!string.Equals(canonical, node.Text, StringComparison.Ordinal))
                    {
                        error = path + " was reformatted by SimpleJSON and cannot be " +
                            "proven to preserve the original Double exactly.";
                        return false;
                    }
                    recovery.ConvertedScalarCount++;
                }
                value = parsed;
                return true;
            }

            if (targetType == typeof(decimal))
            {
                decimal parsed;
                FiniteJsonKind expectedKind = recovery == null
                    ? FiniteJsonKind.Number
                    : FiniteJsonKind.String;
                if (node.Kind != expectedKind ||
                    !decimal.TryParse(
                        node.Text,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out parsed))
                {
                    return MaterializationTypeMismatch(path, "Decimal", node, out error);
                }
                if (recovery != null)
                {
                    string canonical = parsed.ToString("G29", CultureInfo.InvariantCulture);
                    if (!string.Equals(canonical, node.Text, StringComparison.Ordinal))
                    {
                        error = path + " was reformatted by SimpleJSON and cannot be " +
                            "proven to preserve the original Decimal exactly.";
                        return false;
                    }
                    recovery.ConvertedScalarCount++;
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
                            recovery,
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
                        recovery,
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
            SimpleJsonRecoveryContext recovery,
            IntegerParser<T> parser,
            out T value,
            out string error)
        {
            value = default(T);
            error = string.Empty;
            FiniteJsonKind expectedKind = recovery == null
                ? FiniteJsonKind.Number
                : FiniteJsonKind.String;

            if (recovery != null && typeof(T) == typeof(long))
            {
                long witnessedValue;
                if (recovery.ExactInt64Values.TryGetValue(path, out witnessedValue))
                {
                    if (node.Kind != FiniteJsonKind.String)
                    {
                        return MaterializationTypeMismatch(
                            path,
                            "uniformly stringified Int64",
                            node,
                            out error);
                    }

                    value = (T)(object)witnessedValue;
                    recovery.ConvertedScalarCount++;
                    return true;
                }
            }

            if (node.Kind == expectedKind &&
                parser(
                    node.Text,
                    NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture,
                    out value))
            {
                if (recovery != null)
                {
                    string canonical = Convert.ToString(value, CultureInfo.InvariantCulture);
                    if (!string.Equals(canonical, node.Text, StringComparison.Ordinal))
                    {
                        error = path + " was reformatted by SimpleJSON and cannot be " +
                            "proven to preserve the original integer exactly.";
                        return false;
                    }

                    if (typeof(T) == typeof(long))
                    {
                        long longValue = (long)(object)value;
                        if (!HasUniqueSimpleJsonInt64Preimage(longValue, node.Text))
                        {
                            error = path + " does not have a provably unique Int64 " +
                                "preimage through FixSaveFile's Int32/Double conversion.";
                            return false;
                        }
                    }

                    recovery.ConvertedScalarCount++;
                }
                return true;
            }

            return MaterializationTypeMismatch(path, typeof(T).Name, node, out error);
        }

        private static bool TryReadInt64(
            FiniteJsonValue node,
            string path,
            SimpleJsonRecoveryContext recovery,
            out long value,
            out string error)
        {
            return TryReadInteger(
                node,
                path,
                recovery,
                long.TryParse,
                out value,
                out error);
        }

        private sealed class SimpleJsonRecoveryContext
        {
            internal int ConvertedScalarCount;
            internal bool HasSafeInvariantSingleEvidence;
            internal readonly Dictionary<string, long> ExactInt64Values =
                new Dictionary<string, long>(StringComparer.Ordinal);
        }

        /// <summary>
        /// Proves that every member in a compatibility envelope is covered by the
        /// current V1 schema before materialization can discard anything. Single
        /// uniqueness, including dot-as-thousands culture collisions, is proved by
        /// TryRecoverUniqueSimpleJsonSingle when each value is materialized.
        /// </summary>
        private static bool TryValidateSimpleJsonRecoverySchema(
            FiniteJsonValue node,
            Type targetType,
            string path,
            SimpleJsonRecoveryContext recovery,
            out string error)
        {
            error = string.Empty;
            return TryCollectSimpleJsonRecoverySchemaEvidence(
                    node,
                    targetType,
                    path,
                    recovery,
                    out error);
        }

        private static bool TryCollectSimpleJsonRecoverySchemaEvidence(
            FiniteJsonValue node,
            Type targetType,
            string path,
            SimpleJsonRecoveryContext recovery,
            out string error)
        {
            error = string.Empty;
            if (node == null || targetType == null || recovery == null)
            {
                error = path + " has no JSON node, target type, or recovery context.";
                return false;
            }

            if (node.Kind == FiniteJsonKind.String &&
                string.Equals(node.Text, "null", StringComparison.Ordinal) &&
                targetType != typeof(string) &&
                targetType != typeof(char) &&
                (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null))
            {
                return true;
            }

            if (targetType == typeof(float))
            {
                if (node.Kind == FiniteJsonKind.String &&
                    node.Text != null &&
                    node.Text.IndexOf('.') >= 0)
                {
                    // Dot-as-thousands parsing either removes this dot or formats a
                    // non-integral result with another decimal separator. A surviving
                    // invariant dot therefore proves one safe culture class for the
                    // whole SimpleJSON pass.
                    recovery.HasSafeInvariantSingleEvidence = true;
                }
                return true;
            }

            if (typeof(IList).IsAssignableFrom(targetType))
            {
                if (node.Kind != FiniteJsonKind.Array ||
                    !targetType.IsGenericType ||
                    targetType.GetGenericArguments().Length != 1)
                {
                    // The ordinary materializer will emit the precise type mismatch.
                    return true;
                }

                Type elementType = targetType.GetGenericArguments()[0];
                for (int index = 0; index < node.ArrayValues.Count; index++)
                {
                    if (!TryCollectSimpleJsonRecoverySchemaEvidence(
                            node.ArrayValues[index],
                            elementType,
                            path + "[" + index.ToString(CultureInfo.InvariantCulture) + "]",
                            recovery,
                            out error))
                    {
                        return false;
                    }
                }
                return true;
            }

            if (targetType == typeof(string) || targetType == typeof(char) ||
                targetType.IsPrimitive || targetType.IsEnum ||
                targetType == typeof(decimal))
            {
                return true;
            }

            if (node.Kind != FiniteJsonKind.Object || !targetType.IsSerializable)
            {
                // The ordinary materializer will emit the precise type mismatch.
                return true;
            }

            FieldInfo[] fields = GetSerializableFields(targetType);
            Dictionary<string, FieldInfo> fieldsByName =
                new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
            for (int index = 0; index < fields.Length; index++)
            {
                fieldsByName.Add(fields[index].Name, fields[index]);
            }

            foreach (KeyValuePair<string, FiniteJsonValue> pair in node.ObjectValues)
            {
                FieldInfo field;
                if (!fieldsByName.TryGetValue(pair.Key, out field))
                {
                    error = path + "." + pair.Key +
                        " is not a field in the known SNLF V1 schema, so its quoted " +
                        "value has no unique typed interpretation.";
                    return false;
                }

                if (!TryCollectSimpleJsonRecoverySchemaEvidence(
                        pair.Value,
                        field.FieldType,
                        path + "." + field.Name,
                        recovery,
                        out error))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryRequireUniformSimpleJsonScalarShape(
            FiniteJsonValue node,
            string path,
            out string error)
        {
            error = string.Empty;
            if (node == null)
            {
                error = path + " is missing.";
                return false;
            }

            if (node.Kind == FiniteJsonKind.String)
            {
                return true;
            }

            if (node.Kind == FiniteJsonKind.Object)
            {
                foreach (KeyValuePair<string, FiniteJsonValue> pair in node.ObjectValues)
                {
                    if (!TryRequireUniformSimpleJsonScalarShape(
                            pair.Value,
                            path + "." + pair.Key,
                            out error))
                    {
                        return false;
                    }
                }
                return true;
            }

            if (node.Kind == FiniteJsonKind.Array)
            {
                for (int index = 0; index < node.ArrayValues.Count; index++)
                {
                    if (!TryRequireUniformSimpleJsonScalarShape(
                            node.ArrayValues[index],
                            path + "[" + index.ToString(CultureInfo.InvariantCulture) + "]",
                            out error))
                    {
                        return false;
                    }
                }
                return true;
            }

            error = path + " contains a " + node.Kind.ToString() +
                " scalar. FixSaveFile's known SimpleJSON rewrite quotes every scalar, " +
                "so this mixed shape is not eligible for compatibility recovery.";
            return false;
        }

        private static bool TryPrepareSimpleJsonInt64Witnesses(
            FiniteJsonValue root,
            SimpleJsonRecoveryContext recovery,
            out string error)
        {
            error = string.Empty;
            if (root == null || recovery == null || root.Kind != FiniteJsonKind.Object)
            {
                error = "SimpleJSON recovery root/context is unavailable.";
                return false;
            }

            FiniteJsonValue records;
            if (!root.ObjectValues.TryGetValue("records", out records) ||
                records == null ||
                records.Kind != FiniteJsonKind.Object)
            {
                error = "SimpleJSON recovery root has no records object.";
                return false;
            }

            FiniteJsonValue selected;
            if (!records.ObjectValues.TryGetValue("selected_business_proposal", out selected) ||
                selected == null ||
                (selected.Kind == FiniteJsonKind.String &&
                 string.Equals(selected.Text, "null", StringComparison.Ordinal)))
            {
                return true;
            }

            if (selected.Kind != FiniteJsonKind.Object)
            {
                error = "$.records.selected_business_proposal is not an object or " +
                    "stringified null.";
                return false;
            }

            FiniteJsonValue decimalWitness;
            if (!selected.ObjectValues.TryGetValue("liability_decimal", out decimalWitness))
            {
                // Pre-witness V1 envelopes remain eligible only through the unique
                // forward-preimage proof in TryReadInteger<Int64>.
                return true;
            }

            if (decimalWitness == null || decimalWitness.Kind != FiniteJsonKind.String ||
                string.IsNullOrEmpty(decimalWitness.Text))
            {
                error = "$.records.selected_business_proposal.liability_decimal is " +
                    "present but is not a non-empty decimal string.";
                return false;
            }

            long exactValue;
            if (!long.TryParse(
                    decimalWitness.Text,
                    NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture,
                    out exactValue) ||
                !string.Equals(
                    exactValue.ToString(CultureInfo.InvariantCulture),
                    decimalWitness.Text,
                    StringComparison.Ordinal))
            {
                error = "$.records.selected_business_proposal.liability_decimal is " +
                    "not a canonical Int64 decimal witness.";
                return false;
            }

            FiniteJsonValue numericLiability;
            if (!selected.ObjectValues.TryGetValue("liability", out numericLiability) ||
                numericLiability == null ||
                numericLiability.Kind != FiniteJsonKind.String)
            {
                error = "$.records.selected_business_proposal.liability is not the " +
                    "uniformly stringified numeric companion of liability_decimal.";
                return false;
            }

            if (!MatchesSimpleJsonInt64ForwardImage(exactValue, numericLiability.Text))
            {
                error = "$.records.selected_business_proposal liability numeric text " +
                    "is not the SimpleJSON forward image of its exact decimal witness.";
                return false;
            }

            recovery.ExactInt64Values.Add(
                "$.records.selected_business_proposal.liability",
                exactValue);
            return true;
        }

        private static bool TryValidateExactInt64Witnesses(
            RepairEnvelopeV1 envelope,
            out string error)
        {
            error = string.Empty;
            SelectedBusinessProposalRecordV1 record = envelope == null ||
                envelope.records == null
                ? null
                : envelope.records.selected_business_proposal;
            if (record == null || string.IsNullOrEmpty(record.liability_decimal))
            {
                return true;
            }

            long exactValue;
            if (!long.TryParse(
                    record.liability_decimal,
                    NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture,
                    out exactValue) ||
                !string.Equals(
                    exactValue.ToString(CultureInfo.InvariantCulture),
                    record.liability_decimal,
                    StringComparison.Ordinal))
            {
                error = "$.records.selected_business_proposal.liability_decimal must " +
                    "be a canonical Int64 decimal string.";
                return false;
            }

            if (record.liability != exactValue)
            {
                error = "$.records.selected_business_proposal liability and its exact " +
                    "decimal witness disagree.";
                return false;
            }

            return true;
        }

        private static bool TryRecoverUniqueSimpleJsonSingle(
            string observed,
            bool includeDotGroupPreimages,
            out float recovered)
        {
            recovered = 0f;
            if (string.IsNullOrEmpty(observed))
            {
                return false;
            }

            Dictionary<int, float> candidates = new Dictionary<int, float>();
            AddSingleCandidatesFromObserved(
                observed,
                CultureInfo.CurrentCulture,
                candidates);
            AddSingleCandidatesFromObserved(
                observed,
                CultureInfo.InvariantCulture,
                candidates);
            if (includeDotGroupPreimages)
            {
                AddSingleCandidatesFromObserved(
                    observed,
                    DotGroupSimpleJsonCulture,
                    candidates);
                AddDotGroupSingleCandidates(observed, candidates);
            }

            bool found = false;
            foreach (KeyValuePair<int, float> pair in candidates)
            {
                float candidate = pair.Value;
                if (!MatchesSimpleJsonSingleForwardImage(
                        candidate,
                        observed,
                        includeDotGroupPreimages))
                {
                    continue;
                }

                if (found)
                {
                    // Positive and negative zero are semantically identical under the
                    // existing V1 writer/equality contract; every other duplicate is
                    // an information-losing preimage collision.
                    if (candidate == 0f && recovered == 0f)
                    {
                        continue;
                    }
                    recovered = 0f;
                    return false;
                }

                recovered = candidate;
                found = true;
            }

            return found;
        }

        private static void AddSingleCandidatesFromObserved(
            string observed,
            CultureInfo culture,
            Dictionary<int, float> candidates)
        {
            double parsed;
            if (!double.TryParse(
                    observed,
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    culture,
                    out parsed) ||
                double.IsNaN(parsed) ||
                double.IsInfinity(parsed))
            {
                return;
            }

            float center = (float)parsed;
            if (float.IsPositiveInfinity(center) && parsed > 0d)
            {
                center = float.MaxValue;
            }
            else if (float.IsNegativeInfinity(center) && parsed < 0d)
            {
                center = float.MinValue;
            }
            AddSingleCandidateNeighborhood(center, candidates);
        }

        private static void AddSingleCandidate(
            float candidate,
            Dictionary<int, float> candidates)
        {
            if (float.IsNaN(candidate) || float.IsInfinity(candidate))
            {
                return;
            }

            int bits = BitConverter.ToInt32(BitConverter.GetBytes(candidate), 0);
            if (!candidates.ContainsKey(bits))
            {
                candidates.Add(bits, candidate);
            }
        }

        private static void AddDotGroupSingleCandidates(
            string observed,
            Dictionary<int, float> candidates)
        {
            if (string.IsNullOrEmpty(observed))
            {
                return;
            }

            string sign = string.Empty;
            string digits = observed;
            if (digits[0] == '-')
            {
                sign = "-";
                digits = digits.Substring(1);
            }

            if (digits.Length == 0)
            {
                return;
            }
            for (int index = 0; index < digits.Length; index++)
            {
                if (digits[index] < '0' || digits[index] > '9')
                {
                    AddScaledDotGroupSingleCandidates(observed, candidates);
                    return;
                }
            }

            AddScaledDotGroupSingleCandidates(observed, candidates);

            // A dot-as-thousands culture removes the invariant decimal point before
            // formatting. Enumerate every fixed-decimal Single source that could
            // therefore collapse to the observed integral text. Canonical R-text
            // validation below excludes fabricated trailing-zero spellings.
            for (int split = 1; split < digits.Length; split++)
            {
                AddCanonicalSingleCandidateSource(
                    sign + digits.Substring(0, split) + "." +
                    digits.Substring(split),
                    candidates);
            }

            for (int leadingZeros = 0; leadingZeros < 9; leadingZeros++)
            {
                AddCanonicalSingleCandidateSource(
                    sign + "0." + new string('0', leadingZeros) + digits,
                    candidates);
            }
        }

        private static void AddScaledDotGroupSingleCandidates(
            string observed,
            Dictionary<int, float> candidates)
        {
            double dotGroupValue;
            if (!double.TryParse(
                    observed,
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    DotGroupSimpleJsonCulture,
                    out dotGroupValue) ||
                double.IsNaN(dotGroupValue) ||
                double.IsInfinity(dotGroupValue))
            {
                return;
            }

            double divisor = 1d;
            // A round-trip Single has at most nine significant digits. Removing its
            // one invariant decimal point can therefore shift the numeric value by
            // at most nine decimal places, whether the source used fixed or exponent
            // notation. Each candidate is still checked through the exact formatter.
            for (int decimalShift = 1; decimalShift <= 9; decimalShift++)
            {
                divisor *= 10d;
                float candidate = (float)(dotGroupValue / divisor);
                if (float.IsPositiveInfinity(candidate) && dotGroupValue > 0d)
                {
                    candidate = float.MaxValue;
                }
                else if (float.IsNegativeInfinity(candidate) && dotGroupValue < 0d)
                {
                    candidate = float.MinValue;
                }
                AddSingleCandidateNeighborhood(candidate, candidates);
            }
        }

        private static void AddSingleCandidateNeighborhood(
            float center,
            Dictionary<int, float> candidates)
        {
            AddSingleCandidate(center, candidates);

            float lower = center;
            float upper = center;
            // Single round-trip text has at most nine significant digits while the
            // SimpleJSON Double formatter retains fifteen. A small ULP neighborhood
            // is sufficient, and every admitted candidate is still forward-checked.
            for (int index = 0; index < 8; index++)
            {
                lower = NextSingle(lower, false);
                upper = NextSingle(upper, true);
                AddSingleCandidate(lower, candidates);
                AddSingleCandidate(upper, candidates);
            }
        }

        private static void AddCanonicalSingleCandidateSource(
            string source,
            Dictionary<int, float> candidates)
        {
            float candidate;
            if (!float.TryParse(
                    source,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out candidate) ||
                float.IsNaN(candidate) ||
                float.IsInfinity(candidate) ||
                !string.Equals(
                    candidate.ToString("R", CultureInfo.InvariantCulture),
                    source,
                    StringComparison.Ordinal))
            {
                return;
            }

            string image;
            if (TryFormatSimpleJsonSingle(
                    candidate,
                    DotGroupSimpleJsonCulture,
                    out image))
            {
                AddSingleCandidate(candidate, candidates);
            }
        }

        private static float NextSingle(float value, bool upward)
        {
            if (float.IsNaN(value))
            {
                return value;
            }

            if (value == 0f)
            {
                int zeroNeighborBits = upward ? 1 : unchecked((int)0x80000001);
                return BitConverter.ToSingle(BitConverter.GetBytes(zeroNeighborBits), 0);
            }

            int bits = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
            if ((value > 0f) == upward)
            {
                bits++;
            }
            else
            {
                bits--;
            }
            return BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
        }

        private static bool MatchesSimpleJsonSingleForwardImage(
            float value,
            string observed,
            bool includeDotGroupPreimages)
        {
            string currentCultureImage;
            if (TryFormatSimpleJsonSingle(value, CultureInfo.CurrentCulture, out currentCultureImage) &&
                string.Equals(currentCultureImage, observed, StringComparison.Ordinal))
            {
                return true;
            }

            string invariantImage;
            if (TryFormatSimpleJsonSingle(
                    value,
                    CultureInfo.InvariantCulture,
                    out invariantImage) &&
                string.Equals(invariantImage, observed, StringComparison.Ordinal))
            {
                return true;
            }

            if (!includeDotGroupPreimages)
            {
                return false;
            }

            string dotGroupImage;
            return TryFormatSimpleJsonSingle(
                    value,
                    DotGroupSimpleJsonCulture,
                    out dotGroupImage) &&
                string.Equals(dotGroupImage, observed, StringComparison.Ordinal);
        }

        private static bool TryFormatSimpleJsonSingle(
            float value,
            CultureInfo culture,
            out string image)
        {
            image = string.Empty;
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return false;
            }

            string source = value.ToString("R", CultureInfo.InvariantCulture);
            int int32Value;
            if (int.TryParse(source, NumberStyles.Integer, culture, out int32Value))
            {
                image = int32Value.ToString(culture);
                return true;
            }

            double doubleValue;
            if (!double.TryParse(
                    source,
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    culture,
                    out doubleValue) ||
                double.IsNaN(doubleValue) ||
                double.IsInfinity(doubleValue))
            {
                return false;
            }

            image = doubleValue.ToString(culture);
            return true;
        }

        private static bool HasUniqueSimpleJsonInt64Preimage(
            long candidate,
            string observed)
        {
            if (!MatchesSimpleJsonInt64ForwardImage(candidate, observed))
            {
                return false;
            }

            if (candidate >= int.MinValue && candidate <= int.MaxValue)
            {
                return true;
            }

            // The bundled SimpleJSON falls back to Double and then ordinary G-format
            // text. Restrict witness-free recovery to at most 15 decimal digits: every
            // integer in this range is exactly representable as binary64, G15 retains
            // all of its decimal digits, and the forward mapping is one-to-one.
            const long LargestProvenUniqueUnwitnessedInteger = 999999999999999L;
            if (candidate > LargestProvenUniqueUnwitnessedInteger ||
                candidate < -LargestProvenUniqueUnwitnessedInteger)
            {
                return false;
            }

            double asDouble = candidate;
            if ((long)asDouble != candidate)
            {
                return false;
            }

            // Defense in depth against runtime-specific G-format behavior. A second
            // adjacent preimage proves ambiguity even inside the conservative bound.
            if (candidate != long.MinValue &&
                MatchesSimpleJsonInt64ForwardImage(candidate - 1L, observed))
            {
                return false;
            }
            if (candidate != long.MaxValue &&
                MatchesSimpleJsonInt64ForwardImage(candidate + 1L, observed))
            {
                return false;
            }

            return true;
        }

        private static bool MatchesSimpleJsonInt64ForwardImage(
            long value,
            string observed)
        {
            string currentCultureImage;
            if (TryFormatSimpleJsonInt64(value, CultureInfo.CurrentCulture, out currentCultureImage) &&
                string.Equals(currentCultureImage, observed, StringComparison.Ordinal))
            {
                return true;
            }

            string invariantImage;
            return TryFormatSimpleJsonInt64(value, CultureInfo.InvariantCulture, out invariantImage) &&
                string.Equals(invariantImage, observed, StringComparison.Ordinal);
        }

        private static bool TryFormatSimpleJsonInt64(
            long value,
            CultureInfo culture,
            out string image)
        {
            image = string.Empty;
            string canonical = value.ToString(CultureInfo.InvariantCulture);

            int int32Value;
            if (int.TryParse(canonical, NumberStyles.Integer, culture, out int32Value))
            {
                image = int32Value.ToString(culture);
                return true;
            }

            double doubleValue;
            if (!double.TryParse(
                    canonical,
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    culture,
                    out doubleValue) ||
                double.IsNaN(doubleValue) ||
                double.IsInfinity(doubleValue))
            {
                return false;
            }

            image = doubleValue.ToString(culture);
            return true;
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

        internal enum FiniteJsonKind
        {
            Object,
            Array,
            String,
            Number,
            Boolean,
            Null
        }

        internal sealed class FiniteJsonValue
        {
            internal FiniteJsonKind Kind;
            internal Dictionary<string, FiniteJsonValue> ObjectValues;
            internal List<FiniteJsonValue> ArrayValues;
            internal string Text;
            internal bool Boolean;
        }

        internal sealed class FiniteJsonParser
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

        internal static bool TryFindRootPropertyForRawMigration(
            string json,
            string targetKey,
            out bool found,
            out int propertyStart,
            out int propertyEnd,
            out int valueStart,
            out int valueEnd,
            out string error)
        {
            return TryFindRootProperty(
                json,
                targetKey,
                out found,
                out propertyStart,
                out propertyEnd,
                out valueStart,
                out valueEnd,
                out error);
        }

        internal static bool TrySkipJsonValueForRawMigration(
            string json,
            int start,
            out int endExclusive)
        {
            return TrySkipJsonValue(json, start, out endExclusive);
        }

        internal static int SkipWhitespaceForRawMigration(
            string value,
            int index)
        {
            return SkipWhitespace(value, index);
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
